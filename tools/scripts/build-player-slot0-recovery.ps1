[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $SupportZip,
    [Parameter(Mandatory = $true)] [string] $OutputZip,
    [Parameter(Mandatory = $true)] [string] $CaseId,
    [Parameter(Mandatory = $true)] [string] $ExpectedDamagedSha256,
    [Parameter(Mandatory = $true)] [string] $ExpectedRecoverySha256,
    [string] $PreparedRecoveryFile = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-DtmRecoveryBuildSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::OpenRead($Path)
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

function Get-DtmRecoveryBuildStreamSha256 {
    param([Parameter(Mandatory = $true)] [System.IO.Stream] $Stream)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function Get-DtmRecoveryBuildEntrySha256 {
    param([Parameter(Mandatory = $true)] [System.IO.Compression.ZipArchiveEntry] $Entry)

    $stream = $Entry.Open()
    try {
        return Get-DtmRecoveryBuildStreamSha256 -Stream $stream
    }
    finally {
        $stream.Dispose()
    }
}

if ($CaseId -notmatch '^[A-Za-z0-9._-]+$' -or $CaseId.StartsWith('TEST-', [StringComparison]::Ordinal)) {
    throw 'CaseId must be a production-safe identifier and must not use the TEST- prefix.'
}
$ExpectedDamagedSha256 = $ExpectedDamagedSha256.ToUpperInvariant()
$ExpectedRecoverySha256 = $ExpectedRecoverySha256.ToUpperInvariant()
if ($ExpectedDamagedSha256 -notmatch '^[A-F0-9]{64}$' -or
    $ExpectedRecoverySha256 -notmatch '^[A-F0-9]{64}$' -or
    $ExpectedDamagedSha256 -eq $ExpectedRecoverySha256) {
    throw 'Expected SHA-256 values are invalid.'
}

$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$sourceRoot = Join-Path $repo 'tools\release\player-slot0-recovery'
$supportFull = [System.IO.Path]::GetFullPath($SupportZip)
$outputFull = [System.IO.Path]::GetFullPath($OutputZip)
$preparedFull = ''
if (-not [System.IO.File]::Exists($supportFull)) {
    throw 'Support ZIP was not found.'
}
if (-not [string]::IsNullOrWhiteSpace($PreparedRecoveryFile)) {
    $preparedFull = [System.IO.Path]::GetFullPath($PreparedRecoveryFile)
    if (-not [System.IO.File]::Exists($preparedFull)) {
        throw 'Prepared recovery file was not found.'
    }
    $preparedAttributes = [System.IO.File]::GetAttributes($preparedFull)
    if (($preparedAttributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'Prepared recovery file must not be a reparse point.'
    }
}
if ([System.IO.Path]::GetExtension($outputFull) -ine '.zip') {
    throw 'OutputZip must end in .zip.'
}
if ([System.IO.File]::Exists($outputFull)) {
    throw 'Output ZIP already exists; refusing to overwrite it.'
}

$requiredSourceFiles = @(
    '1_restore_verified_slot0.bat',
    'restore-verified-slot.ps1',
    'README.txt'
)
foreach ($required in $requiredSourceFiles) {
    if (-not [System.IO.File]::Exists((Join-Path $sourceRoot $required))) {
        throw ('Recovery source file is missing: ' + $required)
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($supportFull)
$stageFull = ''
$createdOutput = $false
try {
    $currentEntries = @($archive.Entries | Where-Object {
        $_.FullName.Replace('\', '/').EndsWith('/SAVE/doloc-save-0.data', [StringComparison]::OrdinalIgnoreCase)
    })
    $recoveryEntries = @($archive.Entries | Where-Object {
        $_.FullName.Replace('\', '/').EndsWith('/SAVE/doloc-save-0.data.prev0', [StringComparison]::OrdinalIgnoreCase)
    })
    if ($currentEntries.Count -ne 1) {
        throw 'Support ZIP must contain exactly one slot 0 current.'
    }
    if ([string]::IsNullOrWhiteSpace($preparedFull) -and $recoveryEntries.Count -ne 1) {
        throw 'Support ZIP must contain exactly one slot 0 .prev0 when no prepared recovery file is supplied.'
    }
    $currentEntry = $currentEntries[0]
    $currentHash = Get-DtmRecoveryBuildEntrySha256 -Entry $currentEntry
    if ($currentHash -ne $ExpectedDamagedSha256) {
        throw ('Collected current SHA-256 mismatch: ' + $currentHash)
    }

    $recoveryEntry = $null
    $recoveryHash = ''
    $recoveryLength = 0L
    $recoveryLastWriteTimeUtc = [DateTime]::MinValue
    $recoverySourceKind = ''
    $recoverySourceEntry = ''
    $recoveryOriginalFileName = ''
    if ([string]::IsNullOrWhiteSpace($preparedFull)) {
        $recoveryEntry = $recoveryEntries[0]
        $recoveryHash = Get-DtmRecoveryBuildEntrySha256 -Entry $recoveryEntry
        $recoveryLength = [long]$recoveryEntry.Length
        $recoveryLastWriteTimeUtc = $recoveryEntry.LastWriteTime.UtcDateTime
        $recoverySourceKind = 'NativeBackupPrev0'
        $recoverySourceEntry = $recoveryEntry.FullName
        $recoveryOriginalFileName = [System.IO.Path]::GetFileName($recoveryEntry.FullName)
    }
    else {
        $preparedInfo = New-Object System.IO.FileInfo($preparedFull)
        $recoveryHash = Get-DtmRecoveryBuildSha256 -Path $preparedFull
        $recoveryLength = [long]$preparedInfo.Length
        $recoveryLastWriteTimeUtc = $preparedInfo.LastWriteTimeUtc
        $recoverySourceKind = 'PreparedEncryptedArchive'
        $recoveryOriginalFileName = $preparedInfo.Name
    }
    if ($recoveryHash -ne $ExpectedRecoverySha256) {
        throw ('Recovery source SHA-256 mismatch: ' + $recoveryHash)
    }

    $tempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
    $tempPrefix = $tempRoot.TrimEnd('\', '/') + '\'
    $stageFull = [System.IO.Path]::GetFullPath((Join-Path $tempRoot ('player-slot0-recovery-build-' + [Guid]::NewGuid().ToString('N'))))
    if (-not $stageFull.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Recovery staging escaped repository temp.'
    }
    [System.IO.Directory]::CreateDirectory($stageFull) | Out-Null
    foreach ($required in $requiredSourceFiles) {
        [System.IO.File]::Copy((Join-Path $sourceRoot $required), (Join-Path $stageFull $required), $false)
    }

    $recoveryOutput = Join-Path $stageFull 'verified-recovery-source.data'
    if ([string]::IsNullOrWhiteSpace($preparedFull)) {
        $entryStream = $recoveryEntry.Open()
        try {
            $outputStream = [System.IO.File]::Open($recoveryOutput, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $entryStream.CopyTo($outputStream)
            }
            finally {
                $outputStream.Dispose()
            }
        }
        finally {
            $entryStream.Dispose()
        }
    }
    else {
        [System.IO.File]::Copy($preparedFull, $recoveryOutput, $false)
    }
    [System.IO.File]::SetLastWriteTimeUtc($recoveryOutput, $recoveryLastWriteTimeUtc)
    if ((Get-DtmRecoveryBuildSha256 -Path $recoveryOutput) -ne $ExpectedRecoverySha256) {
        throw 'Extracted recovery source failed its post-copy SHA-256 check.'
    }

    $manifest = [ordered]@{
        FormatVersion = 1
        CaseId = $CaseId
        SlotIndex = 0
        TargetFileName = 'doloc-save-0.data'
        RecoverySourceFileName = 'verified-recovery-source.data'
        DamagedCurrentSha256 = $ExpectedDamagedSha256
        DamagedCurrentLength = [long]$currentEntry.Length
        RecoverySha256 = $ExpectedRecoverySha256
        RecoveryLength = $recoveryLength
        RecoverySourceKind = $recoverySourceKind
        RecoverySourceEntry = $recoverySourceEntry
        RecoveryOriginalFileName = $recoveryOriginalFileName
        RecoverySourceLastWriteTimeUtc = $recoveryLastWriteTimeUtc.ToString('o')
        SupportPackageFileName = [System.IO.Path]::GetFileName($supportFull)
        SupportPackageSha256 = Get-DtmRecoveryBuildSha256 -Path $supportFull
    }
    $manifestJson = $manifest | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText(
        (Join-Path $stageFull 'recovery-manifest.json'),
        $manifestJson + [Environment]::NewLine,
        (New-Object System.Text.UTF8Encoding($false)))

    $outputParent = [System.IO.Path]::GetDirectoryName($outputFull)
    [System.IO.Directory]::CreateDirectory($outputParent) | Out-Null
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $stageFull,
        $outputFull,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)
    $createdOutput = $true

    $published = [System.IO.Compression.ZipFile]::OpenRead($outputFull)
    try {
        $expectedNames = @(
            '1_restore_verified_slot0.bat',
            'README.txt',
            'recovery-manifest.json',
            'restore-verified-slot.ps1',
            'verified-recovery-source.data'
        ) | Sort-Object
        $actualNames = @($published.Entries | ForEach-Object { $_.FullName.Replace('\', '/') } | Sort-Object)
        if (($actualNames -join "`n") -cne ($expectedNames -join "`n")) {
            throw 'Published recovery ZIP entry set is not exact.'
        }
        $publishedSource = @($published.Entries | Where-Object { $_.FullName -ceq 'verified-recovery-source.data' })
        if ($publishedSource.Count -ne 1 -or
            (Get-DtmRecoveryBuildEntrySha256 -Entry $publishedSource[0]) -ne $ExpectedRecoverySha256) {
            throw 'Published recovery source SHA-256 mismatch.'
        }
    }
    finally {
        $published.Dispose()
    }

    [pscustomobject]@{
        OutputZip = $outputFull
        Length = (New-Object System.IO.FileInfo($outputFull)).Length
        Sha256 = Get-DtmRecoveryBuildSha256 -Path $outputFull
        CaseId = $CaseId
        DamagedCurrentSha256 = $ExpectedDamagedSha256
        RecoverySha256 = $ExpectedRecoverySha256
        RecoverySourceKind = $recoverySourceKind
    }
}
catch {
    if ($createdOutput -and [System.IO.File]::Exists($outputFull)) {
        [System.IO.File]::Delete($outputFull)
    }
    throw
}
finally {
    $archive.Dispose()
    if (-not [string]::IsNullOrWhiteSpace($stageFull) -and
        [System.IO.Directory]::Exists($stageFull)) {
        $tempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))
        $tempPrefix = $tempRoot.TrimEnd('\', '/') + '\'
        $stageLeaf = [System.IO.Path]::GetFileName($stageFull)
        if ($stageFull.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
            $stageLeaf.StartsWith('player-slot0-recovery-build-', [StringComparison]::Ordinal)) {
            [System.IO.Directory]::Delete($stageFull, $true)
        }
    }
}
