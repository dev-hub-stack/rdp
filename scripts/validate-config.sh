#!/bin/bash

# Configuration Validation Script
# Validates the RDP Relay configuration before deployment

set -e

echo "⚙️  RDP Relay Configuration Validation"
echo "====================================="
echo ""

ERRORS=0
WARNINGS=0

# Helper functions
check_file() {
    if [ ! -f "$1" ]; then
        echo "❌ ERROR: Required file missing: $1"
        ((ERRORS++))
        return 1
    else
        echo "✅ Found: $1"
        return 0
    fi
}

check_directory() {
    if [ ! -d "$1" ]; then
        echo "❌ ERROR: Required directory missing: $1"
        ((ERRORS++))
        return 1
    else
        echo "✅ Found: $1"
        return 0
    fi
}

warn() {
    echo "⚠️  WARNING: $1"
    ((WARNINGS++))
}

info() {
    echo "ℹ️  INFO: $1"
}

# Check required files
echo "📁 Checking required files..."
check_file "docker-compose.yml"
check_file "README.md"
check_file ".gitignore"

# Check directories
echo ""
echo "📂 Checking project structure..."
check_directory "relay"
check_directory "portal-api"
check_directory "portal-web"
check_directory "agent"
check_directory "scripts"
check_directory "docs"

# Check Docker Compose file
echo ""
echo "🐳 Validating Docker Compose configuration..."
if [ -f "docker-compose.yml" ]; then
    if docker-compose config >/dev/null 2>&1; then
        echo "✅ Docker Compose configuration is valid"
    else
        echo "❌ ERROR: Docker Compose configuration is invalid"
        ((ERRORS++))
    fi
    
    # Check for required services
    required_services=("relay" "portal-api" "portal-web" "mongodb" "redis")
    for service in "${required_services[@]}"; do
        if grep -q "^  $service:" docker-compose.yml; then
            echo "✅ Service defined: $service"
        else
            echo "❌ ERROR: Missing service: $service"
            ((ERRORS++))
        fi
    done
else
    echo "❌ ERROR: Cannot validate Docker Compose - file missing"
fi

# Check environment configuration
echo ""
echo "🌍 Checking environment configuration..."
if [ -f ".env" ]; then
    echo "✅ Found .env file"
    
    # Check for sensitive data in .env
    if grep -q "password\|secret\|key" .env; then
        warn ".env file contains sensitive data - ensure it's in .gitignore"
    fi
else
    warn ".env file not found - may need environment variables"
fi

# Check for environment template
if [ -f ".env.example" ]; then
    echo "✅ Found .env.example template"
else
    warn ".env.example template not found - consider creating one"
fi

# Check .gitignore
echo ""
echo "🚫 Validating .gitignore..."
if [ -f ".gitignore" ]; then
    required_ignores=("node_modules/" "*.log" ".env" "dist/" "build/")
    for ignore in "${required_ignores[@]}"; do
        if grep -q "$ignore" .gitignore; then
            echo "✅ Ignoring: $ignore"
        else
            warn ".gitignore should include: $ignore"
        fi
    done
else
    echo "❌ ERROR: .gitignore file missing"
    ((ERRORS++))
fi

# Check documentation
echo ""
echo "📚 Checking documentation..."
required_docs=("README.md" "docs/SESSION_TROUBLESHOOTING.md")
for doc in "${required_docs[@]}"; do
    check_file "$doc"
done

# Check scripts
echo ""
echo "📜 Checking scripts..."
if [ -d "scripts" ]; then
    script_count=$(find scripts -name "*.sh" | wc -l)
    echo "✅ Found $script_count shell scripts"
    
    # Check if scripts are executable
    for script in scripts/*.sh; do
        if [ -f "$script" ]; then
            if [ -x "$script" ]; then
                echo "✅ Executable: $script"
            else
                warn "Script not executable: $script (run: chmod +x $script)"
            fi
        fi
    done
else
    warn "Scripts directory not found"
fi

# Check for common issues
echo ""
echo "🔍 Checking for common issues..."

# Check if node_modules is tracked
if git ls-files | grep -q "node_modules/"; then
    echo "❌ ERROR: node_modules/ files are tracked in git"
    echo "   Run: git rm -r --cached node_modules"
    ((ERRORS++))
else
    echo "✅ node_modules/ not tracked in git"
fi

# Check for large files
large_files=$(find . -type f -size +10M 2>/dev/null | grep -v ".git" | head -5)
if [ ! -z "$large_files" ]; then
    warn "Large files found (>10MB):"
    echo "$large_files"
    echo "   Consider adding to .gitignore if they shouldn't be tracked"
fi

# Check for log files
log_files=$(find . -name "*.log" 2>/dev/null | head -5)
if [ ! -z "$log_files" ]; then
    warn "Log files found in repository:"
    echo "$log_files"
    echo "   Consider adding *.log to .gitignore"
fi

# Port availability check
echo ""
echo "🌐 Checking port availability..."
required_ports=(3000 5001 8080 27017 6379)
for port in "${required_ports[@]}"; do
    if netstat -tulpn 2>/dev/null | grep -q ":$port "; then
        warn "Port $port is already in use"
    else
        echo "✅ Port $port is available"
    fi
done

# Docker availability
echo ""
echo "🐳 Checking Docker availability..."
if command -v docker >/dev/null 2>&1; then
    echo "✅ Docker is installed"
    if docker info >/dev/null 2>&1; then
        echo "✅ Docker daemon is running"
    else
        echo "❌ ERROR: Docker daemon is not running"
        ((ERRORS++))
    fi
else
    echo "❌ ERROR: Docker is not installed"
    ((ERRORS++))
fi

if command -v docker-compose >/dev/null 2>&1; then
    echo "✅ Docker Compose is installed"
else
    echo "❌ ERROR: Docker Compose is not installed"
    ((ERRORS++))
fi

# Summary
echo ""
echo "📊 Validation Summary"
echo "===================="
echo "Errors: $ERRORS"
echo "Warnings: $WARNINGS"
echo ""

if [ $ERRORS -eq 0 ]; then
    echo "✅ Configuration validation passed!"
    if [ $WARNINGS -gt 0 ]; then
        echo "⚠️  However, there are $WARNINGS warnings to consider."
    fi
    echo ""
    echo "🚀 Ready for deployment!"
else
    echo "❌ Configuration validation failed with $ERRORS errors."
    echo ""
    echo "🔧 Please fix the errors above before proceeding."
    exit 1
fi
