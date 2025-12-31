# SignalR Events Reference

This document catalogs all SignalR events in Project Riddle, including their payloads, target groups, publishers, and subscribers.

## Event Naming Convention

All event names are defined as constants in `GameHubEvents.cs` to prevent typos and enable refactoring:

```csharp
public static class GameHubEvents
{
    public const string PlayerConnected = "PlayerConnected";
    // ... etc
}
```

## Events Summary Table

| Event | Target Group | Direction | Publisher | Subscriber(s) |
|-------|--------------|-----------|-----------|---------------|
| `CharacterClaimed` | `_dm` | S→C | `NotificationService` | `Campaign.razor` |
| `CharacterReleased` | `_dm` | S→C | `NotificationService` | `Campaign.razor` |
| `PlayerConnected` | `_dm` | S→C | `GameHub`, `NotificationService` | `Campaign.razor`, `SignalRTest.razor` |
| `PlayerDisconnected` | `_dm` | S→C | `GameHub`, `NotificationService` | `Campaign.razor`, `SignalRTest.razor` |
| `CharacterStateUpdated` | `_all` | S→C | `NotificationService` | `Campaign.razor`, `Dashboard.razor`, `CombatTracker.razor` |
| `ReadAloudTextReceived` | `_dm` | S→C | `NotificationService` | `Campaign.razor` |
| `SceneImageUpdated` | `_all` | S→C | `NotificationService` | (Future use) |
| `PlayerChoicesReceived` | `_players` | S→C | `NotificationService` | `Dashboard.razor` |
| `PlayerChoiceSubmitted` | `_dm` | S→C | `GameHub`, `NotificationService` | `Campaign.razor`, `SignalRTest.razor` |
| `PlayerRollLogged` | `_all` | S→C | `NotificationService` | `Dashboard.razor` |
| `AtmospherePulseReceived` | `_players` | S→C | `NotificationService` | `Dashboard.razor` |
| `NarrativeAnchorUpdated` | `_players` | S→C | `NotificationService` | `Dashboard.razor` |
| `GroupInsightTriggered` | `_players` | S→C | `NotificationService` | `Dashboard.razor` |
| `CombatStarted` | `_all` | S→C | `NotificationService` | `Dashboard.razor`, `CombatTracker.razor` |
| `CombatEnded` | `_all` | S→C | `NotificationService` | `Dashboard.razor`, `CombatTracker.razor` |
| `TurnAdvanced` | `_all` | S→C | `NotificationService` | `Dashboard.razor`, `CombatTracker.razor` |
| `InitiativeSet` | `_all` | S→C | `NotificationService` | (Future use) |

---

## Character & Player Events

### `CharacterClaimed`

Fired when a player claims (selects) a character to play.

| Property | Value |
|----------|-------|
| Target Group | `_dm` |
| Payload | `CharacterClaimPayload` |

**Payload:**
```csharp
public record CharacterClaimPayload(
    string CharacterId,
    string CharacterName,
    string? PlayerId,
    string? PlayerName,
    bool IsClaimed  // Always true for this event
);
```

**Publisher:** `NotificationService.NotifyCharacterClaimedAsync()`

**Subscribers:** DM Dashboard (`Campaign.razor`)

---

### `CharacterReleased`

Fired when a player releases (unclaims) their character.

| Property | Value |
|----------|-------|
| Target Group | `_dm` |
| Payload | `CharacterClaimPayload` |

**Payload:**
```csharp
public record CharacterClaimPayload(
    string CharacterId,
    string CharacterName,
    string? PlayerId,      // null when released
    string? PlayerName,    // null when released
    bool IsClaimed         // Always false for this event
);
```

**Publisher:** `NotificationService.NotifyCharacterReleasedAsync()`

**Subscribers:** DM Dashboard (`Campaign.razor`)

---

### `PlayerConnected`

Fired when a player connects to a campaign session.

| Property | Value |
|----------|-------|
| Target Group | `_dm` |
| Payload | `PlayerConnectionPayload` |

**Payload:**
```csharp
public record PlayerConnectionPayload(
    string PlayerId,
    string PlayerName,
    string? CharacterId,
    string? CharacterName,
    bool IsOnline  // Always true for this event
);
```

**Publishers:** 
- `GameHub.JoinCampaign()` - Direct from hub when player joins
- `NotificationService.NotifyPlayerConnectedAsync()` - Service-level notification

**Subscribers:** DM Dashboard (`Campaign.razor`), `SignalRTest.razor`

---

### `PlayerDisconnected`

Fired when a player disconnects from a campaign session.

| Property | Value |
|----------|-------|
| Target Group | `_dm` |
| Payload | `PlayerConnectionPayload` |

**Payload:**
```csharp
public record PlayerConnectionPayload(
    string PlayerId,
    string PlayerName,
    string? CharacterId,
    string? CharacterName,
    bool IsOnline  // Always false for this event
);
```

**Publishers:**
- `GameHub.LeaveCampaign()` - Explicit leave
- `GameHub.OnDisconnectedAsync()` - Browser close/network loss
- `NotificationService.NotifyPlayerDisconnectedAsync()` - Service-level notification

**Subscribers:** DM Dashboard (`Campaign.razor`), `SignalRTest.razor`

---

## Game State Events

### `CharacterStateUpdated`

Fired when a character's state changes (HP, conditions, etc.). Used for real-time sync across all clients.

| Property | Value |
|----------|-------|
| Target Group | `_all` |
| Payload | `CharacterStatePayload` |

**Payload:**
```csharp
public record CharacterStatePayload(
    string CharacterId,
    string Key,      // e.g., "CurrentHp", "Conditions"
    object Value     // The new value
);
```

**Publisher:** `NotificationService.NotifyCharacterStateUpdatedAsync()`

**Subscribers:** `Campaign.razor`, `Dashboard.razor`, `CombatTracker.razor`

---

### `ReadAloudTextReceived`

Fired when the LLM generates "read aloud" narration text for the DM.

| Property | Value |
|----------|-------|
| Target Group | `_dm` |
| Payload | `string` (raw text) |

**Publisher:** `NotificationService.NotifyReadAloudTextAsync()`

**Subscribers:** DM Dashboard (`Campaign.razor`) - displays in read-aloud panel

---

### `SceneImageUpdated`

Fired when a scene image is generated or changed.

| Property | Value |
|----------|-------|
| Target Group | `_all` |
| Payload | `string` (image URI) |

**Publisher:** `NotificationService.NotifySceneImageAsync()`

**Subscribers:** (Reserved for future implementation)

---

### `PlayerRollLogged`

Fired when a dice roll is performed and should be broadcast to all participants.

| Property | Value |
|----------|-------|
| Target Group | `_all` |
| Payload | Anonymous object or `RollResultPayload` |

**Payload (simple form):**
```csharp
new { characterId, checkType, result, outcome }
```

**Payload (full form):**
```csharp
public record RollResultPayload(
    string Id,
    string CharacterId,
    string CharacterName,
    string CheckType,      // e.g., "Perception", "Attack Roll"
    int Result,
    string Outcome,        // e.g., "Success", "Failure", "Critical"
    DateTime Timestamp
);
```

**Publisher:** `NotificationService.NotifyPlayerRollAsync()` (two overloads)

**Subscribers:** `Dashboard.razor` - updates dice roll display

---

## Player Interaction Events

### `PlayerChoicesReceived`

Fired when the DM/LLM presents choices to the players (e.g., "What do you do?").

| Property | Value |
|----------|-------|
| Target Group | `_players` |
| Payload | `List<string>` |

**Payload Example:**
```json
["Attack the goblin", "Try to negotiate", "Flee into the forest"]
```

**Publisher:** `NotificationService.NotifyPlayerChoicesAsync()`

**Subscribers:** `Dashboard.razor` - displays choice buttons

---

### `PlayerChoiceSubmitted`

Fired when a player submits their choice back to the DM.

| Property | Value |
|----------|-------|
| Target Group | `_dm` |
| Payload | `PlayerChoicePayload` |

**Payload:**
```csharp
public record PlayerChoicePayload(
    string CharacterId,
    string CharacterName,
    string Choice,          // The selected choice text
    DateTime Timestamp
);
```

**Publishers:**
- `GameHub.SubmitChoice()` - Direct from hub
- `NotificationService.NotifyPlayerChoiceSubmittedAsync()` - Service-level

**Subscribers:** `Campaign.razor`, `SignalRTest.razor`

---

## Atmospheric Events (Players Only)

### `AtmospherePulseReceived`

Fleeting, evocative sensory text that auto-fades after ~10 seconds.

| Property | Value |
|----------|-------|
| Target Group | `_players` |
| Payload | `AtmospherePulsePayload` |

**Payload:**
```csharp
public record AtmospherePulsePayload(
    string Text,           // The sensory description
    string? Intensity,     // "Low", "Medium", "High" - controls animation
    string? SensoryType    // "Sound", "Smell", "Visual", "Feeling"
);
```

**Publisher:** `NotificationService.NotifyAtmospherePulseAsync()`

**Subscribers:** `Dashboard.razor` - displays transient overlay

---

### `NarrativeAnchorUpdated`

Updates the persistent "Current Vibe" banner at the top of player screens.

| Property | Value |
|----------|-------|
| Target Group | `_players` |
| Payload | `NarrativeAnchorPayload` |

**Payload:**
```csharp
public record NarrativeAnchorPayload(
    string ShortText,      // Max ~10 words, e.g., "The Ghost is still weeping nearby"
    string? MoodCategory   // "Danger", "Mystery", "Safety", "Urgency"
);
```

**Publisher:** `NotificationService.NotifyNarrativeAnchorAsync()`

**Subscribers:** `Dashboard.razor` - updates banner

---

### `GroupInsightTriggered`

Flash notification for collective discoveries (clues, secrets).

| Property | Value |
|----------|-------|
| Target Group | `_players` |
| Payload | `GroupInsightPayload` |

**Payload:**
```csharp
public record GroupInsightPayload(
    string Text,           // The clue/information
    string RelevantSkill,  // "Perception", "History", "Nature", etc.
    bool HighlightEffect   // If true, text shimmers/glows (critical clue)
);
```

**Publisher:** `NotificationService.NotifyGroupInsightAsync()`

**Subscribers:** `Dashboard.razor` - displays discovery notification

---

## Combat Events

### `CombatStarted`

Fired when combat begins.

| Property | Value |
|----------|-------|
| Target Group | `_all` |
| Payload | `CombatStatePayload` |

**Payload:**
```csharp
public record CombatStatePayload(
    string? CombatId,
    bool IsActive,
    int RoundNumber,
    List<CombatantInfo> TurnOrder,
    int CurrentTurnIndex
);

public record CombatantInfo(
    string Id,
    string Name,
    string Type,           // "PC", "NPC", "Enemy"
    int Initiative,
    int CurrentHp,
    int MaxHp,
    bool IsDefeated,
    bool IsSurprised
);
```

**Publisher:** `NotificationService.NotifyCombatStartedAsync()`

**Subscribers:** `Dashboard.razor`, `CombatTracker.razor`

---

### `CombatEnded`

Fired when combat ends.

| Property | Value |
|----------|-------|
| Target Group | `_all` |
| Payload | None (parameterless) |

**Publisher:** `NotificationService.NotifyCombatEndedAsync()`

**Subscribers:** `Dashboard.razor`, `CombatTracker.razor`

**Note:** This event is sent without a payload. Clients should reset their combat state.

---

### `TurnAdvanced`

Fired when the turn advances to the next combatant.

| Property | Value |
|----------|-------|
| Target Group | `_all` |
| Payload | Three parameters (not a record) |

**Parameters:**
```csharp
// SendAsync(GameHubEvents.TurnAdvanced, newTurnIndex, currentCombatantId, roundNumber)
int newTurnIndex        // Index in turn order (0-based)
string currentCombatantId // ID of combatant whose turn it now is
int roundNumber         // Current round number
```

**Publisher:** `NotificationService.NotifyTurnAdvancedAsync()`

**Subscribers:** `Dashboard.razor`, `CombatTracker.razor`

**Client-side subscription:**
```csharp
_hubConnection.On<int, string, int>(GameHubEvents.TurnAdvanced, 
    async (newIndex, currentId, roundNumber) => { ... });
```

---

### `InitiativeSet`

Fired when a combatant's initiative is set or changed.

| Property | Value |
|----------|-------|
| Target Group | `_all` |
| Payload | Two parameters |

**Parameters:**
```csharp
// SendAsync(GameHubEvents.InitiativeSet, characterId, initiative)
string characterId
int initiative
```

**Publisher:** `NotificationService.NotifyInitiativeSetAsync()`

**Subscribers:** (Reserved for future use)

---

## Client → Server Methods (Hub Invocations)

These are not events but direct method calls from clients to the hub:

| Method | Parameters | Called By |
|--------|------------|-----------|
| `JoinCampaign` | `(Guid campaignId, string userId, string? characterId, bool isDm)` | All dashboards on load |
| `LeaveCampaign` | `(Guid campaignId)` | All dashboards on dispose |
| `SubmitChoice` | `(Guid campaignId, string characterId, string characterName, string choice)` | Player dashboard |

---

## Adding New Events

1. Add the event name constant to `GameHubEvents.cs`
2. Define a payload record in `GameHubEvents.cs` (if needed)
3. Add the notification method to `INotificationService.cs`
4. Implement the method in `NotificationService.cs`
5. Subscribe in the appropriate Blazor component(s)
6. Update this documentation
