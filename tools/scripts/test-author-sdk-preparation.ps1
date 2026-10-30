[CmdletBinding()]
param()
. "$PSScriptRoot/common.ps1"
$ErrorActionPreference='Stop'
$repo=Get-RepoRoot
$testRoot=Join-Path $repo ('temp/sdk-preparation-routing-'+[guid]::NewGuid().ToString('N'))
$driver=Join-Path $testRoot 'tools/scripts'
[void][IO.Directory]::CreateDirectory($driver)
function Write-Fixture([string]$Path,[string]$Value){
    $full=Join-Path $testRoot $Path
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $full))
    [IO.File]::WriteAllText($full,$Value,[Text.UTF8Encoding]::new($false))
}
function Assert-Routing([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
try{
    foreach($name in @('prepare-author-sdk.ps1','build-author-sdk.ps1','check-author-sdk-release.ps1')){
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $driver $name)
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'common/files.ps1') -Destination (Join-Path $driver 'files.ps1')
    Write-Fixture 'tools/scripts/common.ps1' @'
. "$PSScriptRoot/files.ps1"
function Get-RepoRoot { Split-Path -Parent (Split-Path -Parent $PSScriptRoot) }
function Get-DotNetExe { param($RepoRoot,[switch]$NoProvision); Join-Path $PSScriptRoot 'dotnet-fixture.ps1' }
$script:testZipWriter=(Get-Command New-DtmApiZipArchive).ScriptBlock
function New-DtmApiZipArchive {
    param($SourceDirectory,$DestinationPath)
    Add-Content -LiteralPath (Join-Path (Get-RepoRoot) 'trace') -Value 'zip'
    & $script:testZipWriter -SourceDirectory $SourceDirectory -DestinationPath $DestinationPath
}
'@
    Write-Fixture 'tools/scripts/dotnet-fixture.ps1' @'
$root=Get-RepoRoot
$name=[IO.Path]::GetFileNameWithoutExtension($args[1])
Add-Content -LiteralPath (Join-Path $root 'trace') -Value ($args[0]+':'+$name)
if(Test-Path -LiteralPath (Join-Path $root 'fail-build')){$global:LASTEXITCODE=23;return}
$config=$args[[Array]::IndexOf($args,'-c')+1]
$out=if($args[0] -eq 'build'){Join-Path $root "src/$name/bin/$config/net8.0"}else{$args[[Array]::IndexOf($args,'-o')+1]}
$exe=if($name -eq 'DTMAPI.AuthorSdk'){'dtmapi-author'}else{'dtmapi-player-doctor'}
foreach($file in @("$exe.exe","$exe.dll","$exe.runtimeconfig.json",'System.Private.CoreLib.dll','DTMAPI.InstallDoctor.dll','DTMAPI.Tooling.Metadata.dll','Mono.Cecil.dll','README.md','THIRD-PARTY-NOTICES.md','licenses/Mono.Cecil-0.11.6-LICENSE.txt')){
    $path=Join-Path $out $file
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
    [IO.File]::WriteAllText($path,'fixture payload')
}
$global:LASTEXITCODE=0
'@
    Write-Fixture 'tools/scripts/build-api-references.ps1' @'
param($OutputRoot,$Configuration)
Add-Content -LiteralPath (Join-Path (Get-RepoRoot) 'trace') -Value 'api-references'
[void][IO.Directory]::CreateDirectory($OutputRoot)
foreach($api in @('DTMAPI.Abstractions','DTMAPI.ModConfigMenu','DTMAPI.MonsterContent','DTMAPI.AnimalContent','DTMAPI.AudioContent')){
    foreach($ext in @('dll','xml')){[IO.File]::WriteAllText((Join-Path $OutputRoot "$api.$ext"),'reference fixture')}
}
'@
    Write-Fixture 'tools/scripts/LICENSE.txt' 'fixture license'
    Write-Fixture 'tools/scripts/ThirdPartyNotices.txt' 'fixture notices'
    $trace=Join-Path $testRoot 'trace'
    $shell=(Get-Process -Id $PID).Path
    function Invoke-Routing([string]$Entry,[string[]]$Arguments,[bool]$Success,[string]$ExpectedTrace,[string]$Diagnostic=''){
        [IO.File]::WriteAllText($trace,'')
        $prior=$ErrorActionPreference
        try{
            $ErrorActionPreference='Continue'
            $log=(& $shell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $driver $Entry) @Arguments 2>&1 | Out-String)
            $code=$LASTEXITCODE
        }finally{$ErrorActionPreference=$prior}
        $actual=@(Get-Content -LiteralPath $trace) -join ','
        Assert-Routing (($code -eq 0) -eq $Success -and $actual -ceq $ExpectedTrace -and (-not $Diagnostic -or $log.Contains($Diagnostic))) "Routing failed: $Entry exit=$code trace=$actual output=$log"
    }
    Invoke-Routing 'prepare-author-sdk.ps1' @() $true 'build:DTMAPI.AuthorSdk'
    $local=Join-Path $testRoot 'src/DTMAPI.AuthorSdk/bin/Release/net8.0'
    $receipt=Get-Content -LiteralPath (Join-Path $testRoot '.tools/author-sdk/preparation.json') -Raw | ConvertFrom-Json
    Assert-Routing ($receipt.stageRoot -eq $local) 'Local preparation must point to ordinary build output.'
    Assert-Routing (@(Get-ChildItem -LiteralPath $testRoot -Filter '*.zip' -Recurse).Count -eq 0) 'Ordinary preparation created an archive.'
    Invoke-Routing 'prepare-author-sdk.ps1' @('-Check','-OutputRoot',(Join-Path $testRoot 'not-created')) $true ''
    Assert-Routing (-not (Test-Path -LiteralPath (Join-Path $testRoot 'not-created'))) 'Check created output.'
    Invoke-Routing 'prepare-author-sdk.ps1' @() $true 'build:DTMAPI.AuthorSdk'
    Invoke-Routing 'build-author-sdk.ps1' @('-IncludeToolchain') $false '' 'require -ReleasePackage'
    Write-Fixture 'fail-build' 'fail'
    Invoke-Routing 'prepare-author-sdk.ps1' @() $false 'build:DTMAPI.AuthorSdk' 'Local author CLI build failed.'
    Remove-Item -LiteralPath (Join-Path $testRoot 'fail-build')
    $releaseTrace='publish:DTMAPI.AuthorSdk,api-references,publish:DTMAPI.PlayerDoctor,zip'
    Invoke-Routing 'prepare-author-sdk.ps1' @('-RequireReleaseArchive') $true $releaseTrace
    $stage=Join-Path $testRoot 'dist/author-sdk/DTMAPI-AuthorSdk-0.7.5'
    $zip=$stage+'.zip'
    Assert-Routing (Test-Path -LiteralPath $zip) 'Explicit release did not produce its archive.'
    Invoke-Routing 'prepare-author-sdk.ps1' @('-RequireReleaseArchive','-Check') $true ''
    Invoke-Routing 'prepare-author-sdk.ps1' @('-RequireReleaseArchive') $true $releaseTrace
    Assert-Routing (@(Get-ChildItem -LiteralPath (Split-Path -Parent $stage)).Count -eq 2) 'Release retained staging, previous or extra ZIP copies.'
    [IO.File]::WriteAllText((Join-Path $stage 'Assembly-CSharp.dll'),'forbidden native fixture')
    Invoke-Routing 'check-author-sdk-release.ps1' @('-PackagePath',$stage) $false '' 'must not redistribute native/game references'
    Remove-Item -LiteralPath (Join-Path $stage 'Assembly-CSharp.dll')
    [IO.File]::WriteAllText((Join-Path $stage 'README.md'),$testRoot)
    Invoke-Routing 'check-author-sdk-release.ps1' @('-PackagePath',$stage) $false '' 'private source/player path'
    [IO.File]::WriteAllText((Join-Path $stage 'README.md'),'fixture payload')
    Remove-Item -LiteralPath (Join-Path $stage 'dtmapi-author.dll')
    Invoke-Routing 'check-author-sdk-release.ps1' @('-PackagePath',$stage) $false '' 'delivery file is missing'
    Remove-Item -LiteralPath (Join-Path $local 'dtmapi-author.dll')
    Invoke-Routing 'prepare-author-sdk.ps1' @('-Check') $false '' 'Local author CLI is absent'
    [IO.File]::WriteAllText($zip,'not a ZIP')
    Invoke-Routing 'prepare-author-sdk.ps1' @('-RequireReleaseArchive','-Check') $false ''
    Write-Output 'Author SDK preparation routing: PASS (local build only, explicit one-ZIP delivery, fixed replacement, read-only checks, failures and private/game payload refusal).'
}finally{
    $safe=[IO.Path]::GetFullPath($testRoot)
    $temp=[IO.Path]::GetFullPath((Join-Path $repo 'temp'))
    if(-not (Test-DtmApiPathIsSameOrChild -Child $safe -Parent $temp) -or $safe -eq $temp){throw 'Unsafe routing fixture cleanup path.'}
    Remove-Item -LiteralPath $safe -Recurse -Force
}
