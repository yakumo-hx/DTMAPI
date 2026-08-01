@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "DTMAPI_POWERSHELL=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
set "DTMAPI_CAPTURE_SCRIPT=%SCRIPT_DIR%Content\DTMAPIInstaller\tools\capture-startup-trace.ps1"

if not exist "%DTMAPI_POWERSHELL%" (
  echo [ERROR] Windows PowerShell 5.1 was not found.
  pause
  exit /b 1
)
if not exist "%DTMAPI_CAPTURE_SCRIPT%" (
  echo [ERROR] Capture script is missing: %DTMAPI_CAPTURE_SCRIPT%
  echo [INFO] Extract the whole probe ZIP before running this file.
  pause
  exit /b 1
)

fltmc >nul 2>&1
if errorlevel 1 (
  echo [INFO] Requesting administrator permission for Microsoft Process Monitor...
  "%DTMAPI_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -WorkingDirectory '%~dp0' -Verb RunAs"
  if errorlevel 1 (
    echo [ERROR] Administrator permission was cancelled or failed.
    pause
    exit /b 1
  )
  exit /b 0
)

echo [INFO] Starting the DTMAPI startup capture probe...
"%DTMAPI_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_CAPTURE_SCRIPT%" -SkipElevation
set "DTMAPI_EXIT=%ERRORLEVEL%"
if "%DTMAPI_EXIT%"=="0" (
  echo.
  echo [OK] Capture completed. Send the ZIP path printed above to the maintainer.
) else if "%DTMAPI_EXIT%"=="2" (
  echo.
  echo [INFO] Capture cancelled.
) else (
  echo.
  echo [ERROR] Capture failed with exit code %DTMAPI_EXIT%.
  echo [INFO] Send DTMAPI-startup-capture-last-error.txt if it was created on the Desktop.
)
pause
exit /b %DTMAPI_EXIT%
