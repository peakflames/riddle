# Phase 4 Implementation Plan: SignalR & Real-time

**Version:** 1.0  
**Date:** December 29, 2025  
**Status:** Ready for Implementation  
**Phase:** SignalR & Real-time (Week 4)

---

## [Overview]

Phase 4 implements the full SignalR infrastructure for Project Riddle, enabling real-time multi-client synchronization across DM and player dashboards. This phase delivers the GameHub implementation deferred from Phase 3, plus the complete combat tracker with live turn management and player choice submission flow.

**Key Objectives:**
1. Implement GameHub SignalR hub with full event broadcasting
2. Add real-time notification service for character claims and player connections
3. Build Combat Tracker component with live turn order management
4. Implement player choice submission flow via SignalR
5. Add real-time Read Aloud Text Box updates
6. Implement scene image synchronization across clients
7. Add connection status tracking with reconnection handling

**Success Criteria:**
- DM sees real-time notifications when players join/claim characters
- Combat tracker updates synchronously across all connected clients
- Players can submit choices that appear instantly on DM dashboard
- Read Aloud Text Box updates push to all clients within 1 second
- Scene images sync across all player dashboards
- Connection status shows online/offline for all participants
- Graceful reconnection restores current state

**Dependencies:**
- Phase 3 complete (v0.9.0) ✅
- Player Dashboard exists ✅
- DM Campaign page exists ✅
- Character claiming flow works ✅

**BDD Feature Reference:**
- `tests/Riddle.Specs/Features/04_CombatEncounter.feature`
- `tests/Riddle.Specs/Features/05_PlayerDashboard.feature`
- `tests/Riddle.Specs/Features/07_GameStateDashboard.feature`

---

## [Objectives Breakdown]

### Objective 1: GameHub Implementation ✅ COMPLETE
**Scope:** Create SignalR hub with group management and event broadcasting
**Estimated Effort:** Large
**Files:** `Hubs/GameHub.cs`, `Program.cs`
**Status:** Complete - See `docs/verification/phase4-obj1-checklist.md`

### Objective 2: Notification Service ✅ COMPLETE
**Scope:** Service layer for broadcasting events to SignalR groups
**Estimated Effort:** Medium
**Files:** `Services/INotificationService.cs`, `Services/NotificationService.cs`
**Status:** Complete - See `docs/verification/phase4-obj2-checklist.md`

### Objective 3: Combat Tracker Component ⚠️ SUPERSEDED BY OBJ 3.5
**Scope:** Real-time turn order display with initiative management
**Estimated Effort:** Large
**Files:** `Components/Combat/CombatTracker.razor`, `Components/Combat/InitiativeList.razor`

> **Historical Note:** This objective was originally designed for manual DM data entry via UI modal. During implementation, the Product Owner recognized this contradicted the BDD specifications in `04_CombatEncounter.feature`, which describe LLM-initiated combat (e.g., "When I tell Riddle 'Goblins attack!'"). Objective 3.5 was created to implement the correct LLM-driven approach, making the Combat Tracker a display-only component that reacts to SignalR events from LLM tool calls.

### Objective 3.5: LLM-Driven Combat System ✅ COMPLETE
**Scope:** Transform Combat Tracker from manual DM entry to LLM-driven control via tool calls
**Estimated Effort:** Medium
**Files:** `Services/ToolExecutor.cs`, `Services/RiddleLlmService.cs`, `Components/Combat/CombatTracker.razor`, `Components/Combat/CombatantCard.razor`
**Status:** Complete - See `docs/verification/phase4-obj3.5-checklist.md`

**Summary:** LLM now manages combat lifecycle via `start_combat`, `end_combat`, `advance_turn`, `add_combatant`, and `remove_combatant` tools. Combat Tracker is display-only, reactive to SignalR events.

### Objective 4: Player Choice Submission ✅ COMPLETE
**Scope:** Choice buttons to SignalR submission flow
**Estimated Effort:** Medium
**Files:** `Components/Player/PlayerChoicePad.razor`, Player Dashboard updates
**Status:** Complete - See `docs/verification/phase4-obj4-checklist.md`

### Objective 5: Atmospheric Tools for Player Screens ⏳ PENDING
**Scope:** Replace Scene Image with 3 atmospheric LLM tools for immersive player feedback via SignalR
**Estimated Effort:** Medium
**Files:** `Services/RiddleLlmService.cs`, `Services/ToolExecutor.cs`, `Services/INotificationService.cs`, `Services/NotificationService.cs`, `Hubs/GameHubEvents.cs`, `Components/Pages/Player/Dashboard.razor`

> **Design Change Note (2025-12-29):** After beta testing, the Product Owner decided:
> 1. **Read Aloud Text is DM-only** - Removed from Player Dashboard. The DM reads it aloud; players don't need to see it.
> 2. **Scene Image replaced by Atmospheric Tools** - Instead of static images, use 3 dynamic LLM-driven tools that provide immersive sensory feedback to players via SignalR.

**New LLM Tools:**

| Tool | Purpose | UI Element |
|------|---------|------------|
| `broadcast_atmosphere_pulse` | Fleeting sensory text (auto-fades ~10s) | "Atmosphere" area on Player Dashboard |
| `set_narrative_anchor` | Persistent "Current Vibe" banner | Top banner on Player Dashboard |
| `trigger_group_insight` | Flash notification for discoveries | Toast notification popup |

### ~~Objective 6: Scene Image Synchronization~~ ❌ CANCELLED
**Status:** Cancelled - Replaced by Objective 5 (Atmospheric Tools)

### Objective 7: Connection Status Tracking ⏳ PENDING
**Scope:** Online/offline indicators with reconnection handling
**Estimated Effort:** Medium
**Files:** `Services/ConnectionTracker.cs`, UI components

---

## [Types]

### SignalR Event Contracts

```csharp
namespace Riddle.Web.Hubs;

/// <summary>
/// Events that can be sent from server to clients
/// </summary>
public static class GameHubEvents
{
    // === Character & Player Events ===
    public const string CharacterClaimed = "CharacterClaimed";
    public const string CharacterReleased = "CharacterReleased";
    public const string PlayerConnected = "PlayerConnected";
    public const string PlayerDisconnected = "PlayerDisconnected";
    
    // === Game State Events ===
    public const string CharacterStateUpdated = "CharacterStateUpdated";
    public const string ReadAloudTextReceived = "ReadAloudTextReceived";
    public const string SceneImageUpdated = "SceneImageUpdated"; // DEPRECATED - replaced by atmospheric events
    public const string PlayerChoicesReceived = "PlayerChoicesReceived";
    public const string PlayerChoiceSubmitted = "PlayerChoiceSubmitted";
    public const string PlayerRollLogged = "PlayerRollLogged";
    
    // === Atmospheric Events (Objective 5 - Player Screens) ===
    public const string AtmospherePulseReceived = "AtmospherePulseReceived";
    public const string NarrativeAnchorUpdated = "NarrativeAnchorUpdated";
    public const string GroupInsightTriggered = "GroupInsightTriggered";
    
    // === Combat Events ===
    public const string CombatStarted = "CombatStarted";
    public const string CombatEnded = "CombatEnded";
    public const string TurnAdvanced = "TurnAdvanced";
    public const string InitiativeSet = "InitiativeSet";
    public const string CombatantAdded = "CombatantAdded";
    public const string CombatantRemoved = "CombatantRemoved";
    
    // === Connection Events ===
    public const string ConnectionStatusChanged = "ConnectionStatusChanged";
}

/// <summary>
/// Payload for atmosphere pulse events (transient, fleeting sensory text)
/// </summary>
public record AtmospherePulsePayload(
    string Text,
    string? Intensity,     // "Low", "Medium", "High" - controls animation speed/color
    string? SensoryType    // "Sound", "Smell", "Visual", "Feeling" - for icon selection
);

/// <summary>
/// Payload for narrative anchor events (persistent banner at top of player screens)
/// </summary>
public record NarrativeAnchorPayload(
    string ShortText,      // Max 10 words - e.g., "The Ghost is still weeping nearby"
    string? MoodCategory   // "Danger", "Mystery", "Safety", "Urgency" - for border/color styling
);

/// <summary>
/// Payload for group insight events (flash notification for discoveries)
/// </summary>
public record GroupInsightPayload(
    string Text,           // The clue or information discovered
    string RelevantSkill,  // "Perception", "History", "Nature", etc. - for UI labeling
    bool HighlightEffect   // If true, text shimmers/glows to indicate critical clue
);

/// <summary>
/// Payload for character claim events
/// </summary>
public record CharacterClaimPayload(
    string CharacterId,
    string CharacterName,
    string? PlayerId,
    string? PlayerName,
    bool IsClaimed
);

/// <summary>
/// Payload for player connection events
/// </summary>
public record PlayerConnectionPayload(
    string PlayerId,
    string PlayerName,
    string? CharacterId,
    string? CharacterName,
    bool IsOnline
);

/// <summary>
/// Payload for character state updates
/// </summary>
public record CharacterStatePayload(
    string CharacterId,
    string Key,
    object Value
);

/// <summary>
/// Payload for combat state
/// </summary>
public record CombatStatePayload(
    string? CombatId,
    bool IsActive,
    int RoundNumber,
    List<CombatantInfo> TurnOrder,
    int CurrentTurnIndex
);

/// <summary>
/// Combatant info for turn order
/// </summary>
public record CombatantInfo(
    string Id,
    string Name,
    string Type, // "PC", "NPC", "Enemy"
    int Initiative,
    int CurrentHp,
    int MaxHp,
    bool IsDefeated,
    bool IsSurprised
);

/// <summary>
/// Payload for player choice submission
/// </summary>
public record PlayerChoicePayload(
    string CharacterId,
    string CharacterName,
    string Choice,
    DateTime Timestamp
);
```

### Service Interfaces

```csharp
namespace Riddle.Web.Services;

/// <summary>
/// Service for broadcasting events via SignalR
/// </summary>
public interface INotificationService
{
    // === Character Events ===
    Task NotifyCharacterClaimedAsync(Guid campaignId, CharacterClaimPayload payload, CancellationToken ct = default);
    Task NotifyCharacterReleasedAsync(Guid campaignId, CharacterClaimPayload payload, CancellationToken ct = default);
    
    // === Player Connection Events ===
    Task NotifyPlayerConnectedAsync(Guid campaignId, PlayerConnectionPayload payload, CancellationToken ct = default);
    Task NotifyPlayerDisconnectedAsync(Guid campaignId, PlayerConnectionPayload payload, CancellationToken ct = default);
    
    // === Game State Events ===
    Task NotifyCharacterStateUpdatedAsync(Guid campaignId, CharacterStatePayload payload, CancellationToken ct = default);
    Task NotifyReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default);
    Task NotifySceneImageAsync(Guid campaignId, string imageUri, CancellationToken ct = default);
    Task NotifyPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default);
    Task NotifyPlayerChoiceSubmittedAsync(Guid campaignId, PlayerChoicePayload payload, CancellationToken ct = default);
    Task NotifyPlayerRollAsync(Guid campaignId, string characterId, string checkType, int result, string outcome, CancellationToken ct = default);
    
    // === Combat Events ===
    Task NotifyCombatStartedAsync(Guid campaignId, CombatStatePayload payload, CancellationToken ct = default);
    Task NotifyCombatEndedAsync(Guid campaignId, CancellationToken ct = default);
    Task NotifyTurnAdvancedAsync(Guid campaignId, int newTurnIndex, string currentCombatantId, CancellationToken ct = default);
    Task NotifyInitiativeSetAsync(Guid campaignId, string characterId, int initiative, CancellationToken ct = default);
}

/// <summary>
/// Tracks active connections per campaign
/// </summary>
public interface IConnectionTracker
{
    void AddConnection(string connectionId, Guid campaignId, string userId, string? characterId, bool isDm);
    void RemoveConnection(string connectionId);
    IEnumerable<PlayerConnectionPayload> GetConnectedPlayers(Guid campaignId);
    bool IsPlayerOnline(Guid campaignId, string userId);
    string? GetConnectionId(Guid campaignId, string userId);
}
```

### Combat Service Extension

```csharp
namespace Riddle.Web.Services;

/// <summary>
/// Service for combat management operations
/// </summary>
public interface ICombatService
{
    /// <summary>
    /// Start a new combat encounter
    /// </summary>
    Task<CombatEncounter> StartCombatAsync(Guid campaignId, List<CombatantInfo> combatants, CancellationToken ct = default);
    
    /// <summary>
    /// Set initiative for a combatant
    /// </summary>
    Task SetInitiativeAsync(Guid campaignId, string characterId, int initiative, CancellationToken ct = default);
    
    /// <summary>
    /// Advance to the next turn
    /// </summary>
    Task<(int NewTurnIndex, string CurrentCombatantId)> AdvanceTurnAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Mark a combatant as defeated
    /// </summary>
    Task MarkDefeatedAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    
    /// <summary>
    /// End the current combat
    /// </summary>
    Task EndCombatAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Get current combat state
    /// </summary>
    Task<CombatStatePayload?> GetCombatStateAsync(Guid campaignId, CancellationToken ct = default);
}
```

---

## [Files]

### New Files to Create

#### Hubs
| File | Description |
|------|-------------|
| `src/Riddle.Web/Hubs/GameHub.cs` | Main SignalR hub for game events |

#### Services
| File | Description |
|------|-------------|
| `src/Riddle.Web/Services/INotificationService.cs` | Notification service interface |
| `src/Riddle.Web/Services/NotificationService.cs` | SignalR broadcasting implementation |
| `src/Riddle.Web/Services/IConnectionTracker.cs` | Connection tracking interface |
| `src/Riddle.Web/Services/ConnectionTracker.cs` | In-memory connection state |
| `src/Riddle.Web/Services/ICombatService.cs` | Combat management interface |
| `src/Riddle.Web/Services/CombatService.cs` | Combat management implementation |

#### Components - Combat
| File | Description |
|------|-------------|
| `src/Riddle.Web/Components/Combat/CombatTracker.razor` | Turn order display component |
| `src/Riddle.Web/Components/Combat/InitiativeList.razor` | Initiative roll entry |
| `src/Riddle.Web/Components/Combat/CombatantCard.razor` | Individual combatant display |
| `src/Riddle.Web/Components/Combat/TurnIndicator.razor` | Current turn highlight |

#### Components - Player
| File | Description |
|------|-------------|
| `src/Riddle.Web/Components/Player/PlayerChoicePad.razor` | Choice buttons with SignalR submission |
| `src/Riddle.Web/Components/Player/ConnectionStatus.razor` | Online/offline indicator |

#### Components - Shared
| File | Description |
|------|-------------|
| `src/Riddle.Web/Components/Shared/SceneDisplay.razor` | Scene image with real-time updates |
| `src/Riddle.Web/Components/Shared/ReconnectOverlay.razor` | Reconnection UI overlay |

### Files to Modify

| File | Changes |
|------|---------|
| `src/Riddle.Web/Program.cs` | Register SignalR hub, services; configure endpoints |
| `src/Riddle.Web/Services/CharacterService.cs` | Inject NotificationService, broadcast claims |
| `src/Riddle.Web/Services/ToolExecutor.cs` | Use NotificationService instead of direct hub calls |
| `src/Riddle.Web/Components/Pages/DM/Campaign.razor` | Add SignalR subscription, combat tracker |
| `src/Riddle.Web/Components/Pages/Player/Dashboard.razor` | Add SignalR subscription, choice pad |
| `src/Riddle.Web/Components/Chat/DmChat.razor` | Wire ReadAloudTextBox updates |
| `src/Riddle.Web/Components/Characters/CharacterList.razor` | Show online/offline status |

---

## [Implementation Order]

### Objective 1: GameHub Implementation (Days 1-2)

**Step 1.1: Create GameHub.cs**
```csharp
using Microsoft.AspNetCore.SignalR;
using Riddle.Web.Services;

namespace Riddle.Web.Hubs;

public class GameHub : Hub
{
    private readonly IConnectionTracker _connectionTracker;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IConnectionTracker connectionTracker, ILogger<GameHub> logger)
    {
        _connectionTracker = connectionTracker;
        _logger = logger;
    }

    /// <summary>
    /// Join a campaign session as DM or Player
    /// </summary>
    public async Task JoinCampaign(Guid campaignId, string userId, string? characterId, bool isDm)
    {
        var dmGroup = $"campaign_{campaignId}_dm";
        var playersGroup = $"campaign_{campaignId}_players";
        var allGroup = $"campaign_{campaignId}_all";
        
        // Add to appropriate groups
        if (isDm)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, dmGroup);
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, playersGroup);
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, allGroup);
        
        // Track connection
        _connectionTracker.AddConnection(Context.ConnectionId, campaignId, userId, characterId, isDm);
        
        _logger.LogInformation(
            "Client {ConnectionId} joined campaign {CampaignId} as {Role} (User: {UserId}, Character: {CharacterId})",
            Context.ConnectionId, campaignId, isDm ? "DM" : "Player", userId, characterId);
        
        // Notify others of connection (if player)
        if (!isDm && characterId != null)
        {
            await Clients.Group(dmGroup).SendAsync(GameHubEvents.PlayerConnected, new PlayerConnectionPayload(
                userId, Context.User?.Identity?.Name ?? "Unknown", characterId, null, true));
        }
    }

    /// <summary>
    /// Leave a campaign session
    /// </summary>
    public async Task LeaveCampaign(Guid campaignId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign_{campaignId}_dm");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign_{campaignId}_players");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign_{campaignId}_all");
        
        _connectionTracker.RemoveConnection(Context.ConnectionId);
        
        _logger.LogInformation("Client {ConnectionId} left campaign {CampaignId}", Context.ConnectionId, campaignId);
    }

    /// <summary>
    /// Submit a player choice to the DM
    /// </summary>
    public async Task SubmitChoice(Guid campaignId, string characterId, string characterName, string choice)
    {
        _logger.LogInformation(
            "Choice submitted: Campaign={CampaignId}, Character={CharacterId}, Choice={Choice}",
            campaignId, characterId, choice);

        var payload = new PlayerChoicePayload(characterId, characterName, choice, DateTime.UtcNow);
        
        // Send to DM only
        await Clients.Group($"campaign_{campaignId}_dm").SendAsync(GameHubEvents.PlayerChoiceSubmitted, payload);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.RemoveConnection(Context.ConnectionId);
        
        _logger.LogInformation(
            "Client {ConnectionId} disconnected. Exception: {Exception}",
            Context.ConnectionId, exception?.Message);
        
        await base.OnDisconnectedAsync(exception);
    }
}
```

**Step 1.2: Configure SignalR in Program.cs**
```csharp
// Add SignalR
builder.Services.AddSignalR();

// Register services
builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICombatService, CombatService>();

// ...

// Map hub endpoint
app.MapHub<GameHub>("/gamehub");
```

**Step 1.3: Define SignalR Group Naming Convention**
- `campaign_{campaignId}_dm` - DM only
- `campaign_{campaignId}_players` - All players
- `campaign_{campaignId}_all` - Everyone (DM + players)

**Verification:**
- [ ] GameHub.cs compiles
- [ ] /gamehub endpoint accessible
- [ ] Can connect and join groups from client

### Objective 2: Notification Service (Day 2)

**Step 2.1: Create INotificationService.cs**
- Define all event methods as shown in Types section
- Group methods by event category (character, combat, game state)

**Step 2.2: Create NotificationService.cs**
```csharp
using Microsoft.AspNetCore.SignalR;
using Riddle.Web.Hubs;

namespace Riddle.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<GameHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyCharacterClaimedAsync(Guid campaignId, CharacterClaimPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation("Broadcasting CharacterClaimed: {CharacterId} claimed by {PlayerName}", 
            payload.CharacterId, payload.PlayerName);
        
        await _hubContext.Clients
            .Group($"campaign_{campaignId}_dm")
            .SendAsync(GameHubEvents.CharacterClaimed, payload, ct);
    }

    public async Task NotifyReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"campaign_{campaignId}_dm")
            .SendAsync(GameHubEvents.ReadAloudTextReceived, text, ct);
    }

    public async Task NotifyPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"campaign_{campaignId}_players")
            .SendAsync(GameHubEvents.PlayerChoicesReceived, choices, ct);
    }

    public async Task NotifyCharacterStateUpdatedAsync(Guid campaignId, CharacterStatePayload payload, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"campaign_{campaignId}_all")
            .SendAsync(GameHubEvents.CharacterStateUpdated, payload, ct);
    }

    public async Task NotifyCombatStartedAsync(Guid campaignId, CombatStatePayload payload, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"campaign_{campaignId}_all")
            .SendAsync(GameHubEvents.CombatStarted, payload, ct);
    }

    public async Task NotifyTurnAdvancedAsync(Guid campaignId, int newTurnIndex, string currentCombatantId, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"campaign_{campaignId}_all")
            .SendAsync(GameHubEvents.TurnAdvanced, newTurnIndex, currentCombatantId, ct);
    }

    // ... additional methods follow same pattern
}
```

**Step 2.3: Create ConnectionTracker.cs**
```csharp
using System.Collections.Concurrent;

namespace Riddle.Web.Services;

public class ConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConnectionInfo>> _campaignConnections = new();

    public void AddConnection(string connectionId, Guid campaignId, string userId, string? characterId, bool isDm)
    {
        var info = new ConnectionInfo(connectionId, campaignId, userId, characterId, isDm);
        _connections[connectionId] = info;
        
        var campaignDict = _campaignConnections.GetOrAdd(campaignId, _ => new ConcurrentDictionary<string, ConnectionInfo>());
        campaignDict[connectionId] = info;
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var info))
        {
            if (_campaignConnections.TryGetValue(info.CampaignId, out var campaignDict))
            {
                campaignDict.TryRemove(connectionId, out _);
            }
        }
    }

    public IEnumerable<PlayerConnectionPayload> GetConnectedPlayers(Guid campaignId)
    {
        if (!_campaignConnections.TryGetValue(campaignId, out var campaignDict))
            return Enumerable.Empty<PlayerConnectionPayload>();

        return campaignDict.Values
            .Where(c => !c.IsDm)
            .Select(c => new PlayerConnectionPayload(c.UserId, "", c.CharacterId, null, true));
    }

    public bool IsPlayerOnline(Guid campaignId, string userId)
    {
        if (!_campaignConnections.TryGetValue(campaignId, out var campaignDict))
            return false;

        return campaignDict.Values.Any(c => c.UserId == userId);
    }

    public string? GetConnectionId(Guid campaignId, string userId)
    {
        if (!_campaignConnections.TryGetValue(campaignId, out var campaignDict))
            return null;

        return campaignDict.Values.FirstOrDefault(c => c.UserId == userId)?.ConnectionId;
    }

    private record ConnectionInfo(string ConnectionId, Guid CampaignId, string UserId, string? CharacterId, bool IsDm);
}
```

**Verification:**
- [ ] NotificationService broadcasts to correct groups
- [ ] ConnectionTracker tracks connections accurately
- [ ] Services registered in DI container

### Objective 3: Combat Tracker Component (Days 3-4)

**Step 3.1: Create CombatService.cs**
- Implement combat state management
- Wire to database persistence
- Broadcast events via NotificationService

**Step 3.2: Create CombatTracker.razor**
```razor
@using Microsoft.AspNetCore.SignalR.Client
@using Riddle.Web.Hubs
@using Riddle.Web.Models
@implements IAsyncDisposable

<Card>
    <CardHeader>
        <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                <SwordIcon Class="w-5 h-5 text-red-500" />
                Combat Tracker
            </h3>
            @if (Combat?.IsActive == true)
            {
                <Badge Color="BadgeColor.Red">Round @Combat.RoundNumber</Badge>
            }
        </div>
    </CardHeader>
    <CardContent>
        @if (Combat?.IsActive != true)
        {
            <div class="text-center text-gray-500 dark:text-gray-400 py-4">
                <p>No active combat</p>
                @if (IsDm)
                {
                    <Button Color="ButtonColor.Primary" OnClick="StartCombat" Class="mt-2">
                        Start Combat
                    </Button>
                }
            </div>
        }
        else
        {
            <div class="space-y-2">
                @foreach (var (combatant, index) in Combat.TurnOrder.Select((c, i) => (c, i)))
                {
                    <CombatantCard 
                        Combatant="combatant" 
                        IsCurrentTurn="index == Combat.CurrentTurnIndex"
                        IsDm="IsDm"
                        OnAdvanceTurn="AdvanceTurn"
                        OnMarkDefeated="() => MarkDefeated(combatant.Id)" />
                }
            </div>
            
            @if (IsDm)
            {
                <div class="mt-4 flex gap-2">
                    <Button Color="ButtonColor.Light" OnClick="AdvanceTurn">
                        Next Turn
                    </Button>
                    <Button Color="ButtonColor.Red" OnClick="EndCombat">
                        End Combat
                    </Button>
                </div>
            }
        }
    </CardContent>
</Card>

@code {
    [Parameter] public Guid CampaignId { get; set; }
    [Parameter] public CombatStatePayload? Combat { get; set; }
    [Parameter] public bool IsDm { get; set; }
    [Parameter] public EventCallback<CombatStatePayload> CombatChanged { get; set; }
    
    [Inject] private ICombatService CombatService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await SetupSignalR();
    }

    private async Task SetupSignalR()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<CombatStatePayload>(GameHubEvents.CombatStarted, async payload =>
        {
            Combat = payload;
            await CombatChanged.InvokeAsync(payload);
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<int, string>(GameHubEvents.TurnAdvanced, async (newIndex, currentId) =>
        {
            if (Combat != null)
            {
                Combat = Combat with { CurrentTurnIndex = newIndex };
                await CombatChanged.InvokeAsync(Combat);
                await InvokeAsync(StateHasChanged);
            }
        });

        _hubConnection.On(GameHubEvents.CombatEnded, async () =>
        {
            Combat = null;
            await CombatChanged.InvokeAsync(null);
            await InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();
    }

    private async Task StartCombat()
    {
        // Open initiative entry modal
        // Then call CombatService.StartCombatAsync
    }

    private async Task AdvanceTurn()
    {
        if (Combat != null)
        {
            await CombatService.AdvanceTurnAsync(CampaignId);
        }
    }

    private async Task MarkDefeated(string characterId)
    {
        await CombatService.MarkDefeatedAsync(CampaignId, characterId);
    }

    private async Task EndCombat()
    {
        await CombatService.EndCombatAsync(CampaignId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

**Step 3.3: Create CombatantCard.razor**
- Display combatant info (name, HP, initiative)
- Highlight current turn
- DM controls (damage, conditions, defeat)

**Step 3.4: Create InitiativeList.razor**
- Modal for entering initiative rolls
- Auto-sort by initiative value
- Add enemy combatants

**Verification:**
- [ ] Combat tracker displays turn order
- [ ] Turn advances update all clients
- [ ] Defeated combatants removed from order
- [ ] Round counter increments correctly

### Objective 4: Player Choice Submission (Day 4)

**Step 4.1: Create PlayerChoicePad.razor**
```razor
@using Microsoft.AspNetCore.SignalR.Client

<div class="space-y-2">
    @if (Choices.Any())
    {
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-2">
            Choose your action:
        </p>
        <div class="flex flex-wrap gap-2">
            @foreach (var choice in Choices)
            {
                <Button 
                    Color="ButtonColor.Primary" 
                    Disabled="_submitted"
                    OnClick="() => SubmitChoice(choice)">
                    @choice
                </Button>
            }
        </div>
        @if (_submitted)
        {
            <Alert Color="AlertColor.Info" Class="mt-2">
                <span class="font-medium">Choice submitted!</span> Waiting for DM...
            </Alert>
        }
    }
    else
    {
        <div class="text-center text-gray-500 dark:text-gray-400 py-4">
            <p>Waiting for the DM to present choices...</p>
        </div>
    }
</div>

@code {
    [Parameter] public List<string> Choices { get; set; } = [];
    [Parameter] public Guid CampaignId { get; set; }
    [Parameter] public string CharacterId { get; set; } = null!;
    [Parameter] public string CharacterName { get; set; } = null!;
    [Parameter] public HubConnection? HubConnection { get; set; }
    
    private bool _submitted;

    protected override void OnParametersSet()
    {
        // Reset submitted state when new choices arrive
        if (Choices.Any())
        {
            _submitted = false;
        }
    }

    private async Task SubmitChoice(string choice)
    {
        if (HubConnection != null)
        {
            await HubConnection.SendAsync("SubmitChoice", CampaignId, CharacterId, CharacterName, choice);
            _submitted = true;
        }
    }
}
```

**Step 4.2: Update Player Dashboard**
- Subscribe to `PlayerChoicesReceived` event
- Pass choices to PlayerChoicePad
- Clear choices after DM acknowledgment

**Verification:**
- [ ] Player sees choice buttons when presented
- [ ] Clicking choice sends to DM
- [ ] Buttons disable after submission
- [ ] DM sees choice in chat/console

### Objective 5: Atmospheric Tools for Player Screens (Day 5)

> **Design Change:** After beta testing, Read Aloud Text and Scene Image are removed from Player Dashboard. Instead, 3 atmospheric LLM tools provide immersive feedback to players.

**Step 5.1: Add SignalR Event Constants to GameHubEvents.cs**
```csharp
// === Atmospheric Events (Objective 5 - Player Screens) ===
public const string AtmospherePulseReceived = "AtmospherePulseReceived";
public const string NarrativeAnchorUpdated = "NarrativeAnchorUpdated";
public const string GroupInsightTriggered = "GroupInsightTriggered";
```

**Step 5.2: Add Payload Records to GameHubEvents.cs**
```csharp
public record AtmospherePulsePayload(
    string Text,
    string? Intensity,     // "Low", "Medium", "High"
    string? SensoryType    // "Sound", "Smell", "Visual", "Feeling"
);

public record NarrativeAnchorPayload(
    string ShortText,      // Max 10 words
    string? MoodCategory   // "Danger", "Mystery", "Safety", "Urgency"
);

public record GroupInsightPayload(
    string Text,
    string RelevantSkill,  // "Perception", "History", "Nature", etc.
    bool HighlightEffect
);
```

**Step 5.3: Add Notification Methods to INotificationService**
```csharp
// === Atmospheric Events (Players Only) ===
Task NotifyAtmospherePulseAsync(Guid campaignId, AtmospherePulsePayload payload, CancellationToken ct = default);
Task NotifyNarrativeAnchorAsync(Guid campaignId, NarrativeAnchorPayload payload, CancellationToken ct = default);
Task NotifyGroupInsightAsync(Guid campaignId, GroupInsightPayload payload, CancellationToken ct = default);
```

**Step 5.4: Implement Notification Methods in NotificationService.cs**
```csharp
public async Task NotifyAtmospherePulseAsync(Guid campaignId, AtmospherePulsePayload payload, CancellationToken ct = default)
{
    _logger.LogInformation("Broadcasting AtmospherePulse: {Text} ({Intensity}, {SensoryType})", 
        payload.Text, payload.Intensity, payload.SensoryType);
    
    await _hubContext.Clients
        .Group($"campaign_{campaignId}_players")
        .SendAsync(GameHubEvents.AtmospherePulseReceived, payload, ct);
}

public async Task NotifyNarrativeAnchorAsync(Guid campaignId, NarrativeAnchorPayload payload, CancellationToken ct = default)
{
    _logger.LogInformation("Broadcasting NarrativeAnchor: {ShortText} ({MoodCategory})", 
        payload.ShortText, payload.MoodCategory);
    
    await _hubContext.Clients
        .Group($"campaign_{campaignId}_players")
        .SendAsync(GameHubEvents.NarrativeAnchorUpdated, payload, ct);
}

public async Task NotifyGroupInsightAsync(Guid campaignId, GroupInsightPayload payload, CancellationToken ct = default)
{
    _logger.LogInformation("Broadcasting GroupInsight: {Text} ({RelevantSkill}, Highlight={Highlight})", 
        payload.Text, payload.RelevantSkill, payload.HighlightEffect);
    
    await _hubContext.Clients
        .Group($"campaign_{campaignId}_players")
        .SendAsync(GameHubEvents.GroupInsightTriggered, payload, ct);
}
```

**Step 5.5: Add LLM Tool Definitions to RiddleLlmService.BuildToolDefinitions()**
```csharp
new Tool(new ToolFunction(
    "broadcast_atmosphere_pulse",
    "Sends a fleeting, evocative sentence to the 'Atmosphere' section of all Player Screens. Use for transient mood and sensory details.",
    new
    {
        type = "object",
        properties = new
        {
            text = new { type = "string", description = "The atmospheric description to display (e.g., 'The torches flicker violently as a cold draft sweeps through.')." },
            intensity = new { type = "string", description = "The urgency of the pulse (e.g., 'Low', 'Medium', 'High') to control animation speed or color." },
            sensory_type = new { type = "string", description = "The primary sense engaged (e.g., 'Sound', 'Smell', 'Visual', 'Feeling') to help the UI select an icon." }
        },
        required = new[] { "text" }
    })),

new Tool(new ToolFunction(
    "set_narrative_anchor",
    "Updates the persistent 'Current Vibe' or 'Dungeon Instinct' banner at the top of the Player Screens. Use for persistent context.",
    new
    {
        type = "object",
        properties = new
        {
            short_text = new { type = "string", description = "A concise fragment (max 10 words) summarizing the immediate feeling (e.g., 'The Ghost is still weeping nearby')." },
            mood_category = new { type = "string", description = "The thematic mood (e.g., 'Danger', 'Mystery', 'Safety', 'Urgency') to determine the UI border or color." }
        },
        required = new[] { "short_text" }
    })),

new Tool(new ToolFunction(
    "trigger_group_insight",
    "Flashes a distinct notification on all Player Screens representing a collective observation or discovery.",
    new
    {
        type = "object",
        properties = new
        {
            text = new { type = "string", description = "The specific clue or information discovered by the party." },
            relevant_skill = new { type = "string", description = "The skill associated with the finding (e.g., 'Perception', 'History', 'Nature') for UI labeling." },
            highlight_effect = new { type = "boolean", description = "If true, the text will shimmer or glow to indicate a critical clue." }
        },
        required = new[] { "text", "relevant_skill" }
    }))
```

**Step 5.6: Add Tool Handlers to ToolExecutor.cs**
```csharp
case "broadcast_atmosphere_pulse":
{
    var text = GetRequiredString(args, "text");
    var intensity = args.TryGetValue("intensity", out var i) ? i.ToString() : null;
    var sensoryType = args.TryGetValue("sensory_type", out var s) ? s.ToString() : null;
    
    var payload = new AtmospherePulsePayload(text, intensity, sensoryType);
    await _notificationService.NotifyAtmospherePulseAsync(campaignId, payload, ct);
    
    return JsonSerializer.Serialize(new { success = true, message = "Atmosphere pulse broadcast to players" });
}

case "set_narrative_anchor":
{
    var shortText = GetRequiredString(args, "short_text");
    var moodCategory = args.TryGetValue("mood_category", out var m) ? m.ToString() : null;
    
    var payload = new NarrativeAnchorPayload(shortText, moodCategory);
    await _notificationService.NotifyNarrativeAnchorAsync(campaignId, payload, ct);
    
    return JsonSerializer.Serialize(new { success = true, message = "Narrative anchor updated for players" });
}

case "trigger_group_insight":
{
    var text = GetRequiredString(args, "text");
    var relevantSkill = GetRequiredString(args, "relevant_skill");
    var highlightEffect = args.TryGetValue("highlight_effect", out var h) && h.GetBoolean();
    
    var payload = new GroupInsightPayload(text, relevantSkill, highlightEffect);
    await _notificationService.NotifyGroupInsightAsync(campaignId, payload, ct);
    
    return JsonSerializer.Serialize(new { success = true, message = "Group insight triggered for players" });
}
```

**Step 5.7: Update Player Dashboard - Remove Read Aloud & Scene Image, Add Atmospheric UI**

Remove from Dashboard.razor:
- Scene Image section (`@if (!string.IsNullOrEmpty(campaign?.CurrentSceneImageUri))`)
- Read Aloud Text section (`@if (!string.IsNullOrEmpty(campaign?.CurrentReadAloudText))`)

Add new UI elements:
1. **Narrative Anchor Banner** (top of dashboard, persistent)
   - Styled border based on MoodCategory
   - Color coding: Danger=red, Mystery=purple, Safety=green, Urgency=amber
   
2. **Atmosphere Pulse Area** (in Game State Panels column)
   - Fading text with animation (~10s auto-fade)
   - Icon based on SensoryType: 👂 Sound, 👃 Smell, 👁️ Visual, 💭 Feeling
   - Intensity controls animation speed/glow

3. **Group Insight Toast** (floating notification)
   - Skill badge (e.g., "Perception")
   - Shimmer effect if HighlightEffect=true
   - Auto-dismiss after 8-10 seconds

**Step 5.8: Add SignalR Subscriptions to Player Dashboard**
```csharp
// Atmospheric events
_hubConnection.On<AtmospherePulsePayload>(GameHubEvents.AtmospherePulseReceived, async payload =>
{
    _currentAtmosphere = payload;
    _atmosphereTimestamp = DateTime.UtcNow;
    await InvokeAsync(StateHasChanged);
    // Start fade timer
    _ = FadeAtmosphereAfterDelay();
});

_hubConnection.On<NarrativeAnchorPayload>(GameHubEvents.NarrativeAnchorUpdated, async payload =>
{
    _narrativeAnchor = payload;
    await InvokeAsync(StateHasChanged);
});

_hubConnection.On<GroupInsightPayload>(GameHubEvents.GroupInsightTriggered, async payload =>
{
    _groupInsight = payload;
    await InvokeAsync(StateHasChanged);
    // Auto-dismiss after delay
    _ = DismissInsightAfterDelay();
});
```

**Step 5.9: Update System Prompt in RiddleLlmService.cs**
Add atmospheric tools guidance to `<workflow_protocol>`:
```
6. **Atmosphere Tools (Player Screens):**
   - Use `broadcast_atmosphere_pulse()` for transient sensory descriptions (sounds, smells, fleeting visuals)
   - Use `set_narrative_anchor()` to establish persistent mood/context (danger nearby, safe haven found)
   - Use `trigger_group_insight()` for collective discoveries or revelations
   - These tools broadcast ONLY to Players - DM sees tool calls in chat
```

**Verification:**
- [ ] All 3 SignalR events defined in GameHubEvents.cs
- [ ] All 3 payload records defined
- [ ] Notification methods added to INotificationService/NotificationService
- [ ] All 3 LLM tool definitions added to RiddleLlmService
- [ ] All 3 tool handlers added to ToolExecutor
- [ ] Player Dashboard: Read Aloud Text section removed
- [ ] Player Dashboard: Scene Image section removed
- [ ] Player Dashboard: Narrative Anchor banner added
- [ ] Player Dashboard: Atmosphere Pulse display added
- [ ] Player Dashboard: Group Insight toast added
- [ ] SignalR subscriptions wired in Player Dashboard
- [ ] Build passes with `python build.py`
- [ ] Manual test: LLM calls tools and players receive updates

### ~~Objective 6: Scene Image Synchronization~~ ❌ CANCELLED

**Step 6.1: Create SceneDisplay.razor**
```razor
<div class="relative">
    @if (!string.IsNullOrEmpty(ImageUri))
    {
        <img src="@ImageUri" 
             alt="Current Scene" 
             class="w-full h-64 object-cover rounded-lg shadow-lg" />
    }
    else
    {
        <div class="w-full h-64 bg-gray-100 dark:bg-gray-700 rounded-lg flex items-center justify-center">
            <p class="text-gray-500 dark:text-gray-400">No scene image</p>
        </div>
    }
</div>

@code {
    [Parameter] public string? ImageUri { get; set; }
}
```

**Step 6.2: Subscribe to SceneImageUpdated**
- Add SignalR handler in Player Dashboard
- Update scene display on event

**Verification:**
- [ ] Scene image displays on player dashboard
- [ ] Image updates when LLM changes scene
- [ ] All players see same image

### Objective 7: Connection Status Tracking (Day 5-6)

**Step 7.1: Create ConnectionStatus.razor**
```razor
<div class="flex items-center gap-2">
    <span class="@(IsOnline ? "bg-green-500" : "bg-gray-400") w-2 h-2 rounded-full"></span>
    <span class="text-sm @(IsOnline ? "text-green-600 dark:text-green-400" : "text-gray-500")">
        @(IsOnline ? "Connected" : "Offline")
    </span>
</div>

@code {
    [Parameter] public bool IsOnline { get; set; }
}
```

**Step 7.2: Create ReconnectOverlay.razor**
- Show overlay when connection lost
- Auto-reconnect with retry logic
- Restore state on reconnection

**Step 7.3: Update CharacterList with Online Status**
- Query ConnectionTracker for player status
- Show green/gray indicator per character

**Step 7.4: Handle OnDisconnectedAsync**
- Broadcast player disconnect to DM
- Update character list in real-time

**Verification:**
- [ ] Online indicator shows for connected players
- [ ] Indicator updates when player connects/disconnects
- [ ] Reconnection overlay appears on connection loss
- [ ] State restored after reconnection

---

## [Testing]

### Manual Testing Checklist

#### GameHub (Objective 1)
- [ ] Can connect to /gamehub endpoint
- [ ] JoinCampaign adds client to correct groups
- [ ] LeaveCampaign removes from all groups
- [ ] SubmitChoice delivers to DM only
- [ ] OnDisconnectedAsync cleans up connection

#### Notification Service (Objective 2)
- [ ] CharacterClaimed broadcasts to DM group
- [ ] ReadAloudText broadcasts to DM group
- [ ] PlayerChoices broadcasts to players group
- [ ] CharacterStateUpdated broadcasts to all group
- [ ] Combat events broadcast to all group

#### Combat Tracker (Objective 3)
- [ ] Combat can be started with combatants
- [ ] Turn order displays correctly by initiative
- [ ] Next Turn advances to next combatant
- [ ] Round increments after full rotation
- [ ] Defeated combatants removed from order
- [ ] End Combat clears combat state
- [ ] All clients see same combat state

#### Player Choice Submission (Objective 4)
- [ ] Player sees choices when presented
- [ ] Clicking choice sends via SignalR
- [ ] DM receives choice notification
- [ ] Buttons disable after submission
- [ ] New choices reset submission state

#### Read Aloud Text Box (Objective 5)
- [ ] Text updates in real-time
- [ ] Update latency < 1 second
- [ ] Empty state shows when no text

#### Scene Image (Objective 6)
- [ ] Image displays on player dashboard
- [ ] Image updates on SceneImageUpdated event
- [ ] Placeholder shown when no image

#### Connection Status (Objective 7)
- [ ] Green indicator for online players
- [ ] Gray indicator for offline players
- [ ] Status updates on connect/disconnect
- [ ] Reconnect overlay appears on disconnect
- [ ] State restored after reconnection

---

## [Phase 4 Completion Checklist]

### Objective 1: GameHub Implementation
- [ ] Hubs/GameHub.cs created
- [ ] JoinCampaign method with group management
- [ ] LeaveCampaign method
- [ ] SubmitChoice method
- [ ] OnDisconnectedAsync cleanup
- [ ] Hub registered in Program.cs
- [ ] /gamehub endpoint accessible

### Objective 2: Notification Service
- [ ] INotificationService interface created
- [ ] NotificationService implementation
- [ ] IConnectionTracker interface created
- [ ] ConnectionTracker implementation
- [ ] Services registered in DI
- [ ] CharacterService uses NotificationService

### Objective 3: Combat Tracker Component
- [ ] ICombatService interface created
- [ ] CombatService implementation
- [ ] CombatTracker.razor created
- [ ] CombatantCard.razor created
- [ ] InitiativeList.razor created
- [ ] TurnIndicator.razor created
- [ ] Combat start/end workflow
- [ ] Turn advancement with SignalR broadcast

### Objective 4: Player Choice Submission
- [ ] PlayerChoicePad.razor created
- [ ] SignalR SubmitChoice method
- [ ] Player Dashboard subscribes to choices
- [ ] Choice buttons disable after submission
- [ ] DM receives choice notifications

### Objective 5: Read Aloud Text Box Real-time
- [ ] ReadAloudTextBox subscribes to SignalR
- [ ] Text updates in real-time
- [ ] Animation on text change (optional)

### Objective 6: Scene Image Synchronization
- [ ] SceneDisplay.razor created
- [ ] Player Dashboard subscribes to SceneImageUpdated
- [ ] Placeholder for no image state

### Objective 7: Connection Status Tracking
- [ ] ConnectionStatus.razor created
- [ ] ReconnectOverlay.razor created
- [ ] CharacterList shows online status
- [ ] Disconnect broadcasts to DM
- [ ] Reconnection restores state

### Version Bump
- [ ] Version updated to 0.10.0 in Riddle.Web.csproj
- [ ] CHANGELOG.md updated with Phase 4 changes
- [ ] Git tag v0.10.0 created

---

## [Risk Mitigation]

| Risk | Mitigation |
|------|------------|
| SignalR connection drops | Implement WithAutomaticReconnect() with exponential backoff |
| Race conditions in combat | Use database transactions and optimistic concurrency |
| Message ordering issues | Include sequence numbers in combat events |
| Memory leak from connections | ConnectionTracker cleanup on disconnect + periodic sweep |
| Large payload sizes | Paginate combat history, compress scene images |
| Browser tab duplication | Track multiple connections per user, use latest for state |

---

## [UI Mockups]

### Combat Tracker (DM View)
```
┌─────────────────────────────────────┐
│ ⚔️ Combat Tracker        Round 2   │
├─────────────────────────────────────┤
│ ▶ 1. Elara (18)         [Current]  │
│   HP: 15/18  🟢                     │
│                                     │
│   2. Goblin Boss (16)              │
│   HP: 12/21  🟡  [💀 Defeat]       │
│                                     │
│   3. Thorin (15)                   │
│   HP: 8/12   🟠  Poisoned          │
│                                     │
│   4. Goblin 1 (12)                 │
│   HP: 0/7    ☠️  Defeated          │
│                                     │
│ [Next Turn]  [End Combat]          │
└─────────────────────────────────────┘
```

### Player Choice Pad
```
┌─────────────────────────────────────┐
│ Choose your action:                 │
│                                     │
│  [⚔️ Attack]  [🛡️ Defend]          │
│                                     │
│  [🏃 Flee]    [💬 Negotiate]       │
│                                     │
└─────────────────────────────────────┘
```

### Connection Status (DM Party Panel)
```
┌─────────────────────────────────────┐
│ Party                    [+ Add]   │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │ 🧙 Elara         Wizard L3     │ │
│ │ HP: 15/18  AC: 12             │ │
│ │ 🟢 Alice (Online)             │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ ⚔️ Thorin        Fighter L2    │ │
│ │ HP: 8/12   AC: 16  Poisoned   │ │
│ │ ⚫ Bob (Offline)              │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

### Reconnect Overlay
```
┌─────────────────────────────────────────┐
│                                         │
│         ⚠️ Connection Lost              │
│                                         │
│         Reconnecting...                 │
│         [████████░░░░░░░░] 3s           │
│                                         │
│         [Retry Now]  [Cancel]           │
│                                         │
└─────────────────────────────────────────┘
```

---

## [SignalR Client Setup Pattern]

### Blazor Component SignalR Integration

```csharp
@implements IAsyncDisposable

@code {
    private HubConnection? _hubConnection;
    private bool _isConnected;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect(new[] { 
                TimeSpan.Zero, 
                TimeSpan.FromSeconds(2), 
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        // Event subscriptions
        _hubConnection.On<CombatStatePayload>(GameHubEvents.CombatStarted, OnCombatStarted);
        _hubConnection.On<List<string>>(GameHubEvents.PlayerChoicesReceived, OnChoicesReceived);
        _hubConnection.On<string>(GameHubEvents.ReadAloudTextReceived, OnReadAloudReceived);
        
        // Connection state handlers
        _hubConnection.Reconnecting += error =>
        {
            _isConnected = false;
            InvokeAsync(StateHasChanged);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _isConnected = true;
            // Re-join campaign group after reconnection
            return _hubConnection.SendAsync("JoinCampaign", CampaignId, UserId, CharacterId, IsDm);
        };

        _hubConnection.Closed += error =>
        {
            _isConnected = false;
            InvokeAsync(StateHasChanged);
            return Task.CompletedTask;
        };

        await _hubConnection.StartAsync();
        _isConnected = true;
        
        // Join campaign group
        await _hubConnection.SendAsync("JoinCampaign", CampaignId, UserId, CharacterId, IsDm);
    }

    private async Task OnCombatStarted(CombatStatePayload payload)
    {
        // Handle combat started
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnChoicesReceived(List<string> choices)
    {
        // Handle player choices
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnReadAloudReceived(string text)
    {
        // Handle read aloud text
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.SendAsync("LeaveCampaign", CampaignId);
            await _hubConnection.DisposeAsync();
        }
    }
}
```

---

## [Deferred from Phase 3]

The following items from Phase 3 Objective 7 are incorporated into Phase 4:

| Item | Phase 4 Objective |
|------|------------------|
| `CharacterClaimed(campaignId, characterId, playerId, playerName)` event | Objective 2 |
| `PlayerConnected(campaignId, characterId)` event | Objective 1, 7 |
| `PlayerDisconnected(campaignId, characterId)` event | Objective 1, 7 |
| CharacterService broadcasting on claims | Objective 2 |
| Connection status tracking | Objective 7 |
| Online/offline indicators | Objective 7 |

---

## [Implementation Notes & Learnings]

This section documents key learnings from implementing Phase 4 objectives that should inform future development.

### SignalR Payload Design Guidelines

When designing SignalR event payloads, **include ALL state that the client needs to render correctly**. Consider what UI elements depend on each event.

**Example Issue:** `TurnAdvancedPayload` initially only included `CombatantId` and `NewIndex`, but the UI also needed `RoundNumber` to update the round counter. This caused the round display to become stale.

**Resolution:** Added `RoundNumber` to the payload:
```csharp
// Before:
record TurnAdvancedPayload(Guid CombatantId, int NewIndex);

// After:
record TurnAdvancedPayload(Guid CombatantId, int NewIndex, int RoundNumber);
```

### Blazor [Parameter] Mutation Anti-Pattern

**NEVER directly modify `[Parameter]` properties in child components.** Parameters are owned by the parent component - modifying them locally creates a disconnected copy that doesn't trigger parent re-renders.

**Example Issue:** `CombatTracker.razor` was setting `Combat = null` in the `CombatEnded` SignalR handler, but the parent component (`Campaign.razor`) wasn't aware of this change.

**Resolution:** Only invoke the callback, let the parent manage state:
```csharp
// ❌ WRONG
Combat = null;
await CombatChanged.InvokeAsync(null);

// ✅ CORRECT
await CombatChanged.InvokeAsync(null);
```

### LLM Name Normalization for Tool Implementations

LLMs often transform identifiers when using them as tool parameters. Most commonly:
- Spaces → Underscores: `Elara Moonshadow` → `Elara_Moonshadow`
- Mixed case variations

**Resolution:** Always normalize LLM-provided identifiers before database lookups:
```csharp
var normalizedName = pcName.Replace("_", " ");
var character = partyState.FirstOrDefault(c => 
    c.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase) ||
    c.Name.Replace("_", " ").Equals(normalizedName, StringComparison.OrdinalIgnoreCase) ||
    c.Id.ToString().Equals(pcName, StringComparison.OrdinalIgnoreCase));
```

### Debugging Tools Reference

The following `build.py` commands were essential for debugging Phase 4 issues:

| Command | Use Case |
|---------|----------|
| `python build.py log` | View recent app logs for errors/warnings |
| `python build.py log <pattern>` | Search logs for specific issues |
| `python build.py db party` | Verify character data in database |
| `python build.py db characters` | Check character claim status |
| `python build.py db campaigns` | View campaign state including PartyDataLen |

---

## Next Phase Preview

**Phase 5: UI Polish & Integration (Week 5)**
- Complete QuestLog component with LLM integration
- Dice roller modal with 3D animation
- Session management (start/end/pause)
- DM notes panel
- Full end-to-end testing workflow
- Performance optimization
- Error handling and edge cases
- Mobile responsive adjustments
