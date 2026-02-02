using System.Text.RegularExpressions;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

string command = args[0].ToLower();
return command switch
{
    "validate" => ValidateVersions(args.Skip(1).ToArray()),
    "bump" => BumpVersion(args.Skip(1).ToArray()),
    "--help" or "-h" or "help" => PrintUsage(),
    _ => UnsupportedCommand(command)
};

int ValidateVersions(string[] args)
{
    try
    {
        string csprojPath = GetArgValue(args, "--csproj", "-c", "src/PandaBot/PandaBot.csproj") ?? "src/PandaBot/PandaBot.csproj";
        string changelogPath = GetArgValue(args, "--changelog", "-l", "CHANGELOG.md") ?? "CHANGELOG.md";

        var csprojVersion = ExtractCsprojVersion(csprojPath);
        var changelogVersion = ExtractChangelogVersion(changelogPath);

        Console.WriteLine($"Version in .csproj: {csprojVersion}");
        Console.WriteLine($"Version in CHANGELOG: {changelogVersion}");

        if (csprojVersion == changelogVersion)
        {
            Console.WriteLine("✓ Versions match! All good.");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Version mismatch!");
            Console.WriteLine($"  .csproj: {csprojVersion}");
            Console.WriteLine($"  CHANGELOG: {changelogVersion}");
            return 1;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"✗ Error: {ex.Message}");
        return 2;
    }
}

int BumpVersion(string[] args)
{
    try
    {
        string? version = GetArgValue(args, "--version", "-v", null);
        if (string.IsNullOrWhiteSpace(version))
        {
            Console.Error.WriteLine("✗ Error: --version/-v is required");
            return 2;
        }

        string csprojPath = GetArgValue(args, "--csproj", "-c", "src/PandaBot/PandaBot.csproj") ?? "src/PandaBot/PandaBot.csproj";
        string changelogPath = GetArgValue(args, "--changelog", "-l", "CHANGELOG.md") ?? "CHANGELOG.md";
        string type = GetArgValue(args, "--type", "-t", "patch") ?? "patch";
        string message = GetArgValue(args, "--message", "-m", "") ?? "";

        // Update .csproj
        UpdateCsprojVersion(csprojPath, version);
        Console.WriteLine($"✓ Updated .csproj version to {version}");

        // Update CHANGELOG
        UpdateChangelogVersion(changelogPath, version, type, message);
        Console.WriteLine($"✓ Updated CHANGELOG version to {version}");

        Console.WriteLine("✓ Version bump complete!");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"✗ Error: {ex.Message}");
        return 2;
    }
}

int PrintUsage()
{
    Console.WriteLine(@"
PandaBot Version Manager - Synchronizes .csproj and CHANGELOG versions

USAGE:
  VersionManager <command> [options]

COMMANDS:
  validate    Validate that .csproj and CHANGELOG versions match
  bump        Bump version in both .csproj and CHANGELOG
  help        Show this help message

VALIDATE OPTIONS:
  --csproj, -c <path>       Path to .csproj file (default: src/PandaBot/PandaBot.csproj)
  --changelog, -l <path>    Path to CHANGELOG.md file (default: CHANGELOG.md)

BUMP OPTIONS:
  --version, -v <version>   New version number (required)
  --csproj, -c <path>       Path to .csproj file (default: src/PandaBot/PandaBot.csproj)
  --changelog, -l <path>    Path to CHANGELOG.md file (default: CHANGELOG.md)
  --type, -t <type>         Type of bump: patch, minor, major (default: patch)
  --message, -m <msg>       Changelog message/description

EXAMPLES:
  VersionManager validate
  VersionManager bump --version 1.0.5 --type patch --message ""Star Citizen API fix""
");
    return 0;
}

int UnsupportedCommand(string command)
{
    Console.Error.WriteLine($"✗ Unknown command: {command}");
    Console.Error.WriteLine("Use 'VersionManager help' for usage information");
    return 1;
}

string? GetArgValue(string[] args, string longForm, string shortForm, string? defaultValue)
{
    for (int i = 0; i < args.Length; i++)
    {
        if ((args[i] == longForm || args[i] == shortForm) && i + 1 < args.Length)
        {
            return args[i + 1];
        }
    }
    return defaultValue;
}

string ExtractCsprojVersion(string filePath)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"File not found: {filePath}");

    var content = File.ReadAllText(filePath);
    var match = Regex.Match(content, @"<Version>([^<]+)</Version>");

    if (!match.Success)
        throw new InvalidOperationException("Could not find <Version> tag in .csproj file");

    return match.Groups[1].Value;
}

string ExtractChangelogVersion(string filePath)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"File not found: {filePath}");

    var content = File.ReadAllText(filePath);
    // Match ## [x.x.x] format, handling both escaped and non-escaped brackets
    var match = Regex.Match(content, @"##\s+\\?\[([^\]]+)\]");

    if (!match.Success)
        throw new InvalidOperationException("Could not find version header in CHANGELOG.md");

    return match.Groups[1].Value;
}

void UpdateCsprojVersion(string filePath, string newVersion)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"File not found: {filePath}");

    var content = File.ReadAllText(filePath);
    var updatedContent = Regex.Replace(
        content,
        @"<Version>[^<]+</Version>",
        $"<Version>{newVersion}</Version>"
    );

    if (updatedContent == content)
        throw new InvalidOperationException("Failed to update version in .csproj");

    File.WriteAllText(filePath, updatedContent);
}

void UpdateChangelogVersion(string filePath, string newVersion, string type, string message)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"File not found: {filePath}");

    var content = File.ReadAllText(filePath);
    var today = DateTime.Now.ToString("yyyy-MM-dd");

    var newEntry = $"## [{newVersion}] - {today}\n";
    if (!string.IsNullOrWhiteSpace(message))
    {
        newEntry += $"\n### {type.ToUpper()}\n\n";
        newEntry += $"- {message}\n";
    }
    else
    {
        newEntry += $"\n### {type.ToUpper()}\n\n";
        newEntry += "- \n";
    }

    // Insert after the "# Changelog" header
    var updatedContent = Regex.Replace(
        content,
        @"(#\s+Changelog\s*\n)",
        $"$1\n{newEntry}",
        RegexOptions.Multiline
    );

    File.WriteAllText(filePath, updatedContent);
}
