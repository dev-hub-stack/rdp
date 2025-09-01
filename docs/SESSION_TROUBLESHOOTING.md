# 🔧 Session Establishment Troubleshooting Guide

This guide provides comprehensive troubleshooting steps for resolving RDP session establishment issues in the RDP Relay platform.

## 📋 Table of Contents

- [Quick Diagnosis](#quick-diagnosis)
- [Common Issues](#common-issues)
- [Step-by-Step Troubleshooting](#step-by-step-troubleshooting)
- [Log Analysis](#log-analysis)
- [Known Issues and Solutions](#known-issues-and-solutions)
- [Advanced Debugging](#advanced-debugging)
- [Preventive Measures](#preventive-measures)

## 🚀 Quick Diagnosis

### Symptoms Checklist

Check which symptoms match your issue:

- [ ] **Session fails to establish** - Connection never starts
- [ ] **WebSocket disconnections** - Frequent agent disconnects
- [ ] **Connect code not working** - Code doesn't route to correct agent
- [ ] **Timeout errors** - Sessions timeout before establishing
- [ ] **Agent not responding** - Windows agent appears offline
- [ ] **Portal shows no agents** - Web interface shows no available agents

### Quick Health Check Commands

```bash
# Check all services are running
docker-compose ps

# Check relay server logs
docker-compose logs relay

# Check portal API logs  
docker-compose logs portal-api

# Check agent connectivity
curl -X GET http://localhost:5001/api/agents
```

## 🐛 Common Issues

### 1. Connect Code Routing Problem

**Issue**: Connect codes don't route to the correct agent
**Root Cause**: Incomplete implementation in `TcpRelayService.HandleClientAsync()`

**Symptoms**:
- Sessions connect to wrong agent
- Random agent selection instead of code-based routing
- Connect codes appear to be ignored

**Solution**: This is a known code-level issue that needs development fix.

### 2. WebSocket Connection Instability

**Issue**: Windows agents frequently disconnect from relay server
**Root Cause**: Network instability or configuration issues

**Symptoms**:
```
[INFO] Agent connected: agent-id-123
[ERROR] WebSocket connection lost: agent-id-123
[INFO] Agent attempting reconnection...
```

### 3. Agent Registration Failures

**Issue**: Windows agents fail to register with relay server
**Root Cause**: Network, authentication, or configuration problems

## 🔍 Step-by-Step Troubleshooting

### Step 1: Verify Infrastructure

1. **Check Docker Services**
   ```bash
   cd /path/to/rdp-relay
   docker-compose ps
   ```
   
   All services should show "Up" status:
   - relay
   - portal-api
   - portal-web
   - mongodb
   - redis

2. **Check Network Connectivity**
   ```bash
   # Test relay server
   curl -v http://localhost:8080/health
   
   # Test portal API
   curl -v http://localhost:5001/health
   
   # Test web portal
   curl -v http://localhost:3000
   ```

### Step 2: Verify Agent Registration

1. **Check Agent Status via API**
   ```bash
   curl -X GET http://localhost:5001/api/agents | jq '.'
   ```

2. **Expected Response**:
   ```json
   [
     {
       "id": "agent-id-123",
       "name": "WIN-PC-01",
       "status": "Online",
       "lastSeen": "2025-09-02T10:30:00Z",
       "connectCode": "ABC123"
     }
   ]
   ```

3. **If No Agents Shown**:
   - Check Windows agent is running
   - Verify agent configuration
   - Check network connectivity from agent to relay

### Step 3: Test Session Creation

1. **Create Test Session**
   ```bash
   curl -X POST http://localhost:5001/api/sessions \
     -H "Content-Type: application/json" \
     -d '{
       "connectCode": "ABC123",
       "clientIp": "127.0.0.1"
     }'
   ```

2. **Expected Response**:
   ```json
   {
     "sessionId": "sess-456",
     "relayEndpoint": "localhost:8080",
     "status": "Pending"
   }
   ```

### Step 4: Verify Connect Code Routing

⚠️ **Known Issue**: Connect code routing is not fully implemented

1. **Check Current Implementation**
   - Review `relay/RdpRelay.Relay/Services/TcpRelayService.cs`
   - Look for `HandleClientAsync()` method
   - Current code uses simple agent selection instead of connect code validation

2. **Workaround**:
   - Ensure only one agent is registered for testing
   - Use agent ID instead of connect code in API calls

## 📊 Log Analysis

### Relay Server Logs

**Location**: `logs/relay/` or `docker-compose logs relay`

**Key Patterns to Look For**:

```log
# Good - Successful agent connection
[INFO] Agent registered: agent-id-123 with connect code ABC123

# Warning - Connect code routing issue  
[WARN] Using simple agent selection instead of connect code routing

# Error - WebSocket disconnection
[ERROR] WebSocket connection lost for agent agent-id-123

# Error - No available agents
[ERROR] No agents available for session creation
```

### Windows Agent Logs

**Location**: Check Windows agent installation directory

**Key Patterns**:

```log
# Good - Successful connection
[INFO] Connected to relay server at ws://relay:8080/ws

# Error - Connection failed
[ERROR] Failed to connect to relay server: Connection refused

# Error - Authentication failed
[ERROR] Agent authentication failed: Invalid credentials
```

### Portal API Logs

**Location**: `logs/portal-api/` or `docker-compose logs portal-api`

**Key Patterns**:

```log
# Good - Session created
[INFO] Session created: sess-456 for agent agent-id-123

# Error - Session creation failed
[ERROR] Failed to create session: No available agents

# Error - Database connection
[ERROR] MongoDB connection failed
```

## 🔧 Known Issues and Solutions

### Issue 1: Connect Code Routing Not Implemented

**Problem**: The `TcpRelayService.HandleClientAsync()` method has incomplete connect code routing logic.

**Current Code Issue**:
```csharp
// TODO: Implement proper connect code routing
var availableAgent = _agents.FirstOrDefault();
```

**Temporary Solutions**:

1. **Single Agent Mode**:
   - Run only one Windows agent at a time
   - This bypasses the routing issue

2. **Agent ID Direct Connection**:
   ```bash
   curl -X POST http://localhost:5001/api/sessions \
     -H "Content-Type: application/json" \
     -d '{
       "agentId": "specific-agent-id",
       "clientIp": "127.0.0.1"
     }'
   ```

**Permanent Solution** (requires development):
```csharp
// Extract connect code from client connection
var connectCode = ExtractConnectCode(clientStream);

// Find agent by connect code
var targetAgent = _agents.FirstOrDefault(a => a.ConnectCode == connectCode);
if (targetAgent == null)
{
    throw new AgentNotFoundException($"No agent found for connect code: {connectCode}");
}
```

### Issue 2: WebSocket Disconnections

**Problem**: Windows agents frequently lose WebSocket connections.

**Solutions**:

1. **Increase Connection Timeout**:
   ```json
   {
     "WebSocket": {
       "KeepAliveInterval": "00:01:00",
       "CloseTimeout": "00:01:00"
     }
   }
   ```

2. **Add Connection Retry Logic**:
   - Configure exponential backoff
   - Maximum retry attempts
   - Connection health monitoring

3. **Network Configuration**:
   - Check firewall settings
   - Verify proxy configuration
   - Ensure WebSocket ports are open

### Issue 3: Agent Registration Failures

**Problem**: Windows agents fail to register with relay server.

**Solutions**:

1. **Check Agent Configuration**:
   ```json
   {
     "RelayServer": {
       "Url": "ws://localhost:8080/ws",
       "ApiEndpoint": "http://localhost:5001"
     }
   }
   ```

2. **Verify Network Connectivity**:
   ```cmd
   # From Windows agent machine
   telnet localhost 8080
   curl http://localhost:5001/health
   ```

3. **Check Authentication**:
   - Verify agent credentials
   - Check API key configuration
   - Validate certificate settings

## 🔬 Advanced Debugging

### Enable Debug Logging

1. **Relay Server**:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft": "Warning"
       }
     }
   }
   ```

2. **Windows Agent**:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "RdpRelay.Agent": "Trace"
       }
     }
   }
   ```

### Network Traffic Analysis

1. **Monitor WebSocket Traffic**:
   ```bash
   # Using netstat
   netstat -an | grep 8080
   
   # Using tcpdump
   sudo tcpdump -i any -n port 8080
   ```

2. **Check HTTP API Calls**:
   ```bash
   # Monitor API traffic
   sudo tcpdump -i any -A -s 1500 port 5001
   ```

### Database Debugging

1. **Check MongoDB Connection**:
   ```bash
   docker-compose exec mongodb mongo --eval "db.stats()"
   ```

2. **Verify Agent Records**:
   ```bash
   docker-compose exec mongodb mongo
   > use rdprelay
   > db.agents.find().pretty()
   ```

3. **Check Session Records**:
   ```bash
   > db.sessions.find().pretty()
   ```

## 🛡️ Preventive Measures

### Monitoring Setup

1. **Health Checks**:
   ```bash
   # Add to crontab for regular monitoring
   */5 * * * * curl -f http://localhost:5001/health || echo "API down" | mail -s "RDP Relay Alert" admin@company.com
   ```

2. **Log Rotation**:
   ```json
   {
     "Logging": {
       "File": {
         "MaxFileSize": "10MB",
         "MaxRetainedFiles": 5
       }
     }
   }
   ```

### Configuration Validation

1. **Pre-deployment Checks**:
   ```bash
   # Validate configuration files
   ./scripts/validate-config.sh
   
   # Test connectivity
   ./scripts/test-connection.sh
   
   # Verify agents
   ./scripts/test-agents-post-deployment.sh
   ```

2. **Automated Testing**:
   ```bash
   # Regular session establishment tests
   ./scripts/test-session-establishment.sh
   ```

### Best Practices

1. **Agent Deployment**:
   - Use consistent naming conventions
   - Implement proper error handling
   - Configure automatic restart on failure

2. **Network Configuration**:
   - Use static IP addresses for production
   - Configure proper DNS resolution
   - Implement network redundancy

3. **Security**:
   - Use TLS/SSL for all connections
   - Implement proper authentication
   - Regular security updates

## 📞 Getting Help

If you continue to experience issues after following this guide:

1. **Collect Diagnostic Information**:
   ```bash
   # Generate diagnostic report
   ./scripts/collect-diagnostics.sh > diagnostic-report.txt
   ```

2. **Include in Support Request**:
   - Symptom description
   - Steps already tried
   - Log files
   - Configuration files
   - Network topology

3. **Known Issues Repository**:
   - Check GitHub issues
   - Review recent commits
   - Check documentation updates

---

*Last updated: September 2, 2025*
*Version: 1.0.0*
