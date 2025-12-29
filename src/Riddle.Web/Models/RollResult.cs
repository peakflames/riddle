namespace Riddle.Web.Models;

/// <summary>
/// Represents a dice roll result logged by the LLM DM.
/// Stored in CampaignInstance.RecentRolls for display on DM and Player dashboards.
/// </summary>
public class RollResult
{
    /// <summary>
    /// Unique identifier for this roll (UUID v7 for time-ordered sorting)
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();
    
    /// <summary>
    /// ID of the character who made the roll
    /// </summary>
    public string CharacterId { get; set; } = "";
    
    /// <summary>
    /// Display name of the character (denormalized for easy display without lookup)
    /// </summary>
    public string CharacterName { get; set; } = "";
    
    /// <summary>
    /// Type of check performed (e.g., "Perception", "Stealth", "Athletics", "Saving Throw")
    /// </summary>
    public string CheckType { get; set; } = "";
    
    /// <summary>
    /// The numeric result of the dice roll
    /// </summary>
    public int Result { get; set; }
    
    /// <summary>
    /// Outcome of the roll: "Success", "Failure", "Critical Success", "Critical Failure"
    /// </summary>
    public string Outcome { get; set; } = "";
    
    /// <summary>
    /// When this roll was made
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
