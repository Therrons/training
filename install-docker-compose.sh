#!/bin/bash

################################################################################
# Fraud POC - Docker Compose Installation Script
#
# Purpose: Complete local development environment with Docker Compose
# Includes: API, PostgreSQL, Nginx, persistent storage
#
# Usage:
#   ./install-docker-compose.sh [up|down|stop|logs|status|help]
#
# Examples:
#   ./install-docker-compose.sh up    # Start all services
#   ./install-docker-compose.sh down  # Stop and remove
#   ./install-docker-compose.sh logs  # View logs
################################################################################

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Configuration
COMPOSE_FILE="docker-compose.yml"
PROJECT_NAME="fraud-poc"
API_PORT="8085"
DB_PORT="5432"
NGINX_PORT="8085"

# Functions
print_header() {
    echo -e "${BLUE}═══════════════════════════════════════════${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

show_help() {
    cat << EOF
Fraud POC - Docker Compose Installation Helper

${CYAN}COMMANDS:${NC}
  up          Start all services (build if needed)
  down        Stop and remove all services and volumes
  stop        Stop services without removing
  logs        Show live logs from all services
  ps          Show status of all services
  shell-api   Open shell in API container
  shell-db    Open shell in PostgreSQL container
  rebuild     Rebuild images and restart services
  clean       Remove containers, images, and volumes
  help        Show this help message

${CYAN}SERVICES INCLUDED:${NC}
  • PostgreSQL 16 (port 5432)
  • Fraud POC API (port 8080, exposed via Nginx)
  • Nginx Reverse Proxy (port 8085)

${CYAN}ENVIRONMENT:${NC}
  DB_USERNAME:  therrons
  DB_PASSWORD:  therrons
  DB_NAME:      fraud_db

${CYAN}EXAMPLES:${NC}
  # Start the complete environment
  ./install-docker-compose.sh up

  # View live logs
  ./install-docker-compose.sh logs

  # Open shell in API
  ./install-docker-compose.sh shell-api

  # Stop services
  ./install-docker-compose.sh stop

  # Clean everything
  ./install-docker-compose.sh clean

${CYAN}ACCESS POINTS:${NC}
  API (Swagger):     http://localhost:${API_PORT}/swagger
  Nginx Proxy:       http://localhost:${NGINX_PORT}
  PostgreSQL:        localhost:${DB_PORT} (user: therrons)

${CYAN}REQUIREMENTS:${NC}
  - Docker and Docker Compose installed
  - Ports ${API_PORT} and ${DB_PORT} available on host
  - At least 2GB free disk space

${CYAN}TIPS:${NC}
  • Use 'tail -f' style logs: ./install-docker-compose.sh logs
  • Check individual service: docker-compose ps
  • View specific service: docker-compose logs [service-name]
  • Service names: api, postgres, nginx
EOF
}

check_prerequisites() {
    print_header "Checking Prerequisites"

    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        echo "Install from: https://www.docker.com/products/docker-desktop"
        exit 1
    fi
    print_success "Docker installed"

    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed"
        echo "Install from: https://docs.docker.com/compose/install/"
        exit 1
    fi
    print_success "Docker Compose installed"

    # Check Docker running
    if ! docker ps &> /dev/null; then
        print_error "Docker daemon is not running"
        echo "Please start Docker Desktop or Docker daemon"
        exit 1
    fi
    print_success "Docker daemon is running"

    # Check docker-compose.yml exists
    if [ ! -f "${COMPOSE_FILE}" ]; then
        print_error "${COMPOSE_FILE} not found in current directory"
        echo "Please run this script from the root of the fraud_poc project"
        exit 1
    fi
    print_success "docker-compose.yml found"

    # Check .env file
    if [ ! -f ".env" ]; then
        print_warning ".env file not found, creating from .env.example"
        if [ -f ".env.example" ]; then
            cp .env.example .env
            print_success ".env created"
        else
            print_error ".env and .env.example not found"
            exit 1
        fi
    fi

    # Check port availability
    print_info "Checking port availability..."
    if netstat -tuln 2>/dev/null | grep -q ":${API_PORT} " || lsof -i :${API_PORT} 2>/dev/null; then
        print_warning "Port ${API_PORT} is already in use"
        echo "Either free the port or change API_PORT in this script"
    else
        print_success "Port ${API_PORT} is available"
    fi
}

start_services() {
    print_header "Starting Services"

    if [ "$(docker-compose ps -q)" ]; then
        print_warning "Services are already running"
        echo "Use './install-docker-compose.sh logs' to view"
        return
    fi

    print_info "Building images and starting services..."
    print_info "This may take a few minutes on first run...\n"

    docker-compose up -d

    # Wait for services to be ready
    print_info "Waiting for services to become ready..."
    sleep 5

    # Check status
    print_header "Service Status"
    docker-compose ps

    print_success "Services started!"
    echo ""
    echo -e "${CYAN}═══════════════════════════════════════════${NC}"
    echo -e "${CYAN}Access Points:${NC}"
    echo -e "${GREEN}API (Swagger):${NC}       http://localhost:${API_PORT}/swagger"
    echo -e "${GREEN}Nginx Proxy:${NC}         http://localhost:${NGINX_PORT}"
    echo -e "${GREEN}PostgreSQL:${NC}          localhost:${DB_PORT}"
    echo ""
    echo -e "${CYAN}Useful Commands:${NC}"
    echo "  View logs:        ./install-docker-compose.sh logs"
    echo "  Check status:     ./install-docker-compose.sh ps"
    echo "  Shell in API:     ./install-docker-compose.sh shell-api"
    echo "  Stop services:    ./install-docker-compose.sh stop"
    echo -e "${CYAN}═══════════════════════════════════════════${NC}"
}

stop_services() {
    print_header "Stopping Services"

    if [ ! "$(docker-compose ps -q)" ]; then
        print_warning "No services are running"
        return
    fi

    print_info "Stopping services..."
    docker-compose stop

    print_success "Services stopped"
}

down_services() {
    print_header "Removing Services"

    if [ ! "$(docker-compose ps -q)" ]; then
        print_warning "No services are running"
    fi

    print_warning "This will remove containers and volumes"
    read -p "Continue? (yes/no): " -n 3 -r
    echo
    if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        print_info "Removing services and volumes..."
        docker-compose down -v

        print_success "Services and volumes removed"
    else
        print_info "Operation cancelled"
    fi
}

show_logs() {
    print_header "Live Logs (Ctrl+C to exit)"
    docker-compose logs -f
}

show_status() {
    print_header "Service Status"
    docker-compose ps

    echo ""
    if [ "$(docker-compose ps -q)" ]; then
        print_success "Services are RUNNING"
        echo ""
        echo "Access points:"
        echo "  API:    http://localhost:${API_PORT}/swagger"
        echo "  Nginx:  http://localhost:${NGINX_PORT}"
    else
        print_warning "Services are NOT running"
        echo "Start with: ./install-docker-compose.sh up"
    fi
}

shell_api() {
    print_header "API Container Shell"

    if ! docker-compose ps | grep -q "fraud_poc_project"; then
        print_error "API container is not running"
        echo "Start services with: ./install-docker-compose.sh up"
        exit 1
    fi

    print_info "Opening shell in API container..."
    docker-compose exec api /bin/sh
}

shell_db() {
    print_header "PostgreSQL Container Shell"

    if ! docker-compose ps | grep -q "fraud_poc_project_postgres"; then
        print_error "PostgreSQL container is not running"
        echo "Start services with: ./install-docker-compose.sh up"
        exit 1
    fi

    print_info "Opening shell in PostgreSQL container..."
    docker-compose exec postgres /bin/sh
}

rebuild_services() {
    print_header "Rebuilding Services"

    print_info "Stopping services..."
    docker-compose down

    print_info "Removing images..."
    docker-compose rm -f

    print_info "Building and starting services..."
    docker-compose up -d

    sleep 5
    print_header "Service Status"
    docker-compose ps

    print_success "Services rebuilt and started"
}

clean_all() {
    print_header "Complete Cleanup"

    print_warning "This will remove all containers, images, and volumes"
    read -p "Continue? (yes/no): " -n 3 -r
    echo
    if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        print_info "Stopping services..."
        docker-compose down -v 2>/dev/null || true

        print_info "Removing images..."
        docker rmi fraud_poc_project:offline 2>/dev/null || true
        docker rmi postgres:16-alpine 2>/dev/null || true
        docker rmi nginx:alpine 2>/dev/null || true

        print_success "Cleanup completed"
    else
        print_info "Operation cancelled"
    fi
}

# Main
COMMAND="${1:-help}"

case "${COMMAND}" in
    up)
        check_prerequisites
        start_services
        ;;
    down)
        down_services
        ;;
    stop)
        stop_services
        ;;
    ps|status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    shell-api)
        shell_api
        ;;
    shell-db)
        shell_db
        ;;
    rebuild)
        check_prerequisites
        rebuild_services
        ;;
    clean)
        clean_all
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        print_error "Unknown command: ${COMMAND}"
        echo "Run './install-docker-compose.sh help' for usage"
        exit 1
        ;;
esac
