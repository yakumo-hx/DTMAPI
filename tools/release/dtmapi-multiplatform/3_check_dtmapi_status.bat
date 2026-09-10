@echo off
setlocal EnableExtensions DisableDelayedExpansion
"%~dp0DTMAPI-MultiPlatform-Installer.exe" status --pause %*
set "DTMAPI_EXIT=%ERRORLEVEL%"
exit /b %DTMAPI_EXIT%
