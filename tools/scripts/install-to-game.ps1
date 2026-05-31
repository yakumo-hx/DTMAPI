param(
    [string] $Configuration = 'Release',
    [switch] $IncludeTestMods,
    [switch] $IncludeHookProbe,
    [switch] $SkipBuild,
    [switch] $InstallBepInEx,
    [switch] $SkipOfficialLocalMods,
    [switch] $KeepLegacyMigratedGameMods
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if (-not $SkipBuild) {
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$outDir = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
$pluginDir = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

$runtimeFiles = @(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)
Copy-DirectoryContents -Source $outDir -Destination $pluginDir -Include $runtimeFiles

$bepInExCore = Join-Path $gameDir 'BepInEx\core\BepInEx.dll'
if (-not (Test-Path $bepInExCore)) {
    if ($InstallBepInEx) {
        & "$PSScriptRoot\install-bepinex.ps1"
    }
    else {
        Write-Warning "BepInEx core was not found at $bepInExCore. DTMAPI Bootstrap is installed, but the game still needs BepInEx installed once."
    }
}

function Backup-GameModDirectory {
    param(
        [Parameter(Mandatory = $true)] [string] $ModsRoot,
        [Parameter(Mandatory = $true)] [string] $ModId,
        [Parameter(Mandatory = $true)] [string] $BackupRoot,
        [Parameter(Mandatory = $true)] [string] $Reason
    )

    $source = Join-Path $ModsRoot $ModId
    if (-not (Test-Path $source -PathType Container)) {
        return
    }

    $resolvedModsRoot = [System.IO.Path]::GetFullPath($ModsRoot).TrimEnd('\') + '\'
    $resolvedSource = [System.IO.Path]::GetFullPath($source)
    if (-not $resolvedSource.StartsWith($resolvedModsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to move $ModId because the resolved source is outside ModsRoot."
    }

    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
    $dest = Join-Path $BackupRoot $ModId
    Move-Item -LiteralPath $source -Destination $dest
    Write-Host "Moved $ModId out of game Mods to $dest ($Reason)"
}

function Backup-LegacyMigratedGameMods {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [switch] $KeepLegacyMigratedGameMods
    )

    if ($KeepLegacyMigratedGameMods) {
        return
    }

    $modsRoot = Join-Path $GameDir 'Mods'
    $legacyIds = @(
        'Yuuka.DTMAPI.ActionSpeed',
        'Yuuka.DTMAPI.AutoFishing',
        'Yuuka.DTMAPI.OneActionComplete',
        'Yuuka.DTMAPI.FishBreedingAssistant',
        'Yuuka.DTMAPI.AnimalHusbandryProgress'
    )
    $existing = @($legacyIds | Where-Object { Test-Path (Join-Path $modsRoot $_) -PathType Container })
    if ($existing.Count -eq 0) {
        return
    }

    $backupRoot = Join-Path $GameDir ('DTMAPI\backups\official-local-migration-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    foreach ($id in $existing) {
        Backup-GameModDirectory -ModsRoot $modsRoot -ModId $id -BackupRoot $backupRoot -Reason 'official local package now owns enablement'
    }
}

function Backup-StaleHookProbe {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [switch] $IncludeHookProbe
    )

    if ($IncludeHookProbe) {
        return
    }

    $modsRoot = Join-Path $GameDir 'Mods'
    $backupRoot = Join-Path $GameDir ('DTMAPI\backups\disabled-testmods-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Backup-GameModDirectory -ModsRoot $modsRoot -ModId 'DTMAPI.HookProbeMod' -BackupRoot $backupRoot -Reason 'HookProbe is test-only and was not requested'
}

function Backup-StaleSampleMods {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [switch] $IncludeTestMods
    )

    if ($IncludeTestMods) {
        return
    }

    $modsRoot = Join-Path $GameDir 'Mods'
    $backupRoot = Join-Path $GameDir ('DTMAPI\backups\disabled-testmods-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    foreach ($id in @('DTMAPI.HelloDtmMod', 'DTMAPI.ConfigMenuExample')) {
        Backup-GameModDirectory -ModsRoot $modsRoot -ModId $id -BackupRoot $backupRoot -Reason 'sample mod is dev-only and was not requested'
    }
}

if ($IncludeTestMods) {
    $modsRoot = Join-Path $gameDir 'Mods'
    $testMods = @(
        @{ Id = 'DTMAPI.HelloDtmMod'; Project = 'HelloDtmMod'; Dll = 'HelloDtmMod.dll' },
        @{ Id = 'DTMAPI.ConfigMenuExample'; Project = 'ConfigMenuExample'; Dll = 'ConfigMenuExample.dll' }
    )
    if ($IncludeHookProbe) {
        $testMods += @{ Id = 'DTMAPI.HookProbeMod'; Project = 'HookProbeMod'; Dll = 'HookProbeMod.dll' }
    }
    foreach ($mod in $testMods) {
        $source = Join-Path $repo "testmods\$($mod.Project)\bin\$Configuration\netstandard2.0"
        $dest = Join-Path $modsRoot $mod.Id
        Copy-DirectoryContents -Source $source -Destination $dest -Include @($mod.Dll, 'manifest.json', 'i18n')
    }
}

Backup-LegacyMigratedGameMods -GameDir $gameDir -KeepLegacyMigratedGameMods:$KeepLegacyMigratedGameMods
Backup-StaleSampleMods -GameDir $gameDir -IncludeTestMods:$IncludeTestMods
Backup-StaleHookProbe -GameDir $gameDir -IncludeHookProbe:$IncludeHookProbe

function Get-DolocTownPersistentRoot {
    if ($env:DTMAPI_DOLOC_PERSISTENT_ROOT) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_DOLOC_PERSISTENT_ROOT)
    }

    return Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\RedSawGames\DolocTown'
}

function Write-JsonObject {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $json = $Value | ConvertTo-Json -Depth 10
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Install-OfficialLocalDtmApiMod {
    param(
        [hashtable] $Mod
    )

    $persistentRoot = Get-DolocTownPersistentRoot
    $officialModsRoot = Join-Path $persistentRoot 'MODS'
    New-Item -ItemType Directory -Force -Path $officialModsRoot | Out-Null

    $dest = Join-Path $officialModsRoot $Mod.OfficialFolder
    $marker = Join-Path $dest 'Content\DTMAPI\dtmapi-package.json'
    if ((Test-Path $dest) -and -not (Test-Path $marker)) {
        Write-Warning "Skipping $($Mod.OfficialFolder): destination exists but is not marked as a DTMAPI-owned package. Existing user content was left untouched."
        return
    }

    $source = Join-Path $repo "testmods\$($Mod.Project)\bin\$Configuration\netstandard2.0"
    $sourceManifestPath = Join-Path $source 'manifest.json'
    $sourceDllPath = Join-Path $source $Mod.SourceDll
    if (-not (Test-Path $sourceManifestPath) -or -not (Test-Path $sourceDllPath)) {
        Write-Warning "Skipping $($Mod.OfficialFolder): built mod output was not found at $source."
        return
    }

    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    $contentRoot = Join-Path $dest 'Content\DTMAPI'
    New-Item -ItemType Directory -Force -Path $contentRoot | Out-Null

    $manifest = Get-Content -Raw -LiteralPath $sourceManifestPath | ConvertFrom-Json
    $manifest.EntryDll = "Content/DTMAPI/$($Mod.PackageDll)"
    $manifest.MinimumDTMApiVersion = '0.1.13'
    if ($manifest.Dependencies) {
        foreach ($dependency in $manifest.Dependencies) {
            if ($dependency.UniqueID -eq 'DTMAPI.ModConfigMenu') {
                $dependency.MinimumVersion = '0.1.13'
            }
        }
    }

    Copy-Item -Force -LiteralPath $sourceDllPath -Destination (Join-Path $contentRoot $Mod.PackageDll)
    Write-JsonObject -Path (Join-Path $contentRoot 'manifest.json') -Value $manifest

    $i18nSource = Join-Path $repo "testmods\$($Mod.Project)\i18n"
    if (Test-Path $i18nSource) {
        Copy-DirectoryContents -Source (Split-Path -Parent $i18nSource) -Destination $dest -Include @('i18n')
    }

    $assetIcon = Join-Path $repo 'assets\branding\dtmapi-icon.png'
    $assetPreview = Join-Path $repo 'assets\branding\dtmapi-preview.png'
    if (Test-Path $assetIcon) {
        Copy-Item -Force -LiteralPath $assetIcon -Destination (Join-Path $dest 'icon.png')
    }
    if (Test-Path $assetPreview) {
        Copy-Item -Force -LiteralPath $assetPreview -Destination (Join-Path $dest 'preview.png')
    }

    $officialInfoPath = Join-Path $repo "testmods\$($Mod.Project)\official-info.json"
    if (-not (Test-Path $officialInfoPath)) {
        Write-Warning "Skipping $($Mod.OfficialFolder): official-info.json was not found."
        return
    }

    $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $officialInfoPath | ConvertFrom-Json
    $info.version = $manifest.Version
    Write-JsonObject -Path (Join-Path $dest 'info.json') -Value $info

    $packageInfo = [ordered]@{
        owner = 'DTMAPI'
        uniqueId = $manifest.UniqueID
        generatedBy = 'tools/scripts/install-to-game.ps1'
        updatedAt = (Get-Date).ToString('o')
    }
    Write-JsonObject -Path $marker -Value $packageInfo
    Write-Host "Installed official local DTMAPI mod package to $dest"
}

if (-not $SkipOfficialLocalMods) {
    $officialLocalMods = @(
        @{
            OfficialFolder = 'Yuuka_DTMAPI_ActionSpeed'
            Project = 'ActionSpeedMod'
            SourceDll = 'ActionSpeedMod.dll'
            PackageDll = 'Yuuka.DTMAPI.ActionSpeed.dll'
        },
        @{
            OfficialFolder = 'Yuuka_DTMAPI_AutoFishing'
            Project = 'AutoFishingMod'
            SourceDll = 'AutoFishingMod.dll'
            PackageDll = 'Yuuka.DTMAPI.AutoFishing.dll'
        },
        @{
            OfficialFolder = 'Yuuka_DTMAPI_OneActionComplete'
            Project = 'OneActionCompleteMod'
            SourceDll = 'OneActionCompleteMod.dll'
            PackageDll = 'Yuuka.DTMAPI.OneActionComplete.dll'
        },
        @{
            OfficialFolder = 'Yuuka_DTMAPI_FishBreedingAssistant'
            Project = 'FishBreedingAssistantMod'
            SourceDll = 'FishBreedingAssistantMod.dll'
            PackageDll = 'Yuuka.DTMAPI.FishBreedingAssistant.dll'
        },
        @{
            OfficialFolder = 'Yuuka_DTMAPI_AnimalHusbandryProgress'
            Project = 'AnimalHusbandryProgressMod'
            SourceDll = 'AnimalHusbandryProgressMod.dll'
            PackageDll = 'Yuuka.DTMAPI.AnimalHusbandryProgress.dll'
        }
    )

    foreach ($mod in $officialLocalMods) {
        Install-OfficialLocalDtmApiMod -Mod $mod
    }
}

$assetSource = Join-Path $repo 'assets\branding\dtmapi-icon.png'
if (Test-Path $assetSource) {
    $assetDest = Join-Path $pluginDir 'assets\branding'
    New-Item -ItemType Directory -Force -Path $assetDest | Out-Null
    Copy-Item -LiteralPath $assetSource -Destination (Join-Path $assetDest 'dtmapi-icon.png') -Force
}

Write-Host "Installed DTMAPI to $pluginDir"
