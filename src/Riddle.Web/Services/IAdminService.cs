using System.Security.Claims;

namespace Riddle.Web.Services;

/// <summary>
/// Service for checking admin permissions.
/// Admins are defined by email list in appsettings.json "AdminSettings:AdminEmails".
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Check if the given email has admin permissions.
    /// </summary>
    /// <param name="email">The email to check (case-insensitive)</param>
    /// <returns>True if the email is in the admin list</returns>
    bool IsAdmin(string? email);
    
    /// <summary>
    /// Check if the current user has admin permissions.
    /// Extracts email from ClaimTypes.Email claim.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal to check</param>
    /// <returns>True if the user's email is in the admin list</returns>
    bool IsAdmin(ClaimsPrincipal? user);
}
