[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\release-common.ps1"

$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
$expectedCatalogIds = @(
    'action-speed',
    'animal-husbandry-progress',
    'auto-fishing',
    'chest-locator-enhancer',
    'fish-roe-info',
    'more-saves',
    'one-action-complete',
    'y-console',
    'zoom'
)
$forbiddenCatalogIds = @('more-equipment-slots', 'strong-planting-gun', 'mine')

function Copy-ReleaseArtifactSetCatalog {
    param([Parameter(Mandatory = $true)] $Value)

    return ($Value | ConvertTo-Json -Depth 100 | ConvertFrom-Json)
}

function Assert-ReleaseArtifactSetThrows {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [scriptblock] $Action
    )

    $threw = $false
    try {
        & $Action | Out-Null
    }
    catch {
        $threw = $true
    }
    if (-not $threw) {
        throw "$Label did not fail closed."
    }
}

$deterministicJsonFixture = [ordered]@{
    name = 'DTMAPI host fixture'
    nested = [ordered]@{
        quoted = 'a"b'
        path = 'c\d'
        values = @(1, $true, $null)
    }
    emptyObject = [pscustomobject]@{}
    emptyArray = @()
}
$expectedCompactJson = '{"name":"DTMAPI host fixture","nested":{"quoted":"a\"b","path":"c\\d","values":[1,true,null]},"emptyObject":{},"emptyArray":[]}'
$actualCompactJson = $deterministicJsonFixture | ConvertTo-Json -Depth 10 -Compress
if ($actualCompactJson -cne $expectedCompactJson) {
    throw "The release JSON compact representation drifted on PowerShell $($PSVersionTable.PSVersion). Actual=$actualCompactJson"
}
$expectedPrettyJson = @(
    '{'
    '  "name": "DTMAPI host fixture",'
    '  "nested": {'
    '    "quoted": "a\"b",'
    '    "path": "c\\d",'
    '    "values": ['
    '      1,'
    '      true,'
    '      null'
    '    ]'
    '  },'
    '  "emptyObject": {},'
    '  "emptyArray": []'
    '}'
) -join [Environment]::NewLine
$actualPrettyJson = ConvertTo-DtmApiDeterministicPrettyJson -CompactJson $actualCompactJson
if ($actualPrettyJson -cne $expectedPrettyJson) {
    throw "The deterministic release JSON formatter drifted on PowerShell $($PSVersionTable.PSVersion)."
}
Assert-ReleaseArtifactSetThrows -Label 'mismatched deterministic JSON closing token' -Action {
    ConvertTo-DtmApiDeterministicPrettyJson -CompactJson '{]' | Out-Null
}
$releaseCommonSource = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'release-common.ps1')
foreach ($requiredJsonWriterText in @('ConvertTo-Json -Depth $Depth -Compress', 'ConvertTo-DtmApiDeterministicPrettyJson -CompactJson $compactJson')) {
    if (-not $releaseCommonSource.Contains($requiredJsonWriterText)) {
        throw "The release JSON writer no longer routes through the deterministic compact formatter: $requiredJsonWriterText"
    }
}

$selectedCatalogIds = @(Get-DtmApiReleaseContractAdvancedProducts -Catalog $catalog | ForEach-Object {
    [string](Get-DtmApiMapValue -Map $_ -Key 'catalogId' -Default '')
} | Sort-Object)
if (($selectedCatalogIds -join '|') -cne ($expectedCatalogIds -join '|')) {
    throw "The 0.6 exact release artifact set drifted. Expected=$($expectedCatalogIds -join '|'); actual=$($selectedCatalogIds -join '|')."
}
foreach ($forbiddenCatalogId in $forbiddenCatalogIds) {
    if ($selectedCatalogIds -ccontains $forbiddenCatalogId) {
        throw "Excluded product '$forbiddenCatalogId' entered the 0.6 release artifact set."
    }
}

foreach ($forbiddenCatalogId in $forbiddenCatalogIds) {
    Assert-ReleaseArtifactSetThrows -Label "$forbiddenCatalogId builder-selection mutation" -Action {
        $mutated = Copy-ReleaseArtifactSetCatalog -Value $catalog
        $product = @($mutated.products | Where-Object { [string]$_.catalogId -ceq $forbiddenCatalogId }) | Select-Object -First 1
        if ($null -eq $product) {
            throw "Test fixture product is missing: $forbiddenCatalogId"
        }
        $entrypoint = [pscustomobject]@{
            action = 'ExistingWorkshopUpdate'
            catalogId = $forbiddenCatalogId
            workshopId = [string]$product.workshopId
            version = [string]$product.sourceVersion
            officialFolder = [string]$product.officialFolder
            treeSha256 = ('0' * 64)
        }
        $mutated.releaseStop.publicMutationEntrypoints = @($mutated.releaseStop.publicMutationEntrypoints) + @($entrypoint)
        Get-DtmApiReleaseContractAdvancedProducts -Catalog $mutated
    }
}

Assert-ReleaseArtifactSetThrows -Label 'MoreEquipment retained-version mutation' -Action {
    $mutated = Copy-ReleaseArtifactSetCatalog -Value $catalog
    $mutated.releaseStop.explicitlyExcludedExistingWorkshopUpdates[0].retainedVersion = '1.0.0'
    Get-DtmApiReleaseContractAdvancedProducts -Catalog $mutated
}

Assert-ReleaseArtifactSetThrows -Label 'Manbo ordinary-retained activation removal' -Action {
    $mutated = Copy-ReleaseArtifactSetCatalog -Value $catalog
    $mutated.releaseStop.publicMutationEntrypoints = @($mutated.releaseStop.publicMutationEntrypoints | Where-Object {
        [string]$_.catalogId -cne 'manbo-cardboard-audio'
    })
    Get-DtmApiReleaseContractAdvancedProducts -Catalog $mutated
}

$routeCases = @(
    [pscustomobject]@{
        Label = 'default Release driver'
        Path = Join-Path $PSScriptRoot 'test.ps1'
        Required = @('Get-DtmApiReleaseContractAdvancedProducts', '$forbiddenReleaseProducts', 'build-batch6-advanced-product.ps1')
        Forbidden = @('$advancedReleaseProducts = @((', '$codeModKindProperty')
    },
    [pscustomobject]@{
        Label = 'release contract checker'
        Path = Join-Path $PSScriptRoot 'check-release-contract.ps1'
        Required = @('Get-DtmApiReleaseContractAdvancedProducts', 'Assert-ReleaseContractExactArtifactRoot', '$isReleaseArtifact', 'MoreEquipment retained published version')
        Forbidden = @()
    },
    [pscustomobject]@{
        Label = 'Workshop staging route'
        Path = Join-Path $PSScriptRoot 'build-release-workshop-packages.ps1'
        Required = @(
            'Get-DtmApiReleaseContractAdvancedProducts',
            '$forbiddenBuilderCatalogIds',
            'foreach ($mod in $releaseMods.ToArray())',
            'build-advanced-reference-game-fixture.ps1',
            '-PolicyId $referencePolicyId -OutputRoot $policyFixtureRoot',
            '-GameDir $productReferenceGameRoot',
            'Remove-Item -LiteralPath $advancedReferenceFixtureSessionRoot -Recurse -Force',
            'New-DtmApiWorkshopPublicationSession',
            'Publish-DtmApiWorkshopPublicationSession',
            'Restore-DtmApiWorkshopPublicationSessionAfterFailure',
            ".staging-' + `$token",
            ".previous-' + `$token",
            '$expectedStagingDirectories'
        )
        Forbidden = @('foreach ($mod in Get-DtmApiPublishedModDefinitions)')
    }
)
foreach ($routeCase in $routeCases) {
    $source = Get-Content -Raw -Encoding UTF8 -LiteralPath $routeCase.Path
    foreach ($requiredText in @($routeCase.Required)) {
        if (-not $source.Contains([string]$requiredText)) {
            throw "$($routeCase.Label) no longer contains required exact-set route text: $requiredText"
        }
    }
    foreach ($forbiddenText in @($routeCase.Forbidden)) {
        if ($source.Contains([string]$forbiddenText)) {
            throw "$($routeCase.Label) restored a forbidden broad-selection route: $forbiddenText"
        }
    }
    foreach ($forbiddenCatalogId in $forbiddenCatalogIds) {
        if (-not $source.Contains($forbiddenCatalogId)) {
            throw "$($routeCase.Label) lost the fail-before-builder assertion for '$forbiddenCatalogId'."
        }
    }
}

$workshopPlan = @(& (Join-Path $PSScriptRoot 'build-release-workshop-packages.ps1') -ModsOnly -PlanOnly)
$workshopPlanIds = @($workshopPlan | ForEach-Object { [string]$_.CatalogId } | Sort-Object)
if (($workshopPlanIds -join '|') -cne ($expectedCatalogIds -join '|')) {
    throw "The Workshop staging plan does not match the exact nine-product artifact set: $($workshopPlanIds -join '|')."
}
foreach ($workshopPlanItem in $workshopPlan) {
    if ([string]$workshopPlanItem.BuildScript -cne 'build-batch6-advanced-product.ps1') {
        throw "Workshop staging escaped the shared tracked Advanced builder for '$($workshopPlanItem.CatalogId)'."
    }
}

. (Join-Path $PSScriptRoot 'build-release-workshop-packages.ps1') -LibraryOnly
$publicationManagedParent = Join-Path $repo 'temp\test-runs\release-workshop-publication'
$publicationTestRoot = Join-Path $publicationManagedParent ([Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($publicationTestRoot) | Out-Null
try {
    $finalPublicationRoot = Join-Path $publicationTestRoot 'candidate-output'
    [System.IO.Directory]::CreateDirectory($finalPublicationRoot) | Out-Null
    $oldSentinel = Join-Path $finalPublicationRoot 'old-valid-candidate.txt'
    [System.IO.File]::WriteAllText($oldSentinel, 'old-valid-bytes', (New-Object System.Text.UTF8Encoding($false)))
    $oldSentinelSha256 = [string](Get-FileHash -LiteralPath $oldSentinel -Algorithm SHA256).Hash

    $failedSession = New-DtmApiWorkshopPublicationSession -FinalRoot $finalPublicationRoot
    try {
        [System.IO.File]::WriteAllText((Join-Path $failedSession.StagingRoot 'partial-build.txt'), 'partial', (New-Object System.Text.UTF8Encoding($false)))
        throw 'injected build failure before publication'
    }
    catch {
    }
    finally {
        Restore-DtmApiWorkshopPublicationSessionAfterFailure -Session $failedSession
    }
    if (-not (Test-Path -LiteralPath $oldSentinel -PathType Leaf) -or
        [string](Get-FileHash -LiteralPath $oldSentinel -Algorithm SHA256).Hash -cne $oldSentinelSha256 -or
        (Test-Path -LiteralPath $failedSession.StagingRoot) -or
        (Test-Path -LiteralPath $failedSession.BackupRoot)) {
        throw 'Failed Workshop staging did not preserve the previous candidate byte-for-byte and clean its transient sibling.'
    }

    $successSession = New-DtmApiWorkshopPublicationSession -FinalRoot $finalPublicationRoot
    $newSentinel = Join-Path $successSession.StagingRoot 'new-valid-candidate.txt'
    [System.IO.File]::WriteAllText($newSentinel, 'new-valid-bytes', (New-Object System.Text.UTF8Encoding($false)))
    $publishResult = Publish-DtmApiWorkshopPublicationSession -Session $successSession
    if (-not [bool]$publishResult.Published -or
        -not (Test-Path -LiteralPath (Join-Path $finalPublicationRoot 'new-valid-candidate.txt') -PathType Leaf) -or
        (Test-Path -LiteralPath (Join-Path $finalPublicationRoot 'old-valid-candidate.txt')) -or
        (Test-Path -LiteralPath $successSession.StagingRoot) -or
        (Test-Path -LiteralPath $successSession.BackupRoot)) {
        throw 'Successful Workshop publication did not exchange the completed sibling staging root exactly once.'
    }
}
finally {
    $resolvedPublicationRoot = [System.IO.Path]::GetFullPath($publicationTestRoot)
    $resolvedPublicationParent = [System.IO.Path]::GetFullPath($publicationManagedParent)
    if ((Test-DtmApiPathIsSameOrChild -Child $resolvedPublicationRoot -Parent $resolvedPublicationParent) -and
        [System.IO.Path]::GetFileName($resolvedPublicationRoot) -match '^[0-9a-f]{32}$' -and
        (Test-Path -LiteralPath $resolvedPublicationRoot -PathType Container)) {
        Remove-Item -LiteralPath $resolvedPublicationRoot -Recurse -Force
    }
    if ((Test-Path -LiteralPath $resolvedPublicationParent -PathType Container) -and
        @(Get-ChildItem -LiteralPath $resolvedPublicationParent -Force -ErrorAction Stop).Count -eq 0) {
        Remove-Item -LiteralPath $resolvedPublicationParent -Force
    }
}

$hostExe = if (Test-Path -LiteralPath (Join-Path $PSHOME 'pwsh.exe') -PathType Leaf) {
    Join-Path $PSHOME 'pwsh.exe'
}
else {
    Join-Path $PSHOME 'powershell.exe'
}
$missingGameRoot = Join-Path $repo ('temp\release-artifact-set-missing-game-' + [Guid]::NewGuid().ToString('N'))
foreach ($wrapperPath in @(Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter 'build-batch6-*-advanced-pilot.ps1')) {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $hostExe
    $startInfo.Arguments = ('-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -GameDir "{1}"' -f
        $wrapperPath.FullName.Replace('"', '""'),
        $missingGameRoot.Replace('"', '""'))
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    try {
        [void]$process.Start()
        $standardOutput = $process.StandardOutput.ReadToEnd()
        $standardError = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -eq 0) {
            throw ("Legacy wrapper failed to propagate the shared builder error: {0}. stdout={1}; stderr={2}" -f
                $wrapperPath.Name, $standardOutput.Trim(), $standardError.Trim())
        }
    }
    finally {
        $process.Dispose()
    }
}

Write-Host 'DTMAPI 0.6 exact release artifact set tests passed (9 selected; 3 forbidden before builder; deterministic cross-host JSON; MoreEquipment and Manbo retained lanes preserved).'
