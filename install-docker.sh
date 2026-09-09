#!/bin/bash

################################################################################
# Fraud POC - Simple Docker Installation Script
#
# Purpose: Build and run the C# fraud detection solution in Docker
# for local development
#
# Usage:
#   ./install-docker.sh [build|run|stop|clean|help]
#
# Examples:
#   ./install-docker.sh build    # Build the Docker image
#   ./install-docker.sh run      # Run the container
#   ./install-docker.sh help     # Show help
################################################################################

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
IMAGE_NAME="fraud-poc-project"
IMAGE_TAG="offline"
CONTAINER_NAME="fraud-poc-project"
APP_PORT="8080"
HOST_PORT="8080"
DB_PORT="5432"

# Functions
print_header() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}================================${NC}"
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
Fraud POC - Docker Installation Helper

COMMANDS:
  build       Build the Docker image
  run         Run the container (requires database running)
  stop        Stop the running container
  clean       Remove container and image
  ps          Show running container status
  logs        Show container logs
  shell       Open shell inside running container
  help        Show this help message

ENVIRONMENT VARIABLES:
  DB_HOST         Database host (default: host.docker.internal for local dev)
  DB_PORT         Database port (default: 5432)
  DB_NAME         Database name (default: fraud_db)
  DB_USERNAME     Database username (default: therrons)
  DB_PASSWORD     Database password (default: therrons)
  APP_PORT        Application port inside container (default: 8080)
  HOST_PORT       Port to expose on host (default: 8080)

EXAMPLES:
  # Build the image
  ./install-docker.sh build

  # Run with custom database
  export DB_HOST=192.168.1.100
  ./install-docker.sh run

  # View logs
  ./install-docker.sh logs

  # Open shell in running container
  ./install-docker.sh shell

  # Stop and clean up
  ./install-docker.sh stop
  ./install-docker.sh clean

REQUIREMENTS:
  - Docker installed and running
  - PostgreSQL database running (can be local or remote)
  - .NET 8 SDK (for building source, not needed for Docker)
EOF
}

check_prerequisites() {
    print_header "Checking Prerequisites"

    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        echo "Please install Docker from https://www.docker.com/products/docker-desktop"
        exit 1
    fi

    if ! docker ps &> /dev/null; then
        print_error "Docker is not running"
        echo "Please start Docker Desktop or Docker daemon"
        exit 1
    fi

    print_success "Docker is installed and running"
}

build_image() {
    print_header "Building Docker Image"

    if [ ! -f "Dockerfile" ]; then
        print_error "Dockerfile not found in current directory"
        echo "Please run this script from the root of the fraud_poc project"
        exit 1
    fi

    print_info "Building image: ${IMAGE_NAME}:${IMAGE_TAG}"
    docker build \
        -t "${IMAGE_NAME}:${IMAGE_TAG}" \
        --build-arg APP_DLL="fraud_poc_project.dll" \
        --build-arg APP_PORT="${APP_PORT}" \
        .

    print_success "Image built successfully"
    docker images "${IMAGE_NAME}:${IMAGE_TAG}"
}

run_container() {
    print_header "Starting Container"

    # Check if container already running
    if docker ps --filter "name=${CONTAINER_NAME}" --format '{{.Names}}' | grep -q "${CONTAINER_NAME}"; then
        print_warning "Container ${CONTAINER_NAME} is already running"
        echo "Stop it with: ./install-docker.sh stop"
        return
    fi

    # Set default values if not provided
    DB_HOST="${DB_HOST:-host.docker.internal}"
    DB_PORT="${DB_PORT:-5432}"
    DB_NAME="${DB_NAME:-fraud_db}"
    DB_USERNAME="${DB_USERNAME:-therrons}"
    DB_PASSWORD="${DB_PASSWORD:-therrons}"

    print_info "Configuration:"
    print_info "  Container Name: ${CONTAINER_NAME}"
    print_info "  Image: ${IMAGE_NAME}:${IMAGE_TAG}"
    print_info "  Port Mapping: ${HOST_PORT}:${APP_PORT}"
    print_info "  DB Host: ${DB_HOST}"
    print_info "  DB Port: ${DB_PORT}"
    print_info "  DB Name: ${DB_NAME}"
    print_info "  DB Username: ${DB_USERNAME}"

    docker run -d \
        --name "${CONTAINER_NAME}" \
        -p "${HOST_PORT}:${APP_PORT}" \
        -e "ASPNETCORE_URLS=http://0.0.0.0:${APP_PORT}" \
        -e "ASPNETCORE_ENVIRONMENT=RELEASE" \
        -e "DB_HOST=${DB_HOST}" \
        -e "DB_PORT=${DB_PORT}" \
        -e "DB_NAME=${DB_NAME}" \
        -e "DB_USERNAME=${DB_USERNAME}" \
        -e "DB_PASSWORD=${DB_PASSWORD}" \
        --restart unless-stopped \
        "${IMAGE_NAME}:${IMAGE_TAG}"

    print_success "Container started: ${CONTAINER_NAME}"
    echo ""
    echo "Container is starting up. Check logs with:"
    echo "  ./install-docker.sh logs"
    echo ""
    echo "API will be available at:"
    echo "  http://localhost:${HOST_PORT}/swagger"
    echo ""
}

stop_container() {
    print_header "Stopping Container"

    if docker ps --filter "name=${CONTAINER_NAME}" --format '{{.Names}}' | grep -q "${CONTAINER_NAME}"; then
        print_info "Stopping container: ${CONTAINER_NAME}"
        docker stop "${CONTAINER_NAME}"
        print_success "Container stopped"
    else
        print_warning "Container ${CONTAINER_NAME} is not running"
    fi
}

remove_container() {
    print_header "Removing Container"

    stop_container

    if docker ps -a --filter "name=${CONTAINER_NAME}" --format '{{.Names}}' | grep -q "${CONTAINER_NAME}"; then
        print_info "Removing container: ${CONTAINER_NAME}"
        docker rm "${CONTAINER_NAME}"
        print_success "Container removed"
    fi
}

clean_all() {
    print_header "Cleaning Up"

    remove_container

    if docker images "${IMAGE_NAME}:${IMAGE_TAG}" --format '{{.Repository}}:{{.Tag}}' | grep -q "${IMAGE_NAME}:${IMAGE_TAG}"; then
        print_info "Removing image: ${IMAGE_NAME}:${IMAGE_TAG}"
        docker rmi "${IMAGE_NAME}:${IMAGE_TAG}"
        print_success "Image removed"
    fi

    print_success "Cleanup completed"
}

show_status() {
    print_header "Container Status"

    if docker ps --filter "name=${CONTAINER_NAME}" --format '{{.Names}}' | grep -q "${CONTAINER_NAME}"; then
        print_success "Container is RUNNING"
        echo ""
        docker ps --filter "name=${CONTAINER_NAME}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    else
        print_warning "Container is NOT running"

        if docker ps -a --filter "name=${CONTAINER_NAME}" --format '{{.Names}}' | grep -q "${CONTAINER_NAME}"; then
            echo "Container exists but is stopped. Start with:"
            echo "  ./install-docker.sh run"
        else
            echo "Container does not exist. Build and run with:"
            echo "  ./install-docker.sh build"
            echo "  ./install-docker.sh run"
        fi
    fi
}

show_logs() {
    print_header "Container Logs"

    if docker ps -a --filter "name=${CONTAINER_NAME}" --format '{{.Names}}' | grep -q "${CONTAINER_NAME}"; then
        docker logs -f "${CONTAINER_NAME}"
    else
        print_error "Container ${CONTAINER_NAME} not found"
    fi
}

shell_access() {
    print_header "Opening Shell"

    if docker ps --filter "name=${CONTAINER_NAME}" --format '{{.Names}}' | grep -q "${CONTAINER_NAME}"; then
        print_info "Opening interactive shell in container"
        docker exec -it "${CONTAINER_NAME}" /bin/sh
    else
        print_error "Container ${CONTAINER_NAME} is not running"
        exit 1
    fi
}

# Main
COMMAND="${1:-help}"

case "${COMMAND}" in
    build)
        check_prerequisites
        build_image
        ;;
    run)
        check_prerequisites
        if [ ! -f "Dockerfile" ]; then
            print_error "Image not found. Please run build first:"
            echo "  ./install-docker.sh build"
            exit 1
        fi
        run_container
        ;;
    stop)
        stop_container
        ;;
    clean)
        clean_all
        ;;
    ps|status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    shell)
        shell_access
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        print_error "Unknown command: ${COMMAND}"
        echo "Run './install-docker.sh help' for usage"
        exit 1
        ;;
esac
