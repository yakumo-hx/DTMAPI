param(
    [string] $Reason = '',
    [string] $Owner = '',
    [int] $TimeoutSeconds = 3600,
    [int] $PollSeconds = 5,
    [switch] $AllowCurrentWorktreeReuse
)

. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot

$lock = Wait-DtmApiRuntimeLock `
    -RepoRoot $repo `
    -Reason $Reason `
    -Owner $Owner `
    -TimeoutSeconds $TimeoutSeconds `
    -PollSeconds $PollSeconds `
    -AllowCurrentWorktreeReuse:$AllowCurrentWorktreeReuse

Write-Host $lock.Message
Write-Host "Owner: $((Get-DtmApiObjectProperty -Object $lock.Data -Name 'Owner' -Default ''))"
Write-Host "Branch: $((Get-DtmApiObjectProperty -Object $lock.Data -Name 'Branch' -Default ''))"
Write-Host "Commit: $((Get-DtmApiObjectProperty -Object $lock.Data -Name 'Commit' -Default ''))"
Write-Host "Worktree: $((Get-DtmApiObjectProperty -Object $lock.Data -Name 'Worktree' -Default ''))"
Write-Host "Reason: $((Get-DtmApiObjectProperty -Object $lock.Data -Name 'Reason' -Default ''))"
