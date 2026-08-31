@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-WslLinuxBuild.ps1" %*
set "exitCode=%ERRORLEVEL%"
if not "%exitCode%"=="0" pause
exit /b %exitCode%
