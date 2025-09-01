# 🖥️ RDP Relay Platform - Complete System Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Component Details](#component-details)
5. [Database Schema](#database-schema)
6. [API Reference](#api-reference)
7. [Authentication & Security](#authentication--security)
8. [Deployment Guide](#deployment-guide)
9. [Configuration](#configuration)
10. [Troubleshooting](#troubleshooting)

---

## 1. System Overview

### What is RDP Relay?
RDP Relay is a **secure, web-based remote desktop access platform** that allows users to access Windows machines remotely through a web browser without requiring VPN connections or direct RDP exposure to the internet.

### Key Features
- ✅ **Web-based Access**: Access remote desktops through a web browser
- ✅ **Secure Tunneling**: All RDP traffic is tunneled through secure WebSocket connections
- ✅ **Multi-tenant**: Support for multiple organizations with isolated environments
- ✅ **User Management**: Role-based access control (SystemAdmin, TenantAdmin, Operator)
- ✅ **Agent-based**: Lightweight Windows agents that register with the relay server
- ✅ **Session Management**: Track and monitor all remote desktop sessions
- ✅ **Real-time Monitoring**: Live status of agents and active sessions

### How It Works
1. **Windows Agent** runs on target machines and connects to the Relay Server
2. **Web Portal** allows users to browse available machines and initiate connections
3. **Relay Server** brokers the connection between the web client and Windows agent
4. **RDP traffic** is tunneled securely through WebSocket connections
5. **No firewall changes** needed on client networks - agents connect outbound only

---

## 2. Architecture

### High-Level Architecture
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Web Browser   │    │   Relay Server   │    │  Windows Agent  │
│  (User Client)  │◄──►│  (WebSocket Hub) │◄──►│ (Target Machine)│
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                        │
         ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   Web Portal    │    │   Portal API     │
│   (Frontend)    │◄──►│   (Backend)      │
└─────────────────┘    └──────────────────┘
         │                        │
         ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│     Nginx       │    │    MongoDB       │
│  (Reverse Proxy)│    │   (Database)     │
└─────────────────┘    └──────────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │      Redis       │
                       │     (Cache)      │
                       └──────────────────┘
```

### Component Interaction Flow
1. **User Authentication**: Web Portal → Portal API → MongoDB
2. **Agent Registration**: Windows Agent → Relay Server → Portal API → MongoDB
3. **Session Initiation**: Web Portal → Portal API → Relay Server → Windows Agent
4. **RDP Data Flow**: Web Browser ↔ Nginx ↔ Relay Server ↔ Windows Agent ↔ RDP Service

---

## 3. Technology Stack

### Frontend (Portal Web)
- **Framework**: React 18 with TypeScript
- **UI Library**: Material-UI (MUI) v5
- **Build Tool**: Vite
- **State Management**: Zustand
- **HTTP Client**: Axios
- **Routing**: React Router v6
- **Real-time**: WebSocket client for live updates

### Backend API (Portal API)
- **Runtime**: .NET 9.0
- **Framework**: ASP.NET Core Web API
- **Authentication**: JWT Bearer tokens
- **Database ORM**: MongoDB Driver for .NET
- **Logging**: Serilog
- **Validation**: Data Annotations
- **CORS**: Configured for cross-origin requests

### Relay Server
- **Runtime**: .NET 9.0
- **Framework**: ASP.NET Core with WebSocket support
- **Protocol**: Custom WebSocket-based RDP tunneling
- **Encryption**: TLS/SSL for all connections
- **Load Balancing**: Ready for horizontal scaling

### Windows Agent
- **Runtime**: .NET 9.0
- **Type**: Windows Service / Console Application
- **RDP Integration**: Native Windows RDP APIs
- **Communication**: WebSocket client with auto-reconnection
- **Security**: Certificate-based authentication

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Reverse Proxy**: Nginx with SSL termination
- **Database**: MongoDB 7.0 with replica set support
- **Cache**: Redis 7.2 for session storage
- **SSL/TLS**: OpenSSL certificates for secure communication

### DevOps & Monitoring
- **Container Orchestration**: Docker Compose (Production: Kubernetes ready)
- **Logging**: Centralized logging with Serilog
- **Health Checks**: Built-in health endpoints
- **Monitoring**: Ready for Prometheus/Grafana integration

---

## 4. Component Details

### 4.1 Portal Web (Frontend)
**Location**: `/portal-web/`
**Port**: 3000 (internal), 8080 (via nginx)

**Key Features**:
- Material Design responsive UI
- Real-time agent status updates
- Session management dashboard
- User administration (for admins)
- Multi-tenant organization support

**Key Files**:
```
src/
├── components/           # Reusable UI components
├── pages/               # Route-specific pages
│   ├── auth/           # Login/logout pages
│   ├── dashboard/      # Main dashboard
│   ├── agents/         # Agent management
│   ├── sessions/       # Session monitoring
│   └── users/          # User management
├── services/           # API client services
├── stores/             # State management
├── types/              # TypeScript type definitions
└── utils/              # Helper functions
```

### 4.2 Portal API (Backend)
**Location**: `/portal-api/`
**Port**: 5000 (internal), 8080/api/ (via nginx)

**Key Features**:
- RESTful API design
- JWT-based authentication
- Role-based authorization
- MongoDB integration
- Real-time WebSocket events
- Comprehensive logging

**Key Controllers**:
- `AuthController`: Login, logout, token refresh
- `UsersController`: User management operations
- `TenantsController`: Organization management
- `AgentsController`: Agent registration & status
- `SessionsController`: Session management & monitoring

**Key Services**:
```
Services/
├── UserService.cs          # User CRUD operations
├── TenantService.cs        # Tenant management
├── AgentService.cs         # Agent lifecycle
├── SessionService.cs      # Session tracking
├── JwtService.cs          # Token management
└── MongoDbService.cs      # Database abstraction
```

### 4.3 Relay Server
**Location**: `/relay/`
**Port**: 5001 (HTTP), 9443 (HTTPS)

**Key Features**:
- WebSocket hub for agent connections
- RDP protocol tunneling
- Session state management
- Load balancing support
- Certificate-based security

**Core Components**:
```
Services/
├── AgentHub.cs            # WebSocket hub for agents
├── SessionManager.cs      # Active session tracking
├── RdpTunnelService.cs   # RDP protocol handling
└── CertificateService.cs  # SSL/TLS management
```

### 4.4 Windows Agent
**Location**: `/agent-win/`
**Deployment**: Windows Service

**Key Features**:
- Auto-registration with relay server
- RDP session management
- System information reporting
- Secure WebSocket communication
- Auto-reconnection with backoff

**Core Services**:
```
Services/
├── AgentService.cs           # Main service coordinator
├── RelayWebSocketClient.cs  # Server communication
├── RdpConnectionManager.cs  # RDP session handling
└── SystemInfoService.cs     # Hardware/OS reporting
```

---

## 5. Database Schema

### MongoDB Collections

#### 5.1 Tenants Collection
```javascript
{
  _id: ObjectId,
  name: String,                    // Organization name
  domain: String,                  // Unique domain identifier
  isActive: Boolean,
  createdAt: Date,
  updatedAt: Date,
  settings: {
    maxAgents: Number,             // Agent limit
    maxConcurrentSessions: Number, // Session limit
    sessionTimeoutMinutes: Number, // Auto-disconnect timeout
    requireTls: Boolean,           // Force SSL/TLS
    allowedIpRanges: [String]      // CIDR blocks
  }
}
```

#### 5.2 Users Collection
```javascript
{
  _id: ObjectId,
  tenantId: ObjectId,              // Reference to tenant
  email: String,                   // Unique login email
  firstName: String,
  lastName: String,
  passwordHash: String,            // bcrypt hashed password
  role: String,                    // SystemAdmin|TenantAdmin|Operator
  isActive: Boolean,
  createdAt: Date,
  updatedAt: Date,
  lastLoginAt: Date
}
```

#### 5.3 Agents Collection
```javascript
{
  _id: ObjectId,
  tenantId: ObjectId,              // Reference to tenant
  machineName: String,             // Windows computer name
  ipAddress: String,               // Local IP address
  publicKey: String,               // Agent's public key
  status: String,                  // Online|Offline|InSession|Error
  lastHeartbeat: Date,             // Last ping received
  createdAt: Date,
  systemInfo: {
    osVersion: String,             // Windows version
    cpuCores: Number,
    totalRam: Number,              // RAM in MB
    availableRam: Number,
    diskSpace: Number              // Free disk in GB
  }
}
```

#### 5.4 Sessions Collection
```javascript
{
  _id: ObjectId,
  tenantId: ObjectId,              // Reference to tenant
  userId: ObjectId,                // Reference to user
  agentId: ObjectId,               // Reference to agent
  status: String,                  // Pending|Active|Ended|Error
  connectCode: String,             // Unique 6-digit connect code
  startedAt: Date,
  endedAt: Date,
  duration: Number,                // Session length in seconds
  disconnectReason: String         // Normal|Timeout|Error|UserRequest
}
```

### Database Indexes
```javascript
// Performance-critical indexes
db.users.createIndex({ "email": 1 }, { unique: true })
db.users.createIndex({ "tenantId": 1 })
db.tenants.createIndex({ "domain": 1 }, { unique: true })
db.agents.createIndex({ "tenantId": 1, "machineName": 1 })
db.sessions.createIndex({ "userId": 1, "status": 1 })
db.sessions.createIndex({ "connectCode": 1 }, { sparse: true })
```

---

## 6. API Reference

### 6.1 Authentication Endpoints

#### POST /api/auth/login
**Description**: Authenticate user and receive JWT token
**Body**:
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```
**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpc...",
  "expiresAt": "2025-08-27T12:00:00Z",
  "user": {
    "id": "64f...",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "SystemAdmin",
    "tenantId": "64f..."
  }
}
```

#### POST /api/auth/refresh
**Description**: Refresh JWT token using refresh token
**Body**:
```json
{
  "refreshToken": "dGhpcyBpc..."
}
```

#### GET /api/auth/me
**Description**: Get current user information
**Headers**: `Authorization: Bearer <token>`
**Response**: User object

### 6.2 Agent Management

#### GET /api/agents
**Description**: Get all agents for current tenant
**Query Parameters**:
- `skip` (int): Pagination offset
- `limit` (int): Number of items to return
- `status` (string): Filter by status

#### POST /api/agents/register
**Description**: Register new agent (called by Windows agent)
**Body**:
```json
{
  "machineName": "DESKTOP-ABC123",
  "publicKey": "-----BEGIN PUBLIC KEY-----...",
  "systemInfo": {
    "osVersion": "Windows 11 Pro",
    "cpuCores": 8,
    "totalRam": 16384,
    "availableRam": 12288,
    "diskSpace": 500
  }
}
```

### 6.3 Session Management

#### POST /api/sessions
**Description**: Create new RDP session
**Body**:
```json
{
  "agentId": "64f..."
}
```
**Response**:
```json
{
  "sessionId": "64f...",
  "connectCode": "123456",
  "webSocketUrl": "wss://localhost:9443/sessions/64f..."
}
```

#### GET /api/sessions
**Description**: Get user's sessions
**Query Parameters**:
- `status` (string): Filter by status
- `skip`, `limit`: Pagination

#### DELETE /api/sessions/{id}
**Description**: Terminate active session

### 6.4 User Management (Admin only)

#### GET /api/users
**Description**: Get all users in tenant
#### POST /api/users
**Description**: Create new user
#### PUT /api/users/{id}
**Description**: Update user
#### DELETE /api/users/{id}
**Description**: Delete user

---

## 7. Authentication & Security

### 7.1 JWT Token Structure
```json
{
  "nameid": "user_id",
  "role": "SystemAdmin",
  "tenant_id": "tenant_id",
  "tokenType": "access",
  "nbf": 1756241100,
  "exp": 1756269900,
  "iat": 1756241100,
  "iss": "RdpRelayPortal",
  "aud": "RdpRelayPortal"
}
```

### 7.2 Role-Based Access Control
- **SystemAdmin**: Full platform access, can manage all tenants
- **TenantAdmin**: Manage users and agents within their tenant
- **Operator**: Use agents and view sessions within their tenant

### 7.3 Security Features
- **Password Hashing**: bcrypt with salt rounds = 10
- **JWT Expiration**: Access tokens expire in 8 hours
- **Refresh Tokens**: 30-day expiration with rotation
- **HTTPS Everywhere**: All communication encrypted with TLS 1.2+
- **Certificate Pinning**: Agents validate server certificates
- **Rate Limiting**: API endpoints protected from abuse
- **CORS Protection**: Restricted origins for web requests

### 7.4 Network Security
```
Internet → Nginx (SSL Termination) → Internal Network
                ↓
         Container Network (Docker)
           ↓           ↓         ↓
    Portal-Web  Portal-API  Relay-Server
                    ↓           ↓
                MongoDB    Redis Cache
```

---

## 8. Deployment Guide

### 8.1 Prerequisites
- **Docker** 24.0+ and **Docker Compose** 2.0+
- **2GB RAM** minimum, 4GB recommended
- **10GB disk space** for containers and logs
- **Open ports**: 8080 (web), 8443 (SSL), 9443 (relay SSL)

### 8.2 Environment Configuration
Create `.env` file in project root:
```bash
# MongoDB Configuration
MONGODB_PASSWORD=your_secure_mongodb_password

# Redis Configuration  
REDIS_PASSWORD=your_secure_redis_password

# JWT Configuration
JWT_SECRET_KEY=your-super-secret-jwt-key-at-least-32-characters-long
JWT_ISSUER=RdpRelayPortal
JWT_AUDIENCE=RdpRelayPortal

# Certificate Configuration
CERT_PASSWORD=your_certificate_password

# API URLs (adjust for production)
VITE_API_BASE_URL=https://yourdomain.com
VITE_RELAY_WS_URL=wss://yourdomain.com:9443
```

### 8.3 Deployment Steps

#### Development Deployment
```bash
# Clone repository
git clone <repository-url>
cd rdp-relay

# Create environment file
cp .env.example .env
# Edit .env with your settings

# Start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f
```

#### Production Deployment
```bash
# Use production compose file
docker-compose -f docker-compose.prod.yml up -d

# Set up SSL certificates
./scripts/setup-ssl.sh

# Configure firewall
sudo ufw allow 8080
sudo ufw allow 8443  
sudo ufw allow 9443

# Set up log rotation
./scripts/setup-logging.sh
```

### 8.4 Container Health Checks
All containers include health checks:
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

---

## 9. Configuration

### 9.1 Portal API Configuration
**File**: `portal-api/RdpRelay.Portal.Api/appsettings.json`
```json
{
  "Portal": {
    "RequireHttps": true,
    "CorsOrigins": ["https://yourdomain.com"]
  },
  "Jwt": {
    "Issuer": "RdpRelay.Portal",
    "Audience": "RdpRelay.Users", 
    "AccessTokenExpiryHours": 8,
    "RefreshTokenExpiryDays": 30
  },
  "MongoDb": {
    "ConnectionString": "mongodb://admin:password@mongodb:27017/rdp_relay?authSource=admin",
    "DatabaseName": "rdp_relay"
  }
}
```

### 9.2 Relay Server Configuration
**File**: `relay/RdpRelay.Relay/appsettings.json`
```json
{
  "Relay": {
    "MaxConcurrentSessions": 100,
    "SessionTimeoutMinutes": 480,
    "HeartbeatIntervalSeconds": 30
  },
  "Certificates": {
    "ServerCertPath": "/app/certs/relay.pfx",
    "CertPassword": "certificate_password"
  }
}
```

### 9.3 Nginx Configuration
**File**: `infra/nginx/conf.d/default.conf`
- API routing: `/api/*` → `portal-api:8080`
- WebSocket upgrade support for `/api/sessions/*`
- SSL termination and security headers
- Rate limiting: 10 req/sec for API, 1 req/sec for auth

### 9.4 Windows Agent Configuration  
**File**: `agent-win/RdpRelay.Agent.Win/appsettings.json`
```json
{
  "Agent": {
    "RelayServerUrl": "wss://yourdomain.com:9443",
    "ReconnectIntervalSeconds": 30,
    "HeartbeatIntervalSeconds": 60,
    "MaxReconnectAttempts": 10
  }
}
```

---

## 10. Troubleshooting

### 10.1 Common Issues

#### Container Won't Start
```bash
# Check container logs
docker-compose logs [service-name]

# Check resource usage  
docker stats

# Restart specific service
docker-compose restart [service-name]
```

#### Authentication Failures
```bash
# Check JWT secret configuration
docker exec portal-api env | grep JWT

# Verify database connection
docker exec mongodb mongosh --eval "db.runCommand({ping:1})"

# Check user exists
docker exec mongodb mongosh rdp_relay --eval "db.users.findOne({email: 'test@test.com'})"
```

#### Agent Connection Issues
```bash
# Check relay server logs
docker-compose logs relay-server

# Test WebSocket connectivity
curl -I -H "Connection: Upgrade" -H "Upgrade: websocket" http://localhost:9443

# Verify certificates
openssl s_client -connect localhost:9443 -servername localhost
```

### 10.2 Performance Tuning

#### Database Optimization
```javascript
// Add compound indexes for common queries
db.sessions.createIndex({ "userId": 1, "status": 1, "startedAt": -1 })
db.agents.createIndex({ "tenantId": 1, "status": 1, "lastHeartbeat": -1 })
```

#### Container Resource Limits
```yaml
# docker-compose.yml
services:
  portal-api:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          memory: 256M
```

### 10.3 Monitoring & Logging

#### Log Locations
```bash
# Application logs
./logs/portal-api/
./logs/relay/
./logs/nginx/

# Container logs
docker-compose logs -f --tail=100 [service]

# System logs
journalctl -u docker -f
```

#### Health Check Endpoints
- Portal API: `http://localhost:5000/health`
- Relay Server: `http://localhost:5001/health`
- Portal Web: `http://localhost:3000` (returns 200 OK)

---

## 🚀 Quick Start Summary

1. **Prerequisites**: Install Docker & Docker Compose
2. **Environment**: Create `.env` file with secure passwords
3. **Deploy**: Run `docker-compose up -d`
4. **Access**: Open `http://localhost:8080`
5. **Login**: Use `test@test.com` / `password`
6. **Deploy Agents**: Install Windows agent on target machines
7. **Connect**: Select agent and start RDP session

---

## 📞 Support & Maintenance

### Backup Strategy
```bash
# Database backup
docker exec mongodb mongodump --out /backup/$(date +%Y%m%d)

# Configuration backup  
tar -czf config-backup-$(date +%Y%m%d).tar.gz .env infra/
```

### Update Process
```bash
# Pull latest images
docker-compose pull

# Backup data
./scripts/backup.sh

# Update containers
docker-compose up -d

# Verify health
./scripts/health-check.sh
```

---

*This documentation covers the complete RDP Relay platform. For specific deployment scenarios or advanced configuration, refer to the individual component documentation in the `/docs/` directory.*
