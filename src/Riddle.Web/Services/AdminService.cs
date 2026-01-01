using System.Security.Claims;
using Microsoft.Extensions.Options;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for checking admin permissions based on email list in appsettings.json.
/// </summary>
public class AdminService : IAdminService
{
    private readonly HashSet<string> _adminEmails;

    public AdminService(IOptions<AdminSettings> options)
    {
        // Store admin emails in lowercase for case-insensitive comparison
        _adminEmails = new HashSet<string>(
            options.Value.AdminEmails.Select(e => e.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <inheritdoc />
    public bool IsAdmin(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        return _adminEmails.Contains(email.ToLowerInvariant());
    }

    /// <inheritdoc />
    public bool IsAdmin(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;
        
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        return IsAdmin(email);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetAdminEmails() => _adminEmails;
}
