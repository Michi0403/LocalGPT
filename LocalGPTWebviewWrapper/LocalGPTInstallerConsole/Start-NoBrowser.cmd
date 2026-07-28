@echo off
setlocal
cd /d "%~dp0"
call "%~dp0LocalGPTInstallerConsole.exe" --start-localgpt --no-browser --port 5000
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
