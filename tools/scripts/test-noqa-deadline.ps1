param()

$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'run-game-smoke.ps1'


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
    -Issue011Acceptance `
    -AutoDriveNoQaAnimalViewer `
    -ValidateNoQaUiEvidenceGateOnly `
    -OfficialModProfile Published11 `
    -IsolateAllOfficialMods `
    -UseSteam `
    -SkipInstall `
    -SaveSlot 10 2>&1)
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
Assert-NoQaDeadlineTest ([bool]$receipt.SelfTest.AbsentDirectoryReceiptAccepted) 'No-QA directory receipt did not preserve the absent 0/0 array shape.'
Assert-NoQaDeadlineTest ([int]$receipt.SelfTest.AbsentDirectoryCount -eq 0 -and [int]$receipt.SelfTest.AbsentFileCount -eq 0) 'No-QA absent directory receipt returned a non-zero directory/file count.'
Assert-NoQaDeadlineTest ([bool]$receipt.Issue011Acceptance) 'No-QA validation projection lost the ISSUE-011 acceptance route.'
Assert-NoQaDeadlineTest ([bool]$receipt.Issue011ProjectionSelfTest.Passed) 'ISSUE-011 projection positive/negative self-test failed.'
Assert-NoQaDeadlineTest ([bool]$receipt.Issue011ProjectionSelfTest.PositivePassed) 'ISSUE-011 projection did not accept the exact fresh positive fixture.'
Assert-NoQaDeadlineTest ([bool]$receipt.Issue011ProjectionSelfTest.RepeatedInputRejected) 'ISSUE-011 projection accepted repeated Input System/fallback failures.'
Assert-NoQaDeadlineTest ([bool]$receipt.Issue011ProjectionSelfTest.StaleLastGiveRejected) 'ISSUE-011 projection accepted a stale last-give breadcrumb.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.Passed) 'No-QA deadline self-test did not pass.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.ExpiredAtEntryDidNotExecute) 'Expired-at-entry action executed unexpectedly.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.ShortBudgetDidNotExecute) 'Insufficient short-budget action executed unexpectedly.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.LateCompletionRejected) 'A completion observed after the deadline was accepted.'
Assert-NoQaDeadlineTest ([bool]$receipt.DeadlineSelfTest.OnTimeCompletionAccepted) 'An in-budget completion was not accepted.'
Assert-NoQaDeadlineTest ([int]$receipt.DeadlineSelfTest.CappedWaitMilliseconds -eq 125) 'The configured wait was not capped to the deterministic 125 ms budget.'
Assert-NoQaDeadlineTest ([string]$receipt.RunnerDeadlineScope -eq 'one launch-to-process-wait deadline; startup, GameLaunched, SaveLoaded, UI, and process waits consume only its remaining budget') 'The validation receipt lost the single-deadline scope.'

# Catalog evidence is owned by check-product-catalog; this test owns live runner behavior.
Write-Host 'test-noqa-deadline: OK'
