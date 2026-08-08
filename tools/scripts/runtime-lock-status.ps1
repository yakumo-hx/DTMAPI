. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot

$info = Get-DtmApiRuntimeLockInfo -RepoRoot $repo
Write-Host (Format-DtmApiRuntimeLockInfo -LockInfo $info)
if ($info.Exists) {
    Write-Host "Path: $($info.Path)"
}
