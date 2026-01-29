# PandaBot

A Discord bot for the Ashes of Creation community.

## Configuration

### Method 1: User Secrets (Development)

```bash
dotnet user-secrets set "Discord:Token" "your_token_here"
```

### Method 2: Environment Variables (Production)

Set environment variables with the `PANDABOT_` prefix:

```bash
# Linux/Mac
export PANDABOT_Discord__Token="your_token_here"

# Windows PowerShell
$env:PANDABOT_Discord__Token="your_token_here"

# Windows CMD
set PANDABOT_Discord__Token=your_token_here
```

### Method 3: appsettings.json (Not Recommended for Secrets)

Edit `appsettings.json` directly (not recommended for production).

### Configuration Hierarchy

Configuration sources are loaded in this order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables (with `PANDABOT_` prefix)
4. User secrets (development only)

## Running the Bot

```bash
dotnet run
```
