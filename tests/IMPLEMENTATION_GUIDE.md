# Test Implementation Guide

## Getting Started

This guide helps developers implement the unit tests that are currently in stub form. Each test class contains `TODO` comments and `throw new NotImplementedException()` statements marking where implementation is needed.

## How Tests Are Structured

All test files follow the **Arrange-Act-Assert (AAA)** pattern:

```csharp
[Fact]
public void ExampleTest_Scenario_ExpectedResult()
{
    // Arrange: Set up test data and mocks
    var testData = CreateTestData();
    var mockService = new Mock<ISomeService>();

    // Act: Execute the method under test
    var result = mockService.Object.DoSomething(testData);

    // Assert: Verify the results
    result.Should().NotBeNull();
    result.Status.Should().Be(StatusEnum.Success);
}
```

## Implementation Workflow

### 1. Choose a Test
Start with a simple test from any of the three test classes. For example:
- `FraudEvaluationServiceTests.Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord`

### 2. Understand the Test Intent
Read the comments in the test's TODO section to understand what should be verified.

### 3. Replace the Stub
Convert from:
```csharp
throw new NotImplementedException("Test implementation required");
```

To actual test implementation using the AAA pattern.

### 4. Run the Test
```bash
dotnet test --filter "FullyQualifiedName~FraudEvaluationServiceTests.Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord"
```

### 5. Verify the Test Works
- Test should pass when the code under test works correctly
- Test should fail when the code is broken

## Key Components to Understand

### Base Test Classes

Each project has a base test class providing:
- Test setup/teardown lifecycle
- Test data factory methods
- Common mock configurations

**Usage Example:**
```csharp
public class MyTests : BaseBusinessLogicTest
{
    public void MyTest()
    {
        // Use factories from base class
        var transaction = CreateValidTransactionEvent();
        var fraudRecord = CreateFraudEventRecord();
    }
}
```

### Test Data Factories

Located in base classes, these create consistent test data:

```csharp
// Create a standard transaction
var transaction = CreateValidTransactionEvent();

// Create a high-value transaction (triggers rules)
var highValue = CreateHighValueTransactionEvent(amount: 50000m);

// Create a foreign transaction
var foreign = CreateForeignTransactionEvent(countryCode: "US");

// Create fraud event record
var fraudEvent = CreateFraudEventRecord(isFlagged: true, fraudScore: 50m);

// Create multiple events for batch testing
var batch = CreateBatchOfFraudEventRecords(count: 100);
```

### Mock Builders

Special helper classes for building complex mocks:

**MockFraudRuleBuilder** - Creates configurable mock fraud rules:

```csharp
var rules = new MockFraudRuleBuilder()
    .AddTriggeredRule("HIGH_AMOUNT", "High amount detected", scoreContribution: 25m)
    .AddTriggeredRule("FOREIGN_TXN", "Foreign transaction", scoreContribution: 20m)
    .AddNonTriggeredRule("REGULAR_PATTERN", "Normal transaction pattern")
    .Build();

var service = new FraudEvaluationService(rules);
```

## Implementation Examples

### Example 1: Simple Verification Test

**Test:** `Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord`

**Before (Stub):**
```csharp
public void Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord()
{
    // Arrange
    var rules = new MockFraudRuleBuilder()
        .AddNonTriggeredRule("LOW_AMOUNT", "Amount below threshold")
        .AddNonTriggeredRule("DOMESTIC", "Transaction is domestic")
        .Build();

    _service = new FraudEvaluationService(rules);
    var transaction = CreateValidTransactionEvent();

    // Act
    var result = _service.Evaluate(transaction);

    // Assert
    throw new NotImplementedException("Test implementation required");
}
```

**After (Implemented):**
```csharp
public void Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord()
{
    // Arrange
    var rules = new MockFraudRuleBuilder()
        .AddNonTriggeredRule("LOW_AMOUNT", "Amount below threshold")
        .AddNonTriggeredRule("DOMESTIC", "Transaction is domestic")
        .Build();

    _service = new FraudEvaluationService(rules);
    var transaction = CreateValidTransactionEvent();

    // Act
    var result = _service.Evaluate(transaction);

    // Assert
    result.IsFlagged.Should().BeFalse();
    result.FraudScore.Should().Be(0m);
    result.FlaggedReason.Should().BeNull();
    result.RuleResults.Should().HaveCount(2);
}
```

### Example 2: Mock Verification Test

**Test:** `Produces_MessageToKafka`

**After (Implemented):**
```csharp
public void Produce_WithValidFraudEventRecord_SendsMessageToKafka()
{
    // Arrange
    var fraudEventRecord = CreateFraudEventRecord();
    var mockProducer = new Mock<IProducer<string, byte[]>>();
    
    mockProducer
        .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), null))
        .ReturnsAsync(new DeliveryReport<string, byte[]>
        {
            Status = PersistenceStatus.Persisted,
            TopicPartitionOffset = new TopicPartitionOffset("fraud-results", 0, 1)
        })
        .Verifiable();

    // Act
    var deliveryReport = await _producer.ProduceAsync(fraudEventRecord);

    // Assert
    deliveryReport.Status.Should().Be(PersistenceStatus.Persisted);
    mockProducer.Verify(
        p => p.ProduceAsync(
            It.Is<string>(key => key == fraudEventRecord.Event.CustomerId),
            It.IsAny<Message<string, byte[]>>(),
            null),
        Times.Once);
}
```

### Example 3: HTTP API Test

**Test:** `GetEvents_WithValidDateRange_Returns200WithEventList`

**After (Implemented):**
```csharp
public async Task GetEvents_WithValidDateRange_Returns200WithEventList()
{
    // Arrange
    var testRecords = CreateBatchOfFraudEventRecords(count: 5);

    _mockRepository!
        .Setup(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()))
        .ReturnsAsync(testRecords);

    var dateFrom = DateTime.UtcNow.AddDays(-7);
    var dateTo = DateTime.UtcNow;
    var query = $"dateFrom={dateFrom:yyyy-MM-dd HH:mm:ss}&dateTo={dateTo:yyyy-MM-dd HH:mm:ss}";

    // Act
    var response = await _client!.GetAsync($"/api/fraud/events?{query}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    
    var content = await response.Content.ReadAsStringAsync();
    var deserializedRecords = JsonConvert.DeserializeObject<List<FraudEventRecord>>(content);
    
    deserializedRecords.Should().HaveCount(5);
    deserializedRecords.Should().AllSatisfy(r => 
        r.Event.TransactionTime.Should().BeOnOrAfter(dateFrom));
}
```

## Testing Different Scenarios

### Testing Boundary Conditions

```csharp
[Theory]
[InlineData(39.99)]  // Just below threshold
[InlineData(40)]     // Exactly at threshold
[InlineData(40.01)]  // Just above threshold
public void Evaluate_WithVaryingScores_RespectsFlagThreshold(decimal score)
{
    // Arrange
    var rules = new MockFraudRuleBuilder()
        .AddTriggeredRule("TEST", "Test rule", scoreContribution: score)
        .Build();

    _service = new FraudEvaluationService(rules);
    var transaction = CreateValidTransactionEvent();

    // Act
    var result = _service.Evaluate(transaction);

    // Assert
    bool shouldBeFlagged = score >= 40m;
    result.IsFlagged.Should().Be(shouldBeFlagged);
}
```

### Testing Exception Handling

```csharp
[Fact]
public async Task SaveFraudEvaluation_WhenDatabaseThrows_RollsBackTransaction()
{
    // Arrange
    var fraudEventRecord = CreateFraudEventRecord();
    var mockConnection = new Mock<NpgsqlConnection>();
    
    mockConnection
        .Setup(c => c.BeginTransactionAsync())
        .ThrowsAsync(new NpgsqlException("Connection failed"));

    // Act
    Func<Task> act = async () => 
        await _repository!.SaveFraudEvaluationAsync(fraudEventRecord);

    // Assert
    await act.Should().ThrowAsync<NpgsqlException>();
}
```

### Testing Async Operations

```csharp
[Fact]
public async Task EvaluateAsync_ReturnsValidResult()
{
    // Arrange
    var rules = new MockFraudRuleBuilder()
        .AddTriggeredRule("TEST", "Test", scoreContribution: 50m)
        .Build();

    _service = new FraudEvaluationService(rules);
    var transaction = CreateValidTransactionEvent();

    // Act
    var task = _service.EvaluateAsync(transaction);
    var result = await task;

    // Assert
    result.Should().NotBeNull();
    result.IsFlagged.Should().BeTrue();
    result.FraudScore.Should().Be(50m);
}
```

## Moq Usage Patterns

### Verify Method Was Called

```csharp
_mockRepository
    .Verify(r => r.SaveFraudEvaluationAsync(It.IsAny<FraudEventRecord>()), 
            Times.Once,
            "Repository should save evaluation result exactly once");
```

### Verify Method Never Called

```csharp
_mockService
    .Verify(s => s.ThrowException(), 
            Times.Never,
            "Exception should not be thrown");
```

### Verify Specific Parameters

```csharp
_mockRepository
    .Verify(r => r.SaveFraudEvaluationAsync(
                It.Is<FraudEventRecord>(x => x.IsFlagged && x.FraudScore > 50m)),
            Times.Once);
```

### Setup with Multiple Calls (Different Returns)

```csharp
var mockService = new Mock<ISomeService>();
mockService
    .SetupSequence(s => s.GetCount())
    .Returns(1)
    .Returns(2)
    .Returns(3);

// First call returns 1, second returns 2, third returns 3
```

## FluentAssertions Usage

### Common Assertions

```csharp
// Null checks
result.Should().BeNull();
result.Should().NotBeNull();

// Boolean
result.IsFlagged.Should().BeTrue();
result.IsFlagged.Should().BeFalse();

// Numeric
result.FraudScore.Should().Be(50m);
result.FraudScore.Should().BeGreaterThan(40m);
result.FraudScore.Should().BeLessThanOrEqualTo(100m);

// Strings
result.FlaggedReason.Should().Contain("HIGH_AMOUNT");
result.FlaggedReason.Should().StartWith("RULE_");
result.FlaggedReason.Should().BeNullOrEmpty();

// Collections
result.RuleResults.Should().HaveCount(3);
result.RuleResults.Should().NotBeEmpty();
result.RuleResults.Should().AllSatisfy(r => 
    r.ScoreContribution.Should().BeGreaterThan(0m));
```

## Running Tests During Development

### Run All Tests in a File
```bash
dotnet test --filter "ClassName=FraudEvaluationServiceTests"
```

### Run Tests Matching a Pattern
```bash
dotnet test --filter "Name~Null"  # Tests with "Null" in name
```

### Run with Verbose Output
```bash
dotnet test --verbosity=detailed
```

### Run Specific Test Class with xunit
```bash
dotnet test --filter "Namespace=fraud_poc_project_buss.Tests.Services"
```

### Watch Mode (Auto-rerun on Changes)
```bash
dotnet watch test
```

## Common Mistakes to Avoid

1. **Not setting up mocks properly**
   - Every dependency the code uses must be mocked or injected
   - Verify mock setups match actual method signatures

2. **Testing implementation details instead of behavior**
   - Test what the method does, not how it does it
   - Focus on inputs and outputs

3. **Creating dependencies between tests**
   - Each test should be independent
   - Tests should run in any order

4. **Over-mocking**
   - Mock only external dependencies (database, Kafka, HTTP)
   - Test the real code when possible

5. **Unclear test names**
   - Use the pattern: `Method_Scenario_ExpectedResult`
   - Name should describe what is being tested

6. **Not testing error paths**
   - Include tests for exceptions and error cases
   - Verify graceful degradation

## Debugging Tests

### Add Breakpoints
Set breakpoints in both the test and the code being tested to debug issues.

### Use Test Output
```csharp
output.WriteLine($"Test value: {result.FraudScore}");
```

### Temporary Logging in Tests
```csharp
System.Diagnostics.Debug.WriteLine($"Result: {result.IsFlagged}");
```

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~FraudEvaluationServiceTests.Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord"
```

## Continuous Integration

Tests should run on every commit:

```yaml
# Example GitHub Actions workflow (if using GitHub)
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0'
      - run: dotnet test
```

## Resources

- **xUnit Documentation**: https://xunit.net/docs/getting-started
- **Moq Documentation**: https://github.com/Moq/moq4/wiki/Quickstart
- **FluentAssertions**: https://fluentassertions.com/
- **Unit Testing Best Practices**: https://docs.microsoft.com/en-us/dotnet/core/testing/

## Support

For questions about specific tests, refer to:
1. Test class comments
2. Base class implementations
3. Mock builder helper classes
4. This implementation guide
