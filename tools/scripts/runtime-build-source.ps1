# Release-only source checks. Player installers do not need Git or MSBuild.
function Get-DtmApiEvaluatedRuntimeInputs {
    param([string]$RepoRoot, [string]$DotNetExe, [string[]]$Projects, [string]$Configuration = 'Release')
    $root = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\','/')
    $prefix = $root + [IO.Path]::DirectorySeparatorChar
    $inputs = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $visited = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $queue = New-Object 'System.Collections.Generic.Queue[string]'
    foreach ($project in $Projects) { $queue.Enqueue([IO.Path]::GetFullPath((Join-Path $root $project))) }
    while ($queue.Count -gt 0) {
        $project = $queue.Dequeue()
        if (-not $visited.Add($project)) { continue }
        if (-not $project.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Runtime project outside repository: $project" }
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "Missing Runtime project: $project" }
        $evaluation = @(& $DotNetExe msbuild $project -nologo "-p:Configuration=$Configuration" '-getItem:Compile,EmbeddedResource,ProjectReference')
        if ($LASTEXITCODE -ne 0) { throw "Cannot evaluate Runtime source inputs: $project" }
        $data = ($evaluation -join [Environment]::NewLine) | ConvertFrom-Json
        $paths = New-Object 'System.Collections.Generic.List[string]'
        $paths.Add($project)
        foreach ($kind in @('Compile','EmbeddedResource','ProjectReference')) {
            foreach ($item in @($data.Items.$kind)) {
                $path = [string]$item.FullPath
                if (-not $path) { continue }
                $paths.Add($path)
                if ($kind -eq 'ProjectReference' -and $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { $queue.Enqueue($path) }
            }
        }
        # MSBuildAllProjects omits some imports (including Directory.Build.props).
        # Preprocessing supplies the resolved import boundaries, including conditional imports.
        $preprocessed = @(& $DotNetExe msbuild $project -nologo "-p:Configuration=$Configuration" -preprocess)
        if ($LASTEXITCODE -ne 0) { throw "Cannot evaluate Runtime imports: $project" }
        [xml]$xml = $preprocessed -join [Environment]::NewLine
        foreach ($comment in $xml.SelectNodes('//comment()')) {
            foreach ($line in ($comment.Value -split '\r?\n')) {
                $path = $line.Trim()
                if ($path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $path -PathType Leaf)) { $paths.Add($path) }
            }
        }
        foreach ($path in $paths) {
            $full = [IO.Path]::GetFullPath($path)
            if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { continue }
            $relative = $full.Substring($prefix.Length).Replace('\','/')
            if ($relative -match '(^|/)(obj|bin|\.tools|\.git)(/|$)') { continue }
            $null = $inputs.Add($relative)
        }
    }
    return @($inputs | Sort-Object)
}

function Assert-DtmApiRuntimeBuildSource {
    param([string]$RepoRoot, [string]$DotNetExe, [string[]]$Projects, [string]$Configuration = 'Release', [switch]$SkipBuild)
    if ($SkipBuild) { throw 'Runtime SkipBuild has no authenticated output provenance. Run the Workshop builder without -SkipBuild to rebuild from committed inputs.' }
    $commit = [string](& git -C $RepoRoot rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[a-f0-9]{40,64}$') { throw 'Runtime source requires a Git HEAD.' }
    $current = @(Get-DtmApiEvaluatedRuntimeInputs -RepoRoot $RepoRoot -DotNetExe $DotNetExe -Projects $Projects -Configuration $Configuration)
    # A source-only archive is not a checkout/worktree and runs no build targets.
    # Evaluate HEAD as well, so deletion of a globbed or linked input cannot hide it.
    $session = Join-Path ([IO.Path]::GetTempPath()) ('DTMAPI-runtime-source-' + [Guid]::NewGuid().ToString('N'))
    $archive = Join-Path $session 'head.zip'
    $baseline = Join-Path $session 'head'
    [IO.Directory]::CreateDirectory($session) | Out-Null
    try {
        & git -C $RepoRoot archive --format=zip "--output=$archive" $commit
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read committed Runtime source archive.' }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $baseline)
        $committed = @(Get-DtmApiEvaluatedRuntimeInputs -RepoRoot $baseline -DotNetExe $DotNetExe -Projects $Projects -Configuration $Configuration)
        $paths = @(@($current) + @($committed) | Sort-Object -Unique)
        $trackedText = @(& git -C $RepoRoot ls-files -z) -join "`n"
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect tracked Runtime inputs.' }
        $dirtyText = @(& git -C $RepoRoot diff --name-only --no-renames -z HEAD) -join "`n"
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect changed Runtime inputs.' }
        $tracked = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        $dirty = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        foreach ($path in ($trackedText -split "`0")) { $null = $tracked.Add($path) }
        foreach ($path in ($dirtyText -split "`0")) { $null = $dirty.Add($path) }
        $rejected = @($paths | Where-Object { -not $tracked.Contains($_) -or $dirty.Contains($_) })
        if ($rejected.Count -gt 0) { throw "Workshop Runtime package requires committed evaluated build inputs. Dirty paths:`n$($rejected -join [Environment]::NewLine)" }
        $hashes = [ordered]@{}
        foreach ($path in $current) { $hashes[$path] = (Get-FileHash -LiteralPath (Join-Path $RepoRoot $path) -Algorithm SHA256).Hash }
        return [pscustomobject]@{ Commit = $commit; Inputs = $hashes; Projects = $Projects; Configuration = $Configuration }
    }
    finally {
        $full = [IO.Path]::GetFullPath($session)
        $parent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')
        if ((Split-Path -Parent $full) -cne $parent -or (Split-Path -Leaf $full) -notmatch '^DTMAPI-runtime-source-[0-9a-f]{32}$') { throw 'Source archive cleanup escaped its exact session.' }
        Remove-Item -LiteralPath $full -Recurse -Force
    }
}

function Assert-DtmApiRuntimeBuildSourceUnchanged {
    param([string]$RepoRoot, [string]$DotNetExe, $Before)
    $commit = [string](& git -C $RepoRoot rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $commit -cne $Before.Commit) { throw 'Runtime source commit changed during build.' }
    $paths = @(Get-DtmApiEvaluatedRuntimeInputs -RepoRoot $RepoRoot -DotNetExe $DotNetExe -Projects $Before.Projects -Configuration $Before.Configuration)
    if ($paths.Count -ne $Before.Inputs.Count) { throw 'Runtime input set changed during build.' }
    foreach ($path in $paths) {
        if (-not $Before.Inputs.Contains($path) -or (Get-FileHash -LiteralPath (Join-Path $RepoRoot $path) -Algorithm SHA256).Hash -cne $Before.Inputs[$path]) { throw "Runtime input changed during build: $path" }
    }
}
