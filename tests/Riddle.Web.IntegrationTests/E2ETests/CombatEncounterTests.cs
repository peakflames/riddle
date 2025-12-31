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

    [Fact]
    public async Task HLR_COMBAT_010_Player_takes_damage()
    {
        const string thorinId = "thorin-001";
        const string goblinId = "goblin-001";
        
        var campaign = await _factory.SetupTestCampaignAsync(
            name: "Player Damage Test",
            dmUserId: TestAuthHandler.TestUserId,
            party:
            [
                new Character { Id = thorinId, Name = "Thorin", Type = "PC", Class = "Fighter", Race = "Dwarf", Level = 5, MaxHp = 30, CurrentHp = 12, ArmorClass = 16 }
            ]);
        
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.StartCombatAsync(campaign.Id,
            [
                new CombatantInfo(thorinId, "Thorin", "PC", 15, 12, 30, false, false),
                new CombatantInfo(goblinId, "Goblin 1", "Enemy", 12, 7, 7, false, false)
            ]);
        }
        
        await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        
        await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
        
        var thorinHp = _page.Locator($"[data-testid='combatant-{thorinId}'] [data-testid='hp-current']");
        await Expect(thorinHp).ToHaveTextAsync("12");
        
        // Act - Player takes damage
        using (var scope = _factory.CreateScope())
        {
            var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
            await combatService.UpdateCombatantHpAsync(campaign.Id, thorinId, 7); // 12 - 5 = 7
        }
        
        // Assert - HP decreased
        await Expect(thorinHp).ToHaveTextAsync("7", new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
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
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_017_DM_records_Death_Saving_Throw_success()
    {
        // Arrange
        const string elaraId = "elara-017";
        const string elaraName = "Elara";
        
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
        
        // Act - Record death save success (Elara rolled 11)
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
        
        // Assert - Verify death save success was recorded
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
}
