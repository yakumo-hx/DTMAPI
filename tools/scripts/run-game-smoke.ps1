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
    [switch] $AutoExerciseAutoFishingMiniGameComplete,
    [switch] $AutoExerciseTitleButtonLifecycle,
    [switch] $AutoExerciseInstantSave,
    [int] $AutoExerciseInstantSaveDelaySeconds = 3,
    [switch] $AutoPressAutoFishingHotkey,
    [switch] $AutoOpenTitleSettingsMenu,
    [switch] $AutoOpenTitleSettingsStatusPage,
    [switch] $AutoOpenTitleSettingsManagerMvp,
    [switch] $AutoOpenOfficialModUi,
    [switch] $AutoOpenAnimalPanel,
    [switch] $AutoExerciseDebugConsole,
    [switch] $AutoExerciseDebugConsoleMouseGive,
    [switch] $AutoExerciseDebugInventory,
    [switch] $AutoExerciseDebugWeather,
    [switch] $AutoExerciseDebugTeleport,
    [switch] $AutoExerciseDebugTime,
    [switch] $AutoExerciseDebugMovement,
    [switch] $AutoExerciseAdvancedDebug,
    [switch] $AutoExerciseVehicle,
    [switch] $AutoExerciseNewContentApis,
    [switch] $AutoExerciseMineContentApis,
    [switch] $AutoExerciseZoom,
    [switch] $AutoExerciseChestLocatorEnhancer,
    [switch] $AutoExerciseMoreSavesOfficialSaveUi,
    [switch] $AutoExerciseStrongPlantingGun,
    [switch] $AutoExerciseCropHarvestingApi,
    [switch] $AutoExerciseCustomEntityApis,
    [switch] $DisableSecondMotorForSmoke,
    [int] $AutoExitAfterSecondsOverride = 0,
    [switch] $SkipBuild
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

function Get-DolocTownPersistentRootForSmoke {
    if ($env:DTMAPI_DOLOC_PERSISTENT_ROOT) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_DOLOC_PERSISTENT_ROOT)
    }

    return Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\RedSawGames\DolocTown'
}

function Write-SmokeJsonObject {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $json = $Value | ConvertTo-Json -Depth 10
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Get-SmokeStatus {
    param(
        [bool] $Requested,
        [bool] $Passed,
        [bool] $Blocked = $false
    )

    if ($Blocked) {
        return 'Blocked'
    }
    if (-not $Requested) {
        return 'Skipped'
    }
    if ($Passed) {
        return 'Passed'
    }
    return 'Failed'
}

function Get-SmokeAlwaysStatus {
    param(
        [bool] $Passed,
        [bool] $Blocked = $false
    )

    return Get-SmokeStatus -Requested $true -Passed $Passed -Blocked $Blocked
}

function Write-SmokeBlockedResult {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Reason,
        [string] $RunStatus = 'Blocked'
    )

    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'result.json') -Value @{
        SchemaVersion = 2
        RunStatus = $RunStatus
        RunStatusReason = $Reason
        StartupLog = 'Blocked'
        GameLaunched = 'Blocked'
        HookProbe = 'Skipped'
        SaveLoaded = 'Skipped'
        NoFatalInstanceWindow = if ($Reason -match 'Fatal') { 'Failed' } else { 'Passed' }
        ProcessExited = if ($Reason -match 'DolocTown\.exe') { 'Failed' } else { 'Skipped' }
        ForcedClose = 'Skipped'
        Completed = Get-Date -Format o
    }
}

function Set-SmokeOfficialLocalModEnabled {
    param(
        [Parameter(Mandatory = $true)] [string] $OfficialFolder,
        [Parameter(Mandatory = $true)] [bool] $Enabled,
        [Parameter(Mandatory = $true)] [string] $BackupPath,
        [Parameter(Mandatory = $true)] [ref] $HadOriginal
    )

    $persistentRoot = Get-DolocTownPersistentRootForSmoke
    $enablementPath = Join-Path $persistentRoot 'SAVE\mod_infos.json'
    if (-not (Test-Path $enablementPath)) {
        $HadOriginal.Value = $false
        return $enablementPath
    }

    Copy-Item -Force -LiteralPath $enablementPath -Destination $BackupPath
    $HadOriginal.Value = $true
    $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $enablementPath | ConvertFrom-Json
    if ($null -eq $data.modInfos) {
        return $enablementPath
    }

    $id = "Local.$OfficialFolder"
    $property = $data.modInfos.PSObject.Properties[$id]
    if ($property) {
        if ($property.Value.PSObject.Properties['enabled']) {
            $property.Value.enabled = $Enabled
        }
        else {
            $property.Value | Add-Member -MemberType NoteProperty -Name 'enabled' -Value $Enabled -Force
        }
        Write-SmokeJsonObject -Path $enablementPath -Value $data
    }

    return $enablementPath
}

function Restore-SmokeSecondMotorEnablement {
    if ($script:secondMotorEnablementTouched -and $script:secondMotorEnablementHadOriginal -and $script:secondMotorEnablementPath) {
        Copy-Item -Force -LiteralPath $script:secondMotorEnablementBackup -Destination $script:secondMotorEnablementPath -ErrorAction SilentlyContinue
    }
}

$script:secondMotorEnablementBackup = $null
$script:secondMotorEnablementPath = $null
$script:secondMotorEnablementHadOriginal = $false
$script:secondMotorEnablementTouched = $false

trap {
    Restore-SmokeSecondMotorEnablement
    throw
}

$launchViaSteam = [bool]$UseSteam -or -not [bool]$DirectExe
$existingGameProcess = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
if ($existingGameProcess) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'DolocTown.exe already running before smoke launch.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-SmokeBlockedResult -EvidencePath $evidence -Reason $blockedReason -RunStatus 'Blocked'
    Write-Error "DolocTown.exe is already running. Close the existing game/window before launching smoke. Evidence: $evidence"
    exit 1
}
$existingFatalWindow = Test-FatalInstanceWindow
if ($existingFatalWindow) {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
    $blockedReason = 'Fatal instance popup already visible before smoke launch.'
    "Started=$(Get-Date -Format o)`nBlocked=$blockedReason" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
    Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
    Write-SmokeBlockedResult -EvidencePath $evidence -Reason $blockedReason -RunStatus 'Aborted'
    Write-Error "Fatal instance popup is already visible. Close the dialog before launching smoke. Evidence: $evidence"
    exit 1
}

$managerStatusRequested = [bool]$AutoOpenTitleSettingsStatusPage -or [bool]$AutoOpenTitleSettingsManagerMvp
$managerMvpRequested = [bool]$AutoOpenTitleSettingsManagerMvp
$titleSettingsRequested = [bool]$AutoOpenTitleSettingsMenu -or $managerStatusRequested
$includeRuntimeTestMods = [bool]$IncludeHookProbe -or $titleSettingsRequested
$includeDebugConsoleMod = [bool]$AutoExerciseDebugConsole -or [bool]$AutoExerciseDebugConsoleMouseGive -or [bool]$AutoExerciseAdvancedDebug
$requiresDebugConsoleKeySmoke = [bool]$AutoExerciseDebugConsole -or [bool]$AutoExerciseDebugConsoleMouseGive
& "$PSScriptRoot\install-to-game.ps1" -IncludeTestMods:$includeRuntimeTestMods -IncludeDebugConsoleMod:$includeDebugConsoleMod -IncludeHookProbe:$IncludeHookProbe -SkipBuild:$SkipBuild
$installExit = if (Get-Variable LASTEXITCODE -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
if ($installExit -ne 0) {
    exit $installExit
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
New-Item -ItemType Directory -Force -Path $dtmapiDir | Out-Null
$evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'GAME-SMOKE'
$script:secondMotorEnablementBackup = Join-Path $evidence 'mod_infos.before-second-motor-smoke.json'
$script:secondMotorEnablementPath = $null
$script:secondMotorEnablementHadOriginal = $false
$script:secondMotorEnablementTouched = $false
if ($DisableSecondMotorForSmoke) {
    $script:secondMotorEnablementPath = Set-SmokeOfficialLocalModEnabled -OfficialFolder 'DTMAPI_SecondMotor' -Enabled:$false -BackupPath $script:secondMotorEnablementBackup -HadOriginal ([ref]$script:secondMotorEnablementHadOriginal)
    $script:secondMotorEnablementTouched = $true
}
elseif ($AutoExerciseVehicle) {
    $script:secondMotorEnablementPath = Set-SmokeOfficialLocalModEnabled -OfficialFolder 'DTMAPI_SecondMotor' -Enabled:$true -BackupPath $script:secondMotorEnablementBackup -HadOriginal ([ref]$script:secondMotorEnablementHadOriginal)
    $script:secondMotorEnablementTouched = $true
}
$oneActionConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.OneActionComplete.json'
$oneActionConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.OneActionComplete.before.json'
$oneActionConfigHadOriginal = $false
$usesOneActionConfigSmoke = [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation
$actionSpeedConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.ActionSpeed.json'
$actionSpeedConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.ActionSpeed.before.json'
$actionSpeedConfigHadOriginal = $false
$usesActionSpeedConfigSmoke = [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoOpenTitleSettingsMenu
$autoFishingConfigPath = Join-Path $dtmapiDir 'config\Yuuka.DTMAPI.AutoFishing.json'
$autoFishingConfigBackup = Join-Path $evidence 'Yuuka.DTMAPI.AutoFishing.before.json'
$autoFishingConfigHadOriginal = $false
$usesAutoFishingConfigSmoke = [bool]$AutoExerciseAutoFishingPhase -or [bool]$AutoOpenTitleSettingsMenu
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
    $actionSpeedInteractionEnabled = [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoOpenTitleSettingsMenu
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
        AutoFillStrong = $actionSpeedInteractionEnabled
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
if ($usesAutoFishingConfigSmoke) {
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
        AutoCompleteMiniGame = $true
        SkipMiniGame = -not [bool]$AutoExerciseAutoFishingMiniGameComplete
        InstantBite = $true
        FastAnimations = $true
        FastAnimationMultiplier = 3
        VerboseLogging = $true
    } | ConvertTo-Json | Set-Content -LiteralPath $autoFishingConfigPath
}
function Initialize-DtmApiSmokeInput {
    if (-not ('DtmApiSmokeInput' -as [type])) {
        Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Threading;

public struct DtmApiSmokeRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public struct DtmApiSmokePoint
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct DtmApiSmokeMouseInput
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct DtmApiSmokeInputEvent
{
    public uint type;
    public DtmApiSmokeMouseInput mi;
}

public static class DtmApiSmokeInput
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, UIntPtr lParam);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, ref DtmApiSmokeRect lpRect);

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref DtmApiSmokePoint lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, DtmApiSmokeInputEvent[] pInputs, int cbSize);

    public static bool SendMouseButton(uint downFlag, uint upFlag, int holdMilliseconds)
    {
        var down = new DtmApiSmokeInputEvent
        {
            type = 0,
            mi = new DtmApiSmokeMouseInput { dwFlags = downFlag }
        };
        var up = new DtmApiSmokeInputEvent
        {
            type = 0,
            mi = new DtmApiSmokeMouseInput { dwFlags = upFlag }
        };
        int size = Marshal.SizeOf(typeof(DtmApiSmokeInputEvent));
        uint downCount = SendInput(1, new[] { down }, size);
        Thread.Sleep(Math.Max(40, holdMilliseconds));
        uint upCount = SendInput(1, new[] { up }, size);
        return downCount == 1 && upCount == 1;
    }
}
"@
    }
}

function Get-DolocTownInputProcess {
    param(
        [int] $TimeoutSeconds = 20
    )

    Initialize-DtmApiSmokeInput
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($proc) {
            $targetPid = 0
            $targetThread = [DtmApiSmokeInput]::GetWindowThreadProcessId($proc.MainWindowHandle, [ref]$targetPid)
            $foreground = [DtmApiSmokeInput]::GetForegroundWindow()
            $foregroundPid = 0
            $foregroundThread = if ($foreground -ne [IntPtr]::Zero) { [DtmApiSmokeInput]::GetWindowThreadProcessId($foreground, [ref]$foregroundPid) } else { 0 }
            $currentThread = [DtmApiSmokeInput]::GetCurrentThreadId()

            if ($targetThread -ne 0) {
                [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $targetThread, $true)
            }
            if ($foregroundThread -ne 0) {
                [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $foregroundThread, $true)
            }
            try {
                [void][DtmApiSmokeInput]::ShowWindow($proc.MainWindowHandle, 9)
                [void][DtmApiSmokeInput]::BringWindowToTop($proc.MainWindowHandle)
                [DtmApiSmokeInput]::SwitchToThisWindow($proc.MainWindowHandle, $true)
                [void][DtmApiSmokeInput]::SetForegroundWindow($proc.MainWindowHandle)
                [void][DtmApiSmokeInput]::SetActiveWindow($proc.MainWindowHandle)
                [void][DtmApiSmokeInput]::SetFocus($proc.MainWindowHandle)
            }
            finally {
                if ($foregroundThread -ne 0) {
                    [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $foregroundThread, $false)
                }
                if ($targetThread -ne 0) {
                    [void][DtmApiSmokeInput]::AttachThreadInput($currentThread, $targetThread, $false)
                }
            }

            Start-Sleep -Milliseconds 800
            $foreground = [DtmApiSmokeInput]::GetForegroundWindow()
            $foregroundPid = 0
            if ($foreground -ne [IntPtr]::Zero) {
                [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foreground, [ref]$foregroundPid)
            }
            if ($foregroundPid -ne $proc.Id) {
                Start-Sleep -Milliseconds 250
                continue
            }

            return $proc
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    return $null
}

function Send-DolocTownKey {
    param(
        [Parameter(Mandatory = $true)] [int] $VirtualKey,
        [string] $Name = 'key',
        [int] $TimeoutSeconds = 20,
        [int] $HoldMilliseconds = 260
    )

    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds
    if (-not $proc) {
        return $false
    }

    $keyParam = [UIntPtr]::new([uint64]$VirtualKey)
    [DtmApiSmokeInput]::keybd_event([byte]$VirtualKey, 0, 0, [UIntPtr]::Zero)
    [void][DtmApiSmokeInput]::PostMessage($proc.MainWindowHandle, 0x0100, $keyParam, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds ([Math]::Max(40, $HoldMilliseconds))
    [DtmApiSmokeInput]::keybd_event([byte]$VirtualKey, 0, 0x0002, [UIntPtr]::Zero)
    [void][DtmApiSmokeInput]::PostMessage($proc.MainWindowHandle, 0x0101, $keyParam, [UIntPtr]::Zero)
    return $true
}

function Send-DolocTownMouseClick {
    param(
        [ValidateSet('Left', 'Right')] [string] $Button = 'Left',
        [Parameter(Mandatory = $true)] [int] $ClientX,
        [Parameter(Mandatory = $true)] [int] $ClientY,
        [int] $TimeoutSeconds = 20
    )

    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds
    if (-not $proc) {
        return $false
    }

    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return $false
    }

    $clientWidth = $rect.Right - $rect.Left
    $clientHeight = $rect.Bottom - $rect.Top
    if ($ClientX -lt 0 -or $ClientY -lt 0 -or $ClientX -ge $clientWidth -or $ClientY -ge $clientHeight) {
        return $false
    }

    $point = New-Object DtmApiSmokePoint
    $point.X = $ClientX
    $point.Y = $ClientY
    if (-not [DtmApiSmokeInput]::ClientToScreen($proc.MainWindowHandle, [ref]$point)) {
        return $false
    }

    [void][DtmApiSmokeInput]::SetCursorPos($point.X, $point.Y)
    Start-Sleep -Milliseconds 160
    if ($Button -eq 'Right') {
        return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0008, [uint32]0x0010, 320)
    }
    else {
        return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0002, [uint32]0x0004, 180)
    }
}

function Send-DolocTownF6 {
    param(
        [int] $TimeoutSeconds = 20
    )

    return Send-DolocTownKey -VirtualKey 0x75 -Name 'F6' -TimeoutSeconds $TimeoutSeconds
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

function Wait-ForLogLineCount {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $MinimumCount,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $LogPath) {
            $count = @(Select-String -LiteralPath $LogPath -SimpleMatch -Pattern $Pattern -ErrorAction SilentlyContinue).Count
            if ($count -ge $MinimumCount) {
                return $true
            }
        }

        if (Test-FatalInstanceWindow) {
            return $false
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Get-LogRegexCount {
    param(
        [string] $LogPath,
        [string] $Pattern
    )

    if (-not (Test-Path $LogPath)) {
        return 0
    }

    return @(Select-String -LiteralPath $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue).Count
}

function Wait-ForLogRegexCount {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $MinimumCount,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Get-LogRegexCount -LogPath $LogPath -Pattern $Pattern) -ge $MinimumCount) {
            return $true
        }

        if (Test-FatalInstanceWindow) {
            return $false
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Get-DebugConsoleFirstItemCellPoint {
    $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $proc) {
        return $null
    }

    Initialize-DtmApiSmokeInput
    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return $null
    }

    $clientWidth = $rect.Right - $rect.Left
    $clientHeight = $rect.Bottom - $rect.Top
    $panelWidth = 1500
    $panelHeight = 900
    $itemCenterX = 388 + 50
    $itemCenterY = 150 + 48
    $clientX = [int][Math]::Round((($clientWidth - $panelWidth) / 2.0) + $itemCenterX)
    $clientY = [int][Math]::Round((($clientHeight - $panelHeight) / 2.0) + $itemCenterY)

    return [PSCustomObject]@{
        ClientX = $clientX
        ClientY = $clientY
        ClientWidth = $clientWidth
        ClientHeight = $clientHeight
        PanelWidth = $panelWidth
        PanelHeight = $panelHeight
    }
}

function Invoke-DebugConsoleSmokeKey {
    param(
        [string] $Label,
        [int] $VirtualKey,
        [string] $Pattern,
        [int] $MinimumCount,
        [int] $Attempts = 3,
        [int] $WaitSeconds = 5
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $sent = Send-DolocTownKey -VirtualKey $VirtualKey -Name $Label
        "SentExternalDebugConsole${Label}Attempt$attempt=$(Get-Date -Format o);ok=$sent" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        if ($sent -and (Wait-ForLogLineCount -LogPath $logPath -Pattern $Pattern -MinimumCount $MinimumCount -TimeoutSeconds $WaitSeconds)) {
            return $true
        }
    }

    return $false
}

function Invoke-DebugConsoleMouseGiveSmoke {
    $point = Get-DebugConsoleFirstItemCellPoint
    if (-not $point) {
        "DebugConsoleMouseGivePoint=$(Get-Date -Format o);ok=False;reason=no-window-or-client" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        return $false
    }

    $leftPattern = 'Inventory debug give owner=DTMAPI\.DebugConsoleMod .* requested=1 .* success=True'
    $rightPattern = 'Inventory debug give owner=DTMAPI\.DebugConsoleMod .* requested=10 .* success=True'
    $leftBefore = Get-LogRegexCount -LogPath $logPath -Pattern $leftPattern
    $rightBefore = Get-LogRegexCount -LogPath $logPath -Pattern $rightPattern

    "DebugConsoleMouseGivePoint=$(Get-Date -Format o);clientX=$($point.ClientX);clientY=$($point.ClientY);clientWidth=$($point.ClientWidth);clientHeight=$($point.ClientHeight);panelWidth=$($point.PanelWidth);panelHeight=$($point.PanelHeight)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
    Start-Sleep -Milliseconds 1200

    $leftSent = Send-DolocTownMouseClick -Button Left -ClientX $point.ClientX -ClientY $point.ClientY
    $leftOk = $leftSent -and (Wait-ForLogRegexCount -LogPath $logPath -Pattern $leftPattern -MinimumCount ($leftBefore + 1) -TimeoutSeconds 8)
    "DebugConsoleMouseGiveLeft=$(Get-Date -Format o);sent=$leftSent;before=$leftBefore;ok=$leftOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

    Start-Sleep -Milliseconds 900
    $rightSent = Send-DolocTownMouseClick -Button Right -ClientX $point.ClientX -ClientY $point.ClientY
    $rightOk = $rightSent -and (Wait-ForLogRegexCount -LogPath $logPath -Pattern $rightPattern -MinimumCount ($rightBefore + 1) -TimeoutSeconds 8)
    "DebugConsoleMouseGiveRight=$(Get-Date -Format o);sent=$rightSent;before=$rightBefore;ok=$rightOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

    return ($leftOk -and $rightOk)
}

$autoExitAfterSeconds = if ($AutoExitAfterSecondsOverride -gt 0) {
    $AutoExitAfterSecondsOverride
}
elseif (($titleSettingsRequested -or $AutoOpenOfficialModUi) -and $SaveSlot -le 0) {
    [Math]::Max(25, $TimeoutSeconds - 20)
}
else {
    [Math]::Max(90, $TimeoutSeconds - 30)
}
$autoLoadDelaySeconds = if ($titleSettingsRequested) { 30 } else { 8 }
$smokeAutoLoadSaveSlot = if ($managerStatusRequested) { 0 } else { $SaveSlot }
$smokeSettings = @{
    Enabled = $true
    AutoLoadSaveSlot = $smokeAutoLoadSaveSlot
    AutoLoadDelaySeconds = $autoLoadDelaySeconds
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
    AutoExerciseAutoFishingMiniGameComplete = [bool]$AutoExerciseAutoFishingMiniGameComplete
    AutoExerciseTitleButtonLifecycle = [bool]$AutoExerciseTitleButtonLifecycle
    AutoExerciseInstantSave = [bool]$AutoExerciseInstantSave
    AutoExerciseInstantSaveDelaySeconds = $AutoExerciseInstantSaveDelaySeconds
    AutoExerciseDebugConsole = $requiresDebugConsoleKeySmoke
    AutoExerciseDebugConsoleMouseGive = [bool]$AutoExerciseDebugConsoleMouseGive
    AutoExerciseDebugConsoleDelaySeconds = 60
    AutoExerciseDebugInventory = [bool]$AutoExerciseDebugInventory
    AutoExerciseDebugInventoryDelaySeconds = 3
    AutoExerciseDebugWeather = [bool]$AutoExerciseDebugWeather
    AutoExerciseDebugWeatherDelaySeconds = 4
    AutoExerciseDebugTeleport = [bool]$AutoExerciseDebugTeleport
    AutoExerciseDebugTeleportDelaySeconds = 5
    AutoExerciseDebugTime = [bool]$AutoExerciseDebugTime
    AutoExerciseDebugTimeDelaySeconds = 6
    AutoExerciseDebugMovement = [bool]$AutoExerciseDebugMovement
    AutoExerciseDebugMovementDelaySeconds = 7
    AutoExerciseAdvancedDebug = [bool]$AutoExerciseAdvancedDebug
    AutoExerciseAdvancedDebugDelaySeconds = 8
    AutoExerciseVehicle = [bool]$AutoExerciseVehicle
    AutoExerciseVehicleDelaySeconds = 8
    AutoExerciseNewContentApis = [bool]$AutoExerciseNewContentApis
    AutoExerciseNewContentApisDelaySeconds = 3
    AutoExerciseMineContentApis = [bool]$AutoExerciseMineContentApis
    AutoExerciseMineContentApisDelaySeconds = 3
    AutoExerciseZoom = [bool]$AutoExerciseZoom
    AutoExerciseZoomDelaySeconds = 3
    AutoExerciseChestLocatorEnhancer = [bool]$AutoExerciseChestLocatorEnhancer
    AutoExerciseChestLocatorEnhancerDelaySeconds = 3
    AutoExerciseMoreSavesOfficialSaveUi = [bool]$AutoExerciseMoreSavesOfficialSaveUi
    AutoExerciseStrongPlantingGun = [bool]$AutoExerciseStrongPlantingGun
    AutoExerciseStrongPlantingGunDelaySeconds = 3
    AutoExerciseCropHarvestingApi = [bool]$AutoExerciseCropHarvestingApi
    AutoExerciseCropHarvestingApiDelaySeconds = 3
    AutoExerciseCustomEntityApis = [bool]$AutoExerciseCustomEntityApis
    AutoExerciseCustomEntityApisDelaySeconds = 3
    AutoFishingExternalHotkeyRequired = [bool]$AutoPressAutoFishingHotkey
    AutoOpenTitleSettingsMenu = [bool]$AutoOpenTitleSettingsMenu
    AutoOpenTitleSettingsStatusPage = [bool]$AutoOpenTitleSettingsStatusPage
    AutoOpenTitleSettingsManagerMvp = [bool]$AutoOpenTitleSettingsManagerMvp
    AutoOpenTitleSettingsDelaySeconds = 12
    AutoOpenOfficialModUi = [bool]$AutoOpenOfficialModUi
    AutoOpenOfficialModUiDelaySeconds = 12
    AutoOpenAnimalPanel = [bool]$AutoOpenAnimalPanel
    AutoOpenAnimalPanelDelaySeconds = 2
} | ConvertTo-Json
$smokeSettings | Set-Content -LiteralPath (Join-Path $dtmapiDir 'smoke-settings.json')
$freshLogPath = Join-Path $dtmapiDir 'logs\latest.log'
$freshBepLogPath = Join-Path $gameDir 'BepInEx\LogOutput.log'
if (Test-Path $freshLogPath) {
    Remove-Item -Force -LiteralPath $freshLogPath
}
if (Test-Path $freshBepLogPath) {
    Remove-Item -Force -LiteralPath $freshBepLogPath
}

"Started=$(Get-Date -Format o)`nGameDir=$gameDir`nDtmApiStateDir=$dtmapiDir`nSaveSlot=$SaveSlot`nIncludeHookProbe=$IncludeHookProbe`nLaunchMode=$(if ($launchViaSteam) { 'Steam' } else { 'DirectExe' })`nDisableSecondMotorForSmoke=$DisableSecondMotorForSmoke`nAutoSaveAfterLoad=$AutoSaveAfterLoad`nAutoReloadMods=$AutoReloadMods`nAutoExerciseExperimentalHooks=$AutoExerciseExperimentalHooks`nAutoExerciseActionSpeedTool=$AutoExerciseActionSpeedTool`nAutoExerciseActionSpeedConfigApply=$AutoExerciseActionSpeedConfigApply`nAutoExerciseActionSpeedInteraction=$AutoExerciseActionSpeedInteraction`nAutoExerciseOneActionResourceHit=$AutoExerciseOneActionResourceHit`nAutoExerciseOneActionWrongTool=$AutoExerciseOneActionWrongTool`nAutoExerciseOneActionFuelFeed=$AutoExerciseOneActionFuelFeed`nAutoExerciseOneActionVegetation=$AutoExerciseOneActionVegetation`nAutoExerciseAutoFishingPhase=$AutoExerciseAutoFishingPhase`nAutoExerciseAutoFishingMiniGameComplete=$AutoExerciseAutoFishingMiniGameComplete`nAutoExerciseTitleButtonLifecycle=$AutoExerciseTitleButtonLifecycle`nAutoExerciseInstantSave=$AutoExerciseInstantSave`nAutoExerciseInstantSaveDelaySeconds=$AutoExerciseInstantSaveDelaySeconds`nAutoExerciseDebugConsole=$AutoExerciseDebugConsole`nAutoExerciseDebugConsoleMouseGive=$AutoExerciseDebugConsoleMouseGive`nAutoExerciseDebugInventory=$AutoExerciseDebugInventory`nAutoExerciseDebugWeather=$AutoExerciseDebugWeather`nAutoExerciseDebugTeleport=$AutoExerciseDebugTeleport`nAutoExerciseDebugTime=$AutoExerciseDebugTime`nAutoExerciseDebugMovement=$AutoExerciseDebugMovement`nAutoExerciseAdvancedDebug=$AutoExerciseAdvancedDebug`nAutoExerciseVehicle=$AutoExerciseVehicle`nAutoExerciseNewContentApis=$AutoExerciseNewContentApis`nAutoExerciseMineContentApis=$AutoExerciseMineContentApis`nAutoExerciseZoom=$AutoExerciseZoom`nAutoExerciseChestLocatorEnhancer=$AutoExerciseChestLocatorEnhancer`nAutoExerciseMoreSavesOfficialSaveUi=$AutoExerciseMoreSavesOfficialSaveUi`nAutoExerciseStrongPlantingGun=$AutoExerciseStrongPlantingGun`nAutoExerciseCropHarvestingApi=$AutoExerciseCropHarvestingApi`nAutoExerciseCustomEntityApis=$AutoExerciseCustomEntityApis`nAutoPressAutoFishingHotkey=$AutoPressAutoFishingHotkey`nAutoOpenTitleSettingsMenu=$AutoOpenTitleSettingsMenu`nAutoOpenTitleSettingsStatusPage=$AutoOpenTitleSettingsStatusPage`nAutoOpenTitleSettingsManagerMvp=$AutoOpenTitleSettingsManagerMvp`nAutoOpenOfficialModUi=$AutoOpenOfficialModUi`nAutoOpenAnimalPanel=$AutoOpenAnimalPanel`nAutoExitAfterSeconds=$autoExitAfterSeconds" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$launchCommandStartedAt = Get-Date
if ($launchViaSteam) {
    Start-Process 'steam://rungameid/2285550'
}
else {
    $exe = Join-Path $gameDir 'DolocTown.exe'
    if (-not (Test-Path $exe)) {
        throw "DolocTown.exe not found at $exe"
    }
    $oldSteamAppId = $env:SteamAppId
    $oldSteamGameId = $env:SteamGameId
    try {
        $env:SteamAppId = '2285550'
        $env:SteamGameId = '2285550'
        "DirectExeSteamEnv=SteamAppId=2285550;SteamGameId=2285550" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        Start-Process -FilePath $exe -WorkingDirectory $gameDir
    }
    finally {
        if ($null -eq $oldSteamAppId) { Remove-Item Env:\SteamAppId -ErrorAction SilentlyContinue } else { $env:SteamAppId = $oldSteamAppId }
        if ($null -eq $oldSteamGameId) { Remove-Item Env:\SteamGameId -ErrorAction SilentlyContinue } else { $env:SteamGameId = $oldSteamGameId }
    }
}
$launchCommandFinishedAt = Get-Date
"LaunchCommandStarted=$($launchCommandStartedAt.ToString('o'))`nLaunchCommandFinished=$($launchCommandFinishedAt.ToString('o'))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$logPath = $freshLogPath
$startupTimeoutSeconds = $TimeoutSeconds
$launchModeLabel = if ($launchViaSteam) { 'Steam' } else { 'DirectExe' }
$startupOk = Wait-ForStartupLogWithTimeline -LogPath $logPath -Pattern 'DTMAPI runtime starting.' -TimeoutSeconds $startupTimeoutSeconds -EvidenceDir $evidence -LaunchCommandStartedAt $launchCommandStartedAt -LaunchCommandFinishedAt $launchCommandFinishedAt -LaunchMode $launchModeLabel
$gameLaunchedOk = $false
$saveLoadedRequested = (($SaveSlot -gt 0) -and ([bool]$IncludeHookProbe -or [bool]$AutoOpenAnimalPanel -or [bool]$AutoExerciseExperimentalHooks -or [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation -or [bool]$AutoExerciseAutoFishingPhase -or [bool]$AutoExerciseTitleButtonLifecycle -or [bool]$AutoExerciseInstantSave -or $requiresDebugConsoleKeySmoke -or [bool]$AutoExerciseDebugInventory -or [bool]$AutoExerciseDebugWeather -or [bool]$AutoExerciseDebugTeleport -or [bool]$AutoExerciseDebugTime -or [bool]$AutoExerciseDebugMovement -or [bool]$AutoExerciseAdvancedDebug -or [bool]$AutoExerciseVehicle -or [bool]$AutoExerciseNewContentApis -or [bool]$AutoExerciseMineContentApis -or [bool]$AutoExerciseZoom -or [bool]$AutoExerciseChestLocatorEnhancer -or [bool]$AutoExerciseMoreSavesOfficialSaveUi -or [bool]$AutoExerciseStrongPlantingGun -or [bool]$AutoExerciseCropHarvestingApi -or [bool]$AutoExerciseCustomEntityApis))
$probeOk = -not [bool]$IncludeHookProbe
$saveLoadedOk = -not $saveLoadedRequested
$titleLifecycleOk = -not [bool]$AutoExerciseTitleButtonLifecycle
$titleButtonOk = -not $titleSettingsRequested
$titleButtonScreenshotOk = -not $titleSettingsRequested
$titleMenuOk = -not $titleSettingsRequested
$titleMenuScreenshotOk = -not $titleSettingsRequested
$managerStatusPageOk = -not $managerStatusRequested
$managerStatusPageScreenshotOk = -not $managerStatusRequested
$managerStatusPageScreenshotFileOk = -not $managerStatusRequested
$managerStatusSummaryTextOk = -not $managerStatusRequested
$managerStatusSummaryCopyOk = -not $managerMvpRequested
$managerModsPageOk = -not $managerMvpRequested
$managerErrorsPageOk = -not $managerMvpRequested
$managerHooksPageOk = -not $managerMvpRequested
$managerFeaturesPageOk = -not $managerMvpRequested
$managerLogsPageOk = -not $managerMvpRequested
$managerLogsExportButtonOk = -not $managerMvpRequested
$managerLogsExportStateTextOk = -not $managerMvpRequested
$managerLogsPageScreenshotOk = -not $managerMvpRequested
$managerLogsPageScreenshotFileOk = -not $managerMvpRequested
$officialModUiOk = -not [bool]$AutoOpenOfficialModUi
$animalViewerUiOk = -not [bool]$AutoOpenAnimalPanel
$experimentalHooksOk = -not [bool]$AutoExerciseExperimentalHooks
$actionSpeedToolOk = -not [bool]$AutoExerciseActionSpeedTool
$actionSpeedConfigApplyOk = -not [bool]$AutoExerciseActionSpeedConfigApply
$actionSpeedInteractionOk = -not [bool]$AutoExerciseActionSpeedInteraction
$oneActionResourceHitOk = -not [bool]$AutoExerciseOneActionResourceHit
$oneActionWrongToolOk = -not [bool]$AutoExerciseOneActionWrongTool
$oneActionFuelFeedOk = -not [bool]$AutoExerciseOneActionFuelFeed
$oneActionVegetationOk = -not [bool]$AutoExerciseOneActionVegetation
$autoFishingInputLogOk = -not [bool]$AutoPressAutoFishingHotkey
$autoFishingHotkeyOk = -not [bool]$AutoExerciseAutoFishingPhase
$autoFishingMovementCancelOk = -not [bool]$AutoExerciseAutoFishingPhase
$autoFishingPhaseOk = -not [bool]$AutoExerciseAutoFishingPhase
$autoFishingMiniGameSkipOk = -not [bool]$AutoExerciseAutoFishingPhase
$autoFishingMiniGameCompleteOk = -not [bool]$AutoExerciseAutoFishingMiniGameComplete
$autoFishingReportExportOk = -not [bool]$AutoExerciseAutoFishingPhase
$diagnosticsReportExportScenarios = @()
if ($AutoExerciseZoom) {
    $diagnosticsReportExportScenarios += 'Camera'
}
if ($AutoExerciseActionSpeedInteraction) {
    $diagnosticsReportExportScenarios += 'ActionSpeed'
}
if ($AutoExerciseAutoFishingPhase) {
    $diagnosticsReportExportScenarios += 'AutoFishing'
}
$diagnosticsReportExportRequested = $diagnosticsReportExportScenarios.Count -gt 0
$diagnosticsReportExportOk = -not $diagnosticsReportExportRequested
$diagnosticsReportExportPassedScenarios = @()
$instantSaveOk = -not [bool]$AutoExerciseInstantSave
$debugConsoleOpenY1Ok = -not $requiresDebugConsoleKeySmoke
$debugConsoleMouseGiveOk = -not [bool]$AutoExerciseDebugConsoleMouseGive
$debugConsoleCloseEscapeOk = -not $requiresDebugConsoleKeySmoke
$debugConsoleOpenY2Ok = -not $requiresDebugConsoleKeySmoke
$debugConsoleCloseYOk = -not $requiresDebugConsoleKeySmoke
$debugConsoleTenYShortTapsOk = -not $requiresDebugConsoleKeySmoke
$debugConsoleHoldYNoFlickerOk = -not $requiresDebugConsoleKeySmoke
$debugInventoryOk = -not [bool]$AutoExerciseDebugInventory
$debugWeatherOk = -not [bool]$AutoExerciseDebugWeather
$debugTeleportCsvOk = -not [bool]$AutoExerciseDebugTeleport
$debugTeleportOk = -not [bool]$AutoExerciseDebugTeleport
$debugTimeOk = -not [bool]$AutoExerciseDebugTime
$debugMovementOk = -not [bool]$AutoExerciseDebugMovement
$advancedDebugOk = -not [bool]$AutoExerciseAdvancedDebug
$vehicleSecondMotorOk = -not [bool]$AutoExerciseVehicle
$newContentApisOk = -not [bool]$AutoExerciseNewContentApis
$newContentMineApisOk = -not [bool]$AutoExerciseMineContentApis
$newContentOilItemMetadataOk = -not [bool]$AutoExerciseNewContentApis
$newContentOilCoalDropOk = -not [bool]$AutoExerciseNewContentApis
$newContentMineOfficialJsonOk = -not ([bool]$AutoExerciseNewContentApis -or [bool]$AutoExerciseMineContentApis)
$newContentMineOfficialTechTreeUiOk = -not [bool]$AutoExerciseMineContentApis
$newContentMineProductionOk = -not ([bool]$AutoExerciseNewContentApis -or [bool]$AutoExerciseMineContentApis)
$newContentEquipmentSlotsOk = -not [bool]$AutoExerciseNewContentApis
$zoomOk = -not [bool]$AutoExerciseZoom
$chestLocatorEnhancerOk = -not [bool]$AutoExerciseChestLocatorEnhancer
$moreSavesOfficialSaveUiOk = -not [bool]$AutoExerciseMoreSavesOfficialSaveUi
$moreSavesOfficialSaveUiEvidenceOk = -not [bool]$AutoExerciseMoreSavesOfficialSaveUi
$strongPlantingGunOk = -not [bool]$AutoExerciseStrongPlantingGun
$cropHarvestingApiOk = -not [bool]$AutoExerciseCropHarvestingApi
$cropHarvestingApiEvidenceOk = -not [bool]$AutoExerciseCropHarvestingApi
$customEntityApisOk = -not [bool]$AutoExerciseCustomEntityApis

if ($startupOk) {
    $gameLaunchedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'GameLaunched dispatched.' -TimeoutSeconds 60 -AbortOnFatalInstanceWindow
    if ($gameLaunchedOk -and $titleSettingsRequested) {
        $titleButtonOk = Wait-ForLogLine -LogPath $logPath -Pattern 'DTMAPI title settings button visible on HomePageUiState.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($titleButtonOk -and $titleSettingsRequested) {
        $titleButtonScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Title settings button screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($titleButtonScreenshotOk -and $titleSettingsRequested) {
        $titleMenuOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI title settings menu.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($titleMenuOk -and $titleSettingsRequested) {
        $titleMenuScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Title settings menu screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($titleMenuOk -and $managerStatusRequested) {
        $managerStatusPageOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Status page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusPageOk -and $managerStatusRequested) {
        $managerStatusSummaryTextOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Status summary text OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusSummaryTextOk -and $managerStatusRequested) {
        if ($managerMvpRequested) {
            $managerStatusSummaryCopyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Status summary copy OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        }
        $managerStatusPageScreenshotOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Status page screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($managerStatusPageScreenshotOk -and $managerMvpRequested) {
        $managerModsPageOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Mods page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $managerErrorsPageOk = $managerModsPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Errors page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerHooksPageOk = $managerErrorsPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Hooks page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerFeaturesPageOk = $managerHooksPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Features page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsPageOk = $managerFeaturesPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke automation opened DTMAPI Manager Logs page.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsExportButtonOk = $managerLogsPageOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Logs export button OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsExportStateTextOk = $managerLogsExportButtonOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Logs export state OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
        $managerLogsPageScreenshotOk = $managerLogsExportStateTextOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Manager Logs page screenshot OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
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
    elseif ($probeOk -and $AutoExerciseExperimentalHooks -and $SaveSlot -gt 0) {
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
    elseif ($probeOk -and ($requiresDebugConsoleKeySmoke -or $AutoExerciseDebugInventory -or $AutoExerciseDebugWeather -or $AutoExerciseDebugTeleport -or $AutoExerciseDebugTime -or $AutoExerciseDebugMovement -or $AutoExerciseAdvancedDebug -or $AutoExerciseVehicle -or $AutoExerciseNewContentApis -or $AutoExerciseMineContentApis -or $AutoExerciseZoom -or $AutoExerciseChestLocatorEnhancer -or $AutoExerciseMoreSavesOfficialSaveUi -or $AutoExerciseStrongPlantingGun -or $AutoExerciseCropHarvestingApi -or $AutoExerciseCustomEntityApis) -and $SaveSlot -gt 0) {
        $saveLoadedOk = Wait-ForLogLine -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseNewContentApis) {
        $oilOk = Wait-ForLogLine -LogPath $logPath -Pattern 'OilMod content item=crude_oil fuelEnergy=1500 officialJson=item_tbitem.json.' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $mineOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Mine machine API register success=True reason=SaveLoaded' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $equipmentSlotsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'MoreEquipmentSlots API register success=True reason=SaveLoaded' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentOilItemMetadataOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentOilItemMetadata OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentOilCoalDropOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentOilCoalDrop OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineOfficialJsonOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineOfficialJson OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentEquipmentSlotsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentEquipmentSlots OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineProductionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineProduction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentApisOk = $oilOk -and $mineOk -and $equipmentSlotsOk -and $newContentOilItemMetadataOk -and $newContentOilCoalDropOk -and $newContentMineOfficialJsonOk -and $newContentEquipmentSlotsOk -and $newContentMineProductionOk
    }
    if ($saveLoadedOk -and $AutoExerciseMineContentApis) {
        $mineOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Mine machine API register success=True reason=SaveLoaded' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineOfficialJsonOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineOfficialJson OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineOfficialTechTreeUiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Mine official tech tree UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineProductionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise NewContentMineProduction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $newContentMineApisOk = $mineOk -and $newContentMineOfficialJsonOk -and $newContentMineOfficialTechTreeUiOk -and $newContentMineProductionOk
    }
    if ($saveLoadedOk -and $AutoOpenAnimalPanel) {
        $animalViewerUiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Animal viewer UI evidence OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseExperimentalHooks) {
        $experimentalHooksOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.ExperimentalHookExercise = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedTool) {
        $actionSpeedToolOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedTool OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedConfigApply) {
        $actionSpeedConfigApplyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedConfigApply OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseActionSpeedInteraction) {
        $actionSpeedInteractionOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ActionSpeedInteraction OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($actionSpeedInteractionOk) {
            $actionSpeedDiagnosticsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=ActionSpeed' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($actionSpeedDiagnosticsOk) {
                $diagnosticsReportExportPassedScenarios += 'ActionSpeed'
            }
        }
        else {
            $diagnosticsReportExportOk = $false
        }
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
            $autoFishingMovementCancelOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingMovementCancel OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            $autoFishingPhaseOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise AutoFishingPhase OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($AutoExerciseAutoFishingMiniGameComplete) {
                $autoFishingMiniGameSkipOk = $true
                $autoFishingMiniGameCompleteOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingMiniGameComplete = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            else {
                $autoFishingMiniGameSkipOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.AutoFishingMiniGameSkip = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            }
            $autoFishingReportExportOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=AutoFishing' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($autoFishingReportExportOk) {
                $diagnosticsReportExportPassedScenarios += 'AutoFishing'
            }
        }
        else {
            $autoFishingHotkeyOk = $false
            $autoFishingMovementCancelOk = $false
            $autoFishingPhaseOk = $false
            $autoFishingMiniGameSkipOk = $false
            $autoFishingMiniGameCompleteOk = $false
            $autoFishingReportExportOk = $false
            $diagnosticsReportExportOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExerciseInstantSave) {
        $instantSaveOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise InstantSave OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $requiresDebugConsoleKeySmoke) {
        $debugConsoleInGameHotkeyOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugConsoleHotkey OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($debugConsoleInGameHotkeyOk) {
            $debugConsoleOpenY1Ok = $true
            $debugConsoleCloseEscapeOk = $true
            $debugConsoleOpenY2Ok = $true
            $debugConsoleCloseYOk = $true
            $debugConsoleTenYShortTapsOk = $true
            $debugConsoleHoldYNoFlickerOk = $true
            "InGameDebugConsoleHotkeySmoke=$(Get-Date -Format o);ok=True" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            if ($AutoExerciseDebugConsoleMouseGive) {
                $debugConsoleMouseGiveOk = Invoke-DebugConsoleMouseGiveSmoke
            }
        }
        else {
            "InGameDebugConsoleHotkeySmoke=$(Get-Date -Format o);ok=False;fallback=external-key-injection" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
            Start-Sleep -Seconds 1
            $debugConsoleOpenY1Ok = Invoke-DebugConsoleSmokeKey -Label 'Y1' -VirtualKey 0x59 -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount 1 -Attempts 4 -WaitSeconds 5
            if ($debugConsoleOpenY1Ok) {
                if ($AutoExerciseDebugConsoleMouseGive) {
                    $debugConsoleMouseGiveOk = Invoke-DebugConsoleMouseGiveSmoke
                }
                $debugConsoleCloseEscapeOk = Invoke-DebugConsoleSmokeKey -Label 'Escape' -VirtualKey 0x1B -Pattern 'Debug console closed reason=Escape owner=DTMAPI.DebugConsoleMod.' -MinimumCount 1 -Attempts 2 -WaitSeconds 5
            }
            if ($debugConsoleCloseEscapeOk) {
                $debugConsoleOpenY2Ok = Invoke-DebugConsoleSmokeKey -Label 'Y2' -VirtualKey 0x59 -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount 2 -Attempts 4 -WaitSeconds 5
            }
            if ($debugConsoleOpenY2Ok) {
                $debugConsoleCloseYOk = Invoke-DebugConsoleSmokeKey -Label 'Y3' -VirtualKey 0x59 -Pattern 'Debug console closed reason=Y owner=DTMAPI.DebugConsoleMod.' -MinimumCount 1 -Attempts 2 -WaitSeconds 5
            }
            if ($debugConsoleCloseYOk) {
                $debugConsoleTenYShortTapsOk = $true
                for ($tap = 1; $tap -le 10; $tap++) {
                    if (($tap % 2) -eq 1) {
                        $expectedOpenCount = 2 + [int](($tap + 1) / 2)
                        $ok = Invoke-DebugConsoleSmokeKey -Label "YShort$tap" -VirtualKey 0x59 -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount $expectedOpenCount -Attempts 1 -WaitSeconds 4
                    }
                    else {
                        $expectedCloseCount = 1 + [int]($tap / 2)
                        $ok = Invoke-DebugConsoleSmokeKey -Label "YShort$tap" -VirtualKey 0x59 -Pattern 'Debug console closed reason=Y owner=DTMAPI.DebugConsoleMod.' -MinimumCount $expectedCloseCount -Attempts 1 -WaitSeconds 4
                    }

                    if (-not $ok) {
                        $debugConsoleTenYShortTapsOk = $false
                        break
                    }
                }

                if ($debugConsoleTenYShortTapsOk) {
                    $openCountBeforeHold = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -ErrorAction SilentlyContinue).Count
                    $closeYCountBeforeHold = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console closed reason=Y owner=DTMAPI.DebugConsoleMod.' -ErrorAction SilentlyContinue).Count
                    $sentHoldY = Send-DolocTownKey -VirtualKey 0x59 -Name 'YHold' -HoldMilliseconds 1800
                    "SentExternalDebugConsoleYHold=$(Get-Date -Format o);ok=$sentHoldY;openBefore=$openCountBeforeHold;closeYBefore=$closeYCountBeforeHold" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                    if ($sentHoldY -and (Wait-ForLogLineCount -LogPath $logPath -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -MinimumCount ($openCountBeforeHold + 1) -TimeoutSeconds 5)) {
                        Start-Sleep -Seconds 2
                        $openCountAfterHold = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console opened owner=DTMAPI.DebugConsoleMod reason=hotkey Y.' -ErrorAction SilentlyContinue).Count
                        $closeYCountAfterHold = @(Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Debug console closed reason=Y owner=DTMAPI.DebugConsoleMod.' -ErrorAction SilentlyContinue).Count
                        $debugConsoleHoldYNoFlickerOk = ($openCountAfterHold -eq ($openCountBeforeHold + 1)) -and ($closeYCountAfterHold -eq $closeYCountBeforeHold)
                        "DebugConsoleYHoldResult=$(Get-Date -Format o);openAfter=$openCountAfterHold;closeYAfter=$closeYCountAfterHold;ok=$debugConsoleHoldYNoFlickerOk" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                    }
                    else {
                        $debugConsoleHoldYNoFlickerOk = $false
                        "DebugConsoleYHoldResult=$(Get-Date -Format o);ok=False" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
                    }
                }
            }
        }
    }
    if ($saveLoadedOk -and $AutoExerciseDebugInventory) {
        $debugInventoryOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugInventory OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugWeather) {
        $debugWeatherOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugWeather OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugTeleport) {
        $debugTeleportCsvOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugTeleportCsv OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $debugTeleportOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugTeleport OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugTime) {
        $debugTimeOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugTime OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseDebugMovement) {
        $debugMovementOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise DebugMovement OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseAdvancedDebug) {
        $advancedDebugOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise AdvancedDebug OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseVehicle) {
        $vehicleSecondMotorOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise VehicleSecondMotor OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseZoom) {
        $zoomOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise CameraPlayable OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        if ($zoomOk) {
            $cameraDiagnosticsOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.DiagnosticsSnapshot = verified. scenario=Camera' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
            if ($cameraDiagnosticsOk) {
                $diagnosticsReportExportPassedScenarios += 'Camera'
            }
        }
        else {
            $diagnosticsReportExportOk = $false
        }
    }
    if ($saveLoadedOk -and $AutoExerciseChestLocatorEnhancer) {
        $chestLocatorEnhancerOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise ChestLocatorEnhancer OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseMoreSavesOfficialSaveUi) {
        $moreSavesOfficialSaveUiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.MoreSavesOfficialSaveUi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $moreSavesOfficialSaveUiEvidenceOk = $moreSavesOfficialSaveUiOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'MoreSaves official save UI evidence archiveFileCount=' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($saveLoadedOk -and $AutoExerciseStrongPlantingGun) {
        $strongPlantingGunOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise StrongPlantingGun OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    }
    if ($saveLoadedOk -and $AutoExerciseCropHarvestingApi) {
        $cropHarvestingApiOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise CropHarvestingApi OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
        $cropHarvestingApiEvidenceOk = $cropHarvestingApiOk -and (Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke.CropHarvestingApi = verified' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow)
    }
    if ($saveLoadedOk -and $AutoExerciseCustomEntityApis) {
        $customEntityApisOk = Wait-ForLogLine -LogPath $logPath -Pattern 'Smoke exercise CustomEntityApis OK' -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
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
if ($usesAutoFishingConfigSmoke) {
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
Restore-SmokeSecondMotorEnablement
$titleButtonScreenshotFileOk = -not $titleSettingsRequested
$titleMenuScreenshotFileOk = -not $titleSettingsRequested
$officialModUiScreenshotFileOk = -not [bool]$AutoOpenOfficialModUi
$newContentMineOfficialTechTreeUiScreenshotFileOk = -not [bool]$AutoExerciseMineContentApis
if ($titleSettingsRequested -and (Test-Path $logPath)) {
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
    if ($managerStatusRequested) {
        $statusScreenshotLine = Select-String -Path $logPath -Pattern 'Manager Status page screenshot OK screenshot=' | Select-Object -Last 1
        if ($statusScreenshotLine) {
            $statusScreenshotPath = $statusScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
            $managerStatusPageScreenshotFileOk = Test-Path -LiteralPath $statusScreenshotPath
        }
    }
    if ($managerMvpRequested) {
        $logsScreenshotLine = Select-String -Path $logPath -Pattern 'Manager Logs page screenshot OK screenshot=' | Select-Object -Last 1
        if ($logsScreenshotLine) {
            $logsScreenshotPath = $logsScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
            $managerLogsPageScreenshotFileOk = Test-Path -LiteralPath $logsScreenshotPath
        }
    }
}
if ($AutoOpenOfficialModUi -and (Test-Path $logPath)) {
    $officialScreenshotLine = Select-String -Path $logPath -Pattern 'Official Mod UI evidence OK .*screenshot=' | Select-Object -Last 1
    if ($officialScreenshotLine) {
        $officialScreenshotPath = $officialScreenshotLine.Line -replace '^.*screenshot=', '' -replace '\.$', ''
        $officialModUiScreenshotFileOk = Test-Path -LiteralPath $officialScreenshotPath
    }
}
if ($AutoExerciseMineContentApis -and (Test-Path $logPath)) {
    $mineTechTreeScreenshotLine = Select-String -Path $logPath -Pattern 'Mine official tech tree UI evidence OK .*screenshot=' | Select-Object -Last 1
    if ($mineTechTreeScreenshotLine) {
        $mineTechTreeScreenshotPath = $mineTechTreeScreenshotLine.Line -replace '^.*screenshot=', '' -replace ', close=.*$', '' -replace '\.$', ''
        $newContentMineOfficialTechTreeUiScreenshotFileOk = Test-Path -LiteralPath $mineTechTreeScreenshotPath
    }
}
$runAborted = ($fatalWindows.Count -gt 0) -or $forcedClose -or [bool]$leftover
$diagnosticsReportExportOk = (-not $diagnosticsReportExportRequested) -or ($diagnosticsReportExportPassedScenarios.Count -eq $diagnosticsReportExportScenarios.Count)
$runFailed = (-not $startupOk -or -not $gameLaunchedOk -or -not $probeOk -or -not $saveLoadedOk -or -not $titleButtonOk -or -not $titleButtonScreenshotOk -or -not $titleButtonScreenshotFileOk -or -not $titleLifecycleOk -or -not $titleMenuOk -or -not $titleMenuScreenshotOk -or -not $titleMenuScreenshotFileOk -or -not $managerStatusPageOk -or -not $managerStatusPageScreenshotOk -or -not $managerStatusPageScreenshotFileOk -or -not $managerStatusSummaryTextOk -or -not $managerStatusSummaryCopyOk -or -not $managerModsPageOk -or -not $managerErrorsPageOk -or -not $managerHooksPageOk -or -not $managerFeaturesPageOk -or -not $managerLogsPageOk -or -not $managerLogsExportButtonOk -or -not $managerLogsExportStateTextOk -or -not $managerLogsPageScreenshotOk -or -not $managerLogsPageScreenshotFileOk -or -not $officialModUiOk -or -not $officialModUiScreenshotFileOk -or -not $animalViewerUiOk -or -not $experimentalHooksOk -or -not $actionSpeedToolOk -or -not $actionSpeedConfigApplyOk -or -not $actionSpeedInteractionOk -or -not $oneActionResourceHitOk -or -not $oneActionWrongToolOk -or -not $oneActionFuelFeedOk -or -not $oneActionVegetationOk -or -not $autoFishingInputLogOk -or -not $autoFishingHotkeyOk -or -not $autoFishingMovementCancelOk -or -not $autoFishingPhaseOk -or -not $autoFishingMiniGameSkipOk -or -not $autoFishingMiniGameCompleteOk -or -not $autoFishingReportExportOk -or -not $diagnosticsReportExportOk -or -not $instantSaveOk -or -not $debugConsoleOpenY1Ok -or -not $debugConsoleMouseGiveOk -or -not $debugConsoleCloseEscapeOk -or -not $debugConsoleOpenY2Ok -or -not $debugConsoleCloseYOk -or -not $debugConsoleTenYShortTapsOk -or -not $debugConsoleHoldYNoFlickerOk -or -not $debugInventoryOk -or -not $debugWeatherOk -or -not $debugTeleportCsvOk -or -not $debugTeleportOk -or -not $debugTimeOk -or -not $debugMovementOk -or -not $advancedDebugOk -or -not $vehicleSecondMotorOk -or -not $zoomOk -or -not $chestLocatorEnhancerOk -or -not $moreSavesOfficialSaveUiOk -or -not $moreSavesOfficialSaveUiEvidenceOk -or -not $strongPlantingGunOk -or -not $cropHarvestingApiOk -or -not $cropHarvestingApiEvidenceOk -or -not $customEntityApisOk -or -not $newContentApisOk -or -not $newContentMineApisOk -or -not $newContentOilItemMetadataOk -or -not $newContentOilCoalDropOk -or -not $newContentMineOfficialJsonOk -or -not $newContentMineOfficialTechTreeUiOk -or -not $newContentMineOfficialTechTreeUiScreenshotFileOk -or -not $newContentEquipmentSlotsOk -or -not $newContentMineProductionOk -or $runAborted)
$runStatus = if ($runAborted) { 'Aborted' } elseif ($runFailed) { 'Failed' } else { 'Passed' }
$result = @{
    SchemaVersion = 2
    RunStatus = $runStatus
    StartupLog = Get-SmokeAlwaysStatus -Passed $startupOk
    GameLaunched = Get-SmokeAlwaysStatus -Passed $gameLaunchedOk
    HookProbe = Get-SmokeStatus -Requested ([bool]$IncludeHookProbe) -Passed $probeOk
    SaveLoaded = Get-SmokeStatus -Requested $saveLoadedRequested -Passed $saveLoadedOk
    TitleSettingsButton = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleButtonOk
    TitleSettingsButtonScreenshot = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleButtonScreenshotOk
    TitleSettingsButtonScreenshotFile = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleButtonScreenshotFileOk
    TitleButtonLifecycle = Get-SmokeStatus -Requested ([bool]$AutoExerciseTitleButtonLifecycle) -Passed $titleLifecycleOk
    TitleSettingsMenu = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleMenuOk
    TitleSettingsMenuScreenshot = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleMenuScreenshotOk
    TitleSettingsMenuScreenshotFile = Get-SmokeStatus -Requested $titleSettingsRequested -Passed $titleMenuScreenshotFileOk
    ManagerStatusPage = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusPageOk
    ManagerStatusPageScreenshot = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusPageScreenshotOk
    ManagerStatusPageScreenshotFile = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusPageScreenshotFileOk
    ManagerStatusSummaryText = Get-SmokeStatus -Requested $managerStatusRequested -Passed $managerStatusSummaryTextOk
    ManagerStatusSummaryCopy = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerStatusSummaryCopyOk
    ManagerModsPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerModsPageOk
    ManagerErrorsPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerErrorsPageOk
    ManagerHooksPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerHooksPageOk
    ManagerFeaturesPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerFeaturesPageOk
    ManagerLogsPage = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsPageOk
    ManagerLogsExportButton = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsExportButtonOk
    ManagerLogsExportStateText = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsExportStateTextOk
    ManagerLogsPageScreenshot = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsPageScreenshotOk
    ManagerLogsPageScreenshotFile = Get-SmokeStatus -Requested $managerMvpRequested -Passed $managerLogsPageScreenshotFileOk
    OfficialModUi = Get-SmokeStatus -Requested ([bool]$AutoOpenOfficialModUi) -Passed $officialModUiOk
    OfficialModUiScreenshotFile = Get-SmokeStatus -Requested ([bool]$AutoOpenOfficialModUi) -Passed $officialModUiScreenshotFileOk
    AnimalViewerUi = Get-SmokeStatus -Requested ([bool]$AutoOpenAnimalPanel) -Passed $animalViewerUiOk
    ExperimentalHooks = Get-SmokeStatus -Requested ([bool]$AutoExerciseExperimentalHooks) -Passed $experimentalHooksOk
    ActionSpeedTool = Get-SmokeStatus -Requested ([bool]$AutoExerciseActionSpeedTool) -Passed $actionSpeedToolOk
    ActionSpeedConfigApply = Get-SmokeStatus -Requested ([bool]$AutoExerciseActionSpeedConfigApply) -Passed $actionSpeedConfigApplyOk
    ActionSpeedInteraction = Get-SmokeStatus -Requested ([bool]$AutoExerciseActionSpeedInteraction) -Passed $actionSpeedInteractionOk
    OneActionResourceHit = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionResourceHit) -Passed $oneActionResourceHitOk
    OneActionWrongTool = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionWrongTool) -Passed $oneActionWrongToolOk
    OneActionFuelFeed = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionFuelFeed) -Passed $oneActionFuelFeedOk
    OneActionVegetation = Get-SmokeStatus -Requested ([bool]$AutoExerciseOneActionVegetation) -Passed $oneActionVegetationOk
    AutoFishingInputLog = Get-SmokeStatus -Requested ([bool]$AutoPressAutoFishingHotkey) -Passed $autoFishingInputLogOk
    AutoFishingHotkey = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase) -Passed $autoFishingHotkeyOk
    AutoFishingMovementCancel = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase) -Passed $autoFishingMovementCancelOk
    AutoFishingPhase = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase) -Passed $autoFishingPhaseOk
    AutoFishingMiniGameSkip = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase -and -not [bool]$AutoExerciseAutoFishingMiniGameComplete) -Passed $autoFishingMiniGameSkipOk
    AutoFishingMiniGameComplete = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingMiniGameComplete) -Passed $autoFishingMiniGameCompleteOk
    AutoFishingReportExport = Get-SmokeStatus -Requested ([bool]$AutoExerciseAutoFishingPhase) -Passed $autoFishingReportExportOk
    DiagnosticsReportExport = Get-SmokeStatus -Requested $diagnosticsReportExportRequested -Passed $diagnosticsReportExportOk
    InstantSave = Get-SmokeStatus -Requested ([bool]$AutoExerciseInstantSave) -Passed $instantSaveOk
    DebugConsoleOpenY1 = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleOpenY1Ok
    DebugConsoleMouseGive = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugConsoleMouseGive) -Passed $debugConsoleMouseGiveOk
    DebugConsoleCloseEscape = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleCloseEscapeOk
    DebugConsoleOpenY2 = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleOpenY2Ok
    DebugConsoleCloseY = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleCloseYOk
    DebugConsoleTenYShortTaps = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleTenYShortTapsOk
    DebugConsoleHoldYNoFlicker = Get-SmokeStatus -Requested $requiresDebugConsoleKeySmoke -Passed $debugConsoleHoldYNoFlickerOk
    DebugInventory = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugInventory) -Passed $debugInventoryOk
    DebugWeather = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugWeather) -Passed $debugWeatherOk
    DebugTeleportCsv = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugTeleport) -Passed $debugTeleportCsvOk
    DebugTeleport = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugTeleport) -Passed $debugTeleportOk
    DebugTime = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugTime) -Passed $debugTimeOk
    DebugMovement = Get-SmokeStatus -Requested ([bool]$AutoExerciseDebugMovement) -Passed $debugMovementOk
    AdvancedDebug = Get-SmokeStatus -Requested ([bool]$AutoExerciseAdvancedDebug) -Passed $advancedDebugOk
    VehicleSecondMotor = Get-SmokeStatus -Requested ([bool]$AutoExerciseVehicle) -Passed $vehicleSecondMotorOk
    Zoom = Get-SmokeStatus -Requested ([bool]$AutoExerciseZoom) -Passed $zoomOk
    ChestLocatorEnhancer = Get-SmokeStatus -Requested ([bool]$AutoExerciseChestLocatorEnhancer) -Passed $chestLocatorEnhancerOk
    MoreSavesOfficialSaveUi = Get-SmokeStatus -Requested ([bool]$AutoExerciseMoreSavesOfficialSaveUi) -Passed $moreSavesOfficialSaveUiOk
    MoreSavesOfficialSaveUiEvidence = Get-SmokeStatus -Requested ([bool]$AutoExerciseMoreSavesOfficialSaveUi) -Passed $moreSavesOfficialSaveUiEvidenceOk
    StrongPlantingGun = Get-SmokeStatus -Requested ([bool]$AutoExerciseStrongPlantingGun) -Passed $strongPlantingGunOk
    CropHarvestingApi = Get-SmokeStatus -Requested ([bool]$AutoExerciseCropHarvestingApi) -Passed $cropHarvestingApiOk
    CropHarvestingApiEvidence = Get-SmokeStatus -Requested ([bool]$AutoExerciseCropHarvestingApi) -Passed $cropHarvestingApiEvidenceOk
    CustomEntityApis = Get-SmokeStatus -Requested ([bool]$AutoExerciseCustomEntityApis) -Passed $customEntityApisOk
    NewContentApis = Get-SmokeStatus -Requested ([bool]$AutoExerciseNewContentApis) -Passed $newContentApisOk
    NewContentMineApis = Get-SmokeStatus -Requested ([bool]$AutoExerciseMineContentApis) -Passed $newContentMineApisOk
    NewContentOilItemMetadata = Get-SmokeStatus -Requested ([bool]$AutoExerciseNewContentApis) -Passed $newContentOilItemMetadataOk
    NewContentOilCoalDrop = Get-SmokeStatus -Requested ([bool]$AutoExerciseNewContentApis) -Passed $newContentOilCoalDropOk
    NewContentMineOfficialJson = Get-SmokeStatus -Requested ([bool]$AutoExerciseNewContentApis -or [bool]$AutoExerciseMineContentApis) -Passed $newContentMineOfficialJsonOk
    NewContentMineOfficialTechTreeUi = Get-SmokeStatus -Requested ([bool]$AutoExerciseMineContentApis) -Passed $newContentMineOfficialTechTreeUiOk
    NewContentMineOfficialTechTreeUiScreenshotFile = Get-SmokeStatus -Requested ([bool]$AutoExerciseMineContentApis) -Passed $newContentMineOfficialTechTreeUiScreenshotFileOk
    NewContentEquipmentSlots = Get-SmokeStatus -Requested ([bool]$AutoExerciseNewContentApis) -Passed $newContentEquipmentSlotsOk
    NewContentMineProduction = Get-SmokeStatus -Requested ([bool]$AutoExerciseNewContentApis -or [bool]$AutoExerciseMineContentApis) -Passed $newContentMineProductionOk
    NoFatalInstanceWindow = Get-SmokeAlwaysStatus -Passed ($fatalWindows.Count -eq 0)
    ProcessExited = Get-SmokeAlwaysStatus -Passed (-not [bool]$leftover)
    ForcedClose = Get-SmokeAlwaysStatus -Passed (-not $forcedClose)
    Completed = Get-Date -Format o
}
Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value $result
try {
    & "$PSScriptRoot\analyze-startup-evidence.ps1" -EvidencePath $evidence -OutputDirectory $evidence -Quiet
}
catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $evidence 'startup-analysis-error.txt')
}

if ($runFailed) {
    Write-Error "Game smoke failed or left DolocTown.exe running. Evidence: $evidence"
    exit 1
}

Write-Host "Game smoke passed. Evidence: $evidence"
