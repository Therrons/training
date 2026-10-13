const { Document, Packer, Paragraph, TextRun, HeadingLevel, Table, TableRow, TableCell, WidthType, UnorderedList, ListItem } = require('docx');
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
        text: "Workflow & Process Documentation",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 200 }
      }),

      // Document metadata
      new Paragraph({
        text: "Version: 1.0 | Date: October 2026 | Author: Therron Schlemmer",
        spacing: { after: 400 }
      }),

      // Table of Contents
      new Paragraph({
        text: "Overview",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The Fraud Detection System implements two distinct transaction processing workflows:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. Synchronous REST API workflow: Direct client-to-service evaluation",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. Asynchronous Kafka workflow: Event-driven evaluation with audit trail",
        spacing: { after: 200 }
      }),

      // SYNCHRONOUS WORKFLOW
      new Paragraph({
        text: "Workflow 1: Synchronous REST API Evaluation",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Context & Use Cases",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Synchronous evaluation is used when:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Real-time fraud scoring needed at point of transaction",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Decision must be made within seconds (e.g., POS authorization)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Client expects immediate response with fraud status",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Integration with legacy transaction processing systems",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Step-by-Step Workflow",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),

      new Table({
        width: { size: 100, type: WidthType.PERCENTAGE },
        rows: [
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Step")], width: { size: 10, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Component")], width: { size: 20, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Action")], width: { size: 70, type: WidthType.PERCENTAGE } })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("1")] }),
              new TableCell({ children: [new Paragraph("REST Client")] }),
              new TableCell({ children: [new Paragraph("Sends POST /api/fraud/evaluate with TransactionEvent in JSON body. Includes CorrelationId header for tracing.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("2")] }),
              new TableCell({ children: [new Paragraph("CorrelationIdMiddleware")] }),
              new TableCell({ children: [new Paragraph("Extracts or generates CorrelationId from request header. Enriches log context for distributed tracing.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("3")] }),
              new TableCell({ children: [new Paragraph("FraudController")] }),
              new TableCell({ children: [new Paragraph("Receives request, authenticates using JWT bearer token. Logs request with CorrelationId.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("4")] }),
              new TableCell({ children: [new Paragraph("CheckForXssMiddleware")] }),
              new TableCell({ children: [new Paragraph("Validates JSON payload for malicious scripts. Rejects if XSS patterns detected.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("5")] }),
              new TableCell({ children: [new Paragraph("FluentValidation")] }),
              new TableCell({ children: [new Paragraph("Validates TransactionEvent: required fields, data type constraints, business rule compliance (e.g., amount > 0).")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("6")] }),
              new TableCell({ children: [new Paragraph("FraudEvaluationService")] }),
              new TableCell({ children: [new Paragraph("Synchronously invokes Evaluate(transactionEvent). Orchestrates all fraud rules (see Rule Evaluation section).")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("7")] }),
              new TableCell({ children: [new Paragraph("Fraud Rules")] }),
              new TableCell({ children: [new Paragraph("Each rule evaluates transaction in parallel (logically). Returns IsTriggered and ScoreContribution.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("8")] }),
              new TableCell({ children: [new Paragraph("Score Aggregation")] }),
              new TableCell({ children: [new Paragraph("Sum all rule scores. Cap at MaxScore (100). Compare against FlagThreshold (40). Determine IsFlagged flag.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("9")] }),
              new TableCell({ children: [new Paragraph("FraudRepository")] }),
              new TableCell({ children: [new Paragraph("Persists FraudEventRecord to PostgreSQL database. Uses parameterized queries and connection pooling.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("10")] }),
              new TableCell({ children: [new Paragraph("FraudController")] }),
              new TableCell({ children: [new Paragraph("Returns FraudEvaluationResult to client. HTTP 200 if not flagged, 202 if flagged (fraud alert). Includes CorrelationId in response header.")] })
            ]
          })
        ]
      }),
      new Paragraph({ text: "", spacing: { after: 100 } }),

      new Paragraph({
        text: "Example Request/Response",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Request:",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: `POST /api/fraud/evaluate
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Correlation-Id: corr-12345-abcde

{
  "customerId": "CUST-001",
  "accountId": "ACC-001",
  "amount": 75000.00,
  "transactionType": "CNP",
  "channel": "Online",
  "countryCode": "US",
  "merchantCategory": "retail"
}`,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Response (Flagged):",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: `HTTP 202 Accepted
X-Correlation-Id: corr-12345-abcde

{
  "isFlagged": true,
  "fraudScore": 75.0,
  "flaggedReason": "HIGH_AMOUNT, FOREIGN_CNP",
  "ruleResults": [
    { "ruleCode": "HIGH_AMOUNT", "isTriggered": true, "scoreContribution": 40.0 },
    { "ruleCode": "FOREIGN_CNP", "isTriggered": true, "scoreContribution": 35.0 },
    { "ruleCode": "ATM_WITHDRAWAL_LIMIT", "isTriggered": false, "scoreContribution": 0.0 },
    { "ruleCode": "ROUND_AMOUNT", "isTriggered": false, "scoreContribution": 0.0 },
    { "ruleCode": "HIGH_RISK_MERCHANT", "isTriggered": false, "scoreContribution": 0.0 }
  ]
}`,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Error Handling",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "400 Bad Request: Validation failure (missing field, invalid type). Response includes validation error details.",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "401 Unauthorized: Missing or invalid JWT token. Challenge client to re-authenticate.",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "403 Forbidden: Token valid but user lacks permission. Check authorization policy.",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "500 Internal Server Error: Database connection failure, unhandled exception. Logs full stack trace with CorrelationId for post-incident analysis.",
        spacing: { after: 200 }
      }),

      // ASYNCHRONOUS WORKFLOW
      new Paragraph({
        text: "Workflow 2: Asynchronous Kafka Event-Driven Evaluation",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Context & Use Cases",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Asynchronous evaluation is used when:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Transactions arrive from upstream systems via event streaming",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Audit trail required for compliance (SOX, PCI-DSS)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Batch processing or historical analysis needed",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Multiple downstream systems consume fraud events (ML model, case management, alerting)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Decoupling of transaction source and fraud evaluation",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Step-by-Step Workflow",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),

      new Table({
        width: { size: 100, type: WidthType.PERCENTAGE },
        rows: [
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Phase")], width: { size: 15, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Step")], width: { size: 5, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Component")], width: { size: 25, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Action")], width: { size: 55, type: WidthType.PERCENTAGE } })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Ingestion")] }),
              new TableCell({ children: [new Paragraph("1")] }),
              new TableCell({ children: [new Paragraph("Upstream System")] }),
              new TableCell({ children: [new Paragraph("Publishes TransactionEvent to Kafka 'transactions' topic as JSON message. Includes CorrelationId in message header.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("")] }),
              new TableCell({ children: [new Paragraph("2")] }),
              new TableCell({ children: [new Paragraph("FraudConsumerWorker")] }),
              new TableCell({ children: [new Paragraph("IHostedService subscribes to transactions topic as member of consumer group (e.g., 'fraud-evaluation-group'). Runs on application startup.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Processing")] }),
              new TableCell({ children: [new Paragraph("3")] }),
              new TableCell({ children: [new Paragraph("FraudConsumer")] }),
              new TableCell({ children: [new Paragraph("Receives message from Kafka partition. Deserializes JSON to TransactionEvent. Extracts CorrelationId for distributed tracing.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("")] }),
              new TableCell({ children: [new Paragraph("4")] }),
              new TableCell({ children: [new Paragraph("FraudEvaluationService")] }),
              new TableCell({ children: [new Paragraph("Asynchronously invokes EvaluateAsync(transactionEvent). Orchestrates all fraud rules. Returns FraudEventRecord with score and flag.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("")] }),
              new TableCell({ children: [new Paragraph("5")] }),
              new TableCell({ children: [new Paragraph("Rule Engine")] }),
              new TableCell({ children: [new Paragraph("Each fraud rule evaluates transaction. Returns IsTriggered, RuleCode, ScoreContribution. See Rule Evaluation section.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("")] }),
              new TableCell({ children: [new Paragraph("6")] }),
              new TableCell({ children: [new Paragraph("FraudRepository")] }),
              new TableCell({ children: [new Paragraph("Persists FraudEventRecord to PostgreSQL. Uses batch inserts via stored procedure for performance (50K+ records/sec).")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Publication")] }),
              new TableCell({ children: [new Paragraph("7")] }),
              new TableCell({ children: [new Paragraph("FraudProducer")] }),
              new TableCell({ children: [new Paragraph("If IsFlagged=true, publishes FraudEventRecord to Kafka 'fraud-alerts' topic. Includes CorrelationId in header.")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("")] }),
              new TableCell({ children: [new Paragraph("8")] }),
              new TableCell({ children: [new Paragraph("Downstream Systems")] }),
              new TableCell({ children: [new Paragraph("ML scoring system, case management, alerting service, analytics consume fraud-alerts topic. Each can implement custom response (investigation, block, review).")] })
            ]
          })
        ]
      }),
      new Paragraph({ text: "", spacing: { after: 100 } }),

      new Paragraph({
        text: "Message Format",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Transactions Topic (Input):",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: `{
  "customerId": "CUST-001",
  "accountId": "ACC-001",
  "amount": 75000.00,
  "transactionType": "CNP",
  "channel": "Online",
  "countryCode": "US",
  "merchantName": "Online Retailer Inc",
  "merchantCategory": "retail",
  "transactionTime": "2026-10-12T14:30:00Z"
}
Kafka Headers: correlation-id: corr-12345-abcde`,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Fraud-Alerts Topic (Output - Only Flagged Records):",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: `{
  "fraudEventId": "fraud-001",
  "event": { /* TransactionEvent object */ },
  "isFlagged": true,
  "fraudScore": 75.0,
  "flaggedReason": "HIGH_AMOUNT, FOREIGN_CNP",
  "ruleResults": [ /* Array of triggered rules */ ],
  "evaluatedAt": "2026-10-12T14:30:00.100Z",
  "riskLevel": "HIGH"
}
Kafka Headers: correlation-id: corr-12345-abcde`,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Scalability & Partitioning",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Consumer Scaling: Kafka consumer group automatically distributes partitions across FraudConsumerWorker instances. If topic has 10 partitions, 10 replicas can run in parallel (one partition per pod).",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Partition Key: Transactions are partitioned by customerId to preserve order within a customer's stream. This prevents race conditions where rule evaluation order affects fraud decision.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Offset Management: Consumer records offsets to Kafka broker. If pod crashes, restart resumes from last committed offset (no data loss).",
        spacing: { after: 100 }
      }),

      // RULE EVALUATION PROCESS
      new Paragraph({
        text: "Rule Evaluation Process",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "General Flow",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. FraudEvaluationService receives TransactionEvent",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. Iterates through all registered fraud rules (injected as IEnumerable<IFraudRule>)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. Calls rule.Evaluate(transaction) for each rule in sequence",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. Collects FraudRuleSetRecord results from each rule",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "5. Filters to triggered rules only",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "6. Sums scores: total = sum of all triggered rule ScoreContribution values",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "7. Applies cap: score = Math.Min(total, MaxScore)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "8. Determines flag: isFlagged = (score >= FlagThreshold)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "9. Returns FraudEventRecord with all metadata",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Rule Details",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),

      new Table({
        width: { size: 100, type: WidthType.PERCENTAGE },
        rows: [
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("Rule")], width: { size: 20, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Trigger Condition")], width: { size: 40, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Config Parameters")], width: { size: 20, type: WidthType.PERCENTAGE } }),
              new TableCell({ children: [new Paragraph("Score")], width: { size: 20, type: WidthType.PERCENTAGE } })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("HighAmount")] }),
              new TableCell({ children: [new Paragraph("|amount| > threshold")] }),
              new TableCell({ children: [new Paragraph("Threshold, ScoreContribution, Enabled")] }),
              new TableCell({ children: [new Paragraph("+40")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("ForeignCNP")] }),
              new TableCell({ children: [new Paragraph("(CNP OR Online) AND (countryCode != homeCountry)")] }),
              new TableCell({ children: [new Paragraph("HomeCountry, ScoreContribution, Enabled")] }),
              new TableCell({ children: [new Paragraph("+35")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("ATMWithdrawal")] }),
              new TableCell({ children: [new Paragraph("(ATM transaction) AND |amount| > limit")] }),
              new TableCell({ children: [new Paragraph("Limit, ScoreContribution, Enabled")] }),
              new TableCell({ children: [new Paragraph("+30")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("RoundAmount")] }),
              new TableCell({ children: [new Paragraph("|amount| >= min AND |amount| % multiple == 0")] }),
              new TableCell({ children: [new Paragraph("MinAmount, RoundnessMultiple, ScoreContribution, Enabled")] }),
              new TableCell({ children: [new Paragraph("+10")] })
            ]
          }),
          new TableRow({
            cells: [
              new TableCell({ children: [new Paragraph("HighRiskMerchant")] }),
              new TableCell({ children: [new Paragraph("merchantCategory IN [gambling, casino, crypto, ...]")] }),
              new TableCell({ children: [new Paragraph("Categories, ScoreContribution, Enabled")] }),
              new TableCell({ children: [new Paragraph("+25")] })
            ]
          })
        ]
      }),
      new Paragraph({ text: "", spacing: { after: 100 } }),

      new Paragraph({
        text: "Scoring Example",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Transaction: 75K CNP purchase from US (customer in ZA), merchant category=retail",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "HighAmount: 75000 > 50000 threshold → Triggered, +40",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "ForeignCNP: CNP=true, US != ZA → Triggered, +35",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "ATMWithdrawal: TransactionType != ATM → Not triggered, +0",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "RoundAmount: 75000 % 1000 = 0, but < 1000 check would fail → Not triggered, +0",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "HighRiskMerchant: retail not in {gambling, casino, crypto} → Not triggered, +0",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Total Score: 40 + 35 = 75",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "Capped: min(75, 100) = 75",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "IsFlagged: 75 >= 40 threshold → TRUE",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "FlaggedReason: HIGH_AMOUNT, FOREIGN_CNP",
        spacing: { after: 200 }
      }),

      // DATABASE PERSISTENCE
      new Paragraph({
        text: "Database Persistence Flow",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "FraudRepository Operations",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Write (Insert): FraudEventRecord persisted to fraud_events table via INSERT or stored procedure sp_InsertFraudEvents. Uses parameterized queries to prevent SQL injection.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Columns Stored:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • fraud_event_id: Unique identifier (GUID)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • customer_id: Foreign key to customer",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • account_id: Foreign key to account",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • amount: Transaction amount",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • fraud_score: Calculated score (0-100)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • is_flagged: Boolean flag",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • triggered_rules: Comma-separated rule codes (HIGH_AMOUNT,FOREIGN_CNP)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • evaluated_at: Timestamp of evaluation",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • correlation_id: For distributed tracing",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Read (Query): FraudController retrieves history via IFraudRepository.GetFraudRecordsByCustomer(customerId). Returns paginated results for UI display.",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Connection Pooling",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "DbConnection manages pool of PostgreSQL connections (default: 10 min, 20 max). Reuses connections to reduce overhead of establishing new TCP sessions.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "If all connections busy, requestor waits up to timeout (default 30s) for pool to release a connection. After timeout, throws exception to prevent resource exhaustion.",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Pool automatically returns connection on SqlConnection.Dispose(). Monitors idle connections and recycles them periodically.",
        spacing: { after: 200 }
      }),

      // AUTHENTICATION & AUTHORIZATION
      new Paragraph({
        text: "Authentication & Authorization Flow",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Login Workflow",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. Client POSTs to /api/fraud/login with username/password",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. FraudController.Login() receives request (no [Authorize] - public endpoint)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. JwtService.GenerateToken(username, password) authenticates user",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. If credentials valid, generates JWT token with:",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "     - Payload: username, role, iat (issued at), exp (expiration)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "     - Signature: HMAC-SHA256 using secret key from configuration",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "5. Returns token to client with 3600-second expiration (1 hour)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "6. Client stores token in Authorization header for subsequent requests",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Protected Endpoint Access",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. Client sends GET /api/fraud/evaluate with Authorization: Bearer <token>",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. ASP.NET Core middleware intercepts, extracts token from header",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. JwtHandler validates token signature, expiration, issuer",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. If token invalid/expired, returns 401 Unauthorized",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "5. If valid, extracts claims (username, role) and sets HttpContext.User",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "6. [Authorize] attribute checks User.Identity.IsAuthenticated",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "7. If authorized, proceeds to controller action",
        spacing: { after: 200 }
      }),

      // MONITORING & LOGGING
      new Paragraph({
        text: "Monitoring, Logging & Tracing Flow",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Correlation ID Propagation",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "1. CorrelationIdMiddleware inspects incoming request for X-Correlation-Id header",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "2. If present, extracts and stores in HttpContext.Items[\"correlation-id\"]",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "3. If absent, generates new GUID (e.g., corr-12345-abcde)",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "4. Serilog enriches all log entries with correlation-id property",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "5. For Kafka: correlation-id added to message header and propagated downstream",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "6. Log aggregation (ELK, Splunk) uses correlation-id to trace transaction end-to-end",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Structured Logging",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "Example log entries (structured JSON format):",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: `{
  "timestamp": "2026-10-12T14:30:00.100Z",
  "level": "Information",
  "message": "Fraud evaluation completed",
  "correlation-id": "corr-12345-abcde",
  "customer-id": "CUST-001",
  "fraud-score": 75,
  "is-flagged": true,
  "duration-ms": 42,
  "source": "FraudEvaluationService"
}`,
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Metrics Collection",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "MetricsService tracks:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Authentication attempts: success/failure counts, timing",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Fraud scoring: percentiles of fraud scores, flag rate",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • API latency: P50, P95, P99 percentiles for each endpoint",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Database: query execution time, connection pool usage",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Kafka: producer throughput, consumer lag",
        spacing: { after: 100 }
      }),

      new Paragraph({
        text: "Health Checks",
        heading: HeadingLevel.HEADING_3,
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "GET /health endpoint exposed for Kubernetes liveness probe:",
        spacing: { after: 50 }
      }),
      new Paragraph({
        text: "  • Pings PostgreSQL connection pool - checks database reachability",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Pings Kafka broker admin API - checks broker availability",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • Returns 200 OK if all checks pass, 503 Service Unavailable if any fail",
        spacing: { after: 30 }
      }),
      new Paragraph({
        text: "  • K8s restarts pod if health check fails for > 3 consecutive intervals",
        spacing: { after: 200 }
      }),

      // CONCLUSION
      new Paragraph({
        text: "Summary",
        heading: HeadingLevel.HEADING_2,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: "The Fraud Detection System supports two primary workflows - synchronous REST API for immediate fraud decisions and asynchronous Kafka for event-driven batch processing. Both paths converge on the same FraudEvaluationService, ensuring consistent rule application. Comprehensive logging, distributed tracing, and metrics enable production observability and incident analysis. Database persistence, connection pooling, and Kafka consumer groups provide scalability to handle millions of transactions daily."
      })
    ]
  }]
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync('C:/Development/Therron/training/Fraud_Detection_System_Workflow_Documentation.docx', buffer);
  console.log('Workflow document created successfully');
});
