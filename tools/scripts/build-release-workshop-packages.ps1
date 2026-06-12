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
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $installerTools $scriptName) -Force
    }

    $runtimePayload = Join-Path $runtimePackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $outDir = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
    $runtimeFiles = @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Abstractions.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')
    Copy-DirectoryContents -Source $outDir -Destination $runtimePayload -Include $runtimeFiles
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePayload 'assets\branding\dtmapi-icon.png')

    $manifest = New-DtmApiReleaseManifest -RepoRoot $repo -PackageKind 'workshop-runtime' -IncludedAssemblies $runtimeFiles -BundledMods @()
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'Content\DTMAPI\release-manifest.json') -Value $manifest
    $runtimeDescriptionBase64 = 'RFRNQVBJIFJ1bnRpbWUg5piv5aSa5rSb5Y+v5bCP6ZWH5Yqf6IO95oCnIE1vZCDnmoTov5DooYzliY3nva7jgILlroPln7rkuo4gQmVwSW5FeCDlkK/liqjvvIzmj5DkvpsgRFRNQVBJIENvcmXjgIFHYW1lQnJpZGdl44CB6YWN572u6I+c5Y2V44CB5pel5b+X6K+K5pat44CBTWFuYWdlciDnirbmgIHpobXjgIHmiqXlkYrlr7zlh7rvvIzku6Xlj4rnu5nlip/og73mgKcgTW9kIOS9v+eUqOeahCBBUEnjgIIKCuacrOWMheacrOi6q+S4jeaYr+S4gOS4quWNleeLrOeOqeazlSBNb2TvvJvorqLpmIXlkI7or7fov5DooYwgMV9pbnN0YWxsX2R0bWFwaS5iYXQg5a6J6KOFIFJ1bnRpbWXjgILkuYvlkI7lpKfop4bph47jgIHliqjkvZzliqDpgJ/jgIHkuIDplK7lrozmiJDjgIHmm7TlpJrlrZjmoaPjgIFZIOmUruaOp+WItuWPsOetiSBEVE1BUEkgTW9kIOaJjeiDvei/kOihjOOAggoKMC41LjAtYWxwaGEg5paw5aKeIEV4cGVyaW1lbnRhbCBDcm9wcyAvIEhhcnZlc3RpbmcgQVBJ77yM5L6bIE1vZCDkvZzogIXliLbkvZzkvZzniannm4boh6rliqjmlLbojrfnsbsgTW9k77yb5pqC5pe25pyq5a6e546w5LmU5pyo55uG6Ieq5Yqo5pS26I6344CCCgrkuI7ml6cgRExLc21hcGkgLyBEb2xvY1Rvd25TTUFQSSDnmoTljLrliKvvvJpEVE1BUEkg5piv5LuO6Zu26YeN5bu655qE5paw54mI6L+Q6KGM5pe277yM5LiN5aSN5Yi25pen5a6e546w77yb5a6J6KOF44CB5Y246L2944CB54q25oCB5qOA5p+l44CB5pel5b+X5a+85Ye65ZKMIE1vZCDlkK/lgZzot6/lvoTmm7TmuIXmmbDvvJvmma7pgJrlip/og70gTW9kIOmAmui/hyBDb250ZW50L0RUTUFQSS9tYW5pZmVzdC5qc29uIOWKoOi9ve+8jFJ1bnRpbWUg5Y+q5a6J6KOF5YiwIEJlcEluRXgvcGx1Z2lucy9EVE1BUEnjgIIKCui/meaYryBEZXZlbG9wZXIgUHJldmlld++8jOS9huS8muaMgee7ree7tOaKpOOAguWQjue7reS8mue7p+e7reeos+WumiBBUEnjgIHlrozlloQgTWFuYWdlciBVSeOAgeWFvOWuueaXpyBNb2Qg6L+B56e777yM5bm25LyY5YWI5L+d5oqk5a2Y5qGj5LiO546p5a625pys5ZywIE1vZCDlhoXlrrnjgII='
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
