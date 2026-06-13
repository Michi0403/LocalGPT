@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --install-ollama --install-localgpt --setup-learning-base --import-recommended --force-delete --start-localgpt
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPTInstallerConsole failed with exit code %EXITCODE%.
) else (
    echo LocalGPT install/gitfeed force-delete/start finished.
)

echo.
pause
exit /b %EXITCODE%