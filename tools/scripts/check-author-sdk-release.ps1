[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PackagePath)
. "$PSScriptRoot/common.ps1"
$ErrorActionPreference='Stop'
$package=[IO.Path]::GetFullPath($PackagePath)
$required=@(
    'dtmapi-author.exe','dtmapi-author.dll','dtmapi-author.runtimeconfig.json','System.Private.CoreLib.dll',
    'DTMAPI.InstallDoctor.dll','DTMAPI.Tooling.Metadata.dll','Mono.Cecil.dll',
    'README.md','THIRD-PARTY-NOTICES.md','licenses/Mono.Cecil-0.11.6-LICENSE.txt',
    'licenses/dotnet-LICENSE.txt','licenses/dotnet-ThirdPartyNotices.txt',
    'doctor/dtmapi-player-doctor.exe','doctor/dtmapi-player-doctor.dll','doctor/dtmapi-player-doctor.runtimeconfig.json',
    'doctor/System.Private.CoreLib.dll','doctor/licenses/dotnet-LICENSE.txt','doctor/licenses/dotnet-ThirdPartyNotices.txt'
)
foreach($api in @('DTMAPI.Abstractions','DTMAPI.ModConfigMenu','DTMAPI.MonsterContent','DTMAPI.AnimalContent','DTMAPI.AudioContent')){
    $required += "references/$api.dll"
    $required += "references/$api.xml"
}
$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$privateMarkers=@((Get-RepoRoot),$env:USERPROFILE,'references/doloc-town/reverse','references\doloc-town\reverse','references/third-party-mods','references\third-party-mods','steamapps\common\DolocTown') | Where-Object {$_}
function Test-SdkDeliveryEntry([string]$Name,[byte[]]$Bytes){
    $relative=$Name.Replace('\','/')
    if([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|/)\.\.(/|$)|:' -or -not $seen.Add($relative)){throw "Unsafe or duplicate SDK entry: $relative"}
    $leaf=($relative -split '/')[-1]
    if($leaf -match '^(Assembly-CSharp(?:-firstpass)?|UnityEngine.*|UnityEditor.*|BepInEx|0Harmony)\.dll$'){
        throw "SDK must not redistribute native/game references: $relative"
    }
    if($leaf -match '^DTMAPI\.(BepInExBootstrap|Core|GameBridge\..*|RuntimeModules|Modules\..*)\.dll$' -or $relative -match '(^|/)(BepInEx|workshop|MODS)/'){
        throw "SDK must remain separate from player Runtime/Workshop payloads: $relative"
    }
    # Retain the release boundary against accidental private paths/material. No
    # input-hash receipt, target registry, archive replay or staging extraction.
    foreach($text in @([Text.Encoding]::UTF8.GetString($Bytes),[Text.Encoding]::Unicode.GetString($Bytes))){
        foreach($marker in $privateMarkers){
            if($text.IndexOf($marker,[StringComparison]::OrdinalIgnoreCase) -ge 0){throw "SDK contains a private source/player path '$marker' in $relative"}
        }
    }
}
if(Test-Path -LiteralPath $package -PathType Container){
    $prefix=$package.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    foreach($file in Get-ChildItem -LiteralPath $package -File -Recurse){
        Test-SdkDeliveryEntry $file.FullName.Substring($prefix.Length) ([IO.File]::ReadAllBytes($file.FullName))
    }
}elseif((Test-Path -LiteralPath $package -PathType Leaf) -and $package.EndsWith('.zip',[StringComparison]::OrdinalIgnoreCase)){
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip=[IO.Compression.ZipFile]::OpenRead($package)
    try{
        foreach($entry in $zip.Entries){
            if($entry.FullName.EndsWith('/')){continue}
            $stream=$entry.Open();$buffer=[IO.MemoryStream]::new()
            try{
                $stream.CopyTo($buffer)
                if($buffer.Length -ne $entry.Length){throw "Incomplete SDK ZIP entry: $($entry.FullName)"}
                Test-SdkDeliveryEntry $entry.FullName $buffer.ToArray()
            }finally{$buffer.Dispose();$stream.Dispose()}
        }
    }finally{$zip.Dispose()}
}else{throw "SDK delivery directory or ZIP is absent: $package"}
foreach($name in $required){if(-not $seen.Contains($name)){throw "SDK delivery file is missing: $name"}}
Write-Output "Author SDK delivery layout/content check: PASS ($($seen.Count) files): $package"
Write-Output 'This check does not build, repackage, launch tools, or establish author workflow acceptance.'