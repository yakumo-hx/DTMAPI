if ($moreEquipmentSlotsColdRecoveryTransitionRequested) {
    $coldOfficialModsRoot = [System.IO.Path]::GetFullPath(
        (Join-Path (Get-DolocTownLivePersistentRootForSmoke) 'MODS'))
    $coldProductRoot = [System.IO.Path]::GetFullPath(
        (Join-Path $coldOfficialModsRoot 'DTMAPI_MoreEquipmentSlots'))
    $coldPackageBinding = Test-SmokeMoreEquipmentSlotsOfficialPackage `
        -ProductRoot $coldProductRoot -RepoRoot $repo
    $moreEquipmentSlotsColdOfficialPackagePreflightOk =
        [bool]$coldPackageBinding.Passed
    Write-SmokeJsonObject -Path (
        Join-Path $evidence `
            'more-equipment-slots-cold-official-package.json') `
        -Value ([ordered]@{
            ProductRoot = $coldProductRoot
            SourceId = 'Local.DTMAPI_MoreEquipmentSlots'
            OfficialRelativePath = 'MODS/DTMAPI_MoreEquipmentSlots'
            FileCount = @($coldPackageBinding.Files).Count
            Files = @($coldPackageBinding.Files)
            UniqueId = [string]$coldPackageBinding.UniqueId
            Version = [string]$coldPackageBinding.Version
            MinimumDtmApiVersion =
                [string]$coldPackageBinding.MinimumDtmApiVersion
            CodeModKind = [string]$coldPackageBinding.CodeModKind
            ReferencePolicyId = [string]$coldPackageBinding.ReferencePolicyId
            ReferencePolicySha256 = [string]$coldPackageBinding.ReferencePolicySha256
            GameBuildId = [string]$coldPackageBinding.GameBuildId
            HarmonyOwner = [string]$coldPackageBinding.HarmonyOwner
            ManifestSha256 = [string]$coldPackageBinding.ManifestSha256
            EntryDllLength = [int64]$coldPackageBinding.EntryDllLength
            EntryDllSha256 = [string]$coldPackageBinding.EntryDllSha256
            AdvancedReferenceReceiptSha256 = [string]$coldPackageBinding.AdvancedReferenceReceiptSha256
            ReferenceCount = [int]$coldPackageBinding.ReferenceCount
            Passed = $moreEquipmentSlotsColdOfficialPackagePreflightOk
        })
    if (-not $moreEquipmentSlotsColdOfficialPackagePreflightOk) {
        throw 'MoreEquipmentSlots cold recovery requires an exact current-worktree official Local package before any game launch.'
    }
}
if ($AutoExerciseStrongPlantingGun) {
    $strongPlantingGunConfigPath = Join-Path $dtmapiDir 'config\DTMAPI.StrongPlantingGunMod.json'
    $strongPlantingGunConfigBackup = Join-Path $evidence 'DTMAPI.StrongPlantingGunMod.before.json'
    $strongPlantingGunConfigHadOriginal = Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf
    if ($strongPlantingGunConfigHadOriginal) {
        Copy-Item -LiteralPath $strongPlantingGunConfigPath -Destination $strongPlantingGunConfigBackup
    }
    $strongPlantingGunConfig = if (Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf) {
        Get-Content -Raw -Encoding UTF8 -LiteralPath $strongPlantingGunConfigPath | ConvertFrom-Json
    }
    else {
        [pscustomobject]@{}
    }
    foreach ($setting in @(
        [ordered]@{ Name = 'Enabled'; Value = $true },
        [ordered]@{ Name = 'IncludeSeeds'; Value = $true },
        [ordered]@{ Name = 'IncludeFilms'; Value = $true },
        [ordered]@{ Name = 'IncludeFertilizers'; Value = $true },
        [ordered]@{ Name = 'SlotCount'; Value = 3 }
    )) {
        if ($strongPlantingGunConfig.PSObject.Properties.Name -contains [string]$setting.Name) {
            $strongPlantingGunConfig.PSObject.Properties[[string]$setting.Name].Value = $setting.Value
        }
        else {
            $strongPlantingGunConfig | Add-Member -NotePropertyName ([string]$setting.Name) -NotePropertyValue $setting.Value
        }
    }
    Write-SmokeJsonObject -Path $strongPlantingGunConfigPath -Value $strongPlantingGunConfig
    Write-SmokeJsonObject -Path (Join-Path $evidence 'strong-planting-gun-config-stage.json') -Value ([ordered]@{
        ConfigPath = [System.IO.Path]::GetFullPath($strongPlantingGunConfigPath)
        Enabled = $true
        IncludeSeeds = $true
        IncludeFilms = $true
        IncludeFertilizers = $true
        SlotCount = 3
        ExactFileBaselineCapturedBeforeStage = $true
        RestoreOwner = 'strong-planting-gun-exact-config-file'
    })
}
$qaHostStage = if ($StageQaHost) {
    New-SmokeQaHostStage -RepoRoot $repo -GameDir $gameDir -StateDir $dtmapiDir -EvidencePath $evidence -SkipQaBuild ([bool]$SkipBuild) `
        -AutoFishingPerformanceEnabled ([bool]$AutoFishingPerformance) `
        -AutoFishingPerformanceProfile $AutoFishingPerformanceProfile `
        -AutoFishingPerformanceTargetFish $AutoFishingPerformanceTargetFish `
        -AutoFishingPerformanceWarmupFish $AutoFishingPerformanceWarmupFish `
        -AutoFishingPerformanceZeroWarmupSeconds $AutoFishingPerformanceZeroWarmupSeconds `
        -AutoFishingPerformanceZeroMeasureSeconds $AutoFishingPerformanceZeroMeasureSeconds `
        -AutoFishingPerformanceWarmupFrames $AutoFishingPerformanceWarmupFrames `
        -AutoFishingPerformanceTargetFrames $AutoFishingPerformanceTargetFrames `
        -Batch5GcLadderEnabled ([bool]$batch5GcLadderEnabled) `
        -Batch5GcLadderDomain $Batch5GcLadderDomain `
        -Batch5GcLadderLevel $Batch5GcLadderLevel `
        -Batch5GcLadderWorkload $Batch5GcLadderWorkload `
        -Batch5GcLadderMultiplier $Batch5GcLadderMultiplier `
        -Batch5GcLadderMeasureSeconds $Batch5GcLadderMeasureSeconds `
        -Batch5GcLadderSampleSeconds $Batch5GcLadderSampleSeconds `
        -Batch5GcLadderTargetUnits $Batch5GcLadderTargetUnits `
        -Batch6AutoFishingEnabled ([bool]$Batch6AutoFishingPilot) `
        -Batch6AutoFishingLevel $Batch6AutoFishingLevel `
        -Batch6AutoFishingScenario $Batch6AutoFishingScenario `
        -Batch6AutoFishingMeasureSeconds $Batch6AutoFishingMeasureSeconds `
        -Batch6AutoFishingSampleSeconds $Batch6AutoFishingSampleSeconds `
        -Batch6AutoFishingWarmupFish $Batch6AutoFishingWarmupFish `
        -Batch6AutoFishingTargetFish $Batch6AutoFishingTargetFish `
        -Batch6AutoFishingFormal ([bool]$Batch6AutoFishingFormal) `
        -Batch6AutoFishingFormalContract $Batch6AutoFishingFormalContract `
        -Batch6AutoFishingMultiplier $Batch6AutoFishingMultiplier `
        -Batch6AutoFishingCastChargeRatio $Batch6AutoFishingCastChargeRatio `
        -Batch6AutoFishingManualMovementCancel ([bool]$Batch6AutoFishingManualMovementCancel) `
        -Batch6AutoFishingExpectedPackageSha256 $Batch6AutoFishingExpectedPackageSha256 `
        -Batch6AutoFishingExpectedEntrySha256 $Batch6AutoFishingExpectedEntrySha256 `
        -Batch6AutoFishingExpectedManifestSha256 $Batch6AutoFishingExpectedManifestSha256 `
        -Batch6AutoFishingExpectedPolicySha256 $Batch6AutoFishingExpectedPolicySha256 `
        -Batch6AutoFishingManagerEnabled ([bool]$Batch6AutoFishingManagerLifecycle) `
        -Batch6AutoFishingManagerMode $Batch6AutoFishingManagerMode `
        -Batch6AutoFishingManagerProductRoot $Batch6AutoFishingManagerProductRoot `
        -Batch6AutoFishingManagerMarkerSha256 $Batch6AutoFishingManagerMarkerSha256 `
        -Batch6AutoFishingManagerExpectedPackageSha256 $Batch6AutoFishingManagerExpectedPackageSha256 `
        -Batch6AutoFishingManagerExpectedEntrySha256 $Batch6AutoFishingManagerExpectedEntrySha256 `
        -Batch6AutoFishingManagerExpectedManifestSha256 $Batch6AutoFishingManagerExpectedManifestSha256 `
        -Batch6AutoFishingManagerExpectedPolicySha256 $Batch6AutoFishingManagerExpectedPolicySha256 `
        -CustomEntityContractEnabled $qaCustomEntityContractEnabled `
        -FishRoeTooltipObservationEnabled $qaFishRoeTooltipObservationEnabled `
        -DiagnosticsSnapshotEnabled $qaDiagnosticsSnapshotEnabled `
        -DiagnosticsScenario 'G3' `
        -DiagnosticsExpectedFeatureIds $qaDiagnosticsExpectedFeatureIds `
        -ObserveSaveLoaded ([bool]$QaObserveSaveLoaded) `
        -WaitForManualExit ([bool]$WaitForManualExit) `
        -ObserveSaveSaved ([bool]$QaObserveSaveSaved) `
        -ObserveWorkshopReloadCompleted ([bool]$QaObserveWorkshopReloadCompleted) `
        -SaveTestMode $SaveTestMode `
        -DisposableSaveFixtureSaveRoot $(if ($disposableSaveFixtureRequested) { [System.IO.Path]::GetFullPath((Join-Path $disposableSaveFixtureRootResolved 'SAVE')) } else { '' }) `
        -RequireDisposableSaveRedirect $disposableSaveFixtureRequested `
        -TitleSettingsUiEnabled $qaG4TitleSettingsUiEnabled `
        -ManagerStatusUiEnabled $qaG4ManagerStatusUiEnabled `
        -ManagerMvpUiEnabled $qaG4ManagerMvpUiEnabled `
        -OfficialModUiEnabled $qaG4OfficialModUiEnabled `
        -PauseMenuLayoutEnabled $qaG4PauseMenuLayoutEnabled `
        -DebugConsoleEnabled $qaG4DebugConsoleEnabled `
        -SaveSlotsPagingEnabled $qaG4SaveSlotsPagingEnabled `
        -SaveSlot $SaveSlot `
        -AnimalObservationEnabled $qaG4AnimalObservationEnabled `
        -EquipmentSlotsObservationEnabled $qaG4EquipmentSlotsObservationEnabled `
        -MoreEquipmentSlots100UiAcceptanceEnabled $moreEquipmentSlots100UiAcceptanceRequested `
        -AudioObservationEnabled $qaG4AudioObservationEnabled `
        -HatchVoiceEnabled $qaG4HatchVoiceEnabled `
        -CameraPlayableEnabled $qaG4CameraPlayableEnabled `
        -ContentMetadataObservationEnabled $qaG4ContentMetadataObservationEnabled `
        -ContentMetadataExpectOilAbsent ([bool]$ExpectOilAbsent) `
        -ContinuousHomePageTerminalEnabled $qaG4ContinuousHomePageTerminalEnabled `
        -ContinuousHomePageRequiresSaveLoaded $qaG4ContinuousHomePageRequiresSaveLoaded `
        -ContinuousHomePageSeconds $qaG4ContinuousHomePageSeconds `
        -TitleLifecycleEnabled $qaG4TitleLifecycleEnabled `
        -ProductOwnerRefreshEnabled $qaG4ProductOwnerRefreshEnabled `
        -AdvancedProductOwnerDeactivationEnabled $qaG4AdvancedProductOwnerDeactivationEnabled `
        -AdvancedProductOwnerDeactivationOwnerIds @($AdvancedProductOwnerDeactivationOwnerIds) `
        -ExternalPlayerInputObservationEnabled $qaG4ExternalPlayerInputObservationEnabled `
        -ExternalPlayerInputToken $externalPlayerInputHandshakeToken `
        -ExternalPlayerInputMarkerPath $externalPlayerInputCompletionMarkerPath `
        -ExternalPlayerInputEvidenceRoot $(if ($RequireExternalPlayerInputGate) { [System.IO.Path]::GetFullPath($evidence) } else { '' }) `
        -G5WorldMutationCases @($qaG5WorldMutationCases.ToArray()) `
        -DebugConsoleSaveAcceptancePhase $DebugConsoleSaveAcceptancePhase `
        -ExpectedDebugConsoleMoney $ExpectedDebugConsoleMoney `
        -ExpectedMoreEquipmentSlotsBackpackBaseline $ExpectedMoreEquipmentSlotsBackpackBaseline `
        -ExpectedMoreEquipmentSlotsCommittedGeneration $ExpectedMoreEquipmentSlotsCommittedGeneration `
        -ExpectedMoreEquipmentSlotsCommittedOccupied $ExpectedMoreEquipmentSlotsCommittedOccupied `
        -ExpectedMoreEquipmentSlotsCommittedSlots $expectedMoreEquipmentSlotsCommittedSlots `
        -MoreEquipmentSlotsInterruptedCandidateObservationEnabled ([bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery) `
        -ExpectedMoreEquipmentSlotsCandidatePreGeneration $ExpectedMoreEquipmentSlotsCandidatePreGeneration `
        -MoreEquipmentSlotsTransitionPhase $MoreEquipmentSlotsTransitionPhase `
        -G6LifecycleCases @($qaG6LifecycleCases.ToArray()) `
        -G6InitialDelaySeconds 3 `
        -G6AutoFishingScenario $autoFishingScenarioEffective `
        -G6AutoFishingCastChargeRatio $AutoFishingCastChargeRatio `
        -G6AutoFishingSoakLoops $AutoFishingSoakLoops `
        -G6AutoFishingToggleKey $autoFishingToggleKeyEffective `
        -G6AutoFishingMiniGameComplete ([bool]$AutoExerciseAutoFishingMiniGameComplete) `
        -G6AutoFishingMonoGate ([bool]$AutoFishingMonoGate) `
        -G6SaveLoadCycleCount $g6SaveLoadCycleCountEffective `
        -G6SaveLoadCycleInitialTitleIdleSeconds $g6SaveLoadCycleInitialIdleEffective `
        -G6SaveLoadCycleIntervalSeconds $SaveLoadCycleIntervalSeconds `
        -G6SaveLoadCycleInSaveSeconds $SaveLoadCycleInSaveSeconds `
        -G6PreLoadGcProbe ([bool]$AutoExercisePreLoadGcProbe) `
        -G6SaveLoadObjectSnapshotMode $SaveLoadObjectSnapshotMode `
        -G6RootIsolationProfile $SmokeRootIsolationProfile `
        -G6OwnerRootIsolationProfile $SmokeOwnerRootIsolationProfile `
        -G6NativeLoadContinuationProbe $SmokeNativeLoadContinuationProbe `
        -G6PendingPressureSeconds $g6PendingPressureSecondsEffective `
        -G6PendingPressureIntervalSeconds $SaveLoadCyclePendingPressureIntervalSeconds `
        -G6LongTitleIdleSeconds $TitleIdleBeforeSaveSeconds
}
else {
    $null
}
$officialModProfileSummary = Set-SmokeOfficialModProfile -Profile $OfficialModProfile -EvidencePath $evidence -ExtraEnabledIds $OfficialModProfileExtraEnabledIds -IsolateAll ([bool]$IsolateAllOfficialMods)
$officialModProfileRestored = -not [bool]$officialModProfileSummary.Applied
$requestedOfficialModProfileExtraIds = @($OfficialModProfileExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
$officialModProfileAppliedOk = $OfficialModProfile -eq 'Current' -or [bool]$officialModProfileSummary.Applied
$officialModProfileExtraEnabledOk = @($requestedOfficialModProfileExtraIds | Where-Object { @($officialModProfileSummary.EnabledIds) -notcontains $_ }).Count -eq 0
$officialModProfileOilDisabledOk = -not [bool]$ExpectOilAbsent -or @($officialModProfileSummary.DisabledIds) -contains 'Local.DTMAPI_Oil'
$exactElevenProductProfileGateRequested = [bool]$AssertPublishedProductCombination -or [bool]$AssertPublishedProductsDisabled -or
    [bool]$AssertNoQaUiEvidence -or
    ([bool]$QaObserveEquipmentSlotsUi -and
     $MoreEquipmentSlotsTransitionPhase -ne 'U1') -or
    $OfficialModProfile -eq 'Local11'
$publishedProductExpectedEnabledIds = if ($OfficialModProfile -eq 'Published11' -and -not $AssertPublishedProductsDisabled) {
    @(Get-SmokePublishedProductWorkshopIds | Sort-Object)
}
elseif ($OfficialModProfile -eq 'Local11') {
    @(Get-SmokePublishedProductLocalIds | Sort-Object)
}
else {
    @()
}
$publishedProductActualEnabledIds = @($officialModProfileSummary.EnabledIds | Sort-Object)
$publishedProductSelectionOk = if ($exactElevenProductProfileGateRequested) {
    [string]::Equals(
        (@($publishedProductExpectedEnabledIds) -join "`n"),
        (@($publishedProductActualEnabledIds) -join "`n"),
        [System.StringComparison]::Ordinal)
}
else {
    $true
}
$externalPlayerInputWorkshopSelectionOk = -not [bool]$RequireExternalPlayerInputGate -or
    (@($officialModProfileSummary.EnabledIds) -contains 'Workshop.3742714442' -and
     @($officialModProfileSummary.EnabledIds) -notcontains 'Local.DTMAPI_YKeyConsole')
$officialModProfileSelectionOk = $officialModProfileAppliedOk -and $officialModProfileExtraEnabledOk -and $officialModProfileOilDisabledOk -and $publishedProductSelectionOk -and $externalPlayerInputWorkshopSelectionOk
if (-not $officialModProfileSelectionOk) {
    if ([bool]$officialModProfileSummary.Applied) {
        $officialModProfileRestored = Restore-SmokeOfficialModProfile -Summary $officialModProfileSummary -EvidencePath $evidence -Phase 'selection-gate'
    }
    Write-SmokeJsonObject -Path (Join-Path $evidence 'official-mod-profile-gate.json') -Value ([ordered]@{
        Profile = $OfficialModProfile
        IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
        AppliedOk = $officialModProfileAppliedOk
        ExtraEnabledOk = $officialModProfileExtraEnabledOk
        OilDisabledOk = $officialModProfileOilDisabledOk
        PublishedProductSelectionOk = $publishedProductSelectionOk
        ExternalPlayerInputWorkshopSelectionOk = $externalPlayerInputWorkshopSelectionOk
        RestoredAfterFailure = $officialModProfileRestored
        ExpectedEnabledIds = @($publishedProductExpectedEnabledIds)
        RequestedExtraEnabledIds = @($requestedOfficialModProfileExtraIds)
        ActualEnabledIds = @($officialModProfileSummary.EnabledIds)
        ActualDisabledIds = @($officialModProfileSummary.DisabledIds)
        Reason = [string]$officialModProfileSummary.Reason
    })
    throw "Official Mod smoke profile gate failed before launch. profile=$OfficialModProfile; appliedOk=$officialModProfileAppliedOk; extraEnabledOk=$officialModProfileExtraEnabledOk; oilDisabledOk=$officialModProfileOilDisabledOk; externalWorkshopYConsoleOk=$externalPlayerInputWorkshopSelectionOk; restored=$officialModProfileRestored."
}
$actualEnabledModInfoIds = @(Get-DtmApiEnabledModInfoIds -ModInfoPath ([string]$officialModProfileSummary.EnablementPath))
$pendingProductRecovery = Get-SmokePendingProductRecovery `
    -CatalogPath (Join-Path $repo 'tools/release/dtmapi-product-catalog.json') -StateDir $dtmapiDir `
    -OfficialLocalModsRoot (Join-Path (Get-DolocTownLivePersistentRootForSmoke) 'MODS') `
    -WorkshopContentRoot ([IO.Path]::GetFullPath((Join-Path $gameDir '../../workshop/content/2285550'))) `
    -EnabledSourceIds $actualEnabledModInfoIds `
    -ArchiveIndex $(if($SaveSlot -gt 0){Get-SmokeNativeArchiveIndex -UiSaveSlot $SaveSlot}else{-1}) `
    -SaveTestMode $SaveTestMode `
    -ExplicitRecoveryScenario ([bool]$moreEquipmentSlotsColdRecoveryTransitionRequested -or [bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold -or [bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery)
Write-SmokeJsonObject -Path (Join-Path $evidence 'pending-product-recovery-preflight.json') -Value $pendingProductRecovery
if(-not [bool]$pendingProductRecovery.Passed){
    throw ($pendingProductRecovery.Reason+': the selected slot has known MoreEquipment recovery or migration state that can write on SaveLoaded. Resolve the test prerequisite with an appropriate slot/profile or its explicit recovery scenario before another launch.')
}
if ($moreEquipmentSlotsColdRecoveryTransitionRequested) {
    $moreEquipmentSlotsColdOfficialDisabledOk =
        $actualEnabledModInfoIds -notcontains
            'Local.DTMAPI_MoreEquipmentSlots' -and
        @($officialModProfileSummary.DisabledIds) -contains
            'Local.DTMAPI_MoreEquipmentSlots'
    Write-SmokeJsonObject -Path (
        Join-Path $evidence `
            'more-equipment-slots-cold-official-disable.json') `
        -Value ([ordered]@{
            SourceId = 'Local.DTMAPI_MoreEquipmentSlots'
            Profile = $OfficialModProfile
            IsolateAllOfficialMods = [bool]$IsolateAllOfficialMods
            EnabledIds = @($actualEnabledModInfoIds)
            DisabledIds = @($officialModProfileSummary.DisabledIds)
            Passed = $moreEquipmentSlotsColdOfficialDisabledOk
        })
    if (-not $moreEquipmentSlotsColdOfficialDisabledOk) {
        throw 'MoreEquipmentSlots cold recovery requires the official Local source to be disabled by the reversible native profile.'
    }
}
$moreSavesStartupMigrationRoot = if ($disposableSaveFixtureRequested) {
    [System.IO.Path]::GetFullPath((Join-Path $disposableSaveFixtureRootResolved 'SAVE'))
}
else {
    [System.IO.Path]::GetFullPath((Join-Path (Get-DolocTownLivePersistentRootForSmoke) 'SAVE'))
}
$moreSavesStartupRisk = Get-SmokeMoreSavesStartupRisk `
    -EnablementPath ([string]$officialModProfileSummary.EnablementPath) `
    -SaveRoot $moreSavesStartupMigrationRoot `
    -OfficialLocalModsRoot (Join-Path (Get-DolocTownLivePersistentRootForSmoke) 'MODS') `
    -WorkshopContentRoot ([System.IO.Path]::GetFullPath((Join-Path $gameDir '../../workshop/content/2285550'))) `
    -SaveTestMode $SaveTestMode `
    -DisposableFixtureRequested $disposableSaveFixtureRequested `
    -StageQaHost ([bool]$StageQaHost) -DirectExe ([bool]$DirectExe) -UseSteam ([bool]$UseSteam)
$enabledMoreSavesIds = @($moreSavesStartupRisk.EnabledMoreSavesIds)
$moreSavesLegacyCandidates = @($moreSavesStartupRisk.LegacyCandidates)
$moreSavesStartupMigrationDetected = [bool]$moreSavesStartupRisk.StartupMigrationDetected
$moreSavesStartupMigrationClassified = [bool]$moreSavesStartupRisk.Passed
Write-SmokeJsonObject -Path (Join-Path $evidence 'moresaves-startup-migration-preflight.json') -Value ([ordered]@{
    Profile = $OfficialModProfile
    EnablementPath = [System.IO.Path]::GetFullPath([string]$officialModProfileSummary.EnablementPath)
    EnabledMoreSavesIds = @($enabledMoreSavesIds)
    AvailableSources = @($moreSavesStartupRisk.AvailableSources)
    PackageClassificationUncertain = [bool]$moreSavesStartupRisk.PackageClassificationUncertain
    EffectiveSaveRoot = $moreSavesStartupMigrationRoot
    ExactLegacyCandidateDomain = 'indices 6..11 x current/prev/bak legacy 1.00 names'
    LegacyCandidateCount = $moreSavesLegacyCandidates.Count
    LegacyCandidates = @($moreSavesLegacyCandidates)
    StartupMigrationDetected = $moreSavesStartupMigrationDetected
    RequiredSaveTestModeWhenDetected = 'ArchiveMutation'
    SaveTestMode = $SaveTestMode
    DisposableFixtureRequested = $disposableSaveFixtureRequested
    StageQaHost = [bool]$StageQaHost
    DirectExe = [bool]$DirectExe
    UseSteam = [bool]$UseSteam
    PreRuntimeSaveGuardRequired = $moreSavesStartupMigrationDetected
    Passed = $moreSavesStartupMigrationClassified
})
if (-not $moreSavesStartupMigrationClassified) {
    throw "MoreSaves startup migration preflight failed. A loadable enabled source and legacy archives require ArchiveMutation, a disposable fixture, StageQaHost and DirectExe; an unrecognized enabled package must be resolved before launch. See moresaves-startup-migration-preflight.json."
}
# Official Local packages are selected by Local.* enabled state and native
# priority. Functional smoke profiles never publish LocalDevelopment source
# overrides. The recovery-only state clear below is retained solely for the
# bounded Core-only legacy isolation fixture.
$localAuthorSourceRequestedIds = @()
$coreOnlyNoDemandSourceIsolation = $autoFishingNoDemandFrameProfile -and
    $OfficialModProfile -eq 'CoreOnly' -and [bool]$IsolateAllOfficialMods
$localAuthorSourceRequested = $localAuthorSourceRequestedIds.Count -gt 0 -or $coreOnlyNoDemandSourceIsolation
if ($localAuthorSourceRequested) {
    $local11AuthorSourceSummary = Set-SmokeRecoveryOnlyAuthorSourceState -GameDir $gameDir -EvidencePath $evidence
    $local11AuthorSourceAppliedOk = [bool]$local11AuthorSourceSummary.Applied -and @($local11AuthorSourceSummary.Selections).Count -eq $localAuthorSourceRequestedIds.Count
    $local11AuthorSourceRestored = -not [bool]$local11AuthorSourceSummary.Applied
    if (-not $local11AuthorSourceAppliedOk) {
        throw 'Recovery-only Author source isolation did not publish an empty selection set.'
    }
}
if ($DisableSecondMotorForSmoke) {
    Write-Host "SecondMotor smoke package is archived; -DisableSecondMotorForSmoke is now a no-op compatibility flag."
}
$usesOneActionConfigSmoke = [bool]$AutoExerciseOneActionResourceHit -or [bool]$AutoExerciseOneActionWrongTool -or [bool]$AutoExerciseOneActionFuelFeed -or [bool]$AutoExerciseOneActionVegetation -or [bool]$AutoPressOneActionMenuKey
$usesActionSpeedConfigSmoke = [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply -or [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoOpenTitleSettingsMenu -or [bool]$batch5GcLadderActionSpeed
$usesAutoFishingConfigSmoke = [bool]$AutoExerciseAutoFishingPhase -or [bool]$AutoPressAutoFishingHotkey -or [bool]$AutoExerciseAutoFishingMovementCancel -or [bool]$AutoOpenTitleSettingsMenu -or [bool]$batch5GcLadderAutoFishing -or [bool]$Batch6AutoFishingPilot -or ([bool]$Batch6AutoFishingManagerLifecycle -and $Batch6AutoFishingManagerMode -eq 'SameProcessDisable')
if ($usesActionSpeedConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $actionSpeedConfigPath) | Out-Null
    if (Test-Path $actionSpeedConfigPath) {
        Copy-Item -Force -LiteralPath $actionSpeedConfigPath -Destination $actionSpeedConfigBackup
        $actionSpeedConfigHadOriginal = $true
    }
    else {
        'No pre-existing ActionSpeed config file.' | Set-Content -LiteralPath $actionSpeedConfigBackup
    }
    $actionSpeedMultiplier = if ($batch5GcLadderActionSpeed) { $Batch5GcLadderMultiplier } elseif ($AutoExerciseActionSpeedConfigApply) { 2 } else { 3 }
    $actionSpeedProductEnabled = (-not $batch5GcLadderActionSpeed) -or $Batch5GcLadderLevel -ne 'L0'
    $actionSpeedLadderFeatureEnabled = [bool]$batch5GcLadderActionSpeed -and $Batch5GcLadderLevel -ne 'L0'
    $actionSpeedToolEnabled = if ($batch5GcLadderActionSpeed) { $actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -eq 'Tool' } else { [bool]$AutoExerciseActionSpeedTool -or [bool]$AutoExerciseActionSpeedConfigApply }
    $actionSpeedInteractionEnabled = [bool]$AutoExerciseActionSpeedInteraction -or [bool]$AutoOpenTitleSettingsMenu
    $actionSpeedMachineEnabled = $actionSpeedInteractionEnabled -or ($actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -eq 'Interact')
    $actionSpeedEatEnabled = $actionSpeedInteractionEnabled -or ($actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -in @('Eat','ContinuousUse'))
    $actionSpeedContinuousEnabled = $actionSpeedInteractionEnabled -or ($actionSpeedLadderFeatureEnabled -and $Batch5GcLadderWorkload -eq 'ContinuousUse')
    @{
        Enabled = $actionSpeedProductEnabled
        Profile = 'Debug'
        ToolSpeedEnabled = $actionSpeedToolEnabled
        ToolMultiplier = $actionSpeedMultiplier
        BottleFillSpeedEnabled = $actionSpeedContinuousEnabled
        BottleFillMultiplier = $actionSpeedMultiplier
        EatDrinkSpeedEnabled = $actionSpeedEatEnabled
        EatDrinkMultiplier = $actionSpeedMultiplier
        MachineAddSpeedEnabled = $actionSpeedMachineEnabled
        MachineAddMultiplier = $actionSpeedMultiplier
        HarvestSpeedEnabled = $actionSpeedInteractionEnabled
        HarvestMultiplier = 3
        PlantSpeedEnabled = $actionSpeedInteractionEnabled
        PlantMultiplier = 3
        AutoFillBottle = $actionSpeedInteractionEnabled
        AutoFillStrong = $actionSpeedInteractionEnabled
        ContinuousDrinkWithRightClick = $actionSpeedContinuousEnabled
        ContinuousDrinkHoldKey = 'None'
        MenuKey = 'F10'
        AutoActionCooldownSeconds = 0.25
        DebugLabel = 'DTMAPI smoke'
    } | ConvertTo-Json | Set-Content -LiteralPath $actionSpeedConfigPath
}
if ($usesOneActionConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $oneActionConfigPath) | Out-Null
    if (Test-Path $oneActionConfigPath) {
        Copy-Item -Force -LiteralPath $oneActionConfigPath -Destination $oneActionConfigBackup
        $oneActionConfigHadOriginal = $true
    }
    else {
        'No pre-existing OneAction config file.' | Set-Content -LiteralPath $oneActionConfigBackup
    }
    @{
        Enabled = $true
        CompleteTrees = $true
        CompleteOres = $true
        CompleteGarbage = $true
        CompleteWeeds = $true
        CompleteMachineFuel = [bool]$AutoExerciseOneActionFuelFeed
        CompleteFeeder = [bool]$AutoExerciseOneActionFuelFeed
        VerboseLogging = $true
        MenuKey = 'F11'
    } | ConvertTo-Json | Set-Content -LiteralPath $oneActionConfigPath
}
if ($usesAutoFishingConfigSmoke) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $autoFishingConfigPath) | Out-Null
    if (Test-Path $autoFishingConfigPath) {
        Copy-Item -Force -LiteralPath $autoFishingConfigPath -Destination $autoFishingConfigBackup
        $autoFishingConfigHadOriginal = $true
    }
    else {
        'No pre-existing AutoFishing config file.' | Set-Content -LiteralPath $autoFishingConfigBackup
    }
    $autoFishingSkip = $false
    $autoFishingInstantBite = $false
    $autoFishingFastAnimations = $false
    switch ($autoFishingScenarioEffective) {
        'DefaultLoop' { }
        'InstantBite' { $autoFishingInstantBite = $true }
        'SkipMiniGame' { $autoFishingSkip = $true }
        'InstantSkip' {
            $autoFishingSkip = $true
            $autoFishingInstantBite = $true
        }
        'FastAnimations' {
            $autoFishingFastAnimations = $true
        }
        'CombinedInstantComplete' {
            $autoFishingInstantBite = $true
            $autoFishingFastAnimations = $true
        }
        'CombinedInstantSkip' {
            $autoFishingSkip = $true
            $autoFishingInstantBite = $true
            $autoFishingFastAnimations = $true
        }
        default { }
    }
    [ordered]@{
        AnimationMultiplier = $(if ($Batch6AutoFishingPilot) { $Batch6AutoFishingMultiplier } elseif ($Batch6AutoFishingManagerLifecycle) { 1 } elseif ($batch5GcLadderAutoFishing) { $Batch5GcLadderMultiplier } elseif ($autoFishingPerformanceFishRun -or $AutoFishingMonoGate) { 4 } else { 3 })
        CastChargeRatio = $(if ($Batch6AutoFishingPilot) { $Batch6AutoFishingCastChargeRatio } elseif ($Batch6AutoFishingManagerLifecycle) { 0 } else { $AutoFishingCastChargeRatio })
        FastAnimations = $autoFishingFastAnimations
        InstantBite = $autoFishingInstantBite
        SkipMiniGame = $autoFishingSkip
        ToggleKey = $autoFishingToggleKeyEffective
    } | ConvertTo-Json | Set-Content -LiteralPath $autoFishingConfigPath
}

$externalPlayerInputAttempts = New-Object 'System.Collections.Generic.List[object]'
$equipmentSlotsPlayerInputAttempts = New-Object 'System.Collections.Generic.List[object]'
$animalViewerPlayerInputAttempts = New-Object 'System.Collections.Generic.List[object]'

$saveLoadCycleCountEffective = if ($AutoExerciseSaveLoadCycle -and $SaveLoadCycleCount -le 0) { 3 } else { $SaveLoadCycleCount }
$saveLoadCycleInitialIdleEffective = if ($AutoExerciseSaveLoadCycle) { [Math]::Max(1, $SaveLoadCycleInitialTitleIdleSeconds) } else { 0 }
$saveLoadCyclePendingPressureSecondsEffective = if ($AutoExerciseSaveLoadCyclePendingPressure) { if ($SaveLoadCyclePendingPressureSeconds -le 0) { 1200 } else { $SaveLoadCyclePendingPressureSeconds } } else { 0 }
$minimumLongIdleExitSeconds = if ($TitleIdleBeforeSaveSeconds -gt 0) { $TitleIdleBeforeSaveSeconds + 90 } elseif ($AutoExerciseSaveLoadCycle) { $saveLoadCycleInitialIdleEffective + 90 } elseif ($AutoExerciseSaveLoadCyclePendingPressure) { $saveLoadCyclePendingPressureSecondsEffective + 180 } else { 0 }
$minimumAutoFishingPerformanceExitSeconds = if (-not $AutoFishingPerformance) { 0 } elseif ($AutoFishingPerformanceTargetFrames -gt 0) { [int][Math]::Ceiling(($AutoFishingPerformanceWarmupFrames + $AutoFishingPerformanceTargetFrames) / 20.0) + 180 } elseif ($AutoFishingPerformanceTargetFish -eq 0) { $AutoFishingPerformanceZeroWarmupSeconds + $AutoFishingPerformanceZeroMeasureSeconds + 120 } else { ($AutoFishingPerformanceTargetFish * 3) + 180 }
$minimumLongIdleExitSeconds = [Math]::Max($minimumLongIdleExitSeconds, $minimumAutoFishingPerformanceExitSeconds)
if ($batch5GcLadderEnabled) {
    $minimumLongIdleExitSeconds = [Math]::Max($minimumLongIdleExitSeconds, $Batch5GcLadderMeasureSeconds + 180)
}
$noQaRunnerBudgetSeconds = if ($AssertNoQaUiEvidence -and $AutoExitAfterSecondsOverride -gt 0) {
    $AutoExitAfterSecondsOverride
}
elseif ($AssertNoQaUiEvidence) {
    $TimeoutSeconds
}
else {
    0
}
$noQaRunnerBudgetSource = if (-not $AssertNoQaUiEvidence) {
    'NotRequested'
}
elseif ($AutoExitAfterSecondsOverride -gt 0) {
    'AutoExitAfterSecondsOverride'
}
else {
    'TimeoutSeconds'
}
$noQaRunnerDeadline = $null
$noQaUiDeadline = $null
$noQaOrdinaryPlayerHandshakeShown = $false
$noQaSaveLoadedWaitSeconds = 0
$noQaSaveLoadedDeadlineExpired = $false
$noQaProcessWaitSeconds = 0
$noQaBehaviorCompletedAt = $null
$noQaBehaviorCompletedBeforeDeadlineOk = -not [bool]$AssertNoQaUiEvidence
$noQaBehaviorRemainingMilliseconds = 0
$autoExitAfterSeconds = if ($WaitForManualExit) {
    0
}
elseif ($AssertNoQaUiEvidence) {
    $noQaRunnerBudgetSeconds
}
elseif ($AutoExitAfterSecondsOverride -gt 0) {
    $AutoExitAfterSecondsOverride
}
elseif (($titleSettingsRequested -or $AutoOpenOfficialModUi) -and $SaveSlot -le 0) {
    [Math]::Max([Math]::Max(25, $TimeoutSeconds - 20), $minimumLongIdleExitSeconds)
}
else {
    [Math]::Max([Math]::Max(90, $TimeoutSeconds - 30), $minimumLongIdleExitSeconds)
}
$smokeRootIsolationSummaryPath = Join-Path $evidence 'smoke-root-isolation-summary.txt'
$smokeRootIsolationDisabled = if ($SmokeRootIsolationProfile -eq 'UiRuntime') { 'SaveSlots|EquipmentSlots|Camera' } else { '' }
$smokeRootIsolationUnsupported = if ($SmokeRootIsolationProfile -eq 'UiRuntime') { 'DebugConsoleHost' } else { '' }
$smokeRootIsolationActive = if ($SmokeRootIsolationProfile -eq 'UiRuntime') { 'AllExceptUiRuntimeDisabledAndUnsupported' } else { 'AllDefault' }
@(
    "SmokeRootIsolationProfile=$SmokeRootIsolationProfile",
    "SmokeRootIsolation.Disabled=$smokeRootIsolationDisabled",
    "SmokeRootIsolation.Unsupported=$smokeRootIsolationUnsupported",
    "SmokeRootIsolation.Active=$smokeRootIsolationActive",
    "GeneratedAt=$(Get-Date -Format o)"
) | Set-Content -LiteralPath $smokeRootIsolationSummaryPath
$ownerRootTypeIsolationSummaryPath = Join-Path $evidence 'owner-root-type-isolation-summary.txt'
$ownerRootTargetOwners = switch ($SmokeOwnerRootIsolationProfile) {
    'YConsoleZoomNoInput' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'YConsoleZoomNoEvents' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'YConsoleZoomNoInputEvents' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'YConsoleZoomNoConfig' { 'DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod' }
    'ZoomNoInput' { 'DTMAPI.ZoomMod' }
    'YConsoleNoInput' { 'DTMAPI.DebugConsoleMod' }
    default { '' }
}
$ownerRootTargetTypes = switch ($SmokeOwnerRootIsolationProfile) {
    'YConsoleZoomNoInput' { 'InputButton' }
    'YConsoleZoomNoEvents' { 'EventHandler' }
    'YConsoleZoomNoInputEvents' { 'InputButton|EventHandler' }
    'YConsoleZoomNoConfig' { 'ConfigPage' }
    'ZoomNoInput' { 'InputButton' }
    'YConsoleNoInput' { 'InputButton' }
    default { '' }
}
@(
    "SmokeOwnerRootIsolationProfile=$SmokeOwnerRootIsolationProfile",
    "TargetOwners=$ownerRootTargetOwners",
    "TargetRootTypes=$ownerRootTargetTypes",
    "SuppressedRootsExpected=$(if ($SmokeOwnerRootIsolationProfile -eq 'None') { 'False' } else { 'True' })",
    "SuppressedRootsObserved=",
    "SuppressionSucceeded=",
    "UnexpectedRemainingSuppressedRoots=",
    "UnexpectedDisabledCodeOwners=",
    "GeneratedAt=$(Get-Date -Format o)"
) | Set-Content -LiteralPath $ownerRootTypeIsolationSummaryPath
$nativeContinuationProbeSummaryPath = Join-Path $evidence 'native-continuation-probe-summary.txt'
$nativeContinuationSupported = if ($SmokeNativeLoadContinuationProbe -eq 'VersionPatcher') { 'DolocAPI.AfterLoadArchiveData|DolocTown.VersionPatcher.LoadAllVersionPatches|DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond|DolocTown.MapManager.Init' } else { '' }
$nativeContinuationUnsupported = if ($SmokeNativeLoadContinuationProbe -eq 'VersionPatcher') { 'DolocTown.TextureUtils.DrawArea(skipped-high-frequency)' } else { '' }
@(
    "SmokeNativeLoadContinuationProbe=$SmokeNativeLoadContinuationProbe",
    "ExpectedSupported=$nativeContinuationSupported",
    "ExpectedUnsupported=$nativeContinuationUnsupported",
    "GeneratedAt=$(Get-Date -Format o)"
) | Set-Content -LiteralPath $nativeContinuationProbeSummaryPath
$freshLogPath = Join-Path $dtmapiDir 'logs\latest.log'
$freshBepLogPath = Join-Path $gameDir 'BepInEx\LogOutput.log'
if (Test-Path $freshLogPath) {
    Remove-Item -Force -LiteralPath $freshLogPath
}
if (Test-Path $freshBepLogPath) {
    Remove-Item -Force -LiteralPath $freshBepLogPath
}

"Started=$($smokeRunStartedAt.ToString('o'))`nGameDir=$gameDir`nDtmApiStateDir=$dtmapiDir`nSaveTestMode=$SaveTestMode`nDisposableSaveFixtureRoot=$disposableSaveFixtureRootResolved`nSaveSlot=$SaveSlot`nIncludeHookProbe=$IncludeHookProbe`nLaunchMode=$(if ($launchViaSteam) { 'Steam' } else { 'DirectExe' })`nDisableSecondMotorForSmoke=$DisableSecondMotorForSmoke`nAutoSaveAfterLoad=$AutoSaveAfterLoad`nAutoReloadMods=$AutoReloadMods`nAutoExerciseExperimentalHooks=$AutoExerciseExperimentalHooks`nAutoExerciseActionSpeedTool=$AutoExerciseActionSpeedTool`nAutoExerciseActionSpeedConfigApply=$AutoExerciseActionSpeedConfigApply`nAutoExerciseActionSpeedInteraction=$AutoExerciseActionSpeedInteraction`nAutoExerciseOneActionResourceHit=$AutoExerciseOneActionResourceHit`nAutoExerciseOneActionWrongTool=$AutoExerciseOneActionWrongTool`nAutoExerciseOneActionFuelFeed=$AutoExerciseOneActionFuelFeed`nAutoExerciseOneActionVegetation=$AutoExerciseOneActionVegetation`nAutoExerciseAutoFishingPhase=$AutoExerciseAutoFishingPhase`nAutoExerciseLegacyFishingCompatibility=$AutoExerciseLegacyFishingCompatibility`nAutoExerciseAutoFishingMiniGameComplete=$AutoExerciseAutoFishingMiniGameComplete`nAutoFishingScenario=$autoFishingScenarioEffective`nAutoFishingCastChargeRatio=$AutoFishingCastChargeRatio`nAutoFishingSoakLoops=$AutoFishingSoakLoops`nAutoFishingToggleKey=$autoFishingToggleKeyEffective`nAutoFishingPerformance=$AutoFishingPerformance`nAutoFishingPerformanceProfile=$AutoFishingPerformanceProfile`nAutoExercisePauseMenuLayout=$AutoExercisePauseMenuLayout`nAutoExercisePauseMenuLayoutDelaySeconds=$AutoExercisePauseMenuLayoutDelaySeconds`nAutoExerciseTitleButtonLifecycle=$AutoExerciseTitleButtonLifecycle`nAutoExerciseModOwnerLifetime=$AutoExerciseModOwnerLifetime`nAutoExerciseSaveLoadCycle=$AutoExerciseSaveLoadCycle`nSaveLoadCycleCount=$saveLoadCycleCountEffective`nSaveLoadCycleInitialTitleIdleSeconds=$saveLoadCycleInitialIdleEffective`nSaveLoadCycleIntervalSeconds=$SaveLoadCycleIntervalSeconds`nSaveLoadCycleInSaveSeconds=$SaveLoadCycleInSaveSeconds`nAutoExercisePreLoadGcProbe=$AutoExercisePreLoadGcProbe`nSaveLoadObjectSnapshotMode=$SaveLoadObjectSnapshotMode`nSmokeRootIsolationProfile=$SmokeRootIsolationProfile`nSmokeNativeLoadContinuationProbe=$SmokeNativeLoadContinuationProbe`nAutoExerciseSaveLoadCyclePendingPressure=$AutoExerciseSaveLoadCyclePendingPressure`nSaveLoadCyclePendingPressureSeconds=$saveLoadCyclePendingPressureSecondsEffective`nAutoExerciseInstantSave=$AutoExerciseInstantSave`nAutoExerciseInstantSaveDelaySeconds=$AutoExerciseInstantSaveDelaySeconds`nDebugConsoleSaveAcceptancePhase=$DebugConsoleSaveAcceptancePhase`nExpectedDebugConsoleMoney=$ExpectedDebugConsoleMoney`nAutoExerciseDebugConsole=$AutoExerciseDebugConsole`nAutoExerciseDebugConsoleMouseGive=$AutoExerciseDebugConsoleMouseGive`nAutoExerciseDebugInventory=$AutoExerciseDebugInventory`nAutoExerciseDebugWeather=$AutoExerciseDebugWeather`nAutoExerciseDebugTeleport=$AutoExerciseDebugTeleport`nAutoExerciseDebugTime=$AutoExerciseDebugTime`nAutoExerciseDebugMovement=$AutoExerciseDebugMovement`nAutoExerciseAdvancedDebug=$AutoExerciseAdvancedDebug`nAutoExerciseNewContentApis=$AutoExerciseNewContentApis`nExpectOilAbsent=$ExpectOilAbsent`nOilOnly=$OilOnly`nAutoExerciseMineContentApis=$AutoExerciseMineContentApis`nAutoExerciseZoom=$AutoExerciseZoom`nAutoExerciseZoomProductNative=$AutoExerciseZoomProductNative`nAutoExerciseChestLocatorEnhancer=$AutoExerciseChestLocatorEnhancer`nAutoExerciseMoreSavesOfficialSaveUi=$AutoExerciseMoreSavesOfficialSaveUi`nAutoExerciseStrongPlantingGun=$AutoExerciseStrongPlantingGun`nAutoExerciseCropHarvestingApi=$AutoExerciseCropHarvestingApi`nAutoExerciseCustomEntityApis=$AutoExerciseCustomEntityApis`nAutoExerciseFishRoeTooltip=$AutoExerciseFishRoeTooltip`nAutoExerciseDiagnosticsSnapshot=$AutoExerciseDiagnosticsSnapshot`nQaObserveSaveLoaded=$QaObserveSaveLoaded`nQaObserveSaveSaved=$QaObserveSaveSaved`nQaObserveWorkshopReloadCompleted=$QaObserveWorkshopReloadCompleted`nAutoExerciseAudioReplacement=$AutoExerciseAudioReplacement`nAutoExerciseHatchAnimalVoice=$AutoExerciseHatchAnimalVoice`nAutoPressAutoFishingHotkey=$AutoPressAutoFishingHotkey`nAutoOpenTitleSettingsMenu=$AutoOpenTitleSettingsMenu`nAutoOpenTitleSettingsStatusPage=$AutoOpenTitleSettingsStatusPage`nAutoOpenTitleSettingsManagerMvp=$AutoOpenTitleSettingsManagerMvp`nAutoOpenOfficialModUi=$AutoOpenOfficialModUi`nAutoOpenAnimalPanel=$AutoOpenAnimalPanel`nTitleIdleBeforeSaveSeconds=$TitleIdleBeforeSaveSeconds`nFatalWindowCrashDumpGraceSeconds=$FatalWindowCrashDumpGraceSeconds`nFatalWindowProcessDumpMode=$FatalWindowProcessDumpMode`nFatalWindowPostCloseCrashDumpWaitSeconds=$FatalWindowPostCloseCrashDumpWaitSeconds`nAutoExitAfterSeconds=$autoExitAfterSeconds" | Set-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoExerciseMoreEquipmentSlots=$([bool]$AutoExerciseMoreEquipmentSlots)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"SetupMoreEquipmentSlotsCommittedShield=$([bool]$SetupMoreEquipmentSlotsCommittedShield)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreEquipmentSlotsTransitionPhase=$MoreEquipmentSlotsTransitionPhase" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"DamageMoreEquipmentSlotsCommittedShieldNoNativeSave=$([bool]$DamageMoreEquipmentSlotsCommittedShieldNoNativeSave)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave=$([bool]$BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ObserveMoreEquipmentSlotsInterruptedCandidateRecovery=$([bool]$ObserveMoreEquipmentSlotsInterruptedCandidateRecovery)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoExerciseMoreEquipmentSlotsNoNativeSave=$([bool]$AutoExerciseMoreEquipmentSlotsNoNativeSave)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ObserveMoreEquipmentSlotsNoNativeSaveCold=$([bool]$ObserveMoreEquipmentSlotsNoNativeSaveCold)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertMoreEquipmentSlotsColdRecovery=$([bool]$AssertMoreEquipmentSlotsColdRecovery)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"SmokeOwnerRootIsolationProfile=$SmokeOwnerRootIsolationProfile" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoPressOneActionMenuKey=$([bool]$AutoPressOneActionMenuKey)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertProductOwnerRefresh=$([bool]$AssertProductOwnerRefresh)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertAdvancedProductOwnerDeactivation=$([bool]$AssertAdvancedProductOwnerDeactivation)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AdvancedProductOwnerDeactivationOwnerIds=$([string]::Join('|', @($AdvancedProductOwnerDeactivationOwnerIds)))" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoReturnHomeAfterProductOwnerRefresh=$([bool]$AssertProductOwnerRefresh)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoExerciseAutoFishingMovementCancel=$([bool]$AutoExerciseAutoFishingMovementCancel)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"SkipInstall=$([bool]$SkipInstall)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"PackagePayloadRoot=$PackagePayloadRoot" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"RequireExternalPlayerInputGate=$([bool]$RequireExternalPlayerInputGate)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ExternalPlayerInputHoldMilliseconds=$ExternalPlayerInputHoldMilliseconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ExternalPlayerInputHandshakeToken=$externalPlayerInputHandshakeToken" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"ExternalPlayerInputCompletionMarkerPath=$externalPlayerInputCompletionMarkerPath" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertPublishedProductCombination=$([bool]$AssertPublishedProductCombination)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertPublishedProductsDisabled=$([bool]$AssertPublishedProductsDisabled)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AssertNoQaUiEvidence=$([bool]$AssertNoQaUiEvidence)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"Issue011Acceptance=$([bool]$Issue011Acceptance)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"AutoDriveNoQaAnimalViewer=$([bool]$AutoDriveNoQaAnimalViewer)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"NoQaRunnerBudgetSeconds=$noQaRunnerBudgetSeconds" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"NoQaRunnerBudgetSource=$noQaRunnerBudgetSource" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"NoQaAutoExitSemantics=$(if ($AssertNoQaUiEvidence) { 'RunnerDeadlineOnly;InternalQaAutoLoad=False;InternalQaReturnHome=False' } else { 'NotRequested' })" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"StageQaHost=$([bool]$StageQaHost)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaObserveContentMetadata=$([bool]$QaObserveContentMetadata)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4TitleSettingsUiEnabled=$qaG4TitleSettingsUiEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4OfficialModUiEnabled=$qaG4OfficialModUiEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4PauseMenuLayoutEnabled=$qaG4PauseMenuLayoutEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4DebugConsoleEnabled=$qaG4DebugConsoleEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4SaveSlotsPagingEnabled=$qaG4SaveSlotsPagingEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreSavesFixed12AcceptancePhase=$MoreSavesFixed12AcceptancePhase" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreSavesFixed12OfficialLocalRootMode=$moreSavesFixed12OfficialLocalRootMode" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreSavesFixed12OfficialLocalProductRoot=$moreSavesFixed12OfficialLocalProductRoot" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4AnimalObservationEnabled=$qaG4AnimalObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4EquipmentSlotsObservationEnabled=$qaG4EquipmentSlotsObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"MoreEquipmentSlots100UiAcceptanceRequested=$moreEquipmentSlots100UiAcceptanceRequested" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4AudioObservationEnabled=$qaG4AudioObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4HatchVoiceEnabled=$qaG4HatchVoiceEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4CameraPlayableEnabled=$qaG4CameraPlayableEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ContentMetadataObservationEnabled=$qaG4ContentMetadataObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ExternalPlayerInputObservationEnabled=$qaG4ExternalPlayerInputObservationEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ContinuousHomePageTerminalEnabled=$qaG4ContinuousHomePageTerminalEnabled" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaG4ContinuousHomePageRequiresSaveLoaded=$qaG4ContinuousHomePageRequiresSaveLoaded" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
"QaHostRunId=$(if ($null -ne $qaHostStage) { [string]$qaHostStage.RunId } else { '' })" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')

$officialModProfileSummaryLines = @(
    "OfficialModProfile=$OfficialModProfile",
    "IsolateAllOfficialMods=$([bool]$IsolateAllOfficialMods)",
    "OfficialModProfileApplied=$($officialModProfileSummary.Applied)",
    "OfficialModProfileEnabledIds=$([string]::Join('|', @($officialModProfileSummary.EnabledIds)))",
    "OfficialModProfileDisabledIds=$([string]::Join('|', @($officialModProfileSummary.DisabledIds)))",
    "OfficialModProfileBackupPath=$($officialModProfileSummary.BackupPath)"
)
$officialModProfileSummaryLines | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
