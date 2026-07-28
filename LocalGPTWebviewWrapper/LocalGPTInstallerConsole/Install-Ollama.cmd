@echo off
setlocal
cd /d "%~dp0"
call "%~dp0LocalGPTInstallerConsole.exe" --install-ollama
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
