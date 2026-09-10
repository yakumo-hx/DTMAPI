[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [ValidateSet('BuildProduct', 'TestProduct', 'SourceChecks', 'CaptureGame')] [string] $Operation,
    [string] $CatalogId = '', [ValidateSet('Unit', 'Game')] [string] $TestMode = 'Unit',
    [string] $ReferenceGameDir = '', [string] $GameDir = '', [string] $AuthorSdkRoot = '', [string] $OutputRoot = '',
    [switch] $AsJson, [switch] $NoFail
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
. "$PSScriptRoot/author-sdk-preparation.ps1"
. "$PSScriptRoot/workspace-preflight.ps1"
$arguments = @{}
foreach ($key in @('Operation','CatalogId','TestMode','ReferenceGameDir','GameDir','AuthorSdkRoot','OutputRoot')) { $arguments[$key] = Get-Variable -Name $key -ValueOnly }
$report = Get-DtmApiWorkspacePreflight -RepoRoot (Get-RepoRoot) @arguments
if ($AsJson) { $report | ConvertTo-Json -Depth 10 } else { $report | Format-List }
if (-not $report.ready -and -not $NoFail) { exit 1 }
