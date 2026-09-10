function Get-SmokeIssue011FileReceipt {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "ISSUE-011 required evidence file is missing: $fullPath"
    }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if ([int64]$item.Length -le 0) {
        throw "ISSUE-011 required evidence file is empty: $fullPath"
    }
    return [ordered]@{
        Path = $fullPath
        Length = [int64]$item.Length
        Sha256 = (Get-SmokeFileSha256 -Path $fullPath).ToLowerInvariant()
    }
}

function Get-SmokeIssue011LastGiveBaseline {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $exists = Test-Path -LiteralPath $fullPath -PathType Leaf
    $item = if ($exists) { Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop } else { $null }
    return [ordered]@{
        Path = $fullPath
        Existed = $exists
        Length = if ($exists) { [int64]$item.Length } else { [int64]0 }
        Sha256 = if ($exists) { (Get-SmokeFileSha256 -Path $fullPath).ToLowerInvariant() } else { '' }
        LastWriteTimeUtc = if ($exists) { $item.LastWriteTimeUtc.ToString('o') } else { '' }
    }
}

function Get-SmokeIssue011RuntimeAssemblies {
    param([Parameter(Mandatory = $true)] [string] $GameDirectory)

    $runtimeRoot = [System.IO.Path]::GetFullPath((Join-Path $GameDirectory 'BepInEx\plugins\DTMAPI'))
    $expected = @(
        'DTMAPI.Abstractions.dll',
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    ) | Sort-Object
    $actual = if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $runtimeRoot -Recurse -Force -File -Filter '*.dll' -ErrorAction Stop |
            ForEach-Object { $_.FullName.Substring($runtimeRoot.Length + 1).Replace([char]92, [char]47) } |
            Sort-Object)
    }
    else {
        @()
    }
    if (-not [string]::Equals((@($expected) -join "`n"), (@($actual) -join "`n"), [System.StringComparison]::Ordinal)) {
        throw "ISSUE-011 requires the exact five-DLL installed Runtime. expected=$($expected -join '|') actual=$($actual -join '|')"
    }
    return @($expected | ForEach-Object {
        $receipt = Get-SmokeIssue011FileReceipt -Path (Join-Path $runtimeRoot $_)
        [ordered]@{
            FileName = [string]$_
            Path = [string]$receipt.Path
            Length = [int64]$receipt.Length
            Sha256 = [string]$receipt.Sha256
        }
    })
}

function Get-SmokeIssue011SteamLifecycleEvidence {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt,
        [ValidateRange(0, 30)] [int] $WaitSeconds = 15
    )

    $steamInfoPath = Join-Path $EvidencePath 'steam-info.txt'
    $steamRoot = ''
    if (Test-Path -LiteralPath $steamInfoPath -PathType Leaf) {
        $steamRootLine = @(Get-Content -LiteralPath $steamInfoPath -ErrorAction SilentlyContinue |
            Where-Object { [string]$_ -like 'SteamRoot=*' } | Select-Object -First 1)
        if ($steamRootLine.Count -eq 1) {
            $steamRoot = [string]$steamRootLine[0].Substring('SteamRoot='.Length)
        }
    }
    $sourcePath = if ([string]::IsNullOrWhiteSpace($steamRoot)) { '' } else {
        [System.IO.Path]::GetFullPath((Join-Path $steamRoot 'logs\console_log.txt'))
    }
    $snapshotPath = [System.IO.Path]::GetFullPath((Join-Path $EvidencePath 'issue-011-steam-lifecycle.txt'))
    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    $lines = @()
    $addedIndex = -1
    $removedIndex = -1
    $addedLine = ''
    $removedLine = ''
    $processId = 0
    $addedAt = [datetime]::MinValue
    $removedAt = [datetime]::MinValue
    do {
        if (-not [string]::IsNullOrWhiteSpace($sourcePath) -and (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            $lines = @(Get-Content -LiteralPath $sourcePath -Tail 2000 -ErrorAction SilentlyContinue)
            $addedIndex = -1
            for ($index = 0; $index -lt $lines.Count; $index++) {
                $match = [regex]::Match(
                    [string]$lines[$index],
                    '^\[(?<Time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] Game process added : AppID 2285550 .*ProcID (?<Pid>\d+)')
                if (-not $match.Success) { continue }
                $parsed = [datetime]::MinValue
                if (-not [datetime]::TryParseExact(
                        $match.Groups['Time'].Value,
                        'yyyy-MM-dd HH:mm:ss',
                        [Globalization.CultureInfo]::InvariantCulture,
                        [Globalization.DateTimeStyles]::AssumeLocal,
                        [ref]$parsed)) { continue }
                if ($parsed.ToUniversalTime() -ge $RunStartedAt.ToUniversalTime().AddSeconds(-10)) {
                    $addedIndex = $index
                    $addedLine = [string]$lines[$index]
                    $processId = [int]$match.Groups['Pid'].Value
                    $addedAt = $parsed
                }
            }
            if ($addedIndex -ge 0) {
                for ($index = $addedIndex + 1; $index -lt $lines.Count; $index++) {
                    $match = [regex]::Match(
                        [string]$lines[$index],
                        '^\[(?<Time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] Game process removed: AppID 2285550 .*ProcID ' + [regex]::Escape([string]$processId) + '\s*$')
                    if (-not $match.Success) { continue }
                    $parsed = [datetime]::MinValue
                    if ([datetime]::TryParseExact(
                            $match.Groups['Time'].Value,
                            'yyyy-MM-dd HH:mm:ss',
                            [Globalization.CultureInfo]::InvariantCulture,
                            [Globalization.DateTimeStyles]::AssumeLocal,
                            [ref]$parsed)) {
                        $removedIndex = $index
                        $removedLine = [string]$lines[$index]
                        $removedAt = $parsed
                        break
                    }
                }
            }
        }
        if ($removedIndex -ge 0 -or (Get-Date) -ge $deadline) { break }
        Start-Sleep -Milliseconds 500
    } while ($true)

    $waitingAfterRemoval = 0
    if ($removedIndex -ge 0 -and ($removedIndex + 1) -lt $lines.Count) {
        $waitingAfterRemoval = @($lines[($removedIndex + 1)..($lines.Count - 1)] | Where-Object {
            [string]$_ -match 'AppID 2285550.*WaitingForExit|WaitingForExit.*AppID 2285550'
        }).Count
    }
    $snapshotLines = New-Object 'System.Collections.Generic.List[string]'
    $snapshotLines.Add("SourceConsoleLog=$sourcePath") | Out-Null
    $snapshotLines.Add("ObservedAt=$((Get-Date).ToString('o'))") | Out-Null
    $snapshotLines.Add("RunStartedAt=$($RunStartedAt.ToString('o'))") | Out-Null
    if ($addedIndex -ge 0) { $snapshotLines.Add("Added=$addedLine") | Out-Null }
    if ($removedIndex -ge 0) { $snapshotLines.Add("Removed=$removedLine") | Out-Null }
    $snapshotLines.Add("WaitingForExitAfterRemovalCount=$waitingAfterRemoval") | Out-Null
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($snapshotPath, ([string]::Join([Environment]::NewLine, $snapshotLines.ToArray()) + [Environment]::NewLine), $utf8NoBom)
    $passed = $addedIndex -ge 0 -and $removedIndex -gt $addedIndex -and
        $removedAt -ge $addedAt -and $waitingAfterRemoval -eq 0
    return [ordered]@{
        SourceConsoleLogPath = $sourcePath
        LifecycleSnapshot = Get-SmokeIssue011FileReceipt -Path $snapshotPath
        ProcessId = $processId
        AddedAt = if ($addedAt -ne [datetime]::MinValue) { $addedAt.ToString('o') } else { '' }
        RemovedAt = if ($removedAt -ne [datetime]::MinValue) { $removedAt.ToString('o') } else { '' }
        AddedLine = $addedLine
        RemovedLine = $removedLine
        WaitingForExitAfterRemovalCount = $waitingAfterRemoval
        Passed = $passed
    }
}

function New-SmokeIssue011AcceptanceProjection {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $GameDirectory,
        [Parameter(Mandatory = $true)] [int] $SaveSlot,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedAt,
        [Parameter(Mandatory = $true)] $LastGiveBaseline,
        [Parameter(Mandatory = $true)] $CrashFreshness,
        [Parameter(Mandatory = $true)] [bool] $NoQaGatePassed,
        [Parameter(Mandatory = $true)] [bool] $NoQaPublishedOwnerSetPassed,
        [Parameter(Mandatory = $true)] [bool] $SaveLoadedPassed,
        [Parameter(Mandatory = $true)] [bool] $InteractiveEvidenceObserved,
        [Parameter(Mandatory = $true)] [int] $FatalWindowCount,
        [Parameter(Mandatory = $true)] [bool] $ProcessExited,
        [Parameter(Mandatory = $true)] [bool] $ForcedClose
    )

    $evidenceRoot = [System.IO.Path]::GetFullPath($EvidencePath)
    $receiptPath = [System.IO.Path]::GetFullPath((Join-Path $evidenceRoot 'issue-011-acceptance.json'))
    $noQaPath = Join-Path $evidenceRoot 'no-qa-ui-evidence-gate.json'
    $noQaReceipt = Get-SmokeIssue011FileReceipt -Path $noQaPath
    $bepInExReceipt = Get-SmokeIssue011FileReceipt -Path (Join-Path $evidenceRoot 'BepInEx-LogOutput.log')
    $logLines = @(Get-Content -LiteralPath $bepInExReceipt.Path -ErrorAction Stop)

    $titleClicks = @($logLines | Where-Object { [string]$_ -like '*DTMAPI title settings button clicked.*' })
    $titleOpened = @($logLines | Where-Object { [string]$_ -like '*DTMAPI title settings menu opened.*' })
    $titlePassed = $titleClicks.Count -gt 0 -and $titleOpened.Count -gt 0

    $tooltipRows = New-Object 'System.Collections.Generic.List[string]'
    $searchRows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($line in $logLines) {
        $match = [regex]::Match([string]$line, 'DebugConsole status UI\.DebugConsoleItemTooltip=visible .*searchText=(?<Search>[^\.\r\n]+)\.')
        $kind = 'tooltip'
        if ($match.Success) {
            $tooltipRows.Add([string]$line) | Out-Null
        }
        else {
            $match = [regex]::Match([string]$line, 'Debug console open lifecycle state searchText=(?<Search>.*?) category=')
            $kind = 'open-lifecycle'
        }
        if ($match.Success -and -not [string]::Equals($match.Groups['Search'].Value.Trim(), '<empty>', [System.StringComparison]::OrdinalIgnoreCase) -and
            -not [string]::IsNullOrWhiteSpace($match.Groups['Search'].Value)) {
            $searchRows.Add([ordered]@{ Text = $match.Groups['Search'].Value.Trim(); Kind = $kind; Line = [string]$line }) | Out-Null
        }
    }
    $searchEvidence = if ($searchRows.Count -gt 0) { $searchRows[$searchRows.Count - 1] } else { $null }

    $lastGivePath = Join-Path $evidenceRoot 'debug-console-last-give.txt'
    $lastGiveReceipt = if (Test-Path -LiteralPath $lastGivePath -PathType Leaf) {
        Get-SmokeIssue011FileReceipt -Path $lastGivePath
    }
    else { $null }
    $lastGiveTimestamp = [DateTimeOffset]::MinValue
    $lastGiveStatus = ''
    $lastGiveItem = ''
    $lastGiveRequested = 0
    $lastGiveGiven = 0
    $lastGiveRightClick = $false
    $lastGiveParsed = $false
    if ($null -ne $lastGiveReceipt) {
        $lastGiveText = (Get-Content -Raw -LiteralPath $lastGiveReceipt.Path -ErrorAction Stop).Trim()
        $lastGiveMatch = [regex]::Match(
            $lastGiveText,
            '^(?<Time>\S+)\s+status=(?<Status>\S+)\s+source=(?<Source>\S+)\s+item=(?<Item>\S+)\s+requested=(?<Requested>\d+)\s+given=(?<Given>\d+)\s+rightClick=(?<RightClick>True|False)\s+failure=(?<Failure>.*)$')
        if ($lastGiveMatch.Success) {
            $parsedOffset = [DateTimeOffset]::MinValue
            if ([DateTimeOffset]::TryParse($lastGiveMatch.Groups['Time'].Value, [ref]$parsedOffset)) {
                $lastGiveTimestamp = $parsedOffset
                $lastGiveStatus = $lastGiveMatch.Groups['Status'].Value
                $lastGiveItem = $lastGiveMatch.Groups['Item'].Value
                $lastGiveRequested = [int]$lastGiveMatch.Groups['Requested'].Value
                $lastGiveGiven = [int]$lastGiveMatch.Groups['Given'].Value
                $lastGiveRightClick = [string]::Equals($lastGiveMatch.Groups['RightClick'].Value, 'True', [System.StringComparison]::OrdinalIgnoreCase)
                $lastGiveParsed = $true
            }
        }
    }
    $lastGiveChanged = $null -ne $lastGiveReceipt -and (
        -not [bool]$LastGiveBaseline.Existed -or
        -not [string]::Equals([string]$LastGiveBaseline.Sha256, [string]$lastGiveReceipt.Sha256, [System.StringComparison]::OrdinalIgnoreCase))
    $lastGiveFresh = $lastGiveParsed -and $lastGiveTimestamp.UtcDateTime -ge $RunStartedAt.ToUniversalTime().AddSeconds(-5)
    $lastGivePassed = $lastGiveChanged -and $lastGiveFresh -and $lastGiveStatus -ceq 'verified' -and
        -not [string]::IsNullOrWhiteSpace($lastGiveItem) -and $lastGiveRequested -gt 0 -and $lastGiveGiven -gt 0
    $debugPassed = $NoQaGatePassed -and $InteractiveEvidenceObserved -and
        $tooltipRows.Count -gt 0 -and $searchRows.Count -gt 0 -and $lastGivePassed

    $uninitializedLines = @($logLines | Where-Object { [string]$_ -match 'Input System not(?: yet)? initialized' })
    $fallbackFailureLines = @($logLines | Where-Object {
        [string]$_ -match 'Title settings EventSystem fallback creation failed; retrying\.|Debug console EventSystem fallback creation failed; retrying\.'
    })
    $inputSystemPassed = $uninitializedLines.Count -le 1 -and $fallbackFailureLines.Count -eq 0

    $summaryPath = Join-Path $evidenceRoot 'Unity-Crashes\summary.txt'
    $summaryReceipt = Get-SmokeIssue011FileReceipt -Path $summaryPath
    $summaryLines = @(Get-Content -LiteralPath $summaryReceipt.Path -ErrorAction Stop)
    $collectedAt = [DateTimeOffset]::MinValue
    $collectedLine = @($summaryLines | Where-Object { [string]$_ -like 'Collected=*' } | Select-Object -First 1)
    if ($collectedLine.Count -eq 1) {
        $parsedCollected = [DateTimeOffset]::MinValue
        if ([DateTimeOffset]::TryParse($collectedLine[0].Substring('Collected='.Length), [ref]$parsedCollected)) {
            $collectedAt = $parsedCollected
        }
    }
    $crashDumpPaths = @(Get-ChildItem -LiteralPath (Join-Path $evidenceRoot 'Unity-Crashes') -Recurse -Force -File -Filter 'crash.dmp' -ErrorAction SilentlyContinue |
        ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    $missingReasonPath = Join-Path $evidenceRoot 'Unity-Crashes\MISSING-CRASH-DUMP-README.txt'
    $missingReason = if (Test-Path -LiteralPath $missingReasonPath -PathType Leaf) {
        (Get-Content -Raw -LiteralPath $missingReasonPath -ErrorAction SilentlyContinue).Trim()
    }
    elseif (@($summaryLines | Where-Object { [string]$_ -like '*No Unity crash report directories were found*' }).Count -gt 0) {
        'No Unity crash report directories were found for the current Windows user.'
    }
    else { '' }
    $collectorFresh = $collectedAt -ne [DateTimeOffset]::MinValue -and
        $collectedAt.UtcDateTime -ge $RunStartedAt.ToUniversalTime().AddSeconds(-5)
    $packageExplainsDumpState = $crashDumpPaths.Count -gt 0 -or -not [string]::IsNullOrWhiteSpace($missingReason)
    $noFreshNativeCrash = [string]$CrashFreshness.Status -cne 'fresh' -and $FatalWindowCount -eq 0
    $crashPackagePassed = $collectorFresh -and $packageExplainsDumpState -and $noFreshNativeCrash

    $steamLifecycle = Get-SmokeIssue011SteamLifecycleEvidence -EvidencePath $evidenceRoot -RunStartedAt $RunStartedAt
    $runtimeAssemblies = @(Get-SmokeIssue011RuntimeAssemblies -GameDirectory $GameDirectory)
    $processPassed = $FatalWindowCount -eq 0 -and $ProcessExited -and -not $ForcedClose -and [bool]$steamLifecycle.Passed
    $passed = $SaveLoadedPassed -and $NoQaGatePassed -and $NoQaPublishedOwnerSetPassed -and
        $titlePassed -and $debugPassed -and $inputSystemPassed -and $crashPackagePassed -and $processPassed

    return [ordered]@{
        SchemaVersion = 1
        ReceiptKind = 'DTMAPI.ISSUE011.CurrentCandidateAcceptance'
        EvidenceRoot = $evidenceRoot
        ReceiptPath = $receiptPath
        GameDir = [System.IO.Path]::GetFullPath($GameDirectory)
        RunStartedAt = $RunStartedAt.ToString('o')
        CompletedAt = (Get-Date).ToString('o')
        SaveSlot = $SaveSlot
        SaveTestMode = 'NoNativeSave'
        LaunchMode = 'Steam'
        OfficialModProfile = 'Local11'
        AssertNoQaUiEvidence = $true
        RuntimeAssemblies = $runtimeAssemblies
        NoQaGate = [ordered]@{
            Path = [string]$noQaReceipt.Path
            Length = [int64]$noQaReceipt.Length
            Sha256 = [string]$noQaReceipt.Sha256
            Passed = $NoQaGatePassed
        }
        BepInExLog = $bepInExReceipt
        TitleSettings = [ordered]@{
            ButtonClickCount = $titleClicks.Count
            MenuOpenedCount = $titleOpened.Count
            ButtonClickLine = if ($titleClicks.Count -gt 0) { [string]$titleClicks[0] } else { '' }
            MenuOpenedLine = if ($titleOpened.Count -gt 0) { [string]$titleOpened[0] } else { '' }
            Passed = $titlePassed
        }
        DebugConsole = [ordered]@{
            OpenUseClosePassed = $NoQaGatePassed
            InteractiveObserved = $InteractiveEvidenceObserved
            TooltipEvidenceCount = $tooltipRows.Count
            TooltipLine = if ($tooltipRows.Count -gt 0) { [string]$tooltipRows[$tooltipRows.Count - 1] } else { '' }
            SearchEvidenceCount = $searchRows.Count
            SearchText = if ($null -ne $searchEvidence) { [string]$searchEvidence.Text } else { '' }
            SearchEvidenceKind = if ($null -ne $searchEvidence) { [string]$searchEvidence.Kind } else { '' }
            SearchLine = if ($null -ne $searchEvidence) { [string]$searchEvidence.Line } else { '' }
            LastGiveBaseline = $LastGiveBaseline
            LastGiveFile = $lastGiveReceipt
            LastGiveTimestamp = if ($lastGiveTimestamp -ne [DateTimeOffset]::MinValue) { $lastGiveTimestamp.ToString('o') } else { '' }
            LastGiveStatus = $lastGiveStatus
            LastGiveItem = $lastGiveItem
            LastGiveRequested = $lastGiveRequested
            LastGiveGiven = $lastGiveGiven
            LastGiveRightClick = $lastGiveRightClick
            Passed = $debugPassed
        }
        InputSystem = [ordered]@{
            UninitializedCount = $uninitializedLines.Count
            FallbackFailureCount = $fallbackFailureLines.Count
            UninitializedLines = @($uninitializedLines | Select-Object -First 5)
            FallbackFailureLines = @($fallbackFailureLines | Select-Object -First 5)
            Passed = $inputSystemPassed
        }
        CrashPackage = [ordered]@{
            Summary = $summaryReceipt
            CollectedAt = if ($collectedAt -ne [DateTimeOffset]::MinValue) { $collectedAt.ToString('o') } else { '' }
            FreshnessStatus = [string]$CrashFreshness.Status
            FreshnessReason = [string]$CrashFreshness.Reason
            FreshDirectories = @($CrashFreshness.FreshDirectories)
            CrashDumpPaths = $crashDumpPaths
            MissingReasonPath = if (Test-Path -LiteralPath $missingReasonPath -PathType Leaf) { [System.IO.Path]::GetFullPath($missingReasonPath) } else { '' }
            MissingReason = $missingReason
            PackageExplainsDumpState = $packageExplainsDumpState
            NoFreshNativeCrash = $noFreshNativeCrash
            Passed = $crashPackagePassed
        }
        SteamLifecycle = $steamLifecycle
        ProcessExit = [ordered]@{
            NoFatalInstanceWindow = $FatalWindowCount -eq 0
            ProcessExited = $ProcessExited
            ForcedClose = $ForcedClose
            Passed = $processPassed
        }
        SmokeResult = $null
        Passed = $passed
    }
}

function Test-SmokeIssue011AcceptanceProjection {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    $managedParent = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot 'tmp\test-runs\issue011-acceptance'))
    $root = [System.IO.Path]::GetFullPath((Join-Path $managedParent ([Guid]::NewGuid().ToString('N'))))
    if (-not $root.StartsWith($managedParent + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'ISSUE-011 projection self-test root escaped its managed parent.'
    }
    [System.IO.Directory]::CreateDirectory($root) | Out-Null
    try {
        $gameDir = Join-Path $root 'game'
        $runtimeRoot = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
        foreach ($name in @(
                'DTMAPI.Abstractions.dll',
                'DTMAPI.BepInExBootstrap.dll',
                'DTMAPI.Core.dll',
                'DTMAPI.GameBridge.DolocTown.dll',
                'DTMAPI.ModConfigMenu.dll'
            )) {
            [System.IO.Directory]::CreateDirectory($runtimeRoot) | Out-Null
            [System.IO.File]::WriteAllText((Join-Path $runtimeRoot $name), "fixture-$name", (New-Object System.Text.UTF8Encoding($false)))
        }
        $evidence = Join-Path $root 'evidence'
        [System.IO.Directory]::CreateDirectory((Join-Path $evidence 'Unity-Crashes')) | Out-Null
        $runStarted = (Get-Date).AddSeconds(-2)
        $now = [DateTimeOffset]::Now
        Write-SmokeJsonObject -Path (Join-Path $evidence 'no-qa-ui-evidence-gate.json') -Value ([ordered]@{ Passed = $true })
        $cleanLog = @(
            "$($now.ToString('o')) [Info] DTMAPI title settings button clicked.",
            "$($now.ToString('o')) [Info] DTMAPI title settings menu opened.",
            "$($now.ToString('o')) [Info] DebugConsole status UI.DebugConsoleItemTooltip=visible source=Unity UI pointer hover details=item=fixture_item, source=Vanilla, searchText=<empty>..",
            "$($now.ToString('o')) [Info] Debug console open lifecycle state searchText=fixture category=<empty> sourceFilter=__base itemPage=0."
        ) -join [Environment]::NewLine
        [System.IO.File]::WriteAllText((Join-Path $evidence 'BepInEx-LogOutput.log'), $cleanLog, (New-Object System.Text.UTF8Encoding($false)))
        $lastGivePath = Join-Path $evidence 'debug-console-last-give.txt'
        [System.IO.File]::WriteAllText($lastGivePath, ($now.ToString('o') + ' status=verified source=YConsole.ButtonLeftClick item=fixture_item requested=1 given=1 rightClick=False failure=reason=none.'), (New-Object System.Text.UTF8Encoding($false)))
        [System.IO.File]::WriteAllText((Join-Path $evidence 'Unity-Crashes\summary.txt'), "Collected=$($now.ToString('o'))`nNo Unity crash report directories were found for the current Windows user.`nFilesCopiedTotal=0`n", (New-Object System.Text.UTF8Encoding($false)))
        $steamRoot = Join-Path $root 'steam'
        [System.IO.Directory]::CreateDirectory((Join-Path $steamRoot 'logs')) | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $evidence 'steam-info.txt'), "SteamRoot=$steamRoot`n", (New-Object System.Text.UTF8Encoding($false)))
        $steamAdded = [DateTime]::Now.AddSeconds(-1)
        $steamRemoved = [DateTime]::Now
        $steamLines = @(
            ('[' + $steamAdded.ToString('yyyy-MM-dd HH:mm:ss') + '] Game process added : AppID 2285550 "fixture", ProcID 43210, IP 0.0.0.0:0'),
            ('[' + $steamRemoved.ToString('yyyy-MM-dd HH:mm:ss') + '] Game process removed: AppID 2285550 "fixture", ProcID 43210 ')
        ) -join [Environment]::NewLine
        [System.IO.File]::WriteAllText((Join-Path $steamRoot 'logs\console_log.txt'), $steamLines, (New-Object System.Text.UTF8Encoding($false)))
        $baseline = [ordered]@{
            Path = Join-Path $root 'state\debug-console-last-give.txt'
            Existed = $false
            Length = [int64]0
            Sha256 = ''
            LastWriteTimeUtc = ''
        }
        $crash = [pscustomobject]@{
            Status = 'missing'
            Reason = 'No copied crash files or directories could be attributed to this run.'
            FreshDirectories = @()
        }
        $positive = New-SmokeIssue011AcceptanceProjection `
            -EvidencePath $evidence -GameDirectory $gameDir -SaveSlot 10 -RunStartedAt $runStarted `
            -LastGiveBaseline $baseline -CrashFreshness $crash -NoQaGatePassed $true `
            -NoQaPublishedOwnerSetPassed $true -SaveLoadedPassed $true -InteractiveEvidenceObserved $true `
            -FatalWindowCount 0 -ProcessExited $true -ForcedClose $false

        [System.IO.File]::AppendAllText(
            (Join-Path $evidence 'BepInEx-LogOutput.log'),
            "`nInput System not yet initialized`nInput System not initialized`nTitle settings EventSystem fallback creation failed; retrying.",
            (New-Object System.Text.UTF8Encoding($false)))
        $repeatedInput = New-SmokeIssue011AcceptanceProjection `
            -EvidencePath $evidence -GameDirectory $gameDir -SaveSlot 10 -RunStartedAt $runStarted `
            -LastGiveBaseline $baseline -CrashFreshness $crash -NoQaGatePassed $true `
            -NoQaPublishedOwnerSetPassed $true -SaveLoadedPassed $true -InteractiveEvidenceObserved $true `
            -FatalWindowCount 0 -ProcessExited $true -ForcedClose $false

        [System.IO.File]::WriteAllText((Join-Path $evidence 'BepInEx-LogOutput.log'), $cleanLog, (New-Object System.Text.UTF8Encoding($false)))
        $staleGiveAt = $now.AddHours(-1)
        [System.IO.File]::WriteAllText($lastGivePath, ($staleGiveAt.ToString('o') + ' status=verified source=YConsole.ButtonLeftClick item=fixture_item requested=1 given=1 rightClick=False failure=reason=none.'), (New-Object System.Text.UTF8Encoding($false)))
        $staleLastGive = New-SmokeIssue011AcceptanceProjection `
            -EvidencePath $evidence -GameDirectory $gameDir -SaveSlot 10 -RunStartedAt $runStarted `
            -LastGiveBaseline $baseline -CrashFreshness $crash -NoQaGatePassed $true `
            -NoQaPublishedOwnerSetPassed $true -SaveLoadedPassed $true -InteractiveEvidenceObserved $true `
            -FatalWindowCount 0 -ProcessExited $true -ForcedClose $false

        return [ordered]@{
            PositivePassed = [bool]$positive.Passed
            RepeatedInputRejected = -not [bool]$repeatedInput.Passed -and -not [bool]$repeatedInput.InputSystem.Passed
            StaleLastGiveRejected = -not [bool]$staleLastGive.Passed -and -not [bool]$staleLastGive.DebugConsole.Passed
            Passed = [bool]$positive.Passed -and
                (-not [bool]$repeatedInput.Passed) -and (-not [bool]$repeatedInput.InputSystem.Passed) -and
                (-not [bool]$staleLastGive.Passed) -and (-not [bool]$staleLastGive.DebugConsole.Passed)
        }
    }
    finally {
        if (Test-Path -LiteralPath $root -PathType Container) {
            [System.IO.Directory]::Delete($root, $true)
        }
    }
}
