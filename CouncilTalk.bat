@echo off
chcp 65001 > nul
title The Dual-Agent Council — Antigravity ⇄ Claude
cd /d "%~dp0"
python talk.py
pause
