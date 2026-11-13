param(
    [string] $Configuration = 'Release',
    [string] $OutputRoot = '',
    [string] $ProductsPath = '',
    [string] $ProductSourceRoot = '',
    [string[]] $Mod = @(),
    [string] $AssetRoot = $env:DTMAPI_PRIVATE_MODS_ASSET_ROOT,
    [switch] $SkipBuild,
    [switch] $RuntimeOnly,
    [switch] $ModsOnly,
    [switch] $PlanOnly,
    [switch] $LibraryOnly
)

. "$PSScriptRoot\windows-installer/common.ps1"
. "$PSScriptRoot\windows-installer/release-common.ps1" -ProductsPath $ProductsPath -ProductSourceRoot $ProductSourceRoot
$ErrorActionPreference = 'Stop'

function Assert-DtmApiWorkshopOrdinaryTree {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $resolved -PathType Container)) {
        throw "$Context must be an existing directory: $resolved"
    }
    $reparse = @(Get-ChildItem -LiteralPath $resolved -Force -Recurse -ErrorAction Stop | Where-Object {
        ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
    })
    $rootItem = Get-Item -LiteralPath $resolved -Force -ErrorAction Stop
    if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or $reparse.Count -gt 0) {
        throw "$Context must not contain a reparse point: $resolved"
    }
    return $resolved
}

function New-DtmApiWorkshopPublicationSession {
    param([Parameter(Mandatory = $true)] [string] $FinalRoot)

    $final = [System.IO.Path]::GetFullPath($FinalRoot).TrimEnd([char]92, [char]47)
    $parent = [System.IO.Path]::GetFullPath((Split-Path -Parent $final)).TrimEnd([char]92, [char]47)
    $leaf = [System.IO.Path]::GetFileName($final)
    if ([string]::IsNullOrWhiteSpace($leaf) -or $leaf -in @('.','..') -or
        [System.IO.Path]::GetPathRoot($final).TrimEnd([char]92, [char]47) -ceq $final -or
        $leaf.TrimEnd([char]32, [char]46) -cne $leaf -or
        $leaf.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
        throw "Workshop publication OutputRoot is not one safe directory leaf: $final"
    }
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    $parentItem = Get-Item -LiteralPath $parent -Force -ErrorAction Stop
    if (($parentItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Workshop publication parent must not be a reparse point: $parent"
    }
    if (Test-Path -LiteralPath $final) {
        if (-not (Test-Path -LiteralPath $final -PathType Container)) {
            throw "Workshop publication OutputRoot exists but is not a directory: $final"
        }
        $null = Assert-DtmApiWorkshopOrdinaryTree -Path $final -Context 'Existing Workshop publication root'
    }
    $token = [Guid]::NewGuid().ToString('N')
    $staging = Join-Path $parent ('.' + $leaf + '.staging-' + $token)
    $backup = Join-Path $parent ('.' + $leaf + '.previous-' + $token)
    foreach ($transient in @($staging, $backup)) {
        if ((Split-Path -Parent ([System.IO.Path]::GetFullPath($transient))) -cne $parent -or (Test-Path -LiteralPath $transient)) {
            throw "Workshop publication transient path is not one new direct sibling: $transient"
        }
    }
    [System.IO.Directory]::CreateDirectory($staging) | Out-Null
    return [pscustomobject][ordered]@{
        SchemaVersion = 1
        Token = $token
        Parent = $parent
        Leaf = $leaf
        FinalRoot = $final
        StagingRoot = [System.IO.Path]::GetFullPath($staging)
        BackupRoot = [System.IO.Path]::GetFullPath($backup)
        ExistingFinal = Test-Path -LiteralPath $final -PathType Container
    }
}

function Remove-DtmApiWorkshopPublicationTransient {
    param(
        [Parameter(Mandatory = $true)] $Session,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    $allowed = @(
        [System.IO.Path]::GetFullPath([string]$Session.StagingRoot).TrimEnd([char]92, [char]47),
        [System.IO.Path]::GetFullPath([string]$Session.BackupRoot).TrimEnd([char]92, [char]47)
    )
    if ($allowed -cnotcontains $resolved -or
        (Split-Path -Parent $resolved) -cne [string]$Session.Parent -or
        [System.IO.Path]::GetFileName($resolved) -notlike ('.' + [string]$Session.Leaf + '.*-' + [string]$Session.Token)) {
        throw "Workshop publication cleanup escaped its exact sibling session: $resolved"
    }
    if (Test-Path -LiteralPath $resolved) {
        $null = Assert-DtmApiWorkshopOrdinaryTree -Path $resolved -Context 'Workshop publication transient cleanup root'
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

function Publish-DtmApiWorkshopPublicationSession {
    param([Parameter(Mandatory = $true)] $Session)

    $final = [string]$Session.FinalRoot
    $staging = [string]$Session.StagingRoot
    $backup = [string]$Session.BackupRoot
    $null = Assert-DtmApiWorkshopOrdinaryTree -Path $staging -Context 'Completed Workshop publication staging root'
    Remove-DtmApiWorkshopDownloadMarkers -PackageRoot $staging
    if (Test-Path -LiteralPath $backup) {
        throw "Workshop publication backup path already exists: $backup"
    }
    $oldMoved = $false
    if (Test-Path -LiteralPath $final -PathType Container) {
        $null = Assert-DtmApiWorkshopOrdinaryTree -Path $final -Context 'Workshop publication root before exchange'
        [System.IO.Directory]::Move($final, $backup)
        $oldMoved = $true
    }
    try {
        [System.IO.Directory]::Move($staging, $final)
    }
    catch {
        if ($oldMoved -and -not (Test-Path -LiteralPath $final) -and (Test-Path -LiteralPath $backup -PathType Container)) {
            [System.IO.Directory]::Move($backup, $final)
        }
        throw
    }
    if ($oldMoved) {
        Remove-DtmApiWorkshopPublicationTransient -Session $Session -Path $backup
    }
    return [pscustomobject]@{
        FinalRoot = $final
        PreviousRootExisted = $oldMoved
        Published = $true
    }
}

function Restore-DtmApiWorkshopPublicationSessionAfterFailure {
    param([Parameter(Mandatory = $true)] $Session)

    $final = [string]$Session.FinalRoot
    $staging = [string]$Session.StagingRoot
    $backup = [string]$Session.BackupRoot
    if (Test-Path -LiteralPath $backup -PathType Container) {
        if (Test-Path -LiteralPath $final) {
            throw "Workshop publication failure left both final and previous roots; preserving both for bounded recovery. final=$final previous=$backup"
        }
        $null = Assert-DtmApiWorkshopOrdinaryTree -Path $backup -Context 'Workshop publication previous root before rollback'
        [System.IO.Directory]::Move($backup, $final)
    }
    if (Test-Path -LiteralPath $staging) {
        Remove-DtmApiWorkshopPublicationTransient -Session $Session -Path $staging
    }
}

if ($LibraryOnly) {
    return
}

$repo = Get-RepoRoot
if ($RuntimeOnly -and ($ModsOnly -or $Mod.Count -gt 0)) {
    throw '-RuntimeOnly cannot be combined with -ModsOnly or -Mod.'
}
if (-not $OutputRoot) { $OutputRoot = Join-Path $repo 'dist/workshop-packages' }
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$products = @(Get-DtmApiReleaseProducts)
$windowsProduct = $products | Where-Object {
    [IO.Path]::GetFullPath((Join-Path $script:DtmApiProductSourceRoot $_.sourceRoot)) -eq (Join-Path $repo 'build/runtime-workshop')
} | Select-Object -First 1
$multiPlatformProduct = $products | Where-Object {
    [IO.Path]::GetFullPath((Join-Path $script:DtmApiProductSourceRoot $_.sourceRoot)) -eq (Join-Path $repo 'build/dtmapi-multiplatform')
} | Select-Object -First 1
$releaseMods = @()
if (-not $RuntimeOnly) {
    $releaseMods = @(Get-DtmApiProductDefinitions)
    if ($Mod.Count -gt 0) {
        $releaseMods = @($releaseMods | Where-Object {
            $_.UniqueID -in $Mod -or $_.OfficialFolder -in $Mod -or (Split-Path -Leaf $_.SourceRoot) -in $Mod
        })
        foreach ($selection in $Mod) {
            if (-not @($releaseMods | Where-Object {
                $_.UniqueID -eq $selection -or $_.OfficialFolder -eq $selection -or (Split-Path -Leaf $_.SourceRoot) -eq $selection
            }).Count) { throw "Mod is not in the release list: $selection" }
        }
    }
}
$selectedProducts = @(
    if (-not $ModsOnly) {
        if (-not $windowsProduct -or -not $multiPlatformProduct) {
            throw 'The release list must include the Windows and multiplatform Runtime packages.'
        }
        $windowsProduct
        $multiPlatformProduct
    }
    foreach ($definition in $releaseMods) {
        $products | Where-Object { $_.sourceRoot -eq $definition.SourceRoot }
    }
)
if ($PlanOnly) { return $selectedProducts }

if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
    throw 'Pass -AssetRoot (the private artwork directory) or set DTMAPI_PRIVATE_MODS_ASSET_ROOT.'
}
$AssetRoot = [IO.Path]::GetFullPath($AssetRoot)
$runtimeBuildCommit = Get-DtmApiSourceCommit -RepoRoot $repo
if (-not $ModsOnly) {
    $runtimeProjects = @('src/DTMAPI.BepInExBootstrap/DTMAPI.BepInExBootstrap.csproj') + @(
        Get-DtmApiRuntimeComponentFile | Where-Object { $_.Project } | ForEach-Object { $_.Project }
    )
    if (-not $SkipBuild) {
        & "$PSScriptRoot/build-runtime-modules.ps1" -Configuration $Configuration
        & "$PSScriptRoot/build.ps1" -Configuration $Configuration -Projects $runtimeProjects -SkipTests -Rebuild
        if (-not $?) { throw 'Runtime source build failed.' }
    }
}

function Copy-DtmApiCmdFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Missing CMD entry source: $Source"
    }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
    $text = [System.IO.File]::ReadAllText($Source)
    if ($text -match '[^\u0000-\u007F]') {
        throw "CMD entry sources must remain ASCII-only: $Source"
    }
    $text = [regex]::Replace($text, "\r?\n", "`r`n")
    [System.IO.File]::WriteAllText($Destination, $text, [System.Text.Encoding]::ASCII)
}

function Get-DtmApiWorkshopRuntimeAssemblyReceipts {
    param(
        [Parameter(Mandatory = $true)] [string] $PayloadRoot,
        [Parameter(Mandatory = $true)] [string[]] $FileNames
    )
    foreach ($fileName in $FileNames) {
        $path = Join-Path $PayloadRoot $fileName
        $item = Get-Item -LiteralPath $path -ErrorAction Stop
        $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($path).FileVersion
        [ordered]@{
            FileName = $fileName
            Length = [long]$item.Length
            Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
            FileVersion = $fileVersion
        }
    }
}

function Copy-DtmApiWorkshopComponents {
    param([Parameter(Mandatory = $true)] [string] $PayloadRoot)

    & "$PSScriptRoot/prepare-audio-decoder.ps1" | Out-Null
    foreach ($component in @(Get-DtmApiRuntimeComponentFile)) {
        $fileName = [IO.Path]::GetFileName($component.RelativePath)
        if ($component.Auxiliary) {
            $source = Join-Path $repo ('.tools/audio-decoder/distribution/' + $fileName)
        }
        else {
            $projectRoot = Split-Path -Parent (Join-Path $repo $component.Project)
            $source = Join-Path $projectRoot "bin/$Configuration/$($component.TargetFramework)/$fileName"
        }
        $destination = Join-Path $PayloadRoot $component.RelativePath
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath $source -Destination $destination
        $assembly = if (-not $component.Auxiliary) { [Reflection.AssemblyName]::GetAssemblyName($destination) } else { $null }
        [ordered]@{
            ComponentId = $component.Id
            Distribution = 'dormant-shipped'
            LoadPolicy = $component.LoadPolicy
            RelativePath = $component.RelativePath
            Length = (Get-Item -LiteralPath $destination).Length
            Sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
            AssemblyName = $(if ($assembly) { $assembly.Name } else { '' })
            AssemblyVersion = $(if ($assembly) { $assembly.Version.ToString() } else { '' })
            FileVersion = $(if ($assembly) { [Diagnostics.FileVersionInfo]::GetVersionInfo($destination).FileVersion } else { '' })
            TargetFramework = $component.TargetFramework
            DefaultLoadState = 'dormant'
            IncludedInDownloadPackage = $true
        }
    }
}

$publishOutputRoot = $OutputRoot
$publicationSession = New-DtmApiWorkshopPublicationSession -FinalRoot $publishOutputRoot
$OutputRoot = [string]$publicationSession.StagingRoot
$buildRoot = Join-Path ([IO.Path]::GetTempPath()) ('dtmapi-release-' + [guid]::NewGuid().ToString('N'))
$publicationSucceeded = $false
try {
    # A selected rebuild leaves the other packages in the upload directory intact.
    if (($RuntimeOnly -or $ModsOnly -or $Mod.Count -gt 0) -and (Test-Path -LiteralPath $publishOutputRoot)) {
        $selectedFolders = @($selectedProducts | ForEach-Object { $_.uploadFolder })
        foreach ($existing in @(Get-ChildItem -LiteralPath $publishOutputRoot -Directory)) {
            if ($existing.Name -notin $selectedFolders) {
                Copy-Item -LiteralPath $existing.FullName -Destination $OutputRoot -Recurse
            }
        }
    }

    if (-not $ModsOnly) {
        $runtimePackage = Join-Path $OutputRoot $windowsProduct.uploadFolder
        [void][IO.Directory]::CreateDirectory($runtimePackage)
        $runtimeSource = Join-Path $script:DtmApiProductSourceRoot $windowsProduct.sourceRoot
        foreach ($entry in [DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::WindowsEntries) {
            Copy-DtmApiCmdFile -Source (Join-Path "$PSScriptRoot/dtmapi-multiplatform" $entry) -Destination (Join-Path $runtimePackage $entry)
        }
        $brandingRoot = Join-Path $AssetRoot 'branding'
        Copy-Item -LiteralPath (Join-Path $brandingRoot 'dtmapi-icon.png') -Destination (Join-Path $runtimePackage 'icon.png')
        Copy-Item -LiteralPath (Join-Path $brandingRoot 'dtmapi-preview.png') -Destination (Join-Path $runtimePackage 'preview.png')

        $bootstrapArchive = Join-Path $runtimePackage ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::BepInExArchive)
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $bootstrapArchive))
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('bootstrap/' + [IO.Path]::GetFileName($bootstrapArchive))) -Destination $bootstrapArchive

        $payloadRoot = Join-Path $runtimePackage ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::PayloadDirectory)
        $runtimePayload = Join-Path $payloadRoot ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::RuntimeDirectory)
        $runtimeFiles = @(Get-DtmApiRuntimeAssemblies)
        $outDir = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
        Copy-DirectoryContents -Source $outDir -Destination $runtimePayload -Include $runtimeFiles -Required
        $brandingDestination = Join-Path $payloadRoot ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::BrandingFile)
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $brandingDestination))
        Copy-Item -LiteralPath (Join-Path $brandingRoot 'dtmapi-icon.png') -Destination $brandingDestination
        $runtimeAssemblyReceipts = @(Get-DtmApiWorkshopRuntimeAssemblyReceipts -PayloadRoot $runtimePayload -FileNames $runtimeFiles)
        $componentReceipts = @(Copy-DtmApiWorkshopComponents -PayloadRoot $payloadRoot)
        $manifest = New-DtmApiReleaseManifest -RepoRoot $repo -PackageKind 'workshop-runtime' -IncludedAssemblies $runtimeAssemblyReceipts -OptionalComponents $componentReceipts -BundledMods @() -BuildCommit $runtimeBuildCommit.Substring(0, 12)
        Write-Utf8NoBomJson -Path (Join-Path $runtimePackage ([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::ReleaseManifest)) -Value $manifest
        $runtimeInfo = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $runtimeSource 'info.json') | ConvertFrom-Json
        $runtimeInfo | Add-Member -NotePropertyName version -NotePropertyValue $script:DtmApiReleaseVersion -Force
        Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'info.json') -Value $runtimeInfo

        $hostBuild = Join-Path $buildRoot 'installer-hosts'
        & "$PSScriptRoot/build-multiplatform-runtime-installer.ps1" -OutputRoot $hostBuild | Out-Host
        & "$PSScriptRoot/build-dtmapi-multiplatform-package.ps1" -SourceKind Candidate -AcceptedPackageRoot $runtimePackage -HostBuildReceiptPath (Join-Path $hostBuild 'installer-build.json') -OutputRoot (Join-Path $OutputRoot $multiPlatformProduct.uploadFolder) -ProductsPath $ProductsPath -ProductSourceRoot $ProductSourceRoot -AssetRoot $brandingRoot | Out-Host
        if (-not $?) { throw 'Multiplatform Runtime package build failed.' }
        $multiPlatformPackage = Join-Path $OutputRoot $multiPlatformProduct.uploadFolder
        $windowsHost = [DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::WindowsHost
        Copy-Item -LiteralPath (Join-Path $multiPlatformPackage $windowsHost) -Destination (Join-Path $runtimePackage $windowsHost)
        foreach ($relative in @([DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::MultiPlatformManifest, [DTMAPI.MultiPlatformInstaller.RuntimePackageFiles]::HostManifest)) {
            $metadata = Get-Content -LiteralPath (Join-Path $multiPlatformPackage $relative) -Raw -Encoding UTF8 | ConvertFrom-Json
            $metadata.WorkshopId = [string]$windowsProduct.workshopId
            if ($metadata.PSObject.Properties['HostArtifacts']) {
                $metadata.HostArtifacts = @($metadata.HostArtifacts | Where-Object Rid -CEQ 'win-x64')
                $metadata.DisplayName = [string]$runtimeInfo.name
                $metadata.ShowNativeTitleEntry = $false
            }
            if ($metadata.PSObject.Properties['Hosts']) {
                $metadata.Hosts = @($metadata.Hosts | Where-Object Rid -CEQ 'win-x64')
            }
            Write-Utf8NoBomJson -Path (Join-Path $runtimePackage $relative) -Value $metadata
        }
    }

    foreach ($definition in $releaseMods) {
        $sourceRoot = Join-Path $script:DtmApiProductSourceRoot $definition.SourceRoot
        $modOutput = Join-Path $buildRoot $definition.PackageName
        $modAssets = Join-Path $AssetRoot (Split-Path -Leaf $sourceRoot)
        $contentBuilder = Join-Path $sourceRoot 'build-package.ps1'
        if ($definition.ContentOnly -and (Test-Path -LiteralPath $contentBuilder)) {
            $assembledRoot = Join-Path $sourceRoot ('.local-build/' + [guid]::NewGuid().ToString('N'))
            try {
                & (Join-Path $sourceRoot 'build-local-prototype.ps1') -OutputRoot $assembledRoot -AssetRoot $modAssets | Out-Host
                & $contentBuilder -AssembledRoot $assembledRoot -AssetRoot $modAssets -OutputRoot $modOutput | Out-Host
            }
            finally {
                if (Test-Path -LiteralPath $assembledRoot) { Remove-Item -LiteralPath $assembledRoot -Recurse -Force }
            }
        }
        else {
            $modInput = if ($definition.Project) { Join-Path $sourceRoot ($definition.Project + '.csproj') } else { $sourceRoot }
            $arguments = @{ ProjectPath = $modInput; OutputRoot = $modOutput; Configuration = $Configuration; AssetRoot = $modAssets; NoZip = $true }
            if ($SkipBuild -and $definition.Project) { $arguments.BuildOutput = Join-Path $sourceRoot "bin/$Configuration/netstandard2.0" }
            & "$PSScriptRoot/build-mod-project.ps1" @arguments | Out-Host
        }
        if (-not $?) { throw "Mod package build failed: $($definition.UniqueID)" }
        Copy-Item -LiteralPath (Join-Path $modOutput 'package') -Destination (Join-Path $OutputRoot $definition.PackageName) -Recurse
    }

    foreach ($product in $selectedProducts) {
        $controlPath = Join-Path (Join-Path $OutputRoot $product.uploadFolder) 'workshop.json'
        if ($product.workshopId) {
            Write-Utf8NoBomJson -Path $controlPath -Value @{ workshop_id = [string]$product.workshopId }
        }
        else {
            $existingControl = Join-Path (Join-Path $publishOutputRoot $product.uploadFolder) 'workshop.json'
            if (Test-Path -LiteralPath $existingControl -PathType Leaf) { Copy-Item -LiteralPath $existingControl -Destination $controlPath }
        }
    }
    $publicationResult = Publish-DtmApiWorkshopPublicationSession -Session $publicationSession
    $publicationSucceeded = [bool]$publicationResult.Published
    Write-Host "Workshop packages built at $publishOutputRoot"
}
finally {
    if (-not $publicationSucceeded) { Restore-DtmApiWorkshopPublicationSessionAfterFailure -Session $publicationSession }
    if (Test-Path -LiteralPath $buildRoot) { Remove-Item -LiteralPath $buildRoot -Recurse -Force }
}
