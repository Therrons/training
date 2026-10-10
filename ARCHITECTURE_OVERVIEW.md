# Fraud Detection System - Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           EXTERNAL SYSTEMS                              │
├─────────────────────────────────────────────────────────────────────────┤
│  Client Applications  │  Mobile Apps  │  Third-party Services          │
└──────────┬────────────────────────────────────┬──────────────────────────┘
           │                                    │
           └────────────────┬───────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────────────────┐
│                        INGRESS LAYER                                     │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Traefik Ingress Controller (HTTPS, TLS Termination)          │    │
│  │  • Routes traffic to services                                  │    │
│  │  • SSL/TLS certificate handling                                │    │
│  │  • Load balancing                                              │    │
│  └──────────────────────┬─────────────────────────────────────────┘    │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────────────────┐
│                      API GATEWAY LAYER                                   │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Fraud POC API (ASP.NET Core 8.0)                              │    │
│  │  • RESTful endpoints                                            │    │
│  │  • JWT authentication & authorization                          │    │
│  │  • Request validation & transformation                         │    │
│  └──────────────────────┬─────────────────────────────────────────┘    │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│  BUSINESS LOGIC  │ │  DATA ACCESS     │ │  MESSAGING       │
│  (Services)      │ │  (Repository)    │ │  (Kafka/Events)  │
├──────────────────┤ ├──────────────────┤ ├──────────────────┤
│ • Rule Engine    │ │ • Query events   │ │ • Kafka cluster  │
│ • Fraud Scoring  │ │ • Metrics store  │ │ • Event bus      │
│ • Decision Logic │ │ • Cache layer    │ │ • Async processing│
└────────┬─────────┘ └────────┬─────────┘ └────────┬─────────┘
         │                    │                    │
         └────────────────────┼────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────────────────────┐
│                      DATA PERSISTENCE LAYER                               │
│  ┌──────────────────────────────────────┬──────────────────────────────┐  │
│  │  PostgreSQL Database                 │  Cache Layer (optional)      │  │
│  │  ├─ fraud_events                     │  ├─ Redis/In-Memory Cache   │  │
│  │  ├─ fraud_scores                     │  └─ Session store           │  │
│  │  ├─ audit_logs                       │                              │  │
│  │  └─ metrics                          │                              │  │
│  └──────────────────────────────────────┴──────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Details

### 1. **Ingress Layer - Traefik**
**Purpose:** Entry point for all external traffic

**Responsibilities:**
- Route HTTPS traffic to services
- TLS/SSL termination with self-signed certificates
- Load balancing across service replicas
- Request logging and monitoring

**Technology:** Traefik v2.x running in Kubernetes

---

### 2. **API Gateway - Fraud POC API**
**Purpose:** RESTful API for fraud detection requests

**Key Endpoints:**
```
POST   /api/fraud/login                  → Authenticate (JWT token)
POST   /api/fraud/evaluate               → Evaluate transaction for fraud
GET    /api/fraud/rules                  → Get active fraud rules
GET    /api/fraud/events                 → Query transaction events
GET    /health/readiness                 → Liveness probe
GET    /swagger/index.html               → API documentation
```

**Technology:** 
- ASP.NET Core 8.0
- Built-in dependency injection
- Configuration management

---

### 3. **Business Logic Layer**

#### 3.1 **FraudRuleEngine**
Evaluates transactions against configured fraud rules.

**Fraud Rules:**
```
├─ HighAmountRule
│  └─ Triggers when transaction exceeds threshold (default: 50,000)
│
├─ ForeignCnpRule
│  └─ Flags card-not-present transactions from foreign countries
│
├─ AtmWithdrawalLimitRule
│  └─ Triggers when ATM withdrawal exceeds limit (default: 5,000)
│
├─ HighRiskMerchantCategoryRule
│  └─ Flags high-risk merchant categories (gambling, crypto, etc.)
│
└─ RoundAmountRule
   └─ Flags suspicious round-number transactions
```

**Rule Processing:**
1. Load active rules from configuration
2. Evaluate each rule against transaction
3. Collect scores from triggered rules
4. Return aggregated fraud score (0-100+)

#### 3.2 **MetricsService**
Tracks performance and execution metrics.

**Metrics Collected:**
- Rule execution times
- Transaction processing duration
- Fraud score distribution
- Cache hit rates
- Error rates by rule

---

### 4. **Data Access Layer**

#### 4.1 **FraudRepository**
Handles all database interactions.

**Operations:**
```csharp
GetTransactionEventAsync(eventId)
QueryFraudEventsAsync(query, dateFrom, dateTo)
RecordFraudScoreAsync(eventId, score)
GetMetricsAsync()
```

**Database Schema:**
```
fraud_events
├─ id (PK)
├─ transaction_id
├─ amount
├─ country_code
├─ merchant_category
├─ channel (ATM, Online, POS)
└─ timestamp

fraud_scores
├─ id (PK)
├─ event_id (FK)
├─ total_score
├─ triggered_rules (JSON)
└─ created_at

metrics
├─ id (PK)
├─ rule_name
├─ execution_time_ms
├─ triggered_count
└─ timestamp
```

#### 4.2 **Parameter Handling**
Uses Npgsql for type-safe parameter binding:
- Null values converted to `DBNull.Value`
- Prevents SQL injection
- Automatic type conversion

---

### 5. **Authentication Layer**

#### JWT Token Flow
```
1. Client sends credentials
   └─> POST /api/fraud/login {username, password}

2. AuthenticationService validates
   └─> Check against configured credentials (API_USERNAME, API_PASSWORD)

3. JwtService generates token
   └─> Payload: {sub, username, iat, exp}
   └─> Signed with secret key

4. Token returned to client
   └─> Client includes in Authorization header for subsequent requests

5. Middleware validates token
   └─> Check signature, expiration, issuer, audience
   └─> Grant or deny access
```

**JWT Configuration:**
```json
{
  "JWT": {
    "Secret": "your-super-duper-secret-key-of-min-32-characters-long",
    "Issuer": "fraud-poc-api",
    "Audience": "fraud-poc-api-client",
    "ExpirationMinutes": 60
  }
}
```

---

### 6. **Database Layer - PostgreSQL**

**Purpose:** Persistent storage for transactions, scores, and metrics

**Connection String:**
```
Host=localhost;Port=5432;Database=fraud_db;Username=fraud_user;Password=fraud_pass
```

**Key Tables:**
- `fraud_events` - Transaction events to evaluate
- `fraud_scores` - Calculated fraud scores and triggered rules
- `metrics` - Performance metrics and statistics

---

### 7. **Messaging Layer - Kafka** (Optional)

**Purpose:** Asynchronous event processing

**Topics:**
- `fraud-events` - New transactions to process
- `fraud-scores` - Calculated fraud scores
- `alerts` - High-risk transaction alerts

**Flow:**
```
1. Transaction received
   └─> Published to fraud-events topic

2. Workers consume events
   └─> Process through fraud engine
   └─> Calculate score

3. Score published
   └─> Publish to fraud-scores topic

4. Downstream systems consume
   └─> Update dashboards
   └─> Send alerts
   └─> Archive to data warehouse
```

---

## Data Flow - Transaction Evaluation

### Request Flow
```
1. Client Authentication
   ├─ POST /api/fraud/login
   └─ Receive JWT token

2. Transaction Submission
   ├─ POST /api/fraud/evaluate
   │  └─ Body: {transactionId, amount, country, merchant, channel}
   ├─ Validate request
   └─ Authenticate with JWT

3. Business Logic Processing
   ├─ Load fraud rules
   ├─ Evaluate each rule
   │  ├─ HighAmountRule
   │  ├─ ForeignCnpRule
   │  ├─ AtmWithdrawalLimitRule
   │  ├─ HighRiskMerchantRule
   │  └─ RoundAmountRule
   ├─ Aggregate scores
   └─ Store result

4. Data Persistence
   ├─ Save to fraud_events table
   ├─ Save to fraud_scores table
   ├─ Update metrics
   └─ Optional: Publish to Kafka

5. Response
   ├─ Return fraud score (0-100+)
   ├─ Return triggered rules
   └─ HTTP 200 OK
```

---

## Deployment Architecture

### Local Development
```
Developer Machine
├─ .NET CLI (dotnet run)
├─ PostgreSQL (Docker)
├─ Traefik (Docker)
└─ Application runs on localhost:5000
```

### Kubernetes Deployment (Rancher Desktop)
```
Rancher Desktop Kubernetes Cluster
│
├─ Namespace: fraud-poc-api
│  │
│  ├─ Deployment: fraud-poc-api
│  │  └─ Pod (Replicas: 1-3)
│  │     └─ Container: fraud-poc-api (port 8083)
│  │
│  ├─ Service: fraud-poc-api
│  │  └─ ClusterIP service (internal routing)
│  │
│  ├─ Ingress: fraud-poc-api-ingress
│  │  └─ Routes HTTPS traffic via Traefik
│  │
│  └─ ConfigMap: fraud-poc-config
│     └─ Configuration, environment variables
│
├─ Namespace: kube-system
│  └─ Traefik Ingress Controller
│     └─ Routes external HTTPS to services
│
└─ PostgreSQL (External or Containerized)
   └─ Persistent storage
```

---

## Integration Points

### 1. **External Systems → API**
- Mobile apps
- Web clients
- Third-party fraud systems
- Monitoring dashboards

### 2. **API → Database**
- Read transaction events
- Write fraud scores
- Query metrics
- Audit logging

### 3. **API → Messaging** (Optional)
- Publish fraud scores to Kafka
- Consume fraud events
- Downstream processing

### 4. **API → Authentication**
- JWT token generation
- Token validation
- User credential verification

---

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET Core | 8.0 |
| **Framework** | ASP.NET Core | 8.0 |
| **Testing** | xUnit | 2.9.3 |
| **Mocking** | Moq | 4.21.0 |
| **Assertions** | FluentAssertions | 8.11.0 |
| **Database** | PostgreSQL | 14+ |
| **Driver** | Npgsql | 8.0.3 |
| **Messaging** | Kafka/Confluent | 2.10.0 |
| **Ingress** | Traefik | 2.x |
| **Orchestration** | Kubernetes | 1.27+ |
| **Container Runtime** | Docker | 20.10+ |
| **Development Tool** | Rancher Desktop | Latest |

---

## Configuration Management

**Configuration Priority** (highest to lowest):
```
1. Environment Variables
2. appsettings.LOC.json (local)
3. appsettings.json (default)
4. Command-line arguments
5. In-memory defaults
```

**Example Environment Variables:**
```powershell
$env:DB_HOST = "localhost"
$env:DB_PORT = "5432"
$env:DB_NAME = "fraud_db"
$env:DB_USER = "fraud_user"
$env:DB_PASSWORD = "fraud_pass"
$env:API_USERNAME = "fraud-analyst"
$env:API_PASSWORD = "SecurePass123!"
$env:JWT_SECRET = "your-super-duper-secret-key-of-min-32-characters-long"
```

---

## Security Architecture

### Authentication
- JWT token-based authentication
- Configurable expiration (default: 60 minutes)
- Token signature verification
- Issuer and audience validation

### Authorization
- Role-based access control (configurable)
- Endpoint-level protection via `[Authorize]` attributes
- Scope validation for API operations

### Data Protection
- Database user credentials via environment variables
- TLS/SSL for data in transit (Traefik)
- Password hashing (when persisted)
- Audit logging for fraud score changes

---

## Scalability Considerations

### Horizontal Scaling
- Kubernetes deployments support multiple replicas
- Traefik load-balances across replicas
- Stateless API design enables auto-scaling

### Vertical Scaling
- .NET Core optimized for multi-core systems
- Configurable thread pools
- Memory-efficient fraud rule evaluation

### Database Scaling
- Connection pooling (Npgsql)
- Query optimization with indexes
- Optional caching layer for read-heavy operations

---

## Monitoring & Observability

**Health Checks:**
- `/health/readiness` - Service is ready to receive traffic
- Kubernetes uses for liveness/readiness probes

**Metrics Collection:**
- Rule execution times
- Transaction processing duration
- Error rates and exceptions
- Database query performance

**Logging:**
- Structured logging via ILogger
- Configurable log levels by namespace
- Integration with ELK stack (optional)

---

## Error Handling

**Error Flow:**
```
1. Invalid Request
   └─ 400 Bad Request (model validation fails)

2. Authentication Failure
   └─ 401 Unauthorized (invalid credentials or token)

3. Authorization Failure
   └─ 403 Forbidden (insufficient permissions)

4. Resource Not Found
   └─ 404 Not Found (event/rule not found)

5. Database Error
   └─ 500 Internal Server Error (logs details internally)

6. Unhandled Exception
   └─ 500 Internal Server Error (generic response)
```

---

## Future Enhancements

- [ ] Machine learning model for dynamic fraud scoring
- [ ] Real-time dashboard with WebSocket updates
- [ ] Advanced caching with Redis
- [ ] Event sourcing for audit trail
- [ ] GraphQL API support
- [ ] API rate limiting and throttling
- [ ] Multi-tenancy support
- [ ] Distributed tracing (OpenTelemetry)
