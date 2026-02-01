# Version Bump Helper
# Updates version in .csproj and regenerates changelog
# Usage: .\Bump-Version.ps1 -Type patch|minor|major [-NoCommit]

param(
    [ValidateSet('patch', 'minor', 'major')]
    [string]$Type = "patch",
    [switch]$NoCommit
)

$ErrorActionPreference = "Stop"
$csprojFile = "src/PandaBot/PandaBot.csproj"
$csprojContent = Get-Content $csprojFile -Raw
$versionMatch = [regex]::Match($csprojContent, '<Version>([^<]+)</Version>')
if (-not $versionMatch.Success) {
    Write-Error "Could not find version in $csprojFile"
    exit 1
}

$currentVersion = $versionMatch.Groups[1].Value
$parts = $currentVersion -split '\.'
if ($parts.Count -ne 3) {
    Write-Error "Invalid version format: $currentVersion. Expected X.Y.Z"
    exit 1
}

[int]$major = $parts[0]
[int]$minor = $parts[1]
[int]$patch = $parts[2]

switch ($Type) {
    'major' { $major++; $minor = 0; $patch = 0 }
    'minor' { $minor++; $patch = 0 }
    'patch' { $patch++ }
}

$newVersion = "$major.$minor.$patch"

Write-Host "Bumping version: $currentVersion → $newVersion" -ForegroundColor Cyan
Write-Host "Updating $csprojFile..." -ForegroundColor Yellow
$updatedContent = $csprojContent -replace "<Version>$currentVersion</Version>", "<Version>$newVersion</Version>"
Set-Content -Path $csprojFile -Value $updatedContent -Encoding UTF8
Write-Host "✓ Updated .csproj" -ForegroundColor Green

Write-Host "Regenerating CHANGELOG.md..." -ForegroundColor Yellow
& ".\Generate-Changelog.ps1"

if (-not $NoCommit) {
    Write-Host "Committing changes..." -ForegroundColor Yellow
    git add $csprojFile CHANGELOG.md
    git commit -m "chore: bump version to $newVersion"
    Write-Host "✓ Committed" -ForegroundColor Green
}

Write-Host "✅ Version bump complete!" -ForegroundColor Green
