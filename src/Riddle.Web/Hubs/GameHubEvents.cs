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
    public const string SceneImageUpdated = "SceneImageUpdated";
    public const string PlayerChoicesReceived = "PlayerChoicesReceived";
    public const string PlayerChoiceSubmitted = "PlayerChoiceSubmitted";
    public const string PlayerRollLogged = "PlayerRollLogged";
    
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
