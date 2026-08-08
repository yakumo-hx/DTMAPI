param(
    [Parameter(Mandatory = $true)] [string] $Reason,
    [Parameter(Mandatory = $true)] [string] $FilePath,
    [string[]] $ArgumentList = @(),
    [int] $TimeoutSeconds = 3600,
    [int] $PollSeconds = 5
)

. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot

$lock = Wait-DtmApiRuntimeLock `
    -RepoRoot $repo `
    -Reason $Reason `
    -TimeoutSeconds $TimeoutSeconds `
    -PollSeconds $PollSeconds `
    -AllowCurrentWorktreeReuse

Write-Host $lock.Message
try {
    & $FilePath @ArgumentList
    $exitCode = $LASTEXITCODE
    if ($null -ne $exitCode -and $exitCode -ne 0) {
        exit $exitCode
    }
}
finally {
    $release = Release-DtmApiRuntimeLock -RepoRoot $repo
    Write-Host $release.Message
}
