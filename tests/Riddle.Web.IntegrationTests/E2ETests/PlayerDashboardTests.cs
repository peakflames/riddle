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

    [Fact]
    public async Task HLR_PLAYER_003_Player_sees_conditions_applied()
    {
        const string thorinId = "thorin-condition-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Condition Apply Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId, Conditions = [] }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        var nameLocator = _playerPage.GetByText(thorinName).First;
        await Expect(nameLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        var poisonedBadge = _playerPage.GetByText("Poisoned");
        await Expect(poisonedBadge).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 2000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = $$"""{"character_name": "{{thorinName}}", "key": "conditions", "value": ["Poisoned"]}""";
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        await Expect(poisonedBadge.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-004: Player sees conditions removed

    [Fact]
    public async Task HLR_PLAYER_004_Player_sees_conditions_removed()
    {
        const string thorinId = "thorin-condition-remove-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Condition Remove Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId, Conditions = ["Poisoned"] }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        var poisonedBadge = _playerPage.GetByText("Poisoned").First;
        await Expect(poisonedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = $$"""{"character_name": "{{thorinName}}", "key": "conditions", "value": []}""";
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        await Expect(poisonedBadge).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-005: Player receives action choices

    [Fact]
    public async Task HLR_PLAYER_005_Player_receives_action_choices()
    {
        const string thorinId = "thorin-choices-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player Choices Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = """{"choices": ["Attack", "Hide", "Negotiate"]}""";
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "present_player_choices", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        var attackButton = _playerPage.GetByText("Attack").First;
        var hideButton = _playerPage.GetByText("Hide").First;
        var negotiateButton = _playerPage.GetByText("Negotiate").First;
        
        await Expect(attackButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        await Expect(hideButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        await Expect(negotiateButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-007: New choices replace old choices

    [Fact]
    public async Task HLR_PLAYER_007_New_choices_replace_old_choices()
    {
        const string thorinId = "thorin-replace-choices-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player Replace Choices Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = """{"choices": ["Old Choice 1", "Old Choice 2"]}""";
            await toolExecutor.ExecuteAsync(campaign.Id, "present_player_choices", argumentsJson);
        }
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        var oldChoice1 = _playerPage.GetByText("Old Choice 1").First;
        await Expect(oldChoice1).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = """{"choices": ["New Choice A", "New Choice B", "New Choice C"]}""";
            await toolExecutor.ExecuteAsync(campaign.Id, "present_player_choices", argumentsJson);
        }
        
        var newChoiceA = _playerPage.GetByText("New Choice A").First;
        await Expect(newChoiceA).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        await Expect(oldChoice1).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 2000 });
    }

    #endregion

    #region @HLR-PLAYER-008: Player sees atmosphere pulse

    [Fact]
    public async Task HLR_PLAYER_008_Player_sees_atmosphere_pulse()
    {
        const string thorinId = "thorin-atmosphere-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Atmosphere Pulse Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = """{"text": "A cold wind howls through the corridor", "intensity": "medium", "sensory_type": "sound"}""";
            await toolExecutor.ExecuteAsync(campaign.Id, "broadcast_atmosphere_pulse", argumentsJson);
        }
        
        var pulseText = _playerPage.GetByText("A cold wind howls").First;
        await Expect(pulseText).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-009: Player sees narrative anchor

    [Fact]
    public async Task HLR_PLAYER_009_Player_sees_narrative_anchor()
    {
        const string thorinId = "thorin-anchor-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Narrative Anchor Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = """{"short_text": "The Cragmaw Hideout", "mood_category": "tension"}""";
            await toolExecutor.ExecuteAsync(campaign.Id, "set_narrative_anchor", argumentsJson);
        }
        
        var anchorText = _playerPage.GetByText("The Cragmaw Hideout").First;
        await Expect(anchorText).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-010: Player sees group insight

    [Fact]
    public async Task HLR_PLAYER_010_Player_sees_group_insight()
    {
        const string thorinId = "thorin-insight-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Group Insight Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            var argumentsJson = """{"text": "The goblin is lying!", "relevant_skill": "Insight", "highlight_effect": true}""";
            await toolExecutor.ExecuteAsync(campaign.Id, "trigger_group_insight", argumentsJson);
        }
        
        var insightText = _playerPage.GetByText("The goblin is lying!").First;
        await Expect(insightText).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-PLAYER-011: Player cannot see enemy HP

    [Fact]
    public async Task HLR_PLAYER_011_Player_cannot_see_enemy_HP()
    {
        const string thorinId = "thorin-enemy-hp-test";
        const string thorinName = "Thorin";
        const string goblinId = "goblin-hp-hidden";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Enemy HP Hidden Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id, [
                new CombatantInfo(thorinId, thorinName, "PC", 12, 30, 30, false, false),
                new CombatantInfo(goblinId, "Goblin Boss", "Enemy", 14, 21, 21, false, false)
            ]);
        }
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _playerPage.WaitForSelectorAsync("[data-testid='combat-tracker']", new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var goblinCard = _playerPage.Locator($"[data-testid='combatant-{goblinId}']");
        await Expect(goblinCard).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        // Enemy HP should be hidden for players - check the specific hp-current element
        var enemyHpCurrent = goblinCard.Locator("[data-testid='hp-current']");
        await Expect(enemyHpCurrent).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 2000 });
    }

    #endregion

    #region @HLR-PLAYER-012: Player cannot see DM chat

    [Fact]
    public async Task HLR_PLAYER_012_Player_cannot_see_DM_chat()
    {
        const string thorinId = "thorin-no-dm-chat";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "No DM Chat Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        
        // DM chat component should not be visible to players
        var dmChat = _playerPage.Locator("[data-testid='dm-chat']");
        await Expect(dmChat).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 2000 });
        
        // Also check for common DM chat indicators
        var chatInput = _playerPage.Locator("[data-testid='chat-input']");
        await Expect(chatInput).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 2000 });
    }

    #endregion

    #region @HLR-PLAYER-013: Player sees connection indicator

    [Fact]
    public async Task HLR_PLAYER_013_Player_sees_connection_indicator()
    {
        const string thorinId = "thorin-connection-test";
        const string thorinName = "Thorin";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Connection Indicator Test",
            dmUserId: "dm-user-id",
            party: [new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16, PlayerId = TestAuthHandler.TestUserId }]);
        
        var url = $"{_factory.ServerAddress}/play/{campaign.Id}";
        await _playerPage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        // The "Connected" badge should be visible when SignalR connection is established
        var connectedBadge = _playerPage.GetByText("Connected").First;
        await Expect(connectedBadge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
    }

    #endregion

    #region @HLR-PLAYER-014: Player reconnects after disconnect (Complex - Skip for now)

    [Fact(Skip = "Complex test requiring SignalR disconnect simulation - implement in later phase")]
    public async Task HLR_PLAYER_014_Player_reconnects_after_disconnect()
    {
        // This test requires simulating network disconnection and reconnection
        // which is complex to implement reliably in E2E tests.
        // The reconnection logic is tested via SignalR infrastructure.
        await Task.CompletedTask;
    }

    #endregion

    #region @HLR-PLAYER-015: Player with multiple characters selects one (Complex - Skip for now)

    [Fact(Skip = "Multi-character selection is a future feature - implement when UI is ready")]
    public async Task HLR_PLAYER_015_Player_with_multiple_characters_selects_one()
    {
        // This test is for a future feature where a player owns multiple characters
        // and must select which one to play as.
        await Task.CompletedTask;
    }

    #endregion
}
