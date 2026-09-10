param()

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
$fixtureRoot = Join-Path $repo ('tmp\unit-routing\' + [Guid]::NewGuid().ToString('N'))
$fixtureScripts = Join-Path $fixtureRoot 'tools\scripts'
$fixtureEntry = Join-Path $fixtureScripts 'test-unit.ps1'
$sdkWitness = Join-Path $fixtureRoot 'sdk-called.txt'
$runWitness = Join-Path $fixtureRoot 'runs.txt'
$shellExe = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$previousFocus = $env:DTMAPI_UNIT_TEST_FOCUS
$count = 0

function Invoke-RouteGuard {
    param([string] $Arguments, [int] $ExpectedExit, [string] $Pattern)
    $command = "& '" + $fixtureEntry.Replace("'", "''") + "' " + $Arguments
    $priorPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = (& $shellExe -NoProfile -ExecutionPolicy Bypass -Command $command 2>&1 | Out-String)
        $actualExit = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $priorPreference }
    if ($actualExit -ne $ExpectedExit -or $output -notmatch $Pattern) {
        throw "Unit route failure ($Arguments; exit=$actualExit expected=$ExpectedExit): $output"
    }
    $script:count++
}

try {
    New-Item -ItemType Directory -Force -Path $fixtureScripts, (Join-Path $fixtureRoot 'tests\DTMAPI.UnitTests') | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'test-unit.ps1') -Destination $fixtureEntry
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'test-common.ps1') -Destination (Join-Path $fixtureScripts 'test-common.ps1')
    @'
function Get-RepoRoot { return [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..')) }
function Get-DotNetExe {
    param([string] $RepoRoot)
    'called' | Set-Content -LiteralPath (Join-Path $RepoRoot 'sdk-called.txt')
    return (Join-Path $RepoRoot 'fake-dotnet.ps1')
}
'@ | Set-Content -LiteralPath (Join-Path $fixtureScripts 'common.ps1') -Encoding UTF8
    @'
param([Parameter(ValueFromRemainingArguments = $true)] [string[]] $Values)
if ($Values -contains 'build' -or $Values -contains 'run') { throw 'NoBuild attempted a build-capable dotnet command.' }
($Values -join '|') | Add-Content -LiteralPath (Join-Path $PSScriptRoot 'runs.txt')
exit 0
'@ | Set-Content -LiteralPath (Join-Path $fixtureRoot 'fake-dotnet.ps1') -Encoding UTF8
    $fixtureSuites = @()
    foreach ($id in @('alpha', 'beta')) {
        $project = "tests/$id/$id.csproj"
        $directory = Join-Path $fixtureRoot "tests\$id\bin\Release\net8.0"
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
        '' | Set-Content -LiteralPath (Join-Path $directory 'DTMAPI.UnitTests.dll')
        $fixtureSuites += @{ id = $id; project = $project; default = @('Default'); focuses = @{ 'shared-focus' = @('Focused') }; requiresWindows = $false; dependencies = @('MustNotBePreparedByNoBuild') }
    }
    @{ schemaVersion = 1; suites = $fixtureSuites } | ConvertTo-Json -Depth 7 |
        Set-Content -LiteralPath (Join-Path $fixtureRoot 'tests\DTMAPI.UnitTests\suites.json') -Encoding UTF8

    $env:DTMAPI_UNIT_TEST_FOCUS = $null
    foreach ($invalid in @("'unknown-focus'", "' '", "''")) {
        Invoke-RouteGuard -Arguments "-Focus $invalid" -ExpectedExit 1 -Pattern 'Unknown DTMAPI Unit test focus: .*Default tests were not run'
        if (Test-Path -LiteralPath $sdkWitness) { throw 'Invalid focus reached SDK lookup.' }
    }
    $env:DTMAPI_UNIT_TEST_FOCUS = 'unknown-ambient-focus'
    Invoke-RouteGuard -Arguments '' -ExpectedExit 1 -Pattern 'Unknown DTMAPI Unit test focus'
    Invoke-RouteGuard -Arguments "-Focus 'shared-focus' -List" -ExpectedExit 0 -Pattern 'shared-focus'
    if (Test-Path -LiteralPath $sdkWitness) { throw 'List/invalid ambient focus reached SDK lookup.' }

    # An explicit valid focus overrides the ambient value, selects both graphs,
    # and executes only existing DLLs without preparation or build commands.
    Invoke-RouteGuard -Arguments "-Focus 'shared-focus' -NoBuild" -ExpectedExit 0 -Pattern 'Unit selection passed: 2 projects'
    $runs = @(Get-Content -LiteralPath $runWitness)
    if ($runs.Count -ne 2 -or @($runs | Where-Object { $_ -notmatch 'DTMAPI.UnitTests.dll\|--focus\|shared-focus$' }).Count -gt 0) {
        throw "NoBuild focus selected unexpected process arguments: $($runs -join '; ')"
    }
    $count++
    Remove-Item -LiteralPath $sdkWitness, $runWitness
    $env:DTMAPI_UNIT_TEST_FOCUS = $null
    Invoke-RouteGuard -Arguments '-NoBuild' -ExpectedExit 0 -Pattern 'Unit selection passed: 2 projects'
    $runs = @(Get-Content -LiteralPath $runWitness)
    if ($runs.Count -ne 2 -or @($runs | Where-Object { $_ -match '--focus|build' }).Count -gt 0) { throw 'Default NoBuild did not execute both default graphs directly.' }
    $count++

    Remove-Item -LiteralPath $sdkWitness
    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'tests\beta\bin\Release\net8.0\DTMAPI.UnitTests.dll')
    Invoke-RouteGuard -Arguments '-NoBuild' -ExpectedExit 1 -Pattern '(?s)Unit suite output missing:.*test-unit.ps1'
    if (Test-Path -LiteralPath $sdkWitness) { throw 'Missing NoBuild output reached SDK lookup or attempted a rebuild.' }

    $actual = Get-Content -LiteralPath (Join-Path $repo 'tests\DTMAPI.UnitTests\suites.json') -Raw | ConvertFrom-Json
    $defaultCalls = @($actual.suites | ForEach-Object { $_.default })
    if ($defaultCalls -notcontains 'MoreEquipmentSlotsProductTests.RunAll') { throw 'Default Unit coverage lost the MoreEquipment ordinary suite.' }
    if ($defaultCalls -contains 'MoreEquipmentSlotsProductTests.RunCrossProcessClaimActor' -or $defaultCalls -contains 'OfficialSourceArbitrationOneOffMatrix') { throw 'An actor/one-off was incorrectly admitted to default tests.' }
    foreach ($suite in $actual.suites) {
        $projectPath = Join-Path $repo $suite.project
        if (-not (Test-Path -LiteralPath $projectPath)) { throw "Registered Unit project does not exist: $($suite.project)" }
    }
    $count++
    Write-Host "PASS: $count Unit routing checks (Windows PowerShell 5.1; invalid focus before SDK, explicit override, default coverage, and NoBuild)."
}
finally {
    $env:DTMAPI_UNIT_TEST_FOCUS = $previousFocus
    $resolved = [IO.Path]::GetFullPath($fixtureRoot)
    $allowed = [IO.Path]::GetFullPath((Join-Path $repo 'tmp\unit-routing')).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw "Refusing cleanup outside Unit routing fixtures: $resolved" }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
exit 0
