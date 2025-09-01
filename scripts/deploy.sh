#!/bin/bash

# RDP Relay Platform Deployment Script
# This script sets up the complete RDP Relay platform using Docker Compose

set -e

echo "🚀 Starting RDP Relay Platform Deployment"

# Check prerequisites
echo "📋 Checking prerequisites..."

if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

echo "✅ Prerequisites check passed"

# Create environment file if it doesn't exist
if [ ! -f .env ]; then
    echo "📝 Creating .env file from template..."
    cp .env.example .env
    echo "⚠️  Please edit .env file with your configuration before continuing."
    echo "   At minimum, change all passwords and secret keys!"
    read -p "Press Enter when ready to continue..."
fi

# Create required directories
echo "📁 Creating required directories..."
mkdir -p logs/{portal-api,relay,nginx}
mkdir -p infra/certs
mkdir -p data/{mongodb,redis}

# Generate self-signed certificates for development
if [ ! -f infra/certs/relay.crt ]; then
    echo "🔒 Generating self-signed certificates for development..."
    
    # Create certificate configuration
    cat > infra/certs/openssl.conf << EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
C = US
ST = State
L = City
O = RDP Relay
OU = Development
CN = localhost

[v3_req]
keyUsage = keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = relay.localhost
DNS.3 = *.localhost
IP.1 = 127.0.0.1
EOF

    # Generate certificate
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout infra/certs/relay.key \
        -out infra/certs/relay.crt \
        -config infra/certs/openssl.conf \
        -extensions v3_req

    # Generate PFX file for .NET
    openssl pkcs12 -export -out infra/certs/relay.pfx \
        -inkey infra/certs/relay.key \
        -in infra/certs/relay.crt \
        -password pass:$(grep CERT_PASSWORD .env | cut -d '=' -f2)

    echo "✅ Certificates generated"
else
    echo "✅ Certificates already exist"
fi

# Build and start services
echo "🔨 Building Docker images..."
docker-compose build

echo "🚀 Starting services..."
docker-compose up -d

# Wait for services to start
echo "⏳ Waiting for services to start..."
sleep 30

# Check service health
echo "🏥 Checking service health..."

# Check MongoDB
if docker-compose exec mongodb mongosh --eval "db.adminCommand('ping')" > /dev/null 2>&1; then
    echo "✅ MongoDB is healthy"
else
    echo "❌ MongoDB health check failed"
fi

# Check Redis
if docker-compose exec redis redis-cli ping > /dev/null 2>&1; then
    echo "✅ Redis is healthy"
else
    echo "❌ Redis health check failed"
fi

# Check Portal API
if curl -f http://localhost:5000/api/health > /dev/null 2>&1; then
    echo "✅ Portal API is healthy"
else
    echo "❌ Portal API health check failed"
fi

# Check Portal Web
if curl -f http://localhost:3000 > /dev/null 2>&1; then
    echo "✅ Portal Web is healthy"
else
    echo "❌ Portal Web health check failed"
fi

echo ""
echo "🎉 RDP Relay Platform deployment completed!"
echo ""
echo "📋 Service URLs:"
echo "   Portal Web:  http://localhost:80"
echo "   Portal API:  http://localhost:5000"
echo "   Relay Server: https://localhost:5001 (WebSocket)"
echo "   MongoDB:     localhost:27017"
echo "   Redis:       localhost:6379"
echo ""
echo "👤 Default Admin Credentials:"
echo "   Email:    admin@rdprelay.local"
echo "   Password: admin123"
echo ""
echo "⚠️  IMPORTANT: Change default passwords in production!"
echo ""
echo "📖 Next steps:"
echo "   1. Access the portal at http://localhost"
echo "   2. Log in with admin credentials"
echo "   3. Configure your first tenant"
echo "   4. Deploy Windows agents with provisioning tokens"
echo ""
echo "📚 View logs with: docker-compose logs -f [service-name]"
echo "🛑 Stop services with: docker-compose down"
