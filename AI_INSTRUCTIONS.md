# AI Agent Instructions for PandaBot

This file provides guidelines for AI agents (like GitHub Copilot) working on the PandaBot codebase.

**⚠️ ENVIRONMENT NOTE:** This project runs on **Windows with PowerShell**. Always use PowerShell cmdlets instead of Unix/Linux commands (no `ls`, `tail`, `rm`, `cat`, `grep`, etc.). See section [PowerShell Commands](#powershell-commands) for equivalents.

## PowerShell Commands

**CRITICAL:** This codebase runs on Windows. Always use PowerShell cmdlets, NOT bash equivalent commands.

| Task | PowerShell | ❌ DON'T USE |
|------|-----------|------------|
| List files | `Get-ChildItem` or `dir` | `ls` |
| View file end | `Get-Content -Tail 20 filename` | `tail` |
| View file | `Get-Content filename` | `cat` |
| Delete file | `Remove-Item filename` | `rm` |
| Find text | `Select-String "pattern" filename` | `grep` |
| Find files | `Get-ChildItem -Filter "*.txt" -Recurse` | `find` |
| Check if exists | `Test-Path filename` | `test -f` |
| JSON pretty-print | `ConvertFrom-Json \| ConvertTo-Json -Depth 10` | `jq` |

### Common PowerShell Operations

```powershell
# List files in directory
Get-ChildItem

# List files recursively (including subfolders)
Get-ChildItem -Recurse

# View last 20 lines of a file
Get-Content logs/bot.log -Tail 20

# View entire file
Get-Content appsettings.json

# Search for text in files
Select-String "error" logs/bot.log

# Search in multiple files
Get-ChildItem -Recurse | Select-String "TODO"

# Delete a file
Remove-Item filename.txt

# Check if file exists
Test-Path filename.txt

# Test API endpoint (returns valid JSON)
(Invoke-WebRequest -Uri "https://api.example.com/endpoint" -TimeoutSec 10).Content | ConvertFrom-Json
```

## Version Management

**CRITICAL: Always use VersionManager tool for version changes**

Never manually edit `PandaBot.csproj` or `CHANGELOG.md` version numbers.

### Version Bump Workflow

1. **Build VersionManager as Release (REQUIRED):**
   ```bash
   dotnet build tools/VersionManager/VersionManager.csproj -c Release
   ```

2. **Bump the version:**
   ```bash
   dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version X.X.X --type patch --message "Brief description of changes"
   ```

3. **Validate the bump:**
   ```bash
   dotnet artifacts/bin/VersionManager/release/VersionManager.dll validate
   ```

4. **Build to verify:**
   ```bash
   dotnet build
   ```

### Version Bump Types

- **patch** (1.2.0 → 1.2.1): Bug fixes, minor improvements, hotfixes
- **minor** (1.2.0 → 1.3.0): New features, enhancements, new modules
- **major** (1.2.0 → 2.0.0): Breaking changes, major refactors

### Example Bumps

```bash
# Fix for a specific issue
dotnet build tools/VersionManager/VersionManager.csproj -c Release
dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version 1.2.2 --type patch --message "Fix ROR module DI issue"

# New feature added
dotnet build tools/VersionManager/VersionManager.csproj -c Release
dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version 1.3.0 --type minor --message "Add Return of Reckoning module"

# Breaking change
dotnet build tools/VersionManager/VersionManager.csproj -c Release
dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version 2.0.0 --type major --message "Refactor DI system"
```

### Important Notes

- **Always build release first:** The VersionManager must be compiled as Release before use
- **Use the built .dll:** Execute from `artifacts/bin/VersionManager/release/VersionManager.dll`, not via `dotnet run`
- **CHANGELOG consistency:** Verify CHANGELOG.md uses consistent formatting (e.g., `### Fixed`, `### Added` with bullet points)
- **Increment PandaBot version:** Always keep version numbers in sync between .csproj and CHANGELOG.md

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
type(scope): description

optional body
optional footer
```

### Message Types

- **feat:** New feature (use with `minor` version bump)
- **fix:** Bug fix (use with `patch` version bump)
- **refactor:** Code restructuring without changing behavior
- **chore:** Build, dependencies, tooling
- **docs:** Documentation updates
- **test:** Test additions or modifications

### Scopes

Use component/module names:
- `ror` - Return of Reckoning module
- `aoc` - Ashes of Creation module
- `di` - Dependency injection
- `logging` - Logging infrastructure
- `db` - Database/migrations
- `config` - Configuration system
- `discord` - Discord.Net integration
- `version` - Version management

### Examples

```
feat(ror): add web scraper for server status

fix(aoc): resolve JsonHelper method calls

refactor(di): improve module loading error handling

chore(deps): update Discord.Net to v3.18.0

docs(contributing): add AI instructions
```

## Build and Verification Workflow

1. **Always build after making changes:**
   ```bash
   dotnet build
   ```

2. **Verify successful build output:**
   - `Build succeeded in X.Xs`
   - No errors or warnings

3. **If build fails:**
   - Read the error message carefully
   - Check file paths and using statements
   - Verify XML/JSON syntax for config files
   - Do NOT proceed until build succeeds

4. **After major changes, rebuild release config:**
   ```bash
   dotnet build -c Release
   ```

## Code Style and Patterns

### Module Pattern (Discord Interaction Modules)

Always use runtime service resolution with `IServiceProvider` pattern for modules:

```csharp
public class MyModule : InteractionModuleBase<SocketInteractionContext>
{
    public IServiceProvider Services { get; set; } = null!;

    [SlashCommand("mycommand", "Description")]
    public async Task MyCommandAsync()
    {
        var logger = Services.GetRequiredService<ILogger<MyModule>>();
        var myService = Services.GetRequiredService<MyService>();
        
        // Use here
    }
}
```

**Why?** Allows modules to load even when their services aren't registered (for disabled modules).

### Service Registration Pattern

Always conditionally register services based on configuration:

```csharp
if (gameModulesConfig.EnableMyModule)
{
    services.AddHttpClient<MyService>();
    services.AddScoped<MyOtherService>();
}
```

This prevents unnecessary resource allocation when modules are disabled.

### Dependency Injection

- **Scoped:** Services tied to request lifetime (database contexts, per-command operations)
- **Singleton:** Application-wide services (Discord client, config, logger)
- **Transient:** New instance each time (rarely used in this project)

## Logging Best Practices

Always use structured logging via `ILogger<T>`:

```csharp
_logger.LogInformation("User {UserId} executed command {Command}", userId, commandName);
_logger.LogError(ex, "Failed to fetch data from {Url}", apiUrl);
_logger.LogWarning("Service {ServiceName} took {Duration}ms", serviceName, duration);
```

### Log Levels

- **Information:** Normal operations (startup phases, command execution, successful API calls)
- **Warning:** Potentially problematic situations (retries, degraded service, timeouts)
- **Error:** Error occurrences (API failures, invalid data, exceptions)
- **Debug/Trace:** Detailed diagnostic info (statement-level tracing)

### Logging at Startup

Critical startup phases MUST log entry/exit points:

```csharp
_logger.LogInformation("Loading Discord modules...");
try
{
    await _interactionService.AddModulesAsync(typeof(Program).Assembly, _services);
    _logger.LogInformation("Modules loaded successfully");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to load modules");
    throw;
}
```

This ensures production logs show exactly where failures occur.

## File Organization

```
src/PandaBot/
├── Models/                 # Data models
│   └── GameModules/       # Game-specific models
├── Services/              # Business logic services
│   └── GameModules/       # Game-specific services
├── Modules/               # Discord interaction modules
│   └── GameModules/       # Game-specific Discord commands
├── Core/                  # Core framework (DB, data)
├── Extensions/            # DI and extension methods
├── Migrations/            # EF Core migrations
└── appsettings*.json      # Configuration files
```

### Adding a New Game Module

1. Create folder structure:
   ```
   Services/YourGame/
   Models/YourGame/
   Modules/YourGame/
   ```

2. Implement `YourGameService` with business logic

3. Implement `YourGameModule` using `IServiceProvider` pattern

4. Add conditional registration in `ServiceCollectionExtensions.cs`

5. Add feature flag in `GameModulesConfig.cs`

6. Add configuration to `appsettings.json`:
   ```json
   "GameModules": {
     "EnableYourGame": true
   }
   ```

## Testing Changes Locally

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the bot locally:**
   ```bash
   dotnet run
   ```

3. **Check startup logs:**
   - Look for "✅ PandaBot vX.X.X is running"
   - Verify module count: "Found N interaction modules"
   - Verify command count: "Registered M commands"

4. **Test Discord commands:**
   - Connect to test Discord server
   - Run `/about` to see bot info and verify changes
   - Test new features manually

## Testing External APIs

**ALWAYS test external API endpoints before integrating them into services.**

### Before Implementation

Use PowerShell to validate the API response:

```powershell
# Test API endpoint returns valid JSON
(Invoke-WebRequest -Uri "https://api.example.com/endpoint" -TimeoutSec 10).Content | ConvertFrom-Json

# Pretty-print the response to understand structure
(Invoke-WebRequest -Uri "https://api.example.com/endpoint" -TimeoutSec 10).Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

### Validation Checklist

- [ ] API endpoint is accessible (no 404, 500 errors)
- [ ] Response is valid JSON (not HTML error pages)
- [ ] Response structure matches service expectations
- [ ] Response includes required fields for parsing
- [ ] Response handles errors gracefully (rate limits, timeouts, offline)
- [ ] Service code parses response correctly
- [ ] Build succeeds: `dotnet build`

### Example: Testing ROR API

```powershell
# Validate ROR API returns player list
(Invoke-WebRequest -Uri "https://api.returnofreckoning.com/stats/online_list_new.php?realm_id=1" -TimeoutSec 10).Content | ConvertFrom-Json | Measure-Object

# Output shows array length (player count)
```

## Common Tasks

### Fix Compilation Error

1. Read the error message: `Program.cs(23,6): error CS1061: ...`
2. Open the file and line number
3. Check for:
   - Missing `using` statements
   - Typos in method/property names
   - XML/JSON syntax errors
   - Wrong types or generic parameters
4. Fix and run `dotnet build` again

### Add a New Command

1. Create method in appropriate module with `[SlashCommand]` attribute
2. Use `IServiceProvider` to get dependencies
3. Add logging for user action
4. Build and test locally
5. Version bump with `--type minor` (new feature)

### Resolve Merge Conflicts

1. Read the conflict markers (`<<<<`, `====`, `>>>>`)
2. Understand both versions
3. Keep the version that aligns with current codebase patterns
4. Test the merged code: `dotnet build`
5. Commit with `fix(scope): resolve merge conflict`

### Deploy to Production

1. Ensure all changes are committed
2. Verify version is bumped
3. Run `dotnet build` one final time
4. Push to main branch
5. GitHub Actions will validate and deploy
6. Check production logs for startup sequence

## Configuration Management

### appsettings.json (Source Control)

Contains non-sensitive defaults:

```json
{
  "Discord": {
    "Prefix": "!",
    "AllowPrefixCommands": false
  },
  "GameModules": {
    "EnableAshesOfCreation": false,
    "EnableStarCitizen": true,
    "EnablePathOfExile": true,
    "EnableReturnOfReckoning": true
  }
}
```

### appsettings.Production.json (NOT in source control)

Contains production overrides:
- Discord bot token
- Production database connection string
- Environment-specific settings

**Never commit sensitive data to source control.**

## Error Handling Pattern

Always wrap critical operations in try-catch with logging:

```csharp
try
{
    _logger.LogInformation("Starting operation {OperationName}...", operationName);
    var result = await PerformOperationAsync();
    _logger.LogInformation("Operation succeeded");
    return result;
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "HTTP error during operation");
    // Handle specific exception type
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error during operation");
    throw;
}
```

## Performance Considerations

### Memory Cache

Only register memory cache when needed:

```csharp
if (gameModulesConfig.EnableAshesOfCreation)
{
    services.AddMemoryCache();
    // Register services that use cache
}
```

### HTTP Client Factory

Use named clients for external APIs:

```csharp
services.AddHttpClient<RORStatusService>();

// In service constructor:
public RORStatusService(HttpClient httpClient) { }
```

## Resources

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Keep a Changelog](https://keepachangelog.com/)
- [Discord.Net Documentation](https://discordnet.dev/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- Local: Read [DEVELOPMENT.md](DEVELOPMENT.md) for setup guide

## Questions or Changes?

If you encounter patterns not covered here or have suggestions for improvements:
1. Document the pattern you used
2. Add it to this file
3. Mention it in commit message
