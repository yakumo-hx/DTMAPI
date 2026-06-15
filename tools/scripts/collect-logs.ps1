param(
    [string] $CaseId = 'MANUAL',
    [string] $OutputDirectory,
    [switch] $IncludeRuntimeEvidence
)

. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
if ($OutputDirectory) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $evidence = (Resolve-Path -LiteralPath $OutputDirectory).Path
}
else {
    $evidence = New-EvidenceDir -RepoRoot $repo -CaseId $CaseId
}

function Get-SteamRootCandidates {
    $roots = New-Object System.Collections.Generic.List[string]
    foreach ($registryPath in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $steamPath = (Get-ItemProperty -Path $registryPath -ErrorAction Stop).SteamPath
            if ($steamPath -and (Test-Path $steamPath)) {
                $resolved = (Resolve-Path $steamPath).Path
                if (-not $roots.Contains($resolved)) {
                    $roots.Add($resolved)
                }
            }
        }
        catch {
        }
    }

    return @($roots)
}

function Get-SteamLibraryRoots {
    param(
        [string] $SteamRoot
    )

    $roots = New-Object System.Collections.Generic.List[string]
    if ($SteamRoot -and (Test-Path $SteamRoot)) {
        $roots.Add((Resolve-Path $SteamRoot).Path)
    }

    $libraryFile = Join-Path $SteamRoot 'steamapps\libraryfolders.vdf'
    if (Test-Path $libraryFile) {
        $text = Get-Content -Raw -LiteralPath $libraryFile
        foreach ($match in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
            $path = $match.Groups[1].Value.Replace('\\', '\')
            if (Test-Path $path) {
                $resolved = (Resolve-Path $path).Path
                if (-not $roots.Contains($resolved)) {
                    $roots.Add($resolved)
                }
            }
        }
    }

    return @($roots)
}

function Copy-SteamLaunchEvidence {
    param(
        [string] $GameDir,
        [string] $Destination
    )

    $info = New-Object System.Collections.Generic.List[string]
    $gameFullPath = [System.IO.Path]::GetFullPath($GameDir).TrimEnd('\')
    foreach ($steamRoot in Get-SteamRootCandidates) {
        foreach ($libraryRoot in Get-SteamLibraryRoots -SteamRoot $steamRoot) {
            $manifest = Join-Path $libraryRoot 'steamapps\appmanifest_2285550.acf'
            $candidate = Join-Path $libraryRoot 'steamapps\common\Doloc Town'
            if ((Test-Path $manifest) -and (Test-Path $candidate)) {
                $candidateFullPath = [System.IO.Path]::GetFullPath((Resolve-Path $candidate).Path).TrimEnd('\')
                if ($candidateFullPath.Equals($gameFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $info.Add("SteamRoot=$steamRoot")
                    $info.Add("LibraryRoot=$libraryRoot")
                    $info.Add("AppManifest=$manifest")
                    Copy-Item -Force -LiteralPath $manifest -Destination (Join-Path $Destination 'steam-appmanifest-2285550.acf')
                    break
                }
            }
        }

        $steamLogDir = Join-Path $steamRoot 'logs'
        if (Test-Path $steamLogDir) {
            foreach ($logName in @('console_log.txt', 'bootstrap_log.txt', 'content_log.txt', 'workshop_log.txt')) {
                $logPath = Join-Path $steamLogDir $logName
                if (Test-Path $logPath) {
                    Get-Content -Tail 500 -LiteralPath $logPath -ErrorAction SilentlyContinue | Set-Content -LiteralPath (Join-Path $Destination ("steam-$logName.tail.txt"))
                }
            }
        }
    }

    if ($info.Count -gt 0) {
        $info | Set-Content -LiteralPath (Join-Path $Destination 'steam-info.txt')
    }
}

$paths = @(
    @{ Path = Join-Path $dtmapiDir 'logs\latest.log'; Name = 'DTMAPI-latest.log' },
    @{ Path = Join-Path $gameDir 'BepInEx\LogOutput.log'; Name = 'BepInEx-LogOutput.log' },
    @{ Path = Join-Path $env:USERPROFILE 'AppData\LocalLow\RedSawGames\DolocTown\Player.log'; Name = 'Unity-Player.log' },
    @{ Path = Join-Path $dtmapiDir 'reports\latest-report.txt'; Name = 'latest-report.txt' }
)

foreach ($item in $paths) {
    if (Test-Path $item.Path) {
        Copy-Item -Force -LiteralPath $item.Path -Destination (Join-Path $evidence $item.Name)
    }
}

$dtmapiLogDir = Join-Path $dtmapiDir 'logs'
if (Test-Path $dtmapiLogDir) {
    $historyLogs = @(Get-ChildItem -LiteralPath $dtmapiLogDir -Filter 'latest-*.log' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 10)
    if ($historyLogs.Count -gt 0) {
        $historyDir = Join-Path $evidence 'DTMAPI-log-history'
        New-Item -ItemType Directory -Force -Path $historyDir | Out-Null
        foreach ($log in $historyLogs) {
            Copy-Item -Force -LiteralPath $log.FullName -Destination (Join-Path $historyDir $log.Name)
        }
    }
}

$gameEvidenceRoot = Join-Path $dtmapiDir 'evidence'
if (Test-Path $gameEvidenceRoot) {
    if ($IncludeRuntimeEvidence) {
        Copy-Item -Recurse -Force -LiteralPath $gameEvidenceRoot -Destination (Join-Path $evidence 'DTMAPI-evidence')
    }
    else {
        $summary = New-Object System.Collections.Generic.List[string]
        $summary.Add("RuntimeEvidenceRoot=$gameEvidenceRoot")
        $summary.Add('Skipped=True')
        $summary.Add('Reason=collect-logs.ps1 no longer copies the full runtime evidence tree by default. Use -IncludeRuntimeEvidence when a full screenshot/runtime evidence payload is required.')
        $summary.Add('')
        $summary.Add('Recent runtime evidence folders:')
        $recentRuntimeEvidence = Get-ChildItem -LiteralPath $gameEvidenceRoot -Directory -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 20
        foreach ($item in $recentRuntimeEvidence) {
            $summary.Add(("{0}`t{1:o}`t{2}" -f $item.Name, $item.LastWriteTime, $item.FullName))
        }

        if ($recentRuntimeEvidence.Count -eq 0) {
            $summary.Add('(none)')
        }

        $summary | Set-Content -LiteralPath (Join-Path $evidence 'DTMAPI-evidence-skipped.txt')
    }
}

Copy-SteamLaunchEvidence -GameDir $gameDir -Destination $evidence
Write-ProcessCheck -Path (Join-Path $evidence 'process-check.txt')
Write-FatalWindowCheck -Path (Join-Path $evidence 'fatal-window-check.txt')
$summaryName = if (Test-Path -LiteralPath (Join-Path $evidence 'summary.txt')) { 'collect-summary.txt' } else { 'summary.txt' }
"GameDir=$gameDir`nDtmApiStateDir=$dtmapiDir`nCollected=$(Get-Date -Format o)" | Set-Content -LiteralPath (Join-Path $evidence $summaryName)
try {
    & "$PSScriptRoot\analyze-startup-evidence.ps1" -EvidencePath $evidence -OutputDirectory $evidence -Quiet
}
catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $evidence 'startup-analysis-error.txt')
}
Write-Host $evidence
