. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Retired {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $Action,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $message = ''
    try { & $Action | Out-Null }
    catch { $message = [string]$_.Exception.Message }
    Assert-True ($message -like '*retired*restart*<game>/Mods*') `
        "$Label did not fail at the restart-only retired boundary. actual=$message"
}

$outerRunner = Join-Path $PSScriptRoot 'run-batch6-autofishing-manager-lifecycle.ps1'
$smokeRunner = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$outerSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $outerRunner
$smokeSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $smokeRunner

$outerGuard = $outerSource.IndexOf("throw 'Batch 6 AutoFishing same-process Manager lifecycle is retired", [System.StringComparison]::Ordinal)
$outerFirstMutation = $outerSource.IndexOf('New-Item -ItemType Directory -Path $OutputRoot', [System.StringComparison]::Ordinal)
Assert-True ($outerGuard -ge 0 -and $outerFirstMutation -gt $outerGuard) `
    'The historical outer runner must fail before creating evidence, acquiring the Runtime lock, deploying a package, or launching the game.'

$smokeGuard = $smokeSource.IndexOf("throw '-Batch6AutoFishingManagerLifecycle is retired", [System.StringComparison]::Ordinal)
$smokeLegacyPath = $smokeSource.IndexOf("Join-Path `$gameDir 'Mods\Yuuka.DTMAPI.AutoFishing'", [System.StringComparison]::Ordinal)
$smokeMarkerWrite = $smokeSource.IndexOf('[System.IO.File]::WriteAllBytes($batch6ManagerMarkerPath', [System.StringComparison]::Ordinal)
Assert-True ($smokeGuard -ge 0 -and $smokeLegacyPath -gt $smokeGuard -and $smokeMarkerWrite -gt $smokeGuard) `
    'The common smoke runner must reject the retired lane before any historical <game>/Mods path or marker mutation is evaluated.'

Assert-Retired -Label 'Historical outer runner' -Action {
    & $outerRunner -PlanOnly
}
Assert-Retired -Label 'Common smoke runner route' -Action {
    & $smokeRunner -StageQaHost -SaveSlot 5 -Batch6AutoFishingManagerLifecycle -ValidateQaG6RoutingOnly
}

Write-Host 'Batch 6 AutoFishing Manager lifecycle retirement tests passed.'
