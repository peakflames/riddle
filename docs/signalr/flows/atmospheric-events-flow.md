# Atmospheric Events Flow

This document describes the SignalR communication for atmospheric events—immersive effects that enhance the player experience during gameplay. These events are **player-only**; the DM sees them in the LLM chat but doesn't receive them via SignalR.

## Overview

Atmospheric events create ambient storytelling effects on player screens:

| Event | Purpose | Duration | UI Element |
|-------|---------|----------|------------|
| **Atmosphere Pulse** | Fleeting sensory detail | ~10 sec auto-fade | Transient overlay |
| **Narrative Anchor** | Current scene vibe | Persistent until changed | Top banner |
| **Group Insight** | Discovery notification | ~8-10 sec auto-dismiss | Flash notification |

```mermaid
flowchart TB
    subgraph Origin["Origin (Server)"]
        LLM[LLM Response<br/>via ToolExecutor]
        DM[Manual DM Trigger]
    end
    
    subgraph NotificationService["NotificationService"]
        Pulse[NotifyAtmospherePulseAsync]
        Anchor[NotifyNarrativeAnchorAsync]
        Insight[NotifyGroupInsightAsync]
    end
    
    subgraph Players["Player Dashboards Only"]
        P1[Player 1]
        P2[Player 2]
        P3[Player 3]
    end
    
    LLM --> Pulse
    LLM --> Anchor
    LLM --> Insight
    DM --> Pulse
    DM --> Anchor
    DM --> Insight
    
    Pulse -->|"_players group"| P1
    Pulse -->|"_players group"| P2
    Pulse -->|"_players group"| P3
    
    Anchor -->|"_players group"| P1
    Anchor -->|"_players group"| P2
    Anchor -->|"_players group"| P3
    
    Insight -->|"_players group"| P1
    Insight -->|"_players group"| P2
    Insight -->|"_players group"| P3
```

## 1. Atmosphere Pulse

Fleeting, evocative sensory text that creates momentary immersion. Auto-fades after approximately 10 seconds.

### Use Cases
- "A cold draft whispers past your neck"
- "The distant sound of dripping water echoes"
- "A faint smell of sulfur tickles your nose"

### Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant LLM as RiddleLlmService
    participant TE as ToolExecutor
    participant NS as NotificationService
    participant Hub as GameHub
    participant P1 as Player 1
    participant P2 as Player 2

    LLM->>TE: atmosphere_pulse tool call
    Note over TE: Tool extracts:<br/>text, intensity, sensoryType
    
    TE->>NS: NotifyAtmospherePulseAsync(<br/>campaignId, payload)
    
    NS->>Hub: Clients.Group("campaign_{id}_players")<br/>.SendAsync("AtmospherePulseReceived", payload)
    
    par Broadcast to players only
        Hub-->>P1: AtmospherePulseReceived
        Hub-->>P2: AtmospherePulseReceived
    end
    
    P1->>P1: Display overlay with fade-in
    P2->>P2: Display overlay with fade-in
    
    Note over P1,P2: ~10 seconds pass
    
    P1->>P1: Auto-fade out overlay
    P2->>P2: Auto-fade out overlay
```

### AtmospherePulsePayload

```csharp
public record AtmospherePulsePayload(
    string Text,           // The sensory description
    string? Intensity,     // "Low", "Medium", "High" - controls animation speed/color
    string? SensoryType    // "Sound", "Smell", "Visual", "Feeling" - for icon selection
);
```

### Client-Side Handling (Dashboard.razor)

```csharp
_hubConnection.On<AtmospherePulsePayload>(GameHubEvents.AtmospherePulseReceived, async payload =>
{
    _currentAtmospherePulse = payload;
    _showAtmospherePulse = true;
    
    await InvokeAsync(StateHasChanged);
    
    // Auto-dismiss after 10 seconds
    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        _showAtmospherePulse = false;
        await InvokeAsync(StateHasChanged);
    });
});
```

### UI Styling by Intensity

| Intensity | Animation | Color | Opacity |
|-----------|-----------|-------|---------|
| Low | Slow fade | Soft gray | 60% |
| Medium | Standard fade | Neutral | 80% |
| High | Quick pulse | Accent color | 100% |

### UI Styling by Sensory Type

| Sensory Type | Icon | Suggested Style |
|--------------|------|-----------------|
| Sound | 🔊 / ear icon | Waveform background |
| Smell | 👃 / nose icon | Wispy/smoke effect |
| Visual | 👁️ / eye icon | Shimmer effect |
| Feeling | ✋ / hand icon | Ripple effect |

---

## 2. Narrative Anchor

A persistent banner at the top of player screens showing the "current vibe" of the scene. Remains until explicitly updated.

### Use Cases
- "The Ghost is still weeping nearby"
- "Tension hangs heavy in the air"
- "You feel eyes watching from the shadows"

### Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant LLM as RiddleLlmService
    participant TE as ToolExecutor
    participant NS as NotificationService
    participant Hub as GameHub
    participant P1 as Player 1
    participant P2 as Player 2

    LLM->>TE: set_narrative_anchor tool call
    Note over TE: Tool extracts:<br/>shortText, moodCategory
    
    TE->>NS: NotifyNarrativeAnchorAsync(<br/>campaignId, payload)
    
    NS->>Hub: Clients.Group("campaign_{id}_players")<br/>.SendAsync("NarrativeAnchorUpdated", payload)
    
    par Broadcast to players only
        Hub-->>P1: NarrativeAnchorUpdated
        Hub-->>P2: NarrativeAnchorUpdated
    end
    
    P1->>P1: Update persistent banner
    P2->>P2: Update persistent banner
    
    Note over P1,P2: Banner remains visible<br/>until next update
```

### NarrativeAnchorPayload

```csharp
public record NarrativeAnchorPayload(
    string ShortText,      // Max ~10 words - the current narrative state
    string? MoodCategory   // "Danger", "Mystery", "Safety", "Urgency" - for styling
);
```

### Client-Side Handling (Dashboard.razor)

```csharp
_hubConnection.On<NarrativeAnchorPayload>(GameHubEvents.NarrativeAnchorUpdated, payload =>
{
    _currentNarrativeAnchor = payload;
    
    // No auto-dismiss - this stays until replaced
    InvokeAsync(StateHasChanged);
    
    return Task.CompletedTask;
});
```

### UI Styling by Mood Category

| Mood | Border Color | Background | Icon |
|------|--------------|------------|------|
| Danger | Red | Dark red tint | ⚠️ |
| Mystery | Purple | Dark purple tint | ❓ |
| Safety | Green | Dark green tint | 🛡️ |
| Urgency | Orange | Dark orange tint | ⏰ |
| (default) | Gray | Neutral | — |

---

## 3. Group Insight

A flash notification for collective discoveries—clues, secrets, or important information the entire party learns simultaneously.

### Use Cases
- "[Perception] You all notice the hidden door behind the tapestry"
- "[History] This symbol belongs to an ancient cult"
- "[Nature] These tracks were made by something very large"

### Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant LLM as RiddleLlmService
    participant TE as ToolExecutor
    participant NS as NotificationService
    participant Hub as GameHub
    participant P1 as Player 1
    participant P2 as Player 2

    LLM->>TE: group_insight tool call
    Note over TE: Tool extracts:<br/>text, relevantSkill, highlightEffect
    
    TE->>NS: NotifyGroupInsightAsync(<br/>campaignId, payload)
    
    NS->>Hub: Clients.Group("campaign_{id}_players")<br/>.SendAsync("GroupInsightTriggered", payload)
    
    par Broadcast to players only
        Hub-->>P1: GroupInsightTriggered
        Hub-->>P2: GroupInsightTriggered
    end
    
    P1->>P1: Display discovery flash
    P2->>P2: Display discovery flash
    
    Note over P1,P2: ~8-10 seconds
    
    P1->>P1: Auto-dismiss notification
    P2->>P2: Auto-dismiss notification
```

### GroupInsightPayload

```csharp
public record GroupInsightPayload(
    string Text,           // The clue or information discovered
    string RelevantSkill,  // "Perception", "History", "Nature", etc. - for UI labeling
    bool HighlightEffect   // If true, text shimmers/glows to indicate critical clue
);
```

### Client-Side Handling (Dashboard.razor)

```csharp
_hubConnection.On<GroupInsightPayload>(GameHubEvents.GroupInsightTriggered, async payload =>
{
    _currentGroupInsight = payload;
    _showGroupInsight = true;
    
    await InvokeAsync(StateHasChanged);
    
    // Auto-dismiss after 8-10 seconds
    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromSeconds(9));
        _showGroupInsight = false;
        await InvokeAsync(StateHasChanged);
    });
});
```

### UI Considerations

- Display the `RelevantSkill` as a label/badge (e.g., "[Perception]")
- If `HighlightEffect` is true, add shimmer/glow animation to emphasize importance
- Consider a distinct sound effect for critical insights

---

## Combined Flow Example

A typical narrative moment might trigger multiple atmospheric events:

```mermaid
sequenceDiagram
    autonumber
    participant LLM as LLM
    participant Server as Server
    participant Players as All Players

    Note over LLM,Players: === Party enters haunted crypt ===
    
    LLM->>Server: set_narrative_anchor<br/>("An oppressive darkness<br/>surrounds you", "Danger")
    Server-->>Players: NarrativeAnchorUpdated
    Players->>Players: Banner: "An oppressive<br/>darkness surrounds you" 🔴
    
    Note over LLM,Players: === A few moments later ===
    
    LLM->>Server: atmosphere_pulse<br/>("Cold fingers seem to<br/>brush your cheek", "High", "Feeling")
    Server-->>Players: AtmospherePulseReceived
    Players->>Players: Flash: "Cold fingers seem<br/>to brush your cheek" ✋
    
    Note over Players: ~10 sec later, pulse fades
    
    Note over LLM,Players: === Player rolls successful Perception ===
    
    LLM->>Server: group_insight<br/>("Hidden runes glow faintly<br/>on the sarcophagus lid",<br/>"Perception", true)
    Server-->>Players: GroupInsightTriggered
    Players->>Players: Flash: "[Perception] Hidden runes<br/>glow faintly..." ✨
    
    Note over Players: ~9 sec later, insight fades
    Note over Players: Banner still shows danger mood
```

## State Management (Dashboard.razor)

```csharp
// Atmospheric event state
private AtmospherePulsePayload? _currentAtmospherePulse;
private bool _showAtmospherePulse;

private NarrativeAnchorPayload? _currentNarrativeAnchor;  // Persistent, no show flag

private GroupInsightPayload? _currentGroupInsight;
private bool _showGroupInsight;
```

## Event Summary

| Event | Direction | Target | Payload | Duration |
|-------|-----------|--------|---------|----------|
| `AtmospherePulseReceived` | S→C | `_players` | `AtmospherePulsePayload` | ~10 sec |
| `NarrativeAnchorUpdated` | S→C | `_players` | `NarrativeAnchorPayload` | Persistent |
| `GroupInsightTriggered` | S→C | `_players` | `GroupInsightPayload` | ~8-10 sec |

## Key Points

1. **Players only**: All atmospheric events go to `_players` group only. The DM sees these in the chat/LLM response but doesn't need SignalR delivery.

2. **Non-blocking**: These events are fire-and-forget. They don't require acknowledgment or response from players.

3. **Layerable**: Multiple events can be active simultaneously (e.g., narrative anchor + atmosphere pulse + group insight all showing at once).

4. **LLM-driven**: Most atmospheric events originate from LLM tool calls, making them contextually appropriate to the narrative.

5. **Graceful handling**: If an event arrives while a previous one is still showing:
   - Atmosphere Pulse: Replace the current pulse (reset timer)
   - Narrative Anchor: Replace immediately (it's meant to be the current state)
   - Group Insight: Consider queueing or replacing based on UX preference

6. **Reconnection**: After reconnection, the narrative anchor state should be reloaded from the database (if persisted) or the player will see the next update.
