# Local Docker Deployment Guide

This guide covers deploying Project Riddle locally using Docker.

## Prerequisites

- **Docker** installed and running ([Get Docker](https://docs.docker.com/get-docker/))
- **Google OAuth credentials** ([Google Cloud Console](https://console.cloud.google.com/apis/credentials))
- **OpenRouter API key** ([OpenRouter](https://openrouter.ai/keys))

## Quick Start

### Option 1: Docker Run (Simplest)

```bash
docker run -d \
  --name riddle \
  -p 5000:5000 \
  -e GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com \
  -e GOOGLE_CLIENT_SECRET=your-client-secret \
  -e OPENROUTER_API_KEY=your-openrouter-api-key \
  -v riddle-data:/app \
  peakflames/riddle:latest
```

Then open http://localhost:5000 in your browser.

### Option 2: Docker Compose (Recommended)

1. **Create a project directory:**
   ```bash
   mkdir riddle && cd riddle
   ```

2. **Create a `.env` file** with your credentials:
   ```bash
   # .env
   GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
   GOOGLE_CLIENT_SECRET=your-client-secret
   OPENROUTER_API_KEY=your-openrouter-api-key
   ```

3. **Create `docker-compose.yml`:**
   ```yaml
   services:
     riddle:
       image: peakflames/riddle:latest
       container_name: riddle
       restart: unless-stopped
       ports:
         - "5000:5000"
       environment:
         - GOOGLE_CLIENT_ID=${GOOGLE_CLIENT_ID}
         - GOOGLE_CLIENT_SECRET=${GOOGLE_CLIENT_SECRET}
         - OPENROUTER_API_KEY=${OPENROUTER_API_KEY}
       volumes:
         - riddle-data:/app

   volumes:
     riddle-data:
       driver: local
   ```

4. **Start the container:**
   ```bash
   docker-compose up -d
   ```

5. **Access the application:** Open http://localhost:5000

## Setting Up Google OAuth

For local development, you need to configure Google OAuth to allow your local callback URL.

1. Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Create or select a project
3. Navigate to **APIs & Services > Credentials**
4. Create an **OAuth 2.0 Client ID** (Web application type)
5. Add authorized redirect URI: `http://localhost:5000/signin-google`
6. Copy the **Client ID** and **Client Secret**

## Image Tags

| Tag | Description |
|-----|-------------|
| `latest` | Latest stable release from `main` branch |
| `vX.Y.Z` | Specific version (e.g., `v0.23.0`) |
| `develop` | Latest development build (may be unstable) |

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `GOOGLE_CLIENT_ID` | Yes | Google OAuth Client ID |
| `GOOGLE_CLIENT_SECRET` | Yes | Google OAuth Client Secret |
| `OPENROUTER_API_KEY` | Yes | API key for LLM access |
| `ASPNETCORE_ENVIRONMENT` | No | Runtime environment (default: `Production`) |
| `ASPNETCORE_URLS` | No | Listen URLs (default: `http://+:5000`) |

## Data Persistence

The SQLite database (`riddle.db`) is stored inside the container at `/app/riddle.db`. To persist data across container restarts and updates:

- **Docker Run:** Use `-v riddle-data:/app`
- **Docker Compose:** Define a named volume (see example above)

### Backing Up Data

```bash
# Copy database from container
docker cp riddle:/app/riddle.db ./backup-riddle.db

# Restore database to container
docker cp ./backup-riddle.db riddle:/app/riddle.db
docker restart riddle
```

## Updating

To update to the latest version:

```bash
# Docker Compose
docker-compose pull
docker-compose up -d

# Docker Run
docker pull peakflames/riddle:latest
docker stop riddle && docker rm riddle
# Re-run the docker run command from Quick Start
```

## Troubleshooting

### Container won't start

Check the logs:
```bash
docker logs riddle
```

### Google OAuth errors

Verify that:
1. Your redirect URI exactly matches `http://localhost:5000/signin-google`
2. The OAuth consent screen is configured
3. The client ID and secret are correct

### Database issues

If you need to reset the database:
```bash
docker volume rm riddle-data
docker-compose up -d
```

## Ports

The container exposes port `5000` for HTTP traffic. The application does not include built-in HTTPS - if you need HTTPS, place a reverse proxy (nginx, Caddy, Traefik) in front of the container.

## Resource Requirements

- **Memory:** ~256-512 MB
- **Storage:** ~100 MB for image + database growth
- **CPU:** Minimal (single core is sufficient)
