using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Riddle.Web.Models;

/// <summary>
/// Represents a single game night (play session) within a CampaignInstance.
/// Tracks session-specific details like duration, notes, and key events.
/// </summary>
public class PlaySession
{
    /// <summary>
    /// Unique identifier for the play session (UUID v7 for time-ordered sorting)
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();
    
    /// <summary>
    /// The campaign instance this play session belongs to
    /// </summary>
    [Required]
    public Guid CampaignInstanceId { get; set; }
    
    /// <summary>
    /// Navigation property to the campaign instance
    /// </summary>
    [ForeignKey(nameof(CampaignInstanceId))]
    public CampaignInstance CampaignInstance { get; set; } = null!;
    
    /// <summary>
    /// Sequential session number (1, 2, 3, etc.)
    /// </summary>
    public int SessionNumber { get; set; }
    
    /// <summary>
    /// When this play session started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this play session ended (null if still active)
    /// </summary>
    public DateTime? EndedAt { get; set; }
    
    /// <summary>
    /// Whether this play session is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Location ID at the start of this session
    /// </summary>
    [MaxLength(100)]
    public string StartLocationId { get; set; } = "";
    
    /// <summary>
    /// Location ID at the end of this session (null if still active)
    /// </summary>
    [MaxLength(100)]
    public string? EndLocationId { get; set; }
    
    /// <summary>
    /// DM's private notes for this session
    /// </summary>
    [MaxLength(5000)]
    public string? DmNotes { get; set; }
    
    /// <summary>
    /// JSON storage for key events during this session
    /// </summary>
    [Column(TypeName = "text")]
    public string KeyEventsJson { get; set; } = "[]";
    
    /// <summary>
    /// Optional title/name for this session (e.g., "The Cragmaw Hideout")
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }
    
    // NotMapped convenience property
    
    /// <summary>
    /// List of key event descriptions
    /// </summary>
    [NotMapped]
    public List<string> KeyEvents
    {
        get => JsonSerializer.Deserialize<List<string>>(KeyEventsJson) ?? [];
        set => KeyEventsJson = JsonSerializer.Serialize(value);
    }
}
