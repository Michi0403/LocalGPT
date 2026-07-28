@echo off
setlocal
cd /d "%~dp0"
call "%~dp0LocalGPTInstallerConsole.exe" --pull-models --range RTX3060
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
