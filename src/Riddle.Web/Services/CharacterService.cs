using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

public class CharacterService : ICharacterService
{
    private readonly RiddleDbContext _context;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(RiddleDbContext context, ILogger<CharacterService> logger)
    {
        _context = context;
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

        var character = campaign.PartyState.FirstOrDefault(c => c.Id == characterId);
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

        // Claim the character
        character.PlayerId = playerId;
        character.PlayerName = playerName;
        
        // Save - the PartyState setter handles JSON serialization
        campaign.PartyState = campaign.PartyState;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Player {PlayerId} ({PlayerName}) claimed character {CharacterName} in campaign {CampaignId}",
            playerId, playerName, character.Name, campaignId);
        
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
}
