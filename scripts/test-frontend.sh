#!/bin/bash

# RDP Relay Frontend Testing Guide
# Run this script to get step-by-step frontend testing instructions

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

print_test() {
    echo -e "${YELLOW}🧪 TEST:${NC} $1"
}

print_success() {
    echo -e "${GREEN}✅${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ️${NC} $1"
}

echo "🧪 RDP Relay Frontend Testing Guide"
echo "===================================="
echo ""

# Step 1: Check if system is running
print_step "Step 1: Checking System Status"

if docker-compose ps | grep -q "Up"; then
    print_success "Local system is running"
    LOCAL_URL="http://localhost:8080"
    echo "Local Portal URL: $LOCAL_URL"
else
    echo "❌ Local system is not running. Start with: docker-compose up -d"
    exit 1
fi

# Step 2: Test API connectivity
print_step "Step 2: Testing API Connectivity"

if curl -s http://localhost:8080/api/health > /dev/null; then
    print_success "API is responding"
else
    echo "❌ API is not responding"
    exit 1
fi

# Step 3: Open browser for testing
print_step "Step 3: Opening Browser for Testing"

echo "Opening web portal..."
if command -v open > /dev/null; then
    open $LOCAL_URL
elif command -v xdg-open > /dev/null; then
    xdg-open $LOCAL_URL
else
    echo "Please open $LOCAL_URL in your browser"
fi

echo ""
echo "🎯 FRONTEND TESTING CHECKLIST"
echo "=============================="

print_test "1. LOGIN TESTING"
echo "   • Navigate to: $LOCAL_URL"
echo "   • Email: admin@rdprelay.local"
echo "   • Password: admin123"
echo "   • Expected: Successful login, redirected to dashboard"
echo ""

print_test "2. GENERATE TOKEN TESTING (Fixed Issue #1)"
echo "   • Go to 'Agents' page"
echo "   • Click 'Generate Token' button"
echo "   • Expected: Token dialog appears (NO 415 error!)"
echo "   • Expected: Valid token string is displayed"
echo "   • Expected: Expiration time is shown"
echo ""

print_test "3. ADD AGENT TESTING (Fixed Issue #2)"
echo "   • On Agents page, click 'Add Agent' button"
echo "   • VERIFY these fields are present:"
echo "     ✓ Agent Name (required)"
echo "     ✓ Description (optional)"
echo "     ✓ Machine ID (required) ← NEW FIELD!"
echo "     ✓ Machine Name (optional) ← NEW FIELD!"
echo "     ✓ IP Address (optional) ← NEW FIELD!"
echo ""
echo "   • Fill out the form:"
echo "     Agent Name: 'Frontend Test Agent'"
echo "     Machine ID: 'FRONTEND-TEST-$(date +%s)'"
echo "     Machine Name: 'Test Desktop'"
echo "     IP Address: '192.168.1.100'"
echo ""
echo "   • Click 'Create'"
echo "   • Expected: Agent created successfully (NO validation errors!)"
echo "   • Expected: Agent appears in list with 'Offline' status"
echo ""

print_test "4. DASHBOARD TESTING"
echo "   • Navigate to 'Dashboard'"
echo "   • Expected: Statistics show updated agent count"
echo "   • Expected: Charts and metrics display properly"
echo "   • Expected: No JavaScript errors in console"
echo ""

print_test "5. BROWSER CONSOLE TESTING"
echo "   • Open Developer Tools (F12)"
echo "   • Check Console tab"
echo "   • Expected: No JavaScript errors"
echo "   • Expected: No failed API requests"
echo "   • Expected: No CORS errors"
echo ""

print_test "6. RESPONSIVE DESIGN TESTING"
echo "   • Resize browser window"
echo "   • Test mobile view (responsive design)"
echo "   • Expected: Layout adapts properly"
echo "   • Expected: All functionality works on mobile"
echo ""

print_test "7. CROSS-PAGE NAVIGATION"
echo "   • Test navigation between pages:"
echo "     - Dashboard → Agents → Users → Sessions"
echo "   • Expected: Smooth transitions"
echo "   • Expected: No loading errors"
echo "   • Expected: Authentication persists"
echo ""

echo ""
echo "🎉 SUCCESS CRITERIA"
echo "==================="
echo "Your frontend testing is successful when:"
echo "✅ Login works without errors"
echo "✅ Generate Token works (no HTTP 415 error)"
echo "✅ Add Agent form has all required fields"
echo "✅ Add Agent creates successfully (no validation errors)"
echo "✅ Dashboard shows updated statistics"
echo "✅ No JavaScript errors in browser console"
echo "✅ Responsive design works properly"
echo "✅ All page navigation works smoothly"
echo ""

echo "📊 TESTING RESULTS"
echo "=================="
echo "After completing the tests above, document your results:"
echo ""
echo "✅ PASS: Generate Token functionality"
echo "✅ PASS: Add Agent with all fields" 
echo "✅ PASS: Agent creation and listing"
echo "✅ PASS: Dashboard statistics"
echo "✅ PASS: User interface responsiveness"
echo "✅ PASS: Cross-browser compatibility"
echo ""

echo "🔗 Additional Testing URLs:"
echo "=========================="
echo "• Web Portal:     $LOCAL_URL"
echo "• API Health:     http://localhost:8080/api/health"
echo "• Direct Web:     http://localhost:3000"
echo "• API Swagger:    http://localhost:5000/swagger"
echo ""

echo "💡 Tips for Testing:"
echo "==================="
echo "• Keep browser Developer Tools open (F12)"
echo "• Check Network tab for API call details"
echo "• Test with different browsers"
echo "• Try mobile/responsive view"
echo "• Test with slow network (throttling)"
echo ""

print_success "Frontend testing guide completed!"
echo "Start testing by opening: $LOCAL_URL"
