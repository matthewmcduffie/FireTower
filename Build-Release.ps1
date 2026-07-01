#Requires -Version 5.1
<#
.SYNOPSIS
    Builds a release-ready MSI installer for FireTower.

.PARAMETER Version
    Version number written into the MSI (default 1.0.0).

.EXAMPLE
    .\Build-Release.ps1
    .\Build-Release.ps1 -Version 1.1.0
#>
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$root    = $PSScriptRoot
$publish = "$root\installer\publish"

function Write-Step([string]$msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host $msg -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host $msg -ForegroundColor Red }

# ── Clean publish folder ───────────────────────────────────────────────────
Write-Step "Cleaning publish output..."
Remove-Item -Recurse -Force $publish -ErrorAction SilentlyContinue
New-Item   -ItemType Directory -Force $publish | Out-Null

# ── Publish Service (includes all runtime DLLs) ───────────────────────────
Write-Step "Publishing FireTower.Service..."
dotnet publish "$root\src\FireTower.Service\FireTower.Service.csproj" `
    -c Release -o $publish --nologo -q
if ($LASTEXITCODE -ne 0) { Write-Fail "Service publish failed."; exit 1 }

# ── Publish Tray (shared DLLs overwrite; they are identical) ─────────────
Write-Step "Publishing FireTower.Tray..."
dotnet publish "$root\src\FireTower.Tray\FireTower.Tray.csproj" `
    -c Release -o $publish --nologo -q
if ($LASTEXITCODE -ne 0) { Write-Fail "Tray publish failed."; exit 1 }

Write-Ok ("Published " + (Get-ChildItem $publish -File).Count + " files.")

# ── Build MSI ─────────────────────────────────────────────────────────────
Write-Step "Building MSI (v$Version)..."
$msiOut = "$root\installer\bin\Release\FireTower-$Version.msi"
New-Item -ItemType Directory -Force "$root\installer\bin\Release" | Out-Null

wix build "$root\installer\Package.wxs" `
    -arch x64 `
    -ext "$env:USERPROFILE\.nuget\packages\wixtoolset.ui.wixext\5.0.2\wixext5\WixToolset.UI.wixext.dll" `
    -d ProductVersion=$Version `
    -d PublishDir="$publish\" `
    -o $msiOut

if ($LASTEXITCODE -ne 0) { Write-Fail "MSI build failed."; exit 1 }

$size = [math]::Round((Get-Item $msiOut).Length / 1MB, 1)
Write-Ok ""
Write-Ok "Release build complete: $msiOut ($size MB)"
Write-Ok ""
Write-Host "To create a GitHub release and upload:" -ForegroundColor Yellow
Write-Host "  gh release create v$Version `"$msiOut`" --title `"FireTower v$Version`" --notes `"Release v$Version`"" -ForegroundColor White
