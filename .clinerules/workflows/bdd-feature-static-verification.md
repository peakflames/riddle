# BDD Feature Verification Workflow

<purpose>
This workflow provides a systematic process for verifying that the software implements the behavior specified in BDD Cucumber-formatted feature files. The LLM reads feature files, analyzes each scenario against the codebase through static code analysis, documents verification thinking, and generates a timestamped report.
</purpose>

---

## Feature Selection Protocol

<feature_selection>
### When Feature File is Specified

If the user provides a feature file path or name:

1. **Validate the file exists**
   ```bash
   ls tests/Riddle.Specs/Features/{specified-file}.feature
   ```

2. **Read the feature file**
   ```
   read_file tests/Riddle.Specs/Features/{specified-file}.feature
   ```

3. **Proceed to Feature Parsing**

### When No Feature File is Specified

If the user does not specify a feature file:

1. **List available feature files**
   ```
   list_files tests/Riddle.Specs/Features
   ```

2. **Present options to user**
   ```markdown
   ## Available Feature Files for Verification
   
   | # | Feature File | Description |
   |---|--------------|-------------|
   | 1 | 01_CampaignManagement.feature | Campaign instance lifecycle |
   | 2 | 02_DungeonMasterChat.feature | DM chat interactions |
   | 3 | 03_ReadAloudNarration.feature | Narrative generation |
   | 4 | 04_CombatEncounter.feature | Combat system |
   | 5 | 05_PlayerDashboard.feature | Player UI |
   | 6 | 06_StateRecovery.feature | State persistence |
   | 7 | 07_GameStateDashboard.feature | Game state display |
   | 8 | 08_PartyManagement.feature | Party management |
   
   Which feature file would you like me to verify?
   ```

3. **Wait for user selection before proceeding**
</feature_selection>

---

## Feature Parsing Protocol

<feature_parsing>
### Step 1: Read and Parse Feature File

1. **Read the complete feature file**
   ```
   read_file tests/Riddle.Specs/Features/{feature-file}.feature
   ```

2. **Extract Feature Metadata**
   - Feature name (from `Feature:` line)
   - Feature description (lines after Feature until Background/Scenario)
   - Tags (lines starting with `@`)
   - Background steps (if present)

3. **Extract All Scenarios**
   For each `Scenario:` or `Scenario Outline:` block, capture:
   - Scenario name
   - Scenario tags (if any)
   - Given steps
   - When steps
   - Then steps
   - Examples table (for Scenario Outline)

### Step 2: Document Parsed Structure

Present the parsed structure before verification:

```markdown
## Feature: {Feature Name}
**File**: {file path}
**Tags**: {tags}

### Background
{background steps if any}

### Scenarios to Verify ({count})
1. {Scenario 1 name}
2. {Scenario 2 name}
...
```
</feature_parsing>

---

## Verification Process

<verification_process>
For **each scenario** in the feature file, perform the following verification steps.

### Step 1: Understand the Scenario

Document your understanding in `<verification_thinking>` tags:

```xml
<verification_thinking>
**Scenario**: {Scenario Name}

**Given**: {Given steps - preconditions}
**When**: {When steps - actions}
**Then**: {Then steps - expected outcomes}

**Key behaviors to verify**:
1. {behavior 1}
2. {behavior 2}
...

**Code areas to investigate**:
- {Service/Component 1}
- {Service/Component 2}
...
</verification_thinking>
```

### Step 2: Trace Code Paths

Use available tools to investigate the codebase:

1. **Search for relevant implementations**
   ```
   search_files src "keyword from scenario"
   ```

2. **Read relevant source files**
   ```
   read_file src/Riddle.Web/Services/{RelevantService}.cs
   read_file src/Riddle.Web/Components/{RelevantComponent}.razor
   ```

3. **Understand class/function structure**
   ```
   list_code_definition_names src/Riddle.Web/Services
   ```

### Step 3: Document Verification Analysis

Continue `<verification_thinking>` with your analysis:

```xml
<verification_thinking>
**Code Path Analysis**:

**Given Steps Implementation**:
- Given "{step}": 
  - Found in: {file path}
  - Implementation: {description}
  - Status: ✅ Implemented / ❌ Not Found / ⚠️ Partial

**When Steps Implementation**:
- When "{step}":
  - Found in: {file path}
  - Implementation: {description}
  - Status: ✅ Implemented / ❌ Not Found / ⚠️ Partial

**Then Steps Implementation**:
- Then "{step}":
  - Found in: {file path}
  - Implementation: {description}
  - Status: ✅ Implemented / ❌ Not Found / ⚠️ Partial

**Code References**:
- `{file1:line}` - {what it does}
- `{file2:line}` - {what it does}

**Gaps Identified**:
- {gap 1}
- {gap 2}

**Verdict**: PASS / FAIL / SKIP
**Reason**: {brief explanation}
</verification_thinking>
```

### Step 4: Determine Scenario Status

| Status | Criteria |
|--------|----------|
| ✅ **PASS** | All Given/When/Then steps have corresponding code paths that could satisfy the behavior |
| ❌ **FAIL** | One or more steps have no implementation or incomplete implementation |
| ⚠️ **SKIP** | Scenario cannot be verified (e.g., requires external service, future feature, blocked) |

### Step 5: Record Result

Track each scenario result for the final report:
- Scenario name
- Status (PASS/FAIL/SKIP)
- Summary of analysis
- Code references found
- Gaps identified (for FAIL status)
- Skip reason (for SKIP status)
</verification_process>

---

## Verification Guidelines

<verification_guidelines>
### What Constitutes "Implemented"

A scenario step is considered **implemented** if:
- A code path exists that handles the described behavior
- The relevant service/component has methods that would execute the action
- Data models support the required state changes
- UI components render the expected output (for UI scenarios)

### What Constitutes "Not Implemented"

A scenario step is considered **not implemented** if:
- No code path exists for the described behavior
- Methods are stubbed but have no implementation
- Required models/properties are missing
- SignalR events are not defined or handled

### What Warrants "Skip"

A scenario should be **skipped** if:
- It requires external services not available for static analysis
- It tests future/planned functionality explicitly marked as TODO
- It depends on runtime behavior that cannot be determined statically
- The feature is explicitly disabled or behind a feature flag

### Static Analysis Limitations

Be aware that static analysis **cannot verify**:
- Runtime behavior correctness
- Actual data flow through the system
- UI rendering accuracy
- Performance characteristics
- Race conditions or async behavior

Document these limitations in the report when relevant.
</verification_guidelines>

---

## Report Generation

<report_generation>
### Step 1: Determine Report File Name

Generate a timestamped filename:
```
verification-report-{NN}_{FeatureName}-{YYYY-MM-DDTHH-MM-SS}.md
```

Where:
- `{NN}` = Feature file number (e.g., 04)
- `{FeatureName}` = Feature name in kebab-case (e.g., combat-encounter)
- Timestamp in ISO format with hyphens (file-system safe)

**Example**: `verification-report-04_combat-encounter-2025-12-31T02-55-00.md`

### Step 2: Create tmp Directory (if needed)

```bash
mkdir -p tmp
```

### Step 3: Generate Report

Use `write_to_file` to create the report at `tmp/{filename}`.

Follow the **Report Format Template** below.
</report_generation>

---

## Report Format Template

<report_template>
```markdown
# Feature Verification Report

## Summary

| Property | Value |
|----------|-------|
| **Feature** | {Feature Name} |
| **File** | `tests/Riddle.Specs/Features/{filename}.feature` |
| **Tags** | {tags} |
| **Verified At** | {YYYY-MM-DD HH:MM:SS} |
| **Total Scenarios** | {count} |

## Results Overview

| Status | Count | Percentage |
|--------|-------|------------|
| ✅ PASS | {n} | {%} |
| ❌ FAIL | {n} | {%} |
| ⚠️ SKIP | {n} | {%} |

## Progress Bar

```
[████████████░░░░░░░░] 60% (12/20 scenarios passing)
```

---

## Detailed Results

### ✅ PASS: {Scenario Name}

**Steps Verified**:
- Given {step} → `{file:line}`
- When {step} → `{file:line}`
- Then {step} → `{file:line}`

**Analysis Summary**:
{Brief description of how the code satisfies this scenario}

**Code References**:
- `src/Riddle.Web/Services/{File}.cs` - {description}
- `src/Riddle.Web/Components/{File}.razor` - {description}

---

### ❌ FAIL: {Scenario Name}

**Steps Verified**:
- Given {step} → ✅ `{file:line}`
- When {step} → ❌ Not found
- Then {step} → ⚠️ Partial in `{file:line}`

**Analysis Summary**:
{Brief description of what's missing}

**Gaps Identified**:
1. {Gap 1 - what's missing and where it should be}
2. {Gap 2 - what's missing and where it should be}

**Suggested Implementation**:
- Add method `{MethodName}` to `{Service}` for {purpose}
- Add property `{Property}` to `{Model}` for {purpose}

---

### ⚠️ SKIP: {Scenario Name}

**Reason**: {Why this scenario was skipped}

**Notes**: {Any relevant context}

---

## Implementation Gaps Summary

| Gap | Affected Scenarios | Priority |
|-----|-------------------|----------|
| {Gap description} | {Scenario 1}, {Scenario 2} | High/Medium/Low |
| {Gap description} | {Scenario 3} | High/Medium/Low |

## Recommendations

1. **High Priority**: {recommendation}
2. **Medium Priority**: {recommendation}
3. **Low Priority**: {recommendation}

---

## Verification Notes

- Static analysis performed; runtime behavior not verified
- {Any other relevant notes about the verification process}

---

*Report generated by BDD Feature Verification Workflow*
```
</report_template>

---

## Workflow Execution Example

<workflow_example>
### Example: Verifying Combat Encounter Feature

**User Request**: "Verify the combat feature"

**Step 1: Feature Selection**
- Identified: `tests/Riddle.Specs/Features/04_CombatEncounter.feature`

**Step 2: Parse Feature**
- Feature: Combat Encounter
- Tags: @phase2 @phase3 @combat @llm @signalr
- Scenarios: 22 total

**Step 3: Verify Each Scenario**

For "DM starts combat from narrative":
```xml
<verification_thinking>
**Scenario**: DM starts combat from narrative

**Given**: Party is at "Triboar Trail"
**When**: I tell Riddle "Goblins attack from the bushes!"
**Then**: 
- Riddle should initiate combat mode
- Riddle should request initiative rolls
- I should see "Roll initiative for the party" in the chat

**Code areas to investigate**:
- RiddleLlmService (chat processing)
- CombatService (combat initiation)
- GameHub (SignalR events)

**Code Path Analysis**:
- Found StartCombat method in CombatService
- Found combat-related LLM tools in ToolExecutor
- Found CombatStarted SignalR event in GameHubEvents

**Verdict**: PASS
**Reason**: Code paths exist for combat initiation from chat
</verification_thinking>
```

**Step 4: Generate Report**
- Create `tmp/verification-report-04_combat-encounter-2025-12-31T02-55-00.md`
- Include all 22 scenario results
- Summarize gaps and recommendations
</workflow_example>

---

## Quick Reference

<quick_reference>
### Start Verification
```
1. Read feature file
2. Parse scenarios
3. For each scenario:
   a. Document understanding in <verification_thinking>
   b. Search/read relevant code
   c. Analyze implementation status
   d. Record PASS/FAIL/SKIP
4. Generate report to tmp/
```

### Tools to Use
| Purpose | Tool |
|---------|------|
| Read feature file | `read_file` |
| Find implementations | `search_files` |
| Read source code | `read_file` |
| Understand structure | `list_code_definition_names` |
| Write report | `write_to_file` |

### Report Location
```
tmp/verification-report-{NN}_{feature-name}-{timestamp}.md
```

### Status Definitions
| Status | Meaning |
|--------|---------|
| ✅ PASS | Code paths exist for all steps |
| ❌ FAIL | Missing or incomplete implementation |
| ⚠️ SKIP | Cannot verify (external dependency, future feature) |
</quick_reference>
