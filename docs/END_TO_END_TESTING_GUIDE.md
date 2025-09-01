# RDP Relay Platform - End-to-End Testing Guide

## 🎯 Testing Overview

This guide provides comprehensive end-to-end testing procedures to validate the complete RDP Relay platform functionality, from authentication to active RDP sessions.

## 🔐 Test Credentials

**Test Account:**
```
Email: test@test.com
Password: password
Role: SystemAdmin
```

**Platform URLs:**
- **Web Portal**: http://localhost:8080
- **API Direct**: http://localhost:5000  
- **Relay Server**: http://localhost:5001

---

## 📋 Pre-Test Checklist

### 1. Container Health Check
```bash
# Verify all containers are running
docker-compose ps

# Expected output: All services should show "Up" status
# - rdp-relay-mongodb
# - rdp-relay-redis  
# - rdp-relay-portal-api
# - rdp-relay-portal-web
# - rdp-relay-relay-server
# - rdp-relay-nginx
```

### 2. Network Connectivity Test
```bash
# Test web portal
curl -I http://localhost:8080

# Test API health
curl http://localhost:5001/health

# Test database connectivity
docker exec rdp-relay-mongodb mongosh --eval "db.runCommand('ping')"
```

---

## 🧪 Test Suite 1: Authentication & Authorization

### Test 1.1: User Authentication
```bash
# Test login API
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"password"}' | jq .

# Expected Result:
# - Status: 200 OK
# - Response contains: token, refreshToken, expiresAt, user object
# - User role: "SystemAdmin"
```

### Test 1.2: JWT Token Validation
```bash
# Extract token from login response and test protected endpoint
TOKEN="<paste-token-here>"

curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/devices | jq .

# Expected Result:
# - Status: 200 OK
# - Returns device list (may be empty if no agents connected)
```

### Test 1.3: Web Portal Login
1. Open browser: http://localhost:8080
2. Enter credentials: `test@test.com` / `password`  
3. Click "Sign In"
4. **Expected Result**: Dashboard loads with system statistics

---

## 🖥️ Test Suite 2: Windows Agent Connectivity

### Test 2.1: Agent Registration Simulation
```bash
# Simulate agent heartbeat (manual test)
curl -X POST http://localhost:5001/api/agents/heartbeat \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "agentId": "test-agent-001",
    "hostname": "TEST-MACHINE-01",
    "ipAddress": "192.168.1.100",
    "status": "online",
    "rdpPort": 3389,
    "maxConnections": 5,
    "currentConnections": 0
  }'

# Expected Result: 
# - Status: 200 OK or 201 Created
# - Agent should appear in portal device list
```

### Test 2.2: Device Discovery
```bash
# Check if agent appears in device list
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/devices | jq .

# Expected Result:
# - Status: 200 OK
# - Array containing the test agent with status "online"
```

### Test 2.3: Agent Health Monitoring
```bash
# Test agent health endpoint
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/devices/test-agent-001/health | jq .

# Expected Result:
# - Status: 200 OK
# - Health status and connection details
```

---

## 🔗 Test Suite 3: RDP Session Management

### Test 3.1: Session Creation Request
```bash
# Request new RDP session
curl -X POST http://localhost:8080/api/sessions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "deviceId": "test-agent-001",
    "username": "testuser",
    "domain": "",
    "sessionType": "interactive"
  }' | jq .

# Expected Result:
# - Status: 201 Created
# - Session ID and connection details returned
```

### Test 3.2: WebSocket Connection Test
```bash
# Test WebSocket endpoint (requires WebSocket client)
# Use wscat tool: npm install -g wscat

wscat -c ws://localhost:5001/api/sessions/<session-id>/ws \
  -H "Authorization: Bearer $TOKEN"

# Expected Result:
# - WebSocket connection established
# - Receives session status updates
```

### Test 3.3: Session Status Monitoring
```bash
# Check active sessions
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/sessions | jq .

# Expected Result:
# - Status: 200 OK
# - List of active/recent sessions with status
```

---

## 🌐 Test Suite 4: Web Portal Functionality

### Test 4.1: Dashboard Display
1. Login to web portal
2. Navigate to Dashboard
3. **Verify displays:**
   - Total devices count
   - Active sessions count  
   - System health metrics
   - Recent activity log

### Test 4.2: Device Management
1. Navigate to Devices section
2. **Verify functionality:**
   - Device list displays
   - Device status indicators (online/offline)
   - Device details expandable
   - Connection test buttons

### Test 4.3: Session Management
1. Navigate to Sessions section
2. **Verify functionality:**
   - Active sessions list
   - Session history
   - Connect/disconnect controls
   - Session logs viewer

### Test 4.4: User Management (Admin)
1. Navigate to Users section
2. **Verify functionality:**
   - User list displays
   - Add new user form
   - Edit user details
   - Role assignment
   - Password reset options

---

## 🔧 Test Suite 5: System Integration

### Test 5.1: Load Balancing Test
```bash
# Test multiple concurrent API calls
for i in {1..10}; do
  curl -s -H "Authorization: Bearer $TOKEN" \
    http://localhost:8080/api/devices &
done
wait

# Expected Result:
# - All requests return 200 OK
# - Response times remain acceptable (<2s)
```

### Test 5.2: Database Persistence Test
```bash
# Create test data
curl -X POST http://localhost:8080/api/devices \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"hostname":"TEST-DB-PERSIST","ipAddress":"192.168.1.99"}'

# Restart containers
docker-compose restart

# Verify data persisted
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/devices | grep "TEST-DB-PERSIST"

# Expected Result: Data remains after restart
```

### Test 5.3: Security Headers Test  
```bash
# Test security headers
curl -I http://localhost:8080

# Expected headers:
# - X-Frame-Options: DENY
# - X-Content-Type-Options: nosniff
# - X-XSS-Protection: 1; mode=block
# - Strict-Transport-Security (if HTTPS)
```

---

## 📊 Test Suite 6: Performance & Monitoring

### Test 6.1: Response Time Benchmarking
```bash
# Benchmark API performance
for endpoint in "/api/devices" "/api/sessions" "/api/users"; do
  echo "Testing $endpoint:"
  time curl -s -H "Authorization: Bearer $TOKEN" \
    "http://localhost:8080$endpoint" > /dev/null
done

# Expected Results:
# - All endpoints < 500ms response time
# - No timeout errors
```

### Test 6.2: Memory Usage Monitoring
```bash
# Check container memory usage
docker stats --no-stream

# Expected Results:
# - No container using >80% available memory
# - Memory usage stable (not continuously growing)
```

### Test 6.3: Log Analysis
```bash
# Check for errors in logs
docker-compose logs | grep -i "error\|exception\|fail"

# Expected Result:
# - No critical errors or unhandled exceptions
# - Only expected warnings (if any)
```

---

## 🎯 Test Suite 7: Windows Agent Deployment

### Test 7.1: Agent Installation (Windows Required)
**Note: This test requires a Windows machine**

```powershell
# On Windows machine, run:
.\deploy-agent.sh

# Expected Results:
# - Agent builds successfully
# - Service installs without errors  
# - Service starts and remains running
# - Agent appears online in portal
```

### Test 7.2: RDP Connection Test (Windows Required)
**Prerequisites: Windows machine with RDP enabled**

1. Deploy Windows agent on target machine
2. Verify agent appears online in portal
3. Create RDP session through portal
4. Test connection using RDP client
5. **Expected Result**: Full RDP session established

---

## ✅ Test Results Checklist

### ✓ Authentication Tests
- [ ] API login successful
- [ ] JWT tokens valid
- [ ] Web portal login works
- [ ] Protected endpoints accessible

### ✓ Agent Connectivity Tests  
- [ ] Agent registration works
- [ ] Devices appear in portal
- [ ] Health monitoring functional
- [ ] Heartbeat mechanism active

### ✓ Session Management Tests
- [ ] Session creation successful
- [ ] WebSocket connections stable
- [ ] Status monitoring accurate
- [ ] Session cleanup proper

### ✓ Web Portal Tests
- [ ] Dashboard displays correctly
- [ ] Device management functional
- [ ] Session controls work
- [ ] User management complete

### ✓ System Integration Tests
- [ ] Load balancing effective
- [ ] Data persistence confirmed
- [ ] Security headers present
- [ ] Error handling proper

### ✓ Performance Tests
- [ ] Response times acceptable
- [ ] Memory usage stable
- [ ] No memory leaks detected
- [ ] Logs error-free

### ✓ Agent Deployment Tests
- [ ] Windows agent installs
- [ ] Service runs reliably  
- [ ] Portal integration works
- [ ] RDP sessions function

---

## 🚨 Troubleshooting Common Issues

### Issue: Authentication Fails
```bash
# Check JWT configuration
docker exec rdp-relay-portal-api printenv | grep JWT

# Verify database connection
docker exec rdp-relay-mongodb mongosh --eval "db.users.find()"
```

### Issue: Agent Not Appearing Online
```bash
# Check relay server logs
docker-compose logs relay-server | tail -20

# Verify agent configuration
cat agent-win/RdpRelay.Agent.Win/appsettings.json

# Test network connectivity
curl http://localhost:5001/health
```

### Issue: RDP Session Fails
```bash
# Check session logs
docker-compose logs relay-server | grep -i rdp

# Verify Windows agent logs
# (On Windows machine)
type "C:\Program Files\RdpRelay\Agent\logs\agent.log"
```

### Issue: Web Portal Not Loading
```bash
# Check nginx configuration
docker exec rdp-relay-nginx nginx -t

# Verify frontend build
docker-compose logs portal-web | tail -10

# Test direct frontend access
curl http://localhost:3000
```

---

## 🎉 Success Criteria

**Platform is considered fully functional when:**

1. ✅ All containers healthy and responsive
2. ✅ Authentication system working correctly  
3. ✅ Windows agents can connect and register
4. ✅ RDP sessions can be created and managed
5. ✅ Web portal displays all functionality
6. ✅ API endpoints respond within SLA limits
7. ✅ Database persistence confirmed
8. ✅ Security measures active and effective
9. ✅ Monitoring and logging operational
10. ✅ End-to-end RDP workflow completes successfully

**The RDP Relay Platform is now production-ready! 🚀**
