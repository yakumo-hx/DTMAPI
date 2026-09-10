[CmdletBinding()]
param([switch] $Check, [string] $IssueRoot = (Join-Path $PSScriptRoot '../../docs/debug/issues'))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$root = [IO.Path]::GetFullPath($IssueRoot)
$index = Join-Path $root 'README.md'
$text = [IO.File]::ReadAllText($index)
$start = '<!-- generated: issue-index:start -->'
$end = '<!-- generated: issue-index:end -->'
if ([regex]::Matches($text,[regex]::Escape($start)).Count -ne 1 -or [regex]::Matches($text,[regex]::Escape($end)).Count -ne 1 -or $text.IndexOf($start) -ge $text.IndexOf($end)) { throw 'Issue index must contain one ordered generated block.' }
$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('| Issue | State | Current boundary |')
$lines.Add('| --- | --- | --- |')
$seen = @{}
foreach ($file in @(Get-ChildItem -LiteralPath $root -File -Filter 'ISSUE-*.md' | Sort-Object Name)) {
    $name = [regex]::Match($file.Name, '^(ISSUE-\d{3})-[^/\\]+\.md$')
    if (-not $name.Success) { throw "Invalid Issue filename: $($file.Name)" }
    $id = $name.Groups[1].Value
    if ($seen.ContainsKey($id)) { throw "Duplicate Issue identity: $id" }
    $seen[$id] = $true
    $body = [IO.File]::ReadAllText($file.FullName)
    $state = [regex]::Matches($body, '(?m)^- State: `([a-z]+)`\r?$')
    $boundary = [regex]::Matches($body, '(?m)^- Current boundary: ([^\r\n]+)\r?$')
    if ($state.Count -ne 1 -or $boundary.Count -ne 1) { throw "$id needs exactly one canonical State and Current boundary field." }
    $value = $state[0].Groups[1].Value
    if ($value -notin @('open','monitoring','mitigated','verified','closed','deferred')) { throw "Invalid Issue state: $id/$value" }
    $description = $boundary[0].Groups[1].Value.Trim().Replace('|','\|')
    if ([string]::IsNullOrWhiteSpace($description)) { throw "Empty Issue boundary: $id" }
    $target = [Uri]::EscapeDataString($file.Name)
    $lines.Add("| [$id]($target) | $value | $description |")
}
if ($seen.Count -eq 0) { throw 'Refusing to replace the index with an empty Issue inventory.' }
$newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
$replacement = $start + $newline + [string]::Join($newline,$lines) + $newline + $end
$pattern = '(?s)' + [regex]::Escape($start) + '.*?' + [regex]::Escape($end)
$next = [regex]::Replace($text,$pattern,[Text.RegularExpressions.MatchEvaluator]{param($m) $replacement})
if ($Check) {
    if ($next -cne $text) { throw 'Issue index is stale; run tools/scripts/sync-issue-index.ps1.' }
    Write-Host "Issue index: PASS ($($seen.Count) canonical issues)."
    return
}
if ($next -cne $text) {
    $temporary = $index + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporary,$next,(New-Object Text.UTF8Encoding($false)))
        [IO.File]::Replace($temporary,$index,[NullString]::Value)
    }
    finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }
}
Write-Host "Issue index synchronized ($($seen.Count) canonical issues)."
