using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing RiddleSession CRUD operations
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Get all sessions for a specific user
    /// </summary>
    Task<List<RiddleSession>> GetSessionsForUserAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get a session by ID
    /// </summary>
    Task<RiddleSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Create a new session
    /// </summary>
    Task<RiddleSession> CreateSessionAsync(string userId, string campaignName, CancellationToken ct = default);
    
    /// <summary>
    /// Update an existing session
    /// </summary>
    Task<RiddleSession> UpdateSessionAsync(RiddleSession session, CancellationToken ct = default);
    
    /// <summary>
    /// Delete a session
    /// </summary>
    Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Get count of sessions for a user
    /// </summary>
    Task<int> GetSessionCountAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get total character count across all sessions for a user
    /// </summary>
    Task<int> GetCharacterCountAsync(string userId, CancellationToken ct = default);
}
