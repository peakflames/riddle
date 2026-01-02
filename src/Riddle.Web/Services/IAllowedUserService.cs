using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing the user whitelist (beta access control)
/// </summary>
public interface IAllowedUserService
{
    /// <summary>
    /// Check if an email is allowed to sign in (either in whitelist or is admin)
    /// </summary>
    Task<bool> IsEmailAllowedAsync(string email, CancellationToken ct = default);
    
    /// <summary>
    /// Get all allowed users (for admin UI)
    /// </summary>
    Task<List<AllowedUser>> GetAllowedUsersAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Add an email to the whitelist
    /// </summary>
    Task<AllowedUser> AddAllowedUserAsync(string email, string? displayName, string addedByUserId, CancellationToken ct = default);
    
    /// <summary>
    /// Remove an email from the whitelist (hard delete)
    /// </summary>
    Task<bool> RemoveAllowedUserAsync(string id, CancellationToken ct = default);
    
    /// <summary>
    /// Toggle active status (soft enable/disable)
    /// </summary>
    Task<bool> SetActiveStatusAsync(string id, bool isActive, CancellationToken ct = default);
    
    /// <summary>
    /// Check if whitelist enforcement is enabled
    /// </summary>
    bool IsWhitelistEnabled { get; }
}
