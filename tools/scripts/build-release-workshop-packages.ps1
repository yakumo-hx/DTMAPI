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
    if (Test-Path $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
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

if (-not $ModsOnly) {
    $runtimePackage = Join-Path $OutputRoot 'DTMAPI_Runtime'
    Clear-Directory -Path $runtimePackage
    foreach ($bat in @('1_install_dtmapi.bat', '2_uninstall_dtmapi.bat', '3_check_dtmapi_status.bat')) {
        Copy-Item -LiteralPath (Join-Path $repo "tools\release\runtime-workshop\$bat") -Destination (Join-Path $runtimePackage $bat) -Force
    }

    $installerTools = Join-Path $runtimePackage 'Content\DTMAPIInstaller\tools'
    New-Item -ItemType Directory -Force -Path $installerTools | Out-Null
    foreach ($scriptName in @('common.ps1', 'release-common.ps1', 'install-to-game.ps1', 'install-bepinex.ps1', 'uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $installerTools $scriptName) -Force
    }

    $runtimePayload = Join-Path $runtimePackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $outDir = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
    $runtimeFiles = @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Abstractions.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')
    Copy-DirectoryContents -Source $outDir -Destination $runtimePayload -Include $runtimeFiles
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePayload 'assets\branding\dtmapi-icon.png')

    $manifest = New-DtmApiReleaseManifest -RepoRoot $repo -PackageKind 'workshop-runtime' -IncludedAssemblies $runtimeFiles -BundledMods @()
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'Content\DTMAPI\release-manifest.json') -Value $manifest
    $runtimeDescription = [char]0x65B0 + [char]0x589E + ' ' + 'E' + 'x' + 'p' + 'e' + 'r' + 'i' + 'm' + 'e' + 'n' + 't' + 'a' + 'l' + ' ' + 'C' + 'r' + 'o' + 'p' + 's' + ' ' + '/' + ' ' + 'H' + 'a' + 'r' + 'v' + 'e' + 's' + 't' + 'i' + 'n' + 'g' + ' ' + 'A' + 'P' + 'I' + [char]0xFF0C + [char]0x4F9B + ' ' + 'M' + 'o' + 'd' + ' ' + [char]0x4F5C + [char]0x8005 + [char]0x5236 + [char]0x4F5C + [char]0x4F5C + [char]0x7269 + [char]0x76C6 + [char]0x81EA + [char]0x52A8 + [char]0x6536 + [char]0x83B7 + [char]0x7C7B + ' ' + 'M' + 'o' + 'd' + [char]0xFF1B + [char]0x6682 + [char]0x65F6 + [char]0x672A + [char]0x5B9E + [char]0x73B0 + [char]0x4E54 + [char]0x6728 + [char]0x76C6 + [char]0x81EA + [char]0x52A8 + [char]0x6536 + [char]0x83B7 + [char]0x3002
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'info.json') -Value ([ordered]@{
        name = 'DTMAPI Runtime'
        author = 'Yuuka'
        version = $script:DtmApiReleaseVersion
        description = $runtimeDescription
        tags = @('Mod', 'Framework', 'DTMAPI', 'Chinese')
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
            $info.version = $manifest.Version
            Write-Utf8NoBomJson -Path (Join-Path $package 'info.json') -Value $info
        }
        Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $package 'icon.png')
        Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-preview.png') -Destination (Join-Path $package 'preview.png')
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
