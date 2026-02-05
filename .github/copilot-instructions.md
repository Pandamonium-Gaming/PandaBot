# GitHub Copilot Instructions for PandaBot

Essential guidelines for AI code generation on this project. See [AI_INSTRUCTIONS.md](../AI_INSTRUCTIONS.md) for comprehensive documentation.

## 🔴 CRITICAL: Version Management

**NEVER manually edit `src/PandaBot/PandaBot.csproj` or `CHANGELOG.md` versions.**

**ALWAYS** use VersionManager tool:

```bash
dotnet run --project tools/VersionManager/VersionManager.csproj -- bump --version X.X.X --type patch --message "Your description"
```

Then validate:
```bash
dotnet run --project tools/VersionManager/VersionManager.csproj -- validate
```

## Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): description
```

**Types:** `feat` (new feature), `fix` (bug fix), `refactor`, `chore`, `docs`

**Example:** `feat(ror): add web scraper for server status`

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

```
src/PandaBot/
├── Models/YourGame/          # Data models
├── Services/YourGame/        # Business logic
├── Modules/YourGame/         # Discord commands
├── Extensions/               # DI setup
└── appsettings*.json         # Config
```

## Quick Checklist Before Commit

- [ ] Code compiles: `dotnet build`
- [ ] Version bumped with VersionManager tool
- [ ] CHANGELOG.md updated (by VersionManager)
- [ ] Use Conventional Commits message
- [ ] No manual version number edits
- [ ] Meaningful log messages added
- [ ] Tested locally if applicable

## For More Details

See [AI_INSTRUCTIONS.md](../AI_INSTRUCTIONS.md) for:
- Complete development workflow
- Error handling patterns
- Performance considerations
- Configuration management
- Troubleshooting guides
