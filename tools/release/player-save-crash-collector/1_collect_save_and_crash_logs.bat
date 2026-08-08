@echo off
setlocal EnableExtensions DisableDelayedExpansion
title DTMAPI Player Support Collector
set "DTMAPI_SUPPORT_SCRIPT=%~dp0collect-save-and-crash-logs.ps1"
if not exist "%DTMAPI_SUPPORT_SCRIPT%" (
  echo [ERROR] Collector script is missing:
  echo "%DTMAPI_SUPPORT_SCRIPT%"
  if not defined DTMAPI_SUPPORT_NO_PAUSE pause
  exit /b 1
)

echo Close Doloc Town before continuing.
echo Collecting the complete SAVE folder and crash logs. Source files are read-only.
echo.
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_SUPPORT_SCRIPT%"
set "DTMAPI_SUPPORT_EXIT=%ERRORLEVEL%"
echo.
if "%DTMAPI_SUPPORT_EXIT%"=="0" (
  echo [OK] Collection completed. Send the ZIP shown above.
) else (
  echo [ERROR] Collection was incomplete. Read the message above and retry if instructed.
)
if not defined DTMAPI_SUPPORT_NO_PAUSE pause
exit /b %DTMAPI_SUPPORT_EXIT%
