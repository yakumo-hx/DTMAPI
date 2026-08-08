@echo off
setlocal EnableExtensions DisableDelayedExpansion
title DTMAPI Verified Slot 0 Recovery
set "DTMAPI_RECOVERY_SCRIPT=%~dp0restore-verified-slot.ps1"
set "DTMAPI_RECOVERY_MANIFEST=%~dp0recovery-manifest.json"
set "DTMAPI_RECOVERY_SOURCE=%~dp0verified-recovery-source.data"

if exist "%DTMAPI_RECOVERY_SCRIPT%" goto script_ok
echo [ERROR] Recovery script is missing.
goto missing_file

:script_ok
if exist "%DTMAPI_RECOVERY_MANIFEST%" goto manifest_ok
echo [ERROR] Recovery manifest is missing.
goto missing_file

:manifest_ok
if exist "%DTMAPI_RECOVERY_SOURCE%" goto source_ok
echo [ERROR] Verified recovery source is missing.
goto missing_file

:source_ok
echo Fully exit Doloc Town and Steam before continuing.
echo This restores only doloc-save-0.data from the verified encrypted recovery source.
echo No new player-side backup will be created.
echo.
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_RECOVERY_SCRIPT%" -ManifestPath "%DTMAPI_RECOVERY_MANIFEST%" -RecoverySourcePath "%DTMAPI_RECOVERY_SOURCE%"
set "DTMAPI_RECOVERY_EXIT=%ERRORLEVEL%"
echo.
if "%DTMAPI_RECOVERY_EXIT%"=="0" goto recovery_ok
echo [ERROR] Recovery was not completed. Read the message above.
goto finish

:recovery_ok
echo [OK] Recovery completed. You may start Steam and Doloc Town.
goto finish

:missing_file
set "DTMAPI_RECOVERY_EXIT=10"

:finish
if defined DTMAPI_RECOVERY_NO_PAUSE goto no_pause
pause

:no_pause
exit /b %DTMAPI_RECOVERY_EXIT%
