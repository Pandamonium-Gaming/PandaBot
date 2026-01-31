# Changelog

All notable changes to PandaBot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## \[1.0.0] - 2026-01-31

### Added

* Initial release
* Discord bot with slash commands
* Ashes of Creation integration (items, recipes, vendors, mobs)
* Caching system for API data
* Image caching for fast response times
* Versioning system with startup logging
* Bot version displayed in `/serverinfo` command

### Fixed

* Entity Framework Core migration warnings converted to errors
* Model snapshot now properly reflects `CertificationLevel` property

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
