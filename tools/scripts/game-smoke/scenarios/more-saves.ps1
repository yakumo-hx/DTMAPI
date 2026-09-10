function Wait-MoreSavesFixed12Lifecycle {
    param(
        [Parameter(Mandatory = $true)] [string] $Phase,
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [datetime] $Deadline
    )
    if (@(Get-MoreSavesFixed12PhasePlan | Where-Object { $_.Id -ceq $Phase }).Count -ne 1) {
        throw 'Unsupported MoreSaves lifecycle phase.'
    }
    return Wait-ForLogLineAfterOffsetUntilDeadline -LogPath $LogPath -Offset 0 `
        -Pattern ('Smoke exercise MoreSavesFixed12Lifecycle OK case=MoreSavesFixed12' + $Phase + ';') `
        -Deadline $Deadline -AbortOnFatalInstanceWindow
}

function Get-MoreSavesFixed12PhasePlan {
    return @(
        [ordered]@{ Id = 'EnabledLifecycle'; SaveTestMode = 'ArchiveMutation'; ProductEnabled = $true; ArchiveMutationRequested = $true },
        [ordered]@{ Id = 'DisabledCold'; SaveTestMode = 'NoNativeSave'; ProductEnabled = $false; ArchiveMutationRequested = $false },
        [ordered]@{ Id = 'ReenabledCold'; SaveTestMode = 'NoNativeSave'; ProductEnabled = $true; ArchiveMutationRequested = $false }
    )
}

function Assert-MoreSavesFixed12PhaseOptions {
    param([Parameter(Mandatory = $true)] [System.Collections.IDictionary] $Options)
    $phase = @(Get-MoreSavesFixed12PhasePlan | Where-Object { $_.Id -ceq $Options.Phase })
    if ($phase.Count -ne 1) { throw 'Unsupported MoreSaves phase.' }
    if (-not $Options.StageQaHost -or -not $Options.DirectExe -or -not $Options.SkipInstall -or
        -not $Options.SaveSlotExplicit -or $Options.SaveSlot -ne 7 -or
        [string]::IsNullOrWhiteSpace($Options.FixtureRoot) -or $Options.SaveTestMode -cne $phase[0].SaveTestMode) {
        throw 'MoreSaves phase requires QA, DirectExe, SkipInstall, explicit UI slot 7, one fixture and its declared save mode.'
    }
    if ($Options.OfficialModProfile -cne 'CoreOnly' -or -not $Options.IsolateAllOfficialMods) {
        throw 'MoreSaves phase requires the exact isolated CoreOnly native profile.'
    }
    $extras = @($Options.ExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    if (($phase[0].ProductEnabled -and ($extras.Count -ne 1 -or $extras[0] -cne 'Local.DTMAPI_MoreSaves')) -or
        (-not $phase[0].ProductEnabled -and $extras.Count -ne 0)) {
        throw 'MoreSaves phase has an unexpected official source selection.'
    }
    return [pscustomobject]$phase[0]
}

function Test-MoreSavesFixed12PathWithin {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    $childFull = [System.IO.Path]::GetFullPath($Child)
    $parentFull = [System.IO.Path]::GetFullPath($Parent)
    $parentPrefix =
        $parentFull.TrimEnd('\','/') +
        [System.IO.Path]::DirectorySeparatorChar
    return [string]::Equals(
            $childFull,
            $parentFull,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        $childFull.StartsWith(
            $parentPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)
}

function Write-MoreSavesFixed12Json {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force |
            Out-Null
    }
    $json = $Value | ConvertTo-Json -Depth 12
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Assert-MoreSavesFixed12Fixture {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $driveRoot = [System.IO.Path]::GetPathRoot($Root)
    $userRoot = [Environment]::GetFolderPath(
        [Environment+SpecialFolder]::UserProfile)
    $liveRoot = Join-Path $userRoot (
        'AppData\LocalLow\RedSawGames\DolocTown')
    if ([string]::Equals(
            $Root,
            $driveRoot,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals(
            $Root,
            $userRoot,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals(
            $Root,
            $repo,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        (Test-MoreSavesFixed12PathWithin -Child $Root -Parent $liveRoot) -or
        (Test-MoreSavesFixed12PathWithin -Child $liveRoot -Parent $Root)) {
        throw 'The MoreSaves fixed-12 fixture must be one narrow disposable root outside the repository root, user profile root, drive root, and live Steam AutoCloud tree.'
    }
    if (-not (Test-Path -LiteralPath $Root -PathType Container) -or
        -not (Test-Path -LiteralPath (
            Join-Path $Root 'SAVE') -PathType Container) -or
        -not (Test-Path -LiteralPath (
            Join-Path $Root 'DTMAPI') -PathType Container)) {
        throw 'The MoreSaves fixed-12 fixture must already contain isolated SAVE and DTMAPI directories.'
    }
    foreach ($protectedPath in @(
        $Root,
        (Join-Path $Root 'SAVE'),
        (Join-Path $Root 'DTMAPI'))) {
        $protectedItem = Get-Item -LiteralPath $protectedPath -Force
        if (($protectedItem.Attributes -band
                [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "The MoreSaves fixed-12 fixture rejects reparse points: $protectedPath"
        }
    }
    $nestedReparsePoints = @(
        Get-ChildItem -LiteralPath $Root -Recurse -Force |
        Where-Object {
            ($_.Attributes -band
                [System.IO.FileAttributes]::ReparsePoint) -ne 0
        })
    if ($nestedReparsePoints.Count -ne 0) {
        throw 'The MoreSaves fixed-12 fixture rejects nested reparse points.'
    }
    $markerPath =
        Join-Path $Root '.dtmapi-disposable-save-fixture.json'
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw 'The MoreSaves fixed-12 fixture is missing .dtmapi-disposable-save-fixture.json.'
    }
    try {
        $marker =
            Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath |
            ConvertFrom-Json
    }
    catch {
        throw "The MoreSaves fixed-12 fixture marker is invalid JSON: $($_.Exception.Message)"
    }
    if ([int]$marker.schemaVersion -ne 1 -or
        -not [bool]$marker.disposable -or
        -not [bool]$marker.steamAutoCloudIsolated) {
        throw 'The MoreSaves fixed-12 marker must assert schemaVersion=1, disposable=true, and steamAutoCloudIsolated=true.'
    }
    $autoCloudMarkers = @(
        Get-ChildItem -LiteralPath $Root -Recurse -Force -File `
            -Filter 'steam_autocloud.vdf' -ErrorAction SilentlyContinue)
    if ($autoCloudMarkers.Count -ne 0) {
        throw 'The MoreSaves fixed-12 fixture contains steam_autocloud.vdf and is not isolated from Steam AutoCloud.'
    }
}

function Get-MoreSavesFixed12ArchivePath {
    param(
        [Parameter(Mandatory = $true)] [string] $SaveRoot,
        [Parameter(Mandatory = $true)] [int] $Index,
        [ValidateSet('Current','Prev','Backup')]
        [string] $Role = 'Current'
    )

    $suffix = switch ($Role) {
        'Current' { '' }
        'Prev' { '.prev0' }
        'Backup' { '.bak' }
    }
    return Join-Path $SaveRoot (
        'doloc-save-' + $Index + '.data' + $suffix)
}

function Assert-MoreSavesFixed12InitialArchiveState {
    param([Parameter(Mandatory = $true)] [string] $SaveRoot)

    for ($index = 0; $index -lt 6; $index++) {
        $current = Get-MoreSavesFixed12ArchivePath `
            -SaveRoot $SaveRoot -Index $index
        if (-not (Test-Path -LiteralPath $current -PathType Leaf)) {
            throw "The fixed-12 fixture requires occupied native source slot $index."
        }
    }
    for ($index = 6; $index -lt 12; $index++) {
        foreach ($role in @('Current','Prev','Backup')) {
            $path = Get-MoreSavesFixed12ArchivePath `
                -SaveRoot $SaveRoot -Index $index -Role $role
            if (Test-Path -LiteralPath $path) {
                throw "The fixed-12 fixture requires empty extra slot $index; unexpected role=$role."
            }
        }
        foreach ($legacyName in @(
            "ea-playtest-doloc-archive-$index.data",
            "ea-playtest-doloc-archive-$index-prev.data",
            "ea-playtest-doloc-archive-$index-bak.data")) {
            if (Test-Path -LiteralPath (
                    Join-Path $SaveRoot $legacyName)) {
                throw "The fixed-12 lifecycle fixture must start after legacy migration; found $legacyName."
            }
        }
    }
}

function Get-MoreSavesFixed12SaveSnapshot {
    param([Parameter(Mandatory = $true)] [string] $SaveRoot)

    $savePrefix =
        [System.IO.Path]::GetFullPath($SaveRoot).TrimEnd('\','/') +
        [System.IO.Path]::DirectorySeparatorChar
    $entries = @(
        Get-ChildItem -LiteralPath $SaveRoot -Recurse -Force -File |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = $_.FullName.Substring(
                $savePrefix.Length).Replace('\','/')
            [ordered]@{
                RelativePath = $relativePath
                Length = [int64]$_.Length
                Sha256 = (Get-FileHash `
                    -LiteralPath $_.FullName `
                    -Algorithm SHA256).Hash
                LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
            }
        })
    return $entries
}

function Convert-MoreSavesFixed12SnapshotToCanonicalJson {
    param([Parameter(Mandatory = $true)] [object[]] $Snapshot)

    return @($Snapshot) | ConvertTo-Json -Depth 5 -Compress
}

function Get-MoreSavesFixed12ProductSnapshot {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        throw "MoreSaves fixed-12 product root is missing: $resolvedRoot"
    }
    $rootItem = Get-Item -LiteralPath $resolvedRoot -Force
    $allEntries = @(
        Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force)
    if (($rootItem.Attributes -band
            [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        @($allEntries | Where-Object {
            ($_.Attributes -band
                [System.IO.FileAttributes]::ReparsePoint) -ne 0
        }).Count -ne 0) {
        throw "MoreSaves fixed-12 product root contains a reparse point: $resolvedRoot"
    }
    $prefix = $resolvedRoot.TrimEnd('\','/') +
        [System.IO.Path]::DirectorySeparatorChar
    return @(
        $allEntries |
        Where-Object { -not $_.PSIsContainer } |
        Sort-Object FullName |
        ForEach-Object {
            [ordered]@{
                RelativePath = $_.FullName.Substring(
                    $prefix.Length).Replace('\','/')
                Length = [int64]$_.Length
                Sha256 = (Get-FileHash `
                    -LiteralPath $_.FullName `
                    -Algorithm SHA256).Hash
                LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
            }
        })
}

function Convert-MoreSavesFixed12ProductIdentityToCanonicalJson {
    param([Parameter(Mandatory = $true)] [object[]] $Snapshot)

    return @($Snapshot | ForEach-Object {
        [ordered]@{
            RelativePath = [string]$_.RelativePath
            Length = [int64]$_.Length
            Sha256 = [string]$_.Sha256
        }
    }) | ConvertTo-Json -Depth 5 -Compress
}

function Get-MoreSavesFixed12FileSnapshot {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
        throw "MoreSaves fixed-12 required file is missing: $resolvedPath"
    }
    $item = Get-Item -LiteralPath $resolvedPath -Force
    if (($item.Attributes -band
            [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "MoreSaves fixed-12 required file is a reparse point: $resolvedPath"
    }
    return [ordered]@{
        Path = $resolvedPath
        Length = [int64]$item.Length
        Sha256 = (Get-FileHash `
            -LiteralPath $resolvedPath `
            -Algorithm SHA256).Hash
        LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
    }
}

function Assert-MoreSavesFixed12LoadedSource {
    param(
        [Parameter(Mandatory = $true)] [object] $Phase,
        [Parameter(Mandatory = $true)] [string] $SmokeEvidence,
        [Parameter(Mandatory = $true)] [string] $ProductRoot
    )

    $logPath = Join-Path $SmokeEvidence 'DTMAPI-latest.log'
    if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
        throw "MoreSaves fixed-12 $($Phase.Id) has no copied DTMAPI startup log."
    }
    $logText = Get-Content -Raw -Encoding UTF8 -LiteralPath $logPath
    $ownerPrefix =
        'Code mod load-source owner=DTMAPI.MoreSavesMod;'
    $profileSummaryPath = Join-Path $SmokeEvidence (
        'official-mod-profile-summary.json')
    if (-not (Test-Path -LiteralPath $profileSummaryPath -PathType Leaf)) {
        throw "MoreSaves fixed-12 $($Phase.Id) has no official Mod profile summary."
    }
    $profileSummary = Get-Content -Raw -Encoding UTF8 `
        -LiteralPath $profileSummaryPath | ConvertFrom-Json
    $profileId = 'Local.DTMAPI_MoreSaves'
    $profileEnabled = @($profileSummary.EnabledIds) -ccontains $profileId
    $profileDisabled = @($profileSummary.DisabledIds) -ccontains $profileId
    if (-not [bool]$profileSummary.Applied -or
        ([bool]$Phase.ProductEnabled -and
         (-not $profileEnabled -or $profileDisabled)) -or
        (-not [bool]$Phase.ProductEnabled -and
         ($profileEnabled -or -not $profileDisabled))) {
        throw "MoreSaves fixed-12 $($Phase.Id) did not apply the exact official enablement state for $profileId."
    }
    if ([bool]$Phase.ProductEnabled) {
        $expectedDll = Join-Path $ProductRoot (
            'Content\DTMAPI\DTMAPI.MoreSaves.dll')
        $sourceReceipt =
            'source=Local; workshopId=none; root=' +
            [System.IO.Path]::GetFullPath($ProductRoot) +
            '; dll=' + [System.IO.Path]::GetFullPath($expectedDll) + '.'
        $ownerLines = @($logText -split '\r?\n' | Where-Object { $_.Contains($ownerPrefix) })
        if ($ownerLines.Count -ne 1 -or -not $ownerLines[0].Contains($sourceReceipt)) {
            throw "MoreSaves fixed-12 $($Phase.Id) did not load the exact official Local product root."
        }
    }
    elseif ($logText.Contains($ownerPrefix)) {
        throw 'MoreSaves fixed-12 DisabledCold loaded a product that the exact official profile disabled.'
    }
}

function Assert-MoreSavesFixed12PostLifecycleState {
    param(
        [Parameter(Mandatory = $true)] [string] $SaveRoot,
        [Parameter(Mandatory = $true)] [object[]] $Initial,
        [Parameter(Mandatory = $true)] [object[]] $Current
    )

    $initialByPath = @{}
    foreach ($entry in $Initial) {
        $initialByPath[[string]$entry.RelativePath] = $entry
    }
    $currentByPath = @{}
    foreach ($entry in $Current) {
        $currentByPath[[string]$entry.RelativePath] = $entry
    }
    for ($index = 0; $index -lt 6; $index++) {
        $relative = "doloc-save-$index.data"
        if (-not $initialByPath.ContainsKey($relative) -or
            -not $currentByPath.ContainsKey($relative) -or
            [string]$initialByPath[$relative].Sha256 -cne
                [string]$currentByPath[$relative].Sha256) {
            throw "The enabled lifecycle changed native source slot $index."
        }
    }
    if (-not (Test-Path -LiteralPath (
            Get-MoreSavesFixed12ArchivePath `
                -SaveRoot $SaveRoot -Index 6) -PathType Leaf) -or
        (Test-Path -LiteralPath (
            Get-MoreSavesFixed12ArchivePath `
                -SaveRoot $SaveRoot -Index 7) -PathType Leaf) -or
        -not (Test-Path -LiteralPath (
            Get-MoreSavesFixed12ArchivePath `
                -SaveRoot $SaveRoot -Index 7 -Role Backup) -PathType Leaf)) {
        throw 'The enabled lifecycle did not retain created slot 7 and move the deleted slot-8 duplicate to its native .bak role.'
    }
    for ($index = 8; $index -lt 12; $index++) {
        foreach ($role in @('Current','Prev','Backup')) {
            if (Test-Path -LiteralPath (
                    Get-MoreSavesFixed12ArchivePath `
                        -SaveRoot $SaveRoot `
                        -Index $index `
                        -Role $role)) {
                throw "The enabled lifecycle unexpectedly populated slot $index role=$role."
            }
        }
    }
}

function Get-MoreSavesFixed12SmokeEvidencePath {
    param([Parameter(Mandatory = $true)] [object[]] $Output)

    $matches = @(
        $Output |
        ForEach-Object {
            $line = [string]$_
            if ($line.StartsWith(
                    'DTMAPI_SMOKE_EVIDENCE_PATH=',
                    [System.StringComparison]::Ordinal)) {
                $line.Substring(
                    'DTMAPI_SMOKE_EVIDENCE_PATH='.Length).Trim()
            }
        } |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        } |
        Select-Object -Unique)
    if ($matches.Count -ne 1 -or
        -not (Test-Path -LiteralPath $matches[0] -PathType Container)) {
        throw 'MoreSaves fixed-12 smoke did not emit exactly one valid evidence path marker.'
    }
    return [System.IO.Path]::GetFullPath($matches[0])
}

function Assert-MoreSavesFixed12SmokeResult {
    param(
        [Parameter(Mandatory = $true)] [object] $Phase,
        [Parameter(Mandatory = $true)] [object] $Result,
        [Parameter(Mandatory = $true)] [string] $ProductRoot
    )

    foreach ($gate in @(
        'OfficialModProfileGate',
        'SaveFixtureIsolation',
        'SaveFixtureIsolationCleanup',
        'NoFatalInstanceWindow',
        'ProcessExited',
        'MoreSavesFixed12Lifecycle')) {
        if ([string]$Result.$gate -cne 'Passed') {
            throw "MoreSaves fixed-12 $($Phase.Id) gate $gate failed: $($Result.$gate)."
        }
    }
    if ([string]$Result.RunStatus -cne 'Passed' -or
        [string]$Result.SaveTestMode -cne [string]$Phase.SaveTestMode -or
        [string]$Result.MoreSavesFixed12AcceptancePhase -cne
            [string]$Phase.Id -or
        [string]$Result.MoreSavesFixed12OfficialLocalRootMode -cne
            'LiveOfficialUploadRoot' -or
        [string]$Result.MoreSavesFixed12OfficialLocalProductRoot -cne
            [System.IO.Path]::GetFullPath($ProductRoot) -or
        [string]$Result.DisposableSaveFixtureCleanup -cne
            'RetainedForBoundedFollowUp' -or
        -not [bool]$Result.DisposableSaveFixtureRetentionRequested -or
        -not [bool]$Result.OfficialModProfileRestored -or
        [bool]$Result.PlayerArchiveWritebackPerformed) {
        throw "MoreSaves fixed-12 $($Phase.Id) result did not retain the exact isolated phase contract."
    }
    if ([string]$Phase.SaveTestMode -ceq 'NoNativeSave' -and
        ([string]$Result.PlayerSaveUnchangedBeforeCleanup -cne 'Passed' -or
         [string]$Result.CommittedSidecarsUnchangedBeforeCleanup -cne 'Passed')) {
        throw "MoreSaves fixed-12 $($Phase.Id) did not prove the NoNativeSave archive and committed-sidecar gates before cleanup."
    }
}

function Remove-MoreSavesFixed12Fixture {
    param([Parameter(Mandatory = $true)] [string] $Root)

    Assert-MoreSavesFixed12Fixture -Root $Root
    Remove-Item -LiteralPath $Root -Recurse -Force
    if (Test-Path -LiteralPath $Root) {
        throw 'MoreSaves fixed-12 cleanup did not remove the exact marked disposable fixture root.'
    }
}
