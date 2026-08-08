@echo off
setlocal
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Update-FrontendIntegrity.ps1"
if errorlevel 1 exit /b %errorlevel%
echo LocalGPT frontend integrity manifest refreshed and validated.
