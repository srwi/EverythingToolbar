@echo off

reg.exe delete "HKEY_CURRENT_USER\Software\Classes\CLSID\{9D39B79C-E03C-4757-B1B6-ECCE843748F3}" /f
taskkill /F /IM explorer.exe & start explorer

pause