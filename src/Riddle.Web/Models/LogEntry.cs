namespace Riddle.Web.Models;

/// <summary>
/// Represents a narrative log entry
/// Stored as JSON within RiddleSession.NarrativeLogJson
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Unique identifier for the log entry (UUID v7 for time-ordered sorting)
    /// </summary>
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    
    /// <summary>
    /// When this entry was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The narrative text entry
    /// </summary>
    public string Entry { get; set; } = null!;
    
    /// <summary>
    /// Importance level: "minor", "standard", or "critical"
    /// </summary>
    public string Importance { get; set; } = "standard";
}
