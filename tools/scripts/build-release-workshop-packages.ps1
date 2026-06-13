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
    $runtimeDescriptionBase64 = 'RFRNQVBJIOaYr+Wkmua0m+WPr+Wwj+mVh+WKn+iDveaApyBNb2Qg55qE6L+Q6KGM5YmN572u44CCCuWug+S8muWuieijheW5tuWQr+WKqOaJgOmcgOeahOW6leWxgui/kOihjOe7hOS7tu+8jOaPkOS+m+mFjee9ruiPnOWNleOAgeaXpeW/l+iviuaWreOAgU1hbmFnZXIg54q25oCB6aG144CB5oql5ZGK5a+85Ye65ZKM5Yqf6IO95oCnIE1vZCDov5DooYzmlK/mjIHjgIIKCuacrOWMheacrOi6q+S4jeaYr+S4gOS4quWNleeLrOeOqeazlSBNb2TvvJvorqLpmIXlkI7or7fov5DooYwgMV9pbnN0YWxsX2R0bWFwaS5iYXQg5a6J6KOFIERUTUFQSSDov5DooYzliY3nva7jgIIK5LmL5ZCO5aSn6KeG6YeO44CB5Yqo5L2c5Yqg6YCf44CB5LiA6ZSu5a6M5oiQ44CB5pu05aSa5a2Y5qGj44CBWSDplK7mjqfliLblj7DnrYkgRFRNQVBJIE1vZCDmiY3og73ov5DooYzjgIIKCjAuNS4xLWFscGhhIOaUuei/m+S6huWuieijheWZqOWFvOWuueaAp+WSjOeKtuaAgeajgOafpei+k+WHuu+8muWuieijheiEmuacrOWFvOWuuSBXaW5kb3dzIFBvd2VyU2hlbGwgNS4x77yMM19jaGVja19kdG1hcGlfc3RhdHVzLmJhdCDkvJrnlKjmmI7mmL7nmoQgW09LXeOAgVtNSVNTSU5HXeOAgVtXQVJOXeOAgVtJTkZPXSDmmL7npLrov5DooYzml7bmlofku7bjgIFCZXBJbkV444CB5pel5b+X44CB5oql5ZGK5ZKM5pen54mIIERMSy9TTUFQSSDmo4DmtYvnirbmgIHjgIIKCuS4juaXpyBETEtzbWFwaSAvIERvbG9jVG93blNNQVBJIOeahOWMuuWIq++8mkRUTUFQSSDmmK/ku47pm7bph43lu7rnmoTmlrDniYjov5DooYzml7bvvIzkuI3lpI3liLbml6flrp7njrDjgIIK5a6J6KOF44CB5Y246L2944CB54q25oCB5qOA5p+l44CB5pel5b+X5a+85Ye65ZKMIE1vZCDlkK/lgZzot6/lvoTmm7TmuIXmmbDjgIIK5pmu6YCa5Yqf6IO9IE1vZCDpgJrov4cgQ29udGVudC9EVE1BUEkvbWFuaWZlc3QuanNvbiDliqDovb3vvIzov5DooYzliY3nva7lj6rlronoo4XliLAgQmVwSW5FeC9wbHVnaW5zL0RUTUFQSeOAggoK6L+Z5piv5byA5Y+R6ICF6aKE6KeI54mI77yM5L2G5Lya5oyB57ut57u05oqk44CCCuWQjue7reS8mue7p+e7reWujOWWhCBNYW5hZ2VyIFVJ44CB5YW85a655penIE1vZCDov4Hnp7vvvIzlubbkvJjlhYjkv53miqTlrZjmoaPkuI7njqnlrrbmnKzlnLAgTW9kIOWGheWuueOAgg=='
    $runtimeDescription = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($runtimeDescriptionBase64))
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'info.json') -Value ([ordered]@{
        name = 'DTMAPI'
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
