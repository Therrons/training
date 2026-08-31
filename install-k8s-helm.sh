#!/bin/bash

################################################################################
# Fraud POC - Kubernetes Helm Installation Script
#
# Purpose: Deploy to Kubernetes cluster using Helm charts
# Supports: Multiple environments (dev, staging, prod)
#
# Prerequisites:
#   - kubectl configured and connected to K8s cluster
#   - Helm 3.x installed
#   - Helm chart at ./charts/fraud-poc-api/
#
# Usage:
#   ./install-k8s-helm.sh [command] [environment]
#
# Examples:
#   ./install-k8s-helm.sh deploy dev
#   ./install-k8s-helm.sh status staging
#   ./install-k8s-helm.sh uninstall prod
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
CHART_NAME="fraud-poc-api"
CHART_PATH="./charts/fraud-poc-api"
NAMESPACE="fraud-poc-api"
RELEASE_NAME="${CHART_NAME}"

# Environment-specific configurations
declare -A ENVIRONMENTS=([dev]="development" [staging]="staging" [prod]="production")
declare -A NAMESPACES=([dev]="fraud-poc-api-dev" [staging]="fraud-poc-api-staging" [prod]="fraud-poc-api-prod")
declare -A REPLICAS=([dev]="1" [staging]="2" [prod]="3")
declare -A AWS_ACCOUNTS=([dev]="YOUR_DEV_ACCOUNT_ID" [staging]="YOUR_STAGING_ACCOUNT_ID" [prod]="YOUR_PROD_ACCOUNT_ID")
declare -A AWS_REGIONS=([dev]="us-east-1" [staging]="us-east-1" [prod]="us-east-1")

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
Fraud POC - Kubernetes Helm Installation Helper

${CYAN}COMMANDS:${NC}
  deploy      Deploy/upgrade application to K8s
  status      Show deployment status
  logs        Show pod logs
  shell       Open shell in pod
  uninstall   Remove application from K8s
  validate    Validate Helm chart
  dry-run     Show what will be deployed (dry-run)
  help        Show this help message

${CYAN}ENVIRONMENTS:${NC}
  dev         Development environment
  staging     Staging environment
  prod        Production environment

${CYAN}EXAMPLES:${NC}
  # Deploy to development
  ./install-k8s-helm.sh deploy dev

  # Check status in staging
  ./install-k8s-helm.sh status staging

  # View logs in production
  ./install-k8s-helm.sh logs prod

  # Dry-run before deploying to prod
  ./install-k8s-helm.sh dry-run prod

  # Remove from staging
  ./install-k8s-helm.sh uninstall staging

${CYAN}CONFIGURATION:${NC}
  Each environment has its own values file:
    - charts/fraud-poc-api/values-dev.yaml
    - charts/fraud-poc-api/values-staging.yaml
    - charts/fraud-poc-api/values-prod.yaml

  Update AWS_ACCOUNTS and AWS_REGIONS above with your values.

${CYAN}REQUIREMENTS:${NC}
  - kubectl configured and connected to cluster
  - Helm 3.x installed
  - Current context set to correct cluster
  - RBAC permissions to create namespaces and deployments
  - AWS IAM roles pre-configured in cluster

${CYAN}TROUBLESHOOTING:${NC}
  View deployment events:
    kubectl describe deployment fraud-poc-api -n fraud-poc-api-dev

  View pod details:
    kubectl get pods -n fraud-poc-api-dev -o wide

  Stream logs:
    kubectl logs -f deployment/fraud-poc-api -n fraud-poc-api-dev
EOF
}

check_prerequisites() {
    print_header "Checking Prerequisites"

    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl not installed"
        echo "Install from: https://kubernetes.io/docs/tasks/tools/"
        exit 1
    fi
    print_success "kubectl installed: $(kubectl version --client --short)"

    # Check Helm
    if ! command -v helm &> /dev/null; then
        print_error "Helm not installed"
        echo "Install from: https://helm.sh/docs/intro/install/"
        exit 1
    fi
    print_success "Helm installed: $(helm version --short)"

    # Check kubectl context
    if ! kubectl cluster-info &> /dev/null; then
        print_error "kubectl context not configured or cluster unavailable"
        echo "Run: kubectl config use-context <context-name>"
        exit 1
    fi
    print_success "kubectl connected to cluster: $(kubectl cluster-info | head -1)"

    # Check Helm chart
    if [ ! -d "${CHART_PATH}" ]; then
        print_error "Helm chart not found at ${CHART_PATH}"
        exit 1
    fi
    print_success "Helm chart found"
}

validate_environment() {
    local env=$1

    if [ -z "$env" ]; then
        print_error "Environment not specified"
        echo "Usage: $0 <command> <environment>"
        echo "Environments: dev, staging, prod"
        exit 1
    fi

    if [[ ! " ${!ENVIRONMENTS[@]} " =~ " ${env} " ]]; then
        print_error "Invalid environment: ${env}"
        echo "Valid environments: dev staging prod"
        exit 1
    fi
}

validate_chart() {
    print_header "Validating Helm Chart"

    helm lint "${CHART_PATH}"

    print_success "Chart validation passed"
}

deploy_application() {
    local env=$1
    validate_environment "$env"
    check_prerequisites
    validate_chart

    local ns="${NAMESPACES[$env]}"
    local values_file="${CHART_PATH}/values-${env}.yaml"
    local account="${AWS_ACCOUNTS[$env]}"
    local region="${AWS_REGIONS[$env]}"

    print_header "Deploying to ${env} Environment"

    if [ ! -f "$values_file" ]; then
        print_error "Values file not found: $values_file"
        exit 1
    fi

    # Check/create namespace
    if ! kubectl get namespace "$ns" &> /dev/null; then
        print_info "Creating namespace: $ns"
        kubectl create namespace "$ns"
        print_success "Namespace created"
    else
        print_info "Namespace exists: $ns"
    fi

    print_info "Configuration:"
    print_info "  Environment: $env"
    print_info "  Namespace: $ns"
    print_info "  Chart: ${CHART_PATH}"
    print_info "  Values: $values_file"
    print_info "  AWS Account: $account"
    print_info "  AWS Region: $region"
    echo ""

    # Add/update Helm repositories if needed
    # helm repo add fraud-poc https://your-repo (if applicable)
    # helm repo update

    # Deploy/upgrade
    print_info "Deploying with Helm..."
    helm upgrade --install "$RELEASE_NAME" "${CHART_PATH}" \
        --namespace "$ns" \
        --values "${CHART_PATH}/values.yaml" \
        --values "$values_file" \
        --set "aws.account=${account}" \
        --set "aws.region=${region}" \
        --wait \
        --timeout 5m

    print_success "Deployment completed"
    echo ""

    # Show deployment status
    show_deployment_status "$env"
}

show_deployment_status() {
    local env=$1
    validate_environment "$env"

    local ns="${NAMESPACES[$env]}"

    print_header "Deployment Status - ${env}"

    print_info "Helm Release:"
    helm status "$RELEASE_NAME" -n "$ns" 2>/dev/null || echo "  Release not found"

    echo ""
    print_info "Kubernetes Resources:"
    kubectl get deployments,statefulsets,services,ingress -n "$ns"

    echo ""
    print_info "Pods:"
    kubectl get pods -n "$ns" -o wide

    echo ""
    print_info "Pod Status Details:"
    kubectl describe pods -n "$ns"
}

show_logs() {
    local env=$1
    validate_environment "$env"

    local ns="${NAMESPACES[$env]}"

    print_header "Pod Logs - ${env}"

    print_info "Following logs (Ctrl+C to exit)..."
    kubectl logs -f deployment/"${RELEASE_NAME}" -n "$ns"
}

shell_access() {
    local env=$1
    validate_environment "$env"

    local ns="${NAMESPACES[$env]}"

    print_header "Shell Access - ${env}"

    # Get pod name
    local pod=$(kubectl get pods -n "$ns" -l app="${RELEASE_NAME}" -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)

    if [ -z "$pod" ]; then
        print_error "No pods found in namespace $ns"
        echo "Deploy the application first with: ./install-k8s-helm.sh deploy $env"
        exit 1
    fi

    print_info "Connecting to pod: $pod"
    kubectl exec -it "$pod" -n "$ns" -- /bin/sh
}

uninstall_application() {
    local env=$1
    validate_environment "$env"

    local ns="${NAMESPACES[$env]}"

    print_header "Uninstalling from ${env} Environment"

    print_warning "This will remove the deployment from namespace: $ns"
    read -p "Continue? (yes/no): " -n 3 -r
    echo
    if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        print_info "Uninstalling Helm release..."
        helm uninstall "$RELEASE_NAME" -n "$ns" || print_warning "Release not found"

        print_info "Deleting namespace..."
        kubectl delete namespace "$ns" || print_warning "Namespace not found"

        print_success "Uninstall completed"
    else
        print_info "Operation cancelled"
    fi
}

dry_run_deployment() {
    local env=$1
    validate_environment "$env"
    check_prerequisites
    validate_chart

    local ns="${NAMESPACES[$env]}"
    local values_file="${CHART_PATH}/values-${env}.yaml"

    print_header "Dry-Run - ${env} Environment"

    if [ ! -f "$values_file" ]; then
        print_error "Values file not found: $values_file"
        exit 1
    fi

    print_info "Showing what WOULD be deployed (no changes made)..."
    helm upgrade --install "$RELEASE_NAME" "${CHART_PATH}" \
        --namespace "$ns" \
        --values "${CHART_PATH}/values.yaml" \
        --values "$values_file" \
        --dry-run \
        --debug
}

# Main
COMMAND="${1:-help}"
ENVIRONMENT="${2:-}"

case "${COMMAND}" in
    deploy)
        deploy_application "$ENVIRONMENT"
        ;;
    status)
        show_deployment_status "$ENVIRONMENT"
        ;;
    logs)
        show_logs "$ENVIRONMENT"
        ;;
    shell)
        shell_access "$ENVIRONMENT"
        ;;
    uninstall)
        uninstall_application "$ENVIRONMENT"
        ;;
    validate)
        check_prerequisites
        validate_chart
        ;;
    dry-run)
        dry_run_deployment "$ENVIRONMENT"
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        print_error "Unknown command: ${COMMAND}"
        echo "Run './install-k8s-helm.sh help' for usage"
        exit 1
        ;;
esac
