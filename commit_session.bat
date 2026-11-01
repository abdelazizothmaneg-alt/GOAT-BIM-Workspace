@echo off
setlocal
cd /d "%~dp0"

if "%~1"=="" (
    set /p msg="Enter commit message: "
) else (
    set msg=%~1
)

if "%msg%"=="" (
    set msg=Update by session [%date% %time%]
)

git add .
git commit -m "%msg%"
echo.
echo ====================================================
echo  Session changes committed safely to Git!
echo ====================================================
git status -s
pause
