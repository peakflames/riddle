# Developer Rules and Guidelines

## Project Structure Guidelines

### Solution Structure
```
Riddle.sln
├── src/
│   └── Riddle.Web/           # Main Blazor Server application
│       ├── Data/             # EF Core context, migrations
│       ├── Models/           # Domain entities
│       ├── Services/         # Business logic services
│       ├── Tools/            # LLM tool implementations
│       ├── Hubs/             # SignalR hubs
│       ├── Pages/            # Blazor pages (routable)
│       ├── Components/       # Reusable Blazor components
│       └── wwwroot/          # Static assets
├── tests/
│   └── Riddle.Tests/
└── docs/                     # Project documentation
```

### File Organization
- Place routable pages in `Pages/` with `@page` directive
- Place reusable components in `Components/` subdirectories by feature
- Keep SignalR hubs in dedicated `Hubs/` directory
- Separate LLM tool implementations in `Tools/` directory
- Use `Services/` for business logic with interface/implementation pairs


## Build, Test, and Development Commands
Use the Python automation:
- `python build.py` — auto-stops running app, then builds
- `python build.py run` — run in foreground (Ctrl+C to stop)
- `python build.py start` — auto-builds, then starts in background (writes to riddle.log)
- `python build.py stop` — stop background process
- `python build.py status` — check if running
- `python build.py watch` — hot reload .NET and Tailwind

**Key Behaviors:**
- `build` auto-stops any running instance (prevents file lock errors)
- `start` auto-builds before launching (always runs latest code)

### Log Commands (for debugging)
- `python build.py log` — show last 50 lines of riddle.log
- `python build.py log <pattern>` — search log for regex pattern (case-insensitive)
- `python build.py log --tail <n>` — show last n lines
- `python build.py log --level error` — filter by log level (error/warn/info/debug)
- `python build.py log character --tail 100` — combine pattern with options

### Database Commands (for verification)
- `python build.py db tables` — list all database tables
- `python build.py db campaigns` — show campaign instances with party state preview
- `python build.py db characters` — show characters from most recent campaign (with PlayerId claim status)
- `python build.py db characters <campaign-id>` — show characters from specific campaign
- `python build.py db party` — show full PartyStateJson (pretty-printed) from most recent campaign
- `python build.py db party <campaign-id>` — show full PartyStateJson for specific campaign
- `python build.py db update "<name>" <property> "<value>"` — update a character property directly
- `python build.py db create-character "@file.json"` — create a character from JSON file
- `python build.py db delete-character "<name>"` — delete a character by name
- `python build.py db character-template` — show JSON template for creating characters
- `python build.py db "SELECT * FROM CampaignInstances"` — execute custom SQL query

**When to use these commands:**
- **log commands**: Use when debugging runtime issues, checking if operations succeeded, or investigating errors
- **db campaigns**: Use to verify database persistence after UI actions (e.g., after adding characters, check if PartyDataLen increased)
- **db characters**: Use to verify character claims are persisted (PlayerId should show user GUID when claimed)
- **db party**: Use to inspect full character data including roleplay fields (PersonalityTraits, Ideals, Bonds, Flaws, Backstory)
- **db update**: Use to set character properties directly (bypasses UI for testing/automation)
- **db "SQL"**: Use for detailed data inspection when verifying features work correctly

## Coding Standards

### C# Conventions
- Use C# 12 features (collection expressions, primary constructors, etc.)
- Follow Microsoft's C# Coding Conventions
- Use `nullable` reference types throughout
- Prefer `async`/`await` for all I/O operations
- Use dependency injection for all services

### Naming Conventions
- **Services**: `I{Name}Service` interface, `{Name}Service` implementation
- **Models**: PascalCase, singular (e.g., `RiddleSession`, `Character`)
- **SignalR Hubs**: Suffix with `Hub` (e.g., `GameHub`)
- **Tools**: Suffix with `Tool` (e.g., `GetGameStateTool`)
- **Razor Components**: PascalCase matching filename
- **Methods**: Async methods should end with `Async`


## Documentation Requirements
- Update `docs/implementation_plan.md` when architecture changes
- Document all LLM tools with purpose and parameters
- Add XML comments to public APIs
- Maintain README.md with setup instructions
- Document SignalR events and message formats

## Testing Guidelines
- No automated test project ships yet. Before submitting, smoke-test dashboards with `dotnet build` and confirm Tailwind rebuilds cleanly.
- Ensure `src/WebApp/wwwroot/css/app.min.css` is regenerated as part of builds and committed whenever component styles change.


## Lessons Learned (Memory Aid)

### Blazor Server vs WASM
- This project uses **Blazor Server** with `InteractiveServer` render mode, NOT WASM
- The Flowbite Blazor Admin Dashboard reference is WASM - don't blindly copy App.razor/Program.cs
- Interactive pages need `@rendermode InteractiveServer` directive

### .NET 10 Blazor Server Setup
- Use `blazor.web.js` (not `blazor.server.js`) with `@Assets["_framework/blazor.web.js"]` syntax
- App.razor needs: `<ResourcePreloader />`, `<ImportMap />`, `<ReconnectModal />`
- Program.cs: Use `MapStaticAssets()` instead of `UseStaticFiles()`
- For non-development runs: Add `builder.WebHost.UseStaticWebAssets()` before building
- Generate reference: `dotnet new blazor -int Server` in tmp/ folder for correct patterns

### Package Management
- For .NET 10 preview packages: `dotnet add package {Name} --prerelease`
- Use `--version 10.0.1` for specific versions

### Database Issues
- If migrations fail due to existing tables not matching, delete `riddle.db` and re-run `dotnet ef database update`
- Always use `dotnet ef` commands from the repo root with `--project src/Riddle.Web`

### Testing Approach
- User prefers functional/integration tests over unit tests
- Use Playwright MCP for browser-based verification
- Create API endpoints for testing when Blazor interactivity has issues

### Windows Shell Commands
- Don't use Unix commands like `find /i` - use PowerShell: `Select-String -Pattern "error"`
- `sqlite3` may not be installed - verify database via EF Core or migration files

### UUID/GUID
- Use `Guid.CreateVersion7()` for time-sortable IDs (requires .NET 9+)

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

### Flowbite Blazor Component APIs
- **SpinnerSize**: Use `SpinnerSize.Xl`, not `SpinnerSize.ExtraLarge`
- **BadgeColor**: Requires explicit `@using Flowbite.Blazor.Enums` in some contexts. Note: `BadgeColor.Dark` does NOT exist - use `BadgeColor.Gray` for dark tones
- **CardSize**: Use `CardSize.ExtraLarge`, not `CardSize.XLarge`
- **EditForm Context Conflicts**: When EditForm is inside AuthorizeView, add `Context="editContext"` parameter to EditForm to avoid context name collision
- **Icon Components**: Use Flowbite icon components (e.g., `<BookOpenIcon Class="w-5 h-5" />`) from Flowbite.Blazor.Icons namespace
- Always check Flowbite Blazor docs or reference dashboard project for exact API signatures

### CRITICAL: Flowbite Textarea Binding in TabPanels
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

### EF Core Patterns
- When creating services that use DbContext, inject `RiddleDbContext` directly
- For computed properties on models (like `PartyState` backed by `PartyStateJson`), use `[NotMapped]` attribute
- Always call `SaveChangesAsync()` after mutations

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

### Blazor Server Authentication Patterns
- Get current user ID via `AuthenticationStateProvider.GetAuthenticationStateAsync()`
- User claims are accessed via `user.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- Always check `user.Identity?.IsAuthenticated == true` before accessing claims
- Use `<AuthorizeView>` with `<Authorized>` and `<NotAuthorized>` sections for protected pages

### Verification Checklist Discipline
- **NEVER** ask for push/merge approval until ALL verification checklist items are `[x]`
- Before asking "Ready to push?", first `read_file` the verification checklist
- If any item shows `[ ]`, complete that step first (e.g., runtime testing, UI verification)
- The commit is NOT the completion milestone - the full checklist is

## Git Workflow
- Branch from `develop`: `git checkout develop && git pull origin develop`.
- Naming: `fix/issue-{id}-description`, `feature/issue-{id}-description`, or `enhancement/issue-{id}-description`.
- Commit format: `{type}({scope}): {description}` (types: fix, feat, docs, style, refactor, test, chore). Reference issues with `Fixes #{number}` when applicable.
- Ensure `src/WebApp/wwwroot/css/app.min.css` is committed if it has changed. It auto-generate by the WebApp.csproj build instructions.
