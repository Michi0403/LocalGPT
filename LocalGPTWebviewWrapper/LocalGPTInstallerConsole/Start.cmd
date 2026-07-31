@echo off
setlocal
cd /d "%~dp0"
call "%~dp0LocalGPTInstallerConsole.exe" --start-localgpt --port 5000

set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPTInstallerConsole failed with exit code %EXITCODE%.
) else (
    echo LocalGPT start finished.
)

echo.
pause
exit /b %EXITCODE%