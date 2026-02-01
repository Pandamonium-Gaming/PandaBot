# PandaBot

[![Build and Deploy](https://github.com/Pandamonium-Gaming/PandaBot/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Pandamonium-Gaming/PandaBot/actions/workflows/dotnet.yml)

A Discord bot for the Ashes of Creation community.

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed development guidelines, including:
- Setup instructions
- Version bumping workflow
- Changelog maintenance
- Pre-commit hook validation
- Adding new commands

**Quick Setup:**
```bash
bash setup-hooks.sh
```

## Configuration

Configuration is managed through a hierarchy of sources, with later sources overriding earlier ones:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific overrides)
3. Environment variables (with `PANDABOT_` prefix)
4. User secrets (development only)

### Method 1: Environment Variables (Recommended for Production)

Set environment variables with the `PANDABOT_` prefix using double underscores (`__`) for nested properties:

#### Discord Configuration

```bash
# Linux/Mac
export PANDABOT_Discord__Token="your_discord_bot_token_here"
export PANDABOT_Discord__Prefix="!"
export PANDABOT_Discord__GuildId=1044558967545806940
export PANDABOT_Discord__AllowPrefixCommands=false

# Windows PowerShell
$env:PANDABOT_Discord__Token="your_discord_bot_token_here"
$env:PANDABOT_Discord__Prefix="!"
$env:PANDABOT_Discord__GuildId=1044558967545806940
$env:PANDABOT_Discord__AllowPrefixCommands=false

# Windows CMD
set PANDABOT_Discord__Token=your_discord_bot_token_here
set PANDABOT_Discord__Prefix=!
set PANDABOT_Discord__GuildId=1044558967545806940
set PANDABOT_Discord__AllowPrefixCommands=false
```

#### Database Configuration

```bash
# Linux/Mac
export PANDABOT_ConnectionStrings__DefaultConnection="Data Source=pandabot.db"

# Windows PowerShell
$env:PANDABOT_ConnectionStrings__DefaultConnection="Data Source=pandabot.db"

# Windows CMD
set PANDABOT_ConnectionStrings__DefaultConnection=Data Source=pandabot.db
```

#### AshesForge Configuration

```bash
# Linux/Mac
export PANDABOT_AshesForge__CacheExpirationHours=24
export PANDABOT_AshesForge__EnableImageCaching=true

# Windows PowerShell
$env:PANDABOT_AshesForge__CacheExpirationHours=24
$env:PANDABOT_AshesForge__EnableImageCaching=true

# Windows CMD
set PANDABOT_AshesForge__CacheExpirationHours=24
set PANDABOT_AshesForge__EnableImageCaching=true
```

### Method 2: User Secrets (Development)

For development, use .NET user secrets to securely store sensitive configuration:

```bash
dotnet user-secrets set "Discord:Token" "your_token_here"
dotnet user-secrets set "Discord:Prefix" "!"
```

User secrets are stored securely and not committed to version control.

### Method 3: appsettings.json (Development)

Edit `appsettings.json` directly for development defaults. **Never commit sensitive tokens to this file**.

### Configuration Hierarchy Example

When the bot starts, it loads configuration in this order:

1. `appsettings.json` loads default settings
2. `appsettings.Development.json` (if running in Development environment) overrides defaults
3. Environment variables with `PANDABOT_` prefix override everything above
4. User secrets (development only) override everything

Example: If you set both `appsettings.json` and an environment variable, the environment variable wins:

```json
// appsettings.json
{
  "Discord": {
    "Prefix": "!"
  }
}
```

```bash
# Environment variable overrides the ! prefix
export PANDABOT_Discord__Prefix=">"
```

Result: The bot will use `>` as the prefix, not `!`.

## Running the Bot

```bash
# Development mode
dotnet run

# Production mode (set environment first)
# Linux/Mac
export DOTNET_ENVIRONMENT=Production
dotnet run

# Windows PowerShell
$env:DOTNET_ENVIRONMENT="Production"
dotnet run
```

## Available Configuration Options

### Discord Section

* `Token` (string, required): Discord bot token
* `Prefix` (string): Command prefix for text commands (default: `!`)
* `GuildId` (ulong): Guild/Server ID for testing (optional)
* `AllowPrefixCommands` (bool): Enable legacy prefix commands (default: `false`)
* `AllowedFunChannels` (array): Channel IDs where fun commands are allowed

### ConnectionStrings Section

* `DefaultConnection` (string): SQLite database connection string (default: `Data Source=pandabot.db`)

### AshesForge Section

* `CacheExpirationHours` (int): How long to cache AshesForge data (default: `24`)
* `EnableImageCaching` (bool): Enable image caching for faster responses (default: `true`)
