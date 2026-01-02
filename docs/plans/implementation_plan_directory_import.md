# Implementation Plan: Directory/Multi-File JSON Import

[Overview]
Add a "Import Directory" button to the Character Templates page that allows users to select multiple JSON files (or an entire folder) from their local machine and batch-import them as character templates.

This extends the existing single-file JSON import with a multi-file upload capability using Blazor's `InputFile` component with the `multiple` attribute. The feature enables admins on remote machines to quickly import a collection of character template JSON files without importing them one-by-one.

[Types]
No new types are required. The existing `TemplateImportResult` record handles individual import results.

A new callback result type will be used to report batch import progress:
```csharp
// BatchImportResult - transient, used only in the modal component
public record BatchImportResult(int SuccessCount, int FailureCount, List<string> Errors);
```

[Files]
Files to be modified or created:

**New Files:**
- `src/Riddle.Web/Components/Characters/DirectoryImportModal.razor` - New modal component for multi-file selection and import

**Modified Files:**
- `src/Riddle.Web/Components/Pages/DM/CharacterTemplates.razor` - Add "Import Directory" button and wire up new modal
- `src/Riddle.Web/Services/ICharacterTemplateService.cs` - Add batch import method signature
- `src/Riddle.Web/Services/CharacterTemplateService.cs` - Implement batch import method

[Functions]
Functions to be added or modified:

**New Functions:**
- `ICharacterTemplateService.ImportMultipleFromJsonAsync(IEnumerable<(string FileName, string Json)> files, string ownerId, bool isPublic)` - Batch import method returning aggregate results

**Modified Functions:**
- None - existing `ImportFromJsonStringAsync` will be reused internally

[Classes]
Class modifications:

**Modified Classes:**
- `CharacterTemplateService` - Add implementation of `ImportMultipleFromJsonAsync`

**New Components:**
- `DirectoryImportModal` - Blazor component with:
  - `InputFile` with `multiple` and optional `webkitdirectory` attributes
  - File list display showing selected files
  - Progress indicator during import
  - Results summary (success/failure counts)
  - Visibility toggle (public/private)

[Dependencies]
No new dependencies required.

Blazor's built-in `InputFile` component handles multi-file selection. The `webkitdirectory` attribute (for folder selection) works in Chrome/Edge but may have limited support in other browsers - the UI will provide fallback multi-file selection.

[Testing]
Verification approach:

1. **Manual Testing:**
   - Navigate to `/dm/templates`
   - Click "Import Directory" button
   - Select multiple JSON files from local machine
   - Verify all valid files are imported as templates
   - Verify error handling for invalid JSON files
   - Verify visibility toggle (public/private) applies to all imports

2. **Edge Cases:**
   - Empty file selection
   - Mix of valid and invalid JSON files
   - Duplicate template names (should upsert)
   - Large batch (10+ files)

[Implementation Order]
Implementation sequence to minimize conflicts:

1. Add `ImportMultipleFromJsonAsync` method signature to `ICharacterTemplateService`
2. Implement `ImportMultipleFromJsonAsync` in `CharacterTemplateService`
3. Create `DirectoryImportModal.razor` component
4. Add "Import Directory" button to `CharacterTemplates.razor` page
5. Wire up modal show/hide and import handler
6. Test and verify functionality
