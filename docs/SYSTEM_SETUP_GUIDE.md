# 🚀 RDP Relay System - Complete Setup Guide

This comprehensive guide will walk you through setting up and running the RDP Relay system from scratch.

## 📋 Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development Setup](#local-development-setup)
3. [Production Deployment](#production-deployment)
4. [Remote Server Deployment](#remote-server-deployment)
5. [Windows Agent Installation](#windows-agent-installation)
6. [System Testing](#system-testing)
7. [Troubleshooting](#troubleshooting)

---

## 🔧 Prerequisites

### System Requirements

**For Development (Local):**
- **Docker Desktop** 4.20+ with Docker Compose
- **Node.js** 18+ with npm/yarn
- **.NET 8 SDK** (optional, for development)
- **Git** for version control
- **Modern web browser** (Chrome, Firefox, Safari, Edge)

**For Production Server:**
- **Ubuntu 20.04+** or **CentOS 7+**
- **4GB RAM** minimum (8GB recommended)
- **20GB disk space** minimum
- **Docker** and **Docker Compose**
- **Open ports**: 22 (SSH), 80 (HTTP), 443 (HTTPS), 5000 (API), 3000 (Web), 9443 (Relay)

**For Windows Agents:**
- **Windows 10/11** or **Windows Server 2019+**
- **.NET 8 Runtime**
- **RDP enabled** on target machines
- **Network connectivity** to relay server

### Network Requirements

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web Browser   │◄──►│  Relay Server   │◄──►│ Windows Agents  │
│ (Port 8080)     │    │ (Ports 5000,    │    │ (Outbound only) │
│                 │    │  9443, 3000)    │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

---

## 🏠 Local Development Setup

### Step 1: Clone and Prepare

```bash
# Clone the repository
git clone <repository-url>
cd rdp-relay

# Make scripts executable
chmod +x *.sh

# Create required directories
mkdir -p data/{mongodb,redis}
mkdir -p logs/{portal-api,relay,nginx}
```

### Step 2: Environment Configuration

Create a `.env` file:

```bash
# Copy example environment
cp .env.example .env

# Edit configuration
nano .env
```

**Recommended `.env` for local development:**
```env
# MongoDB Configuration
MONGODB_PASSWORD=rdp_relay_dev_password
MONGODB_DATABASE=rdp_relay

# Redis Configuration
REDIS_PASSWORD=rdp_relay_redis_dev

# JWT Configuration
JWT_SECRET_KEY=your_super_secret_jwt_key_for_development
JWT_ISSUER=RdpRelayPortal
JWT_AUDIENCE=RdpRelayPortal

# Certificate Configuration
CERT_PASSWORD=rdp_relay_cert_dev

# Portal Web Configuration
VITE_API_BASE_URL=http://localhost:5000
VITE_RELAY_WS_URL=wss://localhost:9443
VITE_APP_TITLE=RDP Relay Portal - Development

# Docker Compose Configuration
COMPOSE_PROJECT_NAME=rdp-relay-dev
COMPOSE_FILE=docker-compose.yml

# Development Configuration
ASPNETCORE_ENVIRONMENT=Development
SERILOG_MINIMUM_LEVEL=Debug
```

### Step 3: Start the System

```bash
# Option 1: Use the deployment script (recommended)
./deploy.sh

# Option 2: Manual Docker Compose
docker-compose up -d --build

# Monitor logs
docker-compose logs -f
```

### Step 4: Verify Local Setup

```bash
# Run the test script
./test-platform.sh

# Check service status
docker-compose ps

# Test API health
curl http://localhost:8080/api/health
```

### Step 5: Access the System

- **🌐 Web Portal**: http://localhost:8080
- **🔧 API Docs**: http://localhost:5000/swagger
- **📊 Direct Web**: http://localhost:3000

**Default Login:**
- Email: `admin@rdprelay.local`
- Password: `admin123`

---

## 🌐 Production Deployment

### Step 1: Production Server Preparation

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo systemctl enable docker
sudo systemctl start docker

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Add current user to docker group (optional)
sudo usermod -aG docker $USER
```

### Step 2: Deploy to Production Server

```bash
# Upload files to server
rsync -avz --exclude='.git' --exclude='node_modules' . user@server:/opt/rdp-relay/

# SSH to server
ssh user@server
cd /opt/rdp-relay

# Create production environment
cat > .env << 'EOF'
# MongoDB Configuration
MONGODB_PASSWORD=$(openssl rand -base64 32)
MONGODB_DATABASE=rdp_relay

# Redis Configuration
REDIS_PASSWORD=$(openssl rand -base64 32)

# JWT Configuration
JWT_SECRET_KEY=$(openssl rand -base64 32)
JWT_ISSUER=RdpRelayPortal
JWT_AUDIENCE=RdpRelayPortal

# Certificate Configuration
CERT_PASSWORD=$(openssl rand -base64 32)

# Portal Web Configuration - Replace SERVER_IP with your server's IP
VITE_API_BASE_URL=http://YOUR_SERVER_IP:5000
VITE_RELAY_WS_URL=wss://YOUR_SERVER_IP:9443
VITE_APP_TITLE=RDP Relay Portal

# Docker Compose Configuration
COMPOSE_PROJECT_NAME=rdp-relay
COMPOSE_FILE=docker-compose.yml

# Production Configuration
ASPNETCORE_ENVIRONMENT=Production
SERILOG_MINIMUM_LEVEL=Information
EOF

# Create required directories
mkdir -p data/{mongodb,redis}
mkdir -p logs/{portal-api,relay,nginx}
mkdir -p infra/certs

# Set permissions
chmod +x *.sh
chown -R $(id -u):$(id -g) data/ logs/

# Deploy the system
./deploy.sh
```

### Step 3: Configure Firewall

```bash
# Ubuntu/Debian
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw allow 8080/tcp  # Web Portal
sudo ufw allow 5000/tcp  # API
sudo ufw allow 9443/tcp  # Relay Server
sudo ufw enable

# CentOS/RHEL
sudo firewall-cmd --permanent --add-port=22/tcp
sudo firewall-cmd --permanent --add-port=80/tcp
sudo firewall-cmd --permanent --add-port=443/tcp
sudo firewall-cmd --permanent --add-port=8080/tcp
sudo firewall-cmd --permanent --add-port=5000/tcp
sudo firewall-cmd --permanent --add-port=9443/tcp
sudo firewall-cmd --reload
```

---

## 🖥️ Remote Server Deployment

### Automated Remote Deployment

For deploying to a remote server with SSH access:

```bash
# Edit the deployment script with your server details
nano deploy-remote.sh

# Update these variables:
REMOTE_SERVER="your.server.ip"
REMOTE_USER="root"
REMOTE_PASSWORD="your_password"

# Run automated deployment
./deploy-remote.sh
```

### Manual Remote Deployment

```bash
# Test connection first
./test-connection.sh

# For manual deployment with password prompts
./deploy-manual.sh
```

The script will automatically:
1. Install Docker and Docker Compose
2. Upload all project files
3. Configure production environment
4. Build and start all services
5. Run health checks

---

## 💻 Windows Agent Installation

### Step 1: Generate Provisioning Token

1. Access the web portal: `http://your-server:8080`
2. Login with admin credentials
3. Go to **Agents** page
4. Click **"Generate Token"**
5. Copy the generated token

### Step 2: Install .NET Runtime on Windows

```powershell
# Download and install .NET 8 Runtime
winget install Microsoft.DotNet.Runtime.8
```

### Step 3: Deploy Windows Agent

```powershell
# Download agent files to Windows machine
# Copy the agent-win folder to C:\RdpRelayAgent\

# Navigate to agent directory
cd C:\RdpRelayAgent\RdpRelay.Agent.Win\

# Configure the agent
notepad appsettings.json
```

**Update `appsettings.json`:**
```json
{
  "AgentConfig": {
    "PortalApiUrl": "http://your-server:5000",
    "RelayServerUrl": "wss://your-server:9443",
    "ProvisioningToken": "YOUR_GENERATED_TOKEN",
    "MachineId": "UNIQUE_MACHINE_IDENTIFIER",
    "HeartbeatIntervalSeconds": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Step 4: Install as Windows Service

```powershell
# Install the agent as a Windows service
sc create "RDP Relay Agent" binPath="C:\RdpRelayAgent\RdpRelay.Agent.Win\RdpRelay.Agent.Win.exe"
sc start "RDP Relay Agent"

# Check service status
sc query "RDP Relay Agent"
```

---

## 🧪 System Testing

### Automated Testing

```bash
# Local testing
./test-platform.sh

# macOS specific testing
./test-platform-mac.sh

# Remote server testing
./test-remote.sh
```

### Manual Testing Checklist

**Web Portal Testing:**
- [ ] Can access web portal at `http://server:8080`
- [ ] Can login with admin credentials
- [ ] Dashboard displays system statistics
- [ ] Can generate provisioning tokens
- [ ] Can add new agents with all required fields
- [ ] Agent list updates correctly

**API Testing:**
- [ ] API health endpoint responds: `/api/health`
- [ ] Authentication endpoints work
- [ ] Agent management endpoints function
- [ ] Session management endpoints respond

**Agent Testing:**
- [ ] Windows agent can connect to relay server
- [ ] Agent appears in portal with "Online" status
- [ ] Heartbeat updates regularly
- [ ] Agent can be managed from portal

**RDP Session Testing:**
- [ ] Can create RDP session from portal
- [ ] Session appears in session list
- [ ] Can connect to remote desktop
- [ ] Session ends properly

### API Testing Examples

```bash
# Test API health
curl -X GET "http://your-server:8080/api/health"

# Login and get token
curl -X POST "http://your-server:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@rdprelay.local","password":"admin123"}'

# Generate provisioning token (replace JWT_TOKEN)
curl -X POST "http://your-server:8080/api/agents/provisioning-token" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"groupId":null}'

# List agents
curl -X GET "http://your-server:8080/api/agents?skip=0&limit=100" \
  -H "Authorization: Bearer JWT_TOKEN"
```

---

## 🐛 Troubleshooting

### Common Issues and Solutions

#### 1. Docker Container Won't Start

```bash
# Check container logs
docker-compose logs [service-name]

# Check system resources
docker system df
free -h

# Restart specific service
docker-compose restart [service-name]

# Full system restart
docker-compose down && docker-compose up -d
```

#### 2. Database Connection Issues

```bash
# Check MongoDB container
docker-compose logs mongodb

# Verify MongoDB is accessible
docker-compose exec mongodb mongosh --username admin --password

# Reset MongoDB data (WARNING: destroys data)
docker-compose down
sudo rm -rf data/mongodb/*
docker-compose up -d
```

#### 3. Port Conflicts

```bash
# Check what's using ports
sudo netstat -tlnp | grep -E ':(3000|5000|8080|9443|27017|6379)'

# Stop conflicting services
sudo systemctl stop apache2  # if using port 80
sudo systemctl stop nginx    # if using port 80

# Use different ports by modifying docker-compose.yml
```

#### 4. SSL/TLS Certificate Issues

```bash
# Regenerate certificates
cd infra/certs
openssl req -new -x509 -keyout relay.key -out relay.crt -days 365 -nodes
openssl pkcs12 -export -out relay.pfx -inkey relay.key -in relay.crt

# Update certificate permissions
chmod 600 relay.key relay.pfx
```

#### 5. Windows Agent Connection Issues

**Check Network Connectivity:**
```powershell
# Test relay server connectivity
Test-NetConnection your-server -Port 9443

# Test API server connectivity
Test-NetConnection your-server -Port 5000
```

**Verify Agent Configuration:**
```powershell
# Check agent logs
Get-EventLog -LogName Application -Source "RDP Relay Agent" -Newest 50

# Restart agent service
Restart-Service "RDP Relay Agent"
```

#### 6. Web Portal Issues

**Clear Browser Cache:**
- Press `Ctrl+Shift+R` (or `Cmd+Shift+R` on Mac) for hard refresh
- Clear browser cache and cookies
- Try incognito/private browsing mode

**Check Console Errors:**
- Open browser developer tools (F12)
- Check Console tab for JavaScript errors
- Check Network tab for failed API requests

### Service Management Commands

```bash
# View all container status
docker-compose ps

# View logs for specific service
docker-compose logs -f portal-api
docker-compose logs -f portal-web
docker-compose logs -f relay-server

# Restart specific service
docker-compose restart portal-api

# Scale services (if needed)
docker-compose up -d --scale portal-api=2

# Update and restart services
git pull
docker-compose up -d --build

# Backup data
sudo tar -czf backup-$(date +%Y%m%d).tar.gz data/

# Restore data (stop containers first)
docker-compose down
sudo tar -xzf backup-YYYYMMDD.tar.gz
docker-compose up -d
```

### Performance Monitoring

```bash
# Monitor container resources
docker stats

# Monitor system resources
htop
iostat 1

# Monitor logs in real-time
tail -f logs/portal-api/*.log
tail -f logs/relay/*.log

# Check disk space
df -h
```

---

## 🎯 Success Criteria

Your RDP Relay system is successfully running when:

1. ✅ All Docker containers are running and healthy
2. ✅ Web portal is accessible and functional
3. ✅ API endpoints respond correctly
4. ✅ Can generate provisioning tokens
5. ✅ Can add agents through the web interface
6. ✅ Windows agents can connect and appear online
7. ✅ Can create and manage RDP sessions
8. ✅ System logs show no critical errors

---

## 📞 Support and Resources

- **Documentation**: Check other files in `/docs` folder
- **API Reference**: Access `/api/swagger` endpoint
- **System Logs**: Located in `/logs` directory
- **Configuration**: Environment variables in `.env` file

For additional help, check the troubleshooting section or review the comprehensive system documentation in this folder.
