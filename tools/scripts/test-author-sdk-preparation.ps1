[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
. "$PSScriptRoot/author-sdk-preparation.ps1"
$repo = Get-RepoRoot
$testRoot = Join-Path $repo ('temp/sdk-preparation-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$checks = 0
function Assert-Preparation([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}
function Write-TestInput([string] $Path, [string] $Value) { Write-AuthorSdkUtf8NoBom -Path (Join-Path $testRoot $Path) -Value $Value }
try {
    foreach ($name in @('DTMAPI.AuthorSdk', 'DTMAPI.Abstractions', 'Transitive')) {
        $references = if ($name -eq 'DTMAPI.AuthorSdk') { '<ItemGroup><ProjectReference Include="../Transitive/Transitive.csproj" /><Content Include="../../author-sdk/templates/**/*" /></ItemGroup>' } else { '' }
        Write-TestInput "src/$name/$name.csproj" ('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>' + $references + '</Project>')
        Write-TestInput "src/$name/Input.cs" '// initial source'
    }
    foreach ($name in @('build-author-sdk.ps1', 'prepare-author-sdk.ps1', 'author-sdk-preparation.ps1', 'author-sdk-release-common.ps1', 'author-sdk-compatibility.ps1', 'prepare-author-sdk-compatibility.ps1', 'check-author-sdk-release.ps1', 'common.ps1')) { Write-TestInput "tools/scripts/$name" '# tool input' }
    Write-TestInput 'author-sdk/templates/template.txt' 'template'
    Write-TestInput 'Directory.Build.props' '<Project />'
    $dotnet = Get-DotNetExe
    $original = Get-DtmApiAuthorSdkInput -RepoRoot $testRoot -DotNetExe $dotnet
    Write-TestInput 'products/first-party/Example/src/Mod.cs' '// ordinary product change'
    $productChange = Get-DtmApiAuthorSdkInput -RepoRoot $testRoot -DotNetExe $dotnet
    Assert-Preparation ($original.sha256 -ceq $productChange.sha256) 'Ordinary product source invalidated the SDK.'
    Write-TestInput 'author-sdk/examples/Hello/bin/generated.dll' 'not an SDK input'
    Write-TestInput 'author-sdk/samples/api-demand/AutoHarvest/obj/project.assets.json' '{}'
    Write-TestInput 'author-sdk/samples/api-demand/AutoHarvest/src/Mod.cs' '// non-shipped sample source'
    $sampleChange = Get-DtmApiAuthorSdkInput -RepoRoot $testRoot -DotNetExe $dotnet
    Assert-Preparation ($original.sha256 -ceq $sampleChange.sha256) 'Example/sample builds or source invalidated the SDK.'
    foreach ($case in @(
        @{ Path='src/DTMAPI.AuthorSdk/Added.cs'; Value='// newly compiled input' },
        @{ Path='src/Transitive/Input.cs'; Value='// changed transitive input' },
        @{ Path='Directory.Build.props'; Value='<Project><PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>' },
        @{ Path='author-sdk/templates/template.txt'; Value='changed packaged asset' },
        @{ Path='tools/scripts/build-author-sdk.ps1'; Value='# changed build tool' }
    )) {
        Write-TestInput $case.Path $case.Value
        $next = Get-DtmApiAuthorSdkInput -RepoRoot $testRoot -DotNetExe $dotnet
        Assert-Preparation ($next.sha256 -cne $productChange.sha256) "SDK input did not invalidate: $($case.Path)"
        $productChange = $next
    }
    $output = Join-Path $testRoot 'cache'
    $stage = Join-Path $output 'sdk'
    Write-TestInput 'cache/sdk/dtmapi-author.exe' 'synthetic executable'
    $inventory = @{files=@(@{path='dtmapi-author.exe';length=(Get-Item -LiteralPath (Join-Path $stage 'dtmapi-author.exe')).Length;sha256=Get-AuthorSdkSha256 -Path (Join-Path $stage 'dtmapi-author.exe')})}
    Write-TestInput 'cache/sdk/author-sdk-release.json' ($inventory | ConvertTo-Json -Depth 5)
    Write-TestInput 'cache/preparation.json' (@{schemaVersion=1;inputSha256=$original.sha256;stageRoot=$stage;inventorySha256=Get-AuthorSdkSha256 -Path (Join-Path $stage 'author-sdk-release.json')} | ConvertTo-Json)
    Assert-Preparation (Test-DtmApiPreparedAuthorSdk -OutputRoot $output -InputSnapshot $original) 'Valid prepared SDK could not be reused.'
    Assert-Preparation (-not (Test-DtmApiPreparedAuthorSdk -OutputRoot $output -InputSnapshot $productChange)) 'Stale prepared SDK was reused.'
    Write-TestInput 'cache/sdk/dtmapi-author.exe' 'damaged executable!!'
    Assert-Preparation (-not (Test-DtmApiPreparedAuthorSdk -OutputRoot $output -InputSnapshot $original)) 'Damaged SDK output was reused.'
    foreach ($target in @($testRoot, (Join-Path $testRoot 'src'), (Join-Path $testRoot 'src/DTMAPI.AuthorSdk/child'))) {
        $refused = $false
        try { Assert-DtmApiBuildPathsDisjoint -OutputPath $target -InputPaths @((Join-Path $testRoot 'src/DTMAPI.AuthorSdk')) } catch { $refused = $true }
        Assert-Preparation $refused "Overlapping output was accepted: $target"
    }
    Assert-Preparation (Test-Path -LiteralPath (Join-Path $testRoot 'src/DTMAPI.AuthorSdk/Input.cs')) 'Path refusal damaged source.'
    Write-Host "Author SDK preparation behavior: PASS ($checks checks)"
}
finally {
    $safe = Assert-AuthorSdkChildPath -Root (Join-Path $repo 'temp') -Path $testRoot -Label 'preparation test cleanup'
    Remove-Item -LiteralPath $safe -Recurse -Force
}
