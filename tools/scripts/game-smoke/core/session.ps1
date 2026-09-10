function Wait-SmokeLogLine {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds = 120,
        [switch] $Regex,
        [switch] $AbortOnFatalInstanceWindow,
        [datetime] $Deadline = [datetime]::MaxValue
    )
    $remaining = Get-SmokeRemainingBudgetSeconds -Deadline $Deadline -MaximumSeconds $TimeoutSeconds
    if ($remaining -le 0) { return $false }
    $observed = Wait-ForLogLine -LogPath $LogPath -Pattern $Pattern -TimeoutSeconds $remaining `
        -Regex:$Regex -AbortOnFatalInstanceWindow:$AbortOnFatalInstanceWindow
    return $observed -and (Test-SmokeDeadlineHasBudget -Deadline $Deadline)
}

function Test-SmokeDeadlineHasBudget {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [datetime] $ObservedAt = (Get-Date)
    )

    return $ObservedAt -lt $Deadline
}

function Get-SmokeRemainingBudgetMilliseconds {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [int] $MaximumMilliseconds = [int]::MaxValue,
        [datetime] $ObservedAt = (Get-Date)
    )

    if ($MaximumMilliseconds -le 0 -or -not (Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $ObservedAt)) {
        return 0
    }

    $remainingMilliseconds = [Math]::Floor(($Deadline - $ObservedAt).TotalMilliseconds)
    if ($remainingMilliseconds -le 0) {
        return 0
    }

    return [int][Math]::Min([double]$MaximumMilliseconds, $remainingMilliseconds)
}

function Wait-SmokeManualExit {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [scriptblock] $ProcessQuery = { @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue) },
        [scriptblock] $FatalWindowQuery = { @(Get-FatalInstanceWindows) },
        [scriptblock] $Clock = { Get-Date },
        [scriptblock] $Sleep = { param($Milliseconds) Start-Sleep -Milliseconds $Milliseconds },
        [ValidateRange(1,2000)] [int] $PollMilliseconds = 2000
    )
    while ((& $Clock) -lt $Deadline) {
        $processes=@(& $ProcessQuery)
        if($processes.Count -eq 0) {
            $observedAt=& $Clock
            if($observedAt -lt $Deadline) {
                return [pscustomobject]@{Requested=$true;Passed=$true;Reason='ExitObservedBeforeDeadline';TimedOut=$false;Deadline=$Deadline.ToString('o');ObservedAt=$observedAt.ToString('o');FatalWindows=@();FatalDetectedAt=$null}
            }
            break
        }
        $fatal=@(& $FatalWindowQuery)
        if($fatal.Count -gt 0) {
            $observedAt=& $Clock
            return [pscustomobject]@{Requested=$true;Passed=$false;Reason='ManualExitInterruptedByFatalWindow';TimedOut=$false;Deadline=$Deadline.ToString('o');ObservedAt=$observedAt.ToString('o');FatalWindows=$fatal;FatalDetectedAt=$observedAt}
        }
        $remaining=($Deadline-(& $Clock)).TotalMilliseconds
        if($remaining -le 0){break}
        & $Sleep ([int][Math]::Min($PollMilliseconds,[Math]::Ceiling($remaining)))
    }
    return [pscustomobject]@{Requested=$true;Passed=$false;Reason='ManualExitTimeout';TimedOut=$true;Deadline=$Deadline.ToString('o');ObservedAt=(& $Clock).ToString('o');FatalWindows=@();FatalDetectedAt=$null}
}

function Get-SmokeRemainingBudgetSeconds {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [int] $MaximumSeconds = [int]::MaxValue,
        [datetime] $ObservedAt = (Get-Date)
    )

    $remainingSeconds = [Math]::Ceiling(($Deadline - $ObservedAt).TotalSeconds)
    if ($remainingSeconds -le 0 -or $MaximumSeconds -le 0) {
        return 0
    }

    return [int][Math]::Min($remainingSeconds, $MaximumSeconds)
}

function Get-SmokeCappedDeadline {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [Parameter(Mandatory = $true)] [int] $MaximumMilliseconds,
        [datetime] $ObservedAt = (Get-Date)
    )

    if ($MaximumMilliseconds -le 0 -or -not (Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $ObservedAt)) {
        return $ObservedAt
    }

    $candidate = $ObservedAt.AddMilliseconds($MaximumMilliseconds)
    if ($candidate -lt $Deadline) {
        return $candidate
    }

    return $Deadline
}

function Invoke-SmokeDeadlineGuardedAction {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [Parameter(Mandatory = $true)] [scriptblock] $Action,
        [ValidateRange(1, [int]::MaxValue)] [int] $MinimumExecutionBudgetMilliseconds = 1,
        [scriptblock] $Clock = { Get-Date }
    )

    $startedAt = [datetime](& $Clock)
    $remainingAtStart = Get-SmokeRemainingBudgetMilliseconds -Deadline $Deadline -ObservedAt $startedAt
    if ($remainingAtStart -lt $MinimumExecutionBudgetMilliseconds) {
        return [pscustomobject]@{
            Executed = $false
            Result = $false
            StartedAt = $startedAt
            CompletedAt = $startedAt
            RemainingAtStartMilliseconds = $remainingAtStart
            CompletedBeforeDeadline = $false
            Accepted = $false
            Reason = if ($remainingAtStart -le 0) { 'expired-at-entry' } else { 'insufficient-budget' }
        }
    }

    $result = & $Action
    $completedAt = [datetime](& $Clock)
    $completedBeforeDeadline = Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $completedAt
    return [pscustomobject]@{
        Executed = $true
        Result = $result
        StartedAt = $startedAt
        CompletedAt = $completedAt
        RemainingAtStartMilliseconds = $remainingAtStart
        CompletedBeforeDeadline = $completedBeforeDeadline
        Accepted = $completedBeforeDeadline -and [bool]$result
        Reason = if ($completedBeforeDeadline) { 'completed-before-deadline' } else { 'completed-after-deadline' }
    }
}

function Start-SmokeCappedSleep {
    param(
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [Parameter(Mandatory = $true)] [ValidateRange(1, [int]::MaxValue)] [int] $Milliseconds
    )

    $sleepMilliseconds = Get-SmokeRemainingBudgetMilliseconds -Deadline $Deadline -MaximumMilliseconds $Milliseconds
    if ($sleepMilliseconds -le 0) {
        return $false
    }

    Start-Sleep -Milliseconds $sleepMilliseconds
    return Test-SmokeDeadlineHasBudget -Deadline $Deadline
}

function Test-SmokeNoQaDeadlineContract {
    $origin = [datetime]::SpecifyKind([datetime]'2026-07-19T00:00:00', [System.DateTimeKind]::Utc)

    $expiredCalls = New-Object 'System.Collections.Generic.List[int]'
    $expiredClock = { return $origin }.GetNewClosure()
    $expiredAction = { $expiredCalls.Add(1) | Out-Null; return $true }.GetNewClosure()
    $expired = Invoke-SmokeDeadlineGuardedAction -Deadline $origin -Action $expiredAction -Clock $expiredClock

    $shortCalls = New-Object 'System.Collections.Generic.List[int]'
    $shortClock = { return $origin }.GetNewClosure()
    $shortAction = { $shortCalls.Add(1) | Out-Null; return $true }.GetNewClosure()
    $short = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $origin.AddMilliseconds(20) `
        -Action $shortAction `
        -MinimumExecutionBudgetMilliseconds 40 `
        -Clock $shortClock

    $lateClockValues = New-Object 'System.Collections.Generic.Queue[datetime]'
    $lateClockValues.Enqueue($origin)
    $lateClockValues.Enqueue($origin.AddMilliseconds(126))
    $lateClock = { return $lateClockValues.Dequeue() }.GetNewClosure()
    $lateCalls = New-Object 'System.Collections.Generic.List[int]'
    $lateAction = { $lateCalls.Add(1) | Out-Null; return $true }.GetNewClosure()
    $late = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $origin.AddMilliseconds(125) `
        -Action $lateAction `
        -Clock $lateClock

    $onTimeClockValues = New-Object 'System.Collections.Generic.Queue[datetime]'
    $onTimeClockValues.Enqueue($origin)
    $onTimeClockValues.Enqueue($origin.AddMilliseconds(124))
    $onTimeClock = { return $onTimeClockValues.Dequeue() }.GetNewClosure()
    $onTime = Invoke-SmokeDeadlineGuardedAction `
        -Deadline $origin.AddMilliseconds(125) `
        -Action { return $true } `
        -Clock $onTimeClock

    $cappedMilliseconds = Get-SmokeRemainingBudgetMilliseconds `
        -Deadline $origin.AddMilliseconds(125) `
        -MaximumMilliseconds 5000 `
        -ObservedAt $origin
    $passed = -not [bool]$expired.Executed -and $expiredCalls.Count -eq 0 -and
        [string]$expired.Reason -eq 'expired-at-entry' -and
        -not [bool]$short.Executed -and $shortCalls.Count -eq 0 -and
        [string]$short.Reason -eq 'insufficient-budget' -and
        [bool]$late.Executed -and $lateCalls.Count -eq 1 -and
        -not [bool]$late.CompletedBeforeDeadline -and -not [bool]$late.Accepted -and
        [bool]$onTime.Executed -and [bool]$onTime.CompletedBeforeDeadline -and [bool]$onTime.Accepted -and
        $cappedMilliseconds -eq 125
    return [pscustomobject]@{
        ExpiredAtEntryDidNotExecute = -not [bool]$expired.Executed -and $expiredCalls.Count -eq 0
        ShortBudgetDidNotExecute = -not [bool]$short.Executed -and $shortCalls.Count -eq 0
        LateCompletionRejected = [bool]$late.Executed -and -not [bool]$late.Accepted
        OnTimeCompletionAccepted = [bool]$onTime.Accepted
        RequestedWaitMilliseconds = 5000
        CappedWaitMilliseconds = $cappedMilliseconds
        Passed = $passed
    }
}

function Wait-SmokeProcessExitBeforeRecovery {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [ValidateRange(1, 60)] [int] $TimeoutSeconds = 15,
        [scriptblock] $ProcessQuery = { Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue },
        [scriptblock] $Clock = { Get-Date },
        [scriptblock] $Sleep = { param($Milliseconds) Start-Sleep -Milliseconds $Milliseconds },
        [scriptblock] $Close = { param($Process) $Process.CloseMainWindow() }
    )

    $initialProcesses = @(& $ProcessQuery)
    $initialProcessIds = @($initialProcesses | ForEach-Object { [int]$_.Id })
    $gracefulCloseRequested = $false
    foreach ($process in $initialProcesses) {
        try {
            if ((& $Close $process)) {
                $gracefulCloseRequested = $true
            }
        }
        catch {
        }
    }

    $deadline = ([datetime](& $Clock)).AddSeconds($TimeoutSeconds)
    $remainingProcesses = @($initialProcesses)
    $stableAbsenceSeconds = 1.0
    $absenceObservedAt = $null
    while (([datetime](& $Clock)) -lt $deadline) {
        $remainingProcesses = @(& $ProcessQuery)
        if ($remainingProcesses.Count -eq 0) {
            if ($null -eq $absenceObservedAt) {
                $absenceObservedAt = [datetime](& $Clock)
            }
            elseif ((([datetime](& $Clock)) - $absenceObservedAt).TotalSeconds -ge $stableAbsenceSeconds) {
                break
            }
        }
        else {
            $absenceObservedAt = $null
        }
        & $Sleep 250
    }
    $remainingProcesses = @(& $ProcessQuery)
    $stableAbsenceObserved = $remainingProcesses.Count -eq 0 -and
        $null -ne $absenceObservedAt -and
        (([datetime](& $Clock)) - $absenceObservedAt).TotalSeconds -ge $stableAbsenceSeconds

    $result = [ordered]@{
        CheckedAt = ([datetime](& $Clock)).ToString('o')
        TimeoutSeconds = $TimeoutSeconds
        InitialProcessIds = @($initialProcessIds)
        GracefulCloseRequested = $gracefulCloseRequested
        RequiredStableAbsenceSeconds = $stableAbsenceSeconds
        StableAbsenceObserved = $stableAbsenceObserved
        RemainingProcessIds = @($remainingProcesses | ForEach-Object { [int]$_.Id })
        Exited = $stableAbsenceObserved
    }
    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'process-exit-before-state-restore.json') -Value $result
    return [pscustomobject]$result
}

function Wait-ForStartupLogWithTimeline {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds,
        [string] $EvidenceDir,
        [datetime] $LaunchCommandStartedAt,
        [datetime] $LaunchCommandFinishedAt,
        [string] $LaunchMode,
        [datetime] $AbsoluteDeadline = [datetime]::MaxValue
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
    if ($AbsoluteDeadline -lt $deadline) {
        $deadline = $AbsoluteDeadline
    }
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
            if ((Test-SmokeDeadlineHasBudget -Deadline $deadline) -and $text -match [regex]::Escape($Pattern)) {
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

function Get-LogTextAfterOffset {
    param(
        [string] $LogPath,
        [int64] $Offset
    )

    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
        return ''
    }

    $stream = $null
    $reader = $null
    try {
        $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
        $stream = [System.IO.File]::Open($LogPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $share)
        $safeOffset = [Math]::Min([Math]::Max([int64]0, $Offset), $stream.Length)
        [void]$stream.Seek($safeOffset, [System.IO.SeekOrigin]::Begin)
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8, $true, 4096, $true)
        return $reader.ReadToEnd()
    }
    finally {
        if ($reader) { $reader.Dispose() }
        if ($stream) { $stream.Dispose() }
    }
}

function Wait-ForLogLineAfterOffsetUntilDeadline {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [string] $Pattern,
        [Parameter(Mandatory = $true)] [int64] $Offset,
        [Parameter(Mandatory = $true)] [datetime] $Deadline,
        [switch] $AbortOnFatalInstanceWindow
    )

    while (Test-SmokeDeadlineHasBudget -Deadline $Deadline) {
        $text = Get-LogTextAfterOffset -LogPath $LogPath -Offset $Offset
        $observedAt = Get-Date
        if (-not (Test-SmokeDeadlineHasBudget -Deadline $Deadline -ObservedAt $observedAt)) {
            return $false
        }
        if ($text.IndexOf($Pattern, [System.StringComparison]::Ordinal) -ge 0) {
            return $true
        }
        if ($text -match 'QA host lifecycle .*state=failed' -or
            $text -match 'QA scenario .* FAILED' -or
            $text -match 'InternalFixture\.QaHost = failed-closed') {
            return $false
        }
        if ($AbortOnFatalInstanceWindow -and (Test-FatalInstanceWindow)) {
            return $false
        }
        if (-not (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1)) {
            return $false
        }
        if (-not (Start-SmokeCappedSleep -Deadline $Deadline -Milliseconds 250)) {
            return $false
        }
    }

    return $false
}

function Wait-ForAutoFishingLogLine {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $LogPath) {
            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            if ($text -match [regex]::Escape($Pattern)) {
                return $true
            }
            if ($text -match 'Hook status: Smoke\.AutoFishing[^=]* = failed\.') {
                return $false
            }
        }

        if (Test-FatalInstanceWindow) {
            return $false
        }

        if (-not (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
            return $false
        }

        Start-Sleep -Seconds 1
    }

    return $false
}

function Wait-ForAutoFishingPerformanceTerminalStatus {
    param(
        [string] $LogPath,
        [int] $TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (Test-SmokeDeadlineHasBudget -Deadline $deadline) {
        if (Test-Path $LogPath) {
            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            $match = [regex]::Match($text, 'Hook status: Smoke\.AutoFishingPerformance = (verified|blocked)\.')
            if ($match.Success) {
                return $match.Groups[1].Value
            }
            if ($text -match 'Hook status: Smoke\.AutoFishingPerformance = failed\.') {
                return 'failed'
            }
        }

        if (Test-FatalInstanceWindow) {
            return 'failed'
        }
        if (-not (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
            return 'failed'
        }
        if (-not (Start-SmokeCappedSleep -Deadline $deadline -Milliseconds 1000)) {
            break
        }
    }

    return 'failed'
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
