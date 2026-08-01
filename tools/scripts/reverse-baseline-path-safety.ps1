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

function Test-DtmApiReversePathsOverlap {
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

function Assert-DtmApiReversePathHasNoReparseComponent {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepoRoot,
        [Parameter(Mandatory = $true)]
        [string] $CandidatePath
    )

    $repoPath = Get-DtmApiTrimmedFullPath -Path $RepoRoot
    $candidate = Get-DtmApiTrimmedFullPath -Path $CandidatePath
    $repoPrefix = $repoPath + [System.IO.Path]::DirectorySeparatorChar
    $relativePath = $candidate.Substring($repoPrefix.Length)
    $currentPath = $repoPath

    foreach ($segment in @($relativePath -split '[\\/]+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $currentPath = Join-Path $currentPath $segment
        if (-not (Test-Path -LiteralPath $currentPath)) {
            continue
        }

        $item = Get-Item -LiteralPath $currentPath -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Reverse baseline BuildRoot crosses an existing reparse point and is rejected: $currentPath"
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

    $repoPath = Get-DtmApiTrimmedFullPath -Path $RepoRoot
    $candidate = Get-DtmApiTrimmedFullPath -Path $BuildRoot

    if (-not [string]::IsNullOrWhiteSpace($GameDir) -and
        (Test-DtmApiReversePathsOverlap -First $candidate -Second $GameDir)) {
        throw "Reverse baseline BuildRoot must not be the game folder, a child of it, or its parent: $candidate"
    }

    $isRepoRoot = $candidate.Equals($repoPath, [System.StringComparison]::OrdinalIgnoreCase)
    if (-not $isRepoRoot -and -not (Test-DtmApiReversePathIsDescendant -Child $candidate -Parent $repoPath)) {
        return $candidate
    }

    $allowedRoot = Get-DtmApiTrimmedFullPath -Path (Join-Path $repoPath 'references\doloc-town\reverse')
    if (-not (Test-DtmApiReversePathIsDescendant -Child $candidate -Parent $allowedRoot)) {
        throw "Repository-local reverse baseline BuildRoot must be a child of the ignored reverse root '$allowedRoot': $candidate"
    }

    if ((Test-Path -LiteralPath $candidate) -and -not (Test-Path -LiteralPath $candidate -PathType Container)) {
        throw "Reverse baseline BuildRoot exists but is not a directory: $candidate"
    }

    Assert-DtmApiReversePathHasNoReparseComponent -RepoRoot $repoPath -CandidatePath $candidate

    $repoPrefix = $repoPath + [System.IO.Path]::DirectorySeparatorChar
    $relativePath = $candidate.Substring($repoPrefix.Length).Replace('\', '/')
    $git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
    & $git.Source -C $repoPath check-ignore --quiet -- $relativePath
    $ignoreExitCode = $LASTEXITCODE
    if ($ignoreExitCode -eq 1) {
        throw "Repository-local reverse baseline BuildRoot is not ignored by Git: $relativePath"
    }
    if ($ignoreExitCode -ne 0) {
        throw "Unable to verify the reverse baseline BuildRoot with git check-ignore (exit $ignoreExitCode): $relativePath"
    }

    return $candidate
}
