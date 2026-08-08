@echo off
setlocal EnableExtensions DisableDelayedExpansion
set "DTMAPI_DISPATCHER=%~dp0Content\DTMAPIInstaller\tools\invoke-dtmapi-action.cmd"
if not exist "%DTMAPI_DISPATCHER%" (
  echo [ERROR] DTMAPI installer dispatcher is missing: "%DTMAPI_DISPATCHER%"
  if not defined DTMAPI_NO_PAUSE pause
  exit /b 1
)
"%ComSpec%" /d /e:on /v:off /c call "%DTMAPI_DISPATCHER%" uninstall
set "DTMAPI_EXIT=%ERRORLEVEL%"
exit /b %DTMAPI_EXIT%
