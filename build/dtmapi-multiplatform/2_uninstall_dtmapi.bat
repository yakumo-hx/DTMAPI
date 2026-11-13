@echo off
setlocal EnableExtensions DisableDelayedExpansion
"%~dp0DTMAPI-MultiPlatform-Installer.exe" uninstall --pause %*
set "DTMAPI_EXIT=%ERRORLEVEL%"
exit /b %DTMAPI_EXIT%
