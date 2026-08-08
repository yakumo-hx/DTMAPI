param(
    [string] $ContractPath = '',
    [string] $GitRef = '',
    [string] $AuditGitRef = '',
    [string] $CapturedAtUtc = '',
    [string] $OutputPath = '',
    [switch] $Check
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

function Resolve-RepoPath([string] $path, [string] $defaultRelativePath) {
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

function Write-Utf8NoBomText([string] $path, [string] $text) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $text, $encoding)
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
    if (-not [DateTimeOffset]::TryParse([string]$value, [Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$parsed)) {
        throw "CapturedAtUtc must be an ISO-8601 timestamp: $value"
    }
    return $parsed.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
}

function ConvertTo-UtcDateTimeOffset([object] $value, [string] $label) {
    $parsed = [DateTimeOffset]::MinValue
    $styles = [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal
    if (-not [DateTimeOffset]::TryParse([string]$value, [Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$parsed)) {
        throw "$label must be an ISO-8601 timestamp: $value"
    }
    return $parsed.ToUniversalTime()
}

function Sort-OrdinalStrings([object[]] $values) {
    [string[]]$result = @($values | ForEach-Object { [string]$_ })
    [Array]::Sort($result, [StringComparer]::Ordinal)
    return $result
}

function Get-OrdinalUniqueStrings([object[]] $values) {
    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($value in @($values)) { [void]$set.Add([string]$value) }
    return @(Sort-OrdinalStrings @($set))
}

function Get-Sha256Text([string] $text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
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

function Get-GitCommitUtc([string] $commit) {
    $value = ([string](Invoke-GitLines @('show', '-s', '--format=%cI', $commit) | Select-Object -First 1)).Trim()
    return ConvertTo-CanonicalUtcTimestamp $value
}

function Get-TrackedFilesAtCommit([string] $commit) {
    $candidates = @(Invoke-GitLines @('ls-tree', '-r', '--name-only', $commit) |
        ForEach-Object { Normalize-Path $_ } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    return @(Get-OrdinalUniqueStrings $candidates)
}

$contractPathFull = Resolve-RepoPath $ContractPath 'tools\release\contracts\batch6-phase0-domain-contract.json'
$contract = Read-Utf8Text $contractPathFull | ConvertFrom-Json
if ([int]$contract.schemaVersion -ne 2) { throw "Batch 6 Phase 0 domain contract schema must be 2; found $($contract.schemaVersion)." }
if ([string]::IsNullOrWhiteSpace($GitRef)) { $GitRef = [string]$contract.baseline.gitCommit }
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = [string]$contract.baseline.receipt }
$outputPathFull = Resolve-RepoPath $OutputPath ([string]$contract.baseline.receipt)
$existingReceipt = $null
if ($Check -and (Test-Path -LiteralPath $outputPathFull -PathType Leaf)) {
    try { $existingReceipt = Read-Utf8Text $outputPathFull | ConvertFrom-Json }
    catch { throw "Batch 6 Phase 0 baseline receipt is not parseable: $outputPathFull. $($_.Exception.Message)" }
}
if ([string]::IsNullOrWhiteSpace($AuditGitRef)) {
    if ($Check -and $null -ne $existingReceipt -and -not [string]::IsNullOrWhiteSpace([string]$existingReceipt.auditedGitCommit)) {
        $AuditGitRef = [string]$existingReceipt.auditedGitCommit
    }
    else {
        $AuditGitRef = 'HEAD'
    }
}
if ([string]::IsNullOrWhiteSpace($CapturedAtUtc)) {
    if ($Check -and $null -ne $existingReceipt -and -not [string]::IsNullOrWhiteSpace([string]$existingReceipt.capturedAtUtc)) {
        $CapturedAtUtc = [string]$existingReceipt.capturedAtUtc
    }
    else {
        $CapturedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
    }
}
$CapturedAtUtc = ConvertTo-CanonicalUtcTimestamp $CapturedAtUtc

$resolvedCommit = ([string](Invoke-GitLines @('rev-parse', "$GitRef^{commit}") | Select-Object -First 1)).Trim()
$resolvedAuditCommit = ([string](Invoke-GitLines @('rev-parse', "$AuditGitRef^{commit}") | Select-Object -First 1)).Trim()
$sourceCommitTimeUtc = Get-GitCommitUtc $resolvedCommit
$auditedCommitTimeUtc = Get-GitCommitUtc $resolvedAuditCommit
$capturedAtInstant = ConvertTo-UtcDateTimeOffset $CapturedAtUtc 'CapturedAtUtc'
$sourceCommitInstant = ConvertTo-UtcDateTimeOffset $sourceCommitTimeUtc 'Source commit time'
$auditedCommitInstant = ConvertTo-UtcDateTimeOffset $auditedCommitTimeUtc 'Audited commit time'
$nowUtc = [DateTimeOffset]::UtcNow
if ($capturedAtInstant -lt $sourceCommitInstant) { throw "CapturedAtUtc precedes source commit time: capture=$CapturedAtUtc source=$sourceCommitTimeUtc" }
if ($capturedAtInstant -lt $auditedCommitInstant) { throw "CapturedAtUtc precedes audited commit time: capture=$CapturedAtUtc audit=$auditedCommitTimeUtc" }
if ($capturedAtInstant -gt $nowUtc) { throw "CapturedAtUtc is in the future: capture=$CapturedAtUtc now=$($nowUtc.ToString('o'))" }

$trackedFiles = @(Get-TrackedFilesAtCommit $resolvedCommit)
$trackedCsFiles = @($trackedFiles | Where-Object { $_.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) })
$auditedTrackedFiles = @(Get-TrackedFilesAtCommit $resolvedAuditCommit)
$auditedTrackedCsFiles = @($auditedTrackedFiles | Where-Object { $_.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) })

$sourceCache = @{}
function Get-SourceReceiptAtCommit([string] $commit, [string] $path) {
    $normalized = Normalize-Path $path
    $cacheKey = $commit + ':' + $normalized
    if ($sourceCache.ContainsKey($cacheKey)) { return $sourceCache[$cacheKey] }
    $lines = @(Invoke-GitLines @('show', ($commit + ':' + $normalized)))
    $blob = ([string](Invoke-GitLines @('rev-parse', ($commit + ':' + $normalized)) | Select-Object -First 1)).Trim()
    $row = [ordered]@{
        path = $normalized
        physicalLines = [int]$lines.Count
        gitBlob = $blob
    }
    $sourceCache[$cacheKey] = $row
    return $row
}

function Get-SourceReceipt([string] $path) {
    return Get-SourceReceiptAtCommit $resolvedCommit $path
}

function Get-FilesForScopes([object[]] $scopePaths, [string] $domainId, [string] $category) {
    $selected = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($scopeValue in @($scopePaths)) {
        $scope = Normalize-Path ([string]$scopeValue)
        if ([string]::IsNullOrWhiteSpace($scope)) { throw "Domain $domainId category $category contains an empty source scope." }
        $matches = @($trackedCsFiles | Where-Object {
            $_.Equals($scope, [StringComparison]::OrdinalIgnoreCase) -or
            $_.StartsWith($scope + '/', [StringComparison]::OrdinalIgnoreCase)
        })
        if ($matches.Count -eq 0) { throw "Domain $domainId category $category scope has no tracked .cs file at ${resolvedCommit}: $scope" }
        foreach ($match in $matches) { [void]$selected.Add($match) }
    }
    return @(Sort-OrdinalStrings @($selected))
}

$catalogRelativePath = 'tools/release/dtmapi-product-catalog.json'
$catalogText = @(Invoke-GitLines @('show', ($resolvedAuditCommit + ':' + $catalogRelativePath))) -join "`n"
try { $catalog = $catalogText | ConvertFrom-Json }
catch { throw "Batch 6 Phase 0 audited Catalog is not parseable at $resolvedAuditCommit`: $catalogRelativePath. $($_.Exception.Message)" }

function Get-OptionalPropertyValue([object] $value, [string] $name) {
    if ($null -eq $value) { return $null }
    $property = $value.PSObject.Properties[$name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-BaselinePropertyValue([object] $domain, [object] $baselineProjection, [string] $name) {
    if ($null -ne $baselineProjection) {
        $baselineProperty = $baselineProjection.PSObject.Properties[$name]
        if ($null -ne $baselineProperty) { return $baselineProperty.Value }
    }
    $domainProperty = $domain.PSObject.Properties[$name]
    if ($null -eq $domainProperty) { return $null }
    return $domainProperty.Value
}

function Test-TrackedPathAtAudit([string] $path) {
    $normalized = Normalize-Path $path
    if ([string]::IsNullOrWhiteSpace($normalized)) { return $false }
    foreach ($trackedPath in $auditedTrackedFiles) {
        if ($trackedPath.Equals($normalized, [StringComparison]::OrdinalIgnoreCase) -or
            $trackedPath.StartsWith($normalized + '/', [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Get-CurrentConsumerEvidence([string] $consumerUniqueId, [string] $domainId) {
    $matches = @($catalog.products | Where-Object {
        ([string]$_.uniqueId).Equals($consumerUniqueId, [StringComparison]::Ordinal)
    })
    if ($matches.Count -ne 1) {
        throw "Domain $domainId current consumer must match exactly one Catalog UniqueID: '$consumerUniqueId' matched $($matches.Count)."
    }

    $product = $matches[0]
    $catalogId = [string]$product.catalogId
    if (([string](Get-OptionalPropertyValue $product 'identityState')).Equals('FrozenReservedNoArtifact', [StringComparison]::Ordinal)) {
        throw "Domain $domainId current consumer is a reserved no-artifact Catalog identity: $catalogId ($consumerUniqueId)."
    }

    $evidenceKinds = New-Object System.Collections.Generic.List[string]
    $sourceRoot = Normalize-Path ([string](Get-OptionalPropertyValue $product 'sourceRoot'))
    $sourceManifest = Normalize-Path ([string](Get-OptionalPropertyValue $product 'sourceManifest'))
    $sourceManifestBlob = ''
    if (-not [string]::IsNullOrWhiteSpace($sourceRoot)) {
        if (-not (Test-TrackedPathAtAudit $sourceRoot)) {
            throw "Domain $domainId current consumer Catalog sourceRoot is absent at audited commit $resolvedAuditCommit`: $catalogId -> $sourceRoot"
        }
        $evidenceKinds.Add('tracked-source')

        if (-not [string]::IsNullOrWhiteSpace($sourceManifest)) {
            if (-not ($auditedTrackedFiles -contains $sourceManifest)) {
                throw "Domain $domainId current consumer Catalog sourceManifest is absent at audited commit $resolvedAuditCommit`: $catalogId -> $sourceManifest"
            }
            $manifestText = (Invoke-GitLines @('show', ($resolvedAuditCommit + ':' + $sourceManifest))) -join "`n"
            try { $manifest = $manifestText | ConvertFrom-Json }
            catch { throw "Domain $domainId current consumer manifest is not parseable at audited commit: $sourceManifest. $($_.Exception.Message)" }
            if (-not ([string]$manifest.UniqueID).Equals($consumerUniqueId, [StringComparison]::Ordinal)) {
                throw "Domain $domainId current consumer manifest UniqueID mismatch: expected=$consumerUniqueId path=$sourceManifest actual=$($manifest.UniqueID)"
            }
            $sourceManifestBlob = [string](Get-SourceReceiptAtCommit $resolvedAuditCommit $sourceManifest).gitBlob
        }
    }

    $retainedTreeSha256 = ''
    $retainedArtifact = Get-OptionalPropertyValue $product 'retainedArtifact'
    if ($null -ne $retainedArtifact -and
        -not [string]::IsNullOrWhiteSpace([string](Get-OptionalPropertyValue $retainedArtifact 'treeSha256')) -and
        [int64](Get-OptionalPropertyValue $retainedArtifact 'fileCount') -gt 0) {
        $retainedTreeSha256 = ([string](Get-OptionalPropertyValue $retainedArtifact 'treeSha256')).ToLowerInvariant()
        $evidenceKinds.Add('retained-artifact')
    }

    if ($evidenceKinds.Count -eq 0) {
        throw "Domain $domainId current consumer has no Catalog-bound tracked source or retained artifact evidence: $catalogId ($consumerUniqueId)."
    }

    return [ordered]@{
        uniqueId = $consumerUniqueId
        catalogId = $catalogId
        catalogRole = [string]$product.role
        evidenceKinds = @(Sort-OrdinalStrings @($evidenceKinds.ToArray()))
        trackedSourceRoot = $sourceRoot
        trackedSourceManifest = $sourceManifest
        trackedSourceManifestBlob = $sourceManifestBlob
        retainedTreeSha256 = $retainedTreeSha256
        qualifiesAsIndependentRealConsumer = $true
    }
}

$domainRows = New-Object System.Collections.Generic.List[object]
foreach ($domain in @($contract.domains)) {
    $baselineProjection = if ($null -ne $domain.PSObject.Properties['baselineProjection']) {
        $domain.baselineProjection
    }
    else {
        $null
    }
    $baselineCurrentConsumers = @(Get-BaselinePropertyValue $domain $baselineProjection 'currentConsumers')
    $baselineRealConsumerCount = [int](Get-BaselinePropertyValue $domain $baselineProjection 'realConsumerCount')
    $consumerEvidenceRows = New-Object System.Collections.Generic.List[object]
    foreach ($consumerUniqueId in $baselineCurrentConsumers) {
        $consumerEvidenceRows.Add((Get-CurrentConsumerEvidence ([string]$consumerUniqueId) ([string]$domain.id)))
    }
    if ($baselineRealConsumerCount -ne $consumerEvidenceRows.Count) {
        throw "Domain $($domain.id) baseline realConsumerCount does not match Catalog-bound current consumer evidence count. Declared=$baselineRealConsumerCount Evidence=$($consumerEvidenceRows.Count)."
    }

    $baselineScopes = if ($null -ne $domain.PSObject.Properties['baselineSourceScopes']) {
        $domain.baselineSourceScopes
    }
    else {
        $domain.sourceScopes
    }
    $categoryRows = [ordered]@{}
    $uniqueDomainFiles = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($category in @('corePlatform', 'gameBridge', 'product', 'qa', 'compatibility')) {
        $scopeProperty = $baselineScopes.PSObject.Properties[$category]
        if ($null -eq $scopeProperty) { throw "Domain $($domain.id) is missing baseline source scope $category." }
        $paths = @(Get-FilesForScopes @($scopeProperty.Value) ([string]$domain.id) $category)
        $files = New-Object System.Collections.Generic.List[object]
        foreach ($path in $paths) {
            [void]$uniqueDomainFiles.Add($path)
            $files.Add((Get-SourceReceipt $path))
        }
        $lines = 0
        foreach ($fileRow in $files.ToArray()) { $lines += [int]$fileRow.physicalLines }
        $treeText = (@($files | ForEach-Object { "$($_.path)|$($_.physicalLines)|$($_.gitBlob)" }) -join "`n")
        $categoryRows[$category] = [ordered]@{
            scopePaths = @($scopeProperty.Value | ForEach-Object { Normalize-Path ([string]$_) })
            fileCount = [int]$files.Count
            physicalLines = $lines
            treeSha256 = Get-Sha256Text $treeText
            files = @($files.ToArray())
        }
    }
    $uniquePaths = @(Sort-OrdinalStrings @($uniqueDomainFiles))
    $uniqueRows = @($uniquePaths | ForEach-Object { Get-SourceReceipt $_ })
    $uniqueLines = 0
    foreach ($fileRow in $uniqueRows) { $uniqueLines += [int]$fileRow.physicalLines }
    $domainRows.Add([ordered]@{
        id = [string]$domain.id
        reviewDisposition = [string](Get-BaselinePropertyValue $domain $baselineProjection 'reviewDisposition')
        physicalOwnershipCandidates = @(Get-BaselinePropertyValue $domain $baselineProjection 'physicalOwnershipCandidates')
        decisionState = [string](Get-BaselinePropertyValue $domain $baselineProjection 'decisionState')
        targetDeploymentForms = @(Get-BaselinePropertyValue $domain $baselineProjection 'targetDeploymentForms')
        realConsumerCount = $baselineRealConsumerCount
        currentConsumers = $baselineCurrentConsumers
        currentConsumerEvidence = @($consumerEvidenceRows.ToArray())
        plannedConsumers = @((Get-OptionalPropertyValue $domain 'plannedConsumers') | Where-Object { $null -ne $_ })
        dependentRoutes = @((Get-OptionalPropertyValue $domain 'dependentRoutes') | Where-Object { $null -ne $_ })
        observedPrototypeInputs = @((Get-OptionalPropertyValue $domain 'observedPrototypeInputs') | Where-Object { $null -ne $_ })
        nativeOwner = [string]$domain.nativeOwner
        stateHolder = [string](Get-BaselinePropertyValue $domain $baselineProjection 'stateHolder')
        targetAssembly = [string](Get-BaselinePropertyValue $domain $baselineProjection 'targetAssembly')
        targetMandatoryLoad = [bool]$domain.targetMandatoryLoad
        targetMandatoryRuntimeProductLocDeltaMax = [int]$domain.targetMandatoryRuntimeProductLocDeltaMax
        apiDelta = [string](Get-BaselinePropertyValue $domain $baselineProjection 'apiDelta')
        hookAndLifetime = [string](Get-BaselinePropertyValue $domain $baselineProjection 'hookAndLifetime')
        marginalPlan = [string](Get-BaselinePropertyValue $domain $baselineProjection 'marginalPlan')
        uniqueFileCount = [int]$uniqueRows.Count
        uniquePhysicalLines = $uniqueLines
        categories = $categoryRows
    })
}

$productConsumerFiles = @($trackedCsFiles | Where-Object {
    $_.StartsWith('first-party-mods/', [StringComparison]::OrdinalIgnoreCase) -or
    $_.StartsWith('testmods/', [StringComparison]::OrdinalIgnoreCase)
})
$consumerTextCache = @{}
function Get-ConsumerText([string] $path) {
    if ($consumerTextCache.ContainsKey($path)) { return [string]$consumerTextCache[$path] }
    $text = (Invoke-GitLines @('show', ($resolvedCommit + ':' + $path))) -join "`n"
    $consumerTextCache[$path] = $text
    return $text
}

$compatibilityRows = New-Object System.Collections.Generic.List[object]
foreach ($surface in @($contract.compatibilitySurfaces)) {
    $symbolRows = New-Object System.Collections.Generic.List[object]
    $allMatches = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($symbolValue in @($surface.symbols)) {
        $symbol = [string]$symbolValue
        $matches = @(Sort-OrdinalStrings @($productConsumerFiles | Where-Object { (Get-ConsumerText $_).IndexOf($symbol, [StringComparison]::Ordinal) -ge 0 }))
        foreach ($match in $matches) { [void]$allMatches.Add($match) }
        $symbolRows.Add([ordered]@{ symbol = $symbol; trackedProductSourceMatches = $matches })
    }
    [object[]]$baselineKnownTrackedConsumers = @()
    if ($null -ne $surface.PSObject.Properties['baselineKnownTrackedConsumers']) {
        $baselineKnownTrackedConsumers = @($surface.baselineKnownTrackedConsumers)
    }
    else {
        $baselineKnownTrackedConsumers = @($surface.knownTrackedConsumers)
    }
    $baselineRetainedArtifactConsumer =
        if ($null -ne $surface.PSObject.Properties['baselineRetainedArtifactConsumer']) {
            [string]$surface.baselineRetainedArtifactConsumer
        }
        else {
            [string]$surface.retainedArtifactConsumer
        }
    $baselinePlan =
        if ($null -ne $surface.PSObject.Properties['baselinePlan']) {
            [string]$surface.baselinePlan
        }
        else {
            [string]$surface.plan
        }
    $compatibilityRows.Add([ordered]@{
        id = [string]$surface.id
        symbols = @($symbolRows.ToArray())
        trackedProductSourceMatches = @(Sort-OrdinalStrings @($allMatches))
        declaredKnownTrackedConsumers = $baselineKnownTrackedConsumers
        retainedArtifactConsumer = $baselineRetainedArtifactConsumer
        plan = $baselinePlan
    })
}

$batch5FileCandidates = New-Object System.Collections.Generic.List[string]
foreach ($sourceRangeValue in @($contract.batch5Classification.sourceCommitRanges)) {
    foreach ($changedPath in @(Invoke-GitLines @('diff', '--name-only', [string]$sourceRangeValue))) {
        $normalizedChangedPath = Normalize-Path $changedPath
        if ($normalizedChangedPath.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) -or $normalizedChangedPath.EndsWith('.ps1', [StringComparison]::OrdinalIgnoreCase)) {
            $batch5FileCandidates.Add($normalizedChangedPath)
        }
    }
}
$batch5Files = @(Get-OrdinalUniqueStrings @($batch5FileCandidates.ToArray()))
$batch5Rows = New-Object System.Collections.Generic.List[object]
foreach ($path in $batch5Files) {
    $matches = New-Object System.Collections.Generic.List[object]
    foreach ($rule in @($contract.batch5Classification.rules)) {
        $classificationPrefixes = if ($null -ne $rule.PSObject.Properties['baselinePathPrefixes']) {
            @($rule.baselinePathPrefixes)
        }
        else {
            @($rule.pathPrefixes)
        }
        foreach ($prefixValue in $classificationPrefixes) {
            $rawPrefix = ([string]$prefixValue).Trim()
            $isDirectoryPrefix = $rawPrefix.EndsWith('/', [StringComparison]::Ordinal) -or $rawPrefix.EndsWith('\', [StringComparison]::Ordinal)
            $prefix = Normalize-Path ([string]$prefixValue)
            $isPrefixMatch = if ($isDirectoryPrefix) {
                $path.StartsWith($prefix + '/', [StringComparison]::OrdinalIgnoreCase)
            }
            else {
                $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
            }
            if ($path.Equals($prefix, [StringComparison]::OrdinalIgnoreCase) -or $isPrefixMatch) {
                $matches.Add($rule)
                break
            }
        }
    }
    if ($matches.Count -eq 0) { throw "Batch 5 source/tooling file has no Phase 0 classification rule: $path" }
    $selected = $matches[0]
    $batch5Rows.Add([ordered]@{
        path = $path
        ruleId = [string]$selected.id
        classification = [string]$selected.classification
        targetAction = [string]$selected.targetAction
        additionalMatchingRuleIds = @($matches | Select-Object -Skip 1 | ForEach-Object { [string]$_.id })
    })
}

function Get-FilesUnderRoots([object[]] $candidateFiles, [object[]] $rootValues) {
    $selected = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($rootValue in @($rootValues)) {
        $rootPath = Normalize-Path ([string]$rootValue)
        if ([string]::IsNullOrWhiteSpace($rootPath)) { throw 'mandatoryRuntimeAudit.roots contains an empty path.' }
        foreach ($candidatePath in @($candidateFiles)) {
            if ($candidatePath.Equals($rootPath, [StringComparison]::OrdinalIgnoreCase) -or
                $candidatePath.StartsWith($rootPath + '/', [StringComparison]::OrdinalIgnoreCase)) {
                [void]$selected.Add($candidatePath)
            }
        }
    }
    return @(Sort-OrdinalStrings @($selected))
}

function Get-DiffNumStatForPath([string] $baselineCommit, [string] $auditCommit, [string] $path) {
    $added = 0
    $deleted = 0
    foreach ($line in @(Invoke-GitLines @('diff', '--numstat', '--no-renames', $baselineCommit, $auditCommit, '--', $path))) {
        $parts = @([string]$line -split "`t")
        if ($parts.Count -lt 3) { throw "Unexpected git diff --numstat row for ${path}: $line" }
        if ($parts[0] -eq '-' -or $parts[1] -eq '-') { throw "Binary content appeared in mandatory Runtime .cs audit: $path" }
        $added += [int]$parts[0]
        $deleted += [int]$parts[1]
    }
    return [ordered]@{ added = $added; deleted = $deleted }
}

function Get-SourceTreeSummary([string] $commit, [object[]] $paths) {
    $rows = New-Object System.Collections.Generic.List[object]
    $physicalLines = 0
    foreach ($path in @($paths)) {
        $row = Get-SourceReceiptAtCommit $commit ([string]$path)
        $rows.Add($row)
        $physicalLines += [int]$row.physicalLines
    }
    $treeText = (@($rows | ForEach-Object { "$($_.path)|$($_.physicalLines)|$($_.gitBlob)" }) -join "`n")
    return [ordered]@{
        fileCount = [int]$rows.Count
        physicalLines = $physicalLines
        treeSha256 = Get-Sha256Text $treeText
    }
}

$mandatoryRoots = @($contract.mandatoryRuntimeAudit.roots | ForEach-Object { Normalize-Path ([string]$_) })
if ($mandatoryRoots.Count -eq 0) { throw 'mandatoryRuntimeAudit.roots must not be empty.' }
$baselineMandatoryFiles = @(Get-FilesUnderRoots $trackedCsFiles $mandatoryRoots)
$auditedMandatoryFiles = @(Get-FilesUnderRoots $auditedTrackedCsFiles $mandatoryRoots)
$mandatoryFileSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($path in $baselineMandatoryFiles) { [void]$mandatoryFileSet.Add($path) }
foreach ($path in $auditedMandatoryFiles) { [void]$mandatoryFileSet.Add($path) }
$allMandatoryFiles = @(Sort-OrdinalStrings @($mandatoryFileSet))

$allowedClassifications = @($contract.mandatoryRuntimeAudit.allowedOwnershipClassifications | ForEach-Object { [string]$_ })
$allowanceByPath = @{}
foreach ($allowance in @($contract.mandatoryRuntimeAudit.semanticPathAllowances)) {
    $allowancePath = Normalize-Path ([string]$allowance.path)
    if ([string]::IsNullOrWhiteSpace($allowancePath)) { throw 'mandatoryRuntimeAudit.semanticPathAllowances contains an empty path.' }
    if ($allowanceByPath.ContainsKey($allowancePath)) { throw "Duplicate mandatory Runtime semantic path allowance: $allowancePath" }
    if ($allowedClassifications -cnotcontains [string]$allowance.classification) {
        throw "Mandatory Runtime semantic path allowance has an invalid classification: $allowancePath -> $($allowance.classification)"
    }
    $reviewPath = Normalize-Path ([string]$allowance.review)
    if ([string]::IsNullOrWhiteSpace($reviewPath) -or -not (Test-Path -LiteralPath (Join-Path $repo $reviewPath) -PathType Leaf)) {
        throw "Mandatory Runtime semantic path allowance review is missing: $allowancePath -> $reviewPath"
    }
    $allowanceByPath[$allowancePath] = $allowance
}

$mandatoryChangedRows = New-Object System.Collections.Generic.List[object]
$unclassifiedChangedFileCount = 0
$productNativeAddedPhysicalLines = 0
foreach ($path in $allMandatoryFiles) {
    $hasBaseline = $baselineMandatoryFiles -contains $path
    $hasAudit = $auditedMandatoryFiles -contains $path
    $baselineRow = if ($hasBaseline) { Get-SourceReceiptAtCommit $resolvedCommit $path } else { $null }
    $auditedRow = if ($hasAudit) { Get-SourceReceiptAtCommit $resolvedAuditCommit $path } else { $null }
    $status = if (-not $hasBaseline) {
        'Added'
    }
    elseif (-not $hasAudit) {
        'Deleted'
    }
    elseif (-not ([string]$baselineRow.gitBlob).Equals([string]$auditedRow.gitBlob, [StringComparison]::Ordinal)) {
        'Modified'
    }
    else {
        'Unchanged'
    }
    if ($status -eq 'Unchanged') { continue }

    $numStat = Get-DiffNumStatForPath $resolvedCommit $resolvedAuditCommit $path
    $allowance = if ($allowanceByPath.ContainsKey($path)) { $allowanceByPath[$path] } else { $null }
    $classification = if ($null -eq $allowance) { 'Unclassified' } else { [string]$allowance.classification }
    $review = if ($null -eq $allowance) { '' } else { Normalize-Path ([string]$allowance.review) }
    $reason = if ($null -eq $allowance) { '' } else { [string]$allowance.reason }
    if ($classification -eq 'Unclassified') { $unclassifiedChangedFileCount++ }
    if ($classification -eq 'ProductNative') { $productNativeAddedPhysicalLines += [int]$numStat.added }

    $mandatoryChangedRows.Add([ordered]@{
        path = $path
        status = $status
        classification = $classification
        review = $review
        reason = $reason
        baselineBlob = if ($null -eq $baselineRow) { $null } else { [string]$baselineRow.gitBlob }
        auditedBlob = if ($null -eq $auditedRow) { $null } else { [string]$auditedRow.gitBlob }
        baselinePhysicalLines = if ($null -eq $baselineRow) { 0 } else { [int]$baselineRow.physicalLines }
        auditedPhysicalLines = if ($null -eq $auditedRow) { 0 } else { [int]$auditedRow.physicalLines }
        addedPhysicalLines = [int]$numStat.added
        deletedPhysicalLines = [int]$numStat.deleted
    })
}

if ($unclassifiedChangedFileCount -gt 0) {
    $paths = @($mandatoryChangedRows | Where-Object { $_.classification -eq 'Unclassified' } | ForEach-Object { $_.path })
    throw "Mandatory Runtime source delta contains unclassified .cs files: $($paths -join ', ')"
}
$productNativeAddedPhysicalLinesMax = [int]$contract.mandatoryRuntimeAudit.targetProductNativeAddedPhysicalLinesMax
if ($productNativeAddedPhysicalLines -gt $productNativeAddedPhysicalLinesMax) {
    throw "Mandatory Runtime ProductNative added physical lines exceed the contract: actual=$productNativeAddedPhysicalLines max=$productNativeAddedPhysicalLinesMax"
}
$baselineMandatorySummary = Get-SourceTreeSummary $resolvedCommit $baselineMandatoryFiles
$auditedMandatorySummary = Get-SourceTreeSummary $resolvedAuditCommit $auditedMandatoryFiles
$mandatoryAuditReceipt = [ordered]@{
    roots = $mandatoryRoots
    baselineFileCount = [int]$baselineMandatorySummary.fileCount
    baselinePhysicalLines = [int]$baselineMandatorySummary.physicalLines
    baselineTreeSha256 = [string]$baselineMandatorySummary.treeSha256
    auditedFileCount = [int]$auditedMandatorySummary.fileCount
    auditedPhysicalLines = [int]$auditedMandatorySummary.physicalLines
    auditedTreeSha256 = [string]$auditedMandatorySummary.treeSha256
    changedFileCount = [int]$mandatoryChangedRows.Count
    unclassifiedChangedFileCount = $unclassifiedChangedFileCount
    productNativeAddedPhysicalLines = $productNativeAddedPhysicalLines
    targetProductNativeAddedPhysicalLinesMax = $productNativeAddedPhysicalLinesMax
    changedFiles = @($mandatoryChangedRows.ToArray())
}

$receipt = [ordered]@{
    schemaVersion = 2
    contractId = [string]$contract.contractId
    gitCommit = $resolvedCommit
    sourceCommitTimeUtc = $sourceCommitTimeUtc
    auditedGitCommit = $resolvedAuditCommit
    auditedCommitTimeUtc = $auditedCommitTimeUtc
    capturedAtUtc = $CapturedAtUtc
    projectScope = "Batch 6 Phase 0 ownership domains at tracked source commit"
    includeRule = [string]$contract.baseline.includeRule
    excludeRules = @($contract.baseline.excludeRules)
    reproduceCommand = "pwsh -NoProfile -File tools/scripts/build-batch6-phase0-baseline.ps1 -GitRef $resolvedCommit -AuditGitRef $resolvedAuditCommit -CapturedAtUtc $CapturedAtUtc -OutputPath $([string]$contract.baseline.receipt)"
    domainCount = [int]$domainRows.Count
    domains = @($domainRows.ToArray())
    compatibilityConsumerScan = @($compatibilityRows.ToArray())
    mandatoryRuntimeAudit = $mandatoryAuditReceipt
    batch5SourceRanges = @($contract.batch5Classification.sourceCommitRanges)
    batch5SourceRangeNote = [string]$contract.batch5Classification.rangeNote
    batch5ClassifiedFileCount = [int]$batch5Rows.Count
    batch5ClassifiedFiles = @($batch5Rows.ToArray())
}
$json = $receipt | ConvertTo-Json -Depth 100

if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPathFull -PathType Leaf)) { throw "Batch 6 Phase 0 baseline receipt is missing: $outputPathFull" }
    $actual = (Read-Utf8Text $outputPathFull | ConvertFrom-Json) | ConvertTo-Json -Depth 100
    if (-not $actual.Equals($json, [StringComparison]::Ordinal)) {
        throw "Batch 6 Phase 0 baseline receipt is stale. Rebuild it with: $($receipt.reproduceCommand)"
    }
    Write-Host "Batch 6 Phase 0 baseline receipt is reproducible: domains=$($domainRows.Count); batch5Files=$($batch5Rows.Count); commit=$resolvedCommit."
    return
}

$outputDirectory = Split-Path -Parent $outputPathFull
if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}
Write-Utf8NoBomText $outputPathFull ($json + [Environment]::NewLine)
Write-Host "Wrote Batch 6 Phase 0 baseline: $outputPathFull"
Write-Host "Domains=$($domainRows.Count); Batch5ClassifiedFiles=$($batch5Rows.Count); GitCommit=$resolvedCommit"
