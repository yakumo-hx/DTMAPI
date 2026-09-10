Set-StrictMode -Version 2.0

function Assert-DtmApiBuildPathsDisjoint {
    param([string] $OutputPath, [string[]] $InputPaths)
    $output = [IO.Path]::GetFullPath($OutputPath)
    foreach ($inputPath in $InputPaths) {
        if ([string]::IsNullOrWhiteSpace($inputPath)) { continue }
        $inputFull = [IO.Path]::GetFullPath($inputPath)
        if ((Test-DtmApiPathIsSameOrChild -Child $output -Parent $inputFull) -or
            (Test-DtmApiPathIsSameOrChild -Child $inputFull -Parent $output)) {
            throw "Build output overlaps an input; nothing was cleaned: output=$output input=$inputFull"
        }
    }
    $ancestor = $output
    while (-not [string]::IsNullOrWhiteSpace($ancestor)) {
        if ((Test-Path -LiteralPath $ancestor) -and
            ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Build output traverses a reparse point; nothing was cleaned: $ancestor"
        }
        $ancestor = Split-Path -Parent $ancestor
    }
}

function Get-DtmApiAuthorSdkInput {
    param([string] $RepoRoot, [string] $DotNetExe)
    # MSBuild evaluation resolves the actual transitive project graph and linked
    # source/assets without restore, build or target execution.
    $repoFull = [IO.Path]::GetFullPath($RepoRoot)
    $files = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $projects = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $queue = New-Object 'System.Collections.Generic.Queue[string]'
    $queue.Enqueue((Join-Path $repoFull 'src/DTMAPI.AuthorSdk/DTMAPI.AuthorSdk.csproj'))
    $sdkVersion = ''
    while ($queue.Count -gt 0) {
        $project = [IO.Path]::GetFullPath($queue.Dequeue())
        if (-not $projects.Add($project)) { continue }
        $files.Add($project) | Out-Null
        $raw = @(& $DotNetExe msbuild $project -nologo '-getItem:Compile,EmbeddedResource,Content,ProjectReference,Reference' '-getProperty:MSBuildAllProjects,NETCoreSdkVersion' '-p:Configuration=Release' '-p:RuntimeIdentifier=win-x64')
        if ($LASTEXITCODE -ne 0) { throw "SDK input evaluation failed before build: $project" }
        $evaluation = ([string]::Join("`n", $raw)) | ConvertFrom-Json
        $sdkVersion = [string]$evaluation.Properties.NETCoreSdkVersion
        foreach ($kind in @('Compile', 'EmbeddedResource', 'Content')) {
            foreach ($item in @($evaluation.Items.$kind)) {
                $path = [IO.Path]::GetFullPath([string]$item.FullPath)
                if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "SDK input is missing: $path" }
                $files.Add($path) | Out-Null
            }
        }
        foreach ($item in @($evaluation.Items.ProjectReference)) { $queue.Enqueue([string]$item.FullPath) }
        foreach ($item in @($evaluation.Items.Reference)) {
            $hint = $item.PSObject.Properties['HintPath']
            if ($null -ne $hint -and -not [string]::IsNullOrWhiteSpace([string]$hint.Value)) {
                $path = if ([IO.Path]::IsPathRooted([string]$hint.Value)) { [IO.Path]::GetFullPath([string]$hint.Value) } else { [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $project) ([string]$hint.Value))) }
                if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "SDK compiler reference is missing: $path" }
                $files.Add($path) | Out-Null
            }
        }
        foreach ($path in ([string]$evaluation.Properties.MSBuildAllProjects).Split(';')) {
            if (-not [string]::IsNullOrWhiteSpace($path) -and $path -notmatch '[\\/]obj[\\/]' -and (Test-Path -LiteralPath $path -PathType Leaf)) {
                $files.Add([IO.Path]::GetFullPath($path)) | Out-Null
            }
        }
        $directory = Split-Path -Parent $project
        while (Test-DtmApiPathIsSameOrChild -Child $directory -Parent $repoFull) {
            foreach ($name in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', 'NuGet.Config', 'nuget.config', 'packages.lock.json')) {
                $path = Join-Path $directory $name
                if (Test-Path -LiteralPath $path -PathType Leaf) { $files.Add($path) | Out-Null }
            }
            if ($directory -eq $repoFull) { break }
            $directory = Split-Path -Parent $directory
        }
    }
    foreach ($relative in @('tools/scripts/build-author-sdk.ps1', 'tools/scripts/prepare-author-sdk.ps1', 'tools/scripts/author-sdk-preparation.ps1', 'tools/scripts/author-sdk-release-common.ps1', 'tools/scripts/author-sdk-compatibility.ps1', 'tools/scripts/prepare-author-sdk-compatibility.ps1', 'tools/scripts/check-author-sdk-release.ps1', 'tools/scripts/common.ps1')) {
        $files.Add((Join-Path $repoFull $relative)) | Out-Null
    }
    # Content/EmbeddedResource evaluation above owns packaged assets. Only these
    # target files are copied outside MSBuild. Examples, their bin/obj and retired
    # policy history are not SDK build inputs.
    $targetPath = Join-Path $repoFull 'author-sdk/target-catalog.json'
    if (Test-Path -LiteralPath $targetPath -PathType Leaf) {
        $files.Add($targetPath) | Out-Null
        $targets = Get-Content -Raw -Encoding UTF8 -LiteralPath $targetPath | ConvertFrom-Json
        foreach ($target in @($targets.targets | Where-Object { $_.state -ceq 'available' })) {
            $files.Add((Join-Path $repoFull ('author-sdk/' + $target.contractPath))) | Out-Null
            $files.Add((Join-Path $repoFull ('author-sdk/' + $target.payloadPath + '/DTMAPI.Author.props'))) | Out-Null
            $recipePath = Join-Path $repoFull ('author-sdk/' + $target.payloadPath + '/source-build.json')
            $files.Add($recipePath) | Out-Null
            $recipe = Get-Content -Raw -Encoding UTF8 -LiteralPath $recipePath | ConvertFrom-Json
            foreach ($row in $recipe.files) {
                $files.Add((Join-Path $repoFull ('author-sdk/' + $target.payloadPath + '/source/' + $row.path))) | Out-Null
            }
        }
    }
    $files.Add([IO.Path]::GetFullPath($DotNetExe)) | Out-Null
    foreach ($name in @('LICENSE.txt', 'ThirdPartyNotices.txt')) { $files.Add((Join-Path (Split-Path -Parent $DotNetExe) $name)) | Out-Null }
    $rows = @($files | Sort-Object -CaseSensitive | ForEach-Object {
        [ordered]@{ path = $_; sha256 = Get-AuthorSdkSha256 -Path $_ }
    })
    $identity = [ordered]@{ schemaVersion = 1; configuration = 'Release'; runtimeIdentifier = 'win-x64'; dotnetSdkVersion = $sdkVersion; files = $rows }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $digest = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($identity | ConvertTo-Json -Depth 6 -Compress)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    return [pscustomobject]@{ sha256 = $digest; identity = $identity; projects = @($projects) }
}

function Test-DtmApiPreparedAuthorSdk {
    param([string] $OutputRoot, $InputSnapshot)
    $receiptPath = Join-Path $OutputRoot 'preparation.json'
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { return $false }
    try {
        $receipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
        if ($receipt.schemaVersion -ne 1 -or $receipt.inputSha256 -cne $InputSnapshot.sha256) { return $false }
        $stage = Assert-AuthorSdkChildPath -Root $OutputRoot -Path ([string]$receipt.stageRoot) -Label 'prepared SDK'
        if (@(Get-ChildItem -LiteralPath $stage -Force -Recurse | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -gt 0) { return $false }
        $inventoryPath = Join-Path $stage 'author-sdk-release.json'
        if ((Get-AuthorSdkSha256 -Path $inventoryPath) -cne $receipt.inventorySha256) { return $false }
        $inventory = Get-Content -Raw -LiteralPath $inventoryPath | ConvertFrom-Json
        $expected = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        $expected.Add('author-sdk-release.json') | Out-Null
        foreach ($row in $inventory.files) {
            $path = Assert-AuthorSdkChildPath -Root $stage -Path (Join-Path $stage ([string]$row.path)) -Label 'prepared SDK file'
            $expected.Add([string]$row.path) | Out-Null
            if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -ne $row.length -or (Get-AuthorSdkSha256 -Path $path) -cne $row.sha256) { return $false }
        }
        $actual = @(Get-ChildItem -LiteralPath $stage -File -Recurse)
        if ($actual.Count -ne $expected.Count) { return $false }
        foreach ($file in $actual) {
            if (-not $expected.Contains((Get-AuthorSdkRelativePath -Root $stage -Path $file.FullName)) -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) { return $false }
        }
        return $true
    }
    catch { return $false }
}
