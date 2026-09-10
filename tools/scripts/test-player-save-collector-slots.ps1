[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $repoRoot ('temp/player-save-collector-slots-' + [Guid]::NewGuid().ToString('N'))
$profile = Join-Path $testRoot 'profile'
$persistent = Join-Path $profile 'AppData/LocalLow/RedSawGames/DolocTown'
$save = Join-Path $persistent 'SAVE'
$crash = Join-Path $testRoot 'crash'
[void][IO.Directory]::CreateDirectory((Join-Path $save 'nested'))
[void][IO.Directory]::CreateDirectory($crash)
$utf8 = New-Object Text.UTF8Encoding($false)
$sourceFiles = @('doloc-save-0.data', 'doloc-save-0.data.prev0', 'doloc-save-0.data.prev4', 'doloc-save-0.data.bak', 'doloc-save-1.data', 'doloc-save-11.data', 'doloc-save-11.data.prev0', 'doloc-save-9.data.prev0', 'mod_infos.json', 'doloc-save-0-tmp.data', 'nested/other.data')
foreach ($name in $sourceFiles) { [IO.File]::WriteAllText((Join-Path $save $name), ('synthetic-' + $name), $utf8) }
[IO.File]::WriteAllText((Join-Path $persistent 'Player.log'), 'synthetic log', $utf8)
$before = @{}
foreach ($name in $sourceFiles) { $before[$name] = (Get-FileHash -LiteralPath (Join-Path $save $name) -Algorithm SHA256).Hash }
$windowsPowerShell = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$collector = Join-Path $repoRoot 'tools/release/player-save-crash-collector/collect-save-and-crash-logs.ps1'
$oldDiscovery = $env:DTMAPI_SUPPORT_NO_GAME_DISCOVERY
$env:DTMAPI_SUPPORT_NO_GAME_DISCOVERY = '1'
$results = New-Object 'System.Collections.Generic.List[object]'
try {
    foreach ($case in @(
        @{ Name = 'full'; Slot = $null; Exit = 0; Files = $sourceFiles; Scope = 'FullSAVE' },
        @{ Name = 'slot0'; Slot = 0; Exit = 0; Files = @('doloc-save-0.data', 'doloc-save-0.data.prev0', 'doloc-save-0.data.prev4', 'doloc-save-0.data.bak'); Scope = 'SelectedNativeSlots' },
        @{ Name = 'slot11'; Slot = 11; Exit = 0; Files = @('doloc-save-11.data', 'doloc-save-11.data.prev0'); Scope = 'SelectedNativeSlots' },
        @{ Name = 'missing-current'; Slot = 9; Exit = 2; Files = @('doloc-save-9.data.prev0'); Scope = 'SelectedNativeSlots' }
    )) {
        $destination = Join-Path $testRoot $case.Name
        [void][IO.Directory]::CreateDirectory($destination)
        $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $collector, '-DestinationDirectory', $destination, '-UserProfileRoot', $profile, '-PersistentRoot', $persistent, '-CrashTempRoot', $crash, '-SkipWindowsEvents', '-SkipProcessCheck')
        if ($null -ne $case.Slot) { $arguments += @('-SlotIndex', [string]$case.Slot) }
        & $windowsPowerShell @arguments > (Join-Path $destination 'stdout.txt')
        if ($LASTEXITCODE -ne $case.Exit) { throw ('Unexpected collector status: ' + $case.Name + '/' + $LASTEXITCODE) }
        $zipPaths = @(Get-ChildItem -LiteralPath $destination -Filter '*.zip' -File)
        if ($zipPaths.Count -ne 1) { throw 'Expected one collector ZIP.' }
        $zip = [IO.Compression.ZipFile]::OpenRead($zipPaths[0].FullName)
        try {
            $saveEntries = @($zip.Entries | Where-Object { $_.FullName.Replace('\', '/').Contains('/SAVE/') })
            $actualNames = @($saveEntries | ForEach-Object { $_.FullName.Replace('\', '/') -replace '^.*?/SAVE/', '' } | Sort-Object)
            if (($actualNames -join '|') -cne (($case.Files | Sort-Object) -join '|')) { throw ('Unexpected selected SAVE files: ' + $case.Name) }
            $summary = $zip.GetEntry('collection-summary.txt').Open()
            $reader = New-Object IO.StreamReader($summary)
            try { $summaryText = $reader.ReadToEnd() } finally { $reader.Dispose(); $summary.Dispose() }
            if (-not $summaryText.Contains('SaveCollectionScope=' + $case.Scope)) { throw 'Collector scope not recorded.' }
            if ($null -ne $case.Slot -and -not $summaryText.Contains('NativeSlotIndices=' + $case.Slot)) { throw 'Selected native slot not recorded.' }
            if ($case.Exit -eq 2 -and -not $summaryText.Contains('CollectionStatus=Incomplete')) { throw 'Missing current file reported complete.' }
            if (@($zip.Entries | Where-Object { $_.FullName.Replace('\', '/').EndsWith('/Player.log') }).Count -ne 1) { throw 'Selected-slot collection lost the related log.' }
        }
        finally { $zip.Dispose() }
        [void]$results.Add([pscustomobject]@{ Name = $case.Name; Status = 'Passed'; ExpectedExit = $case.Exit; SaveFileCount = $case.Files.Count })
    }
    foreach ($name in $sourceFiles) {
        if ((Get-FileHash -LiteralPath (Join-Path $save $name) -Algorithm SHA256).Hash -cne $before[$name]) { throw 'Collector changed source data.' }
    }
    $report = [ordered]@{ Status = 'Passed'; Cases = $results.ToArray(); SourcesUnchanged = $true; SyntheticFixture = $true; PowerShell = $PSVersionTable.PSVersion.ToString(); TestRoot = $testRoot }
    $reportPath = Join-Path $testRoot 'result.json'
    [IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 8), $utf8)
    [pscustomobject]@{ Status = 'Passed'; Cases = $results.Count; Evidence = $reportPath }
}
finally { $env:DTMAPI_SUPPORT_NO_GAME_DISCOVERY = $oldDiscovery }
