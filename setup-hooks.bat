@echo off
REM Setup script for PandaBot development environment on Windows

echo.
echo PandaBot Development Setup
echo ==========================
echo.

REM Check if git is configured to use .githooks
for /f "tokens=*" %%i in ('git config core.hooksPath 2^>nul') do set HOOKS_PATH=%%i

if "%HOOKS_PATH%"==".githooks" (
    echo ✅ Git hooks path already configured
) else (
    echo ⚙️  Configuring git hooks path...
    git config core.hooksPath .githooks
    echo ✅ Git hooks configured to use .githooks
)

REM Configure git to use PowerShell for hooks on Windows
echo ⚙️  Configuring git to use PowerShell for hooks...
git config core.hooksPath .githooks
REM Note: Windows will automatically use .ps1 files with PowerShell

echo.
echo ✅ Setup complete!
echo.
echo 📖 For development guidelines, see: DEVELOPMENT.md
echo.
echo 🚀 You're ready to start contributing!
echo.
