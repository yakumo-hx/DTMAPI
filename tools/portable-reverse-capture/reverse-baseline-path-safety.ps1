Set-StrictMode -Version Latest

function Get-DtmApiTrimmedFullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $pathRoot = [System.IO.Path]::GetPathRoot($fullPath)
    if ($fullPath.Length -gt $pathRoot.Length) {
        return $fullPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    }

    return $fullPath
}

function Test-DtmApiReversePathIsDescendant {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Child,
        [Parameter(Mandatory = $true)]
        [string] $Parent
    )

    $childPath = Get-DtmApiTrimmedFullPath -Path $Child
    $parentPath = Get-DtmApiTrimmedFullPath -Path $Parent
    $parentPrefix = $parentPath + [System.IO.Path]::DirectorySeparatorChar
    return $childPath.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-PortableCapturePathsOverlap {
    param(
        [Parameter(Mandatory = $true)]
        [string] $First,
        [Parameter(Mandatory = $true)]
        [string] $Second
    )

    $firstPath = Get-DtmApiTrimmedFullPath -Path $First
    $secondPath = Get-DtmApiTrimmedFullPath -Path $Second
    return $firstPath.Equals($secondPath, [System.StringComparison]::OrdinalIgnoreCase) -or
        (Test-DtmApiReversePathIsDescendant -Child $firstPath -Parent $secondPath) -or
        (Test-DtmApiReversePathIsDescendant -Child $secondPath -Parent $firstPath)
}

function Assert-PortableCapturePathHasNoReparseComponent {
    param(
        [Parameter(Mandatory = $true)]
        [string] $CandidatePath
    )

    $candidate = Get-DtmApiTrimmedFullPath -Path $CandidatePath
    $pathRoot = [System.IO.Path]::GetPathRoot($candidate)
    $relativePath = $candidate.Substring($pathRoot.Length)
    $currentPath = $pathRoot

    foreach ($segment in @($relativePath -split '[\\/]+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $currentPath = Join-Path $currentPath $segment
        if (-not (Test-Path -LiteralPath $currentPath)) {
            continue
        }

        $item = Get-Item -LiteralPath $currentPath -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "BuildRoot crosses an existing junction or symbolic link and is rejected: $currentPath"
        }
    }
}

function Resolve-DtmApiReverseBaselineBuildRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepoRoot,
        [Parameter(Mandatory = $true)]
        [string] $BuildRoot,
        [string] $GameDir
    )

    $packageRoot = Get-DtmApiTrimmedFullPath -Path $RepoRoot
    $candidate = Get-DtmApiTrimmedFullPath -Path $BuildRoot

    if ((Test-Path -LiteralPath $candidate) -and -not (Test-Path -LiteralPath $candidate -PathType Container)) {
        throw "BuildRoot exists but is not a directory: $candidate"
    }

    if (-not [string]::IsNullOrWhiteSpace($GameDir) -and
        (Test-PortableCapturePathsOverlap -First $candidate -Second $GameDir)) {
        throw "BuildRoot must not be the game folder, a child of it, or its parent: $candidate"
    }

    $isPackageRoot = $candidate.Equals($packageRoot, [System.StringComparison]::OrdinalIgnoreCase)
    if ($isPackageRoot) {
        throw "BuildRoot must be a dedicated child folder, not the capture-package root: $candidate"
    }
    if (Test-DtmApiReversePathIsDescendant -Child $packageRoot -Parent $candidate) {
        throw "BuildRoot must not be an ancestor of the capture-package root: $candidate"
    }

    if (Test-DtmApiReversePathIsDescendant -Child $candidate -Parent $packageRoot) {
        $allowedRoot = Get-DtmApiTrimmedFullPath -Path (Join-Path $packageRoot 'references\doloc-town\reverse')
        if (-not (Test-DtmApiReversePathIsDescendant -Child $candidate -Parent $allowedRoot)) {
            throw "Package-local BuildRoot must be a child of '$allowedRoot': $candidate"
        }
    }

    Assert-PortableCapturePathHasNoReparseComponent -CandidatePath $candidate
    return $candidate
}
