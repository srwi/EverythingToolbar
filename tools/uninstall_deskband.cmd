@echo off
setlocal enabledelayedexpansion

set "COM_DLL_PATH=%~dp0..\EverythingToolbar.Deskband\bin\x64\Release\net8.0-windows10.0.17763.0\EverythingToolbar.Deskband.comhost.dll"

regsvr32 /u "%COM_DLL_PATH%"

taskkill /f /im explorer.exe >nul 2>&1
timeout /t 1 >nul
start explorer.exe

pause