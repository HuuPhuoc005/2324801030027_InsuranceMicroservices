@echo off
title Insurance Microservices - Khoi dong he thong
cd /d "%~dp0"
echo ========================================================
echo DANG KHOI DONG TOAN BO HE THONG MICROSERVICES...
echo ========================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\run-bai9.ps1"
pause
