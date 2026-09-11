param(
    [string] $Configuration = 'Release',
    [AllowEmptyString()] [string] $Focus,
    [switch] $List,
    [switch] $NoBuild,
    [switch] $BuildOnly
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\test-common.ps1"
$repo = Get-RepoRoot
$mapPath = Join-Path $repo 'tests\DTMAPI.UnitTests\suites.json'
$map = Get-Content -LiteralPath $mapPath -Raw | ConvertFrom-Json
if ($map.schemaVersion -ne 1) { throw "Unsupported Unit suite map: $mapPath" }
if ($NoBuild -and $BuildOnly) { throw '-NoBuild and -BuildOnly cannot be combined.' }

# Validate before SDK lookup, build, dependency preparation, or test-session creation.
$explicitFocus = $PSBoundParameters.ContainsKey('Focus')
$requested = if ($explicitFocus) { $Focus } else { [Environment]::GetEnvironmentVariable('DTMAPI_UNIT_TEST_FOCUS', 'Process') }
$hasFocus = $explicitFocus -or -not [string]::IsNullOrEmpty($requested)
if ($hasFocus -and [string]::IsNullOrWhiteSpace($requested)) {
    throw "Unknown DTMAPI Unit test focus: $requested. Default tests were not run."
}
$selected = @()
foreach ($suite in $map.suites) {
    $calls = @()
    if (-not $hasFocus -or $suite.id -ieq $requested) { $calls = @($suite.default) }
    else {
        $route = @($suite.focuses.PSObject.Properties | Where-Object { $_.Name -ieq $requested })
        if ($route.Count -eq 1) { $calls = @($route[0].Value) }
    }
    if ($calls.Count -gt 0) { $selected += [pscustomobject]@{ Suite = $suite; Calls = $calls } }
}
if ($hasFocus -and $selected.Count -eq 0) {
    throw "Unknown DTMAPI Unit test focus: $requested. Default tests were not run."
}
if ($List) {
    if ($hasFocus) {
        foreach ($row in $selected) {
            [pscustomobject]@{ Focus = $requested; Suite = $row.Suite.id; Entrypoints = $row.Calls.Count; Project = $row.Suite.project }
        }
    }
    else {
        foreach ($suite in $map.suites) {
            [pscustomobject]@{ Suite = $suite.id; DefaultEntrypoints = @($suite.default).Count; Focus = @($suite.focuses.PSObject.Properties.Name) -join ', '; Project = $suite.project }
        }
    }
    exit 0
}
if ($selected.Count -eq 0) { throw 'The Unit suite map has no default tests.' }
$isWindowsHost = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
if (-not $isWindowsHost -and @($selected | Where-Object { $_.Suite.requiresWindows }).Count -gt 0) {
    throw 'The selected suite includes Windows/.NET Framework fixtures. Use a Windows host or select a supported suite explicitly.'
}

Write-Host ("Unit selection: {0}; {1} projects; {2} registered entrypoints" -f $(if ($hasFocus) { $requested } else { 'default' }), $selected.Count, ($selected | ForEach-Object { $_.Calls.Count } | Measure-Object -Sum).Sum)
foreach ($row in $selected) { Write-Host ("  {0}: {1}" -f $row.Suite.id, $row.Suite.project) }
if ($NoBuild) {
    foreach ($row in $selected) {
        $projectDirectory = Split-Path (Join-Path $repo $row.Suite.project)
        $binary = Join-Path $projectDirectory "bin\$Configuration\net8.0\DTMAPI.UnitTests.dll"
        if (-not (Test-Path -LiteralPath $binary -PathType Leaf)) {
            $focusCommand = if ($hasFocus) { " -Focus $requested" } else { '' }
            throw "Unit suite output missing: $binary. Build and run it with: powershell -NoProfile -File tools/scripts/test-unit.ps1 -Configuration $Configuration$focusCommand"
        }
    }
}
$dotnet = Get-DotNetExe -RepoRoot $repo -NoProvision:$NoBuild

if (-not $NoBuild) {
    $dependencies = @($selected | ForEach-Object { $_.Suite.dependencies } | Where-Object { $_ } | Sort-Object -Unique)
    if ($dependencies.Count -gt 0) {
        & "$PSScriptRoot\prepare-unit-test-dependencies.ps1" -Dependency $dependencies
        if ($LASTEXITCODE -ne 0) { throw 'Unit fixture dependency preparation failed.' }
    }
    Invoke-DtmApiProjectBuild -RepoRoot $repo -DotNetExe $dotnet -Projects @($selected | ForEach-Object { $_.Suite.project }) -Configuration $Configuration
}
if ($BuildOnly) { exit 0 }

# -NoBuild runs existing binaries directly; dotnet run/MSBuild are never invoked.
foreach ($row in $selected) {
    $projectDirectory = Split-Path (Join-Path $repo $row.Suite.project)
    $binary = Join-Path $projectDirectory "bin\$Configuration\net8.0\DTMAPI.UnitTests.dll"
    if (-not (Test-Path -LiteralPath $binary -PathType Leaf)) {
        $focusCommand = if ($hasFocus) { " -Focus $requested" } else { '' }
        throw "Unit suite output missing: $binary. Build and run it with: powershell -NoProfile -File tools/scripts/test-unit.ps1 -Configuration $Configuration$focusCommand"
    }
}
$previousFocus = [Environment]::GetEnvironmentVariable('DTMAPI_UNIT_TEST_FOCUS', 'Process')
try {
    [Environment]::SetEnvironmentVariable('DTMAPI_UNIT_TEST_FOCUS', $null, 'Process')
    foreach ($row in $selected) {
        $projectDirectory = Split-Path (Join-Path $repo $row.Suite.project)
        $binary = Join-Path $projectDirectory "bin\$Configuration\net8.0\DTMAPI.UnitTests.dll"
        $arguments = @($binary)
        if ($hasFocus) { $arguments += @('--focus', $requested) }
        & $dotnet @arguments
        if ($LASTEXITCODE -ne 0) { throw "Unit suite failed: $($row.Suite.id)" }
    }
}
finally { [Environment]::SetEnvironmentVariable('DTMAPI_UNIT_TEST_FOCUS', $previousFocus, 'Process') }
Write-Host "Unit selection passed: $($selected.Count) projects."
exit 0
