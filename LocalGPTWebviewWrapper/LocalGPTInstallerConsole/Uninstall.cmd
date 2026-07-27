@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --uninstall --force-delete
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPT uninstall failed with exit code %EXITCODE%.
) else (
    echo LocalGPT uninstall finished. The learning base, Ollama, and installed models remain untouched.
)

echo.
pause
exit /b %EXITCODE%
