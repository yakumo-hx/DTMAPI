param(
    [string] $Configuration = 'Release',
    [string] $OutputRoot = '',
    [switch] $SkipBuild,
    [switch] $RuntimeOnly,
    [switch] $ModsOnly
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\workshop-packages'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

if (-not $SkipBuild) {
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
    if (-not $ModsOnly) {
        & "$PSScriptRoot\build-player-doctor.ps1" -Configuration $Configuration
    }
}

function Clear-Directory {
    param([Parameter(Mandatory = $true)] [string] $Path)
    $preservedWorkshopInfo = $null
    if (Test-Path $Path) {
        $workshopInfoPath = Join-Path $Path 'workshop.json'
        if (Test-Path -LiteralPath $workshopInfoPath -PathType Leaf) {
            $preservedWorkshopInfo = [System.IO.File]::ReadAllBytes($workshopInfoPath)
        }
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    if ($null -ne $preservedWorkshopInfo) {
        [System.IO.File]::WriteAllBytes((Join-Path $Path 'workshop.json'), $preservedWorkshopInfo)
    }
}

function Copy-IfExists {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )
    if (Test-Path $Source) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
    }
}

function Get-DtmApiPublishMetadata {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $UniqueId
    )

    $metadataPath = Join-Path $RepoRoot 'tools\release\dtmapi-mod-publish-zh.json'
    if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
        throw "Missing DTMAPI publish metadata file: $metadataPath"
    }

    $metadata = Get-Content -Raw -Encoding UTF8 -LiteralPath $metadataPath | ConvertFrom-Json
    $entry = @($metadata.mods | Where-Object { $_.uniqueId -eq $UniqueId } | Select-Object -First 1)
    if ($entry.Count -ne 1) {
        throw "DTMAPI publish metadata does not contain exactly one entry for UniqueID '$UniqueId'."
    }

    return $entry[0]
}

function Assert-PlayerPackageExcludesQaHost {
    param(
        [Parameter(Mandatory = $true)] [string] $PackageRoot
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($PackageRoot).TrimEnd('\')
    $forbidden = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force | Where-Object {
        $relative = $_.FullName.Substring($resolvedRoot.Length).TrimStart('\').Replace('\', '/')
        $name = $_.Name
        $relative -match '(^|/)qa-host(/|$)' -or
        $name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
        $name -match '^(?i:DTMAPI\.(Smoke|Tests)\.(dll|pdb))$' -or
        $name -match '^(?i:qa-settings\.json|smoke-settings\.json|qa-host.*\.json)$'
    })
    if ($forbidden.Count -gt 0) {
        $relativeForbidden = @($forbidden | ForEach-Object {
            $_.FullName.Substring($resolvedRoot.Length).TrimStart('\').Replace('\', '/')
        } | Sort-Object -Unique)
        throw "Player Runtime package contains developer-only QA host material: $([string]::Join(', ', $relativeForbidden))"
    }
}

function Get-DtmApiWorkshopRuntimeAssemblyReceipts {
    param(
        [Parameter(Mandatory = $true)] [string] $PayloadRoot,
        [Parameter(Mandatory = $true)] [string[]] $FileNames
    )

    $actualDllNames = @(Get-ChildItem -LiteralPath $PayloadRoot -Filter '*.dll' -File |
        ForEach-Object { $_.Name } |
        Sort-Object)
    $expectedDllNames = @($FileNames | Sort-Object)
    if (($actualDllNames -join '|') -ne ($expectedDllNames -join '|')) {
        throw "Workshop Runtime payload DLL set is not exact. Expected=$($expectedDllNames -join ',') Actual=$($actualDllNames -join ',')"
    }

    $receipts = New-Object 'System.Collections.Generic.List[object]'
    foreach ($fileName in $FileNames) {
        $path = Join-Path $PayloadRoot $fileName
        $item = Get-Item -LiteralPath $path -ErrorAction Stop
        if ($item.Length -le 0) {
            throw "Workshop Runtime payload assembly is empty: $path"
        }

        try {
            $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($path)
            $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($path))
            $fileVersionAttributes = @($assembly.GetCustomAttributesData() | Where-Object {
                [string]$_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute'
            })
        }
        catch {
            throw "Workshop Runtime payload assembly metadata is unreadable: $path. $($_.Exception.Message)"
        }

        $expectedAssemblyName = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
        if (-not [string]::Equals([string]$assemblyName.Name, $expectedAssemblyName, [System.StringComparison]::Ordinal)) {
            throw "Workshop Runtime payload assembly identity mismatch for $fileName. Expected=$expectedAssemblyName Actual=$($assemblyName.Name)"
        }
        if ($fileVersionAttributes.Count -ne 1 -or $fileVersionAttributes[0].ConstructorArguments.Count -ne 1) {
            throw "Workshop Runtime payload assembly does not contain exactly one AssemblyFileVersionAttribute: $path"
        }
        $fileVersion = [string]$fileVersionAttributes[0].ConstructorArguments[0].Value
        if (-not [string]::Equals($fileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
            throw "Workshop Runtime payload assembly version mismatch for $fileName. Expected=$script:DtmApiBinaryVersion Actual=$fileVersion"
        }

        $receipts.Add([ordered]@{
            FileName = $fileName
            Length = [long]$item.Length
            Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
            FileVersion = $fileVersion
        }) | Out-Null
    }

    return $receipts.ToArray()
}

function Get-DtmApiWorkshopOptionalComponentReceipts {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $Configuration,
        [Parameter(Mandatory = $true)] [string] $PayloadRoot
    )

    $catalogPath = Join-Path $RepoRoot 'tools\release\dtmapi-product-catalog.json'
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $invariant = Get-DtmApiMapValue -Map $catalog -Key 'playerRuntimePackageInvariant' -Default $null
    $definitions = @((Get-DtmApiMapValue -Map $invariant -Key 'optionalComponents' -Default @()))
    $receipts = New-Object 'System.Collections.Generic.List[object]'
    $seenIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    $seenPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($definition in $definitions) {
        $componentId = [string](Get-DtmApiMapValue -Map $definition -Key 'id' -Default '')
        $distribution = [string](Get-DtmApiMapValue -Map $definition -Key 'distribution' -Default '')
        $loadPolicy = [string](Get-DtmApiMapValue -Map $definition -Key 'loadPolicy' -Default '')
        $relativePath = ([string](Get-DtmApiMapValue -Map $definition -Key 'relativePath' -Default '')).Replace('\', '/')
        $expectedAssemblyName = [string](Get-DtmApiMapValue -Map $definition -Key 'assemblyName' -Default '')
        $expectedAssemblyVersion = [string](Get-DtmApiMapValue -Map $definition -Key 'assemblyVersion' -Default '')
        $sourceProject = ([string](Get-DtmApiMapValue -Map $definition -Key 'sourceProject' -Default '')).Replace('/', '\')
        $targetFramework = [string](Get-DtmApiMapValue -Map $definition -Key 'targetFramework' -Default '')
        if ([string]::IsNullOrWhiteSpace($componentId) -or -not $seenIds.Add($componentId)) {
            throw "Player Runtime optional component IDs must be non-empty and unique: '$componentId'."
        }
        if ([string]::IsNullOrWhiteSpace($relativePath) -or -not $seenPaths.Add($relativePath) -or
            -not $relativePath.StartsWith('DTMAPI/components/', [System.StringComparison]::Ordinal) -or
            $relativePath.Contains('../') -or $relativePath.Contains('/..') -or
            $relativePath.StartsWith('BepInEx/', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Player Runtime optional component path is unsafe or duplicated: '$relativePath'."
        }
        if (-not [string]::Equals($distribution, 'dormant-shipped', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($targetFramework, 'netstandard2.0', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($expectedAssemblyVersion, $script:DtmApiAssemblyCompatibilityVersion, [System.StringComparison]::Ordinal)) {
            throw "Optional component '$componentId' must remain dormant-shipped and netstandard2.0."
        }

        $sourceProjectPath = Join-Path $RepoRoot $sourceProject
        if (-not (Test-Path -LiteralPath $sourceProjectPath -PathType Leaf)) {
            throw "Optional component source project is missing: $sourceProjectPath"
        }
        $fileName = [System.IO.Path]::GetFileName($relativePath)
        $sourcePath = Join-Path (Split-Path -Parent $sourceProjectPath) ("bin\{0}\{1}\{2}" -f $Configuration, $targetFramework, $fileName)
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Optional component build output is missing: $sourcePath"
        }
        $destinationPath = Join-Path $PayloadRoot ($relativePath.Replace('/', '\'))
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destinationPath) | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force

        $item = Get-Item -LiteralPath $destinationPath -ErrorAction Stop
        if ($item.Length -le 0) {
            throw "Optional component payload is empty: $destinationPath"
        }
        try {
            $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($destinationPath)
            $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($destinationPath))
            $fileVersionAttributes = @($assembly.GetCustomAttributesData() | Where-Object {
                [string]$_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute'
            })
            $targetFrameworkAttributes = @($assembly.GetCustomAttributesData() | Where-Object {
                [string]$_.AttributeType.FullName -eq 'System.Runtime.Versioning.TargetFrameworkAttribute'
            })
        }
        catch {
            throw "Optional component metadata is unreadable: $destinationPath. $($_.Exception.Message)"
        }
        if (-not [string]::Equals([string]$assemblyName.Name, $expectedAssemblyName, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$assemblyName.Version, $expectedAssemblyVersion, [System.StringComparison]::Ordinal)) {
            throw "Optional component assembly identity mismatch for '$componentId'. Expected=$expectedAssemblyName/$expectedAssemblyVersion Actual=$($assemblyName.Name)/$($assemblyName.Version)"
        }
        if ($fileVersionAttributes.Count -ne 1 -or $fileVersionAttributes[0].ConstructorArguments.Count -ne 1 -or
            -not [string]::Equals([string]$fileVersionAttributes[0].ConstructorArguments[0].Value, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
            throw "Optional component FileVersion mismatch for '$componentId'."
        }
        if ($targetFrameworkAttributes.Count -ne 1 -or $targetFrameworkAttributes[0].ConstructorArguments.Count -ne 1 -or
            -not [string]::Equals([string]$targetFrameworkAttributes[0].ConstructorArguments[0].Value, '.NETStandard,Version=v2.0', [System.StringComparison]::Ordinal)) {
            throw "Optional component target framework mismatch for '$componentId'."
        }

        $receipts.Add([ordered]@{
            ComponentId = $componentId
            Distribution = $distribution
            LoadPolicy = $loadPolicy
            RelativePath = $relativePath
            Length = [long]$item.Length
            Sha256 = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash.ToLowerInvariant()
            AssemblyName = [string]$assemblyName.Name
            AssemblyVersion = [string]$assemblyName.Version
            FileVersion = [string]$fileVersionAttributes[0].ConstructorArguments[0].Value
            TargetFramework = $targetFramework
            DefaultLoadState = [string](Get-DtmApiMapValue -Map $definition -Key 'defaultLoadState' -Default '')
            IncludedInDownloadPackage = [bool](Get-DtmApiMapValue -Map $definition -Key 'includedInDownloadPackage' -Default $false)
        }) | Out-Null
    }
    return $receipts.ToArray()
}

function Resolve-DtmApiWorkshopModSourceRoot {
    param(
        $Mod
    )

    $sourceRoot = Get-DtmApiMapValue -Map $Mod -Key 'SourceRoot' -Default ''
    if (-not [string]::IsNullOrWhiteSpace([string]$sourceRoot)) {
        return Join-Path $repo ([string]$sourceRoot)
    }
    throw "Release definition '$($Mod.UniqueID)' must declare SourceRoot after C1 physical classification."
}

if (-not $ModsOnly) {
    $runtimePackage = Join-Path $OutputRoot 'DTMAPI'
    Clear-Directory -Path $runtimePackage
    foreach ($bat in @('1_install_dtmapi.bat', '2_uninstall_dtmapi.bat', '3_check_dtmapi_status.bat', '4_collect_dtmapi_logs.bat')) {
        $batDestination = Join-Path $runtimePackage $bat
        Copy-Item -LiteralPath (Join-Path $repo "tools\release\runtime-workshop\$bat") -Destination $batDestination -Force
        # cmd.exe label seeking is unreliable for UTF-8 batch files with LF-only line endings.
        $batText = [System.IO.File]::ReadAllText($batDestination)
        $batText = [regex]::Replace($batText, "\r?\n", "`r`n")
        [System.IO.File]::WriteAllText($batDestination, $batText, (New-Object System.Text.UTF8Encoding($false)))
    }
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePackage 'icon.png')
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-preview.png') -Destination (Join-Path $runtimePackage 'preview.png')

    $installerTools = Join-Path $runtimePackage 'Content\DTMAPIInstaller\tools'
    New-Item -ItemType Directory -Force -Path $installerTools | Out-Null
    Copy-DtmApiTextFileUtf8Bom `
        -Source (Join-Path $repo 'tools\release\dtmapi-runtime-version.props') `
        -Destination (Join-Path $installerTools 'dtmapi-runtime-version.props')
    $installPreflightScriptNames = @('common.ps1', 'release-common.ps1', 'install-to-game.ps1', 'install-bepinex.ps1', 'probe-install-preflight.ps1', 'probe-powershell-host.ps1')
    $supportInstallerScriptNames = @('uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1')
    $diagnosticInstallerScriptNames = @('collect-logs.ps1', 'analyze-startup-evidence.ps1')
    foreach ($scriptName in @($installPreflightScriptNames + $supportInstallerScriptNames + $diagnosticInstallerScriptNames)) {
        Copy-DtmApiTextFileUtf8Bom -Source (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $installerTools $scriptName)
    }
    $playerDoctorRelease = Join-Path $repo 'dist\player-doctor\win-x64'
    & "$PSScriptRoot\check-player-doctor-release.ps1" -PackageRoot $playerDoctorRelease
    $playerDoctorPackage = Join-Path $installerTools 'player-doctor'
    New-Item -ItemType Directory -Force -Path $playerDoctorPackage | Out-Null
    foreach ($playerDoctorFile in @('dtmapi-player-doctor.exe', 'dotnet-LICENSE.txt', 'dotnet-ThirdPartyNotices.txt')) {
        Copy-Item -LiteralPath (Join-Path $playerDoctorRelease $playerDoctorFile) -Destination (Join-Path $playerDoctorPackage $playerDoctorFile) -Force
    }
    Test-DtmApiWindowsPowerShellSyntax -Paths @(
        $installPreflightScriptNames | ForEach-Object { Join-Path $installerTools $_ }
    )
    Test-DtmApiWindowsPowerShellSyntax -Paths @(
        @($supportInstallerScriptNames + $diagnosticInstallerScriptNames) | ForEach-Object { Join-Path $installerTools $_ }
    ) -WarningOnly

    $bepInExPackage = Join-Path $repo 'tools\release\bootstrap\BepInEx_win_x64_5.4.23.5.zip'
    if (-not (Test-Path -LiteralPath $bepInExPackage -PathType Leaf)) {
        throw "Missing bundled BepInEx package: $bepInExPackage"
    }
    $bepInExPackageDestination = Join-Path $runtimePackage 'Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $bepInExPackageDestination) | Out-Null
    Copy-Item -LiteralPath $bepInExPackage -Destination $bepInExPackageDestination -Force

    $runtimePayload = Join-Path $runtimePackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $outDir = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
    $runtimeFiles = @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Abstractions.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')
    $runtimeSourcePaths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($path in @(
        'Directory.Build.props',
        'src/DTMAPI.BepInExBootstrap',
        'src/DTMAPI.Abstractions',
        'src/DTMAPI.Core',
        'src/DTMAPI.GameBridge.DolocTown',
        'src/DTMAPI.ModConfigMenu'
    )) {
        $runtimeSourcePaths.Add($path) | Out-Null
    }
    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $runtimeInvariant = Get-DtmApiMapValue -Map $catalog -Key 'playerRuntimePackageInvariant' -Default $null
    foreach ($component in @((Get-DtmApiMapValue -Map $runtimeInvariant -Key 'optionalComponents' -Default @()))) {
        $sourceProject = [string](Get-DtmApiMapValue -Map $component -Key 'sourceProject' -Default '')
        if (-not [string]::IsNullOrWhiteSpace($sourceProject)) {
            $runtimeSourcePaths.Add((Split-Path -Parent $sourceProject).Replace('\', '/')) | Out-Null
        }
    }
    $runtimeSourceStatus = @(& git -C $repo status --porcelain --untracked-files=all -- @($runtimeSourcePaths.ToArray()))
    if ($LASTEXITCODE -ne 0) {
        throw 'Workshop Runtime package could not verify the mandatory Runtime source worktree.'
    }
    if ($runtimeSourceStatus.Count -ne 0) {
        throw "Workshop Runtime package requires committed mandatory Runtime source. Dirty paths:`n$([string]::Join([Environment]::NewLine, $runtimeSourceStatus))"
    }
    Copy-DirectoryContents -Source $outDir -Destination $runtimePayload -Include $runtimeFiles
    Copy-IfExists -Source (Join-Path $repo 'assets\branding\dtmapi-icon.png') -Destination (Join-Path $runtimePayload 'assets\branding\dtmapi-icon.png')

    $buildCommit = [string](Get-DtmApiSourceCommit -RepoRoot $repo)
    if ([string]::IsNullOrWhiteSpace($buildCommit) -or $buildCommit.Trim() -notmatch '^[0-9a-fA-F]{7,64}$') {
        throw "Workshop Runtime package requires a valid Git BuildCommit; actual='$buildCommit'."
    }
    $runtimeAssemblyReceipts = @(Get-DtmApiWorkshopRuntimeAssemblyReceipts -PayloadRoot $runtimePayload -FileNames $runtimeFiles)
    $optionalComponentReceipts = @(Get-DtmApiWorkshopOptionalComponentReceipts `
        -RepoRoot $repo `
        -Configuration $Configuration `
        -PayloadRoot (Join-Path $runtimePackage 'Content\DTMAPIInstaller\Payload'))
    $manifest = New-DtmApiReleaseManifest `
        -RepoRoot $repo `
        -PackageKind 'workshop-runtime' `
        -IncludedAssemblies $runtimeAssemblyReceipts `
        -OptionalComponents $optionalComponentReceipts `
        -BundledMods @() `
        -BuildCommit $buildCommit.Trim()
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'Content\DTMAPI\release-manifest.json') -Value $manifest
    $runtimeMetadata = Get-DtmApiPublishMetadata -RepoRoot $repo -UniqueId 'DTMAPI.Runtime'
    $runtimeDescription = [string]$runtimeMetadata.steamDescription
    if ([string]::IsNullOrWhiteSpace($runtimeDescription)) {
        $runtimeDescription = [string]$runtimeMetadata.gameDescription
    }
    $runtimeLocalizedDescription = [ordered]@{
        schinese = $runtimeDescription
        tchinese = $runtimeDescription
        english = $runtimeDescription
    }
    foreach ($localizedPropertyName in @('localizedDescription', 'localized_description')) {
        if ($runtimeMetadata.PSObject.Properties[$localizedPropertyName]) {
            $localizedSource = $runtimeMetadata.$localizedPropertyName
            foreach ($language in @('schinese', 'tchinese', 'english')) {
                if ($localizedSource.PSObject.Properties[$language]) {
                    $localizedValue = [string]$localizedSource.$language
                    if (-not [string]::IsNullOrWhiteSpace($localizedValue)) {
                        $runtimeLocalizedDescription[$language] = $localizedValue
                    }
                }
            }
        }
    }
    Write-Utf8NoBomJson -Path (Join-Path $runtimePackage 'info.json') -Value ([ordered]@{
        name = 'DTMAPI'
        author = 'Yuuka'
        version = $script:DtmApiReleaseVersion
        description = $runtimeDescription
        steamDescription = $runtimeDescription
        tags = @('Mod', 'Framework', 'DTMAPI', 'Chinese')
        localized_description = $runtimeLocalizedDescription
        # Doloc Town's native ModManager normalizes every local info.json to
        # include these keys before upload. Emit the stable form directly so
        # the generated package and the Steam-delivered player payload remain
        # byte-identical.
        localized_name = [ordered]@{
            schinese = ''
            tchinese = ''
            english = ''
        }
    })
    Remove-DtmApiWorkshopDownloadMarkers -PackageRoot $runtimePackage
}

if (-not $RuntimeOnly) {
    foreach ($mod in Get-DtmApiPublishedModDefinitions) {
        $package = Join-Path $OutputRoot $mod.PackageName
        Clear-Directory -Path $package
        $modSourceRoot = Resolve-DtmApiWorkshopModSourceRoot -Mod $mod
        $authorSdkProject = [bool](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkProject' -Default $false)
        if ($authorSdkProject) {
            $builderName = [string](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkBuildScript' -Default '')
            if ([string]::IsNullOrWhiteSpace($builderName)) {
                throw "Author SDK product $($mod.UniqueID) is missing its tracked build script."
            }
            $authorPackageName = [string](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkPackageFile' -Default '')
            if ([string]::IsNullOrWhiteSpace($authorPackageName) -or [IO.Path]::GetFileName($authorPackageName) -cne $authorPackageName) {
                throw "Author SDK product $($mod.UniqueID) is missing one safe tracked package filename."
            }
            $artifactKey = ([string]$mod.UniqueID).ToLowerInvariant() -replace '[^a-z0-9]+', '-'
            $authorOutput = Join-Path $repo ("temp\release-$artifactKey-author-sdk-package")
            $authorPackage = Join-Path $authorOutput $authorPackageName
            if (-not $SkipBuild) {
                $builderCatalogId = [string](Get-DtmApiMapValue -Map $mod -Key 'AuthorSdkCatalogId' -Default '')
                if ([string]::IsNullOrWhiteSpace($builderCatalogId)) {
                    throw "Author SDK product $($mod.UniqueID) is missing its Catalog-driven build id."
                }
                & (Join-Path $PSScriptRoot $builderName) -CatalogId $builderCatalogId -Configuration $Configuration -OutputRoot $authorOutput
                if ($LASTEXITCODE -ne 0) {
                    throw "Author SDK package build failed for $($mod.UniqueID)."
                }
            }
            if (-not (Test-Path -LiteralPath $authorPackage -PathType Leaf)) {
                throw "Author SDK package is unavailable for $($mod.UniqueID): $authorPackage"
            }
            Expand-Archive -LiteralPath $authorPackage -DestinationPath $package -Force
            $manifestPath = Join-Path $package 'Content\DTMAPI\manifest.json'
            $infoPath = Join-Path $package 'info.json'
            $entryPath = Join-Path $package ('Content\DTMAPI\' + $mod.PackageDll)
            if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
                -not (Test-Path -LiteralPath $infoPath -PathType Leaf) -or
                -not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
                throw "Author SDK package projection is incomplete for $($mod.UniqueID)."
            }
            $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
            $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
            Assert-DtmApiCurrentProductProjection `
                -RepoRoot $repo `
                -Definition $mod `
                -Manifest $manifest `
                -Info $info `
                -Context "Author SDK Workshop package $($mod.UniqueID)"
        }
        else {
        $source = Join-Path $modSourceRoot "bin\$Configuration\netstandard2.0"
        $manifestPath = Join-Path $source 'manifest.json'
        $dllPath = Join-Path $source $mod.SourceDll
        if (-not (Test-Path $manifestPath) -or -not (Test-Path $dllPath)) {
            throw "Built output missing for $($mod.Project). Expected $manifestPath and $dllPath."
        }

        $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
        $infoPath = Join-Path $modSourceRoot 'official-info.json'
        if (-not (Test-Path -LiteralPath $infoPath -PathType Leaf)) {
            throw "Missing official-info.json for published product $($mod.UniqueID): $infoPath"
        }
        $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
        Assert-DtmApiCurrentProductProjection `
            -RepoRoot $repo `
            -Definition $mod `
            -Manifest $manifest `
            -Info $info `
            -Context "Published Workshop package $($mod.UniqueID)"

        $contentRoot = Join-Path $package 'Content\DTMAPI'
        New-Item -ItemType Directory -Force -Path $contentRoot | Out-Null
        Copy-Item -LiteralPath $dllPath -Destination (Join-Path $contentRoot $mod.PackageDll) -Force
        $manifest.Author = 'Yuuka'
        $manifest.EntryDll = "Content/DTMAPI/$($mod.PackageDll)"
        Write-Utf8NoBomJson -Path (Join-Path $contentRoot 'manifest.json') -Value $manifest

        $sourceI18n = Join-Path $modSourceRoot 'i18n'
        if (Test-Path $sourceI18n) {
            Copy-DirectoryContents -Source (Split-Path -Parent $sourceI18n) -Destination $package -Include @('i18n')
        }
        $sourceContent = Join-Path $modSourceRoot 'Content'
        if (Test-Path $sourceContent) {
            Copy-DirectoryContents -Source (Split-Path -Parent $sourceContent) -Destination $package -Include @('Content')
        }

        $sourceAssets = Join-Path $source 'assets'
        if (Test-Path $sourceAssets) {
            Copy-DirectoryContents -Source (Split-Path -Parent $sourceAssets) -Destination $contentRoot -Include @('assets')
        }

        $info.author = 'Yuuka'
        Write-Utf8NoBomJson -Path (Join-Path $package 'info.json') -Value $info
        $assetIcon = Join-Path $repo 'assets\branding\dtmapi-icon.png'
        $modIcon = Join-Path $modSourceRoot 'icon.png'
        if (Test-Path -LiteralPath $modIcon -PathType Leaf) {
            $assetIcon = $modIcon
        }

        $assetPreview = Join-Path $repo 'assets\branding\dtmapi-preview.png'
        $modPreview = Join-Path $modSourceRoot 'preview.png'
        if (Test-Path -LiteralPath $modPreview -PathType Leaf) {
            $assetPreview = $modPreview
        }

        Copy-IfExists -Source $assetIcon -Destination (Join-Path $package 'icon.png')
        Copy-IfExists -Source $assetPreview -Destination (Join-Path $package 'preview.png')
        Write-Utf8NoBomJson -Path (Join-Path $contentRoot 'dtmapi-package.json') -Value ([ordered]@{
            owner = 'DTMAPI'
            packageKind = 'workshop-mod'
            uniqueId = $manifest.UniqueID
            generatedBy = 'tools/scripts/build-release-workshop-packages.ps1'
            updatedAt = (Get-Date).ToUniversalTime().ToString('o')
        })
        }
        Remove-DtmApiWorkshopDownloadMarkers -PackageRoot $package
    }
}

if (-not $ModsOnly) {
    Assert-PlayerPackageExcludesQaHost -PackageRoot (Join-Path $OutputRoot 'DTMAPI')
    & "$PSScriptRoot\check-product-catalog.ps1" -RuntimePackageRoot (Join-Path $OutputRoot 'DTMAPI')
    if (-not $?) { throw 'Built Runtime package failed the Catalog/player-payload gate.' }
}

Write-Host "Release Workshop staging packages written to $OutputRoot"
