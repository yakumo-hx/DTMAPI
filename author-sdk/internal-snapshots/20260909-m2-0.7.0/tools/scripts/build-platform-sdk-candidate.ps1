param(
    [Parameter(Mandatory=$true)] [string] $OutputRoot,
    [Parameter(Mandatory=$true)] [string] $OrdinarySdkDirectory
)

. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
. "$PSScriptRoot/author-sdk-preparation.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if (@((Get-AuthorSdkTargetCatalog -RepoRoot $repo).targets | Where-Object { $_.apiTarget -ceq '0.7.0' -and $_.state -ceq 'available' }).Count) { throw 'PN-007.a staging is historical after R2 freeze. Use build-author-sdk.ps1 for the available target.' }
$dotnet = Get-DotNetExe -RepoRoot $repo
$output = [IO.Path]::GetFullPath($OutputRoot)
$ordinary = [IO.Path]::GetFullPath($OrdinarySdkDirectory)
Assert-DtmApiBuildPathsDisjoint -OutputPath $output -InputPaths @((Join-Path $repo 'src'), (Join-Path $repo 'author-sdk'), (Join-Path $repo 'tools'), $ordinary)
if (Test-Path -LiteralPath $output) { throw 'Candidate output must be a fresh directory; existing evidence is never overwritten.' }
if (-not (Test-Path -LiteralPath (Join-Path $ordinary 'author-sdk-release.json'))) { throw 'Build and check the ordinary SDK first.' }
if ((Get-AuthorSdkSha256 -Path (Join-Path $ordinary 'compatibility/0.5.5/DTMAPI.Abstractions.dll')) -ne 'd04d34cd756c18189314e68891e933ceb5c060a79ba1eb54af21832e5f3f2bf8') { throw 'Ordinary frozen payload mismatch.' }
[IO.Directory]::CreateDirectory($output) | Out-Null
$source = Join-Path $output 'source'
$sdk = Join-Path $output 'DTMAPI-Author-SDK-0.2.0-candidate-win-x64'
$runtime = Join-Path $output 'runtime'
$utf8 = [Text.UTF8Encoding]::new($false)
function Write-CandidateText([string]$Path, [string]$Text) { [IO.File]::WriteAllText($Path, $Text, $utf8) }
function Edit-CandidateText([string]$Relative, [string]$Before, [string]$After) {
    $path = Join-Path $source $Relative
    $text = [IO.File]::ReadAllText($path)
    if (-not $text.Contains($Before)) { throw "Candidate source transformation no longer matches: $Relative" }
    Write-CandidateText $path ($text.Replace($Before,$After))
}

# Copy source inputs, excluding ignored bin/obj/caches. All edits below are confined
# to this fresh staging tree; no catalog override is added to the ordinary CLI.
Push-Location $repo
try {
    $inputs = @('Directory.Build.props','global.json') + @(& rg --files src author-sdk tools/release)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to enumerate candidate inputs.' }
} finally { Pop-Location }
$inputs += @('tools/scripts/common.ps1','tools/scripts/author-sdk-release-common.ps1','tools/scripts/check-author-sdk-release.ps1')
$qaProjectPath = Join-Path $repo 'src/DTMAPI.GameBridge.DolocTown.QA/DTMAPI.GameBridge.DolocTown.QA.csproj'
[xml]$qaProject = Get-Content -LiteralPath $qaProjectPath -Raw
foreach ($compile in $qaProject.SelectNodes('//Compile[@Include]')) {
    $linked = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $qaProjectPath) ([string]$compile.Include)))
    Assert-AuthorSdkChildPath -Root $repo -Path $linked -Label 'QA linked source' | Out-Null
    $inputs += [IO.Path]::GetRelativePath($repo, $linked)
}
$inventory = @()
foreach ($relative in ($inputs | Sort-Object -Unique)) {
    $from = Join-Path $repo $relative
    $to = Join-Path $source $relative
    Assert-AuthorSdkChildPath -Root $source -Path $to -Label 'Candidate source' | Out-Null
    [IO.Directory]::CreateDirectory((Split-Path -Parent $to)) | Out-Null
    Copy-Item -LiteralPath $from -Destination $to
    $inventory += [ordered]@{ path=$relative.Replace('\','/'); sha256=Get-AuthorSdkSha256 -Path $from }
}
Write-CandidateText (Join-Path $output 'source-inputs.json') (($inventory | ConvertTo-Json -Depth 4) + "`n")
Edit-CandidateText 'tools/release/dtmapi-runtime-version.props' '<DtmApiReleaseVersion>0.6.1</DtmApiReleaseVersion>' '<DtmApiReleaseVersion>0.7.0</DtmApiReleaseVersion>'
Edit-CandidateText 'tools/release/dtmapi-runtime-version.props' '<DtmApiBinaryFileVersion>0.6.1.0</DtmApiBinaryFileVersion>' '<DtmApiBinaryFileVersion>0.7.0.0</DtmApiBinaryFileVersion>'
Edit-CandidateText 'src/DTMAPI.Authoring.Contracts/AuthorContracts.cs' 'const string SdkVersion = "0.1.0"' 'const string SdkVersion = "0.2.0"'
Edit-CandidateText 'src/DTMAPI.Authoring.Contracts/AuthorContracts.cs' 'const string HighestSupportedRuntimeVersion = "0.6.1"' 'const string HighestSupportedRuntimeVersion = "0.7.0"'
foreach ($project in @('DTMAPI.AuthorSdk','DTMAPI.Authoring.Contracts','DTMAPI.InstallDoctor','DTMAPI.Tooling.Metadata')) {
    Edit-CandidateText "src/$project/$project.csproj" '0.1.0' '0.2.0'
}
Edit-CandidateText 'src/DTMAPI.AuthorSdk/AuthorApplication.cs' 'DTMAPI Author SDK 0.1.0 - frozen API targets' 'DTMAPI Author SDK 0.2.0 candidate - isolated API targets'

$cache = Join-Path $repo '.tools/author-sdk-packages'
Push-Location $source
try {
    & $dotnet build src/DTMAPI.Abstractions/DTMAPI.Abstractions.csproj -c Release --nologo "/p:RestorePackagesPath=$cache"
    if ($LASTEXITCODE -ne 0) { throw 'Candidate Abstractions build failed.' }
} finally { Pop-Location }
$abstractions = Join-Path $source 'src/DTMAPI.Abstractions/bin/Release/netstandard2.0/DTMAPI.Abstractions.dll'
$payload = Join-Path $output 'compatibility-0.7.0'
Copy-Item -LiteralPath (Join-Path $ordinary 'compatibility/0.5.5') -Destination $payload -Recurse
Copy-Item -LiteralPath $abstractions -Destination (Join-Path $payload 'DTMAPI.Abstractions.dll') -Force
$contract = Get-Content (Join-Path $source 'author-sdk/compatibility/0.5.5/compatibility.contract.json') -Raw | ConvertFrom-Json
$contract.sdkVersion = '0.2.0'
$contract.targetRuntimeVersion = '0.7.0'
$contract.abstractionsAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($abstractions).Version.ToString()
$contract.abstractionsFileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($abstractions).FileVersion
$contract.abstractionsSha256 = Get-AuthorSdkSha256 -Path $abstractions
$contract.authorPropsSha256 = Get-AuthorSdkSha256 -Path (Join-Path $payload 'DTMAPI.Author.props')
$contractDir = Join-Path $source 'author-sdk/compatibility/0.7.0'
[IO.Directory]::CreateDirectory($contractDir) | Out-Null
$contractPath = Join-Path $contractDir 'compatibility.contract.json'
Write-CandidateText $contractPath (($contract | ConvertTo-Json -Depth 8) + "`n")
$manifest = Get-Content (Join-Path $payload 'compatibility.json') -Raw | ConvertFrom-Json
$manifest.sdkVersion = $contract.sdkVersion
$manifest.targetRuntimeVersion = $contract.targetRuntimeVersion
$manifest.abstractionsFileVersion = $contract.abstractionsFileVersion
foreach ($file in $manifest.files) { $file.sha256 = Get-AuthorSdkSha256 -Path (Join-Path $payload $file.path) }
Write-CandidateText (Join-Path $payload 'compatibility.json') (($manifest | ConvertTo-Json -Depth 8) + "`n")
$catalogPath = Join-Path $source 'author-sdk/target-catalog.json'
$catalog = Get-Content $catalogPath -Raw | ConvertFrom-Json
$old = $catalog.targets | Where-Object apiTarget -eq '0.5.5'
$old.sdkVersions = @('0.1.0','0.2.0')
$next = $catalog.targets | Where-Object apiTarget -eq '0.7.0'
$next.state = 'available'
$next.contractSha256 = Get-AuthorSdkSha256 -Path $contractPath
$next.capabilities = @('optional-helper-services/1','runtime-context/1','scheduler/1','owned-resources/1','commands/1','owner-files/1','global-data/1','versioned-config/1','input-diagnostics/1','translations/1')
Write-CandidateText $catalogPath (($catalog | ConvertTo-Json -Depth 8) + "`n")
Edit-CandidateText 'src/DTMAPI.AuthorSdk/DTMAPI.AuthorSdk.csproj' '</Project>' @'
  <ItemGroup>
    <EmbeddedResource Include="../../author-sdk/compatibility/0.7.0/compatibility.contract.json" LogicalName="DTMAPI.AuthorSdk.compatibility.0.7.0.contract.json" />
  </ItemGroup>
</Project>
'@

Push-Location $source
try {
    & $dotnet publish src/DTMAPI.AuthorSdk/DTMAPI.AuthorSdk.csproj -c Release -r win-x64 --self-contained true --nologo -o $sdk "/p:RestorePackagesPath=$cache" '/p:DebugType=None' '/p:DebugSymbols=false'
    if ($LASTEXITCODE -ne 0) { throw 'Candidate SDK publish failed.' }
    foreach ($project in @('DTMAPI.BepInExBootstrap','DTMAPI.GameBridge.DolocTown.QA')) {
        & $dotnet build "src/$project/$project.csproj" -c Release --nologo "/p:RestorePackagesPath=$cache"
        if ($LASTEXITCODE -ne 0) { throw "Candidate Runtime build failed: $project" }
    }
} finally { Pop-Location }
Copy-Item -LiteralPath (Join-Path $ordinary 'licenses') -Destination (Join-Path $sdk 'licenses') -Recurse
[IO.Directory]::CreateDirectory((Join-Path $sdk 'compatibility')) | Out-Null
$legacyDestination = Join-Path $sdk 'compatibility/0.5.5'
[IO.Directory]::CreateDirectory($legacyDestination) | Out-Null
foreach ($item in Get-ChildItem -LiteralPath (Join-Path $ordinary 'compatibility/0.5.5')) {
    Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $legacyDestination $item.Name) -Recurse -Force
}
Copy-Item -LiteralPath $payload -Destination (Join-Path $sdk 'compatibility/0.7.0') -Recurse
[IO.Directory]::CreateDirectory((Join-Path $sdk 'contracts')) | Out-Null
Copy-Item -LiteralPath $contractPath -Destination (Join-Path $sdk 'contracts/compatibility-0.7.0.contract.json')
Copy-Item -LiteralPath (Join-Path $source 'author-sdk/compatibility/0.5.5/compatibility.contract.json') -Destination (Join-Path $sdk 'contracts/compatibility-0.5.5.contract.json')
[IO.Directory]::CreateDirectory($runtime) | Out-Null
foreach ($name in @('DTMAPI.Abstractions','DTMAPI.Core','DTMAPI.BepInExBootstrap','DTMAPI.GameBridge.DolocTown','DTMAPI.ModConfigMenu','DTMAPI.GameBridge.DolocTown.QA')) {
    Copy-Item -LiteralPath (Join-Path $source "src/$name/bin/Release/netstandard2.0/$name.dll") -Destination $runtime
}
$release = New-AuthorSdkReleaseInventory -StageRoot $sdk -DotNetSdkVersion '8.0.421' -SdkVersion '0.2.0' -TargetCatalog $catalog
Write-CandidateText (Join-Path $sdk 'author-sdk-release.json') (($release | ConvertTo-Json -Depth 10) + "`n")
$zip = Join-Path $output 'DTMAPI-Author-SDK-0.2.0-candidate-win-x64.zip'
New-AuthorSdkDeterministicZip -SourceRoot $sdk -ZipPath $zip | Out-Null
Write-CandidateText ($zip + '.sha256') ((Get-AuthorSdkSha256 $zip) + ' *' + [IO.Path]::GetFileName($zip) + "`n")
& (Join-Path $source 'tools/scripts/check-author-sdk-release.ps1') -PackagePath $zip
if (-not $?) { throw 'Isolated candidate package checks failed.' }
$receipt = [ordered]@{ candidateOnly=$true; sdkVersion='0.2.0'; apiTarget='0.7.0'; runtimeVersion='0.7.0'; sdkDirectory=$sdk; sourceDirectory=$source; runtimeDirectory=$runtime; zipPath=$zip; zipSha256=Get-AuthorSdkSha256 $zip; contractSha256=$next.contractSha256; sourceInputSha256=Get-AuthorSdkSha256 (Join-Path $output 'source-inputs.json'); runtime=@(Get-ChildItem $runtime -File | ForEach-Object { @{ name=$_.Name; sha256=Get-AuthorSdkSha256 $_.FullName } }); ordinaryCatalogSha256=Get-AuthorSdkSha256 (Join-Path $repo 'author-sdk/target-catalog.json') }
Write-CandidateText (Join-Path $output 'candidate.json') (($receipt | ConvertTo-Json -Depth 8) + "`n")
Write-Output "Candidate SDK and Runtime ready: $output"
