using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;

namespace Riddle.Web.IntegrationTests.E2ETests;

/// <summary>
/// E2E tests for BDD feature: 05_PlayerDashboard.feature
/// 
/// This test class contains test methods for scenarios in the feature file.
/// Focus: Player Dashboard SignalR updates for HP, conditions, and real-time sync.
/// 
/// See tests/Riddle.Specs/Features/05_PlayerDashboard.feature for scenario details.
/// See docs/e2e_testing_philosophy.md for testing patterns.
/// </summary>
[Collection("E2E")]
public class PlayerDashboardTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly PlaywrightFixture _playwrightFixture;
    private IPage _playerPage = null!;
    private IBrowserContext _playerContext = null!;
    
    public PlayerDashboardTests(CustomWebApplicationFactory factory, PlaywrightFixture playwrightFixture)
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

    #region Player sees HP changes in real-time

    /// <summary>
    /// BDD Scenario: Player sees HP changes in real-time
    /// 
    /// Given I am on the Player Dashboard
    /// And "Thorin" has 12 HP
    /// When the DM applies 5 damage to Thorin
    /// Then my HP display should update to "7 / 12"
    /// And I should not need to refresh the page
    /// And the HP bar should visually reflect the damage
    /// 
    /// BUG REPORT (v0.17.0): During gameplay, LLM called update_character_state to update 
    /// a character's HP. DM Dashboard updated correctly BUT Player Dashboard did NOT update:
    /// - Character Card HP bar not updated
    /// - Combat Tracker HP not updated
    /// 
    /// This test MUST FAIL initially to prove it detects the bug.
    /// </summary>
    [Fact]
    public async Task PlayerDashboard_Should_ShowHpChanges_InRealTime_WithoutRefresh()
    {
        // Arrange - Create campaign with character assigned to test player
        const string thorinId = "thorin-hp-realtime";
        const string thorinName = "Thorin";
        const int initialHp = 12;
        const int maxHp = 12;
        const int damageDealt = 5;
        const int expectedHp = 7; // 12 - 5
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player HP Realtime Test",
            dmUserId: "dm-user-id", // Different from TestUserId so player isn't DM
            party:
            [
                new Character
                {
                    Id = thorinId,
                    Name = thorinName,
                    Type = "PC",
                    Class = "Fighter",
                    Race = "Dwarf",
                    Level = 5,
                    MaxHp = maxHp,
                    CurrentHp = initialHp,
                    ArmorClass = 16,
                    PlayerId = TestAuthHandler.TestUserId // Character assigned to test player
                }
            ]);
        
        // Given - Navigate to Player dashboard (not DM dashboard!)
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 15000
        });
        
        // Wait for SignalR to connect - Dashboard shows "Connected" badge when ready
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        // And - Verify "Thorin" has 12 HP displayed as "12 / 12"
        // PlayerCharacterCard displays: "@Character.CurrentHp / @Character.MaxHp"
        var hpDisplayLocator = _playerPage.GetByText($"{initialHp} / {maxHp}").First;
        await Expect(hpDisplayLocator).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        // Give SignalR a moment to fully join the group after connection
        await Task.Delay(500);
        
        // When - DM applies 5 damage to Thorin (via update_character_state tool)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            // This is how the LLM calls the tool to update HP
            var argumentsJson = $$"""
            {
                "character_name": "{{thorinName}}",
                "key": "current_hp",
                "value": {{expectedHp}}
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Then - HP display should update to "7 / 12" WITHOUT page refresh
        // This assertion SHOULD FAIL if SignalR event isn't handled on Player Dashboard
        var updatedHpLocator = _playerPage.GetByText($"{expectedHp} / {maxHp}").First;
        await Expect(updatedHpLocator).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        // And - Original "12 / 12" should no longer be visible
        await Expect(hpDisplayLocator).Not.ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 1000 });
    }

    #endregion

    #region Player sees HP changes in Combat Tracker during combat

    /// <summary>
    /// Extended scenario: Player Dashboard Combat Tracker shows HP changes in real-time
    /// 
    /// Given combat is active
    /// And Player is viewing their Dashboard
    /// When the DM updates a combatant's HP
    /// Then the Combat Tracker should reflect the change
    /// </summary>
    [Fact]
    public async Task PlayerDashboard_CombatTracker_Should_ShowHpChanges_InRealTime()
    {
        // Arrange - Create campaign with character
        const string thorinId = "thorin-combat-hp";
        const string thorinName = "Thorin";
        const int initialHp = 25;
        const int maxHp = 30;
        const int updatedHp = 15;
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player Combat HP Realtime Test",
            dmUserId: "dm-user-id",
            party:
            [
                new Character
                {
                    Id = thorinId,
                    Name = thorinName,
                    Type = "PC",
                    Class = "Fighter",
                    Race = "Dwarf",
                    Level = 3,
                    MaxHp = maxHp,
                    CurrentHp = initialHp,
                    ArmorClass = 18,
                    PlayerId = TestAuthHandler.TestUserId
                }
            ]);
        
        // Start combat so CombatTracker renders
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id, 
            [
                new CombatantInfo(thorinId, thorinName, "PC", 12, initialHp, maxHp, false, false),
                new CombatantInfo("goblin-001", "Goblin 1", "Enemy", 10, 7, 7, false, false)
            ]);
        }
        
        // Given - Navigate to Player dashboard
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 15000
        });
        
        // Wait for CombatTracker to render
        var combatTrackerSelector = "[data-testid='combat-tracker']";
        await _playerPage.WaitForSelectorAsync(combatTrackerSelector, 
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Wait for combatant card
        var combatantSelector = $"[data-testid='combatant-{thorinId}']";
        await _playerPage.WaitForSelectorAsync(combatantSelector, 
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Verify initial HP in Combat Tracker
        var hpLocator = _playerPage.Locator($"{combatantSelector} [data-testid='hp-current']");
        await Expect(hpLocator).ToHaveTextAsync(initialHp.ToString(), 
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
        
        // When - Update HP via tool executor (simulating LLM tool call)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{thorinName}}",
                "key": "current_hp",
                "value": {{updatedHp}}
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Then - Combat Tracker should show updated HP
        // This assertion SHOULD FAIL if SignalR event isn't handled properly
        await Expect(hpLocator).ToHaveTextAsync(updatedHp.ToString(), 
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
    }

    #endregion
}
