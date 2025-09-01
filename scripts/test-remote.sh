#!/bin/bash

# RDP Relay Platform - Remote Testing Script
# Test the deployed system on the remote server

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
REMOTE_PATH="/opt/rdp-relay"

echo "🧪 RDP Relay Platform - Remote Testing"
echo "======================================="
echo "Testing Server: $REMOTE_SERVER"
echo ""

# Step 1: Test basic connectivity
print_step "Testing server connectivity..."

# Test if server is reachable
if ping -c 1 $REMOTE_SERVER > /dev/null 2>&1; then
    print_success "Server $REMOTE_SERVER is reachable"
else
    print_error "Server $REMOTE_SERVER is not reachable"
    exit 1
fi

# Step 2: Test web portal access
print_step "Testing web portal access..."

WEB_URL="http://$REMOTE_SERVER:8080"
if curl -f -s --connect-timeout 10 "$WEB_URL" > /dev/null; then
    print_success "Web portal is accessible at $WEB_URL"
else
    print_error "Web portal is not accessible at $WEB_URL"
fi

# Step 3: Test API endpoints
print_step "Testing API endpoints..."

API_BASE="http://$REMOTE_SERVER:5000/api"

# Test health endpoint
if curl -f -s --connect-timeout 10 "$API_BASE/health" > /dev/null; then
    print_success "API health endpoint is working"
else
    print_warning "API health endpoint not responding (this is expected if no health endpoint exists)"
fi

# Test login endpoint
LOGIN_RESPONSE=$(curl -s -X POST \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@rdprelay.local","password":"admin123"}' \
    "$API_BASE/auth/login" 2>/dev/null || echo "")

if [[ $LOGIN_RESPONSE == *"token"* ]] || [[ $LOGIN_RESPONSE == *"accessToken"* ]]; then
    print_success "API login endpoint is working"
    
    # Extract token for further testing (if possible)
    TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"[^"]*token[^"]*":"[^"]*"' | cut -d'"' -f4 | head -1)
    
    if [ ! -z "$TOKEN" ]; then
        print_success "Authentication token obtained"
        
        # Test protected endpoint
        AGENTS_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" "$API_BASE/tenants/{tenantId}/agents" 2>/dev/null || echo "")
        
        if [[ $AGENTS_RESPONSE == *"items"* ]] || [[ $AGENTS_RESPONSE == *"[]"* ]]; then
            print_success "Protected API endpoints are working"
        else
            print_warning "Protected API endpoints may not be working correctly"
        fi
    fi
else
    print_warning "API login may not be working correctly"
fi

# Step 4: Test service status on remote server
print_step "Checking service status on remote server..."

if command -v sshpass &> /dev/null; then
    SSH_CMD="sshpass -p '$REMOTE_PASSWORD' ssh -o StrictHostKeyChecking=no"
else
    SSH_CMD="ssh"
    print_warning "sshpass not available. You may be prompted for password."
fi

$SSH_CMD $REMOTE_USER@$REMOTE_SERVER << EOF
cd $REMOTE_PATH

echo "=== Docker Container Status ==="
docker-compose ps

echo -e "\n=== Service Ports ==="
netstat -tlnp | grep -E ':(3000|5000|5001|8080|9443|27017|6379)' || echo "Some ports may not be listening"

echo -e "\n=== Disk Usage ==="
df -h /opt/rdp-relay

echo -e "\n=== Recent Logs Summary ==="
echo "--- Portal API (last 5 lines) ---"
docker-compose logs --tail=5 portal-api 2>/dev/null || echo "No portal-api logs"

echo "--- Portal Web (last 5 lines) ---"
docker-compose logs --tail=5 portal-web 2>/dev/null || echo "No portal-web logs"

echo "--- Relay Server (last 5 lines) ---"
docker-compose logs --tail=5 relay-server 2>/dev/null || echo "No relay-server logs"
EOF

# Step 5: Performance and load test
print_step "Running basic performance tests..."

print_warning "Testing web portal response time..."
WEB_TIME=$(curl -o /dev/null -s -w "%{time_total}" --connect-timeout 10 "$WEB_URL" 2>/dev/null || echo "0")
echo "Web portal response time: ${WEB_TIME}s"

print_warning "Testing API response time..."
API_TIME=$(curl -o /dev/null -s -w "%{time_total}" --connect-timeout 10 "$API_BASE/auth/login" 2>/dev/null || echo "0")
echo "API response time: ${API_TIME}s"

# Step 6: Generate test report
print_step "Generating test report..."

echo ""
echo "📋 Test Report Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🌐 Server:           $REMOTE_SERVER"
echo "🔧 Web Portal:       http://$REMOTE_SERVER:8080"
echo "📡 API Endpoint:     http://$REMOTE_SERVER:5000"
echo "⚡ Web Response:     ${WEB_TIME}s"
echo "⚡ API Response:     ${API_TIME}s"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Step 7: Interactive testing menu
while true; do
    echo ""
    echo "🛠️  Interactive Testing Options:"
    echo "1. Open web portal in browser"
    echo "2. Test agent registration"
    echo "3. Monitor server logs"
    echo "4. Check system resources"
    echo "5. Restart services"
    echo "6. Exit"
    echo ""
    read -p "Choose an option (1-6): " choice
    
    case $choice in
        1)
            echo "Opening web portal in browser..."
            if command -v open &> /dev/null; then
                open "http://$REMOTE_SERVER:8080"
            elif command -v xdg-open &> /dev/null; then
                xdg-open "http://$REMOTE_SERVER:8080"
            else
                echo "Please open http://$REMOTE_SERVER:8080 in your browser manually"
            fi
            ;;
        2)
            echo "Testing agent token generation..."
            curl -X POST -H "Content-Type: application/json" \
                -d '{"groupId":null}' \
                "$API_BASE/tenants/{tenantId}/agents/provisioning-token" 2>/dev/null || echo "Token generation test failed"
            ;;
        3)
            echo "Monitoring server logs (Ctrl+C to stop)..."
            $SSH_CMD $REMOTE_USER@$REMOTE_SERVER "cd $REMOTE_PATH && docker-compose logs -f --tail=20"
            ;;
        4)
            echo "Checking system resources..."
            $SSH_CMD $REMOTE_USER@$REMOTE_SERVER << 'EOF'
echo "=== CPU Usage ==="
top -bn1 | grep "Cpu(s)" | sed "s/.*, *\([0-9.]*\)%* id.*/\1/" | awk '{print "CPU Usage: " 100 - $1 "%"}'

echo -e "\n=== Memory Usage ==="
free -h

echo -e "\n=== Disk Usage ==="
df -h

echo -e "\n=== Docker Stats ==="
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"
EOF
            ;;
        5)
            echo "Restarting services..."
            $SSH_CMD $REMOTE_USER@$REMOTE_SERVER "cd $REMOTE_PATH && docker-compose restart"
            echo "Services restarted!"
            ;;
        6)
            echo "Exiting..."
            break
            ;;
        *)
            echo "Invalid option. Please choose 1-6."
            ;;
    esac
done

print_success "Remote testing completed! 🎉"
