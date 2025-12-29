using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Hubs;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

public class CharacterService : ICharacterService
{
    private readonly RiddleDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        RiddleDbContext context, 
        INotificationService notificationService,
        ILogger<CharacterService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<List<Character>> GetAvailableCharactersAsync(Guid campaignId)
    {
        var campaign = await _context.CampaignInstances.FindAsync(campaignId);
        if (campaign == null) return [];

        // Return PCs that haven't been claimed (no PlayerId)
        return campaign.PartyState
            .Where(c => c.Type == "PC" && string.IsNullOrEmpty(c.PlayerId))
            .ToList();
    }

    public async Task<bool> ClaimCharacterAsync(Guid campaignId, string characterId, string playerId, string playerName)
    {
        var campaign = await _context.CampaignInstances.FindAsync(campaignId);
        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found for character claim", campaignId);
            return false;
        }

        // CRITICAL: Get the list ONCE and hold reference - each access to PartyState
        // deserializes JSON fresh, so modifications to a previous access are lost!
        var partyState = campaign.PartyState;
        
        var character = partyState.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
        {
            _logger.LogWarning("Character {CharacterId} not found in campaign {CampaignId}", characterId, campaignId);
            return false;
        }

        // Check if already claimed by someone else
        if (!string.IsNullOrEmpty(character.PlayerId) && character.PlayerId != playerId)
        {
            _logger.LogWarning("Character {CharacterId} already claimed by {ExistingPlayerId}", characterId, character.PlayerId);
            return false;
        }

        // Claim the character - this modifies the object in our local list
        character.PlayerId = playerId;
        character.PlayerName = playerName;
        
        // Set the modified list back to trigger JSON serialization via the setter
        campaign.PartyState = partyState;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Player {PlayerId} ({PlayerName}) claimed character {CharacterName} in campaign {CampaignId}",
            playerId, playerName, character.Name, campaignId);
        
        // Broadcast character claimed event via SignalR
        var payload = new CharacterClaimPayload(
            CharacterId: characterId,
            CharacterName: character.Name,
            PlayerId: playerId,
            PlayerName: playerName,
            IsClaimed: true
        );
        await _notificationService.NotifyCharacterClaimedAsync(campaignId, payload);
        
        return true;
    }

    public async Task<List<Character>> GetPlayerCharactersAsync(Guid campaignId, string playerId)
    {
        var campaign = await _context.CampaignInstances.FindAsync(campaignId);
        if (campaign == null) return [];

        return campaign.PartyState
            .Where(c => c.PlayerId == playerId)
            .ToList();
    }

    public async Task<CampaignInstance?> ValidateInviteCodeAsync(string inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return null;

        return await _context.CampaignInstances
            .FirstOrDefaultAsync(c => c.InviteCode == inviteCode);
    }

    public async Task<bool> UnclaimCharacterAsync(Guid campaignId, string characterId)
    {
        var campaign = await _context.CampaignInstances.FindAsync(campaignId);
        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found for character unclaim", campaignId);
            return false;
        }

        // CRITICAL: Get the list ONCE and hold reference - each access to PartyState
        // deserializes JSON fresh, so modifications to a previous access are lost!
        var partyState = campaign.PartyState;
        
        var character = partyState.FirstOrDefault(c => c.Id == characterId);
        if (character == null)
        {
            _logger.LogWarning("Character {CharacterId} not found in campaign {CampaignId}", characterId, campaignId);
            return false;
        }

        var previousPlayer = character.PlayerName ?? character.PlayerId ?? "unknown";
        
        // Unclaim the character - clear player assignment
        character.PlayerId = null;
        character.PlayerName = null;
        
        // Set the modified list back to trigger JSON serialization via the setter
        campaign.PartyState = partyState;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("DM unclaimed character {CharacterName} (was {PreviousPlayer}) in campaign {CampaignId}",
            character.Name, previousPlayer, campaignId);
        
        // Broadcast character released event via SignalR
        var payload = new CharacterClaimPayload(
            CharacterId: characterId,
            CharacterName: character.Name,
            PlayerId: null,
            PlayerName: null,
            IsClaimed: false
        );
        await _notificationService.NotifyCharacterReleasedAsync(campaignId, payload);
        
        return true;
    }
}
