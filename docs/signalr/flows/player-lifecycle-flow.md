# Player Lifecycle Flow

This document describes the SignalR communication flow for player connections, including joining a campaign, maintaining connection, handling disconnects, and reconnection scenarios.

## Overview

The player lifecycle involves several key phases:
1. **Initial Connection** - Player dashboard loads and connects to SignalR
2. **Campaign Join** - Player joins the campaign's SignalR groups
3. **Active Session** - Player receives events and submits actions
4. **Disconnection** - Browser close, navigation away, or network loss
5. **Reconnection** - Automatic reconnection after network issues

## 1. Player Joins Campaign

When a player navigates to the player dashboard (`/player/campaign/{id}`):

```mermaid
sequenceDiagram
    autonumber
    participant Browser as Player Browser
    participant Dashboard as Dashboard.razor
    participant Hub as GameHub
    participant Tracker as ConnectionTracker
    participant Groups as SignalR Groups
    participant DM as DM Dashboard

    Browser->>Dashboard: Navigate to /player/campaign/{id}
    Dashboard->>Dashboard: OnInitializedAsync()
    Dashboard->>Dashboard: Load campaign & character data
    
    Note over Dashboard: Build HubConnection
    Dashboard->>Hub: new HubConnectionBuilder()<br/>.WithUrl("/gamehub")<br/>.WithAutomaticReconnect()
    
    Dashboard->>Dashboard: Register event handlers<br/>(CombatStarted, PlayerChoicesReceived, etc.)
    
    Dashboard->>Hub: StartAsync()
    Hub-->>Dashboard: Connection established
    
    Dashboard->>Hub: SendAsync("JoinCampaign",<br/>campaignId, userId, characterId, false)
    
    Hub->>Tracker: AddConnection(connId, campaignId,<br/>userId, characterId, isDm=false)
    Hub->>Groups: AddToGroupAsync(connId, "campaign_{id}_players")
    Hub->>Groups: AddToGroupAsync(connId, "campaign_{id}_all")
    
    Note over Hub: Notify DM of player arrival
    Hub->>DM: SendAsync("PlayerConnected",<br/>PlayerConnectionPayload)
    
    DM->>DM: Update player status UI
```

### Code Reference (Dashboard.razor)

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();
        
        // Register event handlers...
        _hubConnection.On<CombatStatePayload>(GameHubEvents.CombatStarted, ...);
        _hubConnection.On<List<string>>(GameHubEvents.PlayerChoicesReceived, ...);
        // etc.
        
        await _hubConnection.StartAsync();
        await _hubConnection.SendAsync("JoinCampaign", CampaignId, currentUserId, 
            selectedCharacter?.Id.ToString(), false);
    }
}
```

## 2. DM Joins Campaign

The DM flow is similar but uses `isDm=true`:

```mermaid
sequenceDiagram
    autonumber
    participant DM as DM Browser
    participant Campaign as Campaign.razor
    participant Hub as GameHub
    participant Groups as SignalR Groups

    DM->>Campaign: Navigate to /dm/campaign/{id}
    Campaign->>Campaign: OnInitializedAsync()
    
    Campaign->>Hub: StartAsync()
    Campaign->>Hub: SendAsync("JoinCampaign",<br/>campaignId, userId, null, true)
    
    Hub->>Groups: AddToGroupAsync(connId, "campaign_{id}_dm")
    Hub->>Groups: AddToGroupAsync(connId, "campaign_{id}_all")
    
    Note over Hub: DM does NOT trigger<br/>PlayerConnected event
```

## 3. Player Disconnection (Explicit)

When a player navigates away or closes the tab:

```mermaid
sequenceDiagram
    autonumber
    participant Browser as Player Browser
    participant Dashboard as Dashboard.razor
    participant Hub as GameHub
    participant Tracker as ConnectionTracker
    participant Groups as SignalR Groups
    participant DM as DM Dashboard

    Browser->>Dashboard: Navigate away / Close tab
    Dashboard->>Dashboard: DisposeAsync()
    
    Dashboard->>Hub: SendAsync("LeaveCampaign", campaignId)
    
    Hub->>Tracker: GetConnectionInfo(connId)
    Tracker-->>Hub: ConnectionInfo (userId, characterId, etc.)
    
    Hub->>Groups: RemoveFromGroupAsync(connId, "_dm")
    Hub->>Groups: RemoveFromGroupAsync(connId, "_players")
    Hub->>Groups: RemoveFromGroupAsync(connId, "_all")
    
    Note over Hub: Notify DM of player departure
    Hub->>DM: SendAsync("PlayerDisconnected",<br/>PlayerConnectionPayload)
    
    Hub->>Tracker: RemoveConnection(connId)
    
    Dashboard->>Hub: DisposeAsync()
```

### Code Reference (Dashboard.razor)

```csharp
public async ValueTask DisposeAsync()
{
    if (_hubConnection != null)
    {
        try
        {
            await _hubConnection.SendAsync("LeaveCampaign", CampaignId);
        }
        catch { /* Ignore errors during cleanup */ }
        
        await _hubConnection.DisposeAsync();
    }
}
```

## 4. Player Disconnection (Unexpected)

When the connection is lost due to network issues or browser crash:

```mermaid
sequenceDiagram
    autonumber
    participant Browser as Player Browser
    participant Hub as GameHub
    participant Tracker as ConnectionTracker
    participant DM as DM Dashboard

    Browser-xHub: Connection lost<br/>(network failure)
    
    Note over Hub: SignalR detects disconnect
    Hub->>Hub: OnDisconnectedAsync(exception)
    
    Hub->>Tracker: GetConnectionInfo(connId)
    Tracker-->>Hub: ConnectionInfo
    
    alt Player with character
        Hub->>DM: SendAsync("PlayerDisconnected",<br/>PlayerConnectionPayload)
        DM->>DM: Show player offline indicator
    end
    
    Hub->>Tracker: RemoveConnection(connId)
    
    Note over Hub: SignalR auto-removes from groups
```

### Code Reference (GameHub.cs)

```csharp
public override async Task OnDisconnectedAsync(Exception? exception)
{
    var connectionInfo = _connectionTracker.GetConnectionInfo(Context.ConnectionId);
    
    if (connectionInfo != null)
    {
        if (!connectionInfo.IsDm && connectionInfo.CharacterId != null)
        {
            await Clients.Group($"campaign_{connectionInfo.CampaignId}_dm").SendAsync(
                GameHubEvents.PlayerDisconnected,
                new PlayerConnectionPayload(...));
        }
    }
    
    _connectionTracker.RemoveConnection(Context.ConnectionId);
    await base.OnDisconnectedAsync(exception);
}
```

## 5. Automatic Reconnection

SignalR's automatic reconnection handles transient network issues:

```mermaid
sequenceDiagram
    autonumber
    participant Browser as Player Browser
    participant Dashboard as Dashboard.razor
    participant Hub as GameHub
    participant Groups as SignalR Groups
    participant DM as DM Dashboard

    Browser-xHub: Connection lost
    
    Note over Dashboard: SignalR detects disconnect<br/>WithAutomaticReconnect() kicks in
    
    Dashboard->>Dashboard: Reconnecting event fires
    
    loop Retry with backoff (0s, 2s, 10s, 30s)
        Dashboard->>Hub: Attempt reconnect
        alt Success
            Hub-->>Dashboard: Connection restored
        else Failure
            Note over Dashboard: Wait and retry
        end
    end
    
    Hub-->>Dashboard: Connection restored
    
    Dashboard->>Dashboard: Reconnected event fires
    Dashboard->>Dashboard: OnReconnectedAsync()
    
    Note over Dashboard: Re-join campaign groups<br/>(connection ID changed!)
    Dashboard->>Hub: SendAsync("JoinCampaign",<br/>campaignId, userId, characterId, false)
    
    Hub->>Groups: AddToGroupAsync(newConnId, "_players")
    Hub->>Groups: AddToGroupAsync(newConnId, "_all")
    
    Hub->>DM: SendAsync("PlayerConnected", ...)
    
    Note over Dashboard: Reload state from DB<br/>(may have missed events)
    Dashboard->>Dashboard: RefreshCampaignData()
```

### Code Reference (Dashboard.razor)

```csharp
// Setup reconnection handler
_hubConnection.Reconnected += OnReconnectedAsync;

private async Task OnReconnectedAsync(string? connectionId)
{
    try
    {
        // Re-join campaign group after reconnection
        await _hubConnection!.SendAsync("JoinCampaign", CampaignId, 
            currentUserId, selectedCharacter?.Id.ToString(), false);
        
        // Reload campaign data to catch up on any missed events
        await RefreshCampaignData();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to rejoin campaign after reconnection");
    }
}
```

## State Diagram

```mermaid
stateDiagram-v2
    [*] --> Disconnected
    Disconnected --> Connecting: StartAsync()
    Connecting --> Connected: Success
    Connecting --> Disconnected: Failure
    Connected --> Joined: JoinCampaign()
    Joined --> Reconnecting: Connection lost
    Reconnecting --> Joined: Reconnected + Rejoin
    Reconnecting --> Disconnected: Max retries exceeded
    Joined --> Disconnected: LeaveCampaign() / Dispose
    Disconnected --> [*]
```

## Key Points

1. **Connection ID changes on reconnect**: When SignalR reconnects, the connection ID may change. The client must re-join groups after reconnection.

2. **Automatic reconnect timing**: Default backoff is 0s, 2s, 10s, 30s. After 4 failed attempts, the connection is closed.

3. **State recovery**: After reconnection, the client should reload state from the database since events may have been missed during disconnection.

4. **DM notification**: The DM is always notified when players connect/disconnect (via `PlayerConnected`/`PlayerDisconnected` events).

5. **Multiple tabs**: Each browser tab creates a separate connection. Closing one tab doesn't affect others.
