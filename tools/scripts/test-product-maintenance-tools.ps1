param([switch] $LedgerOnly)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$tempBase = Join-Path $repo 'tmp\test-runs'
$fixtureRoot = Join-Path $tempBase ('product-maintenance-' + [guid]::NewGuid().ToString('N'))
$utf8 = New-Object System.Text.UTF8Encoding($false)
$shellExe = (Get-Process -Id $PID).Path

function Assert-Maintenance {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}

function Assert-LedgerRejects {
    param([scriptblock] $Action, [string] $Expected)
    $before = [IO.File]::ReadAllText($ledgerPath)
    $caught = $null
    try { & $Action | Out-Null } catch { $caught = $_.Exception.Message }
    Assert-Maintenance ($null -ne $caught -and $caught -match $Expected) "Expected rejection '$Expected', got '$caught'."
    Assert-Maintenance ([IO.File]::ReadAllText($ledgerPath) -ceq $before) 'Rejected operation changed the ledger.'
}

function Assert-CatalogFixture {
    param([string[]] $RequiredFailures = @(), [string[]] $ForbiddenFailures = @())
    [IO.File]::WriteAllText($catalogPath, ($candidate | ConvertTo-Json -Depth 100), $utf8)
    $output = (& $shellExe -NoProfile -File (Join-Path $PSScriptRoot 'check-product-catalog.ps1') -Quiet -CatalogPath $catalogPath 2>&1 | Out-String)
    $expectedExit = if ($RequiredFailures.Count -eq 0) { 0 } else { 1 }
    Assert-Maintenance ($LASTEXITCODE -eq $expectedExit) "Catalog exit mismatch: $output"
    foreach ($pattern in $RequiredFailures) { Assert-Maintenance ($output -match $pattern) "Missing expected Catalog rejection '$pattern': $output" }
    foreach ($pattern in $ForbiddenFailures) { Assert-Maintenance ($output -notmatch $pattern) "Unexpected Catalog rejection '$pattern': $output" }
}

try {
    $scriptDir = New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'tools\scripts')
    $yearDir = New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'docs\updates\2026')
    $syncPath = Join-Path $scriptDir.FullName 'sync-update-ledger.ps1'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'sync-update-ledger.ps1') -Destination $syncPath
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'document-paths.ps1') -Destination $scriptDir.FullName
    $updatePath = Join-Path $yearDir.FullName '20260907-0001-fixture.md'
    $ledgerPath = Join-Path $fixtureRoot 'docs\updates\INDEX-2026-09.md'
    $updateText = @'
# Fixture

## Metadata

- Update ID: `20260907-0001`
- Date: `2026-09-07`
- Lifecycle Status: `in-progress`
- Validation Level: `not-run`
- Runtime Validation: `not-required`
- Related Issue State: `none`

## Summary

Test data only.
'@
    $ledgerText = @'
# Fixture month

| Update ID | Date | Lifecycle | Validation | Runtime | Issue | Area | Summary | Record |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 20260906-0001 | 2026-09-06 | verified | docs | not-required | none | untouched | Keep this exact row. | [older](2026/older.md) |

Footer stays unchanged.
'@
    # Exercise both newline formats; the second case also carries a UTF-8 BOM.
    foreach ($newline in @("`n", "`r`n")) {
        $fixtureLedger = $ledgerText.Replace("`r`n", "`n").Replace("`n", $newline)
        $encoding = New-Object System.Text.UTF8Encoding(($newline -eq "`r`n"))
        [IO.File]::WriteAllText($updatePath, $updateText, $utf8)
        [IO.File]::WriteAllText($ledgerPath, $fixtureLedger, $encoding)
        Assert-LedgerRejects { & $syncPath -UpdatePath $updatePath -Check -Area fixture -Summary 'Human summary.' } 'stale or missing'
        & $syncPath -UpdatePath $updatePath -Area fixture -Summary 'Human summary.' | Out-Null
        $created = [IO.File]::ReadAllText($ledgerPath)
        $row = [regex]::Match($created, '(?m)^\| 20260907-0001[^\r\n]*').Value
        Assert-Maintenance ($created.Replace($row + $newline, '') -ceq $fixtureLedger) 'Creating a row changed surrounding text.'
        $finished = $updateText.Replace('`in-progress`', '`verified`').Replace('`not-run`', '`docs, source`')
        [IO.File]::WriteAllText($updatePath, $finished, $utf8)
        Assert-LedgerRejects { & $syncPath -UpdatePath $updatePath -Check } 'stale or missing'
        & $syncPath -UpdatePath $updatePath | Out-Null
        $expected = $created.Replace('| in-progress | not-run |', '| verified | docs, source |')
        Assert-Maintenance ([IO.File]::ReadAllText($ledgerPath) -ceq $expected) 'Status synchronization changed human text or other rows.'
        $stamp = (Get-Item -LiteralPath $ledgerPath).LastWriteTimeUtc
        $hash = (Get-FileHash -LiteralPath $ledgerPath).Hash
        & $syncPath -UpdatePath $updatePath | Out-Null
        & $syncPath -UpdatePath $updatePath -Check | Out-Null
        Assert-Maintenance ((Get-Item -LiteralPath $ledgerPath).LastWriteTimeUtc -eq $stamp -and (Get-FileHash -LiteralPath $ledgerPath).Hash -ceq $hash) 'Idempotent sync rewrote the ledger.'
        $bytes = [IO.File]::ReadAllBytes($ledgerPath)
        Assert-Maintenance (($bytes[0] -eq 239) -eq ($newline -eq "`r`n")) 'BOM was changed.'
        [IO.File]::WriteAllText($updatePath, $finished.Replace('`verified`', '`complete`'), $utf8)
        Assert-LedgerRejects { & $syncPath -UpdatePath $updatePath } 'Invalid metadata'
        [IO.File]::WriteAllText($updatePath, $finished.Replace('`2026-09-07`', '`2026-09-08`'), $utf8)
        Assert-LedgerRejects { & $syncPath -UpdatePath $updatePath } 'Invalid metadata'
        [IO.File]::WriteAllText($updatePath, $finished, $utf8)
        $duplicatePath = Join-Path $yearDir.FullName '20260907-0001-duplicate.md'
        Copy-Item -LiteralPath $updatePath -Destination $duplicatePath
        Assert-LedgerRejects { & $syncPath -UpdatePath $updatePath } 'duplicate Update ID'
        Remove-Item -LiteralPath $duplicatePath
        [IO.File]::AppendAllText($ledgerPath, $newline + $row, $utf8)
        Assert-LedgerRejects { & $syncPath -UpdatePath $updatePath } 'Duplicate monthly rows'
        Assert-LedgerRejects { & $syncPath -UpdatePath (Join-Path $fixtureRoot '20260907-0001-outside.md') } 'must be in this repository'
    }
    Write-Output 'PASS: ledger creation, status updates, byte preservation, idempotence and invalid-input rejection.'

    # An archived body retains its ID. A legacy alias is not a second Update.
    [IO.File]::WriteAllText($ledgerPath, $expected, $utf8)
    $archiveDir = New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'docs\archive\updates\2026') -Force
    $archivedPath = Join-Path $archiveDir.FullName ([IO.Path]::GetFileName($updatePath))
    Copy-Item -LiteralPath $updatePath -Destination $archivedPath
    [IO.File]::WriteAllText($updatePath, '<!-- archived-document -->', $utf8)
    $manifestDir = New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'docs\archive\migrations') -Force
    $manifest = @{ files = @(@{ source = 'docs/updates/2026/20260907-0001-fixture.md'; current = 'docs/archive/updates/2026/20260907-0001-fixture.md' }) }
    [IO.File]::WriteAllText((Join-Path $manifestDir.FullName '20260908-workspace.json'), ($manifest | ConvertTo-Json -Depth 5), $utf8)
    Assert-LedgerRejects { & $syncPath -UpdatePath $updatePath -Check } 'stale or missing'
    & $syncPath -UpdatePath $updatePath | Out-Null
    & $syncPath -UpdatePath $archivedPath -Check | Out-Null
    Assert-Maintenance ([IO.File]::ReadAllText($ledgerPath).Contains('](../archive/updates/2026/20260907-0001-fixture.md)')) 'Monthly row did not link to the complete archived body.'
    $duplicatePath = Join-Path $yearDir.FullName '20260907-0001-duplicate.md'
    Copy-Item -LiteralPath $archivedPath -Destination $duplicatePath
    Assert-LedgerRejects { & $syncPath -UpdatePath $archivedPath } 'duplicate Update ID'
    Remove-Item -LiteralPath $duplicatePath
    Write-Output 'PASS: archived body, old-path input, alias exclusion, current monthly link and duplicate ID across both locations.'

    if (-not $LedgerOnly) {
        $catalogText = [IO.File]::ReadAllText((Join-Path $repo 'tools\release\dtmapi-product-catalog.json'))
        $catalogPath = Join-Path $fixtureRoot 'catalog.json'
        $candidate = $catalogText | ConvertFrom-Json
        $candidate.statusDate = '2026-09-08'
        Assert-CatalogFixture
        $registryPath = Join-Path $fixtureRoot 'admission.md'
        & (Join-Path $PSScriptRoot 'generate-managed-product-admission-registry.ps1') -CatalogPath $catalogPath -OutputPath $registryPath -Quiet
        $actualRegistry = [IO.File]::ReadAllText((Join-Path $repo 'docs\architecture\managed-product-admission-registry.md')).Replace("`r`n", "`n")
        Assert-Maintenance ([IO.File]::ReadAllText($registryPath).Replace("`r`n", "`n") -ceq $actualRegistry) 'Date-only changes altered admission projection.'
        Write-Output 'PASS: a valid Catalog date change needs no constant or admission-document edit.'

        $candidate = $catalogText | ConvertFrom-Json
        $product = $candidate.products | Where-Object { $_.catalogId -ceq 'more-equipment-slots' }
        $product.sourceVersion = '1.0.2'
        $product.targetVersion = '1.0.2'
        Assert-CatalogFixture -RequiredFailures @('more-equipment-slots manifest Version') -ForbiddenFailures @('Public identity/path', 'candidate/source version')
        Write-Output 'PASS: source drift is rejected without a mutable-version identity-freeze failure.'

        $candidate = $catalogText | ConvertFrom-Json
        $candidate.statusDate = '2026-02-30'
        $candidate.releaseStop.state = 'Unrestricted'
        $product = $candidate.products | Where-Object { $_.catalogId -ceq 'more-equipment-slots' }
        $product.targetVersion = '1.0.2'
        $product.officialFolder = 'InvalidIdentityPath'
        $product.retainedArtifact.bytes = 1
        Assert-CatalogFixture -RequiredFailures @('ISO calendar date', 'Release stop state', 'candidate/source version', 'Public identity/path frozen digest', 'Retained published artifact frozen digest')
        Write-Output 'PASS: invalid date, candidate mismatch, identity/path drift, retained-evidence drift and release-stop drift remain rejected.'
    }
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolvedRoot.StartsWith($tempBase.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup escaped the test root.' }
    if (Test-Path -LiteralPath $resolvedRoot) {
        $links = @(Get-Item -LiteralPath $resolvedRoot; Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force) | Where-Object { ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 }
        if (@($links).Count -gt 0) { throw 'Refusing fixture cleanup through a reparse point.' }
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}
