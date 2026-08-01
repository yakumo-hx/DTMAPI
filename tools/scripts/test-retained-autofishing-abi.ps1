[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $BaselineAbstractionsDll,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $CandidateAbstractionsDll,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $AutoFishingDll,

    [string] $RetainedPublicWorkshopRoot,
    [string] $MandatoryGameBridgeDll,
    [string] $CompatibilityHostDll,
    [string] $ReportPath,
    [string] $Configuration = 'Release',
    [switch] $NoBuild
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

$expectedAutoFishingSha256 = 'E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA'
$expectedBaselineSha256 = '39A51683034FF0B7495BEB3DD1C50F76F591D4E24C63872B6502EA360EF8880B'
$expectedBaselineVersion = '0.5.2.0'
$expectedCandidateVersion = '0.5.3.0'
$expectedConsumerReferenceVersion = '0.5.1.0'

function Resolve-RequiredArtifactFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Label
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) {
        throw "$Label must be a file: $Path"
    }
    return $item.FullName
}

$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$project = Join-Path $repo 'tests\DTMAPI.AbiCompatibilityHarness\DTMAPI.AbiCompatibilityHarness.csproj'
$baseline = Resolve-RequiredArtifactFile -Path $BaselineAbstractionsDll -Label 'Baseline DTMAPI.Abstractions'
$candidate = Resolve-RequiredArtifactFile -Path $CandidateAbstractionsDll -Label 'Candidate DTMAPI.Abstractions'
$consumer = Resolve-RequiredArtifactFile -Path $AutoFishingDll -Label 'Retained AutoFishing consumer'
$hasMandatoryGameBridge = -not [string]::IsNullOrWhiteSpace($MandatoryGameBridgeDll)
$hasCompatibilityHost = -not [string]::IsNullOrWhiteSpace($CompatibilityHostDll)
if ($hasMandatoryGameBridge -xor $hasCompatibilityHost) {
    throw 'MandatoryGameBridgeDll and CompatibilityHostDll must be supplied together.'
}
$mandatoryGameBridge = $null
$compatibilityHost = $null
if ($hasMandatoryGameBridge) {
    $mandatoryGameBridge = Resolve-RequiredArtifactFile -Path $MandatoryGameBridgeDll -Label 'Mandatory GameBridge'
    $compatibilityHost = Resolve-RequiredArtifactFile -Path $CompatibilityHostDll -Label 'Compatibility Host'
    $hostProjectPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.Compatibility\DTMAPI.GameBridge.DolocTown.Compatibility.csproj'
    [xml] $hostProject = Get-Content -LiteralPath $hostProjectPath -Raw
    $targetFramework = [string]$hostProject.Project.PropertyGroup.TargetFramework
    if ($targetFramework -ne 'netstandard2.0') {
        throw "Compatibility Host project must target netstandard2.0, found '$targetFramework'."
    }
    $mandatoryProjectPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\DTMAPI.GameBridge.DolocTown.csproj'
    $mandatoryProjectText = Get-Content -LiteralPath $mandatoryProjectPath -Raw
    if ($mandatoryProjectText -match '<ProjectReference\s+Include="[^\"]*DTMAPI\.GameBridge\.DolocTown\.Compatibility') {
        throw 'Mandatory GameBridge project must not contain a static ProjectReference to the optional Compatibility Host.'
    }
}
$retainedProductDlls = @()
$retainedProductIds = @()
$retainedExternalDlls = @()
$retainedExternalIds = @()
if (-not [string]::IsNullOrWhiteSpace($RetainedPublicWorkshopRoot)) {
    $workshopRoot = (Get-Item -LiteralPath $RetainedPublicWorkshopRoot -Force -ErrorAction Stop).FullName
    $catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
    $catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
    $publicProducts = @($catalog.products | Where-Object {
        [string]$_.role -eq 'PublishedProduct' -and [string]$_.distributionState -eq 'PublicWorkshop'
    })
    if ($publicProducts.Count -ne 11) {
        throw "Expected exactly 11 retained public products in the Catalog, found $($publicProducts.Count)."
    }
    foreach ($product in $publicProducts) {
        $productRoot = Join-Path $workshopRoot ([string]$product.workshopId)
        $retainedArtifactProperty = $product.PSObject.Properties['retainedArtifact']
        $retainedEntryProperty = if ($null -ne $retainedArtifactProperty -and
            $null -ne $retainedArtifactProperty.Value) {
            $retainedArtifactProperty.Value.PSObject.Properties['entryDll']
        }
        else {
            $null
        }
        $retainedEntryDll = if ($null -ne $retainedEntryProperty -and
            -not [string]::IsNullOrWhiteSpace([string]$retainedEntryProperty.Value)) {
            [string]$retainedEntryProperty.Value
        }
        else {
            [string]$product.packageDll
        }
        $matches = @(Get-ChildItem -LiteralPath $productRoot -Recurse -File -Filter $retainedEntryDll -ErrorAction Stop)
        if ($matches.Count -ne 1) {
            throw "Expected one retained DLL '$retainedEntryDll' for '$($product.catalogId)', found $($matches.Count)."
        }
        $retainedProductDlls += $matches[0].FullName
        $retainedProductIds += [string]$product.catalogId
    }

    $externalConsumers = @(
        @{ Id = '3743621104'; RelativePath = 'DolocStorageExpansionMod.dll' },
        @{ Id = '3743644065'; RelativePath = 'DolocStoreCapacityMod.dll' },
        @{ Id = '3754869009'; RelativePath = 'Content\DTMAPI\DolocTownQoL.dll' },
        @{ Id = '3759797170'; RelativePath = 'Content\DTMAPI\Mxx_DolocTownMod_Installer.dll' }
    )
    foreach ($externalConsumer in $externalConsumers) {
        $externalPath = Join-Path (Join-Path $workshopRoot $externalConsumer.Id) $externalConsumer.RelativePath
        $retainedExternalDlls += Resolve-RequiredArtifactFile `
            -Path $externalPath `
            -Label "Retained external DTMAPI consumer $($externalConsumer.Id)"
        $retainedExternalIds += [string]$externalConsumer.Id
    }
}

$arguments = @(
    'run',
    '--project', $project,
    '-c', $Configuration
)
if ($NoBuild) {
    $arguments += '--no-build'
}
$arguments += @(
    '--',
    '--baseline-abstractions', $baseline,
    '--candidate-abstractions', $candidate,
    '--consumer', $consumer,
    '--expected-baseline-sha256', $expectedBaselineSha256,
    '--expected-consumer-sha256', $expectedAutoFishingSha256,
    '--expected-baseline-version', $expectedBaselineVersion,
    '--expected-candidate-version', $expectedCandidateVersion,
    '--expected-consumer-reference-version', $expectedConsumerReferenceVersion
)
if ($retainedProductDlls.Count -gt 0) {
    $arguments += @(
        '--retained-product-dlls', ($retainedProductDlls -join '|'),
        '--retained-product-ids', ($retainedProductIds -join '|')
    )
}
if ($retainedExternalDlls.Count -gt 0) {
    $arguments += @(
        '--retained-external-dlls', ($retainedExternalDlls -join '|'),
        '--retained-external-ids', ($retainedExternalIds -join '|')
    )
}
if ($hasMandatoryGameBridge) {
    $arguments += @(
        '--mandatory-gamebridge', $mandatoryGameBridge,
        '--compatibility-host', $compatibilityHost
    )
}
if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    $arguments += @('--report', [IO.Path]::GetFullPath($ReportPath))
}

& $dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Retained AutoFishing ABI gate failed with exit code $LASTEXITCODE."
}
