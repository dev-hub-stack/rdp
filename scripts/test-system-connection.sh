#!/bin/bash

# Connection Testing Script for RDP Relay Components
# Tests connectivity between all RDP Relay system components

set -e

echo "🌐 RDP Relay System Connection Testing"
echo "======================================"
echo ""

ERRORS=0
SUCCESS=0

# Helper functions
test_http() {
    local url="$1"
    local description="$2"
    local timeout="${3:-10}"
    
    echo -n "Testing $description... "
    
    if curl -s -f -m "$timeout" "$url" >/dev/null 2>&1; then
        echo "✅ SUCCESS"
        ((SUCCESS++))
        return 0
    else
        echo "❌ FAILED"
        ((ERRORS++))
        return 1
    fi
}

test_tcp() {
    local host="$1"
    local port="$2"
    local description="$3"
    local timeout="${4:-5}"
    
    echo -n "Testing $description... "
    
    if timeout "$timeout" bash -c "echo >/dev/tcp/$host/$port" 2>/dev/null; then
        echo "✅ SUCCESS"
        ((SUCCESS++))
        return 0
    else
        echo "❌ FAILED"
        ((ERRORS++))
        return 1
    fi
}

# Basic connectivity tests
echo "🔌 Basic Connectivity Tests"
echo "---------------------------"

# Test web portal
test_http "http://localhost:3000" "Web Portal (port 3000)"

# Test portal API
test_http "http://localhost:5001/health" "Portal API Health (port 5001)"
test_http "http://localhost:5001/api/agents" "Portal API Agents Endpoint"

# Test relay server
test_http "http://localhost:8080/health" "Relay Server Health (port 8080)"

# Test TCP connections
echo ""
echo "🔌 TCP Connection Tests"
echo "----------------------"

test_tcp "localhost" "3000" "Web Portal TCP"
test_tcp "localhost" "5001" "Portal API TCP"
test_tcp "localhost" "8080" "Relay Server TCP"
test_tcp "localhost" "27017" "MongoDB TCP"
test_tcp "localhost" "6379" "Redis TCP"

# Test database connections
echo ""
echo "🗄️  Database Connection Tests"
echo "-----------------------------"

echo -n "Testing MongoDB connection... "
if docker-compose exec -T mongodb mongo --quiet --eval "db.stats()" >/dev/null 2>&1; then
    echo "✅ SUCCESS"
    ((SUCCESS++))
else
    echo "❌ FAILED"
    ((ERRORS++))
fi

echo -n "Testing Redis connection... "
if docker-compose exec -T redis redis-cli ping | grep -q "PONG"; then
    echo "✅ SUCCESS"
    ((SUCCESS++))
else
    echo "❌ FAILED"
    ((ERRORS++))
fi

# Test API endpoints
echo ""
echo "🔗 API Endpoint Tests"
echo "--------------------"

# Test agent registration endpoint
echo -n "Testing agent registration endpoint... "
response=$(curl -s -w "%{http_code}" -X POST "http://localhost:5001/api/agents" \
    -H "Content-Type: application/json" \
    -d '{"name":"test-agent","connectCode":"TEST123"}' \
    -o /dev/null 2>/dev/null)

if [ "$response" = "200" ] || [ "$response" = "201" ] || [ "$response" = "400" ]; then
    echo "✅ SUCCESS (HTTP $response)"
    ((SUCCESS++))
else
    echo "❌ FAILED (HTTP $response)"
    ((ERRORS++))
fi

# Test session creation endpoint
echo -n "Testing session creation endpoint... "
response=$(curl -s -w "%{http_code}" -X POST "http://localhost:5001/api/sessions" \
    -H "Content-Type: application/json" \
    -d '{"connectCode":"TEST123","clientIp":"127.0.0.1"}' \
    -o /dev/null 2>/dev/null)

if [ "$response" = "200" ] || [ "$response" = "201" ] || [ "$response" = "400" ] || [ "$response" = "404" ]; then
    echo "✅ SUCCESS (HTTP $response)"
    ((SUCCESS++))
else
    echo "❌ FAILED (HTTP $response)"
    ((ERRORS++))
fi

# Test WebSocket connection
echo ""
echo "🔌 WebSocket Connection Tests"
echo "----------------------------"

echo -n "Testing WebSocket endpoint... "
# Simple WebSocket test using curl
if curl -s -N -H "Connection: Upgrade" -H "Upgrade: websocket" -H "Sec-WebSocket-Key: test" \
    "http://localhost:8080/ws" -m 5 >/dev/null 2>&1; then
    echo "✅ SUCCESS"
    ((SUCCESS++))
else
    echo "❌ FAILED"
    ((ERRORS++))
fi

# Performance tests
echo ""
echo "⚡ Performance Tests"
echo "------------------"

# Test response times
echo -n "Testing API response time... "
start_time=$(date +%s%N)
curl -s "http://localhost:5001/health" >/dev/null 2>&1
end_time=$(date +%s%N)
response_time=$(( (end_time - start_time) / 1000000 ))

if [ $response_time -lt 1000 ]; then
    echo "✅ SUCCESS (${response_time}ms)"
    ((SUCCESS++))
elif [ $response_time -lt 5000 ]; then
    echo "⚠️  SLOW (${response_time}ms)"
    ((SUCCESS++))
else
    echo "❌ TOO SLOW (${response_time}ms)"
    ((ERRORS++))
fi

# Test concurrent connections
echo -n "Testing concurrent connections... "
concurrent_success=0
for i in {1..5}; do
    if curl -s -f "http://localhost:5001/health" >/dev/null 2>&1 &; then
        ((concurrent_success++))
    fi
done
wait

if [ $concurrent_success -eq 5 ]; then
    echo "✅ SUCCESS (5/5 concurrent requests)"
    ((SUCCESS++))
elif [ $concurrent_success -gt 2 ]; then
    echo "⚠️  PARTIAL ($concurrent_success/5 concurrent requests)"
    ((SUCCESS++))
else
    echo "❌ FAILED ($concurrent_success/5 concurrent requests)"
    ((ERRORS++))
fi

# Service health checks
echo ""
echo "🏥 Service Health Checks"
echo "-----------------------"

# Check Docker container health
services=("relay" "portal-api" "portal-web" "mongodb" "redis")
for service in "${services[@]}"; do
    echo -n "Checking $service health... "
    status=$(docker-compose ps -q "$service" | xargs docker inspect --format='{{.State.Health.Status}}' 2>/dev/null || echo "unknown")
    
    if [ "$status" = "healthy" ]; then
        echo "✅ HEALTHY"
        ((SUCCESS++))
    elif [ "$status" = "unknown" ]; then
        # Check if container is running instead
        if docker-compose ps "$service" | grep -q "Up"; then
            echo "✅ RUNNING"
            ((SUCCESS++))
        else
            echo "❌ NOT RUNNING"
            ((ERRORS++))
        fi
    else
        echo "❌ UNHEALTHY ($status)"
        ((ERRORS++))
    fi
done

# Network diagnostics
echo ""
echo "🌐 Network Diagnostics"
echo "---------------------"

echo "Port listening status:"
netstat -tulpn 2>/dev/null | grep -E ":(3000|5001|8080|27017|6379)" | while read line; do
    echo "  $line"
done

echo ""
echo "Docker network status:"
docker network ls | grep rdp || echo "  No RDP-specific networks found"

# Summary
echo ""
echo "📊 Connection Test Summary"
echo "========================="
echo "Successful tests: $SUCCESS"
echo "Failed tests: $ERRORS"
echo ""

if [ $ERRORS -eq 0 ]; then
    echo "✅ All connection tests passed!"
    echo "🚀 System is ready for RDP relay operations."
else
    echo "❌ $ERRORS connection test(s) failed."
    echo ""
    echo "🔧 Troubleshooting steps:"
    echo "1. Check if all Docker services are running: docker-compose ps"
    echo "2. Review service logs: docker-compose logs [service-name]"
    echo "3. Verify firewall and port settings"
    echo "4. Run diagnostic collection: ./scripts/collect-diagnostics.sh"
    echo "5. Check SESSION_TROUBLESHOOTING.md for detailed guidance"
    
    exit 1
fi
