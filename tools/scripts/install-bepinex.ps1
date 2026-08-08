param(
    [switch] $Force
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$stateDir = Resolve-DtmApiStateDir -GameDir $gameDir
Assert-DtmApiGameNotRunning -GameDir $gameDir -Operation 'BepInEx install'

$coreDll = Join-Path $gameDir 'BepInEx\core\BepInEx.dll'
if ((Test-DtmApiBepInExInstallComplete -GameDir $gameDir) -and -not $Force) {
    Write-Host "BepInEx already installed and complete: $coreDll"
    exit 0
}

$bepInExReleaseTag = 'v5.4.23.5'
$bepInExAssetName = 'BepInEx_win_x64_5.4.23.5.zip'
$bepInExAssetSha256 = '82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4'
$bepInExDownloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/$bepInExReleaseTag/$bepInExAssetName"
$packagedZip = Join-Path $repo ".tools\bepinex\$bepInExAssetName"
$developerSourceZip = Join-Path $repo "tools\release\bootstrap\$bepInExAssetName"
$sessionToken = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$sessionRoot = Join-Path $stateDir ('.bepinex-install-' + $sessionToken)
$downloadZip = Join-Path $sessionRoot $bepInExAssetName
$backupDir = Join-Path $stateDir ('backups\bepinex-install-' + $sessionToken)

function Test-BepInExZipHash {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }
    try {
        return [string]::Equals(
            (Get-DtmApiFileSha256 -Path $Path),
            $bepInExAssetSha256,
            [System.StringComparison]::OrdinalIgnoreCase)
    }
    catch {
        return $false
    }
}

function Get-BepInExPackageSource {
    foreach ($candidate in @(
        [pscustomobject]@{ Kind = 'bundled-package'; Path = $packagedZip },
        [pscustomobject]@{ Kind = 'developer-source-tree'; Path = $developerSourceZip }
    )) {
        if (Test-BepInExZipHash -Path $candidate.Path) {
            return $candidate
        }
        if (Test-Path -LiteralPath $candidate.Path -PathType Leaf) {
            Write-Warning "Ignoring BepInEx ZIP with the wrong SHA-256: $($candidate.Path)"
        }
    }

    try {
        [System.IO.Directory]::CreateDirectory($sessionRoot) | Out-Null
        Invoke-DtmApiFileDownload -Uri $bepInExDownloadUrl -DestinationPath $downloadZip | Out-Null
        if (-not (Test-BepInExZipHash -Path $downloadZip)) {
            $actualHash = if (Test-Path -LiteralPath $downloadZip -PathType Leaf) { Get-DtmApiFileSha256 -Path $downloadZip } else { '(missing)' }
            throw "Downloaded BepInEx SHA-256 mismatch. Expected=$bepInExAssetSha256 Actual=$actualHash"
        }
        return [pscustomobject]@{ Kind = 'official-network-fallback'; Path = $downloadZip }
    }
    catch {
        throw "The bundled BepInEx package is missing or invalid, and the fixed official download failed. Bundled path: $packagedZip. Download URL: $bepInExDownloadUrl. Error: $($_.Exception.Message)"
    }
}

function Expand-BepInExPackageWithRetry {
    param([Parameter(Mandatory = $true)] [string] $ZipPath)

    $firstError = ''
    foreach ($attempt in 1..2) {
        $extractDir = Join-Path $sessionRoot ("extract-$attempt")
        [System.IO.Directory]::CreateDirectory($extractDir) | Out-Null
        try {
            Expand-DtmApiZipArchive -LiteralPath $ZipPath -DestinationPath $extractDir | Out-Null
            return $extractDir
        }
        catch {
            if ($attempt -eq 1) {
                $firstError = $_.Exception.Message
                Write-Warning "BepInEx extraction failed once; retrying the same SHA-256-verified ZIP in a fresh directory. Error: $firstError"
            }
            else {
                throw "BepInEx package failed to extract twice without changing the verified source. First error: $firstError. Second error: $($_.Exception.Message)"
            }
        }
    }
}

function Get-BepInExRelativeFilePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    $prefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
    if (-not $pathFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "BepInEx extracted file escaped its root: $pathFull"
    }
    return $pathFull.Substring($prefix.Length)
}

function Remove-BepInExSessionBestEffort {
    if (-not (Test-Path -LiteralPath $sessionRoot)) {
        return
    }
    try {
        $sessionFull = [System.IO.Path]::GetFullPath($sessionRoot)
        $stateFull = [System.IO.Path]::GetFullPath($stateDir)
        if (-not (Test-DtmApiPathIsSameOrChild -Child $sessionFull -Parent $stateFull) -or
            [string]::Equals($sessionFull.TrimEnd('\', '/'), $stateFull.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase) -or
            [System.IO.Path]::GetFileName($sessionFull) -notlike '.bepinex-install-*') {
            throw "BepInEx session cleanup escaped its state boundary: $sessionFull"
        }
        Remove-Item -LiteralPath $sessionFull -Recurse -Force
    }
    catch {
        Write-Warning "Could not remove temporary BepInEx session: $($_.Exception.Message)"
    }
}

$source = $null
$extractRoot = ''
$createdFiles = New-Object 'System.Collections.Generic.List[string]'
$overwrittenFiles = New-Object 'System.Collections.Generic.List[object]'
$installed = New-Object 'System.Collections.Generic.List[string]'
try {
    [System.IO.Directory]::CreateDirectory($sessionRoot) | Out-Null
    $source = Get-BepInExPackageSource
    $extractRoot = Expand-BepInExPackageWithRetry -ZipPath ([string]$source.Path)
    $sourceFiles = @(Get-ChildItem -LiteralPath $extractRoot -File -Recurse -Force | Sort-Object FullName)
    if ($sourceFiles.Count -eq 0) {
        throw "BepInEx archive extracted no files: $($source.Path)"
    }

    [System.IO.Directory]::CreateDirectory($backupDir) | Out-Null
    foreach ($sourceFile in $sourceFiles) {
        $relativePath = Get-BepInExRelativeFilePath -Root $extractRoot -Path $sourceFile.FullName
        $targetPath = [System.IO.Path]::GetFullPath((Join-Path $gameDir $relativePath))
        if (-not (Test-DtmApiPathIsSameOrChild -Child $targetPath -Parent $gameDir)) {
            throw "BepInEx target escaped the game directory: $targetPath"
        }
        if (Test-Path -LiteralPath $targetPath -PathType Container) {
            throw "BepInEx file target is occupied by a directory: $targetPath"
        }

        if (Test-Path -LiteralPath $targetPath -PathType Leaf) {
            $backupPath = Join-Path $backupDir $relativePath
            [System.IO.Directory]::CreateDirectory((Split-Path -Parent $backupPath)) | Out-Null
            [System.IO.File]::Copy($targetPath, $backupPath, $true)
            $overwrittenFiles.Add([pscustomobject]@{ Target = $targetPath; Backup = $backupPath }) | Out-Null
        }
        else {
            $createdFiles.Add($targetPath) | Out-Null
        }

        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $targetPath)) | Out-Null
        [System.IO.File]::Copy($sourceFile.FullName, $targetPath, $true)
        $installed.Add($relativePath.Replace('\', '/')) | Out-Null
    }

    $missing = @(
        Get-DtmApiBepInExRequiredInstallFiles -GameDir $gameDir |
            Where-Object {
                if (-not (Test-Path -LiteralPath $_.Path -PathType $_.PathType)) {
                    $true
                }
                elseif ($_.PathType -eq 'Leaf') {
                    try { (Get-Item -LiteralPath $_.Path).Length -le 0 } catch { $true }
                }
                else {
                    $false
                }
            }
    )
    if ($missing.Count -gt 0) {
        $details = ($missing | ForEach-Object { "$($_.Label): $($_.Path)" }) -join '; '
        throw "BepInEx install copied its owned files, but required files are still missing: $details"
    }
    if (-not (Test-DtmApiBepInExInstallComplete -GameDir $gameDir)) {
        throw 'BepInEx install copied its owned files, but Doorstop/BepInEx validation still failed.'
    }
}
catch {
    $installError = $_
    foreach ($createdPath in @($createdFiles.ToArray() | Sort-Object Length -Descending)) {
        try {
            if (Test-Path -LiteralPath $createdPath -PathType Leaf) {
                Remove-Item -LiteralPath $createdPath -Force
            }
        }
        catch {
            Write-Warning "Could not remove a newly created BepInEx file during rollback: $createdPath. $($_.Exception.Message)"
        }
    }
    foreach ($record in $overwrittenFiles) {
        try {
            [System.IO.File]::Copy([string]$record.Backup, [string]$record.Target, $true)
        }
        catch {
            Write-Warning "Could not restore an overwritten BepInEx file during rollback: $($record.Target). $($_.Exception.Message)"
        }
    }
    throw $installError
}
finally {
    Remove-BepInExSessionBestEffort
}

$summary = @"
BepInEx install
Installed: $(Get-Date -Format o)
Release: $bepInExReleaseTag
Asset: $bepInExAssetName
SourceKind: $($source.Kind)
Source: $($source.Path)
Sha256: $bepInExAssetSha256
GameDir: $gameDir
BackupDir: $backupDir
OwnedFilesCopied: $($installed.Count)
Items: $($installed -join ', ')
"@
$summary | Set-Content -LiteralPath (Join-Path $backupDir 'install-summary.txt')
Write-Host $summary
