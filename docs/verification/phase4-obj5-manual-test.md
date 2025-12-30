# Manual Test Procedure: Phase 4 Objective 5 - Atmospheric LLM Tools

## Prerequisites
1. Application is running (`python build.py start`)
2. Logged in as DM user
3. Campaign exists with at least one character claimed by a player

## Test Setup

### Terminal 1: Start Application
```bash
python build.py start
```

### Terminal 2: Monitor Logs
```bash
python build.py log --tail 100
```

### Browser Windows
- **Window 1 (DM):** Open `/dm/{campaign-id}` 
- **Window 2 (Player):** Open `/play/{campaign-id}` (use incognito or different browser)

---

## Test 1: broadcast_atmosphere_pulse Tool

### Objective
Verify the LLM can send fleeting sensory text to player screens.

### Steps

1. **In DM Chat**, type a message that should trigger atmospheric description:
   ```
   Describe the musty smell of the dungeon corridor as the party enters
   ```

2. **Expected LLM Behavior:**
   - LLM should call `broadcast_atmosphere_pulse` tool with:
     - `text`: Description of the smell
     - `sensory_type`: "smell"
     - `intensity`: "medium" or similar

3. **On Player Dashboard:**
   - Should see a purple/amber bordered panel appear in the left column
   - Shows 👃 icon (for smell type)
   - Contains italic text with the atmospheric description
   - Auto-dismisses after ~10 seconds

### Alternative Direct Test (via DM prompt):
```
Use the broadcast_atmosphere_pulse tool to send "A cold wind howls through the corridor" with intensity "high" and sensory_type "sound"
```

### Expected Player UI:
```
┌─────────────────────────────────────┐
│ 🔊 A cold wind howls through the    │
│    corridor                         │
└─────────────────────────────────────┘
(Red border with pulse animation for "high" intensity)
```

---

## Test 2: set_narrative_anchor Tool

### Objective
Verify the LLM can set a persistent mood banner on player screens.

### Steps

1. **In DM Chat**, type:
   ```
   The party senses danger ahead. Set the mood to ominous and foreboding.
   ```

2. **Expected LLM Behavior:**
   - LLM should call `set_narrative_anchor` tool with:
     - `short_text`: Something like "An ominous presence lurks ahead..."
     - `mood_category`: "danger"

3. **On Player Dashboard:**
   - Should see a red-bordered banner at the top of the page
   - Shows ⚠️ icon (for danger mood)
   - Contains the short mood text in italic
   - **Does NOT auto-dismiss** - persists until updated

### Alternative Direct Test:
```
Use the set_narrative_anchor tool with short_text "The forest feels mysteriously alive" and mood_category "mystery"
```

### Expected Player UI:
```
┌─────────────────────────────────────────────────┐
│ 🔮 The forest feels mysteriously alive          │
└─────────────────────────────────────────────────┘
(Purple border for "mystery" mood, persists at top)
```

### Mood Categories to Test:
| Category | Icon | Border Color |
|----------|------|--------------|
| danger   | ⚠️   | Red          |
| mystery  | 🔮   | Purple       |
| safety   | 🏠   | Green        |
| urgency  | ⏰   | Amber        |
| (default)| 📍   | Gray         |

---

## Test 3: trigger_group_insight Tool

### Objective
Verify the LLM can flash discovery notifications to player screens.

### Steps

1. **In DM Chat**, type:
   ```
   The party's combined knowledge suggests this is an ancient elven ruin. What do they recall?
   ```

2. **Expected LLM Behavior:**
   - LLM should call `trigger_group_insight` tool with:
     - `text`: Historical/lore information about elven ruins
     - `relevant_skill`: "History"
     - `highlight_effect`: true (optional)

3. **On Player Dashboard:**
   - Should see a yellow-bordered card appear below the narrative anchor
   - Shows 💡 icon
   - Contains skill badge (e.g., "History")
   - Contains the insight text
   - If `highlight_effect: true`, card pulses/animates
   - Auto-dismisses after ~8 seconds

### Alternative Direct Test:
```
Use the trigger_group_insight tool with text "The runes are dwarven, possibly from the Iron Age" and relevant_skill "Arcana" with highlight_effect true
```

### Expected Player UI:
```
┌─────────────────────────────────────────────────┐
│ 💡 [Arcana] Party Insight                       │
│    The runes are dwarven, possibly from the     │
│    Iron Age                                     │
└─────────────────────────────────────────────────┘
(Yellow border with pulse animation)
```

---

## Test 4: Combined Atmospheric Experience

### Objective
Verify multiple atmospheric elements can coexist.

### Steps

1. Set a narrative anchor:
   ```
   Set the mood to urgent - time is running out!
   ```

2. Wait for it to appear, then trigger an atmosphere pulse:
   ```
   Describe the sound of rushing water getting louder
   ```

3. While pulse is visible, trigger a group insight:
   ```
   The party realizes the rising water matches legends of trapped chambers
   ```

### Expected Behavior:
- Narrative anchor: Persists at top (amber "urgency" banner)
- Atmosphere pulse: Appears in left column (sound icon)
- Group insight: Appears below anchor (History/Perception badge)
- Pulse should dismiss after 10s
- Insight should dismiss after 8s
- Anchor should remain until explicitly changed

---

## Log Verification

After each test, check the application logs:

```bash
python build.py log "Broadcast atmosphere pulse|Set narrative anchor|Triggered group insight"
```

Expected log entries:
```
info: Riddle.Web.Services.ToolExecutor - Broadcast atmosphere pulse: [text preview] (intensity: X, type: Y)
info: Riddle.Web.Services.ToolExecutor - Set narrative anchor: [text] (mood: X)
info: Riddle.Web.Services.ToolExecutor - Triggered group insight: [text preview] (skill: X, highlight: Y)
```

---

## Troubleshooting

### Tool Not Being Called
1. Check LLM response in DM chat - is it describing atmospherically?
2. Verify system prompt includes atmospheric tools guidance
3. Try more explicit prompts like "Use the broadcast_atmosphere_pulse tool..."

### Player Not Receiving Updates
1. Check browser console for SignalR connection errors
2. Verify player is connected to correct campaign
3. Check `riddle.log` for SignalR broadcast messages

### UI Not Rendering
1. Check browser console for Blazor errors
2. Verify SignalR subscriptions are registered (check Dashboard.razor OnInitialized)
3. Hard refresh the player page (Ctrl+Shift+R)

---

## Cleanup

After testing:
```bash
python build.py stop
```

## Test Results

| Test | Pass/Fail | Notes |
|------|-----------|-------|
| Test 1: Atmosphere Pulse | | |
| Test 2: Narrative Anchor | | |
| Test 3: Group Insight | | |
| Test 4: Combined | | |
| Log Verification | | |
