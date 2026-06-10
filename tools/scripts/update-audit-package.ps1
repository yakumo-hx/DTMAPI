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
    [switch]$SelfAuditOnly
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

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
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

        $relative = [System.IO.Path]::GetRelativePath($Source, $_.FullName)
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

    $files = & git -C $Root ls-files
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

    $auditDocs = Join-Path $PackageRoot "audit/docs"
    $paths = @(
        "docs/api/public-api-matrix.md",
        "docs/debug/INDEX.md",
        "docs/debug/issues",
        "docs/debug/regressions/smoke-matrix.md",
        "docs/hook-map/README.md",
        "docs/hook-map/focused",
        "docs/reviews/manual-qa/2026",
        "docs/updates/INDEX.md",
        "docs/updates/README.md",
        "docs/updates/2026"
    )

    foreach ($path in $paths) {
        $source = Join-Path $Root $path
        $destination = Join-Path $auditDocs ($path -replace "^docs/", "")

        if (Test-Path -LiteralPath $source -PathType Container) {
            Copy-Tree -Source $source -Destination $destination -IncludeFile {
                param($item)
                $extension = $item.Extension.ToLowerInvariant()
                return $extension -notin @(".dll", ".exe", ".pdb", ".zip", ".rar", ".7z", ".nupkg")
            }
        }
        elseif (Test-Path -LiteralPath $source -PathType Leaf) {
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $source -Destination $destination -Force
        }
    }
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
    foreach ($script in @("run-game-smoke.ps1", "run-hook-probe.ps1")) {
        $source = Join-Path $Root "tools/scripts/$script"
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination (Join-Path $destination $script) -Force
        }
    }
}

function Get-DefaultEvidence {
    return @(
        [pscustomobject]@{ Id = "20260611-031502"; Label = "Final Camera diagnostics report-export smoke" },
        [pscustomobject]@{ Id = "20260611-031721"; Label = "Final ActionSpeed diagnostics report-export smoke" },
        [pscustomobject]@{ Id = "20260611-031838"; Label = "Final AutoFishing behavior and report-export smoke" },
        [pscustomobject]@{ Id = "20260611-031954"; Label = "Final HookProbe UI/runtime smoke" }
    )
}

function Get-EvidenceItems {
    param([string[]]$Ids)

    if ($Ids -and $Ids.Count -gt 0) {
        return $Ids | ForEach-Object {
            [pscustomobject]@{ Id = $_; Label = "User-selected smoke evidence" }
        }
    }

    return Get-DefaultEvidence
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
                $relative = [System.IO.Path]::GetRelativePath($source, $file.FullName) -replace "\\", "/"
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

    if (-not [string]::IsNullOrWhiteSpace($PreviousReportPath) -and (Test-Path -LiteralPath $PreviousReportPath)) {
        Get-ChildItem -LiteralPath $PreviousReportPath -File -Filter "*.zip" | ForEach-Object {
            $reportCandidates.Add($_.FullName)
        }
    }

    $seen = New-Object System.Collections.Generic.HashSet[string]
    foreach ($path in $reportCandidates) {
        if ($seen.Add($path)) {
            Copy-Item -LiteralPath $path -Destination (Join-Path $destination ([System.IO.Path]::GetFileName($path))) -Force
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
        "- Buildable source snapshot: src/, testmods/, tests/, tools/, assets/, root solution/project metadata, and public reference docs tracked in git.",
        "- Focused audit docs under audit/docs/: API matrix, hook map, smoke matrix, CameraZoom manual QA records, and update records.",
        "- Reverse snippet maps under audit/reverse-snippets/.",
        "- Latest smoke scripts under audit/tools/scripts/.",
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
        "- Refactor validation expected before package generation: git diff --check.",
        "- Refactor validation expected before package generation: tools/scripts/build.ps1 -Configuration Release.",
        "- Refactor validation expected before package generation: tools/scripts/test.ps1 -Configuration Release.",
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
        $relative = [System.IO.Path]::GetRelativePath($PackageRoot, $_.FullName) -replace "\\", "/"
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

    foreach ($relative in @("AUDIT-PACKAGE.md", "VALIDATION-SUMMARY.md", "src", "testmods", "tests", "tools/scripts/build.ps1", "audit/docs/api/public-api-matrix.md", "audit/docs/hook-map/README.md", "audit/docs/debug/regressions/smoke-matrix.md")) {
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
    if (Test-Path -LiteralPath $backupPath) {
        Remove-Item -LiteralPath $backupPath -Recurse -Force
    }

    if (Test-Path -LiteralPath $FinalPath) {
        Move-Item -LiteralPath $FinalPath -Destination $backupPath
    }

    Move-Item -LiteralPath $TemporaryPath -Destination $FinalPath

    if (Test-Path -LiteralPath $backupPath) {
        Remove-Item -LiteralPath $backupPath -Recurse -Force
    }
}

function Write-PackageZip {
    param([string]$PackagePath)

    $zipPath = $PackagePath + ".zip"
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -LiteralPath $PackagePath -DestinationPath $zipPath -CompressionLevel Optimal
}

$resolvedSourceRoot = Resolve-SourceRoot -Path $SourceRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Split-Path -Parent $resolvedSourceRoot
}
$resolvedOutputRoot = (Resolve-Path -LiteralPath $OutputRoot).Path

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

if ([string]::IsNullOrWhiteSpace($ReverseSnippetsSource)) {
    $candidate = Join-Path $fullPackagePath "audit/reverse-snippets"
    if (Test-Path -LiteralPath $candidate) {
        $ReverseSnippetsSource = $candidate
    }
}

$previousReportPath = Join-Path $fullPackagePath "audit/report"
$items = @(Get-EvidenceItems -Ids $EvidenceIds)

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
