@echo off
setlocal EnableExtensions DisableDelayedExpansion
"%~dp0DTMAPI-MultiPlatform-Installer.exe" install --pause %*
set "DTMAPI_EXIT=%ERRORLEVEL%"
exit /b %DTMAPI_EXIT%
