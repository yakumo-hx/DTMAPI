param(
    [switch] $Quiet
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$failures = New-Object System.Collections.Generic.List[string]
$checks = 0

function Add-Check {
    param([bool] $Passed, [string] $Message)

    $script:checks++
    if (-not $Passed) {
        $script:failures.Add($Message)
    }
}

function Get-RepoPath {
    param([string] $RelativePath)
    return Join-Path $repo $RelativePath
}

function Read-RepoText {
    param([string] $RelativePath)

    $path = Get-RepoPath $RelativePath
    Add-Check (Test-Path -LiteralPath $path) "Missing required file: $RelativePath"
    if (-not (Test-Path -LiteralPath $path)) {
        return ''
    }

    return [System.IO.File]::ReadAllText($path)
}

$requiredFiles = @(
    'docs/workflows/document-governance.md',
    'docs/onboarding/current-state.md',
    'docs/updates/INDEX.md',
    'docs/updates/INDEX-2026.md',
    'docs/updates/INDEX-2026-05.md',
    'docs/updates/INDEX-2026-06.md',
    'docs/updates/INDEX-2026-07.md',
    'docs/debug/INDEX.md',
    'docs/debug/INDEX-history-through-20260711.md',
    'docs/debug/regressions/smoke-matrix.md',
    'docs/debug/regressions/smoke-matrix-history-through-20260711.md',
    'docs/hook-map/README.md',
    'docs/hook-map/README-history-through-20260711.md'
)

foreach ($relativePath in $requiredFiles) {
    Add-Check (Test-Path -LiteralPath (Get-RepoPath $relativePath)) "Missing required file: $relativePath"
}

$sizeLimits = @{
    'docs/updates/INDEX.md' = 8192
    'docs/updates/INDEX-2026.md' = 8192
    'docs/updates/INDEX-2026-05.md' = 196608
    'docs/updates/INDEX-2026-06.md' = 196608
    'docs/updates/INDEX-2026-07.md' = 196608
    'docs/debug/INDEX.md' = 16384
    'docs/debug/regressions/smoke-matrix.md' = 16384
    'docs/hook-map/README.md' = 16384
    'docs/onboarding/current-state.md' = 16384
}

foreach ($entry in $sizeLimits.GetEnumerator()) {
    $path = Get-RepoPath $entry.Key
    if (Test-Path -LiteralPath $path) {
        $length = (Get-Item -LiteralPath $path).Length
        Add-Check ($length -le $entry.Value) "Router exceeds size limit: $($entry.Key) is $length bytes; limit is $($entry.Value)."
    }
}

$snapshotMinimums = @{
    'docs/debug/INDEX-history-through-20260711.md' = 100000
    'docs/debug/regressions/smoke-matrix-history-through-20260711.md' = 450000
    'docs/hook-map/README-history-through-20260711.md' = 250000
}

foreach ($entry in $snapshotMinimums.GetEnumerator()) {
    $path = Get-RepoPath $entry.Key
    if (Test-Path -LiteralPath $path) {
        $length = (Get-Item -LiteralPath $path).Length
        Add-Check ($length -ge $entry.Value) "Historical ledger appears truncated: $($entry.Key) is $length bytes; expected at least $($entry.Value)."
    }
}

$currentState = Read-RepoText 'docs/onboarding/current-state.md'
Add-Check (-not [regex]::IsMatch($currentState, '(?im)^Status date\s*:')) 'Current-truth router must not contain a manually maintained Status date.'
Add-Check (-not [regex]::IsMatch($currentState, '(?<![0-9])\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?')) 'Current-truth router must not copy a semantic version.'

$activeRuleFiles = @(
    'AGENTS.md',
    'PROJECT.md',
    'docs/workflows/codex-feedback-to-goal.md',
    'docs/workflows/codex-api-rebuild.md',
    'docs/workflows/document-governance.md',
    'docs/workflows/player-feedback-community-loop.md',
    'docs/reviews/README.md',
    'docs/reviews/manual-qa/README.md',
    'docs/reviews/templates/manual-qa-review.md',
    'docs/onboarding/current-state.md'
)

foreach ($relativePath in $activeRuleFiles) {
    $text = Read-RepoText $relativePath
    Add-Check (-not [regex]::IsMatch($text, '(?i)docs/goals|\.goal\.txt|(?m)^\s*/goal\b')) "Retired Goal workflow reference found in active rule: $relativePath"
}

$goalHistory = @(Get-ChildItem -LiteralPath (Get-RepoPath 'docs/goals/2026') -File)
Add-Check ($goalHistory.Count -eq 80) "Historical Goal file count changed: expected 80, found $($goalHistory.Count)."

$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    $goalStatus = @(& git -C $repo status --porcelain -- docs/goals/2026)
    Add-Check ($goalStatus.Count -eq 0) "Historical Goal files were modified, added, or deleted: $($goalStatus -join '; ')"
}

$updateFiles = @(Get-ChildItem -LiteralPath (Get-RepoPath 'docs/updates') -Directory | ForEach-Object {
    Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.md'
} | Where-Object { $_.Name -match '^(\d{8}-\d{4})-.+\.md$' })

$normalizedUpdateFields = @{
    'Lifecycle Status' = 'proposed|in-progress|implemented|verified|blocked|reverted|superseded'
    'Validation Level' = '(?:not-run|docs|source|unit|runtime|player)(?:\s*,\s*(?:not-run|docs|source|unit|runtime|player))*'
    'Runtime Validation' = 'not-required|not-run|passed|failed|blocked|partial'
    'Related Issue State' = 'none|open|monitoring|mitigated|verified|closed|deferred'
}
$updateMetadataById = @{}

$idGroups = @($updateFiles | Group-Object { if ($_.Name -match '^(\d{8}-\d{4})-') { $Matches[1] } })
foreach ($group in $idGroups) {
    Add-Check ($group.Count -eq 1) "Duplicate Update ID $($group.Name): $($group.Group.FullName -join '; ')"
}

foreach ($file in $updateFiles) {
    $id = [regex]::Match($file.Name, '^(\d{8}-\d{4})-').Groups[1].Value
    $year = $id.Substring(0, 4)
    $month = $id.Substring(4, 2)
    $annualRelative = "docs/updates/INDEX-$year.md"
    $annualPath = Get-RepoPath $annualRelative
    Add-Check (Test-Path -LiteralPath $annualPath) "Missing annual Update index: $annualRelative"
    $monthlyRelative = "docs/updates/INDEX-$year-$month.md"
    $monthlyPath = Get-RepoPath $monthlyRelative
    Add-Check (Test-Path -LiteralPath $monthlyPath) "Missing monthly Update index: $monthlyRelative"
    if (Test-Path -LiteralPath $monthlyPath) {
        $monthlyText = [System.IO.File]::ReadAllText($monthlyPath)
        Add-Check ($monthlyText.Contains($file.Name)) "Update record is not linked from ${monthlyRelative}: $($file.Name)"
    }

    if ([string]::CompareOrdinal($id, '20260711-0007') -ge 0) {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        $metadata = @{}

        foreach ($field in $normalizedUpdateFields.GetEnumerator()) {
            $pattern = '(?m)^- ' + [regex]::Escape($field.Key) + ':\s*`(' + $field.Value + ')`\s*$'
            $match = [regex]::Match($text, $pattern)
            Add-Check $match.Success "Invalid or missing $($field.Key) in $($file.Name)."
            if ($match.Success) {
                $metadata[$field.Key] = $match.Groups[1].Value.Trim()
            }
        }

        $updateMetadataById[$id] = $metadata

        Add-Check (-not [regex]::IsMatch($text, '(?m)^- Status\s*:')) "New Update must not use the legacy combined Status field: $($file.Name)"
    }
}

$updateRootText = Read-RepoText 'docs/updates/INDEX.md'
Add-Check (-not [regex]::IsMatch($updateRootText, '(?m)^\|\s*\d{8}-\d{4}\s*\|')) 'Root Update index must not contain Update rows.'

$annualIndexes = @(Get-ChildItem -LiteralPath (Get-RepoPath 'docs/updates') -File -Filter 'INDEX-*.md' |
    Where-Object { $_.Name -match '^INDEX-\d{4}\.md$' })
$monthlyIndexes = @(Get-ChildItem -LiteralPath (Get-RepoPath 'docs/updates') -File -Filter 'INDEX-*.md' |
    Where-Object { $_.Name -match '^INDEX-\d{4}-\d{2}\.md$' })

foreach ($annual in $annualIndexes) {
    Add-Check ($updateRootText.Contains($annual.Name)) "Root Update index does not route to $($annual.Name)."
    $annualText = [System.IO.File]::ReadAllText($annual.FullName)
    Add-Check (-not [regex]::IsMatch($annualText, '(?m)^\|\s*\d{8}-\d{4}\s*\|')) "$($annual.Name) must route to months and must not contain Update rows."

    $year = [regex]::Match($annual.Name, '^INDEX-(\d{4})\.md$').Groups[1].Value
    foreach ($monthly in @($monthlyIndexes | Where-Object { $_.Name -like "INDEX-$year-??.md" })) {
        Add-Check ($annualText.Contains($monthly.Name)) "$($annual.Name) does not route to $($monthly.Name)."
    }
}

$allMonthlyIds = New-Object System.Collections.Generic.List[string]
foreach ($index in $monthlyIndexes) {
    $text = [System.IO.File]::ReadAllText($index.FullName)
    Add-Check ($index.Length -le 196608) "Monthly Update index exceeds 192 KiB: $($index.Name) is $($index.Length) bytes."
    $ids = @([regex]::Matches($text, '(?m)^\|\s*(\d{8}-\d{4})\s*\|') | ForEach-Object { $_.Groups[1].Value })
    foreach ($group in @($ids | Group-Object)) {
        Add-Check ($group.Count -eq 1) "Duplicate row for Update ID $($group.Name) in $($index.Name)."
    }

    $indexMatch = [regex]::Match($index.Name, '^INDEX-(\d{4})-(\d{2})\.md$')
    $expectedPrefix = $indexMatch.Groups[1].Value + $indexMatch.Groups[2].Value
    foreach ($id in $ids) {
        $allMonthlyIds.Add($id)
        Add-Check ($id.StartsWith($expectedPrefix, [System.StringComparison]::Ordinal)) "Update ID $id is filed in the wrong month index: $($index.Name)."

        if ([string]::CompareOrdinal($id, '20260711-0007') -ge 0) {
            $rowPattern = '(?m)^\|\s*' + [regex]::Escape($id) + '\s*\|\s*([^|]*)\|\s*([^|]*)\|\s*([^|]*)\|\s*([^|]*)\|\s*([^|]*)\|'
            $rowMatch = [regex]::Match($text, $rowPattern)
            Add-Check $rowMatch.Success "Normalized Update row has invalid columns for ${id}: $($index.Name)."
            if ($rowMatch.Success) {
                $indexValues = @{
                    'Lifecycle Status' = $rowMatch.Groups[2].Value.Trim()
                    'Validation Level' = $rowMatch.Groups[3].Value.Trim()
                    'Runtime Validation' = $rowMatch.Groups[4].Value.Trim()
                    'Related Issue State' = $rowMatch.Groups[5].Value.Trim()
                }

                foreach ($field in $normalizedUpdateFields.GetEnumerator()) {
                    $indexValue = $indexValues[$field.Key]
                    Add-Check ([regex]::IsMatch($indexValue, '^(?:' + $field.Value + ')$')) "Invalid $($field.Key) '$indexValue' in monthly index row ${id}: $($index.Name)."

                    if ($updateMetadataById.ContainsKey($id) -and $updateMetadataById[$id].ContainsKey($field.Key)) {
                        $recordValue = $updateMetadataById[$id][$field.Key]
                        Add-Check ([string]::Equals($indexValue, $recordValue, [System.StringComparison]::Ordinal)) "Monthly index $($field.Key) '$indexValue' does not match Update record '$recordValue' for ${id}."
                    }
                }
            }
        }
    }
}

foreach ($group in @($allMonthlyIds | Group-Object)) {
    Add-Check ($group.Count -eq 1) "Update ID $($group.Name) appears in more than one monthly index."
}

$updateIds = @($idGroups | ForEach-Object { $_.Name })
foreach ($id in $allMonthlyIds) {
    Add-Check ($updateIds -contains $id) "Monthly index row has no matching Update record: $id"
}
Add-Check ($allMonthlyIds.Count -eq $updateFiles.Count) "Monthly index row count $($allMonthlyIds.Count) does not match Update record count $($updateFiles.Count)."

$linkFiles = @(
    'docs/onboarding/current-state.md',
    'docs/updates/INDEX.md',
    'docs/updates/INDEX-2026.md',
    'docs/updates/INDEX-2026-05.md',
    'docs/updates/INDEX-2026-06.md',
    'docs/updates/INDEX-2026-07.md',
    'docs/debug/INDEX.md',
    'docs/debug/issues/README.md',
    'docs/debug/regressions/smoke-matrix.md',
    'docs/hook-map/README.md',
    'docs/workflows/document-governance.md',
    'docs/updates/README.md',
    'docs/reviews/README.md'
)

foreach ($relativePath in $linkFiles) {
    $path = Get-RepoPath $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        continue
    }

    $text = [System.IO.File]::ReadAllText($path)
    $matches = [regex]::Matches($text, '\[[^\]]+\]\(([^)]+)\)')
    foreach ($match in $matches) {
        $target = $match.Groups[1].Value.Trim().Trim('<', '>')
        if ($target -match '^(?:https?://|mailto:|#)') {
            continue
        }

        $targetWithoutAnchor = ($target -split '#')[0]
        if ([string]::IsNullOrWhiteSpace($targetWithoutAnchor)) {
            continue
        }

        $resolved = Join-Path (Split-Path -Parent $path) $targetWithoutAnchor
        Add-Check (Test-Path -LiteralPath $resolved) "Broken Markdown link in ${relativePath}: $target"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Document governance: FAILED ($($failures.Count) failures / $checks checks)" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }
    exit 1
}

if (-not $Quiet) {
    Write-Host "Document governance: OK ($checks checks)" -ForegroundColor Green
}
