param(
    [ValidateSet('Release')] [string] $Configuration = 'Release',
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\test-common.ps1"
Assert-DtmApiFullTestEnvironment
$repo = Get-RepoRoot
$map = Get-Content -LiteralPath (Join-Path $repo 'tests\DTMAPI.UnitTests\suites.json') -Raw | ConvertFrom-Json
$unitSuites = @($map.suites | Where-Object { @($_.default).Count -gt 0 })
$nonPublic = @($unitSuites | Where-Object { -not $_.publicSource })
if ($nonPublic.Count -gt 0) { throw "Default Unit suites require non-public inputs: $($nonPublic.id -join ', '). Update the source-test boundary explicitly; do not silently skip them." }
$otherSuites = @('DTMAPI.QaUnitTests', 'DTMAPI.InstallDoctor.Tests', 'DTMAPI.MultiPlatformInstaller.Tests', 'DTMAPI.AuthorSdk.Tests')
$projects = @($unitSuites.project) + @($otherSuites | ForEach-Object { "tests/$_/$_.csproj" }) + @('tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj')
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'The public-source profile requires Windows for its PowerShell 5.1 and .NET Framework Harmony fixtures.'
}
$pythonCommand = Get-Command python -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
$nodeCommand = Get-Command node -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
$powershell = Get-DtmApiPowerShellHost -RequireWindowsPowerShell
if ($null -eq $pythonCommand -or $null -eq $nodeCommand) {
    throw 'The public-source profile requires Python 3 and Node.js for history, reverse comparison and native-map fixture tests. Prepare these runtimes before running this profile.'
}
Write-Host 'Public-source scope: all default Unit/product suites; QA, Doctor and installer source tests; SDK handshake, target and pack-build focuses; synthetic player, runner, reverse and history workflow tests; retained public symbol-map fixtures.'
Write-Host 'Outside this profile: exact private game/ABI inputs, live game evidence, historical package matrices, and full Release validation.'
if ($NoBuild) {
    foreach ($project in $projects) {
        $directory = Split-Path (Join-Path $repo $project)
        $assembly = if ($project -in $unitSuites.project) { 'DTMAPI.UnitTests' } else { [IO.Path]::GetFileNameWithoutExtension($project) }
        $binary = Join-Path $directory "bin\$Configuration\net8.0\$assembly.dll"
        if (-not (Test-Path -LiteralPath $binary -PathType Leaf)) { throw "Public-source output missing: $binary. Run tools/scripts/test-public-source.ps1 without -NoBuild first." }
    }
}
$dotnet = Get-DotNetExe -RepoRoot $repo -NoProvision:$NoBuild
$compatibilityOutput = Join-Path $repo '.tools/author-sdk-compatibility'
& "$PSScriptRoot/prepare-author-sdk-compatibility.ps1" -OutputRoot $compatibilityOutput -Check:$NoBuild
if (-not $?) { throw 'Public SDK compatibility preparation/check failed.' }
if (-not $NoBuild) {
    $dependencies = @($unitSuites | ForEach-Object { $_.dependencies } | Where-Object { $_ } | Sort-Object -Unique)
    if ($dependencies.Count -gt 0) {
        & "$PSScriptRoot\prepare-unit-test-dependencies.ps1" -Dependency $dependencies
        if ($LASTEXITCODE -ne 0) { throw 'Public Unit fixture preparation failed.' }
    }
    Invoke-DtmApiTestProjectBuild -RepoRoot $repo -DotNetExe $dotnet -Projects $projects -Configuration $Configuration
}

$previousTestRoot = $env:DTMAPI_TEST_TEMP_ROOT
$previousCompatibilityRoot = $env:DTMAPI_AUTHOR_COMPAT_ROOT
try {
    $env:DTMAPI_TEST_TEMP_ROOT = Join-Path $repo 'tmp\test-runs'
    $env:DTMAPI_AUTHOR_COMPAT_ROOT = Join-Path $compatibilityOutput '0.5.5'
    & "$PSScriptRoot\test-unit.ps1" -Configuration $Configuration -NoBuild
    if ($LASTEXITCODE -ne 0) { throw 'Public Unit source suites failed.' }
    foreach ($suite in $otherSuites) {
        $binary = Join-Path $repo "tests\$suite\bin\$Configuration\net8.0\$suite.dll"
        if ($suite -eq 'DTMAPI.AuthorSdk.Tests') {
            # The full SDK suite includes exact private Advanced reference inputs.
            # These explicit public focuses make the completed scope unambiguous.
            foreach ($focus in @('platform-session-handshake', 'platform-sdk-targets', 'pack-build', 'official-local')) {
                & $dotnet $binary --focus $focus
                if ($LASTEXITCODE -ne 0) { throw "Public SDK focus failed: $focus" }
            }
        }
        else {
            & $dotnet $binary
            if ($LASTEXITCODE -ne 0) { throw "Public source suite failed: $suite" }
        }
    }
    foreach ($script in @(
        'test-dotnet-toolchain.ps1',
        'test-unit-routing.ps1',
        'test-release-routing.ps1',
        'test-author-sdk-compatibility.ps1',
        'test-author-sdk-preparation.ps1',
        'test-workspace-preflight.ps1',
        'test-product-projections.ps1',
        'test-issue-index.ps1',
        'test-audit-output-boundaries.ps1',
        'test-document-archive-tools.ps1',
        'test-player-save-repair.ps1',
        'test-player-save-collector-slots.ps1',
        'test-player-save-crash-collector.ps1',
        'test-game-smoke-save-modes.ps1',
        'test-game-smoke-process-boundaries.ps1',
        'test-game-smoke-modules.ps1',
        'test-reverse-capture-stages.ps1',
        'test-reverse-baseline-path-safety.ps1',
        'test-portable-reverse-capture-path-safety.ps1'
    )) {
        & $powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $script)
        if ($LASTEXITCODE -ne 0) { throw "Public workflow test failed: $script" }
    }
    foreach ($script in @(
        'tools/workspace-history/test_history.py',
        'tools/workspace-history/test_links.py',
        'tools/reverse-capture/test_compare_baselines.py'
    )) {
        & $pythonCommand.Source (Join-Path $repo $script)
        if ($LASTEXITCODE -ne 0) { throw "Public Python fixture test failed: $script" }
    }
    & $nodeCommand.Source (Join-Path $repo 'tools/native-function-map/test_workbench.cjs')
    if ($LASTEXITCODE -ne 0) { throw 'Public native-map fixture tests failed.' }
    & "$PSScriptRoot\test-test-focus-routing.ps1" -Configuration $Configuration -EntryGuardsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Full-entry focus guard tests failed.' }
}
finally {
    $env:DTMAPI_TEST_TEMP_ROOT = $previousTestRoot
    $env:DTMAPI_AUTHOR_COMPAT_ROOT = $previousCompatibilityRoot
}
Write-Host 'PASS: the declared public-source profile completed. Full Release and live-game validation remain separate scopes.'
exit 0
