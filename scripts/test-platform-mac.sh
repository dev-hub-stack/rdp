#!/bin/bash
# RDP Relay Platform Testing Script for macOS
# This script helps you test the entire platform end-to-end

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker Desktop for Mac."
        print_status "Download from: https://www.docker.com/products/docker-desktop/"
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed. Please install Docker Desktop for Mac."
        exit 1
    fi
    
    # Check if Docker is running
    if ! docker info >/dev/null 2>&1; then
        print_warning "Docker daemon is not accessible. Attempting to start Docker Desktop..."
        open /Applications/Docker.app
        
        # Wait for Docker to start
        for i in {1..30}; do
            print_status "Waiting for Docker Desktop to start... ($i/30)"
            sleep 2
            if docker info >/dev/null 2>&1; then
                print_success "Docker Desktop started successfully!"
                break
            fi
            if [ $i -eq 30 ]; then
                print_error "Docker Desktop failed to start within 60 seconds."
                print_error "Please start Docker Desktop manually and try again."
                exit 1
            fi
        done
    fi
    
    print_success "All prerequisites are met!"
    print_status "Docker version: $(docker --version)"
    print_status "Docker Compose version: $(docker-compose --version)"
}

# Start the platform
start_platform() {
    print_status "Starting RDP Relay Platform..."
    
    # Make deploy script executable
    chmod +x ./deploy.sh
    
    # Start the platform
    ./deploy.sh
    
    print_status "Waiting for services to be ready..."
    sleep 30
    
    # Check service health
    check_services_health
}

# Check services health
check_services_health() {
    print_status "Checking service health..."
    
    # Check containers are running
    if docker-compose ps | grep -q "Exit"; then
        print_error "Some containers have exited. Check logs with: docker-compose logs"
        return 1
    fi
    
    # Check Portal API
    if curl -k -s https://localhost:5000/health > /dev/null 2>&1; then
        print_success "Portal API is healthy"
    else
        print_warning "Portal API may not be ready yet"
    fi
    
    # Check Relay Server
    if curl -k -s https://localhost:5001/health > /dev/null 2>&1; then
        print_success "Relay Server is healthy"
    else
        print_warning "Relay Server may not be ready yet"
    fi
    
    # Check Web Portal
    if curl -k -s https://localhost/ > /dev/null 2>&1; then
        print_success "Web Portal is accessible"
    else
        print_warning "Web Portal may not be ready yet"
    fi
}

# Test web portal
test_web_portal() {
    print_status "Testing Web Portal..."
    
    print_status "Opening web portal in browser..."
    open https://localhost
    
    cat << EOF

${GREEN}=== WEB PORTAL TESTING ===${NC}

1. Your browser should open to: https://localhost
2. Accept the SSL certificate warning (it's self-signed)
3. Login with default credentials:
   - Email: admin@example.com
   - Password: SecurePassword123!

4. Test the following features:
   - ✅ Dashboard shows system stats
   - ✅ Navigate to Agents page
   - ✅ Generate a provisioning token
   - ✅ Navigate to Sessions page
   - ✅ Navigate to Users page

Press ENTER when you've completed the web portal testing...
EOF
    read
}

# Test API endpoints
test_api_endpoints() {
    print_status "Testing API endpoints..."
    
    # Login and get token
    print_status "Testing authentication..."
    
    TOKEN_RESPONSE=$(curl -k -s -X POST https://localhost/api/auth/login \
        -H "Content-Type: application/json" \
        -d '{
            "email": "admin@example.com",
            "password": "SecurePassword123!"
        }')
    
    if echo "$TOKEN_RESPONSE" | grep -q "accessToken"; then
        print_success "Authentication successful"
        ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)
        
        # Test other endpoints
        print_status "Testing agents endpoint..."
        curl -k -s -H "Authorization: Bearer $ACCESS_TOKEN" https://localhost/api/agents > /dev/null
        print_success "Agents endpoint working"
        
        print_status "Testing sessions endpoint..."
        curl -k -s -H "Authorization: Bearer $ACCESS_TOKEN" https://localhost/api/sessions > /dev/null
        print_success "Sessions endpoint working"
        
    else
        print_error "Authentication failed. Response: $TOKEN_RESPONSE"
    fi
}

# Generate Windows agent installer
generate_windows_agent() {
    print_status "Preparing Windows Agent for deployment..."
    
    # Build the Windows agent
    print_status "Building Windows Agent..."
    dotnet publish agent-win/RdpRelay.Agent.Win/RdpRelay.Agent.Win.csproj \
        -c Release \
        -r win-x64 \
        --self-contained true \
        -o ./build/agent-win
    
    if [ $? -eq 0 ]; then
        print_success "Windows Agent built successfully"
        
        # Create a simple installer script
        cat > ./build/agent-win/install-agent.ps1 << 'EOF'
# RDP Relay Agent Installation Script for Windows
# Run as Administrator

param(
    [Parameter(Mandatory=$true)]
    [string]$ProvisioningToken,
    
    [string]$RelayServer = "YOUR_RELAY_SERVER_URL"
)

Write-Host "Installing RDP Relay Agent..." -ForegroundColor Green

# Create service directory
$ServiceDir = "C:\Program Files\RdpRelayAgent"
if (-not (Test-Path $ServiceDir)) {
    New-Item -ItemType Directory -Path $ServiceDir -Force
}

# Copy files
Copy-Item ".\*" -Destination $ServiceDir -Recurse -Force

# Update configuration
$ConfigPath = "$ServiceDir\appsettings.json"
$Config = Get-Content $ConfigPath | ConvertFrom-Json
$Config.RelayOptions.RelayUrl = $RelayServer
$Config.RelayOptions.ProvisioningToken = $ProvisioningToken
$Config | ConvertTo-Json -Depth 10 | Set-Content $ConfigPath

# Install as Windows Service
$ServiceName = "RdpRelayAgent"
$ServiceDisplayName = "RDP Relay Agent"
$ServiceDescription = "Secure RDP relay agent for remote connections"
$ServiceExe = "$ServiceDir\RdpRelay.Agent.Win.exe"

# Remove existing service if it exists
if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service $ServiceName -Force
    sc.exe delete $ServiceName
}

# Create new service
sc.exe create $ServiceName binPath= $ServiceExe DisplayName= $ServiceDisplayName start= auto
sc.exe description $ServiceName $ServiceDescription

# Start service
Start-Service $ServiceName

Write-Host "RDP Relay Agent installed and started successfully!" -ForegroundColor Green
Write-Host "Service Status:" -ForegroundColor Yellow
Get-Service $ServiceName
EOF
        
        print_success "Windows Agent installer created at: ./build/agent-win/"
        print_status "To deploy on Windows machine:"
        echo "  1. Copy the entire ./build/agent-win/ folder to Windows machine"
        echo "  2. Get provisioning token from web portal"
        echo "  3. Run PowerShell as Administrator:"
        echo "  4. Set-ExecutionPolicy -ExecutionPolicy RemoteSigned"
        echo "  5. .\\install-agent.ps1 -ProvisioningToken 'YOUR_TOKEN' -RelayServer 'https://your-relay-server.com'"
    else
        print_error "Failed to build Windows Agent"
    fi
}

# Test RDP connection (requires Windows VM or machine)
test_rdp_connection() {
    print_status "RDP Connection Testing..."
    
    cat << EOF

${GREEN}=== RDP CONNECTION TESTING ===${NC}

For testing RDP connections, you'll need:

1. ${BLUE}Windows VM or Machine${NC} with:
   - RDP enabled (System Properties > Remote > Enable Remote Desktop)
   - User account for testing
   - Network access to this Mac

2. ${BLUE}RDP Client on Mac${NC}:
   - Microsoft Remote Desktop (free from Mac App Store)
   - Or use built-in Screen Sharing with RDP support

${YELLOW}Testing Steps:${NC}
1. Deploy Windows Agent on target machine (see instructions above)
2. Verify agent appears as "Online" in web portal
3. Create new session in web portal:
   - Select the agent
   - Enter Windows username
   - Get connection code
4. Connect using RDP client:
   - Host: localhost or your-relay-server.com
   - Port: 443 (or as configured)
   - Use connection code from portal

EOF
}

# Install Microsoft Remote Desktop
install_rdp_client() {
    print_status "Opening Mac App Store to install Microsoft Remote Desktop..."
    open "https://apps.apple.com/us/app/microsoft-remote-desktop/id1295203466?mt=12"
    
    print_status "Alternative RDP clients for Mac:"
    echo "  - Royal TSX (paid, very feature-rich)"
    echo "  - Parallels Client (free)"
    echo "  - Jump Desktop (paid)"
}

# Show logs
show_logs() {
    print_status "Showing platform logs..."
    echo "Choose which logs to view:"
    echo "1) All logs"
    echo "2) Portal API logs"
    echo "3) Relay Server logs"
    echo "4) nginx logs"
    echo "5) MongoDB logs"
    read -p "Enter choice (1-5): " choice
    
    case $choice in
        1) docker-compose logs -f ;;
        2) docker-compose logs -f portal-api ;;
        3) docker-compose logs -f relay-server ;;
        4) docker-compose logs -f nginx ;;
        5) docker-compose logs -f mongodb ;;
        *) echo "Invalid choice" ;;
    esac
}

# Stop platform
stop_platform() {
    print_status "Stopping RDP Relay Platform..."
    docker-compose down
    print_success "Platform stopped"
}

# Main menu
show_menu() {
    cat << EOF

${GREEN}=== RDP RELAY PLATFORM TESTING MENU ===${NC}

1) Check Prerequisites
2) Start Platform
3) Check Service Health  
4) Test Web Portal
5) Test API Endpoints
6) Generate Windows Agent
7) Test RDP Connection Guide
8) Install RDP Client for Mac
9) Show Logs
10) Stop Platform
11) Exit

EOF
}

# Main loop
main() {
    print_status "RDP Relay Platform Testing Tool for macOS"
    print_status "========================================="
    
    while true; do
        show_menu
        read -p "Enter your choice (1-11): " choice
        
        case $choice in
            1) check_prerequisites ;;
            2) start_platform ;;
            3) check_services_health ;;
            4) test_web_portal ;;
            5) test_api_endpoints ;;
            6) generate_windows_agent ;;
            7) test_rdp_connection ;;
            8) install_rdp_client ;;
            9) show_logs ;;
            10) stop_platform ;;
            11) 
                print_status "Goodbye!"
                exit 0
                ;;
            *)
                print_error "Invalid choice. Please try again."
                ;;
        esac
        
        echo
        read -p "Press ENTER to continue..."
    done
}

# Run main function
main
