param(
    [string]$GameDir = "D:\Steam\steamapps\common\Doloc Town"
)

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ProjectDir "..\..\.."))
$SourceDir = Join-Path $ProjectDir "src"
$OutDir = Join-Path $ProjectDir "dist"
$PackagePluginDir = Join-Path $RepoRoot "packages\workshop\WorkshopPackages\DLK_AutoFishing\Content\DolocSMAPI\plugins"
$LocalModsPluginDir = Join-Path $env:USERPROFILE "AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_AutoFishing\Content\DolocSMAPI\plugins"
$SdkDll = Join-Path $RepoRoot "src\smapi\DolocTownSMAPI.SDK\dist\DolocTownSMAPI.SDK.dll"
$ManagedDir = Join-Path $GameDir "DolocTown_Data\Managed"
$CoreDir = Join-Path $GameDir "BepInEx\core"
$Csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (!(Test-Path -LiteralPath $Csc)) {
    throw "csc.exe was not found at $Csc"
}

$required = @(
    $SdkDll,
    (Join-Path $CoreDir "0Harmony.dll"),
    (Join-Path $ManagedDir "Assembly-CSharp.dll"),
    (Join-Path $ManagedDir "netstandard.dll"),
    (Join-Path $ManagedDir "UnityEngine.dll"),
    (Join-Path $ManagedDir "UnityEngine.CoreModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.InputLegacyModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.IMGUIModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.AnimationModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.Physics2DModule.dll"),
    (Join-Path $ManagedDir "UnityEngine.UI.dll"),
    (Join-Path $ManagedDir "Unity.InputSystem.dll")
)

foreach ($path in $required) {
    if (!(Test-Path -LiteralPath $path)) {
        throw "Missing reference: $path"
    }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
New-Item -ItemType Directory -Force -Path $PackagePluginDir | Out-Null
New-Item -ItemType Directory -Force -Path $LocalModsPluginDir | Out-Null

$outDll = Join-Path $OutDir "DolocTownAutoFishing.dll"
$sourceFiles = Get-ChildItem -LiteralPath $SourceDir -Filter "*.cs" -File | ForEach-Object { $_.FullName }
$referenceArgs = $required | ForEach-Object { "/reference:$_" }

& $Csc /nologo /target:library /optimize+ /define:SMAPI_AUTO_FISHING_BUILD "/out:$outDll" $referenceArgs $sourceFiles
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath $outDll -Destination (Join-Path $PackagePluginDir "DolocTownAutoFishing.dll") -Force
Copy-Item -LiteralPath $outDll -Destination (Join-Path $LocalModsPluginDir "DolocTownAutoFishing.dll") -Force
Write-Host "Built native SMAPI AutoFishing mod: $outDll"
Write-Host "Packaged: $(Join-Path $PackagePluginDir 'DolocTownAutoFishing.dll')"
Write-Host "Installed for local MODS test: $(Join-Path $LocalModsPluginDir 'DolocTownAutoFishing.dll')"
