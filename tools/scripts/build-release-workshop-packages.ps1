param(
    [string] $Configuration = 'Release',
    [string] $OutputRoot = '',
    [switch] $SkipBuild,
    [switch] $RuntimeOnly,
    [switch] $ModsOnly
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\workshop-packages'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

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

function Set-DtmApiVersionRequirements {
    param([Parameter(Mandatory = $true)] $Manifest)
    $Manifest.MinimumDTMApiVersion = $script:DtmApiReleaseVersion
    if ($Manifest.Dependencies) {
        foreach ($dependency in $Manifest.Dependencies) {
            if ($dependency.PSObject.Properties['MinimumVersion'] -and (
                $dependency.UniqueID -eq 'DTMAPI.ModConfigMenu' -or
                $dependency.UniqueID -eq 'DTMAPI.GameBridge.DolocTown' -or
                $dependency.UniqueID -eq 'DTMAPI.DebugConsoleHost')) {
                $dependency.MinimumVersion = $script:DtmApiReleaseVersion
            }
        }
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

if (-not $ModsOnly) {
    $runtimePackage = Join-Path $OutputRoot 'DTMAPI'
    Clear-Directory -Path $runtimePackage
    foreach ($bat in @('1_install_dtmapi.bat', '2_uninstall_dtmapi.bat', '3_check_dtmapi_status.bat')) {
        Copy-Item -LiteralPath (Join-Path $repo "tools\release\runtime-workshop\$bat") -Destination (Join-Path $runtimePackage $bat) -Force
    }
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePackage 'icon.png')
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-preview.png') -Destination (Join-Path $runtimePackage 'preview.png')

    $installerTools = Join-Path $runtimePackage 'Content\DTMAPIInstaller\tools'
    New-Item -ItemType Directory -Force -Path $installerTools | Out-Null
    foreach ($scriptName in @('common.ps1', 'release-common.ps1', 'install-to-game.ps1', 'install-bepinex.ps1', 'uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1')) {
        Copy-DtmApiTextFileUtf8Bom -Source (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $installerTools $scriptName)
    }
    Test-DtmApiWindowsPowerShellSyntax -Paths @(
        Get-ChildItem -LiteralPath $installerTools -Filter '*.ps1' -File | ForEach-Object { $_.FullName }
    )

    $runtimePayload = Join-Path $runtimePackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $outDir = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
    $runtimeFiles = @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Abstractions.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')
    Copy-DirectoryContents -Source $outDir -Destination $runtimePayload -Include $runtimeFiles
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePayload 'assets\branding\dtmapi-icon.png')

    $manifest = New-DtmApiReleaseManifest -RepoRoot $repo -PackageKind 'workshop-runtime' -IncludedAssemblies $runtimeFiles -BundledMods @()
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
    })
}

if (-not $RuntimeOnly) {
    foreach ($mod in Get-DtmApiPublishedModDefinitions) {
        $package = Join-Path $OutputRoot $mod.PackageName
        Clear-Directory -Path $package
        $source = Join-Path $repo "testmods\$($mod.Project)\bin\$Configuration\netstandard2.0"
        $manifestPath = Join-Path $source 'manifest.json'
        $dllPath = Join-Path $source $mod.SourceDll
        if (-not (Test-Path $manifestPath) -or -not (Test-Path $dllPath)) {
            throw "Built output missing for $($mod.Project). Expected $manifestPath and $dllPath."
        }

        $contentRoot = Join-Path $package 'Content\DTMAPI'
        New-Item -ItemType Directory -Force -Path $contentRoot | Out-Null
        Copy-Item -LiteralPath $dllPath -Destination (Join-Path $contentRoot $mod.PackageDll) -Force
        $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
        $manifest.Author = 'Yuuka'
        $manifest.EntryDll = "Content/DTMAPI/$($mod.PackageDll)"
        Set-DtmApiVersionRequirements -Manifest $manifest
        Write-Utf8NoBomJson -Path (Join-Path $contentRoot 'manifest.json') -Value $manifest

        $sourceI18n = Join-Path $repo "testmods\$($mod.Project)\i18n"
        if (Test-Path $sourceI18n) {
            Copy-DirectoryContents -Source (Split-Path -Parent $sourceI18n) -Destination $package -Include @('i18n')
        }
        $sourceContent = Join-Path $repo "testmods\$($mod.Project)\Content"
        if (Test-Path $sourceContent) {
            Copy-DirectoryContents -Source (Split-Path -Parent $sourceContent) -Destination $package -Include @('Content')
        }

        $infoPath = Join-Path $repo "testmods\$($mod.Project)\official-info.json"
        if (Test-Path $infoPath) {
            $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
            $info.author = 'Yuuka'
            if (-not $info.PSObject.Properties['version']) {
                $info | Add-Member -NotePropertyName version -NotePropertyValue $manifest.Version
            } elseif ([string]::IsNullOrWhiteSpace([string]$info.version)) {
                $info.version = $manifest.Version
            }
            Write-Utf8NoBomJson -Path (Join-Path $package 'info.json') -Value $info
        }
        $modSourceRoot = Join-Path $repo "testmods\$($mod.Project)"
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
}

Write-Host "Release Workshop staging packages written to $OutputRoot"
