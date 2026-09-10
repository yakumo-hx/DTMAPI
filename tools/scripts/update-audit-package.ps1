param(
    [string]$SourceRoot,
    [string]$OutputRoot,
    [string]$BranchName,
    [string]$SourceCommit,
    [string[]]$EvidenceIds,
    [string[]]$ReportZipPaths,
    [string]$ReverseSnippetsSource,
    [switch]$FullOnly,
    [switch]$WebOnly,
    [switch]$SkipZip,
    [switch]$SelfAuditOnly,
    [switch]$LegacyEvidenceSet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-SourceRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        $Path = Join-Path $PSScriptRoot "..\.."
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Get-GitValue {
    param(
        [string]$Root,
        [string[]]$Arguments,
        [string]$Fallback
    )

    try {
        $value = & git -C $Root @Arguments 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($value)) {
            return ($value | Select-Object -First 1).Trim()
        }
    }
    catch {
        return $Fallback
    }

    return $Fallback
}

function New-CleanDirectory {
    param([string]$Path)

    Assert-AuditOutputPath -Path $Path
    if (Test-Path -LiteralPath $Path) {
        throw "A previous audit staging directory exists; retain it and use a fresh output root: $Path"
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Get-RelativePath {
    param(
        [string]$BasePath,
        [string]$Path
    )

    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath)
    if (-not $baseFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseFullPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $pathFullPath = [System.IO.Path]::GetFullPath($Path)
    $baseUri = [System.Uri]::new($baseFullPath)
    $pathUri = [System.Uri]::new($pathFullPath)
    $relativeUri = $baseUri.MakeRelativeUri($pathUri)
    $relative = [System.Uri]::UnescapeDataString($relativeUri.ToString())
    return $relative -replace "/", [System.IO.Path]::DirectorySeparatorChar
}

function Copy-FilePreservingPath {
    param(
        [string]$SourceRoot,
        [string]$DestinationRoot,
        [string]$RelativePath
    )

    $source = Join-Path $SourceRoot $RelativePath
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        return
    }

    $destination = Join-Path $DestinationRoot $RelativePath
    $destinationDirectory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

function Copy-Tree {
    param(
        [string]$Source,
        [string]$Destination,
        [scriptblock]$IncludeFile
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        return
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    Get-ChildItem -LiteralPath $Source -Recurse -Force -File | ForEach-Object {
        if ($IncludeFile -and -not (& $IncludeFile $_)) {
            return
        }

        $relative = Get-RelativePath -BasePath $Source -Path $_.FullName
        $target = Join-Path $Destination $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $target -Force
    }
}

function Test-IsExcludedSourceFile {
    param([string]$RelativePath)

    $normalized = $RelativePath -replace "\\", "/"
    $fileName = [System.IO.Path]::GetFileName($normalized)
    $extension = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()

    if ($normalized -match "(^|/)(bin|obj|\.tools)(/|$)") {
        return $true
    }

    if ($normalized -like "docs/debug/evidence/*") {
        return $true
    }

    if ($normalized -like ".codex/wiki-maintenance/*") {
        return $true
    }

    if ($normalized -like "references/doloc-town/reverse/builds/*") {
        return $true
    }

    if ($fileName -eq ".DS_Store") {
        return $true
    }

    if ($extension -in @(".dll", ".exe", ".pdb", ".nupkg", ".zip", ".rar", ".7z")) {
        return $true
    }

    return $false
}

function Copy-TrackedSourceSnapshot {
    param(
        [string]$Root,
        [string]$Destination
    )

    $files = & git -C $Root -c core.quotepath=false ls-files
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed for $Root"
    }

    foreach ($file in $files) {
        if (Test-IsExcludedSourceFile -RelativePath $file) {
            continue
        }

        Copy-FilePreservingPath -SourceRoot $Root -DestinationRoot $Destination -RelativePath $file
    }

    $scriptRelative = "tools/scripts/update-audit-package.ps1"
    Copy-FilePreservingPath -SourceRoot $Root -DestinationRoot $Destination -RelativePath $scriptRelative
}

function Copy-AuditDocs {
    param(
        [string]$Root,
        [string]$PackageRoot
    )

    # The tracked source snapshot already contains the canonical docs and archived
    # attachments. A second copy makes both links and status ownership ambiguous.
    $auditDocs = Join-Path $PackageRoot 'audit/docs'
    New-Item -ItemType Directory -Force -Path $auditDocs | Out-Null
    @('# Audit document routes', '',
      '- [Documentation](../../docs/README.md)',
      '- [Issues](../../docs/debug/issues/README.md)',
      '- [Updates](../../docs/updates/README.md)',
      '- [History](../../docs/archive/README.md)',
      '- [Knowledge](../../docs/knowledge/README.md)', '',
      'Bodies and attachments retain their repository paths under docs/. This directory contains navigation only.') |
        Set-Content -LiteralPath (Join-Path $auditDocs 'README.md') -Encoding UTF8
}

function Copy-ReverseSnippets {
    param(
        [string]$Source,
        [string]$PackageRoot
    )

    $destination = Join-Path $PackageRoot "audit/reverse-snippets"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null

    if (-not [string]::IsNullOrWhiteSpace($Source) -and (Test-Path -LiteralPath $Source)) {
        Copy-Tree -Source $Source -Destination $destination -IncludeFile {
            param($item)
            $extension = $item.Extension.ToLowerInvariant()
            return $extension -in @(".md", ".csv", ".txt")
        }
        return
    }

    @(
        "# Reverse Snippet Note",
        "",
        "No reverse snippet source was available when this package was generated.",
        "Regenerate with -ReverseSnippetsSource pointing at the previous approved snippet snapshot if these files are required for review."
    ) | Set-Content -LiteralPath (Join-Path $destination "README.md") -Encoding UTF8
}

function Copy-AuditTools {
    param(
        [string]$Root,
        [string]$PackageRoot
    )

    $destination = Join-Path $PackageRoot "audit/tools/scripts"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    @('# Audit tool routes', '', '[Canonical scripts and their modules](../../../tools/scripts/README.md) remain together under tools/scripts/. Do not copy a runner away from its modules.') |
        Set-Content -LiteralPath (Join-Path $destination 'README.md') -Encoding UTF8
}

function Get-DefaultEvidence {
    return @(
        [pscustomobject]@{ Id = "20260612-000310"; Label = "Release hygiene Manager MVP install-state smoke" },
        [pscustomobject]@{ Id = "20260612-000435"; Label = "Release hygiene HookProbe runtime smoke" },
        [pscustomobject]@{ Id = "20260612-000606"; Label = "Release hygiene Zoom smoke" },
        [pscustomobject]@{ Id = "20260612-000817"; Label = "Release hygiene ActionSpeed smoke" },
        [pscustomobject]@{ Id = "20260612-000929"; Label = "Release hygiene OneAction smoke" },
        [pscustomobject]@{ Id = "20260612-001041"; Label = "Release hygiene ChestLocator smoke" },
        [pscustomobject]@{ Id = "20260612-001151"; Label = "Release hygiene Y-key console smoke" },
        [pscustomobject]@{ Id = "20260612-001359"; Label = "Release hygiene FishRoe/AnimalViewer rendering smoke" },
        [pscustomobject]@{ Id = "20260612-001516"; Label = "Release hygiene Animal panel UI smoke" },
        [pscustomobject]@{ Id = "20260612-002033"; Label = "Release hygiene Steam launch HookProbe smoke" },
        [pscustomobject]@{ Id = "20260612-015402"; Label = "Release hygiene hardening MoreSaves official save UI smoke" },
        [pscustomobject]@{ Id = "20260612-015716"; Label = "Release hygiene hardening Manager MVP install/report smoke" },
        [pscustomobject]@{ Id = "20260612-015832"; Label = "Release hygiene hardening HookProbe runtime smoke" }
    )
}

function Get-EvidenceItems {
    param([string[]]$Ids)

    if ($Ids -and $Ids.Count -gt 0) {
        return $Ids | ForEach-Object {
            if ($_ -notmatch '^\d{8}-\d{6}(?:-[A-Za-z0-9_-]+)?$') { throw "Invalid smoke evidence id: $_" }
            [pscustomobject]@{ Id = $_; Label = "User-selected smoke evidence" }
        }
    }

    if ($LegacyEvidenceSet) { return Get-DefaultEvidence }
    return @()
}

function Copy-Evidence {
    param(
        [string]$Root,
        [string]$PackageRoot,
        [object[]]$Items,
        [bool]$Compact
    )

    $sourceBase = Join-Path $Root "docs/debug/evidence/GAME-SMOKE"
    $destinationBase = Join-Path $PackageRoot "audit/evidence/GAME-SMOKE"
    New-Item -ItemType Directory -Path $destinationBase -Force | Out-Null

    foreach ($item in $Items) {
        $source = Join-Path $sourceBase $item.Id
        $destination = Join-Path $destinationBase $item.Id
        if (-not (Test-Path -LiteralPath $source)) {
            Write-Warning "Evidence directory not found: $source"
            continue
        }

        if ($Compact) {
            Copy-Tree -Source $source -Destination $destination -IncludeFile {
                param($file)
                $relative = (Get-RelativePath -BasePath $source -Path $file.FullName) -replace "\\", "/"
                if ($relative -like "DTMAPI-evidence/*") {
                    return $false
                }

                $extension = $file.Extension.ToLowerInvariant()
                if ($extension -in @(".zip", ".rar", ".7z", ".nupkg", ".dll", ".exe", ".pdb")) {
                    return $false
                }

                return $true
            }
        }
        else {
            Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
        }
    }
}

function Copy-ReportPayloads {
    param(
        [string]$Root,
        [string]$PackageRoot,
        [object[]]$Items,
        [string[]]$ExplicitReportZipPaths,
        [string]$PreviousReportPath
    )

    $destination = Join-Path $PackageRoot "audit/report"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null

    $reportCandidates = New-Object System.Collections.Generic.List[string]

    foreach ($item in $Items) {
        $zip = Join-Path $Root ("docs/debug/evidence/GAME-SMOKE/" + $item.Id + ".zip")
        if (Test-Path -LiteralPath $zip) {
            $reportCandidates.Add((Resolve-Path -LiteralPath $zip).Path)
        }
    }

    if ($ExplicitReportZipPaths) {
        foreach ($path in $ExplicitReportZipPaths) {
            if (Test-Path -LiteralPath $path) {
                $reportCandidates.Add((Resolve-Path -LiteralPath $path).Path)
            }
        }
    }

    if ($LegacyEvidenceSet -and -not [string]::IsNullOrWhiteSpace($PreviousReportPath) -and (Test-Path -LiteralPath $PreviousReportPath)) {
        Get-ChildItem -LiteralPath $PreviousReportPath -File -Filter "*.zip" | ForEach-Object {
            $reportCandidates.Add($_.FullName)
        }
    }

    $seen = New-Object System.Collections.Generic.HashSet[string]
    foreach ($path in $reportCandidates) {
        if ($seen.Add($path)) {
            $target = Join-Path $destination ([System.IO.Path]::GetFileName($path))
            if (Test-Path -LiteralPath $target) {
                if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash) { throw "Different report ZIPs share a filename: $path / $target" }
                continue
            }
            Copy-Item -LiteralPath $path -Destination $target
        }
    }
}

function Write-PackageMarkdown {
    param(
        [string]$PackageRoot,
        [string]$PackageKind,
        [string]$Branch,
        [string]$Commit,
        [object[]]$Items,
        [bool]$Compact
    )

    $title = if ($Compact) { "DTMAPI Refactor Web Upload Audit Package" } else { "DTMAPI Refactor Full Audit Package" }
    $status = if ($Compact) {
        "Compact web-upload package generated from the current Refactor audit source."
    }
    else {
        "Full audit package generated from the current Refactor audit source."
    }

    $evidenceLines = foreach ($item in $Items) {
        "- GAME-SMOKE/$($item.Id) $($item.Label)"
    }

    $reportLine = if ($Compact) {
        "- Web package: keeps source/docs/key logs/result files and omits screenshot-heavy DTMAPI-evidence directories plus report zip payloads. See audit/report/WEB-REPORT-NOTE.md."
    }
    else {
        "- Full package: keeps complete selected smoke evidence directories, including DTMAPI-evidence directories, and includes report zip payloads under audit/report/."
    }

    $content = @(
        "# $title",
        "",
        "Status: $status",
        "",
        "## Source",
        "",
        "- Branch: $Branch",
        ("- Source commit: ``{0}``" -f $Commit),
        "- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss K')",
        "- Package kind: $PackageKind",
        "",
        "## Included Audit Contents",
        "",
        "- Buildable source snapshot: src/, products/, author-sdk/, tests/, tools/, assets/, archive/legacy-product-assets/, root solution/project metadata, and public reference docs tracked in git.",
        "- Canonical current/history docs and attachments under docs/; audit/docs contains navigation only.",
        "- Reverse snippet maps under audit/reverse-snippets/.",
        "- Complete tracked scripts and dependent modules under tools/scripts/.",
        "- Smoke evidence ids:"
    ) + $evidenceLines + @(
        "",
        "## Package Split",
        "",
        $reportLine,
        "",
        "## Boundary",
        "",
        "The package excludes .git, .tools, bin, obj, build binaries, NuGet packages, general archives outside audit/report/, official DLL/EXE/PDB files, reverse input/build decompiled source, private own-mod sources, Stardew SMAPI source, and third-party mod binaries."
    )

    $content | Set-Content -LiteralPath (Join-Path $PackageRoot "AUDIT-PACKAGE.md") -Encoding UTF8
}

function Write-ValidationSummary {
    param(
        [string]$PackageRoot,
        [string]$Branch,
        [string]$Commit,
        [object[]]$Items,
        [bool]$Compact
    )

    $evidenceLines = foreach ($item in $Items) {
        "- $($item.Label): GAME-SMOKE/$($item.Id)."
    }

    $content = @(
        "# Validation Summary",
        "",
        "- Source branch: $Branch.",
        "- Source commit: $Commit.",
        "- This packaging command does not build or test the source. Test results remain in the owning Update and explicitly selected evidence.",
        "- An omitted evidence selection creates a source/document audit package with no runtime validation claim.",
        "- Package self-audit: AUDIT-PACKAGE.md, VALIDATION-SUMMARY.md, and report notes are scanned for control characters and hashtable interpolation text.",
        "- Package cleanup audit: no .git, .tools, bin, or obj directories are allowed in the package snapshot.",
        "- Package binary audit: no DLL/EXE/PDB/NuGet/RAR/7Z payloads are allowed; zip payloads are allowed only under audit/report in the full package.",
        "- Package type: $(if ($Compact) { 'web upload compact package' } else { 'full audit package' }).",
        "",
        "## Evidence",
        ""
    ) + $evidenceLines

    $content | Set-Content -LiteralPath (Join-Path $PackageRoot "VALIDATION-SUMMARY.md") -Encoding UTF8
}

function Write-ReportNotes {
    param(
        [string]$PackageRoot,
        [object[]]$Items,
        [bool]$Compact
    )

    $reportDirectory = Join-Path $PackageRoot "audit/report"
    New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

    $evidenceLines = foreach ($item in $Items) {
        "- GAME-SMOKE/$($item.Id) $($item.Label)"
    }

    if ($Compact) {
        @(
            "# Web Report Note",
            "",
            "This web-upload package intentionally omits complete report zips and screenshot-heavy DTMAPI-evidence folders to stay small enough for browser upload.",
            "",
            "The full audit package contains complete evidence directories for all listed smoke ids and report zip payloads under audit/report/.",
            "",
            "Compact logs, result.json, startup analysis, Unity-Player.log, BepInEx-LogOutput.log, process check, and fatal-window check are preserved under audit/evidence/GAME-SMOKE/.",
            "",
            "Report zip payloads are omitted from this web package; fresh report pointers such as latest-report.txt are preserved with the compact smoke evidence when the smoke exported a report.",
            "",
            "## Evidence",
            ""
        ) + $evidenceLines | Set-Content -LiteralPath (Join-Path $reportDirectory "WEB-REPORT-NOTE.md") -Encoding UTF8
    }
    else {
        $reportFreshnessLine = "Report zip payloads should match the listed smoke ids or explicit report paths used when generating the full package."
        @(
            "# Report Payload Note",
            "",
            "The full package keeps complete selected audit/evidence/GAME-SMOKE/<id>/ directories. The compact web package keeps only key logs, result.json, startup analysis, Player/BepInEx logs, process checks, and fatal-window checks.",
            "",
            "Report zip payloads, when available, are kept under this audit/report directory.",
            "",
            $reportFreshnessLine,
            "",
            "Evidence included in this package:",
            ""
        ) + $evidenceLines | Set-Content -LiteralPath (Join-Path $reportDirectory "REPORT-NOTE.md") -Encoding UTF8
    }
}

function Assert-NoBadMarkdownText {
    param([string]$PackageRoot)

    $markdownFiles = @(
        (Join-Path $PackageRoot "AUDIT-PACKAGE.md"),
        (Join-Path $PackageRoot "VALIDATION-SUMMARY.md"),
        (Join-Path $PackageRoot "audit/report/REPORT-NOTE.md"),
        (Join-Path $PackageRoot "audit/report/WEB-REPORT-NOTE.md")
    ) | Where-Object { Test-Path -LiteralPath $_ }

    foreach ($file in $markdownFiles) {
        $text = Get-Content -LiteralPath $file -Raw
        if ($text -match "System\.Collections\.Hashtable") {
            throw "Package markdown contains hashtable interpolation text: $file"
        }

        foreach ($character in $text.ToCharArray()) {
            $code = [int][char]$character
            if ($code -lt 32 -and $code -notin @(10, 13)) {
                throw ("Package markdown contains control character 0x{0:X2}: {1}" -f $code, $file)
            }
        }
    }
}

function Assert-NoDisallowedPayloads {
    param(
        [string]$PackageRoot,
        [bool]$Compact
    )

    $badDirectories = Get-ChildItem -LiteralPath $PackageRoot -Force -Recurse -Directory | Where-Object {
        $_.Name -in @(".git", ".tools", "bin", "obj")
    }

    if ($badDirectories) {
        throw "Package contains disallowed directories: $($badDirectories.FullName -join ', ')"
    }

    $badFiles = Get-ChildItem -LiteralPath $PackageRoot -Force -Recurse -File | Where-Object {
        $relative = (Get-RelativePath -BasePath $PackageRoot -Path $_.FullName) -replace "\\", "/"
        $extension = $_.Extension.ToLowerInvariant()

        if ($extension -in @(".dll", ".exe", ".pdb", ".nupkg", ".rar", ".7z")) {
            return $true
        }

        if ($extension -eq ".zip") {
            if ($Compact) {
                return $true
            }

            return -not ($relative -like "audit/report/*")
        }

        return $false
    }

    if ($badFiles) {
        throw "Package contains disallowed binary/archive payloads: $($badFiles.FullName -join ', ')"
    }

    if ($Compact) {
        $screenshotEvidence = Get-ChildItem -LiteralPath $PackageRoot -Force -Recurse -Directory | Where-Object {
            $_.Name -eq "DTMAPI-evidence"
        }

        if ($screenshotEvidence) {
            throw "Web package contains screenshot-heavy DTMAPI-evidence directories: $($screenshotEvidence.FullName -join ', ')"
        }
    }
}

function Get-EvidenceIdsFromMarkdown {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return @()
    }

    $matches = [System.Text.RegularExpressions.Regex]::Matches((Get-Content -LiteralPath $Path -Raw), "GAME-SMOKE/([0-9]{8}-[0-9]{6})")
    return @($matches | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
}

function Assert-EvidenceReferences {
    param([string]$PackageRoot)

    $files = @(
        (Join-Path $PackageRoot "AUDIT-PACKAGE.md"),
        (Join-Path $PackageRoot "VALIDATION-SUMMARY.md"),
        (Join-Path $PackageRoot "audit/report/WEB-REPORT-NOTE.md"),
        (Join-Path $PackageRoot "audit/report/REPORT-NOTE.md")
    ) | Where-Object { Test-Path -LiteralPath $_ }

    $expected = $null
    foreach ($file in $files) {
        $ids = @(Get-EvidenceIdsFromMarkdown -Path $file)
        if ($ids.Count -eq 0) {
            continue
        }

        if ($null -eq $expected) {
            $expected = $ids
            continue
        }

        $left = $expected -join "|"
        $right = $ids -join "|"
        if ($left -ne $right) {
            throw "Package evidence list mismatch between markdown files. Expected '$left' but $file lists '$right'."
        }
    }

    if ($null -eq $expected -or $expected.Count -eq 0) { return }

    foreach ($id in $expected) {
        $evidenceDir = Join-Path $PackageRoot ("audit/evidence/GAME-SMOKE/" + $id)
        if (-not (Test-Path -LiteralPath $evidenceDir -PathType Container)) {
            throw "Listed evidence directory is missing: $evidenceDir"
        }

        $resultPath = Join-Path $evidenceDir "result.json"
        if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
            throw "Listed evidence is missing result.json: $resultPath"
        }
    }
}

function Invoke-PackageSelfAudit {
    param(
        [string]$PackageRoot,
        [bool]$Compact
    )

    if (-not (Test-Path -LiteralPath $PackageRoot)) {
        throw "Package path not found: $PackageRoot"
    }

    Assert-NoBadMarkdownText -PackageRoot $PackageRoot
    Assert-NoDisallowedPayloads -PackageRoot $PackageRoot -Compact $Compact
    Assert-EvidenceReferences -PackageRoot $PackageRoot

    foreach ($relative in @("AUDIT-PACKAGE.md", "VALIDATION-SUMMARY.md", "src", "products", "author-sdk", "tests", "tools/scripts/build.ps1", "docs/api/public-api-matrix.md", "docs/hook-map/README.md", "docs/debug/regressions/smoke-matrix.md")) {
        $path = Join-Path $PackageRoot $relative
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required package path missing: $path"
        }
    }
}

function New-AuditPackage {
    param(
        [string]$Root,
        [string]$PackageRoot,
        [string]$Kind,
        [string]$Branch,
        [string]$Commit,
        [object[]]$Items,
        [string]$ReverseSource,
        [bool]$Compact,
        [string[]]$ExplicitReportZipPaths,
        [string]$PreviousReportPath
    )

    New-CleanDirectory -Path $PackageRoot
    Copy-TrackedSourceSnapshot -Root $Root -Destination $PackageRoot
    Copy-AuditDocs -Root $Root -PackageRoot $PackageRoot
    Copy-ReverseSnippets -Source $ReverseSource -PackageRoot $PackageRoot
    Copy-AuditTools -Root $Root -PackageRoot $PackageRoot
    Copy-Evidence -Root $Root -PackageRoot $PackageRoot -Items $Items -Compact $Compact

    if (-not $Compact) {
        Copy-ReportPayloads -Root $Root -PackageRoot $PackageRoot -Items $Items -ExplicitReportZipPaths $ExplicitReportZipPaths -PreviousReportPath $PreviousReportPath
    }
    else {
        New-Item -ItemType Directory -Path (Join-Path $PackageRoot "audit/report") -Force | Out-Null
    }

    Write-PackageMarkdown -PackageRoot $PackageRoot -PackageKind $Kind -Branch $Branch -Commit $Commit -Items $Items -Compact $Compact
    Write-ValidationSummary -PackageRoot $PackageRoot -Branch $Branch -Commit $Commit -Items $Items -Compact $Compact
    Write-ReportNotes -PackageRoot $PackageRoot -Items $Items -Compact $Compact
    Invoke-PackageSelfAudit -PackageRoot $PackageRoot -Compact $Compact
}

function Replace-PackageDirectory {
    param(
        [string]$TemporaryPath,
        [string]$FinalPath
    )

    $backupPath = $FinalPath + ".bak"
    foreach ($path in @($TemporaryPath,$FinalPath,$backupPath)) { Assert-AuditOutputPath -Path $path }
    if (Test-Path -LiteralPath $backupPath) {
        throw "Retained previous audit package exists. Use a fresh output root or review it through the cleanup plan: $backupPath"
    }

    if (Test-Path -LiteralPath $FinalPath) {
        Move-Item -LiteralPath $FinalPath -Destination $backupPath
    }

    try { Move-Item -LiteralPath $TemporaryPath -Destination $FinalPath -ErrorAction Stop }
    catch {
        if (-not (Test-Path -LiteralPath $FinalPath) -and (Test-Path -LiteralPath $backupPath)) { Move-Item -LiteralPath $backupPath -Destination $FinalPath -ErrorAction Stop }
        throw
    }
    # A previous delivery may contain unique selected evidence. Keep it until a
    # concrete canonical-copy/rebuildability review authorizes cleanup.
}

function Write-PackageZip {
    param([string]$PackagePath)

    $zipPath = $PackagePath + ".zip"
    Assert-AuditOutputPath -Path $zipPath
    $temporaryZip = $PackagePath + '.new.zip'
    $previousZip = $zipPath + '.bak'
    foreach ($path in @($temporaryZip,$previousZip)) {
        Assert-AuditOutputPath -Path $path
        if (Test-Path -LiteralPath $path) { throw "Audit ZIP staging/previous delivery already exists: $path" }
    }
    Compress-Archive -LiteralPath $PackagePath -DestinationPath $temporaryZip -CompressionLevel Optimal
    if (Test-Path -LiteralPath $zipPath) { Move-Item -LiteralPath $zipPath -Destination $previousZip }
    try { Move-Item -LiteralPath $temporaryZip -Destination $zipPath -ErrorAction Stop }
    catch { if (-not (Test-Path -LiteralPath $zipPath) -and (Test-Path -LiteralPath $previousZip)) { Move-Item -LiteralPath $previousZip -Destination $zipPath }; throw }
}

function Assert-AuditOutputPath {
    param([string]$Path)
    $full=[IO.Path]::GetFullPath($Path)
    $prefix=$script:resolvedOutputRoot.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { throw "Audit output escapes its declared root: $full" }
    foreach ($inputRoot in @($script:resolvedSourceRoot)) {
        $inputFull=[IO.Path]::GetFullPath($inputRoot).TrimEnd('\','/')
        if ($full.Equals($inputFull,[StringComparison]::OrdinalIgnoreCase) -or $inputFull.StartsWith($full.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Audit output contains the source repository.' }
    }
    $ancestor=$full
    while ($ancestor) {
        if ((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Audit output traverses a reparse point: $ancestor" }
        $ancestor=Split-Path -Parent $ancestor
    }
    if ((Test-Path -LiteralPath $full -PathType Container) -and @(Get-ChildItem -LiteralPath $full -Force -Recurse | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -gt 0) { throw "Audit output contains a reparse point: $full" }
}

$resolvedSourceRoot = Resolve-SourceRoot -Path $SourceRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $resolvedSourceRoot 'dist/audit-packages'
}
$resolvedOutputRoot = if ([IO.Path]::IsPathRooted($OutputRoot)) { [IO.Path]::GetFullPath($OutputRoot) } else { [IO.Path]::GetFullPath((Join-Path $resolvedSourceRoot $OutputRoot)) }
foreach ($protected in @('src','products','author-sdk','tests','tools','docs','references','.git','.tools')) {
    $inputPath=Join-Path $resolvedSourceRoot $protected
    if ($resolvedOutputRoot.Equals($inputPath,[StringComparison]::OrdinalIgnoreCase) -or $resolvedOutputRoot.StartsWith($inputPath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw "Audit output is inside a source/input directory: $resolvedOutputRoot" }
}
if ($FullOnly -and $WebOnly) { throw 'Select FullOnly or WebOnly, not both.' }

if ([string]::IsNullOrWhiteSpace($BranchName)) {
    $BranchName = Get-GitValue -Root $resolvedSourceRoot -Arguments @("branch", "--show-current") -Fallback "unknown"
}

if ([string]::IsNullOrWhiteSpace($SourceCommit)) {
    $SourceCommit = Get-GitValue -Root $resolvedSourceRoot -Arguments @("rev-parse", "--short", "HEAD") -Fallback "unknown"
}

$fullPackagePath = Join-Path $resolvedOutputRoot "DTMAPI-audit-package-Refactor"
$webPackagePath = Join-Path $resolvedOutputRoot "DTMAPI-audit-package-Refactor-web"
$fullTemporaryPath = $fullPackagePath + ".new"
$webTemporaryPath = $webPackagePath + ".new"

if ($LegacyEvidenceSet -and [string]::IsNullOrWhiteSpace($ReverseSnippetsSource)) {
    $candidate = Join-Path $fullPackagePath "audit/reverse-snippets"
    if (Test-Path -LiteralPath $candidate) {
        $ReverseSnippetsSource = $candidate
    }
}

$previousReportPath = Join-Path $fullPackagePath "audit/report"
$items = @(Get-EvidenceItems -Ids $EvidenceIds)
if (-not $SelfAuditOnly) {
    foreach ($item in $items) {
        $result = Join-Path $resolvedSourceRoot ('docs/debug/evidence/GAME-SMOKE/' + $item.Id + '/result.json')
        if (-not (Test-Path -LiteralPath $result -PathType Leaf)) { throw "Selected smoke evidence has no result.json: $result" }
    }
    foreach ($report in $ReportZipPaths) {
        if (-not (Test-Path -LiteralPath $report -PathType Leaf) -or [IO.Path]::GetExtension($report) -ine '.zip') { throw "Selected report is not an existing ZIP: $report" }
    }
}

if ($SelfAuditOnly) {
    if (-not $WebOnly) {
        Invoke-PackageSelfAudit -PackageRoot $fullPackagePath -Compact:$false
        Write-Host "Self-audit passed: $fullPackagePath"
    }
    if (-not $FullOnly) {
        Invoke-PackageSelfAudit -PackageRoot $webPackagePath -Compact:$true
        Write-Host "Self-audit passed: $webPackagePath"
    }
    return
}

if (-not $WebOnly) {
    New-AuditPackage -Root $resolvedSourceRoot -PackageRoot $fullTemporaryPath -Kind "full" -Branch $BranchName -Commit $SourceCommit -Items $items -ReverseSource $ReverseSnippetsSource -Compact:$false -ExplicitReportZipPaths $ReportZipPaths -PreviousReportPath $previousReportPath
    Replace-PackageDirectory -TemporaryPath $fullTemporaryPath -FinalPath $fullPackagePath
    Invoke-PackageSelfAudit -PackageRoot $fullPackagePath -Compact:$false
    if (-not $SkipZip) {
        Write-PackageZip -PackagePath $fullPackagePath
    }
}

if (-not $FullOnly) {
    New-AuditPackage -Root $resolvedSourceRoot -PackageRoot $webTemporaryPath -Kind "web" -Branch $BranchName -Commit $SourceCommit -Items $items -ReverseSource $ReverseSnippetsSource -Compact:$true -ExplicitReportZipPaths @() -PreviousReportPath ""
    Replace-PackageDirectory -TemporaryPath $webTemporaryPath -FinalPath $webPackagePath
    Invoke-PackageSelfAudit -PackageRoot $webPackagePath -Compact:$true
    if (-not $SkipZip) {
        Write-PackageZip -PackagePath $webPackagePath
    }
}

Write-Host "Audit package update complete."
if (-not $WebOnly) {
    Write-Host "Full: $fullPackagePath"
}
if (-not $FullOnly) {
    Write-Host "Web:  $webPackagePath"
}
