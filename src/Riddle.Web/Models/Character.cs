namespace Riddle.Web.Models;

/// <summary>
/// Represents a character in the game (PC or NPC)
/// Stored as JSON within CampaignInstance.PartyStateJson
/// </summary>
public class Character
{
    // ========================================
    // Identity
    // ========================================
    
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
    
    // ========================================
    // Core D&D 5e Fields
    // ========================================
    
    /// <summary>
    /// Character's race (e.g., "High Elf", "Human", "Dwarf")
    /// </summary>
    public string? Race { get; set; }
    
    /// <summary>
    /// Character's class (e.g., "Fighter", "Wizard", "Rogue")
    /// </summary>
    public string? Class { get; set; }
    
    /// <summary>
    /// Character level (1-20)
    /// </summary>
    public int Level { get; set; } = 1;
    
    /// <summary>
    /// Character's background (e.g., "Sage", "Soldier", "Criminal")
    /// </summary>
    public string? Background { get; set; }
    
    /// <summary>
    /// Character's alignment (e.g., "Neutral Good", "Chaotic Evil")
    /// </summary>
    public string? Alignment { get; set; }
    
    // ========================================
    // Ability Scores
    // ========================================
    
    /// <summary>
    /// Strength ability score (1-30, typically 3-20)
    /// </summary>
    public int Strength { get; set; } = 10;
    
    /// <summary>
    /// Dexterity ability score (1-30, typically 3-20)
    /// </summary>
    public int Dexterity { get; set; } = 10;
    
    /// <summary>
    /// Constitution ability score (1-30, typically 3-20)
    /// </summary>
    public int Constitution { get; set; } = 10;
    
    /// <summary>
    /// Intelligence ability score (1-30, typically 3-20)
    /// </summary>
    public int Intelligence { get; set; } = 10;
    
    /// <summary>
    /// Wisdom ability score (1-30, typically 3-20)
    /// </summary>
    public int Wisdom { get; set; } = 10;
    
    /// <summary>
    /// Charisma ability score (1-30, typically 3-20)
    /// </summary>
    public int Charisma { get; set; } = 10;
    
    // ========================================
    // Combat Stats
    // ========================================
    
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
    /// Temporary hit points (from spells, abilities, etc.)
    /// </summary>
    public int TemporaryHp { get; set; }
    
    /// <summary>
    /// Initiative modifier
    /// </summary>
    public int Initiative { get; set; }
    
    /// <summary>
    /// Movement speed (e.g., "30 ft", "25 ft", "30 ft, fly 60 ft")
    /// </summary>
    public string Speed { get; set; } = "30 ft";
    
    // ========================================
    // Skills & Proficiencies
    // ========================================
    
    /// <summary>
    /// Passive perception score
    /// </summary>
    public int PassivePerception { get; set; }
    
    /// <summary>
    /// Saving throw proficiencies (e.g., ["Strength", "Constitution"])
    /// </summary>
    public List<string> SavingThrowProficiencies { get; set; } = [];
    
    /// <summary>
    /// Skill proficiencies (e.g., ["Arcana", "History", "Perception"])
    /// </summary>
    public List<string> SkillProficiencies { get; set; } = [];
    
    /// <summary>
    /// Tool proficiencies (e.g., ["Thieves' Tools", "Smith's Tools"])
    /// </summary>
    public List<string> ToolProficiencies { get; set; } = [];
    
    /// <summary>
    /// Languages known (e.g., ["Common", "Elvish", "Dwarvish"])
    /// </summary>
    public List<string> Languages { get; set; } = ["Common"];
    
    // ========================================
    // Spellcasting (Optional)
    // ========================================
    
    /// <summary>
    /// Whether this character can cast spells
    /// </summary>
    public bool IsSpellcaster { get; set; }
    
    /// <summary>
    /// Spellcasting ability (e.g., "Intelligence", "Wisdom", "Charisma")
    /// </summary>
    public string? SpellcastingAbility { get; set; }
    
    /// <summary>
    /// Spell save DC
    /// </summary>
    public int? SpellSaveDC { get; set; }
    
    /// <summary>
    /// Spell attack bonus
    /// </summary>
    public int? SpellAttackBonus { get; set; }
    
    /// <summary>
    /// Known cantrips (e.g., ["Fire Bolt", "Mage Hand", "Light"])
    /// </summary>
    public List<string> Cantrips { get; set; } = [];
    
    /// <summary>
    /// Known or prepared spells (e.g., ["Magic Missile", "Shield", "Fireball"])
    /// </summary>
    public List<string> SpellsKnown { get; set; } = [];
    
    /// <summary>
    /// Spell slots by level (e.g., {1: 4, 2: 3, 3: 2})
    /// </summary>
    public Dictionary<int, int> SpellSlots { get; set; } = new();
    
    /// <summary>
    /// Spell slots used by level
    /// </summary>
    public Dictionary<int, int> SpellSlotsUsed { get; set; } = new();
    
    // ========================================
    // Equipment & Inventory
    // ========================================
    
    /// <summary>
    /// General equipment and items
    /// </summary>
    public List<string> Equipment { get; set; } = [];
    
    /// <summary>
    /// Weapons carried
    /// </summary>
    public List<string> Weapons { get; set; } = [];
    
    /// <summary>
    /// Copper pieces (currency)
    /// </summary>
    public int CopperPieces { get; set; }
    
    /// <summary>
    /// Silver pieces (currency)
    /// </summary>
    public int SilverPieces { get; set; }
    
    /// <summary>
    /// Gold pieces (currency)
    /// </summary>
    public int GoldPieces { get; set; }
    
    /// <summary>
    /// Platinum pieces (currency)
    /// </summary>
    public int PlatinumPieces { get; set; }
    
    // ========================================
    // Roleplay
    // ========================================
    
    /// <summary>
    /// Character's personality traits
    /// </summary>
    public string? PersonalityTraits { get; set; }
    
    /// <summary>
    /// Character's ideals
    /// </summary>
    public string? Ideals { get; set; }
    
    /// <summary>
    /// Character's bonds
    /// </summary>
    public string? Bonds { get; set; }
    
    /// <summary>
    /// Character's flaws
    /// </summary>
    public string? Flaws { get; set; }
    
    /// <summary>
    /// Character's backstory
    /// </summary>
    public string? Backstory { get; set; }
    
    // ========================================
    // State
    // ========================================
    
    /// <summary>
    /// Active conditions affecting the character (e.g., "Poisoned", "Frightened")
    /// </summary>
    public List<string> Conditions { get; set; } = [];
    
    /// <summary>
    /// Additional status notes for the character
    /// </summary>
    public string? StatusNotes { get; set; }
    
    /// <summary>
    /// Number of death saving throw successes (0-3)
    /// </summary>
    public int DeathSaveSuccesses { get; set; }
    
    /// <summary>
    /// Number of death saving throw failures (0-3)
    /// </summary>
    public int DeathSaveFailures { get; set; }
    
    // ========================================
    // Player Linking
    // ========================================
    
    /// <summary>
    /// User ID of the player controlling this character (for PCs)
    /// </summary>
    public string? PlayerId { get; set; }
    
    /// <summary>
    /// Display name of the player (for PCs)
    /// </summary>
    public string? PlayerName { get; set; }
    
    /// <summary>
    /// Whether this character has been claimed by a player
    /// </summary>
    public bool IsClaimed => !string.IsNullOrEmpty(PlayerId);
    
    // ========================================
    // Computed Properties
    // ========================================
    
    /// <summary>
    /// Strength ability modifier
    /// </summary>
    public int StrengthModifier => CalculateModifier(Strength);
    
    /// <summary>
    /// Dexterity ability modifier
    /// </summary>
    public int DexterityModifier => CalculateModifier(Dexterity);
    
    /// <summary>
    /// Constitution ability modifier
    /// </summary>
    public int ConstitutionModifier => CalculateModifier(Constitution);
    
    /// <summary>
    /// Intelligence ability modifier
    /// </summary>
    public int IntelligenceModifier => CalculateModifier(Intelligence);
    
    /// <summary>
    /// Wisdom ability modifier
    /// </summary>
    public int WisdomModifier => CalculateModifier(Wisdom);
    
    /// <summary>
    /// Charisma ability modifier
    /// </summary>
    public int CharismaModifier => CalculateModifier(Charisma);
    
    /// <summary>
    /// Display string for class and level (e.g., "Wizard L3")
    /// </summary>
    public string DisplayLevel => $"{Class ?? "Unknown"} L{Level}";
    
    /// <summary>
    /// Display string for race and class (e.g., "High Elf Wizard")
    /// </summary>
    public string DisplayRaceClass => $"{Race ?? "Unknown"} {Class ?? "Unknown"}";
    
    /// <summary>
    /// Calculate ability modifier from score using D&D 5e formula
    /// </summary>
    private static int CalculateModifier(int score) => (score - 10) / 2;
}
