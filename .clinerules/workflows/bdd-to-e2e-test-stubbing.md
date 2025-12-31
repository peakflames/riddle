# BDD Feature to E2E Test Stubbing Workflow

<purpose>
This workflow provides a repeatable, feature-agnostic process for analyzing BDD feature files and stubbing out corresponding E2E integration tests. Each BDD scenario gets a dedicated test method with the scenario ID in the method name.

**Use this workflow for any feature file in `tests/Riddle.Specs/Features/`.**
</purpose>

---

## Step 0: Select Feature File

<select_feature>
Before beginning analysis, prompt the user to select the target feature file:

```
<ask_followup_question>
<question>Which BDD feature file should I analyze and stub E2E tests for?</question>
<options>
["01_CampaignManagement.feature", "02_DungeonMasterChat.feature", "03_ReadAloudNarration.feature", "04_CombatEncounter.feature", "05_PlayerDashboard.feature", "06_StateRecovery.feature", "07_GameStateDashboard.feature", "08_PartyManagement.feature"]
</options>
</ask_followup_question>
```

Once the user selects a feature file, proceed to Prerequisites.
</select_feature>

---

## Prerequisites

<prerequisites>
Before starting test stubbing:

### Step 1: Verify Test Project Compiles
```bash
dotnet build tests/Riddle.Web.IntegrationTests
```

### Step 2: Review Testing Philosophy
```
read_file docs/e2e_testing_philosophy.md
```

### Step 3: Review Existing Test Patterns
```
read_file tests/Riddle.Web.IntegrationTests/E2ETests/UpdateCharacterStateToolTests.cs
```
</prerequisites>

---

## Core Principle

<core_principle>
**Scenario-Centric Testing:** Each BDD scenario maps to exactly one test method. The test method name includes the scenario ID for traceability.

```
BDD Scenario (@HLR-XXX-NNN) → Test Method (HLR_XXX_NNN_Scenario_Title)
```

### Why This Approach

1. **Direct Traceability:** Scenario ID in method name creates clear link to requirements
2. **No Hidden Coverage:** Every scenario has explicit test - nothing "implicitly covered"
3. **BDD Alignment:** Given/When/Then becomes Arrange/Act/Assert in comments
4. **Easy Navigation:** Search by scenario ID finds both spec and test
</core_principle>

---

## Feature File Reference

<feature_reference>
| Feature File | Domain | Test Class Name |
|-------------|--------|-----------------|
| `01_CampaignManagement.feature` | Campaign CRUD | `CampaignManagementTests` |
| `02_DungeonMasterChat.feature` | LLM Chat | `DungeonMasterChatTests` |
| `03_ReadAloudNarration.feature` | Text-to-Speech | `ReadAloudNarrationTests` |
| `04_CombatEncounter.feature` | Combat | `CombatEncounterTests` |
| `05_PlayerDashboard.feature` | Player UI | `PlayerDashboardTests` |
| `06_StateRecovery.feature` | Persistence | `StateRecoveryTests` |
| `07_GameStateDashboard.feature` | DM State View | `GameStateDashboardTests` |
| `08_PartyManagement.feature` | Party CRUD | `PartyManagementTests` |
</feature_reference>

---

## Phase 1: Feature Analysis

<feature_analysis>
### Step 1: Read the Feature File

```
read_file tests/Riddle.Specs/Features/{NN}_{FeatureName}.feature
```

Extract the following for each scenario:
- **Scenario tag** - `@HLR-XXX-NNN` requirement ID
- **Scenario title** - Human-readable description
- **Given steps** - Preconditions (becomes Arrange)
- **When steps** - Actions (becomes Act)
- **Then steps** - Expected outcomes (becomes Assert)

### Step 2: Create Scenario Inventory Table

```markdown
| Scenario ID | Scenario Title | Test Method Name |
|------------|----------------|------------------|
| @HLR-XXX-001 | User does something | `HLR_XXX_001_User_does_something` |
| @HLR-XXX-002 | Another action | `HLR_XXX_002_Another_action` |
```

### Step 3: Determine Test Class Name

Test class name derives from feature file name:
- `04_CombatEncounter.feature` → `CombatEncounterTests`
- Remove number prefix and underscore
- Remove `.feature` extension
- Add `Tests` suffix
</feature_analysis>

---

## Phase 2: Test Class Generation

<test_class_generation>
### Step 1: Create Test File

**File path:** `tests/Riddle.Web.IntegrationTests/E2ETests/{FeatureName}Tests.cs`

Example: `tests/Riddle.Web.IntegrationTests/E2ETests/CombatEncounterTests.cs`

### Step 2: Add Class Boilerplate

Every test class must include this exact boilerplate:

```csharp
using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;

namespace Riddle.Web.IntegrationTests.E2ETests;

/// <summary>
/// E2E tests for BDD feature: {NN}_{FeatureName}.feature
/// 
/// This test class contains one test method per scenario in the feature file.
/// Each test method name includes the scenario ID (e.g., HLR_XXX_001) for traceability.
/// 
/// See tests/Riddle.Specs/Features/{NN}_{FeatureName}.feature for scenario details.
/// See docs/e2e_testing_philosophy.md for testing patterns.
/// </summary>
[Collection("E2E")]
public class {FeatureName}Tests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly PlaywrightFixture _playwrightFixture;
    private IPage _page = null!;
    private IBrowserContext _context = null!;
    
    public {FeatureName}Tests(CustomWebApplicationFactory factory, PlaywrightFixture playwrightFixture)
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

    // Test methods for each scenario go below...
}
```

### Step 3: Add Test Method Stubs

For each scenario in the feature file, add a test method stub:

```csharp
#region @HLR-XXX-001: {Scenario Title}

/// <summary>
/// @HLR-XXX-001: {Scenario Title}
/// 
/// Given: {Given step 1}
///        {Given step 2 if any}
/// When:  {When step}
/// Then:  {Then step 1}
///        {Then step 2 if any}
/// </summary>
[Fact]
public async Task HLR_XXX_001_{Scenario_Title_With_Underscores}()
{
    // Arrange - {Given steps as comments}
    
    // Act - {When steps as comments}
    
    // Assert - {Then steps as comments}
    
    throw new NotImplementedException("Stub: Implement @HLR-XXX-001");
}

#endregion
```

### Step 4: Method Naming Convention

Convert scenario ID and title to valid C# method name:
- Replace hyphens with underscores: `HLR-XXX-001` → `HLR_XXX_001`
- Replace spaces with underscores in title
- Remove special characters
- Example: `@HLR-COMBAT-001: DM initiates combat from narrative context`
  → `HLR_COMBAT_001_DM_initiates_combat_from_narrative_context`

### Step 5: Verify Compilation

```bash
dotnet build tests/Riddle.Web.IntegrationTests
```

All stubs should compile (they throw `NotImplementedException` which is valid).
</test_class_generation>

---

## Phase 3: Complete Example

<complete_example>
### Input Feature File: `04_CombatEncounter.feature`

```gherkin
@phase2
Feature: Combat Encounter Management
  As a Dungeon Master
  I want the AI to manage combat encounters
  So that battles flow smoothly with accurate tracking

Background:
  Given an active campaign session
  And combat has been initiated

@HLR-COMBAT-001
Scenario: DM initiates combat from narrative context
  Given a campaign with at least two party members
  When the DM describes a hostile encounter
  Then the Combat Tracker appears
  And all combatants are listed with their initiative order

@HLR-COMBAT-002
Scenario: Initiative rolls are recorded
  Given combat has been initiated
  When initiative is rolled for all combatants
  Then turn order reflects initiative ranking
```

### Output Test Class: `CombatEncounterTests.cs`

```csharp
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

    #region @HLR-COMBAT-001: DM initiates combat from narrative context

    /// <summary>
    /// @HLR-COMBAT-001: DM initiates combat from narrative context
    /// 
    /// Given: a campaign with at least two party members
    /// When:  the DM describes a hostile encounter
    /// Then:  the Combat Tracker appears
    ///        and all combatants are listed with their initiative order
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_001_DM_initiates_combat_from_narrative_context()
    {
        // Arrange - a campaign with at least two party members
        
        // Act - the DM describes a hostile encounter
        
        // Assert - the Combat Tracker appears
        //          and all combatants are listed with their initiative order
        
        throw new NotImplementedException("Stub: Implement @HLR-COMBAT-001");
    }

    #endregion

    #region @HLR-COMBAT-002: Initiative rolls are recorded

    /// <summary>
    /// @HLR-COMBAT-002: Initiative rolls are recorded
    /// 
    /// Given: combat has been initiated
    /// When:  initiative is rolled for all combatants
    /// Then:  turn order reflects initiative ranking
    /// </summary>
    [Fact]
    public async Task HLR_COMBAT_002_Initiative_rolls_are_recorded()
    {
        // Arrange - combat has been initiated
        
        // Act - initiative is rolled for all combatants
        
        // Assert - turn order reflects initiative ranking
        
        throw new NotImplementedException("Stub: Implement @HLR-COMBAT-002");
    }

    #endregion
}
```
</complete_example>

---

## Phase 4: Cross-Reference Documentation

<cross_reference>
After generating the test class, add bidirectional cross-references for traceability.

### Step 1: Update Feature File Header

Add a comment header at the top of the feature file pointing to the E2E test class:

```gherkin
# ==============================================================================
# E2E Test Coverage: tests/Riddle.Web.IntegrationTests/E2ETests/{FeatureName}Tests.cs
# 
# Each @HLR-{DOMAIN}-XXX scenario has a corresponding test method:
#   @HLR-{DOMAIN}-001 → HLR_{DOMAIN}_001_{Scenario_Title}()
#   @HLR-{DOMAIN}-002 → HLR_{DOMAIN}_002_{Scenario_Title}()
#   ... (pattern: replace hyphens with underscores)
# 
# See docs/e2e_testing_philosophy.md for testing patterns.
# ==============================================================================

@phase2 @phase3 ...
Feature: {Feature Name}
```

### Step 2: Verify Test Class Header

Ensure the test class XML doc already references the feature file (part of boilerplate):

```csharp
/// <summary>
/// E2E tests for BDD feature: {NN}_{FeatureName}.feature
/// ...
/// See tests/Riddle.Specs/Features/{NN}_{FeatureName}.feature for scenario details.
/// </summary>
```

### Why Cross-References Matter

1. **Breadcrumb Navigation:** Developers can jump between spec and implementation
2. **Coverage Visibility:** Feature file header shows tests exist without opening test file
3. **Maintenance Aid:** When modifying scenarios, the link reminds to update tests
4. **Onboarding:** New developers see the relationship immediately
</cross_reference>

---

## Phase 5: Output Deliverables

<output_deliverables>
When stubbing tests for a feature file, produce these deliverables:

### Deliverable 1: Scenario Inventory

```markdown
# {Feature Name} E2E Test Stubs

## Feature: {NN}_{FeatureName}.feature
## Test Class: {FeatureName}Tests.cs

### Scenarios

| Scenario ID | Scenario Title | Test Method |
|------------|----------------|-------------|
| @HLR-XXX-001 | {Title} | `HLR_XXX_001_{Title}` |
| @HLR-XXX-002 | {Title} | `HLR_XXX_002_{Title}` |
```

### Deliverable 2: Test File

The actual `.cs` test file with:
- Full class boilerplate (usings, namespace, class declaration, fixtures)
- One stubbed test method per scenario
- `throw new NotImplementedException()` in each method body

### Deliverable 3: Compilation Verification

```bash
dotnet build tests/Riddle.Web.IntegrationTests
```

Confirm all stubs compile successfully.
</output_deliverables>

---

## Verification Checklist

<verification_checklist>
Copy this checklist when starting test stubbing for a new feature:

```markdown
# {Feature Name} E2E Test Stubbing Checklist

**Feature File:** `tests/Riddle.Specs/Features/{NN}_{FeatureName}.feature`
**Test File:** `tests/Riddle.Web.IntegrationTests/E2ETests/{FeatureName}Tests.cs`
**Date:** {date}
**Status:** ⬜ Not Started | 🟡 In Progress | ✅ Complete

## Phase 1: Feature Analysis
- [ ] Read feature file
- [ ] Extract all scenario IDs and titles
- [ ] Create scenario inventory table

## Phase 2: Test Class Generation
- [ ] Create test file with correct name
- [ ] Add class boilerplate (usings, namespace, fixtures)
- [ ] Add test method stub for each scenario
- [ ] Verify method names include scenario IDs

## Phase 3: Validation
- [ ] Test project compiles: `dotnet build tests/Riddle.Web.IntegrationTests`
- [ ] All scenarios have corresponding test methods
- [ ] All test methods have scenario ID in name

## Phase 4: Cross-Reference Documentation
- [ ] Feature file has E2E test header comment added
- [ ] Test class XML doc references feature file

## Scenario Coverage
| Scenario ID | Test Method | Status |
|------------|-------------|--------|
| @HLR-XXX-001 | `HLR_XXX_001_...` | ⬜ Stubbed |
| @HLR-XXX-002 | `HLR_XXX_002_...` | ⬜ Stubbed |
```
</verification_checklist>

---

## Quick Reference

<quick_reference>
### Test File Location
```
tests/Riddle.Web.IntegrationTests/E2ETests/{FeatureName}Tests.cs
```

### Required Usings
```csharp
using Microsoft.Playwright;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Services;
using static Microsoft.Playwright.Assertions;
```

### Class Attributes
```csharp
[Collection("E2E")]
public class {FeatureName}Tests : IAsyncLifetime
```

### Test Method Naming Convention
```
HLR_{DOMAIN}_{NNN}_{Scenario_Title_With_Underscores}
```

### Stub Body
```csharp
throw new NotImplementedException("Stub: Implement @HLR-XXX-NNN");
```

### Execution Commands
```bash
# Build tests
dotnet build tests/Riddle.Web.IntegrationTests

# Run all E2E tests
dotnet test tests/Riddle.Web.IntegrationTests

# Run specific test class
dotnet test tests/Riddle.Web.IntegrationTests --filter "FullyQualifiedName~{FeatureName}Tests"

# Run specific scenario test
dotnet test tests/Riddle.Web.IntegrationTests --filter "FullyQualifiedName~HLR_COMBAT_001"
```
</quick_reference>
