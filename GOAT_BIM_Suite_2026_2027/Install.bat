@echo off
title GOAT BIM Suite Installer (Revit 2026 / 2027)
echo ==========================================================
echo    GOAT BIM Architectural Suite — One-Click Installer
echo ==========================================================
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
echo.
pause
