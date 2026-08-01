param(
    [switch] $Force
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
Assert-DtmApiGameNotRunning -GameDir $gameDir -Operation 'BepInEx install'

$coreDll = Join-Path $gameDir 'BepInEx\core\BepInEx.dll'
if ((Test-DtmApiBepInExInstallComplete -GameDir $gameDir) -and -not $Force) {
    Write-Host "BepInEx already installed and complete: $coreDll"
    exit 0
}

$toolsDir = Join-Path $repo '.tools'
$cacheDir = Join-Path $toolsDir 'bepinex'
$extractDir = Join-Path $cacheDir 'extract'
$bepInExReleaseTag = 'v5.4.23.5'
$bepInExAssetName = 'BepInEx_win_x64_5.4.23.5.zip'
$bepInExAssetSha256 = '82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4'
$bepInExDownloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/$bepInExReleaseTag/$bepInExAssetName"
$bundledZip = Join-Path $repo "tools\release\bootstrap\$bepInExAssetName"
New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null
if (Test-Path $extractDir) {
    Remove-Item -Recurse -Force -LiteralPath $extractDir
}
New-Item -ItemType Directory -Force -Path $extractDir | Out-Null

function Get-BepInExZipSha256 {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha256.ComputeHash($stream)
            return ([System.BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Assert-BepInExZipHash {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $actual = Get-BepInExZipSha256 -Path $Path
    if ($actual -ne $bepInExAssetSha256) {
        throw "BepInEx package hash mismatch for $Path. Expected $bepInExAssetSha256 but got $actual."
    }
}

function Test-BepInExZipHash {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    $actual = Get-BepInExZipSha256 -Path $Path
    return $actual -eq $bepInExAssetSha256
}

function Get-ValidBepInExZip {
    if (Test-BepInExZipHash -Path $zip) {
        return $zip
    }

    if (Test-Path -LiteralPath $zip -PathType Leaf) {
        Write-Warning "Cached BepInEx package is incomplete or corrupted; replacing it."
        Remove-Item -LiteralPath $zip -Force
    }

    if (Test-Path -LiteralPath $bundledZip -PathType Leaf) {
        Copy-Item -LiteralPath $bundledZip -Destination $zip -Force
        Assert-BepInExZipHash -Path $zip
        return $bundledZip
    }

    try {
        Invoke-WebRequest -UseBasicParsing -Uri $bepInExDownloadUrl -OutFile $zip
        Assert-BepInExZipHash -Path $zip
        return $bepInExDownloadUrl
    }
    catch {
        throw "BepInEx offline package is missing or corrupted, and the fallback download failed. Expected local package: $zip. Download URL: $bepInExDownloadUrl. Error: $($_.Exception.Message)"
    }
}

$zip = Join-Path $cacheDir $bepInExAssetName
$zipSource = Get-ValidBepInExZip
try {
    Expand-Archive -Force -LiteralPath $zip -DestinationPath $extractDir
}
catch {
    $extractError = $_.Exception.Message
    Write-Warning "BepInEx package extraction failed; rebuilding local package cache and retrying once. Error: $extractError"
    if (Test-Path -LiteralPath $zip -PathType Leaf) {
        Remove-Item -LiteralPath $zip -Force
    }
    if (Test-Path -LiteralPath $extractDir) {
        Remove-Item -Recurse -Force -LiteralPath $extractDir
    }
    New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
    $zipSource = Get-ValidBepInExZip
    try {
        Expand-Archive -Force -LiteralPath $zip -DestinationPath $extractDir
    }
    catch {
        throw "BepInEx package failed to extract after recovery. Source: $zipSource. First error: $extractError. Second error: $($_.Exception.Message)"
    }
}

$backupDir = Join-Path $gameDir ('DTMAPI\backups\bepinex-install-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$installed = New-Object System.Collections.Generic.List[string]

foreach ($item in Get-ChildItem -LiteralPath $extractDir -Force) {
    $target = Join-Path $gameDir $item.Name
    if (Test-Path $target) {
        Copy-Item -Recurse -Force -LiteralPath $target -Destination (Join-Path $backupDir $item.Name)
    }
    Copy-Item -Recurse -Force -LiteralPath $item.FullName -Destination $gameDir
    $installed.Add($item.Name)
}

$missing = @(
    Get-DtmApiBepInExRequiredInstallFiles -GameDir $gameDir |
        Where-Object {
            if (-not (Test-Path -LiteralPath $_.Path -PathType $_.PathType)) {
                $true
            }
            elseif ($_.PathType -eq 'Leaf') {
                try {
                    (Get-Item -LiteralPath $_.Path).Length -le 0
                }
                catch {
                    $true
                }
            }
            else {
                $false
            }
        }
)
if ($missing.Count -gt 0) {
    $details = ($missing | ForEach-Object { "$($_.Label): $($_.Path)" }) -join '; '
    throw "BepInEx install finished copying files, but required files are still missing: $details"
}
if (-not (Test-DtmApiBepInExInstallComplete -GameDir $gameDir)) {
    throw "BepInEx install finished copying files, but Doorstop/BepInEx validation still failed."
}

$summary = @"
BepInEx install
Installed: $(Get-Date -Format o)
Release: $bepInExReleaseTag
Asset: $bepInExAssetName
Source: $zipSource
Sha256: $bepInExAssetSha256
GameDir: $gameDir
BackupDir: $backupDir
Items: $($installed -join ', ')
"@
$summary | Set-Content -LiteralPath (Join-Path $backupDir 'install-summary.txt')
Write-Host $summary
