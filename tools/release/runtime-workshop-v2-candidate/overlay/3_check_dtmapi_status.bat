@echo off
setlocal EnableExtensions DisableDelayedExpansion
set "DTMAPI_DISPATCHER=%~dp0Content\DTMAPIInstaller\tools\invoke-dtmapi-action.cmd"
if not exist "%DTMAPI_DISPATCHER%" goto dtmapi_missing
"%ComSpec%" /d /e:on /v:off /c call "%DTMAPI_DISPATCHER%" check
set "DTMAPI_EXIT=%ERRORLEVEL%"
exit /b %DTMAPI_EXIT%

:dtmapi_missing
echo [ERROR] The lightweight DTMAPI checker is missing.
echo [HELP] Copy the whole DTMAPI folder to a path containing only English letters and numbers, then try again.
echo [HELP] If files are still missing, resubscribe to DTMAPI. You can also send a screenshot of this window.
if not defined DTMAPI_NO_PAUSE pause
exit /b 1
