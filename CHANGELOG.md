# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.1] - 2025-12-28

### Added
- **Application Event Log Panel**
  - Debug panel in Campaign sidebar for real-time LLM activity visibility
  - `AppEvent` model with `AppEventType` enum (LlmRequest, LlmResponse, ToolCall, ToolResult, StateUpdate, Error)
  - `IAppEventService`/`AppEventService` scoped service with 100-event circular buffer
  - `AppEventLog.razor` collapsible UI component with color-coded badges
  - Event emissions in `RiddleLlmService` for LLM requests, streaming, tool calls, and errors
  - Expandable details section for viewing full payloads

- **Real-time UI Updates for Campaign State**
  - `OnCampaignChanged` event in `IGameStateService` for reactive UI updates
  - Campaign.razor subscribes to state changes for Read-Aloud Text, Player Choices, and Scene Image
  - Instant UI refresh when LLM tools modify campaign state (no page refresh needed)

### Technical
- Session-only in-memory event log (clears on browser refresh)
- Event-driven architecture with `Action<T>` event subscriptions
- Proper `IDisposable` implementation for cleanup

## [0.3.0] - 2025-12-28

### Added
- **LLM Integration (Phase 2 Complete)**
  - LLM Tornado SDK integration with OpenRouter API
  - `IGameStateService`/`GameStateService` for game state CRUD operations
  - `IToolExecutor`/`ToolExecutor` for routing LLM tool calls
  - `IRiddleLlmService`/`RiddleLlmService` for LLM communication with streaming
  - 7 LLM tools implemented:
    - `get_game_state` - Retrieves full campaign state for context recovery
    - `update_character_state` - Updates character HP, conditions, initiative
    - `update_game_log` - Records events to narrative log
    - `display_read_aloud_text` - Sets Read Aloud Text Box content
    - `present_player_choices` - Sets interactive player choices
    - `log_player_roll` - Records dice roll results
    - `update_scene_image` - Updates scene image URI
  - DM Chat UI component using Flowbite Blazor Chat components
  - Real-time streaming token display from LLM responses
  - System prompt with D&D 5e rules and narrative engine directives

### Technical
- OpenRouter configuration via user secrets/environment variables
- Tool execution with JSON argument parsing and result formatting
- Conversation continuity after tool execution
- Error handling and logging throughout LLM pipeline

## [0.2.0] - 2025-12-28

### Changed
- **BREAKING**: Renamed `RiddleSession` entity to `CampaignInstance` to better model D&D campaigns vs individual play sessions
- **BREAKING**: Renamed `ISessionService`/`SessionService` to `ICampaignService`/`CampaignService`
- **BREAKING**: Updated routes from `/sessions/new` to `/campaigns/new` and `/dm/{SessionId}` to `/dm/{CampaignId}`
- Replaced `DbSet<RiddleSessions>` with `DbSet<CampaignInstances>` in `RiddleDbContext`
- Added new `PlaySession` entity to track individual sessions within campaigns
- Updated Program.cs dependency injection to use `ICampaignService`
- Recreated EF Core migration (InitialCreate) with new schema
- Added `CampaignModule` property to `CampaignInstance` for tracking which campaign module is being played
- Implemented UUID v7 (time-sortable IDs) for all entity primary keys

### Added
- `PlaySession` entity with properties: SessionNumber, StartedAt, EndedAt, IsActive, Summary, LocationJson
- Support for tracking multiple play sessions within a single campaign
- Session numbering and location history tracking per campaign

### Fixed
- Corrected data model to properly distinguish between long-running campaigns and individual play sessions

## [0.1.0] - 2025-12-27

### Added
- Initial project structure with ASP.NET Core 10.0 Blazor Server
- Google OAuth authentication with ASP.NET Identity
- Core data models: `RiddleSession`, `Character`, `Quest`, `CombatEncounter`, `LogEntry`, `PartyPreferences`
- Entity Framework Core with SQLite database
- Session management service (`ISessionService`/`SessionService`)
- Dashboard (Home.razor) with session stats and quick actions
- Create session page (`/sessions/new`) with module selection
- DM session view page (`/dm/{SessionId}`) placeholder
- Flowbite Blazor UI integration with Tailwind CSS
- Responsive sidebar layout with dark mode toggle
- Data model test page for debugging (`/test/data-model`)
- Build automation via `build.py` (build, run, start, stop, watch)
- Project documentation: README, CONTRIBUTING, implementation plans

### Technical
- .NET 10.0 with Blazor Server `InteractiveServer` render mode
- SignalR integration for real-time updates (prepared for Phase 2)
- Flowbite Blazor component library reference documentation
- Incremental phase implementation workflow for development

[Unreleased]: https://github.com/peakflames/riddle/compare/v0.3.1...HEAD
[0.3.1]: https://github.com/peakflames/riddle/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/peakflames/riddle/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/peakflames/riddle/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/peakflames/riddle/releases/tag/v0.1.0
