const { Document, Packer, Paragraph, TextRun, HeadingLevel, Table, TableRow, TableCell, WidthType, BorderStyle, PageBreak } = require('docx');
const fs = require('fs');

const doc = new Document({
  sections: [{
    children: [
      // Title
      new Paragraph({
        text: "Fraud Detection System",
        heading: HeadingLevel.HEADING_1,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "Architectural Overview",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 200 }
      }),

      // Document metadata
      new Paragraph({
        text: "Version: 1.0 | Date: October 2026 | Author: Therron Schlemmer",
        spacing: { after: 400 }
      }),

      // Executive Summary
      new Paragraph({
        text: "Executive Summary",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The Fraud Detection System is a enterprise-grade, event-driven application built with .NET 8.0 and C# 12 that processes financial transaction events in real-time to identify and flag potential fraudulent activity. The system combines a configurable multi-rule evaluation engine with a distributed architecture supporting high throughput (>300K transactions/sec), low latency (<1ms), and comprehensive audit trails.",
        spacing: { after: 200 }
      }),

      // System Overview
      new Paragraph({
        text: "System Overview",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The system is organized into four primary layers:",
        spacing: { after: 100 }
      }),

      new Table({
        width: { size: 100, type: WidthType.PERCENTAGE },
        rows: [
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Layer")], width: { size: 25, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Component")], width: { size: 25, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Purpose")], width: { size: 50, type: WidthType.PERCENTAGE } })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("API/Presentation")] }),
              new TableCell({ children: [new Paragraph("fraud_poc_project_ui")] }),
              new TableCell({ children: [new Paragraph("REST endpoints, authentication, middleware, Kafka consumer")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Business Logic")] }),
              new TableCell({ children: [new Paragraph("fraud_poc_project_buss")] }),
              new TableCell({ children: [new Paragraph("Fraud evaluation service, rule engine, scoring logic")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Data Access")] }),
              new TableCell({ children: [new Paragraph("fraud_poc_project_repo")] }),
              new TableCell({ children: [new Paragraph("Repository pattern, database queries, Kafka producer")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Infrastructure")] }),
              new TableCell({ children: [new Paragraph("PostgreSQL, Kafka, Configuration")] }),
              new TableCell({ children: [new Paragraph("Persistent storage, event streaming, runtime settings")] })
            ]
          })
        ]
      }),
      new Paragraph({ text: "", spacing: { after: 200 } }),

      // Technology Stack
      new Paragraph({
        text: "Technology Stack",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "Core Framework & Languages",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: ".NET 8.0 with C# 12 (async/await patterns, nullable reference types, records)",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "ASP.NET Core 8 for REST API",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Messaging & Streaming",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Apache Kafka: Event-driven transaction processing with topic-based pub/sub model",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Consumer groups for horizontal scaling and fault tolerance",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Data Storage",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "PostgreSQL: Transactional ACID database with stored procedures for batch operations",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Connection pooling for optimal resource utilization",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Dependency Injection & Configuration",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Microsoft Extensions DI (IServiceCollection, IOptions<T>)",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Configuration from appsettings.json with environment-specific overrides",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Validation & Data Mapping",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "FluentValidation for input validation and business rule enforcement",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "AutoMapper for DTO-to-Domain mappings",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Testing & Observability",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "xUnit with Fluent Assertions for unit testing",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Serilog for structured logging and event enrichment",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Correlation IDs for distributed tracing across service boundaries",
        spacing: { after: 200 }
      }),

      // Architectural Patterns
      new Paragraph({
        text: "Architectural Patterns",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Strategy Pattern (Fraud Rule Engine)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Each fraud rule implements IFraudRule interface with strategy for evaluating transactions. Rules are composed and injected into FraudEvaluationService, allowing dynamic rule addition/removal without modifying core evaluation logic.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Repository Pattern (Data Access)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "IFraudRepository abstracts database operations. Implementation handles connection pooling, query optimization, and stored procedure invocation. Enables testing with mock repositories.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Dependency Injection Pattern",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Services registered at startup via IServiceCollection. Runtime configuration loaded from IOptions<T> into service constructors. Enables loose coupling and testability.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Event-Driven Architecture",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Transactions arrive as Kafka events consumed by FraudConsumer. Evaluated asynchronously, flagged events published to fraud topic. Enables real-time processing and audit trail.",
        spacing: { after: 200 }
      }),

      // Core Components
      new Paragraph({
        text: "Core Components",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "1. FraudEvaluationService (Business Layer)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Orchestrates fraud detection by:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Receiving a TransactionEvent",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Invoking all configured fraud rules (HighAmountRule, ForeignCnpRule, AtmWithdrawalLimitRule, RoundAmountRule, HighRiskMerchantCategoryRule)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Aggregating rule scores (additive)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Capping score at configured MaxScore (typically 100)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Comparing against FlagThreshold to determine if flagged",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Returning FraudEventRecord with score, flag status, and triggered rules",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "2. Fraud Rules (Strategy Implementations)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "HighAmountRule: Flags transactions exceeding configured threshold. Configuration-driven threshold supports multiple jurisdictions/risk profiles.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "ForeignCnpRule: Flags card-not-present (CNP or Online) transactions from countries outside configured home country. Case-insensitive comparison.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "AtmWithdrawalLimitRule: Flags ATM cash withdrawals (identified by transaction type or channel) exceeding configured single-withdrawal limit.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "RoundAmountRule: Flags large, round-number transactions (multiples of configured roundness multiple, minimum amount threshold) as common fraud indicator.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "HighRiskMerchantCategoryRule: Flags transactions in high-risk categories (configured as comma-separated list: gambling, casino, crypto) which correlate with fraud.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "All rules can be independently enabled/disabled via configuration without code changes. Disabled rules contribute zero score.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "3. FraudRepository (Data Access Layer)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Implements IFraudRepository interface. Manages:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Connection pooling via DbConnection wrapper",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Parameterized queries to prevent SQL injection",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Stored procedures for batch inserts of flagged fraud records",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Query result mapping via FraudRecordMapper",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Exception translation (FraudRepositoryException)",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "4. FraudProducer (Kafka Integration)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Publishes flagged transactions to Kafka fraud topic. Implements IFraudProducer with:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Idempotent producer configuration (deduplication support)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Partitioning strategy (by customer ID for order preservation)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • JSON serialization of fraud events",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Correlation ID propagation for tracing",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "5. FraudConsumer & FraudConsumerWorker (Background Processing)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Hosted service subscribing to transaction topic. For each transaction event:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Deserializes Kafka message to TransactionEvent",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Invokes FraudEvaluationService.Evaluate()",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Persists result to database via FraudRepository",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Publishes flagged event to fraud Kafka topic for downstream systems",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Implements consumer group strategy for horizontal scaling",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "6. FraudController (REST API)",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Exposes endpoints for fraud management:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • POST /api/fraud/login: Authenticate with JWT token generation",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • GET /api/fraud/evaluate: Evaluate a transaction synchronously",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • GET /api/fraud/history: Retrieve flagged transaction history",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • All endpoints protected with JWT authorization and XSS validation",
        spacing: { after: 100 }
      }),

      // Data Flow
      new Paragraph({
        text: "Data Flow Architecture",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Real-Time Synchronous Path (API Endpoint):",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. REST client → POST /api/fraud/evaluate with TransactionEvent",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. FraudController receives, validates with FluentValidation",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. IFraudEvaluationService.Evaluate() scores transaction",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. Result persisted via IFraudRepository",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "5. Response returned to client with 200 (not flagged) or 202 (flagged)",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Event-Driven Asynchronous Path (Kafka Consumer):",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. TransactionEvent published to Kafka transactions topic",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. FraudConsumerWorker (IHostedService) consumes message",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. FraudConsumer deserializes message, extracts CorrelationId",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. IFraudEvaluationService.EvaluateAsync() scores transaction",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "5. FraudEventRecord persisted to PostgreSQL database",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "6. If flagged, IFraudProducer publishes to Kafka fraud topic",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "7. Downstream systems (ML model, alerts, case management) consume fraud topic",
        spacing: { after: 100 }
      }),

      // Configuration Management
      new Paragraph({
        text: "Configuration Management",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "Runtime configuration loaded from multiple sources (priority order):",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. Environment variables (highest priority)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. Kubernetes ConfigMaps/Secrets (if deployed to K8s)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. appsettings.{Environment}.json (Environment-specific overrides)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. appsettings.json (base configuration)",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Key Configuration Sections:",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "FraudRulesSettings: Individual rule thresholds, score contributions, enable/disable flags",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "KafkaSettings: Topic names, consumer group IDs, producer idempotence, broker endpoints",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Database: Connection string, timeout, pool size",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Jwt: Secret key, token expiration, issuer/audience",
        spacing: { after: 200 }
      }),

      // Conclusion
      new Paragraph({
        text: "Conclusion",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The Fraud Detection System demonstrates enterprise-grade architecture principles: separation of concerns, dependency injection, strategy pattern for extensibility, event-driven processing for scalability, comprehensive testing, and production-ready observability. The design supports both synchronous REST API usage and asynchronous Kafka-based event processing, enabling integration into diverse transaction processing pipelines at scale."
      })
    ]
  }]
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync('C:/Development/Therron/training/Fraud_Detection_System_Architecture_Overview.docx', buffer);
  console.log('Architecture document created successfully');
});
