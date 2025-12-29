using System.Collections.Concurrent;
using Riddle.Web.Hubs;

namespace Riddle.Web.Services;

/// <summary>
/// In-memory tracking of SignalR connections
/// Singleton service for cross-request state
/// </summary>
public class ConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConnectionInfo>> _campaignConnections = new();
    private readonly ILogger<ConnectionTracker> _logger;

    public ConnectionTracker(ILogger<ConnectionTracker> logger)
    {
        _logger = logger;
    }

    public void AddConnection(string connectionId, Guid campaignId, string userId, string? characterId, bool isDm)
    {
        var info = new ConnectionInfo(
            connectionId, 
            campaignId, 
            userId,
            "", // UserName will be updated when we have it
            characterId,
            null, // CharacterName will be updated when we have it
            isDm,
            DateTime.UtcNow
        );
        
        _connections[connectionId] = info;
        
        var campaignDict = _campaignConnections.GetOrAdd(campaignId, _ => new ConcurrentDictionary<string, ConnectionInfo>());
        campaignDict[connectionId] = info;
        
        _logger.LogInformation(
            "Connection added: {ConnectionId} to campaign {CampaignId} (User: {UserId}, IsDm: {IsDm})",
            connectionId, campaignId, userId, isDm);
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var info))
        {
            if (_campaignConnections.TryGetValue(info.CampaignId, out var campaignDict))
            {
                campaignDict.TryRemove(connectionId, out _);
                
                // Clean up empty campaign dictionaries
                if (campaignDict.IsEmpty)
                {
                    _campaignConnections.TryRemove(info.CampaignId, out _);
                }
            }
            
            _logger.LogInformation(
                "Connection removed: {ConnectionId} from campaign {CampaignId}",
                connectionId, info.CampaignId);
        }
    }

    public IEnumerable<PlayerConnectionPayload> GetConnectedPlayers(Guid campaignId)
    {
        if (!_campaignConnections.TryGetValue(campaignId, out var campaignDict))
            return Enumerable.Empty<PlayerConnectionPayload>();

        return campaignDict.Values
            .Where(c => !c.IsDm)
            .Select(c => new PlayerConnectionPayload(
                c.UserId, 
                c.UserName, 
                c.CharacterId, 
                c.CharacterName, 
                true));
    }

    public bool IsPlayerOnline(Guid campaignId, string userId)
    {
        if (!_campaignConnections.TryGetValue(campaignId, out var campaignDict))
            return false;

        return campaignDict.Values.Any(c => c.UserId == userId);
    }

    public string? GetConnectionId(Guid campaignId, string userId)
    {
        if (!_campaignConnections.TryGetValue(campaignId, out var campaignDict))
            return null;

        return campaignDict.Values.FirstOrDefault(c => c.UserId == userId)?.ConnectionId;
    }

    public ConnectionInfo? GetConnectionInfo(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var info);
        return info;
    }
}
