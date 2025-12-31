using Microsoft.AspNetCore.SignalR;
using Riddle.Web.Hubs;

namespace Riddle.Web.Services;

/// <summary>
/// Service for broadcasting events via SignalR to campaign participants
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<GameHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    // === SignalR Group Names ===
    private static string DmGroup(Guid campaignId) => $"campaign_{campaignId}_dm";
    private static string PlayersGroup(Guid campaignId) => $"campaign_{campaignId}_players";
    private static string AllGroup(Guid campaignId) => $"campaign_{campaignId}_all";

    // === Character Events ===

    public async Task NotifyCharacterClaimedAsync(Guid campaignId, CharacterClaimPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting CharacterClaimed: {CharacterId} ({CharacterName}) claimed by {PlayerName}",
            payload.CharacterId, payload.CharacterName, payload.PlayerName);

        // Notify DM of character claim
        await _hubContext.Clients
            .Group(DmGroup(campaignId))
            .SendAsync(GameHubEvents.CharacterClaimed, payload, ct);
    }

    public async Task NotifyCharacterReleasedAsync(Guid campaignId, CharacterClaimPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting CharacterReleased: {CharacterId} ({CharacterName}) released",
            payload.CharacterId, payload.CharacterName);

        // Notify DM of character release
        await _hubContext.Clients
            .Group(DmGroup(campaignId))
            .SendAsync(GameHubEvents.CharacterReleased, payload, ct);
    }

    // === Player Connection Events ===

    public async Task NotifyPlayerConnectedAsync(Guid campaignId, PlayerConnectionPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting PlayerConnected: {PlayerName} (Character: {CharacterName}) joined campaign {CampaignId}",
            payload.PlayerName, payload.CharacterName, campaignId);

        // Notify DM when a player connects
        await _hubContext.Clients
            .Group(DmGroup(campaignId))
            .SendAsync(GameHubEvents.PlayerConnected, payload, ct);
    }

    public async Task NotifyPlayerDisconnectedAsync(Guid campaignId, PlayerConnectionPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting PlayerDisconnected: {PlayerName} (Character: {CharacterName}) left campaign {CampaignId}",
            payload.PlayerName, payload.CharacterName, campaignId);

        // Notify DM when a player disconnects
        await _hubContext.Clients
            .Group(DmGroup(campaignId))
            .SendAsync(GameHubEvents.PlayerDisconnected, payload, ct);
    }

    // === Game State Events ===

    public async Task NotifyCharacterStateUpdatedAsync(Guid campaignId, CharacterStatePayload payload, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Broadcasting CharacterStateUpdated: {CharacterId} - {Key} = {Value}",
            payload.CharacterId, payload.Key, payload.Value);

        // Notify all participants (DM + players) of character state changes
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.CharacterStateUpdated, payload, ct);
    }

    public async Task NotifyReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Broadcasting ReadAloudText to campaign {CampaignId}: {TextPreview}...",
            campaignId, text.Length > 50 ? text[..50] : text);

        // Notify DM dashboard of read aloud text
        await _hubContext.Clients
            .Group(DmGroup(campaignId))
            .SendAsync(GameHubEvents.ReadAloudTextReceived, text, ct);
    }

    public async Task NotifySceneImageAsync(Guid campaignId, string imageUri, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Broadcasting SceneImage to campaign {CampaignId}: {ImageUri}",
            campaignId, imageUri);

        // Notify all participants of scene image change
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.SceneImageUpdated, imageUri, ct);
    }

    public async Task NotifyPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting PlayerChoices to campaign {CampaignId}: {Choices}",
            campaignId, string.Join(", ", choices));

        // Notify players only - they need to make a choice
        await _hubContext.Clients
            .Group(PlayersGroup(campaignId))
            .SendAsync(GameHubEvents.PlayerChoicesReceived, choices, ct);
    }

    public async Task NotifyPlayerChoiceSubmittedAsync(Guid campaignId, PlayerChoicePayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting PlayerChoiceSubmitted to DM: {CharacterName} chose {Choice}",
            payload.CharacterName, payload.Choice);

        // Notify DM when a player submits their choice
        await _hubContext.Clients
            .Group(DmGroup(campaignId))
            .SendAsync(GameHubEvents.PlayerChoiceSubmitted, payload, ct);
    }

    public async Task NotifyPlayerRollAsync(Guid campaignId, string characterId, string checkType, int result, string outcome, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting PlayerRoll: Character {CharacterId} rolled {CheckType}: {Result} ({Outcome})",
            characterId, checkType, result, outcome);

        // Notify all participants of dice roll
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.PlayerRollLogged, new { characterId, checkType, result, outcome }, ct);
    }
    
    public async Task NotifyPlayerRollAsync(Guid campaignId, Models.RollResult roll, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting PlayerRoll: {CharacterName} rolled {CheckType}: {Result} ({Outcome})",
            roll.CharacterName, roll.CheckType, roll.Result, roll.Outcome);

        // Notify all participants of dice roll with full roll details
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.PlayerRollLogged, new 
            { 
                id = roll.Id,
                characterId = roll.CharacterId,
                characterName = roll.CharacterName,
                checkType = roll.CheckType, 
                result = roll.Result, 
                outcome = roll.Outcome,
                timestamp = roll.Timestamp
            }, ct);
    }

    // === Combat Events ===

    public async Task NotifyCombatStartedAsync(Guid campaignId, CombatStatePayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting CombatStarted to campaign {CampaignId}: Round {Round}, {CombatantCount} combatants",
            campaignId, payload.RoundNumber, payload.TurnOrder.Count);

        // Notify all participants when combat starts
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.CombatStarted, payload, ct);
    }

    public async Task NotifyCombatEndedAsync(Guid campaignId, CancellationToken ct = default)
    {
        _logger.LogInformation("Broadcasting CombatEnded to campaign {CampaignId}", campaignId);

        // Notify all participants when combat ends
        // Note: No payload - client handler expects parameterless invocation
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.CombatEnded, cancellationToken: ct);
    }

    public async Task NotifyTurnAdvancedAsync(Guid campaignId, TurnAdvancedPayload payload, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Broadcasting TurnAdvanced to campaign {CampaignId}: Round {RoundNumber}, Turn {TurnIndex}, Combatant {CombatantId}",
            campaignId, payload.RoundNumber, payload.NewTurnIndex, payload.CurrentCombatantId);

        // Notify all participants of turn advancement
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.TurnAdvanced, payload, ct);
    }

    public async Task NotifyInitiativeSetAsync(Guid campaignId, InitiativeSetPayload payload, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Broadcasting InitiativeSet to campaign {CampaignId}: Character {CharacterId} = {Initiative}",
            campaignId, payload.CharacterId, payload.Initiative);

        // Notify all participants of initiative changes
        await _hubContext.Clients
            .Group(AllGroup(campaignId))
            .SendAsync(GameHubEvents.InitiativeSet, payload, ct);
    }

    // === Atmospheric Events (Players Only) ===

    public async Task NotifyAtmospherePulseAsync(Guid campaignId, AtmospherePulsePayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting AtmospherePulse to campaign {CampaignId}: {Text} (Intensity: {Intensity}, Type: {SensoryType})",
            campaignId, payload.Text.Length > 50 ? payload.Text[..50] + "..." : payload.Text,
            payload.Intensity ?? "default", payload.SensoryType ?? "general");

        // Notify players only - fleeting sensory feedback
        await _hubContext.Clients
            .Group(PlayersGroup(campaignId))
            .SendAsync(GameHubEvents.AtmospherePulseReceived, payload, ct);
    }

    public async Task NotifyNarrativeAnchorAsync(Guid campaignId, NarrativeAnchorPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting NarrativeAnchor to campaign {CampaignId}: {ShortText} (Mood: {MoodCategory})",
            campaignId, payload.ShortText, payload.MoodCategory ?? "neutral");

        // Notify players only - persistent banner update
        await _hubContext.Clients
            .Group(PlayersGroup(campaignId))
            .SendAsync(GameHubEvents.NarrativeAnchorUpdated, payload, ct);
    }

    public async Task NotifyGroupInsightAsync(Guid campaignId, GroupInsightPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Broadcasting GroupInsight to campaign {CampaignId}: {Text} (Skill: {RelevantSkill}, Highlight: {Highlight})",
            campaignId, payload.Text.Length > 50 ? payload.Text[..50] + "..." : payload.Text,
            payload.RelevantSkill, payload.HighlightEffect);

        // Notify players only - flash notification for discoveries
        await _hubContext.Clients
            .Group(PlayersGroup(campaignId))
            .SendAsync(GameHubEvents.GroupInsightTriggered, payload, ct);
    }
}
