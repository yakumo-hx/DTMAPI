function Get-DtmApiActiveTestProcess {
    param([object[]] $ProcessSnapshot, [int] $OwnerProcessId = $PID, [switch] $IncludeGame)
    if (-not $PSBoundParameters.ContainsKey('ProcessSnapshot')) {
        $ProcessSnapshot = @(Get-CimInstance Win32_Process -ErrorAction Stop)
    }
    $byId = @{}
    foreach ($row in $ProcessSnapshot) { $byId[[int]$row.ProcessId] = $row }
    $ancestors = @{ $OwnerProcessId = $true }
    $cursor = $OwnerProcessId
    while ($byId.ContainsKey($cursor)) {
        $parent = [int]$byId[$cursor].ParentProcessId
        if ($parent -le 0 -or $ancestors.ContainsKey($parent)) { break }
        $ancestors[$parent] = $true
        $cursor = $parent
    }
    foreach ($row in $ProcessSnapshot) {
        if ($ancestors.ContainsKey([int]$row.ProcessId)) { continue }
        $command = [string]$row.CommandLine
        $isTest = $command -match 'DTMAPI\.[\w.]*Tests\b|(?:^|[\\/\s"''])(?:test|test-unit|test-public-source|run-game-smoke)\.ps1(?:[\s"'']|$)'
        if ($isTest -or ($IncludeGame -and $row.Name -ieq 'DolocTown.exe')) {
            [pscustomobject]@{ ProcessId = [int]$row.ProcessId; Name = [string]$row.Name; CommandLine = $command }
        }
    }
}
