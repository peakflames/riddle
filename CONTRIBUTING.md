# Contributing to Riddle

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Python 3.8+](https://www.python.org/downloads/) (for build automation)
- [Node.js 18+](https://nodejs.org/) (optional, for Tailwind development)

### Environment Configuration

1. Copy the example environment file:
   ```bash
   cp src/Riddle.Web/.env.example src/Riddle.Web/.env
   ```

2. Configure required settings in `.env`:
   ```
   GOOGLE_CLIENT_ID=your-google-client-id
   GOOGLE_CLIENT_SECRET=your-google-client-secret
   OPENROUTER_API_KEY=your-openrouter-api-key
   ```

### Build & Run

```bash
# Restore dependencies and build
python build.py

# Run in foreground
python build.py run

# Run in background (logs to riddle.log)
python build.py start

# Stop background process
python build.py stop

# Check if running
python build.py status

# Hot reload (development)
python build.py watch
```

### Database

SQLite is used for development. The database is created automatically on first run.

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/Riddle.Web

# Apply migrations
dotnet ef database update --project src/Riddle.Web

# Reset database (delete riddle.db and re-run update)
```

## Git Workflow

1. Branch from `develop`:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/your-feature-name
   ```

2. Make changes with frequent commits:
   ```bash
   git commit -m "type(scope): description"
   ```
   
   Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

3. Push and create PR:
   ```bash
   git push origin feature/your-feature-name
   ```

## Code Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use nullable reference types
- Prefer `async`/`await` for I/O operations
- Use dependency injection for services

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Services | `I{Name}Service` / `{Name}Service` | `ISessionService`, `SessionService` |
| Models | PascalCase, singular | `RiddleSession`, `Character` |
| SignalR Hubs | Suffix with `Hub` | `GameHub` |
| LLM Tools | Suffix with `Tool` | `GetGameStateTool` |

## Testing

No automated test project yet. Before submitting:

1. Run `python build.py` - ensure clean build
2. Run `python build.py start` - verify app starts
3. Test affected functionality manually
4. Check browser console for errors
5. Review `riddle.log` for runtime issues

## Versioning

This project uses [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes or major milestones
- **MINOR**: New features (increment after each feature/objective)
- **PATCH**: Bug fixes

### After Merging a Feature

1. **Update version** in `src/Riddle.Web/Riddle.Web.csproj`:
   ```xml
   <Version>0.2.0</Version>
   <AssemblyVersion>0.2.0.0</AssemblyVersion>
   <FileVersion>0.2.0.0</FileVersion>
   <InformationalVersion>0.2.0</InformationalVersion>
   ```

2. **Update CHANGELOG.md**:
   - Move items from `[Unreleased]` to new version section
   - Add release date
   - Use categories: Added, Changed, Deprecated, Removed, Fixed, Security

3. **Commit and push**:
   ```bash
   git commit -m "chore(release): bump version to 0.2.0"
   git push origin develop
   ```

## Project Structure

```
src/Riddle.Web/
├── Components/        # Blazor components and pages
│   ├── Layout/       # Shared layouts
│   ├── Pages/        # Routable pages
│   └── Account/      # Auth-related components
├── Data/             # EF Core context
├── Models/           # Domain entities
├── Services/         # Business logic
├── Hubs/             # SignalR hubs
├── Tools/            # LLM tool implementations
└── wwwroot/          # Static assets
```

## UI Guidelines

- Use [Flowbite Blazor](https://flowbite-blazor.org) components
- Follow mobile-first responsive design
- Reference `docs/flowbite_blazor_docs.md` for component APIs
- Tailwind config: `src/Riddle.Web/tailwind.config.js`

## Getting Help

- [Open an issue](https://github.com/peakflames/riddle/issues)
- Check existing documentation in `docs/`
