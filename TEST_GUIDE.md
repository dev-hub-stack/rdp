# RDP Relay Platform - End-to-End Testing Guide

## System Architecture Overview
```
[RDP Client] ←→ [Relay Server:443] ←→ [Windows Agent] ←→ [Target RDP:3389]
                       ↑
              [Portal Web + API] (Management)
```

## Phase 1: Infrastructure & Backend Testing

### 1.1 Start the Platform
```bash
cd /Users/clustox_1/Documents/Network/rdp-relay

# Start all services
./deploy.sh

# Check all containers are running
docker-compose ps
```

### 1.2 Verify Services Health
```bash
# Check Portal API
curl -k https://localhost:5000/health

# Check Relay Server WebSocket endpoint
curl -k https://localhost:5001/health

# Check nginx proxy
curl -k https://localhost/health

# Check logs
docker-compose logs -f portal-api
docker-compose logs -f relay-server
```

### 1.3 Test Database Connectivity
```bash
# Connect to MongoDB
docker-compose exec mongodb mongosh rdp_relay

# Verify initial data
db.tenants.find()
db.users.find()
```

## Phase 2: Portal Web Testing

### 2.1 Access the Web Portal
1. Open browser: `https://localhost`
2. Accept SSL certificate warning
3. Default login credentials:
   - Email: `admin@example.com`
   - Password: `SecurePassword123!`

### 2.2 Test Portal Functions
- [ ] Login/Logout functionality
- [ ] Dashboard shows stats (0 agents, 0 sessions initially)
- [ ] Navigate to Agents page
- [ ] Generate provisioning token
- [ ] Navigate to Users page
- [ ] Create new user
- [ ] Test session management page

## Phase 3: Windows Agent Testing

### 3.1 Prepare Windows Test Machine
You'll need a Windows machine (VM or physical) with:
- RDP enabled
- Network access to your relay server
- Administrator privileges for agent installation

### 3.2 Deploy Windows Agent
From the Portal Web:
1. Go to Agents page
2. Click "Generate Token" 
3. Copy the provisioning token
4. On Windows machine, run PowerShell as Administrator:

```powershell
# Download and install agent (you'll need to create this script)
# For now, manual deployment:
# 1. Copy agent binary to Windows machine
# 2. Create config file with provisioning token
# 3. Install as Windows service
```

### 3.3 Verify Agent Registration
- Check Portal Web Agents page for new agent
- Verify agent shows as "Online"
- Check agent details (hostname, OS, version)

## Phase 4: RDP Connection Testing

### 4.1 Create RDP Session
From Portal Web:
1. Go to Sessions page
2. Click "New Session"
3. Select registered agent
4. Enter Windows username
5. Click "Create Session"
6. Copy the connection details

### 4.2 Test RDP Connection
Using standard RDP client:
1. **Windows**: Use mstsc.exe
   ```
   Computer: localhost:5001 (or your relay server)
   Connect Code: [from portal]
   ```

2. **macOS**: Use Microsoft Remote Desktop
   - Add new connection
   - Use relay server address and port

3. **Linux**: Use rdesktop or xfreerdp
   ```bash
   xfreerdp /v:your-relay-server:443 /u:username
   ```

## Phase 5: Full Integration Testing

### 5.1 Multi-User Scenario
1. Create multiple user accounts
2. Deploy multiple Windows agents
3. Test concurrent RDP sessions
4. Verify session isolation

### 5.2 Network Resilience Testing
- Test with agent behind NAT
- Test with firewall restrictions
- Test connection drops and reconnection
- Test agent offline/online scenarios

### 5.3 Security Testing
- Verify JWT token expiration
- Test unauthorized access attempts
- Verify audit logging
- Test TLS encryption

## Phase 6: Performance & Load Testing

### 6.1 Session Load Testing
```bash
# Simulate multiple RDP connections
for i in {1..10}; do
    # Create concurrent sessions via API
    curl -X POST https://localhost/api/sessions \
         -H "Authorization: Bearer $TOKEN" \
         -H "Content-Type: application/json" \
         -d '{"agentId":"'$AGENT_ID'","username":"testuser'$i'"}'
done
```

### 6.2 Monitor Performance
```bash
# Check resource usage
docker stats

# Monitor logs for errors
docker-compose logs -f --tail=100

# Check database performance
docker-compose exec mongodb mongostat
```

## Troubleshooting Common Issues

### Agent Won't Connect
- Check firewall rules (port 443 outbound)
- Verify provisioning token hasn't expired
- Check agent logs in Windows Event Viewer
- Ensure relay server is accessible

### RDP Connection Fails
- Verify session is active in portal
- Check if target Windows machine has RDP enabled
- Verify Windows user credentials
- Check relay server logs for connection errors

### Performance Issues
- Monitor CPU/memory usage on relay server
- Check network bandwidth utilization
- Verify database query performance
- Scale horizontally if needed

## Success Criteria Checklist

### Basic Functionality
- [ ] All services start successfully
- [ ] Web portal is accessible and functional
- [ ] Windows agent registers successfully
- [ ] RDP sessions can be created
- [ ] RDP connections work end-to-end

### Advanced Features
- [ ] Multiple concurrent sessions work
- [ ] Session recording (if implemented)
- [ ] User management and RBAC
- [ ] Audit logging captures events
- [ ] Agent auto-reconnection works

### Production Readiness
- [ ] TLS certificates are properly configured
- [ ] Database is secured and backed up
- [ ] Monitoring and alerting is set up
- [ ] Load balancing works (if configured)
- [ ] Disaster recovery procedures tested

## Real-World Testing Scenarios

### Scenario 1: Remote IT Support
1. IT admin creates session for user's machine
2. Provides connection details to support technician
3. Support technician connects via RDP
4. Performs maintenance/troubleshooting
5. Session ends automatically after timeout

### Scenario 2: Secure Remote Work
1. Employee's home machine registered as agent
2. Employee requests access via company portal
3. Manager approves access (if approval workflow enabled)
4. Employee connects securely through corporate firewall
5. All activity is logged and auditable

### Scenario 3: Multi-Tenant Environment
1. Multiple customer tenants configured
2. Each tenant has isolated agents and users
3. Cross-tenant access is prevented
4. Each tenant can manage their own resources
5. Platform admin can oversee all tenants

This comprehensive testing approach will validate that your RDP Relay platform works correctly in real-world scenarios and is ready for production deployment.
