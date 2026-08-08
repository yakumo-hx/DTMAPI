[CmdletBinding()]
param(
    [string] $TestRoot = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$gate = Join-Path $PSScriptRoot 'test-batch6-g2-advanced-synthetic.ps1'
$managedRoot = if (-not [string]::IsNullOrWhiteSpace($TestRoot)) {
    [System.IO.Path]::GetFullPath($TestRoot)
}
elseif (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo 'tmp\test-runs'))
}
$fixtureRoot = Join-Path $managedRoot ('batch6-g2-authority-paths-' + [Guid]::NewGuid().ToString('N'))
$externalOwnership = Join-Path $fixtureRoot 'ownership-receipt.json'
$externalRuntime = Join-Path $fixtureRoot 'runtime-receipt.json'
$hostExe = (Get-Process -Id $PID).Path

function ConvertTo-G2ProcessArgument([string] $Value) {
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Invoke-G2Gate([string[]] $AdditionalArguments) {
    $arguments = @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $gate) + $AdditionalArguments
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $hostExe
    $start.Arguments = [string]::Join(' ', @($arguments | ForEach-Object { ConvertTo-G2ProcessArgument ([string]$_) }))
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        Output = $stdout + [Environment]::NewLine + $stderr
    }
}

function Assert-G2Rejected(
    [string] $Label,
    [string[]] $Arguments,
    [string] $ExpectedOutput
) {
    $result = Invoke-G2Gate $Arguments
    if ($result.ExitCode -eq 0) {
        throw "$Label unexpectedly passed the permanent G2 authority gate."
    }
    $normalizedOutput = [Text.RegularExpressions.Regex]::Replace($result.Output, '\s+', '')
    $normalizedExpected = [Text.RegularExpressions.Regex]::Replace($ExpectedOutput, '\s+', '')
    if ($normalizedOutput.IndexOf($normalizedExpected, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "$Label failed without the expected diagnostic '$ExpectedOutput': $($result.Output)"
    }
}

try {
    [System.IO.Directory]::CreateDirectory($fixtureRoot) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo 'tools\release\baselines\batch6-g2-ownership-receipt.json') -Destination $externalOwnership
    Copy-Item -LiteralPath (Join-Path $repo 'tools\release\baselines\batch6-g2-runtime-matrix-receipt.json') -Destination $externalRuntime

    $positive = Invoke-G2Gate @()
    if ($positive.ExitCode -ne 0) {
        throw "Contract-owned G2 authority positive control failed: $($positive.Output)"
    }

    Assert-G2Rejected `
        'External ownership receipt override' `
        @('-OwnershipReceiptPath', $externalOwnership) `
        'must use the contract-owned ownership receipt path'
    Assert-G2Rejected `
        'External Runtime receipt override' `
        @('-RuntimeReceiptPath', $externalRuntime) `
        'must use the contract-owned Runtime receipt path'
    Assert-G2Rejected `
        'Paired external receipt overrides' `
        @('-OwnershipReceiptPath', $externalOwnership, '-RuntimeReceiptPath', $externalRuntime) `
        'must use the contract-owned'

    Write-Host 'Batch 6 G2 permanent authority-path matrix: PASS'
    Write-Host '  positive=contract-owned receipts negative=ownership/runtime/paired external overrides'
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
