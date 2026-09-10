param(
    [switch] $Quiet
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. "$PSScriptRoot\document-paths.ps1"
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
    return Resolve-DtmApiDocumentPath -RepoRoot $repo -RelativePath $RelativePath
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

function Get-NormalizedTextSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $text = [System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString(
            $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($text))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
}

$updateIndexDirectory = Get-RepoPath 'docs/updates'
$annualIndexes = @(Get-ChildItem -LiteralPath $updateIndexDirectory -File -Filter 'INDEX-*.md' |
    Where-Object { $_.Name -match '^INDEX-\d{4}\.md$' })
$monthlyIndexes = @(Get-ChildItem -LiteralPath $updateIndexDirectory -File -Filter 'INDEX-*.md' |
    Where-Object { $_.Name -match '^INDEX-\d{4}-\d{2}\.md$' })
$annualIndexRelativePaths = @($annualIndexes | ForEach-Object { 'docs/updates/' + $_.Name })
$monthlyIndexRelativePaths = @($monthlyIndexes | ForEach-Object { 'docs/updates/' + $_.Name })

$requiredFiles = @(@(
    'docs/workflows/document-governance.md',
    'docs/onboarding/current-state.md',
    'docs/updates/INDEX.md',
    'docs/architecture/managed-product-admission-registry.md',
    'docs/debug/INDEX.md',
    'docs/debug/INDEX-history-through-20260711.md',
    'docs/debug/regressions/smoke-matrix.md',
    'docs/debug/regressions/smoke-matrix-history-through-20260711.md',
    'docs/debug/regressions/smoke-matrix-history-20260804-through-20260809.md',
    'docs/hook-map/README.md',
    'docs/hook-map/README-history-through-20260711.md'
) + $annualIndexRelativePaths + $monthlyIndexRelativePaths | Select-Object -Unique)

foreach ($relativePath in $requiredFiles) {
    Add-Check (Test-Path -LiteralPath (Get-RepoPath $relativePath)) "Missing required file: $relativePath"
}

$sizeLimits = @{
    'docs/updates/INDEX.md' = 8192
    'docs/debug/INDEX.md' = 16384
    'docs/debug/regressions/smoke-matrix.md' = 16384
    'docs/hook-map/README.md' = 16384
    'docs/onboarding/current-state.md' = 16384
    'docs/planning/DolocTownModdingAPI.md' = 8192
    'docs/planning/Debug.md' = 8192
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
    'docs/debug/regressions/smoke-matrix-history-20260804-through-20260809.md' = 14000
    'docs/hook-map/README-history-through-20260711.md' = 250000
}

foreach ($entry in $snapshotMinimums.GetEnumerator()) {
    $path = Get-RepoPath $entry.Key
    if (Test-Path -LiteralPath $path) {
        $length = (Get-Item -LiteralPath $path).Length
        Add-Check ($length -ge $entry.Value) "Historical ledger appears truncated: $($entry.Key) is $length bytes; expected at least $($entry.Value)."
    }
}

$frozenTextHashes = [ordered]@{
    'docs/architecture/batch6-managed-mod-identity-contract.md' = 'a1c7289f8e47b02662398275960e0c81b809c8a9423fc43ce68d4cd10bc90b5c'
    'docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md' = 'd589f26ee7378ec75cca5b69aec6c34ac3ff5f2aeda993df428226cdbb4f6d7f'
    'docs/debug/regressions/smoke-matrix-history-20260804-through-20260809.md' = '47f6dd1ca726ae2a6f4e8bbcb13276c07d3ec61a1376edc81d15ae6a0d099507'
    'docs/planning/archive/20260601-dtmapi-original-planning-transcript.md' = '651aa95b4534b33a092525302c9aab1edcb963c2b388ba2e615118d69262a55b'
    'docs/planning/archive/20260613-dtmapi-debug-system-planning-transcript.md' = '7c4be54cd41718e6fff160dff09dd623c400f597fc065b267c53819d9d5432a4'
}

foreach ($entry in $frozenTextHashes.GetEnumerator()) {
    $path = Get-RepoPath $entry.Key
    Add-Check (Test-Path -LiteralPath $path -PathType Leaf) "Frozen governance record is missing: $($entry.Key)"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $actualHash = Get-NormalizedTextSha256 -Path $path
        Add-Check ([string]::Equals($actualHash, $entry.Value, [System.StringComparison]::Ordinal)) "Frozen governance record changed: $($entry.Key). Use an explicit governance change instead of appending implementation progress."
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

$agentsText = Read-RepoText 'AGENTS.md'
$defaultContextMatch = [regex]::Match(
    $agentsText,
    '(?s)Before design or code work, read:\s*(?<block>.*?)(?=\r?\nBefore )')
Add-Check $defaultContextMatch.Success 'AGENTS.md default design/code context block could not be located.'
if ($defaultContextMatch.Success) {
    $defaultContextPaths = @([regex]::Matches(
        $defaultContextMatch.Groups['block'].Value,
        '(?m)^\s*-\s+`(?<path>[^`]+\.md)`\s*$') | ForEach-Object {
            $_.Groups['path'].Value
        })
    $expectedDefaultContextPaths = @(
        'PROJECT.md',
        'docs/onboarding/current-state.md'
    )
    Add-Check (($defaultContextPaths -join "`n") -eq ($expectedDefaultContextPaths -join "`n")) "AGENTS.md default design/code context must contain exactly PROJECT.md and docs/onboarding/current-state.md; found: $($defaultContextPaths -join ', ')"

    $defaultPlanningPaths = @([regex]::Matches(
        $defaultContextMatch.Groups['block'].Value,
        '`(?<path>docs/planning/[^`]+\.md)`') | ForEach-Object {
            $_.Groups['path'].Value
        } | Select-Object -Unique)

    foreach ($relativePath in $defaultPlanningPaths) {
        $planningText = Read-RepoText $relativePath
        $frontSheetLength = [Math]::Min($planningText.Length, 4096)
        $frontSheet = $planningText.Substring(0, $frontSheetLength)
        $isRetiredPlanning = [regex]::IsMatch(
            $frontSheet,
            '(?im)^(?:-\s*)?(?:Lifecycle Status|Status|Document Role|Role)\s*:\s*.*\b(?:frozen|superseded|historical)\b')
        Add-Check (-not $isRetiredPlanning) "Retired planning document is still mandatory default context: $relativePath"
    }
}

Add-Check ([regex]::IsMatch($agentsText, '(?s)## Conditional Context.*?Before using .*?`references/README\.md`')) 'AGENTS.md does not conditionally route reference/reverse work through references/README.md.'
Add-Check ([regex]::IsMatch($agentsText, '(?s)## Conditional Context.*?Before runtime lifecycle.*?`docs/debug/INDEX\.md`')) 'AGENTS.md does not conditionally route runtime/debug work through docs/debug/INDEX.md.'

$projectText = Read-RepoText 'PROJECT.md'
$projectIntroMatch = [regex]::Match($projectText, '(?s)\A(?<intro>.*?)(?=\r?\n## 当前方向)')
Add-Check $projectIntroMatch.Success 'PROJECT.md stable introduction could not be located.'
if ($projectIntroMatch.Success) {
    $projectIntro = $projectIntroMatch.Groups['intro'].Value
    Add-Check (-not [regex]::IsMatch($projectIntro, '\b[0-9]{10}\b')) 'PROJECT.md introduction must not copy volatile Workshop item IDs.'
    Add-Check (-not [regex]::IsMatch($projectIntro, '(?:首次上传|字节一致性|验收仍待)')) 'PROJECT.md introduction must not copy volatile Workshop publication or acceptance snapshots.'
    Add-Check ($projectIntro.Contains('tools/release/dtmapi-product-catalog.json')) 'PROJECT.md introduction does not route current distribution facts to the Product Catalog.'
    Add-Check ($projectIntro.Contains('tools/release/current-subscription-manifest.json')) 'PROJECT.md introduction does not route current subscription facts to the subscription manifest.'
}

$originalPlanningHandoff = Read-RepoText 'docs/planning/DolocTownModdingAPI.md'
Add-Check ([regex]::IsMatch($originalPlanningHandoff, '(?im)^- Lifecycle Status:\s*`superseded`\s*$')) 'Original planning compatibility path is not marked superseded.'
Add-Check ($originalPlanningHandoff.Contains('archive/20260601-dtmapi-original-planning-transcript.md')) 'Original planning compatibility path does not route to its frozen transcript.'

$debugPlanningHandoff = Read-RepoText 'docs/planning/Debug.md'
Add-Check ([regex]::IsMatch($debugPlanningHandoff, '(?im)^- Lifecycle Status:\s*`superseded`\s*$')) 'Debug planning compatibility path is not marked superseded.'
Add-Check ($debugPlanningHandoff.Contains('archive/20260613-dtmapi-debug-system-planning-transcript.md')) 'Debug planning compatibility path does not route to its frozen transcript.'

$goalHistory = @((Get-DtmApiDocumentManifest $repo).files | Where-Object { $_.source -like 'docs/goals/2026/*' })
Add-Check ($goalHistory.Count -eq 80) "Historical Goal identity count changed: expected 80, found $($goalHistory.Count)."
foreach ($goal in $goalHistory) {
    $path = Get-RepoPath $goal.source
    Add-Check (Test-Path -LiteralPath $path -PathType Leaf) "Historical Goal is missing: $($goal.source)"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $expected = if ($goal.PSObject.Properties['currentTextSha256']) { $goal.currentTextSha256 } else { $goal.textSha256 }
        $actual = Get-NormalizedTextSha256 -Path $path
        Add-Check ([string]::Equals($expected, $actual, [StringComparison]::OrdinalIgnoreCase)) "Historical Goal bytes changed outside the reviewed migration: $($goal.source)"
    }
}

$updateFiles = @(Get-DtmApiUpdateRecordFiles -RepoRoot $repo)

$normalizedUpdateFields = @{
    'Lifecycle Status' = 'proposed|in-progress|implemented|verified|blocked|reverted|superseded'
    'Validation Level' = '(?:not-run|docs|source|unit|runtime|player)(?:\s*,\s*(?:not-run|docs|source|unit|runtime|player))*'
    'Runtime Validation' = 'not-required|not-run|passed|failed|blocked|partial'
    'Related Issue State' = 'none|open|monitoring|mitigated|verified|closed|deferred'
}
$fullRecordCutoff = '20260809-0002'
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
        $recordRow = [regex]::Match($monthlyText, '(?m)^\|\s*' + [regex]::Escape($id) + '\s*\|[^\r\n]*')
        $bodyLinks = @([regex]::Matches($recordRow.Value, '\[[^\]]*\]\(([^)]+)\)') | Where-Object {
            [Uri]::UnescapeDataString($_.Groups[1].Value).EndsWith('/' + $file.Name, [StringComparison]::Ordinal)
        })
        $bodyLinkCorrect = $false
        if ($bodyLinks.Count -eq 1) {
            $baseUri = [Uri]::new((Split-Path -Parent $monthlyPath) + [IO.Path]::DirectorySeparatorChar)
            $linkedPath = [Uri]::new($baseUri, $bodyLinks[0].Groups[1].Value).LocalPath
            # The monthly projection must name the body, not an old redirect.
            $bodyLinkCorrect = $linkedPath -ieq $file.FullName
        }
        Add-Check $bodyLinkCorrect "Update monthly row must link once to the complete current body: ${monthlyRelative} / $($file.Name)"
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

        if ([string]::CompareOrdinal($id, $fullRecordCutoff) -ge 0) {
            $expectedDate = $id.Substring(0, 4) + '-' + $id.Substring(4, 2) + '-' + $id.Substring(6, 2)
            Add-Check ([regex]::IsMatch($text, '(?m)^- Update ID:\s*`' + [regex]::Escape($id) + '`\s*$')) "Invalid or missing exact Update ID in $($file.Name)."
            Add-Check ([regex]::IsMatch($text, '(?m)^- Date:\s*`' + [regex]::Escape($expectedDate) + '`\s*$')) "Invalid or missing exact Date in $($file.Name)."
            Add-Check ([regex]::IsMatch($text, '(?m)^- Source(?: Request)?:\s*\S.+$')) "Invalid or missing Source/Source Request in $($file.Name)."
            foreach ($heading in @('Changed Files', 'Validation', 'Evidence', 'Rollback Notes', 'Follow-Up')) {
                Add-Check ([regex]::IsMatch($text, '(?im)^##\s+' + [regex]::Escape($heading) + '\s*$')) "Missing required '$heading' section in $($file.Name)."
            }
        }
    }
}

$updateRootText = Read-RepoText 'docs/updates/INDEX.md'
Add-Check (-not [regex]::IsMatch($updateRootText, '(?m)^\|\s*\d{8}-\d{4}\s*\|')) 'Root Update index must not contain Update rows.'

foreach ($annual in $annualIndexes) {
    Add-Check ($updateRootText.Contains($annual.Name)) "Root Update index does not route to $($annual.Name)."
    $annualText = [System.IO.File]::ReadAllText($annual.FullName)
    Add-Check ($annual.Length -le 8192) "Annual Update router exceeds 8 KiB: $($annual.Name) is $($annual.Length) bytes."
    Add-Check (-not [regex]::IsMatch($annualText, '(?m)^\|\s*\d{8}-\d{4}\s*\|')) "$($annual.Name) must route to months and must not contain Update rows."

    $year = [regex]::Match($annual.Name, '^INDEX-(\d{4})\.md$').Groups[1].Value
    foreach ($monthly in @($monthlyIndexes | Where-Object { $_.Name -like "INDEX-$year-??.md" })) {
        Add-Check ($annualText.Contains($monthly.Name)) "$($annual.Name) does not route to $($monthly.Name)."
    }
}

$allMonthlyIds = New-Object System.Collections.Generic.List[string]
$latestMonthlyKey = @($monthlyIndexes | ForEach-Object {
    $match = [regex]::Match($_.Name, '^INDEX-(\d{4})-(\d{2})\.md$')
    if ($match.Success) { $match.Groups[1].Value + '-' + $match.Groups[2].Value }
} | Sort-Object -Descending | Select-Object -First 1)
$latestMonthlyKey = if ($latestMonthlyKey.Count -eq 1) { [string]$latestMonthlyKey[0] } else { '' }
foreach ($index in $monthlyIndexes) {
    $text = [System.IO.File]::ReadAllText($index.FullName)
    Add-Check ($index.Length -le 196608) "Monthly Update index exceeds 192 KiB: $($index.Name) is $($index.Length) bytes."
    $ids = @([regex]::Matches($text, '(?m)^\|\s*(\d{8}-\d{4})\s*\|') | ForEach-Object { $_.Groups[1].Value })
    foreach ($group in @($ids | Group-Object)) {
        Add-Check ($group.Count -eq 1) "Duplicate row for Update ID $($group.Name) in $($index.Name)."
    }

    $indexMatch = [regex]::Match($index.Name, '^INDEX-(\d{4})-(\d{2})\.md$')
    $expectedPrefix = $indexMatch.Groups[1].Value + $indexMatch.Groups[2].Value
    $monthlyKey = $indexMatch.Groups[1].Value + '-' + $indexMatch.Groups[2].Value
    if ([string]::CompareOrdinal($monthlyKey, '2026-07') -ge 0 -and
        -not [string]::IsNullOrWhiteSpace($latestMonthlyKey) -and
        [string]::CompareOrdinal($monthlyKey, $latestMonthlyKey) -lt 0) {
        $summaryMatch = [regex]::Match(
            $text,
            '(?ms)^## Non-Authoritative Monthly Summary\s*\r?\n(?<body>.*?)(?=^##\s)')
        Add-Check $summaryMatch.Success "Closed monthly Update index is missing the non-authoritative summary: $($index.Name)."
        if ($summaryMatch.Success) {
            Add-Check ($summaryMatch.Groups['body'].Value.Contains('<!-- non-authoritative-monthly-summary -->')) "Closed monthly summary is missing its marker: $($index.Name)."
            $summaryBytes = [System.Text.Encoding]::UTF8.GetByteCount($summaryMatch.Value)
            Add-Check ($summaryBytes -le 4096) "Closed monthly summary exceeds 4 KiB: $($index.Name) is $summaryBytes bytes."
        }
    }
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

$linkFiles = @(@(
    'AGENTS.md',
    'PROJECT.md',
    'README.md',
    'author-sdk/README.md',
    'src/README.md',
    'docs/onboarding/current-state.md',
    'docs/updates/INDEX.md',
    'docs/architecture/README.md',
    'docs/architecture/managed-product-admission-registry.md',
    'docs/design/dtmapi-manager-ui-mvp.md',
    'docs/planning/README.md',
    'docs/planning/Debug.md',
    'docs/reviews/api/native-owner-domains/INDEX.md',
    'docs/debug/INDEX.md',
    'docs/debug/issues/README.md',
    'docs/debug/regressions/smoke-matrix.md',
    'docs/hook-map/README.md',
    'docs/workflows/document-governance.md',
    'docs/updates/README.md',
    'docs/reviews/README.md'
) + $annualIndexRelativePaths + $monthlyIndexRelativePaths | Select-Object -Unique)

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

try {
    & (Join-Path $PSScriptRoot 'sync-issue-index.ps1') -Check 6>$null
    Add-Check $true 'Issue index projection.'
}
catch { Add-Check $false ("Issue index projection: " + $_.Exception.Message) }

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
