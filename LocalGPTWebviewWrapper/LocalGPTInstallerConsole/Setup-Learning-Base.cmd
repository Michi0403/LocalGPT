@echo off
setlocal
cd /d "%~dp0"
call "%~dp0LocalGPTInstallerConsole.exe" --setup-learning-base
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
