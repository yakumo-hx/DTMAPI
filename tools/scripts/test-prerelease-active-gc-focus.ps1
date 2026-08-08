param(
    [string] $ChildFixturePath = '',
    [string] $ChildExitPoint = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\run-prerelease-active-gc-focus.ps1" -LibraryOnly
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-True {
    param([Parameter(Mandatory = $true)] [bool] $Condition, [Parameter(Mandatory = $true)] [string] $Message)
    if (-not $Condition) { throw $Message }
}

function New-TestJournal {
    param(
        [Parameter(Mandatory = $true)] [string] $UniqueId,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $Destination,
        [Parameter(Mandatory = $true)] [string] $PackageSha,
        [int] $RecoveryCount = 0
    )

    $artifacts = @(
        for ($index = 0; $index -lt $RecoveryCount; $index++) {
            [ordered]@{ role='previous-version'; path=(Join-Path $GameDir ("retained-$index")); treeSha256=('A' * 64) }
        }
    )
    return [ordered]@{
        schemaVersion = 3
        uniqueId = $UniqueId
        packageKind = 'CodeMod'
        codeModKind = 'Advanced'
        status = 'Installed'
        gameRoot = [System.IO.Path]::GetFullPath($GameDir)
        destinationPath = [System.IO.Path]::GetFullPath($Destination)
        active = $null
        localInstall = $null
        committed = [ordered]@{ packageSha256=$PackageSha; transactionId=[Guid]::NewGuid().ToString('N') }
        recoveryArtifacts = $artifacts
    }
}

function New-LeaseFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $game = Join-Path $Root ("game-$Name")
    $state = Split-Path -Parent (Get-Batch5GcAuthorSourceStatePath -GameDir $game)
    $receipt = Join-Path $Root ("receipt-$Name")
    $uniqueId = 'Yuuka.DTMAPI.ActionSpeed'
    $destination = Join-Path (Join-Path $game 'Mods') $uniqueId
    $journal = Join-Path (Join-Path $state 'deployments') ($uniqueId + '.journal.json')
    [System.IO.Directory]::CreateDirectory($destination) | Out-Null
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $journal)) | Out-Null
    [System.IO.Directory]::CreateDirectory($receipt) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $destination 'original.bin'), "original-$Name", [System.Text.Encoding]::UTF8)
    $originalPackage = '1' * 64
    Write-PrereleaseGcAtomicJson -Path $journal -Value (New-TestJournal -UniqueId $uniqueId -GameDir $game `
        -Destination $destination -PackageSha $originalPackage -RecoveryCount 2)
    $candidateSource = Join-Path $Root ("candidate-source-$Name")
    $candidatePackage = Join-Path $Root ("candidate-$Name.zip")
    [System.IO.Directory]::CreateDirectory($candidateSource) | Out-Null
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText((Join-Path $candidateSource 'candidate.bin'), 'candidate', $encoding)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($candidateSource, $candidatePackage)
    $candidatePackageSha = Get-PrereleaseGcFileSha256 -Path $candidatePackage
    $spec = [pscustomobject]@{
        Domain='ActionSpeed'
        CatalogId='action-speed'
        UniqueId=$uniqueId
        PackagePath=$candidatePackage
        PackageSha256=$candidatePackageSha
        PackageEntries=@(Get-PrereleaseGcPackageEntries -PackagePath $candidatePackage)
        EntryDllSha256=('3' * 64)
        DestinationPath=$destination
        JournalPath=$journal
    }
    $originalTree = Get-Candidate11TreeSnapshot -Path $destination -Context "$Name original"
    $originalJournal = Get-PrereleaseGcFileIdentity -Path $journal
    return [pscustomobject]@{
        GameDir=$game
        StateRoot=$state
        ReceiptRoot=$receipt
        Destination=$destination
        Journal=$journal
        Spec=$spec
        OriginalTree=$originalTree
        OriginalJournal=$originalJournal
    }
}

function New-TestCandidateAction {
    param([switch] $ThrowAfterWrite)

    return {
        param($spec, $state)
        $encoding = New-Object System.Text.UTF8Encoding($false)
        [System.IO.Directory]::CreateDirectory([string]$spec.DestinationPath) | Out-Null
        [System.IO.File]::WriteAllText((Join-Path ([string]$spec.DestinationPath) 'candidate.bin'), 'candidate', $encoding)
        $receipt = [ordered]@{
            schemaVersion=2
            uniqueId=[string]$spec.UniqueId
            packageKind='CodeMod'
            codeModKind='Advanced'
            destinationRelativePath=('Mods/' + [string]$spec.UniqueId)
            packageSha256=[string]$spec.PackageSha256
        }
        [System.IO.File]::WriteAllText(
            (Join-Path ([string]$spec.DestinationPath) '.dtmapi-author-receipt.json'),
            (($receipt | ConvertTo-Json -Depth 20) + "`n"),
            $encoding)
        $journal = [ordered]@{
            schemaVersion=3
            uniqueId=[string]$spec.UniqueId
            packageKind='CodeMod'
            codeModKind='Advanced'
            status='Installed'
            gameRoot=[System.IO.Path]::GetFullPath([string]$state.GameDir)
            destinationPath=[System.IO.Path]::GetFullPath([string]$spec.DestinationPath)
            active=$null
            localInstall=$null
            committed=[ordered]@{ packageSha256=[string]$spec.PackageSha256; transactionId=[Guid]::NewGuid().ToString('N') }
            recoveryArtifacts=@()
        }
        [System.IO.File]::WriteAllText(
            [string]$spec.JournalPath,
            (($journal | ConvertTo-Json -Depth 20) + "`n"),
            $encoding)
        if ($ThrowAfterWrite) { throw 'injected candidate publication failure' }
    }.GetNewClosure()
}

if (-not [string]::IsNullOrWhiteSpace($ChildFixturePath)) {
    if ([string]::IsNullOrWhiteSpace($ChildExitPoint)) {
        throw 'Child process mode requires -ChildExitPoint.'
    }
    $descriptor = Read-PrereleaseGcJson -Path $ChildFixturePath
    $fixture = New-LeaseFixture -Root ([string]$descriptor.Root) -Name ([string]$descriptor.Name)
    Write-PrereleaseGcAtomicJson -Path $ChildFixturePath -Value ([ordered]@{
        Root = [string]$descriptor.Root
        Name = [string]$descriptor.Name
        Id = [string]$descriptor.Id
        StatePath = Join-Path $fixture.ReceiptRoot 'action-speed-lease.json'
        GameDir = [string]$fixture.GameDir
        Destination = [string]$fixture.Destination
        Journal = [string]$fixture.Journal
        ReceiptRoot = [string]$fixture.ReceiptRoot
    })
    $startExitPoints = @(
        'after-original-destination-move',
        'after-original-journal-move',
        'after-candidate-publish')
    if ($ChildExitPoint -in $startExitPoints) {
        $env:DTMAPI_PRERELEASE_GC_TEST_EXIT_POINT = $ChildExitPoint
        $null = Start-PrereleaseGcProductLease -Spec $fixture.Spec -GameDir $fixture.GameDir -SdkExe 'unused' `
            -Id ([string]$descriptor.Id) -ReceiptRoot $fixture.ReceiptRoot -TestDeployAction (New-TestCandidateAction)
    }
    else {
        $env:DTMAPI_PRERELEASE_GC_TEST_EXIT_POINT = ''
        $lease = Start-PrereleaseGcProductLease -Spec $fixture.Spec -GameDir $fixture.GameDir -SdkExe 'unused' `
            -Id ([string]$descriptor.Id) -ReceiptRoot $fixture.ReceiptRoot -TestDeployAction (New-TestCandidateAction)
        $env:DTMAPI_PRERELEASE_GC_TEST_EXIT_POINT = $ChildExitPoint
        $null = Complete-PrereleaseGcProductLease -Lease $lease -GameDir $fixture.GameDir -SdkExe 'unused' `
            -ReceiptRoot $fixture.ReceiptRoot
    }
    throw "Child process did not terminate at the requested point: $ChildExitPoint"
}

$repo = Get-RepoRoot
$testRoot = Join-Path $repo ('temp\test-prerelease-active-gc-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($testRoot) | Out-Null
$authorStateRootBefore = $env:DTMAPI_AUTHOR_STATE_ROOT
$env:DTMAPI_AUTHOR_STATE_ROOT = Join-Path $testRoot 'author-state'
try {
    $focusedRunnerText = [System.IO.File]::ReadAllText(
        (Join-Path $PSScriptRoot 'run-prerelease-active-gc-focus.ps1'),
        [System.Text.Encoding]::UTF8)
    Assert-True -Condition ($focusedRunnerText.Contains('[int] $AutoFishingWarmupFish = 5') -and
        $focusedRunnerText.Contains('[int] $AutoFishingTargetFish = 10') -and
        $focusedRunnerText.Contains('requires at least 5 warm-up fish and 10 measured fish') -and
        $focusedRunnerText.Contains('[switch] $RecoverOnly') -and
        $focusedRunnerText.Contains('recovery requires the interrupted Runtime lock to belong to this worktree') -and
        $focusedRunnerText.Contains('RecoveredAfterInterruptedRun')) `
        -Message 'The focused runtime wrapper must default to and enforce the AutoFishing LongRun QA minimum.'

    $normal = New-LeaseFixture -Root $testRoot -Name 'normal'
    $normalLease = Start-PrereleaseGcProductLease -Spec $normal.Spec -GameDir $normal.GameDir -SdkExe 'unused' `
        -Id 'normal' -ReceiptRoot $normal.ReceiptRoot -TestDeployAction (New-TestCandidateAction)
    Assert-True -Condition (Test-Path -LiteralPath (Join-Path $normal.Destination 'candidate.bin') -PathType Leaf) `
        -Message 'Normal fixture did not activate the candidate.'
    $normalRestore = Complete-PrereleaseGcProductLease -Lease $normalLease -GameDir $normal.GameDir -SdkExe 'unused' -ReceiptRoot $normal.ReceiptRoot
    Assert-True -Condition ([bool]$normalRestore.RestoredExact) -Message 'Normal fixture did not report exact restore.'
    Assert-True -Condition (Test-Candidate11SnapshotsEqual -Expected $normal.OriginalTree -Actual (
        Get-Candidate11TreeSnapshot -Path $normal.Destination -Context 'normal restored')) -Message 'Normal fixture original tree drifted.'
    Assert-True -Condition (Test-PrereleaseGcFileIdentity -Expected $normal.OriginalJournal -Path $normal.Journal) `
        -Message 'Normal fixture original journal drifted.'
    $normalJournal = Read-PrereleaseGcJson -Path $normal.Journal
    Assert-True -Condition (@($normalJournal.recoveryArtifacts).Count -eq 2) `
        -Message 'Normal fixture changed the pre-existing recovery-artifact ledger.'

    $failed = New-LeaseFixture -Root $testRoot -Name 'apply-failure'
    $failedObserved = $false
    try {
        $null = Start-PrereleaseGcProductLease -Spec $failed.Spec -GameDir $failed.GameDir -SdkExe 'unused' `
            -Id 'apply-failure' -ReceiptRoot $failed.ReceiptRoot -TestDeployAction (New-TestCandidateAction -ThrowAfterWrite)
    }
    catch {
        $failedObserved = $_.Exception.Message -match 'injected candidate publication failure'
    }
    Assert-True -Condition $failedObserved -Message 'Injected candidate failure was not propagated.'
    Assert-True -Condition (Test-Candidate11SnapshotsEqual -Expected $failed.OriginalTree -Actual (
        Get-Candidate11TreeSnapshot -Path $failed.Destination -Context 'apply-failure restored')) `
        -Message 'Apply failure did not restore the exact original tree.'
    Assert-True -Condition (Test-PrereleaseGcFileIdentity -Expected $failed.OriginalJournal -Path $failed.Journal) `
        -Message 'Apply failure did not restore the exact original journal.'

    $tamper = New-LeaseFixture -Root $testRoot -Name 'tamper'
    $tamperLease = Start-PrereleaseGcProductLease -Spec $tamper.Spec -GameDir $tamper.GameDir -SdkExe 'unused' `
        -Id 'tamper' -ReceiptRoot $tamper.ReceiptRoot -TestDeployAction (New-TestCandidateAction)
    [System.IO.File]::AppendAllText((Join-Path $tamper.Destination 'candidate.bin'), '-tampered', [System.Text.Encoding]::UTF8)
    $tamperRefused = $false
    try {
        $null = Complete-PrereleaseGcProductLease -Lease $tamperLease -GameDir $tamper.GameDir -SdkExe 'unused' -ReceiptRoot $tamper.ReceiptRoot
    }
    catch {
        $tamperRefused = $_.Exception.Message -match 'no longer matches the exact run-owned candidate'
    }
    Assert-True -Condition $tamperRefused -Message 'Tampered candidate was not rejected fail-closed.'
    Assert-True -Condition (Test-Path -LiteralPath ([string]$tamperLease.State.OriginalDestinationBackup) -PathType Container) `
        -Message 'Tamper refusal did not retain the exact original tree backup.'
    Remove-Item -LiteralPath $tamper.Destination -Recurse -Force
    Remove-Item -LiteralPath $tamper.Journal -Force
    Move-Item -LiteralPath ([string]$tamperLease.State.OriginalDestinationBackup) -Destination $tamper.Destination
    Move-Item -LiteralPath ([string]$tamperLease.State.OriginalJournalBackup) -Destination $tamper.Journal

    $processExitPoints = @(
        'after-original-destination-move',
        'after-original-journal-move',
        'after-candidate-publish',
        'after-candidate-destination-preserve',
        'after-candidate-journal-preserve',
        'after-original-destination-restore',
        'after-original-journal-restore')
    $windowsPowerShell = Get-DtmApiPowerShellHost -RequireWindowsPowerShell
    foreach ($exitPoint in $processExitPoints) {
        $caseName = $exitPoint.Replace('after-', '').Replace('-', '_')
        $caseRoot = Join-Path $testRoot ("process-$caseName")
        [System.IO.Directory]::CreateDirectory($caseRoot) | Out-Null
        $descriptorPath = Join-Path $caseRoot 'child-fixture.json'
        Write-PrereleaseGcAtomicJson -Path $descriptorPath -Value ([ordered]@{
            Root = $caseRoot
            Name = $caseName
            Id = "process-$caseName"
        })
        $childOutput = @(
            & $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
                -ChildFixturePath $descriptorPath -ChildExitPoint $exitPoint 2>&1
        )
        $childExitCode = $LASTEXITCODE
        Assert-True -Condition ($childExitCode -eq 197) `
            -Message "Process interruption $exitPoint returned $childExitCode instead of 197. $([string]::Join(' | ', @($childOutput)))"
        $childFixture = Read-PrereleaseGcJson -Path $descriptorPath
        $interruptedState = Read-PrereleaseGcJson -Path ([string]$childFixture.StatePath)
        $restore = Recover-PrereleaseGcProductLease -StatePath ([string]$childFixture.StatePath) `
            -GameDir ([string]$childFixture.GameDir) -SdkExe 'unused' -ReceiptRoot ([string]$childFixture.ReceiptRoot)
        Assert-True -Condition ([bool]$restore.RestoredExact) `
            -Message "Process interruption $exitPoint did not recover exactly."
        $restoredTree = Get-Candidate11TreeSnapshot -Path ([string]$childFixture.Destination) `
            -Context "$exitPoint recovered original" -AllowAlternateDataStreams
        Assert-True -Condition (Test-Candidate11SnapshotsEqual -Expected $interruptedState.OriginalSnapshot -Actual $restoredTree) `
            -Message "Process interruption $exitPoint changed the original product tree."
        Assert-True -Condition (Test-PrereleaseGcFileIdentity -Expected $interruptedState.OriginalJournalIdentity `
            -Path ([string]$childFixture.Journal)) -Message "Process interruption $exitPoint changed the original journal."
        $restoredJournal = Read-PrereleaseGcJson -Path ([string]$childFixture.Journal)
        Assert-True -Condition (@($restoredJournal.recoveryArtifacts).Count -eq 2) `
            -Message "Process interruption $exitPoint changed the original recovery-artifact ledger."
        foreach ($backupPath in @(
            [string]$interruptedState.OriginalDestinationBackup,
            [string]$interruptedState.CandidateDestinationBackup,
            [string]$interruptedState.OriginalJournalBackup,
            [string]$interruptedState.CandidateJournalBackup)) {
            Assert-True -Condition (-not (Test-Path -LiteralPath $backupPath)) `
                -Message "Process interruption $exitPoint left a lease backup: $backupPath"
        }
    }

    $isolationGame = Join-Path $testRoot 'isolation-game'
    $isolationMods = Join-Path $isolationGame 'Mods'
    $targetId = 'Yuuka.DTMAPI.ActionSpeed'
    $createdId = 'Yuuka.DTMAPI.AutoFishing'
    $preexistingId = 'DTMAPI.MoreSavesMod'
    foreach ($id in @($targetId,$createdId,$preexistingId)) {
        $productRoot = Join-Path $isolationMods $id
        [System.IO.Directory]::CreateDirectory($productRoot) | Out-Null
        Write-PrereleaseGcAtomicJson -Path (Join-Path $productRoot '.dtmapi-author-receipt.json') -Value ([ordered]@{
            schemaVersion=2
            uniqueId=$id
            packageKind='CodeMod'
            codeModKind='Advanced'
            destinationRelativePath=('Mods/' + $id)
        })
    }
    $preexistingMarker = Join-Path (Join-Path $isolationMods $preexistingId) 'dtmapi.disabled'
    [System.IO.File]::WriteAllText($preexistingMarker, 'pre-existing', [System.Text.Encoding]::UTF8)
    $preexistingIdentity = Get-PrereleaseGcFileIdentity -Path $preexistingMarker
    $isolationReceiptRoot = Join-Path $testRoot 'isolation-receipts'
    $isolationLease = Start-PrereleaseGcManagedIsolation -GameDir $isolationGame -EnabledUniqueId $targetId `
        -Id 'isolation' -ReceiptRoot $isolationReceiptRoot
    Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path (Join-Path $isolationMods $targetId) 'dtmapi.disabled')) -and
        (Test-Path -LiteralPath (Join-Path (Join-Path $isolationMods $createdId) 'dtmapi.disabled') -PathType Leaf)) `
        -Message 'Managed isolation did not preserve the target while disabling another managed product.'
    Complete-PrereleaseGcManagedIsolation -Lease $isolationLease -GameDir $isolationGame
    Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path (Join-Path $isolationMods $createdId) 'dtmapi.disabled'))) `
        -Message 'Managed isolation did not remove its exact run-created marker.'
    Assert-True -Condition (Test-PrereleaseGcFileIdentity -Expected $preexistingIdentity -Path $preexistingMarker) `
        -Message 'Managed isolation did not preserve a pre-existing disabled marker exactly.'

    $invalidIsolationGame = Join-Path $testRoot 'invalid-isolation-game'
    $invalidMods = Join-Path $invalidIsolationGame 'Mods'
    foreach ($id in @($targetId,$createdId,'ZZZ.Invalid.Managed')) {
        $productRoot = Join-Path $invalidMods $id
        [System.IO.Directory]::CreateDirectory($productRoot) | Out-Null
        Write-PrereleaseGcAtomicJson -Path (Join-Path $productRoot '.dtmapi-author-receipt.json') -Value ([ordered]@{
            schemaVersion=2
            uniqueId=$(if ($id -ceq 'ZZZ.Invalid.Managed') { 'Different.UniqueId' } else { $id })
            packageKind='CodeMod'
            codeModKind='Advanced'
            destinationRelativePath=('Mods/' + $id)
        })
    }
    $invalidIsolationRejected = $false
    try {
        $null = Start-PrereleaseGcManagedIsolation -GameDir $invalidIsolationGame -EnabledUniqueId $targetId `
            -Id 'invalid-isolation' -ReceiptRoot (Join-Path $testRoot 'invalid-isolation-receipts')
    }
    catch { $invalidIsolationRejected = $_.Exception.Message -match 'receipt identity drifted' }
    Assert-True -Condition $invalidIsolationRejected -Message 'Malformed managed isolation receipt was not rejected.'
    Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path (Join-Path $invalidMods $createdId) 'dtmapi.disabled'))) `
        -Message 'Managed isolation mutated markers before completing receipt preflight.'

    $actionPlanRoot = Join-Path $testRoot 'action-plan'
    & "$PSScriptRoot\run-batch5-gc-ladder.ps1" -Domain ActionSpeed -PlanOnly -OutputRoot $actionPlanRoot `
        -StageIds $script:PrereleaseGcActionSpeedStages
    if (-not $?) { throw 'Focused ActionSpeed plan generation failed.' }
    $actionPlan = Read-PrereleaseGcJson -Path (Join-Path $actionPlanRoot 'ladder-plan.json')
    Assert-True -Condition ([int]$actionPlan.StageCount -eq 8) -Message 'Focused ActionSpeed plan did not retain exactly eight stages.'
    Assert-True -Condition (@(Get-ChildItem -LiteralPath $actionPlanRoot -Directory).Count -eq 8) `
        -Message 'Focused ActionSpeed plan emitted unselected stage directories.'

    $fishingPlanRoot = Join-Path $testRoot 'fishing-plan'
    & "$PSScriptRoot\run-batch6-autofishing-gc-ladder.ps1" -PlanOnly -OutputRoot $fishingPlanRoot `
        -PilotOutputRoot (Join-Path $repo 'temp\prerelease-step5-product-auto-fishing') `
        -FocusedLevels $script:PrereleaseGcAutoFishingLevels
    if (-not $?) { throw 'Focused AutoFishing plan generation failed.' }
    $fishingPlan = Read-PrereleaseGcJson -Path (Join-Path $fishingPlanRoot 'ladder-plan.json')
    Assert-True -Condition ([int]$fishingPlan.StageCount -eq 4) -Message 'Focused AutoFishing plan did not retain exactly four levels.'

    $bindingRoot = Join-Path $testRoot 'binding'
    & "$PSScriptRoot\run-batch6-autofishing-gc-ladder.ps1" -ValidateOnly -OutputRoot $bindingRoot `
        -PilotOutputRoot (Join-Path $repo 'temp\prerelease-step5-product-auto-fishing') `
        -AuthorSdkOutputRoot (Join-Path $repo 'temp\prerelease-step5-author-sdk')
    if (-not $?) { throw 'Separated AutoFishing product/Author SDK binding validation failed.' }
    $binding = Read-PrereleaseGcJson -Path (Join-Path $bindingRoot 'artifact-validation.json')
    Assert-True -Condition ([string]$binding.Status -ceq 'Passed') -Message 'Separated Author SDK binding did not pass.'

    Write-Host 'Prerelease active-GC focused transaction tests: PASS'
}
finally {
    $env:DTMAPI_AUTHOR_STATE_ROOT = $authorStateRootBefore
    if (Test-Path -LiteralPath $testRoot -PathType Container) {
        $resolvedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
        $resolvedTempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp')).TrimEnd([char]92, [char]47)
        if (-not $resolvedTestRoot.StartsWith($resolvedTempRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an out-of-bound fixture root: $resolvedTestRoot"
        }
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
