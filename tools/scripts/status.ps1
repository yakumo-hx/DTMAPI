. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo -AllowMissing

Write-Host "Repo: $repo"
Write-Host "dotnet: $dotnet"
& $dotnet --list-sdks
Write-Host "GameDir: $gameDir"
if ($gameDir) {
    $dtmLog = Join-Path $gameDir 'DTMAPI\logs\latest.log'
    $bepLog = Join-Path $gameDir 'BepInEx\LogOutput.log'
    Write-Host "DTMAPI log exists: $(Test-Path $dtmLog)"
    Write-Host "BepInEx log exists: $(Test-Path $bepLog)"
}
Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, Path, StartTime
