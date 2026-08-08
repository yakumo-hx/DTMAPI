param(
    [Parameter(Mandatory = $true)] [string] $Token,
    [Parameter(Mandatory = $true)] [ValidateSet('install', 'uninstall', 'full-uninstall', 'check', 'collect')] [string] $Action,
    [Parameter(Mandatory = $true)] [string] $ToolsRoot,
    [Parameter(Mandatory = $true)] [string] $ResultPath
)

$ErrorActionPreference = 'Stop'

try {
    if ($PSVersionTable.PSVersion.Major -lt 5) {
        throw "PowerShell 5.1 or newer is required. Found $($PSVersionTable.PSVersion)."
    }
    if ([string]$ExecutionContext.SessionState.LanguageMode -ne 'FullLanguage') {
        throw "PowerShell FullLanguage mode is required. Found $($ExecutionContext.SessionState.LanguageMode)."
    }

    $tools = [System.IO.Path]::GetFullPath($ToolsRoot)
    $scriptName = switch ($Action) {
        'install' { 'install-dtmapi.ps1' }
        'uninstall' { 'uninstall-dtmapi.ps1' }
        'full-uninstall' { 'uninstall-dtmapi.ps1' }
        'check' { 'check-dtmapi-status.ps1' }
        'collect' { 'collect-logs.ps1' }
    }
    $required = New-Object 'System.Collections.Generic.List[string]'
    $required.Add((Join-Path $tools $scriptName))
    if ($Action -ne 'check') {
        $required.Add((Join-Path $tools 'player-common.ps1'))
    }

    foreach ($path in $required) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required script is missing: $path"
        }
        $tokens = $null
        $errors = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$errors)
        if ($errors.Count -gt 0) {
            $details = ($errors | ForEach-Object { "$($_.Extent.StartLineNumber):$($_.Extent.StartColumnNumber) $($_.Message)" }) -join '; '
            throw "PowerShell parser rejected $path. $details"
        }
    }

    $parent = Split-Path -Parent $ResultPath
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    $proof = "DTMAPI-PROBE:${Token}:$Action"
    [System.IO.File]::WriteAllText($ResultPath, $proof, [System.Text.UTF8Encoding]::new($false))
    Write-Host "[INFO] Probe OK: PowerShell $($PSVersionTable.PSVersion) $($PSVersionTable.PSEdition); action=$Action"
    exit 0
}
catch {
    Write-Host "[ERROR] PowerShell host probe failed: $($_.Exception.Message)" -ForegroundColor Red
    try {
        if (Test-Path -LiteralPath $ResultPath) {
            Remove-Item -LiteralPath $ResultPath -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
    }
    exit 1
}
