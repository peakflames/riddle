using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;

namespace Riddle.Web.IntegrationTests.E2ETests;

/// <summary>
/// E2E tests for BDD feature: 05_PlayerDashboard.feature
/// 
/// This test class contains one test method per scenario in the feature file.
/// Each test method name includes the scenario ID (e.g., HLR_PLAYER_001) for traceability.
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

    #region @HLR-PLAYER-001: Player sees their character card

    /// <summary>
    /// @HLR-PLAYER-001: Player sees their character card
    /// </summary>
    [Fact]
    public async Task HLR_PLAYER_001_Player_sees_their_character_card()
    {
        const string thorinId = "thorin-card-view";
        const string thorinName = "Thorin";
        const int maxHp = 12;
        const int currentHp = 12;
        const int armorClass = 16;
        const string characterClass = "Fighter";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Tuesday Night Group",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = characterClass, Race = "Dwarf", Level = 1, MaxHp = maxHp, CurrentHp = currentHp, ArmorClass = armorClass, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        var nameLocator = _playerPage.GetByText(thorinName).First;
        await Expect(nameLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        var hpDisplayLocator = _playerPage.GetByText($"{currentHp} / {maxHp}").First;
        await Expect(hpDisplayLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        var acLocator = _playerPage.GetByText(armorClass.ToString()).First;
        await Expect(acLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        var classLocator = _playerPage.GetByText(characterClass).First;
        await Expect(classLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-002: Player sees HP changes in real-time

    /// <summary>
    /// @HLR-PLAYER-002: Player sees HP changes in real-time
    /// </summary>
    [Fact]
    public async Task HLR_PLAYER_002_Player_sees_HP_changes_in_real_time()
    {
        const string thorinId = "thorin-hp-realtime";
        const string thorinName = "Thorin";
        const int initialHp = 12;
        const int maxHp = 12;
        const int expectedHp = 7;
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player HP Realtime Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = maxHp, CurrentHp = initialHp, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        var hpDisplayLocator = _playerPage.GetByText($"{initialHp} / {maxHp}").First;
        await Expect(hpDisplayLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        await Task.Delay(500);
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = $$"""{"character_name": "{{thorinName}}", "key": "current_hp", "value": {{expectedHp}}}""";
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        var updatedHpLocator = _playerPage.GetByText($"{expectedHp} / {maxHp}").First;
        await Expect(updatedHpLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        await Expect(hpDisplayLocator).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 1000 });
    }

    /// <summary>
    /// @HLR-PLAYER-002 Extended: Combat Tracker HP updates
    /// </summary>
    [Fact]
    public async Task HLR_PLAYER_002_Extended_CombatTracker_Shows_HP_Changes_In_Real_Time()
    {
        const string thorinId = "thorin-combat-hp";
        const string thorinName = "Thorin";
        const int initialHp = 25;
        const int maxHp = 30;
        const int updatedHp = 15;
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player Combat HP Realtime Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 3, MaxHp = maxHp, CurrentHp = initialHp, ArmorClass = 18, PlayerId = TestAuthHandler.TestUserId }]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id, [
                new CombatantInfo(thorinId, thorinName, "PC", 12, initialHp, maxHp, false, false),
                new CombatantInfo("goblin-001", "Goblin 1", "Enemy", 10, 7, 7, false, false)
            ]);
        }
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _playerPage.WaitForSelectorAsync("[data-testid='combat-tracker']", new PageWaitForSelectorOptions { Timeout = 10000 });
        var combatantSelector = $"[data-testid='combatant-{thorinId}']";
        await _playerPage.WaitForSelectorAsync(combatantSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var hpLocator = _playerPage.Locator($"{combatantSelector} [data-testid='hp-current']");
        await Expect(hpLocator).ToHaveTextAsync(initialHp.ToString(), new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = $$"""{"character_name": "{{thorinName}}", "key": "current_hp", "value": {{updatedHp}}}""";
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        await Expect(hpLocator).ToHaveTextAsync(updatedHp.ToString(), new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-003: Player sees conditions applied

    /// <summary>@HLR-PLAYER-003: Player sees conditions applied</summary>
    [Fact]
    public async Task HLR_PLAYER_003_Player_sees_conditions_applied()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-003");
    }

    #endregion

    #region @HLR-PLAYER-004: Player sees conditions removed

    /// <summary>@HLR-PLAYER-004: Player sees conditions removed</summary>
    [Fact]
    public async Task HLR_PLAYER_004_Player_sees_conditions_removed()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-004");
    }

    #endregion

    #region @HLR-PLAYER-005: Player receives action choices

    /// <summary>@HLR-PLAYER-005: Player receives action choices</summary>
    [Fact]
    public async Task HLR_PLAYER_005_Player_receives_action_choices()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-005");
    }

    #endregion

    #region @HLR-PLAYER-006: Player selects a choice

    /// <summary>@HLR-PLAYER-006: Player selects a choice</summary>
    [Fact]
    public async Task HLR_PLAYER_006_Player_selects_a_choice()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-006");
    }

    #endregion

    #region @HLR-PLAYER-007: New choices replace old choices

    /// <summary>@HLR-PLAYER-007: New choices replace old choices</summary>
    [Fact]
    public async Task HLR_PLAYER_007_New_choices_replace_old_choices()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-007");
    }

    #endregion

    #region @HLR-PLAYER-008: Player sees atmosphere pulse

    /// <summary>@HLR-PLAYER-008: Player sees atmosphere pulse</summary>
    [Fact]
    public async Task HLR_PLAYER_008_Player_sees_atmosphere_pulse()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-008");
    }

    #endregion

    #region @HLR-PLAYER-009: Player sees narrative anchor

    /// <summary>@HLR-PLAYER-009: Player sees narrative anchor</summary>
    [Fact]
    public async Task HLR_PLAYER_009_Player_sees_narrative_anchor()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-009");
    }

    #endregion

    #region @HLR-PLAYER-010: Player sees group insight

    /// <summary>@HLR-PLAYER-010: Player sees group insight</summary>
    [Fact]
    public async Task HLR_PLAYER_010_Player_sees_group_insight()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-010");
    }

    #endregion

    #region @HLR-PLAYER-011: Player cannot see enemy HP

    /// <summary>@HLR-PLAYER-011: Player cannot see enemy HP</summary>
    [Fact]
    public async Task HLR_PLAYER_011_Player_cannot_see_enemy_HP()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-011");
    }

    #endregion

    #region @HLR-PLAYER-012: Player cannot see DM chat

    /// <summary>@HLR-PLAYER-012: Player cannot see DM chat</summary>
    [Fact]
    public async Task HLR_PLAYER_012_Player_cannot_see_DM_chat()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-012");
    }

    #endregion

    #region @HLR-PLAYER-013: Player sees connection indicator

    /// <summary>@HLR-PLAYER-013: Player sees connection indicator</summary>
    [Fact]
    public async Task HLR_PLAYER_013_Player_sees_connection_indicator()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-013");
    }

    #endregion

    #region @HLR-PLAYER-014: Player reconnects after disconnect

    /// <summary>@HLR-PLAYER-014: Player reconnects after disconnect</summary>
    [Fact]
    public async Task HLR_PLAYER_014_Player_reconnects_after_disconnect()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-014");
    }

    #endregion

    #region @HLR-PLAYER-015: Player with multiple characters selects one

    /// <summary>@HLR-PLAYER-015: Player with multiple characters selects one</summary>
    [Fact]
    public async Task HLR_PLAYER_015_Player_with_multiple_characters_selects_one()
    {
        throw new NotImplementedException("Stub: Implement @HLR-PLAYER-015");
    }

    #endregion
}
