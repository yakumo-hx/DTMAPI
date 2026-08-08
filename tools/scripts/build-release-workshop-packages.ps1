param(
    [string] $Configuration = 'Release',
    [string] $OutputRoot = '',
    [switch] $SkipBuild,
    [switch] $RuntimeOnly,
    [switch] $ModsOnly,
    [switch] $PlanOnly,
    [switch] $LibraryOnly
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
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
if ($RuntimeOnly -and $ModsOnly) {
    throw '-RuntimeOnly and -ModsOnly are mutually exclusive.'
}
if ($PlanOnly -and $RuntimeOnly) {
    throw '-PlanOnly describes the nine-product mutation route and cannot be combined with -RuntimeOnly.'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\workshop-packages'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

$releaseCatalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$releaseCatalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseCatalogPath | ConvertFrom-Json
$releaseProducts = @(Get-DtmApiReleaseContractAdvancedProducts -Catalog $releaseCatalog)
$publishedDefinitions = @(Get-DtmApiPublishedModDefinitions)
$releaseMods = New-Object 'System.Collections.Generic.List[object]'
foreach ($releaseProduct in $releaseProducts) {
    $releaseCatalogId = [string](Get-DtmApiMapValue -Map $releaseProduct -Key 'catalogId' -Default '')
    $matches = @($publishedDefinitions | Where-Object {
        [string](Get-DtmApiMapValue -Map $_ -Key 'AuthorSdkCatalogId' -Default '') -ceq $releaseCatalogId
    })
    if ($matches.Count -ne 1 -or
        -not [bool](Get-DtmApiMapValue -Map $matches[0] -Key 'AuthorSdkProject' -Default $false)) {
        throw "0.6 release product '$releaseCatalogId' must map to exactly one tracked Author SDK Workshop definition."
    }
    $releaseMods.Add($matches[0]) | Out-Null
}
if ($releaseMods.Count -ne 9) {
    throw "The 0.6 Workshop staging route must contain exactly nine current ProductNative updates; found $($releaseMods.Count)."
}
$forbiddenBuilderCatalogIds = @('more-equipment-slots', 'strong-planting-gun', 'mine')
$forbiddenBuilderTargets = @($releaseMods.ToArray() | Where-Object {
    $forbiddenBuilderCatalogIds -ccontains [string](Get-DtmApiMapValue -Map $_ -Key 'AuthorSdkCatalogId' -Default '')
})
if ($forbiddenBuilderTargets.Count -gt 0) {
    throw "The 0.6 Workshop staging route must never invoke the Advanced builder for an excluded product."
}
if ($PlanOnly) {
    foreach ($releaseMod in $releaseMods.ToArray()) {
        [pscustomobject]@{
            CatalogId = [string](Get-DtmApiMapValue -Map $releaseMod -Key 'AuthorSdkCatalogId' -Default '')
            PackageName = [string](Get-DtmApiMapValue -Map $releaseMod -Key 'PackageName' -Default '')
            BuildScript = [string](Get-DtmApiMapValue -Map $releaseMod -Key 'AuthorSdkBuildScript' -Default '')
        }
    }
    return
}

if (-not $SkipBuild) {
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
}

function Clear-Directory {
    param([Parameter(Mandatory = $true)] [string] $Path)
    $preservedWorkshopInfo = $null
    if (Test-Path $Path) {
        $workshopInfoPath = Join-Path $Path 'workshop.json'
        if (Test-Path -LiteralPath $workshopInfoPath -PathType Leaf) {
            $preservedWorkshopInfo = [System.IO.File]::ReadAllBytes($workshopInfoPath)
        }
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    if ($null -ne $preservedWorkshopInfo) {
        [System.IO.File]::WriteAllBytes((Join-Path $Path 'workshop.json'), $preservedWorkshopInfo)
    }
}

function Copy-IfExists {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )
    if (Test-Path $Source) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
    }
}

function Get-DtmApiPublishMetadata {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $UniqueId
    )

    $metadataPath = Join-Path $RepoRoot 'tools\release\dtmapi-mod-publish-zh.json'
    if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
        throw "Missing DTMAPI publish metadata file: $metadataPath"
    }

    $metadata = Get-Content -Raw -Encoding UTF8 -LiteralPath $metadataPath | ConvertFrom-Json
    $entry = @($metadata.mods | Where-Object { $_.uniqueId -eq $UniqueId } | Select-Object -First 1)
    if ($entry.Count -ne 1) {
        throw "DTMAPI publish metadata does not contain exactly one entry for UniqueID '$UniqueId'."
    }

    return $entry[0]
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

function Assert-PlayerPackageExcludesQaHost {
    param(
        [Parameter(Mandatory = $true)] [string] $PackageRoot
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($PackageRoot).TrimEnd('\')
    $forbidden = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force | Where-Object {
        $relative = $_.FullName.Substring($resolvedRoot.Length).TrimStart('\').Replace('\', '/')
        $name = $_.Name
        $relative -match '(^|/)qa-host(/|$)' -or
        $name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
        $name -match '^(?i:DTMAPI\.(Smoke|Tests)\.(dll|pdb))$' -or
        $name -match '^(?i:qa-settings\.json|smoke-settings\.json|qa-host.*\.json)$'
    })
    if ($forbidden.Count -gt 0) {
        $relativeForbidden = @($forbidden | ForEach-Object {
            $_.FullName.Substring($resolvedRoot.Length).TrimStart('\').Replace('\', '/')
        } | Sort-Object -Unique)
        throw "Player Runtime package contains developer-only QA host material: $([string]::Join(', ', $relativeForbidden))"
    }
}

function Get-DtmApiWorkshopRuntimeAssemblyReceipts {
    param(
        [Parameter(Mandatory = $true)] [string] $PayloadRoot,
        [Parameter(Mandatory = $true)] [string[]] $FileNames
    )

    $actualDllNames = @(Get-ChildItem -LiteralPath $PayloadRoot -Filter '*.dll' -File |
        ForEach-Object { $_.Name } |
        Sort-Object)
    $expectedDllNames = @($FileNames | Sort-Object)
    if (($actualDllNames -join '|') -ne ($expectedDllNames -join '|')) {
        throw "Workshop Runtime payload DLL set is not exact. Expected=$($expectedDllNames -join ',') Actual=$($actualDllNames -join ',')"
    }

    $receipts = New-Object 'System.Collections.Generic.List[object]'
    foreach ($fileName in $FileNames) {
        $path = Join-Path $PayloadRoot $fileName
        $item = Get-Item -LiteralPath $path -ErrorAction Stop
        if ($item.Length -le 0) {
            throw "Workshop Runtime payload assembly is empty: $path"
        }

        try {
            $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($path)
            $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($path))
            $fileVersionAttributes = @($assembly.GetCustomAttributesData() | Where-Object {
                [string]$_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute'
            })
        }
        catch {
            throw "Workshop Runtime payload assembly metadata is unreadable: $path. $($_.Exception.Message)"
        }

        $expectedAssemblyName = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
        if (-not [string]::Equals([string]$assemblyName.Name, $expectedAssemblyName, [System.StringComparison]::Ordinal)) {
            throw "Workshop Runtime payload assembly identity mismatch for $fileName. Expected=$expectedAssemblyName Actual=$($assemblyName.Name)"
        }
        if ($fileVersionAttributes.Count -ne 1 -or $fileVersionAttributes[0].ConstructorArguments.Count -ne 1) {
            throw "Workshop Runtime payload assembly does not contain exactly one AssemblyFileVersionAttribute: $path"
        }
        $fileVersion = [string]$fileVersionAttributes[0].ConstructorArguments[0].Value
        if (-not [string]::Equals($fileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
            throw "Workshop Runtime payload assembly version mismatch for $fileName. Expected=$script:DtmApiBinaryVersion Actual=$fileVersion"
        }

        $receipts.Add([ordered]@{
            FileName = $fileName
            Length = [long]$item.Length
            Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
            FileVersion = $fileVersion
        }) | Out-Null
    }

    return $receipts.ToArray()
}

function Get-DtmApiWorkshopOptionalComponentReceipts {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $Configuration,
        [Parameter(Mandatory = $true)] [string] $PayloadRoot
    )

    $catalogPath = Join-Path $RepoRoot 'tools\release\dtmapi-product-catalog.json'
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $invariant = Get-DtmApiMapValue -Map $catalog -Key 'playerRuntimePackageInvariant' -Default $null
    $definitions = @((Get-DtmApiMapValue -Map $invariant -Key 'optionalComponents' -Default @()))
    $receipts = New-Object 'System.Collections.Generic.List[object]'
    $seenIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    $seenPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($definition in $definitions) {
        $componentId = [string](Get-DtmApiMapValue -Map $definition -Key 'id' -Default '')
        $distribution = [string](Get-DtmApiMapValue -Map $definition -Key 'distribution' -Default '')
        $loadPolicy = [string](Get-DtmApiMapValue -Map $definition -Key 'loadPolicy' -Default '')
        $relativePath = ([string](Get-DtmApiMapValue -Map $definition -Key 'relativePath' -Default '')).Replace('\', '/')
        $expectedAssemblyName = [string](Get-DtmApiMapValue -Map $definition -Key 'assemblyName' -Default '')
        $expectedAssemblyVersion = [string](Get-DtmApiMapValue -Map $definition -Key 'assemblyVersion' -Default '')
        $sourceProject = ([string](Get-DtmApiMapValue -Map $definition -Key 'sourceProject' -Default '')).Replace('/', '\')
        $targetFramework = [string](Get-DtmApiMapValue -Map $definition -Key 'targetFramework' -Default '')
        if ([string]::IsNullOrWhiteSpace($componentId) -or -not $seenIds.Add($componentId)) {
            throw "Player Runtime optional component IDs must be non-empty and unique: '$componentId'."
        }
        if ([string]::IsNullOrWhiteSpace($relativePath) -or -not $seenPaths.Add($relativePath) -or
            -not $relativePath.StartsWith('DTMAPI/components/', [System.StringComparison]::Ordinal) -or
            $relativePath.Contains('../') -or $relativePath.Contains('/..') -or
            $relativePath.StartsWith('BepInEx/', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Player Runtime optional component path is unsafe or duplicated: '$relativePath'."
        }
        if (-not [string]::Equals($distribution, 'dormant-shipped', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($targetFramework, 'netstandard2.0', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($expectedAssemblyVersion, $script:DtmApiAssemblyCompatibilityVersion, [System.StringComparison]::Ordinal)) {
            throw "Optional component '$componentId' must remain dormant-shipped and netstandard2.0."
        }

        $sourceProjectPath = Join-Path $RepoRoot $sourceProject
        if (-not (Test-Path -LiteralPath $sourceProjectPath -PathType Leaf)) {
            throw "Optional component source project is missing: $sourceProjectPath"
        }
        $fileName = [System.IO.Path]::GetFileName($relativePath)
        $sourcePath = Join-Path (Split-Path -Parent $sourceProjectPath) ("bin\{0}\{1}\{2}" -f $Configuration, $targetFramework, $fileName)
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Optional component build output is missing: $sourcePath"
        }
        $destinationPath = Join-Path $PayloadRoot ($relativePath.Replace('/', '\'))
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destinationPath) | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force

        $item = Get-Item -LiteralPath $destinationPath -ErrorAction Stop
        if ($item.Length -le 0) {
            throw "Optional component payload is empty: $destinationPath"
        }
        try {
            $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($destinationPath)
            $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($destinationPath))
            $fileVersionAttributes = @($assembly.GetCustomAttributesData() | Where-Object {
                [string]$_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute'
            })
            $targetFrameworkAttributes = @($assembly.GetCustomAttributesData() | Where-Object {
                [string]$_.AttributeType.FullName -eq 'System.Runtime.Versioning.TargetFrameworkAttribute'
            })
        }
        catch {
            throw "Optional component metadata is unreadable: $destinationPath. $($_.Exception.Message)"
        }
        if (-not [string]::Equals([string]$assemblyName.Name, $expectedAssemblyName, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$assemblyName.Version, $expectedAssemblyVersion, [System.StringComparison]::Ordinal)) {
            throw "Optional component assembly identity mismatch for '$componentId'. Expected=$expectedAssemblyName/$expectedAssemblyVersion Actual=$($assemblyName.Name)/$($assemblyName.Version)"
        }
        if ($fileVersionAttributes.Count -ne 1 -or $fileVersionAttributes[0].ConstructorArguments.Count -ne 1 -or
            -not [string]::Equals([string]$fileVersionAttributes[0].ConstructorArguments[0].Value, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
            throw "Optional component FileVersion mismatch for '$componentId'."
        }
        if ($targetFrameworkAttributes.Count -ne 1 -or $targetFrameworkAttributes[0].ConstructorArguments.Count -ne 1 -or
            -not [string]::Equals([string]$targetFrameworkAttributes[0].ConstructorArguments[0].Value, '.NETStandard,Version=v2.0', [System.StringComparison]::Ordinal)) {
            throw "Optional component target framework mismatch for '$componentId'."
        }

        $receipts.Add([ordered]@{
            ComponentId = $componentId
            Distribution = $distribution
            LoadPolicy = $loadPolicy
            RelativePath = $relativePath
            Length = [long]$item.Length
            Sha256 = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash.ToLowerInvariant()
            AssemblyName = [string]$assemblyName.Name
            AssemblyVersion = [string]$assemblyName.Version
            FileVersion = [string]$fileVersionAttributes[0].ConstructorArguments[0].Value
            TargetFramework = $targetFramework
            DefaultLoadState = [string](Get-DtmApiMapValue -Map $definition -Key 'defaultLoadState' -Default '')
            IncludedInDownloadPackage = [bool](Get-DtmApiMapValue -Map $definition -Key 'includedInDownloadPackage' -Default $false)
        }) | Out-Null
    }
    return $receipts.ToArray()
}

function Resolve-DtmApiWorkshopModSourceRoot {
    param(
        $Mod
    )

    $sourceRoot = Get-DtmApiMapValue -Map $Mod -Key 'SourceRoot' -Default ''
    if (-not [string]::IsNullOrWhiteSpace([string]$sourceRoot)) {
        return Join-Path $repo ([string]$sourceRoot)
    }
    throw "Release definition '$($Mod.UniqueID)' must declare SourceRoot after C1 physical classification."
}

$publishOutputRoot = $OutputRoot
$publicationSession = New-DtmApiWorkshopPublicationSession -FinalRoot $publishOutputRoot
$OutputRoot = [string]$publicationSession.StagingRoot
$publicationSucceeded = $false
try {
    if ($ModsOnly -and (Test-Path -LiteralPath (Join-Path $publishOutputRoot 'DTMAPI') -PathType Container)) {
        $existingRuntimePackage = Join-Path $publishOutputRoot 'DTMAPI'
        $null = Assert-DtmApiWorkshopOrdinaryTree -Path $existingRuntimePackage -Context 'Existing Runtime package preserved by ModsOnly publication'
        $existingRuntimeItems = @(Get-ChildItem -LiteralPath $existingRuntimePackage -Force -ErrorAction Stop | ForEach-Object { [string]$_.Name })
        Copy-DirectoryContents -Source $existingRuntimePackage -Destination (Join-Path $OutputRoot 'DTMAPI') -Include $existingRuntimeItems -Required
    }
    $metadataPackageNames = if ($RuntimeOnly) {
        @('DTMAPI')
    }
    elseif ($ModsOnly) {
        @($releaseMods.ToArray() | ForEach-Object { [string]$_.PackageName })
    }
    else {
        @('DTMAPI') + @($releaseMods.ToArray() | ForEach-Object { [string]$_.PackageName })
    }
    foreach ($metadataPackageName in @($metadataPackageNames)) {
        $existingWorkshopJson = Join-Path (Join-Path $publishOutputRoot $metadataPackageName) 'workshop.json'
        if (Test-Path -LiteralPath $existingWorkshopJson -PathType Leaf) {
            $stagedWorkshopJson = Join-Path (Join-Path $OutputRoot $metadataPackageName) 'workshop.json'
            [System.IO.Directory]::CreateDirectory((Split-Path -Parent $stagedWorkshopJson)) | Out-Null
            [System.IO.File]::Copy($existingWorkshopJson, $stagedWorkshopJson, $false)
        }
    }

if (-not $ModsOnly) {
    $runtimePackage = Join-Path $OutputRoot 'DTMAPI'
    Clear-Directory -Path $runtimePackage
    foreach ($bat in @('1_install_dtmapi.bat', '2_uninstall_dtmapi.bat', '3_check_dtmapi_status.bat', '4_collect_dtmapi_logs.bat')) {
        $batDestination = Join-Path $runtimePackage $bat
        Copy-DtmApiCmdFile -Source (Join-Path $repo "tools\release\runtime-workshop\$bat") -Destination $batDestination
    }
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePackage 'icon.png')
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-preview.png') -Destination (Join-Path $runtimePackage 'preview.png')

    $installerTools = Join-Path $runtimePackage 'Content\DTMAPIInstaller\tools'
    New-Item -ItemType Directory -Force -Path $installerTools | Out-Null
    Copy-DtmApiCmdFile `
        -Source (Join-Path $repo 'tools\release\runtime-workshop\invoke-dtmapi-action.cmd') `
        -Destination (Join-Path $installerTools 'invoke-dtmapi-action.cmd')
    Copy-DtmApiTextFileUtf8Bom `
        -Source (Join-Path $repo 'tools\release\dtmapi-runtime-version.props') `
        -Destination (Join-Path $installerTools 'dtmapi-runtime-version.props')
    $playerInstallerScriptNames = @(
        'common.ps1',
        'release-common.ps1',
        'probe-powershell-host.ps1',
        'install-to-game.ps1',
        'install-bepinex.ps1',
        'uninstall-dtmapi.ps1',
        'check-dtmapi-status.ps1',
        'collect-logs.ps1',
        'analyze-startup-evidence.ps1'
    )
    foreach ($scriptName in $playerInstallerScriptNames) {
        Copy-DtmApiTextFileUtf8Bom -Source (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $installerTools $scriptName)
    }
    Test-DtmApiWindowsPowerShellSyntax -Paths @(
        $playerInstallerScriptNames | ForEach-Object { Join-Path $installerTools $_ }
    )

    $bepInExPackage = Join-Path $repo 'tools\release\bootstrap\BepInEx_win_x64_5.4.23.5.zip'
    if (-not (Test-Path -LiteralPath $bepInExPackage -PathType Leaf)) {
        throw "Missing bundled BepInEx package: $bepInExPackage"
    }
    $bepInExPackageDestination = Join-Path $runtimePackage 'Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $bepInExPackageDestination) | Out-Null
    Copy-Item -LiteralPath $bepInExPackage -Destination $bepInExPackageDestination -Force

    $packagedExecutables = @(Get-ChildItem -LiteralPath $runtimePackage -Filter '*.exe' -File -Recurse -Force)
    if ($packagedExecutables.Count -ne 0) {
        throw "The normal Runtime Workshop package must contain zero executable files: $([string]::Join(', ', @($packagedExecutables | ForEach-Object { $_.FullName })))"
    }

    $runtimePayload = Join-Path $runtimePackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $outDir = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
    $runtimeFiles = @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Abstractions.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')
    $runtimeSourcePaths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($path in @(
        'Directory.Build.props',
        'src/DTMAPI.BepInExBootstrap',
        'src/DTMAPI.Abstractions',
        'src/DTMAPI.Core',
        'src/DTMAPI.GameBridge.DolocTown',
        'src/DTMAPI.ModConfigMenu'
    )) {
        $runtimeSourcePaths.Add($path) | Out-Null
    }
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $runtimeInvariant = Get-DtmApiMapValue -Map $catalog -Key 'playerRuntimePackageInvariant' -Default $null
    foreach ($component in @((Get-DtmApiMapValue -Map $runtimeInvariant -Key 'optionalComponents' -Default @()))) {
        $sourceProject = [string](Get-DtmApiMapValue -Map $component -Key 'sourceProject' -Default '')
        if (-not [string]::IsNullOrWhiteSpace($sourceProject)) {
            $runtimeSourcePaths.Add((Split-Path -Parent $sourceProject).Replace('\', '/')) | Out-Null
        }
    }
    $runtimeSourceStatus = @(& git -C $repo status --porcelain --untracked-files=all -- @($runtimeSourcePaths.ToArray()))
    if ($LASTEXITCODE -ne 0) {
        throw 'Workshop Runtime package could not verify the mandatory Runtime source worktree.'
    }
    if ($runtimeSourceStatus.Count -ne 0) {
        throw "Workshop Runtime package requires committed mandatory Runtime source. Dirty paths:`n$([string]::Join([Environment]::NewLine, $runtimeSourceStatus))"
    }
    Copy-DirectoryContents -Source $outDir -Destination $runtimePayload -Include $runtimeFiles
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePayload 'assets\branding\dtmapi-icon.png')

    $buildCommit = [string](Get-DtmApiSourceCommit -RepoRoot $repo)
    if ([string]::IsNullOrWhiteSpace($buildCommit) -or $buildCommit.Trim() -notmatch '^[0-9a-fA-F]{7,64}$') {
        throw "Workshop Runtime package requires a valid Git BuildCommit; actual='$buildCommit'."
    }
    $runtimeAssemblyReceipts = @(Get-DtmApiWorkshopRuntimeAssemblyReceipts -PayloadRoot $runtimePayload -FileNames $runtimeFiles)
    $optionalComponentReceipts = @(Get-DtmApiWorkshopOptionalComponentReceipts `
        -RepoRoot $repo `
        -Configuration $Configuration `
        -PayloadRoot (Join-Path $runtimePackage 'Content\DTMAPIInstaller\Payload'))
    $manifest = New-DtmApiReleaseManifest `
        -RepoRoot $repo `
        -PackageKind 'workshop-runtime' `
        -IncludedAssemblies $runtimeAssemblyReceipts `
        -OptionalComponents $optionalComponentReceipts `
        -BundledMods @() `
        -BuildCommit $buildCommit.Trim()
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'Content\DTMAPI\release-manifest.json') -Value $manifest
    $runtimeMetadata = Get-DtmApiPublishMetadata -RepoRoot $repo -UniqueId 'DTMAPI.Runtime'
    $runtimeDescription = [string]$runtimeMetadata.steamDescription
    if ([string]::IsNullOrWhiteSpace($runtimeDescription)) {
        $runtimeDescription = [string]$runtimeMetadata.gameDescription
    }
    $runtimeLocalizedDescription = [ordered]@{
        schinese = $runtimeDescription
        tchinese = $runtimeDescription
        english = $runtimeDescription
    }
    foreach ($localizedPropertyName in @('localizedDescription', 'localized_description')) {
        if ($runtimeMetadata.PSObject.Properties[$localizedPropertyName]) {
            $localizedSource = $runtimeMetadata.$localizedPropertyName
            foreach ($language in @('schinese', 'tchinese', 'english')) {
                if ($localizedSource.PSObject.Properties[$language]) {
                    $localizedValue = [string]$localizedSource.$language
                    if (-not [string]::IsNullOrWhiteSpace($localizedValue)) {
                        $runtimeLocalizedDescription[$language] = $localizedValue
                    }
                }
            }
        }
    }
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'info.json') -Value ([ordered]@{
        name = 'DTMAPI'
        author = 'Yuuka'
        version = $script:DtmApiReleaseVersion
        description = $runtimeDescription
        steamDescription = $runtimeDescription
        tags = @('Mod', 'Framework', 'DTMAPI', 'Chinese')
        localized_description = $runtimeLocalizedDescription
        # Doloc Town's native ModManager normalizes every local info.json to
        # include these keys before upload. Emit the stable form directly so
        # the generated package and the Steam-delivered player payload remain
        # byte-identical.
        localized_name = [ordered]@{
            schinese = ''
            tchinese = ''
            english = ''
        }
    })
    Remove-DtmApiWorkshopDownloadMarkers -PackageRoot $runtimePackage
}

if (-not $RuntimeOnly) {
    $advancedReferenceFixtureBuilder = Join-Path $PSScriptRoot 'build-advanced-reference-game-fixture.ps1'
    $advancedReferenceFixtureSessionRoot = Join-Path $repo (
        'temp\release-workshop-reference-games-' + $PID + '-' + [Guid]::NewGuid().ToString('N'))
    $advancedReferenceGameRoots = @{}
    try {
        foreach ($excludedPackageName in @('DTMAPI-MoreEquipmentSlots', 'DTMAPI-ManboCardboardAudio', 'DTMAPI-StrongPlantingGun', 'DTMAPI-Mine')) {
            $excludedPackagePath = Join-Path $OutputRoot $excludedPackageName
            if (Test-Path -LiteralPath $excludedPackagePath) {
                throw "Excluded or retained product staging directory must be absent from the 0.6 mutation candidate: $excludedPackagePath"
            }
        }
        foreach ($mod in $releaseMods.ToArray()) {
            $package = Join-Path $OutputRoot $mod.PackageName
            Clear-Directory -Path $package
            $modSourceRoot = Resolve-DtmApiWorkshopModSourceRoot -Mod $mod
            $authorSdkProject = [bool](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkProject' -Default $false)
            if ($authorSdkProject) {
                $builderName = [string](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkBuildScript' -Default '')
                if ([string]::IsNullOrWhiteSpace($builderName)) {
                    throw "Author SDK product $($mod.UniqueID) is missing its tracked build script."
                }
                $authorPackageName = [string](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkPackageFile' -Default '')
                if ([string]::IsNullOrWhiteSpace($authorPackageName) -or [IO.Path]::GetFileName($authorPackageName) -cne $authorPackageName) {
                    throw "Author SDK product $($mod.UniqueID) is missing one safe tracked package filename."
                }
                $artifactKey = ([string]$mod.UniqueID).ToLowerInvariant() -replace '[^a-z0-9]+', '-'
                $authorOutput = Join-Path $repo ("temp\release-$artifactKey-author-sdk-package")
                $authorPackage = Join-Path $authorOutput $authorPackageName
                if (-not $SkipBuild) {
                    $builderCatalogId = [string](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkCatalogId' -Default '')
                    if ([string]::IsNullOrWhiteSpace($builderCatalogId)) {
                        throw "Author SDK product $($mod.UniqueID) is missing its Catalog-driven build id."
                    }
                    $catalogProductMatches = @($releaseProducts | Where-Object {
                        [string](Get-DtmApiMapValue -Map $_ -Key 'catalogId' -Default '') -ceq $builderCatalogId
                    })
                    if ($catalogProductMatches.Count -ne 1) {
                        throw "Author SDK product $($mod.UniqueID) does not resolve to one release-contract Catalog row."
                    }
                    $referencePolicyId = [string](Get-DtmApiMapValue -Map $catalogProductMatches[0] -Key 'referencePolicyId' -Default '')
                    if ([string]::IsNullOrWhiteSpace($referencePolicyId)) {
                        throw "Author SDK product $($mod.UniqueID) has no exact Advanced reference policy."
                    }
                    if (-not $advancedReferenceGameRoots.ContainsKey($referencePolicyId)) {
                        $policyFixtureRoot = Join-Path $advancedReferenceFixtureSessionRoot $referencePolicyId
                        & $advancedReferenceFixtureBuilder -PolicyId $referencePolicyId -OutputRoot $policyFixtureRoot
                        if (-not $?) {
                            throw "Exact Advanced reference fixture build failed for policy '$referencePolicyId'."
                        }
                        $referenceGameRoot = Join-Path $policyFixtureRoot 'steamapps\common\Doloc Town'
                        Assert-DtmApiDolocTownGamePath `
                            -Path $referenceGameRoot `
                            -Source "Workshop release policy '$referencePolicyId' fixture"
                        $advancedReferenceGameRoots[$referencePolicyId] = $referenceGameRoot
                    }
                    $productReferenceGameRoot = [string]$advancedReferenceGameRoots[$referencePolicyId]
                    & (Join-Path $PSScriptRoot $builderName) `
                        -CatalogId $builderCatalogId `
                        -GameDir $productReferenceGameRoot `
                        -Configuration $Configuration `
                        -OutputRoot $authorOutput
                    if (-not $?) {
                        throw "Author SDK package build failed for $($mod.UniqueID)."
                    }
                }
                if (-not (Test-Path -LiteralPath $authorPackage -PathType Leaf)) {
                    throw "Author SDK package is unavailable for $($mod.UniqueID): $authorPackage"
                }
                Expand-Archive -LiteralPath $authorPackage -DestinationPath $package -Force
                $manifestPath = Join-Path $package 'Content\DTMAPI\manifest.json'
                $infoPath = Join-Path $package 'info.json'
                $entryPath = Join-Path $package ('Content\DTMAPI\' + $mod.PackageDll)
                if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
                    -not (Test-Path -LiteralPath $infoPath -PathType Leaf) -or
                    -not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
                    throw "Author SDK package projection is incomplete for $($mod.UniqueID)."
                }
                $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
                $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
                Assert-DtmApiCurrentProductProjection `
                    -RepoRoot $repo `
                    -Definition $mod `
                    -Manifest $manifest `
                    -Info $info `
                    -Context "Author SDK Workshop package $($mod.UniqueID)"
            }
            else {
                $source = Join-Path $modSourceRoot "bin\$Configuration\netstandard2.0"
                $manifestPath = Join-Path $source 'manifest.json'
                $dllPath = Join-Path $source $mod.SourceDll
                if (-not (Test-Path $manifestPath) -or -not (Test-Path $dllPath)) {
                    throw "Built output missing for $($mod.Project). Expected $manifestPath and $dllPath."
                }

                $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
                $infoPath = Join-Path $modSourceRoot 'official-info.json'
                if (-not (Test-Path -LiteralPath $infoPath -PathType Leaf)) {
                    throw "Missing official-info.json for published product $($mod.UniqueID): $infoPath"
                }
                $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
                Assert-DtmApiCurrentProductProjection `
                    -RepoRoot $repo `
                    -Definition $mod `
                    -Manifest $manifest `
                    -Info $info `
                    -Context "Published Workshop package $($mod.UniqueID)"

                $contentRoot = Join-Path $package 'Content\DTMAPI'
                New-Item -ItemType Directory -Force -Path $contentRoot | Out-Null
                Copy-Item -LiteralPath $dllPath -Destination (Join-Path $contentRoot $mod.PackageDll) -Force
                $manifest.Author = 'Yuuka'
                $manifest.EntryDll = "Content/DTMAPI/$($mod.PackageDll)"
                Write-Utf8NoBomJson -Path (Join-Path $contentRoot 'manifest.json') -Value $manifest

                $sourceI18n = Join-Path $modSourceRoot 'i18n'
                if (Test-Path $sourceI18n) {
                    Copy-DirectoryContents -Source (Split-Path -Parent $sourceI18n) -Destination $package -Include @('i18n')
                }
                $sourceContent = Join-Path $modSourceRoot 'Content'
                if (Test-Path $sourceContent) {
                    Copy-DirectoryContents -Source (Split-Path -Parent $sourceContent) -Destination $package -Include @('Content')
                }

                $sourceAssets = Join-Path $source 'assets'
                if (Test-Path $sourceAssets) {
                    Copy-DirectoryContents -Source (Split-Path -Parent $sourceAssets) -Destination $contentRoot -Include @('assets')
                }

                $info.author = 'Yuuka'
                Write-Utf8NoBomJson -Path (Join-Path $package 'info.json') -Value $info
                $assetIcon = Join-Path $repo 'assets\branding\dtmapi-icon.png'
                $modIcon = Join-Path $modSourceRoot 'icon.png'
                if (Test-Path -LiteralPath $modIcon -PathType Leaf) {
                    $assetIcon = $modIcon
                }

                $assetPreview = Join-Path $repo 'assets\branding\dtmapi-preview.png'
                $modPreview = Join-Path $modSourceRoot 'preview.png'
                if (Test-Path -LiteralPath $modPreview -PathType Leaf) {
                    $assetPreview = $modPreview
                }

                Copy-IfExists -Source $assetIcon -Destination (Join-Path $package 'icon.png')
                Copy-IfExists -Source $assetPreview -Destination (Join-Path $package 'preview.png')
                Write-Utf8NoBomJson -Path (Join-Path $contentRoot 'dtmapi-package.json') -Value ([ordered]@{
                    owner = 'DTMAPI'
                    packageKind = 'workshop-mod'
                    uniqueId = $manifest.UniqueID
                    generatedBy = 'tools/scripts/build-release-workshop-packages.ps1'
                    updatedAt = (Get-Date).ToUniversalTime().ToString('o')
                })
            }
            Remove-DtmApiWorkshopDownloadMarkers -PackageRoot $package
        }
    }
    finally {
        if (Test-Path -LiteralPath $advancedReferenceFixtureSessionRoot) {
            $repoTempRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))
            $resolvedFixtureSessionRoot = [System.IO.Path]::GetFullPath($advancedReferenceFixtureSessionRoot)
            if (-not (Test-DtmApiPathIsSameOrChild -Child $resolvedFixtureSessionRoot -Parent $repoTempRoot) -or
                $resolvedFixtureSessionRoot.TrimEnd([char[]]@('\', '/')) -eq $repoTempRoot.TrimEnd([char[]]@('\', '/')) -or
                [System.IO.Path]::GetFileName($resolvedFixtureSessionRoot) -notlike 'release-workshop-reference-games-*') {
                throw "Workshop release reference fixture cleanup escaped its dedicated repository temp boundary: $resolvedFixtureSessionRoot"
            }
            Remove-Item -LiteralPath $advancedReferenceFixtureSessionRoot -Recurse -Force
        }
    }
}

$expectedStagingDirectories = if ($RuntimeOnly) {
    @('DTMAPI')
}
else {
    $names = @($releaseMods.ToArray() | ForEach-Object { [string]$_.PackageName })
    if (-not $ModsOnly -or (Test-Path -LiteralPath (Join-Path $OutputRoot 'DTMAPI') -PathType Container)) {
        $names += 'DTMAPI'
    }
    @($names)
}
$actualStagingDirectories = @(if (Test-Path -LiteralPath $OutputRoot -PathType Container) {
    Get-ChildItem -LiteralPath $OutputRoot -Directory -Force | ForEach-Object { $_.Name } | Sort-Object
})
$expectedStagingDirectories = @($expectedStagingDirectories | Sort-Object)
$stagingDifference = @(Compare-Object -ReferenceObject $expectedStagingDirectories -DifferenceObject $actualStagingDirectories)
if ($stagingDifference.Count -ne 0 -or $actualStagingDirectories.Count -ne $expectedStagingDirectories.Count) {
    throw ("Release Workshop staging root must contain only the exact current mutation candidate. Expected=[{0}] Actual=[{1}]" -f
        ($expectedStagingDirectories -join ', '), ($actualStagingDirectories -join ', '))
}

if (-not $ModsOnly) {
    Assert-PlayerPackageExcludesQaHost -PackageRoot (Join-Path $OutputRoot 'DTMAPI')
    & "$PSScriptRoot\check-product-catalog.ps1" -RuntimePackageRoot (Join-Path $OutputRoot 'DTMAPI')
    if (-not $?) { throw 'Built Runtime package failed the Catalog/player-payload gate.' }
}

$publicationResult = Publish-DtmApiWorkshopPublicationSession -Session $publicationSession
$publicationSucceeded = [bool]$publicationResult.Published
Write-Host "Release Workshop staging packages atomically published to $publishOutputRoot"
}
finally {
    if (-not $publicationSucceeded) {
        Restore-DtmApiWorkshopPublicationSessionAfterFailure -Session $publicationSession
    }
}
