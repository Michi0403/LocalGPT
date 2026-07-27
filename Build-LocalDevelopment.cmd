@echo off
setlocal
pushd "%~dp0"
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Build-LocalDevelopment.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
  echo.
  echo LocalGPT development build failed with exit code %EXITCODE%.
  echo Review the errors above. This window will remain open.
  pause
)
popd
exit /b %EXITCODE%
