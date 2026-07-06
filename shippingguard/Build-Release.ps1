#Requires -Version 5.1
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$root    = $PSScriptRoot
$pubDir  = "$root\installer\publish"
$fragFile = "$root\installer\AppFiles.wxs"
$outDir  = "$root\installer\bin\Release"
$msi     = "$outDir\ShippingGuard-$Version.msi"
$uiExt   = "$env:USERPROFILE\.nuget\packages\wixtoolset.ui.wixext\5.0.2\wixext5\WixToolset.UI.wixext.dll"

function Write-Step([string]$m) { Write-Host $m -ForegroundColor Cyan }
function Write-Ok([string]$m)   { Write-Host $m -ForegroundColor Green }
function Write-Fail([string]$m) { Write-Host $m -ForegroundColor Red }

Write-Step "Cleaning..."
try {
    Get-WmiObject Win32_Process |
        Where-Object { $_.CommandLine -match 'ShippingGuard' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Milliseconds 500
} catch { }
Remove-Item -Recurse -Force $pubDir  -ErrorAction SilentlyContinue
Remove-Item -Force        $fragFile  -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $pubDir | Out-Null
New-Item -ItemType Directory -Force $outDir | Out-Null

foreach ($proj in @("ShippingGuard.Agent", "ShippingGuard.Tray")) {
    Remove-Item "$root\src\$proj\obj\Release" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Step "Publishing ShippingGuard.Agent (self-contained)..."
dotnet publish "$root\src\ShippingGuard.Agent\ShippingGuard.Agent.csproj" `
    -c Release -r win-x64 --self-contained -o $pubDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Fail "Agent publish failed."; exit 1 }

Write-Step "Publishing ShippingGuard.Tray (self-contained)..."
dotnet publish "$root\src\ShippingGuard.Tray\ShippingGuard.Tray.csproj" `
    -c Release -r win-x64 --self-contained -o $pubDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Fail "Tray publish failed."; exit 1 }

Write-Ok "Published $((Get-ChildItem $pubDir -File).Count) files."

Write-Step "Generating AppFiles.wxs..."
$skip = @("ShippingGuard.Agent.exe", "ShippingGuard.Tray.exe", "appsettings.Development.json")
$files = Get-ChildItem $pubDir -File | Where-Object { $_.Name -notin $skip -and $_.Extension -ne ".pdb" }
$components = ($files | ForEach-Object {
    $id = "F_" + ($_.Name -replace '[^a-zA-Z0-9]', '_')
    "      <Component><File Id=`"$id`" Name=`"$($_.Name)`" Source=`"$($_.FullName)`" KeyPath=`"yes`"/></Component>"
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

Write-Step "Building MSI v$Version..."
wix build "$root\installer\Package.wxs" $fragFile `
    -arch x64 `
    -ext $uiExt `
    -d ProductVersion=$Version `
    -d "AgentExe=$pubDir\ShippingGuard.Agent.exe" `
    -d "TrayExe=$pubDir\ShippingGuard.Tray.exe" `
    -o $msi
if ($LASTEXITCODE -ne 0) { Write-Fail "MSI build failed."; exit 1 }

$mb = [math]::Round((Get-Item $msi).Length / 1MB, 1)
Write-Ok "Done: $msi ($mb MB)"
Write-Host "  gh release create v$Version `"$msi`" --title `"ShippingGuard v$Version`"" -ForegroundColor White
