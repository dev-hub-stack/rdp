# ========================================
# RDP Relay Agent - Simple Reinstall Script
# ========================================

param(
    [string]$InstallPath = "C:\RdpRelay"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RDP Relay Agent - Simple Reinstall" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "ERROR: This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please right-click and 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Step 1: Removing existing installation..." -ForegroundColor Yellow

# Stop and remove service
try {
    $service = Get-Service -Name "RdpRelayAgent" -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "  Stopping service..." -ForegroundColor Gray
        Stop-Service -Name "RdpRelayAgent" -Force -ErrorAction SilentlyContinue
        
        Write-Host "  Removing service..." -ForegroundColor Gray
        sc.exe delete RdpRelayAgent | Out-Null
        Start-Sleep -Seconds 2
        Write-Host "  Service removed" -ForegroundColor Green
    }
} catch {
    Write-Host "  Warning: Could not remove service" -ForegroundColor Yellow
}

# Remove directory
if (Test-Path $InstallPath) {
    Write-Host "  Removing directory: $InstallPath" -ForegroundColor Gray
    Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Directory removed" -ForegroundColor Green
}

Write-Host "Step 2: Installing fresh agent..." -ForegroundColor Yellow

# Create directories
New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
New-Item -Path "$InstallPath\Agent" -ItemType Directory -Force | Out-Null
New-Item -Path "$InstallPath\Agent\logs" -ItemType Directory -Force | Out-Null

# Copy files
$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Copy-Item -Path "$currentDir\app\*" -Destination "$InstallPath\Agent" -Recurse -Force
Write-Host "  Application files copied" -ForegroundColor Green

# Install service
$servicePath = "$InstallPath\Agent\RdpRelay.Agent.Win.exe"
New-Service -Name "RdpRelayAgent" `
            -BinaryPathName $servicePath `
            -DisplayName "RDP Relay Agent" `
            -Description "RDP Relay Agent for secure remote desktop connections" `
            -StartupType Automatic

Write-Host "  Service installed" -ForegroundColor Green

# Start service
Start-Service -Name "RdpRelayAgent"
Start-Sleep -Seconds 3

$serviceStatus = Get-Service -Name "RdpRelayAgent"
Write-Host "  Service Status: $($serviceStatus.Status)" -ForegroundColor White

Write-Host "Step 3: Testing connectivity..." -ForegroundColor Yellow

# Test Portal API
Write-Host "  Testing Portal API (192.168.18.101:8080)..." -NoNewline -ForegroundColor Gray
$portalTest = Test-NetConnection -ComputerName "192.168.18.101" -Port 8080 -WarningAction SilentlyContinue
if ($portalTest.TcpTestSucceeded) {
    Write-Host " CONNECTED" -ForegroundColor Green
} else {
    Write-Host " FAILED" -ForegroundColor Red
}

# Test Relay Server  
Write-Host "  Testing Relay Server (192.168.18.101:5001)..." -NoNewline -ForegroundColor Gray
$relayTest = Test-NetConnection -ComputerName "192.168.18.101" -Port 5001 -WarningAction SilentlyContinue
if ($relayTest.TcpTestSucceeded) {
    Write-Host " CONNECTED" -ForegroundColor Green
} else {
    Write-Host " FAILED" -ForegroundColor Red
}

Write-Host ""
Write-Host "Installation completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Open browser: http://192.168.18.101:8080" -ForegroundColor White
Write-Host "2. Login: test@test.com / password" -ForegroundColor White  
Write-Host "3. Go to Agents section" -ForegroundColor White
Write-Host "4. Look for agent: 68b42ba78f7aa507962ee9e5" -ForegroundColor White
Write-Host "5. Status should show: Online" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to exit"
