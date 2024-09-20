@echo off

%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe /nologo /unregister "%~dp0..\EverythingToolbar.Deskband\bin\Release\EverythingToolbar.Deskband.dll"
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe /nologo /codebase "%~dp0..\EverythingToolbar.Deskband\bin\Release\EverythingToolbar.Deskband.dll"

pause