@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --update-localgpt --start-localgpt --shortcuts --port 5000
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPT update/start failed with exit code %EXITCODE%.
) else (
    echo LocalGPT update/start finished.
)

pause
exit /b %EXITCODE%
