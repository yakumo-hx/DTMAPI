[CmdletBinding()]
param(
    [string] $GameDir = '',
    [string] $OutputRoot = '',
    [ValidateSet('Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
try {
    & "$PSScriptRoot\build-batch6-advanced-product.ps1" `
        -CatalogId 'auto-fishing' `
        -GameDir $GameDir `
        -OutputRoot $OutputRoot `
        -Configuration $Configuration
    if (-not $?) { throw 'The shared Advanced product builder returned failure.' }
}
catch {
    Write-Error $_.Exception.Message -ErrorAction Continue
    exit 1
}
