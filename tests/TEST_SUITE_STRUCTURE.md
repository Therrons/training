# Fraud Detection Solution - Comprehensive Unit Test Suite

## Overview
This directory contains a complete unit test suite structure for the C# fraud detection solution. The suite is organized into three test projects targeting different layers of the application with xUnit, Moq, and FluentAssertions frameworks.

## Project Structure

```
tests/
├── fraud_poc_project_buss.Tests/          # Business Logic Tests
│   ├── Fixtures/
│   │   └── BaseBusinessLogicTest.cs       # Base class with test data factories
│   ├── Services/
│   │   └── FraudEvaluationServiceTests.cs # Tests for fraud scoring engine
│   ├── Helpers/
│   │   └── MockFraudRuleBuilder.cs        # Helper for building mock fraud rules
│   └── fraud_poc_project_buss.Tests.csproj
│
├── fraud_poc_project_repo.Tests/          # Repository & Data Access Tests
│   ├── Fixtures/
│   │   └── BaseRepositoryTest.cs          # Base class for repository tests
│   ├── Repositories/
│   │   └── FraudRepositoryTests.cs        # Tests for data persistence
│   ├── Kafka/
│   │   └── FraudProducerTests.cs          # Tests for Kafka message production
│   └── fraud_poc_project_repo.Tests.csproj
│
├── fraud_poc_project.Tests/               # API & Consumer Tests
│   ├── Fixtures/
│   │   └── BaseApiTest.cs                 # Base class for API tests
│   ├── Controllers/
│   │   └── FraudControllerTests.cs        # Tests for API endpoints
│   ├── Kafka/
│   │   └── FraudConsumerWorkerTests.cs    # Tests for background consumer
│   └── fraud_poc_project.Tests.csproj
│
└── TEST_SUITE_STRUCTURE.md                # This file
```

## Test Projects

### 1. fraud_poc_project_buss.Tests - Business Logic Layer

**Purpose:** Unit tests for core fraud evaluation logic and business rules.

**Key Test Classes:**

#### FraudEvaluationServiceTests
Tests the `FraudEvaluationService` which scores transactions and determines fraud flags.

**Test Categories:**

- **Null Input Handling**
  - Test with null transaction event

- **No Rules Triggered**
  - Verify unflagged result when all rules pass

- **Single Rule Triggered Below Threshold**
  - Verify score accumulation works correctly
  - Threshold is 40 points; verify score below threshold doesn't flag

- **Multiple Rules Triggered Above Threshold**
  - Verify multiple rule scores add correctly
  - Verify transaction is flagged at/above threshold

- **Score Capping**
  - Verify score never exceeds maximum of 100
  - Even if rules would sum to more

- **Async Evaluation**
  - Verify async method completes and returns same results as sync

- **Flagged Reason Message**
  - Verify reason string contains all triggered rule codes
  - Verify format is comma-separated

- **Empty Rules Collection**
  - Verify service handles no rules gracefully

- **Boundary Score at Threshold**
  - Verify score exactly at 40 (threshold) returns flagged

- **Transaction Preserved in Result**
  - Verify original transaction included in result

**Test Data Factories:**
- `CreateValidTransactionEvent()` - Standard transaction
- `CreateHighValueTransactionEvent()` - Transaction that triggers high-amount rules
- `CreateForeignTransactionEvent()` - Transaction from unusual country
- `CreateFraudEventRecord()` - Fraud evaluation result

**Mocking Strategy:**
- Use `MockFraudRuleBuilder` to create configurable mock fraud rules
- Simulate triggered/non-triggered rules with various scores

---

### 2. fraud_poc_project_repo.Tests - Data Access Layer

**Purpose:** Unit tests for database operations and Kafka integration.

**Key Test Classes:**

#### FraudRepositoryTests
Tests the `FraudRepository` for CRUD operations and data consistency.

**Test Categories:**

- **SaveFraudEvaluationAsync - Success**
  - Verify evaluation is saved to database
  - Verify fraudEventId is returned

- **SaveFraudEvaluationAsync - Input Validation**
  - Null event record throws exception
  - Null transaction event handled gracefully

- **Transaction Management**
  - Verify rollback on database error
  - Verify no partial data saved on failure

- **Rule Results Persistence**
  - Verify all rule results are saved
  - Verify each rule is linked to fraud event

- **QueryFraudEventsAsync**
  - Verify events within date range are returned
  - Verify empty result for date range with no events

- **QueryFlaggedOnlyFraudEventsAsync**
  - Verify only flagged=true events returned
  - Verify date range filtering works

- **GetRuleResultsForEventAsync**
  - Verify all rule results retrieved for event
  - Verify empty collection for non-existent event

- **Configuration & Connection**
  - Verify connection string is required
  - Verify database schema is configurable

- **SQL Injection Prevention**
  - Verify special characters are handled safely
  - Verify malicious SQL doesn't execute

- **Null Fields Handling**
  - Verify nullable fields (merchant name, category, etc.) insert DBNull
  - Verify no database errors on null values

**Test Data Factories:**
- `CreateValidTransactionEvent()`
- `CreateFraudEventRecord()`
- `CreateBatchOfFraudEventRecords()`

**Mocking Strategy:**
- Mock `IConfiguration` for connection strings and settings
- Mock `IDBConnection` for database operations
- Mock `ILogger` for logging verification

**Integration Options:**
- Testcontainers.PostgreSQL for real database tests
- In-memory mock for unit-only testing

---

#### FraudProducerTests
Tests the `FraudProducer` for Kafka message production.

**Test Categories:**

- **Synchronous Publishing**
  - Verify message sent to Kafka topic
  - Verify message key is customerId
  - Verify no exceptions on success

- **Asynchronous Publishing**
  - Verify delivery report returned
  - Verify async operation completes

- **Dead Letter Topic (DLT) Publishing**
  - Verify failed messages sent to DLT
  - Verify error context included in DLT message

- **Message Serialization**
  - Verify JSON serialization with camelCase
  - Verify all transaction fields included
  - Verify deserialized message matches original

- **Message Partitioning**
  - Verify customerId used as key
  - Verify same customer messages go to same partition

- **Configuration**
  - Verify producer initialized with Kafka broker settings
  - Verify SASL/SSL configuration applied

- **Retry Logic**
  - Verify connection retries work
  - Verify eventual success after retries

- **Idempotence**
  - Verify duplicate detection enabled
  - Verify same message twice has same sequence

- **Resource Cleanup**
  - Verify Dispose closes producer
  - Verify subsequent calls throw ObjectDisposedException

- **Large Payloads**
  - Verify large messages handled correctly
  - Verify message size limits respected

- **Topic Configuration**
  - Verify messages sent to correct topic

**Test Data Factories:**
- `CreateFraudEventRecord()` - Message to produce
- `CreateBatchOfFraudEventRecords()` - Batch testing

**Mocking Strategy:**
- Mock `IProducer<string, byte[]>` from Confluent.Kafka
- Mock `IOptions` for configuration
- Capture and verify message details

---

### 3. fraud_poc_project.Tests - API & Consumer Layer

**Purpose:** Unit tests for API endpoints and background consumer worker.

**Key Test Classes:**

#### FraudControllerTests
Tests the `FraudController` REST API endpoints.

**Test Categories:**

- **GET /api/fraud/events**
  - Query all events in date range returns 200 OK
  - Missing date range returns 400 Bad Request
  - No events in range returns 200 with empty array
  - Large result sets handled correctly

- **GET /api/fraud/events/flagged**
  - Query flagged events only returns 200 OK
  - All returned events have IsFlagged = true
  - No flagged events returns empty array

- **GET /api/fraud/events/{fraudEventId}/rules**
  - Valid event ID returns 200 with rule results
  - Non-existent event returns empty array or 404
  - Invalid event ID (non-numeric, 0, negative) returns 400

- **Response Format**
  - All endpoints return JSON Content-Type
  - JSON is properly deserialized
  - All properties included in response

- **Error Handling**
  - Repository exceptions return 500 Internal Server Error
  - Error messages don't leak sensitive info
  - Appropriate HTTP status codes used

- **Query Filtering**
  - Optional filters (customerId, etc.) work correctly
  - Filters properly passed to repository
  - Multiple filter combinations work

**Test Data Factories:**
- `CreateValidTransactionEvent()`
- `CreateFraudEventRecord()`
- `CreateBatchOfFraudEventRecords()`

**Mocking Strategy:**
- Use `WebApplicationFactory` for integration tests
- Mock `IFraudRepository` to control database responses
- Verify HTTP response codes and content

---

#### FraudConsumerWorkerTests
Tests the `FraudConsumerWorker` background message processor.

**Test Categories:**

- **Message Processing**
  - Processes all messages from Kafka topic
  - Calls evaluation service for each message
  - Saves results to repository

- **Graceful Shutdown**
  - StopAsync stops message consumption
  - Resources are released
  - No new messages processed after stop

- **Service Integration**
  - Verification that evaluation service is called
  - Evaluation service results used for save

- **Repository Integration**
  - Verification that repository save is called
  - Results saved after evaluation

- **Error Handling - Null/Invalid Messages**
  - Null messages handled gracefully
  - Invalid JSON routed to DLT
  - Processing continues after error

- **Error Handling - Service Exceptions**
  - Evaluation service exceptions caught
  - Message routed to DLT
  - Error logged with context

- **Error Handling - Repository Exceptions**
  - Repository save exceptions caught
  - Message routed to DLT with error
  - Original transaction preserved

- **Batch Processing**
  - Multiple messages in batch processed
  - All messages evaluated and saved
  - Service/repository called once per message

- **Kafka Coordination**
  - Joins correct consumer group
  - Offset committed after successful processing
  - Offset NOT committed on failure

- **Rebalance Handling**
  - Heartbeats sent regularly
  - Rebalance handled when new consumer joins
  - No message loss during rebalance

- **Performance**
  - Message processing within benchmark
  - Example: 1000 messages in < 5 seconds

**Test Data Factories:**
- `CreateValidTransactionEvent()`
- `CreateFraudEventRecord()`
- `CreateBatchOfFraudEventRecords()`

**Mocking Strategy:**
- Mock `IFraudEvaluationService` to control evaluation results
- Mock `IFraudRepository` to control save operations
- Mock Kafka consumer to provide test messages
- Mock Kafka producer for DLT routing

---

## Test Framework and Dependencies

### Frameworks & Libraries

| Library | Version | Purpose |
|---------|---------|---------|
| xUnit | 2.7.1 | Unit test framework |
| Moq | 4.20.70 | Mocking framework |
| FluentAssertions | 6.12.1 | Fluent assertion library |
| Microsoft.NET.Test.Sdk | 17.10.0 | Test SDK |
| xunit.runner.visualstudio | 2.5.8 | Visual Studio test runner |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.8 | API integration testing |
| Testcontainers.PostgreSQL | 3.9.0 | Docker container for tests |

### Target Framework
- .NET 8.0

### Package References
- fraud_poc_project_buss
- fraud_poc_project_repo
- fraud_poc_project_ui (for API tests)

---

## Running the Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test tests/fraud_poc_project_buss.Tests/fraud_poc_project_buss.Tests.csproj
dotnet test tests/fraud_poc_project_repo.Tests/fraud_poc_project_repo.Tests.csproj
dotnet test tests/fraud_poc_project.Tests/fraud_poc_project.Tests.csproj
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~FraudEvaluationServiceTests"
```

### Run Specific Test Method
```bash
dotnet test --filter "FullyQualifiedName~FraudEvaluationServiceTests.Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord"
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Run in Watch Mode
```bash
dotnet watch test
```

---

## Test Implementation Checklist

Each test file contains stubs with comments indicating what needs to be implemented. Use the checklist below:

### Business Logic Tests (fraud_poc_project_buss.Tests)
- [ ] FraudEvaluationServiceTests - 10 test methods

### Repository Tests (fraud_poc_project_repo.Tests)
- [ ] FraudRepositoryTests - 10 test methods
- [ ] FraudProducerTests - 11 test methods

### API & Consumer Tests (fraud_poc_project.Tests)
- [ ] FraudControllerTests - 12 test methods
- [ ] FraudConsumerWorkerTests - 13 test methods

**Total: 46 test methods**

---

## Best Practices Applied

1. **Naming Convention**: `MethodName_Condition_ExpectedResult`
   - Example: `Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord`

2. **Arrange-Act-Assert Pattern**: Each test follows AAA structure
   - Arrange: Set up test data and mocks
   - Act: Execute the method under test
   - Assert: Verify the results

3. **Test Data Factories**: Reusable methods for creating consistent test data
   - Base classes provide common factories
   - Builder pattern for complex mocks

4. **Isolation**: Each test is independent
   - Mocks prevent external dependencies
   - Tests can run in any order

5. **Documentation**: Each test includes comments explaining purpose
   - TODO markers for implementation
   - Clear test categories

6. **FluentAssertions**: Readable, chainable assertions
   - Example: `result.IsFlagged.Should().BeTrue()`
   - Better error messages

7. **Moq Verification**: Verify interactions with dependencies
   - Verify service methods called expected times
   - Verify parameters passed correctly

8. **Edge Cases**: Boundary conditions and error scenarios covered
   - Null inputs
   - Empty collections
   - Exact threshold values
   - Maximum values

---

## Coverage Goals

**Target Coverage:**
- Statements: > 85%
- Branches: > 80%
- Methods: > 90%
- Lines: > 85%

**Focus Areas (High Priority):**
1. Core fraud evaluation logic
2. Data persistence (save/query operations)
3. Kafka message handling
4. API endpoint validation
5. Error handling and recovery

---

## Integration Testing Considerations

For tests that need real infrastructure:

1. **PostgreSQL Database**
   - Use Testcontainers.PostgreSQL for automatic container management
   - Tests can spin up isolated database instances
   - No manual database setup required

2. **Kafka**
   - Use Testcontainers.Kafka for similar benefits
   - Or mock for pure unit tests

3. **Configuration**
   - Override with test-specific settings
   - Separate test configuration files if needed

---

## Future Enhancements

1. **Performance Tests**
   - Benchmark fraud evaluation scoring
   - Kafka producer throughput tests

2. **Load Tests**
   - Simulate high message volume
   - Database connection pooling validation

3. **Contract Tests**
   - Kafka message format validation
   - API response schema validation

4. **Mutation Testing**
   - Verify test quality
   - Identify weak tests

5. **Continuous Integration**
   - Automated test runs on push
   - Coverage reports
   - Test result tracking

---

## File Locations Summary

```
C:\Development\Therron\training\tests\
├── fraud_poc_project_buss.Tests\
│   ├── fraud_poc_project_buss.Tests.csproj
│   ├── Fixtures\BaseBusinessLogicTest.cs
│   ├── Helpers\MockFraudRuleBuilder.cs
│   └── Services\FraudEvaluationServiceTests.cs
│
├── fraud_poc_project_repo.Tests\
│   ├── fraud_poc_project_repo.Tests.csproj
│   ├── Fixtures\BaseRepositoryTest.cs
│   ├── Repositories\FraudRepositoryTests.cs
│   └── Kafka\FraudProducerTests.cs
│
├── fraud_poc_project.Tests\
│   ├── fraud_poc_project.Tests.csproj
│   ├── Fixtures\BaseApiTest.cs
│   ├── Controllers\FraudControllerTests.cs
│   └── Kafka\FraudConsumerWorkerTests.cs
│
└── TEST_SUITE_STRUCTURE.md (this file)
```

---

## Contact & Questions

For questions about the test structure or implementation, refer to:
- Test class comments for specific test guidance
- Base class methods for test data factory usage
- Mock builder classes for complex mock setup
