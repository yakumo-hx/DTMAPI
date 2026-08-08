[CmdletBinding()]
param(
    [string] $BasePackagePath,
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$distRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'dist')).TrimEnd('\', '/')
$overlayRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'overlay')).TrimEnd('\', '/')
if ([string]::IsNullOrWhiteSpace($BasePackagePath)) {
    $BasePackagePath = Join-Path $repo 'dist\workshop-packages-0.6.1\DTMAPI'
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repo 'dist\runtime-installer-v2-candidate\DTMAPI'
}
function Get-Full([string] $Path) {
    return [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
}

function Test-Child([string] $Child, [string] $Parent) {
    $childFull = Get-Full $Child
    $parentFull = Get-Full $Parent
    return $childFull.StartsWith($parentFull + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-TreeManifest([string] $Root) {
    $rootFull = Get-Full $Root
    $records = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in @(Get-ChildItem -LiteralPath $rootFull -Recurse -File -Force | Sort-Object FullName)) {
        $relative = $file.FullName.Substring($rootFull.Length).TrimStart('\', '/')
        $stream = [System.IO.File]::OpenRead($file.FullName)
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $hash = ([System.BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $sha.Dispose()
            $stream.Dispose()
        }
        $records.Add("$relative`t$($file.Length)`t$hash")
    }
    return [string[]]$records
}

function Copy-DirectoryContents([string] $Source, [string] $Destination) {
    [System.IO.Directory]::CreateDirectory($Destination) | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $Source -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }
}

function Normalize-CandidateText([string] $Root) {
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -Recurse -File -Force)) {
        $extension = $file.Extension.ToLowerInvariant()
        if ($extension -notin @('.bat', '.cmd', '.ps1')) {
            continue
        }
        $text = [System.IO.File]::ReadAllText($file.FullName)
        $text = ($text -replace "`r?`n", "`r`n").TrimEnd("`r", "`n") + "`r`n"
        if ($extension -eq '.ps1') {
            [System.IO.File]::WriteAllText($file.FullName, $text, [System.Text.UTF8Encoding]::new($true))
        }
        else {
            [System.IO.File]::WriteAllText($file.FullName, $text, [System.Text.Encoding]::ASCII)
        }
    }
}

$base = Get-Full $BasePackagePath
$output = Get-Full $OutputPath
if (-not (Test-Path -LiteralPath $base -PathType Container)) {
    throw "Base Runtime package is missing: $base"
}
if (-not (Test-Path -LiteralPath $overlayRoot -PathType Container)) {
    throw "V2 overlay is missing: $overlayRoot"
}
if (-not (Test-Child -Child $output -Parent $distRoot)) {
    throw "Candidate output must remain below the repository dist directory: $output"
}
if ([string]::Equals($base, $output, [System.StringComparison]::OrdinalIgnoreCase) -or
    (Test-Child -Child $output -Parent $base) -or (Test-Child -Child $base -Parent $output)) {
    throw 'Base package and candidate output must be separate trees.'
}

$baseBefore = Get-TreeManifest $base
$outputParent = Split-Path -Parent $output
[System.IO.Directory]::CreateDirectory($outputParent) | Out-Null
$leaf = [System.IO.Path]::GetFileName($output)
$token = [Guid]::NewGuid().ToString('N').Substring(0, 10)
$staging = Join-Path $outputParent (".$leaf.staging-$token")
$backup = Join-Path $outputParent (".$leaf.backup-$token")
$published = $false

try {
    [System.IO.Directory]::CreateDirectory($staging) | Out-Null
    Copy-DirectoryContents -Source $base -Destination $staging

    $candidateTools = Join-Path $staging 'Content\DTMAPIInstaller\tools'
    if (Test-Path -LiteralPath $candidateTools) {
        Remove-Item -LiteralPath $candidateTools -Recurse -Force
    }
    [System.IO.Directory]::CreateDirectory($candidateTools) | Out-Null
    Copy-DirectoryContents -Source $overlayRoot -Destination $staging
    Normalize-CandidateText -Root $staging

    $expectedBats = @(
        '1_install_dtmapi.bat',
        '2_uninstall_dtmapi.bat',
        '3_check_dtmapi_status.bat',
        '4_collect_dtmapi_logs.bat',
        '9_full_uninstall_dtmapi_and_bepinex.bat'
    )
    $actualBats = @(Get-ChildItem -LiteralPath $staging -File -Filter '*.bat' | Sort-Object Name | ForEach-Object { $_.Name })
    if ([string]::Join('|', $expectedBats) -ne [string]::Join('|', $actualBats)) {
        throw "Unexpected public BAT set: $([string]::Join(', ', $actualBats))"
    }
    $expectedTools = @(
        'check-dtmapi-status.ps1',
        'collect-logs.ps1',
        'install-dtmapi.ps1',
        'invoke-dtmapi-action.cmd',
        'player-common.ps1',
        'probe-powershell-host.ps1',
        'uninstall-dtmapi.ps1'
    )
    $actualTools = @(Get-ChildItem -LiteralPath $candidateTools -File | Sort-Object Name | ForEach-Object { $_.Name })
    if ([string]::Join('|', $expectedTools) -ne [string]::Join('|', $actualTools)) {
        throw "Unexpected candidate tool set: $([string]::Join(', ', $actualTools))"
    }
    if (@(Get-ChildItem -LiteralPath $staging -Recurse -File -Filter '*.exe').Count -ne 0) {
        throw 'The V2 Runtime candidate must contain zero EXE files.'
    }

    $basePayload = Join-Path $base 'Content\DTMAPIInstaller\Payload'
    $candidatePayload = Join-Path $staging 'Content\DTMAPIInstaller\Payload'
    if ([string]::Join("`n", (Get-TreeManifest $basePayload)) -ne [string]::Join("`n", (Get-TreeManifest $candidatePayload))) {
        throw 'Runtime payload bytes changed while applying the installer overlay.'
    }
    $baseBepInEx = Join-Path $base 'Content\.tools\bepinex'
    $candidateBepInEx = Join-Path $staging 'Content\.tools\bepinex'
    if ([string]::Join("`n", (Get-TreeManifest $baseBepInEx)) -ne [string]::Join("`n", (Get-TreeManifest $candidateBepInEx))) {
        throw 'Bundled BepInEx bytes changed while applying the installer overlay.'
    }

    if (Test-Path -LiteralPath $output -PathType Container) {
        Move-Item -LiteralPath $output -Destination $backup
    }
    Move-Item -LiteralPath $staging -Destination $output
    $published = $true
    if (Test-Path -LiteralPath $backup -PathType Container) {
        Remove-Item -LiteralPath $backup -Recurse -Force
    }

    $baseAfter = Get-TreeManifest $base
    if ([string]::Join("`n", $baseBefore) -ne [string]::Join("`n", $baseAfter)) {
        throw 'Base Runtime package changed during candidate construction.'
    }

    Write-Host "[OK] V2 candidate built without modifying the base package: $output" -ForegroundColor Green
    Write-Host "[INFO] Files=$(@(Get-ChildItem -LiteralPath $output -Recurse -File).Count) PowerShell=$(@(Get-ChildItem -LiteralPath $output -Recurse -File -Filter '*.ps1').Count) BAT=$($actualBats.Count) EXE=0"
}
catch {
    if (-not $published -and (Test-Path -LiteralPath $backup -PathType Container) -and -not (Test-Path -LiteralPath $output)) {
        Move-Item -LiteralPath $backup -Destination $output
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $staging -PathType Container) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
    if (Test-Path -LiteralPath $backup -PathType Container) {
        Remove-Item -LiteralPath $backup -Recurse -Force
    }
}
