#!/bin/bash

# RDP Relay Diagnostic Collection Script
# This script collects diagnostic information for troubleshooting session establishment issues

set -e

echo "🔍 RDP Relay Diagnostic Collection"
echo "=================================="
echo ""

TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
DIAG_DIR="diagnostics_${TIMESTAMP}"
mkdir -p "$DIAG_DIR"

echo "📁 Creating diagnostic report in: $DIAG_DIR"
echo ""

# System Information
echo "🖥️  Collecting system information..."
{
    echo "=== SYSTEM INFORMATION ==="
    echo "Date: $(date)"
    echo "Hostname: $(hostname)"
    echo "OS: $(uname -a)"
    echo "Docker version: $(docker --version)"
    echo "Docker Compose version: $(docker-compose --version)"
    echo ""
} > "$DIAG_DIR/system_info.txt"

# Docker Services Status
echo "🐳 Checking Docker services..."
{
    echo "=== DOCKER SERVICES STATUS ==="
    docker-compose ps || echo "ERROR: Could not get Docker services status"
    echo ""
    
    echo "=== DOCKER IMAGES ==="
    docker images --filter "reference=rdp-relay*" || echo "No RDP Relay images found"
    echo ""
} > "$DIAG_DIR/docker_status.txt"

# Service Logs
echo "📋 Collecting service logs..."

# Relay Server Logs
echo "  - Relay server logs..."
{
    echo "=== RELAY SERVER LOGS (Last 100 lines) ==="
    docker-compose logs --tail=100 relay 2>/dev/null || echo "ERROR: Could not get relay server logs"
    echo ""
} > "$DIAG_DIR/relay_logs.txt"

# Portal API Logs
echo "  - Portal API logs..."
{
    echo "=== PORTAL API LOGS (Last 100 lines) ==="
    docker-compose logs --tail=100 portal-api 2>/dev/null || echo "ERROR: Could not get portal API logs"
    echo ""
} > "$DIAG_DIR/portal_api_logs.txt"

# MongoDB Logs
echo "  - MongoDB logs..."
{
    echo "=== MONGODB LOGS (Last 50 lines) ==="
    docker-compose logs --tail=50 mongodb 2>/dev/null || echo "ERROR: Could not get MongoDB logs"
    echo ""
} > "$DIAG_DIR/mongodb_logs.txt"

# Redis Logs
echo "  - Redis logs..."
{
    echo "=== REDIS LOGS (Last 50 lines) ==="
    docker-compose logs --tail=50 redis 2>/dev/null || echo "ERROR: Could not get Redis logs"
    echo ""
} > "$DIAG_DIR/redis_logs.txt"

# Network Connectivity Tests
echo "🌐 Testing network connectivity..."
{
    echo "=== NETWORK CONNECTIVITY TESTS ==="
    
    echo "--- Testing Relay Server (port 8080) ---"
    if curl -s -m 10 "http://localhost:8080/health" >/dev/null 2>&1; then
        echo "✅ Relay server is responding"
        curl -s "http://localhost:8080/health" || echo "Could not get health response"
    else
        echo "❌ Relay server is not responding"
    fi
    echo ""
    
    echo "--- Testing Portal API (port 5001) ---"
    if curl -s -m 10 "http://localhost:5001/health" >/dev/null 2>&1; then
        echo "✅ Portal API is responding"
        curl -s "http://localhost:5001/health" || echo "Could not get health response"
    else
        echo "❌ Portal API is not responding"
    fi
    echo ""
    
    echo "--- Testing Web Portal (port 3000) ---"
    if curl -s -m 10 "http://localhost:3000" >/dev/null 2>&1; then
        echo "✅ Web portal is responding"
    else
        echo "❌ Web portal is not responding"
    fi
    echo ""
    
    echo "--- Port Status ---"
    netstat -tulpn | grep -E ":(3000|5001|8080|27017|6379)" || echo "Could not get port status"
    echo ""
} > "$DIAG_DIR/network_tests.txt"

# Agent Information
echo "🖥️  Collecting agent information..."
{
    echo "=== AGENT INFORMATION ==="
    
    echo "--- Registered Agents (API) ---"
    if curl -s -m 10 "http://localhost:5001/api/agents" 2>/dev/null; then
        echo ""
    else
        echo "❌ Could not retrieve agent information from API"
    fi
    echo ""
    
    echo "--- Agent Database Records ---"
    if docker-compose exec -T mongodb mongo rdprelay --quiet --eval "db.agents.find().pretty()" 2>/dev/null; then
        echo ""
    else
        echo "❌ Could not retrieve agent information from database"
    fi
    echo ""
} > "$DIAG_DIR/agent_info.txt"

# Session Information
echo "📱 Collecting session information..."
{
    echo "=== SESSION INFORMATION ==="
    
    echo "--- Recent Sessions (Database) ---"
    if docker-compose exec -T mongodb mongo rdprelay --quiet --eval "db.sessions.find().sort({createdAt: -1}).limit(10).pretty()" 2>/dev/null; then
        echo ""
    else
        echo "❌ Could not retrieve session information from database"
    fi
    echo ""
} > "$DIAG_DIR/session_info.txt"

# Configuration Files
echo "⚙️  Collecting configuration information..."
{
    echo "=== CONFIGURATION FILES ==="
    
    echo "--- Docker Compose Configuration ---"
    if [ -f "docker-compose.yml" ]; then
        cat docker-compose.yml
    else
        echo "❌ docker-compose.yml not found"
    fi
    echo ""
    
    echo "--- Environment Variables ---"
    if [ -f ".env" ]; then
        # Mask sensitive information
        sed 's/=.*$/=***MASKED***/g' .env
    else
        echo "No .env file found"
    fi
    echo ""
    
    echo "--- Git Status ---"
    git status 2>/dev/null || echo "Not a git repository or git not available"
    echo ""
    
    echo "--- Git Log (Last 5 commits) ---"
    git log --oneline -5 2>/dev/null || echo "Could not get git log"
    echo ""
} > "$DIAG_DIR/configuration.txt"

# Resource Usage
echo "📊 Collecting resource usage..."
{
    echo "=== RESOURCE USAGE ==="
    
    echo "--- Docker Container Stats ---"
    timeout 10 docker stats --no-stream 2>/dev/null || echo "Could not get Docker stats"
    echo ""
    
    echo "--- System Resources ---"
    echo "Memory Usage:"
    free -h 2>/dev/null || echo "Could not get memory info"
    echo ""
    
    echo "Disk Usage:"
    df -h 2>/dev/null | head -10 || echo "Could not get disk info"
    echo ""
    
    echo "Load Average:"
    uptime 2>/dev/null || echo "Could not get load average"
    echo ""
} > "$DIAG_DIR/resources.txt"

# Create summary report
echo "📄 Creating summary report..."
{
    echo "RDP RELAY DIAGNOSTIC SUMMARY"
    echo "============================"
    echo "Generated: $(date)"
    echo "Hostname: $(hostname)"
    echo ""
    
    echo "QUICK STATUS CHECKS:"
    echo "-------------------"
    
    # Check if services are up
    if docker-compose ps | grep -q "Up"; then
        echo "✅ Docker services are running"
    else
        echo "❌ Some Docker services are down"
    fi
    
    # Check connectivity
    if curl -s -m 5 "http://localhost:5001/health" >/dev/null 2>&1; then
        echo "✅ Portal API is accessible"
    else
        echo "❌ Portal API is not accessible"
    fi
    
    if curl -s -m 5 "http://localhost:8080/health" >/dev/null 2>&1; then
        echo "✅ Relay server is accessible"
    else
        echo "❌ Relay server is not accessible"
    fi
    
    # Check for agents
    agent_count=$(curl -s -m 5 "http://localhost:5001/api/agents" 2>/dev/null | jq length 2>/dev/null || echo "unknown")
    echo "📊 Registered agents: $agent_count"
    
    echo ""
    echo "NEXT STEPS:"
    echo "----------"
    echo "1. Review the files in this diagnostic package"
    echo "2. Check SESSION_TROUBLESHOOTING.md for detailed guidance"
    echo "3. Focus on any ❌ items in the status checks above"
    echo ""
    
    echo "FILES INCLUDED:"
    echo "--------------"
    ls -la "$DIAG_DIR/"
    echo ""
} > "$DIAG_DIR/SUMMARY.txt"

# Create archive
echo "📦 Creating diagnostic archive..."
tar -czf "${DIAG_DIR}.tar.gz" "$DIAG_DIR/"

echo ""
echo "✅ Diagnostic collection complete!"
echo ""
echo "📁 Files created:"
echo "   - Directory: $DIAG_DIR/"
echo "   - Archive: ${DIAG_DIR}.tar.gz"
echo ""
echo "📋 Next steps:"
echo "   1. Review $DIAG_DIR/SUMMARY.txt for quick status"
echo "   2. Check docs/SESSION_TROUBLESHOOTING.md for detailed guidance"
echo "   3. Include ${DIAG_DIR}.tar.gz when requesting support"
echo ""
echo "🔍 Quick view of summary:"
echo "========================"
cat "$DIAG_DIR/SUMMARY.txt"
