[CmdletBinding()]
param(
    [string] $GameDir,
    [string] $BuildRoot,
    [string] $Branch,
    [switch] $AllowUnknownSteamBranch,
    [string] $ExpectedSteamBuild,
    [string] $ExpectedSteamManifestSha256,
    [string] $ExpectedAssemblyCSharpSha256,
    [string] $AssetRipperVersion = '1.3.14',
    [string] $AssetRipperSha256 = '808CDDF66DD0357AD6B36B97DE3A2AEF5E3552E63AF3EE0610F9A03A0378101C',
    [switch] $InventoryOnly,
    [switch] $ReuseSnapshot,
    [switch] $ReuseExport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\reverse-baseline-path-safety.ps1"
. "$PSScriptRoot\steam-appmanifest-identity.ps1"

$repo = Get-RepoRoot
$officialRootFiles = @(
    'DolocTown.exe',
    'UnityPlayer.dll',
    'UnityCrashHandler64.exe'
)
$officialRootDirectories = @(
    'DolocTown_Data',
    'MonoBleedingEdge'
)

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [string] $Text
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-NormalizedRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $rootPath = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $pathValue = [System.IO.Path]::GetFullPath($Path)
    $rootUri = [System.Uri]::new($rootPath)
    $pathUri = [System.Uri]::new($pathValue)
    return [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString()).Replace('\', '/')
}

function Get-Sha256Hex {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Get-FileInventory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,
        [string] $ProgressLabel = 'Hashing files'
    )

    $resolvedRoot = (Resolve-Path -LiteralPath $Root).Path
    $files = @(Get-ChildItem -LiteralPath $resolvedRoot -File -Recurse -Force | Sort-Object FullName)
    $result = [System.Collections.Generic.List[object]]::new()

    for ($index = 0; $index -lt $files.Count; $index++) {
        $file = $files[$index]
        if (($index % 500) -eq 0 -or $index -eq ($files.Count - 1)) {
            $percent = if ($files.Count -eq 0) { 100 } else { [int](($index + 1) * 100 / $files.Count) }
            Write-Progress -Activity $ProgressLabel -Status "$($index + 1) / $($files.Count)" -PercentComplete $percent
        }

        $result.Add([ordered]@{
            path = ConvertTo-NormalizedRelativePath -Root $resolvedRoot -Path $file.FullName
            bytes = [long]$file.Length
            sha256 = Get-Sha256Hex -Path $file.FullName
        })
    }

    Write-Progress -Activity $ProgressLabel -Completed
    return @($result)
}

function Get-InventoryByteSum {
    param(
        [Parameter(Mandatory = $true)]
        [object[]] $Entries
    )

    [long]$sum = 0
    foreach ($entry in $Entries) {
        if ($entry -is [System.Collections.IDictionary]) {
            $sum += [long]$entry['bytes']
        }
        else {
            $sum += [long]$entry.bytes
        }
    }
    return $sum
}

function Get-SteamManifestPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ResolvedGameDir
    )

    $commonDir = Split-Path -Parent $ResolvedGameDir
    $steamAppsDir = Split-Path -Parent $commonDir
    $manifestPath = Join-Path $steamAppsDir 'appmanifest_2285550.acf'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Steam manifest not found next to the resolved game folder: $manifestPath"
    }

    return (Resolve-Path -LiteralPath $manifestPath).Path
}

function Copy-DirectoryWithRobocopy {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Source,
        [Parameter(Mandatory = $true)]
        [string] $Destination
    )

    [System.IO.Directory]::CreateDirectory($Destination) | Out-Null
    & robocopy.exe $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /SL /XJ /NP /NFL /NDL
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        throw "robocopy failed for '$Source' with exit code $exitCode."
    }
}

function Get-FreeTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Wait-HttpEndpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Uri,
        [int] $TimeoutSeconds = 30
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Timed out waiting for AssetRipper at $Uri"
}

function Invoke-AssetRipperPost {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Uri,
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [int] $TimeoutSeconds = 3600
    )

    try {
        $response = Invoke-WebRequest -Uri $Uri -Method Post -Body @{ path = $Path } `
            -ContentType 'application/x-www-form-urlencoded' -MaximumRedirection 0 `
            -UseBasicParsing -TimeoutSec $TimeoutSeconds
        if ($response.StatusCode -ne 302 -and $response.StatusCode -ne 200) {
            throw "Unexpected AssetRipper HTTP status $($response.StatusCode) from $Uri"
        }
    }
    catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 302) {
            return
        }

        throw
    }
}

function Ensure-AssetRipper {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Version,
        [Parameter(Mandatory = $true)]
        [string] $ExpectedSha256
    )

    $toolRoot = Join-Path $repo ".tools\assetripper\$Version"
    $zipPath = Join-Path $toolRoot 'AssetRipper_win_x64.zip'
    $appRoot = Join-Path $toolRoot 'app'
    $exePath = Join-Path $appRoot 'AssetRipper.GUI.Free.exe'
    $downloadUri = "https://github.com/AssetRipper/AssetRipper/releases/download/$Version/AssetRipper_win_x64.zip"

    [System.IO.Directory]::CreateDirectory($toolRoot) | Out-Null
    if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
        Write-Host "Downloading AssetRipper $Version from its official GitHub release..."
        Invoke-WebRequest -Uri $downloadUri -OutFile $zipPath -UseBasicParsing
    }

    $actualHash = Get-Sha256Hex -Path $zipPath
    if ($actualHash -cne $ExpectedSha256.ToUpperInvariant()) {
        throw "AssetRipper archive hash mismatch. Expected $ExpectedSha256, found $actualHash."
    }

    if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
        if (Test-Path -LiteralPath $appRoot) {
            throw "AssetRipper app folder exists but the executable is missing: $appRoot"
        }
        Expand-Archive -LiteralPath $zipPath -DestinationPath $appRoot
    }

    return [ordered]@{
        version = $Version
        archiveSha256 = $actualHash
        downloadUri = $downloadUri
        executable = $exePath
    }
}

function Invoke-AssetRipperExport {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary] $Tool,
        [Parameter(Mandatory = $true)]
        [string] $SnapshotGameRoot,
        [Parameter(Mandatory = $true)]
        [string] $OutputRoot,
        [Parameter(Mandatory = $true)]
        [string] $LogPath
    )

    $port = Get-FreeTcpPort
    $baseUri = "http://127.0.0.1:$port"
    $process = $null
    try {
        $process = Start-Process -FilePath $Tool.executable `
            -ArgumentList @('--headless', '--port', [string]$port, '--log-path', ('"' + $LogPath + '"')) `
            -WindowStyle Hidden -PassThru
        Wait-HttpEndpoint -Uri "$baseUri/" -TimeoutSeconds 30
        Write-Host 'Loading the frozen game snapshot into AssetRipper...'
        Invoke-AssetRipperPost -Uri "$baseUri/LoadFolder" -Path $SnapshotGameRoot -TimeoutSeconds 1800
        Write-Host 'Exporting the recovered Unity project...'
        Invoke-AssetRipperPost -Uri "$baseUri/Export/UnityProject" -Path $OutputRoot -TimeoutSeconds 7200
    }
    finally {
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            $process.WaitForExit(5000) | Out-Null
        }
    }
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo
}
else {
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source 'reverse baseline capture -GameDir'
}
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path

if (-not $InventoryOnly -and (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
    throw 'DolocTown.exe is running. Close the game before freezing a reverse baseline.'
}

$steamManifestPath = Get-SteamManifestPath -ResolvedGameDir $GameDir
$steamIdentityParameters = @{
    ManifestPath = $steamManifestPath
    ExplicitBranch = $Branch
}
if ($AllowUnknownSteamBranch) {
    $steamIdentityParameters['AllowUnknownSteamBranch'] = $true
}
$startSteamIdentity = Get-DtmApiSteamBuildIdentity @steamIdentityParameters
$steamBuild = $startSteamIdentity.BuildId
$resolvedBranch = $startSteamIdentity.Branch
if (-not [string]::IsNullOrWhiteSpace($ExpectedSteamBuild) -and
    -not $ExpectedSteamBuild.Equals($steamBuild, [System.StringComparison]::Ordinal)) {
    throw "Steam build changed before capture started. Expected=$ExpectedSteamBuild Actual=$steamBuild"
}
if (-not [string]::IsNullOrWhiteSpace($ExpectedSteamManifestSha256) -and
    -not $ExpectedSteamManifestSha256.Equals($startSteamIdentity.ManifestSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Steam manifest changed before capture started. Expected=$ExpectedSteamManifestSha256 Actual=$($startSteamIdentity.ManifestSha256)"
}
$assemblyPath = Join-Path $GameDir 'DolocTown_Data\Managed\Assembly-CSharp.dll'
$assemblyHash = Get-Sha256Hex -Path $assemblyPath
$assemblyLength = (Get-Item -LiteralPath $assemblyPath).Length
if (-not [string]::IsNullOrWhiteSpace($ExpectedAssemblyCSharpSha256) -and
    -not $ExpectedAssemblyCSharpSha256.Equals($assemblyHash, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Assembly-CSharp.dll changed before capture started. Expected=$ExpectedAssemblyCSharpSha256 Actual=$assemblyHash"
}
$buildName = "${steamBuild}_${resolvedBranch}_$($assemblyHash.Substring(0, 6))"

if ([string]::IsNullOrWhiteSpace($BuildRoot)) {
    $BuildRoot = Join-Path $repo "references\doloc-town\reverse\builds\$buildName"
}
$BuildRoot = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $BuildRoot -GameDir $GameDir
[System.IO.Directory]::CreateDirectory($BuildRoot) | Out-Null

$snapshotRoot = Join-Path $BuildRoot 'raw-snapshot'
$snapshotGameRoot = Join-Path $snapshotRoot 'game'
$snapshotManifestPath = Join-Path $snapshotRoot 'appmanifest_2285550.acf'
$exportRoot = Join-Path $BuildRoot 'asset-ripper-unity-project'
$inventoryRoot = Join-Path $BuildRoot 'full-baseline-inventory'
$assetRipperEvidenceRoot = Join-Path $BuildRoot 'asset-ripper'
$assetRipperLogPath = Join-Path $assetRipperEvidenceRoot "AssetRipper-$AssetRipperVersion.log"

if (-not $InventoryOnly) {
    if (Test-Path -LiteralPath $snapshotGameRoot) {
        if (-not $ReuseSnapshot) {
            throw "Snapshot already exists. Refusing to overwrite frozen bytes without -ReuseSnapshot: $snapshotGameRoot"
        }
    }
    else {
        [System.IO.Directory]::CreateDirectory($snapshotGameRoot) | Out-Null
        foreach ($directoryName in $officialRootDirectories) {
            Copy-DirectoryWithRobocopy -Source (Join-Path $GameDir $directoryName) -Destination (Join-Path $snapshotGameRoot $directoryName)
        }
        foreach ($fileName in $officialRootFiles) {
            Copy-Item -LiteralPath (Join-Path $GameDir $fileName) -Destination (Join-Path $snapshotGameRoot $fileName)
        }
        Copy-Item -LiteralPath $steamManifestPath -Destination $snapshotManifestPath
        Write-Utf8NoBomFile -Path (Join-Path $snapshotRoot 'DO_NOT_DISTRIBUTE_OFFICIAL_GAME_FILES.txt') -Text @'
This directory contains official Doloc Town game files preserved for local reverse-engineering research.
Do not commit, publish, package, or redistribute these files.
'@
    }

    if (-not (Test-Path -LiteralPath $snapshotManifestPath -PathType Leaf)) {
        throw "Frozen Steam manifest is missing: $snapshotManifestPath"
    }
    $snapshotSteamIdentityParameters = @{
        ManifestPath = $snapshotManifestPath
        ExplicitBranch = $resolvedBranch
    }
    if ($AllowUnknownSteamBranch) {
        $snapshotSteamIdentityParameters['AllowUnknownSteamBranch'] = $true
    }
    $snapshotSteamIdentity = Get-DtmApiSteamBuildIdentity @snapshotSteamIdentityParameters
    Assert-DtmApiSteamBuildIdentityMatch `
        -Expected $startSteamIdentity `
        -Actual $snapshotSteamIdentity `
        -ExpectedLabel 'source-start' `
        -ActualLabel 'frozen'

    $snapshotAssemblyHash = Get-Sha256Hex -Path (Join-Path $snapshotGameRoot 'DolocTown_Data\Managed\Assembly-CSharp.dll')
    if ($snapshotAssemblyHash -cne $assemblyHash) {
        throw "Frozen Assembly-CSharp.dll does not match the source game. Source=$assemblyHash Snapshot=$snapshotAssemblyHash"
    }

    if (Test-Path -LiteralPath $exportRoot) {
        if (-not $ReuseExport) {
            throw "AssetRipper export already exists. Refusing to overwrite it without -ReuseExport: $exportRoot"
        }
    }
    else {
        [System.IO.Directory]::CreateDirectory($assetRipperEvidenceRoot) | Out-Null
        $tool = Ensure-AssetRipper -Version $AssetRipperVersion -ExpectedSha256 $AssetRipperSha256
        Invoke-AssetRipperExport -Tool $tool -SnapshotGameRoot $snapshotGameRoot -OutputRoot $exportRoot -LogPath $assetRipperLogPath
        Write-Utf8NoBomFile -Path (Join-Path $assetRipperEvidenceRoot 'tool.json') -Text (($tool | ConvertTo-Json -Depth 5) + [Environment]::NewLine)
    }
}

if (-not (Test-Path -LiteralPath $snapshotGameRoot -PathType Container)) {
    throw "Frozen snapshot is missing: $snapshotGameRoot"
}
if (-not (Test-Path -LiteralPath $snapshotManifestPath -PathType Leaf)) {
    throw "Frozen Steam manifest is missing: $snapshotManifestPath"
}
if (-not (Test-Path -LiteralPath $exportRoot -PathType Container)) {
    throw "AssetRipper Unity project export is missing: $exportRoot"
}

[System.IO.Directory]::CreateDirectory($inventoryRoot) | Out-Null
$snapshotInventory = @(Get-FileInventory -Root $snapshotGameRoot -ProgressLabel 'Hashing frozen game snapshot')
$exportInventory = @(Get-FileInventory -Root $exportRoot -ProgressLabel 'Hashing AssetRipper export')
Write-Utf8NoBomFile -Path (Join-Path $inventoryRoot 'raw-snapshot-files.json') -Text (($snapshotInventory | ConvertTo-Json -Depth 5) + [Environment]::NewLine)
Write-Utf8NoBomFile -Path (Join-Path $inventoryRoot 'asset-ripper-export-files.json') -Text (($exportInventory | ConvertTo-Json -Depth 5) + [Environment]::NewLine)

$snapshotMismatches = [System.Collections.Generic.List[object]]::new()
foreach ($entry in $snapshotInventory) {
    $sourcePath = Join-Path $GameDir ($entry.path.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        $snapshotMismatches.Add([ordered]@{ path = $entry.path; reason = 'missing-source' })
        continue
    }
    $sourceItem = Get-Item -LiteralPath $sourcePath
    if ([long]$sourceItem.Length -ne [long]$entry.bytes) {
        $snapshotMismatches.Add([ordered]@{ path = $entry.path; reason = 'length'; sourceBytes = [long]$sourceItem.Length; snapshotBytes = [long]$entry.bytes })
        continue
    }
    $sourceHash = Get-Sha256Hex -Path $sourcePath
    if ($sourceHash -cne [string]$entry.sha256) {
        $snapshotMismatches.Add([ordered]@{ path = $entry.path; reason = 'sha256'; sourceSha256 = $sourceHash; snapshotSha256 = $entry.sha256 })
    }
}

$snapshotSteamIdentityParameters = @{
    ManifestPath = $snapshotManifestPath
    ExplicitBranch = $resolvedBranch
}
$endSteamIdentityParameters = @{
    ManifestPath = $steamManifestPath
    ExplicitBranch = $resolvedBranch
}
if ($AllowUnknownSteamBranch) {
    $snapshotSteamIdentityParameters['AllowUnknownSteamBranch'] = $true
    $endSteamIdentityParameters['AllowUnknownSteamBranch'] = $true
}
$snapshotSteamIdentity = Get-DtmApiSteamBuildIdentity @snapshotSteamIdentityParameters
$endSteamIdentity = Get-DtmApiSteamBuildIdentity @endSteamIdentityParameters
Assert-DtmApiSteamBuildIdentityMatch `
    -Expected $startSteamIdentity `
    -Actual $snapshotSteamIdentity `
    -ExpectedLabel 'source-start' `
    -ActualLabel 'frozen'
Assert-DtmApiSteamBuildIdentityMatch `
    -Expected $startSteamIdentity `
    -Actual $endSteamIdentity `
    -ExpectedLabel 'source-start' `
    -ActualLabel 'source-end'

$parity = [ordered]@{
    checkedAtUtc = [DateTime]::UtcNow.ToString('o')
    sourceGameDir = $GameDir
    snapshotGameRoot = $snapshotGameRoot
    checkedFiles = $snapshotInventory.Count + 1
    gameFiles = $snapshotInventory.Count
    steamManifest = [ordered]@{
        sourcePath = $steamManifestPath
        snapshotPath = $snapshotManifestPath
        buildId = $snapshotSteamIdentity.BuildId
        branch = $snapshotSteamIdentity.Branch
        sha256 = $snapshotSteamIdentity.ManifestSha256
        sourceStartSha256 = $startSteamIdentity.ManifestSha256
        sourceEndSha256 = $endSteamIdentity.ManifestSha256
        exact = $true
    }
    exact = ($snapshotMismatches.Count -eq 0)
    mismatches = @($snapshotMismatches)
}
Write-Utf8NoBomFile -Path (Join-Path $inventoryRoot 'snapshot-source-parity.json') -Text (($parity | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
if (-not $parity.exact) {
    throw "Frozen snapshot no longer matches the source game in $($snapshotMismatches.Count) file(s). See snapshot-source-parity.json."
}

$projectRoot = Join-Path $exportRoot 'ExportedProject'
$sceneRoot = Join-Path $projectRoot 'Assets\Scenes'
$configRoot = Join-Path $projectRoot 'Assets\Configs\GenDatas'
$streamingRoot = Join-Path $projectRoot 'Assets\StreamingAssets'
$catalogPath = Join-Path $streamingRoot 'aa\catalog.json'
$projectVersionPath = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'

$scenes = @()
if (Test-Path -LiteralPath $sceneRoot -PathType Container) {
    $scenes = @(Get-ChildItem -LiteralPath $sceneRoot -Filter '*.unity' -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path = ConvertTo-NormalizedRelativePath -Root $projectRoot -Path $_.FullName
            bytes = [long]$_.Length
            sha256 = Get-Sha256Hex -Path $_.FullName
        }
    })
}

$builtScenePaths = @()
$globalGameManagersPath = Join-Path $snapshotGameRoot 'DolocTown_Data\globalgamemanagers'
if (Test-Path -LiteralPath $globalGameManagersPath -PathType Leaf) {
    $globalGameManagersText = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($globalGameManagersPath))
    $builtScenePaths = @([System.Text.RegularExpressions.Regex]::Matches(
        $globalGameManagersText,
        'Assets/Scenes/[^\x00-\x1F]{1,240}?\.unity'
    ) | ForEach-Object { $_.Value })
}

$builtScenes = @()
for ($sceneIndex = 0; $sceneIndex -lt $builtScenePaths.Count; $sceneIndex++) {
    $levelPath = Join-Path $sceneRoot "level$sceneIndex.unity"
    $levelFile = if (Test-Path -LiteralPath $levelPath -PathType Leaf) { Get-Item -LiteralPath $levelPath } else { $null }
    $builtScenes += [ordered]@{
        buildIndex = $sceneIndex
        originalPath = $builtScenePaths[$sceneIndex]
        exportedPath = if ($levelFile) { ConvertTo-NormalizedRelativePath -Root $projectRoot -Path $levelFile.FullName } else { $null }
        bytes = if ($levelFile) { [long]$levelFile.Length } else { $null }
        sha256 = if ($levelFile) { Get-Sha256Hex -Path $levelFile.FullName } else { $null }
    }
}
Write-Utf8NoBomFile -Path (Join-Path $inventoryRoot 'built-scenes.json') -Text (($builtScenes | ConvertTo-Json -Depth 6) + [Environment]::NewLine)

$configTables = @()
if (Test-Path -LiteralPath $configRoot -PathType Container) {
    $configTables = @(Get-ChildItem -LiteralPath $configRoot -Filter '*.json' -File | Sort-Object Name | ForEach-Object {
        $rowCount = $null
        $parseError = $null
        try {
            $value = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
            $rowCount = @($value).Count
        }
        catch {
            $parseError = $_.Exception.Message
        }
        [ordered]@{
            name = $_.Name
            rows = $rowCount
            bytes = [long]$_.Length
            sha256 = Get-Sha256Hex -Path $_.FullName
            parseError = $parseError
        }
    })
}

$bundles = @()
if (Test-Path -LiteralPath $streamingRoot -PathType Container) {
    $bundles = @(Get-ChildItem -LiteralPath $streamingRoot -Filter '*.bundle' -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path = ConvertTo-NormalizedRelativePath -Root $streamingRoot -Path $_.FullName
            bytes = [long]$_.Length
            sha256 = Get-Sha256Hex -Path $_.FullName
        }
    })
}

$managedRoot = Join-Path $snapshotGameRoot 'DolocTown_Data\Managed'
$assemblies = @(Get-ChildItem -LiteralPath $managedRoot -Filter '*.dll' -File | Sort-Object Name | ForEach-Object {
    [ordered]@{
        name = $_.Name
        bytes = [long]$_.Length
        sha256 = Get-Sha256Hex -Path $_.FullName
    }
})

$catalog = $null
if (Test-Path -LiteralPath $catalogPath -PathType Leaf) {
    $catalogJson = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $catalog = [ordered]@{
        bytes = (Get-Item -LiteralPath $catalogPath).Length
        sha256 = Get-Sha256Hex -Path $catalogPath
        internalIds = @($catalogJson.m_InternalIds)
        providerIds = @($catalogJson.m_ProviderIds)
        resourceTypes = @($catalogJson.m_resourceTypes)
    }
}

$assetRipperErrors = @()
if (Test-Path -LiteralPath $assetRipperLogPath -PathType Leaf) {
    $assetRipperErrors = @(Get-Content -LiteralPath $assetRipperLogPath | Where-Object { $_ -match '\[Error\]' })
}

$extensionCounts = @($exportInventory | Group-Object { [System.IO.Path]::GetExtension([string]$_.path).ToLowerInvariant() } | Sort-Object Count -Descending | ForEach-Object {
    [ordered]@{ extension = $_.Name; count = $_.Count }
})

$unityVersion = $null
if (Test-Path -LiteralPath $projectVersionPath -PathType Leaf) {
    $versionMatch = [System.Text.RegularExpressions.Regex]::Match([System.IO.File]::ReadAllText($projectVersionPath), 'm_EditorVersion:\s*(?<version>\S+)')
    if ($versionMatch.Success) {
        $unityVersion = $versionMatch.Groups['version'].Value
    }
}

$summary = [ordered]@{
    format = 'dtmapi.doloctown-full-reverse-baseline/v1'
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    steamAppId = 2285550
    steamBuild = $steamBuild
    branch = $resolvedBranch
    branchIdentity = [ordered]@{
        detected = $startSteamIdentity.Branch
        source = $startSteamIdentity.BranchSource
        known = [bool]$startSteamIdentity.IsKnownBranch
        manifestSha256 = $startSteamIdentity.ManifestSha256
    }
    buildName = $buildName
    unityVersion = $unityVersion
    assemblyCSharp = [ordered]@{
        bytes = [long]$assemblyLength
        sha256 = $assemblyHash
    }
    snapshot = [ordered]@{
        files = $snapshotInventory.Count
        bytes = Get-InventoryByteSum -Entries $snapshotInventory
        sourceParityExact = [bool]$parity.exact
    }
    assetRipper = [ordered]@{
        version = $AssetRipperVersion
        archiveSha256 = $AssetRipperSha256
        exportedFiles = $exportInventory.Count
        exportedBytes = Get-InventoryByteSum -Entries $exportInventory
        errorLines = $assetRipperErrors.Count
        knownLimitation = 'The custom Unity version globalgamemanagers produced three setting-object read errors; raw bytes remain preserved.'
    }
    recovered = [ordered]@{
        scenes = $scenes
        sceneCount = $scenes.Count
        builtScenes = $builtScenes
        builtScenePathCount = $builtScenePaths.Count
        builtSceneMappingExact = ($builtScenePaths.Count -eq $scenes.Count -and @($builtScenes | Where-Object { -not $_.exportedPath }).Count -eq 0)
        configTables = $configTables
        configTableCount = $configTables.Count
        bundles = $bundles
        bundleCount = $bundles.Count
        managedAssemblies = $assemblies
        managedAssemblyCount = $assemblies.Count
        catalog = $catalog
        extensionCounts = $extensionCounts
    }
    sourceBoundaries = @(
        'Official binaries, decompiled source, and extracted assets are local research material only.',
        'Do not commit, publish, package, or redistribute the raw snapshot or AssetRipper export.'
    )
}
Write-Utf8NoBomFile -Path (Join-Path $inventoryRoot 'summary.json') -Text (($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine)

Write-Host "Full reverse baseline inventory completed: $BuildRoot"
Write-Host "Steam build: $steamBuild"
Write-Host "Snapshot: $($summary.snapshot.files) files / $($summary.snapshot.bytes) bytes / parity exact=$($summary.snapshot.sourceParityExact)"
Write-Host "AssetRipper export: $($summary.assetRipper.exportedFiles) files / $($summary.assetRipper.exportedBytes) bytes"
Write-Host "Recovered: scenes=$($summary.recovered.sceneCount), named build scenes=$($summary.recovered.builtScenePathCount), config tables=$($summary.recovered.configTableCount), bundles=$($summary.recovered.bundleCount), managed assemblies=$($summary.recovered.managedAssemblyCount)"
Write-Host "AssetRipper error lines: $($summary.assetRipper.errorLines)"
