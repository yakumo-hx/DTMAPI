param(
    [Parameter(Mandatory = $true)] [string] $UpdatePath,
    [string] $Area = '',
    [string] $Summary = '',
    [switch] $Check
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $PSScriptRoot 'document-paths.ps1')
$path = $UpdatePath
if (-not [System.IO.Path]::IsPathRooted($path)) { $path = Join-Path $repo $path }
$path = [System.IO.Path]::GetFullPath($path)
if (-not $path.StartsWith($repo.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Update must be in this repository under docs/updates/YYYY or docs/archive/updates/YYYY.'
}
$path = Resolve-DtmApiDocumentPath $repo $path.Substring($repo.TrimEnd('\', '/').Length + 1)
$name = [System.IO.Path]::GetFileName($path)
$nameMatch = [regex]::Match($name, '^(\d{8}-\d{4})-[a-z0-9][a-z0-9-]*\.md$')
if (-not $nameMatch.Success) { throw 'Expected a YYYYMMDD-NNNN-slug.md Update.' }
$id = $nameMatch.Groups[1].Value
$date = [DateTime]::ParseExact($id.Substring(0, 8), 'yyyyMMdd', [Globalization.CultureInfo]::InvariantCulture)
$yearPath = Join-Path $repo ('docs\updates\' + $date.ToString('yyyy'))
$archiveYearPath = Join-Path $repo ('docs\archive\updates\' + $date.ToString('yyyy'))
if ([System.IO.Path]::GetDirectoryName($path) -inotIn @($yearPath, $archiveYearPath)) {
    throw 'Update must be in this repository under docs/updates/YYYY or docs/archive/updates/YYYY.'
}
$sameIds = @(Get-DtmApiUpdateRecordFiles $repo | Where-Object { $_.Name -like "$id-*.md" })
if ($sameIds.Count -ne 1 -or $sameIds[0].FullName -ine $path) { throw "Missing or duplicate Update ID: $id" }
$updateText = [System.IO.File]::ReadAllText($path)
$metadataMatch = [regex]::Match($updateText, '(?ms)^## Metadata[ \t]*\r?\n(?<body>.*?)(?=^## |\z)')
if (-not $metadataMatch.Success) { throw 'Update requires a Metadata section.' }
$metadata = @{}
$fieldPatterns = [ordered]@{
    'Update ID' = [regex]::Escape($id)
    'Date' = $date.ToString('yyyy-MM-dd')
    'Lifecycle Status' = 'proposed|in-progress|implemented|verified|blocked|reverted|superseded'
    'Validation Level' = '(?:not-run|docs|source|unit|runtime|player)(?:\s*,\s*(?:not-run|docs|source|unit|runtime|player))*'
    'Runtime Validation' = 'not-required|not-run|passed|failed|blocked|partial'
    'Related Issue State' = 'none|open|monitoring|mitigated|verified|closed|deferred'
}
foreach ($field in $fieldPatterns.GetEnumerator()) {
    $prefix = '(?m)^- ' + [regex]::Escape($field.Key) + ':[ \t]*'
    $entries = [regex]::Matches($metadataMatch.Groups['body'].Value, $prefix + '[^\r\n]*')
    if ($entries.Count -ne 1) { throw "Missing or duplicate metadata: $($field.Key)" }
    $value = [regex]::Match($entries[0].Value, $prefix + '`(' + $field.Value + ')`[ \t]*$')
    if (-not $value.Success) { throw "Invalid metadata: $($field.Key)" }
    $metadata[$field.Key] = $value.Groups[1].Value
}

$ledgerPath = Join-Path $repo ('docs\updates\INDEX-' + $date.ToString('yyyy-MM') + '.md')
if (-not (Test-Path -LiteralPath $ledgerPath -PathType Leaf)) {
    throw "Create the monthly router first: $ledgerPath"
}
$ledgerText = [System.IO.File]::ReadAllText($ledgerPath)
$rowPattern = '(?m)^\|[ \t]*' + [regex]::Escape($id) + '[ \t]*\|[^\r\n]*'
$rows = [regex]::Matches($ledgerText, $rowPattern)
if ($rows.Count -gt 1) { throw "Duplicate monthly rows for $id" }
$ledgerBase = [Uri]::new((Split-Path -Parent $ledgerPath) + [System.IO.Path]::DirectorySeparatorChar)
$record = '[' + $name + '](' + $ledgerBase.MakeRelativeUri([Uri]::new($path)).ToString() + ')'
if ($rows.Count -eq 1) {
    $cells = [regex]::Split($rows[0].Value, '(?<!\\)\|')
    if ($cells.Count -ne 11) { throw "Malformed or conflicting monthly row for $id" }
    $link = [regex]::Match($cells[9].Trim(), '^\[[^\]]+\]\(([^)]+)\)$')
    if (-not $link.Success) { throw "Malformed or conflicting monthly row for $id" }
    $linkedPath = [Uri]::new($ledgerBase, $link.Groups[1].Value).LocalPath
    if (-not $linkedPath.StartsWith($repo.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Malformed or conflicting monthly row for $id"
    }
    $linkedPath = Resolve-DtmApiDocumentPath $repo $linkedPath.Substring($repo.TrimEnd('\', '/').Length + 1)
    if ($linkedPath -ine $path) { throw "Malformed or conflicting monthly row for $id" }
    if (-not $PSBoundParameters.ContainsKey('Area')) { $Area = $cells[7].Trim() }
    if (-not $PSBoundParameters.ContainsKey('Summary')) { $Summary = $cells[8].Trim() }
}
foreach ($cell in @($Area, $Summary)) {
    if ([string]::IsNullOrWhiteSpace($cell) -or $cell -match '(?<!\\)\||[\r\n]') {
        throw 'Area and Summary must be nonempty single Markdown cells; supply both for a new row.'
    }
}
$row = '| ' + (@($id, $metadata['Date'], $metadata['Lifecycle Status'], $metadata['Validation Level'],
    $metadata['Runtime Validation'], $metadata['Related Issue State'], $Area, $Summary, $record) -join ' | ') + ' |'
if ($rows.Count -eq 1) {
    $oldRow = $rows[0]
    $nextText = $ledgerText.Substring(0, $oldRow.Index) + $row + $ledgerText.Substring($oldRow.Index + $oldRow.Length)
}
else {
    $header = '| Update ID | Date | Lifecycle | Validation | Runtime | Issue | Area | Summary | Record |'
    $table = [regex]::Matches($ledgerText, '(?m)^' + [regex]::Escape($header) + '\r?\n\|(?:[ \t]*:?-+:?[ \t]*\|){9}[ \t]*\r?\n')
    if ($table.Count -ne 1) { throw 'Expected exactly one normalized monthly table.' }
    $newline = if ($table[0].Value.Contains("`r`n")) { "`r`n" } else { "`n" }
    $nextText = $ledgerText.Insert($table[0].Index + $table[0].Length, $row + $newline)
}
if ($nextText -ceq $ledgerText) { Write-Output "Update ledger already current: $id"; return }
if ($Check) { throw "Monthly row is stale or missing for $id; run sync-update-ledger.ps1 without -Check." }

# Replace one existing file atomically, preserving its BOM and all other rows.
$node = Get-Item -LiteralPath $ledgerPath
while ($null -ne $node -and $node.FullName.Length -ge $repo.Length) {
    if (($node.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Ledger path must not contain reparse points.' }
    $node = if ($node -is [System.IO.FileInfo]) { $node.Directory } else { $node.Parent }
}
$bytes = [System.IO.File]::ReadAllBytes($ledgerPath)
$hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
$tempPath = $ledgerPath + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
try {
    [System.IO.File]::WriteAllText($tempPath, $nextText, (New-Object System.Text.UTF8Encoding($hasBom)))
    if ([System.IO.File]::ReadAllText($ledgerPath) -cne $ledgerText) { throw 'Ledger changed during synchronization; retry with its new contents.' }
    [System.IO.File]::Replace($tempPath, $ledgerPath, [NullString]::Value)
}
finally {
    if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath }
}
Write-Output "Synchronized Update ledger: $id"
