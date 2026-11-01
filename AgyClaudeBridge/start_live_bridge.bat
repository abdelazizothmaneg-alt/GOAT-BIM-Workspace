@echo off
title AgyClaude Live Sync Bridge
cd /d "%~dp0"
echo ====================================================
echo  Starting AgyClaude Live Sync Bridge Daemon...
echo  Monitoring: Antigravity (Gemini) ^<--^> Claude
echo ====================================================
python bridge.py watch --interval 3
pause
