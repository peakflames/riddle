namespace Riddle.Web.Models;

/// <summary>
/// Represents an active combat encounter
/// Stored as JSON within RiddleSession.ActiveCombatJson
/// </summary>
public class CombatEncounter
{
    /// <summary>
    /// Unique identifier for the encounter (UUID v7 for time-ordered sorting)
    /// </summary>
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    
    /// <summary>
    /// Whether combat is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Current round number
    /// </summary>
    public int RoundNumber { get; set; } = 1;
    
    /// <summary>
    /// Turn order as list of character IDs
    /// </summary>
    public List<string> TurnOrder { get; set; } = new();
    
    /// <summary>
    /// Index of the current turn in TurnOrder
    /// </summary>
    public int CurrentTurnIndex { get; set; } = 0;
    
    /// <summary>
    /// Character IDs of entities that are surprised
    /// </summary>
    public List<string> SurprisedEntities { get; set; } = new();
}
