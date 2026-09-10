[CmdletBinding()]
param([string] $OutputRoot = '', [string] $NetStandardPackageRoot = '', [string] $FrozenAbstractionsDll = '', [string] $ApiTarget = '0.5.5', [switch] $NoProvision, [switch] $Check)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
. "$PSScriptRoot/author-sdk-preparation.ps1"
. "$PSScriptRoot/author-sdk-compatibility.ps1"
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo -NoProvision:($NoProvision -or $Check)
$output = if (-not $OutputRoot) { Join-Path $repo '.tools/author-sdk-compatibility' } elseif ([IO.Path]::IsPathRooted($OutputRoot)) { $OutputRoot } else { Join-Path $repo $OutputRoot }
Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -DotNetExe $dotnet -OutputRoot $output -NetStandardPackageRoot $NetStandardPackageRoot -FrozenAbstractionsDll $FrozenAbstractionsDll -ApiTarget $ApiTarget -NoProvision:$NoProvision -Check:$Check | ConvertTo-Json -Depth 4
