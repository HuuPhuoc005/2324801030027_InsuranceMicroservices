@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -SkipTests
exit /b %errorlevel%
