#Requires -Version 5.1
param([switch]$NoBuild)

$ErrorActionPreference = "Stop"
$root   = $PSScriptRoot
$svcLog = "$env:TEMP\firetower-svc.log"

function Write-Step([string]$msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host $msg -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host $msg -ForegroundColor Yellow }
function Write-Fail([string]$msg) { Write-Host $msg -ForegroundColor Red }

# Kill all stale FireTower processes, including the dotnet.exe host processes
# that run the service and tray (named "dotnet", not "FireTower").
Write-Step "Stopping any running FireTower processes..."
try {
    Get-WmiObject Win32_Process |
        Where-Object { $_.CommandLine -match 'FireTower' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
} catch { }
Get-Process -Name "FireTower*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

if (-not $NoBuild) {
    Write-Step "Building FireTower..."

    # Service (also compiles Core, Data, Shared, Providers)
    dotnet build "$root\src\FireTower.Service\FireTower.Service.csproj" --nologo
    if ($LASTEXITCODE -ne 0) { Write-Fail "Service build failed."; exit 1 }

    # The WPF XAML compiler resolves temp-project paths relative to the MSBuild
    # process CWD (the repo root). obj\ must exist there so the copy succeeds.
    New-Item -Path "$root\obj" -ItemType Directory -Force | Out-Null

    # Force-restore ensures nuget.g.props / nuget.g.targets exist in obj\ even
    # after a clean (MSBuild implicit restore skips them when packages are cached).
    dotnet restore "$root\src\FireTower.Tray\FireTower.Tray.csproj" --force --nologo -q

    dotnet build "$root\src\FireTower.Tray\FireTower.Tray.csproj" --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { Write-Fail "Tray build failed."; exit 1 }

    Write-Ok "Build succeeded."
    Write-Host ""
}

Write-Step "Starting FireTower Service..."
Remove-Item $svcLog -ErrorAction SilentlyContinue

$svc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project `"$root\src\FireTower.Service\FireTower.Service.csproj`" --no-build" `
    -PassThru -NoNewWindow `
    -RedirectStandardOutput $svcLog `
    -RedirectStandardError "$env:TEMP\firetower-svc-err.log"

$deadline = (Get-Date).AddSeconds(20)
$ready    = $false
while ((Get-Date) -lt $deadline -and -not $ready) {
    Start-Sleep -Milliseconds 400
    if (Test-Path $svcLog) {
        $ready = (Select-String -Path $svcLog -Pattern "Service Ready" -Quiet)
    }
}

if ($ready) {
    Write-Ok "Service ready."
} else {
    Write-Warn "Service did not report ready within 20 s."
    Write-Warn "Check $svcLog for details."
}

Write-Host ""
Write-Step "Starting FireTower Tray..."
Write-Host "(Close the tray window or right-click tray icon and choose Exit to stop everything.)" -ForegroundColor DarkGray
Write-Host ""

try {
    dotnet run --project "$root\src\FireTower.Tray\FireTower.Tray.csproj" --no-build
} finally {
    Write-Host ""
    Write-Step "Tray closed - stopping service..."
    if ($null -ne $svc -and -not $svc.HasExited) {
        Stop-Process -Id $svc.Id -Force -ErrorAction SilentlyContinue
    }
    try {
        Get-WmiObject Win32_Process |
            Where-Object { $_.CommandLine -match 'FireTower' } |
            ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    } catch { }
    Write-Ok "Stopped."
}
