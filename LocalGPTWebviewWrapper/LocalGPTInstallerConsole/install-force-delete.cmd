@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --install-ollama --install-localgpt --force-delete --start-localgpt
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPTInstallerConsole failed with exit code %EXITCODE%.
) else (
    echo LocalGPT install/start finished.
)

echo.
pause
exit /b %EXITCODE%