# Phase 4 Objective 4: Real-Time Player Choice Submission

**Branch:** `feature/phase4-obj4-player-choice-submission`
**Started:** 2025-12-29
**Status:** ✅ Complete

## Objective Description
Enable players to submit choices via SignalR and have the DM receive those choices in real-time, with visual indicators showing the DM who chose what.

## Acceptance Criteria
- [x] Player Dashboard submits choices via SignalR `SubmitChoice` method
- [x] Player Dashboard subscribes to `PlayerChoicesReceived` for real-time choice updates
- [x] DM Campaign page subscribes to `PlayerChoiceSubmitted` to receive player choices
- [x] Both pages setup SignalR connections with proper cleanup on dispose
- [x] DM sees per-player indicators showing who chose which option
- [x] DM sees "X/Y responded" counter and "Waiting for: [names]" list
- [x] Players can change their choice before DM proceeds

## Dependencies
- [x] Phase 4 Obj 3.5: GameHub SignalR payloads and events (Complete)
- [x] Player Dashboard exists with choice display UI
- [x] GameHub `SubmitChoice` method implemented

## Implementation Steps
- [x] Step 1: Add SignalR `SelectChoice` method to Player Dashboard
- [x] Step 2: Subscribe Player Dashboard to `PlayerChoicesReceived` event
- [x] Step 3: Add SignalR connection to DM Campaign page
- [x] Step 4: Subscribe DM Campaign to `PlayerChoiceSubmitted` event

## Files Modified
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Components/Pages/Player/Dashboard.razor` | Modify | Added SelectChoice method with SignalR submission, subscribe to PlayerChoicesReceived |
| `src/Riddle.Web/Components/Pages/DM/Campaign.razor` | Modify | Added SignalR connection, subscribe to PlayerChoiceSubmitted, IAsyncDisposable |

## Verification Steps
- [x] `python build.py` passes
- [x] Application builds without errors

### Manual Testing Procedure

#### Prerequisites
1. Start the application: `python build.py start`
2. Open browser to `https://localhost:5001`
3. Have **two browser windows** ready (one for DM, one for Player - can use incognito mode for second user)

#### Setup: Create Test Campaign with Choices
1. **DM Browser:** Log in as DM user
2. **DM Browser:** Create new campaign or use existing campaign
3. **DM Browser:** Navigate to `/dm/{campaign-id}`
4. **DM Browser:** Add at least one character to the party (if not already present)
5. **DM Browser:** In DM Chat, send a message that will generate player choices (e.g., "Present the party with three options: fight, flee, or negotiate")
6. **Verify:** Choices appear in the "Player Choices" card on the DM page

#### Test 1: Player Sees Choices When Presented
1. **Player Browser:** Log in as a different user (or use the same user if testing single-player)
2. **Player Browser:** Navigate to `/join` and enter the invite code, OR directly go to `/play/{campaign-id}`
3. **Player Browser:** Select/claim a character if prompted
4. **Verify:** The "Player Choices" section shows the same choices the DM set
5. **Expected:** Buttons are enabled and clickable

**Result:** [ ] Pass / [ ] Fail

#### Test 2: Clicking Choice Sends via SignalR
1. **Player Browser:** Click one of the choice buttons (e.g., "fight")
2. **Verify in Player Browser:** 
   - The clicked choice shows as selected (badge shows "✓ You chose: fight")
   - A "Waiting for DM..." message appears below
3. **Check browser dev tools (F12 → Network → WS):** Look for SignalR message with "SubmitChoice"

**Result:** [ ] Pass / [ ] Fail

#### Test 3: DM Sees Per-Player Choice Indicators
1. **DM Browser:** After player clicks choice, verify:
   - The "Player Choices" card shows "X/Y responded" counter (e.g., "1/2 responded")
   - The choice the player selected shows a filled bullet (●) and character name badge
   - The "Waiting:" section shows names of players who haven't responded
2. **Check visual elements:**
   - Selected choices show green badge with "CharacterName ✓"
   - Unselected choices show empty circle bullet (○)
   - Waiting players show yellow/warning badges

**Result:** [ ] Pass / [ ] Fail

#### Test 4: Player Can Change Choice
1. **Player Browser:** After clicking a choice:
   - **Verify:** The selected choice button turns green with "✓ [choice]"
   - **Verify:** Other choice buttons become outlined (not solid)
   - **Verify:** Message shows "✓ Choice submitted • Click another option to change"
2. **Click a different choice:**
   - **Expected:** New choice becomes selected, old choice becomes unselected
   - **DM Browser:** Verify DM sees the updated choice (character badge moves to new option)

**Result:** [ ] Pass / [ ] Fail

#### Test 5: New Choices Reset Submission State
1. **DM Browser:** In DM Chat, send a new message to generate new choices (e.g., "Give the party new options: sneak, attack, or retreat")
2. **Player Browser:** 
   - **Verify:** Previous selection is cleared
   - **Verify:** New choices appear with enabled buttons
   - **Verify:** "Waiting for DM..." message is gone
3. **Player Browser:** Click a new choice
   - **Verify:** Works correctly, can submit new choice

**Result:** [ ] Pass / [ ] Fail

#### Cleanup
1. Stop the application: `python build.py stop`

### Test Summary
- [x] Test 1: Player sees choices when presented
- [x] Test 2: Clicking choice sends via SignalR
- [x] Test 3: DM receives choice notification
- [x] Test 4: Player Can Change Choice
- [x] Test 5: New choices reset submission state

## Commits
| Hash | Message |
|------|---------|
| f4489d2 | feat(player-dashboard): add SignalR choice submission and DM notifications |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| None | N/A |

## Approvals
- [x] Changes reviewed by user
- [x] Approved for push to origin
- [x] Ensured Application is stopped
- [x] Merged to develop
- [x] Feature branch deleted
