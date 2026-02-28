param(
    [string[]]$Filter
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProject = Join-Path $repoRoot "OracleByFPCLtd.Tests\OracleByFPCLtd.Tests.csproj"
$generatedDirectories = @(
    (Join-Path $repoRoot "OracleByFPCLtd\bin"),
    (Join-Path $repoRoot "OracleByFPCLtd\obj"),
    (Join-Path $repoRoot "OracleByFPCLtd\artifacts"),
    (Join-Path $repoRoot "OracleByFPCLtd.Tests\bin"),
    (Join-Path $repoRoot "OracleByFPCLtd.Tests\obj"),
    (Join-Path $repoRoot "OracleByFPCLtd.Tests\artifacts")
)

function Remove-GeneratedDirectory {
    param([string]$Path)

    if (-not [System.IO.Directory]::Exists($Path)) {
        return
    }

    [System.IO.Directory]::Delete($Path, $true)
}

function Invoke-Cleanup {
    foreach ($path in $generatedDirectories) {
        Remove-GeneratedDirectory -Path $path
    }
}

function Get-NormalizedFilters {
    param([string[]]$Entries)

    $normalized = New-Object System.Collections.Generic.List[string]
    foreach ($entry in $Entries) {
        if ([string]::IsNullOrWhiteSpace($entry)) {
            continue
        }

        foreach ($segment in ($entry -split ",")) {
            $trimmed = $segment.Trim()
            if (-not [string]::IsNullOrWhiteSpace($trimmed)) {
                $normalized.Add($trimmed)
            }
        }
    }

    return $normalized.ToArray()
}

Push-Location $repoRoot
try {
    Write-Host "Cleaning generated build and test output..."
    Invoke-Cleanup

    $exitCode = 0
    $normalizedFilters = @(Get-NormalizedFilters -Entries $Filter)
    if ($normalizedFilters.Length -gt 0) {
        foreach ($entry in $normalizedFilters) {

            Write-Host "Running dotnet test with filter: $entry"
            & dotnet test $testProject --filter $entry
            if ($LASTEXITCODE -ne 0) {
                $exitCode = $LASTEXITCODE
                break
            }
        }
    }
    else {
        Write-Host "Running full dotnet test suite."
        & dotnet test $testProject
        $exitCode = $LASTEXITCODE
    }

    if ($exitCode -ne 0) {
        throw "Test run failed with exit code $exitCode."
    }

    Write-Host "Test run completed successfully."
}
finally {
    Write-Host "Cleaning generated build and test output..."
    try {
        Invoke-Cleanup
    }
    catch {
        Write-Warning ("Post-test cleanup failed: " + $_.Exception.Message)
    }

    Pop-Location
}
