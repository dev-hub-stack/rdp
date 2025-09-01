#!/bin/bash

# RDP Relay Platform - Remote Server Deployment Script
# Deploy and test the RDP Relay system on a remote server

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
REMOTE_PASSWORD="Skido2025#22Apples"
PROJECT_NAME="rdp-relay"
REMOTE_PATH="/opt/rdp-relay"

echo "🚀 RDP Relay Platform - Remote Deployment"
echo "=========================================="
echo "Target Server: $REMOTE_SERVER"
echo "Remote User: $REMOTE_USER"
echo "Remote Path: $REMOTE_PATH"
echo ""

# Step 1: Check local prerequisites
print_step "Checking local prerequisites..."

if ! command -v rsync &> /dev/null; then
    print_error "rsync is not installed. Please install rsync first."
    exit 1
fi

if ! command -v sshpass &> /dev/null; then
    print_warning "sshpass is not installed. You'll need to enter password manually for SSH connections."
    SSHPASS_CMD=""
else
    SSHPASS_CMD="sshpass -p '$REMOTE_PASSWORD'"
    print_success "sshpass available for automated SSH"
fi

print_success "Local prerequisites check passed"

# Step 2: Test SSH connection
print_step "Testing SSH connection to remote server..."

if [ -n "$SSHPASS_CMD" ]; then
    SSH_CMD="sshpass -p '$REMOTE_PASSWORD' ssh -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"
    SCP_CMD="sshpass -p '$REMOTE_PASSWORD' scp -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"
    RSYNC_CMD="sshpass -p '$REMOTE_PASSWORD' rsync -avz --delete -e 'ssh -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null'"
else
    SSH_CMD="ssh -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"
    SCP_CMD="scp -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"
    RSYNC_CMD="rsync -avz --delete -e 'ssh -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null'"
    print_warning "You will be prompted for the SSH password multiple times"
fi

# Test connection
print_step "Attempting SSH connection (this may take a moment)..."

# Try with a longer timeout and more verbose error reporting
if [ -n "$SSHPASS_CMD" ]; then
    TEST_CMD="sshpass -p '$REMOTE_PASSWORD' ssh -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"
else
    TEST_CMD="ssh -o ConnectTimeout=30 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"
fi

if $TEST_CMD $REMOTE_USER@$REMOTE_SERVER "echo 'SSH connection successful'" 2>/dev/null; then
    print_success "SSH connection to $REMOTE_SERVER successful"
else
    print_error "Cannot connect to $REMOTE_SERVER"
    echo "Troubleshooting steps:"
    echo "1. Check if server is reachable: ping $REMOTE_SERVER"
    echo "2. Check if SSH port is open: telnet $REMOTE_SERVER 22"
    echo "3. Verify credentials are correct"
    echo "4. Try manual SSH: ssh root@$REMOTE_SERVER"
    
    # Try to provide more specific error information
    echo ""
    echo "Testing network connectivity..."
    if ping -c 1 -W 5000 $REMOTE_SERVER >/dev/null 2>&1; then
        echo "✓ Server is reachable via ping"
        echo "✗ SSH connection failed - check SSH service and credentials"
    else
        echo "✗ Server is not reachable via ping - check network connectivity"
    fi
    
    exit 1
fi

# Step 3: Prepare remote server
print_step "Preparing remote server environment..."

$SSH_CMD $REMOTE_USER@$REMOTE_SERVER << 'EOF'
# Update system packages
apt update && apt upgrade -y

# Install Docker if not already installed
if ! command -v docker &> /dev/null; then
    echo "Installing Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
    systemctl enable docker
    systemctl start docker
fi

# Install Docker Compose if not already installed
if ! command -v docker-compose &> /dev/null; then
    echo "Installing Docker Compose..."
    curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
fi

# Create project directory
mkdir -p /opt/rdp-relay
chown -R root:root /opt/rdp-relay

echo "Remote server prepared successfully"
EOF

print_success "Remote server environment prepared"

# Step 4: Upload project files
print_step "Uploading project files to remote server..."

# Create a temporary directory with only the files we need
TEMP_DIR=$(mktemp -d)
echo "Created temporary directory: $TEMP_DIR"

# Copy necessary files
cp -r . "$TEMP_DIR/rdp-relay"

# Remove unnecessary files to reduce upload size
cd "$TEMP_DIR/rdp-relay"
rm -rf .git
rm -rf node_modules
rm -rf */bin
rm -rf */obj
rm -rf data/
rm -rf logs/
find . -name "*.log" -delete

# Upload files to remote server
cd ..
$RSYNC_CMD rdp-relay/ $REMOTE_USER@$REMOTE_SERVER:$REMOTE_PATH/

# Clean up temporary directory
rm -rf "$TEMP_DIR"

print_success "Project files uploaded successfully"

# Step 5: Configure environment for remote server
print_step "Configuring environment for remote server..."

$SSH_CMD $REMOTE_USER@$REMOTE_SERVER << EOF
cd $REMOTE_PATH

# Create production environment file
cat > .env << 'ENVEOF'
# MongoDB Configuration
MONGODB_PASSWORD=rdp_relay_production_$(date +%s)
MONGODB_DATABASE=rdp_relay

# Redis Configuration  
REDIS_PASSWORD=rdp_relay_redis_production_$(date +%s)

# JWT Configuration
JWT_SECRET_KEY=$(openssl rand -base64 32)
JWT_ISSUER=RdpRelayPortal
JWT_AUDIENCE=RdpRelayPortal

# Certificate Configuration
CERT_PASSWORD=rdp_relay_cert_production_$(date +%s)

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

# Create required directories
mkdir -p logs/{portal-api,relay,nginx}
mkdir -p data/{mongodb,redis}
mkdir -p infra/certs

# Set proper permissions
chmod +x deploy.sh
chmod +x test-platform.sh

echo "Environment configured for production"
EOF

print_success "Environment configured successfully"

# Step 6: Deploy the platform
print_step "Deploying RDP Relay platform on remote server..."

$SSH_CMD $REMOTE_USER@$REMOTE_SERVER << EOF
cd $REMOTE_PATH

# Stop any existing containers
docker-compose down || true

# Build and start the platform
docker-compose up -d --build

# Wait for services to start
echo "Waiting for services to start..."
sleep 30

# Check container status
echo "Container status:"
docker-compose ps

echo "Deployment completed!"
EOF

print_success "Platform deployed successfully"

# Step 7: Run health checks
print_step "Running health checks..."

sleep 10

$SSH_CMD $REMOTE_USER@$REMOTE_SERVER << EOF
cd $REMOTE_PATH

echo "=== Service Health Check ==="
docker-compose ps

echo -e "\n=== Testing API Health ==="
curl -f http://localhost:8080/api/health || echo "API health check failed"

echo -e "\n=== Container Logs (last 10 lines each) ==="
echo "--- Portal API ---"
docker-compose logs --tail=10 portal-api

echo "--- Portal Web ---"  
docker-compose logs --tail=10 portal-web

echo "--- Relay Server ---"
docker-compose logs --tail=10 relay-server
EOF

# Step 8: Display access information
print_step "Deployment Summary"

echo ""
echo "🎉 RDP Relay Platform deployed successfully!"
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
echo "   3. Generate provisioning tokens"
echo "   4. Deploy Windows agents using the tokens"
echo ""
echo "🛠️  Remote Server Management:"
echo "   SSH: ssh $REMOTE_USER@$REMOTE_SERVER"
echo "   Project Path: $REMOTE_PATH"
echo "   Logs: docker-compose -f $REMOTE_PATH/docker-compose.yml logs"
echo ""

print_success "Deployment completed successfully! 🚀"
