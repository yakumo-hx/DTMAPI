[CmdletBinding()]
param([switch] $NoProvision, [string] $ApiTarget = '0.5.5')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/author-sdk-release-common.ps1"
. "$PSScriptRoot/author-sdk-preparation.ps1"
. "$PSScriptRoot/author-sdk-compatibility.ps1"
$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo -NoProvision:$NoProvision
$testParent = Join-Path $repo 'tmp/test-runs'
$testRoot = Join-Path $testParent ('sdk-compatibility-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
$previousOverride = $env:DTMAPI_AUTHOR_SDK_FROZEN_ABSTRACTIONS_DLL
$checks = 0
function Assert-Compatibility([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message }; $script:checks++ }
function Expect-CompatibilityFailure([scriptblock] $Action, [string] $Message) {
    $failed = $false
    try { & $Action | Out-Null } catch { $failed = $true }
    Assert-Compatibility $failed $Message
}
try {
    $env:DTMAPI_AUTHOR_SDK_FROZEN_ABSTRACTIONS_DLL = $null
    $target = @((Get-AuthorSdkTargetCatalog -RepoRoot $repo).targets | Where-Object apiTarget -CEQ $ApiTarget)[0]
    $contract = Get-AuthorSdkContract -RepoRoot $repo -Target $target
    $output = Join-Path $testRoot 'prepared'
    $first = Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -ApiTarget $ApiTarget -DotNetExe $dotnet -OutputRoot $output -NoProvision:$NoProvision
    Assert-Compatibility (-not $first.reused) 'A fresh isolated output did not build from frozen source.'
    Assert-Compatibility ((Get-AuthorSdkSha256 -Path $first.abstractionsPath) -ceq $contract.abstractionsSha256) 'Frozen DLL bytes changed.'
    Assert-Compatibility ([Reflection.AssemblyName]::GetAssemblyName($first.abstractionsPath).Version.ToString() -ceq '0.5.3.0') 'Frozen assembly identity changed.'
    Assert-Compatibility ([Diagnostics.FileVersionInfo]::GetVersionInfo($first.abstractionsPath).FileVersion -ceq $contract.abstractionsFileVersion) 'Frozen file version changed.'
    $again = Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -ApiTarget $ApiTarget -DotNetExe $dotnet -OutputRoot $output -NoProvision -Check
    Assert-Compatibility ($again.reused -and $again.inputSha256 -ceq $first.inputSha256) 'Verified payload was not reused.'
    [IO.File]::AppendAllText($first.abstractionsPath, 'authored corruption')
    Expect-CompatibilityFailure { Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -ApiTarget $ApiTarget -DotNetExe $dotnet -OutputRoot $output -NoProvision -Check } 'Check accepted damaged compatibility output.'
    $repaired = Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -ApiTarget $ApiTarget -DotNetExe $dotnet -OutputRoot $output -NoProvision:$NoProvision
    Assert-Compatibility (-not $repaired.reused -and (Get-AuthorSdkSha256 -Path $repaired.abstractionsPath) -ceq $contract.abstractionsSha256) 'Damaged cache did not rebuild to the exact frozen bytes.'
    $propsPath = Join-Path $repaired.compatibilityRoot 'DTMAPI.Author.props'
    $manifestPath = Join-Path $repaired.compatibilityRoot 'compatibility.json'
    $receiptPath = Join-Path $output $(if ($ApiTarget -ceq '0.5.5') { 'compatibility.preparation.json' } else { "$ApiTarget.compatibility.preparation.json" })
    $saved = @{}
    foreach ($path in @($propsPath, $manifestPath, $receiptPath)) { $saved[$path] = [IO.File]::ReadAllBytes($path) }
    [IO.File]::AppendAllText($propsPath, '<!-- coherently altered local cache -->')
    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    @($manifest.files | Where-Object path -eq 'DTMAPI.Author.props')[0].sha256 = Get-AuthorSdkSha256 -Path $propsPath
    Write-AuthorSdkUtf8NoBom -Path $manifestPath -Value ($manifest | ConvertTo-Json -Depth 8)
    $receipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
    $receipt.manifestSha256 = Get-AuthorSdkSha256 -Path $manifestPath
    Write-AuthorSdkUtf8NoBom -Path $receiptPath -Value ($receipt | ConvertTo-Json -Depth 5)
    Expect-CompatibilityFailure { Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -ApiTarget $ApiTarget -DotNetExe $dotnet -OutputRoot $output -NoProvision -Check } 'Refreshing local cache hashes replaced a frozen contract anchor.'
    foreach ($path in $saved.Keys) { [IO.File]::WriteAllBytes($path, $saved[$path]) }
    $badOverride = Join-Path $testRoot 'wrong-current.dll'
    [IO.File]::WriteAllText($badOverride, 'a current or unknown DLL must not replace the frozen target')
    Expect-CompatibilityFailure { Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -ApiTarget $ApiTarget -DotNetExe $dotnet -OutputRoot $output -FrozenAbstractionsDll $badOverride -NoProvision } 'Wrong explicit DLL was admitted.'
    $overlap = Join-Path $repo ("author-sdk/compatibility/$ApiTarget/source/invalid-output")
    Expect-CompatibilityFailure { Prepare-DtmApiAuthorSdkCompatibility -RepoRoot $repo -ApiTarget $ApiTarget -DotNetExe $dotnet -OutputRoot $overlap -NoProvision } 'Output was allowed to overlap frozen source.'
    Assert-Compatibility (-not (Test-Path -LiteralPath $overlap)) 'Path refusal created an output inside frozen source.'
    $fixtureRepo = Join-Path $testRoot 'source-fixture'
    $fixtureParent = Join-Path $fixtureRepo 'author-sdk/compatibility'
    [IO.Directory]::CreateDirectory($fixtureParent) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo ("author-sdk/compatibility/$ApiTarget")) -Destination (Join-Path $fixtureParent $ApiTarget) -Recurse
    $source = Get-DtmApiFrozenCompatibilitySource -RepoRoot $fixtureRepo -Contract $contract
    # The contract-bound source manifest owns the input set for every target.
    # Its hash and the tamper/extra-file checks below supersede per-version file counts.
    $sourceFile = Join-Path $source.root 'src/DTMAPI.Abstractions/AssemblyInfo.cs'
    $bytes = [IO.File]::ReadAllBytes($sourceFile)
    [IO.File]::AppendAllText($sourceFile, '// changed')
    Expect-CompatibilityFailure { Get-DtmApiFrozenCompatibilitySource -RepoRoot $fixtureRepo -Contract $contract } 'Changed frozen source was accepted.'
    [IO.File]::WriteAllBytes($sourceFile, $bytes)
    [IO.File]::WriteAllText((Join-Path $source.root 'extra.cs'), '// extra input')
    Expect-CompatibilityFailure { Get-DtmApiFrozenCompatibilitySource -RepoRoot $fixtureRepo -Contract $contract } 'Unlisted source file was accepted.'
    Write-Host "Author SDK frozen compatibility: PASS ($checks checks; exact source rebuild, reuse, damage, input scope)"
}
finally {
    $env:DTMAPI_AUTHOR_SDK_FROZEN_ABSTRACTIONS_DLL = $previousOverride
    $safe = Assert-AuthorSdkChildPath -Root $testParent -Path $testRoot -Label 'compatibility fixture cleanup'
    Remove-Item -LiteralPath $safe -Recurse -Force
}
