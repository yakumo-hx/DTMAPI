param(
    [int] $MaxBatches = 12,
    [int] $BatchSize = 6,
    [int] $TimeoutSeconds = 70,
    [int] $DelaySeconds = 5,
    [int] $AutoExitAfterSeconds = 20,
    [int] $DelaySecondsBetweenBatches = 30,
    [int] $SlowLaunchThresholdMs = 30000,
    [int] $SlowRuntimeThresholdMs = 30000,
    [int] $MaxMinutes = 0,
    [switch] $IncludeTitleSettingsMenu,
    [switch] $SkipBuild,
    [switch] $FailOnTrigger
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ($MaxBatches -lt 1) { throw 'MaxBatches must be at least 1.' }
if ($BatchSize -lt 1) { throw 'BatchSize must be at least 1.' }
if ($TimeoutSeconds -lt 1) { throw 'TimeoutSeconds must be at least 1.' }
if ($AutoExitAfterSeconds -lt 1) { throw 'AutoExitAfterSeconds must be at least 1.' }

$monitorRoot = New-EvidenceDir -RepoRoot $repo -CaseId 'STARTUP-MONITOR'
$batches = New-Object System.Collections.Generic.List[object]
$startedAt = Get-Date
$triggered = $false
$stopReason = $null

"Started=$($startedAt.ToString('o'))`nMaxBatches=$MaxBatches`nBatchSize=$BatchSize`nTimeoutSeconds=$TimeoutSeconds`nDelaySeconds=$DelaySeconds`nAutoExitAfterSeconds=$AutoExitAfterSeconds`nDelaySecondsBetweenBatches=$DelaySecondsBetweenBatches`nSlowLaunchThresholdMs=$SlowLaunchThresholdMs`nSlowRuntimeThresholdMs=$SlowRuntimeThresholdMs`nMaxMinutes=$MaxMinutes`nIncludeTitleSettingsMenu=$IncludeTitleSettingsMenu`nSkipBuild=$SkipBuild" |
    Set-Content -LiteralPath (Join-Path $monitorRoot 'summary.txt')

function Get-SampleEvidenceRootFromOutput {
    param(
        [string] $Text
    )

    $matches = [regex]::Matches($Text, '([A-Za-z]:\\[^\r\n]+\\docs\\debug\\evidence\\STARTUP-SAMPLES\\\d{8}-\d{6})')
    if ($matches.Count -gt 0) {
        return $matches[$matches.Count - 1].Groups[1].Value.Trim()
    }

    return $null
}

function Read-JsonIfExists {
    param(
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    try {
        return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Get-IntProperty {
    param(
        [object] $Object,
        [string] $Name
    )

    if ($null -eq $Object) {
        return 0
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($property -and $null -ne $property.Value) {
        return [int]$property.Value
    }

    return 0
}

function Get-BatchTriggerReasons {
    param(
        [int] $ExitCode,
        [bool] $EvidenceExists,
        [object] $SampleSummary,
        [object] $StartupAnalysis,
        [object[]] $Samples
    )

    $reasons = New-Object System.Collections.Generic.List[string]

    if ($ExitCode -ne 0) { $reasons.Add("sample-run-exit-code-$ExitCode") }
    if (-not $EvidenceExists) { $reasons.Add('missing-startup-samples-evidence') }

    if ((Get-IntProperty -Object $SampleSummary -Name 'SlowLaunchCount') -gt 0) { $reasons.Add('slow-launch-threshold') }
    if ((Get-IntProperty -Object $SampleSummary -Name 'SlowRuntimeCount') -gt 0) { $reasons.Add('slow-runtime-threshold') }

    if ((Get-IntProperty -Object $StartupAnalysis -Name 'DtmapiStartupSlowCount') -gt 0) { $reasons.Add('dtmapi-runtime-slow-classification') }
    if ((Get-IntProperty -Object $StartupAnalysis -Name 'LaunchDelayBeforeDtmapiRuntimeCount') -gt 0) { $reasons.Add('pre-runtime-launch-delay-classification') }
    if ((Get-IntProperty -Object $StartupAnalysis -Name 'SteamLaunchBlockedCount') -gt 0) { $reasons.Add('steam-launch-blocked-classification') }
    if ((Get-IntProperty -Object $StartupAnalysis -Name 'NeedsReviewCount') -gt 0) { $reasons.Add('needs-review-classification') }

    $sampleFailures = @($Samples | Where-Object { $_.ExitCode -ne 0 -or -not $_.EvidenceExists })
    if ($sampleFailures.Count -gt 0) { $reasons.Add("sample-failure-count-$($sampleFailures.Count)") }

    return @($reasons)
}

for ($batchIndex = 1; $batchIndex -le $MaxBatches; $batchIndex++) {
    if ($MaxMinutes -gt 0 -and ((Get-Date) - $startedAt).TotalMinutes -ge $MaxMinutes) {
        $stopReason = 'max-minutes-reached'
        break
    }

    $batchOutputPath = Join-Path $monitorRoot ('batch-{0:D2}-run-startup-samples.txt' -f $batchIndex)
    $batchStartedAt = Get-Date
    Write-Host "Startup monitor batch $batchIndex/$MaxBatches started."

    $sampleArgs = @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        (Join-Path $PSScriptRoot 'run-startup-samples.ps1'),
        '-Count',
        [string]$BatchSize,
        '-TimeoutSeconds',
        [string]$TimeoutSeconds,
        '-DelaySeconds',
        [string]$DelaySeconds,
        '-AutoExitAfterSeconds',
        [string]$AutoExitAfterSeconds,
        '-SlowLaunchThresholdMs',
        [string]$SlowLaunchThresholdMs,
        '-SlowRuntimeThresholdMs',
        [string]$SlowRuntimeThresholdMs,
        '-StopOnSlowSample'
    )
    if (-not $IncludeTitleSettingsMenu) { $sampleArgs += '-NoTitleSettingsMenu' }
    if ($SkipBuild -or $batchIndex -gt 1) { $sampleArgs += '-SkipBuild' }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & powershell @sampleArgs 2>&1
        $exitCode = if (Get-Variable LASTEXITCODE -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    $outputText = $output | Out-String
    $outputText | Set-Content -LiteralPath $batchOutputPath
    $batchFinishedAt = Get-Date

    $sampleRoot = Get-SampleEvidenceRootFromOutput -Text $outputText
    $sampleRootExists = $false
    if ($sampleRoot -and (Test-Path -LiteralPath $sampleRoot)) {
        $sampleRoot = (Resolve-Path -LiteralPath $sampleRoot).Path
        $sampleRootExists = $true
    }

    $sampleSummary = $null
    $startupAnalysis = $null
    $samplesJson = @()
    if ($sampleRootExists) {
        $sampleSummary = Read-JsonIfExists -Path (Join-Path $sampleRoot 'startup-sample-summary.json')
        $startupAnalysis = Read-JsonIfExists -Path (Join-Path $sampleRoot 'startup-analysis.json')
        $samplesJson = @(Read-JsonIfExists -Path (Join-Path $sampleRoot 'startup-samples.json'))
    }

    $reasons = @(Get-BatchTriggerReasons -ExitCode $exitCode -EvidenceExists $sampleRootExists -SampleSummary $sampleSummary -StartupAnalysis $startupAnalysis -Samples $samplesJson)
    $batch = [pscustomobject]@{
        Index = $batchIndex
        Started = $batchStartedAt.ToString('o')
        Finished = $batchFinishedAt.ToString('o')
        DurationSeconds = [int]($batchFinishedAt - $batchStartedAt).TotalSeconds
        ExitCode = $exitCode
        StartupSamplesEvidence = $sampleRoot
        EvidenceExists = $sampleRootExists
        NormalDtmapiStartupCount = Get-IntProperty -Object $startupAnalysis -Name 'NormalDtmapiStartupCount'
        DtmapiStartupSlowCount = Get-IntProperty -Object $startupAnalysis -Name 'DtmapiStartupSlowCount'
        LaunchDelayBeforeDtmapiRuntimeCount = Get-IntProperty -Object $startupAnalysis -Name 'LaunchDelayBeforeDtmapiRuntimeCount'
        SteamLaunchBlockedCount = Get-IntProperty -Object $startupAnalysis -Name 'SteamLaunchBlockedCount'
        NeedsReviewCount = Get-IntProperty -Object $startupAnalysis -Name 'NeedsReviewCount'
        SlowLaunchCount = Get-IntProperty -Object $sampleSummary -Name 'SlowLaunchCount'
        SlowRuntimeCount = Get-IntProperty -Object $sampleSummary -Name 'SlowRuntimeCount'
        Triggered = $reasons.Count -gt 0
        TriggerReasons = $reasons
        Output = $batchOutputPath
    }
    $batches.Add($batch)

    Write-Host "Startup monitor batch $batchIndex/$MaxBatches exitCode=$exitCode evidence=$sampleRoot triggers=$($reasons -join ',')"

    $leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if ($leftover) {
        Write-ProcessCheck -Path (Join-Path $monitorRoot ('batch-{0:D2}-leftover-process-check.txt' -f $batchIndex))
        Write-FatalWindowCheck -Path (Join-Path $monitorRoot ('batch-{0:D2}-fatal-window-check.txt' -f $batchIndex))
        $triggered = $true
        $stopReason = "leftover-doloc-process-after-batch-$batchIndex"
        break
    }

    if ($reasons.Count -gt 0) {
        $triggered = $true
        $stopReason = "triggered-batch-$batchIndex"
        break
    }

    if ($batchIndex -lt $MaxBatches -and $DelaySecondsBetweenBatches -gt 0) {
        Start-Sleep -Seconds $DelaySecondsBetweenBatches
    }
}

if (-not $stopReason) {
    $stopReason = 'max-batches-complete'
}

$finishedAt = Get-Date
$report = [pscustomobject]@{
    Generated = $finishedAt.ToString('o')
    Started = $startedAt.ToString('o')
    Finished = $finishedAt.ToString('o')
    DurationSeconds = [int]($finishedAt - $startedAt).TotalSeconds
    Triggered = $triggered
    StopReason = $stopReason
    MaxBatches = $MaxBatches
    BatchSize = $BatchSize
    SlowLaunchThresholdMs = $SlowLaunchThresholdMs
    SlowRuntimeThresholdMs = $SlowRuntimeThresholdMs
    Batches = $batches
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $monitorRoot 'startup-monitor.json')

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Startup Monitor')
$lines.Add('')
$lines.Add("- Generated: $($report.Generated)")
$lines.Add("- Triggered: $($report.Triggered)")
$lines.Add("- Stop reason: $($report.StopReason)")
$lines.Add("- Batches captured: $($batches.Count)")
$lines.Add("- Batch size: $BatchSize")
$lines.Add("- Slow launch threshold: $SlowLaunchThresholdMs ms")
$lines.Add("- Slow runtime threshold: $SlowRuntimeThresholdMs ms")
$lines.Add('')
$lines.Add('| Batch | DurationSeconds | Normal | SlowRuntime | LaunchDelay | Blocked | NeedsReview | SlowLaunch | SlowThresholdRuntime | Triggered | Reasons | Evidence |')
$lines.Add('| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- |')
foreach ($batch in $batches) {
    $evidenceText = if ($batch.StartupSamplesEvidence) { '`' + $batch.StartupSamplesEvidence + '`' } else { '' }
    $reasonText = ($batch.TriggerReasons -join ', ')
    $lines.Add("| $($batch.Index) | $($batch.DurationSeconds) | $($batch.NormalDtmapiStartupCount) | $($batch.DtmapiStartupSlowCount) | $($batch.LaunchDelayBeforeDtmapiRuntimeCount) | $($batch.SteamLaunchBlockedCount) | $($batch.NeedsReviewCount) | $($batch.SlowLaunchCount) | $($batch.SlowRuntimeCount) | $($batch.Triggered) | $reasonText | $evidenceText |")
}
$lines.Add('')
$lines.Add('Interpretation: triggered batches preserve the matching `STARTUP-SAMPLES` folder. `SlowRuntime` or `DtmapiStartupSlow` is DTMAPI runtime startup evidence; `SlowLaunch` or `LaunchDelay` with normal runtime timings is pre-runtime launch delay; `Blocked` means Steam did not produce a usable game process/runtime log.')
$lines | Set-Content -LiteralPath (Join-Path $monitorRoot 'startup-monitor.md')

"Finished=$($finishedAt.ToString('o'))`nTriggered=$triggered`nStopReason=$stopReason" |
    Add-Content -LiteralPath (Join-Path $monitorRoot 'summary.txt')

Write-Host $monitorRoot
if ($FailOnTrigger -and $triggered) {
    exit 1
}
