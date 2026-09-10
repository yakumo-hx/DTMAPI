param(
    [string] $Configuration = 'Release'
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$checkerPath = Join-Path $PSScriptRoot 'check-batch4-qa-semantic-boundary.ps1'
$catalogCheckerPath = Join-Path $PSScriptRoot 'check-product-catalog.ps1'
$inventoryPath = Join-Path $repo 'tools\release\batch4-production-qa-semantic-inventory.json'
$testRoot = if ([string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    Join-Path $repo 'tmp\test-runs'
}
else {
    $env:DTMAPI_TEST_TEMP_ROOT
}
$testRoot = [System.IO.Path]::GetFullPath($testRoot)
$sessionRoot = [System.IO.Path]::GetFullPath((Join-Path $testRoot ('batch4-semantic-inventory-' + [Guid]::NewGuid().ToString('N'))))
if (-not $sessionRoot.StartsWith($testRoot.TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Semantic inventory test root escaped the managed test root: $sessionRoot"
}
New-Item -ItemType Directory -Force -Path $sessionRoot | Out-Null

$repoTestRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'tmp\test-runs'))
$sourceFixtureRoot = [System.IO.Path]::GetFullPath((Join-Path $repoTestRoot ('batch4-semantic-source-fixture-' + [Guid]::NewGuid().ToString('N'))))
$repoPrefix = $repo.TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
if (-not $sourceFixtureRoot.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Semantic source-fixture root escaped the repository: $sourceFixtureRoot"
}
$sourceFixtureRelativeRoot = $sourceFixtureRoot.Substring($repoPrefix.Length).Replace('\', '/')
$sourceFixturePath = Join-Path $sourceFixtureRoot 'InjectedBoundaryViolation.cs'
$sourceFixtureScriptPath = Join-Path $sourceFixtureRoot 'InjectedRunnerViolation.ps1'
$sourceFixtureScriptRelativePath = $sourceFixtureRelativeRoot + '/InjectedRunnerViolation.ps1'
New-Item -ItemType Directory -Force -Path $sourceFixtureRoot | Out-Null
@'
namespace DTMAPI.SemanticInventory.MetaFixture
{
    internal static class InjectedBoundaryViolation
    {
        internal const string Behavior = "InjectedBehaviorToken";
        internal const string Symbol = "InjectedPlayerSymbol";
        internal const string Consumer = "InjectedQaOnlyConsumer";
        internal const string LifecycleEnd = "InjectedLifecycleEnd";
        internal const string LifecycleBegin = "InjectedLifecycleBegin";

        internal static void NeutralInputAliasProbe(dynamic runtime, dynamic input)
        {
            var spacedRecord = runtime . RecordInputFrame;
            var commentedRecord = runtime./*alias*/RecordInputFrame;
            input . ClearFrame();
            input./*alias*/ClearFrame();
        }
    }
}
'@ | Set-Content -LiteralPath $sourceFixturePath -Encoding UTF8
'InjectedRunnerBehaviorToken' | Set-Content -LiteralPath $sourceFixtureScriptPath -Encoding UTF8

$hostExecutable = (Get-Process -Id $PID).Path
$executedCaseNames = New-Object 'System.Collections.Generic.List[string]'
$executedCatalogProjectionCaseNames = New-Object 'System.Collections.Generic.List[string]'

function Assert-ExactCaseReceiptSet(
    [string] $Label,
    [object[]] $Actual,
    [object[]] $Expected
) {
    $actualValues = @($Actual)
    $expectedValues = @($Expected)
    if ($actualValues.Count -eq 0) { throw "$Label actual receipt contains no case names." }
    if ($expectedValues.Count -eq 0) { throw "$Label expected receipt contains no case names." }

    $actualNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($value in $actualValues) {
        $name = [string]$value
        if ([string]::IsNullOrWhiteSpace($name)) { throw "$Label actual receipt contains an empty case name." }
        if (-not $actualNames.Add($name)) { throw "$Label actual receipt contains duplicate case name: $name" }
    }
    $expectedNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($value in $expectedValues) {
        $name = [string]$value
        if ([string]::IsNullOrWhiteSpace($name)) { throw "$Label expected receipt contains an empty case name." }
        if (-not $expectedNames.Add($name)) { throw "$Label expected receipt contains duplicate case name: $name" }
    }
    if (-not $actualNames.SetEquals($expectedNames)) {
        $actualText = @($actualNames | Sort-Object) -join ', '
        $expectedText = @($expectedNames | Sort-Object) -join ', '
        throw "$Label receipt mismatch. Expected [$expectedText], executed [$actualText]."
    }
}

function Test-ReceiptSetValidation {
    $invalidCases = @(
        [pscustomobject]@{ Name = 'actual-empty-name'; Actual = @('case-a', ' '); Expected = @('case-a') },
        [pscustomobject]@{ Name = 'expected-empty-name'; Actual = @('case-a'); Expected = @('case-a', '') },
        [pscustomobject]@{ Name = 'actual-duplicate-name'; Actual = @('case-a', 'case-a'); Expected = @('case-a') },
        [pscustomobject]@{ Name = 'expected-duplicate-name'; Actual = @('case-a'); Expected = @('case-a', 'case-a') },
        [pscustomobject]@{ Name = 'actual-empty-set'; Actual = @(); Expected = @('case-a') },
        [pscustomobject]@{ Name = 'expected-empty-set'; Actual = @('case-a'); Expected = @() }
    )
    foreach ($case in $invalidCases) {
        $rejected = $false
        try { Assert-ExactCaseReceiptSet $case.Name @($case.Actual) @($case.Expected) }
        catch { $rejected = $true }
        if (-not $rejected) { throw "Receipt-set validation unexpectedly accepted invalid case: $($case.Name)" }
    }
    return $invalidCases.Count
}

$receiptSetValidationCaseCount = Test-ReceiptSetValidation

function Invoke-NegativeInventoryCase(
    [string] $Name,
    [scriptblock] $Mutate,
    [string[]] $ExpectedFailures
) {
    $inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
    & $Mutate $inventory
    $casePath = Join-Path $sessionRoot ($Name + '.json')
    $inventory | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $casePath -Encoding UTF8

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $hostExecutable
    $startInfo.Arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "' + $checkerPath + '" -Configuration "' + $Configuration + '" -InventoryPath "' + $casePath + '"'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "Could not start semantic inventory checker for case: $Name"
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $output = $stdoutTask.Result + [Environment]::NewLine + $stderrTask.Result
        $exitCode = $process.ExitCode
    }
    finally {
        $process.Dispose()
    }
    if ($exitCode -eq 0) {
        throw "Semantic inventory meta-negative case unexpectedly passed: $Name"
    }
    # Windows PowerShell 5.1 may hard-wrap native stderr inside identifiers. Compare without
    # whitespace so those transport-only wraps cannot hide the expected semantic failure.
    $normalizedOutput = [regex]::Replace($output, '\s+', '')
    foreach ($expectedFailure in @($ExpectedFailures)) {
        $normalizedExpectedFailure = [regex]::Replace($expectedFailure, '\s+', '')
        if ($normalizedOutput.IndexOf($normalizedExpectedFailure, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Semantic inventory meta-negative case '$Name' missed expected failure '$expectedFailure'. Output: $output"
        }
    }
    $script:executedCaseNames.Add($Name) | Out-Null
}

function Invoke-NegativeCatalogProjectionCase([string] $Name) {
    $catalog = Get-Content -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') -Raw | ConvertFrom-Json
    $expectedCount = [int]$catalog.playerRuntimePackageInvariant.semanticBoundary.productionSourceFiles
    $catalog.playerRuntimePackageInvariant.semanticBoundary.productionSourceFiles =
        $expectedCount + 1
    $casePath = Join-Path $sessionRoot ($Name + '.json')
    $catalog | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $casePath -Encoding UTF8

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $hostExecutable
    $startInfo.Arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "' + $catalogCheckerPath + '" -Quiet -CatalogPath "' + $casePath + '"'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) { throw 'Could not start Catalog semantic-projection negative case.' }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $output = $stdoutTask.Result + [Environment]::NewLine + $stderrTask.Result
        $exitCode = $process.ExitCode
    }
    finally {
        $process.Dispose()
    }
    if ($exitCode -eq 0) { throw 'Catalog semantic-projection stale-metric case unexpectedly passed.' }
    $normalizedOutput = [regex]::Replace($output, '\s+', '')
    $expected = [regex]::Replace(("Batch 4 production source file count: expected '$expectedCount', got '$($expectedCount + 1)'"), '\s+', '')
    if ($normalizedOutput.IndexOf($expected, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Catalog semantic-projection stale-metric case missed the dynamic mismatch. Output: $output"
    }
    $script:executedCatalogProjectionCaseNames.Add($Name) | Out-Null
}

try {
    Invoke-NegativeInventoryCase 'stale-schema' {
        param($inventory)
        $inventory.schemaVersion = 4
    } 'exactly 5'

    Invoke-NegativeInventoryCase 'contract-and-negative-sample-coverage' {
        param($inventory)
        $inventory.sourceGate.consumerContracts = @()
        $inventory.sourceGate.g9NegativeSamples = @($inventory.sourceGate.g9NegativeSamples | Where-Object {
            $_.contractId -ne 'qa-title-driver-not-owned-by-player'
        })
        $sample = @($inventory.sourceGate.g9NegativeSamples | Where-Object { $_.name -eq 'debug-console-player-evidence-policy-regression' })[0]
        $sample.sample = $sample.sample.Replace('summary.txt', 'summary-removed')
        $sample.expectedPatterns = @($sample.expectedPatterns | Where-Object { $_ -ne 'screenshotSearchText' })

        $undeclaredSample = @($inventory.sourceGate.g9NegativeSamples | Where-Object { $_.name -eq 'core-synthetic-input-fixture-policy-regression' })[0]
        $undeclaredSample.expectedPatterns = @($undeclaredSample.expectedPatterns + 'NeverDeclaredPattern')
        $undeclaredSample.sample += ' NeverDeclaredPattern'

        @($inventory.sourceGate.g9NegativeSamples | Where-Object { $_.name -eq 'animal-viewer-player-evidence-policy-regression' })[0].expectedPatterns = @()
    } @(
        'consumer gate has no contracts',
        'has no negative sample',
        'did not match expected pattern',
        'no negative-sample coverage',
        'not declared',
        'any-pattern matching is forbidden'
    )

    Invoke-NegativeInventoryCase 'duplicate-contract-id' {
        param($inventory)
        $contracts = @($inventory.sourceGate.forbiddenBehaviorContracts)
        $inventory.sourceGate.forbiddenBehaviorContracts = @($contracts + $contracts[0])
    } 'id is duplicated'

    Invoke-NegativeInventoryCase 'renamed-helper-new-file-scope-regression' {
        param($inventory)
        @($inventory.sourceGate.forbiddenBehaviorContracts | Where-Object { $_.id -eq 'debug-console-player-evidence-policy' })[0].scopePaths = @(
            'src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs'
        )
        @($inventory.sourceGate.forbiddenBehaviorContracts | Where-Object { $_.id -eq 'animal-viewer-player-evidence-policy' })[0].scopePaths = @(
            'src/DTMAPI.GameBridge.DolocTown/Compatibility/AnimalViewer/AnimalViewerService.cs',
            'src/DTMAPI.GameBridge.DolocTown/Compatibility/AnimalViewer/AnimalViewerHookBridge.cs',
            'src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs'
        )
        @($inventory.sourceGate.forbiddenBehaviorContracts | Where-Object { $_.id -eq 'equipment-slots-player-evidence-policy' })[0].scopePaths = @(
            'src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs'
        )
        @($inventory.sourceGate.forbiddenBehaviorContracts | Where-Object { $_.id -eq 'gamebridge-player-screenshot-adapter' })[0].scopePaths = @(
            'src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs'
        )
    } @(
        'debug-console-renamed-helper-in-new-player-file-regression does not violate the path boundary',
        'animal-viewer-renamed-helper-in-new-player-file-regression does not violate the path boundary',
        'equipment-slots-renamed-helper-in-new-player-file-regression does not violate the path boundary',
        'gamebridge-renamed-screenshot-helper-in-new-player-file-regression does not violate the path boundary'
    )

    Invoke-NegativeInventoryCase 'lifecycle-negative-order-and-readback-regression' {
        param($inventory)
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-action-speed-receipt-published-after-first-mutation' })[0].sample =
            'originalSlotItem = read.Invoke(inventory, new object[] { slot }); publishRestoreReceipt?.Invoke(inventory, originalSlotItem); take.Invoke(inventory, new object[] { slot });'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-action-speed-void-restore-assumed-success' })[0].sample =
            'private bool RestoreSmokeQuickSlot(Type dolocApi, object inventory, int slot, object originalSlotItem) { object? restored = read.Invoke(inventory, new object[] { slot }); return restored != null; } if (!RestoreSmokeQuickSlot(dolocApi, inventory, slot, originalSlotItem)) throw cleanupError;'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-animal-screenshot-before-visible-receipt' })[0].sample =
            'if (g4AnimalProgressRowCount == 0) return G4FixtureStepResult.Failed("native panel alone cannot satisfy the staged-QA positive gate"); ReadAnimalProductObservationSummary(); renderSummary.IndexOf("state=visible", StringComparison.OrdinalIgnoreCase); ObserveScreenshotForFixture(screenshotPath, ref g4AnimalScreenshotPath, ref requestedAt);'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-mine-world-readback-before-remove' })[0].sample =
            'private bool TryRemoveTransientEquipmentForFixture(object room, object equipment) { if (m.Name != "GetEquipment") return false; removeEquipment.Invoke(room, args); object? remaining = getEquipment.Invoke(room, new[] { cell }); if (ReferenceEquals(remaining, equipment)) return false; return true; }'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-mine-receipt-cleared-before-removal-confirmed' })[0].sample =
            'removed = TryRemoveTransientEquipmentForFixture(room, mine); if (!removed) throw cleanupError; newContentPendingMine = null;'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-staged-debug-console-omitted-from-strict-gate' })[0].sample =
            '$strictPlayerInputGate = [bool]$RequireExternalPlayerInputGate -or [bool]$AssertNoQaUiEvidence -or [bool]$qaG4DebugConsoleEnabled; if ($strictPlayerInputGate -and $Attempts -ne 1) { throw ''exactly one''; } $externalPlayerInputAttempts.Add([ordered]@{}); $y1Attempt = @($externalPlayerInputAttempts.ToArray());'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-staged-debug-console-postmessage-fallback' })[0].sample =
            '$sentInput = [DtmApiSmokeInput]::SendKeyboardKey($VirtualKey, $HoldMilliseconds); $postMessageFallbackRequested = -not ([bool]$RequireExternalPlayerInputGate -or [bool]$AssertNoQaUiEvidence -or [bool]$qaG4DebugConsoleEnabled -or [bool]$qaG4EquipmentSlotsObservationEnabled -or [bool]$moreEquipmentSlotsTransitionRequested -or [bool]$Batch6AutoFishingPilot -or [bool]$Batch6AutoFishingManagerLifecycle -or [bool]$AutoPressOneActionMenuKey); $postMessageFallbackUsed = $false; if ($sentInput -and $postMessageFallbackRequested) { $postMessageFallbackUsed = [bool]$postReceipt.Accepted; } SendInputSucceeded = [bool]$sentInput; PostMessageFallbackUsed = [bool]$postMessageFallbackUsed;'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-debug-console-screenshot-immediately-verified' })[0].sample =
            'if (g4DebugEvidenceCaptured) { if (debugConsoleApi.IsOpen) return G4FixtureStepResult.Pending("EVIDENCE_CAPTURED_WAITING_ESCAPE"); return G4FixtureStepResult.Verified("closeReceipt=product-api-closed"); } G4FixtureStepResult screenshot = ObserveScreenshotForFixture(screenshotPath, ref path, ref requestedAt); g4DebugEvidenceCaptured = true; return G4FixtureStepResult.Pending("EVIDENCE_CAPTURED_WAITING_ESCAPE");'
        @($inventory.sourceGate.lifecycleNegativeSamples | Where-Object { $_.name -eq 'former-runner-escape-before-debug-console-evidence-marker' })[0].sample =
            'Wait-ForLogLineAfterOffset -LogPath $logPath -Offset ([int64]$holdAttempt[0].LogOffset) -Pattern ''EVIDENCE_CAPTURED_WAITING_ESCAPE''; Invoke-DebugConsoleSmokeKey -Label ''YHoldCleanupEscape''; Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern ''Smoke.DebugConsoleHotkey = verified'';'
    } @(
        'former-action-speed-receipt-published-after-first-mutation',
        'former-action-speed-void-restore-assumed-success',
        'former-animal-screenshot-before-visible-receipt',
        'former-mine-world-readback-before-remove',
        'former-mine-receipt-cleared-before-removal-confirmed',
        'former-staged-debug-console-omitted-from-strict-gate',
        'former-staged-debug-console-postmessage-fallback',
        'former-debug-console-screenshot-immediately-verified',
        'former-runner-escape-before-debug-console-evidence-marker'
    )

    Invoke-NegativeInventoryCase 'canonical-selector-redirection-rejected' {
        param($inventory)
        $inventory.sourceGate.publishedProductSourceProjection.catalogPath = 'tmp/redirected-product-catalog.json'
        $inventory.sourceGate.publishedProductSourceProjection.role = 'DeveloperFixture'
        $inventory.sourceGate.publishedProductSourceProjection.distributionState = 'PrivateOnly'
        $inventory.sourceGate.publishedProductSourceProjection.expectedCount = 0
        $inventory.sourceGate.publishedProductSourceProjection.behaviorContractId = 'redirected-contract'
    } @(
        "catalogPath must be exactly 'tools/release/dtmapi-product-catalog.json'",
        "role must be exactly 'PublishedProduct'",
        "distributionState must be exactly 'PublicWorkshop'",
        'expectedCount must be exactly 11',
        "behaviorContractId must be exactly 'published-product-player-evidence-policy'"
    )

    Invoke-NegativeInventoryCase 'production-root-closure-rejected' {
        param($inventory)
        $inventory.sourceGate.productionRoots = @($inventory.sourceGate.productionRoots | Where-Object {
            -not ([string]$_).Equals('src/DTMAPI.Core', [StringComparison]::OrdinalIgnoreCase)
        })
    } 'Production source gate is missing canonical source root: src/DTMAPI.Core'

    Invoke-NegativeInventoryCase 'consumer-root-closure-rejected' {
        param($inventory)
        $inventory.sourceGate.consumerSearchRoots = @($inventory.sourceGate.consumerSearchRoots | Where-Object {
            -not ([string]$_).Equals('tests', [StringComparison]::OrdinalIgnoreCase)
        })
    } 'Consumer source gate is missing canonical source root: tests'

    Invoke-NegativeInventoryCase 'published-symbol-scan-unconditional' {
        param($inventory)
        $contract = @($inventory.sourceGate.symbolOwnershipContracts | Where-Object { $_.id -eq 'qa-evidence-symbols-not-owned-by-player' })[0]
        $contract | Add-Member -NotePropertyName includePublishedProductSources -NotePropertyValue $false -Force
        $contract.patterns = @($contract.patterns + 'namespace\s+DTMAPI\.Zoom')
        $inventory.sourceGate.g9NegativeSamples = @($inventory.sourceGate.g9NegativeSamples + [pscustomobject]@{
            name = 'meta-published-symbol-scan-unconditional'
            gate = 'symbol'
            contractId = 'qa-evidence-symbols-not-owned-by-player'
            samplePath = 'products/first-party/Zoom/src/ModEntry.cs'
            sample = 'namespace DTMAPI.Zoom'
            expectedPatterns = @('namespace\s+DTMAPI\.Zoom')
        })
    } 'Player symbol ownership violation qa-evidence-symbols-not-owned-by-player matched namespace\s+DTMAPI\.Zoom in products/first-party/Zoom/src/ModEntry.cs'

    Invoke-NegativeInventoryCase 'public-root-consumer-whitelist-rejected' {
        param($inventory)
        $contract = @($inventory.sourceGate.consumerContracts | Where-Object { $_.id -eq 'ui-smoke-policy-consumers' })[0]
        $contract.allowedRoots = @(
            $contract.allowedRoots +
            'products/first-party/Zoom/src' +
            'products/first-party/AutoFishing/src/qa'
        )
    } @(
        'Consumer contract ui-smoke-policy-consumers allowedRoot intersects published-product productionSourceRoot: products/first-party/Zoom/src <-> products/first-party/Zoom/src',
        'Consumer contract ui-smoke-policy-consumers allowedRoot intersects published-product productionSourceRoot: products/first-party/AutoFishing/src/qa <-> products/first-party/AutoFishing/src'
    )

    Invoke-NegativeInventoryCase 'neutral-input-alias-bypass-rejected' {
        param($inventory)
        $contract = @($inventory.sourceGate.consumerContracts | Where-Object { $_.id -eq 'neutral-input-injection-consumers' })[0]
        $contract.patterns = @('\.RecordInputFrame\s*\(', '\.Input\.ClearFrame\s*\(')
    } @(
        'expects pattern not declared by consumer contract neutral-input-injection-consumers: \.(?:(?>\s+)|(?>/\*[\s\S]*?\*/)|(?>//[^\r\n]*(?:\r\n|\r|\n)))*RecordInputFrame\b',
        'expects pattern not declared by consumer contract neutral-input-injection-consumers: \.(?:(?>\s+)|(?>/\*[\s\S]*?\*/)|(?>//[^\r\n]*(?:\r\n|\r|\n)))*ClearFrame\b'
    )

    Invoke-NegativeInventoryCase 'actual-source-scanners' {
        param($inventory)
        $fixturePath = $sourceFixtureRelativeRoot + '/InjectedBoundaryViolation.cs'
        $neutralInputContract = @($inventory.sourceGate.consumerContracts | Where-Object { $_.id -eq 'neutral-input-injection-consumers' })[0]
        $neutralInputPatterns = @($neutralInputContract.patterns)
        $inventory.sourceGate.forbiddenBehaviorContracts = @($inventory.sourceGate.forbiddenBehaviorContracts + [pscustomobject]@{
            id = 'meta-injected-forbidden-behavior'
            scopePaths = @($sourceFixtureRelativeRoot)
            forbiddenPatterns = @('InjectedBehaviorToken')
        }, [pscustomobject]@{
            id = 'meta-injected-runner-forbidden-behavior'
            scopePaths = @($sourceFixtureScriptRelativePath)
            forbiddenPatterns = @('InjectedRunnerBehaviorToken')
        })
        $inventory.sourceGate.symbolOwnershipContracts = @($inventory.sourceGate.symbolOwnershipContracts + @(
            [pscustomobject]@{
                id = 'meta-injected-symbol'
                forbiddenRoots = @($sourceFixtureRelativeRoot)
                patterns = @('InjectedPlayerSymbol')
            },
            [pscustomobject]@{
                id = 'meta-injected-neutral-input-aliases'
                forbiddenRoots = @($sourceFixtureRelativeRoot)
                patterns = @($neutralInputPatterns)
            }
        ))
        $inventory.sourceGate.consumerContracts = @($inventory.sourceGate.consumerContracts + [pscustomobject]@{
            id = 'meta-injected-consumer'
            patterns = @('namespace DTMAPI\.Abstractions')
            allowedRoots = @('src/DTMAPI.GameBridge.DolocTown.QA')
        })
        $inventory.sourceGate.lifecycleContracts = @($inventory.sourceGate.lifecycleContracts + [pscustomobject]@{
            path = $fixturePath
            requiredPatterns = @(
                [pscustomobject]@{ pattern = 'InjectedLifecycleBegin'; minimumOccurrences = 1 },
                [pscustomobject]@{ pattern = 'InjectedLifecycleEnd'; minimumOccurrences = 1 }
            )
            orderedTokens = @('InjectedLifecycleBegin', 'InjectedLifecycleEnd')
        })
        $inventory.sourceGate.g9NegativeSamples = @($inventory.sourceGate.g9NegativeSamples + @(
            [pscustomobject]@{
                name = 'meta-injected-forbidden-behavior-sample'
                gate = 'behavior'
                contractId = 'meta-injected-forbidden-behavior'
                samplePath = $fixturePath
                sample = 'InjectedBehaviorToken'
                expectedPatterns = @('InjectedBehaviorToken')
            },
            [pscustomobject]@{
                name = 'meta-injected-runner-forbidden-behavior-sample'
                gate = 'behavior'
                contractId = 'meta-injected-runner-forbidden-behavior'
                samplePath = $sourceFixtureScriptRelativePath
                sample = 'InjectedRunnerBehaviorToken'
                expectedPatterns = @('InjectedRunnerBehaviorToken')
            },
            [pscustomobject]@{
                name = 'meta-injected-symbol-sample'
                gate = 'symbol'
                contractId = 'meta-injected-symbol'
                samplePath = $fixturePath
                sample = 'InjectedPlayerSymbol'
                expectedPatterns = @('InjectedPlayerSymbol')
            },
            [pscustomobject]@{
                name = 'meta-injected-neutral-input-alias-sample'
                gate = 'symbol'
                contractId = 'meta-injected-neutral-input-aliases'
                samplePath = $fixturePath
                sample = 'runtime . RecordInputFrame; runtime./*alias*/RecordInputFrame; input . ClearFrame(); input./*alias*/ClearFrame();'
                expectedPatterns = @($neutralInputPatterns)
            },
            [pscustomobject]@{
                name = 'meta-injected-consumer-sample'
                gate = 'consumer'
                contractId = 'meta-injected-consumer'
                samplePath = 'src/DTMAPI.Abstractions/Manifest.cs'
                sample = 'namespace DTMAPI.Abstractions'
                expectedPatterns = @('namespace DTMAPI\.Abstractions')
            }
        ))
    } @(
        'Forbidden behavior meta-injected-forbidden-behavior matched',
        'Forbidden behavior meta-injected-runner-forbidden-behavior matched InjectedRunnerBehaviorToken',
        'Player symbol ownership violation meta-injected-symbol',
        'Player symbol ownership violation meta-injected-neutral-input-aliases matched \.(?:(?>\s+)|(?>/\*[\s\S]*?\*/)|(?>//[^\r\n]*(?:\r\n|\r|\n)))*RecordInputFrame\b',
        'Player symbol ownership violation meta-injected-neutral-input-aliases matched \.(?:(?>\s+)|(?>/\*[\s\S]*?\*/)|(?>//[^\r\n]*(?:\r\n|\r|\n)))*ClearFrame\b',
        'Consumer ownership violation meta-injected-consumer',
        'does not preserve required token order: InjectedLifecycleBegin -> InjectedLifecycleEnd'
    )

    Invoke-NegativeCatalogProjectionCase 'stale-catalog-semantic-projection'

    $canonicalInventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
    Assert-ExactCaseReceiptSet 'Semantic inventory meta-negative' @($executedCaseNames.ToArray()) @($canonicalInventory.sourceGate.metaNegativeCaseNames)
    Assert-ExactCaseReceiptSet 'Catalog projection meta-negative' @($executedCatalogProjectionCaseNames.ToArray()) @($canonicalInventory.sourceGate.catalogProjectionNegativeCaseNames)
}
finally {
    if (Test-Path -LiteralPath $sourceFixtureRoot -PathType Container) {
        $resolvedSourceFixtureRoot = [System.IO.Path]::GetFullPath($sourceFixtureRoot)
        if (-not $resolvedSourceFixtureRoot.StartsWith($repoTestRoot.TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove semantic source-fixture path outside the managed repository test root: $resolvedSourceFixtureRoot"
        }
        Remove-Item -LiteralPath $resolvedSourceFixtureRoot -Recurse -Force
    }
    if (Test-Path -LiteralPath $sessionRoot -PathType Container) {
        $resolvedSessionRoot = [System.IO.Path]::GetFullPath($sessionRoot)
        if (-not $resolvedSessionRoot.StartsWith($testRoot.TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove semantic inventory test path outside the managed root: $resolvedSessionRoot"
        }
        Remove-Item -LiteralPath $resolvedSessionRoot -Recurse -Force
    }
}

Write-Host "Batch 4 G9 semantic inventory meta-negative tests OK: cases=$($executedCaseNames.Count) [$($executedCaseNames -join ', ')]; catalogProjectionCases=$($executedCatalogProjectionCaseNames.Count) [$($executedCatalogProjectionCaseNames -join ', ')]; receiptSetValidationCases=$receiptSetValidationCaseCount."
