# Repository-only projection helpers. Portable installers use the minimal snapshot
# loaded by release-common.ps1 and never need this file or the source Catalog.
function Get-DtmApiPublishProjection {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    $catalog = Get-Content -LiteralPath (Join-Path $RepoRoot 'tools/release/dtmapi-product-catalog.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $copy = Get-Content -LiteralPath (Join-Path $RepoRoot 'tools/release/dtmapi-mod-publish-zh.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([int]$copy.schemaVersion -eq 1) { return $copy }
    if ([int]$copy.schemaVersion -ne 2) { throw 'Unsupported publish-copy schema.' }
    $seen = @{}
    $rows = @(
        foreach ($text in $copy.mods) {
            foreach ($property in $text.PSObject.Properties.Name) {
                if ($property -notin @('catalogId', 'author', 'steamName', 'steamDescription', 'localizedDescription')) {
                    throw "Publish copy contains a generated or unknown field: $property"
                }
            }
            $id = [string]$text.catalogId
            if ([string]::IsNullOrWhiteSpace($id) -or $seen.ContainsKey($id)) { throw "Duplicate or empty publish-copy Catalog id: '$id'." }
            $seen[$id] = $true
            foreach ($field in @('steamName', 'steamDescription')) {
                if ([string]::IsNullOrWhiteSpace([string](Get-DtmApiMapValue $text $field ''))) { throw "Missing $field for publish copy '$id'." }
            }
            $row = [ordered]@{ catalogId = $id; steamName = $text.steamName; steamDescription = $text.steamDescription }
            if ($null -ne $text.PSObject.Properties['localizedDescription']) { $row.localizedDescription = $text.localizedDescription }
            if ($id -ceq [string]$catalog.runtime.catalogId) {
                $runtime = $catalog.runtime
                $windows = @($runtime.distributions | Where-Object { $_.distributionId -ceq 'dtmapi-windows' })
                if ($windows.Count -ne 1) { throw 'Catalog must contain one Windows Runtime distribution.' }
                $row.uniqueId = $runtime.uniqueId
                $row.modName = $text.steamName
                $row.author = Get-DtmApiMapValue $text 'author' ''
                $row.version = $script:DtmApiReleaseVersion
                $row.gameDescription = $text.steamDescription
                $row.manifestName = $runtime.publicTitle
                $row.folder = Split-Path -Leaf $windows[0].localUploadPath
                $row.scope = 'runtime'
                $row.workshopId = $runtime.workshopId
                $row.localUploadPath = $windows[0].localUploadPath
                $row.distributions = @(
                    foreach ($distribution in $runtime.distributions) {
                        $route = [ordered]@{ distributionId = $distribution.distributionId; workshopId = $distribution.workshopId; localUploadPath = $distribution.localUploadPath }
                        foreach ($field in $distribution.publishMetadata.PSObject.Properties) { $route[$field.Name] = $field.Value }
                        if ($null -ne $distribution.PSObject.Properties['uploadAuthorization']) { $route.uploadAuthorization = $distribution.uploadAuthorization }
                        [pscustomobject]$route
                    }
                )
            }
            else {
                $matches = @($catalog.products | Where-Object { $_.catalogId -ceq $id })
                if ($matches.Count -ne 1) { throw "Publish copy '$id' must resolve to exactly one Catalog product." }
                $product = $matches[0]
                $source = [string]$product.sourceRoot
                if ([string]::IsNullOrWhiteSpace($source)) { throw "Publish copy '$id' has no current source root." }
                $manifest = Get-Content -LiteralPath (Join-Path $RepoRoot $product.sourceManifest) -Raw -Encoding UTF8 | ConvertFrom-Json
                $infoPath = $source + '/official-info.json'
                $info = Get-Content -LiteralPath (Join-Path $RepoRoot $infoPath) -Raw -Encoding UTF8 | ConvertFrom-Json
                if ([string]$manifest.UniqueID -cne [string]$product.uniqueId -or [string]$manifest.Version -cne [string]$product.sourceVersion -or [string]$info.version -cne [string]$product.sourceVersion) {
                    throw "Current source identity/version differs from Catalog for '$id'."
                }
                $row.uniqueId = $product.uniqueId
                $row.modName = $info.name
                $row.author = $info.author
                $row.version = $product.sourceVersion
                $row.gameDescription = $info.description
                $row.manifestName = $manifest.Name
                $row.officialInfoPath = $infoPath
                $row.folder = Get-DtmApiMapValue $product 'officialFolder' ''
                $row.scope = switch ([string]$product.role) {
                    'PublishedProduct' { 'published' }
                    'QaFixture' { 'qaFixture' }
                    'ApiDemandSample' { 'apiDemandSample' }
                    default { if ([string]$product.legacyReleaseLane -ceq 'DeveloperInstallOnly') { 'developerOfficial' } else { 'localOfficial' } }
                }
            }
            [pscustomobject]$row
        }
    )
    return [pscustomobject]@{ schemaVersion = 2; language = $copy.language; scopes = $copy.scopes; mods = $rows }
}
