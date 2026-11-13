Set-StrictMode -Version 2.0
. "$PSScriptRoot/windows-installer/common/files.ps1"

$script:DtmApiMultiPlatformRequiredLaunchOption = 'WINEDLLOVERRIDES="winhttp=n,b" %command%'

Import-DtmApiRuntimePackageFiles

function Get-DtmApiMultiPlatformRepoRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
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
        [Parameter(Mandatory = $true)] [string] $ExpectedWorkshopId
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

function Assert-DtmApiMultiPlatformAcceptedRuntimePackage {
    param([string] $PackageRoot, [ValidateSet('Candidate')] [string] $SourceKind = 'Candidate')
    $resolved = Assert-DtmApiMultiPlatformOrdinaryTree -Path $PackageRoot -Context 'Candidate Windows Runtime'
    $releasePath = Join-Path $resolved 'Content/DTMAPI/release-manifest.json'
    $release = Get-Content -LiteralPath $releasePath -Raw -Encoding UTF8 | ConvertFrom-Json
    [xml]$currentAuthority = Get-Content -LiteralPath (Join-Path (Get-DtmApiMultiPlatformRepoRoot) 'build/dtmapi-runtime-version.props') -Raw
    $currentVersion = [string]$currentAuthority.Project.PropertyGroup.DtmApiReleaseVersion
    $currentBinaryVersion = [string]$currentAuthority.Project.PropertyGroup.DtmApiBinaryFileVersion
    $currentCandidate = $release.DTMAPIVersion -ceq $currentVersion -and $release.BinaryVersion -ceq $currentBinaryVersion
    if ($release.SchemaVersion -ne 1 -or -not $currentCandidate -or
        $release.PackageKind -cne 'workshop-runtime' -or $release.BuildCommit -notmatch '^[0-9a-f]{12,40}$') {
        throw 'Candidate source requires the current Runtime/binary version and a real build commit.'
    }
    $commit = [string](& git -C (Get-DtmApiMultiPlatformRepoRoot) rev-parse --verify ($release.BuildCommit + '^{commit}'))
    if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') { throw 'Candidate Runtime build commit cannot be resolved.' }
    $names = @(Get-DtmApiRuntimeAssemblies)
    $components = @(Get-DtmApiRuntimeComponentFile)
    if ((@($release.IncludedAssemblies.FileName | Sort-Object) -join '|') -cne (($names | Sort-Object) -join '|') -or
        @($release.OptionalComponents).Count -ne $components.Count) { throw 'Candidate Runtime assembly/component set is invalid.' }
    $rows = @($release.IncludedAssemblies | ForEach-Object {
        [pscustomobject]@{ Path = [DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::RuntimeDirectory + $_.FileName; Receipt = $_ }
    }) + @($release.OptionalComponents | ForEach-Object {
        $component = Get-DtmApiRuntimeComponentFile -RelativePath $_.RelativePath
        if ($null -eq $component -or $_.ComponentId -cne $component.Id) { throw 'Unknown Runtime component.' }
        [pscustomobject]@{ Path = $_.RelativePath; Receipt = $_ }
    })
    foreach ($row in $rows) {
        $file = Join-Path $resolved ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::PayloadDirectory + $row.Path)
        $audioAuxiliary = $row.Path.StartsWith('DTMAPI/components/audio/audio-decoder/', [StringComparison]::Ordinal)
        if ((Get-Item -LiteralPath $file).Length -ne $row.Receipt.Length -or
            (Get-DtmApiMultiPlatformFileSha256 -Path $file) -cne $row.Receipt.Sha256 -or
            (-not $audioAuxiliary -and ($row.Receipt.FileVersion -cne $release.BinaryVersion -or
            [Diagnostics.FileVersionInfo]::GetVersionInfo($file).FileVersion -cne $release.BinaryVersion))) { throw "Candidate Runtime receipt/version mismatch: $file" }
    }
    $paths = @([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::GetSharedFiles($true))
    $shared = Get-DtmApiMultiPlatformTreeReceipt -Root $resolved -RelativePaths $paths
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
    $info = Get-Content -LiteralPath (Join-Path $MetadataRoot 'info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $info | Add-Member NoteProperty version $RuntimeVersion -Force
    return ($info | ConvertTo-Json -Depth 20)
}

function Assert-DtmApiMultiPlatformHostBuildReceipt {
    param($Receipt, [string] $WindowsHost, [string] $LinuxHost)
    if ($Receipt.SchemaVersion -ne 1 -or $Receipt.Kind -cne 'CommittedInstallerBuild' -or
        $Receipt.Commit -notmatch '^[0-9a-f]{40}$' -or @($Receipt.Hosts).Count -ne 2) { throw 'Installer build receipt is invalid.' }
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
