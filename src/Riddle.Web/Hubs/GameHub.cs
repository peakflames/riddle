using Microsoft.AspNetCore.SignalR;
using Riddle.Web.Services;

namespace Riddle.Web.Hubs;

/// <summary>
/// Main SignalR hub for real-time game events
/// Handles campaign sessions, combat, player choices, and connection management
/// </summary>
public class GameHub : Hub
{
    private readonly IConnectionTracker _connectionTracker;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IConnectionTracker connectionTracker, ILogger<GameHub> logger)
    {
        _connectionTracker = connectionTracker;
        _logger = logger;
    }

    /// <summary>
    /// Join a campaign session as DM or Player
    /// Groups:
    /// - campaign_{id}_dm: DM only
    /// - campaign_{id}_players: All players
    /// - campaign_{id}_all: Everyone (DM + players)
    /// </summary>
    public async Task JoinCampaign(Guid campaignId, string userId, string? characterId, bool isDm)
    {
        var dmGroup = $"campaign_{campaignId}_dm";
        var playersGroup = $"campaign_{campaignId}_players";
        var allGroup = $"campaign_{campaignId}_all";
        
        // Add to appropriate groups
        if (isDm)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, dmGroup);
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, playersGroup);
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, allGroup);
        
        // Track connection
        _connectionTracker.AddConnection(Context.ConnectionId, campaignId, userId, characterId, isDm);
        
        _logger.LogInformation(
            "Client {ConnectionId} joined campaign {CampaignId} as {Role} (User: {UserId}, Character: {CharacterId})",
            Context.ConnectionId, campaignId, isDm ? "DM" : "Player", userId, characterId);
        
        // Notify DM of player connection (if player with character)
        if (!isDm && characterId != null)
        {
            await Clients.Group(dmGroup).SendAsync(
                GameHubEvents.PlayerConnected, 
                new PlayerConnectionPayload(
                    userId, 
                    Context.User?.Identity?.Name ?? "Unknown", 
                    characterId, 
                    null, 
                    true));
        }
    }

    /// <summary>
    /// Leave a campaign session
    /// </summary>
    public async Task LeaveCampaign(Guid campaignId)
    {
        var connectionInfo = _connectionTracker.GetConnectionInfo(Context.ConnectionId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign_{campaignId}_dm");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign_{campaignId}_players");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign_{campaignId}_all");
        
        // Notify DM if player leaving
        if (connectionInfo != null && !connectionInfo.IsDm && connectionInfo.CharacterId != null)
        {
            await Clients.Group($"campaign_{campaignId}_dm").SendAsync(
                GameHubEvents.PlayerDisconnected,
                new PlayerConnectionPayload(
                    connectionInfo.UserId,
                    connectionInfo.UserName,
                    connectionInfo.CharacterId,
                    connectionInfo.CharacterName,
                    false));
        }
        
        _connectionTracker.RemoveConnection(Context.ConnectionId);
        
        _logger.LogInformation("Client {ConnectionId} left campaign {CampaignId}", Context.ConnectionId, campaignId);
    }

    /// <summary>
    /// Submit a player choice to the DM
    /// </summary>
    public async Task SubmitChoice(Guid campaignId, string characterId, string characterName, string choice)
    {
        _logger.LogInformation(
            "Choice submitted: Campaign={CampaignId}, Character={CharacterId}, Choice={Choice}",
            campaignId, characterId, choice);

        var payload = new PlayerChoicePayload(characterId, characterName, choice, DateTime.UtcNow);
        
        // Send to DM only
        await Clients.Group($"campaign_{campaignId}_dm").SendAsync(GameHubEvents.PlayerChoiceSubmitted, payload);
    }

    /// <summary>
    /// Handle client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionInfo = _connectionTracker.GetConnectionInfo(Context.ConnectionId);
        
        if (connectionInfo != null)
        {
            // Notify DM if player disconnecting
            if (!connectionInfo.IsDm && connectionInfo.CharacterId != null)
            {
                await Clients.Group($"campaign_{connectionInfo.CampaignId}_dm").SendAsync(
                    GameHubEvents.PlayerDisconnected,
                    new PlayerConnectionPayload(
                        connectionInfo.UserId,
                        connectionInfo.UserName,
                        connectionInfo.CharacterId,
                        connectionInfo.CharacterName,
                        false));
            }
        }
        
        _connectionTracker.RemoveConnection(Context.ConnectionId);
        
        _logger.LogInformation(
            "Client {ConnectionId} disconnected. Exception: {Exception}",
            Context.ConnectionId, exception?.Message ?? "none");
        
        await base.OnDisconnectedAsync(exception);
    }
}
