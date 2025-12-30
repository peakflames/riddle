# Phase 4 Objective 5: Atmospheric Tools for Player Screens

**Branch:** `feature/phase4-obj5-atmospheric-tools`
**Started:** TBD
**Status:** ⏳ Pending

## Objective Description
Replace Scene Image and Read Aloud Text on Player Dashboard with 3 atmospheric LLM tools that provide immersive, real-time sensory feedback to players via SignalR.

**Design Change Note (2025-12-29):** After beta testing, the Product Owner decided:
1. **Read Aloud Text is DM-only** - Removed from Player Dashboard. The DM reads it aloud; players don't need to see it.
2. **Scene Image replaced by Atmospheric Tools** - Instead of static images, use 3 dynamic LLM-driven tools that provide immersive sensory feedback to players.

## New LLM Tools

| Tool | Purpose | UI Element | SignalR Event |
|------|---------|------------|---------------|
| `broadcast_atmosphere_pulse` | Fleeting sensory text (auto-fades ~10s) | "Atmosphere" area | `AtmospherePulseReceived` |
| `set_narrative_anchor` | Persistent "Current Vibe" banner | Top banner | `NarrativeAnchorUpdated` |
| `trigger_group_insight` | Flash notification for discoveries | Toast popup | `GroupInsightTriggered` |

## Acceptance Criteria
- [ ] Read Aloud Text section removed from Player Dashboard
- [ ] Scene Image section removed from Player Dashboard
- [ ] All 3 SignalR events defined in `GameHubEvents.cs`
- [ ] All 3 payload records defined in `GameHubEvents.cs`
- [ ] Notification methods added to `INotificationService` and `NotificationService`
- [ ] All 3 LLM tool definitions added to `RiddleLlmService.BuildToolDefinitions()`
- [ ] All 3 tool handlers added to `ToolExecutor.cs`
- [ ] System prompt updated with atmospheric tools guidance
- [ ] Player Dashboard: Narrative Anchor banner added (top, persistent)
- [ ] Player Dashboard: Atmosphere Pulse display added (auto-fading)
- [ ] Player Dashboard: Group Insight toast added (auto-dismiss)
- [ ] SignalR subscriptions wired in Player Dashboard
- [ ] Build passes with `python build.py`

## Dependencies
- [x] Phase 4 Obj 1: GameHub SignalR infrastructure (Complete)
- [x] Phase 4 Obj 2: NotificationService broadcasting (Complete)
- [x] Phase 4 Obj 4: Player Dashboard SignalR connection (Complete)

## Implementation Steps

### Step 1: Add SignalR Event Constants
- [ ] Add to `Hubs/GameHubEvents.cs`:
  - `AtmospherePulseReceived`
  - `NarrativeAnchorUpdated`
  - `GroupInsightTriggered`

### Step 2: Add Payload Records
- [ ] Add to `Hubs/GameHubEvents.cs`:
  - `AtmospherePulsePayload(Text, Intensity?, SensoryType?)`
  - `NarrativeAnchorPayload(ShortText, MoodCategory?)`
  - `GroupInsightPayload(Text, RelevantSkill, HighlightEffect)`

### Step 3: Add Notification Methods
- [ ] Add interface methods to `Services/INotificationService.cs`:
  - `NotifyAtmospherePulseAsync()`
  - `NotifyNarrativeAnchorAsync()`
  - `NotifyGroupInsightAsync()`
- [ ] Implement in `Services/NotificationService.cs` (broadcast to `_players` group)

### Step 4: Add LLM Tool Definitions
- [ ] Add to `Services/RiddleLlmService.cs` `BuildToolDefinitions()`:
  - `broadcast_atmosphere_pulse` tool definition
  - `set_narrative_anchor` tool definition
  - `trigger_group_insight` tool definition

### Step 5: Add Tool Handlers
- [ ] Add case handlers to `Services/ToolExecutor.cs`:
  - `broadcast_atmosphere_pulse` handler
  - `set_narrative_anchor` handler
  - `trigger_group_insight` handler

### Step 6: Update System Prompt
- [ ] Add atmospheric tools guidance to `<workflow_protocol>` in `RiddleLlmService.cs`

### Step 7: Update Player Dashboard UI
- [ ] Remove Scene Image section from `Dashboard.razor`
- [ ] Remove Read Aloud Text section from `Dashboard.razor`
- [ ] Add component state variables:
  - `_narrativeAnchor` (NarrativeAnchorPayload?)
  - `_currentAtmosphere` (AtmospherePulsePayload?)
  - `_atmosphereTimestamp` (DateTime)
  - `_groupInsight` (GroupInsightPayload?)
- [ ] Add Narrative Anchor banner UI (top of dashboard)
  - Color based on MoodCategory: Danger=red, Mystery=purple, Safety=green, Urgency=amber
- [ ] Add Atmosphere Pulse display (in Game State Panels column)
  - Icon based on SensoryType: 👂 Sound, 👃 Smell, 👁️ Visual, 💭 Feeling
  - Intensity controls animation speed/glow
  - Auto-fade after ~10 seconds
- [ ] Add Group Insight toast (floating notification)
  - Skill badge display
  - Shimmer effect if HighlightEffect=true
  - Auto-dismiss after 8-10 seconds

### Step 8: Add SignalR Subscriptions
- [ ] Subscribe to `AtmospherePulseReceived` in `SetupSignalRAsync()`
- [ ] Subscribe to `NarrativeAnchorUpdated` in `SetupSignalRAsync()`
- [ ] Subscribe to `GroupInsightTriggered` in `SetupSignalRAsync()`
- [ ] Implement `FadeAtmosphereAfterDelay()` helper method
- [ ] Implement `DismissInsightAfterDelay()` helper method

## Files Modified
| File | Change Type | Description |
|------|-------------|-------------|
| `src/Riddle.Web/Hubs/GameHubEvents.cs` | Modify | Add 3 event constants and 3 payload records |
| `src/Riddle.Web/Services/INotificationService.cs` | Modify | Add 3 notification method signatures |
| `src/Riddle.Web/Services/NotificationService.cs` | Modify | Implement 3 notification methods |
| `src/Riddle.Web/Services/RiddleLlmService.cs` | Modify | Add 3 tool definitions, update system prompt |
| `src/Riddle.Web/Services/ToolExecutor.cs` | Modify | Add 3 tool handlers |
| `src/Riddle.Web/Components/Pages/Player/Dashboard.razor` | Modify | Remove RAT/SceneImage, add 3 atmospheric UI elements |

## Verification Steps
- [ ] `python build.py` passes
- [ ] Application builds without errors

### Manual Testing Procedure

#### Prerequisites
1. Start the application: `python build.py start`
2. Open browser to `https://localhost:5001`
3. Have **two browser windows** ready (one for DM, one for Player)

#### Setup: Create Test Campaign
1. **DM Browser:** Log in as DM user
2. **DM Browser:** Create new campaign or use existing campaign
3. **DM Browser:** Navigate to `/dm/{campaign-id}`
4. **DM Browser:** Add at least one character to the party
5. **Player Browser:** Log in and navigate to `/play/{campaign-id}`
6. **Player Browser:** Select/claim a character

#### Test 1: Verify Read Aloud Text Removed from Player Dashboard
1. **DM Browser:** In DM Chat, send a message that triggers `display_read_aloud_text`
2. **Player Browser:** Verify NO Read Aloud Text section appears
3. **DM Browser:** Verify Read Aloud Text DOES appear on DM page

**Result:** [ ] Pass / [ ] Fail

#### Test 2: Verify Scene Image Removed from Player Dashboard
1. **Player Browser:** Verify NO Scene Image section exists
2. **Check Dashboard.razor:** Confirm `CurrentSceneImageUri` section removed

**Result:** [ ] Pass / [ ] Fail

#### Test 3: Atmosphere Pulse Tool
1. **DM Browser:** In DM Chat, prompt LLM to use atmosphere: "Describe an ominous sound the party hears"
2. **Verify LLM calls:** Check Event Log for `broadcast_atmosphere_pulse` tool call
3. **Player Browser:** Verify atmospheric text appears with icon
4. **Wait 10+ seconds:** Verify text fades/disappears

**Result:** [ ] Pass / [ ] Fail

#### Test 4: Narrative Anchor Tool
1. **DM Browser:** In DM Chat, prompt: "Set the mood that danger is nearby"
2. **Verify LLM calls:** Check Event Log for `set_narrative_anchor` tool call
3. **Player Browser:** Verify banner appears at top with appropriate color
4. **Player Browser:** Verify banner persists (does NOT auto-fade)

**Result:** [ ] Pass / [ ] Fail

#### Test 5: Group Insight Tool
1. **DM Browser:** In DM Chat, prompt: "The party notices a hidden clue using Perception"
2. **Verify LLM calls:** Check Event Log for `trigger_group_insight` tool call
3. **Player Browser:** Verify toast notification appears with skill badge
4. **Wait 8-10 seconds:** Verify toast auto-dismisses

**Result:** [ ] Pass / [ ] Fail

#### Test 6: Multiple Players Receive Updates
1. **Open third browser:** Log in as another player
2. **DM Browser:** Trigger each atmospheric tool
3. **Both Player Browsers:** Verify both receive the same updates simultaneously

**Result:** [ ] Pass / [ ] Fail

#### Cleanup
1. Stop the application: `python build.py stop`

### Test Summary
- [ ] Test 1: Read Aloud Text removed from Player Dashboard
- [ ] Test 2: Scene Image removed from Player Dashboard
- [ ] Test 3: Atmosphere Pulse broadcasts and fades
- [ ] Test 4: Narrative Anchor persists
- [ ] Test 5: Group Insight toasts and auto-dismisses
- [ ] Test 6: Multiple players receive updates

## Commits
| Hash | Message |
|------|---------|
| TBD | feat(atmospheric-tools): add 3 LLM tools for player screen immersion |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| TBD | TBD |

## UI Design Reference

### Narrative Anchor Banner (Top of Dashboard)
```
┌────────────────────────────────────────────────────────┐
│ 🔮 The Ghost is still weeping nearby                   │  ← MoodCategory: Mystery (purple border)
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│ ⚠️ Danger lurks in the shadows                         │  ← MoodCategory: Danger (red border)
└────────────────────────────────────────────────────────┘
```

### Atmosphere Pulse Area (Fading Text)
```
┌────────────────────────────────────────────────────────┐
│ 👂 Atmosphere                                          │
│                                                        │
│   "The torches flicker violently as a cold             │
│    draft sweeps through the corridor..."               │
│                                           [fading...]  │
└────────────────────────────────────────────────────────┘
```

### Group Insight Toast (Floating)
```
                    ┌─────────────────────────────────────┐
                    │ 👁️ Perception                        │
                    │                                     │
                    │ You notice faint scratches on the   │
                    │ stone floor leading toward the      │
                    │ eastern wall...                     │
                    │                            [✨glow] │
                    └─────────────────────────────────────┘
```

## Approvals
- [ ] Changes reviewed by user
- [ ] Approved for push to origin
- [ ] Ensured Application is stopped
- [ ] Merged to develop
- [ ] Feature branch deleted
