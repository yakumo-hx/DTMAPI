param(
    [string] $Configuration = 'Release',
    [switch] $Quiet
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
$uninstallScript = Join-Path $PSScriptRoot 'uninstall-dtmapi.ps1'
$installScript = Join-Path $PSScriptRoot 'install-to-game.ps1'
$statusScript = Join-Path $PSScriptRoot 'check-dtmapi-status.ps1'

function Assert-OwnershipTest {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw "Player Runtime-only ownership test failed: $Message"
    }
}

function Write-TestText {
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

function Get-RelativeTestPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if ([string]::Equals($rootFull, $pathFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return '.'
    }

    $prefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
    Assert-OwnershipTest -Condition ($pathFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) -Message "Path escaped snapshot root: $pathFull"
    return $pathFull.Substring($prefix.Length).Replace('\', '/')
}

function Get-TestTreeFingerprint {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return '<missing>'
    }

    $rows = New-Object 'System.Collections.Generic.List[string]'
    foreach ($directory in @(Get-ChildItem -LiteralPath $Root -Directory -Recurse -Force | Sort-Object FullName)) {
        $rows.Add('D|' + (Get-RelativeTestPath -Root $Root -Path $directory.FullName)) | Out-Null
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force | Sort-Object FullName)) {
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $rows.Add(('F|{0}|{1}|{2}' -f (Get-RelativeTestPath -Root $Root -Path $file.FullName), $file.Length, $hash)) | Out-Null
    }

    return (@($rows.ToArray()) | Sort-Object) -join "`n"
}

function New-TestModInfos {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string[]] $OfficialFolders
    )

    $modInfos = New-Object PSObject
    foreach ($folder in $OfficialFolders) {
        $entry = [pscustomobject][ordered]@{
            id = 'Local.' + $folder
            enabled = $true
            priority = 7
            source = 'Local'
            title = 'sentinel-' + $folder
        }
        $modInfos | Add-Member -MemberType NoteProperty -Name ('Local.' + $folder) -Value $entry
    }
    $modInfos | Add-Member -MemberType NoteProperty -Name 'Workshop.9999999999' -Value ([pscustomobject][ordered]@{
        id = 'Workshop.9999999999'
        enabled = $true
        priority = 3
        source = 'Workshop'
        title = 'third-party-sentinel'
    })

    Write-Utf8NoBomJson -Path $Path -Value ([ordered]@{ modInfos = $modInfos })
}

function New-OwnershipFixture {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $gameDir = Join-Path $Root 'Doloc Town 测试 Game'
    $persistentRoot = Join-Path $Root '用户 数据 Persistent'
    $stateDir = Join-Path $gameDir 'DTMAPI'
    New-Item -ItemType Directory -Force -Path (Join-Path $gameDir 'DolocTown_Data') | Out-Null
    Write-TestText -Path (Join-Path $gameDir 'DolocTown.exe') -Text 'fake-executable'

    $runtimePlugin = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
    Write-TestText -Path (Join-Path $runtimePlugin 'DTMAPI.BepInExBootstrap.dll') -Text 'runtime-owned'
    Write-TestText -Path (Join-Path $gameDir 'BepInEx\plugins\Third Party\keep.dll') -Text 'third-party-bepinex'
    Write-TestText -Path (Join-Path $stateDir 'tools\uninstall-dtmapi.ps1') -Text 'installed-helper'
    Write-TestText -Path (Join-Path $stateDir 'release-manifest.json') -Text '{"PackageKind":"test"}'
    Write-TestText -Path (Join-Path $stateDir 'reports\keep-report.txt') -Text 'report-sentinel'
    Write-TestText -Path (Join-Path $stateDir 'config\keep-config.json') -Text '{"keep":true}'
    Write-TestText -Path (Join-Path $stateDir 'backups\existing\keep.txt') -Text 'backup-sentinel'

    $candidateRows = @(
        [ordered]@{ Folder = 'ThirdParty Author'; Marker = '{"owner":"Other.Author","packageKind":"content-pack","uniqueId":"Other.Author.Pack"}'; Receipt = ''; Unknown = '' },
        [ordered]@{ Folder = 'Copied DTMAPI Owner'; Marker = '{"owner":"DTMAPI","packageKind":"content-pack","uniqueId":"Copied.Owner.Pack"}'; Receipt = ''; Unknown = '' },
        [ordered]@{ Folder = 'Empty Marker'; Marker = ''; Receipt = ''; Unknown = '' },
        [ordered]@{ Folder = 'Invalid Marker'; Marker = '{this is not json'; Receipt = ''; Unknown = '' },
        [ordered]@{ Folder = 'Legacy Marker Only'; Marker = '{"owner":"DTMAPI","uniqueId":"Legacy.Marker.Pack"}'; Receipt = ''; Unknown = '' },
        [ordered]@{ Folder = 'Forged Receipt'; Marker = $null; Receipt = '{"SchemaVersion":1,"ReceiptKind":"DTMAPI.InstallReceipt","TransactionId":"forged-no-state"}'; Unknown = '' },
        [ordered]@{ Folder = 'Current Looking Receipt'; Marker = '{"owner":"DTMAPI","packageKind":"workshop-mod","uniqueId":"Current.Looking.Pack"}'; Receipt = '{"SchemaVersion":1,"ReceiptKind":"DTMAPI.InstallReceipt","TransactionId":"matching-looking","OfficialFolder":"Current Looking Receipt","UniqueID":"Current.Looking.Pack"}'; Unknown = '' },
        [ordered]@{ Folder = 'Unknown Added File'; Marker = '{"owner":"DTMAPI","packageKind":"workshop-mod","uniqueId":"Unknown.Added.Pack"}'; Receipt = '{"SchemaVersion":1,"ReceiptKind":"DTMAPI.InstallReceipt","TransactionId":"matching-looking","OfficialFolder":"Unknown Added File","UniqueID":"Unknown.Added.Pack"}'; Unknown = 'unknown-user-content' },
        [ordered]@{ Folder = 'No Marker Package'; Marker = $null; Receipt = ''; Unknown = '' }
    )

    $officialFolders = New-Object 'System.Collections.Generic.List[string]'
    foreach ($candidate in $candidateRows) {
        $folder = [string]$candidate.Folder
        $officialFolders.Add($folder) | Out-Null
        $contentRoot = Join-Path $persistentRoot ('MODS\' + $folder + '\Content\DTMAPI')
        Write-TestText -Path (Join-Path $contentRoot 'manifest.json') -Text ('{"Name":"' + $folder + '","UniqueID":"Fixture.' + ($folder.Replace(' ', '')) + '","Version":"1.0.0","Type":"ContentPack"}')
        Write-TestText -Path (Join-Path $contentRoot 'sentinel.bin') -Text ('sentinel-' + $folder)
        if ($null -ne $candidate.Marker) {
            Write-TestText -Path (Join-Path $contentRoot 'dtmapi-package.json') -Text ([string]$candidate.Marker)
        }
        if (-not [string]::IsNullOrWhiteSpace([string]$candidate.Receipt)) {
            Write-TestText -Path (Join-Path $contentRoot 'dtmapi-install-receipt.json') -Text ([string]$candidate.Receipt)
        }
        if (-not [string]::IsNullOrWhiteSpace([string]$candidate.Unknown)) {
            Write-TestText -Path (Join-Path $persistentRoot ('MODS\' + $folder + '\Content\user-added.txt')) -Text ([string]$candidate.Unknown)
        }
    }

    $modInfosPath = Join-Path $persistentRoot 'SAVE\mod_infos.json'
    New-TestModInfos -Path $modInfosPath -OfficialFolders $officialFolders.ToArray()
    Write-Utf8NoBomJson -Path (Join-Path $stateDir 'install-state.json') -Value ([ordered]@{
        SchemaVersion = 1
        BepInExInstalledByDTMAPI = $false
        BundledMods = @(
            [ordered]@{
                OfficialFolder = 'Current Looking Receipt'
                UniqueID = 'Current.Looking.Pack'
                Path = (Join-Path $persistentRoot 'MODS\Current Looking Receipt')
                TransactionId = 'matching-looking'
            }
        )
    })

    return [pscustomobject]@{
        Root = $Root
        GameDir = $gameDir
        PersistentRoot = $persistentRoot
        StateDir = $stateDir
        ModsRoot = (Join-Path $persistentRoot 'MODS')
        ModInfosPath = $modInfosPath
        RuntimePlugin = $runtimePlugin
        ThirdPartyPlugin = (Join-Path $gameDir 'BepInEx\plugins\Third Party\keep.dll')
    }
}

function Get-TestPowerShellHost {
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

function Invoke-PlayerUninstall {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [switch] $DryRun,
        [switch] $LegacyOfficialLocalRequest
    )

    $oldGameDir = $env:DTMAPI_GAME_DIR
    $oldPersistentRoot = $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    $oldRuntimeDir = $env:DTMAPI_RUNTIME_DIR
    try {
        $env:DTMAPI_GAME_DIR = $Fixture.GameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $Fixture.PersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $Fixture.StateDir
        if ($DryRun -and $LegacyOfficialLocalRequest) {
            return @(& $uninstallScript -GameDir $Fixture.GameDir -DryRun -RemoveOfficialLocalPackages 3>&1 2>&1 | ForEach-Object { [string]$_ })
        }
        if ($DryRun) {
            return @(& $uninstallScript -GameDir $Fixture.GameDir -DryRun 3>&1 2>&1 | ForEach-Object { [string]$_ })
        }
        if ($LegacyOfficialLocalRequest) {
            return @(& $uninstallScript -GameDir $Fixture.GameDir -RemoveOfficialLocalPackages 3>&1 2>&1 | ForEach-Object { [string]$_ })
        }

        return @(& $uninstallScript -GameDir $Fixture.GameDir 3>&1 2>&1 | ForEach-Object { [string]$_ })
    }
    finally {
        $env:DTMAPI_GAME_DIR = $oldGameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $oldPersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $oldRuntimeDir
    }
}

function Invoke-TestDeveloperInstall {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $PersistentRoot
    )

    $oldGameDir = $env:DTMAPI_GAME_DIR
    $oldPersistentRoot = $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    $oldRuntimeDir = $env:DTMAPI_RUNTIME_DIR
    try {
        $env:DTMAPI_GAME_DIR = $GameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $PersistentRoot
        $env:DTMAPI_RUNTIME_DIR = Join-Path $GameDir 'DTMAPI'
        return @(& $installScript -Configuration $Configuration -SkipBuild -InstallPublishedModsOnly -LegacyOfficialLocalOnly 3>&1 2>&1 | ForEach-Object { [string]$_ })
    }
    finally {
        $env:DTMAPI_GAME_DIR = $oldGameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $oldPersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $oldRuntimeDir
    }
}

function Assert-PersistentBoundaryUnchanged {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $BeforeFingerprint,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $afterFingerprint = Get-TestTreeFingerprint -Root $Fixture.PersistentRoot
    Assert-OwnershipTest -Condition ([string]::Equals($BeforeFingerprint, $afterFingerprint, [System.StringComparison]::Ordinal)) -Message "$Label changed MODS or mod_infos bytes."
}

$tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$tempRoot = [System.IO.Path]::GetFullPath((Join-Path $tempBase ('DTMAPI player ownership 中文 ' + [Guid]::NewGuid().ToString('N'))))
Assert-OwnershipTest -Condition ($tempRoot.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase)) -Message "Temporary root escaped system temp: $tempRoot"
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

try {
    $uninstallText = [System.IO.File]::ReadAllText($uninstallScript, [System.Text.Encoding]::UTF8)
    Assert-OwnershipTest -Condition ($uninstallText -notmatch 'Get-DtmApiOwnedOfficialLocalPackages|Get-DtmApiLegacyOfficialLocalMetadata') -Message 'Player uninstaller still calls an official-local marker scanner.'
    Assert-OwnershipTest -Condition ($uninstallText -notmatch 'dtmapi-package\.json') -Message 'Player uninstaller still reads package-local marker metadata.'
    Assert-OwnershipTest -Condition ($uninstallText -notmatch 'function\s+Remove-OfficialLocalPackages|function\s+Remove-ModInfosEntries|Backup-ModInfosForUninstall') -Message 'Player uninstaller still contains official-local mutation functions.'

    $installText = [System.IO.File]::ReadAllText($installScript, [System.Text.Encoding]::UTF8)
    Assert-OwnershipTest -Condition ($installText -notmatch 'destination exists but is not marked as a DTMAPI-owned package') -Message 'Developer installer still describes marker metadata as overwrite ownership.'
    Assert-OwnershipTest -Condition ($installText -match 'Legacy package metadata is not an installer receipt') -Message 'Developer installer does not explain the fail-closed existing-destination rule.'
    Assert-OwnershipTest -Condition ($installText.Contains('[System.IO.Directory]::Move($stagingPath, $dest)')) -Message 'Developer installer does not publish a prepared package with collision-failing Directory.Move.'
    Assert-OwnershipTest -Condition ($installText.Contains('$stagingPath = Join-Path $persistentRoot')) -Message 'Developer installer staging is not outside the native MODS scan root.'
    Assert-OwnershipTest -Condition (-not $installText.Contains('New-Item -ItemType Directory -Force -Path $dest')) -Message 'Developer installer can still merge into a destination created after the initial check.'
    Assert-OwnershipTest -Condition (-not $installText.Contains('$officialContentRoot')) -Message 'Developer installer still contains the old destination cleanup path.'
    Assert-OwnershipTest -Condition ($installText -match 'if \(\$InstallPublishedModsOnly\)\s*\{\s*\$definitions = @\(Get-DtmApiPublishedModDefinitions\)') -Message 'Published/default product selection no longer starts from the complete published inventory.'
    Assert-OwnershipTest -Condition ($installText -match 'if \(\$LegacyOfficialLocalOnly\)\s*\{[\s\S]*AuthorSdkProject') -Message 'Explicit legacy official-local scope does not exclude managed Author SDK products at the selection boundary.'
    Assert-OwnershipTest -Condition ($installText -match 'LegacyOfficialLocalOnly is an explicit generic directory-publisher scope and requires') -Message 'Legacy official-local selection can be activated without an explicit install mode.'
    $pausedReleasePattern = 'New managed product installation is paused for DTMAPI ' + [regex]::Escape($script:DtmApiReleaseVersion)
    Assert-OwnershipTest -Condition ($installText -match $pausedReleasePattern) -Message 'Managed Advanced deployment no longer fails closed at the paused Author SDK boundary.'
    Assert-OwnershipTest -Condition ($installText -match 'Existing SDK deployment-status, install-local-status, recover and withdraw commands remain available') -Message 'Paused managed deployment does not preserve explicit old-journal recovery guidance.'
    Assert-OwnershipTest -Condition (-not $installText.Contains('& $authorSdkExe install-local $authorPackage')) -Message 'Runtime installer still invokes the retired Author SDK install-local path.'
    Assert-OwnershipTest -Condition ($installText -match 'if \(\$KeepLegacyMigratedGameMods\) \{[\s\S]*Keeping the complete existing game Mods tree[\s\S]*else \{[\s\S]*Backup-StaleSampleMods[\s\S]*Backup-StaleHookProbe') -Message 'KeepLegacyMigratedGameMods does not cover later stale sample/HookProbe housekeeping.'

    $statusText = [System.IO.File]::ReadAllText($statusScript, [System.Text.Encoding]::UTF8)
    Assert-OwnershipTest -Condition ($statusText -notmatch 'DTMAPI-owned official local packages|add -RemoveOfficialLocalPackages') -Message 'Status output still advertises marker ownership or player package cleanup.'
    Assert-OwnershipTest -Condition ($statusText -match 'Legacy/non-destructive DTMAPI package metadata markers') -Message 'Status output does not classify legacy markers as non-destructive.'

    Test-DtmApiWindowsPowerShellSyntax -Paths @($uninstallScript, $installScript, $statusScript, (Join-Path $PSScriptRoot 'release-common.ps1'), $PSCommandPath) -AllowCoreFallback

    $dryAndRealFixture = New-OwnershipFixture -Root (Join-Path $tempRoot '01 dry and real')
    $entireFixtureBeforeDryRun = Get-TestTreeFingerprint -Root $dryAndRealFixture.Root
    $null = Invoke-PlayerUninstall -Fixture $dryAndRealFixture -DryRun
    $entireFixtureAfterDryRun = Get-TestTreeFingerprint -Root $dryAndRealFixture.Root
    Assert-OwnershipTest -Condition ([string]::Equals($entireFixtureBeforeDryRun, $entireFixtureAfterDryRun, [System.StringComparison]::Ordinal)) -Message 'Dry-run changed the temporary game or persistent tree.'

    $persistentBeforeReal = Get-TestTreeFingerprint -Root $dryAndRealFixture.PersistentRoot
    $null = Invoke-PlayerUninstall -Fixture $dryAndRealFixture
    Assert-PersistentBoundaryUnchanged -Fixture $dryAndRealFixture -BeforeFingerprint $persistentBeforeReal -Label 'Real player uninstall'
    Assert-OwnershipTest -Condition (-not (Test-Path -LiteralPath $dryAndRealFixture.RuntimePlugin)) -Message 'Real uninstall did not remove the DTMAPI Runtime plugin directory.'
    Assert-OwnershipTest -Condition (Test-Path -LiteralPath $dryAndRealFixture.ThirdPartyPlugin -PathType Leaf) -Message 'Real uninstall changed a third-party BepInEx plugin.'
    Assert-OwnershipTest -Condition (Test-Path -LiteralPath (Join-Path $dryAndRealFixture.StateDir 'reports\keep-report.txt') -PathType Leaf) -Message 'Real uninstall removed retained reports.'
    Assert-OwnershipTest -Condition (Test-Path -LiteralPath (Join-Path $dryAndRealFixture.StateDir 'config\keep-config.json') -PathType Leaf) -Message 'Real uninstall removed retained config.'
    Assert-OwnershipTest -Condition (Test-Path -LiteralPath (Join-Path $dryAndRealFixture.StateDir 'backups\existing\keep.txt') -PathType Leaf) -Message 'Real uninstall removed existing backups.'

    $uninstallStateFiles = @(Get-ChildItem -LiteralPath $dryAndRealFixture.StateDir -Filter 'uninstall-state-*.json' -File)
    Assert-OwnershipTest -Condition ($uninstallStateFiles.Count -eq 1) -Message "Expected one uninstall state, found $($uninstallStateFiles.Count)."
    $uninstallState = Get-Content -Raw -Encoding UTF8 -LiteralPath $uninstallStateFiles[0].FullName | ConvertFrom-Json
    Assert-OwnershipTest -Condition ([string]$uninstallState.PlayerUninstallPolicy -eq 'RuntimeOnly') -Message 'Uninstall state does not record RuntimeOnly policy.'
    Assert-OwnershipTest -Condition (-not [bool]$uninstallState.OfficialLocalPackageScanPerformed) -Message 'Uninstall state claims an official-local scan occurred.'
    Assert-OwnershipTest -Condition (-not [bool]$uninstallState.OfficialLocalPackageRemovalPerformed) -Message 'Uninstall state claims official-local removal occurred.'
    Assert-OwnershipTest -Condition (@($uninstallState.OfficialLocalPackagesDetected).Count -eq 0) -Message 'Uninstall state reports official-local candidates.'
    Assert-OwnershipTest -Condition (@($uninstallState.OfficialLocalPackagesRemoved).Count -eq 0) -Message 'Uninstall state reports official-local removals.'
    Assert-OwnershipTest -Condition (@($uninstallState.ModInfosEntriesRemoved).Count -eq 0) -Message 'Uninstall state reports enablement removals.'

    $oldGameDir = $env:DTMAPI_GAME_DIR
    $oldPersistentRoot = $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    $oldRuntimeDir = $env:DTMAPI_RUNTIME_DIR
    try {
        $env:DTMAPI_GAME_DIR = $dryAndRealFixture.GameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $dryAndRealFixture.PersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $dryAndRealFixture.StateDir
        $statusHost = Get-TestPowerShellHost
        $statusOutput = @(& $statusHost -NoProfile -ExecutionPolicy Bypass -File $statusScript -GameDir $dryAndRealFixture.GameDir 2>&1 | ForEach-Object { [string]$_ })
        $statusExit = $LASTEXITCODE
    }
    finally {
        $env:DTMAPI_GAME_DIR = $oldGameDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $oldPersistentRoot
        $env:DTMAPI_RUNTIME_DIR = $oldRuntimeDir
    }
    Assert-OwnershipTest -Condition ($statusExit -ne 0) -Message 'Post-uninstall status incorrectly reported full success.'
    Assert-OwnershipTest -Condition (($statusOutput -join "`n") -match 'Legacy/non-destructive DTMAPI package metadata markers') -Message 'Post-uninstall status omitted non-destructive marker classification.'
    Assert-OwnershipTest -Condition (($statusOutput -join "`n") -match 'player uninstaller removes DTMAPI Runtime only') -Message 'Post-uninstall status omitted the Runtime-only boundary.'

    $legacySwitchFixture = New-OwnershipFixture -Root (Join-Path $tempRoot '02 legacy switch')
    $persistentBeforeLegacySwitch = Get-TestTreeFingerprint -Root $legacySwitchFixture.PersistentRoot
    $legacyOutput = Invoke-PlayerUninstall -Fixture $legacySwitchFixture -LegacyOfficialLocalRequest
    Assert-PersistentBoundaryUnchanged -Fixture $legacySwitchFixture -BeforeFingerprint $persistentBeforeLegacySwitch -Label 'Legacy cleanup-switch uninstall'
    Assert-OwnershipTest -Condition (($legacyOutput -join "`n") -match 'retired and ignored') -Message 'Legacy cleanup switch did not emit a clear refusal warning.'
    Assert-OwnershipTest -Condition (-not (Test-Path -LiteralPath $legacySwitchFixture.RuntimePlugin)) -Message 'Legacy cleanup-switch request prevented Runtime plugin removal.'
    Assert-OwnershipTest -Condition (Test-Path -LiteralPath $legacySwitchFixture.ThirdPartyPlugin -PathType Leaf) -Message 'Legacy cleanup-switch request changed a third-party BepInEx plugin.'
    $legacyStateFile = @(Get-ChildItem -LiteralPath $legacySwitchFixture.StateDir -Filter 'uninstall-state-*.json' -File | Select-Object -First 1)
    Assert-OwnershipTest -Condition ($legacyStateFile.Count -eq 1) -Message 'Legacy cleanup-switch uninstall did not write state.'
    $legacyState = Get-Content -Raw -Encoding UTF8 -LiteralPath $legacyStateFile[0].FullName | ConvertFrom-Json
    Assert-OwnershipTest -Condition ([bool]$legacyState.RemoveOfficialLocalPackagesRequested) -Message 'Legacy cleanup request was not recorded.'
    Assert-OwnershipTest -Condition (-not [bool]$legacyState.OfficialLocalPackageScanPerformed) -Message 'Legacy cleanup request triggered a package scan.'
    Assert-OwnershipTest -Condition (-not [bool]$legacyState.OfficialLocalPackageRemovalPerformed) -Message 'Legacy cleanup request triggered package removal.'

    $legacyMetadataRows = @(Get-DtmApiLegacyOfficialLocalMetadata -PersistentRoot $legacySwitchFixture.PersistentRoot)
    Assert-OwnershipTest -Condition ($legacyMetadataRows.Count -eq 7) -Message "Expected seven legacy marker metadata rows, found $($legacyMetadataRows.Count)."
    foreach ($row in $legacyMetadataRows) {
        Assert-OwnershipTest -Condition ([string]$row.Authority -eq 'None') -Message 'Legacy metadata row has destructive authority.'
        Assert-OwnershipTest -Condition ([string]$row.Classification -eq 'LegacyMetadataOnly') -Message 'Legacy metadata row classification drifted.'
        Assert-OwnershipTest -Condition (-not [bool]$row.DestructiveActionAllowed) -Message 'Legacy metadata row allows a destructive action.'
        Assert-OwnershipTest -Condition ($null -eq $row.PSObject.Properties['ModInfoId']) -Message 'Legacy metadata row still exports the old enablement-removal id.'
    }

    $installFixtureRoot = Join-Path $tempRoot '03 existing destination install'
    $installGameDir = Join-Path $installFixtureRoot 'Doloc Town 安装测试'
    $installPersistentRoot = Join-Path $installFixtureRoot '用户 持久目录'
    New-Item -ItemType Directory -Force -Path (Join-Path $installGameDir 'DolocTown_Data') | Out-Null
    Write-TestText -Path (Join-Path $installGameDir 'DolocTown.exe') -Text 'fake-executable'
    $allPublishedDefinitions = @(Get-DtmApiPublishedModDefinitions)
    $allDeveloperDefinitions = @(Get-DtmApiDeveloperOfficialModDefinitions)
    $managedAuthorSdkDefinitions = @($allDeveloperDefinitions | Where-Object {
        (Test-DtmApiMapKey -Map $_ -Key 'AuthorSdkProject') -and [bool](Get-DtmApiMapValue -Map $_ -Key 'AuthorSdkProject' -Default $false)
    })
    $publishedDefinitions = @($allPublishedDefinitions | Where-Object {
        -not ((Test-DtmApiMapKey -Map $_ -Key 'AuthorSdkProject') -and [bool](Get-DtmApiMapValue -Map $_ -Key 'AuthorSdkProject' -Default $false))
    })
    $managedAuthorSdkIds = @($managedAuthorSdkDefinitions | ForEach-Object { [string]$_.UniqueID } | Sort-Object)
    $productCatalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $catalogAdvancedIds = @($productCatalog.products | Where-Object {
        $null -ne $_.PSObject.Properties['codeModKind'] -and
        [string]$_.codeModKind -eq 'Advanced'
    } | ForEach-Object { [string]$_.uniqueId } | Sort-Object)
    Assert-OwnershipTest -Condition ($catalogAdvancedIds.Count -gt 0) -Message 'The Catalog contains no admitted Advanced products.'
    Assert-OwnershipTest -Condition ([string]::Join('|', $managedAuthorSdkIds) -eq [string]::Join('|', $catalogAdvancedIds)) -Message "Managed Author SDK identities diverged from the Catalog Advanced set. definitions=$([string]::Join('|', $managedAuthorSdkIds)); catalog=$([string]::Join('|', $catalogAdvancedIds))."
    Assert-OwnershipTest -Condition ($publishedDefinitions.Count -gt 0 -and $publishedDefinitions.Count -lt $allPublishedDefinitions.Count) -Message 'Generic official-local inventory did not exclude the managed Author SDK product boundary.'
    foreach ($managedAuthorSdkId in $catalogAdvancedIds) {
        Assert-OwnershipTest -Condition (@($publishedDefinitions | Where-Object { [string]$_.UniqueID -eq $managedAuthorSdkId }).Count -eq 0) -Message "Generic official-local ownership fixture still treats managed Advanced product $managedAuthorSdkId as a legacy directory package."
    }
    $installFolders = New-Object 'System.Collections.Generic.List[string]'
    foreach ($definition in $publishedDefinitions) {
        $folder = [string]$definition.OfficialFolder
        $installFolders.Add($folder) | Out-Null
        $existingRoot = Join-Path $installPersistentRoot ('MODS\' + $folder)
        Write-TestText -Path (Join-Path $existingRoot 'Content\DTMAPI\dtmapi-package.json') -Text ('{"owner":"DTMAPI","uniqueId":"' + [string]$definition.UniqueID + '"}')
        Write-TestText -Path (Join-Path $existingRoot 'Content\unknown-user-file.json') -Text ('{"sentinel":"' + $folder + '"}')
        Write-TestText -Path (Join-Path $existingRoot 'root-sentinel.txt') -Text ('keep-' + $folder)
    }
    New-TestModInfos -Path (Join-Path $installPersistentRoot 'SAVE\mod_infos.json') -OfficialFolders $installFolders.ToArray()
    $installPersistentBefore = Get-TestTreeFingerprint -Root $installPersistentRoot

    $installOutput = Invoke-TestDeveloperInstall -GameDir $installGameDir -PersistentRoot $installPersistentRoot
    $installPersistentAfter = Get-TestTreeFingerprint -Root $installPersistentRoot
    Assert-OwnershipTest -Condition ([string]::Equals($installPersistentBefore, $installPersistentAfter, [System.StringComparison]::Ordinal)) -Message 'Developer installer changed an existing official-local directory or enablement file.'
    $overwriteRefusals = @(($installOutput -join "`n") -split "`n" | Where-Object { $_ -match 'never overwrites the directory' })
    Assert-OwnershipTest -Condition ($overwriteRefusals.Count -eq $publishedDefinitions.Count) -Message "Expected $($publishedDefinitions.Count) existing-destination refusals, found $($overwriteRefusals.Count)."

    $freshInstallRoot = Join-Path $tempRoot '04 fresh destination install'
    $freshGameDir = Join-Path $freshInstallRoot 'Doloc Town 新安装测试'
    $freshPersistentRoot = Join-Path $freshInstallRoot '全新 用户持久目录'
    New-Item -ItemType Directory -Force -Path (Join-Path $freshGameDir 'DolocTown_Data') | Out-Null
    Write-TestText -Path (Join-Path $freshGameDir 'DolocTown.exe') -Text 'fake-executable'
    $freshInstallOutput = Invoke-TestDeveloperInstall -GameDir $freshGameDir -PersistentRoot $freshPersistentRoot
    Assert-OwnershipTest -Condition (($freshInstallOutput -join "`n") -notmatch 'official-local destination appeared') -Message 'Fresh developer install reported an unexpected destination collision.'
    foreach ($definition in $publishedDefinitions) {
        $freshDest = Join-Path $freshPersistentRoot ('MODS\' + [string]$definition.OfficialFolder)
        Assert-OwnershipTest -Condition (Test-Path -LiteralPath (Join-Path $freshDest 'info.json') -PathType Leaf) -Message "Fresh install did not publish info.json for $($definition.OfficialFolder)."
        Assert-OwnershipTest -Condition (Test-Path -LiteralPath (Join-Path $freshDest ('Content\DTMAPI\' + [string]$definition.PackageDll)) -PathType Leaf) -Message "Fresh install did not publish the DLL for $($definition.OfficialFolder)."
        Assert-OwnershipTest -Condition (Test-Path -LiteralPath (Join-Path $freshDest 'Content\DTMAPI\manifest.json') -PathType Leaf) -Message "Fresh install did not publish manifest.json for $($definition.OfficialFolder)."
    }
    $stagingRemainders = @(Get-ChildItem -LiteralPath $freshPersistentRoot -Directory -Filter '.dtmapi-staging-*' -ErrorAction SilentlyContinue)
    Assert-OwnershipTest -Condition ($stagingRemainders.Count -eq 0) -Message 'Fresh developer install left a staging directory behind.'
    $freshModInfos = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $freshPersistentRoot 'SAVE\mod_infos.json') | ConvertFrom-Json
    foreach ($definition in $publishedDefinitions) {
        $enablementId = 'Local.' + [string]$definition.OfficialFolder
        Assert-OwnershipTest -Condition ($null -ne $freshModInfos.modInfos.PSObject.Properties[$enablementId]) -Message "Fresh install did not add enablement for $($definition.OfficialFolder)."
    }

    if (-not $Quiet) {
        Write-Host "Player Runtime-only uninstall ownership tests: OK (candidates=9, legacy-markers=7, existing-destinations=$($publishedDefinitions.Count), fresh-destinations=$($publishedDefinitions.Count))"
    }
}
finally {
    $resolvedTempRoot = [System.IO.Path]::GetFullPath($tempRoot)
    if ($resolvedTempRoot.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedTempRoot) -like 'DTMAPI player ownership 中文 *' -and
        (Test-Path -LiteralPath $resolvedTempRoot)) {
        Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
    }
}
