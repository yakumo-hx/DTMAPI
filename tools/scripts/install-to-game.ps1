param(
    [string] $Configuration = 'Release',
    [switch] $IncludeTestMods,
    [switch] $IncludeDebugConsoleMod,
    [switch] $IncludeHookProbe,
    [switch] $SkipBuild,
    [switch] $InstallBepInEx,
    [switch] $SkipOfficialLocalMods,
    [switch] $KeepLegacyMigratedGameMods,
    [switch] $DryRun,
    [switch] $InstallPublishedModsOnly,
    [switch] $InstallAllDevOfficialMods,
    [switch] $InstallQaFixtures,
    [string] $PackagePayloadRoot = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$detectedPackagePayloadRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Payload'))
if ([string]::IsNullOrWhiteSpace($PackagePayloadRoot) -and (Test-Path $detectedPackagePayloadRoot)) {
    $PackagePayloadRoot = $detectedPackagePayloadRoot
}
$usingPackagePayload = -not [string]::IsNullOrWhiteSpace($PackagePayloadRoot)
if ($usingPackagePayload) {
    $SkipBuild = $true
    $SkipOfficialLocalMods = $true
}
if ($DryRun) {
    $SkipBuild = $true
}
if ($InstallPublishedModsOnly -and $InstallAllDevOfficialMods) {
    throw "Use only one of -InstallPublishedModsOnly or -InstallAllDevOfficialMods."
}
if ($InstallPublishedModsOnly -and $InstallQaFixtures) {
    throw "QA fixtures are developer-only. Do not combine -InstallPublishedModsOnly with -InstallQaFixtures."
}
if (-not $SkipBuild) {
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$outDir = if ($usingPackagePayload) { Join-Path $PackagePayloadRoot 'BepInEx\plugins\DTMAPI' } else { Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration }
$pluginDir = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
$stateDir = Resolve-DtmApiStateDir -GameDir $gameDir
$bepInExCore = Join-Path $gameDir 'BepInEx\core\BepInEx.dll'
$bepInExDetectedBeforeInstall = Test-Path $bepInExCore
$script:DtmInstallFilesInstalled = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallBackupsCreated = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallLegacyModsMoved = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallBundledMods = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallQaFixturesInstalled = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$script:DtmInstallModInfosBackupPath = ''

$runtimeFiles = @(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)

function Resolve-DtmApiRuntimeIconSource {
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($outDir)) {
        $candidates += (Join-Path $outDir 'assets\branding\dtmapi-icon.png')
    }
    if ($usingPackagePayload -and -not [string]::IsNullOrWhiteSpace($PackagePayloadRoot)) {
        $candidates += (Join-Path $PackagePayloadRoot 'BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png')
    }
    $candidates += (Join-Path $repo 'assets\branding\dtmapi-icon.png')

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    return ''
}

function Test-DtmApiDefinitionFlag {
    param(
        $Mod,
        [Parameter(Mandatory = $true)] [string] $Key
    )

    return (Test-DtmApiMapKey -Map $Mod -Key $Key) -and [bool](Get-DtmApiMapValue -Map $Mod -Key $Key -Default $false)
}

function Select-DtmApiOfficialLocalModDefinitions {
    if ($InstallPublishedModsOnly) {
        return @(Get-DtmApiPublishedModDefinitions)
    }

    $definitions = @(Get-DtmApiDeveloperOfficialModDefinitions)
    if (-not $InstallQaFixtures) {
        $definitions = @($definitions | Where-Object { -not (Test-DtmApiDefinitionFlag -Mod $_ -Key 'QaFixture') })
    }

    return $definitions
}

function Get-DtmApiOfficialLocalInstallMode {
    if ($InstallPublishedModsOnly) {
        return 'published release mods only'
    }

    if ($InstallQaFixtures) {
        return 'developer local official mods plus explicit QA fixtures'
    }

    return 'developer local official mods without QA fixtures'
}

if ($DryRun) {
    $legacyDetections = Get-DtmApiLegacyDetections -GameDir $gameDir
    $plannedOfficialMods = if ($SkipOfficialLocalMods) { @() } else { @(Select-DtmApiOfficialLocalModDefinitions) }
    $plannedQaFixtures = @($plannedOfficialMods | Where-Object { Test-DtmApiDefinitionFlag -Mod $_ -Key 'QaFixture' } | ForEach-Object {
        [ordered]@{
            OfficialFolder = $_.OfficialFolder
            UniqueID = $_.UniqueID
            PackageName = $_.PackageName
        }
    })
    $plannedRelease = New-DtmApiReleaseManifest -RepoRoot $repo -PackageKind 'dry-run' -IncludedAssemblies $runtimeFiles -BundledMods $plannedOfficialMods
    $plannedState = New-DtmApiInstallState -RepoRoot $repo -GameDir $gameDir -PluginDir $pluginDir -FilesInstalled $runtimeFiles -BepInExDetectedBeforeInstall $bepInExDetectedBeforeInstall -BepInExInstalledByDTMAPI $false -BackupsCreated @() -LegacyModsMoved @() -LegacyDetections $legacyDetections -QaFixturesInstalled $plannedQaFixtures -DryRun $true
    Write-Host "DRY RUN: would install DTMAPI $($plannedRelease.DTMAPIVersion) to $pluginDir"
    Write-Host "DRY RUN: would write release manifest to $(Join-Path $stateDir 'release-manifest.json')"
    Write-Host "DRY RUN: would write install state to $(Join-Path $stateDir 'install-state.json')"
    Write-Host "DRY RUN: detected legacy item count = $($legacyDetections.Count)"
    if (-not $SkipOfficialLocalMods) {
        Write-Host "DRY RUN: official local mod install mode = $(Get-DtmApiOfficialLocalInstallMode)"
        Write-Host "DRY RUN: official local mod count = $($plannedOfficialMods.Count)"
        Write-Host "DRY RUN: QA fixture install count = $($plannedQaFixtures.Count)"
    }
    $plannedState | ConvertTo-Json -Depth 12
    return
}

New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-DirectoryContents -Source $outDir -Destination $pluginDir -Include $runtimeFiles
foreach ($file in $runtimeFiles) {
    $path = Join-Path $pluginDir $file
    if (Test-Path $path) {
        $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'runtime-assembly'; Path = [System.IO.Path]::GetFullPath($path) }) | Out-Null
    }
}

$bepInExInstalledByDTMAPI = $false
if (-not (Test-Path $bepInExCore)) {
    if ($InstallBepInEx) {
        & "$PSScriptRoot\install-bepinex.ps1"
        $bepInExInstalledByDTMAPI = Test-Path $bepInExCore
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
    $record = [ordered]@{
        ModId = $ModId
        Source = [System.IO.Path]::GetFullPath($source)
        Destination = [System.IO.Path]::GetFullPath($dest)
        Reason = $Reason
    }
    $script:DtmInstallBackupsCreated.Add($record) | Out-Null
    $script:DtmInstallLegacyModsMoved.Add($record) | Out-Null
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

function Write-JsonObjectAtomic {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $tempPath = $Path + ".tmp"
    Write-JsonObject -Path $tempPath -Value $Value
    Move-Item -LiteralPath $tempPath -Destination $Path -Force
}

function Backup-ModInfosBeforeWrite {
    param([Parameter(Mandatory = $true)] [string] $EnablementPath)

    if (-not (Test-Path -LiteralPath $EnablementPath -PathType Leaf)) {
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($script:DtmInstallModInfosBackupPath)) {
        return
    }

    $backupRoot = Join-Path $stateDir ('backups\install-' + $script:DtmInstallStamp)
    New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
    $backupPath = Join-Path $backupRoot 'mod_infos.before.json'
    Copy-Item -LiteralPath $EnablementPath -Destination $backupPath -Force
    $script:DtmInstallModInfosBackupPath = [System.IO.Path]::GetFullPath($backupPath)
    $script:DtmInstallBackupsCreated.Add([ordered]@{
        Kind = 'mod-infos-before-install'
        Path = [System.IO.Path]::GetFullPath($EnablementPath)
        BackupPath = $script:DtmInstallModInfosBackupPath
        Reason = 'Before adding DTMAPI official-local enablement entries'
    }) | Out-Null
}

function Get-NumericPriorityOrNull {
    param($Value)

    if ($null -eq $Value) {
        return $null
    }

    try {
        return [int]$Value
    }
    catch {
        return $null
    }
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

    $staleRoots = @(
        (Join-Path $Destination 'Content\DTMAPI\official-vehicle-example'),
        (Join-Path $Destination 'Content\DTMAPI\vehicle-appearance')
    )
    foreach ($staleRoot in $staleRoots) {
        if (Test-Path $staleRoot) {
            Remove-Item -LiteralPath $staleRoot -Recurse -Force
        }
    }

    $privateRoot = Join-Path $Destination 'Content\DTMAPI\assets\second-motor'
    New-Item -ItemType Directory -Force -Path $privateRoot | Out-Null
    $notePath = Join-Path $privateRoot 'official-vehicle-assets.txt'
    $sourceRoot = Find-OfficialVehicleExampleAssetRoot -GameDir $GameDir
    $sourceLine = if ($sourceRoot) { "Official Workshop example asset root found locally: $sourceRoot" } else { "Official Workshop example asset root was not found locally." }
    $copied = @()
    if ($sourceRoot) {
        $assetCopies = @(
            @{ Source = 'sprite_vehicle_motor.png'; Destination = 'dtmapi_second_motor.png' },
            @{ Source = 'sprite_vehicle_motor.json'; Destination = 'dtmapi_second_motor.json' },
            @{ Source = 'sprite_vehicle_motor_light_mask.png'; Destination = 'dtmapi_second_motor_light_mask.png' },
            @{ Source = 'sprite_vehicle_motor_light_mask.json'; Destination = 'dtmapi_second_motor_light_mask.json' }
        )
        foreach ($assetCopy in $assetCopies) {
            $sourcePath = Join-Path $sourceRoot $assetCopy.Source
            if (Test-Path -LiteralPath $sourcePath -PathType Leaf) {
                Copy-Item -Force -LiteralPath $sourcePath -Destination (Join-Path $privateRoot $assetCopy.Destination)
                $copied += "$($assetCopy.Source)->$($assetCopy.Destination)"
            }
        }
    }
    @(
        "Official vehicle example replacement assets are intentionally not installed as global replacement keys.",
        "The official example uses global sprite_vehicle_motor asset keys, which also changes the original Doloc Town motor.",
        "DTMAPI.SecondMotor copies locally available official example textures into DTMAPI-named private assets and applies them only to the DTMAPI clone through GameBridge scoped-sprite mode.",
        "CopiedFiles=$([string]::Join(',', $copied))",
        $sourceLine,
        "UpdatedAt=$(Get-Date -Format o)"
    ) | Set-Content -LiteralPath $notePath

    Write-Host "Installed private official vehicle example sprite assets for SecondMotor; removed stale global vehicle assets from $([string]::Join(', ', $staleRoots))"
}

function Install-OfficialLocalDtmApiMod {
    param(
        $Mod
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

    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourceManifestPath | ConvertFrom-Json
    $manifest.EntryDll = "Content/DTMAPI/$($Mod.PackageDll)"
    $manifest.MinimumDTMApiVersion = '0.5.2-alpha'
    if ($manifest.Dependencies) {
        foreach ($dependency in $manifest.Dependencies) {
            if ($dependency.UniqueID -eq 'DTMAPI.ModConfigMenu') {
                $dependency.MinimumVersion = '0.5.2-alpha'
            }
            if ($dependency.UniqueID -eq 'DTMAPI.GameBridge.DolocTown') {
                $dependency.MinimumVersion = '0.5.2-alpha'
            }
            if ($dependency.UniqueID -eq 'DTMAPI.DebugConsoleHost') {
                $dependency.MinimumVersion = '0.5.2-alpha'
            }
        }
    }

    Copy-Item -Force -LiteralPath $sourceDllPath -Destination (Join-Path $contentRoot $Mod.PackageDll)
    $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'official-local-dll'; Path = [System.IO.Path]::GetFullPath((Join-Path $contentRoot $Mod.PackageDll)) }) | Out-Null
    Write-JsonObject -Path (Join-Path $contentRoot 'manifest.json') -Value $manifest
    $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'official-local-manifest'; Path = [System.IO.Path]::GetFullPath((Join-Path $contentRoot 'manifest.json')) }) | Out-Null

    $i18nSource = Join-Path $repo "testmods\$($Mod.Project)\i18n"
    if (Test-Path $i18nSource) {
        Copy-DirectoryContents -Source (Split-Path -Parent $i18nSource) -Destination $dest -Include @('i18n')
    }

    $contentSource = Join-Path $repo "testmods\$($Mod.Project)\Content"
    if (Test-Path $contentSource) {
        Copy-DirectoryContents -Source (Split-Path -Parent $contentSource) -Destination $dest -Include @('Content')
    }

    if ((Test-DtmApiMapKey -Map $Mod -Key 'CopyOfficialVehicleExampleAssets') -and $Mod.CopyOfficialVehicleExampleAssets) {
        Install-OfficialVehicleExampleAssets -GameDir $gameDir -Destination $dest
    }

    $modSourceRoot = Join-Path $repo "testmods\$($Mod.Project)"
    $assetIcon = Join-Path $repo 'assets\branding\dtmapi-icon.png'
    $modIcon = Join-Path $modSourceRoot 'icon.png'
    if (Test-Path -LiteralPath $modIcon -PathType Leaf) {
        $assetIcon = $modIcon
    }

    $assetPreview = Join-Path $repo 'assets\branding\dtmapi-preview.png'
    $modPreview = Join-Path $modSourceRoot 'preview.png'
    if (Test-Path -LiteralPath $modPreview -PathType Leaf) {
        $assetPreview = $modPreview
    }

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
    if (-not $info.PSObject.Properties['version']) {
        $info | Add-Member -NotePropertyName version -NotePropertyValue $manifest.Version
    } elseif ([string]::IsNullOrWhiteSpace([string]$info.version)) {
        $info.version = $manifest.Version
    }
    Write-JsonObject -Path (Join-Path $dest 'info.json') -Value $info
    $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'official-local-info'; Path = [System.IO.Path]::GetFullPath((Join-Path $dest 'info.json')) }) | Out-Null
    Ensure-OfficialLocalDtmApiEnablement -OfficialFolder $Mod.OfficialFolder -Info $info

    $packageInfo = [ordered]@{
        owner = 'DTMAPI'
        uniqueId = $manifest.UniqueID
        generatedBy = 'tools/scripts/install-to-game.ps1'
        updatedAt = (Get-Date).ToString('o')
    }
    Write-JsonObject -Path $marker -Value $packageInfo
    $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'official-local-package-marker'; Path = [System.IO.Path]::GetFullPath($marker) }) | Out-Null
    $script:DtmInstallBundledMods.Add([ordered]@{
        OfficialFolder = $Mod.OfficialFolder
        UniqueID = $manifest.UniqueID
        Version = $manifest.Version
        PackageDll = $Mod.PackageDll
        Path = [System.IO.Path]::GetFullPath($dest)
    }) | Out-Null
    if (Test-DtmApiDefinitionFlag -Mod $Mod -Key 'QaFixture') {
        $script:DtmInstallQaFixturesInstalled.Add([ordered]@{
            OfficialFolder = $Mod.OfficialFolder
            UniqueID = $manifest.UniqueID
            Version = $manifest.Version
            PackageDll = $Mod.PackageDll
            Path = [System.IO.Path]::GetFullPath($dest)
        }) | Out-Null
    }
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
        try {
            $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $enablementPath | ConvertFrom-Json
        }
        catch {
            throw "Could not read $enablementPath. Refusing to overwrite mod_infos.json. $($_.Exception.Message)"
        }
    }
    else {
        $data = [pscustomobject]@{ modInfos = [pscustomobject]@{} }
    }
    if ($null -eq $data.modInfos) {
        $data | Add-Member -MemberType NoteProperty -Name 'modInfos' -Value ([pscustomobject]@{}) -Force
    }

    $id = "Local.$OfficialFolder"
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

    if ($data.modInfos.PSObject.Properties[$id]) {
        $existing = $data.modInfos.PSObject.Properties[$id].Value
        if (-not $existing.PSObject.Properties['title']) {
            Backup-ModInfosBeforeWrite -EnablementPath $enablementPath
            $existing | Add-Member -MemberType NoteProperty -Name 'title' -Value $title
            Write-JsonObjectAtomic -Path $enablementPath -Value $data
            Write-Host "Updated official local enablement title for $id"
            return
        }

        if ([string]$existing.title -ne [string]$title) {
            Backup-ModInfosBeforeWrite -EnablementPath $enablementPath
            $existing.title = $title
            Write-JsonObjectAtomic -Path $enablementPath -Value $data
            Write-Host "Updated official local enablement title for $id"
        }
        return
    }

    $priority = 0
    foreach ($property in $data.modInfos.PSObject.Properties) {
        $value = $property.Value
        if ($value -and $value.PSObject.Properties['enabled'] -and [bool]$value.enabled -and $value.PSObject.Properties['priority']) {
            $existingPriority = Get-NumericPriorityOrNull -Value $value.priority
            if ($null -ne $existingPriority) {
                $priority = [Math]::Max($priority, $existingPriority + 1)
            }
        }
    }

    $entry = [ordered]@{
        id = $id
        enabled = $true
        priority = $priority
        source = 'Local'
        title = $title
    }
    Backup-ModInfosBeforeWrite -EnablementPath $enablementPath
    $data.modInfos | Add-Member -MemberType NoteProperty -Name $id -Value ([pscustomobject]$entry)
    Write-JsonObjectAtomic -Path $enablementPath -Value $data
    Write-Host "Added official local enablement entry for $id"
}

if (-not $SkipOfficialLocalMods) {
    $officialLocalMods = @(Select-DtmApiOfficialLocalModDefinitions)
    Write-Host "Installing official local DTMAPI packages using mode: $(Get-DtmApiOfficialLocalInstallMode)"

    foreach ($mod in $officialLocalMods) {
        Install-OfficialLocalDtmApiMod -Mod $mod
    }
}

$assetSource = Resolve-DtmApiRuntimeIconSource
if (-not [string]::IsNullOrWhiteSpace($assetSource)) {
    $assetDest = Join-Path $pluginDir 'assets\branding'
    New-Item -ItemType Directory -Force -Path $assetDest | Out-Null
    Copy-Item -LiteralPath $assetSource -Destination (Join-Path $assetDest 'dtmapi-icon.png') -Force
    $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'runtime-asset'; Path = [System.IO.Path]::GetFullPath((Join-Path $assetDest 'dtmapi-icon.png')) }) | Out-Null
}
else {
    Write-Warning "DTMAPI title icon asset was not found in the runtime payload or repository assets. The title button will use its text fallback."
}

New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$stateToolsDir = Join-Path $stateDir 'tools'
New-Item -ItemType Directory -Force -Path $stateToolsDir | Out-Null
foreach ($scriptName in @('common.ps1', 'release-common.ps1', 'uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1')) {
    $sourceScript = Join-Path $PSScriptRoot $scriptName
    if (Test-Path $sourceScript) {
        $destScript = Join-Path $stateToolsDir $scriptName
        Copy-DtmApiTextFileUtf8Bom -Source $sourceScript -Destination $destScript
        $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'installer-tool'; Path = [System.IO.Path]::GetFullPath($destScript) }) | Out-Null
    }
}
Test-DtmApiWindowsPowerShellSyntax -Paths @(
    Get-ChildItem -LiteralPath $stateToolsDir -Filter '*.ps1' -File | ForEach-Object { $_.FullName }
)

$legacyDetectionsAfterInstall = Get-DtmApiLegacyDetections -GameDir $gameDir
$includedAssemblies = @($runtimeFiles | ForEach-Object { [ordered]@{ FileName = $_; Path = [System.IO.Path]::GetFullPath((Join-Path $pluginDir $_)) } })
$bundledMods = $script:DtmInstallBundledMods.ToArray()
$qaFixturesInstalled = $script:DtmInstallQaFixturesInstalled.ToArray()
$releaseManifest = New-DtmApiReleaseManifest `
    -RepoRoot $repo `
    -PackageKind 'local-install' `
    -IncludedAssemblies $includedAssemblies `
    -BundledMods $bundledMods
$releaseManifestPath = Join-Path $stateDir 'release-manifest.json'
Write-Utf8NoBomJson -Path $releaseManifestPath -Value $releaseManifest
$script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'release-manifest'; Path = [System.IO.Path]::GetFullPath($releaseManifestPath) }) | Out-Null

$installStatePath = Join-Path $stateDir 'install-state.json'
$installedFiles = $script:DtmInstallFilesInstalled.ToArray()
$backupsCreated = $script:DtmInstallBackupsCreated.ToArray()
$legacyModsMoved = $script:DtmInstallLegacyModsMoved.ToArray()
$installState = New-DtmApiInstallState `
    -RepoRoot $repo `
    -GameDir $gameDir `
    -PluginDir $pluginDir `
    -FilesInstalled $installedFiles `
    -BepInExDetectedBeforeInstall $bepInExDetectedBeforeInstall `
    -BepInExInstalledByDTMAPI $bepInExInstalledByDTMAPI `
    -BackupsCreated $backupsCreated `
    -LegacyModsMoved $legacyModsMoved `
    -LegacyDetections $legacyDetectionsAfterInstall `
    -QaFixturesInstalled $qaFixturesInstalled `
    -DryRun $false
Write-Utf8NoBomJson -Path $installStatePath -Value $installState

Write-Host "Installed DTMAPI to $pluginDir"
Write-Host "Wrote DTMAPI release manifest to $releaseManifestPath"
Write-Host "Wrote DTMAPI install state to $installStatePath"
