# Production-Ready HTTPS Deployment Script (PowerShell)
# Deploys fraud-poc-api to K8s with self-signed certificate
# Fetches secrets from AWS Secrets Manager via local AWS credentials

param(
    [string]$Namespace = "fraud-poc-api",
    [string]$AppName = "fraud-poc-api",
    [string]$AWSRegion = "af-south-1",
    [string]$AWSSecretName = "fraud_poc_secrets"
)

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "HTTPS Deployment - Production Ready" -ForegroundColor Cyan
Write-Host "With AWS Secrets Management" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Step 1: Checking prerequisites..." -ForegroundColor Blue

if (-not (Test-Path "certs\localhost.crt") -or -not (Test-Path "certs\localhost.key")) {
    Write-Host "ERROR: Certificate files not found!" -ForegroundColor Red
    exit
}
Write-Host "  OK: Certificate files found" -ForegroundColor Green

if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: kubectl not found" -ForegroundColor Red
    exit
}
Write-Host "  OK: kubectl found" -ForegroundColor Green

if (-not (Get-Command helm -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: helm not found" -ForegroundColor Red
    exit
}
Write-Host "  OK: helm found" -ForegroundColor Green

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: aws CLI not found" -ForegroundColor Red
    exit
}
Write-Host "  OK: aws CLI found" -ForegroundColor Green

Write-Host ""

# Verify AWS credentials
Write-Host "Step 2: Verifying AWS credentials..." -ForegroundColor Blue

try {
    $Identity = aws sts get-caller-identity | ConvertFrom-Json
    Write-Host "  OK: AWS credentials valid" -ForegroundColor Green
    Write-Host "    Account: $($Identity.Account)" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: AWS credentials not configured or invalid" -ForegroundColor Red
    Write-Host "Please run: aws configure"
    exit
}

Write-Host ""

# Fetch secrets from AWS
Write-Host "Step 3: Fetching secrets from AWS Secrets Manager..." -ForegroundColor Blue
Write-Host "  Retrieving: $AWSSecretName from region: $AWSRegion" -ForegroundColor Gray

try {
    $SecretString = aws secretsmanager get-secret-value --secret-id $AWSSecretName --region $AWSRegion --query SecretString --output text
    $SecretJson = $SecretString | ConvertFrom-Json

    $DB_USERNAME = $SecretJson.DB_USERNAME
    $DB_PASSWORD = $SecretJson.DB_PASSWORD
    $KAFKA_USER = $SecretJson.KAFKA_USER
    $KAFKA_PASSWORD = $SecretJson.KAFKA_PASSWORD
    $API_USERNAME = $SecretJson.API_USERNAME
    $API_PASSWORD = $SecretJson.API_PASSWORD

    if (-not $DB_USERNAME -or -not $DB_PASSWORD) {
        Write-Host "ERROR: Missing required secrets" -ForegroundColor Red
        exit
    }

    if (-not $API_USERNAME -or -not $API_PASSWORD) {
        Write-Host "ERROR: Missing API credentials in secrets" -ForegroundColor Red
        exit
    }

    Write-Host "  OK: Secrets retrieved" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to retrieve secret from AWS" -ForegroundColor Red
    Write-Host "  Secret: $AWSSecretName" -ForegroundColor Gray
    Write-Host "  Region: $AWSRegion" -ForegroundColor Gray
    exit
}

Write-Host ""

# Clean up existing deployment
Write-Host "Step 4: Cleaning up existing deployment..." -ForegroundColor Blue

kubectl delete namespace $Namespace 2>$null | Out-Null
Start-Sleep -Seconds 3

Write-Host "  OK: Namespace cleaned" -ForegroundColor Green
Write-Host ""

# Deploy with Helm
Write-Host "Step 5: Deploying with Helm..." -ForegroundColor Blue

try {
    helm upgrade --install $AppName ./charts -f charts/values-localhost-https.yaml `
        --set env.DB_USERNAME=$DB_USERNAME `
        --set env.DB_PASSWORD=$DB_PASSWORD `
        --set env.KAFKA_USER=$KAFKA_USER `
        --set env.KAFKA_PASSWORD=$KAFKA_PASSWORD `
        --set env.API_USERNAME=$API_USERNAME `
        --set env.API_PASSWORD=$API_PASSWORD `
        --create-namespace -n $Namespace | Out-Null
    Write-Host "  OK: Helm deployment completed" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Helm deployment failed" -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit
}

Write-Host ""

# Create TLS secret
Write-Host "Step 6: Creating TLS secret..." -ForegroundColor Blue

try {
    kubectl create secret tls "${AppName}-tls" --cert="certs\localhost.crt" --key="certs\localhost.key" -n $Namespace | Out-Null
    Write-Host "  OK: TLS secret created" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to create TLS secret" -ForegroundColor Red
    exit
}

Write-Host ""

# Restart deployment
Write-Host "Step 7: Restarting deployment..." -ForegroundColor Blue

try {
    kubectl rollout restart deployment/$AppName -n $Namespace | Out-Null
    Write-Host "  OK: Deployment restarted" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to restart deployment" -ForegroundColor Red
    exit
}

Write-Host ""

# Wait for deployment
Write-Host "Step 8: Waiting for deployment (max 5 minutes)..." -ForegroundColor Blue

try {
    kubectl rollout status deployment/$AppName -n $Namespace --timeout=5m | Out-Null
    Write-Host "  OK: Deployment ready" -ForegroundColor Green
}
catch {
    Write-Host "  WARNING: Deployment check timed out" -ForegroundColor Yellow
}

Write-Host ""

# Verify deployment
Write-Host "Step 9: Verifying deployment..." -ForegroundColor Blue
Write-Host ""
Write-Host "Resources in namespace:" -ForegroundColor Cyan
kubectl get all -n $Namespace

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Port-forward: kubectl port-forward -n $Namespace svc/$AppName 8443:443"
Write-Host "2. Test API: curl -k https://localhost:8443/health/readiness"
Write-Host "3. View logs: kubectl logs -f deployment/$AppName -n $Namespace"
Write-Host ""
