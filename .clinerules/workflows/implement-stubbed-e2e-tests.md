# Implement Stubbed E2E Tests Workflow

<purpose>
This workflow guides the systematic implementation of stubbed E2E integration test methods. Each stub contains a `throw new NotImplementedException()` and XML documentation describing the scenario's Given/When/Then steps.

**Use this workflow when you have stubbed test files that need implementation.**
</purpose>

---

## Step 0: Select Test File

<select_file>
Before beginning implementation, prompt the user to select the target test file:

```
<ask_followup_question>
<question>Which E2E test file should I implement?</question>
<options>
["CombatEncounterTests.cs", "CampaignManagementTests.cs", "DungeonMasterChatTests.cs", "ReadAloudNarrationTests.cs", "PlayerDashboardTests.cs", "StateRecoveryTests.cs", "GameStateDashboardTests.cs", "PartyManagementTests.cs"]
</options>
</ask_followup_question>
```

Once the user selects a test file, proceed to Prerequisites.
</select_file>

---

## Prerequisites

<prerequisites>
Before starting implementation:

### Step 1: Read Testing Philosophy
```
read_file docs/e2e_testing_philosophy.md
```
This document contains:
- Test structure patterns (Arrange/Act/Assert)
- Infrastructure usage (`CustomWebApplicationFactory`, `PlaywrightFixture`)
- Hard-won lessons (Kestrel config, auth providers, EF Core services)

### Step 2: Read Reference Implementation
```
read_file tests/Riddle.Web.IntegrationTests/E2ETests/UpdateCharacterStateToolTests.cs
```
This is a fully-implemented test showing canonical patterns.

### Step 3: Read Target Test File
```
read_file tests/Riddle.Web.IntegrationTests/E2ETests/{SelectedFile}
```

### Step 4: Identify Stubbed Tests
Scan the file for methods containing:
```csharp
throw new NotImplementedException("Stub: Implement @HLR-XXX-NNN");
```

### Step 5: Verify Test Infrastructure Compiles
```bash
dotnet build tests/Riddle.Web.IntegrationTests
```
</prerequisites>

---

## Core Principles

<core_principles>
### 1. Test Tools, Not Transport

E2E tests verify that **when an action occurs, the expected UI changes appear**. We test:
```
User Action / Tool Call → Service Layer → SignalR → Blazor UI → DOM
```

### 2. Use Polling Assertions for Async Updates

SignalR updates are asynchronous. Never use immediate assertions:

```csharp
// ❌ WRONG - Races with async SignalR
var text = await _page.Locator("[data-testid='hp']").TextContentAsync();
text.Should().Be("20");

// ✅ CORRECT - Polls until condition met or timeout
await Expect(_page.Locator("[data-testid='hp']"))
    .ToHaveTextAsync("20", new() { Timeout = 5000 });
```

### 3. Follow the Stub Comments

Each stub contains inline comments mapping to BDD steps:
```csharp
// Arrange - {Given steps}
// Act - {When steps}  
// Assert - {Then steps}
```

Implement code under each section matching its description.

### 4. Use `data-testid` Selectors

Prefer semantic test IDs over CSS classes or element structure:
```csharp
// ✅ CORRECT - Resilient to styling changes
var hpLocator = _page.Locator("[data-testid='hp-current']");

// ❌ AVOID - Brittle selectors
var hpLocator = _page.Locator(".combatant-card span.hp-value");
```

### 5. Get Services via Factory Scope

```csharp
using var scope = _factory.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
```
</core_principles>

---

## Implementation Phases

<implementation_phases>
### Phase 1: Analyze Stub

For each stubbed test method:

1. **Read XML Documentation** - Contains Given/When/Then from BDD scenario
2. **Read Verification Notes** - Contains implementation hints (which services, events, etc.)
3. **Identify Required Services** - What DI services are needed (ToolExecutor, CombatService, etc.)
4. **Identify UI Elements** - What `data-testid` elements need verification

### Phase 2: Implement Arrange Section

The Arrange section sets up preconditions:

```csharp
// 1. Create test data via factory
var campaign = await _factory.SetupTestCampaignAsync(
    name: "Test Campaign",
    dmUserId: TestAuthHandler.TestUserId,
    party: [new Character { ... }]);

// 2. Set up additional state (e.g., start combat)
using (var scope = _factory.CreateScope())
{
    var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
    await combatService.StartCombatAsync(campaign.Id, combatants);
}

// 3. Navigate browser to target page
await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

// 4. Wait for UI elements and verify initial state
await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
await Expect(hpLocator).ToHaveTextAsync("30");
```

### Phase 3: Implement Act Section

The Act section performs the action under test:

```csharp
// Option A: Execute tool directly (for tool-triggered scenarios)
using (var scope = _factory.CreateScope())
{
    var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
    var result = await toolExecutor.ExecuteAsync(campaign.Id, "tool_name", argsJson);
    result.Should().Contain("success");
}

// Option B: Use service method directly (for service-triggered scenarios)
using (var scope = _factory.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<ICombatService>();
    await service.AdvanceTurnAsync(campaign.Id);
}

// Option C: Simulate UI interaction (for UI-triggered scenarios)
await _page.ClickAsync("[data-testid='advance-turn-button']");
```

### Phase 4: Implement Assert Section

The Assert section verifies expected outcomes:

```csharp
// UI state verification with polling (handles async SignalR)
await Expect(_page.Locator("[data-testid='hp-current']"))
    .ToHaveTextAsync("20", new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });

// Multiple element verification
await Expect(_page.Locator("[data-testid='round-number']")).ToHaveTextAsync("2");
await Expect(_page.Locator("[data-testid='current-turn']")).ToHaveTextAsync("Elara");

// Visibility verification
await Expect(_page.Locator("[data-testid='combat-tracker']")).ToBeVisibleAsync();
await Expect(_page.Locator("[data-testid='defeated-badge']")).ToBeVisibleAsync();

// Element count verification
await Expect(_page.Locator("[data-testid^='combatant-']")).ToHaveCountAsync(4);
```

### Phase 5: Validate Implementation

After implementing each test:

1. **Build** - `dotnet build tests/Riddle.Web.IntegrationTests`
2. **Run Single Test** - `dotnet test --filter "FullyQualifiedName~HLR_XXX_NNN"`
3. **Debug Failures** - Use Playwright traces/screenshots if needed
</implementation_phases>

---

## Common Patterns

<common_patterns>
### Pattern: Setup Campaign with Party

```csharp
var campaign = await _factory.SetupTestCampaignAsync(
    name: "Test Campaign",
    dmUserId: TestAuthHandler.TestUserId,
    party:
    [
        new Character
        {
            Id = "hero-001",
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
            Id = "hero-002", 
            Name = "Elara",
            Type = "PC",
            Class = "Rogue",
            Race = "Elf",
            Level = 5,
            MaxHp = 22,
            CurrentHp = 22,
            ArmorClass = 14
        }
    ]);
```

### Pattern: Start Combat with Combatants

```csharp
using (var scope = _factory.CreateScope())
{
    var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
    await combatService.StartCombatAsync(campaign.Id,
    [
        new CombatantInfo("hero-001", "Thorin", "PC", 15, 30, 30, false, false),
        new CombatantInfo("hero-002", "Elara", "PC", 18, 22, 22, false, false),
        new CombatantInfo("goblin-001", "Goblin 1", "Enemy", 12, 7, 7, false, false),
        new CombatantInfo("goblin-002", "Goblin 2", "Enemy", 10, 7, 7, false, false)
    ]);
}
```

### Pattern: Execute LLM Tool

```csharp
using (var scope = _factory.CreateScope())
{
    var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
    
    var argsJson = """
    {
        "character_name": "Thorin",
        "key": "current_hp",
        "value": 20
    }
    """;
    
    var result = await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argsJson);
    result.Should().Contain("success", "Tool should execute successfully");
}
```

### Pattern: Wait for SignalR Update

```csharp
// The key insight: use Playwright's Expect() which polls automatically
var hpLocator = _page.Locator($"[data-testid='combatant-{characterId}'] [data-testid='hp-current']");

// Initial state
await Expect(hpLocator).ToHaveTextAsync("30");

// After tool execution
await Expect(hpLocator).ToHaveTextAsync("20", 
    new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
```

### Pattern: Navigate with NetworkIdle

```csharp
await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
    new PageGotoOptions 
    { 
        WaitUntil = WaitUntilState.NetworkIdle,
        Timeout = 15000 
    });

// Then wait for specific element before interacting
await _page.WaitForSelectorAsync("[data-testid='combat-tracker']", 
    new PageWaitForSelectorOptions { Timeout = 10000 });
```

### Pattern: Multiple Browser Contexts (Multi-Client Tests)

```csharp
// For tests requiring DM + Player clients
var dmContext = await _playwrightFixture.Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
var dmPage = await dmContext.NewPageAsync();

var playerContext = await _playwrightFixture.Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
var playerPage = await playerContext.NewPageAsync();

// Navigate both
await dmPage.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}");
await playerPage.GotoAsync($"{_factory.ServerAddress}/player/{campaign.Id}");

// Act on one, assert on both
using (var scope = _factory.CreateScope()) { /* tool execution */ }

await Expect(dmPage.Locator("[data-testid='hp']")).ToHaveTextAsync("20");
await Expect(playerPage.Locator("[data-testid='hp']")).ToHaveTextAsync("20");

// Cleanup
await dmPage.CloseAsync();
await dmContext.DisposeAsync();
await playerPage.CloseAsync();
await playerContext.DisposeAsync();
```
</common_patterns>

---

## Required data-testid Elements

<data_testid_reference>
Ensure components expose these test IDs. If missing, add them to the Razor components.

### CombatTracker Component
```razor
[data-testid="combat-tracker"]           // Root container
[data-testid="combatant-{id}"]           // Individual combatant card
[data-testid="hp-current"]               // Current HP display
[data-testid="hp-max"]                   // Max HP display  
[data-testid="current-turn-indicator"]   // Turn highlight
[data-testid="round-number"]             // Round badge
[data-testid="initiative"]               // Initiative value
[data-testid="defeated-badge"]           // Defeated indicator
[data-testid="surprised-badge"]          // Surprised indicator
[data-testid="advance-turn-button"]      // Turn advance button
```

### Character/Party Display
```razor
[data-testid="character-{id}"]           // Character card
[data-testid="character-name"]           // Character name
[data-testid="condition-{condition}"]    // Condition badges
```

### Chat Component
```razor
[data-testid="chat-messages"]            // Message container
[data-testid="chat-input"]               // Input field
[data-testid="chat-send"]                // Send button
```

If a required `data-testid` is missing from the codebase, **add it to the component** before implementing the test.
</data_testid_reference>

---

## Verification Checklist

<verification_checklist>
Copy this checklist when implementing tests:

```markdown
# {Test Class} Implementation Checklist

**Test File:** `tests/Riddle.Web.IntegrationTests/E2ETests/{TestClass}.cs`
**Date:** {date}
**Status:** ⬜ Not Started | 🟡 In Progress | ✅ Complete

## Prerequisites
- [ ] Read testing philosophy
- [ ] Read reference implementation  
- [ ] Identify all stubbed methods

## Implementation Progress
| Scenario ID | Test Method | Status |
|------------|-------------|--------|
| @HLR-XXX-001 | `HLR_XXX_001_...` | ⬜ Stub → 🟡 In Progress → ✅ Passing |
| @HLR-XXX-002 | `HLR_XXX_002_...` | ⬜ Stub |

## For Each Test
- [ ] Implement Arrange (setup data, navigate, verify initial state)
- [ ] Implement Act (execute tool/service/UI action)
- [ ] Implement Assert (verify UI changes with polling)
- [ ] Remove `throw new NotImplementedException()`
- [ ] Build passes: `dotnet build tests/Riddle.Web.IntegrationTests`
- [ ] Test passes: `dotnet test --filter "FullyQualifiedName~HLR_XXX_NNN"`

## Final Validation
- [ ] All tests compile
- [ ] All tests pass individually
- [ ] Full test class passes: `dotnet test --filter "FullyQualifiedName~{TestClass}"`
```
</verification_checklist>

---

## Example: Full Implementation

<example_implementation>
### Before (Stub)

```csharp
[Fact]
public async Task HLR_COMBAT_005_DM_advances_to_next_turn()
{
    // Arrange - Create campaign with active combat
    //           Set current turn to Elara (index 0)
    //           Navigate to DM dashboard
    
    // Act - Execute advance_turn tool
    
    // Assert - Current turn advanced to next combatant
    //          UI highlight moved to next combatant
    //          TurnAdvanced SignalR event received
    
    throw new NotImplementedException("Stub: Implement @HLR-COMBAT-005");
}
```

### After (Implemented)

```csharp
[Fact]
public async Task HLR_COMBAT_005_DM_advances_to_next_turn()
{
    // Arrange - Create campaign with party
    const string thorinId = "thorin-001";
    const string elaraId = "elara-001";
    
    var campaign = await _factory.SetupTestCampaignAsync(
        name: "Turn Advance Test",
        dmUserId: TestAuthHandler.TestUserId,
        party:
        [
            new Character { Id = thorinId, Name = "Thorin", Type = "PC", MaxHp = 30, CurrentHp = 30, ArmorClass = 16 },
            new Character { Id = elaraId, Name = "Elara", Type = "PC", MaxHp = 22, CurrentHp = 22, ArmorClass = 14 }
        ]);
    
    // Arrange - Start combat with Elara first in initiative order
    using (var scope = _factory.CreateScope())
    {
        var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
        await combatService.StartCombatAsync(campaign.Id,
        [
            new CombatantInfo(elaraId, "Elara", "PC", 18, 22, 22, false, false),
            new CombatantInfo(thorinId, "Thorin", "PC", 15, 30, 30, false, false)
        ]);
    }
    
    // Arrange - Navigate to DM dashboard
    await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
        new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    
    await _page.WaitForSelectorAsync("[data-testid='combat-tracker']");
    
    // Verify initial state: Elara's turn (index 0)
    var elaraCard = _page.Locator($"[data-testid='combatant-{elaraId}']");
    await Expect(elaraCard.Locator("[data-testid='current-turn-indicator']")).ToBeVisibleAsync();
    
    // Act - Execute advance_turn tool
    using (var scope = _factory.CreateScope())
    {
        var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
        await combatService.AdvanceTurnAsync(campaign.Id);
    }
    
    // Assert - Turn advanced to Thorin
    var thorinCard = _page.Locator($"[data-testid='combatant-{thorinId}']");
    await Expect(thorinCard.Locator("[data-testid='current-turn-indicator']"))
        .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    
    // Assert - Elara no longer highlighted
    await Expect(elaraCard.Locator("[data-testid='current-turn-indicator']"))
        .Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
}
```
</example_implementation>

---

## Quick Reference

<quick_reference>
### Test File Location
```
tests/Riddle.Web.IntegrationTests/E2ETests/{FeatureName}Tests.cs
```

### Key Imports
```csharp
using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;
```

### Factory Methods
```csharp
_factory.SetupTestCampaignAsync(...)  // Create test campaign
_factory.CreateScope()                  // Get DI scope for services
_factory.ServerAddress                  // Base URL for navigation
```

### Playwright Assertion Methods
```csharp
await Expect(locator).ToHaveTextAsync("value", new() { Timeout = 5000 });
await Expect(locator).ToBeVisibleAsync();
await Expect(locator).Not.ToBeVisibleAsync();
await Expect(locator).ToHaveCountAsync(4);
await Expect(locator).ToHaveAttributeAsync("class", "active");
```

### Execution Commands
```bash
# Build tests
dotnet build tests/Riddle.Web.IntegrationTests

# Run all E2E tests
dotnet test tests/Riddle.Web.IntegrationTests

# Run specific test class
dotnet test tests/Riddle.Web.IntegrationTests --filter "FullyQualifiedName~CombatEncounterTests"

# Run specific scenario test
dotnet test tests/Riddle.Web.IntegrationTests --filter "FullyQualifiedName~HLR_COMBAT_005"

# Run with verbose output
dotnet test tests/Riddle.Web.IntegrationTests --filter "FullyQualifiedName~HLR_COMBAT_005" -v n
```

### Debug Tips
```csharp
// Take screenshot on failure
await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "debug.png" });

// Log page content
var html = await _page.ContentAsync();
Console.WriteLine(html);

// Slow down for visual debugging
await _page.WaitForTimeoutAsync(2000);
```
</quick_reference>
