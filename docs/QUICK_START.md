# ⚡ Quick Start Guide - RDP Relay System

Get the RDP Relay system up and running in minutes!

## 🚀 Local Development (5 minutes)

### Prerequisites Check
```bash
# Verify Docker is installed
docker --version
docker-compose --version

# If not installed:
# - Install Docker Desktop from https://docker.com/products/docker-desktop
```

### One-Command Setup
```bash
# Clone and start
git clone <repository-url>
cd rdp-relay
chmod +x *.sh
./deploy.sh
```

### Access Your System
- **Web Portal**: http://localhost:8080
- **Login**: admin@rdprelay.local / admin123

**That's it! Your system is running locally.**

---

## 🌐 Production Deployment (10 minutes)

### For Ubuntu/Debian Server

```bash
# 1. Prepare server (run on server)
curl -fsSL https://get.docker.com | sh
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# 2. Upload and deploy (run on local machine)
rsync -avz --exclude='.git' . user@your-server:/opt/rdp-relay/
ssh user@your-server
cd /opt/rdp-relay
chmod +x *.sh
./deploy.sh
```

### Access Production System
- **Web Portal**: http://your-server:8080
- **Login**: admin@rdprelay.local / admin123

---

## 🎯 Quick Test Checklist

After deployment, verify these work:

1. ✅ **Web Access**: Open http://localhost:8080 (or your-server:8080)
2. ✅ **Login**: Use admin@rdprelay.local / admin123
3. ✅ **Generate Token**: Click "Generate Token" button on Agents page
4. ✅ **Add Agent**: Click "Add Agent" and fill the form
5. ✅ **View Dashboard**: Check system statistics

---

## 💻 Windows Agent Setup (2 minutes)

### Quick Agent Deployment

1. **Get Token**: Generate from web portal (Agents → Generate Token)
2. **Install .NET**: `winget install Microsoft.DotNet.Runtime.8`
3. **Deploy Agent**:
   ```powershell
   # Copy agent files to C:\RdpRelayAgent\
   # Edit appsettings.json with your server details
   # Install as service:
   sc create "RDP Relay Agent" binPath="C:\RdpRelayAgent\RdpRelay.Agent.Win.exe"
   sc start "RDP Relay Agent"
   ```

---

## 🐛 Quick Troubleshooting

### System Won't Start?
```bash
# Check what's wrong
docker-compose logs

# Common fixes
docker-compose down && docker-compose up -d
sudo systemctl restart docker
```

### Can't Access Web Portal?
```bash
# Check if running
docker-compose ps

# Check ports
netstat -tlnp | grep 8080

# Access directly
curl http://localhost:8080
```

### Agent Won't Connect?
```powershell
# Test connectivity
Test-NetConnection your-server -Port 9443
Test-NetConnection your-server -Port 5000

# Check agent logs
Get-EventLog -LogName Application -Source "RDP Relay Agent" -Newest 10
```

---

## 🔧 Common Commands

```bash
# Start system
./deploy.sh

# Stop system
docker-compose down

# Restart system
docker-compose restart

# View logs
docker-compose logs -f

# Check status
docker-compose ps

# Update and restart
git pull && docker-compose up -d --build
```

---

## 📱 Mobile-Friendly Access

The web portal works great on mobile devices too! Just open:
- `http://your-server:8080` on any mobile browser

---

## 🎉 You're Ready!

That's it! Your RDP Relay system is now ready for:
- ✅ Managing remote Windows machines
- ✅ Creating RDP sessions
- ✅ Monitoring system status
- ✅ Multi-tenant operations

For detailed configuration and advanced features, see the complete [System Setup Guide](SYSTEM_SETUP_GUIDE.md).
