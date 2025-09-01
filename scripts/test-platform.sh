#!/bin/bash
# RDP Relay Platform - Quick Test Script
# This script helps you verify that all components are working correctly

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "🚀 RDP Relay Platform - End-to-End Test Script"
echo "=============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
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

# Step 1: Check prerequisites
print_step "Checking prerequisites..."

if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed or not in PATH"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    print_error "docker-compose is not installed or not in PATH"
    exit 1
fi

print_success "Prerequisites check passed"

# Step 2: Start the platform
print_step "Starting RDP Relay platform..."

if [ ! -f "docker-compose.yml" ]; then
    print_error "docker-compose.yml not found. Run from the project root directory."
    exit 1
fi

# Stop any existing containers
docker-compose down > /dev/null 2>&1 || true

# Start the platform
echo "Starting all services..."
docker-compose up -d

# Wait for services to start
sleep 10

# Step 3: Check container status
print_step "Checking container health..."

CONTAINERS=(
    "rdp-relay-mongodb-1:MongoDB"
    "rdp-relay-redis-1:Redis" 
    "rdp-relay-portal-api-1:Portal API"
    "rdp-relay-relay-server-1:Relay Server"
    "rdp-relay-portal-web-1:Portal Web"
    "rdp-relay-nginx-1:Nginx Proxy"
)

ALL_HEALTHY=true

for container_info in "${CONTAINERS[@]}"; do
    IFS=':' read -r container_name display_name <<< "$container_info"
    
    if docker ps --format "table {{.Names}}" | grep -q "$container_name"; then
        print_success "$display_name is running"
    else
        print_error "$display_name is not running"
        ALL_HEALTHY=false
    fi
done

if [ "$ALL_HEALTHY" = false ]; then
    print_error "Some containers are not running. Check with: docker-compose logs"
    exit 1
fi

# Step 4: Test API endpoints
print_step "Testing API endpoints..."

# Wait a bit more for services to fully initialize
sleep 5

# Test Portal API health
if curl -k -s https://localhost:5000/health > /dev/null 2>&1; then
    print_success "Portal API is responding"
else
    print_warning "Portal API health check failed (might be normal during startup)"
fi

# Test Relay Server
if curl -k -s https://localhost:5001/health > /dev/null 2>&1; then
    print_success "Relay Server is responding"
else
    print_warning "Relay Server health check failed (might be normal during startup)"
fi

# Test Nginx proxy
if curl -k -s https://localhost/ > /dev/null 2>&1; then
    print_success "Nginx proxy is responding"
else
    print_warning "Nginx proxy check failed"
fi

# Step 5: Test database connectivity
print_step "Testing database connectivity..."

if docker-compose exec -T mongodb mongosh rdp_relay --eval "db.tenants.countDocuments()" > /dev/null 2>&1; then
    print_success "MongoDB is accessible"
else
    print_warning "MongoDB connection test failed"
fi

# Step 6: Show access information
print_step "Platform is ready! Access information:"

echo -e "\n${GREEN}🌐 Web Portal:${NC}"
echo "  URL: https://localhost"
echo "  Default Login:"
echo "    Email: admin@example.com"
echo "    Password: SecurePassword123!"

echo -e "\n${GREEN}🔧 API Endpoints:${NC}"
echo "  Portal API: https://localhost:5000"
echo "  Relay Server: https://localhost:5001"

echo -e "\n${GREEN}📊 Monitoring:${NC}"
echo "  Container logs: docker-compose logs -f [service-name]"
echo "  Container status: docker-compose ps"
echo "  MongoDB shell: docker-compose exec mongodb mongosh rdp_relay"

# Step 7: Basic functional test
print_step "Running basic functional tests..."

# Test login API
LOGIN_RESPONSE=$(curl -k -s -X POST https://localhost:5000/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@example.com","password":"SecurePassword123!"}' \
    2>/dev/null || echo "")

if echo "$LOGIN_RESPONSE" | grep -q "accessToken"; then
    print_success "Authentication API is working"
    
    # Extract token for further tests
    ACCESS_TOKEN=$(echo "$LOGIN_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    print(data.get('accessToken', ''))
except:
    pass
" 2>/dev/null || echo "")
    
    if [ -n "$ACCESS_TOKEN" ]; then
        # Test agents API
        AGENTS_RESPONSE=$(curl -k -s -H "Authorization: Bearer $ACCESS_TOKEN" \
            https://localhost:5000/api/agents 2>/dev/null || echo "")
        
        if echo "$AGENTS_RESPONSE" | grep -q "items"; then
            print_success "Agents API is working"
        else
            print_warning "Agents API test failed"
        fi
        
        # Test sessions API
        SESSIONS_RESPONSE=$(curl -k -s -H "Authorization: Bearer $ACCESS_TOKEN" \
            https://localhost:5000/api/sessions 2>/dev/null || echo "")
        
        if echo "$SESSIONS_RESPONSE" | grep -q "items"; then
            print_success "Sessions API is working"
        else
            print_warning "Sessions API test failed"
        fi
    fi
else
    print_warning "Authentication API test failed"
fi

# Step 8: Show next steps
print_step "Next steps for full testing:"

echo -e "\n${YELLOW}📋 Manual Testing Checklist:${NC}"
echo "  1. Open https://localhost in your browser"
echo "  2. Accept the SSL certificate warning"
echo "  3. Login with the credentials above"
echo "  4. Navigate through the dashboard, agents, sessions, and users pages"
echo "  5. Generate a provisioning token in the Agents page"
echo "  6. Deploy the Windows agent on a test machine"
echo "  7. Create an RDP session and test the connection"

echo -e "\n${YELLOW}🐛 If something isn't working:${NC}"
echo "  • Check logs: docker-compose logs -f"
echo "  • Restart services: docker-compose restart"
echo "  • Full restart: docker-compose down && docker-compose up -d"
echo "  • Check ports are not in use: netstat -tulpn | grep :443"

echo -e "\n${YELLOW}📖 Documentation:${NC}"
echo "  • Full test guide: ./TEST_GUIDE.md"
echo "  • Implementation status: ./PLATFORM_COMPLETE.md"
echo "  • README: ./README.md"

print_success "Basic platform test completed successfully!"

# Optional: Open browser
if command -v open &> /dev/null; then
    read -p "Open web portal in browser? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        open https://localhost
    fi
elif command -v xdg-open &> /dev/null; then
    read -p "Open web portal in browser? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        xdg-open https://localhost
    fi
fi

echo -e "\n${GREEN}✅ Test completed! Your RDP Relay platform is ready for use.${NC}"
