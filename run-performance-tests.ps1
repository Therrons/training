<#
.SYNOPSIS
Runs load and stress tests for the fraud detection system

.DESCRIPTION
This script executes the FraudEvaluationLoadTest and FraudEvaluationStressTest test suites,
measuring performance metrics including throughput, latency, and memory usage.

This script should be run from the training directory root: C:\Development\Therron\training

.PARAMETER TestFilter
Filter for which tests to run. Options: "Load", "Stress", or "All" (default)

.PARAMETER Configuration
Build configuration: "Debug" (default) or "Release"

.PARAMETER Verbose
Show detailed test output

.PARAMETER CollectMetrics
Generate detailed performance metrics reports

.EXAMPLE
.\run-performance-tests.ps1 -TestFilter Load -Configuration Release

.EXAMPLE
.\run-performance-tests.ps1 -TestFilter All -Verbose -CollectMetrics

#>

param(
    [ValidateSet("Load", "Stress", "All")]
    [string]$TestFilter = "All",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$Verbose,

    [switch]$CollectMetrics
)

$ErrorActionPreference = "Stop"
$WarningPreference = "Continue"

# Helper functions for colored output
function WriteSuccess {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Green
}

function WriteErrorMsg {
    param([string]$Message)
    Write-Host "ERROR: $Message" -ForegroundColor Red
}

function WriteWarningMsg {
    param([string]$Message)
    Write-Host "WARNING: $Message" -ForegroundColor Yellow
}

function WriteInfo {
    param([string]$Message)
    Write-Host "INFO: $Message" -ForegroundColor Cyan
}

function WriteSection {
    param([string]$Title)
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host $Title -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta
}

# Get script directory (training root)
$trainingDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path -Path $trainingDir -ChildPath "src"
$testProject = "fraud_poc_project.Tests"

WriteSection "Fraud Detection System - Performance Tests"
WriteInfo "Training Directory: $trainingDir"
WriteInfo "Source Directory: $srcDir"
WriteInfo "Configuration: $Configuration"
WriteInfo "Test Filter: $TestFilter"
WriteInfo "Verbose: $Verbose"
WriteInfo "Collect Metrics: $CollectMetrics"

# Step 1: Verify project exists
WriteSection "Step 1: Verifying Project Structure"
$testProjectPath = Join-Path -Path $srcDir -ChildPath "tests"
$testProjectPath = Join-Path -Path $testProjectPath -ChildPath $testProject
$projectFile = Join-Path -Path $testProjectPath -ChildPath "$testProject.csproj"

if (-not (Test-Path $trainingDir)) {
    WriteErrorMsg "Training directory not found at: $trainingDir"
    exit 1
}
WriteSuccess "Training directory found"

if (-not (Test-Path $srcDir)) {
    WriteErrorMsg "Source directory not found at: $srcDir"
    exit 1
}
WriteSuccess "Source directory found"

if (-not (Test-Path $projectFile)) {
    WriteErrorMsg "Test project not found at: $projectFile"
    WriteInfo "Expected: $projectFile"
    exit 1
}
WriteSuccess "Test project found at: $projectFile"

# Step 2: Check if dotnet CLI is available
WriteSection "Step 2: Checking Prerequisites"
try {
    $dotnetVersion = dotnet --version
    WriteSuccess "dotnet CLI version: $dotnetVersion"
}
catch {
    WriteErrorMsg "dotnet CLI not found. Please install .NET SDK."
    exit 1
}

# Step 3: Clean and restore
WriteSection "Step 3: Preparing Build"
WriteInfo "Cleaning previous builds..."
dotnet clean $projectFile -c $Configuration 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    WriteWarningMsg "Clean failed, continuing..."
}

WriteInfo "Restoring packages..."
dotnet restore $projectFile
if ($LASTEXITCODE -ne 0) {
    WriteErrorMsg "Restore failed"
    exit 1
}
WriteSuccess "Packages restored"

# Step 4: Build tests
WriteSection "Step 4: Building Test Project"
WriteInfo "Building $testProject in $Configuration configuration..."
dotnet build $projectFile -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    WriteErrorMsg "Build failed"
    exit 1
}
WriteSuccess "Build completed"

# Step 5: Run tests based on filter
WriteSection "Step 5: Running Performance Tests"

$testArgs = @(
    "test",
    $projectFile,
    "-c", $Configuration,
    "--no-build",
    "--logger=console;verbosity=normal"
)

# Determine which tests to run
$filterPatterns = @()
switch ($TestFilter) {
    "Load" {
        $filterPatterns += "LoadTest"
    }
    "Stress" {
        $filterPatterns += "StressTest"
    }
    "All" {
        $filterPatterns += "LoadTest"
        $filterPatterns += "StressTest"
    }
}

# Run each test type
$allTestsPassed = $true
$testResults = @()

foreach ($pattern in $filterPatterns) {
    WriteInfo "Running $pattern tests..."
    $startTime = Get-Date

    $runArgs = $testArgs + @("--filter", $pattern)

    if ($Verbose) {
        $runArgs += "--logger=console;verbosity=detailed"
    }

    # Capture output
    $output = dotnet @runArgs 2>&1
    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = $endTime - $startTime

    if ($exitCode -eq 0) {
        WriteSuccess "$pattern PASSED (Duration: $($duration.TotalSeconds)s)"
    }
    else {
        WriteErrorMsg "$pattern FAILED"
        $allTestsPassed = $false
    }

    # Parse output for metrics
    if ($Verbose -or $CollectMetrics) {
        Write-Host $output
    }

    $testResults += @{
        Name = $pattern
        Status = if ($exitCode -eq 0) { "PASSED" } else { "FAILED" }
        Duration = $duration.TotalSeconds
        ExitCode = $exitCode
    }
}

# Step 6: Generate report
WriteSection "Step 6: Performance Test Summary"

WriteInfo "Test Results Summary:"
foreach ($result in $testResults) {
    $statusSymbol = if ($result.Status -eq "PASSED") { "PASS" } else { "FAIL" }
    $statusColor = if ($result.Status -eq "PASSED") { "Green" } else { "Red" }

    Write-Host "  [$statusSymbol] $($result.Name): $($result.Status) ($($result.Duration)s)" -ForegroundColor $statusColor
}

# Step 7: Collect metrics if requested
if ($CollectMetrics) {
    WriteSection "Step 7: Collecting Performance Metrics"

    # Create metrics directory in training root
    $metricsDir = Join-Path -Path $trainingDir -ChildPath "performance-metrics"
    if (-not (Test-Path $metricsDir)) {
        New-Item -ItemType Directory -Path $metricsDir | Out-Null
        WriteSuccess "Created metrics directory: $metricsDir"
    }

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $metricsFile = Join-Path -Path $metricsDir -ChildPath "metrics_$timestamp.txt"

    WriteInfo "Generating metrics report..."

    # Run tests with verbose output and capture to file
    @("Load", "Stress") | ForEach-Object {
        Write-Host "`n=== $_ Tests ===" | Tee-Object -FilePath $metricsFile -Append
        dotnet test $projectFile -c $Configuration --filter "$_Test" --logger=console;verbosity=detailed --no-build 2>&1 | Tee-Object -FilePath $metricsFile -Append
    }

    WriteSuccess "Metrics saved to: $metricsFile"
}

# Step 8: Exit with appropriate code
WriteSection "Performance Tests Complete"
if ($allTestsPassed) {
    WriteSuccess "All performance tests passed!"
    exit 0
}
else {
    WriteErrorMsg "Some performance tests failed"
    exit 1
}
