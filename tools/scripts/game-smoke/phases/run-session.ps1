try {
    $SmokeRunnerActivePhase = 'deploy-session'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/deploy-session.ps1')
    if ($null -ne $SmokeRunnerExitCode) { return }
    $SmokeRunnerActivePhase = 'exercise-session'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/exercise-session.ps1')
}
catch {
    $smokeBodyFailure = [string]$_.Exception.ToString()
    $smokeBodyFailure | Set-Content -LiteralPath (Join-Path $evidence 'runner-body-failure.txt')
    throw
}
finally {
    $SmokeRunnerActivePhase = 'restore-session'
    . (Join-Path $SmokeRunnerModuleRoot 'phases/restore-session.ps1')
}
