param(
    [string] $Configuration = 'Release',
    [string] $OutputRoot = '',
    [string] $NetStandardPackageRoot = '',
    [switch] $NoProvision,
    [switch] $SkipChecks
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\author-sdk-release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Core' -or $PSVersionTable.PSVersion.Major -lt 7) {
    throw 'Author SDK deterministic ZIP creation requires PowerShell 7+; Windows PowerShell 5.1 is retained as a syntax/check host but its .NET Framework ZipArchive cannot emit the frozen store-mode archive.'
}

function Resolve-NetStandardPackageRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $DotNetExe,
        [Parameter(Mandatory = $true)] $Contract,
        [string] $RequestedRoot,
        [switch] $DisableProvision
    )

    $packageCache = Join-Path $RepoRoot '.tools\author-sdk-packages'
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        $requested = [System.IO.Path]::GetFullPath($RequestedRoot)
        $candidates.Add($requested) | Out-Null
        $candidates.Add((Join-Path $requested 'netstandard.library\2.0.3')) | Out-Null
        $candidates.Add((Join-Path $requested 'NETStandard.Library\2.0.3')) | Out-Null
    }
    else {
        $candidates.Add((Join-Path $packageCache 'netstandard.library\2.0.3')) | Out-Null
    }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'build\netstandard2.0\ref') -PathType Container) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        throw "The explicit NETStandard.Library package root does not contain 2.0.3/build/netstandard2.0/ref: $RequestedRoot"
    }
    if ($DisableProvision) {
        throw "The repository-local NETStandard.Library 2.0.3 cache is absent and -NoProvision was requested: $packageCache"
    }

    $provisionRoot = Join-Path $packageCache '.provision'
    if (-not (Test-Path -LiteralPath $provisionRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $provisionRoot -Force | Out-Null
    }
    $projectPath = Join-Path $provisionRoot 'DTMAPI.AuthorSdk.CompatibilityRestore.csproj'
    $projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RestorePackagesPath>$([System.Security.SecurityElement]::Escape($packageCache))</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NETStandard.Library" Version="2.0.3" />
  </ItemGroup>
</Project>
"@
    Write-AuthorSdkUtf8NoBom -Path $projectPath -Value $projectXml
    & $DotNetExe restore $projectPath --packages $packageCache --source 'https://api.nuget.org/v3/index.json' --force-evaluate --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to provision the pinned NETStandard.Library 2.0.3 package into the repository-local Author SDK cache.'
    }
    $resolved = Join-Path $packageCache 'netstandard.library\2.0.3'
    if (-not (Test-Path -LiteralPath (Join-Path $resolved 'build\netstandard2.0\ref') -PathType Container)) {
        throw "NuGet restore completed without the expected pinned package tree: $resolved"
    }
    return [System.IO.Path]::GetFullPath($resolved)
}

function Assert-NetStandardPackage {
    param(
        [Parameter(Mandatory = $true)] [string] $PackageRoot,
        [Parameter(Mandatory = $true)] $Contract
    )

    $nuspec = Join-Path $PackageRoot 'netstandard.library.nuspec'
    if (-not (Test-Path -LiteralPath $nuspec -PathType Leaf)) {
        throw "Pinned package nuspec is missing: $nuspec"
    }
    [xml]$metadata = Get-Content -Raw -Encoding UTF8 -LiteralPath $nuspec
    $id = [string]$metadata.package.metadata.id
    $version = [string]$metadata.package.metadata.version
    if ($id -ne [string]$Contract.netstandardReferencePackage -or $version -ne [string]$Contract.netstandardReferencePackageVersion) {
        throw "Pinned package identity mismatch: id=$id version=$version"
    }
    $referenceRoot = Join-Path $PackageRoot 'build\netstandard2.0\ref'
    $references = @(Get-ChildItem -LiteralPath $referenceRoot -File)
    if ($references.Count -ne [int]$Contract.netstandardReferenceFileCount) {
        throw "Pinned reference count mismatch: expected=$($Contract.netstandardReferenceFileCount) actual=$($references.Count)"
    }
    $digest = Get-AuthorSdkFileTreeDigestV1 -Root $referenceRoot
    if (-not $digest.Equals([string]$Contract.netstandardReferenceInventorySha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Pinned NETStandard.Library reference inventory mismatch: expected=$($Contract.netstandardReferenceInventorySha256) actual=$digest"
    }
    $license = Join-Path $PackageRoot 'LICENSE.TXT'
    $notice = Join-Path $PackageRoot 'THIRD-PARTY-NOTICES.TXT'
    if ((Get-AuthorSdkSha256 -Path $license) -ne ([string]$Contract.netstandardLicenseSha256).ToLowerInvariant()) {
        throw 'Pinned NETStandard.Library license hash mismatch.'
    }
    if ((Get-AuthorSdkSha256 -Path $notice) -ne ([string]$Contract.netstandardNoticeSha256).ToLowerInvariant()) {
        throw 'Pinned NETStandard.Library notice hash mismatch.'
    }
    return $referenceRoot
}

if ($Configuration -ne 'Release') {
    throw 'The published Author SDK compatibility contract is Release-only. Use -Configuration Release.'
}

$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$dotnetSdkVersion = (& $dotnet --version | Select-Object -First 1).Trim()
if ($LASTEXITCODE -ne 0 -or $dotnetSdkVersion -ne '8.0.421') {
    throw "Author SDK 0.1.0 must be built with the frozen .NET SDK 8.0.421 so its Roslyn compiler is reproducible. Resolved: '$dotnetSdkVersion' from '$dotnet'."
}
$contract = Get-AuthorSdkContract -RepoRoot $repo
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\author-sdk'
}
$outputFull = [System.IO.Path]::GetFullPath($OutputRoot)
if (-not (Test-Path -LiteralPath $outputFull -PathType Container)) {
    New-Item -ItemType Directory -Path $outputFull -Force | Out-Null
}
$packageName = 'DTMAPI-Author-SDK-0.1.0-win-x64'
$stageRoot = Join-Path $outputFull $packageName
$zipPath = Join-Path $outputFull ($packageName + '.zip')
$sidecarPath = $zipPath + '.sha256'
$workRoot = Join-Path $outputFull ('.author-sdk-build-' + [System.Diagnostics.Process]::GetCurrentProcess().Id + '-' + [Guid]::NewGuid().ToString('N'))
Assert-AuthorSdkChildPath -Root $outputFull -Path $stageRoot -Label 'Author SDK stage root' | Out-Null
Assert-AuthorSdkChildPath -Root $outputFull -Path $workRoot -Label 'Author SDK temporary build root' | Out-Null

try {
    Remove-AuthorSdkTreeSafely -AllowedRoot $outputFull -Path $stageRoot
    Remove-AuthorSdkTreeSafely -AllowedRoot $outputFull -Path $workRoot
    foreach ($file in @($zipPath, $sidecarPath)) {
        Assert-AuthorSdkChildPath -Root $outputFull -Path $file -Label 'Author SDK output file' | Out-Null
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
    }
    New-Item -ItemType Directory -Path $workRoot -Force | Out-Null

    $packageRoot = Resolve-NetStandardPackageRoot -RepoRoot $repo -DotNetExe $dotnet -Contract $contract -RequestedRoot $NetStandardPackageRoot -DisableProvision:$NoProvision
    $referenceRoot = Assert-NetStandardPackage -PackageRoot $packageRoot -Contract $contract

    $packageCache = Join-Path $repo '.tools\author-sdk-packages'
    $nugetSource = 'https://api.nuget.org/v3/index.json'
    $abstractionsProject = Join-Path $repo 'src\DTMAPI.Abstractions\DTMAPI.Abstractions.csproj'
    $authorProject = Join-Path $repo 'src\DTMAPI.AuthorSdk\DTMAPI.AuthorSdk.csproj'
    $pathMap = $repo + '=/_/DTMAPI'

    & $dotnet restore $abstractionsProject --packages $packageCache --source $nugetSource --force-evaluate --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Failed to restore the fixed Abstractions build graph into the repository-local package cache.' }
    & $dotnet build $abstractionsProject -c Release --no-restore --nologo "/p:RestorePackagesPath=$packageCache" "/p:PathMap=$pathMap" '/p:ContinuousIntegrationBuild=true' '/p:Deterministic=true' '/p:DebugType=None' '/p:DebugSymbols=false'
    if ($LASTEXITCODE -ne 0) { throw 'Failed to build DTMAPI.Abstractions for the fixed compatibility payload.' }

    $abstractionsPath = Join-Path $repo 'src\DTMAPI.Abstractions\bin\Release\netstandard2.0\DTMAPI.Abstractions.dll'
    $abstractionsHash = Get-AuthorSdkSha256 -Path $abstractionsPath
    if ($abstractionsHash -ne ([string]$contract.abstractionsSha256).ToLowerInvariant()) {
        throw "Repository-built Abstractions hash no longer matches the tracked SDK 0.1 contract. expected=$($contract.abstractionsSha256) actual=$abstractionsHash"
    }

    & $dotnet restore $authorProject -r win-x64 --packages $packageCache --source $nugetSource --force-evaluate --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Failed to restore the Author SDK publish graph into the repository-local package cache.' }
    $publishRoot = Join-Path $workRoot 'publish'
    & $dotnet publish $authorProject -c Release -r win-x64 --self-contained true --no-restore --nologo -o $publishRoot "/p:RestorePackagesPath=$packageCache" "/p:PathMap=$pathMap" '/p:ContinuousIntegrationBuild=true' '/p:Deterministic=true' '/p:DebugType=None' '/p:DebugSymbols=false'
    if ($LASTEXITCODE -ne 0) { throw 'Failed to publish the win-x64 self-contained Author SDK.' }

    Move-Item -LiteralPath $publishRoot -Destination $stageRoot
    Get-ChildItem -LiteralPath $stageRoot -Filter '*.pdb' -File -Recurse | Remove-Item -Force

    $compatibilityRoot = Join-Path $stageRoot 'compatibility\0.5.5'
    $compatibilityRefRoot = Join-Path $compatibilityRoot 'ref\netstandard2.0'
    if (-not (Test-Path -LiteralPath $compatibilityRefRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $compatibilityRefRoot -Force | Out-Null
    }
    foreach ($reference in @(Get-ChildItem -LiteralPath $referenceRoot -File | Sort-Object Name -CaseSensitive)) {
        Copy-Item -LiteralPath $reference.FullName -Destination (Join-Path $compatibilityRefRoot $reference.Name) -Force
    }
    Copy-Item -LiteralPath $abstractionsPath -Destination (Join-Path $compatibilityRoot 'DTMAPI.Abstractions.dll') -Force
    Copy-Item -LiteralPath (Join-Path $repo 'author-sdk\compatibility\0.5.5\DTMAPI.Author.props') -Destination (Join-Path $compatibilityRoot 'DTMAPI.Author.props') -Force
    Copy-Item -LiteralPath (Join-Path $packageRoot 'LICENSE.TXT') -Destination (Join-Path $compatibilityRoot 'NETStandard.Library.LICENSE.TXT') -Force
    Copy-Item -LiteralPath (Join-Path $packageRoot 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $compatibilityRoot 'NETStandard.Library.THIRD-PARTY-NOTICES.TXT') -Force

    $compatibilityFiles = New-Object 'System.Collections.Generic.List[object]'
    [string[]]$compatibilityPaths = @(Get-ChildItem -LiteralPath $compatibilityRoot -File -Recurse | ForEach-Object { $_.FullName })
    [Array]::Sort($compatibilityPaths, [System.StringComparer]::Ordinal)
    foreach ($file in $compatibilityPaths) {
        $relative = Get-AuthorSdkRelativePath -Root $compatibilityRoot -Path $file
        $kind = 'reference'
        if ($relative -eq 'DTMAPI.Abstractions.dll') { $kind = 'abstractions' }
        elseif ($relative -eq 'DTMAPI.Author.props') { $kind = 'props' }
        elseif ($relative -eq 'NETStandard.Library.LICENSE.TXT') { $kind = 'license' }
        elseif ($relative -eq 'NETStandard.Library.THIRD-PARTY-NOTICES.TXT') { $kind = 'notice' }
        $compatibilityFiles.Add([ordered]@{ path = $relative; sha256 = Get-AuthorSdkSha256 -Path $file; kind = $kind }) | Out-Null
    }
    $compatibilityManifest = [ordered]@{
        schemaVersion = 1
        sdkVersion = '0.1.0'
        targetRuntimeVersion = '0.5.5'
        abstractionsAssemblyVersion = [string]$contract.abstractionsAssemblyVersion
        abstractionsFileVersion = [string]$contract.abstractionsFileVersion
        files = $compatibilityFiles.ToArray()
    }
    Write-AuthorSdkUtf8NoBom -Path (Join-Path $compatibilityRoot 'compatibility.json') -Value (($compatibilityManifest | ConvertTo-Json -Depth 8) + "`n")

    $contractsRoot = Join-Path $stageRoot 'contracts'
    $licensesRoot = Join-Path $stageRoot 'licenses'
    New-Item -ItemType Directory -Path $contractsRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $licensesRoot -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo 'author-sdk\compatibility\0.5.5\compatibility.contract.json') -Destination (Join-Path $contractsRoot 'compatibility-0.5.5.contract.json') -Force
    $dotnetRoot = Split-Path -Parent $dotnet
    Copy-Item -LiteralPath (Join-Path $dotnetRoot 'LICENSE.txt') -Destination (Join-Path $licensesRoot 'dotnet-LICENSE.txt') -Force
    Copy-Item -LiteralPath (Join-Path $dotnetRoot 'ThirdPartyNotices.txt') -Destination (Join-Path $licensesRoot 'dotnet-ThirdPartyNotices.txt') -Force

    $inventory = New-AuthorSdkReleaseInventory -StageRoot $stageRoot -DotNetSdkVersion $dotnetSdkVersion
    Write-AuthorSdkUtf8NoBom -Path (Join-Path $stageRoot 'author-sdk-release.json') -Value (($inventory | ConvertTo-Json -Depth 10) + "`n")
    New-AuthorSdkDeterministicZip -SourceRoot $stageRoot -ZipPath $zipPath | Out-Null
    $zipHash = Get-AuthorSdkSha256 -Path $zipPath
    Write-AuthorSdkUtf8NoBom -Path $sidecarPath -Value ($zipHash + ' *' + [System.IO.Path]::GetFileName($zipPath) + "`n")

    if (-not $SkipChecks) {
        & "$PSScriptRoot\check-author-sdk-release.ps1" -PackagePath $zipPath
        if ($LASTEXITCODE -ne 0) { throw 'Author SDK release check failed.' }
    }

    Write-Host "Author SDK stage: $stageRoot"
    Write-Host "Author SDK ZIP:   $zipPath"
    Write-Host "Author SDK SHA:   $zipHash"
}
finally {
    if (Test-Path -LiteralPath $workRoot) {
        Remove-AuthorSdkTreeSafely -AllowedRoot $outputFull -Path $workRoot
    }
}
