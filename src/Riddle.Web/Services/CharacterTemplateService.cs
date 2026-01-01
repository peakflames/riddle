using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for managing character templates.
/// Templates are reusable character definitions that DMs can import into campaigns.
/// </summary>
public class CharacterTemplateService : ICharacterTemplateService
{
    private readonly RiddleDbContext _db;
    private readonly ICampaignService _campaignService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CharacterTemplateService> _logger;
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CharacterTemplateService(
        RiddleDbContext db,
        ICampaignService campaignService,
        IWebHostEnvironment env,
        ILogger<CharacterTemplateService> logger)
    {
        _db = db;
        _campaignService = campaignService;
        _env = env;
        _logger = logger;
    }

    // ========================================
    // Query Methods
    // ========================================

    public async Task<List<CharacterTemplate>> GetSystemTemplatesAsync(CancellationToken ct = default)
    {
        return await _db.CharacterTemplates
            .Include(t => t.Owner)
            .Where(t => t.OwnerId == null)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<CharacterTemplate>> GetUserTemplatesAsync(string userId, CancellationToken ct = default)
    {
        return await _db.CharacterTemplates
            .Where(t => t.OwnerId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<CharacterTemplate>> GetAllAvailableTemplatesAsync(string userId, CancellationToken ct = default)
    {
        return await _db.CharacterTemplates
            .Include(t => t.Owner)
            .Where(t => t.OwnerId == null || t.OwnerId == userId)
            .OrderBy(t => t.OwnerId == null ? 0 : 1) // System templates first
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<CharacterTemplate>> GetImportableTemplatesAsync(string userId, CancellationToken ct = default)
    {
        // Return all public templates + user's own templates (regardless of public flag)
        return await _db.CharacterTemplates
            .Include(t => t.Owner)
            .Where(t => t.IsPublic || t.OwnerId == userId)
            .OrderBy(t => t.OwnerId == null ? 0 : 1) // System templates first
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<CharacterTemplate?> GetTemplateByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.CharacterTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<CharacterTemplate?> GetSystemTemplateByNameAsync(string name, CancellationToken ct = default)
    {
        var normalizedName = NormalizeName(name);
        return await _db.CharacterTemplates
            .Where(t => t.OwnerId == null)
            .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedName, ct);
    }

    // ========================================
    // Import Methods
    // ========================================

    public async Task<CharacterTemplate> ImportFromJsonAsync(
        string json, 
        string? ownerId = null, 
        string? sourceFile = null, 
        CancellationToken ct = default)
    {
        // Parse the character from JSON
        var character = JsonSerializer.Deserialize<Character>(json, _jsonOptions);
        if (character == null)
        {
            throw new ArgumentException("Invalid character JSON", nameof(json));
        }

        var normalizedName = NormalizeName(character.Name);

        // Find existing template by Name + OwnerId
        var existing = await _db.CharacterTemplates
            .FirstOrDefaultAsync(t => 
                t.OwnerId == ownerId && 
                t.Name.ToLower() == normalizedName, ct);

        if (existing != null)
        {
            // Update existing (upsert)
            _logger.LogInformation("Updating existing template: {Name} (ID: {Id})", existing.Name, existing.Id);
            
            existing.CharacterJson = json;
            existing.Race = character.Race;
            existing.Class = character.Class;
            existing.Level = character.Level;
            existing.SourceFile = sourceFile;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            _logger.LogInformation("Creating new template: {Name}", character.Name);
            
            existing = new CharacterTemplate
            {
                Name = character.Name,
                OwnerId = ownerId,
                CharacterJson = json,
                Race = character.Race,
                Class = character.Class,
                Level = character.Level,
                SourceFile = sourceFile,
                IsPublic = true // Default to public for system imports
            };
            _db.CharacterTemplates.Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<TemplateImportResult> ImportFromJsonStringAsync(
        string json,
        string ownerId,
        bool isPublic = true,
        CancellationToken ct = default)
    {
        // Validate JSON is not empty
        if (string.IsNullOrWhiteSpace(json))
        {
            return new TemplateImportResult(false, "JSON content is empty.", null);
        }

        // Try to parse the JSON
        Character? character;
        try
        {
            character = JsonSerializer.Deserialize<Character>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format in template import");
            return new TemplateImportResult(false, $"Invalid JSON format: {ex.Message}", null);
        }

        if (character == null)
        {
            return new TemplateImportResult(false, "Failed to parse character from JSON.", null);
        }

        // Validate required fields
        var validationErrors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(character.Name))
        {
            validationErrors.Add("Character name is required.");
        }

        if (validationErrors.Count > 0)
        {
            return new TemplateImportResult(false, string.Join(" ", validationErrors), null);
        }

        // Create the template
        try
        {
            var template = await CreateTemplateAsync(character, ownerId, isPublic, ct);
            _logger.LogInformation("Successfully imported template '{Name}' for user {UserId}", character.Name, ownerId);
            return new TemplateImportResult(true, null, template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template from JSON import");
            return new TemplateImportResult(false, $"Error creating template: {ex.Message}", null);
        }
    }

    public async Task<int> ImportAllSystemTemplatesAsync(CancellationToken ct = default)
    {
        // Find the SampleCharacters directory
        var sampleCharsPath = Path.Combine(_env.ContentRootPath, "Data", "SampleCharacters");
        
        if (!Directory.Exists(sampleCharsPath))
        {
            _logger.LogWarning("SampleCharacters directory not found at: {Path}", sampleCharsPath);
            return 0;
        }

        var jsonFiles = Directory.GetFiles(sampleCharsPath, "*.json");
        _logger.LogInformation("Found {Count} JSON files in {Path}", jsonFiles.Length, sampleCharsPath);

        var importedCount = 0;
        foreach (var filePath in jsonFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath, ct);
                var fileName = Path.GetFileName(filePath);
                
                await ImportFromJsonAsync(json, ownerId: null, sourceFile: fileName, ct);
                importedCount++;
                
                _logger.LogInformation("Imported: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import {FilePath}", filePath);
            }
        }

        _logger.LogInformation("Successfully imported {Count} character templates", importedCount);
        return importedCount;
    }

    // ========================================
    // CRUD Methods
    // ========================================

    public async Task<CharacterTemplate> UpsertTemplateAsync(CharacterTemplate template, CancellationToken ct = default)
    {
        var normalizedName = NormalizeName(template.Name);

        // Find existing template by Name + OwnerId
        var existing = await _db.CharacterTemplates
            .FirstOrDefaultAsync(t => 
                t.OwnerId == template.OwnerId && 
                t.Name.ToLower() == normalizedName, ct);

        if (existing != null)
        {
            // Update existing
            existing.CharacterJson = template.CharacterJson;
            existing.Race = template.Race;
            existing.Class = template.Class;
            existing.Level = template.Level;
            existing.SourceFile = template.SourceFile;
            existing.IsPublic = template.IsPublic;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            _db.CharacterTemplates.Add(template);
            existing = template;
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<CharacterTemplate> CreateTemplateAsync(
        Character character,
        string ownerId,
        bool isPublic = true,
        CancellationToken ct = default)
    {
        // Serialize the character to JSON
        var json = JsonSerializer.Serialize(character, _jsonOptions);

        var template = new CharacterTemplate
        {
            Name = character.Name,
            OwnerId = ownerId,
            CharacterJson = json,
            Race = character.Race,
            Class = character.Class,
            Level = character.Level,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CharacterTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created template '{Name}' (ID: {Id}) for user {UserId}, public: {IsPublic}",
            template.Name, template.Id, ownerId, isPublic);
        
        return template;
    }

    public async Task<CharacterTemplate?> UpdateTemplateAsync(
        string templateId,
        Character character,
        bool? isPublic = null,
        CancellationToken ct = default)
    {
        var template = await _db.CharacterTemplates.FirstOrDefaultAsync(t => t.Id == templateId, ct);
        if (template == null)
        {
            _logger.LogWarning("Template not found for update: {TemplateId}", templateId);
            return null;
        }

        // Serialize the updated character to JSON
        var json = JsonSerializer.Serialize(character, _jsonOptions);

        template.Name = character.Name;
        template.CharacterJson = json;
        template.Race = character.Race;
        template.Class = character.Class;
        template.Level = character.Level;
        template.UpdatedAt = DateTime.UtcNow;
        
        if (isPublic.HasValue)
        {
            template.IsPublic = isPublic.Value;
        }

        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Updated template '{Name}' (ID: {Id}), public: {IsPublic}",
            template.Name, template.Id, template.IsPublic);
        
        return template;
    }

    public async Task<bool> DeleteTemplateAsync(string id, CancellationToken ct = default)
    {
        var template = await _db.CharacterTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template == null)
        {
            return false;
        }

        _db.CharacterTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ========================================
    // Campaign Integration
    // ========================================

    public async Task<Character?> CopyToCampaignAsync(string templateId, string campaignId, CancellationToken ct = default)
    {
        // Get the template
        var template = await GetTemplateByIdAsync(templateId, ct);
        if (template == null)
        {
            _logger.LogWarning("Template not found: {TemplateId}", templateId);
            return null;
        }

        // Parse campaign ID
        if (!Guid.TryParse(campaignId, out var campaignGuid))
        {
            _logger.LogWarning("Invalid campaign ID format: {CampaignId}", campaignId);
            return null;
        }

        // Get the campaign
        var campaign = await _campaignService.GetCampaignAsync(campaignGuid, ct);
        if (campaign == null)
        {
            _logger.LogWarning("Campaign not found: {CampaignId}", campaignId);
            return null;
        }

        // Deep copy the character from template (fresh ID)
        var character = template.Character;
        character.Id = Guid.CreateVersion7().ToString(); // Fresh ID
        character.PlayerId = null; // Not claimed yet
        character.PlayerName = null;

        // Add to campaign's party
        var partyState = campaign.PartyState;
        partyState.Add(character);
        campaign.PartyState = partyState;

        await _campaignService.UpdateCampaignAsync(campaign, ct);
        
        _logger.LogInformation("Copied template '{TemplateName}' to campaign '{CampaignName}' as character ID: {CharacterId}",
            template.Name, campaign.Name, character.Id);

        return character;
    }

    // ========================================
    // Permission Helpers
    // ========================================

    public bool CanUserEditTemplate(CharacterTemplate template, string userId, bool isAdmin)
    {
        // Admins can edit any template
        if (isAdmin)
        {
            return true;
        }

        // Non-admins can only edit their own templates
        return template.OwnerId == userId;
    }

    // ========================================
    // Helper Methods
    // ========================================

    /// <summary>
    /// Normalize a name for comparison (lowercase, trim whitespace).
    /// </summary>
    private static string NormalizeName(string name)
    {
        return name?.ToLowerInvariant().Trim() ?? string.Empty;
    }
}
