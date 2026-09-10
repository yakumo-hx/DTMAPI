param(
    [Parameter(Mandatory=$true)][string]$SdkRoot,
    [Parameter(Mandatory=$true)][string]$ProjectRoot,
    [Parameter(Mandatory=$true)][string]$EvidenceRoot,
    [switch]$RestorePackages,
    [string]$LogicTestScript=''
)
$ErrorActionPreference='Stop'
$sdk=Join-Path ([IO.Path]::GetFullPath($SdkRoot)) 'dtmapi-author.exe'
$project=[IO.Path]::GetFullPath($ProjectRoot)
$evidence=[IO.Path]::GetFullPath($EvidenceRoot)
if(Test-Path -LiteralPath $evidence){throw 'Use a new evidence directory; prior results are retained.'}
New-Item -ItemType Directory -Path $evidence|Out-Null
if($RestorePackages){
    & $sdk restore $project --json | Set-Content -LiteralPath (Join-Path $evidence 'restore.json')
    if($LASTEXITCODE -ne 0){throw 'Explicit restore failed'}
}
foreach($verb in @('validate','build','pack')){
    & $sdk $verb $project --json | Set-Content -LiteralPath (Join-Path $evidence "$verb.json")
    if($LASTEXITCODE -ne 0){throw "$verb failed"}
}
$pack=Get-Content -LiteralPath (Join-Path $evidence 'pack.json') -Raw|ConvertFrom-Json
if((Get-FileHash -LiteralPath $pack.outputPath).Hash -ne $pack.sha256){throw 'Package report/hash mismatch'}
$artifact=Join-Path $evidence 'artifact'
Expand-Archive -LiteralPath $pack.outputPath -DestinationPath $artifact
& $sdk doctor $artifact --json | Set-Content -LiteralPath (Join-Path $evidence 'doctor.json')
if($LASTEXITCODE -ne 0){throw 'Artifact Doctor found errors'}
if($LogicTestScript){
    & ([IO.Path]::GetFullPath($LogicTestScript)) -ProjectRoot $project -ArtifactRoot $artifact -EvidenceRoot $evidence
    if(-not $?){throw 'Author logic tests failed'}
}
Write-Output "Author CI completed: $evidence. Game/Mono validation remains separate."
