param(
    [Parameter(Mandatory=$true)][string]$SdkRoot,
    [Parameter(Mandatory=$true)][string]$ProjectRoot,
    [Parameter(Mandatory=$true)][string]$EvidenceRoot,
    [switch]$RestorePackages,
    [switch]$Offline,
    [string]$GameRoot='',
    [string]$LogicTestScript=''
)
$ErrorActionPreference='Stop'
$sdkDirectory=[IO.Path]::GetFullPath($SdkRoot)
. (Join-Path $sdkDirectory 'Enter-DtmApiEnvironment.ps1')
$sdk=Join-Path $sdkDirectory 'dtmapi-author.exe'
$project=[IO.Path]::GetFullPath($ProjectRoot)
$evidence=[IO.Path]::GetFullPath($EvidenceRoot)
if(Test-Path -LiteralPath $evidence){throw 'Use a new evidence directory; prior results are retained.'}
New-Item -ItemType Directory -Path $evidence|Out-Null
if($RestorePackages){
    & $sdk restore $project --offline $Offline.IsPresent.ToString().ToLowerInvariant() --json | Set-Content -LiteralPath (Join-Path $evidence 'restore.json')
    if($LASTEXITCODE -ne 0){throw 'Explicit restore failed'}
}
foreach($verb in @('validate','pack')){
    $arguments=@($verb,$project,'--json')
    if($verb -eq 'pack'){
        $arguments+=@('--offline',$Offline.IsPresent.ToString().ToLowerInvariant(),'--output',(Join-Path $evidence 'packages'))
        if($GameRoot){$arguments+=@('--game-root',[IO.Path]::GetFullPath($GameRoot))}
    }
    & $sdk @arguments | Set-Content -LiteralPath (Join-Path $evidence "$verb.json")
    if($LASTEXITCODE -ne 0){throw "$verb failed"}
}
$pack=Get-Content -LiteralPath (Join-Path $evidence 'pack.json') -Raw|ConvertFrom-Json
if((Get-FileHash -LiteralPath $pack.outputPath).Hash -ne $pack.sha256){throw 'Package report/hash mismatch'}
& $sdk doctor $pack.outputPath --json | Set-Content -LiteralPath (Join-Path $evidence 'doctor.json')
if($LASTEXITCODE -ne 0){throw 'Artifact Doctor found errors'}
if($LogicTestScript){
    $artifact=Join-Path $evidence 'artifact'
    Expand-Archive -LiteralPath $pack.outputPath -DestinationPath $artifact
    & ([IO.Path]::GetFullPath($LogicTestScript)) -ProjectRoot $project -ArtifactRoot $artifact -EvidenceRoot $evidence
    if(-not $?){throw 'Author logic tests failed'}
}
Write-Output "Author CI completed: $evidence. Game/Mono validation remains separate."
