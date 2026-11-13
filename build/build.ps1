param(
    [string] $Configuration = 'Release',
    [switch] $SkipTests,
    [switch] $Rebuild,
    [string[]] $Projects
)

. "$PSScriptRoot\windows-installer/common.ps1"
. "$PSScriptRoot\build-common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$selectedBuild = $PSBoundParameters.ContainsKey('Projects')
if ($selectedBuild -and (-not $SkipTests -or @($Projects).Count -eq 0 -or @($Projects | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0)) {
    throw '-Projects requires a non-empty selection and -SkipTests. Run the relevant test entry separately.'
}
if (-not $SkipTests) { Assert-DtmApiFullTestEnvironment }
$dotnet = Get-DotNetExe -RepoRoot $repo

# Keep ordinary project membership in the solution, alongside its dependency graph.
# One MSBuild invocation reuses common projects. Keep serial nodes while fixture
# targets share project output directories.
if ($selectedBuild) {
    Invoke-DtmApiProjectBuild -RepoRoot $repo -DotNetExe $dotnet -Projects $Projects -Configuration $Configuration -Rebuild:$Rebuild
}
else {
    & "$PSScriptRoot/prepare-unit-test-dependencies.ps1" -Dependency BepInExFixture
    if ($LASTEXITCODE -ne 0) { throw 'Unit fixture dependency preparation failed.' }
    $buildTarget = if ($Rebuild) { 'Rebuild' } else { 'Build' }
    & $dotnet build (Join-Path $repo 'DTMAPI.sln') -c $Configuration --nologo -m:1 "-t:$buildTarget"
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed: DTMAPI.sln' }
}

if (-not $SkipTests) {
    & "$PSScriptRoot\test-unit.ps1" -Configuration $Configuration -NoBuild
    if ($LASTEXITCODE -ne 0) { throw 'Unit source suites failed.' }
    foreach ($testProject in @(
        'tests\DTMAPI.MultiPlatformInstaller.Tests\DTMAPI.MultiPlatformInstaller.Tests.csproj'
    )) {
        & $dotnet run --project (Join-Path $repo $testProject) -c $Configuration --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed: $testProject"
        }
    }
}
