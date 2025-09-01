# RDP Relay Platform - Comprehensive System Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture & Technology Stack](#architecture--technology-stack)
3. [Component Details](#component-details)
4. [Data Flow & Communication](#data-flow--communication)
5. [Security Implementation](#security-implementation)
6. [Database Schema](#database-schema)
7. [API Documentation](#api-documentation)
8. [Deployment Architecture](#deployment-architecture)
9. [Configuration Management](#configuration-management)
10. [Monitoring & Logging](#monitoring--logging)
11. [Development Workflow](#development-workflow)
12. [Troubleshooting Guide](#troubleshooting-guide)

---

## System Overview

The RDP Relay Platform is a comprehensive **Remote Desktop Protocol (RDP) relay system** that enables secure, centralized management of RDP connections across multiple Windows machines. The platform acts as an intermediary between users and target Windows machines, providing authentication, session management, and secure tunneling capabilities.

### Core Purpose
- **Centralized RDP Management**: Manage multiple Windows machines from a single web interface
- **Secure Access Control**: JWT-based authentication with role-based permissions
- **Session Monitoring**: Track and manage active RDP sessions
- **Multi-Tenant Support**: Support multiple organizations with isolated data
- **Real-time Communication**: WebSocket-based real-time updates and control

### Key Features
- 🌐 **Web-based Interface**: React-based modern UI for RDP management
- 🔒 **Secure Authentication**: JWT tokens with refresh mechanism
- 🏢 **Multi-tenant Architecture**: Support for multiple organizations
- 📊 **Real-time Monitoring**: Live session and agent status updates
- 🔧 **Agent Management**: Deploy and manage Windows agents remotely
- 📋 **Session Management**: Create, monitor, and control RDP sessions
- 🛡️ **Security**: TLS encryption, role-based access, audit logging

---

## Architecture & Technology Stack

### High-Level Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        WB[Web Browser]
        WA[Windows Agent]
    end
    
    subgraph "Proxy Layer"
        NX[Nginx Reverse Proxy]
    end
    
    subgraph "Application Layer"
        API[Portal API<br/>.NET 9]
        WEB[Portal Web<br/>React + Vite]
        REL[Relay Server<br/>.NET 9]
    end
    
    subgraph "Data Layer"
        MG[MongoDB<br/>Database]
        RD[Redis<br/>Cache/Sessions]
    end
    
    subgraph "Target Machines"
        WIN[Windows Machines<br/>RDP Enabled]
    end
    
    WB --> NX
    WA --> REL
    NX --> API
    NX --> WEB
    REL --> WIN
    API --> MG
    API --> RD
    REL --> RD
    
    classDef frontend fill:#e1f5fe
    classDef backend fill:#f3e5f5
    classDef data fill:#e8f5e8
    classDef proxy fill:#fff3e0
    classDef target fill:#ffebee
    
    class WB,WEB frontend
    class API,REL backend
    class MG,RD data
    class NX proxy
    class WA,WIN target
```

### Technology Stack Details

#### Frontend Technologies
- **Framework**: React 18.2.0 with TypeScript
- **Build Tool**: Vite 5.0+ (Modern build tooling)
- **UI Framework**: Material-UI (MUI) v5
- **State Management**: Zustand for global state
- **HTTP Client**: Axios for API communication
- **WebSocket Client**: Native WebSocket API for real-time updates
- **Routing**: React Router v6
- **Development Tools**: 
  - TypeScript for type safety
  - ESLint for code quality
  - Prettier for code formatting

#### Backend Technologies
- **Runtime**: .NET 9.0 (Latest LTS)
- **Framework**: ASP.NET Core Web API
- **Authentication**: JWT Bearer tokens with refresh mechanism
- **WebSocket**: SignalR for real-time communication
- **ORM/Data Access**: MongoDB.Driver (Official MongoDB .NET Driver)
- **Caching**: StackExchange.Redis
- **Logging**: Serilog with structured logging
- **Configuration**: .NET Configuration system with environment variables
- **Security**: BCrypt.Net for password hashing

#### Database Technologies
- **Primary Database**: MongoDB 7.0
  - Document-based NoSQL database
  - Horizontal scaling capabilities
  - Flexible schema for multi-tenant data
- **Caching Layer**: Redis 7.2-Alpine
  - Session storage and caching
  - Real-time data synchronization
  - Pub/Sub for notifications

#### Infrastructure Technologies
- **Containerization**: Docker & Docker Compose
- **Reverse Proxy**: Nginx (Alpine-based)
- **TLS/SSL**: OpenSSL for certificate management
- **Process Management**: Systemd integration for production

#### Development & DevOps
- **Version Control**: Git
- **Container Orchestration**: Docker Compose
- **Environment Management**: .env files
- **Logging**: Centralized logging with Serilog
- **Monitoring**: Built-in health checks

---

## Component Details

### 1. Portal Web (Frontend)

**Location**: `/portal-web/`
**Technology**: React 18 + TypeScript + Vite
**Port**: 3000 (Development), 80 (Production Container)

#### Key Features
- **Modern React Architecture**: Functional components with hooks
- **TypeScript Integration**: Full type safety across the application
- **Material-UI Components**: Professional, accessible UI components
- **Responsive Design**: Mobile-first approach with responsive layouts
- **Real-time Updates**: WebSocket integration for live data
- **Authentication Flow**: JWT token management with auto-refresh

#### Project Structure
```
portal-web/src/
├── components/          # Reusable UI components
│   ├── common/         # Generic components (buttons, forms, etc.)
│   ├── layout/         # Layout components (header, sidebar, etc.)
│   └── modals/         # Modal dialogs
├── pages/              # Page components
│   ├── auth/           # Login, register pages
│   ├── dashboard/      # Dashboard and overview
│   ├── agents/         # Agent management
│   ├── sessions/       # Session management
│   ├── tenants/        # Tenant management (admin only)
│   └── users/          # User management
├── services/           # API client services
│   ├── api.ts          # Base API configuration
│   ├── authApi.ts      # Authentication API calls
│   ├── agentsApi.ts    # Agent management API calls
│   └── ...
├── stores/             # Global state management (Zustand)
├── types/              # TypeScript type definitions
├── hooks/              # Custom React hooks
├── utils/              # Utility functions
└── styles/             # CSS and theme configuration
```

#### Key Components
- **Authentication System**: Login forms, token management, route protection
- **Dashboard**: Overview of system statistics and recent activity
- **Agent Management**: View, configure, and deploy Windows agents
- **Session Management**: Create, monitor, and control RDP sessions
- **User Management**: CRUD operations for users (admin only)
- **Tenant Management**: Multi-tenant configuration (system admin only)

### 2. Portal API (Backend)

**Location**: `/portal-api/RdpRelay.Portal.Api/`
**Technology**: .NET 9 ASP.NET Core Web API
**Port**: 5000 (Direct), 8080 (via Nginx)

#### Key Features
- **RESTful API Design**: Standard HTTP methods and status codes
- **JWT Authentication**: Secure token-based authentication
- **Role-based Authorization**: SystemAdmin, TenantAdmin, Operator roles
- **Multi-tenant Data Isolation**: Tenant-specific data segregation
- **Input Validation**: Comprehensive request validation
- **Error Handling**: Structured error responses
- **Swagger Documentation**: Auto-generated API documentation

#### Project Structure
```
RdpRelay.Portal.Api/
├── Controllers/                    # API Controllers
│   ├── AuthController.cs          # Authentication endpoints
│   ├── AgentsController.cs        # Agent management
│   ├── SessionsController.cs      # Session management
│   └── TenantsController.cs       # Tenant management
├── Services/                      # Business logic services
│   ├── UserService.cs             # User management logic
│   ├── TenantService.cs           # Tenant management logic
│   ├── AgentService.cs            # Agent management logic
│   ├── SessionService.cs          # Session management logic
│   ├── JwtService.cs              # JWT token management
│   └── MongoDbService.cs          # Database connection service
├── Models/                        # Data models
│   ├── DatabaseModels.cs          # MongoDB entity models
│   ├── ApiModels.cs               # Request/Response DTOs
│   └── Options.cs                 # Configuration models
└── Program.cs                     # Application startup and configuration
```

#### Core Services

##### UserService
```csharp
// User authentication and management
- AuthenticateAsync(email, password)          // Login validation
- CreateUserAsync(request, createdBy)         // User creation
- GetUserAsync(tenantId, userId)              // User retrieval
- UpdateUserAsync(userId, request)            // User updates
- GetUserByEmailAsync(email)                  // Email-based lookup
```

##### TenantService
```csharp
// Multi-tenant management
- GetTenantsAsync(skip, limit)                // Paginated tenant list
- GetTenantAsync(tenantId)                    // Single tenant retrieval
- CreateTenantAsync(request, createdBy)       // Tenant creation
- UpdateTenantAsync(tenantId, request)        // Tenant updates
```

##### JwtService
```csharp
// JWT token management
- GenerateAccessToken(user)                   // Create access token
- GenerateRefreshToken()                      // Create refresh token
- ValidateRefreshToken(token)                 // Validate refresh token
- RevokeRefreshToken(token)                   // Token revocation
```

### 3. Relay Server

**Location**: `/relay/RdpRelay.Relay/`
**Technology**: .NET 9 ASP.NET Core with SignalR
**Port**: 5001 (HTTP), 9443 (HTTPS)

#### Key Features
- **WebSocket Communication**: Real-time bi-directional communication
- **RDP Proxy**: Tunneling RDP traffic through WebSocket
- **Agent Management**: Registration and heartbeat monitoring
- **Session Orchestration**: RDP session lifecycle management
- **Load Balancing**: Agent selection and load distribution
- **Security**: TLS encryption and authentication

#### Core Responsibilities
- **Agent Registration**: Windows agents register and maintain connection
- **Session Brokering**: Match user requests with available agents
- **Traffic Relay**: Proxy RDP traffic between users and target machines
- **Health Monitoring**: Monitor agent health and connectivity
- **Event Broadcasting**: Real-time updates to portal web interface

### 4. Windows Agent

**Location**: `/agent-win/RdpRelay.Agent.Win/`
**Technology**: .NET 9 Windows Service
**Platform**: Windows 10/11, Windows Server 2019+

#### Key Features
- **Windows Service**: Runs as a system service for reliability
- **Auto-Registration**: Automatically registers with relay server
- **RDP Management**: Manages local RDP sessions
- **System Monitoring**: Reports system status and resource usage
- **Security**: Certificate-based authentication with relay server

#### Project Structure
```
RdpRelay.Agent.Win/
├── Services/
│   ├── AgentService.cs            # Main agent service
│   ├── RelayWebSocketClient.cs    # WebSocket client for relay communication
│   ├── RdpConnectionManager.cs    # RDP session management
│   └── SystemInfoService.cs      # System information collection
├── Models/
│   └── AgentModels.cs             # Agent-specific data models
└── Program.cs                     # Windows Service configuration
```

### 5. Database Layer

#### MongoDB Collections

##### Users Collection
```javascript
{
  _id: ObjectId,
  tenantId: ObjectId,           // Reference to tenant
  email: String,                // Unique email address
  firstName: String,            // User first name
  lastName: String,             // User last name
  passwordHash: String,         // BCrypt hashed password
  role: String,                 // SystemAdmin|TenantAdmin|Operator
  isActive: Boolean,            // Account status
  createdAt: Date,              // Account creation timestamp
  updatedAt: Date,              // Last modification timestamp
  lastLoginAt: Date             // Last successful login
}
```

##### Tenants Collection
```javascript
{
  _id: ObjectId,
  name: String,                 // Tenant display name
  domain: String,               // Unique tenant domain
  isActive: Boolean,            // Tenant status
  createdAt: Date,              // Creation timestamp
  updatedAt: Date,              // Last modification timestamp
  settings: {
    maxAgents: Number,          // Maximum allowed agents
    maxConcurrentSessions: Number, // Session limit
    sessionTimeoutMinutes: Number, // Session timeout
    requireTls: Boolean,        // Force TLS connections
    allowedIpRanges: [String]   // IP whitelist
  }
}
```

##### Agents Collection
```javascript
{
  _id: ObjectId,
  tenantId: ObjectId,           // Reference to tenant
  machineName: String,          // Windows machine name
  ipAddress: String,            // Agent IP address
  version: String,              // Agent software version
  status: String,               // Online|Offline|InSession|Error
  lastHeartbeat: Date,          // Last communication timestamp
  capabilities: {
    rdpEnabled: Boolean,        // RDP service status
    multiSession: Boolean,      // Multiple session support
    maxSessions: Number         // Maximum concurrent sessions
  },
  systemInfo: {
    os: String,                 // Operating system info
    memory: Number,             // RAM in GB
    cpu: String,                // Processor information
    disk: Number                // Available disk space
  },
  createdAt: Date,
  updatedAt: Date
}
```

##### Sessions Collection
```javascript
{
  _id: ObjectId,
  tenantId: ObjectId,           // Reference to tenant
  userId: ObjectId,             // User who created session
  agentId: ObjectId,            // Target agent
  connectCode: String,          // Unique session identifier
  status: String,               // Pending|Active|Ended|Error
  startTime: Date,              // Session start timestamp
  endTime: Date,                // Session end timestamp (if ended)
  connectionInfo: {
    clientIp: String,           // User's IP address
    screenResolution: String,   // Display resolution
    colorDepth: Number          // Color depth setting
  },
  metadata: {
    duration: Number,           // Session duration in seconds
    dataTransferred: Number     // Bytes transferred
  },
  createdAt: Date
}
```

#### Redis Data Structures

##### Session Storage
```
Key: "session:user:{userId}"
Value: {
  sessionId: String,
  tenantId: String,
  role: String,
  loginTime: Date,
  lastActivity: Date
}
TTL: 3600 seconds (1 hour)
```

##### Agent Heartbeats
```
Key: "agent:heartbeat:{agentId}"
Value: {
  status: String,
  timestamp: Date,
  activeSessions: Number
}
TTL: 300 seconds (5 minutes)
```

---

## Data Flow & Communication

### Authentication Flow

```mermaid
sequenceDiagram
    participant U as User Browser
    participant N as Nginx
    participant A as Portal API
    participant M as MongoDB
    participant R as Redis

    U->>N: POST /api/auth/login
    N->>A: Forward login request
    A->>M: Validate credentials
    M->>A: User data
    A->>R: Store session
    A->>U: JWT tokens + user info
    
    Note over U,R: Subsequent authenticated requests
    U->>N: API request + JWT
    N->>A: Forward with token
    A->>A: Validate JWT
    A->>R: Check session
    A->>M: Execute business logic
    A->>U: Response
```

### Session Creation Flow

```mermaid
sequenceDiagram
    participant U as User Browser
    participant A as Portal API
    participant RS as Relay Server
    participant AG as Windows Agent
    participant W as Windows Machine

    U->>A: Create RDP session
    A->>A: Validate permissions
    A->>A: Select available agent
    A->>RS: Request session creation
    RS->>AG: Initiate RDP session
    AG->>W: Start RDP connection
    W->>AG: RDP ready
    AG->>RS: Session established
    RS->>A: Session details
    A->>U: Connection information
    
    Note over U,W: RDP traffic flows through relay
    U->>RS: RDP traffic
    RS->>AG: Forward traffic
    AG->>W: RDP protocol
    W->>AG: RDP response
    AG->>RS: Forward response
    RS->>U: RDP data
```

### Real-time Updates Flow

```mermaid
sequenceDiagram
    participant W as Portal Web
    participant RS as Relay Server
    participant AG as Windows Agent
    participant R as Redis

    Note over W,R: WebSocket connection establishment
    W->>RS: WebSocket connection
    RS->>R: Subscribe to events
    
    Note over AG,R: Agent status updates
    AG->>RS: Heartbeat/Status update
    RS->>R: Publish agent status
    R->>RS: Agent status event
    RS->>W: Real-time status update
    
    Note over W,R: Session events
    AG->>RS: Session state change
    RS->>R: Publish session event
    R->>RS: Session event
    RS->>W: Real-time session update
```

---

## Security Implementation

### Authentication & Authorization

#### JWT Token Structure
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "nameid": "user_id",
    "role": "SystemAdmin|TenantAdmin|Operator",
    "tenant_id": "tenant_id",
    "tokenType": "access",
    "nbf": timestamp,
    "exp": timestamp,
    "iat": timestamp,
    "iss": "RdpRelayPortal",
    "aud": "RdpRelayPortal"
  }
}
```

#### Role-based Permissions

| Role | Permissions |
|------|-------------|
| **SystemAdmin** | • Manage all tenants<br/>• Create/modify/delete any user<br/>• Access system-wide statistics<br/>• Configure global settings |
| **TenantAdmin** | • Manage users within their tenant<br/>• Configure tenant settings<br/>• Deploy and manage agents<br/>• View tenant-specific reports |
| **Operator** | • Create and manage RDP sessions<br/>• View assigned agents<br/>• Access basic dashboard |

#### Password Security
- **Hashing Algorithm**: BCrypt with salt rounds = 10
- **Password Requirements**: Minimum 8 characters (configurable)
- **Password Storage**: Never stored in plaintext
- **Token Expiry**: Access tokens expire in 1 hour, refresh tokens in 7 days

### Network Security

#### TLS/SSL Configuration
- **Certificate Management**: Self-signed certificates for development
- **Cipher Suites**: Modern TLS 1.3 preferred
- **HSTS**: HTTP Strict Transport Security enabled
- **Certificate Rotation**: Automated certificate renewal support

#### API Security Headers
```nginx
# Nginx security headers
add_header X-Content-Type-Options nosniff;
add_header X-Frame-Options DENY;
add_header X-XSS-Protection "1; mode=block";
add_header Referrer-Policy strict-origin-when-cross-origin;
add_header Content-Security-Policy "default-src 'self'";
```

### Data Protection

#### Database Security
- **Connection Encryption**: TLS connections to MongoDB
- **Authentication**: Username/password with role-based access
- **Data Isolation**: Tenant-based data segregation
- **Backup Encryption**: Encrypted database backups

#### Session Security
- **Session Tokens**: Cryptographically secure random tokens
- **Session Timeout**: Configurable timeout periods
- **Session Invalidation**: Immediate session termination on logout
- **Concurrent Session Control**: Configurable session limits

---

## Deployment Architecture

### Docker Composition

The system is deployed using Docker Compose with the following services:

#### Service Definitions
```yaml
services:
  # Reverse Proxy & Load Balancer
  nginx:
    image: nginx:alpine
    ports: ["8080:80", "8443:443"]
    volumes: 
      - ./infra/nginx:/etc/nginx/conf.d
      - ./infra/certs:/etc/ssl/certs

  # Frontend Application
  portal-web:
    build: ./infra/docker/Dockerfile.portal-web
    ports: ["3000:80"]
    environment:
      - VITE_API_BASE_URL=http://localhost:5000

  # Backend API
  portal-api:
    build: ./infra/docker/Dockerfile.portal-api
    ports: ["5000:8080"]
    environment:
      - MongoDB__ConnectionString=mongodb://admin:password@mongodb:27017/rdp_relay?authSource=admin
      - Redis__ConnectionString=redis:6379,password=redis_password
      - Jwt__SecretKey=jwt_secret_key

  # Relay Server
  relay-server:
    build: ./infra/docker/Dockerfile.relay
    ports: ["5001:8080", "9443:8443"]
    
  # Database
  mongodb:
    image: mongo:7.0
    ports: ["27017:27017"]
    environment:
      - MONGO_INITDB_ROOT_USERNAME=admin
      - MONGO_INITDB_ROOT_PASSWORD=password
    volumes:
      - ./infra/mongodb/init-mongo.js:/docker-entrypoint-initdb.d/init-mongo.js
      - ./data/mongodb:/data/db

  # Cache & Session Store
  redis:
    image: redis:7.2-alpine
    ports: ["6379:6379"]
    command: redis-server --requirepass redis_password
    volumes:
      - ./data/redis:/data
```

### Network Architecture
```
Internet
    ↓
[Load Balancer/Firewall]
    ↓
[Nginx Reverse Proxy] :8080,:8443
    ↓
┌─────────────────┬─────────────────┐
│   Portal Web    │   Portal API    │
│   :3000         │   :5000         │
└─────────────────┴─────────────────┘
                  ↓
    ┌─────────────────┬─────────────────┐
    │   MongoDB       │     Redis       │
    │   :27017        │     :6379       │
    └─────────────────┴─────────────────┘
                  
[Relay Server] :5001,:9443
    ↓
[Windows Agents] → [Target Windows Machines]
```

### Environment Configuration

#### Production Environment Variables
```bash
# MongoDB Configuration
MONGODB_PASSWORD=secure_mongodb_password_2024
MONGODB_DATABASE=rdp_relay

# Redis Configuration
REDIS_PASSWORD=secure_redis_password_2024

# JWT Configuration
JWT_SECRET_KEY=secure-jwt-signing-key-minimum-32-characters-required
JWT_ISSUER=RdpRelayPortal
JWT_AUDIENCE=RdpRelayPortal

# Certificate Configuration
CERT_PASSWORD=secure_certificate_password_2024

# Application URLs
VITE_API_BASE_URL=https://your-domain.com
VITE_RELAY_WS_URL=wss://relay.your-domain.com
VITE_APP_TITLE=RDP Relay Portal
```

---

## Configuration Management

### Application Settings

#### Portal API Configuration (`appsettings.json`)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Portal": {
    "RequireHttps": true,
    "CorsOrigins": ["https://your-domain.com"]
  },
  "Jwt": {
    "Issuer": "RdpRelay.Portal",
    "Audience": "RdpRelay.Users",
    "SigningKey": "environment-variable-override",
    "AccessTokenExpiryHours": 1,
    "RefreshTokenExpiryDays": 30
  },
  "MongoDb": {
    "ConnectionString": "environment-variable-override",
    "DatabaseName": "rdp_relay"
  },
  "Redis": {
    "ConnectionString": "environment-variable-override"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/app/logs/portal-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

#### Relay Server Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Relay": {
    "MaxConcurrentSessions": 100,
    "SessionTimeoutMinutes": 480,
    "HeartbeatIntervalSeconds": 30,
    "AgentTimeoutMinutes": 5
  },
  "SignalR": {
    "MaxMessageSize": 102400,
    "StreamBufferCapacity": 10,
    "TransportMaxBufferSize": 32768
  },
  "Certificates": {
    "Path": "/app/certs/relay.pfx",
    "Password": "environment-variable-override"
  }
}
```

#### Frontend Configuration (`vite.config.ts`)
```typescript
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
          mui: ['@mui/material', '@mui/icons-material']
        }
      }
    }
  }
})
```

---

## Monitoring & Logging

### Application Logging

#### Log Levels & Structure
```csharp
// Structured logging with Serilog
Log.Information("User {Email} authenticated successfully from {IpAddress}", 
    user.Email, context.Connection.RemoteIpAddress);

Log.Warning("Failed login attempt for {Email} from {IpAddress}", 
    request.Email, context.Connection.RemoteIpAddress);

Log.Error(ex, "Database connection failed for tenant {TenantId}", tenantId);
```

#### Log Categories
- **Authentication**: Login attempts, token validation, role checks
- **Session Management**: Session creation, termination, errors
- **Agent Communication**: Heartbeats, registration, disconnections  
- **Database Operations**: Query performance, connection issues
- **Security Events**: Unauthorized access, suspicious activity
- **System Health**: Resource usage, performance metrics

### Health Monitoring

#### Health Check Endpoints
```csharp
// Portal API Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Custom health checks
services.AddHealthChecks()
    .AddMongoDb(connectionString, "mongodb")
    .AddRedis(redisConnectionString, "redis")
    .AddCheck<RelayServerHealthCheck>("relay-server");
```

#### Monitoring Metrics
- **Response Times**: API endpoint performance
- **Error Rates**: HTTP error status codes
- **Database Performance**: Query execution times
- **Memory Usage**: Application memory consumption
- **Connection Counts**: Active database and Redis connections
- **Session Statistics**: Active sessions, session duration averages

### Logging Configuration

#### Container Log Management
```yaml
# Docker Compose logging configuration
services:
  portal-api:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
        
  relay-server:
    logging:
      driver: "json-file" 
      options:
        max-size: "10m"
        max-file: "3"
```

#### Log Aggregation (Production Recommendation)
```yaml
# Example ELK Stack integration
version: '3.8'
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.8.0
    environment:
      - discovery.type=single-node
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
      
  logstash:
    image: docker.elastic.co/logstash/logstash:8.8.0
    volumes:
      - ./logstash/pipeline:/usr/share/logstash/pipeline
      
  kibana:
    image: docker.elastic.co/kibana/kibana:8.8.0
    ports:
      - "5601:5601"
```

---

## Development Workflow

### Local Development Setup

#### Prerequisites
- Docker Desktop 4.0+
- .NET 9 SDK
- Node.js 18+ with npm
- Git

#### Development Commands
```bash
# Clone repository
git clone <repository-url>
cd rdp-relay

# Environment setup
cp .env.example .env
# Edit .env with development values

# Build and start all services
docker-compose up --build

# Development mode (with hot reload)
# Terminal 1: Start infrastructure
docker-compose up mongodb redis nginx

# Terminal 2: Start API in development mode
cd portal-api/RdpRelay.Portal.Api
dotnet run --environment Development

# Terminal 3: Start web app in development mode
cd portal-web
npm install
npm run dev

# Terminal 4: Start relay server in development mode
cd relay/RdpRelay.Relay
dotnet run --environment Development
```

### Code Structure & Standards

#### API Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExampleController : ControllerBase
{
    private readonly IExampleService _service;
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(IExampleService service, ILogger<ExampleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ExampleDto>>> GetItems(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20)
    {
        try
        {
            var items = await _service.GetItemsAsync(skip, limit);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve items");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
```

#### React Component Pattern
```typescript
// TypeScript React component with hooks
interface ComponentProps {
  data: ApiData[];
  onUpdate: (id: string) => void;
}

export const ExampleComponent: React.FC<ComponentProps> = ({ data, onUpdate }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  
  const handleSubmit = useCallback(async (formData: FormData) => {
    setLoading(true);
    setError('');
    
    try {
      await apiService.updateItem(formData);
      onUpdate(formData.id);
    } catch (err) {
      setError('Failed to update item');
    } finally {
      setLoading(false);
    }
  }, [onUpdate]);

  return (
    <Box>
      {/* Component JSX */}
    </Box>
  );
};
```

### Testing Strategy

#### Unit Testing (.NET)
```csharp
[TestFixture]
public class UserServiceTests
{
    private Mock<IMongoCollection<User>> _mockCollection;
    private UserService _service;

    [SetUp]
    public void Setup()
    {
        _mockCollection = new Mock<IMongoCollection<User>>();
        _service = new UserService(mockDatabase.Object, mockLogger.Object);
    }

    [Test]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var user = new User 
        { 
            Email = email, 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true 
        };
        
        // Mock database response
        _mockCollection.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MockCursor(user));

        // Act
        var result = await _service.AuthenticateAsync(email, password);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(email, result.Email);
    }
}
```

#### Integration Testing
```csharp
[TestFixture]
public class AuthControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Replace with test database
                    services.AddSingleton<MongoDbService>(testDbService);
                });
            });
        
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest 
        { 
            Email = "test@test.com", 
            Password = "password" 
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content);
        
        Assert.IsNotNull(loginResponse.Token);
        Assert.IsNotNull(loginResponse.RefreshToken);
    }
}
```

---

## Troubleshooting Guide

### Common Issues & Solutions

#### 1. Container Startup Issues

**Problem**: `portal-api` container fails to start with MongoDB connection errors
```
MongoDB.Driver.MongoAuthenticationException: Authentication failed
```

**Solution**:
```bash
# Check MongoDB connection string
docker exec rdp-relay-portal-api printenv | grep MongoDB

# Verify MongoDB credentials
docker exec rdp-relay-mongodb mongosh \
  "mongodb://admin:password@mongodb:27017/rdp_relay?authSource=admin" \
  --eval "db.runCommand({connectionStatus : 1})"

# Update connection string with authSource
MongoDB__ConnectionString=mongodb://admin:password@mongodb:27017/rdp_relay?authSource=admin
```

#### 2. Authentication Issues

**Problem**: User receives "Invalid email or password" despite correct credentials
```
System.FormatException: Requested value 'admin' was not found
```

**Solution**: Check user role enum values in database
```bash
# Check user role in MongoDB
docker exec rdp-relay-mongodb mongosh \
  "mongodb://admin:password@mongodb:27017/rdp_relay?authSource=admin" \
  --eval "db.users.findOne({email: 'user@example.com'}, {role: 1})"

# Update role to match C# enum
db.users.updateOne(
  {email: 'user@example.com'}, 
  {$set: {role: 'SystemAdmin'}}
)
```

#### 3. JWT Token Issues

**Problem**: JWT tokens expire immediately or fail validation
```
Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException: Token expired
```

**Solution**: Check JWT configuration
```bash
# Verify JWT settings
docker exec rdp-relay-portal-api printenv | grep JWT

# Check system clock synchronization
date
docker exec rdp-relay-portal-api date

# Update JWT settings if needed
JWT_SECRET_KEY=your-256-bit-secret-key-here-minimum-32-characters
```

#### 4. WebSocket Connection Failures

**Problem**: Real-time updates not working, WebSocket connection fails
```
WebSocket connection failed: Connection refused
```

**Solution**: Check relay server and networking
```bash
# Test relay server health
curl http://localhost:5001/health

# Check WebSocket endpoint
wscat -c ws://localhost:5001/hub

# Verify nginx proxy configuration
docker exec rdp-relay-nginx nginx -t
```

#### 5. Database Performance Issues

**Problem**: Slow API responses, database query timeouts
```
MongoDB.Driver.MongoExecutionTimeoutException: Operation timed out
```

**Solution**: Check database indexes and connections
```bash
# Check MongoDB indexes
docker exec rdp-relay-mongodb mongosh \
  "mongodb://admin:password@mongodb:27017/rdp_relay?authSource=admin" \
  --eval "db.users.getIndexes()"

# Monitor active connections
db.serverStatus().connections

# Check query performance
db.users.find({email: 'test@test.com'}).explain('executionStats')
```

### Performance Optimization

#### Database Optimization
```javascript
// Create compound indexes for common queries
db.users.createIndex({ "tenantId": 1, "isActive": 1 });
db.sessions.createIndex({ "tenantId": 1, "status": 1, "createdAt": -1 });
db.agents.createIndex({ "tenantId": 1, "status": 1, "lastHeartbeat": -1 });

// Enable profiling for slow queries
db.setProfilingLevel(2, { slowms: 100 });
```

#### API Performance
```csharp
// Implement response caching
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "tenantId" })]
public async Task<ActionResult<List<Agent>>> GetAgents()

// Use pagination for large datasets  
[HttpGet]
public async Task<ActionResult<PagedResponse<User>>> GetUsers(
    [FromQuery] int skip = 0,
    [FromQuery] int limit = 20)
```

#### Frontend Performance
```typescript
// Implement virtual scrolling for large lists
import { FixedSizeList as List } from 'react-window';

// Use React.memo for expensive components
export const ExpensiveComponent = React.memo(({ data }) => {
  return <ComplexVisualization data={data} />;
});

// Implement proper loading states and error boundaries
```

### Monitoring & Alerting

#### Key Metrics to Monitor
- **Response Time**: API endpoint response times
- **Error Rate**: HTTP 5xx error percentage
- **Database Connections**: Active MongoDB connections
- **Memory Usage**: Application memory consumption
- **Active Sessions**: Current RDP session count
- **Agent Health**: Number of online vs offline agents

#### Alert Thresholds
- API response time > 2 seconds
- Error rate > 5%
- Database connections > 80% of max
- Memory usage > 80%
- Agent offline for > 10 minutes
- Session creation failures > 10%

---

## Future Enhancements

### Planned Features

#### 1. High Availability
- **Load Balancing**: Multiple API instances behind load balancer
- **Database Clustering**: MongoDB replica sets for redundancy
- **Cache Clustering**: Redis Cluster for horizontal scaling
- **Failover Mechanisms**: Automatic service recovery

#### 2. Advanced Security
- **OAuth2/OIDC Integration**: Enterprise identity provider support
- **Multi-Factor Authentication**: TOTP/SMS-based 2FA
- **Certificate Management**: Automated certificate rotation
- **Audit Logging**: Comprehensive security event logging
- **IP Whitelisting**: Network-based access controls

#### 3. Enhanced Monitoring
- **Prometheus Integration**: Metrics collection and alerting
- **Grafana Dashboards**: Visual performance monitoring
- **Distributed Tracing**: Request tracing across services
- **Log Aggregation**: Centralized logging with ELK stack
- **Health Dashboards**: Real-time system health monitoring

#### 4. Scalability Improvements
- **Kubernetes Deployment**: Container orchestration
- **Microservices Architecture**: Service decomposition
- **Event Sourcing**: Audit trail and state reconstruction
- **CQRS Implementation**: Command/Query responsibility segregation
- **Message Queues**: Asynchronous processing with RabbitMQ

#### 5. User Experience
- **Mobile Application**: iOS/Android native apps
- **Desktop Client**: Electron-based desktop application
- **Browser RDP Client**: HTML5 RDP client integration
- **Session Recording**: RDP session recording and playback
- **File Transfer**: Secure file transfer during RDP sessions

#### 6. Advanced Features
- **Session Scheduling**: Scheduled RDP session creation
- **Resource Quotas**: User/tenant-based resource limits
- **Custom Branding**: White-label portal customization
- **API Rate Limiting**: Request throttling and quotas
- **Webhook Integration**: External system notifications

---

## Conclusion

The RDP Relay Platform represents a comprehensive, enterprise-grade solution for managing Remote Desktop Protocol connections across distributed Windows environments. Built with modern technologies and following industry best practices, the platform provides:

- **Scalable Architecture**: Microservices-based design with horizontal scaling capabilities
- **Enterprise Security**: JWT authentication, role-based access, and encrypted communication
- **Real-time Communication**: WebSocket-based real-time updates and session management
- **Multi-tenant Support**: Complete data isolation and tenant-specific configurations
- **Comprehensive Monitoring**: Structured logging, health checks, and performance metrics
- **Developer-friendly**: Well-documented APIs, type-safe frontend, and maintainable code structure

The platform is production-ready with proper deployment configurations, security measures, and monitoring capabilities. The modular architecture allows for easy extension and customization to meet specific organizational requirements.

For support, feature requests, or contributions, please refer to the project repository and documentation.

---

**Document Version**: 1.0  
**Last Updated**: August 27, 2025  
**Maintained by**: RDP Relay Development Team
