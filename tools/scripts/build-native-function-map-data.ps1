param(
    [Parameter(Mandatory = $true)]
    [string]$BuildRoot,
    [string]$ReportsDir = "docs/reviews/api/native-owner-domains",
    [string]$OutputDir = "tools/native-function-map/workbench/data"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$script = Join-Path $repoRoot "tools\native-function-map\build_native_function_map.py"

if (-not (Test-Path -LiteralPath $script)) {
    throw "Missing generator script: $script"
}

Push-Location $repoRoot
try {
    python $script --build-root $BuildRoot --reports-dir $ReportsDir --output-dir $OutputDir
    if ($LASTEXITCODE -ne 0) { throw "Native function map generation failed, exit $LASTEXITCODE." }
}
finally {
    Pop-Location
}
