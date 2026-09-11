param(
    [string] $Configuration = 'Release',
    [string] $OutputRoot = '',
    [string] $NetStandardPackageRoot = '',
    [string] $FrozenAbstractionsDll = '',
    [switch] $NoProvision,
    [switch] $SkipChecks
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\author-sdk-release-common.ps1"
. "$PSScriptRoot\author-sdk-preparation.ps1"
. "$PSScriptRoot\author-sdk-compatibility.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Core' -or $PSVersionTable.PSVersion.Major -lt 7) {
    throw 'Author SDK deterministic ZIP creation requires PowerShell 7+; Windows PowerShell 5.1 is retained as a syntax/check host but its .NET Framework ZipArchive cannot emit the frozen store-mode archive.'
}

if ($Configuration -ne 'Release') {
    throw 'The published Author SDK compatibility contract is Release-only. Use -Configuration Release.'
}

$repo = Get-RepoRoot
$sdkVersion = Get-AuthorSdkReleaseVersion -RepoRoot $repo
$targetCatalog = Get-AuthorSdkTargetCatalog -RepoRoot $repo
$availableTargets = @(Get-AuthorSdkAvailableTargets -Catalog $targetCatalog -SdkVersion $sdkVersion)
$dotnet = Get-DotNetExe -RepoRoot $repo
$dotnetSdkVersion = (& $dotnet --version | Select-Object -First 1).Trim()
if ($LASTEXITCODE -ne 0 -or $dotnetSdkVersion -ne '8.0.421') {
    throw "Author SDK $sdkVersion must be built with the pinned .NET SDK 8.0.421 so its Roslyn compiler is reproducible. Resolved: '$dotnetSdkVersion' from '$dotnet'."
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\author-sdk'
}
$outputFull = if ([IO.Path]::IsPathRooted($OutputRoot)) { [IO.Path]::GetFullPath($OutputRoot) } else { [IO.Path]::GetFullPath((Join-Path $repo $OutputRoot)) }
Assert-DtmApiBuildPathsDisjoint -OutputPath $outputFull -InputPaths @((Join-Path $repo 'src'), (Join-Path $repo 'author-sdk'), (Join-Path $repo 'tools/scripts'), (Join-Path $repo '.tools/dotnet'), (Join-Path $repo '.tools/author-sdk-packages'), $NetStandardPackageRoot, $FrozenAbstractionsDll)
$preparationInput = Get-DtmApiAuthorSdkInput -RepoRoot $repo -DotNetExe $dotnet
$preparedTargets = @{}
foreach ($target in $availableTargets) {
    # The legacy override remains bound to 0.5.5; every other target owns its source recipe.
    $legacyOverride = if ($target.apiTarget -ceq '0.5.5') { $FrozenAbstractionsDll } else { '' }
    $preparedTargets[$target.apiTarget] = Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -DotNetExe $dotnet -ApiTarget $target.apiTarget -NetStandardPackageRoot $NetStandardPackageRoot -FrozenAbstractionsDll $legacyOverride -NoProvision:$NoProvision
}
if (-not (Test-Path -LiteralPath $outputFull -PathType Container)) {
    New-Item -ItemType Directory -Path $outputFull -Force | Out-Null
}
$packageName = "DTMAPI-Author-SDK-$sdkVersion-win-x64"
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

    $packageCache = Join-Path $repo '.tools\author-sdk-packages'
    $nugetSource = 'https://api.nuget.org/v3/index.json'
    $authorProject = Join-Path $repo 'src\DTMAPI.AuthorSdk\DTMAPI.AuthorSdk.csproj'
    $pathMap = $repo + '=/_/DTMAPI'

    & $dotnet restore $authorProject -r win-x64 --packages $packageCache --source $nugetSource --force-evaluate --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Failed to restore the Author SDK publish graph into the repository-local package cache.' }
    $publishRoot = Join-Path $workRoot 'publish'
    & $dotnet publish $authorProject -c Release -r win-x64 --self-contained true --no-restore --nologo -o $publishRoot "/p:RestorePackagesPath=$packageCache" "/p:PathMap=$pathMap" '/p:ContinuousIntegrationBuild=true' '/p:Deterministic=true' '/p:DebugType=None' '/p:DebugSymbols=false'
    if ($LASTEXITCODE -ne 0) { throw 'Failed to publish the win-x64 self-contained Author SDK.' }

    Move-Item -LiteralPath $publishRoot -Destination $stageRoot
    Write-AuthorSdkUtf8NoBom -Path (Join-Path $stageRoot 'API-STATUS.md') -Value (Get-AuthorSdkApiStatusProjection -SourcePath (Join-Path $repo 'docs/api/public-api-matrix.md'))
    Get-ChildItem -LiteralPath $stageRoot -Filter '*.pdb' -File -Recurse | Remove-Item -Force

    Copy-Item -LiteralPath (Join-Path $repo 'author-sdk\target-catalog.json') -Destination (Join-Path $stageRoot 'target-catalog.json') -Force
    $contractsRoot = Join-Path $stageRoot 'contracts'
    New-Item -ItemType Directory -Path $contractsRoot -Force | Out-Null
    foreach ($target in $availableTargets) {
        $contract = Get-AuthorSdkContract -RepoRoot $repo -Target $target
        $preparedCompatibility = $preparedTargets[$target.apiTarget]
        $compatibilityRoot = Join-Path $stageRoot ([string]$target.payloadPath)
        Remove-AuthorSdkTreeSafely -AllowedRoot $stageRoot -Path $compatibilityRoot
        [IO.Directory]::CreateDirectory((Split-Path -Parent $compatibilityRoot)) | Out-Null
        Copy-Item -LiteralPath $preparedCompatibility.compatibilityRoot -Destination $compatibilityRoot -Recurse
        Copy-Item -LiteralPath (Join-Path (Join-Path $repo 'author-sdk') ([string]$target.contractPath)) -Destination (Join-Path $contractsRoot ("compatibility-$($target.apiTarget).contract.json")) -Force
    }

    $licensesRoot = Join-Path $stageRoot 'licenses'
    New-Item -ItemType Directory -Path $licensesRoot -Force | Out-Null
    $dotnetRoot = Split-Path -Parent $dotnet
    Copy-Item -LiteralPath (Join-Path $dotnetRoot 'LICENSE.txt') -Destination (Join-Path $licensesRoot 'dotnet-LICENSE.txt') -Force
    Copy-Item -LiteralPath (Join-Path $dotnetRoot 'ThirdPartyNotices.txt') -Destination (Join-Path $licensesRoot 'dotnet-ThirdPartyNotices.txt') -Force

    # The first public SDK includes the full standard build host, targeting packs,
    # SDK tasks/targets and templates. The self-contained CLI alone is insufficient.
    $toolchainRoot = Join-Path $stageRoot 'toolchain'
    New-Item -ItemType Directory -Path $toolchainRoot -Force | Out-Null
    Copy-Item -LiteralPath $dotnetRoot -Destination (Join-Path $toolchainRoot 'dotnet') -Recurse
    # Only the actual selected analyzer project's package graph enters the offline
    # feed: it includes NETStandard.Library and a working Roslyn generator basis.
    # Never enumerate or ship the user's entire NuGet cache.
    $offlineRoot = Join-Path $stageRoot 'offline-packages'
    New-Item -ItemType Directory -Path $offlineRoot -Force | Out-Null
    $analyzerAssets = Get-Content -Raw -LiteralPath (Join-Path $repo 'src/DTMAPI.Author.Analyzers/obj/project.assets.json') | ConvertFrom-Json
    foreach ($entry in $analyzerAssets.libraries.PSObject.Properties) {
        if ([string]$entry.Value.type -ne 'package') { continue }
        $packageRelative = [string]$entry.Value.path
        $parts = $packageRelative.Split('/')
        $archiveName = $parts[0] + '.' + $parts[1] + '.nupkg'
        $archive = $null
        foreach ($folder in $analyzerAssets.packageFolders.PSObject.Properties.Name) {
            $candidate = Join-Path (Join-Path $folder $packageRelative) $archiveName
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $archive = $candidate; break }
        }
        if ($null -eq $archive) { throw "Selected offline package archive is missing: $packageRelative" }
        Copy-Item -LiteralPath $archive -Destination (Join-Path $offlineRoot $archiveName)
    }

    $inventory = New-AuthorSdkReleaseInventory -StageRoot $stageRoot -DotNetSdkVersion $dotnetSdkVersion -SdkVersion $sdkVersion -TargetCatalog $targetCatalog
    Write-AuthorSdkUtf8NoBom -Path (Join-Path $stageRoot 'author-sdk-release.json') -Value (($inventory | ConvertTo-Json -Depth 10) + "`n")
    New-AuthorSdkDeterministicZip -SourceRoot $stageRoot -ZipPath $zipPath | Out-Null
    $zipHash = Get-AuthorSdkSha256 -Path $zipPath
    Write-AuthorSdkUtf8NoBom -Path $sidecarPath -Value ($zipHash + ' *' + [System.IO.Path]::GetFileName($zipPath) + "`n")

    if (-not $SkipChecks) {
        & "$PSScriptRoot\check-author-sdk-release.ps1" -PackagePath $zipPath
        if (-not $?) { throw 'Author SDK release check failed.' }
    }

    $preparedInput = Get-DtmApiAuthorSdkInput -RepoRoot $repo -DotNetExe $dotnet
    if ($preparedInput.sha256 -cne $preparationInput.sha256) {
        throw 'SDK inputs changed during preparation; no reusable preparation receipt was issued.'
    }
    $preparation = [ordered]@{
        schemaVersion = 1
        inputSha256 = $preparedInput.sha256
        stageRoot = $stageRoot
        inventorySha256 = Get-AuthorSdkSha256 -Path (Join-Path $stageRoot 'author-sdk-release.json')
        inputs = $preparedInput.identity
    }
    Write-AuthorSdkUtf8NoBom -Path (Join-Path $outputFull 'preparation.json') -Value (($preparation | ConvertTo-Json -Depth 8) + "`n")

    Write-Host "Author SDK stage: $stageRoot"
    Write-Host "Author SDK ZIP:   $zipPath"
    Write-Host "Author SDK SHA:   $zipHash"
}
finally {
    if (Test-Path -LiteralPath $workRoot) {
        Remove-AuthorSdkTreeSafely -AllowedRoot $outputFull -Path $workRoot
    }
}
