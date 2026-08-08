$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\player-common.ps1"

function Copy-DtmApiVerifiedLog {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    $before = Get-Item -LiteralPath $Source -Force -ErrorAction Stop
    $beforeLength = [int64]$before.Length
    $beforeWrite = $before.LastWriteTimeUtc
    [System.IO.File]::Copy($before.FullName, $Destination, $false)
    $after = Get-Item -LiteralPath $Source -Force -ErrorAction Stop
    $copied = Get-Item -LiteralPath $Destination -Force -ErrorAction Stop
    if ([int64]$after.Length -ne $beforeLength -or $after.LastWriteTimeUtc -ne $beforeWrite) {
        throw "Log changed while it was being copied: $Source"
    }
    if ([int64]$copied.Length -ne $beforeLength) {
        throw "Copied log length is incomplete: $Source"
    }
    $sourceHash = Get-DtmApiFileSha256 -Path $Source
    $destinationHash = Get-DtmApiFileSha256 -Path $Destination
    if ($sourceHash -ne $destinationHash) {
        throw "Copied log SHA-256 does not match its source: $Source"
    }
    return [pscustomobject]@{
        Source = $Source
        Destination = $Destination
        Length = $beforeLength
        LastWriteTimeUtc = $beforeWrite.ToString('o')
        Sha256 = $sourceHash
    }
}

$staging = ''
$exitCode = 1

try {
    $packageRoot = Get-DtmApiPackageRoot
    $gameDir = Resolve-DtmApiGameDirectory -PackageRoot $packageRoot
    Assert-DtmApiGameNotRunning

    $outputRoot = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_LOG_EXPORT_DIR)) {
        [System.IO.Path]::GetFullPath($env:DTMAPI_LOG_EXPORT_DIR)
    }
    else {
        [Environment]::GetFolderPath('Desktop')
    }
    if ([string]::IsNullOrWhiteSpace($outputRoot)) {
        $outputRoot = $packageRoot
    }
    [System.IO.Directory]::CreateDirectory($outputRoot) | Out-Null

    $token = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $final = Join-Path $outputRoot ("DTMAPI-logs-$token")
    $staging = Join-Path $outputRoot (".DTMAPI-logs-$token.staging")
    [System.IO.Directory]::CreateDirectory($staging) | Out-Null

    $selected = New-Object 'System.Collections.Generic.List[object]'
    $dtmapiLogs = Join-Path $gameDir 'DTMAPI\logs'
    if (Test-Path -LiteralPath $dtmapiLogs -PathType Container) {
        $history = @(Get-ChildItem -LiteralPath $dtmapiLogs -File -Force -ErrorAction Stop |
            Where-Object { $_.Name -eq 'latest.log' -or $_.Name -like 'latest-*.log' } |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 10)
        foreach ($file in $history) {
            $selected.Add([pscustomobject]@{ Source = $file.FullName; Folder = 'DTMAPI'; Name = $file.Name })
        }
    }

    $optional = @(
        [pscustomobject]@{ Source = (Join-Path $gameDir 'BepInEx\LogOutput.log'); Folder = 'BepInEx'; Name = 'LogOutput.log' },
        [pscustomobject]@{ Source = (Join-Path $env:USERPROFILE 'AppData\LocalLow\RedSawGames\DolocTown\Player.log'); Folder = 'Unity'; Name = 'Player.log' },
        [pscustomobject]@{ Source = (Join-Path $env:USERPROFILE 'AppData\LocalLow\RedSawGames\DolocTown\Player-prev.log'); Folder = 'Unity'; Name = 'Player-prev.log' }
    )
    foreach ($entry in $optional) {
        if (Test-Path -LiteralPath $entry.Source -PathType Leaf) {
            $selected.Add($entry)
        }
    }

    $records = New-Object 'System.Collections.Generic.List[object]'
    foreach ($entry in $selected) {
        $folder = Join-Path $staging $entry.Folder
        [System.IO.Directory]::CreateDirectory($folder) | Out-Null
        $destination = Join-Path $folder $entry.Name
        $records.Add((Copy-DtmApiVerifiedLog -Source $entry.Source -Destination $destination))
    }

    $summary = New-Object 'System.Collections.Generic.List[string]'
    $summary.Add("Collected=$([DateTime]::UtcNow.ToString('o'))")
    $summary.Add("GameDir=$gameDir")
    $summary.Add("DTMAPILogLimit=10")
    $summary.Add('FileSizeLimit=none')
    $summary.Add("FilesCopied=$($records.Count)")
    $summary.Add('AllSelectedFilesVerified=True')
    $summary.Add('')
    foreach ($record in $records) {
        $summary.Add("$($record.Length)`t$($record.Sha256)`t$($record.Source)")
    }
    [System.IO.File]::WriteAllLines((Join-Path $staging 'summary.txt'), [string[]]$summary, [System.Text.UTF8Encoding]::new($true))

    [System.IO.Directory]::Move($staging, $final)
    $staging = ''
    Write-Host "[OK] Complete DTMAPI log bundle: $final" -ForegroundColor Green
    Write-Host "[INFO] Copied $($records.Count) complete file(s); newest DTMAPI logs are limited by count only, never by size."
    $exitCode = 0
}
catch {
    Write-Host "[ERROR] DTMAPI log collection failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host '[HELP] Close Doloc Town and try again. No partial support bundle was published.' -ForegroundColor Yellow
    Write-DtmApiPlayerHelp
    $exitCode = 1
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($staging) -and (Test-Path -LiteralPath $staging -PathType Container)) {
        try {
            Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-Host "[WARN] Could not remove failed log staging directory: $staging" -ForegroundColor Yellow
        }
    }
}

exit $exitCode
