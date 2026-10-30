[CmdletBinding()]
param([string]$OutputRoot='')
$ErrorActionPreference='Stop'
. "$PSScriptRoot/common.ps1"
$sampleRepo=Get-RepoRoot
$sampleDotnet=Get-DotNetExe -RepoRoot $sampleRepo
if(-not $OutputRoot){$OutputRoot=Join-Path $sampleRepo ('temp/monster-author-contracts-'+[guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$sampleCount=0
foreach($sample in Get-ChildItem -LiteralPath (Join-Path $sampleRepo 'author-sdk/examples') -Directory -Filter 'monster*'){
 $source=Join-Path $sample.FullName 'ModEntry.cs'
 if(-not(Test-Path -LiteralPath $source)){continue}
 & $sampleDotnet build "$sampleRepo/tests/DTMAPI.MonsterExamples.Build/DTMAPI.MonsterExamples.Build.csproj" -c Release --nologo -m:1 -p:UseSharedCompilation=false "-p:MonsterSampleSource=$source" *> (Join-Path $OutputRoot ($sample.Name+'.log'))
 if($LASTEXITCODE){throw "Contract-only example build failed: $($sample.Name); $OutputRoot"}
 $sampleCount++
}
Write-Output "PASS independent contract-only author examples: $sampleCount; $OutputRoot"
