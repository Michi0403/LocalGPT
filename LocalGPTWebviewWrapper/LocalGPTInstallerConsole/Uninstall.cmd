@echo off
setlocal
cd /d "%~dp0"

rem Destructive removal remains explicit and is never used by the default workflow.
call "%~dp0LocalGPTInstallerConsole.exe" --uninstall --force-delete
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
