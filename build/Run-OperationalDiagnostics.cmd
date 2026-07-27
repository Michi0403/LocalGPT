@echo off
setlocal
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Assert-OperationalDiagnostics.ps1" %*
exit /b %ERRORLEVEL%
