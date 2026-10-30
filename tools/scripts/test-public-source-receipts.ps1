param()
$ErrorActionPreference='Stop'
. "$PSScriptRoot/common.ps1"
$repo=Get-RepoRoot
$dotnet=Get-DotNetExe -RepoRoot $repo
$shellExe=Get-DtmApiPowerShellHost -RequireWindowsPowerShell
$root=Join-Path $repo ('tmp/public-receipt-tests/'+[Guid]::NewGuid().ToString('N'))
$fixture=Join-Path $root 'repository'
$external=Join-Path $root 'external'
$utf8=New-Object Text.UTF8Encoding($false)
$count=0
function Write-Fixture([string]$Path,[string]$Value) {
    New-Item -ItemType Directory -Path (Split-Path $Path) -Force | Out-Null
    [IO.File]::WriteAllText($Path,$Value,$utf8)
}
function Invoke-Receipt([string]$Name,[switch]$NoBuild,[switch]$Rejected,[switch]$Invalidated,[string]$SuiteId='') {
    $log=Join-Path $root ($Name+'.log')
    $report=Join-Path $root ($Name+'.json')
    $arguments=@('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',"$fixture/tools/scripts/test-public-source.ps1",'-Profile','Generic','-ReportPath',$report)
    if($NoBuild){$arguments+='-NoBuild'}
    if($SuiteId){$arguments+=@('-SuiteId',$SuiteId)}
    $startCommit=(& git -C $fixture rev-parse HEAD).Trim()
    Push-Location -LiteralPath $fixture
    $previousErrorAction=$ErrorActionPreference
    try {
        # Windows PowerShell 5.1 turns redirected native stderr into ErrorRecord.
        # Capture the child exit/report before deciding whether rejection was expected.
        $ErrorActionPreference='Continue'
        & $shellExe @arguments *> $log
        $code=$LASTEXITCODE
    } finally {$ErrorActionPreference=$previousErrorAction; Pop-Location}
    $output=Get-Content -LiteralPath $log -Raw
    if($Rejected) {
        $receipt=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        $expected=if($Name -eq 'missing-public-build-Generic-Release.json') {
            'NoBuild requires a matching prior Public source build receipt.'
        } else { 'NoBuild selected inputs or outputs changed; rebuild the selected suites.' }
        if($receipt.Status -cne 'failed' -or $receipt.SourceCommit -cne $startCommit -or
           @($receipt.Results).Count -ne 0 -or @($receipt.Failures) -cnotcontains $expected) {
            throw "Receipt rejection did not report its expected boundary: $Name; see $report"
        }
    }
    if($Invalidated) {
        $receipt=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if($code -eq 0 -or $output -notmatch 'ACTUAL.*source=1' -or $receipt.Status -cne 'failed' -or
           $receipt.SourceCommit -cne $startCommit -or $receipt.IdentityUnchanged -ne $false -or
           $receipt.Results[0].Status -cne 'invalidated' -or $receipt.Results[0].ExecutionStatus -cne 'passed') {
            throw "Concurrent mutation was not rejected with its actual start identity: $Name; see $log"
        }
        if($Name -eq 'during-commit' -and ($receipt.EndSource.Commit -ceq $startCommit -or $receipt.EndSource.Dirty)) {
            throw 'The concurrent commit test must actually advance HEAD and leave a clean tree.'
        }
        $script:count++
        return
    }
    if(($Rejected -and ($code -eq 0 -or $output -match 'ACTUAL')) -or
       (-not $Rejected -and ($code -ne 0 -or $output -notmatch 'ACTUAL embedded=ORIGINAL; linked=1; dependency=1'))) {
        throw "Receipt case failed: $Name exit=$code; see $log"
    }
    $script:count++
}
New-Item -ItemType Directory -Path "$fixture/tools/scripts","$fixture/.tools","$external" -Force | Out-Null
foreach($name in @('common.ps1','test-common.ps1','test-registry.ps1','test-public-source.ps1')) {
    Copy-Item -LiteralPath "$repo/tools/scripts/$name" -Destination "$fixture/tools/scripts/$name"
}
foreach($name in @('common','test-support')) {Copy-Item -LiteralPath "$repo/tools/scripts/$name" -Destination "$fixture/tools/scripts/$name" -Recurse}
Copy-Item -LiteralPath "$repo/global.json" -Destination "$fixture/global.json"
New-Item -ItemType Junction -Path "$fixture/.tools/dotnet" -Target (Split-Path $dotnet) | Out-Null
Write-Fixture "$fixture/.gitignore" "**/bin/`n**/obj/`ntmp/`n.tools/`n"
Write-Fixture "$external/fixture.props" '<Project><PropertyGroup><DefineConstants>$(DefineConstants);$(DTMAPI_RECEIPT_CONDITION)</DefineConstants></PropertyGroup></Project>'
Write-Fixture "$external/shared.cs" 'internal static class LinkedValue { public const int Number = 1; }'
Write-Fixture "$external/copied.dat" 'COPIED'
Write-Fixture "$external/globbed/Feature.cs" 'internal class Feature {}'
Write-Fixture "$external/Library/Library.csproj" '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>'
Write-Fixture "$external/Library/Value.cs" 'public static class DependencyValue { public const int Number = 1; }'
Write-Fixture "$fixture/tests/ReceiptProbe/ReceiptProbe.csproj" @'
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="../../../external/fixture.props" />
  <PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="payload.txt" />
    <Compile Include="../../../external/shared.cs" Link="shared.cs" />
    <Compile Include="../../../external/globbed/*.cs" Exclude="../../../external/globbed/Excluded.cs" />
    <Content Include="../../../external/copied.dat" Link="copied.dat" CopyToOutputDirectory="PreserveNewest" />
    <ProjectReference Include="../../../external/Library/Library.csproj" />
  </ItemGroup>
</Project>
'@
Write-Fixture "$fixture/tests/ReceiptProbe/payload.txt" 'ORIGINAL'
Write-Fixture "$fixture/tests/ReceiptProbe/SourceValue.cs" 'internal static class SourceValue { public const int Number = 1; }'
Write-Fixture "$fixture/tests/ReceiptProbe/Program.cs" @'
using System.Diagnostics;
using System.Reflection;
using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ReceiptProbe.payload.txt")!;
using var reader = new StreamReader(stream);
Console.WriteLine("ACTUAL embedded=" + reader.ReadToEnd() + "; linked=" + LinkedValue.Number + "; dependency=" + DependencyValue.Number + "; source=" + SourceValue.Number);
string mode = Environment.GetEnvironmentVariable("DTMAPI_RECEIPT_MUTATION") ?? "";
if (mode == "dirty" || mode == "commit") {
    File.WriteAllText("tests/ReceiptProbe/SourceValue.cs", "internal static class SourceValue { public const int Number = 2; }");
    if (mode == "commit") {
        void Git(params string[] arguments) {
            var start = new ProcessStartInfo("git") { UseShellExecute = false, CreateNoWindow = true };
            foreach (string argument in arguments) start.ArgumentList.Add(argument);
            using var child = Process.Start(start)!;
            child.WaitForExit();
            if (child.ExitCode != 0) throw new Exception("Fixture commit failed");
        }
        Git("add", "tests/ReceiptProbe/SourceValue.cs");
        Git("commit", "-qm", "Advance source while the previous binary is executing");
    }
}
if (mode == "output") File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "copied.dat"), "REPLACED DURING EXECUTION");
'@
Write-Fixture "$fixture/tests/Other/Other.csproj" '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>'
Write-Fixture "$fixture/tests/Other/Other.cs" 'public class Other {}'
$other=@{id='other';runner='build-only';entry='tests/Other/Other.csproj';project='tests/Other/Other.csproj';distribution='public';platforms=@('Windows','Linux');requires=@('DotNet8');arguments=@();purpose='Independent project';enabledByDefault=$false}
$suite=@{id='receipt-probe';runner='existing-exe-focus';entry='tests/ReceiptProbe/ReceiptProbe.csproj';project='tests/ReceiptProbe/ReceiptProbe.csproj';distribution='public';platforms=@('Windows','Linux');requires=@('DotNet8');arguments=@();purpose='Real build/executable receipt boundary';enabledByDefault=$true;inputs=@('data/')}
Write-Fixture "$fixture/data/probe.json" '{}'
$support=@(Get-ChildItem -LiteralPath "$fixture/tools/scripts" -Filter 'test*.ps1' -File | ForEach-Object {@{path='tools/scripts/'+$_.Name;distribution='public';purpose='Actual runner and dependencies'}})
Write-Fixture "$fixture/tests/DTMAPI.UnitTests/suites.json" (@{schemaVersion=2;suites=@($suite,$other);supportingInputs=$support}|ConvertTo-Json -Depth 12)
& git -C $fixture init -q --initial-branch=main
& git -C $fixture config user.name 'DTMAPI Receipt Tests'
& git -C $fixture config user.email 'tests@dtmapi.invalid'
& git -C $fixture add .
& git -C $fixture commit -qm 'Receipt test input'
if($LASTEXITCODE -ne 0){throw 'Cannot commit receipt fixture.'}
Invoke-Receipt 'baseline'
. "$PSScriptRoot/test-support/build-receipt.ps1"
foreach($case in @(
    @{Path='tests/ReceiptProbe/Added.cs';Expected='receipt-probe'},
    @{Path='../external/Library/Value.cs';Expected='receipt-probe'},
    @{Path='tests/Other/Other.cs';Expected='other'},
    @{Path='data/probe.json';Expected='receipt-probe'},
    @{Path='docs/Review.md';Expected=''},
    @{Path='Directory.Build.props';Expected='receipt-probe,other'}
)) {
    $selected=@(Select-DtmApiAffectedPublicTests -RepoRoot $fixture -Selection @([pscustomobject]$suite,[pscustomobject]$other) -ChangedFiles @($case.Path) -Configuration Release -DotNetExe $dotnet)
    $ids=(@($selected | ForEach-Object {$_.id}) -join ',')
    if($ids -cne $case.Expected){throw "Affected selection mismatch for $($case.Path): $ids"}
    $count++
}
$deletedPath="$external/globbed/Feature.cs"
$deletedBytes=[IO.File]::ReadAllBytes($deletedPath)
try {
    Remove-Item -LiteralPath $deletedPath
    foreach($case in @(@{Path='../external/globbed/Feature.cs';Expected='receipt-probe'},@{Path='../external/globbed/Excluded.cs';Expected=''})) {
        $selected=@(Select-DtmApiAffectedPublicTests -RepoRoot $fixture -Selection @([pscustomobject]$suite,[pscustomobject]$other) -ChangedFiles @($case.Path) -Configuration Release -DotNetExe $dotnet)
        if((@($selected | ForEach-Object {$_.id}) -join ',') -cne $case.Expected){throw "Deleted/excluded glob input selection mismatch: $($case.Path)"}
        $count++
    }
} finally {[IO.File]::WriteAllBytes($deletedPath,$deletedBytes)}
# The real export self-test consumes policy at runtime, outside its project directory.
. "$PSScriptRoot/test-registry.ps1"
$exportSuite=@((Read-DtmApiTestRegistry -RepoRoot $repo).suites | Where-Object {$_.id -eq 'public-source-export'})
$selected=@(Select-DtmApiAffectedPublicTests -RepoRoot $repo -Selection $exportSuite -ChangedFiles @('tools/public-source/export-policy.json') -Configuration Release -DotNetExe $dotnet)
if($selected.Count -ne 1 -or $selected[0].id -ne 'public-source-export'){throw 'Export policy change must select its consumer.'}
if(@(Get-DtmApiPublicReceiptPaths -RepoRoot $repo -Selection $exportSuite) -notcontains (Join-Path $repo 'tools/public-source/export-policy.json')){throw 'Export policy must be included in the receipt.'}
$count+=2
Invoke-Receipt 'matching-nobuild' -NoBuild
Invoke-Receipt 'selected-build' -SuiteId receipt-probe
Invoke-Receipt 'selected-nobuild' -SuiteId receipt-probe -NoBuild
Invoke-Receipt 'full-receipt-retained' -NoBuild
$dependencyBinary="$external/Library/bin/Release/net8.0/Library.dll"
$dependencyBytes=[IO.File]::ReadAllBytes($dependencyBinary)
try {
    [IO.File]::WriteAllText($dependencyBinary,'changed dependency output')
    Invoke-Receipt 'related-project-output' -NoBuild -Rejected
} finally {[IO.File]::WriteAllBytes($dependencyBinary,$dependencyBytes)}
foreach($path in @("$fixture/tests/ReceiptProbe/payload.txt","$external/copied.dat","$external/shared.cs","$external/Library/Value.cs","$external/fixture.props","$fixture/data/probe.json")) {
    $original=[IO.File]::ReadAllBytes($path)
    try {
        [IO.File]::AppendAllText($path,"`n ",$utf8)
        Invoke-Receipt ('changed-'+[IO.Path]::GetFileName($path)) -NoBuild -Rejected
    } finally {[IO.File]::WriteAllBytes($path,$original)}
}
$previousCondition=$env:DTMAPI_RECEIPT_CONDITION
try {
    $env:DTMAPI_RECEIPT_CONDITION='CHANGED_CONFIGURATION'
    Invoke-Receipt 'condition' -NoBuild -Rejected
} finally {$env:DTMAPI_RECEIPT_CONDITION=$previousCondition}
$outputPath="$fixture/tests/ReceiptProbe/bin/Release/net8.0/copied.dat"
$original=[IO.File]::ReadAllBytes($outputPath)
try {
    Write-Fixture $outputPath 'TAMPERED'
    Invoke-Receipt 'tampered-output' -NoBuild -Rejected
} finally {[IO.File]::WriteAllBytes($outputPath,$original)}
foreach($path in @("$fixture/tests/ReceiptProbe/bin/Release/net8.0/ReceiptProbe.dll","$fixture/tmp/public-build-Generic-Release.json")) {
    $original=[IO.File]::ReadAllBytes($path)
    try {
        Remove-Item -LiteralPath $path
        Invoke-Receipt ('missing-'+[IO.Path]::GetFileName($path)) -NoBuild -Rejected
    } finally {[IO.File]::WriteAllBytes($path,$original)}
}
Invoke-Receipt 'matching-after-rejections' -NoBuild
& git -C $fixture commit --allow-empty -qm 'Same files at a different source commit'
if($LASTEXITCODE -ne 0){throw 'Cannot advance fixture HEAD.'}
Invoke-Receipt 'empty-commit-nobuild' -NoBuild
Write-Fixture "$fixture/docs/Review.md" 'unrelated review'
Invoke-Receipt 'unrelated-review' -NoBuild
& git -C $fixture add docs/Review.md
Invoke-Receipt 'unrelated-staging' -NoBuild
& git -C $fixture commit -qm 'Unrelated review'
Invoke-Receipt 'unrelated-commit' -NoBuild
Write-Fixture "$fixture/tests/Other/bin/Release/net8.0/Other.dll" 'OTHER OUTPUT'
Invoke-Receipt 'unrelated-output' -NoBuild
$previousMutation=$env:DTMAPI_RECEIPT_MUTATION
foreach($mode in @('dirty','output','commit')) {
    $path=if($mode -eq 'output'){$outputPath}else{"$fixture/tests/ReceiptProbe/SourceValue.cs"}
    $original=[IO.File]::ReadAllBytes($path)
    try {
        $env:DTMAPI_RECEIPT_MUTATION=$mode
        Invoke-Receipt ('during-'+$mode) -NoBuild -Invalidated
    } finally {
        $env:DTMAPI_RECEIPT_MUTATION=$previousMutation
        if($mode -ne 'commit'){[IO.File]::WriteAllBytes($path,$original)}
    }
}
Write-Host "PASS: $count public build receipt cases. Evidence: $root"
