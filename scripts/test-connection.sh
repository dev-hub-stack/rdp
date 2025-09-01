#!/bin/bash

echo "🔍 Testing connection to 159.89.112.134..."

# Test 1: Basic ping
echo "1. Testing ping connectivity..."
if ping -c 3 -t 10 159.89.112.134 >/dev/null 2>&1; then
    echo "✓ Server responds to ping"
else
    echo "✗ Server does not respond to ping (this might be normal if ICMP is blocked)"
fi

# Test 2: Try to connect to SSH port
echo ""
echo "2. Testing SSH port connectivity..."
if nc -z -w 10 159.89.112.134 22 >/dev/null 2>&1; then
    echo "✓ SSH port 22 is accessible"
else
    echo "✗ SSH port 22 is not accessible"
fi

# Test 3: Manual SSH attempt with password
echo ""
echo "3. Attempting manual SSH connection..."
echo "   This will prompt for password: Skido2025#22Apples"
ssh -o ConnectTimeout=30 -o StrictHostKeyChecking=no root@159.89.112.134 "hostname; uptime"
