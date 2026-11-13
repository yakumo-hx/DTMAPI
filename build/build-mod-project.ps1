[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [string]$OutputRoot='', [string]$GameDir='', [string]$Configuration='Release',
    [string]$ReferencesRoot='', [string]$NativeReferencesRoot='',
    [switch]$BuildOnly, [switch]$NoZip, [switch]$PrepareReferences, [string]$BuildOutput='',
    [string]$AssetRoot=$env:DTMAPI_MOD_ASSET_ROOT
)
. "$PSScriptRoot/windows-installer/common.ps1"
$ErrorActionPreference='Stop'
$modRepo=Get-RepoRoot
$ProjectPath=[IO.Path]::GetFullPath($ProjectPath)
$modHasProject=Test-Path -LiteralPath $ProjectPath -PathType Leaf
if($modHasProject -and [IO.Path]::GetExtension($ProjectPath) -ne '.csproj'){throw 'Compilation requires an explicit .csproj path.'}
if(-not $modHasProject -and -not (Test-Path -LiteralPath $ProjectPath -PathType Container)){throw "Source directory missing: $ProjectPath"}
# Directories are source layouts, never instructions to discover/evaluate projects.
# Finished packages go directly to pack/deploy; BuildOutput means ordinary compiler output.
$modSource=if($modHasProject){Split-Path -Parent $ProjectPath}else{$ProjectPath}
if(-not $BuildOnly){
    if([string]::IsNullOrWhiteSpace($AssetRoot)){throw 'Pass -AssetRoot or set DTMAPI_MOD_ASSET_ROOT to package artwork.'}
    $AssetRoot=[IO.Path]::GetFullPath($AssetRoot)
    if(-not (Test-Path -LiteralPath $AssetRoot -PathType Container)){throw "Mod artwork directory missing: $AssetRoot"}
    foreach($assetName in @('icon.png','preview.png')){
        $assetPath=Join-Path $AssetRoot $assetName
        if(-not (Test-Path -LiteralPath $assetPath -PathType Leaf)){throw "Mod artwork file missing: $assetPath"}
    }
}
if(-not $OutputRoot){$OutputRoot=Join-Path $modRepo ('temp/mod-build/'+[IO.Path]::GetFileNameWithoutExtension($ProjectPath)+'-'+[guid]::NewGuid().ToString('N'))}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw "Output directory exists: $OutputRoot"}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$modBuild=''
$modBuiltEntry=''
if($BuildOutput){
    $modBuild=[IO.Path]::GetFullPath($BuildOutput)
    if(-not (Test-Path -LiteralPath $modBuild -PathType Container)){throw "Build output missing: $modBuild"}
}elseif($modHasProject){
    $modDotnet=Get-DotNetExe -RepoRoot $modRepo
    if(-not $ReferencesRoot){$ReferencesRoot=Join-Path $modRepo '.tools/api-references'}
    $ReferencesRoot=[IO.Path]::GetFullPath($ReferencesRoot)
    if($PrepareReferences -or -not (Test-Path -LiteralPath (Join-Path $ReferencesRoot 'DTMAPI.Abstractions.dll'))){
        & "$PSScriptRoot/build-api-references.ps1" -OutputRoot $ReferencesRoot -Configuration $Configuration
    }
    if($NativeReferencesRoot){$NativeReferencesRoot=[IO.Path]::GetFullPath($NativeReferencesRoot)}
    $modPropertyJson=& $modDotnet msbuild $ProjectPath -nologo '-getProperty:AssemblyName,DtmApiNativeReferencePaths'
    if($LASTEXITCODE){throw 'Mod project evaluation failed.'}
    $modProperties=($modPropertyJson | Out-String | ConvertFrom-Json).Properties
    if($modProperties.DtmApiNativeReferencePaths -and -not $NativeReferencesRoot){
        if(-not $GameDir){$GameDir=Resolve-DolocTownGamePath -RepoRoot $modRepo}
        $NativeReferencesRoot=Join-Path $OutputRoot 'native-references'
        & "$PSScriptRoot/build.ps1" -SkipTests -Projects src/DTMAPI.NativeReferences/DTMAPI.NativeReferences.csproj
        $modNativeDlls=@(([string]$modProperties.DtmApiNativeReferencePaths).Split(';',[StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object {Join-Path $GameDir $_})
        & $modDotnet (Join-Path $modRepo 'src/DTMAPI.NativeReferences/bin/Release/net8.0/DTMAPI.NativeReferences.dll') $NativeReferencesRoot @modNativeDlls
        if($LASTEXITCODE){throw 'Native compiler reference conversion failed.'}
    }
    $modBuild=Join-Path $OutputRoot 'build'
    & $modDotnet build $ProjectPath -c $Configuration --nologo -m:1 "-p:DtmApiReferences=$ReferencesRoot" "-p:DtmApiNativeReferences=$NativeReferencesRoot" -o $modBuild
    if($LASTEXITCODE){throw 'Mod build failed.'}
    $modBuiltEntry=$modProperties.AssemblyName+'.dll'
}
if($BuildOnly){
    if(-not $modBuild){throw 'BuildOnly requires an explicit csproj or BuildOutput.'}
    [ordered]@{project=$ProjectPath;buildOutput=$modBuild} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputRoot 'summary.json') -Encoding utf8
    Write-Output "Mod build: $modBuild"
    return
}
$modPackage=Join-Path $OutputRoot 'package'
[void][IO.Directory]::CreateDirectory($modPackage)
if(Test-Path -LiteralPath (Join-Path $modSource 'package')){Copy-Item -Path (Join-Path $modSource 'package/*') -Destination $modPackage -Recurse -Force}
if(Test-Path -LiteralPath (Join-Path $modSource 'info.json')){Copy-Item -LiteralPath (Join-Path $modSource 'info.json') -Destination $modPackage -Force}
foreach($assetName in @('icon.png','preview.png')){
    Copy-Item -LiteralPath (Join-Path $AssetRoot $assetName) -Destination $modPackage -Force
}
foreach($modFolder in @('Content','PackData','lib','i18n')){
    $modFrom=Join-Path $modSource $modFolder
    if(Test-Path -LiteralPath $modFrom){
        $modTo=Join-Path $modPackage $modFolder
        [void][IO.Directory]::CreateDirectory($modTo)
        Copy-Item -Path (Join-Path $modFrom '*') -Destination $modTo -Recurse -Force
    }
}
if(Test-Path -LiteralPath (Join-Path $modSource 'manifest.json')){
    [void][IO.Directory]::CreateDirectory((Join-Path $modPackage 'Content/DTMAPI'))
    Copy-Item -LiteralPath (Join-Path $modSource 'manifest.json') -Destination (Join-Path $modPackage 'Content/DTMAPI/manifest.json') -Force
}
$modManifestPath=Join-Path $modPackage 'Content/DTMAPI/manifest.json'
if(-not (Test-Path -LiteralPath $modManifestPath)){$modManifestPath=Join-Path $modPackage 'manifest.json'}
$modInfoPath=Join-Path $modPackage 'info.json'
if((Test-Path -LiteralPath $modInfoPath) -and (Test-Path -LiteralPath $modManifestPath)){
    $modInfo=Get-Content -Encoding UTF8 -Raw -LiteralPath $modInfoPath | ConvertFrom-Json
    if($modInfo.PSObject.Properties['MinimumDTMApiVersion']){
        $modManifest=Get-Content -Encoding UTF8 -Raw -LiteralPath $modManifestPath | ConvertFrom-Json
        $modManifest | Add-Member -NotePropertyName MinimumDTMApiVersion -NotePropertyValue ([string]$modInfo.MinimumDTMApiVersion) -Force
        [IO.File]::WriteAllText($modManifestPath, ($modManifest | ConvertTo-Json -Depth 100))
    }
}
if($modBuild){
    $modManifest=Get-Content -Encoding UTF8 -Raw -LiteralPath $modManifestPath | ConvertFrom-Json
    $modEntryRelative=[string]$modManifest.EntryDll
    if(-not $modEntryRelative){throw 'An EntryDll path is needed to place compiler output.'}
    $modEntryTarget=[IO.Path]::GetFullPath((Join-Path $modPackage $modEntryRelative))
    if(-not $modEntryTarget.StartsWith($modPackage+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'EntryDll escapes the package directory.'}
    if(-not $modBuiltEntry){$modBuiltEntry=[IO.Path]::GetFileName($modEntryRelative.Replace('\','/'))}
    $modEntry=Join-Path $modBuild $modBuiltEntry
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $modEntryTarget))
    Copy-Item -LiteralPath $modEntry -Destination $modEntryTarget -Force
    foreach($modDll in @(Get-ChildItem -LiteralPath $modBuild -Filter '*.dll' | Where-Object {$_.FullName -ne $modEntry -and $_.Name -notin @('DTMAPI.Abstractions.dll','DTMAPI.ModConfigMenu.dll','DTMAPI.AudioContent.dll','DTMAPI.AnimalContent.dll','DTMAPI.MonsterContent.dll')})){
        $modLibrary=Join-Path $modPackage 'lib/private'
        [void][IO.Directory]::CreateDirectory($modLibrary)
        Copy-Item -LiteralPath $modDll.FullName -Destination $modLibrary -Force
    }
}
$modZip=''
if(-not $NoZip){
    $modZip=Join-Path $OutputRoot 'package.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    New-DtmApiZipArchive -SourceDirectory $modPackage -DestinationPath $modZip
}
[ordered]@{project=$ProjectPath;packagePath=$modZip;packageDirectory=$modPackage;buildOutput=$modBuild} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputRoot 'summary.json') -Encoding utf8
Write-Output "Mod package: $modPackage"
