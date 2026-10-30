[CmdletBinding()]
param(
    [ValidateSet('Plan','Export','PreparePr','Finalize','Import')][string]$Action='Plan',
    [string]$SourceCommit, [string]$OutputRoot, [string]$ExportDirectory,
    [string]$PublicRepository, [string]$PublicMainCommit, [string]$ExportBaseCommit,
    [switch]$Bootstrap, [string]$DecisionsPath, [string]$AcceptedCommit,
    [string]$ValidationPath, [string[]]$PublicCommits, [string]$PullRequestUrl,
    [string[]]$ProtectedPaths=@()
)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/../scripts/common.ps1"
$deliveryRepo=Get-RepoRoot
# Use configured paths only; normal source export does not discover a game or contact Steam.
$settingsPath=Join-Path $deliveryRepo 'local.settings.json'
if(Test-Path -LiteralPath $settingsPath) {
    $localSettings=Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    foreach($property in $localSettings.PSObject.Properties) {
        if($property.Name -match '(Game|Workshop|Upload|Mods).*(Dir|Root|Path)$' -and $property.Value -is [string] -and [IO.Path]::IsPathRooted($property.Value)){$ProtectedPaths+=@([IO.Path]::GetFullPath($property.Value))}
    }
    if($localSettings.PSObject.Properties['GameDir']) {
        $steamapps=Split-Path (Split-Path ([string]$localSettings.GameDir))
        $ProtectedPaths+=@(Join-Path $steamapps 'workshop/content')
    }
}
if([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    $ProtectedPaths+=@(Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/RedSawGames/DolocTown/MODS')
}
if(-not $SourceCommit -and $ExportDirectory) { $SourceCommit=(Get-Content -LiteralPath (Join-Path $ExportDirectory 'report.json') -Raw | ConvertFrom-Json).SourceCommit }
if($SourceCommit -notmatch '^[a-f0-9]{40}([a-f0-9]{24})?$') { throw 'SourceCommit must be an exact full commit OID.' }
$deliveryDotnet=Get-DotNetExe -RepoRoot $deliveryRepo
# Build the tool bytes owned by S. Neither working-tree source nor a cached DLL is the exporter authority.
$toolSnapshot=Join-Path ([IO.Path]::GetTempPath()) ('dtmapi-export-tool-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $toolSnapshot | Out-Null
$toolArchive=Join-Path $toolSnapshot 'source.tar'
& git -C $deliveryRepo -c core.hooksPath=disabled-hooks archive --format=tar -o $toolArchive $SourceCommit -- tools/public-source/DTMAPI.PublicSource Directory.Build.props global.json tools/release/dtmapi-runtime-version.props
if($LASTEXITCODE -ne 0) { throw 'Committed tool snapshot could not be read.' }
& tar -xf $toolArchive -C $toolSnapshot
if($LASTEXITCODE -ne 0) { throw 'Committed tool snapshot could not be materialized.' }
$toolProject=Join-Path $toolSnapshot 'tools/public-source/DTMAPI.PublicSource/DTMAPI.PublicSource.csproj'
& $deliveryDotnet build $toolProject -c Release --nologo -m:1
if($LASTEXITCODE -ne 0) { throw 'Committed exporter build failed.' }
$toolAssembly=Join-Path $toolSnapshot 'tools/public-source/DTMAPI.PublicSource/bin/Release/net8.0/DTMAPI.PublicSource.dll'
$deliveryArgs=@()
switch($Action) {
    {$_ -in @('Plan','Export')} {
        $deliveryArgs=@($Action.ToLowerInvariant(),'--source',$deliveryRepo,'--commit',$SourceCommit,'--output',[IO.Path]::GetFullPath($OutputRoot))
        if($PublicRepository) {$deliveryArgs+=@('--public',[IO.Path]::GetFullPath($PublicRepository))}
        if($ExportBaseCommit) {$deliveryArgs+=@('--base',$ExportBaseCommit)}
        if($ProtectedPaths.Count -gt 0) {$deliveryArgs+=@('--protected',($ProtectedPaths -join '|'))}
    }
    'PreparePr' {
        $deliveryArgs=@('prepare','--job',[IO.Path]::GetFullPath($ExportDirectory),'--public',[IO.Path]::GetFullPath($PublicRepository),'--main',$PublicMainCommit)
        if($Bootstrap) {$deliveryArgs+=@('--bootstrap','true')}
        if($DecisionsPath) {$deliveryArgs+=@('--decisions',[IO.Path]::GetFullPath($DecisionsPath))}
    }
    'Finalize' {$deliveryArgs=@('finalize','--job',[IO.Path]::GetFullPath($ExportDirectory),'--public',[IO.Path]::GetFullPath($PublicRepository),'--accepted',$AcceptedCommit,'--validation',[IO.Path]::GetFullPath($ValidationPath))}
    'Import' {$deliveryArgs=@('import','--source',$deliveryRepo,'--commit',$SourceCommit,'--public',[IO.Path]::GetFullPath($PublicRepository),'--commits',($PublicCommits -join ','),'--pr',$PullRequestUrl,'--output',[IO.Path]::GetFullPath($OutputRoot))}
}
& $deliveryDotnet $toolAssembly @deliveryArgs
if($LASTEXITCODE -ne 0) { throw "$Action failed; any job diagnostics are retained." }
