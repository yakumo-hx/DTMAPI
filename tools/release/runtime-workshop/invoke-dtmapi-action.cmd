@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "DTMAPI_ACTION=%~1"
set "DTMAPI_TOOLS=%~dp0"
set "DTMAPI_HOST_PROBE=%DTMAPI_TOOLS%probe-powershell-host.ps1"
set "DTMAPI_TARGET="
set "DTMAPI_SUCCESS_MESSAGE="
set "DTMAPI_FAILURE_MESSAGE="
set "DTMAPI_HELP_MESSAGE="

if /i "%DTMAPI_ACTION%"=="install" goto dtmapi_configure_install
if /i "%DTMAPI_ACTION%"=="uninstall" goto dtmapi_configure_uninstall
if /i "%DTMAPI_ACTION%"=="status" goto dtmapi_configure_status
if /i "%DTMAPI_ACTION%"=="collect" goto dtmapi_configure_collect
echo [ERROR] Unknown DTMAPI installer action: "%DTMAPI_ACTION%"
call :dtmapi_pause_if_needed
exit /b 2

:dtmapi_configure_install
set "DTMAPI_TARGET=%DTMAPI_TOOLS%install-to-game.ps1"
set "DTMAPI_SUCCESS_MESSAGE=[DTM-S1001] DTMAPI install script finished; use the bilingual summary above as the authoritative result."
set "DTMAPI_FAILURE_MESSAGE=DTMAPI install failed."
set "DTMAPI_HELP_MESSAGE=Run 3_check_dtmapi_status.bat and include the whole output when asking for help."
goto dtmapi_configured

:dtmapi_configure_uninstall
set "DTMAPI_TARGET=%DTMAPI_TOOLS%uninstall-dtmapi.ps1"
set "DTMAPI_SUCCESS_MESSAGE=DTMAPI uninstall script finished; use the bilingual S2001/S2002 summary above as the authoritative result."
set "DTMAPI_FAILURE_MESSAGE=DTMAPI uninstall failed."
set "DTMAPI_HELP_MESSAGE=Run 3_check_dtmapi_status.bat and include the whole output when asking for help."
goto dtmapi_configured

:dtmapi_configure_status
set "DTMAPI_TARGET=%DTMAPI_TOOLS%check-dtmapi-status.ps1"
set "DTMAPI_SUCCESS_MESSAGE=[DTM-S3001] DTMAPI status check finished; use the single final state above as the authoritative result."
set "DTMAPI_FAILURE_MESSAGE=DTMAPI status check reported problems."
set "DTMAPI_HELP_MESSAGE=Include the whole output above when asking for help."
goto dtmapi_configured

:dtmapi_configure_collect
set "DTMAPI_TARGET=%DTMAPI_TOOLS%collect-logs.ps1"
set "DTMAPI_SUCCESS_MESSAGE=DTMAPI log collection finished."
set "DTMAPI_FAILURE_MESSAGE=DTMAPI log collection failed."
set "DTMAPI_HELP_MESSAGE=Include the whole output above when asking for help."
goto dtmapi_configured

:dtmapi_configured
if not exist "%DTMAPI_HOST_PROBE%" (
  echo [DTM-E1201] PowerShell host probe script is missing: "%DTMAPI_HOST_PROBE%"
  call :dtmapi_pause_if_needed
  exit /b 1
)
if not exist "%DTMAPI_TARGET%" (
  echo [DTM-E1201] DTMAPI action script is missing: "%DTMAPI_TARGET%"
  call :dtmapi_pause_if_needed
  exit /b 1
)

set "DTMAPI_POWERSHELL="
if defined DTMAPI_POWERSHELL_HOST (
  echo [INFO] PowerShell host override: "%DTMAPI_POWERSHELL_HOST%"
  call :dtmapi_try_powershell "%DTMAPI_POWERSHELL_HOST%"
  if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
  echo [DTM-E1101] The configured DTMAPI_POWERSHELL_HOST did not pass the installer capability probe.
  goto dtmapi_no_powershell
)

call :dtmapi_try_powershell "%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
for /f "delims=" %%P in ('where powershell.exe 2^>nul') do (
  call :dtmapi_try_powershell "%%P"
  if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
)
call :dtmapi_try_powershell "%ProgramFiles%\PowerShell\7\pwsh.exe"
if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
call :dtmapi_try_powershell "%ProgramFiles(x86)%\PowerShell\7\pwsh.exe"
if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
for /f "delims=" %%P in ('where pwsh.exe 2^>nul') do (
  call :dtmapi_try_powershell "%%P"
  if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
)

:dtmapi_no_powershell
echo [DTM-E1101] No PowerShell host passed the DTMAPI installer capability probe. Repair Windows PowerShell language-mode/application-control restrictions, or install PowerShell 7, then retry.
call :dtmapi_pause_if_needed
exit /b 1

:dtmapi_powershell_found
echo [INFO] Using PowerShell host: "%DTMAPI_POWERSHELL%"
echo [INFO] Running DTMAPI action: %DTMAPI_ACTION%
if /i "%DTMAPI_ACTION%"=="install" (
  "%DTMAPI_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_TARGET%" -InstallBepInEx
) else if /i "%DTMAPI_ACTION%"=="collect" (
  "%DTMAPI_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_TARGET%" -CaseId PLAYER-CRASH -DesktopTimestampOutput
) else (
  "%DTMAPI_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_TARGET%"
)
set "DTMAPI_EXIT=%ERRORLEVEL%"
echo.
if "%DTMAPI_EXIT%"=="0" (
  echo [OK] %DTMAPI_SUCCESS_MESSAGE%
  echo [OK] Host used: "%DTMAPI_POWERSHELL%"
) else (
  echo [ERROR] %DTMAPI_FAILURE_MESSAGE%
  echo [ERROR] Host used: "%DTMAPI_POWERSHELL%"
  echo [ERROR] Exit code: %DTMAPI_EXIT%
  if defined DTMAPI_HELP_MESSAGE echo [INFO] %DTMAPI_HELP_MESSAGE%
)
call :dtmapi_pause_if_needed
exit /b %DTMAPI_EXIT%

:dtmapi_try_powershell
set "DTMAPI_PS_CANDIDATE=%~1"
if not exist "%DTMAPI_PS_CANDIDATE%" exit /b 0
set "DTMAPI_PROBE_SESSION="
set "DTMAPI_PROBE_SESSION_ATTEMPTS=0"
:dtmapi_claim_probe_session
set /a DTMAPI_PROBE_SESSION_ATTEMPTS+=1
if %DTMAPI_PROBE_SESSION_ATTEMPTS% GTR 256 goto dtmapi_probe_session_failed
set "DTMAPI_PROBE_NONCE=p-%RANDOM%-%RANDOM%-%RANDOM%-%DTMAPI_PROBE_SESSION_ATTEMPTS%"
set "DTMAPI_PROBE_SESSION=%TEMP%\dtmapi-host-probe-%DTMAPI_PROBE_NONCE%"
2>nul md "%DTMAPI_PROBE_SESSION%"
if errorlevel 1 goto dtmapi_claim_probe_session
set "DTMAPI_PROBE_RESULT_PATH=%DTMAPI_PROBE_SESSION%\result.txt"
set "DTMAPI_PROBE_RESULT="
echo [INFO] Probing PowerShell host: "%DTMAPI_PS_CANDIDATE%"
"%DTMAPI_PS_CANDIDATE%" -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_HOST_PROBE%" -ToolsRoot "%DTMAPI_TOOLS%." -Action "%DTMAPI_ACTION%" -ProbeNonce "%DTMAPI_PROBE_NONCE%" -ProbeResultPath "%DTMAPI_PROBE_RESULT_PATH%"
set "DTMAPI_PROBE_EXIT=%ERRORLEVEL%"
if exist "%DTMAPI_PROBE_RESULT_PATH%" set /p DTMAPI_PROBE_RESULT=<"%DTMAPI_PROBE_RESULT_PATH%"
if exist "%DTMAPI_PROBE_RESULT_PATH%" del /f /q "%DTMAPI_PROBE_RESULT_PATH%" >nul 2>nul
rd "%DTMAPI_PROBE_SESSION%" >nul 2>nul
if "%DTMAPI_PROBE_EXIT%"=="0" if /i "%DTMAPI_PROBE_RESULT%"=="DTMAPI_PROBE_OK_%DTMAPI_PROBE_NONCE%_%DTMAPI_ACTION%" set "DTMAPI_POWERSHELL=%DTMAPI_PS_CANDIDATE%"
if not "%DTMAPI_PROBE_EXIT%"=="0" echo [WARN] PowerShell host probe failed: "%DTMAPI_PS_CANDIDATE%" exit=%DTMAPI_PROBE_EXIT%
if "%DTMAPI_PROBE_EXIT%"=="0" if not defined DTMAPI_POWERSHELL echo [WARN] PowerShell host returned success without the required invocation proof: "%DTMAPI_PS_CANDIDATE%"
exit /b 0

:dtmapi_probe_session_failed
echo [WARN] Could not allocate a unique PowerShell probe session for: "%DTMAPI_PS_CANDIDATE%"
exit /b 0

:dtmapi_pause_if_needed
if not defined DTMAPI_NO_PAUSE pause
exit /b 0
