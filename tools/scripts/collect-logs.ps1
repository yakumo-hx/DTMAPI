param(
    [string] $CaseId = 'MANUAL',
    [string] $OutputDirectory,
    [switch] $IncludeRuntimeEvidence,
    [datetime] $RuntimeEvidenceSinceUtc,
    [long] $RuntimeEvidenceMaxTotalBytes = 512MB,
    [long] $RuntimeEvidenceMaxFileBytes = 128MB,
    [int] $RuntimeEvidenceMaxFiles = 2000,
    [int] $RuntimeEvidenceMaxDirectories = 128,
    [ValidateRange(4096, 1073741824)] [long] $TextLogMaxBytes = 4MB,
    [ValidateRange(4096, 1073741824)] [long] $HistoryLogMaxBytes = 2MB,
    [ValidateRange(0, 100)] [int] $HistoryLogCount = 3,
    [switch] $IncludeCrashDumps,
    [switch] $DesktopTimestampOutput
)

. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
$script:DtmCollectWarnings = New-Object 'System.Collections.Generic.List[string]'
$script:DtmUnityCrashMaxDirectories = 6
$script:DtmUnityCrashMaxFilesPerDirectory = 80
$script:DtmUnityCrashMaxFilesTotal = 120
$script:DtmUnityCrashMaxFileBytes = 16MB
$script:DtmUnityCrashMaxDumpFileBytes = 384MB
$script:DtmUnityCrashMaxTotalBytes = 32MB
$script:DtmUnityCrashMaxTotalBytesWithDump = 512MB
$script:DtmUnityCrashPreferredFiles = @('crash.dmp', 'error.log', 'Player.log', 'Player-prev.log')
$runtimeEvidenceWindowRequested = $PSBoundParameters.ContainsKey('RuntimeEvidenceSinceUtc')
$playerDoctorExit = $null
$playerDoctorStatus = 'not-run'
$playerDoctorStdOutPath = ''
$playerDoctorStdErrPath = ''

if ($IncludeRuntimeEvidence -and $runtimeEvidenceWindowRequested) {
    throw 'Use only one of -IncludeRuntimeEvidence or -RuntimeEvidenceSinceUtc.'
}

function Add-DtmCollectWarning {
    param([Parameter(Mandatory = $true)] [string] $Message)

    $script:DtmCollectWarnings.Add($Message) | Out-Null
    Write-Warning $Message
}

function ConvertTo-DtmCollectProcessArgument {
    param([Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Value)

    if ($Value.IndexOf('"') -ge 0) {
        throw "Player Doctor argument contains an unsupported quote: $Value"
    }

    $trailingBackslashCount = 0
    for ($index = $Value.Length - 1; $index -ge 0 -and $Value[$index] -eq '\'; $index--) {
        $trailingBackslashCount++
    }
    $escapedValue = $Value
    if ($trailingBackslashCount -gt 0) {
        $escapedValue = $Value.Substring(0, $Value.Length - $trailingBackslashCount) +
            ((@('\') * ($trailingBackslashCount * 2)) -join '')
    }
    return '"' + $escapedValue + '"'
}

function Invoke-DtmCollectPlayerDoctor {
    param(
        [Parameter(Mandatory = $true)] [string] $Executable,
        [Parameter(Mandatory = $true)] [string] $GameRoot,
        [Parameter(Mandatory = $true)] [string] $OutputRoot,
        [int] $TimeoutMilliseconds = 30000
    )

    $jsonPath = Join-Path $OutputRoot 'player-doctor.json'
    $textPath = Join-Path $OutputRoot 'player-doctor.txt'
    $summaryPath = Join-Path $OutputRoot 'player-doctor-summary.txt'
    $stdoutPath = Join-Path $OutputRoot 'player-doctor-stdout.txt'
    $stderrPath = Join-Path $OutputRoot 'player-doctor-stderr.txt'
    $arguments = @(
        'inspect',
        '--game-root', $GameRoot,
        '--scan-context', 'installed-game',
        '--json-output', $jsonPath,
        '--text-output', $textPath,
        '--summary-output', $summaryPath,
        '--quiet'
    )
    $argumentLine = (@($arguments | ForEach-Object { ConvertTo-DtmCollectProcessArgument -Value ([string]$_) }) -join ' ')
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $Executable
    $startInfo.Arguments = $argumentLine
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $startInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    $process = $null
    $stdoutTask = $null
    $stderrTask = $null
    try {
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw 'Player Doctor process did not start.'
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $timedOut = -not $process.WaitForExit($TimeoutMilliseconds)
        $terminationError = ''
        if ($timedOut) {
            try {
                $process.Kill()
                if (-not $process.WaitForExit(5000)) {
                    $terminationError = 'The process did not report exit within five seconds after termination.'
                }
            }
            catch {
                $terminationError = $_.Exception.Message
            }
        }

        $captureError = ''
        try {
            $streamTasks = [System.Threading.Tasks.Task[]]@($stdoutTask, $stderrTask)
            if (-not [System.Threading.Tasks.Task]::WaitAll($streamTasks, 5000)) {
                $captureError = 'Standard-output capture did not finish within five seconds.'
            }
        }
        catch {
            $captureError = $_.Exception.Message
        }
        $stdout = if ($null -ne $stdoutTask -and $stdoutTask.Status -eq [System.Threading.Tasks.TaskStatus]::RanToCompletion) { [string]$stdoutTask.Result } else { '' }
        $stderr = if ($null -ne $stderrTask -and $stderrTask.Status -eq [System.Threading.Tasks.TaskStatus]::RanToCompletion) { [string]$stderrTask.Result } else { '' }
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($stdoutPath, $stdout, $utf8NoBom)
        [System.IO.File]::WriteAllText($stderrPath, $stderr, $utf8NoBom)

        $exitCode = if ($timedOut) { $null } else { $process.ExitCode }
        return [pscustomobject]@{
            Status = if ($timedOut) { 'timeout' } elseif ($exitCode -eq 0) { 'clean' } elseif ($exitCode -eq 2) { 'findings' } else { 'crashed' }
            ExitCode = $exitCode
            StdOutPath = $stdoutPath
            StdErrPath = $stderrPath
            TerminationError = $terminationError
            CaptureError = $captureError
        }
    }
    finally {
        if ($null -ne $process) {
            try {
                if (-not $process.HasExited) {
                    $process.Kill()
                    [void]$process.WaitForExit(5000)
                }
            }
            catch {
                # The caller records start/crash/timeout warnings. Cleanup remains
                # best-effort and must not suppress the rest of log collection.
            }
            $process.Dispose()
        }
    }
}

$gameDirResolveError = $null
try {
    $gameDir = Resolve-DolocTownGamePath -RepoRoot $repo -AllowMissing
}
catch {
    $gameDir = $null
    $gameDirResolveError = $_.Exception.Message
}

if ($gameDir) {
    $dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
}
elseif ($env:DTMAPI_RUNTIME_DIR) {
    try {
        $dtmapiDir = [System.IO.Path]::GetFullPath($env:DTMAPI_RUNTIME_DIR)
    }
    catch {
        Add-DtmCollectWarning -Message "DTMAPI_RUNTIME_DIR is not a valid path: $($_.Exception.Message)"
        $dtmapiDir = ''
    }
}
elseif ($env:DTMAPI_STATE_DIR) {
    try {
        $dtmapiDir = [System.IO.Path]::GetFullPath($env:DTMAPI_STATE_DIR)
    }
    catch {
        Add-DtmCollectWarning -Message "DTMAPI_STATE_DIR is not a valid path: $($_.Exception.Message)"
        $dtmapiDir = ''
    }
}
else {
    $dtmapiDir = ''
}
if ($DesktopTimestampOutput -and -not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    throw "Use only one of -OutputDirectory or -DesktopTimestampOutput."
}
$script:DtmCollectStagingOutput = ''
$script:DtmCollectFinalOutput = ''
$outputStamp = ''
if ($DesktopTimestampOutput) {
    if ($gameDir) {
        Assert-DtmApiGameNotRunning -GameDir $gameDir -Operation 'collect a stable public log bundle'
    }
    $desktop = [Environment]::GetFolderPath('Desktop')
    if ([string]::IsNullOrWhiteSpace($desktop)) {
        $desktop = Join-Path $env:USERPROFILE 'Desktop'
    }
    $outputStamp = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $desktopOutputRoot = Join-Path $desktop 'DTMAPI-logs'
    $script:DtmCollectFinalOutput = Join-Path $desktopOutputRoot $outputStamp
    $script:DtmCollectStagingOutput = Join-Path $desktopOutputRoot ('.' + $outputStamp + '.partial')
    $OutputDirectory = $script:DtmCollectStagingOutput
}
if ($OutputDirectory) {
    try {
        if ($DesktopTimestampOutput -and ((Test-Path -LiteralPath $script:DtmCollectFinalOutput) -or (Test-Path -LiteralPath $script:DtmCollectStagingOutput))) {
            throw "A log collection path already exists for token '$outputStamp'."
        }
        New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
        $evidence = (Resolve-Path -LiteralPath $OutputDirectory).Path
    }
    catch {
        if (-not $DesktopTimestampOutput) {
            throw
        }

        $failedDesktopOutput = $script:DtmCollectFinalOutput
        if (-not [string]::IsNullOrWhiteSpace($script:DtmCollectStagingOutput) -and (Test-Path -LiteralPath $script:DtmCollectStagingOutput)) {
            Remove-Item -Recurse -Force -LiteralPath $script:DtmCollectStagingOutput -ErrorAction SilentlyContinue
        }
        $fallbackRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'DTMAPI-logs'
        $script:DtmCollectFinalOutput = Join-Path $fallbackRoot $outputStamp
        $script:DtmCollectStagingOutput = Join-Path $fallbackRoot ('.' + $outputStamp + '.partial')
        $OutputDirectory = $script:DtmCollectStagingOutput
        Add-DtmCollectWarning -Message "Desktop log output '$failedDesktopOutput' could not be created: $($_.Exception.Message). Falling back to '$($script:DtmCollectFinalOutput)'."
        if ((Test-Path -LiteralPath $script:DtmCollectFinalOutput) -or (Test-Path -LiteralPath $script:DtmCollectStagingOutput)) {
            throw "A fallback log collection path already exists for token '$outputStamp'."
        }
        New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
        $evidence = (Resolve-Path -LiteralPath $OutputDirectory).Path
    }
}
else {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId $CaseId
}

try {
if ($gameDirResolveError) {
    Add-DtmCollectWarning -Message "Doloc Town game folder could not be resolved: $gameDirResolveError. LocalLow, Temp crash, Steam tail, process, and fatal-window diagnostics will still be collected."
}
elseif (-not $gameDir) {
    Add-DtmCollectWarning -Message "Doloc Town game folder was not found. LocalLow, Temp crash, Steam tail, process, and fatal-window diagnostics will still be collected."
}

$playerDoctorCandidates = New-Object 'System.Collections.Generic.List[string]'
$playerDoctorCandidates.Add((Join-Path $PSScriptRoot 'player-doctor\dtmapi-player-doctor.exe')) | Out-Null
if ($gameDir) {
    $playerDoctorCandidates.Add((Join-Path $gameDir 'DTMAPI\tools\player-doctor\dtmapi-player-doctor.exe')) | Out-Null
}
$playerDoctorCandidates.Add((Join-Path $repo 'dist\player-doctor\win-x64\dtmapi-player-doctor.exe')) | Out-Null
$playerDoctorExe = @($playerDoctorCandidates.ToArray() | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1)
if ($playerDoctorExe.Count -gt 0) {
    $playerDoctorExe = [string]$playerDoctorExe[0]
}
else {
    $playerDoctorExe = [string]$playerDoctorCandidates[0]
}
if ($gameDir -and (Test-Path -LiteralPath $playerDoctorExe -PathType Leaf)) {
    try {
        $playerDoctorResult = Invoke-DtmCollectPlayerDoctor -Executable $playerDoctorExe -GameRoot $gameDir -OutputRoot $evidence
        $playerDoctorStatus = [string]$playerDoctorResult.Status
        $playerDoctorExit = $playerDoctorResult.ExitCode
        $playerDoctorStdOutPath = [string]$playerDoctorResult.StdOutPath
        $playerDoctorStdErrPath = [string]$playerDoctorResult.StdErrPath
        if (-not [string]::IsNullOrWhiteSpace([string]$playerDoctorResult.CaptureError)) {
            Add-DtmCollectWarning -Message "Player Doctor output capture was incomplete: $($playerDoctorResult.CaptureError). Remaining diagnostics will continue."
        }
        if ($playerDoctorStatus -eq 'timeout') {
            $terminationDetail = if ([string]::IsNullOrWhiteSpace([string]$playerDoctorResult.TerminationError)) { '' } else { " Termination error: $($playerDoctorResult.TerminationError)" }
            Add-DtmCollectWarning -Message "Player Doctor timed out after 30 seconds and was terminated; remaining diagnostics will continue.$terminationDetail"
        }
        elseif ($playerDoctorStatus -eq 'crashed') {
            Add-DtmCollectWarning -Message "Player Doctor could not complete during log collection. Exit code: $playerDoctorExit. Captured output: $playerDoctorStdOutPath; $playerDoctorStdErrPath"
        }
        elseif ($playerDoctorStatus -eq 'findings') {
            Write-Host "Player Doctor completed with diagnostic findings (exit 2); its reports were collected."
        }
    }
    catch {
        $playerDoctorStatus = 'start-failed'
        Add-DtmCollectWarning -Message "Player Doctor could not be started during log collection: $($_.Exception.Message). Remaining diagnostics will continue."
    }
}
elseif (-not $gameDir) {
    $playerDoctorStatus = 'skipped-no-game'
    Write-Host '[INFO] Optional Player Doctor was skipped because the game folder could not be resolved.'
}
else {
    $playerDoctorStatus = 'optional-not-installed'
    Write-Host '[INFO] Optional Player Doctor is not installed; normal Runtime log collection continues without an EXE.'
}

function Try-CopyDtmEvidenceFileIfExists {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    try {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
        Copy-Item -Force -LiteralPath $Path -Destination $Destination
        return $true
    }
    catch {
        if (Test-Path -LiteralPath $Destination) {
            Remove-Item -Force -LiteralPath $Destination -ErrorAction SilentlyContinue
        }
        Add-DtmCollectWarning -Message "Failed to copy evidence file '$Path' to '$Destination': $($_.Exception.Message)"
        return $false
    }
}

function Copy-DtmEvidenceFileIfExists {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    [void](Try-CopyDtmEvidenceFileIfExists -Path $Path -Destination $Destination)
}

function Copy-DtmVerifiedFullEvidenceFileIfExists {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    try {
        $sourceBefore = Get-Item -LiteralPath $Path -ErrorAction Stop
        $sourceLength = [long]$sourceBefore.Length
        $sourceWriteTicks = [long]$sourceBefore.LastWriteTimeUtc.Ticks
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
        Copy-Item -Force -LiteralPath $sourceBefore.FullName -Destination $Destination -ErrorAction Stop

        $sourceAfter = Get-Item -LiteralPath $sourceBefore.FullName -ErrorAction Stop
        $destinationAfter = Get-Item -LiteralPath $Destination -ErrorAction Stop
        if ([long]$sourceAfter.Length -ne $sourceLength -or [long]$sourceAfter.LastWriteTimeUtc.Ticks -ne $sourceWriteTicks) {
            throw 'The source changed while it was being copied.'
        }
        if ([long]$destinationAfter.Length -ne $sourceLength) {
            throw "The copied length is $($destinationAfter.Length), expected $sourceLength."
        }

        $sourceHash = Get-DtmApiFileSha256 -Path $sourceAfter.FullName
        $destinationHash = Get-DtmApiFileSha256 -Path $destinationAfter.FullName
        if (-not [string]::Equals($sourceHash, $destinationHash, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'The copied SHA-256 does not match the source.'
        }
        return $true
    }
    catch {
        if (Test-Path -LiteralPath $Destination) {
            Remove-Item -Force -LiteralPath $Destination -ErrorAction SilentlyContinue
        }
        throw "Could not export a complete, stable copy of '$Path': $($_.Exception.Message)"
    }
}

function Copy-DtmBoundedEvidenceFileIfExists {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Destination,
        [Parameter(Mandatory = $true)] [long] $MaxBytes
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return
    }
    if ($MaxBytes -lt 4KB) {
        throw "Bounded evidence limit must be at least 4096 bytes: $MaxBytes"
    }

    try {
        $sourceItem = Get-Item -LiteralPath $Path -ErrorAction Stop
        if ($sourceItem.Length -le $MaxBytes) {
            [void](Try-CopyDtmEvidenceFileIfExists -Path $Path -Destination $Destination)
            return
        }

        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
        $markerText = "[DTMAPI collection note: source log was $($sourceItem.Length) bytes; only the newest $MaxBytes bytes are included.]`r`n"
        $marker = [System.Text.Encoding]::UTF8.GetBytes($markerText)
        $tailBytes = $MaxBytes - $marker.Length
        $source = [System.IO.File]::Open($sourceItem.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $target = $null
        try {
            $source.Position = [Math]::Max([long]0, $source.Length - $tailBytes)
            $target = [System.IO.File]::Open($Destination, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            $target.Write($marker, 0, $marker.Length)
            $buffer = New-Object byte[] 65536
            while (($read = $source.Read($buffer, 0, $buffer.Length)) -gt 0) {
                $target.Write($buffer, 0, $read)
            }
        }
        finally {
            if ($null -ne $target) { $target.Dispose() }
            $source.Dispose()
        }
        Write-Host ("[INFO] Truncated copied log to {0} bytes: {1}" -f $MaxBytes, $Path)
    }
    catch {
        if (Test-Path -LiteralPath $Destination) {
            Remove-Item -Force -LiteralPath $Destination -ErrorAction SilentlyContinue
        }
        Add-DtmCollectWarning -Message "Failed to copy bounded evidence file '$Path' to '$Destination': $($_.Exception.Message)"
    }
}

function Add-DtmMissingCrashDumpInstruction {
    param(
        [System.Collections.Generic.List[string]] $Instructions,
        [Parameter(Mandatory = $true)] [string] $Reason,
        [Parameter(Mandatory = $true)] [System.IO.FileInfo] $File,
        [string] $Details = ''
    )

    $Instructions.Add('DTMAPI could not include a Unity crash dump in the collected logs.')
    $Instructions.Add("Reason=$Reason")
    $Instructions.Add("Path=$($File.FullName)")
    if (-not [string]::IsNullOrWhiteSpace($Details)) {
        $Instructions.Add("Details=$Details")
    }
    $Instructions.Add('Please send this crash.dmp separately together with the DTMAPI logs if possible.')
    $Instructions.Add('')
}

function Copy-DtmFirstEvidenceFileIfExists {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Paths,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    foreach ($path in @($Paths)) {
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            Copy-DtmEvidenceFileIfExists -Path $path -Destination $Destination
            return
        }
    }
}

function Copy-DtmNewestEvidenceFileIfExists {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Paths,
        [Parameter(Mandatory = $true)] [string] $Destination,
        [long] $MaxBytes = 0,
        [switch] $VerifyFullCopy
    )

    $newest = $null
    foreach ($path in @($Paths)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            continue
        }

        try {
            $item = Get-Item -LiteralPath $path -ErrorAction Stop
            if ($null -eq $newest -or $item.LastWriteTimeUtc -gt $newest.LastWriteTimeUtc) {
                $newest = $item
            }
        }
        catch {
            Add-DtmCollectWarning -Message "Failed to inspect evidence file '$path': $($_.Exception.Message)"
        }
    }

    if ($null -ne $newest) {
        if ($VerifyFullCopy) {
            [void](Copy-DtmVerifiedFullEvidenceFileIfExists -Path $newest.FullName -Destination $Destination)
        }
        elseif ($MaxBytes -gt 0) {
            Copy-DtmBoundedEvidenceFileIfExists -Path $newest.FullName -Destination $Destination -MaxBytes $MaxBytes
        }
        else {
            Copy-DtmEvidenceFileIfExists -Path $newest.FullName -Destination $Destination
        }
    }
}

function Copy-DtmRecentEvidenceFiles {
    param(
        [Parameter(Mandatory = $true)] [string] $Directory,
        [Parameter(Mandatory = $true)] [string] $Filter,
        [Parameter(Mandatory = $true)] [string] $DestinationDirectory,
        [int] $Count = 5
    )

    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        return
    }

    New-Item -ItemType Directory -Force -Path $DestinationDirectory | Out-Null
    $files = @(Get-ChildItem -LiteralPath $Directory -Filter $Filter -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First $Count)
    foreach ($file in $files) {
        Copy-DtmEvidenceFileIfExists -Path $file.FullName -Destination (Join-Path $DestinationDirectory $file.Name)
    }
}

function Write-DtmRecentReportSummary {
    param(
        [Parameter(Mandatory = $true)] [string] $Directory,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    $summary = New-Object System.Collections.Generic.List[string]
    $summary.Add("ReportsDirectory=$Directory")
    $summary.Add("FullReportsCopied=False")
    $summary.Add("Reason=Full dtmapi-report zip files are not copied by default because they can already contain Unity crash dumps. Use the listed path if a specific historical report is needed.")
    $summary.Add('')
    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        $summary.Add('Exists=False')
        $summary | Set-Content -LiteralPath $Destination
        return
    }

    $summary.Add('Exists=True')
    $reports = @(Get-ChildItem -LiteralPath $Directory -Filter 'dtmapi-report-*.*' -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 10)
    foreach ($report in $reports) {
        $summary.Add(("{0}`t{1:o}`t{2}`t{3}" -f $report.Name, $report.LastWriteTimeUtc, $report.Length, $report.FullName))
    }

    if ($reports.Count -eq 0) {
        $summary.Add('(none)')
    }

    $summary | Set-Content -LiteralPath $Destination
}

function Get-DtmRecentTextLines {
    param(
        [string] $Path,
        [int] $Tail = 300
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return @()
    }

    try {
        return @(Get-Content -LiteralPath $Path -Tail $Tail -ErrorAction Stop)
    }
    catch {
        Add-DtmCollectWarning -Message "Failed to read text tail '$Path': $($_.Exception.Message)"
        return @()
    }
}

function Write-DtmSupportContext {
    param(
        [Parameter(Mandatory = $true)] [string] $Destination,
        [string] $DtmApiDir,
        [string] $GameDir
    )

    $context = New-Object System.Collections.Generic.List[string]
    $context.Add("Collected=$(Get-Date -Format o)")
    $context.Add("GameDir=$GameDir")
    $context.Add("DtmApiStateDir=$DtmApiDir")
    $context.Add('')

    $latestLog = if (-not [string]::IsNullOrWhiteSpace($DtmApiDir)) { Join-Path $DtmApiDir 'logs\latest.log' } else { '' }
    $bepInExLog = if (-not [string]::IsNullOrWhiteSpace($GameDir)) { Join-Path $GameDir 'BepInEx\LogOutput.log' } else { '' }
    $latestLines = @(Get-DtmRecentTextLines -Path $latestLog -Tail 5000)
    $latestTail = @($latestLines | Select-Object -Last 120)
    $bepInExTail = @(Get-DtmRecentTextLines -Path $bepInExLog -Tail 500)

    $context.Add("DTMAPILatestLog=$latestLog")
    $context.Add("DTMAPILatestLogExists=$([bool]($latestLines.Count -gt 0))")
    $context.Add("DTMAPIRuntimeStartingCount=$(@($latestLines | Where-Object { $_ -match 'DTMAPI runtime starting\.' }).Count)")
    $context.Add("DTMAPIOnApplicationQuitObserved=$(@($latestLines | Where-Object { $_ -match 'Unity OnApplicationQuit observed by DTMAPI bootstrap' }).Count -gt 0)")
    $context.Add("BepInExLog=$bepInExLog")
    $context.Add("BepInExLogTailExists=$([bool]($bepInExTail.Count -gt 0))")
    $lastGivePath = if (-not [string]::IsNullOrWhiteSpace($DtmApiDir)) { Join-Path $DtmApiDir 'debug-console-last-give.txt' } else { '' }
    if (-not [string]::IsNullOrWhiteSpace($lastGivePath) -and (Test-Path -LiteralPath $lastGivePath)) {
        $context.Add("DebugConsoleLastGive=" + ((Get-DtmRecentTextLines -Path $lastGivePath -Tail 5) -join ' | '))
    }
    else {
        $context.Add('DebugConsoleLastGive=(missing)')
    }
    $context.Add('')

    $failurePattern = 'Failed to load|Failed to patch|Harmony|Owner can''t be an array|Exception|api-too-new|MinimumDTMApiVersion'
    $failureLines = @($latestLines + $bepInExTail | Where-Object { $_ -match $failurePattern } | Select-Object -Last 80)
    $context.Add("RecentModOrPatchFailureLines=$($failureLines.Count)")
    foreach ($line in $failureLines) {
        $context.Add("FAILURE-LINE " + $line)
    }
    $context.Add('')

    $context.Add('DTMAPI latest.log tail:')
    if ($latestTail.Count -eq 0) {
        $context.Add('(none)')
    }
    else {
        foreach ($line in $latestTail) {
            $context.Add($line)
        }
    }

    try {
        $context | Set-Content -LiteralPath (Join-Path $Destination 'support-context.txt')
    }
    catch {
        Add-DtmCollectWarning -Message "Failed to write support-context.txt: $($_.Exception.Message)"
    }
}

function Get-DtmSafePathSegment {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return '_'
    }

    $safe = $Value
    foreach ($c in [System.IO.Path]::GetInvalidFileNameChars()) {
        $safe = $safe.Replace($c, '_')
    }

    return $safe
}

function Get-DtmShortPathHash {
    param([string] $Value)

    $text = if ([string]::IsNullOrWhiteSpace($Value)) { '' } else { $Value }
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
            $hash = $sha.ComputeHash($bytes)
            $parts = New-Object System.Collections.Generic.List[string]
            for ($i = 0; $i -lt 6 -and $i -lt $hash.Length; $i++) {
                $parts.Add($hash[$i].ToString('x2'))
            }

            return ($parts -join '')
        }
        finally {
            $sha.Dispose()
        }
    }
    catch {
        Add-DtmCollectWarning -Message "Failed to hash crash path '$text': $($_.Exception.Message)"
        return ([Math]::Abs($text.GetHashCode())).ToString('x')
    }
}

function Get-DtmCrashDirectorySegment {
    param([Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory)

    return (Get-DtmSafePathSegment -Value $Directory.Name) + '-' + (Get-DtmShortPathHash -Value $Directory.FullName)
}

function Get-DtmRelativePath {
    param(
        [Parameter(Mandatory = $true)] [string] $BasePath,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\')
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if ($pathFull.StartsWith($baseFull + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($baseFull.Length + 1)
    }

    return [System.IO.Path]::GetFileName($pathFull)
}

function Get-DtmUnityCrashRootCandidates {
    $roots = New-Object System.Collections.Generic.List[string]
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($tempRoot in @([System.IO.Path]::GetTempPath(), $env:TEMP, $env:TMP)) {
        if ([string]::IsNullOrWhiteSpace($tempRoot)) {
            continue
        }

        foreach ($relativeRoot in @('RedSawGames\DolocTown\Crashes', 'RedSawGames\Doloc Town\Crashes')) {
            try {
                $root = Join-Path $tempRoot $relativeRoot
                $fullRoot = [System.IO.Path]::GetFullPath($root).TrimEnd('\')
                if ($seen.Add($fullRoot)) {
                    $roots.Add($fullRoot)
                }
            }
            catch {
                Add-DtmCollectWarning -Message "Failed to resolve Unity crash root candidate '$tempRoot\$relativeRoot': $($_.Exception.Message)"
            }
        }
    }

    return @($roots)
}

function Test-DtmFileSystemInfoIsReparsePoint {
    param([System.IO.FileSystemInfo] $Item)

    return $null -ne $Item -and (($Item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)
}

function Test-DtmPathIsSameOrChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    if ([string]::IsNullOrWhiteSpace($Child) -or [string]::IsNullOrWhiteSpace($Parent)) {
        return $false
    }

    try {
        $childFull = [System.IO.Path]::GetFullPath($Child).TrimEnd('\')
        $parentFull = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\')
        return $childFull.Equals($parentFull, [System.StringComparison]::OrdinalIgnoreCase) -or
            $childFull.StartsWith($parentFull + '\', [System.StringComparison]::OrdinalIgnoreCase)
    }
    catch {
        return $false
    }
}

function Test-DtmPathIsExcludedRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [string[]] $ExcludedRoots = @()
    )

    foreach ($root in @($ExcludedRoots)) {
        if (Test-DtmPathIsSameOrChildPath -Child $Path -Parent $root) {
            return $true
        }
    }

    return $false
}

function Get-DtmRecentCrashDirectories {
    param(
        [Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Root,
        [Parameter(Mandatory = $true)] [int] $MaxDirectories,
        [System.Collections.Generic.List[string]] $Summary
    )

    $recent = New-Object System.Collections.Generic.List[System.IO.DirectoryInfo]
    if ($MaxDirectories -le 0) {
        return @()
    }

    try {
        foreach ($dir in $Root.EnumerateDirectories()) {
            if (Test-DtmFileSystemInfoIsReparsePoint -Item $dir) {
                $Summary.Add("SkippedReparseDirectory=$($dir.FullName)")
                continue
            }

            $insertAt = -1
            for ($i = 0; $i -lt $recent.Count; $i++) {
                if ($dir.LastWriteTimeUtc -gt $recent[$i].LastWriteTimeUtc) {
                    $insertAt = $i
                    break
                }
            }

            if ($insertAt -lt 0) {
                $recent.Add($dir) | Out-Null
            }
            else {
                $recent.Insert($insertAt, $dir)
            }

            if ($recent.Count -gt $MaxDirectories) {
                $recent.RemoveAt($recent.Count - 1)
            }
        }
    }
    catch {
        $Summary.Add("DirectoryListError=$($Root.FullName) error=$($_.Exception.GetType().Name): $($_.Exception.Message)")
    }

    return @($recent)
}

function Add-DtmRecentCrashDirectoryCandidate {
    param(
        [System.Collections.Generic.List[System.IO.DirectoryInfo]] $Recent,
        [Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory,
        [Parameter(Mandatory = $true)] [int] $MaxDirectories
    )

    $insertAt = -1
    for ($i = 0; $i -lt $Recent.Count; $i++) {
        if ((Compare-DtmCrashDirectoryPriority -Left $Directory -Right $Recent[$i]) -lt 0) {
            $insertAt = $i
            break
        }
    }

    if ($insertAt -lt 0) {
        $Recent.Add($Directory) | Out-Null
    }
    else {
        $Recent.Insert($insertAt, $Directory)
    }

    if ($Recent.Count -gt $MaxDirectories) {
        $Recent.RemoveAt($Recent.Count - 1)
    }

    foreach ($existing in $Recent) {
        if ((Test-DtmPathIsSameOrChildPath -Child $existing.FullName -Parent $Directory.FullName) -and
            (Test-DtmPathIsSameOrChildPath -Child $Directory.FullName -Parent $existing.FullName)) {
            return $true
        }
    }

    return $false
}

function Add-DtmUniqueCrashDirectoryCandidate {
    param(
        [System.Collections.Generic.List[System.IO.DirectoryInfo]] $Selected,
        [Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory,
        [Parameter(Mandatory = $true)] [int] $MaxDirectories
    )

    foreach ($existing in $Selected) {
        if ((Test-DtmPathIsSameOrChildPath -Child $existing.FullName -Parent $Directory.FullName) -and
            (Test-DtmPathIsSameOrChildPath -Child $Directory.FullName -Parent $existing.FullName)) {
            return $true
        }
    }

    if ($Selected.Count -ge $MaxDirectories) {
        return $false
    }

    $Selected.Add($Directory) | Out-Null
    return $true
}

function Test-DtmCrashDirectoryHasDump {
    param([Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory)
    return Test-Path -LiteralPath (Join-Path $Directory.FullName 'crash.dmp') -PathType Leaf
}

function Get-DtmCrashDirectorySortTimeUtc {
    param([Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory)

    $latest = $Directory.LastWriteTimeUtc
    $dump = Join-Path $Directory.FullName 'crash.dmp'
    if (Test-Path -LiteralPath $dump -PathType Leaf) {
        try {
            $dumpTime = (Get-Item -LiteralPath $dump -ErrorAction Stop).LastWriteTimeUtc
            if ($dumpTime -gt $latest) {
                $latest = $dumpTime
            }
        }
        catch {
        }
    }

    return $latest
}

function Compare-DtmCrashDirectoryPriority {
    param(
        [Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Left,
        [Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Right
    )

    $leftHasDump = Test-DtmCrashDirectoryHasDump -Directory $Left
    $rightHasDump = Test-DtmCrashDirectoryHasDump -Directory $Right
    if ($leftHasDump -and -not $rightHasDump) {
        return -1
    }
    if ($rightHasDump -and -not $leftHasDump) {
        return 1
    }

    $leftTime = Get-DtmCrashDirectorySortTimeUtc -Directory $Left
    $rightTime = Get-DtmCrashDirectorySortTimeUtc -Directory $Right
    if ($leftTime -gt $rightTime) {
        return -1
    }
    if ($rightTime -gt $leftTime) {
        return 1
    }

    return 0
}

function Get-DtmRecentCrashDirectoriesAcrossRoots {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Roots,
        [Parameter(Mandatory = $true)] [int] $MaxDirectories,
        [System.Collections.Generic.List[string]] $Summary,
        [System.Collections.Generic.List[string]] $MissingCrashDumpInstructions
    )

    $allCandidates = New-Object System.Collections.Generic.List[System.IO.DirectoryInfo]
    $rootEntries = New-Object System.Collections.Generic.List[object]
    if ($MaxDirectories -le 0) {
        return @()
    }

    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) {
            continue
        }

        try {
            $rootInfo = New-Object System.IO.DirectoryInfo($root)
            $rootCandidates = New-Object System.Collections.Generic.List[System.IO.DirectoryInfo]
            $rootDirectoryCount = 0
            foreach ($dir in $rootInfo.EnumerateDirectories()) {
                if (Test-DtmFileSystemInfoIsReparsePoint -Item $dir) {
                    $Summary.Add("SkippedReparseDirectory=$($dir.FullName)")
                    continue
                }

                $rootDirectoryCount++
                $rootCandidates.Add($dir) | Out-Null
                $allCandidates.Add($dir) | Out-Null
            }
            $Summary.Add("CrashRootDirectoryCount=$root count=$rootDirectoryCount")
            $rootEntries.Add([pscustomobject]@{
                Root = $root
                Dirs = $rootCandidates
            }) | Out-Null
        }
        catch {
            $Summary.Add("DirectoryListError=$root error=$($_.Exception.GetType().Name): $($_.Exception.Message)")
            if ($null -ne $MissingCrashDumpInstructions) {
                $placeholder = New-Object System.IO.FileInfo((Join-Path $root 'crash.dmp'))
                Add-DtmMissingCrashDumpInstruction -Instructions $MissingCrashDumpInstructions -Reason 'directory-read-error' -File $placeholder -Details "$($_.Exception.GetType().Name): $($_.Exception.Message)"
            }
        }
    }

    $selected = New-Object System.Collections.Generic.List[System.IO.DirectoryInfo]
    if ($allCandidates.Count -eq 0) {
        return @()
    }

    $latestBudget = [Math]::Min($MaxDirectories, [Math]::Max($rootEntries.Count, [Math]::Min(3, $allCandidates.Count)))
    foreach ($rootEntry in $rootEntries) {
        $newestForRoot = @($rootEntry.Dirs | Sort-Object `
            @{ Expression = { Get-DtmCrashDirectorySortTimeUtc -Directory $_ }; Descending = $true }, `
            @{ Expression = { $_.FullName }; Descending = $false } | Select-Object -First 1)
        if ($newestForRoot.Count -gt 0) {
            Add-DtmUniqueCrashDirectoryCandidate -Selected $selected -Directory $newestForRoot[0] -MaxDirectories $MaxDirectories | Out-Null
        }
    }

    foreach ($dir in @($allCandidates | Sort-Object `
        @{ Expression = { Get-DtmCrashDirectorySortTimeUtc -Directory $_ }; Descending = $true }, `
        @{ Expression = { $_.FullName }; Descending = $false })) {
        if ($selected.Count -ge $latestBudget) {
            break
        }
        Add-DtmUniqueCrashDirectoryCandidate -Selected $selected -Directory $dir -MaxDirectories $MaxDirectories | Out-Null
    }

    foreach ($dir in @($allCandidates | Sort-Object `
        @{ Expression = { if (Test-DtmCrashDirectoryHasDump -Directory $_) { 1 } else { 0 } }; Descending = $true }, `
        @{ Expression = { Get-DtmCrashDirectorySortTimeUtc -Directory $_ }; Descending = $true }, `
        @{ Expression = { $_.FullName }; Descending = $false })) {
        if ($selected.Count -ge $MaxDirectories) {
            break
        }
        Add-DtmUniqueCrashDirectoryCandidate -Selected $selected -Directory $dir -MaxDirectories $MaxDirectories | Out-Null
    }

    foreach ($dir in @($allCandidates | Sort-Object `
        @{ Expression = { Get-DtmCrashDirectorySortTimeUtc -Directory $_ }; Descending = $true }, `
        @{ Expression = { $_.FullName }; Descending = $false })) {
        if ($selected.Count -ge $MaxDirectories) {
            break
        }
        Add-DtmUniqueCrashDirectoryCandidate -Selected $selected -Directory $dir -MaxDirectories $MaxDirectories | Out-Null
    }

    $Summary.Add('CrashDirectorySelectionMode=latest-per-root-plus-latest-then-dump-priority')
    $Summary.Add("CrashDirectoryLatestBudget=$latestBudget")
    foreach ($rootEntry in $rootEntries) {
        $kept = 0
        foreach ($dir in $selected) {
            if (Test-DtmPathIsSameOrChildPath -Child $dir.FullName -Parent $rootEntry.Root) {
                $kept++
            }
        }
        $skipped = [Math]::Max(0, $rootEntry.Dirs.Count - $kept)
        if ($skipped -gt 0) {
            $Summary.Add("CrashRootDirectorySkippedByBudget=$($rootEntry.Root) count=$skipped")
        }
    }

    return @($selected)
}

function Get-DtmCrashFilesByPattern {
    param(
        [Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory,
        [Parameter(Mandatory = $true)] [string] $Pattern,
        [System.Collections.Generic.List[string]] $Summary,
        [int] $MaxFiles = 80,
        [string[]] $ExcludedRoots = @()
    )

    if ($null -eq $Summary) {
        $Summary = New-Object System.Collections.Generic.List[string]
    }

    if ($MaxFiles -le 0) {
        return
    }

    $emitted = 0
    $stack = New-Object System.Collections.Generic.Stack[System.IO.DirectoryInfo]
    $stack.Push($Directory)
    while ($stack.Count -gt 0) {
        $current = $stack.Pop()
        if (Test-DtmPathIsExcludedRoot -Path $current.FullName -ExcludedRoots $ExcludedRoots) {
            $Summary.Add("SkippedOutputDirectory=$($current.FullName) pattern=$Pattern")
            continue
        }

        if (Test-DtmFileSystemInfoIsReparsePoint -Item $current) {
            $Summary.Add("SkippedReparseDirectory=$($current.FullName) pattern=$Pattern")
            continue
        }

        try {
            foreach ($file in $current.EnumerateFiles($Pattern)) {
                if (Test-DtmPathIsExcludedRoot -Path $file.FullName -ExcludedRoots $ExcludedRoots) {
                    $Summary.Add("SkippedOutputFile=$($file.FullName) pattern=$Pattern")
                    continue
                }

                if (Test-DtmFileSystemInfoIsReparsePoint -Item $file) {
                    $Summary.Add("SkippedReparseFile=$($file.FullName) pattern=$Pattern")
                    continue
                }

                Write-Output $file
                $emitted++
                if ($emitted -ge $MaxFiles) {
                    return
                }
            }
        }
        catch {
            $Summary.Add("FileListError=$($current.FullName) pattern=$Pattern error=$($_.Exception.GetType().Name): $($_.Exception.Message)")
        }

        try {
            foreach ($child in $current.EnumerateDirectories()) {
                $stack.Push($child)
            }
        }
        catch {
            $Summary.Add("DirectoryListError=$($current.FullName) error=$($_.Exception.GetType().Name): $($_.Exception.Message)")
        }
    }
}

function Get-DtmCrashFiles {
    param(
        [Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory,
        [System.Collections.Generic.List[string]] $Summary,
        [int] $MaxFiles = 80,
        [string[]] $ExcludedRoots = @()
    )

    if ($null -eq $Summary) {
        $Summary = New-Object System.Collections.Generic.List[string]
    }

    if ($MaxFiles -le 0) {
        return
    }

    $seen = @{}
    $emitted = 0
    foreach ($name in $script:DtmUnityCrashPreferredFiles) {
        foreach ($file in Get-DtmCrashFilesByPattern -Directory $Directory -Pattern $name -Summary $Summary -MaxFiles ($MaxFiles - $emitted) -ExcludedRoots $ExcludedRoots) {
            $key = $file.FullName.ToLowerInvariant()
            if ($seen.ContainsKey($key)) {
                continue
            }

            $seen[$key] = $true
            Write-Output $file
            $emitted++
            if ($emitted -ge $MaxFiles) {
                return
            }
        }
    }

    foreach ($file in Get-DtmCrashFilesByPattern -Directory $Directory -Pattern '*' -Summary $Summary -MaxFiles ($MaxFiles - $emitted) -ExcludedRoots $ExcludedRoots) {
        $key = $file.FullName.ToLowerInvariant()
        if ($seen.ContainsKey($key)) {
            continue
        }

        $seen[$key] = $true
        Write-Output $file
        $emitted++
        if ($emitted -ge $MaxFiles) {
            return
        }
    }
}

function Copy-DtmUnityCrashReports {
    param([Parameter(Mandatory = $true)] [string] $Destination)

    $crashDest = Join-Path $Destination 'Unity-Crashes'
    New-Item -ItemType Directory -Force -Path $crashDest | Out-Null
    $summary = New-Object System.Collections.Generic.List[string]
    $summary.Add("Collected=$(Get-Date -Format o)")
    $summary.Add("MaxDirectories=$script:DtmUnityCrashMaxDirectories")
    $summary.Add("MaxFilesPerDirectory=$script:DtmUnityCrashMaxFilesPerDirectory")
    $summary.Add("MaxFilesTotal=$script:DtmUnityCrashMaxFilesTotal")
    $summary.Add("MaxFileBytes=$script:DtmUnityCrashMaxFileBytes")
    $summary.Add("MaxDumpFileBytes=$script:DtmUnityCrashMaxDumpFileBytes")
    $summary.Add("MaxTotalBytes=$script:DtmUnityCrashMaxTotalBytes")
    $summary.Add("MaxTotalBytesWithDump=$script:DtmUnityCrashMaxTotalBytesWithDump")
    $summary.Add("IncludeCrashDumps=$([bool]$IncludeCrashDumps)")
    $summary.Add('')

    $copiedDirectories = 0
    $copiedFiles = 0
    $consideredFiles = 0
    $copiedBytes = 0
    $copiedPrimaryCrashDump = $false
    $missingCrashDumpInstructions = New-Object System.Collections.Generic.List[string]
    $crashRoots = @(Get-DtmUnityCrashRootCandidates)
    $dirs = @(Get-DtmRecentCrashDirectoriesAcrossRoots -Roots $crashRoots -MaxDirectories $script:DtmUnityCrashMaxDirectories -Summary $summary -MissingCrashDumpInstructions $missingCrashDumpInstructions)
    foreach ($root in $crashRoots) {
        $summary.Add("CrashRoot=$root")
        if (-not (Test-Path -LiteralPath $root -PathType Container)) {
            $summary.Add('Exists=False')
            $summary.Add('')
            continue
        }

        $summary.Add('Exists=True')
        $rootFullPath = [System.IO.Path]::GetFullPath($root).TrimEnd('\')
        $rootCount = @($dirs | Where-Object { $_.FullName.StartsWith($rootFullPath, [System.StringComparison]::OrdinalIgnoreCase) }).Count
        $summary.Add("RecentDirectoryCountFromRoot=$rootCount")
        $summary.Add('')
    }

    $summary.Add("GlobalRecentDirectoryCount=$($dirs.Count)")
    foreach ($dir in $dirs) {
        $copiedDirectories++
        $safeDirName = Get-DtmCrashDirectorySegment -Directory $dir
        $dirDest = Join-Path $crashDest $safeDirName
        New-Item -ItemType Directory -Force -Path $dirDest | Out-Null
        $summary.Add("Directory=$($dir.FullName)")
        $summary.Add("DirectoryLastWrite=$($dir.LastWriteTime.ToString('o'))")

        $directoryFilesConsidered = 0
        $directorySawCrashDump = $false
        $directoryBudget = [Math]::Min($script:DtmUnityCrashMaxFilesPerDirectory, $script:DtmUnityCrashMaxFilesTotal - $consideredFiles)
        if ($directoryBudget -le 0) {
            $summary.Add("SkippedTotalFileLimitBeforeDirectory=$($dir.FullName)")
            $summary.Add('DirectoryFilesConsidered=0')
            continue
        }

        foreach ($file in Get-DtmCrashFiles -Directory $dir -Summary $summary -MaxFiles $directoryBudget -ExcludedRoots @($Destination, $crashDest)) {
            $directoryFilesConsidered++
            $consideredFiles++
            if ($file.Name.Equals('crash.dmp', [System.StringComparison]::OrdinalIgnoreCase)) {
                $directorySawCrashDump = $true
            }

            $relative = Get-DtmRelativePath -BasePath $dir.FullName -Path $file.FullName
            $relativeParts = @()
            foreach ($part in ($relative -split '[\\/]')) {
                $relativeParts += (Get-DtmSafePathSegment -Value $part)
            }

            $relativeDest = ''
            foreach ($segment in $relativeParts) {
                if ([string]::IsNullOrWhiteSpace($relativeDest)) {
                    $relativeDest = $segment
                }
                else {
                    $relativeDest = Join-Path $relativeDest $segment
                }
            }

            $fileDest = Join-Path $dirDest $relativeDest
            try {
                $file.Refresh()
                if (-not $file.Exists) {
                    $summary.Add("SkippedMissingFile=$($file.FullName)")
                    continue
                }

                $isCrashDump = $file.Name.Equals('crash.dmp', [System.StringComparison]::OrdinalIgnoreCase)
                if ($isCrashDump -and -not $IncludeCrashDumps) {
                    $summary.Add("SkippedCrashDumpByDefault=$($file.FullName) bytes=$($file.Length)")
                    Add-DtmMissingCrashDumpInstruction -Instructions $missingCrashDumpInstructions -Reason 'default-bounded-mode' -File $file -Details 'Crash dumps are not copied by default. Re-run collect-logs.ps1 with -IncludeCrashDumps only when support explicitly requests the dump.'
                    continue
                }
                $isPrimaryCrashDump = $isCrashDump -and -not $copiedPrimaryCrashDump
                $maxFileBytes = if ($isPrimaryCrashDump) { $script:DtmUnityCrashMaxDumpFileBytes } else { $script:DtmUnityCrashMaxFileBytes }
                $maxTotalBytes = if ($isPrimaryCrashDump -or $copiedPrimaryCrashDump) { $script:DtmUnityCrashMaxTotalBytesWithDump } else { $script:DtmUnityCrashMaxTotalBytes }

                if ($file.Length -gt $maxFileBytes) {
                    $summary.Add("SkippedLargeFile=$($file.FullName) bytes=$($file.Length) maxBytes=$maxFileBytes")
                    if ($isCrashDump) {
                        Add-DtmMissingCrashDumpInstruction -Instructions $missingCrashDumpInstructions -Reason 'large-file' -File $file -Details "size=$($file.Length), maxBytes=$maxFileBytes"
                    }
                    continue
                }

                if (($copiedBytes + $file.Length) -gt $maxTotalBytes) {
                    $summary.Add("SkippedTotalByteLimit=$($file.FullName) bytes=$($file.Length) currentBytes=$copiedBytes maxTotalBytes=$maxTotalBytes")
                    if ($isCrashDump) {
                        Add-DtmMissingCrashDumpInstruction -Instructions $missingCrashDumpInstructions -Reason 'total-byte-budget' -File $file -Details "size=$($file.Length), currentBytes=$copiedBytes, maxTotalBytes=$maxTotalBytes"
                    }
                    continue
                }

                $copied = Try-CopyDtmEvidenceFileIfExists -Path $file.FullName -Destination $fileDest
                if (-not $copied) {
                    $summary.Add("CopyFailed=$($file.FullName)")
                    if ($isCrashDump) {
                        Add-DtmMissingCrashDumpInstruction -Instructions $missingCrashDumpInstructions -Reason 'copy-failed' -File $file -Details "destination=$fileDest"
                    }
                    continue
                }

                $copiedFiles++
                $copiedBytes += $file.Length
                if ($isPrimaryCrashDump) {
                    $copiedPrimaryCrashDump = $true
                }
                $summary.Add("CopiedFile=$($file.FullName) bytes=$($file.Length)")
            }
            catch {
                $summary.Add("FileReadError=$($file.FullName) error=$($_.Exception.GetType().Name): $($_.Exception.Message)")
                if ($null -ne $file -and $file.Name.Equals('crash.dmp', [System.StringComparison]::OrdinalIgnoreCase)) {
                    Add-DtmMissingCrashDumpInstruction -Instructions $missingCrashDumpInstructions -Reason 'read-error' -File $file -Details "$($_.Exception.GetType().Name): $($_.Exception.Message)"
                }
                continue
            }
        }

        if (-not $directorySawCrashDump) {
            $expectedDump = Join-Path $dir.FullName 'crash.dmp'
            $summary.Add("MissingCrashDumpFile=$expectedDump")
            $placeholder = New-Object System.IO.FileInfo($expectedDump)
            Add-DtmMissingCrashDumpInstruction -Instructions $missingCrashDumpInstructions -Reason 'missing-file' -File $placeholder -Details 'Unity crash directory existed but no crash.dmp file was found or enumerable.'
        }
        if ($directoryFilesConsidered -ge $script:DtmUnityCrashMaxFilesPerDirectory) {
            $summary.Add("SkippedDirectoryFileLimit=$($dir.FullName)")
        }
        if ($consideredFiles -ge $script:DtmUnityCrashMaxFilesTotal) {
            $summary.Add("SkippedTotalFileLimitAfterDirectory=$($dir.FullName)")
        }
        $summary.Add("DirectoryFilesConsidered=$directoryFilesConsidered")
    }

    if ($copiedDirectories -eq 0) {
        $summary.Add('No Unity crash report directories were found for the current Windows user.')
    }

    $summary.Add("FilesConsideredTotal=$consideredFiles")
    $summary.Add("FilesCopiedTotal=$copiedFiles")
    $summary.Add("BytesCopiedTotal=$copiedBytes")

    if ($missingCrashDumpInstructions.Count -gt 0) {
        try {
            $missingCrashDumpInstructions | Set-Content -LiteralPath (Join-Path $crashDest 'MISSING-CRASH-DUMP-README.txt')
        }
        catch {
            Add-DtmCollectWarning -Message "Failed to write missing crash dump instructions: $($_.Exception.Message)"
        }
    }

    try {
        $summary | Set-Content -LiteralPath (Join-Path $crashDest 'summary.txt')
    }
    catch {
        Add-DtmCollectWarning -Message "Failed to write Unity crash summary: $($_.Exception.Message)"
    }
}

function Get-SteamRootCandidates {
    $roots = New-Object System.Collections.Generic.List[string]
    foreach ($registryPath in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $steamPath = (Get-ItemProperty -Path $registryPath -ErrorAction Stop).SteamPath
            if ($steamPath -and (Test-Path -LiteralPath $steamPath)) {
                $resolved = (Resolve-Path -LiteralPath $steamPath).Path
                if (-not $roots.Contains($resolved)) {
                    $roots.Add($resolved)
                }
            }
        }
        catch {
        }
    }

    return @($roots)
}

function Get-SteamLibraryRoots {
    param(
        [string] $SteamRoot
    )

    $roots = New-Object System.Collections.Generic.List[string]
    if ([string]::IsNullOrWhiteSpace($SteamRoot)) {
        return @($roots)
    }

    try {
        if ($SteamRoot -and (Test-Path -LiteralPath $SteamRoot)) {
            $roots.Add((Resolve-Path -LiteralPath $SteamRoot -ErrorAction Stop).Path)
        }

        $libraryFile = Join-Path $SteamRoot 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $libraryFile) {
            $text = Get-Content -Raw -LiteralPath $libraryFile -ErrorAction Stop
            foreach ($match in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
                $path = $match.Groups[1].Value.Replace('\\', '\')
                if (Test-Path -LiteralPath $path) {
                    try {
                        $resolved = (Resolve-Path -LiteralPath $path -ErrorAction Stop).Path
                        if (-not $roots.Contains($resolved)) {
                            $roots.Add($resolved)
                        }
                    }
                    catch {
                        Add-DtmCollectWarning -Message "Failed to resolve Steam library '$path': $($_.Exception.Message)"
                    }
                }
            }
        }
    }
    catch {
        Add-DtmCollectWarning -Message "Failed to read Steam library roots from '$SteamRoot': $($_.Exception.Message)"
    }

    return @($roots)
}

function Copy-SteamLaunchEvidence {
    param(
        [string] $GameDir,
        [string] $Destination
    )

    $info = New-Object System.Collections.Generic.List[string]
    $gameFullPath = ''
    if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
        try {
            $gameFullPath = [System.IO.Path]::GetFullPath($GameDir).TrimEnd('\')
        }
        catch {
            Add-DtmCollectWarning -Message "Failed to normalize Doloc Town game folder for Steam evidence: $($_.Exception.Message)"
        }
    }
    else {
        $info.Add('GameDirUnavailable=True')
        $info.Add('Mode=Steam log tails only')
    }

    foreach ($steamRoot in Get-SteamRootCandidates) {
        $libraryRoots = @()
        try {
            $libraryRoots = @(Get-SteamLibraryRoots -SteamRoot $steamRoot)
        }
        catch {
            Add-DtmCollectWarning -Message "Failed to enumerate Steam libraries for '$steamRoot': $($_.Exception.Message)"
            $libraryRoots = @()
        }

        foreach ($libraryRoot in $libraryRoots) {
            $manifest = Join-Path $libraryRoot 'steamapps\appmanifest_2285550.acf'
            $candidate = Join-Path $libraryRoot 'steamapps\common\Doloc Town'
            if (-not [string]::IsNullOrWhiteSpace($gameFullPath) -and (Test-Path -LiteralPath $manifest) -and (Test-Path -LiteralPath $candidate)) {
                $candidateFullPath = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $candidate).Path).TrimEnd('\')
                if ($candidateFullPath.Equals($gameFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $info.Add("SteamRoot=$steamRoot")
                    $info.Add("LibraryRoot=$libraryRoot")
                    $info.Add("AppManifest=$manifest")
                    Copy-DtmEvidenceFileIfExists -Path $manifest -Destination (Join-Path $Destination 'steam-appmanifest-2285550.acf')
                    break
                }
            }
        }

        $steamLogDir = Join-Path $steamRoot 'logs'
        if (Test-Path -LiteralPath $steamLogDir) {
            foreach ($logName in @('console_log.txt', 'bootstrap_log.txt', 'content_log.txt', 'workshop_log.txt')) {
                $logPath = Join-Path $steamLogDir $logName
                if (Test-Path -LiteralPath $logPath) {
                    try {
                        Get-Content -Tail 500 -LiteralPath $logPath -ErrorAction SilentlyContinue | Set-Content -LiteralPath (Join-Path $Destination ("steam-$logName.tail.txt"))
                    }
                    catch {
                        Add-DtmCollectWarning -Message "Failed to copy Steam log tail '$logPath': $($_.Exception.Message)"
                    }
                }
            }
        }
    }

    if ($info.Count -gt 0) {
        try {
            $info | Set-Content -LiteralPath (Join-Path $Destination 'steam-info.txt')
        }
        catch {
            Add-DtmCollectWarning -Message "Failed to write steam-info.txt: $($_.Exception.Message)"
        }
    }
}

$paths = @()
if (-not [string]::IsNullOrWhiteSpace($dtmapiDir)) {
    if (-not $DesktopTimestampOutput) {
        $paths += @{ Path = Join-Path $dtmapiDir 'logs\latest.log'; Name = 'DTMAPI-latest.log'; MaxBytes = $TextLogMaxBytes }
    }
    $paths += @{ Path = Join-Path $dtmapiDir 'reports\latest-report.txt'; Name = 'latest-report.txt' }
    $paths += @{ Path = Join-Path $dtmapiDir 'debug-console-last-give.txt'; Name = 'debug-console-last-give.txt' }
}

if (-not [string]::IsNullOrWhiteSpace($gameDir)) {
    $paths += @{ Path = Join-Path $gameDir 'BepInEx\LogOutput.log'; Name = 'BepInEx-LogOutput.log'; MaxBytes = $TextLogMaxBytes }
}

foreach ($item in $paths) {
    if (Test-Path -LiteralPath $item.Path) {
        if ($DesktopTimestampOutput) {
            [void](Copy-DtmVerifiedFullEvidenceFileIfExists -Path $item.Path -Destination (Join-Path $evidence $item.Name))
        }
        elseif ($item.ContainsKey('MaxBytes')) {
            Copy-DtmBoundedEvidenceFileIfExists -Path $item.Path -Destination (Join-Path $evidence $item.Name) -MaxBytes ([long]$item.MaxBytes)
        }
        else {
            Copy-DtmEvidenceFileIfExists -Path $item.Path -Destination (Join-Path $evidence $item.Name)
        }
    }
}

$unityLogRoots = @(
    (Join-Path $env:USERPROFILE 'AppData\LocalLow\RedSawGames\DolocTown'),
    (Join-Path $env:USERPROFILE 'AppData\LocalLow\RedSawGames\Doloc Town')
)
Copy-DtmNewestEvidenceFileIfExists -Paths @($unityLogRoots | ForEach-Object { Join-Path $_ 'Player.log' }) -Destination (Join-Path $evidence 'Unity-Player.log') -MaxBytes $TextLogMaxBytes -VerifyFullCopy:$DesktopTimestampOutput
Copy-DtmNewestEvidenceFileIfExists -Paths @($unityLogRoots | ForEach-Object { Join-Path $_ 'Player-prev.log' }) -Destination (Join-Path $evidence 'Unity-Player-prev.log') -MaxBytes $TextLogMaxBytes -VerifyFullCopy:$DesktopTimestampOutput

if (-not [string]::IsNullOrWhiteSpace($dtmapiDir)) {
    $stateEvidenceDir = Join-Path $evidence 'DTMAPI-state'
    Copy-DtmEvidenceFileIfExists -Path (Join-Path $dtmapiDir 'install-state.json') -Destination (Join-Path $stateEvidenceDir 'install-state.json')
    Copy-DtmEvidenceFileIfExists -Path (Join-Path $dtmapiDir 'release-manifest.json') -Destination (Join-Path $stateEvidenceDir 'release-manifest.json')
    $optionalComponentSummaryPath = Join-Path $stateEvidenceDir 'optional-components.txt'
    try {
        $optionalComponentLines = New-Object 'System.Collections.Generic.List[string]'
        $optionalComponentLines.Add('DTMAPI optional framework components (metadata only; DLL bytes are not copied)') | Out-Null
        $installedStatePath = Join-Path $dtmapiDir 'install-state.json'
        if (Test-Path -LiteralPath $installedStatePath -PathType Leaf) {
            $installedState = Get-Content -Raw -Encoding UTF8 -LiteralPath $installedStatePath | ConvertFrom-Json
            $componentRows = @((Get-DtmApiObjectProperty -Object $installedState -Name 'OptionalComponents' -Default @()))
            $optionalComponentLines.Add(('ReceiptCount={0}' -f $componentRows.Count)) | Out-Null
            foreach ($component in $componentRows) {
                $componentPath = [string](Get-DtmApiObjectProperty -Object $component -Name 'Path' -Default '')
                $actualState = 'missing'
                if (-not [string]::IsNullOrWhiteSpace($componentPath) -and (Test-Path -LiteralPath $componentPath -PathType Leaf)) {
                    $item = Get-Item -LiteralPath $componentPath -ErrorAction Stop
                    $hash = (Get-DtmApiFileSha256 -Path $componentPath).ToLowerInvariant()
                    $actualState = 'present;actualLength=' + [string]$item.Length + ';actualSha256=' + $hash
                }
                $optionalComponentLines.Add(('ComponentId={0};distribution={1};loadPolicy={2};relativePath={3};path={4};expectedLength={5};expectedSha256={6};state={7}' -f
                    [string](Get-DtmApiObjectProperty -Object $component -Name 'ComponentId' -Default ''),
                    [string](Get-DtmApiObjectProperty -Object $component -Name 'Distribution' -Default ''),
                    [string](Get-DtmApiObjectProperty -Object $component -Name 'LoadPolicy' -Default ''),
                    [string](Get-DtmApiObjectProperty -Object $component -Name 'RelativePath' -Default ''),
                    $componentPath,
                    [string](Get-DtmApiObjectProperty -Object $component -Name 'Length' -Default ''),
                    [string](Get-DtmApiObjectProperty -Object $component -Name 'Sha256' -Default ''),
                    $actualState)) | Out-Null
            }
        }
        else {
            $optionalComponentLines.Add('ReceiptCount=unavailable') | Out-Null
        }
        New-Item -ItemType Directory -Force -Path $stateEvidenceDir | Out-Null
        $optionalComponentLines | Set-Content -LiteralPath $optionalComponentSummaryPath -Encoding UTF8
    }
    catch {
        Add-DtmCollectWarning -Message "Failed to summarize optional framework components: $($_.Exception.Message)"
    }
    Copy-DtmRecentEvidenceFiles -Directory $dtmapiDir -Filter 'install-state.failed-*.json' -DestinationDirectory $stateEvidenceDir -Count 5
    Copy-DtmRecentEvidenceFiles -Directory $dtmapiDir -Filter 'uninstall-state-*.json' -DestinationDirectory $stateEvidenceDir -Count 5
    Write-DtmRecentReportSummary -Directory (Join-Path $dtmapiDir 'reports') -Destination (Join-Path $evidence 'DTMAPI-reports-skipped.txt')
}

$dtmapiLogDir = if (-not [string]::IsNullOrWhiteSpace($dtmapiDir)) { Join-Path $dtmapiDir 'logs' } else { '' }
if (-not [string]::IsNullOrWhiteSpace($dtmapiLogDir) -and (Test-Path -LiteralPath $dtmapiLogDir)) {
    if ($DesktopTimestampOutput) {
        $recentLogs = @(Get-ChildItem -LiteralPath $dtmapiLogDir -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq 'latest.log' -or $_.Name -like 'latest-*.log' } |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 10)
        foreach ($log in $recentLogs) {
            if ($log.Name -eq 'latest.log') {
                $logDestination = Join-Path $evidence 'DTMAPI-latest.log'
            }
            else {
                $logDestination = Join-Path (Join-Path $evidence 'DTMAPI-log-history') $log.Name
            }
            [void](Copy-DtmVerifiedFullEvidenceFileIfExists -Path $log.FullName -Destination $logDestination)
        }
    }
    else {
        $historyLogs = @(Get-ChildItem -LiteralPath $dtmapiLogDir -Filter 'latest-*.log' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First $HistoryLogCount)
        if ($historyLogs.Count -gt 0) {
            $historyDir = Join-Path $evidence 'DTMAPI-log-history'
            New-Item -ItemType Directory -Force -Path $historyDir | Out-Null
            foreach ($log in $historyLogs) {
                Copy-DtmBoundedEvidenceFileIfExists -Path $log.FullName -Destination (Join-Path $historyDir $log.Name) -MaxBytes $HistoryLogMaxBytes
            }
        }
    }
}

try {
    Copy-DtmUnityCrashReports -Destination $evidence
}
catch {
    Add-DtmCollectWarning -Message "Unity crash report collection failed: $($_.Exception.GetType().Name): $($_.Exception.Message)"
}

$gameEvidenceRoot = if (-not [string]::IsNullOrWhiteSpace($dtmapiDir)) { Join-Path $dtmapiDir 'evidence' } else { '' }
if (-not [string]::IsNullOrWhiteSpace($gameEvidenceRoot) -and (Test-Path -LiteralPath $gameEvidenceRoot)) {
    if ($runtimeEvidenceWindowRequested) {
        $runtimeEvidenceDest = Join-Path $evidence 'DTMAPI-evidence'
        $runtimeEvidenceSelectionSummary = Join-Path $evidence 'DTMAPI-evidence-selection.txt'
        $runtimeEvidenceResult = Copy-DtmRuntimeEvidenceWindow `
            -SourceRoot $gameEvidenceRoot `
            -DestinationRoot $runtimeEvidenceDest `
            -SinceUtc $RuntimeEvidenceSinceUtc `
            -SummaryPath $runtimeEvidenceSelectionSummary `
            -MaxTotalBytes $RuntimeEvidenceMaxTotalBytes `
            -MaxFileBytes $RuntimeEvidenceMaxFileBytes `
            -MaxFiles $RuntimeEvidenceMaxFiles `
            -MaxDirectories $RuntimeEvidenceMaxDirectories
        if ($runtimeEvidenceResult.Status -eq 'blocked-by-limit') {
            Add-DtmCollectWarning -Message "Current-run runtime evidence exceeded collection limits and was not copied. See $runtimeEvidenceSelectionSummary"
        }
    }
    elseif ($IncludeRuntimeEvidence) {
        $runtimeEvidenceDest = Join-Path $evidence 'DTMAPI-evidence'
        if (Test-DtmApiPathIsSameOrChild -Child $runtimeEvidenceDest -Parent $gameEvidenceRoot) {
            Add-DtmCollectWarning -Message "Skipped runtime evidence copy because destination is inside the runtime evidence source. Source: $gameEvidenceRoot. Destination: $runtimeEvidenceDest"
        }
        else {
            try {
                Copy-Item -Recurse -Force -LiteralPath $gameEvidenceRoot -Destination $runtimeEvidenceDest
            }
            catch {
                Add-DtmCollectWarning -Message "Failed to copy runtime evidence tree '$gameEvidenceRoot': $($_.Exception.Message)"
            }
        }
    }
    else {
        $summary = New-Object System.Collections.Generic.List[string]
        $summary.Add("RuntimeEvidenceRoot=$gameEvidenceRoot")
        $summary.Add('Skipped=True')
        $summary.Add('Reason=collect-logs.ps1 does not copy runtime evidence by default. Use -RuntimeEvidenceSinceUtc for a bounded current-run window, or -IncludeRuntimeEvidence only for an explicit full-tree payload.')
        $summary.Add('')
        $summary.Add('Recent runtime evidence folders:')
        $recentRuntimeEvidence = @(Get-ChildItem -LiteralPath $gameEvidenceRoot -Directory -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 20)
        foreach ($item in $recentRuntimeEvidence) {
            $summary.Add(("{0}`t{1:o}`t{2}" -f $item.Name, $item.LastWriteTime, $item.FullName))
        }

        if ($recentRuntimeEvidence.Count -eq 0) {
            $summary.Add('(none)')
        }

        $summary | Set-Content -LiteralPath (Join-Path $evidence 'DTMAPI-evidence-skipped.txt')
    }
}

Copy-SteamLaunchEvidence -GameDir $gameDir -Destination $evidence
Write-DtmSupportContext -Destination $evidence -DtmApiDir $dtmapiDir -GameDir $gameDir
Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
$summaryName = if (Test-Path -LiteralPath (Join-Path $evidence 'summary.txt')) { 'collect-summary.txt' } else { 'summary.txt' }
$warningText = if ($script:DtmCollectWarnings.Count -gt 0) { $script:DtmCollectWarnings -join "`n" } else { '(none)' }
"GameDir=$gameDir`nDtmApiStateDir=$dtmapiDir`nCollected=$(Get-Date -Format o)`nWarnings=$($script:DtmCollectWarnings.Count)`nPlayerDoctorStatus=$playerDoctorStatus`nPlayerDoctorExit=$playerDoctorExit`nPlayerDoctorStdOut=$playerDoctorStdOutPath`nPlayerDoctorStdErr=$playerDoctorStdErrPath" | Set-Content -LiteralPath (Join-Path $evidence $summaryName)
if ($script:DtmCollectWarnings.Count -gt 0) {
    $warningText | Set-Content -LiteralPath (Join-Path $evidence 'collect-warnings.txt')
}
try {
    & "$PSScriptRoot\analyze-startup-evidence.ps1" -EvidencePath $evidence -OutputDirectory $evidence -Quiet
}
catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $evidence 'startup-analysis-error.txt')
}
if (-not [string]::IsNullOrWhiteSpace($script:DtmCollectStagingOutput)) {
    if (Test-Path -LiteralPath $script:DtmCollectFinalOutput) {
        throw "Final log collection directory already exists: $($script:DtmCollectFinalOutput)"
    }
    [System.IO.Directory]::Move($script:DtmCollectStagingOutput, $script:DtmCollectFinalOutput)
    $evidence = $script:DtmCollectFinalOutput
    $script:DtmCollectStagingOutput = ''
    $script:DtmCollectFinalOutput = ''
}
Write-Host $evidence
}
catch {
    $collectionFailure = $_
    if (-not [string]::IsNullOrWhiteSpace($script:DtmCollectStagingOutput) -and (Test-Path -LiteralPath $script:DtmCollectStagingOutput)) {
        Remove-Item -Recurse -Force -LiteralPath $script:DtmCollectStagingOutput -ErrorAction SilentlyContinue
    }
    throw $collectionFailure
}
