# PandaBot

[![.NET CI](https://github.com/Pandamonium-Gaming/PandaBot/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Pandamonium-Gaming/PandaBot/actions/workflows/dotnet.yml)

A feature-rich Discord bot for gaming communities. Provides information about server statuses, game recipes, moderation tools, and community features.

**Current Version:** 1.7.0 | **Framework:** .NET 10.0 | **Discord.Net:** v3.18.0

## Features

### Game Status Modules (Configurable)

* **Ashes of Creation** - Craft recipes, items, profession levels, and item search
* **Star Citizen** - Server status and component health (🚀 enabled)
* **Path of Exile** - Server status and component health (⚔️ enabled)
* **Return of Reckoning** - Server status and player counts (enabled)

Each module can be independently enabled/disabled via configuration.

### Core Commands

* `/about` - Bot information (version, modules, commands, uptime)
* `/help` - Command reference
* `/ping` - Latency check
* `/serverinfo` - Server information
* `/userinfo` - User profile information

### Moderation Commands

* `/warn` - Issue warnings
* `/warnings` - Check user warnings
* `/ban` - Ban users
* `/kick` - Kick users
* `/mute` / `/unmute` - Mute/unmute users
* `/clear` - Clear messages
* `/purgeuser` - Remove all messages from a user
* `/lock` / `/unlock` - Lock/unlock channels
* `/slowmode` - Enable channel slowmode
* `/singlemessage enable/disable/reset-user/list` - Manage single-message-per-user channel enforcement (requires Manage Channels)

### Moderation Action Logging

All moderation actions are logged as rich embeds to a Discord forum channel. Each embed shows the action type, the target user, the moderator, and the reason. Configure via the `ModerationLog` root-level config section.

### Cross-Channel Spam Detection

Detects users who send identical messages across multiple channels within a configurable time window. When triggered, a spam alert is posted to the moderation log forum channel with **Ban** and **Dismiss** buttons for moderators. Configure via the `CrossChannelSpam` root-level config section.

> **Note:** Both features require the **Message Content** privileged intent to be enabled in the [Discord Developer Portal](https://discord.com/developers/applications). Restart the bot after enabling it — no token refresh is required.

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed development guidelines, including:

* Setup instructions
* Version bumping workflow
* Changelog maintenance
* Pre-commit hook validation
* Adding new commands

### AI Development Guidelines

**For AI Agents (GitHub Copilot, etc.):**

* **Quick Reference:** [.github/copilot-instructions.md](.github/copilot-instructions.md) - Essential guidelines (auto-loaded by GitHub Copilot)
* **Comprehensive Guide:** [AI\_INSTRUCTIONS.md](AI_INSTRUCTIONS.md) - Complete development patterns, error handling, and workflows

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

#### Uptime Heartbeat Configuration

```bash
# Linux/Mac
export PANDABOT_Discord__Heartbeat__Enabled=true
export PANDABOT_Discord__Heartbeat__PushUrl="https://kuma.example.com/api/push/xxxxx"
export PANDABOT_Discord__Heartbeat__IntervalSeconds=60
export PANDABOT_Discord__Heartbeat__StartupDelaySeconds=15
export PANDABOT_Discord__Heartbeat__TimeoutSeconds=10

# Windows PowerShell
$env:PANDABOT_Discord__Heartbeat__Enabled="true"
$env:PANDABOT_Discord__Heartbeat__PushUrl="https://kuma.example.com/api/push/xxxxx"
$env:PANDABOT_Discord__Heartbeat__IntervalSeconds="60"
$env:PANDABOT_Discord__Heartbeat__StartupDelaySeconds="15"
$env:PANDABOT_Discord__Heartbeat__TimeoutSeconds="10"

# Windows CMD
set PANDABOT_Discord__Heartbeat__Enabled=true
set PANDABOT_Discord__Heartbeat__PushUrl=https://kuma.example.com/api/push/xxxxx
set PANDABOT_Discord__Heartbeat__IntervalSeconds=60
set PANDABOT_Discord__Heartbeat__StartupDelaySeconds=15
set PANDABOT_Discord__Heartbeat__TimeoutSeconds=10
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

#### Moderation Log Configuration

```bash
# Linux/Mac
export PANDABOT_ModerationLog__ForumChannelId=1234567890
export PANDABOT_ModerationLog__ModeratorRoleId=1234567890

# Windows PowerShell
$env:PANDABOT_ModerationLog__ForumChannelId="1234567890"
$env:PANDABOT_ModerationLog__ModeratorRoleId="1234567890"

# Windows CMD
set PANDABOT_ModerationLog__ForumChannelId=1234567890
set PANDABOT_ModerationLog__ModeratorRoleId=1234567890
```

#### Cross-Channel Spam Configuration

```bash
# Linux/Mac
export PANDABOT_CrossChannelSpam__Enabled=false
export PANDABOT_CrossChannelSpam__TimeWindowSeconds=30
export PANDABOT_CrossChannelSpam__MinimumChannelCount=3
export PANDABOT_CrossChannelSpam__DeleteMessages=true
export PANDABOT_CrossChannelSpam__TimeoutOnDetection=true

# Windows PowerShell
$env:PANDABOT_CrossChannelSpam__Enabled="false"
$env:PANDABOT_CrossChannelSpam__TimeWindowSeconds="30"
$env:PANDABOT_CrossChannelSpam__MinimumChannelCount="3"
$env:PANDABOT_CrossChannelSpam__DeleteMessages="true"
$env:PANDABOT_CrossChannelSpam__TimeoutOnDetection="true"

# Windows CMD
set PANDABOT_CrossChannelSpam__Enabled=false
set PANDABOT_CrossChannelSpam__TimeWindowSeconds=30
set PANDABOT_CrossChannelSpam__MinimumChannelCount=3
set PANDABOT_CrossChannelSpam__DeleteMessages=true
set PANDABOT_CrossChannelSpam__TimeoutOnDetection=true
```

#### Moderation Exemptions Configuration

```bash
# Linux/Mac
export PANDABOT_ModerationExemptions__ExemptUserIds__0=1234567890
export PANDABOT_ModerationExemptions__ExemptRoleIds__0=1234567890

# Windows PowerShell
$env:PANDABOT_ModerationExemptions__ExemptUserIds__0="1234567890"
$env:PANDABOT_ModerationExemptions__ExemptRoleIds__0="1234567890"

# Windows CMD
set PANDABOT_ModerationExemptions__ExemptUserIds__0=1234567890
set PANDABOT_ModerationExemptions__ExemptRoleIds__0=1234567890
```

#### Command Access Configuration

```bash
# Disable all fun commands (meme, 8ball, roll, joke, say)
PANDABOT_CommandAccess__DisableAllFunCommands=true

# Disable specific commands globally
PANDABOT_CommandAccess__DisabledCommands__0=about
PANDABOT_CommandAccess__DisabledCommands__1=avatar
PANDABOT_CommandAccess__DisabledCommands__2=help
PANDABOT_CommandAccess__DisabledCommands__3=ping
PANDABOT_CommandAccess__DisabledCommands__4=remind
PANDABOT_CommandAccess__DisabledCommands__5=serverinfo
PANDABOT_CommandAccess__DisabledCommands__6=userinfo

# Restrict commands to allowed channel IDs
PANDABOT_CommandAccess__RestrictedChannels__about__0=1234567890
PANDABOT_CommandAccess__RestrictedChannels__help__0=1234567890
PANDABOT_CommandAccess__RestrictedChannels__help__1=2345678901
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
* `Heartbeat.Enabled` (bool): Enable Uptime Kuma push heartbeats (default: `false`)
* `Heartbeat.PushUrl` (string): Uptime Kuma push URL
* `Heartbeat.IntervalSeconds` (int): Heartbeat interval (minimum enforced: `15`)
* `Heartbeat.StartupDelaySeconds` (int): Delay before first heartbeat push
* `Heartbeat.TimeoutSeconds` (int): HTTP timeout for heartbeat push

### GitHub Secrets Mapping (If Using GitHub Actions Deploy)

This repository uses `.github/workflows/dotnet.yml` for CI and `.github/workflows/deploy.yml` for deployment. The deploy workflow writes a runtime `.env` using these env vars:

| GitHub Secret | Runtime Environment Variable |
| --- | --- |
| `DISCORD_TOKEN` | `PANDABOT_Discord__Token` |
| `GUILD_ID` | `PANDABOT_Discord__GuildId` |
| `HEARTBEAT_ENABLED` | `PANDABOT_Discord__Heartbeat__Enabled` |
| `HEARTBEAT_PUSH_URL` | `PANDABOT_Discord__Heartbeat__PushUrl` |
| `HEARTBEAT_INTERVAL_SECONDS` | `PANDABOT_Discord__Heartbeat__IntervalSeconds` |
| `HEARTBEAT_STARTUP_DELAY_SECONDS` | `PANDABOT_Discord__Heartbeat__StartupDelaySeconds` |
| `HEARTBEAT_TIMEOUT_SECONDS` | `PANDABOT_Discord__Heartbeat__TimeoutSeconds` |
| `MODERATION_LOG_FORUM_CHANNEL_ID` | `PANDABOT_ModerationLog__ForumChannelId` |
| `MODERATION_LOG_MODERATOR_ROLE_ID` | `PANDABOT_ModerationLog__ModeratorRoleId` |
| `MODERATION_LOG_EVENT_AUDIT_ENABLED` | `PANDABOT_ModerationLog__EventAuditEnabled` |
| `MODERATION_LOG_EVENT_AUDIT_CHANNEL_ID` | `PANDABOT_ModerationLog__EventAuditChannelId` |
| `MODERATION_LOG_EVENT_AUDIT_LOG_MESSAGE_DELETES` | `PANDABOT_ModerationLog__LogMessageDeletes` |
| `MODERATION_LOG_EVENT_AUDIT_LOG_MEMBER_LEAVES` | `PANDABOT_ModerationLog__LogMemberLeaves` |
| `MODERATION_LOG_AUDIT_LOG_LOOKBACK_SECONDS` | `PANDABOT_ModerationLog__AuditLogLookbackSeconds` |
| `CROSS_CHANNEL_SPAM_ENABLED` | `PANDABOT_CrossChannelSpam__Enabled` |
| `CROSS_CHANNEL_SPAM_TIME_WINDOW_SECONDS` | `PANDABOT_CrossChannelSpam__TimeWindowSeconds` |
| `CROSS_CHANNEL_SPAM_MINIMUM_CHANNEL_COUNT` | `PANDABOT_CrossChannelSpam__MinimumChannelCount` |
| `CROSS_CHANNEL_SPAM_DELETE_MESSAGES` | `PANDABOT_CrossChannelSpam__DeleteMessages` |
| `CROSS_CHANNEL_SPAM_TIMEOUT_ON_DETECTION` | `PANDABOT_CrossChannelSpam__TimeoutOnDetection` |
| `MODERATION_EXEMPT_USER_ID_0` | `PANDABOT_ModerationExemptions__ExemptUserIds__0` |
| `MODERATION_EXEMPT_ROLE_ID_0` | `PANDABOT_ModerationExemptions__ExemptRoleIds__0` |
| `COMMAND_ACCESS_DISABLE_ALL_FUN_COMMANDS` | `PANDABOT_CommandAccess__DisableAllFunCommands` |
| `COMMAND_ACCESS_DISABLED_COMMAND_0` | `PANDABOT_CommandAccess__DisabledCommands__0` |
| `COMMAND_ACCESS_DISABLED_COMMAND_1` | `PANDABOT_CommandAccess__DisabledCommands__1` |
| `COMMAND_ACCESS_DISABLED_COMMAND_2` | `PANDABOT_CommandAccess__DisabledCommands__2` |
| `COMMAND_ACCESS_DISABLED_COMMAND_3` | `PANDABOT_CommandAccess__DisabledCommands__3` |
| `COMMAND_ACCESS_DISABLED_COMMAND_4` | `PANDABOT_CommandAccess__DisabledCommands__4` |
| `COMMAND_ACCESS_DISABLED_COMMAND_5` | `PANDABOT_CommandAccess__DisabledCommands__5` |
| `COMMAND_ACCESS_DISABLED_COMMAND_6` | `PANDABOT_CommandAccess__DisabledCommands__6` |

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

### ModerationLog Section

All moderation actions are logged as rich embeds to a Discord forum channel. Each embed shows the action type, the target user, the moderator, and the reason.

> **Note:** `ModerationLog` is a root-level config section, not nested under `Discord`.

* `ForumChannelId` (ulong): Forum channel ID where moderation log threads are created (`0` = disabled)
* `ModeratorRoleId` (ulong): Optional role to mention in log posts (`0` = no mention)

### CrossChannelSpam Section

Detects users who send identical messages across multiple channels within a short time window. When triggered, a spam alert is posted to the moderation log forum channel with **Ban** and **Dismiss** buttons for moderators.

> **Note:** `CrossChannelSpam` is a root-level config section, not nested under `Discord`. Requires the **Message Content** privileged intent enabled in the Discord Developer Portal.

* `Enabled` (bool): Enable cross-channel spam detection (default: `false`)
* `TimeWindowSeconds` (int): Sliding window duration in seconds (default: `30`)
* `MinimumChannelCount` (int): Minimum number of distinct channels before a detection fires (default: `3`)
* `DeleteMessages` (bool): Delete detected spam messages — requires Manage Messages (default: `true`)
* `TimeoutOnDetection` (bool): Apply a 28-day timeout to the spammer — requires Moderate Members (default: `true`)

### SingleMessage Section

Register channels that should allow only one message per user. Channels must be listed here before `/singlemessage enable` will accept them.

```json
{
  "SingleMessage": {
    "Channels": [
      { "ChannelId": 1234567890123456789, "ScanHistoryOnEnable": false }
    ]
  }
}
```

* `Channels[].ChannelId` (ulong): Discord channel ID to register for single-message enforcement
* `Channels[].ScanHistoryOnEnable` (bool): When `true`, scans the last 100 messages on enable to pre-populate existing posters (default: `false`)

`SingleMessage:Channels` is an array and is best managed in `appsettings.json` rather than environment variables.
