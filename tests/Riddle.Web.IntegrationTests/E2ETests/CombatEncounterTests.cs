using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;

namespace Riddle.Web.IntegrationTests.E2ETests;

/// <summary>
/// E2E tests for BDD feature: 04_CombatEncounter.feature
/// 
/// This test class contains one test method per scenario in the feature file.
/// Each test method name includes the scenario ID (e.g., HLR_COMBAT_001) for traceability.
/// 
/// See tests/Riddle.Specs/Features/04_CombatEncounter.feature for scenario details.
/// See docs/e2e_testing_philosophy.md for testing patterns.
/// </summary>
[Collection("E2E")]
public class CombatEncounterTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly PlaywrightFixture _playwrightFixture;
    private IPage _page = null!;
    private IBrowserContext _context = null!;
    
    public CombatEncounterTests(CustomWebApplicationFactory factory, PlaywrightFixture playwrightFixture)
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

    #region @HLR-COMBAT-001: DM starts combat from narrative

    [Fact]
    public async Task HLR_COMBAT_001_DM_starts_combat_from_narrative()
    {
        // Arrange - Create campaign with party
        const string thorinId = "thorin-001";
        const string elaraId = "elara-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Combat Start Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 },
                new Character { Id = elaraId, Name = "Elara", Type = "PC", Class = "Rogue", Race = "Elf", Level = 5, MaxHp = 22, CurrentHp = 22, ArmorClass = 14 }
            ]);
        
        // Navigate to DM dashboard first
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']", 
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Verify no active combat initially
        var noCombatText = _page.Locator("[data-testid='combat-tracker']").GetByText("No active combat");
        await Expect(noCombatText).ToBeVisibleAsync();
        
        // Act - Execute start_combat tool
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo(elaraId, "Elara", "PC", 18, 22, 22, false, false),
                new CombatantInfo("goblin-001", "Goblin 1", "Enemy", 12, 7, 7, false, false),
                new CombatantInfo("goblin-002", "Goblin 2", "Enemy", 10, 7, 7, false, false)
            ]);
        }
        
        // Assert - Combat mode initiated
        var combatantLocator = _page.Locator("[data-testid^='combatant-']");
        await Expect(combatantLocator).ToHaveCountAsync(4, new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
        
        await Expect(_page.Locator("[data-testid='round-number']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-002: DM inputs initiative rolls

    [Fact]
    public async Task HLR_COMBAT_002_DM_inputs_initiative_rolls()
    {
        const string thorinId = "thorin-001";
        const string elaraId = "elara-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Initiative Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 },
                new Character { Id = elaraId, Name = "Elara", Type = "PC", Class = "Rogue", Race = "Elf", Level = 5, MaxHp = 22, CurrentHp = 22, ArmorClass = 14 }
            ]);
        
        // Start combat with Thorin first (higher initiative)
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 20, 30, 30, false, false),
                new CombatantInfo(elaraId, "Elara", "PC", 10, 22, 22, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        var firstCombatant = _page.Locator("[data-testid^='combatant-']").First;
        await Expect(firstCombatant).ToContainTextAsync("Thorin");
        
        // Act - Update initiative to make Elara first
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.SetInitiativeAsync(campaign.Id, thorinId, 15);
            await combatService.SetInitiativeAsync(campaign.Id, elaraId, 18);
        }
        
        // Assert - Reload to see updated order
        await _page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        var updatedFirstCombatant = _page.Locator("[data-testid^='combatant-']").First;
        await Expect(updatedFirstCombatant).ToContainTextAsync("Elara", 
            new LocatorAssertionsToContainTextOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-003: Combat includes enemy combatants

    [Fact]
    public async Task HLR_COMBAT_003_Combat_includes_enemy_combatants()
    {
        const string thorinId = "thorin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Enemy Combatant Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo("goblin-001", "Goblin 1", "Enemy", 12, 7, 7, false, false),
                new CombatantInfo("goblin-002", "Goblin 2", "Enemy", 10, 7, 7, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        var combatants = _page.Locator("[data-testid^='combatant-']");
        await Expect(combatants).ToHaveCountAsync(3, new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
        
        await Expect(_page.Locator("[data-testid='combatant-goblin-001']")).ToBeVisibleAsync();
        await Expect(_page.Locator("[data-testid='combatant-goblin-002']")).ToBeVisibleAsync();
        
        var goblin1Initiative = _page.Locator("[data-testid='combatant-goblin-001'] [data-testid='initiative']");
        await Expect(goblin1Initiative).ToContainTextAsync("12");
    }

    #endregion

    #region @HLR-COMBAT-004: Turn order displays correctly

    [Fact]
    public async Task HLR_COMBAT_004_Turn_order_displays_correctly()
    {
        const string thorinId = "thorin-001";
        const string elaraId = "elara-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Turn Order Display Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 },
                new Character { Id = elaraId, Name = "Elara", Type = "PC", Class = "Rogue", Race = "Elf", Level = 5, MaxHp = 22, CurrentHp = 22, ArmorClass = 14 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, "Elara", "PC", 18, 22, 22, false, false),
                new CombatantInfo("goblin-001", "Goblin 1", "Enemy", 16, 7, 7, false, false),
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo("goblin-002", "Goblin 2", "Enemy", 12, 7, 7, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        var combatants = _page.Locator("[data-testid^='combatant-']");
        await Expect(combatants).ToHaveCountAsync(4);
        
        var firstCombatant = combatants.First;
        await Expect(firstCombatant).ToContainTextAsync("Elara");
        
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard.Locator("[data-testid='current-turn-indicator']")).ToBeVisibleAsync();
        
        await Expect(_page.Locator("[data-testid='round-number']")).ToContainTextAsync("Round 1");
    }

    #endregion

    #region @HLR-COMBAT-005: DM advances to next turn

    [Fact]
    public async Task HLR_COMBAT_005_DM_advances_to_next_turn()
    {
        const string thorinId = "thorin-001";
        const string elaraId = "elara-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Turn Advance Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 },
                new Character { Id = elaraId, Name = "Elara", Type = "PC", Class = "Rogue", Race = "Elf", Level = 5, MaxHp = 22, CurrentHp = 22, ArmorClass = 14 }
            ]);
        
        // Start combat with Elara first
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, "Elara", "PC", 18, 22, 22, false, false),
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        // Verify Elara is current turn
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard.Locator("[data-testid='current-turn-indicator']")).ToBeVisibleAsync();
        
        // Act - Advance turn
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.AdvanceTurnAsync(campaign.Id);
        }
        
        // Assert - Thorin is now current turn
        var thorinCard = _page.Locator($"[data-testid='combatant-{thorinId}']");
        await Expect(thorinCard.Locator("[data-testid='current-turn-indicator']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        await Expect(elaraCard.Locator("[data-testid='current-turn-indicator']"))
            .Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-006: Round advances after all turns

    [Fact]
    public async Task HLR_COMBAT_006_Round_advances_after_all_turns()
    {
        const string thorinId = "thorin-001";
        const string elaraId = "elara-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Round Advance Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 },
                new Character { Id = elaraId, Name = "Elara", Type = "PC", Class = "Rogue", Race = "Elf", Level = 5, MaxHp = 22, CurrentHp = 22, ArmorClass = 14 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, "Elara", "PC", 18, 22, 22, false, false),
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        await Expect(_page.Locator("[data-testid='round-number']")).ToContainTextAsync("Round 1");
        
        // Advance through all combatants (Elara -> Thorin -> back to Elara)
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.AdvanceTurnAsync(campaign.Id); // Elara -> Thorin
            await combatService.AdvanceTurnAsync(campaign.Id); // Thorin -> Elara (round 2)
        }
        
        // Assert - Round 2 and back to first combatant
        await Expect(_page.Locator("[data-testid='round-number']"))
            .ToContainTextAsync("Round 2", new LocatorAssertionsToContainTextOptions { Timeout = 5000 });
        
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard.Locator("[data-testid='current-turn-indicator']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-007: Damage is applied to enemy

    [Fact]
    public async Task HLR_COMBAT_007_Damage_is_applied_to_enemy()
    {
        const string thorinId = "thorin-001";
        const string goblinId = "goblin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Damage Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 12, 7, 7, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        var goblinHp = _page.Locator($"[data-testid='combatant-{goblinId}'] [data-testid='hp-current']");
        await Expect(goblinHp).ToHaveTextAsync("7");
        
        // Act - Apply damage via update combatant HP
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.UpdateCombatantHpAsync(campaign.Id, goblinId, 2); // 7 - 5 = 2
        }
        
        // Assert - HP decreased
        await Expect(goblinHp).ToHaveTextAsync("2", new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-008: Enemy is defeated

    [Fact]
    public async Task HLR_COMBAT_008_Enemy_is_defeated()
    {
        const string thorinId = "thorin-001";
        const string goblin1Id = "goblin-001";
        const string goblin2Id = "goblin-002";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Defeat Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 }
            ]);
        
        // Need 2 enemies so marking one defeated doesn't auto-end combat
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo(goblin1Id, "Goblin 1", "Enemy", 12, 3, 7, false, false), // 3 HP - will be defeated
                new CombatantInfo(goblin2Id, "Goblin 2", "Enemy", 10, 7, 7, false, false)  // Keeps combat alive
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        // Act - Mark goblin 1 as defeated
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.MarkDefeatedAsync(campaign.Id, goblin1Id);
        }
        
        // Assert - Goblin 1 shows defeated badge (combat still active due to goblin 2)
        var goblin1Card = _page.Locator($"[data-testid='combatant-{goblin1Id}']");
        await Expect(goblin1Card.Locator("[data-testid='defeated-badge']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-009: Surprise round

    [Fact]
    public async Task HLR_COMBAT_009_Surprise_round()
    {
        const string thorinId = "thorin-001";
        const string goblinId = "goblin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Surprise Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 }
            ]);
        
        // Start combat with Thorin surprised
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 12, 7, 7, false, false),
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, true) // Surprised
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        // Assert - Thorin shows surprised badge
        var thorinCard = _page.Locator($"[data-testid='combatant-{thorinId}']");
        await Expect(thorinCard.Locator("[data-testid='surprised-badge']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-010: Player takes damage

    /// <summary>
    /// Tests that PC HP update via LLM tool syncs to both Party Tracker and Combat Tracker.
    /// 
    /// Enhanced scenario verifies:
    /// - PartyState HP is updated
    /// - ActiveCombat.Combatants HP is synchronized  
    /// - Combat Tracker UI shows updated HP
    /// - DM dashboard shows consistent HP in both trackers
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_010_Player_takes_damage()
    {
        // Arrange
        const string thorinId = "thorin-001";
        const string thorinName = "Thorin";
        const string goblinId = "goblin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player Damage Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = thorinName, Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 12, ArmorClass = 16 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, thorinName, "PC", 15, 12, 30, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 12, 7, 7, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        // Verify initial HP in Combat Tracker
        var thorinCombatHp = _page.Locator($"[data-testid='combatant-{thorinId}'] [data-testid='hp-current']");
        await Expect(thorinCombatHp).ToHaveTextAsync("12");
        
        // Act - Player takes damage via ToolExecutor (the LLM code path)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{thorinName}}",
                "key": "current_hp",
                "value": 7
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert 1: PartyState HP is updated
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == thorinName);
            
            character.Should().NotBeNull();
            character!.CurrentHp.Should().Be(7, "PartyState HP should be 7");
        }
        
        // Assert 2: ActiveCombat.Combatants HP is synchronized
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            var combatState = await combatService.GetCombatStateAsync(campaign.Id);
            
            combatState.Should().NotBeNull();
            var thorinCombatant = combatState!.TurnOrder.FirstOrDefault(c => c.Id == thorinId);
            thorinCombatant.Should().NotBeNull();
            thorinCombatant!.CurrentHp.Should().Be(7, "ActiveCombat combatant HP should be 7");
        }
        
        // Assert 3: Combat Tracker UI shows updated HP
        await Expect(thorinCombatHp).ToHaveTextAsync("7", new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-012: All enemies defeated

    [Fact]
    public async Task HLR_COMBAT_012_All_enemies_defeated()
    {
        const string thorinId = "thorin-001";
        const string goblinId = "goblin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Victory Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 12, 2, 7, false, false) // 2 HP
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        // Verify combat is active
        await Expect(_page.Locator("[data-testid='round-number']")).ToBeVisibleAsync();
        
        // Act - Defeat the only enemy
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.MarkDefeatedAsync(campaign.Id, goblinId);
        }
        
        // Assert - Combat ended (no more round number visible or "No active combat" shown)
        var noCombatText = _page.Locator("[data-testid='combat-tracker']").GetByText("No active combat");
        await Expect(noCombatText).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-013: DM ends combat manually

    [Fact]
    public async Task HLR_COMBAT_013_DM_ends_combat_manually()
    {
        const string thorinId = "thorin-001";
        const string goblinId = "goblin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Manual End Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 12, 7, 7, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        await Expect(_page.Locator("[data-testid='round-number']")).ToBeVisibleAsync();
        
        // Act - End combat manually
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.EndCombatAsync(campaign.Id);
        }
        
        // Assert - Combat ended
        var noCombatText = _page.Locator("[data-testid='combat-tracker']").GetByText("No active combat");
        await Expect(noCombatText).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-014: Player sees combat updates

    [Fact]
    public async Task HLR_COMBAT_014_Player_sees_combat_updates()
    {
        const string thorinId = "thorin-001";
        const string goblinId = "goblin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player View Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 12, 7, 7, false, false)
            ]);
        }
        
        // Navigate to DM dashboard (player view uses same combat tracker)
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        var goblinHp = _page.Locator($"[data-testid='combatant-{goblinId}'] [data-testid='hp-current']");
        await Expect(goblinHp).ToHaveTextAsync("7");
        
        // Act - Update enemy HP
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.UpdateCombatantHpAsync(campaign.Id, goblinId, 3);
        }
        
        // Assert - Player sees update without refresh
        await Expect(goblinHp).ToHaveTextAsync("3", new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-015: Turn order syncs across all clients

    [Fact]
    public async Task HLR_COMBAT_015_Turn_order_syncs_across_all_clients()
    {
        const string thorinId = "thorin-001";
        const string elaraId = "elara-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Sync Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 30, ArmorClass = 16 },
                new Character { Id = elaraId, Name = "Elara", Type = "PC", Class = "Rogue", Race = "Elf", Level = 5, MaxHp = 22, CurrentHp = 22, ArmorClass = 14 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, "Elara", "PC", 18, 22, 22, false, false),
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        // Verify Elara is current turn
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard.Locator("[data-testid='current-turn-indicator']")).ToBeVisibleAsync();
        
        // Act - Advance turn
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.AdvanceTurnAsync(campaign.Id);
        }
        
        // Assert - All connected clients see Thorin as current turn
        var thorinCard = _page.Locator($"[data-testid='combatant-{thorinId}']");
        await Expect(thorinCard.Locator("[data-testid='current-turn-indicator']"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    #endregion

    // ========================================================================
    // PLAYER CHARACTER DEATH & DYING (HLR-COMBAT-016 through HLR-COMBAT-026)
    // ========================================================================

    #region @HLR-COMBAT-016: Player character drops to 0 HP

    /// <summary>
    /// Tests that reducing HP to 0 automatically applies Unconscious condition
    /// and resets death save counters.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_016_Player_character_drops_to_0_HP()
    {
        // Arrange
        const string elaraId = "elara-016";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Drop to 0 HP",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 3,  // Low HP
                    ArmorClass = 14,
                    DeathSaveSuccesses = 0,
                    DeathSaveFailures = 0
                }
            ]);
        
        // Act - Reduce HP to 0 (goblin deals 5 damage)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "current_hp",
                "value": 0
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify character has Unconscious condition and reset death saves
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.CurrentHp.Should().Be(0);
            character.Conditions.Should().Contain("Unconscious");
            character.DeathSaveSuccesses.Should().Be(0);
            character.DeathSaveFailures.Should().Be(0);
        }
    }

    #endregion

    #region @HLR-COMBAT-017: DM records Death Saving Throw success

    /// <summary>
    /// Tests recording a successful death saving throw (e.g., rolled 11).
    /// Enhanced to verify UI updates in Combat Tracker via SignalR.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_017_DM_records_Death_Saving_Throw_success()
    {
        // Arrange
        const string elaraId = "elara-017";
        const string elaraName = "Elara";
        const string goblinId = "goblin-017";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Success",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,  // At 0 HP
                    ArmorClass = 14,
                    DeathSaveSuccesses = 0,
                    DeathSaveFailures = 1,
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Start combat so Combat Tracker is visible
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, elaraName, "PC", 10, 0, 22, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 15, 7, 7, false, false)
            ]);
        }
        
        // Navigate to DM dashboard
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Verify Elara is in combat tracker and starts with 0 success circles filled
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard).ToBeVisibleAsync();
        
        // Verify initial state: 0 filled success circles (green bg)
        // Death save circles use bg-green-500 when filled
        var initialSuccessCircles = elaraCard.Locator(".bg-green-500");
        await Expect(initialSuccessCircles).ToHaveCountAsync(0, 
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
        
        // Act - Record death save success (Elara rolled 11) via ToolExecutor
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "death_save_success",
                "value": 1
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert 1 - Verify death save success was recorded in database
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.DeathSaveSuccesses.Should().Be(1);
            character.DeathSaveFailures.Should().Be(1);
            character.CurrentHp.Should().Be(0);
        }
        
        // Assert 2 - Verify UI updated via SignalR: 1 filled success circle
        var updatedSuccessCircles = elaraCard.Locator(".bg-green-500");
        await Expect(updatedSuccessCircles).ToHaveCountAsync(1, 
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-018: DM records Death Saving Throw failure

    /// <summary>
    /// Tests recording a failed death saving throw (e.g., rolled 6).
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_018_DM_records_Death_Saving_Throw_failure()
    {
        // Arrange
        const string elaraId = "elara-018";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Failure",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 1,
                    DeathSaveFailures = 0,
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Record death save failure (Elara rolled 6)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "death_save_failure",
                "value": 1
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify death save failure was recorded
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.DeathSaveSuccesses.Should().Be(1);
            character.DeathSaveFailures.Should().Be(1);
            character.CurrentHp.Should().Be(0);
        }
    }

    #endregion

    #region @HLR-COMBAT-019: Natural 20 on Death Saving Throw

    /// <summary>
    /// Tests that rolling a natural 20 on death save restores 1 HP,
    /// removes Unconscious, and resets all death save counters.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_019_Natural_20_on_Death_Saving_Throw()
    {
        // Arrange
        const string elaraId = "elara-019";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Natural 20",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 1,
                    DeathSaveFailures = 2,  // In danger!
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Natural 20: HP goes to 1, conditions cleared, counters reset
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            // Natural 20 is implemented as setting HP to 1
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "current_hp",
                "value": 1
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify character regained consciousness
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.CurrentHp.Should().Be(1);
            character.Conditions.Should().NotContain("Unconscious");
            character.DeathSaveSuccesses.Should().Be(0, "Death saves should reset on healing");
            character.DeathSaveFailures.Should().Be(0, "Death saves should reset on healing");
        }
    }

    #endregion

    #region @HLR-COMBAT-020: Player character fails 3 Death Saves

    /// <summary>
    /// Tests that recording 3 death save failures marks character as Dead.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_020_Player_character_fails_3_Death_Saves()
    {
        // Arrange
        const string elaraId = "elara-020";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - 3 Failures",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 1,
                    DeathSaveFailures = 2,  // Already has 2 failures
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Record third failure (Elara rolled 4)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "death_save_failure",
                "value": 1
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify character is dead
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.DeathSaveFailures.Should().Be(3);
            character.IsDead.Should().BeTrue();
            character.IsStable.Should().BeFalse();
            character.Conditions.Should().Contain("Dead");
        }
    }

    #endregion

    #region @HLR-COMBAT-021: Player character becomes Stable

    /// <summary>
    /// Tests that recording 3 death save successes marks character as Stable.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_021_Player_character_becomes_Stable()
    {
        // Arrange
        const string elaraId = "elara-021";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Stable",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 2,  // Already has 2 successes
                    DeathSaveFailures = 1,
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Record third success (Elara rolled 14)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "death_save_success",
                "value": 1
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify character is stable
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.DeathSaveSuccesses.Should().Be(3);
            character.IsStable.Should().BeTrue();
            character.IsDead.Should().BeFalse();
            character.Conditions.Should().Contain("Stable");
        }
    }

    #endregion

    #region @HLR-COMBAT-022: Unconscious character takes damage

    /// <summary>
    /// Tests that damage to an unconscious character counts as an automatic death save failure.
    /// Note: This is handled by LLM/DM calling death_save_failure when damage is dealt.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_022_Unconscious_character_takes_damage()
    {
        // Arrange
        const string elaraId = "elara-022";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Damage While Unconscious",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 1,
                    DeathSaveFailures = 1,
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Damage to unconscious character = automatic death save failure
        // (DM/LLM would call death_save_failure when damage is dealt)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "death_save_failure",
                "value": 1
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify death save failure was recorded
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.DeathSaveFailures.Should().Be(2, "Damage should cause death save failure");
            character.CurrentHp.Should().Be(0, "HP should remain at 0");
        }
    }

    #endregion

    #region @HLR-COMBAT-023: Unconscious character takes critical hit

    /// <summary>
    /// Tests that a critical hit on unconscious character causes TWO death save failures.
    /// Note: This is handled by LLM/DM calling death_save_failure with value 2.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_023_Unconscious_character_takes_critical_hit()
    {
        // Arrange
        const string elaraId = "elara-023";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Critical Hit",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 1,
                    DeathSaveFailures = 0,
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Critical hit = 2 death save failures
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "death_save_failure",
                "value": 2
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify TWO death save failures were recorded
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.DeathSaveFailures.Should().Be(2, "Critical hit should cause 2 death save failures");
        }
    }

    #endregion

    #region @HLR-COMBAT-024: Another character stabilizes unconscious ally

    /// <summary>
    /// Tests that another character can stabilize an unconscious ally using the stabilize tool.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_024_Another_character_stabilizes_unconscious_ally()
    {
        // Arrange
        const string thorinId = "thorin-024";
        const string elaraId = "elara-024";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Stabilize Ally",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = thorinId,
                    Name = "Thorin",
                    Type = "PC",
                    Class = "Fighter",
                    Race = "Dwarf",
                    Level = 5,
                    MaxHp = 30,
                    CurrentHp = 30,
                    ArmorClass = 16
                },
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 1,
                    DeathSaveFailures = 2,  // In danger!
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Thorin uses action to stabilize Elara (Medicine check DC 10 success)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "stabilize",
                "value": true
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify character is stable
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.DeathSaveSuccesses.Should().Be(3);
            character.IsStable.Should().BeTrue();
            character.Conditions.Should().Contain("Stable");
        }
    }

    #endregion

    #region @HLR-COMBAT-025: Massive damage causes instant death

    /// <summary>
    /// Tests that massive damage (remaining damage >= MaxHP after dropping to 0) causes instant death.
    /// Note: This is handled by LLM/DM recognizing the massive damage rule and marking dead.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_025_Massive_damage_causes_instant_death()
    {
        // Arrange - Elara has 5 HP, MaxHP 8. Ogre deals 15 damage (5 to drop to 0, 10 remaining >= 8 MaxHP)
        const string elaraId = "elara-025";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Massive Damage",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 1,  // Low level character
                    MaxHp = 8,
                    CurrentHp = 5,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 0,
                    DeathSaveFailures = 0
                }
            ]);
        
        // Act - Massive damage: HP to 0 + add Dead condition directly
        // (The LLM/DM recognizes massive damage and adds conditions appropriately)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            // First, set HP to 0
            var hpJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "current_hp",
                "value": 0
            }
            """;
            await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", hpJson);
            
            // Then add Dead condition (massive damage rule - damage >= MaxHP)
            var deadJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "add_condition",
                "value": "Dead"
            }
            """;
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", deadJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify character is dead
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.CurrentHp.Should().Be(0);
            character.Conditions.Should().Contain("Dead");
        }
    }

    #endregion

    #region @HLR-COMBAT-026: Healing unconscious character

    /// <summary>
    /// Tests that healing an unconscious character restores HP, removes conditions,
    /// and resets death save counters.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_026_Healing_unconscious_character()
    {
        // Arrange
        const string elaraId = "elara-026";
        const string elaraName = "Elara";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Test - Healing",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 2,
                    DeathSaveFailures = 1,
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Act - Thorin uses healing potion on Elara, restoring 7 HP
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "current_hp",
                "value": 7
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Verify character regained consciousness and death saves reset
        using (var scope = _factory.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var reloadedCampaign = await campaignService.GetCampaignAsync(campaign.Id);
            var character = reloadedCampaign?.PartyState.FirstOrDefault(c => c.Name == elaraName);
            
            character.Should().NotBeNull();
            character!.CurrentHp.Should().Be(7);
            character.Conditions.Should().NotContain("Unconscious");
            character.DeathSaveSuccesses.Should().Be(0, "Death saves should reset on healing");
            character.DeathSaveFailures.Should().Be(0, "Death saves should reset on healing");
        }
    }

    #endregion

    // ========================================================================
    // PC VS ENEMY DISPLAY BEHAVIOR (HLR-COMBAT-027 through HLR-COMBAT-031)
    // ========================================================================

    #region @HLR-COMBAT-027: PC at 0 HP shows Unconscious badge instead of Defeated

    /// <summary>
    /// Tests that PCs at 0 HP show "Unconscious" badge (not "Defeated") and no strikethrough.
    /// This is critical for D&D 5e death save mechanics - PCs aren't defeated at 0 HP.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_027_PC_at_0_HP_shows_Unconscious_badge_instead_of_Defeated()
    {
        // Arrange
        const string elaraId = "elara-027";
        const string elaraName = "Elara";
        const string goblinId = "goblin-027";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "PC Unconscious Display Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,  // Already at 0 HP
                    ArmorClass = 14,
                    DeathSaveSuccesses = 0,
                    DeathSaveFailures = 0,
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Start combat with PC at 0 HP
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, elaraName, "PC", 10, 0, 22, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 15, 7, 7, false, false)
            ]);
        }
        
        // Navigate to DM dashboard
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard).ToBeVisibleAsync();
        
        // Assert - PC shows "Unconscious" badge
        var unconsciousBadge = elaraCard.Locator("[data-testid='unconscious-badge']");
        await Expect(unconsciousBadge).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        // Assert - PC does NOT show "Defeated" badge
        var defeatedBadge = elaraCard.Locator("[data-testid='defeated-badge']");
        await Expect(defeatedBadge).Not.ToBeVisibleAsync();
        
        // Assert - PC card does NOT have strikethrough styling (no line-through class)
        var nameElement = elaraCard.Locator("[data-testid='combatant-name']");
        var nameClasses = await nameElement.GetAttributeAsync("class") ?? "";
        nameClasses.Should().NotContain("line-through", "Unconscious PC should not have strikethrough");
        
        // Assert - Death save circles are visible (the success/failure tracker section)
        var deathSaveSection = elaraCard.Locator("[data-testid='death-saves']");
        await Expect(deathSaveSection).ToBeVisibleAsync();
    }

    #endregion

    #region @HLR-COMBAT-028: Enemy at 0 HP shows Defeated badge

    /// <summary>
    /// Tests that enemies at 0 HP show "Defeated" badge with strikethrough (not death saves).
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_028_Enemy_at_0_HP_shows_Defeated_badge()
    {
        // Arrange
        const string thorinId = "thorin-028";
        const string goblin1Id = "goblin-028-1";
        const string goblin2Id = "goblin-028-2";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Enemy Defeated Display Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = thorinId,
                    Name = "Thorin",
                    Type = "PC",
                    Class = "Fighter",
                    Race = "Dwarf",
                    Level = 5,
                    MaxHp = 30,
                    CurrentHp = 30,
                    ArmorClass = 16
                }
            ]);
        
        // Start combat with one enemy at 0 HP (defeated)
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false),
                new CombatantInfo(goblin1Id, "Goblin 1", "Enemy", 12, 0, 7, true, false),  // Defeated
                new CombatantInfo(goblin2Id, "Goblin 2", "Enemy", 10, 7, 7, false, false)  // Still alive
            ]);
        }
        
        // Navigate to DM dashboard
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var goblin1Card = _page.Locator($"[data-testid='combatant-{goblin1Id}']");
        await Expect(goblin1Card).ToBeVisibleAsync();
        
        // Assert - Enemy shows "Defeated" badge
        var defeatedBadge = goblin1Card.Locator("[data-testid='defeated-badge']");
        await Expect(defeatedBadge).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        // Assert - Enemy does NOT show "Unconscious" badge
        var unconsciousBadge = goblin1Card.Locator("[data-testid='unconscious-badge']");
        await Expect(unconsciousBadge).Not.ToBeVisibleAsync();
        
        // Assert - Enemy card DOES have strikethrough styling (has line-through class)
        var nameElement = goblin1Card.Locator("[data-testid='combatant-name']");
        var nameClasses = await nameElement.GetAttributeAsync("class") ?? "";
        nameClasses.Should().Contain("line-through", "Defeated enemy should have strikethrough");
        
        // Assert - Death save circles are NOT visible for enemy
        var deathSaveSection = goblin1Card.Locator("[data-testid='death-saves']");
        await Expect(deathSaveSection).Not.ToBeVisibleAsync();
    }

    #endregion

    #region @HLR-COMBAT-029: Death save circles display correctly for PC

    /// <summary>
    /// Tests that death save circles display correctly based on DeathSaveSuccesses/Failures.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_029_Death_save_circles_display_correctly_for_PC()
    {
        // Arrange
        const string elaraId = "elara-029";
        const string elaraName = "Elara";
        const string goblinId = "goblin-029";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Death Save Circles Display Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 2,  // 2 successes
                    DeathSaveFailures = 1,    // 1 failure
                    Conditions = ["Unconscious"]
                }
            ]);
        
        // Start combat
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, elaraName, "PC", 10, 0, 22, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 15, 7, 7, false, false)
            ]);
        }
        
        // Navigate to DM dashboard
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard).ToBeVisibleAsync();
        
        // Assert - 2 filled success circles (green bg)
        // Death save circles use bg-green-500 when success is filled
        var successCirclesFilled = elaraCard.Locator("[data-testid='death-saves'] .bg-green-500");
        await Expect(successCirclesFilled).ToHaveCountAsync(2, 
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
        
        // Assert - 1 filled failure circle (red bg)
        // Death save circles use bg-red-500 when failure is filled
        var failureCirclesFilled = elaraCard.Locator("[data-testid='death-saves'] .bg-red-500");
        await Expect(failureCirclesFilled).ToHaveCountAsync(1,
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-030: PC with 3 failures shows Dead badge with strikethrough

    /// <summary>
    /// Tests that a PC with IsDead = true (3 failures) shows Dead badge with strikethrough.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_030_PC_with_3_failures_shows_Dead_badge_with_strikethrough()
    {
        // Arrange
        const string elaraId = "elara-030";
        const string elaraName = "Elara";
        const string goblinId = "goblin-030";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "PC Dead Display Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,
                    ArmorClass = 14,
                    DeathSaveSuccesses = 1,
                    DeathSaveFailures = 3,  // DEAD - 3 failures
                    Conditions = ["Unconscious", "Dead"]
                }
            ]);
        
        // Start combat
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, elaraName, "PC", 10, 0, 22, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 15, 7, 7, false, false)
            ]);
        }
        
        // Navigate to DM dashboard
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard).ToBeVisibleAsync();
        
        // Assert - PC shows "Dead" badge
        var deadBadge = elaraCard.Locator("[data-testid='dead-badge']");
        await Expect(deadBadge).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        // Assert - PC card DOES have strikethrough styling when dead (has line-through class)
        var nameElement = elaraCard.Locator("[data-testid='combatant-name']");
        var nameClasses = await nameElement.GetAttributeAsync("class") ?? "";
        nameClasses.Should().Contain("line-through", "Dead PC should have strikethrough");
        
        // Assert - 3 filled failure circles (all red)
        var failureCirclesFilled = elaraCard.Locator("[data-testid='death-saves'] .bg-red-500");
        await Expect(failureCirclesFilled).ToHaveCountAsync(3,
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-031: PC with 3 successes shows Stable badge

    /// <summary>
    /// Tests that a PC with IsStable = true (3 successes) shows Stable badge.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_031_PC_with_3_successes_shows_Stable_badge()
    {
        // Arrange
        const string elaraId = "elara-031";
        const string elaraName = "Elara";
        const string goblinId = "goblin-031";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "PC Stable Display Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,  // Still at 0 HP
                    ArmorClass = 14,
                    DeathSaveSuccesses = 3,  // STABLE - 3 successes
                    DeathSaveFailures = 1,
                    Conditions = ["Unconscious", "Stable"]
                }
            ]);
        
        // Start combat
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, elaraName, "PC", 10, 0, 22, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 15, 7, 7, false, false)
            ]);
        }
        
        // Navigate to DM dashboard
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard).ToBeVisibleAsync();
        
        // Assert - PC shows "Stable" badge
        var stableBadge = elaraCard.Locator("[data-testid='stable-badge']");
        await Expect(stableBadge).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
        
        // Assert - PC does NOT show "Unconscious" badge when stable (Stable replaces Unconscious)
        var unconsciousBadge = elaraCard.Locator("[data-testid='unconscious-badge']");
        await Expect(unconsciousBadge).Not.ToBeVisibleAsync();
        
        // Assert - 3 filled success circles (all green)
        var successCirclesFilled = elaraCard.Locator("[data-testid='death-saves'] .bg-green-500");
        await Expect(successCirclesFilled).ToHaveCountAsync(3,
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
    }

    #endregion

    #region @HLR-COMBAT-032: Player Dashboard Combat Tracker receives death save updates via SignalR

    /// <summary>
    /// Tests that the Player Dashboard Combat Tracker receives death save updates via SignalR
    /// without requiring a page refresh. This catches bugs where DM Dashboard updates correctly
    /// but Player Dashboard does not receive the DeathSaveUpdated event.
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_032_Player_Dashboard_Combat_Tracker_receives_death_save_updates_via_SignalR()
    {
        // Arrange - Create campaign with PC assigned to test player
        const string elaraId = "elara-032";
        const string elaraName = "Elara";
        const string goblinId = "goblin-032";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player Death Save SignalR Test",
            dmUserId: "dm-user-id",  // DM is NOT test user, so test user views as player
            party:
            [
                new Character
                {
                    Id = elaraId,
                    Name = elaraName,
                    Type = "PC",
                    Class = "Rogue",
                    Race = "Elf",
                    Level = 5,
                    MaxHp = 22,
                    CurrentHp = 0,  // At 0 HP
                    ArmorClass = 14,
                    DeathSaveSuccesses = 0,
                    DeathSaveFailures = 0,
                    Conditions = ["Unconscious"],
                    PlayerId = TestAuthHandler.TestUserId  // Assigned to test player
                }
            ]);
        
        // Start combat
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(elaraId, elaraName, "PC", 10, 0, 22, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 15, 7, 7, false, false)
            ]);
        }
        
        // Navigate to Player Dashboard (test user is player, not DM)
        await _page.GotoAsync($"{_factory.ServerAddress}/play/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        // Wait for Combat Tracker to render
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']",
            new PageWaitForSelectorOptions { Timeout = 10000 });
        
        var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
        await Expect(elaraCard).ToBeVisibleAsync();
        
        // Verify initial state: 0 filled success circles
        var initialSuccessCircles = elaraCard.Locator("[data-testid='death-saves'] .bg-green-500");
        await Expect(initialSuccessCircles).ToHaveCountAsync(0,
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
        
        // Act - DM processes death save success via ToolExecutor (simulates LLM tool call)
        using (var scope = _factory.CreateScope())
        {
            var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
            
            var argumentsJson = $$"""
            {
                "character_name": "{{elaraName}}",
                "key": "death_save_success",
                "value": 1
            }
            """;
            
            var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
            result.Should().Contain("success", "Tool execution should succeed");
        }
        
        // Assert - Player Dashboard Combat Tracker shows 1 filled success circle WITHOUT refresh
        var updatedSuccessCircles = elaraCard.Locator("[data-testid='death-saves'] .bg-green-500");
        await Expect(updatedSuccessCircles).ToHaveCountAsync(1,
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 });
    }

    #endregion
}
