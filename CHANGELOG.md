# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.1] - 2025-12-28

### Added
- **Phase 3: Party Management & Character Creation (Objectives 3-4)**
  - Character Management UI components:
    - `CharacterCard.razor` - Individual character display with HP bar, stats, claimed status
    - `CharacterList.razor` - Party roster with Add/Invite buttons, empty state handling
    - `CharacterFormModal.razor` - Add/Edit character modal with quick entry form
  - Invite Link Modal (`InviteLinkModal.razor`):
    - Displays shareable invite URL for campaign joining
    - Copy to clipboard button with JS interop
    - Regenerate invite code with confirmation alert
    - Integrated into Campaign DM page Party panel

### Changed
- Moved "Invite Players" button from campaign header to Party card (next to Add button)
- `build.py` enhanced with smart automation:
  - `build` command auto-stops running instance before building (prevents file lock errors)
  - `start` command auto-builds before launching (always runs latest code)

### Technical
- Flowbite Blazor Alert `CustomContent` parameter used for complex confirmation dialogs
- JS interop for `navigator.clipboard.writeText()` API

## [0.5.0] - 2025-12-28

### Added
- **Phase 3: Party Management & Character Creation (Objectives 1-2)**
  - Expanded Character model with full D&D 5e fields:
    - Ability scores (Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma)
    - Combat stats (ArmorClass, MaxHp, CurrentHp, TemporaryHp, Initiative, Speed)
    - Skills & proficiencies (saving throws, skills, tools, languages)
    - Spellcasting support (cantrips, spells known, spell slots)
    - Equipment & inventory (weapons, equipment, currency)
    - Roleplay fields (personality traits, ideals, bonds, flaws, backstory)
    - Player linking (PlayerId, PlayerName, IsClaimed)
    - Computed ability modifiers
  - Invite code system for campaign sharing:
    - Auto-generated 8-character alphanumeric invite codes
    - `GetByInviteCodeAsync()` to look up campaigns by code
    - `RegenerateInviteCodeAsync()` to create new invite codes
    - Unique database index on InviteCode column
- Gameplay guide in README.md for DMs and players

### Technical
- EF Core migration `AddInviteCodeToCampaign` for InviteCode column
- UUID v7 IDs via `Guid.CreateVersion7()` for time-sortable character IDs
- Character model maintains backward compatibility (existing data unaffected)

## [0.4.1] - 2025-12-28

### Fixed
- Reasoning dropdown now collapsed by default using `DefaultOpen="false"` on Flowbite `Reasoning` component
- Prompt textbox now clears after sending message (added `_inputText = string.Empty` to match ChatAiPage pattern)

## [0.4.0] - 2025-12-28

### Changed
- **BREAKING**: Transitioned DM Chat from streaming to non-streaming LLM responses
  - More stable and predictable behavior
  - Reliable token usage tracking (providers return usage in non-streaming mode)
  - Aligns with Flowbite Blazor AI Chat component patterns
  - Simpler error handling and UI state management

### Added
- `DmChatResponse` record type for structured LLM response data
  - `Content`, `IsSuccess`, `ErrorMessage` for response handling
  - `PromptTokens`, `CompletionTokens`, `TotalTokens` for usage tracking
  - `ToolCallCount`, `DurationMs` for performance metrics
- Loading spinner with "Riddle is thinking..." indicator during LLM processing
- Token count display on assistant messages in chat UI
- Copy message action button on assistant responses

### Technical
- `IRiddleLlmService.ProcessDmInputAsync()` now returns `DmChatResponse` instead of using streaming callback
- `RiddleLlmService` uses `GetResponseRich()` instead of `StreamResponseRich()`
- Tool calls handled in synchronous loop with `MaxToolIterations` (10) safety limit
- Token usage extracted from `ChatRichResponse.Result.Usage` (reliable in non-streaming mode)
- `DmChat.razor` updated with Flowbite Conversation/PromptInput components

### Removed
- Streaming token-by-token display (replaced with full message rendering)
- `onStreamToken` callback parameter from `ProcessDmInputAsync()`

## [0.3.2] - 2025-12-28

### Added
- **Token Usage Tracking Infrastructure**
  - `TokenUsage` event type added to `AppEventType` enum
  - `OnUsageReceived` and `OnFinished` callbacks in `RiddleLlmService` for capturing token counts
  - Orange dollar icon badge for usage events in Event Log panel
  - Detailed usage breakdown: Prompt tokens, Completion tokens, Total tokens
  - Support for cache tokens and reasoning tokens when available

### Technical
- Integrated LLM Tornado SDK's `ChatStreamOptions.IncludeUsage` for streaming usage data
- Fallback handling when providers don't return usage data in streaming mode
- Helper method `LogUsage()` for consistent usage logging and event emission

### Notes
- OpenRouter via Grok model does not currently return token usage in streaming mode
- Event Log shows "Tokens: N/A" with explanation when provider doesn't support streaming usage

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

[Unreleased]: https://github.com/peakflames/riddle/compare/v0.5.1...HEAD
[0.5.1]: https://github.com/peakflames/riddle/compare/v0.5.0...v0.5.1
[0.5.0]: https://github.com/peakflames/riddle/compare/v0.4.1...v0.5.0
[0.4.1]: https://github.com/peakflames/riddle/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/peakflames/riddle/compare/v0.3.2...v0.4.0
[0.3.2]: https://github.com/peakflames/riddle/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/peakflames/riddle/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/peakflames/riddle/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/peakflames/riddle/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/peakflames/riddle/releases/tag/v0.1.0
