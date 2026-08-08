Set-StrictMode -Version 2.0

function Get-AuthorSdkSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Cannot hash missing file: $Path"
    }
    return ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash).ToLowerInvariant()
}

function Write-AuthorSdkUtf8NoBom {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Value
    )

    $parent = Split-Path -Parent $Path
    if ($parent -and -not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Value, $encoding)
}

function Get-AuthorSdkRelativePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    $rootUri = New-Object System.Uri($rootFull)
    $pathUri = New-Object System.Uri($pathFull)
    $relative = [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString())
    if ($relative.StartsWith('../', [System.StringComparison]::Ordinal) -or [System.IO.Path]::IsPathRooted($relative)) {
        throw "Path is outside the expected root: root=$rootFull path=$pathFull"
    }
    return $relative.Replace('\', '/')
}

function Assert-AuthorSdkChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Path,
        [string] $Label = 'path'
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $pathFull = [System.IO.Path]::GetFullPath($Path).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $prefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
    if ($pathFull.Equals($rootFull, [System.StringComparison]::OrdinalIgnoreCase) -or -not $pathFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must be a strict child of '$rootFull': $pathFull"
    }
    return $pathFull
}

function Remove-AuthorSdkTreeSafely {
    param(
        [Parameter(Mandatory = $true)] [string] $AllowedRoot,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $verified = Assert-AuthorSdkChildPath -Root $AllowedRoot -Path $Path -Label 'recursive cleanup target'
    if (Test-Path -LiteralPath $verified) {
        Remove-Item -LiteralPath $verified -Recurse -Force
    }
}

function Get-AuthorSdkContract {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    $path = Join-Path $RepoRoot 'author-sdk\compatibility\0.5.5\compatibility.contract.json'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Tracked Author SDK compatibility contract is missing: $path"
    }
    $contract = Get-Content -Raw -Encoding UTF8 -LiteralPath $path | ConvertFrom-Json
    foreach ($property in @(
        'schemaVersion', 'sdkVersion', 'targetRuntimeVersion', 'abstractionsAssemblyVersion',
        'abstractionsFileVersion', 'abstractionsSha256', 'authorPropsSha256',
        'netstandardReferencePackage', 'netstandardReferencePackageVersion',
        'netstandardReferenceFileCount', 'netstandardReferenceInventoryAlgorithm',
        'netstandardReferenceInventorySha256', 'netstandardLicenseSha256',
        'netstandardNoticeSha256', 'requiredKinds', 'releaseManifestName'
    )) {
        if ($null -eq $contract.PSObject.Properties[$property]) {
            throw "Compatibility contract is missing '$property': $path"
        }
    }
    if ([int]$contract.schemaVersion -ne 1 -or [string]$contract.sdkVersion -ne '0.1.0' -or [string]$contract.targetRuntimeVersion -ne '0.5.5') {
        throw 'Compatibility contract must target Author SDK 0.1.0 and Runtime 0.5.5.'
    }
    if ([string]$contract.netstandardReferencePackage -ne 'NETStandard.Library' -or [string]$contract.netstandardReferencePackageVersion -ne '2.0.3') {
        throw 'Compatibility contract must pin NETStandard.Library 2.0.3.'
    }
    if ([string]$contract.netstandardReferenceInventoryAlgorithm -ne 'DTMAPI-FileTree-SHA256-v1' -or [string]$contract.releaseManifestName -ne 'compatibility.json') {
        throw 'Compatibility contract digest algorithm or manifest name is not the frozen SDK 0.1 contract.'
    }
    $actualKinds = @($contract.requiredKinds | ForEach-Object { [string]$_ } | Sort-Object)
    $expectedKinds = @('abstractions', 'license', 'notice', 'props', 'reference')
    if (@(Compare-Object -ReferenceObject $expectedKinds -DifferenceObject $actualKinds).Count -ne 0 -or $actualKinds.Count -ne $expectedKinds.Count) {
        throw 'Compatibility contract requiredKinds must be exactly abstractions/license/notice/props/reference.'
    }
    return $contract
}

function Get-AuthorSdkFileTreeDigestV1 {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $rootFull = [System.IO.Path]::GetFullPath($Root)
    if (-not (Test-Path -LiteralPath $rootFull -PathType Container)) {
        throw "FileTree root is missing: $rootFull"
    }
    [string[]]$paths = @(Get-ChildItem -LiteralPath $rootFull -File -Recurse | ForEach-Object { $_.FullName })
    [Array]::Sort($paths, [System.Collections.Generic.Comparer[string]]::Create([System.Comparison[string]]{
        param($left, $right)
        return [System.StringComparer]::Ordinal.Compare((Get-AuthorSdkRelativePath -Root $rootFull -Path $left), (Get-AuthorSdkRelativePath -Root $rootFull -Path $right))
    }))

    $aggregate = [System.Security.Cryptography.SHA256]::Create()
    try {
        foreach ($path in $paths) {
            $relative = Get-AuthorSdkRelativePath -Root $rootFull -Path $path
            [byte[]]$relativeBytes = [System.Text.Encoding]::UTF8.GetBytes($relative)
            if ($relativeBytes.Length -gt 0) {
                $aggregate.TransformBlock($relativeBytes, 0, $relativeBytes.Length, $relativeBytes, 0) | Out-Null
            }
            [byte[]]$separator = @(0)
            $aggregate.TransformBlock($separator, 0, 1, $separator, 0) | Out-Null
            [byte[]]$lengthBytes = [System.BitConverter]::GetBytes([Int64](Get-Item -LiteralPath $path).Length)
            if (-not [System.BitConverter]::IsLittleEndian) {
                [Array]::Reverse($lengthBytes)
            }
            $aggregate.TransformBlock($lengthBytes, 0, $lengthBytes.Length, $lengthBytes, 0) | Out-Null
            [byte[]]$fileHash = [System.IO.File]::ReadAllBytes($path)
            $fileSha = [System.Security.Cryptography.SHA256]::Create()
            try {
                $fileHash = $fileSha.ComputeHash($fileHash)
            }
            finally {
                $fileSha.Dispose()
            }
            $aggregate.TransformBlock($fileHash, 0, $fileHash.Length, $fileHash, 0) | Out-Null
        }
        [byte[]]$empty = @()
        $aggregate.TransformFinalBlock($empty, 0, 0) | Out-Null
        return ([System.BitConverter]::ToString($aggregate.Hash) -replace '-', '').ToUpperInvariant()
    }
    finally {
        $aggregate.Dispose()
    }
}

function Get-AuthorSdkReleaseFileKind {
    param([Parameter(Mandatory = $true)] [string] $RelativePath)

    $path = $RelativePath.Replace('\', '/')
    $name = [System.IO.Path]::GetFileName($path)
    if ($path.StartsWith('compatibility/', [System.StringComparison]::Ordinal)) { return 'compatibility' }
    if ($path.StartsWith('templates/', [System.StringComparison]::Ordinal) -or $path.StartsWith('schemas/', [System.StringComparison]::Ordinal)) { return 'asset' }
    if ($path.StartsWith('licenses/', [System.StringComparison]::Ordinal) -or $name.IndexOf('LICENSE', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $name.IndexOf('NOTICE', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { return 'license' }
    if ($name -in @('dtmapi-author.exe', 'dtmapi-author.dll', 'dtmapi-author.deps.json', 'dtmapi-author.runtimeconfig.json')) { return 'cli' }
    if ($name.StartsWith('Microsoft.CodeAnalysis', [System.StringComparison]::OrdinalIgnoreCase)) { return 'compiler' }
    if ($path.StartsWith('contracts/', [System.StringComparison]::Ordinal) -or $name -in @('README.md', 'THIRD-PARTY-NOTICES.md')) { return 'support' }
    if ($name.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase) -or $name.EndsWith('.exe', [System.StringComparison]::OrdinalIgnoreCase) -or $name.EndsWith('.json', [System.StringComparison]::OrdinalIgnoreCase)) { return 'runtime-support' }
    return 'support'
}

function New-AuthorSdkReleaseInventory {
    param(
        [Parameter(Mandatory = $true)] [string] $StageRoot,
        [Parameter(Mandatory = $true)] [string] $DotNetSdkVersion
    )

    $stageFull = [System.IO.Path]::GetFullPath($StageRoot)
    $items = New-Object 'System.Collections.Generic.List[object]'
    [string[]]$files = @(Get-ChildItem -LiteralPath $stageFull -File -Recurse | Where-Object { $_.Name -ne 'author-sdk-release.json' } | ForEach-Object { $_.FullName })
    [Array]::Sort($files, [System.StringComparer]::Ordinal)
    foreach ($file in $files) {
        $relative = Get-AuthorSdkRelativePath -Root $stageFull -Path $file
        $items.Add([ordered]@{
            path = $relative
            length = [Int64](Get-Item -LiteralPath $file).Length
            sha256 = Get-AuthorSdkSha256 -Path $file
            kind = Get-AuthorSdkReleaseFileKind -RelativePath $relative
        }) | Out-Null
    }
    $roslyn = New-Object 'System.Collections.Generic.List[object]'
    foreach ($roslynName in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll')) {
        $roslynPath = Join-Path $stageFull $roslynName
        if (-not (Test-Path -LiteralPath $roslynPath -PathType Leaf)) {
            throw "Published Author SDK is missing the pinned Roslyn compiler component: $roslynName"
        }
        $roslyn.Add([ordered]@{ path = $roslynName; sha256 = Get-AuthorSdkSha256 -Path $roslynPath }) | Out-Null
    }
    return [ordered]@{
        schemaVersion = 1
        sdkVersion = '0.1.0'
        targetRuntimeVersion = '0.5.5'
        buildDotNetSdkVersion = $DotNetSdkVersion
        runtimeIdentifier = 'win-x64'
        packageKind = 'self-contained-portable-author-sdk'
        pathMap = '/_/DTMAPI'
        deterministicZip = [ordered]@{
            entryOrder = 'ordinal-relative-path'
            entryTimestampUtc = '2000-01-01T00:00:00Z'
            compression = 'store'
        }
        roslynCompiler = $roslyn.ToArray()
        files = $items.ToArray()
    }
}

function New-AuthorSdkDeterministicZip {
    param(
        [Parameter(Mandatory = $true)] [string] $SourceRoot,
        [Parameter(Mandatory = $true)] [string] $ZipPath
    )

    Add-Type -AssemblyName System.IO.Compression
    $sourceFull = [System.IO.Path]::GetFullPath($SourceRoot)
    $zipFull = [System.IO.Path]::GetFullPath($ZipPath)
    if (Test-Path -LiteralPath $zipFull) {
        Remove-Item -LiteralPath $zipFull -Force
    }
    $zipParent = Split-Path -Parent $zipFull
    if (-not (Test-Path -LiteralPath $zipParent -PathType Container)) {
        New-Item -ItemType Directory -Path $zipParent -Force | Out-Null
    }

    [string[]]$files = @(Get-ChildItem -LiteralPath $sourceFull -File -Recurse | ForEach-Object { $_.FullName })
    [Array]::Sort($files, [System.Collections.Generic.Comparer[string]]::Create([System.Comparison[string]]{
        param($left, $right)
        return [System.StringComparer]::Ordinal.Compare((Get-AuthorSdkRelativePath -Root $sourceFull -Path $left), (Get-AuthorSdkRelativePath -Root $sourceFull -Path $right))
    }))
    $fileStream = New-Object System.IO.FileStream($zipFull, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $archive = New-Object System.IO.Compression.ZipArchive($fileStream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            $timestamp = New-Object System.DateTimeOffset(2000, 1, 1, 0, 0, 0, [System.TimeSpan]::Zero)
            foreach ($file in $files) {
                $relative = Get-AuthorSdkRelativePath -Root $sourceFull -Path $file
                $entry = $archive.CreateEntry($relative, [System.IO.Compression.CompressionLevel]::NoCompression)
                $entry.LastWriteTime = $timestamp
                $entry.ExternalAttributes = 0
                $input = [System.IO.File]::OpenRead($file)
                try {
                    $output = $entry.Open()
                    try { $input.CopyTo($output) } finally { $output.Dispose() }
                }
                finally { $input.Dispose() }
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $fileStream.Dispose() }
    return $zipFull
}

function Expand-AuthorSdkZipSafely {
    param(
        [Parameter(Mandatory = $true)] [string] $ZipPath,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    Add-Type -AssemblyName System.IO.Compression
    $destinationFull = [System.IO.Path]::GetFullPath($Destination)
    if (-not (Test-Path -LiteralPath $destinationFull -PathType Container)) {
        New-Item -ItemType Directory -Path $destinationFull -Force | Out-Null
    }
    $stream = [System.IO.File]::OpenRead([System.IO.Path]::GetFullPath($ZipPath))
    try {
        $archive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Read, $true)
        try {
            $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($entry in $archive.Entries) {
                $name = $entry.FullName.Replace('\', '/')
                if ([string]::IsNullOrWhiteSpace($name) -or $name.EndsWith('/', [System.StringComparison]::Ordinal)) { continue }
                if ($name.StartsWith('/', [System.StringComparison]::Ordinal) -or $name.Contains('../') -or $name.Contains('/..') -or [System.IO.Path]::IsPathRooted($name)) {
                    throw "Unsafe ZIP entry path: $name"
                }
                if (-not $seen.Add($name)) { throw "Duplicate ZIP entry path: $name" }
                $target = [System.IO.Path]::GetFullPath((Join-Path $destinationFull $name.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
                $prefix = $destinationFull.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
                if (-not $target.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { throw "ZIP entry escapes destination: $name" }
                $parent = Split-Path -Parent $target
                if (-not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
                $input = $entry.Open()
                try {
                    $output = New-Object System.IO.FileStream($target, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
                    try { $input.CopyTo($output) } finally { $output.Dispose() }
                }
                finally { $input.Dispose() }
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $stream.Dispose() }
    return $destinationFull
}
