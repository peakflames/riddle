using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Event args for campaign state changes
/// </summary>
public class CampaignChangedEventArgs : EventArgs
{
    public required Guid CampaignId { get; init; }
    public required string ChangedProperty { get; init; }
    public object? NewValue { get; init; }
}

/// <summary>
/// Service for game state operations used by LLM tools.
/// Provides a simplified interface for reading and updating campaign state.
/// </summary>
public interface IGameStateService
{
    /// <summary>
    /// Event fired when campaign state changes (for real-time UI updates)
    /// </summary>
    event Action<CampaignChangedEventArgs>? OnCampaignChanged;

    /// <summary>
    /// Get a campaign by ID with all related data
    /// </summary>
    Task<CampaignInstance?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Update a campaign's state
    /// </summary>
    Task<CampaignInstance> UpdateCampaignAsync(CampaignInstance campaign, CancellationToken ct = default);
    
    /// <summary>
    /// Get a specific character from a campaign's party
    /// </summary>
    Task<Character?> GetCharacterAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    
    /// <summary>
    /// Update a character's state within a campaign
    /// </summary>
    Task UpdateCharacterAsync(Guid campaignId, Character character, CancellationToken ct = default);
    
    /// <summary>
    /// Add a log entry to the campaign's narrative log
    /// </summary>
    Task AddLogEntryAsync(Guid campaignId, LogEntry entry, CancellationToken ct = default);
    
    /// <summary>
    /// Update the campaign's read-aloud text
    /// </summary>
    Task SetReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default);
    
    /// <summary>
    /// Update the player choices displayed
    /// </summary>
    Task SetPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default);
    
    /// <summary>
    /// Update the scene image URI
    /// </summary>
    Task SetSceneImageAsync(Guid campaignId, string imageUri, CancellationToken ct = default);
    
    /// <summary>
    /// Add a dice roll result to the campaign's recent rolls
    /// </summary>
    Task AddRollResultAsync(Guid campaignId, RollResult roll, CancellationToken ct = default);
}
