using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Hubs;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing combat encounters with real-time SignalR notifications
/// </summary>
public class CombatService : ICombatService
{
    private readonly RiddleDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CombatService> _logger;
    
    // In-memory cache of combatant details (since CombatEncounter only stores IDs)
    // Key: CombatId, Value: Dictionary of characterId -> CombatantInfo
    private static readonly Dictionary<string, Dictionary<string, CombatantInfo>> _combatantCache = new();

    public CombatService(
        RiddleDbContext context,
        INotificationService notificationService,
        ILogger<CombatService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<CombatStatePayload> StartCombatAsync(Guid campaignId, List<CombatantInfo> combatants, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        // Create new combat encounter
        var combat = new CombatEncounter
        {
            IsActive = true,
            RoundNumber = 1,
            CurrentTurnIndex = 0,
            TurnOrder = combatants
                .OrderByDescending(c => c.Initiative)
                .Select(c => c.Id)
                .ToList()
        };

        // Store combatant details in cache
        _combatantCache[combat.Id] = combatants.ToDictionary(c => c.Id);

        // Persist to database
        campaign.ActiveCombat = combat;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Build payload
        var payload = BuildCombatStatePayload(combat, combatants.OrderByDescending(c => c.Initiative).ToList());

        _logger.LogInformation(
            "Combat started: CampaignId={CampaignId}, CombatId={CombatId}, Combatants={Count}",
            campaignId, combat.Id, combatants.Count);

        // Broadcast to all clients
        await _notificationService.NotifyCombatStartedAsync(campaignId, payload, ct);

        return payload;
    }

    public async Task SetInitiativeAsync(Guid campaignId, string characterId, int initiative, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        // Update initiative in cache if exists
        if (_combatantCache.TryGetValue(combat.Id, out var combatants) &&
            combatants.TryGetValue(characterId, out var existingCombatant))
        {
            combatants[characterId] = existingCombatant with { Initiative = initiative };
            
            // Re-sort turn order by initiative
            combat.TurnOrder = combatants.Values
                .Where(c => !c.IsDefeated)
                .OrderByDescending(c => c.Initiative)
                .Select(c => c.Id)
                .ToList();
        }

        campaign.ActiveCombat = combat;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Initiative set: CampaignId={CampaignId}, CharacterId={CharacterId}, Initiative={Initiative}",
            campaignId, characterId, initiative);

        await _notificationService.NotifyInitiativeSetAsync(campaignId, characterId, initiative, ct);
    }

    public async Task<(int NewTurnIndex, string CurrentCombatantId)> AdvanceTurnAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        if (combat.TurnOrder.Count == 0)
            throw new InvalidOperationException("No combatants in turn order");

        // Advance turn
        combat.CurrentTurnIndex++;
        
        // Check if we've completed a round
        if (combat.CurrentTurnIndex >= combat.TurnOrder.Count)
        {
            combat.CurrentTurnIndex = 0;
            combat.RoundNumber++;
            
            // Clear surprised status after first round
            if (combat.RoundNumber == 2)
            {
                combat.SurprisedEntities.Clear();
            }
        }

        var currentCombatantId = combat.TurnOrder[combat.CurrentTurnIndex];

        campaign.ActiveCombat = combat;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Turn advanced: CampaignId={CampaignId}, Round={Round}, TurnIndex={TurnIndex}, CurrentCombatant={CombatantId}",
            campaignId, combat.RoundNumber, combat.CurrentTurnIndex, currentCombatantId);

        await _notificationService.NotifyTurnAdvancedAsync(campaignId, combat.CurrentTurnIndex, currentCombatantId, ct);

        return (combat.CurrentTurnIndex, currentCombatantId);
    }

    public async Task MarkDefeatedAsync(Guid campaignId, string characterId, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        // Update cache
        if (_combatantCache.TryGetValue(combat.Id, out var combatants) &&
            combatants.TryGetValue(characterId, out var combatant))
        {
            combatants[characterId] = combatant with { IsDefeated = true, CurrentHp = 0 };
        }

        // Remove from turn order
        var currentIndex = combat.CurrentTurnIndex;
        var defeatedIndex = combat.TurnOrder.IndexOf(characterId);
        
        if (defeatedIndex >= 0)
        {
            combat.TurnOrder.RemoveAt(defeatedIndex);
            
            // Adjust current turn index if needed
            if (defeatedIndex < currentIndex)
            {
                combat.CurrentTurnIndex--;
            }
            else if (defeatedIndex == currentIndex && combat.CurrentTurnIndex >= combat.TurnOrder.Count)
            {
                combat.CurrentTurnIndex = 0;
            }
        }

        campaign.ActiveCombat = combat;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Combatant defeated: CampaignId={CampaignId}, CharacterId={CharacterId}",
            campaignId, characterId);

        // Check if all enemies are defeated
        if (combatants != null)
        {
            var activeEnemies = combatants.Values
                .Where(c => c.Type == "Enemy" && !c.IsDefeated)
                .ToList();
            
            if (!activeEnemies.Any())
            {
                _logger.LogInformation("All enemies defeated, ending combat");
                await EndCombatAsync(campaignId, ct);
                return;
            }
        }

        // Broadcast updated combat state
        var payload = await GetCombatStateAsync(campaignId, ct);
        if (payload != null)
        {
            await _notificationService.NotifyCombatStartedAsync(campaignId, payload, ct);
        }
    }

    public async Task EndCombatAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat;
        if (combat != null && _combatantCache.ContainsKey(combat.Id))
        {
            _combatantCache.Remove(combat.Id);
        }

        campaign.ActiveCombat = null;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Combat ended: CampaignId={CampaignId}", campaignId);

        await _notificationService.NotifyCombatEndedAsync(campaignId, ct);
    }

    public async Task<CombatStatePayload?> GetCombatStateAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);

        if (campaign?.ActiveCombat == null)
            return null;

        var combat = campaign.ActiveCombat;
        
        // Get combatant details from cache or build from party state
        List<CombatantInfo> combatants;
        if (_combatantCache.TryGetValue(combat.Id, out var cachedCombatants))
        {
            combatants = combat.TurnOrder
                .Where(id => cachedCombatants.ContainsKey(id))
                .Select(id => cachedCombatants[id])
                .ToList();
        }
        else
        {
            // Rebuild from party state if cache was lost (e.g., server restart)
            var partyState = campaign.PartyState;
            combatants = combat.TurnOrder
                .Select(id =>
                {
                    var character = partyState.FirstOrDefault(c => c.Id == id);
                    if (character != null)
                    {
                        return new CombatantInfo(
                            character.Id,
                            character.Name,
                            "PC",
                            0, // Initiative unknown
                            character.CurrentHp,
                            character.MaxHp,
                            character.CurrentHp <= 0,
                            combat.SurprisedEntities.Contains(id)
                        );
                    }
                    // Unknown combatant (likely enemy), create placeholder
                    return new CombatantInfo(id, id, "Enemy", 0, 1, 1, false, combat.SurprisedEntities.Contains(id));
                })
                .ToList();
        }

        return BuildCombatStatePayload(combat, combatants);
    }

    public async Task AddCombatantAsync(Guid campaignId, CombatantInfo combatant, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        // Add to cache
        if (!_combatantCache.TryGetValue(combat.Id, out var combatants))
        {
            combatants = new Dictionary<string, CombatantInfo>();
            _combatantCache[combat.Id] = combatants;
        }
        combatants[combatant.Id] = combatant;

        // Insert into turn order based on initiative
        var insertIndex = 0;
        for (var i = 0; i < combat.TurnOrder.Count; i++)
        {
            if (combatants.TryGetValue(combat.TurnOrder[i], out var existing) && 
                existing.Initiative < combatant.Initiative)
            {
                insertIndex = i;
                break;
            }
            insertIndex = i + 1;
        }
        combat.TurnOrder.Insert(insertIndex, combatant.Id);

        campaign.ActiveCombat = combat;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Combatant added: CampaignId={CampaignId}, CombatantId={CombatantId}, Name={Name}",
            campaignId, combatant.Id, combatant.Name);

        // Broadcast updated state
        var payload = await GetCombatStateAsync(campaignId, ct);
        if (payload != null)
        {
            await _notificationService.NotifyCombatStartedAsync(campaignId, payload, ct);
        }
    }

    public async Task RemoveCombatantAsync(Guid campaignId, string characterId, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        // Remove from cache
        if (_combatantCache.TryGetValue(combat.Id, out var combatants))
        {
            combatants.Remove(characterId);
        }

        // Remove from turn order
        var currentIndex = combat.CurrentTurnIndex;
        var removedIndex = combat.TurnOrder.IndexOf(characterId);
        
        if (removedIndex >= 0)
        {
            combat.TurnOrder.RemoveAt(removedIndex);
            
            if (removedIndex < currentIndex)
            {
                combat.CurrentTurnIndex--;
            }
            else if (removedIndex == currentIndex && combat.CurrentTurnIndex >= combat.TurnOrder.Count)
            {
                combat.CurrentTurnIndex = combat.TurnOrder.Count > 0 ? 0 : -1;
            }
        }

        campaign.ActiveCombat = combat;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Combatant removed: CampaignId={CampaignId}, CharacterId={CharacterId}",
            campaignId, characterId);

        // Broadcast updated state
        var payload = await GetCombatStateAsync(campaignId, ct);
        if (payload != null)
        {
            await _notificationService.NotifyCombatStartedAsync(campaignId, payload, ct);
        }
    }

    public async Task UpdateCombatantHpAsync(Guid campaignId, string characterId, int newHp, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        // Update cache
        if (_combatantCache.TryGetValue(combat.Id, out var combatants) &&
            combatants.TryGetValue(characterId, out var combatant))
        {
            var wasDefeated = combatant.IsDefeated;
            var isNowDefeated = newHp <= 0;
            
            combatants[characterId] = combatant with 
            { 
                CurrentHp = Math.Max(0, newHp),
                IsDefeated = isNowDefeated 
            };

            // If just became defeated, handle removal from turn order
            if (!wasDefeated && isNowDefeated)
            {
                await MarkDefeatedAsync(campaignId, characterId, ct);
                return;
            }
        }

        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Combatant HP updated: CampaignId={CampaignId}, CharacterId={CharacterId}, NewHp={NewHp}",
            campaignId, characterId, newHp);

        // Broadcast character state update
        await _notificationService.NotifyCharacterStateUpdatedAsync(
            campaignId,
            new CharacterStatePayload(characterId, "CurrentHp", newHp),
            ct);
    }

    private static CombatStatePayload BuildCombatStatePayload(CombatEncounter combat, List<CombatantInfo> combatants)
    {
        return new CombatStatePayload(
            combat.Id,
            combat.IsActive,
            combat.RoundNumber,
            combatants,
            combat.CurrentTurnIndex
        );
    }
}
