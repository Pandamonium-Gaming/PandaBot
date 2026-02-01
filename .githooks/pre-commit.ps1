# Pre-commit hook for Windows (PowerShell)
# Validates version synchronization using the .NET VersionManager tool

Write-Host "Validating version synchronization..." -ForegroundColor Yellow

$dllPath = "artifacts/bin/VersionManager/release/VersionManager.dll"

if (-not (Test-Path $dllPath)) {
    Write-Host "VersionManager not built. Building now..." -ForegroundColor Yellow
    dotnet build tools/VersionManager/VersionManager.csproj -c Release -q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to build VersionManager" -ForegroundColor Red
        exit 1
    }
}

# Run the VersionManager tool
dotnet $dllPath validate 2>&1 | ForEach-Object {
    if ($_ -match "match") {
        Write-Host $_ -ForegroundColor Green
    } elseif ($_ -match "mismatch") {
        Write-Host $_ -ForegroundColor Red
    } else {
        Write-Host $_
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Commit blocked: Version mismatch!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please update versions using:" -ForegroundColor Yellow
    Write-Host "  dotnet artifacts/bin/VersionManager/release/VersionManager.dll bump --version X.X.X --type patch --message `"Your message`""
    Write-Host ""
    exit 1
}

Write-Host "[OK] Pre-commit hook passed!" -ForegroundColor Green
exit 0
