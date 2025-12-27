namespace Riddle.Web.Models;

/// <summary>
/// Represents a character in the game (PC or NPC)
/// Stored as JSON within RiddleSession.PartyStateJson
/// </summary>
public class Character
{
    /// <summary>
    /// Unique identifier for the character (UUID v7 for time-ordered sorting)
    /// </summary>
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    
    /// <summary>
    /// Character name
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Type of character: "PC" for player character, "NPC" for non-player character
    /// </summary>
    public string Type { get; set; } = "PC";
    
    /// <summary>
    /// Character's armor class
    /// </summary>
    public int ArmorClass { get; set; }
    
    /// <summary>
    /// Maximum hit points
    /// </summary>
    public int MaxHp { get; set; }
    
    /// <summary>
    /// Current hit points
    /// </summary>
    public int CurrentHp { get; set; }
    
    /// <summary>
    /// Initiative modifier
    /// </summary>
    public int Initiative { get; set; }
    
    /// <summary>
    /// Passive perception score
    /// </summary>
    public int PassivePerception { get; set; }
    
    /// <summary>
    /// Active conditions affecting the character (e.g., "Poisoned", "Frightened")
    /// </summary>
    public List<string> Conditions { get; set; } = new();
    
    /// <summary>
    /// Additional status notes for the character
    /// </summary>
    public string? StatusNotes { get; set; }
    
    /// <summary>
    /// User ID of the player controlling this character (for PCs)
    /// </summary>
    public string? PlayerId { get; set; }
    
    /// <summary>
    /// Display name of the player (for PCs)
    /// </summary>
    public string? PlayerName { get; set; }
}
