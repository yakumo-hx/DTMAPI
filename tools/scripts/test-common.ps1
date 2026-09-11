# Development build/test-entry helpers; not part of the player installer/runtime.
function Get-DtmApiReleaseTestSelection {
    param([string] $Stage = 'All', [string] $StartAt = 'FromStart')
    $stages = @('BuildAndUnit', 'RuntimeInstaller', 'ScriptContracts', 'AuthorSdk',
        'ProductContracts', 'QaLifecycle', 'RetainedAbi', 'InstallerMatrices', 'Governance')
    $aliases = @{ FromStart = 'BuildAndUnit'; PostRuntimeInstaller = 'ScriptContracts' }
    if ([string]::IsNullOrWhiteSpace($Stage) -or ($Stage -ne 'All' -and $Stage -notin $stages)) {
        throw "Unknown Release stage '$Stage'. Use test.ps1 -List."
    }
    if ($aliases.ContainsKey($StartAt)) { $first = $aliases[$StartAt] }
    elseif ($StartAt -in $stages) { $first = @($stages | Where-Object { $_ -eq $StartAt })[0] }
    else { throw "Unknown Release start '$StartAt'. Use test.ps1 -List." }
    if ($Stage -ne 'All' -and $StartAt -ne 'FromStart') {
        throw 'Select either -Stage or -StartAt, not both.'
    }
    $selected = if ($Stage -ne 'All') { @($Stage) }
        else { @($stages[[Array]::IndexOf($stages, $first)..($stages.Count - 1)]) }
    [pscustomobject]@{
        Stages = $selected
        CompleteRun = ($Stage -eq 'All' -and $StartAt -eq 'FromStart')
    }
}

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

function Assert-DtmApiFullTestEnvironment {
    $processEnvironment = [Environment]::GetEnvironmentVariables('Process')
    $filters = @($processEnvironment.Keys | Where-Object {
        $_ -like 'DTMAPI_*_TEST_FOCUS' -and
        -not [string]::IsNullOrEmpty([string]$processEnvironment[$_])
    } | Sort-Object)
    if ($filters.Count -gt 0) {
        throw "Full validation does not accept focus filters: $($filters -join ', '). Run the selected test project directly, or clear these process-scoped variables before a full run."
    }
}

function Invoke-DtmApiProjectBuild {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $DotNetExe,
        [Parameter(Mandatory = $true)] [string[]] $Projects,
        [string] $Configuration = 'Release',
        [switch] $Rebuild
    )
    $selectedProjects = @($Projects | Select-Object -Unique)
    if ($selectedProjects.Count -eq 0) { throw 'No projects were selected for building.' }
    foreach ($project in $selectedProjects) {
        if ([string]::IsNullOrWhiteSpace($project) -or -not (Test-Path -LiteralPath (Join-Path $RepoRoot $project) -PathType Leaf)) { throw "Selected project is missing: $project" }
    }
    $buildTarget = if ($Rebuild) { 'Rebuild' } else { 'Build' }
    if ($selectedProjects.Count -eq 1) {
        & $DotNetExe build (Join-Path $RepoRoot $selectedProjects[0]) -c $Configuration --nologo -m:1 "-t:$buildTarget"
        if ($LASTEXITCODE -ne 0) { throw "Selected project build failed: $($selectedProjects[0])" }
        return
    }

    # This temporary filter is a projection of the caller's selected projects.
    # One MSBuild invocation reuses shared dependencies without a second registry.
    $filterRoot = Join-Path $RepoRoot 'tmp\unit-build-filters'
    New-Item -ItemType Directory -Force -Path $filterRoot | Out-Null
    $filterPath = Join-Path $filterRoot (([Guid]::NewGuid().ToString('N')) + '.slnf')
    try {
        $filter = @{ solution = @{ path = (Join-Path $RepoRoot 'DTMAPI.sln'); projects = @($selectedProjects | ForEach-Object { $_.Replace('/', '\') }) } }
        $encoding = New-Object System.Text.UTF8Encoding($false)
        [IO.File]::WriteAllText($filterPath, ($filter | ConvertTo-Json -Depth 5), $encoding)
        & $DotNetExe build $filterPath -c $Configuration --nologo -m:1 "-t:$buildTarget"
        if ($LASTEXITCODE -ne 0) { throw 'Selected solution-filter build failed.' }
    }
    finally { if (Test-Path -LiteralPath $filterPath) { Remove-Item -LiteralPath $filterPath -Force } }
}
