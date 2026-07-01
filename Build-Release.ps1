#Requires -Version 5.1
<#
.SYNOPSIS
    Builds a release-ready MSI installer for FireTower.
    Each application is published as a single self-contained executable so the
    installer ships exactly two files and has no .NET runtime prerequisite.

.PARAMETER Version
    Version number written into the MSI (default 1.0.0).

.EXAMPLE
    .\Build-Release.ps1
    .\Build-Release.ps1 -Version 1.0.1
#>
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$root    = $PSScriptRoot
$pubDir  = "$root\installer\publish"
$svcExe  = "$pubDir\FireTower.Service.exe"
$trayExe = "$pubDir\FireTower.Tray.exe"
$msiOut  = "$root\installer\bin\Release\FireTower-$Version.msi"

function Write-Step([string]$msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host $msg -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host $msg -ForegroundColor Red }

# ── Clean ─────────────────────────────────────────────────────────────────
Write-Step "Cleaning publish output..."
Remove-Item -Recurse -Force $pubDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $pubDir | Out-Null
New-Item -ItemType Directory -Force "$root\installer\bin\Release" | Out-Null

# ── Publish as single self-contained executables ──────────────────────────
# Each app is compiled into one .exe that bundles the .NET runtime and all
# dependencies — no extra DLLs to manage in the installer.

Write-Step "Publishing FireTower.Service (self-contained single file)..."
dotnet publish "$root\src\FireTower.Service\FireTower.Service.csproj" `
    -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -o $pubDir --nologo -q
if ($LASTEXITCODE -ne 0) { Write-Fail "Service publish failed."; exit 1 }

Write-Step "Publishing FireTower.Tray (self-contained single file)..."
dotnet publish "$root\src\FireTower.Tray\FireTower.Tray.csproj" `
    -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -o $pubDir --nologo -q
if ($LASTEXITCODE -ne 0) { Write-Fail "Tray publish failed."; exit 1 }

if (-not (Test-Path $svcExe))  { Write-Fail "FireTower.Service.exe not found after publish."; exit 1 }
if (-not (Test-Path $trayExe)) { Write-Fail "FireTower.Tray.exe not found after publish."; exit 1 }

$svcMB  = [math]::Round((Get-Item $svcExe).Length  / 1MB, 1)
$trayMB = [math]::Round((Get-Item $trayExe).Length / 1MB, 1)
Write-Ok "Service: $svcMB MB    Tray: $trayMB MB"

# ── Build MSI ─────────────────────────────────────────────────────────────
Write-Step "Building MSI v$Version..."

wix build "$root\installer\Package.wxs" `
    -arch x64 `
    -ext "$env:USERPROFILE\.nuget\packages\wixtoolset.ui.wixext\5.0.2\wixext5\WixToolset.UI.wixext.dll" `
    -d ProductVersion=$Version `
    -d "ServiceExe=$svcExe" `
    -d "TrayExe=$trayExe" `
    -o $msiOut

if ($LASTEXITCODE -ne 0) { Write-Fail "MSI build failed."; exit 1 }

$msiMB = [math]::Round((Get-Item $msiOut).Length / 1MB, 1)
Write-Ok ""
Write-Ok "Release build complete: $msiOut ($msiMB MB)"
Write-Ok ""
Write-Host "Upload to GitHub:" -ForegroundColor Yellow
Write-Host "  gh release create v$Version `"$msiOut`" --title `"FireTower v$Version`" --notes `"See commit history for changes.`"" -ForegroundColor White
