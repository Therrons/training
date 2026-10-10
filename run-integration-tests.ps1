# Run integration tests with clean output (suppressed build warnings)

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("all", "api", "business", "metrics", "controllers")]
    [string]$Category = "all",

    [Parameter(Mandatory=$false)]
    [switch]$ShowWarnings = $false
)

# Navigate to the solution directory (where the .sln file is)
$solutionDir = "C:\Development\Therron\training\nuget-config"
if (-not (Test-Path $solutionDir)) {
    # Fallback to training directory if nuget-config doesn't exist
    $solutionDir = "C:\Development\Therron\training"
}

Push-Location $solutionDir

Write-Host "`n" -NoNewline
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Integration Test Runner" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Solution Directory: $solutionDir" -ForegroundColor Gray

# Determine which tests to run based on category
$filterMap = @{
    "all"        = ""
    "api"        = '--filter "FullyQualifiedName~Integration.Api"'
    "business"   = '--filter "FullyQualifiedName~Integration.Business"'
    "metrics"    = '--filter "FullyQualifiedName~Integration.Metrics"'
    "controllers"= '--filter "FullyQualifiedName~Controllers"'
}

$filter = $filterMap[$Category]

Write-Host "Category: $Category" -ForegroundColor Yellow
Write-Host "Command: dotnet test $filter" -ForegroundColor Yellow
Write-Host ""

# Build the command
$cmd = "dotnet test $filter 2>&1"

# Run the tests
$output = Invoke-Expression $cmd

if ($ShowWarnings) {
    # Show all output including warnings
    $output | ForEach-Object { Write-Host $_ }
}
else {
    # Filter out compiler warnings but keep test output
    $output | Where-Object {
        # Exclude lines that are compiler warnings
        $_ -notmatch "^\s+C:\\.*warning CS\d{4}:" -and `
        $_ -notmatch "warning CS\d{4}:" -and `
        $_ -notmatch "^\s+C:\\.*:\d+,\d+\):" -and `
        $_ -notmatch "^    warning" -and `
        $_ -notmatch "^\s*$warning NU\d{4}:" -and `
        $_ -notmatch "^\s*warning xUnit" -and `
        # But keep important content
        ("$_" -match "test|Test|fraud_poc" -or `
         "$_" -match "Passed|Failed|Finished|succeeded|error" -or `
         "$_" -match "^==|^--" -or `
         "$_" -match "Test Run Summary" -or `
         "$_".Trim() -eq "")
    } | ForEach-Object {
        # Format output for better readability
        if ($_ -match "Passed:|Failed:|Total") {
            Write-Host $_ -ForegroundColor Green
        }
        elseif ($_ -match "error") {
            Write-Host $_ -ForegroundColor Red
        }
        else {
            Write-Host $_
        }
    }
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Execution Complete" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

if ($ShowWarnings) {
    Write-Host "`nNote: Compiler warnings displayed. Use -ShowWarnings:`$false to hide them." -ForegroundColor Gray
}
else {
    Write-Host "`nNote: Compiler warnings suppressed. Use -ShowWarnings to display them." -ForegroundColor Gray
}

Pop-Location
