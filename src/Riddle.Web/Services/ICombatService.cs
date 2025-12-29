using Riddle.Web.Hubs;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for combat management operations
/// </summary>
public interface ICombatService
{
    /// <summary>
    /// Start a new combat encounter
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="combatants">List of combatants with their initiative</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created combat encounter state</returns>
    Task<CombatStatePayload> StartCombatAsync(Guid campaignId, List<CombatantInfo> combatants, CancellationToken ct = default);
    
    /// <summary>
    /// Set initiative for a combatant
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="characterId">Character ID</param>
    /// <param name="initiative">Initiative roll value</param>
    /// <param name="ct">Cancellation token</param>
    Task SetInitiativeAsync(Guid campaignId, string characterId, int initiative, CancellationToken ct = default);
    
    /// <summary>
    /// Advance to the next turn
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of new turn index and current combatant ID</returns>
    Task<(int NewTurnIndex, string CurrentCombatantId)> AdvanceTurnAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Mark a combatant as defeated
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="characterId">Character ID to mark defeated</param>
    /// <param name="ct">Cancellation token</param>
    Task MarkDefeatedAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    
    /// <summary>
    /// End the current combat
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="ct">Cancellation token</param>
    Task EndCombatAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Get current combat state
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current combat state or null if no active combat</returns>
    Task<CombatStatePayload?> GetCombatStateAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Add a combatant to an existing combat
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="combatant">Combatant to add</param>
    /// <param name="ct">Cancellation token</param>
    Task AddCombatantAsync(Guid campaignId, CombatantInfo combatant, CancellationToken ct = default);
    
    /// <summary>
    /// Remove a combatant from combat (flee, removed, etc.)
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="characterId">Character ID to remove</param>
    /// <param name="ct">Cancellation token</param>
    Task RemoveCombatantAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    
    /// <summary>
    /// Update combatant HP
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <param name="characterId">Character ID</param>
    /// <param name="newHp">New HP value</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateCombatantHpAsync(Guid campaignId, string characterId, int newHp, CancellationToken ct = default);
}
