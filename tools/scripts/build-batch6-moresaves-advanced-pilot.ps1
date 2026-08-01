[CmdletBinding()]
param(
    [string] $GameDir = '',
    [string] $OutputRoot = '',
    [ValidateSet('Release')]
    [string] $Configuration = 'Release'
)

& "$PSScriptRoot\build-batch6-advanced-product.ps1" `
    -CatalogId 'more-saves' `
    -GameDir $GameDir `
    -OutputRoot $OutputRoot `
    -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
