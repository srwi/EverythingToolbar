@echo off

echo Installation directory:

set dll_path=%~dp0
echo %dll_path%

echo Registering EverythingToolbar...

set base_key="HKEY_CURRENT_USER\Software\Classes\CLSID\{9D39B79C-E03C-4757-B1B6-ECCE843748F3}"
reg.exe add "%base_key%" /ve /t "REG_SZ" /d "Everything Toolbar" /f
reg.exe add "%base_key%\Implemented Categories" /f
reg.exe add "%base_key%\Implemented Categories\{00021492-0000-0000-c000-000000000046}" /f
reg.exe add "%base_key%\Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29} /f{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}" /f
reg.exe add "%base_key%\InprocServer32" /ve /t "REG_SZ" /d "mscoree.dll" /f
reg.exe add "%base_key%\InprocServer32" /v "ThreadingModel" /t "REG_SZ" /d "Both" /f
reg.exe add "%base_key%\InprocServer32" /v "Class" /t "REG_SZ" /d "CSDeskBand.Deskband" /f
reg.exe add "%base_key%\InprocServer32" /v "Assembly" /t "REG_SZ" /d "EverythingToolbar, Version=0.6.1.0, Culture=neutral, PublicKeyToken=3b7d0980372ca18d" /f
reg.exe add "%base_key%\InprocServer32" /v "RuntimeVersion" /t "REG_SZ" /d "v4.0.30319" /f
reg.exe add "%base_key%\InprocServer32" /v "CodeBase" /t "REG_SZ" /d "file:///%dll_path%EverythingToolbar.dll" /f
reg.exe add "%base_key%\InprocServer32\0.6.1.0" /v "Class" /t "REG_SZ" /d "CSDeskBand.Deskband" /f
reg.exe add "%base_key%\InprocServer32\0.6.1.0" /v "Assembly" /t "REG_SZ" /d "EverythingToolbar, Version=0.6.1.0, Culture=neutral, PublicKeyToken=3b7d0980372ca18d" /f
reg.exe add "%base_key%\InprocServer32\0.6.1.0" /v "RuntimeVersion" /t "REG_SZ" /d "v4.0.30319" /f
reg.exe add "%base_key%\InprocServer32\0.6.1.0" /v "CodeBase" /t "REG_SZ" /d "file:///%dll_path%EverythingToolbar.dll" /f
reg.exe add "%base_key%\ProgId" /ve /t "REG_SZ" /d "CSDeskBand.Deskband" /f

echo Please open the taskbar context menu twice for EverythingToolbar to show up!

pause
