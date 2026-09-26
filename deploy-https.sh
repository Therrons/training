#!/bin/bash

# Production-Ready HTTPS Deployment Script
# Deploys fraud-poc-api to K8s with self-signed certificate
# Certificate must already exist in certs/ folder

set -e  # Exit on error

echo "=========================================="
echo "HTTPS Deployment - Production Ready"
echo "=========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Step 1: Verify Certificate Files
echo -e "${BLUE}Step 1: Checking for certificate files...${NC}"
if [ ! -f "certs/localhost.crt" ] || [ ! -f "certs/localhost.key" ]; then
    echo -e "${RED}✗ ERROR: Certificate files not found!${NC}"
    echo ""
    echo "Please generate certificate first using one of these methods:"
    echo "  - PowerShell: .\generate-cert.ps1"
    echo "  - Python: python generate_cert.py"
    echo "  - Docker: docker run --rm -v \$(pwd)/certs:/certs alpine/openssl ..."
    echo ""
    exit 1
fi
echo -e "${GREEN}✓ Certificate files found${NC}"
echo ""

# Step 2: Create Namespace
echo -e "${BLUE}Step 2: Creating Kubernetes namespace...${NC}"
kubectl create namespace fraud-poc-api 2>/dev/null || echo -e "${YELLOW}⚠️  Namespace already exists${NC}"
echo -e "${GREEN}✓ Namespace ready${NC}"
echo ""

# Step 3: Create TLS Secret
echo -e "${BLUE}Step 3: Creating TLS secret in Kubernetes...${NC}"
kubectl delete secret fraud-poc-api-tls -n fraud-poc-api 2>/dev/null || true
kubectl create secret tls fraud-poc-api-tls \
  --cert=certs/localhost.crt \
  --key=certs/localhost.key \
  -n fraud-poc-api
echo -e "${GREEN}✓ TLS secret created${NC}"
echo ""

# Step 4: Deploy with Helm
echo -e "${BLUE}Step 4: Deploying with Helm...${NC}"
helm upgrade --install fraud-poc-api ./charts \
  -f charts/values-localhost-https.yaml \
  -n fraud-poc-api
echo -e "${GREEN}✓ Helm deployment completed${NC}"
echo ""

# Step 5: Wait for deployment
echo -e "${BLUE}Step 5: Waiting for deployment to be ready (max 5 minutes)...${NC}"
if kubectl rollout status deployment/fraud-poc-api -n fraud-poc-api --timeout=5m; then
    echo -e "${GREEN}✓ Deployment ready${NC}"
else
    echo -e "${YELLOW}⚠️  Deployment status check timed out${NC}"
    echo "Check pod status with: kubectl get pods -n fraud-poc-api"
fi
echo ""

# Step 6: Verify
echo -e "${BLUE}Step 6: Verifying deployment...${NC}"
echo ""
echo "Resources in fraud-poc-api namespace:"
kubectl get all -n fraud-poc-api
echo ""
echo "TLS Secret:"
kubectl get secret fraud-poc-api-tls -n fraud-poc-api
echo ""

# Step 7: Success message
echo -e "${GREEN}=========================================="
echo "✓ HTTPS Deployment Successful!"
echo "==========================================${NC}"
echo ""
echo "Next steps:"
echo "1. Port-forward to access the service:"
echo "   kubectl port-forward -n fraud-poc-api svc/fraud-poc-api 8443:443"
echo ""
echo "2. In another terminal, test the REST API:"
echo "   curl -k https://localhost:8443/api/fraud/events"
echo ""
echo "3. View certificate details:"
echo "   openssl s_client -connect localhost:8443 -servername localhost"
echo ""
echo "4. View pod logs:"
echo "   kubectl logs -f deployment/fraud-poc-api -n fraud-poc-api"
echo ""
echo "For full documentation, see: HTTPS_DEPLOYMENT_GUIDE.md"
echo ""
