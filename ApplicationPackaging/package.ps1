param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [string]$RepoRoot = "Y:\Desktop\Development\Oracle",
    [string]$PackagesRoot = "Y:\Desktop\Development\ShippedPackages\OracleByFPC",
    [switch]$StartupOptimized = $true
)

$ErrorActionPreference = "Stop"

$releaseDir = Join-Path $PackagesRoot ("v" + $Version)
$stagingDir = Join-Path $releaseDir "source-staging"
$publishDir = Join-Path $releaseDir "publish"
$defaultExe = Join-Path $publishDir "OracleByFPCLtd.exe"
$versionedExe = Join-Path $publishDir ("OracleByFPC_v" + $Version + ".exe")

Write-Host "Packaging OracleByFPCLtd v$Version"
Write-Host "RepoRoot: $RepoRoot"
Write-Host "ReleaseDir: $releaseDir"
Write-Host "StartupOptimized: $StartupOptimized"

if (Test-Path $releaseDir) {
    Write-Host "Existing release directory found, overwriting: $releaseDir"
    Remove-Item -Path $releaseDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

# Copy app project (exclude build outputs)
robocopy "$RepoRoot\OracleByFPCLtd" "$stagingDir\OracleByFPCLtd" /E /XD bin obj
if ($LASTEXITCODE -ge 8) {
    throw "robocopy for OracleByFPCLtd failed with exit code $LASTEXITCODE"
}

# Copy required UI resource
New-Item -ItemType Directory -Force -Path "$stagingDir\UserInterface" | Out-Null
robocopy "$RepoRoot\UserInterface" "$stagingDir\UserInterface" feeny-logo-100-circle-black-back.png
if ($LASTEXITCODE -ge 8) {
    throw "robocopy for UserInterface failed with exit code $LASTEXITCODE"
}

# Stage packaging changelog so project content include resolves during publish
New-Item -ItemType Directory -Force -Path "$stagingDir\ApplicationPackaging" | Out-Null
Copy-Item "$RepoRoot\ApplicationPackaging\CHANGELOG.md" "$stagingDir\ApplicationPackaging\CHANGELOG.md" -Force

# Publish single-file EXE
$csproj = "$stagingDir\OracleByFPCLtd\OracleByFPCLtd.csproj"

$publishArgs = @(
    "publish", $csproj,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:Version=$Version",
    "-o", $publishDir
)

if ($StartupOptimized) {
    # Faster startup at the expense of larger package and longer publish time.
    $publishArgs += "-p:PublishReadyToRun=true"
    $publishArgs += "-p:EnableCompressionInSingleFile=false"
}

dotnet @publishArgs

if (!(Test-Path $defaultExe)) {
    throw "Publish succeeded but expected EXE not found: $defaultExe"
}

if (Test-Path $versionedExe) {
    Remove-Item $versionedExe -Force
}

Rename-Item -Path $defaultExe -NewName (Split-Path $versionedExe -Leaf)

# Copy changelog and build metadata
Copy-Item "$RepoRoot\ApplicationPackaging\CHANGELOG.md" "$releaseDir\CHANGELOG.md" -Force
@(
    "Version: $Version"
    "Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    "PublishDir: $publishDir"
    "Executable: $versionedExe"
) | Set-Content "$releaseDir\build-info.txt"

Write-Host "Package complete."
Write-Host "EXE: $versionedExe"
