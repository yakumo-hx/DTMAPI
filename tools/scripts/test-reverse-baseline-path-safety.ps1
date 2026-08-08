[CmdletBinding()]
param(
    [string] $TestRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\reverse-baseline-path-safety.ps1"

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

$testId = [Guid]::NewGuid().ToString('N')
$sessionRoot = Join-Path $sessionBase "reverse-baseline-path-safety-$testId"
$repoReverseTestRoot = Join-Path $repo "references\doloc-town\reverse\path-safety-tests\$testId"
$junctionPath = Join-Path $repoReverseTestRoot 'junction'
$junctionTarget = Join-Path $sessionRoot 'junction-target'
$gameDir = Join-Path $sessionRoot 'game'

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

    throw "Expected reverse baseline path rejection: $Label"
}

try {
    [System.IO.Directory]::CreateDirectory($sessionRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($gameDir) | Out-Null

    $defaultBuildRoot = Join-Path $repo 'references\doloc-town\reverse\builds\test-build'
    $resolvedDefault = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $defaultBuildRoot
    if ($resolvedDefault -cne [System.IO.Path]::GetFullPath($defaultBuildRoot)) {
        throw "Default ignored reverse path was not preserved: $resolvedDefault"
    }

    Assert-Rejected -Label 'repository root' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $repo
    }
    Assert-Rejected -Label 'tracked docs directory' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot (Join-Path $repo 'docs\reverse-output')
    }
    Assert-Rejected -Label 'reverse sibling prefix' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot (Join-Path $repo 'references\doloc-town\reverse-escape\build')
    }
    Assert-Rejected -Label 'normalized traversal escape' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot (Join-Path $repo 'references\doloc-town\reverse\..\..\..\docs\reverse-output')
    }
    Assert-Rejected -Label 'game root overlap' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $gameDir -GameDir $gameDir
    }
    Assert-Rejected -Label 'game child overlap' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot (Join-Path $gameDir 'output') -GameDir $gameDir
    }
    Assert-Rejected -Label 'game parent overlap' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $sessionRoot -GameDir $gameDir
    }

    $unignoredRepo = Join-Path $sessionRoot 'unignored-repo'
    [System.IO.Directory]::CreateDirectory((Join-Path $unignoredRepo 'references\doloc-town\reverse')) | Out-Null
    & git -C $unignoredRepo init --quiet
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to initialize the unignored-path test repository.'
    }

    $externalBuildRoot = Join-Path $sessionRoot 'external-build'
    $resolvedExternal = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $unignoredRepo -BuildRoot $externalBuildRoot
    if ($resolvedExternal -cne [System.IO.Path]::GetFullPath($externalBuildRoot)) {
        throw "Explicit external path was not preserved: $resolvedExternal"
    }

    Assert-Rejected -Label 'repository-local but unignored reverse child' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $unignoredRepo -BuildRoot (Join-Path $unignoredRepo 'references\doloc-town\reverse\builds\test-build')
    }

    [System.IO.Directory]::CreateDirectory($repoReverseTestRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($junctionTarget) | Out-Null
    New-Item -ItemType Junction -Path $junctionPath -Target $junctionTarget | Out-Null
    Assert-Rejected -Label 'existing junction component' -Action {
        Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot (Join-Path $junctionPath 'escaped-build')
    }

    Write-Host 'REVERSE BASELINE PATH SAFETY TEST: OK'
}
finally {
    if (Test-Path -LiteralPath $junctionPath) {
        Remove-Item -LiteralPath $junctionPath -Force
    }
    if (Test-Path -LiteralPath $repoReverseTestRoot) {
        Remove-Item -LiteralPath $repoReverseTestRoot -Recurse -Force
    }
    if (Test-Path -LiteralPath $sessionRoot) {
        Remove-Item -LiteralPath $sessionRoot -Recurse -Force
    }
}
