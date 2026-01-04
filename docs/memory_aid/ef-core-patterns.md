# EF Core & Database Patterns

> **Keywords:** DbContext, migrations, SaveChangesAsync, JSON serialization, NotMapped, static Dictionary, persistence
> **Related:** [Blazor Components](./blazor-components.md), [LLM Tools](./llm-tools.md)

This document covers Entity Framework Core patterns, database persistence, and state management gotchas.

---

## EF Core Basics

- When creating services that use DbContext, inject `RiddleDbContext` directly
- For computed properties on models (like `PartyState` backed by `PartyStateJson`), use `[NotMapped]` attribute
- Always call `SaveChangesAsync()` after mutations

---

## Database Issues

- If migrations fail due to existing tables not matching, delete `riddle.db` and re-run `dotnet ef database update`
- Always use `dotnet ef` commands from the repo root with `--project src/Riddle.Web`

---

## CRITICAL: EF Core Migration Default Values on Existing Data

When adding a new column with a default value to a table that already has data, **existing rows imported BEFORE the migration may have NULL values** instead of the default. This happens because:

1. The migration creates the column with `defaultValue: X`
2. But if data was imported via build.py commands that bypass EF Core tracking (or via raw SQL), the default constraint only applies to new INSERTs

**Symptom:** After migration, query shows `NULL` for the new column on existing rows, even though the migration specified a default.

**Fix:** Run SQL to update existing rows after migration:
```bash
python build.py db "UPDATE CharacterTemplates SET IsPublic = 1 WHERE IsPublic IS NULL"
```

**Prevention:** In service code that creates records, always explicitly set the property value rather than relying on database defaults:
```csharp
// ✅ CORRECT - Explicitly set IsPublic
var template = new CharacterTemplate
{
    Name = character.Name,
    IsPublic = true  // Don't rely on DB default
};
```

---

## CRITICAL: JSON-Backed [NotMapped] Property Pattern

When a model uses `[NotMapped]` properties that serialize/deserialize JSON (like `PartyState` backed by `PartyStateJson`), **each access to the getter deserializes JSON fresh** - modifications to a previous access are LOST!

```csharp
// ❌ WRONG - modifications lost because second access creates new list
var character = campaign.PartyState.FirstOrDefault(c => c.Id == id);
character.PlayerId = userId;  // Modifies object in list we'll discard
campaign.PartyState = campaign.PartyState.ToList();  // Deserializes AGAIN - changes gone!
```

```csharp
// ✅ CORRECT - get list ONCE, modify, set back
var partyState = campaign.PartyState;  // Get once and hold reference
var character = partyState.FirstOrDefault(c => c.Id == id);
character.PlayerId = userId;  // Modifies object in our held reference
campaign.PartyState = partyState;  // Set modified list back (triggers serialization)
```

**Rule:** Always capture JSON-backed list properties in a local variable before modifying.

---

## CRITICAL: Persist State to Database, Not In-Memory

**Never use `static Dictionary` for state that must survive server restart.** Blazor Server apps restart when the server reboots, code is deployed, or the app pool recycles - all in-memory static state is LOST.

❌ **WRONG - In-memory cache:**
```csharp
// Lost on server restart!
private static readonly Dictionary<string, CombatantInfo> _combatantCache = new();
```

✅ **CORRECT - Persist to database:**
```csharp
// CombatEncounter model with persisted Combatants dictionary
public class CombatEncounter
{
    public Dictionary<string, CombatantDetails> Combatants { get; set; } = new();
}
```

**Symptom:** After browser refresh, stale/missing data appears even though operations "succeeded" before restart.

---

## CharacterTemplates Pattern

**Character templates are a picklist** for DMs to import pre-made characters into campaigns. The architecture separates reusable templates (in the database) from campaign-specific characters (embedded JSON in `PartyStateJson`).

**Key Design Decisions:**
1. **Unique constraint**: `Name + OwnerId` (allows same-named characters for different owners)
2. **System templates**: `OwnerId = NULL` (available to all DMs)
3. **User templates**: `OwnerId = userId` (private to that DM, OR public to all if `IsPublic = true`)
4. **Shadow columns**: `Race`, `Class`, `Level` are denormalized from JSON for filtering/sorting
5. **JSON import**: `build.py db import-templates` syncs `SampleCharacters/*.json` → database

**Entity Pattern:**
```csharp
public class CharacterTemplate
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string Name { get; set; } = string.Empty;
    public string? OwnerId { get; set; }              // NULL = system template
    public string CharacterJson { get; set; } = "{}";  // Full Character model serialized
    
    // Shadow columns (denormalized for indexing/display)
    public string? Race { get; set; }
    public string? Class { get; set; }
    public int Level { get; set; } = 1;
    public string? SourceFile { get; set; }           // Original JSON filename
    
    [NotMapped]
    public Character Character => JsonSerializer.Deserialize<Character>(CharacterJson)!;
}
```

**Upsert Pattern:**
```csharp
// Find existing by Name + OwnerId (case-insensitive)
var existing = await _db.CharacterTemplates
    .FirstOrDefaultAsync(t => t.OwnerId == ownerId && t.Name.ToLower() == normalizedName);

if (existing != null) { /* update fields */ }
else { _db.CharacterTemplates.Add(newTemplate); }
```

**Copy to Campaign:**
```csharp
var character = template.Character;
character.Id = Guid.CreateVersion7().ToString();  // Fresh ID!
character.PlayerId = null;  // Not claimed yet
campaign.PartyState.Add(character);
