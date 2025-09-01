# 🔧 Operations Guide - RDP Relay System

Complete guide for day-to-day operations, maintenance, and management.

## 📊 Daily Operations

### System Health Monitoring

```bash
# Quick health check
./test-platform.sh

# Check all services status
docker-compose ps

# Monitor resource usage
docker stats --no-stream

# Check disk space
df -h
du -sh data/ logs/
```

### Log Management

```bash
# View real-time logs
docker-compose logs -f portal-api
docker-compose logs -f relay-server
docker-compose logs -f portal-web

# Check specific time range
docker-compose logs --since="2h" portal-api

# Export logs for analysis
docker-compose logs --since="24h" > system-logs-$(date +%Y%m%d).log

# Rotate logs (prevent disk full)
find logs/ -name "*.log" -mtime +30 -delete
```

### Database Operations

```bash
# MongoDB backup
docker-compose exec mongodb mongodump --username admin --password --authenticationDatabase admin --out /data/backup

# MongoDB restore
docker-compose exec mongodb mongorestore --username admin --password --authenticationDatabase admin /data/backup

# Redis backup
docker-compose exec redis redis-cli --rdb /data/dump.rdb

# Check database sizes
docker-compose exec mongodb mongo --username admin --password --eval "db.stats()"
```

---

## 🔄 Service Management

### Container Operations

```bash
# Restart individual services
docker-compose restart portal-api
docker-compose restart portal-web
docker-compose restart relay-server
docker-compose restart mongodb
docker-compose restart redis
docker-compose restart nginx

# Rebuild and update service
docker-compose up -d --build portal-api

# Scale services (load balancing)
docker-compose up -d --scale portal-api=2
docker-compose up -d --scale relay-server=2

# Stop specific service
docker-compose stop portal-api

# Start specific service
docker-compose start portal-api

# Remove and recreate service
docker-compose rm -f portal-api
docker-compose up -d portal-api
```

### System Updates

```bash
# Update system (with backup)
# 1. Create backup
sudo tar -czf backup-$(date +%Y%m%d-%H%M).tar.gz data/ .env

# 2. Pull latest changes
git pull origin main

# 3. Rebuild with zero downtime
docker-compose up -d --build --force-recreate

# 4. Verify update
./test-platform.sh
```

### Configuration Updates

```bash
# Update environment variables
nano .env

# Apply configuration changes
docker-compose up -d --force-recreate

# Update docker-compose configuration
nano docker-compose.yml
docker-compose up -d

# Update nginx configuration
nano infra/nginx/nginx.conf
docker-compose restart nginx
```

---

## 👥 User Management

### Admin Operations via API

```bash
# Get auth token (replace with actual credentials)
TOKEN=$(curl -s -X POST "http://localhost:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@rdprelay.local","password":"admin123"}' \
  | jq -r '.data.token')

# Create new user
curl -X POST "http://localhost:8080/api/users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@company.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Operator",
    "password": "SecurePassword123!"
  }'

# List all users
curl -X GET "http://localhost:8080/api/users" \
  -H "Authorization: Bearer $TOKEN"

# Update user role
curl -X PUT "http://localhost:8080/api/users/{userId}" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"role": "TenantAdmin"}'

# Deactivate user
curl -X DELETE "http://localhost:8080/api/users/{userId}" \
  -H "Authorization: Bearer $TOKEN"
```

### Agent Management

```bash
# Generate provisioning token
curl -X POST "http://localhost:8080/api/agents/provisioning-token" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"groupId": null}'

# List all agents
curl -X GET "http://localhost:8080/api/agents" \
  -H "Authorization: Bearer $TOKEN"

# Get agent details
curl -X GET "http://localhost:8080/api/agents/{agentId}" \
  -H "Authorization: Bearer $TOKEN"

# Update agent
curl -X PUT "http://localhost:8080/api/agents/{agentId}" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name": "Updated Agent Name"}'

# Remove agent
curl -X DELETE "http://localhost:8080/api/agents/{agentId}" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 🔐 Security Management

### SSL/TLS Certificate Management

```bash
# Check current certificates
openssl x509 -in infra/certs/relay.crt -text -noout

# Generate new certificates
cd infra/certs
openssl req -new -x509 -keyout relay.key -out relay.crt -days 365 -nodes \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=your-domain.com"

# Create PKCS12 format for .NET
openssl pkcs12 -export -out relay.pfx -inkey relay.key -in relay.crt -password pass:your-cert-password

# Update certificate in environment
echo "CERT_PASSWORD=your-cert-password" >> .env

# Restart services to load new certificates
docker-compose restart relay-server
```

### Password Management

```bash
# Generate secure passwords
openssl rand -base64 32

# Update database passwords
nano .env
# Update MONGODB_PASSWORD and REDIS_PASSWORD

# Update JWT secret
nano .env
# Update JWT_SECRET_KEY

# Apply password changes
docker-compose down
docker-compose up -d
```

### Network Security

```bash
# Check open ports
netstat -tlnp | grep -E ':(3000|5000|8080|9443)'

# Configure firewall (Ubuntu)
sudo ufw enable
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 8080/tcp # Web Portal
sudo ufw allow 5000/tcp # API
sudo ufw allow 9443/tcp # Relay

# Block unwanted traffic
sudo ufw deny from 192.168.1.0/24 to any port 27017  # Block external MongoDB access
```

---

## 📈 Performance Monitoring

### System Metrics

```bash
# Container resource usage
docker stats

# System resource usage
htop
iotop
free -h

# Network connections
netstat -an | grep :8080
netstat -an | grep :9443

# Database performance
docker-compose exec mongodb mongo --username admin --password --eval "db.runCommand({serverStatus: 1})"
```

### Application Metrics

```bash
# API response times
curl -w "@curl-format.txt" -o /dev/null -s "http://localhost:8080/api/health"

# Create curl-format.txt for timing
cat > curl-format.txt << 'EOF'
     time_namelookup:  %{time_namelookup}\n
        time_connect:  %{time_connect}\n
     time_appconnect:  %{time_appconnect}\n
    time_pretransfer:  %{time_pretransfer}\n
       time_redirect:  %{time_redirect}\n
  time_starttransfer:  %{time_starttransfer}\n
                     ----------\n
          time_total:  %{time_total}\n
EOF

# Agent connection status
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/agents" | jq '.data[] | {name: .name, status: .status, lastHeartbeat: .lastHeartbeat}'

# Session statistics
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/sessions/stats" | jq
```

### Log Analysis

```bash
# Error analysis
grep -i error logs/portal-api/*.log | tail -20
grep -i exception logs/portal-api/*.log | tail -20

# Connection analysis
grep -i "connection" logs/relay/*.log | tail -20

# Performance analysis
grep -i "slow\|timeout\|performance" logs/*/*.log | tail -20

# Generate daily report
cat > daily-report.sh << 'EOF'
#!/bin/bash
echo "=== Daily System Report - $(date) ==="
echo ""
echo "Container Status:"
docker-compose ps
echo ""
echo "System Resources:"
free -h
df -h /
echo ""
echo "Recent Errors:"
grep -i error logs/portal-api/*.log | tail -5
echo ""
echo "Agent Status:"
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/agents" | jq -r '.data[] | "\(.name): \(.status)"'
EOF
chmod +x daily-report.sh
```

---

## 🔄 Backup and Recovery

### Automated Backup Script

```bash
cat > backup-system.sh << 'EOF'
#!/bin/bash

BACKUP_DIR="/opt/backups/rdp-relay"
DATE=$(date +%Y%m%d-%H%M%S)
BACKUP_FILE="rdp-relay-backup-$DATE.tar.gz"

# Create backup directory
mkdir -p $BACKUP_DIR

# Stop services (optional for consistency)
echo "Creating backup..."
docker-compose exec mongodb mongodump --username admin --password --authenticationDatabase admin --out /data/mongodb-backup

# Create full backup
tar -czf "$BACKUP_DIR/$BACKUP_FILE" \
  --exclude='logs/*.log' \
  --exclude='.git' \
  data/ \
  .env \
  docker-compose.yml \
  infra/

echo "Backup created: $BACKUP_DIR/$BACKUP_FILE"

# Clean old backups (keep last 7 days)
find $BACKUP_DIR -name "rdp-relay-backup-*.tar.gz" -mtime +7 -delete

echo "Backup completed successfully"
EOF

chmod +x backup-system.sh
```

### Recovery Procedures

```bash
# Full system recovery
cat > restore-system.sh << 'EOF'
#!/bin/bash

BACKUP_FILE="$1"

if [ -z "$BACKUP_FILE" ]; then
  echo "Usage: $0 <backup-file>"
  exit 1
fi

echo "Stopping services..."
docker-compose down

echo "Restoring from $BACKUP_FILE..."
tar -xzf "$BACKUP_FILE" -C .

echo "Starting services..."
docker-compose up -d

echo "Waiting for services to start..."
sleep 30

echo "Running health check..."
./test-platform.sh

echo "Recovery completed"
EOF

chmod +x restore-system.sh
```

### Database Recovery

```bash
# MongoDB point-in-time recovery
docker-compose exec mongodb mongorestore --username admin --password --authenticationDatabase admin --drop /data/mongodb-backup

# Redis recovery
docker-compose exec redis redis-cli --rdb /data/dump.rdb
docker-compose restart redis
```

---

## 🚨 Incident Response

### Critical Service Failure

```bash
# Quick diagnosis
docker-compose ps
docker-compose logs --tail=50 [failed-service]

# Emergency restart
docker-compose restart [failed-service]

# If restart fails
docker-compose stop [failed-service]
docker-compose rm [failed-service]
docker-compose up -d [failed-service]

# Full system restart (last resort)
docker-compose down
docker-compose up -d
```

### Database Issues

```bash
# MongoDB corruption
docker-compose stop portal-api
docker-compose exec mongodb mongod --repair
docker-compose start portal-api

# Redis issues
docker-compose exec redis redis-cli FLUSHALL
docker-compose restart redis
```

### Network Issues

```bash
# Check network connectivity
docker network ls
docker network inspect rdp-relay_default

# Recreate network
docker-compose down
docker network prune
docker-compose up -d
```

### High CPU/Memory Usage

```bash
# Identify resource hogs
docker stats --no-stream | sort -k 3 -hr

# Scale down if needed
docker-compose scale portal-api=1
docker-compose scale relay-server=1

# Restart high-usage containers
docker-compose restart [high-usage-service]
```

---

## 📋 Maintenance Schedule

### Daily Tasks
- [ ] Check system health: `./test-platform.sh`
- [ ] Review error logs: `grep -i error logs/*/*.log`
- [ ] Monitor disk space: `df -h`
- [ ] Check agent status via web portal

### Weekly Tasks
- [ ] Update system packages: `sudo apt update && sudo apt upgrade`
- [ ] Rotate logs: `find logs/ -name "*.log" -mtime +7 -delete`
- [ ] Create system backup: `./backup-system.sh`
- [ ] Review performance metrics

### Monthly Tasks
- [ ] Update Docker images: `docker-compose pull && docker-compose up -d`
- [ ] Update SSL certificates (if needed)
- [ ] Review and update firewall rules
- [ ] Clean up old backups
- [ ] Performance optimization review

### Quarterly Tasks
- [ ] Full security audit
- [ ] Disaster recovery test
- [ ] Documentation updates
- [ ] Capacity planning review

---

## 📞 Emergency Contacts

Create an `emergency-contacts.txt` file:

```
System Administrator: admin@company.com
Database Admin: dba@company.com
Network Admin: network@company.com
Security Team: security@company.com

Emergency Procedures:
1. Check system status: ./test-platform.sh
2. Review logs: docker-compose logs
3. Restart services: docker-compose restart
4. Contact administrator if issue persists
```

---

## 🎯 Success Metrics

Track these KPIs for operational success:

- **Uptime**: Target 99.9%
- **Response Time**: API < 200ms, Web < 2s
- **Agent Connectivity**: > 95% online
- **Error Rate**: < 0.1% of requests
- **Backup Success**: 100% daily backups
- **Security Incidents**: 0 per month

Use these metrics to continuously improve system operations and reliability.
