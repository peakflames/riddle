using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Riddle.Web.Models;

/// <summary>
/// Represents a reusable character template that DMs can import into campaigns.
/// Templates are stored independently from campaigns and act as a picklist.
/// </summary>
public class CharacterTemplate
{
    // ========================================
    // Primary Key
    // ========================================
    
    /// <summary>
    /// Unique identifier for the template (UUID v7 for time-ordered sorting)
    /// </summary>
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    
    // ========================================
    // Identity
    // ========================================
    
    /// <summary>
    /// Character name (indexed for fast lookup)
    /// Combined with OwnerId forms a unique constraint
    /// </summary>
    public string Name { get; set; } = null!;
    
    // ========================================
    // Ownership
    // ========================================
    
    /// <summary>
    /// User ID of the template owner.
    /// null = system template (imported from JSON files, visible to all)
    /// {userId} = user template (visible only to owner)
    /// </summary>
    public string? OwnerId { get; set; }
    
    /// <summary>
    /// Navigation property to the owner (optional)
    /// </summary>
    public ApplicationUser? Owner { get; set; }
    
    /// <summary>
    /// Whether this is a system-provided template (OwnerId is null)
    /// </summary>
    public bool IsSystemTemplate => OwnerId == null;
    
    /// <summary>
    /// Whether this template is publicly available for import by other users.
    /// If false, only the owner (and admins) can import it into their campaigns.
    /// Default: true (public). System templates are always public.
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    // ========================================
    // Source Tracking
    // ========================================
    
    /// <summary>
    /// Original source file name if imported from JSON (e.g., "gandalf_the_grey.json")
    /// </summary>
    public string? SourceFile { get; set; }
    
    // ========================================
    // Character Data (JSON Blob)
    // ========================================
    
    /// <summary>
    /// The complete character data stored as JSON.
    /// Uses the same structure as Character model.
    /// </summary>
    [Column(TypeName = "text")]
    public string CharacterJson { get; set; } = "{}";
    
    /// <summary>
    /// Computed accessor for deserializing/serializing the character data.
    /// Note: Each access to getter deserializes fresh - capture in local variable when modifying!
    /// </summary>
    [NotMapped]
    public Character Character
    {
        get => JsonSerializer.Deserialize<Character>(CharacterJson) ?? new();
        set => CharacterJson = JsonSerializer.Serialize(value);
    }
    
    // ========================================
    // Shadow Columns (for filtering/querying)
    // ========================================
    
    /// <summary>
    /// Character's race (denormalized from JSON for filtering)
    /// </summary>
    public string? Race { get; set; }
    
    /// <summary>
    /// Character's class (denormalized from JSON for filtering)
    /// </summary>
    public string? Class { get; set; }
    
    /// <summary>
    /// Character's level (denormalized from JSON for filtering)
    /// </summary>
    public int? Level { get; set; }
    
    // ========================================
    // Metadata
    // ========================================
    
    /// <summary>
    /// When the template was first created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the template was last updated (e.g., via upsert)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // ========================================
    // Display Helpers
    // ========================================
    
    /// <summary>
    /// Display string for race and class (e.g., "High Elf Wizard")
    /// </summary>
    public string DisplayRaceClass => $"{Race ?? "Unknown"} {Class ?? "Unknown"}";
    
    /// <summary>
    /// Display string for class and level (e.g., "Wizard L3")
    /// </summary>
    public string DisplayLevel => $"{Class ?? "Unknown"} L{Level ?? 1}";
}
