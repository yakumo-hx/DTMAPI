param(
    [string] $Configuration = 'Release'
)

. "$PSScriptRoot\common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
$installerPath = Join-Path $PSScriptRoot 'install-to-game.ps1'
$statusPath = Join-Path $PSScriptRoot 'check-dtmapi-status.ps1'
$testRoot = if ([string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    Join-Path $repo 'tmp\test-runs'
}
else {
    $env:DTMAPI_TEST_TEMP_ROOT
}
$testRoot = [System.IO.Path]::GetFullPath($testRoot)
$sessionRoot = [System.IO.Path]::GetFullPath((Join-Path $testRoot ('installer-invalid-target-' + [Guid]::NewGuid().ToString('N'))))
$testPrefix = $testRoot.TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
if (-not $sessionRoot.StartsWith($testPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Installer invalid-target test escaped the managed test root: $sessionRoot"
}
New-Item -ItemType Directory -Force -Path $sessionRoot | Out-Null

function Quote-InstallerTestArgument {
    param([Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Value)

    if ($Value.Contains('"')) {
        throw "Installer invalid-target test argument contains a quote: $Value"
    }
    return '"' + $Value + '"'
}

function Invoke-InvalidTargetCase {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $ExpectedMessage,
        [string[]] $AdditionalArguments = @()
    )

    $hostExecutable = (Get-Process -Id $PID).Path
    $arguments = @(
        '-NoLogo',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $installerPath,
        '-Configuration',
        $Configuration,
        '-SkipBuild',
        '-SkipOfficialLocalMods',
        '-PackagePayloadRoot',
        (Join-Path $sessionRoot 'unused-payload')
    )
    $arguments += @($AdditionalArguments)
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $hostExecutable
    $start.WorkingDirectory = $repo
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = (@($arguments | ForEach-Object { Quote-InstallerTestArgument -Value $_ }) -join ' ')
    $start.EnvironmentVariables['DTMAPI_GAME_DIR'] = $GameDir
    $start.EnvironmentVariables.Remove('DTMAPI_STATE_DIR') | Out-Null

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) {
            throw "Could not start installer invalid-target case: $Name"
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.Result
        $stderr = $stderrTask.Result
        $exitCode = $process.ExitCode
    }
    finally {
        $process.Dispose()
    }

    $combined = $stdout + [Environment]::NewLine + $stderr
    [System.IO.File]::WriteAllText((Join-Path $sessionRoot ($Name + '.out.txt')), $combined, (New-Object System.Text.UTF8Encoding($false)))
    if ($exitCode -eq 0) {
        throw "Installer invalid-target case unexpectedly succeeded: $Name"
    }
    if ($combined.IndexOf('DTMAPI install failed before the game folder was resolved:', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
        $combined.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Installer invalid-target case missed its friendly original error: $Name output=$combined"
    }
    foreach ($forbidden in @(
        'DtmRuntimeInstallTransaction',
        'cannot be retrieved because it has not been set',
        'The variable cannot be validated because the value',
        'InvalidOperation:'
    )) {
        if ($combined.IndexOf($forbidden, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Installer invalid-target case exposed a secondary trap/StrictMode failure '$forbidden': $Name output=$combined"
        }
    }
}

function Invoke-InvalidStatusTargetCase {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $ExpectedMessage
    )

    $hostExecutable = (Get-Process -Id $PID).Path
    $arguments = @(
        '-NoLogo',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $statusPath
    )
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $hostExecutable
    $start.WorkingDirectory = $repo
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = (@($arguments | ForEach-Object { Quote-InstallerTestArgument -Value $_ }) -join ' ')
    $start.EnvironmentVariables['DTMAPI_GAME_DIR'] = $GameDir
    $start.EnvironmentVariables.Remove('DTMAPI_STATE_DIR') | Out-Null

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) {
            throw "Could not start status invalid-target case: $Name"
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.Result
        $stderr = $stderrTask.Result
        $exitCode = $process.ExitCode
    }
    finally {
        $process.Dispose()
    }

    $combined = $stdout + [Environment]::NewLine + $stderr
    [System.IO.File]::WriteAllText((Join-Path $sessionRoot ($Name + '.out.txt')), $combined, (New-Object System.Text.UTF8Encoding($false)))
    if ($exitCode -eq 0) {
        throw "Status invalid-target case unexpectedly succeeded: $Name"
    }
    if ($combined.IndexOf('[INVALID] Doloc Town game folder could not be resolved or validated.', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
        $combined.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
        $combined.IndexOf('Set DTMAPI_GAME_DIR to the folder containing DolocTown.exe and DolocTown_Data', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Status invalid-target case missed its broad friendly diagnostic: $Name output=$combined"
    }
    foreach ($forbidden in @(
        'InvalidOperation:',
        'Exception:',
        'common.ps1:',
        'check-dtmapi-status.ps1:'
    )) {
        if ($combined.IndexOf($forbidden, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Status invalid-target case exposed a raw PowerShell failure '$forbidden': $Name output=$combined"
        }
    }
}

try {
    $missingGameDir = Join-Path $sessionRoot 'Missing Doloc Town 中文'
    $emptyGameDir = Join-Path $sessionRoot 'Empty Doloc Town 中文'
    New-Item -ItemType Directory -Force -Path $emptyGameDir | Out-Null

    Invoke-InvalidTargetCase `
        -Name 'missing-game-dir' `
        -GameDir $missingGameDir `
        -ExpectedMessage 'DTMAPI_GAME_DIR is set but does not exist'
    Invoke-InvalidTargetCase `
        -Name 'empty-game-dir' `
        -GameDir $emptyGameDir `
        -ExpectedMessage 'does not point to a valid Doloc Town game folder'
    Invoke-InvalidTargetCase `
        -Name 'conflicting-published-and-all-dev-options' `
        -GameDir $missingGameDir `
        -ExpectedMessage 'Use only one of -InstallPublishedModsOnly or -InstallAllDevOfficialMods.' `
        -AdditionalArguments @('-InstallPublishedModsOnly', '-InstallAllDevOfficialMods')
    Invoke-InvalidTargetCase `
        -Name 'conflicting-published-and-qa-options' `
        -GameDir $missingGameDir `
        -ExpectedMessage 'QA fixtures are developer-only. Do not combine -InstallPublishedModsOnly with -InstallQaFixtures.' `
        -AdditionalArguments @('-InstallPublishedModsOnly', '-InstallQaFixtures')

    Invoke-InvalidStatusTargetCase `
        -Name 'status-missing-game-dir' `
        -GameDir $missingGameDir `
        -ExpectedMessage 'DTMAPI_GAME_DIR is set but does not exist'
    Invoke-InvalidStatusTargetCase `
        -Name 'status-empty-game-dir' `
        -GameDir $emptyGameDir `
        -ExpectedMessage 'does not point to a valid Doloc Town game folder'

    Write-Host 'Installer/status friendly-failure tests OK: installer-target=2 installer-option-conflict=2 status-target=2.'
}
finally {
    if (Test-Path -LiteralPath $sessionRoot -PathType Container) {
        Remove-Item -LiteralPath $sessionRoot -Recurse -Force
    }
}
