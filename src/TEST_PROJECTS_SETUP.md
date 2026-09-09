# Unit Test Projects Setup

Three comprehensive test projects have been created for your fraud detection solution.

## Test Projects Created

### 1. `fraud_poc_project_buss.Tests`
Tests for business logic and fraud detection rules.

**Contents:**
- `TestDataBuilder.cs` - Fluent builders for creating test transaction data
- `FraudRules/VelocityRuleTests.cs` - Tests for velocity-based fraud detection
- `FraudRules/AmountRuleTests.cs` - Tests for amount-based anomalies
- `FraudRules/GeographicRuleTests.cs` - Tests for impossible travel detection
- `FraudEvaluationServiceTests.cs` - Tests for fraud evaluation orchestration

**Key Tests:**
- 6 tests for VelocityRule
- 8 tests for AmountRule
- 7 tests for GeographicRule
- 8 tests for FraudEvaluationService

**Total: ~29 tests**

### 2. `fraud_poc_project_repo.Tests`
Tests for data repository layer.

**Contents:**
- `FraudRepositoryTests.cs` - Tests for repository interface contracts

**Key Tests:**
- Save fraud events
- Get customer transaction history
- Filter by time windows
- Handle null/empty scenarios

**Total: ~8 tests**

### 3. `fraud_poc_project.Tests`
Tests for API controllers and integration.

**Contents:**
- `Controllers/FraudControllerTests.cs` - Tests for API endpoints

**Key Tests:**
- Transaction evaluation endpoint
- Transaction history retrieval
- Kafka producer integration
- Repository persistence
- Error handling

**Total: ~8 tests**

---

## Running the Tests

### Option 1: Visual Studio
1. Open Solution in Visual Studio
2. Test Explorer → Run All Tests
3. Or right-click test project → Run Tests

### Option 2: Command Line
```bash
cd C:\Development\Therron\training

# Run all tests
dotnet test

# Run specific test project
dotnet test src\fraud_poc_project_buss.Tests\fraud_poc_project_buss.Tests.csproj

# Run with verbose output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter ClassName=VelocityRuleTests

# Generate test results
dotnet test --logger "trx;LogFileName=test-results.trx"
```

### Option 3: Using xUnit Test Runner
```bash
# Install xUnit test runner (if not in VS)
dotnet tool install -g xunit.runner.console

# Run tests
dotnet xunit fraud_poc_project_buss.Tests.dll
```

---

## Test Structure

### Test Data Builders
The `TestDataBuilder.cs` provides fluent APIs for creating test data:

```csharp
// Create a transaction with defaults
var transaction = new FraudTransactionEventBuilder()
    .WithCustomerId("CUST-001")
    .WithAmount(1000m)
    .WithCountry("GB")
    .Build();

// Create a fraud result
var result = new FraudEvaluationResultBuilder()
    .WithIsFraud(true)
    .WithRiskScore(0.8m)
    .WithFlaggedRules("VelocityRule", "AmountRule")
    .Build();
```

### Mocking Pattern
Tests use Moq for creating mock objects:

```csharp
var mockService = new Mock<IFraudEvaluationService>();
mockService
    .Setup(s => s.Evaluate(It.IsAny<FraudTransactionEvent>(), It.IsAny<List<FraudTransactionEvent>>()))
    .Returns(new FraudEvaluationResult { IsFraud = true });
```

### Assertions
Tests use FluentAssertions for readable assertions:

```csharp
result.IsFraud.Should().BeTrue();
result.RiskScore.Should().BeGreaterThan(0.5m);
result.FlaggedRules.Should().Contain("VelocityRule");
```

---

## Test Coverage

| Component | Tests | Status |
|-----------|-------|--------|
| VelocityRule | 6 | ✅ Covered |
| AmountRule | 8 | ✅ Covered |
| GeographicRule | 7 | ✅ Covered |
| FraudEvaluationService | 8 | ✅ Covered |
| FraudRepository | 6 | ✅ Covered |
| FraudController | 8 | ✅ Covered |
| **Total** | **43** | **✅ Covered** |

---

## Extending the Tests

### Add Tests for Additional Rules
1. Create new file: `FraudRules/YourRuleTests.cs`
2. Follow the pattern from existing rule tests
3. Use `FraudTransactionEventBuilder` for test data

Example:
```csharp
public class YourRuleTests
{
    private readonly YourRule _rule = new();

    [Fact]
    public void Evaluate_WithSomeCondition_ReturnsExpectedResult()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithAmount(100m)
            .Build();

        // Act
        var result = _rule.Evaluate(transaction, new List<FraudTransactionEvent>());

        // Assert
        result.Should().BeFalse();
    }
}
```

### Add Integration Tests
1. Create new file: `Integration/FullFlowTests.cs`
2. Use `WebApplicationFactory<Program>` for ASP.NET Core integration
3. Test full request/response cycles

### Add Database Tests
1. Create new file: `Database/FraudRepositoryIntegrationTests.cs`
2. Use test database or in-memory provider
3. Test actual database operations (separate from mock tests)

---

## Test Frameworks & Versions

- **xUnit 2.6.6** - Test framework
- **Moq 4.20.70** - Mocking library
- **FluentAssertions 6.12.0** - Assertion library
- **Microsoft.AspNetCore.Mvc.Testing 8.0.0** - Web API testing

All packages are available in your offline NuGet packages folder.

---

## Troubleshooting

### Tests won't run
```bash
# Rebuild solution
dotnet clean
dotnet build

# Restore packages
dotnet restore --configfile nuget-config/NuGet.config
```

### Missing references
- Ensure `fraud_poc_project_buss.csproj` is referenced in test project
- Check project paths are relative to test project location

### Moq issues
- Ensure interfaces are public
- Mock only public methods
- Use `It.IsAny<T>()` for flexible matching

---

## Next Steps

1. ✅ Run tests: `dotnet test`
2. ✅ Review test coverage
3. ✅ Add tests for remaining fraud rules (if any)
4. ✅ Add integration tests with test database
5. ✅ Set up CI/CD to run tests on commit

---

## Quick Commands

```bash
# Run all tests with coverage
dotnet test /p:CollectCoverage=true

# Run tests matching pattern
dotnet test --filter "Category=Rules"

# Run with output
dotnet test -- --verbosity verbose

# Watch mode (re-run on file changes)
dotnet watch test
```
