param(
    [string] $Configuration = 'Release',
    [string] $PackageRoot = '',
    [switch] $KeepTemp
)

. "$PSScriptRoot\common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-PackagedDoctorTreeDigest {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return '<missing>'
    }

    $rootPath = [System.IO.Path]::GetFullPath($Root).TrimEnd('\')
    $rows = New-Object 'System.Collections.Generic.List[string]'
    foreach ($item in @(Get-ChildItem -LiteralPath $rootPath -Recurse -Force | Sort-Object FullName)) {
        $relative = $item.FullName.Substring($rootPath.Length).TrimStart('\').Replace('\', '/')
        if ($item.PSIsContainer) {
            $rows.Add(('D|{0}' -f $relative)) | Out-Null
        }
        else {
            $hash = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
            $rows.Add(('F|{0}|{1}|{2}' -f $relative, $item.Length, $hash)) | Out-Null
        }
    }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes((@($rows.ToArray()) -join "`n"))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function Invoke-PackagedDoctorBatch {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $OutputPath
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
    $commandLine = 'call "{0}" < nul' -f $Path.Replace('"', '""')
    $lines = @(& $env:ComSpec /d /s /c $commandLine 2>&1)
    $exitCode = $LASTEXITCODE
    @($lines | ForEach-Object { [string]$_ }) | Set-Content -Encoding UTF8 -LiteralPath $OutputPath
    return [pscustomobject]@{
        ExitCode = $exitCode
        OutputPath = $OutputPath
        Text = (@($lines | ForEach-Object { [string]$_ }) -join "`n")
    }
}

function Assert-NoPackagedDoctorParserFailure {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Text
    )

    $failurePatterns = @(
        'ParserError',
        'PowerShell host was not found',
        'PowerShell host probe failed',
        'The term .* is not recognized',
        'At .*\.ps1:[0-9]+ char:'
    )
    foreach ($pattern in $failurePatterns) {
        if ($Text -match $pattern) {
            throw "$Label reported a PowerShell/parser failure matching '$pattern'."
        }
    }
}

function Get-PackagedDoctorDirectorySet {
    param([string[]] $Roots)

    $set = @{}
    foreach ($root in @($Roots)) {
        if (-not [string]::IsNullOrWhiteSpace($root) -and (Test-Path -LiteralPath $root -PathType Container)) {
            foreach ($directory in @(Get-ChildItem -LiteralPath $root -Directory -Force -ErrorAction SilentlyContinue)) {
                $set[[System.IO.Path]::GetFullPath($directory.FullName)] = $true
            }
        }
    }
    return $set
}

function Copy-PackagedDoctorDirectoryContents {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $Source -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }
}

function Restore-PackagedDoctorEnvironment {
    param([Parameter(Mandatory = $true)] [hashtable] $Snapshot)

    foreach ($name in $Snapshot.Keys) {
        [Environment]::SetEnvironmentVariable($name, $Snapshot[$name], [EnvironmentVariableTarget]::Process)
    }
}

$repo = Get-RepoRoot
$fixture = Join-Path $repo ("tests\DTMAPI.InstallDoctor.Tests\bin\{0}\net8.0\DoctorCodeModFixture.dll" -f $Configuration)
if (-not (Test-Path -LiteralPath $fixture -PathType Leaf)) {
    throw "Packaged Player Doctor fixture is missing; run build.ps1 first: $fixture"
}

$tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$tempRoot = [System.IO.Path]::GetFullPath((Join-Path $tempBase ('DTMAPI Packaged Doctor 入口 中文 ' + [Guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $tempRoot -Parent $tempBase)) {
    throw "Packaged Player Doctor test root escaped system temp: $tempRoot"
}

$desktop = [Environment]::GetFolderPath('Desktop')
if ([string]::IsNullOrWhiteSpace($desktop)) {
    $desktop = Join-Path $env:USERPROFILE 'Desktop'
}
$processTemp = Join-Path $tempRoot 'Process Temp 中文'
$collectRoots = @(
    (Join-Path $desktop 'DTMAPI-logs'),
    (Join-Path $processTemp 'DTMAPI-logs')
)
$collectRootExisted = @{}
foreach ($collectRoot in $collectRoots) {
    $collectRootExisted[[System.IO.Path]::GetFullPath($collectRoot)] = Test-Path -LiteralPath $collectRoot -PathType Container
}
$collectBefore = Get-PackagedDoctorDirectorySet -Roots $collectRoots

$environmentNames = @('DTMAPI_GAME_DIR', 'DTMAPI_RUNTIME_DIR', 'DTMAPI_STATE_DIR', 'TEMP', 'TMP', 'USERPROFILE', 'HOME')
$environmentSnapshot = @{}
foreach ($name in $environmentNames) {
    $environmentSnapshot[$name] = [Environment]::GetEnvironmentVariable($name, [EnvironmentVariableTarget]::Process)
}

$lock = $null
$installed = $false
$uninstalled = $false
$createdCollectEvidence = ''
try {
    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $processTemp | Out-Null

    if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
        $constructedRoot = Join-Path $tempRoot 'Constructed Workshop Package 中文'
        & "$PSScriptRoot\build-release-workshop-packages.ps1" -Configuration $Configuration -SkipBuild -RuntimeOnly -OutputRoot $constructedRoot
        if (-not $?) {
            throw 'Failed to construct the Runtime Workshop package for the packaged Player Doctor entry-point matrix.'
        }
        $PackageRoot = Join-Path $constructedRoot 'DTMAPI'
    }

    $sourcePackage = [System.IO.Path]::GetFullPath($PackageRoot)
    if (-not (Test-Path -LiteralPath $sourcePackage -PathType Container)) {
        throw "Runtime Workshop package is missing: $sourcePackage"
    }
    $expectedRootBats = @(
        '1_install_dtmapi.bat',
        '2_uninstall_dtmapi.bat',
        '3_check_dtmapi_status.bat',
        '4_collect_dtmapi_logs.bat'
    )
    foreach ($required in $expectedRootBats) {
        if (-not (Test-Path -LiteralPath (Join-Path $sourcePackage $required) -PathType Leaf)) {
            throw "Runtime Workshop package is missing $required`: $sourcePackage"
        }
    }
    $actualRootBats = @(
        Get-ChildItem -LiteralPath $sourcePackage -File -Filter '*.bat' |
            ForEach-Object { [string]$_.Name } |
            Sort-Object
    )
    if (($actualRootBats -join [char]0) -cne
        (@($expectedRootBats | Sort-Object) -join [char]0)) {
        throw "Runtime Workshop package root BAT layout must be exactly 1..4. Actual=$([string]::Join(', ', $actualRootBats))"
    }
    if (Test-Path -LiteralPath (Join-Path $sourcePackage '0_probe_dtmapi_install.bat')) {
        throw 'Runtime Workshop player package must not expose 0_probe_dtmapi_install.bat.'
    }
    $dormantProbe = Join-Path $sourcePackage 'Content\DTMAPIInstaller\tools\probe-install-preflight.ps1'
    if (-not (Test-Path -LiteralPath $dormantProbe -PathType Leaf)) {
        throw 'Runtime Workshop package must retain the dormant internal preflight script used by the separate diagnostic package.'
    }

    $package = Join-Path $tempRoot '订阅 Package 含 space'
    Copy-PackagedDoctorDirectoryContents -Source $sourcePackage -Destination $package

    $game = Join-Path $tempRoot '游戏 Game 含 space'
    New-Item -ItemType Directory -Force -Path (Join-Path $game 'DolocTown_Data') | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $game 'DolocTown.exe'), [System.Text.Encoding]::ASCII.GetBytes('DTMAPI fake game marker'))

    $fakeProfile = Join-Path $tempRoot 'User Profile 中文'
    New-Item -ItemType Directory -Force -Path $fakeProfile | Out-Null
    $env:DTMAPI_GAME_DIR = $game
    $env:DTMAPI_RUNTIME_DIR = $null
    $env:DTMAPI_STATE_DIR = $null
    $env:TEMP = $processTemp
    $env:TMP = $processTemp
    $env:USERPROFILE = $fakeProfile
    $env:HOME = $fakeProfile

    $install = Invoke-PackagedDoctorBatch -Path (Join-Path $package '1_install_dtmapi.bat') -OutputPath (Join-Path $tempRoot 'install-output.txt')
    Assert-NoPackagedDoctorParserFailure -Label 'Packaged installer' -Text $install.Text
    if ($install.ExitCode -ne 0) {
        throw "Packaged Runtime install failed with exit code $($install.ExitCode). See $($install.OutputPath)"
    }
    $installed = $true

    $wrongDirectory = Join-Path $game 'BepInEx\plugins\Wrong CodeMod 中文'
    $modsDirectory = Join-Path $game 'Mods'
    New-Item -ItemType Directory -Force -Path $wrongDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $modsDirectory | Out-Null
    $wrongDll = Join-Path $wrongDirectory 'WrongCodeMod.dll'
    Copy-Item -LiteralPath $fixture -Destination $wrongDll -Force

    $pluginsBefore = Get-PackagedDoctorTreeDigest -Root (Join-Path $game 'BepInEx\plugins')
    $modsBefore = Get-PackagedDoctorTreeDigest -Root $modsDirectory
    $lock = New-Object System.IO.FileStream($wrongDll, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)

    $check = Invoke-PackagedDoctorBatch -Path (Join-Path $package '3_check_dtmapi_status.bat') -OutputPath (Join-Path $tempRoot 'check-output.txt')
    Assert-NoPackagedDoctorParserFailure -Label '3_check_dtmapi_status.bat' -Text $check.Text
    if ($check.ExitCode -ne 1) {
        throw "3_check_dtmapi_status.bat should return semantic invalid-state exit 1 for a misplaced ordinary CodeMod; got $($check.ExitCode)."
    }
    if ($check.Text -notmatch 'misplaced-artifact' -or
        $check.Text -notmatch 'DtmApiCodeMod/Misplaced' -or
        $check.Text -notmatch 'Player Doctor found installation, placement, or minimum-version errors') {
        throw '3_check_dtmapi_status.bat did not expose the packaged Player Doctor placement diagnosis.'
    }
    if ($pluginsBefore -ne (Get-PackagedDoctorTreeDigest -Root (Join-Path $game 'BepInEx\plugins')) -or
        $modsBefore -ne (Get-PackagedDoctorTreeDigest -Root $modsDirectory)) {
        throw '3_check_dtmapi_status.bat changed the BepInEx/plugins or Mods tree.'
    }

    $collect = Invoke-PackagedDoctorBatch -Path (Join-Path $package '4_collect_dtmapi_logs.bat') -OutputPath (Join-Path $tempRoot 'collect-output.txt')
    Assert-NoPackagedDoctorParserFailure -Label '4_collect_dtmapi_logs.bat' -Text $collect.Text
    if ($collect.ExitCode -ne 0) {
        throw "4_collect_dtmapi_logs.bat failed with exit code $($collect.ExitCode)."
    }

    $collectAfter = Get-PackagedDoctorDirectorySet -Roots $collectRoots
    $newEvidence = @($collectAfter.Keys | Where-Object { -not $collectBefore.ContainsKey($_) })
    if ($newEvidence.Count -ne 1) {
        throw "4_collect_dtmapi_logs.bat should create exactly one new evidence directory; found $($newEvidence.Count)."
    }
    $createdCollectEvidence = [System.IO.Path]::GetFullPath($newEvidence[0])
    $capturedEvidence = Join-Path $tempRoot 'captured collect evidence'
    Copy-PackagedDoctorDirectoryContents -Source $createdCollectEvidence -Destination $capturedEvidence

    $doctorJson = Join-Path $capturedEvidence 'player-doctor.json'
    $doctorText = Join-Path $capturedEvidence 'player-doctor.txt'
    $doctorSummary = Join-Path $capturedEvidence 'player-doctor-summary.txt'
    foreach ($reportPath in @($doctorJson, $doctorText, $doctorSummary)) {
        if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
            throw "4_collect_dtmapi_logs.bat did not export packaged Player Doctor evidence: $reportPath"
        }
    }

    $report = Get-Content -Raw -Encoding UTF8 -LiteralPath $doctorJson | ConvertFrom-Json
    $misplaced = @($report.artifacts | Where-Object {
        $_.kind -eq 'DtmApiCodeMod' -and
        $_.placement -eq 'Misplaced' -and
        [string]$_.path -like '*WrongCodeMod.dll'
    })
    if ($misplaced.Count -ne 1) {
        throw 'Collected player-doctor.json did not retain the misplaced ordinary CodeMod artifact.'
    }
    if ((Get-Content -Raw -Encoding UTF8 -LiteralPath $doctorText) -notmatch 'DtmApiCodeMod/Misplaced') {
        throw 'Collected player-doctor.txt did not retain the misplaced ordinary CodeMod diagnosis.'
    }
    if ((Get-Content -Raw -Encoding UTF8 -LiteralPath $doctorSummary) -notmatch 'misplaced=[1-9][0-9]*') {
        throw 'Collected player-doctor-summary.txt did not report a misplaced artifact.'
    }
    $collectSummary = @(
        (Join-Path $capturedEvidence 'summary.txt'),
        (Join-Path $capturedEvidence 'collect-summary.txt')
    ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace([string]$collectSummary) -or
        (Get-Content -Raw -LiteralPath $collectSummary) -notmatch 'PlayerDoctorExit=2') {
        throw '4_collect_dtmapi_logs.bat did not record the expected semantic Player Doctor exit 2 as collected evidence.'
    }
    if ($pluginsBefore -ne (Get-PackagedDoctorTreeDigest -Root (Join-Path $game 'BepInEx\plugins')) -or
        $modsBefore -ne (Get-PackagedDoctorTreeDigest -Root $modsDirectory)) {
        throw '4_collect_dtmapi_logs.bat changed the BepInEx/plugins or Mods tree.'
    }

    $lock.Dispose()
    $lock = $null
    $uninstall = Invoke-PackagedDoctorBatch -Path (Join-Path $package '2_uninstall_dtmapi.bat') -OutputPath (Join-Path $tempRoot 'uninstall-output.txt')
    Assert-NoPackagedDoctorParserFailure -Label 'Packaged uninstaller' -Text $uninstall.Text
    if ($uninstall.ExitCode -ne 0) {
        throw "Packaged Runtime uninstall failed with exit code $($uninstall.ExitCode)."
    }
    $uninstalled = $true
    if (-not (Test-Path -LiteralPath $wrongDll -PathType Leaf)) {
        throw 'The Runtime uninstaller removed the externally owned misplaced CodeMod fixture.'
    }

    Write-Host 'Packaged Player Doctor offline entry-point matrix: PASS'
    Write-Host "Package: $sourcePackage"
    Write-Host "Temporary game: $game"
    Write-Host "3_check exit: $($check.ExitCode) (expected placement-invalid semantic result)"
    Write-Host "4_collect exit: $($collect.ExitCode)"
    Write-Host "Captured evidence: $capturedEvidence"
}
finally {
    if ($null -ne $lock) {
        $lock.Dispose()
        $lock = $null
    }

    if ($installed -and -not $uninstalled -and (Test-Path -LiteralPath (Join-Path $tempRoot '订阅 Package 含 space\2_uninstall_dtmapi.bat') -PathType Leaf)) {
        try {
            [void](Invoke-PackagedDoctorBatch `
                -Path (Join-Path $tempRoot '订阅 Package 含 space\2_uninstall_dtmapi.bat') `
                -OutputPath (Join-Path $tempRoot 'uninstall-finally-output.txt'))
        }
        catch {
            Write-Warning "Best-effort temp Runtime uninstall failed: $($_.Exception.Message)"
        }
    }

    Restore-PackagedDoctorEnvironment -Snapshot $environmentSnapshot

    if (-not [string]::IsNullOrWhiteSpace($createdCollectEvidence) -and (Test-Path -LiteralPath $createdCollectEvidence -PathType Container)) {
        $ownedCollectPath = $false
        foreach ($collectRoot in $collectRoots) {
            if (Test-DtmApiPathIsSameOrChild -Child $createdCollectEvidence -Parent $collectRoot) {
                $ownedCollectPath = $true
                break
            }
        }
        if ($ownedCollectPath -and -not $collectBefore.ContainsKey($createdCollectEvidence)) {
            Remove-Item -LiteralPath $createdCollectEvidence -Recurse -Force
        }
        else {
            Write-Warning "Refusing to clean unowned collect output: $createdCollectEvidence"
        }
    }

    foreach ($collectRoot in $collectRoots) {
        $fullCollectRoot = [System.IO.Path]::GetFullPath($collectRoot)
        if (-not $collectRootExisted[$fullCollectRoot] -and (Test-Path -LiteralPath $fullCollectRoot -PathType Container)) {
            $remaining = @(Get-ChildItem -LiteralPath $fullCollectRoot -Force -ErrorAction SilentlyContinue)
            if ($remaining.Count -eq 0) {
                Remove-Item -LiteralPath $fullCollectRoot -Force
            }
        }
    }

    if (-not $KeepTemp -and (Test-Path -LiteralPath $tempRoot)) {
        if (-not (Test-DtmApiPathIsSameOrChild -Child $tempRoot -Parent $tempBase)) {
            throw "Refusing to clean escaped packaged Player Doctor test root: $tempRoot"
        }
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
    elseif ($KeepTemp) {
        Write-Host "Packaged Player Doctor test tree retained: $tempRoot"
    }
}
