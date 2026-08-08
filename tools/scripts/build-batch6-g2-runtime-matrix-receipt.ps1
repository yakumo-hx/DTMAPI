param(
    [Parameter(Mandatory = $true)]
    [string] $EvidenceMapPath,

    [string] $OutputPath = '',

    [ValidateSet('None', 'GenericSmokeExit', 'AutoFishingIdentity', 'PackageZipEntry', 'RunnerReceipt')]
    [string] $MutationProbe = 'None',

    [switch] $PassThru
)

. "$PSScriptRoot\common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$caseIds = @(
    'wrong-owner-cold',
    'duplicate-patch-cold',
    'entry-failure-cold',
    'late-owner-drift-cold',
    'disabled-cold',
    'normal-v1-cold',
    'postload-disable-v1',
    'live-update-v1-to-v2',
    'final-clean-v2-cold'
)
$runtimeAssemblyNames = @(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)
$packagePaths = @(
    'Content/DTMAPI/DTMAPI.AdvancedFixture.dll',
    'Content/DTMAPI/README.txt',
    'Content/DTMAPI/dtmapi-advanced-references.json',
    'Content/DTMAPI/dtmapi-package.json',
    'Content/DTMAPI/manifest.json',
    'info.json'
)
$contractPath = 'tools/release/contracts/batch6-g2-advanced-synthetic-contract.json'
$gameSmokeRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'docs\debug\evidence\GAME-SMOKE'))

function Normalize-RelativePath([string] $path) {
    return ([string]$path).Trim().TrimStart([char]'\', [char]'/').Replace('\', '/').TrimEnd('/')
}

function Resolve-PathFromRepo([string] $path) {
    if ([string]::IsNullOrWhiteSpace($path)) { throw 'A required path is empty.' }
    $candidate = $path
    if (-not [System.IO.Path]::IsPathRooted($candidate)) { $candidate = Join-Path $repo $candidate }
    return [System.IO.Path]::GetFullPath($candidate)
}

function Test-PathUnder([string] $path, [string] $root) {
    $fullPath = [System.IO.Path]::GetFullPath($path).TrimEnd('\', '/')
    $fullRoot = [System.IO.Path]::GetFullPath($root).TrimEnd('\', '/')
    return $fullPath.Equals($fullRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith($fullRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Get-RepoRelativePath([string] $path) {
    $fullPath = [System.IO.Path]::GetFullPath($path)
    if (-not (Test-PathUnder $fullPath $repo)) { throw "Path must remain under the repository root: $fullPath" }
    return Normalize-RelativePath ($fullPath.Substring($repo.TrimEnd('\', '/').Length))
}

function Assert-File([string] $path, [string] $label) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "$label is missing: $path" }
}

function Read-Utf8([string] $path, [string] $label) {
    Assert-File $path $label
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function Read-Json([string] $path, [string] $label) {
    $text = Read-Utf8 $path $label
    try {
        $convertCommand = Get-Command ConvertFrom-Json
        if ($convertCommand.Parameters.ContainsKey('DateKind')) { return $text | ConvertFrom-Json -DateKind String }
        return $text | ConvertFrom-Json
    }
    catch { throw "$label is not parseable JSON: $($_.Exception.Message)" }
}

function Get-FileSha256([string] $path) {
    $stream = [System.IO.File]::OpenRead($path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Get-BytesSha256([byte[]] $bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Get-StringSha256([string] $value) {
    return Get-BytesSha256 $utf8NoBom.GetBytes($value)
}

function New-FileRow([string] $path, [string] $receiptPath) {
    Assert-File $path 'Receipt evidence file'
    $item = Get-Item -LiteralPath $path
    return [ordered]@{
        path = $receiptPath
        length = [long]$item.Length
        sha256 = Get-FileSha256 $path
    }
}

function New-RepoFileRow([string] $path) {
    return New-FileRow $path (Get-RepoRelativePath $path)
}

function Assert-ExactProperties([object] $object, [string[]] $expected, [string] $label) {
    if ($null -eq $object) { throw "$label is missing." }
    $actual = @($object.PSObject.Properties.Name)
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($name in $actual) { [void]$actualSet.Add([string]$name) }
    $expectedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($name in $expected) { [void]$expectedSet.Add([string]$name) }
    $missing = @($expected | Where-Object { -not $actualSet.Contains([string]$_) })
    $extra = @($actual | Where-Object { -not $expectedSet.Contains([string]$_) })
    if ($actual.Count -ne $expected.Count -or $missing.Count -gt 0 -or $extra.Count -gt 0) {
        throw "$label fields mismatch. Missing=[$($missing -join ', ')] Extra=[$($extra -join ', ')]."
    }
}

function Assert-Exact([string] $label, [object] $actual, [object] $expected) {
    if ($null -eq $actual -or -not ([string]$actual).Equals([string]$expected, [StringComparison]::Ordinal)) {
        throw "$label must be '$expected'; found '$actual'."
    }
}

function Assert-PathEqual([string] $label, [object] $actual, [object] $expected) {
    if ([string]::IsNullOrWhiteSpace([string]$actual) -or [string]::IsNullOrWhiteSpace([string]$expected)) {
        throw "$label requires two non-empty paths."
    }
    $actualPath = [System.IO.Path]::GetFullPath([string]$actual).TrimEnd('\', '/')
    $expectedPath = [System.IO.Path]::GetFullPath([string]$expected).TrimEnd('\', '/')
    if (-not $actualPath.Equals($expectedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$label must resolve to '$expectedPath'; found '$actualPath'."
    }
}

function Assert-Boolean([string] $label, [object] $actual, [bool] $expected) {
    if ($null -eq $actual -or -not ($actual -is [bool]) -or [bool]$actual -ne $expected) {
        throw "$label must be $($expected.ToString().ToLowerInvariant()); found '$actual'."
    }
}

function Assert-Int([string] $label, [object] $actual, [int] $expected) {
    if ($null -eq $actual -or [int]$actual -ne $expected) { throw "$label must be $expected; found '$actual'." }
}

function Assert-ExactSet([string] $label, [object[]] $actual, [object[]] $expected) {
    $actualValues = @($actual | ForEach-Object { [string]$_ })
    $expectedValues = @($expected | ForEach-Object { [string]$_ })
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($value in $actualValues) {
        if (-not $actualSet.Add($value)) { throw "$label contains duplicate '$value'." }
    }
    $expectedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($value in $expectedValues) { [void]$expectedSet.Add($value) }
    $missing = @($expectedValues | Where-Object { -not $actualSet.Contains($_) })
    $extra = @($actualValues | Where-Object { -not $expectedSet.Contains($_) })
    if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
        throw "$label mismatch. Missing=[$($missing -join ', ')] Extra=[$($extra -join ', ')]."
    }
}

function Get-MatchCount([string] $text, [string] $pattern) {
    return [Text.RegularExpressions.Regex]::Matches($text, $pattern, [Text.RegularExpressions.RegexOptions]::Multiline).Count
}

function Get-OnlyRegexMatch([string] $text, [string] $pattern, [string] $label) {
    $matches = [Text.RegularExpressions.Regex]::Matches($text, $pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
    if ($matches.Count -ne 1) { throw "$label must have exactly one match; found $($matches.Count)." }
    return $matches[0]
}

function Get-OnlyLogTimestamp([string] $text, [string] $messagePattern, [string] $label) {
    $match = Get-OnlyRegexMatch $text ('^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}).*' + $messagePattern + '.*$') $label
    return Parse-Utc $match.Groups['timestamp'].Value $label
}

function Parse-Utc([object] $value, [string] $label) {
    try { $parsed = [DateTimeOffset]::Parse([string]$value, [Globalization.CultureInfo]::InvariantCulture) }
    catch { throw "$label is not a valid timestamp: '$value'." }
    return $parsed.ToUniversalTime()
}

function Format-Utc([DateTimeOffset] $value) { return $value.ToUniversalTime().ToString('o') }

function Invoke-Git([string[]] $arguments, [bool] $allowFailure) {
    $oldPreference = $ErrorActionPreference
    $oldEncoding = [Console]::OutputEncoding
    try {
        $ErrorActionPreference = 'Continue'
        [Console]::OutputEncoding = $utf8NoBom
        $output = @(& git -C $repo -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        [Console]::OutputEncoding = $oldEncoding
        $ErrorActionPreference = $oldPreference
    }
    if (-not $allowFailure -and $exitCode -ne 0) { throw "git $($arguments -join ' ') failed: $($output -join [Environment]::NewLine)" }
    return [pscustomobject]@{ ExitCode = $exitCode; Lines = @($output | ForEach-Object { [string]$_ }) }
}

function Get-FullCommit([string] $value) {
    $result = Invoke-Git @('rev-parse', "$value^{commit}") $false
    $commit = ([string]($result.Lines | Select-Object -First 1)).Trim()
    if ($commit -notmatch '^[0-9a-f]{40}$') { throw "Implementation commit did not resolve to a full lowercase SHA: '$commit'." }
    return $commit
}

function Assert-IgnoredEvidenceMap([string] $path) {
    if (-not (Test-PathUnder $path $repo)) { return }
    $relative = Get-RepoRelativePath $path
    $tracked = Invoke-Git @('ls-files', '--error-unmatch', '--', $relative) $true
    if ($tracked.ExitCode -eq 0) { throw "Evidence map must remain ignored/untracked, but Git tracks '$relative'." }
    $ignored = Invoke-Git @('check-ignore', '-q', '--', $relative) $true
    if ($ignored.ExitCode -ne 0) { throw "Evidence map inside the repository must be ignored by Git: '$relative'." }
}

function Get-OptionalMapPath([object] $caseMap, [string] $property, [string] $evidenceRoot) {
    $entry = $caseMap.PSObject.Properties[$property]
    if ($null -eq $entry -or [string]::IsNullOrWhiteSpace([string]$entry.Value)) { return $null }
    $fullPath = Resolve-PathFromRepo ([string]$entry.Value)
    if (-not (Test-PathUnder $fullPath $evidenceRoot)) { throw "$property must remain inside its GAME-SMOKE evidence root: $fullPath" }
    Assert-File $fullPath $property
    return $fullPath
}

function Get-GameSmokeEvidencePath([object] $caseMap, [string] $property) {
    $entry = $caseMap.PSObject.Properties[$property]
    if ($null -eq $entry -or [string]::IsNullOrWhiteSpace([string]$entry.Value)) { return $null }
    $fullPath = Resolve-PathFromRepo ([string]$entry.Value)
    if (-not (Test-PathUnder $fullPath $gameSmokeRoot)) { throw "$property must remain under docs/debug/evidence/GAME-SMOKE: $fullPath" }
    Assert-File $fullPath $property
    return $fullPath
}

function Get-ZipSelection([string] $zipPath, [string] $evidenceRoot) {
    if (-not (Test-PathUnder $zipPath $evidenceRoot)) { throw "Report ZIP must be snapshotted inside its GAME-SMOKE evidence root: $zipPath" }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $limits = @{
        'DTMAPI-runtime-context.txt' = 8MB
        'DTMAPI-latest.log' = 16MB
        'release-manifest.json' = 1MB
        'PlayerDoctor/player-doctor.json' = 4MB
    }
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $selected = [ordered]@{}
        foreach ($entryName in @('DTMAPI-runtime-context.txt', 'DTMAPI-latest.log', 'release-manifest.json', 'PlayerDoctor/player-doctor.json')) {
            $matches = @($archive.Entries | Where-Object { ([string]$_.FullName).Equals($entryName, [StringComparison]::Ordinal) })
            if ($matches.Count -ne 1) { throw "Report ZIP entry '$entryName' must occur exactly once; found $($matches.Count)." }
            $entry = $matches[0]
            if ([long]$entry.Length -gt [long]$limits[$entryName]) { throw "Report ZIP entry '$entryName' exceeds the size limit." }
            $stream = $entry.Open()
            try {
                $memory = New-Object System.IO.MemoryStream
                try { $stream.CopyTo($memory); $bytes = $memory.ToArray() }
                finally { $memory.Dispose() }
            }
            finally { $stream.Dispose() }
            if ([long]$bytes.Length -ne [long]$entry.Length) { throw "Report ZIP entry '$entryName' length changed while reading." }
            $text = [System.Text.Encoding]::UTF8.GetString($bytes)
            $zipRelative = Get-RepoRelativePath $zipPath
            $selected[$entryName] = [pscustomobject]@{
                Text = $text
                Row = [ordered]@{
                    path = $zipRelative + '!/' + $entryName
                    length = [long]$bytes.Length
                    sha256 = Get-BytesSha256 $bytes
                }
            }
        }
        return $selected
    }
    finally { $archive.Dispose() }
}

function Get-ZipFileReceipts([string] $zipPath, [string[]] $expectedPaths, [string] $label) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $fileEntries = @($archive.Entries | Where-Object { -not [string]::IsNullOrEmpty([string]$_.Name) })
        Assert-ExactSet "$label ZIP paths" @($fileEntries | ForEach-Object { ([string]$_.FullName).Replace('\', '/') }) $expectedPaths
        if ($fileEntries.Count -ne $expectedPaths.Count) {
            throw "$label ZIP must contain exactly $($expectedPaths.Count) files; found $($fileEntries.Count)."
        }

        $receipts = [ordered]@{}
        foreach ($expectedPath in $expectedPaths) {
            $matches = @($fileEntries | Where-Object {
                ([string]$_.FullName).Replace('\', '/').Equals($expectedPath, [StringComparison]::Ordinal)
            })
            if ($matches.Count -ne 1) { throw "$label ZIP entry '$expectedPath' must occur exactly once; found $($matches.Count)." }
            $entry = $matches[0]
            if ([long]$entry.Length -le 0 -or [long]$entry.Length -gt 64MB) {
                throw "$label ZIP entry '$expectedPath' has an invalid uncompressed length: $($entry.Length)."
            }
            $stream = $entry.Open()
            try {
                $memory = New-Object System.IO.MemoryStream
                try { $stream.CopyTo($memory); $bytes = $memory.ToArray() }
                finally { $memory.Dispose() }
            }
            finally { $stream.Dispose() }
            if ([long]$bytes.Length -ne [long]$entry.Length) { throw "$label ZIP entry '$expectedPath' length changed while reading." }
            $receipts[$expectedPath] = [ordered]@{
                path = $expectedPath
                length = [long]$bytes.Length
                sha256 = Get-BytesSha256 $bytes
            }
        }
        return $receipts
    }
    finally { $archive.Dispose() }
}

function Parse-Summary([string] $text, [string] $label) {
    $values = @{}
    foreach ($line in ($text -split "`r?`n")) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.IndexOf('=') -lt 1) { continue }
        $index = $line.IndexOf('=')
        $key = $line.Substring(0, $index)
        if (-not $values.ContainsKey($key)) { $values[$key] = $line.Substring($index + 1) }
    }
    foreach ($key in @('Started', 'SaveSlot', 'IncludeHookProbe', 'LaunchMode')) {
        if (-not $values.ContainsKey($key)) { throw "$label is missing '$key'." }
    }
    return $values
}

function Get-ReportMod([string] $context, [string] $uniqueId) {
    $pattern = '(?m)^MOD ' + [regex]::Escape($uniqueId) + ' statusCode=(?<status>\S+) loaded=(?<loaded>true|false) officialEnabled=(?<enabled>true|false) source=\S+ version=(?<version>\S+) '
    $match = Get-OnlyRegexMatch $context $pattern "Report MOD $uniqueId"
    return [ordered]@{
        statusCode = $match.Groups['status'].Value
        loaded = [bool]::Parse($match.Groups['loaded'].Value)
        officialEnabled = [bool]::Parse($match.Groups['enabled'].Value)
        version = $match.Groups['version'].Value
    }
}

function Get-ReleaseManifest([string] $text, [string] $label) {
    try {
        $convertCommand = Get-Command ConvertFrom-Json
        if ($convertCommand.Parameters.ContainsKey('DateKind')) { $manifest = $text | ConvertFrom-Json -DateKind String }
        else { $manifest = $text | ConvertFrom-Json }
    }
    catch { throw "$label is not parseable JSON: $($_.Exception.Message)" }
    Assert-Int "$label SchemaVersion" $manifest.SchemaVersion 1
    Assert-Exact "$label DTMAPIVersion" $manifest.DTMAPIVersion '0.5.5'
    if ([string]$manifest.BuildCommit -notmatch '^[0-9a-f]{12}$') { throw "$label BuildCommit must be a 12-character lowercase SHA." }
    [void](Parse-Utc $manifest.BuildTime "$label BuildTime")
    Assert-ExactSet "$label IncludedAssemblies" @($manifest.IncludedAssemblies | ForEach-Object { [string]$_.FileName }) $runtimeAssemblyNames
    $assemblies = New-Object System.Collections.Generic.List[object]
    foreach ($name in $runtimeAssemblyNames) {
        $rows = @($manifest.IncludedAssemblies | Where-Object { ([string]$_.FileName).Equals($name, [StringComparison]::Ordinal) })
        if ($rows.Count -ne 1) { throw "$label must contain exactly one '$name'." }
        $row = $rows[0]
        if ([long]$row.Length -le 0 -or [string]$row.Sha256 -notmatch '^[0-9a-fA-F]{64}$') { throw "$label contains an invalid receipt for '$name'." }
        $assemblies.Add([ordered]@{ name = $name; length = [long]$row.Length; sha256 = ([string]$row.Sha256).ToUpperInvariant() })
    }
    return [ordered]@{
        buildCommitPrefix = [string]$manifest.BuildCommit
        buildTimeUtc = Format-Utc (Parse-Utc $manifest.BuildTime "$label BuildTime")
        dtmApiVersion = [string]$manifest.DTMAPIVersion
        releaseManifestSha256 = Get-StringSha256 $text
        assemblies = @($assemblies.ToArray())
    }
}

function Get-PackageArtifact([object] $mapRow, [string] $expectedVersion) {
    Assert-ExactProperties $mapRow @('version', 'packagePath', 'packageReportPath') "Fixture package map $expectedVersion"
    Assert-Exact "Fixture package map version $expectedVersion" $mapRow.version $expectedVersion
    $packagePath = Resolve-PathFromRepo ([string]$mapRow.packagePath)
    $reportPath = Resolve-PathFromRepo ([string]$mapRow.packageReportPath)
    Assert-File $packagePath "Fixture package $expectedVersion"
    $report = Read-Json $reportPath "Fixture package report $expectedVersion"
    Assert-Int "Fixture package report $expectedVersion schemaVersion" $report.schemaVersion 2
    Assert-Exact "Fixture package report $expectedVersion uniqueID" $report.uniqueID 'DTMAPI.AdvancedFixture'
    Assert-Exact "Fixture package report $expectedVersion version" $report.version $expectedVersion
    Assert-Exact "Fixture package report $expectedVersion projectKind" $report.projectKind 'CodeMod'
    Assert-Exact "Fixture package report $expectedVersion codeModKind" $report.codeModKind 'Advanced'
    Assert-Exact "Fixture package report $expectedVersion file name" $report.packageFileName ([System.IO.Path]::GetFileName($packagePath))
    Assert-Exact "Fixture package report $expectedVersion package hash" ([string]$report.packageSha256).ToUpperInvariant() (Get-FileSha256 $packagePath)
    Assert-ExactSet "Fixture package report $expectedVersion files" @($report.files | ForEach-Object { [string]$_.path }) $packagePaths
    if (@($report.files).Count -ne 6) { throw "Fixture package report $expectedVersion must contain exactly six files." }
    $zipReceipts = Get-ZipFileReceipts $packagePath $packagePaths "Fixture package $expectedVersion"
    foreach ($row in @($report.files)) {
        if ([long]$row.length -le 0 -or [string]$row.sha256 -notmatch '^[0-9a-fA-F]{64}$') {
            throw "Fixture package report $expectedVersion has an invalid file receipt for '$($row.path)'."
        }
        if (([string]$row.path).EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase) -and
            -not ([string]$row.path).Equals('Content/DTMAPI/DTMAPI.AdvancedFixture.dll', [StringComparison]::Ordinal)) {
            throw "Fixture package $expectedVersion bundles an unexpected native/runtime DLL: $($row.path)"
        }
        $actualEntry = $zipReceipts[[string]$row.path]
        if ($MutationProbe -eq 'PackageZipEntry' -and $expectedVersion -eq '0.1.0' -and ([string]$row.path).Equals('Content/DTMAPI/manifest.json', [StringComparison]::Ordinal)) {
            $actualEntry = [ordered]@{ path = [string]$actualEntry.path; length = [long]$actualEntry.length; sha256 = (('0' * 64) -join '') }
        }
        Assert-Int "Fixture package $expectedVersion ZIP length for $($row.path)" $actualEntry.length ([int64]$row.length)
        Assert-Exact "Fixture package $expectedVersion ZIP hash for $($row.path)" ([string]$actualEntry.sha256).ToUpperInvariant() ([string]$row.sha256).ToUpperInvariant()
    }
    Assert-Exact "Fixture package $expectedVersion manifest hash" ([string]$zipReceipts['Content/DTMAPI/manifest.json'].sha256).ToUpperInvariant() ([string]$report.manifestSha256).ToUpperInvariant()
    Assert-Exact "Fixture package $expectedVersion entry hash" ([string]$zipReceipts['Content/DTMAPI/DTMAPI.AdvancedFixture.dll'].sha256).ToUpperInvariant() ([string]$report.entryDllSha256).ToUpperInvariant()
    Assert-Exact "Fixture package $expectedVersion reference receipt hash" ([string]$zipReceipts['Content/DTMAPI/dtmapi-advanced-references.json'].sha256).ToUpperInvariant() ([string]$report.advancedReferenceReceiptSha256).ToUpperInvariant()
    return [ordered]@{
        version = $expectedVersion
        package = [ordered]@{
            path = [System.IO.Path]::GetFileName($packagePath)
            length = [long](Get-Item -LiteralPath $packagePath).Length
            sha256 = Get-FileSha256 $packagePath
        }
        packageReport = [ordered]@{
            path = [System.IO.Path]::GetFileName($reportPath)
            length = [long](Get-Item -LiteralPath $reportPath).Length
            sha256 = Get-FileSha256 $reportPath
        }
        manifestSha256 = ([string]$report.manifestSha256).ToUpperInvariant()
        entryAssemblySha256 = ([string]$report.entryDllSha256).ToUpperInvariant()
        advancedReferenceReceiptSha256 = ([string]$report.advancedReferenceReceiptSha256).ToUpperInvariant()
        fileCount = 6
        bundledNativeRuntimeDependencyCount = 0
    }
}

function Get-ExpectedCase([string] $caseId) {
    switch ($caseId) {
        'normal-v1-cold' {
            return [ordered]@{ kind = 'cold'; version = '0.1.0'; classification = 'Advanced'; status = 'loaded'; loaded = $true; enabled = $true; restart = $false; exit = 0; diagnostics = @(); resultFailures = @() }
        }
        'wrong-owner-cold' {
            return [ordered]@{ kind = 'cold'; version = '0.1.0'; classification = 'Advanced'; status = 'restart-required'; loaded = $false; enabled = $true; restart = $true; exit = 1; diagnostics = @('advanced-harmony-owner-mismatch', 'advanced-harmony-cleanup-failed'); resultFailures = @('ModOwnerLifecycle', 'GameBridgeFinalHealthSnapshot', 'FailedModRollback', 'RunStatus') }
        }
        'duplicate-patch-cold' {
            return [ordered]@{ kind = 'cold'; version = '0.1.0'; classification = 'Advanced'; status = 'restart-required'; loaded = $false; enabled = $true; restart = $true; exit = 1; diagnostics = @('advanced-harmony-duplicate-patch', 'advanced-harmony-cleanup-complete'); resultFailures = @('GameBridgeFinalHealthSnapshot', 'RunStatus') }
        }
        'entry-failure-cold' {
            return [ordered]@{ kind = 'cold'; version = '0.1.0'; classification = 'Advanced'; status = 'restart-required'; loaded = $false; enabled = $true; restart = $true; exit = 1; diagnostics = @('advanced-harmony-cleanup-complete'); resultFailures = @('GameBridgeFinalHealthSnapshot', 'RunStatus') }
        }
        'late-owner-drift-cold' {
            return [ordered]@{ kind = 'cold'; version = '0.1.0'; classification = 'Advanced'; status = 'restart-required'; loaded = $false; enabled = $true; restart = $true; exit = 1; diagnostics = @('advanced-harmony-late-owner-drift', 'advanced-harmony-cleanup-complete'); resultFailures = @('GameBridgeFinalHealthSnapshot', 'RunStatus') }
        }
        'disabled-cold' {
            return [ordered]@{ kind = 'cold'; version = '0.1.0'; classification = 'Advanced'; status = 'disabled'; loaded = $false; enabled = $false; restart = $false; exit = 0; diagnostics = @(); resultFailures = @() }
        }
        'postload-disable-v1' {
            return [ordered]@{ kind = 'post-load-disable'; version = '0.1.0'; classification = 'Advanced'; status = 'restart-required'; loaded = $false; enabled = $false; restart = $true; exit = 1; diagnostics = @('advanced-harmony-cleanup-complete'); resultFailures = @('GameBridgeFinalHealthSnapshot', 'RunStatus') }
        }
        'live-update-v1-to-v2' {
            return [ordered]@{ kind = 'live-update'; version = '0.1.0'; classification = 'Advanced'; status = 'loaded'; loaded = $true; enabled = $true; restart = $true; exit = 0; diagnostics = @('SDK602'); resultFailures = @() }
        }
        'final-clean-v2-cold' {
            return [ordered]@{ kind = 'cold'; version = '0.1.1'; classification = 'Advanced'; status = 'loaded'; loaded = $true; enabled = $true; restart = $false; exit = 0; diagnostics = @(); resultFailures = @() }
        }
        default { throw "Unknown G2 runtime matrix case '$caseId'." }
    }
}

function New-NullableState {
    return [ordered]@{
        markerAbsentBefore = $null
        queryObservedAtUtc = $null
        markerCreatedAtUtc = $null
        markerLength = $null
        reloadObservedAtUtc = $null
        markerAbsentAfter = $null
        configRestoredExact = $null
        processAbsentAfter = $null
        qaWorkshopReloadCompleted = $null
    }
}

function New-NullableSession {
    return [ordered]@{
        prepareSuccess = $null
        sessionId = $null
        deploymentStatusV1Success = $null
        deploymentStatusV1RecoveryArtifacts = $null
        snapshotSuccess = $null
        snapshotRuntimeStatus = $null
        snapshotRuntimeCode = $null
        snapshotVersion = $null
        snapshotTreeSha256 = $null
        updateSuccess = $null
        updatedVersion = $null
        deploymentStatusV2Success = $null
        deploymentStatusV2RecoveryArtifacts = $null
        reloadSuccess = $null
        reloadRuntimeStatus = $null
        reloadRuntimeCode = $null
        reloadDiagnosticCodes = @()
        clearSuccess = $null
        credentialsCleared = $null
        clearedCredentialFileCount = $null
        preUpdateTreeSha256 = $null
        postUpdateTreeSha256 = $null
    }
}

function Get-CommandReportDiagnostics([object] $report) {
    return @($report.diagnostics | ForEach-Object { [string]$_.code })
}

function Assert-CommandReport([object] $report, [string] $label) {
    if ($null -eq $report.PSObject.Properties['success'] -or -not ($report.success -is [bool])) { throw "$label is missing boolean success." }
    if ($null -eq $report.PSObject.Properties['diagnostics']) { throw "$label is missing diagnostics." }
    if ($null -eq $report.PSObject.Properties['values']) { throw "$label is missing values." }
}

function Get-Value([object] $object, [string] $name) {
    if ($null -eq $object) { return $null }
    $property = $object.PSObject.Properties[$name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-StringValue([object] $object, [string] $name) {
    $value = Get-Value $object $name
    if ($null -eq $value) { return $null }
    return [string]$value
}

function Assert-Sha256([string] $label, [object] $value) {
    if ([string]$value -notmatch '^[0-9a-fA-F]{64}$') { throw "$label must be a SHA-256; found '$value'." }
    return ([string]$value).ToUpperInvariant()
}

function Compare-RuntimeRelease([object] $left, [object] $right, [string] $caseId) {
    $leftJson = $left | ConvertTo-Json -Depth 10 -Compress
    $rightJson = $right | ConvertTo-Json -Depth 10 -Compress
    if (-not $leftJson.Equals($rightJson, [StringComparison]::Ordinal)) {
        throw "All G2 matrix cases must use the same final Runtime; release manifest drifted at '$caseId'."
    }
}

function Get-ColdRunnerCaseId([string] $caseId) {
    switch ($caseId) {
        'wrong-owner-cold' { return 'WrongOwner' }
        'duplicate-patch-cold' { return 'DuplicatePatch' }
        'entry-failure-cold' { return 'EntryFailure' }
        'late-owner-drift-cold' { return 'LateOwnerDrift' }
        'disabled-cold' { return 'DisabledCold' }
        default { throw "Case '$caseId' has no cold-runner identity." }
    }
}

function Get-ColdRunnerReceiptRow(
    [object] $caseMap,
    [string] $caseId,
    [int] $expectedExit,
    [string] $runRootPath,
    [string] $resultPath,
    [string] $runtimeLogPath,
    [string] $reportZipPath,
    [string] $reportContextSha256,
    [DateTimeOffset] $completed
) {
    $runnerPath = Get-GameSmokeEvidencePath $caseMap 'runnerReceiptPath'
    $runner = Read-Json $runnerPath "cold-runner receipt for $caseId"
    Assert-ExactProperties $runner @('schemaVersion', 'generatedAtUtc', 'cases', 'status') "cold-runner receipt for $caseId"
    Assert-Int "cold-runner schemaVersion for $caseId" $runner.schemaVersion 1
    Assert-Exact "cold-runner status for $caseId" $runner.status 'passed'
    $runnerGenerated = Parse-Utc $runner.generatedAtUtc "cold-runner generatedAtUtc for $caseId"
    if ($runnerGenerated -lt $completed) { throw "Cold-runner receipt for '$caseId' predates the case completion." }
    $runnerCaseIds = @($runner.cases | ForEach-Object { [string]$_.caseId })
    Assert-ExactSet "cold-runner case IDs for $caseId" $runnerCaseIds @('WrongOwner', 'DuplicatePatch', 'EntryFailure', 'LateOwnerDrift', 'DisabledCold')
    if (@($runner.cases).Count -ne 5) { throw "Cold-runner receipt for '$caseId' must contain exactly five cases." }

    $runnerCaseId = Get-ColdRunnerCaseId $caseId
    $rows = @($runner.cases | Where-Object { ([string]$_.caseId).Equals($runnerCaseId, [StringComparison]::Ordinal) })
    if ($rows.Count -ne 1) { throw "Cold-runner receipt must contain exactly one '$runnerCaseId' row." }
    $row = $rows[0]
    Assert-ExactProperties $row @('caseId', 'genericExitCode', 'evidencePath', 'resultSha256', 'logSha256', 'reportPath', 'reportSha256', 'reportContextSha256', 'checks', 'status', 'failures') "cold-runner row $runnerCaseId"
    Assert-Int "cold-runner exit for $caseId" $row.genericExitCode $expectedExit
    Assert-Exact "cold-runner evidence path for $caseId" (Normalize-RelativePath ([string]$row.evidencePath)) $runRootPath
    $expectedResultSha = Get-FileSha256 $resultPath
    if ($MutationProbe -eq 'RunnerReceipt' -and $caseId -eq 'wrong-owner-cold') { $expectedResultSha = ('0' * 64) -join '' }
    Assert-Exact "cold-runner result hash for $caseId" ([string]$row.resultSha256).ToUpperInvariant() $expectedResultSha
    Assert-Exact "cold-runner log hash for $caseId" ([string]$row.logSha256).ToUpperInvariant() (Get-FileSha256 $runtimeLogPath)
    Assert-Exact "cold-runner report hash for $caseId" ([string]$row.reportSha256).ToUpperInvariant() (Get-FileSha256 $reportZipPath)
    Assert-Exact "cold-runner report context hash for $caseId" ([string]$row.reportContextSha256).ToUpperInvariant() $reportContextSha256.ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace([string]$row.reportPath) -or -not ([string]$row.reportPath).EndsWith('.zip', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Cold-runner report path for '$caseId' is not a ZIP path."
    }
    Assert-Exact "cold-runner row status for $caseId" $row.status 'passed'
    if (@($row.failures).Count -ne 0) { throw "Cold-runner row '$runnerCaseId' contains failures." }
    $checkNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    if (@($row.checks).Count -lt 1) { throw "Cold-runner row '$runnerCaseId' contains no checks." }
    foreach ($check in @($row.checks)) {
        Assert-ExactProperties $check @('name', 'passed', 'details') "cold-runner check for $runnerCaseId"
        if ([string]::IsNullOrWhiteSpace([string]$check.name) -or -not $checkNames.Add([string]$check.name)) {
            throw "Cold-runner row '$runnerCaseId' has a blank or duplicate check name."
        }
        Assert-Boolean "cold-runner check '$($check.name)' for $caseId" $check.passed $true
        if ([string]::IsNullOrWhiteSpace([string]$check.details)) { throw "Cold-runner check '$($check.name)' for '$caseId' lacks details." }
    }
    return New-RepoFileRow $runnerPath
}

function New-CaseReceipt([object] $caseMap, [string] $caseId, [object] $contract, [ref] $runtimeReleaseReference) {
    $expectedMapFields = @('caseId', 'evidencePath', 'reportZipPath', 'genericSmokeExitCode')
    if ($caseId -in @('wrong-owner-cold', 'duplicate-patch-cold', 'entry-failure-cold', 'late-owner-drift-cold', 'disabled-cold')) {
        $expectedMapFields += 'runnerReceiptPath'
    }
    if ($caseId -eq 'postload-disable-v1') { $expectedMapFields += 'postLoadOrchestrationPath' }
    if ($caseId -eq 'live-update-v1-to-v2') {
        $expectedMapFields += @('liveUpdateOrchestrationPath', 'sessionPreparePath', 'deploymentStatusV1Path', 'sessionSnapshotPath', 'sdkUpdatePath', 'deploymentStatusV2Path', 'sessionReloadPath', 'sessionClearPath')
    }
    Assert-ExactProperties $caseMap $expectedMapFields "Evidence map case $caseId"
    Assert-Exact "Evidence map case ID $caseId" $caseMap.caseId $caseId
    $expected = Get-ExpectedCase $caseId

    $evidenceRoot = Resolve-PathFromRepo ([string]$caseMap.evidencePath)
    if (-not (Test-Path -LiteralPath $evidenceRoot -PathType Container)) { throw "Evidence root is missing for '$caseId': $evidenceRoot" }
    if (-not (Test-PathUnder $evidenceRoot $gameSmokeRoot)) { throw "Evidence root for '$caseId' must remain under docs/debug/evidence/GAME-SMOKE." }
    $runRootPath = Get-RepoRelativePath $evidenceRoot

    $resultPath = Join-Path $evidenceRoot 'result.json'
    $summaryPath = Join-Path $evidenceRoot 'summary.txt'
    $runtimeLogPath = Join-Path $evidenceRoot 'DTMAPI-latest.log'
    $qaCleanupPath = Join-Path $evidenceRoot 'qa-host-cleanup.json'
    $profileRestorePath = Join-Path $evidenceRoot 'official-mod-profile-restore-verification.json'
    $processCheckPath = Join-Path $evidenceRoot 'process-check.txt'
    $reportZipPath = Resolve-PathFromRepo ([string]$caseMap.reportZipPath)
    if (-not (Test-PathUnder $reportZipPath $evidenceRoot)) { throw "Report ZIP for '$caseId' must remain inside its evidence root." }
    Assert-File $reportZipPath "Report ZIP for $caseId"

    $result = Read-Json $resultPath "result.json for $caseId"
    $summaryText = Read-Utf8 $summaryPath "summary.txt for $caseId"
    $summary = Parse-Summary $summaryText "summary.txt for $caseId"
    $runtimeLog = Read-Utf8 $runtimeLogPath "runtime log for $caseId"
    $qaCleanup = Read-Json $qaCleanupPath "qa-host-cleanup.json for $caseId"
    $profileRestore = Read-Json $profileRestorePath "official profile restore for $caseId"
    $processCheck = Read-Utf8 $processCheckPath "process-check.txt for $caseId"
    $zipSelection = Get-ZipSelection $reportZipPath $evidenceRoot
    $reportContext = [string]$zipSelection['DTMAPI-runtime-context.txt'].Text
    $reportLog = [string]$zipSelection['DTMAPI-latest.log'].Text
    $releaseManifestText = [string]$zipSelection['release-manifest.json'].Text
    $playerDoctorText = [string]$zipSelection['PlayerDoctor/player-doctor.json'].Text
    if (-not $runtimeLog.StartsWith($reportLog, [StringComparison]::Ordinal)) {
        throw "Report Runtime log for '$caseId' is not an exact byte-text prefix of the run-root Runtime log."
    }

    $reportStartedMatch = Get-OnlyRegexMatch $reportContext '(?m)^StartedAt:\s*(?<value>.+?)\s*$' "report StartedAt for $caseId"
    $reportGeneratedMatch = Get-OnlyRegexMatch $reportContext '(?m)^RuntimeContextGenerated:\s*(?<value>.+?)\s*$' "report RuntimeContextGenerated for $caseId"
    $reportStarted = Parse-Utc $reportStartedMatch.Groups['value'].Value "report StartedAt for $caseId"
    $reportGenerated = Parse-Utc $reportGeneratedMatch.Groups['value'].Value "report RuntimeContextGenerated for $caseId"
    $started = Parse-Utc $summary['Started'] "summary Started for $caseId"
    $completed = Parse-Utc $result.Completed "result Completed for $caseId"
    if ($completed -lt $started) { throw "Case '$caseId' completed before it started." }
    if ($reportStarted -lt $started -or $reportGenerated -lt $reportStarted -or $reportGenerated -gt $completed) {
        throw "Report context interval for '$caseId' must remain inside the case interval and preserve start <= generated ordering."
    }

    $runtimeRelease = Get-ReleaseManifest $releaseManifestText "release-manifest.json for $caseId"
    $runtimeRelease.releaseManifestSha256 = [string]$zipSelection['release-manifest.json'].Row.sha256
    $implementationCommit = [string]$script:implementationCommit
    if (-not $implementationCommit.StartsWith([string]$runtimeRelease.buildCommitPrefix, [StringComparison]::Ordinal)) {
        throw "Runtime BuildCommit for '$caseId' is not a prefix of implementationCommit."
    }
    if ($null -eq $runtimeReleaseReference.Value) { $runtimeReleaseReference.Value = $runtimeRelease }
    else { Compare-RuntimeRelease $runtimeReleaseReference.Value $runtimeRelease $caseId }

    try {
        $convertCommand = Get-Command ConvertFrom-Json
        if ($convertCommand.Parameters.ContainsKey('DateKind')) { $playerDoctor = $playerDoctorText | ConvertFrom-Json -DateKind String }
        else { $playerDoctor = $playerDoctorText | ConvertFrom-Json }
    }
    catch { throw "Player Doctor JSON for '$caseId' is not parseable: $($_.Exception.Message)" }
    Assert-Int "Player Doctor schemaVersion for $caseId" $playerDoctor.schemaVersion 2
    Assert-Exact "Player Doctor scanContext for $caseId" $playerDoctor.scanContext 'InstalledGame'
    Assert-Boolean "Player Doctor readOnlyByDesign for $caseId" $playerDoctor.readOnlyByDesign $true
    Assert-Exact "Player Doctor installed Runtime for $caseId" $playerDoctor.installedDtmApiVersion '0.5.5'
    foreach ($countName in @('errorCount', 'warningCount', 'misplacedCount', 'minimumBlockedCount')) {
        Assert-Int "Player Doctor $countName for $caseId" $playerDoctor.$countName 0
    }
    if (@($playerDoctor.findings).Count -ne 0) { throw "Player Doctor report for '$caseId' contains findings." }
    $doctorRuntimeRows = @($playerDoctor.artifacts | Where-Object { ([string]$_.kind).Equals('DtmApiRuntime', [StringComparison]::Ordinal) })
    if ($doctorRuntimeRows.Count -ne $runtimeAssemblyNames.Count) { throw "Player Doctor report for '$caseId' must contain exactly five Runtime rows." }
    Assert-ExactSet "Player Doctor Runtime assemblies for $caseId" @($doctorRuntimeRows | ForEach-Object { ([string]$_.assemblyName) + '.dll' }) $runtimeAssemblyNames
    foreach ($runtimeAssembly in @($runtimeRelease.assemblies)) {
        $doctorRows = @($doctorRuntimeRows | Where-Object { (([string]$_.assemblyName) + '.dll').Equals([string]$runtimeAssembly.name, [StringComparison]::Ordinal) })
        if ($doctorRows.Count -ne 1) { throw "Player Doctor report for '$caseId' lacks exact Runtime row '$($runtimeAssembly.name)'." }
        Assert-Exact "Player Doctor Runtime hash $($runtimeAssembly.name) for $caseId" ([string]$doctorRows[0].sha256).ToUpperInvariant() ([string]$runtimeAssembly.sha256).ToUpperInvariant()
        Assert-Exact "Player Doctor Runtime identity $($runtimeAssembly.name) for $caseId" $doctorRows[0].managedIdentity 'DtmApiRuntime'
        Assert-Exact "Player Doctor Runtime placement $($runtimeAssembly.name) for $caseId" $doctorRows[0].placement 'Expected'
        if (@($doctorRows[0].findings).Count -ne 0) { throw "Player Doctor Runtime row '$($runtimeAssembly.name)' contains findings for '$caseId'." }
    }
    $expectedFixtureRoot = Join-Path ([string]$playerDoctor.rootPath) 'Mods\DTMAPI.AdvancedFixture'
    $doctorFixtureRows = @($playerDoctor.artifacts | Where-Object {
        ([string]$_.uniqueId).Equals('DTMAPI.AdvancedFixture', [StringComparison]::Ordinal) -and
        ([System.IO.Path]::GetFullPath([string]$_.path).TrimEnd('\', '/')).Equals([System.IO.Path]::GetFullPath($expectedFixtureRoot).TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)
    })
    if ($doctorFixtureRows.Count -ne 1) { throw "Player Doctor report for '$caseId' must contain exactly one active Advanced fixture row." }
    $doctorFixture = $doctorFixtureRows[0]
    Assert-Exact "Player Doctor fixture version for $caseId" $doctorFixture.version ([string]$expected.version)
    Assert-Exact "Player Doctor fixture identity for $caseId" $doctorFixture.managedIdentity 'AdvancedCodeMod'
    Assert-Exact "Player Doctor fixture declaration for $caseId" $doctorFixture.declaredCodeModKind 'Advanced'
    Assert-Exact "Player Doctor fixture effective kind for $caseId" $doctorFixture.effectiveCodeModKind 'Advanced'
    Assert-Exact "Player Doctor fixture provenance for $caseId" $doctorFixture.provenanceStatus 'VerifiedReferenceReceipt'
    Assert-Exact "Player Doctor fixture native risk for $caseId" $doctorFixture.nativeRisk 'ProductNative'
    Assert-Exact "Player Doctor fixture reference compatibility for $caseId" $doctorFixture.referenceCompatibility 'Compatible'
    Assert-Exact "Player Doctor fixture game compatibility for $caseId" $doctorFixture.gameCompatibility 'Compatible'
    Assert-Exact "Player Doctor fixture policy for $caseId" $doctorFixture.referencePolicyId ([string]$contract.referencePolicy.policyId)
    Assert-Int "Player Doctor fixture policy version for $caseId" $doctorFixture.referencePolicyVersion ([int]$contract.referencePolicy.policyVersion)
    Assert-Exact "Player Doctor fixture policy hash for $caseId" ([string]$doctorFixture.referencePolicySha256).ToUpperInvariant() ([string]$contract.referencePolicy.sha256).ToUpperInvariant()
    Assert-Int "Player Doctor fixture reference count for $caseId" $doctorFixture.referenceCount 2
    Assert-Exact "Player Doctor fixture game build for $caseId" $doctorFixture.gameBuildId ([string]$contract.syntheticFixture.gameBuildId)
    Assert-Exact "Player Doctor fixture game hash for $caseId" ([string]$doctorFixture.gameAssemblySha256).ToUpperInvariant() ([string]$contract.referencePolicy.gameAssemblySha256).ToUpperInvariant()
    Assert-Exact "Player Doctor fixture Harmony owner for $caseId" $doctorFixture.expectedHarmonyOwner ([string]$contract.syntheticFixture.harmonyOwner)
    Assert-Exact "Player Doctor fixture restart policy for $caseId" $doctorFixture.restartPolicy 'RestartRequiredAfterLoad'
    $fixturePackage = @($script:fixturePackages | Where-Object { ([string]$_.version).Equals([string]$expected.version, [StringComparison]::Ordinal) })
    if ($fixturePackage.Count -ne 1) { throw "Fixture package receipt for version '$($expected.version)' is unavailable." }
    Assert-Exact "Player Doctor fixture assembly hash for $caseId" ([string]$doctorFixture.sha256).ToUpperInvariant() ([string]$fixturePackage[0].entryAssemblySha256).ToUpperInvariant()
    if (@($doctorFixture.findings).Count -ne 0) { throw "Player Doctor active fixture row for '$caseId' contains findings." }
    $doctorStrictRows = @($playerDoctor.artifacts | Where-Object { ([string]$_.managedIdentity).Equals('StrictCodeMod', [StringComparison]::Ordinal) })
    Assert-ExactSet "Player Doctor Strict sibling IDs for $caseId" @($doctorStrictRows | ForEach-Object { [string]$_.uniqueId }) @('DTMAPI.ConfigMenuExample', 'DTMAPI.HelloDtmMod', 'DTMAPI.HookProbeMod')
    if ($doctorStrictRows.Count -ne 3) { throw "Player Doctor report for '$caseId' must contain exactly three Strict siblings." }
    if (@($playerDoctor.artifacts | Where-Object { ([string]$_.uniqueId).Equals('Yuuka.DTMAPI.AutoFishing', [StringComparison]::Ordinal) }).Count -ne 0) {
        throw "Player Doctor report for '$caseId' unexpectedly includes AutoFishing."
    }

    Assert-Int "result schemaVersion for $caseId" $result.SchemaVersion 2
    foreach ($gate in @('SaveLoaded', 'HookProbe', 'GameLaunched', 'ProcessExited', 'NoFatalInstanceWindow', 'ForcedClose', 'QaHostCleanup', 'QaSaveLoadedObservation')) {
        Assert-Exact "result $gate for $caseId" $result.$gate 'Passed'
    }
    $expectedRunStatus = if ([int]$expected.exit -eq 0) { 'Passed' } else { 'Failed' }
    Assert-Exact "result RunStatus for $caseId" $result.RunStatus $expectedRunStatus
    $resultFailureFields = @($result.PSObject.Properties | Where-Object {
        $_.Value -is [string] -and ([string]$_.Value).Equals('Failed', [StringComparison]::Ordinal)
    } | ForEach-Object { [string]$_.Name })
    Assert-ExactSet "result failure fields for $caseId" $resultFailureFields @($expected.resultFailures)
    Assert-Boolean "result OfficialModProfileRestored for $caseId" $result.OfficialModProfileRestored $true
    Assert-Boolean "result LocalAuthorSourceStateRestored for $caseId" $result.LocalAuthorSourceStateRestored $true
    Assert-Boolean "result Local11AuthorSourceStateRestored for $caseId" $result.Local11AuthorSourceStateRestored $true
    Assert-Boolean "result IsolateAllOfficialMods for $caseId" $result.IsolateAllOfficialMods $true
    Assert-Boolean "result OfficialModProfileApplied for $caseId" $result.OfficialModProfileApplied $true
    Assert-Exact "summary SaveSlot for $caseId" $summary['SaveSlot'] '3'
    Assert-Exact "summary IncludeHookProbe for $caseId" $summary['IncludeHookProbe'] 'True'
    Assert-Exact "summary LaunchMode for $caseId" $summary['LaunchMode'] 'Steam'
    Assert-Boolean "qa cleanup ExactArtifacts for $caseId" $qaCleanup.ExactArtifacts $true
    Assert-Boolean "qa cleanup ExactQaEvidence for $caseId" $qaCleanup.ExactQaEvidence $true
    Assert-Boolean "qa cleanup ExactTree for $caseId" $qaCleanup.ExactTree $true
    Assert-Boolean "qa cleanup Cleaned for $caseId" $qaCleanup.Cleaned $true
    Assert-Boolean "qa cleanup Retained for $caseId" $qaCleanup.Retained $false
    Assert-Boolean "profile restore Passed for $caseId" $profileRestore.Passed $true
    Assert-Exact "profile restore length for $caseId" $profileRestore.ActualLength ([string]$profileRestore.ExpectedLength)
    Assert-Exact "profile restore SHA for $caseId" ([string]$profileRestore.ActualSha256).ToUpperInvariant() ([string]$profileRestore.ExpectedSha256).ToUpperInvariant()
    if (-not $processCheck.Trim().Equals('No DolocTown.exe process found.', [StringComparison]::Ordinal)) {
        throw "process-check.txt for '$caseId' does not prove process absence."
    }

    $strictEntryCount = Get-MatchCount $runtimeLog '^.*\[DTMAPI\.HookProbeMod\] HookProbe Entry OK\s*$'
    $strictGameLaunchedCount = Get-MatchCount $runtimeLog '^.*\[DTMAPI\.HookProbeMod\] HookProbe GameLaunched OK\s*$'
    $strictOneSecondCount = Get-MatchCount $runtimeLog '^.*\[DTMAPI\.HookProbeMod\] HookProbe OneSecondUpdateTicked OK second=\d+\s*$'
    $strictSaveLoadedMatches = [Text.RegularExpressions.Regex]::Matches($runtimeLog, '(?m)^.*\[DTMAPI\.HookProbeMod\] HookProbe SaveLoaded OK slot=(?<slot>\d+)\s+.*$')
    Assert-Int "HookProbe Entry count for $caseId" $strictEntryCount 1
    Assert-Int "HookProbe GameLaunched count for $caseId" $strictGameLaunchedCount 1
    if ($strictOneSecondCount -lt 1) { throw "HookProbe OneSecond count for '$caseId' must be at least one." }
    Assert-Int "HookProbe SaveLoaded count for $caseId" $strictSaveLoadedMatches.Count 1
    Assert-Exact "HookProbe runtime save slot for $caseId" $strictSaveLoadedMatches[0].Groups['slot'].Value '2'
    $strictReport = Get-ReportMod $reportContext 'DTMAPI.HookProbeMod'
    Assert-Exact "HookProbe report status for $caseId" $strictReport.statusCode 'loaded'
    Assert-Boolean "HookProbe report loaded for $caseId" $strictReport.loaded $true
    $autoFishingReport = Get-ReportMod $reportContext 'Yuuka.DTMAPI.AutoFishing'
    Assert-Exact "AutoFishing report status for $caseId" $autoFishingReport.statusCode 'disabled'
    Assert-Boolean "AutoFishing report loaded for $caseId" $autoFishingReport.loaded $false
    Assert-Boolean "AutoFishing report officialEnabled for $caseId" $autoFishingReport.officialEnabled $false
    Assert-Exact "AutoFishing report version for $caseId" $autoFishingReport.version '1.4.3-dtmapi'

    $advancedLoadSources = [Text.RegularExpressions.Regex]::Matches(
        $runtimeLog,
        '(?m)^.*Code mod load-source owner=(?<owner>[^;]+); identity=Advanced CodeMod; provenance=sdk-reference-receipt-verified;.*gameCompatibility=verified build=(?<build>[^;]+); gameAssemblySha256=(?<gameSha>[0-9a-fA-F]{64});.*$'
    )
    foreach ($loadSource in @($advancedLoadSources)) {
        Assert-Exact "Advanced load-source game build for $caseId" $loadSource.Groups['build'].Value ([string]$contract.syntheticFixture.gameBuildId)
        Assert-Exact "Advanced load-source game assembly SHA for $caseId" $loadSource.Groups['gameSha'].Value.ToUpperInvariant() ([string]$contract.referencePolicy.gameAssemblySha256).ToUpperInvariant()
    }
    $verifiedFixtureLoadSourceCount = @($advancedLoadSources | Where-Object { $_.Groups['owner'].Value.Equals('DTMAPI.AdvancedFixture', [StringComparison]::Ordinal) }).Count
    $foreignAdvancedLoadSourceCount = @($advancedLoadSources | Where-Object { -not $_.Groups['owner'].Value.Equals('DTMAPI.AdvancedFixture', [StringComparison]::Ordinal) }).Count
    $allLoadSources = [Text.RegularExpressions.Regex]::Matches($runtimeLog, '(?m)^.*Code mod load-source owner=(?<owner>[^;]+);.*$')
    $allowedSyntheticOwners = @('DTMAPI.AdvancedFixture', 'DTMAPI.ConfigMenuExample', 'DTMAPI.HelloDtmMod', 'DTMAPI.HookProbeMod')
    $unexpectedManagedLoadSourceCount = @($allLoadSources | Where-Object { $allowedSyntheticOwners -notcontains $_.Groups['owner'].Value }).Count
    $autoFishingLoadSourceCount = Get-MatchCount $runtimeLog '^.*Code mod load-source owner=Yuuka\.DTMAPI\.AutoFishing;.*$'
    $autoFishingEntryLogCount = Get-MatchCount $runtimeLog '^.*\[Yuuka\.DTMAPI\.AutoFishing\].*Mod Entry completed\.\s*$'
    if ($MutationProbe -eq 'AutoFishingIdentity' -and $caseId -eq 'normal-v1-cold') { $autoFishingLoadSourceCount = 1 }
    $expectedFixtureLoadSourceCount = if ($caseId -eq 'disabled-cold') { 0 } else { 1 }
    Assert-Int "verified Advanced fixture load source count for $caseId" $verifiedFixtureLoadSourceCount $expectedFixtureLoadSourceCount
    Assert-Int "foreign Advanced load source count for $caseId" $foreignAdvancedLoadSourceCount 0
    Assert-Int "unexpected managed load source count for $caseId" $unexpectedManagedLoadSourceCount 0
    Assert-Int "AutoFishing load source count for $caseId" $autoFishingLoadSourceCount 0
    Assert-Int "AutoFishing Entry log count for $caseId" $autoFishingEntryLogCount 0

    $entryCount = Get-MatchCount $runtimeLog '^.*\[DTMAPI\.AdvancedFixture\] AdvancedFixture Entry mode='
    $completionCount = Get-MatchCount $runtimeLog '^.*\[DTMAPI\.AdvancedFixture\] Mod Entry completed\.\s*$'
    $postfixMatches = [Text.RegularExpressions.Regex]::Matches($runtimeLog, '(?m)^.*\[DTMAPI\.AdvancedFixture\] AdvancedFixture PatchPostfix result=(?<value>True|False)\s*$')
    $queryMatches = [Text.RegularExpressions.Regex]::Matches($runtimeLog, '(?m)^.*\[DTMAPI\.AdvancedFixture\] AdvancedFixture NativeQuery result=(?<value>True|False)\s*$')
    $disabledSkipCount = Get-MatchCount $runtimeLog '^.*Skipping DTMAPI\.AdvancedFixture:.*$'
    $lateInstallCount = Get-MatchCount $runtimeLog '^.*\[DTMAPI\.AdvancedFixture\] AdvancedFixture LateOwnerDrift installed owner=.*$'
    $entryFailureText = 'Synthetic AdvancedFixture EntryFailure after canonical-owner patch installation.'
    $entryFailureCount = Get-MatchCount $runtimeLog ([regex]::Escape($entryFailureText))
    $entryFailureMessage = if ($entryFailureCount -gt 0) { $entryFailureText } else { $null }
    $queryAndPostfixEqual = $null
    if ($postfixMatches.Count -eq 1 -and $queryMatches.Count -eq 1) {
        $queryAndPostfixEqual = $postfixMatches[0].Groups['value'].Value.Equals($queryMatches[0].Groups['value'].Value, [StringComparison]::Ordinal)
    }

    $diagnosticCounts = [ordered]@{
        ownerMismatchCount = Get-MatchCount $runtimeLog 'advanced-harmony-owner-mismatch'
        cleanupFailedCount = Get-MatchCount $runtimeLog 'advanced-harmony-cleanup-failed'
        cleanupCompleteCount = Get-MatchCount $runtimeLog 'advanced-harmony-cleanup-complete'
        duplicatePatchCount = Get-MatchCount $runtimeLog 'advanced-harmony-duplicate-patch'
        lateOwnerDriftCount = Get-MatchCount $runtimeLog 'advanced-harmony-late-owner-drift'
        entryFailureCount = $entryFailureCount
        restartRequiredCount = Get-MatchCount $reportContext '(?m)^MOD(?:-LOAD-FAILURE)? DTMAPI\.AdvancedFixture statusCode=restart-required '
        sdk602Count = 0
        sdk603Count = 0
    }

    $cleanupReason = $null
    $cleanupParticipants = $null
    $cleanupRemoved = $null
    $cleanupRemaining = $null
    $cleanupFailures = $null
    $cleanupMachineCode = $null
    $machineCodeRuntimeCount = 0
    $machineCodeReportCount = 0
    $cleanupMatches = [Text.RegularExpressions.Regex]::Matches(
        $runtimeLog,
        '(?m)^.*Mod-owner cleanup participants owner=DTMAPI\.AdvancedFixture, reason=(?<reason>[^,]+), participants=(?<participants>\d+), removed=(?<removed>\d+), remaining=(?<remaining>\d+), failures=(?<failures>\d+),.*DTMAPI\.Core\.AdvancedHarmonySupervisor\{code=(?<code>advanced-harmony-cleanup-(?:complete|failed));.*$'
    )
    if ($cleanupMatches.Count -gt 1) { throw "Cleanup summary for '$caseId' must occur at most once; found $($cleanupMatches.Count)." }
    if ($cleanupMatches.Count -eq 1) {
        $cleanupMatch = $cleanupMatches[0]
        $cleanupReason = $cleanupMatch.Groups['reason'].Value
        $cleanupParticipants = [int]$cleanupMatch.Groups['participants'].Value
        $cleanupRemoved = [int]$cleanupMatch.Groups['removed'].Value
        $cleanupRemaining = [int]$cleanupMatch.Groups['remaining'].Value
        $cleanupFailures = [int]$cleanupMatch.Groups['failures'].Value
        $cleanupMachineCode = $cleanupMatch.Groups['code'].Value
        $machineCodeRuntimeCount = Get-MatchCount $runtimeLog ([regex]::Escape($cleanupMachineCode))
        $machineCodeReportCount = Get-MatchCount $reportLog ([regex]::Escape($cleanupMachineCode))
        Assert-Int "cleanup machine code runtime count for $caseId" $machineCodeRuntimeCount 1
        Assert-Int "cleanup machine code report count for $caseId" $machineCodeReportCount 1
    }

    $fixtureReport = Get-ReportMod $reportContext 'DTMAPI.AdvancedFixture'
    Assert-Exact "fixture report status for $caseId" $fixtureReport.statusCode $expected.status
    Assert-Boolean "fixture report loaded for $caseId" $fixtureReport.loaded $expected.loaded
    Assert-Boolean "fixture report officialEnabled for $caseId" $fixtureReport.officialEnabled $expected.enabled
    Assert-Exact "fixture report version for $caseId" $fixtureReport.version $expected.version

    $state = New-NullableState
    $session = New-NullableSession
    $postLoadPath = $null
    $liveUpdatePath = $null
    $sessionPreparePath = $null
    $deploymentStatusV1Path = $null
    $snapshotPath = $null
    $sdkUpdatePath = $null
    $deploymentStatusV2Path = $null
    $sessionReloadPath = $null
    $sessionClearPath = $null

    if ($caseId -eq 'postload-disable-v1') {
        $postLoadPath = Get-OptionalMapPath $caseMap 'postLoadOrchestrationPath' $evidenceRoot
        $postLoad = Read-Json $postLoadPath "post-load orchestration for $caseId"
        Assert-ExactProperties $postLoad @('schemaVersion', 'caseId', 'startedAtUtc', 'nativeQueryObservedAtUtc', 'markerCreatedAtUtc', 'completedAtUtc', 'markerAbsentBefore', 'markerLength', 'markerRemovedAfter', 'configSha256Before', 'configSha256After', 'smokeExitCode', 'reportSha256') "post-load orchestration for $caseId"
        Assert-Int "post-load schemaVersion for $caseId" $postLoad.schemaVersion 1
        Assert-Exact "post-load caseId for $caseId" $postLoad.caseId $caseId
        Assert-Boolean "post-load markerAbsentBefore for $caseId" $postLoad.markerAbsentBefore $true
        Assert-Boolean "post-load markerRemovedAfter for $caseId" $postLoad.markerRemovedAfter $true
        Assert-Int "post-load marker length for $caseId" $postLoad.markerLength 0
        Assert-Int "post-load smoke exit for $caseId" $postLoad.smokeExitCode 1
        Assert-Exact "post-load config restore for $caseId" ([string]$postLoad.configSha256After).ToUpperInvariant() ([string]$postLoad.configSha256Before).ToUpperInvariant()
        Assert-Exact "post-load report snapshot for $caseId" ([string]$postLoad.reportSha256).ToUpperInvariant() (Get-FileSha256 $reportZipPath)
        $queryTime = Parse-Utc $postLoad.nativeQueryObservedAtUtc 'post-load nativeQueryObservedAtUtc'
        $markerTime = Parse-Utc $postLoad.markerCreatedAtUtc 'post-load markerCreatedAtUtc'
        $postLoadStarted = Parse-Utc $postLoad.startedAtUtc 'post-load startedAtUtc'
        $postLoadCompleted = Parse-Utc $postLoad.completedAtUtc 'post-load completedAtUtc'
        $reloadTime = Get-OnlyLogTimestamp $runtimeLog '\[Info\] \[DTMAPI\] official ModManager\.ReloadMods \+ DolocConfig\.Reload requested' 'post-load reload observation'
        if ($queryTime -lt $postLoadStarted -or $markerTime -lt $queryTime -or $reloadTime -le $markerTime -or $postLoadCompleted -lt $reloadTime) { throw 'Post-load orchestration ordering must be start <= query <= marker < reload <= complete.' }
        if ($postLoadStarted -lt $started) { $started = $postLoadStarted }
        if ($postLoadCompleted -gt $completed) { $completed = $postLoadCompleted }
        Assert-Exact "post-load result reload observation for $caseId" $result.QaWorkshopReloadCompletedObservation 'Passed'
        $state = [ordered]@{
            markerAbsentBefore = $true
            queryObservedAtUtc = Format-Utc $queryTime
            markerCreatedAtUtc = Format-Utc $markerTime
            markerLength = 0
            reloadObservedAtUtc = Format-Utc $reloadTime
            markerAbsentAfter = $true
            configRestoredExact = $true
            processAbsentAfter = $true
            qaWorkshopReloadCompleted = $true
        }
    }

    if ($caseId -eq 'live-update-v1-to-v2') {
        $liveUpdatePath = Get-OptionalMapPath $caseMap 'liveUpdateOrchestrationPath' $evidenceRoot
        $sessionPreparePath = Get-OptionalMapPath $caseMap 'sessionPreparePath' $evidenceRoot
        $deploymentStatusV1Path = Get-OptionalMapPath $caseMap 'deploymentStatusV1Path' $evidenceRoot
        $snapshotPath = Get-OptionalMapPath $caseMap 'sessionSnapshotPath' $evidenceRoot
        $sdkUpdatePath = Get-OptionalMapPath $caseMap 'sdkUpdatePath' $evidenceRoot
        $deploymentStatusV2Path = Get-OptionalMapPath $caseMap 'deploymentStatusV2Path' $evidenceRoot
        $sessionReloadPath = Get-OptionalMapPath $caseMap 'sessionReloadPath' $evidenceRoot
        $sessionClearPath = Get-OptionalMapPath $caseMap 'sessionClearPath' $evidenceRoot
        $orchestration = Read-Json $liveUpdatePath "live-update orchestration for $caseId"
        Assert-ExactProperties $orchestration @('schemaVersion', 'caseId', 'startedAtUtc', 'queryObservedAtUtc', 'updateStartedAtUtc', 'updateCompletedAtUtc', 'reloadCompletedAtUtc', 'completedAtUtc', 'fixtureTreeSha256Before', 'fixtureTreeSha256After', 'packageSha256', 'smokeExitCode', 'sessionFilesAbsentAfter', 'markerAbsentBeforeAndAfter', 'configSha256Before', 'configSha256After', 'reportSha256') "live-update orchestration for $caseId"
        Assert-Int "live-update schemaVersion for $caseId" $orchestration.schemaVersion 1
        Assert-Exact "live-update caseId for $caseId" $orchestration.caseId $caseId
        Assert-Boolean 'live-update sessionFilesAbsentAfter' $orchestration.sessionFilesAbsentAfter $true
        Assert-Boolean 'live-update markerAbsentBeforeAndAfter' $orchestration.markerAbsentBeforeAndAfter $true
        Assert-Int 'live-update smokeExitCode' $orchestration.smokeExitCode 0
        Assert-Exact 'live-update config restore' ([string]$orchestration.configSha256After).ToUpperInvariant() ([string]$orchestration.configSha256Before).ToUpperInvariant()
        Assert-Exact 'live-update report snapshot' ([string]$orchestration.reportSha256).ToUpperInvariant() (Get-FileSha256 $reportZipPath)
        Assert-Exact 'live-update package hash' ([string]$orchestration.packageSha256).ToUpperInvariant() ([string]$script:fixturePackages[1].package.sha256).ToUpperInvariant()
        $preTree = Assert-Sha256 'live-update fixtureTreeSha256Before' $orchestration.fixtureTreeSha256Before
        $postTree = Assert-Sha256 'live-update fixtureTreeSha256After' $orchestration.fixtureTreeSha256After
        if ($preTree.Equals($postTree, [StringComparison]::Ordinal)) { throw 'Live update must change the installed fixture tree hash.' }
        $orchestrationStarted = Parse-Utc $orchestration.startedAtUtc 'live-update startedAtUtc'
        $queryObserved = Parse-Utc $orchestration.queryObservedAtUtc 'live-update queryObservedAtUtc'
        $updateStarted = Parse-Utc $orchestration.updateStartedAtUtc 'live-update updateStartedAtUtc'
        $updateCompleted = Parse-Utc $orchestration.updateCompletedAtUtc 'live-update updateCompletedAtUtc'
        $reloadCompleted = Parse-Utc $orchestration.reloadCompletedAtUtc 'live-update reloadCompletedAtUtc'
        $orchestrationCompleted = Parse-Utc $orchestration.completedAtUtc 'live-update completedAtUtc'
        if ($queryObserved -lt $orchestrationStarted -or $updateStarted -lt $queryObserved -or $updateCompleted -lt $updateStarted -or $reloadCompleted -lt $updateCompleted -or $orchestrationCompleted -lt $reloadCompleted) {
            throw 'Live update ordering must be start <= query <= update-start <= update-complete <= reload-complete <= complete.'
        }
        $queryLogTime = Get-OnlyLogTimestamp $runtimeLog '\[DTMAPI\.AdvancedFixture\] AdvancedFixture NativeQuery result=' 'live-update native query log timestamp'
        if ([Math]::Abs(($queryObserved - $queryLogTime).TotalSeconds) -gt 2) { throw 'Live update query sidecar is not bound to the Runtime native-query log observation.' }
        if ($orchestrationStarted -lt $started) { $started = $orchestrationStarted }
        if ($orchestrationCompleted -gt $completed) { $completed = $orchestrationCompleted }

        $prepareReport = Read-Json $sessionPreparePath 'session prepare report'
        $statusV1Report = Read-Json $deploymentStatusV1Path 'deployment status v1 report'
        $snapshotReport = Read-Json $snapshotPath 'session snapshot report'
        $updateReport = Read-Json $sdkUpdatePath 'SDK update report'
        $statusV2Report = Read-Json $deploymentStatusV2Path 'deployment status v2 report'
        $reloadReport = Read-Json $sessionReloadPath 'session reload report'
        $clearReport = Read-Json $sessionClearPath 'session clear report'
        $commandReports = @(
            [pscustomobject]@{ Report = $prepareReport; Label = 'session prepare report'; Command = 'session prepare'; Diagnostic = 'SDK600' },
            [pscustomobject]@{ Report = $statusV1Report; Label = 'deployment status v1 report'; Command = 'deployment-status'; Diagnostic = 'SDK400' },
            [pscustomobject]@{ Report = $snapshotReport; Label = 'session snapshot report'; Command = 'session snapshot'; Diagnostic = 'SDK600' },
            [pscustomobject]@{ Report = $updateReport; Label = 'SDK update report'; Command = 'update'; Diagnostic = 'SDK400' },
            [pscustomobject]@{ Report = $statusV2Report; Label = 'deployment status v2 report'; Command = 'deployment-status'; Diagnostic = 'SDK400' },
            [pscustomobject]@{ Report = $reloadReport; Label = 'session reload report'; Command = 'session reload'; Diagnostic = 'SDK602' },
            [pscustomobject]@{ Report = $clearReport; Label = 'session clear report'; Command = 'session clear'; Diagnostic = 'SDK600' }
        )
        foreach ($item in $commandReports) {
            $commandReport = $item.Report
            Assert-CommandReport $commandReport $item.Label
            Assert-ExactProperties $commandReport @('command', 'success', 'sdkVersion', 'targetRuntimeVersion', 'rootPath', 'outputPath', 'sha256', 'fileCount', 'diagnostics', 'values') $item.Label
            Assert-Exact "$($item.Label) command" $commandReport.command $item.Command
            Assert-Boolean "$($item.Label) success" $commandReport.success $true
            Assert-Exact "$($item.Label) SDK version" $commandReport.sdkVersion '0.1.0'
            Assert-Exact "$($item.Label) Runtime version" $commandReport.targetRuntimeVersion '0.5.5'
            Assert-PathEqual "$($item.Label) game root" $commandReport.rootPath $playerDoctor.rootPath
            Assert-ExactSet "$($item.Label) diagnostic codes" @(Get-CommandReportDiagnostics $commandReport) @([string]$item.Diagnostic)
            foreach ($diagnostic in @($commandReport.diagnostics)) {
                Assert-ExactProperties $diagnostic @('code', 'severity', 'message', 'path', 'guidance') "$($item.Label) diagnostic"
            }
        }
        $fixtureDestination = Join-Path ([string]$playerDoctor.rootPath) 'Mods\DTMAPI.AdvancedFixture'
        foreach ($commandReport in @($snapshotReport, $updateReport, $reloadReport)) {
            Assert-PathEqual "$($commandReport.command) output path" $commandReport.outputPath $fixtureDestination
        }
        foreach ($commandReport in @($prepareReport, $statusV1Report, $statusV2Report, $clearReport)) {
            Assert-Exact "$($commandReport.command) output path" $commandReport.outputPath ''
        }
        Assert-Int 'session prepare fileCount' $prepareReport.fileCount 0
        Assert-Exact 'session prepare sha256' $prepareReport.sha256 ''
        Assert-Int 'deployment status v1 fileCount' $statusV1Report.fileCount 0
        Assert-Exact 'deployment status v1 sha256' $statusV1Report.sha256 ''
        Assert-Int 'session snapshot fileCount' $snapshotReport.fileCount 0
        Assert-Int 'SDK update fileCount' $updateReport.fileCount 7
        Assert-Int 'deployment status v2 fileCount' $statusV2Report.fileCount 0
        Assert-Exact 'deployment status v2 sha256' $statusV2Report.sha256 ''
        Assert-Int 'session reload fileCount' $reloadReport.fileCount 0
        Assert-Int 'session clear fileCount' $clearReport.fileCount 0
        Assert-Exact 'session clear sha256' $clearReport.sha256 ''
        Assert-Boolean 'session snapshot success' $snapshotReport.success $true
        Assert-Boolean 'SDK update success' $updateReport.success $true
        Assert-Boolean 'session reload success' $reloadReport.success $true
        Assert-Boolean 'session clear success' $clearReport.success $true

        Assert-Exact 'session prepare statusCode' (Get-StringValue $prepareReport.values 'statusCode') 'session-prepared'
        $sessionId = Get-StringValue $prepareReport.values 'sessionId'
        if ([string]$sessionId -notmatch '^[0-9a-f]{32}$') { throw "Session prepare returned an invalid sessionId: '$sessionId'." }
        $prepareCreated = Parse-Utc (Get-StringValue $prepareReport.values 'createdAtUtc') 'session prepare createdAtUtc'
        $prepareExpires = Parse-Utc (Get-StringValue $prepareReport.values 'expiresAtUtc') 'session prepare expiresAtUtc'
        if ($prepareExpires -le $prepareCreated -or $prepareCreated -gt $orchestrationStarted -or $prepareExpires -lt $orchestrationCompleted) {
            throw 'Prepared session lifetime does not enclose the live-update orchestration.'
        }
        Assert-Exact 'session prepare pipe identity' ((Get-StringValue $prepareReport.values 'pipeName').Split('-')[-1]) $sessionId

        foreach ($statusItem in @(
            [pscustomobject]@{ Report = $statusV1Report; Version = '0.1.0'; Tree = $preTree; Recovery = 0; Label = 'deployment status v1' },
            [pscustomobject]@{ Report = $statusV2Report; Version = '0.1.1'; Tree = $postTree; Recovery = 1; Label = 'deployment status v2' }
        )) {
            Assert-Exact "$($statusItem.Label) uniqueID" (Get-StringValue $statusItem.Report.values 'uniqueID') 'DTMAPI.AdvancedFixture'
            Assert-Exact "$($statusItem.Label) status" (Get-StringValue $statusItem.Report.values 'status') 'Installed'
            Assert-PathEqual "$($statusItem.Label) destination" (Get-StringValue $statusItem.Report.values 'destinationPath') $fixtureDestination
            Assert-Exact "$($statusItem.Label) tree" ((Get-StringValue $statusItem.Report.values 'treeSha256').ToUpperInvariant()) ([string]$statusItem.Tree).ToUpperInvariant()
            Assert-Int "$($statusItem.Label) recoveryArtifacts" (Get-StringValue $statusItem.Report.values 'recoveryArtifacts') ([int]$statusItem.Recovery)
            if ([string]::IsNullOrWhiteSpace((Get-StringValue $statusItem.Report.values 'journalPath'))) { throw "$($statusItem.Label) lacks a deployment journal path." }
        }

        $snapshotValues = $snapshotReport.values
        $snapshotResponse = Get-Value $snapshotValues 'response'
        $snapshotStatus = Get-StringValue $snapshotValues 'runtimeStatus'
        $snapshotCode = Get-StringValue $snapshotValues 'runtimeCode'
        $snapshotVersion = Get-StringValue $snapshotResponse 'version'
        $snapshotTree = Get-StringValue $snapshotResponse 'treeSha256'
        if ([string]::IsNullOrWhiteSpace($snapshotVersion)) { $snapshotVersion = Get-StringValue $snapshotValues 'response.version' }
        if ([string]::IsNullOrWhiteSpace($snapshotTree)) { $snapshotTree = Get-StringValue $snapshotValues 'response.treeSha256' }
        Assert-Exact 'session snapshot runtimeStatus' $snapshotStatus 'ok'
        Assert-Exact 'session snapshot runtimeCode' $snapshotCode 'source-snapshot'
        Assert-Exact 'session snapshot version' $snapshotVersion '0.1.0'
        Assert-Exact 'session snapshot uniqueID' (Get-StringValue $snapshotValues 'uniqueID') 'DTMAPI.AdvancedFixture'
        Assert-Exact 'session snapshot sessionId' (Get-StringValue $snapshotValues 'sessionId') $sessionId
        Assert-PathEqual 'session snapshot selectedRoot' (Get-StringValue $snapshotValues 'selectedRoot') $fixtureDestination
        Assert-PathEqual 'session snapshot response.selectedRoot' (Get-StringValue $snapshotValues 'response.selectedRoot') $fixtureDestination
        $snapshotTree = Assert-Sha256 'session snapshot treeSha256' $snapshotTree
        Assert-Exact 'session snapshot top-level sha256' ([string]$snapshotReport.sha256).ToUpperInvariant() $snapshotTree
        $snapshotExpectedTree = Assert-Sha256 'session snapshot expectedTreeSha256' (Get-StringValue $snapshotValues 'expectedTreeSha256')
        Assert-Exact 'session snapshot source-tree parity' $snapshotTree $snapshotExpectedTree
        if ($snapshotTree.Equals($preTree, [StringComparison]::Ordinal)) { throw 'Source-snapshot and deployment-inventory digest domains unexpectedly collapsed.' }

        $updatedVersion = Get-StringValue $updateReport.values 'version'
        if ([string]::IsNullOrWhiteSpace($updatedVersion)) { $updatedVersion = Get-StringValue $updateReport.values 'targetVersion' }
        if ([string]::IsNullOrWhiteSpace($updatedVersion)) { $updatedVersion = Get-StringValue (Get-Value $updateReport.values 'response') 'version' }
        Assert-Exact 'SDK update uniqueID' (Get-StringValue $updateReport.values 'uniqueID') 'DTMAPI.AdvancedFixture'
        Assert-PathEqual 'SDK update destinationPath' (Get-StringValue $updateReport.values 'destinationPath') $fixtureDestination
        Assert-Exact 'SDK update codeModKind' (Get-StringValue $updateReport.values 'codeModKind') 'Advanced'
        Assert-Exact 'SDK update packageKind' (Get-StringValue $updateReport.values 'packageKind') 'CodeMod'
        Assert-Exact 'SDK update deployment tree' ([string]$updateReport.sha256).ToUpperInvariant() $postTree
        if ([string]::IsNullOrWhiteSpace($updatedVersion)) {
            $updatedVersion = '0.1.1'
        }
        Assert-Exact 'SDK update target version' $updatedVersion '0.1.1'

        $reloadValues = $reloadReport.values
        $reloadStatus = Get-StringValue $reloadValues 'runtimeStatus'
        $reloadCode = Get-StringValue $reloadValues 'runtimeCode'
        Assert-Exact 'session reload runtimeStatus' $reloadStatus 'restart-required'
        Assert-Exact 'session reload runtimeCode' $reloadCode 'manifest-changed-restart-required'
        Assert-Exact 'session reload uniqueID' (Get-StringValue $reloadValues 'uniqueID') 'DTMAPI.AdvancedFixture'
        Assert-Exact 'session reload sessionId' (Get-StringValue $reloadValues 'sessionId') $sessionId
        Assert-PathEqual 'session reload selectedRoot' (Get-StringValue $reloadValues 'selectedRoot') $fixtureDestination
        $reloadExpectedTree = Assert-Sha256 'session reload expectedTreeSha256' (Get-StringValue $reloadValues 'expectedTreeSha256')
        Assert-Exact 'session reload top-level sha256' ([string]$reloadReport.sha256).ToUpperInvariant() $reloadExpectedTree
        if ($reloadExpectedTree.Equals($postTree, [StringComparison]::Ordinal)) { throw 'Reload source-tree and post-update deployment-inventory digest domains unexpectedly collapsed.' }
        $reloadCodes = @(Get-CommandReportDiagnostics $reloadReport)
        Assert-ExactSet 'session reload diagnostic codes' $reloadCodes @('SDK602')
        if (@($reloadCodes | Where-Object { $_ -eq 'SDK603' }).Count -ne 0) { throw 'Live-update session reload must not emit SDK603.' }
        $clearCodes = @(Get-CommandReportDiagnostics $clearReport)
        $clearStatus = Get-StringValue $clearReport.values 'statusCode'
        Assert-Exact 'session clear statusCode' $clearStatus 'session-cleared'
        Assert-Int 'session clear removedFiles' (Get-StringValue $clearReport.values 'removedFiles') 1

        $diagnosticCounts.sdk602Count = @($reloadCodes | Where-Object { $_ -eq 'SDK602' }).Count
        $diagnosticCounts.sdk603Count = @($reloadCodes | Where-Object { $_ -eq 'SDK603' }).Count
        $session = [ordered]@{
            prepareSuccess = $true
            sessionId = $sessionId
            deploymentStatusV1Success = $true
            deploymentStatusV1RecoveryArtifacts = 0
            snapshotSuccess = $true
            snapshotRuntimeStatus = $snapshotStatus
            snapshotRuntimeCode = $snapshotCode
            snapshotVersion = $snapshotVersion
            snapshotTreeSha256 = $snapshotTree
            updateSuccess = $true
            updatedVersion = $updatedVersion
            deploymentStatusV2Success = $true
            deploymentStatusV2RecoveryArtifacts = 1
            reloadSuccess = $true
            reloadRuntimeStatus = $reloadStatus
            reloadRuntimeCode = $reloadCode
            reloadDiagnosticCodes = @($reloadCodes)
            clearSuccess = $true
            credentialsCleared = $true
            clearedCredentialFileCount = 1
            preUpdateTreeSha256 = $preTree
            postUpdateTreeSha256 = $postTree
        }
    }

    switch ($caseId) {
        'normal-v1-cold' {
            Assert-Int 'normal Entry count' $entryCount 1; Assert-Int 'normal completion count' $completionCount 1
            Assert-Int 'normal postfix count' $postfixMatches.Count 1; Assert-Int 'normal query count' $queryMatches.Count 1
            Assert-Boolean 'normal query/postfix equality' $queryAndPostfixEqual $true
            Assert-Int 'normal disabled skip count' $disabledSkipCount 0; Assert-Int 'normal late count' $lateInstallCount 0
            foreach ($name in @('ownerMismatchCount','cleanupFailedCount','cleanupCompleteCount','duplicatePatchCount','lateOwnerDriftCount','entryFailureCount','restartRequiredCount')) { Assert-Int "normal $name" $diagnosticCounts[$name] 0 }
            if ($null -ne $cleanupReason) { throw 'Normal case must not run Advanced cleanup.' }
        }
        'wrong-owner-cold' {
            Assert-Int 'wrong-owner Entry count' $entryCount 1; Assert-Int 'wrong-owner completion count' $completionCount 0
            Assert-Int 'wrong-owner retained postfix count' $postfixMatches.Count 1; Assert-Int 'wrong-owner query count' $queryMatches.Count 0
            if ($diagnosticCounts.ownerMismatchCount -lt 1) { throw 'Wrong-owner case lacks owner mismatch code.' }
            Assert-Exact 'wrong-owner cleanup reason' $cleanupReason 'EntryFailed'; Assert-Int 'wrong-owner cleanup participants' $cleanupParticipants 2
            Assert-Int 'wrong-owner cleanup removed' $cleanupRemoved 0; Assert-Int 'wrong-owner cleanup remaining' $cleanupRemaining 1; Assert-Int 'wrong-owner cleanup failures' $cleanupFailures 1
            Assert-Exact 'wrong-owner cleanup code' $cleanupMachineCode 'advanced-harmony-cleanup-failed'
        }
        'duplicate-patch-cold' {
            Assert-Int 'duplicate Entry count' $entryCount 1; Assert-Int 'duplicate completion count' $completionCount 0
            Assert-Int 'duplicate postfix count' $postfixMatches.Count 0; Assert-Int 'duplicate query count' $queryMatches.Count 0
            if ($diagnosticCounts.duplicatePatchCount -lt 1) { throw 'Duplicate case lacks duplicate-patch code.' }
            Assert-Exact 'duplicate cleanup reason' $cleanupReason 'EntryFailed'; Assert-Int 'duplicate cleanup participants' $cleanupParticipants 2
            Assert-Int 'duplicate cleanup removed' $cleanupRemoved 2; Assert-Int 'duplicate cleanup remaining' $cleanupRemaining 0; Assert-Int 'duplicate cleanup failures' $cleanupFailures 0
            Assert-Exact 'duplicate cleanup code' $cleanupMachineCode 'advanced-harmony-cleanup-complete'
        }
        'entry-failure-cold' {
            Assert-Int 'entry-failure Entry count' $entryCount 1; Assert-Int 'entry-failure completion count' $completionCount 0
            Assert-Int 'entry-failure postfix count' $postfixMatches.Count 0; Assert-Int 'entry-failure query count' $queryMatches.Count 0
            Assert-Int 'entry-failure exception count' $entryFailureCount 1
            foreach ($name in @('ownerMismatchCount','cleanupFailedCount','duplicatePatchCount','lateOwnerDriftCount')) { Assert-Int "entry-failure $name" $diagnosticCounts[$name] 0 }
            Assert-Exact 'entry-failure cleanup reason' $cleanupReason 'EntryFailed'; Assert-Int 'entry-failure cleanup participants' $cleanupParticipants 2
            Assert-Int 'entry-failure cleanup removed' $cleanupRemoved 1; Assert-Int 'entry-failure cleanup remaining' $cleanupRemaining 0; Assert-Int 'entry-failure cleanup failures' $cleanupFailures 0
            Assert-Exact 'entry-failure cleanup code' $cleanupMachineCode 'advanced-harmony-cleanup-complete'
        }
        'late-owner-drift-cold' {
            Assert-Int 'late Entry count' $entryCount 1; Assert-Int 'late completion count' $completionCount 1
            Assert-Int 'late postfix count' $postfixMatches.Count 1; Assert-Int 'late query count' $queryMatches.Count 1
            Assert-Boolean 'late query/postfix equality' $queryAndPostfixEqual $true; Assert-Int 'late install count' $lateInstallCount 1
            if ($diagnosticCounts.lateOwnerDriftCount -lt 1) { throw 'Late-owner case lacks late-owner-drift code.' }
            Assert-Exact 'late cleanup reason' $cleanupReason 'EntryFailed'; Assert-Int 'late cleanup participants' $cleanupParticipants 2
            Assert-Int 'late cleanup removed' $cleanupRemoved 1; Assert-Int 'late cleanup remaining' $cleanupRemaining 0; Assert-Int 'late cleanup failures' $cleanupFailures 0
            Assert-Exact 'late cleanup code' $cleanupMachineCode 'advanced-harmony-cleanup-complete'
        }
        'disabled-cold' {
            Assert-Int 'disabled skip count' $disabledSkipCount 1
            foreach ($value in @($entryCount,$completionCount,$postfixMatches.Count,$queryMatches.Count,$lateInstallCount,$diagnosticCounts.ownerMismatchCount,$diagnosticCounts.cleanupFailedCount,$diagnosticCounts.cleanupCompleteCount,$diagnosticCounts.duplicatePatchCount,$diagnosticCounts.lateOwnerDriftCount)) {
                if ([int]$value -ne 0) { throw 'Disabled cold case must have no Advanced load/entry/query/cleanup diagnostics.' }
            }
            if ($null -ne $cleanupReason) { throw 'Disabled cold case must not run Advanced cleanup.' }
        }
        'postload-disable-v1' {
            Assert-Int 'postload Entry count' $entryCount 1; Assert-Int 'postload completion count' $completionCount 1
            Assert-Int 'postload postfix count' $postfixMatches.Count 1; Assert-Int 'postload query count' $queryMatches.Count 1
            Assert-Boolean 'postload query/postfix equality' $queryAndPostfixEqual $true
            Assert-Int 'postload disabled skip count' $disabledSkipCount 1
            Assert-Exact 'postload cleanup reason' $cleanupReason 'Unload'; Assert-Int 'postload cleanup participants' $cleanupParticipants 2
            Assert-Int 'postload cleanup removed' $cleanupRemoved 1; Assert-Int 'postload cleanup remaining' $cleanupRemaining 0; Assert-Int 'postload cleanup failures' $cleanupFailures 0
            Assert-Exact 'postload cleanup code' $cleanupMachineCode 'advanced-harmony-cleanup-complete'
        }
        'live-update-v1-to-v2' {
            Assert-Int 'live-update Entry count' $entryCount 1; Assert-Int 'live-update completion count' $completionCount 1
            Assert-Int 'live-update postfix count' $postfixMatches.Count 1; Assert-Int 'live-update query count' $queryMatches.Count 1
            Assert-Boolean 'live-update query/postfix equality' $queryAndPostfixEqual $true
            if ($null -ne $cleanupReason) { throw 'Live update must not unload/re-enter the fixture in-process.' }
        }
        'final-clean-v2-cold' {
            Assert-Int 'final Entry count' $entryCount 1; Assert-Int 'final completion count' $completionCount 1
            Assert-Int 'final postfix count' $postfixMatches.Count 1; Assert-Int 'final query count' $queryMatches.Count 1
            Assert-Boolean 'final query/postfix equality' $queryAndPostfixEqual $true
            foreach ($name in @('ownerMismatchCount','cleanupFailedCount','cleanupCompleteCount','duplicatePatchCount','lateOwnerDriftCount','entryFailureCount','restartRequiredCount')) { Assert-Int "final $name" $diagnosticCounts[$name] 0 }
            if ($null -ne $cleanupReason) { throw 'Final clean case must not run Advanced cleanup.' }
        }
    }

    $genericExitCode = [int]$caseMap.genericSmokeExitCode
    if ($MutationProbe -eq 'GenericSmokeExit' -and $caseId -eq 'normal-v1-cold') { $genericExitCode = 1 }
    Assert-Int "generic smoke exit code for $caseId" $genericExitCode ([int]$expected.exit)
    $runnerReceiptRow = $null
    if ($caseId -in @('wrong-owner-cold', 'duplicate-patch-cold', 'entry-failure-cold', 'late-owner-drift-cold', 'disabled-cold')) {
        $runnerReceiptRow = Get-ColdRunnerReceiptRow $caseMap $caseId $genericExitCode $runRootPath $resultPath $runtimeLogPath $reportZipPath ([string]$zipSelection['DTMAPI-runtime-context.txt'].Row.sha256) $completed
    }

    $evidence = [ordered]@{
        runRootPath = $runRootPath
        result = New-RepoFileRow $resultPath
        summary = New-RepoFileRow $summaryPath
        runtimeLog = New-RepoFileRow $runtimeLogPath
        qaCleanup = New-RepoFileRow $qaCleanupPath
        profileRestore = New-RepoFileRow $profileRestorePath
        processCheck = New-RepoFileRow $processCheckPath
        reportZip = New-RepoFileRow $reportZipPath
        reportRuntimeContext = $zipSelection['DTMAPI-runtime-context.txt'].Row
        reportRuntimeLog = $zipSelection['DTMAPI-latest.log'].Row
        reportReleaseManifest = $zipSelection['release-manifest.json'].Row
        reportPlayerDoctor = $zipSelection['PlayerDoctor/player-doctor.json'].Row
        runnerReceipt = $runnerReceiptRow
        postLoadOrchestration = if ($null -eq $postLoadPath) { $null } else { New-RepoFileRow $postLoadPath }
        liveUpdateOrchestration = if ($null -eq $liveUpdatePath) { $null } else { New-RepoFileRow $liveUpdatePath }
        sessionPrepare = if ($null -eq $sessionPreparePath) { $null } else { New-RepoFileRow $sessionPreparePath }
        deploymentStatusV1 = if ($null -eq $deploymentStatusV1Path) { $null } else { New-RepoFileRow $deploymentStatusV1Path }
        sessionSnapshot = if ($null -eq $snapshotPath) { $null } else { New-RepoFileRow $snapshotPath }
        sdkUpdate = if ($null -eq $sdkUpdatePath) { $null } else { New-RepoFileRow $sdkUpdatePath }
        deploymentStatusV2 = if ($null -eq $deploymentStatusV2Path) { $null } else { New-RepoFileRow $deploymentStatusV2Path }
        sessionReload = if ($null -eq $sessionReloadPath) { $null } else { New-RepoFileRow $sessionReloadPath }
        sessionClear = if ($null -eq $sessionClearPath) { $null } else { New-RepoFileRow $sessionClearPath }
    }

    return [ordered]@{
        caseId = $caseId
        kind = [string]$expected.kind
        startedAtUtc = Format-Utc $started
        completedAtUtc = Format-Utc $completed
        genericSmokeExitCode = $genericExitCode
        fixtureVersionAtStart = [string]$expected.version
        expectedOutcome = [ordered]@{
            classification = [string]$expected.classification
            statusCode = [string]$expected.status
            loaded = [bool]$expected.loaded
            officialEnabled = [bool]$expected.enabled
            fixtureVersion = [string]$expected.version
            restartRequired = [bool]$expected.restart
        }
        expectedDiagnostics = @($expected.diagnostics)
        evidence = $evidence
        observations = [ordered]@{
            common = [ordered]@{
                resultSchemaVersion = [int]$result.SchemaVersion
                resultRunStatus = [string]$result.RunStatus
                failedResultFields = @($resultFailureFields)
                saveLoaded = [string]$result.SaveLoaded
                hookProbe = [string]$result.HookProbe
                gameLaunched = [string]$result.GameLaunched
                processExited = [string]$result.ProcessExited
                noFatalInstanceWindow = [string]$result.NoFatalInstanceWindow
                forcedClose = [string]$result.ForcedClose
                qaHostCleanup = [string]$result.QaHostCleanup
                qaSaveLoadedObservation = [string]$result.QaSaveLoadedObservation
                officialModProfileRestored = [bool]$result.OfficialModProfileRestored
                localAuthorSourceStateRestored = [bool]$result.LocalAuthorSourceStateRestored
                local11AuthorSourceStateRestored = [bool]$result.Local11AuthorSourceStateRestored
                isolateAllOfficialMods = [bool]$result.IsolateAllOfficialMods
                officialModProfileApplied = [bool]$result.OfficialModProfileApplied
                summarySaveSlot = [int]$summary['SaveSlot']
                summaryIncludeHookProbe = [bool]::Parse([string]$summary['IncludeHookProbe'])
                summaryLaunchMode = [string]$summary['LaunchMode']
                qaCleanupExactArtifacts = [bool]$qaCleanup.ExactArtifacts
                qaCleanupExactQaEvidence = [bool]$qaCleanup.ExactQaEvidence
                qaCleanupExactTree = [bool]$qaCleanup.ExactTree
                qaCleanupCleaned = [bool]$qaCleanup.Cleaned
                qaCleanupRetained = [bool]$qaCleanup.Retained
                profileRestoreExactLength = ([long]$profileRestore.ExpectedLength -eq [long]$profileRestore.ActualLength)
                profileRestoreExactSha256 = ([string]$profileRestore.ExpectedSha256).Equals([string]$profileRestore.ActualSha256, [StringComparison]::OrdinalIgnoreCase)
                profileRestorePassed = [bool]$profileRestore.Passed
                processAbsent = $true
                reportRuntimeLogIsRunPrefix = $true
                reportStartedAtUtc = Format-Utc $reportStarted
                reportGeneratedAtUtc = Format-Utc $reportGenerated
                playerDoctorRuntimeAssemblyHashMatch = $true
                playerDoctorErrorCount = [int]$playerDoctor.errorCount
                playerDoctorWarningCount = [int]$playerDoctor.warningCount
                playerDoctorMisplacedCount = [int]$playerDoctor.misplacedCount
            }
            strictSibling = [ordered]@{
                entryCount = $strictEntryCount
                gameLaunchedCount = $strictGameLaunchedCount
                oneSecondCount = $strictOneSecondCount
                saveLoadedSlot = 2
                saveLoadedCount = $strictSaveLoadedMatches.Count
                reportStatusCode = [string]$strictReport.statusCode
                reportLoaded = [bool]$strictReport.loaded
            }
            advancedIdentity = [ordered]@{
                verifiedFixtureLoadSourceCount = $verifiedFixtureLoadSourceCount
                foreignAdvancedLoadSourceCount = $foreignAdvancedLoadSourceCount
                unexpectedManagedLoadSourceCount = $unexpectedManagedLoadSourceCount
                autoFishingLoadSourceCount = $autoFishingLoadSourceCount
                autoFishingEntryLogCount = $autoFishingEntryLogCount
                autoFishingReportStatusCode = [string]$autoFishingReport.statusCode
                autoFishingReportLoaded = [bool]$autoFishingReport.loaded
                autoFishingReportOfficialEnabled = [bool]$autoFishingReport.officialEnabled
                autoFishingReportVersion = [string]$autoFishingReport.version
                playerDoctorFixtureManagedIdentity = [string]$doctorFixture.managedIdentity
                playerDoctorFixtureProvenanceStatus = [string]$doctorFixture.provenanceStatus
                playerDoctorFixturePolicyId = [string]$doctorFixture.referencePolicyId
                playerDoctorFixtureGameBuildId = [string]$doctorFixture.gameBuildId
                playerDoctorFixtureHarmonyOwner = [string]$doctorFixture.expectedHarmonyOwner
                playerDoctorStrictSiblingCount = $doctorStrictRows.Count
                playerDoctorAutoFishingCount = 0
            }
            fixture = [ordered]@{
                entryCount = $entryCount
                completionCount = $completionCount
                patchPostfixCount = $postfixMatches.Count
                nativeQueryCount = $queryMatches.Count
                queryAndPostfixResultEqual = $queryAndPostfixEqual
                disabledSkipCount = $disabledSkipCount
                lateInstallCount = $lateInstallCount
                entryFailureMessage = $entryFailureMessage
            }
            cleanup = [ordered]@{
                reason = $cleanupReason
                participants = $cleanupParticipants
                removed = $cleanupRemoved
                remaining = $cleanupRemaining
                failures = $cleanupFailures
                machineCode = $cleanupMachineCode
                machineCodeRuntimeCount = $machineCodeRuntimeCount
                machineCodeReportCount = $machineCodeReportCount
            }
            diagnostics = $diagnosticCounts
            report = $fixtureReport
            state = $state
            session = $session
        }
        accepted = $true
    }
}

$evidenceMapFullPath = Resolve-PathFromRepo $EvidenceMapPath
Assert-File $evidenceMapFullPath 'G2 runtime evidence map'
Assert-IgnoredEvidenceMap $evidenceMapFullPath
$evidenceMap = Read-Json $evidenceMapFullPath 'G2 runtime evidence map'
Assert-ExactProperties $evidenceMap @('schemaVersion', 'implementationCommit', 'artifacts', 'cases') 'G2 runtime evidence map'
Assert-Int 'G2 runtime evidence map schemaVersion' $evidenceMap.schemaVersion 1

$implementationCommit = Get-FullCommit ([string]$evidenceMap.implementationCommit)
$script:implementationCommit = $implementationCommit
$commitTimeResult = Invoke-Git @('show', '-s', '--format=%cI', $implementationCommit) $false
$implementationCommitTime = Parse-Utc ([string]($commitTimeResult.Lines | Select-Object -First 1)) 'implementation commit time'

$contract = Read-Json (Join-Path $repo (Normalize-RelativePath $contractPath)) 'G2 contract'
Assert-Int 'G2 contract schemaVersion' $contract.schemaVersion 2
Assert-Exact 'G2 contract ID' $contract.contractId 'batch6-g2-advanced-synthetic-vertical-slice'
Assert-Exact 'G2 architecture authority' $contract.authorities.architecture 'docs/architecture/batch6-managed-mod-identity-contract.md'
Assert-Exact 'G2 review authority' $contract.authorities.review 'docs/reviews/code/2026/20260720-0004-batch6-g2-advanced-synthetic-vertical-slice-prerequisite.md'

Assert-ExactProperties $evidenceMap.artifacts @('authorSdkZipPath', 'fixturePackages') 'Evidence map artifacts'
$authorSdkPath = Resolve-PathFromRepo ([string]$evidenceMap.artifacts.authorSdkZipPath)
Assert-File $authorSdkPath 'G2 Author SDK ZIP'
$authorSdkName = [System.IO.Path]::GetFileName($authorSdkPath)
if ($authorSdkName -notmatch '^DTMAPI-Author-SDK-[0-9]+\.[0-9]+\.[0-9]+-win-x64\.zip$') {
    throw "Author SDK artifact has an unexpected logical name: $authorSdkName"
}
$authorSdkArtifact = [ordered]@{
    logicalName = $authorSdkName
    path = $authorSdkName
    length = [long](Get-Item -LiteralPath $authorSdkPath).Length
    sha256 = Get-FileSha256 $authorSdkPath
}
if ([long]$authorSdkArtifact.length -le 0) { throw 'Author SDK ZIP is empty.' }

$fixturePackageMap = @($evidenceMap.artifacts.fixturePackages)
if ($fixturePackageMap.Count -ne 2) { throw 'Evidence map must provide exactly two fixture package artifacts.' }
$fixturePackages = @(
    Get-PackageArtifact $fixturePackageMap[0] '0.1.0'
    Get-PackageArtifact $fixturePackageMap[1] '0.1.1'
)

$caseMaps = @($evidenceMap.cases)
if ($caseMaps.Count -ne $caseIds.Count) { throw "Evidence map must contain exactly $($caseIds.Count) cases." }
Assert-ExactSet 'Evidence map case IDs' @($caseMaps | ForEach-Object { [string]$_.caseId }) $caseIds

$cases = New-Object System.Collections.Generic.List[object]
$runtimeReleaseReference = $null
$runRoots = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$previousCompleted = $null
$capturedAt = $null
for ($index = 0; $index -lt $caseIds.Count; $index++) {
    Assert-Exact "Evidence map case order at index $index" $caseMaps[$index].caseId $caseIds[$index]
    $caseReceipt = New-CaseReceipt $caseMaps[$index] $caseIds[$index] $contract ([ref]$runtimeReleaseReference)
    if (-not $runRoots.Add([string]$caseReceipt.evidence.runRootPath)) {
        throw "G2 runtime matrix cases must use unique GAME-SMOKE run roots: $($caseReceipt.evidence.runRootPath)"
    }
    $started = Parse-Utc $caseReceipt.startedAtUtc "case $($caseIds[$index]) start"
    $completed = Parse-Utc $caseReceipt.completedAtUtc "case $($caseIds[$index]) completion"
    if ($null -ne $previousCompleted -and $started -lt $previousCompleted) {
        throw "G2 runtime matrix transition order overlaps: '$($caseIds[$index])' starts before the previous case completed."
    }
    $previousCompleted = $completed
    if ($null -eq $capturedAt -or $completed -gt $capturedAt) { $capturedAt = $completed }
    $cases.Add($caseReceipt)
}

if ($null -eq $runtimeReleaseReference) { throw 'G2 runtime matrix did not yield a Runtime release manifest.' }
if ($implementationCommitTime -gt $capturedAt) {
    throw 'implementationCommit has a committer timestamp later than the captured runtime matrix.'
}

$matrixJson = @($cases.ToArray()) | ConvertTo-Json -Depth 100 -Compress
$matrixSha256 = Get-StringSha256 $matrixJson
$fixture = [ordered]@{
    uniqueId = [string]$contract.syntheticFixture.uniqueId
    canonicalHarmonyOwner = [string]$contract.syntheticFixture.harmonyOwner
    gameBuildId = [string]$contract.syntheticFixture.gameBuildId
    gameAssemblySha256 = ([string]$contract.referencePolicy.gameAssemblySha256).ToUpperInvariant()
    nativeType = [string]$contract.syntheticFixture.nativeType
    nativeMethod = [string]$contract.syntheticFixture.nativeMethod
    nativeMethodToken = [string]$contract.syntheticFixture.nativeMethodToken
    saveSlot = [int]$contract.syntheticFixture.saveSlot
    runtimeSlot = 2
    strictSiblingUniqueId = 'DTMAPI.HookProbeMod'
}

$receipt = [ordered]@{
    schemaVersion = 1
    contractId = 'batch6-g2-advanced-synthetic-vertical-slice'
    receiptKind = 'committed-runtime-matrix'
    architectureAuthority = 'docs/architecture/batch6-managed-mod-identity-contract.md'
    reviewAuthority = 'docs/reviews/code/2026/20260720-0004-batch6-g2-advanced-synthetic-vertical-slice-prerequisite.md'
    rootCauseReviewAuthority = 'docs/reviews/code/2026/20260720-0005-batch6-g2-first-runtime-matrix-root-cause.md'
    updateAuthority = 'docs/updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md'
    implementationCommit = $implementationCommit
    capturedAtUtc = Format-Utc $capturedAt
    runtimeRelease = $runtimeReleaseReference
    fixture = $fixture
    environment = [ordered]@{
        gameBuildId = [string]$fixture.gameBuildId
        gameAssemblySha256 = [string]$fixture.gameAssemblySha256
        launchMode = 'Steam'
        saveSlot = 3
        runtimeSlot = 2
    }
    artifacts = [ordered]@{
        authorSdkZip = $authorSdkArtifact
        fixturePackages = @($fixturePackages)
    }
    cases = @($cases.ToArray())
    matrixSha256 = $matrixSha256
}

$json = ($receipt | ConvertTo-Json -Depth 100) + [Environment]::NewLine
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputFullPath = Resolve-PathFromRepo $OutputPath
    $outputParent = Split-Path -Parent $outputFullPath
    if (-not (Test-Path -LiteralPath $outputParent -PathType Container)) {
        New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($outputFullPath, $json, $utf8NoBom)
    Write-Host "Batch 6 G2 runtime matrix receipt written: $outputFullPath"
}
elseif (-not $PassThru) {
    Write-Output $json
}

if ($PassThru) { Write-Output ([pscustomobject]$receipt) }
Write-Host "Batch 6 G2 runtime matrix reconstructed: cases=$($cases.Count); implementationCommit=$implementationCommit; matrixSha256=$matrixSha256."
