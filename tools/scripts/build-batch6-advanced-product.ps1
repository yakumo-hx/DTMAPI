[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $CatalogId,
    [string] $GameDir = '',
    [string] $OutputRoot = '',
    [string] $AuthorSdkRoot = '',
    [ValidateSet('Release')]
    [string] $Configuration = 'Release'
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\author-sdk-release-common.ps1"
. "$PSScriptRoot\author-sdk-preparation.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
$matches = @($catalog.products | Where-Object { [string]$_.catalogId -ceq $CatalogId })
if ($matches.Count -ne 1) {
    throw "Catalog-driven Advanced build requires exactly one row for catalogId '$CatalogId'; found $($matches.Count)."
}
$product = $matches[0]
if ([string]$product.codeModKind -cne 'Advanced' -or
    [string]$product.productionBuildAuthority -cne 'DTMAPI Author SDK build/pack/deploy' -or
    [string]$product.nativeOwnership -cne 'ProductNative') {
    throw "Catalog row '$CatalogId' is not an admitted ProductNative Advanced Author SDK product."
}

$requiredCatalogFields = @(
    'sourceRoot', 'sourceManifest', 'packageName', 'sourceDll', 'sourceVersion',
    'sourceMinimumDtmApiVersion', 'uniqueId', 'referencePolicyId', 'canonicalHarmonyOwner'
)
foreach ($field in $requiredCatalogFields) {
    if ([string]::IsNullOrWhiteSpace([string]$product.$field)) {
        throw "Catalog row '$CatalogId' is missing required Advanced build field '$field'."
    }
}

$projectRoot = Join-Path $repo ([string]$product.sourceRoot).Replace('/', '\')
$manifestPath = Join-Path $repo ([string]$product.sourceManifest).Replace('/', '\')
$authorProjectPath = Join-Path $projectRoot 'dtmapi.author.json'
$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
$authorProject = Get-Content -Raw -Encoding UTF8 -LiteralPath $authorProjectPath | ConvertFrom-Json
$uniqueId = [string]$product.uniqueId
$entryDll = [string]$product.sourceDll
$entryType = [string]$manifest.EntryType
$policyId = [string]$product.referencePolicyId
$harmonyOwner = [string]$product.canonicalHarmonyOwner
$packageFile = ([string]$product.packageName) + '-advanced-pilot.zip'
$policyPath = Join-Path $repo ('author-sdk\advanced-reference-policies\' + $policyId + '.json')
$policy = Get-Content -Raw -Encoding UTF8 -LiteralPath $policyPath | ConvertFrom-Json
$authorSchemaPath = Join-Path $repo 'author-sdk\schemas\dtmapi-author.schema.json'
$targetCatalog = Get-AuthorSdkTargetCatalog -RepoRoot $repo
$authorSdkApiTarget = [string]$authorProject.targetDtmApiVersion
$sdkTargets = @($targetCatalog.targets | Where-Object { $_.apiTarget -ceq $authorSdkApiTarget -and $_.state -ceq 'available' })
if ($sdkTargets.Count -ne 1) {
    throw "Author SDK target catalog does not admit API target '$authorSdkApiTarget'."
}
$sdkVersion = Get-AuthorSdkReleaseVersion -RepoRoot $repo
if (@($sdkTargets[0].sdkVersions) -cnotcontains $sdkVersion) {
    throw "Current Author SDK $sdkVersion does not admit API target '$authorSdkApiTarget'."
}

if ([string]$manifest.UniqueID -cne $uniqueId -or
    [string]$manifest.Version -cne [string]$product.sourceVersion -or
    [string]$manifest.MinimumDTMApiVersion -cne [string]$product.sourceMinimumDtmApiVersion -or
    [string]$manifest.Type -cne 'CodeMod' -or
    [string]$manifest.CodeModKind -cne 'Advanced' -or
    [string]$manifest.EntryDll -cne $entryDll -or
    [string]::IsNullOrWhiteSpace($entryType)) {
    throw "Catalog/source manifest drift for Advanced product '$CatalogId'."
}
if ([string]$authorProject.schemaVersion -cne '4' -or
    [string]$authorProject.projectKind -cne 'CodeMod' -or
    [string]$authorProject.codeModKind -cne 'Advanced' -or
    [string]$authorProject.targetDtmApiVersion -cne $authorSdkApiTarget -or
    [string]::IsNullOrWhiteSpace([string]$authorProject.projectFile) -or
    [string]$authorProject.advanced.referencePolicyId -cne $policyId) {
    throw "Catalog/source Author SDK intent drift for Advanced product '$CatalogId'."
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo
}
else {
    $GameDir = [IO.Path]::GetFullPath($GameDir)
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source '-GameDir'
}

$repoTemp = [IO.Path]::GetFullPath((Join-Path $repo 'temp'))
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $safeCatalogId = $CatalogId.ToLowerInvariant() -replace '[^a-z0-9]+', '-'
    $OutputRoot = Join-Path $repoTemp ("batch6-$safeCatalogId-advanced-pilot")
}
else {
    $OutputRoot = if ([IO.Path]::IsPathRooted($OutputRoot)) {
        [IO.Path]::GetFullPath($OutputRoot)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path $repo $OutputRoot))
    }
}
if (-not (Test-DtmApiPathIsSameOrChild -Child $OutputRoot -Parent $repoTemp)) {
    throw "Advanced product output must remain below the repository temp root: $repoTemp"
}
$trim = [char[]]@('\', '/')
if ($OutputRoot.TrimEnd($trim) -eq $repoTemp.TrimEnd($trim)) {
    throw 'Advanced product output cannot be the repository temp root itself.'
}
foreach ($required in @($projectRoot, $manifestPath, $authorProjectPath, (Join-Path $projectRoot 'src'), $policyPath, $authorSchemaPath)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Catalog-driven Advanced project input is missing: $required"
    }
}

$sdkRoot = if ([string]::IsNullOrWhiteSpace($AuthorSdkRoot)) {
    Join-Path $repo '.tools/author-sdk'
}
else {
    if ([IO.Path]::IsPathRooted($AuthorSdkRoot)) {
        [IO.Path]::GetFullPath($AuthorSdkRoot)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path $repo $AuthorSdkRoot))
    }
}
Assert-DtmApiBuildPathsDisjoint -OutputPath $OutputRoot -InputPaths @($projectRoot, $sdkRoot, $GameDir)
# An explicit prepared SDK is verified before any old product output is removed.
& "$PSScriptRoot\prepare-author-sdk.ps1" -OutputRoot $sdkRoot -Check:([string]::IsNullOrWhiteSpace($AuthorSdkRoot) -eq $false)
if (-not $?) { throw "Author SDK preparation failed for '$CatalogId'." }
$sdkExe = Join-Path $sdkRoot "DTMAPI-Author-SDK-$sdkVersion-win-x64\dtmapi-author.exe"
if (-not (Test-Path -LiteralPath $sdkExe -PathType Leaf)) {
    throw "The supplied Author SDK root does not contain the frozen SDK executable for '$CatalogId': $sdkExe"
}
if (Test-Path -LiteralPath $OutputRoot) {
    Remove-Item -LiteralPath $OutputRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$buildRoot = Join-Path $OutputRoot 'build'
$packagePath = Join-Path $OutputRoot $packageFile
$utf8 = New-Object Text.UTF8Encoding($false)

function Invoke-AuthorSdkJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )
    $lines = @(& $sdkExe @Arguments --json 2>&1)
    $exitCode = $LASTEXITCODE
    $text = [string]::Join([Environment]::NewLine, @($lines | ForEach-Object { [string]$_ }))
    $path = Join-Path $OutputRoot ($Name + '.json')
    [IO.File]::WriteAllText($path, $text + [Environment]::NewLine, $utf8)
    if ($exitCode -ne 0) {
        throw "Author SDK $Name failed with exit $exitCode. Report: $path"
    }
    $report = $text | ConvertFrom-Json
    if (-not [bool]$report.success) {
        throw "Author SDK $Name returned success=false. Report: $path"
    }
    return $report
}

$validate = Invoke-AuthorSdkJson -Name 'validate-report' -Arguments @('validate', $projectRoot)
$restore = Invoke-AuthorSdkJson -Name 'restore-report' -Arguments @('restore', $projectRoot, '--offline', 'true')
$sourceTreeSha256 = Get-AuthorProductSourceTreeSha256 -SourceRoot (Join-Path $projectRoot 'src')
$pack = Invoke-AuthorSdkJson -Name 'pack-report' -Arguments @('pack', $projectRoot, '--game-root', $GameDir, '--build-output', $buildRoot, '--output', $packagePath)
if ((Get-AuthorProductSourceTreeSha256 -SourceRoot (Join-Path $projectRoot 'src')) -cne $sourceTreeSha256) {
    throw 'Product source files changed during pack; rerun from stable inputs.'
}
$factsPath = Join-Path $OutputRoot 'build-facts.xml'
Copy-Item -LiteralPath ([string]$pack.values.buildFactsPath) -Destination $factsPath
if ((Get-FileHash -LiteralPath $factsPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$pack.values.buildFactsSha256) {
    throw 'MSBuild facts changed after the SDK build report was produced.'
}
if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "Author SDK did not produce the Catalog package: $packagePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName })
    foreach ($required in @(
        'info.json',
        'Content/DTMAPI/manifest.json',
        "Content/DTMAPI/$entryDll",
        'Content/DTMAPI/dtmapi-advanced-references.json',
        'Content/DTMAPI/dtmapi-package.json'
    )) {
        if ($entries -notcontains $required) {
            throw "Catalog product '$CatalogId' SDK package is missing $required"
        }
    }
    $dlls = @($entries | Where-Object { $_ -like '*.dll' })
    if ($dlls.Count -ne 1 -or $dlls[0] -cne "Content/DTMAPI/$entryDll") {
        throw ("Catalog product '$CatalogId' package must contain exactly one canonical product DLL; found: " + [string]::Join(', ', $dlls))
    }
    $forbiddenNames = @('Assembly-CSharp.dll', '0Harmony.dll', 'BepInEx.dll')
    $forbidden = @($entries | Where-Object {
        $name = [IO.Path]::GetFileName($_)
        $forbiddenNames -contains $name -or $name -like 'UnityEngine*.dll'
    })
    if ($forbidden.Count -ne 0) {
        throw ("Catalog product '$CatalogId' bundled forbidden Runtime/native bytes: " + [string]::Join(', ', $forbidden))
    }

    function Read-ZipJson([string] $entryName) {
        $entry = $archive.GetEntry($entryName)
        if ($null -eq $entry) { throw "Missing ZIP JSON entry: $entryName" }
        $reader = New-Object IO.StreamReader($entry.Open(), (New-Object Text.UTF8Encoding($false, $true)))
        try { return ($reader.ReadToEnd() | ConvertFrom-Json) }
        finally { $reader.Dispose() }
    }

    $packagedManifest = Read-ZipJson 'Content/DTMAPI/manifest.json'
    if ([string]$packagedManifest.UniqueID -cne $uniqueId -or
        [string]$packagedManifest.Version -cne [string]$manifest.Version -or
        [string]$packagedManifest.Type -cne 'CodeMod' -or
        [string]$packagedManifest.CodeModKind -cne 'Advanced' -or
        [string]$packagedManifest.EntryDll -cne "Content/DTMAPI/$entryDll" -or
        [string]$packagedManifest.EntryType -cne $entryType -or
        [string]$packagedManifest.MinimumDTMApiVersion -cne [string]$manifest.MinimumDTMApiVersion) {
        throw "Catalog product '$CatalogId' packaged manifest drifted from source/Catalog identity."
    }
    $sourceDependencies = @($manifest.Dependencies) | ConvertTo-Json -Depth 8 -Compress
    $packagedDependencies = @($packagedManifest.Dependencies) | ConvertTo-Json -Depth 8 -Compress
    if ($sourceDependencies -cne $packagedDependencies) {
        throw "Catalog product '$CatalogId' packaged dependencies drifted from its source manifest."
    }

    $receipt = Read-ZipJson 'Content/DTMAPI/dtmapi-advanced-references.json'
    if ([string]$receipt.uniqueId -cne $uniqueId -or
        [string]$receipt.referencePolicyId -cne $policyId -or
        [string]$receipt.codeModKind -cne 'Advanced' -or
        [string]$receipt.harmonyOwner -cne $harmonyOwner -or
        [string]$receipt.entryDllPath -cne "Content/DTMAPI/$entryDll") {
        throw "Catalog product '$CatalogId' Advanced receipt drifted from its Catalog binding."
    }
    [string[]]$expectedReferenceNames = @($authorProject.advanced.references | ForEach-Object { [string]$_ })
    [string[]]$actualReferenceNames = @($receipt.references | ForEach-Object { [string]$_.assemblyName })
    [Array]::Sort($expectedReferenceNames, [StringComparer]::Ordinal)
    [Array]::Sort($actualReferenceNames, [StringComparer]::Ordinal)
    if ($expectedReferenceNames.Count -ne $actualReferenceNames.Count -or
        [string]::Join("`n", $expectedReferenceNames) -cne [string]::Join("`n", $actualReferenceNames)) {
        throw "Catalog product '$CatalogId' receipt reference set drifted from dtmapi.author.json."
    }

    $marker = Read-ZipJson 'Content/DTMAPI/dtmapi-package.json'
    if ([string]$marker.uniqueId -cne $uniqueId -or
        [string]$marker.codeModKind -cne 'Advanced' -or
        [string]$marker.authority -cne 'dtmapi-author-sdk-package-binding' -or
        [string]$marker.entryDllSha256 -cne [string]$pack.values.entryDllSha256 -or
        [string]$marker.advancedReferenceReceiptSha256 -cne [string]$pack.values.advancedReferenceReceiptSha256) {
        throw "Catalog product '$CatalogId' package marker does not bind the pack report hashes."
    }
}
finally {
    $archive.Dispose()
}

$summary = [ordered]@{
    schemaVersion = 3
    status = 'Passed'
    catalogId = $CatalogId
    uniqueId = $uniqueId
    version = [string]$manifest.Version
    codeModKind = 'Advanced'
    policyId = $policyId
    harmonyOwner = $harmonyOwner
    gameBuildId = [string]$policy.gameBuildId
    projectRoot = $projectRoot
    packagePath = $packagePath
    packageSha256 = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
    entryDllSha256 = [string]$pack.values.entryDllSha256
    advancedReferenceReceiptSha256 = [string]$pack.values.advancedReferenceReceiptSha256
    bundledNativeDependencies = @()
    buildOutput = [string]$pack.values.buildOutputPath
    sourceTreeSha256 = $sourceTreeSha256
    sourceTreeScope = 'product-src-csharp'
    buildFactsFile = 'build-facts.xml'
    buildFactsSha256 = [string]$pack.values.buildFactsSha256
    buildBackend = [string]$pack.values.buildBackend
    compilerSha256 = [string]$pack.values.compilerSha256
    buildReport = 'pack-report.json'
    validateFileCount = [int]$validate.fileCount
}
$summaryPath = Join-Path $OutputRoot 'summary.json'
[IO.File]::WriteAllText($summaryPath, (($summary | ConvertTo-Json -Depth 8) + "`n"), $utf8)
Write-Host "Batch 6 Catalog Advanced SDK package ($CatalogId): PASS"
Write-Host "Package: $packagePath"
Write-Host "SHA-256: $($summary.packageSha256)"
Write-Host "Summary: $summaryPath"
