[CmdletBinding()]
param([string]$OutputRoot='', [switch]$IncludeSource)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/windows-installer/common.ps1"
$audioRepo=Get-RepoRoot
$audioSpec=Get-Content -LiteralPath "$audioRepo/build/audio-decoder.json" -Raw | ConvertFrom-Json
$audioCache=Join-Path $audioRepo '.tools/audio-decoder'
$audioBuilt=Join-Path $audioCache $audioSpec.version
if(-not $OutputRoot){$OutputRoot=Join-Path $audioCache 'distribution'}
$audioDestination=[IO.Path]::GetFullPath($OutputRoot)
if(-not(Test-Path -LiteralPath "$audioBuilt/ffmpeg.exe")){
    throw 'Build the fixed decoder once with build/build-audio-decoder.ps1. Package preparation does not install a compiler or download a player-side decoder.'
}
$audioVersion=@(& "$audioBuilt/ffmpeg.exe" -version 2>&1 | ForEach-Object {$_.ToString()})
if($LASTEXITCODE -or $audioVersion[0] -notlike "ffmpeg version $($audioSpec.version) *" -or ($audioVersion -join ' ') -match '--enable-(gpl|nonfree)(\s|$)'){
    throw 'The selected decoder does not match the pinned audio-only LGPL build.'
}
[void][IO.Directory]::CreateDirectory($audioDestination)
foreach($component in @(Get-DtmApiRuntimeComponentFile | Where-Object { $_.Auxiliary })){
    $audioFile=[IO.Path]::GetFileName($component.RelativePath)
    Copy-Item -LiteralPath (Join-Path $audioBuilt $audioFile) -Destination (Join-Path $audioDestination $audioFile) -Force
}
if($IncludeSource){
    Copy-Item -LiteralPath "$audioCache/ffmpeg-sources.tar.gz" -Destination "$audioDestination/ffmpeg-sources.tar.gz" -Force
    Copy-Item -LiteralPath (Join-Path $audioRepo $audioSpec.buildRecipe) -Destination "$audioDestination/build-minimal.sh" -Force
    Copy-Item -LiteralPath "$audioRepo/build/build-audio-decoder.ps1" -Destination "$audioDestination/build-audio-decoder.ps1" -Force
    Copy-Item -LiteralPath "$audioRepo/build/audio-decoder.json" -Destination "$audioDestination/audio-decoder.json" -Force
    Copy-Item -LiteralPath "$audioRepo/build/audio-decoder/inputs.sha256" -Destination "$audioDestination/inputs.sha256" -Force
    Copy-Item -LiteralPath "$audioRepo/build/audio-decoder/README.md" -Destination "$audioDestination/README-source.md" -Force
}
Write-Output $audioDestination
