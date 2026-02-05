# PandaBot

[![Build and Deploy](https://github.com/Pandamonium-Gaming/PandaBot/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Pandamonium-Gaming/PandaBot/actions/workflows/dotnet.yml)

A feature-rich Discord bot for gaming communities. Provides information about server statuses, game recipes, moderation tools, and community features.

**Current Version:** 1.2.1 | **Framework:** .NET 9.0 | **Discord.Net:** v3.18.0

## Features

### Game Status Modules (Configurable)

- **Ashes of Creation** - Craft recipes, items, profession levels, and item search
- **Star Citizen** - Server status and component health (🚀 enabled)
- **Path of Exile** - Server status and component health (⚔️ enabled)
- **Return of Reckoning** - Server status and player counts (enabled)

Each module can be independently enabled/disabled via configuration.

### Core Commands

- `/about` - Bot information (version, modules, commands, uptime)
- `/help` - Command reference
- `/ping` - Latency check
- `/serverinfo` - Server information
- `/userinfo` - User profile information

### Moderation Commands

- `/warn` - Issue warnings
- `/warnings` - Check user warnings
- `/ban` - Ban users
- `/kick` - Kick users
- `/mute` / `/unmute` - Mute/unmute users
- `/clear` - Clear messages
- `/purgeuser` - Remove all messages from a user
- `/lock` / `/unlock` - Lock/unlock channels
- `/slowmode` - Enable channel slowmode

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed development guidelines, including:

* Setup instructions
* Version bumping workflow
* Changelog maintenance
* Pre-commit hook validation
* Adding new commands

### AI Development Guidelines

**For AI Agents (GitHub Copilot, etc.):**

- **Quick Reference:** [.github/copilot-instructions.md](.github/copilot-instructions.md) - Essential guidelines (auto-loaded by GitHub Copilot)
- **Comprehensive Guide:** [AI_INSTRUCTIONS.md](AI_INSTRUCTIONS.md) - Complete development patterns, error handling, and workflows

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

#### Game Modules Configuration

```bash
# Linux/Mac
export PANDABOT_GameModules__EnableAshesOfCreation=false
export PANDABOT_GameModules__EnableStarCitizen=true
export PANDABOT_GameModules__EnablePathOfExile=true
export PANDABOT_GameModules__EnableReturnOfReckoning=true

# Windows PowerShell
$env:PANDABOT_GameModules__EnableAshesOfCreation="false"
$env:PANDABOT_GameModules__EnableStarCitizen="true"
$env:PANDABOT_GameModules__EnablePathOfExile="true"
$env:PANDABOT_GameModules__EnableReturnOfReckoning="true"

# Windows CMD
set PANDABOT_GameModules__EnableAshesOfCreation=false
set PANDABOT_GameModules__EnableStarCitizen=true
set PANDABOT_GameModules__EnablePathOfExile=true
set PANDABOT_GameModules__EnableReturnOfReckoning=true
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

### GameModules Section

Controls which game modules are loaded and available:

* `EnableAshesOfCreation` (bool): Enable Ashes of Creation module (default: `false`)
* `EnableStarCitizen` (bool): Enable Star Citizen status module (default: `true`)
* `EnablePathOfExile` (bool): Enable Path of Exile status module (default: `true`)
* `EnableReturnOfReckoning` (bool): Enable Return of Reckoning status module (default: `true`)

**Note:** Modules that are disabled will not load services or consume resources. Slash commands from disabled modules will not be available to users.
