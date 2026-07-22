@echo off

taskkill /f /im explorer.exe 2>nul &set errorlevel=0

rd /s /q "%~dp0..\EverythingSDK\x64"
rd /s /q "%~dp0..\EverythingSDK3\x64"
rd /s /q "%~dp0..\EverythingToolbar\bin"
rd /s /q "%~dp0..\EverythingToolbar\obj"
rd /s /q "%~dp0..\EverythingToolbar.Deskband\bin"
rd /s /q "%~dp0..\EverythingToolbar.Deskband\obj"
rd /s /q "%~dp0..\EverythingToolbar.Launcher\bin"
rd /s /q "%~dp0..\EverythingToolbar.Launcher\obj"
rd /s /q "%~dp0..\Installer\output"

powershell start-process %windir%\explorer.exe

pause