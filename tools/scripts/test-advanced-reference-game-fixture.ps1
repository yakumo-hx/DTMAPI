[CmdletBinding()]
param(
    [string] $TestRoot = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$repoTempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))
$fixtureBuilder = Join-Path $PSScriptRoot 'build-advanced-reference-game-fixture.ps1'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$resolvedTestRoot = if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    Join-Path $repoTempRoot ('advanced-reference-game-fixture-test-' + [Guid]::NewGuid().ToString('N'))
}
elseif ([System.IO.Path]::IsPathRooted($TestRoot)) {
    [System.IO.Path]::GetFullPath($TestRoot)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo $TestRoot))
}
if (-not (Test-DtmApiPathIsSameOrChild -Child $resolvedTestRoot -Parent $repoTempRoot) -or
    $resolvedTestRoot.TrimEnd([char[]]@('\', '/')) -eq $repoTempRoot.TrimEnd([char[]]@('\', '/'))) {
    throw "Advanced reference fixture test root must be a dedicated child of the repository temp root: $repoTempRoot"
}
if (Test-Path -LiteralPath $resolvedTestRoot) {
    throw "Advanced reference fixture test root already exists: $resolvedTestRoot"
}

function Assert-AdvancedFixtureTestTrue {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-AdvancedFixtureMatchesPolicy {
    param(
        [Parameter(Mandatory = $true)] [string] $PolicyId,
        [Parameter(Mandatory = $true)] [string] $FixtureRoot
    )

    $policyPath = Join-Path $repo ('author-sdk\advanced-reference-policies\' + $PolicyId + '.json')
    $policy = Get-Content -Raw -Encoding UTF8 -LiteralPath $policyPath | ConvertFrom-Json
    $gameRoot = Join-Path $FixtureRoot 'steamapps\common\Doloc Town'
    Assert-AdvancedFixtureTestTrue `
        -Condition (Test-Path -LiteralPath (Join-Path $gameRoot 'DolocTown.exe') -PathType Leaf) `
        -Message "$PolicyId fixture is missing its synthetic DolocTown.exe marker."
    $manifestPath = Join-Path $FixtureRoot 'steamapps\appmanifest_2285550.acf'
    $manifestText = [System.IO.File]::ReadAllText($manifestPath, [System.Text.Encoding]::UTF8)
    Assert-AdvancedFixtureTestTrue `
        -Condition ($manifestText -match ('"buildid"\s+"' + [regex]::Escape([string]$policy.gameBuildId) + '"')) `
        -Message "$PolicyId fixture manifest does not carry the exact policy build id."

    $expectedFiles = New-Object 'System.Collections.Generic.List[string]'
    $expectedFiles.Add('steamapps/appmanifest_2285550.acf') | Out-Null
    $expectedFiles.Add('steamapps/common/Doloc Town/DolocTown.exe') | Out-Null
    foreach ($reference in @($policy.references)) {
        $relativePath = ([string]$reference.gameRelativePath).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        $path = [System.IO.Path]::GetFullPath((Join-Path $gameRoot $relativePath))
        Assert-AdvancedFixtureTestTrue -Condition (Test-Path -LiteralPath $path -PathType Leaf) -Message "$PolicyId fixture reference is missing: $relativePath"
        $item = Get-Item -LiteralPath $path
        Assert-AdvancedFixtureTestTrue -Condition ([long]$item.Length -eq [long]$reference.length) -Message "$PolicyId fixture reference length drifted: $relativePath"
        $sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToUpperInvariant()
        Assert-AdvancedFixtureTestTrue -Condition ($sha256 -ceq ([string]$reference.sha256).ToUpperInvariant()) -Message "$PolicyId fixture reference hash drifted: $relativePath"
        $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($path).Name
        Assert-AdvancedFixtureTestTrue -Condition ([string]$assemblyName -ceq [string]$reference.assemblyName) -Message "$PolicyId fixture assembly identity drifted: $relativePath"
        $expectedFiles.Add(('steamapps/common/Doloc Town/' + ([string]$reference.gameRelativePath).Replace('\', '/'))) | Out-Null
    }

    $actualFiles = @(Get-ChildItem -LiteralPath $FixtureRoot -Recurse -Force -File | ForEach-Object {
        $_.FullName.Substring($FixtureRoot.TrimEnd([char[]]@('\', '/')).Length + 1).Replace('\', '/')
    } | Sort-Object)
    $expected = @($expectedFiles.ToArray() | Sort-Object)
    Assert-AdvancedFixtureTestTrue `
        -Condition ($actualFiles.Count -eq $expected.Count -and [string]::Join("`n", $actualFiles) -ceq [string]::Join("`n", $expected)) `
        -Message ("$PolicyId fixture contains an unexpected file set. Actual: " + [string]::Join(', ', $actualFiles))
}

function Assert-AdvancedFixtureFailure {
    param(
        [Parameter(Mandatory = $true)] [hashtable] $Parameters,
        [Parameter(Mandatory = $true)] [string] $ExpectedMessage,
        [Parameter(Mandatory = $true)] [string] $Label,
        [switch] $AllowExistingOutput
    )

    $message = ''
    try {
        & $fixtureBuilder @Parameters 2>&1 | Out-String | Out-Null
    }
    catch {
        $message = $_.Exception.Message
    }
    if ([string]::IsNullOrWhiteSpace($message)) {
        throw "$Label unexpectedly passed."
    }
    if ($message.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "$Label failed without the expected diagnostic '$ExpectedMessage': $message"
    }
    if (-not $AllowExistingOutput -and
        $Parameters.ContainsKey('OutputRoot') -and
        (Test-Path -LiteralPath ([string]$Parameters['OutputRoot']))) {
        throw "$Label left a rejected output root behind: $($Parameters['OutputRoot'])"
    }
}

try {
    [System.IO.Directory]::CreateDirectory($resolvedTestRoot) | Out-Null

    $oldFixture = Join-Path $resolvedTestRoot 'exact-23762374'
    & $fixtureBuilder -PolicyId 'doloctown-23762374-zoom-v1' -OutputRoot $oldFixture
    if (-not $?) { throw '23762374 Advanced reference fixture positive failed.' }
    Assert-AdvancedFixtureMatchesPolicy -PolicyId 'doloctown-23762374-zoom-v1' -FixtureRoot $oldFixture

    $currentFixture = Join-Path $resolvedTestRoot 'exact-24456188'
    & $fixtureBuilder -PolicyId 'doloctown-24456188-debugconsole-v1' -OutputRoot $currentFixture
    if (-not $?) { throw '24456188 Advanced reference fixture positive failed.' }
    Assert-AdvancedFixtureMatchesPolicy -PolicyId 'doloctown-24456188-debugconsole-v1' -FixtureRoot $currentFixture

    $emptyReverseRoot = Join-Path $resolvedTestRoot 'empty-reverse'
    [System.IO.Directory]::CreateDirectory($emptyReverseRoot) | Out-Null
    Assert-AdvancedFixtureFailure `
        -Parameters @{
            PolicyId = 'doloctown-23762374-zoom-v1'
            OutputRoot = (Join-Path $resolvedTestRoot 'missing-output')
            ReverseBuildsRoot = $emptyReverseRoot
        } `
        -ExpectedMessage 'found 0' `
        -Label 'Missing exact reverse Assembly-CSharp negative'

    $ambiguousReverseRoot = Join-Path $resolvedTestRoot 'ambiguous-reverse'
    $oldPolicy = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-zoom-v1.json') | ConvertFrom-Json
    $oldAssemblySource = Join-Path $oldFixture ('steamapps\common\Doloc Town\' + ([string]$oldPolicy.gameAssemblyRelativePath).Replace('/', '\'))
    foreach ($name in @('copy-a', 'copy-b')) {
        $destination = Join-Path $ambiguousReverseRoot ($name + '\raw-snapshot\game\' + ([string]$oldPolicy.gameAssemblyRelativePath).Replace('/', '\'))
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $destination)) | Out-Null
        [System.IO.File]::Copy($oldAssemblySource, $destination, $false)
    }
    Assert-AdvancedFixtureFailure `
        -Parameters @{
            PolicyId = 'doloctown-23762374-zoom-v1'
            OutputRoot = (Join-Path $resolvedTestRoot 'ambiguous-output')
            ReverseBuildsRoot = $ambiguousReverseRoot
        } `
        -ExpectedMessage 'found 2' `
        -Label 'Ambiguous exact reverse Assembly-CSharp negative'

    Assert-AdvancedFixtureFailure `
        -Parameters @{
            PolicyId = 'doloctown-23762374-zoom-v1'
            OutputRoot = (Join-Path $resolvedTestRoot 'missing-harmony-output')
            BepInExArchive = (Join-Path $resolvedTestRoot 'missing-bepinex.zip')
        } `
        -ExpectedMessage 'BepInEx archive is missing' `
        -Label 'Missing tracked Harmony archive negative'

    Assert-AdvancedFixtureFailure `
        -Parameters @{
            PolicyId = '../doloctown-23762374-zoom-v1'
            OutputRoot = (Join-Path $resolvedTestRoot 'unsafe-policy-output')
        } `
        -ExpectedMessage 'policy id is unsafe' `
        -Label 'Unsafe policy-id negative'

    Assert-AdvancedFixtureFailure `
        -Parameters @{
            PolicyId = 'doloctown-23762374-zoom-v1'
            OutputRoot = $oldFixture
        } `
        -ExpectedMessage 'output already exists' `
        -Label 'Existing fixture output negative' `
        -AllowExistingOutput

    Write-Host 'Advanced reference game fixture matrix: PASS (23762374 + 24456188 exact; missing + ambiguous + archive + path + existing-output negatives)'
}
finally {
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
