# Development-only input/output receipt. A NoBuild run must not reuse stale or tampered binaries.
function Get-DtmApiPublicBuildGraph {
    param([string]$RepoRoot,[string]$Configuration,[string[]]$Projects,[string]$DotNetExe,[switch]$AllowMissing,[string[]]$ChangedFiles=@())
    $scratch=Join-Path $RepoRoot ('tmp/build-inputs/'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $scratch -Force | Out-Null
    try {
        [IO.File]::WriteAllLines((Join-Path $scratch 'projects.txt'),[string[]]@($Projects | ForEach-Object {Join-Path $RepoRoot $_}))
        [IO.File]::WriteAllLines((Join-Path $scratch 'changed.txt'),[string[]]@($ChangedFiles | ForEach-Object {[IO.Path]::GetFullPath((Join-Path $RepoRoot $_))}))
        $raw=@(& $DotNetExe msbuild (Join-Path $PSScriptRoot 'build-inputs.csproj') -nologo -restore -t:Collect "-p:Configuration=$Configuration" "-p:BaseIntermediateOutputPath=$RepoRoot/tmp/build-input-reader/obj/" "-p:OutputPath=$RepoRoot/tmp/build-input-reader/bin/" "-p:DtmApiProjectsFile=$scratch/projects.txt" "-p:DtmApiIdentityFile=$scratch/identity.txt" "-p:DtmApiPathsFile=$scratch/paths.json" "-p:DtmApiChangedPathsFile=$scratch/changed.txt" "-p:DtmApiAllowMissing=$($AllowMissing.IsPresent.ToString().ToLowerInvariant())")
        if($LASTEXITCODE -ne 0){throw "Build input graph evaluation failed: $($raw -join [Environment]::NewLine)"}
        # Windows PowerShell 5.1 emits a JSON root array as one pipeline item.
        # Enumerate the decoded value so each graph root stays a separate object.
        $decodedRoots=Get-Content -LiteralPath (Join-Path $scratch 'paths.json') -Raw | ConvertFrom-Json
        return [pscustomobject]@{Identity=[IO.File]::ReadAllText((Join-Path $scratch 'identity.txt'));Roots=@($decodedRoots)}
    } finally {
        foreach($name in @('projects.txt','identity.txt','paths.json','changed.txt')) { Remove-Item -LiteralPath (Join-Path $scratch $name) -Force -ErrorAction SilentlyContinue }
        Remove-Item -LiteralPath $scratch -Force -ErrorAction SilentlyContinue
    }
}

function Get-DtmApiPublicReceiptPatterns {
    param([object[]]$Selection)
    @('global.json','Directory.Build.props','Directory.Build.targets','NuGet.Config','tools/scripts/common.ps1','tools/scripts/common/repository.ps1','tools/scripts/common/files.ps1','tools/scripts/common/toolchain.ps1','tools/scripts/test-public-source.ps1','tools/scripts/test-common.ps1','tools/scripts/test-registry.ps1','tools/scripts/test-support/')
    foreach($suite in $Selection) {
        $suite.entry
        if($suite.runner -eq 'unit-exe') { 'tools/scripts/test-unit.ps1' }
        if($suite.PSObject.Properties['dependencies'] -and $suite.dependencies) { 'tools/scripts/prepare-unit-test-dependencies.ps1' }
        if($suite.PSObject.Properties['inputs'] -and $suite.inputs) { $suite.inputs }
        if($suite.requires -contains 'AuthorCompatibility') {
            'author-sdk/'; 'tools/scripts/author-sdk-compatibility.ps1'; 'tools/scripts/prepare-author-sdk-compatibility.ps1'; 'tools/scripts/author-sdk-preparation.ps1'; 'tools/scripts/author-sdk-release-common.ps1'
        }
    }
}

function Get-DtmApiPublicReceiptPaths {
    param([string]$RepoRoot,[object[]]$Selection)
    $repositoryFiles=$null
    $files=@(foreach($pattern in @(Get-DtmApiPublicReceiptPatterns -Selection $Selection | Sort-Object -Unique)) {
        if([Management.Automation.WildcardPattern]::ContainsWildcardCharacters($pattern)) {
            if($null -eq $repositoryFiles) {
                $repositoryFiles=@(& git -C $RepoRoot -c core.quotePath=false ls-files --cached --others --exclude-standard)
                if($LASTEXITCODE -ne 0){throw 'Cannot enumerate declared test input globs.'}
            }
            $matches=@($repositoryFiles | Where-Object {$_ -like $pattern})
            if($matches.Count){$matches | ForEach-Object {Join-Path $RepoRoot $_}}else{Join-Path $RepoRoot $pattern}
            continue
        }
        $path=Join-Path $RepoRoot $pattern
        if(Test-Path -Path $path) {
            foreach($item in @(Get-Item -Path $path)) {
                if($item.PSIsContainer) { Get-ChildItem -LiteralPath $item.FullName -Recurse -File | Where-Object {$_.FullName.Substring($RepoRoot.TrimEnd('\','/').Length).TrimStart('\','/') -notmatch '(^|[\\/])(bin|obj|tmp|temp|\.local-build|\.dtmapi|\.git)[\\/]'} | Select-Object -ExpandProperty FullName }
                else { $item.FullName }
            }
        } else { $path }
    })
    return @($files | Sort-Object -Unique)
}

# CI selection uses the same evaluated project graph, plus explicitly declared
# runtime data/scripts. MSBuild globs match changes even after their last file is deleted.
function Select-DtmApiAffectedPublicTests {
    param([string]$RepoRoot,[object[]]$Selection,[string[]]$ChangedFiles,[string]$Configuration,[string]$DotNetExe)
    $projects=@($Selection | ForEach-Object {$_.project} | Where-Object {$_} | Sort-Object -Unique)
    $graph=if($projects.Count){Get-DtmApiPublicBuildGraph -RepoRoot $RepoRoot -Configuration $Configuration -Projects $projects -DotNetExe $DotNetExe -AllowMissing -ChangedFiles $ChangedFiles}else{$null}
    foreach($suite in $Selection) {
        $patterns=@(Get-DtmApiPublicReceiptPatterns -Selection @($suite)) + @('tests/DTMAPI.UnitTests/suites.json','.github/workflows/public-source.yml')
        $root=if($graph -and $suite.project){@($graph.Roots | Where-Object {$_.Project -ieq (Join-Path $RepoRoot $suite.project)})}else{@()}
        $affected=$false
        foreach($changed in $ChangedFiles) {
            foreach($pattern in $patterns) {
                if($changed -like $pattern -or ($pattern.EndsWith('/') -and $changed.StartsWith($pattern,[StringComparison]::OrdinalIgnoreCase))) { $affected=$true;break }
            }
            $absolute=[IO.Path]::GetFullPath((Join-Path $RepoRoot $changed))
            foreach($node in $root) {
                if($node.Files -contains $absolute){$affected=$true;break}

            }
            if($affected){break}
        }
        if($affected){$suite}
    }
}

function Get-DtmApiPublicBuildIdentity {
    param([string]$RepoRoot,[string]$Profile,[string]$Configuration,[string[]]$Projects,[string]$DotNetExe,[object[]]$Selection=@(),[switch]$Outputs)
    $rows=New-Object 'System.Collections.Generic.List[string]'
    $rows.Add("$Profile|$Configuration|$($Projects -join '|')")
    if($Outputs) {
        $files=@()
        if($Projects.Count) {
            $graph=Get-DtmApiPublicBuildGraph -RepoRoot $RepoRoot -Configuration $Configuration -Projects $Projects -DotNetExe $DotNetExe
            foreach($bin in @($graph.Roots | ForEach-Object {$_.OutputDirectories} | Sort-Object -Unique)) {
                if(Test-Path -LiteralPath $bin) {$files+=@(Get-ChildItem -LiteralPath $bin -Recurse -File)}
            }
        }
        $paths=@($files | Select-Object -ExpandProperty FullName -Unique | Sort-Object)
    } else {
        $paths=@(Get-DtmApiPublicReceiptPaths -RepoRoot $RepoRoot -Selection $Selection)
        # Re-read only selected registrations so concurrent relevant routing edits
        # are detected without binding unrelated suite metadata.
        $registered=Get-Content -LiteralPath (Join-Path $RepoRoot 'tests/DTMAPI.UnitTests/suites.json') -Raw | ConvertFrom-Json
        $ids=@($Selection | ForEach-Object {$_.id})
        $rows.Add((@($registered.suites | Where-Object {$_.id -in $ids}) | ConvertTo-Json -Depth 20 -Compress))
        if($Projects.Count -gt 0) {
            $graph=Get-DtmApiPublicBuildGraph -RepoRoot $RepoRoot -Configuration $Configuration -Projects $Projects -DotNetExe $DotNetExe
            $rows.Add($graph.Identity)
        }
    }
    foreach($path in $paths) {
        if(-not (Test-Path -LiteralPath $path -PathType Leaf)){$rows.Add($path+'|missing');continue}
        $relative=$path.Replace('\','/')
        $rows.Add($relative+'|'+(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)
    }
    $hash=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes(($rows -join "`n"))))).Replace('-','').ToLowerInvariant()}
    finally{$hash.Dispose()}
}

function Get-DtmApiPublicSourceState {
    param([string]$RepoRoot)
    $commit=(& git -C $RepoRoot rev-parse HEAD).Trim()
    if($LASTEXITCODE -ne 0){throw 'Cannot read source commit.'}
    $tree=(& git -C $RepoRoot rev-parse 'HEAD^{tree}').Trim()
    if($LASTEXITCODE -ne 0){throw 'Cannot read source tree.'}
    $status=(& git -C $RepoRoot status --porcelain --untracked-files=normal | Out-String)
    if($LASTEXITCODE -ne 0){throw 'Cannot read source status.'}
    return [pscustomobject]@{Commit=$commit;Tree=$tree;Dirty=(-not [string]::IsNullOrWhiteSpace($status))}

}
