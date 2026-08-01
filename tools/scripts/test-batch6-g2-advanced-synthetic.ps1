param(
    [string] $ContractPath = '',
    [string] $GameRoot = '',
    [string] $OwnershipReceiptPath = '',
    [string] $RuntimeReceiptPath = '',
    [string] $RuntimeEvidenceMapPath = '',
    [switch] $AllowInProgress
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure([string] $message) { $script:failures.Add($message) }

function Normalize-Path([string] $path) {
    return ([string]$path).Trim().TrimStart([char]'\', [char]'/').Replace('\', '/').TrimEnd('/')
}

function Resolve-RepoFile([string] $value, [string] $defaultRelativePath) {
    $candidate = if ([string]::IsNullOrWhiteSpace($value)) { $defaultRelativePath } else { $value }
    if (-not [System.IO.Path]::IsPathRooted($candidate)) { $candidate = Join-Path $repo $candidate }
    return [System.IO.Path]::GetFullPath($candidate)
}

function Read-Utf8([string] $path) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Failure "Required file is missing: $path"
        return ''
    }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function Get-Sha256([string] $path) {
    $stream = [System.IO.File]::OpenRead($path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Assert-Exact([string] $label, [object] $actual, [string] $expected) {
    if ($null -eq $actual -or -not ([string]$actual).Equals($expected, [StringComparison]::Ordinal)) {
        Add-Failure "$label must be '$expected'; found '$actual'."
    }
}

function Assert-ExactNullableString([string] $label, [object] $actual, [object] $expected) {
    $actualIsNull = $null -eq $actual
    $expectedIsNull = $null -eq $expected
    if ($actualIsNull -or $expectedIsNull) {
        if ($actualIsNull -ne $expectedIsNull) {
            $actualDisplay = if ($actualIsNull) { '<null>' } else { [string]$actual }
            $expectedDisplay = if ($expectedIsNull) { '<null>' } else { [string]$expected }
            Add-Failure "$label must be '$expectedDisplay'; found '$actualDisplay'."
        }
        return
    }
    Assert-Exact $label $actual ([string]$expected)
}

function Assert-True([string] $label, [object] $actual) {
    if ($null -eq $actual -or -not ($actual -is [bool]) -or -not [bool]$actual) {
        Add-Failure "$label must be true; found '$actual'."
    }
}

function Assert-ExactSet([string] $label, [object[]] $actual, [object[]] $expected) {
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $expectedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($value in @($actual)) {
        if (-not $actualSet.Add([string]$value)) { Add-Failure "$label contains duplicate '$value'." }
    }
    foreach ($value in @($expected)) {
        if (-not $expectedSet.Add([string]$value)) { throw "$label expectation contains duplicate '$value'." }
    }
    $missing = @($expectedSet | Where-Object { -not $actualSet.Contains([string]$_) } | Sort-Object)
    $extra = @($actualSet | Where-Object { -not $expectedSet.Contains([string]$_) } | Sort-Object)
    if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
        Add-Failure "$label mismatch. Missing=[$($missing -join ', ')] Extra=[$($extra -join ', ')]."
    }
}

function Assert-Contains([string] $relativePath, [string[]] $tokens) {
    $text = Read-Utf8 (Join-Path $repo (Normalize-Path $relativePath))
    foreach ($token in @($tokens)) {
        if ($text.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
            Add-Failure "$relativePath is missing required token: $token"
        }
    }
}

function Invoke-GitLines([string[]] $arguments) {
    $previousErrorAction = $ErrorActionPreference
    $previousOutputEncoding = [Console]::OutputEncoding
    try {
        $ErrorActionPreference = 'Continue'
        [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
        $output = @(& git -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 2>$null)
        $exitCode = $LASTEXITCODE
    }
    finally {
        [Console]::OutputEncoding = $previousOutputEncoding
        $ErrorActionPreference = $previousErrorAction
    }
    if ($exitCode -ne 0) {
        try {
            $ErrorActionPreference = 'Continue'
            [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
            $details = @(& git -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 2>&1)
        }
        finally {
            [Console]::OutputEncoding = $previousOutputEncoding
            $ErrorActionPreference = $previousErrorAction
        }
        throw "git $($arguments -join ' ') failed: $($details -join [Environment]::NewLine)"
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Read-GitText([string] $commit, [string] $relativePath) {
    $lines = @(Invoke-GitLines @('show', ($commit + ':' + (Normalize-Path $relativePath))))
    if ($lines.Count -eq 0) { return '' }
    return ($lines -join "`n") + "`n"
}

function Test-GitCommand([string[]] $arguments) {
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & git -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 1>$null 2>$null
        return $LASTEXITCODE -eq 0
    }
    finally { $ErrorActionPreference = $previousErrorAction }
}

function Test-PathUnderRoot([string] $path, [string] $root) {
    $normalizedPath = Normalize-Path $path
    $normalizedRoot = Normalize-Path $root
    return $normalizedPath.Equals($normalizedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $normalizedPath.StartsWith($normalizedRoot + '/', [StringComparison]::OrdinalIgnoreCase)
}

function Get-ChangedPaths([string] $baselineCommit, [string] $targetCommit, [string[]] $roots, [bool] $useWorktree) {
    $arguments = New-Object System.Collections.Generic.List[string]
    foreach ($value in @('diff', '--name-only', '--no-renames', $baselineCommit)) { $arguments.Add($value) }
    if (-not $useWorktree) { $arguments.Add($targetCommit) }
    $arguments.Add('--')
    foreach ($root in $roots) { $arguments.Add((Normalize-Path $root)) }
    $paths = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in @(Invoke-GitLines $arguments.ToArray())) { [void]$paths.Add((Normalize-Path $path)) }
    if ($useWorktree) {
        $untrackedArguments = New-Object System.Collections.Generic.List[string]
        foreach ($value in @('ls-files', '--others', '--exclude-standard', '--')) { $untrackedArguments.Add($value) }
        foreach ($root in $roots) { $untrackedArguments.Add((Normalize-Path $root)) }
        foreach ($path in @(Invoke-GitLines $untrackedArguments.ToArray())) { [void]$paths.Add((Normalize-Path $path)) }
    }
    return @($paths | Sort-Object)
}

function Get-CommitChangedPaths([string] $parentCommit, [string] $commit) {
    $paths = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in @(Invoke-GitLines @('diff-tree', '--no-commit-id', '--name-only', '-r', '--no-renames', $parentCommit, $commit, '--'))) {
        [void]$paths.Add((Normalize-Path $path))
    }
    return @($paths | Sort-Object)
}

function Get-CSharpTestCaseText([string] $text, [string] $caseName) {
    $pattern = '(?m)^\s*(?:private|public|internal)\s+static\s+(?:async\s+)?[^\r\n(]+\s+(?<name>[A-Za-z0-9_]+)\s*\('
    $matches = [Text.RegularExpressions.Regex]::Matches($text, $pattern)
    for ($index = 0; $index -lt $matches.Count; $index++) {
        if (-not $matches[$index].Groups['name'].Value.Equals($caseName, [StringComparison]::Ordinal)) { continue }
        $end = if ($index + 1 -lt $matches.Count) { $matches[$index + 1].Index } else { $text.Length }
        return $text.Substring($matches[$index].Index, $end - $matches[$index].Index)
    }
    return ''
}

function Assert-TrackedAndClean([string] $relativePath) {
    if (-not (Test-GitCommand @('ls-files', '--error-unmatch', '--', (Normalize-Path $relativePath)))) {
        Add-Failure "Passed G2 authority must be tracked: $relativePath"
        return
    }
    $status = @(Invoke-GitLines @('status', '--porcelain', '--', (Normalize-Path $relativePath)))
    if ($status.Count -ne 0) { Add-Failure "Passed G2 authority must be clean and committed: $relativePath" }
}

function Compare-ReceiptMechanics([object] $receipt, [object] $candidate, [object] $audit) {
    Assert-ExactSet 'Ownership receipt root fields' @($receipt.PSObject.Properties.Name) @(
        'schemaVersion',
        'contractId',
        'architectureAuthority',
        'reviewAuthority',
        'targetKind',
        'baselineCommit',
        'implementationCommit',
        'worktreeHeadCommit',
        'sourceDiffSha256',
        'changedFileCount',
        'addedPhysicalLines',
        'deletedPhysicalLines',
        'mandatoryRuntimeProductNativeAddedPhysicalLines',
        'stableAbstractionsAddedPhysicalLines',
        'stableAbstractionsDeletedPhysicalLines',
        'files'
    )
    foreach ($property in @('schemaVersion', 'contractId', 'architectureAuthority', 'reviewAuthority', 'baselineCommit', 'implementationCommit', 'sourceDiffSha256', 'changedFileCount', 'addedPhysicalLines', 'deletedPhysicalLines')) {
        Assert-Exact "Ownership receipt $property" $receipt.$property ([string]$candidate.$property)
    }
    Assert-Exact 'Ownership receipt target kind' $receipt.targetKind 'committed-implementation'
    if ($null -ne $receipt.worktreeHeadCommit) { Add-Failure 'Committed ownership receipt worktreeHeadCommit must be null.' }
    Assert-ExactSet 'Ownership receipt changed files' @($receipt.files | ForEach-Object { [string]$_.path }) @($candidate.files | ForEach-Object { [string]$_.path })

    $productNativeAdded = 0
    $abstractionsAdded = 0
    $abstractionsDeleted = 0
    $receiptAdded = 0
    $receiptDeleted = 0
    foreach ($row in @($receipt.files)) {
        Assert-ExactSet "Ownership receipt row fields $($row.path)" @($row.PSObject.Properties.Name) @('path', 'ownership', 'addedPhysicalLines', 'deletedPhysicalLines', 'baselineBlob', 'targetBlob', 'diffSha256')
        $matches = @($candidate.files | Where-Object { ([string]$_.path).Equals([string]$row.path, [StringComparison]::Ordinal) })
        if ($matches.Count -ne 1) { continue }
        $candidateRow = $matches[0]
        foreach ($property in @('ownership', 'addedPhysicalLines', 'deletedPhysicalLines', 'baselineBlob', 'targetBlob', 'diffSha256')) {
            if (@('baselineBlob', 'targetBlob') -contains $property) {
                Assert-ExactNullableString "Ownership receipt $($row.path) $property" $row.$property $candidateRow.$property
            }
            else {
                Assert-Exact "Ownership receipt $($row.path) $property" $row.$property ([string]$candidateRow.$property)
            }
        }

        $ownership = [string]$row.ownership
        $path = [string]$row.path
        $receiptAdded += [int]$row.addedPhysicalLines
        $receiptDeleted += [int]$row.deletedPhysicalLines
        $isSynthetic = @($audit.syntheticProductRoots | Where-Object { Test-PathUnderRoot $path ([string]$_) }).Count -gt 0
        $isAbstractions = Test-PathUnderRoot $path ([string]$audit.stableAbstractionsRoot)
        $isSharedNative = Test-PathUnderRoot $path ([string]$audit.sharedNativeRoot)
        $isPlatform = @($audit.platformSourceRoots | Where-Object { Test-PathUnderRoot $path ([string]$_) }).Count -gt 0
        if ($isSynthetic -and $ownership -ne 'SyntheticProductNative') { Add-Failure "Synthetic fixture source must be classified SyntheticProductNative: $path" }
        elseif ($isAbstractions -and $ownership -ne 'StableAbstractions') { Add-Failure "Stable Abstractions source must be classified StableAbstractions: $path" }
        elseif ($isSharedNative -and $ownership -ne 'SharedNative') { Add-Failure "GameBridge source must be classified SharedNative: $path" }
        elseif ($isPlatform -and @('Platform', 'ProductNative') -notcontains $ownership) { Add-Failure "Platform-root source has an invalid audited classification '$ownership': $path" }
        elseif (-not $isSynthetic -and -not $isAbstractions -and -not $isSharedNative -and -not $isPlatform) { Add-Failure "Ownership receipt row escaped classified roots: $path" }

        $isMandatoryRuntime = @($audit.mandatoryRuntimeRoots | Where-Object { Test-PathUnderRoot $path ([string]$_) }).Count -gt 0
        if ($isMandatoryRuntime -and $ownership -eq 'ProductNative') { $productNativeAdded += [int]$row.addedPhysicalLines }
        if ($isAbstractions) {
            $abstractionsAdded += [int]$row.addedPhysicalLines
            $abstractionsDeleted += [int]$row.deletedPhysicalLines
        }
    }
    Assert-Exact 'Receipt row added-line sum' $receipt.addedPhysicalLines ([string]$receiptAdded)
    Assert-Exact 'Receipt row deleted-line sum' $receipt.deletedPhysicalLines ([string]$receiptDeleted)
    Assert-Exact 'Receipt row count' $receipt.changedFileCount ([string](@($receipt.files).Count))
    Assert-Exact 'Receipt Runtime ProductNative added physical lines' $receipt.mandatoryRuntimeProductNativeAddedPhysicalLines ([string]$productNativeAdded)
    Assert-Exact 'Receipt stable Abstractions added physical lines' $receipt.stableAbstractionsAddedPhysicalLines ([string]$abstractionsAdded)
    Assert-Exact 'Receipt stable Abstractions deleted physical lines' $receipt.stableAbstractionsDeletedPhysicalLines ([string]$abstractionsDeleted)
    if ($productNativeAdded -gt [int]$audit.mandatoryRuntimeProductNativeAddedPhysicalLinesMax) { Add-Failure "Mandatory Runtime ProductNative additions exceed G2 maximum: $productNativeAdded" }
    if ($abstractionsAdded -gt [int]$audit.stableAbstractionsAddedPhysicalLinesMax -or $abstractionsDeleted -gt [int]$audit.stableAbstractionsDeletedPhysicalLinesMax) {
        Add-Failure "Stable Abstractions changed during G2: added=$abstractionsAdded deleted=$abstractionsDeleted."
    }
}

$contractFullPath = Resolve-RepoFile $ContractPath 'tools\release\contracts\batch6-g2-advanced-synthetic-contract.json'
$contractText = Read-Utf8 $contractFullPath
try { $contract = $contractText | ConvertFrom-Json }
catch { Add-Failure "G2 contract is not parseable JSON: $($_.Exception.Message)"; $contract = $null }

$candidate = $null
$isPassed = $false
$isInProgress = $false
if ($null -ne $contract) {
    if ([int]$contract.schemaVersion -ne 2) { Add-Failure "G2 contract schemaVersion must be 2; found $($contract.schemaVersion)." }
    Assert-Exact 'G2 contract ID' $contract.contractId 'batch6-g2-advanced-synthetic-vertical-slice'
    Assert-Exact 'Phase 0 admission commit' $contract.phaseState.phase0AdmissionCommit '1239aa577d74e4f0c29131ba644e8a751eebbf5f'
    $g2State = [string]$contract.phaseState.g2Implementation
    $isPassed = $g2State.Equals('passed', [StringComparison]::Ordinal)
    $isInProgress = $g2State.Equals('in-progress', [StringComparison]::Ordinal)
    if ($AllowInProgress) {
        if (-not $isPassed -and -not $isInProgress) { Add-Failure "G2 implementation state must be in-progress or passed; found '$g2State'." }
    }
    elseif (-not $isPassed) {
        Add-Failure "G2 implementation state must be passed for the permanent gate; found '$g2State'. Use -AllowInProgress only for a NON-RELEASE development candidate."
    }
    if ($isInProgress) {
        if ($null -ne $contract.phaseState.g2ImplementationCommit) { Add-Failure 'In-progress G2 must not claim an immutable implementation commit.' }
        Assert-Exact 'G2 synthetic fixture state while in progress' $contract.phaseState.g2SyntheticFixture 'not-verified'
        Assert-Exact 'AutoFishing state during in-progress synthetic G2' $contract.phaseState.autoFishingPilot 'blocked'
    }
    if ($isPassed) {
        Assert-Exact 'G2 synthetic fixture state' $contract.phaseState.g2SyntheticFixture 'passed'
        if ([string]::IsNullOrWhiteSpace([string]$contract.phaseState.g2ImplementationCommit)) { Add-Failure 'Passed G2 must bind g2ImplementationCommit.' }
        Assert-Exact 'AutoFishing state after passed synthetic G2' $contract.phaseState.autoFishingPilot 'admitted-not-migrated'
    }
    Assert-Exact 'Other real-product state during synthetic G2' $contract.phaseState.otherRealProducts 'blocked'
    Assert-Exact 'Content Host state during synthetic G2' $contract.phaseState.contentHostG7 'blocked'
    Assert-Exact '0.5.5 release state during synthetic G2' $contract.phaseState.release055 'independently-blocked'
    Assert-Exact 'G2 architecture authority' $contract.authorities.architecture 'docs/architecture/batch6-managed-mod-identity-contract.md'
    Assert-Exact 'G2 prerequisite Review authority' $contract.authorities.review 'docs/reviews/code/2026/20260720-0004-batch6-g2-advanced-synthetic-vertical-slice-prerequisite.md'
    Assert-Exact 'G2 Runtime root-cause Review authority' $contract.authorities.rootCauseReview 'docs/reviews/code/2026/20260720-0005-batch6-g2-first-runtime-matrix-root-cause.md'
    Assert-Exact 'G2 Update authority' $contract.authorities.update 'docs/updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md'

    Assert-Exact 'Manifest type' $contract.manifestWire.type 'CodeMod'
    Assert-Exact 'Manifest discriminator' $contract.manifestWire.discriminator 'CodeModKind'
    Assert-ExactSet 'Manifest discriminator values' @($contract.manifestWire.values) @('Strict', 'Advanced')
    Assert-Exact 'Manifest omitted meaning' $contract.manifestWire.omittedValue 'Strict'
    Assert-True 'ContentPack discriminator rejection' (-not [bool]$contract.manifestWire.contentPackAllowsDiscriminator)
    Assert-Exact 'Historical G2 omitted meaning retained by amendment' $contract.runtime055Amendment.historicalG2OmittedValue 'Strict'
    Assert-Exact 'Current 0.5.5 omitted meaning' $contract.runtime055Amendment.currentOmittedCodeModKindMeaning 'LegacyNativeCompatibility'
    Assert-Exact 'Current 0.5.5 explicit Strict meaning' $contract.runtime055Amendment.currentExplicitStrictMeaning 'Strict'
    Assert-Exact 'Current 0.5.5 explicit Advanced meaning' $contract.runtime055Amendment.currentExplicitAdvancedMeaning 'Advanced'
    Assert-True 'Current schema-2 Strict author discriminator is explicit' ([bool]$contract.runtime055Amendment.schema2StrictAuthorProjectsDeclareDiscriminator)
    Assert-Exact 'Current legacy native load boundary' $contract.runtime055Amendment.legacyNativeLoadBoundary 'cold-start-only'
    Assert-Exact 'Author schema latest version' $contract.authorProjectWire.latestSchemaVersion '2'
    Assert-Exact 'Author schema legacy version' $contract.authorProjectWire.legacySchemaVersion '1'
    Assert-Exact 'Strict diagnostic' $contract.authorProjectWire.strictDiagnostic 'SDK160'
    Assert-Exact 'Advanced target framework' $contract.authorProjectWire.targetFramework 'netstandard2.0'
    Assert-True 'Advanced explicit game-root requirement' $contract.authorProjectWire.advancedBuildRequiresExplicitGameRoot

    $policyRelativePath = Normalize-Path ([string]$contract.referencePolicy.path)
    $policyPath = Resolve-RepoFile $policyRelativePath $policyRelativePath
    $policyText = Read-Utf8 $policyPath
    try { $policy = $policyText | ConvertFrom-Json }
    catch { Add-Failure "Advanced reference policy is not parseable JSON: $($_.Exception.Message)"; $policy = $null }
    if ($null -ne $policy) {
        Assert-Exact 'Policy ID' $policy.policyId ([string]$contract.referencePolicy.policyId)
        Assert-Exact 'Policy version' $policy.policyVersion ([string]$contract.referencePolicy.policyVersion)
        Assert-Exact 'Policy game build' $policy.gameBuildId ([string]$contract.referencePolicy.gameBuildId)
        Assert-ExactSet 'Policy reference names' @($policy.references | ForEach-Object { [string]$_.assemblyName }) @('0Harmony', 'Assembly-CSharp')
        foreach ($expected in @($contract.referencePolicy.references)) {
            $rows = @($policy.references | Where-Object { ([string]$_.assemblyName).Equals([string]$expected.assemblyName, [StringComparison]::Ordinal) })
            if ($rows.Count -ne 1) { Add-Failure "Policy reference '$($expected.assemblyName)' must appear exactly once; found $($rows.Count)."; continue }
            Assert-Exact "Policy path $($expected.assemblyName)" $rows[0].gameRelativePath ([string]$expected.gameRelativePath)
            Assert-Exact "Policy length $($expected.assemblyName)" $rows[0].length ([string]$expected.length)
            Assert-Exact "Policy SHA $($expected.assemblyName)" ([string]$rows[0].sha256).ToUpperInvariant() ([string]$expected.sha256)
            if ([bool]$rows[0].copyLocal) { Add-Failure "Policy reference '$($expected.assemblyName)' must not be copy-local." }
        }
    }
    if (Test-Path -LiteralPath $policyPath -PathType Leaf) { Assert-Exact 'Tracked reference-policy SHA' (Get-Sha256 $policyPath) ([string]$contract.referencePolicy.sha256) }

    Assert-Contains 'author-sdk/schemas/manifest.schema.json' @('CodeModKind', 'Strict', 'Advanced', 'ContentPack')
    Assert-Contains 'author-sdk/schemas/dtmapi-author.schema.json' @('schemaVersion', 'targetDtmApiVersion', 'codeModKind', 'referencePolicyId')
    Assert-Contains 'src/DTMAPI.AuthorSdk/ProjectValidator.cs' @('SDK160', 'Advanced')
    Assert-Contains 'src/DTMAPI.Core/Manifesting/ManagedModClassification.cs' @('AdvancedReferencePolicyAuthority', 'Assembly.LoadFrom', 'bundled-native-runtime-dependency')
    Assert-Contains 'src/DTMAPI.Core/Runtime/AdvancedHarmonySupervisor.cs' @('dtmapi.mod.', 'restart', 'late')
    Assert-Contains 'src/DTMAPI.InstallDoctor/AdvancedReferencePolicyAuthority.cs' @('RegistryResourceName', 'policySha256', 'embedded Advanced reference policy')
    Assert-Contains 'src/DTMAPI.InstallDoctor/DoctorReportFormatter.cs' @('Managed identity', 'Native risk', 'Restart policy')
    Assert-Contains 'author-sdk/schemas/doctor-report.schema.json' @('managedIdentity', 'provenanceStatus', 'nativeRisk', 'referenceCompatibility', 'gameCompatibility', 'expectedHarmonyOwner', 'restartPolicy')
    Assert-Contains 'src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs' @('identity=', 'provenance=', 'nativeRisk=', 'gameCompatibility=', 'restart=')
    Assert-Contains 'tests/mod-fixtures/qa/AdvancedCodeMod/manifest.json' @('"Type": "CodeMod"', '"CodeModKind": "Advanced"')
    Assert-Contains 'tests/mod-fixtures/qa/AdvancedCodeMod/dtmapi.author.json' @('"schemaVersion": 2', '"codeModKind": "Advanced"')
    Assert-Contains 'tests/mod-fixtures/qa/AdvancedCodeMod/src/ModEntry.cs' @('Has087DemoData', 'dtmapi.mod.dtmapi.advancedfixture', 'WrongOwner', 'DuplicatePatch', 'EntryFailure', 'LateOwnerDrift')
    Assert-Contains 'tools/scripts/test.ps1' @('DTMAPI.UnitTests\DTMAPI.UnitTests.csproj', 'DTMAPI.InstallDoctor.Tests\DTMAPI.InstallDoctor.Tests.csproj', 'DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj', 'test-batch6-g2-advanced-synthetic.ps1', '-AllowInProgress')
    foreach ($projectPath in @('src/DTMAPI.AuthorSdk/DTMAPI.AuthorSdk.csproj', 'src/DTMAPI.Core/DTMAPI.Core.csproj', 'src/DTMAPI.InstallDoctor/DTMAPI.InstallDoctor.csproj')) { Assert-Contains $projectPath @('advanced-reference-policies', 'EmbeddedResource') }

    Assert-ExactSet 'Failure-code assertion mapping' @($contract.failureCodeAssertions | ForEach-Object { [string]$_.code }) @($contract.requiredFailureCodes)
    foreach ($mapping in @($contract.failureCodeAssertions)) {
        foreach ($property in @('code', 'component', 'productionPath', 'negativeTestPath', 'negativeTestCase')) {
            if ([string]::IsNullOrWhiteSpace([string]$mapping.$property)) { Add-Failure "Failure-code mapping is missing $property for '$($mapping.code)'." }
        }
        $productionText = Read-Utf8 (Join-Path $repo (Normalize-Path ([string]$mapping.productionPath)))
        if ($productionText.IndexOf([string]$mapping.code, [StringComparison]::Ordinal) -lt 0) { Add-Failure "Failure code '$($mapping.code)' is absent from mapped $($mapping.component) production file $($mapping.productionPath)." }
        $testText = Read-Utf8 (Join-Path $repo (Normalize-Path ([string]$mapping.negativeTestPath)))
        $caseText = Get-CSharpTestCaseText $testText ([string]$mapping.negativeTestCase)
        if ([string]::IsNullOrWhiteSpace($caseText)) { Add-Failure "Mapped negative test case '$($mapping.negativeTestCase)' was not found in $($mapping.negativeTestPath)." }
        elseif ($caseText.IndexOf([string]$mapping.code, [StringComparison]::Ordinal) -lt 0) { Add-Failure "Mapped negative test '$($mapping.negativeTestCase)' has no exact '$($mapping.code)' assertion." }
    }

    $admissionCommit = ([string](Invoke-GitLines @('rev-parse', "$($contract.phaseState.phase0AdmissionCommit)^{commit}") | Select-Object -First 1)).Trim()
    $targetCommit = ''
    $useWorktree = -not $isPassed
    if ($isPassed) {
        try { $targetCommit = ([string](Invoke-GitLines @('rev-parse', "$($contract.phaseState.g2ImplementationCommit)^{commit}") | Select-Object -First 1)).Trim() }
        catch { Add-Failure "G2 implementation commit is not resolvable: $($_.Exception.Message)" }
        Assert-Exact 'Resolved G2 implementation commit' $targetCommit ([string]$contract.phaseState.g2ImplementationCommit)
        if (-not (Test-GitCommand @('merge-base', '--is-ancestor', $admissionCommit, $targetCommit))) { Add-Failure 'G2 implementation commit does not descend from the Phase 0 admission commit.' }
        if (-not (Test-GitCommand @('merge-base', '--is-ancestor', $targetCommit, 'HEAD'))) { Add-Failure 'G2 implementation commit is not an ancestor of current HEAD.' }
    }

    $protectedRoots = @('src/DTMAPI.Abstractions', 'src/DTMAPI.GameBridge.DolocTown')
    $protectedChanges = @(Get-ChangedPaths $admissionCommit $targetCommit $protectedRoots $useWorktree)
    if ($protectedChanges.Count -gt 0) { Add-Failure "G2 changed frozen Abstractions/GameBridge roots (tracked or untracked): $($protectedChanges -join ', ')" }

    Assert-ExactSet 'Explicit synthetic fixture roots' @($contract.productCatalogFreeze.syntheticFixtureRoots) @('testmods/DTMAPI.AdvancedFixture')
    try { $catalog = Read-GitText $admissionCommit ([string]$contract.productCatalogFreeze.catalogPath) | ConvertFrom-Json }
    catch { Add-Failure "Phase 0 admission Product Catalog is not parseable: $($_.Exception.Message)"; $catalog = $null }
    if ($null -ne $catalog) {
        $explicitExclusions = @($contract.productCatalogFreeze.syntheticCatalogExclusions)
        Assert-ExactSet 'Explicit synthetic Catalog exclusions' @($explicitExclusions | ForEach-Object { [string]$_.catalogId }) @(
            $catalog.products | Where-Object { @('QaFixture', 'Example', 'NegativeFixture') -contains [string]$_.role -and -not [string]::IsNullOrWhiteSpace([string]$_.sourceRoot) } | ForEach-Object { [string]$_.catalogId }
        )
        foreach ($exclusion in $explicitExclusions) {
            $matches = @($catalog.products | Where-Object { ([string]$_.catalogId).Equals([string]$exclusion.catalogId, [StringComparison]::Ordinal) })
            if ($matches.Count -ne 1) { Add-Failure "Synthetic Catalog exclusion must bind exactly one row: $($exclusion.catalogId)"; continue }
            Assert-Exact "Synthetic Catalog exclusion role $($exclusion.catalogId)" $matches[0].role ([string]$exclusion.role)
            if (@('QaFixture', 'Example', 'NegativeFixture') -notcontains [string]$matches[0].role) { Add-Failure "Real-product Catalog role cannot be excluded as synthetic: $($exclusion.catalogId)" }
        }
        $excludedIds = @($explicitExclusions | ForEach-Object { [string]$_.catalogId })
        $realProductRoots = @($catalog.products | Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.sourceRoot) -and $excludedIds -notcontains [string]$_.catalogId
        } | ForEach-Object { Normalize-Path ([string]$_.sourceRoot) } | Sort-Object -Unique)
        foreach ($syntheticRoot in @($contract.productCatalogFreeze.syntheticFixtureRoots)) {
            if ($realProductRoots -contains (Normalize-Path ([string]$syntheticRoot))) { Add-Failure "Synthetic G2 fixture root leaked into Catalog-derived real-product set: $syntheticRoot" }
        }
        $productChanges = @(Get-ChangedPaths $admissionCommit $targetCommit $realProductRoots $useWorktree)
        if ($productChanges.Count -gt 0) { Add-Failure "Catalog-derived real-product roots changed before AutoFishing admission: $($productChanges -join ', ')" }
    }

    $runtimeAudit = $contract.runtimeAudit
    Assert-Exact 'Runtime audit schema path' $runtimeAudit.schemaPath 'tools/release/contracts/batch6-g2-runtime-matrix-receipt.schema.json'
    Assert-Exact 'Runtime audit receipt path' $runtimeAudit.receiptPath 'tools/release/baselines/batch6-g2-runtime-matrix-receipt.json'
    Assert-Exact 'Runtime audit builder path' $runtimeAudit.builderPath 'tools/scripts/build-batch6-g2-runtime-matrix-receipt.ps1'
    Assert-Exact 'Runtime audit validator path' $runtimeAudit.validatorPath 'tools/scripts/test-batch6-g2-runtime-matrix-receipt.ps1'
    Assert-Exact 'Runtime audit receipt schema version' $runtimeAudit.receiptSchemaVersion '1'
    Assert-Exact 'Runtime audit implementation commit' $runtimeAudit.implementationCommit 'c79306dfc7de0e85c74e24ece7d4c5cd47cb0822'
    if ($isPassed) {
        Assert-Exact 'Passed G2 implementation/runtime evidence commit' $targetCommit ([string]$runtimeAudit.implementationCommit)
    }
    Assert-Exact 'Runtime audit matrix SHA' $runtimeAudit.matrixSha256 'EC26CED5E689A0839B438FC4C421BDD032AFD26FC308C17C20936812BC95BAD3'
    Assert-ExactSet 'Runtime audit required cases' @($runtimeAudit.requiredCaseIds) @(
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
    Assert-ExactSet 'Runtime closure governance paths' @($runtimeAudit.closureGovernancePaths) @(
        'tools/release/contracts/batch6-g2-advanced-synthetic-contract.json',
        'tools/release/contracts/batch6-g2-ownership-receipt.schema.json',
        'tools/release/contracts/batch6-g2-ownership-semantic-allowances.json',
        'tools/release/contracts/batch6-g2-ownership-semantic-allowances.schema.json',
        'tools/release/contracts/batch6-g2-runtime-matrix-receipt.schema.json',
        'tools/release/baselines/batch6-g2-ownership-receipt.json',
        'tools/release/baselines/batch6-g2-runtime-matrix-receipt.json',
        'tools/scripts/build-batch6-g2-runtime-matrix-receipt.ps1',
        'tools/scripts/build-batch6-g2-ownership-receipt.ps1',
        'tools/scripts/test-batch6-g2-runtime-matrix-receipt.ps1',
        'tools/scripts/test-batch6-g2-advanced-synthetic.ps1',
        'tools/scripts/test.ps1',
        'AGENTS.md',
        'PROJECT.md',
        'README.md',
        'author-docs/README.md',
        'src/README.md',
        'docs/architecture/README.md',
        'docs/architecture/batch6-managed-mod-identity-contract.md',
        'docs/workflows/codex-api-rebuild.md',
        'docs/planning/DolocTownModdingAPI.md',
        'docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md',
        'docs/design/dtmapi-manager-ui-mvp.md',
        'docs/reviews/api/native-owner-domains/INDEX.md',
        'docs/reviews/code/2026/20260720-0004-batch6-g2-advanced-synthetic-vertical-slice-prerequisite.md',
        'docs/reviews/code/2026/20260720-0005-batch6-g2-first-runtime-matrix-root-cause.md',
        'docs/updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md',
        'docs/updates/INDEX-2026-07.md',
        'docs/debug/regressions/smoke-matrix.md',
        'docs/debug/evidence-retention-allowlist.json',
        'docs/hook-map/README.md',
        'docs/hook-map/focused/AdvancedFixture.md'
    )
    Assert-Contains ([string]$runtimeAudit.schemaPath) @('capturedAtUtc', 'genericSmokeExitCode', 'matrixSha256', 'live-update-v1-to-v2')

    $runtimeReceiptRelativePath = if ([string]::IsNullOrWhiteSpace($RuntimeReceiptPath)) { [string]$runtimeAudit.receiptPath } else { $RuntimeReceiptPath }
    $runtimeReceiptFullPath = Resolve-RepoFile $runtimeReceiptRelativePath ([string]$runtimeAudit.receiptPath)
    $runtimeValidatorFullPath = Resolve-RepoFile ([string]$runtimeAudit.validatorPath) ([string]$runtimeAudit.validatorPath)
    try { $runtimeReceipt = Read-Utf8 $runtimeReceiptFullPath | ConvertFrom-Json }
    catch { Add-Failure "G2 Runtime matrix receipt is not parseable: $($_.Exception.Message)"; $runtimeReceipt = $null }
    if ($null -ne $runtimeReceipt) {
        Assert-Exact 'Runtime receipt schema version' $runtimeReceipt.schemaVersion ([string]$runtimeAudit.receiptSchemaVersion)
        Assert-Exact 'Runtime receipt implementation commit' $runtimeReceipt.implementationCommit ([string]$runtimeAudit.implementationCommit)
        Assert-Exact 'Runtime receipt matrix SHA' $runtimeReceipt.matrixSha256 ([string]$runtimeAudit.matrixSha256)
        Assert-ExactSet 'Runtime receipt cases' @($runtimeReceipt.cases | ForEach-Object { [string]$_.caseId }) @($runtimeAudit.requiredCaseIds)
    }
    try {
        if (-not [string]::IsNullOrWhiteSpace($RuntimeEvidenceMapPath)) {
            $runtimeEvidenceMapFullPath = Resolve-RepoFile $RuntimeEvidenceMapPath $RuntimeEvidenceMapPath
            & $runtimeValidatorFullPath -ReceiptPath $runtimeReceiptFullPath -VerifyRawEvidence -EvidenceMapPath $runtimeEvidenceMapFullPath
        }
        else {
            & $runtimeValidatorFullPath -ReceiptPath $runtimeReceiptFullPath
        }
        if ($LASTEXITCODE -ne 0) { Add-Failure "G2 Runtime matrix receipt validator failed with exit code $LASTEXITCODE." }
    }
    catch { Add-Failure "G2 Runtime matrix receipt validator failed: $($_.Exception.Message)" }

    $audit = $contract.ownershipAudit
    Assert-Exact 'Ownership audit baseline' $audit.baselineCommit $admissionCommit
    Assert-Exact 'Ownership receipt schema version' $audit.receiptSchemaVersion '1'
    Assert-Exact 'Ownership semantic allowance path' $audit.semanticAllowancePath 'tools/release/contracts/batch6-g2-ownership-semantic-allowances.json'
    Assert-Exact 'Ownership semantic allowance schema path' $audit.semanticAllowanceSchemaPath 'tools/release/contracts/batch6-g2-ownership-semantic-allowances.schema.json'
    Assert-Exact 'Ownership semantic allowance SHA' $audit.semanticAllowanceSha256 '57EFBB873EE9592E99574A0D79B43AEDBEF1727DF91BEC0D65E9B0EAA31A08B0'
    Assert-Exact 'Ownership semantic allowance schema SHA' $audit.semanticAllowanceSchemaSha256 '2A2072F131CA5BD6348C7034A09286F78BD4F12A01ABFB1D322C862395859C8F'
    Assert-ExactSet 'Ownership audit source roots' @($audit.auditSourceRoots) @(
        'src/DTMAPI.Abstractions',
        'src/DTMAPI.AuthorSdk',
        'src/DTMAPI.Authoring.Contracts',
        'src/DTMAPI.BepInExBootstrap',
        'src/DTMAPI.Core',
        'src/DTMAPI.GameBridge.DolocTown',
        'src/DTMAPI.InstallDoctor',
        'src/DTMAPI.ModConfigMenu',
        'src/DTMAPI.PlayerDoctor',
        'src/DTMAPI.Tooling.Metadata',
        'testmods/DTMAPI.AdvancedFixture'
    )
    Assert-ExactSet 'Ownership Platform source roots' @($audit.platformSourceRoots) @(
        'author-sdk',
        'src/DTMAPI.AuthorSdk',
        'src/DTMAPI.Authoring.Contracts',
        'src/DTMAPI.BepInExBootstrap',
        'src/DTMAPI.Core',
        'src/DTMAPI.InstallDoctor',
        'src/DTMAPI.ModConfigMenu',
        'src/DTMAPI.PlayerDoctor',
        'src/DTMAPI.Tooling.Metadata'
    )
    Assert-ExactSet 'G2 implementation freeze roots' @($audit.implementationFreezeRoots) @(
        'author-sdk',
        'src/DTMAPI.AuthorSdk',
        'src/DTMAPI.Authoring.Contracts',
        'src/DTMAPI.Abstractions',
        'src/DTMAPI.BepInExBootstrap',
        'src/DTMAPI.Core',
        'src/DTMAPI.GameBridge.DolocTown',
        'src/DTMAPI.InstallDoctor',
        'src/DTMAPI.ModConfigMenu',
        'src/DTMAPI.PlayerDoctor',
        'src/DTMAPI.Tooling.Metadata',
        'tests/DTMAPI.AuthorSdk.Tests',
        'tests/DTMAPI.InstallDoctor.Tests',
        'tests/DTMAPI.UnitTests',
        'testmods/DTMAPI.AdvancedFixture',
        'tools/release/contracts/batch6-g2-ownership-receipt.schema.json',
        'tools/scripts/build-batch6-g2-advanced-fixture.ps1',
        'tools/scripts/build-batch6-g2-ownership-receipt.ps1',
        'tools/scripts/check-author-sdk-release.ps1',
        'tools/scripts/test-batch6-g2-advanced-synthetic.ps1',
        'tools/scripts/test-batch6-phase0-contract.ps1',
        'tools/scripts/test.ps1'
    )
    Assert-ExactSet 'Ownership mandatory Runtime roots' @($audit.mandatoryRuntimeRoots) @('src/DTMAPI.Abstractions', 'src/DTMAPI.Core', 'src/DTMAPI.BepInExBootstrap', 'src/DTMAPI.GameBridge.DolocTown', 'src/DTMAPI.ModConfigMenu')
    Assert-ExactSet 'Ownership synthetic ProductNative roots' @($audit.syntheticProductRoots) @('testmods/DTMAPI.AdvancedFixture')
    Assert-Exact 'Ownership stable Abstractions root' $audit.stableAbstractionsRoot 'src/DTMAPI.Abstractions'
    Assert-Exact 'Ownership SharedNative root' $audit.sharedNativeRoot 'src/DTMAPI.GameBridge.DolocTown'
    if ([int]$audit.mandatoryRuntimeProductNativeAddedPhysicalLinesMax -ne 0 -or [int]$audit.stableAbstractionsAddedPhysicalLinesMax -ne 0 -or [int]$audit.stableAbstractionsDeletedPhysicalLinesMax -ne 0) { Add-Failure 'G2 ownership zero-delta maxima must all remain zero.' }
    Assert-Contains ([string]$audit.schemaPath) @('sourceDiffSha256', 'mandatoryRuntimeProductNativeAddedPhysicalLines', 'stableAbstractionsAddedPhysicalLines', 'stableAbstractionsDeletedPhysicalLines')

    $tempBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) { [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT) } else { Join-Path $repo 'tmp\test-runs' }
    if (-not (Test-Path -LiteralPath $tempBase -PathType Container)) { New-Item -ItemType Directory -Path $tempBase -Force | Out-Null }
    $candidatePath = Join-Path $tempBase ('batch6-g2-ownership-candidate-' + [Guid]::NewGuid().ToString('N') + '.json')
    try {
        $generator = Resolve-RepoFile ([string]$audit.generatorPath) ([string]$audit.generatorPath)
        if ($useWorktree) { & $generator -ContractPath $contractFullPath -BaselineGitRef $admissionCommit -Worktree -OutputPath $candidatePath }
        else { & $generator -ContractPath $contractFullPath -BaselineGitRef $admissionCommit -TargetGitRef $targetCommit -OutputPath $candidatePath }
        if (-not $?) { Add-Failure 'G2 ownership receipt candidate generation failed.' }
        if (Test-Path -LiteralPath $candidatePath -PathType Leaf) { $candidate = Read-Utf8 $candidatePath | ConvertFrom-Json }
    }
    catch { Add-Failure "G2 ownership receipt candidate generation failed: $($_.Exception.Message)" }
    finally { if (Test-Path -LiteralPath $candidatePath -PathType Leaf) { Remove-Item -LiteralPath $candidatePath -Force } }

    if ($null -ne $generator -and (Test-Path -LiteralPath $generator -PathType Leaf)) {
        $hostExe = (Get-Process -Id $PID).Path
        $probeSpecs = @(
            [pscustomobject]@{ Name='UnmappedMandatory'; Arguments=@('-ClassificationMutationProbePath','src/DTMAPI.Core/Batch6G2UnmappedMandatoryMutationProbe.cs','-ClassificationMutationProbeKind','UnmappedMandatory') },
            [pscustomobject]@{ Name='ProductNative'; Arguments=@('-ClassificationMutationProbePath','src/DTMAPI.Core/Batch6G2ProductNativeMutationProbe.cs','-ClassificationMutationProbeKind','ProductNative') },
            [pscustomobject]@{ Name='UnauditedMandatory'; Arguments=@('-ClassificationMutationProbePath','src/DTMAPI.Core/Directory.Build.targets','-ClassificationMutationProbeKind','UnauditedMandatory') },
            [pscustomobject]@{ Name='ExtraAllowance'; Arguments=@('-SemanticAllowanceMutationProbe','ExtraAllowance') },
            [pscustomobject]@{ Name='TargetBlob'; Arguments=@('-SemanticAllowanceMutationProbe','TargetBlob') },
            [pscustomobject]@{ Name='DiffSha256'; Arguments=@('-SemanticAllowanceMutationProbe','DiffSha256') }
        )
        foreach ($probeSpec in $probeSpecs) {
            $probeOutputPath = Join-Path $tempBase ('batch6-g2-ownership-mutation-' + $probeSpec.Name + '-' + [Guid]::NewGuid().ToString('N') + '.json')
            $probeArguments = @('-NoLogo', '-NoProfile', '-NonInteractive')
            if ([System.IO.Path]::GetFileName($hostExe).Equals('powershell.exe', [StringComparison]::OrdinalIgnoreCase)) { $probeArguments += @('-ExecutionPolicy', 'Bypass') }
            $probeArguments += @('-File', $generator, '-ContractPath', $contractFullPath, '-BaselineGitRef', $admissionCommit, '-OutputPath', $probeOutputPath)
            if ($useWorktree) { $probeArguments += '-Worktree' }
            else { $probeArguments += @('-TargetGitRef', $targetCommit) }
            $probeArguments += @($probeSpec.Arguments)
            $previousErrorAction = $ErrorActionPreference
            try {
                $ErrorActionPreference = 'Continue'
                $probeOutput = @(& $hostExe @probeArguments 2>&1)
                $probeExitCode = $LASTEXITCODE
            }
            catch { $probeExitCode = -1; Add-Failure "G2 ownership mutation probe '$($probeSpec.Name)' could not run: $($_.Exception.Message)" }
            finally { $ErrorActionPreference = $previousErrorAction }
            if ($probeExitCode -eq 0) { Add-Failure "G2 ownership mutation probe '$($probeSpec.Name)' unexpectedly passed." }
            if (Test-Path -LiteralPath $probeOutputPath -PathType Leaf) {
                Add-Failure "G2 ownership mutation probe '$($probeSpec.Name)' wrote an accepted candidate."
                Remove-Item -LiteralPath $probeOutputPath -Force
            }
        }
    }

    if ($null -ne $candidate) {
        if ($isPassed) {
            Assert-Exact 'Passed G2 ownership target commit' $candidate.implementationCommit $targetCommit
            $receiptRelativePath = if ([string]::IsNullOrWhiteSpace($OwnershipReceiptPath)) { [string]$audit.receiptPath } else { $OwnershipReceiptPath }
            $receiptFullPath = Resolve-RepoFile $receiptRelativePath ([string]$audit.receiptPath)
            try { $receipt = Read-Utf8 $receiptFullPath | ConvertFrom-Json }
            catch { Add-Failure "Passed G2 ownership receipt is not parseable: $($_.Exception.Message)"; $receipt = $null }
            if ($null -ne $receipt) { Compare-ReceiptMechanics $receipt $candidate $audit }
            $contractRelativePath = Normalize-Path $contractFullPath.Substring(([System.IO.Path]::GetFullPath($repo).TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar).Length)
            $dedicatedMachineAuthorities = @(
                'tools/release/contracts/batch6-g2-advanced-synthetic-contract.json',
                'tools/release/contracts/batch6-g2-ownership-receipt.schema.json',
                'tools/release/contracts/batch6-g2-ownership-semantic-allowances.json',
                'tools/release/contracts/batch6-g2-ownership-semantic-allowances.schema.json',
                'tools/release/contracts/batch6-g2-runtime-matrix-receipt.schema.json',
                'tools/release/baselines/batch6-g2-ownership-receipt.json',
                'tools/release/baselines/batch6-g2-runtime-matrix-receipt.json',
                'tools/scripts/build-batch6-g2-runtime-matrix-receipt.ps1',
                'tools/scripts/build-batch6-g2-ownership-receipt.ps1',
                'tools/scripts/test-batch6-g2-runtime-matrix-receipt.ps1',
                'tools/scripts/test-batch6-g2-advanced-synthetic.ps1'
            )
            foreach ($machineAuthority in $dedicatedMachineAuthorities) {
                Assert-TrackedAndClean $machineAuthority
            }
            if (Test-GitCommand @('ls-files', '--error-unmatch', '--', (Normalize-Path $receiptRelativePath))) {
                $receiptCommit = ([string](Invoke-GitLines @('log', '-1', '--format=%H', '--', (Normalize-Path $receiptRelativePath)) | Select-Object -First 1)).Trim()
                if (-not (Test-GitCommand @('merge-base', '--is-ancestor', $targetCommit, $receiptCommit))) { Add-Failure 'Ownership receipt-containing commit must descend from the G2 implementation commit.' }
                if (-not (Test-GitCommand @('merge-base', '--is-ancestor', $receiptCommit, 'HEAD'))) { Add-Failure 'Ownership receipt-containing commit is not an ancestor of current HEAD.' }
                $contractCommit = ([string](Invoke-GitLines @('log', '-1', '--format=%H', '--', $contractRelativePath) | Select-Object -First 1)).Trim()
                Assert-Exact 'Passed contract/receipt closure commit' $contractCommit $receiptCommit
                foreach ($governancePath in @($runtimeAudit.closureGovernancePaths)) {
                    $normalizedGovernancePath = Normalize-Path ([string]$governancePath)
                    if (-not (Test-GitCommand @('cat-file', '-e', ($receiptCommit + ':' + $normalizedGovernancePath)))) {
                        Add-Failure "G2 closure path did not exist in the closure commit: $normalizedGovernancePath"
                    }
                }
                $receiptCommitLine = ([string](Invoke-GitLines @('rev-list', '--parents', '-n', '1', $receiptCommit) | Select-Object -First 1)).Trim()
                $receiptCommitParts = @($receiptCommitLine -split '\s+' | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
                if ($receiptCommitParts.Count -ne 2) {
                    Add-Failure "G2 closure commit must have exactly one parent; found $($receiptCommitParts.Count - 1)."
                }
                else {
                    Assert-Exact 'G2 closure commit identity' $receiptCommitParts[0] $receiptCommit
                    Assert-Exact 'G2 closure commit direct parent' $receiptCommitParts[1] $targetCommit
                }
                $closureImplementationDrift = @(Get-CommitChangedPaths $targetCommit $receiptCommit)
                $allowedClosureGateDrift = @($runtimeAudit.closureGovernancePaths | ForEach-Object { Normalize-Path ([string]$_) })
                Assert-ExactSet 'G2 implementation-to-closure changed paths' $closureImplementationDrift $allowedClosureGateDrift
            }
        }
        else {
            if ([int]$candidate.mandatoryRuntimeProductNativeAddedPhysicalLines -gt [int]$audit.mandatoryRuntimeProductNativeAddedPhysicalLinesMax) { Add-Failure 'In-progress candidate adds mandatory Runtime ProductNative physical lines.' }
            if ([int]$candidate.stableAbstractionsAddedPhysicalLines -gt 0 -or [int]$candidate.stableAbstractionsDeletedPhysicalLines -gt 0) { Add-Failure 'In-progress candidate changes stable Abstractions source.' }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($GameRoot)) {
        $gameRootFull = [System.IO.Path]::GetFullPath($GameRoot)
        foreach ($reference in @($contract.referencePolicy.references)) {
            $referencePath = Join-Path $gameRootFull ([string]$reference.gameRelativePath).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
            if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) { Add-Failure "Installed policy reference is missing: $referencePath"; continue }
            $info = Get-Item -LiteralPath $referencePath
            Assert-Exact "Installed reference length $($reference.assemblyName)" $info.Length ([string]$reference.length)
            Assert-Exact "Installed reference SHA $($reference.assemblyName)" (Get-Sha256 $referencePath) ([string]$reference.sha256)
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }
    exit 1
}

if ($isPassed) {
    Write-Host "Batch 6 G2 synthetic contract PASSED: implementation=$($contract.phaseState.g2ImplementationCommit); ownershipReceipt=verified; fixture=synthetic; autoFishing=admitted-not-migrated; otherRealProducts=blocked."
}
else {
    Write-Warning "Batch 6 G2 development candidate is internally consistent, but G2 remains IN-PROGRESS / NON-RELEASE / BLOCKED. Candidate diff=$($candidate.sourceDiffSha256); no immutable implementation commit or ownership receipt has been accepted."
}
