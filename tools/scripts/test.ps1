param(
    [string] $Configuration = 'Release',
    [string] $TestTempRoot = '',
    [switch] $KeepFailedTestTemp
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($TestTempRoot)) {
    $TestTempRoot = Join-Path $repo 'tmp\test-runs'
}
$env:DTMAPI_TEST_TEMP_ROOT = [System.IO.Path]::GetFullPath($TestTempRoot)
$env:DTMAPI_KEEP_FAILED_TEST_TEMP = if ($KeepFailedTestTemp) { '1' } else { '0' }
Write-Host "Managed test session root: $($env:DTMAPI_TEST_TEMP_ROOT)"

function Clear-CompletedTestSessions {
    $manifestPath = Join-Path $repo 'tmp\test-artifact-cleanup-latest.json'
    & "$PSScriptRoot\cleanup-test-artifacts.ps1" -TestRoot $env:DTMAPI_TEST_TEMP_ROOT -Apply -ManifestPath $manifestPath
    if (-not $?) {
        throw "Managed test-session cleanup failed. See '$manifestPath'."
    }
}

& "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
$dotnet = Get-DotNetExe -RepoRoot $repo
& $dotnet run --project (Join-Path $repo 'tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj') -c $Configuration --no-build
$unitTestExitCode = $LASTEXITCODE
Clear-CompletedTestSessions
if ($unitTestExitCode -ne 0) {
    exit $unitTestExitCode
}

& $dotnet run --project (Join-Path $repo 'tests\DTMAPI.QaUnitTests\DTMAPI.QaUnitTests.csproj') -c $Configuration --no-build
$qaUnitTestExitCode = $LASTEXITCODE
Clear-CompletedTestSessions
if ($qaUnitTestExitCode -ne 0) {
    exit $qaUnitTestExitCode
}

& $dotnet run --project (Join-Path $repo 'tests\DTMAPI.InstallDoctor.Tests\DTMAPI.InstallDoctor.Tests.csproj') -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& "$PSScriptRoot\build-player-doctor.ps1" -Configuration $Configuration
if (-not $?) {
    exit 1
}
& "$PSScriptRoot\test-player-doctor-portable.ps1" -Configuration $Configuration
if (-not $?) {
    exit 1
}

$packageTestBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$packageTestRoot = [System.IO.Path]::GetFullPath((Join-Path $packageTestBase ('DTMAPI Player Package 中文 ' + [Guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $packageTestRoot -Parent $packageTestBase)) {
    throw "Runtime package test root escaped system temp: $packageTestRoot"
}
try {
    & "$PSScriptRoot\build-release-workshop-packages.ps1" -Configuration $Configuration -SkipBuild -RuntimeOnly -OutputRoot $packageTestRoot
    if (-not $?) { exit 1 }
    & "$PSScriptRoot\test-player-doctor-packaged-entrypoints.ps1" `
        -Configuration $Configuration `
        -PackageRoot (Join-Path $packageTestRoot 'DTMAPI')
    if (-not $?) { exit 1 }
}
finally {
    if (Test-Path -LiteralPath $packageTestRoot) {
        Remove-Item -LiteralPath $packageTestRoot -Recurse -Force
    }
}

& "$PSScriptRoot\test-runtime-evidence-retention.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\build-evidence-retention-allowlist.ps1" -Check
if (-not $?) {
    exit 1
}

$productCatalogChecker = Join-Path $PSScriptRoot 'check-product-catalog.ps1'
$releaseContractChecker = Join-Path $PSScriptRoot 'check-release-contract.ps1'
$ownershipMatrix = Join-Path $PSScriptRoot 'test-player-runtime-only-uninstall.ps1'
$installerInvalidTargetMatrix = Join-Path $PSScriptRoot 'test-installer-invalid-target-failure.ps1'
$installTransactionMatrix = Join-Path $PSScriptRoot 'test-developer-official-local-install-transaction.ps1'
$runtimeUpgradeTransactionMatrix = Join-Path $PSScriptRoot 'test-runtime-upgrade-transaction.ps1'
$candidate11SourceTransactionHelper = Join-Path $PSScriptRoot 'candidate11-source-transaction.ps1'
$candidate11SourceTransactionTest = Join-Path $PSScriptRoot 'test-candidate11-source-transaction.ps1'
$workshopDownloadMarkerTest = Join-Path $PSScriptRoot 'test-workshop-download-markers.ps1'
$reverseBaselinePathSafety = Join-Path $PSScriptRoot 'reverse-baseline-path-safety.ps1'
$reverseBaselineCapture = Join-Path $PSScriptRoot 'capture-doloctown-reverse-baseline.ps1'
$reverseBaselinePathSafetyTest = Join-Path $PSScriptRoot 'test-reverse-baseline-path-safety.ps1'
$steamAppManifestIdentity = Join-Path $PSScriptRoot 'steam-appmanifest-identity.ps1'
$steamAppManifestIdentityTest = Join-Path $PSScriptRoot 'test-steam-appmanifest-identity.ps1'
$portableReverseCaptureRoot = Join-Path $repo 'tools\portable-reverse-capture'
$portableReverseCaptureBuilder = Join-Path $PSScriptRoot 'build-portable-reverse-capture-package.ps1'
$portableReverseCapturePathSafetyTest = Join-Path $PSScriptRoot 'test-portable-reverse-capture-path-safety.ps1'
Test-DtmApiWindowsPowerShellSyntax -Paths @(
    $productCatalogChecker,
    $releaseContractChecker,
    $ownershipMatrix,
    $installerInvalidTargetMatrix,
    $installTransactionMatrix,
    $runtimeUpgradeTransactionMatrix,
    $candidate11SourceTransactionHelper,
    $candidate11SourceTransactionTest,
    $workshopDownloadMarkerTest,
    $reverseBaselinePathSafety,
    $reverseBaselineCapture,
    $reverseBaselinePathSafetyTest,
    $steamAppManifestIdentity,
    $steamAppManifestIdentityTest,
    $portableReverseCaptureBuilder,
    $portableReverseCapturePathSafetyTest,
    (Join-Path $portableReverseCaptureRoot 'common.ps1'),
    (Join-Path $portableReverseCaptureRoot 'reverse-baseline-path-safety.ps1'),
    (Join-Path $portableReverseCaptureRoot 'run-doloctown-full-capture.ps1'),
    (Join-Path $PSScriptRoot 'uninstall-dtmapi.ps1'),
    (Join-Path $PSScriptRoot 'install-to-game.ps1'),
    (Join-Path $PSScriptRoot 'check-dtmapi-status.ps1'),
    (Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
    (Join-Path $PSScriptRoot 'dump-governance.ps1'),
    (Join-Path $PSScriptRoot 'release-common.ps1'),
    (Join-Path $PSScriptRoot 'author-sdk-release-common.ps1'),
    (Join-Path $PSScriptRoot 'build-author-sdk.ps1'),
    (Join-Path $PSScriptRoot 'check-author-sdk-release.ps1'),
    (Join-Path $PSScriptRoot 'test-author-sdk-portable.ps1'),
    (Join-Path $PSScriptRoot 'build-player-doctor.ps1'),
    (Join-Path $PSScriptRoot 'check-player-doctor-release.ps1'),
    (Join-Path $PSScriptRoot 'test-player-doctor-portable.ps1'),
    (Join-Path $PSScriptRoot 'test-player-doctor-packaged-entrypoints.ps1'),
    (Join-Path $PSScriptRoot 'cleanup-test-artifacts.ps1'),
    (Join-Path $PSScriptRoot 'check-test-artifact-governance.ps1'),
    (Join-Path $PSScriptRoot 'check-batch4-qa-semantic-boundary.ps1'),
    (Join-Path $PSScriptRoot 'test-batch4-qa-semantic-inventory.ps1'),
    (Join-Path $PSScriptRoot 'run-batch5-gc-ladder.ps1'),
    (Join-Path $PSScriptRoot 'test-batch5-gc-ladder.ps1'),
    (Join-Path $PSScriptRoot 'batch6-autofishing-runtime-transaction.ps1'),
    (Join-Path $PSScriptRoot 'run-batch6-autofishing-gc-ladder.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-autofishing-gc-ladder.ps1'),
    (Join-Path $PSScriptRoot 'run-batch6-autofishing-behavior-matrix.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-autofishing-behavior-matrix.ps1'),
    (Join-Path $PSScriptRoot 'run-batch6-autofishing-manager-lifecycle.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-autofishing-manager-lifecycle.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-autofishing-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-oneactioncomplete-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-oneactioncomplete-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-actionspeed-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-actionspeed-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-fishbreedingassistant-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-fishbreedingassistant-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-animalhusbandryprogress-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-animalhusbandryprogress-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'run-batch5-no-demand-profile.ps1'),
    (Join-Path $PSScriptRoot 'test-batch5-no-demand-profile.ps1'),
    (Join-Path $PSScriptRoot 'test-noqa-deadline.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-phase0-baseline.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-phase0-contract.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-g2-ownership-receipt.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-g2-runtime-matrix-receipt.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-g2-advanced-synthetic.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-g2-runtime-matrix-receipt.ps1'),
    (Join-Path $PSScriptRoot 'test-synthetic-retained-abi.ps1'),
    (Join-Path $PSScriptRoot 'test-retained-autofishing-abi.ps1')
) -AllowCoreFallback

$windowsPowerShell = Get-DtmApiPowerShellHost -RequireWindowsPowerShell
& $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $reverseBaselinePathSafetyTest -TestRoot $env:DTMAPI_TEST_TEMP_ROOT
$reverseBaselinePathSafetyExitCode = $LASTEXITCODE
if ($reverseBaselinePathSafetyExitCode -ne 0) {
    exit $reverseBaselinePathSafetyExitCode
}

& $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $steamAppManifestIdentityTest -TestRoot $env:DTMAPI_TEST_TEMP_ROOT
$steamAppManifestIdentityExitCode = $LASTEXITCODE
if ($steamAppManifestIdentityExitCode -ne 0) {
    exit $steamAppManifestIdentityExitCode
}

& $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $portableReverseCapturePathSafetyTest -TestRoot $env:DTMAPI_TEST_TEMP_ROOT
$portableReverseCapturePathSafetyExitCode = $LASTEXITCODE
if ($portableReverseCapturePathSafetyExitCode -ne 0) {
    exit $portableReverseCapturePathSafetyExitCode
}

& $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $workshopDownloadMarkerTest -TestRoot $env:DTMAPI_TEST_TEMP_ROOT
$workshopDownloadMarkerExitCode = $LASTEXITCODE
if ($workshopDownloadMarkerExitCode -ne 0) {
    exit $workshopDownloadMarkerExitCode
}

if ($Configuration -eq 'Release') {
    & "$PSScriptRoot\build-author-sdk.ps1" -Configuration Release
    if (-not $?) {
        exit 1
    }

    $previousSelfContainedExe = $env:DTMAPI_AUTHOR_SELF_CONTAINED_EXE
    try {
        $env:DTMAPI_AUTHOR_SELF_CONTAINED_EXE = Join-Path $repo 'dist\author-sdk\DTMAPI-Author-SDK-0.1.0-win-x64\dtmapi-author.exe'
        & $dotnet run --project (Join-Path $repo 'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj') -c $Configuration --no-build
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
    finally {
        $env:DTMAPI_AUTHOR_SELF_CONTAINED_EXE = $previousSelfContainedExe
    }

    & "$PSScriptRoot\test-author-sdk-portable.ps1" -PackagePath (Join-Path $repo 'dist\author-sdk\DTMAPI-Author-SDK-0.1.0-win-x64.zip')
    if (-not $?) {
        exit 1
    }
}
else {
    & $dotnet run --project (Join-Path $repo 'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj') -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

& $ownershipMatrix -Configuration $Configuration
if (-not $?) {
    exit 1
}

& $installerInvalidTargetMatrix -Configuration $Configuration
if (-not $?) {
    exit 1
}

& $runtimeUpgradeTransactionMatrix -Configuration $Configuration
if (-not $?) {
    exit 1
}

& $installTransactionMatrix -Configuration $Configuration
if (-not $?) {
    exit 1
}

$candidate11Pwsh = Get-Command pwsh.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $candidate11Pwsh -or $candidate11Pwsh.Version.Major -lt 7) {
    throw 'Candidate11 source transaction tests require PowerShell 7+ (pwsh.exe); Windows PowerShell 5.1 remains a syntax-validation host only.'
}
& $candidate11Pwsh.Source -NoProfile -ExecutionPolicy Bypass -File $candidate11SourceTransactionTest
$candidate11TestExitCode = $LASTEXITCODE
if ($candidate11TestExitCode -ne 0) {
    exit $candidate11TestExitCode
}

& $productCatalogChecker
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-phase0-contract.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-oneactioncomplete-advanced-product.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-actionspeed-advanced-product.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-fishbreedingassistant-advanced-product.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-animalhusbandryprogress-advanced-product.ps1"
if (-not $?) {
    exit 1
}

$g2ContractPath = Join-Path $repo 'tools\release\contracts\batch6-g2-advanced-synthetic-contract.json'
$g2Contract = Get-Content -LiteralPath $g2ContractPath -Raw -Encoding UTF8 | ConvertFrom-Json
$g2ImplementationState = [string]$g2Contract.phaseState.g2Implementation
if ($g2ImplementationState.Equals('passed', [StringComparison]::Ordinal)) {
    & "$PSScriptRoot\test-batch6-g2-advanced-synthetic.ps1"
}
elseif ($g2ImplementationState.Equals('in-progress', [StringComparison]::Ordinal)) {
    Write-Warning 'Batch 6 G2 is in-progress: the full development suite runs the candidate gate, but this result is NON-RELEASE / BLOCKED and cannot satisfy the permanent G2 gate.'
    & "$PSScriptRoot\test-batch6-g2-advanced-synthetic.ps1" -AllowInProgress
}
else {
    Write-Error "Unsupported Batch 6 G2 implementation state in full test driver: $g2ImplementationState"
    exit 1
}
if (-not $?) {
    exit 1
}

$authorSdkReleaseContractRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp\batch2-autofishing-release-contract'))
$repoTempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))
if (-not (Test-DtmApiPathIsSameOrChild -Child $authorSdkReleaseContractRoot -Parent $repoTempRoot) -or
    $authorSdkReleaseContractRoot.TrimEnd([char[]]@('\', '/')) -eq $repoTempRoot.TrimEnd([char[]]@('\', '/'))) {
    throw "Batch 2 Author SDK release-contract root escaped its dedicated repository temp boundary: $authorSdkReleaseContractRoot"
}
$authorSdkPrimaryRoot = Join-Path $authorSdkReleaseContractRoot 'primary'
$authorSdkRepeatRoot = Join-Path $authorSdkReleaseContractRoot 'repeat'
$sharedAuthorSdkRoot = Join-Path $repo 'dist\author-sdk'
$sharedAuthorSdkExe = Join-Path $sharedAuthorSdkRoot 'DTMAPI-Author-SDK-0.1.0-win-x64\dtmapi-author.exe'
if (Test-Path -LiteralPath $authorSdkReleaseContractRoot) {
    Remove-Item -LiteralPath $authorSdkReleaseContractRoot -Recurse -Force
}

$releaseContractPassed = $false
try {
    if ($Configuration -ne 'Release') {
        throw 'The managed Advanced product release contract is Release-only; run tools/scripts/test.ps1 -Configuration Release.'
    }
    if (-not (Test-Path -LiteralPath $sharedAuthorSdkExe -PathType Leaf)) {
        throw "The Release run must reuse its single Author SDK build for all Advanced product package checks: $sharedAuthorSdkExe"
    }
    $advancedReleaseProducts = @((Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json).products | Where-Object {
        $codeModKindProperty = $_.PSObject.Properties['codeModKind']
        $null -ne $codeModKindProperty -and [string]$codeModKindProperty.Value -ceq 'Advanced'
    })
    foreach ($advancedProduct in $advancedReleaseProducts) {
        $advancedCatalogId = [string]$advancedProduct.catalogId
        & "$PSScriptRoot\build-batch6-advanced-product.ps1" -CatalogId $advancedCatalogId -Configuration Release -OutputRoot (Join-Path $authorSdkPrimaryRoot $advancedCatalogId) -AuthorSdkRoot $sharedAuthorSdkRoot
        if (-not $?) { throw "Primary $advancedCatalogId Author SDK release-contract build failed." }
        & "$PSScriptRoot\build-batch6-advanced-product.ps1" -CatalogId $advancedCatalogId -Configuration Release -OutputRoot (Join-Path $authorSdkRepeatRoot $advancedCatalogId) -AuthorSdkRoot $sharedAuthorSdkRoot
        if (-not $?) { throw "Repeat $advancedCatalogId Author SDK release-contract build failed." }
    }

    & $releaseContractChecker -Configuration $Configuration `
        -AuthorSdkArtifactRoot $authorSdkPrimaryRoot `
        -AuthorSdkRepeatArtifactRoot $authorSdkRepeatRoot
    $releaseContractPassed = $?
}
finally {
    if (Test-Path -LiteralPath $authorSdkReleaseContractRoot) {
        Remove-Item -LiteralPath $authorSdkReleaseContractRoot -Recurse -Force
    }
}
if (-not $releaseContractPassed) {
    exit 1
}

& "$PSScriptRoot\check-batch4-qa-semantic-boundary.ps1" -Configuration $Configuration
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch4-qa-semantic-inventory.ps1" -Configuration $Configuration
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch5-gc-ladder.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-autofishing-gc-ladder.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-autofishing-behavior-matrix.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch6-autofishing-manager-lifecycle.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-batch5-no-demand-profile.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-noqa-deadline.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-synthetic-retained-abi.ps1" -Configuration $Configuration -NoBuild
if (-not $?) {
    exit 1
}

$privateAbiValues = @(
    $env:DTMAPI_RETAINED_BASELINE_ABSTRACTIONS_DLL,
    $env:DTMAPI_RETAINED_AUTOFISHING_DLL,
    $env:DTMAPI_RETAINED_PUBLIC_WORKSHOP_ROOT
)
$privateAbiConfigured = @($privateAbiValues | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
if ($privateAbiConfigured -ne 0 -and $privateAbiConfigured -ne $privateAbiValues.Count) {
    throw 'Private retained-ABI consistency test requires all of DTMAPI_RETAINED_BASELINE_ABSTRACTIONS_DLL, DTMAPI_RETAINED_AUTOFISHING_DLL, and DTMAPI_RETAINED_PUBLIC_WORKSHOP_ROOT.'
}
if ($privateAbiConfigured -eq $privateAbiValues.Count) {
    & "$PSScriptRoot\test-retained-autofishing-abi.ps1" `
        -BaselineAbstractionsDll $env:DTMAPI_RETAINED_BASELINE_ABSTRACTIONS_DLL `
        -CandidateAbstractionsDll (Join-Path $repo "src\DTMAPI.Abstractions\bin\$Configuration\netstandard2.0\DTMAPI.Abstractions.dll") `
        -AutoFishingDll $env:DTMAPI_RETAINED_AUTOFISHING_DLL `
        -RetainedPublicWorkshopRoot $env:DTMAPI_RETAINED_PUBLIC_WORKSHOP_ROOT `
        -ReportPath (Join-Path $repo 'reports\private-retained-abi-latest.json') `
        -Configuration $Configuration `
        -NoBuild
    if (-not $?) { exit 1 }
}
else {
    Write-Host 'Private retained-ABI consistency test skipped: explicit artifact environment variables were not provided.'
}

& "$PSScriptRoot\check-test-artifact-governance.ps1" -RunCleanupFixture
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\check-doc-governance.ps1" -Quiet
exit $LASTEXITCODE
