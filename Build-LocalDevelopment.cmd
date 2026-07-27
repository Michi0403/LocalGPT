@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-LocalDevelopment.ps1" %*
