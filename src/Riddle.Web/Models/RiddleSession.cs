using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Riddle.Web.Models;

/// <summary>
/// Root entity representing a game session
/// </summary>
[Index(nameof(DmUserId))]
public class RiddleSession
{
    /// <summary>
    /// Unique identifier for the session (UUID v7 for time-ordered sorting)
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();
    
    /// <summary>
    /// Name of the campaign
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CampaignName { get; set; } = "Lost Mine of Phandelver";
    
    /// <summary>
    /// When this session was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    // Owner
    /// <summary>
    /// User ID of the Dungeon Master
    /// </summary>
    [Required]
    public string DmUserId { get; set; } = null!;
    
    /// <summary>
    /// Navigation property to the DM user
    /// </summary>
    [ForeignKey(nameof(DmUserId))]
    public ApplicationUser DmUser { get; set; } = null!;
    
    // Campaign Progression
    /// <summary>
    /// Current chapter in the campaign
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CurrentChapterId { get; set; } = "chapter_1";
    
    /// <summary>
    /// Current location in the campaign
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CurrentLocationId { get; set; } = "goblin_ambush";
    
    // JSON stored collections
    /// <summary>
    /// JSON storage for completed milestones
    /// </summary>
    [Column(TypeName = "text")]
    public string CompletedMilestonesJson { get; set; } = "[]";
    
    /// <summary>
    /// JSON storage for known NPC IDs
    /// </summary>
    [Column(TypeName = "text")]
    public string KnownNpcIdsJson { get; set; } = "[]";
    
    /// <summary>
    /// JSON storage for discovered locations
    /// </summary>
    [Column(TypeName = "text")]
    public string DiscoveredLocationsJson { get; set; } = "[]";
    
    // Entity collections as JSON
    /// <summary>
    /// JSON storage for party state (characters)
    /// </summary>
    [Column(TypeName = "text")]
    public string PartyStateJson { get; set; } = "[]";
    
    /// <summary>
    /// JSON storage for active quests
    /// </summary>
    [Column(TypeName = "text")]
    public string ActiveQuestsJson { get; set; } = "[]";
    
    /// <summary>
    /// JSON storage for active combat encounter (nullable)
    /// </summary>
    [Column(TypeName = "text")]
    public string? ActiveCombatJson { get; set; }
    
    /// <summary>
    /// JSON storage for narrative log entries
    /// </summary>
    [Column(TypeName = "text")]
    public string NarrativeLogJson { get; set; } = "[]";
    
    /// <summary>
    /// JSON storage for party preferences
    /// </summary>
    [Column(TypeName = "text")]
    public string PreferencesJson { get; set; } = "{}";
    
    // Context
    /// <summary>
    /// Summary of the last narrative for context
    /// </summary>
    [MaxLength(5000)]
    public string? LastNarrativeSummary { get; set; }
    
    // UI State
    /// <summary>
    /// JSON storage for active player choices
    /// </summary>
    [Column(TypeName = "text")]
    public string ActivePlayerChoicesJson { get; set; } = "[]";
    
    /// <summary>
    /// URI to the current scene image
    /// </summary>
    [MaxLength(500)]
    public string? CurrentSceneImageUri { get; set; }
    
    /// <summary>
    /// Current read-aloud text for the players
    /// </summary>
    [MaxLength(5000)]
    public string? CurrentReadAloudText { get; set; }
    
    // NotMapped properties for JSON deserialization convenience
    
    /// <summary>
    /// List of completed milestone IDs
    /// </summary>
    [NotMapped]
    public List<string> CompletedMilestones
    {
        get => JsonSerializer.Deserialize<List<string>>(CompletedMilestonesJson) ?? new();
        set => CompletedMilestonesJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// List of known NPC IDs
    /// </summary>
    [NotMapped]
    public List<string> KnownNpcIds
    {
        get => JsonSerializer.Deserialize<List<string>>(KnownNpcIdsJson) ?? new();
        set => KnownNpcIdsJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// List of discovered location IDs
    /// </summary>
    [NotMapped]
    public List<string> DiscoveredLocations
    {
        get => JsonSerializer.Deserialize<List<string>>(DiscoveredLocationsJson) ?? new();
        set => DiscoveredLocationsJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// Party state as list of characters
    /// </summary>
    [NotMapped]
    public List<Character> PartyState
    {
        get => JsonSerializer.Deserialize<List<Character>>(PartyStateJson) ?? new();
        set => PartyStateJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// List of active quests
    /// </summary>
    [NotMapped]
    public List<Quest> ActiveQuests
    {
        get => JsonSerializer.Deserialize<List<Quest>>(ActiveQuestsJson) ?? new();
        set => ActiveQuestsJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// Active combat encounter (nullable)
    /// </summary>
    [NotMapped]
    public CombatEncounter? ActiveCombat
    {
        get => string.IsNullOrEmpty(ActiveCombatJson) 
            ? null 
            : JsonSerializer.Deserialize<CombatEncounter>(ActiveCombatJson);
        set => ActiveCombatJson = value == null ? null : JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// Narrative log entries
    /// </summary>
    [NotMapped]
    public List<LogEntry> NarrativeLog
    {
        get => JsonSerializer.Deserialize<List<LogEntry>>(NarrativeLogJson) ?? new();
        set => NarrativeLogJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// Party preferences
    /// </summary>
    [NotMapped]
    public PartyPreferences Preferences
    {
        get => JsonSerializer.Deserialize<PartyPreferences>(PreferencesJson) ?? new();
        set => PreferencesJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// Active player choices
    /// </summary>
    [NotMapped]
    public List<string> ActivePlayerChoices
    {
        get => JsonSerializer.Deserialize<List<string>>(ActivePlayerChoicesJson) ?? new();
        set => ActivePlayerChoicesJson = JsonSerializer.Serialize(value);
    }
}
