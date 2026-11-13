[CmdletBinding()]
param([string]$OutputRoot='', [string]$Configuration='Release')
. "$PSScriptRoot/windows-installer/common.ps1"
$apiRepo=Get-RepoRoot
$apiDotnet=Get-DotNetExe -RepoRoot $apiRepo
if(-not $OutputRoot){$OutputRoot=Join-Path $apiRepo '.tools/api-references'}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
[void][IO.Directory]::CreateDirectory($OutputRoot)
foreach($apiName in @('DTMAPI.Abstractions','DTMAPI.ModConfigMenu','DTMAPI.MonsterContent','DTMAPI.AnimalContent','DTMAPI.AudioContent')){
    $apiProject=Join-Path $apiRepo "src/$apiName/$apiName.csproj"
    & $apiDotnet build $apiProject -c $Configuration --nologo -m:1 -p:GenerateDocumentationFile=true -p:NoWarn=1591
    if($LASTEXITCODE){throw "API reference build failed: $apiName"}
    foreach($apiExtension in @('dll','xml')){
        $apiFile=Join-Path $apiRepo "src/$apiName/bin/$Configuration/netstandard2.0/$apiName.$apiExtension"
        if(Test-Path -LiteralPath $apiFile){Copy-Item -LiteralPath $apiFile -Destination $OutputRoot -Force}
    }
}
Write-Output "API DLL/XML references: $OutputRoot"
