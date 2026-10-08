# NuGet Package Cleanup Verification Script
# Preview what will be deleted without actually deleting anything
#
# Run:
# & "C:\Development\Therron\training\src\verify-cleanup.ps1"

$nugetPath = "C:\Development\Therron\training\src\nuget-packages"
$totalSize = 0
$packageList = @()

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "NuGet Cleanup Verification (Preview)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$packagesToDelete = @(
    # PHASE 1: Test Platform
    @{Package="microsoft.testplatform.testhost"; Version="17.6.0"; Phase="1"},
    @{Package="microsoft.testplatform.testhost"; Version="17.7.2"; Phase="1"},
    @{Package="microsoft.testplatform.testhost"; Version="17.8.0"; Phase="1"},
    @{Package="microsoft.testplatform.objectmodel"; Version="17.6.0"; Phase="1"},
    @{Package="microsoft.testplatform.objectmodel"; Version="17.7.2"; Phase="1"},
    @{Package="microsoft.testplatform.objectmodel"; Version="17.8.0"; Phase="1"},
    @{Package="microsoft.codecoverage"; Version="17.6.0"; Phase="1"},
    @{Package="microsoft.codecoverage"; Version="17.7.2"; Phase="1"},
    @{Package="microsoft.codecoverage"; Version="17.8.0"; Phase="1"},

    # PHASE 2: xUnit
    @{Package="xunit.core"; Version="2.4.2"; Phase="2"},
    @{Package="xunit.core"; Version="2.5.3"; Phase="2"},
    @{Package="xunit.core"; Version="2.6.0"; Phase="2"},
    @{Package="xunit.assert"; Version="2.4.2"; Phase="2"},
    @{Package="xunit.assert"; Version="2.5.3"; Phase="2"},
    @{Package="xunit.assert"; Version="2.6.0"; Phase="2"},
    @{Package="xunit.extensibility.core"; Version="2.4.2"; Phase="2"},
    @{Package="xunit.extensibility.core"; Version="2.5.3"; Phase="2"},
    @{Package="xunit.extensibility.core"; Version="2.6.0"; Phase="2"},
    @{Package="xunit.extensibility.execution"; Version="2.4.2"; Phase="2"},
    @{Package="xunit.extensibility.execution"; Version="2.5.3"; Phase="2"},
    @{Package="xunit.extensibility.execution"; Version="2.6.0"; Phase="2"},

    # PHASE 3: Identity Model
    @{Package="microsoft.identitymodel.abstractions"; Version="6.22.0"; Phase="3"},
    @{Package="microsoft.identitymodel.abstractions"; Version="6.35.0"; Phase="3"},
    @{Package="microsoft.identitymodel.abstractions"; Version="8.17.0"; Phase="3"},
    @{Package="microsoft.identitymodel.logging"; Version="6.35.0"; Phase="3"},
    @{Package="microsoft.identitymodel.logging"; Version="8.17.0"; Phase="3"},

    # PHASE 4: Microsoft Extensions
    @{Package="microsoft.extensions.logging"; Version="8.0.0"; Phase="4"},
    @{Package="microsoft.extensions.logging.abstractions"; Version="8.0.0"; Phase="4"},
    @{Package="microsoft.extensions.logging.abstractions"; Version="8.0.2"; Phase="4"},
    @{Package="microsoft.extensions.diagnostics.abstractions"; Version="8.0.0"; Phase="4"},
    @{Package="microsoft.extensions.diagnostics.abstractions"; Version="9.0.0"; Phase="4"},
    @{Package="microsoft.extensions.configuration.abstractions"; Version="8.0.0"; Phase="4"},
    @{Package="microsoft.extensions.configuration.binder"; Version="8.0.0"; Phase="4"},
    @{Package="microsoft.extensions.configuration"; Version="8.0.0"; Phase="4"},
    @{Package="microsoft.extensions.dependencyinjection.abstractions"; Version="8.0.0"; Phase="4"},
    @{Package="microsoft.extensions.dependencyinjection"; Version="8.0.0"; Phase="4"},

    # PHASE 5: System Packages
    @{Package="system.diagnostics.eventlog"; Version="6.0.0"; Phase="5"},
    @{Package="system.reflection.metadata"; Version="1.6.0"; Phase="5"},

    # PHASE 6: AWS
    @{Package="awssdk.core"; Version="4.0.3.28"; Phase="6"}
)

$phaseGroups = $packagesToDelete | Group-Object Phase

foreach ($group in $phaseGroups | Sort-Object Name) {
    $phase = $group.Name
    Write-Host ""
    Write-Host "=== PHASE $phase ===" -ForegroundColor Yellow

    $phaseSize = 0
    $phaseCount = 0

    foreach ($item in $group.Group) {
        $path = Join-Path $nugetPath $item.Package $item.Version

        if (Test-Path $path) {
            $folderSize = (Get-ChildItem -Path $path -Recurse -Force | Measure-Object -Property Length -Sum).Sum
            $folderSizeMB = [math]::Round($folderSize / 1024 / 1024, 2)

            Write-Host "  [FOUND] $($item.Package) v$($item.Version)" -ForegroundColor Green
            Write-Host "          Size: $folderSizeMB MB" -ForegroundColor Gray

            $phaseSize += $folderSize
            $phaseCount++
        }
        else {
            Write-Host "  [MISSING] $($item.Package) v$($item.Version)" -ForegroundColor Red
        }
    }

    $phaseSizeMB = [math]::Round($phaseSize / 1024 / 1024, 2)
    Write-Host "  Phase Total: $phaseCount packages, $phaseSizeMB MB" -ForegroundColor Cyan

    $totalSize += $phaseSize
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
$totalSizeMB = [math]::Round($totalSize / 1024 / 1024, 2)
$totalSizeGB = [math]::Round($totalSize / 1024 / 1024 / 1024, 2)

Write-Host "TOTAL TO DELETE:" -ForegroundColor Yellow
Write-Host "  Packages: $($packagesToDelete.Count)" -ForegroundColor White
Write-Host "  Size: $totalSizeMB MB ($totalSizeGB GB)" -ForegroundColor Green
Write-Host ""
Write-Host "When ready to proceed:" -ForegroundColor Cyan
Write-Host "  Run: Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process" -ForegroundColor Gray
Write-Host "  Run: & `"C:\Development\Therron\training\src\cleanup-nuget-packages.ps1`"" -ForegroundColor Gray
Write-Host ""
