@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "DTMAPI_PS_PROBE_LIST=%SCRIPT_DIR%Content\DTMAPIInstaller\tools\common.ps1;%SCRIPT_DIR%Content\DTMAPIInstaller\tools\release-common.ps1;%SCRIPT_DIR%Content\DTMAPIInstaller\tools\check-dtmapi-status.ps1;%SCRIPT_DIR%Content\DTMAPIInstaller\tools\probe-powershell-host.ps1"
set "DTMAPI_PS_HOST_PROBE=%SCRIPT_DIR%Content\DTMAPIInstaller\tools\probe-powershell-host.ps1"
if not exist "%DTMAPI_PS_HOST_PROBE%" (
  echo [ERROR] PowerShell host probe script is missing: %DTMAPI_PS_HOST_PROBE%
  pause
  exit /b 1
)
set "DTMAPI_POWERSHELL="
call :dtmapi_try_powershell "%ProgramFiles%\PowerShell\7\pwsh.exe"
if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
call :dtmapi_try_powershell "%ProgramFiles(x86)%\PowerShell\7\pwsh.exe"
if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
for /f "delims=" %%P in ('where pwsh.exe 2^>nul') do (
  call :dtmapi_try_powershell "%%P"
  if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
)
call :dtmapi_try_powershell "%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
for /f "delims=" %%P in ('where powershell.exe 2^>nul') do (
  call :dtmapi_try_powershell "%%P"
  if defined DTMAPI_POWERSHELL goto dtmapi_powershell_found
)
echo [ERROR] PowerShell host was not found.
echo [ERROR] Tested Windows PowerShell and PowerShell 7 candidate paths. Please install/update PowerShell, then run this file again.
pause
exit /b 1
:dtmapi_powershell_found
echo [INFO] Using PowerShell host: %DTMAPI_POWERSHELL%
echo [INFO] Running status script: %SCRIPT_DIR%Content\DTMAPIInstaller\tools\check-dtmapi-status.ps1
"%DTMAPI_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Content\DTMAPIInstaller\tools\check-dtmapi-status.ps1"
set "DTMAPI_EXIT=%ERRORLEVEL%"
if not "%DTMAPI_EXIT%"=="0" (
  echo.
  echo [ERROR] DTMAPI status check found missing or invalid required files.
  echo [ERROR] Host used: %DTMAPI_POWERSHELL%
  echo [ERROR] Exit code: %DTMAPI_EXIT%
  echo [INFO] If a GameDir path error appears above, fix DTMAPI_GAME_DIR or local.settings.json first.
  echo [INFO] If you just ran 2_uninstall_dtmapi.bat, missing DTMAPI runtime files are expected after uninstall.
  echo [INFO] Otherwise run 1_install_dtmapi.bat again and include the whole output when asking for help.
  pause
  exit /b %DTMAPI_EXIT%
)
echo.
echo [OK] DTMAPI status check finished.
echo [OK] Host used: %DTMAPI_POWERSHELL%
pause
exit /b 0

:dtmapi_try_powershell
set "DTMAPI_PS_CANDIDATE=%~1"
if not exist "%DTMAPI_PS_CANDIDATE%" exit /b 0
echo [INFO] Probing PowerShell host: %DTMAPI_PS_CANDIDATE%
"%DTMAPI_PS_CANDIDATE%" -NoProfile -ExecutionPolicy Bypass -File "%DTMAPI_PS_HOST_PROBE%" -ProbeList "%DTMAPI_PS_PROBE_LIST%"
set "DTMAPI_PROBE_EXIT=%ERRORLEVEL%"
if "%DTMAPI_PROBE_EXIT%"=="0" set "DTMAPI_POWERSHELL=%DTMAPI_PS_CANDIDATE%"
if not "%DTMAPI_PROBE_EXIT%"=="0" echo [WARN] PowerShell host probe failed: %DTMAPI_PS_CANDIDATE% exit=%DTMAPI_PROBE_EXIT%
exit /b 0
