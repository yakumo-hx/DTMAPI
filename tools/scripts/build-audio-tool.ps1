[CmdletBinding()]
param([string]$OutputRoot='dist/audio-075', [string]$Configuration='Release')
$ErrorActionPreference='Stop'
. "$PSScriptRoot/common.ps1"
$audioRepo=Get-RepoRoot
$audioDotnet=Get-DotNetExe -RepoRoot $audioRepo -NoProvision
$audioOutput=[IO.Path]::GetFullPath($OutputRoot)
$audioTool=Join-Path $audioOutput 'DTMAPI-AudioPrepare-win-x64'
if(Test-Path -LiteralPath $audioTool){throw 'Choose a fresh audio tool delivery directory.'}
& $audioDotnet publish "$audioRepo/tools/AudioPrepare/AudioPrepare.csproj" -c $Configuration -r win-x64 --self-contained true --nologo -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false -o $audioTool
if($LASTEXITCODE){throw 'Audio preparation tool publish failed.'}
& "$PSScriptRoot/prepare-audio-decoder.ps1" -OutputRoot "$audioTool/audio-decoder"
Copy-Item -LiteralPath "$audioRepo/author-sdk/AUDIO-CONTENT.md" -Destination "$audioTool/README.md"
Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $audioDotnet) 'LICENSE.txt') -Destination "$audioTool/dotnet-LICENSE.txt"
Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $audioDotnet) 'ThirdPartyNotices.txt') -Destination "$audioTool/dotnet-ThirdPartyNotices.txt"
Compress-Archive -LiteralPath $audioTool -DestinationPath "$audioOutput/DTMAPI-AudioPrepare-0.7.5-win-x64.zip"
Write-Output "$audioOutput/DTMAPI-AudioPrepare-0.7.5-win-x64.zip"
