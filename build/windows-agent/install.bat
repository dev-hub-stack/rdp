@echo off
echo RDP Relay Agent Installer
echo =========================
echo.
echo This will install the RDP Relay Agent as a Windows Service.
echo.
set /p TOKEN=Enter your provisioning token: 
echo.
echo Installing with token: %TOKEN%
echo.
powershell -ExecutionPolicy Bypass -File "Install-RdpAgent.ps1" -ProvisioningToken "%TOKEN%"
pause
