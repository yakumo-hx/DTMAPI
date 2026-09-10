function Write-SmokeRunnerFailure {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Phase,
        [Parameter(Mandatory = $true)] [string] $Reason,
        [Parameter(Mandatory = $true)] [string] $SaveTestMode,
        [bool] $LaunchAttempted = $false
    )
    if (-not (Test-Path -LiteralPath $EvidencePath -PathType Container)) {
        throw 'Failure evidence requires the existing runner-owned evidence directory.'
    }
    $resultPath = Join-Path $EvidencePath 'result.json'
    $result = [ordered]@{}
    if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
        $existing = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($property in $existing.PSObject.Properties) { $result[$property.Name] = $property.Value }
    }
    $exitVerified = $false
    $exitPath = Join-Path $EvidencePath 'process-exit-before-state-restore.json'
    if (Test-Path -LiteralPath $exitPath -PathType Leaf) {
        $exitReceipt = Get-Content -LiteralPath $exitPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $exitVerified = [bool]$exitReceipt.Exited -and [bool]$exitReceipt.StableAbsenceObserved -and @($exitReceipt.RemainingProcessIds).Count -eq 0
    }
    $manualRecoveryRequired = Test-Path -LiteralPath (Join-Path $EvidencePath 'manual-recovery-required.json') -PathType Leaf
    $result['SchemaVersion'] = 2
    if ([string]$result['RunStatus'] -notin @('Blocked','Aborted')) { $result['RunStatus'] = 'Failed' }
    $result['RunStatusReason'] = $Reason
    $result['FailedPhase'] = $Phase
    $result['SaveTestMode'] = $SaveTestMode
    $result['GameLaunchAttempted'] = $LaunchAttempted
    $result['ProcessExited'] = if ($exitVerified) { 'Passed' } else { 'Unverified' }
    $result['StateRestoration'] = if ($manualRecoveryRequired) { 'Blocked' } else { 'Unverified' }
    $result['ManualRecoveryRequired'] = $manualRecoveryRequired
    $result['RoutinePlayerSaveByteBackupCreated'] = $false
    $result['PlayerArchiveWritebackPerformed'] = $false
    $manualExitPath = Join-Path $EvidencePath 'manual-exit.json'
    if(Test-Path -LiteralPath $manualExitPath -PathType Leaf) {
        $manualExit = Get-Content -LiteralPath $manualExitPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $result['WaitForManualExit'] = [bool]$manualExit.Requested
        $result['ManualExit'] = if([bool]$manualExit.Passed){'Passed'}else{'Failed'}
        $result['ManualExitReason'] = [string]$manualExit.Reason
        $result['ManualExitTimeout'] = [bool]$manualExit.TimedOut
    }
    foreach ($gate in @(
        @{Field='PlayerSaveUnchangedBeforeCleanup';File='player-save-unchanged-before-cleanup.json'},
        @{Field='CommittedSidecarsUnchangedBeforeCleanup';File='committed-sidecar-unchanged-before-cleanup.json'}
    )) {
        $status = if ($SaveTestMode -ceq 'NoNativeSave') { 'Unverified' } else { 'Skipped' }
        $path = Join-Path $EvidencePath $gate.File
        if ($SaveTestMode -ceq 'NoNativeSave' -and $exitVerified -and -not $manualRecoveryRequired -and (Test-Path -LiteralPath $path -PathType Leaf)) {
            $receipt = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
            if ([string]$receipt.SaveTestMode -ceq 'NoNativeSave' -and [bool]$receipt.ComparedBeforeRunnerOrExternalRestore -and
                -not [bool]$receipt.RoutineByteBackupCreated -and -not [bool]$receipt.PlayerArchiveWritebackPerformed) {
                $status = if ([bool]$receipt.Passed) { 'Passed' } else { 'Failed' }
            }
        }
        $result[$gate.Field] = $status
    }
    $result['Completed'] = Get-Date -Format o
    Write-SmokeJsonObject -Path $resultPath -Value $result
    Write-Output ('DTMAPI_SMOKE_EVIDENCE_PATH=' + [IO.Path]::GetFullPath($EvidencePath))
}

function Write-SmokeJsonObject {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $json = $Value | ConvertTo-Json -Depth 10
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Get-SmokeQaHostVersionAuthority {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    $path = Join-Path $RepoRoot 'tools\release\dtmapi-runtime-version.props'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "QA host staging requires the tracked Runtime version authority: $path"
    }
    [xml]$document = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $schema = [string]$document.Project.PropertyGroup.DtmApiVersionAuthoritySchema
    $release = [string]$document.Project.PropertyGroup.DtmApiReleaseVersion
    $binary = [string]$document.Project.PropertyGroup.DtmApiBinaryFileVersion
    $assembly = [string]$document.Project.PropertyGroup.DtmApiAssemblyCompatibilityVersion
    if ($schema -ne '1' -or [string]::IsNullOrWhiteSpace($release) -or
        $binary -notmatch '^\d+\.\d+\.\d+\.\d+$' -or
        $assembly -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "QA host staging found an invalid Runtime version authority: $path"
    }
    return [pscustomobject]@{
        ReleaseVersion = $release
        BinaryVersion = $binary
        AssemblyVersion = $assembly
    }
}

function Get-SmokeFileMetadataSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $Kind,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $exists = Test-Path -LiteralPath $resolved -PathType Leaf
    $item = if ($exists) { Get-Item -LiteralPath $resolved } else { $null }
    return [pscustomobject]@{
        Kind = $Kind
        Path = $resolved
        Existed = $exists
        Length = if ($exists) { [int64]$item.Length } else { [int64]0 }
        Sha256 = if ($exists) { Get-SmokeFileSha256 -Path $resolved } else { '' }
        LastWriteTimeUtc = if ($exists) { $item.LastWriteTimeUtc.ToString('o') } else { '' }
        LastWriteTimeUtcTicks = if ($exists) { [int64]$item.LastWriteTimeUtc.Ticks } else { [int64]0 }
    }
}

function Compare-SmokeFileMetadataSnapshots {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [object[]] $Snapshots,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $SaveTestMode
    )

    $checks = New-Object 'System.Collections.Generic.List[object]'
    $passed = $true
    foreach ($snapshot in @($Snapshots)) {
        $actual = Get-SmokeFileMetadataSnapshot -Kind ([string]$snapshot.Kind) -Path ([string]$snapshot.Path)
        $unchanged =
            [bool]$actual.Existed -eq [bool]$snapshot.Existed -and
            [int64]$actual.Length -eq [int64]$snapshot.Length -and
            [string]::Equals([string]$actual.Sha256, [string]$snapshot.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -and
            [int64]$actual.LastWriteTimeUtcTicks -eq [int64]$snapshot.LastWriteTimeUtcTicks
        $passed = $passed -and $unchanged
        $checks.Add([ordered]@{
            Kind = [string]$snapshot.Kind
            Path = [string]$snapshot.Path
            ExistedBefore = [bool]$snapshot.Existed
            ExistsBeforeCleanup = [bool]$actual.Existed
            LengthBefore = [int64]$snapshot.Length
            LengthBeforeCleanup = [int64]$actual.Length
            Sha256Before = [string]$snapshot.Sha256
            Sha256BeforeCleanup = [string]$actual.Sha256
            LastWriteTimeUtcBefore = [string]$snapshot.LastWriteTimeUtc
            LastWriteTimeUtcBeforeCleanup = [string]$actual.LastWriteTimeUtc
            LastWriteTimeUtcTicksBefore = [int64]$snapshot.LastWriteTimeUtcTicks
            LastWriteTimeUtcTicksBeforeCleanup = [int64]$actual.LastWriteTimeUtcTicks
            UnchangedBeforeCleanup = $unchanged
        }) | Out-Null
    }
    Write-SmokeJsonObject -Path $EvidencePath -Value ([ordered]@{
        SaveTestMode = $SaveTestMode
        ComparedBeforeRunnerOrExternalRestore = $true
        RoutineByteBackupCreated = $false
        PlayerArchiveWritebackPerformed = $false
        Files = @($checks.ToArray())
        Passed = $passed
    })
    return $passed
}

function Get-SmokeCommittedSidecarPaths {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $DtmApiStateDir,
        [Parameter(Mandatory = $true)] [int] $ArchiveIndex
    )

    $catalogPath = Join-Path $RepoRoot 'tools\release\dtmapi-product-catalog.json'
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $paths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($product in @($catalog.products)) {
        foreach ($templateValue in @($product.saveSidecars)) {
            $template = [string]$templateValue
            if ([string]::IsNullOrWhiteSpace($template)) {
                continue
            }
            $relative = $template.Replace('<archiveIndex>', $ArchiveIndex.ToString([System.Globalization.CultureInfo]::InvariantCulture)).Replace('/', '\')
            if ($relative -match '<[^>]+>') {
                throw "Committed sidecar template retained an unsupported placeholder: $template"
            }
            if (-not $relative.StartsWith('DTMAPI\', [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Committed sidecar template escaped the DTMAPI state root: $template"
            }
            $livePath =
                [System.IO.Path]::GetFullPath(
                    (Join-Path $DtmApiStateDir $relative.Substring('DTMAPI\'.Length)))
            $paths.Add($livePath) | Out-Null
            $paths.Add($livePath + '.previous') | Out-Null
        }
    }
    return @($paths.ToArray() | Sort-Object -Unique)
}

function Get-SmokeQaHostFileReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Kind,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $item = Get-Item -LiteralPath $resolved -ErrorAction Stop
    if ($item.PSIsContainer -or $item.Length -le 0) {
        throw "QA host staged artifact is missing or empty: $resolved"
    }
    return [pscustomobject]@{
        Kind = $Kind
        Path = $resolved
        Length = [int64]$item.Length
        Sha256 = Get-SmokeFileSha256 -Path $resolved
    }
}

function Get-SmokeDirectoryReceipt {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $exists = Test-Path -LiteralPath $resolved -PathType Container
    $directories = @(if ($exists) {
        Get-ChildItem -LiteralPath $resolved -Recurse -Directory -Force | ForEach-Object {
            $_.FullName.Substring($resolved.Length).TrimStart('\','/').Replace('\','/')
        } | Sort-Object
    }
    else { @() })
    $files = @(if ($exists) {
        Get-ChildItem -LiteralPath $resolved -Recurse -File -Force | ForEach-Object {
            [ordered]@{
                RelativePath = $_.FullName.Substring($resolved.Length).TrimStart('\','/').Replace('\','/')
                Length = [int64]$_.Length
                Sha256 = Get-SmokeFileSha256 -Path $_.FullName
            }
        } | Sort-Object RelativePath
    }
    else { @() })
    return [pscustomobject]@{
        Path = $resolved
        Existed = $exists
        DirectoryCount = $directories.Count
        Directories = @($directories)
        FileCount = $files.Count
        Files = @($files)
    }
}

function Compare-SmokeDirectoryReceipt {
    param(
        [Parameter(Mandatory = $true)] $Before,
        [Parameter(Mandatory = $true)] $After
    )

    $beforeDirectoriesJson = @($Before.Directories) | ConvertTo-Json -Depth 5 -Compress
    $afterDirectoriesJson = @($After.Directories) | ConvertTo-Json -Depth 5 -Compress
    $beforeFilesJson = @($Before.Files) | ConvertTo-Json -Depth 5 -Compress
    $afterFilesJson = @($After.Files) | ConvertTo-Json -Depth 5 -Compress
    $passed = [bool]$Before.Existed -eq [bool]$After.Existed -and
        [int]$Before.DirectoryCount -eq [int]$After.DirectoryCount -and
        [string]::Equals($beforeDirectoriesJson, $afterDirectoriesJson, [System.StringComparison]::Ordinal) -and
        [int]$Before.FileCount -eq [int]$After.FileCount -and
        [string]::Equals($beforeFilesJson, $afterFilesJson, [System.StringComparison]::Ordinal)
    return [pscustomobject]@{
        Path = [string]$Before.Path
        ExistedBefore = [bool]$Before.Existed
        ExistsAfter = [bool]$After.Existed
        DirectoryCountBefore = [int]$Before.DirectoryCount
        DirectoryCountAfter = [int]$After.DirectoryCount
        DirectoriesBefore = @($Before.Directories)
        DirectoriesAfter = @($After.Directories)
        FileCountBefore = [int]$Before.FileCount
        FileCountAfter = [int]$After.FileCount
        FilesBefore = @($Before.Files)
        FilesAfter = @($After.Files)
        Passed = $passed
    }
}

function Get-SmokeNoQaRuntimeReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir
    )

    $activationPath = [System.IO.Path]::GetFullPath((Join-Path $StateDir 'qa-host-activation.json'))
    $qaRoot = [System.IO.Path]::GetFullPath((Join-Path $StateDir 'qa-host'))
    $runtimeRoot = [System.IO.Path]::GetFullPath((Join-Path $GameDir 'BepInEx\plugins\DTMAPI'))
    $expectedRuntimeDlls = @(
        'DTMAPI.Abstractions.dll',
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    ) | Sort-Object
    $actualRuntimeDlls = if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $runtimeRoot -Recurse -File -Filter '*.dll' | ForEach-Object { $_.Name } | Sort-Object)
    }
    else {
        @()
    }
    $qaDlls = New-Object 'System.Collections.Generic.List[string]'
    foreach ($searchRoot in @((Join-Path $GameDir 'BepInEx\plugins'), $StateDir)) {
        if (Test-Path -LiteralPath $searchRoot -PathType Container) {
            foreach ($file in @(Get-ChildItem -LiteralPath $searchRoot -Recurse -File -Filter 'DTMAPI.GameBridge.DolocTown.QA.dll' -ErrorAction SilentlyContinue)) {
                $qaDlls.Add([System.IO.Path]::GetFullPath($file.FullName)) | Out-Null
            }
        }
    }
    $qaDllPaths = @($qaDlls.ToArray() | Sort-Object -Unique)
    $runtimeShapePassed = [string]::Equals(
        (@($expectedRuntimeDlls) -join "`n"),
        (@($actualRuntimeDlls) -join "`n"),
        [System.StringComparison]::Ordinal)
    $activationAbsent = -not (Test-Path -LiteralPath $activationPath)
    $qaRootAbsent = -not (Test-Path -LiteralPath $qaRoot)
    return [pscustomobject]@{
        ActivationPath = $activationPath
        ActivationAbsent = $activationAbsent
        QaRootPath = $qaRoot
        QaRootAbsent = $qaRootAbsent
        RuntimeRoot = $runtimeRoot
        ExpectedRuntimeDlls = @($expectedRuntimeDlls)
        ActualRuntimeDlls = @($actualRuntimeDlls)
        ExactFiveRuntimeDlls = $runtimeShapePassed
        QaDllPaths = @($qaDllPaths)
        QaDllAbsent = $qaDllPaths.Count -eq 0
        Passed = $activationAbsent -and $qaRootAbsent -and $qaDllPaths.Count -eq 0 -and $runtimeShapePassed
    }
}

function Test-SmokeNoQaReceiptComparison {
    $absentPath = Join-Path $repo ('tmp\test-runs\noqa-directory-receipt-absent-' + [Guid]::NewGuid().ToString('N'))
    if (Test-Path -LiteralPath $absentPath) {
        throw "No-QA directory-receipt absent fixture unexpectedly exists: $absentPath"
    }
    $absentReceipt = Get-SmokeDirectoryReceipt -Path $absentPath
    $absentReceiptAccepted = -not [bool]$absentReceipt.Existed -and
        [int]$absentReceipt.DirectoryCount -eq 0 -and @($absentReceipt.Directories).Count -eq 0 -and
        [int]$absentReceipt.FileCount -eq 0 -and @($absentReceipt.Files).Count -eq 0
    $unchangedBefore = [pscustomobject]@{
        Path = 'X:\evidence\DEBUG-CONSOLE-UI'
        Existed = $true
        DirectoryCount = 1
        Directories = @('old')
        FileCount = 1
        Files = @([ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) })
    }
    $unchangedAfter = [pscustomobject]@{
        Path = $unchangedBefore.Path
        Existed = $true
        DirectoryCount = 1
        Directories = @('old')
        FileCount = 1
        Files = @([ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) })
    }
    $changedAfter = [pscustomobject]@{
        Path = $unchangedBefore.Path
        Existed = $true
        DirectoryCount = 2
        Directories = @('new', 'old')
        FileCount = 2
        Files = @(
            [ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) },
            [ordered]@{ RelativePath = 'new\summary.txt'; Length = 3; Sha256 = ('B' * 64) }
        )
    }
    $emptyDirectoryChangedAfter = [pscustomobject]@{
        Path = $unchangedBefore.Path
        Existed = $true
        DirectoryCount = 2
        Directories = @('new-empty', 'old')
        FileCount = 1
        Files = @([ordered]@{ RelativePath = 'old\summary.txt'; Length = 3; Sha256 = ('A' * 64) })
    }
    $unchanged = Compare-SmokeDirectoryReceipt -Before $unchangedBefore -After $unchangedAfter
    $changed = Compare-SmokeDirectoryReceipt -Before $unchangedBefore -After $changedAfter
    $emptyDirectoryChanged = Compare-SmokeDirectoryReceipt -Before $unchangedBefore -After $emptyDirectoryChangedAfter
    return [pscustomobject]@{
        AbsentDirectoryReceiptAccepted = $absentReceiptAccepted
        AbsentDirectoryCount = [int]$absentReceipt.DirectoryCount
        AbsentFileCount = [int]$absentReceipt.FileCount
        UnchangedAccepted = [bool]$unchanged.Passed
        GrowthRejected = -not [bool]$changed.Passed
        EmptyDirectoryGrowthRejected = -not [bool]$emptyDirectoryChanged.Passed
        Passed = $absentReceiptAccepted -and [bool]$unchanged.Passed -and
            -not [bool]$changed.Passed -and -not [bool]$emptyDirectoryChanged.Passed
    }
}

function Get-SmokeQaG4ExpectedEvidencePaths {
    param(
        [Parameter(Mandatory = $true)] [bool] $TitleSettingsUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerStatusUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerMvpUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $OfficialModUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $PauseMenuLayoutEnabled,
        [Parameter(Mandatory = $true)] [bool] $DebugConsoleEnabled,
        [Parameter(Mandatory = $true)] [bool] $SaveSlotsPagingEnabled,
        [Parameter(Mandatory = $true)] [bool] $MoreSavesPostTitlePanelEnabled,
        [Parameter(Mandatory = $true)] [bool] $AnimalObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $EquipmentSlotsObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $MoreEquipmentSlots100UiAcceptanceEnabled,
        [Parameter(Mandatory = $true)] [bool] $CameraPlayableEnabled
    )

    $paths = New-Object 'System.Collections.Generic.List[string]'
    if ($TitleSettingsUiEnabled) { $paths.Add('ui\title-settings.png') }
    if ($ManagerStatusUiEnabled -or $ManagerMvpUiEnabled) { $paths.Add('ui\manager-status-page.png') }
    if ($ManagerMvpUiEnabled) {
        $paths.Add('ui\manager-mods-page.png')
        $paths.Add('ui\manager-advanced-page.png')
        $paths.Add('ui\manager-logs-page.png')
    }
    if ($OfficialModUiEnabled) { $paths.Add('ui\official-mod-ui.png') }
    if ($PauseMenuLayoutEnabled) { $paths.Add('ui\pause-menu-layout.png') }
    if ($DebugConsoleEnabled) { $paths.Add('ui\debug-console.png') }
    if ($SaveSlotsPagingEnabled) { $paths.Add('ui\official-save-ui.png') }
    if ($MoreSavesPostTitlePanelEnabled) {
        $paths.Add('ui\official-save-ui-post-title.png')
    }
    if ($AnimalObservationEnabled) { $paths.Add('ui\animal-panel.png') }
    if ($EquipmentSlotsObservationEnabled) {
        $paths.Add('ui\equipment-slots.png')
        $paths.Add('ui\equipment-slots-summary.txt')
    }
    if ($MoreEquipmentSlots100UiAcceptanceEnabled) {
        foreach ($name in @(
            'equipment-slots-1920x1080-dynamic-row.png',
            'equipment-slots-1024x768-dynamic-row.png',
            'equipment-slots-1024x768-reopen.png')) {
            $paths.Add('ui\' + $name)
        }
    }
    if ($CameraPlayableEnabled) {
        foreach ($name in @('before.png','scale-4x.png','movement-start.png','movement-mid.png','movement-end.png','fallback-2x.png','reset.png','telemetry.csv')) {
            $paths.Add('camera-playable\' + $name)
        }
    }
    return @($paths.ToArray() | Sort-Object -Unique)
}

function Get-SmokeFileSha256 {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::Open(
            [System.IO.Path]::GetFullPath($Path),
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
        try {
            return ([BitConverter]::ToString($sha256.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $sha256.Dispose()
    }
}

function Assert-SmokeExactJsonPropertySet {
    param(
        [Parameter(Mandatory = $true)] [object] $Value,
        [Parameter(Mandatory = $true)] [string[]] $Expected,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    if ($null -eq $Value) {
        throw "$Context must contain one JSON object."
    }
    [string[]]$actualNames = @($Value.PSObject.Properties | ForEach-Object { [string]$_.Name })
    [string[]]$expectedNames = @($Expected | ForEach-Object { [string]$_ })
    [Array]::Sort($actualNames, [System.StringComparer]::Ordinal)
    [Array]::Sort($expectedNames, [System.StringComparer]::Ordinal)
    if ($actualNames.Count -ne $expectedNames.Count -or
        [string]::Join("`n", $actualNames) -cne [string]::Join("`n", $expectedNames)) {
        throw "$Context has an unknown, missing, duplicate, or wrong-version field."
    }
}

function Test-SmokeSha256Equal {
    param([string] $Actual, [string] $Expected)

    return $Actual -match '^[0-9a-fA-F]{64}$' -and
        $Expected -match '^[0-9a-fA-F]{64}$' -and
        [string]::Equals($Actual, $Expected, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-SmokeWorkshopArtifactSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Product,
        [ValidateSet('Auto', 'Retained', 'CurrentPublished')]
        [string] $ArtifactBoundary = 'Auto'
    )

    $currentPublishedArtifactProperty = $Product.PSObject.Properties['currentPublishedArtifact']
    $currentPublishedArtifact = if ($null -ne $currentPublishedArtifactProperty) {
        $currentPublishedArtifactProperty.Value
    }
    else {
        $null
    }
    $effectiveArtifactBoundary = if ($ArtifactBoundary -eq 'Auto') {
        if ($null -ne $currentPublishedArtifact) { 'CurrentPublished' } else { 'Retained' }
    }
    else {
        $ArtifactBoundary
    }
    if ($effectiveArtifactBoundary -eq 'CurrentPublished' -and $null -eq $currentPublishedArtifact) {
        throw "Published-product artifact snapshot requires currentPublishedArtifact for $($Product.catalogId)."
    }

    $expectedArtifact = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        $currentPublishedArtifact
    }
    else {
        $Product.retainedArtifact
    }
    if ($null -eq $expectedArtifact) {
        throw "Published-product artifact snapshot requires $effectiveArtifactBoundary metadata for $($Product.catalogId)."
    }
    $treeDigestAlgorithm = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [string]$expectedArtifact.treeDigestAlgorithm
    }
    else {
        'DTMAPI-Retained-SHA256SUMS-v1'
    }
    $expectedFileCount = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [int]$expectedArtifact.steamDeliveredFileCount
    }
    else {
        [int]$expectedArtifact.fileCount
    }
    $expectedBytes = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [int64]$expectedArtifact.steamDeliveredBytes
    }
    else {
        [int64]$expectedArtifact.bytes
    }
    $expectedTreeSha256 = if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        [string]$expectedArtifact.steamDeliveredTreeSha256
    }
    else {
        [string]$expectedArtifact.treeSha256
    }
    $snapshot = [ordered]@{
        CatalogId = [string]$Product.catalogId
        UniqueId = [string]$Product.uniqueId
        WorkshopId = [string]$Product.workshopId
        ArtifactBoundary = $effectiveArtifactBoundary
        TreeDigestAlgorithm = $treeDigestAlgorithm
        Path = [System.IO.Path]::GetFullPath($Path)
        Exists = Test-Path -LiteralPath $Path -PathType Container
        ExpectedFileCount = $expectedFileCount
        ExpectedBytes = $expectedBytes
        ExpectedTreeSha256 = $expectedTreeSha256
        ActualFileCount = 0
        ActualBytes = [int64]0
        ActualTreeSha256 = ''
        ExcludedRelativePaths = @()
        ReparsePointPaths = @()
        ReparsePointFree = $false
        Passed = $false
    }
    if (-not $snapshot.Exists) {
        return $snapshot
    }

    $rootItem = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    $entries = @(Get-ChildItem -LiteralPath $Path -Recurse -Force -ErrorAction Stop)
    $reparsePointPaths = @(
        @($rootItem) + $entries |
            Where-Object {
                ($_.Attributes -band
                    [System.IO.FileAttributes]::ReparsePoint) -ne 0
            } |
            ForEach-Object {
                $_.FullName
            } |
            Sort-Object -Unique)
    $snapshot.ReparsePointPaths = @($reparsePointPaths)
    $snapshot.ReparsePointFree = $reparsePointPaths.Count -eq 0

    $fileRows = @($entries | Where-Object {
        -not $_.PSIsContainer
    } | ForEach-Object {
        [pscustomobject]@{
            File = $_
            RelativePath = $_.FullName.Substring($Path.Length).TrimStart([char]92, [char]47).Replace([char]92, [char]47)
        }
    })
    if ($effectiveArtifactBoundary -eq 'CurrentPublished') {
        $excludedRows = @($fileRows | Where-Object {
            $_.RelativePath.StartsWith(
                'Content/.tools/bepinex/extract/',
                [System.StringComparison]::OrdinalIgnoreCase)
        })
        $snapshot.ExcludedRelativePaths = @($excludedRows | ForEach-Object { [string]$_.RelativePath })
        $includedRowsByPath = New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::Ordinal)
        foreach ($row in @($fileRows | Where-Object {
            -not $_.RelativePath.StartsWith(
                'Content/.tools/bepinex/extract/',
                [System.StringComparison]::OrdinalIgnoreCase)
        })) {
            $includedRowsByPath.Add([string]$row.RelativePath, $row)
        }
        $files = @($includedRowsByPath.Values)
    }
    else {
        $retainedRowsByPath = New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($row in $fileRows) {
            $retainedRowsByPath.Add([string]$row.RelativePath, $row)
        }
        $files = @($retainedRowsByPath.Values)
    }
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($row in $files) {
        $fileHash = (Get-SmokeFileSha256 -Path $row.File.FullName).ToLowerInvariant()
        $lines.Add($fileHash + '  ' + [string]$row.RelativePath) | Out-Null
        $snapshot.ActualBytes += [int64]$row.File.Length
    }
    $snapshot.ActualFileCount = $files.Count
    $normalizedTree = $lines.ToArray() -join "`n"
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $snapshot.ActualTreeSha256 = [System.BitConverter]::ToString($hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($normalizedTree))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
    $snapshot.Passed = $snapshot.ActualFileCount -eq $snapshot.ExpectedFileCount -and
        $snapshot.ActualBytes -eq $snapshot.ExpectedBytes -and
        $snapshot.ReparsePointFree -and
        [string]::Equals($snapshot.ActualTreeSha256, $snapshot.ExpectedTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)
    return $snapshot
}

function Get-SmokeStatus {
    param(
        [bool] $Requested,
        [bool] $Passed,
        [bool] $Blocked = $false
    )

    if ($Blocked) {
        return 'Blocked'
    }
    if (-not $Requested) {
        return 'Skipped'
    }
    if ($Passed) {
        return 'Passed'
    }
    return 'Failed'
}

function Get-SmokeAlwaysStatus {
    param(
        [bool] $Passed,
        [bool] $Blocked = $false
    )

    return Get-SmokeStatus -Requested $true -Passed $Passed -Blocked $Blocked
}

function Write-SmokeBlockedResult {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Reason,
        [string] $RunStatus = 'Blocked'
    )

    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'result.json') -Value @{
        SchemaVersion = 2
        RunStatus = $RunStatus
        RunStatusReason = $Reason
        StartupLog = 'Blocked'
        GameLaunched = 'Blocked'
        HookProbe = 'Skipped'
        SaveLoaded = 'Skipped'
        NoFatalInstanceWindow = if ($Reason -match 'Fatal') { 'Failed' } else { 'Passed' }
        ProcessExited = if ($Reason -match 'DolocTown\.exe') { 'Failed' } else { 'Skipped' }
        ForcedClose = 'Skipped'
        LifecycleObservation = 'Skipped'
        LifecycleBoundaryContract = 'Skipped'
        LifecycleBoundaryContractSummary = ''
        ShadowContentRegistry = 'Skipped'
        ContentRegistry = 'Skipped'
        ManifestRegistry = 'Skipped'
        DependencyCompatibility = 'Skipped'
        ContentPackOwnership = 'Skipped'
        RegistryDiffs = 'Skipped'
        RefactorScaffoldFlags = 'Skipped'
        RefactorScaffoldFlagsSummary = ''
        ResourceLifecycleLedger = 'Skipped'
        ResourceLifecycleCleanup = 'Skipped'
        ResourceLifecycleSummary = ''
        TitleIdleResourceGrowth = 'Skipped'
        SaveLoadRequestCoordinator = 'Skipped'
        SaveLoadRequestSummary = ''
        DuplicateLoadRequests = 'Skipped'
        SaveLoadBoundary = 'Skipped'
        TitleReturnBoundaryLedger = 'Skipped'
        TitleReturnBoundarySummary = ''
        HookScheduler = 'Skipped'
        CoreHookReadiness = 'Skipped'
        FeatureHookReadiness = 'Skipped'
        HookStatusQueue = 'Skipped'
        OffThreadHookRequests = 'Skipped'
        AssemblyLoadSubscription = 'Skipped'
        RetryTimerAlive = 'Skipped'
        LongTitleIdleBeforeSave = 'Skipped'
        TitleIdleBeforeSaveSeconds = $TitleIdleBeforeSaveSeconds
        SaveLoadCycle = 'Skipped'
        SaveLoadCycleSummary = ''
        SaveLoadObjectSnapshotMode = $SaveLoadObjectSnapshotMode
        SmokeOwnerRootIsolationProfile = $SmokeOwnerRootIsolationProfile
        SmokeOwnerRootIsolationSummary = ''
        SmokeNativeLoadContinuationProbe = $SmokeNativeLoadContinuationProbe
        SmokeNativeLoadContinuationSummary = ''
        LastNativeContinuationLine = ''
        SaveLoadCyclePendingPressure = 'Skipped'
        SaveLoadCyclePendingPressureSummary = ''
        AutoFishingLifecycle = 'Skipped'
        AutoFishingLifecycleSummary = ''
        AutoFishingSoak = 'Skipped'
        ModOwnerLifecycle = 'Skipped'
        ModOwnerLifecycleSummary = ''
        ModLoadTransaction = 'Skipped'
        OwnerBoundInput = 'Skipped'
        EventHandlerCleanup = 'Skipped'
        ConfigPreviewAudit = 'Skipped'
        FailedModRollback = 'Skipped'
        GameBridgeFeatureContracts = 'Skipped'
        GameBridgeFinalHealthSnapshot = 'Skipped'
        GameBridgeFinalHealthSummary = ''
        Completed = Get-Date -Format o
    }
}
