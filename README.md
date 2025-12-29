# Riddle

An LLM-driven Dungeon Master assistant for D&D 5th Edition.

## Features

- **AI Dungeon Master** - LLM-powered DM that tracks game state, manages combat, and narrates the story
- **Session Management** - Create and manage multiple campaign sessions
- **Real-time Updates** - SignalR-powered live game state synchronization
- **D&D 5e Rules** - Built-in rules reference and automated game mechanics

## Tech Stack

- ASP.NET Core 10.0 (Blazor Server)
- Entity Framework Core with SQLite/PostgreSQL
- OpenRouter LLM integration via LLM Tornado SDK
- Flowbite Blazor + Tailwind CSS
- Google OAuth authentication

## Quick Start

```bash
# Clone and navigate
git clone https://github.com/peakflames/riddle.git
cd riddle

# Build
python build.py

# Run
python build.py run
```

Open [http://localhost:5000](http://localhost:5000) in your browser.

## Gameplay Guide

### For Dungeon Masters

#### Starting a New Campaign

1. **Sign In** - Log in with your Google account
2. **Create Campaign** - Click "New Campaign" from the dashboard
   - Enter your campaign name (e.g., "Lost Mine of Phandelver")
   - Optionally add a description
   - Click "Create Campaign"
3. **Share Invite Code** - Once created, your campaign displays a unique **8-character invite code** (e.g., `ABC12DEF`)
   - Share this code with your players via Discord, text, email, etc.
   - Players use this code to join your campaign

#### Running a Session

1. **Open Campaign** - Click on your campaign from the dashboard
2. **Chat with the AI DM** - The AI Dungeon Master is ready to help you run the game
   - Describe scenarios and the AI will narrate
   - Ask for rules clarifications
   - Request NPC dialogue or descriptions
3. **Manage Party** - View connected players and their characters
4. **Combat Encounters** - The AI helps track initiative, HP, and combat actions

#### DM Tips

- Give the AI context about your campaign setting and current situation
- Ask the AI to generate "read-aloud" text for dramatic moments
- Use the AI to improvise NPC interactions
- The AI tracks game state automatically - ask it to summarize recent events

### For Players

#### Joining a Campaign

1. **Get Invite Code** - Your DM will share an 8-character code (e.g., `ABC12DEF`)
2. **Sign In** - Log in with your Google account
3. **Join Campaign** - Enter the invite code on the join page
4. **Create Character** - Fill out your character details:
   - Name, Race, Class, Level
   - Background and alignment
   - Ability scores and HP

#### During a Session

1. **View Dashboard** - See your character stats, inventory, and party info
2. **Follow the Story** - The DM's narration appears in real-time
3. **Take Actions** - Describe what your character does when it's your turn
4. **Combat** - Your character's initiative and HP are tracked automatically

#### Player Tips

- Keep your character sheet updated
- Watch for "read-aloud" narrative moments from the DM
- The AI remembers your character's backstory - reference it!
- Check the quest log to stay on track with objectives

### Game Flow Example

```
1. DM creates "Dragon's Lair" campaign → gets invite code "XYZ78ABC"
2. DM shares code with 4 players via Discord
3. Players join using the code and create characters
4. DM starts session: "You stand at the entrance of a dark cave..."
5. Players take turns describing actions
6. AI DM narrates outcomes, tracks combat, and manages NPCs
7. Session ends, game state is saved automatically
8. Next session picks up exactly where you left off
```

## LLM Tool Functions

The AI Dungeon Master has access to the following tools for managing game state and player interactions:

### `get_game_state`

Retrieves the full game state including character HP, locations, quests, and combat status. MUST be called first in any new conversation.

> **PARAMETERS**
>
> *None*

### `update_character_state`

Updates a character's HP, conditions, initiative, or status notes.

> **PARAMETERS**
>
> `character_id`* — ID of the character to update
>
> `key`* — The property to update (enum: `current_hp`, `conditions`, `status_notes`, `initiative`)
>
> `value`* — New value (int for HP/initiative, string[] for conditions, string for notes)

### `update_game_log`

Records an event to the narrative log for context recovery in future conversations.

> **PARAMETERS**
>
> `entry`* — Description of the event
>
> `importance` — Importance level of this event (enum: `minor`, `standard`, `critical`)

### `display_read_aloud_text`

Sends atmospheric, boxed narrative text to the DM's Read Aloud Text Box and player screens.

> **PARAMETERS**
>
> `text`* — The prose to display in the Read Aloud Text Box

### `present_player_choices`

Sends interactive choice buttons to player screens for decision points.

> **PARAMETERS**
>
> `choices`* — List of choices for players (string[])

### `log_player_roll`

Records a dice roll result to the player dashboard.

> **PARAMETERS**
>
> `character_id`* — Character who made the roll
>
> `check_type`* — Type of check (e.g., "Perception")
>
> `result`* — The dice roll result (integer)
>
> `outcome`* — Outcome of the roll (enum: `Success`, `Failure`, `Critical Success`, `Critical Failure`)

### `update_scene_image`

Updates the scene image displayed to players based on a description (for image generation/selection).

> **PARAMETERS**
>
> `description`* — Description for image generation or selection

---

*Parameters marked with `*` are required.*

## Documentation

- [Project Description](docs/project_description.md)
- [Software Design](docs/software_design.md)
- [Implementation Plan](docs/implementation_plan.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## License

[MIT](LICENSE)
