namespace Riddle.Web.Models;

/// <summary>
/// Represents party preferences for campaign customization
/// Stored as JSON within RiddleSession.PreferencesJson
/// </summary>
public class PartyPreferences
{
    /// <summary>
    /// Combat intensity preference: "Low", "Medium", or "High"
    /// </summary>
    public string CombatFocus { get; set; } = "Medium";
    
    /// <summary>
    /// Roleplay intensity preference: "Low", "Medium", or "High"
    /// </summary>
    public string RoleplayFocus { get; set; } = "Medium";
    
    /// <summary>
    /// Game pacing preference: "Fast" or "Methodical"
    /// </summary>
    public string Pacing { get; set; } = "Methodical";
    
    /// <summary>
    /// Campaign tone: "Adventurous", "Dark", or "Comedic"
    /// </summary>
    public string Tone { get; set; } = "Adventurous";
    
    /// <summary>
    /// Topics the party wants to avoid in the campaign
    /// </summary>
    public List<string> AvoidedTopics { get; set; } = new();
}
