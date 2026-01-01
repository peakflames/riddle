# Implementation Plan: Character Template Management

[Overview]
Enable signed-in users to create, edit, and delete character templates through both manual form entry and JSON import, with a public/private visibility system and admin controls.

This feature extends the existing CharacterTemplate system to support user contributions. Currently, only admin-level JSON imports are supported via `python build.py db import-templates`. The new system will add:
- A web-based UI for creating templates manually (reusing the existing `CharacterFormModal` pattern)
- JSON import capability with schema validation
- Public/Private visibility toggle (default: Public)
- Edit/Delete permissions (creator OR admin)
- Admin role defined via email list in `appsettings.json`
- Schema documentation viewer using markdown rendering

The existing template browsing and campaign import functionality remains unchanged, but import rules will now respect the public/private flag.

[Types]

### New Types and Modifications

**1. CharacterTemplate Model Enhancement** (`src/Riddle.Web/Models/CharacterTemplate.cs`)

Add new property:
```csharp
/// <summary>
/// Whether this template is publicly available for import by other users.
/// If false, only the owner can import it into their campaigns.
/// Default: true (public)
/// </summary>
public bool IsPublic { get; set; } = true;
```

**2. Admin Settings** (`src/Riddle.Web/appsettings.json`)

Add new configuration section:
```json
{
  "AdminSettings": {
    "AdminEmails": ["admin@example.com"]
  }
}
```

**3. AdminSettings Record** (new file: `src/Riddle.Web/Models/AdminSettings.cs`)
```csharp
namespace Riddle.Web.Models;

/// <summary>
/// Configuration for admin-level permissions.
/// Bound from appsettings.json "AdminSettings" section.
/// </summary>
public class AdminSettings
{
    public List<string> AdminEmails { get; set; } = new();
}
```

**4. IAdminService Interface** (new file: `src/Riddle.Web/Services/IAdminService.cs`)
```csharp
namespace Riddle.Web.Services;

public interface IAdminService
{
    bool IsAdmin(string? email);
    bool IsAdmin(System.Security.Claims.ClaimsPrincipal? user);
}
```

**5. AdminService Implementation** (new file: `src/Riddle.Web/Services/AdminService.cs`)
```csharp
// Implementation that checks user email against AdminSettings.AdminEmails
```

**6. TemplateImportResult Record** (can be in CharacterTemplateService.cs or separate)
```csharp
public record TemplateImportResult(
    bool Success,
    string? ErrorMessage,
    CharacterTemplate? Template
);
```

[Files]

### New Files to Create
| Path | Purpose |
|------|---------|
| `src/Riddle.Web/Models/AdminSettings.cs` | Admin configuration POCO |
| `src/Riddle.Web/Services/IAdminService.cs` | Admin check interface |
| `src/Riddle.Web/Services/AdminService.cs` | Admin check implementation |
| `src/Riddle.Web/Components/Characters/CharacterTemplateFormModal.razor` | Modal for creating/editing templates (form-based) |
| `src/Riddle.Web/Components/Characters/JsonImportModal.razor` | Modal for JSON import with validation |
| `src/Riddle.Web/Components/Characters/SchemaViewerModal.razor` | Modal displaying JSON schema documentation |
| `src/Riddle.Web/wwwroot/docs/character-schema.md` | Markdown documentation of Character JSON schema |
| `src/Riddle.Web/Migrations/{timestamp}_AddIsPublicToCharacterTemplates.cs` | EF migration for IsPublic column |

### Existing Files to Modify
| Path | Changes |
|------|---------|
| `src/Riddle.Web/Models/CharacterTemplate.cs` | Add `IsPublic` property |
| `src/Riddle.Web/appsettings.json` | Add `AdminSettings` section |
| `src/Riddle.Web/Program.cs` | Register AdminService, bind AdminSettings |
| `src/Riddle.Web/Services/ICharacterTemplateService.cs` | Add methods: `CreateTemplateAsync`, `UpdateTemplateAsync`, `CanUserEditTemplate`, `GetImportableTemplatesAsync`, `ImportFromJsonStringAsync` |
| `src/Riddle.Web/Services/CharacterTemplateService.cs` | Implement new methods, update queries for IsPublic |
| `src/Riddle.Web/Components/Pages/DM/CharacterTemplates.razor` | Add CRUD buttons, modals, schema viewer link |
| `src/Riddle.Web/Components/Characters/CharacterTemplatePickerModal.razor` | Filter by importable templates (public OR owned) |
| `src/Riddle.Web/Data/RiddleDbContext.cs` | Ensure IsPublic column is configured |
| `build.py` | Add `--owner-email` parameter to `import-templates` command |

[Functions]

### New Functions

**ICharacterTemplateService Interface:**
```csharp
Task<CharacterTemplate> CreateTemplateAsync(Character character, string ownerId, bool isPublic = true, CancellationToken ct = default);
Task<CharacterTemplate> UpdateTemplateAsync(string templateId, Character character, bool? isPublic = null, CancellationToken ct = default);
Task<List<CharacterTemplate>> GetImportableTemplatesAsync(string userId, CancellationToken ct = default);
Task<TemplateImportResult> ImportFromJsonStringAsync(string json, string ownerId, bool isPublic = true, CancellationToken ct = default);
bool CanUserEditTemplate(CharacterTemplate template, string userId, bool isAdmin);
```

**IAdminService Interface:**
```csharp
bool IsAdmin(string? email);
bool IsAdmin(ClaimsPrincipal? user);
```

**CharacterTemplates.razor page:**
```csharp
private async Task CreateTemplateManually()
private async Task CreateTemplateFromJson()
private async Task EditTemplate(CharacterTemplate template)
private async Task DeleteTemplate(CharacterTemplate template)
private void ShowSchemaViewer()
private bool CanEditTemplate(CharacterTemplate template)
```

### Modified Functions

**CharacterTemplateService.GetAllAvailableTemplatesAsync:**
- Update to include IsPublic in ordering/filtering

**CharacterTemplateService.ImportFromJsonAsync:**
- Update to accept and use the `isPublic` parameter
- Add JSON validation with detailed error messages

**build.py `import_templates` function:**
- Add `--owner-email` argument
- Lookup user ID from email before import
- Pass owner ID to service

[Classes]

### New Classes
| Class | File | Purpose |
|-------|------|---------|
| `AdminSettings` | `Models/AdminSettings.cs` | POCO for admin configuration |
| `AdminService` | `Services/AdminService.cs` | Checks admin permissions |
| `CharacterTemplateFormModal` | `Components/Characters/CharacterTemplateFormModal.razor` | Form UI for template create/edit (extends CharacterFormModal pattern) |
| `JsonImportModal` | `Components/Characters/JsonImportModal.razor` | JSON paste/upload with validation preview |
| `SchemaViewerModal` | `Components/Characters/SchemaViewerModal.razor` | Markdown schema display |

### Modified Classes
| Class | File | Changes |
|-------|------|---------|
| `CharacterTemplate` | `Models/CharacterTemplate.cs` | Add `IsPublic` property |
| `CharacterTemplateService` | `Services/CharacterTemplateService.cs` | Add CRUD methods, permission checks |
| `CharacterTemplates` (page) | `Pages/DM/CharacterTemplates.razor` | Add create/edit/delete UI, modals |
| `CharacterTemplatePickerModal` | `Components/Characters/CharacterTemplatePickerModal.razor` | Use `GetImportableTemplatesAsync` |

[Dependencies]

### No New NuGet Packages Required
The existing stack supports all requirements:
- Flowbite Blazor: Modals, forms, buttons, tables
- Markdig (if not present): May need for markdown rendering - check existing usage
- System.Text.Json: JSON parsing/validation

### Configuration Binding
```csharp
// In Program.cs
builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings"));
builder.Services.AddScoped<IAdminService, AdminService>();
```

[Testing]

### Manual Testing Checklist
1. **Template Creation (Manual):**
   - Sign in as regular user
   - Navigate to /dm/templates
   - Click "Add Template"
   - Fill form, verify template appears in list
   - Verify `IsPublic` defaults to true

2. **Template Creation (JSON Import):**
   - Click "Import JSON"
   - Paste valid JSON, verify preview
   - Paste invalid JSON, verify error message
   - Submit, verify template created

3. **Edit Permissions:**
   - As creator: can edit/delete own template
   - As non-creator non-admin: cannot edit/delete others' templates
   - As admin (email in appsettings): can edit/delete any template

4. **Import Rules:**
   - Create private template as User A
   - As User B, verify private template NOT in picker
   - As User A, verify private template IS in picker
   - Public templates visible to all in picker

5. **Schema Viewer:**
   - Click "View Schema"
   - Verify markdown renders correctly
   - Verify all Character fields documented

6. **build.py import-templates:**
   - Run `python build.py db import-templates --owner-email admin@example.com`
   - Verify templates have correct OwnerId
   - Verify templates are public by default

### Database Verification Commands
```bash
python build.py db templates                    # List all templates with IsPublic status
python build.py db "SELECT Id, Name, OwnerId, IsPublic FROM CharacterTemplates"
```

[Implementation Order]

Sequential steps to minimize conflicts and ensure successful integration.

1. **Create feature branch**
   - `git checkout -b feature/character-template-management`

2. **Add IsPublic column to CharacterTemplate**
   - Modify `CharacterTemplate.cs`
   - Create EF migration
   - Apply migration

3. **Add AdminSettings configuration**
   - Create `AdminSettings.cs` model
   - Add section to `appsettings.json`
   - Register in `Program.cs`

4. **Create AdminService**
   - Create `IAdminService.cs` interface
   - Create `AdminService.cs` implementation
   - Register in DI container

5. **Extend CharacterTemplateService**
   - Add `CreateTemplateAsync` method
   - Add `UpdateTemplateAsync` method
   - Add `GetImportableTemplatesAsync` method
   - Add `ImportFromJsonStringAsync` with validation
   - Add `CanUserEditTemplate` helper

6. **Create schema documentation**
   - Create `wwwroot/docs/character-schema.md`
   - Document all Character model fields with types and examples

7. **Create UI modals**
   - Create `CharacterTemplateFormModal.razor` (adapt from CharacterFormModal)
   - Create `JsonImportModal.razor`
   - Create `SchemaViewerModal.razor`

8. **Update CharacterTemplates.razor page**
   - Add "Add Template" and "Import JSON" buttons
   - Add "View Schema" button
   - Add edit/delete buttons per row (conditional on permissions)
   - Wire up all modals
   - Add owner display column
   - Add public/private badge

9. **Update CharacterTemplatePickerModal**
   - Switch from `GetAllAvailableTemplatesAsync` to `GetImportableTemplatesAsync`

10. **Update build.py**
    - Add `--owner-email` parameter to `import-templates`
    - Look up user ID from email
    - Pass to service

11. **Testing and verification**
    - Run through manual testing checklist
    - Verify all permission scenarios
    - Verify database state

12. **Documentation update**
    - Update `docs/memory_aid.md` with any gotchas discovered
