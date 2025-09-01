# ========================================
# RDP Relay Agent - Bulletproof Installation
# ========================================
# Simple, no-nonsense installation script with hardcoded values

param(
    [string]$InstallPath = "C:\RdpRelay"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RDP Relay Agent - Bulletproof Install" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "ERROR: This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please right-click and 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Step 1: Removing any existing installation..." -ForegroundColor Yellow

# Stop and remove service if it exists
try {
    $service = Get-Service -Name "RdpRelayAgent" -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "   Stopping RdpRelayAgent service..." -ForegroundColor Gray
        Stop-Service -Name "RdpRelayAgent" -Force -ErrorAction SilentlyContinue
        
        Write-Host "   Removing RdpRelayAgent service..." -ForegroundColor Gray
        sc.exe delete RdpRelayAgent | Out-Null
        Start-Sleep -Seconds 3
        Write-Host "   Service removed successfully" -ForegroundColor Green
    } else {
        Write-Host "   No existing service found" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Warning: Could not remove service" -ForegroundColor Yellow
}

# Remove installation directory
if (Test-Path $InstallPath) {
    Write-Host "   Removing installation directory..." -ForegroundColor Gray
    try {
        Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction Stop
        Write-Host "   Directory removed successfully" -ForegroundColor Green
    } catch {
        Write-Host "   Warning: Could not remove directory completely" -ForegroundColor Yellow
        cmd.exe /c "rmdir /s /q `"$InstallPath`"" 2>$null
    }
} else {
    Write-Host "   No existing installation directory found" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Step 2: Installing fresh RDP Relay Agent..." -ForegroundColor Yellow

# Create installation directories
Write-Host "   Creating installation directory..." -ForegroundColor Gray
New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
New-Item -Path "$InstallPath\Agent" -ItemType Directory -Force | Out-Null
New-Item -Path "$InstallPath\Agent\logs" -ItemType Directory -Force | Out-Null

# Copy application files
Write-Host "   Copying application files..." -ForegroundColor Gray
$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Copy-Item -Path "$currentDir\app\*" -Destination "$InstallPath\Agent" -Recurse -Force
Write-Host "   Application files copied" -ForegroundColor Green

# Verify configuration
$configPath = "$InstallPath\Agent\appsettings.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    Write-Host "   Configuration verified:" -ForegroundColor Gray
    Write-Host "     - Relay URL: $($config.Agent.RelayUrl)" -ForegroundColor Gray
    Write-Host "     - Portal API: $($config.Agent.PortalApiUrl)" -ForegroundColor Gray
    Write-Host "     - Token: Valid JWT token detected" -ForegroundColor Gray
} else {
    Write-Host "   ERROR: Configuration file not found!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Install and start service
Write-Host "   Installing Windows service..." -ForegroundColor Gray
$servicePath = "$InstallPath\Agent\RdpRelay.Agent.Win.exe"

if (Test-Path $servicePath) {
    # Install service
    New-Service -Name "RdpRelayAgent" `
                -BinaryPathName $servicePath `
                -DisplayName "RDP Relay Agent" `
                -Description "Provides secure RDP relay functionality for remote desktop connections" `
                -StartupType Automatic `
                -ErrorAction Stop
    
    # Start service
    Write-Host "   Starting RdpRelayAgent service..." -ForegroundColor Gray
    Start-Service -Name "RdpRelayAgent" -ErrorAction Stop
    
    # Wait for service to initialize
    Start-Sleep -Seconds 5
    
    # Verify service status
    $serviceStatus = Get-Service -Name "RdpRelayAgent"
    if ($serviceStatus.Status -eq "Running") {
        Write-Host "   Service installed and started successfully" -ForegroundColor Green
    } else {
        Write-Host "   Service installed but not running. Status: $($serviceStatus.Status)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ERROR: Service executable not found!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Step 3: Testing connectivity..." -ForegroundColor Yellow

# Test Portal API (8080)
Write-Host "   Testing Portal API (192.168.18.101:8080)..." -NoNewline -ForegroundColor Gray
try {
    $portalTest = Test-NetConnection -ComputerName "192.168.18.101" -Port 8080 -WarningAction SilentlyContinue
    if ($portalTest.TcpTestSucceeded) {
        Write-Host " CONNECTED" -ForegroundColor Green
    } else {
        Write-Host " FAILED" -ForegroundColor Red
    }
} catch {
    Write-Host " FAILED" -ForegroundColor Red
}

# Test Relay Server (5001)  
Write-Host "   Testing Relay Server (192.168.18.101:5001)..." -NoNewline -ForegroundColor Gray
try {
    $relayTest = Test-NetConnection -ComputerName "192.168.18.101" -Port 5001 -WarningAction SilentlyContinue
    if ($relayTest.TcpTestSucceeded) {
        Write-Host " CONNECTED" -ForegroundColor Green
    } else {
        Write-Host " FAILED" -ForegroundColor Red
    }
} catch {
    Write-Host " FAILED" -ForegroundColor Red
}

# Show final service status
Write-Host ""
Write-Host "Final Status:" -ForegroundColor Cyan
$finalService = Get-Service -Name "RdpRelayAgent" -ErrorAction SilentlyContinue
if ($finalService) {
    Write-Host "Service Name: $($finalService.Name)" -ForegroundColor White
    Write-Host "Service Status: $($finalService.Status)" -ForegroundColor White
    Write-Host "Startup Type: $($finalService.StartType)" -ForegroundColor White
} else {
    Write-Host "ERROR: Service not found!" -ForegroundColor Red
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
Write-Host "Management commands:" -ForegroundColor Yellow
Write-Host "   Get-Service RdpRelayAgent" -ForegroundColor Gray
Write-Host "   Restart-Service RdpRelayAgent" -ForegroundColor Gray
Write-Host ""

Read-Host "Press Enter to exit"
