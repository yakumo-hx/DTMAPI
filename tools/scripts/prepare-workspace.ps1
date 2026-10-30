[CmdletBinding()]
param([ValidateSet('DotNet', 'AuthorSdk')] [string[]] $Dependency = @('DotNet'), [string] $AuthorSdkRoot = '')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
$repo = Get-RepoRoot
# Preparation is explicit and limited to selected missing/invalid dependencies.
# Source projects restore their own packages during their normal first build.
if ('DotNet' -in $Dependency -or 'AuthorSdk' -in $Dependency) {
    $dotnet = Get-DotNetExe -RepoRoot $repo
    Write-Host "Compatible .NET 8 host: $dotnet"
}
if ('AuthorSdk' -in $Dependency) {
    & "$PSScriptRoot/prepare-author-sdk.ps1" -OutputRoot $AuthorSdkRoot
    if (-not $?) { throw 'Author SDK preparation failed.' }
}
