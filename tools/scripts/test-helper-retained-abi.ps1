[CmdletBinding()]
param([string] $ReportPath = '', [switch] $NoBuild)
. "$PSScriptRoot/common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$abiDotnet = Get-DotNetExe -RepoRoot $repo
$frozen = Join-Path $repo '.tools/author-sdk-compatibility/0.5.5/DTMAPI.Abstractions.dll'
if (-not (Test-Path -LiteralPath $frozen)) {
    & "$PSScriptRoot/prepare-author-sdk-compatibility.ps1"
    if ($LASTEXITCODE -ne 0) { throw 'Frozen compatibility preparation failed.' }
}
$sourceReceipt = Get-Content -Raw -LiteralPath (Join-Path $repo 'author-sdk/compatibility/0.5.5/source-build.json') | ConvertFrom-Json
$frozenHash = (Get-FileHash -LiteralPath $frozen -Algorithm SHA256).Hash
if ($frozenHash -ne $sourceReceipt.abstractionsSha256) { throw 'Frozen Abstractions hash mismatch.' }
foreach ($name in $(if ($NoBuild) { @() } else { @('HelperImplementer','HelperConsumer') })) {
    $project = Join-Path $repo "tests/DTMAPI.AbiCompatibilityHarness/Fixtures/$name/$name.csproj"
    & $abiDotnet build $project -c Release "-p:FrozenAbstractionsPath=$frozen" "-p:FrozenAbstractionsSha256=$frozenHash"
    if ($LASTEXITCODE -ne 0) { throw "Frozen $name build failed." }
}
$harness = Join-Path $repo 'tests/DTMAPI.AbiCompatibilityHarness/DTMAPI.AbiCompatibilityHarness.csproj'
if (-not $NoBuild) {
    & $abiDotnet build $harness -c Release
    if ($LASTEXITCODE -ne 0) { throw 'ABI harness build failed.' }
}
if (-not $ReportPath) { $ReportPath = Join-Path $repo 'reports/helper-retained-abi-latest.json' }
& $abiDotnet run --project $harness -c Release --no-build -- `
    --baseline-abstractions $frozen --expected-baseline-sha256 $frozenHash `
    --candidate-abstractions (Join-Path $repo 'src/DTMAPI.Abstractions/bin/Release/netstandard2.0/DTMAPI.Abstractions.dll') `
    --helper-implementer (Join-Path $repo 'tests/DTMAPI.AbiCompatibilityHarness/Fixtures/HelperImplementer/bin/Release/netstandard2.0/Frozen.HelperImplementer.dll') `
    --helper-consumer (Join-Path $repo 'tests/DTMAPI.AbiCompatibilityHarness/Fixtures/HelperConsumer/bin/Release/netstandard2.0/Frozen.HelperConsumer.dll') `
    --report $ReportPath
if ($LASTEXITCODE -ne 0) { throw 'Actual retained helper ABI calls failed.' }
