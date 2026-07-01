#Requires -Version 5.1
<#
.SYNOPSIS
    Builds a release-ready MSI installer for FireTower.

.PARAMETER Version
    Version number written into the MSI (default 1.0.0).

.EXAMPLE
    .\Build-Release.ps1
    .\Build-Release.ps1 -Version 1.0.2
#>
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$root   = $PSScriptRoot
$outDir = "$root\installer\bin\Release"
$msi    = "$outDir\FireTower-$Version.msi"

function Write-Step([string]$msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host $msg -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host $msg -ForegroundColor Red }

# Clean old publish output so harvesting picks up only current files
Write-Step "Cleaning previous publish output..."
Remove-Item -Recurse -Force "$root\installer\publish" -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $outDir | Out-Null

#
# dotnet msbuild drives the whole pipeline:
#   1. PublishApps target runs first (BeforeTargets="Build") publishing
#      Service + Tray to installer\publish\ (framework-dependent, all DLLs).
#   2. WiX HarvestDirectory generates a component group from the publish output.
#   3. WiX build links everything into the MSI.
#
Write-Step "Building MSI v$Version (publish + harvest + link)..."
dotnet msbuild "$root\installer\FireTower.Installer.wixproj" `
    -p:Configuration=Release `
    -p:ProductVersion=$Version `
    -p:OutputPath="$outDir\\" `
    -t:Build `
    --nologo

if ($LASTEXITCODE -ne 0) { Write-Fail "MSI build failed."; exit 1 }

# wixproj outputs FireTower.msi; rename to include version number
$built = "$outDir\FireTower.msi"
if (Test-Path $built) { Move-Item $built $msi -Force }

if (-not (Test-Path $msi)) { Write-Fail "MSI not found at $msi"; exit 1 }

$mb = [math]::Round((Get-Item $msi).Length / 1MB, 1)
Write-Ok ""
Write-Ok "Done: $msi ($mb MB)"
Write-Ok ""
Write-Host "Upload to GitHub:" -ForegroundColor Yellow
Write-Host "  gh release create v$Version `"$msi`" --title `"FireTower v$Version`"" -ForegroundColor White
