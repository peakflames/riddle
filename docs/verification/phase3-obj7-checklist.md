# Phase 3 Objective 7: Real-time Notifications

**Branch:** N/A (not yet implemented)
**Started:** N/A
**Status:** ⬜ Merged to Phase 4

## Objective Description
Add real-time notifications for character claims, player connections via SignalR.

## Acceptance Criteria (from Phase 3 Implementation Plan)

### Step 7.1: Add SignalR Events to GameHub
- [ ] Create `Hubs/GameHub.cs` file
- [ ] `CharacterClaimed(campaignId, characterId, playerId, playerName)` event
- [ ] `PlayerConnected(campaignId, characterId)` event
- [ ] `PlayerDisconnected(campaignId, characterId)` event

### Step 7.2: Wire CharacterService to Hub
- [ ] Broadcast CharacterClaimed when player claims
- [ ] Update character list in real-time on DM dashboard

### Step 7.3: Add Connection Status Tracking
- [ ] Track connected players in campaign
- [ ] Show online/offline status on DM dashboard

## Files Required (from Phase 3 Plan)
| File | Status | Description |
|------|--------|-------------|
| `src/Riddle.Web/Hubs/GameHub.cs` | ❌ Does Not Exist | SignalR hub for game events |
| `src/Riddle.Web/Services/NotificationService.cs` | ❌ Does Not Exist | Notification broadcasting |

## Current State Assessment
- **No `Hubs/` directory exists** in `src/Riddle.Web/`
- SignalR is NOT yet configured in `Program.cs` for custom hubs
- Player Dashboard has static "Connected" badge (placeholder)
- CharacterService does NOT broadcast events on claims

## Verification Steps
- [ ] `python build.py` passes
- [ ] `python build.py start` runs without errors
- [ ] SignalR hub accessible at expected endpoint
- [ ] DM sees toast when player claims character
- [ ] Character card updates to show player name in real-time
- [ ] Online/offline indicator works

## Dependencies
- Phase 3 Objectives 1-6 complete ✅
- SignalR package already included (Microsoft.AspNetCore.SignalR is part of ASP.NET Core)

## Decision Required

**Option A: Implement Phase 3 Objective 7**
- Create GameHub with character claim/connection events
- Wire CharacterService to broadcast claims
- Add connection tracking
- This completes Phase 3 before moving to Phase 4

**Option B: Defer to Phase 4**
- Phase 4 (SignalR & Real-time) has broader scope for SignalR
- Combine Objective 7 into Phase 4 planning
- Mark Phase 3 as complete with Objectives 1-6

## Notes
The Phase 3 Implementation Plan mentions this as a medium-effort objective on Day 5, but it appears to have been deferred. The TODO comment in Dashboard.razor (`SelectChoice` method) acknowledges SignalR integration is pending.

## Approvals
- [ ] User decision on Option A vs Option B
- [ ] Implementation completed (if Option A)
- [ ] Merged to develop
