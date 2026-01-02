# Local Docker Deployment Guide

This guide covers deploying Project Riddle locally using Docker.

## Prerequisites

- **Docker** installed and running ([Get Docker](https://docs.docker.com/get-docker/))
- **Google OAuth credentials** ([Google Cloud Console](https://console.cloud.google.com/apis/credentials))
- **OpenRouter API key** ([OpenRouter](https://openrouter.ai/keys))

## Quick Start

### Option 1: Docker Compose (Recommended)

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
         - "1983:8080"
       environment:
         - ASPNETCORE_ENVIRONMENT=Production
         - Kestrel__Endpoints__Http__Url=http://+:8080
         - ConnectionStrings__DefaultConnection=Data Source=/app/data/riddle.db
       env_file:
         - .env
       volumes:
         - ./data:/app/data
       healthcheck:
         test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
         interval: 30s
         timeout: 10s
         retries: 3
         start_period: 15s
   ```

4. **Start the container:**
   ```bash
   docker compose up -d
   ```

5. **Access the application:** Open http://localhost:1983

> **Why port 1983?** That's the year the Dungeons & Dragons animated TV series debuted!

6. **Stop the container when done:**
   ```bash
   docker compose down
   ```

### Option 2: Docker Run

```bash
docker run -d \
  --name riddle \
  -p 1983:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Kestrel__Endpoints__Http__Url=http://+:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/riddle.db" \
  -e GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com \
  -e GOOGLE_CLIENT_SECRET=your-client-secret \
  -e OPENROUTER_API_KEY=your-openrouter-api-key \
  -v riddle-data:/app/data \
  peakflames/riddle:latest
```

Then open http://localhost:1983 in your browser.

To stop the container:
```bash
docker stop riddle
```

To remove the container (data is preserved in the volume):
```bash
docker rm riddle
```

## Setting Up Google OAuth

For local development, you need to configure Google OAuth to allow your local callback URL.

1. Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Create or select a project
3. Navigate to **APIs & Services > Credentials**
4. Create an **OAuth 2.0 Client ID** (Web application type)
5. Add authorized redirect URI: `http://localhost:1983/signin-google`
6. Copy the **Client ID** and **Client Secret**

## Image Tags

| Tag | Description |
|-----|-------------|
| `latest` | Latest stable release from `main` branch |
| `develop` | Latest development build from `develop` branch (may be unstable) |

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `GOOGLE_CLIENT_ID` | Yes | Google OAuth Client ID |
| `GOOGLE_CLIENT_SECRET` | Yes | Google OAuth Client Secret |
| `OPENROUTER_API_KEY` | Yes | API key for LLM access |
| `ASPNETCORE_ENVIRONMENT` | No | Runtime environment (default: `Production`) |
| `Kestrel__Endpoints__Http__Url` | Yes | Internal listen URL - use `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | Yes | SQLite database path - use `Data Source=/app/data/riddle.db` |

## Port Mapping

The container runs on port **8080 internally** and should be mapped to your preferred external port:

```
External (host) → Internal (container)
      1983      →       8080
```

- **Internal port (8080):** The ASP.NET Core app listens on this port inside the container
- **External port (1983):** The port you access in your browser

## Data Persistence

The SQLite database is stored at `/app/data/riddle.db` inside the container. To persist data across container restarts:

- **Docker Compose:** Use a bind mount (`./data:/app/data`) or named volume
- **Docker Run:** Use `-v riddle-data:/app/data`

The `data` directory will be created automatically and will contain:
- `riddle.db` - SQLite database
- `riddle.db-shm` - SQLite shared memory file (temporary)
- `riddle.db-wal` - SQLite write-ahead log (temporary)

### Backing Up Data

```bash
# Copy database from container
docker cp riddle:/app/data/riddle.db ./backup-riddle.db

# Restore database to container
docker cp ./backup-riddle.db riddle:/app/data/riddle.db
docker restart riddle
```

## Updating

To update to the latest version:

```bash
# Docker Compose
docker compose pull
docker compose down
docker compose up -d

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

Common issues:
- **HTTPS certificate errors:** The container is HTTP-only. Do not configure HTTPS endpoints.
- **Kestrel binding errors:** Ensure `Kestrel__Endpoints__Http__Url=http://+:8080` is set.

### Google OAuth errors

Verify that:
1. Your redirect URI exactly matches `http://localhost:1983/signin-google`
2. The OAuth consent screen is configured
3. The client ID and secret are correct

### Database issues

If you need to reset the database:
```bash
# Docker Compose
docker compose down
rm -rf ./data
docker compose up -d

# Docker Run
docker stop riddle && docker rm riddle
docker volume rm riddle-data
# Re-run docker run command
```

## Production Deployment Notes

For production deployments:

1. **Use HTTPS via reverse proxy:** Place nginx, Caddy, or Traefik in front of the container
2. **Use a proper database:** Consider PostgreSQL instead of SQLite for concurrent users
3. **Set up proper secrets management:** Don't commit `.env` files to version control
4. **Configure proper OAuth redirect URIs:** Update to your production domain

## Resource Requirements

- **Memory:** ~256-512 MB
- **Storage:** ~100 MB for image + database growth
- **CPU:** Minimal (single core is sufficient)
