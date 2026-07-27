@echo off
setlocal
cd /d "%~dp0"

rem Existing runtime identity, trust, databases and user data are preserved.
call "%~dp0LocalGPTInstallerConsole.exe" --install-localgpt --shortcuts --start-localgpt --port 5000
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo LocalGPT update failed with exit code %EXITCODE%.
) else (
    echo LocalGPT update and startup finished.
)

echo.
pause
exit /b %EXITCODE%
