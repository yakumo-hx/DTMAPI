Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'document-paths.ps1')
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$boundary = Join-Path $repo 'tmp\test-runs'
$fixture = Join-Path $boundary ('document-archive-' + [guid]::NewGuid().ToString('N'))
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Write-ArchiveFixture {
    param([string] $Relative, [string] $Text)
    $path = Join-Path $fixture $Relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [IO.File]::WriteAllText($path, $Text, $utf8)
}

try {
    $first = Join-Path $fixture '源 仓库'
    $second = Join-Path $fixture 'second'
    $old = 'docs/updates/2026/20260101-0001-history.md'
    $current = 'docs/archive/updates/2026/20260101-0001-history.md'
    Write-ArchiveFixture ('源 仓库/' + $old) '<!-- archived-document -->'
    Write-ArchiveFixture ('源 仓库/' + $current) '# Complete historical observation and failure'
    Write-ArchiveFixture '源 仓库/docs/updates/INDEX-2026-01.md' '[body](../archive/updates/2026/20260101-0001-history.md)'
    Write-ArchiveFixture '源 仓库/docs/archive/reviews/manual-qa/2026/原始附件.txt' 'Unique review attachment'
    Write-ArchiveFixture '源 仓库/docs/archive/migrations/20260908-workspace.json' (@{ files = @(@{ source = $old; current = $current }) } | ConvertTo-Json -Depth 5)
    Write-ArchiveFixture ('second/' + $old) '# Independent repository'

    if ((Resolve-DtmApiDocumentPath $first $old) -ine (Join-Path $first $current)) { throw 'Old identity did not resolve to the body.' }
    if ((Resolve-DtmApiDocumentPath $second $old) -ine (Join-Path $second $old)) { throw 'Location cache leaked across repositories.' }
    $records = @(Get-DtmApiUpdateRecordFiles $first)
    if ($records.Count -ne 1 -or $records[0].FullName -ine (Join-Path $first $current)) { throw 'Alias was counted as a body.' }
    $escaped = $false
    try { Resolve-DtmApiDocumentPath $first '../outside.md' | Out-Null } catch { $escaped = $true }
    if (-not $escaped) { throw 'Escaping document path was accepted.' }

    # Import only the pure copy functions, not the package builder entrypoint.
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'update-audit-package.ps1'), [ref]$tokens, [ref]$errors)
    if ($errors.Count -gt 0) { throw $errors[0] }
    foreach ($name in @('Get-RelativePath', 'Copy-Tree', 'Copy-AuditDocs', 'Copy-FilePreservingPath', 'Test-IsExcludedSourceFile', 'Copy-TrackedSourceSnapshot')) {
        $definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
        if ($null -eq $definition) { throw "Missing copy function: $name" }
        . ([scriptblock]::Create($definition.Extent.Text))
    }
    $package = Join-Path $fixture 'package'
    & git -C $first init -q
    if ($LASTEXITCODE -ne 0) { throw 'Could not initialize the source-snapshot fixture.' }
    & git -C $first add -- docs
    if ($LASTEXITCODE -ne 0) { throw 'Could not track the source-snapshot fixture.' }
    Copy-TrackedSourceSnapshot -Root $first -Destination $package
    Copy-AuditDocs -Root $first -PackageRoot $package
    foreach ($relative in @('archive/updates/2026/20260101-0001-history.md', 'archive/reviews/manual-qa/2026/原始附件.txt', 'updates/INDEX-2026-01.md', 'archive/migrations/20260908-workspace.json')) {
        $source = Join-Path $first ('docs/' + $relative)
        $copy = Join-Path $package ('docs/' + $relative)
        if (-not (Test-Path -LiteralPath $copy) -or (Get-FileHash -LiteralPath $source).Hash -cne (Get-FileHash -LiteralPath $copy).Hash) {
            throw "Audit package lost or rewrote the archived body/attachment: $relative"
        }
    }
    if (@(Get-ChildItem -LiteralPath (Join-Path $package 'audit/docs') -File -Recurse).Count -ne 1) { throw 'Audit docs duplicated canonical bodies.' }
    Write-Output 'PASS: document resolution, repository cache boundary, alias exclusion, path rejection, and audit body/attachment byte preservation.'
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    if (-not $resolved.StartsWith($boundary.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup escaped fixture boundary.' }
    if (Test-Path -LiteralPath $resolved) {
        $links = @(Get-Item -LiteralPath $resolved; Get-ChildItem -LiteralPath $resolved -Force -Recurse) | Where-Object { ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 }
        if (@($links).Count -gt 0) { throw 'Refusing cleanup through a reparse point.' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
