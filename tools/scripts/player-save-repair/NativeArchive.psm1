Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
if (-not ('DtmApi.PlayerSaveRepair.ArchiveFormat' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'ArchiveFormat.cs')
}

function Assert-RepairPlainPath {
    param([string] $Path)
    $full = [IO.Path]::GetFullPath($Path)
    $cursor = $full
    while (-not [string]::IsNullOrWhiteSpace($cursor)) {
        if (([IO.File]::Exists($cursor) -or [IO.Directory]::Exists($cursor)) -and
            (([IO.File]::GetAttributes($cursor) -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
            throw 'Repair input/output paths may not traverse reparse points.'
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
    return $full
}

function Read-RepairBytes {
    param([string] $Path, [long] $Limit = 33554432)
    $full = Assert-RepairPlainPath $Path
    $stream = [IO.File]::Open($full, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($stream.Length -lt 1 -or $stream.Length -gt $Limit) { throw 'Repair input size is unsupported.' }
        $memory = New-Object IO.MemoryStream
        try { $stream.CopyTo($memory); return ,$memory.ToArray() }
        finally { $memory.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Get-RepairSha {
    param([byte[]] $Bytes)
    return [DtmApi.PlayerSaveRepair.ArchiveFormat]::Sha256($Bytes)
}

function Read-RepairProfile {
    param([string] $Path)
    $bytes = Read-RepairBytes -Path $Path -Limit 65536
    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    $json = $utf8.GetString($bytes)
    [void][DtmApi.PlayerSaveRepair.JsonReader]::Parse($json)
    $profile = $json | ConvertFrom-Json
    if ($profile.FormatVersion -ne 1 -or $profile.FormatId -cne 'doloc-aes-cbc-utf8-v1' -or
        $profile.ReferenceBuild -cne '24966367_public_958EAF' -or
        $profile.ResourceSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
        $profile.ArchiveFileName -cne 'doloc-save-{0}.data' -or $profile.Prefix -cne 'DOLOC-TOWN:' -or
        @($profile.SupportedSaveVersions).Count -ne 2 -or
        ((@($profile.SupportedSaveVersions | Sort-Object) -join ',') -cne '1.00.02,1.00.06') -or
        $profile.SyntheticFixture -isnot [bool]) {
        throw 'Profile does not describe the reviewed archive format.'
    }
    if (-not $profile.SyntheticFixture -and $profile.ResourceSha256.ToUpperInvariant() -cne 'EF5C6B5BA093595755AF09F6E3A36B860ABD8E2492C542743A337DDC9BEDABE1') {
        throw 'Production profile resource does not match the reviewed GameManager baseline.'
    }
    $key = [Convert]::FromBase64String($profile.KeyBase64)
    $iv = [Convert]::FromBase64String($profile.IVBase64)
    if ($key.Length -notin @(16, 24, 32) -or $iv.Length -ne 16) { throw 'Invalid local format material.' }
    return [pscustomobject]@{
        Sha256 = Get-RepairSha $bytes
        FormatId = $profile.FormatId
        ReferenceBuild = $profile.ReferenceBuild
        ResourceSha256 = $profile.ResourceSha256.ToUpperInvariant()
        SupportedSaveVersions = @($profile.SupportedSaveVersions)
        SyntheticFixture = $profile.SyntheticFixture
        Key = $key
        IV = $iv
    }
}

function Read-RepairZip {
    param([string] $Path, [switch] $SupportPackage)
    $full = Assert-RepairPlainPath $Path
    $file = [IO.File]::Open($full, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($file.Length -gt 1073741824) { throw 'Support ZIP exceeds the 1 GiB input limit.' }
        $zipHashAlgorithm = [Security.Cryptography.SHA256]::Create()
        try { $zipHash = ([BitConverter]::ToString($zipHashAlgorithm.ComputeHash($file))).Replace('-', '') }
        finally { $zipHashAlgorithm.Dispose() }
        $file.Position = 0
        $zip = New-Object IO.Compression.ZipArchive($file, [IO.Compression.ZipArchiveMode]::Read, $true)
        try {
            if ($zip.Entries.Count -gt 20000) { throw 'Support ZIP entry limit exceeded.' }
            $found = @($zip.Entries | Where-Object {
                $name = $_.FullName.Replace('\', '/')
                if ($SupportPackage) { $name.EndsWith('/SAVE/doloc-save-0.data', [StringComparison]::OrdinalIgnoreCase) }
                else { $name -ceq 'doloc-save-0.data' }
            })
            if ($found.Count -ne 1 -or (-not $SupportPackage -and $zip.Entries.Count -ne 1)) {
                throw 'ZIP must contain exactly one unambiguous slot 0 source (candidate ZIP has one entry only).'
            }
            $entry = $found[0]
            if ($entry.FullName.Replace('\', '/') -match '(^/|(^|/)\.\.(/|$)|:)') { throw 'Unsafe archive entry path.' }
            if ($entry.Length -lt 1 -or $entry.Length -gt 33554432) { throw 'ZIP archive entry size is unsupported.' }
            $stream = $entry.Open()
            $memory = New-Object IO.MemoryStream
            try {
                $buffer = New-Object byte[] 65536
                while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                    if ($memory.Length + $read -gt 33554432) { throw 'Expanded archive size limit exceeded.' }
                    $memory.Write($buffer, 0, $read)
                }
                return [pscustomobject]@{ Bytes = $memory.ToArray(); ZipSha256 = $zipHash; Entry = $entry.FullName }
            }
            finally { $stream.Dispose(); $memory.Dispose() }
        }
        finally { $zip.Dispose() }
    }
    finally { $file.Dispose() }
}

function Write-RepairNewBytes {
    param([string] $Path, [byte[]] $Bytes)
    $full = Assert-RepairPlainPath $Path
    $stream = [IO.File]::Open($full, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($Bytes, 0, $Bytes.Length); $stream.Flush($true) }
    finally { $stream.Dispose() }
}

function Write-RepairNewJson {
    param([string] $Path, $Value)
    $utf8 = New-Object Text.UTF8Encoding($false)
    Write-RepairNewBytes -Path $Path -Bytes $utf8.GetBytes(($Value | ConvertTo-Json -Depth 12))
}

function Write-RepairNewZip {
    param([string] $Path, [string] $EntryName, [byte[]] $Bytes)
    $full = Assert-RepairPlainPath $Path
    $file = [IO.File]::Open($full, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $zip = New-Object IO.Compression.ZipArchive($file, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            $entry = $zip.CreateEntry($EntryName, [IO.Compression.CompressionLevel]::Optimal)
            $stream = $entry.Open()
            try { $stream.Write($Bytes, 0, $Bytes.Length) }
            finally { $stream.Dispose() }
        }
        finally { $zip.Dispose() }
    }
    finally { $file.Dispose() }
}

Export-ModuleMember -Function Assert-RepairPlainPath, Read-RepairBytes, Get-RepairSha, Read-RepairProfile, Read-RepairZip, Write-RepairNewBytes, Write-RepairNewJson, Write-RepairNewZip
