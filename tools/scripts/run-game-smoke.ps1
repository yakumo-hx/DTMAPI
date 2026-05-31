param(
    [int] $SaveSlot = 3,
    [int] $TimeoutSeconds = 180,
    [switch] $IncludeHookProbe,
    [switch] $UseSteam,
    [switch] $DirectExe,
    [switch] $AutoSaveAfterLoad,
    [switch] $AutoReloadMods,
    [switch] $AutoExerciseExperimentalHooks,
    [switch] $AutoExerciseActionSpeedTool,
    [switch] $AutoExerciseActionSpeedConfigApply,
    [switch] $AutoExerciseActionSpeedInteraction,
    [switch] $AutoExerciseOneActionResourceHit,
    [switch] $AutoExerciseOneActionWrongTool,
    [switch] $AutoExerciseOneActionFuelFeed,
    [switch] $AutoExerciseOneActionVegetation,
    [switch] $AutoExerciseAutoFishingPhase,
    [switch] $AutoExerciseTitleButtonLifecycle,
    [switch] $AutoExerciseInstantSave,
    [switch] $AutoPressAutoFishingHotkey,
    [switch] $AutoOpenTitleSettingsMenu,
    [switch] $AutoOpenOfficialModUi,
    [switch] $AutoOpenAnimalPanel,
    [int] $AutoExitAfterSecondsOverride = 0,
    [switch] $SkipBuild
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$launchViaSteam = [bool]$UseSteam -or -not [bool]$DirectExe
$existingGameProcess = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
if ($existingGameProcess) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    "Started=$(Get-Date -Format o)`nBlocked=DolocTown.exe already running before smoke launch." | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-Error "DolocTown.exe is already running. Close the existing game/window before launching smoke. Evidence: $evidence"
    exit 1
}
$existingFatalWindow = Test-FatalInstanceWindow
if ($existingFatalWindow) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    "Started=$(Get-Date -Format o)`nBlocked=Fatal instance popup already visible before smoke launch." | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-Error "Fatal instance popup is already visible. Close the dialog before launching smoke. Evidence: $evidence"
    exit 1
}

$includeRuntimeTestMods = [bool]$IncludeHookProbe -or [bool]$AutoOpenTitleSettingsMenu
& "$PSScriptRoot\install-to-game.ps1" -IncludeTestMods:$includeRuntimeTestMods -IncludeHookProbe:$IncludeHookProbe -SkipBuild:$SkipBuild
$installExit = if (Get-Variable LASTEXITCODE -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
if ($installExit -ne 0) {
    exit $installExit
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Join-Path $gameDir 'DTMAPI'
New-Item -ItemType Directory -Force -Path $dtmapiDir | Out-Null
$evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
$oneActionConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.OneActionComplete.json'
$oneActionConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.OneActionComplete.before.json'
$oneActionConfigHadOriginal = $false
$usesOneActionConfigSmoke = [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation
$actionSpeedConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.ActionSpeed.json'
$actionSpeedConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.ActionSpeed.before.json'
$actionSpeedConfigHadOriginal = $false
$usesActionSpeedConfigSmoke = [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction
$autoFishingConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.AutoFishing.json'
$autoFishingConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.AutoFishing.before.json'
$autoFishingConfigHadOriginal = $false
if ($usesActionSpeedConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $actionSpeedConfigPath) | Out-Null
    if (Test-Path $actionSpeedConfigPath) {
        Copy-Item -Force -LiteralPath $actionSpeedConfigPath -Destination $actionSpeedConfigBackup
        $actionSpeedConfigHadOriginal = $true
    }
    else {
        'No pre-existing ActionSpeed config file.' | Set-Content -LiteralPath $actionSpeedConfigBackup
    }
    $actionSpeedToolMultiplier = if ($AutoExerciseActionSpeedConfigApply) { 2 } else { 3 }
    $actionSpeedToolEnabled = [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply
    $actionSpeedInteractionEnabled = [bool]$AutoExerciseActionSpeedInteraction
    @{
        Enabled = $true
        Profile = 'Debug'
        ToolSpeedEnabled = $actionSpeedToolEnabled
        ToolMultiplier = $actionSpeedToolMultiplier
        BottleFillSpeedEnabled = $actionSpeedInteractionEnabled
        BottleFillMultiplier = 3
        EatDrinkSpeedEnabled = $actionSpeedInteractionEnabled
        EatDrinkMultiplier = 3
        MachineAddSpeedEnabled = $actionSpeedInteractionEnabled
        MachineAddMultiplier = 3
        HarvestSpeedEnabled = $actionSpeedInteractionEnabled
        HarvestMultiplier = 3
        PlantSpeedEnabled = $actionSpeedInteractionEnabled
        PlantMultiplier = 3
        AutoFillBottle = $actionSpeedInteractionEnabled
        ContinuousDrinkWithRightClick = $actionSpeedInteractionEnabled
        ContinuousDrinkHoldKey = 'None'
        MenuKey = 'F10'
        AutoActionCooldownSeconds = 0.25
        DebugLabel = 'DTMAPI smoke'
    } | ConvertTo-Json | Set-Content -LiteralPath $actionSpeedConfigPath
}
if ($usesOneActionConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $oneActionConfigPath) | Out-Null
    if (Test-Path $oneActionConfigPath) {
        Copy-Item -Force -LiteralPath $oneActionConfigPath -Destination $oneActionConfigBackup
        $oneActionConfigHadOriginal = $true
    }
    else {
        'No pre-existing OneAction config file.' | Set-Content -LiteralPath $oneActionConfigBackup
    }
    @{
        Enabled = $true
        CompleteTrees = $true
        CompleteOres = $true
        CompleteGarbage = $true
        CompleteWeeds = $true
        CompleteMachineFuel = [bool]$AutoExerciseOneActionFuelFeed
        CompleteFeeder = [bool]$AutoExerciseOneActionFuelFeed
        VerboseLogging = $true
        MenuKey = 'F11'
    } | ConvertTo-Json | Set-Content -LiteralPath $oneActionConfigPath
}
if ($AutoExerciseAutoFishingPhase) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $autoFishingConfigPath) | Out-Null
    if (Test-Path $autoFishingConfigPath) {
        Copy-Item -Force -LiteralPath $autoFishingConfigPath -Destination $autoFishingConfigBackup
        $autoFishingConfigHadOriginal = $true
    }
    else {
        'No pre-existing AutoFishing config file.' | Set-Content -LiteralPath $autoFishingConfigBackup
    }
    @{
        ToggleKey = 'F6'
        InfoKey = 'F9'
        AutoRecast = $false
        StopOnManualMove = $true
        RequireSelectedFishingRod = $false
        CastReleaseProgress = 0
        RecastDelaySeconds = 0.25
        SkipMiniGame = $false
        InstantBite = $true
        FastAnimations = $false
        FastAnimationMultiplier = 3
        VerboseLogging = $true
    } | ConvertTo-Json | Set-Content -LiteralPath $autoFishingConfigPath
}
function Send-DolocTownF6 {
    param(
        [int] $TimeoutSeconds = 20
    )

    if (-not ('DtmApiSmokeInput' -as [type])) {
        Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class DtmApiSmokeInput
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
"@
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($proc) {
            [void][DtmApiSmokeInput]::ShowWindow($proc.MainWindowHandle, 9)
            [void][DtmApiSmokeInput]::BringWindowToTop($proc.MainWindowHandle)
            [void][DtmApiSmokeInput]::SetForegroundWindow($proc.MainWindowHandle)
            Start-Sleep -Milliseconds 500
            [DtmApiSmokeInput]::keybd_event(0x75, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 120
            [DtmApiSmokeInput]::keybd_event(0x75, 0, 0x0002, [UIntPtr]::Zero)
            return $true
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    return $false
}

function Wait-ForStartupLogWithTimeline {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds,
        [string] $EvidenceDir,
        [datetime] $LaunchCommandStartedAt,
        [datetime] $LaunchCommandFinishedAt,
        [string] $LaunchMode
    )

    function Format-DateOrNull {
        param($Value)
        if ($null -eq $Value) {
            return $null
        }
        return ([datetime]$Value).ToString('o')
    }

    function Get-ElapsedMsOrNull {
        param($From, $To)
        if ($null -eq $From -or $null -eq $To) {
            return $null
        }
        return [int64]([datetime]$To - [datetime]$From).TotalMilliseconds
    }

    $waitStartedAt = Get-Date
    $deadline = $waitStartedAt.AddSeconds($TimeoutSeconds)
    $firstProcessAt = $null
    $firstProcessId = $null
    $firstLogFileAt = $null
    $firstPatternAt = $null
    $fatalWindowAt = $null

    while ((Get-Date) -lt $deadline) {
        if ($null -eq $firstProcessAt) {
            $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($proc) {
                $firstProcessAt = Get-Date
                $firstProcessId = $proc.Id
            }
        }

        if (Test-Path $LogPath) {
            if ($null -eq $firstLogFileAt) {
                $firstLogFileAt = Get-Date
            }

            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            if ($text -match [regex]::Escape($Pattern)) {
                $firstPatternAt = Get-Date
                break
            }
        }

        if (Test-FatalInstanceWindow) {
            $fatalWindowAt = Get-Date
            break
        }

        Start-Sleep -Seconds 1
    }

    $finishedAt = Get-Date
    $found = $null -ne $firstPatternAt
    $timeline = [ordered]@{
        LaunchMode = $LaunchMode
        LaunchCommandStartedAt = Format-DateOrNull $LaunchCommandStartedAt
        LaunchCommandFinishedAt = Format-DateOrNull $LaunchCommandFinishedAt
        WaitStartedAt = Format-DateOrNull $waitStartedAt
        WaitFinishedAt = Format-DateOrNull $finishedAt
        WaitTimeoutSeconds = $TimeoutSeconds
        StartupPattern = $Pattern
        StartupLogFound = $found
        TimedOut = (-not $found) -and ($null -eq $fatalWindowAt)
        FatalInstanceWindowAt = Format-DateOrNull $fatalWindowAt
        FirstDolocTownProcessAt = Format-DateOrNull $firstProcessAt
        FirstDolocTownProcessId = $firstProcessId
        LaunchToProcessMs = Get-ElapsedMsOrNull $LaunchCommandStartedAt $firstProcessAt
        FirstDtmapiLogFileAt = Format-DateOrNull $firstLogFileAt
        LaunchToDtmapiLogFileMs = Get-ElapsedMsOrNull $LaunchCommandStartedAt $firstLogFileAt
        FirstStartupPatternAt = Format-DateOrNull $firstPatternAt
        LaunchToStartupPatternMs = Get-ElapsedMsOrNull $LaunchCommandStartedAt $firstPatternAt
        WaitDurationMs = Get-ElapsedMsOrNull $waitStartedAt $finishedAt
    }
    $timeline | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $EvidenceDir 'startup-timeline.json')

    return $found
}

$autoExitAfterSeconds = if ($AutoExitAfterSecondsOverride -gt 0) {
    $AutoExitAfterSecondsOverride
}
elseif (($AutoOpenTitleSettingsMenu -or $AutoOpenOfficialModUi) -and $SaveSlot -le 0) {
    [Math]::Max(25, $TimeoutSeconds - 20)
}
else {
    [Math]::Max(90, $TimeoutSeconds - 30)
}
$smokeSettings = @{
    Enabled = $true
    AutoLoadSaveSlot = $SaveSlot
    AutoLoadDelaySeconds = 8
    AutoExitAfterSeconds = $autoExitAfterSeconds
    AutoExitAfterSaveLoaded = $true
    AutoSaveAfterLoad = [bool]$AutoSaveAfterLoad
    AutoSaveDelaySeconds = 2
    AutoReloadMods = [bool]$AutoReloadMods
    AutoReloadModsDelaySeconds = 12
    AutoExerciseExperimentalHooks = [bool]$AutoExerciseExperimentalHooks
    AutoExerciseActionSpeedTool = [bool]$AutoExerciseActionSpeedTool
    AutoExerciseActionSpeedToolDelaySeconds = 3
    AutoExerciseActionSpeedConfigApply = [bool]$AutoExerciseActionSpeedConfigApply
    AutoExerciseActionSpeedConfigApplyDelaySeconds = 3
    AutoExerciseActionSpeedInteraction = [bool]$AutoExerciseActionSpeedInteraction
    AutoExerciseActionSpeedInteractionDelaySeconds = 3
    AutoExerciseOneActionResourceHit = [bool]$AutoExerciseOneActionResourceHit
    AutoExerciseOneActionResourceHitDelaySeconds = 3
    AutoExerciseOneActionWrongTool = [bool]$AutoExerciseOneActionWrongTool
    AutoExerciseOneActionWrongToolDelaySeconds = 3
    AutoExerciseOneActionFuelFeed = [bool]$AutoExerciseOneActionFuelFeed
    AutoExerciseOneActionFuelFeedDelaySeconds = 3
    AutoExerciseOneActionVegetation = [bool]$AutoExerciseOneActionVegetation
    AutoExerciseOneActionVegetationDelaySeconds = 3
    AutoExerciseAutoFishingPhase = [bool]$AutoExerciseAutoFishingPhase
    AutoExerciseAutoFishingPhaseDelaySeconds = 3
    AutoExerciseTitleButtonLifecycle = [bool]$AutoExerciseTitleButtonLifecycle
    AutoExerciseInstantSave = [bool]$AutoExerciseInstantSave
    AutoExerciseInstantSaveDelaySeconds = 3
    AutoFishingExternalHotkeyRequired = [bool]$AutoPressAutoFishingHotkey
    AutoOpenTitleSettingsMenu = [bool]$AutoOpenTitleSettingsMenu
    AutoOpenTitleSettingsDelaySeconds = 12
    AutoOpenOfficialModUi = [bool]$AutoOpenOfficialModUi
    AutoOpenOfficialModUiDelaySeconds = 12
    AutoOpenAnimalPanel = [bool]$AutoOpenAnimalPanel
    AutoOpenAnimalPanelDelaySeconds = 2
} | ConvertTo-Json
$smokeSettings | Set-Content -LiteralPath (Join-Path $dtmapiDir 'smoke-settings.json')
$freshLogPath = Join-Path $gameDir 'DTMAPI\logs\latest.log'
$freshBepLogPath = Join-Path $gameDir 'BepInEx\LogOutput.log'
if (Test-Path $freshLogPath) {
    Remove-Item -Force -LiteralPath $freshLogPath
}
if (Test-Path $freshBepLogPath) {
    Remove-Item -Force -LiteralPath $freshBepLogPath
}

"Started=$(Get-Date -Format o)`nGameDir=$gameDir`nSaveSlot=$SaveSlot`nIncludeHookProbe=$IncludeHookProbe`nLaunchMode=$(if ($launchViaSteam) { 'Steam' } else { 'DirectExe' })`nAutoSaveAfterLoad=$AutoSaveAfterLoad`nAutoReloadMods=$AutoReloadMods`nAutoExerciseExperimentalHooks=$AutoExerciseExperimentalHooks`nAutoExerciseActionSpeedTool=$AutoExerciseActionSpeedTool`nAutoExerciseActionSpeedConfigApply=$AutoExerciseActionSpeedConfigApply`nAutoExerciseActionSpeedInteraction=$AutoExerciseActionSpeedInteraction`nAutoExerciseOneActionResourceHit=$AutoExerciseOneActionResourceHit`nAutoExerciseOneActionWrongTool=$AutoExerciseOneActionWrongTool`nAutoExerciseOneActionFuelFeed=$AutoExerciseOneActionFuelFeed`nAutoExerciseOneActionVegetation=$AutoExerciseOneActionVegetation`nAutoExerciseAutoFishingPhase=$AutoExerciseAutoFishingPhase`nAutoExerciseTitleButtonLifecycle=$AutoExerciseTitleButtonLifecycle`nAutoExerciseInstantSave=$AutoExerciseInstantSave`nAutoPressAutoFishingHotkey=$AutoPressAutoFishingHotkey`nAutoOpenTitleSettingsMenu=$AutoOpenTitleSettingsMenu`nAutoOpenOfficialModUi=$AutoOpenOfficialModUi`nAutoOpenAnimalPanel=$AutoOpenAnimalPanel`nAutoExitAfterSeconds=$autoExitAfterSeconds" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$launchCommandStartedAt = Get-Date
if ($launchViaSteam) {
    Start-Process 'steam://rungameid/2285550'
}
else {
    $exe = Join-Path $gameDir 'DolocTown.exe'
    if (-not (Test-Path $exe)) {
        throw "DolocTown.exe not found at $exe"
    }
    Start-Process -FilePath $exe -WorkingDirectory $gameDir
}
$launchCommandFinishedAt = Get-Date
"LaunchCommandStarted=$($launchCommandStartedAt.ToString('o'))`nLaunchCommandFinished=$($launchCommandFinishedAt.ToString('o'))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$logPath = $freshLogPath
$startupTimeoutSeconds = $TimeoutSeconds
$launchModeLabel = if ($launchViaSteam) { 'Steam' } else { 'DirectExe' }
$startupOk = Wait-ForStartupLogWithTimeline -LogPath $logPath -Pattern 'DTMAPI runtime starting.' -TimeoutSeconds $startupTimeoutSeconds -EvidenceDir $evidence -LaunchCommandStartedAt $launchCommandStartedAt -LaunchCommandFinishedAt $launchCommandFinishedAt -LaunchMode $launchModeLabel
$gameLaunchedOk = $false
$probeOk = -not [bool]$IncludeHookProbe
$saveLoadedOk = -not (($SaveSlot -gt 0) -and ([bool]$IncludeHookProbe -or [bool]$AutoOpenAnimalPanel -or [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation -or [bool]$AutoExerciseAutoFishingPhase -or [bool]$AutoExerciseTitleButtonLifecycle -or [bool]$AutoExerciseInstantSave))
$titleLifecycleOk = -not [bool]$AutoExerciseTitleButtonLifecycle
$titleButtonOk = -not [bool]$AutoOpenTitleSettingsMenu
$titleButtonScreenshotOk = -not [bool]$AutoOpenTitleSettingsMenu
$titleMenuOk = -not [bool]$AutoOpenTitleSettingsMenu
$titleMenuScreenshotOk = -not [bool]$AutoOpenTitleSettingsMenu
$officialModUiOk = -not [bool]$AutoOpenOfficialModUi
$animalViewerUiOk = -not [bool]$AutoOpenAnimalPanel
$actionSpeedToolOk = -not [bool]$AutoExerciseActionSpeedTool
$actionSpeedConfigApplyOk = -not [bool]$AutoExerciseActionSpeedConfigApply
$actionSpeedInteractionOk = -not [bool]$AutoExerciseActionSpeedInteraction
$oneActionResourceHitOk = -not [bool]$AutoExerciseOneActionResourceHit
$oneActionWrongToolOk = -not [bool]$AutoExerciseOneActionWrongTool
$oneActionFuelFeedOk = -not [bool]$AutoExerciseOneActionFuelFeed
$oneActionVegetationOk = -not [bool]$AutoExerciseOneActionVegetation
$autoFishingInputLogOk = -not [bool]$AutoPressAutoFishingHotkey
$autoFishingHotkeyOk = -not [bool]$AutoExerciseAutoFishingPhase
$autoFishingPhaseOk = -not [bool]$AutoExerciseAutoFishingPhase
$instantSaveOk = -not [bool]$AutoExerciseInstantSave

if ($startupOk) {
    $gameLaunchedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'GameLaunched dispatched.' -TimeoutSeconds 60 -AbortOnFatalInstanceWindow
    if ($gameLaunchedOk -and $AutoOpenTitleSettingsMenu) {
        $titleButtonOk = Wait-ForLogLine -LogPath $logPath -Pattern 'DTMAPI title settings button visible on HomePageUiState.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($titleButtonOk -and $AutoOpenTitleSettingsMenu) {
        $titleButtonScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Title settings button screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($titleButtonScreenshotOk -and $AutoOpenTitleSettingsMenu) {
        $titleMenuOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI title settings menu.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($titleMenuOk -and $AutoOpenTitleSettingsMenu) {
        $titleMenuScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Title settings menu screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($gameLaunchedOk -and $AutoOpenOfficialModUi) {
        $officialModUiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Official Mod UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($gameLaunchedOk -and $IncludeHookProbe) {
        $probeOk = Wait-ForLogLine -LogPath $logPath -Pattern 'HookProbe GameLaunched OK' -TimeoutSeconds 60 -AbortOnFatalInstanceWindow
    }
    if ($probeOk -and $IncludeHookProbe -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'HookProbe SaveLoaded OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseTitleButtonLifecycle -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoOpenAnimalPanel -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedTool -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedConfigApply -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseActionSpeedInteraction -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionResourceHit -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionWrongTool -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionFuelFeed -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseOneActionVegetation -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseAutoFishingPhase -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    elseif ($probeOk -and $AutoExerciseInstantSave -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoOpenAnimalPanel) {
        $animalViewerUiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Animal viewer UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedTool) {
        $actionSpeedToolOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedTool OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedConfigApply) {
        $actionSpeedConfigApplyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedConfigApply OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedInteraction) {
        $actionSpeedInteractionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedInteraction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionResourceHit) {
        $oneActionResourceHitOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionResourceHit OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionWrongTool) {
        $oneActionWrongToolOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionWrongTool OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionFuelFeed) {
        $oneActionFuelFeedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionFuelFeed OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseOneActionVegetation) {
        $oneActionVegetationOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise OneActionVegetation OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseTitleButtonLifecycle) {
        $titleLifecycleOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise TitleButtonLifecycle OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoPressAutoFishingHotkey) {
        for ($attempt = 1; $attempt -le 3 -and -not $autoFishingInputLogOk; $attempt++) {
            $sentF6 = Send-DolocTownF6
            if ($sentF6) {
                "SentExternalF6Attempt$attempt=$(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                $autoFishingInputLogOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Input F6 pressed dispatched to DTMAPI mods.' -TimeoutSeconds 8 -AbortOnFatalInstanceWindow
            }
            else {
                "SentExternalF6Attempt${attempt}Failed=$(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            }
        }
    }
    if ($saveLoadedOk -and $AutoExerciseAutoFishingPhase) {
        if (-not $AutoPressAutoFishingHotkey -or $autoFishingInputLogOk) {
            $autoFishingHotkeyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'AutoFishing automation enabled reason=hotkey F6' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $autoFishingPhaseOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingPhase OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        else {
            $autoFishingHotkeyOk = $false
            $autoFishingPhaseOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExerciseInstantSave) {
        $instantSaveOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise InstantSave OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
}

$processWaitSeconds = if ($startupOk) { $TimeoutSeconds } else { 30 }
$deadline = (Get-Date).AddSeconds($processWaitSeconds)
while ((Get-Date) -lt $deadline) {
    $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if (-not $proc) {
        break
    }
    Start-Sleep -Seconds 2
}

& "$PSScriptRoot\collect-logs.ps1" -CaseId 'GAME-SMOKE' -OutputDirectory $evidence | Tee-Object -FilePath (Join-Path $evidence 'collect-logs-output.txt')
Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
$fatalWindows = @(Get-FatalInstanceWindows)

$leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
$forcedClose = $false
if ($leftover) {
    $null = $leftover.CloseMainWindow()
    Start-Sleep -Seconds 10
    $leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if ($leftover) {
        $leftover | Stop-Process -Force
        $forcedClose = $true
        Start-Sleep -Seconds 2
        $leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    }
}
@{
    Enabled = $false
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dtmapiDir 'smoke-settings.json')
if ($usesOneActionConfigSmoke) {
    if ($oneActionConfigHadOriginal) {
        Copy-Item -Force -LiteralPath $oneActionConfigBackup -Destination $oneActionConfigPath
    }
    else {
        Remove-Item -Force -LiteralPath $oneActionConfigPath -ErrorAction SilentlyContinue
    }
}
if ($AutoExerciseAutoFishingPhase) {
    if ($autoFishingConfigHadOriginal) {
        Copy-Item -Force -LiteralPath $autoFishingConfigBackup -Destination $autoFishingConfigPath
    }
    else {
        Remove-Item -Force -LiteralPath $autoFishingConfigPath -ErrorAction SilentlyContinue
    }
}
if ($usesActionSpeedConfigSmoke) {
    if ($actionSpeedConfigHadOriginal) {
        Copy-Item -Force -LiteralPath $actionSpeedConfigBackup -Destination $actionSpeedConfigPath
    }
    else {
        Remove-Item -Force -LiteralPath $actionSpeedConfigPath -ErrorAction SilentlyContinue
    }
}
$titleButtonScreenshotFileOk = -not [bool]$AutoOpenTitleSettingsMenu
$titleMenuScreenshotFileOk = -not [bool]$AutoOpenTitleSettingsMenu
$officialModUiScreenshotFileOk = -not [bool]$AutoOpenOfficialModUi
if ($AutoOpenTitleSettingsMenu -and (Test-Path $logPath)) {
    $buttonScreenshotLine = Select-String -Path $logPath -Pattern 'Title settings button screenshot OK screenshot=' | Select-Object -Last 1
    if ($buttonScreenshotLine) {
        $buttonScreenshotPath = $buttonScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
        $titleButtonScreenshotFileOk = Test-Path -LiteralPath $buttonScreenshotPath
    }
    $menuScreenshotLine = Select-String -Path $logPath -Pattern 'Title settings menu screenshot OK screenshot=' | Select-Object -Last 1
    if ($menuScreenshotLine) {
        $menuScreenshotPath = $menuScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
        $titleMenuScreenshotFileOk = Test-Path -LiteralPath $menuScreenshotPath
    }
}
if ($AutoOpenOfficialModUi -and (Test-Path $logPath)) {
    $officialScreenshotLine = Select-String -Path $logPath -Pattern 'Official Mod UI evidence OK .*screenshot=' | Select-Object -Last 1
    if ($officialScreenshotLine) {
        $officialScreenshotPath = $officialScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
        $officialModUiScreenshotFileOk = Test-Path -LiteralPath $officialScreenshotPath
    }
}
$result = @{
    StartupLog = $startupOk
    GameLaunched = $gameLaunchedOk
    HookProbe = $probeOk
    SaveLoaded = $saveLoadedOk
    TitleSettingsButton = $titleButtonOk
    TitleSettingsButtonScreenshot = $titleButtonScreenshotOk
    TitleSettingsButtonScreenshotFile = $titleButtonScreenshotFileOk
    TitleButtonLifecycle = $titleLifecycleOk
    TitleSettingsMenu = $titleMenuOk
    TitleSettingsMenuScreenshot = $titleMenuScreenshotOk
    TitleSettingsMenuScreenshotFile = $titleMenuScreenshotFileOk
    OfficialModUi = $officialModUiOk
    OfficialModUiScreenshotFile = $officialModUiScreenshotFileOk
    AnimalViewerUi = $animalViewerUiOk
    ActionSpeedTool = $actionSpeedToolOk
    ActionSpeedConfigApply = $actionSpeedConfigApplyOk
    ActionSpeedInteraction = $actionSpeedInteractionOk
    OneActionResourceHit = $oneActionResourceHitOk
    OneActionWrongTool = $oneActionWrongToolOk
    OneActionFuelFeed = $oneActionFuelFeedOk
    OneActionVegetation = $oneActionVegetationOk
    AutoFishingInputLog = $autoFishingInputLogOk
    AutoFishingHotkey = $autoFishingHotkeyOk
    AutoFishingPhase = $autoFishingPhaseOk
    InstantSave = $instantSaveOk
    NoFatalInstanceWindow = $fatalWindows.Count -eq 0
    ProcessExited = -not [bool]$leftover
    ForcedClose = $forcedClose
    Completed = Get-Date -Format o
} | ConvertTo-Json
$result | Set-Content -LiteralPath (Join-Path $evidence 'result.json')
try {
    & "$PSScriptRoot\analyze-startup-evidence.ps1" -EvidencePath $evidence -OutputDirectory $evidence -Quiet
}
catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $evidence 'startup-analysis-error.txt')
}

if (-not $startupOk -or -not $gameLaunchedOk -or -not $probeOk -or -not $saveLoadedOk -or -not $titleButtonOk -or -not $titleButtonScreenshotOk -or -not $titleButtonScreenshotFileOk -or -not $titleLifecycleOk -or -not $titleMenuOk -or -not $titleMenuScreenshotOk -or -not $titleMenuScreenshotFileOk -or -not $officialModUiOk -or -not $officialModUiScreenshotFileOk -or -not $animalViewerUiOk -or -not $actionSpeedToolOk -or -not $actionSpeedConfigApplyOk -or -not $actionSpeedInteractionOk -or -not $oneActionResourceHitOk -or -not $oneActionWrongToolOk -or -not $oneActionFuelFeedOk -or -not $oneActionVegetationOk -or -not $autoFishingInputLogOk -or -not $autoFishingHotkeyOk -or -not $autoFishingPhaseOk -or -not $instantSaveOk -or $fatalWindows.Count -gt 0 -or $forcedClose -or $leftover) {
    Write-Error "Game smoke failed or left DolocTown.exe running. Evidence: $evidence"
    exit 1
}

Write-Host "Game smoke passed. Evidence: $evidence"
