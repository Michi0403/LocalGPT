@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --pull-models --range Slim
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPTInstallerConsole failed with exit code %EXITCODE%.
) else (
    echo Ollama Slim model pull/update finished.
)

echo.
pause
exit /b %EXITCODE%