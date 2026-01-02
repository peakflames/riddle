using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Result of attempting to import a template from JSON.
/// </summary>
public record TemplateImportResult(
    bool Success,
    string? ErrorMessage,
    CharacterTemplate? Template
);

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
    /// Get templates that a user can import into their campaigns.
    /// Returns: all public templates + user's own templates (regardless of public flag).
    /// </summary>
    /// <param name="userId">The user ID to check ownership against</param>
    Task<List<CharacterTemplate>> GetImportableTemplatesAsync(string userId, CancellationToken ct = default);
    
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
    /// Import a character from JSON string with validation.
    /// Returns a result object with success/failure status and detailed error messages.
    /// </summary>
    /// <param name="json">Character JSON data</param>
    /// <param name="ownerId">User ID of owner (required for user templates)</param>
    /// <param name="isPublic">Whether the template should be publicly visible</param>
    /// <returns>Result containing success status, error message, and created template</returns>
    Task<TemplateImportResult> ImportFromJsonStringAsync(string json, string ownerId, bool isPublic = true, CancellationToken ct = default);
    
    /// <summary>
    /// Import all JSON files from the SampleCharacters directory as system templates.
    /// Uses upsert logic - existing templates with same name will be replaced.
    /// </summary>
    /// <returns>Number of templates imported</returns>
    Task<int> ImportAllSystemTemplatesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Import multiple character templates from JSON strings (batch import).
    /// Each file is processed individually, with failures not blocking other imports.
    /// </summary>
    /// <param name="files">Collection of (FileName, JsonContent) tuples</param>
    /// <param name="ownerId">User ID of owner for all imported templates</param>
    /// <param name="isPublic">Whether imported templates should be publicly visible</param>
    /// <returns>Tuple of (SuccessCount, FailureCount, ErrorMessages)</returns>
    Task<(int SuccessCount, int FailureCount, List<string> Errors)> ImportMultipleFromJsonAsync(
        IEnumerable<(string FileName, string Json)> files, 
        string ownerId, 
        bool isPublic = true, 
        CancellationToken ct = default);
    
    // ========================================
    // CRUD Methods
    // ========================================
    
    /// <summary>
    /// Create or update a template (upsert by Name + OwnerId).
    /// </summary>
    Task<CharacterTemplate> UpsertTemplateAsync(CharacterTemplate template, CancellationToken ct = default);
    
    /// <summary>
    /// Create a new template from a Character object.
    /// </summary>
    /// <param name="character">The character data</param>
    /// <param name="ownerId">User ID of the template owner</param>
    /// <param name="isPublic">Whether the template is publicly visible (default: true)</param>
    /// <returns>The created template</returns>
    Task<CharacterTemplate> CreateTemplateAsync(Character character, string ownerId, bool isPublic = true, CancellationToken ct = default);
    
    /// <summary>
    /// Update an existing template with new character data.
    /// </summary>
    /// <param name="templateId">ID of the template to update</param>
    /// <param name="character">Updated character data</param>
    /// <param name="isPublic">Optional: update the public visibility flag</param>
    /// <returns>The updated template, or null if not found</returns>
    Task<CharacterTemplate?> UpdateTemplateAsync(string templateId, Character character, bool? isPublic = null, CancellationToken ct = default);
    
    /// <summary>
    /// Delete a template by ID.
    /// </summary>
    Task<bool> DeleteTemplateAsync(string id, CancellationToken ct = default);
    
    // ========================================
    // Permission Helpers
    // ========================================
    
    /// <summary>
    /// Check if a user can edit/delete a template.
    /// Returns true if: user is the owner OR user is an admin.
    /// </summary>
    /// <param name="template">The template to check</param>
    /// <param name="userId">The user's ID</param>
    /// <param name="isAdmin">Whether the user has admin privileges</param>
    bool CanUserEditTemplate(CharacterTemplate template, string userId, bool isAdmin);
    
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
