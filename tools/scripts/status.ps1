. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo -AllowMissing

Write-Host "Repo: $repo"
Write-Host "dotnet: $dotnet"
& $dotnet --list-sdks
Write-Host "GameDir: $gameDir"
$runtimeLock = Get-DtmApiRuntimeLockInfo -RepoRoot $repo
Write-Host (Format-DtmApiRuntimeLockInfo -LockInfo $runtimeLock)
if ($gameDir) {
    $dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
    $dtmLog = Join-Path $dtmapiDir 'logs\latest.log'
    $bepLog = Join-Path $gameDir 'BepInEx\LogOutput.log'
    Write-Host "DtmApiStateDir: $dtmapiDir"
    Write-Host "DTMAPI log exists: $(Test-Path $dtmLog)"
    Write-Host "BepInEx log exists: $(Test-Path $bepLog)"
}
Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, Path, StartTime
