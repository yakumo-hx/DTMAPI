param()
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/test-common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$root = Join-Path $repo ('tmp/test-runs/release-routing-' + [guid]::NewGuid().ToString('N'))
$scripts = Join-Path $root 'tools/scripts'
$shell = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$checks = 0
function Assert-Route([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw "Release routing failed: $Message" }
    $script:checks++
}
function Write-Fixture([string] $Path, [string] $Content) {
    [IO.File]::WriteAllText($Path, $Content, (New-Object Text.UTF8Encoding($false)))
}

# A sibling suite blocks shared cleanup; the owning driver and its ancestors do
# not. Include the game only for the Release entry's shared-environment guard.
$snapshot = @(
    [pscustomobject]@{ProcessId=10;ParentProcessId=0;Name='pwsh.exe';CommandLine='pwsh -File tools/scripts/test.ps1'},
    [pscustomobject]@{ProcessId=11;ParentProcessId=10;Name='pwsh.exe';CommandLine='pwsh -File tools/scripts/cleanup-test-artifacts.ps1'},
    [pscustomobject]@{ProcessId=12;ParentProcessId=0;Name='dotnet.exe';CommandLine='dotnet DTMAPI.MoreSaves.Tests.dll'},
    [pscustomobject]@{ProcessId=13;ParentProcessId=0;Name='DolocTown.exe';CommandLine=$null},
    [pscustomobject]@{ProcessId=14;ParentProcessId=0;Name='pwsh.exe';CommandLine='pwsh -File tools/scripts/test-unit.ps1'},
    [pscustomobject]@{ProcessId=15;ParentProcessId=0;Name='pwsh.exe';CommandLine='pwsh -File unrelated.ps1'}
)
$active = @(Get-DtmApiActiveTestProcess -ProcessSnapshot $snapshot -OwnerProcessId 11)
Assert-Route (($active.ProcessId -join ',') -eq '12,14') 'split suites detected; owning driver and unrelated processes excluded'
$active = @(Get-DtmApiActiveTestProcess -ProcessSnapshot $snapshot -OwnerProcessId 11 -IncludeGame)
Assert-Route (($active.ProcessId -join ',') -eq '12,13,14') 'game detected even without a readable command line'

New-Item -ItemType Directory -Path $scripts -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'test.ps1') -Destination (Join-Path $scripts 'test.ps1')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'test-common.ps1') -Destination (Join-Path $scripts 'test-common.ps1')
Add-Content -LiteralPath (Join-Path $scripts 'test-common.ps1') -Value @'
function Get-DtmApiActiveTestProcess {
    param([switch] $IncludeGame)
    if (Test-Path (Join-Path $PSScriptRoot 'active')) { [pscustomobject]@{ProcessId=1234} }
}
'@
Write-Fixture (Join-Path $scripts 'common.ps1') @'
function Get-RepoRoot { [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')) }
function Get-DotNetExe { param($RepoRoot) Join-Path $PSScriptRoot 'forbidden-dotnet.ps1' }
function Test-DtmApiPathIsSameOrChild {
    param($Child, $Parent)
    [IO.Path]::GetFullPath($Child).StartsWith([IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)
}
'@
Write-Fixture (Join-Path $scripts 'release-common.ps1') '# No release mutation helpers in this fixture.'
Write-Fixture (Join-Path $scripts 'author-sdk-release-common.ps1') 'function Get-AuthorSdkReleaseVersion { param($RepoRoot) "fixture" }'
Write-Fixture (Join-Path $scripts 'forbidden-dotnet.ps1') "throw 'Unexpected compiler execution.'"
$preflight = @'
param([switch] $Check)
if (-not $Check) { throw 'Preflight must be read-only.' }
Add-Content -LiteralPath (Join-Path $PSScriptRoot 'trace') -Value 'preflight'
if (Test-Path (Join-Path $PSScriptRoot 'stale')) { throw 'Stale evidence index.' }
'@
Write-Fixture (Join-Path $scripts 'build-evidence-retention-allowlist.ps1') $preflight
$firstCommands = [ordered]@{
    BuildAndUnit = 'build.ps1'
    RuntimeInstaller = 'test-runtime-build-source.ps1'
    ScriptContracts = 'test-runtime-evidence-retention.ps1'
    AuthorSdk = 'build-author-sdk.ps1'
    InstallerMatrices = 'test-player-runtime-only-uninstall.ps1'
    ProductContracts = 'check-product-catalog.ps1'
    QaLifecycle = 'check-batch4-qa-semantic-boundary.ps1'
    RetainedAbi = 'test-synthetic-retained-abi.ps1'
}
foreach ($name in $firstCommands.Values) {
    Write-Fixture (Join-Path $scripts $name) @'
Add-Content -LiteralPath (Join-Path $PSScriptRoot 'trace') -Value $MyInvocation.MyCommand.Name
exit 7
'@
}
foreach ($name in @('check-test-artifact-governance.ps1', 'check-doc-governance.ps1')) {
    Write-Fixture (Join-Path $scripts $name) @'
Add-Content -LiteralPath (Join-Path $PSScriptRoot 'trace') -Value $MyInvocation.MyCommand.Name
exit 0
'@
}
function Invoke-Route([string[]] $Arguments, [bool] $Success, [string] $Trace) {
    $tracePath = Join-Path $scripts 'trace'
    Write-Fixture $tracePath ''
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = (& $shell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $scripts 'test.ps1') @Arguments 2>&1 | Out-String)
        $code = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    $actual = @(Get-Content -LiteralPath $tracePath | Where-Object { $_ }) -join ','
    Assert-Route (($code -eq 0) -eq $Success -and $actual -eq $Trace) ("$Arguments exit=$code; trace=$actual; output=$output")
    return $output
}
$priorFilters = @{}
foreach ($key in @([Environment]::GetEnvironmentVariables('Process').Keys)) {
    if ($key -like 'DTMAPI_*_TEST_FOCUS') {
        $priorFilters[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $null, 'Process')
    }
}
try {
    $listing = Invoke-Route -Arguments @('-List') -Success $true -Trace '' | ConvertFrom-Json
    Assert-Route ($listing.CompleteRun -and $listing.Stages.Count -eq 9) 'default selection retains all stages'
    $tail = Invoke-Route -Arguments @('-StartAt','PostRuntimeInstaller','-List') -Success $true -Trace '' | ConvertFrom-Json
    Assert-Route (-not $tail.CompleteRun -and $tail.Stages[0] -eq 'ScriptContracts') 'legacy diagnostic tail remains partial'
    $tail = Invoke-Route -Arguments @('-StartAt','retainedabi','-List') -Success $true -Trace '' | ConvertFrom-Json
    Assert-Route (($tail.Stages -join ',') -eq 'RetainedAbi,InstallerMatrices,Governance') 'case-insensitive named tail preserves stage order'
    foreach ($arguments in @(@('-Stage','unknown'), @('-Stage',' '), @('-StartAt','unknown'), @('-Stage','Governance','-StartAt','RetainedAbi'))) {
        Invoke-Route -Arguments $arguments -Success $false -Trace '' | Out-Null
    }
    Write-Fixture (Join-Path $scripts 'active') 'fixture'
    Invoke-Route -Arguments @() -Success $false -Trace '' | Out-Null
    Invoke-Route -Arguments @('-List') -Success $true -Trace '' | Out-Null
    Remove-Item -LiteralPath (Join-Path $scripts 'active')
    Write-Fixture (Join-Path $scripts 'stale') 'fixture'
    Invoke-Route -Arguments @() -Success $false -Trace 'preflight' | Out-Null
    Remove-Item -LiteralPath (Join-Path $scripts 'stale')
    foreach ($entry in $firstCommands.GetEnumerator()) {
        $prefix = if ($entry.Key -eq 'ScriptContracts') { 'preflight,' } else { '' }
        Invoke-Route -Arguments @('-Stage', $entry.Key) -Success $false -Trace ($prefix + $entry.Value) | Out-Null
    }
    Invoke-Route -Arguments @() -Success $false -Trace 'preflight,build.ps1' | Out-Null
    foreach ($selector in @('-Stage','-StartAt')) {
        $output = Invoke-Route -Arguments @($selector,'Governance') -Success $true -Trace 'check-test-artifact-governance.ps1,check-doc-governance.ps1'
        Assert-Route ($output -match 'Diagnostic selection passed' -and $output -notmatch 'Complete test sequence passed') 'partial success does not claim complete acceptance'
    }
    Write-Fixture (Join-Path $root 'result.json') (@{Passed=$true;Checks=$checks;RealWindowsPowerShellChildren=$true;GameStarted=$false;BuildStarted=$false} | ConvertTo-Json)
    Write-Host "Release routing: $checks checks passed. Result: $root/result.json"
}
finally {
    foreach ($key in $priorFilters.Keys) { [Environment]::SetEnvironmentVariable($key, $priorFilters[$key], 'Process') }
}
