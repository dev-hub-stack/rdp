#!/bin/bash

# Build Windows Agent Installation Package
set -e

echo "🔨 Building Windows Agent Installation Package"
echo "=============================================="

# Configuration
AGENT_DIR="agent-win/RdpRelay.Agent.Win"
BUILD_DIR="build/windows-agent"
REMOTE_SERVER="159.89.112.134"

# Clean and create build directory
echo "📁 Creating build directory..."
rm -rf build/
mkdir -p "$BUILD_DIR"

# Build the agent
echo "🔧 Building Windows Agent..."
cd "$AGENT_DIR"
dotnet publish -c Release -r win-x64 --self-contained true -o "../../$BUILD_DIR/app"
cd ../..

# Create configuration file
echo "⚙️ Creating configuration template..."
cat > "$BUILD_DIR/app/appsettings.json" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Agent": {
    "RelayUrl": "wss://$REMOTE_SERVER:9443",
    "PortalApiUrl": "http://$REMOTE_SERVER:5000", 
    "ProvisioningToken": "REPLACE_WITH_GENERATED_TOKEN",
    "HeartbeatIntervalSeconds": 30,
    "MaxRdpConnections": 5,
    "LogLevel": "Information"
  }
}
EOF

# Create PowerShell installer script
echo "📦 Creating PowerShell installer..."
cat > "$BUILD_DIR/Install-RdpAgent.ps1" << 'EOF'
# RDP Relay Windows Agent Installer
# Run as Administrator

param(
    [Parameter(Mandatory=$true)]
    [string]$ProvisioningToken,
    
    [string]$InstallPath = "C:\Program Files\RdpRelay\Agent",
    [string]$ServiceName = "RdpRelayAgent"
)

Write-Host "🔐 Checking administrator privileges..." -ForegroundColor Blue
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "❌ This script requires administrator privileges. Please run as Administrator."
    exit 1
}

Write-Host "✅ Administrator privileges confirmed" -ForegroundColor Green

# Stop and remove existing service
Write-Host "🛑 Stopping existing service if running..." -ForegroundColor Yellow
try {
    Stop-Service $ServiceName -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName 2>$null | Out-Null
} catch {
    Write-Host "   Service not running or doesn't exist" -ForegroundColor Gray
}

# Create install directory
Write-Host "📁 Creating install directory..." -ForegroundColor Blue
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null

# Copy application files
Write-Host "📦 Copying agent files..." -ForegroundColor Blue
Copy-Item -Path "app\*" -Destination $InstallPath -Recurse -Force

# Update configuration with token
Write-Host "⚙️ Updating configuration..." -ForegroundColor Blue
$configPath = "$InstallPath\appsettings.json"
$config = Get-Content $configPath | ConvertFrom-Json
$config.Agent.ProvisioningToken = $ProvisioningToken
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath

# Install as Windows service
Write-Host "🔧 Installing Windows service..." -ForegroundColor Blue
$exePath = "$InstallPath\RdpRelay.Agent.Win.exe"
sc.exe create $ServiceName binPath= "`"$exePath`"" DisplayName= "RDP Relay Agent" start= auto | Out-Null

# Configure service recovery
sc.exe failure $ServiceName reset= 86400 actions= restart/10000/restart/10000/restart/10000 | Out-Null

# Start service
Write-Host "▶️ Starting service..." -ForegroundColor Blue
Start-Service $ServiceName

# Check service status
Write-Host "🔍 Service status:" -ForegroundColor Blue
Get-Service $ServiceName

Write-Host ""
Write-Host "✅ RDP Relay Agent installed successfully!" -ForegroundColor Green
Write-Host "📋 Service Information:" -ForegroundColor Cyan
Write-Host "   Name: $ServiceName"
Write-Host "   Install Directory: $InstallPath"
Write-Host "   Logs: $InstallPath\logs\"
Write-Host ""
Write-Host "🛠️ Management Commands:" -ForegroundColor Cyan
Write-Host "   Start:   Start-Service $ServiceName"
Write-Host "   Stop:    Stop-Service $ServiceName" 
Write-Host "   Status:  Get-Service $ServiceName"
Write-Host "   Logs:    Get-Content '$InstallPath\logs\*.log' -Tail 50"
EOF

# Create batch file for easy execution
cat > "$BUILD_DIR/install.bat" << 'EOF'
@echo off
echo RDP Relay Agent Installer
echo =========================
echo.
echo This will install the RDP Relay Agent as a Windows Service.
echo.
set /p TOKEN=Enter your provisioning token: 
echo.
echo Installing with token: %TOKEN%
echo.
powershell -ExecutionPolicy Bypass -File "Install-RdpAgent.ps1" -ProvisioningToken "%TOKEN%"
pause
EOF

# Create README
cat > "$BUILD_DIR/README.md" << EOF
# RDP Relay Windows Agent Installation

## Prerequisites
- Windows 10/11 or Windows Server 2019+
- Administrator privileges
- Network access to RDP Relay server

## Installation Steps

### Method 1: Using PowerShell (Recommended)
1. Open PowerShell as Administrator
2. Navigate to this folder
3. Run: \`.\Install-RdpAgent.ps1 -ProvisioningToken "YOUR_TOKEN_HERE"\`

### Method 2: Using Batch File
1. Right-click \`install.bat\` and select "Run as administrator"
2. Enter your provisioning token when prompted

## Getting a Provisioning Token
1. Access the RDP Relay web portal: http://$REMOTE_SERVER:8080
2. Login with your credentials
3. Go to the **Agents** page
4. Click **"Generate Token"**
5. Copy the generated token

## Verification
After installation, verify the agent is working:

1. **Check Service Status:**
   \`\`\`powershell
   Get-Service RdpRelayAgent
   \`\`\`

2. **View Logs:**
   \`\`\`powershell
   Get-Content "C:\Program Files\RdpRelay\Agent\logs\*.log" -Tail 20
   \`\`\`

3. **Check Web Portal:**
   - The agent should appear as "Online" in the web portal
   - Status updates every 30 seconds

## Troubleshooting

### Agent Shows as Offline
- Check Windows Firewall settings
- Verify network connectivity to $REMOTE_SERVER:9443
- Check service logs for errors

### Service Won't Start
- Verify .NET Runtime is installed
- Check file permissions in install directory
- Run \`Get-EventLog -LogName Application -Source "RdpRelay*" -Newest 10\`

### Connection Issues
- Test connectivity: \`Test-NetConnection $REMOTE_SERVER -Port 9443\`
- Verify provisioning token is not expired
- Check server logs for authentication errors

## Management Commands

\`\`\`powershell
# Start service
Start-Service RdpRelayAgent

# Stop service  
Stop-Service RdpRelayAgent

# Restart service
Restart-Service RdpRelayAgent

# Check status
Get-Service RdpRelayAgent

# View logs
Get-Content "C:\Program Files\RdpRelay\Agent\logs\*.log" -Tail 50

# Uninstall
Stop-Service RdpRelayAgent
sc.exe delete RdpRelayAgent
Remove-Item "C:\Program Files\RdpRelay" -Recurse -Force
\`\`\`

## Support
For issues, check the logs first and verify network connectivity.
EOF

echo ""
echo "✅ Windows Agent package created successfully!"
echo ""
echo "📦 Package Location: $BUILD_DIR/"
echo "📋 Package Contents:"
ls -la "$BUILD_DIR/"
echo ""
echo "🚀 Next Steps:"
echo "1. Copy the entire '$BUILD_DIR' folder to your Windows machine"
echo "2. Generate a provisioning token from the web portal"
echo "3. Run Install-RdpAgent.ps1 with the token on Windows"
echo ""
