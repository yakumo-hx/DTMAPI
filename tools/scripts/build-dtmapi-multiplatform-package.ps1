[CmdletBinding()]
param(
    [string] $AcceptedPackageRoot = '',
    [ValidateSet('ObservedPublished','Candidate')] [string] $SourceKind = 'ObservedPublished',
    [string] $HostBuildReceiptPath = '',
    [string] $WindowsHostPath = '',
    [string] $LinuxHostPath = '',
    [string] $OutputRoot = '',
    [string] $MetadataRoot = '',
    [string] $IconPath = '',
    [string] $PreviewPath = '',
    [string] $InstallerBuildCommit = '',
    [switch] $SkipFocusedAudit
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\multiplatform-package-common.ps1"

function Resolve-DtmApiMultiPlatformInputFile {
    param(
        [string] $ExplicitPath,
        [Parameter(Mandatory = $true)] [string] $DefaultPath,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $candidate = if ([string]::IsNullOrWhiteSpace($ExplicitPath)) { $DefaultPath } else { $ExplicitPath }
    $resolved = [System.IO.Path]::GetFullPath($candidate)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "$Label is missing: $resolved"
    }
    $item = Get-Item -LiteralPath $resolved -Force -ErrorAction Stop
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must not be a reparse point: $resolved"
    }
    return $resolved
}

function Assert-DtmApiMultiPlatformSafeOutputLeaf {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    $root = [System.IO.Path]::GetPathRoot($resolved).TrimEnd([char]92, [char]47)
    $leaf = [System.IO.Path]::GetFileName($resolved)
    if ([string]::IsNullOrWhiteSpace($leaf) -or
        $leaf -in @('.', '..') -or
        [string]::Equals($resolved, $root, [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($leaf, 'MODS', [System.StringComparison]::OrdinalIgnoreCase) -or
        $leaf.TrimEnd([char]32, [char]46) -cne $leaf -or
        $leaf.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
        throw "OutputRoot must be one safe package leaf and must never be the MODS root: $resolved"
    }

    $parent = [System.IO.Path]::GetFullPath((Split-Path -Parent $resolved)).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    $parentItem = Get-Item -LiteralPath $parent -Force -ErrorAction Stop
    if (($parentItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "OutputRoot parent must not be a reparse point: $parent"
    }
    if (Test-Path -LiteralPath $resolved) {
        if (-not (Test-Path -LiteralPath $resolved -PathType Container)) {
            throw "OutputRoot exists but is not a directory: $resolved"
        }
        $null = Assert-DtmApiMultiPlatformOrdinaryTree -Path $resolved -Context 'Existing multi-platform package output'
    }

    return [pscustomobject][ordered]@{
        Root = $resolved
        Parent = $parent
        Leaf = $leaf
    }
}

function Remove-DtmApiMultiPlatformTransient {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Parent,
        [Parameter(Mandatory = $true)] [string] $ExpectedLeafPrefix
    )

    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    if ((Split-Path -Parent $resolved) -cne $Parent -or
        -not [System.IO.Path]::GetFileName($resolved).StartsWith($ExpectedLeafPrefix, [System.StringComparison]::Ordinal)) {
        throw "Multi-platform package cleanup escaped its direct sibling boundary: $resolved"
    }
    if (Test-Path -LiteralPath $resolved) {
        $null = Assert-DtmApiMultiPlatformOrdinaryTree -Path $resolved -Context 'Multi-platform package transient cleanup'
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

$repo = Get-DtmApiMultiPlatformRepoRoot
if ($SourceKind -eq 'Candidate' -and ([string]::IsNullOrWhiteSpace($AcceptedPackageRoot) -or [string]::IsNullOrWhiteSpace($OutputRoot))) { throw 'Candidate requires explicit source and independent output roots.' }
if ($SourceKind -eq 'Candidate' -and (Test-Path -LiteralPath $OutputRoot)) { throw 'Candidate output must be a new independent directory.' }
if ([string]::IsNullOrWhiteSpace($AcceptedPackageRoot)) {
    $AcceptedPackageRoot = Get-DtmApiMultiPlatformDefaultAcceptedPackageRoot
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\DTMAPI-MultiPlatform'
}
if ([string]::IsNullOrWhiteSpace($MetadataRoot)) {
    $MetadataRoot = Join-Path $repo 'tools\release\dtmapi-multiplatform'
}
if ([string]::IsNullOrWhiteSpace($IconPath)) {
    $IconPath = Join-Path $repo 'assets\branding\dtmapi-multiplatform-icon.png'
}
if ([string]::IsNullOrWhiteSpace($PreviewPath)) {
    $PreviewPath = Join-Path $repo 'assets\branding\dtmapi-multiplatform-preview.png'
}

$AcceptedPackageRoot = [System.IO.Path]::GetFullPath($AcceptedPackageRoot)
$MetadataRoot = [System.IO.Path]::GetFullPath($MetadataRoot)
$accepted = Assert-DtmApiMultiPlatformAcceptedRuntimePackage -PackageRoot $AcceptedPackageRoot -SourceKind $SourceKind
$output = Assert-DtmApiMultiPlatformSafeOutputLeaf -Path $OutputRoot
$metadata = Assert-DtmApiMultiPlatformOrdinaryTree -Path $MetadataRoot -Context 'Multi-platform package metadata root'

$windowsDefault = Join-Path $repo 'artifacts\multiplatform-installer\win-x64\DTMAPI-MultiPlatform-Installer.exe'
$linuxDefault = Join-Path $repo 'artifacts\multiplatform-installer\linux-x64\DTMAPI-MultiPlatform-Installer'
$windowsHost = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $WindowsHostPath -DefaultPath $windowsDefault -Label 'win-x64 installer publish output'
$linuxHost = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $LinuxHostPath -DefaultPath $linuxDefault -Label 'linux-x64 installer publish output'
$icon = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $IconPath -DefaultPath $IconPath -Label 'DTMAPI icon asset'
$preview = Resolve-DtmApiMultiPlatformInputFile -ExplicitPath $PreviewPath -DefaultPath $PreviewPath -Label 'DTMAPI preview asset'
Assert-DtmApiMultiPlatformPeX64 -Path $windowsHost
Assert-DtmApiMultiPlatformElfX64 -Path $linuxHost

$metadataFile = Join-Path $metadata 'package-metadata.json'
$infoFile = Join-Path $metadata 'info.json'
$readmeFile = Join-Path $metadata 'README.txt'
$windowsBatNames = @(
    '1_install_dtmapi.bat',
    '2_uninstall_dtmapi.bat',
    '3_check_dtmapi_status.bat',
    '4_collect_dtmapi_logs.bat'
)
foreach ($required in @($metadataFile, $infoFile, $readmeFile) + @($windowsBatNames | ForEach-Object { Join-Path $metadata $_ })) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Multi-platform package metadata file is missing: $required"
    }
}
try {
    $packageMetadata = [System.IO.File]::ReadAllText($metadataFile, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $info = [System.IO.File]::ReadAllText($infoFile, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
}
catch {
    throw "Multi-platform package metadata JSON is invalid: $($_.Exception.Message)"
}
if ([int]$packageMetadata.schemaVersion -ne 1 -or
    -not [string]::Equals([string]$packageMetadata.distributionId, 'dtmapi-multiplatform', [System.StringComparison]::Ordinal) -or
    -not [string]::Equals([string]$packageMetadata.runtimeVersion, $script:DtmApiMultiPlatformRuntimeVersion, [System.StringComparison]::Ordinal) -or
    -not [string]::Equals([string]$packageMetadata.steamAppId, '2285550', [System.StringComparison]::Ordinal) -or
    -not [string]::Equals([string]$packageMetadata.workshopId, $script:DtmApiMultiPlatformWorkshopId, [System.StringComparison]::Ordinal) -or
    -not [string]::Equals([string]$packageMetadata.sourcePlayerPayloadTreeSha256, $script:DtmApiMultiPlatformPublishedPayloadTreeSha256, [System.StringComparison]::OrdinalIgnoreCase) -or
    -not [string]::Equals([string]$packageMetadata.windowsHostPackagePath, 'DTMAPI-MultiPlatform-Installer.exe', [System.StringComparison]::Ordinal) -or
    -not [string]::Equals([string]$packageMetadata.requiredLinuxLaunchOption, $script:DtmApiMultiPlatformRequiredLaunchOption, [System.StringComparison]::Ordinal) -or
    -not [string]::Equals([string]$packageMetadata.crossOverDllOverride, 'winhttp=n,b', [System.StringComparison]::Ordinal)) {
    throw "Multi-platform package metadata does not match the bounded 0.6.1 distribution contract: $metadataFile"
}
$expectedPackageName = 'DTMAPI-' + [char]0x591a + [char]0x5e73 + [char]0x53f0
if (-not [string]::Equals([string]$info.name, $expectedPackageName, [System.StringComparison]::Ordinal) -or
    -not [string]::Equals([string]$info.version, $script:DtmApiMultiPlatformRuntimeVersion, [System.StringComparison]::Ordinal)) {
    throw "Multi-platform info.json name/version is invalid: $infoFile"
}
if ([string]::IsNullOrWhiteSpace([string]$packageMetadata.requiredSteamDescriptionLead) -or
    -not ([string]$info.steamDescription).StartsWith([string]$packageMetadata.requiredSteamDescriptionLead, [System.StringComparison]::Ordinal) -or
    -not ([string]$info.localized_description.schinese).StartsWith([string]$packageMetadata.requiredSteamDescriptionLead, [System.StringComparison]::Ordinal)) {
    throw 'Multi-platform Steam description must put the required product overview at the first line.'
}
$requiredWorkshopContentPath = 'workshop/content/' + [string]$packageMetadata.steamAppId + '/' + [string]$packageMetadata.workshopId
$descriptionProjection = [string]$info.description + "`n" + [string]$info.steamDescription + "`n" + [System.IO.File]::ReadAllText($readmeFile, [System.Text.Encoding]::UTF8)
if ($descriptionProjection.IndexOf($script:DtmApiMultiPlatformRequiredLaunchOption, [System.StringComparison]::Ordinal) -lt 0 -or
    $descriptionProjection.IndexOf($requiredWorkshopContentPath, [System.StringComparison]::Ordinal) -lt 0 -or
    $descriptionProjection.IndexOf('<本项目 Workshop ID>', [System.StringComparison]::Ordinal) -ge 0 -or
    $descriptionProjection.IndexOf('<本項目 Workshop ID>', [System.StringComparison]::Ordinal) -ge 0 -or
    $descriptionProjection.IndexOf("<this item's Workshop ID>", [System.StringComparison]::Ordinal) -ge 0 -or
    $descriptionProjection.IndexOf('CrossOver', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
    $descriptionProjection.IndexOf('not code-signed', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
    $descriptionProjection.IndexOf('Doloc Town Steam Workshop', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Multi-platform player metadata must include the exact Linux launch option, CrossOver route, unsigned-tool notice, and official Workshop source.'
}
foreach ($locale in @('schinese', 'tchinese', 'english')) {
    $localized = [string]$info.localized_description.$locale
    foreach ($requiredText in @(
        $script:DtmApiMultiPlatformRequiredLaunchOption,
        $requiredWorkshopContentPath,
        'Shift+F4',
        'bash ./1_install_dtmapi.sh',
        'bash ./2_uninstall_dtmapi.sh',
        'bash ./3_check_dtmapi_status.sh',
        'bash ./4_collect_dtmapi_logs.sh',
        'DTMAPI-MultiPlatform-Installer.exe',
        'Native, then Builtin',
        'CrossOver')) {
        if ($localized.IndexOf($requiredText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Multi-platform localized_description.$locale is incomplete; missing: $requiredText"
        }
    }
    $localizedDisclosureMarkers = @($packageMetadata.localizedDisclosureMarkers.$locale)
    if ($localizedDisclosureMarkers.Count -ne 2) {
        throw "Multi-platform localized disclosure contract is invalid for: $locale"
    }
    foreach ($requiredText in $localizedDisclosureMarkers) {
        if ($localized.IndexOf($requiredText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Multi-platform localized_description.$locale disclosure is incomplete; missing: $requiredText"
        }
    }
}

if ($SourceKind -eq 'Candidate') {
    $hostBuild = Get-Content -LiteralPath $HostBuildReceiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-DtmApiMultiPlatformHostBuildReceipt -Receipt $hostBuild -WindowsHost $windowsHost -LinuxHost $linuxHost
    if ($InstallerBuildCommit -and $InstallerBuildCommit -cne $hostBuild.Commit) { throw 'Installer commit conflicts with actual build.' }
    $InstallerBuildCommit = $hostBuild.Commit
    $packageMetadata.installerVersion = $hostBuild.InstallerVersion
}

$preservedWorkshopControl = $null
if (Test-Path -LiteralPath $output.Root -PathType Container) {
    $workshopControlFiles = @(Get-ChildItem -LiteralPath $output.Root -Filter 'workshop.json' -File -Force -Recurse -ErrorAction Stop)
    if ($workshopControlFiles.Count -gt 1 -or
        ($workshopControlFiles.Count -eq 1 -and
            -not [string]::Equals($workshopControlFiles[0].FullName, (Join-Path $output.Root 'workshop.json'), [System.StringComparison]::OrdinalIgnoreCase))) {
        throw 'Existing multi-platform output may contain at most one root workshop.json upload control file.'
    }
    if ($workshopControlFiles.Count -eq 1) {
        $preservedWorkshopControl = Assert-DtmApiMultiPlatformWorkshopControlFile `
            -Path $workshopControlFiles[0].FullName `
            -ExpectedWorkshopId ([string]$packageMetadata.workshopId)
    }
}

if ([string]::IsNullOrWhiteSpace($InstallerBuildCommit)) {
    $InstallerBuildCommit = [string](& git -C $repo rev-parse --short=12 HEAD 2>$null | Select-Object -First 1)
}
if ($InstallerBuildCommit -notmatch '^[0-9a-fA-F]{7,64}$') {
    throw "InstallerBuildCommit must be a Git commit token: $InstallerBuildCommit"
}
$installerSourceStatus = @(& git -C $repo status --porcelain --untracked-files=all -- 'src/DTMAPI.MultiPlatformInstaller' 2>$null)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not inspect the multi-platform installer source worktree state.'
}
$installerSourceState = if ($installerSourceStatus.Count -eq 0) { 'clean' } else { 'dirty' }

$token = [Guid]::NewGuid().ToString('N')
$transientPrefix = '.' + $output.Leaf + '.dtmapi-multiplatform-'
$staging = Join-Path $output.Parent ($transientPrefix + 'staging-' + $token)
$backup = Join-Path $output.Parent ($transientPrefix + 'previous-' + $token)
if ((Test-Path -LiteralPath $staging) -or (Test-Path -LiteralPath $backup)) {
    throw 'Multi-platform package transient path unexpectedly exists.'
}
[System.IO.Directory]::CreateDirectory($staging) | Out-Null
$published = $false
$oldMoved = $false
try {
    foreach ($relative in $accepted.SharedPaths) {
        Copy-DtmApiMultiPlatformFile `
            -Source (Join-Path $accepted.Root $relative.Replace('/', '\')) `
            -Destination (Join-Path $staging $relative.Replace('/', '\'))
    }

    $windowsBatActionMap = [ordered]@{
        '1_install_dtmapi.bat' = 'install'
        '2_uninstall_dtmapi.bat' = 'uninstall'
        '3_check_dtmapi_status.bat' = 'status'
        '4_collect_dtmapi_logs.bat' = 'collect-logs'
    }
    foreach ($entry in $windowsBatActionMap.GetEnumerator()) {
        $batName = [string]$entry.Key
        $sourceBat = Join-Path $metadata $batName
        Assert-DtmApiMultiPlatformWindowsBatShim -Path $sourceBat -Action ([string]$entry.Value)
        Copy-DtmApiMultiPlatformFile `
            -Source $sourceBat `
            -Destination (Join-Path $staging $batName)
    }

    foreach ($shellName in @('1_install_dtmapi.sh', '2_uninstall_dtmapi.sh', '3_check_dtmapi_status.sh', '4_collect_dtmapi_logs.sh')) {
        $sourceShell = Join-Path $metadata $shellName
        Assert-DtmApiMultiPlatformLfNoBomShellFile -Path $sourceShell
        Copy-DtmApiMultiPlatformFile -Source $sourceShell -Destination (Join-Path $staging $shellName)
    }

    Copy-DtmApiMultiPlatformFile -Source $readmeFile -Destination (Join-Path $staging 'README_FIRST.txt')
    if ($SourceKind -eq 'Candidate') {
        [IO.File]::WriteAllText((Join-Path $staging 'info.json'), (Get-DtmApiMultiPlatformCandidateInfoText -MetadataRoot $metadata -RuntimeVersion $accepted.RuntimeVersion), (New-Object Text.UTF8Encoding($false)))
    } else { Copy-DtmApiMultiPlatformFile -Source $infoFile -Destination (Join-Path $staging 'info.json') }
    Copy-DtmApiMultiPlatformFile -Source $icon -Destination (Join-Path $staging 'icon.png')
    Copy-DtmApiMultiPlatformFile -Source $preview -Destination (Join-Path $staging 'preview.png')

    $windowsPackageRelative = ([string]$packageMetadata.windowsHostPackagePath).Replace('/', '\')
    $linuxPackageRelative = ([string]$packageMetadata.linuxHostPackagePath).Replace('/', '\')
    $hostManifestRelative = ([string]$packageMetadata.hostManifestPackagePath).Replace('/', '\')
    $runtimePackageManifestRelative = ([string]$packageMetadata.runtimePackageManifestPath).Replace('/', '\')
    foreach ($relative in @($windowsPackageRelative, $linuxPackageRelative, $hostManifestRelative, $runtimePackageManifestRelative)) {
        if ([string]::IsNullOrWhiteSpace($relative) -or
            [System.IO.Path]::IsPathRooted($relative) -or
            @($relative.Split('\')) -contains '..') {
            throw "Unsafe host package path in package metadata: $relative"
        }
    }
    Copy-DtmApiMultiPlatformFile -Source $windowsHost -Destination (Join-Path $staging $windowsPackageRelative)
    Copy-DtmApiMultiPlatformFile -Source $linuxHost -Destination (Join-Path $staging $linuxPackageRelative)

    $windowsHostPackagePath = Join-Path $staging $windowsPackageRelative
    $linuxHostPackagePath = Join-Path $staging $linuxPackageRelative
    $windowsItem = Get-Item -LiteralPath $windowsHostPackagePath -Force -ErrorAction Stop
    $linuxItem = Get-Item -LiteralPath $linuxHostPackagePath -Force -ErrorAction Stop
    $windowsHostSha256 = Get-DtmApiMultiPlatformFileSha256 -Path $windowsHostPackagePath
    $linuxHostSha256 = Get-DtmApiMultiPlatformFileSha256 -Path $linuxHostPackagePath
    $bepInExRelative = 'Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip'
    $bepInExPath = Join-Path $staging $bepInExRelative.Replace('/', '\')
    $bepInExItem = Get-Item -LiteralPath $bepInExPath -Force -ErrorAction Stop
    $bepInExSha256 = Get-DtmApiMultiPlatformFileSha256 -Path $bepInExPath

    $runtimePackageManifest = [ordered]@{
        SchemaVersion = 1
        DistributionId = [string]$packageMetadata.distributionId
        WorkshopId = [string]$packageMetadata.workshopId
        DisplayName = [string]$packageMetadata.displayName
        InstallerVersion = [string]$packageMetadata.installerVersion
        RuntimeVersion = [string]$accepted.RuntimeVersion
        SourceWorkshopId = [string]$packageMetadata.sourceRuntimeWorkshopId
        SourceWorkshopManifestId = [string]$packageMetadata.sourceRuntimeWorkshopManifestId
        SourcePlayerPayloadTreeSha256 = [string]$accepted.PlayerPayload.TreeSha256
        BepInExArchive = [ordered]@{
            Rid = ''
            RelativePath = $bepInExRelative
            Length = [long]$bepInExItem.Length
            Sha256 = $bepInExSha256
        }
        HostArtifacts = @(
            [ordered]@{
                Rid = 'win-x64'
                RelativePath = ([string]$packageMetadata.windowsHostPackagePath).Replace('\', '/')
                Length = [long]$windowsItem.Length
                Sha256 = $windowsHostSha256
            },
            [ordered]@{
                Rid = 'linux-x64'
                RelativePath = ([string]$packageMetadata.linuxHostPackagePath).Replace('\', '/')
                Length = [long]$linuxItem.Length
                Sha256 = $linuxHostSha256
            }
        )
        RequiredLinuxLaunchOption = $script:DtmApiMultiPlatformRequiredLaunchOption
        CrossOverDllOverride = [string]$packageMetadata.crossOverDllOverride
    }
    if ($SourceKind -eq 'Candidate') {
        $runtimePackageManifest.SchemaVersion = 2
        $runtimePackageManifest.InstallerBuildCommit = $hostBuild.Commit
        foreach ($key in @('SourceWorkshopId','SourceWorkshopManifestId','SourcePlayerPayloadTreeSha256')) { $runtimePackageManifest.Remove($key) }
        $runtimePackageManifest.RuntimeSource = $accepted.RuntimeSource
    }
    Write-DtmApiMultiPlatformJsonNoBom -Path (Join-Path $staging $runtimePackageManifestRelative) -Value $runtimePackageManifest

    $hostManifest = [ordered]@{
        SchemaVersion = 1
        DistributionId = [string]$packageMetadata.distributionId
        DistributionState = 'published-workshop-item-update-candidate'
        WorkshopId = [string]$packageMetadata.workshopId
        InstallerVersion = [string]$packageMetadata.installerVersion
        InstallerBuildCommit = $InstallerBuildCommit.ToLowerInvariant()
        InstallerSourceTreeState = $installerSourceState
        RuntimeVersion = [string]$accepted.RuntimeVersion
        RuntimeBuildCommit = [string]$accepted.BuildCommit
        ImportedPublishedPlayerPayload = [ordered]@{
            SourceWorkshopId = [string]$packageMetadata.sourceRuntimeWorkshopId
            Algorithm = [string]$accepted.PlayerPayload.Algorithm
            FileCount = [int]$accepted.PlayerPayload.FileCount
            Bytes = [long]$accepted.PlayerPayload.Bytes
            TreeSha256 = [string]$accepted.PlayerPayload.TreeSha256
        }
        SharedPayload = [ordered]@{
            Algorithm = [string]$accepted.SharedPayload.Algorithm
            FileCount = [int]$accepted.SharedPayload.FileCount
            Bytes = [long]$accepted.SharedPayload.Bytes
            TreeSha256 = [string]$accepted.SharedPayload.TreeSha256
        }
        Hosts = @(
            [ordered]@{
                Rid = 'win-x64'
                Format = 'PE32+'
                RelativePath = ([string]$packageMetadata.windowsHostPackagePath).Replace('\', '/')
                Length = [long]$windowsItem.Length
                Sha256 = $windowsHostSha256
            },
            [ordered]@{
                Rid = 'linux-x64'
                Format = 'ELF64'
                RelativePath = ([string]$packageMetadata.linuxHostPackagePath).Replace('\', '/')
                Length = [long]$linuxItem.Length
                Sha256 = $linuxHostSha256
            }
        )
        RequiredLinuxLaunchOption = $script:DtmApiMultiPlatformRequiredLaunchOption
        CodeSigning = 'unsigned-experimental'
    }
    if ($SourceKind -eq 'Candidate') {
        $hostManifest.SchemaVersion = 2
        $hostManifest.DistributionState = 'Candidate'
        $hostManifest.InstallerSourceTreeState = 'clean-committed-inputs'
        $hostManifest.InstallerBuildSource = $hostBuild
        $hostManifest.RuntimeSource = $accepted.RuntimeSource
        $hostManifest.ImportedRuntimePayload = $hostManifest.ImportedPublishedPlayerPayload
        $hostManifest.ImportedRuntimePayload.Remove('SourceWorkshopId')
        $hostManifest.Remove('ImportedPublishedPlayerPayload')
    }
    Write-DtmApiMultiPlatformJsonNoBom -Path (Join-Path $staging $hostManifestRelative) -Value $hostManifest

    if (@(Get-ChildItem -LiteralPath $staging -Filter 'workshop.json' -File -Force -Recurse).Count -ne 0) {
        throw 'Generated multi-platform content staging must start without workshop.json; only an already-bound, validated root upload control file may be preserved after content audit.'
    }
    $sharedStaged = Get-DtmApiMultiPlatformTreeReceipt -Root $staging -RelativePaths $accepted.SharedPaths
    if (-not [string]::Equals($sharedStaged.TreeSha256, $accepted.SharedPayload.TreeSha256, [System.StringComparison]::OrdinalIgnoreCase) -or
        $sharedStaged.FileCount -ne $accepted.SharedPayload.FileCount -or
        $sharedStaged.Bytes -ne $accepted.SharedPayload.Bytes) {
        throw 'Staged multi-platform package changed the selected exact shared payload.'
    }

    if (-not $SkipFocusedAudit) {
        $auditResult = & (Join-Path $PSScriptRoot 'test-dtmapi-multiplatform-package.ps1') `
            -PackageRoot $staging `
            -AcceptedPackageRoot $accepted.Root `
            -SourceKind $SourceKind `
            -SkipBashSyntax `
            -Quiet
        if ($null -eq $auditResult -or -not [bool]$auditResult.Passed) {
            throw 'Focused multi-platform package audit failed before publication.'
        }
    }

    if ($null -ne $preservedWorkshopControl) {
        Copy-DtmApiMultiPlatformFile `
            -Source $preservedWorkshopControl.Path `
            -Destination (Join-Path $staging 'workshop.json')
        $null = Assert-DtmApiMultiPlatformWorkshopControlFile `
            -Path (Join-Path $staging 'workshop.json') `
            -ExpectedWorkshopId ([string]$packageMetadata.workshopId)
    }

    if (Test-Path -LiteralPath $output.Root -PathType Container) {
        [System.IO.Directory]::Move($output.Root, $backup)
        $oldMoved = $true
    }
    try {
        [System.IO.Directory]::Move($staging, $output.Root)
    }
    catch {
        if ($oldMoved -and -not (Test-Path -LiteralPath $output.Root) -and (Test-Path -LiteralPath $backup -PathType Container)) {
            [System.IO.Directory]::Move($backup, $output.Root)
            $oldMoved = $false
        }
        throw
    }
    $published = $true
    if ($oldMoved) {
        Remove-DtmApiMultiPlatformTransient -Path $backup -Parent $output.Parent -ExpectedLeafPrefix $transientPrefix
        $oldMoved = $false
    }
}
finally {
    if (-not $published) {
        if ($oldMoved -and -not (Test-Path -LiteralPath $output.Root) -and (Test-Path -LiteralPath $backup -PathType Container)) {
            [System.IO.Directory]::Move($backup, $output.Root)
            $oldMoved = $false
        }
        if (Test-Path -LiteralPath $staging) {
            Remove-DtmApiMultiPlatformTransient -Path $staging -Parent $output.Parent -ExpectedLeafPrefix $transientPrefix
        }
        if ($oldMoved -and (Test-Path -LiteralPath $backup)) {
            throw "Publication failed after moving the previous package; preserving bounded backup for recovery: $backup"
        }
    }
}

$finalReceipt = Get-DtmApiMultiPlatformPublishedContentReceipt -Root $output.Root
$finalWorkshopControl = $null
if (Test-Path -LiteralPath (Join-Path $output.Root 'workshop.json') -PathType Leaf) {
    $finalWorkshopControl = Assert-DtmApiMultiPlatformWorkshopControlFile `
        -Path (Join-Path $output.Root 'workshop.json') `
        -ExpectedWorkshopId ([string]$packageMetadata.workshopId)
}
Write-Host ("DTMAPI multi-platform package atomically published: {0}" -f $output.Root)
Write-Host ("Imported Runtime: {0} files, {1} bytes, {2}" -f $accepted.PlayerPayload.FileCount, $accepted.PlayerPayload.Bytes, $accepted.PlayerPayload.TreeSha256)
Write-Host ("Shared byte-equal projection: {0} files, {1} bytes, {2}" -f $accepted.SharedPayload.FileCount, $accepted.SharedPayload.Bytes, $accepted.SharedPayload.TreeSha256)
Write-Host ("Final candidate: {0} files, {1} bytes, {2}" -f $finalReceipt.FileCount, $finalReceipt.Bytes, $finalReceipt.TreeSha256)
if ($null -ne $finalWorkshopControl) {
    Write-Host ("Preserved Workshop upload control: ID={0}; {1} bytes; {2}" -f $finalWorkshopControl.WorkshopId, $finalWorkshopControl.Bytes, $finalWorkshopControl.Sha256)
}

[pscustomobject][ordered]@{
    PackageRoot = $output.Root
    ImportedRuntimeRoot = $accepted.Root
    ImportedPlayerPayloadFileCount = [int]$accepted.PlayerPayload.FileCount
    ImportedPlayerPayloadBytes = [long]$accepted.PlayerPayload.Bytes
    ImportedPlayerPayloadTreeSha256 = [string]$accepted.PlayerPayload.TreeSha256
    SharedFileCount = [int]$accepted.SharedPayload.FileCount
    SharedBytes = [long]$accepted.SharedPayload.Bytes
    SharedTreeSha256 = [string]$accepted.SharedPayload.TreeSha256
    PackageFileCount = [int]$finalReceipt.FileCount
    PackageBytes = [long]$finalReceipt.Bytes
    PackageTreeSha256 = [string]$finalReceipt.TreeSha256
    WorkshopControlFilePreserved = ($null -ne $finalWorkshopControl)
    WorkshopControlFileBytes = if ($null -eq $finalWorkshopControl) { 0L } else { [long]$finalWorkshopControl.Bytes }
    WorkshopControlFileSha256 = if ($null -eq $finalWorkshopControl) { '' } else { [string]$finalWorkshopControl.Sha256 }
}
