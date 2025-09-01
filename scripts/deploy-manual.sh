#!/bin/bash

# RDP Relay Platform - Manual Remote Deployment Script
# This script will prompt for password at each step

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_step() {
    echo -e "\n${BLUE}==>${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

# Configuration
REMOTE_SERVER="159.89.112.134"
REMOTE_USER="root"
PROJECT_NAME="rdp-relay"
REMOTE_PATH="/opt/rdp-relay"

echo "🚀 RDP Relay Platform - Manual Remote Deployment"
echo "================================================"
echo "Target Server: $REMOTE_SERVER"
echo "Remote User: $REMOTE_USER"
echo "Remote Path: $REMOTE_PATH"
echo ""
echo "⚠️  Note: You will be prompted for the SSH password multiple times"
echo "    Password: Skido2025#22Apples"
echo ""

# Step 1: Test SSH connection
print_step "Testing SSH connection to remote server..."

if ssh -o ConnectTimeout=10 -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_SERVER "echo 'SSH connection successful'" 2>/dev/null; then
    print_success "SSH connection to $REMOTE_SERVER successful"
else
    print_error "Cannot connect to $REMOTE_SERVER. Please check credentials."
    exit 1
fi

# Step 2: Prepare remote server
print_step "Preparing remote server environment..."
echo "Enter password when prompted..."

ssh -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_SERVER << 'EOF'
echo "Updating system packages..."
apt update > /dev/null 2>&1

echo "Installing Docker if needed..."
if ! command -v docker &> /dev/null; then
    echo "Installing Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh > /dev/null 2>&1
    rm get-docker.sh
    systemctl enable docker
    systemctl start docker
    echo "Docker installed successfully"
else
    echo "Docker already installed"
fi

echo "Installing Docker Compose if needed..."
if ! command -v docker-compose &> /dev/null; then
    echo "Installing Docker Compose..."
    curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose > /dev/null 2>&1
    chmod +x /usr/local/bin/docker-compose
    echo "Docker Compose installed successfully"
else
    echo "Docker Compose already installed"
fi

echo "Creating project directory..."
mkdir -p /opt/rdp-relay
chown -R root:root /opt/rdp-relay

echo "Remote server preparation completed"
EOF

print_success "Remote server environment prepared"

# Step 3: Upload project files
print_step "Uploading project files to remote server..."
echo "This may take a few minutes depending on network speed..."

# Create a clean copy of the project
TEMP_DIR=$(mktemp -d)
echo "Creating clean project copy in: $TEMP_DIR"

cp -r . "$TEMP_DIR/rdp-relay"
cd "$TEMP_DIR/rdp-relay"

# Remove unnecessary files to reduce upload size
rm -rf .git
rm -rf node_modules
rm -rf */bin
rm -rf */obj
rm -rf data/mongodb/*
rm -rf data/redis/*
rm -rf logs/*
find . -name "*.log" -delete

# Upload files using rsync
cd ..
echo "Uploading files (enter password when prompted)..."
rsync -avz --progress --delete -e 'ssh -o StrictHostKeyChecking=no' rdp-relay/ $REMOTE_USER@$REMOTE_SERVER:$REMOTE_PATH/

# Clean up
rm -rf "$TEMP_DIR"

print_success "Project files uploaded successfully"

# Step 4: Configure environment
print_step "Configuring environment for remote server..."

ssh -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_SERVER << EOF
cd $REMOTE_PATH

echo "Creating production environment file..."
cat > .env << 'ENVEOF'
# MongoDB Configuration
MONGODB_PASSWORD=rdp_relay_production_\$(date +%s)
MONGODB_DATABASE=rdp_relay

# Redis Configuration  
REDIS_PASSWORD=rdp_relay_redis_production_\$(date +%s)

# JWT Configuration
JWT_SECRET_KEY=\$(openssl rand -base64 32)
JWT_ISSUER=RdpRelayPortal
JWT_AUDIENCE=RdpRelayPortal

# Certificate Configuration
CERT_PASSWORD=rdp_relay_cert_production_\$(date +%s)

# Portal Web Configuration - Update with server IP
VITE_API_BASE_URL=http://$REMOTE_SERVER:5000
VITE_RELAY_WS_URL=wss://$REMOTE_SERVER:9443
VITE_APP_TITLE=RDP Relay Portal

# Docker Compose Configuration
COMPOSE_PROJECT_NAME=rdp-relay
COMPOSE_FILE=docker-compose.yml

# Production Configuration
ASPNETCORE_ENVIRONMENT=Production
SERILOG_MINIMUM_LEVEL=Information
ENVEOF

echo "Creating required directories..."
mkdir -p logs/{portal-api,relay,nginx}
mkdir -p data/{mongodb,redis}
mkdir -p infra/certs

echo "Setting permissions..."
chmod +x deploy.sh 2>/dev/null || true
chmod +x test-platform.sh 2>/dev/null || true

echo "Environment configuration completed"
EOF

print_success "Environment configured successfully"

# Step 5: Deploy the platform
print_step "Deploying RDP Relay platform on remote server..."
echo "This will take several minutes to build and start all containers..."

ssh -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_SERVER << EOF
cd $REMOTE_PATH

echo "Stopping any existing containers..."
docker-compose down 2>/dev/null || true

echo "Building and starting the platform..."
echo "This may take 5-10 minutes for the first build..."
docker-compose up -d --build

echo "Waiting for services to initialize..."
sleep 45

echo "Checking container status..."
docker-compose ps
EOF

print_success "Platform deployed successfully"

# Step 6: Health checks
print_step "Running health checks..."

ssh -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_SERVER << EOF
cd $REMOTE_PATH

echo "=== Final Service Status ==="
docker-compose ps

echo -e "\n=== Testing Web Portal ==="
sleep 5
curl -f -m 10 http://localhost:8080/ > /dev/null 2>&1 && echo "✓ Web Portal is accessible" || echo "⚠ Web Portal not ready yet"

echo -e "\n=== Testing API Health ==="
curl -f -m 10 http://localhost:8080/api/health > /dev/null 2>&1 && echo "✓ API is healthy" || echo "⚠ API not ready yet"

echo -e "\n=== Container Resource Usage ==="
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"

echo -e "\n=== Recent Container Logs ==="
echo "--- Portal API (last 5 lines) ---"
docker-compose logs --tail=5 portal-api 2>/dev/null || echo "No logs yet"

echo "--- Portal Web (last 5 lines) ---"  
docker-compose logs --tail=5 portal-web 2>/dev/null || echo "No logs yet"

echo "--- Relay Server (last 5 lines) ---"
docker-compose logs --tail=5 relay-server 2>/dev/null || echo "No logs yet"
EOF

# Step 7: Final summary
print_step "🎉 Deployment Summary"

echo ""
echo "🚀 RDP Relay Platform deployed successfully!"
echo ""
echo "📋 Access Information:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🌐 Web Portal:       http://$REMOTE_SERVER:8080"
echo "🔧 API Endpoint:     http://$REMOTE_SERVER:5000"  
echo "🔄 Relay Server:     wss://$REMOTE_SERVER:9443"
echo "📊 Direct Web:       http://$REMOTE_SERVER:3000"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "🔑 Default Login Credentials:"
echo "   Email:    admin@rdprelay.local"
echo "   Password: admin123"
echo ""
echo "📋 Next Steps:"
echo "   1. Open http://$REMOTE_SERVER:8080 in your browser"
echo "   2. Login with the credentials above"
echo "   3. Test the Agent and Token generation features"
echo "   4. Generate provisioning tokens for Windows agents"
echo ""
echo "🛠️  Remote Server Management:"
echo "   SSH Access: ssh $REMOTE_USER@$REMOTE_SERVER"
echo "   Project Path: $REMOTE_PATH"
echo "   View Logs: ssh $REMOTE_USER@$REMOTE_SERVER 'cd $REMOTE_PATH && docker-compose logs'"
echo "   Restart: ssh $REMOTE_USER@$REMOTE_SERVER 'cd $REMOTE_PATH && docker-compose restart'"
echo ""
echo "🔍 Testing Commands:"
echo "   curl http://$REMOTE_SERVER:8080/api/health"
echo "   curl http://$REMOTE_SERVER:8080/"
echo ""

print_success "Remote deployment completed! 🚀"
print_warning "Note: It may take a few more minutes for all services to be fully ready"
