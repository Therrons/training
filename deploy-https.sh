#!/bin/bash

# Production-Ready HTTPS Deployment Script
# Deploys fraud-poc-api to K8s with self-signed certificate
# Fetches secrets from AWS Secrets Manager via local AWS credentials
# Injects secrets as environment variables into Kubernetes pods

set -e  # Exit on error

echo "=========================================="
echo "HTTPS Deployment - Production Ready"
echo "With AWS Secrets Management"
echo "=========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="fraud-poc-api"
APP_NAME="fraud-poc-api"
AWS_REGION="${AWS_REGION:-af-south-1}"
AWS_SECRET_NAME="fraud_poc_secrets"  # Change this to your AWS secret name

# Step 1: Verify prerequisites
echo -e "${BLUE}Step 1: Checking prerequisites...${NC}"
if [ ! -f "certs/localhost.crt" ] || [ ! -f "certs/localhost.key" ]; then
    echo -e "${RED}✗ ERROR: Certificate files not found!${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Certificate files found${NC}"

if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}✗ ERROR: kubectl not found${NC}"
    exit 1
fi
echo -e "${GREEN}✓ kubectl found${NC}"

if ! command -v helm &> /dev/null; then
    echo -e "${RED}✗ ERROR: helm not found${NC}"
    exit 1
fi
echo -e "${GREEN}✓ helm found${NC}"

if ! command -v aws &> /dev/null; then
    echo -e "${RED}✗ ERROR: aws CLI not found${NC}"
    exit 1
fi
echo -e "${GREEN}✓ aws CLI found${NC}"
echo ""

# Step 2: Verify AWS credentials
echo -e "${BLUE}Step 2: Verifying AWS credentials...${NC}"
if ! aws sts get-caller-identity &> /dev/null; then
    echo -e "${RED}✗ ERROR: AWS credentials not configured or invalid${NC}"
    echo "Please run: aws configure"
    exit 1
fi
echo -e "${GREEN}✓ AWS credentials valid${NC}"
echo ""

# Step 3: Fetch secrets from AWS Secrets Manager
echo -e "${BLUE}Step 3: Fetching secrets from AWS Secrets Manager...${NC}"
echo "Retrieving secret: $AWS_SECRET_NAME from region: $AWS_REGION"

SECRET_JSON=$(aws secretsmanager get-secret-value \
  --secret-id "$AWS_SECRET_NAME" \
  --region "$AWS_REGION" \
  --query SecretString \
  --output text 2>/dev/null) || {
    echo -e "${RED}✗ ERROR: Failed to retrieve secret from AWS${NC}"
    echo "Secret name: $AWS_SECRET_NAME"
    echo "AWS region: $AWS_REGION"
    echo ""
    echo "Make sure:"
    echo "  1. Secret '$AWS_SECRET_NAME' exists in AWS Secrets Manager"
    echo "  2. AWS region is correct: $AWS_REGION"
    echo "  3. Your AWS credentials have permission to read the secret"
    exit 1
}

# Extract individual secret values
DB_USERNAME=$(echo "$SECRET_JSON" | jq -r '.DB_USERNAME // empty' 2>/dev/null)
DB_PASSWORD=$(echo "$SECRET_JSON" | jq -r '.DB_PASSWORD // empty' 2>/dev/null)
KAFKA_USER=$(echo "$SECRET_JSON" | jq -r '.KAFKA_USER // empty' 2>/dev/null)
KAFKA_PASSWORD=$(echo "$SECRET_JSON" | jq -r '.KAFKA_PASSWORD // empty' 2>/dev/null)
API_USERNAME=$(echo "$SECRET_JSON" | jq -r '.API_USERNAME // empty' 2>/dev/null)
API_PASSWORD=$(echo "$SECRET_JSON" | jq -r '.API_PASSWORD // empty' 2>/dev/null)

if [ -z "$DB_USERNAME" ] || [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}✗ ERROR: Missing required secrets (DB_USERNAME or DB_PASSWORD)${NC}"
    exit 1
fi

if [ -z "$API_USERNAME" ] || [ -z "$API_PASSWORD" ]; then
    echo -e "${RED}✗ ERROR: Missing API credentials in secrets (API_USERNAME or API_PASSWORD)${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Secrets retrieved successfully${NC}"
echo -e "  DB_USERNAME: ${DB_USERNAME:0:3}***"
echo -e "  DB_PASSWORD: ***"
if [ -n "$KAFKA_USER" ]; then
    echo -e "  KAFKA_USER: ${KAFKA_USER:0:3}***"
fi
echo ""

# Step 4: Clean up existing deployment
echo -e "${BLUE}Step 4: Cleaning up existing deployment...${NC}"
kubectl delete namespace $NAMESPACE 2>/dev/null || true
sleep 3
echo -e "${GREEN}✓ Namespace cleaned${NC}"
echo ""

# Step 5: Deploy with Helm (creates namespace with Helm metadata)
echo -e "${BLUE}Step 5: Deploying with Helm (injecting secrets)...${NC}"
helm upgrade --install $APP_NAME ./charts \
  -f charts/values-localhost-https.yaml \
  --set env.DB_USERNAME="$DB_USERNAME" \
  --set env.DB_PASSWORD="$DB_PASSWORD" \
  --set env.KAFKA_USER="$KAFKA_USER" \
  --set env.KAFKA_PASSWORD="$KAFKA_PASSWORD" \
  --set env.API_USERNAME="$API_USERNAME" \
  --set env.API_PASSWORD="$API_PASSWORD" \
  --create-namespace \
  -n $NAMESPACE
echo -e "${GREEN}✓ Helm deployment completed${NC}"
echo ""

# Step 6: Create TLS Secret
echo -e "${BLUE}Step 6: Creating TLS secret in Kubernetes...${NC}"
kubectl create secret tls ${APP_NAME}-tls \
  --cert=certs/localhost.crt \
  --key=certs/localhost.key \
  -n $NAMESPACE
echo -e "${GREEN}✓ TLS secret created${NC}"
echo ""

# Step 7: Restart deployment to pick up secrets
echo -e "${BLUE}Step 7: Restarting deployment to pick up secrets...${NC}"
kubectl rollout restart deployment/$APP_NAME -n $NAMESPACE
echo -e "${GREEN}✓ Deployment restarted${NC}"
echo ""

# Step 8: Wait for deployment
echo -e "${BLUE}Step 8: Waiting for deployment to be ready (max 5 minutes)...${NC}"
if kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=5m; then
    echo -e "${GREEN}✓ Deployment ready${NC}"
else
    echo -e "${YELLOW}⚠️  Deployment status check timed out${NC}"
    echo "Check pod status with: kubectl get pods -n $NAMESPACE"
fi
echo ""

# Step 9: Verify
echo -e "${BLUE}Step 9: Verifying deployment...${NC}"
echo ""
echo "Resources in $NAMESPACE namespace:"
kubectl get all -n $NAMESPACE
echo ""
echo "TLS Secret:"
kubectl get secret ${APP_NAME}-tls -n $NAMESPACE
echo ""

# Step 10: Success message
echo -e "${GREEN}=========================================="
echo "✓ HTTPS Deployment Successful!"
echo "==========================================${NC}"
echo ""
echo "Next steps:"
echo "1. Port-forward to access the service:"
echo "   kubectl port-forward -n $NAMESPACE svc/$APP_NAME 8443:443"
echo ""
echo "2. In another terminal, test the REST API:"
echo "   curl -k https://localhost:8443/api/fraud/events"
echo ""
echo "3. View pod logs:"
echo "   kubectl logs -f deployment/$APP_NAME -n $NAMESPACE"
echo ""
echo "4. Check environment variables in pod:"
echo "   kubectl exec -it -n $NAMESPACE deployment/$APP_NAME -- env | grep DB"
echo ""
