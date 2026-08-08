[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $RuntimePackageRoot,
    [string] $CatalogPath = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$checker = Join-Path $PSScriptRoot 'check-product-catalog.ps1'
$runtimeRoot = [System.IO.Path]::GetFullPath($RuntimePackageRoot)
$catalog = if ([string]::IsNullOrWhiteSpace($CatalogPath)) {
    Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
}
else {
    [System.IO.Path]::GetFullPath($CatalogPath)
}
$infoPath = Join-Path $runtimeRoot 'info.json'
$fixtureRoot = Join-Path (Split-Path -Parent $runtimeRoot) ('runtime-info-boundary-' + [Guid]::NewGuid().ToString('N'))
$fixtureCatalogPath = Join-Path $fixtureRoot 'catalog.json'
$hostExe = (Get-Process -Id $PID).Path

function ConvertTo-ProcessArgument([string] $Value) {
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Invoke-CatalogChecker([string] $EffectiveCatalogPath) {
    $arguments = @(
        '-NoLogo',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $checker,
        '-Quiet',
        '-RuntimePackageRoot',
        $runtimeRoot,
        '-CatalogPath',
        $EffectiveCatalogPath
    )
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $hostExe
    $start.Arguments = [string]::Join(' ', @($arguments | ForEach-Object { ConvertTo-ProcessArgument ([string]$_) }))
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        Output = $stdout + [Environment]::NewLine + $stderr
    }
}

function Assert-CheckerPassed([string] $Label, [string] $EffectiveCatalogPath) {
    $result = Invoke-CatalogChecker $EffectiveCatalogPath
    if ($result.ExitCode -ne 0) {
        throw "$Label unexpectedly failed with exit code $($result.ExitCode): $($result.Output)"
    }
}

function Assert-CheckerRejected(
    [string] $Label,
    [string] $EffectiveCatalogPath,
    [string] $ExpectedOutput
) {
    $result = Invoke-CatalogChecker $EffectiveCatalogPath
    if ($result.ExitCode -eq 0) {
        throw "$Label unexpectedly passed the Runtime candidate/published info boundary."
    }
    if ($result.Output.IndexOf($ExpectedOutput, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "$Label failed without the expected diagnostic '$ExpectedOutput': $($result.Output)"
    }
}

function Restore-Info([byte[]] $Bytes) {
    [System.IO.File]::WriteAllBytes($infoPath, $Bytes)
}

function Read-FreshInfo([byte[]] $Bytes) {
    return ([System.Text.Encoding]::UTF8.GetString($Bytes) | ConvertFrom-Json)
}

function Write-JsonNoBom([string] $Path, [object] $Value) {
    $text = $Value | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($Path, $text, (New-Object System.Text.UTF8Encoding($false)))
}

if (-not (Test-Path -LiteralPath $runtimeRoot -PathType Container)) {
    throw "Runtime info boundary fixture root is missing: $runtimeRoot"
}
if (-not (Test-Path -LiteralPath $infoPath -PathType Leaf)) {
    throw "Runtime info boundary requires an existing positive-control info.json: $infoPath"
}
if (-not (Test-Path -LiteralPath $catalog -PathType Leaf)) {
    throw "Runtime info boundary Catalog is missing: $catalog"
}

$canonicalInfoBytes = [System.IO.File]::ReadAllBytes($infoPath)
$canonicalInfoHash = (Get-FileHash -LiteralPath $infoPath -Algorithm SHA256).Hash
if ($canonicalInfoBytes.Length -ne 9192 -or
    $canonicalInfoHash -cne 'C148F2EBB71805A9D749C7683F8EF9B053E74C6A049F64290EEC6AF432A82D41') {
    throw "Runtime info boundary positive control is not the exact 0.6 candidate: $($canonicalInfoBytes.Length)/$canonicalInfoHash"
}

try {
    [System.IO.Directory]::CreateDirectory($fixtureRoot) | Out-Null
    Assert-CheckerPassed 'Exact Runtime candidate positive control' $catalog

    $info = Read-FreshInfo $canonicalInfoBytes
    $info.PSObject.Properties.Remove('version')
    Write-JsonNoBom $infoPath $info
    Assert-CheckerRejected 'Missing Runtime candidate version' $catalog 'Built Runtime info.json candidate version'
    Restore-Info $canonicalInfoBytes

    $info = Read-FreshInfo $canonicalInfoBytes
    $info.version = '0.6.1'
    Write-JsonNoBom $infoPath $info
    Assert-CheckerRejected 'Wrong Runtime candidate version' $catalog 'Built Runtime info.json candidate version'
    Restore-Info $canonicalInfoBytes

    $info = Read-FreshInfo $canonicalInfoBytes
    $info.description = ([string]$info.description) + ' '
    Write-JsonNoBom $infoPath $info
    Assert-CheckerRejected 'Legal JSON byte drift' $catalog 'matches current candidate stable-form SHA-256'
    Restore-Info $canonicalInfoBytes

    Remove-Item -LiteralPath $infoPath -Force
    Assert-CheckerRejected 'Missing Runtime info.json' $catalog 'Required JSON file is missing'
    Restore-Info $canonicalInfoBytes

    $info = Read-FreshInfo $canonicalInfoBytes
    $info.PSObject.Properties.Remove('localized_name')
    Write-JsonNoBom $infoPath $info
    Assert-CheckerRejected 'Missing Runtime localized_name' $catalog 'localized_name language set'
    Restore-Info $canonicalInfoBytes

    $info = Read-FreshInfo $canonicalInfoBytes
    $info.localized_name.english = 'DTMAPI'
    Write-JsonNoBom $infoPath $info
    Assert-CheckerRejected 'Non-empty Runtime localized_name' $catalog 'localized_name.english remains the official empty fallback'
    Restore-Info $canonicalInfoBytes

    $catalogFixture = [System.IO.File]::ReadAllText($catalog, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $catalogFixture.runtime.currentSourceBaseline.candidateInfoJsonSha256 =
        [string]$catalogFixture.runtime.currentPublishedArtifact.infoJsonSha256
    Write-JsonNoBom $fixtureCatalogPath $catalogFixture
    Assert-CheckerRejected 'Candidate owner replaced by published info identity' $fixtureCatalogPath 'Current Runtime candidate info.json SHA-256'

    $catalogFixture = [System.IO.File]::ReadAllText($catalog, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $catalogFixture.runtime.currentPublishedArtifact.infoJsonSha256 =
        [string]$catalogFixture.runtime.currentSourceBaseline.candidateInfoJsonSha256
    Write-JsonNoBom $fixtureCatalogPath $catalogFixture
    Assert-CheckerRejected 'Published owner replaced by candidate info identity' $fixtureCatalogPath 'Published current Runtime info.json SHA-256'

    Write-Host 'Runtime candidate/published info hostile matrix: PASS'
    Write-Host '  positive=exact candidate'
    Write-Host '  package-negative=6 catalog-owner-negative=2'
}
finally {
    if ($canonicalInfoBytes.Count -gt 0) {
        Restore-Info $canonicalInfoBytes
    }
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
