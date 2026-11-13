Set-StrictMode -Version 2.0

function Get-AuthorSdkSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Cannot hash missing file: $Path"
    }
    return ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash).ToLowerInvariant()
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

function Get-AuthorSdkReleaseVersion {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    [xml]$project = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $RepoRoot 'src\DTMAPI.AuthorSdk\DTMAPI.AuthorSdk.csproj')
    $version = [string]$project.Project.PropertyGroup.Version
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Author SDK project must declare one release version.' }
    return $version
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
