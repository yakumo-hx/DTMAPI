@echo off
setlocal
set SCRIPT_DIR=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Content\DTMAPIInstaller\tools\uninstall-dtmapi.ps1"
if errorlevel 1 (
  echo.
  echo DTMAPI uninstall failed. Please run 3_check_dtmapi_status.bat and include the output when asking for help.
  pause
  exit /b 1
)
echo.
echo DTMAPI uninstall finished.
pause
