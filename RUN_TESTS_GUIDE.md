# Running Tests with Clean Output

## Quick Start

The `run-tests.ps1` script automatically suppresses compiler warnings and provides clean test output.

### Basic Usage

From the training directory (where `run-tests.ps1` is located):

# ================================
# open powershell as ADMINISTRATOR
# ================================

# Run all integration tests
.\run-tests.ps1


### Run Specific Category

powershell
# API tests only
.\run-tests.ps1 -Category api

# Business logic tests only
.\run-tests.ps1 -Category business

# Metrics tests only
.\run-tests.ps1 -Category metrics

# Controller tests only
.\run-tests.ps1 -Category controllers


### Show Compiler Warnings

powershell
# Include all build warnings in output
.\run-tests.ps1 -ShowWarnings

# Show warnings for specific category
.\run-tests.ps1 -Category api -ShowWarnings


---

## Examples

### Example 1: Daily Quick Check
powershell
.\run-tests.ps1 -Category controllers

**Output**: Fast feedback (2-3 seconds), no warnings

### Example 2: Before Commit
powershell
.\run-tests.ps1

**Output**: All 65 tests, clean summary (30-40 seconds)

### Example 3: Validate Business Rules
powershell
.\run-tests.ps1 -Category business

**Output**: 27 business logic tests, clean output (10-15 seconds)

### Example 4: Debug Specific Failure
powershell
.\run-tests.ps1 -Category api -ShowWarnings

**Output**: API tests with all warnings visible (for debugging)

---

## Script Parameters

| Parameter | Values | Default | Description |
|-----------|--------|---------|-------------|
| `-Category` | all, api, business, metrics, controllers | all | Which tests to run |
| `-ShowWarnings` | Switch (flag) | $false | Include build warnings in output |

---

## Test Categories

| Category | Count | Description |
|----------|-------|-------------|
| **all** | 65 | All integration tests (default) |
| **api** | 14 | HTTP endpoints, JWT auth |
| **business** | 27 | Fraud rule logic |
| **metrics** | 21 | Performance metrics |
| **controllers** | 9 | Request handling |

---

## What Gets Filtered Out

### Removed

warning CS8618: Non-nullable property must contain a non-null value
warning CS8601: Possible null reference assignment
warning CS1591: Missing XML comment for publicly visible type
warning NU1603: Package version mismatch


### Kept (Shown)

Passed: 65
Failed: 0
Test names and results
Project compilation summaries


---

## Troubleshooting

### Script Not Found
Make sure you're in the training directory or use the full path:
powershell
& ".\run-tests.ps1"


### Permission Denied
Allow script execution:
powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser


### Tests Not Running
Try with verbose output:
powershell
.\run-tests.ps1 -ShowWarnings


Check if dotnet is installed:
powershell
dotnet --version


---

## Summary

| Task | Command |
|------|---------|
| Run all tests (clean) | `.\run-tests.ps1` |
| Run API tests | `.\run-tests.ps1 -Category api` |
| Show warnings | `.\run-tests.ps1 -ShowWarnings` |
| Quick check | `.\run-tests.ps1 -Category controllers` |
| Business logic | `.\run-tests.ps1 -Category business` |

**This is the recommended way to run tests locally!**
