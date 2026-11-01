$ErrorActionPreference = "Stop"

$ProjectRoot = "C:\Users\faqih\.gemini\antigravity\scratch\MaxToRevitConverter"
$DllSource   = Join-Path $ProjectRoot "RevitAddin\bin\Release\net10.0-windows\MaxToRevitBridge.dll"
$AddinSource = Join-Path $ProjectRoot "RevitAddin\MaxToRevitBridge.addin"
$MainMs      = Join-Path $ProjectRoot "MaxScript\MaxToRevitConverter.ms"
$StartupMs   = Join-Path $ProjectRoot "MaxScript\MaxToRevit_StartupMacro.ms"

Write-Host "=== Installing MaxToRevit Bridge Pro ===" -ForegroundColor Cyan

# 1. Install Revit 2026 Add-in into %APPDATA%\Autodesk\Revit\Addins\2026
$RevitUserAddins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2026"
New-Item -ItemType Directory -Force -Path $RevitUserAddins | Out-Null

$DestDllDir = Join-Path $RevitUserAddins "MaxToRevitBridge"
New-Item -ItemType Directory -Force -Path $DestDllDir | Out-Null
$DestDllPath = Join-Path $DestDllDir "MaxToRevitBridge.dll"

if (Test-Path $DllSource) {
    Copy-Item -Path $DllSource -Destination $DestDllPath -Force
    Write-Host "[OK] Copied MaxToRevitBridge.dll -> $DestDllPath" -ForegroundColor Green
}

$AddinXml = @"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>MaxToRevit Bridge Pro</Name>
    <Assembly>$DestDllPath</Assembly>
    <AddInId>8D8A4762-9F53-42E6-B8C2-9A5E14B12026</AddInId>
    <FullClassName>MaxToRevitBridge.App</FullClassName>
    <VendorId>M2RB</VendorId>
    <VendorDescription>MaxToRevit Bridge Pro — 3ds Max to Revit Family/Project Converter</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

$DestAddinFile = Join-Path $RevitUserAddins "MaxToRevitBridge.addin"
Set-Content -Path $DestAddinFile -Value $AddinXml -Encoding UTF8
Write-Host "[OK] Installed Revit 2026 Add-in Manifest -> $DestAddinFile" -ForegroundColor Green

# 2. Install Startup MacroScript into 3ds Max (2025, 2026, 2027) User Startup Scripts
$MaxLocalAppData = Join-Path $env:LOCALAPPDATA "Autodesk\3dsMax"
if (Test-Path $MaxLocalAppData) {
    $MaxVersionDirs = Get-ChildItem -Path $MaxLocalAppData -Directory -ErrorAction SilentlyContinue
    foreach ($verDir in $MaxVersionDirs) {
        $enuDirs = Get-ChildItem -Path $verDir.FullName -Directory -ErrorAction SilentlyContinue
        foreach ($langDir in $enuDirs) {
            $startupDir = Join-Path $langDir.FullName "scripts\startup"
            if (Test-Path (Join-Path $langDir.FullName "scripts")) {
                New-Item -ItemType Directory -Force -Path $startupDir | Out-Null
                $destStartup = Join-Path $startupDir "MaxToRevit_StartupMacro.ms"
                Copy-Item -Path $StartupMs -Destination $destStartup -Force
                Write-Host "[OK] Installed 3ds Max Startup MacroScript -> $destStartup" -ForegroundColor Green
            }
        }
    }
}

# 3. Ensure Bridge Queue/Status Folders exist
$BridgeBase = Join-Path $env:LOCALAPPDATA "MaxToRevitBridge"
New-Item -ItemType Directory -Force -Path (Join-Path $BridgeBase "Queue") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $BridgeBase "Staging") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $BridgeBase "Processed") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $BridgeBase "Status") | Out-Null
Write-Host "[OK] Initialized Bridge Queue Folders -> $BridgeBase" -ForegroundColor Green

Write-Host "=== Installation Complete! ===" -ForegroundColor Cyan
