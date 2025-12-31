using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Hubs;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing combat encounters with real-time SignalR notifications.
/// Combatant details are persisted in CombatEncounter.Combatants (survives server restart).
/// </summary>
public class CombatService : ICombatService
{
    private readonly RiddleDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CombatService> _logger;

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

        // Create new combat encounter with persisted combatant details
        var combat = new CombatEncounter
        {
            IsActive = true,
            RoundNumber = 1,
            CurrentTurnIndex = 0,
            TurnOrder = combatants
                .OrderByDescending(c => c.Initiative)
                .Select(c => c.Id)
                .ToList(),
            // Persist combatant details to DB (survives server restart)
            Combatants = combatants.ToDictionary(c => c.Id, c => new CombatantDetails
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Initiative = c.Initiative,
                CurrentHp = c.CurrentHp,
                MaxHp = c.MaxHp,
                IsDefeated = c.IsDefeated
            })
        };

        // Persist to database
        campaign.ActiveCombat = combat;
        campaign.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Build payload
        var payload = BuildCombatStatePayload(combat);

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

        // Update initiative in persisted combatants
        if (combat.Combatants.TryGetValue(characterId, out var combatant))
        {
            combatant.Initiative = initiative;
            
            // Re-sort turn order by initiative
            combat.TurnOrder = combat.Combatants.Values
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

        await _notificationService.NotifyInitiativeSetAsync(campaignId, new InitiativeSetPayload(characterId, initiative), ct);
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

        await _notificationService.NotifyTurnAdvancedAsync(campaignId, new TurnAdvancedPayload(combat.CurrentTurnIndex, currentCombatantId, combat.RoundNumber), ct);

        return (combat.CurrentTurnIndex, currentCombatantId);
    }

    public async Task MarkDefeatedAsync(Guid campaignId, string characterId, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        // Update persisted combatant
        if (combat.Combatants.TryGetValue(characterId, out var combatant))
        {
            combatant.IsDefeated = true;
            combatant.CurrentHp = 0;
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
        var activeEnemies = combat.Combatants.Values
            .Where(c => c.Type == "Enemy" && !c.IsDefeated)
            .ToList();
        
        if (!activeEnemies.Any())
        {
            _logger.LogInformation("All enemies defeated, ending combat");
            await EndCombatAsync(campaignId, ct);
            return;
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

        return BuildCombatStatePayload(campaign.ActiveCombat);
    }

    public async Task AddCombatantAsync(Guid campaignId, CombatantInfo combatant, CancellationToken ct = default)
    {
        var campaign = await _context.CampaignInstances.FindAsync([campaignId], ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var combat = campaign.ActiveCombat
            ?? throw new InvalidOperationException("No active combat");

        // Add to persisted combatants
        combat.Combatants[combatant.Id] = new CombatantDetails
        {
            Id = combatant.Id,
            Name = combatant.Name,
            Type = combatant.Type,
            Initiative = combatant.Initiative,
            CurrentHp = combatant.CurrentHp,
            MaxHp = combatant.MaxHp,
            IsDefeated = combatant.IsDefeated
        };

        // Insert into turn order based on initiative
        var insertIndex = 0;
        for (var i = 0; i < combat.TurnOrder.Count; i++)
        {
            if (combat.Combatants.TryGetValue(combat.TurnOrder[i], out var existing) && 
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

        // Remove from persisted combatants
        combat.Combatants.Remove(characterId);

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

        // Update persisted combatant
        if (combat.Combatants.TryGetValue(characterId, out var combatant))
        {
            var wasDefeated = combatant.IsDefeated;
            var isNowDefeated = newHp <= 0;
            
            combatant.CurrentHp = Math.Max(0, newHp);
            combatant.IsDefeated = isNowDefeated;

            campaign.ActiveCombat = combat;
            campaign.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            // If just became defeated, handle removal from turn order
            if (!wasDefeated && isNowDefeated)
            {
                await MarkDefeatedAsync(campaignId, characterId, ct);
                return;
            }
        }

        _logger.LogInformation(
            "Combatant HP updated: CampaignId={CampaignId}, CharacterId={CharacterId}, NewHp={NewHp}",
            campaignId, characterId, newHp);

        // Broadcast character state update
        await _notificationService.NotifyCharacterStateUpdatedAsync(
            campaignId,
            new CharacterStatePayload(characterId, "CurrentHp", newHp),
            ct);
    }

    /// <summary>
    /// Builds CombatStatePayload from persisted CombatEncounter data
    /// </summary>
    private static CombatStatePayload BuildCombatStatePayload(CombatEncounter combat)
    {
        // Convert persisted CombatantDetails to CombatantInfo in turn order
        var combatants = combat.TurnOrder
            .Where(id => combat.Combatants.ContainsKey(id))
            .Select(id =>
            {
                var c = combat.Combatants[id];
                return new CombatantInfo(
                    c.Id,
                    c.Name,
                    c.Type,
                    c.Initiative,
                    c.CurrentHp,
                    c.MaxHp,
                    c.IsDefeated,
                    combat.SurprisedEntities.Contains(c.Id)
                );
            })
            .ToList();

        return new CombatStatePayload(
            combat.Id,
            combat.IsActive,
            combat.RoundNumber,
            combatants,
            combat.CurrentTurnIndex
        );
    }
}
