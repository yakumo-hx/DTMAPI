param(
    [string] $Configuration = 'Release',
    [switch] $IncludeTestMods,
    [switch] $IncludeDebugConsoleMod,
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
        [switch] $IncludeTestMods,
        [switch] $IncludeDebugConsoleMod
    )

    if ($IncludeTestMods) {
        return
    }

    $modsRoot = Join-Path $GameDir 'Mods'
    $backupRoot = Join-Path $GameDir ('DTMAPI\backups\disabled-testmods-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    foreach ($id in @('DTMAPI.HelloDtmMod', 'DTMAPI.ConfigMenuExample', 'DTMAPI.DebugConsoleMod')) {
        if ($id -eq 'DTMAPI.DebugConsoleMod' -and $IncludeDebugConsoleMod) {
            continue
        }
        Backup-GameModDirectory -ModsRoot $modsRoot -ModId $id -BackupRoot $backupRoot -Reason 'sample mod is dev-only and was not requested'
    }
}

if ($IncludeTestMods -or $IncludeDebugConsoleMod) {
    $modsRoot = Join-Path $gameDir 'Mods'
    $testMods = @()
    if ($IncludeTestMods) {
        $testMods += @{ Id = 'DTMAPI.HelloDtmMod'; Project = 'HelloDtmMod'; Dll = 'HelloDtmMod.dll' }
        $testMods += @{ Id = 'DTMAPI.ConfigMenuExample'; Project = 'ConfigMenuExample'; Dll = 'ConfigMenuExample.dll' }
    }
    if ($IncludeDebugConsoleMod) {
        $testMods += @{ Id = 'DTMAPI.DebugConsoleMod'; Project = 'DebugConsoleMod'; Dll = 'DebugConsoleMod.dll' }
    }
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
Backup-StaleSampleMods -GameDir $gameDir -IncludeTestMods:$IncludeTestMods -IncludeDebugConsoleMod:$IncludeDebugConsoleMod
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

function Find-OfficialVehicleExampleAssetRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    $candidates = @()
    if ($env:DTMAPI_OFFICIAL_VEHICLE_ASSET_ROOT) {
        $candidates += $env:DTMAPI_OFFICIAL_VEHICLE_ASSET_ROOT
    }

    $gameSteamApps = [System.IO.Path]::GetFullPath((Join-Path $GameDir '..\..'))
    $candidates += (Join-Path $gameSteamApps 'workshop\content\2285550\3705665433\Content')
    $candidates += 'D:\Steam\steamapps\workshop\content\2285550\3705665433\Content'
    $candidates += 'D:\steam\steamapps\workshop\content\2285550\3705665433\Content'

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (-not (Test-Path $candidate)) {
            continue
        }
        $match = Get-ChildItem -Path $candidate -Recurse -File -Filter 'sprite_vehicle_motor.png' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($match) {
            return $match.Directory.FullName
        }
    }

    return $null
}

function Install-OfficialVehicleExampleAssets {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    $destRoot = Join-Path $Destination 'Content\DTMAPI\official-vehicle-example'
    if (Test-Path $destRoot) {
        Remove-Item -LiteralPath $destRoot -Recurse -Force
    }

    $noteRoot = Join-Path $Destination 'Content\DTMAPI\vehicle-appearance'
    New-Item -ItemType Directory -Force -Path $noteRoot | Out-Null
    $notePath = Join-Path $noteRoot 'official-vehicle-assets.txt'
    $sourceRoot = Find-OfficialVehicleExampleAssetRoot -GameDir $GameDir
    $sourceLine = if ($sourceRoot) { "Official Workshop example asset root found locally: $sourceRoot" } else { "Official Workshop example asset root was not found locally." }
    @(
        "Official vehicle example replacement assets are intentionally not installed into this DTMAPI local package.",
        "The official example uses global sprite_vehicle_motor asset keys, which also changes the original Doloc Town motor.",
        "DTMAPI.SecondMotor now uses an instance-scoped GameBridge tint for the cloned motor until a scoped sprite adapter is implemented.",
        $sourceLine,
        "UpdatedAt=$(Get-Date -Format o)"
    ) | Set-Content -LiteralPath $notePath

    Write-Host "Skipped global official vehicle example sprite assets for SecondMotor; removed stale assets from $destRoot"
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
    $officialContentRoot = Join-Path $dest 'Content'
    if (Test-Path $officialContentRoot) {
        Get-ChildItem -LiteralPath $officialContentRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -ne 'DTMAPI' } |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
        Get-ChildItem -LiteralPath $officialContentRoot -File -Filter '*.json' -ErrorAction SilentlyContinue |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
    }
    $contentRoot = Join-Path $dest 'Content\DTMAPI'
    New-Item -ItemType Directory -Force -Path $contentRoot | Out-Null

    $manifest = Get-Content -Raw -LiteralPath $sourceManifestPath | ConvertFrom-Json
    $manifest.EntryDll = "Content/DTMAPI/$($Mod.PackageDll)"
    $manifest.MinimumDTMApiVersion = '0.4.0'
    if ($manifest.Dependencies) {
        foreach ($dependency in $manifest.Dependencies) {
            if ($dependency.UniqueID -eq 'DTMAPI.ModConfigMenu') {
                $dependency.MinimumVersion = '0.4.0'
            }
            if ($dependency.UniqueID -eq 'DTMAPI.GameBridge.DolocTown') {
                $dependency.MinimumVersion = '0.4.0'
            }
            if ($dependency.UniqueID -eq 'DTMAPI.DebugConsoleHost') {
                $dependency.MinimumVersion = '0.4.0'
            }
        }
    }

    Copy-Item -Force -LiteralPath $sourceDllPath -Destination (Join-Path $contentRoot $Mod.PackageDll)
    Write-JsonObject -Path (Join-Path $contentRoot 'manifest.json') -Value $manifest

    $i18nSource = Join-Path $repo "testmods\$($Mod.Project)\i18n"
    if (Test-Path $i18nSource) {
        Copy-DirectoryContents -Source (Split-Path -Parent $i18nSource) -Destination $dest -Include @('i18n')
    }

    $contentSource = Join-Path $repo "testmods\$($Mod.Project)\Content"
    if (Test-Path $contentSource) {
        Copy-DirectoryContents -Source (Split-Path -Parent $contentSource) -Destination $dest -Include @('Content')
    }

    if ($Mod.ContainsKey('CopyOfficialVehicleExampleAssets') -and $Mod.CopyOfficialVehicleExampleAssets) {
        Install-OfficialVehicleExampleAssets -GameDir $gameDir -Destination $dest
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
    Ensure-OfficialLocalDtmApiEnablement -OfficialFolder $Mod.OfficialFolder -Info $info

    $packageInfo = [ordered]@{
        owner = 'DTMAPI'
        uniqueId = $manifest.UniqueID
        generatedBy = 'tools/scripts/install-to-game.ps1'
        updatedAt = (Get-Date).ToString('o')
    }
    Write-JsonObject -Path $marker -Value $packageInfo
    Write-Host "Installed official local DTMAPI mod package to $dest"
}

function Ensure-OfficialLocalDtmApiEnablement {
    param(
        [Parameter(Mandatory = $true)] [string] $OfficialFolder,
        [Parameter(Mandatory = $true)] $Info
    )

    $persistentRoot = Get-DolocTownPersistentRoot
    $saveRoot = Join-Path $persistentRoot 'SAVE'
    New-Item -ItemType Directory -Force -Path $saveRoot | Out-Null
    $enablementPath = Join-Path $saveRoot 'mod_infos.json'
    if (Test-Path $enablementPath) {
        $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $enablementPath | ConvertFrom-Json
    }
    else {
        $data = [pscustomobject]@{ modInfos = [pscustomobject]@{} }
    }
    if ($null -eq $data.modInfos) {
        $data | Add-Member -MemberType NoteProperty -Name 'modInfos' -Value ([pscustomobject]@{}) -Force
    }

    $id = "Local.$OfficialFolder"
    if ($data.modInfos.PSObject.Properties[$id]) {
        return
    }

    $priority = 0
    foreach ($property in $data.modInfos.PSObject.Properties) {
        $value = $property.Value
        if ($value -and $value.enabled -and $value.priority -is [int]) {
            $priority = [Math]::Max($priority, [int]$value.priority + 1)
        }
    }

    $title = $null
    if ($Info.PSObject.Properties['title']) {
        $title = $Info.title
    }
    if (-not $title -and $Info.PSObject.Properties['name']) {
        $title = $Info.name
    }
    if ($Info.PSObject.Properties['localized_name'] -and $Info.localized_name.PSObject.Properties['schinese']) {
        $title = $Info.localized_name.schinese
    }
    if (-not $title) {
        $title = $OfficialFolder
    }

    $entry = [ordered]@{
        id = $id
        enabled = $true
        priority = $priority
        source = 'Local'
        title = $title
    }
    $data.modInfos | Add-Member -MemberType NoteProperty -Name $id -Value ([pscustomobject]$entry)
    Write-JsonObject -Path $enablementPath -Value $data
    Write-Host "Added official local enablement entry for $id"
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
        },
        @{
            OfficialFolder = 'DTMAPI_YKeyConsole'
            Project = 'DebugConsoleMod'
            SourceDll = 'DebugConsoleMod.dll'
            PackageDll = 'DTMAPI.YKeyConsole.dll'
        },
        @{
            OfficialFolder = 'DTMAPI_SecondMotor'
            Project = 'SecondMotorMod'
            SourceDll = 'SecondMotorMod.dll'
            PackageDll = 'DTMAPI.SecondMotor.dll'
            CopyOfficialVehicleExampleAssets = $true
        },
        @{
            OfficialFolder = 'DTMAPI_Oil'
            Project = 'OilMod'
            SourceDll = 'OilMod.dll'
            PackageDll = 'DTMAPI.Oil.dll'
        },
        @{
            OfficialFolder = 'DTMAPI_Mine'
            Project = 'MineMod'
            SourceDll = 'MineMod.dll'
            PackageDll = 'DTMAPI.Mine.dll'
        },
        @{
            OfficialFolder = 'DTMAPI_MoreEquipmentSlots'
            Project = 'MoreEquipmentSlotsMod'
            SourceDll = 'MoreEquipmentSlotsMod.dll'
            PackageDll = 'DTMAPI.MoreEquipmentSlots.dll'
        },
        @{
            OfficialFolder = 'DTMAPI_MoreSaves'
            Project = 'MoreSavesMod'
            SourceDll = 'MoreSavesMod.dll'
            PackageDll = 'DTMAPI.MoreSaves.dll'
        },
        @{
            OfficialFolder = 'DTMAPI_Zoom'
            Project = 'ZoomMod'
            SourceDll = 'ZoomMod.dll'
            PackageDll = 'DTMAPI.Zoom.dll'
        },
        @{
            OfficialFolder = 'DTMAPI_ChestLocatorEnhancer'
            Project = 'ChestLocatorEnhancerMod'
            SourceDll = 'ChestLocatorEnhancerMod.dll'
            PackageDll = 'DTMAPI.ChestLocatorEnhancer.dll'
        },
        @{
            OfficialFolder = 'DTMAPI_StrongPlantingGun'
            Project = 'StrongPlantingGunMod'
            SourceDll = 'StrongPlantingGunMod.dll'
            PackageDll = 'DTMAPI.StrongPlantingGun.dll'
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
