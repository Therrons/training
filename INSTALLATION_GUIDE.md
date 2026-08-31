# Fraud POC - Installation & Deployment Guide

Complete guide to installing and running the Fraud Detection POC application using three different deployment methods.

---

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [Option 1: Simple Docker](#option-1-simple-docker-local-development)
3. [Option 2: Docker Compose](#option-2-docker-compose-formal-local-setup)
4. [Option 3: Kubernetes with Helm](#option-3-kubernetes-with-helm-production)
5. [Troubleshooting](#troubleshooting)
6. [Comparison](#comparison)

---

## 🚀 Quick Start

### Prerequisites (All Methods)
```bash
# Verify installation
docker --version          # Docker 20.10+
docker-compose --version  # Docker Compose 2.0+ (or 1.29+)
kubectl version          # kubectl (for K8s deployment)
helm version             # Helm 3.x (for K8s deployment)
```

### Choose Your Installation Method

- **Local Development?** → Use [Option 1: Simple Docker](#option-1-simple-docker-local-development)
- **Full Local Environment?** → Use [Option 2: Docker Compose](#option-2-docker-compose-formal-local-setup)
- **Production Deployment?** → Use [Option 3: Kubernetes](#option-3-kubernetes-with-helm-production)

---

## Option 1: Simple Docker (Local Development)

**Best for:** Individual development, quick testing, minimal setup

**What it does:**
- Builds a single Docker image from the Dockerfile
- Runs the API container with your local or remote database
- Quick start, minimal configuration

### Installation Steps

1. **Make script executable:**
   ```bash
   chmod +x install-docker.sh
   ```

2. **Build the image:**
   ```bash
   ./install-docker.sh build
   ```

3. **Run the container (with local database):**
   ```bash
   export DB_HOST=host.docker.internal
   export DB_PORT=5432
   export DB_USERNAME=therrons
   export DB_PASSWORD=therrons
   ./install-docker.sh run
   ```

4. **Access the application:**
   ```
   http://localhost:8080/swagger
   ```

5. **Stop when done:**
   ```bash
   ./install-docker.sh stop
   ```

### Available Commands

```bash
./install-docker.sh build       # Build Docker image
./install-docker.sh run         # Start container
./install-docker.sh stop        # Stop container
./install-docker.sh logs        # View logs
./install-docker.sh ps          # Show status
./install-docker.sh shell       # Open shell in container
./install-docker.sh clean       # Remove container and image
./install-docker.sh help        # Show help
```

### Customize with Environment Variables

```bash
# Run with custom database
export DB_HOST=192.168.1.100
export DB_USERNAME=myuser
export DB_PASSWORD=mypass
./install-docker.sh run

# Run on different port
export HOST_PORT=9090
./install-docker.sh run
```

### Example: Development Workflow

```bash
# 1. Build image
./install-docker.sh build

# 2. Start with your local database
./install-docker.sh run

# 3. Access Swagger UI
# http://localhost:8080/swagger

# 4. Monitor logs in real-time
./install-docker.sh logs

# 5. When done, stop and clean
./install-docker.sh stop
./install-docker.sh clean
```

---

## Option 2: Docker Compose (Formal Local Setup)

**Best for:** Full local environment, testing multi-service interactions, realistic simulation

**What it includes:**
- PostgreSQL 16 database
- Fraud POC API
- Nginx reverse proxy
- Persistent storage volumes
- Health checks and networking

### Installation Steps

1. **Make script executable:**
   ```bash
   chmod +x install-docker-compose.sh
   ```

2. **Verify .env file:**
   ```bash
   # .env file should contain:
   POSTGRES_PASSWORD=therrons
   POSTGRES_USER=therrons
   POSTGRES_DB=fraud_db
   ```

3. **Start all services:**
   ```bash
   ./install-docker-compose.sh up
   ```

4. **Access the services:**
   ```
   API (Swagger):     http://localhost:8085/swagger
   Nginx Proxy:       http://localhost:8085
   PostgreSQL:        localhost:5432
   ```

5. **Stop when done:**
   ```bash
   ./install-docker-compose.sh stop
   ```

### Available Commands

```bash
./install-docker-compose.sh up          # Start all services
./install-docker-compose.sh down        # Stop and remove services
./install-docker-compose.sh stop        # Stop (keep containers)
./install-docker-compose.sh logs        # View live logs
./install-docker-compose.sh ps          # Show service status
./install-docker-compose.sh shell-api   # Shell in API container
./install-docker-compose.sh shell-db    # Shell in DB container
./install-docker-compose.sh rebuild     # Rebuild and restart
./install-docker-compose.sh clean       # Remove all (containers, images, volumes)
./install-docker-compose.sh help        # Show help
```

### Example: Full Development Cycle

```bash
# 1. Start complete environment (builds on first run)
./install-docker-compose.sh up
# Takes ~1-2 minutes on first run

# 2. Verify services are healthy
./install-docker-compose.sh ps

# 3. View logs
./install-docker-compose.sh logs

# 4. Open shell in API to debug
./install-docker-compose.sh shell-api

# 5. When done, stop everything
./install-docker-compose.sh down
```

### Customization

Edit `docker-compose.yml` to:
- Change port mappings
- Add additional services
- Modify resource limits
- Adjust environment variables

---

## Option 3: Kubernetes with Helm (Production)

**Best for:** Production deployments, multi-environment setups, scalability

**What it provides:**
- Multi-environment support (dev, staging, prod)
- Helm charts for templated deployments
- Persistent storage
- Service mesh ready
- AWS IAM integration
- Horizontal scaling

### Prerequisites

```bash
# 1. Connect to your K8s cluster
kubectl config use-context <your-cluster-context>

# 2. Verify connection
kubectl cluster-info

# 3. Configure AWS IAM roles for each environment
# (Pre-create IAM roles in AWS before deploying)
```

### Installation Steps

1. **Make script executable:**
   ```bash
   chmod +x install-k8s-helm.sh
   ```

2. **Configure for your AWS accounts:**
   ```bash
   # Edit install-k8s-helm.sh
   # Update AWS_ACCOUNTS and AWS_REGIONS:
   
   declare -A AWS_ACCOUNTS=([dev]="123456789012" [staging]="210987654321" [prod]="109876543210")
   declare -A AWS_REGIONS=([dev]="us-east-1" [staging]="us-east-1" [prod]="us-east-1")
   ```

3. **Update Helm values for your environment:**
   ```bash
   # Edit values-dev.yaml, values-staging.yaml, values-prod.yaml
   # Set correct IAM role ARNs:
   
   serviceAccount:
     iamRoleArn: "arn:aws:iam::123456789012:role/fraud-poc-api-dev-sa"
   ```

4. **Deploy to development:**
   ```bash
   ./install-k8s-helm.sh deploy dev
   ```

5. **Check deployment status:**
   ```bash
   ./install-k8s-helm.sh status dev
   ```

6. **Access the application:**
   ```bash
   # Get service details
   kubectl get svc -n fraud-poc-api-dev
   
   # Port-forward for testing
   kubectl port-forward svc/fraud-poc-api 8080:8080 -n fraud-poc-api-dev
   # http://localhost:8080/swagger
   ```

### Available Commands

```bash
./install-k8s-helm.sh deploy dev         # Deploy to development
./install-k8s-helm.sh status staging     # Check status
./install-k8s-helm.sh logs prod          # View logs
./install-k8s-helm.sh shell dev          # Shell in pod
./install-k8s-helm.sh dry-run prod       # Preview changes
./install-k8s-helm.sh uninstall staging  # Remove from cluster
./install-k8s-helm.sh validate           # Validate Helm chart
./install-k8s-helm.sh help               # Show help
```

### Multi-Environment Deployment

```bash
# Deploy to all environments
./install-k8s-helm.sh deploy dev
./install-k8s-helm.sh deploy staging
./install-k8s-helm.sh deploy prod

# Verify all deployments
./install-k8s-helm.sh status dev
./install-k8s-helm.sh status staging
./install-k8s-helm.sh status prod
```

### Example: Production Deployment Workflow

```bash
# 1. Validate chart
./install-k8s-helm.sh validate

# 2. Preview what will be deployed
./install-k8s-helm.sh dry-run prod

# 3. Deploy to production
./install-k8s-helm.sh deploy prod

# 4. Monitor deployment
./install-k8s-helm.sh status prod
./install-k8s-helm.sh logs prod

# 5. Access application
kubectl port-forward svc/fraud-poc-api 8080:8080 -n fraud-poc-api-prod
# http://localhost:8080/swagger
```

### Helm Chart Structure

```
charts/fraud-poc-api/
├── Chart.yaml                    # Chart metadata
├── values.yaml                   # Base values
├── values-dev.yaml               # Dev environment overrides
├── values-staging.yaml           # Staging environment overrides
├── values-prod.yaml              # Production environment overrides
├── templates/
│   ├── deployment.yaml           # Main deployment
│   ├── service.yaml              # Kubernetes service
│   ├── ingress.yaml              # Ingress configuration
│   ├── statefulset.yaml          # StatefulSet (for databases)
│   ├── pvc.yaml                  # Persistent volume claims
│   └── ...
```

---

## 🔧 Troubleshooting

### Docker Issues

**Problem:** "Docker daemon is not running"
```bash
# Solution: Start Docker Desktop or Docker daemon
# macOS: open -a Docker
# Linux: sudo systemctl start docker
# Windows: Open Docker Desktop application
```

**Problem:** "Port already in use"
```bash
# Find process using port
lsof -i :8080  # macOS/Linux
netstat -ano | findstr :8080  # Windows

# Kill process or change port in script
```

**Problem:** "Image not found"
```bash
# Rebuild the image
./install-docker.sh build
```

### Docker Compose Issues

**Problem:** "Database connection refused"
```bash
# Wait for PostgreSQL to be ready
./install-docker-compose.sh ps

# Check database logs
./install-docker-compose.sh logs
```

**Problem:** "Services fail to start"
```bash
# Clean and rebuild
./install-docker-compose.sh clean
./install-docker-compose.sh up
```

### Kubernetes Issues

**Problem:** "Error: release not found"
```bash
# Chart needs to be deployed first
./install-k8s-helm.sh deploy dev
```

**Problem:** "Pod is in CrashLoopBackOff"
```bash
# Check pod logs
./install-k8s-helm.sh logs dev

# Check pod events
kubectl describe pod <pod-name> -n fraud-poc-api-dev
```

**Problem:** "No pods found in namespace"
```bash
# Deployment may still be starting
./install-k8s-helm.sh status dev

# Wait a moment and try again
sleep 30
./install-k8s-helm.sh status dev
```

---

## 📊 Comparison

| Aspect | Docker | Docker Compose | Kubernetes |
|--------|--------|----------------|-----------|
| **Complexity** | Simple | Medium | Advanced |
| **Setup Time** | ~5 min | ~10 min | ~20 min |
| **Database** | Separate | Included | Managed |
| **Scaling** | Manual | Manual | Automatic |
| **Multi-env** | N/A | Possible | Built-in |
| **Production** | Not recommended | Development | ✓ Recommended |
| **High Availability** | No | No | ✓ Yes |
| **Best Use Case** | Quick dev | Full local env | Production |

---

## 🎯 Recommended Workflows

### For Developers

```bash
# Local development with full services
cd ~/fraud-poc
./install-docker-compose.sh up

# Or simple testing with custom DB
./install-docker.sh build
./install-docker.sh run
```

### For QA/Testing

```bash
# Full environment simulation
./install-docker-compose.sh up

# Test multiple scenarios
./install-docker-compose.sh ps
./install-docker-compose.sh logs
./install-docker-compose.sh shell-api
```

### For DevOps/Production

```bash
# Deploy to development
./install-k8s-helm.sh deploy dev

# Validate before staging
./install-k8s-helm.sh dry-run staging

# Deploy to staging
./install-k8s-helm.sh deploy staging

# Final validation before prod
./install-k8s-helm.sh dry-run prod

# Deploy to production
./install-k8s-helm.sh deploy prod
```

---

## 📞 Support & Debugging

### Collect Debug Information

```bash
# System info
docker --version
docker-compose --version
kubectl version
helm version

# Container logs
docker logs <container-name>
./install-docker-compose.sh logs
./install-k8s-helm.sh logs <environment>

# Resource usage
docker stats
kubectl top pods -n <namespace>
```

### Common Port Issues

| Service | Port | When Using |
|---------|------|-----------|
| API | 8080 | All methods |
| Nginx | 8085 | Docker Compose |
| PostgreSQL | 5432 | Docker Compose, K8s |
| Swagger UI | 8080/8085 | All methods |

---

## 📚 Next Steps

1. **Choose your installation method** based on your use case
2. **Review the specific script** for your method
3. **Configure environment variables** as needed
4. **Run the installation** using the provided script
5. **Verify the deployment** works with provided commands
6. **Refer to troubleshooting** if issues arise

---

## 🔗 Related Documentation

- [ABSOLUTE_PATHS_FIXED.md](./ABSOLUTE_PATHS_FIXED.md) - Path configuration details
- [COMPREHENSIVE_AUDIT_RESULTS.md](./COMPREHENSIVE_AUDIT_RESULTS.md) - Deployment readiness checklist
- [Dockerfile](./Dockerfile) - Container image definition
- [docker-compose.yml](./docker-compose.yml) - Multi-service configuration
- [charts/fraud-poc-api/](./charts/fraud-poc-api/) - Helm chart

---

**Last Updated:** 2026-08-29  
**Version:** 1.0  
**Status:** Production Ready ✓
