# Pre-commit hook for Windows (PowerShell)
# Validates version synchronization between .csproj and CHANGELOG.md

$ErrorActionPreference = "Stop"

Write-Host "Validating version synchronization..." -ForegroundColor Yellow

# Extract version from .csproj
$csprojFile = "src/PandaBot/PandaBot.csproj"
if (-not (Test-Path $csprojFile)) {
    Write-Host "Error: $csprojFile not found" -ForegroundColor Red
    exit 1
}

$csprojContent = Get-Content $csprojFile -Raw
$csprojMatch = [regex]::Match($csprojContent, '<Version>([^<]+)</Version>')
if (-not $csprojMatch.Success) {
    Write-Host "Error: Could not extract version from $csprojFile" -ForegroundColor Red
    exit 1
}
$csprojVersion = $csprojMatch.Groups[1].Value

Write-Host "Version in .csproj: " -ForegroundColor White -NoNewline
Write-Host $csprojVersion -ForegroundColor Green

# Extract version from CHANGELOG.md
$changelogFile = "CHANGELOG.md"
if (-not (Test-Path $changelogFile)) {
    Write-Host "Error: $changelogFile not found" -ForegroundColor Red
    exit 1
}

$changelogContent = Get-Content $changelogFile -Raw
$changelogMatch = [regex]::Match($changelogContent, '## \\?\[([^\]]+)\]')
if (-not $changelogMatch.Success) {
    Write-Host "Error: Could not extract version from $changelogFile" -ForegroundColor Red
    exit 1
}
$changelogVersion = $changelogMatch.Groups[1].Value

Write-Host "Version in CHANGELOG: " -ForegroundColor White -NoNewline
Write-Host $changelogVersion -ForegroundColor Green

# Compare versions
if ($csprojVersion -ne $changelogVersion) {
    Write-Host "`nVersion Mismatch!" -ForegroundColor Red
    Write-Host "   .csproj version: " -ForegroundColor White -NoNewline
    Write-Host $csprojVersion -ForegroundColor Red
    Write-Host "   CHANGELOG version: " -ForegroundColor White -NoNewline
    Write-Host $changelogVersion -ForegroundColor Red
    Write-Host ""
    Write-Host "Fix this by:" -ForegroundColor Yellow
    Write-Host "   1. Update version in $csprojFile to match CHANGELOG"
    Write-Host "   2. OR update CHANGELOG to match .csproj version"
    Write-Host "   3. Make sure CHANGELOG entry is: ## [$csprojVersion] - YYYY-MM-DD"
    Write-Host ""
    exit 1
}

Write-Host "`nVersions match! Proceeding with commit..." -ForegroundColor Green
exit 0
