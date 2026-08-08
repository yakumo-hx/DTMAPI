[CmdletBinding()]
param(
    [string] $RepositoryRoot = '',
    [string] $RetainedArtifactsRoot = '',
    [string] $BaselineAbstractionsDll = $env:DTMAPI_RETAINED_BASELINE_ABSTRACTIONS_DLL,
    [string] $AutoFishingDll = $env:DTMAPI_RETAINED_AUTOFISHING_DLL,
    [string] $PublicWorkshopRoot = $env:DTMAPI_RETAINED_PUBLIC_WORKSHOP_ROOT,
    [string] $TestRoot = $env:DTMAPI_TEST_TEMP_ROOT
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\retained-abi-snapshot-selection.ps1"
$ErrorActionPreference = 'Stop'

function Resolve-RequiredFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label does not exist as a file: $Path"
    }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) {
        throw "$Label must be a file: $Path"
    }
    return $item.FullName
}

function Resolve-RequiredDirectory {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Label does not exist as a directory: $Path"
    }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer) {
        throw "$Label must be a directory: $Path"
    }
    return $item.FullName
}

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Get-RepoRoot
}
$repo = [System.IO.Path]::GetFullPath($RepositoryRoot)

$overrideValues = @($BaselineAbstractionsDll, $AutoFishingDll, $PublicWorkshopRoot)
$overrideCount = @($overrideValues | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count
if ($overrideCount -ne 0 -and $overrideCount -ne $overrideValues.Count) {
    throw 'Retained release ABI path overrides are all-or-none: set DTMAPI_RETAINED_BASELINE_ABSTRACTIONS_DLL, DTMAPI_RETAINED_AUTOFISHING_DLL, and DTMAPI_RETAINED_PUBLIC_WORKSHOP_ROOT together.'
}

if ($overrideCount -eq $overrideValues.Count) {
    [pscustomobject]@{
        ResolutionMode = 'ExplicitAllOrNoneOverrides'
        BaselineAbstractionsDll = Resolve-RequiredFile -Path $BaselineAbstractionsDll -Label 'Retained baseline DTMAPI.Abstractions'
        AutoFishingDll = Resolve-RequiredFile -Path $AutoFishingDll -Label 'Retained AutoFishing consumer'
        PublicWorkshopRoot = Resolve-RequiredDirectory -Path $PublicWorkshopRoot -Label 'Retained public Workshop root'
        CleanupRoot = $null
    }
    return
}

if ([string]::IsNullOrWhiteSpace($RetainedArtifactsRoot)) {
    $RetainedArtifactsRoot = Join-Path (Split-Path -Parent $repo) 'DTMAPI-retained-artifacts'
}
$retainedRoot = Assert-DtmApiPrivateArtifactPathOutsideRepository `
    -Path $RetainedArtifactsRoot `
    -RepositoryRoot $repo `
    -Label 'Retained ABI artifact authority root'
$retainedRoot = Resolve-RequiredDirectory -Path $retainedRoot -Label 'Retained ABI artifact authority root'

$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
$publicProducts = @($catalog.products | Where-Object {
    [string]$_.role -ceq 'PublishedProduct' -and [string]$_.distributionState -ceq 'PublicWorkshop'
})
if ($publicProducts.Count -ne 11) {
    throw "Retained ABI input resolution expected exactly 11 PublicWorkshop products, found $($publicProducts.Count)."
}

$retainedAuditRelativePath = [string]$catalog.publicApiFreeze.retainedBinaryAudit
if ([string]::IsNullOrWhiteSpace($retainedAuditRelativePath)) {
    throw 'The Product Catalog does not identify its retained binary audit authority.'
}
$retainedAuditPath = Join-Path $repo $retainedAuditRelativePath.Replace('/', '\')
$retainedAudit = Get-Content -LiteralPath $retainedAuditPath -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not [bool]$retainedAudit.overallPass) {
    throw "The Catalog-owned retained binary audit is not passing: $retainedAuditPath"
}
$retainedAuditProducts = @($retainedAudit.retainedPublicProducts)
$retainedAuditExternal = @($retainedAudit.retainedExternalConsumers)
if ($retainedAuditProducts.Count -ne 11 -or $retainedAuditExternal.Count -ne 4) {
    throw "Retained binary audit must contain exactly 11 first-party and four external consumers; found $($retainedAuditProducts.Count) and $($retainedAuditExternal.Count)."
}
$retainedAuditByProductId = @{}
foreach ($auditProduct in $retainedAuditProducts) {
    $productId = [string]$auditProduct.productId
    if ([string]::IsNullOrWhiteSpace($productId) -or $retainedAuditByProductId.ContainsKey($productId)) {
        throw "Retained binary audit contains a missing or duplicate productId: '$productId'."
    }
    $retainedAuditByProductId[$productId] = $auditProduct
}
$expectedSnapshotFiles = @()
foreach ($product in $publicProducts) {
    $productId = [string]$product.catalogId
    if (-not $retainedAuditByProductId.ContainsKey($productId)) {
        throw "Retained binary audit is missing Catalog public product '$productId'."
    }
    $auditProduct = $retainedAuditByProductId[$productId]
    $expectedSnapshotFiles += [pscustomobject]@{
        WorkshopId = [string]$product.workshopId
        FileName = [string]$auditProduct.fileName
        Sha256 = [string]$auditProduct.sha256
    }
}
foreach ($external in $retainedAuditExternal) {
    $expectedSnapshotFiles += [pscustomobject]@{
        WorkshopId = [string]$external.workshopId
        FileName = [string]$external.fileName
        Sha256 = [string]$external.sha256
    }
}
foreach ($expected in $expectedSnapshotFiles) {
    if ([string]::IsNullOrWhiteSpace([string]$expected.WorkshopId) -or
        [string]::IsNullOrWhiteSpace([string]$expected.FileName) -or
        [string]$expected.Sha256 -notmatch '^[0-9A-Fa-f]{64}$') {
        throw 'Retained binary audit contains an incomplete Workshop/file/hash identity.'
    }
}
$nonUniqueWorkshopIdentities = @($expectedSnapshotFiles | Group-Object WorkshopId | Where-Object { $_.Count -ne 1 })
if ($nonUniqueWorkshopIdentities.Count -ne 0) {
    throw 'Retained binary audit and Product Catalog do not project 15 unique Workshop consumer identities.'
}

$autoFishing = @($publicProducts | Where-Object { [string]$_.catalogId -ceq 'auto-fishing' })
if ($autoFishing.Count -ne 1) {
    throw "Retained ABI input resolution expected exactly one auto-fishing Catalog row, found $($autoFishing.Count)."
}
$autoFishingProduct = $autoFishing[0]
$autoFishingArchive = [string]$autoFishingProduct.retainedArtifact.archiveSubdirectory
if ([string]::IsNullOrWhiteSpace($autoFishingArchive)) {
    throw 'The auto-fishing Catalog row does not identify its retained artifact archive subdirectory.'
}
$retainedEntryProperty = $autoFishingProduct.retainedArtifact.PSObject.Properties['entryDll']
$autoFishingEntry = if ($null -ne $retainedEntryProperty -and
    -not [string]::IsNullOrWhiteSpace([string]$retainedEntryProperty.Value)) {
    [string]$retainedEntryProperty.Value
}
else {
    [string]$autoFishingProduct.packageDll
}
$resolvedAutoFishing = Resolve-RequiredFile `
    -Path (Join-Path $retainedRoot ("products\{0}\payload\Content\DTMAPI\{1}" -f $autoFishingArchive, $autoFishingEntry)) `
    -Label 'Catalog-owned retained AutoFishing consumer'

$steamAppId = [string]$catalog.runtime.steamAppId
$subscriptionRoot = Resolve-RequiredDirectory `
    -Path (Join-Path $retainedRoot 'subscriptions') `
    -Label 'Retained Workshop subscription authority root'
$resolvedWorkshopRoot = Resolve-UniqueExactRetainedWorkshopSnapshot `
    -SubscriptionRoot $subscriptionRoot `
    -SteamAppId $steamAppId `
    -ExpectedFiles $expectedSnapshotFiles

$runtimeArchive = Resolve-RequiredFile `
    -Path (Join-Path $retainedRoot 'runtime\DTMAPI-0.5.2-alpha-workshop-3743016467.zip') `
    -Label 'Retained DTMAPI 0.5.2 Runtime archive'
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repo 'tmp\test-runs'
}
$managedTestRoot = [System.IO.Path]::GetFullPath($TestRoot)
New-Item -ItemType Directory -Path $managedTestRoot -Force | Out-Null
$cleanupRoot = [System.IO.Path]::GetFullPath((Join-Path $managedTestRoot ('DTMAPI.RetainedAbi.' + [Guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $cleanupRoot -Parent $managedTestRoot) -or
    $cleanupRoot.TrimEnd([char[]]@('\', '/')) -eq $managedTestRoot.TrimEnd([char[]]@('\', '/'))) {
    throw "Retained ABI extraction session escaped its managed test boundary: $cleanupRoot"
}

try {
    New-Item -ItemType Directory -Path $cleanupRoot -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($runtimeArchive)
    try {
        $entryPath = 'payload/Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.Abstractions.dll'
        $entries = @($archive.Entries | Where-Object { [string]$_.FullName -ceq $entryPath })
        if ($entries.Count -ne 1) {
            throw "Retained Runtime archive must contain exactly one '$entryPath' entry, found $($entries.Count)."
        }
        $baseline = Join-Path $cleanupRoot 'DTMAPI.Abstractions.dll'
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $baseline, $false)
    }
    finally {
        $archive.Dispose()
    }

    [pscustomobject]@{
        ResolutionMode = 'SiblingRetainedArtifactAuthority'
        BaselineAbstractionsDll = Resolve-RequiredFile -Path $baseline -Label 'Extracted retained baseline DTMAPI.Abstractions'
        AutoFishingDll = $resolvedAutoFishing
        PublicWorkshopRoot = $resolvedWorkshopRoot
        CleanupRoot = $cleanupRoot
    }
}
catch {
    if (Test-Path -LiteralPath $cleanupRoot) {
        Remove-Item -LiteralPath $cleanupRoot -Recurse -Force
    }
    throw
}
