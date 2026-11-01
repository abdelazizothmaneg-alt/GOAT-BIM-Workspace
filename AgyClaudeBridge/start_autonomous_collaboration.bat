@echo off
title Antigravity <-> Claude Autonomous Collaboration Loop
cd /d "%~dp0"
echo ==========================================================
echo  Starting Autonomous Git Collaboration Daemon...
echo  Claude pushes to GitHub --> Antigravity builds locally!
echo ==========================================================
python agent_sync_loop.py 10
pause
