[CmdletBinding()]
param(
    [string] $CandidateAbstractionsDll = '',
    [string] $ReportPath = '',
    [string] $Configuration = 'Release',
    [string] $TestRoot = $env:DTMAPI_TEST_TEMP_ROOT,
    [switch] $NoBuild
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

if ($Configuration -cne 'Release') {
    throw 'The mandatory real retained-consumer ABI gate is Release-only.'
}
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repo 'tmp\test-runs'
}
if ([string]::IsNullOrWhiteSpace($CandidateAbstractionsDll)) {
    $CandidateAbstractionsDll = Join-Path $repo 'src\DTMAPI.Abstractions\bin\Release\netstandard2.0\DTMAPI.Abstractions.dll'
}
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $repo 'reports\retained-release-abi-latest.json'
}

$retainedAbiInputs = & "$PSScriptRoot\resolve-retained-abi-inputs.ps1" `
    -RepositoryRoot $repo `
    -TestRoot $TestRoot
if ($null -eq $retainedAbiInputs) {
    throw 'Default Release retained-ABI input resolution returned no inputs.'
}
Write-Host "Retained Release ABI inputs resolved through $($retainedAbiInputs.ResolutionMode)."

try {
    & "$PSScriptRoot\test-retained-autofishing-abi.ps1" `
        -BaselineAbstractionsDll $retainedAbiInputs.BaselineAbstractionsDll `
        -CandidateAbstractionsDll $CandidateAbstractionsDll `
        -AutoFishingDll $retainedAbiInputs.AutoFishingDll `
        -RetainedPublicWorkshopRoot $retainedAbiInputs.PublicWorkshopRoot `
        -ReportPath $ReportPath `
        -Configuration $Configuration `
        -NoBuild:$NoBuild
    if (-not $?) {
        throw 'Default Release retained-ABI gate failed.'
    }
}
finally {
    if (-not [string]::IsNullOrWhiteSpace([string]$retainedAbiInputs.CleanupRoot)) {
        $retainedCleanupRoot = [System.IO.Path]::GetFullPath([string]$retainedAbiInputs.CleanupRoot)
        $managedTestRoot = [System.IO.Path]::GetFullPath($TestRoot)
        if (-not (Test-DtmApiPathIsSameOrChild -Child $retainedCleanupRoot -Parent $managedTestRoot) -or
            $retainedCleanupRoot.TrimEnd([char[]]@('\', '/')) -eq $managedTestRoot.TrimEnd([char[]]@('\', '/'))) {
            throw "Refusing to clean retained-ABI extraction root outside its managed test boundary: $retainedCleanupRoot"
        }
        if (Test-Path -LiteralPath $retainedCleanupRoot) {
            Remove-Item -LiteralPath $retainedCleanupRoot -Recurse -Force
        }
    }
}
