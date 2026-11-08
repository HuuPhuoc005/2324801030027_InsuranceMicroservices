@echo off
title Insurance Microservices - Dung he thong
cd /d "%~dp0"
echo ========================================================
echo DANG DUNG TOAN BO CAC TIEN TRINH HE THONG...
echo ========================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\stop-bai9.ps1"
pause
