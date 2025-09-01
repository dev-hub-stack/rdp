# RDP Relay Platform - Deployment & Operations Guide

## 🔐 Login Credentials

**Working Login Account:**
```
Email: test@test.com
Password: password
Role: SystemAdmin
```

**Platform Access URLs:**
- **Web Portal**: http://localhost:8080
- **API Direct**: http://localhost:5000  
- **API via Proxy**: http://localhost:8080/api/*
- **Relay Server**: http://localhost:5001

---

## Table of Contents
1. [Quick Start Deployment](#quick-start-deployment)
2. [Production Deployment](#production-deployment)
3. [Security Configuration](#security-configuration)
4. [Monitoring & Logging](#monitoring--logging)
5. [Backup & Recovery](#backup--recovery)
6. [Performance Tuning](#performance-tuning)
7. [Troubleshooting](#troubleshooting)
8. [Windows Agent Deployment](#windows-agent-deployment)
9. [Maintenance Procedures](#maintenance-procedures)
10. [Scaling & Load Balancing](#scaling--load-balancing)

---

## Quick Start Deployment

### Prerequisites
- **Docker**: Version 24.0+ with Docker Compose v2
- **System Resources**: 4GB RAM minimum, 8GB recommended
- **Disk Space**: 10GB for containers and logs
- **Network Ports**: 8080, 8443, 5000, 5001, 9443, 27017, 6379, 3000

### 1. Clone and Setup
```bash
# Clone repository
git clone <repository-url>
cd rdp-relay

# Copy environment configuration
cp .env.example .env

# Edit environment variables (optional for quick start)
nano .env
```

### 2. Start the Platform
```bash
# Build and start all services
docker-compose up --build -d

# Monitor startup logs
docker-compose logs -f

# Check all services are running
docker-compose ps
```

### 3. Verify Installation
```bash
# Check web portal
curl -I http://localhost:8080

# Test API health
curl http://localhost:5001/health

# Test authentication
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"password"}'
```

### 4. Access the Platform
1. Open browser: http://localhost:8080
2. Login with: `test@test.com` / `password`
3. Verify dashboard loads with system statistics

---

## Production Deployment

### Environment Configuration

#### 1. Production .env File
```bash
# MongoDB Configuration
MONGODB_PASSWORD=highly_secure_mongodb_password_2025_change_me
MONGODB_DATABASE=rdp_relay_production

# Redis Configuration  
REDIS_PASSWORD=highly_secure_redis_password_2025_change_me

# JWT Configuration
JWT_SECRET_KEY=production-jwt-signing-key-must-be-at-least-32-characters-long-and-cryptographically-secure
JWT_ISSUER=RdpRelayPortal
JWT_AUDIENCE=RdpRelayPortal

# Certificate Configuration
CERT_PASSWORD=secure_certificate_password_2025_change_me

# Production URLs
VITE_API_BASE_URL=https://api.your-domain.com
VITE_RELAY_WS_URL=wss://relay.your-domain.com
VITE_APP_TITLE=RDP Relay Portal - Production

# Optional: External monitoring
PROMETHEUS_ENDPOINT=https://prometheus.your-domain.com
GRAFANA_ENDPOINT=https://grafana.your-domain.com
```

#### 2. SSL Certificate Setup
```bash
# Generate production certificates
cd infra/certs

# Create private key
openssl genrsa -out relay.key 4096

# Create certificate signing request
openssl req -new -key relay.key -out relay.csr \
  -config openssl.conf

# Generate self-signed certificate (or use CA-signed)
openssl x509 -req -days 365 -in relay.csr \
  -signkey relay.key -out relay.crt

# Create PFX bundle for .NET applications
openssl pkcs12 -export -out relay.pfx \
  -inkey relay.key -in relay.crt \
  -passout pass:${CERT_PASSWORD}

# Set proper permissions
chmod 600 relay.key relay.pfx
chmod 644 relay.crt
```

### Production Docker Compose

#### 1. docker-compose.prod.yml
```yaml
version: '3.8'

services:
  mongodb:
    image: mongo:7.0
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: ${MONGODB_PASSWORD}
      MONGO_INITDB_DATABASE: ${MONGODB_DATABASE}
    volumes:
      - ./data/mongodb:/data/db
      - ./infra/mongodb:/docker-entrypoint-initdb.d
    networks:
      - rdp-network
    deploy:
      resources:
        limits:
          memory: 2G
        reservations:
          memory: 512M

  redis:
    image: redis:7.2-alpine
    restart: unless-stopped
    command: redis-server --requirepass ${REDIS_PASSWORD}
    volumes:
      - ./data/redis:/data
    networks:
      - rdp-network
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 128M

  portal-api:
    build:
      context: .
      dockerfile: infra/docker/Dockerfile.portal-api
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - MongoDB__ConnectionString=mongodb://admin:${MONGODB_PASSWORD}@mongodb:27017/${MONGODB_DATABASE}?authSource=admin
      - Redis__ConnectionString=redis:6379,password=${REDIS_PASSWORD}
      - Jwt__SecretKey=${JWT_SECRET_KEY}
      - Jwt__Issuer=${JWT_ISSUER}
      - Jwt__Audience=${JWT_AUDIENCE}
    depends_on:
      - mongodb
      - redis
    networks:
      - rdp-network
    deploy:
      resources:
        limits:
          memory: 1G
        reservations:
          memory: 256M

  portal-web:
    build:
      context: ./portal-web
      dockerfile: ../infra/docker/Dockerfile.portal-web
      args:
        - VITE_API_BASE_URL=${VITE_API_BASE_URL}
        - VITE_RELAY_WS_URL=${VITE_RELAY_WS_URL}
        - VITE_APP_TITLE=${VITE_APP_TITLE}
    restart: unless-stopped
    networks:
      - rdp-network
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 64M

  relay-server:
    build:
      context: .
      dockerfile: infra/docker/Dockerfile.relay
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:8443;http://+:8080
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/relay.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - ./infra/certs:/app/certs:ro
    networks:
      - rdp-network
    deploy:
      resources:
        limits:
          memory: 1G
        reservations:
          memory: 256M

  nginx:
    image: nginx:alpine
    restart: unless-stopped
    volumes:
      - ./infra/nginx:/etc/nginx:ro
      - ./infra/certs:/etc/nginx/certs:ro
      - ./logs/nginx:/var/log/nginx
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - portal-web
      - portal-api
      - relay-server
    networks:
      - rdp-network
    deploy:
      resources:
        limits:
          memory: 256M
        reservations:
          memory: 64M

networks:
  rdp-network:
    driver: bridge

volumes:
  mongodb_data:
  redis_data:
  # Nginx with production configuration
  nginx:
    image: nginx:alpine
    container_name: rdp-relay-nginx-prod
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./infra/nginx/prod:/etc/nginx/conf.d
      - ./infra/certs:/etc/ssl/certs:ro
      - ./logs/nginx:/var/log/nginx
    environment:
      - NGINX_WORKER_PROCESSES=auto
      - NGINX_WORKER_CONNECTIONS=1024
    depends_on:
      - portal-web
      - portal-api
    networks:
      - rdp-relay-network

  # Portal Web with production build
  portal-web:
    build:
      context: .
      dockerfile: ./infra/docker/Dockerfile.portal-web
      args:
        - NODE_ENV=production
    container_name: rdp-relay-portal-web-prod
    restart: unless-stopped
    expose:
      - "80"
    environment:
      - NODE_ENV=production
      - VITE_API_BASE_URL=${VITE_API_BASE_URL}
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:80"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - rdp-relay-network

  # Portal API with production settings
  portal-api:
    build:
      context: .
      dockerfile: ./infra/docker/Dockerfile.portal-api
    container_name: rdp-relay-portal-api-prod
    restart: unless-stopped
    expose:
      - "8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - MongoDB__ConnectionString=mongodb://admin:${MONGODB_PASSWORD}@mongodb:27017/rdp_relay?authSource=admin
      - MongoDB__DatabaseName=rdp_relay
      - Redis__ConnectionString=redis:6379,password=${REDIS_PASSWORD}
      - Jwt__SecretKey=${JWT_SECRET_KEY}
      - Jwt__Issuer=${JWT_ISSUER}
      - Jwt__Audience=${JWT_AUDIENCE}
    volumes:
      - ./logs/portal-api:/app/logs
    depends_on:
      - mongodb
      - redis
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - rdp-relay-network

  # Relay Server with production configuration
  relay-server:
    build:
      context: .
      dockerfile: ./infra/docker/Dockerfile.relay
    container_name: rdp-relay-relay-server-prod
    restart: unless-stopped
    ports:
      - "5001:8080"
      - "9443:8443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080;https://+:8443
      - Redis__ConnectionString=redis:6379,password=${REDIS_PASSWORD}
      - Certificates__Path=/app/certs/relay.pfx
      - Certificates__Password=${CERT_PASSWORD}
    volumes:
      - ./logs/relay:/app/logs
      - ./infra/certs:/app/certs:ro
    depends_on:
      - redis
    networks:
      - rdp-relay-network

  # MongoDB with replica set for HA
  mongodb:
    image: mongo:7.0
    container_name: rdp-relay-mongodb-prod
    restart: unless-stopped
    ports:
      - "27017:27017"
    environment:
      - MONGO_INITDB_ROOT_USERNAME=admin
      - MONGO_INITDB_ROOT_PASSWORD=${MONGODB_PASSWORD}
    volumes:
      - ./infra/mongodb/init-mongo.js:/docker-entrypoint-initdb.d/init-mongo.js:ro
      - ./data/mongodb:/data/db
      - ./logs/mongodb:/var/log/mongodb
    command: --replSet rs0 --journal --smallfiles
    networks:
      - rdp-relay-network

  # Redis with persistence enabled
  redis:
    image: redis:7.2-alpine
    container_name: rdp-relay-redis-prod
    restart: unless-stopped
    ports:
      - "6379:6379"
    command: >
      redis-server 
      --requirepass ${REDIS_PASSWORD}
      --appendonly yes
      --appendfsync everysec
      --maxmemory 1gb
      --maxmemory-policy allkeys-lru
    volumes:
      - ./data/redis:/data
      - ./logs/redis:/var/log/redis
    networks:
      - rdp-relay-network

networks:
  rdp-relay-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16

volumes:
  mongodb-data:
    driver: local
  redis-data:
    driver: local
```

#### 2. Deploy Production Environment
```bash
# Deploy with production configuration
docker-compose -f docker-compose.prod.yml up -d

# Monitor deployment
docker-compose -f docker-compose.prod.yml logs -f

# Verify all services healthy
docker-compose -f docker-compose.prod.yml ps
```

---

## Security Configuration

### 1. Firewall Rules
```bash
# Allow only necessary ports
ufw allow 22    # SSH
ufw allow 80    # HTTP
ufw allow 443   # HTTPS
ufw allow 5001  # Relay Server (for agents)
ufw allow 9443  # Secure Relay (HTTPS)

# Block direct database access from external
ufw deny 27017  # MongoDB
ufw deny 6379   # Redis

# Enable firewall
ufw enable
```

### 2. nginx Security Configuration
```nginx
# /infra/nginx/prod/security.conf
# Security headers
add_header X-Frame-Options "DENY" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';" always;
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

# Hide nginx version
server_tokens off;

# Rate limiting
limit_req_zone $binary_remote_addr zone=auth:10m rate=5r/m;
limit_req_zone $binary_remote_addr zone=api:10m rate=100r/m;

# Main server block
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;
    
    # SSL configuration
    ssl_certificate /etc/ssl/certs/relay.crt;
    ssl_certificate_key /etc/ssl/certs/relay.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    
    # API routes with rate limiting
    location /api/auth/ {
        limit_req zone=auth burst=10 nodelay;
        proxy_pass http://portal-api:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    location /api/ {
        limit_req zone=api burst=200 nodelay;
        proxy_pass http://portal-api:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Frontend application
    location / {
        proxy_pass http://portal-web:80;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 3. Database Security
```javascript
// MongoDB security script
// Run: docker exec -it rdp-relay-mongodb-prod mongosh

// Create application user with limited permissions
use admin
db.createUser({
  user: "rdp_relay_app",
  pwd: "secure_app_password_2025",
  roles: [
    { role: "readWrite", db: "rdp_relay" },
    { role: "dbAdmin", db: "rdp_relay" }
  ]
})

// Enable authentication
use rdp_relay
db.runCommand({authSchemaUpgrade: 1})

// Create indexes for performance and security
db.users.createIndex({ "email": 1 }, { unique: true })
db.users.createIndex({ "tenantId": 1, "isActive": 1 })
db.sessions.createIndex({ "createdAt": 1 }, { expireAfterSeconds: 2592000 }) // 30 days
```

### 4. Redis Security Configuration
```bash
# Redis security configuration
cat > ./infra/redis/redis.conf << 'EOF'
# Network security
bind 127.0.0.1 ::1
protected-mode yes
port 0
unixsocket /var/run/redis/redis-server.sock
unixsocketperm 700

# Authentication
requirepass highly_secure_redis_password_2025_change_me

# Disable dangerous commands
rename-command FLUSHDB ""
rename-command FLUSHALL ""
rename-command KEYS ""
rename-command CONFIG "CONFIG_SECURE_COMMAND"
rename-command DEBUG ""
rename-command EVAL ""

# Memory and persistence
maxmemory 1gb
maxmemory-policy allkeys-lru
save 900 1
save 300 10
save 60 10000
EOF
```

---

## Monitoring & Logging

### 1. Application Monitoring Setup

#### Prometheus Configuration
```yaml
# docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    container_name: rdp-relay-prometheus
    restart: unless-stopped
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus:/etc/prometheus
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=200h'
    networks:
      - rdp-relay-network

  grafana:
    image: grafana/grafana:latest
    container_name: rdp-relay-grafana
    restart: unless-stopped
    ports:
      - "3001:3000"
    volumes:
      - grafana-data:/var/lib/grafana
      - ./monitoring/grafana:/etc/grafana/provisioning
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=secure_grafana_password
      - GF_USERS_ALLOW_SIGN_UP=false
    networks:
      - rdp-relay-network

  node-exporter:
    image: prom/node-exporter:latest
    container_name: rdp-relay-node-exporter
    restart: unless-stopped
    ports:
      - "9100:9100"
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.rootfs=/rootfs'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
    networks:
      - rdp-relay-network

volumes:
  prometheus-data:
  grafana-data:
```

#### Prometheus Targets Configuration
```yaml
# monitoring/prometheus/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "alert_rules.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets: []

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'portal-api'
    static_configs:
      - targets: ['portal-api:8080']
    metrics_path: '/metrics'
    scrape_interval: 30s

  - job_name: 'relay-server'
    static_configs:
      - targets: ['relay-server:8080']
    metrics_path: '/metrics'
    scrape_interval: 30s

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']

  - job_name: 'mongodb-exporter'
    static_configs:
      - targets: ['mongodb-exporter:9216']

  - job_name: 'redis-exporter'
    static_configs:
      - targets: ['redis-exporter:9121']
```

### 2. Log Management

#### ELK Stack Configuration
```yaml
# docker-compose.logging.yml
version: '3.8'

services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.8.0
    container_name: rdp-relay-elasticsearch
    restart: unless-stopped
    environment:
      - discovery.type=single-node
      - "ES_JAVA_OPTS=-Xms1g -Xmx1g"
      - xpack.security.enabled=false
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data
    ports:
      - "9200:9200"
    networks:
      - rdp-relay-network

  logstash:
    image: docker.elastic.co/logstash/logstash:8.8.0
    container_name: rdp-relay-logstash
    restart: unless-stopped
    volumes:
      - ./logging/logstash/pipeline:/usr/share/logstash/pipeline
      - ./logs:/var/log/rdp-relay:ro
    ports:
      - "5044:5044"
    environment:
      - "LS_JAVA_OPTS=-Xmx1g -Xms1g"
    depends_on:
      - elasticsearch
    networks:
      - rdp-relay-network

  kibana:
    image: docker.elastic.co/kibana/kibana:8.8.0
    container_name: rdp-relay-kibana
    restart: unless-stopped
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch
    networks:
      - rdp-relay-network

volumes:
  elasticsearch-data:
```

#### Logstash Pipeline Configuration
```ruby
# logging/logstash/pipeline/rdp-relay.conf
input {
  file {
    path => "/var/log/rdp-relay/portal-api/*.log"
    start_position => "beginning"
    type => "portal-api"
  }
  
  file {
    path => "/var/log/rdp-relay/relay/*.log"
    start_position => "beginning"
    type => "relay-server"
  }
  
  file {
    path => "/var/log/rdp-relay/nginx/*.log"
    start_position => "beginning"
    type => "nginx"
  }
}

filter {
  if [type] == "portal-api" or [type] == "relay-server" {
    json {
      source => "message"
    }
    
    date {
      match => [ "timestamp", "ISO8601" ]
    }
  }
  
  if [type] == "nginx" {
    grok {
      match => { "message" => "%{COMBINEDAPACHELOG}" }
    }
    
    date {
      match => [ "timestamp", "dd/MMM/yyyy:HH:mm:ss Z" ]
    }
  }
}

output {
  elasticsearch {
    hosts => ["elasticsearch:9200"]
    index => "rdp-relay-%{type}-%{+YYYY.MM.dd}"
  }
  
  stdout {
    codec => rubydebug
  }
}
```

### 3. Health Check Monitoring Script
```bash
#!/bin/bash
# monitoring/health-check.sh

# Health check script for RDP Relay Platform
echo "=== RDP Relay Platform Health Check ==="
echo "Timestamp: $(date)"

# Check container health
echo -e "\n1. Container Status:"
docker-compose -f docker-compose.prod.yml ps

# Check service endpoints
echo -e "\n2. Service Health:"
services=(
  "Portal Web:http://localhost:80"
  "Portal API:http://localhost:5000/health"
  "Relay Server:http://localhost:5001/health"
  "MongoDB:mongodb://admin:password@localhost:27017"
  "Redis:redis://localhost:6379"
)

for service in "${services[@]}"; do
  name=$(echo $service | cut -d: -f1)
  url=$(echo $service | cut -d: -f2-)
  
  if [[ $url == *"mongodb"* ]]; then
    # MongoDB health check
    if docker exec rdp-relay-mongodb-prod mongosh --eval "db.runCommand({ping:1})" > /dev/null 2>&1; then
      echo "✓ $name: Healthy"
    else
      echo "✗ $name: Unhealthy"
    fi
  elif [[ $url == *"redis"* ]]; then
    # Redis health check
    if docker exec rdp-relay-redis-prod redis-cli -a password ping > /dev/null 2>&1; then
      echo "✓ $name: Healthy"
    else
      echo "✗ $name: Unhealthy"
    fi
  else
    # HTTP health check
    if curl -sf $url > /dev/null 2>&1; then
      echo "✓ $name: Healthy"
    else
      echo "✗ $name: Unhealthy"
    fi
  fi
done

# Check disk space
echo -e "\n3. Disk Usage:"
df -h | grep -E "(Filesystem|/dev/)"

# Check memory usage
echo -e "\n4. Memory Usage:"
free -h

# Check load average
echo -e "\n5. System Load:"
uptime

echo -e "\n=== Health Check Complete ==="
```

---

## Backup & Recovery

### 1. MongoDB Backup Script
```bash
#!/bin/bash
# backup/mongodb-backup.sh

BACKUP_DIR="/opt/rdp-relay/backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="mongodb_backup_$DATE"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create MongoDB backup
docker exec rdp-relay-mongodb-prod mongodump \
  --uri="mongodb://admin:${MONGODB_PASSWORD}@localhost:27017/rdp_relay?authSource=admin" \
  --out="/tmp/backup"

# Copy backup from container
docker cp rdp-relay-mongodb-prod:/tmp/backup $BACKUP_DIR/$BACKUP_NAME

# Compress backup
cd $BACKUP_DIR
tar -czf ${BACKUP_NAME}.tar.gz $BACKUP_NAME
rm -rf $BACKUP_NAME

# Clean up old backups (keep last 7 days)
find $BACKUP_DIR -name "mongodb_backup_*.tar.gz" -mtime +7 -delete

echo "MongoDB backup completed: ${BACKUP_NAME}.tar.gz"
```

### 2. Redis Backup Script
```bash
#!/bin/bash
# backup/redis-backup.sh

BACKUP_DIR="/opt/rdp-relay/backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="redis_backup_$DATE"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create Redis backup
docker exec rdp-relay-redis-prod redis-cli -a ${REDIS_PASSWORD} BGSAVE

# Wait for backup to complete
while [ $(docker exec rdp-relay-redis-prod redis-cli -a ${REDIS_PASSWORD} LASTSAVE) -eq $(docker exec rdp-relay-redis-prod redis-cli -a ${REDIS_PASSWORD} LASTSAVE) ]; do
  sleep 1
done

# Copy RDB file
docker cp rdp-relay-redis-prod:/data/dump.rdb $BACKUP_DIR/${BACKUP_NAME}.rdb

echo "Redis backup completed: ${BACKUP_NAME}.rdb"
```

### 3. Application Data Backup Script
```bash
#!/bin/bash
# backup/full-backup.sh

BACKUP_ROOT="/opt/rdp-relay/backups"
DATE=$(date +%Y%m%d_%H%M%S)
FULL_BACKUP_DIR="$BACKUP_ROOT/full_backup_$DATE"

# Create backup directory
mkdir -p $FULL_BACKUP_DIR

echo "Starting full RDP Relay Platform backup..."

# Backup MongoDB
echo "Backing up MongoDB..."
./backup/mongodb-backup.sh

# Backup Redis
echo "Backing up Redis..."
./backup/redis-backup.sh

# Backup configuration files
echo "Backing up configuration..."
cp -r ./infra $FULL_BACKUP_DIR/
cp .env $FULL_BACKUP_DIR/
cp docker-compose.prod.yml $FULL_BACKUP_DIR/

# Backup certificates
echo "Backing up certificates..."
cp -r ./infra/certs $FULL_BACKUP_DIR/

# Backup logs (last 7 days only)
echo "Backing up recent logs..."
find ./logs -name "*.log" -mtime -7 -exec cp {} $FULL_BACKUP_DIR/logs/ \;

# Create compressed archive
echo "Creating compressed archive..."
cd $BACKUP_ROOT
tar -czf full_backup_$DATE.tar.gz full_backup_$DATE/
rm -rf full_backup_$DATE/

# Upload to S3 (optional)
if [ "$AWS_S3_BUCKET" != "" ]; then
  echo "Uploading to S3..."
  aws s3 cp full_backup_$DATE.tar.gz s3://$AWS_S3_BUCKET/rdp-relay-backups/
fi

echo "Full backup completed: full_backup_$DATE.tar.gz"
```

### 4. Recovery Procedures

#### MongoDB Recovery
```bash
#!/bin/bash
# recovery/mongodb-restore.sh

BACKUP_FILE=$1
TEMP_DIR="/tmp/restore"

if [ -z "$BACKUP_FILE" ]; then
  echo "Usage: $0 <backup_file.tar.gz>"
  exit 1
fi

# Extract backup
mkdir -p $TEMP_DIR
tar -xzf $BACKUP_FILE -C $TEMP_DIR

# Stop application services
docker-compose -f docker-compose.prod.yml stop portal-api relay-server

# Restore MongoDB
docker exec rdp-relay-mongodb-prod mongorestore \
  --uri="mongodb://admin:${MONGODB_PASSWORD}@localhost:27017/rdp_relay?authSource=admin" \
  --drop \
  /tmp/restore/rdp_relay

# Restart services
docker-compose -f docker-compose.prod.yml start portal-api relay-server

echo "MongoDB restoration completed"
```

#### Redis Recovery
```bash
#!/bin/bash
# recovery/redis-restore.sh

BACKUP_FILE=$1

if [ -z "$BACKUP_FILE" ]; then
  echo "Usage: $0 <redis_backup.rdb>"
  exit 1
fi

# Stop Redis
docker-compose -f docker-compose.prod.yml stop redis

# Replace RDB file
docker cp $BACKUP_FILE rdp-relay-redis-prod:/data/dump.rdb

# Start Redis
docker-compose -f docker-compose.prod.yml start redis

echo "Redis restoration completed"
```

---

## Performance Tuning

### 1. Database Optimization

#### MongoDB Performance Tuning
```javascript
// MongoDB performance optimization script
// Run: docker exec -it rdp-relay-mongodb-prod mongosh

use rdp_relay

// Create compound indexes for common queries
db.users.createIndex({ "tenantId": 1, "isActive": 1, "email": 1 })
db.sessions.createIndex({ "tenantId": 1, "status": 1, "createdAt": -1 })
db.agents.createIndex({ "tenantId": 1, "status": 1, "lastHeartbeat": -1 })

// Create TTL indexes for cleanup
db.sessions.createIndex({ "createdAt": 1 }, { expireAfterSeconds: 2592000 }) // 30 days
db.audit_logs.createIndex({ "timestamp": 1 }, { expireAfterSeconds: 7776000 }) // 90 days

// Enable profiling for slow queries
db.setProfilingLevel(1, { slowms: 100 })

// Check index usage
db.users.explain("executionStats").find({ "tenantId": ObjectId(), "isActive": true })
```

#### Redis Performance Configuration
```bash
# Redis performance tuning
cat >> ./infra/redis/redis-performance.conf << 'EOF'
# Memory optimization
hash-max-ziplist-entries 512
hash-max-ziplist-value 64
list-max-ziplist-size -2
set-max-intset-entries 512
zset-max-ziplist-entries 128
zset-max-ziplist-value 64

# Network optimization
tcp-keepalive 60
tcp-backlog 511
timeout 0

# Persistence optimization
save 900 1
save 300 10
save 60 10000
stop-writes-on-bgsave-error yes
rdbcompression yes
rdbchecksum yes

# Memory management
maxmemory-policy allkeys-lru
maxmemory-samples 5
EOF
```

### 2. Application Performance

#### .NET API Performance Settings
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 30000000,
      "KeepAliveTimeout": "00:02:00",
      "RequestHeadersTimeout": "00:00:30"
    }
  },
  "ConnectionStrings": {
    "MongoDb": "mongodb://admin:password@mongodb:27017/rdp_relay?authSource=admin&maxPoolSize=100&maxIdleTimeMS=300000",
    "Redis": "redis:6379,password=password,connectTimeout=5000,syncTimeout=5000,abortConnect=false"
  }
}
```

#### nginx Performance Optimization
```nginx
# nginx performance configuration
worker_processes auto;
worker_connections 1024;
worker_rlimit_nofile 2048;

events {
    use epoll;
    multi_accept on;
}

http {
    # Basic optimization
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    keepalive_requests 100;
    
    # Buffer sizes
    client_body_buffer_size 128k;
    client_max_body_size 50M;
    client_header_buffer_size 1k;
    large_client_header_buffers 4 4k;
    output_buffers 1 32k;
    postpone_output 1460;
    
    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types
        text/plain
        text/css
        text/xml
        text/javascript
        application/json
        application/javascript
        application/xml+rss
        application/atom+xml
        image/svg+xml;
    
    # Caching
    location ~* \.(jpg|jpeg|png|gif|ico|css|js)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=login:10m rate=5r/m;
    limit_req_zone $binary_remote_addr zone=api:10m rate=100r/m;
    
    # Connection limiting
    limit_conn_zone $binary_remote_addr zone=conn_limit_per_ip:10m;
    limit_conn conn_limit_per_ip 20;
}
```

### 3. System-level Optimization

#### Docker Performance Tuning
```bash
# Docker daemon optimization
cat > /etc/docker/daemon.json << 'EOF'
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "storage-driver": "overlay2",
  "storage-opts": [
    "overlay2.override_kernel_check=true"
  ],
  "default-ulimits": {
    "nofile": {
      "Name": "nofile",
      "Hard": 65536,
      "Soft": 65536
    }
  }
}
EOF

# Restart Docker
systemctl restart docker
```

#### System Limits Configuration
```bash
# /etc/security/limits.conf
* soft nofile 65536
* hard nofile 65536
* soft nproc 4096
* hard nproc 4096

# /etc/sysctl.conf
net.core.somaxconn = 1024
net.core.netdev_max_backlog = 5000
net.core.rmem_default = 262144
net.core.rmem_max = 16777216
net.core.wmem_default = 262144
net.core.wmem_max = 16777216
net.ipv4.tcp_rmem = 4096 65536 16777216
net.ipv4.tcp_wmem = 4096 65536 16777216
vm.swappiness = 10
vm.dirty_ratio = 15
vm.dirty_background_ratio = 5
```

---

## Windows Agent Deployment

### 1. Agent Installation Package

#### Create Agent Installer Script
```powershell
# deploy-agent.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$RelayServerUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$AgentKey,
    
    [string]$InstallPath = "C:\Program Files\RDP Relay Agent",
    [string]$ServiceName = "RdpRelayAgent"
)

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Exiting..."
    exit 1
}

Write-Host "Installing RDP Relay Agent..." -ForegroundColor Green

# Create installation directory
if (!(Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force
}

# Download agent files
$AgentUrl = "$RelayServerUrl/agent/download"
$ZipPath = "$env:TEMP\rdp-relay-agent.zip"

Write-Host "Downloading agent from $AgentUrl..."
try {
    Invoke-WebRequest -Uri $AgentUrl -OutFile $ZipPath -UseBasicParsing
    Expand-Archive -Path $ZipPath -DestinationPath $InstallPath -Force
    Remove-Item $ZipPath -Force
} catch {
    Write-Error "Failed to download agent: $_"
    exit 1
}

# Configure agent
$ConfigPath = "$InstallPath\appsettings.json"
$Config = @{
    "Relay" = @{
        "ServerUrl" = $RelayServerUrl
        "AgentKey" = $AgentKey
        "HeartbeatInterval" = 30000
        "ReconnectDelay" = 5000
    }
    "Logging" = @{
        "LogLevel" = @{
            "Default" = "Information"
        }
        "WriteTo" = @(
            @{
                "Name" = "File"
                "Args" = @{
                    "path" = "C:\ProgramData\RDP Relay Agent\logs\agent-.log"
                    "rollingInterval" = "Day"
                    "retainedFileCountLimit" = 7
                }
            }
        )
    }
} | ConvertTo-Json -Depth 4

Set-Content -Path $ConfigPath -Value $Config -Encoding UTF8

# Install as Windows Service
$ServicePath = "$InstallPath\RdpRelay.Agent.Win.exe"
if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping existing service..."
    Stop-Service $ServiceName
    Remove-Service $ServiceName
}

Write-Host "Installing Windows Service..."
New-Service -Name $ServiceName -BinaryPathName $ServicePath -DisplayName "RDP Relay Agent" -StartupType Automatic -Description "RDP Relay Agent Service"

# Configure service recovery
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000

# Start service
Write-Host "Starting RDP Relay Agent service..."
Start-Service $ServiceName

# Verify service status
$Service = Get-Service $ServiceName
if ($Service.Status -eq "Running") {
    Write-Host "RDP Relay Agent installed and started successfully!" -ForegroundColor Green
} else {
    Write-Error "Service installation failed. Check logs for details."
    exit 1
}

# Configure Windows Firewall
Write-Host "Configuring Windows Firewall..."
New-NetFirewallRule -DisplayName "RDP Relay Agent" -Direction Inbound -Protocol TCP -LocalPort 3389 -Action Allow
New-NetFirewallRule -DisplayName "RDP Relay Agent Outbound" -Direction Outbound -Protocol TCP -RemotePort 5001,9443 -Action Allow

Write-Host "Installation completed successfully!" -ForegroundColor Green
Write-Host "Agent Key: $AgentKey" -ForegroundColor Yellow
Write-Host "Relay Server: $RelayServerUrl" -ForegroundColor Yellow
```

### 2. Mass Deployment Script

#### Group Policy Deployment
```powershell
# mass-deploy-agents.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ComputerListFile,
    
    [Parameter(Mandatory=$true)]
    [string]$RelayServerUrl,
    
    [Parameter(Mandatory=$true)]
    [PSCredential]$Credential
)

# Read computer list
$Computers = Get-Content $ComputerListFile

# Deployment results
$Results = @()

foreach ($Computer in $Computers) {
    Write-Host "Deploying to $Computer..." -ForegroundColor Yellow
    
    try {
        # Generate unique agent key
        $AgentKey = [System.Guid]::NewGuid().ToString()
        
        # Copy installer to remote computer
        $Session = New-PSSession -ComputerName $Computer -Credential $Credential
        Copy-Item ".\deploy-agent.ps1" -Destination "C:\Temp\" -ToSession $Session
        
        # Execute installation remotely
        $Result = Invoke-Command -Session $Session -ScriptBlock {
            param($RelayUrl, $Key)
            & "C:\Temp\deploy-agent.ps1" -RelayServerUrl $RelayUrl -AgentKey $Key
        } -ArgumentList $RelayServerUrl, $AgentKey
        
        Remove-PSSession $Session
        
        $Results += [PSCustomObject]@{
            Computer = $Computer
            Status = "Success"
            AgentKey = $AgentKey
            Message = "Deployed successfully"
        }
        
        Write-Host "✓ Deployed to $Computer" -ForegroundColor Green
        
    } catch {
        $Results += [PSCustomObject]@{
            Computer = $Computer
            Status = "Failed"
            AgentKey = ""
            Message = $_.Exception.Message
        }
        
        Write-Host "✗ Failed to deploy to $Computer`: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Export results
$Results | Export-Csv "deployment-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').csv" -NoTypeInformation

Write-Host "`nDeployment Summary:" -ForegroundColor Yellow
Write-Host "Successful: $(($Results | Where-Object Status -eq 'Success').Count)" -ForegroundColor Green
Write-Host "Failed: $(($Results | Where-Object Status -eq 'Failed').Count)" -ForegroundColor Red
```

### 3. Agent Management Scripts

#### Agent Status Check
```powershell
# check-agent-status.ps1
param(
    [string]$ComputerName = "localhost"
)

$ServiceName = "RdpRelayAgent"

if ($ComputerName -eq "localhost") {
    $Service = Get-Service $ServiceName -ErrorAction SilentlyContinue
} else {
    $Service = Get-Service $ServiceName -ComputerName $ComputerName -ErrorAction SilentlyContinue
}

if ($Service) {
    Write-Host "Service Status: $($Service.Status)" -ForegroundColor $(if($Service.Status -eq "Running"){"Green"}else{"Red"})
    
    # Check last log entry
    $LogPath = "C:\ProgramData\RDP Relay Agent\logs"
    if ($ComputerName -ne "localhost") {
        $LogPath = "\\$ComputerName\C$\ProgramData\RDP Relay Agent\logs"
    }
    
    $LatestLog = Get-ChildItem $LogPath -Filter "agent-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($LatestLog) {
        $LastEntry = Get-Content $LatestLog.FullName | Select-Object -Last 1
        Write-Host "Last Log Entry: $LastEntry" -ForegroundColor Yellow
    }
} else {
    Write-Host "RDP Relay Agent service not found on $ComputerName" -ForegroundColor Red
}
```

#### Agent Update Script
```powershell
# update-agents.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersionUrl,
    
    [string]$ComputerListFile,
    [PSCredential]$Credential
)

$ServiceName = "RdpRelayAgent"
$InstallPath = "C:\Program Files\RDP Relay Agent"

if ($ComputerListFile) {
    $Computers = Get-Content $ComputerListFile
} else {
    $Computers = @("localhost")
}

foreach ($Computer in $Computers) {
    Write-Host "Updating agent on $Computer..." -ForegroundColor Yellow
    
    try {
        if ($Computer -eq "localhost") {
            # Local update
            Stop-Service $ServiceName
            
            # Backup current version
            $BackupPath = "$InstallPath.backup.$(Get-Date -Format 'yyyyMMdd')"
            if (!(Test-Path $BackupPath)) {
                Copy-Item $InstallPath $BackupPath -Recurse
            }
            
            # Download and extract new version
            $TempZip = "$env:TEMP\agent-update.zip"
            Invoke-WebRequest -Uri $NewVersionUrl -OutFile $TempZip -UseBasicParsing
            Expand-Archive -Path $TempZip -DestinationPath $InstallPath -Force
            Remove-Item $TempZip
            
            # Start service
            Start-Service $ServiceName
            
        } else {
            # Remote update
            $Session = New-PSSession -ComputerName $Computer -Credential $Credential
            
            Invoke-Command -Session $Session -ScriptBlock {
                param($Url, $Service, $Path)
                
                Stop-Service $Service
                
                # Download and update
                $TempZip = "$env:TEMP\agent-update.zip"
                Invoke-WebRequest -Uri $Url -OutFile $TempZip -UseBasicParsing
                Expand-Archive -Path $TempZip -DestinationPath $Path -Force
                Remove-Item $TempZip
                
                Start-Service $Service
                
            } -ArgumentList $NewVersionUrl, $ServiceName, $InstallPath
            
            Remove-PSSession $Session
        }
        
        Write-Host "✓ Updated agent on $Computer" -ForegroundColor Green
        
    } catch {
        Write-Host "✗ Failed to update agent on $Computer`: $($_.Exception.Message)" -ForegroundColor Red
    }
}
```

---

## Maintenance Procedures

### 1. Regular Maintenance Tasks

#### Daily Maintenance Script
```bash
#!/bin/bash
# maintenance/daily-maintenance.sh

echo "=== Daily Maintenance - $(date) ==="

# Health check
echo "1. Running health checks..."
./monitoring/health-check.sh

# Log rotation
echo "2. Rotating logs..."
find ./logs -name "*.log" -size +100M -exec logrotate {} \;

# Database cleanup
echo "3. Cleaning up old data..."
docker exec rdp-relay-mongodb-prod mongosh --eval "
  use rdp_relay;
  // Remove sessions older than 30 days
  db.sessions.deleteMany({createdAt: {\$lt: new Date(Date.now() - 30*24*60*60*1000)}});
  // Remove audit logs older than 90 days
  db.audit_logs.deleteMany({timestamp: {\$lt: new Date(Date.now() - 90*24*60*60*1000)}});
  print('Database cleanup completed');
"

# Redis memory cleanup
echo "4. Cleaning Redis cache..."
docker exec rdp-relay-redis-prod redis-cli -a ${REDIS_PASSWORD} --eval "
  local expired = 0
  local keys = redis.call('keys', '*:session:*')
  for i=1,#keys do
    if redis.call('ttl', keys[i]) == -1 then
      redis.call('del', keys[i])
      expired = expired + 1
    end
  end
  return expired
" 0

# Check disk space
echo "5. Checking disk space..."
df -h | awk '$5 > 80 {print "WARNING: " $1 " is " $5 " full"}'

# Container resource usage
echo "6. Container resource usage:"
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"

echo "Daily maintenance completed"
```

#### Weekly Maintenance Script
```bash
#!/bin/bash
# maintenance/weekly-maintenance.sh

echo "=== Weekly Maintenance - $(date) ==="

# Full backup
echo "1. Creating full backup..."
./backup/full-backup.sh

# Update container images
echo "2. Updating container images..."
docker-compose -f docker-compose.prod.yml pull

# Database optimization
echo "3. Optimizing database..."
docker exec rdp-relay-mongodb-prod mongosh --eval "
  use rdp_relay;
  db.runCommand({compact: 'users'});
  db.runCommand({compact: 'sessions'});
  db.runCommand({compact: 'agents'});
  db.runCommand({reIndex: 'users'});
  db.runCommand({reIndex: 'sessions'});
  db.runCommand({reIndex: 'agents'});
  print('Database optimization completed');
"

# SSL certificate check
echo "4. Checking SSL certificates..."
openssl x509 -in ./infra/certs/relay.crt -noout -dates | grep "After"

# Security scan
echo "5. Running security scan..."
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy system

echo "Weekly maintenance completed"
```

### 2. Update Procedures

#### Application Update Process
```bash
#!/bin/bash
# maintenance/update-application.sh

VERSION=${1:-latest}
BACKUP_DIR="/opt/rdp-relay/backups"

echo "=== RDP Relay Application Update ==="
echo "Target Version: $VERSION"

# Pre-update backup
echo "1. Creating pre-update backup..."
./backup/full-backup.sh

# Stop services
echo "2. Stopping application services..."
docker-compose -f docker-compose.prod.yml stop portal-web portal-api relay-server

# Pull new images
echo "3. Pulling new container images..."
if [ "$VERSION" = "latest" ]; then
  docker-compose -f docker-compose.prod.yml pull portal-web portal-api relay-server
else
  # Update image tags in docker-compose file
  sed -i "s|:latest|:$VERSION|g" docker-compose.prod.yml
  docker-compose -f docker-compose.prod.yml pull portal-web portal-api relay-server
fi

# Database migrations (if needed)
echo "4. Running database migrations..."
docker-compose -f docker-compose.prod.yml run --rm portal-api dotnet ef database update

# Start services
echo "5. Starting updated services..."
docker-compose -f docker-compose.prod.yml up -d portal-web portal-api relay-server

# Verify update
echo "6. Verifying update..."
sleep 30

# Health check
if ./monitoring/health-check.sh | grep -q "✗"; then
  echo "❌ Update failed - rolling back..."
  
  # Rollback
  docker-compose -f docker-compose.prod.yml down
  # Restore from backup
  ./recovery/full-restore.sh $BACKUP_DIR/full_backup_$(date +%Y%m%d)*.tar.gz
  
  echo "Rollback completed"
  exit 1
else
  echo "✅ Update completed successfully"
fi
```

### 3. Certificate Renewal

#### Let's Encrypt Certificate Renewal
```bash
#!/bin/bash
# maintenance/renew-certificates.sh

DOMAIN="your-domain.com"
CERT_PATH="./infra/certs"

echo "=== SSL Certificate Renewal ==="

# Check current certificate expiration
EXPIRY=$(openssl x509 -enddate -noout -in $CERT_PATH/relay.crt | cut -d= -f2)
EXPIRY_DATE=$(date -d "$EXPIRY" +%s)
CURRENT_DATE=$(date +%s)
DAYS_UNTIL_EXPIRY=$(( (EXPIRY_DATE - CURRENT_DATE) / 86400 ))

echo "Current certificate expires in $DAYS_UNTIL_EXPIRY days"

# Renew if less than 30 days remaining
if [ $DAYS_UNTIL_EXPIRY -lt 30 ]; then
  echo "Renewing certificate..."
  
  # Stop nginx temporarily
  docker-compose -f docker-compose.prod.yml stop nginx
  
  # Run Certbot
  docker run --rm \
    -p 80:80 \
    -v $(pwd)/infra/certs:/etc/letsencrypt \
    certbot/certbot certonly \
    --standalone \
    --email admin@$DOMAIN \
    --agree-tos \
    --no-eff-email \
    -d $DOMAIN
  
  # Convert to PFX for .NET
  openssl pkcs12 -export \
    -out $CERT_PATH/relay.pfx \
    -inkey $CERT_PATH/live/$DOMAIN/privkey.pem \
    -in $CERT_PATH/live/$DOMAIN/cert.pem \
    -certfile $CERT_PATH/live/$DOMAIN/chain.pem \
    -passout pass:${CERT_PASSWORD}
  
  # Update nginx certificate paths
  cp $CERT_PATH/live/$DOMAIN/fullchain.pem $CERT_PATH/relay.crt
  cp $CERT_PATH/live/$DOMAIN/privkey.pem $CERT_PATH/relay.key
  
  # Restart services
  docker-compose -f docker-compose.prod.yml start nginx
  docker-compose -f docker-compose.prod.yml restart relay-server
  
  echo "Certificate renewed successfully"
else
  echo "Certificate renewal not needed"
fi
```

---

## Troubleshooting

### 1. Common Issues and Solutions

#### Container Issues
```bash
# Issue: Container won't start
# Solution: Check logs and resource constraints

# Check container status
docker-compose -f docker-compose.prod.yml ps

# Check specific container logs
docker-compose -f docker-compose.prod.yml logs portal-api

# Check resource usage
docker stats --no-stream

# Restart specific service
docker-compose -f docker-compose.prod.yml restart portal-api

# Clean restart all services
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d
```

#### Database Connection Issues
```bash
# Issue: MongoDB connection failures
# Solution: Verify connection string and credentials

# Test MongoDB connection
docker exec rdp-relay-mongodb-prod mongosh \
  "mongodb://admin:${MONGODB_PASSWORD}@localhost:27017/rdp_relay?authSource=admin" \
  --eval "db.runCommand({connectionStatus : 1})"

# Check MongoDB logs
docker-compose -f docker-compose.prod.yml logs mongodb

# Fix connection string in environment
docker exec rdp-relay-portal-api-prod printenv | grep MongoDB

# Issue: Redis connection failures
# Solution: Verify Redis configuration

# Test Redis connection
docker exec rdp-relay-redis-prod redis-cli -a ${REDIS_PASSWORD} ping

# Check Redis configuration
docker exec rdp-relay-redis-prod redis-cli -a ${REDIS_PASSWORD} CONFIG GET "*"
```

#### Authentication Issues
```bash
# Issue: JWT token validation failures
# Solution: Check JWT configuration and clock sync

# Verify JWT settings
docker exec rdp-relay-portal-api-prod printenv | grep JWT

# Check system time synchronization
date
docker exec rdp-relay-portal-api-prod date

# Test token generation
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"password"}' | jq .

# Issue: User role enum mismatch
# Solution: Update database role values

docker exec rdp-relay-mongodb-prod mongosh \
  "mongodb://admin:${MONGODB_PASSWORD}@localhost:27017/rdp_relay?authSource=admin" \
  --eval "db.users.updateMany({role: 'admin'}, {\$set: {role: 'SystemAdmin'}})"
```

#### Network Issues
```bash
# Issue: Service communication failures
# Solution: Check Docker networking

# Check network configuration
docker network ls
docker network inspect rdp-relay_rdp-relay-network

# Test inter-service communication
docker exec rdp-relay-portal-api-prod nslookup mongodb
docker exec rdp-relay-portal-api-prod ping -c 3 redis

# Check firewall rules
ufw status
iptables -L
```

### 2. Performance Issues

#### High CPU Usage
```bash
# Identify high CPU containers
docker stats --no-stream | sort -k3 -hr

# Check application-level CPU usage
docker exec rdp-relay-portal-api-prod top -p 1

# Scale horizontally if needed
docker-compose -f docker-compose.prod.yml up -d --scale portal-api=3
```

#### High Memory Usage
```bash
# Check memory usage by container
docker stats --format "table {{.Container}}\t{{.MemUsage}}\t{{.MemPerc}}"

# Check for memory leaks
docker exec rdp-relay-portal-api-prod ps aux --sort=-%mem | head

# Implement memory limits
# Add to docker-compose.prod.yml:
# deploy:
#   resources:
#     limits:
#       memory: 1G
#     reservations:
#       memory: 512M
```

#### Database Performance Issues
```bash
# Check slow queries in MongoDB
docker exec rdp-relay-mongodb-prod mongosh \
  "mongodb://admin:${MONGODB_PASSWORD}@localhost:27017/rdp_relay?authSource=admin" \
  --eval "db.system.profile.find().limit(5).sort({ts:-1}).pretty()"

# Check Redis performance
docker exec rdp-relay-redis-prod redis-cli -a ${REDIS_PASSWORD} --latency-history

# Optimize database indexes
docker exec rdp-relay-mongodb-prod mongosh \
  "mongodb://admin:${MONGODB_PASSWORD}@localhost:27017/rdp_relay?authSource=admin" \
  --eval "db.users.getIndexes()"
```

### 3. Diagnostic Tools and Scripts

#### System Diagnostic Script
```bash
#!/bin/bash
# troubleshooting/system-diagnostic.sh

echo "=== RDP Relay System Diagnostic ==="
echo "Timestamp: $(date)"
echo "Hostname: $(hostname)"
echo "Uptime: $(uptime)"

# System resources
echo -e "\n=== System Resources ==="
echo "CPU: $(nproc) cores"
echo "Memory: $(free -h | grep '^Mem:' | awk '{print $2}')"
echo "Disk: $(df -h | grep -E '^/dev/' | awk '{print $1 " " $4}')"

# Docker information
echo -e "\n=== Docker Information ==="
docker version --format '{{.Server.Version}}'
docker info --format '{{.Name}}: {{.ServerVersion}}'

# Container status
echo -e "\n=== Container Status ==="
docker-compose -f docker-compose.prod.yml ps

# Network connectivity
echo -e "\n=== Network Connectivity ==="
ping -c 1 8.8.8.8 >/dev/null 2>&1 && echo "✓ Internet: Connected" || echo "✗ Internet: Disconnected"
curl -sf http://localhost:8080 >/dev/null && echo "✓ Web Portal: Accessible" || echo "✗ Web Portal: Inaccessible"
curl -sf http://localhost:5000/health >/dev/null && echo "✓ API: Healthy" || echo "✗ API: Unhealthy"

# Service logs (last 10 lines)
echo -e "\n=== Recent Logs ==="
for service in portal-api relay-server mongodb redis nginx; do
  echo "--- $service ---"
  docker-compose -f docker-compose.prod.yml logs --tail=5 $service 2>/dev/null || echo "Service not found"
done

echo -e "\n=== Diagnostic Complete ==="
```

This comprehensive deployment and operations guide covers all aspects of running the RDP Relay Platform in production, from initial deployment to ongoing maintenance and troubleshooting. The platform is now fully documented and ready for enterprise deployment.
