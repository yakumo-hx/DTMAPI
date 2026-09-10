$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
$repo=Get-RepoRoot
$root=Join-Path $repo ('temp/issue-index-' + [Guid]::NewGuid().ToString('N'))
$hostPath=Get-DtmApiPowerShellHost
$scriptPath=Join-Path $PSScriptRoot 'sync-issue-index.ps1'
$checks=0
function Assert-Index { param([bool]$Condition,[string]$Message); if(-not $Condition){throw $Message}; $script:checks++ }
function Invoke-IndexProcess {
    param([switch] $Check)
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $scriptPath, '-IssueRoot', $root)
    if ($Check) { $arguments += '-Check' }
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        # Expected rejection writes native stderr under Windows PowerShell 5.1.
        $ErrorActionPreference = 'Continue'
        $output = @(& $hostPath @arguments 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousErrorActionPreference }
    return [pscustomobject]@{ ExitCode = $exitCode; Text = $output -join "`n" }
}
try {
    New-Item -ItemType Directory -Path $root | Out-Null
    $index=Join-Path $root 'README.md'
    [IO.File]::WriteAllText($index,"# Test index`n`n<!-- generated: issue-index:start -->`nold`n<!-- generated: issue-index:end -->`nKeep this prose.`n",[Text.Encoding]::UTF8)
    $issue=Join-Path $root ('ISSUE-001-' + [char]0x4E2D + [char]0x6587 + ' ' + [char]0x7A7A + [char]0x683C + '.md')
    [IO.File]::WriteAllText($issue,"# ISSUE-001`n`n- State: ``open```n- Current boundary: Waiting | evidence.`n",[Text.Encoding]::UTF8)
    $result = Invoke-IndexProcess
    Assert-Index ($result.ExitCode -eq 0) ("Index generation failed: " + $result.Text)
    $first=[IO.File]::ReadAllText($index)
    Assert-Index ($first.Contains('Waiting \| evidence.') -and $first.Contains('%E4%B8%AD%E6%96%87%20') -and $first.Contains('Keep this prose.')) 'Unicode/path/table escaping or unrelated prose changed.'
    $result = Invoke-IndexProcess -Check
    Assert-Index ($result.ExitCode -eq 0 -and [IO.File]::ReadAllText($index) -ceq $first) 'Check is not idempotent/read-only.'
    [IO.File]::WriteAllText($issue,([IO.File]::ReadAllText($issue).Replace('`open`','`verified`')),[Text.Encoding]::UTF8)
    $result = Invoke-IndexProcess -Check
    Assert-Index ($result.ExitCode -ne 0 -and $result.Text.Contains('Issue index is stale') -and [IO.File]::ReadAllText($index) -ceq $first) 'Stale index was accepted or rewritten by Check.'
    Copy-Item -LiteralPath $issue -Destination (Join-Path $root 'ISSUE-001-duplicate.md')
    $result = Invoke-IndexProcess
    Assert-Index ($result.ExitCode -ne 0 -and $result.Text.Contains('Duplicate Issue identity') -and [IO.File]::ReadAllText($index) -ceq $first) 'Duplicate ID overwrote the index.'
    Remove-Item -LiteralPath (Join-Path $root 'ISSUE-001-duplicate.md')
    [IO.File]::WriteAllText($issue,([IO.File]::ReadAllText($issue).Replace('`verified`','`pending`')),[Text.Encoding]::UTF8)
    $result = Invoke-IndexProcess
    Assert-Index ($result.ExitCode -ne 0 -and $result.Text.Contains('Invalid Issue state') -and [IO.File]::ReadAllText($index) -ceq $first) 'Unknown state overwrote the index.'
    Write-Host "Issue index behavior: PASS ($checks checks)."
}
finally {
    $resolved=[IO.Path]::GetFullPath($root)
    $prefix=[IO.Path]::GetFullPath((Join-Path $repo 'temp'))+[IO.Path]::DirectorySeparatorChar
    if(-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing Issue test cleanup outside temp.'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
}
exit 0
