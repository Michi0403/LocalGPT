@echo off
setlocal
cd /d "%~dp0"

call "%~dp0LocalGPTInstallerConsole.exe" --uninstall
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo Uninstall-LocalGPT-DryRun failed with exit code %EXITCODE%.
) else (
    echo LocalGPT Uninstall-LocalGPT-DryRun finished.
)

echo.
pause
exit /b %EXITCODE%