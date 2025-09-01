#!/bin/bash
# Generate SSL certificate that works with IP address

SERVER_IP="159.89.112.134"
CERT_DIR="/Users/clustox_1/Documents/Network/rdp-relay/infra/certs"
PASSWORD="rdp_relay_cert_production_1756368355"

cd "$CERT_DIR"

# Create OpenSSL config that includes IP address
cat > ip-cert.conf << EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
C=US
ST=CA
L=San Francisco
O=RDP Relay
CN=$SERVER_IP

[v3_req]
keyUsage = keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
IP.1 = $SERVER_IP
DNS.1 = localhost
EOF

# Generate private key
openssl genrsa -out relay-ip.key 2048

# Generate certificate with IP address support
openssl req -new -x509 -key relay-ip.key -out relay-ip.crt -days 365 -config ip-cert.conf -extensions v3_req

# Create PFX file
openssl pkcs12 -export -out relay-ip.pfx -inkey relay-ip.key -in relay-ip.crt -password pass:$PASSWORD

echo "✅ SSL Certificate generated for IP address: $SERVER_IP"
echo "📁 Files created:"
echo "   - relay-ip.key (private key)"
echo "   - relay-ip.crt (certificate)"  
echo "   - relay-ip.pfx (PKCS12 bundle)"
echo ""
echo "🚀 Next: Upload to server and restart relay"
