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
    'src\DTMAPI.BepInExBootstrap\DTMAPI.BepInExBootstrap.csproj',
    'testmods\HelloDtmMod\HelloDtmMod.csproj',
    'testmods\ConfigMenuExample\ConfigMenuExample.csproj',
    'testmods\ActionSpeedMod\ActionSpeedMod.csproj',
    'testmods\DebugConsoleMod\DebugConsoleMod.csproj',
    'testmods\OneActionCompleteMod\OneActionCompleteMod.csproj',
    'testmods\AutoFishingMod\AutoFishingMod.csproj',
    'testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj',
    'testmods\AnimalHusbandryProgressMod\AnimalHusbandryProgressMod.csproj',
    'testmods\SecondMotorMod\SecondMotorMod.csproj',
    'testmods\OilMod\OilMod.csproj',
    'testmods\MineMod\MineMod.csproj',
    'testmods\MoreEquipmentSlotsMod\MoreEquipmentSlotsMod.csproj',
    'testmods\MoreSavesMod\MoreSavesMod.csproj',
    'testmods\ZoomMod\ZoomMod.csproj',
    'testmods\ChestLocatorEnhancerMod\ChestLocatorEnhancerMod.csproj',
    'testmods\StrongPlantingGunMod\StrongPlantingGunMod.csproj',
    'testmods\AutoHarvestMod\AutoHarvestMod.csproj',
    'testmods\HookProbeMod\HookProbeMod.csproj',
    'tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj'
)

foreach ($project in $projects) {
    & $dotnet build (Join-Path $repo $project) -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $project"
    }
}

if (-not $SkipTests) {
    & $dotnet run --project (Join-Path $repo 'tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj') -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Unit tests failed."
    }
}
