[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $PolicyId,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputRoot,
    [string] $ReverseBuildsRoot = '',
    [string] $BepInExArchive = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$repoTempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))
$policyRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'author-sdk\advanced-reference-policies'))
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Resolve-AdvancedFixtureChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath) -or [System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "$Label must be one non-rooted relative path: '$RelativePath'."
    }
    $normalized = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $resolved = [System.IO.Path]::GetFullPath((Join-Path $Root $normalized))
    if (-not (Test-DtmApiPathIsSameOrChild -Child $resolved -Parent $Root) -or
        $resolved.TrimEnd([char[]]@('\', '/')) -eq ([System.IO.Path]::GetFullPath($Root)).TrimEnd([char[]]@('\', '/'))) {
        throw "$Label escaped its root: '$RelativePath'."
    }
    return $resolved
}

function Get-AdvancedFixtureBytesSha256 {
    param([Parameter(Mandatory = $true)] [byte[]] $Bytes)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha256.ComputeHash($Bytes))).Replace('-', '').ToUpperInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Assert-AdvancedFixtureFileIdentity {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [long] $ExpectedLength,
        [Parameter(Mandatory = $true)] [string] $ExpectedSha256,
        [Parameter(Mandatory = $true)] [string] $ExpectedAssemblyName,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label is missing: $Path"
    }
    $item = Get-Item -LiteralPath $Path
    if ([long]$item.Length -ne $ExpectedLength) {
        throw "$Label length mismatch: expected $ExpectedLength, actual $($item.Length)."
    }
    $actualSha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualSha256 -cne $ExpectedSha256.ToUpperInvariant()) {
        throw "$Label SHA-256 mismatch: expected $ExpectedSha256, actual $actualSha256."
    }
    try {
        $actualAssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($Path).Name
    }
    catch {
        throw "$Label is not the declared managed assembly '$ExpectedAssemblyName': $($_.Exception.Message)"
    }
    if ([string]$actualAssemblyName -cne $ExpectedAssemblyName) {
        throw "$Label assembly identity mismatch: expected '$ExpectedAssemblyName', actual '$actualAssemblyName'."
    }
}

if ($PolicyId -notmatch '^[a-z0-9][a-z0-9.-]{0,127}$') {
    throw "Advanced reference policy id is unsafe: '$PolicyId'."
}
$policyPath = Resolve-AdvancedFixtureChildPath `
    -Root $policyRoot `
    -RelativePath ($PolicyId + '.json') `
    -Label 'Advanced reference policy path'
if (-not (Test-Path -LiteralPath $policyPath -PathType Leaf)) {
    throw "Tracked Advanced reference policy is missing: $policyPath"
}
$policy = Get-Content -Raw -Encoding UTF8 -LiteralPath $policyPath | ConvertFrom-Json
if ([int]$policy.schemaVersion -ne 1 -or [string]$policy.policyId -cne $PolicyId) {
    throw "Tracked Advanced reference policy identity/schema mismatch for '$PolicyId'."
}
$gameBuildId = [string]$policy.gameBuildId
$gameAssemblyRelativePath = [string]$policy.gameAssemblyRelativePath
$gameAssemblySha256 = [string]$policy.gameAssemblySha256
if ($gameBuildId -notmatch '^[0-9]+$' -or
    [string]::IsNullOrWhiteSpace($gameAssemblyRelativePath) -or
    $gameAssemblySha256 -notmatch '^[A-Fa-f0-9]{64}$') {
    throw "Tracked Advanced reference policy '$PolicyId' has invalid game identity fields."
}

$references = @($policy.references)
if ($references.Count -eq 0) {
    throw "Tracked Advanced reference policy '$PolicyId' has no references."
}
$seenAssemblyNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
$seenRelativePaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($reference in $references) {
    $assemblyName = [string]$reference.assemblyName
    $relativePath = [string]$reference.gameRelativePath
    $sha256 = [string]$reference.sha256
    if ([string]::IsNullOrWhiteSpace($assemblyName) -or
        [string]::IsNullOrWhiteSpace($relativePath) -or
        [long]$reference.length -le 0 -or
        $sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
        [bool]$reference.copyLocal) {
        throw "Tracked Advanced reference policy '$PolicyId' has an invalid reference entry."
    }
    Resolve-AdvancedFixtureChildPath -Root $repoTempRoot -RelativePath $relativePath -Label 'Advanced reference relative path' | Out-Null
    if (-not $seenAssemblyNames.Add($assemblyName) -or -not $seenRelativePaths.Add($relativePath)) {
        throw "Tracked Advanced reference policy '$PolicyId' has duplicate reference identity/path entries."
    }
}

$assemblyReferences = @($references | Where-Object { [string]$_.assemblyName -ceq 'Assembly-CSharp' })
if ($assemblyReferences.Count -ne 1) {
    throw "Tracked Advanced reference policy '$PolicyId' must declare exactly one Assembly-CSharp reference; found $($assemblyReferences.Count)."
}
$assemblyReference = $assemblyReferences[0]
if ([string]$assemblyReference.gameRelativePath -cne $gameAssemblyRelativePath -or
    [string]$assemblyReference.sha256 -cne $gameAssemblySha256) {
    throw "Tracked Advanced reference policy '$PolicyId' game assembly fields disagree with its Assembly-CSharp reference."
}

$resolvedOutputRoot = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    [System.IO.Path]::GetFullPath($OutputRoot)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo $OutputRoot))
}
if (-not (Test-DtmApiPathIsSameOrChild -Child $resolvedOutputRoot -Parent $repoTempRoot) -or
    $resolvedOutputRoot.TrimEnd([char[]]@('\', '/')) -eq $repoTempRoot.TrimEnd([char[]]@('\', '/'))) {
    throw "Advanced reference fixture output must be a dedicated child of the repository temp root: $repoTempRoot"
}
if (Test-Path -LiteralPath $resolvedOutputRoot) {
    throw "Advanced reference fixture output already exists: $resolvedOutputRoot"
}

$resolvedReverseBuildsRoot = if ([string]::IsNullOrWhiteSpace($ReverseBuildsRoot)) {
    [System.IO.Path]::GetFullPath((Join-Path $repo 'references\doloc-town\reverse\builds'))
}
elseif ([System.IO.Path]::IsPathRooted($ReverseBuildsRoot)) {
    [System.IO.Path]::GetFullPath($ReverseBuildsRoot)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo $ReverseBuildsRoot))
}
if (-not (Test-Path -LiteralPath $resolvedReverseBuildsRoot -PathType Container)) {
    throw "Advanced reference reverse-build root is missing: $resolvedReverseBuildsRoot"
}

$assemblyCandidates = New-Object 'System.Collections.Generic.List[string]'
foreach ($buildDirectory in @(Get-ChildItem -LiteralPath $resolvedReverseBuildsRoot -Directory -Force)) {
    $rawGameRoot = Join-Path $buildDirectory.FullName 'raw-snapshot\game'
    $candidate = Resolve-AdvancedFixtureChildPath `
        -Root $rawGameRoot `
        -RelativePath $gameAssemblyRelativePath `
        -Label 'Reverse Assembly-CSharp candidate path'
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        continue
    }
    $candidateItem = Get-Item -LiteralPath $candidate
    if ([long]$candidateItem.Length -ne [long]$assemblyReference.length) {
        continue
    }
    $candidateSha256 = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($candidateSha256 -ceq ([string]$assemblyReference.sha256).ToUpperInvariant()) {
        $assemblyCandidates.Add([System.IO.Path]::GetFullPath($candidate)) | Out-Null
    }
}
if ($assemblyCandidates.Count -ne 1) {
    throw ("Advanced reference policy '{0}' requires exactly one local reverse Assembly-CSharp match for build {1}, length {2}, SHA-256 {3}; found {4} under {5}." -f `
        $PolicyId,
        $gameBuildId,
        [long]$assemblyReference.length,
        ([string]$assemblyReference.sha256).ToUpperInvariant(),
        $assemblyCandidates.Count,
        $resolvedReverseBuildsRoot)
}
$assemblySource = $assemblyCandidates[0]
Assert-AdvancedFixtureFileIdentity `
    -Path $assemblySource `
    -ExpectedLength ([long]$assemblyReference.length) `
    -ExpectedSha256 ([string]$assemblyReference.sha256) `
    -ExpectedAssemblyName 'Assembly-CSharp' `
    -Label "Advanced reference policy '$PolicyId' reverse Assembly-CSharp"

$trackedBepInExArchive = [System.IO.Path]::GetFullPath((Join-Path $repo 'tools\release\bootstrap\BepInEx_win_x64_5.4.23.5.zip'))
$resolvedBepInExArchive = if ([string]::IsNullOrWhiteSpace($BepInExArchive)) {
    $trackedBepInExArchive
}
elseif ([System.IO.Path]::IsPathRooted($BepInExArchive)) {
    [System.IO.Path]::GetFullPath($BepInExArchive)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo $BepInExArchive))
}

$referenceSources = @{}
$harmonyReferences = @($references | Where-Object { [string]$_.assemblyName -ceq '0Harmony' })
if ($harmonyReferences.Count -gt 0) {
    if (-not (Test-Path -LiteralPath $resolvedBepInExArchive -PathType Leaf)) {
        throw "Tracked BepInEx archive is missing for Advanced reference fixture: $resolvedBepInExArchive"
    }
    if (-not (Test-Path -LiteralPath $trackedBepInExArchive -PathType Leaf)) {
        throw "Repository-tracked BepInEx archive authority is missing: $trackedBepInExArchive"
    }
    $trackedArchiveSha256 = (Get-FileHash -LiteralPath $trackedBepInExArchive -Algorithm SHA256).Hash.ToUpperInvariant()
    $resolvedArchiveSha256 = (Get-FileHash -LiteralPath $resolvedBepInExArchive -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($resolvedArchiveSha256 -cne $trackedArchiveSha256) {
        throw "Advanced reference fixture BepInEx archive does not match the repository-tracked archive identity."
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedBepInExArchive)
    try {
        foreach ($reference in $harmonyReferences) {
            $entryName = ([string]$reference.gameRelativePath).Replace('\', '/')
            $entries = @($archive.Entries | Where-Object { [string]$_.FullName -ceq $entryName })
            if ($entries.Count -ne 1) {
                throw "Tracked BepInEx archive must contain exactly one '$entryName' entry; found $($entries.Count)."
            }
            $entry = $entries[0]
            $stream = $entry.Open()
            try {
                $memory = New-Object System.IO.MemoryStream
                try {
                    $stream.CopyTo($memory)
                    [byte[]]$bytes = $memory.ToArray()
                }
                finally {
                    $memory.Dispose()
                }
            }
            finally {
                $stream.Dispose()
            }
            $actualSha256 = Get-AdvancedFixtureBytesSha256 -Bytes $bytes
            if ([long]$bytes.LongLength -ne [long]$reference.length -or
                $actualSha256 -cne ([string]$reference.sha256).ToUpperInvariant()) {
                throw "Tracked BepInEx archive entry '$entryName' does not match policy '$PolicyId'."
            }
            $referenceSources[[string]$reference.gameRelativePath] = [pscustomobject]@{
                Kind = 'Bytes'
                Bytes = $bytes
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

foreach ($reference in $references) {
    $assemblyName = [string]$reference.assemblyName
    $relativePath = [string]$reference.gameRelativePath
    if ($assemblyName -ceq 'Assembly-CSharp') {
        $referenceSources[$relativePath] = [pscustomobject]@{
            Kind = 'File'
            Path = $assemblySource
        }
    }
    elseif ($assemblyName -cne '0Harmony') {
        throw "Advanced reference fixture does not have a reviewed source for assembly '$assemblyName' in policy '$PolicyId'."
    }
}

$outputParent = Split-Path -Parent $resolvedOutputRoot
[System.IO.Directory]::CreateDirectory($outputParent) | Out-Null
$stagingRoot = Join-Path $outputParent ('.' + [System.IO.Path]::GetFileName($resolvedOutputRoot) + '.staging-' + [Guid]::NewGuid().ToString('N'))
if (-not (Test-DtmApiPathIsSameOrChild -Child $stagingRoot -Parent $repoTempRoot)) {
    throw "Advanced reference fixture staging path escaped the repository temp root: $stagingRoot"
}

try {
    $gameRoot = Join-Path $stagingRoot 'steamapps\common\Doloc Town'
    [System.IO.Directory]::CreateDirectory($gameRoot) | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $gameRoot 'DolocTown.exe'),
        'Synthetic marker for exact Advanced release-contract reference validation only.',
        $utf8)

    foreach ($reference in $references) {
        $relativePath = [string]$reference.gameRelativePath
        $destination = Resolve-AdvancedFixtureChildPath `
            -Root $gameRoot `
            -RelativePath $relativePath `
            -Label 'Advanced reference fixture destination'
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $destination)) | Out-Null
        $source = $referenceSources[$relativePath]
        if ($null -eq $source) {
            throw "Advanced reference fixture source resolution failed for '$relativePath'."
        }
        if ([string]$source.Kind -ceq 'File') {
            [System.IO.File]::Copy([string]$source.Path, $destination, $false)
        }
        elseif ([string]$source.Kind -ceq 'Bytes') {
            [System.IO.File]::WriteAllBytes($destination, [byte[]]$source.Bytes)
        }
        else {
            throw "Advanced reference fixture source kind is invalid for '$relativePath'."
        }
        Assert-AdvancedFixtureFileIdentity `
            -Path $destination `
            -ExpectedLength ([long]$reference.length) `
            -ExpectedSha256 ([string]$reference.sha256) `
            -ExpectedAssemblyName ([string]$reference.assemblyName) `
            -Label "Staged Advanced reference '$relativePath'"
    }

    $manifestPath = Join-Path $stagingRoot 'steamapps\appmanifest_2285550.acf'
    $manifestText = "`"AppState`"`n{`n`t`"appid`"`t`t`"2285550`"`n`t`"buildid`"`t`t`"$gameBuildId`"`n}`n"
    [System.IO.File]::WriteAllText($manifestPath, $manifestText, $utf8)

    Move-Item -LiteralPath $stagingRoot -Destination $resolvedOutputRoot
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

$finalGameRoot = Join-Path $resolvedOutputRoot 'steamapps\common\Doloc Town'
foreach ($reference in $references) {
    $destination = Resolve-AdvancedFixtureChildPath `
        -Root $finalGameRoot `
        -RelativePath ([string]$reference.gameRelativePath) `
        -Label 'Final Advanced reference fixture destination'
    Assert-AdvancedFixtureFileIdentity `
        -Path $destination `
        -ExpectedLength ([long]$reference.length) `
        -ExpectedSha256 ([string]$reference.sha256) `
        -ExpectedAssemblyName ([string]$reference.assemblyName) `
        -Label "Final Advanced reference '$([string]$reference.gameRelativePath)'"
}

Write-Host "Advanced reference game fixture ($PolicyId / build $gameBuildId): PASS"
Write-Host "Game root: $finalGameRoot"
