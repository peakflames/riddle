using Riddle.Web.Hubs;

namespace Riddle.Web.Services;

/// <summary>
/// Service for broadcasting events via SignalR to campaign participants
/// </summary>
public interface INotificationService
{
    // === Character Events ===
    
    /// <summary>
    /// Notify when a player claims a character
    /// </summary>
    Task NotifyCharacterClaimedAsync(Guid campaignId, CharacterClaimPayload payload, CancellationToken ct = default);
    
    /// <summary>
    /// Notify when a character is released (unclaimed)
    /// </summary>
    Task NotifyCharacterReleasedAsync(Guid campaignId, CharacterClaimPayload payload, CancellationToken ct = default);
    
    // === Player Connection Events ===
    
    /// <summary>
    /// Notify when a player connects to the campaign
    /// </summary>
    Task NotifyPlayerConnectedAsync(Guid campaignId, PlayerConnectionPayload payload, CancellationToken ct = default);
    
    /// <summary>
    /// Notify when a player disconnects from the campaign
    /// </summary>
    Task NotifyPlayerDisconnectedAsync(Guid campaignId, PlayerConnectionPayload payload, CancellationToken ct = default);
    
    // === Game State Events ===
    
    /// <summary>
    /// Notify when a character's state is updated (HP, conditions, etc.)
    /// </summary>
    Task NotifyCharacterStateUpdatedAsync(Guid campaignId, CharacterStatePayload payload, CancellationToken ct = default);
    
    /// <summary>
    /// Broadcast read aloud text to DM dashboard
    /// </summary>
    Task NotifyReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default);
    
    /// <summary>
    /// Broadcast scene image URI to all clients
    /// </summary>
    Task NotifySceneImageAsync(Guid campaignId, string imageUri, CancellationToken ct = default);
    
    /// <summary>
    /// Broadcast player choices to all players
    /// </summary>
    Task NotifyPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default);
    
    /// <summary>
    /// Notify DM when a player submits their choice
    /// </summary>
    Task NotifyPlayerChoiceSubmittedAsync(Guid campaignId, PlayerChoicePayload payload, CancellationToken ct = default);
    
    /// <summary>
    /// Notify when a player rolls dice
    /// </summary>
    Task NotifyPlayerRollAsync(Guid campaignId, string characterId, string checkType, int result, string outcome, CancellationToken ct = default);
    
    // === Combat Events ===
    
    /// <summary>
    /// Notify all clients when combat starts
    /// </summary>
    Task NotifyCombatStartedAsync(Guid campaignId, CombatStatePayload payload, CancellationToken ct = default);
    
    /// <summary>
    /// Notify all clients when combat ends
    /// </summary>
    Task NotifyCombatEndedAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Notify all clients when turn advances (includes round number for UI update)
    /// </summary>
    Task NotifyTurnAdvancedAsync(Guid campaignId, int newTurnIndex, string currentCombatantId, int roundNumber, CancellationToken ct = default);
    
    /// <summary>
    /// Notify when initiative is set for a combatant
    /// </summary>
    Task NotifyInitiativeSetAsync(Guid campaignId, string characterId, int initiative, CancellationToken ct = default);
}
