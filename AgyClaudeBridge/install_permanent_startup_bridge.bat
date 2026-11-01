@echo off
setlocal
cd /d "%~dp0"

echo ==========================================================
echo  Installing AgyClaudeBridge into Windows Startup...
echo ==========================================================

set "TARGET_BAT=%~dp0start_autonomous_collaboration.bat"
set "SHORTCUT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\AgyClaudeBridge.lnk"

powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%SHORTCUT%'); $s.TargetPath = '%TARGET_BAT%'; $s.WorkingDirectory = '%~dp0'; $s.WindowStyle = 7; $s.Save()"

if exist "%SHORTCUT%" (
    echo [SUCCESS] Permanent Bridge installed to Windows Startup!
    echo It will start silently every time your computer boots.
) else (
    echo [ERROR] Could not create startup shortcut.
)

pause
