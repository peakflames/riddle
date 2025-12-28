using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Implementation of game state service for LLM tool operations.
/// Provides read/write access to campaign state for tool handlers.
/// </summary>
public class GameStateService : IGameStateService
{
    private readonly RiddleDbContext _dbContext;
    private readonly ILogger<GameStateService> _logger;

    public GameStateService(RiddleDbContext dbContext, ILogger<GameStateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CampaignInstance?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting campaign {CampaignId}", campaignId);
        return await _dbContext.CampaignInstances
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);
    }

    public async Task<CampaignInstance> UpdateCampaignAsync(CampaignInstance campaign, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating campaign {CampaignId}", campaign.Id);
        campaign.LastActivityAt = DateTime.UtcNow;
        _dbContext.CampaignInstances.Update(campaign);
        await _dbContext.SaveChangesAsync(ct);
        return campaign;
    }

    public async Task<Character?> GetCharacterAsync(Guid campaignId, string characterId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting character {CharacterId} from campaign {CampaignId}", characterId, campaignId);
        var campaign = await GetCampaignAsync(campaignId, ct);
        return campaign?.PartyState.FirstOrDefault(c => c.Id == characterId);
    }

    public async Task UpdateCharacterAsync(Guid campaignId, Character character, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating character {CharacterId} in campaign {CampaignId}", character.Id, campaignId);
        
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        var partyState = campaign.PartyState;
        var index = partyState.FindIndex(c => c.Id == character.Id);
        
        if (index >= 0)
        {
            partyState[index] = character;
            _logger.LogDebug("Updated existing character at index {Index}", index);
        }
        else
        {
            partyState.Add(character);
            _logger.LogDebug("Added new character to party");
        }
        
        campaign.PartyState = partyState;
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task AddLogEntryAsync(Guid campaignId, LogEntry entry, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding log entry to campaign {CampaignId}: {Entry}", campaignId, entry.Entry);
        
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        var log = campaign.NarrativeLog;
        log.Add(entry);
        campaign.NarrativeLog = log;
        
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task SetReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default)
    {
        _logger.LogDebug("Setting read-aloud text for campaign {CampaignId}", campaignId);
        
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.CurrentReadAloudText = text;
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task SetPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default)
    {
        _logger.LogDebug("Setting {Count} player choices for campaign {CampaignId}", choices.Count, campaignId);
        
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.ActivePlayerChoices = choices;
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task SetSceneImageAsync(Guid campaignId, string imageUri, CancellationToken ct = default)
    {
        _logger.LogDebug("Setting scene image for campaign {CampaignId}: {ImageUri}", campaignId, imageUri);
        
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.CurrentSceneImageUri = imageUri;
        await UpdateCampaignAsync(campaign, ct);
    }
}
