# GitHub Copilot Instructions for PandaBot

Essential guidelines for AI code generation on this project. See [AI\_INSTRUCTIONS.md](../AI_INSTRUCTIONS.md) for comprehensive documentation.

## Command Formatting

When providing terminal commands to users, always wrap commands in fenced code blocks.

**⚠️ ENVIRONMENT NOTE:** This project runs on **Windows with PowerShell**. Always use PowerShell cmdlets instead of Unix/Linux commands (e.g., use `Get-ChildItem` instead of `ls`, `Get-Content -Tail` instead of `tail`, `Remove-Item` instead of `rm`). Refer to [AI\_INSTRUCTIONS.md](../AI_INSTRUCTIONS.md#powershell-commands) for command equivalents.

## CRITICAL: Version Management

Never manually edit `src/PandaBot/PandaBot.csproj` version or `CHANGELOG.md` version entries.

Use VersionManager when changes affect shipped bot runtime behavior (for example, code under `src/PandaBot/`, runtime configuration behavior, commands, services, or user-visible functionality).

Do not require a version bump for workflow/CI-only, deployment-only, docs-only, tests-only, or tooling-only changes that do not change bot runtime behavior.

When a version bump is required, use the VersionManager tool. Build it before use:

```bash
# Step 1: Build VersionManager as Release
dotnet build tools/VersionManager/VersionManager.csproj -c Release

# Step 2 (OPTIONAL - Recommended): Check if version bump is needed based on git commits
dotnet artifacts/bin/VersionManager/release/VersionManager.dll check-commits
# This analyzes commits since the last version tag and warns if version needs updating

# Step 3: Use the built executable to bump version
dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version X.X.X --type patch --message "Your description"

# Step 4: Validate the changes
dotnet artifacts/bin/VersionManager/release/VersionManager.dll validate

# Step 5: Build main project to verify compile
dotnet build
```

Git-aware version tracking: The VersionManager analyzes git commits since the last version tag and warns if your version bump does not match conventional commits. Use `check-commits` before bumping to ensure accuracy:

* **BREAKING changes** → major version bump required
* **feat commits** → minor version bump required
* **fix commits** → patch version bump required

Build validation enforces version consistency. If `PandaBot.csproj` and `CHANGELOG.md` versions differ, build fails.

Important:

* Increment PandaBot version when runtime behavior changes are shipped.
* Use `check-commits` to ensure version bump aligns with actual commits.
* Verify CHANGELOG formatting is consistent (use `### Fixed`, `### Added`, etc. with bullet points, not dashes).
* Never commit code that breaks the build due to version mismatch.

## Commit Message Format

Use Conventional Commits:

```text
type(scope): description
```

Types: `feat`, `fix`, `refactor`, `chore`, `docs`

Example: `feat(ror): add web scraper for server status`

## Build Verification

After any code changes:

```bash
dotnet build
```

Ensure output shows: `Build succeeded in X.Xs`

## Module Pattern

Use runtime service resolution for Discord modules:

```csharp
public class MyModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;

    [SlashCommand("cmd", "Description")]
    public async Task CommandAsync()
    {
        var logger = Services.GetRequiredService<ILogger<MyModule>>();
        var service = Services.GetRequiredService<MyService>();
    }
}
```

## Service Registration

Always conditionally register based on config:

```csharp
if (gameModulesConfig.EnableMyModule)
{
    services.AddHttpClient<MyService>();
}
```

## Logging

Use structured logging everywhere:

```csharp
_logger.LogInformation("Operation started for {UserId}", userId);
_logger.LogError(ex, "Failed to fetch from {Url}", apiUrl);
```

Critical startup phases MUST log entry/exit:

```csharp
_logger.LogInformation("Loading modules...");
try
{
    await _interactionService.AddModulesAsync(Assembly, _services);
    _logger.LogInformation("Modules loaded successfully");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to load modules");
    throw;
}
```

## File Organization

```text
src/PandaBot/
├── Models/YourGame/          # Data models
├── Services/YourGame/        # Business logic
├── Modules/YourGame/         # Discord commands
├── Extensions/               # DI setup
└── appsettings*.json         # Config
```

## Quick Checklist Before Commit

* \[ ] Code compiles: `dotnet build`
* \[ ] Version bumped with VersionManager tool (required only for runtime behavior changes)
* \[ ] CHANGELOG.md updated (by VersionManager, required only for runtime behavior changes)
* \[ ] Use Conventional Commits message
* \[ ] No manual version number edits
* \[ ] Meaningful log messages added
* \[ ] Tested locally if applicable

## For More Details

See [AI\_INSTRUCTIONS.md](../AI_INSTRUCTIONS.md) for:

* Complete development workflow
* Error handling patterns
* Performance considerations
* Configuration management
* Troubleshooting guides
