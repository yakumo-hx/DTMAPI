param()

$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$catalogChecker = Join-Path $PSScriptRoot 'check-product-catalog.ps1'
$catalogPath = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'tools\release\dtmapi-product-catalog.json'

function Assert-NoQaDeadlineTest {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$output = @(& powershell.exe `
    -NoProfile `
    -NonInteractive `
    -ExecutionPolicy Bypass `
    -File $runner `
    -AssertNoQaUiEvidence `
    -AutoDriveNoQaAnimalViewer `
    -ValidateNoQaUiEvidenceGateOnly `
    -OfficialModProfile Published11 `
    -IsolateAllOfficialMods `
    -UseSteam `
    -SkipInstall `
    -SaveSlot 3 2>&1)
$childExitCode = $LASTEXITCODE
$text = @($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
Assert-NoQaDeadlineTest ($childExitCode -eq 0) "No-QA deadline validation child failed with exit code $childExitCode.`n$text"

try {
    $receipt = $text | ConvertFrom-Json
}
catch {
    throw "No-QA deadline validation did not emit one JSON receipt.`n$text`n$([string]$_.Exception.Message)"
}

Assert-NoQaDeadlineTest ([bool]$receipt.Passed) 'No-QA validation aggregate did not pass.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.Passed) 'No-QA deadline self-test did not pass.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.ExpiredAtEntryDidNotExecute) 'Expired-at-entry action executed unexpectedly.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.ShortBudgetDidNotExecute) 'Insufficient short-budget action executed unexpectedly.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.LateCompletionRejected) 'A completion observed after the deadline was accepted.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.OnTimeCompletionAccepted) 'An in-budget completion was not accepted.'
Assert-NoQaDeadlineTest ([int]$receipt.DeadlineSelfTest.CappedWaitMilliseconds -eq 125) 'The configured wait was not capped to the deterministic 125 ms budget.'
Assert-NoQaDeadlineTest ([string]$receipt.RunnerDeadlineScope -eq 'one launch-to-process-wait deadline; startup, GameLaunched, SaveLoaded, UI, and process waits consume only its remaining budget') 'The validation receipt lost the single-deadline scope.'

$runnerSource = [System.IO.File]::ReadAllText($runner)
foreach ($requiredSourceContract in @(
    'NoQaBehaviorCompletedBeforeDeadline',
    'Wait-ForLogLineAfterOffsetUntilDeadline',
    'Invoke-SmokeDeadlineGuardedAction',
    '-Deadline $noQaUiDeadline'
)) {
    Assert-NoQaDeadlineTest ($runnerSource.Contains($requiredSourceContract)) "Runner source is missing deadline contract: $requiredSourceContract"
}

$catalog = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$batch5CurrentTree = $catalog.playerRuntimePackageInvariant.ordinaryNoQaAcceptance.batch5CurrentTree
Assert-NoQaDeadlineTest ([string]$batch5CurrentTree.status -eq 'passed-current-local11-exact-tree') 'Catalog did not promote the corrected-runner current Local11 exact-tree pass.'
Assert-NoQaDeadlineTest ([string]$batch5CurrentTree.receipt -eq 'docs/debug/evidence/GAME-SMOKE/20260719-214048/result.json') 'Catalog lost the corrected-runner current Local11 receipt.'
Assert-NoQaDeadlineTest ([string]$batch5CurrentTree.requiredCurrentReceiptGate -eq 'NoQaBehaviorCompletedBeforeDeadline') 'Catalog lost the corrected-runner current receipt gate.'
Assert-NoQaDeadlineTest (@($batch5CurrentTree.supersededPassingReceipts).Count -eq 1) 'Catalog must classify exactly one pre-deadline-gate passing receipt as superseded passing evidence.'
Assert-NoQaDeadlineTest ([string]$batch5CurrentTree.supersededPassingReceipts[0] -eq 'docs/debug/evidence/GAME-SMOKE/20260719-034102/result.json') 'Catalog lost the retained 034102 superseded passing receipt.'
Assert-NoQaDeadlineTest (@($batch5CurrentTree.supersededFailedReceipts).Count -eq 2) 'Catalog must retain the two superseded failed product receipts.'
Assert-NoQaDeadlineTest (@($batch5CurrentTree.supersededFailedReceipts) -contains 'docs/debug/evidence/GAME-SMOKE/20260718-190150/result.json') 'Catalog lost superseded failed receipt 190150.'
Assert-NoQaDeadlineTest (@($batch5CurrentTree.supersededFailedReceipts) -contains 'docs/debug/evidence/GAME-SMOKE/20260718-192244/result.json') 'Catalog lost superseded failed receipt 192244.'
Assert-NoQaDeadlineTest (@($batch5CurrentTree.infrastructureTimeoutReceipts).Count -eq 2) 'Catalog must retain both pre-SaveLoaded infrastructure/operator-handshake timeouts.'
Assert-NoQaDeadlineTest (@($batch5CurrentTree.infrastructureTimeoutReceipts) -contains 'docs/debug/evidence/GAME-SMOKE/20260719-130406/result.json') 'Catalog lost the 130406 infrastructure-timeout receipt.'
Assert-NoQaDeadlineTest (@($batch5CurrentTree.infrastructureTimeoutReceipts) -contains 'docs/debug/evidence/GAME-SMOKE/20260719-212913/result.json') 'Catalog lost the 212913 operator-handshake-timeout receipt.'

$catalogOutput = @(& powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $catalogChecker -Quiet 2>&1)
$catalogExitCode = $LASTEXITCODE
Assert-NoQaDeadlineTest ($catalogExitCode -eq 0) "Catalog checker rejected the truthful passed-current state.`n$(@($catalogOutput) -join [Environment]::NewLine)"

$catalogCheckerSource = [System.IO.File]::ReadAllText($catalogChecker, [System.Text.Encoding]::UTF8)
foreach ($requiredCatalogContract in @(
    "'replay-required', 'passed-current-local11-exact-tree'",
    "Get-ObjectValue `$batch5NoQaCurrentTree 'receipt'",
    "Current Batch 5 ordinary no-QA completed-before-deadline gate",
    "Get-ObjectValue `$batch5NoQaResult 'NoQaBehaviorCompletedBeforeDeadline'"
)) {
    Assert-NoQaDeadlineTest ($catalogCheckerSource.Contains($requiredCatalogContract)) "Catalog checker source is missing ordinary no-QA state contract: $requiredCatalogContract"
}
Assert-NoQaDeadlineTest (-not $catalogCheckerSource.Contains("-Expected 'docs/debug/evidence/GAME-SMOKE/20260719-034102/result.json'")) 'Catalog checker still hard-codes 034102 as the current passing receipt.'

Write-Host 'test-noqa-deadline: OK'
