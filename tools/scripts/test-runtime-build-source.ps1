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
    & git -C $fixture add -- Directory.Build.props src data build .gitignore > $null
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
    Write-Fixture '.gitignore' "**/bin/`n**/obj/`npackage.bin`n"
    Write-Fixture 'Directory.Build.props' '<Project><Import Project="build/version.props" /></Project>'
    Write-Fixture 'build/version.props' '<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>'
    Write-Fixture 'src/Host/Host.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include="../Shared/*.cs"/><Compile Include="../Product/*.cs"/><EmbeddedResource Include="../../data/*.json"/><ProjectReference Include="../Stubs/Stubs.csproj"/></ItemGroup></Project>'
    Write-Fixture 'src/Host/Main.cs' 'public class Host { public int Value => new Shared().Value; }'
    Write-Fixture 'src/Shared/Shared.cs' 'public class Shared { public int Value => 1; }'
    Write-Fixture 'src/Product/Linked.cs' 'public class Linked { }'
    Write-Fixture 'src/Stubs/Stubs.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>'
    Write-Fixture 'src/Stubs/Stub.cs' 'public class Stub { }'
    Write-Fixture 'data/embedded.json' '{"value":1}'
    Write-Fixture 'package.bin' 'existing-package'
    & git -C $fixture init -q
    Commit-Fixture
    $clean = Check-Source
    foreach ($required in @('Directory.Build.props','build/version.props','src/Shared/Shared.cs','src/Product/Linked.cs','data/embedded.json','src/Stubs/Stub.cs')) {
        if (-not $clean.Inputs.Contains($required)) { throw "Missing actual evaluated input: $required" }
    }
    & $dotnet build (Join-Path $fixture 'src/Host/Host.csproj') -c Release --nologo -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Clean fixture build failed.' }
    Assert-DtmApiRuntimeBuildSourceUnchanged -RepoRoot $fixture -DotNetExe $dotnet -Before $clean
    Write-Host 'PASS clean evaluated closure and real output'
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
