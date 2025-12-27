using Microsoft.AspNetCore.Identity;

namespace Riddle.Web.Models;

/// <summary>
/// Application user extending Identity user with additional properties
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Date when the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
