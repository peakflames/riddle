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

## Run Your Own Game Server

Deploy your own Riddle instance with Docker. See the **[Docker Deployment Guide](docs/deployment/docker.md)** for complete setup instructions.

> **Why port 1983?** That's the year the Dungeons & Dragons animated series debuted!

## Gameplay Guide

### For Dungeon Masters

#### Starting a New Campaign

1. **Sign In** - Log in with your Google account
2. **Create Campaign** - Click "New Campaign" from the dashboard
3. **Add Characters** - Import pre-made characters or create new ones
4. **Share Invite Code** - Your campaign displays a unique **8-character invite code**
   - Share this code with your players via Discord, text, email, etc.
   - Players use this code to join and claim characters

#### Running a Session

1. **Open Campaign** - Click on your campaign from the dashboard
2. **Chat with the AI DM** - The AI Dungeon Master helps you run the game
   - Describe scenarios and the AI will narrate
   - Ask for rules clarifications
   - Request NPC dialogue or descriptions
3. **Combat Encounters** - The AI helps track initiative, HP, and combat actions

### For Players

#### Joining a Campaign

1. **Get Invite Code** - Your DM will share an 8-character code
2. **Sign In** - Log in with your Google account
3. **Join Campaign** - Enter the invite code on the join page
4. **Claim Character** - Select your character from the available roster

#### During a Session

- **View Dashboard** - See your character stats, inventory, and party info
- **Follow the Story** - The DM's narration appears in real-time
- **Combat** - Your character's initiative and HP are tracked automatically

## Development Setup

For contributors and developers:

```bash
# Clone and navigate
git clone https://github.com/peakflames/riddle.git
cd riddle

# Build
python build.py

# Import sample character templates
python build.py db import-templates

# Run (development mode)
python build.py run
```

Open http://localhost:5000 in your browser.

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines and build commands.

## Documentation

- [Docker Deployment](docs/deployment/docker.md) - Run your own game server
- [Project Description](docs/project_description.md) - Overview and goals
- [Software Design](docs/software_design.md) - Architecture details
- [Developer Rules](docs/developer_rules.md) - Coding standards

## License

This project is licensed under the [Creative Commons Attribution-NonCommercial 4.0 International License (CC BY-NC 4.0)](LICENSE.md).

### What This Means

✅ **You CAN:**
- View, study, and learn from the source code
- Fork and modify the code for personal or educational use
- Share your modifications with others (under the same license)
- Use this project to run games with friends

❌ **You CANNOT:**
- Use this project for commercial purposes
- Sell or monetize any derivative works
- Offer this as a paid service

If you're interested in commercial licensing, please reach out to discuss options.
