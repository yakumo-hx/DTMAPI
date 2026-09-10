Set-StrictMode -Version 2.0

$script:DtmApiMultiPlatformRuntimeVersion = '0.6.1'
$script:DtmApiMultiPlatformWorkshopId = '3792681186'
$script:DtmApiMultiPlatformPublishedPayloadFileCount = 28
$script:DtmApiMultiPlatformPublishedPayloadBytes = 3866857L
$script:DtmApiMultiPlatformPublishedPayloadTreeSha256 = 'b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa'
$script:DtmApiMultiPlatformSharedFileCount = 20
$script:DtmApiMultiPlatformSharedBytes = 3826337L
$script:DtmApiMultiPlatformSharedTreeSha256 = 'ae80f6661390b82824af424b2006fb28267ec7181b2f98d75df9975c6a1d20c5'
$script:DtmApiMultiPlatformRequiredLaunchOption = 'WINEDLLOVERRIDES="winhttp=n,b" %command%'

$script:DtmApiMultiPlatformSharedPaths = @(
    'Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip',
    'Content/DTMAPI/release-manifest.json',
    'Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.Abstractions.dll',
    'Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.BepInExBootstrap.dll',
    'Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.Core.dll',
    'Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.GameBridge.DolocTown.dll',
    'Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.ModConfigMenu.dll',
    'Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/assets/branding/dtmapi-icon.png',
    'Content/DTMAPIInstaller/Payload/DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll',
    'Content/DTMAPIInstaller/tools/analyze-startup-evidence.ps1',
    'Content/DTMAPIInstaller/tools/check-dtmapi-status.ps1',
    'Content/DTMAPIInstaller/tools/collect-logs.ps1',
    'Content/DTMAPIInstaller/tools/common.ps1',
    'Content/DTMAPIInstaller/tools/dtmapi-runtime-version.props',
    'Content/DTMAPIInstaller/tools/install-bepinex.ps1',
    'Content/DTMAPIInstaller/tools/install-to-game.ps1',
    'Content/DTMAPIInstaller/tools/invoke-dtmapi-action.cmd',
    'Content/DTMAPIInstaller/tools/probe-powershell-host.ps1',
    'Content/DTMAPIInstaller/tools/release-common.ps1',
    'Content/DTMAPIInstaller/tools/uninstall-dtmapi.ps1'
)

function Get-DtmApiMultiPlatformRepoRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
}

function Get-DtmApiMultiPlatformDefaultAcceptedPackageRoot {
    $profile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
    if ([string]::IsNullOrWhiteSpace($profile)) {
        throw 'Could not resolve the current user profile for the accepted Runtime package default.'
    }

    return [System.IO.Path]::GetFullPath((Join-Path $profile 'AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI'))
}

function Get-DtmApiMultiPlatformRelativePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([char]92, [char]47)
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $prefix = $resolvedRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the expected tree. root=$resolvedRoot path=$resolvedPath"
    }

    return $resolvedPath.Substring($prefix.Length).Replace('\', '/')
}

function Get-DtmApiMultiPlatformFileSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
        $stream.Dispose()
    }
}

function Get-DtmApiMultiPlatformTextSha256 {
    param([Parameter(Mandatory = $true)] [string] $Text)

    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = New-Object System.Text.UTF8Encoding($false)
        return [System.BitConverter]::ToString($hasher.ComputeHash($bytes.GetBytes($Text))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
}

function Assert-DtmApiMultiPlatformOrdinaryTree {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $resolved -PathType Container)) {
        throw "$Context must be an existing directory: $resolved"
    }

    $items = @((Get-Item -LiteralPath $resolved -Force -ErrorAction Stop)) + @(Get-ChildItem -LiteralPath $resolved -Force -Recurse -ErrorAction Stop)
    $reparse = @($items | Where-Object {
        ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
    })
    if ($reparse.Count -gt 0) {
        throw "$Context must not contain a reparse point: $($reparse[0].FullName)"
    }

    return $resolved
}

function Test-DtmApiMultiPlatformIgnoredAcceptedPath {
    param([Parameter(Mandatory = $true)] [string] $RelativePath)

    if ([string]::Equals($RelativePath, 'workshop.json', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }
    return $RelativePath.StartsWith('Content/.tools/bepinex/extract/', [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-DtmApiMultiPlatformTreeReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [string[]] $RelativePaths = @(),
        [switch] $AcceptedPlayerPayload
    )

    $resolvedRoot = Assert-DtmApiMultiPlatformOrdinaryTree -Path $Root -Context 'Tree receipt root'
    $paths = New-Object 'System.Collections.Generic.List[string]'
    if ($RelativePaths.Count -gt 0) {
        foreach ($relativePath in @($RelativePaths)) {
            $normalized = ([string]$relativePath).Replace('\', '/').TrimStart('/')
            if ([string]::IsNullOrWhiteSpace($normalized) -or
                [System.IO.Path]::IsPathRooted($normalized) -or
                @($normalized.Split('/')) -contains '..') {
                throw "Unsafe tree-receipt relative path: $relativePath"
            }
            $paths.Add($normalized) | Out-Null
        }
    }
    else {
        foreach ($file in @(Get-ChildItem -LiteralPath $resolvedRoot -File -Force -Recurse -ErrorAction Stop)) {
            $relative = Get-DtmApiMultiPlatformRelativePath -Root $resolvedRoot -Path $file.FullName
            if ($AcceptedPlayerPayload -and (Test-DtmApiMultiPlatformIgnoredAcceptedPath -RelativePath $relative)) {
                continue
            }
            $paths.Add($relative) | Out-Null
        }
    }

    $orderedPaths = [string[]]$paths.ToArray()
    [Array]::Sort($orderedPaths, [System.StringComparer]::Ordinal)
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    $rows = New-Object 'System.Collections.Generic.List[string]'
    $files = New-Object 'System.Collections.Generic.List[object]'
    [long]$bytes = 0
    foreach ($relative in $orderedPaths) {
        if (-not $seen.Add($relative)) {
            throw "Duplicate tree-receipt relative path: $relative"
        }
        $fullPath = Join-Path $resolvedRoot $relative.Replace('/', '\')
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            throw "Tree-receipt file is missing: $relative"
        }
        $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Tree-receipt file must not be a reparse point: $relative"
        }
        $hash = Get-DtmApiMultiPlatformFileSha256 -Path $fullPath
        $rows.Add($hash + '  ' + $relative) | Out-Null
        $bytes += [long]$item.Length
        $files.Add([pscustomobject][ordered]@{
            RelativePath = $relative
            Length = [long]$item.Length
            Sha256 = $hash
        }) | Out-Null
    }

    $manifestText = [string]::Join("`n", $rows.ToArray())
    return [pscustomobject][ordered]@{
        Algorithm = 'DTMAPI-Published-SHA256SUMS-v1'
        FileCount = $orderedPaths.Count
        Bytes = $bytes
        TreeSha256 = Get-DtmApiMultiPlatformTextSha256 -Text $manifestText
        Files = @($files.ToArray())
        ManifestText = $manifestText
    }
}

function Assert-DtmApiMultiPlatformWorkshopControlFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [string] $ExpectedWorkshopId = $script:DtmApiMultiPlatformWorkshopId
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "Workshop upload control file is missing: $resolved"
    }
    $item = Get-Item -LiteralPath $resolved -Force -ErrorAction Stop
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Workshop upload control file must not be a reparse point: $resolved"
    }
    try {
        $control = [System.IO.File]::ReadAllText($resolved, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    }
    catch {
        throw "Workshop upload control file is invalid JSON: $resolved. $($_.Exception.Message)"
    }
    $properties = @($control.PSObject.Properties)
    if ($properties.Count -ne 1 -or
        -not [string]::Equals([string]$properties[0].Name, 'workshop_id', [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$control.workshop_id, $ExpectedWorkshopId, [System.StringComparison]::Ordinal)) {
        throw "Workshop upload control file must contain only workshop_id=${ExpectedWorkshopId}: $resolved"
    }

    return [pscustomobject][ordered]@{
        Path = $resolved
        WorkshopId = $ExpectedWorkshopId
        Bytes = [long]$item.Length
        Sha256 = Get-DtmApiMultiPlatformFileSha256 -Path $resolved
    }
}

function Get-DtmApiMultiPlatformPublishedContentReceipt {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $resolved = Assert-DtmApiMultiPlatformOrdinaryTree -Path $Root -Context 'Published multi-platform content receipt root'
    $relativePaths = @(
        Get-ChildItem -LiteralPath $resolved -File -Force -Recurse -ErrorAction Stop |
            ForEach-Object { Get-DtmApiMultiPlatformRelativePath -Root $resolved -Path $_.FullName } |
            Where-Object { -not [string]::Equals($_, 'workshop.json', [System.StringComparison]::OrdinalIgnoreCase) }
    )
    return Get-DtmApiMultiPlatformTreeReceipt -Root $resolved -RelativePaths $relativePaths
}

function Assert-DtmApiMultiPlatformPublishedRuntimePackage {
    param([Parameter(Mandatory = $true)] [string] $PackageRoot)

    $resolved = Assert-DtmApiMultiPlatformOrdinaryTree -Path $PackageRoot -Context 'Accepted published Runtime package'
    $receipt = Get-DtmApiMultiPlatformTreeReceipt -Root $resolved -AcceptedPlayerPayload
    if ($receipt.FileCount -ne $script:DtmApiMultiPlatformPublishedPayloadFileCount -or
        $receipt.Bytes -ne $script:DtmApiMultiPlatformPublishedPayloadBytes -or
        -not [string]::Equals($receipt.TreeSha256, $script:DtmApiMultiPlatformPublishedPayloadTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ("Accepted Runtime package is not the exact published 0.6.1 player payload. Expected={0} files/{1} bytes/{2}; Actual={3} files/{4} bytes/{5}; Path={6}" -f
            $script:DtmApiMultiPlatformPublishedPayloadFileCount,
            $script:DtmApiMultiPlatformPublishedPayloadBytes,
            $script:DtmApiMultiPlatformPublishedPayloadTreeSha256,
            $receipt.FileCount,
            $receipt.Bytes,
            $receipt.TreeSha256,
            $resolved)
    }

    $releaseManifestPath = Join-Path $resolved 'Content\DTMAPI\release-manifest.json'
    try {
        $releaseManifest = [System.IO.File]::ReadAllText($releaseManifestPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    }
    catch {
        throw "Accepted Runtime release manifest is unreadable: $releaseManifestPath. $($_.Exception.Message)"
    }
    if (-not [string]::Equals([string]$releaseManifest.DTMAPIVersion, $script:DtmApiMultiPlatformRuntimeVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$releaseManifest.PackageKind, 'workshop-runtime', [System.StringComparison]::Ordinal)) {
        throw "Accepted Runtime release manifest is not the published 0.6.1 Workshop Runtime authority: $releaseManifestPath"
    }

    $sharedReceipt = Get-DtmApiMultiPlatformTreeReceipt -Root $resolved -RelativePaths $script:DtmApiMultiPlatformSharedPaths
    if ($sharedReceipt.FileCount -ne $script:DtmApiMultiPlatformSharedFileCount -or
        $sharedReceipt.Bytes -ne $script:DtmApiMultiPlatformSharedBytes -or
        -not [string]::Equals($sharedReceipt.TreeSha256, $script:DtmApiMultiPlatformSharedTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ("Accepted Runtime shared-path projection is not exact. Expected={0} files/{1} bytes/{2}; Actual={3} files/{4} bytes/{5}" -f
            $script:DtmApiMultiPlatformSharedFileCount,
            $script:DtmApiMultiPlatformSharedBytes,
            $script:DtmApiMultiPlatformSharedTreeSha256,
            $sharedReceipt.FileCount,
            $sharedReceipt.Bytes,
            $sharedReceipt.TreeSha256)
    }

    return [pscustomobject][ordered]@{
        Root = $resolved
        RuntimeVersion = [string]$releaseManifest.DTMAPIVersion
        BuildCommit = [string]$releaseManifest.BuildCommit
        PlayerPayload = $receipt
        SharedPayload = $sharedReceipt
    }
}

function Assert-DtmApiMultiPlatformPeX64 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 512 -or $bytes[0] -ne 0x4d -or $bytes[1] -ne 0x5a) {
        throw "Windows host is not a PE executable: $Path"
    }
    $peOffset = [System.BitConverter]::ToInt32($bytes, 0x3c)
    if ($peOffset -lt 0x40 -or ($peOffset + 26) -gt $bytes.Length -or
        $bytes[$peOffset] -ne 0x50 -or $bytes[$peOffset + 1] -ne 0x45 -or
        $bytes[$peOffset + 2] -ne 0 -or $bytes[$peOffset + 3] -ne 0) {
        throw "Windows host has an invalid PE header: $Path"
    }
    $machine = [System.BitConverter]::ToUInt16($bytes, $peOffset + 4)
    $optionalMagic = [System.BitConverter]::ToUInt16($bytes, $peOffset + 24)
    if ($machine -ne 0x8664 -or $optionalMagic -ne 0x20b) {
        throw ("Windows host must be an x64 PE32+ executable. Machine=0x{0:x4} OptionalMagic=0x{1:x4} Path={2}" -f $machine, $optionalMagic, $Path)
    }
}

function Assert-DtmApiMultiPlatformElfX64 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $bytes = New-Object byte[] 20
        if ($stream.Read($bytes, 0, $bytes.Length) -ne $bytes.Length -or
            $bytes[0] -ne 0x7f -or $bytes[1] -ne 0x45 -or $bytes[2] -ne 0x4c -or $bytes[3] -ne 0x46) {
            throw "Linux host is not an ELF executable: $Path"
        }
        if ($bytes[4] -ne 2 -or $bytes[5] -ne 1 -or $bytes[18] -ne 0x3e -or $bytes[19] -ne 0) {
            throw "Linux host must be a little-endian ELF64 x86-64 executable: $Path"
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Write-DtmApiMultiPlatformJsonNoBom {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    $compactJson = $Value | ConvertTo-Json -Depth 20 -Compress
    $json = ConvertTo-DtmApiMultiPlatformDeterministicPrettyJson -CompactJson $compactJson
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json + "`n", $encoding)
}

function ConvertTo-DtmApiMultiPlatformDeterministicPrettyJson {
    param([Parameter(Mandatory = $true)] [string] $CompactJson)

    $builder = New-Object System.Text.StringBuilder
    $closingTokens = New-Object 'System.Collections.Generic.Stack[char]'
    $indent = 0
    $inString = $false
    $escaped = $false
    for ($index = 0; $index -lt $CompactJson.Length; $index++) {
        $character = $CompactJson[$index]
        if ($inString) {
            $builder.Append($character) | Out-Null
            if ($escaped) {
                $escaped = $false
            }
            elseif ($character -eq '\') {
                $escaped = $true
            }
            elseif ($character -eq '"') {
                $inString = $false
            }
            continue
        }

        if ($character -eq '"') {
            $inString = $true
            $builder.Append($character) | Out-Null
            continue
        }
        if ($character -eq '{' -or $character -eq '[') {
            $builder.Append($character) | Out-Null
            $closing = if ($character -eq '{') { '}' } else { ']' }
            if ($index + 1 -lt $CompactJson.Length -and $CompactJson[$index + 1] -eq $closing) {
                $builder.Append($closing) | Out-Null
                $index++
                continue
            }
            $closingTokens.Push([char]$closing)
            $indent = $closingTokens.Count
            $builder.Append("`n") | Out-Null
            $builder.Append(' ' * ($indent * 2)) | Out-Null
            continue
        }
        if ($character -eq '}' -or $character -eq ']') {
            if ($closingTokens.Count -eq 0 -or $closingTokens.Peek() -ne $character) {
                throw "Deterministic JSON formatting encountered an unmatched closing token '$character'."
            }
            [void]$closingTokens.Pop()
            $indent = $closingTokens.Count
            $builder.Append("`n") | Out-Null
            $builder.Append(' ' * ($indent * 2)) | Out-Null
            $builder.Append($character) | Out-Null
            continue
        }
        if ($character -eq ',') {
            $builder.Append($character) | Out-Null
            $builder.Append("`n") | Out-Null
            $builder.Append(' ' * ($indent * 2)) | Out-Null
            continue
        }
        if ($character -eq ':') {
            $builder.Append(': ') | Out-Null
            continue
        }
        if (-not [char]::IsWhiteSpace($character)) {
            $builder.Append($character) | Out-Null
        }
    }

    if ($inString -or $escaped -or $closingTokens.Count -ne 0 -or $indent -ne 0) {
        throw 'Deterministic JSON formatting ended in an invalid parser state.'
    }
    return $builder.ToString()
}

function Copy-DtmApiMultiPlatformFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Required package input file is missing: $Source"
    }
    $parent = Split-Path -Parent $Destination
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    [System.IO.File]::Copy($Source, $Destination, $true)
}

function Assert-DtmApiMultiPlatformLfNoBomShellFile {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 20 -or
        ($bytes.Length -ge 3 -and $bytes[0] -eq 0xef -and $bytes[1] -eq 0xbb -and $bytes[2] -eq 0xbf) -or
        @($bytes | Where-Object { $_ -eq 0x0d }).Count -gt 0) {
        throw "Shell entry must use UTF-8 without BOM and LF-only line endings: $Path"
    }
    $text = New-Object System.Text.UTF8Encoding($false, $true)
    try {
        $decoded = $text.GetString($bytes)
    }
    catch {
        throw "Shell entry is not valid UTF-8: $Path"
    }
    if (-not $decoded.StartsWith('#!/usr/bin/env bash' + "`n", [System.StringComparison]::Ordinal)) {
        throw "Shell entry has an unexpected shebang: $Path"
    }
}

function Assert-DtmApiMultiPlatformWindowsBatShim {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [ValidateSet('install', 'uninstall', 'status', 'collect-logs')] [string] $Action
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Windows BAT shim is missing: $Path"
    }
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 80 -or @($bytes | Where-Object { $_ -gt 0x7f }).Count -ne 0) {
        throw "Windows BAT shim must remain plain ASCII text: $Path"
    }
    $text = [System.Text.Encoding]::ASCII.GetString($bytes)
    $exactDispatch = '"%~dp0DTMAPI-MultiPlatform-Installer.exe" ' + $Action + ' --pause %*'
    if (-not $text.StartsWith('@echo off', [System.StringComparison]::Ordinal) -or
        $text.IndexOf('setlocal EnableExtensions DisableDelayedExpansion', [System.StringComparison]::Ordinal) -lt 0 -or
        $text.IndexOf($exactDispatch, [System.StringComparison]::Ordinal) -lt 0 -or
        [regex]::Matches($text, '(?i)DTMAPI-MultiPlatform-Installer\.exe').Count -ne 1 -or
        $text.IndexOf('set "DTMAPI_EXIT=%ERRORLEVEL%"', [System.StringComparison]::Ordinal) -lt 0 -or
        $text.IndexOf('exit /b %DTMAPI_EXIT%', [System.StringComparison]::Ordinal) -lt 0 -or
        $text.IndexOf('powershell', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('invoke-dtmapi-action', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        [regex]::IsMatch($text, '(?m)^\s*:[^:]')) {
        throw "Windows BAT shim must invoke exactly one quoted root host action with --pause, propagate its exit code, and remain label-free: $Path"
    }
}


# Schema 1 remains the immutable observed 0.6.1 branch. Candidate receipts are
# generated from the selected Windows artifact, never from a Steam observation.
function Assert-DtmApiMultiPlatformAcceptedRuntimePackage {
    param([string] $PackageRoot, [ValidateSet('ObservedPublished','Candidate')] [string] $SourceKind = 'ObservedPublished')
    if ($SourceKind -eq 'ObservedPublished') {
        $accepted = Assert-DtmApiMultiPlatformPublishedRuntimePackage -PackageRoot $PackageRoot
        $accepted | Add-Member NoteProperty SourceKind $SourceKind
        $accepted | Add-Member NoteProperty SharedPaths $script:DtmApiMultiPlatformSharedPaths
        return $accepted
    }
    $resolved = Assert-DtmApiMultiPlatformOrdinaryTree -Path $PackageRoot -Context 'Candidate Windows Runtime'
    if (Test-Path -LiteralPath (Join-Path $resolved 'workshop.json')) { throw 'Candidate source must not carry a Workshop upload control file.' }
    $releasePath = Join-Path $resolved 'Content/DTMAPI/release-manifest.json'
    $release = Get-Content -LiteralPath $releasePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($release.SchemaVersion -ne 1 -or $release.DTMAPIVersion -cne '0.7.0' -or $release.BinaryVersion -cne '0.7.0.0' -or
        $release.PackageKind -cne 'workshop-runtime' -or $release.BuildCommit -notmatch '^[0-9a-f]{12,40}$') {
        throw 'Candidate source requires exact Runtime 0.7.0 / binary 0.7.0.0 and a real build commit.'
    }
    $commit = [string](& git -C (Get-DtmApiMultiPlatformRepoRoot) rev-parse --verify ($release.BuildCommit + '^{commit}'))
    if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') { throw 'Candidate Runtime build commit cannot be resolved.' }
    $names = @('DTMAPI.Abstractions.dll','DTMAPI.BepInExBootstrap.dll','DTMAPI.Core.dll','DTMAPI.GameBridge.DolocTown.dll','DTMAPI.ModConfigMenu.dll')
    if ((@($release.IncludedAssemblies.FileName | Sort-Object) -join '|') -cne (($names | Sort-Object) -join '|') -or
        @($release.OptionalComponents).Count -ne 1) { throw 'Candidate Runtime assembly/component set is invalid.' }
    $component = $release.OptionalComponents[0]
    if ($component.ComponentId -cne 'gamebridge-compatibility-host' -or
        $component.RelativePath.Replace('\','/') -cne 'DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll') { throw 'Candidate compatibility identity is invalid.' }
    $rows = @($release.IncludedAssemblies | ForEach-Object {
        [pscustomobject]@{ Path = 'BepInEx/plugins/DTMAPI/' + $_.FileName; Receipt = $_ }
    }) + @([pscustomobject]@{ Path = $component.RelativePath; Receipt = $component })
    foreach ($row in $rows) {
        $file = Join-Path $resolved ('Content/DTMAPIInstaller/Payload/' + $row.Path)
        if ((Get-Item -LiteralPath $file).Length -ne $row.Receipt.Length -or
            (Get-DtmApiMultiPlatformFileSha256 -Path $file) -cne $row.Receipt.Sha256 -or
            $row.Receipt.FileVersion -cne $release.BinaryVersion -or
            [Diagnostics.FileVersionInfo]::GetVersionInfo($file).FileVersion -cne $release.BinaryVersion) { throw "Candidate Runtime receipt/version mismatch: $file" }
    }
    $paths = @($script:DtmApiMultiPlatformSharedPaths) + 'Content/DTMAPIInstaller/tools/dtmapi-product-definitions.json'
    $shared = Get-DtmApiMultiPlatformTreeReceipt -Root $resolved -RelativePaths $paths
    [xml]$props = Get-Content -LiteralPath (Join-Path $resolved 'Content/DTMAPIInstaller/tools/dtmapi-runtime-version.props') -Raw
    if ($props.Project.PropertyGroup.DtmApiReleaseVersion -cne $release.DTMAPIVersion -or
        $props.Project.PropertyGroup.DtmApiBinaryFileVersion -cne $release.BinaryVersion) { throw 'Candidate version authority disagrees with release manifest.' }
    return [pscustomobject]@{
        Root = $resolved; SourceKind = $SourceKind; RuntimeVersion = $release.DTMAPIVersion; BuildCommit = $release.BuildCommit
        SharedPaths = $paths; SharedPayload = $shared
        PlayerPayload = Get-DtmApiMultiPlatformTreeReceipt -Root $resolved -AcceptedPlayerPayload
        RuntimeSource = [ordered]@{
            Kind = 'Candidate'; BuildCommit = $commit
            ReleaseManifestSha256 = Get-DtmApiMultiPlatformFileSha256 -Path $releasePath
            SharedFiles = @($shared.Files)
        }
    }
}

function Get-DtmApiMultiPlatformCandidateInfoText {
    param([string] $MetadataRoot, [string] $RuntimeVersion)
    # Preserve all localized metadata verbatim; only project the version field.
    $text = [IO.File]::ReadAllText((Join-Path $MetadataRoot 'info.json'))
    if ([regex]::Matches($text, '"version": "0\.6\.1"').Count -ne 1) { throw 'Legacy info template version is not exact.' }
    return $text.Replace('"version": "0.6.1"', ('"version": "' + $RuntimeVersion + '"'))
}

function Assert-DtmApiMultiPlatformHostBuildReceipt {
    param($Receipt, [string] $WindowsHost, [string] $LinuxHost)
    if ($Receipt.SchemaVersion -ne 1 -or $Receipt.Kind -cne 'CommittedInstallerBuild' -or
        $Receipt.Commit -notmatch '^[0-9a-f]{40}$' -or $Receipt.InstallerVersion -cne '0.2.0-experimental' -or
        @($Receipt.Inputs.PSObject.Properties).Count -lt 10 -or @($Receipt.Hosts).Count -ne 2) { throw 'Installer build receipt is invalid.' }
    $repo = Get-DtmApiMultiPlatformRepoRoot
    $resolved = [string](& git -C $repo rev-parse --verify ($Receipt.Commit + '^{commit}'))
    if ($LASTEXITCODE -ne 0 -or $resolved -cne $Receipt.Commit) { throw 'Installer build commit is not real.' }
    foreach ($rid in @('win-x64','linux-x64')) {
        $rows = @($Receipt.Hosts | Where-Object { $_.Rid -ceq $rid })
        $path = if ($rid -eq 'win-x64') { $WindowsHost } else { $LinuxHost }
        if ($rows.Count -ne 1 -or $rows[0].Length -ne (Get-Item -LiteralPath $path).Length -or
            $rows[0].Sha256 -cne (Get-DtmApiMultiPlatformFileSha256 -Path $path)) { throw "Installer build output differs for $rid." }
    }
}
