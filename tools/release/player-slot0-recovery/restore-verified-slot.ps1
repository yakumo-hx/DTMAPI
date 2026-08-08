[CmdletBinding()]
param(
    [string] $ManifestPath = '',
    [string] $RecoverySourcePath = '',
    [string] $SaveDirectory = '',
    [switch] $SkipProcessCheck
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$script:PartialPath = ''
$script:SaveRoot = ''

function Throw-DtmRecoveryFailure {
    param(
        [int] $Code,
        [string] $Message
    )

    $errorRecord = New-Object System.InvalidOperationException -ArgumentList $Message
    $errorRecord.Data['DtmRecoveryExitCode'] = $Code
    throw $errorRecord
}

function Get-DtmRecoverySha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-DtmRecoveryManifestValue {
    param(
        [Parameter(Mandatory = $true)] [object] $Manifest,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $property = $Manifest.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) {
        Throw-DtmRecoveryFailure -Code 10 -Message ('Manifest field is missing: ' + $Name)
    }
    return $property.Value
}

function Assert-DtmRecoveryHash {
    param(
        [string] $Value,
        [string] $Name
    )

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -notmatch '^[A-Fa-f0-9]{64}$') {
        Throw-DtmRecoveryFailure -Code 10 -Message ('Manifest SHA-256 is invalid: ' + $Name)
    }
}

function Assert-DtmRecoveryNoReparsePoint {
    param(
        [string] $Path,
        [string] $Label
    )

    $attributes = [System.IO.File]::GetAttributes($Path)
    if (($attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        Throw-DtmRecoveryFailure -Code 30 -Message ($Label + ' must not be a reparse point: ' + $Path)
    }
}

function Invoke-DtmRecoveryAtomicReplace {
    param(
        [string] $ReplacementPath,
        [string] $TargetPath
    )

    $signature = [Type[]]@([string], [string], [string], [bool])
    $method = [System.IO.File].GetMethod('Replace', $signature)
    if ($null -eq $method) {
        Throw-DtmRecoveryFailure -Code 40 -Message 'The required atomic File.Replace overload is unavailable.'
    }
    $arguments = New-Object object[] 4
    $arguments[0] = $ReplacementPath
    $arguments[1] = $TargetPath
    $arguments[2] = $null
    $arguments[3] = $true
    try {
        [void]$method.Invoke($null, $arguments)
    }
    catch [System.Reflection.TargetInvocationException] {
        if ($null -ne $_.Exception.InnerException) {
            throw $_.Exception.InnerException
        }
        throw
    }
}

function Get-DtmRecoveryBlockedProcesses {
    $names = @('DolocTown', 'steam', 'steamwebhelper', 'GameOverlayUI')
    $found = New-Object 'System.Collections.Generic.List[string]'
    foreach ($name in $names) {
        foreach ($process in @(Get-Process -Name $name -ErrorAction SilentlyContinue)) {
            $label = $process.ProcessName + '#' + $process.Id
            if (-not $found.Contains($label)) {
                [void]$found.Add($label)
            }
        }
    }
    return @($found)
}

function Assert-DtmRecoveryProcessesClosed {
    if ($SkipProcessCheck) {
        return
    }
    $blocked = @(Get-DtmRecoveryBlockedProcesses)
    if ($blocked.Count -gt 0) {
        Throw-DtmRecoveryFailure -Code 20 -Message ('Close Doloc Town and fully exit Steam, then retry. Running: ' + ($blocked -join ', '))
    }
}

function Resolve-DtmRecoverySaveDirectory {
    param([string] $TargetFileName)

    if (-not [string]::IsNullOrWhiteSpace($SaveDirectory)) {
        return [System.IO.Path]::GetFullPath($SaveDirectory)
    }

    $profileRoot = $env:USERPROFILE
    if ([string]::IsNullOrWhiteSpace($profileRoot)) {
        $profileRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
    }
    if ([string]::IsNullOrWhiteSpace($profileRoot)) {
        Throw-DtmRecoveryFailure -Code 30 -Message 'The Windows user profile could not be resolved.'
    }

    $candidates = @(
        (Join-Path $profileRoot 'AppData\LocalLow\RedSawGames\DolocTown\SAVE'),
        (Join-Path $profileRoot 'AppData\LocalLow\RedSawGames\Doloc Town\SAVE')
    )
    $matches = New-Object 'System.Collections.Generic.List[string]'
    foreach ($candidate in $candidates) {
        $full = [System.IO.Path]::GetFullPath($candidate)
        if ([System.IO.File]::Exists((Join-Path $full $TargetFileName))) {
            [void]$matches.Add($full)
        }
    }
    if ($matches.Count -eq 0) {
        Throw-DtmRecoveryFailure -Code 30 -Message 'The slot 0 current save was not found under AppData\LocalLow\RedSawGames.'
    }
    if ($matches.Count -gt 1) {
        Throw-DtmRecoveryFailure -Code 30 -Message 'More than one slot 0 SAVE location was found. Recovery refused.'
    }
    return $matches[0]
}

function Remove-DtmRecoveryPartial {
    if ([string]::IsNullOrWhiteSpace($script:PartialPath)) {
        return
    }
    try {
        $partialFull = [System.IO.Path]::GetFullPath($script:PartialPath)
        $leaf = [System.IO.Path]::GetFileName($partialFull)
        $parent = [System.IO.Path]::GetDirectoryName($partialFull)
        if ($leaf.StartsWith('.dtmapi-verified-recovery-', [StringComparison]::Ordinal) -and
            $leaf.EndsWith('.partial', [StringComparison]::Ordinal) -and
            -not [string]::IsNullOrWhiteSpace($script:SaveRoot) -and
            $parent -ieq $script:SaveRoot -and
            [System.IO.File]::Exists($partialFull)) {
            [System.IO.File]::Delete($partialFull)
        }
    }
    catch {
        Write-Host ('[WARN] Could not clean recovery-owned partial: ' + $_.Exception.Message) -ForegroundColor Yellow
    }
    finally {
        $script:PartialPath = ''
    }
}

try {
    $packageRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
    if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
        $ManifestPath = Join-Path $packageRoot 'recovery-manifest.json'
    }
    if ([string]::IsNullOrWhiteSpace($RecoverySourcePath)) {
        $RecoverySourcePath = Join-Path $packageRoot 'verified-recovery-source.data'
    }
    $manifestFull = [System.IO.Path]::GetFullPath($ManifestPath)
    $sourceFull = [System.IO.Path]::GetFullPath($RecoverySourcePath)
    $packagePrefix = $packageRoot.TrimEnd('\', '/') + '\'
    if (-not $manifestFull.StartsWith($packagePrefix, [StringComparison]::OrdinalIgnoreCase) -or
        -not $sourceFull.StartsWith($packagePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        Throw-DtmRecoveryFailure -Code 10 -Message 'Manifest and recovery source must remain beside the recovery script.'
    }
    if ([System.IO.Path]::GetFileName($manifestFull) -cne 'recovery-manifest.json' -or
        [System.IO.Path]::GetFileName($sourceFull) -cne 'verified-recovery-source.data') {
        Throw-DtmRecoveryFailure -Code 10 -Message 'Recovery package filenames do not match the fixed contract.'
    }
    if (-not [System.IO.File]::Exists($manifestFull) -or -not [System.IO.File]::Exists($sourceFull)) {
        Throw-DtmRecoveryFailure -Code 10 -Message 'Recovery package is incomplete.'
    }
    Assert-DtmRecoveryNoReparsePoint -Path $packageRoot -Label 'Recovery package directory'
    Assert-DtmRecoveryNoReparsePoint -Path $manifestFull -Label 'Recovery manifest'
    Assert-DtmRecoveryNoReparsePoint -Path $sourceFull -Label 'Recovery source'

    $manifest = Get-Content -LiteralPath $manifestFull -Raw | ConvertFrom-Json
    $formatVersion = [Convert]::ToInt32((Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'FormatVersion'))
    $caseId = [string](Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'CaseId')
    $slotIndex = [Convert]::ToInt32((Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'SlotIndex'))
    $targetFileName = [string](Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'TargetFileName')
    $sourceFileName = [string](Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'RecoverySourceFileName')
    $damagedHash = ([string](Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'DamagedCurrentSha256')).ToUpperInvariant()
    $damagedLength = [Convert]::ToInt64((Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'DamagedCurrentLength'))
    $recoveryHash = ([string](Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'RecoverySha256')).ToUpperInvariant()
    $recoveryLength = [Convert]::ToInt64((Get-DtmRecoveryManifestValue -Manifest $manifest -Name 'RecoveryLength'))
    if ($formatVersion -ne 1 -or $slotIndex -ne 0 -or
        $targetFileName -cne 'doloc-save-0.data' -or
        $sourceFileName -cne 'verified-recovery-source.data' -or
        $damagedLength -lt 1 -or $recoveryLength -lt 1 -or
        [string]::IsNullOrWhiteSpace($caseId) -or $caseId -notmatch '^[A-Za-z0-9._-]+$') {
        Throw-DtmRecoveryFailure -Code 10 -Message 'Recovery manifest does not match the fixed slot 0 contract.'
    }
    Assert-DtmRecoveryHash -Value $damagedHash -Name 'DamagedCurrentSha256'
    Assert-DtmRecoveryHash -Value $recoveryHash -Name 'RecoverySha256'
    if ($damagedHash -eq $recoveryHash) {
        Throw-DtmRecoveryFailure -Code 10 -Message 'Damaged and recovery hashes must differ.'
    }

    $testFixture = $false
    $testProperty = $manifest.PSObject.Properties['TestFixture']
    if ($null -ne $testProperty) {
        $testFixture = [Convert]::ToBoolean($testProperty.Value)
    }
    $testSkipValue = [Environment]::GetEnvironmentVariable('DTMAPI_RECOVERY_TEST_SKIP_PROCESS_CHECK')
    if (-not [string]::IsNullOrWhiteSpace($testSkipValue) -and
        @('1', 'true', 'yes', 'on') -contains $testSkipValue.Trim().ToLowerInvariant()) {
        if (-not $testFixture -or -not $caseId.StartsWith('TEST-', [StringComparison]::Ordinal)) {
            Throw-DtmRecoveryFailure -Code 10 -Message 'The process-check test override is not allowed by this package.'
        }
        $SkipProcessCheck = $true
    }
    if (($SkipProcessCheck -or -not [string]::IsNullOrWhiteSpace($SaveDirectory)) -and
        (-not $testFixture -or -not $caseId.StartsWith('TEST-', [StringComparison]::Ordinal))) {
        Throw-DtmRecoveryFailure -Code 10 -Message 'Test-only recovery overrides are not allowed by this package.'
    }

    $sourceInfo = New-Object System.IO.FileInfo($sourceFull)
    if ($sourceInfo.Length -ne $recoveryLength) {
        Throw-DtmRecoveryFailure -Code 30 -Message 'Verified recovery source length does not match its manifest.'
    }
    $sourceHash = Get-DtmRecoverySha256 -Path $sourceFull
    if ($sourceHash -ne $recoveryHash) {
        Throw-DtmRecoveryFailure -Code 30 -Message 'Verified recovery source SHA-256 does not match its manifest.'
    }

    $saveRoot = Resolve-DtmRecoverySaveDirectory -TargetFileName $targetFileName
    if (-not [System.IO.Directory]::Exists($saveRoot) -or
        [System.IO.Path]::GetFileName($saveRoot) -ine 'SAVE') {
        Throw-DtmRecoveryFailure -Code 30 -Message ('Invalid SAVE directory: ' + $saveRoot)
    }
    Assert-DtmRecoveryNoReparsePoint -Path $saveRoot -Label 'SAVE directory'
    $script:SaveRoot = $saveRoot
    $savePrefix = $saveRoot.TrimEnd('\', '/') + '\'
    $targetFull = [System.IO.Path]::GetFullPath((Join-Path $saveRoot $targetFileName))
    if (-not $targetFull.StartsWith($savePrefix, [StringComparison]::OrdinalIgnoreCase) -or
        -not [System.IO.File]::Exists($targetFull)) {
        Throw-DtmRecoveryFailure -Code 30 -Message 'The expected slot 0 current save is missing.'
    }
    Assert-DtmRecoveryNoReparsePoint -Path $targetFull -Label 'Slot 0 current save'

    Assert-DtmRecoveryProcessesClosed
    $targetInfo = New-Object System.IO.FileInfo($targetFull)
    $targetHash = Get-DtmRecoverySha256 -Path $targetFull
    if ($targetHash -eq $recoveryHash) {
        Write-Host ('[OK] Slot 0 is already restored. Case=' + $caseId) -ForegroundColor Green
        exit 0
    }
    if ($targetHash -ne $damagedHash -or $targetInfo.Length -ne $damagedLength) {
        Throw-DtmRecoveryFailure -Code 30 -Message ('Current slot 0 has changed since collection. Recovery refused. SHA256=' + $targetHash)
    }

    $partialName = '.dtmapi-verified-recovery-' + [Guid]::NewGuid().ToString('N') + '.partial'
    $script:PartialPath = [System.IO.Path]::GetFullPath((Join-Path $saveRoot $partialName))
    if (-not $script:PartialPath.StartsWith($savePrefix, [StringComparison]::OrdinalIgnoreCase) -or
        [System.IO.File]::Exists($script:PartialPath)) {
        Throw-DtmRecoveryFailure -Code 40 -Message 'Could not reserve a safe same-directory recovery partial.'
    }
    [System.IO.File]::Copy($sourceFull, $script:PartialPath, $false)
    [System.IO.File]::SetLastWriteTimeUtc($script:PartialPath, [DateTime]::UtcNow)
    $partialInfo = New-Object System.IO.FileInfo($script:PartialPath)
    if ($partialInfo.Length -ne $recoveryLength -or
        (Get-DtmRecoverySha256 -Path $script:PartialPath) -ne $recoveryHash) {
        Throw-DtmRecoveryFailure -Code 40 -Message 'Same-directory recovery partial verification failed.'
    }

    Assert-DtmRecoveryProcessesClosed
    $preReplaceHash = Get-DtmRecoverySha256 -Path $targetFull
    if ($preReplaceHash -ne $damagedHash) {
        Throw-DtmRecoveryFailure -Code 30 -Message 'Current slot 0 changed during verification. Recovery refused.'
    }
    Invoke-DtmRecoveryAtomicReplace -ReplacementPath $script:PartialPath -TargetPath $targetFull
    $script:PartialPath = ''

    $restoredInfo = New-Object System.IO.FileInfo($targetFull)
    $restoredHash = Get-DtmRecoverySha256 -Path $targetFull
    if ($restoredInfo.Length -ne $recoveryLength -or $restoredHash -ne $recoveryHash) {
        Throw-DtmRecoveryFailure -Code 40 -Message 'Post-recovery slot 0 verification failed. Keep Steam closed and contact the maintainer.'
    }
    Write-Host ('[OK] Restored doloc-save-0.data from the verified encrypted recovery source. Case=' + $caseId) -ForegroundColor Green
    Write-Host ('[OK] SHA256=' + $restoredHash) -ForegroundColor Green
    Write-Host '[OK] Existing .prev backups and unrelated SAVE files were not changed.' -ForegroundColor Green
    exit 0
}
catch {
    $exitCode = 40
    if ($_.Exception.Data.Contains('DtmRecoveryExitCode')) {
        $exitCode = [Convert]::ToInt32($_.Exception.Data['DtmRecoveryExitCode'])
    }
    Remove-DtmRecoveryPartial
    Write-Host ('[ERROR] ' + $_.Exception.Message) -ForegroundColor Red
    exit $exitCode
}
