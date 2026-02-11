# Packaging (Windows Single-File EXE)

## Goal
Build a single-file Windows x64 `.exe` with a packaging-time version and store each release in a dedicated version folder for long-term traceability.

## Prereqs
- Windows machine with .NET SDK 8 installed.
- Repo available on `Y:\Desktop\Development\Oracle`.
- Packaging output root: `Y:\Desktop\Development\ShippedPackages\OracleByFPC`.

## Release Layout
Each shipped version gets its own folder:

```text
Y:\Desktop\Development\ShippedPackages\OracleByFPC\
  v1.1\
    build-info.txt
    CHANGELOG.md
    source-staging\
      OracleByFPCLtd\...
      UserInterface\...
    publish\
      OracleByFPC_v1.1.exe
      OracleByFPCLtd.pdb
```

This keeps versions easy to find and compare later.

## Versioning
Use `-p:Version=<X>` on `dotnet publish`.

Examples:
- `1.0`
- `1.1`
- `1.2`

## Packaging Command (PowerShell)
Use the script in this folder:

```powershell
Y:\Desktop\Development\Oracle\ApplicationPackaging\package.ps1 -Version 1.1
```

## Manual Equivalent (PowerShell)
```powershell
$version = "1.1"
$repo = "Y:\Desktop\Development\Oracle"
$root = "Y:\Desktop\Development\ShippedPackages\OracleByFPC"
$release = Join-Path $root ("v" + $version)
$staging = Join-Path $release "source-staging"
$publish = Join-Path $release "publish"

if (Test-Path $release) {
  Remove-Item $release -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $staging | Out-Null
New-Item -ItemType Directory -Force -Path $publish | Out-Null

robocopy "$repo\OracleByFPCLtd" "$staging\OracleByFPCLtd" /E /XD bin obj
robocopy "$repo\UserInterface" "$staging\UserInterface" feeny-logo-100-circle-black-back.png

dotnet publish "$staging\OracleByFPCLtd\OracleByFPCLtd.csproj" `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:Version=$version `
  -o $publish

# Rename final EXE artifact
if (Test-Path "$publish\OracleByFPC_v$version.exe") {
  Remove-Item "$publish\OracleByFPC_v$version.exe" -Force
}
Rename-Item "$publish\OracleByFPCLtd.exe" "OracleByFPC_v$version.exe"

Copy-Item "$repo\ApplicationPackaging\CHANGELOG.md" "$release\CHANGELOG.md" -Force
"Version: $version`r`nBuilt: $(Get-Date -Format 'yyyy-MM-dd HH:mm')`r`n" | Set-Content "$release\build-info.txt"
```

## Notes
- Re-shipping a version intentionally overwrites that version folder.
- Keep the changelog in `ApplicationPackaging\CHANGELOG.md` up to date before packaging.
- If packaging fails, fix and re-run with the same version to overwrite.
- Final artifact name must be `OracleByFPC_v<version>.exe`.

## Troubleshooting
- In PowerShell, line continuation is backtick `` ` `` (not `\`).
- Use Windows paths (`Y:\...`) in PowerShell commands.
- If `MSB1009 Project file does not exist`, verify staging first:
  - `Test-Path "$staging\OracleByFPCLtd\OracleByFPCLtd.csproj"` should be `True`.
