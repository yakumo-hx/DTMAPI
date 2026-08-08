param(
    [string] $Configuration = 'Release',
    [switch] $Quiet,
    [switch] $HostMatrixChild,
    [switch] $ManagedAuthorSdk,
    [string] $ManagedAuthorSdkArtifactRoot = '',
    [string] $ManagedReferenceGameDir = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
$installScript = Join-Path $PSScriptRoot 'install-to-game.ps1'
$allPublishedDefinitions = @(Get-DtmApiPublishedModDefinitions)
$allDeveloperDefinitions = @(Get-DtmApiDeveloperOfficialModDefinitions)
$managedAuthorSdkDefinitions = @($allDeveloperDefinitions | Where-Object {
    (Test-DtmApiMapKey -Map $_ -Key 'AuthorSdkProject') -and [bool](Get-DtmApiMapValue -Map $_ -Key 'AuthorSdkProject' -Default $false)
})
$publishedDefinitions = @($allPublishedDefinitions | Where-Object {
    -not ((Test-DtmApiMapKey -Map $_ -Key 'AuthorSdkProject') -and [bool](Get-DtmApiMapValue -Map $_ -Key 'AuthorSdkProject' -Default $false))
})
$developerDefinitions = @($allDeveloperDefinitions | Where-Object {
    -not ((Test-DtmApiMapKey -Map $_ -Key 'QaFixture') -and [bool](Get-DtmApiMapValue -Map $_ -Key 'QaFixture' -Default $false)) -and
    -not ((Test-DtmApiMapKey -Map $_ -Key 'AuthorSdkProject') -and [bool](Get-DtmApiMapValue -Map $_ -Key 'AuthorSdkProject' -Default $false))
})

if (-not $HostMatrixChild) {
    if ($ManagedAuthorSdk) {
        $managedDefinition = @($managedAuthorSdkDefinitions | Where-Object {
            [string]$_.UniqueID -ceq 'DTMAPI.ZoomMod'
        })
        if ($managedDefinition.Count -ne 1) {
            throw 'Developer official-local install transaction test failed: The bounded managed fixture requires exactly one DTMAPI.ZoomMod definition.'
        }
        if ([string]::IsNullOrWhiteSpace($ManagedReferenceGameDir)) {
            $ManagedReferenceGameDir = Resolve-DolocTownGamePath -RepoRoot $repo
        }
        else {
            $ManagedReferenceGameDir = [System.IO.Path]::GetFullPath($ManagedReferenceGameDir)
        }
        if ([string]::IsNullOrWhiteSpace($ManagedAuthorSdkArtifactRoot)) {
            $ManagedAuthorSdkArtifactRoot = Join-Path $repo 'temp\prerelease-step3-author-sdk-install-transaction'
            & (Join-Path $PSScriptRoot 'build-batch6-advanced-product.ps1') `
                -CatalogId ([string]$managedDefinition[0].AuthorSdkCatalogId) `
                -Configuration $Configuration `
                -GameDir $ManagedReferenceGameDir `
                -OutputRoot $ManagedAuthorSdkArtifactRoot
            if ($LASTEXITCODE -ne 0) {
                throw 'Developer official-local install transaction test failed: Could not build the bounded managed Advanced fixture.'
            }
        }
        $ManagedAuthorSdkArtifactRoot = [System.IO.Path]::GetFullPath($ManagedAuthorSdkArtifactRoot)
    }

    $hostCandidates = New-Object 'System.Collections.Generic.List[string]'
    $currentHost = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    if (Test-Path -LiteralPath $currentHost -PathType Leaf) {
        $hostCandidates.Add([System.IO.Path]::GetFullPath($currentHost)) | Out-Null
    }
    $windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    if (Test-Path -LiteralPath $windowsPowerShell -PathType Leaf) {
        $hostCandidates.Add([System.IO.Path]::GetFullPath($windowsPowerShell)) | Out-Null
    }
    $pwshCommand = Get-Command pwsh.exe -ErrorAction SilentlyContinue
    if ($pwshCommand -and (Test-Path -LiteralPath $pwshCommand.Source -PathType Leaf)) {
        $hostCandidates.Add([System.IO.Path]::GetFullPath($pwshCommand.Source)) | Out-Null
    }

    $hosts = @($hostCandidates.ToArray() | Sort-Object -Unique)
    if ($hosts.Count -eq 0) {
        throw 'Developer official-local install transaction test failed: No PowerShell host was available for the transaction matrix.'
    }
    foreach ($hostExe in $hosts) {
        $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Configuration', $Configuration, '-HostMatrixChild')
        if ($Quiet) {
            $arguments += '-Quiet'
        }
        if ($ManagedAuthorSdk) {
            $arguments += @(
                '-ManagedAuthorSdk',
                '-ManagedAuthorSdkArtifactRoot', $ManagedAuthorSdkArtifactRoot,
                '-ManagedReferenceGameDir', $ManagedReferenceGameDir
            )
        }
        & $hostExe @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Developer official-local install transaction matrix failed under $hostExe with exit code $LASTEXITCODE."
        }
    }
    if (-not $Quiet) {
        Write-Host "Developer official-local install transaction host matrix: OK (hosts=$($hosts.Count))"
    }
    return
}

function Assert-InstallTransactionTest {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw "Developer official-local install transaction test failed: $Message"
    }
}

function Write-TransactionTestText {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [AllowEmptyString()] [string] $Text
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

function Write-ValidTransactionModInfos {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $modInfos = New-Object PSObject
    $modInfos | Add-Member -MemberType NoteProperty -Name 'Workshop.9999999999' -Value ([pscustomobject][ordered]@{
        id = 'Workshop.9999999999'
        enabled = $true
        priority = 3
        source = 'Workshop'
        title = 'third-party-sentinel'
    })
    Write-Utf8NoBomJson -Path $Path -Value ([ordered]@{ modInfos = $modInfos })
}

function New-InstallTransactionFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [string] $GameDir = ''
    )

    $gameDir = if ([string]::IsNullOrWhiteSpace($GameDir)) {
        Join-Path $Root 'Doloc Town 事务 Game'
    }
    else {
        [System.IO.Path]::GetFullPath($GameDir)
    }
    $persistentRoot = Join-Path $Root '用户 持久事务目录'
    New-Item -ItemType Directory -Force -Path (Join-Path $gameDir 'DolocTown_Data') | Out-Null
    Write-TransactionTestText -Path (Join-Path $gameDir 'DolocTown.exe') -Text 'fake-executable'
    return [pscustomobject]@{
        Root = $Root
        GameDir = $gameDir
        PersistentRoot = $persistentRoot
        ModsRoot = (Join-Path $persistentRoot 'MODS')
        ModInfosPath = (Join-Path $persistentRoot 'SAVE\mod_infos.json')
        StateDir = (Join-Path $gameDir 'DTMAPI')
    }
}

function Get-TransactionPowerShellHost {
    $windowsPowerShell = Join-Path $PSHOME 'powershell.exe'
    if (Test-Path -LiteralPath $windowsPowerShell -PathType Leaf) {
        return $windowsPowerShell
    }
    $powerShellCore = Join-Path $PSHOME 'pwsh.exe'
    if (Test-Path -LiteralPath $powerShellCore -PathType Leaf) {
        return $powerShellCore
    }
    throw "Could not resolve the current PowerShell executable under $PSHOME."
}

function Invoke-TransactionInstaller {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [switch] $AllDeveloperDefinitions,
        [switch] $IncludeManagedProducts
    )

    $oldGameDir = $env:DTMAPI_GAME_DIR
    $oldPersistentRoot = $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    $oldRuntimeDir = $env:DTMAPI_RUNTIME_DIR
    try {
        $env:DTMAPI_GAME_DIR = $Fixture.GameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $Fixture.PersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $Fixture.StateDir
        $hostExe = Get-TransactionPowerShellHost
        $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $installScript, '-Configuration', $Configuration, '-SkipBuild')
        if ($AllDeveloperDefinitions) {
            $arguments += '-InstallAllDevOfficialMods'
        }
        else {
            $arguments += '-InstallPublishedModsOnly'
        }
        if (-not $IncludeManagedProducts) {
            $arguments += '-LegacyOfficialLocalOnly'
        }
        $previousErrorActionPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $output = @(& $hostExe @arguments 2>&1 | ForEach-Object { [string]$_ })
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        return [pscustomobject]@{
            ExitCode = $exitCode
            Output = $output
        }
    }
    finally {
        $env:DTMAPI_GAME_DIR = $oldGameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $oldPersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $oldRuntimeDir
    }
}

function Invoke-ManagedAuthorSdkTransactionInstaller {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $ArtifactRoot,
        [Parameter(Mandatory = $true)] [string] $UniqueId,
        [string] $FailPhase = '',
        [string] $AuthorFault = '',
        [string] $ExpectedVersion = '',
        [switch] $CorruptReport
    )

    $oldValues = [ordered]@{}
    foreach ($name in @(
        'DTMAPI_GAME_DIR',
        'DTMAPI_DOLOC_PERSISTENT_ROOT',
        'DTMAPI_RUNTIME_DIR',
        'DTMAPI_INSTALL_TRANSACTION_TEST_MODE',
        'DTMAPI_INSTALL_AUTHOR_SDK_ARTIFACT_ROOT',
        'DTMAPI_INSTALL_AUTHOR_SDK_ONLY_UNIQUE_ID',
        'DTMAPI_INSTALL_AUTHOR_SDK_EXPECTED_VERSION',
        'DTMAPI_INSTALL_AUTHOR_SDK_CORRUPT_REPORT',
        'DTMAPI_AUTHOR_SDK_INSTALL_FAIL_PHASE',
        'DTMAPI_AUTHOR_FAULT',
        'DTMAPI_AUTHOR_STATE_ROOT')) {
        $oldValues[$name] = [Environment]::GetEnvironmentVariable($name)
    }
    try {
        $env:DTMAPI_GAME_DIR = $Fixture.GameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $Fixture.PersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $Fixture.StateDir
        $env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE = '1'
        $env:DTMAPI_INSTALL_AUTHOR_SDK_ARTIFACT_ROOT = $ArtifactRoot
        $env:DTMAPI_INSTALL_AUTHOR_SDK_ONLY_UNIQUE_ID = $UniqueId
        $env:DTMAPI_INSTALL_AUTHOR_SDK_EXPECTED_VERSION = $ExpectedVersion
        $env:DTMAPI_INSTALL_AUTHOR_SDK_CORRUPT_REPORT = if ($CorruptReport) { '1' } else { '' }
        $env:DTMAPI_AUTHOR_SDK_INSTALL_FAIL_PHASE = $FailPhase
        $env:DTMAPI_AUTHOR_FAULT = $AuthorFault
        $env:DTMAPI_AUTHOR_STATE_ROOT = Join-Path $Fixture.StateDir 'author-sdk-state'
        $hostExe = Get-TransactionPowerShellHost
        $arguments = @(
            '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $installScript,
            '-Configuration', $Configuration, '-SkipBuild', '-InstallPublishedModsOnly'
        )
        $previousErrorActionPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $output = @(& $hostExe @arguments 2>&1 | ForEach-Object { [string]$_ })
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        return [pscustomobject]@{
            ExitCode = $exitCode
            Output = $output
        }
    }
    finally {
        foreach ($name in $oldValues.Keys) {
            [Environment]::SetEnvironmentVariable($name, $oldValues[$name])
        }
    }
}

function Get-ManagedAuthorSdkTreeFingerprint {
    param([Parameter(Mandatory = $true)] [string] $Root)

    Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $Root -PathType Container) -Message "Managed tree is missing: $Root"
    $rows = New-Object 'System.Collections.Generic.List[string]'
    foreach ($directory in @(Get-ChildItem -LiteralPath $Root -Directory -Recurse -Force | Sort-Object FullName)) {
        $relative = $directory.FullName.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
        $rows.Add('D|' + $relative) | Out-Null
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force | Sort-Object FullName)) {
        $relative = $file.FullName.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
        $rows.Add('F|' + $relative + '|' + (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) | Out-Null
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes([string]::Join("`n", $rows.ToArray()))
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($algorithm.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $algorithm.Dispose()
    }
}

function Copy-ManagedAuthorSdkReferenceFiles {
    param(
        [Parameter(Mandatory = $true)] [string] $PackagePath,
        [Parameter(Mandatory = $true)] [string] $ReferenceGameDir,
        [Parameter(Mandatory = $true)] [string] $FixtureGameDir
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entry = $archive.GetEntry('Content/DTMAPI/dtmapi-advanced-references.json')
        Assert-InstallTransactionTest -Condition ($null -ne $entry) -Message 'Managed fixture package omitted its SDK Advanced reference receipt.'
        $stream = $entry.Open()
        $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
        try {
            $receipt = $reader.ReadToEnd() | ConvertFrom-Json
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
    foreach ($reference in @($receipt.references)) {
        $relative = ([string]$reference.gameRelativePath).Replace('/', '\')
        $source = Join-Path $ReferenceGameDir $relative
        $destination = Join-Path $FixtureGameDir $relative
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $source -PathType Leaf) -Message "Managed fixture reference is missing: $source"
        $parent = Split-Path -Parent $destination
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination -Force
        Assert-InstallTransactionTest -Condition (
            (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -eq
            (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        ) -Message "Managed fixture reference copy drifted: $relative"
    }
}

function Assert-ManagedAuthorSdkPreState {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $UniqueId,
        [Parameter(Mandatory = $true)] [string] $DestinationFingerprint,
        [Parameter(Mandatory = $true)] [string] $JournalHash,
        [Parameter(Mandatory = $true)] [string] $SourceStateHash,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $destination = Join-Path (Join-Path $Fixture.GameDir 'Mods') $UniqueId
    $authorStateRoot = Join-Path $Fixture.StateDir 'author-sdk-state'
    $journals = @(Get-ChildItem -LiteralPath $authorStateRoot -Recurse -File -Filter ($UniqueId + '.journal.json') -ErrorAction SilentlyContinue)
    $sourceStates = @(Get-ChildItem -LiteralPath $authorStateRoot -Recurse -File -Filter 'source-state.json' -ErrorAction SilentlyContinue)
    Assert-InstallTransactionTest -Condition ($journals.Count -eq 1) -Message "$Label could not resolve one Author SDK deployment journal."
    Assert-InstallTransactionTest -Condition ($sourceStates.Count -eq 1) -Message "$Label could not resolve one Author SDK source state."
    $journal = $journals[0].FullName
    $sourceState = $sourceStates[0].FullName
    Assert-InstallTransactionTest -Condition (
        (Get-ManagedAuthorSdkTreeFingerprint -Root $destination) -eq $DestinationFingerprint
    ) -Message "$Label changed the exact managed destination tree."
    Assert-InstallTransactionTest -Condition (
        (Test-Path -LiteralPath $journal -PathType Leaf) -and
        (Get-FileHash -LiteralPath $journal -Algorithm SHA256).Hash -eq $JournalHash
    ) -Message "$Label changed the Author SDK deployment journal."
    Assert-InstallTransactionTest -Condition (
        (Test-Path -LiteralPath $sourceState -PathType Leaf) -and
        (Get-FileHash -LiteralPath $sourceState -Algorithm SHA256).Hash -eq $SourceStateHash
    ) -Message "$Label changed the Author SDK source state."
    $outerTransactionRoot = Join-Path $Fixture.GameDir 'Mods\.dtmapi-installer-author-sdk'
    Assert-InstallTransactionTest -Condition (-not (Test-Path -LiteralPath $outerTransactionRoot)) -Message "$Label left an outer installer transaction."
}

function Get-LatestInstallFailureState {
    param([Parameter(Mandatory = $true)] $Fixture)

    $file = @(Get-ChildItem -LiteralPath $Fixture.StateDir -Filter 'install-state.failed-*.json' -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1)
    Assert-InstallTransactionTest -Condition ($file.Count -eq 1) -Message "Expected one latest install failure state under $($Fixture.StateDir)."
    return Get-Content -Raw -Encoding UTF8 -LiteralPath $file[0].FullName | ConvertFrom-Json
}

function Get-PublishedDestinationCount {
    param([Parameter(Mandatory = $true)] $Fixture)

    if (-not (Test-Path -LiteralPath $Fixture.ModsRoot -PathType Container)) {
        return 0
    }
    return @($publishedDefinitions | Where-Object {
        Test-Path -LiteralPath (Join-Path $Fixture.ModsRoot ([string]$_.OfficialFolder)) -PathType Container
    }).Count
}

function Assert-NoInstallTransactionResidue {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $staging = @(Get-ChildItem -LiteralPath $Fixture.PersistentRoot -Directory -Filter '.dtmapi-staging-*' -ErrorAction SilentlyContinue)
    Assert-InstallTransactionTest -Condition ($staging.Count -eq 0) -Message "$Label left a package staging directory."
    $saveRoot = Split-Path -Parent $Fixture.ModInfosPath
    $temps = @()
    if (Test-Path -LiteralPath $saveRoot -PathType Container) {
        $temps = @(Get-ChildItem -LiteralPath $saveRoot -Force -Filter 'mod_infos.json.tmp-*' -ErrorAction SilentlyContinue)
    }
    Assert-InstallTransactionTest -Condition ($temps.Count -eq 0) -Message "$Label left an enablement temp file."
}

function Assert-CompletePublishedInstall {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label,
        [int] $ExpectedBundledModsCount = $publishedDefinitions.Count
    )

    Assert-InstallTransactionTest -Condition ((Get-PublishedDestinationCount -Fixture $Fixture) -eq $publishedDefinitions.Count) -Message "$Label did not publish every expected package."
    $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $Fixture.ModInfosPath | ConvertFrom-Json
    Assert-InstallTransactionTest -Condition ($null -ne $data.modInfos.PSObject.Properties['Workshop.9999999999']) -Message "$Label lost the third-party enablement sentinel."
    foreach ($definition in $publishedDefinitions) {
        $id = 'Local.' + [string]$definition.OfficialFolder
        Assert-InstallTransactionTest -Condition ($null -ne $data.modInfos.PSObject.Properties[$id]) -Message "$Label missed enablement $id."
        $dest = Join-Path $Fixture.ModsRoot ([string]$definition.OfficialFolder)
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath (Join-Path $dest 'Content\DTMAPI\manifest.json') -PathType Leaf) -Message "$Label missed the package manifest for $($definition.OfficialFolder)."
    }

    $releaseManifestPath = Join-Path $Fixture.StateDir 'release-manifest.json'
    Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $releaseManifestPath -PathType Leaf) -Message "$Label did not write release-manifest.json."
    $releaseManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseManifestPath | ConvertFrom-Json
    Assert-InstallTransactionTest -Condition (@($releaseManifest.BundledMods).Count -eq $ExpectedBundledModsCount) -Message "$Label BundledMods count did not match the current invocation receipt count $ExpectedBundledModsCount."
    Assert-NoInstallTransactionResidue -Fixture $Fixture -Label $Label
}

function Assert-OilCurrentVersionProjection {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    foreach ($definition in $developerDefinitions) {
        $dest = Join-Path $Fixture.ModsRoot ([string]$definition.OfficialFolder)
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $dest -PathType Container) -Message "$Label missed developer package $($definition.OfficialFolder)."
    }

    $oilDest = Join-Path $Fixture.ModsRoot 'DTMAPI_Oil'
    $stagedManifestPath = Join-Path $oilDest 'Content\DTMAPI\manifest.json'
    $stagedInfoPath = Join-Path $oilDest 'info.json'
    $sourceManifestPath = Join-Path $repo 'products\first-party\Oil\manifest.json'
    $sourceInfoPath = Join-Path $repo 'products\first-party\Oil\official-info.json'
    foreach ($requiredPath in @($stagedManifestPath, $stagedInfoPath, $sourceManifestPath, $sourceInfoPath)) {
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $requiredPath -PathType Leaf) -Message "$Label missed version projection input $requiredPath."
    }

    $sourceManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourceManifestPath | ConvertFrom-Json
    $sourceInfo = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourceInfoPath | ConvertFrom-Json
    $stagedManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $stagedManifestPath | ConvertFrom-Json
    $stagedInfo = Get-Content -Raw -Encoding UTF8 -LiteralPath $stagedInfoPath | ConvertFrom-Json
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $publishText = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-mod-publish-zh.json') | ConvertFrom-Json
    $releaseManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Fixture.StateDir 'release-manifest.json') | ConvertFrom-Json

    $oilCatalog = @($catalog.products | Where-Object { [string]$_.uniqueId -eq 'DTMAPI.OilMod' })
    $oilPublish = @($publishText.mods | Where-Object { [string]$_.uniqueId -eq 'DTMAPI.OilMod' })
    $oilReceipt = @($releaseManifest.BundledMods | Where-Object { [string]$_.UniqueID -eq 'DTMAPI.OilMod' })
    Assert-InstallTransactionTest -Condition ($oilCatalog.Count -eq 1) -Message "$Label did not find exactly one Oil Catalog row."
    Assert-InstallTransactionTest -Condition ($oilPublish.Count -eq 1) -Message "$Label did not find exactly one Oil publish-text row."
    Assert-InstallTransactionTest -Condition ($oilReceipt.Count -eq 1) -Message "$Label did not find exactly one Oil release receipt."
    Assert-InstallTransactionTest -Condition (@($releaseManifest.BundledMods).Count -eq $developerDefinitions.Count) -Message "$Label release receipt count did not match the non-QA developer definition count $($developerDefinitions.Count)."

    $expectedVersion = [string]$oilCatalog[0].sourceVersion
    $projectedVersions = @(
        [string]$sourceManifest.Version,
        [string]$sourceInfo.version,
        [string]$stagedManifest.Version,
        [string]$stagedInfo.version,
        [string]$oilPublish[0].version,
        [string]$oilReceipt[0].Version
    )
    $distinctVersions = @($projectedVersions | Sort-Object -Unique)
    Assert-InstallTransactionTest -Condition ($expectedVersion -eq '0.3.1-dtmapi') -Message "$Label Oil Catalog current version changed unexpectedly to $expectedVersion."
    Assert-InstallTransactionTest -Condition ($distinctVersions.Count -eq 1 -and $distinctVersions[0] -eq $expectedVersion) -Message "$Label Oil current versions diverged: $($projectedVersions -join ', ')."
    Assert-InstallTransactionTest -Condition ([string]$oilCatalog[0].targetVersion -eq '1.0.0' -and [string]$oilCatalog[0].releaseEligibility -eq 'PrototypeBlocked') -Message "$Label promoted or rewrote Oil's blocked 1.0.0 target."
    Assert-InstallTransactionTest -Condition ([bool]$oilReceipt[0].ContentOnly) -Message "$Label Oil receipt did not preserve ContentOnly=true."
    Assert-InstallTransactionTest -Condition ([string]::IsNullOrWhiteSpace([string]$oilReceipt[0].PackageDll)) -Message "$Label Oil receipt unexpectedly names a package DLL."
    Assert-InstallTransactionTest -Condition ($null -eq $sourceManifest.PSObject.Properties['MinimumDTMApiVersion'] -and $null -eq $stagedManifest.PSObject.Properties['MinimumDTMApiVersion']) -Message "$Label Oil source or staged manifest gained a DTMAPI minimum."
    Assert-InstallTransactionTest -Condition (@(Get-ChildItem -LiteralPath $oilDest -Recurse -File -Filter '*.dll' -ErrorAction Stop).Count -eq 0) -Message "$Label Oil staged package contains a DLL."
    Assert-NoInstallTransactionResidue -Fixture $Fixture -Label $Label
}

$tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$tempRoot = [System.IO.Path]::GetFullPath((Join-Path $tempBase ('DTMAPI install transaction 中文 ' + [Guid]::NewGuid().ToString('N'))))
Assert-InstallTransactionTest -Condition ($tempRoot.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase)) -Message "Temporary root escaped system temp: $tempRoot"
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
$managedExternalGameDir = ''

try {
    Assert-InstallTransactionTest -Condition ($publishedDefinitions.Count -gt 0) -Message 'Published definition inventory is empty.'
    $managedAuthorSdkIds = @($managedAuthorSdkDefinitions | ForEach-Object { [string]$_.UniqueID } | Sort-Object)
    $productCatalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $catalogAdvancedIds = @($productCatalog.products | Where-Object {
        $null -ne $_.PSObject.Properties['codeModKind'] -and
        [string]$_.codeModKind -eq 'Advanced'
    } | ForEach-Object { [string]$_.uniqueId } | Sort-Object)
    Assert-InstallTransactionTest -Condition ($catalogAdvancedIds.Count -gt 0) -Message 'The Catalog contains no admitted Advanced products.'
    Assert-InstallTransactionTest -Condition ([string]::Join('|', $managedAuthorSdkIds) -eq [string]::Join('|', $catalogAdvancedIds)) -Message "Managed Author SDK transaction identities diverged from the Catalog Advanced set. definitions=$([string]::Join('|', $managedAuthorSdkIds)); catalog=$([string]::Join('|', $catalogAdvancedIds))."
    Assert-InstallTransactionTest -Condition ($publishedDefinitions.Count -lt $allPublishedDefinitions.Count) -Message 'Generic published transaction inventory did not exclude managed Author SDK products.'
    Assert-InstallTransactionTest -Condition ($developerDefinitions.Count -gt $publishedDefinitions.Count) -Message 'Non-QA developer definition inventory did not extend the published inventory.'
    $installText = [System.IO.File]::ReadAllText($installScript, [System.Text.Encoding]::UTF8)
    Assert-InstallTransactionTest -Condition ($installText -match 'RollbackRefusedUnknownChanges') -Message 'Installer does not fail closed when a just-published package fingerprint changes.'
    foreach ($requiredPausedAuthorSdkToken in @(
        'New managed product installation is paused for DTMAPI 0.6.1',
        'the unpublished Author SDK still targets the retired <game>/Mods root',
        'Existing SDK deployment-status, install-local-status, recover and withdraw commands remain available for old deployments')) {
        Assert-InstallTransactionTest -Condition ($installText.Contains($requiredPausedAuthorSdkToken)) `
            -Message "Managed Author SDK pause boundary is missing: $requiredPausedAuthorSdkToken"
    }
    foreach ($forbiddenRetiredAuthorSdkInstallToken in @(
        'Read-DtmApiAuthorSdkCatalogPackagePreflight',
        'Invoke-DtmApiAuthorSdkInstallFault',
        '& $authorSdkExe install-local $authorPackage',
        "Source = 'AuthorSdkAtomicLocalInstall'",
        'DTMAPI_INSTALL_AUTHOR_SDK_ONLY_UNIQUE_ID',
        'DTMAPI_INSTALL_AUTHOR_SDK_CORRUPT_REPORT',
        'New-DtmApiAuthorSdkInstallTransaction',
        'Restore-DtmApiAuthorSdkInstallTransaction',
        'Complete-DtmApiAuthorSdkInstallTransaction',
        'Complete-DtmApiAuthorSdkUncommittedInstallTransaction',
        '.dtmapi-installer-author-sdk',
        'before-source-state.bin',
        'foreach ($reportAttempt in 1..2)')) {
        Assert-InstallTransactionTest -Condition (-not $installText.Contains($forbiddenRetiredAuthorSdkInstallToken)) `
            -Message "Installer still contains a retired Author SDK install-local path: $forbiddenRetiredAuthorSdkInstallToken"
    }
    Assert-InstallTransactionTest -Condition ($installText -match "DestructiveAuthority = 'None'") -Message 'Failure diagnostics do not explicitly deny destructive authority.'
    Assert-InstallTransactionTest -Condition ($installText -notmatch "deployedManifest\.Version -cne '[0-9]") -Message 'Managed Advanced deployment validation still hard-codes one product version.'
    $pausedPlanIndex = $installText.IndexOf('$officialLocalMods = @()', [System.StringComparison]::Ordinal)
    $pausedSelectionIndex = $installText.IndexOf('if (-not $SkipOfficialLocalMods)', [System.StringComparison]::Ordinal)
    $runtimeCommitIndex = $installText.IndexOf('Complete-DtmApiRuntimeInstallTransaction -Transaction $script:DtmRuntimeInstallTransaction', [System.StringComparison]::Ordinal)
    Assert-InstallTransactionTest -Condition (
        $pausedPlanIndex -ge 0 -and
        $pausedSelectionIndex -gt $pausedPlanIndex -and
        $runtimeCommitIndex -gt $pausedSelectionIndex
    ) -Message 'Deterministic managed-product plan and pause preflight do not precede the Runtime commit.'
    Test-DtmApiWindowsPowerShellSyntax -Paths @($installScript, $PSCommandPath) -AllowCoreFallback

    foreach ($pausedMode in @(
        [ordered]@{ Label = 'published'; AllDeveloper = $false },
        [ordered]@{ Label = 'all-developer'; AllDeveloper = $true }
    )) {
        $pausedFixture = New-InstallTransactionFixture -Root (Join-Path $tempRoot ('00 paused preflight ' + [string]$pausedMode.Label))
        Write-TransactionTestText -Path (Join-Path $pausedFixture.GameDir 'preflight-sentinel.txt') -Text ('unchanged-' + [string]$pausedMode.Label)
        $pausedBefore = Get-ManagedAuthorSdkTreeFingerprint -Root $pausedFixture.Root
        $pausedResult = Invoke-TransactionInstaller -Fixture $pausedFixture `
            -AllDeveloperDefinitions:([bool]$pausedMode.AllDeveloper) `
            -IncludeManagedProducts
        $pausedAfter = Get-ManagedAuthorSdkTreeFingerprint -Root $pausedFixture.Root
        $pausedOutput = [string]::Join(' | ', @($pausedResult.Output))
        Assert-InstallTransactionTest -Condition ($pausedResult.ExitCode -ne 0) -Message "$($pausedMode.Label) managed-product pause unexpectedly succeeded."
        $reportedZeroMutationBoundary =
            $pausedOutput -match 'blocked by deterministic product preflight' -and
            $pausedOutput -match 'before any Runtime' -and
            $pausedOutput -match 'enablement mutation'
        Assert-InstallTransactionTest -Condition $reportedZeroMutationBoundary -Message "$($pausedMode.Label) pause did not report its zero-mutation boundary. Output=$pausedOutput"
        Assert-InstallTransactionTest -Condition ($pausedBefore -ceq $pausedAfter) -Message "$($pausedMode.Label) pause changed the valid game/persistent fixture tree before failing."
        Assert-InstallTransactionTest -Condition (-not (Test-Path -LiteralPath $pausedFixture.StateDir)) -Message "$($pausedMode.Label) pause wrote Runtime/install failure state."
        Assert-InstallTransactionTest -Condition (-not (Test-Path -LiteralPath $pausedFixture.ModsRoot)) -Message "$($pausedMode.Label) pause created the official MODS root."
        Assert-InstallTransactionTest -Condition (-not (Test-Path -LiteralPath $pausedFixture.ModInfosPath)) -Message "$($pausedMode.Label) pause created official enablement state."
    }

    Assert-DtmApiManifestInfoVersionParity -Manifest ([ordered]@{ Version = '0.3.1-dtmapi' }) -Info ([ordered]@{ version = '0.3.1-dtmapi' }) -Context 'matching version fixture'
    $mismatchError = ''
    try {
        Assert-DtmApiManifestInfoVersionParity -Manifest ([ordered]@{ Version = '0.3.1-dtmapi' }) -Info ([ordered]@{ version = '1.0.0' }) -Context 'mismatched version fixture'
    }
    catch {
        $mismatchError = [string]$_.Exception.Message
    }
    Assert-InstallTransactionTest -Condition ($mismatchError -match 'version projection mismatch' -and $mismatchError -match '0\.3\.1-dtmapi' -and $mismatchError -match '1\.0\.0') -Message 'Manifest/info version mismatch did not fail closed with both divergent values.'

    $malformed = New-InstallTransactionFixture -Root (Join-Path $tempRoot '01 malformed preflight')
    Write-TransactionTestText -Path $malformed.ModInfosPath -Text '{"modInfos":'
    $malformedHash = (Get-FileHash -LiteralPath $malformed.ModInfosPath -Algorithm SHA256).Hash
    $firstMalformed = Invoke-TransactionInstaller -Fixture $malformed
    Assert-InstallTransactionTest -Condition ($firstMalformed.ExitCode -ne 0) -Message 'Malformed mod_infos.json unexpectedly installed successfully.'
    Assert-InstallTransactionTest -Condition ((Get-PublishedDestinationCount -Fixture $malformed) -eq 0) -Message 'Malformed preflight published a package directory.'
    Assert-InstallTransactionTest -Condition ((Get-FileHash -LiteralPath $malformed.ModInfosPath -Algorithm SHA256).Hash -eq $malformedHash) -Message 'Malformed preflight changed mod_infos.json bytes.'
    Assert-NoInstallTransactionResidue -Fixture $malformed -Label 'Malformed preflight'
    $malformedState = Get-LatestInstallFailureState -Fixture $malformed
    $malformedAttempt = @($malformedState.OfficialLocalAttempts | Where-Object { [string]$_.Phase -eq 'EnablementPreflight' } | Select-Object -Last 1)
    Assert-InstallTransactionTest -Condition ($malformedAttempt.Count -eq 1) -Message 'Malformed failure state omitted the EnablementPreflight attempt.'
    Assert-InstallTransactionTest -Condition ([string]$malformedState.Error -match 'Could not read .*Refusing to overwrite mod_infos.json') -Message 'Malformed failure state omitted the fail-closed read error.'

    $secondMalformed = Invoke-TransactionInstaller -Fixture $malformed
    Assert-InstallTransactionTest -Condition ($secondMalformed.ExitCode -ne 0) -Message 'Unchanged malformed mod_infos.json unexpectedly succeeded on retry.'
    Assert-InstallTransactionTest -Condition ((Get-PublishedDestinationCount -Fixture $malformed) -eq 0) -Message 'Unchanged malformed retry published a package directory.'
    Assert-InstallTransactionTest -Condition ((Get-FileHash -LiteralPath $malformed.ModInfosPath -Algorithm SHA256).Hash -eq $malformedHash) -Message 'Unchanged malformed retry changed mod_infos.json bytes.'
    Assert-NoInstallTransactionResidue -Fixture $malformed -Label 'Malformed retry'

    Write-ValidTransactionModInfos -Path $malformed.ModInfosPath
    $malformedRecovery = Invoke-TransactionInstaller -Fixture $malformed
    Assert-InstallTransactionTest -Condition ($malformedRecovery.ExitCode -eq 0) -Message "Malformed recovery retry failed: $($malformedRecovery.Output -join ' | ')"
    Assert-CompletePublishedInstall -Fixture $malformed -Label 'Malformed recovery retry'

    foreach ($invalidShape in @(
        [ordered]@{ Label = 'array root'; Json = '[]' },
        [ordered]@{ Label = 'array modInfos'; Json = '{"modInfos":[]}' }
    )) {
        $shapeFixture = New-InstallTransactionFixture -Root (Join-Path $tempRoot ('invalid shape ' + [string]$invalidShape.Label))
        Write-TransactionTestText -Path $shapeFixture.ModInfosPath -Text ([string]$invalidShape.Json)
        $shapeHash = (Get-FileHash -LiteralPath $shapeFixture.ModInfosPath -Algorithm SHA256).Hash
        $shapeResult = Invoke-TransactionInstaller -Fixture $shapeFixture
        Assert-InstallTransactionTest -Condition ($shapeResult.ExitCode -ne 0) -Message "$($invalidShape.Label) unexpectedly installed successfully."
        Assert-InstallTransactionTest -Condition ((Get-PublishedDestinationCount -Fixture $shapeFixture) -eq 0) -Message "$($invalidShape.Label) published a package directory."
        Assert-InstallTransactionTest -Condition ((Get-FileHash -LiteralPath $shapeFixture.ModInfosPath -Algorithm SHA256).Hash -eq $shapeHash) -Message "$($invalidShape.Label) changed mod_infos.json bytes."
        Assert-NoInstallTransactionResidue -Fixture $shapeFixture -Label ([string]$invalidShape.Label)
    }

    $writeFailure = New-InstallTransactionFixture -Root (Join-Path $tempRoot '02 write failure rollback')
    Write-ValidTransactionModInfos -Path $writeFailure.ModInfosPath
    $writeFailureHash = (Get-FileHash -LiteralPath $writeFailure.ModInfosPath -Algorithm SHA256).Hash
    $lock = [System.IO.File]::Open($writeFailure.ModInfosPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $failedWrite = Invoke-TransactionInstaller -Fixture $writeFailure
    }
    finally {
        $lock.Dispose()
    }
    Assert-InstallTransactionTest -Condition ($failedWrite.ExitCode -ne 0) -Message 'Locked mod_infos.json unexpectedly installed successfully.'
    Assert-InstallTransactionTest -Condition ((Get-FileHash -LiteralPath $writeFailure.ModInfosPath -Algorithm SHA256).Hash -eq $writeFailureHash) -Message 'Enablement write failure changed mod_infos.json bytes.'
    Assert-InstallTransactionTest -Condition ((Get-PublishedDestinationCount -Fixture $writeFailure) -eq 0) -Message 'Enablement write failure did not roll back its just-published package.'
    Assert-NoInstallTransactionResidue -Fixture $writeFailure -Label 'Enablement write failure'

    $writeFailureState = Get-LatestInstallFailureState -Fixture $writeFailure
    $firstFolder = [string]$publishedDefinitions[0].OfficialFolder
    $writeAttempt = @($writeFailureState.OfficialLocalAttempts | Where-Object { [string]$_.OfficialFolder -eq $firstFolder } | Select-Object -Last 1)
    Assert-InstallTransactionTest -Condition ($writeAttempt.Count -eq 1) -Message 'Write failure state omitted the first package attempt.'
    Assert-InstallTransactionTest -Condition ([string]$writeAttempt[0].Phase -eq 'EnablementCommit') -Message 'Write failure phase was not EnablementCommit.'
    Assert-InstallTransactionTest -Condition ([bool]$writeAttempt[0].PublishSucceeded) -Message 'Write failure did not record the transient publish.'
    Assert-InstallTransactionTest -Condition ([string]$writeAttempt[0].PublishRollback -eq 'Completed') -Message 'Write failure did not record a completed publish rollback.'
    Assert-InstallTransactionTest -Condition (-not [bool]$writeAttempt[0].PackagePresentAfterFailure) -Message 'Write failure claims the rolled-back package is still present.'
    Assert-InstallTransactionTest -Condition ([string]$writeAttempt[0].DestructiveAuthority -eq 'None') -Message 'Write failure diagnostics expose destructive authority.'
    $rolledBackInstalledRows = @($writeFailureState.FilesInstalledBeforeFailure | Where-Object { [string]$_.Kind -like 'official-local-*' })
    Assert-InstallTransactionTest -Condition ($rolledBackInstalledRows.Count -eq 0) -Message 'Failure state lists rolled-back package files as installed.'
    $modInfosBackups = @($writeFailureState.BackupsCreatedBeforeFailure | Where-Object { [string]$_.Kind -eq 'mod-infos-before-install' })
    Assert-InstallTransactionTest -Condition ($modInfosBackups.Count -eq 1) -Message 'Write failure did not preserve exactly one before-image backup.'
    Assert-InstallTransactionTest -Condition ((Get-FileHash -LiteralPath ([string]$modInfosBackups[0].BackupPath) -Algorithm SHA256).Hash -eq $writeFailureHash) -Message 'Write failure before-image backup does not match the original mod_infos.json.'

    $writeRecovery = Invoke-TransactionInstaller -Fixture $writeFailure
    Assert-InstallTransactionTest -Condition ($writeRecovery.ExitCode -eq 0) -Message "Write-failure recovery retry failed: $($writeRecovery.Output -join ' | ')"
    Assert-CompletePublishedInstall -Fixture $writeFailure -Label 'Write-failure recovery retry'

    $drift = New-InstallTransactionFixture -Root (Join-Path $tempRoot '03 post-fingerprint drift')
    Write-ValidTransactionModInfos -Path $drift.ModInfosPath
    $driftOriginalHash = (Get-FileHash -LiteralPath $drift.ModInfosPath -Algorithm SHA256).Hash
    $driftFolder = [string]$publishedDefinitions[0].OfficialFolder
    $driftDest = Join-Path $drift.ModsRoot $driftFolder
    $driftSignal = Join-Path $drift.PersistentRoot ('.dtmapi-transaction-test-' + $driftFolder + '-post-fingerprint.signal')
    $driftSentinel = Join-Path $driftDest 'unknown-after-fingerprint.txt'
    $driftWriter = Start-Job -ScriptBlock {
        param($SignalPath, $SentinelPath)
        $deadline = [DateTime]::UtcNow.AddSeconds(20)
        while (-not (Test-Path -LiteralPath $SignalPath -PathType Leaf)) {
            if ([DateTime]::UtcNow -ge $deadline) {
                throw "Timed out waiting for rollback fingerprint signal: $SignalPath"
            }
            Start-Sleep -Milliseconds 20
        }
        [System.IO.File]::WriteAllText($SentinelPath, 'foreign-drift-sentinel', (New-Object System.Text.UTF8Encoding($false)))
    } -ArgumentList $driftSignal, $driftSentinel
    $oldTestMode = $env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE
    $oldTestPause = $env:DTMAPI_TEST_ROLLBACK_POST_FINGERPRINT_PAUSE_MS
    $driftLock = [System.IO.File]::Open($drift.ModInfosPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE = '1'
        $env:DTMAPI_TEST_ROLLBACK_POST_FINGERPRINT_PAUSE_MS = '4000'
        $driftResult = Invoke-TransactionInstaller -Fixture $drift
    }
    finally {
        $driftLock.Dispose()
        $env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE = $oldTestMode
        $env:DTMAPI_TEST_ROLLBACK_POST_FINGERPRINT_PAUSE_MS = $oldTestPause
        Wait-Job -Job $driftWriter -Timeout 25 | Out-Null
        $driftJobOutput = @(Receive-Job -Job $driftWriter -ErrorAction Continue 2>&1)
        $driftJobState = [string]$driftWriter.State
        Remove-Job -Job $driftWriter -Force
        if (Test-Path -LiteralPath $driftSignal -PathType Leaf) {
            Remove-Item -LiteralPath $driftSignal -Force
        }
    }
    Assert-InstallTransactionTest -Condition ($driftJobState -eq 'Completed') -Message "Fingerprint drift writer did not complete: state=$driftJobState output=$($driftJobOutput -join ' | ')"
    Assert-InstallTransactionTest -Condition ($driftResult.ExitCode -ne 0) -Message 'Post-fingerprint drift unexpectedly installed successfully.'
    Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $driftSentinel -PathType Leaf) -Message 'Post-fingerprint drift sentinel was deleted.'
    Assert-InstallTransactionTest -Condition ((Get-FileHash -LiteralPath $drift.ModInfosPath -Algorithm SHA256).Hash -eq $driftOriginalHash) -Message 'Post-fingerprint drift changed mod_infos.json bytes.'
    $driftState = Get-LatestInstallFailureState -Fixture $drift
    $driftAttempt = @($driftState.OfficialLocalAttempts | Where-Object { [string]$_.OfficialFolder -eq $driftFolder } | Select-Object -Last 1)
    Assert-InstallTransactionTest -Condition ($driftAttempt.Count -eq 1) -Message 'Post-fingerprint drift failure state omitted the package attempt.'
    Assert-InstallTransactionTest -Condition ([string]$driftAttempt[0].PublishRollback -eq 'RollbackRefusedUnknownChanges') -Message 'Post-fingerprint drift did not fail closed after the second fingerprint.'
    Assert-InstallTransactionTest -Condition ([bool]$driftAttempt[0].PackagePresentAfterFailure) -Message 'Post-fingerprint drift did not restore the published package path.'
    Assert-NoInstallTransactionResidue -Fixture $drift -Label 'Post-fingerprint drift'
    Remove-Item -LiteralPath $driftDest -Recurse -Force
    $driftRecovery = Invoke-TransactionInstaller -Fixture $drift
    Assert-InstallTransactionTest -Condition ($driftRecovery.ExitCode -eq 0) -Message "Post-fingerprint drift operator recovery failed: $($driftRecovery.Output -join ' | ')"
    Assert-CompletePublishedInstall -Fixture $drift -Label 'Post-fingerprint drift operator recovery'

    $foreign = New-InstallTransactionFixture -Root (Join-Path $tempRoot '04 pre-existing foreign destination')
    Write-ValidTransactionModInfos -Path $foreign.ModInfosPath
    $foreignFolder = [string]$publishedDefinitions[0].OfficialFolder
    $foreignDest = Join-Path $foreign.ModsRoot $foreignFolder
    $foreignSentinel = Join-Path $foreignDest 'foreign-owner-sentinel.txt'
    Write-TransactionTestText -Path $foreignSentinel -Text 'foreign-owner-bytes'
    $foreignSentinelHash = (Get-FileHash -LiteralPath $foreignSentinel -Algorithm SHA256).Hash
    $foreignFirst = Invoke-TransactionInstaller -Fixture $foreign
    Assert-InstallTransactionTest -Condition ($foreignFirst.ExitCode -eq 0) -Message "Pre-existing foreign destination run failed: $($foreignFirst.Output -join ' | ')"
    Assert-InstallTransactionTest -Condition ((Get-FileHash -LiteralPath $foreignSentinel -Algorithm SHA256).Hash -eq $foreignSentinelHash) -Message 'Pre-existing foreign destination bytes changed.'
    $foreignData = Get-Content -Raw -Encoding UTF8 -LiteralPath $foreign.ModInfosPath | ConvertFrom-Json
    Assert-InstallTransactionTest -Condition ($null -eq $foreignData.modInfos.PSObject.Properties['Local.' + $foreignFolder]) -Message 'Pre-existing foreign destination gained DTMAPI enablement authority.'
    Assert-InstallTransactionTest -Condition ((Get-PublishedDestinationCount -Fixture $foreign) -eq $publishedDefinitions.Count) -Message 'Pre-existing foreign destination run did not install the remaining packages.'
    $foreignRelease = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $foreign.StateDir 'release-manifest.json') | ConvertFrom-Json
    Assert-InstallTransactionTest -Condition (@($foreignRelease.BundledMods).Count -eq ($publishedDefinitions.Count - 1)) -Message 'Pre-existing foreign destination receipt count was not fail-closed.'
    Remove-Item -LiteralPath $foreignDest -Recurse -Force
    $foreignRecovery = Invoke-TransactionInstaller -Fixture $foreign
    Assert-InstallTransactionTest -Condition ($foreignRecovery.ExitCode -eq 0) -Message "Pre-existing foreign destination operator recovery failed: $($foreignRecovery.Output -join ' | ')"
    Assert-CompletePublishedInstall -Fixture $foreign -Label 'Pre-existing foreign destination operator recovery' -ExpectedBundledModsCount 1

    $versionProjection = New-InstallTransactionFixture -Root (Join-Path $tempRoot '05 content-only version projection')
    Write-ValidTransactionModInfos -Path $versionProjection.ModInfosPath
    $versionProjectionResult = Invoke-TransactionInstaller -Fixture $versionProjection -AllDeveloperDefinitions
    Assert-InstallTransactionTest -Condition ($versionProjectionResult.ExitCode -eq 0) -Message "All-developer version projection install failed: $($versionProjectionResult.Output -join ' | ')"
    Assert-OilCurrentVersionProjection -Fixture $versionProjection -Label 'All-developer Oil current version projection'

    if ($ManagedAuthorSdk) {
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $ManagedAuthorSdkArtifactRoot -PathType Container) -Message "Managed Author SDK artifact root is missing: $ManagedAuthorSdkArtifactRoot"
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $ManagedReferenceGameDir -PathType Container) -Message "Managed reference game root is missing: $ManagedReferenceGameDir"
        $managedDefinition = @($managedAuthorSdkDefinitions | Where-Object {
            [string]$_.UniqueID -ceq 'DTMAPI.ZoomMod'
        })
        Assert-InstallTransactionTest -Condition ($managedDefinition.Count -eq 1) -Message 'Managed transaction lane requires exactly one DTMAPI.ZoomMod definition.'
        $managedUniqueId = [string]$managedDefinition[0].UniqueID
        $managedArtifact = Join-Path $tempRoot '06 managed Author SDK artifact'
        Copy-Item -LiteralPath $ManagedAuthorSdkArtifactRoot -Destination $managedArtifact -Recurse -Force
        $managedPackage = Join-Path $managedArtifact ([string]$managedDefinition[0].AuthorSdkPackageFile)
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $managedPackage -PathType Leaf) -Message "Managed transaction package is missing: $managedPackage"
        $managedSdkExe = Join-Path $managedArtifact 'author-sdk\DTMAPI-Author-SDK-0.1.0-win-x64\dtmapi-author.exe'
        Assert-InstallTransactionTest -Condition (Test-Path -LiteralPath $managedSdkExe -PathType Leaf) -Message "Managed transaction SDK executable is missing: $managedSdkExe"

        $managedSteamCommon = [System.IO.Path]::GetFullPath((Split-Path -Parent $ManagedReferenceGameDir))
        Assert-InstallTransactionTest -Condition (
            [string]::Equals((Split-Path -Leaf $managedSteamCommon), 'common', [System.StringComparison]::OrdinalIgnoreCase)
        ) -Message "Managed reference game is not an immediate steamapps/common child: $ManagedReferenceGameDir"
        $managedExternalGameDir = Join-Path $managedSteamCommon ('DTMAPI Author SDK Transaction Fixture ' + [Guid]::NewGuid().ToString('N'))
        Assert-InstallTransactionTest -Condition (
            [string]::Equals(
                [System.IO.Path]::GetFullPath((Split-Path -Parent $managedExternalGameDir)),
                $managedSteamCommon,
                [System.StringComparison]::OrdinalIgnoreCase)
        ) -Message "Managed transaction fixture escaped steamapps/common: $managedExternalGameDir"
        $managed = New-InstallTransactionFixture -Root (Join-Path $tempRoot '06 managed Author SDK transaction') `
            -GameDir $managedExternalGameDir
        Write-ValidTransactionModInfos -Path $managed.ModInfosPath
        Copy-ManagedAuthorSdkReferenceFiles -PackagePath $managedPackage `
            -ReferenceGameDir $ManagedReferenceGameDir -FixtureGameDir $managed.GameDir
        $managedSeed = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
            -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId
        Assert-InstallTransactionTest -Condition ($managedSeed.ExitCode -eq 0) -Message "Managed Author SDK seed install failed: $($managedSeed.Output -join ' | ')"

        $managedDestination = Join-Path (Join-Path $managed.GameDir 'Mods') $managedUniqueId
        $managedAuthorStateRoot = Join-Path $managed.StateDir 'author-sdk-state'
        $managedJournalMatches = @(Get-ChildItem -LiteralPath $managedAuthorStateRoot -Recurse -File -Filter ($managedUniqueId + '.journal.json'))
        $managedSourceStateMatches = @(Get-ChildItem -LiteralPath $managedAuthorStateRoot -Recurse -File -Filter 'source-state.json')
        Assert-InstallTransactionTest -Condition ($managedJournalMatches.Count -eq 1) -Message 'Managed seed did not create exactly one deployment journal.'
        Assert-InstallTransactionTest -Condition ($managedSourceStateMatches.Count -eq 1) -Message 'Managed seed did not create exactly one source state.'
        $managedJournal = $managedJournalMatches[0].FullName
        $managedSourceState = $managedSourceStateMatches[0].FullName
        $managedDestinationFingerprint = Get-ManagedAuthorSdkTreeFingerprint -Root $managedDestination
        $managedJournalHash = (Get-FileHash -LiteralPath $managedJournal -Algorithm SHA256).Hash
        $managedSourceStateHash = (Get-FileHash -LiteralPath $managedSourceState -Algorithm SHA256).Hash
        $managedPackageSha256 = (Get-FileHash -LiteralPath $managedPackage -Algorithm SHA256).Hash
        $managedInstalledManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (
            Join-Path $managedDestination 'Content\DTMAPI\manifest.json') | ConvertFrom-Json
        $managedExpectedVersion = [string]$managedInstalledManifest.Version
        $missingPackage = $managedPackage + '.missing-fixture'
        Move-Item -LiteralPath $managedPackage -Destination $missingPackage
        try {
            $managedMissing = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
                -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId
        }
        finally {
            Move-Item -LiteralPath $missingPackage -Destination $managedPackage
        }
        Assert-InstallTransactionTest -Condition ($managedMissing.ExitCode -ne 0) -Message 'Managed missing-source preflight unexpectedly succeeded.'
        Assert-ManagedAuthorSdkPreState -Fixture $managed -UniqueId $managedUniqueId `
            -DestinationFingerprint $managedDestinationFingerprint -JournalHash $managedJournalHash `
            -SourceStateHash $managedSourceStateHash -Label 'Managed missing-source preflight'

        $validPackageBackup = $managedPackage + '.valid-fixture'
        Copy-Item -LiteralPath $managedPackage -Destination $validPackageBackup -Force
        try {
            Write-TransactionTestText -Path $managedPackage -Text 'not-a-valid-sdk-zip'
            $managedCorrupt = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
                -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId
        }
        finally {
            Move-Item -LiteralPath $validPackageBackup -Destination $managedPackage -Force
        }
        Assert-InstallTransactionTest -Condition ($managedCorrupt.ExitCode -ne 0) -Message 'Managed corrupt-source preflight unexpectedly succeeded.'
        Assert-ManagedAuthorSdkPreState -Fixture $managed -UniqueId $managedUniqueId `
            -DestinationFingerprint $managedDestinationFingerprint -JournalHash $managedJournalHash `
            -SourceStateHash $managedSourceStateHash -Label 'Managed corrupt-source preflight'

        $managedVersionMismatch = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
            -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId `
            -ExpectedVersion '0.0.0-transaction-fixture'
        Assert-InstallTransactionTest -Condition ($managedVersionMismatch.ExitCode -ne 0) -Message 'Managed Catalog/package version mismatch unexpectedly succeeded.'
        Assert-ManagedAuthorSdkPreState -Fixture $managed -UniqueId $managedUniqueId `
            -DestinationFingerprint $managedDestinationFingerprint -JournalHash $managedJournalHash `
            -SourceStateHash $managedSourceStateHash -Label 'Managed version-mismatch preflight'

        $managedPreflightFault = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
            -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId -FailPhase 'AfterPackagePreflight'
        Assert-InstallTransactionTest -Condition ($managedPreflightFault.ExitCode -ne 0) -Message 'Managed post-preflight fault unexpectedly succeeded.'
        Assert-ManagedAuthorSdkPreState -Fixture $managed -UniqueId $managedUniqueId `
            -DestinationFingerprint $managedDestinationFingerprint -JournalHash $managedJournalHash `
            -SourceStateHash $managedSourceStateHash -Label 'Managed post-preflight fault'

        foreach ($combinedCrashPoint in @(
            'install-local.after-prepare',
            'install-local.after-source-selection')) {
            $combined = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
                -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId `
                -AuthorFault ('crash:' + $combinedCrashPoint) -CorruptReport
            $combinedText = [string]::Join([Environment]::NewLine, $combined.Output)
            Assert-InstallTransactionTest -Condition ($combined.ExitCode -ne 0) -Message "Managed corrupt-report + $combinedCrashPoint unexpectedly registered success."
            Assert-InstallTransactionTest -Condition (
                $combinedText.Contains('RecoveryRequired') -and
                $combinedText.Contains('not CommittedLocalDevelopment') -and
                $combinedText.Contains('exact SDK recover command')
            ) -Message "Managed corrupt-report + $combinedCrashPoint did not fail with exact recovery guidance: $combinedText"
            Assert-InstallTransactionTest -Condition (
                -not $combinedText.Contains("Installed $managedUniqueId through Author SDK atomic reconciled")
            ) -Message "Managed corrupt-report + $combinedCrashPoint emitted a false installed result."
            $combinedFailureState = Get-LatestInstallFailureState -Fixture $managed
            $combinedAttempt = @($combinedFailureState.OfficialLocalAttempts | Where-Object {
                [string]$_.EnablementId -ceq $managedUniqueId
            } | Select-Object -Last 1)
            Assert-InstallTransactionTest -Condition ($combinedAttempt.Count -eq 1) `
                -Message "Managed corrupt-report + $combinedCrashPoint omitted the failed product attempt."
            Assert-InstallTransactionTest -Condition (
                [string]$combinedAttempt[0].Phase -ceq 'AuthorSdkAtomicLocalInstall' -and
                -not [bool]$combinedAttempt[0].PublishSucceeded -and
                [string]$combinedAttempt[0].Error -match 'RecoveryRequired'
            ) -Message "Managed corrupt-report + $combinedCrashPoint falsely registered a committed product attempt."
            Assert-InstallTransactionTest -Condition (
                $null -eq $combinedAttempt[0].PSObject.Properties['ReadOnlyReconciliation']
            ) -Message "Managed corrupt-report + $combinedCrashPoint falsely recorded committed read-only reconciliation."

            $oldAuthorStateRoot = $env:DTMAPI_AUTHOR_STATE_ROOT
            try {
                $env:DTMAPI_AUTHOR_STATE_ROOT = $managedAuthorStateRoot
                $pendingText = [string]::Join([Environment]::NewLine, @(
                    & $managedSdkExe install-local-status $managedUniqueId `
                        --game-root $managed.GameDir `
                        --expected-version $managedExpectedVersion `
                        --expected-package-sha256 $managedPackageSha256 `
                        --json 2>&1 | ForEach-Object { [string]$_ }))
                $pendingExit = $LASTEXITCODE
                $pending = $pendingText | ConvertFrom-Json
                Assert-InstallTransactionTest -Condition (
                    $pendingExit -eq 0 -and
                    [bool]$pending.success -and
                    [string]$pending.values.status -ceq 'RecoveryRequired'
                ) -Message "Managed corrupt-report + $combinedCrashPoint did not preserve a retryable RecoveryRequired marker: $pendingText"

                $recoverText = [string]::Join([Environment]::NewLine, @(
                    & $managedSdkExe recover $managedUniqueId --game-root $managed.GameDir --json 2>&1 |
                        ForEach-Object { [string]$_ }))
                $recoverExit = $LASTEXITCODE
                $recover = $recoverText | ConvertFrom-Json
                Assert-InstallTransactionTest -Condition (
                    $recoverExit -eq 0 -and
                    [bool]$recover.success -and
                    [string]$recover.values.recovered -ceq 'true'
                ) -Message "Managed corrupt-report + $combinedCrashPoint exact recovery failed: $recoverText"
            }
            finally {
                $env:DTMAPI_AUTHOR_STATE_ROOT = $oldAuthorStateRoot
            }
            Assert-ManagedAuthorSdkPreState -Fixture $managed -UniqueId $managedUniqueId `
                -DestinationFingerprint $managedDestinationFingerprint -JournalHash $managedJournalHash `
                -SourceStateHash $managedSourceStateHash -Label "Managed corrupt-report + $combinedCrashPoint recovery"
        }

        foreach ($postDeployFault in @(
            'install-local.after-deployment',
            'install-local.source.before-state',
            'install-local.after-source-selection',
            'update.commit.after-journal')) {
            $managedPostDeployFailure = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
                -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId -AuthorFault $postDeployFault
            Assert-InstallTransactionTest -Condition ($managedPostDeployFailure.ExitCode -ne 0) -Message "Managed $postDeployFault fault unexpectedly succeeded."
            Assert-ManagedAuthorSdkPreState -Fixture $managed -UniqueId $managedUniqueId `
                -DestinationFingerprint $managedDestinationFingerprint -JournalHash $managedJournalHash `
                -SourceStateHash $managedSourceStateHash -Label "Managed $postDeployFault rollback"
        }

        foreach ($crashPoint in @(
            'install-local.after-prepare',
            'install-local.after-source-selection')) {
            $managedCrash = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
                -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId `
                -AuthorFault ('crash:' + $crashPoint)
            Assert-InstallTransactionTest -Condition ($managedCrash.ExitCode -ne 0) -Message "Managed $crashPoint process-crash fixture unexpectedly succeeded."
            $oldAuthorStateRoot = $env:DTMAPI_AUTHOR_STATE_ROOT
            $oldAuthorFault = $env:DTMAPI_AUTHOR_FAULT
            try {
                $env:DTMAPI_AUTHOR_STATE_ROOT = $managedAuthorStateRoot
                if ($crashPoint -ceq 'install-local.after-source-selection') {
                    $env:DTMAPI_AUTHOR_FAULT = 'fail:install-local.recover.journal.after-state'
                }
                $recoverText = [string]::Join([Environment]::NewLine, @(& $managedSdkExe recover $managedUniqueId --game-root $managed.GameDir --json 2>&1 | ForEach-Object { [string]$_ }))
                $recoverExit = $LASTEXITCODE
                $recover = $recoverText | ConvertFrom-Json
                Assert-InstallTransactionTest -Condition (
                    $recoverExit -eq 0 -and
                    [bool]$recover.success -and
                    [string]$recover.values.recovered -ceq 'true'
                ) -Message "Managed $crashPoint durable recovery failed: $recoverText"
            }
            finally {
                $env:DTMAPI_AUTHOR_FAULT = $oldAuthorFault
                $env:DTMAPI_AUTHOR_STATE_ROOT = $oldAuthorStateRoot
            }
            Assert-ManagedAuthorSdkPreState -Fixture $managed -UniqueId $managedUniqueId `
                -DestinationFingerprint $managedDestinationFingerprint -JournalHash $managedJournalHash `
                -SourceStateHash $managedSourceStateHash -Label "Managed $crashPoint durable recovery"
        }

        $managedTerminalCommit = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
            -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId `
            -AuthorFault 'install-local.commit.after-journal'
        Assert-InstallTransactionTest -Condition (
            $managedTerminalCommit.ExitCode -eq 0 -and
            [string]::Join([Environment]::NewLine, $managedTerminalCommit.Output).Contains('Installed ' + $managedUniqueId)
        ) -Message "Managed Author SDK terminal write reconciliation failed: $($managedTerminalCommit.Output -join ' | ')"

        $managedCommit = Invoke-ManagedAuthorSdkTransactionInstaller -Fixture $managed `
            -ArtifactRoot $managedArtifact -UniqueId $managedUniqueId -CorruptReport
        Assert-InstallTransactionTest -Condition (
            $managedCommit.ExitCode -eq 0 -and
            [string]::Join([Environment]::NewLine, $managedCommit.Output).Contains('atomic reconciled')
        ) -Message "Managed Author SDK lost-response read-only reconciliation failed: $($managedCommit.Output -join ' | ')"
        $oldAuthorStateRoot = $env:DTMAPI_AUTHOR_STATE_ROOT
        try {
            $env:DTMAPI_AUTHOR_STATE_ROOT = $managedAuthorStateRoot
            $managedStatusText = [string]::Join([Environment]::NewLine, @(& $managedSdkExe deployment-status $managedUniqueId --game-root $managed.GameDir --json 2>&1 | ForEach-Object { [string]$_ }))
            $managedStatusExit = $LASTEXITCODE
            $managedStatus = $managedStatusText | ConvertFrom-Json
            Assert-InstallTransactionTest -Condition ($managedStatusExit -eq 0 -and [bool]$managedStatus.success -and [string]$managedStatus.values.status -ceq 'Installed') -Message 'Managed successful update did not preserve Author SDK deployment-status semantics.'
            $managedSourceText = [string]::Join([Environment]::NewLine, @(& $managedSdkExe source status $managedUniqueId --game-root $managed.GameDir --json 2>&1 | ForEach-Object { [string]$_ }))
            $managedSourceExit = $LASTEXITCODE
            $managedSource = $managedSourceText | ConvertFrom-Json
            Assert-InstallTransactionTest -Condition ($managedSourceExit -eq 0 -and [bool]$managedSource.success -and [string]$managedSource.values.mode -ceq 'LocalDevelopment') -Message 'Managed successful update did not preserve exact LocalDevelopment source status.'
        }
        finally {
            $env:DTMAPI_AUTHOR_STATE_ROOT = $oldAuthorStateRoot
        }
        Assert-InstallTransactionTest -Condition (-not (Test-Path -LiteralPath (Join-Path $managed.GameDir 'Mods\.dtmapi-installer-author-sdk'))) -Message 'Managed successful update left an outer installer transaction.'
    }

    if (-not $Quiet) {
        $managedSummary = if ($ManagedAuthorSdk) { ', managed-advanced=real-preflight/update/rollback' } else { '' }
        Write-Host "Developer official-local install transaction tests: OK (published=$($publishedDefinitions.Count), developer=$($developerDefinitions.Count), malformed=preflight, write-failure=rollback, drift=preserved, foreign=fail-closed, oil-version=aligned$managedSummary)"
    }
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($managedExternalGameDir)) {
        $resolvedManagedExternalGameDir = [System.IO.Path]::GetFullPath($managedExternalGameDir)
        $managedExternalParent = [System.IO.Path]::GetFullPath((Split-Path -Parent $resolvedManagedExternalGameDir))
        if ([string]::Equals((Split-Path -Leaf $managedExternalParent), 'common', [System.StringComparison]::OrdinalIgnoreCase) -and
            (Split-Path -Leaf $resolvedManagedExternalGameDir) -like 'DTMAPI Author SDK Transaction Fixture *' -and
            (Test-Path -LiteralPath $resolvedManagedExternalGameDir)) {
            Remove-Item -LiteralPath $resolvedManagedExternalGameDir -Recurse -Force
        }
    }
    $resolvedTempRoot = [System.IO.Path]::GetFullPath($tempRoot)
    if ($resolvedTempRoot.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedTempRoot) -like 'DTMAPI install transaction 中文 *' -and
        (Test-Path -LiteralPath $resolvedTempRoot)) {
        Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
    }
}
