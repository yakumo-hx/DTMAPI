param(
    [string] $Configuration = 'Release',
    [string] $TestTempRoot = '',
    [switch] $KeepFailedTestTemp,
    [string] $StartAt = 'FromStart',
    [string] $Stage = 'All',
    [switch] $List
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\test-common.ps1"
. "$PSScriptRoot\release-common.ps1"
. "$PSScriptRoot\author-sdk-release-common.ps1"
$ErrorActionPreference = 'Stop'
$selection = Get-DtmApiReleaseTestSelection -Stage $Stage -StartAt $StartAt
if ($List) {
    $selection | ConvertTo-Json -Depth 3
    return
}
Assert-DtmApiFullTestEnvironment
$repo = Get-RepoRoot
# Fail before building or generating packages. The same process boundary is
# checked again by cleanup; do not launch another test/game during this run.
$activeProcesses = @(Get-DtmApiActiveTestProcess -IncludeGame)
if ($activeProcesses.Count -gt 0) {
    throw "Release tests require an idle game/test environment. Active PIDs: $($activeProcesses.ProcessId -join ', ')."
}
if ($selection.CompleteRun -or $selection.Stages -contains 'ScriptContracts') {
    & "$PSScriptRoot\build-evidence-retention-allowlist.ps1" -Check
    if (-not $?) { exit 1 }
}
if (-not $selection.CompleteRun) {
    Write-Warning 'DIAGNOSTIC ONLY: this selection never counts as a complete Release PASS. Rebuild changed inputs before using stages that consume existing outputs.'
}
$currentAuthorSdkPackageName = "DTMAPI-Author-SDK-$(Get-AuthorSdkReleaseVersion -RepoRoot $repo)-win-x64"
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

$dotnet = if ($selection.Stages -contains 'BuildAndUnit' -or $selection.Stages -contains 'AuthorSdk') {
    Get-DotNetExe -RepoRoot $repo
}
if ($selection.Stages -contains 'BuildAndUnit') {
& "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
if (-not $?) { exit 1 }
& "$PSScriptRoot\test-unit.ps1" -Configuration $Configuration -NoBuild
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
}

if ($selection.Stages -contains 'RuntimeInstaller') {
$packageTestBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$packageTestUnicodeChinese = -join @([char]0x4E2D, [char]0x6587)
$packageTestRoot = [System.IO.Path]::GetFullPath((Join-Path $packageTestBase ('DTMAPI Player Package ' + $packageTestUnicodeChinese + ' ' + [Guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $packageTestRoot -Parent $packageTestBase)) {
    throw "Runtime package test root escaped system temp: $packageTestRoot"
}
try {
    & "$PSScriptRoot\test-runtime-build-source.ps1"
    if (-not $?) { exit 1 }
    & "$PSScriptRoot\build-release-workshop-packages.ps1" -Configuration $Configuration -RuntimeOnly -OutputRoot $packageTestRoot
    if (-not $?) { exit 1 }
    & "$PSScriptRoot\test-runtime-candidate-published-info-boundary.ps1" `
        -RuntimePackageRoot (Join-Path $packageTestRoot 'DTMAPI')
    if (-not $?) { exit 1 }
    & "$PSScriptRoot\test-runtime-workshop-installer-061.ps1" `
        -PackageRoot (Join-Path $packageTestRoot 'DTMAPI')
    if (-not $?) { exit 1 }
}
finally {
    if (Test-Path -LiteralPath $packageTestRoot) {
        Remove-Item -LiteralPath $packageTestRoot -Recurse -Force
    }
}
}

if ($selection.Stages -contains 'ScriptContracts') {
& "$PSScriptRoot\test-runtime-evidence-retention.ps1"
if (-not $?) {
    exit 1
}

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
$chestLocatorNativeTrace = Join-Path $PSScriptRoot 'test-dtmapi-060-chestlocator-native-trace.ps1'
$debugConsoleNativeTrace = Join-Path $PSScriptRoot 'test-dtmapi-060-debugconsole-native-trace.ps1'
$autoFishingNativeTrace = Join-Path $PSScriptRoot 'test-dtmapi-060-autofishing-native-trace.ps1'
$runtimeFloorCompatibility = Join-Path $PSScriptRoot 'test-dtmapi-060-runtime-floor-compatibility.ps1'
$advancedReferenceFixtureBuilder = Join-Path $PSScriptRoot 'build-advanced-reference-game-fixture.ps1'
$advancedReferenceFixtureTest = Join-Path $PSScriptRoot 'test-advanced-reference-game-fixture.ps1'
$releaseArtifactSetTest = Join-Path $PSScriptRoot 'test-dtmapi-060-release-artifact-set.ps1'
$portableReverseCaptureRoot = Join-Path $repo 'tools\portable-reverse-capture'
$portableReverseCaptureBuilder = Join-Path $PSScriptRoot 'build-portable-reverse-capture-package.ps1'
$portableReverseCapturePathSafetyTest = Join-Path $PSScriptRoot 'test-portable-reverse-capture-path-safety.ps1'
if ($selection.Stages -contains 'ScriptContracts') {
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
    $chestLocatorNativeTrace,
    $debugConsoleNativeTrace,
    $autoFishingNativeTrace,
    $runtimeFloorCompatibility,
    $advancedReferenceFixtureBuilder,
    $advancedReferenceFixtureTest,
    (Join-Path $PSScriptRoot 'test-runtime-candidate-published-info-boundary.ps1'),
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
    (Join-Path $PSScriptRoot 'test-author-sdk-required-reference.ps1'),
    (Join-Path $PSScriptRoot 'build-release-workshop-packages.ps1'),
    (Join-Path $PSScriptRoot 'test-runtime-workshop-installer-061.ps1'),
    (Join-Path $PSScriptRoot 'cleanup-test-artifacts.ps1'),
    (Join-Path $PSScriptRoot 'check-test-artifact-governance.ps1'),
    (Join-Path $PSScriptRoot 'check-batch4-qa-semantic-boundary.ps1'),
    (Join-Path $PSScriptRoot 'test-batch4-qa-semantic-inventory.ps1'),
    (Join-Path $PSScriptRoot 'run-moresaves-fixed12-acceptance.ps1'),
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
    $releaseArtifactSetTest,
    (Join-Path $PSScriptRoot 'build-batch6-autofishing-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-oneactioncomplete-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-oneactioncomplete-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-actionspeed-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-actionspeed-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-fishbreedingassistant-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-fishbreedingassistant-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-animalhusbandryprogress-advanced-product.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-animalhusbandryprogress-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-moresaves-advanced-pilot.ps1'),
    (Join-Path $PSScriptRoot 'run-batch5-no-demand-profile.ps1'),
    (Join-Path $PSScriptRoot 'test-batch5-no-demand-profile.ps1'),
    (Join-Path $PSScriptRoot 'test-noqa-deadline.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-phase0-baseline.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-phase0-contract.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-g2-ownership-receipt.ps1'),
    (Join-Path $PSScriptRoot 'build-batch6-g2-runtime-matrix-receipt.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-g2-advanced-synthetic.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-g2-authority-paths.ps1'),
    (Join-Path $PSScriptRoot 'test-batch6-g2-runtime-matrix-receipt.ps1'),
    (Join-Path $PSScriptRoot 'test-synthetic-retained-abi.ps1'),
    (Join-Path $PSScriptRoot 'resolve-retained-abi-inputs.ps1'),
    (Join-Path $PSScriptRoot 'test-retained-abi-input-resolution.ps1'),
    (Join-Path $PSScriptRoot 'test-retained-release-abi.ps1'),
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

}

if ($selection.Stages -contains 'AuthorSdk') {
$previousAuthorDotnet = $env:DTMAPI_AUTHOR_DOTNET
try {
    # In-process SDK tests must select the same supported host as their runner.
    # The distributed portable test explicitly clears this inherited override.
    $env:DTMAPI_AUTHOR_DOTNET = $dotnet
if ($Configuration -eq 'Release') {
    & "$PSScriptRoot\build-author-sdk.ps1" -Configuration Release
    if (-not $?) {
        exit 1
    }

    $previousSelfContainedExe = $env:DTMAPI_AUTHOR_SELF_CONTAINED_EXE
    try {
        $env:DTMAPI_AUTHOR_SELF_CONTAINED_EXE = Join-Path $repo "dist\author-sdk\$currentAuthorSdkPackageName\dtmapi-author.exe"
        & $dotnet run --project (Join-Path $repo 'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj') -c $Configuration --no-build -- --require-exact-advanced-reference
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
        & "$PSScriptRoot\test-author-sdk-required-reference.ps1" `
            -Configuration $Configuration `
            -TestRoot $env:DTMAPI_TEST_TEMP_ROOT
        if (-not $?) {
            exit 1
        }
    }
    finally {
        $env:DTMAPI_AUTHOR_SELF_CONTAINED_EXE = $previousSelfContainedExe
    }

    & "$PSScriptRoot\test-author-sdk-portable.ps1" -PackagePath (Join-Path $repo "dist\author-sdk\$currentAuthorSdkPackageName.zip")
    if (-not $?) {
        exit 1
    }
}
else {
    & "$PSScriptRoot\prepare-author-sdk-compatibility.ps1"
    if (-not $?) { throw 'Frozen Author SDK compatibility preparation failed.' }
    & $dotnet run --project (Join-Path $repo 'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj') -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

}
finally {
    $env:DTMAPI_AUTHOR_DOTNET = $previousAuthorDotnet
}
}

if ($selection.Stages -contains 'ProductContracts') {
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
    if (-not $?) { exit 1 }
    & "$PSScriptRoot\test-batch6-g2-authority-paths.ps1" -TestRoot $env:DTMAPI_TEST_TEMP_ROOT
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

& $advancedReferenceFixtureTest
if (-not $?) {
    exit 1
}

& $releaseArtifactSetTest
if (-not $?) {
    exit 1
}

$authorSdkReleaseContractRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp\batch6-advanced-release-contract'))
$repoTempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))
if (-not (Test-DtmApiPathIsSameOrChild -Child $authorSdkReleaseContractRoot -Parent $repoTempRoot) -or
    $authorSdkReleaseContractRoot.TrimEnd([char[]]@('\', '/')) -eq $repoTempRoot.TrimEnd([char[]]@('\', '/'))) {
    throw "Batch 6 Advanced Author SDK release-contract root escaped its dedicated repository temp boundary: $authorSdkReleaseContractRoot"
}
$authorSdkPrimaryRoot = Join-Path $authorSdkReleaseContractRoot 'primary'
$authorSdkRepeatRoot = Join-Path $authorSdkReleaseContractRoot 'repeat'
$authorSdkReferenceFixtureRoot = Join-Path $authorSdkReleaseContractRoot 'reference-games'
$sharedAuthorSdkRoot = Join-Path $repo 'dist\author-sdk'
$sharedAuthorSdkExe = Join-Path $sharedAuthorSdkRoot "$currentAuthorSdkPackageName\dtmapi-author.exe"
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
    $releaseCatalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $advancedReleaseProducts = @(Get-DtmApiReleaseContractAdvancedProducts -Catalog $releaseCatalog)
    $advancedReleaseCatalogIds = @($advancedReleaseProducts | ForEach-Object { [string]$_.catalogId })
    if ($advancedReleaseCatalogIds.Count -eq 0) {
        throw 'The current-published Advanced ProductNative artifact selection must not be empty.'
    }
    $forbiddenReleaseProducts = @('strong-planting-gun', 'mine')
    $forbiddenSelectedProducts = @($advancedReleaseCatalogIds | Where-Object { $forbiddenReleaseProducts -ccontains $_ })
    if ($forbiddenSelectedProducts.Count -gt 0) {
        throw "The 0.6 Release contract must never invoke the Advanced builder for: $($forbiddenSelectedProducts -join ', ')."
    }
    $referenceGameRoots = @{}
    foreach ($advancedProduct in $advancedReleaseProducts) {
        $advancedCatalogId = [string]$advancedProduct.catalogId
        $referencePolicyId = [string]$advancedProduct.referencePolicyId
        if ([string]::IsNullOrWhiteSpace($referencePolicyId)) {
            throw "Advanced Catalog product '$advancedCatalogId' has no exact reference policy for the release-contract build."
        }
        if (-not $referenceGameRoots.ContainsKey($referencePolicyId)) {
            $policyFixtureRoot = Join-Path $authorSdkReferenceFixtureRoot $referencePolicyId
            & $advancedReferenceFixtureBuilder -PolicyId $referencePolicyId -OutputRoot $policyFixtureRoot
            if (-not $?) {
                throw "Exact Advanced reference fixture build failed for policy '$referencePolicyId'."
            }
            $referenceGameRoot = Join-Path $policyFixtureRoot 'steamapps\common\Doloc Town'
            Assert-DtmApiDolocTownGamePath -Path $referenceGameRoot -Source "Advanced release-contract policy '$referencePolicyId' fixture"
            $referenceGameRoots[$referencePolicyId] = $referenceGameRoot
        }
        $productReferenceGameRoot = [string]$referenceGameRoots[$referencePolicyId]
        & "$PSScriptRoot\build-batch6-advanced-product.ps1" -CatalogId $advancedCatalogId -GameDir $productReferenceGameRoot -Configuration Release -OutputRoot (Join-Path $authorSdkPrimaryRoot $advancedCatalogId) -AuthorSdkRoot $sharedAuthorSdkRoot
        if (-not $?) { throw "Primary $advancedCatalogId Author SDK release-contract build failed." }
        & "$PSScriptRoot\build-batch6-advanced-product.ps1" -CatalogId $advancedCatalogId -GameDir $productReferenceGameRoot -Configuration Release -OutputRoot (Join-Path $authorSdkRepeatRoot $advancedCatalogId) -AuthorSdkRoot $sharedAuthorSdkRoot
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

}

if ($selection.Stages -contains 'QaLifecycle') {
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

$autoFishingReleaseManagedRoot = [System.IO.Path]::GetFullPath((Join-Path $env:DTMAPI_TEST_TEMP_ROOT `
    ('release-autofishing-acceptance-' + [Guid]::NewGuid().ToString('N')))).TrimEnd([char]92, [char]47)
if (-not [string]::Equals((Split-Path -Parent $autoFishingReleaseManagedRoot),
    [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT).TrimEnd([char]92, [char]47),
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Release AutoFishing test root escaped the managed test root: $autoFishingReleaseManagedRoot"
}
if (Test-Path -LiteralPath $autoFishingReleaseManagedRoot) {
    throw "Release AutoFishing test root already exists: $autoFishingReleaseManagedRoot"
}
New-Item -ItemType Directory -Path $autoFishingReleaseManagedRoot -Force | Out-Null
$autoFishingReleaseOwnerToken = [Guid]::NewGuid().ToString('N')
$autoFishingReleaseOwnerMarker = Join-Path $autoFishingReleaseManagedRoot '.dtmapi-release-autofishing-test-owner'
$autoFishingReleaseOwnerToken | Set-Content -LiteralPath $autoFishingReleaseOwnerMarker -Encoding ASCII
$previousManagedTestRoot = $env:DTMAPI_TEST_TEMP_ROOT
try {
    $env:DTMAPI_TEST_TEMP_ROOT = $autoFishingReleaseManagedRoot
    & "$PSScriptRoot\test-batch6-autofishing-behavior-matrix.ps1"
    if (-not $?) {
        throw 'Batch 6 AutoFishing behavior-matrix tests failed.'
    }

    & "$PSScriptRoot\test-batch6-autofishing-manager-lifecycle.ps1"
    if (-not $?) {
        throw 'Batch 6 AutoFishing Manager lifecycle tests failed.'
    }

    $autoFishingReleaseLeaks = @(Get-ChildItem -LiteralPath $autoFishingReleaseManagedRoot -Force |
        Where-Object { $_.FullName -cne $autoFishingReleaseOwnerMarker })
    if ($autoFishingReleaseLeaks.Count -ne 0) {
        throw "Release AutoFishing tests leaked $($autoFishingReleaseLeaks.Count) child artifact(s) inside their dedicated managed root."
    }
}
finally {
    $env:DTMAPI_TEST_TEMP_ROOT = $previousManagedTestRoot
    $markerMatches = (Test-Path -LiteralPath $autoFishingReleaseOwnerMarker -PathType Leaf) -and
        [string]::Equals((Get-Content -Raw -LiteralPath $autoFishingReleaseOwnerMarker).Trim(), $autoFishingReleaseOwnerToken, [System.StringComparison]::Ordinal)
    $directManagedChild = [string]::Equals((Split-Path -Parent $autoFishingReleaseManagedRoot),
        [System.IO.Path]::GetFullPath($previousManagedTestRoot).TrimEnd([char]92, [char]47),
        [System.StringComparison]::OrdinalIgnoreCase)
    $ordinaryRoot = (Test-Path -LiteralPath $autoFishingReleaseManagedRoot -PathType Container) -and
        (((Get-Item -LiteralPath $autoFishingReleaseManagedRoot -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)
    $reparseDescendants = @(
        if ($ordinaryRoot) {
            Get-ChildItem -LiteralPath $autoFishingReleaseManagedRoot -Force -Recurse |
                Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 }
        }
    )
    if ($markerMatches -and $directManagedChild -and $ordinaryRoot -and $reparseDescendants.Count -eq 0) {
        Remove-Item -LiteralPath $autoFishingReleaseManagedRoot -Recurse -Force
    }
    elseif (Test-Path -LiteralPath $autoFishingReleaseManagedRoot) {
        throw "Release AutoFishing test cleanup refused an unowned or unsafe path: $autoFishingReleaseManagedRoot"
    }
}

& "$PSScriptRoot\test-batch5-no-demand-profile.ps1"
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-noqa-deadline.ps1"
if (-not $?) {
    exit 1
}

}

if ($selection.Stages -contains 'RetainedAbi') {
& "$PSScriptRoot\test-synthetic-retained-abi.ps1" -Configuration $Configuration -NoBuild
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-retained-abi-input-resolution.ps1" -TestRoot $env:DTMAPI_TEST_TEMP_ROOT
if (-not $?) {
    exit 1
}

& $runtimeFloorCompatibility
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\test-retained-release-abi.ps1" `
    -CandidateAbstractionsDll (Join-Path $repo "src\DTMAPI.Abstractions\bin\$Configuration\netstandard2.0\DTMAPI.Abstractions.dll") `
    -ReportPath (Join-Path $repo 'reports\retained-release-abi-latest.json') `
    -Configuration $Configuration `
    -TestRoot $env:DTMAPI_TEST_TEMP_ROOT `
    -NoBuild
if (-not $?) {
    exit 1
}

}

if ($selection.Stages -contains 'InstallerMatrices') {
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

# Candidate11/Local11 is a frozen 0.6 milestone transaction, not a current
# product-release invariant. Keep its helper, dedicated replay and syntax
# validation as historical audit tooling, but do not replay the complete
# nine-source/two-retained transaction in every current Release suite.
}

if ($selection.Stages -contains 'Governance') {
& "$PSScriptRoot\check-test-artifact-governance.ps1" -RunCleanupFixture
if (-not $?) {
    exit 1
}

& "$PSScriptRoot\check-doc-governance.ps1" -Quiet
if (-not $?) { exit 1 }
}
Write-Host $(if ($selection.CompleteRun) { 'Complete test sequence passed; release eligibility still follows the named release gates.' } else { 'Diagnostic selection passed; full Release acceptance is not established.' })
exit 0
