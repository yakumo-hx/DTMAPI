[CmdletBinding()]
param(
    [string] $PackageSource = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-DtmCollectorTest {
    param(
        [bool] $Condition,
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-DtmCollectorTestSha256 {
    param([string] $Path)

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Write-DtmCollectorTestText {
    param(
        [string] $Path,
        [string] $Text
    )

    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

function Invoke-DtmCollectorTestBat {
    param(
        [string] $BatPath,
        [hashtable] $Environment,
        [string] $OutputBase
    )

    $oldValues = @{}
    foreach ($key in $Environment.Keys) {
        $oldValues[$key] = [Environment]::GetEnvironmentVariable([string]$key, 'Process')
        [Environment]::SetEnvironmentVariable([string]$key, [string]$Environment[$key], 'Process')
    }
    try {
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $env:ComSpec
        $startInfo.Arguments = '/d /e:off /v:off /c call "' + $BatPath + '"'
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw 'Could not start collector BAT.'
        }
        if (-not $process.WaitForExit(120000)) {
            try { $process.Kill() } catch { }
            throw 'Collector BAT timed out.'
        }
        $stdout = $process.StandardOutput.ReadToEnd()
        $stderr = $process.StandardError.ReadToEnd()
        Write-DtmCollectorTestText -Path ($OutputBase + '.stdout.txt') -Text $stdout
        Write-DtmCollectorTestText -Path ($OutputBase + '.stderr.txt') -Text $stderr
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            StdOut = $stdout
            StdErr = $stderr
        }
    }
    finally {
        foreach ($key in $Environment.Keys) {
            [Environment]::SetEnvironmentVariable([string]$key, $oldValues[$key], 'Process')
        }
    }
}

function Expand-DtmCollectorTestZip {
    param(
        [string] $ZipPath,
        [string] $Destination
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Directory]::CreateDirectory($Destination) | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $Destination)
}

$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($PackageSource)) {
    $PackageSource = Join-Path $repo 'tools\release\player-save-crash-collector'
}
$packageSource = [System.IO.Path]::GetFullPath($PackageSource)
$collectorSource = Join-Path $packageSource 'collect-save-and-crash-logs.ps1'
$winPs = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists($collectorSource)) -Message 'Collector source is missing.'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists($winPs)) -Message 'Windows PowerShell 5.1 was not found.'

$sessionRoot = Join-Path $repo ('temp\player-save-crash-collector-test-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$sessionFull = [System.IO.Path]::GetFullPath($sessionRoot)
$allowedPrefix = ([System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))).TrimEnd('\') + '\'
Assert-DtmCollectorTest -Condition ($sessionFull.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) -Message 'Test session escaped repository temp.'
[System.IO.Directory]::CreateDirectory($sessionFull) | Out-Null

$nonAsciiText = ([char]0x4E2D).ToString() + ([char]0x6587).ToString()
$packageCopy = Join-Path $sessionFull ('Program Files (x86);' + $nonAsciiText + ' & logs\collector package')
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($packageCopy)) | Out-Null
Copy-Item -LiteralPath $packageSource -Destination $packageCopy -Recurse -Force
$bat = Join-Path $packageCopy '1_collect_save_and_crash_logs.bat'
$collectorCopy = Join-Path $packageCopy 'collect-save-and-crash-logs.ps1'

$env:DTMAPI_PARSE_TARGET = $collectorCopy
$parseOutput = & $winPs -NoLogo -NoProfile -Command '$tokens=$null;$errors=$null;[System.Management.Automation.Language.Parser]::ParseFile($env:DTMAPI_PARSE_TARGET,[ref]$tokens,[ref]$errors)|Out-Null;$errors|ForEach-Object{$_.ToString()};Write-Output ("ParserErrors="+$errors.Count);if($errors.Count){exit 1}' 2>&1
$parseExit = $LASTEXITCODE
Write-DtmCollectorTestText -Path (Join-Path $sessionFull 'winps-parser.txt') -Text ($parseOutput -join [Environment]::NewLine)
Assert-DtmCollectorTest -Condition ($parseExit -eq 0) -Message ('Windows PowerShell parser failed: ' + ($parseOutput -join ' | '))

$userRoot = Join-Path $sessionFull ('player profile ' + $nonAsciiText)
$persistentRoot = Join-Path $userRoot 'AppData\LocalLow\RedSawGames\DolocTown'
$saveRoot = Join-Path $persistentRoot 'SAVE'
$gameRoot = Join-Path $sessionFull ('Tencent Files\123456\FileRecv\steamapps\common\Doloc Town (test) & ' + $nonAsciiText)
$tempRoot = Join-Path $sessionFull ('process temp ' + $nonAsciiText + ' & data')
$outputRoot = Join-Path $sessionFull ('Desktop output (x86);' + $nonAsciiText + ' & data')
$workRoot = Join-Path $sessionFull 'collector work'

$saveFiles = @{
    'doloc-archive-0.data' = '{"baseData":{"archiveIndex":0,"currentScene":null},"payload":"current"}'
    'doloc-archive-0.data.prev0' = '{"baseData":{"archiveIndex":0,"currentScene":"MainFarm"},"payload":"previous"}'
    'doloc-archive-0.data.prev1' = 'older-previous'
    'doloc-archive-0.data.bak' = 'backup'
    'mod_infos.json' = '[{"id":"test"}]'
    'nested\player-note.json' = '{"note":"unicode path fixture"}'
}
foreach ($relative in $saveFiles.Keys) {
    Write-DtmCollectorTestText -Path (Join-Path $saveRoot $relative) -Text $saveFiles[$relative]
}
Write-DtmCollectorTestText -Path (Join-Path $persistentRoot 'Player.log') -Text 'unity current crash stack'
Write-DtmCollectorTestText -Path (Join-Path $persistentRoot 'Player-prev.log') -Text 'unity previous crash stack'

Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'DolocTown.exe') -Text 'fake game marker'
Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'DTMAPI\logs\latest.log') -Text ('latest-log-' + ('x' * 1048576))
Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'DTMAPI\logs\latest-20260808-000001.log') -Text 'history-log'
Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'DTMAPI\reports\old-report.txt') -Text 'report'
Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'DTMAPI\install-state.json') -Text '{"status":"installed"}'
Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'DTMAPI\release-manifest.json') -Text '{"version":"0.6.1"}'
Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'DTMAPI\debug-console-last-give.txt') -Text 'debug console last action'
Write-DtmCollectorTestText -Path (Join-Path $gameRoot 'BepInEx\LogOutput.log') -Text 'bepinex-log'

$crashRoot = Join-Path $tempRoot 'RedSawGames\DolocTown\Crashes\Crash_2026-08-08_150936'
Write-DtmCollectorTestText -Path (Join-Path $crashRoot 'error.log') -Text 'native crash error'
Write-DtmCollectorTestText -Path (Join-Path $crashRoot 'Player.log') -Text 'crash-local player log'
$crashDump = Join-Path $crashRoot 'crash.dmp'
[System.IO.File]::WriteAllBytes($crashDump, (New-Object byte[] 8192))
$localDump = Join-Path $userRoot 'AppData\Local\CrashDumps\DolocTown.exe.1234.dmp'
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($localDump)) | Out-Null
[System.IO.File]::WriteAllBytes($localDump, (New-Object byte[] 4096))

$saveBefore = @{}
foreach ($file in @(Get-ChildItem -LiteralPath $saveRoot -Recurse -File -Force)) {
    $saveBefore[$file.FullName] = Get-DtmCollectorTestSha256 -Path $file.FullName
}

$successEnvironment = @{
    DTMAPI_SUPPORT_USERPROFILE = $userRoot
    DTMAPI_SUPPORT_PERSISTENT_ROOT = $persistentRoot
    DTMAPI_SUPPORT_GAME_DIR = $gameRoot
    DTMAPI_SUPPORT_TEMP_ROOT = $tempRoot
    DTMAPI_SUPPORT_OUTPUT_DIR = $outputRoot
    DTMAPI_SUPPORT_WORK_ROOT = $workRoot
    DTMAPI_SUPPORT_SKIP_WINDOWS_EVENTS = '1'
    DTMAPI_SUPPORT_SKIP_PROCESS_CHECK = '1'
    DTMAPI_SUPPORT_NO_GAME_DISCOVERY = '1'
    DTMAPI_SUPPORT_NO_PAUSE = '1'
}
$success = Invoke-DtmCollectorTestBat -BatPath $bat -Environment $successEnvironment -OutputBase (Join-Path $sessionFull 'success')
Assert-DtmCollectorTest -Condition ($success.ExitCode -eq 0) -Message ('Successful BAT matrix exited ' + $success.ExitCode + ': ' + $success.StdErr)
$successZips = @(Get-ChildItem -LiteralPath $outputRoot -File -Filter 'DTMAPI-player-support-*.zip')
Assert-DtmCollectorTest -Condition ($successZips.Count -eq 1) -Message 'Successful matrix did not publish exactly one ZIP.'
$expanded = Join-Path $sessionFull 'success-expanded'
Expand-DtmCollectorTestZip -ZipPath $successZips[0].FullName -Destination $expanded

$summaryPath = Join-Path $expanded 'collection-summary.txt'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists($summaryPath)) -Message 'Successful ZIP lacks collection-summary.txt.'
$summaryText = [System.IO.File]::ReadAllText($summaryPath)
Assert-DtmCollectorTest -Condition ($summaryText -match 'CollectionStatus=Complete') -Message 'Successful ZIP was not marked Complete.'
Assert-DtmCollectorTest -Condition ($summaryText -match 'SaveRootsFound=1') -Message 'SAVE root count was not one.'
Assert-DtmCollectorTest -Condition ($summaryText -match 'GameRootsFound=1') -Message 'Game root count was not one.'
Assert-DtmCollectorTest -Condition ($summaryText -match 'UnityCrashRootsFound=1') -Message 'Crash root count was not one.'

$collectedSaveRoot = Join-Path $expanded 'PlayerData\Persistent-01-DolocTown\SAVE'
foreach ($file in @(Get-ChildItem -LiteralPath $saveRoot -Recurse -File -Force)) {
    $relative = $file.FullName.Substring($saveRoot.Length).TrimStart([char[]]@('\', '/'))
    $copied = Join-Path $collectedSaveRoot $relative
    Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists($copied)) -Message ('SAVE file was not packaged: ' + $relative)
    Assert-DtmCollectorTest -Condition ((Get-DtmCollectorTestSha256 -Path $copied) -eq $saveBefore[$file.FullName]) -Message ('Packaged SAVE hash mismatch: ' + $relative)
    Assert-DtmCollectorTest -Condition ((Get-DtmCollectorTestSha256 -Path $file.FullName) -eq $saveBefore[$file.FullName]) -Message ('Source SAVE changed: ' + $relative)
}
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists((Join-Path $expanded 'Game-01\DTMAPI\logs\latest.log'))) -Message 'DTMAPI latest.log was not packaged.'
Assert-DtmCollectorTest -Condition ((Get-DtmCollectorTestSha256 -Path (Join-Path $expanded 'Game-01\DTMAPI\logs\latest.log')) -eq (Get-DtmCollectorTestSha256 -Path (Join-Path $gameRoot 'DTMAPI\logs\latest.log'))) -Message 'Large DTMAPI log hash mismatch.'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists((Join-Path $expanded 'Game-01\BepInEx\LogOutput.log'))) -Message 'BepInEx log was not packaged.'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists((Join-Path $expanded 'Game-01\DTMAPI\state\debug-console-last-give.txt'))) -Message 'DebugConsole last-action state was not packaged.'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists((Join-Path $expanded 'Unity-Crashes\Root-01\Crash_2026-08-08_150936\crash.dmp'))) -Message 'Unity crash.dmp was not packaged.'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists((Join-Path $expanded 'Windows-CrashDumps\DolocTown.exe.1234.dmp'))) -Message 'Windows local crash dump was not packaged.'
Assert-DtmCollectorTest -Condition (@(Get-ChildItem -LiteralPath $workRoot -Directory -Force -ErrorAction SilentlyContinue).Count -eq 0) -Message 'Successful collector left staging behind.'

$discoveryOutput = Join-Path $sessionFull 'discovery desktop'
$discoveryWork = Join-Path $sessionFull 'discovery work'
Write-DtmCollectorTestText -Path (Join-Path $discoveryOutput 'DTMAPI-logs\previous\summary.txt') -Text ('GameDir=' + $gameRoot + [Environment]::NewLine)
$discoveryEnvironment = @{
    DTMAPI_SUPPORT_USERPROFILE = $userRoot
    DTMAPI_SUPPORT_PERSISTENT_ROOT = $persistentRoot
    DTMAPI_SUPPORT_TEMP_ROOT = $tempRoot
    DTMAPI_SUPPORT_OUTPUT_DIR = $discoveryOutput
    DTMAPI_SUPPORT_WORK_ROOT = $discoveryWork
    DTMAPI_SUPPORT_SKIP_WINDOWS_EVENTS = '1'
    DTMAPI_SUPPORT_SKIP_PROCESS_CHECK = '1'
    DTMAPI_SUPPORT_NO_PAUSE = '1'
    DTMAPI_SUPPORT_GAME_DIR = ''
    DTMAPI_SUPPORT_NO_GAME_DISCOVERY = '0'
}
$discovery = Invoke-DtmCollectorTestBat -BatPath $bat -Environment $discoveryEnvironment -OutputBase (Join-Path $sessionFull 'discovery')
Assert-DtmCollectorTest -Condition ($discovery.ExitCode -eq 0) -Message ('Prior-summary discovery matrix exited ' + $discovery.ExitCode + '.')
$discoveryZip = @(Get-ChildItem -LiteralPath $discoveryOutput -File -Filter 'DTMAPI-player-support-*.zip' | Select-Object -First 1)
Assert-DtmCollectorTest -Condition ($discoveryZip.Count -eq 1) -Message 'Discovery matrix did not publish one ZIP.'
$discoveryExpanded = Join-Path $sessionFull 'discovery-expanded'
Expand-DtmCollectorTestZip -ZipPath $discoveryZip[0].FullName -Destination $discoveryExpanded
$discoverySummary = [System.IO.File]::ReadAllText((Join-Path $discoveryExpanded 'collection-summary.txt'))
Assert-DtmCollectorTest -Condition ($discoverySummary -match 'GameRootsFound=[1-9][0-9]*') -Message 'Prior support summary did not discover any game directory.'
Assert-DtmCollectorTest -Condition ($discoverySummary -match [regex]::Escape($gameRoot)) -Message 'Prior support summary did not discover its exact game directory.'
Assert-DtmCollectorTest -Condition ([System.IO.File]::Exists((Join-Path $discoveryExpanded 'Game-01\DTMAPI\logs\latest.log'))) -Message 'Prior-summary game was not the first packaged game root.'

$missingUser = Join-Path $sessionFull 'missing player'
$missingOutput = Join-Path $sessionFull 'missing output'
$missingWork = Join-Path $sessionFull 'missing work'
[System.IO.Directory]::CreateDirectory($missingUser) | Out-Null
$missingEnvironment = @{
    DTMAPI_SUPPORT_USERPROFILE = $missingUser
    DTMAPI_SUPPORT_OUTPUT_DIR = $missingOutput
    DTMAPI_SUPPORT_WORK_ROOT = $missingWork
    DTMAPI_SUPPORT_SKIP_WINDOWS_EVENTS = '1'
    DTMAPI_SUPPORT_SKIP_PROCESS_CHECK = '1'
    DTMAPI_SUPPORT_NO_GAME_DISCOVERY = '1'
    DTMAPI_SUPPORT_NO_PAUSE = '1'
    DTMAPI_SUPPORT_PERSISTENT_ROOT = ''
    DTMAPI_SUPPORT_GAME_DIR = ''
    DTMAPI_SUPPORT_TEMP_ROOT = $tempRoot
}
$missing = Invoke-DtmCollectorTestBat -BatPath $bat -Environment $missingEnvironment -OutputBase (Join-Path $sessionFull 'missing')
Assert-DtmCollectorTest -Condition ($missing.ExitCode -eq 2) -Message ('Missing-SAVE matrix should exit 2, got ' + $missing.ExitCode + '.')
$missingZip = @(Get-ChildItem -LiteralPath $missingOutput -File -Filter 'DTMAPI-player-support-*.zip')
Assert-DtmCollectorTest -Condition ($missingZip.Count -eq 1) -Message 'Missing-SAVE matrix did not publish its diagnostic ZIP.'
$missingExpanded = Join-Path $sessionFull 'missing-expanded'
Expand-DtmCollectorTestZip -ZipPath $missingZip[0].FullName -Destination $missingExpanded
$missingSummary = [System.IO.File]::ReadAllText((Join-Path $missingExpanded 'collection-summary.txt'))
Assert-DtmCollectorTest -Condition ($missingSummary -match 'CollectionStatus=Incomplete') -Message 'Missing-SAVE summary was not Incomplete.'
Assert-DtmCollectorTest -Condition ($missingSummary -match 'No SAVE directory was found') -Message 'Missing-SAVE reason was not recorded.'

$result = [ordered]@{
    Status = 'Passed'
    WindowsPowerShellParser = 'Passed'
    BatSpecialPath = 'Passed'
    CompleteSaveFamily = 'Passed'
    SourceHashUnchanged = 'Passed'
    DtmapiAndBepInExLogs = 'Passed'
    UnityCrashDump = 'Passed'
    PreviousBundleGameDiscovery = 'Passed'
    MissingSaveDiagnostic = 'Passed'
    Evidence = $sessionFull
    SuccessZip = $successZips[0].FullName
}
$resultPath = Join-Path $sessionFull 'result.json'
[System.IO.File]::WriteAllText($resultPath, ($result | ConvertTo-Json -Depth 5), (New-Object System.Text.UTF8Encoding($false)))
Write-Host 'Player SAVE/crash collector tests: PASS' -ForegroundColor Green
Write-Host ('Evidence: ' + $sessionFull)
