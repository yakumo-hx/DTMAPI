@echo off
setlocal
set SCRIPT_DIR=%~dp0
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set STAMP=%%i
set OUTPUT_DIR=%USERPROFILE%\Desktop\DTMAPI-logs\%STAMP%
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Content\DTMAPIInstaller\tools\collect-logs.ps1" -CaseId PLAYER-CRASH -OutputDirectory "%OUTPUT_DIR%"
if errorlevel 1 (
  echo.
  echo Failed to collect DTMAPI logs. Please run 3_check_dtmapi_status.bat and include the output when asking for help.
  pause
  exit /b 1
)
echo.
echo DTMAPI logs collected:
echo %OUTPUT_DIR%
echo Please send this folder when asking for help after a crash.
pause
