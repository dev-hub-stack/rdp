# RDP Relay Platform - API Reference Documentation

## Authentication & Authorization

All API endpoints (except login/register) require JWT authentication via the Authorization header:
```
Authorization: Bearer <jwt_token>
```

### Base URL
- **Development**: `http://localhost:5000`
- **Production**: `https://your-domain.com/api`

---

## Authentication Endpoints

### POST /api/auth/login
Authenticate user and receive JWT tokens.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "PhmEAbDpDLTlxg7TWuR4I8xO1qzFC8FqHdp...",
  "expiresAt": "2025-08-26T21:18:55.2617423Z",
  "user": {
    "id": "68ae16a34a4bd0ff3c89b03d",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "SystemAdmin",
    "tenantId": "68ae09d3096d59594289b03d"
  }
}
```

**Error Responses:**
- `401` - Invalid credentials
- `403` - Account disabled
- `429` - Too many login attempts

---

### POST /api/auth/refresh
Refresh expired access token using refresh token.

**Request Body:**
```json
{
  "refreshToken": "PhmEAbDpDLTlxg7TWuR4I8xO1qzFC8FqHdp..."
}
```

**Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "NewRefreshTokenHere...",
  "expiresAt": "2025-08-26T22:18:55.2617423Z"
}
```

---

### POST /api/auth/logout
Logout user and invalidate tokens.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "message": "Logged out successfully"
}
```

---

### GET /api/auth/me
Get current authenticated user information.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "id": "68ae16a34a4bd0ff3c89b03d",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "SystemAdmin",
  "tenantId": "68ae09d3096d59594289b03d"
}
```

---

## User Management Endpoints

### GET /api/users
Get paginated list of users (TenantAdmin+ only).

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `skip` (optional): Number of records to skip (default: 0)
- `limit` (optional): Number of records to return (default: 20, max: 100)
- `search` (optional): Search term for email/name
- `role` (optional): Filter by user role
- `isActive` (optional): Filter by active status

**Response (200):**
```json
{
  "items": [
    {
      "id": "68ae16a34a4bd0ff3c89b03d",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Operator",
      "isActive": true,
      "createdAt": "2025-08-26T19:24:03.459Z",
      "lastLoginAt": "2025-08-26T20:15:30.123Z"
    }
  ],
  "total": 25,
  "skip": 0,
  "limit": 20
}
```

---

### GET /api/users/{id}
Get specific user by ID.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "id": "68ae16a34a4bd0ff3c89b03d",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Operator",
  "isActive": true,
  "tenantId": "68ae09d3096d59594289b03d",
  "createdAt": "2025-08-26T19:24:03.459Z",
  "updatedAt": "2025-08-26T19:24:03.459Z",
  "lastLoginAt": "2025-08-26T20:15:30.123Z"
}
```

---

### POST /api/users
Create new user (TenantAdmin+ only).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "email": "newuser@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "password": "securePassword123",
  "role": "Operator",
  "tenantId": "68ae09d3096d59594289b03d"
}
```

**Response (201):**
```json
{
  "id": "68ae16a34a4bd0ff3c89b03e",
  "email": "newuser@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "role": "Operator",
  "isActive": true,
  "tenantId": "68ae09d3096d59594289b03d",
  "createdAt": "2025-08-26T20:30:15.789Z"
}
```

---

### PUT /api/users/{id}
Update existing user (TenantAdmin+ only).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "email": "updated@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "role": "TenantAdmin",
  "isActive": true
}
```

**Response (200):** Updated user object

---

### DELETE /api/users/{id}
Delete user (TenantAdmin+ only).

**Headers:** `Authorization: Bearer <token>`

**Response (204):** No content

---

## Agent Management Endpoints

### GET /api/agents
Get paginated list of agents.

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `skip` (optional): Number of records to skip
- `limit` (optional): Number of records to return
- `status` (optional): Filter by agent status (Online, Offline, InSession, Error)
- `search` (optional): Search by machine name

**Response (200):**
```json
{
  "items": [
    {
      "id": "68ae16a34a4bd0ff3c89b03f",
      "tenantId": "68ae09d3096d59594289b03d",
      "machineName": "WIN-SERVER-01",
      "ipAddress": "192.168.1.100",
      "version": "1.0.0",
      "status": "Online",
      "lastHeartbeat": "2025-08-26T20:29:45.123Z",
      "capabilities": {
        "rdpEnabled": true,
        "multiSession": true,
        "maxSessions": 5
      },
      "systemInfo": {
        "os": "Windows Server 2022",
        "memory": 16,
        "cpu": "Intel Xeon E5-2680 v4",
        "disk": 500
      },
      "createdAt": "2025-08-26T10:00:00.000Z",
      "updatedAt": "2025-08-26T20:29:45.123Z"
    }
  ],
  "total": 10,
  "skip": 0,
  "limit": 20
}
```

---

### GET /api/agents/{id}
Get specific agent by ID.

**Headers:** `Authorization: Bearer <token>`

**Response (200):** Agent object with full details

---

### POST /api/agents
Register new agent (typically called by agent software).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "machineName": "WIN-DESKTOP-02",
  "ipAddress": "192.168.1.101",
  "version": "1.0.0",
  "capabilities": {
    "rdpEnabled": true,
    "multiSession": false,
    "maxSessions": 1
  },
  "systemInfo": {
    "os": "Windows 11 Pro",
    "memory": 32,
    "cpu": "AMD Ryzen 7 5800X",
    "disk": 1000
  }
}
```

**Response (201):** Created agent object with ID

---

### PUT /api/agents/{id}
Update agent information.

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "status": "Online",
  "capabilities": {
    "rdpEnabled": true,
    "multiSession": true,
    "maxSessions": 3
  },
  "systemInfo": {
    "memory": 32,
    "disk": 800
  }
}
```

**Response (200):** Updated agent object

---

### DELETE /api/agents/{id}
Remove agent from system (TenantAdmin+ only).

**Headers:** `Authorization: Bearer <token>`

**Response (204):** No content

---

### POST /api/agents/{id}/heartbeat
Send agent heartbeat (called by agent software).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "status": "Online",
  "activeSessions": 2,
  "systemMetrics": {
    "cpuUsage": 45.2,
    "memoryUsage": 68.7,
    "diskUsage": 23.1
  }
}
```

**Response (200):**
```json
{
  "acknowledged": true,
  "nextHeartbeatInterval": 30
}
```

---

## Session Management Endpoints

### GET /api/sessions
Get paginated list of sessions.

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `skip` (optional): Number of records to skip
- `limit` (optional): Number of records to return
- `status` (optional): Filter by session status (Pending, Active, Ended, Error)
- `userId` (optional): Filter by user ID
- `agentId` (optional): Filter by agent ID
- `dateFrom` (optional): Filter sessions from date (ISO 8601)
- `dateTo` (optional): Filter sessions to date (ISO 8601)

**Response (200):**
```json
{
  "items": [
    {
      "id": "68ae16a34a4bd0ff3c89b040",
      "tenantId": "68ae09d3096d59594289b03d",
      "userId": "68ae16a34a4bd0ff3c89b03d",
      "agentId": "68ae16a34a4bd0ff3c89b03f",
      "connectCode": "RDP-2025-001234",
      "status": "Active",
      "startTime": "2025-08-26T20:00:00.000Z",
      "endTime": null,
      "connectionInfo": {
        "clientIp": "203.0.113.45",
        "screenResolution": "1920x1080",
        "colorDepth": 32
      },
      "metadata": {
        "duration": 1800,
        "dataTransferred": 52428800
      },
      "createdAt": "2025-08-26T20:00:00.000Z"
    }
  ],
  "total": 15,
  "skip": 0,
  "limit": 20
}
```

---

### GET /api/sessions/{id}
Get specific session by ID.

**Headers:** `Authorization: Bearer <token>`

**Response (200):** Session object with full details

---

### POST /api/sessions
Create new RDP session.

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "agentId": "68ae16a34a4bd0ff3c89b03f",
  "connectionInfo": {
    "screenResolution": "1920x1080",
    "colorDepth": 32
  },
  "metadata": {
    "purpose": "System Administration",
    "estimatedDuration": 3600
  }
}
```

**Response (201):**
```json
{
  "id": "68ae16a34a4bd0ff3c89b041",
  "connectCode": "RDP-2025-001235",
  "status": "Pending",
  "connectionUrl": "rdp://relay.example.com:9443/session/RDP-2025-001235",
  "credentials": {
    "username": "rdp_user_001235",
    "password": "temp_password_xyz789"
  },
  "createdAt": "2025-08-26T20:35:00.000Z"
}
```

---

### PUT /api/sessions/{id}
Update session (typically status changes).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "status": "Ended",
  "endTime": "2025-08-26T21:35:00.000Z",
  "metadata": {
    "duration": 3600,
    "dataTransferred": 104857600,
    "endReason": "User requested"
  }
}
```

**Response (200):** Updated session object

---

### DELETE /api/sessions/{id}
Terminate active session.

**Headers:** `Authorization: Bearer <token>`

**Response (204):** No content

---

### GET /api/sessions/{id}/logs
Get session connection logs.

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `level` (optional): Filter by log level (Info, Warning, Error)
- `limit` (optional): Number of log entries to return

**Response (200):**
```json
{
  "logs": [
    {
      "timestamp": "2025-08-26T20:00:15.123Z",
      "level": "Info",
      "message": "RDP session established successfully",
      "details": {
        "clientIp": "203.0.113.45",
        "agentId": "68ae16a34a4bd0ff3c89b03f"
      }
    },
    {
      "timestamp": "2025-08-26T20:15:30.456Z",
      "level": "Warning", 
      "message": "High bandwidth usage detected",
      "details": {
        "bandwidthMbps": 25.4
      }
    }
  ],
  "total": 245
}
```

---

## Tenant Management Endpoints (SystemAdmin only)

### GET /api/tenants
Get paginated list of tenants.

**Headers:** `Authorization: Bearer <token>` (SystemAdmin role required)

**Query Parameters:**
- `skip` (optional): Number of records to skip
- `limit` (optional): Number of records to return
- `isActive` (optional): Filter by active status
- `search` (optional): Search by name or domain

**Response (200):**
```json
{
  "items": [
    {
      "id": "68ae09d3096d59594289b03d",
      "name": "Acme Corporation",
      "domain": "acme.local",
      "isActive": true,
      "createdAt": "2025-08-26T10:00:00.000Z",
      "updatedAt": "2025-08-26T15:30:00.000Z",
      "settings": {
        "maxAgents": 100,
        "maxConcurrentSessions": 50,
        "sessionTimeoutMinutes": 480,
        "requireTls": true,
        "allowedIpRanges": ["203.0.113.0/24", "198.51.100.0/24"]
      },
      "statistics": {
        "totalUsers": 25,
        "totalAgents": 15,
        "activeSessions": 8
      }
    }
  ],
  "total": 5,
  "skip": 0,
  "limit": 20
}
```

---

### GET /api/tenants/{id}
Get specific tenant by ID.

**Headers:** `Authorization: Bearer <token>` (SystemAdmin role required)

**Response (200):** Tenant object with full details

---

### POST /api/tenants
Create new tenant.

**Headers:** `Authorization: Bearer <token>` (SystemAdmin role required)

**Request Body:**
```json
{
  "name": "New Company Inc",
  "domain": "newcompany.local",
  "settings": {
    "maxAgents": 50,
    "maxConcurrentSessions": 25,
    "sessionTimeoutMinutes": 240,
    "requireTls": true,
    "allowedIpRanges": ["192.168.1.0/24"]
  }
}
```

**Response (201):** Created tenant object

---

### PUT /api/tenants/{id}
Update existing tenant.

**Headers:** `Authorization: Bearer <token>` (SystemAdmin role required)

**Request Body:**
```json
{
  "name": "Updated Company Name",
  "isActive": true,
  "settings": {
    "maxAgents": 75,
    "maxConcurrentSessions": 40,
    "sessionTimeoutMinutes": 360,
    "allowedIpRanges": ["192.168.1.0/24", "10.0.0.0/16"]
  }
}
```

**Response (200):** Updated tenant object

---

### DELETE /api/tenants/{id}
Delete tenant (also removes all associated data).

**Headers:** `Authorization: Bearer <token>` (SystemAdmin role required)

**Response (204):** No content

---

## Statistics & Reporting Endpoints

### GET /api/stats/overview
Get system overview statistics.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "totalTenants": 5,
  "totalUsers": 125,
  "totalAgents": 45,
  "onlineAgents": 38,
  "activeSessions": 23,
  "totalSessionsToday": 156,
  "generatedAt": "2025-08-26T20:45:00.000Z"
}
```

---

### GET /api/stats/sessions
Get session statistics with filters.

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `period` (optional): Time period (hour, day, week, month)
- `dateFrom` (optional): Start date for custom period
- `dateTo` (optional): End date for custom period
- `tenantId` (optional): Filter by tenant (SystemAdmin only)

**Response (200):**
```json
{
  "period": "day",
  "data": [
    {
      "timestamp": "2025-08-26T00:00:00.000Z",
      "totalSessions": 24,
      "activeSessions": 3,
      "averageDuration": 3240,
      "totalDataTransferred": 2147483648
    },
    {
      "timestamp": "2025-08-26T01:00:00.000Z",
      "totalSessions": 18,
      "activeSessions": 2,
      "averageDuration": 2890,
      "totalDataTransferred": 1610612736
    }
  ],
  "summary": {
    "totalSessions": 524,
    "averageDuration": 3150,
    "totalDataTransferred": 52428800000,
    "peakConcurrentSessions": 45
  }
}
```

---

### GET /api/stats/agents
Get agent statistics and performance metrics.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "totalAgents": 45,
  "onlineAgents": 38,
  "agentsByStatus": {
    "Online": 38,
    "Offline": 5,
    "InSession": 2,
    "Error": 0
  },
  "agentsByOS": {
    "Windows Server 2022": 25,
    "Windows 11": 15,
    "Windows 10": 5
  },
  "averageResourceUsage": {
    "cpu": 35.2,
    "memory": 45.8,
    "disk": 28.1
  },
  "topPerformers": [
    {
      "agentId": "68ae16a34a4bd0ff3c89b03f",
      "machineName": "WIN-SERVER-01",
      "uptime": 99.8,
      "avgResponseTime": 25
    }
  ]
}
```

---

## Error Response Format

All endpoints return errors in the following standardized format:

```json
{
  "message": "Human readable error message",
  "code": "ERROR_CODE_IDENTIFIER",
  "details": {
    "field": "Specific field error details",
    "validationErrors": [
      {
        "field": "email",
        "message": "Invalid email format"
      }
    ]
  },
  "timestamp": "2025-08-26T20:45:00.000Z",
  "traceId": "12345678-1234-5678-9abc-123456789012"
}
```

### Common HTTP Status Codes

- `200` - Success
- `201` - Created successfully
- `204` - No content (successful deletion)
- `400` - Bad request (validation errors)
- `401` - Unauthorized (invalid/expired token)
- `403` - Forbidden (insufficient permissions)
- `404` - Resource not found
- `409` - Conflict (duplicate resource)
- `422` - Unprocessable entity (business logic error)
- `429` - Too many requests (rate limited)
- `500` - Internal server error
- `503` - Service unavailable

---

## Rate Limiting

API endpoints are rate limited based on user role:

- **SystemAdmin**: 10,000 requests/hour
- **TenantAdmin**: 5,000 requests/hour
- **Operator**: 1,000 requests/hour
- **Unauthenticated**: 100 requests/hour

Rate limit headers are included in all responses:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 995
X-RateLimit-Reset: 1640995200
```

---

## WebSocket API (Real-time Updates)

### Connection Endpoint
```
ws://localhost:5001/hub (Development)
wss://relay.your-domain.com/hub (Production)
```

### Authentication
WebSocket connections require JWT token in the query string:
```
ws://localhost:5001/hub?access_token=<jwt_token>
```

### Event Types

#### Agent Status Updates
```json
{
  "type": "AgentStatusChanged",
  "data": {
    "agentId": "68ae16a34a4bd0ff3c89b03f",
    "status": "Online",
    "timestamp": "2025-08-26T20:45:00.000Z"
  }
}
```

#### Session State Changes
```json
{
  "type": "SessionStateChanged",
  "data": {
    "sessionId": "68ae16a34a4bd0ff3c89b040",
    "status": "Active",
    "timestamp": "2025-08-26T20:45:00.000Z"
  }
}
```

#### System Notifications
```json
{
  "type": "SystemNotification",
  "data": {
    "level": "Warning",
    "message": "High system load detected",
    "timestamp": "2025-08-26T20:45:00.000Z"
  }
}
```

### Client-to-Server Messages

#### Subscribe to Events
```json
{
  "type": "Subscribe",
  "events": ["AgentStatusChanged", "SessionStateChanged"]
}
```

#### Send Command
```json
{
  "type": "Command",
  "target": "session",
  "sessionId": "68ae16a34a4bd0ff3c89b040",
  "action": "terminate"
}
```

---

## SDK Examples

### JavaScript/TypeScript Client

```typescript
// API Client Class
class RdpRelayApiClient {
  private baseUrl: string;
  private token: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await fetch(`${this.baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    const data = await response.json();
    this.token = data.token;
    return data;
  }

  async getAgents(params?: GetAgentsParams): Promise<PagedResponse<Agent>> {
    const queryString = new URLSearchParams(params as any).toString();
    const response = await fetch(`${this.baseUrl}/api/agents?${queryString}`, {
      headers: { 'Authorization': `Bearer ${this.token}` }
    });

    return response.json();
  }

  async createSession(request: CreateSessionRequest): Promise<Session> {
    const response = await fetch(`${this.baseUrl}/api/sessions`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(request)
    });

    return response.json();
  }
}

// Usage Example
const client = new RdpRelayApiClient('http://localhost:5000');

try {
  const loginResult = await client.login('test@test.com', 'password');
  console.log('Logged in:', loginResult.user);

  const agents = await client.getAgents({ status: 'Online' });
  console.log('Available agents:', agents.items);

  const session = await client.createSession({
    agentId: agents.items[0].id,
    connectionInfo: {
      screenResolution: '1920x1080',
      colorDepth: 32
    }
  });
  console.log('Session created:', session);
} catch (error) {
  console.error('API Error:', error);
}
```

### C# Client

```csharp
// API Client Class
public class RdpRelayApiClient
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public RdpRelayApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var request = new LoginRequest { Email = email, Password = password };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _token = result.Token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        
        return result;
    }

    public async Task<PagedResponse<Agent>> GetAgentsAsync(GetAgentsParams? parameters = null)
    {
        var queryString = BuildQueryString(parameters);
        var response = await _httpClient.GetAsync($"/api/agents?{queryString}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<PagedResponse<Agent>>();
    }

    public async Task<Session> CreateSessionAsync(CreateSessionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/sessions", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<Session>();
    }
}

// Usage Example
var client = new RdpRelayApiClient("http://localhost:5000");

try
{
    var loginResult = await client.LoginAsync("test@test.com", "password");
    Console.WriteLine($"Logged in: {loginResult.User.Email}");

    var agents = await client.GetAgentsAsync(new GetAgentsParams { Status = "Online" });
    Console.WriteLine($"Available agents: {agents.Items.Count}");

    var session = await client.CreateSessionAsync(new CreateSessionRequest
    {
        AgentId = agents.Items.First().Id,
        ConnectionInfo = new ConnectionInfo
        {
            ScreenResolution = "1920x1080",
            ColorDepth = 32
        }
    });
    Console.WriteLine($"Session created: {session.ConnectCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
}
```

This API reference provides comprehensive documentation for all endpoints, request/response formats, authentication, error handling, and real-time communication capabilities of the RDP Relay Platform.
