param(
    [string] $GameDir = '',
    [string] $OutputRoot = '',
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ($Configuration -ne 'Release') {
    throw 'The G2 Advanced fixture must use the frozen Release Author SDK compatibility contract.'
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo
}
else {
    $GameDir = [System.IO.Path]::GetFullPath($GameDir)
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source '-GameDir'
}

$managedAssembly = Join-Path $GameDir 'DolocTown_Data\Managed\Assembly-CSharp.dll'
$harmonyAssembly = Join-Path $GameDir 'BepInEx\core\0Harmony.dll'
foreach ($requiredFile in @($managedAssembly, $harmonyAssembly)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "G2 Advanced fixture requires the tracked local reference: $requiredFile"
    }
}

$defaultOutputRoot = Join-Path $repo 'temp\batch6-g2-advanced-fixture'
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = $defaultOutputRoot
}
else {
    $OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot, $repo)
}

$repoTemp = Join-Path $repo 'temp'
if (-not (Test-DtmApiPathIsSameOrChild -Child $OutputRoot -Parent $repoTemp)) {
    throw "G2 fixture output must remain under the repository temp root: $repoTemp"
}
$trimSeparators = [char[]]@('\', '/')
if ([System.IO.Path]::GetFullPath($OutputRoot).TrimEnd($trimSeparators) -eq [System.IO.Path]::GetFullPath($repoTemp).TrimEnd($trimSeparators)) {
    throw 'G2 fixture output cannot be the repository temp root itself.'
}
if (Test-Path -LiteralPath $OutputRoot) {
    Remove-Item -LiteralPath $OutputRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$fixtureRoot = Join-Path $repo 'tests\mod-fixtures\qa\AdvancedCodeMod'
$sdkReleaseRoot = Join-Path $OutputRoot 'author-sdk'
$sdkStageRoot = Join-Path $sdkReleaseRoot 'DTMAPI-Author-SDK-0.1.0-win-x64'
$sdkExecutable = Join-Path $sdkStageRoot 'dtmapi-author.exe'
$buildRoot = Join-Path $OutputRoot 'build'
$packagePath = Join-Path $OutputRoot 'DTMAPI.AdvancedFixture-g2.zip'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

& "$PSScriptRoot\build-author-sdk.ps1" -Configuration Release -OutputRoot $sdkReleaseRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to build and verify the frozen Author SDK release before the G2 fixture run.'
}
if (-not (Test-Path -LiteralPath $sdkExecutable -PathType Leaf)) {
    throw "Author SDK release did not contain the expected CLI: $sdkExecutable"
}

function Invoke-AuthorSdkJson {
    param(
        [Parameter(Mandatory = $true)] [string] $ReportName,
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    $output = @(& $sdkExecutable @Arguments --json 2>&1)
    $exitCode = $LASTEXITCODE
    $text = [string]::Join([Environment]::NewLine, @($output | ForEach-Object { [string]$_ }))
    $reportPath = Join-Path $OutputRoot ($ReportName + '.json')
    [System.IO.File]::WriteAllText($reportPath, $text + [Environment]::NewLine, $utf8NoBom)
    if ($exitCode -ne 0) {
        throw "Author SDK $ReportName failed with exit $exitCode. Report: $reportPath"
    }
    $report = $text | ConvertFrom-Json
    if (-not [bool]$report.success) {
        throw "Author SDK $ReportName returned success=false. Report: $reportPath"
    }
    return $report
}

$buildReport = Invoke-AuthorSdkJson -ReportName 'build-report' -Arguments @(
    'build', $fixtureRoot,
    '--game-root', $GameDir,
    '--output', $buildRoot
)
$packReport = Invoke-AuthorSdkJson -ReportName 'pack-report' -Arguments @(
    'pack', $fixtureRoot,
    '--game-root', $GameDir,
    '--output', $packagePath
)

if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "Author SDK did not create the expected G2 package: $packagePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName })
    $requiredEntries = @(
        'info.json',
        'Content/DTMAPI/manifest.json',
        'Content/DTMAPI/DTMAPI.AdvancedFixture.dll',
        'Content/DTMAPI/dtmapi-advanced-references.json',
        'Content/DTMAPI/dtmapi-package.json'
    )
    foreach ($entry in $requiredEntries) {
        if ($entries -notcontains $entry) {
            throw "G2 Advanced fixture package is missing $entry"
        }
    }

    $forbiddenNativeNames = @('Assembly-CSharp.dll', '0Harmony.dll', 'BepInEx.dll')
    $forbiddenEntries = @($entries | Where-Object {
        $name = [System.IO.Path]::GetFileName($_)
        $forbiddenNativeNames -contains $name -or $name -like 'UnityEngine*.dll'
    })
    if ($forbiddenEntries.Count -ne 0) {
        throw ('G2 Advanced fixture package bundled forbidden native dependencies: ' + [string]::Join(', ', $forbiddenEntries))
    }

    $entryDlls = @($entries | Where-Object { $_ -like 'Content/DTMAPI/*.dll' })
    if ($entryDlls.Count -ne 1 -or $entryDlls[0] -ne 'Content/DTMAPI/DTMAPI.AdvancedFixture.dll') {
        throw ('G2 Advanced fixture must contain exactly one managed entry DLL; found: ' + [string]::Join(', ', $entryDlls))
    }
}
finally {
    $archive.Dispose()
}

$packageHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
$summary = [ordered]@{
    SchemaVersion = 1
    Status = 'Passed'
    FixtureRoot = $fixtureRoot
    GameRoot = $GameDir
    GameBuildId = '23762374'
    BuildOutput = [string]$buildReport.outputPath
    BuildSha256 = [string]$buildReport.sha256
    PackagePath = $packagePath
    PackageSha256 = $packageHash
    ReceiptPath = 'Content/DTMAPI/dtmapi-advanced-references.json'
    HarmonyOwner = 'dtmapi.mod.dtmapi.advancedfixture'
    BundledNativeDependencies = @()
}
$summaryPath = Join-Path $OutputRoot 'summary.json'
[System.IO.File]::WriteAllText($summaryPath, (($summary | ConvertTo-Json -Depth 6) + [Environment]::NewLine), $utf8NoBom)

Write-Host "Batch 6 G2 Advanced fixture package: PASS"
Write-Host "Package: $packagePath"
Write-Host "SHA-256: $packageHash"
Write-Host "Summary: $summaryPath"
