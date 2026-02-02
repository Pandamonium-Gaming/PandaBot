#!/bin/bash
# Setup script for PandaBot development environment

echo "🔧 PandaBot Development Setup"
echo "=============================="
echo ""

# Check if git is configured to use .githooks
HOOKS_PATH=$(git config core.hooksPath 2>/dev/null || echo "")

if [ "$HOOKS_PATH" = ".githooks" ]; then
    echo "✅ Git hooks path already configured"
else
    echo "⚙️  Configuring git hooks path..."
    git config core.hooksPath .githooks
    echo "✅ Git hooks configured to use .githooks"
fi

# Make pre-commit hook executable
if [ -f ".githooks/pre-commit" ]; then
    chmod +x .githooks/pre-commit
    echo "✅ Pre-commit hook is executable"
else
    echo "❌ Warning: .githooks/pre-commit not found"
fi

echo ""
echo "✅ Setup complete!"
echo ""
echo "📖 For development guidelines, see: DEVELOPMENT.md"
echo ""
echo "🚀 You're ready to start contributing!"
