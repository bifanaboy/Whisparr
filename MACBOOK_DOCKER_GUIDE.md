# Running Whisparr via Docker on MacBook

This guide explains how to run your customized Whisparr app in Docker with minimal disk space usage, avoiding the Colima >40GB issue.

## Why This Approach Solves Your Problems

1. **No Colima bloat** - Uses native Docker Desktop or OrbStack, no VM disk image growing
2. **Auto-updates on PR merge** - GitHub Actions builds and pushes to ghcr.io whenever you push to `v2-develop` or `v2`
3. **Minimal SSD usage** - Multi-stage Docker builds produce ~100MB images (vs Colima's 40GB VM)

## Prerequisites

- **Docker Desktop** (recommended: [OrbStack](https://orbstack.dev/) - ~200MB vs Docker Desktop's ~4GB) or Docker Desktop
- GitHub account with write access to the repo
- Terminal access

## Quick Start (OrbStack or Docker Desktop)

### 1. Build and run locally

```bash
# Build the image (first time only, subsequent pulls from GHCR)
docker build -t whisparr:local .

# Or pull the latest from GitHub Actions:
docker pull ghcr.io/WHISPARR_OWNER/whisparr:latest

# Run with data persistence
docker run -d \
  --name whisparr \
  -p 6969:6969 \
  -e PUID=1000 \
  -e PGID=1000 \
  -e UMASK=002 \
  -e TZ=Etc/UTC \
  -e WEBUI_PORTS=6969/tcp \
  -v $(pwd)/config:/config \
  -v $(pwd)/data:/data \
  whisparr:local
```

### 2. Access the UI

Open `http://localhost:6969` in your browser.

### 3. Data persistence

- `config/` folder: Stores Whisparr configuration
- `data/` folder: Stores your movie library data
- These are bind-mounted from your host, survive container restarts

## Auto-Updating on PR Merge

The `.github/workflows/docker.yml` workflow triggers on every push to `v2-develop` or `v2`:

- On merge: builds Docker image and pushes to `ghcr.io/<your-owner>/whisparr:latest`
- On PR open: builds image tagged `develop` for testing
- The `latest` tag always points to the most recent merged `v2-develop` commit

**To update:** Just merge your PR to `v2-develop` → GitHub Actions runs → new image available → restart container or pull new image

## Disk Space Optimization

### Why Images Are Small (~100MB vs 40GB VM)

1. **Multi-stage build**: Builder stage (SDK) is discarded; only runtime output remains
2. **`mcr.microsoft.com/dotnet/aspnet:10.0`** base image (~100MB) vs Colima's full Linux VM
3. **Self-contained publish**: Only the Whisparr binary and required deps, no full .NET SDK
4. **Layer caching**: Rebuilds are fast; only changed layers are re-uploaded

### Cleanup Commands

```bash
# Remove unused images (keeps your tagged versions)
docker image prune -a

# Remove unused containers
docker container prune

# Remove build cache (free space)
docker builder prune -a

# Check what's using space
docker system df
```

## MacBook-Specific Tips

### OrbStack vs Docker Desktop

| Feature | OrbStack | Docker Desktop |
|---------|----------|----------------|
| Size | ~200MB | ~4GB+ |
| Startup | <1 second | 10-30 seconds |
| Disk usage | Minimal (ephemeral) | Grows with VM snapshots |
| Rosetta | Optional | Required for x86 images on Apple Silicon |
| Colima required | No | Yes (built-in) |

### Recommended OrbStack Setup

```bash
# Install OrbStack (if not installed)
orbstack install

# Start it
orbstack start

# Pull and run your image
docker pull ghcr.io/WHISPARR_OWNER/whisparr:latest
docker run -d -p 6969:6969 -v $(pwd)/config:/config -v $(pwd)/data:/data ghcr.io/WHISPARR_OWNER/whisparr:latest
```

### Without Colima - No More 40GB!

- **Data lives in bind mounts** (`config/` and `data/` folders), not in the container image
- **Container layers are ephemeral** - `docker system prune` cleans them
- **No nested VM**: Docker Desktop/OrbStack runs containers directly on the Mac kernel
- **Snapshot bloat avoided**: Unlike Colima's lightweight VM with its disk image

## Troubleshooting

### Port 6969 Already in Use

```bash
# Check what's using port 6969
lsof -i :6969
# Or kill any existing process
kill $(lsof -t -i:6969)
```

### Can't Connect to UI

```bash
# Verify container is running
docker ps

# Check logs
docker logs whisparr

# Verify port mapping
docker port whisparr
```

### Disk Space Running Low

```bash
# Aggressive cleanup
docker system prune -a --volumes

# Check what's using space
du -sh ~/.docker/cli-plugins/ ~/.docker/ctx/
```

## GitHub Actions CI/CD Details

The workflow builds multi-arch images (linux/amd64, linux/arm64) and pushes to:

```
ghcr.io/<your-org>/whisparr:latest    # Always latest merged v2-develop
ghcr.io/<your-org>/whisparr:develop    # Latest v2-develop branch
ghcr.io/<your-org>/whisparr:v2-x.x.x  # Versioned releases
```

On every merge to `v2-develop` or `v2`, the image is rebuilt and pushed. Your local container can then `docker pull` the new image.

## Customizing for Your Fork

1. Fork the repo to your GitHub account
2. Update `.github/workflows/docker.yml` to use your repo owner:
   - Change `${{ github.repository_owner }}` to your username/org
3. Or keep it pointing at the upstream repo and pull images from there

## License

GNU GPL v3 - Copyright 2010-2022 Whisparr Team