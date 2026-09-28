[Code]
function IsProductInstalled(ProductCode: string): Boolean;
var
  UninstallKey: string;
  UninstallString: string;
begin
  Result := False;
  UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + ProductCode;

  if RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', UninstallString) then
    Result := True
  else if RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', UninstallString) then
    Result := True;
end;

function UninstallWixVersion(): Boolean;
var
  ResultCode: Integer;
  ProductCodes: array of string;
  i: Integer;
begin
  Result := True;

  SetArrayLength(ProductCodes, 21);
  ProductCodes[0]  := '{BAF2A7BE-A3BD-4263-9F73-4CDA8D311642}'; // 1.5.5
  ProductCodes[1]  := '{E2B7C73E-7C4E-4CD1-A1E9-E5CD5D33F6C3}'; // 1.5.4
  ProductCodes[2]  := '{5E8338F2-4A1B-4491-B81D-01DED7F43186}'; // 1.5.3
  ProductCodes[3]  := '{0B0076BB-DE90-4A46-99EA-B11FF507EE87}'; // 1.5.2
  ProductCodes[4]  := '{5868369F-87A1-4FBA-BB53-F4121CFDDF7C}'; // 1.5.1
  ProductCodes[5]  := '{F17EB002-4E95-4958-8C88-5EEE14C898A0}'; // 1.5.0
  ProductCodes[6]  := '{F3D08C1A-8A4E-49B6-94CF-DAD91E13B8D2}'; // 1.4.1
  ProductCodes[7]  := '{013139A7-CB39-4018-8867-EFF14CD474C6}'; // 1.4.0
  ProductCodes[8]  := '{C3C9E188-7622-406E-A3D9-030D17BA817C}'; // 1.3.4
  ProductCodes[9]  := '{8F06F947-8F5B-4B98-BCED-5D4B66DF53FC}'; // 1.3.3
  ProductCodes[10] := '{06F8F6B2-D39C-419E-BC50-DBCFE5B5016C}'; // 1.3.2
  ProductCodes[11] := '{00FD83A4-9098-474A-B65F-9965B5DBE3FB}'; // 1.3.1
  ProductCodes[12] := '{71E92908-DDD0-4B21-9BFD-4CEF2DB95C23}'; // 1.3.0
  ProductCodes[13] := '{176E0C8F-CA9D-4458-8335-6FE7810C3309}'; // 1.2.0
  ProductCodes[14] := '{85F08065-DC9C-427F-832C-77CE642D6C4C}'; // 1.1.1
  ProductCodes[15] := '{CEC55ACD-E19D-4A48-A27E-CF6FFDDA873B}'; // 1.1.0
  ProductCodes[16] := '{A55F3806-F2C3-46E8-B713-07F6E599689B}'; // 1.0.5
  ProductCodes[17] := '{960D3A75-6DF2-4856-9F8B-22EEBCD6FA27}'; // 1.0.3
  ProductCodes[18] := '{80E3C077-9C9C-4487-B16D-06FFCA15A78D}'; // 1.0.2
  ProductCodes[19] := '{7BB2157B-CE2A-443F-A25B-AC7FF12FE955}'; // 1.0.1
  ProductCodes[20] := '{750BA96B-2EE9-49C5-95F5-D6EE9C4B0B47}'; // 1.0.0

  for i := 0 to GetArrayLength(ProductCodes) - 1 do
  begin
    if IsProductInstalled(ProductCodes[i]) then
    begin
      if SuppressibleMsgBox('A previous version of EverythingToolbar was detected. Due to changes in the installation process, ' +
                'it needs to be removed before continuing. The system will restart after uninstalling. ' +
                'Please launch this installer again after the restart to complete the installation. Do you want to continue?',
                mbConfirmation, MB_YESNO, IDYES) = IDYES then
      begin
        // We need to delete the pinned taskbar icon because the user might install the new version to a different location which would break the link
        DeleteFile(ExpandConstant('{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\EverythingToolbar.lnk'));
        if not Exec('msiexec.exe', '/x' + ProductCodes[i] + ' /passive /forcerestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
        begin
          SuppressibleMsgBox('Failed to uninstall the previous version. Please uninstall it manually before continuing.', mbError, MB_OK, IDOK);
          Result := False;
        end;
      end
      else
      begin
        Result := False;
      end;
      Break;
    end;
  end;
end;
