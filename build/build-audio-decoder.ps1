[CmdletBinding()]
param([string]$WslDistribution='Ubuntu')
$ErrorActionPreference='Stop'
. "$PSScriptRoot/windows-installer/common.ps1"
$decoderRepo=Get-RepoRoot
$decoderSpec=Get-Content -LiteralPath "$decoderRepo/build/audio-decoder.json" -Raw | ConvertFrom-Json
$decoderCache=Join-Path $decoderRepo '.tools/audio-decoder'
$decoderPackages=Join-Path $decoderCache 'toolchain-packages'
$decoderOutput=Join-Path $decoderCache $decoderSpec.version
[void][IO.Directory]::CreateDirectory($decoderPackages)
[void][IO.Directory]::CreateDirectory($decoderOutput)
foreach($decoderUrl in $decoderSpec.toolchainPackages){
    $decoderFile=Join-Path $decoderPackages ([Uri]::UnescapeDataString(([Uri]$decoderUrl).Segments[-1]))
    if(-not(Test-Path -LiteralPath $decoderFile)){Invoke-WebRequest -Uri $decoderUrl -OutFile $decoderFile}
}
$decoderSource=Join-Path $decoderCache 'ffmpeg-sources.tar.gz'
if(-not(Test-Path -LiteralPath $decoderSource)){Invoke-WebRequest -Uri $decoderSpec.sourceUrl -OutFile $decoderSource}
foreach($line in Get-Content -LiteralPath "$decoderRepo/build/audio-decoder/inputs.sha256"){
    $expected,$relative=$line -split '  ',2
    if((Get-FileHash -LiteralPath (Join-Path $decoderCache $relative) -Algorithm SHA256).Hash -ine $expected){throw "Pinned decoder input differs: $relative"}
}
Copy-Item -LiteralPath "$decoderRepo/build/audio-decoder/inputs.sha256" -Destination $decoderCache
function ConvertTo-AudioWslPath([string]$path){
    $converted=@(& wsl.exe -d $WslDistribution --exec wslpath -a $path)
    if($LASTEXITCODE){throw 'The developer-only FFmpeg build requires a working x64 Ubuntu WSL with make and dpkg-deb.'}
    return ($converted -join '').Trim()
}
$decoderScript=ConvertTo-AudioWslPath (Join-Path $decoderRepo $decoderSpec.buildRecipe)
$decoderLinuxCache=ConvertTo-AudioWslPath $decoderCache
$decoderLinuxOutput=ConvertTo-AudioWslPath $decoderOutput
& wsl.exe -d $WslDistribution --exec sh $decoderScript $decoderLinuxCache $decoderLinuxOutput
if($LASTEXITCODE){throw "Audio decoder compilation failed; inspect $decoderOutput/configure.log and build.log."}
$decoderVersion=@(& "$decoderOutput/ffmpeg.exe" -version 2>&1 | ForEach-Object {$_.ToString()})
if($LASTEXITCODE -or $decoderVersion[0] -notlike "ffmpeg version $($decoderSpec.version) *" -or ($decoderVersion -join ' ') -match '--enable-(gpl|nonfree)(\s|$)'){
    throw 'The built decoder does not match the fixed audio-only LGPL profile.'
}
$decoderRecord=[ordered]@{version=$decoderSpec.version;sourceCommit=$decoderSpec.sourceCommit;sourceUrl=$decoderSpec.sourceUrl;buildRecipe=$decoderSpec.buildRecipe;license=$decoderSpec.license;configuration=($decoderVersion|Where-Object {$_ -like 'configuration:*'});toolchainPackages=$decoderSpec.toolchainPackages;notes=$decoderSpec.distributionNotes}
[IO.File]::WriteAllText("$decoderOutput/distribution.json",($decoderRecord|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
Write-Output $decoderOutput
