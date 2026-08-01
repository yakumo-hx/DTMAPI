param(
    [string] $ProbeList = ''
)

$ErrorActionPreference = 'Stop'

Write-Host ('[INFO]   Version: {0} {1}' -f $PSVersionTable.PSVersion, $PSVersionTable.PSEdition)

if ([string]::IsNullOrWhiteSpace($ProbeList)) {
    Write-Host '[ERROR]   Probe list is empty.'
    exit 4
}

foreach ($probe in ($ProbeList -split ';')) {
    if ([string]::IsNullOrWhiteSpace($probe)) {
        continue
    }

    Write-Host ('[INFO]   Checking script: ' + $probe)
    if (-not (Test-Path -LiteralPath $probe -PathType Leaf)) {
        Write-Host ('[ERROR]   Missing script: ' + $probe)
        exit 3
    }

    try {
        $tokens = $null
        $errors = $null
        [System.Management.Automation.Language.Parser]::ParseFile($probe, [ref] $tokens, [ref] $errors) | Out-Null
        if ($errors -and $errors.Count -gt 0) {
            Write-Host ('[ERROR]   Parser errors in ' + $probe)
            foreach ($err in @($errors | Select-Object -First 8)) {
                Write-Host ('[ERROR]     {0}:{1} {2}' -f $err.Extent.StartLineNumber, $err.Extent.StartColumnNumber, $err.Message)
            }
            exit 1
        }
    }
    catch {
        Write-Host ('[ERROR]   Host/parser error in ' + $probe)
        Write-Host ('[ERROR]     ' + $_.Exception.Message)
        exit 2
    }
}

Write-Host '[INFO]   Probe OK.'
exit 0
