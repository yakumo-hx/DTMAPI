param([string] $OutputRoot = '')
. "$PSScriptRoot/common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo ('tmp/test-runs/game-smoke-modules-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $OutputRoot) { throw 'Module tests require a new output directory.' }
New-Item -ItemType Directory -Path $OutputRoot | Out-Null
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$SmokeRunnerModuleRoot = Join-Path $PSScriptRoot 'game-smoke'
$SmokeRunnerBoundParameters = @{}
. (Join-Path $SmokeRunnerModuleRoot 'load-modules.ps1')
. (Join-Path $SmokeRunnerModuleRoot 'scenarios/more-saves.ps1')
$checks = New-Object 'Collections.Generic.List[string]'
function Assert-Module([bool]$Passed,[string]$Name) {
    if (-not $Passed) { throw "Module behavior failed: $Name" }
    $checks.Add($Name)
}
function Assert-Rejected([scriptblock]$Action,[string]$Name) {
    $rejected=$false
    try { & $Action | Out-Null } catch { $rejected=$true }
    Assert-Module $rejected $Name
}
function Write-TestText([string]$Path,[string]$Text) {
    [IO.File]::WriteAllText($Path,$Text,(New-Object Text.UTF8Encoding($false)))
}
function New-TestDirectory([string]$Name) {
    $path=Join-Path $OutputRoot $Name
    New-Item -ItemType Directory -Path $path -Force | Out-Null
    return $path
}
Assert-Module (@(Get-SmokeSelectedScenarioModules -Parameters @{}).Count -eq 0) 'ordinary run selects no product scenario'
$selected=@(Get-SmokeSelectedScenarioModules -Parameters @{StageQaHost=$true;MoreSavesFixed12AcceptancePhase='EnabledLifecycle'})
Assert-Module (($selected -join '|') -ceq 'core/qa-host.ps1|scenarios/more-saves.ps1') 'MoreSaves selects only QA and its scenario'
Assert-Module (@(Get-SmokeSelectedScenarioModules -Parameters @{Batch6AutoFishingPilot=$true}) -contains 'scenarios/desktop-input.ps1') 'existing Batch6 input route retains its input helpers'
Assert-Module ((Get-SmokeNativeArchiveIndex -UiSaveSlot 1) -eq 0 -and (Get-SmokeNativeArchiveIndex -UiSaveSlot 12) -eq 11) 'UI slots map to native indices'
Assert-Rejected { Get-SmokeNativeArchiveIndex -UiSaveSlot 0 } 'title-only zero is not a native slot'
Assert-Module ([bool](Test-SmokeNoQaDeadlineContract).Passed) 'expired and late deadline actions rejected'
Assert-Module ((Get-SmokeRemainingBudgetSeconds -Deadline ([datetime]::MaxValue) -MaximumSeconds 12) -eq 12) 'uncapped legacy deadline preserves requested wait'

$plan=@(Get-MoreSavesFixed12PhasePlan)
Assert-Module (($plan.Id -join '|') -ceq 'EnabledLifecycle|DisabledCold|ReenabledCold' -and ($plan.SaveTestMode -join '|') -ceq 'ArchiveMutation|NoNativeSave|NoNativeSave') 'ordered three-phase save contract'
$options=@{Phase='EnabledLifecycle';StageQaHost=$true;DirectExe=$true;SkipInstall=$true;SaveSlotExplicit=$true;SaveSlot=7;FixtureRoot='declared-fixture';SaveTestMode='ArchiveMutation';OfficialModProfile='CoreOnly';IsolateAllOfficialMods=$true;ExtraEnabledIds=@('Local.DTMAPI_MoreSaves')}
Assert-Module ([bool](Assert-MoreSavesFixed12PhaseOptions -Options $options).ProductEnabled) 'enabled phase accepted'
foreach($key in @('StageQaHost','DirectExe','SkipInstall','SaveSlotExplicit','IsolateAllOfficialMods')) {
    $negative=$options.Clone();$negative[$key]=$false
    Assert-Rejected { Assert-MoreSavesFixed12PhaseOptions -Options $negative } ("phase missing $key")
}
foreach($change in @(@{Key='SaveSlot';Value=6},@{Key='SaveTestMode';Value='NoNativeSave'},@{Key='ExtraEnabledIds';Value=@('Workshop.3742763050')},@{Key='FixtureRoot';Value=''})) {
    $negative=$options.Clone();$negative[$change.Key]=$change.Value
    Assert-Rejected { Assert-MoreSavesFixed12PhaseOptions -Options $negative } ("phase rejects mismatched $($change.Key)")
}
$disabled=$options.Clone();$disabled.Phase='DisabledCold';$disabled.SaveTestMode='NoNativeSave';$disabled.ExtraEnabledIds=@()
Assert-Module (-not [bool](Assert-MoreSavesFixed12PhaseOptions -Options $disabled).ProductEnabled) 'disabled phase has no enabled product'
$disabled.ExtraEnabledIds=@('Local.DTMAPI_MoreSaves')
Assert-Rejected { Assert-MoreSavesFixed12PhaseOptions -Options $disabled } 'disabled phase rejects retained product'

$save=New-TestDirectory 'risk/SAVE';$local=New-TestDirectory 'risk/MODS';$workshop=New-TestDirectory 'risk/workshop'
$info=Join-Path $OutputRoot 'risk/mod_infos.json'
Write-TestText $info '{"modInfos":{"Local.DTMAPI_MoreSaves":{"enabled":true}}}'
$riskArgs=@{EnablementPath=$info;SaveRoot=$save;OfficialLocalModsRoot=$local;WorkshopContentRoot=$workshop;SaveTestMode='NoNativeSave';DisposableFixtureRequested=$false;StageQaHost=$false;DirectExe=$true;UseSteam=$false}
Write-TestText (Join-Path $save 'ea-playtest-doloc-archive-6.data') 'legacy'
$risk=Get-SmokeMoreSavesStartupRisk @riskArgs
Assert-Module ($risk.Passed -and -not $risk.StartupMigrationDetected) 'stale enabled ID without package does not require isolation'
$product=New-TestDirectory 'risk/MODS/DTMAPI_MoreSaves';[void](New-TestDirectory 'risk/MODS/DTMAPI_MoreSaves/Content/DTMAPI')
Write-TestText (Join-Path $product 'info.json') '{}'
Write-TestText (Join-Path $product 'Content/DTMAPI/manifest.json') '{"UniqueID":"DTMAPI.MoreSavesMod","EntryDll":"Content/DTMAPI/DTMAPI.MoreSaves.dll"}'
Write-TestText (Join-Path $product 'Content/DTMAPI/DTMAPI.MoreSaves.dll') 'synthetic-entry-presence-only'
$risk=Get-SmokeMoreSavesStartupRisk @riskArgs
Assert-Module (-not $risk.Passed -and $risk.StartupMigrationDetected) 'ordinary run blocks actual enabled entry with legacy archive'
$riskArgs.SaveTestMode='ArchiveMutation';$riskArgs.DisposableFixtureRequested=$true;$riskArgs.StageQaHost=$true
Assert-Module ([bool](Get-SmokeMoreSavesStartupRisk @riskArgs).Passed) 'classified disposable migration accepted'
Write-TestText (Join-Path $product 'Content/DTMAPI/manifest.json') '{"UniqueID":"DTMAPI.MoreSavesMod","EntryDll":"DTMAPI.MoreSaves.dll"}'
Assert-Module (-not [bool](Get-SmokeMoreSavesStartupRisk @riskArgs).Passed) 'source-only EntryDll template is not an official package binding'
Write-TestText (Join-Path $product 'Content/DTMAPI/manifest.json') '{"UniqueID":"DTMAPI.MoreSavesMod","EntryDll":"../DTMAPI.MoreSaves.dll"}'
Assert-Module (-not [bool](Get-SmokeMoreSavesStartupRisk @riskArgs).Passed) 'package entry traversal rejected'
Write-TestText (Join-Path $product 'Content/DTMAPI/manifest.json') '{"UniqueID":"DTMAPI.MoreSavesMod","EntryDll":"Content/DTMAPI/DTMAPI.MoreSaves.dll"}'
$riskArgs.UseSteam=$true
Assert-Module (-not [bool](Get-SmokeMoreSavesStartupRisk @riskArgs).Passed) 'Steam cannot bypass pre-Runtime migration guard'
$riskArgs.UseSteam=$false;$riskArgs.SaveTestMode='NoNativeSave'
Write-TestText $info '{"modInfos":{"Local.DTMAPI_MoreSaves":{"enabled":false}}}'
Assert-Module ([bool](Get-SmokeMoreSavesStartupRisk @riskArgs).Passed) 'disabled entry with legacy archives needs no mutation mode'
Write-TestText $info '{"modInfos":{"Local.DTMAPI_MoreSaves":{"enabled":true}}}'
Write-TestText (Join-Path $product 'Content/DTMAPI/manifest.json') '{"UniqueID":"Other","EntryDll":"DTMAPI.MoreSaves.dll"}'
$risk=Get-SmokeMoreSavesStartupRisk @riskArgs
Assert-Module (-not $risk.Passed -and $risk.PackageClassificationUncertain) 'unrecognized present package fails closed'
Remove-Item -LiteralPath (Join-Path $save 'ea-playtest-doloc-archive-6.data')
Assert-Module ([bool](Get-SmokeMoreSavesStartupRisk @riskArgs).Passed) 'no legacy candidates avoids unnecessary product inspection'

$equipment=New-TestDirectory 'pending/MODS/DTMAPI_MoreEquipmentSlots'
[void](New-TestDirectory 'pending/MODS/DTMAPI_MoreEquipmentSlots/Content/DTMAPI')
Write-TestText (Join-Path $equipment 'info.json') '{}'
Write-TestText (Join-Path $equipment 'Content/DTMAPI/manifest.json') '{"UniqueID":"DTMAPI.MoreEquipmentSlotsMod","EntryDll":"Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll"}'
Write-TestText (Join-Path $equipment 'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll') 'synthetic-entry'
$pendingCatalog=Join-Path $OutputRoot 'pending/catalog.json'
Write-SmokeJsonObject -Path $pendingCatalog -Value @{products=@(@{catalogId='more-equipment-slots';uniqueId='DTMAPI.MoreEquipmentSlotsMod';officialFolder='DTMAPI_MoreEquipmentSlots';packageDll='DTMAPI.MoreEquipmentSlots.dll';workshopId='3744059735';saveSidecars=@('DTMAPI/config/protected-items/equipment-slots/slot-<archiveIndex>/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json')})}
$pendingState=New-TestDirectory 'pending/state'
$pendingDirectory=New-TestDirectory 'pending/state/config/protected-items/equipment-slots/slot-2'
$pendingSidecar=Join-Path $pendingDirectory 'equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json'
$pendingText='{"schemaVersion":3,"scope":{"archiveIndex":2},"generation":11,"journal":{"origin":3,"phase":0,"attemptStarted":true,"escrow":[{},{}]}}'
Write-TestText $pendingSidecar $pendingText
$pendingArgs=@{CatalogPath=$pendingCatalog;StateDir=$pendingState;OfficialLocalModsRoot=(Join-Path $OutputRoot 'pending/MODS');WorkshopContentRoot=$workshop;EnabledSourceIds=@('Local.DTMAPI_MoreEquipmentSlots');ArchiveIndex=2;SaveTestMode='NoNativeSave'}
$pending=Get-SmokePendingProductRecovery @pendingArgs
Assert-Module (-not $pending.Passed -and $pending.Reason -ceq 'PendingProductRecovery') 'known selected-slot unfinished attempt stops before launch'
$differentSlot=$pendingArgs.Clone();$differentSlot.ArchiveIndex=5
Assert-Module ([bool](Get-SmokePendingProductRecovery @differentSlot).Passed) 'pending journal in another slot does not block selected slot'
$disabledOwner=$pendingArgs.Clone();$disabledOwner.EnabledSourceIds=@()
Assert-Module ([bool](Get-SmokePendingProductRecovery @disabledOwner).Passed) 'disabled owner does not trigger recovery prerequisite'
$recoveryScenario=$pendingArgs.Clone();$recoveryScenario.ExplicitRecoveryScenario=$true
Assert-Module ([bool](Get-SmokePendingProductRecovery @recoveryScenario).Passed) 'explicit recovery scenario keeps its own gate'
Write-TestText $pendingSidecar $pendingText.Replace('"attemptStarted":true','"attemptStarted":false')
Assert-Module ([bool](Get-SmokePendingProductRecovery @pendingArgs).Passed) 'generation and escrow alone do not imply pending recovery'
Write-TestText $pendingSidecar '{"schemaVersion":3,"scope":{"archiveIndex":2},"generation":11,"journal":null}'
Assert-Module ([bool](Get-SmokePendingProductRecovery @pendingArgs).Passed) 'current v3 with no journal accepted'
Write-TestText $pendingSidecar $pendingText.Replace('"phase":0','"phase":1')
Assert-Module ([bool](Get-SmokePendingProductRecovery @pendingArgs).Passed) 'guard does not generalize to an unreviewed journal phase'
Write-TestText $pendingSidecar $pendingText
Assert-Module ((Get-SmokeFileSha256 -Path $pendingSidecar) -ceq $pending.SidecarSha256) 'recovery preflight never edits or clears the sidecar'

$flatText='{"schemaVersion":3,"ownerId":"DTMAPI.MoreEquipmentSlotsMod","storageScope":"slot-2","archiveIndex":2,"generation":3,"slots":[],"journal":null}'
Write-TestText $pendingSidecar $flatText
$flatHash=Get-SmokeFileSha256 -Path $pendingSidecar
$migration=Get-SmokePendingProductRecovery @pendingArgs
Assert-Module (-not $migration.Passed -and $migration.PendingProductMigration -and -not $migration.PendingProductRecovery -and $migration.Reason -ceq 'PendingProductMigration') 'reviewed scoped flat schema 3 without journal stops before load migration'
Assert-Module ([bool](Get-SmokePendingProductRecovery @disabledOwner).Passed) 'scoped flat file does not block when its owner will not load'
Assert-Module ([bool](Get-SmokePendingProductRecovery @recoveryScenario).Passed) 'scoped flat explicit recovery retains its own prerequisite'
Assert-Module ((Get-SmokeFileSha256 -Path $pendingSidecar) -ceq $flatHash) 'migration preflight never converts or backs up the sidecar'
Write-TestText $pendingSidecar $flatText.Replace('"archiveIndex":2','"archiveIndex":5')
Assert-Module (-not [bool](Get-SmokePendingProductRecovery @pendingArgs).PendingProductMigration) 'flat migration classification binds the selected native index'
Write-TestText $pendingSidecar $flatText.Replace('"storageScope":"slot-2"','"storageScope":"slot-5"')
Assert-Module (-not [bool](Get-SmokePendingProductRecovery @pendingArgs).PendingProductMigration) 'flat migration classification binds the scoped slot name'
Write-TestText $pendingSidecar $flatText.Replace('"DTMAPI.MoreEquipmentSlotsMod"','"Other.Owner"')
Assert-Module (-not [bool](Get-SmokePendingProductRecovery @pendingArgs).PendingProductMigration) 'flat migration classification binds the reviewed owner'
Write-TestText $pendingSidecar '{"schemaVersion":3,"scope":{"archiveIndex":2},"generation":3,"slots":[],"journal":null}'
Assert-Module ([bool](Get-SmokePendingProductRecovery @pendingArgs).Passed) 'Product v3 scope shape without journal is not a flat migration'

$coldText='{"schemaVersion":3,"scope":{"archiveIndex":2},"generation":12,"slots":[],"journal":{"origin":3,"phase":0,"scope":{"archiveIndex":2},"attemptStarted":false,"escrow":[{"itemId":"fixture_hat"},{"itemId":"fixture_button"}]}}'
Write-TestText $pendingSidecar $coldText
$coldHash=Get-SmokeFileSha256 -Path $pendingSidecar
$cold=Get-SmokePendingProductRecovery @disabledOwner
Assert-Module (-not $cold.Passed -and $cold.PendingProductColdRecovery -and $cold.Reason -ceq 'PendingProductColdRecovery') 'disabled owner with canonical recovery journal requires cold-recovery scope'
Assert-Module ([bool](Get-SmokePendingProductRecovery @pendingArgs).Passed) 'loaded Product with inactive attempt is not classified by escrow count as cold recovery'
$coldScenario=$disabledOwner.Clone();$coldScenario.ExplicitRecoveryScenario=$true
Assert-Module ([bool](Get-SmokePendingProductRecovery @coldScenario).Passed) 'explicit cold-recovery scenario retains its own gate'
Assert-Module ((Get-SmokeFileSha256 -Path $pendingSidecar) -ceq $coldHash) 'cold preflight leaves canonical and native storage untouched'
Write-TestText $pendingSidecar '{"schemaVersion":3,"scope":{"archiveIndex":2},"slots":[{"index":0,"itemId":"fixture_hat"}],"journal":null}'
Assert-Module (-not [bool](Get-SmokePendingProductRecovery @disabledOwner).Passed) 'disabled owner committed occupied slot requires cold-recovery scope'
Write-TestText $pendingSidecar '{"schemaVersion":3,"scope":{"archiveIndex":2},"slots":[{"index":0,"itemId":"  "}],"journal":null}'
Assert-Module ([bool](Get-SmokePendingProductRecovery @disabledOwner).Passed) 'empty Product slots without a journal have no known cold placement'
Write-TestText $pendingSidecar $coldText.Replace('"archiveIndex":2','"archiveIndex":5')
Assert-Module ([bool](Get-SmokePendingProductRecovery @disabledOwner).Passed) 'cold preflight binds the selected native scope'
Write-TestText $pendingSidecar $coldText.Replace('"origin":3','"origin":0')
Assert-Module (-not [bool](Get-SmokePendingProductRecovery @disabledOwner).PendingProductColdRecovery) 'unknown journal origin is not asserted to be the observed cold-recovery route'

$state=@{Now=[datetime]'2026-09-08T00:00:00Z';Alive=$true}
$clock={ $state.Now }.GetNewClosure()
$sleep={param($Milliseconds) $state.Now=$state.Now.AddMilliseconds($Milliseconds)}.GetNewClosure()
$processQuery={if($state.Alive){[pscustomobject]@{Id=12345}}}.GetNewClosure()
$stuckEvidence=New-TestDirectory 'stuck-process'
$stuck=Wait-SmokeProcessExitBeforeRecovery -EvidencePath $stuckEvidence -TimeoutSeconds 2 -ProcessQuery $processQuery -Clock $clock -Sleep $sleep -Close {param($Process) $true}
Assert-Module (-not $stuck.Exited -and @($stuck.RemainingProcessIds).Count -eq 1 -and $stuck.GracefulCloseRequested) 'close request without stable process absence does not permit restore'
$state.Alive=$false
$exitEvidence=New-TestDirectory 'exited-process'
$exited=Wait-SmokeProcessExitBeforeRecovery -EvidencePath $exitEvidence -TimeoutSeconds 2 -ProcessQuery $processQuery -Clock $clock -Sleep $sleep -Close {param($Process) $false}
Assert-Module ($exited.Exited -and $exited.StableAbsenceObserved) 'stable process absence permits restore'
$state.Now=[datetime]'2026-09-08T00:00:00Z'
$intermittent={if($state.Now.Second -eq 0 -and $state.Now.Millisecond -ge 750){[pscustomobject]@{Id=54321}}}.GetNewClosure()
$intermittentEvidence=New-TestDirectory 'intermittent-process'
$intermittentResult=Wait-SmokeProcessExitBeforeRecovery -EvidencePath $intermittentEvidence -TimeoutSeconds 1 -ProcessQuery $intermittent -Clock $clock -Sleep $sleep -Close {param($Process) $false}
Assert-Module (-not $intermittentResult.Exited) 'process reappearance resets stable absence'
foreach($file in @('player-save-unchanged-before-cleanup.json','committed-sidecar-unchanged-before-cleanup.json')) {
    Write-SmokeJsonObject -Path (Join-Path $stuckEvidence $file) -Value @{Passed=$true;SaveTestMode='NoNativeSave';ComparedBeforeRunnerOrExternalRestore=$true;RoutineByteBackupCreated=$false;PlayerArchiveWritebackPerformed=$false}
}
Write-SmokeRunnerFailure -EvidencePath $stuckEvidence -Phase 'restore-session' -Reason 'simulated running game' -SaveTestMode NoNativeSave -LaunchAttempted $true | Out-Null
$failed=Get-Content -LiteralPath (Join-Path $stuckEvidence 'result.json') -Raw|ConvertFrom-Json
Assert-Module ($failed.RunStatus -ceq 'Failed' -and $failed.ProcessExited -cne 'Passed' -and $failed.PlayerSaveUnchangedBeforeCleanup -cne 'Passed' -and $failed.StateRestoration -cne 'Passed') 'failed exit never reports NoNativeSave or restoration success'

$assetParent=New-TestDirectory 'assets';$asset=New-TestDirectory 'assets/config';Write-TestText (Join-Path $asset 'settings.json') 'before'
$baseline=New-SmokeDirectoryBaseline -Path $asset -BackupPath (Join-Path $OutputRoot 'config-backup')
Write-TestText (Join-Path $asset 'settings.json') 'changed'
Assert-Module (Restore-SmokeDirectoryBaseline -Baseline $baseline -AllowedRoot $assetParent -EvidencePath (Join-Path $OutputRoot 'config-restored.json')) 'non-save directory restores exact contents'
Write-TestText (Join-Path $asset 'settings.json') 'keep-on-failure'
Write-TestText (Join-Path $baseline.BackupPath 'settings.json') 'corrupted-backup'
Assert-Rejected {Restore-SmokeDirectoryBaseline -Baseline $baseline -AllowedRoot $assetParent -EvidencePath (Join-Path $OutputRoot 'bad-restore.json')} 'corrupted recovery baseline rejected'
Assert-Module ((Get-Content -LiteralPath (Join-Path $asset 'settings.json') -Raw) -ceq 'keep-on-failure') 'failed recovery material leaves target untouched'
Assert-Rejected {Restore-SmokeDirectoryBaseline -Baseline $baseline -AllowedRoot (Join-Path $OutputRoot 'elsewhere') -EvidencePath (Join-Path $OutputRoot 'escape-restore.json')} 'recovery cannot leave allowed root'

$sidecar=Join-Path $OutputRoot 'committed.json';Write-TestText $sidecar 'committed'
$snap=Get-SmokeFileMetadataSnapshot -Kind Product -Path $sidecar
Assert-Module (Compare-SmokeFileMetadataSnapshots -Snapshots @($snap) -EvidencePath (Join-Path $OutputRoot 'sidecar-same.json') -SaveTestMode NoNativeSave) 'committed sidecar unchanged'
$beforeTicks=(Get-Item -LiteralPath $sidecar).LastWriteTimeUtc
[IO.File]::SetLastWriteTimeUtc($sidecar,$beforeTicks.AddSeconds(1))
Assert-Module (-not (Compare-SmokeFileMetadataSnapshots -Snapshots @($snap) -EvidencePath (Join-Path $OutputRoot 'sidecar-changed.json') -SaveTestMode NoNativeSave)) 'same bytes with changed write time fails NoNativeSave'

$sourceEvidence=New-TestDirectory 'source-evidence'
$loadPrefix='Code mod load-source owner=DTMAPI.MoreSavesMod; '
$loadSource='source=Local; workshopId=none; root='+[IO.Path]::GetFullPath($product)+'; dll='+[IO.Path]::GetFullPath((Join-Path $product 'Content/DTMAPI/DTMAPI.MoreSaves.dll'))+'.'
Write-TestText (Join-Path $sourceEvidence 'official-mod-profile-summary.json') '{"Applied":true,"EnabledIds":["Local.DTMAPI_MoreSaves"],"DisabledIds":[]}'
Write-TestText (Join-Path $sourceEvidence 'DTMAPI-latest.log') ($loadPrefix+$loadSource)
Assert-MoreSavesFixed12LoadedSource -Phase $plan[0] -SmokeEvidence $sourceEvidence -ProductRoot $product
Assert-Module $true 'exact Local source accepted'
Write-TestText (Join-Path $sourceEvidence 'DTMAPI-latest.log') ($loadPrefix+'source=Workshop;'+[Environment]::NewLine+'owner=Other; '+$loadSource)
Assert-Rejected {Assert-MoreSavesFixed12LoadedSource -Phase $plan[0] -SmokeEvidence $sourceEvidence -ProductRoot $product} 'owner and expected root on different log lines rejected'
Write-TestText (Join-Path $sourceEvidence 'official-mod-profile-summary.json') '{"Applied":true,"EnabledIds":[],"DisabledIds":["Local.DTMAPI_MoreSaves"]}'
Assert-Rejected {Assert-MoreSavesFixed12LoadedSource -Phase $plan[1] -SmokeEvidence $sourceEvidence -ProductRoot $product} 'disabled phase rejects a product load'
Write-TestText (Join-Path $sourceEvidence 'DTMAPI-latest.log') 'Core only'
Assert-MoreSavesFixed12LoadedSource -Phase $plan[1] -SmokeEvidence $sourceEvidence -ProductRoot $product
Assert-Module $true 'disabled cold source accepted'
$candidateIdentity=Convert-MoreSavesFixed12ProductIdentityToCanonicalJson -Snapshot @(Get-MoreSavesFixed12ProductSnapshot -Root $product)
Write-TestText (Join-Path $product 'Content/DTMAPI/DTMAPI.MoreSaves.dll') 'changed-candidate'
$changedIdentity=Convert-MoreSavesFixed12ProductIdentityToCanonicalJson -Snapshot @(Get-MoreSavesFixed12ProductSnapshot -Root $product)
Assert-Module ($candidateIdentity -cne $changedIdentity) 'changed package bytes invalidate candidate binding'
$lifecycleSave=New-TestDirectory 'lifecycle/SAVE'
foreach($index in 0..5){Write-TestText (Join-Path $lifecycleSave "doloc-save-$index.data") "native-$index"}
$initial=@(Get-MoreSavesFixed12SaveSnapshot -SaveRoot $lifecycleSave)
Write-TestText (Join-Path $lifecycleSave 'doloc-save-6.data') 'created-slot'
Write-TestText (Join-Path $lifecycleSave 'doloc-save-7.data.bak') 'deleted-duplicate'
$current=@(Get-MoreSavesFixed12SaveSnapshot -SaveRoot $lifecycleSave)
Assert-MoreSavesFixed12PostLifecycleState -SaveRoot $lifecycleSave -Initial $initial -Current $current
Assert-Module $true 'lifecycle preserves six sources and native deleted-slot backup role'
Write-TestText (Join-Path $lifecycleSave 'doloc-save-0.data') 'changed-source'
Assert-Rejected {Assert-MoreSavesFixed12PostLifecycleState -SaveRoot $lifecycleSave -Initial $initial -Current @(Get-MoreSavesFixed12SaveSnapshot -SaveRoot $lifecycleSave)} 'lifecycle rejects changed source slot'
Write-TestText (Join-Path $lifecycleSave 'doloc-save-0.data') 'native-0'
Write-TestText (Join-Path $lifecycleSave 'doloc-save-8.data') 'unexpected-extra'
Assert-Rejected {Assert-MoreSavesFixed12PostLifecycleState -SaveRoot $lifecycleSave -Initial $initial -Current @(Get-MoreSavesFixed12SaveSnapshot -SaveRoot $lifecycleSave)} 'lifecycle rejects unrelated extra slot'
$result=[ordered]@{Passed=$true;CheckCount=$checks.Count;Checks=@($checks.ToArray());GameStarted=$false;SharedAssetsTouched=$false}
Write-SmokeJsonObject -Path (Join-Path $OutputRoot 'result.json') -Value $result
Write-Output ('DTMAPI_SMOKE_MODULE_TEST_RESULT='+(Join-Path $OutputRoot 'result.json'))
