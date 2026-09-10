param(
    [string] $TestRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"

function Assert-WorkshopDownloadMarkerTest {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-WorkshopTestAlternateStreamNames {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    return @(Get-Item -LiteralPath $Path -Stream * -ErrorAction Stop |
        Where-Object { [string]$_.Stream -cne ':$DATA' } |
        ForEach-Object { [string]$_.Stream })
}

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repo 'tmp\test-runs'
}

$builderPath = Join-Path $PSScriptRoot 'build-release-workshop-packages.ps1'
$builderText = [System.IO.File]::ReadAllText($builderPath)
$runtimeMarkerCall = [regex]::Matches($builderText, '(?m)^\s*Remove-DtmApiWorkshopDownloadMarkers\s+-PackageRoot\s+\$runtimePackage\s*$')
$modMarkerCall = [regex]::Matches($builderText, '(?m)^\s*Remove-DtmApiWorkshopDownloadMarkers\s+-PackageRoot\s+\$package\s*$')
Assert-WorkshopDownloadMarkerTest -Condition ($runtimeMarkerCall.Count -eq 1) -Message 'Runtime Workshop packaging must normalize download markers exactly once.'
Assert-WorkshopDownloadMarkerTest -Condition ($modMarkerCall.Count -eq 1) -Message 'Each mod Workshop packaging loop must normalize download markers exactly once.'

$resolvedTestRoot = [System.IO.Path]::GetFullPath($TestRoot)
New-Item -ItemType Directory -Force -Path $resolvedTestRoot | Out-Null
$fixtureRoot = [System.IO.Path]::GetFullPath((Join-Path $resolvedTestRoot ('workshop-download-markers-' + [guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $fixtureRoot -Parent $resolvedTestRoot) -or
    [string]::Equals($fixtureRoot, $resolvedTestRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Workshop download-marker fixture escaped its managed test root: $fixtureRoot"
}

New-Item -ItemType Directory -Force -Path $fixtureRoot | Out-Null
try {
    $knownRoot = Join-Path $fixtureRoot 'known marker package 中文'
    New-Item -ItemType Directory -Force -Path $knownRoot | Out-Null
    $knownFile = Join-Path $knownRoot 'payload.dll'
    [System.IO.File]::WriteAllText($knownFile, 'DTMAPI workshop payload')
    $knownHashBefore = Get-DtmApiFileSha256 -Path $knownFile
    Set-Content -LiteralPath $knownFile -Stream 'Zone.Identifier' -Encoding ASCII -Value "[ZoneTransfer]`r`nZoneId=3"

    $knownStreamsBefore = @(Get-WorkshopTestAlternateStreamNames -Path $knownFile)
    Assert-WorkshopDownloadMarkerTest -Condition ($knownStreamsBefore.Count -eq 1 -and $knownStreamsBefore[0] -ceq 'Zone.Identifier') -Message 'Known-marker fixture did not create exactly one Zone.Identifier stream.'

    Remove-DtmApiWorkshopDownloadMarkers -PackageRoot $knownRoot

    $knownStreamsAfter = @(Get-WorkshopTestAlternateStreamNames -Path $knownFile)
    $knownHashAfter = Get-DtmApiFileSha256 -Path $knownFile
    Assert-WorkshopDownloadMarkerTest -Condition ($knownStreamsAfter.Count -eq 0) -Message 'Zone.Identifier remained after Workshop package normalization.'
    Assert-WorkshopDownloadMarkerTest -Condition ([string]::Equals($knownHashBefore, $knownHashAfter, [System.StringComparison]::Ordinal)) -Message 'Workshop marker normalization changed the payload data stream.'

    $unexpectedRoot = Join-Path $fixtureRoot 'unexpected marker package'
    New-Item -ItemType Directory -Force -Path $unexpectedRoot | Out-Null
    $unexpectedFile = Join-Path $unexpectedRoot 'payload.dll'
    [System.IO.File]::WriteAllText($unexpectedFile, 'DTMAPI workshop payload')
    Set-Content -LiteralPath $unexpectedFile -Stream 'Zone.Identifier' -Encoding ASCII -Value "[ZoneTransfer]`r`nZoneId=3"
    Set-Content -LiteralPath $unexpectedFile -Stream 'DTMAPI.Unexpected' -Encoding ASCII -Value 'must fail closed'

    $unexpectedRejected = $false
    $unexpectedMessage = ''
    try {
        Remove-DtmApiWorkshopDownloadMarkers -PackageRoot $unexpectedRoot
    }
    catch {
        $unexpectedRejected = $true
        $unexpectedMessage = [string]$_.Exception.Message
    }

    Assert-WorkshopDownloadMarkerTest -Condition $unexpectedRejected -Message 'Workshop normalization accepted an unexpected alternate data stream.'
    Assert-WorkshopDownloadMarkerTest -Condition ($unexpectedMessage -match 'unexpected alternate data stream' -and $unexpectedMessage -match 'DTMAPI\.Unexpected') -Message "Unexpected ADS rejection was not diagnostic: $unexpectedMessage"
    $unexpectedStreamsAfter = @(Get-WorkshopTestAlternateStreamNames -Path $unexpectedFile)
    Assert-WorkshopDownloadMarkerTest -Condition ($unexpectedStreamsAfter -ccontains 'DTMAPI.Unexpected') -Message 'Fail-closed handling removed the unexpected alternate data stream.'
    Assert-WorkshopDownloadMarkerTest -Condition ($unexpectedStreamsAfter -ccontains 'Zone.Identifier') -Message 'Fail-closed preflight modified a package before rejecting its unexpected alternate data stream.'

    Write-Host 'Workshop download-marker tests: OK (Zone.Identifier stripped; unexpected ADS rejected before mutation)'
}
finally {
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixtureRoot)
    if (-not (Test-DtmApiPathIsSameOrChild -Child $resolvedFixture -Parent $resolvedTestRoot) -or
        [string]::Equals($resolvedFixture, $resolvedTestRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean an invalid Workshop download-marker fixture path: $resolvedFixture"
    }

    if (Test-Path -LiteralPath $resolvedFixture) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
