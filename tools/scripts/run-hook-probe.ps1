param(
    [int] $SaveSlot = 3,
    [int] $TimeoutSeconds = 220,
    [switch] $UseSteam,
    [switch] $DirectExe,
    [switch] $AutoSaveAfterLoad,
    [switch] $AutoReloadMods,
    [switch] $AutoOpenAnimalPanel,
    [switch] $SkipBuild
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

function Test-LogLine {
    param(
        [string] $LogPath,
        [string] $Pattern
    )

    if (-not (Test-Path $LogPath)) {
        return $false
    }

    return [bool](Select-String -LiteralPath $LogPath -Pattern $Pattern -SimpleMatch -Quiet)
}

& "$PSScriptRoot\run-game-smoke.ps1" -SaveSlot $SaveSlot -TimeoutSeconds $TimeoutSeconds -IncludeHookProbe -UseSteam:$UseSteam -DirectExe:$DirectExe -AutoSaveAfterLoad:$AutoSaveAfterLoad -AutoReloadMods:$AutoReloadMods -AutoExerciseExperimentalHooks -AutoOpenAnimalPanel:$AutoOpenAnimalPanel -SkipBuild:$SkipBuild
$smokeExit = if (Get-Variable LASTEXITCODE -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
if ($smokeExit -ne 0) {
    exit $smokeExit
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
$logPath = Join-Path $dtmapiDir 'logs\latest.log'
$evidence = New-EvidenceDir -RepoRoot $repo -CaseId 'HOOK-PROBE'
$checks = [ordered]@{
    Entry = Test-LogLine -LogPath $logPath -Pattern 'HookProbe Entry OK'
    GameLaunched = Test-LogLine -LogPath $logPath -Pattern 'HookProbe GameLaunched OK'
    InputRegistrations = Test-LogLine -LogPath $logPath -Pattern 'HookProbe InputRegistrations OK keys=F6,F9,F10,F11'
    UpdateTicked = Test-LogLine -LogPath $logPath -Pattern 'HookProbe UpdateTicked OK'
    OneSecond = Test-LogLine -LogPath $logPath -Pattern 'HookProbe OneSecondUpdateTicked OK'
    SaveLoaded = Test-LogLine -LogPath $logPath -Pattern 'HookProbe SaveLoaded OK'
    ActionSpeedEntry = Test-LogLine -LogPath $logPath -Pattern 'ActionSpeed migrated to DTMAPI shell'
    OneActionEntry = Test-LogLine -LogPath $logPath -Pattern 'OneActionComplete migrated to DTMAPI shell'
    OneActionHookStatus = Test-LogLine -LogPath $logPath -Pattern 'HookProbe HookStatusChanged OK Actions.OneActionComplete=experimental'
    ActionSpeedConfigVisible = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage Visible OK Yuuka.DTMAPI.ActionSpeed'
    ActionSpeedConfigCancel = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage CancelNoWrite OK Yuuka.DTMAPI.ActionSpeed'
    ActionSpeedConfigSave = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage SaveWrite OK Yuuka.DTMAPI.ActionSpeed'
    ActionSpeedConfigReset = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage ResetDefault OK Yuuka.DTMAPI.ActionSpeed'
    AutoFishingConfigVisible = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage Visible OK Yuuka.DTMAPI.AutoFishing'
    AutoFishingConfigCancel = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage CancelNoWrite OK Yuuka.DTMAPI.AutoFishing'
    AutoFishingConfigSave = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage SaveWrite OK Yuuka.DTMAPI.AutoFishing'
    AutoFishingConfigReset = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage ResetDefault OK Yuuka.DTMAPI.AutoFishing'
    OneActionConfigVisible = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage Visible OK Yuuka.DTMAPI.OneActionComplete'
    OneActionConfigCancel = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage CancelNoWrite OK Yuuka.DTMAPI.OneActionComplete'
    OneActionConfigSave = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage SaveWrite OK Yuuka.DTMAPI.OneActionComplete'
    OneActionConfigReset = Test-LogLine -LogPath $logPath -Pattern 'HookProbe ConfigPage ResetDefault OK Yuuka.DTMAPI.OneActionComplete'
    AutoFishingEntry = Test-LogLine -LogPath $logPath -Pattern 'AutoFishing migrated to DTMAPI shell'
    FishingHookStatus = Test-LogLine -LogPath $logPath -Pattern 'HookProbe HookStatusChanged OK Fishing.Automation=experimental'
    FishBreedingEntry = Test-LogLine -LogPath $logPath -Pattern 'FishBreedingAssistant migrated to DTMAPI shell'
    FishRoeHookStatus = Test-LogLine -LogPath $logPath -Pattern 'HookProbe HookStatusChanged OK Items.FishRoeTooltip=experimental'
    FishRoeTooltipExercise = Test-LogLine -LogPath $logPath -Pattern 'Smoke exercise FishRoeTooltip OK'
    AnimalProgressEntry = Test-LogLine -LogPath $logPath -Pattern 'AnimalHusbandryProgress migrated to DTMAPI shell'
    AnimalViewerHookStatus = Test-LogLine -LogPath $logPath -Pattern 'HookProbe HookStatusChanged OK Animals.ViewerRendering=experimental'
    AnimalViewerExercise = Test-LogLine -LogPath $logPath -Pattern 'Smoke exercise AnimalViewerRendering OK'
    AnimalViewerUiEvidence = (-not [bool]$AutoOpenAnimalPanel) -or (Test-LogLine -LogPath $logPath -Pattern 'Animal viewer UI evidence OK')
}
if ($checks['SaveLoaded']) {
    $checks['UiStatus'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe UI Status OK'
    $checks['UiMods'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe UI Mods OK'
    $checks['UiConfig'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe UI Config OK'
    $checks['UiErrors'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe UI Errors OK'
    $checks['UiHooks'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe UI Hooks OK'
    $checks['UiExportLogs'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe UI ExportLogs OK'
}
else {
    $checks['UiStatus'] = $false
    $checks['UiMods'] = $false
    $checks['UiConfig'] = $false
    $checks['UiErrors'] = $false
    $checks['UiHooks'] = $false
    $checks['UiExportLogs'] = $false
}
if ($AutoSaveAfterLoad) {
    $checks['SaveSaving'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe SaveSaving OK'
    $checks['SaveSaved'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe SaveSaved OK'
}
if ($AutoReloadMods) {
    $checks['WorkshopModListChanged'] = Test-LogLine -LogPath $logPath -Pattern 'HookProbe WorkshopModListChanged OK'
}

$exitDeadline = (Get-Date).AddSeconds(60)
while ((Get-Date) -lt $exitDeadline) {
    $proc = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if (-not $proc) {
        break
    }
    Start-Sleep -Seconds 2
}

$forcedClose = $false
$leftover = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
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
$checks['ProcessExited'] = -not [bool]$leftover
$checks['ForcedClose'] = -not $forcedClose
$checks['NoFatalInstanceWindow'] = -not (Test-FatalInstanceWindow)
$checks | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'hook-checks.json')
if (Test-Path $logPath) {
    Copy-Item -Force -LiteralPath $logPath -Destination (Join-Path $evidence 'DTMAPI-latest.log')
}
$latestReport = Join-Path $dtmapiDir 'reports\latest-report.txt'
if (Test-Path $latestReport) {
    Copy-Item -Force -LiteralPath $latestReport -Destination (Join-Path $evidence 'latest-report.txt')
}
Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')

$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
if ($failed.Count -gt 0) {
    Write-Error "Hook probe missing evidence: $($failed.Name -join ', '). Evidence: $evidence"
    exit 1
}

Write-Host "Hook probe passed. Evidence: $evidence"
