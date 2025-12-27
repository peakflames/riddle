# Cline Rules for Project Riddle

## Project Overview
Project Riddle is a LLM-driven Dungeon Master assistant for D&D 5th Edition built with ASP.NET Core 9.0, Blazor Server, SignalR, and Flowbite Blazor UI components.

## Technology Stack
- **Backend**: ASP.NET Core 9.0 (Blazor Server)
- **LLM Provider**: OpenRouter via LLM Tornado SDK
- **Real-time**: SignalR (all-in architecture)
- **UI Framework**: Flowbite Blazor + Tailwind CSS
- **Database**: Entity Framework Core with SQLite (dev) / PostgreSQL (prod)
- **Authentication**: ASP.NET Identity + Google OAuth

## Essential Project Repositories
- **LLM Tornado SDK**: `C:\Users\tschavey\projects\github\LlmTornado`
- **Flowbite Blazor**: `C:\Users\tschavey\projects\themesberg\flowbite-blazor`
- **Current Project**: `C:\Users\tschavey\projects\peakflames\riddle`

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

### ASP.NET Core Best Practices
- Register services with appropriate lifetime (Singleton, Scoped, Transient)
- Use `IConfiguration` for settings, never hardcode
- Implement proper error handling and logging
- Use strongly-typed configuration with Options pattern
- Follow the Repository pattern for data access

### Blazor Server Specific
- Use `@inject` for service injection in components
- Implement `IDisposable`/`IAsyncDisposable` for cleanup
- Use `StateHasChanged()` only when necessary
- Avoid long-running operations on UI thread
- Use SignalR circuit events for connection management

### SignalR Guidelines
- Use groups for multi-client scenarios (DM vs Players)
- Implement connection lifetime management
- Handle reconnection scenarios gracefully
- Use strongly-typed hub methods
- Broadcast state changes from server, not client

### LLM Tornado Integration
- Always use `OpenRouter` provider: `LLmProviders.OpenRouter`
- Define tools with proper JSON schemas
- Use `StreamResponseRich` with `ChatStreamEventHandler` for tool handling
- Implement tool results with `FunctionResult`
- Handle tool calls in `FunctionCallHandler` callback
- Continue conversation in `AfterFunctionCallsResolvedHandler`

### Entity Framework Core
- Use fluent API in `OnModelCreating` for complex configurations
- Store complex types (Lists, nested objects) as JSON columns
- Use migrations for all schema changes
- Implement proper indexes for query performance
- Use `AsNoTracking()` for read-only queries

## Security Considerations
- Store API keys in `appsettings.json` (not committed)
- Use User Secrets for local development
- Implement authorization policies for DM vs Player access
- Validate all user inputs
- Sanitize LLM outputs before display
- Use HTTPS in production
- Implement CSRF protection

## Documentation Requirements
- Update `docs/implementation_plan.md` when architecture changes
- Document all LLM tools with purpose and parameters
- Add XML comments to public APIs
- Maintain README.md with setup instructions
- Document SignalR events and message formats

## Testing Guidelines
- Write unit tests for services and tools
- Test SignalR hubs with mock clients
- Integration test LLM service with real OpenRouter calls
- Test authentication flows
- Validate EF Core migrations

## Git Practices
- Never commit `appsettings.json` with real API keys
- Use `.gitignore` for build artifacts, user secrets
- Commit migrations with descriptive names
- Write clear commit messages referencing issues/features

## Performance Optimization
- Use caching for static game data (D&D rules, modules)
- Implement database connection pooling
- Minimize SignalR payload sizes
- Use compression for large responses
- Lazy-load Blazor components where appropriate

## Error Handling
- Use structured logging with categories
- Implement global exception handler
- Return user-friendly error messages
- Log LLM API errors with context
- Handle SignalR disconnections gracefully

## When Creating New Features
1. Define models in `Models/` with proper EF Core attributes
2. Create service interface and implementation
3. Register service in `Program.cs`
4. Implement SignalR hub methods if real-time needed
5. Create Blazor components using Flowbite primitives
6. Add routing in `Pages/` if routable
7. Write tests
8. Update documentation

## Common Commands
- `dotnet build` - Build solution
- `dotnet run --project src/Riddle.Web` - Run application
- `dotnet ef migrations add {Name} --project src/Riddle.Web` - Add migration
- `dotnet ef database update --project src/Riddle.Web` - Update database
- `dotnet test` - Run tests

## Reference Documentation Locations
- Software Design: `docs/software_design.md`
- Implementation Plan: `docs/implementation_plan.md`
- System Prompts: `docs/candidate_system_prompts.md`
- Game Rules: `src/GameReferenceData/`

## Important Notes
- The LLM is stateless - all state must be persisted to database
- Use `get_game_state` tool at conversation start for state recovery
- SignalR is the ONLY way to push updates to clients
- All tool calls must broadcast relevant updates via SignalR
- DM and Player interfaces have separate SignalR groups
- Use Flowbite Blazor components - don't reinvent UI primitives
