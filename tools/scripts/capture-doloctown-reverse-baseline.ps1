[CmdletBinding()]
param(
    [string] $GameDir,
    [string] $BuildRoot,
    [string] $Branch,
    [switch] $AllowUnknownSteamBranch,
    [switch] $AllowPendingBranchSwitch,
    [string] $ExpectedSteamBuild,
    [string] $ExpectedSteamManifestSha256,
    [string] $ExpectedAssemblyCSharpSha256,
    [string] $AssetRipperVersion = '1.3.14',
    [string] $AssetRipperSha256 = '808CDDF66DD0357AD6B36B97DE3A2AEF5E3552E63AF3EE0610F9A03A0378101C',
    [switch] $InventoryOnly,
    [switch] $ReuseSnapshot,
    [switch] $ReuseExport,
    [switch] $Resume,
    [switch] $CodeOnly,
    [switch] $Status
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\reverse-baseline-path-safety.ps1"
. "$PSScriptRoot\steam-appmanifest-identity.ps1"
. "$PSScriptRoot\reverse-capture-state.ps1"

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

if ($Status) {
    if ([string]::IsNullOrWhiteSpace($BuildRoot)) { throw '-Status requires -BuildRoot; it does not discover the live game.' }
    Get-ReverseCaptureStatus -BuildRoot $BuildRoot | ConvertTo-Json -Depth 10
    return
}
$Resume = $Resume -or $ReuseSnapshot -or $ReuseExport
if ($InventoryOnly) { Write-Warning 'InventoryOnly is a full integrity check; use Status for a read-only receipt query.' }
$offline = $Resume -and -not [string]::IsNullOrWhiteSpace($BuildRoot) -and [string]::IsNullOrWhiteSpace($GameDir)
if ($offline) {
    $BuildRoot = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $BuildRoot
    $steamManifestPath = Join-Path $BuildRoot 'raw-snapshot/appmanifest_2285550.acf'
    if (-not (Test-Path -LiteralPath $steamManifestPath -PathType Leaf)) { throw 'Frozen manifest is incomplete. Resume with explicit GameDir for the same source identity to retry only snapshot.' }
    $identityRoot = Join-Path $BuildRoot 'raw-snapshot/game'
}
else {
    if ([string]::IsNullOrWhiteSpace($GameDir)) { $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo }
    else { Assert-DtmApiDolocTownGamePath -Path $GameDir -Source 'reverse baseline capture -GameDir' }
    $GameDir = (Resolve-Path -LiteralPath $GameDir).Path
    if (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue) { throw 'Close DolocTown.exe before freezing a reverse baseline.' }
    $steamManifestPath = Get-SteamManifestPath -ResolvedGameDir $GameDir
    $identityRoot = $GameDir
}
$steamIdentityParameters = @{ ManifestPath = $steamManifestPath; ExplicitBranch = $Branch }
if ($AllowUnknownSteamBranch) { $steamIdentityParameters.AllowUnknownSteamBranch = $true }
if ($AllowPendingBranchSwitch) { $steamIdentityParameters.AllowPendingBranchSwitch = $true }
$startSteamIdentity = Get-DtmApiSteamBuildIdentity @steamIdentityParameters
$steamBuild = $startSteamIdentity.BuildId
$resolvedBranch = $startSteamIdentity.Branch
if ($ExpectedSteamBuild -and $ExpectedSteamBuild -cne $steamBuild) { throw 'Steam build changed before capture started.' }
if ($ExpectedSteamManifestSha256 -and $ExpectedSteamManifestSha256 -ine $startSteamIdentity.ManifestSha256) { throw 'Steam manifest changed before capture started.' }
$assemblyPath = Join-Path $identityRoot 'DolocTown_Data/Managed/Assembly-CSharp.dll'
$assemblyHash = Get-Sha256Hex -Path $assemblyPath
$assemblyLength = (Get-Item -LiteralPath $assemblyPath).Length
if ($ExpectedAssemblyCSharpSha256 -and $ExpectedAssemblyCSharpSha256 -ine $assemblyHash) { throw 'Assembly-CSharp.dll changed before capture started.' }
$buildName = "${steamBuild}_${resolvedBranch}_$($assemblyHash.Substring(0, 6))"
if ([string]::IsNullOrWhiteSpace($BuildRoot)) { $BuildRoot = Join-Path $repo "references/doloc-town/reverse/builds/$buildName" }
$BuildRoot = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $BuildRoot -GameDir $GameDir
[IO.Directory]::CreateDirectory($BuildRoot) | Out-Null
$snapshotRoot = Join-Path $BuildRoot 'raw-snapshot'
$snapshotGameRoot = Join-Path $snapshotRoot 'game'
$snapshotManifestPath = Join-Path $snapshotRoot 'appmanifest_2285550.acf'
$exportRoot = Join-Path $BuildRoot 'asset-ripper-unity-project'
$inventoryRoot = Join-Path $BuildRoot 'full-baseline-inventory'
$assetRipperEvidenceRoot = Join-Path $BuildRoot 'asset-ripper'
$assetRipperLogPath = Join-Path $assetRipperEvidenceRoot "AssetRipper-$AssetRipperVersion.log"
$snapshotInputs = [ordered]@{
    format = 1; steamBuild = $steamBuild; branch = $resolvedBranch
    manifestSha256 = $startSteamIdentity.ManifestSha256; assemblySha256 = $assemblyHash
    files = $officialRootFiles; directories = $officialRootDirectories
}
$snapshotReceipt = $null
if (($Resume -or $InventoryOnly) -and $CodeOnly) {
    $candidateSnapshot = Get-ReverseStageReceipt $BuildRoot 'snapshot'
    if ((Get-ReverseValue $candidateSnapshot 'status') -eq 'complete' -and $candidateSnapshot.inputFingerprint -ceq (Get-ReverseDigest $snapshotInputs) -and $candidateSnapshot.outputFingerprint -ceq (Get-ReverseDigest @($candidateSnapshot.outputs))) {
        $codeInputs = @($candidateSnapshot.outputs | Where-Object { $_.path -eq 'appmanifest_2285550.acf' -or $_.path.StartsWith('game/DolocTown_Data/Managed/') })
        if (Test-ReverseInventory $snapshotRoot $codeInputs -AllowExtra) { $snapshotReceipt = $candidateSnapshot }
    }
}
elseif ($Resume -or $InventoryOnly) { $snapshotReceipt = Test-ReverseStage $BuildRoot 'snapshot' $snapshotInputs $snapshotRoot }
$legacyRawPath = Join-Path $inventoryRoot 'raw-snapshot-files.json'
$legacyParityPath = Join-Path $inventoryRoot 'snapshot-source-parity.json'
if (-not $snapshotReceipt -and ($Resume -or $InventoryOnly) -and -not (Get-ReverseStageReceipt $BuildRoot 'snapshot') -and (Test-Path -LiteralPath $legacyRawPath) -and (Test-Path -LiteralPath $legacyParityPath)) {
    $legacyInventory = @(Read-ReverseJsonItems $legacyRawPath)
    $legacyParity = Get-Content -Raw -LiteralPath $legacyParityPath | ConvertFrom-Json
    $legacyManifest = Get-ReverseValue $legacyParity 'steamManifest'
    if ($legacyParity.exact -and (Get-ReverseValue $legacyManifest 'sha256') -ieq $startSteamIdentity.ManifestSha256 -and (Test-ReverseInventory $snapshotGameRoot $legacyInventory)) {
        $snapshotOutputs = @($legacyInventory | ForEach-Object { [ordered]@{ path = 'game/' + $_.path; bytes = $_.bytes; sha256 = $_.sha256 } })
        $snapshotOutputs += @(Get-ChildItem -LiteralPath $snapshotRoot -File -Force | Sort-Object Name | ForEach-Object { [ordered]@{ path = $_.Name; bytes = $_.Length; sha256 = Get-Sha256Hex $_.FullName } })
        $snapshotReceipt = Complete-ReverseStage $BuildRoot 'snapshot' $snapshotInputs $snapshotRoot -Inventory $snapshotOutputs -Origin 'validated-legacy-inventory'
    }
}
if (-not $snapshotReceipt) {
    if ($offline -or $InventoryOnly) { throw 'Frozen snapshot is incomplete, damaged, or lacks proof. It was preserved; Resume with explicit GameDir for the same source identity to retry only snapshot.' }
    if (Test-Path -LiteralPath $snapshotRoot) {
        if (-not $Resume) { throw 'Snapshot exists. Use Resume to validate it; never overwrite frozen bytes with a different installation.' }
        $priorSnapshot = Get-ReverseStageReceipt $BuildRoot 'snapshot'
        if (-not $priorSnapshot -or $priorSnapshot.inputFingerprint -cne (Get-ReverseDigest $snapshotInputs)) { throw 'Snapshot retry requires an existing receipt with the exact same source identity. Use a new BuildRoot for a different source or unproven legacy capture.' }
    }
    Start-ReverseStage $BuildRoot 'snapshot' $snapshotInputs 'raw-snapshot' | Out-Null
    try {
        [IO.Directory]::CreateDirectory($snapshotGameRoot) | Out-Null
        foreach ($directoryName in $officialRootDirectories) { Copy-DirectoryWithRobocopy (Join-Path $GameDir $directoryName) (Join-Path $snapshotGameRoot $directoryName) }
        foreach ($fileName in $officialRootFiles) { Copy-Item -LiteralPath (Join-Path $GameDir $fileName) -Destination (Join-Path $snapshotGameRoot $fileName) }
        Copy-Item -LiteralPath $steamManifestPath -Destination $snapshotManifestPath
        Write-Utf8NoBomFile (Join-Path $snapshotRoot 'DO_NOT_DISTRIBUTE_OFFICIAL_GAME_FILES.txt') 'Official game files for local research only. Do not commit, publish, package, or redistribute.'
        $snapshotOutputs = @(Get-ReverseInventory $snapshotRoot)
        $snapshotInventory = @($snapshotOutputs | Where-Object { $_.path.StartsWith('game/') } | ForEach-Object { [ordered]@{ path = $_.path.Substring(5); bytes = $_.bytes; sha256 = $_.sha256 } })
        $mismatches = @($snapshotInventory | Where-Object {
            $source = Join-Path $GameDir $_.path
            -not (Test-Path -LiteralPath $source -PathType Leaf) -or (Get-Item -LiteralPath $source).Length -ne $_.bytes -or (Get-Sha256Hex $source) -cne $_.sha256
        })
        $frozenIdentity = Get-DtmApiSteamBuildIdentity -ManifestPath $snapshotManifestPath -ExplicitBranch $resolvedBranch -AllowUnknownSteamBranch:$AllowUnknownSteamBranch -AllowPendingBranchSwitch:$AllowPendingBranchSwitch
        $endIdentity = Get-DtmApiSteamBuildIdentity @steamIdentityParameters
        Assert-DtmApiSteamBuildIdentityMatch -Expected $startSteamIdentity -Actual $frozenIdentity -ExpectedLabel 'source-start' -ActualLabel 'frozen'
        Assert-DtmApiSteamBuildIdentityMatch -Expected $startSteamIdentity -Actual $endIdentity -ExpectedLabel 'source-start' -ActualLabel 'source-end'
        if ($mismatches.Count) { throw "Frozen/source payload mismatch: $($mismatches.Count) files." }
        $parity = [ordered]@{
            checkedAtUtc = [DateTime]::UtcNow.ToString('o'); sourceGameDir = $GameDir; snapshotGameRoot = $snapshotGameRoot
            checkedFiles = $snapshotInventory.Count + 1; gameFiles = $snapshotInventory.Count; exact = $true; mismatches = @()
            steamManifest = [ordered]@{ sourcePath = $steamManifestPath; snapshotPath = $snapshotManifestPath; buildId = $steamBuild; branch = $resolvedBranch; sha256 = $frozenIdentity.ManifestSha256; sourceStartSha256 = $startSteamIdentity.ManifestSha256; sourceEndSha256 = $endIdentity.ManifestSha256; exact = $true }
        }
        Write-ReverseJson (Join-Path $inventoryRoot 'snapshot-source-parity.json') $parity
        $snapshotReceipt = Complete-ReverseStage $BuildRoot 'snapshot' $snapshotInputs $snapshotRoot -Inventory $snapshotOutputs
    }
    catch { Fail-ReverseStage $BuildRoot 'snapshot' $_.Exception.Message; throw }
}
$snapshotInventory = @($snapshotReceipt.outputs | Where-Object { $_.path.StartsWith('game/') } | ForEach-Object { [ordered]@{ path = $_.path.Substring(5); bytes = $_.bytes; sha256 = $_.sha256 } })
Write-ReverseJson (Join-Path $inventoryRoot 'raw-snapshot-files.json') $snapshotInventory
if ($CodeOnly) { Write-Host "Frozen code inputs ready; resource export not requested: $BuildRoot"; return }
$exportInputs = [ordered]@{
    format = 1; snapshot = $snapshotReceipt.outputFingerprint
    tool = [ordered]@{ version = $AssetRipperVersion; archiveSha256 = $AssetRipperSha256.ToUpperInvariant() }
    parameters = @('LoadFolder', 'Export/UnityProject', 'default-settings')
}
$exportReceipt = $null
if ($Resume -or $InventoryOnly) { $exportReceipt = Test-ReverseStage $BuildRoot 'export' $exportInputs $exportRoot }
$legacyExportPath = Join-Path $inventoryRoot 'asset-ripper-export-files.json'
$legacyToolPath = Join-Path $assetRipperEvidenceRoot 'tool.json'
if (-not $exportReceipt -and ($Resume -or $InventoryOnly) -and -not (Get-ReverseStageReceipt $BuildRoot 'export') -and (Test-Path -LiteralPath $legacyExportPath) -and (Test-Path -LiteralPath $legacyToolPath)) {
    $legacyTool = Get-Content -Raw -LiteralPath $legacyToolPath | ConvertFrom-Json
    $legacyInventory = @(Read-ReverseJsonItems $legacyExportPath)
    if ($legacyTool.version -eq $AssetRipperVersion -and $legacyTool.archiveSha256 -ieq $AssetRipperSha256 -and (Test-ReverseInventory $exportRoot $legacyInventory)) {
        $exportReceipt = Complete-ReverseStage $BuildRoot 'export' $exportInputs $exportRoot -Inventory $legacyInventory -Origin 'validated-legacy-inventory'
    }
}
if (-not $exportReceipt) {
    if ($InventoryOnly) { throw 'Export integrity/tool identity failed; InventoryOnly does not re-export.' }
    if ((Test-Path -LiteralPath $exportRoot) -and -not $Resume) { throw 'Export exists. Use Resume to validate or retry only that stage.' }
    Start-ReverseStage $BuildRoot 'export' $exportInputs 'asset-ripper-unity-project' | Out-Null
    try {
        [IO.Directory]::CreateDirectory($assetRipperEvidenceRoot) | Out-Null
        $tool = Ensure-AssetRipper $AssetRipperVersion $AssetRipperSha256
        Invoke-AssetRipperExport $tool $snapshotGameRoot $exportRoot $assetRipperLogPath
        if (-not (Test-Path -LiteralPath (Join-Path $exportRoot 'ExportedProject/ProjectSettings/ProjectVersion.txt'))) { throw 'AssetRipper did not produce a complete project marker.' }
        Write-ReverseJson (Join-Path $assetRipperEvidenceRoot 'tool.json') $tool
        $exportReceipt = Complete-ReverseStage $BuildRoot 'export' $exportInputs $exportRoot
    }
    catch { Fail-ReverseStage $BuildRoot 'export' $_.Exception.Message; throw }
}
$exportInventory = @($exportReceipt.outputs)
Write-ReverseJson (Join-Path $inventoryRoot 'asset-ripper-export-files.json') $exportInventory
$parity = Get-Content -Raw -LiteralPath (Join-Path $inventoryRoot 'snapshot-source-parity.json') | ConvertFrom-Json
$inventoryInputs = [ordered]@{ format = 2; snapshot = $snapshotReceipt.outputFingerprint; export = $exportReceipt.outputFingerprint; generatorSha256 = Get-Sha256Hex $PSCommandPath }
if (($Resume -or $InventoryOnly) -and (Test-ReverseStage $BuildRoot 'inventory' $inventoryInputs $inventoryRoot)) { Write-Host "Capture stages verified and reused: $BuildRoot"; return }
# Reuse the inventories just verified by their stages; do not hash the same
# scenes, bundles and managed files again while formatting derived summaries.
$knownCaptureHashes = @{}
foreach ($entry in $snapshotInventory) { $knownCaptureHashes[[IO.Path]::GetFullPath((Join-Path $snapshotGameRoot $entry.path))] = $entry.sha256 }
foreach ($entry in $exportInventory) { $knownCaptureHashes[[IO.Path]::GetFullPath((Join-Path $exportRoot $entry.path))] = $entry.sha256 }
function Get-KnownCaptureHash {
    param([string] $Path)
    $full = [IO.Path]::GetFullPath($Path)
    if ($knownCaptureHashes.ContainsKey($full)) { return $knownCaptureHashes[$full] }
    return Get-Sha256Hex $full
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
            sha256 = Get-KnownCaptureHash -Path $_.FullName
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
        sha256 = if ($levelFile) { Get-KnownCaptureHash -Path $levelFile.FullName } else { $null }
    }
}
Write-ReverseJson (Join-Path $inventoryRoot 'built-scenes.json') @($builtScenes)

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
            sha256 = Get-KnownCaptureHash -Path $_.FullName
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
            sha256 = Get-KnownCaptureHash -Path $_.FullName
        }
    })
}

$managedRoot = Join-Path $snapshotGameRoot 'DolocTown_Data\Managed'
$assemblies = @(Get-ChildItem -LiteralPath $managedRoot -Filter '*.dll' -File | Sort-Object Name | ForEach-Object {
    [ordered]@{
        name = $_.Name
        bytes = [long]$_.Length
        sha256 = Get-KnownCaptureHash -Path $_.FullName
    }
})

$catalog = $null
if (Test-Path -LiteralPath $catalogPath -PathType Leaf) {
    $catalogJson = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $catalog = [ordered]@{
        bytes = (Get-Item -LiteralPath $catalogPath).Length
        sha256 = Get-KnownCaptureHash -Path $catalogPath
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
        userBranch = $startSteamIdentity.UserBranch
        mountedBranch = $startSteamIdentity.MountedBranch
        pendingSwitch = [bool]$startSteamIdentity.PendingBranchSwitch
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
        knownLimitation = 'AssetRipper may report per-scene Cubemap size mismatches plus three custom-version globalgamemanagers setting-object read errors; raw bytes remain preserved and the exact error count is recorded.'
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

$inventoryOutputs = @('raw-snapshot-files.json', 'asset-ripper-export-files.json', 'snapshot-source-parity.json', 'built-scenes.json', 'summary.json') | ForEach-Object {
    $file = Get-Item -LiteralPath (Join-Path $inventoryRoot $_)
    [ordered]@{ path = $_; bytes = $file.Length; sha256 = Get-KnownCaptureHash $file.FullName }
}
Complete-ReverseStage $BuildRoot 'inventory' $inventoryInputs $inventoryRoot -Inventory @($inventoryOutputs) -AllowExtra | Out-Null
