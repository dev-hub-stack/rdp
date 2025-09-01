# RDP Relay Platform - macOS Testing Guide

## Quick Start for Mac Users

### 1. Prerequisites Setup

```bash
# Install Docker Desktop for Mac (if not already installed)
# Download from: https://www.docker.com/products/docker-desktop/

# Verify installation
docker --version
docker-compose --version
```

### 2. Start Testing

```bash
cd /Users/clustox_1/Documents/Network/rdp-relay

# Use the interactive testing script
./test-platform-mac.sh
```

### 3. What This System Does

Your RDP Relay platform enables:
- **Secure RDP access** through firewalls using only HTTPS (port 443)
- **Multi-tenant management** of Windows machines and users
- **Web-based administration** for IT teams
- **Audit logging** of all remote connections

```
┌─────────────┐    HTTPS/443    ┌──────────────┐    WebSocket    ┌─────────────┐    RDP/3389    ┌─────────────┐
│   RDP       │ ────────────────▶│    Relay     │ ───────────────▶│   Windows   │ ──────────────▶│   Target    │
│   Client    │                  │   Server     │                 │    Agent    │                │   Machine   │
│  (Mac/Win)  │                  │              │                 │             │                │             │
└─────────────┘                  └──────────────┘                 └─────────────┘                └─────────────┘
                                          ▲
                                          │ HTTPS/API
                                          ▼
                                 ┌──────────────┐
                                 │   Portal     │
                                 │   Web UI     │
                                 │              │
                                 └──────────────┘
```

### 4. Testing Scenarios on Mac

#### Scenario A: Complete Local Testing
```bash
# 1. Start platform locally
./test-platform-mac.sh
# Choose option 2: Start Platform

# 2. Test web interface
# Choose option 4: Test Web Portal
# Browser opens to https://localhost

# 3. Login credentials:
# Email: admin@example.com
# Password: SecurePassword123!
```

#### Scenario B: Windows VM Testing
If you have Parallels, VMware, or VirtualBox with Windows:

```bash
# 1. Start platform
./test-platform-mac.sh

# 2. Generate Windows agent
# Choose option 6: Generate Windows Agent

# 3. In Windows VM:
# - Copy agent files from ./build/agent-win/
# - Run install-agent.ps1 as Administrator
# - Use provisioning token from web portal

# 4. Test RDP connection from Mac to Windows VM
# - Install Microsoft Remote Desktop from App Store
# - Create session in web portal
# - Connect using session details
```

#### Scenario C: Remote Windows Machine Testing
For testing with actual remote Windows machines:

```bash
# 1. Make your Mac accessible from internet
# Option A: Use ngrok for testing
brew install ngrok
ngrok http 443  # Exposes your local server

# Option B: Deploy to cloud (AWS/Azure/GCP)
# Use provided docker-compose.yml on cloud instance

# 2. Deploy Windows agent on remote machine
# Use generated installer with your public URL
```

### 5. Mac-Specific RDP Clients

#### Microsoft Remote Desktop (Recommended)
- **Install**: Mac App Store (free)
- **Features**: Full RDP support, multiple sessions, file transfer
- **Connection**: Use relay server address and session code

#### Alternative Clients
```bash
# Royal TSX (paid, very feature-rich)
brew install --cask royal-tsx

# Jump Desktop (paid, cross-platform)
# Available on Mac App Store

# Built-in Screen Sharing (basic RDP support)
# Applications > Utilities > Screen Sharing
```

### 6. Development Testing Workflow

```bash
# Daily development testing routine:

# 1. Start services
./test-platform-mac.sh
# Choose: Start Platform

# 2. Check web portal
open https://localhost

# 3. Check API
curl -k https://localhost/api/health

# 4. View logs
./test-platform-mac.sh
# Choose: Show Logs

# 5. Stop when done
./test-platform-mac.sh  
# Choose: Stop Platform
```

### 7. Troubleshooting on Mac

#### Docker Issues
```bash
# Restart Docker Desktop
killall Docker && open /Applications/Docker.app

# Reset Docker (if needed)
docker system prune -a
```

#### SSL Certificate Issues
```bash
# Add self-signed cert to Keychain (optional)
# This stops browser warnings
security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ./infra/certs/cert.pem
```

#### Port Conflicts
```bash
# Check what's using ports 80/443/5000/5001
sudo lsof -i :443
sudo lsof -i :5000
sudo lsof -i :5001

# Kill conflicting processes if needed
sudo kill -9 <PID>
```

### 8. Real-World Testing Scenarios

#### IT Support Use Case
1. **Setup**: Company Windows machines with agents installed
2. **Workflow**: 
   - Help desk creates session for user's machine
   - Provides connection details to technician
   - Technician connects via RDP from any location
   - All activity logged and auditable

#### Remote Work Use Case  
1. **Setup**: Employee's home Windows machine
2. **Workflow**:
   - Employee requests access via portal
   - Manager approves (if approval workflow enabled)
   - Secure connection through corporate firewall
   - Time-limited sessions with automatic logout

#### Multi-Tenant SaaS Use Case
1. **Setup**: MSP managing multiple client environments
2. **Workflow**:
   - Each client has isolated tenant
   - Client IT staff can only access their machines
   - MSP admin can oversee all tenants
   - Billing based on usage/sessions

### 9. Success Metrics

✅ **Basic Functionality**
- [ ] Platform starts without errors
- [ ] Web portal accessible and responsive  
- [ ] Windows agent connects and stays online
- [ ] RDP sessions can be created and used
- [ ] Multi-user access works correctly

✅ **Security & Compliance**
- [ ] All traffic encrypted (TLS 1.2+)
- [ ] JWT tokens expire appropriately
- [ ] Audit logs capture all activities
- [ ] User permissions enforced
- [ ] Sessions automatically timeout

✅ **Performance & Scale**
- [ ] Multiple concurrent sessions
- [ ] Low latency RDP experience
- [ ] Stable under load
- [ ] Quick agent reconnection
- [ ] Efficient resource usage

This testing approach on Mac gives you a complete validation of your RDP Relay platform before deploying to production environments.
