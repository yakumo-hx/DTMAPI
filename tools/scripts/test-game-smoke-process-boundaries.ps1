param([string] $OutputRoot = '')
. "$PSScriptRoot/common.ps1"
$ErrorActionPreference = 'Stop'
$repo=Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot=Join-Path $repo ('tmp/test-runs/game-smoke-process-boundaries-'+[guid]::NewGuid().ToString('N'))
}
if(Test-Path -LiteralPath $OutputRoot){throw 'Process boundary tests require a new output directory.'}
New-Item -ItemType Directory -Path $OutputRoot | Out-Null
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
$powershell=Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$utf8=New-Object Text.UTF8Encoding($false)
$checks=New-Object 'Collections.Generic.List[object]'
function Write-ChildScript([string]$Path,[string]$Text){[IO.File]::WriteAllText($Path,$Text,$utf8)}
function Assert-Boundary([bool]$Passed,[string]$Name){if(-not $Passed){throw "Process boundary test failed: $Name"}}
function Invoke-TestChild([string]$Name,[string]$Runner,[hashtable]$Arguments,[string]$HookRoot='') {
    $caseRoot=Join-Path $OutputRoot $Name
    New-Item -ItemType Directory -Path $caseRoot | Out-Null
    $argumentPath=Join-Path $caseRoot 'arguments.json'
    Write-ChildScript $argumentPath ($Arguments|ConvertTo-Json -Depth 6)
    $child=@'
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
$object=Get-Content -LiteralPath $env:DTMAPI_BOUNDARY_ARGUMENTS -Raw|ConvertFrom-Json
$arguments=@{}
foreach($property in $object.PSObject.Properties){$arguments[$property.Name]=$property.Value}
& $env:DTMAPI_BOUNDARY_RUNNER @arguments
if($null -ne $LASTEXITCODE){exit $LASTEXITCODE}
'@
    $start=New-Object Diagnostics.ProcessStartInfo
    $start.FileName=$powershell
    $start.Arguments='-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand '+[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($child))
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    $start.WorkingDirectory=$repo
    $start.EnvironmentVariables['DTMAPI_BOUNDARY_ARGUMENTS']=$argumentPath
    $start.EnvironmentVariables['DTMAPI_BOUNDARY_RUNNER']=$Runner
    $start.EnvironmentVariables['DTMAPI_BOUNDARY_CASE_ROOT']=$caseRoot
    $start.EnvironmentVariables['DTMAPI_BOUNDARY_HOOK_ROOT']=$HookRoot
    [void]$start.EnvironmentVariables.Remove('DTMAPI_DOLOC_PERSISTENT_ROOT')
    [void]$start.EnvironmentVariables.Remove('DTMAPI_STATE_DIR')
    $process=New-Object Diagnostics.Process
    $process.StartInfo=$start
    $watch=[Diagnostics.Stopwatch]::StartNew()
    try {
        [void]$process.Start()
        $stdoutTask=$process.StandardOutput.ReadToEndAsync()
        $stderrTask=$process.StandardError.ReadToEndAsync()
        if(-not $process.WaitForExit(15000)){
            $process.Kill();$process.WaitForExit()
            throw "Owned test child exceeded 15 seconds: $Name"
        }
        $stdout=$stdoutTask.Result;$stderr=$stderrTask.Result
        $code=$process.ExitCode
    }
    finally{$watch.Stop();$process.Dispose()}
    Write-ChildScript (Join-Path $caseRoot 'stdout.txt') $stdout
    Write-ChildScript (Join-Path $caseRoot 'stderr.txt') $stderr
    return [pscustomobject]@{Name=$Name;ExitCode=$code;Stdout=$stdout;Stderr=$stderr;Milliseconds=$watch.ElapsedMilliseconds;Root=$caseRoot}
}
$runner=Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$fixture=Join-Path $OutputRoot 'fixture'
New-Item -ItemType Directory -Path (Join-Path $fixture 'SAVE'),(Join-Path $fixture 'DTMAPI') -Force | Out-Null
Write-ChildScript (Join-Path $fixture '.dtmapi-disposable-save-fixture.json') '{"schemaVersion":1,"disposable":true,"steamAutoCloudIsolated":true}'
$cases=@(
    @{Name='ordinary-projection';Arguments=@{ValidateSaveTestModeOnly=$true}},
    @{Name='manual-title-projection';Arguments=@{ValidateSaveTestModeOnly=$true;WaitForManualExit=$true;SaveSlot=0}},
    @{Name='native-projection';Arguments=@{ValidateSaveTestModeOnly=$true;SaveTestMode='NativeSaveExpected';StageQaHost=$true;DirectExe=$true;DisposableSaveFixtureRoot=$fixture}},
    @{Name='routing-projection';Arguments=@{ValidateQaG5RoutingOnly=$true;StageQaHost=$true;SaveSlot=3;AutoExerciseTitleButtonLifecycle=$true;AutoExerciseMoreEquipmentSlotsNoNativeSave=$true}},
    @{Name='manual-exit-projection';Arguments=@{ValidateQaG3RoutingOnly=$true;StageQaHost=$true;QaObserveSaveLoaded=$true;SaveSlot=1;WaitForManualExit=$true}},
    @{Name='default-qa-exit-projection';Arguments=@{ValidateQaG3RoutingOnly=$true;StageQaHost=$true;QaObserveSaveLoaded=$true;SaveSlot=1}}
)
foreach($case in $cases){
    $run=Invoke-TestChild -Name $case.Name -Runner $runner -Arguments $case.Arguments
    Assert-Boundary ($run.ExitCode -eq 0 -and [string]::IsNullOrWhiteSpace($run.Stderr)) ($case.Name+' clean exit')
    $receipt=$run.Stdout|ConvertFrom-Json
    Assert-Boundary ($null -ne $receipt -and $run.Stdout -notmatch 'DTMAPI_SMOKE_EVIDENCE_PATH=|Build succeeded|Game smoke passed') ($case.Name+' emits only projection')
    if($case.Name -ceq 'manual-exit-projection'){Assert-Boundary ([bool]$receipt.WaitForManualExit -and [bool]$receipt.QaG3SaveLoadedBranchRequested) 'manual route retains SaveLoaded observation'}
    if($case.Name -ceq 'default-qa-exit-projection'){Assert-Boundary (-not [bool]$receipt.WaitForManualExit) 'ordinary QA retains automatic exit'}
    if($case.Name -ceq 'manual-title-projection'){
        Assert-Boundary ($receipt.TitleOnlyManualObservation -and $receipt.WaitForManualExit -and $receipt.SaveSlot -eq 0 -and -not $receipt.NativeSaveRouteRequested -and -not $receipt.ArchiveMutationRouteRequested) 'title manual route has no forced load, native save or archive mutation'
    }
    $checks.Add([ordered]@{Name=$case.Name;Passed=$true;Milliseconds=$run.Milliseconds})
}
foreach($case in @(
    @{Name='missing-fixture';Arguments=@{ValidateSaveTestModeOnly=$true;SaveTestMode='NativeSaveExpected';StageQaHost=$true;DirectExe=$true}},
    @{Name='retired-restore';Arguments=@{ValidateSaveTestModeOnly=$true;RequirePlayerSaveRestore=$true}},
    @{Name='wrong-moresaves-phase';Arguments=@{ValidateSaveTestModeOnly=$true;MoreSavesFixed12AcceptancePhase='EnabledLifecycle';SaveTestMode='NoNativeSave'}},
    @{Name='manual-without-qa';Arguments=@{ValidateQaG3RoutingOnly=$true;WaitForManualExit=$true;QaObserveSaveLoaded=$true;SaveSlot=1}},
    @{Name='manual-without-observation';Arguments=@{ValidateQaG3RoutingOnly=$true;WaitForManualExit=$true;StageQaHost=$true;SaveSlot=1}},
    @{Name='manual-at-title';Arguments=@{ValidateQaG3RoutingOnly=$true;WaitForManualExit=$true;StageQaHost=$true;QaObserveSaveLoaded=$true;SaveSlot=0}}
)){
    $run=Invoke-TestChild -Name $case.Name -Runner $runner -Arguments $case.Arguments
    Assert-Boundary ($run.ExitCode -ne 0 -and $run.Stdout -notmatch 'DTMAPI_SMOKE_EVIDENCE_PATH=|Build succeeded|Game smoke passed') ($case.Name+' preflight stops')
    $checks.Add([ordered]@{Name=$case.Name;Passed=$true;Milliseconds=$run.Milliseconds})
}

# Keep the real CLI, dispatch and try/finally in a private copy. Replace only
# deployment/OS boundaries with traps; even a control-flow regression cannot
# build, deploy or start the shared game while this test investigates it.
$shadow=Join-Path $OutputRoot 'shadow/tools/scripts'
New-Item -ItemType Directory -Path $shadow -Force | Out-Null
foreach($file in @('run-game-smoke.ps1','common.ps1','dump-governance.ps1')){
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $shadow $file)
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'game-smoke') -Destination (Join-Path $shadow 'game-smoke') -Recurse
$shadowRunner=Join-Path $shadow 'run-game-smoke.ps1'
$phases=Join-Path $shadow 'game-smoke/phases'
Write-ChildScript (Join-Path $phases 'prepare-session.ps1') @'
[IO.File]::WriteAllText((Join-Path $env:DTMAPI_BOUNDARY_CASE_ROOT 'forbidden-session-entry.txt'),'reached')
throw 'Forbidden session boundary reached.'
'@
$shadowProjection=Invoke-TestChild -Name 'projection-before-session-trap' -Runner $shadowRunner -Arguments @{ValidateSaveTestModeOnly=$true}
Assert-Boundary ($shadowProjection.ExitCode -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $shadowProjection.Root 'forbidden-session-entry.txt'))) 'projection does not reach session preparation'
$checks.Add([ordered]@{Name='projection-before-session-trap';Passed=$true;Milliseconds=$shadowProjection.Milliseconds})
$shadowFailure=Invoke-TestChild -Name 'preflight-before-session-trap' -Runner $shadowRunner -Arguments @{RequirePlayerSaveRestore=$true}
Assert-Boundary ($shadowFailure.ExitCode -ne 0 -and -not (Test-Path -LiteralPath (Join-Path $shadowFailure.Root 'forbidden-session-entry.txt'))) 'failed preflight does not reach session preparation'
$checks.Add([ordered]@{Name='preflight-before-session-trap';Passed=$true;Milliseconds=$shadowFailure.Milliseconds})

Write-ChildScript (Join-Path $phases 'prepare-session.ps1') @'
$evidence=Join-Path $env:DTMAPI_BOUNDARY_CASE_ROOT 'evidence'
New-Item -ItemType Directory -Path $evidence | Out-Null
'@
Write-ChildScript (Join-Path $phases 'deploy-session.ps1') '# Synthetic deployment boundary: no assets.'
Write-ChildScript (Join-Path $phases 'exercise-session.ps1') "throw 'Synthetic exercise failure.'"
Write-ChildScript (Join-Path $phases 'restore-session.ps1') @'
[IO.File]::WriteAllText((Join-Path $evidence 'restore-entered.txt'),'reached')
throw 'Synthetic restore failure.'
'@
foreach($kind in @('restore-failure','process-still-running')) {
    if($kind -ceq 'process-still-running'){
        Write-ChildScript (Join-Path $phases 'restore-session.ps1') @'
[IO.File]::WriteAllText((Join-Path $evidence 'restore-entered.txt'),'reached')
Write-SmokeJsonObject -Path (Join-Path $evidence 'process-exit-before-state-restore.json') -Value @{Exited=$false;StableAbsenceObserved=$false;RemainingProcessIds=@(12345)}
Write-SmokeJsonObject -Path (Join-Path $evidence 'manual-recovery-required.json') -Value @{Reason='Synthetic still-running process'}
throw 'Synthetic process still running; restoration blocked.'
'@
    }
    $run=Invoke-TestChild -Name $kind -Runner $shadowRunner -Arguments @{}
    $evidenceRoot=Join-Path $run.Root 'evidence'
    Assert-Boundary ($run.ExitCode -ne 0 -and (Test-Path -LiteralPath (Join-Path $evidenceRoot 'restore-entered.txt'))) ($kind+' executes finally and fails child')
    $receipt=Get-Content -LiteralPath (Join-Path $evidenceRoot 'result.json') -Raw|ConvertFrom-Json
    Assert-Boundary ($receipt.RunStatus -ceq 'Failed' -and -not $receipt.GameLaunchAttempted -and $receipt.StateRestoration -cne 'Passed' -and $receipt.ProcessExited -cne 'Passed' -and $receipt.PlayerSaveUnchangedBeforeCleanup -cne 'Passed' -and $receipt.CommittedSidecarsUnchangedBeforeCleanup -cne 'Passed') ($kind+' cannot manufacture success')
    Assert-Boundary (@($run.Stdout -split '\r?\n'|Where-Object {$_ -like 'DTMAPI_SMOKE_EVIDENCE_PATH=*'}).Count -eq 1) ($kind+' emits one failure evidence marker')
    $checks.Add([ordered]@{Name=$kind;Passed=$true;Milliseconds=$run.Milliseconds})
}
# A real owned worker exits normally on a stop file. Observe it before or after
# the deadline, then complete cleanup without killing it. A clean late exit
# must not turn the manual observation into a successful manual exit.
$manualRunner=Join-Path $OutputRoot 'manual-exit-worker-test.ps1'
Write-ChildScript $manualRunner @'
param([string]$Mode,[string]$ModuleRoot)
$ErrorActionPreference='Stop'
. (Join-Path $ModuleRoot 'core/evidence.ps1')
. (Join-Path $ModuleRoot 'core/session.ps1')
$caseRoot=$env:DTMAPI_BOUNDARY_CASE_ROOT
$evidence=Join-Path $caseRoot 'evidence'
New-Item -ItemType Directory -Path $evidence | Out-Null
$stop=Join-Path $caseRoot 'stop-worker'
$ready=Join-Path $caseRoot 'worker-ready'
$workerCode=@"
`$ready=`$env:DTMAPI_MANUAL_WORKER_READY
`$stop=`$env:DTMAPI_MANUAL_WORKER_STOP
[IO.File]::WriteAllText(`$ready,'ready')
while(-not [IO.File]::Exists(`$stop)){[Threading.Thread]::Sleep(10)}
exit 0
"@
$start=New-Object Diagnostics.ProcessStartInfo
$start.FileName=Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$start.Arguments='-NoProfile -NonInteractive -EncodedCommand '+[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($workerCode))
$start.UseShellExecute=$false;$start.CreateNoWindow=$true
$start.EnvironmentVariables['DTMAPI_MANUAL_WORKER_READY']=$ready
$start.EnvironmentVariables['DTMAPI_MANUAL_WORKER_STOP']=$stop
$worker=New-Object Diagnostics.Process
$worker.StartInfo=$start
try {
    [void]$worker.Start()
    $readyDeadline=(Get-Date).AddSeconds(4)
    while(-not (Test-Path -LiteralPath $ready) -and (Get-Date) -lt $readyDeadline){Start-Sleep -Milliseconds 10}
    if(-not (Test-Path -LiteralPath $ready)){throw 'Owned worker did not become ready.'}
    $query={ $worker.Refresh();if(-not $worker.HasExited){$worker} }.GetNewClosure()
    $sleep={param($Milliseconds)
        if($Mode -ceq 'Early' -and -not [IO.File]::Exists($stop)){[IO.File]::WriteAllText($stop,'normal exit')}
        [Threading.Thread]::Sleep($Milliseconds)
    }.GetNewClosure()
    $budget=if($Mode -ceq 'Early'){2000}else{150}
    $observation=Wait-SmokeManualExit -Deadline ((Get-Date).AddMilliseconds($budget)) -ProcessQuery $query -FatalWindowQuery {@()} -Sleep $sleep -PollMilliseconds 25
    Write-SmokeJsonObject -Path (Join-Path $evidence 'manual-exit.json') -Value $observation
    $aliveBeforeCleanup=-not $worker.HasExited
    [IO.File]::WriteAllText($stop,'normal cleanup exit')
    if(-not $worker.WaitForExit(3000) -or $worker.ExitCode -ne 0){throw 'Owned worker did not complete normal cleanup.'}
    $exitReceipt=Wait-SmokeProcessExitBeforeRecovery -EvidencePath $evidence -TimeoutSeconds 2 -ProcessQuery $query -Close {param($Process) throw 'Worker must already be absent.'}
    if(-not $exitReceipt.Exited){throw 'Cleanup did not verify stable process absence.'}
    if($Mode -ceq 'Timeout') {
        if(-not $aliveBeforeCleanup -or $observation.Passed -or -not $observation.TimedOut){throw 'Deadline failure was lost before normal cleanup.'}
        Write-SmokeRunnerFailure -EvidencePath $evidence -Phase 'manual-exit' -Reason $observation.Reason -SaveTestMode NoNativeSave -LaunchAttempted $true | Out-Null
        exit 1
    }
    if(-not $observation.Passed -or $observation.TimedOut){throw 'Normal early exit failed observation.'}
    Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json') -Value @{Passed=$true;ManualExit='Passed';ProcessExited='Passed';ForcedClose=$false}
    exit 0
}
finally {
    if(-not $worker.HasExited){$worker.Kill();$worker.WaitForExit()}
    $worker.Dispose()
}
'@
foreach($mode in @('Early','Timeout')) {
    $run=Invoke-TestChild -Name ('manual-worker-'+$mode.ToLowerInvariant()) -Runner $manualRunner -Arguments @{Mode=$mode;ModuleRoot=(Join-Path $PSScriptRoot 'game-smoke')}
    $receipt=Get-Content -LiteralPath (Join-Path $run.Root 'evidence/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
    if($mode -ceq 'Early') {
        Assert-Boundary ($run.ExitCode -eq 0 -and $receipt.ManualExit -ceq 'Passed' -and $receipt.ProcessExited -ceq 'Passed') 'real worker normal exit before deadline passes manual observation'
    }
    else {
        Assert-Boundary ($run.ExitCode -ne 0 -and $receipt.RunStatus -ceq 'Failed' -and $receipt.ManualExit -ceq 'Failed' -and $receipt.ManualExitTimeout -and $receipt.ManualExitReason -ceq 'ManualExitTimeout' -and $receipt.ProcessExited -ceq 'Passed') 'normal cleanup after manual timeout retains failure and permits verified cleanup'
    }
    $checks.Add([ordered]@{Name=('manual-worker-'+$mode.ToLowerInvariant());Passed=$true;Milliseconds=$run.Milliseconds})
}
$result=[ordered]@{Passed=$true;CheckCount=$checks.Count;Checks=@($checks.ToArray());RealWindowsPowerShellChildren=$true;GameStarted=$false;SharedAssetsTouched=$false}
Write-ChildScript (Join-Path $OutputRoot 'result.json') ($result|ConvertTo-Json -Depth 6)
Write-Output ('DTMAPI_SMOKE_PROCESS_BOUNDARY_TEST_RESULT='+(Join-Path $OutputRoot 'result.json'))
