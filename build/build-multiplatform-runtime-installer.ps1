[CmdletBinding()]
param([Parameter(Mandatory = $true)] [string] $OutputRoot)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/windows-installer/common.ps1"
. "$PSScriptRoot/windows-installer/release-common.ps1"
. "$PSScriptRoot/multiplatform-package-common.ps1"
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$project = 'src/DTMAPI.MultiPlatformInstaller/DTMAPI.MultiPlatformInstaller.csproj'
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw 'Installer build requires a new independent output directory.' }
$commit = Invoke-DtmApiGitValue -RepoRoot $repo -Arguments @('rev-parse','HEAD')
[xml]$projectXml = Get-Content -LiteralPath (Join-Path $repo $project) -Raw
$version = [string]$projectXml.Project.PropertyGroup.InformationalVersion
if ($version -cne '0.2.0-experimental') { throw 'Unexpected installer version.' }
$hosts = @()
foreach ($rid in @('win-x64','linux-x64')) {
    $destination = Join-Path $output $rid
    & $dotnet publish (Join-Path $repo $project) -c Release -r $rid --self-contained true -o $destination "-p:InformationalVersion=$version+$commit"
    if ($LASTEXITCODE -ne 0) { throw "Installer publish failed: $rid" }
    $name = if ($rid -eq 'win-x64') { 'DTMAPI-MultiPlatform-Installer.exe' } else { 'DTMAPI-MultiPlatform-Installer' }
    $file = Join-Path $destination $name
    $hosts += [ordered]@{ Rid = $rid; RelativePath = "$rid/$name"; Length = (Get-Item -LiteralPath $file).Length; Sha256 = Get-DtmApiMultiPlatformFileSha256 -Path $file }
}
$receipt = [ordered]@{ SchemaVersion = 1; Kind = 'CommittedInstallerBuild'; Commit = $commit; InstallerVersion = $version; Hosts = $hosts }
Write-DtmApiMultiPlatformJsonNoBom -Path (Join-Path $output 'installer-build.json') -Value $receipt
[pscustomobject]$receipt
