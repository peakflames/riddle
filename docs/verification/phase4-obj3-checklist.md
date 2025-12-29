# Phase 4 Objective 3: Combat Tracker UI

**Branch:** `feature/phase4-obj3-combat-tracker`
**Started:** 2025-12-29
**Status:** ✅ Complete

## Objective Description
Implement a Combat Tracker component for managing turn-based combat in the DM interface.

## Acceptance Criteria
- [x] Combat Tracker component displays in DM Campaign page
- [x] "Start Combat" button shows when no combat active
- [x] Initiative modal pre-populates with party characters
- [x] DM can add enemies with name, HP, and initiative
- [x] Combat start creates sorted turn order
- [x] Current turn is clearly indicated (▶ marker)
- [x] "Next Turn" advances to next combatant
- [x] "End Combat" stops combat and resets state
- [x] PC and enemy icons differentiated (🧙 vs 👹)
- [x] HP display shows current/max values
- [x] Round number displayed during active combat

## Dependencies
- Phase 4 Objective 1 (Player Dashboard base) - Complete
- Party character data available from campaign state

## Implementation Steps
- [x] Create ICombatService interface with combat management methods
- [x] Create CombatService implementation with in-memory state
- [x] Register CombatService in Program.cs DI container
- [x] Create CombatTracker.razor component with modal
- [x] Create CombatantCard.razor for individual display
- [x] Wire CombatTracker to DM Campaign.razor page
- [x] Add SignalR integration for state broadcasting

## Files Modified
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Services/ICombatService.cs` | New | Combat service interface |
| `src/Riddle.Web/Services/CombatService.cs` | New | Combat service implementation |
| `src/Riddle.Web/Components/Combat/CombatTracker.razor` | New | Main combat tracker component |
| `src/Riddle.Web/Components/Combat/CombatantCard.razor` | New | Individual combatant card |
| `src/Riddle.Web/Program.cs` | Modify | Register CombatService |
| `src/Riddle.Web/Components/Pages/DM/Campaign.razor` | Modify | Add CombatTracker to page |

## Verification Steps
- [x] `python build.py` passes
- [x] `python build.py start` runs without errors
- [x] Navigate to DM Campaign page - Combat Tracker visible
- [x] Click "Start Combat" - Modal opens with party members
- [x] Add enemy "Goblin" - Appears in combatant list
- [x] Click "Start Combat" in modal - Initiative order created
- [x] Click "Next Turn" - Turn marker advances
- [x] Click "End Combat" - Returns to "No active combat" state
- [x] No console errors in browser

## Commits
| Hash | Message |
|------|---------|
| 1791f35 | feat(combat): add Combat Tracker UI component |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| Modal Open property not found | Changed to Show property and used ModalHeader/ModalBody/ModalFooter |
| Context collision with AuthorizeView | N/A - not encountered |

## Approvals
- [x] Changes reviewed by user
- [x] Approved for push to origin
- [ ] Ensured Application is stopped
- [ ] Merged to develop
- [ ] Feature branch deleted
