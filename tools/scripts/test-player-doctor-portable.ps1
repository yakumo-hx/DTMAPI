param(
    [string] $Configuration = 'Release',
    [string] $ExecutablePath = '',
    [switch] $KeepTemp
)

. "$PSScriptRoot\common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-PlayerDoctorTreeDigest {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return '<missing>' }
    $rows = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force | Sort-Object FullName)) {
        $relative = $file.FullName.Substring($Root.TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        $rows.Add(('{0}|{1}|{2}' -f $relative, $file.Length, $hash)) | Out-Null
    }
    $text = @($rows.ToArray()) -join "`n"
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    $ExecutablePath = Join-Path $repo 'dist\player-doctor\win-x64\dtmapi-player-doctor.exe'
}
$exe = [System.IO.Path]::GetFullPath($ExecutablePath)
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Player Doctor executable is missing: $exe" }

$fixtureRoot = Join-Path $repo ("tests\DTMAPI.InstallDoctor.Tests\bin\{0}\net8.0" -f $Configuration)
$pluginFixture = Join-Path $fixtureRoot 'DoctorBepInExPluginFixture.dll'
$codeModFixture = Join-Path $fixtureRoot 'DoctorCodeModFixture.dll'
foreach ($fixture in @($pluginFixture, $codeModFixture)) {
    if (-not (Test-Path -LiteralPath $fixture -PathType Leaf)) { throw "Player Doctor fixture is missing; run build.ps1 first: $fixture" }
}

$tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$tempRoot = [System.IO.Path]::GetFullPath((Join-Path $tempBase ('DTMAPI Player Doctor 只读 含 space ' + [Guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $tempRoot -Parent $tempBase)) { throw "Portable test root escaped system temp: $tempRoot" }
$lock = $null
try {
    $game = Join-Path $tempRoot '游戏 Game'
    $external = Join-Path $game 'BepInEx\plugins\External owner'
    $wrong = Join-Path $game 'BepInEx\plugins\Wrong CodeMod'
    $mods = Join-Path $game 'Mods'
    $reports = Join-Path $tempRoot '报告 Reports'
    foreach ($directory in @($external, $wrong, $mods, $reports)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    Copy-Item -LiteralPath $pluginFixture -Destination (Join-Path $external 'External.dll') -Force
    $wrongDll = Join-Path $wrong 'WrongCodeMod.dll'
    Copy-Item -LiteralPath $codeModFixture -Destination $wrongDll -Force

    $pluginBefore = Get-PlayerDoctorTreeDigest -Root (Join-Path $game 'BepInEx\plugins')
    $modsBefore = Get-PlayerDoctorTreeDigest -Root $mods
    $lock = New-Object System.IO.FileStream($wrongDll, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)

    $json = Join-Path $reports 'player-doctor.json'
    $text = Join-Path $reports 'player-doctor.txt'
    $summary = Join-Path $reports 'player-doctor-summary.txt'
    $oldPath = $env:PATH
    $oldDotnetRoot = $env:DOTNET_ROOT
    $oldDotnetRootX64 = $env:DOTNET_ROOT_X64
    try {
        $env:PATH = ''
        $env:DOTNET_ROOT = ''
        $env:DOTNET_ROOT_X64 = ''
        & $exe inspect --game-root $game --runtime-version '0.5.5-preview+portable' --scan-context installed-game --json-output $json --text-output $text --summary-output $summary --quiet
        $exit = $LASTEXITCODE
    }
    finally {
        $env:PATH = $oldPath
        $env:DOTNET_ROOT = $oldDotnetRoot
        $env:DOTNET_ROOT_X64 = $oldDotnetRootX64
    }

    if ($exit -ne 2) { throw "Portable Player Doctor should complete with placement findings (exit 2); got $exit" }
    if (-not (Test-Path -LiteralPath $json -PathType Leaf) -or -not (Test-Path -LiteralPath $text -PathType Leaf) -or -not (Test-Path -LiteralPath $summary -PathType Leaf)) {
        throw 'Portable Player Doctor did not write all three explicit report files.'
    }
    $report = Get-Content -Raw -Encoding UTF8 -LiteralPath $json | ConvertFrom-Json
    $externalArtifact = @($report.artifacts | Where-Object { $_.kind -eq 'ExternalBepInExPlugin' })
    $misplacedArtifact = @($report.artifacts | Where-Object { $_.kind -eq 'DtmApiCodeMod' -and $_.placement -eq 'Misplaced' })
    if ($externalArtifact.Count -ne 1 -or $externalArtifact[0].placement -ne 'Expected') { throw 'Portable Player Doctor did not preserve external BepInEx ownership/placement.' }
    if ($misplacedArtifact.Count -ne 1) { throw 'Portable Player Doctor did not diagnose the ordinary CodeMod misplaced under BepInEx/plugins.' }
    if ((Get-Content -Raw -Encoding UTF8 -LiteralPath $summary).IndexOf('runtime=0.5.5-preview+portable', [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Portable Player Doctor summary lost the suffix-bearing Runtime compatibility version.'
    }

    $statusScript = Join-Path $repo 'tools\scripts\check-dtmapi-status.ps1'
    $powerShellHost = (Get-Process -Id $PID).Path
    $statusLines = @(& $powerShellHost -NoProfile -ExecutionPolicy Bypass -File $statusScript -GameDir $game 2>&1)
    $statusText = @($statusLines | ForEach-Object { [string]$_ }) -join "`n"
    if ($statusText.IndexOf('Player Doctor helper is present (source: developer-build).', [System.StringComparison]::Ordinal) -lt 0) {
        throw "Repository-source status entry did not resolve the tracked developer Player Doctor output.`n$statusText"
    }
    if ($statusText.IndexOf('Player Doctor helper is missing', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Repository-source status entry still reported the Player Doctor as missing.`n$statusText"
    }
    if ($pluginBefore -ne (Get-PlayerDoctorTreeDigest -Root (Join-Path $game 'BepInEx\plugins')) -or $modsBefore -ne (Get-PlayerDoctorTreeDigest -Root $mods)) {
        throw 'Portable Player Doctor changed the read-only BepInEx/plugins or Mods tree.'
    }

    Write-Host 'Player Doctor portable gate: PASS'
    Write-Host "Executable: $exe"
    Write-Host "Test root: $tempRoot"
}
finally {
    if ($null -ne $lock) { $lock.Dispose() }
    if (-not $KeepTemp -and (Test-Path -LiteralPath $tempRoot)) {
        if (-not (Test-DtmApiPathIsSameOrChild -Child $tempRoot -Parent $tempBase)) { throw "Refusing to clean escaped portable test root: $tempRoot" }
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
    elseif ($KeepTemp) {
        Write-Host "Portable Player Doctor evidence retained: $tempRoot"
    }
}
