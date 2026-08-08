param(
    [Parameter(Mandatory = $true)]
    [string] $DisposableSaveFixtureRoot,
    [ValidateRange(1, 3600)]
    [int] $TimeoutSeconds = 300,
    [string] $OutputRoot = '',
    [string] $ExpectedLocalProductRoot = '',
    [switch] $SkipBuild,
    [switch] $RetainDisposableSaveFixtureOnSuccess,
    [switch] $PlanOnly,
    [switch] $ValidateOnly
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ($PlanOnly -and $ValidateOnly) {
    throw '-PlanOnly and -ValidateOnly are mutually exclusive.'
}

$fixtureRoot =
    [System.IO.Path]::GetFullPath(
        $DisposableSaveFixtureRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo (
        'docs\debug\evidence\MORESAVES-FIXED12\' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
        [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$livePersistentRoot = [System.IO.Path]::GetFullPath(
    (Join-Path (
        [Environment]::GetFolderPath('UserProfile')) `
        'AppData\LocalLow\RedSawGames\DolocTown'))
$liveOfficialProductRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $livePersistentRoot 'MODS\DTMAPI_MoreSaves'))
$liveOfficialProfilePath = [System.IO.Path]::GetFullPath(
    (Join-Path $livePersistentRoot 'SAVE\mod_infos.json'))
$expectedLocalProductRootResolved =
    if ([string]::IsNullOrWhiteSpace($ExpectedLocalProductRoot)) {
        ''
    }
    else {
        [System.IO.Path]::GetFullPath($ExpectedLocalProductRoot)
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
        if (-not $logText.Contains($ownerPrefix) -or
            -not $logText.Contains($sourceReceipt)) {
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

Assert-MoreSavesFixed12Fixture -Root $fixtureRoot
if ((Test-MoreSavesFixed12PathWithin `
        -Child $OutputRoot -Parent $fixtureRoot) -or
    (Test-MoreSavesFixed12PathWithin `
        -Child $fixtureRoot -Parent $OutputRoot)) {
    throw 'The output root and disposable fixture root must not overlap.'
}
$saveRoot = Join-Path $fixtureRoot 'SAVE'
Assert-MoreSavesFixed12InitialArchiveState -SaveRoot $saveRoot

$liveProductInitialSnapshot = @()
$expectedProductSnapshot = @()
$liveProfileInitialSnapshot = $null
$officialProductBinding = [ordered]@{
    LiveOfficialProductRoot = $liveOfficialProductRoot
    ExpectedCandidateProductRoot =
        $expectedLocalProductRootResolved
    RootsDistinct = $false
    FileCount = 0
    CandidateIdentityExact = $false
    LiveTreeUnchanged = $null
    LiveProfileUnchanged = $null
    State = 'NotEvaluated'
}
if (-not $PlanOnly -and -not $ValidateOnly) {
    if ([string]::IsNullOrWhiteSpace(
            $expectedLocalProductRootResolved)) {
        throw 'A real MoreSaves fixed-12 run requires -ExpectedLocalProductRoot bound to an independent frozen candidate package.'
    }
    if ([string]::Equals(
            $liveOfficialProductRoot,
            $expectedLocalProductRootResolved,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The expected MoreSaves candidate root must be independent from the live official upload root.'
    }
    $liveProductInitialSnapshot = @(
        Get-MoreSavesFixed12ProductSnapshot `
            -Root $liveOfficialProductRoot)
    $expectedProductSnapshot = @(
        Get-MoreSavesFixed12ProductSnapshot `
            -Root $expectedLocalProductRootResolved)
    $liveIdentity =
        Convert-MoreSavesFixed12ProductIdentityToCanonicalJson `
            -Snapshot $liveProductInitialSnapshot
    $expectedIdentity =
        Convert-MoreSavesFixed12ProductIdentityToCanonicalJson `
            -Snapshot $expectedProductSnapshot
    if ($liveIdentity -cne $expectedIdentity) {
        throw 'The live official MoreSaves package does not match the independent frozen candidate root.'
    }
    $manifestPath = Join-Path $liveOfficialProductRoot (
        'Content\DTMAPI\manifest.json')
    $manifest = Get-Content -Raw -Encoding UTF8 `
        -LiteralPath $manifestPath | ConvertFrom-Json
    if ([string]$manifest.UniqueID -cne 'DTMAPI.MoreSavesMod' -or
        [string]$manifest.Version -cne '1.0.1' -or
        [string]$manifest.MinimumDTMApiVersion -cne '0.6.0' -or
        [string]$manifest.CodeModKind -cne 'Advanced' -or
        [string]$manifest.EntryDll -cne
            'Content/DTMAPI/DTMAPI.MoreSaves.dll') {
        throw 'The exact candidate comparison resolved a non-current MoreSaves manifest.'
    }
    $liveProfileInitialSnapshot =
        Get-MoreSavesFixed12FileSnapshot `
            -Path $liveOfficialProfilePath
    $officialProductBinding.RootsDistinct = $true
    $officialProductBinding.FileCount =
        $liveProductInitialSnapshot.Count
    $officialProductBinding.CandidateIdentityExact = $true
    $officialProductBinding.State = 'PreflightPassed'
}

$phases = @(
    [ordered]@{
        Id = 'EnabledLifecycle'
        SaveTestMode = 'ArchiveMutation'
        ProductEnabled = $true
        ArchiveMutationRequested = $true
    },
    [ordered]@{
        Id = 'DisabledCold'
        SaveTestMode = 'NoNativeSave'
        ProductEnabled = $false
        ArchiveMutationRequested = $false
    },
    [ordered]@{
        Id = 'ReenabledCold'
        SaveTestMode = 'NoNativeSave'
        ProductEnabled = $true
        ArchiveMutationRequested = $false
    })

New-Item -ItemType Directory -Path $OutputRoot -Force |
    Out-Null
$initialSnapshot = @(Get-MoreSavesFixed12SaveSnapshot -SaveRoot $saveRoot)
Write-MoreSavesFixed12Json `
    -Path (Join-Path $OutputRoot 'initial-save-snapshot.json') `
    -Value ([ordered]@{
        CapturedAt = (Get-Date).ToString('o')
        Entries = @($initialSnapshot)
    })

$receipt = [ordered]@{
    SchemaVersion = 1
    Authority = 'moresaves-fixed12-official-native-three-process-acceptance'
    FixtureRoot = $fixtureRoot
    SaveSlot = 7
    OfficialModProfile = 'CoreOnly'
    OfficialLocalProductSource = 'Local.DTMAPI_MoreSaves'
    OfficialLocalProductBinding = $officialProductBinding
    PhaseOrder = @($phases | ForEach-Object { $_.Id })
    Phases = @($phases)
    InitialSnapshot = [System.IO.Path]::GetFullPath(
        (Join-Path $OutputRoot 'initial-save-snapshot.json'))
    FinalCleanup = if ($RetainDisposableSaveFixtureOnSuccess) {
        'RetainExplicitly'
    }
    else {
        'DeleteEntireMarkedDisposableFixtureAfterFinalSnapshot'
    }
    Status = 'planned'
    OutputRoot = $OutputRoot
    Results = @()
}
$receiptPath = Join-Path $OutputRoot 'moresaves-fixed12-acceptance.json'
Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt

if ($PlanOnly) {
    Write-Output (
        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_PLAN=' +
        [System.IO.Path]::GetFullPath($receiptPath))
    return
}

if ($ValidateOnly) {
    $projections = @()
    foreach ($phase in $phases) {
        $arguments = @(
            '-NoProfile',
            '-ExecutionPolicy',
            'Bypass',
            '-File',
            (Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
            '-StageQaHost',
            '-DirectExe',
            '-SkipInstall',
            '-SkipBuild',
            '-SaveSlot',
            '7',
            '-SaveTestMode',
            [string]$phase.SaveTestMode,
            '-DisposableSaveFixtureRoot',
            $fixtureRoot,
            '-OfficialModProfile',
            'CoreOnly',
            '-IsolateAllOfficialMods',
            '-MoreSavesFixed12AcceptancePhase',
            [string]$phase.Id,
            '-RetainDisposableSaveFixtureOnSuccess',
            '-ValidateSaveTestModeOnly')
        if ([bool]$phase.ProductEnabled) {
            $arguments += @(
                '-OfficialModProfileExtraEnabledIds',
                'Local.DTMAPI_MoreSaves')
        }
        $projectionOutput = @(& powershell.exe @arguments 2>&1)
        $projectionExit = $LASTEXITCODE
        if ($projectionExit -ne 0) {
            throw "MoreSaves fixed-12 $($phase.Id) validation projection failed with exit ${projectionExit}: $($projectionOutput -join [Environment]::NewLine)"
        }
        $projection =
            ($projectionOutput -join [Environment]::NewLine) |
            ConvertFrom-Json
        if (-not [bool]$projection.Passed -or
            [string]$projection.SaveTestMode -cne
                [string]$phase.SaveTestMode -or
            [string]$projection.MoreSavesFixed12AcceptancePhase -cne
                [string]$phase.Id -or
            [string]$projection.MoreSavesFixed12OfficialLocalRootMode -cne
                'LiveOfficialUploadRoot' -or
            [string]$projection.MoreSavesFixed12OfficialLocalProductRoot -cne
                $liveOfficialProductRoot -or
            [bool]$projection.NativeSaveRouteRequested -or
            [bool]$projection.ArchiveMutationRouteRequested -ne
                [bool]$phase.ArchiveMutationRequested -or
            -not [bool]$projection.SteamAutoCloudIsolated -or
            [bool]$projection.DisposableSaveFixtureCleanupRequested -or
            -not [bool]$projection.DisposableSaveFixtureRetentionRequested) {
            throw "MoreSaves fixed-12 $($phase.Id) validation projection was not exact."
        }
        $projections += $projection
    }
    $receipt.Status = 'validated'
    $receipt.Results = @($projections)
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    Write-Output (
        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_VALIDATION=' +
        [System.IO.Path]::GetFullPath($receiptPath))
    return
}

$lockAcquired = $false
try {
    & "$PSScriptRoot\wait-runtime-lock.ps1" `
        -Reason 'MoreSaves disposable fixed-12 lifecycle acceptance'
    if (-not $?) {
        throw 'Could not acquire the shared runtime lock.'
    }
    $lockAcquired = $true
    $receipt.Status = 'running'
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt

    $phaseResults = @()
    $postLifecycleCanonical = ''
    for ($index = 0; $index -lt $phases.Count; $index++) {
        $phase = $phases[$index]
        $phaseRoot = Join-Path $OutputRoot ([string]$phase.Id)
        New-Item -ItemType Directory -Path $phaseRoot -Force |
            Out-Null
        $arguments = @(
            '-NoProfile',
            '-ExecutionPolicy',
            'Bypass',
            '-File',
            (Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
            '-StageQaHost',
            '-DirectExe',
            '-SkipInstall',
            '-SaveSlot',
            '7',
            '-SaveTestMode',
            [string]$phase.SaveTestMode,
            '-DisposableSaveFixtureRoot',
            $fixtureRoot,
            '-OfficialModProfile',
            'CoreOnly',
            '-IsolateAllOfficialMods',
            '-MoreSavesFixed12AcceptancePhase',
            [string]$phase.Id,
            '-RetainDisposableSaveFixtureOnSuccess',
            '-TimeoutSeconds',
            [string]$TimeoutSeconds)
        if ([bool]$phase.ProductEnabled) {
            $arguments += @(
                '-OfficialModProfileExtraEnabledIds',
                'Local.DTMAPI_MoreSaves')
        }
        if ($SkipBuild -or $index -gt 0) {
            $arguments += '-SkipBuild'
        }
        $smokeOutput = @(& powershell.exe @arguments 2>&1)
        $smokeExit = $LASTEXITCODE
        $smokeOutput |
            Set-Content -LiteralPath (
                Join-Path $phaseRoot 'smoke-output.txt') -Encoding UTF8
        if ($smokeExit -ne 0) {
            throw "MoreSaves fixed-12 $($phase.Id) smoke failed with exit $smokeExit."
        }
        $smokeEvidence =
            Get-MoreSavesFixed12SmokeEvidencePath `
                -Output $smokeOutput
        $resultPath = Join-Path $smokeEvidence 'result.json'
        $result =
            Get-Content -Raw -Encoding UTF8 -LiteralPath $resultPath |
            ConvertFrom-Json
        Assert-MoreSavesFixed12SmokeResult `
            -Phase $phase `
            -Result $result `
            -ProductRoot $liveOfficialProductRoot
        Assert-MoreSavesFixed12LoadedSource `
            -Phase $phase `
            -SmokeEvidence $smokeEvidence `
            -ProductRoot $liveOfficialProductRoot

        $liveProductCurrentSnapshot = @(
            Get-MoreSavesFixed12ProductSnapshot `
                -Root $liveOfficialProductRoot)
        if ((Convert-MoreSavesFixed12SnapshotToCanonicalJson `
                -Snapshot $liveProductCurrentSnapshot) -cne
            (Convert-MoreSavesFixed12SnapshotToCanonicalJson `
                -Snapshot $liveProductInitialSnapshot)) {
            throw "MoreSaves fixed-12 $($phase.Id) changed the live official product tree."
        }
        $liveProfileCurrentSnapshot =
            Get-MoreSavesFixed12FileSnapshot `
                -Path $liveOfficialProfilePath
        if (($liveProfileCurrentSnapshot | ConvertTo-Json -Depth 4 -Compress) -cne
            ($liveProfileInitialSnapshot | ConvertTo-Json -Depth 4 -Compress)) {
            throw "MoreSaves fixed-12 $($phase.Id) did not restore the live official profile exactly."
        }

        $phaseSnapshot = @(
            Get-MoreSavesFixed12SaveSnapshot -SaveRoot $saveRoot)
        $phaseSnapshotPath = Join-Path $phaseRoot 'save-snapshot.json'
        Write-MoreSavesFixed12Json `
            -Path $phaseSnapshotPath `
            -Value ([ordered]@{
                CapturedAt = (Get-Date).ToString('o')
                Phase = [string]$phase.Id
                Entries = @($phaseSnapshot)
            })
        $phaseCanonical =
            Convert-MoreSavesFixed12SnapshotToCanonicalJson `
                -Snapshot $phaseSnapshot
        if ($index -eq 0) {
            Assert-MoreSavesFixed12PostLifecycleState `
                -SaveRoot $saveRoot `
                -Initial $initialSnapshot `
                -Current $phaseSnapshot
            $postLifecycleCanonical = $phaseCanonical
        }
        elseif ($phaseCanonical -cne $postLifecycleCanonical) {
            throw "MoreSaves fixed-12 $($phase.Id) changed the post-lifecycle SAVE snapshot during a NoNativeSave cold process."
        }

        $phaseResult = [ordered]@{
            Phase = [string]$phase.Id
            SaveTestMode = [string]$phase.SaveTestMode
            ProductEnabled = [bool]$phase.ProductEnabled
            SmokeEvidence = $smokeEvidence
            SmokeResult = [System.IO.Path]::GetFullPath($resultPath)
            SaveSnapshot = [System.IO.Path]::GetFullPath($phaseSnapshotPath)
            OfficialLocalProductSourceVerified = $true
            LiveOfficialProductTreeUnchanged = $true
            LiveOfficialProfileUnchanged = $true
            FixtureRetained = $true
            Passed = $true
        }
        $phaseResults += $phaseResult
        $receipt.Results = @($phaseResults)
        Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    }

    $officialProductBinding.LiveTreeUnchanged = $true
    $officialProductBinding.LiveProfileUnchanged = $true
    $officialProductBinding.State = 'CompletedUnchanged'
    $receipt.OfficialLocalProductBinding = $officialProductBinding

    $finalSmokeEvidence = [string]$phaseResults[-1].SmokeEvidence
    if ([string]::IsNullOrWhiteSpace($finalSmokeEvidence) -or
        -not (Test-Path -LiteralPath $finalSmokeEvidence -PathType Container)) {
        throw 'The final MoreSaves cold process did not leave one valid GAME-SMOKE evidence root.'
    }
    $durableReceiptPath =
        Join-Path $finalSmokeEvidence 'moresaves-fixed12-acceptance.json'
    $receipt['DurableReceipt'] =
        [System.IO.Path]::GetFullPath($durableReceiptPath)
    $receipt['FixtureCleanup'] = if ($RetainDisposableSaveFixtureOnSuccess) {
        'RetainedExplicitly'
    }
    else {
        'PendingExactMarkedRootDeletion'
    }
    $receipt.Status = 'completed'
    $receipt['CompletedAt'] = (Get-Date).ToString('o')
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    Write-MoreSavesFixed12Json `
        -Path $durableReceiptPath -Value $receipt

    if (-not $RetainDisposableSaveFixtureOnSuccess) {
        Remove-MoreSavesFixed12Fixture -Root $fixtureRoot
        $receipt.FixtureCleanup = 'DeletedExactMarkedRoot'
    }
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    Write-MoreSavesFixed12Json `
        -Path $durableReceiptPath -Value $receipt
    Write-Output (
        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_RECEIPT=' +
        [System.IO.Path]::GetFullPath($durableReceiptPath))
}
catch {
    $receipt.Status = 'failed'
    $receipt['Failure'] =
        $_.Exception.GetType().FullName + ': ' +
        $_.Exception.Message
    $receipt['FailedAt'] = (Get-Date).ToString('o')
    $receipt['FixtureCleanup'] = 'RetainedOnFailure'
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    throw
}
finally {
    if ($lockAcquired) {
        & "$PSScriptRoot\release-runtime-lock.ps1"
    }
}
