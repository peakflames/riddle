namespace Riddle.Web.Models;

/// <summary>
/// Represents a quest in the game
/// Stored as JSON within RiddleSession.ActiveQuestsJson
/// </summary>
public class Quest
{
    /// <summary>
    /// Unique identifier for the quest (UUID v7 for time-ordered sorting)
    /// </summary>
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    
    /// <summary>
    /// Quest title
    /// </summary>
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Current state of the quest: "Active", "Completed", or "Failed"
    /// </summary>
    public string State { get; set; } = "Active";
    
    /// <summary>
    /// Whether this is part of the main story line
    /// </summary>
    public bool IsMainStory { get; set; }
    
    /// <summary>
    /// List of quest objectives
    /// </summary>
    public List<string> Objectives { get; set; } = new();
    
    /// <summary>
    /// Description of the quest reward
    /// </summary>
    public string? RewardDescription { get; set; }
}
