param(
    [string] $Configuration = 'Release'
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
& "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
$dotnet = Get-DotNetExe -RepoRoot $repo
& $dotnet run --project (Join-Path $repo 'tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj') -c $Configuration --no-build
exit $LASTEXITCODE
