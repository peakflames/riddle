using System.Security.Claims;
using Microsoft.Extensions.Options;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for checking admin permissions based on email list in appsettings.json.
/// Uses IOptionsMonitor for hot-reload support when appsettings.json changes.
/// </summary>
public class AdminService : IAdminService
{
    private readonly IOptionsMonitor<AdminSettings> _optionsMonitor;

    public AdminService(IOptionsMonitor<AdminSettings> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    // Access .CurrentValue each time for hot-reload support
    private IEnumerable<string> AdminEmails => _optionsMonitor.CurrentValue.AdminEmails;

    /// <inheritdoc />
    public bool IsAdmin(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        var normalizedEmail = email.ToLowerInvariant();
        return AdminEmails.Any(e => e.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
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
    public IReadOnlyCollection<string> GetAdminEmails() => 
        AdminEmails.ToList().AsReadOnly();
}
