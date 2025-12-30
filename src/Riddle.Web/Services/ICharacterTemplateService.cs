using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service interface for managing character templates.
/// Templates are reusable character definitions that DMs can import into campaigns.
/// </summary>
public interface ICharacterTemplateService
{
    // ========================================
    // Query Methods
    // ========================================
    
    /// <summary>
    /// Get all system-provided templates (OwnerId is null).
    /// These are imported from JSON files and visible to all users.
    /// </summary>
    Task<List<CharacterTemplate>> GetSystemTemplatesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Get all templates owned by a specific user.
    /// </summary>
    Task<List<CharacterTemplate>> GetUserTemplatesAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get all templates available to a user (system + user-owned).
    /// </summary>
    Task<List<CharacterTemplate>> GetAllAvailableTemplatesAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get a template by its ID.
    /// </summary>
    Task<CharacterTemplate?> GetTemplateByIdAsync(string id, CancellationToken ct = default);
    
    /// <summary>
    /// Get a template by name (for system templates, OwnerId is null).
    /// </summary>
    Task<CharacterTemplate?> GetSystemTemplateByNameAsync(string name, CancellationToken ct = default);
    
    // ========================================
    // Import Methods
    // ========================================
    
    /// <summary>
    /// Import a character from JSON string, using upsert logic.
    /// If a template with the same name and owner exists, it will be replaced.
    /// </summary>
    /// <param name="json">Character JSON data</param>
    /// <param name="ownerId">User ID of owner (null for system templates)</param>
    /// <param name="sourceFile">Original source file name (for tracking)</param>
    /// <returns>The created or updated template</returns>
    Task<CharacterTemplate> ImportFromJsonAsync(string json, string? ownerId = null, string? sourceFile = null, CancellationToken ct = default);
    
    /// <summary>
    /// Import all JSON files from the SampleCharacters directory as system templates.
    /// Uses upsert logic - existing templates with same name will be replaced.
    /// </summary>
    /// <returns>Number of templates imported</returns>
    Task<int> ImportAllSystemTemplatesAsync(CancellationToken ct = default);
    
    // ========================================
    // CRUD Methods
    // ========================================
    
    /// <summary>
    /// Create or update a template (upsert by Name + OwnerId).
    /// </summary>
    Task<CharacterTemplate> UpsertTemplateAsync(CharacterTemplate template, CancellationToken ct = default);
    
    /// <summary>
    /// Delete a template by ID.
    /// </summary>
    Task<bool> DeleteTemplateAsync(string id, CancellationToken ct = default);
    
    // ========================================
    // Campaign Integration
    // ========================================
    
    /// <summary>
    /// Copy a template to a campaign's party.
    /// Creates a new Character with a fresh ID, no link back to template.
    /// </summary>
    /// <param name="templateId">ID of the template to copy</param>
    /// <param name="campaignId">ID of the campaign to add character to</param>
    /// <returns>The newly created character (with fresh ID)</returns>
    Task<Character?> CopyToCampaignAsync(string templateId, string campaignId, CancellationToken ct = default);
}
