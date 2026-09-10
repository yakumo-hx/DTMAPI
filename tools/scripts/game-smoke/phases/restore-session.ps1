$restoreErrors = New-Object 'System.Collections.Generic.List[string]'
    $recoveryExit = Wait-SmokeProcessExitBeforeRecovery -EvidencePath $evidence -TimeoutSeconds 15
    if (-not [bool]$recoveryExit.Exited) {
        $manualRecoveryItems = New-Object 'System.Collections.Generic.List[object]'
        if ($null -ne $officialModProfileSummary -and [bool]$officialModProfileSummary.Applied -and -not $officialModProfileRestored) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'OfficialModEnablement'
                TargetPath = [string]$officialModProfileSummary.EnablementPath
                BackupPath = [string]$officialModProfileSummary.BackupPath
                ExistedBefore = $true
                ActionAfterExit = 'CopyBackupToTarget'
            }) | Out-Null
        }
        if ($null -ne $local11AuthorSourceSummary -and [bool]$local11AuthorSourceSummary.Applied -and -not $local11AuthorSourceRestored) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'LocalProductAuthorSourceState'
                TargetPath = [string]$local11AuthorSourceSummary.StatePath
                BackupPath = [string]$local11AuthorSourceSummary.BackupPath
                ExistedBefore = [bool]$local11AuthorSourceSummary.OriginalExisted
                ActionAfterExit = if ([bool]$local11AuthorSourceSummary.OriginalExisted) { 'CopyBackupToTarget' } else { 'RemoveTargetIfPresent' }
                ExpectedLength = [int64]$local11AuthorSourceSummary.OriginalLength
                ExpectedSha256 = [string]$local11AuthorSourceSummary.OriginalSha256
            }) | Out-Null
        }
        if ($AutoExerciseStrongPlantingGun) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'StrongPlantingGunExactConfig'
                TargetPath = [System.IO.Path]::GetFullPath($strongPlantingGunConfigPath)
                BackupPath = if ($strongPlantingGunConfigHadOriginal) {
                    [System.IO.Path]::GetFullPath($strongPlantingGunConfigBackup)
                }
                else {
                    ''
                }
                ExistedBefore = [bool]$strongPlantingGunConfigHadOriginal
                ActionAfterExit = if ($strongPlantingGunConfigHadOriginal) {
                    'CopyBackupToTarget'
                }
                else {
                    'RemoveTargetIfPresent'
                }
            }) | Out-Null
        }
        foreach ($config in @(
            [ordered]@{ Used = [bool]$usesOneActionConfigSmoke; Kind = 'OneActionConfig'; TargetPath = $oneActionConfigPath; BackupPath = $oneActionConfigBackup; ExistedBefore = [bool]$oneActionConfigHadOriginal },
            [ordered]@{ Used = [bool]$usesAutoFishingConfigSmoke; Kind = 'AutoFishingConfig'; TargetPath = $autoFishingConfigPath; BackupPath = $autoFishingConfigBackup; ExistedBefore = [bool]$autoFishingConfigHadOriginal },
            [ordered]@{ Used = [bool]$usesActionSpeedConfigSmoke; Kind = 'ActionSpeedConfig'; TargetPath = $actionSpeedConfigPath; BackupPath = $actionSpeedConfigBackup; ExistedBefore = [bool]$actionSpeedConfigHadOriginal }
        )) {
            if (-not [bool]$config.Used) {
                continue
            }
            $manualRecoveryItems.Add([ordered]@{
                Kind = [string]$config.Kind
                TargetPath = [System.IO.Path]::GetFullPath([string]$config.TargetPath)
                BackupPath = [System.IO.Path]::GetFullPath([string]$config.BackupPath)
                ExistedBefore = [bool]$config.ExistedBefore
                ActionAfterExit = if ([bool]$config.ExistedBefore) { 'CopyBackupToTarget' } else { 'RemoveTargetIfPresent' }
            }) | Out-Null
        }
        if ($Batch6AutoFishingManagerLifecycle) {
            $manualRecoveryItems.Add([ordered]@{
                Kind = 'Batch6AutoFishingManagerDisabledMarker'
                TargetPath = $batch6ManagerMarkerPath
                BackupPath = $batch6ManagerMarkerBackupPath
                ExistedBefore = $batch6ManagerMarkerExistedBefore
                ActionAfterExit = if ($batch6ManagerMarkerExistedBefore) { 'CopyBackupToTarget' } else { 'RemoveTargetIfPresent' }
                ExpectedLength = $batch6ManagerMarkerLengthBefore
                ExpectedSha256 = $batch6ManagerMarkerSha256Before
            }) | Out-Null
        }
        if ($null -ne $qaHostStage) {
            foreach ($artifact in @($qaHostStage.Artifacts)) {
                $manualRecoveryItems.Add([ordered]@{
                    Kind = 'QaHost' + [string]$artifact.Kind
                    TargetPath = [string]$artifact.Path
                    ActionAfterExit = 'RemoveOnlyIfLengthAndSha256Match'
                    ExpectedLength = [int64]$artifact.Length
                    ExpectedSha256 = [string]$artifact.Sha256
                }) | Out-Null
            }
            foreach ($directory in @([string]$qaHostStage.RunDir, [string]$qaHostStage.QaRoot)) {
                $manualRecoveryItems.Add([ordered]@{
                    Kind = 'QaHostDirectory'
                    TargetPath = $directory
                    ActionAfterExit = 'RemoveOnlyIfEmptyAfterExactArtifacts'
                }) | Out-Null
            }
        }

        $manualRecoveryReceiptPath = Join-Path $evidence 'manual-recovery-required.json'
        $manualRecoveryInstructionsPath = Join-Path $evidence 'MANUAL-RECOVERY-REQUIRED.txt'
        $manualRecoveryReceiptError = ''
        try {
            Write-SmokeJsonObject -Path $manualRecoveryReceiptPath -Value ([ordered]@{
                CreatedAt = (Get-Date).ToString('o')
                Reason = 'Stable DolocTown process absence was not proven during the bounded recovery wait. No player-owned save was backed up or written back; mod_infos.json, author source state, smoke setting, product config, and runner-owned QA host cleanup remain blocked.'
                ProcessExitEvidence = $recoveryExit
                RecoveryItems = @($manualRecoveryItems.ToArray())
                RequiredOrder = @(
                    'Verify Get-Process -Name DolocTown returns no process.',
                    'Apply each RecoveryItems action exactly as recorded.',
                    'Verify each non-save test asset against its recorded receipt before launching the game again.'
                )
            })
            @(
                'DTMAPI smoke restoration was intentionally blocked because stable DolocTown process absence was not proven.',
                'Do not edit or restore player save files; this runner created no routine archive backup.',
                "Use the exact target, backup, action, and expected hash entries in: $manualRecoveryReceiptPath",
                'After recovery, verify every changed non-save test asset byte-for-byte before starting DolocTown again.'
            ) | Set-Content -LiteralPath $manualRecoveryInstructionsPath
        }
        catch {
            $manualRecoveryReceiptError = [string]$_.Exception.Message
        }
        throw "Smoke cleanup blocked: stable DolocTown process absence was not proven during a 15-second bounded wait. No routine player archive backup or writeback exists. Manual recovery receipt: $manualRecoveryReceiptPath. receiptError=$manualRecoveryReceiptError"
    }
    if ($noNativeSaveMetadataRequested) {
        try {
            $playerSaveFamilyChecks = @($playerSaveSnapshots | ForEach-Object {
                Compare-DtmApiCurrentSaveArchiveFamilySnapshot -Baseline $_
            })
            $playerSaveUnchangedBeforeCleanupOk =
                @($playerSaveFamilyChecks | Where-Object { -not [bool]$_.Passed }).Count -eq 0
            Write-SmokeJsonObject -Path (Join-Path $evidence 'player-save-unchanged-before-cleanup.json') -Value ([ordered]@{
                SaveTestMode = $SaveTestMode
                ComparedBeforeRunnerOrExternalRestore = $true
                RoutineByteBackupCreated = $false
                PlayerArchiveWritebackPerformed = $false
                CurrentArchiveFamily = '<current>.data + <current>.data.prev[0-9]+ + <current>.data.bak'
                Families = @($playerSaveFamilyChecks)
                Passed = $playerSaveUnchangedBeforeCleanupOk
            })
            $committedSidecarsUnchangedBeforeCleanupOk = Compare-SmokeFileMetadataSnapshots `
                -Snapshots @($committedSidecarSnapshots) `
                -EvidencePath (Join-Path $evidence 'committed-sidecar-unchanged-before-cleanup.json') `
                -SaveTestMode $SaveTestMode
            $g5CommittedSidecarUnchangedOk =
                -not [bool]$qaG5AnyRequested -or
                $committedSidecarsUnchangedBeforeCleanupOk
        }
        catch {
            $playerSaveUnchangedBeforeCleanupOk = $false
            $committedSidecarsUnchangedBeforeCleanupOk = $false
            $g5CommittedSidecarUnchangedOk = $false
            $restoreErrors.Add("no-native-save-pre-cleanup-verification:$([string]$_.Exception.Message)") | Out-Null
        }
        if (-not $playerSaveUnchangedBeforeCleanupOk) {
            $restoreErrors.Add('player-save:changed-before-cleanup') | Out-Null
        }
        if (-not $committedSidecarsUnchangedBeforeCleanupOk) {
            $restoreErrors.Add('committed-sidecar:changed-before-config-cleanup') | Out-Null
        }
    }
    if ($AutoExerciseStrongPlantingGun) {
        try {
            if ($strongPlantingGunConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $strongPlantingGunConfigBackup -Destination $strongPlantingGunConfigPath
            }
            elseif (Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf) {
                Remove-Item -LiteralPath $strongPlantingGunConfigPath -Force
            }
            $strongPlantingGunConfigRestored =
                if ($strongPlantingGunConfigHadOriginal) {
                    (Test-Path -LiteralPath $strongPlantingGunConfigPath -PathType Leaf) -and
                    [string]::Equals(
                        (Get-SmokeFileSha256 -Path $strongPlantingGunConfigPath),
                        (Get-SmokeFileSha256 -Path $strongPlantingGunConfigBackup),
                        [System.StringComparison]::OrdinalIgnoreCase)
                }
                else {
                    -not (Test-Path -LiteralPath $strongPlantingGunConfigPath)
                }
            if (-not $strongPlantingGunConfigRestored) {
                $g5ConfigDirectoryRestored = $false
                $restoreErrors.Add('strong-planting-gun-config:verification-failed') | Out-Null
            }
        }
        catch {
            $g5ConfigDirectoryRestored = $false
            $restoreErrors.Add("strong-planting-gun-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($null -ne $officialModProfileSummary -and [bool]$officialModProfileSummary.Applied -and -not $officialModProfileRestored) {
        try {
            $officialModProfileRestored = Restore-SmokeOfficialModProfile -Summary $officialModProfileSummary -EvidencePath $evidence -Phase 'run-finally'
            "OfficialModProfileRestored=$officialModProfileRestored $(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        catch {
            $officialModProfileRestored = $false
            $restoreErrors.Add("official-mod-profile:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($null -ne $local11AuthorSourceSummary -and [bool]$local11AuthorSourceSummary.Applied -and -not $local11AuthorSourceRestored) {
        try {
            $local11AuthorSourceRestored = Restore-SmokeRecoveryOnlyAuthorSourceState -Summary $local11AuthorSourceSummary -EvidencePath $evidence
            "LocalProductAuthorSourceStateRestored=$local11AuthorSourceRestored $(Get-Date -Format o)" | Add-Content -LiteralPath (Join-Path $evidence 'summary.txt')
        }
        catch {
            $local11AuthorSourceRestored = $false
            $restoreErrors.Add("local11-author-source-state:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($usesOneActionConfigSmoke) {
        try {
            if ($oneActionConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $oneActionConfigBackup -Destination $oneActionConfigPath
            }
            elseif (Test-Path -LiteralPath $oneActionConfigPath) {
                Remove-Item -Force -LiteralPath $oneActionConfigPath
            }
        }
        catch {
            $restoreErrors.Add("one-action-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($usesAutoFishingConfigSmoke) {
        try {
            if ($autoFishingConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $autoFishingConfigBackup -Destination $autoFishingConfigPath
            }
            elseif (Test-Path -LiteralPath $autoFishingConfigPath) {
                Remove-Item -Force -LiteralPath $autoFishingConfigPath
            }
        }
        catch {
            $restoreErrors.Add("auto-fishing-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($usesActionSpeedConfigSmoke) {
        try {
            if ($actionSpeedConfigHadOriginal) {
                Copy-Item -Force -LiteralPath $actionSpeedConfigBackup -Destination $actionSpeedConfigPath
            }
            elseif (Test-Path -LiteralPath $actionSpeedConfigPath) {
                Remove-Item -Force -LiteralPath $actionSpeedConfigPath
            }
        }
        catch {
            $restoreErrors.Add("action-speed-config:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($Batch6AutoFishingManagerLifecycle) {
        try {
            if ($batch6ManagerMarkerExistedBefore) {
                Copy-Item -Force -LiteralPath $batch6ManagerMarkerBackupPath -Destination $batch6ManagerMarkerPath
            }
            elseif (Test-Path -LiteralPath $batch6ManagerMarkerPath) {
                if (-not (Test-Path -LiteralPath $batch6ManagerMarkerPath -PathType Leaf)) {
                    throw 'Marker restore refused to remove a non-file dtmapi.disabled path.'
                }
                $markerShaBeforeRemove = Get-SmokeFileSha256 -Path $batch6ManagerMarkerPath
                if (-not [string]::Equals($markerShaBeforeRemove, $Batch6AutoFishingManagerMarkerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                    throw 'Marker restore refused to remove a dtmapi.disabled file not created by this lifecycle run.'
                }
                Remove-Item -Force -LiteralPath $batch6ManagerMarkerPath
            }
            $markerExistsAfterRestore = Test-Path -LiteralPath $batch6ManagerMarkerPath -PathType Leaf
            $markerLengthAfterRestore = if ($markerExistsAfterRestore) { [int64](Get-Item -LiteralPath $batch6ManagerMarkerPath).Length } else { [int64]0 }
            $markerShaAfterRestore = if ($markerExistsAfterRestore) { Get-SmokeFileSha256 -Path $batch6ManagerMarkerPath } else { '' }
            $batch6ManagerMarkerRestoreOk = $markerExistsAfterRestore -eq $batch6ManagerMarkerExistedBefore -and
                $markerLengthAfterRestore -eq $batch6ManagerMarkerLengthBefore -and
                [string]::Equals($markerShaAfterRestore, $batch6ManagerMarkerSha256Before, [System.StringComparison]::OrdinalIgnoreCase)
            Write-SmokeJsonObject -Path (Join-Path $evidence 'batch6-autofishing-manager-marker-restore.json') -Value ([ordered]@{
                Path = $batch6ManagerMarkerPath
                ExpectedExists = $batch6ManagerMarkerExistedBefore
                ActualExists = $markerExistsAfterRestore
                ExpectedLength = $batch6ManagerMarkerLengthBefore
                ActualLength = $markerLengthAfterRestore
                ExpectedSha256 = $batch6ManagerMarkerSha256Before
                ActualSha256 = $markerShaAfterRestore
                Passed = $batch6ManagerMarkerRestoreOk
            })
            if (-not $batch6ManagerMarkerRestoreOk) { $restoreErrors.Add('batch6-autofishing-manager-marker:verification-failed') | Out-Null }
        }
        catch {
            $batch6ManagerMarkerRestoreOk = $false
            $restoreErrors.Add("batch6-autofishing-manager-marker:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($null -ne $qaHostStage) {
        try {
            $qaHostCleanup = Remove-SmokeQaHostStage -Stage $qaHostStage -EvidencePath $evidence
            if (-not [bool]$qaHostCleanup.Cleaned) {
                $restoreErrors.Add("qa-host-cleanup:$([string]$qaHostCleanup.Reason)") | Out-Null
            }
        }
        catch {
            $restoreErrors.Add("qa-host-cleanup:$([string]$_.Exception.Message)") | Out-Null
        }
    }
    if ($restoreErrors.Count -gt 0) {
        throw "Smoke state restoration failed: $([string]::Join('; ', $restoreErrors.ToArray())). Original failure: $smokeBodyFailure Evidence: $evidence"
    }
