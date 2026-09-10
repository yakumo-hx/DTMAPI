$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
. "$PSScriptRoot/author-sdk-preparation.ps1"
. "$PSScriptRoot/workspace-preflight.ps1"
$repo = Get-RepoRoot
$root = Join-Path $repo ('temp/preflight-' + [Guid]::NewGuid().ToString('N'))
$checks = 0
function Assert-Preflight { param([bool] $Condition, [string] $Message); if (-not $Condition) { throw $Message }; $script:checks++ }
function Write-Fixture { param([string] $Relative, [string] $Text); $path=Join-Path $root $Relative; New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null; [IO.File]::WriteAllText($path,$Text,[Text.Encoding]::UTF8) }
function Get-FixtureIdentity { return (@(Get-ChildItem -LiteralPath $root -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName + ' ' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }) -join "`n") }
try {
    Write-Fixture 'tools/release/dtmapi-product-catalog.json' '{"products":[{"catalogId":"fixture","uniqueId":"Test.Fixture","sourceVersion":"1.0.0","sourceRoot":"products/fixture","productionBuildAuthority":"DTMAPI Author SDK build/pack/deploy","referencePolicyId":"fixture"}]}'
    Write-Fixture 'products/fixture/dtmapi.author.json' '{"targetDtmApiVersion":"0.5.5"}'
    Write-Fixture 'reference/DolocTown_Data/Managed/Assembly-CSharp.dll' 'reference bytes'
    $hash = (Get-FileHash -LiteralPath (Join-Path $root 'reference/DolocTown_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash
    Write-Fixture 'author-sdk/advanced-reference-policies/fixture.json' ('{"gameBuildId":"fixture-baseline","gameAssemblyRelativePath":"DolocTown_Data/Managed/Assembly-CSharp.dll","gameAssemblySha256":"' + $hash + '"}')
    Write-Fixture 'test-game/DolocTown.exe' ''
    Write-Fixture 'test-game/UnityPlayer.dll' ''
    Write-Fixture 'test-game/DolocTown_Data/Managed/Assembly-CSharp.dll' 'different installed game bytes'
    $before = Get-FixtureIdentity
    function Test-DtmApiDotNet8Toolchain { param([string] $Path); return $false }
    $rejected=$false
    try { Get-DotNetExe -RepoRoot $root -NoProvision | Out-Null } catch { $rejected=$true }
    Assert-Preflight $rejected 'Read-only .NET resolution did not refuse missing toolchain.'
    Assert-Preflight ((Get-FixtureIdentity) -ceq $before) 'Read-only .NET resolution created files.'
    function Get-DotNetExe { param([string] $RepoRoot, [switch] $NoProvision); if (-not $NoProvision) { throw 'Preflight attempted provisioning.' }; return (Join-Path $root '.tools/dotnet/dotnet.exe') }
    function Get-DtmApiAuthorSdkInput { param($RepoRoot,$DotNetExe); return @{sha256='fixture'} }
    function Test-DtmApiPreparedAuthorSdk { param($OutputRoot,$InputSnapshot); return $script:sdkReady }
    $script:sdkReady=$true
    $args=@{RepoRoot=$root;Operation='BuildProduct';CatalogId='fixture';ReferenceGameDir=(Join-Path $root 'reference');GameDir=(Join-Path $root 'test-game')}
    $p=Get-DtmApiWorkspacePreflight @args
    Assert-Preflight $p.ready 'Valid explicit build prerequisites were refused.'
    Assert-Preflight ($p.compileReference.gameDir -cne $p.testGame -and $p.compileReference.actualAssemblySha256 -ceq $hash) 'Compile and runtime game identities were conflated.'
    $args.ReferenceGameDir=$args.GameDir
    $p=Get-DtmApiWorkspacePreflight @args
    Assert-Preflight (-not $p.ready -and ($p.problems -join ' ') -match 'Compile reference must match') 'Wrong installed game was accepted as a compile reference.'
    $args.ReferenceGameDir=Join-Path $root 'reference'
    $script:sdkReady=$false
    $p=Get-DtmApiWorkspacePreflight @args
    Assert-Preflight (-not $p.ready -and ($p.problems -join ' ') -match 'SDK preparation') 'Stale SDK preparation was accepted.'
    $script:sdkReady=$true
    $p=Get-DtmApiWorkspacePreflight @args -OutputRoot (Join-Path $root 'products')
    Assert-Preflight (-not $p.ready -and ($p.problems -join ' ') -match 'overlaps') 'Output overlapping source was accepted.'
    $args.Operation='TestProduct'
    $args.TestMode='Unit'
    $args.GameDir=''
    $p=Get-DtmApiWorkspacePreflight @args
    Assert-Preflight ($p.ready -and $null -eq $p.sdkRoot -and $null -eq $p.compileReference) 'Unit test preflight loaded unrelated SDK/game prerequisites.'
    $args.TestMode='Game'
    $p=Get-DtmApiWorkspacePreflight @args
    Assert-Preflight (-not $p.ready) 'Game preflight accepted an unspecified game.'
    $args.CatalogId='unknown'
    $p=Get-DtmApiWorkspacePreflight @args
    Assert-Preflight (-not $p.ready -and ($p.problems -join ' ') -match 'exact CatalogId') 'Unknown Catalog id was accepted.'
    Assert-Preflight ((Get-FixtureIdentity) -ceq $before) 'Preflight changed input files or created build output.'
    Write-Host "Workspace preflight: PASS ($checks checks; no build, provision or game process)."
}
finally {
    $resolved=[IO.Path]::GetFullPath($root)
    $prefix=[IO.Path]::GetFullPath((Join-Path $repo 'temp')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Refusing preflight fixture cleanup outside temp.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
