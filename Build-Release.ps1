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
$root      = $PSScriptRoot
$pubDir    = "$root\installer\publish"
$fragFile  = "$root\installer\AppFiles.wxs"
$outDir    = "$root\installer\bin\Release"
$msi       = "$outDir\FireTower-$Version.msi"
$uiExt     = "$env:USERPROFILE\.nuget\packages\wixtoolset.ui.wixext\5.0.2\wixext5\WixToolset.UI.wixext.dll"

function Write-Step([string]$msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host $msg -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host $msg -ForegroundColor Red }

# ── Clean ─────────────────────────────────────────────────────────────────
Write-Step "Cleaning..."

# Kill any running FireTower processes so they cannot hold file locks.
try {
    Get-WmiObject Win32_Process |
        Where-Object { $_.CommandLine -match 'FireTower' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Milliseconds 500
} catch { }

Remove-Item -Recurse -Force $pubDir  -ErrorAction SilentlyContinue
Remove-Item -Force        $fragFile  -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $pubDir | Out-Null
New-Item -ItemType Directory -Force $outDir | Out-Null

# Clean only the two projects we publish, not the whole solution.
# Cleaning the whole solution pulls in the Tests project which can fail and
# produces hundreds of irrelevant warnings about deleting test DLLs.
foreach ($proj in @("FireTower.Service", "FireTower.Tray")) {
    $objRelease = "$root\src\$proj\obj\Release"
    Remove-Item -Recurse -Force $objRelease -ErrorAction SilentlyContinue
}

# ── Publish (framework-dependent — required for Windows Service / LocalSystem) ──
Write-Step "Publishing FireTower.Service (self-contained, win-x64)..."
dotnet publish "$root\src\FireTower.Service\FireTower.Service.csproj" `
    -c Release -r win-x64 --self-contained -o $pubDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Fail "Service publish failed."; exit 1 }

Write-Step "Publishing FireTower.Tray (self-contained, win-x64)..."
dotnet publish "$root\src\FireTower.Tray\FireTower.Tray.csproj" `
    -c Release -r win-x64 --self-contained -o $pubDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Fail "Tray publish failed."; exit 1 }

$total = (Get-ChildItem $pubDir -File).Count
Write-Ok "Published $total files."

# ── Generate WiX fragment for all DLLs ────────────────────────────────────
#
# Package.wxs manually declares FireTower.Service.exe (needs ServiceInstall)
# and FireTower.Tray.exe (needs its own component for the CA_LaunchTray
# FileRef). Everything else — all the DLLs and config files — goes into the
# CG_AppFiles component group generated here.
#
Write-Step "Generating AppFiles.wxs..."

$skip = @("FireTower.Service.exe", "FireTower.Tray.exe",
          "appsettings.Development.json")

$files = Get-ChildItem $pubDir -File | Where-Object {
    $_.Name -notin $skip -and $_.Extension -ne ".pdb"
}

$components = ($files | ForEach-Object {
    $id  = "F_" + ($_.Name -replace '[^a-zA-Z0-9]', '_')
    $src = $_.FullName
    "      <Component><File Id=`"$id`" Name=`"$($_.Name)`" Source=`"$src`" KeyPath=`"yes`"/></Component>"
}) -join "`n"

@"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="CG_AppFiles" Directory="INSTALLFOLDER">
$components
    </ComponentGroup>
  </Fragment>
</Wix>
"@ | Set-Content $fragFile -Encoding UTF8

Write-Ok "Generated $($files.Count) file components."

# ── Build MSI ─────────────────────────────────────────────────────────────
Write-Step "Building MSI v$Version..."

$svcExe  = "$pubDir\FireTower.Service.exe"
$trayExe = "$pubDir\FireTower.Tray.exe"

wix build "$root\installer\Package.wxs" $fragFile `
    -arch x64 `
    -ext $uiExt `
    -d ProductVersion=$Version `
    -d "ServiceExe=$svcExe" `
    -d "TrayExe=$trayExe" `
    -o $msi

if ($LASTEXITCODE -ne 0) { Write-Fail "MSI build failed."; exit 1 }

$mb = [math]::Round((Get-Item $msi).Length / 1MB, 1)
Write-Ok ""
Write-Ok "Done: $msi ($mb MB)"
Write-Ok ""
Write-Host "Upload to GitHub:" -ForegroundColor Yellow
Write-Host "  gh release create v$Version `"$msi`" --title `"FireTower v$Version`"" -ForegroundColor White
