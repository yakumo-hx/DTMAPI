param(
    [Parameter(Mandatory = $true)]
    [string[]] $EvidencePath,
    [string] $OutputDirectory,
    [switch] $Quiet
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

function Resolve-EvidencePath {
    param(
        [string] $Path
    )

    if (Test-Path -LiteralPath $Path) {
        return (Resolve-Path -LiteralPath $Path).Path
    }

    $candidate = Join-Path $repo $Path
    if (Test-Path -LiteralPath $candidate) {
        return (Resolve-Path -LiteralPath $candidate).Path
    }

    throw "Evidence path not found: $Path"
}

function Get-RelativePathForReport {
    param(
        [string] $Path
    )

    $full = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetFullPath($repo).TrimEnd('\') + '\'
    if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($root.Length).Replace('\', '/')
    }
    return $full
}

function Read-OptionalText {
    param(
        [string] $Path
    )

    if (Test-Path -LiteralPath $Path) {
        return Get-Content -Raw -LiteralPath $Path -ErrorAction SilentlyContinue
    }

    return ''
}

function Read-OptionalJson {
    param(
        [string] $Path
    )

    if (Test-Path -LiteralPath $Path) {
        try {
            return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
        }
        catch {
            return $null
        }
    }

    return $null
}

function Get-FirstNumber {
    param(
        [string] $Text,
        [string] $Pattern
    )

    $match = [regex]::Match($Text, $Pattern)
    if ($match.Success) {
        return [int]$match.Groups[1].Value
    }

    return $null
}

function Get-StartupLineCount {
    param(
        [string] $Text
    )

    if (-not $Text) {
        return 0
    }

    return ([regex]::Matches($Text, 'Startup segment|DTMAPI startup segment')).Count
}

function Analyze-EvidenceDirectory {
    param(
        [string] $Directory
    )

    $result = Read-OptionalJson -Path (Join-Path $Directory 'result.json')
    $summary = Read-OptionalText -Path (Join-Path $Directory 'summary.txt')
    $processCheck = Read-OptionalText -Path (Join-Path $Directory 'process-check.txt')
    $fatalCheck = Read-OptionalText -Path (Join-Path $Directory 'fatal-window-check.txt')
    $timeline = Read-OptionalJson -Path (Join-Path $Directory 'startup-timeline.json')
    $dtmapiLog = Read-OptionalText -Path (Join-Path $Directory 'DTMAPI-latest.log')
    $bepInExLog = Read-OptionalText -Path (Join-Path $Directory 'BepInEx-LogOutput.log')

    $runtimeText = ''
    $runtimeLogSource = 'none'
    if ($dtmapiLog) {
        $runtimeText = $dtmapiLog
        $runtimeLogSource = 'DTMAPI-latest.log'
    }
    elseif ($bepInExLog) {
        $runtimeText = $bepInExLog
        $runtimeLogSource = 'BepInEx-LogOutput.log'
    }

    $startupLog = $null
    $gameLaunched = $null
    $saveLoaded = $null
    if ($result) {
        if ($result.PSObject.Properties.Name -contains 'StartupLog') { $startupLog = [bool]$result.StartupLog }
        if ($result.PSObject.Properties.Name -contains 'GameLaunched') { $gameLaunched = [bool]$result.GameLaunched }
        if ($result.PSObject.Properties.Name -contains 'SaveLoaded') { $saveLoaded = [bool]$result.SaveLoaded }
    }

    $startupLineCount = Get-StartupLineCount -Text $runtimeText
    $bootstrapAwakeMs = Get-FirstNumber -Text $runtimeText -Pattern 'Bootstrap\.StartRuntime totalMs=(\d+)'
    if ($null -eq $bootstrapAwakeMs) {
        $bootstrapAwakeMs = Get-FirstNumber -Text $runtimeText -Pattern 'Bootstrap\.Awake totalMs=(\d+)'
    }
    $discoverModsMs = Get-FirstNumber -Text $runtimeText -Pattern 'DiscoverMods totalMs=(\d+)'
    $manifestScanMs = Get-FirstNumber -Text $runtimeText -Pattern 'ManifestScan elapsedMs=(\d+)'
    $officialModsScanMs = Get-FirstNumber -Text $runtimeText -Pattern 'OfficialModsScan elapsedMs=(\d+)'
    $workshopScanMs = Get-FirstNumber -Text $runtimeText -Pattern 'WorkshopScan elapsedMs=(\d+)'
    $contentIndexMs = Get-FirstNumber -Text $runtimeText -Pattern 'ContentQueryIndex elapsedMs=(\d+)'
    $modLoadMs = Get-FirstNumber -Text $runtimeText -Pattern 'ModLoad elapsedMs=(\d+)'
    $coreStartMs = Get-FirstNumber -Text $runtimeText -Pattern 'Core\.Start totalMs=(\d+)'
    $runtimeStartMs = Get-FirstNumber -Text $runtimeText -Pattern 'Bootstrap\.RuntimeStart elapsedMs=(\d+)'
    $harmonyMs = Get-FirstNumber -Text $runtimeText -Pattern 'Bootstrap\.HarmonyInitialize elapsedMs=(\d+)'
    $iconMs = Get-FirstNumber -Text $runtimeText -Pattern 'IconLoad elapsedMs=(\d+)'

    $noDolocProcess = $processCheck -match 'No DolocTown\.exe process found'
    $fatalPopup = $fatalCheck -and ($fatalCheck -notmatch 'No fatal instance popup found')
    $runtimeStarting = $runtimeText -match 'DTMAPI runtime starting\.'
    $launchToProcessMs = $null
    $launchToDtmapiLogFileMs = $null
    $launchToStartupPatternMs = $null
    $startupWaitTimedOut = $null
    $timelineLaunchMode = $null
    if ($timeline) {
        if ($timeline.PSObject.Properties.Name -contains 'LaunchToProcessMs') { $launchToProcessMs = $timeline.LaunchToProcessMs }
        if ($timeline.PSObject.Properties.Name -contains 'LaunchToDtmapiLogFileMs') { $launchToDtmapiLogFileMs = $timeline.LaunchToDtmapiLogFileMs }
        if ($timeline.PSObject.Properties.Name -contains 'LaunchToStartupPatternMs') { $launchToStartupPatternMs = $timeline.LaunchToStartupPatternMs }
        if ($timeline.PSObject.Properties.Name -contains 'TimedOut') { $startupWaitTimedOut = [bool]$timeline.TimedOut }
        if ($timeline.PSObject.Properties.Name -contains 'LaunchMode') { $timelineLaunchMode = $timeline.LaunchMode }
    }

    $classification = 'NeedsReview'
    if ($bootstrapAwakeMs -ne $null -and $bootstrapAwakeMs -ge 30000) {
        $classification = 'DtmapiStartupSlow'
    }
    elseif ($launchToStartupPatternMs -ne $null -and $launchToStartupPatternMs -ge 30000 -and ($bootstrapAwakeMs -eq $null -or $bootstrapAwakeMs -lt 30000)) {
        $classification = 'LaunchDelayBeforeDtmapiRuntime'
    }
    elseif ($startupLineCount -gt 0 -and ($runtimeStarting -or $bootstrapAwakeMs -ne $null)) {
        $classification = 'NormalDtmapiStartup'
    }
    elseif ($startupLineCount -eq 0 -and $noDolocProcess -and -not $fatalPopup) {
        $classification = 'SteamLaunchBlockedBeforeProcessOrNoFreshRuntimeLog'
    }

    return [pscustomobject]@{
        Evidence = Get-RelativePathForReport -Path $Directory
        Classification = $classification
        RuntimeLogSource = $runtimeLogSource
        StartupLog = $startupLog
        GameLaunched = $gameLaunched
        SaveLoaded = $saveLoaded
        StartupSegmentLines = $startupLineCount
        BootstrapAwakeTotalMs = $bootstrapAwakeMs
        DiscoverModsTotalMs = $discoverModsMs
        ManifestScanMs = $manifestScanMs
        OfficialModsScanMs = $officialModsScanMs
        WorkshopScanMs = $workshopScanMs
        ContentQueryIndexMs = $contentIndexMs
        ModLoadMs = $modLoadMs
        CoreStartTotalMs = $coreStartMs
        BootstrapRuntimeStartMs = $runtimeStartMs
        BootstrapHarmonyInitializeMs = $harmonyMs
        IconLoadMs = $iconMs
        LaunchMode = $timelineLaunchMode
        LaunchToProcessMs = $launchToProcessMs
        LaunchToDtmapiLogFileMs = $launchToDtmapiLogFileMs
        LaunchToStartupPatternMs = $launchToStartupPatternMs
        StartupWaitTimedOut = $startupWaitTimedOut
        NoDolocTownProcess = $noDolocProcess
        FatalInstancePopup = [bool]$fatalPopup
        Summary = ($summary -replace '\r?\n', '; ')
    }
}

$inputEvidence = @()
foreach ($path in $EvidencePath) {
    foreach ($part in ($path -split ',')) {
        $trimmed = $part.Trim()
        if ($trimmed) {
            $inputEvidence += $trimmed
        }
    }
}
$resolvedEvidence = @($inputEvidence | ForEach-Object { Resolve-EvidencePath -Path $_ })
if (-not $OutputDirectory) {
    $OutputDirectory = New-EvidenceDir -RepoRoot $repo -CaseId 'STARTUP-COMPARE'
}
elseif (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
}
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

$items = @($resolvedEvidence | ForEach-Object { Analyze-EvidenceDirectory -Directory $_ })
$normalCount = @($items | Where-Object { $_.Classification -eq 'NormalDtmapiStartup' }).Count
$slowCount = @($items | Where-Object { $_.Classification -eq 'DtmapiStartupSlow' }).Count
$launchDelayCount = @($items | Where-Object { $_.Classification -eq 'LaunchDelayBeforeDtmapiRuntime' }).Count
$blockedCount = @($items | Where-Object { $_.Classification -eq 'SteamLaunchBlockedBeforeProcessOrNoFreshRuntimeLog' }).Count
$needsReviewCount = @($items | Where-Object { $_.Classification -eq 'NeedsReview' }).Count

$report = [pscustomobject]@{
    Generated = Get-Date -Format o
    EvidenceCount = $items.Count
    NormalDtmapiStartupCount = $normalCount
    DtmapiStartupSlowCount = $slowCount
    LaunchDelayBeforeDtmapiRuntimeCount = $launchDelayCount
    SteamLaunchBlockedCount = $blockedCount
    NeedsReviewCount = $needsReviewCount
    Items = $items
}

$jsonPath = Join-Path $OutputDirectory 'startup-analysis.json'
$mdPath = Join-Path $OutputDirectory 'startup-analysis.md'
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Startup Evidence Analysis')
$lines.Add('')
$lines.Add("- Generated: $($report.Generated)")
$lines.Add("- Evidence count: $($report.EvidenceCount)")
$lines.Add("- Normal DTMAPI startup: $normalCount")
$lines.Add("- DTMAPI startup slow: $slowCount")
$lines.Add("- Launch delay before DTMAPI runtime: $launchDelayCount")
$lines.Add("- Steam/pre-process blocked or no fresh runtime log: $blockedCount")
$lines.Add("- Needs review: $needsReviewCount")
$lines.Add('')
$lines.Add('| Evidence | Classification | Log | StartupLog | GameLaunched | SaveLoaded | LaunchToProcessMs | LaunchToStartupMs | BootstrapAwakeMs | DiscoverModsMs | ModLoadMs | HarmonyMs | IconMs | Process | Fatal |')
$lines.Add('| --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |')
foreach ($item in $items) {
    $processText = if ($item.NoDolocTownProcess) { 'no DolocTown.exe' } else { 'process unknown/present' }
    $fatalText = if ($item.FatalInstancePopup) { 'fatal popup' } else { 'none/unknown' }
    $evidenceCell = '`' + $item.Evidence + '`'
    $lines.Add("| $evidenceCell | $($item.Classification) | $($item.RuntimeLogSource) | $($item.StartupLog) | $($item.GameLaunched) | $($item.SaveLoaded) | $($item.LaunchToProcessMs) | $($item.LaunchToStartupPatternMs) | $($item.BootstrapAwakeTotalMs) | $($item.DiscoverModsTotalMs) | $($item.ModLoadMs) | $($item.BootstrapHarmonyInitializeMs) | $($item.IconLoadMs) | $processText | $fatalText |")
}
$lines.Add('')
$lines.Add('Interpretation rule: only evidence with fresh DTMAPI startup segment lines near 30000ms can prove a DTMAPI runtime startup slowdown. Current runs use `Bootstrap.StartRuntime totalMs`; historical runs fall back to `Bootstrap.Awake totalMs`. If launch-to-startup is slow but that runtime segment is normal, classify it as a pre-runtime launch delay. A run with no `DolocTown.exe`, no fatal popup, and no startup segments is launch-blocking evidence, not a DTMAPI runtime timing sample.')
$lines | Set-Content -LiteralPath $mdPath

if (-not $Quiet) {
    Write-Host $OutputDirectory
}
