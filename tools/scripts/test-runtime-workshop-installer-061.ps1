param(
    [Parameter(Mandatory = $true)] [string] $PackageRoot,
    [switch] $KeepTemp
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-Installer061 {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )
    if (-not $Condition) { throw "Runtime Workshop 0.6.1 test failed: $Message" }
}

function Copy-Installer061Tree {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $Source -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }
}

function Invoke-Installer061Bat {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $OutputPath
    )
    $commandLine = 'call "{0}" < nul' -f $Path.Replace('"', '""')
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        # Several fixtures intentionally exercise a non-zero BAT result. PowerShell 7
        # projects native stderr as an ErrorRecord, so capture it without allowing the
        # script-wide Stop policy to abort before the fixture can assert the exit code.
        $ErrorActionPreference = 'Continue'
        $lines = @(& $env:ComSpec /d /e:off /v:off /s /c $commandLine 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    @($lines | ForEach-Object { [string]$_ }) | Set-Content -Encoding UTF8 -LiteralPath $OutputPath
    return [pscustomobject]@{
        ExitCode = $exitCode
        Text = (@($lines | ForEach-Object { [string]$_ }) -join "`n")
        OutputPath = $OutputPath
    }
}

function Start-Installer061BatProcess {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $OutputPath,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $env:ComSpec
    $psi.Arguments = '/d /e:off /v:off /c call "' + $Path.Replace('"', '""') + '" > "' + $OutputPath.Replace('"', '""') + '" 2>&1'
    $psi.WorkingDirectory = Split-Path -Parent $Path
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    if (-not $process.Start()) { throw "Could not start concurrent installer action: $Label" }
    return [pscustomobject]@{ Process = $process; OutputPath = $OutputPath; Label = $Label }
}

function Complete-Installer061BatProcess {
    param(
        [Parameter(Mandatory = $true)] $Handle,
        [int] $TimeoutMilliseconds = 120000
    )
    try {
        if (-not $Handle.Process.WaitForExit($TimeoutMilliseconds)) {
            try { $Handle.Process.Kill() } catch { }
            throw "Concurrent installer action timed out: $($Handle.Label)"
        }
        $text = if (Test-Path -LiteralPath $Handle.OutputPath -PathType Leaf) {
            Get-Content -LiteralPath $Handle.OutputPath -Raw
        }
        else { '' }
        return [pscustomobject]@{
            ExitCode = $Handle.Process.ExitCode
            Text = [string]$text
            OutputPath = $Handle.OutputPath
            Label = $Handle.Label
        }
    }
    finally {
        $Handle.Process.Dispose()
    }
}

function Get-Installer061ChildDirectorySet {
    param([string[]] $Roots)
    $set = @{}
    foreach ($root in @($Roots)) {
        if (Test-Path -LiteralPath $root -PathType Container) {
            foreach ($directory in @(Get-ChildItem -LiteralPath $root -Directory -Force -ErrorAction SilentlyContinue)) {
                $set[[System.IO.Path]::GetFullPath($directory.FullName)] = $true
            }
        }
    }
    return $set
}

function Assert-Installer061NoParserFailure {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Text
    )
    foreach ($pattern in @('ParserError', 'Missing script:', 'Host/parser error', 'No PowerShell host passed', 'At .*\.ps1:[0-9]+ char:')) {
        Assert-Installer061 -Condition ($Text -notmatch $pattern) -Message "$Label contained parser/host failure '$pattern'."
    }
}

$repo = Get-RepoRoot
$sourcePackage = [System.IO.Path]::GetFullPath($PackageRoot)
Assert-Installer061 -Condition (Test-Path -LiteralPath $sourcePackage -PathType Container) -Message "Package is missing: $sourcePackage"
$candidateManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sourcePackage 'Content\DTMAPI\release-manifest.json') | ConvertFrom-Json
$expectedVersionSummary = 'Runtime={0}; Binary={1}' -f $candidateManifest.DTMAPIVersion, $candidateManifest.BinaryVersion

$testBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo 'tmp\test-runs'))
}
$unicode = -join @([char]0x4E2D, [char]0x6587)
$testRoot = [System.IO.Path]::GetFullPath((Join-Path $testBase ('runtime-installer-061-' + [Guid]::NewGuid().ToString('N'))))
Assert-Installer061 -Condition (Test-DtmApiPathIsSameOrChild -Child $testRoot -Parent $testBase) -Message "Test root escaped managed root: $testRoot"

$libraryRoot = Join-Path $testRoot ('Program Files (x86);' + $unicode)
$package = Join-Path $libraryRoot 'steamapps\workshop\content\2285550\3743016467'
$gameInstallDirectory = 'Game (x86) & ' + $unicode + '; path'
$game = Join-Path $libraryRoot (Join-Path 'steamapps\common' $gameInstallDirectory)
$processTemp = Join-Path $testRoot ('Temp ' + $unicode)
$fakeProfile = Join-Path $testRoot ('Profile ' + $unicode)
$desktop = [Environment]::GetFolderPath('Desktop')
if ([string]::IsNullOrWhiteSpace($desktop)) { $desktop = Join-Path $env:USERPROFILE 'Desktop' }
$collectRoots = @((Join-Path $desktop 'DTMAPI-logs'), (Join-Path $processTemp 'DTMAPI-logs'))
$collectBefore = Get-Installer061ChildDirectorySet -Roots $collectRoots
$createdCollectDirectories = New-Object 'System.Collections.Generic.List[string]'
$environmentNames = @('DTMAPI_GAME_DIR', 'DTMAPI_RUNTIME_DIR', 'DTMAPI_STATE_DIR', 'DTMAPI_POWERSHELL_HOST', 'DTMAPI_NO_PAUSE', 'TEMP', 'TMP', 'USERPROFILE')
$environmentSnapshot = @{}
foreach ($name in $environmentNames) {
    $environmentSnapshot[$name] = [Environment]::GetEnvironmentVariable($name, [EnvironmentVariableTarget]::Process)
}

try {
    # The inference fixture must not inherit a maintainer's explicit game override.
    # Restore the captured value in the existing environment cleanup below.
    $env:DTMAPI_GAME_DIR = $null
    New-Item -ItemType Directory -Force -Path $testRoot, $processTemp, $fakeProfile | Out-Null
    Copy-Installer061Tree -Source $sourcePackage -Destination $package

    $expectedBats = @('1_install_dtmapi.bat', '2_uninstall_dtmapi.bat', '3_check_dtmapi_status.bat', '4_collect_dtmapi_logs.bat') | Sort-Object
    $actualBats = @(Get-ChildItem -LiteralPath $package -Filter '*.bat' -File | ForEach-Object Name | Sort-Object)
    Assert-Installer061 -Condition (($actualBats -join '|') -eq ($expectedBats -join '|')) -Message "Root BAT set is not exact. Actual=$($actualBats -join ',')"
    Assert-Installer061 -Condition (@(Get-ChildItem -LiteralPath $package -Filter '*.exe' -File -Recurse -Force).Count -eq 0) -Message 'Normal Runtime package contains an EXE.'
    Assert-Installer061 -Condition (-not (Test-Path -LiteralPath (Join-Path $package 'Content\DTMAPIInstaller\tools\probe-install-preflight.ps1'))) -Message 'Dormant install preflight remains packaged.'
    Assert-Installer061 -Condition (-not (Test-Path -LiteralPath (Join-Path $package 'Content\DTMAPIInstaller\tools\player-doctor'))) -Message 'Player Doctor remains packaged.'

    $toolsRoot = Join-Path $package 'Content\DTMAPIInstaller\tools'
    $toolScripts = @(Get-ChildItem -LiteralPath $toolsRoot -Filter '*.ps1' -File | ForEach-Object FullName)
    Test-DtmApiWindowsPowerShellSyntax -Paths $toolScripts -AllowCoreFallback
    foreach ($scriptPath in $toolScripts) {
        $tokens = $null
        $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors)
        Assert-Installer061 -Condition (@($errors).Count -eq 0) -Message "Packaged script parser error: $scriptPath"
        foreach ($command in @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true))) {
            $commandName = $command.GetCommandName()
            Assert-Installer061 -Condition ($commandName -notin @('Get-FileHash', 'Expand-Archive', 'Invoke-WebRequest')) -Message "Packaged script directly requires optional command '$commandName': $scriptPath"
        }
    }
    $commonSource = Get-Content -Raw -LiteralPath (Join-Path $toolsRoot 'common.ps1')
    Assert-Installer061 -Condition ($commonSource -notmatch '(?i)libraryfolders\.vdf|Microsoft\.Win32\.Registry|Registry::|HKCU:|HKLM:') -Message 'Packaged resolver still contains global Steam-library or Registry scanning.'
    Assert-Installer061 -Condition ($commonSource -match 'appmanifest_2285550\.acf') -Message 'Packaged resolver no longer contains the current-library appmanifest path.'

    New-Item -ItemType Directory -Force -Path (Join-Path $game 'DolocTown_Data') | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $game 'DolocTown.exe'), [System.Text.Encoding]::ASCII.GetBytes('fake-game'))
    $appManifest = Join-Path $libraryRoot 'steamapps\appmanifest_2285550.acf'
    [System.IO.File]::WriteAllLines($appManifest, @(
        '"AppState"',
        '{',
        '    "appid" "2285550"',
        ('    "installdir" "{0}"' -f $gameInstallDirectory),
        '}'
    ), [System.Text.Encoding]::UTF8)
    $inferredGame = Resolve-DolocTownGamePath -RepoRoot (Join-Path $package 'Content')
    Assert-Installer061 -Condition ([string]::Equals([System.IO.Path]::GetFullPath($inferredGame), [System.IO.Path]::GetFullPath($game), [System.StringComparison]::OrdinalIgnoreCase)) -Message 'Workshop-library/appmanifest game-path inference failed.'
    $externalPlugin = Join-Path $game 'BepInEx\plugins\External.Owner\keep.dll'
    $externalConfig = Join-Path $game 'BepInEx\config\external-owner.cfg'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $externalPlugin), (Split-Path -Parent $externalConfig) | Out-Null
    [System.IO.File]::WriteAllBytes($externalPlugin, [System.Text.Encoding]::ASCII.GetBytes('external-plugin-sentinel'))
    [System.IO.File]::WriteAllBytes($externalConfig, [System.Text.Encoding]::ASCII.GetBytes('external-config-sentinel'))
    $externalPluginHash = Get-DtmApiFileSha256 -Path $externalPlugin
    $externalConfigHash = Get-DtmApiFileSha256 -Path $externalConfig

    $partialCore = Join-Path $game 'BepInEx\core\BepInEx.dll'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $partialCore) | Out-Null
    [System.IO.File]::WriteAllBytes($partialCore, [System.Text.Encoding]::ASCII.GetBytes('partial-core-sentinel'))
    $partialCoreHash = Get-DtmApiFileSha256 -Path $partialCore
    $blockingTarget = Join-Path $game 'winhttp.dll'
    New-Item -ItemType Directory -Force -Path $blockingTarget | Out-Null

    $windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    Assert-Installer061 -Condition (Test-Path -LiteralPath $windowsPowerShell -PathType Leaf) -Message 'Windows PowerShell 5.1 is unavailable.'
    $explicitPathHosts = New-Object 'System.Collections.Generic.List[string]'
    $explicitPathHosts.Add($windowsPowerShell) | Out-Null
    $pwsh = Get-Command pwsh.exe -ErrorAction SilentlyContinue
    if ($pwsh -and (Test-Path -LiteralPath $pwsh.Source -PathType Leaf)) { $explicitPathHosts.Add($pwsh.Source) | Out-Null }
    foreach ($hostExe in @($explicitPathHosts.ToArray() | Sort-Object -Unique)) {
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $invalidPathOutput = @(& $hostExe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $toolsRoot 'check-dtmapi-status.ps1') -GameDir 'C:\invalid|game:path' 2>&1 | ForEach-Object { [string]$_ })
            $invalidPathExit = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousPreference
        }
        Assert-Installer061 -Condition ($invalidPathExit -eq 1 -and ($invalidPathOutput -join "`n") -match 'DTM-E1002') -Message "Invalid explicit game path was not converted to DTM-E1002 under $hostExe. Output=$($invalidPathOutput -join ' | ')"
    }
    $env:DTMAPI_GAME_DIR = $game
    $env:DTMAPI_RUNTIME_DIR = $null
    $env:DTMAPI_STATE_DIR = $null
    $env:DTMAPI_POWERSHELL_HOST = $windowsPowerShell
    $env:DTMAPI_NO_PAUSE = '1'
    $env:TEMP = $processTemp
    $env:TMP = $processTemp
    $env:USERPROFILE = $fakeProfile

    $env:DTMAPI_POWERSHELL_HOST = $env:ComSpec
    $fakeHostProbe = Invoke-Installer061Bat -Path (Join-Path $package '3_check_dtmapi_status.bat') -OutputPath (Join-Path $testRoot 'fake-host-proof.txt')
    Assert-Installer061 -Condition ($fakeHostProbe.ExitCode -ne 0 -and $fakeHostProbe.Text -match 'returned success without the required invocation proof' -and $fakeHostProbe.Text -match 'No PowerShell host passed') -Message "A false-success host was not rejected. See $($fakeHostProbe.OutputPath)"
    Assert-Installer061 -Condition (-not (Test-Path -LiteralPath (Join-Path $game 'DTMAPI\install-state.json'))) -Message 'False-success host probe mutated install state.'
    $env:DTMAPI_POWERSHELL_HOST = $windowsPowerShell

    $probePressureBefore = Get-Installer061ChildDirectorySet -Roots $collectRoots
    $heldMutationLock = Enter-DtmApiInstallerMutationLock -GameDir $game -Operation 'test-held-lock'
    try {
        $pressureHandles = New-Object 'System.Collections.Generic.List[object]'
        $pressureActions = @(
            [pscustomobject]@{ Bat = '3_check_dtmapi_status.bat'; Kind = 'status' },
            [pscustomobject]@{ Bat = '4_collect_dtmapi_logs.bat'; Kind = 'collect' },
            [pscustomobject]@{ Bat = '1_install_dtmapi.bat'; Kind = 'install' },
            [pscustomobject]@{ Bat = '2_uninstall_dtmapi.bat'; Kind = 'uninstall' }
        )
        foreach ($index in 0..23) {
            $action = $pressureActions[$index % $pressureActions.Count]
            $label = 'probe-pressure-{0:00}-{1}' -f $index, $action.Kind
            $output = Join-Path $testRoot ($label + '.txt')
            $pressureHandles.Add((Start-Installer061BatProcess -Path (Join-Path $package $action.Bat) -OutputPath $output -Label $label)) | Out-Null
        }
        $pressureResults = @($pressureHandles | ForEach-Object { Complete-Installer061BatProcess -Handle $_ })
        foreach ($result in $pressureResults) {
            Assert-Installer061 -Condition ($result.Text -match 'Using PowerShell host:') -Message "$($result.Label) did not select the real PowerShell host. See $($result.OutputPath)"
            Assert-Installer061 -Condition ($result.Text -notmatch 'No PowerShell host passed|without the required invocation proof|Could not allocate a unique PowerShell probe session') -Message "$($result.Label) reported a probe-session collision. See $($result.OutputPath)"
            if ($result.Label -match '-collect$') {
                Assert-Installer061 -Condition ($result.ExitCode -eq 0) -Message "$($result.Label) collect action failed after host selection. See $($result.OutputPath)"
            }
            else {
                Assert-Installer061 -Condition ($result.ExitCode -ne 0) -Message "$($result.Label) unexpectedly succeeded in the controlled missing/locked state. See $($result.OutputPath)"
            }
        }
        Assert-Installer061 -Condition (@(Get-ChildItem -LiteralPath $processTemp -Directory -Filter 'dtmapi-host-probe-*' -Force -ErrorAction SilentlyContinue).Count -eq 0) -Message 'Concurrent host probes left claimed session directories.'

        $probePressureAfter = Get-Installer061ChildDirectorySet -Roots $collectRoots
        foreach ($pressureCollectDirectory in @($probePressureAfter.Keys | Where-Object { -not $probePressureBefore.ContainsKey($_) })) {
            $underOwnedOutput = (Test-DtmApiPathIsSameOrChild -Child $pressureCollectDirectory -Parent $desktop) -or
                (Test-DtmApiPathIsSameOrChild -Child $pressureCollectDirectory -Parent $processTemp)
            if ($underOwnedOutput) { Remove-Item -LiteralPath $pressureCollectDirectory -Recurse -Force }
        }

        $blockedInstall = Invoke-Installer061Bat -Path (Join-Path $package '1_install_dtmapi.bat') -OutputPath (Join-Path $testRoot 'install-lock-blocked.txt')
    }
    finally {
        Exit-DtmApiInstallerMutationLock -Lock $heldMutationLock
    }
    Assert-Installer061 -Condition ($blockedInstall.ExitCode -ne 0 -and $blockedInstall.Text -match 'DTM-E1302' -and $blockedInstall.Text -match 'Another DTMAPI install or uninstall is already running for this game directory') -Message "Per-game mutation lock did not block a concurrent install with DTM-E1302. See $($blockedInstall.OutputPath)"
    Assert-Installer061 -Condition (@(Get-ChildItem -LiteralPath (Join-Path $game 'DTMAPI') -Filter 'install-state.failed-*.json' -File -ErrorAction SilentlyContinue).Count -eq 0) -Message 'Lock contention wrote a competing failure-state file.'

    $rollbackInstall = Invoke-Installer061Bat -Path (Join-Path $package '1_install_dtmapi.bat') -OutputPath (Join-Path $testRoot 'install-rollback.txt')
    Assert-Installer061 -Condition ($rollbackInstall.ExitCode -ne 0 -and $rollbackInstall.Text -match 'DTM-E1102' -and $rollbackInstall.Text -match 'BepInEx file target is occupied by a directory') -Message "BepInEx rollback fixture did not report its controlled local-file failure as DTM-E1102. See $($rollbackInstall.OutputPath)"
    Assert-Installer061 -Condition ((Get-DtmApiFileSha256 -Path $partialCore) -eq $partialCoreHash) -Message 'BepInEx rollback did not restore the overwritten partial core.'
    Assert-Installer061 -Condition ((Get-DtmApiFileSha256 -Path $externalPlugin) -eq $externalPluginHash -and (Get-DtmApiFileSha256 -Path $externalConfig) -eq $externalConfigHash) -Message 'BepInEx rollback changed external plugin/config files.'
    Remove-Item -LiteralPath $blockingTarget -Force

    $install = Invoke-Installer061Bat -Path (Join-Path $package '1_install_dtmapi.bat') -OutputPath (Join-Path $testRoot 'install.txt')
    Assert-Installer061NoParserFailure -Label 'install' -Text $install.Text
    Assert-Installer061 -Condition ($install.ExitCode -eq 0 -and $install.Text -match 'Version: 5\.1' -and $install.Text -match 'Portable ZIP extraction capability: OK' -and $install.Text -match 'DTM-S1001' -and $install.Text.Contains($expectedVersionSummary) -and $install.Text.Contains("DTMAPI $($candidateManifest.DTMAPIVersion) Runtime installed successfully.")) -Message "Windows PowerShell 5.1 install or final summary failed. Expected $expectedVersionSummary. See $($install.OutputPath)"
    Assert-Installer061 -Condition ((Get-DtmApiFileSha256 -Path $externalPlugin) -eq $externalPluginHash -and (Get-DtmApiFileSha256 -Path $externalConfig) -eq $externalConfigHash) -Message 'Successful BepInEx repair changed external plugin/config files.'
    Assert-Installer061 -Condition ((Get-Item -LiteralPath $partialCore).Length -gt 'partial-core-sentinel'.Length) -Message 'Successful BepInEx install did not repair the partial core.'
    $bepSummary = @(Get-ChildItem -LiteralPath (Join-Path $game 'DTMAPI\backups') -Filter 'install-summary.txt' -File -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
    Assert-Installer061 -Condition ($bepSummary.Count -eq 1 -and (Get-Content -Raw -LiteralPath $bepSummary[0].FullName) -match 'SourceKind: bundled-package') -Message 'BepInEx did not prefer the bundled fixed-hash ZIP.'
    Assert-Installer061 -Condition (-not (Test-Path -LiteralPath (Join-Path $game 'DTMAPI\tools\player-doctor'))) -Message 'Installed Runtime contains Player Doctor.'
    $installState = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $game 'DTMAPI\install-state.json') | ConvertFrom-Json
    Assert-Installer061 -Condition (@($installState.FilesInstalled | Where-Object { [string]$_.Kind -like 'player-doctor*' }).Count -eq 0) -Message 'Install state contains Player Doctor receipts.'

    $status = Invoke-Installer061Bat -Path (Join-Path $package '3_check_dtmapi_status.bat') -OutputPath (Join-Path $testRoot 'status.txt')
    Assert-Installer061NoParserFailure -Label 'status' -Text $status.Text
    Assert-Installer061 -Condition ($status.ExitCode -eq 0 -and $status.Text -match 'normal Runtime package intentionally contains no EXE' -and $status.Text -match 'DTM-S3001' -and $status.Text -match 'INSTALLED_HEALTHY') -Message "Clean no-EXE status or final summary failed. See $($status.OutputPath)"

    $logRoot = Join-Path $game 'DTMAPI\logs'
    New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $logRoot 'latest.log'), (New-Object byte[] (5MB)))
    (Get-Item -LiteralPath (Join-Path $logRoot 'latest.log')).LastWriteTimeUtc = [datetime]::UtcNow.AddMinutes(20)
    foreach ($index in 1..11) {
        $historyPath = Join-Path $logRoot ('latest-20260807-130{0}0.log' -f $index)
        $historyBytes = New-Object byte[] (4096 + $index)
        $historyBytes[0] = [byte]$index
        [System.IO.File]::WriteAllBytes($historyPath, $historyBytes)
        (Get-Item -LiteralPath $historyPath).LastWriteTimeUtc = [datetime]::UtcNow.AddMinutes($index)
    }
    $crashRoot = Join-Path $processTemp 'RedSawGames\DolocTown\Crashes\Crash_2026-08-07_130000'
    New-Item -ItemType Directory -Force -Path $crashRoot | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $crashRoot 'crash.dmp'), (New-Object byte[] (1MB)))
    [System.IO.File]::WriteAllText((Join-Path $crashRoot 'error.log'), 'controlled crash text')

    $lockedLatestStream = [System.IO.File]::Open(
        (Join-Path $logRoot 'latest.log'),
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::ReadWrite,
        [System.IO.FileShare]::None)
    try {
        $lockedCollect = Invoke-Installer061Bat -Path (Join-Path $package '4_collect_dtmapi_logs.bat') -OutputPath (Join-Path $testRoot 'collect-locked-source.txt')
    }
    finally {
        $lockedLatestStream.Dispose()
    }
    Assert-Installer061 -Condition ($lockedCollect.ExitCode -ne 0 -and $lockedCollect.Text -match 'Could not export a complete, stable copy') -Message "A locked selected log did not fail the public collection. See $($lockedCollect.OutputPath)"
    $collectAfterLockedFailure = Get-Installer061ChildDirectorySet -Roots $collectRoots
    $publishedAfterLockedFailure = @($collectAfterLockedFailure.Keys | Where-Object { -not $collectBefore.ContainsKey($_) })
    Assert-Installer061 -Condition ($publishedAfterLockedFailure.Count -eq 0) -Message 'Failed stable-log collection left a published final or partial directory.'

    $collect = Invoke-Installer061Bat -Path (Join-Path $package '4_collect_dtmapi_logs.bat') -OutputPath (Join-Path $testRoot 'collect.txt')
    Assert-Installer061NoParserFailure -Label 'collect' -Text $collect.Text
    Assert-Installer061 -Condition ($collect.ExitCode -eq 0) -Message "Log collection failed. See $($collect.OutputPath)"
    $collectSecond = Invoke-Installer061Bat -Path (Join-Path $package '4_collect_dtmapi_logs.bat') -OutputPath (Join-Path $testRoot 'collect-second.txt')
    Assert-Installer061NoParserFailure -Label 'second collect' -Text $collectSecond.Text
    Assert-Installer061 -Condition ($collectSecond.ExitCode -eq 0) -Message "Second log collection failed. See $($collectSecond.OutputPath)"
    $collectAfter = Get-Installer061ChildDirectorySet -Roots $collectRoots
    $newCollectDirectories = @($collectAfter.Keys | Where-Object { -not $collectBefore.ContainsKey($_) })
    Assert-Installer061 -Condition ($newCollectDirectories.Count -eq 2) -Message "Expected two unique log directories; found $($newCollectDirectories.Count)."
    foreach ($directory in $newCollectDirectories) {
        $createdCollectDirectory = [System.IO.Path]::GetFullPath([string]$directory)
        $createdCollectDirectories.Add($createdCollectDirectory) | Out-Null
        Assert-Installer061 -Condition ((Split-Path -Leaf $createdCollectDirectory) -notlike '*.partial') -Message 'A partial staging directory was published as a completed log bundle.'
        $collectedLatest = Join-Path $createdCollectDirectory 'DTMAPI-latest.log'
        Assert-Installer061 -Condition ((Get-Item -LiteralPath $collectedLatest).Length -eq 5MB -and (Get-DtmApiFileSha256 -Path $collectedLatest) -eq (Get-DtmApiFileSha256 -Path (Join-Path $logRoot 'latest.log'))) -Message 'Current DTMAPI log was truncated or changed during export.'
        $collectedHistory = @(Get-ChildItem -LiteralPath (Join-Path $createdCollectDirectory 'DTMAPI-log-history') -Filter '*.log' -File)
        Assert-Installer061 -Condition ($collectedHistory.Count -eq 9) -Message 'The public bundle did not contain exactly the other nine logs from the newest-ten set.'
        foreach ($index in 3..11) {
            $sourceHistory = Join-Path $logRoot ('latest-20260807-130{0}0.log' -f $index)
            $copiedHistory = Join-Path (Join-Path $createdCollectDirectory 'DTMAPI-log-history') (Split-Path -Leaf $sourceHistory)
            Assert-Installer061 -Condition ((Get-DtmApiFileSha256 -Path $copiedHistory) -eq (Get-DtmApiFileSha256 -Path $sourceHistory)) -Message "History log $index was not copied in full."
        }
        Assert-Installer061 -Condition (-not (Test-Path -LiteralPath (Join-Path $createdCollectDirectory 'DTMAPI-log-history\latest-20260807-13010.log')) -and -not (Test-Path -LiteralPath (Join-Path $createdCollectDirectory 'DTMAPI-log-history\latest-20260807-13020.log'))) -Message 'Older logs outside the newest-ten set were exported.'
        Assert-Installer061 -Condition (@(Get-ChildItem -LiteralPath $createdCollectDirectory -Filter '*.dmp' -File -Recurse).Count -eq 0) -Message 'Default collection included a crash dump without opt-in.'
    }

    $pendingStamp = '20260820-030301-001-99999999'
    $pendingTransaction = Join-Path $game ('.dtmapi-runtime-install-' + $pendingStamp)
    $pendingStateTransaction = Join-Path (Join-Path $game 'DTMAPI') ('.runtime-install-transaction-' + $pendingStamp)
    New-Item -ItemType Directory -Force -Path $pendingTransaction | Out-Null
    $pendingCandidateRoot = Join-Path $pendingStateTransaction 'candidate'
    Write-Utf8NoBomJson -Path (Join-Path $pendingTransaction 'transaction.json') -Value ([ordered]@{
        SchemaVersion = 1
        GameDir = [System.IO.Path]::GetFullPath($game)
        StateDir = [System.IO.Path]::GetFullPath((Join-Path $game 'DTMAPI'))
        PluginDir = [System.IO.Path]::GetFullPath((Join-Path $game 'BepInEx\plugins\DTMAPI'))
        Phase = 'Created'
        FailedPhase = ''
        RuntimeTransactionRoot = [System.IO.Path]::GetFullPath($pendingTransaction)
        TransactionRoot = [System.IO.Path]::GetFullPath($pendingStateTransaction)
        CandidatePlugin = [System.IO.Path]::GetFullPath((Join-Path $pendingTransaction 'candidate-plugin'))
        RecoveryPlugin = [System.IO.Path]::GetFullPath((Join-Path $pendingTransaction 'recovery-plugin'))
        CandidateTools = [System.IO.Path]::GetFullPath((Join-Path $pendingCandidateRoot 'tools'))
        CandidateComponents = [System.IO.Path]::GetFullPath((Join-Path $pendingCandidateRoot 'components'))
        CandidateReleaseManifest = [System.IO.Path]::GetFullPath((Join-Path $pendingCandidateRoot 'release-manifest.json'))
        CandidateInstallState = [System.IO.Path]::GetFullPath((Join-Path $pendingCandidateRoot 'install-state.json'))
        RecoveryState = [System.IO.Path]::GetFullPath((Join-Path $pendingStateTransaction 'recovery'))
        LiveTools = [System.IO.Path]::GetFullPath((Join-Path $game 'DTMAPI\tools'))
        LiveComponents = [System.IO.Path]::GetFullPath((Join-Path $game 'DTMAPI\components'))
        LiveReleaseManifest = [System.IO.Path]::GetFullPath((Join-Path $game 'DTMAPI\release-manifest.json'))
        LiveInstallState = [System.IO.Path]::GetFullPath((Join-Path $game 'DTMAPI\install-state.json'))
        OldPluginExisted = $false; OldPluginMoved = $false; CandidatePluginPlaced = $false
        OldToolsExisted = $false; OldToolsMoved = $false; CandidateToolsPlaced = $false
        OldComponentsExisted = $false; OldComponentsMoved = $false; CandidateComponentsPlaced = $false
        OldReleaseManifestExisted = $false; OldReleaseManifestMoved = $false; CandidateReleaseManifestPlaced = $false
        OldInstallStateExisted = $false; OldInstallStateMoved = $false; CandidateInstallStatePlaced = $false
        CommitSucceeded = $false; RollbackSucceeded = $null
    })
    $pendingStatus = Invoke-Installer061Bat -Path (Join-Path $package '3_check_dtmapi_status.bat') -OutputPath (Join-Path $testRoot 'status-recovery-pending.txt')
    Assert-Installer061 -Condition ($pendingStatus.ExitCode -eq 1 -and $pendingStatus.Text -match 'DTM-E1302' -and $pendingStatus.Text -match 'RECOVERY_PENDING' -and (Test-Path -LiteralPath $pendingTransaction -PathType Container)) -Message "Status did not report a recoverable transaction read-only. See $($pendingStatus.OutputPath)"
    $pendingUninstall = Invoke-Installer061Bat -Path (Join-Path $package '2_uninstall_dtmapi.bat') -OutputPath (Join-Path $testRoot 'uninstall-pending.txt')
    Assert-Installer061 -Condition ($pendingUninstall.ExitCode -ne 0 -and $pendingUninstall.Text -match 'DTM-E1302' -and $pendingUninstall.Text -match 'recoverable Runtime install transaction is pending') -Message "Uninstall did not fail closed on a validated pending transaction. See $($pendingUninstall.OutputPath)"
    Assert-Installer061 -Condition (Test-Path -LiteralPath (Join-Path $game 'BepInEx\plugins\DTMAPI') -PathType Container) -Message 'Pending-transaction uninstall changed the installed Runtime.'
    Remove-Item -LiteralPath $pendingTransaction -Recurse -Force

    $sterileTransaction = Join-Path $game '.dtmapi-runtime-install-20260820-030302-002-aaaaaaaa'
    New-Item -ItemType Directory -Force -Path $sterileTransaction | Out-Null
    $sterileStatus = Invoke-Installer061Bat -Path (Join-Path $package '3_check_dtmapi_status.bat') -OutputPath (Join-Path $testRoot 'status-repairable-stale.txt')
    Assert-Installer061 -Condition ($sterileStatus.ExitCode -eq 1 -and $sterileStatus.Text -match 'DTM-W1301' -and $sterileStatus.Text -match 'REPAIRABLE_STALE' -and (Test-Path -LiteralPath $sterileTransaction -PathType Container)) -Message "Status did not report a sterile root read-only. See $($sterileStatus.OutputPath)"

    $uninstall = Invoke-Installer061Bat -Path (Join-Path $package '2_uninstall_dtmapi.bat') -OutputPath (Join-Path $testRoot 'uninstall.txt')
    Assert-Installer061NoParserFailure -Label 'uninstall' -Text $uninstall.Text
    Assert-Installer061 -Condition ($uninstall.ExitCode -eq 0 -and $uninstall.Text -match 'DTM-W1301' -and $uninstall.Text -match 'DTM-S2001' -and -not (Test-Path -LiteralPath $sterileTransaction)) -Message "Runtime uninstall did not repair the sterile root and report success. See $($uninstall.OutputPath)"
    Assert-Installer061 -Condition ((Get-DtmApiFileSha256 -Path $externalPlugin) -eq $externalPluginHash -and (Get-DtmApiFileSha256 -Path $externalConfig) -eq $externalConfigHash) -Message 'Runtime uninstall changed external BepInEx files.'
    $uninstallReceiptCount = @(Get-ChildItem -LiteralPath (Join-Path $game 'DTMAPI') -Filter 'uninstall-state-*.json' -File -ErrorAction SilentlyContinue).Count
    $noOpUninstall = Invoke-Installer061Bat -Path (Join-Path $package '2_uninstall_dtmapi.bat') -OutputPath (Join-Path $testRoot 'uninstall-no-op.txt')
    Assert-Installer061 -Condition ($noOpUninstall.ExitCode -eq 0 -and $noOpUninstall.Text -match 'DTM-S2002' -and $noOpUninstall.Text -match 'No installed DTMAPI-owned Runtime files were found; no files were removed') -Message "Repeated uninstall did not report an explicit no-op. See $($noOpUninstall.OutputPath)"
    $uninstallReceiptCountAfter = @(Get-ChildItem -LiteralPath (Join-Path $game 'DTMAPI') -Filter 'uninstall-state-*.json' -File -ErrorAction SilentlyContinue).Count
    Assert-Installer061 -Condition ($uninstallReceiptCountAfter -eq ($uninstallReceiptCount + 1)) -Message 'Repeated uninstall collided with or overwrote an earlier receipt.'

    Write-Host 'DTMAPI Runtime Workshop installer 0.6.1 matrix: PASS'
    Write-Host "Package path: $package"
    Write-Host "Game path: $game"
}
finally {
    foreach ($name in $environmentSnapshot.Keys) {
        [Environment]::SetEnvironmentVariable($name, $environmentSnapshot[$name], [EnvironmentVariableTarget]::Process)
    }
    foreach ($createdCollectDirectory in @($createdCollectDirectories.ToArray())) {
        if (-not (Test-Path -LiteralPath $createdCollectDirectory -PathType Container)) { continue }
        $ownedCollectOutput = $false
        foreach ($root in $collectRoots) {
            if ((Test-DtmApiPathIsSameOrChild -Child $createdCollectDirectory -Parent $root) -and
                -not $collectBefore.ContainsKey($createdCollectDirectory)) {
                $ownedCollectOutput = $true
                break
            }
        }
        if ($ownedCollectOutput) { Remove-Item -LiteralPath $createdCollectDirectory -Recurse -Force }
    }
    if (-not $KeepTemp -and (Test-Path -LiteralPath $testRoot)) {
        Assert-Installer061 -Condition (Test-DtmApiPathIsSameOrChild -Child $testRoot -Parent $testBase) -Message "Cleanup escaped managed root: $testRoot"
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
    elseif ($KeepTemp) {
        Write-Host "Retained test root: $testRoot"
    }
}
