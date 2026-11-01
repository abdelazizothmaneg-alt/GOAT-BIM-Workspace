Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Uninstalling OR Architectural Tools (Revit 2026 & 2027) " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

$Versions = @("2026", "2027")

foreach ($ver in $Versions) {
    $AddinsRoot = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$ver"
    $TargetFolder = Join-Path $AddinsRoot "ORTools"
    $ManifestFile = Join-Path $AddinsRoot "ORTools.addin"

    if (Test-Path $TargetFolder) {
        Remove-Item -Path $TargetFolder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Removed folder: $TargetFolder" -ForegroundColor Yellow
    }

    if (Test-Path $ManifestFile) {
        Remove-Item -Path $ManifestFile -Force -ErrorAction SilentlyContinue
        Write-Host "Removed manifest: $ManifestFile" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "OR Tools has been completely uninstalled from Revit 2026 and 2027." -ForegroundColor Green
