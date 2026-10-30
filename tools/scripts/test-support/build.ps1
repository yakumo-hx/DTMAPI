function Invoke-DtmApiProjectBuild {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $DotNetExe,
        [Parameter(Mandatory = $true)] [string[]] $Projects,
        [string] $Configuration = 'Release',
        [switch] $Rebuild,
        [switch] $RestoreOnly
    )
    $selectedProjects = @($Projects | Select-Object -Unique)
    if ($selectedProjects.Count -eq 0) { throw 'No projects were selected for building.' }
    foreach ($project in $selectedProjects) {
        if ([string]::IsNullOrWhiteSpace($project) -or -not (Test-Path -LiteralPath (Join-Path $RepoRoot $project) -PathType Leaf)) { throw "Selected project is missing: $project" }
    }
    $buildTarget = if ($RestoreOnly) { 'Restore' } elseif ($Rebuild) { 'Rebuild' } else { 'Build' }
    if ($selectedProjects.Count -eq 1) {
        & $DotNetExe build (Join-Path $RepoRoot $selectedProjects[0]) -c $Configuration --nologo -m:1 "-t:$buildTarget"
        if ($LASTEXITCODE -ne 0) { throw "Selected project build failed: $($selectedProjects[0])" }
        return
    }

    # Internal workflow suites need not be members of the public solution.
    $solutionText = [IO.File]::ReadAllText((Join-Path $RepoRoot 'DTMAPI.sln'))
    $outsideSolution = @($selectedProjects | Where-Object { -not $solutionText.Contains($_.Replace('/', '\')) })
    if ($outsideSolution.Count -gt 0) {
        $insideSolution = @($selectedProjects | Where-Object { $_ -notin $outsideSolution })
        if ($insideSolution.Count -gt 0) {
            Invoke-DtmApiProjectBuild -RepoRoot $RepoRoot -DotNetExe $DotNetExe -Projects $insideSolution -Configuration $Configuration -Rebuild:$Rebuild -RestoreOnly:$RestoreOnly
        }
        foreach ($project in $outsideSolution) {
            Invoke-DtmApiProjectBuild -RepoRoot $RepoRoot -DotNetExe $DotNetExe -Projects @($project) -Configuration $Configuration -Rebuild:$Rebuild -RestoreOnly:$RestoreOnly
        }
        return
    }

    # This temporary filter is a projection of the caller's selected projects.
    # One MSBuild invocation reuses shared dependencies without a second registry.
    $filterRoot = Join-Path $RepoRoot 'tmp\unit-build-filters'
    New-Item -ItemType Directory -Force -Path $filterRoot | Out-Null
    $filterPath = Join-Path $filterRoot (([Guid]::NewGuid().ToString('N')) + '.slnf')
    try {
        $filter = @{ solution = @{ path = (Join-Path $RepoRoot 'DTMAPI.sln'); projects = @($selectedProjects | ForEach-Object { $_.Replace('/', '\') }) } }
        $encoding = New-Object System.Text.UTF8Encoding($false)
        [IO.File]::WriteAllText($filterPath, ($filter | ConvertTo-Json -Depth 5), $encoding)
        & $DotNetExe build $filterPath -c $Configuration --nologo -m:1 "-t:$buildTarget"
        if ($LASTEXITCODE -ne 0) { throw 'Selected solution-filter build failed.' }
    }
    finally { if (Test-Path -LiteralPath $filterPath) { Remove-Item -LiteralPath $filterPath -Force } }
}
