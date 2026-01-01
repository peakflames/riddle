# Implementation Plan: Campaign Management & State Persistence Fixes

## [Overview]
Fix four critical bugs discovered during new campaign dry run testing.

This implementation addresses:
1. **Missing delete campaign UI** - The backend `DeleteCampaignAsync` exists but no UI button triggers it
2. **LLM not proactive** - Riddle waits for DM input instead of proactively suggesting next steps and reminding of pending actions
3. **Combat tracker HP not updating** - HP changes from `update_character_state` tool don't reflect in the Combat Tracker because it doesn't subscribe to `CharacterStateUpdated` SignalR events
4. **Player character claims lost on refresh** - Players see "No characters assigned" after page refresh because the Dashboard doesn't properly reload campaign state from the database on reconnection

## [Types]
No new types are required for this implementation.

The existing types are sufficient:
- `CombatStatePayload` - Already contains `TurnOrder` with `CombatantInfo` including HP
- `CharacterStatePayload` - Already broadcasts HP changes with `CharacterId`, `Key`, `Value`
- `CampaignInstance` - Already has `PartyState` with character claim data

## [Files]
Files to be modified across four bug fixes.

### Bug 1: Delete Campaign UI
- **Modify**: `src/Riddle.Web/Components/Pages/Home.razor`
  - Add delete button to each campaign row
  - Add confirmation modal before deletion
  - Add delete handler that calls `CampaignService.DeleteCampaignAsync`

### Bug 2: LLM Proactive Prompting
- **Modify**: `src/Riddle.Web/Services/RiddleLlmService.cs`
  - Update system prompt to include proactive behavior instructions
  - Add explicit guidance to suggest next steps and remind DM of pending actions
  - Add instruction to summarize "what should happen next" at the end of responses

### Bug 3: Combat Tracker HP Updates
- **Modify**: `src/Riddle.Web/Components/Combat/CombatTracker.razor`
  - Add SignalR subscription for `CharacterStateUpdated` event
  - When HP update received, update the corresponding combatant in `TurnOrder`
  - Trigger re-render when combatant HP changes
- **Modify**: `src/Riddle.Web/Services/CombatService.cs`
  - After `UpdateCombatantHpAsync`, also broadcast full `CombatStatePayload` via `CombatStarted` event (reuses existing pattern for state sync)

### Bug 4: Player Character Claims Persistence
- **Modify**: `src/Riddle.Web/Components/Pages/Player/Dashboard.razor`
  - Add SignalR reconnection handler that reloads campaign from database
  - Reload character assignments on `OnReconnectedAsync` 
  - Add `_hubConnection.Reconnected` event handler to refresh state
- **Modify**: `src/Riddle.Web/Components/Pages/Player/Dashboard.razor`
  - Move character filtering logic to use fresh DB data on component initialization
  - Ensure `playerCharacters` list is repopulated after any reconnection

## [Functions]
Function modifications for each bug fix.

### Bug 1: Delete Campaign UI
**New Functions in Home.razor:**
- `ShowDeleteConfirmation(CampaignInstance campaign)` - Opens confirmation modal
- `ConfirmDelete()` - Calls service and refreshes list
- `CancelDelete()` - Closes modal without action

### Bug 2: LLM Proactive Prompting
**Modified Functions in RiddleLlmService.cs:**
- `BuildSystemPrompt(CampaignInstance campaign)` - Add new `<proactive_behavior>` section with instructions for:
  - Suggesting 2-3 concrete next actions the DM could take
  - Reminding of pending player choices or unresolved situations
  - Prompting for initiative rolls when combat should start
  - Nudging the story forward when players seem stuck

### Bug 3: Combat Tracker HP Updates
**Modified Functions in CombatTracker.razor:**
- `SetupSignalR()` - Add subscription to `CharacterStateUpdated` event
- **New**: `HandleCharacterStateUpdate(CharacterStatePayload payload)` - Update combatant HP in TurnOrder when `Key == "CurrentHp"`

**Modified Functions in CombatService.cs:**
- `UpdateCombatantHpAsync()` - After saving, broadcast full combat state via `NotifyCombatStartedAsync` to ensure all clients sync

### Bug 4: Player Character Claims Persistence  
**Modified Functions in Dashboard.razor:**
- `SetupSignalRAsync()` - Add `_hubConnection.Reconnected += OnReconnected` handler
- **New**: `OnReconnected(string? connectionId)` - Reload campaign from DB and repopulate character list
- `OnInitializedAsync()` - Ensure character filtering happens AFTER fresh DB load

## [Classes]
No new classes are required.

Existing classes already support the needed functionality:
- `CampaignService` - Has `DeleteCampaignAsync` 
- `CombatService` - Has `UpdateCombatantHpAsync` and `GetCombatStateAsync`
- `CharacterService` - Has `GetPlayerCharactersAsync`

## [Dependencies]
No new dependencies required.

All functionality uses existing:
- Flowbite Blazor for UI components (Modal, Button)
- SignalR for real-time updates
- EF Core for database operations

## [Testing]
Manual testing approach for each bug fix.

### Bug 1: Delete Campaign
1. Navigate to Home page with multiple campaigns
2. Click delete button on a campaign
3. Verify confirmation modal appears
4. Confirm deletion
5. Verify campaign is removed from list
6. Verify database using `python build.py db campaigns`

### Bug 2: LLM Proactive Prompting
1. Start a new campaign
2. Send initial message to Riddle
3. Verify response includes suggestions for next steps
4. After combat ends, verify Riddle suggests post-combat actions
5. When players are waiting, verify Riddle reminds DM of pending choices

### Bug 3: Combat Tracker HP Updates
1. Start combat in a campaign
2. Have LLM deal damage to a combatant
3. Verify Combat Tracker HP bar updates in real-time
4. Refresh page and verify HP is still correct
5. Verify both DM and Player dashboards show updated HP

### Bug 4: Player Character Claims
1. Player joins campaign and claims character
2. Verify claim with `python build.py db characters`
3. Player refreshes page
4. Verify character is still assigned (not "No characters assigned")
5. Test with multiple players simultaneously
6. Simulate SignalR reconnection (disable/enable network)

## [Implementation Order]
Sequential implementation to minimize conflicts and enable incremental testing.

1. **Bug 4: Player Character Claims** (Foundation)
   - This is the most critical user-facing bug
   - Simple fix with clear verification path
   - No dependencies on other fixes

2. **Bug 3: Combat Tracker HP Updates** (Core Functionality)
   - Depends on understanding SignalR event flow
   - Required for combat to be usable
   - Enables verification of combat mechanics

3. **Bug 1: Delete Campaign UI** (DM Quality of Life)
   - Independent feature
   - Low risk modification
   - Improves DM campaign management

4. **Bug 2: LLM Proactive Prompting** (Enhancement)
   - System prompt modification only
   - No code changes to tool execution
   - Requires play-testing to verify effectiveness
   - Most subjective to evaluate
