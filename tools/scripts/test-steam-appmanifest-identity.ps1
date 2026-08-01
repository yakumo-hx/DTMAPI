[CmdletBinding()]
param(
    [string] $TestRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\steam-appmanifest-identity.ps1"

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
$sessionRoot = Join-Path $sessionBase ("steam-appmanifest-identity-" + [Guid]::NewGuid().ToString('N'))

function Assert-Branch {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ManifestPath,
        [Parameter(Mandatory = $true)]
        [string] $ExpectedBranch
    )

    $identity = Get-DtmApiSteamBranchIdentity -ManifestPath $ManifestPath
    if ($identity.ResolvedBranch -cne $ExpectedBranch) {
        throw "Expected branch '$ExpectedBranch', found '$($identity.ResolvedBranch)' for $ManifestPath"
    }
    if ($identity.Source -cne 'appmanifest.UserConfig+MountedConfig') {
        throw "Unexpected branch evidence source: $($identity.Source)"
    }
}

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

    throw "Expected Steam manifest identity rejection: $Label"
}

function Write-TestManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [AllowNull()]
        [string] $UserBranch,
        [AllowNull()]
        [string] $MountedBranch,
        [string] $BuildId = '1',
        [string] $ExtraValue = ''
    )

    $userLine = if ($null -eq $UserBranch) { '' } else { "`t`t`"BetaKey`"`t`t`"$UserBranch`"" }
    $mountedLine = if ($null -eq $MountedBranch) { '' } else { "`t`t`"BetaKey`"`t`t`"$MountedBranch`"" }
    $text = @"
"AppState"
{
    "buildid" "$BuildId"
    "StateFlags" "$ExtraValue"
    "UserConfig"
    {
$userLine
    }
    "MountedConfig"
    {
$mountedLine
    }
    "PrivateDepots"
    {
        "branches"
        {
            "unrelated"
            {
                "BetaKey" "must-not-be-read"
            }
        }
    }
}
"@
    $path = Join-Path $sessionRoot "$Name.acf"
    [System.IO.File]::WriteAllText($path, $text, [System.Text.UTF8Encoding]::new($false))
    return $path
}

try {
    [System.IO.Directory]::CreateDirectory($sessionRoot) | Out-Null

    $syntheticPublicManifest = Write-TestManifest -Name 'public' -UserBranch 'public' -MountedBranch 'public'
    $syntheticTestManifest = Write-TestManifest -Name 'test' -UserBranch 'test' -MountedBranch 'test'
    Assert-Branch -ManifestPath $syntheticPublicManifest -ExpectedBranch 'public'
    Assert-Branch -ManifestPath $syntheticTestManifest -ExpectedBranch 'test'

    $localManifestExpectations = @(
        [ordered]@{
            path = Join-Path $repo 'references\doloc-town\reverse\builds\23762374_public_C416D4\raw-snapshot\appmanifest_2285550.acf'
            branch = 'public'
        },
        [ordered]@{
            path = Join-Path $repo 'references\doloc-town\reverse\builds\24256979_test_7A1907\raw-snapshot\appmanifest_2285550.acf'
            branch = 'test'
        }
    )
    foreach ($expectation in $localManifestExpectations) {
        if (Test-Path -LiteralPath $expectation.path -PathType Leaf) {
            Assert-Branch -ManifestPath $expectation.path -ExpectedBranch $expectation.branch
        }
    }

    $knownManifest = Write-TestManifest -Name 'known' -UserBranch 'test' -MountedBranch 'TEST'
    $knownIdentity = Get-DtmApiSteamBranchIdentity -ManifestPath $knownManifest -ExplicitBranch 'Test'
    if ($knownIdentity.ResolvedBranch -cne 'test' -or -not $knownIdentity.IsKnownBranch) {
        throw 'Known branch normalization or allowlist classification failed.'
    }

    $unknownManifest = Write-TestManifest -Name 'unknown' -UserBranch 'future_branch' -MountedBranch 'future_branch'
    Assert-Rejected -Label 'unknown branch without override' -Action {
        Get-DtmApiSteamBranchIdentity -ManifestPath $unknownManifest
    }
    Assert-Rejected -Label 'unknown branch without explicit match' -Action {
        Get-DtmApiSteamBranchIdentity -ManifestPath $unknownManifest -AllowUnknownSteamBranch
    }
    $unknownIdentity = Get-DtmApiSteamBranchIdentity `
        -ManifestPath $unknownManifest `
        -ExplicitBranch 'future_branch' `
        -AllowUnknownSteamBranch
    if ($unknownIdentity.ResolvedBranch -cne 'future_branch' -or $unknownIdentity.IsKnownBranch) {
        throw 'Explicit unknown branch opt-in failed.'
    }

    $conflictManifest = Write-TestManifest -Name 'conflict' -UserBranch 'public' -MountedBranch 'test'
    Assert-Rejected -Label 'UserConfig/MountedConfig conflict' -Action {
        Get-DtmApiSteamBranchIdentity -ManifestPath $conflictManifest
    }
    $missingManifest = Write-TestManifest -Name 'missing' -UserBranch 'public' -MountedBranch $null
    Assert-Rejected -Label 'missing MountedConfig BetaKey' -Action {
        Get-DtmApiSteamBranchIdentity -ManifestPath $missingManifest
    }
    $unsafeManifest = Write-TestManifest -Name 'unsafe' -UserBranch '..\escape' -MountedBranch '..\escape'
    Assert-Rejected -Label 'unsafe branch' -Action {
        Get-DtmApiSteamBranchIdentity -ManifestPath $unsafeManifest -ExplicitBranch '..\escape' -AllowUnknownSteamBranch
    }
    Assert-Rejected -Label 'explicit branch mismatch' -Action {
        Get-DtmApiSteamBranchIdentity -ManifestPath $knownManifest -ExplicitBranch 'public'
    }

    $frozenIdentityManifest = Write-TestManifest -Name 'frozen-a' -UserBranch 'test' -MountedBranch 'test' -BuildId '24256979' -ExtraValue '4'
    $matchingIdentityManifest = Write-TestManifest -Name 'frozen-b' -UserBranch 'test' -MountedBranch 'test' -BuildId '24256979' -ExtraValue '4'
    $changedBuildManifest = Write-TestManifest -Name 'changed-build' -UserBranch 'test' -MountedBranch 'test' -BuildId '24256980' -ExtraValue '4'
    $changedBytesManifest = Write-TestManifest -Name 'changed-bytes' -UserBranch 'test' -MountedBranch 'test' -BuildId '24256979' -ExtraValue '5'
    $frozenIdentity = Get-DtmApiSteamBuildIdentity -ManifestPath $frozenIdentityManifest
    Assert-DtmApiSteamBuildIdentityMatch `
        -Expected $frozenIdentity `
        -Actual (Get-DtmApiSteamBuildIdentity -ManifestPath $matchingIdentityManifest) `
        -ExpectedLabel 'start' `
        -ActualLabel 'frozen'
    Assert-Rejected -Label 'build identity changed' -Action {
        Assert-DtmApiSteamBuildIdentityMatch `
            -Expected $frozenIdentity `
            -Actual (Get-DtmApiSteamBuildIdentity -ManifestPath $changedBuildManifest)
    }
    Assert-Rejected -Label 'manifest bytes changed with same build and branch' -Action {
        Assert-DtmApiSteamBuildIdentityMatch `
            -Expected $frozenIdentity `
            -Actual (Get-DtmApiSteamBuildIdentity -ManifestPath $changedBytesManifest)
    }

    Write-Host 'STEAM APPMANIFEST IDENTITY TEST: OK'
}
finally {
    if (Test-Path -LiteralPath $sessionRoot) {
        Remove-Item -LiteralPath $sessionRoot -Recurse -Force
    }
}
