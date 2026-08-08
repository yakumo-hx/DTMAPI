[CmdletBinding()]
param(
    [string] $GameDir,
    [string] $BuildRoot,
    [string] $Branch,
    [switch] $AllowUnknownSteamBranch,
    [switch] $AllowPendingBranchSwitch,
    [string] $AssetRipperVersion = '1.3.14',
    [string] $AssetRipperSha256 = '808CDDF66DD0357AD6B36B97DE3A2AEF5E3552E63AF3EE0610F9A03A0378101C',
    [switch] $InventoryOnly,
    [switch] $ReuseSnapshot,
    [switch] $ReuseExport,
    [switch] $ReuseDecompile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\reverse-baseline-path-safety.ps1"
$steamIdentityHelper = Join-Path $PSScriptRoot 'steam-appmanifest-identity.ps1'
if (-not (Test-Path -LiteralPath $steamIdentityHelper -PathType Leaf)) {
    $steamIdentityHelper = Join-Path $PSScriptRoot '..\scripts\steam-appmanifest-identity.ps1'
}
. $steamIdentityHelper
$sourceWorkspaceMode = $false
$captureScript = Join-Path $PSScriptRoot 'capture-doloctown-reverse-baseline.ps1'
if (-not (Test-Path -LiteralPath $captureScript -PathType Leaf)) {
    $captureScript = Join-Path $PSScriptRoot '..\scripts\capture-doloctown-reverse-baseline.ps1'
    $sourceWorkspaceMode = $true
}
if (-not (Test-Path -LiteralPath $captureScript -PathType Leaf)) {
    throw "Reverse baseline capture script is missing: $captureScript"
}

$packageRoot = if ($sourceWorkspaceMode) {
    (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
}
else {
    Get-RepoRoot
}
[System.Net.ServicePointManager]::SecurityProtocol =
    [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12
$dotNetRuntimeVersion = '8.0.27'
$dotNetRuntimeUri = 'https://builds.dotnet.microsoft.com/dotnet/Runtime/8.0.27/dotnet-runtime-8.0.27-win-x64.zip'
$dotNetRuntimeSha512 = 'E31528B5452AFDEC4CDF78FB073E8693ED0C24A14F5B69065E7018F89EEFC38E52FCFD4EED8255B5D58867AA850F66790FEAD5B66F0FAB13A72BEBF098E98937'
$ilSpyVersion = '9.1.0.7988'
$ilSpyPackageUri = 'https://api.nuget.org/v3-flatcontainer/ilspycmd/9.1.0.7988/ilspycmd.9.1.0.7988.nupkg'
$ilSpyPackageSha256 = '2B5058F5CCC164C33B7AABF1A5EB0CF3D3A6AF6C145AAF58EFD3ED891443AF7C'

function Write-PortableUtf8NoBomFile {
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

function Get-PortableFileHashHex {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [ValidateSet('SHA256', 'SHA512')]
        [string] $Algorithm = 'SHA256'
    )

    return (Get-FileHash -LiteralPath $Path -Algorithm $Algorithm).Hash.ToUpperInvariant()
}

function Get-PortableSteamManifestPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ResolvedGameDir
    )

    $commonDir = Split-Path -Parent $ResolvedGameDir
    $steamAppsDir = Split-Path -Parent $commonDir
    $manifestPath = Join-Path $steamAppsDir 'appmanifest_2285550.acf'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Steam manifest not found next to the game folder: $manifestPath"
    }
    return (Resolve-Path -LiteralPath $manifestPath).Path
}

function Assert-PortableCaptureFreeSpace {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [long] $RequiredBytes = 12GB
    )

    $pathRoot = [System.IO.Path]::GetPathRoot([System.IO.Path]::GetFullPath($Path))
    if ([string]::IsNullOrWhiteSpace($pathRoot) -or $pathRoot.StartsWith('\\')) {
        Write-Warning 'Unable to enforce the 12 GiB free-space check for this output path. Verify capacity manually.'
        return
    }

    $drive = [System.IO.DriveInfo]::new($pathRoot)
    if ($drive.IsReady -and $drive.AvailableFreeSpace -lt $RequiredBytes) {
        $freeGiB = [Math]::Round($drive.AvailableFreeSpace / 1GB, 2)
        throw "The output drive has only $freeGiB GiB free. At least 12 GiB is required before a new full capture."
    }
}

function Test-PortableDotNet8Runtime {
    param(
        [Parameter(Mandatory = $true)]
        [string] $DotNetPath
    )

    if (-not (Test-Path -LiteralPath $DotNetPath -PathType Leaf)) {
        return $false
    }

    try {
        $runtimes = & $DotNetPath --list-runtimes 2>$null
        return $LASTEXITCODE -eq 0 -and [bool]($runtimes -match '^Microsoft\.NETCore\.App 8\.')
    }
    catch {
        return $false
    }
}

function Ensure-PortableDotNet8Runtime {
    $localRoot = Join-Path $packageRoot ".tools\dotnet-runtime\$dotNetRuntimeVersion"
    $archivePath = Join-Path $localRoot "dotnet-runtime-$dotNetRuntimeVersion-win-x64.zip"
    $appRoot = Join-Path $localRoot 'app'
    $localDotNet = Join-Path $appRoot 'dotnet.exe'

    if (Test-PortableDotNet8Runtime -DotNetPath $localDotNet) {
        return [ordered]@{
            executable = $localDotNet
            version = $dotNetRuntimeVersion
            source = 'package-cache'
            archiveSha512 = $dotNetRuntimeSha512
            downloadUri = $dotNetRuntimeUri
        }
    }

    $systemDotNet = Get-Command dotnet.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($systemDotNet -and (Test-PortableDotNet8Runtime -DotNetPath $systemDotNet.Source)) {
        return [ordered]@{
            executable = $systemDotNet.Source
            version = 'system-8.x'
            source = 'system'
            archiveSha512 = $null
            downloadUri = $null
        }
    }

    [System.IO.Directory]::CreateDirectory($localRoot) | Out-Null
    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
        Write-Host "Downloading the pinned .NET $dotNetRuntimeVersion runtime from Microsoft..."
        Invoke-WebRequest -Uri $dotNetRuntimeUri -OutFile $archivePath -UseBasicParsing
    }

    $actualHash = Get-PortableFileHashHex -Path $archivePath -Algorithm SHA512
    if ($actualHash -cne $dotNetRuntimeSha512) {
        throw ".NET runtime archive hash mismatch. Expected $dotNetRuntimeSha512, found $actualHash."
    }

    if (-not (Test-Path -LiteralPath $localDotNet -PathType Leaf)) {
        if (Test-Path -LiteralPath $appRoot) {
            throw ".NET runtime folder exists but dotnet.exe is missing: $appRoot"
        }
        Expand-Archive -LiteralPath $archivePath -DestinationPath $appRoot
    }

    if (-not (Test-PortableDotNet8Runtime -DotNetPath $localDotNet)) {
        throw "The downloaded .NET runtime is not usable: $localDotNet"
    }

    return [ordered]@{
        executable = $localDotNet
        version = $dotNetRuntimeVersion
        source = 'downloaded-package-cache'
        archiveSha512 = $actualHash
        downloadUri = $dotNetRuntimeUri
    }
}

function Ensure-PortableIlSpy {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary] $DotNet
    )

    $toolRoot = Join-Path $packageRoot ".tools\ilspycmd\$ilSpyVersion"
    $packagePath = Join-Path $toolRoot "ilspycmd.$ilSpyVersion.nupkg"
    $appRoot = Join-Path $toolRoot 'app'
    $toolDll = Join-Path $appRoot 'tools\net8.0\any\ilspycmd.dll'

    [System.IO.Directory]::CreateDirectory($toolRoot) | Out-Null
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        Write-Host "Downloading the pinned ILSpy command-line package $ilSpyVersion from NuGet..."
        Invoke-WebRequest -Uri $ilSpyPackageUri -OutFile $packagePath -UseBasicParsing
    }

    $actualHash = Get-PortableFileHashHex -Path $packagePath -Algorithm SHA256
    if ($actualHash -cne $ilSpyPackageSha256) {
        throw "ILSpy package hash mismatch. Expected $ilSpyPackageSha256, found $actualHash."
    }

    if (-not (Test-Path -LiteralPath $toolDll -PathType Leaf)) {
        if (Test-Path -LiteralPath $appRoot) {
            throw "ILSpy app folder exists but ilspycmd.dll is missing: $appRoot"
        }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($packagePath, $appRoot)
    }

    $versionOutput = @(& $DotNet.executable $toolDll --version 2>&1)
    if ($LASTEXITCODE -ne 0 -or -not ($versionOutput -match [System.Text.RegularExpressions.Regex]::Escape($ilSpyVersion))) {
        throw "The extracted ILSpy tool did not report expected version $ilSpyVersion."
    }

    return [ordered]@{
        version = $ilSpyVersion
        packageSha256 = $actualHash
        downloadUri = $ilSpyPackageUri
        assembly = $toolDll
    }
}

function Get-PortableDecompileInventory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root
    )

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return @()
    }

    $resolvedRoot = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    return @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path = $_.FullName.Substring($resolvedRoot.Length).Replace('\', '/')
            bytes = [long]$_.Length
            sha256 = Get-PortableFileHashHex -Path $_.FullName
        }
    })
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Resolve-DolocTownGamePath -RepoRoot $packageRoot
}
else {
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source 'portable full capture -GameDir'
}
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path

$manifestPath = Get-PortableSteamManifestPath -ResolvedGameDir $GameDir
$steamIdentityParameters = @{
    ManifestPath = $manifestPath
    ExplicitBranch = $Branch
}
if ($AllowUnknownSteamBranch) {
    $steamIdentityParameters['AllowUnknownSteamBranch'] = $true
}
if ($AllowPendingBranchSwitch) {
    $steamIdentityParameters['AllowPendingBranchSwitch'] = $true
}
$startSteamIdentity = Get-DtmApiSteamBuildIdentity @steamIdentityParameters
$steamBuild = $startSteamIdentity.BuildId
$resolvedBranch = $startSteamIdentity.Branch
$sourceAssembly = Join-Path $GameDir 'DolocTown_Data\Managed\Assembly-CSharp.dll'
$sourceAssemblyHash = Get-PortableFileHashHex -Path $sourceAssembly
$buildName = "${steamBuild}_${resolvedBranch}_$($sourceAssemblyHash.Substring(0, 6))"
if ([string]::IsNullOrWhiteSpace($BuildRoot)) {
    $BuildRoot = Join-Path $packageRoot "references\doloc-town\reverse\builds\$buildName"
}
$BuildRoot = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $packageRoot -BuildRoot $BuildRoot -GameDir $GameDir

if (-not $InventoryOnly) {
    Assert-PortableCaptureFreeSpace -Path $BuildRoot
}

$captureParameters = @{
    GameDir = $GameDir
    BuildRoot = $BuildRoot
    Branch = $resolvedBranch
    ExpectedSteamBuild = $steamBuild
    ExpectedSteamManifestSha256 = $startSteamIdentity.ManifestSha256
    ExpectedAssemblyCSharpSha256 = $sourceAssemblyHash
    AssetRipperVersion = $AssetRipperVersion
    AssetRipperSha256 = $AssetRipperSha256
}
if ($AllowUnknownSteamBranch) { $captureParameters['AllowUnknownSteamBranch'] = $true }
if ($AllowPendingBranchSwitch) { $captureParameters['AllowPendingBranchSwitch'] = $true }
if ($InventoryOnly) { $captureParameters['InventoryOnly'] = $true }
if ($ReuseSnapshot) { $captureParameters['ReuseSnapshot'] = $true }
if ($ReuseExport) { $captureParameters['ReuseExport'] = $true }

Write-Host "Capture package: $packageRoot"
Write-Host "Source game: $GameDir"
Write-Host "Output build: $BuildRoot"
& $captureScript @captureParameters

$snapshotManagedRoot = Join-Path $BuildRoot 'raw-snapshot\game\DolocTown_Data\Managed'
$decompileRoot = Join-Path $BuildRoot 'decompiled'
$inventoryRoot = Join-Path $BuildRoot 'full-baseline-inventory'
$decompileLogRoot = Join-Path $BuildRoot 'code-decompile'
$decompileTargets = @(
    [ordered]@{ name = 'Assembly-CSharp'; file = 'Assembly-CSharp.dll'; required = $true },
    [ordered]@{ name = 'Assembly-CSharp-firstpass'; file = 'Assembly-CSharp-firstpass.dll'; required = $false }
)

$dotNetReceipt = $null
$ilSpyReceipt = $null
if (-not $InventoryOnly) {
    $dotNetReceipt = Ensure-PortableDotNet8Runtime
    $ilSpyReceipt = Ensure-PortableIlSpy -DotNet $dotNetReceipt
}

$decompileResults = [System.Collections.Generic.List[object]]::new()
foreach ($target in $decompileTargets) {
    $targetAssembly = Join-Path $snapshotManagedRoot $target.file
    $targetOutput = Join-Path $decompileRoot $target.name
    $targetLog = Join-Path $decompileLogRoot "$($target.name)-ilspy.log"
    $targetWasReused = $false

    if (-not (Test-Path -LiteralPath $targetAssembly -PathType Leaf)) {
        if ($target.required) {
            throw "Required frozen managed assembly is missing: $targetAssembly"
        }
        $decompileResults.Add([ordered]@{ assembly = $target.file; status = 'source-not-present'; files = 0; bytes = 0; sha256 = $null })
        continue
    }

    if ($InventoryOnly) {
        if (-not (Test-Path -LiteralPath $targetOutput -PathType Container)) {
            $decompileResults.Add([ordered]@{
                assembly = $target.file
                status = 'decompile-not-present'
                sourceBytes = [long](Get-Item -LiteralPath $targetAssembly).Length
                sourceSha256 = Get-PortableFileHashHex -Path $targetAssembly
                files = 0
                bytes = 0
            })
            continue
        }
    }
    elseif (Test-Path -LiteralPath $targetOutput) {
        if (-not $ReuseDecompile) {
            throw "Decompile output already exists. Refusing to overwrite it without -ReuseDecompile: $targetOutput"
        }
        $targetWasReused = $true
    }
    else {
        [System.IO.Directory]::CreateDirectory($decompileLogRoot) | Out-Null
        Write-Host "Decompiling $($target.file) with ILSpy $ilSpyVersion..."
        & $dotNetReceipt.executable $ilSpyReceipt.assembly -p -r $snapshotManagedRoot -o $targetOutput $targetAssembly 2>&1 |
            Tee-Object -FilePath $targetLog
        if ($LASTEXITCODE -ne 0) {
            throw "ILSpy failed for $($target.file) with exit code $LASTEXITCODE. See $targetLog"
        }
    }

    $targetInventory = @(Get-PortableDecompileInventory -Root $targetOutput)
    [long]$targetBytes = 0
    foreach ($entry in $targetInventory) {
        $targetBytes += [long]$entry.bytes
    }
    $inventoryPath = Join-Path $inventoryRoot "$($target.name)-decompiled-files.json"
    Write-PortableUtf8NoBomFile -Path $inventoryPath -Text (($targetInventory | ConvertTo-Json -Depth 5) + [Environment]::NewLine)
    $decompileResults.Add([ordered]@{
        assembly = $target.file
        status = $(if ($InventoryOnly) { 'verified-existing' } elseif ($targetWasReused) { 'reused-existing' } else { 'decompiled' })
        sourceBytes = [long](Get-Item -LiteralPath $targetAssembly).Length
        sourceSha256 = Get-PortableFileHashHex -Path $targetAssembly
        files = $targetInventory.Count
        bytes = $targetBytes
        output = $targetOutput
        inventory = $inventoryPath
    })
}

$portableSummary = [ordered]@{
    format = 'dtmapi.doloctown-portable-full-capture/v1'
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
    gameDir = $GameDir
    buildRoot = $BuildRoot
    assemblyCSharpSha256 = $sourceAssemblyHash
    inventoryOnly = [bool]$InventoryOnly
    assetRipper = [ordered]@{
        version = $AssetRipperVersion
        archiveSha256 = $AssetRipperSha256
    }
    dotNet = $dotNetReceipt
    ilSpy = $ilSpyReceipt
    decompiledAssemblies = @($decompileResults)
    sourceBoundary = 'Local research only. Do not publish or redistribute official snapshots, extracted assets, or decompiled source.'
}
$portableSummaryPath = Join-Path $inventoryRoot 'portable-full-capture-summary.json'
Write-PortableUtf8NoBomFile -Path $portableSummaryPath -Text (($portableSummary | ConvertTo-Json -Depth 10) + [Environment]::NewLine)

Write-Host "Portable full capture completed: $BuildRoot"
Write-Host "Portable receipt: $portableSummaryPath"
