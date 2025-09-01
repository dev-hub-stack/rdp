# ========================================
# RDP Relay Agent - Complete Reinstallation Script
# ========================================
# This script completely removes any existing installation and installs fresh

param(
    [string]$InstallPath = "C:\RdpRelay",
    [switch]$Force = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RDP Relay Agent - Complete Reinstall" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please right-click and 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "🔧 Step 1: Completely removing existing installation..." -ForegroundColor Yellow

# Stop and remove service if it exists
try {
    $service = Get-Service -Name "RdpRelayAgent" -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "   • Stopping RdpRelayAgent service..." -ForegroundColor Gray
        Stop-Service -Name "RdpRelayAgent" -Force -ErrorAction SilentlyContinue
        
        Write-Host "   • Removing RdpRelayAgent service..." -ForegroundColor Gray
        sc.exe delete RdpRelayAgent | Out-Null
        
        # Wait for service to be fully removed
        Start-Sleep -Seconds 3
        Write-Host "   • Service removed successfully" -ForegroundColor Green
    } else {
        Write-Host "   • No existing service found" -ForegroundColor Gray
    }
} catch {
    Write-Host "   • Warning: Could not remove service: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Remove installation directory
if (Test-Path $InstallPath) {
    Write-Host "   • Removing installation directory: $InstallPath" -ForegroundColor Gray
    try {
        Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction Stop
        Write-Host "   • Directory removed successfully" -ForegroundColor Green
    } catch {
        Write-Host "   • Warning: Could not remove directory: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "   • Trying to force removal..." -ForegroundColor Gray
        
        # Try alternative removal methods
        cmd.exe /c "rmdir /s /q `"$InstallPath`"" 2>$null
        if (Test-Path $InstallPath) {
            Write-Host "   • Could not completely remove directory. Continuing anyway..." -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   • No existing installation directory found" -ForegroundColor Gray
}

# Clean up any leftover registry entries or event log sources
try {
    Remove-EventLog -Source "RdpRelayAgent" -ErrorAction SilentlyContinue
    Write-Host "   • Cleaned up event log sources" -ForegroundColor Gray
} catch {
    # Ignore errors - event log source might not exist
}

Write-Host ""
Write-Host "🚀 Step 2: Installing fresh RDP Relay Agent..." -ForegroundColor Yellow

# Create installation directories
Write-Host "   • Creating installation directory: $InstallPath" -ForegroundColor Gray
New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
New-Item -Path "$InstallPath\Agent" -ItemType Directory -Force | Out-Null
New-Item -Path "$InstallPath\Agent\logs" -ItemType Directory -Force | Out-Null

# Copy application files
Write-Host "   • Copying application files..." -ForegroundColor Gray
$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Copy-Item -Path "$currentDir\app\*" -Destination "$InstallPath\Agent" -Recurse -Force

# Verify configuration
$configPath = "$InstallPath\Agent\appsettings.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    Write-Host "   • Configuration verified:" -ForegroundColor Gray
    Write-Host "     - Relay URL: $($config.Agent.RelayUrl)" -ForegroundColor Gray
    Write-Host "     - Portal API: $($config.Agent.PortalApiUrl)" -ForegroundColor Gray
    Write-Host "     - Token: $(if($config.Agent.ProvisioningToken.StartsWith('eyJ')){'✅ Valid'}else{'❌ Needs replacement'})" -ForegroundColor Gray
} else {
    Write-Host "   • ❌ Configuration file not found!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Install and start service
Write-Host "   • Installing Windows service..." -ForegroundColor Gray
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
    Write-Host "   • Starting RdpRelayAgent service..." -ForegroundColor Gray
    Start-Service -Name "RdpRelayAgent" -ErrorAction Stop
    
    # Wait a moment for service to initialize
    Start-Sleep -Seconds 5
    
    # Verify service status
    $serviceStatus = Get-Service -Name "RdpRelayAgent"
    if ($serviceStatus.Status -eq "Running") {
        Write-Host "   • ✅ Service installed and started successfully" -ForegroundColor Green
    } else {
        Write-Host "   • ⚠️ Service installed but not running. Status: $($serviceStatus.Status)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   • ❌ Service executable not found at: $servicePath" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "🧪 Step 3: Testing connectivity..." -ForegroundColor Yellow

# Test network connectivity
$relayHost = "192.168.18.101"
$portalPort = 8080
$relayPort = 5001

Write-Host ("   • Testing Portal API (" + $relayHost + ":" + $portalPort + ")...") -ForegroundColor Gray -NoNewline
try {
    $portalTest = Test-NetConnection -ComputerName $relayHost -Port $portalPort -WarningAction SilentlyContinue
    if ($portalTest.TcpTestSucceeded) {
        Write-Host " ✅ Connected" -ForegroundColor Green
    } else {
        Write-Host " ❌ Failed" -ForegroundColor Red
    }
} catch {
    Write-Host " ❌ Failed" -ForegroundColor Red
}

Write-Host ("   • Testing Relay Server (" + $relayHost + ":" + $relayPort + ")...") -ForegroundColor Gray -NoNewline
try {
    $relayTest = Test-NetConnection -ComputerName $relayHost -Port $relayPort -WarningAction SilentlyContinue
    if ($relayTest.TcpTestSucceeded) {
        Write-Host " ✅ Connected" -ForegroundColor Green
    } else {
        Write-Host " ❌ Failed" -ForegroundColor Red
    }
} catch {
    Write-Host " ❌ Failed" -ForegroundColor Red
}

# Show final service status
Write-Host ""
Write-Host "📊 Final Status:" -ForegroundColor Cyan
$finalService = Get-Service -Name "RdpRelayAgent" -ErrorAction SilentlyContinue
if ($finalService) {
    $finalService | Format-List Name, Status, StartType | Out-String | Write-Host -ForegroundColor White
} else {
    Write-Host "❌ Service not found!" -ForegroundColor Red
}

Write-Host ""
Write-Host "✅ Installation completed!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Next steps:" -ForegroundColor Yellow
Write-Host "1. Open browser: http://192.168.18.101:8080" -ForegroundColor White
Write-Host "2. Login: test@test.com / password" -ForegroundColor White
Write-Host "3. Go to Agents section" -ForegroundColor White
Write-Host "4. Look for agent: 68b42ba78f7aa507962ee9e5" -ForegroundColor White
Write-Host "5. Status should show: Online 🟢" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Management commands:" -ForegroundColor Yellow
Write-Host "   Get-Service RdpRelayAgent" -ForegroundColor Gray
Write-Host "   Restart-Service RdpRelayAgent" -ForegroundColor Gray
Write-Host "   Get-EventLog -LogName Application -Source RdpRelayAgent -Newest 10" -ForegroundColor Gray
Write-Host ""

Read-Host "Press Enter to exit"
