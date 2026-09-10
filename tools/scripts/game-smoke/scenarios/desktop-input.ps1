function Get-SmokeNoQaAnimalInputSequences {
    param([Parameter(Mandatory = $true)] [bool] $AutoDrive)

    if (-not $AutoDrive) {
        return 'NoQaAnimalViewerEscape'
    }

    return @(
        [string]::Join("`n", [string[]]@(
            'NoQaAnimalApproachA1',
            'NoQaAnimalOpenE1',
            'NoQaAnimalSelectSecondRow',
            'NoQaAnimalViewerEscape'
        )),
        [string]::Join("`n", [string[]]@(
            'NoQaAnimalApproachA1',
            'NoQaAnimalOpenE1',
            'NoQaAnimalApproachA2',
            'NoQaAnimalOpenE2',
            'NoQaAnimalSelectSecondRow',
            'NoQaAnimalViewerEscape'
        ))
    )
}

function Test-SmokeNoQaAnimalInputSequences {
    $autoDrive = @(Get-SmokeNoQaAnimalInputSequences -AutoDrive $true)
    $manual = @(Get-SmokeNoQaAnimalInputSequences -AutoDrive $false)
    $oneAttempt = [string]::Join("`n", [string[]]@(
        'NoQaAnimalApproachA1',
        'NoQaAnimalOpenE1',
        'NoQaAnimalSelectSecondRow',
        'NoQaAnimalViewerEscape'
    ))
    $twoAttempts = [string]::Join("`n", [string[]]@(
        'NoQaAnimalApproachA1',
        'NoQaAnimalOpenE1',
        'NoQaAnimalApproachA2',
        'NoQaAnimalOpenE2',
        'NoQaAnimalSelectSecondRow',
        'NoQaAnimalViewerEscape'
    ))
    $passed = $autoDrive.Count -eq 2 -and
        [string]::Equals([string]$autoDrive[0], $oneAttempt, [System.StringComparison]::Ordinal) -and
        [string]::Equals([string]$autoDrive[1], $twoAttempts, [System.StringComparison]::Ordinal) -and
        $manual.Count -eq 1 -and
        [string]::Equals([string]$manual[0], 'NoQaAnimalViewerEscape', [System.StringComparison]::Ordinal) -and
        @($autoDrive | Where-Object { [string]$_ -match 'System\.Object\[\]' }).Count -eq 0
    return [pscustomobject]@{
        AutoDriveSequenceCount = $autoDrive.Count
        AutoDriveSequences = @($autoDrive)
        ManualSequences = @($manual)
        Passed = $passed
    }
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
    public DtmApiSmokeInputUnion data;
}

[StructLayout(LayoutKind.Sequential)]
public struct DtmApiSmokeKeyboardInput
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Explicit)]
public struct DtmApiSmokeInputUnion
{
    [FieldOffset(0)]
    public DtmApiSmokeMouseInput mi;

    [FieldOffset(0)]
    public DtmApiSmokeKeyboardInput ki;
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

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static bool SendMouseButton(uint downFlag, uint upFlag, int holdMilliseconds)
    {
        var down = new DtmApiSmokeInputEvent
        {
            type = 0,
            data = new DtmApiSmokeInputUnion { mi = new DtmApiSmokeMouseInput { dwFlags = downFlag } }
        };
        var up = new DtmApiSmokeInputEvent
        {
            type = 0,
            data = new DtmApiSmokeInputUnion { mi = new DtmApiSmokeMouseInput { dwFlags = upFlag } }
        };
        int size = Marshal.SizeOf(typeof(DtmApiSmokeInputEvent));
        uint downCount = SendInput(1, new[] { down }, size);
        Thread.Sleep(Math.Max(40, holdMilliseconds));
        uint upCount = SendInput(1, new[] { up }, size);
        return downCount == 1 && upCount == 1;
    }

    public static bool SendKeyboardKey(ushort virtualKey, int holdMilliseconds)
    {
        ushort scanCode = (ushort)MapVirtualKey(virtualKey, 0);
        var down = new DtmApiSmokeInputEvent
        {
            type = 1,
            data = new DtmApiSmokeInputUnion { ki = new DtmApiSmokeKeyboardInput { wVk = 0, wScan = scanCode, dwFlags = 0x0008 } }
        };
        var up = new DtmApiSmokeInputEvent
        {
            type = 1,
            data = new DtmApiSmokeInputUnion { ki = new DtmApiSmokeKeyboardInput { wVk = 0, wScan = scanCode, dwFlags = 0x0008 | 0x0002 } }
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
        [int] $TimeoutSeconds = 20,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $deadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $deadline)) {
        return $null
    }
    Initialize-DtmApiSmokeInput
    do {
        if (-not (Test-SmokeDeadlineHasBudget -Deadline $deadline)) {
            break
        }
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

            if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 800)) {
                return $null
            }
            $foreground = [DtmApiSmokeInput]::GetForegroundWindow()
            $foregroundPid = 0
            if ($foreground -ne [IntPtr]::Zero) {
                [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foreground, [ref]$foregroundPid)
            }
            if ($foregroundPid -ne $proc.Id) {
                if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 250)) {
                    return $null
                }
                continue
            }

            return $proc
        }
        if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 250)) {
            break
        }
    } while (Test-SmokeDeadlineHasBudget -Deadline $deadline)

    return $null
}

function Send-DolocTownKey {
    param(
        [Parameter(Mandatory = $true)] [int] $VirtualKey,
        [string] $Name = 'key',
        [int] $TimeoutSeconds = 20,
        [int] $HoldMilliseconds = 260,
        [string] $InputLogPath = '',
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $script:lastDolocTownInputEvidence = [ordered]@{
        DolocTownPid = 0
        ForegroundPidAtSend = 0
        ForegroundMatchedAtSend = $false
        SendInputSucceeded = $false
        PostMessageFallbackUsed = $false
        KeyDownAt = $null
        KeyUpAt = $null
        LogOffsetAtSend = [int64]0
        CompletedBeforeDeadline = $false
    }
    $effectiveDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $effectiveDeadline)) {
        return $false
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds -Deadline $effectiveDeadline
    if (-not $proc) {
        return $false
    }

    $foregroundWindow = [DtmApiSmokeInput]::GetForegroundWindow()
    $foregroundPid = 0
    if ($foregroundWindow -ne [IntPtr]::Zero) {
        [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foregroundWindow, [ref]$foregroundPid)
    }
    $keyParam = [UIntPtr]::new([uint64]$VirtualKey)
    $logOffsetAtSend = if ($InputLogPath -and (Test-Path -LiteralPath $InputLogPath -PathType Leaf)) { [int64](Get-Item -LiteralPath $InputLogPath).Length } else { [int64]0 }
    $effectiveHold = [Math]::Max(40, $HoldMilliseconds)
    $sendReceipt = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $effectiveDeadline `
        -MinimumExecutionBudgetMilliseconds ($effectiveHold + 50) `
        -Action { return [DtmApiSmokeInput]::SendKeyboardKey([uint16]$VirtualKey, $effectiveHold) }
    $keyDownAt = $sendReceipt.StartedAt.ToUniversalTime().ToString('o')
    $keyUpAt = $sendReceipt.CompletedAt.ToUniversalTime().ToString('o')
    $sentInput = [bool]$sendReceipt.Accepted
    $postMessageFallbackRequested = -not ([bool]$RequireExternalPlayerInputGate -or [bool]$AssertNoQaUiEvidence -or [bool]$qaG4DebugConsoleEnabled -or [bool]$qaG4EquipmentSlotsObservationEnabled -or [bool]$moreEquipmentSlotsTransitionRequested -or [bool]$Batch6AutoFishingPilot -or [bool]$Batch6AutoFishingManagerLifecycle -or [bool]$AutoPressOneActionMenuKey)
    $postMessageFallbackUsed = $false
    if ($sentInput -and $postMessageFallbackRequested) {
        $postReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -Action {
            $downPosted = [DtmApiSmokeInput]::PostMessage($proc.MainWindowHandle, 0x0100, $keyParam, [UIntPtr]::Zero)
            $upPosted = [DtmApiSmokeInput]::PostMessage($proc.MainWindowHandle, 0x0101, $keyParam, [UIntPtr]::Zero)
            return $downPosted -and $upPosted
        }
        $postMessageFallbackUsed = [bool]$postReceipt.Accepted
    }
    $script:lastDolocTownInputEvidence = [ordered]@{
        DolocTownPid = [int]$proc.Id
        ForegroundPidAtSend = [int]$foregroundPid
        ForegroundMatchedAtSend = [bool]($foregroundPid -eq $proc.Id)
        SendInputSucceeded = [bool]$sentInput
        PostMessageFallbackUsed = [bool]$postMessageFallbackUsed
        KeyDownAt = $keyDownAt
        KeyUpAt = $keyUpAt
        LogOffsetAtSend = [int64]$logOffsetAtSend
        CompletedBeforeDeadline = [bool]$sendReceipt.CompletedBeforeDeadline
    }
    return $sentInput
}

function Resolve-DolocTownVirtualKey {
    param(
        [string] $Key
    )

    if ([string]::IsNullOrWhiteSpace($Key) -or $Key.Equals('None', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    $normalized = $Key.Trim()
    if ($normalized -match '^F([1-9]|1[0-9]|2[0-4])$') {
        return 0x70 + [int]$Matches[1] - 1
    }
    if ($normalized -match '^[A-Z]$') {
        return [int][char]$normalized
    }
    if ($normalized -match '^Alpha([0-9])$') {
        return [int][char]$Matches[1]
    }

    switch ($normalized) {
        'Escape' { return 0x1B }
        'Backspace' { return 0x08 }
        'Delete' { return 0x2E }
        'Space' { return 0x20 }
        'Tab' { return 0x09 }
        'Return' { return 0x0D }
        'KeypadEnter' { return 0x0D }
        'LeftShift' { return 0xA0 }
        'RightShift' { return 0xA1 }
        'LeftControl' { return 0xA2 }
        'RightControl' { return 0xA3 }
        'LeftAlt' { return 0xA4 }
        'RightAlt' { return 0xA5 }
        'Insert' { return 0x2D }
        'Home' { return 0x24 }
        'End' { return 0x23 }
        'PageUp' { return 0x21 }
        'PageDown' { return 0x22 }
        'Plus' { return 0xBB }
        'Equals' { return 0xBB }
        'Minus' { return 0xBD }
        'KeypadPlus' { return 0x6B }
        'KeypadMinus' { return 0x6D }
        'UpArrow' { return 0x26 }
        'DownArrow' { return 0x28 }
        'LeftArrow' { return 0x25 }
        'RightArrow' { return 0x27 }
        default { return $null }
    }
}

function Send-DolocTownNamedKey {
    param(
        [string] $Key,
        [int] $TimeoutSeconds = 20,
        [int] $HoldMilliseconds = 260,
        [string] $InputLogPath = '',
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $virtualKey = Resolve-DolocTownVirtualKey -Key $Key
    if ($null -eq $virtualKey) {
        return $false
    }

    return Send-DolocTownKey -VirtualKey $virtualKey -Name $Key -TimeoutSeconds $TimeoutSeconds -HoldMilliseconds $HoldMilliseconds -InputLogPath $InputLogPath -Deadline $Deadline
}

function Wait-Batch6AutoFishingHandshakeState {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [string] $State,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $Receipts,
        [string] $HookId = 'Smoke.Batch6.AutoFishingPilot.Handshake'
    )

    # Wait-ForLogLineAfterOffset owns literal escaping. Keep this value literal
    # and include the separator after the exact state to prevent prefix matches.
    $pattern = $HookId + ' state=' + $State + ' '
    $startedAt = (Get-Date).ToUniversalTime()
    $passed = Wait-ForLogLineAfterOffset -LogPath $LogPath -Offset $Offset -Pattern $pattern -TimeoutSeconds $TimeoutSeconds -AbortOnFatalInstanceWindow
    $receipt = [ordered]@{
        Sequence = $Receipts.Count + 1
        State = $State
        LogOffset = [int64]$Offset
        Pattern = $pattern
        StartedAtUtc = $startedAt.ToString('o')
        CompletedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        Observed = [bool]$passed
    }
    [void]$Receipts.Add($receipt)
    return [pscustomobject]$receipt
}

function Send-Batch6AutoFishingRecordedKey {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [string] $State,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $InputReceipts,
        [Parameter(Mandatory = $true)] [bool] $HandshakeObserved,
        [ValidateSet('F6','F7','A')] [string] $Key = 'F6',
        [ValidateSet('ConfiguredToggle','ManualMovement')] [string] $Purpose = 'ConfiguredToggle',
        [ValidateRange(1, 1000)] [int] $HoldMilliseconds = 260
    )

    $sent = $false
    $inputEvidence = [ordered]@{
        DolocTownPid = 0
        ForegroundPidAtSend = 0
        ForegroundMatchedAtSend = $false
        SendInputSucceeded = $false
        PostMessageFallbackUsed = $false
        KeyDownAt = $null
        KeyUpAt = $null
        LogOffsetAtSend = [int64]0
        CompletedBeforeDeadline = $false
    }
    if ($HandshakeObserved) {
        $sent = Send-DolocTownNamedKey -Key $Key -TimeoutSeconds $TimeoutSeconds -HoldMilliseconds $HoldMilliseconds -InputLogPath $LogPath
        $inputEvidence = $script:lastDolocTownInputEvidence
    }
    $provenancePassed = $HandshakeObserved -and [bool]$sent -and
        [bool]$inputEvidence.ForegroundMatchedAtSend -and [bool]$inputEvidence.SendInputSucceeded -and
        -not [bool]$inputEvidence.PostMessageFallbackUsed -and [bool]$inputEvidence.CompletedBeforeDeadline
    $receipt = [ordered]@{
        Sequence = $InputReceipts.Count + 1
        Purpose = $Purpose
        HandshakeState = $State
        HandshakeLogOffset = [int64]$Offset
        HandshakeObserved = $HandshakeObserved
        Key = $Key
        HoldMilliseconds = $HoldMilliseconds
        Sent = [bool]$sent
        DolocTownPid = [int]$inputEvidence.DolocTownPid
        ForegroundPidAtSend = [int]$inputEvidence.ForegroundPidAtSend
        ForegroundMatchedAtSend = [bool]$inputEvidence.ForegroundMatchedAtSend
        SendInputSucceeded = [bool]$inputEvidence.SendInputSucceeded
        PostMessageFallbackUsed = [bool]$inputEvidence.PostMessageFallbackUsed
        KeyDownAt = $inputEvidence.KeyDownAt
        KeyUpAt = $inputEvidence.KeyUpAt
        InputLogOffset = [int64]$inputEvidence.LogOffsetAtSend
        CompletedBeforeDeadline = [bool]$inputEvidence.CompletedBeforeDeadline
        ProvenancePassed = [bool]$provenancePassed
    }
    [void]$InputReceipts.Add($receipt)
    return [pscustomobject]$receipt
}

function Send-Batch6AutoFishingF6AfterHandshake {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [string] $State,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $HandshakeReceipts,
        [Parameter(Mandatory = $true)] [System.Collections.IList] $InputReceipts,
        [ValidateSet('F6','F7','A')] [string] $Key = 'F6',
        [string] $HookId = 'Smoke.Batch6.AutoFishingPilot.Handshake'
    )

    $handshake = Wait-Batch6AutoFishingHandshakeState -LogPath $LogPath -Offset $Offset -State $State -TimeoutSeconds $TimeoutSeconds -Receipts $HandshakeReceipts -HookId $HookId
    $purpose = if ([string]::Equals($Key, 'A', [System.StringComparison]::Ordinal)) { 'ManualMovement' } else { 'ConfiguredToggle' }
    return Send-Batch6AutoFishingRecordedKey -LogPath $LogPath -Offset $Offset -State $State -TimeoutSeconds $TimeoutSeconds `
        -InputReceipts $InputReceipts -HandshakeObserved ([bool]$handshake.Observed) -Key $Key -Purpose $purpose
}

function Send-DolocTownMouseClick {
    param(
        [ValidateSet('Left', 'Right')] [string] $Button = 'Left',
        [Parameter(Mandatory = $true)] [int] $ClientX,
        [Parameter(Mandatory = $true)] [int] $ClientY,
        [int] $TimeoutSeconds = 20,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $effectiveDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $effectiveDeadline)) {
        return $false
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds -Deadline $effectiveDeadline
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

    $cursorReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -Action {
        return [DtmApiSmokeInput]::SetCursorPos($point.X, $point.Y)
    }
    if (-not [bool]$cursorReceipt.Accepted -or -not (Start-SmokeCappedSleep -Deadline $effectiveDeadline -Milliseconds 160)) {
        return $false
    }
    $mouseHoldMilliseconds = if ($Button -eq 'Right') { 320 } else { 180 }
    $mouseReceipt = if ($Button -eq 'Right') {
        Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
            return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0008, [uint32]0x0010, $mouseHoldMilliseconds)
        }
    }
    else {
        Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
            return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0002, [uint32]0x0004, $mouseHoldMilliseconds)
        }
    }
    return [bool]$mouseReceipt.Accepted
}

function Send-DolocTownNormalizedMouseClick {
    param(
        [ValidateSet('Left', 'Right')] [string] $Button = 'Left',
        [Parameter(Mandatory = $true)] [ValidateRange(0.0, 1.0)] [double] $NormalizedX,
        [Parameter(Mandatory = $true)] [ValidateRange(0.0, 1.0)] [double] $NormalizedY,
        [int] $TimeoutSeconds = 20,
        [string] $InputLogPath = '',
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $script:lastDolocTownMouseInputEvidence = [ordered]@{
        DolocTownPid = 0
        ForegroundPidAtSend = 0
        ForegroundMatchedAtSend = $false
        SendInputSucceeded = $false
        PostMessageFallbackUsed = $false
        SetCursorPosSucceeded = $false
        NormalizedX = $NormalizedX
        NormalizedY = $NormalizedY
        ClientX = -1
        ClientY = -1
        ClientWidth = 0
        ClientHeight = 0
        SentAtUtc = $null
        LogOffsetAtSend = [int64]0
        CompletedBeforeDeadline = $false
    }
    $effectiveDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $TimeoutSeconds) * 1000)
    if (-not (Test-SmokeDeadlineHasBudget -Deadline $effectiveDeadline)) {
        return $false
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $TimeoutSeconds -Deadline $effectiveDeadline
    if (-not $proc) {
        return $false
    }

    $foregroundWindow = [DtmApiSmokeInput]::GetForegroundWindow()
    $foregroundPid = 0
    if ($foregroundWindow -ne [IntPtr]::Zero) {
        [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foregroundWindow, [ref]$foregroundPid)
    }
    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return $false
    }
    $clientWidth = $rect.Right - $rect.Left
    $clientHeight = $rect.Bottom - $rect.Top
    if ($clientWidth -le 0 -or $clientHeight -le 0) {
        return $false
    }
    $clientX = [Math]::Max(0, [Math]::Min($clientWidth - 1, [int][Math]::Round(($clientWidth - 1) * $NormalizedX)))
    $clientY = [Math]::Max(0, [Math]::Min($clientHeight - 1, [int][Math]::Round(($clientHeight - 1) * $NormalizedY)))
    $point = New-Object DtmApiSmokePoint
    $point.X = $clientX
    $point.Y = $clientY
    if (-not [DtmApiSmokeInput]::ClientToScreen($proc.MainWindowHandle, [ref]$point)) {
        return $false
    }

    $logOffsetAtSend = if ($InputLogPath -and (Test-Path -LiteralPath $InputLogPath -PathType Leaf)) { [int64](Get-Item -LiteralPath $InputLogPath).Length } else { [int64]0 }
    $cursorReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -Action {
        return [DtmApiSmokeInput]::SetCursorPos($point.X, $point.Y)
    }
    $cursorSet = [bool]$cursorReceipt.Accepted
    $mouseHoldMilliseconds = if ($Button -eq 'Right') { 320 } else { 180 }
    $mouseReceipt = $null
    if ($cursorSet -and (Start-SmokeCappedSleep -Deadline $effectiveDeadline -Milliseconds 160)) {
        $mouseReceipt = if ($Button -eq 'Right') {
            Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
                return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0008, [uint32]0x0010, $mouseHoldMilliseconds)
            }
        }
        else {
            Invoke-SmokeDeadlineGuardedAction -Deadline $effectiveDeadline -MinimumExecutionBudgetMilliseconds ($mouseHoldMilliseconds + 50) -Action {
                return [DtmApiSmokeInput]::SendMouseButton([uint32]0x0002, [uint32]0x0004, $mouseHoldMilliseconds)
            }
        }
    }
    $sentAtUtc = if ($null -ne $mouseReceipt) { $mouseReceipt.StartedAt.ToUniversalTime().ToString('o') } else { $null }
    $sentInput = $null -ne $mouseReceipt -and [bool]$mouseReceipt.Accepted
    $script:lastDolocTownMouseInputEvidence = [ordered]@{
        DolocTownPid = [int]$proc.Id
        ForegroundPidAtSend = [int]$foregroundPid
        ForegroundMatchedAtSend = [bool]($foregroundPid -eq $proc.Id)
        SendInputSucceeded = [bool]$sentInput
        PostMessageFallbackUsed = $false
        SetCursorPosSucceeded = [bool]$cursorSet
        NormalizedX = $NormalizedX
        NormalizedY = $NormalizedY
        ClientX = $clientX
        ClientY = $clientY
        ClientWidth = $clientWidth
        ClientHeight = $clientHeight
        SentAtUtc = $sentAtUtc
        LogOffsetAtSend = [int64]$logOffsetAtSend
        CompletedBeforeDeadline = $null -ne $mouseReceipt -and [bool]$mouseReceipt.CompletedBeforeDeadline
    }
    return [bool]$sentInput
}

function Send-DolocTownF6 {
    param(
        [int] $TimeoutSeconds = 20,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    return Send-DolocTownNamedKey -Key 'F6' -TimeoutSeconds $TimeoutSeconds -Deadline $Deadline
}

function Invoke-DolocTownHoverSweep {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [int64] $LogOffset,
        [Parameter(Mandatory = $true)] [string] $Pattern,
        [Parameter(Mandatory = $true)] [datetime] $Deadline
    )

    $receipt = [ordered]@{
        DolocTownPid = 0
        ForegroundMatched = $false
        MoveOnly = $true
        MouseClickSent = $false
        PointsVisited = 0
        MatchedExpectedLog = $false
        ClientX = -1
        ClientY = -1
        ClientWidth = 0
        ClientHeight = 0
    }
    $remaining = Get-SmokeRemainingBudgetSeconds -Deadline $Deadline -MaximumSeconds 20
    if ($remaining -le 0) {
        return [PSCustomObject]$receipt
    }
    $proc = Get-DolocTownInputProcess -TimeoutSeconds $remaining -Deadline $Deadline
    if (-not $proc) {
        return [PSCustomObject]$receipt
    }

    $receipt.DolocTownPid = [int]$proc.Id
    $foregroundWindow = [DtmApiSmokeInput]::GetForegroundWindow()
    $foregroundPid = 0
    if ($foregroundWindow -ne [IntPtr]::Zero) {
        [void][DtmApiSmokeInput]::GetWindowThreadProcessId($foregroundWindow, [ref]$foregroundPid)
    }
    $receipt.ForegroundMatched = [bool]($foregroundPid -eq $proc.Id)
    if (-not $receipt.ForegroundMatched) {
        return [PSCustomObject]$receipt
    }

    $rect = New-Object DtmApiSmokeRect
    if (-not [DtmApiSmokeInput]::GetClientRect($proc.MainWindowHandle, [ref]$rect)) {
        return [PSCustomObject]$receipt
    }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $receipt.ClientWidth = $width
    $receipt.ClientHeight = $height
    $minX = [int][Math]::Max(12, [Math]::Floor($width * 0.18))
    $maxX = [int][Math]::Min($width - 12, [Math]::Ceiling($width * 0.82))
    $minY = [int][Math]::Max(12, [Math]::Floor($height * 0.18))
    $maxY = [int][Math]::Min($height - 12, [Math]::Ceiling($height * 0.88))

    for ($y = $minY; $y -le $maxY -and (Test-SmokeDeadlineHasBudget -Deadline $Deadline); $y += 44) {
        for ($x = $minX; $x -le $maxX -and (Test-SmokeDeadlineHasBudget -Deadline $Deadline); $x += 44) {
            $point = New-Object DtmApiSmokePoint
            $point.X = $x
            $point.Y = $y
            if ([DtmApiSmokeInput]::ClientToScreen($proc.MainWindowHandle, [ref]$point)) {
                $cursorReceipt = Invoke-SmokeDeadlineGuardedAction -Deadline $Deadline -Action {
                    return [DtmApiSmokeInput]::SetCursorPos($point.X, $point.Y)
                }
                if (-not [bool]$cursorReceipt.Accepted) {
                    return [PSCustomObject]$receipt
                }
                $receipt.PointsVisited++
                $receipt.ClientX = $x
                $receipt.ClientY = $y
                if (-not (Start-SmokeCappedSleep -Deadline $Deadline -Milliseconds 55)) {
                    return [PSCustomObject]$receipt
                }
                $tail = Get-LogTextAfterOffset -LogPath $LogPath -Offset $LogOffset
                if ((Test-SmokeDeadlineHasBudget -Deadline $Deadline) -and $tail.IndexOf($Pattern, [System.StringComparison]::Ordinal) -ge 0) {
                    $receipt.MatchedExpectedLog = $true
                    return [PSCustomObject]$receipt
                }
            }
        }
    }
    return [PSCustomObject]$receipt
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
    if ($clientWidth -le 0 -or $clientHeight -le 0) {
        return $null
    }

    # Unity can render below the Win32 client resolution.  Use the QA-owned
    # product screenshot as the render-space receipt when it is available,
    # then project the real first-cell centre back into client coordinates.
    # Falling back to the client dimensions preserves the non-QA smoke route.
    $renderWidth = $clientWidth
    $renderHeight = $clientHeight
    $coordinateSource = 'client-fallback'
    $screenshotPath = Join-Path $evidence 'qa-host/g4/ui/debug-console.png'
    $screenshotDeadline = (Get-Date).AddSeconds(5)
    do {
        if (Test-Path -LiteralPath $screenshotPath -PathType Leaf) {
            try {
                $stream = [System.IO.File]::OpenRead($screenshotPath)
                try {
                    $header = New-Object byte[] 24
                    if ($stream.Read($header, 0, $header.Length) -eq $header.Length -and
                        $header[0] -eq 0x89 -and $header[1] -eq 0x50 -and
                        $header[2] -eq 0x4E -and $header[3] -eq 0x47) {
                        $pngWidth = [System.Net.IPAddress]::NetworkToHostOrder(
                            [System.BitConverter]::ToInt32($header, 16))
                        $pngHeight = [System.Net.IPAddress]::NetworkToHostOrder(
                            [System.BitConverter]::ToInt32($header, 20))
                        if ($pngWidth -gt 0 -and $pngHeight -gt 0) {
                            $renderWidth = $pngWidth
                            $renderHeight = $pngHeight
                            $coordinateSource = 'qa-screenshot-render-space'
                            break
                        }
                    }
                }
                finally {
                    $stream.Dispose()
                }
            }
            catch {
                # The screenshot flush is asynchronous; retry until the short
                # receipt deadline before using the client-space fallback.
            }
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $screenshotDeadline)

    $scale = [Math]::Sqrt([Math]::Max(
        0.0001,
        ($renderWidth / 1920.0) * ($renderHeight / 1080.0)))
    $physicalInset = 24.0 * $scale
    $innerWidth = [Math]::Max(1.0, $renderWidth - 2.0 * $physicalInset)
    $innerHeight = [Math]::Max(1.0, $renderHeight - 2.0 * $physicalInset)
    $availableLogicalWidth = $innerWidth / $scale
    $availableLogicalHeight = $innerHeight / $scale
    $wide = [Math]::Min($availableLogicalWidth, $innerWidth) -ge 1500.0
    $logicalWidth = if ($wide) {
        [Math]::Min($availableLogicalWidth, 1500.0)
    }
    else {
        $availableLogicalWidth
    }
    $logicalHeight = if ($wide) {
        [Math]::Min($availableLogicalHeight, 820.0)
    }
    else {
        $availableLogicalHeight
    }
    $panelWidth = $logicalWidth * $scale
    $panelHeight = $logicalHeight * $scale
    $panelLeft = $physicalInset + (($innerWidth - $panelWidth) / 2.0)
    $panelTop = $physicalInset + (($innerHeight - $panelHeight) / 2.0)
    $itemCenterRenderX = $panelLeft + (388.0 + 42.0) * $scale
    $itemCenterFromPanelTop = if ($wide) { 202.0 } else { 248.0 }
    $itemCenterRenderY = $panelTop + $itemCenterFromPanelTop * $scale
    $clientX = [int][Math]::Round(
        ($itemCenterRenderX / $renderWidth) * $clientWidth)
    $clientY = [int][Math]::Round(
        ($itemCenterRenderY / $renderHeight) * $clientHeight)

    return [PSCustomObject]@{
        ClientX = $clientX
        ClientY = $clientY
        ClientWidth = $clientWidth
        ClientHeight = $clientHeight
        PanelWidth = [int][Math]::Round($panelWidth)
        PanelHeight = [int][Math]::Round($panelHeight)
        RenderWidth = $renderWidth
        RenderHeight = $renderHeight
        CoordinateSource = $coordinateSource
        Breakpoint = if ($wide) { 'Wide' } else { 'TabsOrCompact' }
    }
}

function Invoke-DebugConsoleSmokeKey {
    param(
        [string] $Label,
        [int] $VirtualKey,
        [string] $Pattern,
        [int] $MinimumCount,
        [int] $Attempts = 1,
        [int] $WaitSeconds = 5,
        [int] $HoldMilliseconds = 0,
        [datetime] $Deadline = [datetime]::MaxValue
    )

    $strictPlayerInputGate = [bool]$RequireExternalPlayerInputGate -or [bool]$AssertNoQaUiEvidence -or [bool]$qaG4DebugConsoleEnabled
    $effectiveHoldMilliseconds = if ($HoldMilliseconds -gt 0) { $HoldMilliseconds } elseif ($strictPlayerInputGate) { $ExternalPlayerInputHoldMilliseconds } else { 260 }
    if ($strictPlayerInputGate -and $Attempts -ne 1) {
        throw "The strict external player-input gate permits exactly one send attempt per label; requested label=$Label attempts=$Attempts."
    }

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        if (-not (Test-SmokeDeadlineHasBudget -Deadline $Deadline)) {
            break
        }
        $attemptStartedAt = (Get-Date).ToUniversalTime().ToString('o')
        $sent = Send-DolocTownKey -VirtualKey $VirtualKey -Name $Label -HoldMilliseconds $effectiveHoldMilliseconds -InputLogPath $logPath -Deadline $Deadline
        $sendEvidence = $script:lastDolocTownInputEvidence
        "SentExternalDebugConsole${Label}Attempt$attempt=$(Get-Date -Format o);ok=$sent;holdMilliseconds=$effectiveHoldMilliseconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        $matchedExpectedLog = if (-not $sent) {
            $false
        }
        elseif ($strictPlayerInputGate) {
            $waitDeadline = Get-SmokeCappedDeadline -Deadline $Deadline -MaximumMilliseconds ([Math]::Max(0, $WaitSeconds) * 1000)
            Wait-ForLogLineAfterOffsetUntilDeadline -LogPath $logPath -Offset ([int64]$sendEvidence.LogOffsetAtSend) -Pattern $Pattern -Deadline $waitDeadline
        }
        else {
            Wait-ForLogLineCount -LogPath $logPath -Pattern $Pattern -MinimumCount $MinimumCount -TimeoutSeconds $WaitSeconds
        }
        if ($strictPlayerInputGate) {
            $externalPlayerInputAttempts.Add([ordered]@{
                Label = $Label
                Attempt = $attempt
                Timestamp = [string]$sendEvidence.KeyDownAt
                AttemptStartedAt = $attemptStartedAt
                KeyDownAt = [string]$sendEvidence.KeyDownAt
                KeyUpAt = [string]$sendEvidence.KeyUpAt
                VirtualKey = $VirtualKey
                HoldMilliseconds = $effectiveHoldMilliseconds
                LogOffset = [int64]$sendEvidence.LogOffsetAtSend
                Sent = [bool]$sent
                MatchedExpectedLog = [bool]$matchedExpectedLog
                DolocTownPid = [int]$sendEvidence.DolocTownPid
                ForegroundPid = [int]$sendEvidence.ForegroundPidAtSend
                ForegroundMatched = [bool]$sendEvidence.ForegroundMatchedAtSend
                SendInputSucceeded = [bool]$sendEvidence.SendInputSucceeded
                CompletedBeforeDeadline = [bool]$sendEvidence.CompletedBeforeDeadline
                PostMessageFallbackUsed = [bool]$sendEvidence.PostMessageFallbackUsed
            }) | Out-Null
        }
        if ($matchedExpectedLog) {
            return $true
        }
        if ($strictPlayerInputGate -and $sent) {
            break
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

    $leftPattern = 'DebugConsole action=item-give success=True owner=ProductNative .* Give item owner=DTMAPI\.DebugConsoleMod .* requested=1 given=1 '
    $rightPattern = 'DebugConsole action=item-give success=True owner=ProductNative .* Give item owner=DTMAPI\.DebugConsoleMod .* requested=10 given=[1-9][0-9]* '
    $leftBefore = Get-LogRegexCount -LogPath $logPath -Pattern $leftPattern
    $rightBefore = Get-LogRegexCount -LogPath $logPath -Pattern $rightPattern

    "DebugConsoleMouseGivePoint=$(Get-Date -Format o);clientX=$($point.ClientX);clientY=$($point.ClientY);clientWidth=$($point.ClientWidth);clientHeight=$($point.ClientHeight);renderWidth=$($point.RenderWidth);renderHeight=$($point.RenderHeight);panelWidth=$($point.PanelWidth);panelHeight=$($point.PanelHeight);coordinateSource=$($point.CoordinateSource);breakpoint=$($point.Breakpoint)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
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
