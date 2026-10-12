# Code Coverage Configuration and Reporting

## Overview
This document describes the code coverage setup for the fraud detection system using Coverlet and OpenCover integration.

## Configuration Files

### coverletSettings.json
Located in the test project root, this file configures code coverage collection:

```json
{
  "include": [
    "fraud_poc_project_buss*",
    "fraud_poc_project_ui*",
    "fraud_poc_project_repo*"
  ],
  "threshold": 75,
  "threshold-type": "line",
  "threshold-stat": "average"
}
```

**Key Settings:**
- **include**: Specifies which assemblies to include in coverage
- **exclude**: Excludes auto-generated code and EF migrations
- **exclude-by-attribute**: Skips classes marked with ExcludeFromCodeCoverage
- **threshold**: Minimum 75% line coverage required
- **threshold-type**: Measures line coverage (not branch coverage)
- **threshold-stat**: Calculates average across all modules

## Running Coverage Tests

### Command Line
```bash
# Run with coverage collection
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:"path/to/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;HtmlSummary;JsonSummary;PdfSummary"
```

### Coverage Results
After running tests with coverage collection:
1. Coverage data is saved to: `TestResults/*/coverage.cobertura.xml`
2. HTML report is generated showing line, branch, and method coverage
3. Coverage badges can be generated for CI/CD pipelines

## Coverage Targets by Component

| Component | Target | Rationale |
|-----------|--------|-----------|
| FraudRules | 85% | Core business logic - high coverage essential |
| FraudEvaluationService | 90% | Service orchestration - must be well-tested |
| FraudRepository | 75% | Database layer - integration tests provide coverage |
| ValidationRules | 90% | Input validation - comprehensive coverage needed |
| Middleware | 80% | Cross-cutting concerns - important for reliability |
| Controllers | 70% | API endpoints - typically lower coverage acceptable |

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage" --no-build

- name: Generate coverage report
  run: |
    dotnet tool install -g reportgenerator
    reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"

- name: Check coverage threshold
  run: |
    coverage=$(grep -oP 'linecoverage="\K[0-9.]+' coverage-report/status.json || echo "0")
    if (( $(echo "$coverage < 75" | bc -l) )); then
      echo "Coverage $coverage% is below threshold 75%"
      exit 1
    fi
```

## Test Coverage by Feature

### Fraud Rules (Expected 85%+ coverage)
- HighAmountRule: Tests for threshold scenarios, boundary conditions
- ForeignCnpRule: Tests for country matching, null handling
- AtmWithdrawalLimitRule: Tests for limit enforcement
- RoundAmountRule: Tests for rounding detection
- HighRiskMerchantCategoryRule: Tests for category matching

### API Endpoints (Expected 70%+ coverage)
- FraudQueryController: Tests for valid/invalid queries
- AuthenticationController: Tests for login/logout flows
- HealthCheck endpoints: Basic availability tests

### Database Layer (Expected 75%+ coverage)
- FraudRepository: Tests for CRUD operations
- Error handling: Tests for DLT error recording
- Transaction handling: Tests for atomicity guarantees

### Validation Layer (Expected 90%+ coverage)
- FraudQueryDtoValidator: All validation rules
- LoginRequestValidator: All validation rules
- Custom validators: Edge cases and boundaries

## Interpreting Coverage Reports

### Line Coverage
Percentage of executable lines run by tests. Target: **75%**
- Green (80%+): Excellent coverage
- Yellow (70-79%): Acceptable coverage
- Red (<70%): Below threshold, investigate uncovered code

### Branch Coverage
Percentage of conditional branches executed. Target: **70%**
- Measures both true and false paths through conditionals

### Method Coverage
Percentage of methods executed. Target: **80%**
- Ensures public API is tested

## Improving Coverage

### Hotspots (Low Coverage Areas)
1. **Error paths**: Add exception handling tests
2. **Edge cases**: Boundary value tests
3. **Conditional logic**: Branch coverage tests

### Adding Tests
```csharp
[Fact]
[DisplayName("Specific scenario with high coverage impact")]
public void TestScenario()
{
    // Arrange: Set up specific conditions
    
    // Act: Execute code path
    
    // Assert: Verify both positive and negative outcomes
}
```

### Coverage-Driven Development
1. Run coverage tests to identify gaps
2. Prioritize untested public methods
3. Add focused tests for uncovered branches
4. Re-run to verify improvement

## Build Integration

### Local Pre-commit Check
```bash
# Run coverage, fail if below threshold
dotnet test --collect:"XPlat Code Coverage" --configuration Release
```

### CI Pipeline Gate
Coverage checks run in CI/CD before merge:
- Block merge if coverage drops below 75%
- Require review for coverage reductions
- Track coverage trends over time

## Tools and Resources

- **Coverlet**: https://github.com/coverlet-coverage/coverlet
- **ReportGenerator**: https://github.com/danielpalme/ReportGenerator
- **OpenCover**: https://github.com/opencover/opencover
- **Codecov Integration**: https://codecov.io

## Next Steps

1. **Baseline Coverage**: Run full suite and establish baseline
2. **Automated Reporting**: Integrate with CI/CD
3. **Coverage Badges**: Add to README
4. **Trend Tracking**: Monitor coverage over releases
5. **Team Standards**: Document coverage expectations per component
