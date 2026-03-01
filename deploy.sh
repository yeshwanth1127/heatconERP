#!/bin/bash

# HeatconERP Docker Deployment Quick Start
# This script automates the setup process

set -e  # Exit on error

echo "================================"
echo "HeatconERP Docker Deployment"
echo "================================"
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check prerequisites
echo -n "Checking Docker... "
if ! command -v docker &> /dev/null; then
    echo -e "${RED}NOT FOUND${NC}"
    echo "Please install Docker from https://www.docker.com"
    exit 1
fi
echo -e "${GREEN}OK${NC}"

echo -n "Checking Docker Compose... "
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}NOT FOUND${NC}"
    echo "Please install Docker Compose from https://docs.docker.com/compose/install"
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# Check if .env exists
echo ""
if [ ! -f .env ]; then
    echo -e "${YELLOW}⚠️  .env file not found${NC}"
    echo "Creating .env from template..."
    cp .env.example .env 2>/dev/null || {
        cat > .env << 'EOF'
DB_USER=postgres
DB_PASSWORD=SecurePasswordHere123!@#
DB_NAME=heatconerp
DB_PORT=5432
API_PORT=5212
WEB_PORT=5118
EOF
    }
    echo -e "${GREEN}✓${NC} .env created. Please edit it with your production values:"
    echo "  nano .env"
    read -p "Press Enter after editing .env... "
fi

# Build images
echo ""
echo -e "${YELLOW}Building Docker images...${NC}"
docker-compose build

# Start services
echo ""
echo -e "${YELLOW}Starting services...${NC}"
docker-compose up -d

# Wait for database to be ready
echo ""
echo -e "${YELLOW}Waiting for PostgreSQL to be ready...${NC}"
for i in {1..30}; do
    if docker-compose exec -T postgres pg_isready -U postgres &> /dev/null; then
        echo -e "${GREEN}✓${NC} PostgreSQL is ready"
        break
    fi
    echo -n "."
    sleep 2
done

# Apply migrations
echo ""
echo -e "${YELLOW}Applying database migrations...${NC}"
docker-compose exec -T api dotnet ef database update \
    --project /app/HeatconERP.Infrastructure.dll \
    --no-build 2>/dev/null || {
    echo -e "${YELLOW}Note: Manual migration may be needed${NC}"
}

# Display status
echo ""
echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}================================${NC}"
echo ""

docker-compose ps

echo ""
echo "Access your application:"
echo -e "  ${YELLOW}Web Application:${NC} http://localhost:5118"
echo -e "  ${YELLOW}API Swagger:${NC} http://localhost:5212/swagger"
echo ""
echo "Default credentials:"
echo -e "  ${YELLOW}Username:${NC} admin"
echo -e "  ${YELLOW}Password:${NC} admin123"
echo ""
echo "View logs:"
echo "  docker-compose logs -f"
echo ""
