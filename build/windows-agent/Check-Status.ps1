# ========================================
# RDP Relay Agent - Status Checker
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RDP Relay Agent - Status Check" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check service status
Write-Host "🔍 Service Status:" -ForegroundColor Yellow
try {
    $service = Get-Service -Name "RdpRelayAgent" -ErrorAction Stop
    $service | Format-List Name, Status, StartType | Out-String | Write-Host -ForegroundColor White
    
    if ($service.Status -eq "Running") {
        Write-Host "✅ Service is running correctly" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Service is not running!" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ RdpRelayAgent service not found!" -ForegroundColor Red
}

Write-Host ""

# Check network connectivity
Write-Host "🌐 Network Connectivity:" -ForegroundColor Yellow
$relayHost = "192.168.18.101"

Write-Host "   Portal API (8080)..." -NoNewline -ForegroundColor Gray
try {
    $portalTest = Test-NetConnection -ComputerName $relayHost -Port 8080 -WarningAction SilentlyContinue
    if ($portalTest.TcpTestSucceeded) {
        Write-Host " ✅ Connected" -ForegroundColor Green
    } else {
        Write-Host " ❌ Failed" -ForegroundColor Red
    }
} catch {
    Write-Host " ❌ Failed" -ForegroundColor Red
}

Write-Host "   Relay Server (5001)..." -NoNewline -ForegroundColor Gray
try {
    $relayTest = Test-NetConnection -ComputerName $relayHost -Port 5001 -WarningAction SilentlyContinue
    if ($relayTest.TcpTestSucceeded) {
        Write-Host " ✅ Connected" -ForegroundColor Green
    } else {
        Write-Host " ❌ Failed" -ForegroundColor Red
    }
} catch {
    Write-Host " ❌ Failed" -ForegroundColor Red
}

Write-Host ""

# Check configuration
Write-Host "⚙️ Configuration:" -ForegroundColor Yellow
$configPath = "C:\RdpRelay\Agent\appsettings.json"
if (Test-Path $configPath) {
    try {
        $config = Get-Content $configPath -Raw | ConvertFrom-Json
        Write-Host "   Relay URL: $($config.Agent.RelayUrl)" -ForegroundColor White
        Write-Host "   Portal API: $($config.Agent.PortalApiUrl)" -ForegroundColor White
        Write-Host "   Token: $(if($config.Agent.ProvisioningToken.Length -gt 50){'✅ Present'}else{'❌ Missing'})" -ForegroundColor White
        
        # Validate URLs
        if ($config.Agent.RelayUrl -like "*192.168.18.101*") {
            Write-Host "   ✅ Relay URL points to local server" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️ Relay URL points to remote server" -ForegroundColor Yellow
        }
        
        if ($config.Agent.RelayUrl -like "*api/agent*") {
            Write-Host "   ✅ Correct WebSocket endpoint (/api/agent)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Wrong WebSocket endpoint (should be /api/agent)" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "   ❌ Could not read configuration: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ❌ Configuration file not found at: $configPath" -ForegroundColor Red
}

Write-Host ""

# Check recent logs
Write-Host "📋 Recent Logs (last 5 entries):" -ForegroundColor Yellow
try {
    $logs = Get-EventLog -LogName Application -Source "RdpRelayAgent" -Newest 5 -ErrorAction SilentlyContinue
    if ($logs) {
        foreach ($log in $logs) {
            $level = switch ($log.EntryType) {
                "Error" { "❌"; $color = "Red" }
                "Warning" { "⚠️"; $color = "Yellow" }
                "Information" { "ℹ️"; $color = "White" }
                default { "📝"; $color = "Gray" }
            }
            Write-Host "   $level [$($log.TimeGenerated.ToString('MM/dd HH:mm:ss'))] $($log.Message)" -ForegroundColor $color
        }
    } else {
        Write-Host "   No recent log entries found" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Could not read event logs: $($_.Exception.Message)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "📱 Web Portal Check:" -ForegroundColor Yellow
Write-Host "   URL: http://192.168.18.101:8080" -ForegroundColor White
Write-Host "   Login: test@test.com / password" -ForegroundColor White
Write-Host "   Agent ID: 68b42ba78f7aa507962ee9e5" -ForegroundColor White
Write-Host ""

Write-Host "🛠️ Management Commands:" -ForegroundColor Yellow
Write-Host "   Restart-Service RdpRelayAgent" -ForegroundColor Gray
Write-Host "   Stop-Service RdpRelayAgent" -ForegroundColor Gray  
Write-Host "   Start-Service RdpRelayAgent" -ForegroundColor Gray
Write-Host ""

Read-Host "Press Enter to exit"
