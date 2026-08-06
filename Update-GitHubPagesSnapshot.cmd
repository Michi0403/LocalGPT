@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Update-GitHubPagesSnapshot.ps1" %*
set "exitCode=%ERRORLEVEL%"
if not "%exitCode%"=="0" echo LocalGPT GitHub Pages snapshot update failed with exit code %exitCode%.
exit /b %exitCode%
