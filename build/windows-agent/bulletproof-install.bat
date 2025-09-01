@echo off
echo ========================================
echo RDP Relay Agent - Bulletproof Install
echo ========================================
echo.
echo This is a foolproof installation script with no variable issues.
echo.
pause

PowerShell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Bulletproof-Install.ps1"
pause
