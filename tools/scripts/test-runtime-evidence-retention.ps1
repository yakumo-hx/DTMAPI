Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"

function Assert-DtmRuntimeEvidenceTest {
    param([bool] $Condition, [string] $Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-DtmRuntimeEvidenceThrows {
    param([scriptblock] $Action, [string] $Message)

    $threw = $false
    try {
        & $Action
    }
    catch {
        $threw = $true
    }
    Assert-DtmRuntimeEvidenceTest $threw $Message
}

$previousAllowlistTestMode = $env:DTMAPI_EVIDENCE_RETENTION_ALLOWLIST_TEST_MODE
try {
    $env:DTMAPI_EVIDENCE_RETENTION_ALLOWLIST_TEST_MODE = '1'
    . (Join-Path $PSScriptRoot 'build-evidence-retention-allowlist.ps1') -LibraryOnly
}
finally {
    $env:DTMAPI_EVIDENCE_RETENTION_ALLOWLIST_TEST_MODE = $previousAllowlistTestMode
}

$cleanupScriptPath = Join-Path $PSScriptRoot 'cleanup-duplicate-runtime-evidence.ps1'
$cleanupTokens = $null
$cleanupParseErrors = $null
$cleanupAst = [System.Management.Automation.Language.Parser]::ParseFile($cleanupScriptPath, [ref]$cleanupTokens, [ref]$cleanupParseErrors)
Assert-DtmRuntimeEvidenceTest (@($cleanupParseErrors).Count -eq 0) 'Duplicate-evidence cleanup script must parse before its durable-root consumer can be tested.'
foreach ($functionName in @('Get-TextSha256', 'Get-SortedSetFingerprint', 'Get-DurableRootSnapshot')) {
    $functionAst = $cleanupAst.Find({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $functionName
    }, $true)
    Assert-DtmRuntimeEvidenceTest ($null -ne $functionAst) "Duplicate-evidence cleanup function is missing: $functionName"
    Invoke-Expression $functionAst.Extent.Text
}

$rootReferences = @(Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text @'
`docs/debug/evidence/BATCH5-GC-LADDER/20260719-105157-ed51cf27/AutoFishing-L0/stage.json`
`docs/debug/evidence/BATCH5-NO-DEMAND/formal-final-qa-vitals-20260719-2000/result.json`
`docs/debug/evidence/BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260721-010101-a1b2c3d4/manager-lifecycle.json`
`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260721-020202-b2c3d4e5/behavior-matrix.json`
`docs/debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-030303-c3d4e5f6/ladder-plan.json`
`docs/debug/evidence/PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r14/auto-fishing/final-validator-reevaluation.json`
`docs\debug\evidence\CANDIDATE11\candidate-20260719-final\transaction.json`
`docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260719-041012/Results/stress-summary.md`
`docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/20260715-batch2-055-candidate/DTMAPI Workshop Audit 20260715-153835/Results/stress-summary.md`
`docs/debug/evidence/BATCH5-GC-LADDER/<run-id>/stage.json`
Candidate11/Local11 acceptance is a release concept, not an evidence path.
'@)
Assert-DtmRuntimeEvidenceTest ($rootReferences.Count -eq 9) 'Durable evidence parser should retain nine concrete roots and ignore the placeholder root.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[0].Root -eq 'BATCH5-GC-LADDER/20260719-105157-ed51cf27') 'Batch 5 GC root was not canonicalized.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[1].Root -eq 'BATCH5-NO-DEMAND/formal-final-qa-vitals-20260719-2000') 'Batch 5 no-demand root was not canonicalized.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[2].Root -eq 'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260721-010101-a1b2c3d4') 'Batch 6 AutoFishing Manager lifecycle root was not canonicalized.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[3].Root -eq 'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260721-020202-b2c3d4e5') 'Batch 6 AutoFishing behavior-matrix root was not canonicalized.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[4].Root -eq 'BATCH6-AUTOFISHING-GC-LADDER/20260721-030303-c3d4e5f6') 'Batch 6 AutoFishing GC-ladder root was not canonicalized.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[5].Root -eq 'PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r14') 'Pre-release active-GC root was not canonicalized.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[6].Root -eq 'CANDIDATE11/candidate-20260719-final') 'Candidate11 root was not canonicalized from backslashes.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[7].Root -eq 'WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260719-041012') 'Workshop audit root with spaces was truncated.'
Assert-DtmRuntimeEvidenceTest ($rootReferences[8].Root -eq 'WORKSHOP-SUBSCRIPTION-AUDIT/20260715-batch2-055-candidate') 'Nested Workshop audit evidence should retain its first-level evidence root.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/BATCH5-GC-LADDER/../outside`' } 'Traversal references must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/BATCH5-GC-LADDER/run/../`' } 'Trailing traversal references must fail before punctuation normalization.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/CANDIDATE11/C:/outside`' } 'Rooted path references must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/CANDIDATE11/NUL/receipt.json`' } 'Reserved Windows device roots must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/run/../../outside`' } 'Nested traversal references must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/BATCH5-NO-DEMAND/../outside`' } 'No-demand traversal references must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/../outside`' } 'Batch 6 Manager lifecycle traversal references must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/C:/outside`' } 'Batch 6 behavior-matrix rooted references must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/run//summary.json`' } 'Batch 6 behavior-matrix empty path segments must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/NUL/stage.json`' } 'Batch 6 GC-ladder reserved-device roots must fail closed.'
Assert-DtmRuntimeEvidenceThrows { Get-DtmDurableEvidenceRootReferencesFromText -SourcePath '<test>' -Text '`docs/debug/evidence/PRERELEASE-ACTIVE-GC/../outside`' } 'Pre-release active-GC traversal references must fail closed.'

$smokeScript = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'run-game-smoke.ps1'))
Assert-DtmRuntimeEvidenceTest (-not [regex]::IsMatch($smokeScript, '(?m)collect-logs\.ps1.*-IncludeRuntimeEvidence')) 'Routine GAME-SMOKE must not request a full runtime evidence tree.'
Assert-DtmRuntimeEvidenceTest ([regex]::IsMatch($smokeScript, '(?m)collect-logs\.ps1.*-RuntimeEvidenceSinceUtc')) 'Routine GAME-SMOKE must pass its current-run evidence boundary.'
$formalOuterContracts = @(
    [pscustomobject]@{ Script = 'run-batch6-autofishing-manager-lifecycle.ps1'; Category = 'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE'; Summary = 'manager-lifecycle.json' },
    [pscustomobject]@{ Script = 'run-batch6-autofishing-behavior-matrix.ps1'; Category = 'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX'; Summary = 'behavior-matrix.json' },
    [pscustomobject]@{ Script = 'run-batch6-autofishing-gc-ladder.ps1'; Category = 'BATCH6-AUTOFISHING-GC-LADDER'; Summary = 'ladder-plan.json' }
)
foreach ($contract in $formalOuterContracts) {
    $runnerText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot ([string]$contract.Script)))
    $expectedRoot = 'docs\debug\evidence\' + [string]$contract.Category + '\'
    Assert-DtmRuntimeEvidenceTest ($runnerText.Contains($expectedRoot)) "Formal outer runner $($contract.Script) does not default to its durable evidence category $($contract.Category)."
    Assert-DtmRuntimeEvidenceTest ($runnerText.Contains([string]$contract.Summary)) "Formal outer runner $($contract.Script) does not write its root summary $($contract.Summary)."
}
$moreEquipmentColdRunner =
    [System.IO.File]::ReadAllText((
        Join-Path $PSScriptRoot `
            'run-moreequipment-cold-recovery-acceptance.ps1'))
Assert-DtmRuntimeEvidenceTest (
    $moreEquipmentColdRunner.Contains(
        '$phaseResults[-1].SmokeEvidence') -and
    $moreEquipmentColdRunner.Contains(
        "Join-Path `$finalSmokeEvidence 'cold-recovery-acceptance.json'") -and
    $moreEquipmentColdRunner.Contains(
        "`$receipt['DurableReceipt']")) `
    'The MoreEquipmentSlots outer runner must copy its completed aggregate receipt into the final GAME-SMOKE evidence root.'

$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("dtmapi-runtime-evidence-test-" + [guid]::NewGuid().ToString('N'))
try {
    $source = Join-Path $testRoot 'source'
    $destination = Join-Path $testRoot 'destination'
    $summary = Join-Path $testRoot 'selection.txt'
    $since = [datetime]::UtcNow.AddMinutes(-5)

    $durableFixtureRoot = Join-Path $testRoot 'durable-evidence'
    $durableFixtureIdentities = @(
        'BATCH5-GC-LADDER/gc-run',
        'BATCH5-NO-DEMAND/no-demand-run',
        'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/manager-run',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/behavior-run',
        'BATCH6-AUTOFISHING-GC-LADDER/gc-ladder-run',
        'PRERELEASE-ACTIVE-GC/prerelease-run',
        'CANDIDATE11/candidate-run',
        'WORKSHOP-SUBSCRIPTION-AUDIT/workshop run'
    )
    foreach ($identity in $durableFixtureIdentities) {
        $fixturePath = Join-Path $durableFixtureRoot ($identity.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        New-Item -ItemType Directory -Force -Path $fixturePath | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $fixturePath 'receipt.txt'), $identity)
    }
    $durableFixtureAllowlist = [pscustomobject]@{
        batch5GcLadderRoots = @('BATCH5-GC-LADDER/gc-run')
        batch5NoDemandRoots = @('BATCH5-NO-DEMAND/no-demand-run')
        batch6AutoFishingManagerLifecycleRoots = @('BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/manager-run')
        batch6AutoFishingBehaviorMatrixRoots = @('BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/behavior-run')
        batch6AutoFishingGcLadderRoots = @('BATCH6-AUTOFISHING-GC-LADDER/gc-ladder-run')
        prereleaseActiveGcRoots = @('PRERELEASE-ACTIVE-GC/prerelease-run')
        candidate11Roots = @('CANDIDATE11/candidate-run')
        workshopSubscriptionAuditRoots = @('WORKSHOP-SUBSCRIPTION-AUDIT/workshop run')
    }
    $durableFixtureSnapshot = Get-DurableRootSnapshot -Allowlist $durableFixtureAllowlist -EvidenceRoot $durableFixtureRoot
    Assert-DtmRuntimeEvidenceTest ($durableFixtureSnapshot.roots -eq 8) 'Cleanup durable-root consumer must preserve all eight category roots.'
    Assert-DtmRuntimeEvidenceTest ($durableFixtureSnapshot.files -eq 8) 'Cleanup durable-root consumer must fingerprint every file in every indivisible root.'
    Assert-DtmRuntimeEvidenceTest (-not [string]::IsNullOrWhiteSpace([string]$durableFixtureSnapshot.fileManifestSha256)) 'Cleanup durable-root consumer must produce a complete file-manifest fingerprint.'
    $missingDurableFixtureAllowlist = [pscustomobject]@{
        batch5GcLadderRoots = @()
        batch5NoDemandRoots = @()
        batch6AutoFishingManagerLifecycleRoots = @('BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/missing-run')
        batch6AutoFishingBehaviorMatrixRoots = @()
        batch6AutoFishingGcLadderRoots = @()
        prereleaseActiveGcRoots = @()
        candidate11Roots = @()
        workshopSubscriptionAuditRoots = @()
    }
    Assert-DtmRuntimeEvidenceThrows { Get-DurableRootSnapshot -Allowlist $missingDurableFixtureAllowlist -EvidenceRoot $durableFixtureRoot } 'Cleanup durable-root consumer must fail closed when an allowlisted root is missing.'
    $nestedDurableFixtureAllowlist = [pscustomobject]@{
        batch5GcLadderRoots = @()
        batch5NoDemandRoots = @()
        batch6AutoFishingManagerLifecycleRoots = @()
        batch6AutoFishingBehaviorMatrixRoots = @('BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/behavior-run/nested')
        batch6AutoFishingGcLadderRoots = @()
        prereleaseActiveGcRoots = @()
        candidate11Roots = @()
        workshopSubscriptionAuditRoots = @()
    }
    Assert-DtmRuntimeEvidenceThrows { Get-DurableRootSnapshot -Allowlist $nestedDurableFixtureAllowlist -EvidenceRoot $durableFixtureRoot } 'Cleanup durable-root consumer must reject a Batch 6 identity that is not one indivisible first-level root.'
    $wrongCategoryFixtureAllowlist = [pscustomobject]@{
        batch5GcLadderRoots = @()
        batch5NoDemandRoots = @()
        batch6AutoFishingManagerLifecycleRoots = @('BATCH6-AUTOFISHING-GC-LADDER/gc-ladder-run')
        batch6AutoFishingBehaviorMatrixRoots = @()
        batch6AutoFishingGcLadderRoots = @()
        prereleaseActiveGcRoots = @()
        candidate11Roots = @()
        workshopSubscriptionAuditRoots = @()
    }
    Assert-DtmRuntimeEvidenceThrows { Get-DurableRootSnapshot -Allowlist $wrongCategoryFixtureAllowlist -EvidenceRoot $durableFixtureRoot } 'Cleanup durable-root consumer must reject a Batch 6 root placed under the wrong allowlist property.'
    if ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT) {
        $reparseTarget = Join-Path $testRoot 'durable-reparse-target'
        $reparseRoot = Join-Path $durableFixtureRoot 'BATCH6-AUTOFISHING-GC-LADDER\reparse-run'
        New-Item -ItemType Directory -Force -Path $reparseTarget | Out-Null
        try {
            New-Item -ItemType Junction -Path $reparseRoot -Target $reparseTarget | Out-Null
            $reparseDurableFixtureAllowlist = [pscustomobject]@{
                batch5GcLadderRoots = @()
                batch5NoDemandRoots = @()
                batch6AutoFishingManagerLifecycleRoots = @()
                batch6AutoFishingBehaviorMatrixRoots = @()
                batch6AutoFishingGcLadderRoots = @('BATCH6-AUTOFISHING-GC-LADDER/reparse-run')
                prereleaseActiveGcRoots = @()
                candidate11Roots = @()
                workshopSubscriptionAuditRoots = @()
            }
            Assert-DtmRuntimeEvidenceThrows { Get-DurableRootSnapshot -Allowlist $reparseDurableFixtureAllowlist -EvidenceRoot $durableFixtureRoot } 'Cleanup durable-root consumer must fail closed when an allowlisted root is a junction/reparse point.'
        }
        finally {
            if ([System.IO.Directory]::Exists($reparseRoot)) {
                [System.IO.Directory]::Delete($reparseRoot)
            }
        }
    }
    $oldRun = Join-Path $source 'CASE-A\20260712-000000'
    $newRun = Join-Path $source 'CASE-B\20260712-120000'
    New-Item -ItemType Directory -Force -Path $oldRun, $newRun | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $oldRun 'old.txt'), 'old')
    [System.IO.File]::WriteAllText((Join-Path $newRun 'new.txt'), 'new')
    New-Item -ItemType Directory -Force -Path (Join-Path $newRun 'nested') | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $newRun 'nested\child.txt'), 'child')
    $oldItem = Get-Item -LiteralPath $oldRun
    $oldItem.CreationTimeUtc = $since.AddMinutes(-10)
    $oldItem.LastWriteTimeUtc = $since.AddMinutes(-10)

    $copied = Copy-DtmRuntimeEvidenceWindow -SourceRoot $source -DestinationRoot $destination -SinceUtc $since -SummaryPath $summary -MaxTotalBytes 1024 -MaxFileBytes 512 -MaxFiles 10 -MaxDirectories 10
    Assert-DtmRuntimeEvidenceTest ($copied.Status -eq 'copied') 'Fresh runtime evidence should be copied.'
    Assert-DtmRuntimeEvidenceTest ($copied.DirectoryCount -eq 1) 'Only one fresh runtime evidence directory should be selected.'
    Assert-DtmRuntimeEvidenceTest (Test-Path -LiteralPath (Join-Path $destination 'CASE-B\20260712-120000\new.txt')) 'Fresh runtime evidence file is missing.'
    Assert-DtmRuntimeEvidenceTest (Test-Path -LiteralPath (Join-Path $destination 'CASE-B\20260712-120000\nested\child.txt')) 'Nested runtime evidence file is missing.'
    Assert-DtmRuntimeEvidenceTest (-not (Test-Path -LiteralPath (Join-Path $destination 'CASE-A\20260712-000000\old.txt'))) 'Old runtime evidence must not be copied.'
    $copiedAgain = Copy-DtmRuntimeEvidenceWindow -SourceRoot $source -DestinationRoot $destination -SinceUtc $since -SummaryPath $summary -MaxTotalBytes 1024 -MaxFileBytes 512 -MaxFiles 10 -MaxDirectories 10
    Assert-DtmRuntimeEvidenceTest ($copiedAgain.Status -eq 'copied') 'Repeated current-run collection should remain valid.'
    Assert-DtmRuntimeEvidenceTest (-not (Test-Path -LiteralPath (Join-Path $destination 'CASE-B\20260712-120000\nested\nested\child.txt'))) 'Repeated collection must not nest an existing evidence directory.'

    $blockedDestination = Join-Path $testRoot 'blocked-destination'
    $blockedSummary = Join-Path $testRoot 'blocked-selection.txt'
    $blocked = Copy-DtmRuntimeEvidenceWindow -SourceRoot $source -DestinationRoot $blockedDestination -SinceUtc $since -SummaryPath $blockedSummary -MaxTotalBytes 2 -MaxFileBytes 2 -MaxFiles 10 -MaxDirectories 10
    Assert-DtmRuntimeEvidenceTest ($blocked.Status -eq 'blocked-by-limit') 'Over-limit runtime evidence should be rejected as one batch.'
    Assert-DtmRuntimeEvidenceTest (-not (Test-Path -LiteralPath (Join-Path $blockedDestination 'CASE-B\20260712-120000\new.txt'))) 'Blocked runtime evidence must not be partially copied.'
    Assert-DtmRuntimeEvidenceTest ((Get-Content -Raw -LiteralPath $blockedSummary) -match 'Status=blocked-by-limit') 'Blocked selection summary is missing its status.'

    $allowlistOutput = Join-Path $testRoot 'evidence-retention-allowlist.json'
    & (Join-Path $PSScriptRoot 'build-evidence-retention-allowlist.ps1') -OutputPath $allowlistOutput
    & (Join-Path $PSScriptRoot 'build-evidence-retention-allowlist.ps1') -OutputPath $allowlistOutput -Check
    $allowlist = Get-Content -LiteralPath $allowlistOutput -Raw | ConvertFrom-Json
    Assert-DtmRuntimeEvidenceTest ($allowlist.schemaVersion -eq 5) 'Generated evidence allowlist should use schema version 5.'
    Assert-DtmRuntimeEvidenceTest ($null -ne $allowlist.batch5GcLadderRoots) 'Generated evidence allowlist is missing Batch 5 GC roots.'
    Assert-DtmRuntimeEvidenceTest ($null -ne $allowlist.batch5NoDemandRoots) 'Generated evidence allowlist is missing Batch 5 no-demand roots.'
    foreach ($propertyName in @(
        'batch6AutoFishingManagerLifecycleRoots',
        'batch6AutoFishingBehaviorMatrixRoots',
        'batch6AutoFishingGcLadderRoots',
        'prereleaseActiveGcRoots')) {
        Assert-DtmRuntimeEvidenceTest ($allowlist.PSObject.Properties.Name -contains $propertyName) "Generated evidence allowlist is missing $propertyName."
        Assert-DtmRuntimeEvidenceTest ($allowlist.counts.PSObject.Properties.Name -contains $propertyName) "Generated evidence allowlist counts are missing $propertyName."
        Assert-DtmRuntimeEvidenceTest ([int]$allowlist.counts.$propertyName -eq @($allowlist.$propertyName).Count) "Generated evidence allowlist count does not match $propertyName."
    }
    Assert-DtmRuntimeEvidenceTest ($null -ne $allowlist.candidate11Roots) 'Generated evidence allowlist is missing Candidate11 roots.'
    Assert-DtmRuntimeEvidenceTest ($null -ne $allowlist.workshopSubscriptionAuditRoots) 'Generated evidence allowlist is missing Workshop subscription audit roots.'
    Assert-DtmRuntimeEvidenceTest (
        @($allowlist.gameSmokeArtifacts) -contains
            'GAME-SMOKE/20260806-015320/cold-recovery-acceptance.json') `
        'Generated evidence allowlist is missing the durable MoreEquipmentSlots aggregate receipt copied into the final GAME-SMOKE run.'
    $expectedGcRoots = @(
        'BATCH5-GC-LADDER/20260719-041457-94325d1a',
        'BATCH5-GC-LADDER/20260719-044157-70ce39e6',
        'BATCH5-GC-LADDER/20260719-101706-1f2dfe53',
        'BATCH5-GC-LADDER/20260719-115313-0b93d47f',
        'BATCH5-GC-LADDER/formal-autofishing-vitals250-final-20260719-2014'
    )
    $expectedNoDemandRoots = @(
        'BATCH5-NO-DEMAND/formal-final-qa-vitals-20260719-2000',
        'BATCH5-NO-DEMAND/prerelease-055-candidate-20260728-r3',
        'BATCH5-NO-DEMAND/prerelease-055-candidate-20260728-r4'
    )
    $expectedBatch6ManagerRoots = @(
        'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260721-121144-ded2b298',
        'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260804-204034-fa6e7198',
        'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260804-210301-d880655d'
    )
    $expectedBatch6BehaviorRoots = @(
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260721-122112-c46597d6',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-095013-55b4a76f',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-100952-70e21c42',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-125535-5e02fea6',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-132752-3ece549c',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-140417-97255f77',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-143231-db9bc829',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-151549-6fdf0608',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-170252-b9909791',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-173611-724358a3',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-194749-2c4b332c',
        'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-201455-47ff8a41'
    )
    $expectedBatch6GcLadderRoots = @(
        'BATCH6-AUTOFISHING-GC-LADDER/20260721-132436-b7e23d5f',
        'BATCH6-AUTOFISHING-GC-LADDER/20260721-152146-c761b08d-l4-only',
        'BATCH6-AUTOFISHING-GC-LADDER/20260721-155340-d24150aa-l5-only',
        'BATCH6-AUTOFISHING-GC-LADDER/20260804-175508-27232cc8',
        'BATCH6-AUTOFISHING-GC-LADDER/20260804-191406-a2ba87b2-l5-reentry-diagnostic'
    )
    $expectedPrereleaseActiveGcRoots = @(
        'PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r14',
        'PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r15'
    )
    $expectedCandidateRoots = @(
        'CANDIDATE11/batch5-final-manual-handshake-20260719-214044',
        'CANDIDATE11/issue011-566467f0-20260806-r6'
    )
    $expectedWorkshopRoots = @(
        'WORKSHOP-SUBSCRIPTION-AUDIT/20260715-batch2-055-candidate',
        'WORKSHOP-SUBSCRIPTION-AUDIT/20260719-1848-batch5-final',
        'WORKSHOP-SUBSCRIPTION-AUDIT/DTMAPI Workshop Audit 20260713-124155'
    )
    $requiredSmokeRoots = @(
        'GAME-SMOKE/20260719-031553',
        'GAME-SMOKE/20260719-130406',
        'GAME-SMOKE/20260719-195806',
        'GAME-SMOKE/20260719-212635',
        'GAME-SMOKE/20260719-212758',
        'GAME-SMOKE/20260719-212913',
        'GAME-SMOKE/20260719-214048',
        'GAME-SMOKE/20260720-025016',
        'GAME-SMOKE/20260728-230231',
        'GAME-SMOKE/20260728-230430',
        'GAME-SMOKE/20260728-230535',
        'GAME-SMOKE/20260804-173631',
        'GAME-SMOKE/20260804-173737',
        'GAME-SMOKE/20260804-173832',
        'GAME-SMOKE/20260804-173924',
        'GAME-SMOKE/20260804-174024',
        'GAME-SMOKE/20260804-174116',
        'GAME-SMOKE/20260804-174207',
        'GAME-SMOKE/20260804-174302',
        'GAME-SMOKE/20260804-185030',
        'GAME-SMOKE/20260804-191409',
        'GAME-SMOKE/20260804-194752',
        'GAME-SMOKE/20260804-194857',
        'GAME-SMOKE/20260804-194958',
        'GAME-SMOKE/20260804-195058',
        'GAME-SMOKE/20260804-201457',
        'GAME-SMOKE/20260804-201612',
        'GAME-SMOKE/20260804-201715',
        'GAME-SMOKE/20260804-201818',
        'GAME-SMOKE/20260804-201925',
        'GAME-SMOKE/20260804-202025',
        'GAME-SMOKE/20260804-202131',
        'GAME-SMOKE/20260804-202239',
        'GAME-SMOKE/20260804-202514',
        'GAME-SMOKE/20260804-204036',
        'GAME-SMOKE/20260804-210304',
        'GAME-SMOKE/20260804-210420',
        'GAME-SMOKE/20260804-210517',
        'GAME-SMOKE/20260804-212108',
        'GAME-SMOKE/20260804-212423',
        'GAME-SMOKE/20260804-212602',
        'GAME-SMOKE/20260804-215633',
        'GAME-SMOKE/20260804-220123',
        'GAME-SMOKE/20260806-004228',
        'GAME-SMOKE/20260806-004555',
        'GAME-SMOKE/20260806-004819',
        'GAME-SMOKE/20260806-005111',
        'GAME-SMOKE/20260806-015140',
        'GAME-SMOKE/20260806-015232',
        'GAME-SMOKE/20260806-015320',
        'GAME-SMOKE/20260806-080739'
    )
    $expectedDurableRootSourceFiles = @(
        'docs/debug/regressions/smoke-matrix-history-20260728.md',
        'docs/debug/regressions/smoke-matrix.md',
        'docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md',
        'docs/reviews/code/2026/20260719-0002-batch5-gc-ladder-save-loaded-evidence-path-runner-failure.md',
        'docs/reviews/code/2026/20260719-0005-batch5-autofishing-native-session-normal-state-stall.md',
        'docs/reviews/code/2026/20260719-0006-batch5-autofishing-native-control-no-water-spawn.md',
        'docs/reviews/code/2026/20260719-0009-batch5-autofishing-performance-observer-effect.md',
        'docs/reviews/code/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md',
        'docs/reviews/code/2026/20260804-0010-autofishing-fifth-save-fixture-availability-review.md',
        'docs/reviews/code/2026/20260804-0012-autofishing-facing-preflight-neutral-handshake-review.md',
        'docs/reviews/code/2026/20260804-0014-autofishing-direct-neutral-load-contact-race-review.md',
        'docs/reviews/code/2026/20260804-0015-autofishing-l5-reentry-direct-neutral-refresh-race-review.md',
        'docs/reviews/code/2026/20260804-0016-autofishing-behavior-initial-nonproduct-cast-race-review.md',
        'docs/reviews/code/2026/20260804-0017-autofishing-manager-unrelated-equipmentslots-cleanup-fanout-review.md',
        'docs/updates/2026/20260713-0009-workshop-subscription-and-prerelease-baselines.md',
        'docs/updates/2026/20260715-0010-runtime-upgrade-transaction.md',
        'docs/updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md',
        'docs/updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md',
        'docs/updates/2026/20260727-0001-dtmapi-055-prerelease-route.md',
        'docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md',
        'tools/release/dtmapi-product-catalog.json'
    )
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedGcRoots -DifferenceObject @($allowlist.batch5GcLadderRoots)).Count -eq 0 -and @($allowlist.batch5GcLadderRoots).Count -eq $expectedGcRoots.Count) 'Generated allowlist Batch 5 GC root set is not exact.'
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedNoDemandRoots -DifferenceObject @($allowlist.batch5NoDemandRoots)).Count -eq 0 -and @($allowlist.batch5NoDemandRoots).Count -eq $expectedNoDemandRoots.Count) 'Generated allowlist Batch 5 no-demand root set is not exact.'
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedBatch6ManagerRoots -DifferenceObject @($allowlist.batch6AutoFishingManagerLifecycleRoots)).Count -eq 0 -and @($allowlist.batch6AutoFishingManagerLifecycleRoots).Count -eq $expectedBatch6ManagerRoots.Count) 'Generated allowlist Batch 6 AutoFishing Manager lifecycle root set is not exact.'
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedBatch6BehaviorRoots -DifferenceObject @($allowlist.batch6AutoFishingBehaviorMatrixRoots)).Count -eq 0 -and @($allowlist.batch6AutoFishingBehaviorMatrixRoots).Count -eq $expectedBatch6BehaviorRoots.Count) 'Generated allowlist Batch 6 AutoFishing behavior-matrix root set is not exact.'
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedBatch6GcLadderRoots -DifferenceObject @($allowlist.batch6AutoFishingGcLadderRoots)).Count -eq 0 -and @($allowlist.batch6AutoFishingGcLadderRoots).Count -eq $expectedBatch6GcLadderRoots.Count) 'Generated allowlist Batch 6 AutoFishing GC-ladder root set is not exact.'
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedPrereleaseActiveGcRoots -DifferenceObject @($allowlist.prereleaseActiveGcRoots)).Count -eq 0 -and @($allowlist.prereleaseActiveGcRoots).Count -eq $expectedPrereleaseActiveGcRoots.Count) 'Generated allowlist pre-release active-GC root set is not exact.'
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedCandidateRoots -DifferenceObject @($allowlist.candidate11Roots)).Count -eq 0 -and @($allowlist.candidate11Roots).Count -eq $expectedCandidateRoots.Count) 'Generated allowlist Candidate11 root set is not exact.'
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedWorkshopRoots -DifferenceObject @($allowlist.workshopSubscriptionAuditRoots)).Count -eq 0 -and @($allowlist.workshopSubscriptionAuditRoots).Count -eq $expectedWorkshopRoots.Count) 'Generated allowlist Workshop subscription-audit root set is not exact.'
    foreach ($requiredSmokeRoot in $requiredSmokeRoots) {
        Assert-DtmRuntimeEvidenceTest (@($allowlist.gameSmokeRuns) -contains $requiredSmokeRoot) "Generated allowlist is missing required smoke identity $requiredSmokeRoot."
    }
    Assert-DtmRuntimeEvidenceTest (@(Compare-Object -ReferenceObject $expectedDurableRootSourceFiles -DifferenceObject @($allowlist.durableRootSourceFiles)).Count -eq 0 -and @($allowlist.durableRootSourceFiles).Count -eq $expectedDurableRootSourceFiles.Count) 'Generated allowlist durable-root source file set is not exact or the Catalog JSON source is missing.'
    Assert-DtmRuntimeEvidenceTest (@($allowlist.sourceFiles) -contains 'tools/release/dtmapi-product-catalog.json') 'Generated allowlist did not scan the Catalog JSON source.'

    $cleanupScript = [System.IO.File]::ReadAllText($cleanupScriptPath)
    Assert-DtmRuntimeEvidenceTest ([regex]::IsMatch($cleanupScript, 'Get-DurableRootSnapshot')) 'Duplicate-evidence cleanup must snapshot indivisible durable roots.'
    foreach ($propertyName in @(
        'batch5GcLadderRoots',
        'batch5NoDemandRoots',
        'batch6AutoFishingManagerLifecycleRoots',
        'batch6AutoFishingBehaviorMatrixRoots',
        'batch6AutoFishingGcLadderRoots',
        'prereleaseActiveGcRoots',
        'candidate11Roots',
        'workshopSubscriptionAuditRoots')) {
        Assert-DtmRuntimeEvidenceTest ($cleanupScript.IndexOf($propertyName, [System.StringComparison]::Ordinal) -ge 0) "Duplicate-evidence cleanup does not consume durable-root property $propertyName."
    }
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}

Write-Host 'Runtime evidence retention tests: OK'
