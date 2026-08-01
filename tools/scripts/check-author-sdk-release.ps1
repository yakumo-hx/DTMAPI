param(
    [Parameter(Mandatory = $true)] [string] $PackagePath,
    [switch] $SkipDeterministicZipCheck
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\author-sdk-release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-ReleaseEqual {
    param([string] $Label, $Actual, $Expected)
    if (-not [object]::Equals($Actual, $Expected)) {
        throw "$Label mismatch: expected='$Expected' actual='$Actual'"
    }
}

function Assert-ReleaseHash {
    param([string] $Label, [string] $Path, [string] $Expected)
    $actual = Get-AuthorSdkSha256 -Path $Path
    if (-not $actual.Equals($Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label SHA-256 mismatch: expected=$Expected actual=$actual"
    }
}

function Get-OrdinalSortedStrings {
    param([string[]] $Values)
    [string[]]$copy = @($Values)
    [Array]::Sort($copy, [System.StringComparer]::Ordinal)
    return $copy
}

$repo = Get-RepoRoot
$contract = Get-AuthorSdkContract -RepoRoot $repo
$packageFull = [System.IO.Path]::GetFullPath($PackagePath)
$temporaryBase = Join-Path ([System.IO.Path]::GetTempPath()) 'DTMAPI Author SDK release checks'
$temporaryRoot = Join-Path $temporaryBase ([Guid]::NewGuid().ToString('N'))
$isZip = (Test-Path -LiteralPath $packageFull -PathType Leaf) -and $packageFull.EndsWith('.zip', [System.StringComparison]::OrdinalIgnoreCase)
if ($isZip) {
    Add-Type -AssemblyName System.IO.Compression
    $zipStream = [System.IO.File]::OpenRead($packageFull)
    try {
        $zipArchive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Read, $true)
        try {
            [string[]]$zipEntryNames = @($zipArchive.Entries | ForEach-Object { $_.FullName })
            $sortedZipEntryNames = Get-OrdinalSortedStrings -Values $zipEntryNames
            if (@(Compare-Object -ReferenceObject $sortedZipEntryNames -DifferenceObject $zipEntryNames -SyncWindow 0).Count -ne 0) {
                throw 'Author SDK ZIP entries are not in canonical ordinal relative-path order.'
            }
            foreach ($zipEntry in $zipArchive.Entries) {
                if ($zipEntry.FullName.EndsWith('/', [System.StringComparison]::Ordinal)) { throw "Author SDK canonical ZIP must not contain directory entries: $($zipEntry.FullName)" }
                $entryTime = $zipEntry.LastWriteTime
                if ($entryTime.Year -ne 2000 -or $entryTime.Month -ne 1 -or $entryTime.Day -ne 1 -or $entryTime.Hour -ne 0 -or $entryTime.Minute -ne 0 -or $entryTime.Second -ne 0) { throw "Author SDK ZIP entry timestamp is not frozen: $($zipEntry.FullName)" }
                if ($zipEntry.Length -gt 0 -and $zipEntry.CompressedLength -ne $zipEntry.Length) { throw "Author SDK canonical ZIP must use store mode: $($zipEntry.FullName)" }
            }
        }
        finally { $zipArchive.Dispose() }
    }
    finally { $zipStream.Dispose() }
    if (-not (Test-Path -LiteralPath $temporaryBase -PathType Container)) { New-Item -ItemType Directory -Path $temporaryBase -Force | Out-Null }
    $stageRoot = Expand-AuthorSdkZipSafely -ZipPath $packageFull -Destination $temporaryRoot
}
elseif (Test-Path -LiteralPath $packageFull -PathType Container) {
    $stageRoot = $packageFull
}
else {
    throw "Author SDK release package must be a stage directory or ZIP: $packageFull"
}

try {
    $releasePath = Join-Path $stageRoot 'author-sdk-release.json'
    if (-not (Test-Path -LiteralPath $releasePath -PathType Leaf)) { throw "Top-level Author SDK inventory is missing: $releasePath" }
    $release = Get-Content -Raw -Encoding UTF8 -LiteralPath $releasePath | ConvertFrom-Json
    Assert-ReleaseEqual -Label 'Release schemaVersion' -Actual ([int]$release.schemaVersion) -Expected 1
    Assert-ReleaseEqual -Label 'Release sdkVersion' -Actual ([string]$release.sdkVersion) -Expected '0.1.0'
    Assert-ReleaseEqual -Label 'Release targetRuntimeVersion' -Actual ([string]$release.targetRuntimeVersion) -Expected '0.5.5'
    Assert-ReleaseEqual -Label 'Release buildDotNetSdkVersion' -Actual ([string]$release.buildDotNetSdkVersion) -Expected '8.0.421'
    Assert-ReleaseEqual -Label 'Release runtimeIdentifier' -Actual ([string]$release.runtimeIdentifier) -Expected 'win-x64'
    Assert-ReleaseEqual -Label 'Release packageKind' -Actual ([string]$release.packageKind) -Expected 'self-contained-portable-author-sdk'
    Assert-ReleaseEqual -Label 'Release PathMap' -Actual ([string]$release.pathMap) -Expected '/_/DTMAPI'
    Assert-ReleaseEqual -Label 'Release ZIP order' -Actual ([string]$release.deterministicZip.entryOrder) -Expected 'ordinal-relative-path'
    $releaseZipTimestamp = ([DateTimeOffset]$release.deterministicZip.entryTimestampUtc).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [System.Globalization.CultureInfo]::InvariantCulture)
    Assert-ReleaseEqual -Label 'Release ZIP timestamp' -Actual $releaseZipTimestamp -Expected '2000-01-01T00:00:00Z'
    Assert-ReleaseEqual -Label 'Release ZIP compression' -Actual ([string]$release.deterministicZip.compression) -Expected 'store'

    $declared = @($release.files)
    if ($declared.Count -eq 0) { throw 'Top-level Author SDK inventory is empty.' }
    $declaredByPath = @{}
    [string[]]$declaredOrder = @($declared | ForEach-Object { [string]$_.path })
    $expectedOrder = Get-OrdinalSortedStrings -Values $declaredOrder
    if (@(Compare-Object -ReferenceObject $expectedOrder -DifferenceObject $declaredOrder -SyncWindow 0).Count -ne 0) {
        throw 'Top-level Author SDK inventory is not sorted by ordinal relative path.'
    }
    foreach ($entry in $declared) {
        $relative = ([string]$entry.path).Replace('\', '/')
        if ([string]::IsNullOrWhiteSpace($relative) -or [System.IO.Path]::IsPathRooted($relative) -or $relative.Contains('../') -or $declaredByPath.ContainsKey($relative.ToLowerInvariant())) {
            throw "Unsafe or duplicate top-level inventory path: $relative"
        }
        $path = Join-Path $stageRoot $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Top-level inventory file is missing: $relative" }
        Assert-ReleaseEqual -Label "Inventory length $relative" -Actual ([Int64](Get-Item -LiteralPath $path).Length) -Expected ([Int64]$entry.length)
        Assert-ReleaseHash -Label "Inventory $relative" -Path $path -Expected ([string]$entry.sha256)
        Assert-ReleaseEqual -Label "Inventory kind $relative" -Actual ([string]$entry.kind) -Expected (Get-AuthorSdkReleaseFileKind -RelativePath $relative)
        $declaredByPath[$relative.ToLowerInvariant()] = $entry
    }
    [string[]]$actualPaths = @(Get-ChildItem -LiteralPath $stageRoot -File -Recurse | Where-Object { $_.FullName -ne $releasePath } | ForEach-Object { Get-AuthorSdkRelativePath -Root $stageRoot -Path $_.FullName })
    $actualPaths = Get-OrdinalSortedStrings -Values $actualPaths
    if (@(Compare-Object -ReferenceObject $declaredOrder -DifferenceObject $actualPaths).Count -ne 0 -or $actualPaths.Count -ne $declaredOrder.Count) {
        throw 'Top-level Author SDK inventory is not an exact declaration of every staged file.'
    }
    $requiredReleaseKinds = @('asset', 'cli', 'compatibility', 'compiler', 'license', 'runtime-support', 'support')
    $actualReleaseKinds = @($declared | ForEach-Object { [string]$_.kind } | Sort-Object -Unique)
    foreach ($kind in $requiredReleaseKinds) {
        if ($actualReleaseKinds -notcontains $kind) { throw "Top-level Author SDK inventory is missing required file kind: $kind" }
    }

    $roslynRows = @($release.roslynCompiler)
    $expectedRoslynNames = @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll')
    if ($roslynRows.Count -ne 2) { throw 'Top-level inventory must record exactly the two pinned Roslyn compiler hashes.' }
    foreach ($name in $expectedRoslynNames) {
        $matches = @($roslynRows | Where-Object { [string]$_.path -eq $name })
        if ($matches.Count -ne 1) { throw "Top-level Roslyn inventory must contain exactly one $name row." }
        Assert-ReleaseHash -Label "Roslyn $name" -Path (Join-Path $stageRoot $name) -Expected ([string]$matches[0].sha256)
    }

    foreach ($required in @(
        'dtmapi-author.exe', 'dtmapi-author.dll', 'DTMAPI.InstallDoctor.dll', 'DTMAPI.Tooling.Metadata.dll',
        'README.md', 'THIRD-PARTY-NOTICES.md', 'licenses/dotnet-LICENSE.txt',
        'licenses/dotnet-ThirdPartyNotices.txt', 'contracts/compatibility-0.5.5.contract.json'
    )) {
        if (-not (Test-Path -LiteralPath (Join-Path $stageRoot $required.Replace('/', [System.IO.Path]::DirectorySeparatorChar)) -PathType Leaf)) {
            throw "Author SDK release support file is missing: $required"
        }
    }

    $trackedJournalSchemaPath = Join-Path $repo 'author-sdk\schemas\deployment-journal.schema.json'
    $stagedJournalSchemaPath = Join-Path $stageRoot 'schemas\deployment-journal.schema.json'
    $trackedReadmePath = Join-Path $repo 'author-sdk\README.md'
    $stagedReadmePath = Join-Path $stageRoot 'README.md'
    Assert-ReleaseHash -Label 'Staged deployment-journal schema' -Path $stagedJournalSchemaPath -Expected (Get-AuthorSdkSha256 -Path $trackedJournalSchemaPath)
    Assert-ReleaseHash -Label 'Staged Author SDK README' -Path $stagedReadmePath -Expected (Get-AuthorSdkSha256 -Path $trackedReadmePath)

    $contractsSource = [System.IO.File]::ReadAllText(
        (Join-Path $repo 'src\DTMAPI.Authoring.Contracts\AuthorContracts.cs'),
        [System.Text.Encoding]::UTF8)
    $journalVersionMatch = [regex]::Match(
        $contractsSource,
        'public\s+const\s+int\s+DeploymentJournalSchemaVersion\s*=\s*(\d+)\s*;')
    $receiptVersionMatch = [regex]::Match(
        $contractsSource,
        'public\s+const\s+int\s+DeploymentSchemaVersion\s*=\s*(\d+)\s*;')
    if (-not $journalVersionMatch.Success -or -not $receiptVersionMatch.Success) {
        throw 'Author SDK release could not resolve deployment journal/receipt version constants from AuthorContracts.cs.'
    }
    $journalVersion = [int]$journalVersionMatch.Groups[1].Value
    $receiptVersion = [int]$receiptVersionMatch.Groups[1].Value
    $journalSchema = Get-Content -Raw -Encoding UTF8 -LiteralPath $stagedJournalSchemaPath | ConvertFrom-Json
    Assert-ReleaseEqual -Label 'Deployment journal published schema/current writer parity' -Actual ([int]$journalSchema.properties.schemaVersion.const) -Expected $journalVersion
    if (@($journalSchema.required | Where-Object { [string]$_ -ceq 'localInstall' }).Count -ne 1 -or
        @($journalSchema.properties.localInstall.oneOf | Where-Object {
            $_.PSObject.Properties.Name -contains '$ref' -and
            [string]$_.'$ref' -ceq '#/$defs/localInstallTransaction'
        }).Count -ne 1) {
        throw 'Deployment journal schema must require nullable localInstall and reference its exact transaction definition.'
    }
    [string[]]$expectedLocalInstallFields = @(
        'expectedPackageSha256', 'expectedVersion', 'failedPath', 'journalBeforeBase64',
        'journalBeforeExisted', 'next', 'phase', 'previous', 'recoveryPath',
        'sourceStateBeforeBase64', 'sourceStateBeforeExisted', 'sourceStatePath',
        'stagingPath', 'transactionId'
    )
    [string[]]$actualLocalInstallFields = @(
        $journalSchema.'$defs'.localInstallTransaction.required |
            ForEach-Object { [string]$_ }
    )
    $actualLocalInstallFields = Get-OrdinalSortedStrings -Values $actualLocalInstallFields
    $expectedLocalInstallFields = Get-OrdinalSortedStrings -Values $expectedLocalInstallFields
    if (@(Compare-Object -ReferenceObject $expectedLocalInstallFields -DifferenceObject $actualLocalInstallFields -SyncWindow 0).Count -ne 0) {
        throw 'Deployment journal schema localInstall required fields drifted from the published transaction contract.'
    }
    Assert-ReleaseEqual -Label 'Deployment journal localInstall phase' -Actual ([string]$journalSchema.'$defs'.localInstallTransaction.properties.phase.const) -Expected 'Prepared'

    $stagedReadme = [System.IO.File]::ReadAllText($stagedReadmePath, [System.Text.Encoding]::UTF8)
    $journalStatement = "New deployment journals and every journal write use schema $journalVersion."
    $receiptStatement = "Package-local receipts and package markers remain schema $receiptVersion."
    if ($stagedReadme.IndexOf($journalStatement, [System.StringComparison]::Ordinal) -lt 0 -or
        $stagedReadme.IndexOf($receiptStatement, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Author SDK README does not distinguish the current deployment journal schema from the receipt/package-marker schema.'
    }

    foreach ($versionedPe in @(
        'dtmapi-author.exe',
        'dtmapi-author.dll',
        'DTMAPI.Authoring.Contracts.dll',
        'DTMAPI.InstallDoctor.dll',
        'DTMAPI.Tooling.Metadata.dll'
    )) {
        $versionInfo = (Get-Item -LiteralPath (Join-Path $stageRoot $versionedPe)).VersionInfo
        Assert-ReleaseEqual -Label "$versionedPe FileVersion" -Actual ([string]$versionInfo.FileVersion) -Expected '0.1.0.0'
        Assert-ReleaseEqual -Label "$versionedPe ProductVersion" -Actual ([string]$versionInfo.ProductVersion) -Expected '0.1.0'
    }
    $trackedContractPath = Join-Path $repo 'author-sdk\compatibility\0.5.5\compatibility.contract.json'
    Assert-ReleaseHash -Label 'Staged tracked compatibility contract' -Path (Join-Path $stageRoot 'contracts\compatibility-0.5.5.contract.json') -Expected (Get-AuthorSdkSha256 -Path $trackedContractPath)

    $compatibilityRoot = Join-Path $stageRoot 'compatibility\0.5.5'
    $manifestPath = Join-Path $compatibilityRoot ([string]$contract.releaseManifestName)
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Compatibility manifest is missing: $manifestPath" }
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    Assert-ReleaseEqual -Label 'Compatibility schemaVersion' -Actual ([int]$manifest.schemaVersion) -Expected 1
    Assert-ReleaseEqual -Label 'Compatibility sdkVersion' -Actual ([string]$manifest.sdkVersion) -Expected ([string]$contract.sdkVersion)
    Assert-ReleaseEqual -Label 'Compatibility targetRuntimeVersion' -Actual ([string]$manifest.targetRuntimeVersion) -Expected ([string]$contract.targetRuntimeVersion)
    Assert-ReleaseEqual -Label 'Compatibility Abstractions AssemblyVersion' -Actual ([string]$manifest.abstractionsAssemblyVersion) -Expected ([string]$contract.abstractionsAssemblyVersion)
    Assert-ReleaseEqual -Label 'Compatibility Abstractions FileVersion' -Actual ([string]$manifest.abstractionsFileVersion) -Expected ([string]$contract.abstractionsFileVersion)

    $compatibilityRows = @($manifest.files)
    $expectedCompatibilityCount = [int]$contract.netstandardReferenceFileCount + 4
    if ($compatibilityRows.Count -ne $expectedCompatibilityCount) {
        throw "Compatibility inventory count mismatch: expected=$expectedCompatibilityCount actual=$($compatibilityRows.Count)"
    }
    $compatibilityByPath = @{}
    foreach ($row in $compatibilityRows) {
        $relative = ([string]$row.path).Replace('\', '/')
        $key = $relative.ToLowerInvariant()
        if ([string]::IsNullOrWhiteSpace($relative) -or [System.IO.Path]::IsPathRooted($relative) -or $relative.Contains('../') -or $compatibilityByPath.ContainsKey($key)) {
            throw "Unsafe or duplicate compatibility inventory path: $relative"
        }
        $path = Join-Path $compatibilityRoot $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Compatibility inventory file is missing: $relative" }
        Assert-ReleaseHash -Label "Compatibility $relative" -Path $path -Expected ([string]$row.sha256)
        $compatibilityByPath[$key] = $row
    }
    [string[]]$actualCompatibilityPaths = @(Get-ChildItem -LiteralPath $compatibilityRoot -File -Recurse | Where-Object { $_.FullName -ne $manifestPath } | ForEach-Object { Get-AuthorSdkRelativePath -Root $compatibilityRoot -Path $_.FullName })
    if (@(Compare-Object -ReferenceObject @($compatibilityRows | ForEach-Object { [string]$_.path }) -DifferenceObject $actualCompatibilityPaths).Count -ne 0 -or $actualCompatibilityPaths.Count -ne $compatibilityRows.Count) {
        throw 'compatibility.json is not an exact declaration of the compatibility payload.'
    }

    $kindCounts = @{}
    foreach ($kind in @($contract.requiredKinds)) {
        $kindCounts[[string]$kind] = @($compatibilityRows | Where-Object { [string]$_.kind -eq [string]$kind }).Count
    }
    foreach ($single in @('abstractions', 'props', 'license', 'notice')) {
        if ($kindCounts[$single] -ne 1) { throw "Compatibility payload must contain exactly one '$single' file." }
    }
    if ($kindCounts['reference'] -ne [int]$contract.netstandardReferenceFileCount) { throw 'Compatibility reference kind count does not match the frozen contract.' }

    $abstractionsPath = Join-Path $compatibilityRoot 'DTMAPI.Abstractions.dll'
    $propsPath = Join-Path $compatibilityRoot 'DTMAPI.Author.props'
    $licensePath = Join-Path $compatibilityRoot 'NETStandard.Library.LICENSE.TXT'
    $noticePath = Join-Path $compatibilityRoot 'NETStandard.Library.THIRD-PARTY-NOTICES.TXT'
    Assert-ReleaseHash -Label 'Frozen DTMAPI.Abstractions' -Path $abstractionsPath -Expected ([string]$contract.abstractionsSha256)
    Assert-ReleaseHash -Label 'Frozen Author props' -Path $propsPath -Expected ([string]$contract.authorPropsSha256)
    Assert-ReleaseHash -Label 'Frozen NETStandard license' -Path $licensePath -Expected ([string]$contract.netstandardLicenseSha256)
    Assert-ReleaseHash -Label 'Frozen NETStandard notice' -Path $noticePath -Expected ([string]$contract.netstandardNoticeSha256)
    $referenceRoot = Join-Path $compatibilityRoot 'ref\netstandard2.0'
    $referenceFiles = @(Get-ChildItem -LiteralPath $referenceRoot -File)
    if ($referenceFiles.Count -ne [int]$contract.netstandardReferenceFileCount -or @(Get-ChildItem -LiteralPath $referenceRoot -Directory).Count -ne 0) {
        throw 'Compatibility references must be the exact immediate-child NETStandard.Library 2.0.3 reference set.'
    }
    $referenceDigest = Get-AuthorSdkFileTreeDigestV1 -Root $referenceRoot
    if (-not $referenceDigest.Equals([string]$contract.netstandardReferenceInventorySha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Compatibility reference FileTree digest mismatch: expected=$($contract.netstandardReferenceInventorySha256) actual=$referenceDigest"
    }

    $catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $runtimeAssemblies = @($catalog.playerRuntimePackageInvariant.productionAssemblies | ForEach-Object { [string]$_ } | Sort-Object)
    $expectedRuntimeAssemblies = @('DTMAPI.Abstractions.dll', 'DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll') | Sort-Object
    if ($runtimeAssemblies.Count -ne 5 -or @(Compare-Object -ReferenceObject $expectedRuntimeAssemblies -DifferenceObject $runtimeAssemblies).Count -ne 0) {
        throw 'The Catalog no longer preserves the five-DLL player Runtime invariant.'
    }
    foreach ($forbiddenRuntime in @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')) {
        if (@(Get-ChildItem -LiteralPath $stageRoot -Filter $forbiddenRuntime -File -Recurse).Count -ne 0) {
            throw "Author SDK release must remain independent from the player Runtime payload: $forbiddenRuntime"
        }
    }
    foreach ($forbiddenStandaloneDoctor in @('dtmapi-doctor.exe', 'dtmapi-doctor.runtimeconfig.json', 'dtmapi-doctor.deps.json')) {
        if (@(Get-ChildItem -LiteralPath $stageRoot -Filter $forbiddenStandaloneDoctor -File -Recurse).Count -ne 0) {
            throw "Doctor must ship only as the dtmapi-author subcommand, not as a second executable: $forbiddenStandaloneDoctor"
        }
    }
    foreach ($forbiddenNativePayload in @('Assembly-CSharp.dll', '0Harmony.dll', 'BepInEx.dll')) {
        if (@(Get-ChildItem -LiteralPath $stageRoot -Filter $forbiddenNativePayload -File -Recurse).Count -ne 0) {
            throw "Author SDK release must not redistribute the native/game reference payload: $forbiddenNativePayload"
        }
    }
    if (@(Get-ChildItem -LiteralPath $stageRoot -Filter 'UnityEngine*.dll' -File -Recurse).Count -ne 0) {
        throw 'Author SDK release must not redistribute Unity reference payloads.'
    }
    $advancedPolicyPath = Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-g2-v1.json'
    $advancedPolicy = Get-Content -Raw -Encoding UTF8 -LiteralPath $advancedPolicyPath | ConvertFrom-Json
    foreach ($file in @(Get-ChildItem -LiteralPath $stageRoot -File -Recurse)) {
        foreach ($reference in @($advancedPolicy.references | Where-Object { [Int64]$_.length -eq [Int64]$file.Length })) {
            $actualHash = Get-AuthorSdkSha256 -Path $file.FullName
            if ($actualHash.Equals([string]$reference.sha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Author SDK release contains tracked native/game bytes under an arbitrary file name: $(Get-AuthorSdkRelativePath -Root $stageRoot -Path $file.FullName) identity=$($reference.assemblyName)"
            }
        }
    }
    $abstractionsCopies = @(Get-ChildItem -LiteralPath $stageRoot -Filter 'DTMAPI.Abstractions.dll' -File -Recurse)
    if ($abstractionsCopies.Count -ne 1 -or $abstractionsCopies[0].FullName -ne $abstractionsPath) {
        throw 'Author SDK release must carry exactly one fixed Abstractions DLL, only inside compatibility/0.5.5.'
    }
    $forbiddenBuildArtifacts = @(Get-ChildItem -LiteralPath $stageRoot -File -Recurse | Where-Object { $_.Extension -in @('.pdb', '.nupkg') })
    if ($forbiddenBuildArtifacts.Count -ne 0) {
        throw 'Author SDK release must not contain PDBs or NuGet packages.'
    }
    foreach ($entry in @(Get-ChildItem -LiteralPath $stageRoot -File -Recurse)) {
        $relative = Get-AuthorSdkRelativePath -Root $stageRoot -Path $entry.FullName
        if ($relative.IndexOf('BepInEx/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $relative.IndexOf('Workshop', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $relative.EndsWith('.bat', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Author SDK ZIP contains a player installer/Workshop path: $relative"
        }
    }

    $forbiddenMarkers = @(
        [System.IO.Path]::GetFullPath($repo), [System.IO.Path]::GetFullPath($env:USERPROFILE), 'references/doloc-town/reverse',
        'references\doloc-town\reverse', 'references/third-party-mods', 'references\third-party-mods',
        'steamapps\common\DolocTown'
    )
    foreach ($file in @(Get-ChildItem -LiteralPath $stageRoot -File -Recurse)) {
        [byte[]]$bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $asciiContent = [System.Text.Encoding]::ASCII.GetString($bytes)
        $unicodeContent = [System.Text.Encoding]::Unicode.GetString($bytes)
        foreach ($marker in $forbiddenMarkers) {
            if ($asciiContent.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $unicodeContent.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Author SDK release leaks an absolute/private/player reference marker '$marker' in $(Get-AuthorSdkRelativePath -Root $stageRoot -Path $file.FullName)."
            }
        }
    }

    if ($isZip -and -not $SkipDeterministicZipCheck -and $PSVersionTable.PSEdition -eq 'Core') {
        $repacked = Join-Path $temporaryBase ([Guid]::NewGuid().ToString('N') + '.zip')
        try {
            New-AuthorSdkDeterministicZip -SourceRoot $stageRoot -ZipPath $repacked | Out-Null
            Assert-ReleaseHash -Label 'Canonical deterministic ZIP replay' -Path $repacked -Expected (Get-AuthorSdkSha256 -Path $packageFull)
        }
        finally {
            if (Test-Path -LiteralPath $repacked) { Remove-Item -LiteralPath $repacked -Force }
        }
    }
    elseif ($isZip -and -not $SkipDeterministicZipCheck) {
        Write-Warning 'Canonical ZIP structure passed; byte-for-byte replay is skipped on Windows PowerShell 5.1 because its .NET Framework ZipArchive cannot emit store mode.'
    }

    Write-Host "Author SDK release check: PASS"
    Write-Host "Package: $packageFull"
    Write-Host "Files:   $($declared.Count + 1)"
    if ($isZip) { Write-Host "SHA-256: $(Get-AuthorSdkSha256 -Path $packageFull)" }
}
finally {
    if ($isZip -and (Test-Path -LiteralPath $temporaryRoot)) {
        Remove-AuthorSdkTreeSafely -AllowedRoot $temporaryBase -Path $temporaryRoot
    }
}
