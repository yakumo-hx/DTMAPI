function Get-SmokeUnityCrashRootCandidates {
    $roots = New-Object 'System.Collections.Generic.List[string]'
    $tempRoot = [System.IO.Path]::GetTempPath()
    foreach ($relative in @(
            'RedSawGames\DolocTown\Crashes',
            'RedSawGames\Doloc Town\Crashes'
        )) {
        $path = [System.IO.Path]::GetFullPath((Join-Path $tempRoot $relative))
        if (-not $roots.Contains($path)) {
            $roots.Add($path) | Out-Null
        }
    }

    return @($roots.ToArray())
}

function Get-SmokeCrashDirectorySortTimeUtc {
    param([Parameter(Mandatory = $true)] [System.IO.DirectoryInfo] $Directory)

    $latest = $Directory.LastWriteTimeUtc
    foreach ($file in @(Get-ChildItem -LiteralPath $Directory.FullName -File -ErrorAction SilentlyContinue)) {
        if ($file.LastWriteTimeUtc -gt $latest) {
            $latest = $file.LastWriteTimeUtc
        }
    }

    return $latest
}

function Write-SmokeCrashBaseline {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt
    )

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.Add("runStartedAt=$($RunStartedAt.ToString('o'))") | Out-Null
    $latestCrashId = ''
    $latestCrashTime = [datetime]::MinValue
    foreach ($root in @(Get-SmokeUnityCrashRootCandidates)) {
        $exists = Test-Path -LiteralPath $root -PathType Container
        $lines.Add("CrashRoot=$root") | Out-Null
        $lines.Add("Exists=$exists") | Out-Null
        if (-not $exists) {
            continue
        }

        $dirs = @(Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
            Sort-Object @{ Expression = { Get-SmokeCrashDirectorySortTimeUtc -Directory $_ }; Descending = $true })
        $lines.Add("DirectoryCount=$($dirs.Count)") | Out-Null
        foreach ($dir in $dirs) {
            $sortTime = Get-SmokeCrashDirectorySortTimeUtc -Directory $dir
            if ($sortTime -gt $latestCrashTime) {
                $latestCrashTime = $sortTime
                $latestCrashId = $dir.Name
            }

            $files = @(Get-ChildItem -LiteralPath $dir.FullName -File -ErrorAction SilentlyContinue |
                ForEach-Object { "$($_.Name):$($_.Length):$($_.LastWriteTime.ToString('o'))" })
            $lines.Add("Directory=$($dir.FullName)") | Out-Null
            $lines.Add("DirectoryLastWrite=$($dir.LastWriteTime.ToString('o'))") | Out-Null
            $lines.Add("DirectorySortTime=$($sortTime.ToString('o'))") | Out-Null
            $lines.Add("Files=$([string]::Join('|', $files))") | Out-Null
        }
    }

    if ($latestCrashTime -ne [datetime]::MinValue) {
        $lines.Add("latestPreexistingCrashId=$latestCrashId") | Out-Null
        $lines.Add("latestPreexistingCrashSortTime=$($latestCrashTime.ToString('o'))") | Out-Null
    }
    else {
        $lines.Add('latestPreexistingCrashId=') | Out-Null
        $lines.Add('latestPreexistingCrashSortTime=') | Out-Null
    }

    $lines | Set-Content -LiteralPath $Path
}

function Get-SmokeUnityCrashFreshness {
    param(
        [Parameter(Mandatory = $true)] [string] $SummaryPath,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt,
        [object] $FatalDetectedAt = $null,
        [string] $Phase = 'unknown'
    )

    $result = [ordered]@{
        Phase = $Phase
        Status = 'missing'
        FreshDirectories = @()
        StaleDirectories = @()
        CopiedFiles = 0
        RunStartedAt = $RunStartedAt.ToString('o')
        FatalDetectedAt = if ($null -ne $FatalDetectedAt) { ([datetime]$FatalDetectedAt).ToString('o') } else { '' }
        Reason = ''
    }

    if (-not (Test-Path -LiteralPath $SummaryPath -PathType Leaf)) {
        $result.Reason = 'Unity-Crashes/summary.txt missing.'
        return $result
    }

    $lines = @(Get-Content -LiteralPath $SummaryPath -ErrorAction SilentlyContinue)
    $currentDirectory = ''
    $copiedFiles = @()
    foreach ($line in $lines) {
        if ($line -like 'Directory=*') {
            $currentDirectory = $line.Substring('Directory='.Length)
            continue
        }

        if ($line -like 'DirectoryLastWrite=*') {
            if ([string]::IsNullOrWhiteSpace($currentDirectory)) {
                continue
            }

            $rawTime = $line.Substring('DirectoryLastWrite='.Length)
            $parsedTime = [datetime]::MinValue
            if ([datetime]::TryParse($rawTime, [ref]$parsedTime)) {
                if ($parsedTime.ToUniversalTime() -ge $RunStartedAt.ToUniversalTime().AddSeconds(-5)) {
                    $result.FreshDirectories += @($currentDirectory)
                }
                else {
                    $result.StaleDirectories += @($currentDirectory)
                }
            }
            else {
                $result.StaleDirectories += @("$currentDirectory (unparsed-time=$rawTime)")
            }
            continue
        }

        if ($line -like 'CopiedFile=*') {
            $copiedFiles += @($line.Substring('CopiedFile='.Length))
        }
    }

    $result.CopiedFiles = $copiedFiles.Count
    if (@($result.FreshDirectories).Count -gt 0) {
        $result.Status = 'fresh'
        $result.Reason = 'At least one copied crash directory was created or modified after this smoke run started.'
    }
    elseif ($copiedFiles.Count -gt 0 -or @($result.StaleDirectories).Count -gt 0) {
        $result.Status = 'stale-only'
        $result.Reason = 'Copied crash evidence only came from directories older than this smoke run.'
    }
    else {
        $result.Status = 'missing'
        $result.Reason = 'No copied crash files or directories could be attributed to this run.'
    }

    return $result
}

function Write-SmokeCrashFreshness {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Freshness
    )

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.Add("Phase=$($Freshness.Phase)") | Out-Null
    $lines.Add("Status=$($Freshness.Status)") | Out-Null
    $lines.Add("RunStartedAt=$($Freshness.RunStartedAt)") | Out-Null
    $lines.Add("FatalDetectedAt=$($Freshness.FatalDetectedAt)") | Out-Null
    $lines.Add("CopiedFiles=$($Freshness.CopiedFiles)") | Out-Null
    $lines.Add("FreshDirectories=$([string]::Join('|', @($Freshness.FreshDirectories)))") | Out-Null
    $lines.Add("StaleDirectories=$([string]::Join('|', @($Freshness.StaleDirectories)))") | Out-Null
    $lines.Add("Reason=$($Freshness.Reason)") | Out-Null
    $lines | Set-Content -LiteralPath $Path
}

function Invoke-DtmApiDumpTempScavenge {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return
    }

    $now = [DateTime]::UtcNow
    $totalBytes = 0L
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -ErrorAction SilentlyContinue)) {
        $totalBytes += [long]$file.Length
    }
    if ($totalBytes -ge 6GB) {
        Write-Warning "Managed DTMAPI dump temp is at or above 6 GiB: $Root ($totalBytes bytes)."
    }

    $receiptArchive = Join-Path $Root '_receipts'
    foreach ($directory in @(Get-ChildItem -LiteralPath $Root -Directory -ErrorAction SilentlyContinue)) {
        if ($directory.Name -eq '_receipts' -or ($directory.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            continue
        }

        $receiptPath = Join-Path $directory.FullName 'dump-session.json'
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
            Write-Warning "Unmanaged entry exists under DTMAPI dump temp and was not removed: $($directory.FullName)"
            continue
        }

        try {
            $receipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
            if ($receipt.owner -ne 'DTMAPI.DumpCapture' -or [int]$receipt.schemaVersion -ne 1) {
                Write-Warning "Unrecognized dump receipt; directory was not removed: $($directory.FullName)"
                continue
            }
        }
        catch {
            Write-Warning "Unreadable dump receipt; directory was not removed: $($directory.FullName)"
            continue
        }

        $leasePath = Join-Path $directory.FullName 'active.lock'
        $leaseProbe = $null
        try {
            if (Test-Path -LiteralPath $leasePath -PathType Leaf) {
                $leaseProbe = [System.IO.File]::Open($leasePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            }
        }
        catch {
            continue
        }
        finally {
            if ($leaseProbe) {
                $leaseProbe.Dispose()
            }
        }

        $age = $now - $directory.LastWriteTimeUtc
        $completed = [string]$receipt.status -like 'completed-*' -or [string]$receipt.status -eq 'cleanup-pending'
        if ($completed -or $age.TotalHours -ge 24) {
            New-Item -ItemType Directory -Force -Path $receiptArchive | Out-Null
            Copy-Item -Force -LiteralPath $receiptPath -Destination (Join-Path $receiptArchive ($directory.Name + '.json'))
            Remove-Item -LiteralPath $directory.FullName -Recurse -Force
            Write-Host "Recovered expired DTMAPI dump temp session: $($directory.FullName)"
        }
        elseif ($age.TotalHours -ge 2) {
            Write-Warning "DTMAPI dump temp session is older than two hours: $($directory.FullName)"
        }
    }

    if (Test-Path -LiteralPath $receiptArchive -PathType Container) {
        foreach ($receiptFile in @(Get-ChildItem -LiteralPath $receiptArchive -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
            if (($now - $receiptFile.LastWriteTimeUtc).TotalDays -ge 7) {
                Remove-Item -LiteralPath $receiptFile.FullName -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

function Invoke-SmokeFatalProcessDump {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Mode,
        [System.Diagnostics.Process] $Process
    )

    $dumpDir = Join-Path $EvidencePath 'Process-Dumps'
    New-Item -ItemType Directory -Force -Path $dumpDir | Out-Null
    $summaryPath = Join-Path $dumpDir 'process-dump-summary.txt'
    $errorPath = Join-Path $dumpDir 'process-dump-error.txt'
    $commandPath = Join-Path $dumpDir 'process-dump-command.txt'
    $stdoutPath = Join-Path $dumpDir 'process-dump-stdout.txt'
    $stderrPath = Join-Path $dumpDir 'process-dump-stderr.txt'
    $missingPath = Join-Path $dumpDir 'MISSING-PROCESS-DUMP-README.txt'
    $summary = New-Object 'System.Collections.Generic.List[string]'
    $summary.Add("Mode=$Mode") | Out-Null
    $summary.Add("StartedAt=$(Get-Date -Format o)") | Out-Null
    $tempDumpRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'DTMAPI-Dumps'
    Invoke-DtmApiDumpTempScavenge -Root $tempDumpRoot
    '' | Set-Content -LiteralPath $commandPath
    '' | Set-Content -LiteralPath $stdoutPath
    '' | Set-Content -LiteralPath $stderrPath
    if (Test-Path -LiteralPath $errorPath) {
        Remove-Item -LiteralPath $errorPath -Force
    }
    if (Test-Path -LiteralPath $missingPath) {
        Remove-Item -LiteralPath $missingPath -Force
    }

    if ($Mode -eq 'None') {
        $summary.Add('Status=Skipped') | Out-Null
        $summary.Add('Reason=FatalWindowProcessDumpMode=None') | Out-Null
        $summary | Set-Content -LiteralPath $summaryPath
        return 'Skipped'
    }

    if ($null -eq $Process) {
        $summary.Add('Status=MissingProcess') | Out-Null
        $summary.Add('Reason=No DolocTown process was available for live dump capture.') | Out-Null
        $summary | Set-Content -LiteralPath $summaryPath
        'No DolocTown process was available for live dump capture.' | Set-Content -LiteralPath $missingPath
        return 'MissingProcess'
    }

    $targetPid = $Process.Id
    $summary.Add("ProcessId=$targetPid") | Out-Null
    $summary.Add("MainWindowTitle=$($Process.MainWindowTitle)") | Out-Null
    $tempDumpRunId = "$(Get-Date -Format 'yyyyMMdd-HHmmss')-$targetPid-$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
    $tempDumpDir = Join-Path $tempDumpRoot $tempDumpRunId
    New-Item -ItemType Directory -Force -Path $tempDumpDir | Out-Null
    $tempDumpLeasePath = Join-Path $tempDumpDir 'active.lock'
    $tempDumpLease = [System.IO.File]::Open($tempDumpLeasePath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    $tempDumpReceiptPath = Join-Path $tempDumpDir 'dump-session.json'
    [ordered]@{
        owner = 'DTMAPI.DumpCapture'
        schemaVersion = 1
        runId = $tempDumpRunId
        processId = $targetPid
        startedAtUtc = [DateTime]::UtcNow.ToString('o')
        status = 'active'
        sourceRoot = $tempDumpDir
    } | ConvertTo-Json | Set-Content -LiteralPath $tempDumpReceiptPath -Encoding UTF8
    $summary.Add("TempDumpDir=$tempDumpDir") | Out-Null

    function Complete-DumpTempSession {
        param(
            [Parameter(Mandatory = $true)] [string] $Status,
            [Parameter(Mandatory = $true)] [bool] $RetainWhenFilesRemain
        )

        $remainingDumps = @(Get-ChildItem -LiteralPath $tempDumpDir -File -Filter '*.dmp' -ErrorAction SilentlyContinue)
        $retain = $RetainWhenFilesRemain -and $remainingDumps.Count -gt 0
        $remainingDumpBytes = if ($remainingDumps.Count -gt 0) { [long](($remainingDumps | Measure-Object -Property Length -Sum).Sum) } else { 0L }
        [ordered]@{
            owner = 'DTMAPI.DumpCapture'
            schemaVersion = 1
            runId = $tempDumpRunId
            processId = $targetPid
            startedAtUtc = $summary[1].Substring('StartedAt='.Length)
            completedAtUtc = [DateTime]::UtcNow.ToString('o')
            status = $(if ($retain) { 'handoff-failed-retained' } else { $Status })
            retainedUntilUtc = $(if ($retain) { [DateTime]::UtcNow.AddHours(24).ToString('o') } else { $null })
            sourceRoot = $tempDumpDir
            remainingDumpCount = $remainingDumps.Count
            remainingDumpBytes = $remainingDumpBytes
        } | ConvertTo-Json | Set-Content -LiteralPath $tempDumpReceiptPath -Encoding UTF8

        if ($tempDumpLease) {
            $tempDumpLease.Dispose()
            $tempDumpLease = $null
        }
        if (-not $retain) {
            Remove-Item -LiteralPath $tempDumpDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $tempDumpDir -PathType Container) {
            Write-Warning "DTMAPI dump temp cleanup remains pending: $tempDumpDir"
        }
    }

    function Add-DumpText {
        param(
            [Parameter(Mandatory = $true)] [string] $Path,
            [AllowNull()] [string] $Text
        )

        if ($null -eq $Text) {
            '' | Add-Content -LiteralPath $Path
            return
        }

        $Text | Add-Content -LiteralPath $Path
    }

    function Copy-DumpToEvidence {
        param(
            [Parameter(Mandatory = $true)] [string] $SourcePath,
            [Parameter(Mandatory = $true)] [string] $DestinationPath
        )

        return Copy-DtmApiVerifiedFile -SourcePath $SourcePath -DestinationPath $DestinationPath
    }

    function Write-TooLargeDumpReadme {
        param(
            [Parameter(Mandatory = $true)] $DumpItem,
            [Parameter(Mandatory = $true)] [string] $Mode,
            [Parameter(Mandatory = $true)] [string] $DumpSha256
        )

        $readmePath = Join-Path $dumpDir 'TOO-LARGE-DUMP-README.txt'
        $hash = $DumpSha256
        @(
            'A full live process dump was captured but intentionally omitted from routine evidence packages.',
            "CapturedMode=$Mode",
            "DumpPath=$($DumpItem.FullName)",
            "DumpSize=$($DumpItem.Length)",
            "DumpSha256=$hash",
            "GeneratedAt=$(Get-Date -Format o)"
        ) | Set-Content -LiteralPath $readmePath
        $summary.Add("DumpSha256=$hash") | Out-Null
        $summary.Add("TooLargeDumpReadme=$readmePath") | Out-Null
    }

    function Invoke-ComSvcsDumpAttempt {
        $attempt = [ordered]@{
            Mode = 'ComSvcsFull'
            Status = 'MissingDump'
            DumpPath = $null
            TempDumpPath = $null
            DumpSize = 0
            DumpSha256 = $null
            HandoffVerified = $false
            ExitCode = $null
            Error = $null
        }

        $rundll32Path = Join-Path $env:WINDIR 'System32\rundll32.exe'
        $comsvcsPath = Join-Path $env:WINDIR 'System32\comsvcs.dll'
        $tempDumpPath = Join-Path $tempDumpDir ("DolocTown-$targetPid-fatal-live-comsvcs.dmp")
        $finalDumpPath = Join-Path $dumpDir ("DolocTown-$targetPid-fatal-live-comsvcs.dmp")
        $modeStdoutPath = Join-Path $dumpDir 'process-dump-comsvcs-stdout.txt'
        $modeStderrPath = Join-Path $dumpDir 'process-dump-comsvcs-stderr.txt'
        $attempt.TempDumpPath = $tempDumpPath
        $attempt.DumpPath = $finalDumpPath

        try {
            if (-not (Test-Path -LiteralPath $rundll32Path -PathType Leaf)) {
                throw "rundll32.exe was not found at '$rundll32Path'."
            }

            if (-not (Test-Path -LiteralPath $comsvcsPath -PathType Leaf)) {
                throw "comsvcs.dll was not found at '$comsvcsPath'."
            }

            Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $finalDumpPath -Force -ErrorAction SilentlyContinue
            $argumentParts = @("$comsvcsPath,", 'MiniDump', [string]$targetPid, $tempDumpPath, 'full')
            $commandLine = "`"$rundll32Path`" $([string]::Join(' ', $argumentParts))"
            Add-DumpText -Path $commandPath -Text "[ComSvcsFull] $commandLine"
            $startedAt = Get-Date
            $dumpProcess = Start-Process -FilePath $rundll32Path -ArgumentList $argumentParts -Wait -PassThru -NoNewWindow -RedirectStandardOutput $modeStdoutPath -RedirectStandardError $modeStderrPath
            $finishedAt = Get-Date
            $attempt.ExitCode = $dumpProcess.ExitCode
            Add-DumpText -Path $stdoutPath -Text "[ComSvcsFull stdout]"
            if (Test-Path -LiteralPath $modeStdoutPath) {
                Add-DumpText -Path $stdoutPath -Text (Get-Content -Raw -LiteralPath $modeStdoutPath -ErrorAction SilentlyContinue)
            }
            Add-DumpText -Path $stderrPath -Text "[ComSvcsFull stderr]"
            if (Test-Path -LiteralPath $modeStderrPath) {
                Add-DumpText -Path $stderrPath -Text (Get-Content -Raw -LiteralPath $modeStderrPath -ErrorAction SilentlyContinue)
            }

            $copiedDump = Copy-DumpToEvidence -SourcePath $tempDumpPath -DestinationPath $finalDumpPath
            if ($null -ne $copiedDump) {
                $attempt.Status = 'Captured'
                $attempt.DumpSize = $copiedDump.Length
                $attempt.DumpSha256 = $copiedDump.Sha256
                $attempt.HandoffVerified = [bool]$copiedDump.Verified
            }
            else {
                $attempt.Status = 'MissingDump'
            }

            $summary.Add("ComSvcsFull.StartedAt=$($startedAt.ToString('o'))") | Out-Null
            $summary.Add("ComSvcsFull.FinishedAt=$($finishedAt.ToString('o'))") | Out-Null
            $summary.Add("ComSvcsFull.ExitCode=$($attempt.ExitCode)") | Out-Null
            $summary.Add("ComSvcsFull.TempDumpPath=$tempDumpPath") | Out-Null
            $summary.Add("ComSvcsFull.DumpPath=$finalDumpPath") | Out-Null
            $summary.Add("ComSvcsFull.Status=$($attempt.Status)") | Out-Null
            $summary.Add("ComSvcsFull.DumpSize=$($attempt.DumpSize)") | Out-Null
            $summary.Add("ComSvcsFull.DumpSha256=$($attempt.DumpSha256)") | Out-Null
            $summary.Add("ComSvcsFull.HandoffVerified=$($attempt.HandoffVerified)") | Out-Null
            return [pscustomobject]$attempt
        }
        catch {
            $attempt.Status = 'Error'
            $attempt.Error = "$($_.Exception.GetType().Name): $($_.Exception.Message)"
            $summary.Add("ComSvcsFull.Status=Error") | Out-Null
            $summary.Add("ComSvcsFull.Error=$($attempt.Error)") | Out-Null
            Add-DumpText -Path $errorPath -Text "[ComSvcsFull] $($attempt.Error)"
            return [pscustomobject]$attempt
        }
        finally {
            if ($attempt.HandoffVerified -and (Test-Path -LiteralPath $tempDumpPath -PathType Leaf)) {
                try {
                    Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction Stop
                    $summary.Add("ComSvcsFull.TempCleanup=DeletedAfterVerifiedHandoff") | Out-Null
                }
                catch {
                    $summary.Add("ComSvcsFull.TempCleanup=CleanupPending") | Out-Null
                    Add-DumpText -Path $errorPath -Text "[ComSvcsFull cleanup] $($_.Exception.GetType().Name): $($_.Exception.Message)"
                }
            }
        }
    }

    function Invoke-DbgHelpDumpAttempt {
        $attempt = [ordered]@{
            Mode = 'DbgHelpFull'
            Status = 'MissingDump'
            DumpPath = $null
            TempDumpPath = $null
            DumpSize = 0
            DumpSha256 = $null
            HandoffVerified = $false
            ExitCode = $null
            Error = $null
        }

        $tempDumpPath = Join-Path $tempDumpDir ("DolocTown-$targetPid-fatal-live-dbghelp.dmp")
        $finalDumpPath = Join-Path $dumpDir ("DolocTown-$targetPid-fatal-live-dbghelp.dmp")
        $attempt.TempDumpPath = $tempDumpPath
        $attempt.DumpPath = $finalDumpPath
        Add-DumpText -Path $commandPath -Text "[DbgHelpFull] MiniDumpWriteDump(pid=$targetPid, dumpType=MiniDumpWithFullMemory, tempPath=$tempDumpPath)"

        try {
            if (-not ([System.Management.Automation.PSTypeName]'SmokeMiniDumpWriter').Type) {
                Add-Type @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class SmokeMiniDumpWriter
{
    [DllImport("Dbghelp.dll", SetLastError = true)]
    public static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        int processId,
        SafeFileHandle hFile,
        int dumpType,
        IntPtr exceptionParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);

    public static int GetLastError()
    {
        return Marshal.GetLastWin32Error();
    }
}
'@
            }

            Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $finalDumpPath -Force -ErrorAction SilentlyContinue
            $startedAt = Get-Date
            $fileStream = [System.IO.File]::Open($tempDumpPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $miniDumpWithFullMemory = 2
                $ok = [SmokeMiniDumpWriter]::MiniDumpWriteDump(
                    $Process.Handle,
                    $targetPid,
                    $fileStream.SafeFileHandle,
                    $miniDumpWithFullMemory,
                    [IntPtr]::Zero,
                    [IntPtr]::Zero,
                    [IntPtr]::Zero)
                $lastError = [SmokeMiniDumpWriter]::GetLastError()
            }
            finally {
                $fileStream.Dispose()
            }
            $finishedAt = Get-Date

            if (-not $ok) {
                $attempt.Status = 'Error'
                $attempt.Error = "MiniDumpWriteDump returned false. LastWin32Error=$lastError"
                Add-DumpText -Path $errorPath -Text "[DbgHelpFull] $($attempt.Error)"
            }

            $copiedDump = Copy-DumpToEvidence -SourcePath $tempDumpPath -DestinationPath $finalDumpPath
            if ($null -ne $copiedDump) {
                $attempt.Status = 'Captured'
                $attempt.DumpSize = $copiedDump.Length
                $attempt.DumpSha256 = $copiedDump.Sha256
                $attempt.HandoffVerified = [bool]$copiedDump.Verified
            }
            elseif ($attempt.Status -ne 'Error') {
                $attempt.Status = 'MissingDump'
            }

            $summary.Add("DbgHelpFull.StartedAt=$($startedAt.ToString('o'))") | Out-Null
            $summary.Add("DbgHelpFull.FinishedAt=$($finishedAt.ToString('o'))") | Out-Null
            $summary.Add("DbgHelpFull.LastWin32Error=$lastError") | Out-Null
            $summary.Add("DbgHelpFull.TempDumpPath=$tempDumpPath") | Out-Null
            $summary.Add("DbgHelpFull.DumpPath=$finalDumpPath") | Out-Null
            $summary.Add("DbgHelpFull.Status=$($attempt.Status)") | Out-Null
            $summary.Add("DbgHelpFull.DumpSize=$($attempt.DumpSize)") | Out-Null
            $summary.Add("DbgHelpFull.DumpSha256=$($attempt.DumpSha256)") | Out-Null
            $summary.Add("DbgHelpFull.HandoffVerified=$($attempt.HandoffVerified)") | Out-Null
            return [pscustomobject]$attempt
        }
        catch {
            $attempt.Status = 'Error'
            $attempt.Error = "$($_.Exception.GetType().Name): $($_.Exception.Message)"
            $summary.Add("DbgHelpFull.Status=Error") | Out-Null
            $summary.Add("DbgHelpFull.Error=$($attempt.Error)") | Out-Null
            Add-DumpText -Path $errorPath -Text "[DbgHelpFull] $($attempt.Error)"
            return [pscustomobject]$attempt
        }
        finally {
            if ($attempt.HandoffVerified -and (Test-Path -LiteralPath $tempDumpPath -PathType Leaf)) {
                try {
                    Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction Stop
                    $summary.Add("DbgHelpFull.TempCleanup=DeletedAfterVerifiedHandoff") | Out-Null
                }
                catch {
                    $summary.Add("DbgHelpFull.TempCleanup=CleanupPending") | Out-Null
                    Add-DumpText -Path $errorPath -Text "[DbgHelpFull cleanup] $($_.Exception.GetType().Name): $($_.Exception.Message)"
                }
            }
        }
    }

    $attempts = New-Object 'System.Collections.Generic.List[object]'
    if ($Mode -eq 'ComSvcsFull' -or $Mode -eq 'Both') {
        $attempts.Add((Invoke-ComSvcsDumpAttempt)) | Out-Null
    }
    if ($Mode -eq 'DbgHelpFull' -or ($Mode -eq 'Both' -and -not ($attempts | Where-Object { $_.Status -eq 'Captured' }))) {
        $attempts.Add((Invoke-DbgHelpDumpAttempt)) | Out-Null
    }
    elseif ($Mode -eq 'Both') {
        $summary.Add('DbgHelpFull.Status=Skipped') | Out-Null
        $summary.Add('DbgHelpFull.Reason=ComSvcsFull captured a dump; fallback was not needed.') | Out-Null
    }

    $captured = @($attempts | Where-Object { $_.Status -eq 'Captured' })
    if ($captured.Count -gt 0) {
        $firstCapture = $captured | Select-Object -First 1
        $summary.Add("Status=Captured") | Out-Null
        $summary.Add("CapturedMode=$($firstCapture.Mode)") | Out-Null
        $summary.Add("DumpPath=$($firstCapture.DumpPath)") | Out-Null
        $summary.Add("DumpSize=$($firstCapture.DumpSize)") | Out-Null
        $summary.Add("DumpSha256=$($firstCapture.DumpSha256)") | Out-Null
        $summary.Add("HandoffVerified=$($firstCapture.HandoffVerified)") | Out-Null
        $capturedDumpItem = Get-Item -LiteralPath $firstCapture.DumpPath -ErrorAction SilentlyContinue
        if ($capturedDumpItem) {
            Write-TooLargeDumpReadme -DumpItem $capturedDumpItem -Mode $firstCapture.Mode -DumpSha256 $firstCapture.DumpSha256
        }
        [ordered]@{
            owner = 'DTMAPI.ProcessDumpEvidence'
            schemaVersion = 1
            runId = $tempDumpRunId
            status = 'captured-verified'
            captureMode = $firstCapture.Mode
            dumpPath = $firstCapture.DumpPath
            dumpSize = [long]$firstCapture.DumpSize
            dumpSha256 = $firstCapture.DumpSha256
            handoffVerified = [bool]$firstCapture.HandoffVerified
            analysisStatus = 'not-analyzed'
            duplicatePolicy = 'retain-one-canonical-per-reviewed-signature-or-hash'
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dumpDir 'process-dump-receipt.json') -Encoding UTF8
        $summary | Set-Content -LiteralPath $summaryPath
        Complete-DumpTempSession -Status 'completed-verified' -RetainWhenFilesRemain $false
        return "Captured:$($firstCapture.Mode)"
    }

    $errors = @($attempts | Where-Object { $_.Status -eq 'Error' })
    if ($errors.Count -gt 0) {
        $summary.Add('Status=Error') | Out-Null
        $summary.Add("Reason=No dump was captured and at least one dump attempt failed. See process-dump-error.txt and process-dump-command.txt.") | Out-Null
        $summary | Set-Content -LiteralPath $summaryPath
        "No dump was captured. See process-dump-summary.txt, process-dump-command.txt, and process-dump-error.txt." | Set-Content -LiteralPath $missingPath
        [ordered]@{
            owner = 'DTMAPI.ProcessDumpEvidence'
            schemaVersion = 1
            runId = $tempDumpRunId
            status = 'capture-error'
            captureMode = $Mode
            handoffVerified = $false
            analysisStatus = 'not-applicable'
            createdAtUtc = [DateTime]::UtcNow.ToString('o')
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dumpDir 'process-dump-receipt.json') -Encoding UTF8
        Complete-DumpTempSession -Status 'capture-error' -RetainWhenFilesRemain $true
        return 'Error'
    }

    $summary.Add('Status=MissingDump') | Out-Null
    $summary.Add('DumpSize=0') | Out-Null
    $summary.Add("Reason=No configured dump attempt produced a non-empty dump file.") | Out-Null
    $summary | Set-Content -LiteralPath $summaryPath
    "No configured process dump attempt produced a non-empty dump file. See process-dump-summary.txt and process-dump-command.txt." | Set-Content -LiteralPath $missingPath
    [ordered]@{
        owner = 'DTMAPI.ProcessDumpEvidence'
        schemaVersion = 1
        runId = $tempDumpRunId
        status = 'missing-dump'
        captureMode = $Mode
        handoffVerified = $false
        analysisStatus = 'not-applicable'
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dumpDir 'process-dump-receipt.json') -Encoding UTF8
    Complete-DumpTempSession -Status 'missing-dump' -RetainWhenFilesRemain $true
    return 'MissingDump'
}

function Complete-DumpTempSession {
        param(
            [Parameter(Mandatory = $true)] [string] $Status,
            [Parameter(Mandatory = $true)] [bool] $RetainWhenFilesRemain
        )

        $remainingDumps = @(Get-ChildItem -LiteralPath $tempDumpDir -File -Filter '*.dmp' -ErrorAction SilentlyContinue)
        $retain = $RetainWhenFilesRemain -and $remainingDumps.Count -gt 0
        $remainingDumpBytes = if ($remainingDumps.Count -gt 0) { [long](($remainingDumps | Measure-Object -Property Length -Sum).Sum) } else { 0L }
        [ordered]@{
            owner = 'DTMAPI.DumpCapture'
            schemaVersion = 1
            runId = $tempDumpRunId
            processId = $targetPid
            startedAtUtc = $summary[1].Substring('StartedAt='.Length)
            completedAtUtc = [DateTime]::UtcNow.ToString('o')
            status = $(if ($retain) { 'handoff-failed-retained' } else { $Status })
            retainedUntilUtc = $(if ($retain) { [DateTime]::UtcNow.AddHours(24).ToString('o') } else { $null })
            sourceRoot = $tempDumpDir
            remainingDumpCount = $remainingDumps.Count
            remainingDumpBytes = $remainingDumpBytes
        } | ConvertTo-Json | Set-Content -LiteralPath $tempDumpReceiptPath -Encoding UTF8

        if ($tempDumpLease) {
            $tempDumpLease.Dispose()
            $tempDumpLease = $null
        }
        if (-not $retain) {
            Remove-Item -LiteralPath $tempDumpDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $tempDumpDir -PathType Container) {
            Write-Warning "DTMAPI dump temp cleanup remains pending: $tempDumpDir"
        }
    }

function Add-DumpText {
        param(
            [Parameter(Mandatory = $true)] [string] $Path,
            [AllowNull()] [string] $Text
        )

        if ($null -eq $Text) {
            '' | Add-Content -LiteralPath $Path
            return
        }

        $Text | Add-Content -LiteralPath $Path
    }

function Copy-DumpToEvidence {
        param(
            [Parameter(Mandatory = $true)] [string] $SourcePath,
            [Parameter(Mandatory = $true)] [string] $DestinationPath
        )

        return Copy-DtmApiVerifiedFile -SourcePath $SourcePath -DestinationPath $DestinationPath
    }

function Write-TooLargeDumpReadme {
        param(
            [Parameter(Mandatory = $true)] $DumpItem,
            [Parameter(Mandatory = $true)] [string] $Mode,
            [Parameter(Mandatory = $true)] [string] $DumpSha256
        )

        $readmePath = Join-Path $dumpDir 'TOO-LARGE-DUMP-README.txt'
        $hash = $DumpSha256
        @(
            'A full live process dump was captured but intentionally omitted from routine evidence packages.',
            "CapturedMode=$Mode",
            "DumpPath=$($DumpItem.FullName)",
            "DumpSize=$($DumpItem.Length)",
            "DumpSha256=$hash",
            "GeneratedAt=$(Get-Date -Format o)"
        ) | Set-Content -LiteralPath $readmePath
        $summary.Add("DumpSha256=$hash") | Out-Null
        $summary.Add("TooLargeDumpReadme=$readmePath") | Out-Null
    }

function Invoke-ComSvcsDumpAttempt {
        $attempt = [ordered]@{
            Mode = 'ComSvcsFull'
            Status = 'MissingDump'
            DumpPath = $null
            TempDumpPath = $null
            DumpSize = 0
            DumpSha256 = $null
            HandoffVerified = $false
            ExitCode = $null
            Error = $null
        }

        $rundll32Path = Join-Path $env:WINDIR 'System32\rundll32.exe'
        $comsvcsPath = Join-Path $env:WINDIR 'System32\comsvcs.dll'
        $tempDumpPath = Join-Path $tempDumpDir ("DolocTown-$targetPid-fatal-live-comsvcs.dmp")
        $finalDumpPath = Join-Path $dumpDir ("DolocTown-$targetPid-fatal-live-comsvcs.dmp")
        $modeStdoutPath = Join-Path $dumpDir 'process-dump-comsvcs-stdout.txt'
        $modeStderrPath = Join-Path $dumpDir 'process-dump-comsvcs-stderr.txt'
        $attempt.TempDumpPath = $tempDumpPath
        $attempt.DumpPath = $finalDumpPath

        try {
            if (-not (Test-Path -LiteralPath $rundll32Path -PathType Leaf)) {
                throw "rundll32.exe was not found at '$rundll32Path'."
            }

            if (-not (Test-Path -LiteralPath $comsvcsPath -PathType Leaf)) {
                throw "comsvcs.dll was not found at '$comsvcsPath'."
            }

            Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $finalDumpPath -Force -ErrorAction SilentlyContinue
            $argumentParts = @("$comsvcsPath,", 'MiniDump', [string]$targetPid, $tempDumpPath, 'full')
            $commandLine = "`"$rundll32Path`" $([string]::Join(' ', $argumentParts))"
            Add-DumpText -Path $commandPath -Text "[ComSvcsFull] $commandLine"
            $startedAt = Get-Date
            $dumpProcess = Start-Process -FilePath $rundll32Path -ArgumentList $argumentParts -Wait -PassThru -NoNewWindow -RedirectStandardOutput $modeStdoutPath -RedirectStandardError $modeStderrPath
            $finishedAt = Get-Date
            $attempt.ExitCode = $dumpProcess.ExitCode
            Add-DumpText -Path $stdoutPath -Text "[ComSvcsFull stdout]"
            if (Test-Path -LiteralPath $modeStdoutPath) {
                Add-DumpText -Path $stdoutPath -Text (Get-Content -Raw -LiteralPath $modeStdoutPath -ErrorAction SilentlyContinue)
            }
            Add-DumpText -Path $stderrPath -Text "[ComSvcsFull stderr]"
            if (Test-Path -LiteralPath $modeStderrPath) {
                Add-DumpText -Path $stderrPath -Text (Get-Content -Raw -LiteralPath $modeStderrPath -ErrorAction SilentlyContinue)
            }

            $copiedDump = Copy-DumpToEvidence -SourcePath $tempDumpPath -DestinationPath $finalDumpPath
            if ($null -ne $copiedDump) {
                $attempt.Status = 'Captured'
                $attempt.DumpSize = $copiedDump.Length
                $attempt.DumpSha256 = $copiedDump.Sha256
                $attempt.HandoffVerified = [bool]$copiedDump.Verified
            }
            else {
                $attempt.Status = 'MissingDump'
            }

            $summary.Add("ComSvcsFull.StartedAt=$($startedAt.ToString('o'))") | Out-Null
            $summary.Add("ComSvcsFull.FinishedAt=$($finishedAt.ToString('o'))") | Out-Null
            $summary.Add("ComSvcsFull.ExitCode=$($attempt.ExitCode)") | Out-Null
            $summary.Add("ComSvcsFull.TempDumpPath=$tempDumpPath") | Out-Null
            $summary.Add("ComSvcsFull.DumpPath=$finalDumpPath") | Out-Null
            $summary.Add("ComSvcsFull.Status=$($attempt.Status)") | Out-Null
            $summary.Add("ComSvcsFull.DumpSize=$($attempt.DumpSize)") | Out-Null
            $summary.Add("ComSvcsFull.DumpSha256=$($attempt.DumpSha256)") | Out-Null
            $summary.Add("ComSvcsFull.HandoffVerified=$($attempt.HandoffVerified)") | Out-Null
            return [pscustomobject]$attempt
        }
        catch {
            $attempt.Status = 'Error'
            $attempt.Error = "$($_.Exception.GetType().Name): $($_.Exception.Message)"
            $summary.Add("ComSvcsFull.Status=Error") | Out-Null
            $summary.Add("ComSvcsFull.Error=$($attempt.Error)") | Out-Null
            Add-DumpText -Path $errorPath -Text "[ComSvcsFull] $($attempt.Error)"
            return [pscustomobject]$attempt
        }
        finally {
            if ($attempt.HandoffVerified -and (Test-Path -LiteralPath $tempDumpPath -PathType Leaf)) {
                try {
                    Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction Stop
                    $summary.Add("ComSvcsFull.TempCleanup=DeletedAfterVerifiedHandoff") | Out-Null
                }
                catch {
                    $summary.Add("ComSvcsFull.TempCleanup=CleanupPending") | Out-Null
                    Add-DumpText -Path $errorPath -Text "[ComSvcsFull cleanup] $($_.Exception.GetType().Name): $($_.Exception.Message)"
                }
            }
        }
    }

function Invoke-DbgHelpDumpAttempt {
        $attempt = [ordered]@{
            Mode = 'DbgHelpFull'
            Status = 'MissingDump'
            DumpPath = $null
            TempDumpPath = $null
            DumpSize = 0
            DumpSha256 = $null
            HandoffVerified = $false
            ExitCode = $null
            Error = $null
        }

        $tempDumpPath = Join-Path $tempDumpDir ("DolocTown-$targetPid-fatal-live-dbghelp.dmp")
        $finalDumpPath = Join-Path $dumpDir ("DolocTown-$targetPid-fatal-live-dbghelp.dmp")
        $attempt.TempDumpPath = $tempDumpPath
        $attempt.DumpPath = $finalDumpPath
        Add-DumpText -Path $commandPath -Text "[DbgHelpFull] MiniDumpWriteDump(pid=$targetPid, dumpType=MiniDumpWithFullMemory, tempPath=$tempDumpPath)"

        try {
            if (-not ([System.Management.Automation.PSTypeName]'SmokeMiniDumpWriter').Type) {
                Add-Type @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class SmokeMiniDumpWriter
{
    [DllImport("Dbghelp.dll", SetLastError = true)]
    public static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        int processId,
        SafeFileHandle hFile,
        int dumpType,
        IntPtr exceptionParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);

    public static int GetLastError()
    {
        return Marshal.GetLastWin32Error();
    }
}
'@
            }

            Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $finalDumpPath -Force -ErrorAction SilentlyContinue
            $startedAt = Get-Date
            $fileStream = [System.IO.File]::Open($tempDumpPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $miniDumpWithFullMemory = 2
                $ok = [SmokeMiniDumpWriter]::MiniDumpWriteDump(
                    $Process.Handle,
                    $targetPid,
                    $fileStream.SafeFileHandle,
                    $miniDumpWithFullMemory,
                    [IntPtr]::Zero,
                    [IntPtr]::Zero,
                    [IntPtr]::Zero)
                $lastError = [SmokeMiniDumpWriter]::GetLastError()
            }
            finally {
                $fileStream.Dispose()
            }
            $finishedAt = Get-Date

            if (-not $ok) {
                $attempt.Status = 'Error'
                $attempt.Error = "MiniDumpWriteDump returned false. LastWin32Error=$lastError"
                Add-DumpText -Path $errorPath -Text "[DbgHelpFull] $($attempt.Error)"
            }

            $copiedDump = Copy-DumpToEvidence -SourcePath $tempDumpPath -DestinationPath $finalDumpPath
            if ($null -ne $copiedDump) {
                $attempt.Status = 'Captured'
                $attempt.DumpSize = $copiedDump.Length
                $attempt.DumpSha256 = $copiedDump.Sha256
                $attempt.HandoffVerified = [bool]$copiedDump.Verified
            }
            elseif ($attempt.Status -ne 'Error') {
                $attempt.Status = 'MissingDump'
            }

            $summary.Add("DbgHelpFull.StartedAt=$($startedAt.ToString('o'))") | Out-Null
            $summary.Add("DbgHelpFull.FinishedAt=$($finishedAt.ToString('o'))") | Out-Null
            $summary.Add("DbgHelpFull.LastWin32Error=$lastError") | Out-Null
            $summary.Add("DbgHelpFull.TempDumpPath=$tempDumpPath") | Out-Null
            $summary.Add("DbgHelpFull.DumpPath=$finalDumpPath") | Out-Null
            $summary.Add("DbgHelpFull.Status=$($attempt.Status)") | Out-Null
            $summary.Add("DbgHelpFull.DumpSize=$($attempt.DumpSize)") | Out-Null
            $summary.Add("DbgHelpFull.DumpSha256=$($attempt.DumpSha256)") | Out-Null
            $summary.Add("DbgHelpFull.HandoffVerified=$($attempt.HandoffVerified)") | Out-Null
            return [pscustomobject]$attempt
        }
        catch {
            $attempt.Status = 'Error'
            $attempt.Error = "$($_.Exception.GetType().Name): $($_.Exception.Message)"
            $summary.Add("DbgHelpFull.Status=Error") | Out-Null
            $summary.Add("DbgHelpFull.Error=$($attempt.Error)") | Out-Null
            Add-DumpText -Path $errorPath -Text "[DbgHelpFull] $($attempt.Error)"
            return [pscustomobject]$attempt
        }
        finally {
            if ($attempt.HandoffVerified -and (Test-Path -LiteralPath $tempDumpPath -PathType Leaf)) {
                try {
                    Remove-Item -LiteralPath $tempDumpPath -Force -ErrorAction Stop
                    $summary.Add("DbgHelpFull.TempCleanup=DeletedAfterVerifiedHandoff") | Out-Null
                }
                catch {
                    $summary.Add("DbgHelpFull.TempCleanup=CleanupPending") | Out-Null
                    Add-DumpText -Path $errorPath -Text "[DbgHelpFull cleanup] $($_.Exception.GetType().Name): $($_.Exception.Message)"
                }
            }
        }
    }
