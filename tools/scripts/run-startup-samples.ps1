param(
    [int] $Count = 3,
    [int] $TimeoutSeconds = 120,
    [int] $DelaySeconds = 10,
    [int] $AutoExitAfterSeconds = 25,
    [int] $SlowLaunchThresholdMs = 30000,
    [int] $SlowRuntimeThresholdMs = 30000,
    [switch] $NoTitleSettingsMenu,
    [switch] $StopOnSlowSample,
    [switch] $SkipBuild,
    [switch] $FailOnSampleFailure
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ($Count -lt 1) {
    throw 'Count must be at least 1.'
}

$sampleRoot = New-EvidenceDir -RepoRoot $repo -CaseId 'STARTUP-SAMPLES'
$samples = New-Object System.Collections.Generic.List[object]
$evidencePaths = New-Object System.Collections.Generic.List[string]

"Started=$(Get-Date -Format o)`nCount=$Count`nTimeoutSeconds=$TimeoutSeconds`nDelaySeconds=$DelaySeconds`nAutoExitAfterSeconds=$AutoExitAfterSeconds`nSlowLaunchThresholdMs=$SlowLaunchThresholdMs`nSlowRuntimeThresholdMs=$SlowRuntimeThresholdMs`nNoTitleSettingsMenu=$NoTitleSettingsMenu`nStopOnSlowSample=$StopOnSlowSample`nSkipBuild=$SkipBuild" |
    Set-Content -LiteralPath (Join-Path $sampleRoot 'summary.txt')

function Get-SmokeEvidencePathFromOutput {
    param(
        [string] $Text
    )

    $pathMatches = [regex]::Matches($Text, '([A-Za-z]:\\[^\r\n]+\\docs\\debug\\evidence\\GAME-SMOKE\\\d{8}-\d{6})')
    for ($matchIndex = $pathMatches.Count - 1; $matchIndex -ge 0; $matchIndex--) {
        $candidate = $pathMatches[$matchIndex].Groups[1].Value.Trim()
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function Get-NumberStats {
    param(
        [object[]] $Values
    )

    $numbers = @($Values | Where-Object { $null -ne $_ } | ForEach-Object { [double]$_ })
    if ($numbers.Count -eq 0) {
        return [pscustomobject]@{
            Count = 0
            Min = $null
            Max = $null
            Average = $null
        }
    }

    $measure = $numbers | Measure-Object -Minimum -Maximum -Average
    return [pscustomobject]@{
        Count = $numbers.Count
        Min = [int64]$measure.Minimum
        Max = [int64]$measure.Maximum
        Average = [math]::Round([double]$measure.Average, 2)
    }
}

function Get-SampleStartupItem {
    param(
        [string] $EvidencePath
    )

    if (-not $EvidencePath) {
        return $null
    }

    $analysisPath = Join-Path $EvidencePath 'startup-analysis.json'
    if (-not (Test-Path -LiteralPath $analysisPath)) {
        return $null
    }

    try {
        $analysis = Get-Content -Raw -LiteralPath $analysisPath | ConvertFrom-Json
        return @($analysis.Items) | Select-Object -First 1
    }
    catch {
        return $null
    }
}

for ($index = 1; $index -le $Count; $index++) {
    $sampleName = 'sample-{0:D2}' -f $index
    $outputPath = Join-Path $sampleRoot "$sampleName-run-game-smoke.txt"
    $startedAt = Get-Date
    Write-Host "Startup sample $index/$Count started."

    $smokeArgs = @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        (Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
        '-SaveSlot',
        '0',
        '-TimeoutSeconds',
        [string]$TimeoutSeconds,
        '-AutoExitAfterSecondsOverride',
        [string]$AutoExitAfterSeconds
    )
    if (-not $NoTitleSettingsMenu) {
        $smokeArgs += '-AutoOpenTitleSettingsMenu'
    }
    if ($SkipBuild -or $index -gt 1) {
        $smokeArgs += '-SkipBuild'
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & powershell @smokeArgs 2>&1
        $exitCode = if (Get-Variable LASTEXITCODE -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    $outputText = $output | Out-String
    $outputText | Set-Content -LiteralPath $outputPath
    $finishedAt = Get-Date
    $evidencePath = Get-SmokeEvidencePathFromOutput -Text $outputText
    $evidenceExists = $false
    if ($evidencePath -and (Test-Path -LiteralPath $evidencePath)) {
        $evidencePath = (Resolve-Path -LiteralPath $evidencePath).Path
        $evidencePaths.Add($evidencePath)
        $evidenceExists = $true
    }

    $startupItem = Get-SampleStartupItem -EvidencePath $evidencePath
    $classification = $null
    $launchToStartupPatternMs = $null
    $bootstrapAwakeTotalMs = $null
    if ($startupItem) {
        if ($startupItem.PSObject.Properties.Name -contains 'Classification') { $classification = $startupItem.Classification }
        if ($startupItem.PSObject.Properties.Name -contains 'LaunchToStartupPatternMs') { $launchToStartupPatternMs = $startupItem.LaunchToStartupPatternMs }
        if ($startupItem.PSObject.Properties.Name -contains 'BootstrapAwakeTotalMs') { $bootstrapAwakeTotalMs = $startupItem.BootstrapAwakeTotalMs }
    }
    $slowLaunch = $launchToStartupPatternMs -ne $null -and [int64]$launchToStartupPatternMs -ge $SlowLaunchThresholdMs
    $slowRuntime = $bootstrapAwakeTotalMs -ne $null -and [int64]$bootstrapAwakeTotalMs -ge $SlowRuntimeThresholdMs

    $sample = [pscustomobject]@{
        Index = $index
        Started = $startedAt.ToString('o')
        Finished = $finishedAt.ToString('o')
        DurationSeconds = [int]($finishedAt - $startedAt).TotalSeconds
        ExitCode = $exitCode
        Classification = $classification
        LaunchToStartupPatternMs = $launchToStartupPatternMs
        BootstrapAwakeTotalMs = $bootstrapAwakeTotalMs
        SlowLaunch = $slowLaunch
        SlowRuntime = $slowRuntime
        Evidence = $evidencePath
        EvidenceExists = $evidenceExists
        Output = $outputPath
    }
    $samples.Add($sample)

    Write-Host "Startup sample $index/$Count exitCode=$exitCode evidence=$evidencePath"

    $leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if ($leftover) {
        Write-ProcessCheck -Path (Join-Path $sampleRoot "$sampleName-leftover-process-check.txt")
        Write-FatalWindowCheck -Path (Join-Path $sampleRoot "$sampleName-fatal-window-check.txt")
        break
    }

    if ($StopOnSlowSample -and ($slowLaunch -or $slowRuntime)) {
        "StoppedOnSlowSample=$index`nStoppedOnSlowLaunch=$slowLaunch`nStoppedOnSlowRuntime=$slowRuntime`nStoppedOnEvidence=$evidencePath" |
            Add-Content -LiteralPath (Join-Path $sampleRoot 'summary.txt')
        Write-Host "Stopped after sample $index because slow startup threshold was reached."
        break
    }

    if ($index -lt $Count -and $DelaySeconds -gt 0) {
        Start-Sleep -Seconds $DelaySeconds
    }
}

$samples | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.json')

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Startup Samples')
$lines.Add('')
$lines.Add("- Generated: $(Get-Date -Format o)")
$lines.Add("- Count requested: $Count")
$lines.Add("- Count captured: $($samples.Count)")
$lines.Add('')
$lines.Add('| Sample | ExitCode | DurationSeconds | Classification | LaunchToStartupMs | BootstrapAwakeMs | SlowLaunch | SlowRuntime | Evidence |')
$lines.Add('| ---: | ---: | ---: | --- | ---: | ---: | --- | --- | --- |')
foreach ($sample in $samples) {
    $evidenceText = if ($sample.Evidence) { '`' + $sample.Evidence + '`' } else { '' }
    $lines.Add("| $($sample.Index) | $($sample.ExitCode) | $($sample.DurationSeconds) | $($sample.Classification) | $($sample.LaunchToStartupPatternMs) | $($sample.BootstrapAwakeTotalMs) | $($sample.SlowLaunch) | $($sample.SlowRuntime) | $evidenceText |")
}
$lines.Add('')
$lines.Add('Run `startup-analysis.md` in this folder for classification. A failed sample with evidence is still useful startup evidence; it may represent Steam launch blocking rather than DTMAPI runtime startup cost.')
$lines | Set-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md')

if ($evidencePaths.Count -gt 0) {
    & "$PSScriptRoot\analyze-startup-evidence.ps1" -EvidencePath $evidencePaths.ToArray() -OutputDirectory $sampleRoot -Quiet
}

$analysisPath = Join-Path $sampleRoot 'startup-analysis.json'
if (Test-Path -LiteralPath $analysisPath) {
    $analysis = Get-Content -Raw -LiteralPath $analysisPath | ConvertFrom-Json
    $items = @($analysis.Items)
    $slowLaunchItems = @($items | Where-Object { $_.LaunchToStartupPatternMs -ne $null -and [int64]$_.LaunchToStartupPatternMs -ge $SlowLaunchThresholdMs })
    $slowRuntimeItems = @($items | Where-Object { $_.BootstrapAwakeTotalMs -ne $null -and [int64]$_.BootstrapAwakeTotalMs -ge $SlowRuntimeThresholdMs })
    $summary = [pscustomobject]@{
        Generated = Get-Date -Format o
        SampleCount = $samples.Count
        EvidenceCount = $items.Count
        SlowLaunchThresholdMs = $SlowLaunchThresholdMs
        SlowRuntimeThresholdMs = $SlowRuntimeThresholdMs
        DurationSeconds = Get-NumberStats -Values @($samples | ForEach-Object { $_.DurationSeconds })
        LaunchToProcessMs = Get-NumberStats -Values @($items | ForEach-Object { $_.LaunchToProcessMs })
        LaunchToStartupPatternMs = Get-NumberStats -Values @($items | ForEach-Object { $_.LaunchToStartupPatternMs })
        BootstrapAwakeTotalMs = Get-NumberStats -Values @($items | ForEach-Object { $_.BootstrapAwakeTotalMs })
        DiscoverModsTotalMs = Get-NumberStats -Values @($items | ForEach-Object { $_.DiscoverModsTotalMs })
        ModLoadMs = Get-NumberStats -Values @($items | ForEach-Object { $_.ModLoadMs })
        BootstrapHarmonyInitializeMs = Get-NumberStats -Values @($items | ForEach-Object { $_.BootstrapHarmonyInitializeMs })
        SlowLaunchCount = $slowLaunchItems.Count
        SlowRuntimeCount = $slowRuntimeItems.Count
        SlowLaunchEvidence = @($slowLaunchItems | ForEach-Object { $_.Evidence })
        SlowRuntimeEvidence = @($slowRuntimeItems | ForEach-Object { $_.Evidence })
    }

    $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $sampleRoot 'startup-sample-summary.json')

    $summaryLines = New-Object System.Collections.Generic.List[string]
    $summaryLines.Add('# Startup Sample Summary')
    $summaryLines.Add('')
    $summaryLines.Add("- Generated: $($summary.Generated)")
    $summaryLines.Add("- Samples: $($summary.SampleCount)")
    $summaryLines.Add("- Slow launch threshold: $SlowLaunchThresholdMs ms")
    $summaryLines.Add("- Slow runtime threshold: $SlowRuntimeThresholdMs ms")
    $summaryLines.Add("- Slow launch samples: $($summary.SlowLaunchCount)")
    $summaryLines.Add("- Slow runtime samples: $($summary.SlowRuntimeCount)")
    $summaryLines.Add('')
    $summaryLines.Add('| Metric | Count | Min | Max | Average |')
    $summaryLines.Add('| --- | ---: | ---: | ---: | ---: |')
    foreach ($metric in @(
        @('DurationSeconds', $summary.DurationSeconds),
        @('LaunchToProcessMs', $summary.LaunchToProcessMs),
        @('LaunchToStartupPatternMs', $summary.LaunchToStartupPatternMs),
        @('BootstrapAwakeTotalMs', $summary.BootstrapAwakeTotalMs),
        @('DiscoverModsTotalMs', $summary.DiscoverModsTotalMs),
        @('ModLoadMs', $summary.ModLoadMs),
        @('BootstrapHarmonyInitializeMs', $summary.BootstrapHarmonyInitializeMs)
    )) {
        $name = $metric[0]
        $stats = $metric[1]
        $summaryLines.Add("| $name | $($stats.Count) | $($stats.Min) | $($stats.Max) | $($stats.Average) |")
    }
    $summaryLines.Add('')
    $summaryLines.Add('Interpretation: `SlowRuntimeCount > 0` is candidate DTMAPI runtime startup slowness. `SlowLaunchCount > 0` with `SlowRuntimeCount = 0` points to Steam/game pre-runtime launch delay.')
    $summaryLines | Set-Content -LiteralPath (Join-Path $sampleRoot 'startup-sample-summary.md')

    Add-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md') -Value ''
    Add-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md') -Value '## Summary'
    Add-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md') -Value ''
    Add-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md') -Value "- Slow launch samples: $($summary.SlowLaunchCount) / $($summary.EvidenceCount)"
    Add-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md') -Value "- Slow runtime samples: $($summary.SlowRuntimeCount) / $($summary.EvidenceCount)"
    Add-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md') -Value "- LaunchToStartupPatternMs range: $($summary.LaunchToStartupPatternMs.Min)-$($summary.LaunchToStartupPatternMs.Max)"
    Add-Content -LiteralPath (Join-Path $sampleRoot 'startup-samples.md') -Value "- Bootstrap.Awake totalMs range: $($summary.BootstrapAwakeTotalMs.Min)-$($summary.BootstrapAwakeTotalMs.Max)"
}

$sampleFailures = @($samples | Where-Object { $_.ExitCode -ne 0 -or -not $_.EvidenceExists })
if ($FailOnSampleFailure -and $sampleFailures.Count -gt 0) {
    Write-Error "One or more startup samples failed. Evidence: $sampleRoot"
    exit 1
}

Write-Host $sampleRoot
