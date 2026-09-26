@echo off
REM Production-Ready HTTPS Deployment Script (Windows)
REM Deploys fraud-poc-api to K8s with self-signed certificate
REM Certificate must already exist in certs/ folder

setlocal enabledelayedexpansion

echo.
echo ==========================================
echo HTTPS Deployment - Production Ready
echo ==========================================
echo.

REM Verify certificate files exist
echo [Step 1] Checking for certificate files...
if not exist "certs\localhost.crt" (
    echo ERROR: certs\localhost.crt not found!
    echo Please generate certificate first using one of these methods:
    echo   - PowerShell: .\generate-cert.ps1
    echo   - Python: python generate_cert.py
    echo   - Docker: docker run --rm -v %%CD%%\certs:/certs alpine/openssl ...
    pause
    exit /b 1
)
if not exist "certs\localhost.key" (
    echo ERROR: certs\localhost.key not found!
    echo Please generate certificate first.
    pause
    exit /b 1
)
echo [OK] Certificate files found
echo.

REM Step 1: Create Namespace
echo [Step 1] Creating Kubernetes namespace...
kubectl create namespace fraud-poc-api 2>nul
echo [OK] Namespace ready
echo.

REM Step 2: Create TLS Secret
echo [Step 2] Creating TLS secret in Kubernetes...
kubectl delete secret fraud-poc-api-tls -n fraud-poc-api 2>nul
kubectl create secret tls fraud-poc-api-tls ^
  --cert=certs/localhost.crt ^
  --key=certs/localhost.key ^
  -n fraud-poc-api
if errorlevel 1 (
    echo ERROR: Failed to create TLS secret
    pause
    exit /b 1
)
echo [OK] TLS secret created
echo.

REM Step 3: Deploy with Helm
echo [Step 3] Deploying with Helm...
helm upgrade --install fraud-poc-api ./charts ^
  -f charts/values-localhost-https.yaml ^
  -n fraud-poc-api
if errorlevel 1 (
    echo ERROR: Helm deployment failed
    pause
    exit /b 1
)
echo [OK] Helm deployment completed
echo.

REM Step 4: Wait for deployment
echo [Step 4] Waiting for deployment to be ready (max 5 minutes)...
kubectl rollout status deployment/fraud-poc-api -n fraud-poc-api --timeout=5m
if errorlevel 1 (
    echo WARNING: Deployment status check timed out
    echo Check pod status with: kubectl get pods -n fraud-poc-api
) else (
    echo [OK] Deployment ready
)
echo.

REM Step 5: Verify
echo [Step 5] Verifying deployment...
echo.
echo Resources in fraud-poc-api namespace:
kubectl get all -n fraud-poc-api
echo.
echo TLS Secret:
kubectl get secret fraud-poc-api-tls -n fraud-poc-api
echo.

REM Step 6: Success message
echo.
echo ==========================================
echo SUCCESS: HTTPS Deployment Complete!
echo ==========================================
echo.
echo Next steps:
echo 1. Port-forward to access the service:
echo    kubectl port-forward -n fraud-poc-api svc/fraud-poc-api 8443:443
echo.
echo 2. In another terminal, test the REST API:
echo    curl -k https://localhost:8443/api/fraud/events
echo.
echo 3. View certificate details:
echo    openssl s_client -connect localhost:8443 -servername localhost
echo.
echo 4. View pod logs:
echo    kubectl logs -f deployment/fraud-poc-api -n fraud-poc-api
echo.
echo For full documentation, see: HTTPS_DEPLOYMENT_GUIDE.md
echo.
pause
