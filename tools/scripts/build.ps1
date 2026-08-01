param(
    [string] $Configuration = 'Release',
    [switch] $SkipTests
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo

$projects = @(
    'src\DTMAPI.BepInExStubs\DTMAPI.BepInExStubs.csproj',
    'src\DTMAPI.Abstractions\DTMAPI.Abstractions.csproj',
    'src\DTMAPI.Core\DTMAPI.Core.csproj',
    'src\DTMAPI.ModConfigMenu\DTMAPI.ModConfigMenu.csproj',
    'src\DTMAPI.GameBridge.DolocTown\DTMAPI.GameBridge.DolocTown.csproj',
    'src\DTMAPI.GameBridge.DolocTown.QA\DTMAPI.GameBridge.DolocTown.QA.csproj',
    'src\DTMAPI.BepInExBootstrap\DTMAPI.BepInExBootstrap.csproj',
    'src\DTMAPI.Authoring.Contracts\DTMAPI.Authoring.Contracts.csproj',
    'src\DTMAPI.Tooling.Metadata\DTMAPI.Tooling.Metadata.csproj',
    'src\DTMAPI.InstallDoctor\DTMAPI.InstallDoctor.csproj',
    'src\DTMAPI.PlayerDoctor\DTMAPI.PlayerDoctor.csproj',
    'src\DTMAPI.AuthorSdk\DTMAPI.AuthorSdk.csproj',
    'author-sdk\examples\HelloDtm\HelloDtmMod.csproj',
    'author-sdk\examples\ConfigMenu\ConfigMenuExample.csproj',
    'author-sdk\samples\api-demand\AutoHarvest\AutoHarvestMod.csproj',
    'tests\mod-fixtures\qa\CropHarvesting\CropHarvestingQaMod.csproj',
    'tests\mod-fixtures\qa\HookProbe\HookProbeMod.csproj',
    'products\first-party\ManboCardboardAudio\ManboCardboardAudioMod.csproj',
    'tests\DTMAPI.AbiCompatibilityHarness\DTMAPI.AbiCompatibilityHarness.csproj',
    'tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj',
    'tests\DTMAPI.QaUnitTests\DTMAPI.QaUnitTests.csproj',
    'tests\DTMAPI.InstallDoctor.Tests\DTMAPI.InstallDoctor.Tests.csproj',
    'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj'
)

foreach ($project in $projects) {
    & $dotnet build (Join-Path $repo $project) -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $project"
    }
}

# Author SDK 0.1 compiles against the frozen Release compatibility assembly even
# when the host/test configuration is Debug. Keep that tracked contract available
# without changing the ordinary Author SDK build into a RID/self-contained build.
if ($Configuration -ne 'Release') {
    $compatibilityProject = Join-Path $repo 'src\DTMAPI.Abstractions\DTMAPI.Abstractions.csproj'
    & $dotnet build $compatibilityProject -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to build the fixed Release DTMAPI.Abstractions compatibility assembly.'
    }
}

if (-not $SkipTests) {
    foreach ($testProject in @(
        'tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj',
        'tests\DTMAPI.QaUnitTests\DTMAPI.QaUnitTests.csproj',
        'tests\DTMAPI.InstallDoctor.Tests\DTMAPI.InstallDoctor.Tests.csproj',
        'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj'
    )) {
        & $dotnet run --project (Join-Path $repo $testProject) -c $Configuration --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed: $testProject"
        }
    }
}
