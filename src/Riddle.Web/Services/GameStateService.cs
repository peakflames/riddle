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

    public event Action<CampaignChangedEventArgs>? OnCampaignChanged;

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

    public async Task SetReadAloudTextAsync(Guid campaignId, string text, string? tone, string? pacing, CancellationToken ct = default)
    {
        _logger.LogDebug("Setting read-aloud text for campaign {CampaignId} (tone: {Tone}, pacing: {Pacing})", campaignId, tone ?? "none", pacing ?? "none");
        
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.CurrentReadAloudText = text;
        campaign.CurrentReadAloudTone = tone;
        campaign.CurrentReadAloudPacing = pacing;
        await UpdateCampaignAsync(campaign, ct);
        
        // Notify subscribers of the text change
        OnCampaignChanged?.Invoke(new CampaignChangedEventArgs
        {
            CampaignId = campaignId,
            ChangedProperty = "CurrentReadAloudText",
            NewValue = text
        });
        
        // Notify subscribers of the tone change
        OnCampaignChanged?.Invoke(new CampaignChangedEventArgs
        {
            CampaignId = campaignId,
            ChangedProperty = "CurrentReadAloudTone",
            NewValue = tone
        });
        
        // Notify subscribers of the pacing change
        OnCampaignChanged?.Invoke(new CampaignChangedEventArgs
        {
            CampaignId = campaignId,
            ChangedProperty = "CurrentReadAloudPacing",
            NewValue = pacing
        });
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
        
        // Notify subscribers of the change
        OnCampaignChanged?.Invoke(new CampaignChangedEventArgs
        {
            CampaignId = campaignId,
            ChangedProperty = "ActivePlayerChoices",
            NewValue = choices
        });
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
        
        // Notify subscribers of the change
        OnCampaignChanged?.Invoke(new CampaignChangedEventArgs
        {
            CampaignId = campaignId,
            ChangedProperty = "CurrentSceneImageUri",
            NewValue = imageUri
        });
    }

    public async Task AddRollResultAsync(Guid campaignId, RollResult roll, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding roll result for campaign {CampaignId}: {CharacterName} {CheckType} = {Result} ({Outcome})", 
            campaignId, roll.CharacterName, roll.CheckType, roll.Result, roll.Outcome);
        
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        // Get current rolls, add new one (with deduplication), keep only most recent 50
        var rolls = campaign.RecentRolls;
        
        // Dedupe: Skip if roll with same ID already exists
        if (!rolls.Any(r => r.Id == roll.Id))
        {
            rolls.Insert(0, roll);  // Add to front (most recent first)
        if (rolls.Count > 50)
        {
            rolls = rolls.Take(50).ToList();
        }
            campaign.RecentRolls = rolls;
        }
        else
        {
            _logger.LogDebug("Skipping duplicate roll with Id {RollId}", roll.Id);
            return; // Don't save or notify if duplicate
        }
        
        await UpdateCampaignAsync(campaign, ct);
        
        // Notify subscribers of the change
        OnCampaignChanged?.Invoke(new CampaignChangedEventArgs
        {
            CampaignId = campaignId,
            ChangedProperty = "RecentRolls",
            NewValue = roll  // Send just the new roll for efficiency
        });
    }
}
