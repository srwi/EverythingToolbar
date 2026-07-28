<#
.SYNOPSIS
    Stops Windows Explorer, but only if it is holding the deskband build output open.

.DESCRIPTION
    While the deskband is in use, Explorer keeps its COM host and the managed assemblies
    next to it loaded, so the build cannot overwrite them. Restarting Explorer on every
    build is only needed in that case, which this script detects by checking whether any
    DLL in the build output is locked.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetDir
)

function Test-OutputLocked {
    if (-not (Test-Path -LiteralPath $TargetDir)) {
        return $false
    }

    foreach ($file in Get-ChildItem -LiteralPath $TargetDir -Filter *.dll -File) {
        try {
            [IO.File]::Open($file.FullName, 'Open', 'Write', 'None').Dispose()
        }
        catch [IO.IOException] {
            return $true
        }
        catch {
            # Not a sharing violation (e.g. a read-only file), so Explorer is not to blame.
        }
    }

    return $false
}

if (-not (Test-OutputLocked)) {
    Write-Host 'Deskband is not loaded, leaving Explorer running.'
    exit 0
}

Write-Host 'Deskband is loaded, restarting Explorer to release the build output...'
taskkill /f /im explorer.exe | Out-Null

# Handles are closed asynchronously, so give them a moment to disappear.
for ($i = 0; $i -lt 50; $i++) {
    Start-Sleep -Milliseconds 100
    if (-not (Test-OutputLocked)) {
        exit 0
    }
}

Write-Warning 'Build output is still locked after stopping Explorer.'
exit 0
