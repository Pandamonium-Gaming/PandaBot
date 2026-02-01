# PandaBot Development Guide

## Setup

### Initial Setup (One Time)

1. Clone the repository

2. Run the setup script for your OS:

   **Linux/Mac:**

   ```bash
   bash setup-hooks.sh
   ```

   **Windows (PowerShell):**

   ```powershell
   .\setup-hooks.bat
   ```

3. Configure git to use the hooks:
   ```bash
   git config core.hooksPath .githooks
   ```

The pre-commit hook will now run automatically before each commit to validate version synchronization.

## Development Workflow

### Quick Version Bump (Automated)

We provide a PowerShell script that automates version bumping and changelog generation:

```powershell
# Bump patch version (1.0.4 → 1.0.5)
.\Bump-Version.ps1 -Type patch

# Bump minor version (1.0.5 → 1.1.0)
.\Bump-Version.ps1 -Type minor

# Bump major version (1.1.0 → 2.0.0)
.\Bump-Version.ps1 -Type major

# Run without auto-committing
.\Bump-Version.ps1 -Type patch -NoCommit
```

**What it does:**
1. Updates version in `src/PandaBot/PandaBot.csproj`
2. Parses git commits using [Conventional Commits](https://www.conventionalcommits.org/)
3. Auto-generates `CHANGELOG.md` with:
   - **BREAKING CHANGES** (from `BREAKING CHANGE:` in commit body)
   - **Added** features (commits starting with `feat:`)
   - **Fixed** bugs (commits starting with `fix:`)
   - **Changed** items (commits starting with `refactor:`)
4. Commits both files with message: `chore: bump version to X.Y.Z`

## Commit Message Format

To get automatic changelog generation, follow [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): description

optional body

optional footer
```

**Types:**
- `feat:` - New feature (shows in "Added")
- `fix:` - Bug fix (shows in "Fixed")
- `refactor:` - Code refactoring (shows in "Changed")
- `chore:` - Build, CI/CD, deps (not in changelog)
- `docs:` - Documentation (not in changelog)

**Examples:**
```
feat(discord): add Star Citizen status command
fix: resolve database migration timeout issue
refactor(core): simplify service initialization
BREAKING CHANGE: removed deprecated API endpoint
```

## GitHub Actions CI Validation

Every push runs a CI check that validates:
- ✓ Version in `.csproj` matches top entry in `CHANGELOG.md`
- ✓ Project builds successfully
- ✓ Code compiles with no errors

If validation fails, the build will be rejected and you'll need to fix the version mismatch.

### Manual Version Workflow (Optional)

If you prefer to manage versions manually:

### Manual Version Workflow (Optional)

Every code change **MUST** follow these steps before committing:

### 1. Make Your Code Changes

Implement your feature or fix in the codebase.

### 2. Determine Version Bump Type

Follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html):

* **PATCH** (1.0.X): Bug fixes, minor improvements, dependency updates
* **MINOR** (1.X.0): New features, new commands, significant improvements
* **MAJOR** (X.0.0): Breaking changes, major architectural changes

### 3. Update Version in `.csproj`

Edit `src/PandaBot/PandaBot.csproj` and update the version:

```xml
<Version>X.Y.Z</Version>
```

### 4. Add Changelog Entry

Edit `CHANGELOG.md` and add an entry at the top under a new `## [X.Y.Z] - YYYY-MM-DD` section:

```markdown
## [1.0.5] - 2026-02-01

### Added

* New feature description

### Fixed

* Bug fix description

### Changed

* Breaking change description
```

Use these sections as appropriate:

* **Added** - New features or functionality
* **Changed** - Changes to existing features
* **Fixed** - Bug fixes
* **Removed** - Removed features
* **Deprecated** - Deprecated features

### 5. Build and Test

```bash
dotnet build -c Release
# Test your changes locally if needed
```

### 6. Commit

```bash
git add -A
git commit -m "chore: bump version to X.Y.Z"
# or
git commit -m "feat: add new feature (v1.0.5)"
```

**The pre-commit hook will validate that:**

* Version in `.csproj` matches the top entry in `CHANGELOG.md`
* The changelog entry exists and is formatted correctly
* If validation fails, the commit will be rejected

### 7. Push

```bash
git push
```

This triggers GitHub Actions to build and deploy automatically.

## Deployment

The GitHub Actions workflow (`.github/workflows/dotnet.yml`) automatically:

1. Builds the project
2. Deploys to the server via SSH
3. Updates the systemd service
4. The bot restarts with the new version

You can verify the new version is running with:

```bash
curl http://bothost/bot/version
# or check logs:
ssh pandabot@bothost "tail -f /opt/pandabot/logs/pandabot-*.log"
```

## Common Tasks

### Viewing Changelog

```bash
head -50 CHANGELOG.md
```

### Checking Current Version

```bash
grep '<Version>' src/PandaBot/PandaBot.csproj
```

### Troubleshooting Pre-commit Hook

If the pre-commit hook is preventing your commit:

1. Check what validation failed:
   ```bash
   .githooks/pre-commit
   ```

2. Ensure:
   * `.csproj` version matches changelog top entry
   * Changelog entry is in correct format: `## [X.Y.Z] - YYYY-MM-DD`
   * You've added content under the version heading

3. If you need to bypass (not recommended):
   ```bash
   git commit --no-verify
   ```

## Project Structure

```
src/PandaBot/
├── Program.cs                 # Entry point, version logging
├── PandaBot.csproj           # Version specification (source of truth)
├── Modules/                  # Command modules (Ashes, StarCitizen, Core, Moderation)
├── Services/                 # Business logic services
├── Core/
│   ├── Data/                # Database context and models
│   └── Models/              # Core data models
└── Extensions/              # DI configuration
```

## Adding New Commands

1. Create a new module under `src/PandaBot/Modules/{GameName}/`
2. Inherit from `InteractionModuleBase<SocketInteractionContext>`
3. Decorate class with `[Group("command-name", "description")]`
4. Add slash command methods decorated with `[SlashCommand(...)]`
5. Register services in `ServiceCollectionExtensions.cs` if needed
6. Increment PATCH version and update CHANGELOG.md
7. After deployment, sync commands: `/admin sync-commands`

## Questions?

Refer to:

* `CHANGELOG.md` for version history and examples
* Discord.Net documentation: https://docs.discord.net/
* Entity Framework Core: https://docs.microsoft.com/en-us/ef/core/
