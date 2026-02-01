# Changelog



## [1.1.1] - 2026-02-01

### PATCH

- Fix Path of Exile API endpoint parsing
## [1.1.0] - 2026-02-01

### MINOR

- Add Path of Exile status command
All notable changes to PandaBot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## \[1.0.4] - 2026-02-01

### Fixed

* Star Citizen status API endpoint
  * Changed from blocked `/api/v2/components.json` to publicly accessible `/index.json`
  * Simplified status display showing overall status and per-system status

## \[1.0.3] - 2026-02-01

### Added

* Star Citizen server status command (`/starcitizen status`)
  * Fetches real-time status from RSI status API
  * Groups components by category (Game Servers, Website, etc.)
  * Color-coded status indicators with emoji (✅ Operational, ⚠️ Degraded, 🔴 Partial Outage, ❌ Major Outage)

### Changed

* Database migration system cleaned and consolidated
  * Removed all incremental migrations (7 migration files)
  * Created single `InitialCreate` migration from current model
  * Ensures clean database schema with all properties in sync

### Fixed

* Entity Framework Core model/snapshot mismatch resolved
  * Removed snapshot file and let EF Core regenerate
  * Consolidated migrations to prevent future sync issues
  * Service now runs as correct user (`pandabot` instead of `deployment`)

## \[1.0.2] - 2026-01-31

### Added

* Service file deployment in GitHub Actions
* Passwordless sudo configuration for deployment commands
* Improved .env file handling in deployment

## \[1.0.1] - 2026-01-31

### Added

* Version bump system
* Version displayed in bot startup logs
* Bot version shown in `/serverinfo` command

## \[1.0.0] - 2026-01-31

### Added

* Initial release
* Discord bot with slash commands
* Ashes of Creation integration (items, recipes, vendors, mobs)
* Caching system for API data
* Image caching for fast response times
* Versioning system with startup logging

### Fixed

* Entity Framework Core migration warnings converted to errors
* Model snapshot properly reflects all properties

## Version Bumping Guidelines

**IMPORTANT: Every code change must increment the version in [`PandaBot.csproj`](src/PandaBot/PandaBot.csproj)**

### Semantic Versioning (MAJOR.MINOR.PATCH)

* **PATCH** (1.0.X): Bug fixes, minor improvements, dependency updates
* **MINOR** (1.X.0): New features, new commands, significant improvements
* **MAJOR** (X.0.0): Breaking changes, major architectural changes

### Steps for Version Bumping

1. Make your code changes
2. Update `<Version>X.Y.Z</Version>` in [`PandaBot.csproj`](src/PandaBot/PandaBot.csproj)
3. Add an entry to this CHANGELOG.md under the new version
4. Commit with message: `chore: bump version to X.Y.Z`
5. The GitHub Actions workflow will build and deploy with the new version

### Example

```xml
<!-- Before -->
<Version>1.0.0</Version>

<!-- After (for bug fix) -->
<Version>1.0.1</Version>
```

**The version in the `.csproj` file is the source of truth. Always keep it in sync with the CHANGELOG.**
