namespace Riddle.Web.Models;

/// <summary>
/// Configuration for the user whitelist feature.
/// Bound from appsettings.json "WhitelistSettings" section.
/// </summary>
public class WhitelistSettings
{
    /// <summary>
    /// Whether the whitelist is enforced. When false, all authenticated users can access.
    /// Default: true (whitelist enforced during beta)
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Message shown to users who are not on the whitelist.
    /// </summary>
    public string RejectionMessage { get; set; } = 
        "This application is currently in private beta. Contact the administrator for access.";
}
