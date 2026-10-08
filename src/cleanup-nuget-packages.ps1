# NuGet Package Cleanup Script
# Removes unused versions of NuGet packages from offline cache
# Total estimated savings: 900MB - 1.5GB
#
# Run as Administrator:
# Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
# & "C:\Development\Therron\training\src\cleanup-nuget-packages.ps1"

$nugetPath = "C:\Development\Therron\training\src\nuget-packages"
$logFile = "C:\Development\Therron\training\nuget-cleanup-log.txt"
$deletedCount = 0
$deletedSize = 0

# Validate paths
if (-not (Test-Path $nugetPath)) {
    Write-Error "NuGet packages folder not found: $nugetPath"
    exit 1
}

# Create log file
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
"=== NuGet Cleanup Log ===" | Out-File -FilePath $logFile
"Start Time: $timestamp" | Add-Content -Path $logFile
"Path: $nugetPath" | Add-Content -Path $logFile
"" | Add-Content -Path $logFile

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "NuGet Package Cleanup Utility" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Source: $nugetPath" -ForegroundColor Gray
Write-Host "Log File: $logFile" -ForegroundColor Gray
Write-Host ""

function Remove-PackageVersion {
    param([string]$PackageName, [string]$Version)

    # Build path properly
    $path = $nugetPath + "\" + $PackageName + "\" + $Version

    if (Test-Path $path) {
        try {
            # Get size before deletion
            $items = Get-ChildItem -Path $path -Recurse -Force
            $size = ($items | Measure-Object -Property Length -Sum).Sum

            # Delete
            Remove-Item -Path $path -Recurse -Force -ErrorAction Stop

            $script:deletedCount++
            $script:deletedSize += $size
            $sizeKB = [math]::Round($size / 1024, 2)

            $message = "[OK] Deleted $PackageName v$Version ($sizeKB KB)"
            Write-Host $message -ForegroundColor Green
            Add-Content -Path $logFile -Value $message

            return $true
        }
        catch {
            $message = "[FAILED] Failed to delete $PackageName v$Version - $_"
            Write-Host $message -ForegroundColor Red
            Add-Content -Path $logFile -Value $message
            return $false
        }
    }
    else {
        $message = "[SKIP] Not found: $PackageName v$Version"
        Write-Host $message -ForegroundColor Yellow
        Add-Content -Path $logFile -Value $message
        return $null
    }
}

# === PHASE 1: Test Platform Packages ===
Write-Host ""
Write-Host "=== PHASE 1: Test Platform Packages ===" -ForegroundColor Cyan
Write-Host "Estimated savings: ~400 MB" -ForegroundColor Gray

Remove-PackageVersion "microsoft.testplatform.testhost" "17.6.0"
Remove-PackageVersion "microsoft.testplatform.testhost" "17.7.2"
Remove-PackageVersion "microsoft.testplatform.testhost" "17.8.0"
Remove-PackageVersion "microsoft.testplatform.objectmodel" "17.6.0"
Remove-PackageVersion "microsoft.testplatform.objectmodel" "17.7.2"
Remove-PackageVersion "microsoft.testplatform.objectmodel" "17.8.0"
Remove-PackageVersion "microsoft.codecoverage" "17.6.0"
Remove-PackageVersion "microsoft.codecoverage" "17.7.2"
Remove-PackageVersion "microsoft.codecoverage" "17.8.0"

# === PHASE 2: xUnit Packages ===
Write-Host ""
Write-Host "=== PHASE 2: xUnit Packages ===" -ForegroundColor Cyan
Write-Host "Estimated savings: ~200 MB" -ForegroundColor Gray

Remove-PackageVersion "xunit.core" "2.4.2"
Remove-PackageVersion "xunit.core" "2.5.3"
Remove-PackageVersion "xunit.core" "2.6.0"
Remove-PackageVersion "xunit.assert" "2.4.2"
Remove-PackageVersion "xunit.assert" "2.5.3"
Remove-PackageVersion "xunit.assert" "2.6.0"
Remove-PackageVersion "xunit.extensibility.core" "2.4.2"
Remove-PackageVersion "xunit.extensibility.core" "2.5.3"
Remove-PackageVersion "xunit.extensibility.core" "2.6.0"
Remove-PackageVersion "xunit.extensibility.execution" "2.4.2"
Remove-PackageVersion "xunit.extensibility.execution" "2.5.3"
Remove-PackageVersion "xunit.extensibility.execution" "2.6.0"

# === PHASE 3: Identity Model Packages ===
Write-Host ""
Write-Host "=== PHASE 3: Identity Model Packages ===" -ForegroundColor Cyan
Write-Host "Estimated savings: ~150 MB" -ForegroundColor Gray

Remove-PackageVersion "microsoft.identitymodel.abstractions" "6.22.0"
Remove-PackageVersion "microsoft.identitymodel.abstractions" "6.35.0"
Remove-PackageVersion "microsoft.identitymodel.abstractions" "8.17.0"
Remove-PackageVersion "microsoft.identitymodel.logging" "6.35.0"
Remove-PackageVersion "microsoft.identitymodel.logging" "8.17.0"

# === PHASE 4: Old Microsoft Extensions ===
Write-Host ""
Write-Host "=== PHASE 4: Old Microsoft Extensions ===" -ForegroundColor Cyan
Write-Host "Estimated savings: ~150 MB" -ForegroundColor Gray

Remove-PackageVersion "microsoft.extensions.logging" "8.0.0"
Remove-PackageVersion "microsoft.extensions.logging.abstractions" "8.0.0"
Remove-PackageVersion "microsoft.extensions.logging.abstractions" "8.0.2"
Remove-PackageVersion "microsoft.extensions.diagnostics.abstractions" "8.0.0"
Remove-PackageVersion "microsoft.extensions.diagnostics.abstractions" "9.0.0"
Remove-PackageVersion "microsoft.extensions.configuration.abstractions" "8.0.0"
Remove-PackageVersion "microsoft.extensions.configuration.binder" "8.0.0"
Remove-PackageVersion "microsoft.extensions.configuration" "8.0.0"
Remove-PackageVersion "microsoft.extensions.dependencyinjection.abstractions" "8.0.0"
Remove-PackageVersion "microsoft.extensions.dependencyinjection" "8.0.0"

# === PHASE 5: Old System Packages ===
Write-Host ""
Write-Host "=== PHASE 5: Old System Packages ===" -ForegroundColor Cyan
Write-Host "Estimated savings: ~30 MB" -ForegroundColor Gray

Remove-PackageVersion "system.diagnostics.eventlog" "6.0.0"
Remove-PackageVersion "system.reflection.metadata" "1.6.0"

# === PHASE 6: Old AWS Packages ===
Write-Host ""
Write-Host "=== PHASE 6: Old AWS Packages ===" -ForegroundColor Cyan
Write-Host "Estimated savings: ~40 MB" -ForegroundColor Gray

Remove-PackageVersion "awssdk.core" "4.0.3.28"

# === Summary ===
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Cleanup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

$sizeMB = [math]::Round($deletedSize / 1024 / 1024, 2)
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  Packages Deleted: $deletedCount" -ForegroundColor White
Write-Host "  Space Freed: $sizeMB MB" -ForegroundColor White
Write-Host "  Log File: $logFile" -ForegroundColor White
Write-Host ""

# Log summary
"" | Add-Content -Path $logFile
"=== Summary ===" | Add-Content -Path $logFile
"Total Deleted: $deletedCount packages" | Add-Content -Path $logFile
"Space Freed: $sizeMB MB" | Add-Content -Path $logFile
"End Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Add-Content -Path $logFile

# Verify next steps
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Review the log file for any errors"
Write-Host "  2. Run: dotnet clean" -ForegroundColor Gray
Write-Host "  3. Run: dotnet restore" -ForegroundColor Gray
Write-Host "  4. Run: dotnet build" -ForegroundColor Gray
Write-Host ""
Write-Host "If issues occur, run 'dotnet restore' to re-download packages." -ForegroundColor Yellow
