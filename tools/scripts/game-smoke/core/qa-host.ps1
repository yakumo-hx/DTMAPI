function New-SmokeQaHostStage {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [bool] $SkipQaBuild,
        [Parameter(Mandatory = $true)] [bool] $AutoFishingPerformanceEnabled,
        [Parameter(Mandatory = $true)] [string] $AutoFishingPerformanceProfile,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceTargetFish,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceWarmupFish,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceZeroWarmupSeconds,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceZeroMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceWarmupFrames,
        [Parameter(Mandatory = $true)] [int] $AutoFishingPerformanceTargetFrames,
        [Parameter(Mandatory = $true)] [bool] $Batch5GcLadderEnabled,
        [Parameter(Mandatory = $true)] [string] $Batch5GcLadderDomain,
        [Parameter(Mandatory = $true)] [string] $Batch5GcLadderLevel,
        [Parameter(Mandatory = $true)] [string] $Batch5GcLadderWorkload,
        [Parameter(Mandatory = $true)] [double] $Batch5GcLadderMultiplier,
        [Parameter(Mandatory = $true)] [int] $Batch5GcLadderMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch5GcLadderSampleSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch5GcLadderTargetUnits,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingEnabled,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingLevel,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingScenario,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingSampleSeconds,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingWarmupFish,
        [Parameter(Mandatory = $true)] [int] $Batch6AutoFishingTargetFish,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingFormal,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingFormalContract,
        [Parameter(Mandatory = $true)] [double] $Batch6AutoFishingMultiplier,
        [Parameter(Mandatory = $true)] [double] $Batch6AutoFishingCastChargeRatio,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingManualMovementCancel,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedPackageSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedEntrySha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedManifestSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingExpectedPolicySha256,
        [Parameter(Mandatory = $true)] [bool] $Batch6AutoFishingManagerEnabled,
        [Parameter(Mandatory = $true)] [string] $Batch6AutoFishingManagerMode,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerProductRoot,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerMarkerSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedPackageSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedEntrySha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedManifestSha256,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Batch6AutoFishingManagerExpectedPolicySha256,
        [Parameter(Mandatory = $true)] [bool] $CustomEntityContractEnabled,
        [Parameter(Mandatory = $true)] [bool] $FishRoeTooltipObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $DiagnosticsSnapshotEnabled,
        [Parameter(Mandatory = $true)] [string] $DiagnosticsScenario,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $DiagnosticsExpectedFeatureIds,
        [Parameter(Mandatory = $true)] [bool] $ObserveSaveLoaded,
        [bool] $WaitForManualExit = $false,
        [Parameter(Mandatory = $true)] [bool] $ObserveSaveSaved,
        [Parameter(Mandatory = $true)] [bool] $ObserveWorkshopReloadCompleted,
        [Parameter(Mandatory = $true)] [string] $SaveTestMode,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $DisposableSaveFixtureSaveRoot,
        [Parameter(Mandatory = $true)] [bool] $RequireDisposableSaveRedirect,
        [Parameter(Mandatory = $true)] [bool] $TitleSettingsUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerStatusUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $ManagerMvpUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $OfficialModUiEnabled,
        [Parameter(Mandatory = $true)] [bool] $PauseMenuLayoutEnabled,
        [Parameter(Mandatory = $true)] [bool] $DebugConsoleEnabled,
        [Parameter(Mandatory = $true)] [bool] $SaveSlotsPagingEnabled,
        [Parameter(Mandatory = $true)] [int] $SaveSlot,
        [Parameter(Mandatory = $true)] [bool] $AnimalObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $EquipmentSlotsObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $MoreEquipmentSlots100UiAcceptanceEnabled,
        [Parameter(Mandatory = $true)] [bool] $AudioObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $HatchVoiceEnabled,
        [Parameter(Mandatory = $true)] [bool] $CameraPlayableEnabled,
        [Parameter(Mandatory = $true)] [bool] $ContentMetadataObservationEnabled,
        [Parameter(Mandatory = $true)] [bool] $ContentMetadataExpectOilAbsent,
        [Parameter(Mandatory = $true)] [bool] $ContinuousHomePageTerminalEnabled,
        [Parameter(Mandatory = $true)] [bool] $ContinuousHomePageRequiresSaveLoaded,
        [Parameter(Mandatory = $true)] [double] $ContinuousHomePageSeconds,
        [Parameter(Mandatory = $true)] [bool] $TitleLifecycleEnabled,
        [Parameter(Mandatory = $true)] [bool] $ProductOwnerRefreshEnabled,
        [Parameter(Mandatory = $true)] [bool] $AdvancedProductOwnerDeactivationEnabled,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $AdvancedProductOwnerDeactivationOwnerIds,
        [Parameter(Mandatory = $true)] [bool] $ExternalPlayerInputObservationEnabled,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExternalPlayerInputToken,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExternalPlayerInputMarkerPath,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExternalPlayerInputEvidenceRoot,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $G5WorldMutationCases,
        [Parameter(Mandatory = $true)] [string] $DebugConsoleSaveAcceptancePhase,
        [Parameter(Mandatory = $true)] [int] $ExpectedDebugConsoleMoney,
        [Parameter(Mandatory = $true)] [int] $ExpectedMoreEquipmentSlotsBackpackBaseline,
        [Parameter(Mandatory = $true)] [long] $ExpectedMoreEquipmentSlotsCommittedGeneration,
        [Parameter(Mandatory = $true)] [int] $ExpectedMoreEquipmentSlotsCommittedOccupied,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $ExpectedMoreEquipmentSlotsCommittedSlots,
        [Parameter(Mandatory = $true)] [bool] $MoreEquipmentSlotsInterruptedCandidateObservationEnabled,
        [Parameter(Mandatory = $true)] [long] $ExpectedMoreEquipmentSlotsCandidatePreGeneration,
        [Parameter(Mandatory = $true)] [string] $MoreEquipmentSlotsTransitionPhase,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $G6LifecycleCases,
        [Parameter(Mandatory = $true)] [int] $G6InitialDelaySeconds,
        [Parameter(Mandatory = $true)] [string] $G6AutoFishingScenario,
        [Parameter(Mandatory = $true)] [double] $G6AutoFishingCastChargeRatio,
        [Parameter(Mandatory = $true)] [int] $G6AutoFishingSoakLoops,
        [Parameter(Mandatory = $true)] [string] $G6AutoFishingToggleKey,
        [Parameter(Mandatory = $true)] [bool] $G6AutoFishingMiniGameComplete,
        [Parameter(Mandatory = $true)] [bool] $G6AutoFishingMonoGate,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleCount,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleInitialTitleIdleSeconds,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleIntervalSeconds,
        [Parameter(Mandatory = $true)] [int] $G6SaveLoadCycleInSaveSeconds,
        [Parameter(Mandatory = $true)] [bool] $G6PreLoadGcProbe,
        [Parameter(Mandatory = $true)] [string] $G6SaveLoadObjectSnapshotMode,
        [Parameter(Mandatory = $true)] [string] $G6RootIsolationProfile,
        [Parameter(Mandatory = $true)] [string] $G6OwnerRootIsolationProfile,
        [Parameter(Mandatory = $true)] [string] $G6NativeLoadContinuationProbe,
        [Parameter(Mandatory = $true)] [int] $G6PendingPressureSeconds,
        [Parameter(Mandatory = $true)] [double] $G6PendingPressureIntervalSeconds,
        [Parameter(Mandatory = $true)] [int] $G6LongTitleIdleSeconds
    )

    $projectPath = Join-Path $RepoRoot 'src\DTMAPI.GameBridge.DolocTown.QA\DTMAPI.GameBridge.DolocTown.QA.csproj'
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "QA host project is missing: $projectPath"
    }
    if (-not $SkipQaBuild) {
        $dotnet = Get-DotNetExe -RepoRoot $RepoRoot
        # A PowerShell function returns every uncaptured pipeline object. Capture
        # native build output so callers receive only the stage receipt object.
        $qaBuildOutput = @(& $dotnet build $projectPath -c Release --nologo 2>&1)
        $qaBuildExitCode = $LASTEXITCODE
        foreach ($line in $qaBuildOutput) {
            Write-Host $line
        }
        if ($qaBuildExitCode -ne 0) {
            throw "QA host Release build failed: $projectPath"
        }
    }

    $version = Get-SmokeQaHostVersionAuthority -RepoRoot $RepoRoot
    $sourceDll = Join-Path (Split-Path -Parent $projectPath) 'bin\Release\netstandard2.0\DTMAPI.GameBridge.DolocTown.QA.dll'
    if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) {
        throw "QA host Release DLL is missing. Run without -SkipBuild or build it first: $sourceDll"
    }
    $qaAssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($sourceDll)
    $qaVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($sourceDll)
    if ([string]$qaAssemblyName.Name -ne 'DTMAPI.GameBridge.DolocTown.QA' -or
        [string]$qaAssemblyName.Version -ne [string]$version.AssemblyVersion -or
        [string]$qaVersion.FileVersion -ne [string]$version.BinaryVersion -or
        [string]$qaVersion.ProductVersion -ne [string]$version.ReleaseVersion) {
        throw "QA host metadata mismatch. Expected DTMAPI.GameBridge.DolocTown.QA/$($version.AssemblyVersion)/$($version.BinaryVersion)/$($version.ReleaseVersion); actual $($qaAssemblyName.Name)/$($qaAssemblyName.Version)/$($qaVersion.FileVersion)/$($qaVersion.ProductVersion)."
    }

    $runtimeRoot = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
    $runtimeFiles = @(
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Abstractions.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    )
    $actualRuntimeDlls = if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $runtimeRoot -Recurse -File -Filter '*.dll' | ForEach-Object { $_.Name } | Sort-Object)
    }
    else {
        @()
    }
    if (($actualRuntimeDlls -join '|') -ne (($runtimeFiles | Sort-Object) -join '|')) {
        throw "QA host staging requires the exact five installed production Runtime DLLs. Expected=$($runtimeFiles -join ',') Actual=$($actualRuntimeDlls -join ',')"
    }
    foreach ($runtimeFile in $runtimeFiles) {
        $runtimePath = Join-Path $runtimeRoot $runtimeFile
        $runtimeAssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($runtimePath)
        $runtimeVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($runtimePath)
        if ([string]$runtimeAssemblyName.Name -ne [System.IO.Path]::GetFileNameWithoutExtension($runtimeFile) -or
            [string]$runtimeAssemblyName.Version -ne [string]$version.AssemblyVersion -or
            [string]$runtimeVersion.FileVersion -ne [string]$version.BinaryVersion -or
            [string]$runtimeVersion.ProductVersion -ne [string]$version.ReleaseVersion) {
            throw "QA host staging found incompatible installed Runtime metadata: $runtimePath"
        }
        $runtimeAssembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($runtimePath))
        if (@($runtimeAssembly.GetReferencedAssemblies() | Where-Object { $_.Name -eq 'DTMAPI.GameBridge.DolocTown.QA' }).Count -ne 0) {
            throw "Production Runtime assembly contains a forbidden QA AssemblyRef: $runtimePath"
        }
    }

    $runId = [Guid]::NewGuid().ToString('N')
    $qaRoot = Join-Path $StateDir 'qa-host'
    $runDir = Join-Path $qaRoot $runId
    $activationPath = Join-Path $StateDir 'qa-host-activation.json'
    $settingsPath = Join-Path $runDir 'qa-settings.json'
    $stagedDll = Join-Path $runDir 'DTMAPI.GameBridge.DolocTown.QA.dll'
    $expectedQaEvidencePaths = @(Get-SmokeQaG4ExpectedEvidencePaths `
        -TitleSettingsUiEnabled $TitleSettingsUiEnabled `
        -ManagerStatusUiEnabled $ManagerStatusUiEnabled `
        -ManagerMvpUiEnabled $ManagerMvpUiEnabled `
        -OfficialModUiEnabled $OfficialModUiEnabled `
        -PauseMenuLayoutEnabled $PauseMenuLayoutEnabled `
        -DebugConsoleEnabled $DebugConsoleEnabled `
        -SaveSlotsPagingEnabled $SaveSlotsPagingEnabled `
        -MoreSavesPostTitlePanelEnabled ([bool]$SaveSlotsPagingEnabled -and [bool]$AdvancedProductOwnerDeactivationEnabled -and @($AdvancedProductOwnerDeactivationOwnerIds | Where-Object { $_ -eq 'DTMAPI.MoreSavesMod' }).Count -gt 0) `
        -AnimalObservationEnabled $AnimalObservationEnabled `
        -EquipmentSlotsObservationEnabled $EquipmentSlotsObservationEnabled `
        -MoreEquipmentSlots100UiAcceptanceEnabled $MoreEquipmentSlots100UiAcceptanceEnabled `
        -CameraPlayableEnabled $CameraPlayableEnabled)
    if (Test-Path -LiteralPath $activationPath) {
        throw "QA host activation already exists; refusing to overwrite unknown state: $activationPath"
    }
    if (Test-Path -LiteralPath $qaRoot) {
        throw "QA host staging root already exists; recover or classify it before another run: $qaRoot"
    }

    try {
        New-Item -ItemType Directory -Path $runDir -Force | Out-Null
        Copy-Item -LiteralPath $sourceDll -Destination $stagedDll
        Write-SmokeJsonObject -Path $settingsPath -Value ([ordered]@{
            schemaVersion = 1
            protocolVersion = 7
            runId = $runId
            mode = 'participant-only'
            SaveTestMode = $SaveTestMode
            DisposableSaveFixtureSaveRoot = $DisposableSaveFixtureSaveRoot
            RequireDisposableSaveRedirect = $RequireDisposableSaveRedirect
            AutoFishingPerformanceEnabled = $AutoFishingPerformanceEnabled
            AutoFishingPerformanceProfile = $AutoFishingPerformanceProfile
            AutoFishingPerformanceTargetFish = $AutoFishingPerformanceTargetFish
            AutoFishingPerformanceWarmupFish = $AutoFishingPerformanceWarmupFish
            AutoFishingPerformanceZeroWarmupSeconds = $AutoFishingPerformanceZeroWarmupSeconds
            AutoFishingPerformanceZeroMeasureSeconds = $AutoFishingPerformanceZeroMeasureSeconds
            AutoFishingPerformanceWarmupFrames = $AutoFishingPerformanceWarmupFrames
            AutoFishingPerformanceTargetFrames = $AutoFishingPerformanceTargetFrames
            Batch5NoDemandEnabled = $AutoFishingPerformanceEnabled -and
                $AutoFishingPerformanceProfile -ceq 'InactiveNoConsumer' -and
                $AutoFishingPerformanceTargetFrames -gt 0
            Batch5NoDemandWarmupFrames = $AutoFishingPerformanceWarmupFrames
            Batch5NoDemandTargetFrames = $AutoFishingPerformanceTargetFrames
            Batch5GcLadderEnabled = $Batch5GcLadderEnabled
            Batch5GcLadderDomain = $Batch5GcLadderDomain
            Batch5GcLadderLevel = $Batch5GcLadderLevel
            Batch5GcLadderWorkload = $Batch5GcLadderWorkload
            Batch5GcLadderMultiplier = $Batch5GcLadderMultiplier
            Batch5GcLadderMeasureSeconds = $Batch5GcLadderMeasureSeconds
            Batch5GcLadderSampleSeconds = $Batch5GcLadderSampleSeconds
            Batch5GcLadderTargetUnits = $Batch5GcLadderTargetUnits
            Batch6AutoFishingPilot = [ordered]@{
                Enabled = $Batch6AutoFishingEnabled
                Level = $Batch6AutoFishingLevel
                Scenario = $Batch6AutoFishingScenario
                MeasureSeconds = $Batch6AutoFishingMeasureSeconds
                SampleSeconds = $Batch6AutoFishingSampleSeconds
                WarmupFish = $Batch6AutoFishingWarmupFish
                TargetFish = $Batch6AutoFishingTargetFish
                Formal = [bool]$Batch6AutoFishingFormal
                FormalContract = $Batch6AutoFishingFormalContract
                Multiplier = $Batch6AutoFishingMultiplier
                CastChargeRatio = $Batch6AutoFishingCastChargeRatio
                ManualMovementCancel = $Batch6AutoFishingManualMovementCancel
                ToggleKey = $G6AutoFishingToggleKey
                ExpectedPackageSha256 = $Batch6AutoFishingExpectedPackageSha256
                ExpectedEntryDllSha256 = $Batch6AutoFishingExpectedEntrySha256
                ExpectedManifestSha256 = $Batch6AutoFishingExpectedManifestSha256
                ExpectedReferencePolicySha256 = $Batch6AutoFishingExpectedPolicySha256
            }
            Batch6AutoFishingManagerLifecycle = [ordered]@{
                Enabled = $Batch6AutoFishingManagerEnabled
                Mode = $Batch6AutoFishingManagerMode
                ExpectedProductRoot = $Batch6AutoFishingManagerProductRoot
                ExpectedDisabledMarkerSha256 = $Batch6AutoFishingManagerMarkerSha256
                ExpectedPackageSha256 = $Batch6AutoFishingManagerExpectedPackageSha256
                ExpectedEntryDllSha256 = $Batch6AutoFishingManagerExpectedEntrySha256
                ExpectedManifestSha256 = $Batch6AutoFishingManagerExpectedManifestSha256
                ExpectedReferencePolicySha256 = $Batch6AutoFishingManagerExpectedPolicySha256
            }
            CustomEntityContractEnabled = $CustomEntityContractEnabled
            FishRoeTooltipObservationEnabled = $FishRoeTooltipObservationEnabled
            DiagnosticsSnapshotEnabled = $DiagnosticsSnapshotEnabled
            DiagnosticsScenario = $DiagnosticsScenario
            DiagnosticsExpectedFeatureIds = @($DiagnosticsExpectedFeatureIds)
            ObserveSaveLoaded = $ObserveSaveLoaded
            WaitForManualExit = $WaitForManualExit
            ObserveSaveSaved = $ObserveSaveSaved
            ObserveWorkshopReloadCompleted = $ObserveWorkshopReloadCompleted
            TitleSettingsUiEnabled = $TitleSettingsUiEnabled
            ManagerStatusUiEnabled = $ManagerStatusUiEnabled
            ManagerMvpUiEnabled = $ManagerMvpUiEnabled
            OfficialModUiEnabled = $OfficialModUiEnabled
            PauseMenuLayoutEnabled = $PauseMenuLayoutEnabled
            DebugConsoleEnabled = $DebugConsoleEnabled
            SaveSlotsPagingEnabled = $SaveSlotsPagingEnabled
            SaveSlot = $SaveSlot
            AnimalObservationEnabled = $AnimalObservationEnabled
            EquipmentSlotsObservationEnabled = $EquipmentSlotsObservationEnabled
            AudioObservationEnabled = $AudioObservationEnabled
            HatchVoiceEnabled = $HatchVoiceEnabled
            CameraPlayableEnabled = $CameraPlayableEnabled
            ContentMetadataObservationEnabled = $ContentMetadataObservationEnabled
            ContentMetadataExpectOilAbsent = $ContentMetadataExpectOilAbsent
            ContinuousHomePageTerminalEnabled = $ContinuousHomePageTerminalEnabled
            ContinuousHomePageRequiresSaveLoaded = $ContinuousHomePageRequiresSaveLoaded
            ContinuousHomePageSeconds = $ContinuousHomePageSeconds
            TitleLifecycleEnabled = $TitleLifecycleEnabled
            ProductOwnerRefreshEnabled = $ProductOwnerRefreshEnabled
            AdvancedProductOwnerDeactivationEnabled = $AdvancedProductOwnerDeactivationEnabled
            AdvancedProductOwnerDeactivationOwnerIds = @($AdvancedProductOwnerDeactivationOwnerIds)
            ExternalPlayerInputObservationEnabled = $ExternalPlayerInputObservationEnabled
            ExternalPlayerInputToken = $ExternalPlayerInputToken
            ExternalPlayerInputMarkerPath = $ExternalPlayerInputMarkerPath
            ExternalPlayerInputEvidenceRoot = $ExternalPlayerInputEvidenceRoot
            G5WorldMutationCases = @($G5WorldMutationCases)
            DebugConsoleSaveAcceptancePhase =
                $DebugConsoleSaveAcceptancePhase
            ExpectedDebugConsoleMoney =
                $ExpectedDebugConsoleMoney
            ExpectedMoreEquipmentSlotsBackpackBaseline =
                $ExpectedMoreEquipmentSlotsBackpackBaseline
            ExpectedMoreEquipmentSlotsCommittedGeneration =
                $ExpectedMoreEquipmentSlotsCommittedGeneration
            ExpectedMoreEquipmentSlotsCommittedOccupied =
                $ExpectedMoreEquipmentSlotsCommittedOccupied
            ExpectedMoreEquipmentSlotsCommittedSlots =
                $ExpectedMoreEquipmentSlotsCommittedSlots
            ExpectedMoreEquipmentSlotsCandidatePreGeneration =
                $ExpectedMoreEquipmentSlotsCandidatePreGeneration
            MoreEquipmentSlotsInterruptedCandidateObservationEnabled =
                $MoreEquipmentSlotsInterruptedCandidateObservationEnabled
            MoreEquipmentSlotsTransitionPhase =
                $MoreEquipmentSlotsTransitionPhase
            G6LifecycleCases = @($G6LifecycleCases)
            G6InitialDelaySeconds = $G6InitialDelaySeconds
            G6AutoFishingScenario = $G6AutoFishingScenario
            G6AutoFishingCastChargeRatio = $G6AutoFishingCastChargeRatio
            G6AutoFishingSoakLoops = $G6AutoFishingSoakLoops
            G6AutoFishingToggleKey = $G6AutoFishingToggleKey
            G6AutoFishingMiniGameComplete = $G6AutoFishingMiniGameComplete
            G6AutoFishingMonoGate = $G6AutoFishingMonoGate
            G6SaveLoadCycleCount = $G6SaveLoadCycleCount
            G6SaveLoadCycleInitialTitleIdleSeconds = $G6SaveLoadCycleInitialTitleIdleSeconds
            G6SaveLoadCycleIntervalSeconds = $G6SaveLoadCycleIntervalSeconds
            G6SaveLoadCycleInSaveSeconds = $G6SaveLoadCycleInSaveSeconds
            G6PreLoadGcProbe = $G6PreLoadGcProbe
            G6SaveLoadObjectSnapshotMode = $G6SaveLoadObjectSnapshotMode
            G6RootIsolationProfile = $G6RootIsolationProfile
            G6OwnerRootIsolationProfile = $G6OwnerRootIsolationProfile
            G6NativeLoadContinuationProbe = $G6NativeLoadContinuationProbe
            G6PendingPressureSeconds = $G6PendingPressureSeconds
            G6PendingPressureIntervalSeconds = $G6PendingPressureIntervalSeconds
            G6LongTitleIdleSeconds = $G6LongTitleIdleSeconds
        })
        $dllReceipt = Get-SmokeQaHostFileReceipt -Kind 'QaAssembly' -Path $stagedDll
        $settingsReceipt = Get-SmokeQaHostFileReceipt -Kind 'QaSettings' -Path $settingsPath
        $activation = [ordered]@{
            schemaVersion = 1
            protocolVersion = 7
            runId = $runId
            runtimeReleaseVersion = [string]$version.ReleaseVersion
            runtimeBinaryVersion = [string]$version.BinaryVersion
            runtimeAssemblyVersion = [string]$version.AssemblyVersion
            assemblyName = [string]$qaAssemblyName.Name
            factoryTypeName = 'DTMAPI.GameBridge.DolocTown.QA.QaHostFactory'
            dllLength = [int64]$dllReceipt.Length
            dllSha256 = [string]$dllReceipt.Sha256
            dllAssemblyVersion = [string]$qaAssemblyName.Version
            dllFileVersion = [string]$qaVersion.FileVersion
            dllProductVersion = [string]$qaVersion.ProductVersion
            settingsLength = [int64]$settingsReceipt.Length
            settingsSha256 = [string]$settingsReceipt.Sha256
        }
        # The fixed activation receipt is the transaction commit point and must be written last.
        Write-SmokeJsonObject -Path $activationPath -Value $activation
        $activationReceipt = Get-SmokeQaHostFileReceipt -Kind 'Activation' -Path $activationPath
        $stage = [pscustomobject]@{
            RunId = $runId
            QaRoot = [System.IO.Path]::GetFullPath($qaRoot)
            RunDir = [System.IO.Path]::GetFullPath($runDir)
            ActivationPath = [System.IO.Path]::GetFullPath($activationPath)
            Artifacts = @($activationReceipt, $dllReceipt, $settingsReceipt)
            ExpectedQaEvidencePaths = @($expectedQaEvidencePaths)
            Activation = $activation
        }
        Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'qa-host-stage.json') -Value ([ordered]@{
            StagedAt = (Get-Date).ToUniversalTime().ToString('o')
            RunId = $stage.RunId
            QaRoot = $stage.QaRoot
            RunDir = $stage.RunDir
            ActivationPath = $stage.ActivationPath
            Artifacts = @($stage.Artifacts)
            ExpectedQaEvidencePaths = @($stage.ExpectedQaEvidencePaths)
            Activation = $stage.Activation
            ActivationWrittenLast = $true
        })
        return $stage
    }
    catch {
        foreach ($path in @($activationPath, $settingsPath, $stagedDll)) {
            if (Test-Path -LiteralPath $path -PathType Leaf) {
                Remove-Item -LiteralPath $path -Force
            }
        }
        if ((Test-Path -LiteralPath $runDir -PathType Container) -and @(Get-ChildItem -LiteralPath $runDir -Force).Count -eq 0) {
            Remove-Item -LiteralPath $runDir -Force
        }
        if ((Test-Path -LiteralPath $qaRoot -PathType Container) -and @(Get-ChildItem -LiteralPath $qaRoot -Force).Count -eq 0) {
            Remove-Item -LiteralPath $qaRoot -Force
        }
        throw
    }
}

function Get-SmokeQaUInt32BigEndian {
    param(
        [Parameter(Mandatory = $true)] [byte[]] $Bytes,
        [Parameter(Mandatory = $true)] [int] $Offset
    )

    if ($Offset -lt 0 -or $Offset + 4 -gt $Bytes.Length) {
        throw "Big-endian UInt32 read escaped the evidence buffer. offset=$Offset length=$($Bytes.Length)"
    }
    return [uint64]$Bytes[$Offset] * 16777216L +
        [uint64]$Bytes[$Offset + 1] * 65536L +
        [uint64]$Bytes[$Offset + 2] * 256L +
        [uint64]$Bytes[$Offset + 3]
}

function Get-SmokeQaPngCrc32 {
    param(
        [Parameter(Mandatory = $true)] [byte[]] $Bytes,
        [Parameter(Mandatory = $true)] [int] $Offset,
        [Parameter(Mandatory = $true)] [int] $Count
    )

    if ($Offset -lt 0 -or $Count -lt 0 -or $Offset + $Count -gt $Bytes.Length) {
        throw "CRC32 read escaped the evidence buffer. offset=$Offset count=$Count length=$($Bytes.Length)"
    }
    $crcTableVariable = Get-Variable -Scope Script -Name SmokeQaPngCrc32Table -ErrorAction SilentlyContinue
    if ($null -eq $crcTableVariable -or $null -eq $crcTableVariable.Value) {
        $polynomial = [Convert]::ToUInt32('EDB88320', 16)
        [uint32[]]$table = New-Object 'System.UInt32[]' 256
        for ($tableIndex = 0; $tableIndex -lt $table.Length; $tableIndex++) {
            [uint32]$value = [uint32]$tableIndex
            for ($bit = 0; $bit -lt 8; $bit++) {
                if (($value -band [uint32]1) -ne 0) {
                    $value = [uint32](($value -shr 1) -bxor $polynomial)
                }
                else {
                    $value = [uint32]($value -shr 1)
                }
            }
            $table[$tableIndex] = $value
        }
        $script:SmokeQaPngCrc32Table = $table
    }

    [uint32]$crc = [uint32]::MaxValue
    for ($index = 0; $index -lt $Count; $index++) {
        $tableIndex = [int](($crc -bxor [uint32]$Bytes[$Offset + $index]) -band [uint32]255)
        $crc = [uint32](($crc -shr 8) -bxor $script:SmokeQaPngCrc32Table[$tableIndex])
    }
    return [uint32]($crc -bxor [uint32]::MaxValue)
}

function Test-SmokeQaPngEvidence {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    try {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        if ($item.PSIsContainer -or [int64]$item.Length -gt [int]::MaxValue) {
            return [pscustomobject]@{ Valid = $false; Details = 'png-size-out-of-range' }
        }
        [byte[]]$bytes = [System.IO.File]::ReadAllBytes($item.FullName)
        [byte[]]$signature = @(137,80,78,71,13,10,26,10)
        if ($bytes.Length -lt 8) {
            return [pscustomobject]@{ Valid = $false; Details = 'png-signature-truncated' }
        }
        for ($signatureIndex = 0; $signatureIndex -lt $signature.Length; $signatureIndex++) {
            if ($bytes[$signatureIndex] -ne $signature[$signatureIndex]) {
                return [pscustomobject]@{ Valid = $false; Details = 'png-signature-invalid' }
            }
        }

        $offset = 8
        $chunkIndex = 0
        $width = 0L
        $height = 0L
        $idatBytes = 0L
        $seenIhdr = $false
        $seenIend = $false
        while ($offset -lt $bytes.Length) {
            if ($bytes.Length - $offset -lt 12) {
                return [pscustomobject]@{ Valid = $false; Details = "png-chunk-header-truncated;chunk=$chunkIndex" }
            }
            [uint64]$chunkLength = Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset $offset
            if ($chunkLength -gt [int]::MaxValue) {
                return [pscustomobject]@{ Valid = $false; Details = "png-chunk-length-out-of-range;chunk=$chunkIndex" }
            }
            [uint64]$chunkTotal = $chunkLength + 12
            if ([uint64]$offset + $chunkTotal -gt [uint64]$bytes.Length) {
                return [pscustomobject]@{ Valid = $false; Details = "png-chunk-out-of-bounds;chunk=$chunkIndex" }
            }

            $chunkType = [System.Text.Encoding]::ASCII.GetString($bytes, $offset + 4, 4)
            if ($chunkIndex -eq 0 -and -not $chunkType.Equals('IHDR', [System.StringComparison]::Ordinal)) {
                return [pscustomobject]@{ Valid = $false; Details = 'png-first-chunk-not-ihdr' }
            }
            if ($chunkType.Equals('IHDR', [System.StringComparison]::Ordinal)) {
                if ($seenIhdr -or $chunkIndex -ne 0 -or $chunkLength -ne 13) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-ihdr-invalid' }
                }
                $width = [int64](Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset ($offset + 8))
                $height = [int64](Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset ($offset + 12))
                if ($width -le 0 -or $height -le 0) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-dimensions-invalid' }
                }
                $seenIhdr = $true
            }
            elseif (-not $seenIhdr) {
                return [pscustomobject]@{ Valid = $false; Details = 'png-chunk-before-ihdr' }
            }

            $crcOffset = $offset + 8 + [int]$chunkLength
            [uint32]$expectedCrc = [uint32](Get-SmokeQaUInt32BigEndian -Bytes $bytes -Offset $crcOffset)
            [uint32]$actualCrc = Get-SmokeQaPngCrc32 -Bytes $bytes -Offset ($offset + 4) -Count ([int]$chunkLength + 4)
            if ($actualCrc -ne $expectedCrc) {
                return [pscustomobject]@{ Valid = $false; Details = "png-crc-invalid;chunk=$chunkIndex;type=$chunkType" }
            }

            if ($chunkType.Equals('IDAT', [System.StringComparison]::Ordinal) -and $chunkLength -gt 0) {
                $idatBytes += [int64]$chunkLength
            }
            if ($chunkType.Equals('IEND', [System.StringComparison]::Ordinal)) {
                if ($chunkLength -ne 0) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-iend-length-invalid' }
                }
                if ([uint64]$offset + $chunkTotal -ne [uint64]$bytes.Length) {
                    return [pscustomobject]@{ Valid = $false; Details = 'png-trailing-bytes-after-iend' }
                }
                $seenIend = $true
                $offset += [int]$chunkTotal
                $chunkIndex++
                break
            }

            $offset += [int]$chunkTotal
            $chunkIndex++
        }

        if (-not $seenIhdr -or $idatBytes -le 0 -or -not $seenIend -or $offset -ne $bytes.Length) {
            return [pscustomobject]@{ Valid = $false; Details = 'png-required-chunk-or-length-invalid' }
        }
        return [pscustomobject]@{
            Valid = $true
            Details = "png-structure-crc-ok;width=$width;height=$height;chunks=$chunkIndex;idatBytes=$idatBytes"
        }
    }
    catch {
        return [pscustomobject]@{ Valid = $false; Details = 'png-parse-failed:' + [string]$_.Exception.GetType().Name + ':' + [string]$_.Exception.Message }
    }
}

function Test-SmokeQaCameraTelemetryEvidence {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $reader = $null
    try {
        $reader = [System.IO.File]::OpenText($Path)
        $header = [string]$reader.ReadLine()
        $expectedHeader = 'timestamp,marker,agentX,agentY,agentZ,cameraX,cameraY,cameraZ,appliedScale,activeOwner'
        if (-not [string]::Equals($header, $expectedHeader, [System.StringComparison]::Ordinal)) {
            return [pscustomobject]@{ Valid = $false; Details = 'camera-telemetry-header-invalid' }
        }

        $rowCount = 0
        $lineNumber = 1
        while ($null -ne ($line = $reader.ReadLine())) {
            $lineNumber++
            [string[]]$fields = $line.Split([char]',')
            if ($fields.Length -ne 10) {
                return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-field-count-invalid;line=$lineNumber;actual=$($fields.Length)" }
            }
            [DateTimeOffset]$timestamp = [DateTimeOffset]::MinValue
            if (-not [DateTimeOffset]::TryParseExact(
                $fields[0],
                'O',
                [System.Globalization.CultureInfo]::InvariantCulture,
                [System.Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$timestamp)) {
                return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-timestamp-invalid;line=$lineNumber" }
            }
            if ([string]::IsNullOrWhiteSpace($fields[1]) -or [string]::IsNullOrWhiteSpace($fields[9])) {
                return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-marker-or-owner-empty;line=$lineNumber" }
            }
            foreach ($fieldIndex in 2..8) {
                [double]$number = 0
                if (-not [double]::TryParse(
                    $fields[$fieldIndex],
                    [System.Globalization.NumberStyles]::Float,
                    [System.Globalization.CultureInfo]::InvariantCulture,
                    [ref]$number) -or [double]::IsNaN($number) -or [double]::IsInfinity($number)) {
                    return [pscustomobject]@{ Valid = $false; Details = "camera-telemetry-number-invalid;line=$lineNumber;field=$fieldIndex" }
                }
            }
            $rowCount++
        }
        if ($rowCount -lt 1) {
            return [pscustomobject]@{ Valid = $false; Details = 'camera-telemetry-no-data-rows' }
        }
        return [pscustomobject]@{ Valid = $true; Details = "camera-telemetry-rows-ok;rows=$rowCount" }
    }
    catch {
        return [pscustomobject]@{ Valid = $false; Details = 'camera-telemetry-parse-failed:' + [string]$_.Exception.GetType().Name + ':' + [string]$_.Exception.Message }
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
    }
}

function Remove-SmokeQaHostStage {
    param(
        [Parameter(Mandatory = $true)] $Stage,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $artifactStates = New-Object 'System.Collections.Generic.List[object]'
    $allArtifactsExact = $true
    foreach ($artifact in @($Stage.Artifacts)) {
        $exists = Test-Path -LiteralPath $artifact.Path -PathType Leaf
        $actualLength = if ($exists) { [int64](Get-Item -LiteralPath $artifact.Path).Length } else { [int64]0 }
        $actualHash = if ($exists) { Get-SmokeFileSha256 -Path $artifact.Path } else { '' }
        $exact = $exists -and $actualLength -eq [int64]$artifact.Length -and
            [string]::Equals($actualHash, [string]$artifact.Sha256, [System.StringComparison]::Ordinal)
        $allArtifactsExact = $allArtifactsExact -and $exact
        $artifactStates.Add([ordered]@{
            Kind = [string]$artifact.Kind
            Path = [string]$artifact.Path
            Exists = $exists
            ExpectedLength = [int64]$artifact.Length
            ActualLength = $actualLength
            ExpectedSha256 = [string]$artifact.Sha256
            ActualSha256 = $actualHash
            Exact = $exact
        }) | Out-Null
    }

    # QA evidence is an owned, bounded extension of the staged run tree. Copy
    # only the reviewed receipt names into the durable smoke evidence tree,
    # verify the copied bytes, then remove those exact source files so the
    # original activation/artifact transaction can use its strict cleanup.
    $qaEvidenceSourceRoot = Join-Path $Stage.RunDir 'qa-host\g4'
    $qaEvidenceArchiveRoot = Join-Path $EvidencePath 'qa-host\g4'
    $qaEvidenceStates = New-Object 'System.Collections.Generic.List[object]'
    $reviewedQaEvidenceFiles = @(
        'ui\title-settings.png',
        'ui\manager-status-page.png',
        'ui\manager-mods-page.png',
        'ui\manager-advanced-page.png',
        'ui\manager-logs-page.png',
        'ui\official-mod-ui.png',
        'ui\pause-menu-layout.png',
        'ui\official-save-ui.png',
        'ui\official-save-ui-post-title.png',
        'ui\animal-panel.png',
        'ui\equipment-slots.png',
        'ui\equipment-slots-1920x1080-dynamic-row.png',
        'ui\equipment-slots-1024x768-dynamic-row.png',
        'ui\equipment-slots-1024x768-reopen.png',
        'ui\equipment-slots-summary.txt',
        'ui\debug-console.png',
        'camera-playable\before.png',
        'camera-playable\scale-4x.png',
        'camera-playable\movement-start.png',
        'camera-playable\movement-mid.png',
        'camera-playable\movement-end.png',
        'camera-playable\fallback-2x.png',
        'camera-playable\reset.png',
        'camera-playable\telemetry.csv'
    )
    $expectedQaEvidenceFiles = @($Stage.ExpectedQaEvidencePaths | ForEach-Object {
        ([string]$_).Replace('/', '\').TrimStart('\')
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    $expectedQaEvidenceDirectories = @($expectedQaEvidenceFiles | ForEach-Object {
        Split-Path -Parent $_
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    $unreviewedExpectedQaEvidence = @($expectedQaEvidenceFiles | Where-Object { $reviewedQaEvidenceFiles -notcontains $_ })
    $sourceFiles = @()
    $sourceDirectories = @()
    $actualQaEvidenceFiles = @()
    $actualQaEvidenceDirectories = @()
    if (Test-Path -LiteralPath $qaEvidenceSourceRoot -PathType Container) {
        $sourceRootWithSeparator = [System.IO.Path]::GetFullPath($qaEvidenceSourceRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
        $sourceFiles = @(Get-ChildItem -LiteralPath $qaEvidenceSourceRoot -Recurse -Force -File)
        $sourceDirectories = @(Get-ChildItem -LiteralPath $qaEvidenceSourceRoot -Recurse -Force -Directory)
        $actualQaEvidenceFiles = @($sourceFiles | ForEach-Object {
            [System.IO.Path]::GetFullPath($_.FullName).Substring($sourceRootWithSeparator.Length).Replace('/', '\')
        } | Sort-Object -Unique)
        $actualQaEvidenceDirectories = @($sourceDirectories | ForEach-Object {
            [System.IO.Path]::GetFullPath($_.FullName).Substring($sourceRootWithSeparator.Length).Replace('/', '\')
        } | Sort-Object -Unique)
        foreach ($file in $sourceFiles) {
            $relativePath = [System.IO.Path]::GetFullPath($file.FullName).Substring($sourceRootWithSeparator.Length).Replace('/', '\')
            $sourceLength = [int64]$file.Length
            $sourceHash = if ($sourceLength -gt 0) { Get-SmokeFileSha256 -Path $file.FullName } else { '' }
            $contentValid = $sourceLength -gt 0
            $contentValidation = if ($contentValid) { 'non-empty' } else { 'empty' }
            if ($contentValid -and $relativePath.EndsWith('.png', [System.StringComparison]::OrdinalIgnoreCase)) {
                $pngValidation = Test-SmokeQaPngEvidence -Path $file.FullName
                $contentValid = [bool]$pngValidation.Valid
                $contentValidation = [string]$pngValidation.Details
            }
            elseif ($contentValid -and $relativePath.Equals('camera-playable\telemetry.csv', [System.StringComparison]::OrdinalIgnoreCase)) {
                $telemetryValidation = Test-SmokeQaCameraTelemetryEvidence -Path $file.FullName
                $contentValid = [bool]$telemetryValidation.Valid
                $contentValidation = [string]$telemetryValidation.Details
            }
            $qaEvidenceStates.Add([ordered]@{
                RelativePath = $relativePath
                SourcePath = [System.IO.Path]::GetFullPath($file.FullName)
                Length = $sourceLength
                Sha256 = $sourceHash
                Expected = $expectedQaEvidenceFiles -contains $relativePath
                Reviewed = $reviewedQaEvidenceFiles -contains $relativePath
                ContentValid = $contentValid
                ContentValidation = $contentValidation
            }) | Out-Null
        }
    }

    $missingQaEvidence = @($expectedQaEvidenceFiles | Where-Object { $actualQaEvidenceFiles -notcontains $_ })
    $unexpectedQaEvidence = @($actualQaEvidenceFiles | Where-Object { $expectedQaEvidenceFiles -notcontains $_ })
    $missingQaEvidenceDirectories = @($expectedQaEvidenceDirectories | Where-Object { $actualQaEvidenceDirectories -notcontains $_ })
    $unexpectedQaEvidenceDirectories = @($actualQaEvidenceDirectories | Where-Object { $expectedQaEvidenceDirectories -notcontains $_ })
    $damagedQaEvidence = @($qaEvidenceStates.ToArray() | Where-Object {
        [int64]$_.Length -le 0 -or [string]::IsNullOrWhiteSpace([string]$_.Sha256) -or -not [bool]$_.ContentValid
    } | ForEach-Object { [string]$_.RelativePath })
    $qaEvidenceExact = $unreviewedExpectedQaEvidence.Count -eq 0 -and
        $missingQaEvidence.Count -eq 0 -and $unexpectedQaEvidence.Count -eq 0 -and
        $missingQaEvidenceDirectories.Count -eq 0 -and $unexpectedQaEvidenceDirectories.Count -eq 0 -and
        $damagedQaEvidence.Count -eq 0

    # Prove the complete stage membership before archiving or removing any G4
    # evidence. Otherwise an artifact mismatch or an unknown sibling outside
    # qa-host\g4 could leave a retained stage whose evidence had already moved.
    $expectedRunFiles = @($Stage.Artifacts | Where-Object { [string]$_.Kind -ne 'Activation' } | ForEach-Object { [System.IO.Path]::GetFullPath([string]$_.Path) } | Sort-Object)
    $qaEvidenceParent = Split-Path -Parent $qaEvidenceSourceRoot
    $qaEvidenceSourceExists = Test-Path -LiteralPath $qaEvidenceSourceRoot -PathType Container
    $expectedRunEntriesBeforeArchive = @($expectedRunFiles)
    if ($qaEvidenceSourceExists) {
        $expectedRunEntriesBeforeArchive += [System.IO.Path]::GetFullPath($qaEvidenceParent)
    }
    $expectedRunEntriesBeforeArchive = @($expectedRunEntriesBeforeArchive | Sort-Object)
    $actualRunEntriesBeforeArchive = if (Test-Path -LiteralPath $Stage.RunDir -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.RunDir -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $expectedQaEvidenceParentEntriesBeforeArchive = if ($qaEvidenceSourceExists) {
        @([System.IO.Path]::GetFullPath($qaEvidenceSourceRoot))
    }
    else {
        @()
    }
    $actualQaEvidenceParentEntriesBeforeArchive = if (Test-Path -LiteralPath $qaEvidenceParent -PathType Container) {
        @(Get-ChildItem -LiteralPath $qaEvidenceParent -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $expectedRootEntries = @([System.IO.Path]::GetFullPath([string]$Stage.RunDir))
    $actualRootEntriesBeforeArchive = if (Test-Path -LiteralPath $Stage.QaRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.QaRoot -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $treeExactBeforeArchive = ($actualRunEntriesBeforeArchive -join '|') -eq ($expectedRunEntriesBeforeArchive -join '|') -and
        ($actualQaEvidenceParentEntriesBeforeArchive -join '|') -eq ($expectedQaEvidenceParentEntriesBeforeArchive -join '|') -and
        ($actualRootEntriesBeforeArchive -join '|') -eq ($expectedRootEntries -join '|')

    if ($allArtifactsExact -and $qaEvidenceExact -and $treeExactBeforeArchive -and $qaEvidenceSourceExists) {
        try {
            foreach ($receipt in @($qaEvidenceStates.ToArray())) {
                $destination = Join-Path $qaEvidenceArchiveRoot ([string]$receipt.RelativePath)
                $destinationDirectory = Split-Path -Parent $destination
                New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
                if (Test-Path -LiteralPath $destination -PathType Leaf) {
                    $existingLength = [int64](Get-Item -LiteralPath $destination).Length
                    $existingHash = Get-SmokeFileSha256 -Path $destination
                    if ($existingLength -ne [int64]$receipt.Length -or
                        -not [string]::Equals($existingHash, [string]$receipt.Sha256, [System.StringComparison]::Ordinal)) {
                        throw "QA evidence archive already contains different bytes: $destination"
                    }
                }
                else {
                    Copy-Item -LiteralPath $receipt.SourcePath -Destination $destination
                }
                $copiedLength = [int64](Get-Item -LiteralPath $destination).Length
                $copiedHash = Get-SmokeFileSha256 -Path $destination
                if ($copiedLength -ne [int64]$receipt.Length -or
                    -not [string]::Equals($copiedHash, [string]$receipt.Sha256, [System.StringComparison]::Ordinal)) {
                    throw "QA evidence archive verification failed: $destination"
                }
            }
            foreach ($receipt in @($qaEvidenceStates.ToArray())) {
                Remove-Item -LiteralPath $receipt.SourcePath -Force
            }
            foreach ($directory in @($sourceDirectories | Sort-Object { $_.FullName.Length } -Descending)) {
                if ((Test-Path -LiteralPath $directory.FullName -PathType Container) -and @(Get-ChildItem -LiteralPath $directory.FullName -Force).Count -eq 0) {
                    Remove-Item -LiteralPath $directory.FullName -Force
                }
            }
            if (@(Get-ChildItem -LiteralPath $qaEvidenceSourceRoot -Force).Count -eq 0) {
                Remove-Item -LiteralPath $qaEvidenceSourceRoot -Force
            }
            if ((Test-Path -LiteralPath $qaEvidenceParent -PathType Container) -and @(Get-ChildItem -LiteralPath $qaEvidenceParent -Force).Count -eq 0) {
                Remove-Item -LiteralPath $qaEvidenceParent -Force
            }
        }
        catch {
            $qaEvidenceExact = $false
            $qaEvidenceStates.Add([ordered]@{ Error = [string]$_.Exception.Message }) | Out-Null
        }
    }

    $actualRunEntries = if (Test-Path -LiteralPath $Stage.RunDir -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.RunDir -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $actualRootEntries = if (Test-Path -LiteralPath $Stage.QaRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $Stage.QaRoot -Force | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else {
        @()
    }
    $treeExactAfterArchive = ($actualRunEntries -join '|') -eq ($expectedRunFiles -join '|') -and
        ($actualRootEntries -join '|') -eq ($expectedRootEntries -join '|')
    $treeExact = $treeExactBeforeArchive -and $treeExactAfterArchive
    $cleaned = $false
    $reason = ''
    if ($allArtifactsExact -and $qaEvidenceExact -and $treeExact) {
        try {
            foreach ($artifact in @($Stage.Artifacts)) {
                Remove-Item -LiteralPath $artifact.Path -Force
            }
            if (@(Get-ChildItem -LiteralPath $Stage.RunDir -Force).Count -eq 0) {
                Remove-Item -LiteralPath $Stage.RunDir -Force
            }
            if (@(Get-ChildItem -LiteralPath $Stage.QaRoot -Force).Count -eq 0) {
                Remove-Item -LiteralPath $Stage.QaRoot -Force
            }
            $cleaned = -not (Test-Path -LiteralPath $Stage.ActivationPath) -and
                -not (Test-Path -LiteralPath $Stage.RunDir) -and
                -not (Test-Path -LiteralPath $Stage.QaRoot)
            if (-not $cleaned) {
                $reason = 'Known artifacts were removed but one or more owned paths remained.'
            }
        }
        catch {
            $reason = 'Exact-artifact cleanup failed: ' + [string]$_.Exception.Message
        }
    }
    else {
        $reason = 'Staged bytes, bounded QA evidence, or directory membership changed; the pre-archive gate retained the complete stage and G4 evidence.'
    }

    $result = [pscustomobject]@{
        RunId = [string]$Stage.RunId
        ExactArtifacts = $allArtifactsExact
        ExactQaEvidence = $qaEvidenceExact
        ExactTree = $treeExact
        Cleaned = $cleaned
        Retained = -not $cleaned
        Reason = $reason
        Artifacts = @($artifactStates.ToArray())
        QaEvidenceArchiveRoot = [System.IO.Path]::GetFullPath($qaEvidenceArchiveRoot)
        ExpectedQaEvidencePaths = @($expectedQaEvidenceFiles)
        ActualQaEvidencePaths = @($actualQaEvidenceFiles)
        MissingQaEvidencePaths = @($missingQaEvidence)
        UnexpectedQaEvidencePaths = @($unexpectedQaEvidence)
        DamagedQaEvidencePaths = @($damagedQaEvidence)
        QaEvidence = @($qaEvidenceStates.ToArray())
        TreeExactBeforeArchive = $treeExactBeforeArchive
        ActualRunEntriesBeforeArchive = @($actualRunEntriesBeforeArchive)
        ActualQaEvidenceParentEntriesBeforeArchive = @($actualQaEvidenceParentEntriesBeforeArchive)
        ActualRootEntriesBeforeArchive = @($actualRootEntriesBeforeArchive)
        ActualRunEntries = @($actualRunEntries)
        ActualRootEntries = @($actualRootEntries)
    }
    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'qa-host-cleanup.json') -Value $result
    if (-not $cleaned) {
        $manualPath = Join-Path $EvidencePath 'qa-host-manual-recovery-required.json'
        Write-SmokeJsonObject -Path $manualPath -Value ([ordered]@{
            CreatedAt = (Get-Date).ToUniversalTime().ToString('o')
            Reason = $reason
            Rule = 'After proving DolocTown.exe is absent, remove only files whose length and SHA-256 still match the recorded stage receipt; remove directories only when empty.'
            Cleanup = $result
        })
        @(
            'DTMAPI QA host cleanup was intentionally incomplete.',
            'Do not load, move, delete, or overwrite changed or unknown files.',
            "Use the exact length/SHA-256 receipt in: $manualPath"
        ) | Set-Content -LiteralPath (Join-Path $EvidencePath 'QA-HOST-MANUAL-RECOVERY-REQUIRED.txt')
    }
    return $result
}

function Invoke-SmokeQaG4EvidenceCleanupSelfTest {
    $testRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) ('dtmapi-qa-g4-cleanup-' + [Guid]::NewGuid().ToString('N'))))
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $testRoot.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "QA evidence cleanup self-test escaped the system temp root: $testRoot"
    }

    $caseResults = New-Object 'System.Collections.Generic.List[object]'
    try {
        [byte[]]$validPngBytes = [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=')
        $telemetryHeader = 'timestamp,marker,agentX,agentY,agentZ,cameraX,cameraY,cameraZ,appliedScale,activeOwner'
        $validTelemetryRow = '2026-07-16T00:00:00.0000000+00:00,movement,1,2,3,4,5,6,4,qa.camera.owner'
        foreach ($case in @(
            [pscustomobject]@{
                Name = 'exact'
                Expected = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Files = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Garbage = @()
                ExpectCleaned = $true
            },
            [pscustomobject]@{
                Name = 'g9-equipment-slots'
                Expected = @('ui\equipment-slots.png','ui\equipment-slots-summary.txt')
                Files = @('ui\equipment-slots.png','ui\equipment-slots-summary.txt')
                Garbage = @()
                ExpectCleaned = $true
            },
            [pscustomobject]@{ Name = 'missing'; Expected = @('ui\title-settings.png'); Files = @(); Garbage = @(); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'extra-allowlisted'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png','ui\official-mod-ui.png'); Garbage = @(); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'unknown'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png','ui\unknown.png'); Garbage = @(); ExpectCleaned = $false },
            [pscustomobject]@{
                Name = 'damaged'
                Expected = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Files = @('ui\title-settings.png','camera-playable\telemetry.csv')
                Garbage = @('ui\title-settings.png','camera-playable\telemetry.csv')
                ExpectCleaned = $false
            },
            [pscustomobject]@{ Name = 'truncated-png'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); TruncatedPng = @('ui\title-settings.png'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'bad-crc-png'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); BadCrcPng = @('ui\title-settings.png'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'header-only-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); HeaderOnlyCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'bad-timestamp-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); BadTimestampCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'bad-number-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); BadNumberCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'empty-marker-owner-csv'; Expected = @('camera-playable\telemetry.csv'); Files = @('camera-playable\telemetry.csv'); EmptyMarkerOwnerCsv = @('camera-playable\telemetry.csv'); ExpectCleaned = $false },
            [pscustomobject]@{ Name = 'artifact-mismatch-retains-evidence'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); TamperArtifact = $true; ExpectCleaned = $false; ExpectExactQaEvidence = $true },
            [pscustomobject]@{ Name = 'outside-g4-unknown-retains-evidence'; Expected = @('ui\title-settings.png'); Files = @('ui\title-settings.png'); AddOutsideG4Unknown = $true; ExpectCleaned = $false; ExpectExactQaEvidence = $true }
        )) {
            $caseRoot = Join-Path $testRoot ([string]$case.Name)
            $stateDir = Join-Path $caseRoot 'state'
            $qaRoot = Join-Path $stateDir 'qa-host'
            $runId = ([string]$case.Name).Replace('-', '') + '0123456789abcdef0123456789abcdef'
            $runId = $runId.Substring(0, 32)
            $runDir = Join-Path $qaRoot $runId
            $evidencePath = Join-Path $caseRoot 'evidence'
            $activationPath = Join-Path $stateDir 'qa-host-activation.json'
            $dllPath = Join-Path $runDir 'DTMAPI.GameBridge.DolocTown.QA.dll'
            $settingsPath = Join-Path $runDir 'qa-settings.json'
            New-Item -ItemType Directory -Force -Path $runDir, $evidencePath | Out-Null
            [System.IO.File]::WriteAllText($activationPath, 'activation-receipt')
            [System.IO.File]::WriteAllText($dllPath, 'qa-assembly')
            [System.IO.File]::WriteAllText($settingsPath, 'qa-settings')

            $garbagePaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'Garbage' } | ForEach-Object { @($_.Value) })
            $truncatedPngPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'TruncatedPng' } | ForEach-Object { @($_.Value) })
            $badCrcPngPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'BadCrcPng' } | ForEach-Object { @($_.Value) })
            $headerOnlyCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'HeaderOnlyCsv' } | ForEach-Object { @($_.Value) })
            $badTimestampCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'BadTimestampCsv' } | ForEach-Object { @($_.Value) })
            $badNumberCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'BadNumberCsv' } | ForEach-Object { @($_.Value) })
            $emptyMarkerOwnerCsvPaths = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'EmptyMarkerOwnerCsv' } | ForEach-Object { @($_.Value) })
            foreach ($relativePath in @($case.Files)) {
                $path = Join-Path (Join-Path $runDir 'qa-host\g4') ([string]$relativePath)
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
                if ($garbagePaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, 'non-empty-damaged-evidence')
                }
                elseif ($truncatedPngPaths -contains [string]$relativePath) {
                    [byte[]]$truncated = New-Object byte[] ($validPngBytes.Length - 4)
                    [Array]::Copy($validPngBytes, $truncated, $truncated.Length)
                    [System.IO.File]::WriteAllBytes($path, $truncated)
                }
                elseif ($badCrcPngPaths -contains [string]$relativePath) {
                    [byte[]]$badCrc = New-Object byte[] $validPngBytes.Length
                    [Array]::Copy($validPngBytes, $badCrc, $badCrc.Length)
                    # Flip the first byte of the non-empty IDAT payload while
                    # retaining its original CRC, so the structural parser must
                    # reject a bounded-but-tampered PNG.
                    $badCrc[41] = [byte]($badCrc[41] -bxor 1)
                    [System.IO.File]::WriteAllBytes($path, $badCrc)
                }
                elseif ($headerOnlyCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n")
                }
                elseif ($badTimestampCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + 'not-a-timestamp,movement,1,2,3,4,5,6,4,qa.camera.owner' + "`n")
                }
                elseif ($badNumberCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + '2026-07-16T00:00:00.0000000+00:00,movement,1,2,not-a-number,4,5,6,4,qa.camera.owner' + "`n")
                }
                elseif ($emptyMarkerOwnerCsvPaths -contains [string]$relativePath) {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + '2026-07-16T00:00:00.0000000+00:00,,1,2,3,4,5,6,4,' + "`n")
                }
                elseif ([string]$relativePath -like '*.png') {
                    [System.IO.File]::WriteAllBytes($path, $validPngBytes)
                }
                elseif ([string]$relativePath -ieq 'camera-playable\telemetry.csv') {
                    [System.IO.File]::WriteAllText($path, $telemetryHeader + "`n" + $validTelemetryRow + "`n")
                }
                else {
                    [System.IO.File]::WriteAllText($path, 'qa-evidence-' + [string]$relativePath)
                }
            }

            $stage = [pscustomobject]@{
                RunId = $runId
                QaRoot = [System.IO.Path]::GetFullPath($qaRoot)
                RunDir = [System.IO.Path]::GetFullPath($runDir)
                ActivationPath = [System.IO.Path]::GetFullPath($activationPath)
                Artifacts = @(
                    (Get-SmokeQaHostFileReceipt -Kind 'Activation' -Path $activationPath),
                    (Get-SmokeQaHostFileReceipt -Kind 'QaAssembly' -Path $dllPath),
                    (Get-SmokeQaHostFileReceipt -Kind 'QaSettings' -Path $settingsPath)
                )
                ExpectedQaEvidencePaths = @($case.Expected)
            }
            if (@($case.PSObject.Properties | Where-Object { $_.Name -eq 'TamperArtifact' -and [bool]$_.Value }).Count -eq 1) {
                [System.IO.File]::AppendAllText($dllPath, '-changed-after-receipt')
            }
            if (@($case.PSObject.Properties | Where-Object { $_.Name -eq 'AddOutsideG4Unknown' -and [bool]$_.Value }).Count -eq 1) {
                $outsideG4Path = Join-Path $runDir 'qa-host\outside-g4-unknown.txt'
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outsideG4Path) | Out-Null
                [System.IO.File]::WriteAllText($outsideG4Path, 'unknown-outside-reviewed-g4-root')
            }
            $cleanup = Remove-SmokeQaHostStage -Stage $stage -EvidencePath $evidencePath
            $expectExactQaEvidenceProperty = @($case.PSObject.Properties | Where-Object { $_.Name -eq 'ExpectExactQaEvidence' } | Select-Object -First 1)
            $expectExactQaEvidence = if ($expectExactQaEvidenceProperty.Count -eq 1) { [bool]$expectExactQaEvidenceProperty[0].Value } else { [bool]$case.ExpectCleaned }
            $sourceEvidenceRoot = Join-Path $runDir 'qa-host\g4'
            $sourceEvidenceRetained = @($case.Files | Where-Object {
                -not (Test-Path -LiteralPath (Join-Path $sourceEvidenceRoot ([string]$_)) -PathType Leaf)
            }).Count -eq 0
            $archiveEvidenceAbsent = @($case.Files | Where-Object {
                Test-Path -LiteralPath (Join-Path $cleanup.QaEvidenceArchiveRoot ([string]$_)) -PathType Leaf
            }).Count -eq 0
            $passed = [bool]$cleanup.Cleaned -eq [bool]$case.ExpectCleaned
            if ($case.ExpectCleaned) {
                $passed = $passed -and [bool]$cleanup.ExactQaEvidence -and
                    @($case.Expected | Where-Object {
                        $archive = Join-Path $cleanup.QaEvidenceArchiveRoot ([string]$_)
                        -not (Test-Path -LiteralPath $archive -PathType Leaf) -or (Get-Item -LiteralPath $archive).Length -le 0
                    }).Count -eq 0
            }
            else {
                $passed = $passed -and [bool]$cleanup.ExactQaEvidence -eq $expectExactQaEvidence -and
                    (Test-Path -LiteralPath $activationPath -PathType Leaf) -and
                    (Test-Path -LiteralPath $runDir -PathType Container) -and
                    $sourceEvidenceRetained -and $archiveEvidenceAbsent
            }
            $caseResults.Add([ordered]@{
                Name = [string]$case.Name
                Passed = $passed
                Cleaned = [bool]$cleanup.Cleaned
                ExactQaEvidence = [bool]$cleanup.ExactQaEvidence
                ExactArtifacts = [bool]$cleanup.ExactArtifacts
                ExactTree = [bool]$cleanup.ExactTree
                SourceEvidenceRetained = $sourceEvidenceRetained
                ArchiveEvidenceAbsent = $archiveEvidenceAbsent
                Missing = @($cleanup.MissingQaEvidencePaths)
                Unexpected = @($cleanup.UnexpectedQaEvidencePaths)
                Damaged = @($cleanup.DamagedQaEvidencePaths)
                ContentValidation = @($cleanup.QaEvidence | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.RelativePath) } | ForEach-Object {
                    [ordered]@{ RelativePath = [string]$_.RelativePath; Validation = [string]$_.ContentValidation }
                })
            }) | Out-Null
        }

        $allPassed = @($caseResults.ToArray() | Where-Object { -not [bool]$_.Passed }).Count -eq 0
        return [pscustomobject]@{
            Passed = $allPassed
            Cases = @($caseResults.ToArray())
        }
    }
    finally {
        if (Test-Path -LiteralPath $testRoot -PathType Container) {
            Remove-Item -LiteralPath $testRoot -Recurse -Force
        }
    }
}

function Test-SmokeQaHostLifecycleLog {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [string] $RunId
    )

    $states = @('validated', 'activated', 'attached', 'started', 'updated', 'closed')
    $counts = [ordered]@{
        Validated = 0
        Activated = 0
        Attached = 0
        Started = 0
        Updated = 0
        Closed = 0
        Failed = 0
    }
    $lineNumbers = [ordered]@{
        Validated = 0
        Activated = 0
        Attached = 0
        Started = 0
        Updated = 0
        Closed = 0
    }
    $allLifecycleMarkerCount = 0
    $currentRunLifecycleMarkerCount = 0
    $allMarkersCurrentRun = $false
    $ordered = $false
    $closedDetailsPassed = $false
    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        $escapedRunId = [regex]::Escape($RunId)
        $allLifecycleMarkers = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern 'QA host lifecycle runId=[^;]+; state=[^;]+;')
        $currentRunLifecycleMarkers = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern ("QA host lifecycle runId={0}; state=[^;]+;" -f $escapedRunId))
        $allLifecycleMarkerCount = $allLifecycleMarkers.Count
        $currentRunLifecycleMarkerCount = $currentRunLifecycleMarkers.Count
        $allMarkersCurrentRun = $allLifecycleMarkerCount -gt 0 -and
            $allLifecycleMarkerCount -eq $currentRunLifecycleMarkerCount

        $orderedLines = New-Object 'System.Collections.Generic.List[int]'
        foreach ($state in $states) {
            $matches = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern ("QA host lifecycle runId={0}; state={1};" -f $escapedRunId, [regex]::Escape($state)))
            $key = $state.Substring(0, 1).ToUpperInvariant() + $state.Substring(1)
            $counts[$key] = $matches.Count
            if ($matches.Count -eq 1) {
                $lineNumbers[$key] = [int]$matches[0].LineNumber
                $orderedLines.Add([int]$matches[0].LineNumber) | Out-Null
            }
            if ($state -eq 'closed' -and $matches.Count -eq 1) {
                $closedLine = [string]$matches[0].Line
                $closedDetailsPassed = $closedLine -cmatch '(?:^|; )participantCloseSucceeded=true(?:;|$)' -and
                    $closedLine -cmatch '(?:^|; )listeners=0(?:;|$)' -and
                    $closedLine -cmatch '(?:^|; )participant=null(?:;|$)' -and
                    $closedLine -cmatch '(?:^|; )fallback=false(?:;|$)'
            }
        }
        $failedMatches = @(Select-String -LiteralPath $LogPath -CaseSensitive -Pattern ("QA host lifecycle runId={0}; state=failed;" -f $escapedRunId))
        $counts.Failed = $failedMatches.Count

        $ordered = $orderedLines.Count -eq $states.Count
        if ($ordered) {
            for ($index = 1; $index -lt $orderedLines.Count; $index++) {
                if ($orderedLines[$index] -le $orderedLines[$index - 1]) {
                    $ordered = $false
                    break
                }
            }
        }
    }

    $passed = $counts.Validated -eq 1 -and
        $counts.Activated -eq 1 -and
        $counts.Attached -eq 1 -and
        $counts.Started -eq 1 -and
        $counts.Updated -eq 1 -and
        $counts.Closed -eq 1 -and
        $counts.Failed -eq 0 -and
        $currentRunLifecycleMarkerCount -eq 6 -and
        $allMarkersCurrentRun -and
        $ordered -and
        $closedDetailsPassed
    return [pscustomobject]@{
        RunId = $RunId
        MarkerCounts = $counts
        MarkerLineNumbers = $lineNumbers
        AllLifecycleMarkerCount = $allLifecycleMarkerCount
        CurrentRunLifecycleMarkerCount = $currentRunLifecycleMarkerCount
        AllMarkersCurrentRun = $allMarkersCurrentRun
        Ordered = $ordered
        ClosedDetailsPassed = $closedDetailsPassed
        FailedStateAbsent = $counts.Failed -eq 0
        Passed = $passed
    }
}
