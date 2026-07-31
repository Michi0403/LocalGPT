@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --pull-models --range Full
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPTInstallerConsole failed with exit code %EXITCODE%.
) else (
    echo Ollama Full RX7900XTXSet model pull/update finished.
)

echo.
pause
exit /b %EXITCODE%