# PandaBot Deployment Setup Guide

This guide covers setting up PandaBot on a DigitalOcean droplet (or any Linux host).

## Prerequisites

- Linux host (Ubuntu 20.04+)
- SSH access with sudo privileges
- SSH key pair for deployment

## Initial Setup (One-time)

### 1. Create Deployment User

```bash
sudo useradd -m -s /bin/bash deployment
sudo usermod -aG sudo deployment
```

### 2. Setup Directory Structure

```bash
sudo mkdir -p /opt/pandabot
sudo chown deployment:deployment /opt/pandabot
```

### 3. Create `.env` File

The deployment workflow automatically creates/updates this file with secrets from GitHub Actions.

Manual creation (if needed):
```bash
sudo tee /opt/pandabot/.env > /dev/null <<EOF
PANDABOT_Discord__Token=your_discord_token_here
PANDABOT_Discord__Prefix=!
PANDABOT_ConnectionStrings__DefaultConnection=Data Source=/opt/pandabot/pandabot.db
PANDABOT_AshesForge__CacheExpirationHours=24
PANDABOT_AshesForge__EnableImageCaching=true
EOF

sudo chmod 600 /opt/pandabot/.env
```

### 4. Install .NET Runtime

```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir /usr/local/dotnet
sudo ln -sf /usr/local/dotnet/dotnet /usr/bin/dotnet
```

### 5. Install Systemd Service

```bash
sudo cp pandabot.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable pandabot.service
```

## Deployment Workflow

The GitHub Actions workflow handles:
1. Building the project
2. Publishing Release build
3. Uploading files via SSH
4. Creating/updating `.env` file with secrets
5. Managing systemd service (stop → deploy → start)
6. Checking service status

### Required GitHub Secrets

Set these in your repository settings (Settings → Secrets and variables → Actions):

- `DEPLOY_SSH_KEY`: Private SSH key for deployment user
- `DEPLOY_HOST`: IP/hostname of your Digital Ocean droplet
- `DISCORD_TOKEN`: Your Discord bot token

## Managing the Service

After initial setup, manage the bot with:

```bash
# Check status
sudo systemctl status pandabot.service

# View logs
sudo journalctl -u pandabot.service -f

# Restart manually
sudo systemctl restart pandabot.service

# Stop
sudo systemctl stop pandabot.service

# Start
sudo systemctl start pandabot.service
```

## Updating Environment Variables

### Option 1: Via Deployment
Update the workflow in `.github/workflows/deploy.yml` and push to main branch.

### Option 2: Manual SSH
```bash
ssh deployment@your-host
sudo nano /opt/pandabot/.env
# Edit the file, save and exit
sudo systemctl restart pandabot.service
```

## Troubleshooting

### Service won't start
```bash
sudo journalctl -u pandabot.service -n 50
```

### .env file not found
```bash
ls -la /opt/pandabot/.env
# Should show: -rw------- (600 permissions)
```

### Permission denied errors
```bash
sudo chown -R deployment:deployment /opt/pandabot
```

### Check if .NET is installed
```bash
dotnet --version
```

## Database

SQLite database is stored at `/opt/pandabot/pandabot.db`. This persists between deployments.

To backup:
```bash
cp /opt/pandabot/pandabot.db /opt/pandabot/pandabot.db.backup
```
