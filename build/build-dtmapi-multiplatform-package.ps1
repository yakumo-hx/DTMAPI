[CmdletBinding()]
param(
    [string] $AcceptedPackageRoot = '',
    [ValidateSet('Candidate')] [string] $SourceKind = 'Candidate',
    [string] $HostBuildReceiptPath = '',
    [string] $WindowsHostPath = '',
    [string] $LinuxHostPath = '',
    [string] $OutputRoot = '',
    [string] $MetadataRoot = '',
    [string] $AssetRoot = $env:DTMAPI_BRANDING_ASSET_ROOT,
    [string] $IconPath = '',
    [string] $PreviewPath = '',
    [string] $InstallerBuildCommit = '',
    [string] $ProductsPath = '',
    [string] $ProductSourceRoot = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/multiplatform-package-common.ps1"
. "$PSScriptRoot/windows-installer/release-common.ps1" -ProductsPath $ProductsPath -ProductSourceRoot $ProductSourceRoot

function Resolve-DtmApiMultiPlatformInputFile {
    param(
        [string] $ExplicitPath,
        [Parameter(Mandatory = $true)] [string] $DefaultPath,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $candidate = if ([string]::IsNullOrWhiteSpace($ExplicitPath)) { $DefaultPath } else { $ExplicitPath }
    $resolved = [System.IO.Path]::GetFullPath($candidate)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "$Label is missing: $resolved"
    }
    $item = Get-Item -LiteralPath $resolved -Force -ErrorAction Stop
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must not be a reparse point: $resolved"
    }
    return $resolved
}

function Assert-DtmApiMultiPlatformSafeOutputLeaf {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    $root = [System.IO.Path]::GetPathRoot($resolved).TrimEnd([char]92, [char]47)
    $leaf = [System.IO.Path]::GetFileName($resolved)
    if ([string]::IsNullOrWhiteSpace($leaf) -or
        $leaf -in @('.', '..') -or
        [string]::Equals($resolved, $root, [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($leaf, 'MODS', [System.StringComparison]::OrdinalIgnoreCase) -or
        $leaf.TrimEnd([char]32, [char]46) -cne $leaf -or
        $leaf.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
        throw "OutputRoot must be one safe package leaf and must never be the MODS root: $resolved"
    }

    $parent = [System.IO.Path]::GetFullPath((Split-Path -Parent $resolved)).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    $parentItem = Get-Item -LiteralPath $parent -Force -ErrorAction Stop
    if (($parentItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "OutputRoot parent must not be a reparse point: $parent"
    }
    if (Test-Path -LiteralPath $resolved) {
        if (-not (Test-Path -LiteralPath $resolved -PathType Container)) {
            throw "OutputRoot exists but is not a directory: $resolved"
        }
        $null = Assert-DtmApiMultiPlatformOrdinaryTree -Path $resolved -Context 'Existing multi-platform package output'
    }

    return [pscustomobject][ordered]@{
        Root = $resolved
        Parent = $parent
        Leaf = $leaf
    }
}

function Remove-DtmApiMultiPlatformTransient {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Parent,
        [Parameter(Mandatory = $true)] [string] $ExpectedLeafPrefix
    )

    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    if ((Split-Path -Parent $resolved) -cne $Parent -or
        -not [System.IO.Path]::GetFileName($resolved).StartsWith($ExpectedLeafPrefix, [System.StringComparison]::Ordinal)) {
        throw "Multi-platform package cleanup escaped its direct sibling boundary: $resolved"
    }
    if (Test-Path -LiteralPath $resolved) {
        $null = Assert-DtmApiMultiPlatformOrdinaryTree -Path $resolved -Context 'Multi-platform package transient cleanup'
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

$repo = Get-DtmApiMultiPlatformRepoRoot
$releaseProducts = @(Get-DtmApiReleaseProducts)
$product = $releaseProducts | Where-Object {
    [IO.Path]::GetFullPath((Join-Path $script:DtmApiProductSourceRoot $_.sourceRoot)) -eq (Join-Path $repo 'build/dtmapi-multiplatform')
} | Select-Object -First 1
if ($null -eq $product) { throw 'The multi-platform Runtime package is missing from products.json.' }
if ([string]::IsNullOrWhiteSpace($AcceptedPackageRoot)) { throw 'Pass the current Windows Runtime package with -AcceptedPackageRoot.' }
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path (Join-Path $repo 'dist') $product.uploadFolder
}
if (Test-Path -LiteralPath $OutputRoot) { throw 'Candidate output must be a new independent directory.' }
if ([string]::IsNullOrWhiteSpace($MetadataRoot)) {
    $MetadataRoot = Join-Path $repo 'build\dtmapi-multiplatform'
}
if ([string]::IsNullOrWhiteSpace($IconPath) -or [string]::IsNullOrWhiteSpace($PreviewPath)) {
    if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
        throw 'Pass -AssetRoot or set DTMAPI_BRANDING_ASSET_ROOT to the private branding directory.'
    }
    $AssetRoot = [System.IO.Path]::GetFullPath($AssetRoot)
    if (-not (Test-Path -LiteralPath $AssetRoot -PathType Container)) {
        throw "Private branding directory missing: $AssetRoot"
    }
}
if ([string]::IsNullOrWhiteSpace($IconPath)) {
    $IconPath = Join-Path $AssetRoot 'dtmapi-multiplatform-icon.png'
}
if ([string]::IsNullOrWhiteSpace($PreviewPath)) {
    $PreviewPath = Join-Path $AssetRoot 'dtmapi-multiplatform-preview.png'
}

$AcceptedPackageRoot = [System.IO.Path]::GetFullPath($AcceptedPackageRoot)
$MetadataRoot = [System.IO.Path]::GetFullPath($MetadataRoot)
$accepted = Assert-DtmApiMultiPlatformAcceptedRuntimePackage -PackageRoot $AcceptedPackageRoot -SourceKind $SourceKind
$output = Assert-DtmApiMultiPlatformSafeOutputLeaf -Path $OutputRoot
$metadata = Assert-DtmApiMultiPlatformOrdinaryTree -Path $MetadataRoot -Context 'Multi-platform package metadata root'

$hostBuild = Get-Content -LiteralPath $HostBuildReceiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
$hostBuildRoot = Split-Path -Parent ([IO.Path]::GetFullPath($HostBuildReceiptPath))
$windowsDefault = Join-Path $hostBuildRoot ([string]@($hostBuild.Hosts | Where-Object Rid -CEQ 'win-x64')[0].RelativePath)
$linuxDefault = Join-Path $hostBuildRoot ([string]@($hostBuild.Hosts | Where-Object Rid -CEQ 'linux-x64')[0].RelativePath)
$windowsHost = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $WindowsHostPath -DefaultPath $windowsDefault -Label 'win-x64 installer publish output'
$linuxHost = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $LinuxHostPath -DefaultPath $linuxDefault -Label 'linux-x64 installer publish output'
$icon = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $IconPath -DefaultPath $IconPath -Label 'DTMAPI icon asset'
$preview = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $PreviewPath -DefaultPath $PreviewPath -Label 'DTMAPI preview asset'
Assert-DtmApiMultiPlatformPeX64 -Path $windowsHost
Assert-DtmApiMultiPlatformElfX64 -Path $linuxHost

$infoFile = Join-Path $metadata 'info.json'
$readmeFile = Join-Path $metadata 'README.txt'
$windowsBatNames = [DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::WindowsEntries
foreach ($required in @($infoFile, $readmeFile) + @($windowsBatNames | ForEach-Object { Join-Path $metadata $_ })) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Multi-platform package metadata file is missing: $required"
    }
}
$info = Get-Content -LiteralPath $infoFile -Raw -Encoding UTF8 | ConvertFrom-Json
Assert-DtmApiMultiPlatformHostBuildReceipt -Receipt $hostBuild -WindowsHost $windowsHost -LinuxHost $linuxHost
if ($InstallerBuildCommit -and $InstallerBuildCommit -cne $hostBuild.Commit) { throw 'Installer commit conflicts with actual build.' }
$InstallerBuildCommit = $hostBuild.Commit

$preservedWorkshopControl = $null
if (Test-Path -LiteralPath $output.Root -PathType Container) {
    $workshopControlFiles = @(Get-ChildItem -LiteralPath $output.Root -Filter 'workshop.json' -File -Force -Recurse -ErrorAction Stop)
    if ($workshopControlFiles.Count -gt 1 -or
        ($workshopControlFiles.Count -eq 1 -and
            -not [string]::Equals($workshopControlFiles[0].FullName, (Join-Path $output.Root 'workshop.json'), [System.StringComparison]::OrdinalIgnoreCase))) {
        throw 'Existing multi-platform output may contain at most one root workshop.json upload control file.'
    }
    if ($workshopControlFiles.Count -eq 1) {
        $preservedWorkshopControl = Assert-DtmApiMultiPlatformWorkshopControlFile `
            -Path $workshopControlFiles[0].FullName `
            -ExpectedWorkshopId ([string]$product.workshopId)
    }
}

$token = [Guid]::NewGuid().ToString('N')
$transientPrefix = '.' + $output.Leaf + '.dtmapi-multiplatform-'
$staging = Join-Path $output.Parent ($transientPrefix + 'staging-' + $token)
$backup = Join-Path $output.Parent ($transientPrefix + 'previous-' + $token)
if ((Test-Path -LiteralPath $staging) -or (Test-Path -LiteralPath $backup)) {
    throw 'Multi-platform package transient path unexpectedly exists.'
}
[System.IO.Directory]::CreateDirectory($staging) | Out-Null
$published = $false
$oldMoved = $false
try {
    foreach ($relative in $accepted.SharedPaths) {
        Copy-DtmApiMultiPlatformFile `
            -Source (Join-Path $accepted.Root $relative.Replace('/', '\')) `
            -Destination (Join-Path $staging $relative.Replace('/', '\'))
    }

    foreach ($batName in $windowsBatNames) {
        $sourceBat = Join-Path $metadata $batName
        Copy-DtmApiMultiPlatformFile `
            -Source $sourceBat `
            -Destination (Join-Path $staging $batName)
    }

    foreach ($shellName in [DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::LinuxEntries) {
        $sourceShell = Join-Path $metadata $shellName
        Copy-DtmApiMultiPlatformFile -Source $sourceShell -Destination (Join-Path $staging $shellName)
    }

    Copy-DtmApiMultiPlatformFile -Source $readmeFile -Destination (Join-Path $staging 'README_FIRST.txt')
    [IO.File]::WriteAllText((Join-Path $staging 'info.json'),
        (Get-DtmApiMultiPlatformCandidateInfoText -MetadataRoot $metadata -RuntimeVersion $accepted.RuntimeVersion),
        (New-Object Text.UTF8Encoding($false)))
    Copy-DtmApiMultiPlatformFile -Source $icon -Destination (Join-Path $staging 'icon.png')
    Copy-DtmApiMultiPlatformFile -Source $preview -Destination (Join-Path $staging 'preview.png')

    $windowsPackageRelative = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::WindowsHost).Replace('/', '\')
    $linuxPackageRelative = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::LinuxHost).Replace('/', '\')
    $hostManifestRelative = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::HostManifest).Replace('/', '\')
    $runtimePackageManifestRelative = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::MultiPlatformManifest).Replace('/', '\')
    Copy-DtmApiMultiPlatformFile -Source $windowsHost -Destination (Join-Path $staging $windowsPackageRelative)
    Copy-DtmApiMultiPlatformFile -Source $linuxHost -Destination (Join-Path $staging $linuxPackageRelative)

    $windowsHostPackagePath = Join-Path $staging $windowsPackageRelative
    $linuxHostPackagePath = Join-Path $staging $linuxPackageRelative
    $windowsItem = Get-Item -LiteralPath $windowsHostPackagePath -Force -ErrorAction Stop
    $linuxItem = Get-Item -LiteralPath $linuxHostPackagePath -Force -ErrorAction Stop
    $windowsHostSha256 = Get-DtmApiMultiPlatformFileSha256 -Path $windowsHostPackagePath
    $linuxHostSha256 = Get-DtmApiMultiPlatformFileSha256 -Path $linuxHostPackagePath
    $bepInExRelative = [DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::BepInExArchive
    $bepInExPath = Join-Path $staging $bepInExRelative.Replace('/', '\')
    $bepInExItem = Get-Item -LiteralPath $bepInExPath -Force -ErrorAction Stop
    $bepInExSha256 = Get-DtmApiMultiPlatformFileSha256 -Path $bepInExPath

    $runtimePackageManifest = [ordered]@{
        SchemaVersion = 2
        DistributionId = 'dtmapi-multiplatform'
        WorkshopId = [string]$product.workshopId
        ShowNativeTitleEntry = $true
        DisplayName = [string]$info.name
        InstallerVersion = [string]$hostBuild.InstallerVersion
        RuntimeVersion = [string]$accepted.RuntimeVersion
        InstallerBuildCommit = $InstallerBuildCommit
        RuntimeSource = $accepted.RuntimeSource
        BepInExArchive = [ordered]@{
            Rid = ''
            RelativePath = $bepInExRelative
            Length = [long]$bepInExItem.Length
            Sha256 = $bepInExSha256
        }
        HostArtifacts = @(
            [ordered]@{
                Rid = 'win-x64'
                RelativePath = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::WindowsHost).Replace('\', '/')
                Length = [long]$windowsItem.Length
                Sha256 = $windowsHostSha256
            },
            [ordered]@{
                Rid = 'linux-x64'
                RelativePath = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::LinuxHost).Replace('\', '/')
                Length = [long]$linuxItem.Length
                Sha256 = $linuxHostSha256
            }
        )
        RequiredLinuxLaunchOption = $script:DtmApiMultiPlatformRequiredLaunchOption
        CrossOverDllOverride = 'winhttp=n,b'
    }
    Write-DtmApiMultiPlatformJsonNoBom -Path (Join-Path $staging $runtimePackageManifestRelative) -Value $runtimePackageManifest

    $hostManifest = [ordered]@{
        SchemaVersion = 2
        DistributionId = 'dtmapi-multiplatform'
        DistributionState = 'Candidate'
        WorkshopId = [string]$product.workshopId
        InstallerVersion = [string]$hostBuild.InstallerVersion
        InstallerBuildCommit = $InstallerBuildCommit.ToLowerInvariant()
        InstallerSourceTreeState = 'clean-committed-inputs'
        InstallerBuildSource = $hostBuild
        RuntimeSource = $accepted.RuntimeSource
        RuntimeVersion = [string]$accepted.RuntimeVersion
        RuntimeBuildCommit = [string]$accepted.BuildCommit
        ImportedRuntimePayload = [ordered]@{
            Algorithm = [string]$accepted.PlayerPayload.Algorithm
            FileCount = [int]$accepted.PlayerPayload.FileCount
            Bytes = [long]$accepted.PlayerPayload.Bytes
            TreeSha256 = [string]$accepted.PlayerPayload.TreeSha256
        }
        SharedPayload = [ordered]@{
            Algorithm = [string]$accepted.SharedPayload.Algorithm
            FileCount = [int]$accepted.SharedPayload.FileCount
            Bytes = [long]$accepted.SharedPayload.Bytes
            TreeSha256 = [string]$accepted.SharedPayload.TreeSha256
        }
        Hosts = @(
            [ordered]@{
                Rid = 'win-x64'
                Format = 'PE32+'
                RelativePath = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::WindowsHost).Replace('\', '/')
                Length = [long]$windowsItem.Length
                Sha256 = $windowsHostSha256
            },
            [ordered]@{
                Rid = 'linux-x64'
                Format = 'ELF64'
                RelativePath = ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::LinuxHost).Replace('\', '/')
                Length = [long]$linuxItem.Length
                Sha256 = $linuxHostSha256
            }
        )
        RequiredLinuxLaunchOption = $script:DtmApiMultiPlatformRequiredLaunchOption
        CodeSigning = 'unsigned-experimental'
    }
    Write-DtmApiMultiPlatformJsonNoBom -Path (Join-Path $staging $hostManifestRelative) -Value $hostManifest

    if ($null -ne $preservedWorkshopControl) {
        Copy-DtmApiMultiPlatformFile `
            -Source $preservedWorkshopControl.Path `
            -Destination (Join-Path $staging 'workshop.json')
    }

    if (Test-Path -LiteralPath $output.Root -PathType Container) {
        [System.IO.Directory]::Move($output.Root, $backup)
        $oldMoved = $true
    }
    try {
        [System.IO.Directory]::Move($staging, $output.Root)
    }
    catch {
        if ($oldMoved -and -not (Test-Path -LiteralPath $output.Root) -and (Test-Path -LiteralPath $backup -PathType Container)) {
            [System.IO.Directory]::Move($backup, $output.Root)
            $oldMoved = $false
        }
        throw
    }
    $published = $true
    if ($oldMoved) {
        Remove-DtmApiMultiPlatformTransient -Path $backup -Parent $output.Parent -ExpectedLeafPrefix $transientPrefix
        $oldMoved = $false
    }
}
finally {
    if (-not $published) {
        if ($oldMoved -and -not (Test-Path -LiteralPath $output.Root) -and (Test-Path -LiteralPath $backup -PathType Container)) {
            [System.IO.Directory]::Move($backup, $output.Root)
            $oldMoved = $false
        }
        if (Test-Path -LiteralPath $staging) {
            Remove-DtmApiMultiPlatformTransient -Path $staging -Parent $output.Parent -ExpectedLeafPrefix $transientPrefix
        }
        if ($oldMoved -and (Test-Path -LiteralPath $backup)) {
            throw "Publication failed after moving the previous package; preserving bounded backup for recovery: $backup"
        }
    }
}

$finalReceipt = Get-DtmApiMultiPlatformPublishedContentReceipt -Root $output.Root
$finalWorkshopControl = $preservedWorkshopControl
Write-Host ("DTMAPI multi-platform package atomically published: {0}" -f $output.Root)
Write-Host ("Imported Runtime: {0} files, {1} bytes, {2}" -f $accepted.PlayerPayload.FileCount, $accepted.PlayerPayload.Bytes, $accepted.PlayerPayload.TreeSha256)
Write-Host ("Shared Runtime files: {0} files, {1} bytes, {2}" -f $accepted.SharedPayload.FileCount, $accepted.SharedPayload.Bytes, $accepted.SharedPayload.TreeSha256)
Write-Host ("Final candidate: {0} files, {1} bytes, {2}" -f $finalReceipt.FileCount, $finalReceipt.Bytes, $finalReceipt.TreeSha256)
if ($null -ne $finalWorkshopControl) {
    Write-Host ("Preserved Workshop upload control: ID={0}; {1} bytes; {2}" -f $finalWorkshopControl.WorkshopId, $finalWorkshopControl.Bytes, $finalWorkshopControl.Sha256)
}

[pscustomobject][ordered]@{
    PackageRoot = $output.Root
    ImportedRuntimeRoot = $accepted.Root
    ImportedPlayerPayloadFileCount = [int]$accepted.PlayerPayload.FileCount
    ImportedPlayerPayloadBytes = [long]$accepted.PlayerPayload.Bytes
    ImportedPlayerPayloadTreeSha256 = [string]$accepted.PlayerPayload.TreeSha256
    SharedFileCount = [int]$accepted.SharedPayload.FileCount
    SharedBytes = [long]$accepted.SharedPayload.Bytes
    SharedTreeSha256 = [string]$accepted.SharedPayload.TreeSha256
    PackageFileCount = [int]$finalReceipt.FileCount
    PackageBytes = [long]$finalReceipt.Bytes
    PackageTreeSha256 = [string]$finalReceipt.TreeSha256
    WorkshopControlFilePreserved = ($null -ne $finalWorkshopControl)
    WorkshopControlFileBytes = if ($null -eq $finalWorkshopControl) { 0L } else { [long]$finalWorkshopControl.Bytes }
    WorkshopControlFileSha256 = if ($null -eq $finalWorkshopControl) { '' } else { [string]$finalWorkshopControl.Sha256 }
}
