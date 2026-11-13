[CmdletBinding()]
param([string]$Configuration='Release', [string]$OutputRoot='', [switch]$NoProvision, [switch]$ReleasePackage, [switch]$IncludeToolchain, [switch]$IncludeAnalyzer)
. "$PSScriptRoot/windows-installer/common.ps1"
$ErrorActionPreference='Stop'
$sdkRepo=Get-RepoRoot
if(($IncludeToolchain -or $IncludeAnalyzer) -and -not $ReleasePackage){throw 'Distribution additions require -ReleasePackage; ordinary validation builds only the local CLI.'}
if(-not $OutputRoot){$OutputRoot=Join-Path $sdkRepo $(if($ReleasePackage){'dist/author-sdk'}else{'.tools/author-sdk'})}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
$sdkProject=Join-Path $sdkRepo 'src/DTMAPI.AuthorSdk/DTMAPI.AuthorSdk.csproj'
function Update-AuthorSdkDocumentLinks([string]$DocumentRoot){
    # The existing SDK flattens author guides at its root; only copied links change.
    foreach($document in Get-ChildItem -LiteralPath $DocumentRoot -File -Filter '*.md'){
        $original=[IO.File]::ReadAllText($document.FullName)
        $text=[regex]::Replace($original,'\[([^\]]+)\]\(([^)]+)\)',{
            param($match)
            $target=$match.Groups[2].Value
            if($target -match '^(https?://|#|mailto:)'){return $match.Value}
            $packageTarget=$target.Replace('../../sdk/','')
            if(Test-Path -LiteralPath (Join-Path $DocumentRoot $packageTarget.Split('#')[0])){
                return '['+$match.Groups[1].Value+']('+$packageTarget+')'
            }
            # Keep source-only examples and schemas outside this SDK delivery.
            $sourceTarget=$target -replace '^\.\./\.\./',''
            return $match.Groups[1].Value+'（公开源码：`'+$sourceTarget+'`）'
        })
        if($text -cne $original){
            $bytes=[IO.File]::ReadAllBytes($document.FullName)
            $bom=$bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
            [IO.File]::WriteAllText($document.FullName,$text,[Text.UTF8Encoding]::new($bom))
        }
    }
}
if(-not $ReleasePackage){
    $sdkDotnet=Get-DotNetExe -RepoRoot $sdkRepo -NoProvision:$NoProvision
    & $sdkDotnet build $sdkProject -c $Configuration --nologo -m:1
    if($LASTEXITCODE){throw 'Local author CLI build failed.'}
    $sdkLocal=Join-Path $sdkRepo "src/DTMAPI.AuthorSdk/bin/$Configuration/net8.0"
    Update-AuthorSdkDocumentLinks -DocumentRoot $sdkLocal
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
Update-AuthorSdkDocumentLinks -DocumentRoot $sdkStage
& "$PSScriptRoot/build-api-references.ps1" -OutputRoot (Join-Path $sdkStage 'references') -Configuration $Configuration
foreach($sdkNotice in @('LICENSE.txt','ThirdPartyNotices.txt')){
    $sdkNoticeSource=Join-Path (Split-Path -Parent $sdkDotnet) $sdkNotice
    foreach($sdkNoticeRoot in @($sdkStage)){
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
