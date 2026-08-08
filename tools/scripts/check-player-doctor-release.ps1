param(
    [string] $PackageRoot = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
    $PackageRoot = Join-Path $repo 'dist\player-doctor\win-x64'
}
$root = [System.IO.Path]::GetFullPath($PackageRoot)
if (-not (Test-Path -LiteralPath $root -PathType Container)) {
    throw "Player Doctor release directory is missing: $root"
}

$expectedFiles = @('dtmapi-player-doctor.exe', 'dotnet-LICENSE.txt', 'dotnet-ThirdPartyNotices.txt') | Sort-Object
$actualFiles = @(Get-ChildItem -LiteralPath $root -File -Recurse | ForEach-Object {
    $_.FullName.Substring($root.TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
} | Sort-Object)
if (($actualFiles -join '|') -ne (($expectedFiles | ForEach-Object { $_.Replace('\', '/') }) -join '|')) {
    throw "Player Doctor release file set is not exact. Expected=$($expectedFiles -join ',') Actual=$($actualFiles -join ',')"
}

$exe = Join-Path $root 'dtmapi-player-doctor.exe'
$item = Get-Item -LiteralPath $exe
if ($item.Length -le 0) { throw "Player Doctor executable is empty: $exe" }
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if (-not [string]::Equals([string]$version.FileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
    throw "Player Doctor FileVersion mismatch. Expected=$script:DtmApiBinaryVersion Actual=$($version.FileVersion)"
}
if (-not [string]::Equals([string]$version.ProductVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal)) {
    throw "Player Doctor ProductVersion mismatch. Expected=$script:DtmApiReleaseVersion Actual=$($version.ProductVersion)"
}

$forbiddenNames = @('dtmapi-author.exe', 'templates', 'schemas', 'compatibility', 'author-sdk-release.json')
foreach ($forbidden in $forbiddenNames) {
    if (@(Get-ChildItem -LiteralPath $root -Force -Recurse | Where-Object { $_.Name.Equals($forbidden, [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) {
        throw "Player Doctor release contains forbidden Author SDK material: $forbidden"
    }
}

$versionOutput = & $exe version 2>&1
if ($LASTEXITCODE -ne 0 -or ([string]($versionOutput -join "`n")).IndexOf($script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -lt 0) {
    throw "Player Doctor version command failed or did not identify Runtime $script:DtmApiReleaseVersion. Output=$($versionOutput -join ' ')"
}

Write-Host 'Player Doctor release gate: PASS'
Write-Host "Executable: $exe"
Write-Host "SHA256: $((Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash)"
