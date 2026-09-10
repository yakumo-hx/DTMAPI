[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/release-common.ps1"
. "$PSScriptRoot/product-projections.ps1"
$repo = Get-RepoRoot
$root = Join-Path $repo ('temp/product-projections-' + [Guid]::NewGuid().ToString('N'))
$checks = 0
function Assert-Projection { param([bool] $Condition, [string] $Message); if (-not $Condition) { throw $Message }; $script:checks++ }
try {
    New-Item -ItemType Directory -Path $root | Out-Null
    $catalog = Get-Content -LiteralPath (Join-Path $repo 'tools/release/dtmapi-product-catalog.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $definitions = @(Get-DtmApiProductDefinitions)
    $expected = @($catalog.products | Where-Object { (Get-DtmApiMapValue $_ 'legacyReleaseLane' '') -in @('PublishedBuilder','DeveloperInstallOnly','ExplicitQaInstallOnly') })
    Assert-Projection ($definitions.Count -eq $expected.Count) 'Catalog lane projection lost or added a product.'
    foreach ($product in $expected) {
        $definition = @($definitions | Where-Object { $_.UniqueID -ceq $product.uniqueId })
        Assert-Projection ($definition.Count -eq 1 -and $definition[0].PackageName -ceq $product.packageName -and $definition[0].OfficialFolder -ceq $product.officialFolder) "Changed package identity for $($product.catalogId)."
    }
    Assert-Projection (@(Get-DtmApiPublishedModDefinitions | Where-Object { [bool](Get-DtmApiMapValue $_ 'QaFixture' $false) -or [bool](Get-DtmApiMapValue $_ 'DeveloperOnly' $false) }).Count -eq 0) 'Developer/QA product entered published definitions.'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'release-common.ps1') -Destination $root
    Copy-Item -LiteralPath $script:DtmApiVersionAuthority.Path -Destination $root
    $snapshot = Join-Path $root 'dtmapi-product-definitions.json'
    & {
        function Get-FileHash { throw 'Optional hash cmdlet is unavailable in this host.' }
        Export-DtmApiProductDefinitionsSnapshot -Path $snapshot
    }
    $snapshotValue = Get-Content -LiteralPath $snapshot -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-Projection ($snapshotValue.sourceCatalogSha256 -ceq (Get-DtmApiFileSha256 -Path (Join-Path $repo 'tools/release/dtmapi-product-catalog.json')).ToLowerInvariant()) 'Snapshot export must preserve Catalog identity without optional hash cmdlets.'
    $run = Join-Path $root 'read.ps1'
    [IO.File]::WriteAllText($run, '. "$PSScriptRoot/release-common.ps1"; @(Get-DtmApiProductDefinitions) | ConvertTo-Json -Depth 8', [Text.Encoding]::UTF8)
    $hostPath = Get-DtmApiPowerShellHost
    $portableOutput = @(& $hostPath -NoProfile -ExecutionPolicy Bypass -File $run)
    $portableExitCode = $LASTEXITCODE
    $portable = ($portableOutput -join "`n") | ConvertFrom-Json
    Assert-Projection ($portableExitCode -eq 0 -and @($portable).Count -eq $definitions.Count) 'Portable definitions require a repository or a missing helper.'
    foreach ($definition in $definitions) {
        $loaded = @($portable | Where-Object { $_.UniqueID -ceq $definition.UniqueID })
        Assert-Projection ($loaded.Count -eq 1 -and $loaded[0].PackageName -ceq $definition.PackageName) 'Portable product identity differs.'
    }
    $bad = Get-Content -LiteralPath $snapshot -Raw | ConvertFrom-Json
    $bad.definitions[0].OfficialFolder = '../outside'
    Write-Utf8NoBomJson -Path $snapshot -Value $bad
    $rejectionLog = Join-Path $root 'rejected.log'
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        # Windows PowerShell reports expected native stderr as NativeCommandError.
        $ErrorActionPreference = 'Continue'
        & $hostPath -NoProfile -ExecutionPolicy Bypass -File $run *> $rejectionLog
        $rejectedExitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousErrorActionPreference }
    Assert-Projection ($rejectedExitCode -ne 0) 'Packaged path traversal was accepted.'
    Assert-Projection ((Get-Content -LiteralPath $rejectionLog -Raw) -match 'Invalid product OfficialFolder') 'Portable rejection did not identify the unsafe product path.'
    $projection = Get-DtmApiPublishProjection -RepoRoot $repo
    $copy = Get-Content -LiteralPath (Join-Path $repo 'tools/release/dtmapi-mod-publish-zh.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($row in $copy.mods) {
        $generated = @($projection.mods | Where-Object { $_.catalogId -ceq $row.catalogId })
        Assert-Projection ($generated.Count -eq 1 -and $generated[0].steamDescription -ceq $row.steamDescription -and $generated[0].steamName -ceq $row.steamName) 'Manual upload wording was changed by projection.'
    }
    $rt = @($projection.mods | Where-Object { $_.uniqueId -ceq 'DTMAPI.Runtime' })[0]
    Assert-Projection ($rt.version -ceq $script:DtmApiReleaseVersion -and @($rt.distributions).Count -eq @($catalog.runtime.distributions).Count) 'Runtime source/distribution projection changed.'
    Assert-Projection (@($rt.distributions | Where-Object { $_.distributionId -eq 'dtmapi-multiplatform' })[0].uploadAuthorization -ceq 'None') 'Projection granted upload authorization.'
    Write-Host "Product projection behavior: PASS ($checks checks; portable Windows PowerShell process included)."
}
finally {
    $resolved = [IO.Path]::GetFullPath($root)
    $allowed = [IO.Path]::GetFullPath((Join-Path $repo 'temp')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Refusing test cleanup outside temp.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
exit 0
