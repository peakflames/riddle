# Developer Rules and Guidelines

> **Related Document:** See [`memory_aid.md`](./memory_aid.md) for lessons learned and gotchas discovered through development.

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

### SignalR Documentation

Comprehensive SignalR documentation lives in `docs/signalr/`:

```
docs/signalr/
├── README.md              # Architecture overview, quick reference
├── groups.md              # Group naming conventions (_dm, _players, _all)
├── events-reference.md    # All 17 events with payloads & subscribers
└── flows/
    ├── player-lifecycle-flow.md    # Join/leave/reconnect sequences
    ├── combat-flow.md              # Combat start/turn/HP/end flows
    ├── player-choice-flow.md       # Choice presentation & submission
    └── atmospheric-events-flow.md  # Pulse/Anchor/Insight events
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
- `python build.py db characters [campaign-id]` — show characters (with PlayerId claim status)
- `python build.py db party [campaign-id]` — show full PartyStateJson (pretty-printed)
- `python build.py db update "<name>" <property> "<value>"` — update a character property directly
- `python build.py db create-character "@file.json"` — create a character from JSON file
- `python build.py db delete-character "<name>"` — delete a character by name
- `python build.py db character-template` — show JSON template for creating characters
- `python build.py db templates` — show all character templates in CharacterTemplates table
- `python build.py db import-templates` — import JSON files from SampleCharacters into CharacterTemplates table
- `python build.py db rolls [campaign-id]` — show recent dice rolls
- `python build.py db clear-rolls [campaign-id]` — clear recent rolls
- `python build.py db "SELECT * FROM CampaignInstances"` — execute custom SQL query

### Database Backup Commands
- `python build.py db backup [name]` — backup database (auto-timestamps if no name)
- `python build.py db restore <name>` — restore database from backup
- `python build.py db backups` — list all available backups
- `python build.py db delete-backup <name>` — delete a backup

**Backup behavior:** Auto-stops running app for clean backup/restore. Backups stored in `./backups/`.

**When to use these commands:**
- **log commands**: Use when debugging runtime issues, checking if operations succeeded, or investigating errors
- **db campaigns**: Use to verify database persistence after UI actions (e.g., after adding characters, check if PartyDataLen increased)
- **db characters**: Use to verify character claims are persisted (PlayerId should show user GUID when claimed)
- **db party**: Use to inspect full character data including roleplay fields (PersonalityTraits, Ideals, Bonds, Flaws, Backstory)
- **db templates**: Use to verify character templates are stored correctly in the CharacterTemplates table
- **db import-templates**: Use after adding/editing JSON files in SampleCharacters/ to sync them to the database
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


## Git Workflow

- Branch from `develop`: `git checkout develop && git pull origin develop`.
- Naming: `fix/issue-{id}-description`, `feature/issue-{id}-description`, or `enhancement/issue-{id}-description`.
- Commit format: `{type}({scope}): {description}` (types: fix, feat, docs, style, refactor, test, chore). Reference issues with `Fixes #{number}` when applicable.
- Ensure `src/WebApp/wwwroot/css/app.min.css` is committed if it has changed. It auto-generate by the WebApp.csproj build instructions.


## Verification Checklist Discipline

- **NEVER** ask for push/merge approval until ALL verification checklist items are `[x]`
- Before asking "Ready to push?", first `read_file` the verification checklist
- If any item shows `[ ]`, complete that step first (e.g., runtime testing, UI verification)
- The commit is NOT the completion milestone - the full checklist is
