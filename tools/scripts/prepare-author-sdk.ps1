[CmdletBinding()]
param([string] $OutputRoot = '', [switch] $Check, [switch] $NoProvision)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
. "$PSScriptRoot/author-sdk-preparation.ps1"
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $OutputRoot = Join-Path $repo '.tools/author-sdk' }
$output = if ([IO.Path]::IsPathRooted($OutputRoot)) { [IO.Path]::GetFullPath($OutputRoot) } else { [IO.Path]::GetFullPath((Join-Path $repo $OutputRoot)) }
Assert-DtmApiBuildPathsDisjoint -OutputPath $output -InputPaths @((Join-Path $repo 'src'), (Join-Path $repo 'author-sdk'), (Join-Path $repo 'tools/scripts'), (Join-Path $repo '.tools/dotnet'), (Join-Path $repo '.tools/author-sdk-packages'))
$dotnet = Get-DotNetExe -NoProvision:($Check -or $NoProvision)
$inputSnapshot = Get-DtmApiAuthorSdkInput -RepoRoot $repo -DotNetExe $dotnet
if (-not (Test-DtmApiPreparedAuthorSdk -OutputRoot $output -InputSnapshot $inputSnapshot)) {
    if ($Check) { throw "Author SDK preparation is absent, changed or damaged. Run prepare-author-sdk.ps1 -OutputRoot '$output'." }
    & "$PSScriptRoot/build-author-sdk.ps1" -OutputRoot $output -NoProvision:$NoProvision
    if (-not $?) { throw 'Author SDK preparation build failed.' }
    $receipt = Get-Content -Raw -LiteralPath (Join-Path $output 'preparation.json') | ConvertFrom-Json
    $inputSnapshot.sha256 = [string]$receipt.inputSha256
    Write-Host "Author SDK prepared: $output"
}
else { Write-Host "Author SDK reused: $output" }
Write-Host "SDK input SHA-256: $($inputSnapshot.sha256)"
