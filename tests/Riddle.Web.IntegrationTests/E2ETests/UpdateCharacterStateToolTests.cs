using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;

namespace Riddle.Web.IntegrationTests.E2ETests;

/// <summary>
/// E2E tests for the update_character_state LLM tool.
/// Verifies the full flow from tool execution through SignalR to UI rendering.
/// These tests catch sender/receiver contract mismatches that transport-layer tests cannot detect.
/// 
/// See docs/e2e_testing_philosophy.md for patterns and rationale.
/// </summary>
[Collection("E2E")]
public class UpdateCharacterStateToolTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly PlaywrightFixture _playwrightFixture;
    private IPage _page = null!;
    private IBrowserContext _context = null!;
    
    public UpdateCharacterStateToolTests(CustomWebApplicationFactory factory, PlaywrightFixture playwrightFixture)
    {
        _factory = factory;
        _playwrightFixture = playwrightFixture;
    }
    
    public async Task InitializeAsync()
    {
        _context = await _playwrightFixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        _page = await _context.NewPageAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _page.CloseAsync();
        await _context.DisposeAsync();
    }

    /// <summary>
    /// REQ: HLR-COMBAT-001 - The software shall propagate the updated character data to the 
    /// following UI areas when the LLM invokes the update_character_state tool:
    /// - DM Dashboard Combat Tracker
    /// - DM Dashboard Party Panel
    /// - Player Dashboard Combat Tracker
    /// - Player Dashboard Party Members
    /// - Player Character Card
    /// 
    /// Setup:
    /// - Start test server with seeded campaign containing test character (CurrentHp=30, MaxHp=30)
    /// - Start combat encounter including test character
    /// - Navigate browser to DM dashboard with CombatTracker visible
    /// 
    /// Steps:
    /// 1. Navigate to DM dashboard for test campaign
    /// 2. Verify initial HP display shows "30"
    /// 3. Inject ToolExecutor via server's DI container
    /// 4. Execute: update_character_state(character_name="TestHero", key="current_hp", value=20)
    /// 5. Wait for SignalR event propagation (max 5 seconds)
    /// 6. Query CombatantCard for test character
    /// 
    /// Expected:
    /// - CombatantCard displays HP as "20" (not "30")
    /// - No JavaScript console errors related to SignalR payload processing
    /// 
    /// This test WILL FAIL if there's a sender/receiver contract mismatch
    /// (e.g., sender sends "current_hp" but receiver expects "CurrentHp")
    /// </summary>
    [Fact]
    public async Task Should_UpdateCombatTrackerHp_When_ToolExecutorChangesCharacterHp()
    {
        // Arrange - Create test campaign with character
        const string testCharacterId = "test-hero-001";
        const string testCharacterName = "TestHero";
        const int initialHp = 30;
        const int updatedHp = 20;
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "E2E Test Campaign",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = testCharacterId,
                    Name = testCharacterName,
                    Type = "PC",
                    Class = "Fighter",
                    Race = "Human",
                    Level = 5,
                    MaxHp = initialHp,
                    CurrentHp = initialHp,
                    ArmorClass = 16
                }
            ]);
        
        // Arrange - Start combat with the test character
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id, 
            [
                new CombatantInfo(
                    Id: testCharacterId,
                    Name: testCharacterName,
                    Type: "PC",
                    Initiative: 15,
                    CurrentHp: initialHp,
                    MaxHp: initialHp,
                    IsDefeated: false,
                    IsSurprised: false
                )
            ]);
        }
        
        // Arrange - Navigate to DM dashboard with NetworkIdle for Blazor async rendering
        var url = $"{_factory.ServerAddress}/dm/{campaign.Id}";
        await _page.GotoAsync(url, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 15000
        });
        
        // Wait for CombatTracker to render with our test character
        var combatantSelector = $"[data-testid='combatant-{testCharacterId}']";
        await _page.WaitForSelectorAsync(combatantSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Verify initial HP state using Expect() polling
        var hpLocator = _page.Locator($"{combatantSelector} [data-testid='hp-current']");
        await Expect(hpLocator).ToHaveTextAsync(initialHp.ToString(), 
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
        
        // Act - Execute tool via DI container to update HP
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{testCharacterName}}",
                "key": "current_hp",
                "value": {{updatedHp}}
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Wait for SignalR propagation and verify UI update using Expect() polling
        // This is the critical assertion: if sender/receiver contract doesn't match,
        // the UI will never update and this will timeout with a clear error message
        await Expect(hpLocator).ToHaveTextAsync(updatedHp.ToString(), 
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
    }
}
