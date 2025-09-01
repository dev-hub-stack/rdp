# RDP Relay Platform - Technical Architecture & Data Flow

## System Architecture Diagrams

### 1. High-Level System Architecture

```mermaid
graph TB
    subgraph "External Users"
        ADM[System Administrator]
        TEN[Tenant Administrator] 
        OPR[Operator]
    end
    
    subgraph "Load Balancer & Proxy Layer"
        LB[Load Balancer<br/>nginx:alpine]
        SSL[SSL/TLS Termination]
    end
    
    subgraph "Application Tier"
        WEB[Portal Web<br/>React 18 + TypeScript<br/>Port: 3000]
        API[Portal API<br/>.NET 9 Web API<br/>Port: 5000]
        REL[Relay Server<br/>.NET 9 + SignalR<br/>Port: 5001/9443]
    end
    
    subgraph "Data Tier"
        MONGO[(MongoDB 7.0<br/>Primary Database<br/>Port: 27017)]
        REDIS[(Redis 7.2<br/>Cache & Sessions<br/>Port: 6379)]
    end
    
    subgraph "Windows Environment"
        AG1[Windows Agent 1<br/>.NET 9 Service]
        AG2[Windows Agent 2<br/>.NET 9 Service]
        AGN[Windows Agent N<br/>.NET 9 Service]
        
        WIN1[Windows Server 2019+<br/>RDP Enabled]
        WIN2[Windows 10/11<br/>RDP Enabled]
        WINN[Windows Machine N<br/>RDP Enabled]
    end
    
    subgraph "Monitoring & Logging"
        LOG[Log Aggregation<br/>Serilog + Files]
        MON[Health Monitoring<br/>Built-in Checks]
    end
    
    %% External connections
    ADM --> LB
    TEN --> LB
    OPR --> LB
    
    %% Load balancer routing
    LB --> SSL
    SSL --> WEB
    SSL --> API
    
    %% Application tier connections
    WEB -.->|API Calls| API
    API --> MONGO
    API --> REDIS
    REL --> REDIS
    
    %% Windows agent connections
    AG1 -.->|WebSocket| REL
    AG2 -.->|WebSocket| REL
    AGN -.->|WebSocket| REL
    
    AG1 --> WIN1
    AG2 --> WIN2
    AGN --> WINN
    
    %% Monitoring connections
    API --> LOG
    REL --> LOG
    API --> MON
    REL --> MON
    
    %% Styling
    classDef user fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef web fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef api fill:#e8f5e8,stroke:#388e3c,stroke-width:2px
    classDef data fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef windows fill:#ffebee,stroke:#d32f2f,stroke-width:2px
    classDef monitor fill:#f1f8e9,stroke:#689f38,stroke-width:2px
    classDef proxy fill:#fce4ec,stroke:#c2185b,stroke-width:2px
    
    class ADM,TEN,OPR user
    class WEB web
    class API,REL api
    class MONGO,REDIS data
    class AG1,AG2,AGN,WIN1,WIN2,WINN windows
    class LOG,MON monitor
    class LB,SSL proxy
```

### 2. Container Architecture & Networking

```mermaid
graph TB
    subgraph "Docker Host"
        subgraph "rdp-relay-network (Bridge)"
            subgraph "Frontend Tier"
                NGINX[nginx:alpine<br/>rdp-relay-nginx<br/>8080:80, 8443:443]
                WEB[portal-web<br/>rdp-relay-portal-web<br/>3000:80]
            end
            
            subgraph "Backend Tier"
                API[portal-api<br/>rdp-relay-portal-api<br/>5000:8080]
                RELAY[relay-server<br/>rdp-relay-relay-server<br/>5001:8080, 9443:8443]
            end
            
            subgraph "Data Tier"
                MONGO[mongo:7.0<br/>rdp-relay-mongodb<br/>27017:27017]
                REDIS[redis:7.2-alpine<br/>rdp-relay-redis<br/>6379:6379]
            end
        end
        
        subgraph "Volume Mounts"
            LOGS[./logs:/app/logs]
            DATA_MONGO[./data/mongodb:/data/db]
            DATA_REDIS[./data/redis:/data]
            CERTS[./infra/certs:/app/certs]
            CONFIG[./infra/nginx:/etc/nginx/conf.d]
        end
    end
    
    subgraph "External Network"
        CLIENT[Client Browser<br/>:8080,:8443]
        AGENTS[Windows Agents<br/>:5001,:9443]
    end
    
    %% Network connections
    CLIENT --> NGINX
    NGINX --> WEB
    NGINX --> API
    AGENTS --> RELAY
    
    %% Internal service connections
    API --> MONGO
    API --> REDIS
    RELAY --> REDIS
    
    %% Volume connections
    API -.-> LOGS
    RELAY -.-> LOGS
    MONGO -.-> DATA_MONGO
    REDIS -.-> DATA_REDIS
    RELAY -.-> CERTS
    NGINX -.-> CONFIG
    
    classDef container fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef volume fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef external fill:#ffebee,stroke:#d32f2f,stroke-width:2px
    
    class NGINX,WEB,API,RELAY,MONGO,REDIS container
    class LOGS,DATA_MONGO,DATA_REDIS,CERTS,CONFIG volume
    class CLIENT,AGENTS external
```

### 3. Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant U as User Browser
    participant N as Nginx Proxy
    participant W as Portal Web
    participant A as Portal API
    participant M as MongoDB
    participant R as Redis
    
    Note over U,R: Initial Authentication Flow
    U->>W: Access protected route
    W->>U: Redirect to login page
    U->>W: Submit login credentials
    W->>N: POST /api/auth/login
    N->>A: Forward login request
    
    A->>M: Query user by email
    M->>A: Return user data
    A->>A: Verify password (BCrypt)
    A->>A: Generate JWT tokens
    A->>R: Store refresh token
    A->>W: Return tokens + user info
    W->>W: Store tokens in localStorage
    W->>U: Redirect to dashboard
    
    Note over U,R: Authenticated API Requests
    U->>W: Interact with UI
    W->>N: API request + JWT token
    N->>A: Forward with Authorization header
    A->>A: Validate JWT signature
    A->>A: Check token expiration
    A->>A: Extract user claims
    A->>A: Authorize based on role
    A->>M: Execute business logic
    M->>A: Return data
    A->>W: Return response
    W->>U: Update UI
    
    Note over U,R: Token Refresh Flow
    A->>W: Token expired (401)
    W->>N: POST /api/auth/refresh
    N->>A: Refresh token request
    A->>R: Validate refresh token
    R->>A: Confirm token validity
    A->>A: Generate new access token
    A->>R: Update refresh token
    A->>W: Return new tokens
    W->>W: Update stored tokens
    W->>N: Retry original request
```

### 4. RDP Session Creation & Management Flow

```mermaid
sequenceDiagram
    participant U as User Browser
    participant W as Portal Web
    participant A as Portal API
    participant R as Redis
    participant RS as Relay Server
    participant AG as Windows Agent
    participant WIN as Windows Machine
    participant M as MongoDB
    
    Note over U,WIN: Session Creation Request
    U->>W: Request new RDP session
    W->>A: POST /api/sessions (with target agent)
    A->>M: Validate user permissions
    A->>M: Check agent availability
    A->>M: Create session record
    M->>A: Return session ID
    
    A->>R: Cache session metadata
    A->>RS: Request session initiation
    RS->>AG: Send session creation command
    AG->>WIN: Initialize RDP connection
    WIN->>AG: RDP service ready
    AG->>RS: Confirm session ready
    RS->>A: Session established
    A->>W: Return connection details
    W->>U: Display connection info
    
    Note over U,WIN: Real-time Session Updates
    AG->>RS: Session status updates
    RS->>R: Publish session events
    R->>RS: Distribute to subscribers
    RS->>W: WebSocket status update
    W->>U: Update session UI
    
    Note over U,WIN: RDP Traffic Flow
    U->>RS: RDP client connection
    RS->>AG: Forward RDP traffic
    AG->>WIN: RDP protocol data
    WIN->>AG: RDP response
    AG->>RS: Forward response
    RS->>U: RDP data to client
    
    Note over U,WIN: Session Termination
    U->>W: End session request
    W->>A: DELETE /api/sessions/{id}
    A->>RS: Terminate session command
    RS->>AG: Send termination signal
    AG->>WIN: Close RDP connection
    AG->>RS: Confirm termination
    RS->>A: Session ended
    A->>M: Update session record
    A->>R: Clear session cache
    A->>W: Confirm termination
    W->>U: Update UI
```

### 5. Agent Registration & Heartbeat Flow

```mermaid
sequenceDiagram
    participant AG as Windows Agent
    participant RS as Relay Server
    participant R as Redis
    participant A as Portal API
    participant M as MongoDB
    participant W as Portal Web
    
    Note over AG,W: Agent Registration
    AG->>AG: Service startup
    AG->>AG: Collect system info
    AG->>RS: WebSocket connection
    RS->>RS: Authenticate agent
    AG->>RS: Registration request
    RS->>A: Register new agent
    A->>M: Store agent record
    A->>R: Cache agent status
    
    Note over AG,W: Heartbeat Cycle
    loop Every 30 seconds
        AG->>RS: Heartbeat + status
        RS->>R: Update agent cache
        R->>RS: Acknowledge update
        RS->>AG: Heartbeat ACK
        
        RS->>W: Real-time status update
        W->>W: Update agent UI
    end
    
    Note over AG,W: Agent Offline Detection
    RS->>RS: Monitor heartbeat timeouts
    RS->>R: Check last heartbeat
    R->>RS: Agent timeout detected
    RS->>A: Report agent offline
    A->>M: Update agent status
    A->>R: Update cached status
    RS->>W: Agent offline notification
    W->>W: Update UI (agent offline)
    
    Note over AG,W: Agent Reconnection
    AG->>RS: Reconnect attempt
    RS->>RS: Validate reconnection
    AG->>RS: Status update
    RS->>A: Agent back online
    A->>M: Update agent status
    A->>R: Update cached status
    RS->>W: Agent online notification
    W->>W: Update UI (agent online)
```

### 6. Data Storage & Caching Strategy

```mermaid
graph TB
    subgraph "Application Layer"
        API[Portal API]
        REL[Relay Server]
        WEB[Portal Web]
    end
    
    subgraph "Caching Layer (Redis)"
        subgraph "Session Data"
            SESS[User Sessions<br/>TTL: 1 hour]
            JWT[JWT Blacklist<br/>TTL: Token lifetime]
        end
        
        subgraph "Real-time Data"
            AGENT[Agent Status<br/>TTL: 5 minutes]
            HEART[Heartbeats<br/>TTL: 1 minute]
        end
        
        subgraph "Pub/Sub Channels"
            EVENTS[Session Events]
            STATUS[Agent Status Changes]
            NOTIFY[Real-time Notifications]
        end
    end
    
    subgraph "Persistent Storage (MongoDB)"
        subgraph "Core Collections"
            USERS[Users Collection<br/>Indexed: email, tenantId]
            TENANTS[Tenants Collection<br/>Indexed: domain]
        end
        
        subgraph "Operational Collections"
            AGENTS_DB[Agents Collection<br/>Indexed: tenantId, status, lastHeartbeat]
            SESSIONS_DB[Sessions Collection<br/>Indexed: tenantId, userId, agentId, status, createdAt]
        end
        
        subgraph "Audit & Logs"
            AUDIT[Audit Logs<br/>Indexed: timestamp, userId, action]
            METRICS[Performance Metrics<br/>Indexed: timestamp, service]
        end
    end
    
    %% Data flow connections
    API --> SESS
    API --> JWT
    API --> USERS
    API --> TENANTS
    API --> SESSIONS_DB
    
    REL --> AGENT
    REL --> HEART
    REL --> EVENTS
    REL --> STATUS
    REL --> AGENTS_DB
    
    WEB --> NOTIFY
    
    %% Cache-through patterns
    API -.->|Cache Miss| USERS
    API -.->|Cache Hit| SESS
    REL -.->|Cache Miss| AGENTS_DB
    REL -.->|Cache Hit| AGENT
    
    classDef app fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef cache fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef db fill:#e8f5e8,stroke:#388e3c,stroke-width:2px
    
    class API,REL,WEB app
    class SESS,JWT,AGENT,HEART,EVENTS,STATUS,NOTIFY cache
    class USERS,TENANTS,AGENTS_DB,SESSIONS_DB,AUDIT,METRICS db
```

### 7. Security Architecture & Threat Model

```mermaid
graph TB
    subgraph "External Threats"
        DDOS[DDoS Attacks]
        INJECT[SQL/NoSQL Injection]
        XSS[Cross-Site Scripting]
        CSRF[CSRF Attacks]
        MITM[Man-in-the-Middle]
    end
    
    subgraph "Security Layers"
        subgraph "Network Security"
            FW[Firewall Rules]
            LB[Load Balancer<br/>Rate Limiting]
            TLS[TLS 1.3 Encryption]
        end
        
        subgraph "Application Security"
            AUTH[JWT Authentication]
            AUTHZ[Role-based Authorization]
            VALID[Input Validation]
            HEADERS[Security Headers]
        end
        
        subgraph "Data Security"
            ENCRYPT[Data Encryption]
            HASH[Password Hashing<br/>BCrypt]
            TENANT[Tenant Isolation]
            AUDIT[Audit Logging]
        end
        
        subgraph "Infrastructure Security"
            CONT[Container Security]
            NET[Network Isolation]
            CERTS[Certificate Management]
            SECRETS[Secrets Management]
        end
    end
    
    subgraph "Protected Assets"
        USER_DATA[User Credentials]
        SESSION_DATA[Session Information]
        RDP_TRAFFIC[RDP Communications]
        BUSINESS_DATA[Business Logic]
    end
    
    %% Threat mitigation connections
    DDOS -.->|Blocked by| LB
    INJECT -.->|Prevented by| VALID
    XSS -.->|Mitigated by| HEADERS
    CSRF -.->|Protected by| HEADERS
    MITM -.->|Prevented by| TLS
    
    %% Security implementations
    FW --> LB
    LB --> TLS
    TLS --> AUTH
    AUTH --> AUTHZ
    AUTHZ --> VALID
    VALID --> HEADERS
    
    ENCRYPT --> USER_DATA
    HASH --> USER_DATA
    AUTH --> SESSION_DATA
    TLS --> RDP_TRAFFIC
    TENANT --> BUSINESS_DATA
    
    classDef threat fill:#ffebee,stroke:#d32f2f,stroke-width:2px
    classDef security fill:#e8f5e8,stroke:#388e3c,stroke-width:2px
    classDef asset fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    
    class DDOS,INJECT,XSS,CSRF,MITM threat
    class FW,LB,TLS,AUTH,AUTHZ,VALID,HEADERS,ENCRYPT,HASH,TENANT,AUDIT,CONT,NET,CERTS,SECRETS security
    class USER_DATA,SESSION_DATA,RDP_TRAFFIC,BUSINESS_DATA asset
```

### 8. Deployment & Scaling Architecture

```mermaid
graph TB
    subgraph "Production Environment"
        subgraph "Load Balancer Tier"
            LB1[Load Balancer 1<br/>nginx + HAProxy]
            LB2[Load Balancer 2<br/>nginx + HAProxy]
        end
        
        subgraph "Application Tier"
            subgraph "Web Servers"
                WEB1[Portal Web 1<br/>nginx + React]
                WEB2[Portal Web 2<br/>nginx + React]
                WEBN[Portal Web N<br/>nginx + React]
            end
            
            subgraph "API Servers"
                API1[Portal API 1<br/>.NET 9]
                API2[Portal API 2<br/>.NET 9]
                APIN[Portal API N<br/>.NET 9]
            end
            
            subgraph "Relay Servers"
                REL1[Relay Server 1<br/>.NET 9 + SignalR]
                REL2[Relay Server 2<br/>.NET 9 + SignalR]
                RELN[Relay Server N<br/>.NET 9 + SignalR]
            end
        end
        
        subgraph "Data Tier"
            subgraph "Database Cluster"
                MONGO_P[MongoDB Primary<br/>Replica Set Leader]
                MONGO_S1[MongoDB Secondary 1<br/>Replica Set Member]
                MONGO_S2[MongoDB Secondary 2<br/>Replica Set Member]
            end
            
            subgraph "Cache Cluster"
                REDIS_M[Redis Master<br/>Cluster Node]
                REDIS_S1[Redis Slave 1<br/>Cluster Node]
                REDIS_S2[Redis Slave 2<br/>Cluster Node]
            end
        end
        
        subgraph "Monitoring & Logging"
            PROM[Prometheus<br/>Metrics Collection]
            GRAF[Grafana<br/>Dashboards]
            ELK[ELK Stack<br/>Log Aggregation]
        end
    end
    
    subgraph "Edge Locations"
        EDGE1[Edge Location 1<br/>CDN + Cache]
        EDGE2[Edge Location 2<br/>CDN + Cache]
        EDGEN[Edge Location N<br/>CDN + Cache]
    end
    
    subgraph "Client Access"
        CLIENTS[Client Browsers<br/>Global Users]
        AGENTS[Windows Agents<br/>Distributed Machines]
    end
    
    %% Traffic routing
    CLIENTS --> EDGE1
    CLIENTS --> EDGE2
    CLIENTS --> EDGEN
    
    EDGE1 --> LB1
    EDGE2 --> LB2
    EDGEN --> LB1
    
    LB1 --> WEB1
    LB1 --> WEB2
    LB2 --> WEBN
    
    LB1 --> API1
    LB1 --> API2
    LB2 --> APIN
    
    AGENTS --> REL1
    AGENTS --> REL2
    AGENTS --> RELN
    
    %% Database connections
    API1 --> MONGO_P
    API2 --> MONGO_S1
    APIN --> MONGO_S2
    
    API1 --> REDIS_M
    API2 --> REDIS_S1
    APIN --> REDIS_S2
    
    REL1 --> REDIS_M
    REL2 --> REDIS_S1
    RELN --> REDIS_S2
    
    %% Replication
    MONGO_P -.-> MONGO_S1
    MONGO_P -.-> MONGO_S2
    REDIS_M -.-> REDIS_S1
    REDIS_M -.-> REDIS_S2
    
    %% Monitoring
    API1 --> PROM
    API2 --> PROM
    APIN --> PROM
    PROM --> GRAF
    API1 --> ELK
    API2 --> ELK
    APIN --> ELK
    
    classDef lb fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef app fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef data fill:#e8f5e8,stroke:#388e3c,stroke-width:2px
    classDef monitor fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef edge fill:#ffebee,stroke:#d32f2f,stroke-width:2px
    classDef client fill:#f1f8e9,stroke:#689f38,stroke-width:2px
    
    class LB1,LB2 lb
    class WEB1,WEB2,WEBN,API1,API2,APIN,REL1,REL2,RELN app
    class MONGO_P,MONGO_S1,MONGO_S2,REDIS_M,REDIS_S1,REDIS_S2 data
    class PROM,GRAF,ELK monitor
    class EDGE1,EDGE2,EDGEN edge
    class CLIENTS,AGENTS client
```

## Key Technical Specifications

### Performance Characteristics
- **Concurrent Users**: Up to 10,000 simultaneous users
- **RDP Sessions**: Up to 1,000 concurrent RDP sessions
- **Response Time**: < 200ms for API calls, < 100ms for cached data
- **Throughput**: 10,000 requests/second peak load
- **Database**: 1TB+ storage capacity with 99.9% uptime
- **WebSocket Connections**: Up to 50,000 concurrent connections

### Scalability Metrics
- **Horizontal Scaling**: Auto-scaling based on CPU/memory usage
- **Database Sharding**: Tenant-based sharding for multi-tenancy
- **Cache Distribution**: Redis Cluster for distributed caching
- **Load Distribution**: Round-robin with health checks
- **Geographic Distribution**: Multi-region deployment support

### Security Standards
- **Authentication**: OAuth 2.0 + JWT with RS256 signing
- **Authorization**: Role-based access control (RBAC)
- **Encryption**: TLS 1.3 for transport, AES-256 for data at rest
- **Password Policy**: BCrypt with 12 rounds, minimum 8 characters
- **Session Management**: Secure token rotation with blacklisting
- **Audit Compliance**: SOC 2, GDPR, and HIPAA ready

### Technology Versions
- **.NET**: 9.0 LTS
- **React**: 18.2.0 with TypeScript 5.0+
- **MongoDB**: 7.0 with replica sets
- **Redis**: 7.2 with cluster mode
- **nginx**: 1.25+ with HTTP/2 and TLS 1.3
- **Docker**: 24.0+ with Compose v2
- **Node.js**: 18.0+ LTS for build tools
