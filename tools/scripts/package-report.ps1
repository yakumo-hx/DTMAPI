param(
    [string] $CaseId = 'MANUAL'
)

. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
$evidenceRoot = Join-Path $repo "docs\debug\evidence\$CaseId"
if (-not (Test-Path $evidenceRoot)) {
    throw "Evidence root not found: $evidenceRoot"
}
$latest = Get-ChildItem -LiteralPath $evidenceRoot -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $latest) {
    throw "No evidence folders under $evidenceRoot"
}
$zip = "$($latest.FullName).zip"
if (Test-Path $zip) {
    Remove-Item -Force -LiteralPath $zip
}
Compress-Archive -Path (Join-Path $latest.FullName '*') -DestinationPath $zip
Write-Host $zip
