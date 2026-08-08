@echo off
setlocal
cd /d "%~dp0"

echo Doloc Town portable full baseline capture
echo Close the game and keep this window open until completion.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-doloctown-full-capture.ps1"
set "CAPTURE_EXIT=%ERRORLEVEL%"

echo.
if "%CAPTURE_EXIT%"=="0" (
    echo Capture completed successfully.
) else (
    echo Capture failed with exit code %CAPTURE_EXIT%.
    echo Read README.zh-CN.md for path configuration and safe retry commands.
)
echo.
pause
exit /b %CAPTURE_EXIT%
