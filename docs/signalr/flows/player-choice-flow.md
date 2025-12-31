# Player Choice Flow

This document describes the SignalR communication for the player choice system, where the DM/LLM presents options to players and collects their responses.

## Overview

The player choice flow is a request-response pattern:
1. **DM/LLM presents choices** → Server broadcasts options to all players
2. **Players select their choice** → Each player sends their selection to the DM
3. **DM collects responses** → LLM or DM incorporates choices into the narrative

```mermaid
flowchart LR
    DM[DM/LLM] -->|"1. Presents"| Server[Server]
    Server -->|"2. PlayerChoicesReceived"| Players[All Players]
    Players -->|"3. SubmitChoice"| Server
    Server -->|"4. PlayerChoiceSubmitted"| DM
```

## 1. DM/LLM Presents Choices

When the narrative reaches a decision point:

```mermaid
sequenceDiagram
    autonumber
    participant DM as DM Dashboard
    participant LLM as RiddleLlmService
    participant TE as ToolExecutor
    participant NS as NotificationService
    participant Hub as GameHub
    participant P1 as Player 1
    participant P2 as Player 2

    alt Manual Choice from DM
        DM->>NS: NotifyPlayerChoicesAsync(campaignId, choices)
    else LLM Tool Call
        LLM->>TE: present_choices tool
        TE->>NS: NotifyPlayerChoicesAsync(campaignId, choices)
    end
    
    NS->>Hub: Clients.Group("campaign_{id}_players")<br/>.SendAsync("PlayerChoicesReceived", choices)
    
    Note over NS,Hub: Only players receive choices<br/>(DM already knows them)
    
    par Broadcast to players
        Hub-->>P1: PlayerChoicesReceived
        Hub-->>P2: PlayerChoicesReceived
    end
    
    P1->>P1: Display choice buttons
    P2->>P2: Display choice buttons
```

### Payload Format

```csharp
// Simple list of choice strings
List<string> choices = new List<string>
{
    "Attack the goblin",
    "Try to negotiate",
    "Flee into the forest",
    "Search the room for another exit"
};
```

### Client-Side Handling (Dashboard.razor)

```csharp
_hubConnection.On<List<string>>(GameHubEvents.PlayerChoicesReceived, async choices =>
{
    _availableChoices = choices;
    _selectedChoice = null;  // Reset any previous selection
    _hasSubmittedChoice = false;
    
    await InvokeAsync(StateHasChanged);
});
```

### UI Considerations

- Choices appear as buttons/cards on the player dashboard
- Previous selections are cleared when new choices arrive
- Players should see clear visual feedback that choices are available

## 2. Player Submits Choice

When a player clicks a choice option:

```mermaid
sequenceDiagram
    autonumber
    participant Player as Player Dashboard
    participant Hub as GameHub
    participant DM as DM Dashboard

    Player->>Player: Click "Attack the goblin"
    Player->>Player: _selectedChoice = choice
    
    Player->>Hub: SendAsync("SubmitChoice",<br/>campaignId, characterId,<br/>characterName, choice)
    
    Hub->>Hub: Log choice submission
    Hub->>Hub: Create PlayerChoicePayload
    
    Hub->>DM: Clients.Group("campaign_{id}_dm")<br/>.SendAsync("PlayerChoiceSubmitted", payload)
    
    DM->>DM: Display submitted choice<br/>in DM dashboard
    
    Player->>Player: Mark choice as submitted<br/>Disable choice buttons
```

### Hub Method

```csharp
// GameHub.cs
public async Task SubmitChoice(Guid campaignId, string characterId, string characterName, string choice)
{
    _logger.LogInformation(
        "Choice submitted: Campaign={CampaignId}, Character={CharacterId}, Choice={Choice}",
        campaignId, characterId, choice);

    var payload = new PlayerChoicePayload(characterId, characterName, choice, DateTime.UtcNow);
    
    // Send to DM only
    await Clients.Group($"campaign_{campaignId}_dm").SendAsync(
        GameHubEvents.PlayerChoiceSubmitted, payload);
}
```

### PlayerChoicePayload

```csharp
public record PlayerChoicePayload(
    string CharacterId,
    string CharacterName,
    string Choice,          // The exact choice text selected
    DateTime Timestamp      // When the choice was made
);
```

### Client-Side Submission (Dashboard.razor)

```csharp
private async Task SubmitChoice(string choice)
{
    if (selectedCharacter == null || _hubConnection == null) return;
    
    _selectedChoice = choice;
    _hasSubmittedChoice = true;
    
    await _hubConnection.SendAsync(
        "SubmitChoice", 
        CampaignId, 
        selectedCharacter.Id.ToString(), 
        selectedCharacter.Name, 
        choice);
    
    await InvokeAsync(StateHasChanged);
}
```

## 3. DM Receives Choices

The DM dashboard collects all player submissions:

```mermaid
sequenceDiagram
    autonumber
    participant P1 as Player 1<br/>(Elara)
    participant P2 as Player 2<br/>(Zeke)
    participant Hub as GameHub
    participant DM as DM Dashboard
    participant LLM as RiddleLlmService

    P1->>Hub: SubmitChoice("Attack the goblin")
    Hub->>DM: PlayerChoiceSubmitted<br/>(Elara: "Attack the goblin")
    DM->>DM: Add to pending choices list
    
    P2->>Hub: SubmitChoice("Try to negotiate")
    Hub->>DM: PlayerChoiceSubmitted<br/>(Zeke: "Try to negotiate")
    DM->>DM: Add to pending choices list
    
    Note over DM: All players have responded
    
    alt Continue via LLM
        DM->>LLM: Include choices in next prompt
        LLM->>LLM: Generate narrative<br/>incorporating all choices
    else Manual DM narrative
        DM->>DM: DM narrates outcome<br/>based on choices
    end
```

### Client-Side Handling (Campaign.razor)

```csharp
_hubConnection.On<PlayerChoicePayload>(GameHubEvents.PlayerChoiceSubmitted, async payload =>
{
    _logger.LogInformation(
        "{CharacterName} chose: {Choice}", 
        payload.CharacterName, 
        payload.Choice);
    
    // Add to list of pending choices
    _pendingChoices.Add(payload);
    
    // Update UI to show which players have responded
    await InvokeAsync(StateHasChanged);
});
```

## Full Choice Round Flow

```mermaid
sequenceDiagram
    autonumber
    participant DM as DM
    participant Server as Server
    participant P1 as Elara (Player 1)
    participant P2 as Zeke (Player 2)
    participant P3 as Gandalf (Player 3)

    Note over DM,P3: === DM presents decision point ===
    DM->>Server: "What do you do?"<br/>+ 3 choices
    Server-->>P1: PlayerChoicesReceived
    Server-->>P2: PlayerChoicesReceived
    Server-->>P3: PlayerChoicesReceived
    
    Note over P1,P3: === Players deliberate ===
    P1->>P1: Views choices
    P2->>P2: Views choices
    P3->>P3: Views choices
    
    Note over DM,P3: === Players respond (any order) ===
    P2->>Server: SubmitChoice("Attack")
    Server-->>DM: Zeke chose "Attack"
    
    P1->>Server: SubmitChoice("Negotiate")
    Server-->>DM: Elara chose "Negotiate"
    
    P3->>Server: SubmitChoice("Attack")
    Server-->>DM: Gandalf chose "Attack"
    
    Note over DM: DM sees: 2 Attack, 1 Negotiate
    
    DM->>DM: Narrate combined outcome<br/>"Zeke and Gandalf charge forward<br/>while Elara attempts diplomacy..."
```

## State Management

### Player Dashboard State

```csharp
// Choice-related state
private List<string>? _availableChoices;     // Current choices from DM
private string? _selectedChoice;              // What this player chose
private bool _hasSubmittedChoice;             // Prevents double-submission
```

### DM Dashboard State

```csharp
// Tracking player responses
private List<PlayerChoicePayload> _pendingChoices = new();
private int _expectedResponseCount;           // Number of active players
```

## Error Handling

### Double Submission Prevention

```csharp
private async Task SubmitChoice(string choice)
{
    if (_hasSubmittedChoice) return;  // Ignore duplicate clicks
    
    _hasSubmittedChoice = true;
    // ... submit logic
}
```

### Disconnection During Choice

If a player disconnects while choices are pending:
- DM receives `PlayerDisconnected` event
- DM can proceed without that player's choice
- Reconnected player will need new choices broadcast

### Choice Timeout (Optional)

Consider implementing a timeout if players don't respond:

```csharp
// Pseudo-code for timeout handling
private async Task StartChoiceTimer(int seconds)
{
    await Task.Delay(TimeSpan.FromSeconds(seconds));
    
    if (_pendingChoices.Count < _expectedResponseCount)
    {
        // Some players haven't responded
        // DM can proceed with partial responses
    }
}
```

## Event Summary

| Event | Direction | Target | Payload | When |
|-------|-----------|--------|---------|------|
| `PlayerChoicesReceived` | S→C | `_players` | `List<string>` | DM/LLM presents choices |
| `PlayerChoiceSubmitted` | C→S→C | `_dm` | `PlayerChoicePayload` | Player selects choice |

## Key Points

1. **Choices go to players only**: The DM already knows the choices (they presented them), so `PlayerChoicesReceived` only goes to `_players` group.

2. **Submissions go to DM only**: When a player submits, only the DM needs to know. Other players don't see each other's choices (unless you want to add that feature).

3. **No automatic aggregation**: The system doesn't automatically determine "winner" or consensus. The DM/LLM decides how to handle conflicting choices.

4. **Asynchronous responses**: Players can submit in any order. There's no requirement for simultaneous submission.

5. **State reset**: When new choices arrive, previous selections are cleared. Each choice round is independent.
