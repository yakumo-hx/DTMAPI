param(
    [string]$GameDir = "D:\Steam\steamapps\common\Doloc Town",
    [switch]$SkipLocalInstall
)

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ProjectDir "..\..\.."))
$SourceDir = Join-Path $ProjectDir "src"
$OutDir = Join-Path $ProjectDir "dist"
$ReleaseDir = Join-Path $ProjectDir "release"
$PackageDir = Join-Path $RepoRoot "packages\workshop\WorkshopPackages\DLK_AnimalHusbandryProgress"
$PackageSmapiDir = Join-Path $PackageDir "Content\DolocSMAPI"
$PackagePluginDir = Join-Path $PackageSmapiDir "plugins"
$LocalModsDir = Join-Path $env:USERPROFILE "AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_AnimalHusbandryProgress"
$LocalSmapiDir = Join-Path $LocalModsDir "Content\DolocSMAPI"
$LocalModsPluginDir = Join-Path $LocalSmapiDir "plugins"
$SdkDll = Join-Path $RepoRoot "src\smapi\DolocTownSMAPI.SDK\dist\DolocTownSMAPI.SDK.dll"
$ManagedDir = Join-Path $GameDir "DolocTown_Data\Managed"
$Csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

function ConvertFrom-UnicodeEscape([string]$Text) {
    return [regex]::Replace($Text, "\\u([0-9a-fA-F]{4})", {
        param($match)
        return [char][Convert]::ToInt32($match.Groups[1].Value, 16)
    })
}

$CoverRoot = "D:\" + (ConvertFrom-UnicodeEscape "\u56fe\u7247")
$CoverImage = Join-Path (Join-Path $CoverRoot "Screenshots") (ConvertFrom-UnicodeEscape "\u5c4f\u5e55\u622a\u56fe 2026-05-21 023748.png")
$ModName = ConvertFrom-UnicodeEscape "\u7267\u94c3\u663e\u793a\u9690\u85cf\u4ea7\u7269\u8fdb\u5ea6"
$ModNameTraditional = ConvertFrom-UnicodeEscape "\u7267\u9234\u986f\u793a\u96b1\u85cf\u7522\u7269\u9032\u5ea6"
$DescriptionSimplified = ConvertFrom-UnicodeEscape "\u5728\u7267\u94c3\u754c\u9762\u663e\u793a\u5f53\u524d\u623f\u95f4\u5185\u52a8\u7269\u7684\u7279\u6b8a\u4ea7\u7269\u8fdb\u5ea6\u3002\u9700\u8981\u5148\u5b89\u88c5 DolocTown SMAPI Runtime 0.2.1 / API 0.8.22\u3002"
$DescriptionTraditional = ConvertFrom-UnicodeEscape "\u5728\u7267\u9234\u4ecb\u9762\u986f\u793a\u76ee\u524d\u623f\u9593\u5167\u52d5\u7269\u7684\u7279\u6b8a\u7522\u7269\u9032\u5ea6\u3002\u9700\u8981\u5148\u5b89\u88dd DolocTown SMAPI Runtime 0.2.1 / API 0.8.22\u3002"
$UniqueId = "Yuuka.AnimalHusbandryProgress"
$Version = "1.0.0"
$DllName = "DolocTownAnimalHusbandryProgress.dll"
$EntryType = "Dlk.DolocAnimalHusbandryProgress.AnimalHusbandryProgressMod"
$MinimumApiVersion = "0.8.22"

if (!(Test-Path -LiteralPath $Csc)) {
    throw "csc.exe was not found at $Csc"
}

$required = @(
    $SdkDll,
    (Join-Path $ManagedDir "netstandard.dll"),
    (Join-Path $ManagedDir "UnityEngine.dll"),
    (Join-Path $ManagedDir "UnityEngine.CoreModule.dll")
)

foreach ($path in $required) {
    if (!(Test-Path -LiteralPath $path)) {
        throw "Missing reference: $path"
    }
}
if (!(Test-Path -LiteralPath $CoverImage)) {
    throw "Missing cover image: $CoverImage"
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
New-Item -ItemType Directory -Force -Path $PackagePluginDir | Out-Null
New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null
if (!$SkipLocalInstall) {
    New-Item -ItemType Directory -Force -Path $LocalModsPluginDir | Out-Null
}

$outDll = Join-Path $OutDir $DllName
$sourceFiles = Get-ChildItem -LiteralPath $SourceDir -Filter "*.cs" -File | ForEach-Object { $_.FullName }
$referenceArgs = $required | ForEach-Object { "/reference:$_" }

& $Csc /nologo /target:library /optimize+ "/out:$outDll" $referenceArgs $sourceFiles
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

$info = [ordered]@{
    name = "DLK_AnimalHusbandryProgress"
    author = "Yuuka"
    version = $Version
    description = $DescriptionSimplified
    tags = @("Mod", "Gameplay", "Functional", "Chinese")
    priority = 0
    localized_name = [ordered]@{
        schinese = $ModName
        tchinese = $ModNameTraditional
        english = "Animal Bell Hidden Produce Progress"
    }
    localized_description = [ordered]@{
        schinese = $DescriptionSimplified
        tchinese = $DescriptionTraditional
        english = "Shows hidden special-produce progress for animals in the current room in the animal bell viewer. Requires DolocTown SMAPI Runtime 0.2.1 / API 0.8.22."
    }
}

$manifest = [ordered]@{
    Name = $ModName
    Author = "Yuuka"
    Version = $Version
    Description = $DescriptionSimplified
    UniqueID = $UniqueId
    MinimumApiVersion = $MinimumApiVersion
    GameVersion = ""
    EntryDll = "plugins/$DllName"
    EntryType = $EntryType
    Kind = "Code"
    RequiresRestartOnDisable = $false
    Dependencies = @()
    IncompatibleWith = @()
    UpdateKeys = @()
}

$info | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PackageDir "info.json") -Encoding UTF8
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PackageSmapiDir "manifest.json") -Encoding UTF8
Copy-Item -LiteralPath $outDll -Destination (Join-Path $PackagePluginDir $DllName) -Force
Copy-Item -LiteralPath (Join-Path $ProjectDir "README.md") -Destination (Join-Path $PackageDir "README.md") -Force
Copy-Item -LiteralPath $CoverImage -Destination (Join-Path $PackageDir "icon.png") -Force
Copy-Item -LiteralPath $CoverImage -Destination (Join-Path $PackageDir "preview.png") -Force

if (!$SkipLocalInstall) {
    $info | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $LocalModsDir "info.json") -Encoding UTF8
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $LocalSmapiDir "manifest.json") -Encoding UTF8
    Copy-Item -LiteralPath $outDll -Destination (Join-Path $LocalModsPluginDir $DllName) -Force
    Copy-Item -LiteralPath $CoverImage -Destination (Join-Path $LocalModsDir "icon.png") -Force
    Copy-Item -LiteralPath $CoverImage -Destination (Join-Path $LocalModsDir "preview.png") -Force
}

$zipPath = Join-Path $ReleaseDir "DolocTownAnimalHusbandryProgress-SMAPI-$Version.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $PackageDir "*") -DestinationPath $zipPath -Force

Write-Host "Built: $outDll"
Write-Host "Packaged: $(Join-Path $PackagePluginDir $DllName)"
if (!$SkipLocalInstall) {
    Write-Host "Installed for local MODS test: $(Join-Path $LocalModsPluginDir $DllName)"
}
Write-Host "Release zip: $zipPath"
