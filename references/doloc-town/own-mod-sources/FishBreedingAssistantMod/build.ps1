param(
    [string]$GameDir = "D:\Steam\steamapps\common\Doloc Town"
)

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ProjectDir "..\..\.."))
$SourceDir = Join-Path $ProjectDir "src"
$OutDir = Join-Path $ProjectDir "dist"
$SdkDll = Join-Path $RepoRoot "src\smapi\DolocTownSMAPI.SDK\dist\DolocTownSMAPI.SDK.dll"
$ManagedDir = Join-Path $GameDir "DolocTown_Data\Managed"
$CoreDir = Join-Path $GameDir "BepInEx\core"
$Csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (!(Test-Path -LiteralPath $Csc)) {
    throw "csc.exe was not found at $Csc"
}

$generatedData = Join-Path $SourceDir "FishBreedingLookup.g.cs"
if (!(Test-Path -LiteralPath $generatedData)) {
    throw "Missing generated source: $generatedData. Run tools\build_fish_outputs.py first."
}

$required = @(
    $SdkDll,
    (Join-Path $CoreDir "0Harmony.dll"),
    (Join-Path $ManagedDir "netstandard.dll"),
    (Join-Path $ManagedDir "UnityEngine.dll"),
    (Join-Path $ManagedDir "UnityEngine.CoreModule.dll")
)

foreach ($path in $required) {
    if (!(Test-Path -LiteralPath $path)) {
        throw "Missing reference: $path"
    }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$outDll = Join-Path $OutDir "DolocTownFishBreedingAssistant.dll"
$sourceFiles = Get-ChildItem -LiteralPath $SourceDir -Filter "*.cs" -File | ForEach-Object { $_.FullName }
$referenceArgs = $required | ForEach-Object { "/reference:$_" }

& $Csc /nologo /target:library /optimize+ "/out:$outDll" $referenceArgs $sourceFiles
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Host "Built: $outDll"
