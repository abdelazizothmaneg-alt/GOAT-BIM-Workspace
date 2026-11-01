$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Installing GOAT BIM Architectural Suite (.NET 10)      " -ForegroundColor Cyan
Write-Host "   GOAT Setup | GOAT Data | GOAT Finishes | GOAT Ceiling | GOAT Documentation | GOAT Delivery" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$FilesDir  = Join-Path $ScriptDir "Files"
$SourceDir = Join-Path $FilesDir "Source"
$DllPath   = Join-Path $FilesDir "ORTools.dll"

$revitProc = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
if ($null -ne $revitProc) {
    Write-Host "WARNING: Autodesk Revit is currently running. Please close Revit and re-run Install.bat if the DLL is locked." -ForegroundColor Yellow
}

$CsprojPath = Join-Path $SourceDir "ORTools.csproj"
$BuildOutDir = Join-Path $FilesDir "BuildOutput"
$dotnetCmd = Get-Command "dotnet" -ErrorAction SilentlyContinue

if (($null -ne $dotnetCmd) -and (Test-Path $CsprojPath)) {
    Write-Host "[1/3] Compiling latest ORTools.dll (GOAT BIM Suite) from C# Source..." -ForegroundColor Yellow
    & dotnet build $CsprojPath -c Release -o $BuildOutDir | Out-Null
    if ($LASTEXITCODE -eq 0 -and (Test-Path (Join-Path $BuildOutDir "ORTools.dll"))) {
        Copy-Item (Join-Path $BuildOutDir "ORTools.dll") -Destination $FilesDir -Force
        if (Test-Path (Join-Path $BuildOutDir "ORTools.deps.json")) {
            Copy-Item (Join-Path $BuildOutDir "ORTools.deps.json") -Destination $FilesDir -Force
        }
        Write-Host "      Successfully compiled latest ORTools.dll!" -ForegroundColor Green
    } else {
        Write-Host "      Using pre-compiled ORTools.dll from Files folder..." -ForegroundColor Yellow
    }
} else {
    Write-Host "[1/3] Using pre-compiled ORTools.dll from Files folder..." -ForegroundColor Green
}

if (-not (Test-Path $DllPath)) {
    Write-Host "ERROR: ORTools.dll not found at $DllPath" -ForegroundColor Red
    exit 1
}

$Versions = @("2026", "2027")
Write-Host "[2/3] Deploying GOAT BIM Add-in files to Revit Addins folders..." -ForegroundColor Yellow

foreach ($ver in $Versions) {
    $AddinsRoot = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$ver"
    $TargetFolder = Join-Path $AddinsRoot "ORTools"

    if (-not (Test-Path $AddinsRoot)) { New-Item -ItemType Directory -Path $AddinsRoot -Force | Out-Null }
    if (-not (Test-Path $TargetFolder)) { New-Item -ItemType Directory -Path $TargetFolder -Force | Out-Null }

    Copy-Item (Join-Path $FilesDir "ORTools.addin") -Destination $AddinsRoot -Force
    Copy-Item $DllPath -Destination $TargetFolder -Force

    $DepsPath = Join-Path $FilesDir "ORTools.deps.json"
    if (Test-Path $DepsPath) { Copy-Item $DepsPath -Destination $TargetFolder -Force }

    Copy-Item (Join-Path $FilesDir "*.jpg") -Destination $TargetFolder -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $FilesDir "*.png") -Destination $TargetFolder -Force -ErrorAction SilentlyContinue

    Get-ChildItem -Path $AddinsRoot -Filter "ORTools*" -Recurse | Unblock-File -ErrorAction SilentlyContinue
    Write-Host "      -> Installed for Autodesk Revit $ver : $AddinsRoot" -ForegroundColor Green
}

Write-Host ""
Write-Host "[3/3] Installation Complete! Open Revit 2026 or 2027 and check the 'GOAT BIM' tab (6 Panels, 15 Tools)." -ForegroundColor Cyan
