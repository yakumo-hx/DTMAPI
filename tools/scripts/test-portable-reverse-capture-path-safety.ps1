[CmdletBinding()]
param(
    [string] $TestRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$sessionBase = if (-not [string]::IsNullOrWhiteSpace($TestRoot)) {
    [System.IO.Path]::GetFullPath($TestRoot)
}
elseif (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo 'tmp\test-runs'))
}
$sessionRoot = Join-Path $sessionBase ("portable-reverse-path-safety-" + [Guid]::NewGuid().ToString('N'))
$packageParent = Join-Path $sessionRoot 'package-parent'
$packageRoot = Join-Path $packageParent 'capture-package'
$GameDir = Join-Path $sessionRoot 'game'

function Assert-Rejected {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock] $Action,
        [Parameter(Mandatory = $true)]
        [string] $Label
    )

    try {
        & $Action | Out-Null
    }
    catch {
        return
    }

    throw "Expected portable reverse path rejection: $Label"
}

try {
    [System.IO.Directory]::CreateDirectory($packageRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($GameDir) | Out-Null
    . "$repo\tools\portable-reverse-capture\reverse-baseline-path-safety.ps1"

    $allowedPackageChild = Join-Path $packageRoot 'references\doloc-town\reverse\builds\test-build'
    $resolvedPackageChild = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot $allowedPackageChild -GameDir $GameDir
    if ($resolvedPackageChild -cne [System.IO.Path]::GetFullPath($allowedPackageChild)) {
        throw "Allowed package-local reverse path was not preserved: $resolvedPackageChild"
    }

    $externalBuildRoot = Join-Path $sessionRoot 'external-build'
    $resolvedExternal = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot $externalBuildRoot -GameDir $GameDir
    if ($resolvedExternal -cne [System.IO.Path]::GetFullPath($externalBuildRoot)) {
        throw "Allowed external build path was not preserved: $resolvedExternal"
    }

    Assert-Rejected -Label 'package root' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot $packageRoot -GameDir $GameDir
    }
    Assert-Rejected -Label 'package parent' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot $packageParent -GameDir $GameDir
    }
    Assert-Rejected -Label 'package-local non-reverse child' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot (Join-Path $packageRoot 'output') -GameDir $GameDir
    }
    Assert-Rejected -Label 'game root' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot $GameDir -GameDir $GameDir
    }
    Assert-Rejected -Label 'game parent' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot $sessionRoot -GameDir $GameDir
    }
    Assert-Rejected -Label 'game child' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot (Join-Path $GameDir 'output') -GameDir $GameDir
    }

    Write-Host 'PORTABLE REVERSE CAPTURE PATH SAFETY TEST: OK'
}
finally {
    if (Test-Path -LiteralPath $sessionRoot) {
        Remove-Item -LiteralPath $sessionRoot -Recurse -Force
    }
}
