using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;

namespace Riddle.Web.IntegrationTests.E2ETests;

/// <summary>
/// E2E tests for the update_character_state LLM tool - Player Dashboard perspective.
/// Verifies that HP changes propagate via SignalR to the Player Dashboard.
/// 
/// This test catches the bug where DM Dashboard updates correctly but Player Dashboard does not.
/// See docs/e2e_testing_philosophy.md for patterns and rationale.
/// </summary>
[Collection("E2E")]
public class UpdateCharacterStateToolTests_PlayerDashboard : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly PlaywrightFixture _playwrightFixture;
    private IPage _playerPage = null!;
    private IBrowserContext _playerContext = null!;
    
    public UpdateCharacterStateToolTests_PlayerDashboard(CustomWebApplicationFactory factory, PlaywrightFixture playwrightFixture)
    {
        _factory = factory;
        _playwrightFixture = playwrightFixture;
    }
    
    public async Task InitializeAsync()
    {
        _playerContext = await _playwrightFixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        _playerPage = await _playerContext.NewPageAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _playerPage.CloseAsync();
        await _playerContext.DisposeAsync();
    }

    /// <summary>
    /// REQ: HLR-COMBAT-000 - Player Dashboard must display updated character HP when 
    /// the LLM invokes update_character_state tool.
    /// 
    /// BUG REPORT: During gameplay, LLM called update_character_state to update 
    /// "Will the Wise" character HP. DM Dashboard Combat Tracker and Party data 
    /// updated correctly. BUT Player Dashboard did NOT update:
    /// - Combat Tracker HP not updated
    /// - Character Card HP bar not updated
    /// 
    /// This test MUST FAIL initially to prove it detects the bug.
    /// 
    /// Setup:
    /// - Create campaign with test character assigned to test player
    /// - Start combat encounter including test character
    /// - Navigate Player browser to player dashboard
    /// 
    /// Steps:
    /// 1. Navigate to Player dashboard for test campaign
    /// 2. Verify initial HP display shows "30" in character card
    /// 3. Execute: update_character_state(character_name="TestHero", key="current_hp", value=10)
    /// 4. Wait for SignalR event propagation (max 5 seconds)
    /// 
    /// Expected:
    /// - PlayerCharacterCard displays HP as "10 / 30" (not "30 / 30")
    /// </summary>
    [Fact]
    public async Task Should_UpdatePlayerCharacterCardHp_When_ToolExecutorChangesCharacterHp()
    {
        // Arrange - Create test campaign with character assigned to test player
        const string testCharacterId = "test-hero-player-001";
        const string testCharacterName = "TestHero";
        const int initialHp = 30;
        const int updatedHp = 10;
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "E2E Player HP Test Campaign",
            dmUserId: "dm-user-id", // Different from TestUserId so player isn't DM
            party:
            [
                new Character
                {
                    Id = testCharacterId,
                    Name = testCharacterName,
                    Type = "PC",
                    Class = "Wizard",
                    Race = "Human",
                    Level = 5,
                    MaxHp = initialHp,
                    CurrentHp = initialHp,
                    ArmorClass = 12,
                    PlayerId = TestAuthHandler.TestUserId // Character assigned to test player
                }
            ]);
        
        // Arrange - Navigate to Player dashboard
        // The test user is authenticated as TestUserId who owns the character
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 15000
        });
        
        // Wait for PlayerCharacterCard to render with our test character
        // The HP display shows "CurrentHp / MaxHp" format
        var hpTextSelector = "text=/\\d+\\s*\\/\\s*\\d+/"; // Regex to match "X / Y" pattern
        await _playerPage.WaitForSelectorAsync(hpTextSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Verify initial HP state - look for the "30 / 30" text in the character card
        // PlayerCharacterCard displays: "@Character.CurrentHp / @Character.MaxHp"
        var hpDisplayLocator = _playerPage.GetByText($"{initialHp} / {initialHp}").First;
        await Expect(hpDisplayLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
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
        
        // Assert - Wait for SignalR propagation and verify UI update
        // The character card should now show "10 / 30" instead of "30 / 30"
        // This is the critical assertion that SHOULD FAIL to detect the bug
        var updatedHpLocator = _playerPage.GetByText($"{updatedHp} / {initialHp}").First;
        await Expect(updatedHpLocator).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }
    
    /// <summary>
    /// REQ: HLR-COMBAT-000 - Player Dashboard Combat Tracker must display updated combatant HP
    /// when the LLM invokes update_character_state tool during combat.
    /// 
    /// This test verifies the Combat Tracker in the Player Dashboard updates.
    /// </summary>
    [Fact]
    public async Task Should_UpdatePlayerCombatTrackerHp_When_ToolExecutorChangesCharacterHpDuringCombat()
    {
        // Arrange - Create test campaign with character assigned to test player
        const string testCharacterId = "test-hero-combat-001";
        const string testCharacterName = "CombatHero";
        const int initialHp = 25;
        const int updatedHp = 15;
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "E2E Player Combat HP Test",
            dmUserId: "dm-user-id", // Different from TestUserId so player isn't DM
            party:
            [
                new Character
                {
                    Id = testCharacterId,
                    Name = testCharacterName,
                    Type = "PC",
                    Class = "Fighter",
                    Race = "Dwarf",
                    Level = 3,
                    MaxHp = initialHp,
                    CurrentHp = initialHp,
                    ArmorClass = 18,
                    PlayerId = TestAuthHandler.TestUserId // Character assigned to test player
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
                    Initiative: 12,
                    CurrentHp: initialHp,
                    MaxHp: initialHp,
                    IsDefeated: false,
                    IsSurprised: false
                )
            ]);
        }
        
        // Arrange - Navigate to Player dashboard
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 15000
        });
        
        // Wait for CombatTracker to render with our test character
        var combatTrackerSelector = "[data-testid='combat-tracker']";
        await _playerPage.WaitForSelectorAsync(combatTrackerSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Wait for combatant card to appear
        var combatantSelector = $"[data-testid='combatant-{testCharacterId}']";
        await _playerPage.WaitForSelectorAsync(combatantSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Verify initial HP state in Combat Tracker
        var hpLocator = _playerPage.Locator($"{combatantSelector} [data-testid='hp-current']");
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
        
        // Assert - Wait for SignalR propagation and verify Combat Tracker UI update
        // This is the critical assertion that SHOULD FAIL to detect the bug
        await Expect(hpLocator).ToHaveTextAsync(updatedHp.ToString(), 
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
    }
}
