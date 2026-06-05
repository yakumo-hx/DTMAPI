param(
    [int] $TimeoutSeconds = 120,
    [int] $PollSeconds = 1,
    [switch] $NoResetLogs,
    [int] $WaitForExitSeconds = 0,
    [switch] $FailOnTimeout
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
$evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'STARTUP-OBSERVE'
$logPath = Join-Path $dtmapiDir 'logs\latest.log'
$bepInExLogPath = Join-Path $gameDir 'BepInEx\LogOutput.log'
$startedAt = Get-Date
$existingProcess = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
$resetLogs = -not [bool]$NoResetLogs
$resetLogResult = 'not-requested'

function Format-DateOrNull {
    param([object] $Value)
    if ($null -eq $Value) { return $null }
    return ([datetime]$Value).ToString('o')
}

function Get-ElapsedMsOrNull {
    param(
        [object] $Start,
        [object] $End
    )

    if ($null -eq $Start -or $null -eq $End) {
        return $null
    }

    return [int64]([datetime]$End - [datetime]$Start).TotalMilliseconds
}

if ($resetLogs) {
    if ($existingProcess) {
        $resetLogResult = 'skipped-game-already-running'
    }
    else {
        foreach ($path in @($logPath, $bepInExLogPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -Force -LiteralPath $path
            }
        }
        $resetLogResult = 'removed-existing-logs'
    }
}

"Started=$($startedAt.ToString('o'))`nGameDir=$gameDir`nDtmApiStateDir=$dtmapiDir`nTimeoutSeconds=$TimeoutSeconds`nPollSeconds=$PollSeconds`nNoResetLogs=$NoResetLogs`nResetLogResult=$resetLogResult`nWaitForExitSeconds=$WaitForExitSeconds`nMode=ExternalObserved" |
    Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$waitStartedAt = Get-Date
$deadline = $waitStartedAt.AddSeconds($TimeoutSeconds)
$startupPattern = 'DTMAPI runtime starting.'
$firstProcessAt = $null
$firstProcessId = $null
$firstLogFileAt = $null
$firstStartupPatternAt = $null
$fatalWindowAt = $null

while ((Get-Date) -lt $deadline) {
    if ($null -eq $firstProcessAt) {
        $process = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($process) {
            $firstProcessAt = Get-Date
            $firstProcessId = $process.Id
        }
    }

    if (Test-Path -LiteralPath $logPath) {
        $logItem = Get-Item -LiteralPath $logPath
        if ($logItem.LastWriteTime -ge $startedAt.AddSeconds(-1)) {
            if ($null -eq $firstLogFileAt) {
                $firstLogFileAt = Get-Date
            }

            $text = Get-Content -Raw -LiteralPath $logPath -ErrorAction SilentlyContinue
            if ($text -match [regex]::Escape($startupPattern)) {
                $firstStartupPatternAt = Get-Date
                break
            }
        }
    }

    if (Test-FatalInstanceWindow) {
        $fatalWindowAt = Get-Date
        break
    }

    Start-Sleep -Seconds ([Math]::Max(1, $PollSeconds))
}

$waitFinishedAt = Get-Date
$startupFound = $null -ne $firstStartupPatternAt

if ($WaitForExitSeconds -gt 0) {
    $exitDeadline = (Get-Date).AddSeconds($WaitForExitSeconds)
    while ((Get-Date) -lt $exitDeadline) {
        if (-not (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
            break
        }

        Start-Sleep -Seconds 1
    }
}

$timeline = [ordered]@{
    LaunchMode = 'ExternalObserved'
    LaunchCommandStartedAt = Format-DateOrNull $startedAt
    LaunchCommandFinishedAt = $null
    ObserveStartedAt = Format-DateOrNull $startedAt
    WaitStartedAt = Format-DateOrNull $waitStartedAt
    WaitFinishedAt = Format-DateOrNull $waitFinishedAt
    WaitTimeoutSeconds = $TimeoutSeconds
    StartupPattern = $startupPattern
    StartupLogFound = $startupFound
    TimedOut = (-not $startupFound) -and ($null -eq $fatalWindowAt)
    FatalInstanceWindowAt = Format-DateOrNull $fatalWindowAt
    FirstDolocTownProcessAt = Format-DateOrNull $firstProcessAt
    FirstDolocTownProcessId = $firstProcessId
    LaunchToProcessMs = Get-ElapsedMsOrNull $startedAt $firstProcessAt
    FirstDtmapiLogFileAt = Format-DateOrNull $firstLogFileAt
    LaunchToDtmapiLogFileMs = Get-ElapsedMsOrNull $startedAt $firstLogFileAt
    FirstStartupPatternAt = Format-DateOrNull $firstStartupPatternAt
    LaunchToStartupPatternMs = Get-ElapsedMsOrNull $startedAt $firstStartupPatternAt
    WaitDurationMs = Get-ElapsedMsOrNull $waitStartedAt $waitFinishedAt
}
$timeline | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidence 'startup-timeline.json')

$result = [ordered]@{
    StartupFound = $startupFound
    TimedOut = (-not $startupFound) -and ($null -eq $fatalWindowAt)
    FatalInstanceWindow = $null -ne $fatalWindowAt
    ProcessObserved = $null -ne $firstProcessAt
    DtmapiLogObserved = $null -ne $firstLogFileAt
    Evidence = $evidence
    Completed = (Get-Date).ToString('o')
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidence 'observer-result.json')

& "$PSScriptRoot\collect-logs.ps1" -CaseId 'STARTUP-OBSERVE' -OutputDirectory $evidence | Tee-Object -FilePath (Join-Path $evidence 'collect-logs-output.txt')

Write-Host $evidence
if ($FailOnTimeout -and -not $startupFound) {
    Write-Error "Startup observer timed out before seeing DTMAPI runtime startup. Evidence: $evidence"
    exit 1
}
