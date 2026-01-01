namespace Riddle.Web.Models;

/// <summary>
/// Represents an email address allowed to sign into the application.
/// Used for beta testing access control.
/// </summary>
public class AllowedUser
{
    /// <summary>
    /// Primary key (GUID v7 for time-sortability)
    /// </summary>
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    
    /// <summary>
    /// Email address (stored lowercase, matched case-insensitively)
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional display name or note about this user
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Who added this user to the whitelist (admin's user ID)
    /// </summary>
    public string? AddedByUserId { get; set; }
    
    /// <summary>
    /// When this user was added to the whitelist
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this entry is currently active (allows soft-disable without delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
