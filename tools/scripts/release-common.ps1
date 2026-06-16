Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:DtmApiReleaseVersion = '0.5.2-alpha'
$script:DtmApiBinaryVersion = '0.5.2.0'
$script:DtmApiInstallScriptVersion = '2'

function Write-Utf8NoBomJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value,
        [int] $Depth = 12
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $json = $Value | ConvertTo-Json -Depth $Depth
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Write-Utf8BomTextFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Text
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($Path, $Text, $utf8Bom)
}

function Copy-DtmApiTextFileUtf8Bom {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Missing text source file: $Source"
    }

    $text = [System.IO.File]::ReadAllText($Source, [System.Text.Encoding]::UTF8)
    Write-Utf8BomTextFile -Path $Destination -Text $text
}

function Test-DtmApiWindowsPowerShellSyntax {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Paths
    )

    $powershell = Get-Command powershell.exe -ErrorAction SilentlyContinue
    if (-not $powershell) {
        Write-Warning "powershell.exe was not found; skipping Windows PowerShell syntax validation."
        return
    }

    foreach ($path in $Paths) {
        $resolved = [System.IO.Path]::GetFullPath($path)
        if (-not (Test-Path -LiteralPath $resolved)) {
            throw "PowerShell syntax validation target is missing: $resolved"
        }

        $encodedPath = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($resolved))
        $checkScript = @"
`$path = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('$encodedPath'))
`$tokens = `$null
`$errors = `$null
[System.Management.Automation.Language.Parser]::ParseFile(`$path, [ref] `$tokens, [ref] `$errors) | Out-Null
if (`$errors -and `$errors.Count -gt 0) {
    foreach (`$err in `$errors) {
        Write-Error ("{0}:{1} {2}" -f `$err.Extent.StartLineNumber, `$err.Extent.StartColumnNumber, `$err.Message)
    }
    exit 1
}
"@
        $encodedCommand = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($checkScript))
        & $powershell.Source -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encodedCommand
        if ($LASTEXITCODE -ne 0) {
            throw "Windows PowerShell syntax validation failed for $resolved"
        }
    }
}

function Get-DtmApiSourceCommit {
    param(
        [string] $RepoRoot
    )

    if (-not $RepoRoot -or -not (Test-Path (Join-Path $RepoRoot '.git'))) {
        return ''
    }

    $commit = & git -C $RepoRoot rev-parse --short=12 HEAD 2>$null
    if ($LASTEXITCODE -ne 0) {
        return ''
    }

    return ($commit | Select-Object -First 1)
}

function Get-DtmApiPersistentRoot {
    if ($env:DTMAPI_DOLOC_PERSISTENT_ROOT) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_DOLOC_PERSISTENT_ROOT)
    }

    return Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\RedSawGames\DolocTown'
}

function Get-DtmApiPublishedModDefinitions {
    return @(
        [ordered]@{
            OfficialFolder = 'DTMAPI_Zoom'
            Project = 'ZoomMod'
            SourceDll = 'ZoomMod.dll'
            PackageDll = 'DTMAPI.Zoom.dll'
            UniqueID = 'DTMAPI.ZoomMod'
            DisplayName = 'DTMAPI Zoom'
            PackageName = 'DTMAPI-Zoom'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_ActionSpeed'
            Project = 'ActionSpeedMod'
            SourceDll = 'ActionSpeedMod.dll'
            PackageDll = 'Yuuka.DTMAPI.ActionSpeed.dll'
            UniqueID = 'Yuuka.DTMAPI.ActionSpeed'
            DisplayName = 'DTMAPI Action Speed'
            PackageName = 'DTMAPI-ActionSpeed'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_OneActionComplete'
            Project = 'OneActionCompleteMod'
            SourceDll = 'OneActionCompleteMod.dll'
            PackageDll = 'Yuuka.DTMAPI.OneActionComplete.dll'
            UniqueID = 'Yuuka.DTMAPI.OneActionComplete'
            DisplayName = 'DTMAPI One Action Complete'
            PackageName = 'DTMAPI-OneActionComplete'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_ChestLocatorEnhancer'
            Project = 'ChestLocatorEnhancerMod'
            SourceDll = 'ChestLocatorEnhancerMod.dll'
            PackageDll = 'DTMAPI.ChestLocatorEnhancer.dll'
            UniqueID = 'DTMAPI.ChestLocatorEnhancerMod'
            DisplayName = 'DTMAPI Chest Locator Enhancer'
            PackageName = 'DTMAPI-ChestLocatorEnhancer'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_YKeyConsole'
            Project = 'DebugConsoleMod'
            SourceDll = 'DebugConsoleMod.dll'
            PackageDll = 'DTMAPI.YKeyConsole.dll'
            UniqueID = 'DTMAPI.DebugConsoleMod'
            DisplayName = 'DTMAPI Y-Key Console'
            PackageName = 'DTMAPI-YKeyConsole'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_FishBreedingAssistant'
            Project = 'FishBreedingAssistantMod'
            SourceDll = 'FishBreedingAssistantMod.dll'
            PackageDll = 'Yuuka.DTMAPI.FishBreedingAssistant.dll'
            UniqueID = 'Yuuka.DTMAPI.FishBreedingAssistant'
            DisplayName = 'DTMAPI Fish Roe Info Display'
            PackageName = 'DTMAPI-FishBreedingAssistant'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_MoreSaves'
            Project = 'MoreSavesMod'
            SourceDll = 'MoreSavesMod.dll'
            PackageDll = 'DTMAPI.MoreSaves.dll'
            UniqueID = 'DTMAPI.MoreSavesMod'
            DisplayName = 'DTMAPI More Saves'
            PackageName = 'DTMAPI-MoreSaves'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_AnimalHusbandryProgress'
            Project = 'AnimalHusbandryProgressMod'
            SourceDll = 'AnimalHusbandryProgressMod.dll'
            PackageDll = 'Yuuka.DTMAPI.AnimalHusbandryProgress.dll'
            UniqueID = 'Yuuka.DTMAPI.AnimalHusbandryProgress'
            DisplayName = 'DTMAPI Animal Bell Hidden Produce Progress'
            PackageName = 'DTMAPI-AnimalHusbandryProgress'
        }
    )
}

function Get-DtmApiDeveloperOfficialModDefinitions {
    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($item in @(Get-DtmApiPublishedModDefinitions)) {
        $items.Add($item) | Out-Null
    }

    foreach ($item in @(
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_AutoFishing'
            Project = 'AutoFishingMod'
            SourceDll = 'AutoFishingMod.dll'
            PackageDll = 'Yuuka.DTMAPI.AutoFishing.dll'
            UniqueID = 'Yuuka.DTMAPI.AutoFishing'
            DisplayName = 'DTMAPI Auto Fishing'
            PackageName = 'DTMAPI-AutoFishing'
            DeveloperOnly = $true
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_Oil'
            Project = 'OilMod'
            SourceDll = 'OilMod.dll'
            PackageDll = 'DTMAPI.Oil.dll'
            UniqueID = 'DTMAPI.OilMod'
            DisplayName = 'DTMAPI Oil'
            PackageName = 'DTMAPI-Oil'
            DeveloperOnly = $true
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_Mine'
            Project = 'MineMod'
            SourceDll = 'MineMod.dll'
            PackageDll = 'DTMAPI.Mine.dll'
            UniqueID = 'DTMAPI.MineMod'
            DisplayName = 'DTMAPI Mine'
            PackageName = 'DTMAPI-Mine'
            DeveloperOnly = $true
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_MoreEquipmentSlots'
            Project = 'MoreEquipmentSlotsMod'
            SourceDll = 'MoreEquipmentSlotsMod.dll'
            PackageDll = 'DTMAPI.MoreEquipmentSlots.dll'
            UniqueID = 'DTMAPI.MoreEquipmentSlotsMod'
            DisplayName = 'DTMAPI More Equipment Slots'
            PackageName = 'DTMAPI-MoreEquipmentSlots'
            DeveloperOnly = $true
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_StrongPlantingGun'
            Project = 'StrongPlantingGunMod'
            SourceDll = 'StrongPlantingGunMod.dll'
            PackageDll = 'DTMAPI.StrongPlantingGun.dll'
            UniqueID = 'DTMAPI.StrongPlantingGunMod'
            DisplayName = 'DTMAPI Strong Planting Gun'
            PackageName = 'DTMAPI-StrongPlantingGun'
            DeveloperOnly = $true
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_CropHarvestingQA'
            Project = 'CropHarvestingQaMod'
            SourceDll = 'CropHarvestingQaMod.dll'
            PackageDll = 'DTMAPI.CropHarvestingQA.dll'
            UniqueID = 'DTMAPI.CropHarvestingQaMod'
            DisplayName = 'DTMAPI Crop Harvesting QA'
            PackageName = 'DTMAPI-CropHarvestingQA'
            DeveloperOnly = $true
            QaFixture = $true
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_ManboCardboardAudio'
            Project = 'ManboCardboardAudioMod'
            SourceDll = 'ManboCardboardAudioMod.dll'
            PackageDll = 'Yuuka.DTMAPI.ManboCardboardAudio.dll'
            UniqueID = 'Yuuka.DTMAPI.ManboCardboardAudio'
            DisplayName = 'DTMAPI Manbo Cardboard Audio'
            PackageName = 'DTMAPI-ManboCardboardAudio'
            DeveloperOnly = $true
        }
    )) {
        $items.Add($item) | Out-Null
    }

    return $items.ToArray()
}

function Test-DtmApiMapKey {
    param(
        [Parameter(Mandatory = $true)] $Map,
        [Parameter(Mandatory = $true)] [string] $Key
    )

    if ($Map -is [System.Collections.IDictionary]) {
        return $Map.Contains($Key)
    }

    return $null -ne $Map.PSObject.Properties[$Key]
}

function Get-DtmApiMapValue {
    param(
        [Parameter(Mandatory = $true)] $Map,
        [Parameter(Mandatory = $true)] [string] $Key,
        $Default = $null
    )

    if ($Map -is [System.Collections.IDictionary]) {
        if ($Map.Contains($Key)) {
            return $Map[$Key]
        }

        return $Default
    }

    $property = $Map.PSObject.Properties[$Key]
    if ($property) {
        return $property.Value
    }

    return $Default
}

function Get-DtmApiOwnedOfficialLocalPackages {
    param(
        [string] $PersistentRoot = (Get-DtmApiPersistentRoot)
    )

    $modsRoot = Join-Path $PersistentRoot 'MODS'
    if (-not (Test-Path -LiteralPath $modsRoot -PathType Container)) {
        return @()
    }

    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($dir in @(Get-ChildItem -LiteralPath $modsRoot -Directory -ErrorAction SilentlyContinue)) {
        $marker = Join-Path $dir.FullName 'Content\DTMAPI\dtmapi-package.json'
        if (-not (Test-Path -LiteralPath $marker -PathType Leaf)) {
            continue
        }

        $uniqueId = ''
        $version = ''
        try {
            $markerData = Get-Content -Raw -Encoding UTF8 -LiteralPath $marker | ConvertFrom-Json
            if ($markerData.PSObject.Properties['uniqueId']) {
                $uniqueId = [string]$markerData.uniqueId
            }
        }
        catch {
            $uniqueId = ''
        }

        $manifest = Join-Path $dir.FullName 'Content\DTMAPI\manifest.json'
        if (Test-Path -LiteralPath $manifest -PathType Leaf) {
            try {
                $manifestData = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifest | ConvertFrom-Json
                if (-not $uniqueId -and $manifestData.PSObject.Properties['UniqueID']) {
                    $uniqueId = [string]$manifestData.UniqueID
                }
                if ($manifestData.PSObject.Properties['Version']) {
                    $version = [string]$manifestData.Version
                }
            }
            catch {
                $version = ''
            }
        }

        $items.Add([ordered]@{
            OfficialFolder = $dir.Name
            ModInfoId = 'Local.' + $dir.Name
            Path = [System.IO.Path]::GetFullPath($dir.FullName)
            MarkerPath = [System.IO.Path]::GetFullPath($marker)
            UniqueID = $uniqueId
            Version = $version
        }) | Out-Null
    }

    return $items.ToArray()
}

function Add-DtmApiLegacyDetection {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [System.Collections.Generic.List[object]] $Items,
        [Parameter(Mandatory = $true)] [string] $Kind,
        [Parameter(Mandatory = $true)] [string] $Path,
        [string] $Action = 'detected-only',
        [string] $Notes = ''
    )

    if (-not (Test-Path $Path)) {
        return
    }

    $Items.Add([ordered]@{
        Kind = $Kind
        Path = [System.IO.Path]::GetFullPath($Path)
        Action = $Action
        Notes = $Notes
    }) | Out-Null
}

function Get-DtmApiLegacyDetections {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [string] $PersistentRoot = (Get-DtmApiPersistentRoot)
    )

    $items = New-Object 'System.Collections.Generic.List[object]'
    Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-smapi-plugin' -Path (Join-Path $GameDir 'BepInEx\plugins\DolocTownSMAPI') -Notes 'Old DolocTown SMAPI plugin directory.'
    Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-smapi-runtime' -Path (Join-Path $GameDir 'BepInEx\DolocTownSMAPI') -Notes 'Old DolocTown SMAPI runtime directory.'
    Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-workshop-bridge' -Path (Join-Path $GameDir 'BepInEx\plugins\DLKWorkshopBridge') -Notes 'Old DLK Workshop bridge plugin directory.'

    $gameMods = Join-Path $GameDir 'Mods'
    foreach ($id in @('Yuuka.DTMAPI.ActionSpeed', 'Yuuka.DTMAPI.AutoFishing', 'Yuuka.DTMAPI.OneActionComplete', 'Yuuka.DTMAPI.FishBreedingAssistant', 'Yuuka.DTMAPI.AnimalHusbandryProgress', 'DTMAPI.HookProbeMod', 'DTMAPI.HelloDtmMod', 'DTMAPI.ConfigMenuExample')) {
        Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-game-mod' -Path (Join-Path $gameMods $id) -Notes 'Old local Mods/ package; installer may back up migrated local packages but will not delete Workshop content.'
    }

    $localMods = Join-Path $PersistentRoot 'MODS'
    if (Test-Path $localMods) {
        foreach ($dir in @(Get-ChildItem -LiteralPath $localMods -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'DLK_*' })) {
            Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-local-dlk-mod' -Path $dir.FullName -Notes 'Old local DLK_* official-local package; detected for migration guidance.'
        }
    }

    $workshopRoot = [System.IO.Path]::GetFullPath((Join-Path $GameDir '..\..\workshop\content\2285550'))
    $legacyWorkshopIds = @(
        @{ Id = '3726044511'; Name = 'DolocTown SMAPI runtime' },
        @{ Id = '3728035966'; Name = 'old AutoFishing' },
        @{ Id = '3728245966'; Name = 'old Y console candidate' },
        @{ Id = '3728240703'; Name = 'old OneActionComplete' },
        @{ Id = '3728240789'; Name = 'old ActionSpeed' },
        @{ Id = '3729655857'; Name = 'old FishBreedingAssistant' },
        @{ Id = '3729757101'; Name = 'old AnimalHusbandryProgress' }
    )
    foreach ($entry in $legacyWorkshopIds) {
        Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-workshop-cache' -Path (Join-Path $workshopRoot $entry.Id) -Notes ($entry.Name + '; detected only, never deleted by DTMAPI.')
    }

    return $items.ToArray()
}

function New-DtmApiReleaseManifest {
    param(
        [string] $RepoRoot = '',
        [string] $PackageKind = 'local-install',
        [object[]] $IncludedAssemblies = @(),
        [object[]] $BundledMods = @()
    )

    $commit = Get-DtmApiSourceCommit -RepoRoot $RepoRoot
    [ordered]@{
        SchemaVersion = 1
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        BuildCommit = $commit
        BuildTime = (Get-Date).ToUniversalTime().ToString('o')
        PackageKind = $PackageKind
        SupportedGameVersion = 'Doloc Town Windows Steam build supported by the current Refactor smoke evidence'
        MinimumGameVersion = ''
        IncludedAssemblies = @($IncludedAssemblies)
        BundledMods = @($BundledMods)
        ExperimentalApiNotice = 'DTMAPI 0.5.2-alpha is a Developer Preview. Player-facing mods in this package are intended to be stable for normal use, but most GameBridge gameplay APIs remain Experimental for mod developers.'
    }
}

function New-DtmApiInstallState {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir,
        [object[]] $FilesInstalled = @(),
        [bool] $BepInExDetectedBeforeInstall = $false,
        [bool] $BepInExInstalledByDTMAPI = $false,
        [object[]] $BackupsCreated = @(),
        [object[]] $LegacyModsMoved = @(),
        [object[]] $LegacyDetections = @(),
        [object[]] $QaFixturesInstalled = @(),
        [bool] $DryRun = $false
    )

    $commit = Get-DtmApiSourceCommit -RepoRoot $RepoRoot
    [ordered]@{
        SchemaVersion = 1
        InstalledAt = (Get-Date).ToUniversalTime().ToString('o')
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        SourceRepoCommit = $commit
        GameDir = [System.IO.Path]::GetFullPath($GameDir)
        PluginDir = [System.IO.Path]::GetFullPath($PluginDir)
        FilesInstalled = @($FilesInstalled)
        BepInExDetectedBeforeInstall = $BepInExDetectedBeforeInstall
        BepInExInstalledByDTMAPI = $BepInExInstalledByDTMAPI
        BackupsCreated = @($BackupsCreated)
        LegacyModsMoved = @($LegacyModsMoved)
        LegacyDetections = @($LegacyDetections)
        QaFixturesInstalledCount = @($QaFixturesInstalled).Count
        QaFixturesInstalled = @($QaFixturesInstalled)
        InstallScriptVersion = $script:DtmApiInstallScriptVersion
        DryRun = $DryRun
    }
}
