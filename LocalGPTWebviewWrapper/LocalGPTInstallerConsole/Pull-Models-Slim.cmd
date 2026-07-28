@echo off
setlocal
cd /d "%~dp0"
call "%~dp0LocalGPTInstallerConsole.exe" --pull-models --range Slim
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
