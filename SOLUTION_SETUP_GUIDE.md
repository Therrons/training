# Fraud Detection System - Setup & Deployment Guide

## Overview

This guide walks through getting the fraud detection system running:
1. Local development environment setup
2. Containerized infrastructure setup
3. Local solution deployment
4. Kubernetes deployment
5. Integration testing

---

## Prerequisites

### System Requirements
- **Windows 11 Enterprise** or later
- **PowerShell 7.x** or later (run as Administrator)
- **.NET 8.0 SDK** installed
- **Docker** (via Rancher Desktop)

---

## Step 1: Install Rancher Desktop

Rancher Desktop provides Kubernetes and Docker support locally.

### Installation
1. Download from [Rancher Desktop](https://rancherdesktop.io/)
2. Install and run Rancher Desktop
3. Verify installation:
   ```powershell
   docker --version
   kubectl version --client
   ```

### Enable Kubernetes
1. Open Rancher Desktop settings
2. Ensure Kubernetes is enabled and running
3. Wait for "Kubernetes is running" message

---

## Step 2: Install Traefik & PostgreSQL

### Prerequisites
- Rancher Desktop must be running
- Docker must be accessible

### Installation Steps

1. **Navigate to the solution directory:**
   ```powershell
   cd C:\Development\Therron\training\nuget-config
   ```

2. **Run the docker-compose file:**
   ```powershell
   docker-compose -f docker-compose.traefik.postgresql.yml up -d
   ```

3. **Verify services are running:**
   ```powershell
   docker ps
   ```
   Look for:
   - `postgres` container (port 5432)
   - `traefik` container (port 8080, 8081)

### Test Installation

**Traefik Dashboard:**
- Open browser: `http://localhost:8081`
- Should see Traefik dashboard

**PostgreSQL:**
- Open browser: `http://localhost:8080`
- Should see PostgreSQL service running

---

## Step 3: Run Solution Locally

### Prerequisites
- .NET 8.0 SDK installed
- PostgreSQL running (from Step 2)
- Solution cloned to `C:\Development\Therron\training`

### Run Locally

1. **Navigate to solution directory:**
   ```powershell
   cd C:\Development\Therron\training
   ```

2. **Build the solution:**
   ```powershell
   dotnet build
   ```

3. **Run the solution:**
   ```powershell
   dotnet run --project .\src\fraud_poc_project_ui\fraud_poc_project.csproj
   ```

4. **Access locally:**
   - API: `http://localhost:5000`
   - Swagger: `http://localhost:5000/swagger/index.html`
   - Health: `http://localhost:5000/health/readiness`

---

## Step 4: Deploy to Kubernetes (Rancher Desktop)

### Prerequisites
- Rancher Desktop running with Kubernetes enabled
- Traefik and PostgreSQL running (Step 2)
- Solution built successfully (Step 3)

### Deployment Steps

1. **Navigate to solution directory:**
   ```powershell
   cd C:\Development\Therron\training
   ```

2. **Run deployment script (as Administrator):**
   ```powershell
   .\deploy-https.ps1
   ```

3. **Wait for deployment to complete:**
   - Script will deploy to `fraud-poc-api` namespace
   - Wait 2-3 minutes for all pods to be ready

4. **Verify deployment:**
   ```powershell
   kubectl get pods -n fraud-poc-api
   kubectl get services -n fraud-poc-api
   ```

---

## Step 5: Test Kubernetes Deployment

After successful deployment, test the running service using one of two methods:

### Option 1: Via HTTPS Ingress (Recommended)

Since Traefik ingress is configured:

1. **Access via HTTPS:**
   - Swagger: `https://localhost/swagger/index.html`
   - Health: `https://localhost/health/readiness`

2. **Certificate Warning:**
   - You'll get a self-signed certificate warning
   - Click "Advanced" and accept to proceed

### Option 2: Via Port-Forward

**Terminal 1 - Start port-forward:**
```powershell
kubectl port-forward -n fraud-poc-api svc/fraud-poc-api 8085:8083
```

**Terminal 2 - Access via localhost:**
- Health: `http://localhost:8085/health/readiness`
- Swagger: `http://localhost:8085/swagger/index.html`

---

## Step 6: Run Integration Tests

Integration tests validate the entire system including local and Kubernetes deployments.

### Prerequisites
- Solution deployed (either locally or to K8s)
- PostgreSQL running
- PowerShell running as **Administrator**

### Run Tests

1. **Open PowerShell as Administrator**

2. **Navigate to training directory:**
   ```powershell
   cd C:\Development\Therron\training
   ```

3. **Run integration tests:**
   ```powershell
   .\run-tests.ps1
   ```

4. **View results:**
   - Script automatically suppresses compiler warnings
   - Shows clean test output with pass/fail summary
   - Expected: All 65+ tests passing

### Test Categories

Run specific test categories:

```powershell
# API tests only (14 tests)
.\run-tests.ps1 -Category api

# Business logic tests (27 tests)
.\run-tests.ps1 -Category business

# Metrics tests (21 tests)
.\run-tests.ps1 -Category metrics

# Controller tests (9 tests)
.\run-tests.ps1 -Category controllers

# Show compiler warnings if needed
.\run-tests.ps1 -ShowWarnings
```

---

## Complete Workflow

### First Time Setup (Local Development)
1. Install Rancher Desktop
2. Install Traefik & PostgreSQL
3. Run solution locally
4. Run integration tests

### First Time Setup (Kubernetes Deployment)
1. Install Rancher Desktop
2. Install Traefik & PostgreSQL
3. Deploy to Kubernetes
4. Test Kubernetes deployment
5. Run integration tests

### Daily Development
```powershell
# 1. Ensure Rancher Desktop is running
# 2. Quick test - controllers only (2-3 seconds)
.\run-tests.ps1 -Category controllers

# 3. Full test before commit (30-40 seconds)
.\run-tests.ps1

# 4. If failures, debug with warnings
.\run-tests.ps1 -ShowWarnings
```

---

## Troubleshooting

### Rancher Desktop Won't Start
- Check Windows Hyper-V is enabled
- Restart Docker daemon
- Restart machine if needed

### PostgreSQL Connection Failed
```powershell
# Check if container is running
docker ps | findstr postgres

# Check logs
docker logs <container-id>

# Restart services
docker-compose -f docker-compose.traefik.postgresql.yml restart
```

### Kubernetes Deployment Failed
```powershell
# Check deployment status
kubectl describe deployment fraud-poc-api -n fraud-poc-api

# Check pod logs
kubectl logs -n fraud-poc-api -l app=fraud-poc-api --tail=50

# Delete failed deployment and redeploy
kubectl delete namespace fraud-poc-api
.\deploy-https.ps1
```

### Tests Failing
```powershell
# Run with warnings to see detailed output
.\run-tests.ps1 -ShowWarnings

# Run specific test category to isolate issues
.\run-tests.ps1 -Category business

# Check if services are running
docker ps
kubectl get pods -n fraud-poc-api
```

---

## Quick Reference

| Action | Command |
|--------|---------|
| Start services | `docker-compose -f docker-compose.traefik.postgresql.yml up -d` |
| Stop services | `docker-compose -f docker-compose.traefik.postgresql.yml down` |
| View logs | `docker-compose logs -f` |
| Deploy to K8s | `.\deploy-https.ps1` |
| Run all tests | `.\run-tests.ps1` |
| Run API tests | `.\run-tests.ps1 -Category api` |
| Port-forward | `kubectl port-forward -n fraud-poc-api svc/fraud-poc-api 8085:8083` |
| Check K8s pods | `kubectl get pods -n fraud-poc-api` |

---

## Support

For detailed test information, see: `RUN_TESTS_GUIDE.md`

For Kubernetes troubleshooting, check deployment logs:
```powershell
kubectl describe pod <pod-name> -n fraud-poc-api
kubectl logs <pod-name> -n fraud-poc-api
```
