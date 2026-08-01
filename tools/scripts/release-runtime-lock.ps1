param(
    [switch] $Force
)

. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot

$result = Release-DtmApiRuntimeLock -RepoRoot $repo -Force:$Force
Write-Host $result.Message
