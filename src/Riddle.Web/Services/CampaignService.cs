using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing CampaignInstance CRUD operations
/// </summary>
public class CampaignService : ICampaignService
{
    private readonly RiddleDbContext _dbContext;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(RiddleDbContext dbContext, ILogger<CampaignService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<CampaignInstance>> GetCampaignsForUserAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting campaigns for user {UserId}", userId);
        
        return await _dbContext.CampaignInstances
            .Where(c => c.DmUserId == userId)
            .OrderByDescending(c => c.LastActivityAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<CampaignInstance?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting campaign {CampaignId}", campaignId);
        
        return await _dbContext.CampaignInstances
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);
    }

    /// <inheritdoc/>
    public async Task<CampaignInstance> CreateCampaignAsync(string userId, string name, string campaignModule, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new campaign '{Name}' ({Module}) for user {UserId}", name, campaignModule, userId);
        
        var campaign = new CampaignInstance
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            CampaignModule = campaignModule,
            DmUserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _dbContext.CampaignInstances.Add(campaign);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created campaign {CampaignId}", campaign.Id);
        
        return campaign;
    }

    /// <inheritdoc/>
    public async Task<CampaignInstance> UpdateCampaignAsync(CampaignInstance campaign, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating campaign {CampaignId}", campaign.Id);
        
        campaign.LastActivityAt = DateTime.UtcNow;
        _dbContext.CampaignInstances.Update(campaign);
        await _dbContext.SaveChangesAsync(ct);
        
        return campaign;
    }

    /// <inheritdoc/>
    public async Task DeleteCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting campaign {CampaignId}", campaignId);
        
        var campaign = await _dbContext.CampaignInstances
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        
        if (campaign != null)
        {
            _dbContext.CampaignInstances.Remove(campaign);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetCampaignCountAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.CampaignInstances
            .CountAsync(c => c.DmUserId == userId, ct);
    }

    /// <inheritdoc/>
    public async Task<int> GetCharacterCountAsync(string userId, CancellationToken ct = default)
    {
        var campaigns = await _dbContext.CampaignInstances
            .Where(c => c.DmUserId == userId)
            .ToListAsync(ct);
        
        return campaigns.Sum(c => c.PartyState.Count);
    }
    
    /// <inheritdoc/>
    public async Task<CampaignInstance?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return null;
        
        // Normalize to uppercase for case-insensitive matching
        var normalizedCode = inviteCode.Trim().ToUpperInvariant();
        
        _logger.LogDebug("Looking up campaign by invite code {InviteCode}", normalizedCode);
        
        return await _dbContext.CampaignInstances
            .FirstOrDefaultAsync(c => c.InviteCode == normalizedCode, ct);
    }
    
    /// <inheritdoc/>
    public async Task<string> RegenerateInviteCodeAsync(Guid campaignId, CancellationToken ct = default)
    {
        _logger.LogInformation("Regenerating invite code for campaign {CampaignId}", campaignId);
        
        var campaign = await _dbContext.CampaignInstances
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }
        
        campaign.RegenerateInviteCode();
        campaign.LastActivityAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Regenerated invite code for campaign {CampaignId}: {InviteCode}", campaignId, campaign.InviteCode);
        
        return campaign.InviteCode;
    }
}
