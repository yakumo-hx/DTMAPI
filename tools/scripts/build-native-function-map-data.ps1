param(
    [string]$BuildRoot = "references/doloc-town/reverse/builds/23465763_workshop_38581E",
    [string]$ReportsDir = "docs/reviews/api/native-owner-domains",
    [string]$OutputDir = "docs/reviews/api/native-function-map/data"
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
}
finally {
    Pop-Location
}
