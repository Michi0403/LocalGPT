@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" uninstall --force-delete
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo Uninstall-LocalGPT-Force-Delete failed with exit code %EXITCODE%.
) else (
    echo LocalGPT Uninstall-LocalGPT-Force-Delete finished.
)

echo.
pause
exit /b %EXITCODE%