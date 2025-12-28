using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing CampaignInstance CRUD operations
/// </summary>
public interface ICampaignService
{
    /// <summary>
    /// Get all campaigns for a specific user
    /// </summary>
    Task<List<CampaignInstance>> GetCampaignsForUserAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get a campaign by ID
    /// </summary>
    Task<CampaignInstance?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Create a new campaign
    /// </summary>
    Task<CampaignInstance> CreateCampaignAsync(string userId, string name, string campaignModule, CancellationToken ct = default);
    
    /// <summary>
    /// Update an existing campaign
    /// </summary>
    Task<CampaignInstance> UpdateCampaignAsync(CampaignInstance campaign, CancellationToken ct = default);
    
    /// <summary>
    /// Delete a campaign
    /// </summary>
    Task DeleteCampaignAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Get count of campaigns for a user
    /// </summary>
    Task<int> GetCampaignCountAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get total character count across all campaigns for a user
    /// </summary>
    Task<int> GetCharacterCountAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get a campaign by its invite code
    /// </summary>
    Task<CampaignInstance?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default);
    
    /// <summary>
    /// Regenerate the invite code for a campaign
    /// </summary>
    /// <returns>The new invite code</returns>
    Task<string> RegenerateInviteCodeAsync(Guid campaignId, CancellationToken ct = default);
}
