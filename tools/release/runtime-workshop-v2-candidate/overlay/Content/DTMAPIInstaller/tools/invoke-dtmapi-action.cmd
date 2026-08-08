@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "DTMAPI_ACTION=%~1"
set "DTMAPI_TOOLS_ROOT=%~dp0"
set "DTMAPI_PROBE=%DTMAPI_TOOLS_ROOT%probe-powershell-host.ps1"
set "DTMAPI_SCRIPT="
set "DTMAPI_HOST="
set "DTMAPI_EXIT=1"

if /i "%DTMAPI_ACTION%"=="install" set "DTMAPI_SCRIPT=%DTMAPI_TOOLS_ROOT%install-dtmapi.ps1"
if /i "%DTMAPI_ACTION%"=="uninstall" set "DTMAPI_SCRIPT=%DTMAPI_TOOLS_ROOT%uninstall-dtmapi.ps1"
if /i "%DTMAPI_ACTION%"=="full-uninstall" set "DTMAPI_SCRIPT=%DTMAPI_TOOLS_ROOT%uninstall-dtmapi.ps1"
if /i "%DTMAPI_ACTION%"=="check" set "DTMAPI_SCRIPT=%DTMAPI_TOOLS_ROOT%check-dtmapi-status.ps1"
if /i "%DTMAPI_ACTION%"=="collect" set "DTMAPI_SCRIPT=%DTMAPI_TOOLS_ROOT%collect-logs.ps1"

if not defined DTMAPI_SCRIPT goto dtmapi_bad_action
if not exist "%DTMAPI_PROBE%" goto dtmapi_missing_probe
if not exist "%DTMAPI_SCRIPT%" goto dtmapi_missing_script

call :dtmapi_try_host "%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if defined DTMAPI_HOST goto dtmapi_run
call :dtmapi_try_host "%ProgramFiles%\PowerShell\7\pwsh.exe"
if defined DTMAPI_HOST goto dtmapi_run
call :dtmapi_try_programfiles_x86
if defined DTMAPI_HOST goto dtmapi_run
goto dtmapi_no_host

:dtmapi_try_programfiles_x86
if "%ProgramFiles(x86)%"=="" exit /b 1
call :dtmapi_try_host "%ProgramFiles(x86)%\PowerShell\7\pwsh.exe"
exit /b %ERRORLEVEL%

:dtmapi_try_host
set "DTMAPI_CANDIDATE=%~1"
if not exist "%DTMAPI_CANDIDATE%" exit /b 1
set "DTMAPI_SESSION_ATTEMPTS=0"
:dtmapi_new_probe_session
set /a DTMAPI_SESSION_ATTEMPTS+=1
if %DTMAPI_SESSION_ATTEMPTS% GTR 20 exit /b 1
set "DTMAPI_TOKEN=%RANDOM%-%RANDOM%-%RANDOM%"
set "DTMAPI_PROBE_SESSION=%TEMP%\dtmapi-host-probe-%DTMAPI_TOKEN%"
2>nul md "%DTMAPI_PROBE_SESSION%"
if errorlevel 1 goto dtmapi_new_probe_session
set "DTMAPI_RESULT=%DTMAPI_PROBE_SESSION%\result.txt"
set "DTMAPI_RESULT_LINE="
"%DTMAPI_CANDIDATE%" -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%DTMAPI_PROBE%" -Token "%DTMAPI_TOKEN%" -Action "%DTMAPI_ACTION%" -ToolsRoot "%DTMAPI_TOOLS_ROOT%." -ResultPath "%DTMAPI_RESULT%"
if errorlevel 1 goto dtmapi_probe_rejected
if not exist "%DTMAPI_RESULT%" goto dtmapi_probe_rejected
set /p DTMAPI_RESULT_LINE=<"%DTMAPI_RESULT%"
if /i not "%DTMAPI_RESULT_LINE%"=="DTMAPI-PROBE:%DTMAPI_TOKEN%:%DTMAPI_ACTION%" goto dtmapi_probe_rejected
set "DTMAPI_HOST=%DTMAPI_CANDIDATE%"
del /q "%DTMAPI_RESULT%" >nul 2>nul
rd "%DTMAPI_PROBE_SESSION%" >nul 2>nul
exit /b 0

:dtmapi_probe_rejected
del /q "%DTMAPI_RESULT%" >nul 2>nul
rd "%DTMAPI_PROBE_SESSION%" >nul 2>nul
exit /b 1

:dtmapi_run
echo [INFO] Using PowerShell host: "%DTMAPI_HOST%"
echo [INFO] Running DTMAPI action: %DTMAPI_ACTION%
if /i "%DTMAPI_ACTION%"=="full-uninstall" goto dtmapi_run_full
"%DTMAPI_HOST%" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_SCRIPT%"
set "DTMAPI_EXIT=%ERRORLEVEL%"
goto dtmapi_action_done

:dtmapi_run_full
"%DTMAPI_HOST%" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_SCRIPT%" -Full
set "DTMAPI_EXIT=%ERRORLEVEL%"

:dtmapi_action_done
if "%DTMAPI_EXIT%"=="0" echo [OK] DTMAPI action finished: %DTMAPI_ACTION%
if not "%DTMAPI_EXIT%"=="0" echo [ERROR] DTMAPI action failed: %DTMAPI_ACTION% ^(exit %DTMAPI_EXIT%^)
goto dtmapi_finish

:dtmapi_bad_action
echo [ERROR] Unknown DTMAPI action: "%DTMAPI_ACTION%"
set "DTMAPI_EXIT=64"
goto dtmapi_finish

:dtmapi_missing_probe
echo [ERROR] DTMAPI PowerShell probe is missing.
goto dtmapi_common_help

:dtmapi_missing_script
echo [ERROR] DTMAPI action script is missing.
goto dtmapi_common_help

:dtmapi_no_host
echo [ERROR] Windows PowerShell 5.1 and PowerShell 7 could not run the DTMAPI action.
goto dtmapi_common_help

:dtmapi_common_help
echo [HELP] Copy the whole DTMAPI folder to a path containing only English letters and numbers, then try again.
echo [HELP] If files are missing or the problem continues, resubscribe to DTMAPI.
echo [HELP] Run 3_check_dtmapi_status.bat or send a screenshot of this window.
set "DTMAPI_EXIT=1"

:dtmapi_finish
if not defined DTMAPI_NO_PAUSE pause
exit /b %DTMAPI_EXIT%
