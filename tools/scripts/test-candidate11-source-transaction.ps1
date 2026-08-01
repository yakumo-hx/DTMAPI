param(
    [switch] $Quiet
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
. "$PSScriptRoot\candidate11-source-transaction.ps1" -LibraryOnly
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-Candidate11Test {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw "Candidate11 source transaction test failed: $Message"
    }
}

function Write-Candidate11TestText {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [AllowEmptyString()] [string] $Text
    )

    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

function Write-Candidate11TestJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    Write-Candidate11TestText -Path $Path -Text (($Value | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
}

function New-Candidate11TestFixture {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $candidateRoot = Join-Path $Root 'C'
    $persistentRoot = Join-Path $Root 'P'
    $modsRoot = Join-Path $persistentRoot 'MODS'
    $gameDir = Join-Path $Root 'G'
    [System.IO.Directory]::CreateDirectory($candidateRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($modsRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory((Join-Path $gameDir 'DolocTown_Data')) | Out-Null
    Write-Candidate11TestText -Path (Join-Path $gameDir 'DolocTown.exe') -Text 'fixture-game'

    $runtimePackageRoot = Join-Path $candidateRoot 'DTMAPI'
    $runtimePayloadRoot = Join-Path $runtimePackageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $runtimeAssemblyReceipts = New-Object 'System.Collections.Generic.List[object]'
    foreach ($runtimeAssemblyName in $script:Candidate11RuntimeAssemblyNames) {
        $runtimeAssemblyPath = Join-Path $runtimePayloadRoot $runtimeAssemblyName
        Write-Candidate11TestText -Path $runtimeAssemblyPath -Text ('runtime-candidate-' + $runtimeAssemblyName)
        $runtimeItem = Get-Item -LiteralPath $runtimeAssemblyPath -Force
        $runtimeAssemblyReceipts.Add([ordered]@{
            FileName = $runtimeAssemblyName
            Length = [int64]$runtimeItem.Length
            Sha256 = ([string](Get-FileHash -LiteralPath $runtimeAssemblyPath -Algorithm SHA256).Hash).ToLowerInvariant()
            FileVersion = $script:DtmApiBinaryVersion
        }) | Out-Null
    }
    $runtimeBuildCommit = '0123456789abcdef0123456789abcdef01234567'
    $runtimeManifest = [ordered]@{
        SchemaVersion = 1
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        BuildCommit = $runtimeBuildCommit
        BuildTime = '2026-07-19T00:00:00.0000000Z'
        PackageKind = 'workshop-runtime'
        IncludedAssemblies = @($runtimeAssemblyReceipts.ToArray())
        BundledMods = @()
    }
    Write-Candidate11TestJson -Path (Join-Path $runtimePackageRoot 'Content\DTMAPI\release-manifest.json') -Value $runtimeManifest

    $installedRuntimeRoot = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
    foreach ($runtimeAssemblyName in $script:Candidate11RuntimeAssemblyNames) {
        $installedRuntimePath = Join-Path $installedRuntimeRoot $runtimeAssemblyName
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $installedRuntimePath)) | Out-Null
        [System.IO.File]::Copy((Join-Path $runtimePayloadRoot $runtimeAssemblyName), $installedRuntimePath, $false)
    }
    $installedManifest = [ordered]@{
        SchemaVersion = 1
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        BuildCommit = $runtimeBuildCommit
        BuildTime = '2026-07-19T00:00:01.0000000Z'
        PackageKind = 'local-install'
        IncludedAssemblies = @($runtimeAssemblyReceipts.ToArray())
        BundledMods = @()
    }
    Write-Candidate11TestJson -Path (Join-Path $gameDir 'DTMAPI\release-manifest.json') -Value $installedManifest

    $products = New-Object 'System.Collections.Generic.List[object]'
    $originalSnapshots = [ordered]@{}
    for ($index = 1; $index -le 11; $index++) {
        $suffix = $index.ToString('00')
        $catalogId = 'product-' + $suffix
        $uniqueId = 'Fixture.Candidate11.Product' + $suffix
        $officialFolder = 'Fixture_Official_' + $suffix
        $packageName = 'Fixture-Package-' + $suffix
        $packageDll = 'Fixture.Candidate11.Product' + $suffix + '.dll'
        $product = [ordered]@{
            catalogId = $catalogId
            role = 'PublishedProduct'
            productType = 'FunctionalCodeMod'
            distributionState = 'PublicWorkshop'
            releaseEligibility = 'RebuildBlocked'
            uniqueId = $uniqueId
            workshopId = '990000' + $suffix
            officialFolder = $officialFolder
            packageName = $packageName
            packageDll = $packageDll
            sourceVersion = '1.0.0-fixture'
        }
        $products.Add($product) | Out-Null

        $packageRoot = Join-Path $candidateRoot $packageName
        $contentRoot = Join-Path $packageRoot 'Content\DTMAPI'
        Write-Candidate11TestJson -Path (Join-Path $contentRoot 'manifest.json') -Value ([ordered]@{
            Name = 'Fixture ' + $suffix
            Author = 'DTMAPI Test'
            Version = '1.0.0-fixture'
            UniqueID = $uniqueId
            EntryDll = 'Content/DTMAPI/' + $packageDll
        })
        Write-Candidate11TestJson -Path (Join-Path $packageRoot 'info.json') -Value ([ordered]@{
            name = 'Fixture ' + $suffix
            author = 'DTMAPI Test'
            version = '1.0.0-fixture'
        })
        Write-Candidate11TestJson -Path (Join-Path $contentRoot 'dtmapi-package.json') -Value ([ordered]@{
            owner = 'DTMAPI'
            packageKind = 'workshop-mod'
            uniqueId = $uniqueId
        })
        Write-Candidate11TestText -Path (Join-Path $contentRoot $packageDll) -Text ('candidate-dll-' + $suffix)
        Write-Candidate11TestText -Path (Join-Path $packageRoot 'candidate-byte-sentinel.txt') -Text ('candidate-' + $suffix)

        $officialRoot = Join-Path $modsRoot $officialFolder
        Write-Candidate11TestText -Path (Join-Path $officialRoot 'original-byte-sentinel.txt') -Text ('original-' + $suffix)
        Write-Candidate11TestText -Path (Join-Path $officialRoot 'nested\state.bin') -Text ('original-nested-' + $suffix)
        if ($index -eq 1) {
            Set-Content -LiteralPath (Join-Path $officialRoot 'original-byte-sentinel.txt') -Stream 'original-preserved' -Value 'original-stream-byte' -Encoding UTF8
        }
        $originalSnapshots[$catalogId] = Get-Candidate11TreeSnapshot -Path $officialRoot -Context "Candidate11 test original $catalogId" -AllowAlternateDataStreams
    }

    $catalogPath = Join-Path $Root 'catalog.json'
    Write-Candidate11TestJson -Path $catalogPath -Value ([ordered]@{
        schemaVersion = 1
        catalogId = 'candidate11-fixture'
        products = @($products.ToArray())
    })

    $smokePath = Join-Path $Root 'fake-candidate11-smoke.ps1'
    $smokeSource = @'
param(
    [Parameter(Mandatory = $true)] [string] $PersistentRoot,
    [Parameter(Mandatory = $true)] [string] $CatalogPath,
    [Parameter(Mandatory = $true)] [string] $GameDir,
    [switch] $Fail,
    [switch] $Drift,
    [switch] $RuntimeDrift
)
$ErrorActionPreference = 'Stop'
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $CatalogPath | ConvertFrom-Json
foreach ($product in @($catalog.products)) {
    $root = Join-Path (Join-Path $PersistentRoot 'MODS') ([string]$product.officialFolder)
    $candidate = Join-Path $root 'candidate-byte-sentinel.txt'
    $original = Join-Path $root 'original-byte-sentinel.txt'
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf) -or (Test-Path -LiteralPath $original)) {
        throw "Fake smoke did not observe the candidate tree for $($product.catalogId)."
    }
}
if ($Drift) {
    $first = @($catalog.products | Sort-Object catalogId | Select-Object -First 1)[0]
    [System.IO.File]::WriteAllText(
        (Join-Path (Join-Path (Join-Path $PersistentRoot 'MODS') ([string]$first.officialFolder)) 'foreign-after-smoke.txt'),
        'preserve-this-drift',
        (New-Object System.Text.UTF8Encoding($false)))
}
if ($RuntimeDrift) {
    [System.IO.File]::AppendAllText(
        (Join-Path $GameDir 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'),
        '-runtime-drift',
        (New-Object System.Text.UTF8Encoding($false)))
}
Write-Output 'FAKE-CANDIDATE11-SMOKE-OBSERVED=11'
if ($Fail) {
    exit 23
}
exit 0
'@
    Write-Candidate11TestText -Path $smokePath -Text $smokeSource

    return [pscustomobject]@{
        Root = $Root
        CandidateRoot = $candidateRoot
        PersistentRoot = $persistentRoot
        GameDir = $gameDir
        RuntimePackageRoot = $runtimePackageRoot
        RuntimePayloadRoot = $runtimePayloadRoot
        InstalledRuntimeRoot = $installedRuntimeRoot
        ModsRoot = $modsRoot
        CatalogPath = $catalogPath
        SmokePath = $smokePath
        Products = @($products.ToArray())
        OriginalSnapshots = $originalSnapshots
    }
}

function Assert-Candidate11OriginalsRestored {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    foreach ($product in @($Fixture.Products)) {
        $officialRoot = Join-Path $Fixture.ModsRoot ([string]$product.officialFolder)
        Assert-Candidate11Test -Condition (Test-Path -LiteralPath (Join-Path $officialRoot 'original-byte-sentinel.txt') -PathType Leaf) -Message "$Label did not restore $($product.catalogId)."
        Assert-Candidate11Test -Condition (-not (Test-Path -LiteralPath (Join-Path $officialRoot 'candidate-byte-sentinel.txt'))) -Message "$Label left candidate bytes active for $($product.catalogId)."
        $actual = Get-Candidate11TreeSnapshot -Path $officialRoot -Context "$Label restored $($product.catalogId)" -AllowAlternateDataStreams
        $catalogKey = [string]$product.catalogId
        $expected = $Fixture.OriginalSnapshots[$catalogKey]
        Assert-Candidate11Test -Condition (Test-Candidate11SnapshotsEqual -Expected $expected -Actual $actual) -Message "$Label did not restore exact original bytes for $($product.catalogId)."
    }
}

$repo = Get-RepoRoot
$managedParent = Join-Path $repo 'tmp\test-runs\c11'
$testRoot = Join-Path $managedParent ([Guid]::NewGuid().ToString('N'))
$resolvedManagedParent = Get-Candidate11CanonicalPath -Path $managedParent
$resolvedTestRoot = Get-Candidate11CanonicalPath -Path $testRoot
Assert-Candidate11Test -Condition (Test-Candidate11SameOrChildPath -Child $resolvedTestRoot -Parent $resolvedManagedParent) -Message 'Managed fixture root escaped tmp/test-runs.'
[System.IO.Directory]::CreateDirectory($resolvedTestRoot) | Out-Null

$oldTestMode = $env:DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE
try {
    $env:DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE = '1'
    $sourceText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'candidate11-source-transaction.ps1'), [System.Text.Encoding]::UTF8)
    Assert-Candidate11Test -Condition ($sourceText -notmatch '(?im)^\s*Remove-Item\b') -Message 'Production transaction helper contains a Remove-Item operation.'
    Assert-Candidate11Test -Condition ($sourceText -match '\[System\.IO\.Directory\]::Move') -Message 'Production helper does not use same-volume Directory.Move operations.'
    Assert-Candidate11Test -Condition ($sourceText -match '\[System\.IO\.FileMode\]::CreateNew') -Message 'Production receipts are not fail-closed CreateNew writes.'
    Test-DtmApiWindowsPowerShellSyntax -Paths @(
        (Join-Path $PSScriptRoot 'candidate11-source-transaction.ps1'),
        $PSCommandPath
    ) -AllowCoreFallback

    $trackedCatalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
    $trackedCatalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $trackedCatalogPath | ConvertFrom-Json
    $trackedCandidate11 = @($trackedCatalog.products | Where-Object {
        [string]$_.role -ceq 'PublishedProduct' -and [string]$_.distributionState -ceq 'PublicWorkshop'
    })
    Assert-Candidate11Test -Condition ($trackedCandidate11.Count -eq 11) -Message "Tracked Catalog does not currently select exactly 11 Candidate11 products; found $($trackedCandidate11.Count)."
    Assert-Candidate11Test -Condition (@($trackedCandidate11 | Where-Object {
        [string]::IsNullOrWhiteSpace([string]$_.catalogId) -or
        [string]::IsNullOrWhiteSpace([string]$_.uniqueId) -or
        [string]::IsNullOrWhiteSpace([string]$_.officialFolder) -or
        [string]::IsNullOrWhiteSpace([string]$_.packageName) -or
        [string]::IsNullOrWhiteSpace([string]$_.packageDll)
    }).Count -eq 0) -Message 'Tracked Candidate11 Catalog rows contain incomplete identity/path fields.'

    Assert-Candidate11SmokeContract -Arguments @('-UseSteam','-SkipInstall','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence')
    $badSmokeContract = ''
    try {
        Assert-Candidate11SmokeContract -Arguments @('-UseSteam','-SkipInstall','-OfficialModProfile','Published11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence')
    }
    catch {
        $badSmokeContract = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($badSmokeContract -match 'Local11') -Message 'Published11 smoke arguments did not fail the Candidate11 contract.'
    $qaSmokeContract = ''
    try {
        Assert-Candidate11SmokeContract -Arguments @('-UseSteam','-SkipInstall','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-StageQaHost')
    }
    catch {
        $qaSmokeContract = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($qaSmokeContract -match 'ordinary no-QA') -Message 'QA-host staging did not fail the ordinary Candidate11 contract.'

    $unicodeCaseLeaf = '01 ' + [char]0x4e2d + [char]0x6587
    $success = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot $unicodeCaseLeaf)
    $successEvidence = Join-Path $success.Root 'E'
    $successResult = Invoke-DtmApiCandidate11SourceTransaction `
        -CandidatePackageRoot $success.CandidateRoot `
        -ProductCatalogPath $success.CatalogPath `
        -DolocPersistentRoot $success.PersistentRoot `
        -GameDirectory $success.GameDir `
        -FormalEvidenceRoot $successEvidence `
        -Id 'success-case' `
        -SmokePath $success.SmokePath `
        -SmokeArguments @('-PersistentRoot',$success.PersistentRoot,'-CatalogPath',$success.CatalogPath,'-GameDir',$success.GameDir) `
        -SkipRuntimeLockForTest `
        -SkipProcessCheckForTest `
        -SkipSmokeContractForTest
    Assert-Candidate11Test -Condition ([bool]$successResult.Passed) -Message 'Happy-path transaction did not pass.'
    Assert-Candidate11Test -Condition ([int]$successResult.ProductCount -eq 11) -Message 'Happy-path transaction did not bind exactly 11 products.'
    Assert-Candidate11Test -Condition (
        [bool]$successResult.RuntimeBinding.Passed -and
        [int]$successResult.RuntimeBinding.CandidatePreflight.AssemblyCount -eq 5 -and
        [bool]$successResult.RuntimeBinding.InstalledPostSmoke.AllAssemblyBytesMatched -and
        [bool]$successResult.RuntimeBinding.InstalledPostSmoke.ReleaseManifestProjectionMatched -and
        [bool]$successResult.RuntimeBinding.CandidateSourceUnchanged
    ) -Message 'Happy-path transaction did not bind the candidate Runtime manifest/five DLLs to the installed Runtime.'
    Assert-Candidate11OriginalsRestored -Fixture $success -Label 'Happy path'
    $successWork = Join-Path (Join-Path $success.PersistentRoot '.dtmapi-candidate11-transactions') 'success-case'
    Assert-Candidate11Test -Condition (@(Get-ChildItem -LiteralPath (Join-Path $successWork 'tested-candidate') -Directory -ErrorAction Stop).Count -eq 11) -Message 'Happy path did not preserve all tested candidate trees outside MODS.'
    $successReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $successEvidence '99-result.json') | ConvertFrom-Json
    Assert-Candidate11Test -Condition (
        [int]$successReceipt.SchemaVersion -eq 2 -and
        [bool]$successReceipt.Passed -and
        [bool]$successReceipt.RestoreAllExact -and
        [bool]$successReceipt.RuntimeBinding.Passed -and
        [int]$successReceipt.RuntimeBinding.CandidatePreflight.AssemblyCount -eq 5 -and
        [string]$successReceipt.CatalogSha256Role -match 'not a self-referential final pass gate'
    ) -Message 'Happy-path formal result receipt is incomplete.'
    Assert-Candidate11Test -Condition (Test-Path -LiteralPath (Join-Path $successEvidence '41-installed-runtime-post-smoke.json') -PathType Leaf) -Message 'Happy path did not persist the post-smoke installed Runtime receipt.'
    $successOutput = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $successEvidence '30-smoke-output.txt')
    Assert-Candidate11Test -Condition ($successOutput -match 'FAKE-CANDIDATE11-SMOKE-OBSERVED=11') -Message 'Happy-path child smoke output was not captured.'

    $failedSmoke = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '02')
    $failedEvidence = Join-Path $failedSmoke.Root 'E'
    $failedMessage = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $failedSmoke.CandidateRoot `
            -ProductCatalogPath $failedSmoke.CatalogPath `
            -DolocPersistentRoot $failedSmoke.PersistentRoot `
            -GameDirectory $failedSmoke.GameDir `
            -FormalEvidenceRoot $failedEvidence `
            -Id 'smoke-failure-case' `
            -SmokePath $failedSmoke.SmokePath `
            -SmokeArguments @('-PersistentRoot',$failedSmoke.PersistentRoot,'-CatalogPath',$failedSmoke.CatalogPath,'-GameDir',$failedSmoke.GameDir,'-Fail') `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest `
            -SkipSmokeContractForTest
    }
    catch {
        $failedMessage = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($failedMessage -match 'failed') -Message 'Non-zero child smoke did not fail the transaction.'
    Assert-Candidate11OriginalsRestored -Fixture $failedSmoke -Label 'Smoke failure'
    $failedReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $failedEvidence '99-result.json') | ConvertFrom-Json
    $failedSmokeExitCode = if ($null -ne $failedReceipt.Smoke -and $null -ne $failedReceipt.Smoke.PSObject.Properties['ExitCode']) { [int]$failedReceipt.Smoke.ExitCode } else { -1 }
    Assert-Candidate11Test -Condition (-not [bool]$failedReceipt.Passed -and $failedSmokeExitCode -eq 23 -and [bool]$failedReceipt.RestoreAllExact) -Message ('Smoke-failure receipt did not preserve exit/restoration facts: ' + ($failedReceipt | ConvertTo-Json -Depth 8 -Compress))

    $drift = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '03')
    $driftEvidence = Join-Path $drift.Root 'E'
    $driftMessage = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $drift.CandidateRoot `
            -ProductCatalogPath $drift.CatalogPath `
            -DolocPersistentRoot $drift.PersistentRoot `
            -GameDirectory $drift.GameDir `
            -FormalEvidenceRoot $driftEvidence `
            -Id 'post-smoke-drift-case' `
            -SmokePath $drift.SmokePath `
            -SmokeArguments @('-PersistentRoot',$drift.PersistentRoot,'-CatalogPath',$drift.CatalogPath,'-GameDir',$drift.GameDir,'-Drift') `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest `
            -SkipSmokeContractForTest
    }
    catch {
        $driftMessage = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($driftMessage -match 'drifted') -Message ("Post-smoke byte drift did not fail the transaction. Actual='$driftMessage'.")
    Assert-Candidate11OriginalsRestored -Fixture $drift -Label 'Post-smoke drift'
    $driftWork = Join-Path (Join-Path $drift.PersistentRoot '.dtmapi-candidate11-transactions') 'post-smoke-drift-case'
    $driftPreserved = Join-Path $driftWork 'tested-candidate\Fixture_Official_01\foreign-after-smoke.txt'
    Assert-Candidate11Test -Condition (Test-Path -LiteralPath $driftPreserved -PathType Leaf) -Message 'Post-smoke unknown bytes were not preserved outside MODS.'
    Assert-Candidate11Test -Condition ((Get-Content -Raw -LiteralPath $driftPreserved) -ceq 'preserve-this-drift') -Message 'Preserved post-smoke drift bytes changed.'
    $driftReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $driftEvidence '99-result.json') | ConvertFrom-Json
    Assert-Candidate11Test -Condition (-not [bool]$driftReceipt.PostSmokeAllMatched -and [bool]$driftReceipt.RestoreAllExact) -Message 'Post-smoke drift receipt did not separate byte failure from exact restoration.'

    $extra = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '04')
    [System.IO.Directory]::CreateDirectory((Join-Path $extra.CandidateRoot 'Unexpected-Package')) | Out-Null
    $extraError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $extra.CatalogPath -Root $extra.CandidateRoot
    }
    catch {
        $extraError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($extraError -match 'not exact') -Message 'Unexpected top-level candidate package did not fail closed.'
    Assert-Candidate11OriginalsRestored -Fixture $extra -Label 'Unexpected package preflight'

    $ads = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '05')
    $adsFile = Join-Path $ads.CandidateRoot 'Fixture-Package-01\candidate-byte-sentinel.txt'
    Set-Content -LiteralPath $adsFile -Stream 'candidate11-forbidden' -Value 'foreign-stream' -Encoding UTF8
    $adsError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $ads.CatalogPath -Root $ads.CandidateRoot
    }
    catch {
        $adsError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($adsError -match 'alternate data stream') -Message 'Candidate ADS did not fail closed.'
    Assert-Candidate11OriginalsRestored -Fixture $ads -Label 'ADS preflight'

    $runtimeManifestMismatch = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '06')
    $runtimeManifestMismatchPath = Join-Path $runtimeManifestMismatch.RuntimePackageRoot 'Content\DTMAPI\release-manifest.json'
    $runtimeManifestMismatchJson = Get-Content -Raw -Encoding UTF8 -LiteralPath $runtimeManifestMismatchPath | ConvertFrom-Json
    $runtimeManifestMismatchJson.IncludedAssemblies[0].Sha256 = (('0' * 64) -join '')
    Write-Candidate11TestJson -Path $runtimeManifestMismatchPath -Value $runtimeManifestMismatchJson
    $runtimeManifestMismatchError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $runtimeManifestMismatch.CatalogPath -Root $runtimeManifestMismatch.CandidateRoot
    }
    catch {
        $runtimeManifestMismatchError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($runtimeManifestMismatchError -match 'receipt does not match payload bytes') -Message 'Runtime release-manifest/DLL SHA mismatch did not fail preflight.'
    Assert-Candidate11OriginalsRestored -Fixture $runtimeManifestMismatch -Label 'Runtime manifest mismatch preflight'

    $runtimeDrift = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '07')
    $runtimeDriftEvidence = Join-Path $runtimeDrift.Root 'E'
    $runtimeDriftMessage = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $runtimeDrift.CandidateRoot `
            -ProductCatalogPath $runtimeDrift.CatalogPath `
            -DolocPersistentRoot $runtimeDrift.PersistentRoot `
            -GameDirectory $runtimeDrift.GameDir `
            -FormalEvidenceRoot $runtimeDriftEvidence `
            -Id 'installed-runtime-drift-case' `
            -SmokePath $runtimeDrift.SmokePath `
            -SmokeArguments @('-PersistentRoot',$runtimeDrift.PersistentRoot,'-CatalogPath',$runtimeDrift.CatalogPath,'-GameDir',$runtimeDrift.GameDir,'-RuntimeDrift') `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest `
            -SkipSmokeContractForTest
    }
    catch {
        $runtimeDriftMessage = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($runtimeDriftMessage -match 'installed Runtime cross-check failed') -Message 'Post-smoke installed Runtime byte drift did not fail the transaction.'
    Assert-Candidate11OriginalsRestored -Fixture $runtimeDrift -Label 'Installed Runtime drift'
    $runtimeDriftReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $runtimeDriftEvidence '99-result.json') | ConvertFrom-Json
    Assert-Candidate11Test -Condition (
        -not [bool]$runtimeDriftReceipt.Passed -and
        -not [bool]$runtimeDriftReceipt.RuntimeBinding.Passed -and
        -not [bool]$runtimeDriftReceipt.RuntimeBinding.InstalledPostSmoke.Passed -and
        [bool]$runtimeDriftReceipt.RuntimeBinding.CandidateSourceUnchanged -and
        [bool]$runtimeDriftReceipt.RestoreAllExact
    ) -Message 'Installed Runtime drift receipt did not preserve Runtime failure and exact product restoration facts.'

    $missingRuntime = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '08')
    [System.IO.Directory]::Move($missingRuntime.RuntimePackageRoot, (Join-Path $missingRuntime.Root 'runtime-held-outside-candidate'))
    $missingRuntimeError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $missingRuntime.CatalogPath -Root $missingRuntime.CandidateRoot
    }
    catch {
        $missingRuntimeError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($missingRuntimeError -match 'missing=DTMAPI') -Message "Candidate root without the Runtime package did not fail the exact top-level inventory gate. Actual=$missingRuntimeError"
    Assert-Candidate11OriginalsRestored -Fixture $missingRuntime -Label 'Missing Runtime preflight'

    if (-not $Quiet) {
        Write-Host 'Candidate11 source transaction tests: OK (required Runtime+exact11 binding, child-smoke, smoke-failure restore, product/runtime drift, inventory/ADS preflight)'
    }
}
finally {
    $env:DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE = $oldTestMode
    $resolvedRoot = Get-Candidate11CanonicalPath -Path $resolvedTestRoot
    if ((Test-Candidate11SameOrChildPath -Child $resolvedRoot -Parent $resolvedManagedParent) -and
        (Split-Path -Leaf $resolvedRoot) -match '^[0-9a-f]{32}$' -and
        (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
    if ((Test-Path -LiteralPath $resolvedManagedParent -PathType Container) -and
        @(Get-ChildItem -LiteralPath $resolvedManagedParent -Force -ErrorAction Stop).Count -eq 0) {
        Remove-Item -LiteralPath $resolvedManagedParent -Force
    }
}
