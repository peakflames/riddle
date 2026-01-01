using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Riddle.Web.Data;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing the user whitelist (beta access control)
/// </summary>
public class AllowedUserService : IAllowedUserService
{
    private readonly RiddleDbContext _db;
    private readonly IAdminService _adminService;
    private readonly WhitelistSettings _settings;
    private readonly ILogger<AllowedUserService> _logger;

    public AllowedUserService(
        RiddleDbContext db,
        IAdminService adminService,
        IOptions<WhitelistSettings> settings,
        ILogger<AllowedUserService> logger)
    {
        _db = db;
        _adminService = adminService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsWhitelistEnabled => _settings.IsEnabled;

    /// <inheritdoc />
    public async Task<bool> IsEmailAllowedAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // If whitelist is disabled, everyone is allowed
        if (!_settings.IsEnabled)
        {
            _logger.LogDebug("Whitelist disabled, allowing {Email}", email);
            return true;
        }

        // Admins are always allowed
        if (_adminService.IsAdmin(email))
        {
            _logger.LogDebug("Admin email {Email} allowed", email);
            return true;
        }

        // Check whitelist database
        var normalizedEmail = email.ToLowerInvariant();
        var isAllowed = await _db.AllowedUsers
            .AnyAsync(u => u.Email == normalizedEmail && u.IsActive, ct);

        _logger.LogDebug("Email {Email} whitelist check: {IsAllowed}", email, isAllowed);
        return isAllowed;
    }

    /// <inheritdoc />
    public async Task<List<AllowedUser>> GetAllowedUsersAsync(CancellationToken ct = default)
    {
        return await _db.AllowedUsers
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<AllowedUser> AddAllowedUserAsync(string email, string? displayName, string addedByUserId, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();

        // Check if already exists
        var existing = await _db.AllowedUsers
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        
        if (existing != null)
        {
            _logger.LogWarning("Attempted to add duplicate email {Email} to whitelist", email);
            throw new InvalidOperationException($"Email '{email}' is already in the whitelist.");
        }

        var user = new AllowedUser
        {
            Email = normalizedEmail,
            DisplayName = displayName?.Trim(),
            AddedByUserId = addedByUserId,
            IsActive = true
        };

        _db.AllowedUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Added {Email} to whitelist by {AddedBy}", email, addedByUserId);
        return user;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAllowedUserAsync(string id, CancellationToken ct = default)
    {
        var user = await _db.AllowedUsers.FindAsync([id], ct);
        if (user == null)
            return false;

        _db.AllowedUsers.Remove(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Removed {Email} from whitelist", user.Email);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SetActiveStatusAsync(string id, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.AllowedUsers.FindAsync([id], ct);
        if (user == null)
            return false;

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Set {Email} whitelist status to {IsActive}", user.Email, isActive);
        return true;
    }
}
