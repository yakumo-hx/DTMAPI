param(
    [string] $IdentityContractPath = '',
    [string] $DomainContractPath = '',
    [string] $G2ContractPath = '',
    [string] $ReceiptPath = '',
    [string] $AuditGitRef = '',
    [switch] $AllowStaleReceipt
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure([string] $message) { $script:failures.Add($message) }

function Resolve-RepoFile([string] $path, [string] $defaultRelativePath) {
    $value = if ([string]::IsNullOrWhiteSpace($path)) { $defaultRelativePath } else { $path }
    if (-not [System.IO.Path]::IsPathRooted($value)) { $value = Join-Path $repo $value }
    return [System.IO.Path]::GetFullPath($value)
}

function Normalize-Path([string] $path) {
    return ([string]$path).Trim().TrimStart([char]'\', [char]'/').Replace('\', '/').TrimEnd('/')
}

function Read-Utf8Text([string] $path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function Sort-OrdinalStrings([object[]] $values) {
    [string[]]$result = @($values | ForEach-Object { [string]$_ })
    [Array]::Sort($result, [StringComparer]::Ordinal)
    return $result
}

function Assert-ExactSet([string] $label, [object[]] $actual, [object[]] $expected) {
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $expectedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($value in @($actual)) {
        if (-not $actualSet.Add([string]$value)) { Add-Failure "$label contains duplicate value: $value" }
    }
    foreach ($value in @($expected)) {
        if (-not $expectedSet.Add([string]$value)) { throw "$label checker expectation contains a duplicate value: $value" }
    }
    $missing = @(Sort-OrdinalStrings @($expectedSet | Where-Object { -not $actualSet.Contains([string]$_) }))
    $extra = @(Sort-OrdinalStrings @($actualSet | Where-Object { -not $expectedSet.Contains([string]$_) }))
    if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
        Add-Failure "$label mismatch. Missing=[$($missing -join ', ')] Extra=[$($extra -join ', ')]"
    }
}

function Assert-ExactSequence([string] $label, [object[]] $actual, [object[]] $expected) {
    $actualValues = @($actual | ForEach-Object { [string]$_ })
    $expectedValues = @($expected | ForEach-Object { [string]$_ })
    if ($actualValues.Count -ne $expectedValues.Count) {
        Add-Failure "$label count mismatch. Expected=$($expectedValues.Count) Actual=$($actualValues.Count)."
        return
    }
    for ($index = 0; $index -lt $expectedValues.Count; $index++) {
        if (-not $actualValues[$index].Equals($expectedValues[$index], [StringComparison]::Ordinal)) {
            Add-Failure "$label order/value mismatch at index $index. Expected='$($expectedValues[$index])' Actual='$($actualValues[$index])'."
        }
    }
}

function Assert-ExactValue([string] $label, [object] $actual, [string] $expected) {
    if ($null -eq $actual -or -not ([string]$actual).Equals($expected, [StringComparison]::Ordinal)) {
        Add-Failure "$label must be '$expected'; found '$actual'."
    }
}

function Assert-ExactBoolean([string] $label, [object] $actual, [bool] $expected) {
    if ($null -eq $actual -or -not ($actual -is [bool]) -or [bool]$actual -ne $expected) {
        Add-Failure "$label must be $expected; found '$actual'."
    }
}

function ConvertTo-CanonicalUtcTimestamp([object] $value) {
    if ($value -is [DateTime]) {
        return ([DateTime]$value).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
    }
    if ($value -is [DateTimeOffset]) {
        return ([DateTimeOffset]$value).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
    }
    $parsed = [DateTimeOffset]::MinValue
    $styles = [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal
    if (-not [DateTimeOffset]::TryParse([string]$value, [Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$parsed)) { return '' }
    return $parsed.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
}

function ConvertTo-UtcDateTimeOffset([object] $value) {
    $parsed = [DateTimeOffset]::MinValue
    $styles = [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal
    if (-not [DateTimeOffset]::TryParse([string]$value, [Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$parsed)) { return $null }
    return $parsed.ToUniversalTime()
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

function Test-GitCommand([string[]] $arguments) {
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & git -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 1>$null 2>$null
        return $LASTEXITCODE -eq 0
    }
    finally { $ErrorActionPreference = $previousErrorAction }
}

function Read-GitText([string] $commit, [string] $relativePath) {
    $lines = @(Invoke-GitLines @('show', ($commit + ':' + (Normalize-Path $relativePath))))
    if ($lines.Count -eq 0) { return '' }
    return ($lines -join "`n") + "`n"
}

function Resolve-GitCommit([string] $gitRef) {
    return ([string](Invoke-GitLines @('rev-parse', "$gitRef^{commit}") | Select-Object -First 1)).Trim()
}

function Get-GitCommitUtc([string] $commit) {
    $value = ([string](Invoke-GitLines @('show', '-s', '--format=%cI', $commit) | Select-Object -First 1)).Trim()
    return ConvertTo-CanonicalUtcTimestamp $value
}

function Assert-NonEmpty([string] $label, [object] $value) {
    if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) { Add-Failure "$label must not be empty." }
}

function Get-OptionalPropertyValue([object] $value, [string] $name) {
    if ($null -eq $value) { return $null }
    $property = $value.PSObject.Properties[$name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-BaselinePropertyValue([object] $domain, [string] $name) {
    $baselineProjection = Get-OptionalPropertyValue $domain 'baselineProjection'
    if ($null -ne $baselineProjection) {
        $baselineProperty = $baselineProjection.PSObject.Properties[$name]
        if ($null -ne $baselineProperty) { return $baselineProperty.Value }
    }
    return Get-OptionalPropertyValue $domain $name
}

function Resolve-Phase0SampleInterpretation([object] $sample) {
    $type = ([string]$sample.type).Trim()
    $entryDll = ([string]$sample.entryDll).Trim()
    $kind = ([string]$sample.codeModKind).Trim()
    if ([string]::IsNullOrWhiteSpace($type)) {
        if (-not [string]::IsNullOrWhiteSpace($kind) -or [string]::IsNullOrWhiteSpace($entryDll)) { return 'invalid' }
        return 'legacy-declared-strict'
    }
    if ($type.Equals('CodeMod', [StringComparison]::OrdinalIgnoreCase)) {
        if ([string]::IsNullOrWhiteSpace($entryDll)) { return 'invalid' }
        if (-not [string]::IsNullOrWhiteSpace($kind)) { return 'invalid-before-g2' }
        return 'declared-strict'
    }
    if ($type.Equals('ContentPack', [StringComparison]::OrdinalIgnoreCase)) {
        if (-not [string]::IsNullOrWhiteSpace($entryDll) -or -not [string]::IsNullOrWhiteSpace($kind)) { return 'invalid' }
        return 'content-pack'
    }
    return 'invalid'
}

$identityPath = Resolve-RepoFile $IdentityContractPath 'tools\release\contracts\batch6-g0-mod-identity-contract.json'
$domainPath = Resolve-RepoFile $DomainContractPath 'tools\release\contracts\batch6-phase0-domain-contract.json'
$g2Path = Resolve-RepoFile $G2ContractPath 'tools\release\contracts\batch6-g2-advanced-synthetic-contract.json'
$identity = Read-Utf8Text $identityPath | ConvertFrom-Json
$domains = Read-Utf8Text $domainPath | ConvertFrom-Json
$g2 = $null
$g2State = 'not-started'
$g2Activated = $false
if (Test-Path -LiteralPath $g2Path -PathType Leaf) {
    try { $g2 = Read-Utf8Text $g2Path | ConvertFrom-Json }
    catch { Add-Failure "G2 state contract is not parseable: $($_.Exception.Message)" }
    if ($null -ne $g2) {
        if ([int]$g2.schemaVersion -ne 2) { Add-Failure "G2 state contract schema must be 2; found $($g2.schemaVersion)." }
        Assert-ExactValue 'G2 Phase 0 admission commit' $g2.phaseState.phase0AdmissionCommit '1239aa577d74e4f0c29131ba644e8a751eebbf5f'
        $g2State = [string]$g2.phaseState.g2Implementation
        if (@('in-progress', 'passed') -contains $g2State) { $g2Activated = $true }
        elseif (-not [string]::IsNullOrWhiteSpace($g2State)) { Add-Failure "Unsupported G2 implementation state: $g2State" }
    }
}

if ([int]$identity.schemaVersion -ne 1) { Add-Failure "G0 identity contract schema must be 1; found $($identity.schemaVersion)." }
if ([int]$domains.schemaVersion -ne 2) { Add-Failure "Phase 0 domain contract schema must be 2; found $($domains.schemaVersion)." }
if (-not ([string]$identity.canonicalIdentityAuthority).StartsWith('PROJECT.md#', [StringComparison]::Ordinal)) { Add-Failure 'Canonical identity authority must be an anchored PROJECT.md section.' }
Assert-ExactValue 'Architecture projection' $identity.architectureProjection 'docs/architecture/batch6-managed-mod-identity-contract.md'

$expectedPhaseStates = [ordered]@{
    phase0 = 'passed'
    g0Authority = 'passed'
    g1Baseline = 'passed'
    g1OwnershipGate = 'blocked'
    g2Design = 'passed'
    g2Runtime = 'blocked'
    g2MinimalFixture = 'not-implemented'
    autoFishingPilot = 'blocked'
    otherProductMigration = 'blocked'
    contentHostRuntime = 'blocked'
    release055 = 'independently-blocked'
}
Assert-ExactSet 'Phase-state fields' @($identity.phaseState.PSObject.Properties.Name) @($expectedPhaseStates.Keys)
foreach ($entry in $expectedPhaseStates.GetEnumerator()) {
    Assert-ExactValue "Phase state $($entry.Key)" $identity.phaseState.($entry.Key) ([string]$entry.Value)
}

Assert-ExactBoolean 'Advanced Runtime implemented flag' $identity.currentWire.advancedRuntimeImplemented $false
Assert-ExactBoolean 'Content Host implemented flag' $identity.currentWire.contentHostImplemented $false
if ($null -ne $identity.currentWire.advancedManifestDiscriminator) { Add-Failure 'The live Advanced manifest discriminator must remain null before G2.' }

Assert-ExactSet 'Canonical managed identities' @($identity.canonicalTerms.managedIdentities) @('Strict CodeMod', 'Advanced CodeMod', 'ContentPack')
Assert-ExactValue 'Canonical External identity' $identity.canonicalTerms.externalIdentity 'External BepInEx Plugin'
Assert-ExactSet 'Canonical physical ownership categories' @($identity.canonicalTerms.physicalOwnership) @('Platform', 'SharedNative', 'ProductNative', 'ContentOwner')
Assert-ExactSet 'Internal provider types excluded from author identity' @($identity.canonicalTerms.internalProviderTypesExcludedFromAuthorIdentity) @('Runtime', 'RuntimeApi')
Assert-ExactSet 'Current manifest Type values' @($identity.currentWire.manifestTypes) @('CodeMod', 'ContentPack')
Assert-ExactSet 'Current author project kinds' @($identity.currentWire.authorProjectKinds) @('CodeMod', 'ContentPack')
Assert-ExactValue 'Strict SDK diagnostic' $identity.currentWire.strictSdkDiagnostic 'SDK160'
Assert-ExactValue 'Reserved manifest discriminator name' $identity.currentWire.reservedG2ManifestDiscriminator.name 'CodeModKind'
Assert-ExactSet 'Reserved G2 CodeModKind values' @($identity.currentWire.reservedG2ManifestDiscriminator.values) @('Strict', 'Advanced')
Assert-ExactValue 'Reserved manifest discriminator omission' $identity.currentWire.reservedG2ManifestDiscriminator.omittedValue 'Strict'
Assert-ExactBoolean 'Reserved manifest discriminator live-before-G2 flag' $identity.currentWire.reservedG2ManifestDiscriminator.liveBeforeG2 $false
Assert-ExactValue 'Reserved author-project discriminator name' $identity.currentWire.reservedG2AuthorProjectDiscriminator.name 'codeModKind'
Assert-ExactSet 'Reserved author-project discriminator values' @($identity.currentWire.reservedG2AuthorProjectDiscriminator.values) @('Strict', 'Advanced')
Assert-ExactBoolean 'Reserved author-project discriminator live-before-G2 flag' $identity.currentWire.reservedG2AuthorProjectDiscriminator.liveBeforeG2 $false
Assert-ExactBoolean '0.5.5 amendment preserves historical snapshot' $identity.runtime055Amendment.historicalSnapshotPreserved $true
Assert-ExactValue '0.5.5 omitted CodeModKind meaning' $identity.runtime055Amendment.omittedCodeModKindMeaning 'LegacyNativeCompatibility'
Assert-ExactValue '0.5.5 explicit Strict meaning' $identity.runtime055Amendment.explicitStrictMeaning 'Strict'
Assert-ExactValue '0.5.5 explicit Advanced meaning' $identity.runtime055Amendment.explicitAdvancedMeaning 'Advanced'
Assert-ExactBoolean '0.5.5 schema-2 Strict author discriminator' $identity.runtime055Amendment.schema2StrictAuthorProjectsDeclareDiscriminator $true
Assert-ExactValue '0.5.5 legacy native load boundary' $identity.runtime055Amendment.legacyNativeLoadBoundary 'cold-start-only'
Assert-ExactSet 'G2 atomic surfaces' @($identity.g2AtomicSurfaces) @(
    'manifest-schema-and-classifier',
    'core-loader-preload-admission',
    'author-sdk-project-and-reference-policy',
    'game-and-reference-build-receipt',
    'doctor-machine-and-player-projection',
    'manager-and-ui-projection',
    'package-deploy-journal-and-recovery',
    'harmony-owner-and-restart-policy',
    'synthetic-fixture-static-tests',
    'synthetic-fixture-game-evidence',
    'logs-report-and-rollback'
)
Assert-ExactSet 'G2 pre-load fail-closed reasons' @($identity.preLoadFailClosedReasons) @(
    'unknown-manifest-type',
    'unknown-code-mod-kind',
    'code-mod-kind-on-content-pack',
    'content-pack-with-code-fields',
    'advanced-reference-receipt-missing',
    'advanced-reference-hash-mismatch',
    'advanced-game-build-unverifiable',
    'advanced-game-build-incompatible',
    'wrong-target-framework',
    'bundled-native-runtime-dependency'
)
Assert-ExactSet 'Forbidden work before G2' @($identity.forbiddenBeforeG2) @(
    'relax-sdk160',
    'accept-code-mod-kind-in-live-schema',
    'accept-advanced-as-manifest-type',
    'infer-advanced-from-source-or-assembly-references',
    'hand-package-advanced',
    'migrate-autofishing',
    'migrate-any-real-product'
)

Assert-ExactValue 'Runtime package root' $identity.runtimePackageTopology.root 'BepInEx/plugins/DTMAPI'
if ([int]$identity.runtimePackageTopology.currentAcceptanceDllCount -ne 5) { Add-Failure 'Runtime package topology must retain the current five-DLL acceptance count.' }
Assert-ExactBoolean 'Permanent assembly-count promise' $identity.runtimePackageTopology.permanentAssemblyCountPromise $false

$projectText = Read-Utf8Text (Join-Path $repo 'PROJECT.md')
foreach ($term in @('Strict CodeMod', 'Advanced CodeMod', 'ContentPack', 'External BepInEx Plugin', 'Platform', 'SharedNative', 'ProductNative', 'ContentOwner')) {
    if ($projectText.IndexOf([string]$term, [StringComparison]::Ordinal) -lt 0) { Add-Failure "PROJECT.md is missing canonical term: $term" }
}

$expectedDocumentationTokens = [ordered]@{
    'PROJECT.md' = @('one BepInEx plugin entry', 'SDK160', 'ProductNative', 'Phase 0 correction is verified')
    'AGENTS.md' = @('one BepInEx plugin entry', 'batch6-managed-mod-identity-contract.md', 'SDK160', 'Phase 0 correction is verified')
    'src/README.md' = @('one BepInEx plugin entry', 'four co-located Runtime dependencies')
    'src/DTMAPI.BepInExBootstrap/README.md' = @('only DTMAPI assembly containing the BepInEx plugin entry', 'four DTMAPI Runtime dependencies')
    'docs/onboarding/current-state.md' = @('one DTMAPI BepInEx plugin entry', 'four co-located Runtime dependencies')
    'author-sdk/README.md' = @('Strict CodeMod', 'batch6-managed-mod-identity-contract.md', 'SDK160')
    'docs/design/dtmapi-manager-ui-mvp.md' = @('Managed identity projection', 'batch6-managed-mod-identity-contract.md')
    'docs/architecture/batch6-managed-mod-identity-contract.md' = @('Phase 0 correction verified', 'This correction changes only pre-G2', 'it does not implement', 'G1 reproducible baseline: PASS', 'G2 Advanced Runtime and minimal fixture: BLOCKED')
}
Assert-ExactSet 'Required G0 projection documents' @($identity.requiredDocumentationTokens.PSObject.Properties.Name) @($expectedDocumentationTokens.Keys)
foreach ($entry in $expectedDocumentationTokens.GetEnumerator()) {
    $contractTokens = $identity.requiredDocumentationTokens.PSObject.Properties[[string]$entry.Key]
    if ($null -eq $contractTokens) {
        Add-Failure "Required documentation token contract is missing: $($entry.Key)"
        continue
    }
    Assert-ExactSet "Required tokens for $($entry.Key)" @($contractTokens.Value) @($entry.Value)
    $docPath = Join-Path $repo (Normalize-Path ([string]$entry.Key))
    if ($g2Activated) {
        try { $text = Read-GitText ([string]$g2.phaseState.phase0AdmissionCommit) ([string]$entry.Key) }
        catch { Add-Failure "Required historical G0 projection document is unavailable at Phase 0 admission: $($entry.Key)"; continue }
    }
    else {
        if (-not (Test-Path -LiteralPath $docPath -PathType Leaf)) {
            Add-Failure "Required G0 projection document is missing: $($entry.Key)"
            continue
        }
        $text = Read-Utf8Text $docPath
    }
    foreach ($token in @($entry.Value)) {
        if ($text.IndexOf([string]$token, [StringComparison]::Ordinal) -lt 0) { Add-Failure "$($entry.Key) is missing required token: $token" }
    }
}

$manifestSchema = Read-Utf8Text (Join-Path $repo 'author-sdk\schemas\manifest.schema.json') | ConvertFrom-Json
$authorSchema = Read-Utf8Text (Join-Path $repo 'author-sdk\schemas\dtmapi-author.schema.json') | ConvertFrom-Json
Assert-ExactSet 'Live author manifest schema Type enum' @($manifestSchema.properties.Type.enum) @('CodeMod', 'ContentPack')
if ($g2Activated) {
    Assert-ExactSet 'G2 author project schema-1 kind enum' @($authorSchema.'$defs'.schema1.properties.projectKind.enum) @('CodeMod', 'ContentPack')
    Assert-ExactSet 'G2 author project schema-2 kind enum' @($authorSchema.'$defs'.schema2.properties.projectKind.enum) @('CodeMod', 'ContentPack')
}
else {
    Assert-ExactSet 'Live author project schema kind enum' @($authorSchema.properties.projectKind.enum) @('CodeMod', 'ContentPack')
}
Assert-ExactBoolean 'Live author manifest schema top-level additionalProperties' $manifestSchema.additionalProperties $false
Assert-ExactBoolean 'Live author manifest dependency additionalProperties' $manifestSchema.properties.Dependencies.items.additionalProperties $false

if (-not $g2Activated) {
    $runtimeManifestJsonSupportText = Read-Utf8Text (Join-Path $repo 'src\DTMAPI.AuthorSdk\JsonSupport.cs')
    if ($runtimeManifestJsonSupportText.IndexOf('UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow', [StringComparison]::Ordinal) -lt 0) {
        Add-Failure 'Author SDK RuntimeManifest deserialization must reject unmapped fields before G2.'
    }
    $coreManifestReaderText = Read-Utf8Text (Join-Path $repo 'src\DTMAPI.Core\Manifesting\ManifestReader.cs')
    foreach ($token in @('ManifestReservedFieldGuard', 'CodeModKind is reserved until the G2 Advanced CodeMod vertical slice is implemented')) {
        if ($coreManifestReaderText.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Add-Failure "Core manifest pre-load reserved-field guard is missing token: $token" }
    }
    $doctorManifestProbeText = Read-Utf8Text (Join-Path $repo 'src\DTMAPI.InstallDoctor\ManifestProbe.cs')
    foreach ($token in @('CodeModKind', 'reserved until the G2 Advanced CodeMod vertical slice is implemented')) {
        if ($doctorManifestProbeText.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Add-Failure "Install Doctor reserved-field rejection is missing token: $token" }
    }
    $hostileTestContracts = [ordered]@{
        'tests/DTMAPI.AuthorSdk.Tests/Program.cs' = @('reserved CodeModKind validate', 'reserved CodeModKind pack', 'reserved CodeModKind deploy', 'must not publish a Mod destination')
        'tests/DTMAPI.InstallDoctor.Tests/Program.cs' = @('Doctor must reject the reserved CodeModKind field as an invalid manifest before G2.')
        'tests/DTMAPI.UnitTests/Program.cs' = @('ReservedCodeModKindFailsClosedBeforeDiscoveryAndAssemblyLoad', 'rejected before Assembly.LoadFrom')
    }
    foreach ($entry in $hostileTestContracts.GetEnumerator()) {
        $testText = Read-Utf8Text (Join-Path $repo ([string]$entry.Key))
        foreach ($token in @($entry.Value)) {
            if ($testText.IndexOf([string]$token, [StringComparison]::Ordinal) -lt 0) { Add-Failure "$($entry.Key) is missing hostile reserved-field assertion: $token" }
        }
    }
    $fullTestDriverText = Read-Utf8Text (Join-Path $repo 'tools\scripts\test.ps1')
    foreach ($projectToken in @('DTMAPI.UnitTests\DTMAPI.UnitTests.csproj', 'DTMAPI.InstallDoctor.Tests\DTMAPI.InstallDoctor.Tests.csproj', 'DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj')) {
        if ($fullTestDriverText.IndexOf($projectToken, [StringComparison]::Ordinal) -lt 0) { Add-Failure "Full Release gate does not execute hostile-test owner project: $projectToken" }
    }

    $liveWireFiles = @(
        'author-sdk/schemas/manifest.schema.json',
        'author-sdk/schemas/dtmapi-author.schema.json',
        'author-sdk/templates/codemod/manifest.json.template',
        'author-sdk/templates/codemod/dtmapi.author.json.template',
        'author-sdk/templates/contentpack/manifest.json.template',
        'author-sdk/templates/contentpack/dtmapi.author.json.template',
        'src/DTMAPI.Authoring.Contracts/AuthorContracts.cs',
        'src/DTMAPI.Core/Manifesting/ManifestModels.cs',
        'src/DTMAPI.Abstractions/Manifest.cs'
    )
    foreach ($relativePath in $liveWireFiles) {
        $text = Read-Utf8Text (Join-Path $repo $relativePath)
        if ($text.IndexOf('CodeModKind', [StringComparison]::Ordinal) -ge 0 -or $text.IndexOf('codeModKind', [StringComparison]::Ordinal) -ge 0) {
            Add-Failure "Reserved G2 discriminator leaked into a live schema/model/template before G2: $relativePath"
        }
    }
}
$validatorText = Read-Utf8Text (Join-Path $repo 'src\DTMAPI.AuthorSdk\ProjectValidator.cs')
if ($validatorText.IndexOf([string]$identity.currentWire.strictSdkDiagnostic, [StringComparison]::Ordinal) -lt 0) { Add-Failure 'SDK160 is missing from the current Strict Author SDK validator.' }

$catalogRelativePath = 'tools/release/dtmapi-product-catalog.json'
$currentCatalog = Read-Utf8Text (Join-Path $repo $catalogRelativePath) | ConvertFrom-Json
if ($g2Activated) {
    $catalog = Read-GitText ([string]$g2.phaseState.phase0AdmissionCommit) $catalogRelativePath | ConvertFrom-Json
}
else {
    $catalog = $currentCatalog
}
Assert-ExactSet 'Current Runtime package assemblies' @($catalog.playerRuntimePackageInvariant.productionAssemblies) @(
    [string]$identity.runtimePackageTopology.bepInPluginEntry
    @($identity.runtimePackageTopology.coLocatedRuntimeDependencies)
)
if ([int]$catalog.playerRuntimePackageInvariant.productionAssemblyCount -ne [int]$identity.runtimePackageTopology.currentAcceptanceDllCount) { Add-Failure 'Catalog Runtime DLL count disagrees with the G0 topology contract.' }
if (-not ([string]$catalog.playerRuntimePackageInvariant.bepInExEntryAssembly).Equals([string]$identity.runtimePackageTopology.bepInPluginEntry, [StringComparison]::Ordinal)) { Add-Failure 'Catalog BepInEx entry assembly disagrees with the G0 topology contract.' }
$identityOptionalComponents = @($identity.runtimePackageTopology.optionalFrameworkComponents)
$catalogOptionalComponents = @($currentCatalog.playerRuntimePackageInvariant.optionalComponents)
if ($identityOptionalComponents.Count -ne 1 -or $catalogOptionalComponents.Count -ne 1) {
    Add-Failure 'The G0 topology and current Catalog must each freeze exactly one optional framework component.'
}
else {
    foreach ($field in @('id', 'distribution', 'loadPolicy', 'relativePath', 'assemblyName', 'sourceProject', 'targetFramework', 'isFrameworkComponent', 'isMod', 'isBepInExPlugin', 'includedInDownloadPackage', 'defaultLoadState')) {
        Assert-ExactValue "Optional framework component field $field" $catalogOptionalComponents[0].$field $identityOptionalComponents[0].$field
    }
}

[xml]$runtimeVersionAuthority = Read-Utf8Text (Join-Path $repo 'tools\release\dtmapi-runtime-version.props')
$runtimeVersionProperties = @($runtimeVersionAuthority.Project.PropertyGroup | Select-Object -First 1)[0]
Assert-ExactValue 'Runtime release/API version authority' $runtimeVersionProperties.DtmApiReleaseVersion '0.6.1'
Assert-ExactValue 'Runtime binary/file version authority' $runtimeVersionProperties.DtmApiBinaryFileVersion '0.6.1.0'
Assert-ExactValue 'Runtime assembly compatibility authority' $runtimeVersionProperties.DtmApiAssemblyCompatibilityVersion '0.5.3.0'
$apiMatrixText = Read-Utf8Text (Join-Path $repo 'docs\api\public-api-matrix.md')
foreach ($token in @('release/API `0.6.1`', 'binary/file `0.6.1.0`', 'assembly compatibility `0.5.3.0`', 'future `0.6.2-alpha` requirement is blocked')) {
    if ($apiMatrixText.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Add-Failure "Public API matrix is missing the active Runtime version projection: $token" }
}
foreach ($staleCurrentText in @('Current version baseline is `0.5.3-alpha`', '`0.5.3-alpha` is the current dev baseline label', 'future `0.5.4-alpha` requirements are blocked')) {
    if ($apiMatrixText.IndexOf($staleCurrentText, [StringComparison]::Ordinal) -ge 0) { Add-Failure "Public API matrix retains a stale current-version statement: $staleCurrentText" }
}
$roadmapText = Read-Utf8Text (Join-Path $repo 'docs\planning\20260712-dtmapi-lightweight-functional-mod-roadmap.md')
$zoomPriorityPhrase = ([string][char]0x4F18) + ([char]0x5148) + ([char]0x8FC1) + ([char]0x56DE) + ' Zoom'
$solePilotPhrase = ([string][char]0x552F) + ([char]0x4E00) + ([char]0x771F) + ([char]0x5B9E) + ([char]0x4EA7) + ([char]0x54C1) + ' pilot'
$secondProductPhrase = ([string][char]0x7B2C) + ([char]0x4E8C) + ([char]0x771F) + ([char]0x5B9E) + ([char]0x4EA7) + ([char]0x54C1)
if ($roadmapText.IndexOf($zoomPriorityPhrase, [StringComparison]::Ordinal) -ge 0) { Add-Failure 'Roadmap still prioritizes Zoom ahead of the AutoFishing pilot.' }
if ($roadmapText.IndexOf($solePilotPhrase, [StringComparison]::Ordinal) -lt 0 -and
    ($roadmapText.IndexOf($secondProductPhrase, [StringComparison]::Ordinal) -lt 0 -or
     $roadmapText.IndexOf('OneActionComplete', [StringComparison]::Ordinal) -lt 0)) {
    Add-Failure 'Roadmap is missing both the historical unique AutoFishing pilot gate and the current unique-second-product OneActionComplete gate.'
}
foreach ($token in @('G2 synthetic fixture', 'G3/G4', 'G5/G6', 'G0-G7')) {
    if ($roadmapText.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { Add-Failure "Roadmap is missing the corrected AutoFishing-first order token: $token" }
}

$omittedTypePaths = New-Object System.Collections.Generic.List[string]
foreach ($product in @($currentCatalog.products)) {
    $sourceRoot = Normalize-Path ([string]$product.sourceRoot)
    if ([string]::IsNullOrWhiteSpace($sourceRoot)) { continue }
    $manifestPath = Normalize-Path ($sourceRoot + '/manifest.json')
    try {
        $fullManifestPath = Join-Path $repo $manifestPath
        if (-not (Test-Path -LiteralPath $fullManifestPath -PathType Leaf)) { continue }
        $manifestText = Read-Utf8Text $fullManifestPath
        $manifest = $manifestText | ConvertFrom-Json
    }
    catch { Add-Failure "Catalog manifest is not parseable for Phase 0 identity scan: $manifestPath"; continue }
    if (-not $g2Activated -and
        ($null -ne $manifest.PSObject.Properties['CodeModKind'] -or
         $null -ne $manifest.PSObject.Properties['codeModKind'])) {
        Add-Failure "Real product/fixture manifest uses the reserved G2 discriminator before G2: $manifestPath"
    }
    $typeProperty = $manifest.PSObject.Properties['Type']
    if ($null -eq $typeProperty -or [string]::IsNullOrWhiteSpace([string]$typeProperty.Value)) {
        $omittedTypePaths.Add($manifestPath)
    }
    elseif (@('CodeMod', 'ContentPack') -notcontains [string]$typeProperty.Value) {
        Add-Failure "Catalog manifest has an unsupported current Type: $manifestPath Type=$($typeProperty.Value)"
    }
}
$expectedOmittedTypePaths = @(
    'products/first-party/ManboCardboardAudio/manifest.json'
)
Assert-ExactSet 'Contract legacy omitted-Type manifest inputs' @($identity.legacyOmittedTypeManifestPaths) $expectedOmittedTypePaths
Assert-ExactSet 'Scanned legacy omitted-Type manifest inputs' @($omittedTypePaths.ToArray()) $expectedOmittedTypePaths

$expectedSampleOutcomes = [ordered]@{
    'explicit-code-mod' = 'declared-strict'
    'omitted-type-legacy' = 'legacy-declared-strict'
    'content-pack' = 'content-pack'
    'content-pack-with-dll' = 'invalid'
    'unknown-type' = 'invalid'
    'reserved-kind-before-g2' = 'invalid-before-g2'
}
Assert-ExactSet 'Identity interpretation sample names' @($identity.interpretationSamples | ForEach-Object { [string]$_.name }) @($expectedSampleOutcomes.Keys)
foreach ($sample in @($identity.interpretationSamples)) {
    $expectedOutcome = [string]$expectedSampleOutcomes[[string]$sample.name]
    Assert-ExactValue "Identity sample $($sample.name) expected outcome" $sample.expectedPhase0Interpretation $expectedOutcome
    $actual = Resolve-Phase0SampleInterpretation $sample
    if (-not $actual.Equals($expectedOutcome, [StringComparison]::Ordinal)) {
        Add-Failure "Identity meta-negative sample $($sample.name) expected $expectedOutcome but classified $actual."
    }
}

$expectedDomainIds = @(
    'auto-fishing',
    'zoom',
    'one-action-complete',
    'auto-harvest',
    'action-speed',
    'more-saves',
    'y-console',
    'chest-locator',
    'fish-roe-tooltip',
    'animal-viewer',
    'strong-planting',
    'oil-drop',
    'mine-machine',
    'equipment-slots',
    'custom-animals-host',
    'animal-pack',
    'audio-replacement-host',
    'bgm-replacement',
    'gmcm-manager-ui',
    'event-lifetime-kernel',
    'custom-entities',
    'multiplayer',
    'pets-and-vehicles'
)
$expectedPhysicalOwnershipCategories = @('Platform', 'SharedNative', 'ProductNative', 'ContentOwner')
$expectedDecisionStates = @('decided', 'split-decided', 'content-only-decided', 'candidate', 'candidate-deferred', 'blocked-unresolved', 'deferred-independent', 'deferred-unadjudicated')
$expectedTargetDeploymentForms = @('Advanced CodeMod', 'ContentPack', 'Content Host', 'DTMAPI Runtime')

Assert-ExactValue 'Phase 0 baseline Git commit' $domains.baseline.gitCommit '653487b7463778c23e9b96aed9ef713364def22a'
if ($null -ne $domains.baseline.PSObject.Properties['capturedAtUtc']) { Add-Failure 'The domain contract must not own a capturedAtUtc fact; generation time belongs only to the receipt.' }
Assert-ExactValue 'Phase 0 baseline generator' $domains.baseline.generator 'tools/scripts/build-batch6-phase0-baseline.ps1'
Assert-ExactValue 'Phase 0 baseline receipt' $domains.baseline.receipt 'tools/release/baselines/batch6-phase0-ownership-baseline-20260720.json'
$expectedMandatoryRoots = @(
    'src/DTMAPI.Abstractions',
    'src/DTMAPI.Core',
    'src/DTMAPI.BepInExBootstrap',
    'src/DTMAPI.GameBridge.DolocTown',
    'src/DTMAPI.ModConfigMenu'
)
Assert-ExactSet 'Mandatory Runtime source roots' @($domains.mandatoryRuntimeAudit.roots) $expectedMandatoryRoots
Assert-ExactSet 'Mandatory Runtime allowed ownership classifications' @($domains.mandatoryRuntimeAudit.allowedOwnershipClassifications) @('Platform', 'SharedNative', 'ProductNative')
if ([int]$domains.mandatoryRuntimeAudit.targetProductNativeAddedPhysicalLinesMax -ne 0) { Add-Failure 'Mandatory Runtime ProductNative added physical-line maximum must be zero.' }
$semanticAllowances = @($domains.mandatoryRuntimeAudit.semanticPathAllowances)
if ($semanticAllowances.Count -ne 1) {
    Add-Failure "Mandatory Runtime semantic path allowance count must be 1; found $($semanticAllowances.Count)."
}
else {
    Assert-ExactValue 'ManifestReader semantic allowance path' $semanticAllowances[0].path 'src/DTMAPI.Core/Manifesting/ManifestReader.cs'
    Assert-ExactValue 'ManifestReader semantic allowance classification' $semanticAllowances[0].classification 'Platform'
    Assert-ExactValue 'ManifestReader semantic allowance review' $semanticAllowances[0].review 'docs/reviews/code/2026/20260720-0003-batch5-closeout-and-batch6-phase0-acceptance-audit.md'
    Assert-NonEmpty 'ManifestReader semantic allowance reason' $semanticAllowances[0].reason
}
Assert-ExactSet 'Batch 5 source ranges' @($domains.batch5Classification.sourceCommitRanges) @('7ff75c4c^..9e31aae5', 'e5aaa964..653487b7463778c23e9b96aed9ef713364def22a')
Assert-ExactValue 'Batch 5 source-range note' $domains.batch5Classification.rangeNote 'Union of the Batch 5 checkpoint-to-closure source/tooling ranges; excludes the intervening e5aaa964 reverse-baseline-only correction while retaining the shared test.ps1 entry point where Batch 5 also changed it.'
Assert-ExactSet 'Contract required Batch 6 domain IDs' @($domains.requiredDomainIds) $expectedDomainIds
Assert-ExactSet 'Batch 6 domain IDs' @($domains.domains | ForEach-Object { [string]$_.id }) $expectedDomainIds
Assert-ExactSet 'Allowed physical ownership candidates' @($domains.allowedPhysicalOwnershipCandidates) $expectedPhysicalOwnershipCategories
Assert-ExactSet 'Allowed decision states' @($domains.allowedDecisionStates) $expectedDecisionStates
Assert-ExactSet 'Allowed target deployment forms' @($domains.allowedTargetDeploymentForms) $expectedTargetDeploymentForms

$expectedDomainDecisions = [ordered]@{
    'auto-fishing' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'zoom' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'one-action-complete' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'auto-harvest' = [ordered]@{ disposition = 'ApiDemandSample'; candidates = @(); state = 'deferred-unadjudicated'; identities = @() }
    'action-speed' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'more-saves' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'y-console' = [ordered]@{ disposition = 'ProductNative+PlatformSeam'; candidates = @('ProductNative', 'Platform'); state = 'split-decided'; identities = @('Advanced CodeMod', 'DTMAPI Runtime') }
    'chest-locator' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'fish-roe-tooltip' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'animal-viewer' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'strong-planting' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'oil-drop' = [ordered]@{ disposition = 'ContentPack'; candidates = @(); state = 'content-only-decided'; identities = @('ContentPack') }
    'mine-machine' = [ordered]@{ disposition = 'ProductNative+ContentOwner'; candidates = @('ProductNative', 'ContentOwner'); state = 'split-decided'; identities = @('Advanced CodeMod', 'ContentPack') }
    'equipment-slots' = [ordered]@{ disposition = 'ProductNative'; candidates = @('ProductNative'); state = 'decided'; identities = @('Advanced CodeMod') }
    'custom-animals-host' = [ordered]@{ disposition = 'ContentOwner'; candidates = @('ContentOwner'); state = 'decided'; identities = @('Content Host') }
    'animal-pack' = [ordered]@{ disposition = 'ContentPack'; candidates = @(); state = 'content-only-decided'; identities = @('ContentPack') }
    'audio-replacement-host' = [ordered]@{ disposition = 'ContentOwnerCandidate'; candidates = @('ContentOwner'); state = 'candidate'; identities = @('Content Host') }
    'bgm-replacement' = [ordered]@{ disposition = 'ContentOwnerCandidate-deferred'; candidates = @('ContentOwner'); state = 'candidate-deferred'; identities = @('Content Host') }
    'gmcm-manager-ui' = [ordered]@{ disposition = 'Platform'; candidates = @('Platform'); state = 'decided'; identities = @('DTMAPI Runtime') }
    'event-lifetime-kernel' = [ordered]@{ disposition = 'Platform'; candidates = @('Platform'); state = 'decided'; identities = @('DTMAPI Runtime') }
    'custom-entities' = [ordered]@{ disposition = 'blocked-retire-or-internalize'; candidates = @(); state = 'blocked-unresolved'; identities = @() }
    'multiplayer' = [ordered]@{ disposition = 'deferred-independent-project'; candidates = @(); state = 'deferred-independent'; identities = @() }
    'pets-and-vehicles' = [ordered]@{ disposition = 'deferred-unadjudicated'; candidates = @(); state = 'deferred-unadjudicated'; identities = @() }
}
foreach ($domain in @($domains.domains)) {
    $id = [string]$domain.id
    $expectedDecision = $expectedDomainDecisions[$id]
    foreach ($field in @('reviewDisposition', 'decisionState', 'nativeOwner', 'stateHolder', 'targetAssembly', 'apiDelta', 'hookAndLifetime', 'marginalPlan')) {
        Assert-NonEmpty "Domain $id $field" $domain.$field
    }
    Assert-ExactValue "Domain $id review disposition" $domain.reviewDisposition ([string]$expectedDecision.disposition)
    Assert-ExactValue "Domain $id decision state" $domain.decisionState ([string]$expectedDecision.state)
    Assert-ExactSet "Domain $id physical ownership candidates" @($domain.physicalOwnershipCandidates) @($expectedDecision.candidates)
    Assert-ExactSet "Domain $id target deployment forms" @($domain.targetDeploymentForms) @($expectedDecision.identities)
    foreach ($candidate in @($domain.physicalOwnershipCandidates)) {
        if ($expectedPhysicalOwnershipCategories -cnotcontains [string]$candidate) { Add-Failure "Domain $id contains an invalid physical ownership candidate: $candidate" }
    }
    foreach ($deploymentForm in @($domain.targetDeploymentForms)) {
        if ($expectedTargetDeploymentForms -cnotcontains [string]$deploymentForm) { Add-Failure "Domain $id contains an invalid target deployment form: $deploymentForm" }
    }
    if ([int]$domain.realConsumerCount -ne @($domain.currentConsumers).Count) { Add-Failure "Domain $id realConsumerCount does not match currentConsumers count." }
    $seenConsumers = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($consumerUniqueId in @($domain.currentConsumers)) {
        if (-not $seenConsumers.Add([string]$consumerUniqueId)) { Add-Failure "Domain $id contains a duplicate current consumer: $consumerUniqueId" }
        $consumerProducts = @($currentCatalog.products | Where-Object { ([string]$_.uniqueId).Equals([string]$consumerUniqueId, [StringComparison]::Ordinal) })
        if ($consumerProducts.Count -ne 1) {
            Add-Failure "Domain $id current consumer must bind to exactly one Catalog UniqueID: $consumerUniqueId matched $($consumerProducts.Count)."
            continue
        }
        $consumerProduct = $consumerProducts[0]
        if (([string](Get-OptionalPropertyValue $consumerProduct 'identityState')).Equals('FrozenReservedNoArtifact', [StringComparison]::Ordinal)) {
            Add-Failure "Domain $id current consumer is a reserved no-artifact Catalog identity: $consumerUniqueId"
        }
        $sourceRoot = Normalize-Path ([string](Get-OptionalPropertyValue $consumerProduct 'sourceRoot'))
        $sourceManifest = Normalize-Path ([string](Get-OptionalPropertyValue $consumerProduct 'sourceManifest'))
        $hasTrackedSource = -not [string]::IsNullOrWhiteSpace($sourceRoot)
        if ($hasTrackedSource) {
            $sourceRootPath = Join-Path $repo $sourceRoot
            if (-not (Test-Path -LiteralPath $sourceRootPath -PathType Container)) {
                Add-Failure "Domain $id current consumer Catalog sourceRoot is missing from the current tree: $consumerUniqueId -> $sourceRoot"
            }
            if ([string]::IsNullOrWhiteSpace($sourceManifest)) {
                Add-Failure "Domain $id current consumer Catalog tracked source is missing sourceManifest: $consumerUniqueId"
            }
            else {
                $sourceManifestPath = Join-Path $repo $sourceManifest
                if (-not (Test-Path -LiteralPath $sourceManifestPath -PathType Leaf)) {
                    Add-Failure "Domain $id current consumer Catalog sourceManifest is missing from the current tree: $consumerUniqueId -> $sourceManifest"
                }
                else {
                    try { $currentManifest = Read-Utf8Text $sourceManifestPath | ConvertFrom-Json }
                    catch { Add-Failure "Domain $id current consumer Catalog sourceManifest is not parseable: $sourceManifest"; $currentManifest = $null }
                    if ($null -ne $currentManifest -and -not ([string]$currentManifest.UniqueID).Equals([string]$consumerUniqueId, [StringComparison]::Ordinal)) {
                        Add-Failure "Domain $id current consumer manifest UniqueID mismatch: expected=$consumerUniqueId path=$sourceManifest actual=$($currentManifest.UniqueID)"
                    }
                }
            }
        }
        $retainedArtifact = Get-OptionalPropertyValue $consumerProduct 'retainedArtifact'
        $hasRetainedArtifact = $null -ne $retainedArtifact -and
            -not [string]::IsNullOrWhiteSpace([string](Get-OptionalPropertyValue $retainedArtifact 'treeSha256')) -and
            [int64](Get-OptionalPropertyValue $retainedArtifact 'fileCount') -gt 0
        if (-not $hasTrackedSource -and -not $hasRetainedArtifact) {
            Add-Failure "Domain $id current consumer has no Catalog tracked-source or retained-artifact evidence: $consumerUniqueId"
        }
    }
    if ([int]$domain.targetMandatoryRuntimeProductLocDeltaMax -ne 0) { Add-Failure "Domain $id allows a nonzero mandatory Runtime product LOC delta." }
    foreach ($category in @('corePlatform', 'gameBridge', 'product', 'qa', 'compatibility')) {
        if ($null -eq $domain.sourceScopes.PSObject.Properties[$category]) { Add-Failure "Domain $id is missing sourceScopes.$category." }
    }
    if ([bool]$domain.targetMandatoryLoad -and (@($domain.physicalOwnershipCandidates).Count -ne 1 -or -not ([string]$domain.physicalOwnershipCandidates[0]).Equals('Platform', [StringComparison]::Ordinal))) {
        Add-Failure "Only a decided Platform domain may target mandatory load in Phase 0: $id"
    }
    if (([string]$domain.decisionState).StartsWith('candidate', [StringComparison]::Ordinal) -or ([string]$domain.decisionState).StartsWith('deferred', [StringComparison]::Ordinal) -or ([string]$domain.decisionState).Equals('blocked-unresolved', [StringComparison]::Ordinal)) {
        if ([bool]$domain.targetMandatoryLoad) { Add-Failure "Unresolved/deferred domain $id cannot target mandatory load." }
    }
}

$autoHarvestDomain = @($domains.domains | Where-Object {
    ([string]$_.id).Equals('auto-harvest', [StringComparison]::Ordinal)
})[0]
if ($null -eq $autoHarvestDomain) {
    Add-Failure 'Current Phase 0 projection is missing the AutoHarvest domain.'
}
else {
    if ([int]$autoHarvestDomain.realConsumerCount -ne 0 -or @($autoHarvestDomain.currentConsumers).Count -ne 0) {
        Add-Failure 'AutoHarvest ApiDemandSample must not count as a current real consumer.'
    }
    Assert-ExactSet 'AutoHarvest current product source scope' @($autoHarvestDomain.sourceScopes.product) @()
    Assert-ExactSet 'AutoHarvest current QA/sample source scope' @($autoHarvestDomain.sourceScopes.qa) @(
        'author-sdk/samples/api-demand/AutoHarvest',
        'tests/mod-fixtures/qa/CropHarvesting',
        'src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/CropHarvestingFixtureCase.cs')
    $autoHarvestSamples = @($autoHarvestDomain.apiDemandSamples)
    if ($autoHarvestSamples.Count -ne 1) {
        Add-Failure "AutoHarvest apiDemandSamples count must be 1; found $($autoHarvestSamples.Count)."
    }
    else {
        $sample = $autoHarvestSamples[0]
        Assert-ExactValue 'AutoHarvest sample UniqueID' $sample.uniqueId 'Yuuka.DTMAPI.AutoHarvest'
        Assert-ExactValue 'AutoHarvest sample Catalog ID' $sample.catalogId 'auto-harvest-sample'
        Assert-ExactValue 'AutoHarvest sample Catalog role' $sample.catalogRole 'ApiDemandSample'
        Assert-ExactValue 'AutoHarvest sample release eligibility' $sample.releaseEligibility 'NeverPublish'
        Assert-ExactValue 'AutoHarvest sample source root' $sample.sourceRoot 'author-sdk/samples/api-demand/AutoHarvest'
        $sampleCatalogRows = @($currentCatalog.products | Where-Object {
            ([string]$_.catalogId).Equals([string]$sample.catalogId, [StringComparison]::Ordinal) -and
            ([string]$_.uniqueId).Equals([string]$sample.uniqueId, [StringComparison]::Ordinal)
        })
        if ($sampleCatalogRows.Count -ne 1) {
            Add-Failure "AutoHarvest sample must bind to exactly one current Catalog row; found $($sampleCatalogRows.Count)."
        }
        else {
            Assert-ExactValue 'AutoHarvest Catalog role binding' $sampleCatalogRows[0].role $sample.catalogRole
            Assert-ExactValue 'AutoHarvest Catalog release binding' $sampleCatalogRows[0].releaseEligibility $sample.releaseEligibility
            Assert-ExactValue 'AutoHarvest Catalog source binding' $sampleCatalogRows[0].sourceRoot $sample.sourceRoot
        }
    }
    Assert-ExactValue 'AutoHarvest historical disposition' (Get-BaselinePropertyValue $autoHarvestDomain 'reviewDisposition') 'ProductNative'
    Assert-ExactSet 'AutoHarvest historical ownership candidates' @(Get-BaselinePropertyValue $autoHarvestDomain 'physicalOwnershipCandidates') @('ProductNative')
    Assert-ExactSet 'AutoHarvest historical consumers' @(Get-BaselinePropertyValue $autoHarvestDomain 'currentConsumers') @('Yuuka.DTMAPI.AutoHarvest')
    if ([int](Get-BaselinePropertyValue $autoHarvestDomain 'realConsumerCount') -ne 1) {
        Add-Failure 'AutoHarvest historical baseline must retain one consumer for receipt reproduction.'
    }
}

$zoomDomain = @($domains.domains | Where-Object {
    ([string]$_.id).Equals('zoom', [StringComparison]::Ordinal)
})[0]
if ($null -eq $zoomDomain) {
    Add-Failure 'Current Phase 0 projection is missing the Zoom domain.'
}
else {
    Assert-ExactSet 'Zoom current product source scope' @($zoomDomain.sourceScopes.product) @('products/first-party/Zoom')
    Assert-ExactSet 'Zoom current compatibility source scope' @($zoomDomain.sourceScopes.compatibility) @('src/DTMAPI.GameBridge.DolocTown/Compatibility/Camera')
    Assert-ExactSet 'Zoom current QA source scope' @($zoomDomain.sourceScopes.qa) @('src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/ZoomProductNativeFixtureCase.cs')
    Assert-ExactSet 'Zoom historical Phase 0 product source scope' @($zoomDomain.baselineSourceScopes.product) @('first-party-mods/ZoomMod')
    Assert-ExactSet 'Zoom historical Phase 0 compatibility source scope' @($zoomDomain.baselineSourceScopes.compatibility) @()
    if ([string]$zoomDomain.hookAndLifetime -notmatch 'SetEnvCamera\(Vector2,Vector2,bool,bool,bool\)' -or
        [string]$zoomDomain.hookAndLifetime -notmatch 'existing Compatibility Host') {
        Add-Failure 'Zoom current hook/lifetime projection must name the exact five-parameter native reset and existing dormant Host.'
    }
}

$moreSavesDomain = @($domains.domains | Where-Object { ([string]$_.id).Equals('more-saves', [StringComparison]::Ordinal) })[0]
if ($null -eq $moreSavesDomain) {
    Add-Failure 'Current Phase 0 projection is missing the MoreSaves domain.'
}
else {
    Assert-ExactSet 'MoreSaves current product source scope' @($moreSavesDomain.sourceScopes.product) @('products/first-party/MoreSaves')
    Assert-ExactSet 'MoreSaves current compatibility source scope' @($moreSavesDomain.sourceScopes.compatibility) @('src/DTMAPI.GameBridge.DolocTown/Compatibility/SaveSlots')
    Assert-ExactSet 'MoreSaves historical Phase 0 product source scope' @($moreSavesDomain.baselineSourceScopes.product) @('testmods/MoreSavesMod')
    Assert-ExactSet 'MoreSaves historical Phase 0 compatibility source scope' @($moreSavesDomain.baselineSourceScopes.compatibility) @()
    Assert-ExactValue 'MoreSaves historical Phase 0 Hook projection' $moreSavesDomain.baselineProjection.hookAndLifetime 'Product owns slot/page patches; Platform may own generic save data/lifecycle only.'
    if ([string]$moreSavesDomain.hookAndLifetime -notmatch 'zero Harmony patches' -or
        [string]$moreSavesDomain.hookAndLifetime -notmatch 'restores native six' -or
        [string]$moreSavesDomain.hookAndLifetime -match 'slot/page patches') {
        Add-Failure 'MoreSaves current hook/lifetime projection must describe zero-Harmony direct archive-count ownership and must not retain the old slot/page Hook claim.'
    }
}

$equipmentSlotsDomain = @($domains.domains | Where-Object {
    ([string]$_.id).Equals('equipment-slots', [StringComparison]::Ordinal)
})[0]
if ($null -eq $equipmentSlotsDomain) {
    Add-Failure 'Current Phase 0 projection is missing the EquipmentSlots domain.'
}
else {
    Assert-ExactSet 'EquipmentSlots current product source scope' @($equipmentSlotsDomain.sourceScopes.product) @('products/first-party/MoreEquipmentSlots')
    Assert-ExactSet 'EquipmentSlots current compatibility source scope' @($equipmentSlotsDomain.sourceScopes.compatibility) @('src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots')
    Assert-ExactSet 'EquipmentSlots historical Phase 0 product source scope' @($equipmentSlotsDomain.baselineSourceScopes.product) @('testmods/MoreEquipmentSlotsMod')
    Assert-ExactSet 'EquipmentSlots historical Phase 0 compatibility source scope' @($equipmentSlotsDomain.baselineSourceScopes.compatibility) @()
}

$mineDomain = @($domains.domains | Where-Object {
    ([string]$_.id).Equals('mine-machine', [StringComparison]::Ordinal)
})[0]
if ($null -eq $mineDomain) {
    Add-Failure 'Current Phase 0 projection is missing the Mine domain.'
}
else {
    Assert-ExactSet 'Mine current product source scope' @($mineDomain.sourceScopes.product) @('products/first-party/Mine')
    Assert-ExactSet 'Mine current GameBridge source scope' @($mineDomain.sourceScopes.gameBridge) @()
    Assert-ExactSet 'Mine historical Phase 0 product source scope' @($mineDomain.baselineSourceScopes.product) @('testmods/MineMod')
    Assert-ExactSet 'Mine historical Phase 0 GameBridge source scope' @($mineDomain.baselineSourceScopes.gameBridge) @('src/DTMAPI.GameBridge.DolocTown/Features/MachineProduction')
    if ([string]$mineDomain.hookAndLifetime -notmatch 'session-derived' -or
        [string]$mineDomain.marginalPlan -notmatch 'no Mine sidecar') {
        Add-Failure 'Mine current projection must bind the session-derived scheduler and explicit no-sidecar boundary.'
    }
}

$animalPackDomain = @($domains.domains | Where-Object { ([string]$_.id).Equals('animal-pack', [StringComparison]::Ordinal) })[0]
if ([int]$animalPackDomain.realConsumerCount -ne 0 -or @($animalPackDomain.currentConsumers).Count -ne 0) { Add-Failure 'AnimalPack must not be counted as a current real consumer before an artifact exists.' }
$animalPackPlannedConsumers = @($animalPackDomain.plannedConsumers)
if ($animalPackPlannedConsumers.Count -ne 1) {
    Add-Failure "AnimalPack planned consumer count must be 1; found $($animalPackPlannedConsumers.Count)."
}
else {
    Assert-ExactValue 'AnimalPack planned Catalog ID' $animalPackPlannedConsumers[0].catalogId 'animal-pack'
    Assert-ExactValue 'AnimalPack planned UniqueID' $animalPackPlannedConsumers[0].uniqueId 'DTMAPI.AnimalPack'
    Assert-ExactValue 'AnimalPack planned state' $animalPackPlannedConsumers[0].state 'FrozenReservedNoArtifact'
}

$customAnimalsDomain = @($domains.domains | Where-Object { ([string]$_.id).Equals('custom-animals-host', [StringComparison]::Ordinal) })[0]
if ([int]$customAnimalsDomain.realConsumerCount -ne 0 -or @($customAnimalsDomain.currentConsumers).Count -ne 0) { Add-Failure 'Mutable-only CustomAnimals prototypes must not count as current real consumers.' }
$expectedPrototypeInputs = @('DTMAPI.HatchAssets', 'DTMAPI.MoleAssets', 'DTMAPI.DreckoAssets', 'DTMAPI.OilfloaterAssets', 'DTMAPI.ShellCrabMod')
$prototypeInputs = @($customAnimalsDomain.observedPrototypeInputs)
Assert-ExactSet 'CustomAnimals observed prototype input identities' @($prototypeInputs | ForEach-Object { [string]$_.uniqueId }) $expectedPrototypeInputs
foreach ($prototypeInput in $prototypeInputs) {
    Assert-ExactValue "CustomAnimals observed prototype authority $($prototypeInput.uniqueId)" $prototypeInput.evidenceAuthority 'MutableLocalEvidenceOnly'
    Assert-ExactBoolean "CustomAnimals observed prototype real-consumer qualification $($prototypeInput.uniqueId)" $prototypeInput.qualifiesAsIndependentRealConsumer $false
    $catalogMatches = @($catalog.products | Where-Object { ([string]$_.catalogId).Equals([string]$prototypeInput.catalogId, [StringComparison]::Ordinal) -and ([string]$_.uniqueId).Equals([string]$prototypeInput.uniqueId, [StringComparison]::Ordinal) })
    if ($catalogMatches.Count -ne 1) { Add-Failure "CustomAnimals observed prototype input does not bind to one Catalog row: $($prototypeInput.uniqueId)" }
}

$audioHostDomain = @($domains.domains | Where-Object { ([string]$_.id).Equals('audio-replacement-host', [StringComparison]::Ordinal) })[0]
Assert-ExactSet 'Audio host current real consumers' @($audioHostDomain.currentConsumers) @('Yuuka.DTMAPI.ManboCardboardAudio')
if ([int]$audioHostDomain.realConsumerCount -ne 1) { Add-Failure "Audio host real consumer count must be 1; found $($audioHostDomain.realConsumerCount)." }
$audioDependentRoutes = @($audioHostDomain.dependentRoutes)
if ($audioDependentRoutes.Count -ne 1) {
    Add-Failure "Audio host dependent route count must be 1; found $($audioDependentRoutes.Count)."
}
else {
    Assert-ExactValue 'Audio host dependent route ID' $audioDependentRoutes[0].id 'custom-animals-animal-voice'
    Assert-ExactBoolean 'Audio host dependent route real-consumer qualification' $audioDependentRoutes[0].qualifiesAsIndependentRealConsumer $false
}

$expectedCompatibility = [ordered]@{
    'legacy-fishing-public' = [ordered]@{
        symbols = @('IFishingAutomationApi', 'FishingAutomationOptions'); roots = @(); files = @()
        retained = 'Published AutoFishing 0.5.2 DLL, SHA-256 E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA'
        plan = 'Freeze through 0.5.5 with warning; optionalize executor; earliest removal 0.6.0 after zero-consumer rescan, migration guide and one published warning-bearing preview.'
    }
    'first-party-fishing-primitives' = [ordered]@{
        symbols = @('IFirstPartyFishingPrimitivesApi', 'IFirstPartyFishingSession'); roots = @('first-party-mods/AutoFishingMod'); files = @('first-party-mods/AutoFishingMod/ModEntry.cs')
        retained = 'none; internal friend surface'
        plan = 'Remove only inside the admitted AutoFishing pilot after the G2 fixture, together with provider/facade/demand/tests.'
    }
    'camera-compatibility' = [ordered]@{
        symbols = @('ICameraZoomApi', 'ICameraViewApi'); roots = @(); files = @()
        retained = 'Published Zoom 0.4.2-dtmapi DLL, SHA-256 DFA74BDD3561A9E9647FEC01AB9B48F095826D2A752AE920566BE2C5063971B3'
        plan = 'Keep the exact frozen MemberRefs behind the existing dormant Compatibility Host; the current Advanced Zoom product consumes neither API, and no removal or stability promotion is authorized.'
    }
    'lamp-disabled-shell' = [ordered]@{
        symbols = @('ILampControlApi'); roots = @(); files = @()
        retained = 'retained ABI requires four types and 92 signatures'
        plan = 'Keep disabled shell through 0.5.5; earliest removal 0.6.0 after warning cycle and renewed consumer scan.'
    }
    'product-mirror-apis' = [ordered]@{
        symbols = @('IActionCompletionApi', 'IActionSpeedApi', 'ISaveSlotsApi', 'IChestLocatorEnhancerApi', 'IItemTooltipApi', 'IAnimalViewerApi', 'IStrongPlantingGunApi', 'ICropHarvestingApi', 'IEquipmentSlotsApi', 'IMachineProductionApi', 'IAudioReplacementApi')
        roots = @('author-sdk/samples/api-demand/AutoHarvest', 'tests/mod-fixtures/qa/CropHarvesting', 'products/first-party/ManboCardboardAudio')
        files = @('author-sdk/samples/api-demand/AutoHarvest/ModEntry.cs', 'tests/mod-fixtures/qa/CropHarvesting/ModEntry.cs', 'products/first-party/ManboCardboardAudio/ModEntry.cs')
        retained = 'tracked product Catalog plus retained public ABI audit; external universe is not provably closed'
        plan = 'No Phase 0 deletion. Each migration preserves ABI or records a warning-bearing breaking boundary and fresh external scan.'
    }
    'custom-entity-speculative-public' = [ordered]@{
        symbols = @('ICustomAnimalApi', 'ICustomMonsterApi', 'ICustomAttackApi', 'ICustomDroneApi'); roots = @(); files = @()
        retained = 'none known; tests exercise registry and blocked runtime verbs'
        plan = 'Freeze expansion; retire/internalize only at an explicit breaking boundary after warning and consumer scan.'
    }
}
Assert-ExactSet 'Compatibility surface IDs' @($domains.compatibilitySurfaces | ForEach-Object { [string]$_.id }) @($expectedCompatibility.Keys)
foreach ($surface in @($domains.compatibilitySurfaces)) {
    $expectedSurface = $expectedCompatibility[[string]$surface.id]
    Assert-ExactValue "Compatibility surface $($surface.id) plan" $surface.plan ([string]$expectedSurface.plan)
    Assert-ExactValue "Compatibility surface $($surface.id) retained artifact consumer" $surface.retainedArtifactConsumer ([string]$expectedSurface.retained)
    Assert-ExactSet "Compatibility surface $($surface.id) symbols" @($surface.symbols) @($expectedSurface.symbols)
    Assert-ExactSet "Compatibility surface $($surface.id) known tracked consumers" @($surface.knownTrackedConsumers) @($expectedSurface.roots)
    if ([string]$surface.id -eq 'product-mirror-apis') {
        $symbolPattern = '(?<![A-Za-z0-9_])(?:' + ((@($surface.symbols) | ForEach-Object { [regex]::Escape([string]$_) }) -join '|') + ')(?![A-Za-z0-9_])'
        $sourceMatches = New-Object 'System.Collections.Generic.List[string]'
        $sourceRoots = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        foreach ($scan in @(
            [ordered]@{ path = 'first-party-mods'; rootLength = 2 },
            [ordered]@{ path = 'products/first-party'; rootLength = 3 },
            [ordered]@{ path = 'author-sdk/samples/api-demand'; rootLength = 4 },
            [ordered]@{ path = 'tests/mod-fixtures/qa'; rootLength = 4 }
        )) {
            $absoluteScanRoot = Join-Path $repo $scan.path
            if (-not (Test-Path -LiteralPath $absoluteScanRoot)) { continue }
            foreach ($sourceFile in @(Get-ChildItem -LiteralPath $absoluteScanRoot -Recurse -File -Filter '*.cs')) {
                if (-not [regex]::IsMatch((Read-Utf8Text $sourceFile.FullName), $symbolPattern)) { continue }
                $relativePath = Normalize-Path $sourceFile.FullName.Substring($repo.Length)
                $sourceMatches.Add($relativePath)
                $segments = @($relativePath.Split('/'))
                $rootLength = [int]$scan.rootLength
                if ($segments.Count -ge $rootLength) {
                    $sourceRoots.Add(($segments[0..($rootLength - 1)] -join '/')) | Out-Null
                }
            }
        }
        Assert-ExactSet 'Product mirror current source matches' @($sourceMatches.ToArray()) @($expectedSurface.files)
        Assert-ExactSet 'Product mirror current source roots' @($sourceRoots) @($surface.knownTrackedConsumers)
    }
}

$expectedBatch5Rules = [ordered]@{
    'core-platform-kernel' = [ordered]@{
        classification = 'Platform'
        prefixes = @('src/DTMAPI.Core/')
        action = 'Retain generic event, generation, boundary, demand accounting, registry and owner-cleanup mechanisms; split any product-specific branch.'
    }
    'product-source' = [ordered]@{
        classification = 'ProductNative-or-product-policy'
        prefixes = @('first-party-mods/', 'products/first-party/', 'author-sdk/samples/')
        action = 'Keep policy with product; native code moves only after its Advanced admission.'
    }
    'optional-qa' = [ordered]@{
        classification = 'QA'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown.QA/', 'tests/')
        action = 'Remain optional/test-only; product-native QA follows the admitted product.'
    }
    'frozen-compatibility' = [ordered]@{
        classification = 'Compatibility'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown/Compatibility/')
        action = 'No new product logic; optionalize and retire only after warning/consumer/version gates.'
    }
    'custom-animals-content-owner' = [ordered]@{
        classification = 'ContentOwner'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/')
        action = 'Target an optional Content Host under G7; do not mix species policy.'
    }
    'generic-owner-cleanup' = [ordered]@{
        classification = 'Platform-SharedNative'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown/Features/OwnerCleanup/')
        action = 'Retain generic participant/restore accounting while deleting product-specific cleanup branches with products.'
    }
    'product-feature-routes' = [ordered]@{
        classification = 'ProductNative-transition'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown/Features/')
        action = 'Move single-product implementation with each admitted product; explicit OwnerCleanup remains generic.'
    }
    'gamebridge-diagnostic-projection' = [ordered]@{
        classification = 'Mixed-PlatformDiagnostic-ProductStatus'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown/Diagnostics/', 'src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs')
        action = 'Retain generic bounded diagnostics; remove product status/probe branches with the owning product.'
    }
    'gamebridge-qa-host-seam' = [ordered]@{
        classification = 'QA-seam'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown/QaHost/')
        action = 'Keep only the neutral optional-host seam; product scenario authority stays in the optional QA assembly.'
    }
    'mixed-gamebridge-shell' = [ordered]@{
        classification = 'Mixed-Platform-SharedNative-ProductRoutes'
        prefixes = @('src/DTMAPI.GameBridge.DolocTown/Demand/', 'src/DTMAPI.GameBridge.DolocTown/Hooking/', 'src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge', 'src/DTMAPI.GameBridge.DolocTown/OwnerBoundGameBridgeApis.cs', 'src/DTMAPI.GameBridge.DolocTown/AuthorSessionReloadBridge.cs')
        action = 'Retain only proven generic/shared shell and remove product routes during the owning migration.'
    }
    'batch5-tooling' = [ordered]@{
        classification = 'QA-release-tooling'
        prefixes = @('tools/scripts/')
        action = 'Keep evidence/release tooling outside player Runtime; product-specific orchestrators follow product QA.'
    }
}
Assert-ExactSequence 'Batch 5 classification rule order' @($domains.batch5Classification.rules | ForEach-Object { [string]$_.id }) @($expectedBatch5Rules.Keys)
foreach ($rule in @($domains.batch5Classification.rules)) {
    $expectedRule = $expectedBatch5Rules[[string]$rule.id]
    Assert-ExactValue "Batch 5 rule $($rule.id) classification" $rule.classification ([string]$expectedRule.classification)
    Assert-ExactSet "Batch 5 rule $($rule.id) prefixes" @($rule.pathPrefixes) @($expectedRule.prefixes)
    Assert-ExactValue "Batch 5 rule $($rule.id) target action" $rule.targetAction ([string]$expectedRule.action)
}

$receipt = $null
$receiptPathFull = Resolve-RepoFile $ReceiptPath ([string]$domains.baseline.receipt)
if (-not (Test-Path -LiteralPath $receiptPathFull -PathType Leaf)) {
    Add-Failure "Phase 0 baseline receipt is missing: $receiptPathFull"
}
else {
    try { $receipt = Read-Utf8Text $receiptPathFull | ConvertFrom-Json }
    catch { Add-Failure "Phase 0 baseline receipt is not parseable: $($_.Exception.Message)" }
}
if ($null -ne $receipt) {
    if ([int]$receipt.schemaVersion -ne 2) {
        if ($AllowStaleReceipt) {
            Write-Warning "Phase 0 receipt schema is stale ($($receipt.schemaVersion)); receipt/time/delta checks are explicitly skipped for this development run."
            $receipt = $null
        }
        else {
            Add-Failure "Phase 0 receipt schema must be 2; found $($receipt.schemaVersion). Regenerate the receipt after committing the audited source tree."
        }
    }
}
if ($null -ne $receipt -and [int]$receipt.schemaVersion -eq 2) {
    Assert-ExactValue 'Receipt contract ID' $receipt.contractId 'batch6-phase0-domain-ownership'
    Assert-ExactValue 'Receipt Git commit' $receipt.gitCommit '653487b7463778c23e9b96aed9ef713364def22a'
    $resolvedSourceCommit = Resolve-GitCommit ([string]$receipt.gitCommit)
    $resolvedAuditCommit = Resolve-GitCommit ([string]$receipt.auditedGitCommit)
    Assert-ExactValue 'Receipt resolved source commit' $resolvedSourceCommit ([string]$receipt.gitCommit)
    Assert-ExactValue 'Receipt source commit time' (ConvertTo-CanonicalUtcTimestamp $receipt.sourceCommitTimeUtc) (Get-GitCommitUtc $resolvedSourceCommit)
    Assert-ExactValue 'Receipt audited commit time' (ConvertTo-CanonicalUtcTimestamp $receipt.auditedCommitTimeUtc) (Get-GitCommitUtc $resolvedAuditCommit)
    if (-not [string]::IsNullOrWhiteSpace($AuditGitRef)) {
        Assert-ExactValue 'Receipt requested audited commit' $resolvedAuditCommit (Resolve-GitCommit $AuditGitRef)
    }

    $captureInstant = ConvertTo-UtcDateTimeOffset $receipt.capturedAtUtc
    $sourceCommitInstant = ConvertTo-UtcDateTimeOffset $receipt.sourceCommitTimeUtc
    $auditedCommitInstant = ConvertTo-UtcDateTimeOffset $receipt.auditedCommitTimeUtc
    if ($null -eq $captureInstant) { Add-Failure "Receipt capturedAtUtc is invalid: $($receipt.capturedAtUtc)" }
    if ($null -eq $sourceCommitInstant) { Add-Failure "Receipt sourceCommitTimeUtc is invalid: $($receipt.sourceCommitTimeUtc)" }
    if ($null -eq $auditedCommitInstant) { Add-Failure "Receipt auditedCommitTimeUtc is invalid: $($receipt.auditedCommitTimeUtc)" }
    if ($null -ne $captureInstant -and $null -ne $sourceCommitInstant -and $captureInstant -lt $sourceCommitInstant) { Add-Failure 'Receipt capture precedes the source commit.' }
    if ($null -ne $captureInstant -and $null -ne $auditedCommitInstant -and $captureInstant -lt $auditedCommitInstant) { Add-Failure 'Receipt capture precedes the audited commit.' }
    if ($null -ne $captureInstant -and $captureInstant -gt [DateTimeOffset]::UtcNow) { Add-Failure 'Receipt capture is later than the current trusted UTC clock.' }

    if (-not (Test-GitCommand @('merge-base', '--is-ancestor', $resolvedAuditCommit, 'HEAD'))) { Add-Failure "Receipt audited commit is not an ancestor of current HEAD: $resolvedAuditCommit" }

    if ([int]$receipt.domainCount -ne 23) { Add-Failure "Receipt domain count must be 23; found $($receipt.domainCount)." }
    Assert-ExactSet 'Receipt domain IDs' @($receipt.domains | ForEach-Object { [string]$_.id }) $expectedDomainIds
    Assert-ExactSet 'Receipt Batch 5 source ranges' @($receipt.batch5SourceRanges) @('7ff75c4c^..9e31aae5', 'e5aaa964..653487b7463778c23e9b96aed9ef713364def22a')
    Assert-ExactValue 'Receipt Batch 5 source-range note' $receipt.batch5SourceRangeNote 'Union of the Batch 5 checkpoint-to-closure source/tooling ranges; excludes the intervening e5aaa964 reverse-baseline-only correction while retaining the shared test.ps1 entry point where Batch 5 also changed it.'
    if ([int]$receipt.batch5ClassifiedFileCount -ne 115) { Add-Failure "Receipt Batch 5 classified file count must be 115; found $($receipt.batch5ClassifiedFileCount)." }

    Assert-ExactSet 'Receipt compatibility surface IDs' @($receipt.compatibilityConsumerScan | ForEach-Object { [string]$_.id }) @($expectedCompatibility.Keys)
    foreach ($surfaceId in @($expectedCompatibility.Keys)) {
        $rows = @($receipt.compatibilityConsumerScan | Where-Object { ([string]$_.id).Equals([string]$surfaceId, [StringComparison]::Ordinal) })
        if ($rows.Count -ne 1) {
            Add-Failure "Receipt compatibility surface $surfaceId must appear exactly once; found $($rows.Count)."
            continue
        }
        $expectedSurface = $expectedCompatibility[[string]$surfaceId]
        $expectedReceiptFiles = @($expectedSurface.files)
        $expectedReceiptRoots = @($expectedSurface.roots)
        if ([string]$surfaceId -eq 'product-mirror-apis') {
            # The receipt is a frozen 2026-07-20 topology observation. Current
            # ProductNative source rehomes are validated above against the live
            # contract and must not rewrite this historical baseline.
            $expectedReceiptRoots = @(
                'testmods/OneActionCompleteMod',
                'testmods/ActionSpeedMod',
                'testmods/MoreSavesMod',
                'testmods/ChestLocatorEnhancerMod',
                'testmods/FishBreedingAssistantMod',
                'testmods/AnimalHusbandryProgressMod',
                'testmods/StrongPlantingGunMod',
                'testmods/AutoHarvestMod',
                'testmods/CropHarvestingQaMod',
                'testmods/MoreEquipmentSlotsMod',
                'testmods/MineMod',
                'testmods/ManboCardboardAudioMod')
            $expectedReceiptFiles = @($expectedReceiptRoots | ForEach-Object { "$_/ModEntry.cs" })
        }
        elseif ([string]$surfaceId -eq 'camera-compatibility') {
            # The Phase 0 receipt predates the admitted Zoom rehome. Preserve
            # that historical source observation while the live contract above
            # binds the current retained binary consumer and dormant Host.
            $expectedReceiptRoots = @('first-party-mods/ZoomMod')
            $expectedReceiptFiles = @('first-party-mods/ZoomMod/ModEntry.cs')
        }
        Assert-ExactSet "Receipt compatibility surface $surfaceId tracked source matches" @($rows[0].trackedProductSourceMatches) $expectedReceiptFiles
        Assert-ExactSet "Receipt compatibility surface $surfaceId declared consumers" @($rows[0].declaredKnownTrackedConsumers) $expectedReceiptRoots
    }

    Assert-ExactSet 'Receipt mandatory Runtime roots' @($receipt.mandatoryRuntimeAudit.roots) $expectedMandatoryRoots
    if ([int]$receipt.mandatoryRuntimeAudit.changedFileCount -ne @($receipt.mandatoryRuntimeAudit.changedFiles).Count) { Add-Failure 'Receipt mandatory Runtime changed-file count does not match its rows.' }
    if ([int]$receipt.mandatoryRuntimeAudit.unclassifiedChangedFileCount -ne 0) { Add-Failure 'Receipt contains unclassified mandatory Runtime .cs changes.' }
    if ([int]$receipt.mandatoryRuntimeAudit.productNativeAddedPhysicalLines -gt [int]$domains.mandatoryRuntimeAudit.targetProductNativeAddedPhysicalLinesMax) {
        Add-Failure "Receipt mandatory Runtime ProductNative additions exceed the contract: $($receipt.mandatoryRuntimeAudit.productNativeAddedPhysicalLines)"
    }

    foreach ($receiptDomain in @($receipt.domains)) {
        $domainId = [string]$receiptDomain.id
        $contractDomain = @($domains.domains | Where-Object { ([string]$_.id).Equals($domainId, [StringComparison]::Ordinal) })[0]
        foreach ($optionalEvidenceName in @('plannedConsumers', 'dependentRoutes', 'observedPrototypeInputs')) {
            $optionalEvidenceRows = @($receiptDomain.PSObject.Properties[$optionalEvidenceName].Value)
            if (@($optionalEvidenceRows | Where-Object { $null -eq $_ }).Count -ne 0) {
                Add-Failure "Receipt domain $domainId contains a null $optionalEvidenceName row instead of an empty array."
            }
        }
        $consumerEvidence = @($receiptDomain.currentConsumerEvidence)
        $baselineRealConsumerCount = [int](Get-BaselinePropertyValue $contractDomain 'realConsumerCount')
        $baselineCurrentConsumers = @(Get-BaselinePropertyValue $contractDomain 'currentConsumers')
        if ($consumerEvidence.Count -ne $baselineRealConsumerCount) { Add-Failure "Receipt domain $domainId consumer evidence count does not match baseline realConsumerCount." }
        Assert-ExactSet "Receipt domain $domainId consumer evidence identities" @($consumerEvidence | ForEach-Object { [string]$_.uniqueId }) $baselineCurrentConsumers
        foreach ($evidence in $consumerEvidence) {
            if (-not [bool]$evidence.qualifiesAsIndependentRealConsumer) { Add-Failure "Receipt domain $domainId current consumer is not independently qualified: $($evidence.uniqueId)" }
            if (@($evidence.evidenceKinds).Count -eq 0) { Add-Failure "Receipt domain $domainId current consumer has no evidence kind: $($evidence.uniqueId)" }
            $catalogMatches = @($catalog.products | Where-Object { ([string]$_.catalogId).Equals([string]$evidence.catalogId, [StringComparison]::Ordinal) -and ([string]$_.uniqueId).Equals([string]$evidence.uniqueId, [StringComparison]::Ordinal) })
            if ($catalogMatches.Count -ne 1) { Add-Failure "Receipt domain $domainId current consumer evidence no longer binds to one Catalog row: $($evidence.uniqueId)" }
        }
    }

    if (-not $g2Activated) {
        $driftArguments = New-Object System.Collections.Generic.List[string]
        foreach ($value in @('diff', '--name-only', $resolvedAuditCommit, '--')) { $driftArguments.Add($value) }
        foreach ($rootValue in $expectedMandatoryRoots) { $driftArguments.Add($rootValue) }
        $mandatoryTrackedDrift = @(Invoke-GitLines $driftArguments.ToArray() | ForEach-Object { Normalize-Path $_ } | Where-Object { $_.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) })
        if ($mandatoryTrackedDrift.Count -gt 0) { Add-Failure "Mandatory Runtime tracked .cs drift exists after the audited commit: $($mandatoryTrackedDrift -join ', ')" }
        $untrackedArguments = New-Object System.Collections.Generic.List[string]
        foreach ($value in @('ls-files', '--others', '--exclude-standard', '--')) { $untrackedArguments.Add($value) }
        foreach ($rootValue in $expectedMandatoryRoots) { $untrackedArguments.Add($rootValue) }
        $mandatoryUntracked = @(Invoke-GitLines $untrackedArguments.ToArray() | ForEach-Object { Normalize-Path $_ } | Where-Object { $_.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) })
        if ($mandatoryUntracked.Count -gt 0) { Add-Failure "Mandatory Runtime untracked .cs files exist after the audited commit: $($mandatoryUntracked -join ', ')" }
    }

    $repoPrefix = [System.IO.Path]::GetFullPath($repo).TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
    if ($receiptPathFull.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        $receiptRelativePath = Normalize-Path $receiptPathFull.Substring($repoPrefix.Length)
        if (Test-GitCommand @('ls-files', '--error-unmatch', '--', $receiptRelativePath)) {
            $receiptStatus = @(Invoke-GitLines @('status', '--porcelain', '--', $receiptRelativePath))
            if ($receiptStatus.Count -eq 0) {
                $receiptCommitTimeText = ([string](Invoke-GitLines @('log', '-1', '--format=%cI', '--', $receiptRelativePath) | Select-Object -First 1)).Trim()
                $receiptCommitInstant = ConvertTo-UtcDateTimeOffset $receiptCommitTimeText
                if ($null -eq $receiptCommitInstant) { Add-Failure "Unable to resolve the receipt-containing commit time: $receiptRelativePath" }
                elseif ($null -ne $captureInstant -and $captureInstant -gt $receiptCommitInstant) { Add-Failure 'Receipt capture is later than the commit containing the current receipt.' }
            }
            else {
                Write-Warning 'Receipt is not yet committed; the containing-commit upper-bound check must be rerun after commit.'
            }
        }
    }

    if ($failures.Count -eq 0) {
        try {
            & "$PSScriptRoot\build-batch6-phase0-baseline.ps1" -ContractPath $domainPath -GitRef $resolvedSourceCommit -AuditGitRef $resolvedAuditCommit -CapturedAtUtc ([string]$receipt.capturedAtUtc) -OutputPath $receiptPathFull -Check
            if (-not $?) { Add-Failure 'Batch 6 Phase 0 baseline reproducibility check failed.' }
        }
        catch {
            Add-Failure "Batch 6 Phase 0 baseline reproducibility check failed: $($_.Exception.Message)"
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }
    exit 1
}

$identityCount = @($identity.canonicalTerms.managedIdentities).Count + 1
$ownershipCategoryCount = @($identity.canonicalTerms.physicalOwnership).Count
$receiptDomainCount = if ($null -eq $receipt) { 'stale-skipped' } else { [string]$receipt.domainCount }
$receiptBatch5FileCount = if ($null -eq $receipt) { 'stale-skipped' } else { [string]$receipt.batch5ClassifiedFileCount }
Write-Host "Batch 6 Phase 0 historical contract OK: identities=$identityCount; ownershipCategories=$ownershipCategoryCount; domains=$receiptDomainCount; Batch5Files=$receiptBatch5FileCount; currentG2State=$g2State; Phase0Receipt=reproduced."
