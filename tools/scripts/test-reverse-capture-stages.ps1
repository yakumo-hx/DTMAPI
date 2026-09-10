[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/reverse-capture-state.ps1"
. "$PSScriptRoot/steam-appmanifest-identity.ps1"
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testParent = [IO.Path]::GetFullPath((Join-Path $repo 'references/doloc-town/reverse/stage-tests'))
$testRoot = Resolve-ReverseChild $testParent ([Guid]::NewGuid().ToString('N'))

function Assert-Stage([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }
function Write-Fixture([string] $Path, [string] $Text) {
    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

try {
    if ($PSVersionTable.PSVersion.Major -le 5) {
        $metadataGame = Join-Path $testRoot 'metadata/game'
        $metadataManaged = Join-Path $metadataGame 'DolocTown_Data/Managed'
        [IO.Directory]::CreateDirectory($metadataManaged) | Out-Null
        $metadataAssembly = Join-Path $metadataManaged 'Fixture.Metadata.dll'
        Add-Type -TypeDefinition ('public sealed class ReferenceFixture' + [Guid]::NewGuid().ToString('N') + ' { public int Value; }') -OutputAssembly $metadataAssembly
        Copy-Item -LiteralPath $metadataAssembly -Destination (Join-Path $metadataManaged 'Unreferenced.dll')
        $metadataRows = @(Get-ReverseInventory $metadataGame)
        $firstDependencies = Get-ReverseManagedDependencies $metadataAssembly $metadataManaged $metadataRows
        $repeatedDependencies = Get-ReverseManagedDependencies $metadataAssembly $metadataManaged $metadataRows
        Assert-Stage ($firstDependencies.mode -eq 'reflection-only-reference-closure' -and $firstDependencies.files.Count -eq 1) 'Authored metadata fixture did not resolve its exact dependency closure.'
        Assert-Stage ((Get-ReverseDigest $firstDependencies) -eq (Get-ReverseDigest $repeatedDependencies)) 'Repeated metadata inspection expanded unchanged dependencies.'
        [IO.File]::AppendAllText($metadataAssembly, 'changed identity')
        $changedDependencies = Get-ReverseManagedDependencies $metadataAssembly $metadataManaged @(Get-ReverseInventory $metadataGame)
        Assert-Stage ($changedDependencies.mode -eq 'all-managed-conservative-fallback' -and $changedDependencies.files.Count -eq 2) 'Same-name changed assembly reused stale reflection metadata.'
    }
    $inputs = [ordered]@{ assembly = 'a'; tool = '1.0'; parameters = @('-p') }
    $output = Start-ReverseStage $testRoot 'a' $inputs 'decompiled/a'
    Write-Fixture (Join-Path $output 'a.cs') 'first'
    $receipt = Complete-ReverseStage $testRoot 'a' $inputs $output
    Assert-Stage ([bool](Test-ReverseStage $testRoot 'a' $inputs $output)) 'Valid stage was not reusable.'
    $changedTool = [ordered]@{ assembly = 'a'; tool = '2.0'; parameters = @('-p') }
    Assert-Stage (-not (Test-ReverseStage $testRoot 'a' $changedTool $output)) 'Tool change reused stale output.'
    $changedParameters = [ordered]@{ assembly = 'a'; tool = '1.0'; parameters = @('-p', '--new') }
    Assert-Stage (-not (Test-ReverseStage $testRoot 'a' $changedParameters $output)) 'Parameter change reused stale output.'
    $changedAssembly = [ordered]@{ assembly = 'b'; tool = '1.0'; parameters = @('-p') }
    Assert-Stage (-not (Test-ReverseStage $testRoot 'a' $changedAssembly $output)) 'Assembly change reused stale output.'

    $secondOutput = Start-ReverseStage $testRoot 'b' $inputs 'decompiled/b'
    Write-Fixture (Join-Path $secondOutput 'b.cs') 'unchanged'
    Complete-ReverseStage $testRoot 'b' $inputs $secondOutput | Out-Null
    $secondReceiptHash = (Get-FileHash (Get-ReverseStagePath $testRoot 'b')).Hash
    $originalTime = (Get-Item (Join-Path $output 'a.cs')).LastWriteTimeUtc
    Write-Fixture (Join-Path $output 'a.cs') 'other'
    (Get-Item (Join-Path $output 'a.cs')).LastWriteTimeUtc = $originalTime
    Assert-Stage (-not (Test-ReverseStage $testRoot 'a' $inputs $output)) 'Same-size/same-time corruption was not detected.'
    Start-ReverseStage $testRoot 'a' $inputs 'decompiled/a' | Out-Null
    Assert-Stage (@(Get-ChildItem (Join-Path $testRoot '.stage-failures') -Filter 'a-*' -Directory).Count -eq 1) 'Invalid output was not preserved.'
    Fail-ReverseStage $testRoot 'a' 'simulated interrupted tool'
    Assert-Stage (-not (Test-ReverseStage $testRoot 'a' $inputs $output)) 'Failed stage was reusable.'
    Assert-Stage ((Get-FileHash (Get-ReverseStagePath $testRoot 'b')).Hash -eq $secondReceiptHash) 'Unrelated stage receipt changed.'
    Assert-Stage ([bool](Test-ReverseStage $testRoot 'b' $inputs $secondOutput)) 'Unrelated stage stopped being reusable.'
    Write-Fixture (Join-Path $secondOutput 'extra.cs') 'extra'
    Assert-Stage (-not (Test-ReverseStage $testRoot 'b' $inputs $secondOutput)) 'Unexpected output file was accepted.'
    $escaped = $false
    try { Resolve-ReverseChild $testRoot '../escape' | Out-Null } catch { $escaped = $true }
    Assert-Stage $escaped 'Traversal was accepted.'

    # A real entry-point run uses only this tiny authored fixture, with no game/tool dependency.
    $build = Join-Path $testRoot 'code-fixture'
    $snapshot = Join-Path $build 'raw-snapshot'
    $managed = Join-Path $snapshot 'game/DolocTown_Data/Managed'
    Write-Fixture (Join-Path $managed 'Assembly-CSharp.dll') 'DTMAPI authored fake assembly, not executable'
    Write-Fixture (Join-Path $snapshot 'appmanifest_2285550.acf') @'
"AppState"
{
    "buildid" "999"
    "UserConfig"
    {
        "BetaKey" "public"
    }
    "MountedConfig"
    {
        "BetaKey" "public"
    }
}
'@
    $identity = Get-DtmApiSteamBuildIdentity -ManifestPath (Join-Path $snapshot 'appmanifest_2285550.acf')
    $sourceHash = (Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash
    $snapshotInputs = [ordered]@{
        format = 1; steamBuild = '999'; branch = 'public'; manifestSha256 = $identity.ManifestSha256; assemblySha256 = $sourceHash
        files = @('DolocTown.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe'); directories = @('DolocTown_Data', 'MonoBleedingEdge')
    }
    Complete-ReverseStage $build 'snapshot' $snapshotInputs $snapshot | Out-Null
    $raw = @([ordered]@{ path = 'DolocTown_Data/Managed/Assembly-CSharp.dll'; bytes = (Get-Item (Join-Path $managed 'Assembly-CSharp.dll')).Length; sha256 = $sourceHash })
    Write-ReverseJson (Join-Path $build 'full-baseline-inventory/raw-snapshot-files.json') $raw
    $codeInputs = [ordered]@{
        format = 1; assembly = 'Assembly-CSharp.dll'; sourceSha256 = $sourceHash
        tool = [ordered]@{ version = '9.1.0.7988'; packageSha256 = '2B5058F5CCC164C33B7AABF1A5EB0CF3D3A6AF6C145AAF58EFD3ED891443AF7C'; runtime = 'Microsoft.NETCore.App-8' }
        parameters = @('-p', '-r', '<frozen-managed-root>'); dependencies = Get-ReverseManagedDependencies (Join-Path $managed 'Assembly-CSharp.dll') $managed $raw
    }
    $codeOutput = Join-Path $build 'decompiled/Assembly-CSharp'
    Write-Fixture (Join-Path $codeOutput 'Fixture.cs') 'namespace Fixture { class Example { } }'
    Complete-ReverseStage $build 'decompile-Assembly-CSharp' $codeInputs $codeOutput | Out-Null
    & "$repo/tools/portable-reverse-capture/run-doloctown-full-capture.ps1" -BuildRoot $build -Resume -CodeOnly
    Assert-Stage (-not (Test-Path (Join-Path $build 'asset-ripper-unity-project'))) 'Code-only run exported resources.'
    Assert-Stage (-not (Test-Path (Join-Path $build 'asset-ripper'))) 'Code-only run invoked AssetRipper.'
    $result = Get-Content -Raw (Join-Path $build 'full-baseline-inventory/portable-full-capture-summary.json') | ConvertFrom-Json
    Assert-Stage ($result.scope -eq 'code' -and $result.decompiledAssemblies[0].status -eq 'reused-verified') 'Entry-point did not reuse the proven code stage.'
    $copyBuild = Join-Path $testRoot 'copy-fixture'
    $copySnapshot = Join-Path $copyBuild 'raw-snapshot'
    [IO.Directory]::CreateDirectory($copyBuild) | Out-Null
    Copy-Item -LiteralPath $snapshot -Destination $copySnapshot -Recurse
    Complete-ReverseStage $copyBuild 'snapshot' $snapshotInputs $copySnapshot | Out-Null
    & "$repo/tools/portable-reverse-capture/run-doloctown-full-capture.ps1" -BuildRoot $copyBuild -Resume -CodeOnly -ReuseBuildRoot $build
    $copyResult = Get-Content -Raw (Join-Path $copyBuild 'full-baseline-inventory/portable-full-capture-summary.json') | ConvertFrom-Json
    Assert-Stage ($copyResult.decompiledAssemblies[0].status -eq 'copied-identical-assembly-and-dependencies') 'Identical assembly/dependencies were not reused across captures.'

    $export = Join-Path $build 'asset-ripper-unity-project'
    Write-Fixture (Join-Path $export 'ExportedProject/ProjectSettings/ProjectVersion.txt') 'm_EditorVersion: 2021.3.fixture'
    $snapshotReceipt = Get-ReverseStageReceipt $build 'snapshot'
    $exportInputs = [ordered]@{
        format = 1; snapshot = $snapshotReceipt.outputFingerprint
        tool = [ordered]@{ version = '1.3.14'; archiveSha256 = '808CDDF66DD0357AD6B36B97DE3A2AEF5E3552E63AF3EE0610F9A03A0378101C' }
        parameters = @('LoadFolder', 'Export/UnityProject', 'default-settings')
    }
    Complete-ReverseStage $build 'export' $exportInputs $export | Out-Null
    Write-ReverseJson (Join-Path $build 'full-baseline-inventory/snapshot-source-parity.json') ([ordered]@{ exact = $true; fixture = $true })
    & "$repo/tools/scripts/capture-doloctown-reverse-baseline.ps1" -BuildRoot $build -Resume
    Assert-Stage ((Get-ReverseStageReceipt $build 'inventory').status -eq 'complete') 'Derived inventory did not complete.'
    $derivedBefore = (Get-FileHash (Get-ReverseStagePath $build 'inventory')).Hash
    & "$repo/tools/scripts/capture-doloctown-reverse-baseline.ps1" -BuildRoot $build -Resume
    Assert-Stage ((Get-FileHash (Get-ReverseStagePath $build 'inventory')).Hash -eq $derivedBefore) 'Unchanged derived stage was rebuilt.'

    $package = Join-Path $testRoot 'portable-package'
    [IO.Directory]::CreateDirectory($package) | Out-Null
    foreach ($name in @('capture-doloctown-reverse-baseline.ps1', 'reverse-capture-state.ps1', 'steam-appmanifest-identity.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $package $name) }
    foreach ($name in @('common.ps1', 'reverse-baseline-path-safety.ps1', 'run-doloctown-full-capture.ps1')) { Copy-Item -LiteralPath (Join-Path "$repo/tools/portable-reverse-capture" $name) -Destination (Join-Path $package $name) }
    & (Join-Path $package 'run-doloctown-full-capture.ps1') -BuildRoot $copyBuild -Resume -CodeOnly
    Assert-Stage (-not (Test-Path (Join-Path $package '.tools'))) 'Packaged reuse downloaded or installed tools.'

    $fakeGame = Join-Path $testRoot 'source/steamapps/common/DolocTown'
    foreach ($name in @('DolocTown.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'DolocTown_Data/Managed/Assembly-CSharp.dll', 'MonoBleedingEdge/fixture.txt')) {
        Write-Fixture (Join-Path $fakeGame $name) ('authored source fixture: ' + $name)
    }
    Copy-Item -LiteralPath (Join-Path $snapshot 'appmanifest_2285550.acf') -Destination (Join-Path $testRoot 'source/steamapps/appmanifest_2285550.acf')
    $freshBuild = Join-Path $testRoot 'fresh-snapshot'
    & {
        # This nested scope has only a fake GameDir and no executable game;
        # unrelated user processes must not determine a filesystem fixture.
        function Get-Process { param([string] $Name) }
        & "$repo/tools/scripts/capture-doloctown-reverse-baseline.ps1" -GameDir $fakeGame -BuildRoot $freshBuild -CodeOnly
        $firstSnapshot = Get-ReverseStageReceipt $freshBuild 'snapshot'
        Assert-Stage ($firstSnapshot.status -eq 'complete') 'Fresh fixture snapshot failed.'
        Write-Fixture (Join-Path $freshBuild 'raw-snapshot/game/DolocTown_Data/Managed/Assembly-CSharp.dll') 'damaged'
        & "$repo/tools/scripts/capture-doloctown-reverse-baseline.ps1" -GameDir $fakeGame -BuildRoot $freshBuild -CodeOnly -Resume
        Assert-Stage ((Get-ReverseStageReceipt $freshBuild 'snapshot').outputFingerprint -eq $firstSnapshot.outputFingerprint) 'Same-input snapshot retry changed frozen contents.'
        Assert-Stage (@(Get-ChildItem (Join-Path $freshBuild '.stage-failures') -Filter 'snapshot-*' -Directory).Count -eq 1) 'Snapshot retry did not preserve damaged output.'
        Write-Fixture (Join-Path $fakeGame 'DolocTown_Data/Managed/Assembly-CSharp.dll') 'different source identity'
        $rejected = $false
        try { & "$repo/tools/scripts/capture-doloctown-reverse-baseline.ps1" -GameDir $fakeGame -BuildRoot $freshBuild -CodeOnly -Resume } catch { $rejected = $true }
        Assert-Stage $rejected 'Snapshot retry accepted a different source identity.'
    }
    $beforeStatus = @(Get-ReverseInventory $testRoot)
    $status = & "$repo/tools/portable-reverse-capture/run-doloctown-full-capture.ps1" -BuildRoot $build -Status | ConvertFrom-Json
    Assert-Stage $status.readOnly 'Status did not identify its read-only scope.'
    Assert-Stage ((Get-ReverseDigest $beforeStatus) -eq (Get-ReverseDigest @(Get-ReverseInventory $testRoot))) 'Status changed fixture files.'
    Write-Host 'REVERSE CAPTURE STAGES: OK (identity, corruption, interruption, stage isolation, code-only reuse, read-only status)'
}
finally {
    $checkedRoot = Resolve-ReverseChild $testParent (Split-Path -Leaf $testRoot)
    if ($checkedRoot -cne $testRoot) { throw 'Refusing cleanup outside the verified fixture root.' }
    if (Test-Path -LiteralPath $checkedRoot) { Remove-Item -LiteralPath $checkedRoot -Recurse -Force }
}
