# PandaBot Development Guide

## Setup

### Initial Setup (One Time)

1. Clone the repository

2. Build the VersionManager tool:

   ```bash
   dotnet build tools/VersionManager/VersionManager.csproj -c Release
   ```

3. Run the setup script for your OS to configure git hooks:

   **Linux/Mac:**

   ```bash
   bash setup-hooks.sh
   ```

   **Windows (PowerShell):**

   ```powershell
   .\setup-hooks.bat
   ```

The pre-commit hook will now run automatically before each commit to validate version synchronization.

## Development Workflow

### Version Management

We use a .NET console tool (`VersionManager`) to synchronize versions between `.csproj` and `CHANGELOG.md`. This ensures they never get out of sync.

#### Quick Version Bump (Using VersionManager)

```bash
# Bump patch version (1.0.4 → 1.0.5)
dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version 1.0.5 --type patch --message "Your change description"

# Bump minor version (1.0.5 → 1.1.0)
dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version 1.1.0 --type minor --message "Your feature description"

# Bump major version (1.1.0 → 2.0.0)
dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version 2.0.0 --type major --message "Your breaking change description"
```

**What it does:**

1. Updates version in `src/PandaBot/PandaBot.csproj`
2. Adds new changelog entry in `CHANGELOG.md` with:
   - Version number and today's date
   - Type category (PATCH/MINOR/MAJOR)
   - Your message
3. Both files stay synchronized automatically

### Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/) for clarity:

```bash
type(scope): description

optional body
```

**Types:**

- `feat:` - New feature
- `fix:` - Bug fix
- `refactor:` - Code refactoring
- `chore:` - Build, CI/CD, deps
- `docs:` - Documentation

### GitHub Actions CI Validation

Every push validates that:

- ✓ Version in `.csproj` matches top entry in `CHANGELOG.md`
- ✓ Project builds successfully
- ✓ Code compiles with no errors

If validation fails, fix the version mismatch and try again.

## Standard Workflow

1. Make your code changes
2. Bump version with VersionManager tool
3. Commit with appropriate Conventional Commit message
4. Push to main
5. GitHub Actions validates and deploys

## Manual Version Synchronization (If Needed)

To manually validate versions without bumping:

```bash
dotnet artifacts/bin/VersionManager/release/VersionManager.dll validate
```

Exit code 0 = versions match, exit code 1 = mismatch.

## Version Numbering (Semantic Versioning)

When bumping versions, follow [Semantic Versioning](https://semver.org/):

- **PATCH** (1.0.X): Bug fixes, minor improvements
- **MINOR** (1.X.0): New features, new commands
- **MAJOR** (X.0.0): Breaking changes, major architectural changes

## Deployment

The GitHub Actions workflow (`.github/workflows/dotnet.yml`) automatically:

1. Validates version synchronization
2. Builds the project
3. Deploys to the server via SSH
4. Updates the systemd service
5. The bot restarts with the new version

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
   dotnet artifacts/bin/VersionManager/release/VersionManager.dll validate
   ```

2. Ensure:
   - `.csproj` version matches changelog top entry
   - Changelog entry is in correct format: `## [X.Y.Z] - YYYY-MM-DD`

3. If you need to bypass (not recommended):

   ```bash
   git commit --no-verify
   ```

## Project Structure

```bash
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

## Resources

- [Discord.Net documentation](https://docs.discord.net/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
