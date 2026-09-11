param([switch]$KeepFixture)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/runtime-build-source.ps1"
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('DTMAPI-runtime-source-test-' + [Guid]::NewGuid().ToString('N'))
function Write-Fixture([string]$Path, [string]$Text) {
    $full = Join-Path $fixture $Path
    [IO.Directory]::CreateDirectory((Split-Path -Parent $full)) | Out-Null
    [IO.File]::WriteAllText($full, $Text)
}
function Commit-Fixture {
    & git -C $fixture add -- Directory.Build.props Directory.Build.targets DTMAPI.sln src data build .gitignore > $null
    if ($LASTEXITCODE -ne 0) { throw 'Fixture stage failed.' }
    & git -C $fixture -c user.name=DTMAPI-Test -c user.email=test@example.invalid commit -qm fixture
    if ($LASTEXITCODE -ne 0) { throw 'Fixture commit failed.' }
}
function Check-Source([switch]$SkipBuild) {
    Assert-DtmApiRuntimeBuildSource -RepoRoot $fixture -DotNetExe $dotnet -Projects @('src/Host/Host.csproj') -SkipBuild:$SkipBuild
}
function Expect-Rejection([string]$Expected, [switch]$SkipBuild) {
    $failed = $false
    try { $null = Check-Source -SkipBuild:$SkipBuild } catch {
        if ($_.Exception.Message -notlike "*$Expected*") { throw }
        $failed = $true
    }
    if (-not $failed) { throw "Expected source rejection: $Expected" }
    if ([IO.File]::ReadAllText((Join-Path $fixture 'package.bin')) -cne 'existing-package') { throw 'Failed source gate changed the previous package.' }
}
try {
    Write-Fixture '.gitignore' "**/bin/`n**/obj/`npackage.bin`nbuild-counts/`ntmp/`n"
    Write-Fixture 'Directory.Build.props' '<Project><Import Project="build/version.props" /></Project>'
    Write-Fixture 'Directory.Build.targets' @'
<Project>
  <Target Name="RecordCompilation" AfterTargets="CoreCompile">
    <MakeDir Directories="$(MSBuildThisFileDirectory)build-counts" />
    <WriteLinesToFile File="$(MSBuildThisFileDirectory)build-counts/$(MSBuildProjectName).txt" Lines="compiled" Overwrite="false" />
  </Target>
</Project>
'@
    Write-Fixture 'build/version.props' '<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>'
    Write-Fixture 'src/Host/Host.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include="../Shared/*.cs"/><Compile Include="../Product/*.cs"/><EmbeddedResource Include="../../data/*.json"/><ProjectReference Include="../Stubs/Stubs.csproj"/></ItemGroup></Project>'
    Write-Fixture 'src/Host/Main.cs' 'public class Host { public int Value => new Shared().Value; }'
    Write-Fixture 'src/Shared/Shared.cs' 'public class Shared { public int Value => 1; }'
    Write-Fixture 'src/Product/Linked.cs' 'public class Linked { }'
    Write-Fixture 'src/Stubs/Stubs.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>'
    Write-Fixture 'src/Stubs/Stub.cs' 'public class Stub { }'
    Write-Fixture 'src/Optional/Optional.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include="../Stubs/Stubs.csproj"/></ItemGroup></Project>'
    Write-Fixture 'src/Optional/Optional.cs' 'public class Optional { public Stub Value => new Stub(); }'
    Write-Fixture 'src/Unrelated/Unrelated.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><Target Name="RejectUnrelatedBuild" BeforeTargets="PrepareForBuild"><Error Text="An unrelated project was built."/></Target></Project>'
    Write-Fixture 'data/embedded.json' '{"value":1}'
    Write-Fixture 'package.bin' 'existing-package'
    & $dotnet new sln -n DTMAPI -o $fixture --force > $null
    if ($LASTEXITCODE -ne 0) { throw 'Fixture solution creation failed.' }
    & $dotnet sln (Join-Path $fixture 'DTMAPI.sln') add (Join-Path $fixture 'src/Host/Host.csproj') (Join-Path $fixture 'src/Stubs/Stubs.csproj') (Join-Path $fixture 'src/Optional/Optional.csproj') (Join-Path $fixture 'src/Unrelated/Unrelated.csproj') > $null
    if ($LASTEXITCODE -ne 0) { throw 'Fixture solution membership failed.' }
    Write-Fixture 'tools/scripts/common.ps1' (@'
function Get-RepoRoot {{ [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')) }}
function Get-DotNetExe {{ param($RepoRoot) '{0}' }}
'@ -f $dotnet.Replace("'", "''"))
    foreach ($scriptName in @('build.ps1', 'test-common.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $fixture "tools/scripts/$scriptName")
    }
    & git -C $fixture init -q
    Commit-Fixture
    $clean = Check-Source
    foreach ($required in @('Directory.Build.props','build/version.props','src/Shared/Shared.cs','src/Product/Linked.cs','data/embedded.json','src/Stubs/Stub.cs')) {
        if (-not $clean.Inputs.Contains($required)) { throw "Missing actual evaluated input: $required" }
    }
    & (Join-Path $fixture 'tools/scripts/build.ps1') -Projects @('src/Host/Host.csproj') -SkipTests
    if (-not $?) { throw 'Clean selected fixture build failed.' }
    Assert-DtmApiRuntimeBuildSourceUnchanged -RepoRoot $fixture -DotNetExe $dotnet -Before $clean
    Write-Host 'PASS clean evaluated closure and single-root build'
    $binary = Join-Path $fixture 'src/Host/bin/Release/net8.0/Host.dll'
    $originalHash = (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash
    [IO.File]::WriteAllText($binary, 'unauthenticated stale binary')
    [IO.File]::SetLastWriteTimeUtc($binary, [DateTime]::UtcNow.AddHours(1))
    foreach ($path in @(Get-ChildItem -LiteralPath (Join-Path $fixture 'build-counts') -File)) { Remove-Item -LiteralPath $path.FullName }
    & (Join-Path $fixture 'tools/scripts/build.ps1') -Projects @('src/Host/Host.csproj', 'src/Optional/Optional.csproj') -SkipTests -Rebuild
    if (-not $? -or (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -cne $originalHash) { throw 'Selected Rebuild did not replace the stale binary with source output.' }
    foreach ($name in @('Host', 'Optional', 'Stubs')) {
        $count = @(Get-Content -LiteralPath (Join-Path $fixture "build-counts/$name.txt")).Count
        if ($count -ne 1) { throw "Selected Rebuild must compile $name once; actual=$count" }
    }
    if (Test-Path -LiteralPath (Join-Path $fixture 'src/Unrelated/bin')) { throw 'Selected build touched an unrelated project.' }
    if (@(Get-ChildItem -LiteralPath (Join-Path $fixture 'tmp/unit-build-filters') -Filter '*.slnf').Count -ne 0) { throw 'Selected build left its temporary solution filter.' }
    Assert-DtmApiRuntimeBuildSourceUnchanged -RepoRoot $fixture -DotNetExe $dotnet -Before $clean
    $refused = $false
    try { & (Join-Path $fixture 'tools/scripts/build.ps1') -Projects @('src/Host/Host.csproj') } catch { $refused = $_.Exception.Message -like '*-SkipTests*' }
    if (-not $refused) { throw 'Partial build implicitly invoked the full test suite.' }
    Write-Host 'PASS two roots/shared dependency compiled once; unrelated project excluded; stale output replaced; partial test ambiguity rejected'
    $refused = $false
    try { & (Join-Path $fixture 'tools/scripts/build.ps1') -Projects @('src/Host/Host.csproj', 'src/Unrelated/Unrelated.csproj') -SkipTests } catch { $refused = $_.Exception.Message -like '*build failed*' }
    if (-not $refused) { throw 'Selected build failure was swallowed.' }
    if (@(Get-ChildItem -LiteralPath (Join-Path $fixture 'tmp/unit-build-filters') -Filter '*.slnf').Count -ne 0) { throw 'Failed build left its temporary solution filter.' }
    Write-Host 'PASS selected compiler failure propagated and temporary filter removed'
    Write-Fixture 'docs/wiki.md' 'unrelated documentation'
    $null = Check-Source
    Write-Host 'PASS unrelated documentation allowed'
    foreach ($path in @('src/Shared/Shared.cs','build/version.props','data/embedded.json','src/Product/Linked.cs')) {
        $full = Join-Path $fixture $path
        $original = [IO.File]::ReadAllText($full)
        # Keep XML/JSON valid so rejection demonstrates dirty inputs, not failed evaluation.
        [IO.File]::WriteAllText($full, $original + [Environment]::NewLine)
        Expect-Rejection $path
        [IO.File]::WriteAllText($full, $original)
        Write-Host "PASS modified $path"
    }
    foreach ($path in @('src/Shared/Added.cs','data/added.json','src/Product/Added.cs')) {
        Write-Fixture $path '// added input'
        Expect-Rejection $path
        Remove-Item -LiteralPath (Join-Path $fixture $path)
        Write-Host "PASS new glob input $path"
    }
    foreach ($path in @('src/Shared/Shared.cs','data/embedded.json','src/Product/Linked.cs')) {
        $full = Join-Path $fixture $path
        $original = [IO.File]::ReadAllText($full)
        Remove-Item -LiteralPath $full
        Expect-Rejection $path
        [IO.File]::WriteAllText($full, $original)
        Write-Host "PASS deleted glob input $path"
    }
    Write-Fixture 'src/Shared/Shared.cs' 'public class Shared { public int Value => 2; }'
    Commit-Fixture
    Expect-Rejection 'SkipBuild' -SkipBuild
    Write-Host 'PASS clean new commit cannot relabel old output through SkipBuild'
    $after = Check-Source
    Write-Fixture 'src/Shared/Late.cs' 'public class Late { }'
    $failed = $false
    try { Assert-DtmApiRuntimeBuildSourceUnchanged -RepoRoot $fixture -DotNetExe $dotnet -Before $after } catch { $failed = $true }
    if (-not $failed) { throw 'Input-set change during build was missed.' }
    Write-Host 'PASS input-set drift rejected before publication'
}
finally {
    if ($KeepFixture) { Write-Host "Fixture retained: $fixture" }
    elseif (Test-Path -LiteralPath $fixture) {
        $full = [IO.Path]::GetFullPath($fixture)
        $parent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')
        if ((Split-Path -Parent $full) -cne $parent -or (Split-Path -Leaf $full) -notmatch '^DTMAPI-runtime-source-test-[0-9a-f]{32}$') { throw 'Fixture cleanup escaped its exact session.' }
        Remove-Item -LiteralPath $full -Recurse -Force
    }
}
