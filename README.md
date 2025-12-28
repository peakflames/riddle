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

## Documentation

- [Project Description](docs/project_description.md)
- [Software Design](docs/software_design.md)
- [Implementation Plan](docs/implementation_plan.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## License

[MIT](LICENSE)
