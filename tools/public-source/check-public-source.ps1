param([string]$TreeCommit='HEAD',[Parameter(Mandatory=$true)][string]$PolicyCommit)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/../scripts/common.ps1"
$checkRepo=Get-RepoRoot
$checkDotnet=Get-DotNetExe -RepoRoot $checkRepo
# CI selects the trusted base policy and tool, never the pull request's proposed checker.
$checkSnapshot=Join-Path ([IO.Path]::GetTempPath()) ('dtmapi-boundary-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $checkSnapshot | Out-Null
& git -C $checkRepo archive --format=tar -o (Join-Path $checkSnapshot 'source.tar') $PolicyCommit -- tools/public-source/DTMAPI.PublicSource Directory.Build.props global.json tools/release/dtmapi-runtime-version.props
if($LASTEXITCODE -ne 0){throw 'Cannot read trusted boundary tool.'}
& tar -xf (Join-Path $checkSnapshot 'source.tar') -C $checkSnapshot
if($LASTEXITCODE -ne 0){throw 'Cannot materialize trusted boundary tool.'}
& $checkDotnet build (Join-Path $checkSnapshot 'tools/public-source/DTMAPI.PublicSource/DTMAPI.PublicSource.csproj') -c Release --nologo -m:1
if($LASTEXITCODE -ne 0){throw 'Trusted boundary tool build failed.'}
$resolvedTree=(& git -C $checkRepo rev-parse ($TreeCommit+'^{commit}')).Trim()
$resolvedPolicy=(& git -C $checkRepo rev-parse ($PolicyCommit+'^{commit}')).Trim()
& $checkDotnet (Join-Path $checkSnapshot 'tools/public-source/DTMAPI.PublicSource/bin/Release/net8.0/DTMAPI.PublicSource.dll') check --repository $checkRepo --tree-commit $resolvedTree --policy-commit $resolvedPolicy
if($LASTEXITCODE -ne 0){throw 'Public boundary validation failed.'}
