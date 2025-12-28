# Cline Rules for Project Riddle

## Project Overview
Project Riddle is a LLM-driven Dungeon Master assistant for D&D 5th Edition built with ASP.NET Core 9.0, Blazor Server, SignalR, and Flowbite Blazor UI components.

## Technology Stack
- **Backend**: ASP.NET Core 10.0 (Blazor Server with InteractiveServer)
- **LLM Provider**: OpenRouter via LLM Tornado SDK
- **Real-time**: SignalR (all-in architecture)
- **UI Framework**: Flowbite Blazor + Tailwind CSS
- **Database**: Entity Framework Core 10 with SQLite (dev) / PostgreSQL (prod)
- **Authentication**: ASP.NET Identity + Google OAuth

## Essential Project Repositories
- **LLM Tornado SDK**: `C:\Users\tschavey\projects\github\LlmTornado`
- **Flowbite Blazor Admin Dashboard (WASM Standalone)**: `C:\Users\tschavey\projects\peakflames\flowbite-blazor-admin-dashboard`

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


## Build, Test, and Development Commands
Use the Python automation:
- `python build.py` — restore dependencies and build
- `python build.py run` — start the app (foreground)
- `python build.py start` — start in background (writes to riddle.log)
- `python build.py stop` — stop background process
- `python build.py status` — check if running
- `python build.py watch` — hot reload .NET and Tailwind

## DB Migration Commands
- `dotnet ef migrations add {Name} --project src/Riddle.Web` - Add migration
- `dotnet ef database update --project src/Riddle.Web` - Update database

## UI Assets & Theming Tips
Tailwind config lives in `src/Riddle.Web/tailwind.config.js`; PostCSS in `src/Riddle.Web/postcss.config.js`. Place new icons, fonts, or sample data in `wwwroot` and reference them relatively. Bundle third-party JS or CSS through `wwwroot` and document new dependencies in the PR rationale.

It is ULTRA IMPORTATNT to adhere to the Flowbite Design Style System as it is a Mobile first and good looking.

PREFER to leverage components and pages already create over at `- **Flowbite Blazor Admin Dashboard (WASM Standalone)**: `C:\Users\tschavey\projects\peakflames\flowbite-blazor-admin-dashboard`

## Problem-Solving Approach
1. Analyze and form a hypothesis before modifying code.
2. Implement a focused fix and verify it.
3. If the hypothesis fails, stop and surface findings instead of pivoting blindly.


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

### Flowbite Blazor Component APIs
- **SpinnerSize**: Use `SpinnerSize.Xl`, not `SpinnerSize.ExtraLarge`
- **BadgeColor**: Requires explicit `@using Flowbite.Blazor.Enums` in some contexts
- **CardSize**: Use `CardSize.ExtraLarge`, not `CardSize.XLarge`
- **EditForm Context Conflicts**: When EditForm is inside AuthorizeView, add `Context="editContext"` parameter to EditForm to avoid context name collision
- **Icon Components**: Use Flowbite icon components (e.g., `<BookOpenIcon Class="w-5 h-5" />`) from Flowbite.Blazor.Icons namespace
- Always check Flowbite Blazor docs or reference dashboard project for exact API signatures

### EF Core Patterns
- When creating services that use DbContext, inject `RiddleDbContext` directly
- For computed properties on models (like `PartyState` backed by `PartyStateJson`), use `[NotMapped]` attribute
- Always call `SaveChangesAsync()` after mutations

### Blazor Server Authentication Patterns
- Get current user ID via `AuthenticationStateProvider.GetAuthenticationStateAsync()`
- User claims are accessed via `user.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- Always check `user.Identity?.IsAuthenticated == true` before accessing claims
- Use `<AuthorizeView>` with `<Authorized>` and `<NotAuthorized>` sections for protected pages

## Git Workflow
- Branch from `develop`: `git checkout develop && git pull origin develop`.
- Naming: `fix/issue-{id}-description`, `feature/issue-{id}-description`, or `enhancement/issue-{id}-description`.
- Commit format: `{type}({scope}): {description}` (types: fix, feat, docs, style, refactor, test, chore). Reference issues with `Fixes #{number}` when applicable.
- Ensure `src/WebApp/wwwroot/css/app.min.css` is committed if it has changed. It auto-generate by the WebApp.csproj build instructions.
