function Test-SmokeMoreEquipmentSlotsOfficialPackage {
    param(
        [Parameter(Mandatory = $true)] [string] $ProductRoot,
        [Parameter(Mandatory = $true)] [string] $RepoRoot
    )

    $canonicalProductRoot = [System.IO.Path]::GetFullPath($ProductRoot).TrimEnd([char]92, [char]47)
    $contentRoot = Join-Path $canonicalProductRoot 'Content\DTMAPI'
    $infoPath = Join-Path $canonicalProductRoot 'info.json'
    $manifestPath = Join-Path $contentRoot 'manifest.json'
    $markerPath = Join-Path $contentRoot 'dtmapi-package.json'
    $receiptPath = Join-Path $contentRoot 'dtmapi-advanced-references.json'
    $entryDllPath = Join-Path $contentRoot 'DTMAPI.MoreEquipmentSlots.dll'
    $expectedFiles = @(
        'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll',
        'Content/DTMAPI/dtmapi-advanced-references.json',
        'Content/DTMAPI/dtmapi-package.json',
        'Content/DTMAPI/manifest.json',
        'i18n/english.json',
        'i18n/schinese.json',
        'info.json'
    )
    if (-not (Test-Path -LiteralPath $canonicalProductRoot -PathType Container)) {
        throw 'MoreEquipmentSlots cold recovery requires its SDK-generated official Local package.'
    }
    $rootItem = Get-Item -LiteralPath $canonicalProductRoot -Force
    $entries = @(Get-ChildItem -LiteralPath $canonicalProductRoot -Recurse -Force -ErrorAction Stop)
    if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        @($entries | Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 }).Count -ne 0) {
        throw 'MoreEquipmentSlots official Local package may not contain reparse points.'
    }
    [string[]]$relativeFiles = @($entries | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        $_.FullName.Substring($canonicalProductRoot.Length + 1).Replace([char]92, [char]47)
    })
    [string[]]$expectedFileSet = @($expectedFiles)
    [Array]::Sort($relativeFiles, [System.StringComparer]::Ordinal)
    [Array]::Sort($expectedFileSet, [System.StringComparer]::Ordinal)
    if (($relativeFiles -join '|') -cne ($expectedFileSet -join '|')) {
        throw 'MoreEquipmentSlots cold recovery refused a non-exact official Local package file set.'
    }
    foreach ($requiredPath in @($infoPath, $manifestPath, $markerPath, $receiptPath, $entryDllPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "MoreEquipmentSlots official Local package is missing a required file: $requiredPath"
        }
    }

    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $RepoRoot 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $catalogRows = @($catalog.products | Where-Object { [string]$_.catalogId -ceq 'more-equipment-slots' })
    if ($catalogRows.Count -ne 1) {
        throw 'MoreEquipmentSlots official package preflight requires one exact Catalog row.'
    }
    $product = $catalogRows[0]
    $policyId = [string]$product.referencePolicyId
    $registryPath = Join-Path $RepoRoot 'author-sdk\advanced-reference-policies\registry.json'
    $policyPath = Join-Path $RepoRoot ('author-sdk\advanced-reference-policies\' + $policyId + '.json')
    $registry = Get-Content -Raw -Encoding UTF8 -LiteralPath $registryPath | ConvertFrom-Json
    $registryRows = @($registry.policies | Where-Object { [string]$_.policyId -ceq $policyId })
    if ($registryRows.Count -ne 1) {
        throw "MoreEquipmentSlots official package preflight requires one exact policy row for $policyId."
    }
    $registryRow = $registryRows[0]
    $policy = Get-Content -Raw -Encoding UTF8 -LiteralPath $policyPath | ConvertFrom-Json
    $policySha256 = Get-SmokeFileSha256 -Path $policyPath
    if ([string]$product.uniqueId -cne 'DTMAPI.MoreEquipmentSlotsMod' -or
        [string]$product.sourceVersion -cne '1.0.0' -or
        [string]$product.sourceMinimumDtmApiVersion -cne '0.6.0' -or
        [string]$product.targetMinimumDtmApiVersion -cne '0.6.0' -or
        [string]$product.codeModKind -cne 'Advanced' -or
        [string]$product.canonicalHarmonyOwner -cne 'dtmapi.mod.dtmapi.moreequipmentslotsmod' -or
        [int]$registry.schemaVersion -ne 2 -or
        [string]$registryRow.requiredUniqueId -cne [string]$product.uniqueId -or
        [string]$registryRow.minimumDtmApiVersion -cne '0.6.0' -or
        -not (Test-SmokeSha256Equal -Actual ([string]$registryRow.policySha256) -Expected $policySha256) -or
        [int]$policy.schemaVersion -ne 1 -or
        [string]$policy.policyId -cne $policyId -or
        [int]$policy.policyVersion -ne 1) {
        throw 'MoreEquipmentSlots Catalog/registry/policy authority is inconsistent or understates the 0.6 Runtime floor.'
    }

    $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
    $receipt = Get-Content -Raw -Encoding UTF8 -LiteralPath $receiptPath | ConvertFrom-Json
    Assert-SmokeExactJsonPropertySet -Value $manifest -Expected @(
        'Name','Author','Version','Description','UniqueID','EntryDll','EntryType',
        'MinimumDTMApiVersion','Type','Dependencies','CodeModKind'
    ) -Context 'MoreEquipmentSlots manifest'
    Assert-SmokeExactJsonPropertySet -Value $marker -Expected @(
        'advancedReferenceReceiptPath','advancedReferenceReceiptSha256','authorSdkVersion','authority',
        'codeModKind','entryDllPath','entryDllSha256','manifestPath','manifestSha256','owner',
        'packageKind','schemaVersion','targetDtmApiVersion','uniqueId','version'
    ) -Context 'MoreEquipmentSlots SDK package marker'
    Assert-SmokeExactJsonPropertySet -Value $receipt -Expected @(
        'schemaVersion','receiptKind','referencePolicyId','referencePolicyVersion','referencePolicySha256',
        'uniqueId','codeModKind','targetFramework','gameBuildId','gameAssemblyRelativePath',
        'gameAssemblySha256','manifestPath','manifestSha256','entryDllPath','entryDllLength',
        'entryDllSha256','harmonyOwner','references'
    ) -Context 'MoreEquipmentSlots Advanced reference receipt'
    $dependencies = @($manifest.Dependencies)
    if ($dependencies.Count -ne 1) {
        throw 'MoreEquipmentSlots manifest must retain one exact ModConfigMenu dependency.'
    }
    Assert-SmokeExactJsonPropertySet -Value $dependencies[0] -Expected @('UniqueID','MinimumVersion','Required') -Context 'MoreEquipmentSlots manifest dependency'

    $manifestSha256 = Get-SmokeFileSha256 -Path $manifestPath
    $entryDllSha256 = Get-SmokeFileSha256 -Path $entryDllPath
    $receiptSha256 = Get-SmokeFileSha256 -Path $receiptPath
    $entryDllLength = [int64](Get-Item -LiteralPath $entryDllPath -Force).Length
    $bindingMatches = [string]$info.version -ceq '1.0.0' -and
        [string]$manifest.UniqueID -ceq [string]$product.uniqueId -and
        [string]$manifest.Version -ceq [string]$product.sourceVersion -and
        [string]$manifest.MinimumDTMApiVersion -ceq '0.6.0' -and
        [string]$manifest.Type -ceq 'CodeMod' -and
        [string]$manifest.CodeModKind -ceq 'Advanced' -and
        [string]$manifest.EntryDll -ceq 'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll' -and
        [string]$manifest.EntryType -ceq 'DTMAPI.MoreEquipmentSlots.ModEntry' -and
        [string]$dependencies[0].UniqueID -ceq 'DTMAPI.ModConfigMenu' -and
        [string]$dependencies[0].MinimumVersion -ceq '0.5.5' -and
        ($dependencies[0].Required -is [bool]) -and [bool]$dependencies[0].Required -and
        [int]$marker.schemaVersion -eq 2 -and [string]$marker.owner -ceq 'DTMAPI' -and
        [string]$marker.uniqueId -ceq [string]$product.uniqueId -and
        [string]$marker.version -ceq [string]$product.sourceVersion -and
        [string]$marker.packageKind -ceq 'CodeMod' -and [string]$marker.codeModKind -ceq 'Advanced' -and
        [string]$marker.authorSdkVersion -ceq '0.1.0' -and [string]$marker.targetDtmApiVersion -ceq '0.5.5' -and
        [string]$marker.manifestPath -ceq 'Content/DTMAPI/manifest.json' -and
        (Test-SmokeSha256Equal -Actual ([string]$marker.manifestSha256) -Expected $manifestSha256) -and
        [string]$marker.entryDllPath -ceq 'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll' -and
        (Test-SmokeSha256Equal -Actual ([string]$marker.entryDllSha256) -Expected $entryDllSha256) -and
        [string]$marker.advancedReferenceReceiptPath -ceq 'Content/DTMAPI/dtmapi-advanced-references.json' -and
        (Test-SmokeSha256Equal -Actual ([string]$marker.advancedReferenceReceiptSha256) -Expected $receiptSha256) -and
        [string]$marker.authority -ceq 'dtmapi-author-sdk-package-binding' -and
        [int]$receipt.schemaVersion -eq 1 -and [string]$receipt.receiptKind -ceq 'DTMAPI.AdvancedCodeMod.ReferenceReceipt' -and
        [string]$receipt.referencePolicyId -ceq $policyId -and
        [int]$receipt.referencePolicyVersion -eq [int]$policy.policyVersion -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.referencePolicySha256) -Expected $policySha256) -and
        [string]$receipt.uniqueId -ceq [string]$product.uniqueId -and [string]$receipt.codeModKind -ceq 'Advanced' -and
        [string]$receipt.targetFramework -ceq 'netstandard2.0' -and
        [string]$receipt.gameBuildId -ceq [string]$policy.gameBuildId -and
        [string]$receipt.gameAssemblyRelativePath -ceq [string]$policy.gameAssemblyRelativePath -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.gameAssemblySha256) -Expected ([string]$policy.gameAssemblySha256)) -and
        [string]$receipt.manifestPath -ceq 'Content/DTMAPI/manifest.json' -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.manifestSha256) -Expected $manifestSha256) -and
        [string]$receipt.entryDllPath -ceq 'Content/DTMAPI/DTMAPI.MoreEquipmentSlots.dll' -and
        [int64]$receipt.entryDllLength -eq $entryDllLength -and
        (Test-SmokeSha256Equal -Actual ([string]$receipt.entryDllSha256) -Expected $entryDllSha256) -and
        [string]$receipt.harmonyOwner -ceq [string]$product.canonicalHarmonyOwner
    if (-not $bindingMatches) {
        throw 'MoreEquipmentSlots cold recovery refused drifted official Local manifest/package/reference authority.'
    }

    $expectedReferences = @($policy.references)
    $actualReferences = @($receipt.references)
    if ($actualReferences.Count -ne $expectedReferences.Count) {
        throw 'MoreEquipmentSlots Advanced receipt reference count does not match its tracked policy.'
    }
    for ($referenceIndex = 0; $referenceIndex -lt $expectedReferences.Count; $referenceIndex++) {
        $expectedReference = $expectedReferences[$referenceIndex]
        $actualReference = $actualReferences[$referenceIndex]
        Assert-SmokeExactJsonPropertySet -Value $actualReference -Expected @(
            'gameRelativePath','assemblyName','length','sha256','copyLocal'
        ) -Context "MoreEquipmentSlots Advanced reference receipt row $referenceIndex"
        if ([string]$actualReference.gameRelativePath -cne [string]$expectedReference.gameRelativePath -or
            [string]$actualReference.assemblyName -cne [string]$expectedReference.assemblyName -or
            [int64]$actualReference.length -ne [int64]$expectedReference.length -or
            -not (Test-SmokeSha256Equal -Actual ([string]$actualReference.sha256) -Expected ([string]$expectedReference.sha256)) -or
            -not ($actualReference.copyLocal -is [bool]) -or [bool]$actualReference.copyLocal) {
            throw "MoreEquipmentSlots Advanced receipt reference row $referenceIndex does not match its tracked policy."
        }
    }

    return [ordered]@{
        ProductRoot = $canonicalProductRoot
        Files = @($relativeFiles)
        UniqueId = [string]$manifest.UniqueID
        Version = [string]$manifest.Version
        MinimumDtmApiVersion = [string]$manifest.MinimumDTMApiVersion
        CodeModKind = [string]$manifest.CodeModKind
        ReferencePolicyId = [string]$receipt.referencePolicyId
        ReferencePolicySha256 = $policySha256
        GameBuildId = [string]$receipt.gameBuildId
        HarmonyOwner = [string]$receipt.harmonyOwner
        ManifestSha256 = $manifestSha256
        EntryDllLength = $entryDllLength
        EntryDllSha256 = $entryDllSha256
        AdvancedReferenceReceiptSha256 = $receiptSha256
        ReferenceCount = $actualReferences.Count
        Passed = $true
    }
}
