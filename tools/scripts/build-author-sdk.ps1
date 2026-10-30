[CmdletBinding()]
param([string]$Configuration='Release', [string]$OutputRoot='', [switch]$NoProvision, [switch]$ReleasePackage, [switch]$IncludeToolchain, [switch]$IncludeAnalyzer)
. "$PSScriptRoot/common.ps1"
$ErrorActionPreference='Stop'
$sdkRepo=Get-RepoRoot
if(($IncludeToolchain -or $IncludeAnalyzer) -and -not $ReleasePackage){throw 'Distribution additions require -ReleasePackage; ordinary validation builds only the local CLI.'}
if(-not $OutputRoot){$OutputRoot=Join-Path $sdkRepo $(if($ReleasePackage){'dist/author-sdk'}else{'.tools/author-sdk'})}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
$sdkProject=Join-Path $sdkRepo 'src/DTMAPI.AuthorSdk/DTMAPI.AuthorSdk.csproj'
if(-not $ReleasePackage){
    $sdkDotnet=Get-DotNetExe -RepoRoot $sdkRepo -NoProvision:$NoProvision
    & $sdkDotnet build $sdkProject -c $Configuration --nologo -m:1
    if($LASTEXITCODE){throw 'Local author CLI build failed.'}
    $sdkLocal=Join-Path $sdkRepo "src/DTMAPI.AuthorSdk/bin/$Configuration/net8.0"
    [void][IO.Directory]::CreateDirectory($OutputRoot)
    [ordered]@{schemaVersion=2;stageRoot=$sdkLocal;sdkVersion='0.7.5';description='Local framework-dependent CLI build output; no distribution or Runtime admission proof.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputRoot 'preparation.json') -Encoding utf8
    Write-Output "Local author CLI: $sdkLocal"
    Write-Output "Run with the selected .NET host: & '$sdkDotnet' '$(Join-Path $sdkLocal 'dtmapi-author.dll')' <arguments>"
    return
}
# Full self-contained delivery is an explicit prerelease action. Reuse its fixed
# destination; prepare the replacement first and do not retain previous copies.
$sdkFinal=Join-Path $OutputRoot 'DTMAPI-AuthorSdk-0.7.5'
$sdkArchive=$sdkFinal+'.zip'
$sdkDotnet=Get-DotNetExe -RepoRoot $sdkRepo -NoProvision:$NoProvision
$sdkStage=$sdkFinal+'.stage-'+[guid]::NewGuid().ToString('N')
[void][IO.Directory]::CreateDirectory($sdkStage)
& $sdkDotnet publish $sdkProject -c $Configuration -r win-x64 --self-contained true -o $sdkStage --nologo -m:1
if($LASTEXITCODE){throw 'Author tools publication failed.'}
& "$PSScriptRoot/build-api-references.ps1" -OutputRoot (Join-Path $sdkStage 'references') -Configuration $Configuration
& $sdkDotnet publish (Join-Path $sdkRepo 'src/DTMAPI.PlayerDoctor/DTMAPI.PlayerDoctor.csproj') -c $Configuration -r win-x64 --self-contained true -o (Join-Path $sdkStage 'doctor') --nologo -m:1
if($LASTEXITCODE){throw 'Standalone Doctor publication failed.'}
foreach($sdkNotice in @('LICENSE.txt','ThirdPartyNotices.txt')){
    $sdkNoticeSource=Join-Path (Split-Path -Parent $sdkDotnet) $sdkNotice
    foreach($sdkNoticeRoot in @($sdkStage,(Join-Path $sdkStage 'doctor'))){
        [void][IO.Directory]::CreateDirectory((Join-Path $sdkNoticeRoot 'licenses'))
        Copy-Item -LiteralPath $sdkNoticeSource -Destination (Join-Path $sdkNoticeRoot ('licenses/dotnet-'+$sdkNotice))
    }
}
if($IncludeAnalyzer){
    & $sdkDotnet build (Join-Path $sdkRepo 'src/DTMAPI.Author.Analyzers/DTMAPI.Author.Analyzers.csproj') -c $Configuration --nologo
    if($LASTEXITCODE){throw 'Optional analyzer build failed.'}
    [void][IO.Directory]::CreateDirectory((Join-Path $sdkStage 'analyzers'))
    Copy-Item -LiteralPath (Join-Path $sdkRepo "src/DTMAPI.Author.Analyzers/bin/$Configuration/netstandard2.0/DTMAPI.Author.Analyzers.dll") -Destination (Join-Path $sdkStage 'analyzers') -Force
}
if($IncludeToolchain){Copy-Item -LiteralPath (Split-Path -Parent $sdkDotnet) -Destination (Join-Path $sdkStage 'toolchain') -Recurse -Force}
& "$PSScriptRoot/check-author-sdk-release.ps1" -PackagePath $sdkStage
if(-not $?){throw 'Author SDK delivery layout check failed.'}
$sdkStageArchive=$sdkStage+'.zip'
New-DtmApiZipArchive -SourceDirectory $sdkStage -DestinationPath $sdkStageArchive
foreach($sdkPath in @($sdkStage,$sdkStageArchive,$sdkFinal,$sdkArchive)){
    if(-not (Test-DtmApiPathIsSameOrChild -Child $sdkPath -Parent $OutputRoot) -or [IO.Path]::GetFullPath($sdkPath) -eq $OutputRoot){throw 'SDK delivery paths escaped the output root.'}
    for($sdkCursor=$sdkPath;$sdkCursor;$sdkCursor=Split-Path -Parent $sdkCursor){
        if((Test-Path -LiteralPath $sdkCursor) -and ((Get-Item -LiteralPath $sdkCursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "SDK delivery path uses a reparse point: $sdkCursor"}
    }
}
if(Test-Path -LiteralPath $sdkFinal){Remove-Item -LiteralPath $sdkFinal -Recurse -Force}
if(Test-Path -LiteralPath $sdkArchive){Remove-Item -LiteralPath $sdkArchive -Force}
Move-Item -LiteralPath $sdkStage -Destination $sdkFinal
Move-Item -LiteralPath $sdkStageArchive -Destination $sdkArchive
Write-Output "Author SDK delivery: $sdkArchive"
