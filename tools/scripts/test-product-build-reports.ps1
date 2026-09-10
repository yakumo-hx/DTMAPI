[CmdletBinding()]
param([Parameter(Mandatory = $true)] [string] $ArtifactRoot, [Parameter(Mandatory = $true)] [string] $LegacyArtifactRoot)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
$repo = Get-RepoRoot
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'check-release-contract.ps1'), [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw 'Release checker did not parse.' }
foreach ($definition in $ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.FunctionDefinitionAst] }) {
    . ([scriptblock]::Create($definition.Extent.Text))
}
$testRoot = Join-Path $repo ('temp/product-report-tests-' + [Guid]::NewGuid().ToString('N'))
$original = Get-Content -Raw -LiteralPath (Join-Path $ArtifactRoot 'summary.json') | ConvertFrom-Json
$catalog = Get-Content -Raw -LiteralPath (Join-Path $repo 'tools/release/dtmapi-product-catalog.json') | ConvertFrom-Json
$product = @($catalog.products | Where-Object { $_.catalogId -ceq $original.catalogId })[0]
$sourceManifest = Get-Content -Raw -LiteralPath (Join-Path $repo $product.sourceManifest) | ConvertFrom-Json
$arguments = @{
    ProjectRoot = [string]$original.projectRoot
    UniqueId = [string]$original.uniqueId
    AssemblyName = [IO.Path]::GetFileNameWithoutExtension([string]$sourceManifest.EntryDll)
    EntryDll = [string]$sourceManifest.EntryDll
    Version = [string]$original.version
    MinimumDtmApiVersion = [string]$sourceManifest.MinimumDTMApiVersion
    ReferencePolicyId = [string]$original.policyId
    HarmonyOwner = [string]$original.harmonyOwner
    PackageFile = [IO.Path]::GetFileName([string]$original.packagePath)
    ExpectedBinaryVersion = Get-ReleaseContractFourPartVersion -Version ([string]$original.version) -Label 'test product'
}
try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    foreach ($version in @(1, 2)) {
        $caseRoot = Join-Path $testRoot "schema-$version"
        Copy-Item -LiteralPath $ArtifactRoot -Destination $caseRoot -Recurse
        $summary = Get-Content -Raw -LiteralPath (Join-Path $caseRoot 'summary.json') | ConvertFrom-Json
        $pack = Get-Content -Raw -LiteralPath (Join-Path $caseRoot 'pack-report.json') | ConvertFrom-Json
        $summary.schemaVersion = $version
        $summary.packagePath = Join-Path $caseRoot $arguments.PackageFile
        $summary.buildOutput = Join-Path $caseRoot ('build/' + $arguments.EntryDll)
        $pack.outputPath = $summary.packagePath
        $pack.values.buildOutputPath = $summary.buildOutput
        if ($version -eq 1) {
            $legacy = Get-Content -Raw -LiteralPath (Join-Path $LegacyArtifactRoot 'build-report.json') | ConvertFrom-Json
            $legacy.outputPath = $summary.buildOutput
            Write-AuthorSdkUtf8NoBom -Path (Join-Path $caseRoot 'build-report.json') -Value ($legacy | ConvertTo-Json -Depth 12)
        }
        Write-AuthorSdkUtf8NoBom -Path (Join-Path $caseRoot 'summary.json') -Value ($summary | ConvertTo-Json -Depth 12)
        Write-AuthorSdkUtf8NoBom -Path (Join-Path $caseRoot 'pack-report.json') -Value ($pack | ConvertTo-Json -Depth 12)
        $script:failures = New-Object 'System.Collections.Generic.List[string]'
        $result = Test-ReleaseContractAuthorSdkArtifact -Label "schema $version" -ArtifactRoot $caseRoot @arguments
        if ($null -eq $result -or $script:failures.Count -gt 0) { throw "Valid schema $version was rejected: $($script:failures -join '; ')" }
        if ($version -eq 2) {
            if (Test-Path -LiteralPath (Join-Path $caseRoot 'build-report.json')) { throw 'New report test accidentally depended on a legacy build report.' }
            $summary.buildInputSha256 = '0' * 64
            Write-AuthorSdkUtf8NoBom -Path (Join-Path $caseRoot 'summary.json') -Value ($summary | ConvertTo-Json -Depth 12)
            $script:failures.Clear()
            $null = Test-ReleaseContractAuthorSdkArtifact -Label 'wrong input binding' -ArtifactRoot $caseRoot @arguments
            if ($script:failures.Count -eq 0) { throw 'Mismatched build input identity was accepted.' }
            $summary.schemaVersion = 99
            Write-AuthorSdkUtf8NoBom -Path (Join-Path $caseRoot 'summary.json') -Value ($summary | ConvertTo-Json -Depth 12)
            $script:failures.Clear()
            $null = Test-ReleaseContractAuthorSdkArtifact -Label 'unknown summary' -ArtifactRoot $caseRoot @arguments
            if ($script:failures.Count -eq 0) { throw 'Unknown summary schema was accepted.' }
        }
    }
    Write-Host 'Product build reports: PASS (real package, legacy v1, single-build v2, missing legacy report, invalid identity/schema)'
}
finally {
    $safe = Assert-AuthorSdkChildPath -Root (Join-Path $repo 'temp') -Path $testRoot -Label 'report fixture cleanup'
    if (Test-Path -LiteralPath $safe) { Remove-Item -LiteralPath $safe -Recurse -Force }
}
