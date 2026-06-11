@echo off
setlocal
set SCRIPT_DIR=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Content\DTMAPIInstaller\tools\check-dtmapi-status.ps1"
echo.
pause
