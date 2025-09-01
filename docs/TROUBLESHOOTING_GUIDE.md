# 🐛 Troubleshooting Guide - RDP Relay System

Comprehensive troubleshooting guide for common issues and their solutions.

## 🚨 Quick Emergency Fixes

### System Completely Down

```bash
# Nuclear option - restart everything
docker-compose down
docker system prune -f
docker-compose up -d --build

# If still failing, check disk space
df -h
# Clean up if needed
docker system prune -a -f
```

### Can't Access Web Portal

```bash
# 1. Check if containers are running
docker-compose ps

# 2. Check logs for errors
docker-compose logs portal-web nginx

# 3. Test direct container access
curl http://localhost:3000  # Direct web container
curl http://localhost:8080  # Through nginx

# 4. Restart web services
docker-compose restart portal-web nginx
```

### Database Connection Failed

```bash
# 1. Check MongoDB status
docker-compose logs mongodb

# 2. Test MongoDB connection
docker-compose exec mongodb mongosh --username admin --password

# 3. Restart MongoDB
docker-compose restart mongodb

# 4. If corrupted, restore from backup
docker-compose exec mongodb mongod --repair
```

---

## 🔧 Service-Specific Issues

### Portal API Issues

#### Symptoms
- API returns 500 Internal Server Error
- Authentication fails
- Database connection errors

#### Diagnosis
```bash
# Check API logs
docker-compose logs -f portal-api

# Test API health
curl http://localhost:8080/api/health

# Check environment variables
docker-compose exec portal-api printenv | grep -E "(MONGO|JWT|CORS)"

# Check database connectivity
docker-compose exec portal-api ping mongodb
```

#### Solutions
```bash
# Restart API service
docker-compose restart portal-api

# Rebuild API container
docker-compose up -d --build portal-api

# Check MongoDB connectivity
docker-compose exec portal-api nslookup mongodb

# Reset API container
docker-compose stop portal-api
docker-compose rm -f portal-api
docker-compose up -d portal-api
```

### Portal Web Issues

#### Symptoms
- Web page won't load
- JavaScript errors in console
- API calls failing from frontend

#### Diagnosis
```bash
# Check web container logs
docker-compose logs portal-web

# Check nginx configuration
docker-compose exec nginx nginx -t

# Test static file serving
curl -I http://localhost:3000

# Check browser console for errors
```

#### Solutions
```bash
# Restart web services
docker-compose restart portal-web nginx

# Clear browser cache (hard refresh)
# Ctrl+Shift+R (or Cmd+Shift+R on Mac)

# Rebuild web container
docker-compose up -d --build portal-web

# Check nginx configuration
docker-compose exec nginx cat /etc/nginx/conf.d/default.conf
```

### Relay Server Issues

#### Symptoms
- Agents can't connect
- WebSocket connection failures
- SSL/TLS handshake errors

#### Diagnosis
```bash
# Check relay server logs
docker-compose logs -f relay-server

# Test WebSocket connection
wscat -c wss://localhost:9443

# Check SSL certificates
openssl s_client -connect localhost:9443

# Test from agent perspective
telnet your-server 9443
```

#### Solutions
```bash
# Restart relay server
docker-compose restart relay-server

# Regenerate SSL certificates
cd infra/certs
openssl req -new -x509 -keyout relay.key -out relay.crt -days 365 -nodes
openssl pkcs12 -export -out relay.pfx -inkey relay.key -in relay.crt
docker-compose restart relay-server

# Check firewall
sudo ufw status
sudo ufw allow 9443/tcp
```

### MongoDB Issues

#### Symptoms
- Connection refused
- Authentication failures
- Data corruption

#### Diagnosis
```bash
# Check MongoDB logs
docker-compose logs mongodb

# Test connection
docker-compose exec mongodb mongosh --username admin --password

# Check disk space
df -h data/mongodb

# Check database status
docker-compose exec mongodb mongosh --eval "db.runCommand({serverStatus: 1})"
```

#### Solutions
```bash
# Restart MongoDB
docker-compose restart mongodb

# If authentication fails, reset auth
docker-compose stop mongodb
docker run --rm -v $(pwd)/data/mongodb:/data/db mongo:7.0 mongod --noauth --repair
docker-compose start mongodb

# If disk full, clean up logs
docker-compose exec mongodb find /var/log/mongodb -name "*.log" -mtime +7 -delete

# Repair database
docker-compose exec mongodb mongod --repair
```

### Redis Issues

#### Symptoms
- Redis connection timeouts
- Cache not working
- Memory errors

#### Diagnosis
```bash
# Check Redis logs
docker-compose logs redis

# Test Redis connection
docker-compose exec redis redis-cli ping

# Check Redis info
docker-compose exec redis redis-cli info

# Check memory usage
docker-compose exec redis redis-cli info memory
```

#### Solutions
```bash
# Restart Redis
docker-compose restart redis

# Clear Redis cache
docker-compose exec redis redis-cli FLUSHALL

# If memory issues, increase memory limit
# Edit docker-compose.yml:
# redis:
#   deploy:
#     resources:
#       limits:
#         memory: 512M
```

---

## 🌐 Network and Connectivity Issues

### Port Conflicts

#### Symptoms
- "Port already in use" errors
- Services won't start
- Connection refused

#### Diagnosis
```bash
# Check what's using ports
sudo netstat -tlnp | grep -E ':(3000|5000|8080|9443|27017|6379)'
sudo lsof -i :8080
sudo lsof -i :9443

# Check Docker port mappings
docker-compose ps
docker port rdp-relay-portal-web
```

#### Solutions
```bash
# Kill processes using required ports
sudo kill -9 $(sudo lsof -t -i:8080)

# Stop conflicting services
sudo systemctl stop apache2
sudo systemctl stop nginx

# Change ports in docker-compose.yml if needed
# portal-web:
#   ports:
#     - "8081:80"  # Change from 8080 to 8081
```

### DNS Resolution Issues

#### Symptoms
- Container can't reach other containers
- External API calls fail
- Database connection by hostname fails

#### Diagnosis
```bash
# Test inter-container communication
docker-compose exec portal-api ping mongodb
docker-compose exec portal-api nslookup mongodb

# Check Docker network
docker network ls
docker network inspect rdp-relay_default

# Check DNS settings
docker-compose exec portal-api cat /etc/resolv.conf
```

#### Solutions
```bash
# Recreate Docker network
docker-compose down
docker network prune
docker-compose up -d

# Use IP addresses instead of hostnames
docker-compose exec portal-api ping 172.18.0.2

# Add explicit network configuration in docker-compose.yml
```

### Firewall Issues

#### Symptoms
- External connections blocked
- Agents can't connect from outside
- Web portal inaccessible remotely

#### Diagnosis
```bash
# Check firewall status
sudo ufw status
sudo iptables -L

# Test port accessibility from outside
nmap -p 8080,9443 your-server-ip

# Check if services are listening on all interfaces
netstat -tlnp | grep -E ':(8080|9443)'
```

#### Solutions
```bash
# Open required ports
sudo ufw allow 8080/tcp
sudo ufw allow 9443/tcp
sudo ufw allow 5000/tcp

# For iptables
sudo iptables -A INPUT -p tcp --dport 8080 -j ACCEPT
sudo iptables-save

# Check Docker daemon configuration
sudo systemctl status docker
```

---

## 💻 Windows Agent Issues

### Agent Won't Install

#### Symptoms
- Service installation fails
- .NET runtime missing
- Permission denied errors

#### Diagnosis
```powershell
# Check .NET runtime
dotnet --version

# Check Windows Event Log
Get-EventLog -LogName Application -Source "RDP Relay Agent" -Newest 10

# Check service status
Get-Service "RDP Relay Agent"

# Test executable
cd C:\RdpRelayAgent\RdpRelay.Agent.Win\
.\RdpRelay.Agent.Win.exe --help
```

#### Solutions
```powershell
# Install .NET runtime
winget install Microsoft.DotNet.Runtime.8

# Install service with full path
sc create "RDP Relay Agent" binPath="C:\RdpRelayAgent\RdpRelay.Agent.Win\RdpRelay.Agent.Win.exe"
sc config "RDP Relay Agent" start= auto

# Set service permissions
sc config "RDP Relay Agent" obj= "LocalSystem"

# Start service
sc start "RDP Relay Agent"
```

### Agent Can't Connect

#### Symptoms
- Agent shows offline in portal
- Network connection errors
- SSL/TLS handshake failures

#### Diagnosis
```powershell
# Test network connectivity
Test-NetConnection your-server -Port 9443
Test-NetConnection your-server -Port 5000

# Check agent configuration
Get-Content C:\RdpRelayAgent\RdpRelay.Agent.Win\appsettings.json

# Check Windows Event Log
Get-EventLog -LogName Application -Source "RDP Relay Agent" -Newest 20

# Test SSL connectivity
openssl s_client -connect your-server:9443
```

#### Solutions
```powershell
# Update appsettings.json with correct server details
notepad C:\RdpRelayAgent\RdpRelay.Agent.Win\appsettings.json

# Check Windows Firewall
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*RDP*"}
New-NetFirewallRule -DisplayName "RDP Relay Agent" -Direction Outbound -Port 9443 -Protocol TCP -Action Allow

# Restart agent service
Restart-Service "RDP Relay Agent"

# Re-generate provisioning token if expired
```

### RDP Connection Issues

#### Symptoms
- Can't establish RDP session
- Authentication failures
- Session disconnects immediately

#### Diagnosis
```powershell
# Check RDP status
Get-Service TermService
Get-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name "fDenyTSConnections"

# Check RDP port
netstat -an | findstr :3389

# Test local RDP
mstsc /v:localhost

# Check Windows Event Log for RDP events
Get-WinEvent -LogName "Microsoft-Windows-TerminalServices-LocalSessionManager/Operational" -MaxEvents 10
```

#### Solutions
```powershell
# Enable RDP
Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name "fDenyTSConnections" -Value 0
Enable-NetFirewallRule -DisplayGroup "Remote Desktop"

# Start RDP service
Start-Service TermService
Set-Service TermService -StartupType Automatic

# Check user permissions
net localgroup "Remote Desktop Users"

# Add user to RDP group
net localgroup "Remote Desktop Users" username /add
```

---

## 🔐 Authentication and Authorization Issues

### Login Failures

#### Symptoms
- Invalid credentials error
- JWT token errors
- Session expires immediately

#### Diagnosis
```bash
# Check API logs for auth errors
docker-compose logs portal-api | grep -i auth

# Test login endpoint directly
curl -X POST "http://localhost:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@rdprelay.local","password":"admin123"}'

# Check JWT configuration
docker-compose exec portal-api printenv | grep JWT

# Check database for users
docker-compose exec mongodb mongosh --username admin --password --eval "db.users.find()"
```

#### Solutions
```bash
# Reset default admin user
docker-compose exec mongodb mongosh --username admin --password << 'EOF'
use rdp_relay
db.users.updateOne(
  {email: "admin@rdprelay.local"}, 
  {$set: {password: "$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi"}}
)
EOF

# Update JWT secret
openssl rand -base64 32
# Add to .env as JWT_SECRET_KEY
docker-compose restart portal-api

# Check CORS settings
curl -X OPTIONS "http://localhost:8080/api/auth/login" -H "Origin: http://localhost:3000" -v
```

### Permission Denied

#### Symptoms
- 403 Forbidden errors
- User can't access certain features
- Role-based access not working

#### Diagnosis
```bash
# Check user roles in database
docker-compose exec mongodb mongosh --username admin --password --eval "db.users.find({}, {email: 1, role: 1})"

# Check JWT token claims
# Use jwt.io to decode token from browser

# Check API authorization logs
docker-compose logs portal-api | grep -i "unauthorized\|forbidden"
```

#### Solutions
```bash
# Update user role
curl -X PUT "http://localhost:8080/api/users/{userId}" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"role": "TenantAdmin"}'

# Reset user permissions
docker-compose exec mongodb mongosh --username admin --password << 'EOF'
use rdp_relay
db.users.updateOne(
  {email: "user@company.com"}, 
  {$set: {role: "TenantAdmin"}}
)
EOF
```

---

## 📊 Performance Issues

### Slow Response Times

#### Symptoms
- Web interface is slow
- API calls take too long
- Database queries are slow

#### Diagnosis
```bash
# Check container resource usage
docker stats --no-stream

# Check system resources
htop
free -h
iostat 1

# Test API response times
curl -w "@curl-format.txt" -o /dev/null -s "http://localhost:8080/api/health"

# Check database performance
docker-compose exec mongodb mongosh --eval "db.runCommand({serverStatus: 1})" | grep -i "operation\|connection"
```

#### Solutions
```bash
# Increase container resources in docker-compose.yml
# portal-api:
#   deploy:
#     resources:
#       limits:
#         memory: 1G
#         cpus: '1.0'

# Add database indexes
docker-compose exec mongodb mongosh << 'EOF'
use rdp_relay
db.agents.createIndex({tenantId: 1, status: 1})
db.sessions.createIndex({tenantId: 1, createdAt: 1})
EOF

# Scale services
docker-compose up -d --scale portal-api=2

# Optimize database
docker-compose exec mongodb mongosh --eval "db.runCommand({compact: 'agents'})"
```

### Memory Issues

#### Symptoms
- Out of memory errors
- Container restarts
- System becomes unresponsive

#### Diagnosis
```bash
# Check memory usage
free -h
docker stats --no-stream | sort -k 4 -hr

# Check container memory limits
docker inspect $(docker-compose ps -q) | grep -i memory

# Check system logs for OOM killer
dmesg | grep -i "killed process"
```

#### Solutions
```bash
# Increase system swap
sudo swapon --show
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile

# Increase container memory limits
# Edit docker-compose.yml memory limits

# Clean up unused resources
docker system prune -a -f
```

---

## 🎯 Specific Error Messages

### "Failed to generate token"

```bash
# Check API logs
docker-compose logs portal-api | grep -i token

# Test token generation directly
curl -X POST "http://localhost:8080/api/agents/provisioning-token" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"groupId": null}'

# Check database connection
docker-compose exec portal-api ping mongodb
```

### "MachineId field is required"

This was the issue we fixed! If you still see this:

```bash
# Verify frontend is updated
docker-compose logs portal-web

# Check if form has all fields
curl http://localhost:3000 | grep -i "machine"

# Rebuild frontend
docker-compose up -d --build portal-web
```

### "Not a valid 24 digit hex string"

This was our ObjectId issue! If it persists:

```bash
# Check if backend is updated
docker-compose logs portal-api | grep -i objectid

# Verify ObjectId generation
docker-compose exec portal-api grep -r "ObjectId.GenerateNewId" /app

# Rebuild backend
docker-compose up -d --build portal-api
```

### "Internal server error"

```bash
# Check detailed API logs
docker-compose logs portal-api --tail=50

# Check database connectivity
docker-compose exec portal-api ping mongodb

# Check if database is ready
docker-compose exec mongodb mongosh --username admin --password --eval "db.runCommand({ismaster: 1})"
```

---

## 🚀 Recovery Procedures

### Complete System Recovery

```bash
#!/bin/bash
echo "Starting emergency recovery procedure..."

# Stop all services
docker-compose down

# Clean up docker system
docker system prune -f

# Check disk space
df -h

# Start core services first
docker-compose up -d mongodb redis

# Wait for databases to be ready
sleep 30

# Start application services
docker-compose up -d portal-api relay-server

# Wait for APIs to be ready
sleep 30

# Start frontend services
docker-compose up -d portal-web nginx

# Run health check
sleep 30
./test-platform.sh

echo "Recovery procedure completed"
```

### Database Recovery

```bash
# Stop application services
docker-compose stop portal-api relay-server

# Repair MongoDB
docker-compose exec mongodb mongod --repair

# Restart MongoDB
docker-compose restart mongodb

# Clear Redis cache
docker-compose exec redis redis-cli FLUSHALL
docker-compose restart redis

# Restart application services
docker-compose start portal-api relay-server

# Verify recovery
./test-platform.sh
```

---

## 📞 When to Contact Support

Contact technical support if:

- Multiple recovery attempts fail
- Data corruption is detected
- Security breach is suspected
- System is down for >30 minutes
- Performance degrades significantly

Include in your support request:
- Current system status: `docker-compose ps`
- Recent logs: `docker-compose logs --tail=100`
- System resources: `free -h && df -h`
- Error messages and timestamps
- Steps taken to resolve the issue

---

## 🔍 Debug Mode

Enable debug logging for troubleshooting:

```bash
# Update .env file
echo "SERILOG_MINIMUM_LEVEL=Debug" >> .env
echo "ASPNETCORE_ENVIRONMENT=Development" >> .env

# Restart services
docker-compose restart portal-api relay-server

# View debug logs
docker-compose logs -f portal-api | grep -i debug
```

Remember: Always check the logs first - they contain most of the information needed to diagnose issues!
