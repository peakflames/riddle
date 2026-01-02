namespace Riddle.Web.Models;

/// <summary>
/// Configuration for admin-level permissions.
/// Bound from appsettings.json "AdminSettings" section.
/// Admins can edit/delete any character template regardless of ownership.
/// </summary>
public class AdminSettings
{
    /// <summary>
    /// List of email addresses that have admin permissions.
    /// Case-insensitive comparison is used.
    /// </summary>
    public List<string> AdminEmails { get; set; } = new();
}
