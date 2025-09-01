# RDP Relay Windows Agent - Complete Installation Package

## 🚨 IMPORTANT: Complete Reinstallation Required

This package contains a **corrected version** of the Windows agent that fixes the WebSocket endpoint connection issue. Any previous installation must be completely removed and reinstalled.

## ⚡ Quick Start (Recommended)

### Option 1: Automatic Installation (Easiest)
1. **Double-click**: `complete-reinstall.bat`
2. **Click "Yes"** when prompted for Administrator privileges
3. **Wait** for the installation to complete
4. **Check** the web portal for your agent status

### Option 2: Manual PowerShell
1. **Right-click** on `Complete-Reinstall.ps1`
2. **Select** "Run with PowerShell"
3. **Click "Yes"** when prompted for Administrator privileges

## 🔧 What This Package Does

### Complete Cleanup
- ✅ Stops and removes existing RdpRelayAgent service
- ✅ Deletes all old installation files
- ✅ Cleans up registry and event log entries
- ✅ Removes any leftover configuration

### Fresh Installation  
- ✅ Installs corrected agent binaries
- ✅ Uses correct WebSocket endpoint: `/api/agent`
- ✅ Points to local server: `192.168.18.101`
- ✅ Includes valid provisioning token
- ✅ Configures Windows service to start automatically

## 📋 System Requirements

- **Operating System**: Windows 10/11 or Windows Server 2016+
- **Framework**: .NET 9.0 Runtime (installer will check)
- **Privileges**: Administrator access required
- **Network**: Access to `192.168.18.101` on ports 5001 and 8080

## 🔍 Verification

After installation, use the verification script:
```batch
Check-Status.ps1
```

Or manually check:
1. **Service Status**: `Get-Service RdpRelayAgent`
2. **Web Portal**: http://192.168.18.101:8080
3. **Agent ID**: Look for `68b42ba78f7aa507962ee9e5`

## 📁 Package Contents

```
complete-reinstall.bat          # Easy installer (double-click)
Complete-Reinstall.ps1          # PowerShell installation script  
Check-Status.ps1                # Status verification script
Install-RdpAgent.ps1            # Legacy installer (not recommended)
install.bat                     # Legacy installer (not recommended)
UPDATE_AGENT.md                 # Update instructions
app/                            # Agent application files
  ├── RdpRelay.Agent.Win.exe    # Main service executable
  ├── appsettings.json          # Configuration (pre-configured)
  └── [other binaries]          # Supporting files
```

## 🎯 Expected Results

After successful installation:

### Service Status
```
Name      : RdpRelayAgent
Status    : Running  
StartType : Automatic
```

### Network Connectivity
- ✅ Portal API (8080): Connected
- ✅ Relay Server (5001): Connected

### Web Portal
- Navigate to: http://192.168.18.101:8080
- Login: `test@test.com` / `password`
- Go to **Agents** section
- Agent `68b42ba78f7aa507962ee9e5` should show: **Online** 🟢

## 🛠️ Troubleshooting

### Agent Shows Offline
1. **Check service**: `Get-Service RdpRelayAgent`
2. **Check logs**: `Get-EventLog -LogName Application -Source RdpRelayAgent -Newest 10`
3. **Test connectivity**: `Test-NetConnection 192.168.18.101 -Port 5001`

### Service Won't Start
1. **Check .NET Runtime**: Ensure .NET 9.0 is installed
2. **Check permissions**: Verify the service has proper permissions
3. **Check firewall**: Ensure Windows Firewall allows the connection

### Connection Refused
1. **Server Status**: Verify the Mac server is running
2. **Network**: Ensure both machines are on same network
3. **IP Address**: Confirm Mac server IP is `192.168.18.101`

## 🔄 Management Commands

```powershell
# Service management
Get-Service RdpRelayAgent                    # Check status
Restart-Service RdpRelayAgent               # Restart service
Stop-Service RdpRelayAgent                  # Stop service
Start-Service RdpRelayAgent                 # Start service

# Logs and diagnostics
Get-EventLog -LogName Application -Source RdpRelayAgent -Newest 20
Test-NetConnection 192.168.18.101 -Port 5001
Test-NetConnection 192.168.18.101 -Port 8080

# Complete removal (if needed)
Stop-Service RdpRelayAgent -Force
sc.exe delete RdpRelayAgent
Remove-Item C:\RdpRelay -Recurse -Force
```

## ⚙️ Configuration Details

The agent is pre-configured with:

- **Relay URL**: `ws://192.168.18.101:5001/api/agent` ✅
- **Portal API**: `http://192.168.18.101:8080/api` ✅  
- **Agent ID**: `68b42ba78f7aa507962ee9e5`
- **Token**: Pre-installed and valid until August 2026

## 🆘 Support

If you encounter issues:

1. **Run Status Check**: Execute `Check-Status.ps1`
2. **Check Server Logs**: On Mac, check Docker container logs
3. **Network Issues**: Verify both machines can ping each other
4. **Firewall**: Check Windows Firewall and any antivirus software

---

**Last Updated**: August 31, 2025  
**Version**: 2.0 (Fixed WebSocket endpoint)  
**Configuration**: Local server (192.168.18.101)
