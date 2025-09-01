@echo off
echo ========================================
echo RDP Relay Agent - Complete Reinstall
echo ========================================
echo.
echo This will completely remove and reinstall the RDP Relay Agent
echo with the correct local server configuration.
echo.
echo Server: 192.168.18.101
echo Agent ID: 68b42ba78f7aa507962ee9e5
echo.
pause

REM Check for admin privileges and elevate if needed
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with Administrator privileges...
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Complete-Reinstall.ps1"
) else (
    echo Requesting Administrator privileges...
    powershell.exe -Command "Start-Process PowerShell -Verb RunAs -ArgumentList '-ExecutionPolicy Bypass -File \"%~dp0Complete-Reinstall.ps1\"'"
)

pause
