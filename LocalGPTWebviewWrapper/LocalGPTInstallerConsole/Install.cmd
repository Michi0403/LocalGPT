@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --install-localgpt --force-delete --shortcuts --start-localgpt --port 5000
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPT installation failed with exit code %EXITCODE%.
) else (
    echo LocalGPT installation and startup finished.
)

echo.
pause
exit /b %EXITCODE%
