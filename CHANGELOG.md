# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/peakflames/riddle/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/peakflames/riddle/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/peakflames/riddle/releases/tag/v0.1.0
