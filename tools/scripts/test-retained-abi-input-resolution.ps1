[CmdletBinding()]
param(
    [string] $TestRoot = $env:DTMAPI_TEST_TEMP_ROOT
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\retained-abi-snapshot-selection.ps1"
$ErrorActionPreference = 'Stop'

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)] [AllowNull()] $Actual,
        [Parameter(Mandatory = $true)] [AllowNull()] $Expected,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if (-not [object]::Equals($Actual, $Expected)) {
        throw "$Label mismatch. Expected '$Expected', found '$Actual'."
    }
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $Action,
        [Parameter(Mandatory = $true)] [string] $MessagePattern,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    try {
        & $Action
    }
    catch {
        if ([string]$_.Exception.Message -notmatch $MessagePattern) {
            throw "$Label produced an unexpected error: $($_.Exception.Message)"
        }
        return
    }
    throw "$Label did not fail."
}

function Copy-ExactSnapshotFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $SourcePayloadRoot,
        [Parameter(Mandatory = $true)] [string] $DestinationSnapshotRoot,
        [Parameter(Mandatory = $true)] [object[]] $ExpectedFiles,
        [string] $MutateWorkshopId = ''
    )

    $payloadRoot = Join-Path $DestinationSnapshotRoot 'payload'
    foreach ($expected in $ExpectedFiles) {
        $sourceItemRoot = Join-Path $SourcePayloadRoot ([string]$expected.WorkshopId)
        $matches = @(Get-ChildItem -LiteralPath $sourceItemRoot -Recurse -File -Filter ([string]$expected.FileName) -ErrorAction Stop)
        Assert-Equal -Actual $matches.Count -Expected 1 -Label "Snapshot fixture source $([string]$expected.WorkshopId)/$([string]$expected.FileName) count"
        $destinationItemRoot = Join-Path $payloadRoot ([string]$expected.WorkshopId)
        New-Item -ItemType Directory -Path $destinationItemRoot -Force | Out-Null
        $destination = Join-Path $destinationItemRoot ([string]$expected.FileName)
        Copy-Item -LiteralPath $matches[0].FullName -Destination $destination -Force
        if ([string]$expected.WorkshopId -ceq $MutateWorkshopId) {
            [System.IO.File]::AppendAllText($destination, 'intentional-newer-snapshot-drift')
        }
    }
}

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repo 'tmp\test-runs'
}
$managedTestRoot = [System.IO.Path]::GetFullPath($TestRoot)
New-Item -ItemType Directory -Path $managedTestRoot -Force | Out-Null
$sessionRoot = [System.IO.Path]::GetFullPath((Join-Path $managedTestRoot ('DTMAPI.RetainedAbiResolverTest.' + [Guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $sessionRoot -Parent $managedTestRoot) -or
    $sessionRoot.TrimEnd([char[]]@('\', '/')) -eq $managedTestRoot.TrimEnd([char[]]@('\', '/'))) {
    throw "Retained ABI resolver test session escaped its managed root: $sessionRoot"
}
New-Item -ItemType Directory -Path $sessionRoot -Force | Out-Null

try {
    $resolved = & "$PSScriptRoot\resolve-retained-abi-inputs.ps1" `
        -RepositoryRoot $repo `
        -BaselineAbstractionsDll '' `
        -AutoFishingDll '' `
        -PublicWorkshopRoot '' `
        -TestRoot $sessionRoot
    Assert-Equal -Actual ([string]$resolved.ResolutionMode) -Expected 'SiblingRetainedArtifactAuthority' -Label 'Default resolution mode'
    Assert-Equal `
        -Actual ((Get-FileHash -LiteralPath $resolved.BaselineAbstractionsDll -Algorithm SHA256).Hash) `
        -Expected '39A51683034FF0B7495BEB3DD1C50F76F591D4E24C63872B6502EA360EF8880B' `
        -Label 'Retained baseline SHA-256'
    Assert-Equal `
        -Actual ((Get-FileHash -LiteralPath $resolved.AutoFishingDll -Algorithm SHA256).Hash) `
        -Expected 'E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA' `
        -Label 'Retained AutoFishing SHA-256'

    $catalog = Get-Content -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $publicProducts = @($catalog.products | Where-Object {
        [string]$_.role -ceq 'PublishedProduct' -and [string]$_.distributionState -ceq 'PublicWorkshop'
    })
    $publicIds = @($publicProducts | ForEach-Object { [string]$_.workshopId })
    Assert-Equal -Actual $publicIds.Count -Expected 11 -Label 'Catalog public retained consumer count'
    $externalIds = @('3743621104', '3743644065', '3754869009', '3759797170')
    foreach ($workshopId in @($publicIds + $externalIds)) {
        if (-not (Test-Path -LiteralPath (Join-Path $resolved.PublicWorkshopRoot $workshopId) -PathType Container)) {
            throw "Resolved retained Workshop snapshot is missing consumer $workshopId."
        }
    }

    $retainedAuditPath = Join-Path $repo (([string]$catalog.publicApiFreeze.retainedBinaryAudit).Replace('/', '\'))
    $retainedAudit = Get-Content -LiteralPath $retainedAuditPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $auditByProductId = @{}
    foreach ($auditProduct in @($retainedAudit.retainedPublicProducts)) {
        $auditByProductId[[string]$auditProduct.productId] = $auditProduct
    }
    $expectedSnapshotFiles = @()
    foreach ($product in $publicProducts) {
        $auditProduct = $auditByProductId[[string]$product.catalogId]
        $expectedSnapshotFiles += [pscustomobject]@{
            WorkshopId = [string]$product.workshopId
            FileName = [string]$auditProduct.fileName
            Sha256 = [string]$auditProduct.sha256
        }
    }
    foreach ($external in @($retainedAudit.retainedExternalConsumers)) {
        $expectedSnapshotFiles += [pscustomobject]@{
            WorkshopId = [string]$external.workshopId
            FileName = [string]$external.fileName
            Sha256 = [string]$external.sha256
        }
    }
    Assert-Equal -Actual $expectedSnapshotFiles.Count -Expected 15 -Label 'Catalog/binary-audit exact snapshot identity count'

    $fakeSubscriptions = Join-Path $sessionRoot 'snapshot-selection-fixture'
    $exactSnapshot = Join-Path $fakeSubscriptions 'workshop-2285550-20260101-exact'
    $newerDriftSnapshot = Join-Path $fakeSubscriptions 'workshop-2285550-99991231-drift'
    Copy-ExactSnapshotFixture `
        -SourcePayloadRoot $resolved.PublicWorkshopRoot `
        -DestinationSnapshotRoot $exactSnapshot `
        -ExpectedFiles $expectedSnapshotFiles
    Copy-ExactSnapshotFixture `
        -SourcePayloadRoot $resolved.PublicWorkshopRoot `
        -DestinationSnapshotRoot $newerDriftSnapshot `
        -ExpectedFiles $expectedSnapshotFiles `
        -MutateWorkshopId ([string]$expectedSnapshotFiles[0].WorkshopId)

    $maskedResolution = Resolve-UniqueExactRetainedWorkshopSnapshot `
        -SubscriptionRoot $fakeSubscriptions `
        -SteamAppId '2285550' `
        -ExpectedFiles $expectedSnapshotFiles
    Assert-Equal `
        -Actual ([System.IO.Path]::GetFullPath([string]$maskedResolution)) `
        -Expected ([System.IO.Path]::GetFullPath((Join-Path $exactSnapshot 'payload'))) `
        -Label 'Newer hash-drifted snapshot must not mask the unique exact snapshot'

    $duplicateExactSnapshot = Join-Path $fakeSubscriptions 'workshop-2285550-20260102-exact-duplicate'
    Copy-ExactSnapshotFixture `
        -SourcePayloadRoot $resolved.PublicWorkshopRoot `
        -DestinationSnapshotRoot $duplicateExactSnapshot `
        -ExpectedFiles $expectedSnapshotFiles
    Assert-Throws -Label 'Ambiguous exact retained snapshot authority' -MessagePattern 'Multiple exact retained Workshop' -Action {
        Resolve-UniqueExactRetainedWorkshopSnapshot `
            -SubscriptionRoot $fakeSubscriptions `
            -SteamAppId '2285550' `
            -ExpectedFiles $expectedSnapshotFiles
    }
    foreach ($fixtureSnapshot in @($exactSnapshot, $duplicateExactSnapshot)) {
        $resolvedFixtureSnapshot = [System.IO.Path]::GetFullPath($fixtureSnapshot)
        if (-not (Test-DtmApiPathIsSameOrChild -Child $resolvedFixtureSnapshot -Parent $sessionRoot) -or
            $resolvedFixtureSnapshot.TrimEnd([char[]]@('\', '/')) -eq $sessionRoot.TrimEnd([char[]]@('\', '/'))) {
            throw "Refusing to remove retained snapshot fixture outside its managed session: $resolvedFixtureSnapshot"
        }
        Remove-Item -LiteralPath $resolvedFixtureSnapshot -Recurse -Force
    }
    Assert-Throws -Label 'Missing exact retained snapshot authority' -MessagePattern 'No exact retained Workshop' -Action {
        Resolve-UniqueExactRetainedWorkshopSnapshot `
            -SubscriptionRoot $fakeSubscriptions `
            -SteamAppId '2285550' `
            -ExpectedFiles $expectedSnapshotFiles
    }

    $explicit = & "$PSScriptRoot\resolve-retained-abi-inputs.ps1" `
        -RepositoryRoot $repo `
        -BaselineAbstractionsDll $resolved.BaselineAbstractionsDll `
        -AutoFishingDll $resolved.AutoFishingDll `
        -PublicWorkshopRoot $resolved.PublicWorkshopRoot `
        -TestRoot $sessionRoot
    Assert-Equal -Actual ([string]$explicit.ResolutionMode) -Expected 'ExplicitAllOrNoneOverrides' -Label 'Explicit resolution mode'
    Assert-Equal -Actual $explicit.CleanupRoot -Expected $null -Label 'Explicit resolution cleanup root'

    Assert-Throws -Label 'Partial retained ABI override' -MessagePattern 'all-or-none' -Action {
        & "$PSScriptRoot\resolve-retained-abi-inputs.ps1" `
            -RepositoryRoot $repo `
            -BaselineAbstractionsDll $resolved.BaselineAbstractionsDll `
            -AutoFishingDll '' `
            -PublicWorkshopRoot '' `
            -TestRoot $sessionRoot
    }

    $missingAuthorityRoot = Join-Path (Split-Path -Parent $repo) ('DTMAPI-retained-artifacts-missing-' + [Guid]::NewGuid().ToString('N'))
    Assert-Throws -Label 'Missing retained ABI authority' -MessagePattern 'Cannot find path|does not exist' -Action {
        & "$PSScriptRoot\resolve-retained-abi-inputs.ps1" `
            -RepositoryRoot $repo `
            -RetainedArtifactsRoot $missingAuthorityRoot `
            -BaselineAbstractionsDll '' `
            -AutoFishingDll '' `
            -PublicWorkshopRoot '' `
            -TestRoot $sessionRoot
    }

    Write-Host 'Retained ABI input resolver: PASS (unique exact sibling authority, newer drift masking rejected, duplicate/zero exact failure, explicit all-or-none override, missing authority failure).'
}
finally {
    if (Test-Path -LiteralPath $sessionRoot) {
        Remove-Item -LiteralPath $sessionRoot -Recurse -Force
    }
}
