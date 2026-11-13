[CmdletBinding()]
param([string]$OutputRoot='', [string]$Configuration='Release', [switch]$Check, [switch]$NoProvision, [switch]$RequireReleaseArchive, [switch]$IncludeToolchain, [switch]$IncludeAnalyzer)
. "$PSScriptRoot/windows-installer/common.ps1"
$ErrorActionPreference='Stop'
$repo=Get-RepoRoot
if(($IncludeToolchain -or $IncludeAnalyzer) -and -not $RequireReleaseArchive){throw 'Distribution additions require -RequireReleaseArchive.'}
if(-not $OutputRoot){$OutputRoot=Join-Path $repo $(if($RequireReleaseArchive){'dist/author-sdk'}else{'.tools/author-sdk'})}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if($Check){
    if($RequireReleaseArchive){
        $archive=Join-Path $OutputRoot 'DTMAPI-AuthorSdk-0.7.5.zip'
        if(-not (Test-Path -LiteralPath $archive -PathType Leaf)){throw "Author SDK archive is absent: $archive"}
    }else{
        $stage=Join-Path $repo "src/DTMAPI.AuthorSdk/bin/$Configuration/net8.0"
        foreach($file in @('dtmapi-author.exe','dtmapi-author.dll','dtmapi-author.runtimeconfig.json')){
            if(-not (Test-Path -LiteralPath (Join-Path $stage $file) -PathType Leaf)){throw "Local author CLI is absent: $stage. Run prepare-author-sdk.ps1 when this tool is needed."}
        }
        Write-Output "Local author CLI present: $stage (presence only; rebuild after relevant input changes)."
    }
    return
}
& "$PSScriptRoot/build-author-sdk.ps1" -OutputRoot $OutputRoot -Configuration $Configuration -NoProvision:$NoProvision -ReleasePackage:$RequireReleaseArchive -IncludeToolchain:$IncludeToolchain -IncludeAnalyzer:$IncludeAnalyzer
if(-not $?){throw 'Author SDK preparation failed.'}
