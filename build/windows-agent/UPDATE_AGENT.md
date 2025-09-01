# Windows Agent Update Guide

## Issue Fixed
The Windows agent was connecting to the wrong WebSocket endpoint. This has been corrected:

- **Before**: `ws://192.168.18.101:5001/agent/ws` ❌
- **After**: `ws://192.168.18.101:5001/api/agent` ✅

## Update Steps

### Option 1: Update Existing Installation

If you already have the agent installed, copy the corrected `app` folder to your Windows machine and restart the service:

```powershell
# Run as Administrator
# 1. Stop the service
Stop-Service RdpRelayAgent

# 2. Replace the application files
# Copy the new 'app' folder from this package to C:\RdpRelay\Agent\app

# 3. Restart the service
Start-Service RdpRelayAgent

# 4. Check status
Get-Service RdpRelayAgent
```

### Option 2: Fresh Installation

If you want to do a clean install:

```powershell
# Run as Administrator
# 1. Uninstall current service (if exists)
sc.exe delete RdpRelayAgent

# 2. Remove old directory
Remove-Item -Path "C:\RdpRelay" -Recurse -Force -ErrorAction SilentlyContinue

# 3. Run the installer
.\Install-RdpAgent.ps1 -Token "your_token_here"
```

## Verification

After updating, you should see connection attempts to the correct endpoint in the server logs:
- ✅ Connections to `/api/agent` (correct)
- ❌ No more 404 errors for `/agent/ws`

## Quick Test

```powershell
# Test connectivity to the correct endpoints
Test-NetConnection -ComputerName 192.168.18.101 -Port 5001
Test-NetConnection -ComputerName 192.168.18.101 -Port 8080
```

Then check the web portal at http://192.168.18.101:8080 to see if the agent appears as "Online".

---
*Updated: August 31, 2025*
*Fix: Corrected WebSocket endpoint from /agent/ws to /api/agent*
