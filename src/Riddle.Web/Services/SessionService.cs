using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing RiddleSession CRUD operations
/// </summary>
public class SessionService : ISessionService
{
    private readonly RiddleDbContext _dbContext;
    private readonly ILogger<SessionService> _logger;

    public SessionService(RiddleDbContext dbContext, ILogger<SessionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<RiddleSession>> GetSessionsForUserAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting sessions for user {UserId}", userId);
        
        return await _dbContext.RiddleSessions
            .Where(s => s.DmUserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<RiddleSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting session {SessionId}", sessionId);
        
        return await _dbContext.RiddleSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    /// <inheritdoc/>
    public async Task<RiddleSession> CreateSessionAsync(string userId, string campaignName, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new session '{CampaignName}' for user {UserId}", campaignName, userId);
        
        var session = new RiddleSession
        {
            Id = Guid.CreateVersion7(),
            CampaignName = campaignName,
            DmUserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _dbContext.RiddleSessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created session {SessionId}", session.Id);
        
        return session;
    }

    /// <inheritdoc/>
    public async Task<RiddleSession> UpdateSessionAsync(RiddleSession session, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating session {SessionId}", session.Id);
        
        session.LastActivityAt = DateTime.UtcNow;
        _dbContext.RiddleSessions.Update(session);
        await _dbContext.SaveChangesAsync(ct);
        
        return session;
    }

    /// <inheritdoc/>
    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting session {SessionId}", sessionId);
        
        var session = await _dbContext.RiddleSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        
        if (session != null)
        {
            _dbContext.RiddleSessions.Remove(session);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetSessionCountAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.RiddleSessions
            .CountAsync(s => s.DmUserId == userId, ct);
    }

    /// <inheritdoc/>
    public async Task<int> GetCharacterCountAsync(string userId, CancellationToken ct = default)
    {
        var sessions = await _dbContext.RiddleSessions
            .Where(s => s.DmUserId == userId)
            .ToListAsync(ct);
        
        return sessions.Sum(s => s.PartyState.Count);
    }
}
