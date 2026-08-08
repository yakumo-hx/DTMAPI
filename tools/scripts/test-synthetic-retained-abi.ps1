[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $CandidateAbstractionsDll,
    [string] $ReportPath,
    [switch] $NoBuild
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$project = Join-Path $repo 'tests\DTMAPI.AbiCompatibilityHarness\DTMAPI.AbiCompatibilityHarness.csproj'
$contract = Join-Path $repo 'tools\release\contracts\retained-abi-synthetic-contract.json'
if ([string]::IsNullOrWhiteSpace($CandidateAbstractionsDll)) {
    $CandidateAbstractionsDll = Join-Path $repo "src\DTMAPI.Abstractions\bin\$Configuration\netstandard2.0\DTMAPI.Abstractions.dll"
}
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $repo 'reports\synthetic-retained-abi-latest.json'
}
if (-not $NoBuild) {
    & $dotnet build $project -c $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& $dotnet run --project $project -c $Configuration --no-build -- `
    --synthetic-contract $contract `
    --candidate-abstractions $CandidateAbstractionsDll `
    --report $ReportPath
if ($LASTEXITCODE -ne 0) {
    throw "Tracked synthetic retained-ABI gate failed with exit code $LASTEXITCODE."
}
