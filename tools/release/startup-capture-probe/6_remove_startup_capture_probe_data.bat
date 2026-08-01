@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "DTMAPI_POWERSHELL=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
set "DTMAPI_CLEANUP_SCRIPT=%SCRIPT_DIR%Content\DTMAPIInstaller\tools\remove-startup-capture-probe-data.ps1"

if not exist "%DTMAPI_POWERSHELL%" (
  echo [ERROR] Windows PowerShell 5.1 was not found.
  pause
  exit /b 1
)
if not exist "%DTMAPI_CLEANUP_SCRIPT%" (
  echo [ERROR] Cleanup script is missing: %DTMAPI_CLEANUP_SCRIPT%
  echo [INFO] Extract the whole probe ZIP before running this file.
  pause
  exit /b 1
)

"%DTMAPI_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_CLEANUP_SCRIPT%"
set "DTMAPI_EXIT=%ERRORLEVEL%"
if "%DTMAPI_EXIT%"=="0" (
  echo.
  echo [OK] Probe data cleanup completed.
  echo [INFO] You may now delete this extracted probe folder.
) else if "%DTMAPI_EXIT%"=="2" (
  echo.
  echo [INFO] Cleanup cancelled. No probe data was deleted.
) else (
  echo.
  echo [ERROR] Probe data cleanup failed with exit code %DTMAPI_EXIT%.
)
pause
exit /b %DTMAPI_EXIT%
