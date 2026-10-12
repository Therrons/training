const { Document, Packer, Paragraph, TextRun, HeadingLevel } = require('docx');
const fs = require('fs');

// Architecture Document
const archDoc = new Document({
  sections: [{
    children: [
      new Paragraph({
        text: "FRAUD DETECTION SYSTEM",
        heading: HeadingLevel.HEADING_1,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "Architectural Overview",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 200 }
      }),
      new Paragraph({
        text: "Version: 1.0 | Date: October 2026 | Author: Therron Schlemmer",
        spacing: { after: 400 }
      }),

      new Paragraph({
        text: "EXECUTIVE SUMMARY",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The Fraud Detection System is an enterprise-grade, event-driven application built with .NET 8.0 and C# 12 that processes financial transaction events in real-time to identify and flag potential fraudulent activity. The system combines a configurable multi-rule evaluation engine with a distributed architecture supporting high throughput (>300K transactions/sec), low latency (<1ms), and comprehensive audit trails.",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "SYSTEM ARCHITECTURE",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The system is organized into four primary layers:",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "1. API/Presentation Layer (fraud_poc_project_ui)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "REST endpoints, JWT authentication, middleware, Kafka consumer, health checks, CORS configuration",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "2. Business Logic Layer (fraud_poc_project_buss)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "FraudEvaluationService orchestrates fraud detection. Fraud rules implement IFraudRule interface. HighAmountRule, ForeignCnpRule, AtmWithdrawalLimitRule, RoundAmountRule, HighRiskMerchantCategoryRule. Scoring aggregation with cap/threshold enforcement.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "3. Data Access Layer (fraud_poc_project_repo)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "FraudRepository implements IFraudRepository. Repository pattern abstracts database operations. Connection pooling via DbConnection. Parameterized queries for SQL injection prevention. Stored procedures for batch operations.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "4. Infrastructure Layer",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "PostgreSQL with ACID transactions and stored procedures. Apache Kafka for event streaming. Configuration from appsettings.json with environment overrides.",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "TECHNOLOGY STACK",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Core Framework: .NET 8.0 with C# 12 (async/await, nullable reference types)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Web: ASP.NET Core 8 REST API",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Messaging: Apache Kafka (pub/sub topics, consumer groups, partitioning)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Database: PostgreSQL with connection pooling and stored procedures",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "DI & Configuration: Microsoft.Extensions.DependencyInjection, IOptions<T>",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Validation: FluentValidation for input validation and business rules",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Logging: Serilog with structured JSON enrichment",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Testing: xUnit with Fluent Assertions, integration tests, load/stress tests",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "CORE COMPONENTS",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "FraudEvaluationService: Orchestrates fraud detection by receiving TransactionEvent, invoking all configured fraud rules, aggregating rule scores additively, capping score at MaxScore (100), comparing against FlagThreshold (40) to determine if flagged, returning FraudEventRecord with score, flag status, and triggered rules.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Fraud Rules (Strategy Pattern):",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "HighAmountRule: Flags transactions exceeding configured threshold (Score: +40)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "ForeignCnpRule: Flags CNP/Online transactions from countries outside home country (Score: +35)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "AtmWithdrawalLimitRule: Flags ATM withdrawals exceeding single-transaction limit (Score: +30)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "RoundAmountRule: Flags large round-number transactions as fraud indicator (Score: +10)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "HighRiskMerchantCategoryRule: Flags transactions in high-risk categories (Score: +25)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "All rules can be independently enabled/disabled via configuration without code changes.",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "CONFIGURATION & PERFORMANCE",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Configuration Management: Runtime configuration loaded from (priority): Environment variables, Kubernetes ConfigMaps/Secrets, appsettings.{Environment}.json, appsettings.json",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Performance Targets:",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Throughput: >300K transactions/sec (single instance)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "P95 Latency: <1ms per transaction",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Memory: <10 MB per 1M transactions (stable growth)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "CPU: Linear with throughput (~1 core per 100K tx/sec)",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "SECURITY & TESTING",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Security: JWT authentication, FluentValidation input validation, XSS protection middleware, parameterized SQL queries, CORS configuration, correlation ID propagation for audit trail.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Testing Strategy:",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Unit Tests: Individual fraud rule behavior with xUnit and Fluent Assertions",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Integration Tests: Service orchestration, rule composition, score aggregation",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Load Tests: Throughput, latency percentiles, memory stability measurements",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Stress Tests: Increasing load, sustained load, spike scenarios",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "API Integration Tests: REST endpoints, authentication, error handling",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "MONITORING & OBSERVABILITY",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Structured Logging: Serilog with correlation IDs, timestamps, structured fields",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Metrics Collection: Authentication attempts, fraud scoring percentiles, API latency percentiles",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Distributed Tracing: Correlation ID propagation through requests and Kafka messages",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Health Checks: Database connectivity, Kafka broker availability",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "CONCLUSION",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The Fraud Detection System demonstrates enterprise-grade architecture: separation of concerns, dependency injection, strategy pattern for extensibility, event-driven processing for scalability, comprehensive testing, and production-ready observability. Supports both synchronous REST API and asynchronous Kafka-based event processing for integration into diverse transaction pipelines at scale."
      })
    ]
  }]
});

Packer.toBuffer(archDoc).then(buffer => {
  fs.writeFileSync('/sessions/zen-blissful-johnson/mnt/training/Fraud_Detection_System_Architecture_Overview.docx', buffer);
  console.log('Architecture document created successfully');
});

// Workflow Document
const workflowDoc = new Document({
  sections: [{
    children: [
      new Paragraph({
        text: "FRAUD DETECTION SYSTEM",
        heading: HeadingLevel.HEADING_1,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "Workflow & Process Documentation",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 200 }
      }),
      new Paragraph({
        text: "Version: 1.0 | Date: October 2026 | Author: Therron Schlemmer",
        spacing: { after: 400 }
      }),

      new Paragraph({
        text: "OVERVIEW",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "Two distinct transaction processing workflows:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. Synchronous REST API: Direct client-to-service evaluation for immediate fraud decisions",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. Asynchronous Kafka: Event-driven evaluation with audit trail for batch processing",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "WORKFLOW 1: SYNCHRONOUS REST API EVALUATION",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Use Cases: Real-time fraud scoring at point of transaction, immediate decision required (POS authorization), client expects instant response, legacy system integration.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Step-by-Step Flow:",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),

      new Paragraph({
        text: "1. Client sends POST /api/fraud/evaluate with TransactionEvent JSON and CorrelationId header",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. CorrelationIdMiddleware extracts or generates ID and enriches logging context",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. FraudController authenticates using JWT bearer token",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. CheckForXssMiddleware validates JSON payload for malicious scripts",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "5. FluentValidation validates required fields, data types, business rules",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "6. FraudEvaluationService.Evaluate() orchestrates all fraud rules",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "7. Each rule evaluates transaction, returns IsTriggered and ScoreContribution",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "8. Aggregate scores: sum, cap at MaxScore (100), compare to threshold (40)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "9. FraudRepository persists FraudEventRecord to PostgreSQL via parameterized queries",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "10. Return FraudEvaluationResult: HTTP 200 if not flagged, 202 if flagged",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Example Response: HTTP 202 with fraudScore: 75, flaggedReason: HIGH_AMOUNT,FOREIGN_CNP, detailed rule results array",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Error Handling: 400 Bad Request (validation failure), 401 Unauthorized (missing JWT), 403 Forbidden (no permission), 500 Internal Server Error (database failure logged with CorrelationId)",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "WORKFLOW 2: ASYNCHRONOUS KAFKA EVENT-DRIVEN EVALUATION",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Use Cases: Upstream systems publish events, compliance audit trail required, batch/historical analysis, multiple downstream consumers, decouple source and evaluation.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Three Phases:",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),

      new Paragraph({
        text: "INGESTION:",
        heading: HeadingLevel.HEADING_4,
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Upstream publishes TransactionEvent to Kafka 'transactions' topic with CorrelationId header. FraudConsumerWorker subscribes as consumer group member.",
        spacing: { after: 50 }
      }),

      new Paragraph({
        text: "PROCESSING:",
        heading: HeadingLevel.HEADING_4,
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "FraudConsumer deserializes message, extracts CorrelationId. FraudEvaluationService.EvaluateAsync() scores transaction. All fraud rules evaluate. FraudRepository persists to PostgreSQL.",
        spacing: { after: 50 }
      }),

      new Paragraph({
        text: "PUBLICATION:",
        heading: HeadingLevel.HEADING_4,
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "If IsFlagged=true, FraudProducer publishes to Kafka 'fraud-alerts' topic. Downstream systems consume and respond (ML, case management, alerts).",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Scalability: Consumer group distributes partitions across instances (1:1). Partition key = customerId preserves order. Offset tracking enables crash recovery.",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "FRAUD RULE EVALUATION",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "All Rules Evaluated: HighAmountRule, ForeignCnpRule, AtmWithdrawalLimitRule, RoundAmountRule, HighRiskMerchantCategoryRule",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Scoring Example (75K CNP purchase from US, ZA customer, retail merchant):",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "HighAmount: 75000 > 50000 → Triggered +40",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "ForeignCnp: CNP+US != ZA → Triggered +35",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "AtmWithdrawal: Not ATM → Not triggered +0",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "RoundAmount: Complex logic → Not triggered +0",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "HighRiskMerchant: retail not in list → Not triggered +0",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Total: 40+35=75, Capped=75, IsFlagged: 75>=40 → TRUE, Reason: HIGH_AMOUNT,FOREIGN_CNP",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "DATABASE & CONNECTION MANAGEMENT",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Persistence: FraudEventRecord written to fraud_events table (fraud_event_id, customer_id, account_id, amount, fraud_score, is_flagged, triggered_rules, evaluated_at, correlation_id)",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Connection Pooling: DbConnection manages PostgreSQL pool (10 min, 20 max). Reuses connections to reduce TCP overhead. Timeout (30s) prevents exhaustion. Auto-return on Dispose().",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "MONITORING & OBSERVABILITY",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Correlation ID: CorrelationIdMiddleware inspects/generates X-Correlation-Id header. Serilog enriches all logs. Kafka propagates in message headers. Log aggregation traces end-to-end.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Structured Logging: Serilog produces JSON with timestamp, level, message, correlation-id, customer-id, fraud-score, is-flagged, duration-ms",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Metrics: MetricsService tracks authentication, fraud scoring percentiles, API latency (P50/P95/P99), database timing, Kafka throughput/lag",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Health Checks: GET /health validates PostgreSQL pool and Kafka broker. K8s restarts on failure.",
        spacing: { after: 200 }
      }),

      new Paragraph({
        text: "SUMMARY",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "Two workflows converge on same FraudEvaluationService ensuring consistent rule application. Synchronous for immediate decisions, asynchronous for batch/audit. Comprehensive logging/tracing with correlation IDs. Connection pooling and Kafka consumer groups enable millions of transactions daily at scale."
      })
    ]
  }]
});

Packer.toBuffer(workflowDoc).then(buffer => {
  fs.writeFileSync('/sessions/zen-blissful-johnson/mnt/training/Fraud_Detection_System_Workflow_Documentation.docx', buffer);
  console.log('Workflow document created successfully');
});

setTimeout(() => {
  console.log('Both documents generated. Exiting.');
  process.exit(0);
}, 1000);
