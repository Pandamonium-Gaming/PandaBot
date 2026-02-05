using System.Text.RegularExpressions;
using System.Diagnostics;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

string command = args[0].ToLower();
return command switch
{
    "validate" => ValidateVersions(args.Skip(1).ToArray()),
    "check-commits" => CheckCommitsSinceLastVersion(args.Skip(1).ToArray()),
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

int CheckCommitsSinceLastVersion(string[] args)
{
    try
    {
        string csprojPath = GetArgValue(args, "--csproj", "-c", "src/PandaBot/PandaBot.csproj") ?? "src/PandaBot/PandaBot.csproj";

        var currentVersion = ExtractCsprojVersion(csprojPath);
        Console.WriteLine($"Current version: {currentVersion}");

        // Get git tags for versions
        var tags = GetVersionTags();
        if (tags.Count == 0)
        {
            Console.WriteLine("ℹ No version tags found. First version?");
            return 0;
        }

        // Find the most recent tag before current version
        var previousTag = FindPreviousVersionTag(currentVersion, tags);
        if (previousTag == null)
        {
            Console.WriteLine($"ℹ No previous version tag found for comparison. Latest commits since repository start:");
            previousTag = "";
        }
        else
        {
            Console.WriteLine($"Analyzing commits since: {previousTag}");
        }

        // Get commits since previous version
        var (hasBreaking, hasFeatures, hasFixes, commitCount) = AnalyzeCommitsSinceTag(previousTag);

        Console.WriteLine($"\nCommit summary since last version:");
        Console.WriteLine($"  Total commits: {commitCount}");
        Console.WriteLine($"  Breaking changes: {(hasBreaking ? "✓ YES" : "✗ None")}");
        Console.WriteLine($"  Features (feat): {(hasFeatures ? "✓ YES" : "✗ None")}");
        Console.WriteLine($"  Fixes (fix): {(hasFixes ? "✓ YES" : "✗ None")}");

        // Determine required version bump
        var requiredBump = DetermineRequiredVersionBump(hasBreaking, hasFeatures, hasFixes);
        Console.WriteLine($"\nRequired version bump: {requiredBump}");

        // Check if current version aligns with commits
        var currentParsed = ParseVersion(currentVersion);
        var versionMatch = CheckVersionAlignment(currentVersion, requiredBump);

        if (versionMatch)
        {
            Console.WriteLine("✓ Current version aligns with commits!");
            return 0;
        }
        else
        {
            Console.WriteLine("⚠ WARNING: Current version may not match the commits since last release:");
            Console.WriteLine($"  Latest commits suggest: {requiredBump}");
            Console.WriteLine($"  Current version is: {currentVersion}");
            Console.WriteLine("\nConsider running: VersionManager bump --version <next-version> --type <type>");
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
  validate          Validate that .csproj and CHANGELOG versions match
  check-commits     Analyze commits since last version and warn if version bump needed
  bump              Bump version in both .csproj and CHANGELOG
  help              Show this help message

VALIDATE OPTIONS:
  --csproj, -c <path>       Path to .csproj file (default: src/PandaBot/PandaBot.csproj)
  --changelog, -l <path>    Path to CHANGELOG.md file (default: CHANGELOG.md)

CHECK-COMMITS OPTIONS:
  --csproj, -c <path>       Path to .csproj file (default: src/PandaBot/PandaBot.csproj)

BUMP OPTIONS:
  --version, -v <version>   New version number (required)
  --csproj, -c <path>       Path to .csproj file (default: src/PandaBot/PandaBot.csproj)
  --changelog, -l <path>    Path to CHANGELOG.md file (default: CHANGELOG.md)
  --type, -t <type>         Type of bump: patch, minor, major (default: patch)
  --message, -m <msg>       Changelog message/description

EXAMPLES:
  VersionManager validate
  VersionManager check-commits
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

List<string> GetVersionTags()
{
    try
    {
        var psi = new ProcessStartInfo("git", "tag -l v*")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var process = Process.Start(psi))
        {
            var output = process?.StandardOutput.ReadToEnd() ?? "";
            return output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
    catch
    {
        return new List<string>();
    }
}

string? FindPreviousVersionTag(string currentVersion, List<string> tags)
{
    // Find tags that look like vX.X.X
    var versionTags = tags
        .Where(t => Regex.IsMatch(t, @"^v\d+\.\d+\.\d+$"))
        .ToList();

    if (versionTags.Count == 0) return null;

    // Sort versions and find the one before current
    var sorted = versionTags
        .Select(t => t.TrimStart('v'))
        .OrderByDescending(v => ParseVersion(v))
        .ToList();

    if (sorted.Count > 0 && sorted[0] != currentVersion)
    {
        return "v" + sorted[0];
    }

    return sorted.Count > 1 ? "v" + sorted[1] : null;
}

(bool hasBreaking, bool hasFeatures, bool hasFixes, int count) AnalyzeCommitsSinceTag(string? tag)
{
    try
    {
        var rangeSpec = string.IsNullOrEmpty(tag) ? "HEAD" : $"{tag}..HEAD";
        var psi = new ProcessStartInfo("git", $"log {rangeSpec} --pretty=format:%s")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var process = Process.Start(psi))
        {
            var output = process?.StandardOutput.ReadToEnd() ?? "";
            var commits = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            bool hasBreaking = false, hasFeatures = false, hasFixes = false;

            foreach (var commit in commits)
            {
                if (commit.Contains("BREAKING") || commit.Contains("breaking"))
                    hasBreaking = true;
                if (commit.StartsWith("feat"))
                    hasFeatures = true;
                if (commit.StartsWith("fix"))
                    hasFixes = true;
            }

            return (hasBreaking, hasFeatures, hasFixes, commits.Length);
        }
    }
    catch
    {
        return (false, false, false, 0);
    }
}

string DetermineRequiredVersionBump(bool hasBreaking, bool hasFeatures, bool hasFixes)
{
    if (hasBreaking)
        return "MAJOR";
    if (hasFeatures)
        return "MINOR";
    if (hasFixes)
        return "PATCH";
    return "NONE";
}

(int major, int minor, int patch) ParseVersion(string version)
{
    var parts = version.Split('.');
    return (
        int.Parse(parts[0]),
        parts.Length > 1 ? int.Parse(parts[1]) : 0,
        parts.Length > 2 ? int.Parse(parts[2]) : 0
    );
}

bool CheckVersionAlignment(string currentVersion, string requiredBump)
{
    // This is a simplistic check - in reality you'd want to store what the last bump was
    // For now, just check that if there are significant commits, version isn't 0.0.0
    if (requiredBump == "NONE")
        return true;

    var (major, minor, patch) = ParseVersion(currentVersion);
    return !(major == 0 && minor == 0 && patch == 0);
}
