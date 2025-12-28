using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for character operations including claiming characters via invite links
/// </summary>
public interface ICharacterService
{
    /// <summary>
    /// Gets available (unclaimed) characters in a campaign for a player to claim
    /// </summary>
    Task<List<Character>> GetAvailableCharactersAsync(Guid campaignId);
    
    /// <summary>
    /// Claims a character for a player by assigning their PlayerId
    /// </summary>
    Task<bool> ClaimCharacterAsync(Guid campaignId, string characterId, string playerId, string playerName);
    
    /// <summary>
    /// Gets all characters claimed by a specific player in a campaign
    /// </summary>
    Task<List<Character>> GetPlayerCharactersAsync(Guid campaignId, string playerId);
    
    /// <summary>
    /// Validates an invite code and returns the campaign if valid
    /// </summary>
    Task<CampaignInstance?> ValidateInviteCodeAsync(string inviteCode);
    
    /// <summary>
    /// Unclaims a character, releasing it for another player to claim.
    /// Used by DM to manage party composition.
    /// </summary>
    Task<bool> UnclaimCharacterAsync(Guid campaignId, string characterId);
}
