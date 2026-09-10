# Read-only operation selection. No restore, build, deployment, game launch or output creation.
function Get-DtmApiWorkspacePreflight {
    param(
        [string] $RepoRoot,
        [ValidateSet('BuildProduct', 'TestProduct', 'SourceChecks', 'CaptureGame')] [string] $Operation,
        [string] $CatalogId = '',
        [ValidateSet('Unit', 'Game')] [string] $TestMode = 'Unit',
        [string] $ReferenceGameDir = '', [string] $GameDir = '',
        [string] $AuthorSdkRoot = '', [string] $OutputRoot = ''
    )
    $repo = [IO.Path]::GetFullPath($RepoRoot)
    $problems = New-Object 'System.Collections.Generic.List[string]'
    $dependencies = New-Object 'System.Collections.Generic.List[object]'
    $report = [ordered]@{ schemaVersion = 1; operation = $Operation; testMode = $TestMode; ready = $false; product = $null; sdkApiTarget = $null; sdkRoot = $null; compileReference = $null; testGame = $null; outputRoot = $null; dependencies = @(); problems = @() }
    $product = $null
    if ($Operation -in @('BuildProduct', 'TestProduct')) {
        $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools/release/dtmapi-product-catalog.json') | ConvertFrom-Json
        $matches = @($catalog.products | Where-Object { $_.catalogId -ceq $CatalogId })
        if ([string]::IsNullOrWhiteSpace($CatalogId) -or $matches.Count -ne 1) { $problems.Add("Choose one exact CatalogId for $Operation; found $($matches.Count) for '$CatalogId'.") }
        else {
            $product = $matches[0]
            $source = Get-DtmApiObjectProperty $product 'sourceRoot' ''
            $report.product = [ordered]@{ catalogId = $product.catalogId; uniqueId = $product.uniqueId; sourceRoot = $source; sourceVersion = $product.sourceVersion }
            if ([string]::IsNullOrWhiteSpace($source)) { $problems.Add('Selected product has no source root.') }
            elseif (-not (Test-Path -LiteralPath (Join-Path $repo $source) -PathType Container)) { $problems.Add("Missing product source: $source") }
            if ($Operation -eq 'BuildProduct') {
                if ([string](Get-DtmApiObjectProperty $product 'productionBuildAuthority' '') -cne 'DTMAPI Author SDK build/pack/deploy') { $problems.Add('BuildProduct is the Author SDK product route; select the documented content/legacy build entry for this product.') }
                $authorPath = Join-Path (Join-Path $repo $source) 'dtmapi.author.json'
                if (Test-Path -LiteralPath $authorPath -PathType Leaf) {
                    $author = Get-Content -Raw -Encoding UTF8 -LiteralPath $authorPath | ConvertFrom-Json
                    $report.sdkApiTarget = $author.targetDtmApiVersion
                } else { $problems.Add("Missing Author SDK project: $authorPath") }
            }
        }
    }
    $requiresGame = $Operation -eq 'CaptureGame' -or ($Operation -eq 'TestProduct' -and $TestMode -eq 'Game')
    if ([string]::IsNullOrWhiteSpace($GameDir)) {
        try { $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo -AllowMissing } catch { $problems.Add($_.Exception.Message) }
    }
    if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
        $GameDir = [IO.Path]::GetFullPath($GameDir)
        $report.testGame = $GameDir
        if ($requiresGame -and -not (Test-DtmApiDolocTownGamePath -Path $GameDir)) { $problems.Add("Invalid test/capture game directory: $GameDir") }
    } elseif ($requiresGame) { $problems.Add('Set -GameDir, DTMAPI_GAME_DIR or local.settings.json GameDir before game operations.') }

    $dotnet = Join-Path $repo '.tools/dotnet/dotnet.exe'
    if ($Operation -ne 'CaptureGame') {
        $dotnetReady = $false
        try { $dotnet = Get-DotNetExe -RepoRoot $repo -NoProvision; $dotnetReady = $true } catch { $problems.Add($_.Exception.Message) }
        $dependencies.Add([ordered]@{ name = 'DotNet8'; path = $dotnet; ready = $dotnetReady; prepare = 'tools/scripts/prepare-workspace.ps1 -Dependency DotNet' })
        if (-not $dotnetReady) { $problems.Add("Missing compatible repository .NET 8 host: $dotnet") }
    }
    if ($Operation -eq 'BuildProduct' -and $null -ne $product) {
        if ([string]::IsNullOrWhiteSpace($AuthorSdkRoot)) { $AuthorSdkRoot = Join-Path $repo '.tools/author-sdk' }
        $AuthorSdkRoot = [IO.Path]::GetFullPath($AuthorSdkRoot)
        $report.sdkRoot = $AuthorSdkRoot
        $sdkReady = $false
        if ($dotnetReady) {
            try { $inputs = Get-DtmApiAuthorSdkInput -RepoRoot $repo -DotNetExe $dotnet; $sdkReady = Test-DtmApiPreparedAuthorSdk -OutputRoot $AuthorSdkRoot -InputSnapshot $inputs }
            catch { $problems.Add($_.Exception.Message) }
        }
        $dependencies.Add([ordered]@{ name = 'AuthorSdk'; path = $AuthorSdkRoot; ready = $sdkReady; prepare = "tools/scripts/prepare-author-sdk.ps1 -OutputRoot '$AuthorSdkRoot'" })
        if (-not $sdkReady) { $problems.Add('Author SDK preparation is missing, stale or damaged; use the reported prepare command.') }
        $policyId = [string](Get-DtmApiObjectProperty $product 'referencePolicyId' '')
        if (-not [string]::IsNullOrWhiteSpace($policyId)) {
            $policy = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo "author-sdk/advanced-reference-policies/$policyId.json") | ConvertFrom-Json
            if ([string]::IsNullOrWhiteSpace($ReferenceGameDir)) { $ReferenceGameDir = $GameDir }
            $reference = [ordered]@{ policyId = $policyId; gameBuildId = $policy.gameBuildId; gameDir = $ReferenceGameDir; expectedAssemblySha256 = $policy.gameAssemblySha256; actualAssemblySha256 = $null }
            if (-not [string]::IsNullOrWhiteSpace($ReferenceGameDir)) {
                $ReferenceGameDir = [IO.Path]::GetFullPath($ReferenceGameDir)
                $reference.gameDir = $ReferenceGameDir
                $assembly = Join-Path $ReferenceGameDir $policy.gameAssemblyRelativePath
                if (Test-Path -LiteralPath $assembly -PathType Leaf) { $reference.actualAssemblySha256 = (Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash }
            }
            if ($reference.actualAssemblySha256 -ne $reference.expectedAssemblySha256) { $problems.Add("Compile reference must match policy '$policyId' (game $($policy.gameBuildId)); prepare/reuse its reference fixture, not a different installed game.") }
            $report.compileReference = $reference
        }
    }
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $OutputRoot = switch ($Operation) {
            'BuildProduct' { Join-Path $repo "temp/batch6-$CatalogId-advanced-pilot" }
            'TestProduct' { if ($TestMode -eq 'Game') { Join-Path $repo 'temp/runtime-smoke' } else { Join-Path $repo 'tests' } }
            'CaptureGame' { Join-Path $repo 'references/doloctown' }
            'SourceChecks' { Join-Path $repo 'temp/source-checks' }
        }
    }
    $report.outputRoot = [IO.Path]::GetFullPath($OutputRoot)
    if (-not ($Operation -eq 'TestProduct' -and $TestMode -eq 'Unit')) {
        $protected = @((Join-Path $repo 'src'), (Join-Path $repo 'products'), (Join-Path $repo 'author-sdk'), (Join-Path $repo 'tools'), (Join-Path $repo '.tools'), $GameDir, $ReferenceGameDir, $AuthorSdkRoot)
        try { Assert-DtmApiBuildPathsDisjoint -OutputPath $report.outputRoot -InputPaths $protected } catch { $problems.Add($_.Exception.Message) }
    }
    $report.dependencies = $dependencies.ToArray()
    $report.problems = $problems.ToArray()
    $report.ready = $problems.Count -eq 0
    return [pscustomobject]$report
}
