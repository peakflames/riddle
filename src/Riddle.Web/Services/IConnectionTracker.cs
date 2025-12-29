using Riddle.Web.Hubs;

namespace Riddle.Web.Services;

/// <summary>
/// Tracks active connections per campaign
/// </summary>
public interface IConnectionTracker
{
    /// <summary>
    /// Register a new connection
    /// </summary>
    void AddConnection(string connectionId, Guid campaignId, string userId, string? characterId, bool isDm);
    
    /// <summary>
    /// Remove a connection when client disconnects
    /// </summary>
    void RemoveConnection(string connectionId);
    
    /// <summary>
    /// Get all connected players for a campaign
    /// </summary>
    IEnumerable<PlayerConnectionPayload> GetConnectedPlayers(Guid campaignId);
    
    /// <summary>
    /// Check if a specific player is online
    /// </summary>
    bool IsPlayerOnline(Guid campaignId, string userId);
    
    /// <summary>
    /// Get connection ID for a specific user in a campaign
    /// </summary>
    string? GetConnectionId(Guid campaignId, string userId);
    
    /// <summary>
    /// Get connection info by connection ID
    /// </summary>
    ConnectionInfo? GetConnectionInfo(string connectionId);
}

/// <summary>
/// Information about a SignalR connection
/// </summary>
public record ConnectionInfo(
    string ConnectionId, 
    Guid CampaignId, 
    string UserId,
    string UserName,
    string? CharacterId,
    string? CharacterName, 
    bool IsDm,
    DateTime ConnectedAt
);
