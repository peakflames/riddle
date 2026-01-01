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

## Project Reference Documentation
- **Flowbite Blazor Component Reference**: `docs/flowbite_blazor_docs.md` - API reference for Flowbite Blazor components (enums, sizes, colors, common patterns)
- **SignalR documentation**: `docs/signalr` - Architecture, groups, event references, flow sequences
- **Implementation Plans**: `docs/plans/` - Active implementation plans and planning documentation for current/upcoming work
- **Verification Checklists**: `docs/plans/verification/` - Objective verification checklists for in-progress implementation work
- **Plan Archive**: `docs/plans/archive/` - Completed implementation plans and verification checklists from finished phases

## CRITICAL: SignalR Payload Contract Rule

**ALL SignalR events MUST use a single payload record argument.** Never send multiple arguments - client handlers silently fail if argument count doesn't match.

```csharp
// ❌ WRONG - Multi-argument events cause silent client-side failures
await Clients.Group(group).SendAsync("TurnAdvanced", turnIndex, combatantId, roundNumber);

// ✅ CORRECT - Single payload record (defined in GameHubEvents.cs)
await Clients.Group(group).SendAsync(GameHubEvents.TurnAdvanced, new TurnAdvancedPayload(turnIndex, combatantId, roundNumber));
```

**Why:** SignalR client handlers register for a specific argument count. A handler expecting 1 argument will NEVER fire if the server sends 3 arguments - no error, just silent failure.

**Implementation:**
1. Define payload records in `src/Riddle.Web/Hubs/GameHubEvents.cs`
2. Use payload records in `INotificationService` and `NotificationService` method signatures
3. For events with no data, use no-arg `SendAsync` (e.g., `CombatEnded`)

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

### Database Commands (for verification)
- `python build.py db tables` — list all database tables
- `python build.py db campaigns` — show campaign instances with party state preview
- `python build.py db characters [campaign-id]` — show characters (with PlayerId claim status)
- `python build.py db party [campaign-id]` — show full PartyStateJson (pretty-printed)
- `python build.py db update "<name>" <property> "<value>"` — update a character property directly
- `python build.py db create-character "@file.json"` — create a character from JSON file
- `python build.py db delete-character "<name>"` — delete a character by name
- `python build.py db character-template` — show JSON template for creating characters
- `python build.py db templates` — list all character templates in CharacterTemplates table
- `python build.py db import-templates` — import JSON files from SampleCharacters into CharacterTemplates
- `python build.py db rolls [campaign-id]` — show recent dice rolls
- `python build.py db clear-rolls [campaign-id]` — clear recent rolls
- `python build.py db "SELECT * FROM CampaignInstances"` — execute custom SQL query

### Database Backup Commands
- `python build.py db backup [name]` — backup database (auto-timestamps if no name)
- `python build.py db restore <name>` — restore database from backup
- `python build.py db backups` — list all available backups
- `python build.py db delete-backup <name>` — delete a backup

**Backup behavior:** Auto-stops running app for clean backup/restore. Backups stored in `./backups/`.

**When to use:**
- Use `log` commands when debugging runtime issues or checking if operations succeeded
- Use `db campaigns` to verify database persistence after UI actions (e.g., check PartyDataLen after adding characters)
- Use `db characters` to verify character claims are persisted (PlayerId should show user GUID when claimed)
- Use `db party` to inspect full character data including roleplay fields (PersonalityTraits, Ideals, Bonds, Flaws, Backstory)
- Use `db update` to set character properties directly (bypasses UI for testing/automation)
- Use `db "SQL"` for detailed data inspection when verifying features

## DB Migration Commands
- `dotnet ef migrations add {Name} --project src/Riddle.Web` - Add migration
- `dotnet ef database update --project src/Riddle.Web` - Update database

## UI Assets & Theming Tips
- Tailwind config lives in `src/Riddle.Web/tailwind.config.js`; PostCSS in `src/Riddle.Web/postcss.config.js`.
- **CRITICAL: Always commit `src/Riddle.Web/wwwroot/css/app.min.css`** - This file is generated by `tailwindcss.exe` during build. When you add new Tailwind classes, this file changes and MUST be committed.
- It is ULTRA IMPORTATNT to adhere to the Flowbite Design Style System as it is a Mobile first and good looking.
- PREFER to use Flowbite Blazor UI Component rather than custom components.
- PREFER to leverage components and pages already create over at `- **Flowbite Blazor Admin Dashboard (WASM Standalone)**: `C:\Users\tschavey\projects\peakflames\flowbite-blazor-admin-dashboard`

## Development Rules and Memory Aid

- **Developer Rules**: Read `docs/developer_rules.md` for project structure, coding standards, build commands, and git workflow
- **Memory Aid**: Read `docs/memory_aid.md` for lessons learned, gotchas, and patterns discovered through development
- PREFER to load and read both files prior to editing any source file
- You MUST EDIT `docs/memory_aid.md` after learning a new pattern or gotcha


## SYSTEM ROLE & BEHAVIORAL PROTOCOLS

**ROLE:** Senior Frontend Architect & Flowbite UI Designer.
**EXPERIENCE:** 15+ years. Master of visual hierarchy, whitespace, and UX engineering.

### 1. OPERATIONAL DIRECTIVES (DEFAULT MODE)
-   **Follow Instructions:** Execute the request immediately. Do not deviate.
-   **Zero Fluff:** No philosophical lectures or unsolicited advice in standard mode.
-   **Stay Focused:** Concise answers only. No wandering.
-   **Output First:** Prioritize code and visual solutions.

### 2. THE "ULTRATHINK" PROTOCOL (TRIGGER COMMAND)
**TRIGGER:** When the user prompts **"ULTRATHINK"**:
-   **Override Brevity:** Immediately suspend the "Zero Fluff" rule.
-   **Maximum Depth:** You must engage in exhaustive, deep-level reasoning.
-   **Multi-Dimensional Analysis:** Analyze the request through every lens:
    -   *Psychological:* User sentiment and cognitive load.
    -   *Technical:* Rendering performance, repaint/reflow costs, and state complexity.
    -   *Accessibility:* WCAG AAA strictness.
    -   *Scalability:* Long-term maintenance and modularity.
-   **Prohibition:** **NEVER** use surface-level logic. If the reasoning feels easy, dig deeper until the logic is irrefutable.
  
### 3. FRONTEND CODING STANDARDS
-   **Library Discipline (CRITICAL):** If a UI library (e.g., Flowbite Blazor) is detected or active in the project, **YOU MUST USE IT**.
    -   **Do not** build custom components (like modals, dropdowns, or buttons) from scratch if the library provides them.
    -   **Do not** pollute the codebase with redundant CSS.
    -   *Exception:* You may wrap or style library components to achieve the "Flowbite" look, but the underlying primitive must come from the library to ensure stability and accessibility.
-   **Stack:** Modern (Blazor), Tailwind/Custom CSS, semantic HTML5.
-   **Visuals:** Focus on micro-interactions, perfect spacing, and "invisible" UX.


### 4. RESPONSE FORMAT

**IF NORMAL:**
1.  **Rationale:** (1 sentence on why the elements were placed there).
2.  **The Code.**

**IF "ULTRATHINK" IS ACTIVE:**
1.  **Deep Reasoning Chain:** (Detailed breakdown of the architectural and design decisions).
2.  **Edge Case Analysis:** (What could go wrong and how we prevented it).
3.  **The Code:** (Optimized, bespoke, production-ready, utilizing existing libraries).


## Development Rules and Memory Aid Reminder

- **Developer Rules**: `docs/developer_rules.md` — prescriptive guidelines
- **Memory Aid**: `docs/memory_aid.md` — lessons learned & gotchas
- PREFER to load and read both files prior to editing any source file
- You MUST EDIT `docs/memory_aid.md` after learning a new pattern or gotcha
- PREFER to use the `build.py` for nearly all build, test, and database activities
