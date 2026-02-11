# Packaging (Windows Single-File EXE)

## Goal
Build a single-file Windows x64 `.exe` from this repo without modifying source version numbers. Version is injected only at packaging time via `dotnet publish`.

## Prereqs
- Windows machine with .NET SDK 8 installed.
- Repo available on `Y:\Desktop\Development\Oracle` (adjust if needed).

## Versioning Strategy (Option A)
Use `-p:Version=<X>` on `dotnet publish` to set the version displayed in the app.

Example versions:
- `1.0` (first package)
- `1.1` (next package)
- `1.2` (next package)

## Packaging Steps

1. Pick the next version (example: `1.1`).
2. Use a clean staging folder (prevents repo pollution).
3. Run `dotnet publish` with the version parameter.

### Example Commands (Windows PowerShell)
```powershell
# 1) Create a staging folder
mkdir Y:\Desktop\Development\TestPackages\OracleByFPCLtd-staging

# 2) Copy only required project files (exclude bin/obj)
robocopy Y:\Desktop\Development\Oracle\OracleByFPCLtd `
  Y:\Desktop\Development\TestPackages\OracleByFPCLtd-staging\OracleByFPCLtd `
  /E /XD bin obj

# 3) Ensure the UI image resource exists in staging
mkdir Y:\Desktop\Development\TestPackages\OracleByFPCLtd-staging\UserInterface
copy Y:\Desktop\Development\Oracle\UserInterface\feeny-logo-100-circle-black-back.png `
  Y:\Desktop\Development\TestPackages\OracleByFPCLtd-staging\UserInterface

# 4) Publish single-file, self-contained, versioned EXE
dotnet publish Y:\Desktop\Development\TestPackages\OracleByFPCLtd-staging\OracleByFPCLtd\OracleByFPCLtd.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:Version=1.1 `
  -o Y:\Desktop\Development\TestPackages\OracleByFPCLtd-staging\publish
```

## Output
The EXE will be at:
```
Y:\Desktop\Development\TestPackages\OracleByFPCLtd-staging\publish\OracleByFPCLtd.exe
```

## Notes
- Only the `-p:Version=` value changes for each package.
- This version should surface in the app title and Help → About once the UI is wired to `AssemblyInformationalVersion` or `AssemblyVersion`.
