# Test Scripts Documentation
## C# Fraud Detection System

**Document Version:** 1.0  
**Last Updated:** October 2026  
**Solution Location:** `<training-root>` (relative path - wherever you've cloned/downloaded the solution)

> **Note:** All paths in this document are relative. Replace `<training-root>` with your actual training directory path, or use `./` to reference the current directory when running scripts.

---

## Table of Contents

1. [Path Configuration](#path-configuration)
2. [Overview](#overview)
3. [Script Inventory](#script-inventory)
4. [Performance Tests Script](#performance-tests-script)
5. [Integration Tests Script](#integration-tests-script)
6. [Test Categories Explained](#test-categories-explained)
7. [Usage Examples](#usage-examples)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)

---

## Path Configuration

### Understanding Relative Paths

This documentation uses **relative paths** so it works regardless of where you install the solution. Paths are relative to the **training directory root** where the test scripts are located.

### Path Notation Reference

| Notation | Meaning | Example |
|----------|---------|---------|
| `./` | Current directory (training root) | `./run-performance-tests.ps1` |
| `./src/` | Source directory under training root | `./src/tests/fraud_poc_project.Tests/` |
| `<training-root>` | Placeholder for your installation path | Replace with your actual path |
| `performance-metrics/` | Metrics folder (created in training root) | `./performance-metrics/metrics_*.txt` |

### Your Installation Path

**Find your training root directory:**

```powershell
# Option 1: Navigate to where you cloned the solution
# Look for these files to confirm you're in the right place:
ls run-performance-tests.ps1      # ✓ Should exist
ls run-integration-tests.ps1      # ✓ Should exist
ls src/                           # ✓ Should exist

# Option 2: Get the current directory
$trainingRoot = Get-Location
Write-Host "Training root: $trainingRoot"
```

### Examples for Different Installation Paths

**If installed on C: drive:**
```powershell
cd C:\Development\Therron\training
.\run-performance-tests.ps1
```

**If installed on D: drive:**
```powershell
cd D:\my-projects\fraud-detection\training
.\run-performance-tests.ps1
```

**If cloned in home directory:**
```powershell
cd ~\Projects\fraud-detection-system\training
.\run-performance-tests.ps1
```

**Key Point:** Regardless of where you installed it, once you `cd` into the training root, all the relative paths in this document will work correctly!

---

---

## Overview

The fraud detection system provides two primary test automation scripts for validating system functionality, performance, and integration:

| Script | Purpose | Location | Language |
|--------|---------|----------|----------|
| `run-performance-tests.ps1` | Load & Stress Testing | `<training-root>` | PowerShell |
| `run-integration-tests.ps1` | Integration Testing | `<training-root>` | PowerShell |

Both scripts are designed to run from the **training directory root** and support multiple test categories and filtering options.

### Getting Started

1. **Navigate to the training root directory** where the solution is located:
   ```powershell
   cd <path-to-your-training-directory>
   ```

2. **Verify scripts exist:**
   ```powershell
   ls run-*.ps1
   # Should show: run-performance-tests.ps1 and run-integration-tests.ps1
   ```

3. **Check directory structure:**
   ```powershell
   ls
   # Should show: src/, run-performance-tests.ps1, run-integration-tests.ps1, etc.
   ```

---

## Script Inventory

### Test Projects Covered

```
fraud_poc_project.Tests (53 tests, 2,324 LOC)
├── Unit Tests
│   ├── FraudControllerTests (9 tests)
│   └── FraudRuleIntegrationTests (14 tests)
├── Integration Tests
│   ├── FraudControllerApiTests (16 tests)
│   └── MetricsServiceTests (28 tests)
└── Performance Tests
    ├── FraudEvaluationLoadTest (3 tests)
    └── FraudEvaluationStressTest (3 tests)

fraud_poc_project_buss.Tests (64 tests, 1,572 LOC)
├── FraudEvaluationServiceTests (12 tests)
├── AtmWithdrawalLimitRuleTests (10 tests)
├── ForeignCnpRuleTests (10 tests)
├── HighAmountRuleTests (10 tests)
├── HighRiskMerchantCategoryRuleTests (15 tests)
└── RoundAmountRuleTests (13 tests)

fraud_poc_project_repo.Tests (9 tests, 290 LOC)
└── FraudRepositoryTests (9 tests)

TOTAL: 140+ tests across 3 projects
```

---

## Performance Tests Script

### Script: `run-performance-tests.ps1`

**Purpose:** Execute load and stress tests to measure system performance characteristics (throughput, latency, memory usage).

**Location:** `./run-performance-tests.ps1` (in training root)

**Target Tests:**
- `FraudEvaluationLoadTest` - Measures throughput and latency
- `FraudEvaluationStressTest` - Measures system stability under increasing load

### Parameters

| Parameter | Type | Default | Options | Description |
|-----------|------|---------|---------|-------------|
| `-TestFilter` | String | `All` | `Load`, `Stress`, `All` | Which performance test suite to run |
| `-Configuration` | String | `Debug` | `Debug`, `Release` | Build configuration to test |
| `-Verbose` | Switch | Off | — | Display detailed test output |
| `-CollectMetrics` | Switch | Off | — | Generate detailed performance metrics report |

### Test Filter Options

#### 1. Load Tests (`-TestFilter Load`)
Executes **FraudEvaluationLoadTest** suite:
- **10,000 sequential transactions** test
- **100 concurrent threads** test  
- **50KB memory allocation** test

**What it measures:**
- Throughput: > 1000 transactions/second
- Latency: P50 < 10ms, P95 < 50ms, P99 < 100ms
- Memory: < 10MB per 1M transactions
- Database connection pooling efficiency

**Duration:** ~2-3 minutes

```powershell
# Run only load tests in Release configuration
.\run-performance-tests.ps1 -TestFilter Load -Configuration Release
```

#### 2. Stress Tests (`-TestFilter Stress`)
Executes **FraudEvaluationStressTest** suite:
- **Increasing load test** (10 → 100 → 500 → 1000 threads)
- **30-second sustained load** test
- **Spike load test** (sudden spike from baseline to 5000 concurrent)

**What it measures:**
- System stability under increasing concurrency
- Memory leak detection
- Thread pool saturation point
- Recovery from spike events

**Duration:** ~4-5 minutes

```powershell
# Run only stress tests with verbose output
.\run-performance-tests.ps1 -TestFilter Stress -Verbose
```

#### 3. All Tests (`-TestFilter All`) — Default
Executes **both Load and Stress** test suites sequentially.

**Total Duration:** ~6-8 minutes

```powershell
# Run all performance tests with metrics collection
.\run-performance-tests.ps1 -TestFilter All -CollectMetrics
```

### Configuration Options

#### Debug Configuration (`-Configuration Debug`)
- **Default setting**
- Includes debug symbols
- Slower execution (optimization disabled)
- Useful for debugging failures

```powershell
.\run-performance-tests.ps1 -Configuration Debug
```

#### Release Configuration (`-Configuration Release`)
- **Recommended for performance testing**
- Optimizations enabled
- Faster execution
- More accurate performance metrics
- Closer to production environment

```powershell
.\run-performance-tests.ps1 -Configuration Release
```

### Output Options

#### Standard Output
Displays test status, pass/fail results, and execution time:

```
========================================
Fraud Detection System - Performance Tests
========================================

Step 1: Verifying Project Structure
✓ Training directory found
✓ Source directory found  
✓ Test project found at: ./src/tests/fraud_poc_project.Tests/fraud_poc_project.Tests.csproj

Step 4: Building Test Project
Building fraud_poc_project.Tests in Debug configuration...
✓ Build completed

Step 5: Running Performance Tests
INFO: Running LoadTest tests...
INFO: Running StressTest tests...

Step 6: Performance Test Summary
  [PASS] LoadTest: PASSED (45.2s)
  [PASS] StressTest: PASSED (52.8s)

✓ All performance tests passed!
```

#### Verbose Output (`-Verbose`)
Displays detailed test execution information:
- Individual test method names
- Assertion details
- Stack traces for failures
- Performance metric breakdowns

```powershell
.\run-performance-tests.ps1 -TestFilter Load -Verbose
```

**Output includes:**
```
PASS fraud_poc_project.Tests.FraudEvaluationLoadTest.SequentialLoadTest_10000Transactions_MeasuresThroughputAndLatency
Throughput: 15234.5 tx/sec
P50 Latency: 6.2ms
P95 Latency: 45.3ms
P99 Latency: 98.7ms

PASS fraud_poc_project.Tests.FraudEvaluationStressTest.IncreasingLoadTest_ConcurrencyFrom10To1000...
```

#### Metrics Collection (`-CollectMetrics`)
Generates timestamped performance report file:

- **Location:** `./performance-metrics/` (created in training root)
- **Filename:** `metrics_YYYYMMDD_HHMMSS.txt`
- **Contents:** Complete detailed test output including all assertions and measurements

```powershell
.\run-performance-tests.ps1 -TestFilter All -CollectMetrics
```

**Example Report:**
```
performance-metrics/
├── metrics_20261012_140530.txt  (10/12/26 @ 2:05:30 PM)
├── metrics_20261011_093045.txt  (10/11/26 @ 9:30:45 AM)
└── metrics_20261010_165812.txt  (10/10/26 @ 4:58:12 PM)
```

### Performance Test Details

#### Load Test: SequentialLoadTest (10,000 transactions)
**Purpose:** Baseline throughput and latency measurement

**What happens:**
1. Creates 10,000 TransactionEvent objects
2. Evaluates each sequentially (no parallelism)
3. Measures individual latencies
4. Aggregates P50, P95, P99 latencies
5. Calculates throughput (tx/sec)

**Assertions:**
```
✓ Throughput > 1000 tx/sec
✓ P50 Latency < 10ms
✓ P95 Latency < 50ms  
✓ P99 Latency < 100ms
✓ Average < 10ms
```

#### Load Test: ConcurrentThreadsTest (100 threads)
**Purpose:** Thread pool and concurrency testing

**What happens:**
1. Spawns 100 concurrent threads
2. Each thread processes 100 transactions
3. Measures aggregate throughput
4. Tracks thread safety of MetricsService

**Assertions:**
```
✓ All 10,000 transactions complete
✓ No race conditions in metrics
✓ Latency within acceptable range
```

#### Load Test: MemoryAllocationTest (50KB)
**Purpose:** Memory efficiency validation

**What happens:**
1. Allocates 50KB of transaction data
2. Evaluates all transactions
3. Measures peak memory usage
4. Verifies no memory leaks

**Assertions:**
```
✓ Memory usage < 10MB per 1M transactions
✓ No unbounded collection growth
```

#### Stress Test: IncreasingLoadTest (10 → 1000 threads)
**Purpose:** System stability as concurrency increases

**Progression:**
- 10 threads (warm-up)
- 100 threads (baseline)
- 500 threads (high load)
- 1000 threads (extreme load)

**Measurements at each level:**
- Thread count
- Average latency
- Latency degradation %
- Error count

**Assertions:**
```
✓ 10 threads: P99 < 100ms
✓ 100 threads: P99 < 150ms (50% degradation acceptable)
✓ 500 threads: P99 < 200ms (100% degradation acceptable)
✓ 1000 threads: P99 < 300ms (200% degradation acceptable)
✓ Zero exceptions/crashes
```

#### Stress Test: SustainedLoadTest (30 seconds @ 100 threads)
**Purpose:** Detect memory leaks, connection pool exhaustion

**What happens:**
1. Runs 100 threads continuously for 30 seconds
2. Measures latency, throughput over time
3. Checks for degradation patterns
4. Verifies stable memory usage

**Assertions:**
```
✓ Stable throughput (variance < 10%)
✓ Memory usage stable (no growth trend)
✓ No connection pool exhaustion
```

#### Stress Test: SpikeLoadTest (sudden 5000 concurrent)
**Purpose:** Recovery from unexpected spike in traffic

**What happens:**
1. Establishes 100-thread baseline
2. Measures warm-up latency
3. Suddenly jumps to 5000 concurrent threads
4. Measures spike latency
5. Verifies recovery

**Assertions:**
```
✓ Spike latency ≤ 200% of baseline (not infinite)
✓ No dropped transactions
✓ System recovers after spike
```

---

## Integration Tests Script

### Script: `run-integration-tests.ps1`

**Purpose:** Execute integration tests to validate component interactions, API functionality, and business logic flows.

**Location:** `./run-integration-tests.ps1` (in training root)

**Target Projects:**
- `./src/tests/fraud_poc_project.Tests` — API integration tests
- `./src/tests/fraud_poc_project_buss.Tests` — Business logic tests
- `./src/tests/fraud_poc_project_repo.Tests` — Repository tests

### Parameters

| Parameter | Type | Default | Options | Description |
|-----------|------|---------|---------|-------------|
| `-Category` | String | `all` | `all`, `api`, `business`, `metrics`, `controllers` | Which integration test category to run |
| `-ShowWarnings` | Switch | Off | — | Display compiler warnings in output |

### Test Category Options

#### 1. All Tests (`-Category all`) — Default
Executes **all integration tests** across all test projects.

**Tests included:**
- API integration tests (16 tests)
- Business logic tests (14 tests)
- Metrics tests (28 tests)
- Controller tests (9 tests)
- Rule tests (64 tests)
- Repository tests (9 tests)

**Total:** ~140+ test methods

**Duration:** ~2-3 minutes

```powershell
# Run all integration tests (default)
.\run-integration-tests.ps1

# Equivalent explicit call
.\run-integration-tests.ps1 -Category all
```

#### 2. API Tests (`-Category api`)
Executes **API integration tests** from `FraudControllerApiTests.cs`

**Test Classes Included:**
- FraudControllerApiTests

**Tests:**
1. Login endpoint validation
2. Authentication token generation
3. Authorized endpoint access
4. Unauthorized access rejection
5. Query endpoint filtering
6. Response structure validation
7. Error response format

**Sample tests:**
- `LoginEndpoint_WithValidCredentials_ReturnsJwtToken`
- `QueryEventsEndpoint_WithValidToken_ReturnsFilteredResults`
- `QueryEventsEndpoint_WithoutToken_ReturnsUnauthorized`
- `QueryEventsEndpoint_WithInvalidDateRange_ReturnsBadRequest`

**Duration:** ~30 seconds

```powershell
# Run only API integration tests
.\run-integration-tests.ps1 -Category api
```

#### 3. Business Logic Tests (`-Category business`)
Executes **business logic integration tests** from `FraudRuleIntegrationTests.cs`

**Test Classes Included:**
- FraudRuleIntegrationTests
- FraudEvaluationServiceTests

**Tests:**
1. Rule evaluation with real configurations
2. Score aggregation accuracy
3. Threshold comparison logic
4. Multiple rules firing simultaneously
5. Disabled rule handling
6. Edge case transaction scenarios

**Sample tests:**
- `AllRulesFiringSimultaneously_ScoresAreAggregated`
- `FraudScoreBelowThreshold_TransactionNotFlagged`
- `FraudScoreAboveThreshold_TransactionFlagged`
- `DisabledRulesContributeZeroScore`

**Duration:** ~45 seconds

```powershell
# Run only business logic tests
.\run-integration-tests.ps1 -Category business
```

#### 4. Metrics Tests (`-Category metrics`)
Executes **metrics collection and accuracy tests** from `MetricsServiceTests.cs`

**Test Classes Included:**
- MetricsServiceTests

**Tests:**
1. Transaction count tracking
2. Fraud rate calculation
3. Latency percentile computation
4. Metrics aggregation
5. Thread-safe metrics updates
6. Metrics snapshot consistency

**Sample tests:**
- `RecordTransactionProcessed_CountsIncrements`
- `GetSnapshot_ReturnsConsistentMetrics`
- `ConcurrentMetricsUpdates_AreThreadSafe`
- `LatencyPercentileCalculation_IsAccurate`

**Duration:** ~20 seconds

```powershell
# Run only metrics tests
.\run-integration-tests.ps1 -Category metrics
```

#### 5. Controllers Tests (`-Category controllers`)
Executes **controller unit and integration tests** from `FraudControllerTests.cs`

**Test Classes Included:**
- FraudControllerTests

**Tests:**
1. Controller constructor injection
2. Login method routing
3. Query events method routing
4. Authorization filtering
5. Response mapping
6. Error handling

**Sample tests:**
- `FraudController_LoginAction_ReturnsOkWithToken`
- `FraudController_QueryAction_CallsService`
- `FraudController_UnauthorizedRequest_ReturnsUnauthorized`

**Duration:** ~20 seconds

```powershell
# Run only controller tests
.\run-integration-tests.ps1 -Category controllers
```

### Output Options

#### Standard Output (Warnings Suppressed)
Default output suppresses compiler warnings for cleaner display:

```
================================
Integration Test Runner
================================
Solution Directory: C:\Development\Therron\training
Category: all
Command: dotnet test

Test Run Summary:
  Passed: 140
  Failed: 0
  Total: 140

PASSED - fraud_poc_project.Tests.FraudControllerApiTests
PASSED - fraud_poc_project_buss.Tests.FraudEvaluationServiceTests
PASSED - fraud_poc_project_repo.Tests.FraudRepositoryTests

================================
Execution Complete
================================

Note: Compiler warnings suppressed. Use -ShowWarnings to display them.
```

#### With Warnings (`-ShowWarnings`)
Displays all compiler warnings along with test results:

```powershell
# Show compiler warnings during test execution
.\run-integration-tests.ps1 -Category all -ShowWarnings
```

**Output includes:**
```
C:\Development\Therron\training\src\fraud_poc_project.Tests\FraudControllerTests.cs(42,5): warning CS8602: Dereference of a possibly null reference.
C:\Development\Therron\training\src\fraud_poc_project_buss.Tests\FraudEvaluationServiceTests.cs(18,15): warning CS0219: The variable 'unused' is assigned but its value is never used

Test Run Summary:
  Passed: 140
  Failed: 0
  Total: 140
```

---

## Test Categories Explained

### By Test Scope

#### Unit Tests
- **What:** Individual component in isolation
- **Examples:** Rule evaluation, metric calculation
- **Dependencies:** Mocked
- **Speed:** Fast (~1 second per test)
- **Coverage:** High

#### Integration Tests
- **What:** Multiple components working together
- **Examples:** Service + Repository, Service + Rules
- **Dependencies:** Real or stubbed collaborators
- **Speed:** Medium (~2-5 seconds per test)
- **Coverage:** Medium-high

#### API Tests
- **What:** Full API stack from HTTP to database
- **Examples:** Login endpoint, Query endpoint
- **Dependencies:** Full DI container
- **Speed:** Slower (~5-10 seconds per test)
- **Coverage:** Medium

#### Performance Tests
- **What:** Throughput, latency, memory, stability
- **Examples:** Load test, stress test
- **Dependencies:** Full system
- **Speed:** Slowest (~30-60 seconds per test)
- **Coverage:** Operational characteristics

### By Execution Time

| Category | Count | Duration | Best For |
|----------|-------|----------|----------|
| Unit Tests | 64 | ~30 sec | Development |
| Integration Tests | 53 | ~1 min | CI/CD pipeline |
| Performance Tests | 6 | ~6 min | Pre-release |
| All Tests | 140+ | ~8 min | Full validation |

---

## Usage Examples

### 1. Quick Sanity Check (Development)
Run only business logic tests to verify fraud rules still work:

```powershell
.\run-integration-tests.ps1 -Category business
```

**Expected output:** `Passed: 14, Failed: 0` (in ~45 seconds)

### 2. Pre-Commit Validation
Run all integration tests before committing code:

```powershell
.\run-integration-tests.ps1 -Category all
```

**Expected output:** `Passed: 140, Failed: 0` (in ~2 minutes)

### 3. Performance Baseline (Development)
Measure current performance in Debug configuration:

```powershell
.\run-performance-tests.ps1 -TestFilter All -Configuration Debug
```

**Expected output:** All tests PASSED with throughput/latency metrics

### 4. Performance Comparison (Release vs Debug)
Benchmark Release configuration for production readiness:

```powershell
# Run in Release (optimized)
.\run-performance-tests.ps1 -TestFilter All -Configuration Release -CollectMetrics

# Run in Debug (for comparison)
.\run-performance-tests.ps1 -TestFilter All -Configuration Debug -CollectMetrics
```

**Compare the two metrics files:**
```powershell
# Show latest metrics files
Get-ChildItem ./performance-metrics/metrics_*.txt | Select-Object -Last 2

# Compare content
Compare-Object (Get-Content ./performance-metrics/metrics_*.txt -Tail 20) `
               -DifferenceObject (Get-ChildItem ./performance-metrics/metrics_*.txt | Sort-Object LastWriteTime -Descending | Select-Object -Skip 1 -First 1 | Get-Content -Tail 20)
```

**Example results:**
```
Release: 15,234 tx/sec, P99: 87ms
Debug:    9,456 tx/sec, P99: 142ms
```

### 5. Stress Testing (Production Readiness)
Verify system stability under extreme load:

```powershell
.\run-performance-tests.ps1 -TestFilter Stress -Configuration Release -Verbose
```

**Validates:**
- No crashes at 1000 concurrent threads
- Memory stable over 30 seconds
- Recovery from 5000-thread spike

### 6. API Testing (Endpoint Validation)
Verify REST API endpoints work end-to-end:

```powershell
.\run-integration-tests.ps1 -Category api
```

**Validates:**
- Login endpoint returns JWT
- Query endpoint requires auth
- Response format correct
- Error responses proper

### 7. Metrics Testing (Observability)
Verify metrics collection accuracy:

```powershell
.\run-integration-tests.ps1 -Category metrics
```

**Validates:**
- Transaction counts correct
- Latency percentiles calculated
- Thread safety maintained
- No memory leaks in metrics

### 8. Full System Validation (Release)
Complete validation before production deployment:

```powershell
# Run all integration tests
.\run-integration-tests.ps1 -Category all

# Run all performance tests in Release
.\run-performance-tests.ps1 -TestFilter All -Configuration Release -CollectMetrics

# Verify results in metrics folder
dir ./performance-metrics/
```

---

## Best Practices

### 1. Always Use Release Configuration for Performance Tests
Performance metrics from Debug are 30-40% slower:

```powershell
# ✓ Correct - Use Release for accurate metrics
.\run-performance-tests.ps1 -Configuration Release

# ✗ Avoid - Debug is too slow for performance testing
.\run-performance-tests.ps1 -Configuration Debug
```

### 2. Collect Metrics for Historical Trending
Compare performance over time to detect regressions:

```powershell
# Collect metrics weekly
.\run-performance-tests.ps1 -Configuration Release -CollectMetrics

# Review trends
Get-Content performance-metrics/metrics_*.txt | Select-String "Throughput|P99"
```

### 3. Run Integration Tests Before Each Commit
Catch regressions early in development cycle:

```powershell
# Pre-commit hook equivalent
.\run-integration-tests.ps1 -Category all
if ($LASTEXITCODE -ne 0) { exit 1 }
git commit -m "Changes"
```

### 4. Test Different Configurations
Verify code works in both Debug and Release:

```powershell
# Test both configurations
@("Debug", "Release") | ForEach-Object {
    Write-Host "Testing in $_ configuration..."
    .\run-integration-tests.ps1 -Category all
}
```

### 5. Focus Tests for Development
Run only affected test category during development:

```powershell
# Changed fraud rules? Test business logic only
.\run-integration-tests.ps1 -Category business

# Changed API? Test API only
.\run-integration-tests.ps1 -Category api

# Changed metrics? Test metrics only
.\run-integration-tests.ps1 -Category metrics
```

### 6. Use Verbose Mode for Debugging
When tests fail, use verbose output to see details:

```powershell
.\run-performance-tests.ps1 -TestFilter Load -Verbose
```

### 7. Establish Performance Baseline
Document current performance as baseline:

```powershell
# Create baseline report
.\run-performance-tests.ps1 -Configuration Release -CollectMetrics

# Rename for tracking (latest metrics file)
$latestMetrics = Get-ChildItem ./performance-metrics/metrics_*.txt | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Copy-Item $latestMetrics "./performance-metrics/BASELINE_RELEASE.txt"
```

---

## Troubleshooting

### Issue: "Test project not found"

**Error:**
```
ERROR: Test project not found at: ./src/tests/fraud_poc_project.Tests/fraud_poc_project.Tests.csproj
```

**Solution:**
1. Verify you're in the training directory root (where `run-performance-tests.ps1` is located)
2. Check `src/tests/` directory exists
3. Verify `fraud_poc_project.Tests.csproj` exists

```powershell
# Navigate to your training root directory
# (wherever you cloned/downloaded the solution)
cd <path-to-your-solution>/training

# Verify structure
ls src/tests/fraud_poc_project.Tests/
```

### Issue: "dotnet CLI not found"

**Error:**
```
ERROR: dotnet CLI not found. Please install .NET SDK.
```

**Solution:**
1. Download .NET 8.0 SDK from microsoft.com
2. Install and restart PowerShell
3. Verify installation:

```powershell
dotnet --version
# Should output: 8.x.xxx
```

### Issue: Tests timeout or hang

**Error:**
```
ERROR: Tests failed to complete (timed out)
```

**Solution:**
1. Check if localhost is available
2. Verify no other instances running
3. Try in Debug configuration first
4. Increase timeout if necessary

```powershell
# Verify no instances running
Get-Process | Where-Object { $_.Name -match "fraud|dotnet" }

# Kill if necessary
Stop-Process -Name "fraud_poc_project*" -Force
```

### Issue: Performance below expectations

**Symptoms:**
```
FAIL: LoadTest
P99 Latency: 450ms (expected < 100ms)
```

**Solutions:**
1. Verify Release configuration: `.\run-performance-tests.ps1 -Configuration Release`
2. Check system resources (CPU, memory, I/O)
3. Review verbose output for bottlenecks
4. Compare against baseline metrics

```powershell
# Run with verbose to see details
.\run-performance-tests.ps1 -Configuration Release -Verbose

# Compare against baseline
Compare-Object -ReferenceObject (Get-Content ./performance-metrics/BASELINE_RELEASE.txt) `
                -DifferenceObject (Get-ChildItem ./performance-metrics/metrics_*.txt | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content)
```

### Issue: "No matching tests found"

**Error:**
```
INFO: No matching tests with the filter pattern: 'LoadTest'
```

**Solution:**
1. Verify test names match filter pattern
2. Check test project was built successfully
3. Use correct filter case

```powershell
# Valid filters: Load, Stress, All
.\run-performance-tests.ps1 -TestFilter Load     # ✓ Correct
.\run-performance-tests.ps1 -TestFilter load     # ✗ Wrong case
.\run-performance-tests.ps1 -TestFilter Unit     # ✗ Not available
```

---

## Exit Codes

### Performance Tests

| Code | Status | Meaning |
|------|--------|---------|
| `0` | ✓ SUCCESS | All tests passed |
| `1` | ✗ FAILURE | One or more tests failed |

### Integration Tests

| Code | Status | Meaning |
|------|--------|---------|
| `0` | ✓ SUCCESS | All tests passed |
| Non-zero | ✗ FAILURE | One or more tests failed |

---

## Quick Reference

### Run Test Categories (Integration)

```powershell
# Run all tests
.\run-integration-tests.ps1

# Run specific category
.\run-integration-tests.ps1 -Category api
.\run-integration-tests.ps1 -Category business
.\run-integration-tests.ps1 -Category metrics
.\run-integration-tests.ps1 -Category controllers

# Show compiler warnings
.\run-integration-tests.ps1 -ShowWarnings
```

### Run Performance Tests

```powershell
# Run all performance tests (default)
.\run-performance-tests.ps1

# Run specific type
.\run-performance-tests.ps1 -TestFilter Load
.\run-performance-tests.ps1 -TestFilter Stress

# Use Release configuration (recommended for perf)
.\run-performance-tests.ps1 -Configuration Release

# With detailed output
.\run-performance-tests.ps1 -Verbose

# Collect metrics to file
.\run-performance-tests.ps1 -CollectMetrics

# Combined: Everything
.\run-performance-tests.ps1 -TestFilter All -Configuration Release -Verbose -CollectMetrics
```

---

## Summary

Your fraud detection system includes two comprehensive test automation scripts:

1. **`run-performance-tests.ps1`** — Load and stress testing with throughput/latency validation
   - **Parameters:** TestFilter, Configuration, Verbose, CollectMetrics
   - **Options:** Load, Stress, All; Debug, Release
   - **Duration:** 2-8 minutes depending on options

2. **`run-integration-tests.ps1`** — Integration testing across API, business, metrics, controllers
   - **Parameters:** Category, ShowWarnings
   - **Options:** all, api, business, metrics, controllers
   - **Duration:** 20 seconds - 3 minutes depending on category

Both scripts support filtering, detailed output, and comprehensive reporting for development, CI/CD, and production validation scenarios.

### For Developers

> **Remember:** All paths in this document are **relative**. Before running any scripts:
> 1. Navigate to your training root directory: `cd <your-training-path>`
> 2. Verify you're in the right place: `ls run-*.ps1`
> 3. Run scripts using: `.\run-performance-tests.ps1` or `.\run-integration-tests.ps1`

This makes the documentation portable across all developer installations!

---

**Last Updated:** October 2026  
**Portable:** Yes - Uses relative paths  
**For questions or updates:** Refer to individual script headers or project documentation
