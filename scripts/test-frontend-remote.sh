#!/bin/bash

# Remote Frontend Testing Script for RDP Relay
# Test the deployed system on the remote server

# Configuration
REMOTE_SERVER="159.89.112.134"
REMOTE_URL="http://$REMOTE_SERVER:8080"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_step() {
    echo -e "\n${BLUE}==>${NC} $1"
}

print_success() {
    echo -e "${GREEN}✅${NC} $1"
}

print_test() {
    echo -e "${YELLOW}🧪 TEST:${NC} $1"
}

echo "🌐 RDP Relay Remote Frontend Testing"
echo "====================================="
echo "Remote Server: $REMOTE_SERVER"
echo "Portal URL: $REMOTE_URL"
echo ""

# Test 1: Check if remote server is accessible
print_step "Testing Remote Server Connectivity"

if ping -c 1 -W 5000 $REMOTE_SERVER >/dev/null 2>&1; then
    print_success "Server $REMOTE_SERVER is reachable"
else
    echo "❌ Server $REMOTE_SERVER is not reachable"
    exit 1
fi

# Test 2: Check if web portal is accessible
print_step "Testing Web Portal Accessibility"

if curl -s --connect-timeout 10 "$REMOTE_URL" >/dev/null; then
    print_success "Web portal is accessible at $REMOTE_URL"
else
    echo "⚠️  Web portal may not be ready yet. Deployment might still be in progress."
    echo "   Please wait for deployment to complete and try again."
fi

# Test 3: Check API health
print_step "Testing API Health"

API_HEALTH=$(curl -s --connect-timeout 10 "$REMOTE_URL/api/health" 2>/dev/null)
if [[ -n "$API_HEALTH" ]]; then
    print_success "API is responding"
    echo "   API Response: $API_HEALTH"
else
    echo "⚠️  API may not be ready yet"
fi

# Open browser for manual testing
print_step "Opening Remote Portal for Testing"

echo "Opening remote portal..."
if command -v open >/dev/null; then
    open $REMOTE_URL
elif command -v xdg-open >/dev/null; then
    xdg-open $REMOTE_URL
else
    echo "Please open $REMOTE_URL in your browser"
fi

echo ""
echo "🎯 REMOTE FRONTEND TESTING CHECKLIST"
echo "====================================="

print_test "1. ACCESS REMOTE PORTAL"
echo "   • URL: $REMOTE_URL"
echo "   • Expected: Portal loads without errors"
echo "   • Expected: Login page appears"
echo ""

print_test "2. LOGIN TO REMOTE SYSTEM"
echo "   • Email: admin@rdprelay.local"
echo "   • Password: admin123"
echo "   • Expected: Successful authentication"
echo "   • Expected: Dashboard loads"
echo ""

print_test "3. TEST GENERATE TOKEN (Remote)"
echo "   • Navigate to 'Agents' page"
echo "   • Click 'Generate Token'"
echo "   • Expected: Token generated successfully"
echo "   • Expected: No HTTP 415 errors"
echo ""

print_test "4. TEST ADD AGENT (Remote)"
echo "   • Click 'Add Agent' button"
echo "   • Fill form with:"
echo "     Agent Name: 'Remote Test Agent'"
echo "     Machine ID: 'REMOTE-TEST-$(date +%s)'"
echo "     Machine Name: 'Remote Desktop'"
echo "     IP Address: '10.0.0.100'"
echo "   • Click 'Create'"
echo "   • Expected: Agent created successfully"
echo ""

print_test "5. NETWORK PERFORMANCE"
echo "   • Test page load speeds"
echo "   • Test API response times"
echo "   • Expected: Reasonable performance over internet"
echo ""

print_test "6. CROSS-BROWSER TESTING"
echo "   • Test in multiple browsers"
echo "   • Test from different devices"
echo "   • Expected: Consistent functionality"
echo ""

echo ""
echo "🌍 REMOTE vs LOCAL COMPARISON"
echo "=============================="
echo "Both systems should have identical functionality:"
echo "• Local:  http://localhost:8080"
echo "• Remote: $REMOTE_URL"
echo ""
echo "Key differences:"
echo "• Remote: Accessible from anywhere"
echo "• Remote: Real production environment"
echo "• Remote: Network latency considerations"
echo "• Local:  Faster response times"
echo "• Local:  Full debugging access"
echo ""

echo "📊 TESTING ENDPOINTS"
echo "===================="
echo "• Portal:     $REMOTE_URL"
echo "• API Health: $REMOTE_URL/api/health"
echo "• API Docs:   http://$REMOTE_SERVER:5000/swagger"
echo "• Direct Web: http://$REMOTE_SERVER:3000"
echo ""

echo "🔑 PROVISIONING TOKEN TESTING"
echo "=============================="
echo "After generating a token in the web portal:"
echo "1. Copy the provisioning token"
echo "2. Use it to configure a Windows agent"
echo "3. Verify agent appears in portal as 'Online'"
echo "4. Test RDP session creation"
echo ""

echo "🎉 REMOTE TESTING SUCCESS CRITERIA"
echo "=================================="
echo "✅ Remote portal accessible from internet"
echo "✅ All frontend functionality works over WAN"
echo "✅ Generate Token works on remote server"
echo "✅ Add Agent works with remote database"
echo "✅ Performance is acceptable over internet"
echo "✅ Cross-browser compatibility maintained"
echo "✅ Mobile/responsive design works remotely"
echo ""

print_success "Remote frontend testing guide ready!"
echo ""
echo "💡 Next Steps:"
echo "1. Complete deployment on $REMOTE_SERVER"
echo "2. Open $REMOTE_URL in your browser"
echo "3. Follow the testing checklist above"
echo "4. Compare functionality with local version"
echo "5. Test Windows agent connectivity to remote server"
