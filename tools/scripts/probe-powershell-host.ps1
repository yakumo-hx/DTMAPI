param(
    [Parameter(Mandatory = $true)] [string] $ToolsRoot,
    [Parameter(Mandatory = $true)]
    [ValidateSet('install', 'uninstall', 'status', 'collect')]
    [string] $Action,
    [Parameter(Mandatory = $true)] [string] $ProbeNonce,
    [Parameter(Mandatory = $true)] [string] $ProbeResultPath
)

$ErrorActionPreference = 'Stop'
$hostProbePath = $PSCommandPath

if ($ProbeNonce -notmatch '^[A-Za-z0-9-]{8,128}$') {
    Write-Host '[ERROR]   Host probe nonce is invalid.'
    exit 9
}
if ([string]::IsNullOrWhiteSpace($ProbeResultPath)) {
    Write-Host '[ERROR]   Host probe result path is empty.'
    exit 9
}
$probeResultFull = [System.IO.Path]::GetFullPath($ProbeResultPath)
if (Test-Path -LiteralPath $probeResultFull) {
    Write-Host ('[ERROR]   Host probe result path already exists: ' + $probeResultFull)
    exit 9
}

Write-Host ('[INFO]   Version: {0} {1}' -f $PSVersionTable.PSVersion, $PSVersionTable.PSEdition)
Write-Host ('[INFO]   LanguageMode: {0}' -f $ExecutionContext.SessionState.LanguageMode)

if (-not [string]::Equals(
    [string]$ExecutionContext.SessionState.LanguageMode,
    'FullLanguage',
    [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host '[ERROR]   DTMAPI installer actions require PowerShell FullLanguage mode.'
    exit 5
}

if ([string]::IsNullOrWhiteSpace($ToolsRoot)) {
    Write-Host '[ERROR]   Tools root is empty.'
    exit 4
}

$toolsRootFull = [System.IO.Path]::GetFullPath($ToolsRoot)
$scriptNames = New-Object 'System.Collections.Generic.List[string]'
$scriptNames.Add('common.ps1') | Out-Null
if ($Action -ne 'collect') {
    $scriptNames.Add('release-common.ps1') | Out-Null
}
$scriptNames.Add('probe-powershell-host.ps1') | Out-Null
switch ($Action) {
    'install' {
        $scriptNames.Add('install-to-game.ps1') | Out-Null
        $scriptNames.Add('install-bepinex.ps1') | Out-Null
    }
    'uninstall' { $scriptNames.Add('uninstall-dtmapi.ps1') | Out-Null }
    'status' { $scriptNames.Add('check-dtmapi-status.ps1') | Out-Null }
    'collect' {
        $scriptNames.Add('collect-logs.ps1') | Out-Null
        $scriptNames.Add('analyze-startup-evidence.ps1') | Out-Null
    }
}

$probePaths = New-Object 'System.Collections.Generic.List[string]'
foreach ($scriptName in $scriptNames) {
    $probePath = [System.IO.Path]::GetFullPath((Join-Path $toolsRootFull $scriptName))
    $probePaths.Add($probePath) | Out-Null
    Write-Host ('[INFO]   Checking script: ' + $probePath)
    if (-not (Test-Path -LiteralPath $probePath -PathType Leaf)) {
        Write-Host ('[ERROR]   Missing script: ' + $probePath)
        exit 3
    }

    try {
        $tokens = $null
        $errors = $null
        [System.Management.Automation.Language.Parser]::ParseFile($probePath, [ref] $tokens, [ref] $errors) | Out-Null
        if ($errors -and $errors.Count -gt 0) {
            Write-Host ('[ERROR]   Parser errors in ' + $probePath)
            foreach ($err in @($errors | Select-Object -First 8)) {
                Write-Host ('[ERROR]     {0}:{1} {2}' -f $err.Extent.StartLineNumber, $err.Extent.StartColumnNumber, $err.Message)
            }
            exit 1
        }
    }
    catch {
        Write-Host ('[ERROR]   Host/parser error in ' + $probePath)
        Write-Host ('[ERROR]     ' + $_.Exception.Message)
        exit 2
    }
}

try {
    foreach ($commandName in @('ConvertFrom-Json', 'ConvertTo-Json')) {
        if ($null -eq (Get-Command $commandName -ErrorAction SilentlyContinue)) {
            throw "Required JSON command is unavailable: $commandName"
        }
    }
    $jsonValue = '{"Value":1}' | ConvertFrom-Json
    $jsonRoundTrip = $jsonValue | ConvertTo-Json -Compress
    if ([string]::IsNullOrWhiteSpace([string]$jsonRoundTrip)) {
        throw 'PowerShell JSON round-trip returned no data.'
    }
    Write-Host '[INFO]   JSON capability: OK'
}
catch {
    Write-Host ('[ERROR]   JSON capability probe failed: ' + $_.Exception.Message)
    exit 6
}

$commonPath = @($probePaths.ToArray() | Where-Object {
    [string]::Equals(
        [System.IO.Path]::GetFileName($_),
        'common.ps1',
        [System.StringComparison]::OrdinalIgnoreCase)
} | Select-Object -First 1)
if ($commonPath.Count -ne 1) {
    Write-Host '[ERROR]   common.ps1 is missing from the capability probe list.'
    exit 7
}

try {
    . $commonPath[0]
    $selfHash = Get-DtmApiFileSha256 -Path $hostProbePath
    if ($selfHash -notmatch '^[0-9A-F]{64}$') {
        throw 'Portable SHA-256 helper returned an invalid digest.'
    }
    Write-Host '[INFO]   Portable SHA-256 capability: OK'

    if ($Action -eq 'install') {
        if (-not (Test-DtmApiZipArchiveSupport)) {
            throw 'The .NET ZIP extraction runtime is unavailable.'
        }
        Write-Host '[INFO]   Portable ZIP extraction capability: OK'
    }
}
catch {
    Write-Host ('[ERROR]   DTMAPI runtime capability probe failed: ' + $_.Exception.Message)
    exit 8
}

try {
    $proof = 'DTMAPI_PROBE_OK_{0}_{1}' -f $ProbeNonce, $Action
    $encoding = New-Object System.Text.ASCIIEncoding
    $stream = [System.IO.File]::Open(
        $probeResultFull,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)
    try {
        $bytes = $encoding.GetBytes($proof)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush()
    }
    finally {
        $stream.Dispose()
    }
}
catch {
    Write-Host ('[ERROR]   Could not write invocation-bound probe proof: ' + $_.Exception.Message)
    exit 9
}

Write-Host '[INFO]   Probe OK with invocation proof.'
exit 0
