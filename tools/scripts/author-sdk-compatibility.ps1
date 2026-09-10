Set-StrictMode -Version 2.0

function Resolve-NetStandardPackageRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $DotNetExe,
        [Parameter(Mandatory = $true)] $Contract,
        [string] $RequestedRoot,
        [switch] $DisableProvision
    )

    $packageCache = Join-Path $RepoRoot '.tools\author-sdk-packages'
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        $requested = [System.IO.Path]::GetFullPath($RequestedRoot)
        $candidates.Add($requested) | Out-Null
        $candidates.Add((Join-Path $requested 'netstandard.library\2.0.3')) | Out-Null
        $candidates.Add((Join-Path $requested 'NETStandard.Library\2.0.3')) | Out-Null
    }
    else {
        $candidates.Add((Join-Path $packageCache 'netstandard.library\2.0.3')) | Out-Null
    }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'build\netstandard2.0\ref') -PathType Container) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        throw "The explicit NETStandard.Library package root does not contain 2.0.3/build/netstandard2.0/ref: $RequestedRoot"
    }
    if ($DisableProvision) {
        throw "The repository-local NETStandard.Library 2.0.3 cache is absent and -NoProvision was requested: $packageCache"
    }

    $provisionRoot = Join-Path $packageCache '.provision'
    if (-not (Test-Path -LiteralPath $provisionRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $provisionRoot -Force | Out-Null
    }
    $projectPath = Join-Path $provisionRoot 'DTMAPI.AuthorSdk.CompatibilityRestore.csproj'
    $projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RestorePackagesPath>$([System.Security.SecurityElement]::Escape($packageCache))</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NETStandard.Library" Version="2.0.3" />
  </ItemGroup>
</Project>
"@
    Write-AuthorSdkUtf8NoBom -Path $projectPath -Value $projectXml
    & $DotNetExe restore $projectPath --packages $packageCache --source 'https://api.nuget.org/v3/index.json' --force-evaluate --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to provision the pinned NETStandard.Library 2.0.3 package into the repository-local Author SDK cache.'
    }
    $resolved = Join-Path $packageCache 'netstandard.library\2.0.3'
    if (-not (Test-Path -LiteralPath (Join-Path $resolved 'build\netstandard2.0\ref') -PathType Container)) {
        throw "NuGet restore completed without the expected pinned package tree: $resolved"
    }
    return [System.IO.Path]::GetFullPath($resolved)
}

function Assert-NetStandardPackage {
    param(
        [Parameter(Mandatory = $true)] [string] $PackageRoot,
        [Parameter(Mandatory = $true)] $Contract
    )

    $nuspec = Join-Path $PackageRoot 'netstandard.library.nuspec'
    if (-not (Test-Path -LiteralPath $nuspec -PathType Leaf)) {
        throw "Pinned package nuspec is missing: $nuspec"
    }
    [xml]$metadata = Get-Content -Raw -Encoding UTF8 -LiteralPath $nuspec
    $id = [string]$metadata.package.metadata.id
    $version = [string]$metadata.package.metadata.version
    if ($id -ne [string]$Contract.netstandardReferencePackage -or $version -ne [string]$Contract.netstandardReferencePackageVersion) {
        throw "Pinned package identity mismatch: id=$id version=$version"
    }
    $referenceRoot = Join-Path $PackageRoot 'build\netstandard2.0\ref'
    $references = @(Get-ChildItem -LiteralPath $referenceRoot -File)
    if ($references.Count -ne [int]$Contract.netstandardReferenceFileCount) {
        throw "Pinned reference count mismatch: expected=$($Contract.netstandardReferenceFileCount) actual=$($references.Count)"
    }
    $digest = Get-AuthorSdkFileTreeDigestV1 -Root $referenceRoot
    if (-not $digest.Equals([string]$Contract.netstandardReferenceInventorySha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Pinned NETStandard.Library reference inventory mismatch: expected=$($Contract.netstandardReferenceInventorySha256) actual=$digest"
    }
    $license = Join-Path $PackageRoot 'LICENSE.TXT'
    $notice = Join-Path $PackageRoot 'THIRD-PARTY-NOTICES.TXT'
    if ((Get-AuthorSdkSha256 -Path $license) -ne ([string]$Contract.netstandardLicenseSha256).ToLowerInvariant()) {
        throw 'Pinned NETStandard.Library license hash mismatch.'
    }
    if ((Get-AuthorSdkSha256 -Path $notice) -ne ([string]$Contract.netstandardNoticeSha256).ToLowerInvariant()) {
        throw 'Pinned NETStandard.Library notice hash mismatch.'
    }
    return $referenceRoot
}

function Get-DtmApiFrozenCompatibilitySource {
    param([string] $RepoRoot, $Contract)
    $root = Join-Path $RepoRoot ('author-sdk/compatibility/' + [string]$Contract.targetRuntimeVersion)
    $manifestPath = Join-Path $root 'source-build.json'
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1 -or $manifest.apiTarget -cne $Contract.targetRuntimeVersion -or
        $manifest.abstractionsSha256 -cne $Contract.abstractionsSha256 -or $manifest.configuration -cne 'Release' -or
        $manifest.pathMapTarget -cne '/_/DTMAPI' -or $manifest.dotnetSdkVersion -cne '8.0.421') {
        throw 'Frozen source recipe does not match the existing compatibility contract.'
    }
    $sourceRoot = Join-Path $root 'source'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($row in $manifest.files) {
        $path = Assert-AuthorSdkChildPath -Root $sourceRoot -Path (Join-Path $sourceRoot ([string]$row.path)) -Label 'frozen source input'
        if (-not $seen.Add([string]$row.path) -or -not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-Item -LiteralPath $path).Length -ne [long]$row.bytes -or (Get-AuthorSdkSha256 -Path $path) -cne $row.sha256) {
            throw "Frozen source input changed or is missing: $($row.path)"
        }
    }
    $actual = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse -Force)
    if ($actual.Count -ne $seen.Count -or -not $seen.Contains([string]$manifest.project)) { throw 'Frozen source input set is incomplete or contains extras.' }
    foreach ($file in $actual) {
        if (-not $seen.Contains((Get-AuthorSdkRelativePath -Root $sourceRoot -Path $file.FullName))) { throw "Unexpected frozen source input: $($file.FullName)" }
    }
    return [pscustomobject]@{ root = $sourceRoot; manifest = $manifest; manifestSha256 = Get-AuthorSdkSha256 -Path $manifestPath }
}

function Test-DtmApiPreparedCompatibility {
    param([string] $OutputRoot, [string] $PayloadRoot, [string] $InputSha256, $Contract)
    try {
        $receiptName = if ($Contract.targetRuntimeVersion -ceq '0.5.5') { 'compatibility.preparation.json' } else { "$($Contract.targetRuntimeVersion).compatibility.preparation.json" }
        $receipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $OutputRoot $receiptName) | ConvertFrom-Json
        if ($receipt.schemaVersion -ne 1 -or $receipt.inputSha256 -cne $InputSha256) { return $false }
        $manifestPath = Join-Path $PayloadRoot ([string]$Contract.releaseManifestName)
        if ((Get-AuthorSdkSha256 -Path $manifestPath) -cne $receipt.manifestSha256) { return $false }
        $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
        if ($manifest.schemaVersion -ne 1 -or $manifest.sdkVersion -cne $Contract.sdkVersion -or
            $manifest.targetRuntimeVersion -cne $Contract.targetRuntimeVersion -or
            $manifest.abstractionsAssemblyVersion -cne $Contract.abstractionsAssemblyVersion -or
            $manifest.abstractionsFileVersion -cne $Contract.abstractionsFileVersion) { return $false }
        $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        $seen.Add([string]$Contract.releaseManifestName) | Out-Null
        foreach ($row in $manifest.files) {
            $path = Assert-AuthorSdkChildPath -Root $PayloadRoot -Path (Join-Path $PayloadRoot ([string]$row.path)) -Label 'prepared compatibility file'
            if (-not $seen.Add([string]$row.path) -or (Get-AuthorSdkSha256 -Path $path) -cne $row.sha256) { return $false }
        }
        $actual = @(Get-ChildItem -LiteralPath $PayloadRoot -File -Recurse -Force)
        if ($actual.Count -ne $seen.Count) { return $false }
        foreach ($file in $actual) { if (-not $seen.Contains((Get-AuthorSdkRelativePath -Root $PayloadRoot -Path $file.FullName))) { return $false } }
        foreach ($anchor in @(
            @{ path = 'DTMAPI.Abstractions.dll'; sha = $Contract.abstractionsSha256 },
            @{ path = 'DTMAPI.Author.props'; sha = $Contract.authorPropsSha256 },
            @{ path = 'NETStandard.Library.LICENSE.TXT'; sha = $Contract.netstandardLicenseSha256 },
            @{ path = 'NETStandard.Library.THIRD-PARTY-NOTICES.TXT'; sha = $Contract.netstandardNoticeSha256 }
        )) { if ((Get-AuthorSdkSha256 -Path (Join-Path $PayloadRoot $anchor.path)) -cne $anchor.sha) { return $false } }
        $referenceRoot = Join-Path $PayloadRoot 'ref/netstandard2.0'
        if (@(Get-ChildItem -LiteralPath $referenceRoot -File).Count -ne [int]$Contract.netstandardReferenceFileCount -or
            (Get-AuthorSdkFileTreeDigestV1 -Root $referenceRoot) -ine $Contract.netstandardReferenceInventorySha256) { return $false }
        return $true
    }
    catch { return $false }
}

function Prepare-DtmApiAuthorSdkCompatibility {
    param(
        [string] $RepoRoot,
        [string] $DotNetExe,
        [string] $OutputRoot,
        [string] $NetStandardPackageRoot = '',
        [string] $FrozenAbstractionsDll = '',
        [string] $ApiTarget = '0.5.5',
        [switch] $NoProvision,
        [switch] $Check
    )
    if (-not $OutputRoot) { $OutputRoot = Join-Path $RepoRoot '.tools/author-sdk-compatibility' }
    $output = [IO.Path]::GetFullPath($OutputRoot)
    $catalog = Get-AuthorSdkTargetCatalog -RepoRoot $RepoRoot
    $target = @($catalog.targets | Where-Object { $_.apiTarget -ceq $ApiTarget -and $_.state -ceq 'available' })
    if ($target.Count -ne 1) { throw "No available frozen API target: $ApiTarget" }
    $contract = Get-AuthorSdkContract -RepoRoot $RepoRoot -Target $target[0]
    $contractRoot = Join-Path $RepoRoot ('author-sdk/compatibility/' + $ApiTarget)
    $receiptName = if ($ApiTarget -ceq '0.5.5') { 'compatibility.preparation.json' } else { "$ApiTarget.compatibility.preparation.json" }
    $source = Get-DtmApiFrozenCompatibilitySource -RepoRoot $RepoRoot -Contract $contract
    # Drain native stdout before inspecting the exit code; Select-Object -First can
    # close the pipe early under Windows PowerShell and terminate a valid host.
    $dotnetVersionOutput = @(& $DotNetExe --version)
    $dotnetVersionExitCode = $LASTEXITCODE
    $dotnetVersion = if ($dotnetVersionOutput.Count -eq 1) { ([string]$dotnetVersionOutput[0]).Trim() } else { '' }
    if ($dotnetVersionExitCode -ne 0 -or $dotnetVersion -cne $source.manifest.dotnetSdkVersion) { throw "Frozen compatibility build requires .NET SDK $($source.manifest.dotnetSdkVersion), actual: $dotnetVersion (exit $dotnetVersionExitCode)" }
    $requested = if ($FrozenAbstractionsDll) { $FrozenAbstractionsDll } elseif ($ApiTarget -ceq '0.5.5') { $env:DTMAPI_AUTHOR_SDK_FROZEN_ABSTRACTIONS_DLL } else { '' }
    if ($requested) {
        $requested = [IO.Path]::GetFullPath($requested)
        if ((Get-AuthorSdkSha256 -Path $requested) -cne $contract.abstractionsSha256) { throw 'Explicit frozen Abstractions override does not match the unchanged contract SHA.' }
    }
    Assert-DtmApiBuildPathsDisjoint -OutputPath $output -InputPaths @((Join-Path $RepoRoot 'src'), (Join-Path $RepoRoot 'author-sdk'), (Join-Path $RepoRoot 'tools/scripts'), (Join-Path $RepoRoot '.tools/dotnet'), (Join-Path $RepoRoot '.tools/author-sdk-packages'), $NetStandardPackageRoot, $requested)
    $input = [ordered]@{
        sourceManifestSha256 = $source.manifestSha256; dotnetSdkVersion = $dotnetVersion
        contractSha256 = Get-AuthorSdkSha256 -Path (Join-Path $contractRoot 'compatibility.contract.json')
        propsSha256 = Get-AuthorSdkSha256 -Path (Join-Path $contractRoot 'DTMAPI.Author.props')
        helperSha256 = Get-AuthorSdkSha256 -Path $PSCommandPath
    }
    $hash = [Security.Cryptography.SHA256]::Create()
    try { $inputSha = ([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes(($input | ConvertTo-Json -Compress))))).Replace('-', '').ToLowerInvariant() }
    finally { $hash.Dispose() }
    $payload = Join-Path $output ([string]$contract.targetRuntimeVersion)
    if (Test-DtmApiPreparedCompatibility -OutputRoot $output -PayloadRoot $payload -InputSha256 $inputSha -Contract $contract) {
        return [pscustomobject]@{ compatibilityRoot = $payload; abstractionsPath = Join-Path $payload 'DTMAPI.Abstractions.dll'; reused = $true; inputSha256 = $inputSha }
    }
    if ($Check) { throw "Prepared compatibility payload is absent, changed or damaged. Run prepare-author-sdk-compatibility.ps1 -OutputRoot '$output'." }
    $packageRoot = Resolve-NetStandardPackageRoot -RepoRoot $RepoRoot -DotNetExe $DotNetExe -Contract $contract -RequestedRoot $NetStandardPackageRoot -DisableProvision:$NoProvision
    $referenceRoot = Assert-NetStandardPackage -PackageRoot $packageRoot -Contract $contract
    $work = Join-Path $output ('.compatibility-build-' + [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($work) | Out-Null
    $completed = $false
    try {
        $abstractions = $requested
        if (-not $abstractions) {
            $buildRoot = Join-Path $work 'inputs'
            foreach ($row in $source.manifest.files) {
                $destination = Assert-AuthorSdkChildPath -Root $buildRoot -Path (Join-Path $buildRoot ([string]$row.path)) -Label 'frozen build input'
                [IO.Directory]::CreateDirectory((Split-Path -Parent $destination)) | Out-Null
                Copy-Item -LiteralPath (Join-Path $source.root ([string]$row.path)) -Destination $destination
            }
            $project = Join-Path $buildRoot ([string]$source.manifest.project)
            $packageCache = Join-Path $RepoRoot '.tools/author-sdk-packages'
            $restoreSource = if ($NoProvision) { $packageCache } else { 'https://api.nuget.org/v3/index.json' }
            & $DotNetExe restore $project --packages $packageCache --source $restoreSource --nologo | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'Frozen Abstractions restore failed.' }
            & $DotNetExe build $project -c Release --no-restore --nologo "/p:RestorePackagesPath=$packageCache" "/p:PathMap=$buildRoot=/_/DTMAPI" '/p:ContinuousIntegrationBuild=true' '/p:Deterministic=true' '/p:DebugType=None' '/p:DebugSymbols=false' '/p:ImportDirectoryBuildTargets=false' | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'Frozen Abstractions source build failed.' }
            $abstractions = Join-Path $buildRoot 'src/DTMAPI.Abstractions/bin/Release/netstandard2.0/DTMAPI.Abstractions.dll'
        }
        if ((Get-AuthorSdkSha256 -Path $abstractions) -cne $contract.abstractionsSha256) { throw "Frozen source build did not reproduce the unchanged Abstractions SHA: $($contract.abstractionsSha256)" }
        $staged = Join-Path $work 'payload'
        $refs = Join-Path $staged 'ref/netstandard2.0'
        [IO.Directory]::CreateDirectory($refs) | Out-Null
        foreach ($file in Get-ChildItem -LiteralPath $referenceRoot -File) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $refs $file.Name) }
        Copy-Item -LiteralPath $abstractions -Destination (Join-Path $staged 'DTMAPI.Abstractions.dll')
        Copy-Item -LiteralPath (Join-Path $contractRoot 'DTMAPI.Author.props') -Destination (Join-Path $staged 'DTMAPI.Author.props')
        Copy-Item -LiteralPath (Join-Path $packageRoot 'LICENSE.TXT') -Destination (Join-Path $staged 'NETStandard.Library.LICENSE.TXT')
        Copy-Item -LiteralPath (Join-Path $packageRoot 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $staged 'NETStandard.Library.THIRD-PARTY-NOTICES.TXT')
        $files = @()
        [string[]]$paths = @(Get-ChildItem -LiteralPath $staged -File -Recurse | ForEach-Object FullName)
        [Array]::Sort($paths, [StringComparer]::Ordinal)
        foreach ($path in $paths) {
            $relative = Get-AuthorSdkRelativePath -Root $staged -Path $path
            $kind = if ($relative -eq 'DTMAPI.Abstractions.dll') { 'abstractions' } elseif ($relative -eq 'DTMAPI.Author.props') { 'props' } elseif ($relative -eq 'NETStandard.Library.LICENSE.TXT') { 'license' } elseif ($relative -eq 'NETStandard.Library.THIRD-PARTY-NOTICES.TXT') { 'notice' } else { 'reference' }
            $files += [ordered]@{ path = $relative; sha256 = Get-AuthorSdkSha256 -Path $path; kind = $kind }
        }
        $manifest = [ordered]@{ schemaVersion = 1; sdkVersion = $contract.sdkVersion; targetRuntimeVersion = $contract.targetRuntimeVersion; abstractionsAssemblyVersion = $contract.abstractionsAssemblyVersion; abstractionsFileVersion = $contract.abstractionsFileVersion; files = $files }
        Write-AuthorSdkUtf8NoBom -Path (Join-Path $staged $contract.releaseManifestName) -Value (($manifest | ConvertTo-Json -Depth 8) + "`n")
        Remove-AuthorSdkTreeSafely -AllowedRoot $output -Path $payload
        Move-Item -LiteralPath $staged -Destination $payload
        $receipt = [ordered]@{ schemaVersion = 1; inputSha256 = $inputSha; sourceCommit = $source.manifest.sourceCommit; abstractionsSha256 = $contract.abstractionsSha256; manifestSha256 = Get-AuthorSdkSha256 -Path (Join-Path $payload $contract.releaseManifestName) }
        Write-AuthorSdkUtf8NoBom -Path (Join-Path $output $receiptName) -Value (($receipt | ConvertTo-Json -Depth 5) + "`n")
        $completed = $true
        return [pscustomobject]@{ compatibilityRoot = $payload; abstractionsPath = Join-Path $payload 'DTMAPI.Abstractions.dll'; reused = $false; inputSha256 = $inputSha }
    }
    finally {
        if ($completed) { Remove-AuthorSdkTreeSafely -AllowedRoot $output -Path $work }
        else { Write-Warning "Compatibility preparation failed; build diagnostics remain at $work" }
    }
}
