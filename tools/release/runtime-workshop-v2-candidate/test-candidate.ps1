[CmdletBinding()]
param(
    [string] $CandidatePath,
    [string] $OutputRoot
)

$ErrorActionPreference = 'Stop'
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
if ([string]::IsNullOrWhiteSpace($CandidatePath)) {
    $CandidatePath = Join-Path $repo 'dist\runtime-installer-v2-candidate\DTMAPI'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $token = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $OutputRoot = Join-Path $repo ("tmp\test-runs\runtime-installer-v2-$token")
}
$candidate = [System.IO.Path]::GetFullPath($CandidatePath).TrimEnd('\', '/')
$evidence = [System.IO.Path]::GetFullPath($OutputRoot).TrimEnd('\', '/')
if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
    throw "V2 candidate is missing: $candidate"
}
[System.IO.Directory]::CreateDirectory($evidence) | Out-Null

$cases = New-Object 'System.Collections.Generic.List[object]'
$processes = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'

function Add-Case([string] $Name, [string] $Result, [string] $Details) {
    $script:cases.Add([pscustomobject]@{ Name = $Name; Result = $Result; Details = $Details })
    $color = if ($Result -eq 'PASS') { 'Green' } else { 'Red' }
    Write-Host "[$Result] $Name - $Details" -ForegroundColor $color
}

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function New-TestDirectory([string] $Relative) {
    $path = Join-Path $evidence $Relative
    [System.IO.Directory]::CreateDirectory($path) | Out-Null
    return $path
}

function Copy-DirectoryContents([string] $Source, [string] $Destination) {
    [System.IO.Directory]::CreateDirectory($Destination) | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $Source -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }
}

function New-FakeGame([string] $Path) {
    [System.IO.Directory]::CreateDirectory($Path) | Out-Null
    [System.IO.Directory]::CreateDirectory((Join-Path $Path 'DolocTown_Data')) | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $Path 'DolocTown.exe'), [byte[]]@())
    [System.IO.File]::WriteAllText((Join-Path $Path 'unrelated-game-file.keep'), 'keep')
    return [System.IO.Path]::GetFullPath($Path)
}

function Quote-Argument([string] $Value) {
    if ($Value -notmatch '[\s&;()\[\]"]') {
        return $Value
    }
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Start-CapturedProcess {
    param(
        [Parameter(Mandatory = $true)] [string] $FileName,
        [Parameter(Mandatory = $true)] [string] $Arguments,
        [Parameter(Mandatory = $true)] [hashtable] $Environment,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FileName
    $psi.Arguments = $Arguments
    $psi.WorkingDirectory = $repo
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.RedirectStandardInput = $true
    foreach ($key in $Environment.Keys) {
        $psi.EnvironmentVariables[$key] = [string]$Environment[$key]
    }
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    if (-not $process.Start()) {
        throw "Could not start process: $FileName $Arguments"
    }
    $process.StandardInput.Close()
    $script:processes.Add($process)
    return [pscustomobject]@{ Process = $process; Name = $Name }
}

function Complete-CapturedProcess {
    param(
        [Parameter(Mandatory = $true)] $Handle,
        [int] $TimeoutMilliseconds = 120000
    )

    if (-not $Handle.Process.WaitForExit($TimeoutMilliseconds)) {
        try { $Handle.Process.Kill() } catch { }
        throw "Timed out: $($Handle.Name)"
    }
    $stdout = $Handle.Process.StandardOutput.ReadToEnd()
    $stderr = $Handle.Process.StandardError.ReadToEnd()
    $result = [pscustomobject]@{
        Name = $Handle.Name
        ExitCode = $Handle.Process.ExitCode
        Stdout = $stdout
        Stderr = $stderr
        Combined = $stdout + $stderr
    }
    $safeName = ($Handle.Name -replace '[^A-Za-z0-9_.-]', '_')
    [System.IO.File]::WriteAllText((Join-Path $evidence "$safeName.out.txt"), $stdout, [System.Text.UTF8Encoding]::new($true))
    [System.IO.File]::WriteAllText((Join-Path $evidence "$safeName.err.txt"), $stderr, [System.Text.UTF8Encoding]::new($true))
    return $result
}

function Invoke-CapturedProcess {
    param(
        [Parameter(Mandatory = $true)] [string] $FileName,
        [Parameter(Mandatory = $true)] [string] $Arguments,
        [Parameter(Mandatory = $true)] [hashtable] $Environment,
        [Parameter(Mandatory = $true)] [string] $Name,
        [int] $TimeoutMilliseconds = 120000
    )

    return Complete-CapturedProcess -Handle (Start-CapturedProcess -FileName $FileName -Arguments $Arguments -Environment $Environment -Name $Name) -TimeoutMilliseconds $TimeoutMilliseconds
}

function Get-TestEnvironment([string] $GameDir, [string] $ProfileRoot, [string] $LogExportRoot) {
    $environment = @{
        'DTMAPI_GAME_DIR' = $GameDir
        'DTMAPI_NO_PAUSE' = '1'
        'USERPROFILE' = $ProfileRoot
        'TEMP' = (Join-Path $evidence 'process-temp')
        'TMP' = (Join-Path $evidence 'process-temp')
    }
    if (-not [string]::IsNullOrWhiteSpace($LogExportRoot)) {
        $environment['DTMAPI_LOG_EXPORT_DIR'] = $LogExportRoot
    }
    [System.IO.Directory]::CreateDirectory($environment['USERPROFILE']) | Out-Null
    [System.IO.Directory]::CreateDirectory($environment['TEMP']) | Out-Null
    return $environment
}

function Invoke-Bat {
    param(
        [Parameter(Mandatory = $true)] [string] $PackageRoot,
        [Parameter(Mandatory = $true)] [string] $BatName,
        [Parameter(Mandatory = $true)] [hashtable] $Environment,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $bat = Join-Path $PackageRoot $BatName
    $arguments = '/d /e:off /v:off /c call ' + (Quote-Argument $bat)
    return Invoke-CapturedProcess -FileName $env:ComSpec -Arguments $arguments -Environment $Environment -Name $Name
}

function Start-Bat {
    param(
        [Parameter(Mandatory = $true)] [string] $PackageRoot,
        [Parameter(Mandatory = $true)] [string] $BatName,
        [Parameter(Mandatory = $true)] [hashtable] $Environment,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $bat = Join-Path $PackageRoot $BatName
    $arguments = '/d /e:off /v:off /c call ' + (Quote-Argument $bat)
    return Start-CapturedProcess -FileName $env:ComSpec -Arguments $arguments -Environment $Environment -Name $Name
}

function Invoke-PowerShellScript {
    param(
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $ScriptPath,
        [string[]] $ExtraArguments,
        [Parameter(Mandatory = $true)] [hashtable] $Environment,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $parts = New-Object 'System.Collections.Generic.List[string]'
    $parts.Add('-NoLogo')
    $parts.Add('-NoProfile')
    $parts.Add('-NonInteractive')
    $parts.Add('-ExecutionPolicy')
    $parts.Add('Bypass')
    $parts.Add('-File')
    $parts.Add((Quote-Argument $ScriptPath))
    foreach ($argument in @($ExtraArguments)) {
        $parts.Add((Quote-Argument $argument))
    }
    return Invoke-CapturedProcess -FileName $HostPath -Arguments ([string]::Join(' ', $parts)) -Environment $Environment -Name $Name
}

function Get-Sha256([string] $Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
        $stream.Dispose()
    }
}

function Get-TreeManifest([string] $Root) {
    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in @(Get-ChildItem -LiteralPath $rootFull -Recurse -File -Force | Sort-Object FullName)) {
        $relative = $file.FullName.Substring($rootFull.Length).TrimStart('\', '/')
        $lines.Add("$relative`t$($file.Length)`t$(Get-Sha256 $file.FullName)")
    }
    return [string[]]$lines
}

$immutableRoots = @(
    (Join-Path $repo 'tools\release\runtime-workshop'),
    (Join-Path $repo 'dist\workshop-packages-0.6.1\DTMAPI')
)
$immutableBefore = @{}
foreach ($root in $immutableRoots) {
    $immutableBefore[$root] = [string]::Join("`n", (Get-TreeManifest $root))
}

try {
    [System.IO.File]::WriteAllText(
        (Join-Path $evidence 'session.json'),
        ('{"owner":"DTMAPI.RuntimeInstallerV2Stress","started":"' + [DateTime]::UtcNow.ToString('o') + '","candidate":"' + $candidate.Replace('\', '\\') + '"}'),
        [System.Text.UTF8Encoding]::new($true))

    $winPs = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    Assert-True (Test-Path -LiteralPath $winPs -PathType Leaf) 'Windows PowerShell 5.1 is unavailable.'
    $pwshCommand = Get-Command pwsh.exe -ErrorAction SilentlyContinue
    $pwsh = if ($null -ne $pwshCommand) { $pwshCommand.Source } else { '' }

    $bats = @(Get-ChildItem -LiteralPath $candidate -File -Filter '*.bat' | Sort-Object Name | ForEach-Object { $_.Name })
    $psFiles = @(Get-ChildItem -LiteralPath $candidate -Recurse -File -Filter '*.ps1')
    Assert-True ($bats.Count -eq 5) "Expected five BAT files, found $($bats.Count)."
    Assert-True ($psFiles.Count -eq 6) "Expected six PowerShell files, found $($psFiles.Count)."
    Assert-True (@(Get-ChildItem -LiteralPath $candidate -Recurse -File -Filter '*.exe').Count -eq 0) 'Candidate contains an EXE.'
    foreach ($scriptFile in @(Get-ChildItem -LiteralPath $candidate -Recurse -File | Where-Object { $_.Extension -in @('.bat', '.cmd', '.ps1') })) {
        $bytes = [System.IO.File]::ReadAllBytes($scriptFile.FullName)
        Assert-True ($bytes.Length -ge 2) "Candidate script is unexpectedly empty: $($scriptFile.FullName)"
        for ($byteIndex = 0; $byteIndex -lt $bytes.Length; $byteIndex++) {
            if ($bytes[$byteIndex] -eq 10) {
                Assert-True ($byteIndex -gt 0 -and $bytes[$byteIndex - 1] -eq 13) "Candidate script contains a non-CRLF line ending: $($scriptFile.FullName)"
            }
        }
        if ($scriptFile.Extension -eq '.ps1') {
            Assert-True ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) "PowerShell script lacks a UTF-8 BOM: $($scriptFile.FullName)"
        }
    }
    $allPlayerText = [string]::Join("`n", @((Get-ChildItem -LiteralPath $candidate -Recurse -File | Where-Object { $_.Extension -in @('.bat', '.cmd', '.ps1', '.txt') }) | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }))
    $cmdPlayerText = [string]::Join("`n", @((Get-ChildItem -LiteralPath $candidate -Recurse -File | Where-Object { $_.Extension -in @('.bat', '.cmd') }) | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }))
    Assert-True ($allPlayerText -notmatch [regex]::Escape('C:\DTMAPI')) 'Candidate recommends the forbidden fixed C:\DTMAPI path.'
    Assert-True ($allPlayerText -notmatch 'move.+Steam|Steam.+library.+move') 'Candidate includes Steam-library relocation guidance.'
    Assert-True ($allPlayerText -notmatch '\bGet-FileHash\b|\bExpand-Archive\b|https?://') 'Candidate retained forbidden cmdlet or network installer dependencies.'
    Assert-True ($cmdPlayerText -notmatch '(?im)^\s*[^r\n]*\s-(?:EncodedCommand|Command)\b') 'BAT/CMD entry retained an inline or encoded PowerShell command.'
    Add-Case 'package-layout-and-static-boundary' 'PASS' 'five BATs, six BOM+CRLF PS1 files, zero EXEs, no fixed C:\DTMAPI/network/optional-cmdlet/inline-command contract'

    $probe = Join-Path $candidate 'Content\DTMAPIInstaller\tools\probe-powershell-host.ps1'
    $probeEnvironment = Get-TestEnvironment -GameDir $evidence -ProfileRoot (Join-Path $evidence 'probe-profile') -LogExportRoot ''
    foreach ($hostRecord in @([pscustomobject]@{ Name = 'winps51'; Path = $winPs }, [pscustomobject]@{ Name = 'pwsh7'; Path = $pwsh })) {
        if ([string]::IsNullOrWhiteSpace($hostRecord.Path)) {
            if ($hostRecord.Name -eq 'pwsh7') {
                Add-Case 'powershell-7-probe' 'PASS' 'PowerShell 7 is not installed; fallback coverage is not applicable on this machine'
                continue
            }
        }
        foreach ($action in @('install', 'uninstall', 'full-uninstall', 'check', 'collect')) {
            $nonce = [Guid]::NewGuid().ToString('N')
            $resultPath = Join-Path $evidence ("probe-$($hostRecord.Name)-$action-$nonce.txt")
            $result = Invoke-PowerShellScript -HostPath $hostRecord.Path -ScriptPath $probe -ExtraArguments @('-Token', $nonce, '-Action', $action, '-ToolsRoot', (Join-Path $candidate 'Content\DTMAPIInstaller\tools'), '-ResultPath', $resultPath) -Environment $probeEnvironment -Name "probe-$($hostRecord.Name)-$action"
            Assert-True ($result.ExitCode -eq 0) "$($hostRecord.Name) probe failed for $action. $($result.Combined)"
            Assert-True ((Get-Content -LiteralPath $resultPath -Raw) -eq "DTMAPI-PROBE:${nonce}:$action") "$($hostRecord.Name) probe proof mismatch for $action."
        }
        Add-Case "powershell-$($hostRecord.Name)-probe" 'PASS' 'all five action script sets parsed and produced nonce-bound proof'
    }

    $specialPackage = New-TestDirectory 'special-path\Program Files (x86) & 中文; [literal]\steamapps\workshop\content\2285550\3743016467'
    Copy-DirectoryContents -Source $candidate -Destination $specialPackage
    $specialGame = New-FakeGame (Join-Path $evidence 'G')
    $profile = New-TestDirectory 'isolated-user-profile'
    $logOutput = New-TestDirectory 'log-output'
    $environment = Get-TestEnvironment -GameDir $specialGame -ProfileRoot $profile -LogExportRoot $logOutput

    $install = Invoke-Bat -PackageRoot $specialPackage -BatName '1_install_dtmapi.bat' -Environment $environment -Name 'special-install'
    Assert-True ($install.ExitCode -eq 0) "Special-path install failed. $($install.Combined)"
    $status = Invoke-Bat -PackageRoot $specialPackage -BatName '3_check_dtmapi_status.bat' -Environment $environment -Name 'special-check'
    Assert-True ($status.ExitCode -eq 0) "Special-path status failed. $($status.Combined)"
    Assert-True ($status.Combined -match 'static file check; it does not prove') 'Status output blurred static file presence into current-session load proof.'
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll') -PathType Leaf) 'Runtime DLL missing after install.'
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png') -PathType Leaf) 'Runtime title icon asset missing after install.'
    [System.IO.Directory]::CreateDirectory((Join-Path $specialGame 'BepInEx\plugins\Foreign [literal]')) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $specialGame 'BepInEx\plugins\Foreign [literal]\foreign.keep'), 'keep')
    [System.IO.Directory]::CreateDirectory((Join-Path $specialGame 'BepInEx\config')) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $specialGame 'BepInEx\config\foreign.cfg'), 'keep')
    Add-Case 'common-special-path-chain' 'PASS' 'outer cmd /e:off reached WinPS 5.1 through package parentheses, ampersand, Chinese, semicolon and square brackets'

    $logDir = Join-Path $specialGame 'DTMAPI\logs'
    [System.IO.Directory]::CreateDirectory($logDir) | Out-Null
    for ($index = 0; $index -lt 12; $index++) {
        $name = if ($index -eq 0) { 'latest.log' } else { 'latest-{0:00}.log' -f $index }
        $path = Join-Path $logDir $name
        $stream = [System.IO.FileStream]::new($path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
        try {
            $length = if ($index -eq 0) { 6MB + 137 } else { 1024 + $index }
            $stream.SetLength($length)
        }
        finally {
            $stream.Dispose()
        }
        [System.IO.File]::SetLastWriteTimeUtc($path, [DateTime]::UtcNow.AddMinutes(-$index))
    }
    [System.IO.File]::WriteAllText((Join-Path $specialGame 'BepInEx\LogOutput.log'), 'bepinex complete log')
    $unityRoot = Join-Path $profile 'AppData\LocalLow\RedSawGames\DolocTown'
    [System.IO.Directory]::CreateDirectory($unityRoot) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $unityRoot 'Player.log'), 'unity complete log')

    $collect = Invoke-Bat -PackageRoot $specialPackage -BatName '4_collect_dtmapi_logs.bat' -Environment $environment -Name 'collect-ten-full'
    Assert-True ($collect.ExitCode -eq 0) "Complete log collection failed. $($collect.Combined)"
    $published = @(Get-ChildItem -LiteralPath $logOutput -Directory -Filter 'DTMAPI-logs-*')
    Assert-True ($published.Count -eq 1) "Expected one published log directory, found $($published.Count)."
    Assert-True (@(Get-ChildItem -LiteralPath (Join-Path $published[0].FullName 'DTMAPI') -File).Count -eq 10) 'Collector did not publish exactly the newest ten DTMAPI logs.'
    $copiedLatest = Join-Path $published[0].FullName 'DTMAPI\latest.log'
    Assert-True ((Get-Item -LiteralPath $copiedLatest).Length -eq (6MB + 137)) 'Large latest.log was truncated.'
    Assert-True ((Get-Sha256 $copiedLatest) -eq (Get-Sha256 (Join-Path $logDir 'latest.log'))) 'Large latest.log hash mismatch.'
    Assert-True (@(Get-ChildItem -LiteralPath $logOutput -Directory -Filter '*.staging').Count -eq 0) 'Published output retained a staging directory.'
    Add-Case 'ten-complete-unbounded-logs' 'PASS' 'newest ten DTMAPI logs copied in full; 6 MiB file length and SHA-256 match; no staging publication'

    $legacyRuntimeTransaction = Join-Path $specialGame '.dtmapi-runtime-install-20260808-010203-004-0123abcd'
    [System.IO.Directory]::CreateDirectory((Join-Path $legacyRuntimeTransaction 'recovery-plugin')) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $legacyRuntimeTransaction 'recovery-plugin\old-runtime.keep'), 'old')
    [System.IO.File]::WriteAllText((Join-Path $legacyRuntimeTransaction 'transaction.json'), '{"historical":true}')

    $normalUninstall = Invoke-Bat -PackageRoot $specialPackage -BatName '2_uninstall_dtmapi.bat' -Environment $environment -Name 'normal-uninstall'
    Assert-True ($normalUninstall.ExitCode -eq 0) "Normal uninstall failed. $($normalUninstall.Combined)"
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\plugins\DTMAPI'))) 'Normal uninstall left the Runtime plugin.'
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\plugins\Foreign [literal]\foreign.keep') -PathType Leaf) 'Normal uninstall removed a foreign BepInEx plugin.'
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\core\BepInEx.dll') -PathType Leaf) 'Normal uninstall removed BepInEx core.'
    Assert-True (Test-Path -LiteralPath (Join-Path $logDir 'latest.log') -PathType Leaf) 'Normal uninstall removed DTMAPI logs.'
    Assert-True (Test-Path -LiteralPath $legacyRuntimeTransaction -PathType Container) 'Normal Runtime-only uninstall unexpectedly treated a historical transaction receipt as live authority.'
    $afterUninstall = Invoke-Bat -PackageRoot $specialPackage -BatName '3_check_dtmapi_status.bat' -Environment $environment -Name 'check-after-normal-uninstall'
    Assert-True ($afterUninstall.ExitCode -eq 1) 'Check after normal uninstall should report missing Runtime.'
    Add-Case 'normal-uninstall-ownership' 'PASS' 'removed DTMAPI Runtime while preserving BepInEx core, foreign plugin and DTMAPI logs'

    $repair = Invoke-Bat -PackageRoot $specialPackage -BatName '1_install_dtmapi.bat' -Environment $environment -Name 'repair-after-uninstall'
    Assert-True ($repair.ExitCode -eq 0) "Repair install failed. $($repair.Combined)"
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\plugins\DTMAPI\old-runtime.keep'))) 'V2 install restored bytes from a historical Runtime transaction.'
    Assert-True ((Get-Sha256 (Join-Path $specialGame 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll')) -eq (Get-Sha256 (Join-Path $specialPackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'))) 'V2 reinstall did not converge live Runtime to packaged bytes while a historical transaction directory existed.'
    Add-Case 'historical-runtime-transaction-is-inert' 'PASS' 'normal uninstall did not trust an old receipt; reinstall converged from package bytes and never restored the old recovery tree'
    Remove-Item -LiteralPath (Join-Path $specialGame 'BepInEx\core\BepInEx.dll') -Force
    $repairBepInEx = Invoke-Bat -PackageRoot $specialPackage -BatName '1_install_dtmapi.bat' -Environment $environment -Name 'repair-partial-bepinex'
    Assert-True ($repairBepInEx.ExitCode -eq 0) "Partial BepInEx repair failed. $($repairBepInEx.Combined)"
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\core\BepInEx.dll') -PathType Leaf) 'Partial BepInEx repair did not restore BepInEx.dll.'
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\plugins\Foreign [literal]\foreign.keep') -PathType Leaf) 'BepInEx repair removed a foreign plugin.'
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\config\foreign.cfg') -PathType Leaf) 'BepInEx repair removed a foreign configuration file.'
    Add-Case 'convergent-repair' 'PASS' 'rerun restored DTMAPI and one missing BepInEx core file without deleting foreign plugin/configuration data'

    $cancel = Invoke-Bat -PackageRoot $specialPackage -BatName '9_full_uninstall_dtmapi_and_bepinex.bat' -Environment $environment -Name 'full-uninstall-cancel'
    Assert-True ($cancel.ExitCode -eq 2) "Complete uninstall without confirmation should cancel with exit 2. $($cancel.Combined)"
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx\core\BepInEx.dll') -PathType Leaf) 'Cancelled complete uninstall changed BepInEx.'

    $uninstallScript = Join-Path $specialPackage 'Content\DTMAPIInstaller\tools\uninstall-dtmapi.ps1'
    $full = Invoke-PowerShellScript -HostPath $winPs -ScriptPath $uninstallScript -ExtraArguments @('-Full', '-ConfirmFullRemove') -Environment $environment -Name 'full-uninstall-confirmed'
    Assert-True ($full.ExitCode -eq 0) "Confirmed complete uninstall failed. $($full.Combined)"
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $specialGame 'BepInEx'))) 'Complete uninstall left BepInEx.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $specialGame 'DTMAPI'))) 'Complete uninstall left DTMAPI logs/state.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $specialGame 'winhttp.dll'))) 'Complete uninstall left Doorstop winhttp.dll.'
    Assert-True (-not (Test-Path -LiteralPath $legacyRuntimeTransaction)) 'Complete uninstall left a production-shaped historical Runtime transaction directory.'
    Assert-True (Test-Path -LiteralPath (Join-Path $specialGame 'unrelated-game-file.keep') -PathType Leaf) 'Complete uninstall removed an unrelated game-root file.'
    Add-Case 'separate-complete-uninstall' 'PASS' 'interactive no-confirm path changed nothing; confirmed path removed exact DTMAPI/BepInEx/Doorstop ownership plus production-shaped old transaction residue and preserved unrelated game data'

    $noOpGame = New-FakeGame (Join-Path $evidence 'never-installed-game')
    $noOpEnvironment = Get-TestEnvironment -GameDir $noOpGame -ProfileRoot (Join-Path $evidence 'never-installed-profile') -LogExportRoot (Join-Path $evidence 'never-installed-logs')
    $noOpUninstall = Invoke-Bat -PackageRoot $specialPackage -BatName '2_uninstall_dtmapi.bat' -Environment $noOpEnvironment -Name 'never-installed-uninstall'
    Assert-True ($noOpUninstall.ExitCode -eq 0) 'Never-installed uninstall should succeed as a no-op.'
    Assert-True ($noOpUninstall.Combined -match 'not installed; uninstall made no changes') 'Never-installed uninstall did not report a truthful no-op.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $noOpGame 'BepInEx'))) 'Never-installed uninstall created or changed BepInEx.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $noOpGame 'DTMAPI'))) 'Never-installed uninstall created or changed DTMAPI.'
    Add-Case 'truthful-never-installed-uninstall' 'PASS' 'returned success, stated that nothing was installed, and created no Runtime/BepInEx state'

    $missingPackage = New-TestDirectory 'missing-package\DTMAPI'
    Copy-DirectoryContents -Source $candidate -Destination $missingPackage
    Remove-Item -LiteralPath (Join-Path $missingPackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Core.dll') -Force
    $missingGame = New-FakeGame (Join-Path $evidence 'missing-package-game')
    $missingEnvironment = Get-TestEnvironment -GameDir $missingGame -ProfileRoot (Join-Path $evidence 'missing-profile') -LogExportRoot (Join-Path $evidence 'missing-logs')
    $missingResult = Invoke-Bat -PackageRoot $missingPackage -BatName '1_install_dtmapi.bat' -Environment $missingEnvironment -Name 'missing-package-install'
    Assert-True ($missingResult.ExitCode -eq 1) 'Incomplete package install should fail.'
    Assert-True ($missingResult.Combined -match 'Resubscribe') 'Incomplete package did not give the stable resubscribe guidance.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $missingGame 'BepInEx'))) 'Incomplete package mutated the game before failing.'
    Add-Case 'incomplete-package-guidance' 'PASS' 'missing payload failed before mutation and printed the stable resubscribe guidance'

    $corruptPackage = New-TestDirectory 'corrupt-bepinex-package\DTMAPI'
    Copy-DirectoryContents -Source $candidate -Destination $corruptPackage
    [System.IO.File]::WriteAllText((Join-Path $corruptPackage 'Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip'), 'not a zip archive')
    $corruptGame = New-FakeGame (Join-Path $evidence 'corrupt-bepinex-game')
    $corruptEnvironment = Get-TestEnvironment -GameDir $corruptGame -ProfileRoot (Join-Path $evidence 'corrupt-profile') -LogExportRoot (Join-Path $evidence 'corrupt-logs')
    $corruptResult = Invoke-Bat -PackageRoot $corruptPackage -BatName '1_install_dtmapi.bat' -Environment $corruptEnvironment -Name 'corrupt-bepinex-install'
    Assert-True ($corruptResult.ExitCode -eq 1) 'Corrupt bundled BepInEx archive should fail.'
    Assert-True ($corruptResult.Combined -match 'Resubscribe') 'Corrupt bundled BepInEx archive did not give the stable resubscribe guidance.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $corruptGame 'BepInEx')) -and -not (Test-Path -LiteralPath (Join-Path $corruptGame 'DTMAPI'))) 'Corrupt BepInEx archive left a partial install.'
    Add-Case 'corrupt-bundled-bepinex-fails-before-copy' 'PASS' 'invalid ZIP failed before game mutation and used the single resubscribe guidance; no network/cache fallback exists'

    $invalidGame = New-TestDirectory 'invalid-game-target'
    [System.IO.File]::WriteAllText((Join-Path $invalidGame 'unrelated.keep'), 'keep')
    $invalidEnvironment = Get-TestEnvironment -GameDir $invalidGame -ProfileRoot (Join-Path $evidence 'invalid-profile') -LogExportRoot (Join-Path $evidence 'invalid-logs')
    $invalidResult = Invoke-Bat -PackageRoot $specialPackage -BatName '1_install_dtmapi.bat' -Environment $invalidEnvironment -Name 'invalid-game-install'
    Assert-True ($invalidResult.ExitCode -eq 1) 'Invalid explicit game target should fail.'
    Assert-True ($invalidResult.Combined -match 'Expected DolocTown\.exe and DolocTown_Data') 'Invalid explicit game target did not report marker validation.'
    Assert-True (Test-Path -LiteralPath (Join-Path $invalidGame 'unrelated.keep') -PathType Leaf) 'Invalid game target changed its pre-existing file.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $invalidGame 'BepInEx')) -and -not (Test-Path -LiteralPath (Join-Path $invalidGame 'DTMAPI'))) 'Invalid game target was mutated.'
    Add-Case 'invalid-explicit-game-target' 'PASS' 'missing game markers failed before mutation; no Steam-library relocation advice was emitted'

    $fallbackPackage = New-TestDirectory 'missing-dispatcher\DTMAPI'
    Copy-DirectoryContents -Source $candidate -Destination $fallbackPackage
    Remove-Item -LiteralPath (Join-Path $fallbackPackage 'Content\DTMAPIInstaller\tools\invoke-dtmapi-action.cmd') -Force
    $fallbackResult = Invoke-Bat -PackageRoot $fallbackPackage -BatName '3_check_dtmapi_status.bat' -Environment $missingEnvironment -Name 'cmd-only-fallback'
    Assert-True ($fallbackResult.ExitCode -eq 1) 'Missing dispatcher should fail.'
    Assert-True ($fallbackResult.Combined -match 'English letters and numbers') 'CMD-only fallback did not print path guidance.'
    Assert-True ($fallbackResult.Combined -match 'resubscribe') 'CMD-only fallback did not print resubscribe guidance.'
    Add-Case 'cmd-only-checker-fallback' 'PASS' 'root checker printed stable help without reaching any PowerShell script'

    $falseHostPackage = New-TestDirectory 'false-host\DTMAPI'
    Copy-DirectoryContents -Source $candidate -Destination $falseHostPackage
    $falseDispatcher = Join-Path $falseHostPackage 'Content\DTMAPIInstaller\tools\invoke-dtmapi-action.cmd'
    $dispatcherText = [System.IO.File]::ReadAllText($falseDispatcher)
    $dispatcherText = $dispatcherText.Replace('%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe', '%SystemRoot%\System32\cmd.exe')
    $dispatcherText = $dispatcherText.Replace('%ProgramFiles%\PowerShell\7\pwsh.exe', '%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe')
    [System.IO.File]::WriteAllText($falseDispatcher, $dispatcherText, [System.Text.Encoding]::ASCII)
    $falseHostGame = New-FakeGame (Join-Path $evidence 'false-host-game')
    $falseHostEnvironment = Get-TestEnvironment -GameDir $falseHostGame -ProfileRoot (Join-Path $evidence 'false-host-profile') -LogExportRoot (Join-Path $evidence 'false-host-logs')
    $falseHostResult = Invoke-Bat -PackageRoot $falseHostPackage -BatName '1_install_dtmapi.bat' -Environment $falseHostEnvironment -Name 'false-host-fallback'
    Assert-True ($falseHostResult.ExitCode -eq 0) "False-host fallback did not reach a real PowerShell. $($falseHostResult.Combined)"
    Assert-True ($falseHostResult.Combined -match 'WindowsPowerShell\\v1\.0\\powershell\.exe') 'False-host fallback output did not identify the real selected host.'
    Assert-True ($falseHostResult.Combined -notmatch 'Host used.+cmd\.exe|Using PowerShell host.+cmd\.exe') 'False host was accepted as PowerShell.'
    Assert-True (Test-Path -LiteralPath (Join-Path $falseHostGame 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll') -PathType Leaf) 'False-host fallback reported success without installing Runtime.'
    Add-Case 'nonce-bound-false-host-rejection' 'PASS' 'cmd.exe candidate could not forge the nonce result; dispatcher fell through to real Windows PowerShell and executed install once'

    $concurrentPackage = New-TestDirectory 'concurrent\DTMAPI'
    Copy-DirectoryContents -Source $candidate -Destination $concurrentPackage
    $concurrentGame = New-FakeGame (Join-Path $evidence 'concurrent-game')
    $concurrentEnvironment = Get-TestEnvironment -GameDir $concurrentGame -ProfileRoot (Join-Path $evidence 'concurrent-profile') -LogExportRoot (Join-Path $evidence 'concurrent-logs')
    $externalLockPath = Join-Path $concurrentGame '.dtmapi-installer.lock'
    $externalLock = [System.IO.FileStream]::new($externalLockPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $blockedHandles = @()
        for ($index = 0; $index -lt 4; $index++) {
            $blockedHandles += Start-Bat -PackageRoot $concurrentPackage -BatName '1_install_dtmapi.bat' -Environment $concurrentEnvironment -Name "blocked-concurrent-install-$index"
        }
        $blockedResults = @($blockedHandles | ForEach-Object { Complete-CapturedProcess -Handle $_ })
        Assert-True (@($blockedResults | Where-Object { $_.ExitCode -ne 1 }).Count -eq 0) 'Externally locked installers must all fail before mutation.'
        Assert-True (@($blockedResults | Where-Object { $_.Combined -match 'already using this game directory' }).Count -eq 4) 'Externally locked installers did not all report deterministic contention.'
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $concurrentGame 'BepInEx'))) 'Lock contention mutated BepInEx before failing.'
    }
    finally {
        $externalLock.Dispose()
        if (Test-Path -LiteralPath $externalLockPath -PathType Leaf) {
            Remove-Item -LiteralPath $externalLockPath -Force
        }
    }

    $handles = @()
    for ($index = 0; $index -lt 4; $index++) {
        $handles += Start-Bat -PackageRoot $concurrentPackage -BatName '1_install_dtmapi.bat' -Environment $concurrentEnvironment -Name "concurrent-install-$index"
    }
    $concurrentResults = @($handles | ForEach-Object { Complete-CapturedProcess -Handle $_ })
    Assert-True (@($concurrentResults | Where-Object { $_.ExitCode -eq 0 }).Count -ge 1) 'No concurrent installer completed.'
    Assert-True (@($concurrentResults | Where-Object { $_.ExitCode -notin @(0, 1) }).Count -eq 0) 'Concurrent install returned an unexpected exit code.'
    $concurrentCheck = Invoke-Bat -PackageRoot $concurrentPackage -BatName '3_check_dtmapi_status.bat' -Environment $concurrentEnvironment -Name 'concurrent-final-check'
    Assert-True ($concurrentCheck.ExitCode -eq 0) 'Final state after concurrent install is incomplete.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $concurrentGame '.dtmapi-installer.lock'))) 'Concurrent matrix left the installer lock file.'
    Add-Case 'per-game-cross-process-lock' 'PASS' 'four externally blocked installers failed before mutation; release-time four-process pressure converged to a healthy state without lock residue'

    $lockedLogOutput = New-TestDirectory 'locked-log-output'
    $lockedEnvironment = Get-TestEnvironment -GameDir $concurrentGame -ProfileRoot (Join-Path $evidence 'locked-profile') -LogExportRoot $lockedLogOutput
    $concurrentLogDir = Join-Path $concurrentGame 'DTMAPI\logs'
    [System.IO.Directory]::CreateDirectory($concurrentLogDir) | Out-Null
    $lockedLog = Join-Path $concurrentLogDir 'latest.log'
    [System.IO.File]::WriteAllText($lockedLog, 'locked log')
    $logLock = [System.IO.FileStream]::new($lockedLog, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $lockedCollect = Invoke-Bat -PackageRoot $concurrentPackage -BatName '4_collect_dtmapi_logs.bat' -Environment $lockedEnvironment -Name 'locked-log-collect'
        Assert-True ($lockedCollect.ExitCode -eq 1) 'Collector should fail when a selected log cannot be copied.'
        Assert-True (@(Get-ChildItem -LiteralPath $lockedLogOutput -Directory -Filter 'DTMAPI-logs-*').Count -eq 0) 'Collector published a partial bundle after a locked-file failure.'
        Assert-True (@(Get-ChildItem -LiteralPath $lockedLogOutput -Directory -Filter '*.staging').Count -eq 0) 'Collector left staging after a locked-file failure.'
    }
    finally {
        $logLock.Dispose()
    }
    $collectHandles = @(
        (Start-Bat -PackageRoot $concurrentPackage -BatName '4_collect_dtmapi_logs.bat' -Environment $lockedEnvironment -Name 'parallel-collect-1'),
        (Start-Bat -PackageRoot $concurrentPackage -BatName '4_collect_dtmapi_logs.bat' -Environment $lockedEnvironment -Name 'parallel-collect-2')
    )
    $collectResults = @($collectHandles | ForEach-Object { Complete-CapturedProcess -Handle $_ })
    Assert-True (@($collectResults | Where-Object { $_.ExitCode -ne 0 }).Count -eq 0) 'Parallel collectors did not both succeed after the log lock was released.'
    $parallelPublished = @(Get-ChildItem -LiteralPath $lockedLogOutput -Directory -Filter 'DTMAPI-logs-*')
    Assert-True ($parallelPublished.Count -eq 2) "Parallel collectors should publish two unique directories, found $($parallelPublished.Count)."
    Assert-True (@(Get-ChildItem -LiteralPath $lockedLogOutput -Directory -Filter '*.staging').Count -eq 0) 'Parallel collectors left staging directories.'
    Add-Case 'log-all-or-nothing-and-concurrency' 'PASS' 'locked source published nothing; two parallel retries produced two unique complete directories'

    $outside = New-TestDirectory 'junction-outside'
    [System.IO.File]::WriteAllText((Join-Path $outside 'outside.keep'), 'keep')
    $junctionGame = New-FakeGame (Join-Path $evidence 'junction-game')
    New-Item -ItemType Junction -Path (Join-Path $junctionGame 'BepInEx') -Target $outside | Out-Null
    [System.IO.Directory]::CreateDirectory((Join-Path $junctionGame 'DTMAPI')) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $junctionGame 'DTMAPI\log.keep'), 'keep')
    $junctionEnvironment = Get-TestEnvironment -GameDir $junctionGame -ProfileRoot (Join-Path $evidence 'junction-profile') -LogExportRoot (Join-Path $evidence 'junction-logs')
    $junctionFull = Invoke-PowerShellScript -HostPath $winPs -ScriptPath (Join-Path $concurrentPackage 'Content\DTMAPIInstaller\tools\uninstall-dtmapi.ps1') -ExtraArguments @('-Full', '-ConfirmFullRemove') -Environment $junctionEnvironment -Name 'junction-full-uninstall'
    Assert-True ($junctionFull.ExitCode -eq 1) 'Complete uninstall should refuse a BepInEx junction.'
    Assert-True (Test-Path -LiteralPath (Join-Path $outside 'outside.keep') -PathType Leaf) 'Junction refusal failed to preserve outside data.'
    Assert-True (Test-Path -LiteralPath (Join-Path $junctionGame 'unrelated-game-file.keep') -PathType Leaf) 'Junction refusal removed unrelated game data.'
    Add-Case 'reparse-and-outside-tree-safety' 'PASS' 'complete uninstall refused a BepInEx junction before recursive deletion and preserved outside bytes'

    . (Join-Path $candidate 'Content\DTMAPIInstaller\tools\player-common.ps1')
    $driveRejected = $false
    try {
        [void](Assert-DtmApiGameDirectory -Path ([System.IO.Path]::GetPathRoot($evidence)))
    }
    catch {
        $driveRejected = $_.Exception.Message -match 'drive root'
    }
    Assert-True $driveRejected 'Drive-root deletion guard did not fail before marker validation.'
    Add-Case 'drive-root-without-depth-heuristic' 'PASS' 'drive root rejected explicitly while the valid one-letter test game directory G installed successfully'

    foreach ($root in $immutableRoots) {
        $after = [string]::Join("`n", (Get-TreeManifest $root))
        Assert-True ($after -eq $immutableBefore[$root]) "Immutable existing installer/base tree changed: $root"
    }
    Add-Case 'existing-installer-and-base-immutability' 'PASS' 'current installer source and current 0.6.1 package manifests are byte-identical before/after'

    $summary = New-Object 'System.Collections.Generic.List[string]'
    $summary.Add('# Runtime Installer V2 Candidate Stress Summary')
    $summary.Add('')
    $summary.Add(('- Candidate: `{0}`' -f $candidate))
    $summary.Add(('- Evidence: `{0}`' -f $evidence))
    $summary.Add(('- Windows PowerShell: `{0}`' -f $winPs))
    $pwshDisplay = if ($pwsh) { $pwsh } else { 'not installed' }
    $summary.Add(('- PowerShell 7: `{0}`' -f $pwshDisplay))
    $summary.Add("- Cases: $($cases.Count)")
    $summary.Add("- Failures: $(@($cases | Where-Object { $_.Result -ne 'PASS' }).Count)")
    $summary.Add('')
    $summary.Add('| Case | Result | Details |')
    $summary.Add('| --- | --- | --- |')
    foreach ($case in $cases) {
        $summary.Add("| $($case.Name) | $($case.Result) | $($case.Details.Replace('|', '\|')) |")
    }
    [System.IO.File]::WriteAllLines((Join-Path $evidence 'stress-summary.md'), [string[]]$summary, [System.Text.UTF8Encoding]::new($true))
    $cases | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidence 'stress-cases.json') -Encoding UTF8
    Write-Host "[OK] Runtime Installer V2 stress matrix passed. Evidence: $evidence" -ForegroundColor Green
}
catch {
    Add-Case 'matrix-aborted' 'FAIL' $_.Exception.Message
    $failure = @(
        '# Runtime Installer V2 Candidate Stress Summary',
        '',
        ('- Candidate: `{0}`' -f $candidate),
        ('- Evidence: `{0}`' -f $evidence),
        "- Failure: $($_.Exception.Message)",
        '',
        '| Case | Result | Details |',
        '| --- | --- | --- |'
    )
    foreach ($case in $cases) {
        $failure += "| $($case.Name) | $($case.Result) | $($case.Details.Replace('|', '\|')) |"
    }
    [System.IO.File]::WriteAllLines((Join-Path $evidence 'stress-summary.md'), [string[]]$failure, [System.Text.UTF8Encoding]::new($true))
    throw
}
finally {
    foreach ($process in $processes) {
        try {
            if (-not $process.HasExited) {
                $process.Kill()
            }
            $process.Dispose()
        }
        catch {
        }
    }
}
