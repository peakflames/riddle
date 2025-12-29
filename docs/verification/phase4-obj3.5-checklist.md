# Phase 4 Objective 3.5: LLM-Driven Combat System

## Overview
Transform the Combat Tracker from manual DM data entry to LLM-driven control. The LLM will manage combat lifecycle via tool calls: `start_combat`, `end_combat`, `advance_turn`, `add_combatant`, and `remove_combatant`.

## Rationale
- **Before:** DM manually enters combat data via modal (duplicates what LLM already knows)
- **After:** LLM initiates and manages combat via tool calls → Combat Tracker updates automatically
- **Benefit:** Seamless experience, reduced cognitive load, narrative and visualization synced

## Design Decisions

### Tool Return Values
All combat tools return **human-readable strings** for the LLM:
- Success: `"Combat started with 5 combatants: Thorin (PC), Elara (PC), Goblin 1, Goblin 2, Goblin Boss. Turn order established."`
- Error: `"Error: Combat already active. Call end_combat first to start new combat."`

### Narrative Log Updates
Combat tools automatically log to game log:
- `start_combat`: `"[Combat] Combat initiated: 3 PCs vs 2 enemies"`
- `advance_turn`: `"[Combat] Turn advanced: Goblin 1's turn (Round 2)"`
- `add_combatant`: `"[Combat] Reinforcements: Goblin Shaman joined combat"`
- `remove_combatant`: `"[Combat] Goblin 1 fled/removed from combat"`
- `end_combat`: `"[Combat] Combat ended after 4 rounds"`

### Edge Case Handling
| Scenario | Handling |
|----------|----------|
| Combat already active | Return error string: "Combat already active. Call end_combat first." |
| Missing PC in party state | Log warning, skip that PC, continue with available PCs |
| Invalid initiative (outside 1-30) | Clamp to range, return message noting adjustment |
| Mid-combat additions | `add_combatant` tool inserts at correct initiative order |
| Mid-combat removals | `remove_combatant` tool removes and adjusts turn if needed |

---

## Implementation Checklist

### 1. Add Combat Tool Definitions
- [x] **File:** `src/Riddle.Web/Services/RiddleLlmService.cs`
- [x] Add `start_combat` tool schema:
  ```
  enemies: array of {name, initiative, max_hp, current_hp?, ac?}
  pc_initiatives: object mapping character_id → initiative value
  ```
- [x] Add `end_combat` tool schema (no parameters)
- [x] Add `advance_turn` tool schema (no parameters)
- [x] Add `add_combatant` tool schema:
  ```
  name: string
  initiative: integer
  max_hp: integer
  current_hp?: integer (defaults to max_hp)
  ac?: integer (defaults to 10)
  is_enemy: boolean (true=enemy, false=ally/summon)
  ```
- [x] Add `remove_combatant` tool schema:
  ```
  combatant_name: string (name or character_id)
  reason?: string (fled, dismissed, etc. - for log entry)
  ```

### 2. Inject ICombatService into ToolExecutor
- [x] **File:** `src/Riddle.Web/Services/ToolExecutor.cs`
- [x] Add `ICombatService _combatService` field
- [x] Update constructor to inject `ICombatService`

### 3. Implement `start_combat` Handler
- [x] **File:** `src/Riddle.Web/Services/ToolExecutor.cs`
- [x] Add switch case: `"start_combat" => await ExecuteStartCombatAsync(...)`
- [x] Parse enemies array from JSON
- [x] Parse pc_initiatives map from JSON
- [x] Get party state from campaign
- [x] For each PC: lookup from party state, apply initiative (clamp 1-30)
- [x] Log warning for missing PCs, continue with others
- [x] Build `List<CombatantInfo>` combining PCs + enemies
- [x] Check if combat already active → return error string
- [x] Call `ICombatService.StartCombatAsync()`
- [x] Log to narrative log: `"[Combat] Combat initiated..."`
- [x] Return success string with combatant summary

### 4. Implement `end_combat` Handler
- [x] **File:** `src/Riddle.Web/Services/ToolExecutor.cs`
- [x] Add switch case: `"end_combat" => await ExecuteEndCombatAsync(...)`
- [x] Check if combat is active → if not, return error string
- [x] Get round count for log entry
- [x] Call `ICombatService.EndCombatAsync()`
- [x] Log to narrative log: `"[Combat] Combat ended after N rounds"`
- [x] Return success string

### 5. Implement `advance_turn` Handler
- [x] **File:** `src/Riddle.Web/Services/ToolExecutor.cs`
- [x] Add switch case: `"advance_turn" => await ExecuteAdvanceTurnAsync(...)`
- [x] Check if combat is active → if not, return error string
- [x] Call `ICombatService.AdvanceTurnAsync()`
- [x] Log to narrative log: `"[Combat] Turn advanced: {Name}'s turn (Round N)"`
- [x] Return success string with current combatant info

### 6. Implement `add_combatant` Handler
- [x] **File:** `src/Riddle.Web/Services/ToolExecutor.cs`
- [x] Add switch case: `"add_combatant" => await ExecuteAddCombatantAsync(...)`
- [x] Check if combat is active → if not, return error string
- [x] Parse combatant data from JSON
- [x] Clamp initiative to 1-30, note if clamped
- [x] Build `CombatantInfo` object
- [x] Call `ICombatService.AddCombatantAsync()`
- [x] Log to narrative log: `"[Combat] {Name} joined combat (initiative: N)"`
- [x] Return success string

### 7. Implement `remove_combatant` Handler
- [x] **File:** `src/Riddle.Web/Services/ToolExecutor.cs`
- [x] Add switch case: `"remove_combatant" => await ExecuteRemoveCombatantAsync(...)`
- [x] Check if combat is active → if not, return error string
- [x] Find combatant by name or ID
- [x] If not found, return error string
- [x] Call `ICombatService.RemoveCombatantAsync()`
- [x] Log to narrative log with reason: `"[Combat] {Name} {reason}"`
- [x] Return success string

### 8. Update ICombatService Interface (if needed)
- [x] **File:** `src/Riddle.Web/Services/ICombatService.cs`
- [x] Verify all needed methods exist (already complete from Phase 4 Obj 3)

### 9. Update CombatService Implementation (if needed)
- [x] **File:** `src/Riddle.Web/Services/CombatService.cs`
- [x] Implementation complete (already done in Phase 4 Obj 3)
- [x] Proper SignalR notifications in place

### 10. Update System Prompt
- [x] **File:** `src/Riddle.Web/Services/RiddleLlmService.cs`
- [x] Added `# COMBAT_PROTOCOL` section to `BuildSystemPrompt()` with tool descriptions

### 11. Simplify CombatTracker Component
- [x] **File:** `src/Riddle.Web/Components/Combat/CombatTracker.razor`
- [x] Removed "Start Combat" modal (LLM initiates via tool)
- [x] Removed "End Combat" button (LLM controls via tool)
- [x] Removed "Next Turn" button (LLM controls via tool)
- [x] Component is now display-only, reactive to SignalR events
- [x] **File:** `src/Riddle.Web/Components/Combat/CombatantCard.razor`
- [x] Removed manual action buttons (advance turn, mark defeated)
- [x] Component is now display-only

### 12. Update DI Registration
- [x] **File:** `src/Riddle.Web/Program.cs`
- [x] `ICombatService` already registered (from Phase 4 Obj 3)

---

## Verification Tests

### Unit Tests (Manual)
- [ ] Start combat with 2 PCs and 2 enemies → Combat Tracker shows 4 combatants
- [ ] Advance turn 4 times → Round increments
- [ ] Add combatant mid-combat → Appears in correct initiative order
- [ ] Remove combatant → Disappears, turn order adjusts
- [ ] End combat → Tracker clears

### Edge Case Tests
- [ ] Start combat when already active → Error message returned
- [ ] Invalid initiative (35) → Clamped to 30, message notes this
- [ ] Missing PC ID in pc_initiatives → Warning logged, other PCs included
- [ ] Remove non-existent combatant → Error message returned
- [ ] Advance turn with no active combat → Error message returned

### Integration Tests
- [ ] LLM chat: "The goblins attack!" → LLM calls start_combat
- [ ] LLM describes attack → Updates HP → Calls advance_turn
- [ ] LLM says "combat ends" → Calls end_combat → Tracker clears

---

## Files Modified
1. `src/Riddle.Web/Services/RiddleLlmService.cs` - Tool definitions + system prompt
2. `src/Riddle.Web/Services/ToolExecutor.cs` - Tool handlers
3. `src/Riddle.Web/Components/Combat/CombatTracker.razor` - UI simplified to display-only
4. `src/Riddle.Web/Components/Combat/CombatantCard.razor` - UI simplified to display-only

## Build Status
- **Build:** ✅ SUCCESS with 7 warnings (unrelated nullable warnings in Campaign.razor)
- **Date:** 2025-12-29
