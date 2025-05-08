@echo off

%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe /nologo /unregister "%~dp0..\EverythingToolbar.Deskband\bin\x64\Release\net8.0-windows10.0.17763.0\EverythingToolbar.Deskband.dll"
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe /nologo /codebase "%~dp0..\EverythingToolbar.Deskband\bin\x64\Release\net8.0-windows10.0.17763.0\EverythingToolbar.Deskband.dll"

pause