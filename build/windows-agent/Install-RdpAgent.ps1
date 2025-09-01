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
