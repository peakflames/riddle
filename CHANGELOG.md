# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.28.4] - 2026-01-03

### Fixed
- **User-friendly OpenRouter API error messages**
  - Raw JSON errors like `{"error":{"message":"User not found.","code":401}}` now display as helpful messages
  - Error code 401: "Your OpenRouter API key is invalid or expired. Please verify your key at https://openrouter.ai/keys and update your .env file."
  - Error code 402: "Insufficient OpenRouter credits. Please add funds to your account at https://openrouter.ai/credits"
  - Error code 429: "Rate limit exceeded. Please wait a moment and try again."
  - 5xx errors: Generic service error message with retry suggestion
  - Added `ParseOpenRouterError()` helper in `RiddleLlmService` with JSON parsing and switch expression

## [0.28.3] - 2026-01-03

### Fixed
- **SignalR 403 Forbidden for player joins via Cloudflare tunnel**
  - Root cause: `NavigationManager.ToAbsoluteUri()` resolves to external URL (e.g., `riddle.peakflames.org`)
  - Cloudflare proxies block WebSocket upgrade requests with 403 Forbidden
  - Fix: Server-side `HubConnection` must ALWAYS connect to localhost (internal connection)

### Changed
- **Dynamic SignalR port detection** - `RealtimeBaseComponent.GetSignalRHubUrl()` improved:
  - Primary: Uses `IServer.Features.Get<IServerAddressesFeature>()` to get Kestrel's actual bound port at runtime
  - Fallback: `ASPNETCORE_HTTP_PORTS` or `ASPNETCORE_URLS` environment variables
  - Works for ANY deployment: dev (5000), local Docker (8080), self-hosted (arbitrary port)
  - No manual port configuration required

### Technical
- `RealtimeBaseComponent` now injects `IServer` from `Microsoft.AspNetCore.Hosting.Server`
- Normalizes wildcard bindings (`*`, `+`, `0.0.0.0`, `[::]`) to `localhost`
- Logging shows bound address source for debugging

## [0.28.2] - 2026-01-03

### Fixed
- **Character Templates search bar keyboard lag** - Replaced Flowbite `TextInput` with native HTML input
  - Same issue as Admin Settings (Flowbite TextInput laggy in Blazor Server InteractiveServer mode)
  - Applied consistent workaround pattern established in 0.28.1

## [0.28.1] - 2026-01-03

### Fixed
- **Admin Settings TextInput keyboard lag** - Replaced Flowbite `TextInput` with native HTML inputs
  - Flowbite TextInput causes laggy keyboard entry in Blazor Server InteractiveServer mode
  - Filed upstream issue: https://github.com/themesberg/flowbite-blazor/issues/15
  - Workaround: Use native `<input>` with Tailwind classes matching Flowbite styling

### Documentation
- Clarified CharacterTemplates ownership pattern in memory_aid.md: user templates can be private (`OwnerId` only) OR public (`IsPublic = true`)

## [0.28.0] - 2026-01-03

### Added
- **RealtimeBaseComponent Base Class**
  - Centralized SignalR connection URL logic for Docker/local environment detection
  - `GetSignalRHubUrl()` method detects `DOTNET_RUNNING_IN_CONTAINER` env var
  - `CreateHubConnection()` factory method with automatic reconnect policy
  - All SignalR-enabled components now inherit from this base class:
    - `Campaign.razor` (DM Dashboard)
    - `Dashboard.razor` (Player Dashboard)
    - `CombatTracker.razor`
    - `SignalRTest.razor`

- **Docker Commands in build.py**
  - `python build.py docker build` — build local Docker image
  - `python build.py docker run` — run container on port 8080
  - `python build.py docker stop` — stop and remove container
  - `python build.py docker status` — show container status
  - `python build.py docker logs` — show container logs
  - `python build.py docker shell` — open shell in running container

### Changed
- Documented port strategy: dev=5000, local Docker=8080, production=1983
- Updated AGENT.md, developer_rules.md, and memory_aid.md with Docker commands

### Technical
- `RealtimeBaseComponent.cs` in `Components/Shared/` directory
- Uses `DOTNET_RUNNING_IN_CONTAINER` for reliable Docker detection
- Falls back to `ASPNETCORE_HTTP_PORTS` (default 8080) for internal port

## [0.27.1] - 2026-01-02

### Fixed
- **SignalR HubConnection fails in Docker containers**
  - Root cause: `Navigation.ToAbsoluteUri("/gamehub")` resolved to external port (e.g., `localhost:1983`) which is unreachable from inside the container
  - Container listens on port 8080 internally; Docker's port mapping only works from host
  - Fix: Use `GetSignalRHubUrl()` helper that reads `ASPNETCORE_HTTP_PORTS` env var (default 8080)
  - Applied to both `Campaign.razor` (DM) and `Player/Dashboard.razor`

## [0.27.0] - 2026-01-02

### Changed

- Fix: Add AllowAnonymous to GameHub for reverse proxy compatibility

## [0.26.0] - 2026-01-01

### Added
- **Sidebar Icon Improvements**
  - `FlowbiteLogoIcon.razor` - Custom Flowbite logo icon for "Flowbite Blazor" sidebar link
  - `RobotIcon.razor` - Robot icon for "LLM Tornado" sidebar link (homage to the LLM Tornado SDK)
  - New sidebar entry linking to https://llmtornado.ai/

### Changed
- Flowbite Blazor sidebar link now uses official Flowbite logo icon instead of StarIcon
- Robot icon stroke width reduced to 1 for thinner, more consistent appearance with other icons

### Technical
- Custom icons inherit from `Flowbite.Base.IconBase` for consistent sizing/styling
- `FlowbiteLogoIcon` named to avoid conflict with existing `Flowbite.Icons.Extended.FlowbiteIcon`

## [0.25.0] - 2026-01-01

### Added
- **Multi-File Template Import**
  - "Import Files" button on Character Templates page for batch import
  - `DirectoryImportModal.razor` component with drag-and-drop file picker UI
  - Multi-file selection using Blazor `InputFile` with `multiple` attribute
  - Public/Private visibility toggle for all imported templates
  - File list preview with sizes and clear button
  - Progress bar during import with success/failure count summary
  - Error details for any failed imports
  - `ImportMultipleFromJsonAsync` service method with batch processing
  - Upsert pattern (creates or updates based on Name + OwnerId uniqueness)

### Technical
- `ICharacterTemplateService.ImportMultipleFromJsonAsync()` returns `BatchImportResult` record
- `BatchImportResult` contains SuccessCount, FailureCount, and Errors list
- Max 100 files per import, 1MB file size limit per file
- JSON validation with detailed error messages per file

## [0.24.0] - 2026-01-01

### Added
- **Docker Container Health Check**
  - `/health` endpoint for container orchestration (Docker, Kubernetes)
  - ASP.NET Core health checks middleware integration
  - Health check configuration in docker-compose.yml

- **Docker Deployment Documentation**
  - `docs/deployment/docker.md` comprehensive deployment guide
  - Local Docker deployment with Docker Compose
  - Docker Hub image: `peakflames/riddle:develop` (develop branch) and `peakflames/riddle:latest` (main branch)
  - Environment variable configuration for Google OAuth and OpenRouter API

### Changed
- RuntimeIdentifier removed from csproj (now set via CLI `-r linux-x64` for container builds only)
- GitHub Actions workflow updated to pass RuntimeIdentifier on container publish
- Local development builds now target native platform (Windows on Windows)

### Technical
- GitHub Actions workflow `.github/workflows/docker-publish.yml` for automated Docker Hub publishing
- MSBuild SDK container support (`Microsoft.NET.Build.Containers`)
- Container exposes port 8080 internally, configurable external mapping

## [0.23.0] - 2026-01-01

### Added
- **User Whitelist Feature**
  - Email-based user whitelist with enable/disable configuration
  - `AllowedUser` model with Email, DisplayName, IsActive, CreatedAt, AddedBy fields
  - `WhitelistSettings` configuration section in appsettings.json
  - `IAllowedUserService`/`AllowedUserService` for whitelist CRUD operations
  - Google OAuth `OnTicketReceived` event handler to enforce whitelist on sign-in
  - `/Account/AccessDenied` page with configurable rejection message
  - `/admin/settings` page for admins to manage whitelist (add/remove/toggle users)
  - "Admin Settings" link in sidebar (visible only to admins)
  - Admins bypass whitelist (always allowed to sign in)
  - Admin emails displayed at top of whitelist table with purple "Admin" badge
  - `GetAdminEmails()` method on `IAdminService` for retrieving configured admins

### Changed
- `AdminService` now uses `IOptionsMonitor<AdminSettings>` for hot-reload support
- Admin email changes in appsettings.json take effect on page refresh (no app restart needed)
- AGENT.md updated with "Verification Before Commit" rule (build ≠ verified)

### Technical
- EF Core migration `AddAllowedUsers` for AllowedUsers table
- `@using Riddle.Web.Services` added to _Imports.razor for global service access
- WhitelistSettings bound from configuration with `Enabled`, `AdminEmails`, `RejectionMessage`

## [0.22.0] - 2026-01-01

### Added
- **D&D 5e Rules Reference Pages**
  - `/rules/playing-the-game` - Core game mechanics from D&D 5e SRD
  - `/rules/glossary` - Rules glossary with terminology definitions
  - Markdown content loaded from `wwwroot/docs/` and rendered with Markdig
  - Sidebar navigation under "Game Reference" section with book and clipboard icons
  - Header dropdown (`RulesHelpDropdown.razor`) for quick access from any page
    - Custom dropdown with `right-0` positioning to prevent viewport overflow
    - Available in navbar next to dark mode toggle

### Changed
- Sidebar reorganized with new "Game Reference" collapsible section
- Rules pages use prose styling with `max-w-none` for full-width content

## [0.21.0] - 2026-01-01

### Added
- **Character Template Management (Phase 5 Feature)**
  - User-owned templates with `OwnerId` foreign key to AspNetUsers
  - Public/Private visibility toggle (`IsPublic` boolean, default: true)
  - Template filtering tabs: All Templates, My Templates, System Templates
  - Create Template form modal with all D&D 5e character fields
  - Edit Template functionality (owner or admin only)
  - Delete Template with confirmation (owner or admin only)
  - JSON Import modal with visibility selection (Public/Private toggle)
  - Schema Viewer modal for character JSON documentation
  - Admin role system via `AdminSettings:AdminEmails` in appsettings.json
  - `IAdminService`/`AdminService` for permission checks by email list

- **Debounced Search Bar**
  - Search templates by name, race, class, or spell names
  - 300ms debounce for responsive filtering
  - Clear button to reset search
  - Result count display ("Found X templates matching...")

### Changed
- Character Templates page UI refactored to use Flowbite Blazor components
- Search bar uses `<TextInput>` with `SearchIcon` instead of raw HTML input
- Template table shows Owner column ("System" badge vs user email)
- Template table shows Visibility column ("Public"/"Private" badges)

### Technical
- EF Core migration `AddIsPublicToCharacterTemplates` for visibility column
- `CharacterTemplateService` extended with ownership and visibility logic
- Import rules: users can import Public templates OR their own Private templates
- Edit/Delete permissions: creator OR admin can modify templates

## [0.20.1] - 2026-01-01

### Fixed
- **Player Dashboard `TurnAdvanced` SignalR handler silent failure**
  - Handler used wrong signature `On<int, string, int>` expecting 3 positional args
  - Server sends `TurnAdvancedPayload` record (single arg)
  - SignalR handlers silently ignore events when arg count doesn't match
  - Players never saw turn advancement updates during combat
  - Fixed to use `On<TurnAdvancedPayload>` matching CombatTracker.razor pattern

## [0.20.0] - 2025-12-31

### Fixed
- **Player Dashboard Combat Tracker not receiving death save updates via SignalR**
  - Root cause: `DeathSaveUpdated` handler violated JSON-backed property pattern
  - Each access to `campaign.PartyState` deserialized fresh from JSON, so modifications were lost
  - Fix: Get `PartyState` once, modify character, set back to trigger re-serialization
  - Added E2E test `HLR_COMBAT_032` to catch this class of bug

- **PC Death Save UI issues in Combat Tracker**
  - CombatantCard showed "Defeated" badge for ALL combatants at 0 HP (including PCs)
  - Fixed to show "Unconscious" for PCs making death saves, "Defeated" only for enemies/NPCs
  - `update_character_state` tool enum was missing `death_save_success`, `death_save_failure`, `stabilize` keys

### Added
- E2E test `HLR_COMBAT_032_Player_Dashboard_Combat_Tracker_receives_death_save_updates_via_SignalR`
- Additional death save display tests (HLR_COMBAT_027 through HLR_COMBAT_031)

### Technical
- Documented JSON-backed property pattern for SignalR handlers in memory_aid.md
- Pattern: `var list = campaign.Property; modify(list); campaign.Property = list;`

## [0.19.0] - 2025-12-31

### Added
- **D&D 5e Death Saving Throw Mechanics**
  - `DeathSaveSuccesses` and `DeathSaveFailures` properties on Character model
  - Visual death save tracker UI on CombatantCard (skull icons for failures, heart icons for successes)
  - Death save tracker shows only for defeated PCs (0 HP)
  - `reset_death_saves` LLM tool to clear death saves (e.g., when healed)
  - `update_character_state` tool supports `death_save_successes` and `death_save_failures` properties
  - D&D 5e death save rules in system prompt:
    - Roll d20 at start of turn when at 0 HP
    - 10+ = success, 9- = failure
    - Natural 20 = regain 1 HP and consciousness
    - Natural 1 = 2 failures
    - 3 successes = stabilized, 3 failures = dead
  - 11 new E2E tests for death save mechanics (HLR_COMBAT_016 through HLR_COMBAT_026)

### Fixed
- **Combat Tracker HP synchronization bug** - Combat Tracker now shows correct HP after page reload
  - Root cause: Dual data sources (PartyState vs ActiveCombat.Combatants) caused stale HP in Combat Tracker
  - Solution: PartyState is now single source of truth for PC HP
  - `BuildCombatStatePayload()` overlays fresh PartyState HP onto PC combatants
  - Removed redundant dual-update code from ToolExecutor

### Changed
- **`build.py test` now auto-stops running app** before tests to prevent ObjectDisposedException

### Technical
- `CombatService.BuildCombatStatePayload()` accepts optional `partyState` parameter
- All 7 callers updated to pass PartyState for HP lookup
- Memory aid updated with HP synchronization pattern documentation

## [0.18.0] - 2025-12-31

### Added
- **14 Player Dashboard E2E Tests** (`PlayerDashboardTests.cs`)
  - HLR_PLAYER_001: Player Dashboard renders correctly
  - HLR_PLAYER_002: Player sees own character card
  - HLR_PLAYER_003: Player sees Read Aloud text
  - HLR_PLAYER_004: Player sees player choices
  - HLR_PLAYER_005: Player can submit choice
  - HLR_PLAYER_007: Player sees dice roll history
  - HLR_PLAYER_008: Player character HP updates in real-time
  - HLR_PLAYER_009: Player sees combat tracker when combat active
  - HLR_PLAYER_010: Player sees party members during combat
  - HLR_PLAYER_011: Player cannot see enemy HP (shows health description instead)
  - HLR_PLAYER_012: Player can see own character HP
  - HLR_PLAYER_013: Player sees turn indicator in combat
  - HLR_PLAYER_014: Player choice submission updates DM view
  - HLR_PLAYER_015: Player Dashboard SignalR reconnects after disconnect

- **Enemy HP Hidden from Players** (HLR_PLAYER_011)
  - `CombatantCard.razor` gains `IsPlayerView` parameter
  - Players see health description badges instead of exact HP for enemies:
    - "Healthy" (green) - >75% HP
    - "Bloodied" (yellow) - 25-75% HP
    - "Near Death" (red) - 1-25% HP
    - "Critical" (dark red) - 0% HP
  - DM still sees exact HP values for all combatants
  - Players can see exact HP for their own character

### Changed
- **Solution format upgraded to SLNX** (.NET 10 XML-based solution format)
  - Replaced `Riddle.sln` (GUID-heavy) with `Riddle.slnx` (clean XML)
  - Solution now includes both web app and integration tests
- **`build.py` now builds full solution**
  - `python build.py` builds both `Riddle.Web` and `Riddle.Web.IntegrationTests`
  - Eliminates separate test project build step

### Technical
- `CombatantCard.razor`: New `IsPlayerView` parameter propagated from `CombatTracker`
- Health description logic: `GetHealthDescription()` and `GetApproximateHealthWidth()` helper methods
- `data-testid='health-description'` attribute for E2E test selectors

## [0.17.0] - 2025-12-31

### Added
- **14 Combat Encounter E2E Tests** (`CombatEncounterTests.cs`)
  - HLR_COMBAT_001: DM starts combat from narrative
  - HLR_COMBAT_002: DM inputs initiative rolls
  - HLR_COMBAT_003: Combat includes enemy combatants
  - HLR_COMBAT_004: Turn order displays correctly
  - HLR_COMBAT_005: DM advances to next turn
  - HLR_COMBAT_006: Round advances after all turns
  - HLR_COMBAT_007: Damage is applied to enemy
  - HLR_COMBAT_008: Enemy is defeated (shows defeated badge)
  - HLR_COMBAT_009: Surprise round handling
  - HLR_COMBAT_010: Player takes damage
  - HLR_COMBAT_012: All enemies defeated auto-ends combat
  - HLR_COMBAT_013: DM ends combat manually
  - HLR_COMBAT_014: Player sees combat updates
  - HLR_COMBAT_015: Turn order syncs across all clients

- **`data-testid` attributes for Combat UI**
  - `CombatTracker.razor`: `combat-tracker`, `round-number`
  - `CombatantCard.razor`: `combatant-{Id}`, `current-turn-indicator`, `surprised-badge`, `defeated-badge`, `initiative`, `hp-current`, `hp-max`

### Fixed
- **Defeated combatants now visible in Combat Tracker** (D&D VTT style)
  - `MarkDefeatedAsync` no longer removes defeated from TurnOrder
  - Defeated combatants display with "💀 Defeated" badge
  - `AdvanceTurnAsync` automatically skips defeated combatants
  - Matches Roll20/Foundry VTT behavior (defeated stay visible for narrative/looting)
- **LLM visibility**: `get_combat_state` tool shows defeated status in Status column

### Technical
- Playwright E2E tests verify full SignalR flow: Service action → SignalR event → Blazor UI → DOM
- Tests use `Expect().ToHaveTextAsync()` polling for async SignalR propagation
- `CustomWebApplicationFactory.SetupTestCampaignAsync()` creates isolated test campaigns

## [0.16.0] - 2025-12-31

### Fixed
- **Combat Tracker HP not updating when LLM calls `update_character_state` tool**
  - Root cause: `CombatTracker.razor` expected PascalCase `"CurrentHp"` but `ToolExecutor.cs` sends snake_case `"current_hp"` (matching LLM JSON convention)
  - Changed `CombatTracker.razor` to check for `"current_hp"` instead of `"CurrentHp"`
  - This was a sender/receiver contract mismatch that passed all transport-layer tests

### Added
- **Playwright E2E Test Infrastructure**
  - `UpdateCharacterStateToolTests` - E2E test for `update_character_state` LLM tool
  - Verifies full flow: Tool execution → SignalR → Blazor UI → DOM update
  - Catches sender/receiver contract mismatches that transport tests cannot detect
  - `CustomWebApplicationFactory` with Donbavand/Costello dual-host pattern for Playwright + WebApplicationFactory
  - `PlaywrightFixture` for browser lifecycle management
  - Test authentication: `TestAuthHandler` + `TestAuthenticationStateProvider`
  - In-memory database isolation for test campaigns

### Changed
- **E2E Testing Philosophy Documentation** (`docs/e2e_testing_philosophy.md`)
  - Rewritten to focus on "Test Tools, Not Transport" philosophy
  - Documents `{ToolName}ToolTests` naming convention
  - Includes canonical test structure and real code examples
  - Captures 9 hard-won lessons from debugging test infrastructure

### Removed
- Transport-layer SignalR tests (`HubTests/`, `Services/`) - replaced by E2E tests that verify actual behavior

### Technical
- E2E tests use `data-testid` attributes for reliable DOM selectors
- `Expect()` polling pattern for async SignalR propagation
- `WaitUntil.NetworkIdle` for Blazor Server async rendering
- Kestrel binds to dynamic port via `IPAddress.Loopback`

## [0.15.0] - 2025-12-30

### Added
- **Character Template Library**
  - Reusable character templates for quick campaign setup
  - `CharacterTemplate` entity with JSON blob storage for full character data
  - Shadow columns for Race, Class, Level for efficient querying
  - System templates (OwnerId = null) vs user templates
  - 10 sample system templates imported from SampleCharacters folder
  - `/dm/templates` page with Flowbite Table displaying all templates
  - "View" button to open read-only `CharacterViewModal` for full character details
  - "Owner" column showing "System" badge for system templates

- **Character Template Import**
  - Import characters from templates directly into campaigns
  - `CharacterTemplatePickerModal` with multi-select checkbox table
  - DM Dashboard Party card "Import" icon button
  - `CopyToCampaignAsync` service method creates fresh character instances
  - Characters get new UUID v7 IDs and null PlayerId when imported

- **CharacterViewModal Component**
  - 6-tab read-only character detail view (Combat, Abilities, Skills, Spells, Equipment, Roleplay)
  - Visual enhancements: borders around values, semibold labels, styled ability boxes
  - Reusable for both template viewing and character inspection

- **UI Improvements**
  - Party card header buttons converted to icon-only style with hover tooltips
  - Matches CharacterCard action button pattern for consistency

### Technical
- `ICharacterTemplateService` with CRUD operations and campaign import
- EF Core migration `AddCharacterTemplates` for CharacterTemplates table
- `GetAllAvailableTemplatesAsync(userId)` returns system + user's templates
- Navigation property `Owner` for user templates with `.Include()`
- Sidebar link to Character Templates page

## [0.14.3] - 2025-12-30

### Fixed
- **Player Choices rows horizontally centered on DM Dashboard**
  - Added `text-left` and `justify-start` classes to fix alignment
  - Choice option rows now align to the left as expected

## [0.14.2] - 2025-12-30

### Fixed
- **Player Dashboard dice rolls not updating in real-time**
  - Root cause: Player Dashboard was not subscribed to `GameStateService.OnCampaignChanged` events
  - Added `IGameStateService` injection and `HandleCampaignChanged` event subscription
  - New rolls now appear immediately when LLM calls `log_player_roll` tool
  - Also handles `ActivePlayerChoices` updates as backup to SignalR handler
- **DM Dashboard character state not updating in real-time**
  - Root cause: `ToolExecutor` called `UpdateCharacterAsync` but no SignalR notification was broadcast
  - Added `NotifyCharacterStateUpdatedAsync` call after character state updates in ToolExecutor
  - HP, conditions, and other character changes now update immediately on all connected dashboards
  - Added `CharacterStateUpdated` SignalR handler to DM Campaign page

### Technical
- Event-driven pattern: Subscribe to `OnCampaignChanged` in `OnInitializedAsync`, unsubscribe in `DisposeAsync`
- Documented "SignalR notification gap" pattern in developer memory aid: always broadcast after state mutations

## [0.14.1] - 2025-12-30

- Add implementation plan for 4 critical campaign management bugs:
  - Delete campaign UI missing
  - LLM not proactive in suggesting next steps
  - Combat tracker HP not updating via SignalR
  - Player character claims lost on page refresh
- Document update_character_state tool requirement to search both
  PartyState (PCs) and ActiveCombat.Combatants (enemies/allies)
- Include code example for dual data source lookup pattern

## [0.14.0] - 2025-12-29

### Added
- **Phase 4: Real-Time Game Hub (Objective 5) - Atmospheric LLM Tools**
  - Three new LLM tools for immersive player experience:
    - `broadcast_atmosphere_pulse` - Send fleeting sensory text to player screens
      - Parameters: `text` (required), `intensity` (low/medium/high), `sensory_type` (sound/smell/visual/feeling)
      - Auto-dismisses after 10 seconds
      - Visual styling varies by intensity (red pulse for high, amber for medium, purple for low)
    - `set_narrative_anchor` - Update persistent mood banner on player screens
      - Parameters: `short_text` (required), `mood_category` (danger/mystery/safety/urgency)
      - Persists until explicitly changed
      - Color-coded by mood (red for danger, purple for mystery, green for safety, amber for urgency)
    - `trigger_group_insight` - Flash discovery notification on player screens
      - Parameters: `text` (required), `relevant_skill` (required), `highlight_effect` (optional)
      - Auto-dismisses after 8 seconds
      - Shows skill badge and optional pulse animation
  - Three new SignalR events for atmospheric updates (player-only):
    - `AtmospherePulseReceived`, `NarrativeAnchorUpdated`, `GroupInsightTriggered`
  - NotificationService methods for atmospheric broadcasting
  - Player Dashboard atmospheric UI components:
    - Narrative Anchor banner at top of page
    - Atmosphere Pulse panel in left column
    - Group Insight flash card with skill badge
  - System prompt guidance for using atmospheric tools during gameplay

### Changed
- Removed Scene Image Card from Player Dashboard (DM-only content)
- Removed Read-Aloud Text Card from Player Dashboard (atmospheric tools provide this now)

### Fixed
- Narrative Anchor not updating on subsequent uses (Blazor state detection issue)

### Technical
- `AtmospherePulsePayload`, `NarrativeAnchorPayload`, `GroupInsightPayload` records in GameHubEvents
- CancellationTokenSource pattern for auto-dismiss timers
- Created new object reference on update to trigger Blazor re-render

## [0.13.0] - 2025-12-29

### Added
- **Phase 4: Real-Time Game Hub (Objective 4)**
  - Real-time Player Choice Submission:
    - Player Dashboard submits choices via SignalR `SubmitChoice` method
    - Players can change their choice before DM proceeds (button turns green with checkmark)
    - New choices from LLM automatically reset player submission state
  - DM Per-Player Choice Indicators:
    - Visual display showing who chose which option with character name badges
    - "X/Y responded" counter in Player Choices card header
    - "Waiting for: [names]" section with yellow badges for pending players
    - "✓ All players have responded" message when complete
  - Bidirectional SignalR communication:
    - `PlayerChoicesReceived` event pushes new choices to players
    - `PlayerChoiceSubmitted` event notifies DM of player selections
    - ToolExecutor now calls `NotifyPlayerChoicesAsync` when LLM sets choices

### Fixed
- Player Dashboard not receiving new choices when LLM reset them (missing SignalR notification)

### Technical
- `ToolExecutor` injected with `INotificationService` for SignalR broadcasting
- Player choice state tracked per-character for multi-player support
- UI uses Flowbite Blazor Badge and Button components with dynamic colors

## [0.12.0] - 2025-12-29

### Added
- **Phase 4: Real-Time Game Hub (Objective 3 & 3.5)**
  - Combat Tracker component (`CombatTracker.razor`):
    - Real-time turn order display with initiative values
    - Current turn indicator (▶) and position numbers
    - HP display with color-coded health badges
    - Type icons (🧙 PC, 👹 Enemy, 📜 NPC)
    - Compact card layout with header showing round number
    - DM controls: Next Turn button, End Combat button
    - Read-only mode for players (`IsDm="false"`)
  - CombatService (`ICombatService`/`CombatService`):
    - `StartCombatAsync()` - Initialize combat with combatants
    - `AdvanceTurnAsync()` - Advance to next combatant, auto-increment rounds
    - `EndCombatAsync()` - End combat encounter
    - `GetCombatStateAsync()` - Retrieve current combat state as `CombatStatePayload`
    - `UpdateCombatantHpAsync()` - Update combatant HP during combat
    - Automatic round tracking and turn wrapping
  - LLM Combat Tools:
    - `start_combat` - Start combat with PC names and enemy definitions
    - `advance_turn` - Advance to next combatant
    - `end_combat` - End the combat encounter
    - `apply_damage` / `apply_healing` - Modify combatant HP
  - Combat Tracker on Player Dashboard:
    - Real-time SignalR updates for combat state changes
    - Shows turn order, current turn, round number
  - Party Members card on Player Dashboard:
    - Shows other PCs in party with HP/AC/DEX stats
    - Class icons and color-coded health badges
  - Player Dashboard 3-column layout on XL screens:
    - Left: Scene Image, Read Aloud, Player Choices, Dice Rolls
    - Middle: Combat Tracker + Party Members (sticky)
    - Right: Character Card (sticky)

### Fixed
- CombatEnded SignalR event not clearing Combat Tracker UI (was mutating `[Parameter]` directly)
- Round number not updating during `advance_turn` (added `RoundNumber` to `TurnAdvanced` event)
- PC lookup failing after `end_combat`→`start_combat` (normalize LLM names: underscores to spaces)
- CS8602 null reference warnings in Campaign.razor with null-forgiving operators

### Technical
- `CombatStatePayload` record for SignalR combat state broadcasting
- `CombatantInfo` record for turn order data
- TurnAdvanced SignalR event uses individual params `(int, string, int)` not payload record
- Blazor `[Parameter]` anti-pattern documented: never mutate directly, use EventCallback
- LLM name normalization pattern: replace underscores with spaces before character lookup
- JSON `[NotMapped]` property pattern: capture to local variable before modification

## [0.11.0] - 2025-12-29

### Added
- **Phase 4: Real-Time Game Hub (Objective 2)**
  - NotificationService for centralized SignalR event broadcasting:
    - `INotificationService` interface with 14 notification methods
    - `NotificationService` implementation using `IHubContext<GameHub>`
  - Notification methods by category:
    - Character events: `NotifyCharacterClaimedAsync`, `NotifyCharacterReleasedAsync`
    - Player connection: `NotifyPlayerConnectedAsync`, `NotifyPlayerDisconnectedAsync`
    - Game state: `NotifyCharacterStateUpdatedAsync`, `NotifyReadAloudTextAsync`, `NotifySceneImageUpdatedAsync`, `NotifyPlayerChoicesAsync`
    - Player actions: `NotifyPlayerChoiceSubmittedAsync`, `NotifyPlayerRollLoggedAsync`
    - Combat events: `NotifyCombatStartedAsync`, `NotifyCombatEndedAsync`, `NotifyTurnAdvancedAsync`, `NotifyInitiativeSetAsync`
  - Group routing for targeted broadcasts:
    - DM group: CharacterClaimed, CharacterReleased, PlayerConnected, PlayerDisconnected, PlayerChoiceSubmitted
    - Players group: PlayerChoicesReceived
    - All group: CharacterStateUpdated, SceneImageUpdated, combat events, PlayerRollLogged
  - CharacterService integration:
    - Broadcasts `CharacterClaimed` when player claims character
    - Broadcasts `CharacterReleased` when DM unclaims character

### Technical
- NotificationService registered as scoped service in DI
- Uses `IHubContext<GameHub>` for server-side SignalR broadcasting
- Group naming: `campaign_{id}_dm`, `campaign_{id}_players`, `campaign_{id}_all`

## [0.10.0] - 2025-12-29

### Added
- **Phase 4: Real-Time Game Hub (Objective 1)**
  - SignalR GameHub (`/gamehub`) for real-time game communication:
    - `JoinCampaign(campaignId, userId, characterId, isDm)` - Join campaign with group assignment
    - `LeaveCampaign(campaignId)` - Leave campaign and notify DM
    - `SubmitChoice(campaignId, characterId, characterName, choice)` - Submit player choices
    - `OnDisconnectedAsync()` - Automatic cleanup on disconnect
  - Connection tracking infrastructure:
    - `IConnectionTracker`/`ConnectionTracker` singleton for in-memory connection state
    - Track userId, characterId, and isDm per connection
    - Map connections to campaign groups
  - SignalR event contracts (`GameHubEvents.cs`):
    - Event constants: `CharacterClaimed`, `PlayerConnected`, `PlayerDisconnected`, `PlayerChoiceSubmitted`, etc.
    - Payload records: `CharacterClaimPayload`, `PlayerConnectionPayload`, `PlayerChoicePayload`, `CombatStatePayload`, `CombatantInfo`
  - SignalR test page (`/test/signalr`) for hub verification
  - Group naming convention: `campaign_{id}_dm`, `campaign_{id}_players`, `campaign_{id}_all`

### Technical
- Added `Microsoft.AspNetCore.SignalR.Client` NuGet package for client connections
- SignalR services registered in Program.cs with `/gamehub` endpoint
- `ConnectionTracker` uses `ConcurrentDictionary` for thread-safe state management

## [0.9.0] - 2025-12-29

### Added
- **Conversation History Support**
  - LLM now receives full conversation history for context continuity
  - Previous messages preserved across chat interactions within session
  - `LlmConversationMessage` record for structured message history

- **File Attachment Support**
  - Drag-and-drop file attachments in DM Chat
  - Image attachments (PNG, JPG, GIF, WebP) sent to LLM as base64 multipart messages
  - Text file attachments (.txt, .md, .markdown, .log) included as content
  - 5MB file size limit with validation
  - PDF attachments rejected with user-friendly error message
  - `LlmAttachment` record for attachment data modeling

- **Enhanced DmChat Component**
  - `PromptInputHeader` with `PromptInputAttachments` for file management
  - Attachment preview in chat messages (images displayed, files show name/size)
  - Visual validation feedback for unsupported attachments

### Changed
- `IRiddleLlmService.ProcessDmInputAsync()` signature extended with optional `conversationHistory` and `attachments` parameters
- `RiddleLlmService` builds multipart messages when attachments include images
- Preserves existing 12 tool calling functionality while adding multimodal support

### Technical
- `DmChat.razor` refactored to code-behind pattern (`DmChat.razor.cs`) for maintainability
- Attachment helper methods ported from Flowbite Blazor ChatAiPage reference implementation
- `LlmConversationMessage` and `LlmAttachment` records in Models folder
- Base64 encoding for image data with proper MIME type handling

## [0.8.0] - 2025-12-29

### Added
- **Enhanced LLM Tool System**
  - `display_read_aloud_text` tool now supports `tone` and `pacing` parameters for delivery guidance
  - Four new query tools for LLM context recovery:
    - `get_game_log` - Retrieves narrative log entries in markdown table format (limit param, default 50)
    - `get_player_roll_log` - Retrieves dice roll history in markdown table format
    - `get_character_property_names` - Returns categorized list of 45 queryable character properties
    - `get_character_properties` - Retrieves specific properties for characters in markdown table

- **Read-Aloud Text Tone/Pacing UI**
  - Visual badges above read-aloud text showing tone (🎭) and pacing (⏱️)
  - Color-coded tone badges: ominous=gray, tense=pink, mysterious=purple, cheerful=green, etc.
  - Color-coded pacing badges: slow=blue, fast=pink, building=warning

- **System Prompt Enhancement**
  - LLM startup sequence now calls `get_game_log()` after `get_game_state()` for full context recovery
  - Enables better session continuity when resuming campaigns

### Technical
- `CharacterPropertyGetters` static dictionary in `ToolExecutor` with 45 property mappings for safe character property access
- `CurrentReadAloudTone` and `CurrentReadAloudPacing` properties on `CampaignInstance` model
- EF Core migration `AddReadAloudTonePacing` for new database columns
- Updated `IGameStateService.SetReadAloudTextAsync()` signature with tone/pacing parameters
- Real-time UI updates via `OnCampaignChanged` events for tone/pacing changes

## [0.7.0] - 2025-12-28

### Added
- **Dice Rolling System**
  - `RollResult` model for persisting roll data with character name, check type, result, and outcome
  - `RecentRollsJson` column on `CampaignInstance` for storing last 50 rolls
  - `AddRollAsync()` in `IGameStateService` for recording dice rolls with automatic trimming
  - EF Core migration `AddRecentRollsJson` for new column
  - Real-time roll updates via `OnCampaignChanged` event with "RecentRolls" property

- **Compact Dice Rolls UI (DM & Player Dashboards)**
  - Single-row compact design: Character | Check Type | Result | Outcome Badge | Time
  - Max 50 rolls displayed with scrollable container
  - Color-coded outcome badges (✨ crit, ✓ pass, ✗ fail, 💀 crit fail)
  - Short time format (now, 5m, 2h, 3d)

- **Application Version Display**
  - Version number `v0.7.0-alpha` shown in navbar next to "Riddle" brand
  - Visible on all pages using StackedLayout

### Changed
- **DM Dashboard now uses StackedLayout** - Removed sidebar for cleaner full-width experience
- **Player Dashboard column order swapped** - Game content (Read-Aloud, Choices) now appears first on mobile for better UX
- **Player Dashboard cards restyled** to match DM Dashboard:
  - Read-Aloud Text: Standard gray card with amber left-border content box
  - Player Choices: Added icon + badge count header with divider
  - Consistent `CardSize.ExtraLarge` and `p-4` padding wrapper pattern

### Removed
- Location Info card from Player Dashboard (showed chapter/location info)
- Sidebar navigation on DM Dashboard (now uses full-width StackedLayout)

### Technical
- `StackedLayout` used consistently across DM and Player dashboards
- Card pattern: `<Card Size="CardSize.ExtraLarge">` + inner `<div class="p-4">` wrapper

## [0.6.0] - 2025-12-28

### Added
- **Phase 3: Party Management & Character Creation (Objective 5)**
  - Player Join Flow:
    - `/join` page with manual invite code entry
    - `/join/{InviteCode}` route for direct link joining
    - Multi-step workflow: code validation → authentication → character selection
    - Automatic redirect to Player Dashboard after claiming character
  - Character Service (`ICharacterService`/`CharacterService`):
    - `GetAvailableCharactersAsync()` - Get unclaimed PCs
    - `ClaimCharacterAsync()` - Claim a character for a player
    - `GetPlayerCharactersAsync()` - Get player's claimed characters
    - `ValidateInviteCodeAsync()` - Validate invite codes
  - Player Dashboard (`/player/{CampaignId}`):
    - Full character card display with all D&D 5e properties
    - Stats, abilities, skills, equipment, spells, roleplay fields
    - Responsive grid layout with Flowbite Blazor components
  - Sample Characters:
    - Elara Moonshadow (Half-Elf Cleric L5) - full spellcaster with backstory
    - Zeke Shadowstep (Lightfoot Halfling Rogue L5) - rogue with full skills
  - Build CLI Enhancements (`build.py`):
    - `db update "<name>" <property> "<value>"` - Direct character property updates
    - `db create-character "@file.json"` - Create character from JSON file
    - `db delete-character "<name>"` - Remove character by name
    - `db character-template` - Show JSON template for character creation

### Fixed
- Flowbite Blazor Textarea binding in TabPanels - use native HTML textarea with @bind
- JSON `[NotMapped]` property pattern - capture list in local variable before modifying

### Technical
- `StackedLayout.razor` for consistent page structure
- `PlayerCharacterCard.razor` and `AbilityScoreDisplay.razor` components
- Enhanced `CharacterFormModal.razor` with roleplay tab and native textareas
- Documented Flowbite Textarea TabPanel bug in developer memory aid

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
