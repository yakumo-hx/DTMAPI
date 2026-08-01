[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $PackageTag = '20260720'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PortablePackageFullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $pathRoot = [System.IO.Path]::GetPathRoot($fullPath)
    if ($fullPath.Length -gt $pathRoot.Length) {
        return $fullPath.TrimEnd('\', '/')
    }
    return $fullPath
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ($PackageTag -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') {
    throw "PackageTag must be a safe file-name slug: '$PackageTag'."
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'dist'
}
$OutputDirectory = Get-PortablePackageFullPath -Path $OutputDirectory

$packageName = "DTMAPI-DolocTown-Full-Reverse-Capture-$PackageTag"
$stagingRoot = [System.IO.Path]::GetFullPath((Join-Path $OutputDirectory $packageName))
$zipPath = [System.IO.Path]::GetFullPath((Join-Path $OutputDirectory "$packageName.zip"))
$zipHashPath = "$zipPath.sha256.txt"

foreach ($directChild in @($stagingRoot, $zipPath, $zipHashPath)) {
    $parent = Get-PortablePackageFullPath -Path (Split-Path -Parent $directChild)
    if (-not $parent.Equals($OutputDirectory, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Package output escaped its direct OutputDirectory boundary: $directChild"
    }
}

foreach ($path in @($stagingRoot, $zipPath, $zipHashPath)) {
    if (Test-Path -LiteralPath $path) {
        throw "Package output already exists; refusing to overwrite it: $path"
    }
}

$portableRoot = Join-Path $repoRoot 'tools\portable-reverse-capture'
$packageFiles = @(
    [ordered]@{ source = (Join-Path $repoRoot 'tools\scripts\capture-doloctown-reverse-baseline.ps1'); destination = 'capture-doloctown-reverse-baseline.ps1' },
    [ordered]@{ source = (Join-Path $repoRoot 'tools\scripts\steam-appmanifest-identity.ps1'); destination = 'steam-appmanifest-identity.ps1' },
    [ordered]@{ source = (Join-Path $portableRoot 'run-doloctown-full-capture.ps1'); destination = 'run-doloctown-full-capture.ps1' },
    [ordered]@{ source = (Join-Path $portableRoot 'common.ps1'); destination = 'common.ps1' },
    [ordered]@{ source = (Join-Path $portableRoot 'reverse-baseline-path-safety.ps1'); destination = 'reverse-baseline-path-safety.ps1' },
    [ordered]@{ source = (Join-Path $portableRoot '1_RUN_FULL_UNPACK.bat'); destination = '1_RUN_FULL_UNPACK.bat' },
    [ordered]@{ source = (Join-Path $portableRoot 'README.zh-CN.md'); destination = 'README.zh-CN.md' },
    [ordered]@{ source = (Join-Path $portableRoot 'THIRD-PARTY-TOOLS.txt'); destination = 'THIRD-PARTY-TOOLS.txt' }
)

$builderRelativePath = 'tools/scripts/build-portable-reverse-capture-package.ps1'
$sourceRelativePaths = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $packageFiles) {
    $sourceRelativePaths.Add(
        $entry.source.Substring($repoRoot.TrimEnd('\', '/').Length + 1).Replace('\', '/')
    )
}
$sourceRelativePaths.Add($builderRelativePath)

foreach ($relativePath in $sourceRelativePaths) {
    & git -C $repoRoot ls-files --error-unmatch -- $relativePath 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Package source input is not tracked by Git: $relativePath"
    }
}

$statusArguments = @(
    '-C',
    $repoRoot,
    'status',
    '--porcelain=v1',
    '--untracked-files=all',
    '--'
) + @($sourceRelativePaths)
$scopedStatus = @(& git @statusArguments)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to verify package source inputs with Git.'
}
if ($scopedStatus.Count -gt 0) {
    throw "Package source inputs do not exactly match HEAD:`n$($scopedStatus -join [Environment]::NewLine)"
}

$sourceCommit = (& git -C $repoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '^[0-9a-fA-F]{40}$') {
    throw 'Unable to resolve an exact source commit for the package.'
}

[System.IO.Directory]::CreateDirectory($stagingRoot) | Out-Null

foreach ($entry in $packageFiles) {
    if (-not (Test-Path -LiteralPath $entry.source -PathType Leaf)) {
        throw "Required package source file is missing: $($entry.source)"
    }
    Copy-Item -LiteralPath $entry.source -Destination (Join-Path $stagingRoot $entry.destination)
}

$endSourceCommit = (& git -C $repoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or -not $sourceCommit.Equals($endSourceCommit, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Repository HEAD changed while the portable package was being staged. Start=$sourceCommit End=$endSourceCommit"
}
$scopedStatusAfterCopy = @(& git @statusArguments)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to reverify package source inputs with Git after staging.'
}
if ($scopedStatusAfterCopy.Count -gt 0) {
    throw "Package source inputs changed while the package was being staged:`n$($scopedStatusAfterCopy -join [Environment]::NewLine)"
}
foreach ($entry in $packageFiles) {
    $stagedPath = Join-Path $stagingRoot $entry.destination
    $sourceHash = (Get-FileHash -LiteralPath $entry.source -Algorithm SHA256).Hash
    $stagedHash = (Get-FileHash -LiteralPath $stagedPath -Algorithm SHA256).Hash
    if (-not $sourceHash.Equals($stagedHash, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Staged package payload does not exactly match its source input: $($entry.destination)"
    }
}

$manifestFiles = @(Get-ChildItem -LiteralPath $stagingRoot -File | Sort-Object Name | ForEach-Object {
    [ordered]@{
        path = $_.Name
        bytes = [long]$_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    }
})

$manifest = [ordered]@{
    format = 'dtmapi.portable-reverse-capture-package/v2'
    packageName = $packageName
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    sourceCommit = $sourceCommit
    source = [ordered]@{
        commit = $sourceCommit
        state = 'exact-tracked-inputs'
        builderPath = $builderRelativePath
        inputPaths = @($sourceRelativePaths)
    }
    sourceEngine = 'tools/scripts/capture-doloctown-reverse-baseline.ps1'
    thirdPartyExecutablesBundled = $false
    officialGameContentBundled = $false
    manifestSelfHashExcluded = $true
    files = $manifestFiles
}
$manifestPath = Join-Path $stagingRoot 'PACKAGE-MANIFEST.json'
[System.IO.File]::WriteAllText(
    $manifestPath,
    (($manifest | ConvertTo-Json -Depth 8) + [Environment]::NewLine),
    [System.Text.UTF8Encoding]::new($false)
)

$forbiddenPayloads = @(Get-ChildItem -LiteralPath $stagingRoot -File -Recurse | Where-Object {
    $_.Extension -in @('.dll', '.exe', '.pdb', '.assets', '.bundle')
})
if ($forbiddenPayloads.Count -gt 0) {
    throw "Portable script package unexpectedly contains binary/game payloads: $($forbiddenPayloads.FullName -join ', ')"
}

Compress-Archive -LiteralPath $stagingRoot -DestinationPath $zipPath -CompressionLevel Optimal
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
[System.IO.File]::WriteAllText(
    $zipHashPath,
    "$zipHash  $([System.IO.Path]::GetFileName($zipPath))$([Environment]::NewLine)",
    [System.Text.UTF8Encoding]::new($false)
)

[pscustomobject]@{
    Package = $zipPath
    Sha256 = $zipHash
    Sha256File = $zipHashPath
    StagingRoot = $stagingRoot
    Bytes = [long](Get-Item -LiteralPath $zipPath).Length
}
