param(
    [string] $Configuration = 'Release',
    [switch] $SkipTests,
    [switch] $Rebuild
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\test-common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if (-not $SkipTests) { Assert-DtmApiFullTestEnvironment }
$dotnet = Get-DotNetExe -RepoRoot $repo

# Keep ordinary project membership in the solution, alongside its dependency graph.
# One MSBuild invocation reuses common projects. Keep serial nodes while fixture
# targets share project output directories.
$buildTarget = if ($Rebuild) { 'Rebuild' } else { 'Build' }
& $dotnet build (Join-Path $repo 'DTMAPI.sln') -c $Configuration --nologo -m:1 "-t:$buildTarget"
if ($LASTEXITCODE -ne 0) {
    throw 'Solution build failed: DTMAPI.sln'
}

if (-not $SkipTests) {
    & "$PSScriptRoot\test-unit.ps1" -Configuration $Configuration -NoBuild
    if ($LASTEXITCODE -ne 0) { throw 'Unit source suites failed.' }
    foreach ($testProject in @(
        'tests\DTMAPI.QaUnitTests\DTMAPI.QaUnitTests.csproj',
        'tests\DTMAPI.InstallDoctor.Tests\DTMAPI.InstallDoctor.Tests.csproj',
        'tests\DTMAPI.MultiPlatformInstaller.Tests\DTMAPI.MultiPlatformInstaller.Tests.csproj',
        'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj'
    )) {
        if ($testProject -eq 'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj') {
            & "$PSScriptRoot\prepare-author-sdk-compatibility.ps1"
            if (-not $?) { throw 'Frozen Author SDK compatibility preparation failed.' }
        }
        & $dotnet run --project (Join-Path $repo $testProject) -c $Configuration --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed: $testProject"
        }
    }
}
