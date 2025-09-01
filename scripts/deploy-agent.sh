#!/bin/bash

# Windows Agent Deployment Script
# This script helps deploy the RDP Relay Windows Agent as a Windows Service

set -e

echo "🪟 RDP Relay Windows Agent Deployment"

# Check if running on Windows (WSL/Git Bash)
if [[ "$OSTYPE" != "msys"* ]] && [[ "$OSTYPE" != "cygwin"* ]] && [[ ! -f /proc/version ]] || ! grep -qi microsoft /proc/version 2>/dev/null; then
    echo "❌ This script should be run on Windows or WSL"
    echo "   Please use PowerShell or run from WSL on Windows"
    exit 1
fi

# Configuration
AGENT_DIR="agent-win/RdpRelay.Agent.Win"
INSTALL_DIR="C:/Program Files/RdpRelay/Agent"
SERVICE_NAME="RdpRelayAgent"
SERVICE_DISPLAY_NAME="RDP Relay Windows Agent"

echo "📋 Agent Deployment Configuration:"
echo "   Source: $AGENT_DIR"
echo "   Install Directory: $INSTALL_DIR"
echo "   Service Name: $SERVICE_NAME"
echo ""

# Check if running as administrator
echo "🔐 Checking administrator privileges..."
if ! net session >/dev/null 2>&1; then
    echo "❌ This script requires administrator privileges"
    echo "   Please run as administrator"
    exit 1
fi
echo "✅ Administrator privileges confirmed"

# Build the agent
echo "🔨 Building Windows Agent..."
if [ ! -f "$AGENT_DIR/RdpRelay.Agent.Win.csproj" ]; then
    echo "❌ Agent project not found at $AGENT_DIR"
    exit 1
fi

cd "$AGENT_DIR"
dotnet publish -c Release -r win-x64 --self-contained true -o "bin/publish"
cd ../..

# Stop existing service if running
echo "🛑 Stopping existing service if running..."
sc stop "$SERVICE_NAME" 2>/dev/null || echo "   Service not running or doesn't exist"
sc delete "$SERVICE_NAME" 2>/dev/null || echo "   Service doesn't exist"

# Create install directory
echo "📁 Creating install directory..."
mkdir -p "$INSTALL_DIR" 2>/dev/null || true

# Copy files
echo "📦 Copying agent files..."
cp -r "$AGENT_DIR/bin/publish/"* "$INSTALL_DIR/"

# Create configuration
echo "⚙️  Creating configuration..."
cat > "$INSTALL_DIR/appsettings.json" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Agent": {
    "RelayUrl": "https://relay.your-domain.com",
    "ProvisioningToken": "",
    "HeartbeatIntervalSeconds": 30,
    "MaxRdpConnections": 5,
    "LogLevel": "Information"
  }
}
EOF

echo "📝 Please edit $INSTALL_DIR/appsettings.json with your configuration:"
echo "   - RelayUrl: URL of your relay server"
echo "   - ProvisioningToken: Token from your portal admin"
echo ""
read -p "Press Enter when configuration is complete..."

# Install as Windows service
echo "🔧 Installing Windows service..."
sc create "$SERVICE_NAME" \
    binPath="\"$INSTALL_DIR/RdpRelay.Agent.Win.exe\"" \
    DisplayName="$SERVICE_DISPLAY_NAME" \
    start=auto

# Configure service to restart on failure
sc failure "$SERVICE_NAME" reset=86400 actions=restart/10000/restart/10000/restart/10000

# Start service
echo "▶️  Starting service..."
sc start "$SERVICE_NAME"

# Check service status
echo "🔍 Checking service status..."
sc query "$SERVICE_NAME"

echo ""
echo "✅ Windows Agent deployment completed!"
echo ""
echo "📋 Service Information:"
echo "   Name: $SERVICE_NAME"
echo "   Display Name: $SERVICE_DISPLAY_NAME"
echo "   Install Directory: $INSTALL_DIR"
echo ""
echo "🛠️  Management Commands:"
echo "   Start:   sc start $SERVICE_NAME"
echo "   Stop:    sc stop $SERVICE_NAME" 
echo "   Status:  sc query $SERVICE_NAME"
echo "   Logs:    Check $INSTALL_DIR/logs/"
echo ""
echo "⚠️  Next Steps:"
echo "   1. Verify the agent appears online in the portal"
echo "   2. Test RDP connections through the relay"
echo "   3. Configure firewall rules if needed"
echo "   4. Set up monitoring and alerting"
