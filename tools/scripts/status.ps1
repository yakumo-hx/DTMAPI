. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
# Status is read-only: Get-DotNetExe can download/install a missing toolchain.
$dotnet = $null
$dotnetCandidates = @((Join-Path $repo '.tools\dotnet\dotnet.exe'))
$systemDotnet = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
if ($systemDotnet) { $dotnetCandidates += $systemDotnet.Source }
foreach ($candidate in ($dotnetCandidates | Select-Object -Unique)) {
    if (Test-DtmApiDotNet8Toolchain -Path $candidate) {
        $dotnet = $candidate
        break
    }
}

Write-Host "Repo: $repo"
if ($dotnet) {
    Write-Host "dotnet: $dotnet"
    & $dotnet --list-sdks
}
else {
    Write-Host 'dotnet: compatible SDK/.NET 8 not found; build/test commands can provision it when needed.'
}
$gameDir = $null
try { $gameDir = Resolve-DolocTownGamePath -RepoRoot $repo -AllowMissing }
catch { Write-Host "Game path unavailable: $($_.Exception.Message)" }
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
