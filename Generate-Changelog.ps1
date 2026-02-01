# Changelog Generator from Conventional Commits
# Generates CHANGELOG.md from git commit history
# Usage: .\Generate-Changelog.ps1 [-OutputFile "CHANGELOG.md"]

param(
    [string]$OutputFile = "CHANGELOG.md"
)

$ErrorActionPreference = "Stop"

Write-Host "Generating changelog from git commits..." -ForegroundColor Cyan

# Get all commits since last tag (or all if no tags)
$lastTag = $null
try {
    $lastTag = git describe --tags --abbrev=0 2>&1
    if ($LASTEXITCODE -ne 0) {
        $lastTag = $null
    }
} catch {
    $lastTag = $null
}
if ($null -eq $lastTag) {
    Write-Host "No tags found, processing all commits" -ForegroundColor Yellow
    $commits = git log --pretty=format:"%H|%ae|%ad|%s|%b" --date=short 2>$null
} else {
    Write-Host "Processing commits since $lastTag" -ForegroundColor Yellow
    $commits = git log "$lastTag..HEAD" --pretty=format:"%H|%ae|%ad|%s|%b" --date=short 2>$null
}

if (-not $commits) {
    Write-Host "No new commits found" -ForegroundColor Gray
    exit 0
}

# Parse commits into conventional format
$added = @()
$fixed = @()
$changed = @()
$breaking = @()

foreach ($commit in $commits) {
    if (-not $commit) { continue }
    
    $parts = $commit -split '\|', 5
    if ($parts.Count -lt 4) { continue }
    
    $hash = $parts[0].Substring(0, 7)
    $subject = $parts[3]
    $body = $parts[4]
    
    # Check for breaking changes
    if ($body -match 'BREAKING CHANGE:') {
        $msg = $subject
        if ($body -match 'BREAKING CHANGE: (.+)') {
            $msg = $Matches[1]
        }
        $breaking += @{ msg = $msg; hash = $hash }
    }
    # Parse conventional commit
    elseif ($subject -match '^feat(\(.+\))?:') {
        $msg = $subject -replace '^feat(\(.+\))?: ', ''
        $added += @{ msg = $msg; hash = $hash }
    }
    elseif ($subject -match '^fix(\(.+\))?:') {
        $msg = $subject -replace '^fix(\(.+\))?: ', ''
        $fixed += @{ msg = $msg; hash = $hash }
    }
    elseif ($subject -match '^chore(\(.+\))?:') {
        # Skip chore commits in changelog
    }
    elseif ($subject -match '^docs(\(.+\))?:') {
        # Skip docs commits
    }
    elseif ($subject -match '^refactor(\(.+\))?:') {
        $msg = $subject -replace '^refactor(\(.+\))?: ', ''
        $changed += @{ msg = $msg; hash = $hash }
    }
}

# Extract current version from .csproj
$csprojContent = Get-Content "src/PandaBot/PandaBot.csproj" -Raw
$versionMatch = [regex]::Match($csprojContent, '<Version>([^<]+)</Version>')
if (-not $versionMatch.Success) {
    Write-Error "Could not find version in .csproj"
    exit 1
}
$version = $versionMatch.Groups[1].Value
$date = Get-Date -Format "yyyy-MM-dd"

# Generate changelog entry
$changelogEntry = "## [$version] - $date`n`n"

if ($breaking.Count -gt 0) {
    $changelogEntry += "### BREAKING CHANGES`n`n"
    foreach ($item in $breaking) {
        $changelogEntry += "* $($item.msg) ([``$($item.hash)``])`n"
    }
    $changelogEntry += "`n"
}

if ($added.Count -gt 0) {
    $changelogEntry += "### Added`n`n"
    foreach ($item in $added) {
        $changelogEntry += "* $($item.msg) ([``$($item.hash)``])`n"
    }
    $changelogEntry += "`n"
}

if ($fixed.Count -gt 0) {
    $changelogEntry += "### Fixed`n`n"
    foreach ($item in $fixed) {
        $changelogEntry += "* $($item.msg) ([``$($item.hash)``])`n"
    }
    $changelogEntry += "`n"
}

if ($changed.Count -gt 0) {
    $changelogEntry += "### Changed`n`n"
    foreach ($item in $changed) {
        $changelogEntry += "* $($item.msg) ([``$($item.hash)``])`n"
    }
    $changelogEntry += "`n"
}

# Read existing changelog (if it exists)
$existingChangelog = ""
if (Test-Path $OutputFile) {
    $existingChangelog = Get-Content $OutputFile -Raw
}

# Write new changelog with header + new entry + old entries
$newChangelog = "# Changelog`n`nAll notable changes to PandaBot will be documented in this file.`n`nThe format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),`nand this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).`n`n$changelogEntry"

if ($existingChangelog) {
    # Skip the header and first blank line from existing
    $existingLines = $existingChangelog -split "`n"
    $skipIndex = 0
    for ($i = 0; $i -lt $existingLines.Count; $i++) {
        if ($existingLines[$i] -match '## \[') {
            $skipIndex = $i
            break
        }
    }
    if ($skipIndex -gt 0) {
        $existingBody = $existingLines[$skipIndex..($existingLines.Count - 1)] -join "`n"
        $newChangelog += $existingBody
    }
}

# Write to file
Set-Content -Path $OutputFile -Value $newChangelog -Encoding UTF8

Write-Host "Changelog generated: $OutputFile" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Breaking changes: $($breaking.Count)" -ForegroundColor $(if ($breaking.Count -gt 0) { 'Red' } else { 'Gray' })
Write-Host "  Added: $($added.Count)" -ForegroundColor Green
Write-Host "  Fixed: $($fixed.Count)" -ForegroundColor Yellow
Write-Host "  Changed: $($changed.Count)" -ForegroundColor Cyan
