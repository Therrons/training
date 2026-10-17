# Cross-Pod Metrics Aggregation - Configuration Guide

## Overview
Solution 1: Cross-Pod Aggregation uses a simple, configuration-based approach to discover and query pod endpoints. No Kubernetes API client or special permissions required.

## Three Discovery Modes

### Mode 1: LOCAL (Default)
**Best for**: Local development and testing

**How it works**:
- Queries only `localhost:8080`
- No configuration needed
- Single pod metrics returned as aggregated result
- Perfect for testing aggregation logic locally

**Configuration**:
```
MetricsAggregation:DiscoveryMode = "local"
```

**Environment Variable**:
```
DISCOVERY_MODE=local
```

**Example Flow**:
```
GET http://localhost:8080/api/metrics/aggregated
  ↓
Discovers pods: [localhost:8080]
  ↓
Queries: http://localhost:8080/api/metrics/static
  ↓
Returns single pod metrics as aggregated result
```

---

### Mode 2: SERVICE (Kubernetes Service DNS)
**Best for**: Kubernetes deployments with a service

**How it works**:
- Uses Kubernetes Service DNS: `<service-name>.<namespace>.svc.cluster.local`
- Service automatically load-balances across all pod replicas
- Kubernetes handles pod discovery internally
- Simple, requires only service name

**Configuration**:
```
MetricsAggregation:DiscoveryMode = "service"
MetricsAggregation:ServiceName = "fraud-api-service"
MetricsAggregation:PodPort = "8080"
```

**Environment Variables**:
```
DISCOVERY_MODE=service
FRAUD_API_SERVICE_NAME=fraud-api-service
METRICS_POD_PORT=8080
```

**Kubernetes Setup**:

**service.yaml**:
```yaml
apiVersion: v1
kind: Service
metadata:
  name: fraud-api-service
  namespace: default
spec:
  selector:
    app: fraud-poc-api
  ports:
  - name: http
    port: 8080
    targetPort: 8080
  type: ClusterIP
```

**Example Flow** (with 3 pod replicas):
```
GET http://rancher-desktop:8083/api/metrics/aggregated
  ↓
Discovers pods: [fraud-api-service.default.svc.cluster.local:8080]
  ↓
Queries: http://fraud-api-service.default.svc.cluster.local:8080/api/metrics/static
  ↓
Kubernetes service load-balances across 3 pods
  ↓
Pod 1 responds with its metrics
  ↓
Returns single query result (represents all pods through LB)
```

**Deploy Service**:
```bash
kubectl apply -f service.yaml

# Verify service
kubectl get service fraud-api-service
kubectl get endpoints fraud-api-service
```

---

### Mode 3: HARDCODED (Explicit Pod Endpoints)
**Best for**: Static deployments or explicit control

**How it works**:
- Explicitly list all pod endpoints in configuration
- Queries each pod individually and aggregates results
- Enables per-pod metrics visibility
- Most control, but requires manual maintenance

**Configuration Format**:
```
podName:podIP:port,podName:podIP:port,...
```

**Configuration**:
```
MetricsAggregation:DiscoveryMode = "hardcoded"
MetricsAggregation:PodEndpoints = "fraud-api-pod-1:10.42.0.10:8080,fraud-api-pod-2:10.42.0.20:8080,fraud-api-pod-3:10.42.0.25:8080"
```

**Environment Variable**:
```
DISCOVERY_MODE=hardcoded
POD_ENDPOINTS=fraud-api-pod-1:10.42.0.10:8080,fraud-api-pod-2:10.42.0.20:8080,fraud-api-pod-3:10.42.0.25:8080
```

**Example Flow** (with 3 hardcoded pods):
```
GET http://rancher-desktop:8083/api/metrics/aggregated
  ↓
Discovers pods: [
  fraud-api-pod-1:10.42.0.10:8080,
  fraud-api-pod-2:10.42.0.20:8080,
  fraud-api-pod-3:10.42.0.25:8080
]
  ↓
Queries in parallel:
  - http://10.42.0.10:8080/api/metrics/static
  - http://10.42.0.20:8080/api/metrics/static
  - http://10.42.0.25:8080/api/metrics/static
  ↓
Aggregates all 3 responses:
  - Sum transactions
  - Average processing times
  - Combine rule metrics
  ↓
Returns AggregatedMetrics
```

**Getting Pod IPs in Kubernetes**:
```bash
# List all fraud-api pods with their IPs
kubectl get pods -o wide -l app=fraud-poc-api

# Example output:
# NAME                    READY   STATUS    RESTARTS   AGE   IP           NODE
# fraud-api-6f4d7-abc12   1/1     Running   0          2m    10.42.0.10   rancher-desktop
# fraud-api-6f4d7-def45   1/1     Running   0          2m    10.42.0.20   rancher-desktop
# fraud-api-6f4d7-ghi78   1/1     Running   0          2m    10.42.0.25   rancher-desktop
```

---

## Configuration Methods

### 1. appsettings.json (Recommended for Production)
```json
{
  "MetricsAggregation": {
    "DiscoveryMode": "service",
    "ServiceName": "fraud-api-service",
    "PodPort": 8080,
    "PodEndpoints": "fraud-api-pod-1:localhost:8080"
  }
}
```

### 2. appsettings.Kubernetes.json (Environment-Specific)
```json
{
  "MetricsAggregation": {
    "DiscoveryMode": "hardcoded",
    "PodEndpoints": "fraud-api-pod-1:10.42.0.10:8080,fraud-api-pod-2:10.42.0.20:8080,fraud-api-pod-3:10.42.0.25:8080"
  }
}
```

### 3. Environment Variables (Container/Docker Compose)
```bash
# Local
export DISCOVERY_MODE=local

# Kubernetes with service
export DISCOVERY_MODE=service
export FRAUD_API_SERVICE_NAME=fraud-api-service

# Kubernetes with hardcoded endpoints
export DISCOVERY_MODE=hardcoded
export POD_ENDPOINTS=fraud-api-pod-1:10.42.0.10:8080,fraud-api-pod-2:10.42.0.20:8080
```

### 4. Docker Compose (Using Service Name)
```yaml
version: '3.8'
services:
  fraud-api-1:
    image: fraud-poc-api:latest
    environment:
      DISCOVERY_MODE: hardcoded
      POD_ENDPOINTS: "fraud-api-1:fraud-api-1:8080,fraud-api-2:fraud-api-2:8080,fraud-api-3:fraud-api-3:8080"
    ports:
      - "8081:8080"

  fraud-api-2:
    image: fraud-poc-api:latest
    environment:
      DISCOVERY_MODE: hardcoded
      POD_ENDPOINTS: "fraud-api-1:fraud-api-1:8080,fraud-api-2:fraud-api-2:8080,fraud-api-3:fraud-api-3:8080"
    ports:
      - "8082:8080"

  fraud-api-3:
    image: fraud-poc-api:latest
    environment:
      DISCOVERY_MODE: hardcoded
      POD_ENDPOINTS: "fraud-api-1:fraud-api-1:8080,fraud-api-2:fraud-api-2:8080,fraud-api-3:fraud-api-3:8080"
    ports:
      - "8083:8080"
```

---

## Testing the Three Modes

### Test Local Mode
```bash
# Start application locally
dotnet run --environment=Development

# In another terminal, test aggregation
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/api/metrics/aggregated

# Should return single pod metrics
```

### Test Service Mode (Rancher Desktop)
```bash
# Apply service
kubectl apply -f service.yaml

# Deploy 3 replicas
kubectl set env deployment/fraud-api DISCOVERY_MODE=service

# Test aggregation
curl -H "Authorization: Bearer $TOKEN" \
  http://rancher-desktop:8083/api/metrics/aggregated

# Verify service is routing to all pods
kubectl get endpoints fraud-api-service -w
```

### Test Hardcoded Mode (Rancher Desktop)
```bash
# Get pod IPs
kubectl get pods -o wide -l app=fraud-poc-api

# Create environment variable with pod IPs (example)
export POD_ENDPOINTS="fraud-api-pod-1:10.42.0.10:8080,fraud-api-pod-2:10.42.0.20:8080,fraud-api-pod-3:10.42.0.25:8080"

# Deploy with hardcoded mode
kubectl set env deployment/fraud-api \
  DISCOVERY_MODE=hardcoded \
  POD_ENDPOINTS="$POD_ENDPOINTS"

# Test aggregation
curl -H "Authorization: Bearer $TOKEN" \
  http://rancher-desktop:8083/api/metrics/aggregated

# Check per-pod metrics
curl -H "Authorization: Bearer $TOKEN" \
  http://rancher-desktop:8083/api/metrics/per-pod
```

---

## Response Examples

### Aggregated Metrics Response
```json
{
  "aggregatedAt": "2026-10-14T10:30:00Z",
  "totalPods": 3,
  "healthyPods": 3,
  "unhealthyPods": 0,
  "totalTransactions": 1500,
  "flaggedTransactions": 45,
  "averageFraudRate": 0.03,
  "averageFraudScore": 25.5,
  "averageProcessingTime": 25,
  "maxProcessingTime": 150,
  "minProcessingTime": 5,
  "averageP95ProcessingTime": 80,
  "averageP99ProcessingTime": 120,
  "totalAuthenticationAttempts": 300,
  "totalSuccessfulAuthentications": 290,
  "averageAuthenticationSuccessRate": 0.967,
  "totalRulesExecuted": 5,
  "ruleMetrics": {
    "HighAmountRule": {
      "ruleCode": "HighAmountRule",
      "totalExecutionCount": 450,
      "totalTriggeredCount": 20,
      "averageExecutionTime": 2,
      "averageTriggeredRate": 0.044
    }
  },
  "podMetrics": [...]
}
```

### Per-Pod Metrics Response
```json
{
  "timestamp": "2026-10-14T10:30:00Z",
  "podCount": 3,
  "healthyPods": 3,
  "pods": [
    {
      "name": "fraud-api-pod-1",
      "status": "Running",
      "isHealthy": true,
      "responseTimeMs": 45,
      "errorMessage": null,
      "metrics": {
        "totalTransactions": 500,
        "flaggedTransactions": 15,
        "fraudRate": 0.03,
        "avgProcessingTime": 25
      }
    },
    {
      "name": "fraud-api-pod-2",
      "status": "Running",
      "isHealthy": true,
      "responseTimeMs": 42,
      "errorMessage": null,
      "metrics": {
        "totalTransactions": 500,
        "flaggedTransactions": 15,
        "fraudRate": 0.03,
        "avgProcessingTime": 24
      }
    },
    {
      "name": "fraud-api-pod-3",
      "status": "Running",
      "isHealthy": true,
      "responseTimeMs": 48,
      "errorMessage": null,
      "metrics": {
        "totalTransactions": 500,
        "flaggedTransactions": 15,
        "fraudRate": 0.03,
        "avgProcessingTime": 26
      }
    }
  ]
}
```

---

## Troubleshooting

### Issue: "No valid pod endpoints configured, falling back to localhost"
**Cause**: POD_ENDPOINTS not set or malformed
**Solution**:
```bash
# Verify format
echo $POD_ENDPOINTS
# Should be: pod1:ip1:port,pod2:ip2:port,pod3:ip3:port

# Set correctly
export POD_ENDPOINTS="fraud-api-1:10.42.0.10:8080,fraud-api-2:10.42.0.20:8080"
```

### Issue: Service mode returns empty metrics
**Cause**: Service not routing to pods or pods not responding
**Solution**:
```bash
# Check service endpoints
kubectl get endpoints fraud-api-service

# Test service connectivity
kubectl exec -it <pod-name> -- curl http://fraud-api-service:8080/api/metrics/static

# Check pod logs
kubectl logs -f deployment/fraud-api
```

### Issue: Hardcoded mode shows unhealthy pods
**Cause**: Pod IP addresses changed or pods not running
**Solution**:
```bash
# Get fresh pod IPs
kubectl get pods -o wide -l app=fraud-poc-api

# Update POD_ENDPOINTS with new IPs
# Redeploy application
```

### Issue: Aggregation timeout (30s default)
**Cause**: Slow pod responses or network latency
**Solution**: Increase timeout in MetricsAggregatorService or reduce pod count

---

## Performance Considerations

| Mode | Discovery Time | Query Time | Per-Pod Visibility | Best For |
|------|----------------|------------|-------------------|----------|
| LOCAL | <1ms | ~20ms | No | Development |
| SERVICE | <1ms | ~50ms | No | Kubernetes (simple) |
| HARDCODED | <1ms | ~100ms (parallel) | Yes | Kubernetes (detailed) |

---

## Migration Path

1. **Start with LOCAL** for development and testing
2. **Move to SERVICE** when deploying to Kubernetes with standard service
3. **Use HARDCODED** if you need per-pod metrics visibility or explicit control

All three modes work with the same `MetricsAggregatorService` - just change configuration!
