param(
    [string] $Configuration = 'Release',
    [switch] $EntryGuardsOnly
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\test-common.ps1"
$repo = Get-RepoRoot
$shellExe = (Get-Process -Id $PID).Path
$suites = @(
    @{ Name = 'DTMAPI.UnitTests'; Variable = 'DTMAPI_UNIT_TEST_FOCUS'; Success = 'OK \(platform-sdk-targets\)' },
    @{ Name = 'DTMAPI.AuthorSdk.Tests'; Variable = 'DTMAPI_AUTHOR_SDK_TEST_FOCUS'; Success = 'OK \(platform-sdk-targets\)' },
    @{ Name = 'DTMAPI.InstallDoctor.Tests'; Variable = 'DTMAPI_DOCTOR_TEST_FOCUS'; Success = 'PASS.*PlatformSdkTargetsMatchPackageBytes' }
)
$previous = @{}
$processEnvironment = [Environment]::GetEnvironmentVariables('Process')
foreach ($name in @($processEnvironment.Keys | Where-Object { $_ -like 'DTMAPI_*_TEST_FOCUS' }) +
    @($suites.Variable) + @('DTMAPI_TEST_TEMP_ROOT', 'DTMAPI_KEEP_FAILED_TEST_TEMP')) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

function Invoke-ExpectedTestProcess {
    param([string] $File, [string[]] $Arguments, [int] $ExpectedExit, [string] $Pattern)
    $previousPreference = $ErrorActionPreference
    try {
        # Capture expected nonzero exits, including native stderr on WinPS 5.1.
        $ErrorActionPreference = 'Continue'
        $output = (& $File @Arguments 2>&1 | Out-String)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }
    if ($exitCode -ne $ExpectedExit -or $output -notmatch $Pattern) {
        throw "Unexpected test routing (exit $exitCode, expected $ExpectedExit / $Pattern): $output"
    }
}

try {
    foreach ($name in $previous.Keys) {
        if ($name -like 'DTMAPI_*_TEST_FOCUS') { [Environment]::SetEnvironmentVariable($name, $null, 'Process') }
    }
    Assert-DtmApiFullTestEnvironment
    foreach ($suite in $suites) {
        [Environment]::SetEnvironmentVariable($suite.Variable, 'platform-sdk-targets', 'Process')
        foreach ($entry in @('build.ps1', 'test.ps1')) {
            Invoke-ExpectedTestProcess -File $shellExe -Arguments @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot $entry)) `
                -ExpectedExit 1 -Pattern ('Full validation does not accept focus filters: ' + $suite.Variable)
        }
        [Environment]::SetEnvironmentVariable($suite.Variable, $null, 'Process')
    }
    Write-Output 'PASS: unfiltered environment accepted; both full entrypoints reject each ambient focus before build.'

    if (-not $EntryGuardsOnly) {
        $dotnet = Get-DotNetExe -RepoRoot $repo
        $env:DTMAPI_TEST_TEMP_ROOT = Join-Path $repo 'tmp\test-runs'
        $env:DTMAPI_KEEP_FAILED_TEST_TEMP = '0'
        foreach ($suite in $suites) {
            $binary = Join-Path $repo ("tests\{0}\bin\{1}\net8.0\{0}.dll" -f $suite.Name, $Configuration)
            if (-not (Test-Path -LiteralPath $binary -PathType Leaf)) {
                throw "Build the changed test project first: tests/$($suite.Name)/$($suite.Name).csproj"
            }
            foreach ($invalid in @('nonexistent-focus', ' ')) {
                [Environment]::SetEnvironmentVariable($suite.Variable, $invalid, 'Process')
                Invoke-ExpectedTestProcess -File $dotnet -Arguments @($binary) -ExpectedExit 1 -Pattern 'Unknown DTMAPI .* test focus: .*Default tests were not run'
            }
            [Environment]::SetEnvironmentVariable($suite.Variable, 'platform-sdk-targets', 'Process')
            Invoke-ExpectedTestProcess -File $dotnet -Arguments @($binary) -ExpectedExit 0 -Pattern $suite.Success
            if ($suite.Name -eq 'DTMAPI.AuthorSdk.Tests') {
                # An explicit valid CLI focus still overrides an invalid ambient value.
                [Environment]::SetEnvironmentVariable($suite.Variable, 'nonexistent-focus', 'Process')
                Invoke-ExpectedTestProcess -File $dotnet -Arguments @($binary, '--focus', 'platform-sdk-targets') -ExpectedExit 0 -Pattern $suite.Success
                [Environment]::SetEnvironmentVariable($suite.Variable, 'platform-sdk-targets', 'Process')
                Invoke-ExpectedTestProcess -File $dotnet -Arguments @($binary, '--focus', 'nonexistent-focus') -ExpectedExit 1 -Pattern 'Unknown DTMAPI Author SDK test focus'
                Invoke-ExpectedTestProcess -File $dotnet -Arguments @($binary, '--focus', '') -ExpectedExit 1 -Pattern 'Unknown DTMAPI Author SDK test (focus|argument)'
            }
            [Environment]::SetEnvironmentVariable($suite.Variable, $null, 'Process')
            Write-Output "PASS: $($suite.Name) accepts its existing focus and rejects unknown/whitespace focus without default-suite fallback."
        }
    }
}
finally {
    foreach ($entry in $previous.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, 'Process')
    }
}
exit 0
