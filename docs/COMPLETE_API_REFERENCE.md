# 📡 RDP Relay Platform - Complete API Reference

## Table of Contents
1. [Authentication Endpoints](#authentication-endpoints)
2. [User Management](#user-management)
3. [Tenant Management](#tenant-management)
4. [Agent Management](#agent-management)
5. [Session Management](#session-management)
6. [Health & Monitoring](#health--monitoring)
7. [Error Handling](#error-handling)
8. [Rate Limiting](#rate-limiting)
9. [WebSocket API](#websocket-api)

---

## Base Configuration

**Base URL**: `http://localhost:8080/api` (via nginx proxy)
**Direct API**: `http://localhost:5000` (development only)
**Content-Type**: `application/json`
**Authentication**: JWT Bearer Token

### Common Headers
```http
Authorization: Bearer <jwt_token>
Content-Type: application/json
Accept: application/json
```

---

## 1. Authentication Endpoints

### POST /api/auth/login
Authenticate user and receive JWT access token.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Success Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4=",
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

**Error Response (401):**
```json
{
  "message": "Invalid email or password"
}
```

---

### POST /api/auth/refresh
Refresh JWT token using refresh token.

**Request Body:**
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4="
}
```

**Success Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4=",
  "expiresAt": "2025-08-27T20:00:00Z"
}
```

---

### POST /api/auth/logout
Logout and invalidate refresh token.

**Headers Required:**
```http
Authorization: Bearer <token>
```

**Success Response (200):**
```json
{
  "message": "Logged out successfully"
}
```

---

### GET /api/auth/me
Get current authenticated user information.

**Headers Required:**
```http
Authorization: Bearer <token>
```

**Success Response (200):**
```json
{
  "id": "64f...",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "SystemAdmin",
  "tenantId": "64f..."
}
```

---

## 2. User Management

### GET /api/users
Get all users in the current tenant (Admin/TenantAdmin only).

**Headers Required:**
```http
Authorization: Bearer <token>
```

**Query Parameters:**
- `skip` (int, optional): Number of records to skip (default: 0)
- `limit` (int, optional): Number of records to return (default: 50, max: 100)
- `search` (string, optional): Search by email or name
- `role` (string, optional): Filter by user role
- `isActive` (boolean, optional): Filter by active status

**Example Request:**
```http
GET /api/users?skip=0&limit=20&search=john&role=Operator&isActive=true
```

**Success Response (200):**
```json
{
  "items": [
    {
      "id": "64f...",
      "email": "john.doe@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Operator",
      "isActive": true,
      "createdAt": "2025-08-26T10:00:00Z",
      "lastLoginAt": "2025-08-27T08:30:00Z"
    }
  ],
  "total": 1,
  "skip": 0,
  "limit": 20
}
```

---

### POST /api/users
Create new user (Admin/TenantAdmin only).

**Request Body:**
```json
{
  "email": "newuser@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "password": "securePassword123",
  "role": "Operator",
  "tenantId": "64f..." // Optional, defaults to current user's tenant
}
```

**Success Response (201):**
```json
{
  "id": "64f...",
  "email": "newuser@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "role": "Operator",
  "isActive": true,
  "createdAt": "2025-08-27T10:00:00Z",
  "tenantId": "64f..."
}
```

**Error Response (400):**
```json
{
  "message": "User with email newuser@example.com already exists"
}
```

---

### GET /api/users/{id}
Get specific user by ID.

**Success Response (200):**
```json
{
  "id": "64f...",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Operator",
  "isActive": true,
  "createdAt": "2025-08-26T10:00:00Z",
  "updatedAt": "2025-08-27T08:30:00Z",
  "lastLoginAt": "2025-08-27T08:30:00Z",
  "tenantId": "64f..."
}
```

---

### PUT /api/users/{id}
Update user information.

**Request Body:**
```json
{
  "firstName": "John Updated",
  "lastName": "Doe Updated",
  "role": "TenantAdmin",
  "isActive": false,
  "password": "newPassword123" // Optional
}
```

**Success Response (200):**
```json
{
  "id": "64f...",
  "email": "user@example.com",
  "firstName": "John Updated",
  "lastName": "Doe Updated",
  "role": "TenantAdmin",
  "isActive": false,
  "updatedAt": "2025-08-27T10:15:00Z"
}
```

---

### DELETE /api/users/{id}
Delete user (cannot delete own account).

**Success Response (204):**
```
No Content
```

**Error Response (400):**
```json
{
  "message": "Cannot delete your own account"
}
```

---

## 3. Tenant Management

### GET /api/tenants
Get all tenants (SystemAdmin only).

**Query Parameters:**
- `skip` (int, optional): Pagination offset
- `limit` (int, optional): Number of items to return
- `search` (string, optional): Search by name or domain
- `isActive` (boolean, optional): Filter by active status

**Success Response (200):**
```json
{
  "items": [
    {
      "id": "64f...",
      "name": "Acme Corporation",
      "domain": "acme.local",
      "isActive": true,
      "createdAt": "2025-08-26T10:00:00Z",
      "settings": {
        "maxAgents": 100,
        "maxConcurrentSessions": 50,
        "sessionTimeoutMinutes": 480,
        "requireTls": true,
        "allowedIpRanges": ["192.168.1.0/24", "10.0.0.0/8"]
      }
    }
  ],
  "total": 1,
  "skip": 0,
  "limit": 50
}
```

---

### POST /api/tenants
Create new tenant (SystemAdmin only).

**Request Body:**
```json
{
  "name": "New Company",
  "domain": "newcompany.local",
  "maxAgents": 50,
  "maxConcurrentSessions": 25,
  "sessionTimeoutMinutes": 240,
  "allowedIpRanges": ["192.168.100.0/24"]
}
```

**Success Response (201):**
```json
{
  "id": "64f...",
  "name": "New Company",
  "domain": "newcompany.local",
  "isActive": true,
  "createdAt": "2025-08-27T10:00:00Z",
  "settings": {
    "maxAgents": 50,
    "maxConcurrentSessions": 25,
    "sessionTimeoutMinutes": 240,
    "requireTls": true,
    "allowedIpRanges": ["192.168.100.0/24"]
  }
}
```

---

### GET /api/tenants/{id}
Get specific tenant details.

### PUT /api/tenants/{id}
Update tenant settings.

### DELETE /api/tenants/{id}
Delete tenant and all associated data.

---

## 4. Agent Management

### GET /api/agents
Get all agents for current tenant.

**Query Parameters:**
- `skip` (int, optional): Pagination offset
- `limit` (int, optional): Number of items to return
- `status` (string, optional): Filter by status (Online, Offline, InSession, Error)
- `search` (string, optional): Search by machine name or IP

**Success Response (200):**
```json
{
  "items": [
    {
      "id": "64f...",
      "machineName": "DESKTOP-ABC123",
      "ipAddress": "192.168.1.100",
      "status": "Online",
      "lastHeartbeat": "2025-08-27T10:00:00Z",
      "createdAt": "2025-08-26T09:00:00Z",
      "systemInfo": {
        "osVersion": "Windows 11 Pro",
        "cpuCores": 8,
        "totalRam": 16384,
        "availableRam": 12288,
        "diskSpace": 500
      }
    }
  ],
  "total": 1,
  "skip": 0,
  "limit": 50
}
```

---

### POST /api/agents/register
Register new agent (called by Windows agent).

**Request Body:**
```json
{
  "machineName": "DESKTOP-XYZ789",
  "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFA...",
  "systemInfo": {
    "osVersion": "Windows Server 2022",
    "cpuCores": 16,
    "totalRam": 32768,
    "availableRam": 28672,
    "diskSpace": 1000
  }
}
```

**Success Response (201):**
```json
{
  "agentId": "64f...",
  "agentKey": "base64_encoded_key",
  "provisioningJwt": "eyJhbGciOiJIUzI1NiIs...",
  "relayServerUrl": "wss://localhost:9443/agent/ws",
  "heartbeatIntervalSeconds": 60
}
```

---

### PUT /api/agents/{id}/heartbeat
Update agent heartbeat (called by Windows agent).

**Request Body:**
```json
{
  "status": "Online",
  "ipAddress": "192.168.1.100",
  "systemInfo": {
    "availableRam": 12000,
    "diskSpace": 480
  },
  "activeSessions": 1
}
```

**Success Response (200):**
```json
{
  "acknowledged": true,
  "nextHeartbeatIn": 60
}
```

---

### DELETE /api/agents/{id}
Deregister agent.

**Success Response (204):**
```
No Content
```

---

### POST /api/agents/{id}/restart
Restart agent remotely (Admin only).

**Success Response (200):**
```json
{
  "message": "Restart command sent to agent"
}
```

---

## 5. Session Management

### GET /api/sessions
Get user's sessions or all sessions (for admins).

**Query Parameters:**
- `skip` (int, optional): Pagination offset
- `limit` (int, optional): Number of items to return
- `status` (string, optional): Filter by status (Pending, Active, Ended, Error)
- `agentId` (string, optional): Filter by agent ID
- `userId` (string, optional): Filter by user ID (admin only)

**Success Response (200):**
```json
{
  "items": [
    {
      "id": "64f...",
      "userId": "64f...",
      "agentId": "64f...",
      "status": "Active",
      "connectCode": "123456",
      "startedAt": "2025-08-27T10:00:00Z",
      "duration": 3600,
      "agent": {
        "machineName": "DESKTOP-ABC123",
        "ipAddress": "192.168.1.100"
      },
      "user": {
        "firstName": "John",
        "lastName": "Doe",
        "email": "john.doe@example.com"
      }
    }
  ],
  "total": 1,
  "skip": 0,
  "limit": 50
}
```

---

### POST /api/sessions
Create new RDP session.

**Request Body:**
```json
{
  "agentId": "64f...",
  "notes": "Remote troubleshooting session"
}
```

**Success Response (201):**
```json
{
  "sessionId": "64f...",
  "connectCode": "123456",
  "webSocketUrl": "wss://localhost:9443/sessions/64f...",
  "expiresAt": "2025-08-27T18:00:00Z",
  "status": "Pending"
}
```

**Error Response (409):**
```json
{
  "message": "Agent is already in use by another session"
}
```

---

### GET /api/sessions/{id}
Get specific session details.

**Success Response (200):**
```json
{
  "id": "64f...",
  "userId": "64f...",
  "agentId": "64f...",
  "status": "Active",
  "connectCode": "123456",
  "startedAt": "2025-08-27T10:00:00Z",
  "endedAt": null,
  "duration": 3600,
  "disconnectReason": null,
  "webSocketUrl": "wss://localhost:9443/sessions/64f...",
  "agent": {
    "machineName": "DESKTOP-ABC123",
    "ipAddress": "192.168.1.100",
    "systemInfo": {
      "osVersion": "Windows 11 Pro"
    }
  }
}
```

---

### DELETE /api/sessions/{id}
Terminate active session.

**Success Response (200):**
```json
{
  "message": "Session terminated successfully"
}
```

**Error Response (400):**
```json
{
  "message": "Session is not active"
}
```

---

### POST /api/sessions/{id}/extend
Extend session timeout.

**Request Body:**
```json
{
  "additionalMinutes": 120
}
```

**Success Response (200):**
```json
{
  "expiresAt": "2025-08-27T20:00:00Z",
  "message": "Session extended successfully"
}
```

---

## 6. Health & Monitoring

### GET /api/health
Get API health status.

**Success Response (200):**
```json
{
  "status": "Healthy",
  "timestamp": "2025-08-27T10:00:00Z",
  "version": "1.0.0",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "relay_server": "Healthy"
  }
}
```

---

### GET /api/stats
Get platform statistics (Admin only).

**Success Response (200):**
```json
{
  "totalTenants": 5,
  "totalUsers": 150,
  "totalAgents": 75,
  "onlineAgents": 68,
  "activeSessions": 12,
  "totalSessionsToday": 45,
  "generatedAt": "2025-08-27T10:00:00Z"
}
```

---

### GET /api/stats/agents
Get agent statistics.

**Query Parameters:**
- `period` (string): Time period (hour, day, week, month)

**Success Response (200):**
```json
{
  "period": "day",
  "data": [
    {
      "timestamp": "2025-08-27T00:00:00Z",
      "onlineCount": 65,
      "offlineCount": 10,
      "inSessionCount": 8,
      "errorCount": 2
    }
  ]
}
```

---

## 7. Error Handling

### Standard Error Response Format
```json
{
  "message": "Error description",
  "code": "ERROR_CODE",
  "timestamp": "2025-08-27T10:00:00Z",
  "traceId": "abc123...",
  "details": {
    "field": "Specific field error"
  }
}
```

### HTTP Status Codes
- **200**: Success
- **201**: Created
- **204**: No Content
- **400**: Bad Request (validation errors)
- **401**: Unauthorized (invalid/missing token)
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found
- **409**: Conflict (resource already exists)
- **429**: Too Many Requests (rate limited)
- **500**: Internal Server Error

### Common Error Codes
- `INVALID_CREDENTIALS`: Login failed
- `TOKEN_EXPIRED`: JWT token has expired
- `INSUFFICIENT_PERMISSIONS`: User lacks required permissions
- `RESOURCE_NOT_FOUND`: Requested resource doesn't exist
- `VALIDATION_ERROR`: Request data validation failed
- `AGENT_UNAVAILABLE`: Agent is offline or busy
- `SESSION_LIMIT_EXCEEDED`: Too many active sessions
- `TENANT_QUOTA_EXCEEDED`: Tenant limits reached

---

## 8. Rate Limiting

### Rate Limit Headers
All API responses include rate limiting headers:
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1635724800
```

### Rate Limits by Endpoint Type
- **Authentication**: 5 requests per minute
- **General API**: 100 requests per minute
- **Health checks**: No limit
- **WebSocket connections**: 10 connections per minute

### Rate Limit Exceeded Response (429)
```json
{
  "message": "Rate limit exceeded",
  "code": "RATE_LIMIT_EXCEEDED",
  "retryAfter": 60
}
```

---

## 9. WebSocket API

### Connection Endpoint
```
wss://localhost:9443/sessions/{sessionId}
```

### Authentication
Include JWT token in query parameter:
```
wss://localhost:9443/sessions/64f...?token=eyJhbGciOiJIUzI1NiIs...
```

### Message Format
All WebSocket messages use JSON format:
```json
{
  "type": "message_type",
  "data": {...},
  "timestamp": "2025-08-27T10:00:00Z"
}
```

### Message Types

#### Client → Server Messages

**Connect to RDP Session:**
```json
{
  "type": "connect_rdp",
  "data": {
    "connectCode": "123456",
    "screenResolution": {
      "width": 1920,
      "height": 1080
    }
  }
}
```

**Send RDP Data:**
```json
{
  "type": "rdp_data",
  "data": {
    "payload": "base64_encoded_rdp_data"
  }
}
```

**Disconnect Session:**
```json
{
  "type": "disconnect",
  "data": {
    "reason": "user_request"
  }
}
```

#### Server → Client Messages

**Connection Established:**
```json
{
  "type": "connected",
  "data": {
    "sessionId": "64f...",
    "agentInfo": {
      "machineName": "DESKTOP-ABC123",
      "osVersion": "Windows 11 Pro"
    }
  }
}
```

**RDP Data Stream:**
```json
{
  "type": "rdp_data",
  "data": {
    "payload": "base64_encoded_rdp_data"
  }
}
```

**Session Status Updates:**
```json
{
  "type": "status_update",
  "data": {
    "status": "active",
    "duration": 1800,
    "bandwidth": {
      "upload": "2.5 Mbps",
      "download": "8.1 Mbps"
    }
  }
}
```

**Error Messages:**
```json
{
  "type": "error",
  "data": {
    "code": "CONNECTION_FAILED",
    "message": "Failed to establish RDP connection",
    "fatal": true
  }
}
```

**Session Terminated:**
```json
{
  "type": "disconnected",
  "data": {
    "reason": "timeout",
    "duration": 3600,
    "message": "Session ended due to inactivity"
  }
}
```

---

## Example Usage

### Complete Authentication Flow
```javascript
// 1. Login
const loginResponse = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'test@test.com',
    password: 'password'
  })
});

const { token, user } = await loginResponse.json();

// 2. Get available agents
const agentsResponse = await fetch('/api/agents', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const { items: agents } = await agentsResponse.json();

// 3. Create session
const sessionResponse = await fetch('/api/sessions', {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json' 
  },
  body: JSON.stringify({
    agentId: agents[0].id
  })
});

const { sessionId, webSocketUrl } = await sessionResponse.json();

// 4. Connect via WebSocket
const ws = new WebSocket(`${webSocketUrl}?token=${token}`);
ws.onopen = () => {
  ws.send(JSON.stringify({
    type: 'connect_rdp',
    data: { connectCode: '123456' }
  }));
};
```

---

*This API reference covers all endpoints available in the RDP Relay platform. For implementation details and examples, refer to the source code in the respective controller files.*
