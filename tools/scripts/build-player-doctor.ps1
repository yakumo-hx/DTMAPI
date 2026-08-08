param(
    [string] $Configuration = 'Release',
    [string] $OutputRoot = ''
)

. "$PSScriptRoot\common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\player-doctor\win-x64'
}
$output = [System.IO.Path]::GetFullPath($OutputRoot)
$allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'dist\player-doctor'))
if (-not (Test-DtmApiPathIsSameOrChild -Child $output -Parent $allowedRoot)) {
    throw "Player Doctor output must remain under $allowedRoot. Requested: $output"
}
if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}
New-Item -ItemType Directory -Path $output -Force | Out-Null

$project = Join-Path $repo 'src\DTMAPI.PlayerDoctor\DTMAPI.PlayerDoctor.csproj'
& $dotnet publish $project -c $Configuration -r win-x64 --self-contained true --nologo -o $output `
    '/p:PublishSingleFile=true' '/p:IncludeNativeLibrariesForSelfExtract=true' '/p:PublishTrimmed=false' `
    '/p:DebugType=None' '/p:DebugSymbols=false'
if ($LASTEXITCODE -ne 0) { throw 'Failed to publish the self-contained Player Doctor.' }

Get-ChildItem -LiteralPath $output -Filter '*.pdb' -File -Recurse | Remove-Item -Force
$dotnetRoot = Split-Path -Parent $dotnet
Copy-Item -LiteralPath (Join-Path $dotnetRoot 'LICENSE.txt') -Destination (Join-Path $output 'dotnet-LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $dotnetRoot 'ThirdPartyNotices.txt') -Destination (Join-Path $output 'dotnet-ThirdPartyNotices.txt') -Force

& "$PSScriptRoot\check-player-doctor-release.ps1" -PackageRoot $output
if (-not $?) { throw 'Player Doctor release gate failed.' }
Write-Host "Player Doctor release written to $output"
