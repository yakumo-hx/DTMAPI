if ($disposableSaveFixtureRequested -and
        -not [string]::IsNullOrWhiteSpace(
            $disposableSaveFixtureRootResolved)) {
        $smokeSaveEnvironmentScopeApplied = $true
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT =
            if ($moreSavesFixed12AcceptanceRequested) {
                Get-DolocTownLivePersistentRootForSmoke
            }
            else {
                $disposableSaveFixtureRootResolved
            }
        $env:DTMAPI_STATE_DIR = [System.IO.Path]::GetFullPath(
            (Join-Path $disposableSaveFixtureRootResolved 'DTMAPI'))
        $disposableSaveFixtureCleanupRequested =
            -not [bool]$ValidateSaveTestModeOnly -and
            -not [bool]$RetainDisposableSaveFixtureOnSuccess
    }

    if ($ValidateSaveTestModeOnly) {
        [pscustomobject]@{
            SaveTestMode = $SaveTestMode
            SaveSlot = $SaveSlot
            WaitForManualExit = [bool]$WaitForManualExit
            TitleOnlyManualObservation = [bool]$WaitForManualExit -and $SaveSlot -eq 0
            NativeSaveRouteRequested = $nativeSaveRouteRequested
            ArchiveMutationRouteRequested = $archiveMutationRouteRequested
            MoreSavesFixed12AcceptancePhase =
                $MoreSavesFixed12AcceptancePhase
            MoreSavesFixed12OfficialLocalRootMode =
                $moreSavesFixed12OfficialLocalRootMode
            MoreSavesFixed12OfficialLocalProductRoot =
                $moreSavesFixed12OfficialLocalProductRoot
            DisposableSaveFixtureRoot = $disposableSaveFixtureRootResolved
            SteamAutoCloudIsolated = if ($null -ne $disposableSaveFixtureMarker) {
                [bool]$disposableSaveFixtureMarker.steamAutoCloudIsolated
            }
            else {
                $null
            }
            PlayerArchiveWritebackAllowed = $false
            DisposableSaveFixtureCleanupRequested = $false
            DisposableSaveFixtureRetentionRequested =
                [bool]$RetainDisposableSaveFixtureOnSuccess
            Passed = $true
        } | ConvertTo-Json -Depth 4
        $SmokeRunnerExitCode = [int](0); return
    }
