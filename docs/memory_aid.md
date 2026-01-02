# Memory Aid - Lessons Learned

> **Related Document:** See [`developer_rules.md`](./developer_rules.md) for prescriptive guidelines and project structure.

This document captures gotchas, patterns, and hard-won knowledge discovered through development. Items marked **CRITICAL** have caused significant debugging time.

---

## Documentation Structure

### `docs/` Directory Organization

- **`docs/`** - Active reference documentation (developer_rules, memory_aid, e2e_testing_philosophy, flowbite_blazor_docs)
- **`docs/plans/`** - Implementation plans and roadmaps (archived planning documents)
- **`docs/signalr/`** - SignalR architecture documentation (events, groups, flows)
- **`docs/design/`** - UX and feature design specs
- **`docs/verification/`** - Phase completion checklists
- **`docs/surveys/`** - User research (DM/Player surveys)

---

## Framework & Platform Gotchas

### Blazor Server vs WASM
- This project uses **Blazor Server** with `InteractiveServer` render mode, NOT WASM
- The Flowbite Blazor Admin Dashboard reference is WASM - don't blindly copy App.razor/Program.cs
- Interactive pages need `@rendermode InteractiveServer` directive

### CRITICAL: HttpClient Not Available in Blazor Server by Default

**Problem:** `HttpClient` is NOT registered in DI by default for Blazor Server apps. Using `@inject HttpClient Http` causes runtime exceptions when the component renders.

**Error:** `InvalidOperationException: Cannot provide a value for property 'Http' on type 'YourComponent'. There is no registered service of type 'System.Net.Http.HttpClient'.`

**Root cause:** Blazor WASM automatically configures `HttpClient` for same-origin requests. Blazor Server does NOT because server-side code can access files/APIs directly without HTTP.

**Fix for loading files from wwwroot:**

❌ **WRONG - HttpClient (WASM pattern):**
```csharp
@inject HttpClient Http

var content = await Http.GetStringAsync("docs/my-file.md");  // Fails in Blazor Server!
```

✅ **CORRECT - IWebHostEnvironment (Server pattern):**
```csharp
@inject IWebHostEnvironment WebHostEnvironment

var filePath = Path.Combine(WebHostEnvironment.WebRootPath, "docs", "my-file.md");
var content = await File.ReadAllTextAsync(filePath);  // Works in Blazor Server
```

**Alternative:** If you need HttpClient for external API calls, register it explicitly in Program.cs:
```csharp
builder.Services.AddHttpClient();  // Basic registration
// Or for typed clients:
builder.Services.AddHttpClient<IMyApiClient, MyApiClient>();
```

### .NET 10 Blazor Server Setup
- Use `blazor.web.js` (not `blazor.server.js`) with `@Assets["_framework/blazor.web.js"]` syntax
- App.razor needs: `<ResourcePreloader />`, `<ImportMap />`, `<ReconnectModal />`
- Program.cs: Use `MapStaticAssets()` instead of `UseStaticFiles()`
- For non-development runs: Add `builder.WebHost.UseStaticWebAssets()` before building
- Generate reference: `dotnet new blazor -int Server` in tmp/ folder for correct patterns

### Package Management
- For .NET 10 preview packages: `dotnet add package {Name} --prerelease`
- Use `--version 10.0.1` for specific versions

### Windows Shell Commands
- Don't use Unix commands like `find /i` - use PowerShell: `Select-String -Pattern "error"`
- `sqlite3` may not be installed - verify database via EF Core or migration files

### UUID/GUID
- Use `Guid.CreateVersion7()` for time-sortable IDs (requires .NET 9+)

---

## Flowbite Blazor Patterns

### Component API Quick Reference
- **SpinnerSize**: Use `SpinnerSize.Sm`, `SpinnerSize.Lg`, `SpinnerSize.Xl` - NOT `SpinnerSize.Small`, `SpinnerSize.Large`, etc.
- **BadgeColor**: Requires explicit `@using Flowbite.Blazor.Enums` in some contexts. Note: `BadgeColor.Dark` does NOT exist - use `BadgeColor.Gray` for dark tones
- **ButtonColor**: Use `ButtonColor.Green` for success-style buttons, NOT `ButtonColor.Success` (which doesn't exist)
- **ButtonSize**: Use `ButtonSize.Small`, `ButtonSize.ExtraSmall` - NOT `ButtonSize.Sm`, `ButtonSize.Xs`
- **CardSize**: Use `CardSize.ExtraLarge`, not `CardSize.XLarge`
- **Alert Component**: Use **`TextEmphasis`** and **`Text`** parameters, NOT ChildContent:
  ```razor
  @* ✅ CORRECT - Use TextEmphasis and Text parameters *@
  <Alert Color="AlertColor.Failure" TextEmphasis="Error!" Text="@_errorMessage" />
  
  @* ❌ WRONG - ChildContent renders empty green box *@
  <Alert Color="AlertColor.Success">@_successMessage</Alert>
  ```
  Color values: `AlertColor.Failure` (red), `AlertColor.Success` (green), `AlertColor.Info` (blue), `AlertColor.Warning` (yellow)
- **EditForm Context Conflicts**: When EditForm is inside AuthorizeView, add `Context="editContext"` parameter to EditForm to avoid context name collision
- **Icon Components**: Use Flowbite icon components (e.g., `<BookOpenIcon Class="w-5 h-5" />`) from Flowbite.Blazor.Icons namespace
- **TableRow onclick**: Flowbite `TableRow` component does NOT support `@onclick` event handlers - use click handlers on inner elements (e.g., checkbox, button) instead

Always check Flowbite Blazor docs or reference dashboard project for exact API signatures.

### CRITICAL: Textarea Binding in TabPanels

**Flowbite Blazor `<Textarea>` does NOT bind correctly** when placed inside `<TabPanel>` components. Both `@bind-Value` and explicit `Value`/`ValueChanged` patterns fail - the model values remain empty/null when the form submits.

**Workaround:** Use native HTML `<textarea>` with `@bind` and Tailwind classes for styling:
```razor
<!-- ❌ BROKEN - Flowbite Textarea in TabPanel -->
<Textarea Id="personality" @bind-Value="_model.PersonalityTraits" Rows="2" />

<!-- ✅ WORKS - Native HTML textarea with @bind -->
<textarea id="personality" @bind="_model.PersonalityTraits" rows="2" 
  class="block p-2.5 w-full text-sm text-gray-900 bg-gray-50 rounded-lg border border-gray-300 
         focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-700 dark:border-gray-600 
         dark:placeholder-gray-400 dark:text-white dark:focus:ring-primary-500 dark:focus:border-primary-500">
</textarea>
```

Note: Flowbite Textarea works fine in non-TabPanel contexts (like simple modals or forms).

---

## EF Core & Database Patterns

### EF Core Basics
- When creating services that use DbContext, inject `RiddleDbContext` directly
- For computed properties on models (like `PartyState` backed by `PartyStateJson`), use `[NotMapped]` attribute
- Always call `SaveChangesAsync()` after mutations

### Database Issues
- If migrations fail due to existing tables not matching, delete `riddle.db` and re-run `dotnet ef database update`
- Always use `dotnet ef` commands from the repo root with `--project src/Riddle.Web`

### CRITICAL: EF Core Migration Default Values on Existing Data

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

### CRITICAL: JSON-Backed [NotMapped] Property Pattern

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

### CRITICAL: Persist State to Database, Not In-Memory

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

## Blazor Component Patterns

### Authentication Patterns
- Get current user ID via `AuthenticationStateProvider.GetAuthenticationStateAsync()`
- User claims are accessed via `user.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- Always check `user.Identity?.IsAuthenticated == true` before accessing claims
- Use `<AuthorizeView>` with `<Authorized>` and `<NotAuthorized>` sections for protected pages

### Combat State Management Pattern

**Combat state must be loaded from database on page initialization**, not just from SignalR events. The `CombatTracker` component receives state as a parameter from the parent page, so:

1. **On page load/refresh**: The parent page (e.g., `Campaign.razor`) must call `CombatService.GetCombatStateAsync()` to load persisted combat state
2. **During runtime**: SignalR events (`CombatStarted`, `TurnAdvanced`, `CombatEnded`) update the state in real-time
3. **Issue symptoms**: Stale combat data after refresh, or `end_combat` not clearing UI

```csharp
// ✅ CORRECT - Load combat state on page initialization
private async Task LoadCampaignAsync()
{
    campaign = await CampaignService.GetCampaignAsync(CampaignId);
    if (campaign != null)
    {
        _combatState = await CombatService.GetCombatStateAsync(CampaignId);  // Load from DB!
    }
}
```

### CRITICAL: [Parameter] Mutation Anti-Pattern

**Never directly modify `[Parameter]` properties in child components.** Parameters are owned by the parent component - modifying them locally creates a disconnected copy that doesn't trigger parent re-renders.

❌ **WRONG - Modifying parameter directly:**
```csharp
// Child component
[Parameter] public CombatStatePayload? Combat { get; set; }
[Parameter] public EventCallback<CombatStatePayload?> CombatChanged { get; set; }

_hubConnection.On(GameHubEvents.CombatEnded, async () =>
{
    Combat = null;  // WRONG! This creates a local copy, parent not notified properly
    await CombatChanged.InvokeAsync(null);
    await InvokeAsync(StateHasChanged);  // Re-renders with stale local state
});
```

✅ **CORRECT - Only invoke callback, let parent manage state:**
```csharp
_hubConnection.On(GameHubEvents.CombatEnded, async () =>
{
    // Don't modify Combat directly - it's a [Parameter] owned by parent
    // Just invoke the callback to notify parent to update its state
    await InvokeAsync(async () =>
    {
        await CombatChanged.InvokeAsync(null);
    });
});
```

**Key principle:** Child components should ONLY notify parents via `EventCallback` - never modify `[Parameter]` values directly. The parent sets the parameter value, which flows down to the child via normal Blazor parameter binding.

**Symptom:** UI doesn't update even though callbacks are invoked and SignalR events are received.

---

## LLM Tool Patterns

### Parameter Naming: Use Names, Not IDs

When defining LLM tool parameters that reference characters or combatants, **always use `character_name` (not `character_id`)** in tool definitions. LLMs think in natural language and will always prefer human-readable names.

| Bad Parameter Name | Good Parameter Name | Description |
|-------------------|---------------------|-------------|
| `character_id` | `character_name` | "The character's name (e.g., 'Elara Moonshadow')" |
| `character_ids` | `character_names` | "Array of character names to query" |
| `combatant_id` | `combatant_name` | "Name of the combatant (e.g., 'Goblin 1')" |

**Backward Compatibility:** Tool executors should accept BOTH the new `_name` parameter AND legacy `_id` parameter:
```csharp
// Accept both character_name (preferred) and character_id (legacy)
if (!args.TryGetProperty("character_name", out var nameElement) && 
    !args.TryGetProperty("character_id", out nameElement))
{
    return JsonSerializer.Serialize(new { error = "Missing required parameter: character_name" });
}
```

### Parameter Flexibility: Name Normalization

When LLM tools accept character identifiers, **always match by both ID (GUID) and Name with normalization**. LLMs naturally use display names, not GUIDs, and often transform separators:

**Problem:** LLMs often replace spaces with underscores:
- Stored name: `Elara Moonshadow`  
- LLM sends: `Elara_Moonshadow`

**Solution:** Normalize names for comparison by converting separators:

```csharp
// ✅ CORRECT - Match by ID OR normalized Name
var character = campaign.PartyState.FirstOrDefault(c => 
    c.Id == characterNameOrId || 
    NormalizeName(c.Name) == NormalizeName(characterNameOrId));

// Helper method
private static string NormalizeName(string name)
{
    return name
        .ToLowerInvariant()
        .Replace('_', ' ')
        .Replace('-', ' ')
        .Trim();
}
```

**Response Best Practice:** Include both `character_name` and `character_id` in tool responses:
```csharp
return JsonSerializer.Serialize(new { 
    success = true, 
    character_name = character.Name,  // Human-readable
    character_id = character.Id,       // For internal reference
    key, 
    updated = true 
});
```

### update_character_state Must Search Multiple Data Sources

The `update_character_state` tool must search **both** `PartyState` (PCs) and `ActiveCombat.Combatants` (enemies/allies). Enemy combatants have IDs like `enemy_2a99c5387ee84ddd94f4c887158fa496` that won't exist in `PartyState`.

**Problem:** LLM updates enemy HP during combat → tool returns "Character not found"
- Enemies are stored in `CampaignInstance.ActiveCombat.Combatants` dictionary
- PCs are stored in `CampaignInstance.PartyState` list

**Solution:** Check both data sources in `update_character_state`:

```csharp
// ✅ CORRECT - Check PartyState first, then combat combatants
var character = await _stateService.GetCharacterAsync(campaignId, characterId, ct);

if (character == null)
{
    var combatState = await _combatService.GetCombatStateAsync(campaignId, ct);
    if (combatState?.IsActive == true)
    {
        var combatant = combatState.TurnOrder.FirstOrDefault(c => 
            c.Id == characterId || 
            NormalizeName(c.Name) == NormalizeName(characterId));
        
        if (combatant != null)
        {
            // Route to CombatService for updates (supports HP, initiative)
            return await UpdateCombatantStateAsync(campaignId, combatant, key, valueElement, ct);
        }
    }
}
```

**Note:** Combat combatants only support `current_hp` and `initiative` updates (no conditions/status_notes).

### CRITICAL: ToolExecutor Must Broadcast SignalR Events

When `ToolExecutor` updates state via services (e.g., `GameStateService.UpdateCharacterAsync`), it **must also call `NotificationService`** to broadcast changes to connected clients. State updates alone won't trigger UI refreshes - SignalR events are required.

❌ **WRONG - Updates database but UI never refreshes:**
```csharp
await _stateService.UpdateCharacterAsync(campaignId, character, ct);
// Missing SignalR notification! Dashboards won't update.
```

✅ **CORRECT - Update database AND broadcast to clients:**
```csharp
await _stateService.UpdateCharacterAsync(campaignId, character, ct);

// Broadcast state change to all connected clients (DM + Players)
var payload = new CharacterStatePayload(character.Id, key, valueElement.ToString());
await _notificationService.NotifyCharacterStateUpdatedAsync(campaignId, payload, ct);
```

**Affected tools:** `update_character_state`, and any combatant state updates in `UpdateCombatantStateAsync`.

**Symptom:** Tool returns success but dashboards show stale data until manual refresh.

### Flexible Type Parsing

LLMs often send integers as quoted strings (e.g., `"15"` instead of `15`). Tool executors must handle both:

```csharp
// ✅ CORRECT - Parse int from number OR string
private static (bool success, int value, string? error) ParseIntValue(JsonElement element, string fieldName)
{
    if (element.ValueKind == JsonValueKind.Number)
        return (true, element.GetInt32(), null);
    
    if (element.ValueKind == JsonValueKind.String)
    {
        if (int.TryParse(element.GetString(), out var parsed))
            return (true, parsed, null);
        return (false, 0, $"Invalid {fieldName}: not a valid integer");
    }
    
    return (false, 0, $"Invalid {fieldName}: expected integer or string");
}
```

**Apply to:** `current_hp`, `initiative`, and any other integer fields the LLM updates.

---

## Architecture Patterns

### CharacterTemplates Pattern

**Character templates are a picklist** for DMs to import pre-made characters into campaigns. The architecture separates reusable templates (in the database) from campaign-specific characters (embedded JSON in `PartyStateJson`).

**Key Design Decisions:**
1. **Unique constraint**: `Name + OwnerId` (allows same-named characters for different owners)
2. **System templates**: `OwnerId = NULL` (available to all DMs)
3. **User templates**: `OwnerId = userId` (private to that DM)
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
```

### Dynamic App Version Display

**Don't hardcode version strings in UI components.** Use reflection to read from `AssemblyInformationalVersionAttribute`:

```csharp
@using System.Reflection

private string AppInformationalVersion
{
    get
    {
        var appVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
        
        // Strip Git commit hash suffix (e.g., "0.9.0+abc123" → "0.9.0")
        var plusCharIndex = appVersion.IndexOf('+');
        if (plusCharIndex > -1)
        {
            appVersion = appVersion.Substring(0, plusCharIndex);
        }
        return appVersion;
    }
}
```

Then in markup: `<Badge>v@($"{AppInformationalVersion}")</Badge>`

Version is set in `.csproj` via `<Version>` or `<InformationalVersion>` property.

---

## Testing & Verification

### Testing Approach
- User prefers functional/integration tests over unit tests
- Use Playwright MCP for browser-based verification
- Create API endpoints for testing when Blazor interactivity has issues

### CRITICAL: EF Core Integration Testing with WebApplicationFactory

When testing Blazor Server apps with `WebApplicationFactory<Program>` and replacing SQLite with InMemory database, you must:

1. **Skip database init in Program.cs** - Add environment check around `EnsureCreated()`:
   ```csharp
   if (!app.Environment.IsEnvironment("Testing"))
   {
       using var scope = app.Services.CreateScope();
       var db = scope.ServiceProvider.GetRequiredService<RiddleDbContext>();
       db.Database.EnsureCreated();
   }
   ```

2. **Remove ALL EF Core services** in test fixture - EF Core caches provider registrations internally. Removing just `DbContextOptions` is NOT enough:
   ```csharp
   builder.ConfigureServices(services =>
   {
       // Remove DbContext options
       var dbContextOptionsDescriptors = services
           .Where(d => d.ServiceType == typeof(DbContextOptions<RiddleDbContext>) ||
                      d.ServiceType == typeof(DbContextOptions))
           .ToList();
       foreach (var descriptor in dbContextOptionsDescriptors)
           services.Remove(descriptor);
       
       services.RemoveAll<RiddleDbContext>();
       services.RemoveAll(typeof(IDbContextFactory<RiddleDbContext>));
       
       // CRITICAL: Remove ALL EF Core namespace services to prevent provider caching
       var efCoreDescriptors = services
           .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                      d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
           .ToList();
       foreach (var descriptor in efCoreDescriptors)
           services.Remove(descriptor);
       
       // Now add fresh InMemory provider
       services.AddDbContext<RiddleDbContext>(options =>
           options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
   });
   ```

3. **Use "Testing" environment** in fixture:
   ```csharp
   builder.UseEnvironment("Testing");
   ```

**Symptom:** `InvalidOperationException: Services for database providers 'Sqlite', 'InMemory' have been registered`

### CRITICAL: SignalR Argument Arity Must Match

**SignalR client handlers must match the exact number of arguments sent from the server.** A handler with wrong arity will NEVER fire - no error, just silent failure.

❌ **WRONG - Server sends 3 args, client expects 1:**
```csharp
// Server sends:
await Clients.Group(group).SendAsync("TurnAdvanced", turnIndex, combatantId, roundNumber);

// Client handler with 1 arg NEVER fires - silent failure!
connection.On<TurnAdvancedPayload>("TurnAdvanced", handler);  // ❌ Expects 1 arg, server sends 3
```

✅ **CORRECT - Wrap multiple values in a single payload record:**
```csharp
// Server sends single payload:
await Clients.Group(group).SendAsync("TurnAdvanced", new TurnAdvancedPayload(turnIndex, combatantId, roundNumber));

// Client handler expects 1 arg - matches!
connection.On<TurnAdvancedPayload>("TurnAdvanced", handler);  // ✅ Both sides agree on arity
```

**Diagnostic pattern:**
- **5.0s timeout** → Handler never fired (arity mismatch, wrong event name, or event not sent)
- **Immediate failure** → Handler fired but payload didn't match assertions

**Best practice:** Always use single payload records for SignalR events, even for simple data. Easier to extend and self-documenting.

### CRITICAL: SignalR Client Handler Registration Conflicts

**Problem:** When registering SignalR client handlers with `HubConnection.On<T>()`, **registering multiple handlers for the same event name with different type arguments causes conflicts**. Only the first registration is invoked - subsequent handlers are silently ignored.

❌ **WRONG - Multiple handlers for same event, only first invoked:**
```csharp
// These conflict! Only the no-arg handler fires
Connection.On(eventName, () => RecordEvent(eventName, []));
Connection.On<object?>(eventName, (arg1) => RecordEvent(eventName, [arg1]));
Connection.On<object?, object?>(eventName, (arg1, arg2) => RecordEvent(eventName, [arg1, arg2]));
```

✅ **CORRECT - Single handler per event name:**
```csharp
// Most SignalR events send a single payload object
Connection.On<object?>(eventName, (arg1) => RecordEvent(eventName, [arg1]));
```

**Root cause:** SignalR's client-side handler registry uses event name as the key. Multiple `On()` calls for the same event name overwrite or conflict with each other.

**Symptom:** Test client shows "Events received: []" even though server sends events and connection is in `Connected` state.

### JSON Deserialization of `object` Types

When a record has an `object` property (like `CharacterStatePayload.Value`), **JSON deserializes numbers as `JsonElement`, not the native type**. Direct comparison with FluentAssertions fails:

❌ **WRONG - Types don't match:**
```csharp
payload.Value.Should().Be(30);  // ❌ Fails: JsonElement != int
```

✅ **CORRECT - Compare as strings for type-agnostic matching:**
```csharp
payload.Value?.ToString().Should().Be("30");  // ✅ Works regardless of underlying type
```

**Why this happens:** When JSON deserializes `{ "value": 30 }` into `object Value`, the runtime type is `System.Text.Json.JsonElement`, not `int`. FluentAssertions' `.Be()` compares by type AND value.

**Alternative fix:** Change payload record to use concrete types (e.g., `int? Value`) if the type is always known.

### CRITICAL: SignalR Handlers Must Update Local State Directly

**Problem:** When a SignalR event arrives with updated data, **DO NOT re-fetch from database**. The scoped `DbContext` in the component may have a stale cached entity, causing the UI to show old data.

❌ **WRONG - Re-fetching from DB returns stale data:**
```csharp
// CharacterStateUpdated handler
_hubConnection.On<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated, async payload =>
{
    // WRONG! DbContext may have stale cached entity
    campaign = await CampaignService.GetCampaignAsync(CampaignId);
    await InvokeAsync(StateHasChanged);  // Shows OLD data!
});
```

✅ **CORRECT - Update local state directly from payload:**
```csharp
// CharacterStateUpdated handler  
_hubConnection.On<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated, async payload =>
{
    if (campaign == null) return;
    
    // Update local state directly from payload
    var character = campaign.PartyState.FirstOrDefault(c => c.Id == payload.CharacterId);
    if (character != null)
    {
        switch (payload.Key)
        {
            case "current_hp":
                if (int.TryParse(payload.Value?.ToString(), out var hp))
                    character.CurrentHp = hp;
                break;
            // ... other cases
        }
    }
    await InvokeAsync(StateHasChanged);  // Shows FRESH data!
});
```

**Pattern consistency:** Compare with `PlayerChoicesReceived` handler which correctly updates `campaign.ActivePlayerChoices = choices;` directly from the payload.

**Symptom:** DM Dashboard updates correctly but Player Dashboard shows stale data after SignalR events.

---

## CRITICAL: Dual Data Sources for PC HP During Combat

**Problem:** When a PC is in active combat, their HP exists in TWO places:
1. **`PartyState`** (canonical source) - Character's authoritative HP
2. **`ActiveCombat.Combatants`** (combat snapshot) - HP captured when combat started

When `update_character_state` updates PC HP, both sources must be synchronized. Otherwise:
- **Party Card** (reads from `PartyState`) → Shows correct HP
- **Combat Tracker** (reads from `ActiveCombat.Combatants`) → Shows stale HP

**Symptom:** After LLM updates a PC's HP via `update_character_state`, the Party Card shows "0/27" but Combat Tracker shows "27/27" - even after page reload.

**Root Cause:** `ToolExecutor.UpdateCharacterPropertyAsync` only updated `PartyState`, not the corresponding combatant in `ActiveCombat.Combatants`.

**Fix:** In `ToolExecutor`, when updating `current_hp` for a PC, also update the matching combatant in active combat:

```csharp
// Update PartyState (canonical source)
character.CurrentHp = newHp;
await _stateService.UpdateCharacterAsync(campaignId, character, ct);

// ALSO sync to ActiveCombat.Combatants if PC is in combat
if (campaign.ActiveCombat?.Combatants != null)
{
    if (campaign.ActiveCombat.Combatants.TryGetValue(character.Id, out var combatant))
    {
        combatant.CurrentHp = newHp;
        await _combatService.SaveCombatStateAsync(campaignId, campaign.ActiveCombat, ct);
    }
}
```

**Verification:** Use `python build.py db party` to check `PartyState` HP, and check `ActiveCombat` JSON in database to verify both show synchronized values.

---

## D&D 5e Rules Implementation

### Death Saving Throw Pattern

**D&D 5e death saves** are implemented via `update_character_state` tool with special keys:

| Key | Value | Behavior |
|-----|-------|----------|
| `death_save_success` | `true` or `"nat20"` | Increment successes (+1 or wake with 1 HP on nat20) |
| `death_save_failure` | `true` or `2` | Increment failures (+1 or +2 for crit damage) |
| `stabilize` | `true` | Set successes to 3 (Medicine check/Spare the Dying) |

**Auto-rules enforcement in ToolExecutor:**
```csharp
// When HP drops to 0: Add Unconscious, reset death saves
if (newHp <= 0 && oldHp > 0)
{
    character.Conditions.Add("Unconscious");
    character.DeathSaveSuccesses = 0;
    character.DeathSaveFailures = 0;
}

// When healed from 0: Remove Unconscious, reset death saves
if (newHp > 0 && oldHp <= 0)
{
    character.Conditions.Remove("Unconscious");
    character.DeathSaveSuccesses = 0;
    character.DeathSaveFailures = 0;
}

// 3 successes = Stable (add condition)
if (character.DeathSaveSuccesses >= 3)
    character.Conditions.Add("Stable");

// 3 failures = Dead (add condition)
if (character.DeathSaveFailures >= 3)
    character.Conditions.Add("Dead");
```

**Computed properties on Character model:**
```csharp
[NotMapped]
public bool IsStable => DeathSaveSuccesses >= 3;

[NotMapped]
public bool IsDead => DeathSaveFailures >= 3;
```

**SignalR broadcast:** Use `DeathSaveUpdated` event with `DeathSavePayload` to update all clients when death save state changes.

### CRITICAL: Parent Must Handle DeathSaveUpdated Before Child Calls StateHasChanged

**Problem:** When ToolExecutor sends both `DeathSaveUpdated` AND `CharacterStateUpdated` events, there's a race condition if the child component (CombatTracker) receives and processes events before the parent (Campaign.razor).

**Symptom:** Death save circles don't fill in the UI even though the database has correct values. LLM acknowledges "1 success" but the circles stay empty.

**Root cause:** CombatTracker receives `DeathSaveUpdated`, calls `StateHasChanged()`, but at that moment `PartyCharacters` (passed from parent) hasn't been updated yet because Campaign.razor hasn't processed its events.

**Fix:** Campaign.razor must ALSO subscribe to `DeathSaveUpdated` and update `PartyState` directly:

```csharp
// Parent component (Campaign.razor) - handle BEFORE CombatTracker calls StateHasChanged
_hubConnection.On<DeathSavePayload>(GameHubEvents.DeathSaveUpdated, async payload =>
{
    var character = campaign.PartyState.FirstOrDefault(c => c.Id == payload.CharacterId);
    if (character != null)
    {
        character.DeathSaveSuccesses = payload.DeathSaveSuccesses;
        character.DeathSaveFailures = payload.DeathSaveFailures;
        campaign.PartyState = partyState;  // Trigger re-serialization
        await InvokeAsync(StateHasChanged);
    }
});
```

**Key insight:** When child component's `StateHasChanged()` is called, it re-reads `[Parameter]` values from parent. If parent hasn't updated those values yet, child renders stale data.

---

## PC vs Enemy Display at 0 HP (Combat Tracker)

**D&D 5e Rule:** PCs at 0 HP are NOT defeated - they make death saving throws. Enemies at 0 HP ARE defeated immediately.

**UI Behavior:**
| Combatant Type | HP State | Badge | Strikethrough | Death Save Circles |
|---------------|----------|-------|---------------|-------------------|
| PC | 0 HP, not stable/dead | "😵 Unconscious" | NO | YES (visible) |
| PC | 0 HP, 3 successes | "💚 Stable" | NO | YES (3 green filled) |
| PC | 0 HP, 3 failures | "💀 Dead" | YES | YES (3 red filled) |
| Enemy/NPC | 0 HP | "Defeated" | YES | NO |

**Implementation in CombatantCard.razor:**
```razor
@* Defeated badge - only for non-PCs *@
@if (Combatant.IsDefeated && Combatant.Type != "PC")
{
    <span data-testid="defeated-badge">Defeated</span>
}

@* PC unconscious badge - 0 HP but not yet dead/stable *@
@if (Combatant.Type == "PC" && Combatant.CurrentHp <= 0 && !IsPcDead && !IsPcStable)
{
    <span data-testid="unconscious-badge">😵 Unconscious</span>
}

@* Strikethrough logic *@
@{
    bool shouldStrikethrough = (Combatant.Type == "PC") ? IsPcDead : Combatant.IsDefeated;
}
```

**LLM Tool Enum Fix:** The `update_character_state` tool definition must include ALL death save keys:
```csharp
"enum": ["current_hp", "conditions", "add_condition", "remove_condition", 
         "status_notes", "initiative", "death_save_success", "death_save_failure", "stabilize"]
```

**Bug Pattern:** If LLM writes death saves to `StatusNotes` instead of incrementing `DeathSaveSuccesses`, the tool definition enum is probably missing the death save keys.

---

## CRITICAL: Player Dashboard Also Needs DeathSaveUpdated Handler

**Problem:** When DM Dashboard (`Campaign.razor`) receives `DeathSaveUpdated` SignalR events and updates correctly, the Player Dashboard (`/play/{id}`) may NOT update - showing stale death save data in its Combat Tracker.

**Root cause:** Player Dashboard is a SEPARATE page from DM Dashboard. It has its own SignalR connection and must subscribe to ALL the same events, including `DeathSaveUpdated`.

**Symptom:**
- DM Dashboard shows "😵 Unconscious" badge, death save circles (S:⚫⚫⚫ F:⚫⚫⚫) ✅
- Player's Character Card shows updated death saves ✅
- Player's Combat Tracker shows STALE death save state ❌

**Fix:** Player Dashboard must:
1. Subscribe to `DeathSaveUpdated` event
2. Update local `_partyState` directly from payload (same pattern as Campaign.razor)
3. Pass `PartyCharacters` parameter to CombatTracker component

```razor
@* Player Dashboard.razor must pass PartyCharacters to CombatTracker *@
<CombatTracker 
    Combat="_combatState" 
    CombatChanged="OnCombatChanged"
    PartyCharacters="_partyState" />  @* Critical for death save lookups *@
```

```csharp
// Player Dashboard SignalR handler
_hubConnection.On<DeathSavePayload>(GameHubEvents.DeathSaveUpdated, async payload =>
{
    var character = _partyState.FirstOrDefault(c => c.Id == payload.CharacterId);
    if (character != null)
    {
        character.DeathSaveSuccesses = payload.DeathSaveSuccesses;
        character.DeathSaveFailures = payload.DeathSaveFailures;
        await InvokeAsync(StateHasChanged);
    }
});
```

**Key insight:** Any page or component that displays character state from `PartyState` must handle ALL relevant SignalR events that modify that state. There is NO automatic propagation - each SignalR subscriber must handle events independently.

---

## CRITICAL: SignalR Hub Requires [AllowAnonymous] Behind Reverse Proxies

**Problem:** When running Blazor Server app behind a reverse proxy (Cloudflare tunnel, nginx, Traefik), SignalR client connections fail with `403 Forbidden` during negotiation phase.

**Error in logs:**
```
System.Net.Http.HttpRequestException: Response status code does not indicate success: 403 (Forbidden).
   at Microsoft.AspNetCore.Http.Connections.Client.HttpConnection.NegotiateAsync(...)
```

**Root cause:** SignalR client connections make a **separate HTTP request** for negotiation (POST to `/gamehub/negotiate`). This request:
1. May not carry cookies correctly through the proxy
2. May be blocked by anti-forgery middleware (which runs before auth)
3. May be affected by CORS or other middleware

**Symptom:** App works locally but crashes in Docker/production when navigating to campaign page.

**Fix:** Add `[AllowAnonymous]` attribute to the SignalR Hub:

```csharp
using Microsoft.AspNetCore.Authorization;

/// <remarks>
/// AllowAnonymous is required because SignalR client connections make a separate
/// HTTP request for negotiation that may not include auth cookies/tokens properly,
/// especially when behind reverse proxies (Cloudflare tunnel).
/// Authentication is handled at the application layer (Campaign page requires auth).
/// </remarks>
[AllowAnonymous]
public class GameHub : Hub
```

**Why this is safe:** The pages that connect to the hub (Campaign.razor, Dashboard.razor) already require authentication via `<AuthorizeView>`. The hub itself doesn't need to enforce auth - it just needs to be reachable.

**Alternative (not recommended):** Configure HubConnection with explicit credentials:
```csharp
.WithUrl(Navigation.ToAbsoluteUri("/gamehub"), options =>
{
    options.AccessTokenProvider = async () => { /* get JWT */ };
})
```
This is more complex and requires switching to JWT-based auth instead of cookies.
