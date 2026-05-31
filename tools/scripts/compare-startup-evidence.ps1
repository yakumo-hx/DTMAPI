param(
    [Parameter(Mandatory = $true)]
    [string[]] $NormalEvidencePath,
    [string[]] $CandidateEvidencePath = @(),
    [string] $OutputDirectory
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

function Expand-EvidenceArgs {
    param(
        [string[]] $Values
    )

    $expanded = New-Object System.Collections.Generic.List[string]
    foreach ($value in @($Values)) {
        foreach ($part in ($value -split ',')) {
            $trimmed = $part.Trim()
            if ($trimmed) {
                $expanded.Add($trimmed)
            }
        }
    }

    return @($expanded)
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

function Select-Class {
    param(
        [object[]] $Items,
        [string] $Classification
    )

    return @($Items | Where-Object { $_.Classification -eq $Classification })
}

$normalInput = @(Expand-EvidenceArgs -Values $NormalEvidencePath)
$candidateInput = @(Expand-EvidenceArgs -Values $CandidateEvidencePath)
if ($normalInput.Count -eq 0) {
    throw 'At least one normal evidence path is required.'
}

if (-not $OutputDirectory) {
    $OutputDirectory = New-EvidenceDir -RepoRoot $repo -CaseId 'STARTUP-COMPARE'
}
elseif (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
}
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

$allEvidence = @($normalInput + $candidateInput)
& "$PSScriptRoot\analyze-startup-evidence.ps1" -EvidencePath $allEvidence -OutputDirectory $OutputDirectory -Quiet
$analysis = Get-Content -Raw -LiteralPath (Join-Path $OutputDirectory 'startup-analysis.json') | ConvertFrom-Json
$items = @($analysis.Items)

$normalItems = @(Select-Class -Items $items -Classification 'NormalDtmapiStartup')
$runtimeSlowItems = @(Select-Class -Items $items -Classification 'DtmapiStartupSlow')
$launchDelayItems = @(Select-Class -Items $items -Classification 'LaunchDelayBeforeDtmapiRuntime')
$blockedItems = @(Select-Class -Items $items -Classification 'SteamLaunchBlockedBeforeProcessOrNoFreshRuntimeLog')
$needsReviewItems = @(Select-Class -Items $items -Classification 'NeedsReview')

$status = 'PendingAbnormalRuntimeEvidence'
if ($normalItems.Count -eq 0) {
    $status = 'MissingNormalBaseline'
}
elseif ($runtimeSlowItems.Count -gt 0) {
    $status = 'RuntimeSlowComparisonReady'
}
elseif ($launchDelayItems.Count -gt 0 -or $blockedItems.Count -gt 0) {
    $status = 'OnlyPreRuntimeOrBlockedAbnormalEvidence'
}
elseif ($needsReviewItems.Count -gt 0) {
    $status = 'NeedsReview'
}

$summary = [pscustomobject]@{
    Generated = Get-Date -Format o
    Status = $status
    EvidenceCount = $items.Count
    NormalCount = $normalItems.Count
    RuntimeSlowCount = $runtimeSlowItems.Count
    LaunchDelayCount = $launchDelayItems.Count
    BlockedCount = $blockedItems.Count
    NeedsReviewCount = $needsReviewItems.Count
    Normal = [pscustomobject]@{
        LaunchToStartupPatternMs = Get-NumberStats -Values @($normalItems | ForEach-Object { $_.LaunchToStartupPatternMs })
        BootstrapAwakeTotalMs = Get-NumberStats -Values @($normalItems | ForEach-Object { $_.BootstrapAwakeTotalMs })
        DiscoverModsTotalMs = Get-NumberStats -Values @($normalItems | ForEach-Object { $_.DiscoverModsTotalMs })
        ModLoadMs = Get-NumberStats -Values @($normalItems | ForEach-Object { $_.ModLoadMs })
        BootstrapHarmonyInitializeMs = Get-NumberStats -Values @($normalItems | ForEach-Object { $_.BootstrapHarmonyInitializeMs })
    }
    RuntimeSlow = [pscustomobject]@{
        LaunchToStartupPatternMs = Get-NumberStats -Values @($runtimeSlowItems | ForEach-Object { $_.LaunchToStartupPatternMs })
        BootstrapAwakeTotalMs = Get-NumberStats -Values @($runtimeSlowItems | ForEach-Object { $_.BootstrapAwakeTotalMs })
        DiscoverModsTotalMs = Get-NumberStats -Values @($runtimeSlowItems | ForEach-Object { $_.DiscoverModsTotalMs })
        ModLoadMs = Get-NumberStats -Values @($runtimeSlowItems | ForEach-Object { $_.ModLoadMs })
        BootstrapHarmonyInitializeMs = Get-NumberStats -Values @($runtimeSlowItems | ForEach-Object { $_.BootstrapHarmonyInitializeMs })
    }
    LaunchDelayEvidence = @($launchDelayItems | ForEach-Object { $_.Evidence })
    BlockedEvidence = @($blockedItems | ForEach-Object { $_.Evidence })
    RuntimeSlowEvidence = @($runtimeSlowItems | ForEach-Object { $_.Evidence })
    NeedsReviewEvidence = @($needsReviewItems | ForEach-Object { $_.Evidence })
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'startup-comparison.json')

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Startup Comparison')
$lines.Add('')
$lines.Add("- Generated: $($summary.Generated)")
$lines.Add("- Status: $($summary.Status)")
$lines.Add("- Evidence count: $($summary.EvidenceCount)")
$lines.Add("- Normal DTMAPI startup: $($summary.NormalCount)")
$lines.Add("- Runtime slow: $($summary.RuntimeSlowCount)")
$lines.Add("- Pre-runtime launch delay: $($summary.LaunchDelayCount)")
$lines.Add("- Blocked/no fresh runtime log: $($summary.BlockedCount)")
$lines.Add("- Needs review: $($summary.NeedsReviewCount)")
$lines.Add('')
$lines.Add('| Group | Metric | Count | Min | Max | Average |')
$lines.Add('| --- | --- | ---: | ---: | ---: | ---: |')
foreach ($group in @(
    @('Normal', $summary.Normal),
    @('RuntimeSlow', $summary.RuntimeSlow)
)) {
    $groupName = $group[0]
    $statsObject = $group[1]
    foreach ($metricName in @('LaunchToStartupPatternMs', 'BootstrapAwakeTotalMs', 'DiscoverModsTotalMs', 'ModLoadMs', 'BootstrapHarmonyInitializeMs')) {
        $stats = $statsObject.$metricName
        $lines.Add("| $groupName | $metricName | $($stats.Count) | $($stats.Min) | $($stats.Max) | $($stats.Average) |")
    }
}
$lines.Add('')
if ($status -eq 'RuntimeSlowComparisonReady') {
    $lines.Add('Interpretation: at least one true DTMAPI runtime slow sample is present. Compare the `RuntimeSlow` segment ranges against the normal baseline before changing runtime code.')
}
elseif ($status -eq 'OnlyPreRuntimeOrBlockedAbnormalEvidence') {
    $lines.Add('Interpretation: abnormal candidates are pre-runtime launch delay or blocked/no-fresh-log evidence. They do not prove DTMAPI runtime startup slowness.')
}
elseif ($status -eq 'MissingNormalBaseline') {
    $lines.Add('Interpretation: no normal DTMAPI startup baseline was found; collect normal evidence before comparing abnormal samples.')
}
else {
    $lines.Add('Interpretation: no true abnormal DTMAPI runtime startup sample is present yet. Keep collecting evidence instead of optimizing by guesswork.')
}
$lines.Add('')
$lines.Add('Details are available in `startup-analysis.md` and `startup-comparison.json`.')
$lines | Set-Content -LiteralPath (Join-Path $OutputDirectory 'startup-comparison.md')

Write-Host $OutputDirectory
