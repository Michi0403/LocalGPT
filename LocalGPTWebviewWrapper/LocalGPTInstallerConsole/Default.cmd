@echo off
setlocal
cd /d "%~dp0"

rem No arguments intentionally run the preservation-first default install/update routine.
call "%~dp0LocalGPTInstallerConsole.exe"
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPT default install/update failed with exit code %EXITCODE%.
) else (
    echo LocalGPT default install/update and startup finished.
)

echo.
pause
exit /b %EXITCODE%
