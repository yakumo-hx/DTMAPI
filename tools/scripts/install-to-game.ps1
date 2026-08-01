param(
    [string] $Configuration = 'Release',
    [switch] $IncludeTestMods,
    [switch] $IncludeDebugConsoleMod,
    [switch] $IncludeHookProbe,
    [switch] $SkipBuild,
    [switch] $InstallBepInEx,
    [switch] $SkipOfficialLocalMods,
    [switch] $KeepLegacyMigratedGameMods,
    [switch] $DryRun,
    [switch] $InstallPublishedModsOnly,
    [switch] $InstallAllDevOfficialMods,
    [switch] $LegacyOfficialLocalOnly,
    [switch] $InstallQaFixtures,
    [string] $PackagePayloadRoot = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'
$script:DtmInstallFilesInstalled = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallBackupsCreated = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallLegacyModsMoved = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallBundledMods = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallQaFixturesInstalled = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallOfficialLocalAttempts = New-Object 'System.Collections.Generic.List[object]'
$script:DtmInstallStamp = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$script:DtmInstallModInfosBackupPath = ''
$script:DtmRuntimeInstallTransaction = $null
$script:DtmInstallGamePathResolved = $false
$script:DtmInstallSourceCommit = ''
$repo = Get-RepoRoot
$script:DtmInstallSourceCommit = Get-DtmApiSourceCommit -RepoRoot $repo
$detectedPackagePayloadRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Payload'))
if ([string]::IsNullOrWhiteSpace($PackagePayloadRoot) -and (Test-Path $detectedPackagePayloadRoot)) {
    $PackagePayloadRoot = $detectedPackagePayloadRoot
}
$usingPackagePayload = -not [string]::IsNullOrWhiteSpace($PackagePayloadRoot)
$installerToolSourceRoot = if ($usingPackagePayload) {
    [System.IO.Path]::GetFullPath((Join-Path $PackagePayloadRoot '..\tools'))
}
else {
    [System.IO.Path]::GetFullPath($PSScriptRoot)
}
if ($usingPackagePayload) {
    $SkipBuild = $true
    $SkipOfficialLocalMods = $true
}
if ($DryRun) {
    $SkipBuild = $true
}
if ($InstallPublishedModsOnly -and $InstallAllDevOfficialMods) {
    throw "Use only one of -InstallPublishedModsOnly or -InstallAllDevOfficialMods."
}
if ($InstallPublishedModsOnly -and $InstallQaFixtures) {
    throw "QA fixtures are developer-only. Do not combine -InstallPublishedModsOnly with -InstallQaFixtures."
}
if ($LegacyOfficialLocalOnly -and -not ($InstallPublishedModsOnly -or $InstallAllDevOfficialMods)) {
    throw "-LegacyOfficialLocalOnly is an explicit generic directory-publisher scope and requires -InstallPublishedModsOnly or -InstallAllDevOfficialMods."
}
$authorSdkInstallTestVariables = @(
    'DTMAPI_INSTALL_AUTHOR_SDK_ARTIFACT_ROOT',
    'DTMAPI_INSTALL_AUTHOR_SDK_ONLY_UNIQUE_ID',
    'DTMAPI_INSTALL_AUTHOR_SDK_EXPECTED_VERSION',
    'DTMAPI_INSTALL_AUTHOR_SDK_CORRUPT_REPORT',
    'DTMAPI_AUTHOR_SDK_INSTALL_FAIL_PHASE'
)
$configuredAuthorSdkInstallTestVariables = @($authorSdkInstallTestVariables | Where-Object {
    -not [string]::IsNullOrWhiteSpace([string][Environment]::GetEnvironmentVariable($_))
})
if ($configuredAuthorSdkInstallTestVariables.Count -gt 0 -and
    -not [string]::Equals([string]$env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal)) {
    throw "Author SDK installer transaction overrides are test-only and require DTMAPI_INSTALL_TRANSACTION_TEST_MODE=1: $([string]::Join(', ', $configuredAuthorSdkInstallTestVariables))"
}
$authorSdkInstallFaultPhases = @('AfterPackagePreflight')
if (-not [string]::IsNullOrWhiteSpace([string]$env:DTMAPI_AUTHOR_SDK_INSTALL_FAIL_PHASE) -and
    $authorSdkInstallFaultPhases -notcontains [string]$env:DTMAPI_AUTHOR_SDK_INSTALL_FAIL_PHASE) {
    throw "Unsupported Author SDK installer fault phase '$($env:DTMAPI_AUTHOR_SDK_INSTALL_FAIL_PHASE)'."
}
if (-not $SkipBuild) {
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipTests
    & "$PSScriptRoot\build-player-doctor.ps1" -Configuration $Configuration
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$pluginDir = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
$stateDir = Resolve-DtmApiStateDir -GameDir $gameDir
$bepInExCore = Join-Path $gameDir 'BepInEx\core\BepInEx.dll'
$bepInExDetectedBeforeInstall = Test-Path $bepInExCore
$bepInExCompleteBeforeInstall = Test-DtmApiBepInExInstallComplete -GameDir $gameDir
$script:DtmInstallGamePathResolved = $true

function Write-DtmApiInstallFailureState {
    param([Parameter(Mandatory = $true)] $ErrorRecord)

    try {
        New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
        $failurePath = Join-Path $stateDir ("install-state.failed-$script:DtmInstallStamp.json")
        $failure = [ordered]@{
            SchemaVersion = 1
            FailedAt = (Get-Date).ToUniversalTime().ToString('o')
            DTMAPIVersion = $script:DtmApiReleaseVersion
            BinaryVersion = $script:DtmApiBinaryVersion
            SourceRepoCommit = $script:DtmInstallSourceCommit
            GameDir = [System.IO.Path]::GetFullPath($gameDir)
            PluginDir = [System.IO.Path]::GetFullPath($pluginDir)
            Error = [string]$ErrorRecord.Exception.Message
            ScriptName = [string]$ErrorRecord.InvocationInfo.ScriptName
            ScriptLineNumber = $ErrorRecord.InvocationInfo.ScriptLineNumber
            Line = [string]$ErrorRecord.InvocationInfo.Line
            FilesInstalledBeforeFailure = @($script:DtmInstallFilesInstalled.ToArray())
            BackupsCreatedBeforeFailure = @($script:DtmInstallBackupsCreated.ToArray())
            LegacyModsMovedBeforeFailure = @($script:DtmInstallLegacyModsMoved.ToArray())
            OfficialLocalAttempts = @($script:DtmInstallOfficialLocalAttempts.ToArray())
            RuntimeTransactionPhase = if ($null -eq $script:DtmRuntimeInstallTransaction) { '' } elseif (-not [string]::IsNullOrWhiteSpace([string]$script:DtmRuntimeInstallTransaction.FailedPhase)) { [string]$script:DtmRuntimeInstallTransaction.FailedPhase } else { [string]$script:DtmRuntimeInstallTransaction.Phase }
            RuntimeRollbackSucceeded = if ($null -eq $script:DtmRuntimeInstallTransaction) { $null } else { $script:DtmRuntimeInstallTransaction.RollbackSucceeded }
            RuntimeCommitSucceeded = if ($null -eq $script:DtmRuntimeInstallTransaction) { $false } else { [bool]$script:DtmRuntimeInstallTransaction.CommitSucceeded }
            RuntimeRecoveryBoundary = if ($null -ne $script:DtmRuntimeInstallTransaction -and $script:DtmRuntimeInstallTransaction.CommitSucceeded) { 'Runtime committed and retained before official-local product publication; later product failure does not roll Runtime back.' } else { 'Runtime was not committed; transaction rollback owns only Runtime and installer state.' }
            OfficialLocalRecoveryBoundary = 'Each package owns only its current publication/enablement rollback. Earlier completed packages remain installed and are listed in FilesInstalledBeforeFailure and OfficialLocalAttempts.'
        }
        Write-Utf8NoBomJson -Path $failurePath -Value $failure
        Write-Warning "DTMAPI install failed. Failure state written to $failurePath"
    }
    catch {
        Write-Warning "DTMAPI install failed, and writing failure state also failed: $($_.Exception.Message)"
    }
}

trap {
    $installErrorRecord = $_
    if ($null -ne $script:DtmRuntimeInstallTransaction -and
        (Get-Command Restore-DtmApiRuntimeInstallTransaction -ErrorAction SilentlyContinue)) {
        try {
            Restore-DtmApiRuntimeInstallTransaction -Transaction $script:DtmRuntimeInstallTransaction
        }
        catch {
            Write-Warning "DTMAPI Runtime transaction rollback failed: $($_.Exception.Message)"
        }
    }
    if ($script:DtmInstallGamePathResolved -and
        (Get-Command Write-DtmApiInstallFailureState -ErrorAction SilentlyContinue)) {
        Write-DtmApiInstallFailureState -ErrorRecord $installErrorRecord
    }
    else {
        Write-Warning "DTMAPI install failed before the game folder was resolved: $($installErrorRecord.Exception.Message)"
    }
    break
}

$packageSourceReleaseManifestPath = ''
$packageSourceReleaseManifest = $null
$packageBuildCommit = ''
if ($usingPackagePayload) {
    $packageSourceReleaseManifestPath = [System.IO.Path]::GetFullPath((Join-Path $PackagePayloadRoot '..\..\DTMAPI\release-manifest.json'))
    if (Test-Path -LiteralPath $packageSourceReleaseManifestPath -PathType Leaf) {
        try {
            $packageSourceReleaseManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $packageSourceReleaseManifestPath | ConvertFrom-Json
        }
        catch {
            throw "Packaged DTMAPI release manifest is unreadable: $packageSourceReleaseManifestPath. $($_.Exception.Message)"
        }
        $packageBuildCommit = [string](Get-DtmApiObjectProperty -Object $packageSourceReleaseManifest -Name 'BuildCommit' -Default '')
        if ([int](Get-DtmApiObjectProperty -Object $packageSourceReleaseManifest -Name 'SchemaVersion' -Default 0) -ne 1 -or
            -not [string]::Equals([string](Get-DtmApiObjectProperty -Object $packageSourceReleaseManifest -Name 'DTMAPIVersion' -Default ''), $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string](Get-DtmApiObjectProperty -Object $packageSourceReleaseManifest -Name 'BinaryVersion' -Default ''), $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string](Get-DtmApiObjectProperty -Object $packageSourceReleaseManifest -Name 'PackageKind' -Default ''), 'workshop-runtime', [System.StringComparison]::Ordinal) -or
            [string]::IsNullOrWhiteSpace($packageBuildCommit) -or
            $packageBuildCommit.Trim() -notmatch '^[0-9a-fA-F]{7,64}$') {
            throw "Packaged DTMAPI release manifest failed schema/version/build provenance validation: $packageSourceReleaseManifestPath"
        }
    }
    else {
        throw "Packaged DTMAPI release manifest is missing: $packageSourceReleaseManifestPath"
    }
}
$outDir = if ($usingPackagePayload) { Join-Path $PackagePayloadRoot 'BepInEx\plugins\DTMAPI' } else { Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration }

$runtimeFiles = @(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)
$versionAuthorityFileName = 'dtmapi-runtime-version.props'
$installPreflightScriptNames = @('common.ps1', 'release-common.ps1', 'install-to-game.ps1', 'install-bepinex.ps1')
$requiredStateToolScriptNames = @('common.ps1', 'release-common.ps1', 'uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1')
$diagnosticStateToolScriptNames = @('collect-logs.ps1', 'analyze-startup-evidence.ps1')
$stateToolScriptNames = @($requiredStateToolScriptNames + $diagnosticStateToolScriptNames)
$playerDoctorFileNames = @('dtmapi-player-doctor.exe', 'dotnet-LICENSE.txt', 'dotnet-ThirdPartyNotices.txt')

function Assert-DtmApiPlayerRuntimeSourceExcludesQaHost {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        throw "DTMAPI Runtime source root is missing: $resolvedRoot"
    }
    $forbidden = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force | Where-Object {
        $relative = $_.FullName.Substring($resolvedRoot.Length).TrimStart('\').Replace('\', '/')
        $name = $_.Name
        $relative -match '(^|/)qa-host(/|$)' -or
        $name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
        $name -match '^(?i:DTMAPI\.(Smoke|Tests)\.(dll|pdb))$' -or
        $name -match '^(?i:qa-settings\.json|smoke-settings\.json|qa-host.*\.json)$'
    })
    if ($forbidden.Count -gt 0) {
        $paths = @($forbidden | ForEach-Object {
            $_.FullName.Substring($resolvedRoot.Length).TrimStart('\').Replace('\', '/')
        } | Sort-Object -Unique)
        throw "DTMAPI player Runtime source contains developer-only QA host material: $([string]::Join(', ', $paths))"
    }
}

$playerRuntimeSourceGateRoot = if ($usingPackagePayload) { $PackagePayloadRoot } else { $outDir }
if ($usingPackagePayload -or -not $DryRun -or (Test-Path -LiteralPath $playerRuntimeSourceGateRoot -PathType Container)) {
    Assert-DtmApiPlayerRuntimeSourceExcludesQaHost -Root $playerRuntimeSourceGateRoot
}

$playerDoctorSourceRoot = Join-Path $installerToolSourceRoot 'player-doctor'
if (-not (Test-Path -LiteralPath $playerDoctorSourceRoot -PathType Container) -and -not $usingPackagePayload) {
    $playerDoctorSourceRoot = Join-Path $repo 'dist\player-doctor\win-x64'
}
$strictInstallerScriptNames = @($installPreflightScriptNames) | Sort-Object -Unique
$warningOnlyInstallerScriptNames = @($requiredStateToolScriptNames + $diagnosticStateToolScriptNames |
    Where-Object { $installPreflightScriptNames -notcontains $_ }) | Sort-Object -Unique
$strictSourceToolScripts = foreach ($scriptName in $strictInstallerScriptNames) {
    $sourceScript = Join-Path $installerToolSourceRoot $scriptName
    if (-not (Test-Path -LiteralPath $sourceScript -PathType Leaf)) {
        throw "Required DTMAPI installer helper script is missing: $sourceScript"
    }

    $sourceScript
}
$warningOnlySourceToolScripts = foreach ($scriptName in $warningOnlyInstallerScriptNames) {
    $sourceScript = Join-Path $installerToolSourceRoot $scriptName
    if (Test-Path -LiteralPath $sourceScript -PathType Leaf) {
        $sourceScript
    }
    else {
        Write-Warning "DTMAPI post-install helper script is missing and will not block install: $sourceScript"
    }
}
Test-DtmApiWindowsPowerShellSyntax -Paths @($strictSourceToolScripts) -AllowCoreFallback
if (@($warningOnlySourceToolScripts).Count -gt 0) {
    Test-DtmApiWindowsPowerShellSyntax -Paths @($warningOnlySourceToolScripts) -WarningOnly -AllowCoreFallback
}

function Resolve-DtmApiRuntimeIconSource {
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($outDir)) {
        $candidates += (Join-Path $outDir 'assets\branding\dtmapi-icon.png')
    }
    if ($usingPackagePayload -and -not [string]::IsNullOrWhiteSpace($PackagePayloadRoot)) {
        $candidates += (Join-Path $PackagePayloadRoot 'BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png')
    }
    $candidates += (Join-Path $repo 'assets\branding\dtmapi-icon.png')

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    return ''
}

function New-DtmApiRuntimeInstallTransaction {
    $allowedFaultPhases = @(
        'CandidatePrepared',
        'OldRuntimeMoved',
        'CandidatePlaced',
        'MovingOldComponents',
        'PlacingComponents',
        'ToolsCommitted',
        'ReleaseManifestCommitted',
        'InstallStateCommitted'
    )
    $requestedFaultPhase = [string]$env:DTMAPI_RUNTIME_INSTALL_FAIL_PHASE
    $requestedRollbackFaultPhase = [string]$env:DTMAPI_RUNTIME_INSTALL_ROLLBACK_FAIL_PHASE
    if (-not [string]::IsNullOrWhiteSpace($requestedFaultPhase)) {
        if (-not [string]::Equals([string]$env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal)) {
            throw 'DTMAPI_RUNTIME_INSTALL_FAIL_PHASE is test-only and requires DTMAPI_INSTALL_TRANSACTION_TEST_MODE=1.'
        }
        if ($allowedFaultPhases -notcontains $requestedFaultPhase) {
            throw "Unsupported DTMAPI Runtime install fault phase '$requestedFaultPhase'."
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($requestedRollbackFaultPhase)) {
        if (-not [string]::Equals([string]$env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal)) {
            throw 'DTMAPI_RUNTIME_INSTALL_ROLLBACK_FAIL_PHASE is test-only and requires DTMAPI_INSTALL_TRANSACTION_TEST_MODE=1.'
        }
        if (@('InstallState', 'ReleaseManifest', 'Tools', 'Components', 'Runtime') -notcontains $requestedRollbackFaultPhase) {
            throw "Unsupported DTMAPI Runtime rollback fault phase '$requestedRollbackFaultPhase'."
        }
    }

    Recover-DtmApiInterruptedRuntimeInstallTransactions

    $runtimeTransactionRoot = Join-Path $gameDir ('.dtmapi-runtime-install-' + $script:DtmInstallStamp)
    $transactionRoot = Join-Path $stateDir ('.runtime-install-transaction-' + $script:DtmInstallStamp)
    return [pscustomobject][ordered]@{
        Phase = 'Created'
        FailedPhase = ''
        RuntimeTransactionRoot = [System.IO.Path]::GetFullPath($runtimeTransactionRoot)
        ReceiptPath = [System.IO.Path]::GetFullPath((Join-Path $runtimeTransactionRoot 'transaction.json'))
        TransactionRoot = [System.IO.Path]::GetFullPath($transactionRoot)
        CandidatePlugin = [System.IO.Path]::GetFullPath((Join-Path $runtimeTransactionRoot 'candidate-plugin'))
        RecoveryPlugin = [System.IO.Path]::GetFullPath((Join-Path $runtimeTransactionRoot 'recovery-plugin'))
        CandidateTools = [System.IO.Path]::GetFullPath((Join-Path $transactionRoot 'candidate\tools'))
        CandidateComponents = [System.IO.Path]::GetFullPath((Join-Path $transactionRoot 'candidate\components'))
        CandidateReleaseManifest = [System.IO.Path]::GetFullPath((Join-Path $transactionRoot 'candidate\release-manifest.json'))
        CandidateInstallState = [System.IO.Path]::GetFullPath((Join-Path $transactionRoot 'candidate\install-state.json'))
        RecoveryState = [System.IO.Path]::GetFullPath((Join-Path $transactionRoot 'recovery'))
        LiveTools = [System.IO.Path]::GetFullPath((Join-Path $stateDir 'tools'))
        LiveComponents = [System.IO.Path]::GetFullPath((Join-Path $stateDir 'components'))
        LiveReleaseManifest = [System.IO.Path]::GetFullPath((Join-Path $stateDir 'release-manifest.json'))
        LiveInstallState = [System.IO.Path]::GetFullPath((Join-Path $stateDir 'install-state.json'))
        RuntimeMetadata = @()
        RuntimeFileRecords = @()
        StateToolRecords = @()
        OptionalComponentMetadata = @()
        OptionalComponentFileRecords = @()
        AssetRecord = $null
        OldPluginExisted = $false
        OldPluginMoved = $false
        CandidatePluginPlaced = $false
        OldToolsExisted = $false
        OldToolsMoved = $false
        CandidateToolsPlaced = $false
        OldComponentsExisted = $false
        OldComponentsMoved = $false
        CandidateComponentsPlaced = $false
        OldReleaseManifestExisted = $false
        OldReleaseManifestMoved = $false
        CandidateReleaseManifestPlaced = $false
        OldInstallStateExisted = $false
        OldInstallStateMoved = $false
        CandidateInstallStatePlaced = $false
        CommitSucceeded = $false
        RollbackSucceeded = $null
        RollbackFaultConsumed = $false
    }
}

function Invoke-DtmApiRuntimeInstallFault {
    param([Parameter(Mandatory = $true)] [string] $Phase)

    if ([string]::Equals([string]$env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal) -and
        [string]::Equals([string]$env:DTMAPI_RUNTIME_INSTALL_FAIL_PHASE, $Phase, [System.StringComparison]::Ordinal)) {
        throw "Injected DTMAPI Runtime install transaction failure at phase $Phase."
    }
}

function Invoke-DtmApiRuntimeRollbackFault {
    param(
        [Parameter(Mandatory = $true)] $Transaction,
        [Parameter(Mandatory = $true)] [string] $Phase
    )

    if (-not $Transaction.RollbackFaultConsumed -and
        [string]::Equals([string]$env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal) -and
        [string]::Equals([string]$env:DTMAPI_RUNTIME_INSTALL_ROLLBACK_FAIL_PHASE, $Phase, [System.StringComparison]::Ordinal)) {
        $Transaction.RollbackFaultConsumed = $true
        throw "Injected one-time DTMAPI Runtime rollback failure at phase $Phase."
    }
}

function Write-DtmApiRuntimeTransactionReceipt {
    param([Parameter(Mandatory = $true)] $Transaction)

    New-Item -ItemType Directory -Force -Path $Transaction.RuntimeTransactionRoot | Out-Null
    $receipt = [ordered]@{
        SchemaVersion = 1
        GameDir = [System.IO.Path]::GetFullPath($gameDir)
        StateDir = [System.IO.Path]::GetFullPath($stateDir)
        PluginDir = [System.IO.Path]::GetFullPath($pluginDir)
        Phase = [string]$Transaction.Phase
        FailedPhase = [string]$Transaction.FailedPhase
        RuntimeTransactionRoot = [string]$Transaction.RuntimeTransactionRoot
        TransactionRoot = [string]$Transaction.TransactionRoot
        CandidatePlugin = [string]$Transaction.CandidatePlugin
        RecoveryPlugin = [string]$Transaction.RecoveryPlugin
        CandidateTools = [string]$Transaction.CandidateTools
        CandidateComponents = [string]$Transaction.CandidateComponents
        CandidateReleaseManifest = [string]$Transaction.CandidateReleaseManifest
        CandidateInstallState = [string]$Transaction.CandidateInstallState
        RecoveryState = [string]$Transaction.RecoveryState
        LiveTools = [string]$Transaction.LiveTools
        LiveComponents = [string]$Transaction.LiveComponents
        LiveReleaseManifest = [string]$Transaction.LiveReleaseManifest
        LiveInstallState = [string]$Transaction.LiveInstallState
        OldPluginExisted = [bool]$Transaction.OldPluginExisted
        OldPluginMoved = [bool]$Transaction.OldPluginMoved
        CandidatePluginPlaced = [bool]$Transaction.CandidatePluginPlaced
        OldToolsExisted = [bool]$Transaction.OldToolsExisted
        OldToolsMoved = [bool]$Transaction.OldToolsMoved
        CandidateToolsPlaced = [bool]$Transaction.CandidateToolsPlaced
        OldComponentsExisted = [bool]$Transaction.OldComponentsExisted
        OldComponentsMoved = [bool]$Transaction.OldComponentsMoved
        CandidateComponentsPlaced = [bool]$Transaction.CandidateComponentsPlaced
        OldReleaseManifestExisted = [bool]$Transaction.OldReleaseManifestExisted
        OldReleaseManifestMoved = [bool]$Transaction.OldReleaseManifestMoved
        CandidateReleaseManifestPlaced = [bool]$Transaction.CandidateReleaseManifestPlaced
        OldInstallStateExisted = [bool]$Transaction.OldInstallStateExisted
        OldInstallStateMoved = [bool]$Transaction.OldInstallStateMoved
        CandidateInstallStatePlaced = [bool]$Transaction.CandidateInstallStatePlaced
        CommitSucceeded = [bool]$Transaction.CommitSucceeded
        RollbackSucceeded = $Transaction.RollbackSucceeded
    }
    $json = $receipt | ConvertTo-Json -Depth 8
    $tempPath = $Transaction.ReceiptPath + '.tmp-' + [Guid]::NewGuid().ToString('N')
    $backupPath = $Transaction.ReceiptPath + '.bak-' + [Guid]::NewGuid().ToString('N')
    try {
        [System.IO.File]::WriteAllText($tempPath, $json, (New-Object System.Text.UTF8Encoding($false)))
        if (Test-Path -LiteralPath $Transaction.ReceiptPath -PathType Leaf) {
            [System.IO.File]::Replace($tempPath, $Transaction.ReceiptPath, $backupPath)
            if (Test-Path -LiteralPath $backupPath) {
                Remove-Item -LiteralPath $backupPath -Force
            }
        }
        else {
            [System.IO.File]::Move($tempPath, $Transaction.ReceiptPath)
        }
    }
    finally {
        foreach ($path in @($tempPath, $backupPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force
            }
        }
    }
}

function Get-DtmApiManagedAssemblyFileVersion {
    param([Parameter(Mandatory = $true)] [string] $Path)

    try {
        # FileVersionInfo silently returns an empty value for some otherwise
        # readable long Unicode paths on Windows PowerShell 5.1. Reading the
        # managed metadata from bytes keeps candidate validation path-safe and
        # does not lock the staged DLL during the later directory switch.
        $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($Path))
        $attributes = @($assembly.GetCustomAttributesData() | Where-Object {
            [string]$_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute'
        })
        if ($attributes.Count -ne 1 -or $attributes[0].ConstructorArguments.Count -ne 1) {
            throw 'The managed assembly does not contain exactly one AssemblyFileVersionAttribute.'
        }
        return [string]$attributes[0].ConstructorArguments[0].Value
    }
    catch {
        throw "Could not read the managed file version from $Path. $($_.Exception.Message)"
    }
}

function Get-DtmApiRuntimeAssemblyMetadata {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $IntendedLiveRoot,
        [string] $ReferenceRoot = ''
    )

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        throw "DTMAPI Runtime candidate directory is missing: $Root"
    }

    $actualDllNames = @(Get-ChildItem -LiteralPath $Root -Filter '*.dll' -File | ForEach-Object { $_.Name } | Sort-Object)
    $expectedDllNames = @($runtimeFiles | Sort-Object)
    if (($actualDllNames -join '|') -ne ($expectedDllNames -join '|')) {
        throw "DTMAPI Runtime candidate DLL set is not exact. Expected=$($expectedDllNames -join ',') Actual=$($actualDllNames -join ',')"
    }

    $metadata = New-Object 'System.Collections.Generic.List[object]'
    foreach ($fileName in $runtimeFiles) {
        $path = Join-Path $Root $fileName
        $item = Get-Item -LiteralPath $path -ErrorAction Stop
        if ($item.Length -le 0) {
            throw "DTMAPI Runtime candidate assembly is empty: $path"
        }

        try {
            $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($path)
        }
        catch {
            throw "DTMAPI Runtime candidate assembly is not a valid managed DLL: $path. $($_.Exception.Message)"
        }
        $expectedAssemblyName = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
        if (-not [string]::Equals([string]$assemblyName.Name, $expectedAssemblyName, [System.StringComparison]::Ordinal)) {
            throw "DTMAPI Runtime candidate assembly identity mismatch for $fileName. Expected=$expectedAssemblyName Actual=$($assemblyName.Name)"
        }

        $fileVersion = Get-DtmApiManagedAssemblyFileVersion -Path $path
        if (-not [string]::Equals($fileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
            throw "DTMAPI Runtime candidate binary version mismatch for $fileName. Expected=$script:DtmApiBinaryVersion Actual=$fileVersion"
        }

        $sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not [string]::IsNullOrWhiteSpace($ReferenceRoot)) {
            $referencePath = Join-Path $ReferenceRoot $fileName
            if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
                throw "DTMAPI Runtime reference assembly is missing: $referencePath"
            }
            $referenceHash = (Get-FileHash -LiteralPath $referencePath -Algorithm SHA256).Hash.ToLowerInvariant()
            if (-not [string]::Equals($sha256, $referenceHash, [System.StringComparison]::Ordinal)) {
                throw "DTMAPI Runtime candidate copy hash mismatch for $fileName."
            }
        }

        $metadata.Add([ordered]@{
            FileName = $fileName
            Path = [System.IO.Path]::GetFullPath((Join-Path $IntendedLiveRoot $fileName))
            Length = [long]$item.Length
            Sha256 = $sha256
            FileVersion = $fileVersion
        }) | Out-Null
    }

    return $metadata.ToArray()
}

function Get-DtmApiOptionalComponentSourceDefinitions {
    $sourceRows = if ($usingPackagePayload) {
        @((Get-DtmApiObjectProperty -Object $packageSourceReleaseManifest -Name 'OptionalComponents' -Default @()))
    }
    else {
        $catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
        $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
        $packageInvariant = Get-DtmApiObjectProperty -Object $catalog -Name 'playerRuntimePackageInvariant' -Default $null
        @((Get-DtmApiObjectProperty -Object $packageInvariant -Name 'optionalComponents' -Default @()))
    }
    $sourceRows = @($sourceRows)
    if ($usingPackagePayload -and $sourceRows.Count -eq 0 -and
        -not [string]::Equals([string]$env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal)) {
        throw 'Packaged DTMAPI release manifest has no Catalog-driven OptionalComponents projection.'
    }

    $definitions = New-Object 'System.Collections.Generic.List[object]'
    $seenIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    $seenPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $sourceRows) {
        $componentId = [string](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'ComponentId' } else { 'id' }) -Default '')
        $distribution = [string](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'Distribution' } else { 'distribution' }) -Default '')
        $loadPolicy = [string](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'LoadPolicy' } else { 'loadPolicy' }) -Default '')
        $relativePath = ([string](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'RelativePath' } else { 'relativePath' }) -Default '')).Replace('\', '/')
        $assemblyName = [string](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'AssemblyName' } else { 'assemblyName' }) -Default '')
        $targetFramework = [string](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'TargetFramework' } else { 'targetFramework' }) -Default '')
        if ([string]::IsNullOrWhiteSpace($componentId) -or -not $seenIds.Add($componentId) -or
            [string]::IsNullOrWhiteSpace($relativePath) -or -not $seenPaths.Add($relativePath) -or
            -not $relativePath.StartsWith('DTMAPI/components/', [System.StringComparison]::Ordinal) -or
            $relativePath.Contains('../') -or $relativePath.Contains('/..') -or
            -not [string]::Equals($distribution, 'dormant-shipped', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($targetFramework, 'netstandard2.0', [System.StringComparison]::Ordinal)) {
            throw "Optional component definition is unsafe or unsupported: id='$componentId' path='$relativePath'."
        }
        $fileName = [System.IO.Path]::GetFileName($relativePath)
        if (-not [string]::Equals($fileName, $assemblyName + '.dll', [System.StringComparison]::Ordinal)) {
            throw "Optional component filename/assembly identity mismatch for '$componentId'."
        }
        $sourcePath = if ($usingPackagePayload) {
            Join-Path $PackagePayloadRoot ($relativePath.Replace('/', '\'))
        }
        else {
            $sourceProject = ([string](Get-DtmApiObjectProperty -Object $row -Name 'sourceProject' -Default '')).Replace('/', '\')
            if ([string]::IsNullOrWhiteSpace($sourceProject)) {
                throw "Optional component '$componentId' has no source project."
            }
            Join-Path (Split-Path -Parent (Join-Path $repo $sourceProject)) ("bin\{0}\{1}\{2}" -f $Configuration, $targetFramework, $fileName)
        }
        $definitions.Add([pscustomobject][ordered]@{
            ComponentId = $componentId
            Distribution = $distribution
            LoadPolicy = $loadPolicy
            RelativePath = $relativePath
            AssemblyName = $assemblyName
            TargetFramework = $targetFramework
            DefaultLoadState = [string](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'DefaultLoadState' } else { 'defaultLoadState' }) -Default '')
            IncludedInDownloadPackage = [bool](Get-DtmApiObjectProperty -Object $row -Name $(if ($usingPackagePayload) { 'IncludedInDownloadPackage' } else { 'includedInDownloadPackage' }) -Default $false)
            ExpectedLength = [long](Get-DtmApiObjectProperty -Object $row -Name 'Length' -Default 0)
            ExpectedSha256 = [string](Get-DtmApiObjectProperty -Object $row -Name 'Sha256' -Default '')
            ExpectedAssemblyVersion = [string](Get-DtmApiObjectProperty -Object $row -Name 'AssemblyVersion' -Default $script:DtmApiAssemblyCompatibilityVersion)
            ExpectedFileVersion = [string](Get-DtmApiObjectProperty -Object $row -Name 'FileVersion' -Default $script:DtmApiBinaryVersion)
            SourcePath = [System.IO.Path]::GetFullPath($sourcePath)
        }) | Out-Null
    }
    return $definitions.ToArray()
}

function Get-DtmApiOptionalComponentMetadata {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [object[]] $Definitions,
        [Parameter(Mandatory = $true)] [string] $IntendedLiveRoot,
        [string] $DestinationRoot = ''
    )

    $metadata = New-Object 'System.Collections.Generic.List[object]'
    foreach ($definition in $Definitions) {
        $sourcePath = [string]$definition.SourcePath
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Optional component source is missing: $sourcePath"
        }
        $path = $sourcePath
        $relativeUnderState = ([string]$definition.RelativePath).Substring('DTMAPI/'.Length).Replace('/', '\')
        if (-not [string]::IsNullOrWhiteSpace($DestinationRoot)) {
            $path = Join-Path $DestinationRoot $relativeUnderState
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
            Copy-Item -LiteralPath $sourcePath -Destination $path -Force
        }
        $item = Get-Item -LiteralPath $path -ErrorAction Stop
        if ($item.Length -le 0) {
            throw "Optional component is empty: $path"
        }
        try {
            $managedName = [System.Reflection.AssemblyName]::GetAssemblyName($path)
            $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($path))
            $targetAttributes = @($assembly.GetCustomAttributesData() | Where-Object {
                [string]$_.AttributeType.FullName -eq 'System.Runtime.Versioning.TargetFrameworkAttribute'
            })
        }
        catch {
            throw "Optional component metadata is unreadable: $path. $($_.Exception.Message)"
        }
        $fileVersion = Get-DtmApiManagedAssemblyFileVersion -Path $path
        $sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not [string]::Equals([string]$managedName.Name, [string]$definition.AssemblyName, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$managedName.Version, [string]$definition.ExpectedAssemblyVersion, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($fileVersion, [string]$definition.ExpectedFileVersion, [System.StringComparison]::Ordinal) -or
            $targetAttributes.Count -ne 1 -or $targetAttributes[0].ConstructorArguments.Count -ne 1 -or
            -not [string]::Equals([string]$targetAttributes[0].ConstructorArguments[0].Value, '.NETStandard,Version=v2.0', [System.StringComparison]::Ordinal)) {
            throw "Optional component identity/version/TFM mismatch for '$($definition.ComponentId)'."
        }
        if ([long]$definition.ExpectedLength -gt 0 -and [long]$definition.ExpectedLength -ne [long]$item.Length) {
            throw "Optional component length mismatch for '$($definition.ComponentId)'."
        }
        if (-not [string]::IsNullOrWhiteSpace([string]$definition.ExpectedSha256) -and
            -not [string]::Equals([string]$definition.ExpectedSha256, $sha256, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Optional component SHA-256 mismatch for '$($definition.ComponentId)'."
        }
        $metadata.Add([ordered]@{
            ComponentId = [string]$definition.ComponentId
            Distribution = [string]$definition.Distribution
            LoadPolicy = [string]$definition.LoadPolicy
            RelativePath = [string]$definition.RelativePath
            Path = [System.IO.Path]::GetFullPath((Join-Path $IntendedLiveRoot $relativeUnderState))
            Length = [long]$item.Length
            Sha256 = $sha256
            AssemblyName = [string]$managedName.Name
            AssemblyVersion = [string]$managedName.Version
            FileVersion = $fileVersion
            TargetFramework = [string]$definition.TargetFramework
            DefaultLoadState = [string]$definition.DefaultLoadState
            IncludedInDownloadPackage = [bool]$definition.IncludedInDownloadPackage
        }) | Out-Null
    }
    return $metadata.ToArray()
}

function Assert-DtmApiPackagedRuntimeManifestMatchesPayload {
    param(
        [Parameter(Mandatory = $true)] $Manifest,
        [Parameter(Mandatory = $true)] [string] $ManifestPath,
        [Parameter(Mandatory = $true)] [object[]] $ActualMetadata
    )

    $manifestAssemblies = @((Get-DtmApiObjectProperty -Object $Manifest -Name 'IncludedAssemblies' -Default @()))
    if ($manifestAssemblies.Count -ne $runtimeFiles.Count) {
        throw "Packaged DTMAPI release manifest payload receipt count mismatch. Expected=$($runtimeFiles.Count) Actual=$($manifestAssemblies.Count): $ManifestPath"
    }

    foreach ($actual in $ActualMetadata) {
        $fileName = [string]$actual.FileName
        $projected = @($manifestAssemblies | Where-Object {
            [string]::Equals([string](Get-DtmApiObjectProperty -Object $_ -Name 'FileName' -Default ''), $fileName, [System.StringComparison]::Ordinal)
        })
        if ($projected.Count -ne 1) {
            throw "Packaged DTMAPI release manifest must contain exactly one payload receipt for ${fileName}: $ManifestPath"
        }

        $receipt = $projected[0]
        $receiptSha256 = [string](Get-DtmApiObjectProperty -Object $receipt -Name 'Sha256' -Default '')
        $receiptFileVersion = [string](Get-DtmApiObjectProperty -Object $receipt -Name 'FileVersion' -Default '')
        $receiptLength = [long](Get-DtmApiObjectProperty -Object $receipt -Name 'Length' -Default 0)
        if ($receiptSha256 -notmatch '^[0-9a-fA-F]{64}$' -or
            -not [string]::Equals($receiptSha256, [string]$actual.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -or
            $receiptLength -ne [long]$actual.Length -or
            -not [string]::Equals($receiptFileVersion, [string]$actual.FileVersion, [System.StringComparison]::Ordinal)) {
            throw "Packaged DTMAPI release manifest payload receipt mismatch for $fileName. ExpectedLength=$receiptLength ActualLength=$($actual.Length) ExpectedSha256=$receiptSha256 ActualSha256=$($actual.Sha256) ExpectedFileVersion=$receiptFileVersion ActualFileVersion=$($actual.FileVersion): $ManifestPath"
        }
    }
}

function Initialize-DtmApiRuntimeInstallCandidate {
    param([Parameter(Mandatory = $true)] $Transaction)

    foreach ($path in @($Transaction.RuntimeTransactionRoot, $Transaction.TransactionRoot)) {
        if (Test-Path -LiteralPath $path) {
            throw "DTMAPI Runtime transaction path already exists: $path"
        }
    }

    New-Item -ItemType Directory -Force -Path $Transaction.RuntimeTransactionRoot | Out-Null
    Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
    $Transaction.Phase = 'PreparingCandidate'
    Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
    New-Item -ItemType Directory -Force -Path $Transaction.CandidatePlugin | Out-Null
    New-Item -ItemType Directory -Force -Path $Transaction.CandidateTools | Out-Null
    New-Item -ItemType Directory -Force -Path $Transaction.CandidateComponents | Out-Null
    Copy-DirectoryContents -Source $outDir -Destination $Transaction.CandidatePlugin -Include $runtimeFiles -Required

    $assetSource = Resolve-DtmApiRuntimeIconSource
    if (-not [string]::IsNullOrWhiteSpace($assetSource)) {
        $candidateAsset = Join-Path $Transaction.CandidatePlugin 'assets\branding\dtmapi-icon.png'
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $candidateAsset) | Out-Null
        Copy-Item -LiteralPath $assetSource -Destination $candidateAsset -Force
        if ((Get-Item -LiteralPath $candidateAsset).Length -le 0) {
            throw "DTMAPI Runtime candidate icon is empty: $candidateAsset"
        }
        $Transaction.AssetRecord = [ordered]@{
            Kind = 'runtime-asset'
            Path = [System.IO.Path]::GetFullPath((Join-Path $pluginDir 'assets\branding\dtmapi-icon.png'))
            Sha256 = (Get-FileHash -LiteralPath $candidateAsset -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    else {
        Write-Warning 'DTMAPI title icon asset was not found in the runtime payload or repository assets. The title button will use its text fallback.'
    }

    $Transaction.RuntimeMetadata = @(Get-DtmApiRuntimeAssemblyMetadata -Root $Transaction.CandidatePlugin -IntendedLiveRoot $pluginDir -ReferenceRoot $outDir)
    $Transaction.RuntimeFileRecords = @($Transaction.RuntimeMetadata | ForEach-Object {
        [ordered]@{
            Kind = 'runtime-assembly'
            Path = [string]$_.Path
            Sha256 = [string]$_.Sha256
            Length = [long]$_.Length
            FileVersion = [string]$_.FileVersion
        }
    })

    $candidateStateRoot = Split-Path -Parent $Transaction.CandidateComponents
    $Transaction.OptionalComponentMetadata = @(Get-DtmApiOptionalComponentMetadata `
        -Definitions $optionalComponentSourceDefinitions `
        -IntendedLiveRoot $stateDir `
        -DestinationRoot $candidateStateRoot)
    $Transaction.OptionalComponentFileRecords = @($Transaction.OptionalComponentMetadata | ForEach-Object {
        [ordered]@{
            Kind = 'optional-framework-component'
            ComponentId = [string]$_.ComponentId
            Path = [string]$_.Path
            Sha256 = [string]$_.Sha256
            Length = [long]$_.Length
            FileVersion = [string]$_.FileVersion
            Distribution = [string]$_.Distribution
            LoadPolicy = [string]$_.LoadPolicy
        }
    })

    $versionAuthorityDest = Join-Path $Transaction.CandidateTools $versionAuthorityFileName
    Copy-DtmApiTextFileUtf8Bom -Source $script:DtmApiVersionAuthority.Path -Destination $versionAuthorityDest
    [xml]$candidateAuthority = Get-Content -Raw -Encoding UTF8 -LiteralPath $versionAuthorityDest
    $candidateReleaseVersion = [string]$candidateAuthority.SelectSingleNode('/Project/PropertyGroup/DtmApiReleaseVersion').InnerText
    $candidateBinaryVersion = [string]$candidateAuthority.SelectSingleNode('/Project/PropertyGroup/DtmApiBinaryFileVersion').InnerText
    if (-not [string]::Equals($candidateReleaseVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals($candidateBinaryVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
        throw 'DTMAPI Runtime candidate version authority does not match the installer authority.'
    }
    $toolRecords = New-Object 'System.Collections.Generic.List[object]'
    $toolRecords.Add([ordered]@{
        Kind = 'version-authority'
        Path = [System.IO.Path]::GetFullPath((Join-Path $Transaction.LiveTools $versionAuthorityFileName))
    }) | Out-Null
    foreach ($scriptName in $requiredStateToolScriptNames) {
        $sourceScript = Join-Path $installerToolSourceRoot $scriptName
        if (-not (Test-Path -LiteralPath $sourceScript -PathType Leaf)) {
            throw "Required DTMAPI post-install helper script is missing: $sourceScript"
        }
        Copy-DtmApiTextFileUtf8Bom -Source $sourceScript -Destination (Join-Path $Transaction.CandidateTools $scriptName)
        $toolRecords.Add([ordered]@{
            Kind = 'installer-tool'
            Path = [System.IO.Path]::GetFullPath((Join-Path $Transaction.LiveTools $scriptName))
        }) | Out-Null
    }
    foreach ($scriptName in $diagnosticStateToolScriptNames) {
        $sourceScript = Join-Path $installerToolSourceRoot $scriptName
        if (-not (Test-Path -LiteralPath $sourceScript -PathType Leaf)) {
            Write-Warning "Optional DTMAPI diagnostic helper script was not staged because it is missing: $sourceScript"
            continue
        }
        Copy-DtmApiTextFileUtf8Bom -Source $sourceScript -Destination (Join-Path $Transaction.CandidateTools $scriptName)
        $toolRecords.Add([ordered]@{
            Kind = 'diagnostic-tool'
            Path = [System.IO.Path]::GetFullPath((Join-Path $Transaction.LiveTools $scriptName))
        }) | Out-Null
    }
    if (-not (Test-Path -LiteralPath $playerDoctorSourceRoot -PathType Container)) {
        throw "Required read-only Player Doctor directory is missing: $playerDoctorSourceRoot"
    }
    $actualPlayerDoctorFiles = @(Get-ChildItem -LiteralPath $playerDoctorSourceRoot -File -Recurse | ForEach-Object {
        $_.FullName.Substring(([System.IO.Path]::GetFullPath($playerDoctorSourceRoot)).TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
    } | Sort-Object)
    $expectedPlayerDoctorFiles = @($playerDoctorFileNames | Sort-Object)
    if (($actualPlayerDoctorFiles -join '|') -ne ($expectedPlayerDoctorFiles -join '|')) {
        throw "Player Doctor source file set is not exact. Expected=$($expectedPlayerDoctorFiles -join ',') Actual=$($actualPlayerDoctorFiles -join ',')"
    }
    $playerDoctorExeSource = Join-Path $playerDoctorSourceRoot 'dtmapi-player-doctor.exe'
    $playerDoctorVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($playerDoctorExeSource)
    if (-not [string]::Equals([string]$playerDoctorVersion.FileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$playerDoctorVersion.ProductVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal)) {
        throw "Player Doctor version mismatch. Expected=$script:DtmApiReleaseVersion/$script:DtmApiBinaryVersion Actual=$($playerDoctorVersion.ProductVersion)/$($playerDoctorVersion.FileVersion)"
    }
    $candidatePlayerDoctorRoot = Join-Path $Transaction.CandidateTools 'player-doctor'
    New-Item -ItemType Directory -Path $candidatePlayerDoctorRoot -Force | Out-Null
    foreach ($playerDoctorFileName in $playerDoctorFileNames) {
        $sourceFile = Join-Path $playerDoctorSourceRoot $playerDoctorFileName
        $destinationFile = Join-Path $candidatePlayerDoctorRoot $playerDoctorFileName
        Copy-Item -LiteralPath $sourceFile -Destination $destinationFile -Force
        $toolRecords.Add([ordered]@{
            Kind = if ($playerDoctorFileName -eq 'dtmapi-player-doctor.exe') { 'player-doctor' } else { 'player-doctor-license' }
            Path = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $Transaction.LiveTools 'player-doctor') $playerDoctorFileName))
            Sha256 = (Get-FileHash -LiteralPath $destinationFile -Algorithm SHA256).Hash.ToLowerInvariant()
        }) | Out-Null
    }
    $Transaction.StateToolRecords = $toolRecords.ToArray()

    $requiredCandidateTools = @($requiredStateToolScriptNames | ForEach-Object { Join-Path $Transaction.CandidateTools $_ })
    Test-DtmApiWindowsPowerShellSyntax -Paths $requiredCandidateTools -AllowCoreFallback
    $diagnosticCandidateTools = @(Get-ChildItem -LiteralPath $Transaction.CandidateTools -Filter '*.ps1' -File |
        Where-Object { $diagnosticStateToolScriptNames -contains $_.Name } |
        ForEach-Object { $_.FullName })
    if ($diagnosticCandidateTools.Count -gt 0) {
        Test-DtmApiWindowsPowerShellSyntax -Paths $diagnosticCandidateTools -WarningOnly -AllowCoreFallback
    }

    $Transaction.Phase = 'CandidatePrepared'
    Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
    Invoke-DtmApiRuntimeInstallFault -Phase 'CandidatePrepared'
}

function Set-DtmApiRuntimeInstallCandidateState {
    param(
        [Parameter(Mandatory = $true)] $Transaction,
        [Parameter(Mandatory = $true)] $ReleaseManifest,
        [Parameter(Mandatory = $true)] $InstallState
    )

    $Transaction.Phase = 'PreparingCandidateState'
    Write-Utf8NoBomJson -Path $Transaction.CandidateReleaseManifest -Value $ReleaseManifest
    Write-Utf8NoBomJson -Path $Transaction.CandidateInstallState -Value $InstallState
    $parsedRelease = Get-Content -Raw -Encoding UTF8 -LiteralPath $Transaction.CandidateReleaseManifest | ConvertFrom-Json
    $parsedState = Get-Content -Raw -Encoding UTF8 -LiteralPath $Transaction.CandidateInstallState | ConvertFrom-Json
    if (-not [string]::Equals([string]$parsedRelease.DTMAPIVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$parsedRelease.BinaryVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) -or
        @($parsedRelease.IncludedAssemblies).Count -ne $runtimeFiles.Count) {
        throw 'DTMAPI Runtime candidate release-manifest.json failed version or assembly-set validation.'
    }
    foreach ($expected in @($Transaction.RuntimeMetadata)) {
        $projected = @($parsedRelease.IncludedAssemblies | Where-Object { [string]$_.FileName -eq [string]$expected.FileName })
        if ($projected.Count -ne 1 -or
            -not [string]::Equals([string]$projected[0].Sha256, [string]$expected.Sha256, [System.StringComparison]::Ordinal) -or
            [long]$projected[0].Length -ne [long]$expected.Length -or
            -not [string]::Equals([string]$projected[0].FileVersion, [string]$expected.FileVersion, [System.StringComparison]::Ordinal)) {
            throw "DTMAPI Runtime candidate release-manifest projection mismatch for $($expected.FileName)."
        }
    }
    $releaseOptionalComponents = @((Get-DtmApiObjectProperty -Object $parsedRelease -Name 'OptionalComponents' -Default @()))
    $stateOptionalComponents = @((Get-DtmApiObjectProperty -Object $parsedState -Name 'OptionalComponents' -Default @()))
    if ($releaseOptionalComponents.Count -ne @($Transaction.OptionalComponentMetadata).Count -or
        $stateOptionalComponents.Count -ne @($Transaction.OptionalComponentMetadata).Count) {
        throw 'DTMAPI Runtime candidate optional-component projection count mismatch.'
    }
    foreach ($expected in @($Transaction.OptionalComponentMetadata)) {
        foreach ($projectionSet in @(
            [pscustomobject]@{ Label = 'release-manifest'; Rows = $releaseOptionalComponents },
            [pscustomobject]@{ Label = 'install-state'; Rows = $stateOptionalComponents }
        )) {
            $rows = @($projectionSet.Rows | Where-Object { [string]$_.ComponentId -eq [string]$expected.ComponentId })
            if ($rows.Count -ne 1 -or
                -not [string]::Equals([string]$rows[0].RelativePath, [string]$expected.RelativePath, [System.StringComparison]::Ordinal) -or
                -not [string]::Equals([string]$rows[0].Sha256, [string]$expected.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -or
                [long]$rows[0].Length -ne [long]$expected.Length -or
                -not [string]::Equals([string]$rows[0].AssemblyName, [string]$expected.AssemblyName, [System.StringComparison]::Ordinal) -or
                -not [string]::Equals([string]$rows[0].AssemblyVersion, [string]$expected.AssemblyVersion, [System.StringComparison]::Ordinal) -or
                -not [string]::Equals([string]$rows[0].TargetFramework, [string]$expected.TargetFramework, [System.StringComparison]::Ordinal)) {
                throw "DTMAPI Runtime candidate $($projectionSet.Label) optional-component projection mismatch for $($expected.ComponentId)."
            }
        }
    }
    if (-not [string]::Equals([string]$parsedState.DTMAPIVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$parsedState.BinaryVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$parsedRelease.BuildCommit, $script:DtmInstallSourceCommit, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$parsedState.SourceRepoCommit, $script:DtmInstallSourceCommit, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([System.IO.Path]::GetFullPath([string]$parsedState.PluginDir), [System.IO.Path]::GetFullPath($pluginDir), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'DTMAPI Runtime candidate install-state.json failed version or install-path validation.'
    }
    $Transaction.Phase = 'CandidateStatePrepared'
    Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
}

function Remove-DtmApiRuntimeInstallTransactionArtifacts {
    param([Parameter(Mandatory = $true)] $Transaction)

    # Keep the durable receipt root until last so an interrupted cleanup can be
    # resumed safely on the next installer run.
    foreach ($path in @($Transaction.TransactionRoot, $Transaction.CandidatePlugin, $Transaction.RecoveryPlugin, $Transaction.RuntimeTransactionRoot)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

function Restore-DtmApiRuntimeInstallTransaction {
    param([Parameter(Mandatory = $true)] $Transaction)

    if ($Transaction.CommitSucceeded) {
        Remove-DtmApiRuntimeInstallTransactionArtifacts -Transaction $Transaction
        return
    }
    if ($Transaction.RollbackSucceeded -eq $true) {
        Remove-DtmApiRuntimeInstallTransactionArtifacts -Transaction $Transaction
        return
    }

    if ([string]::IsNullOrWhiteSpace([string]$Transaction.FailedPhase)) {
        $Transaction.FailedPhase = [string]$Transaction.Phase
    }
    $componentRecoveryPath = Join-Path $Transaction.RecoveryState 'components'
    switch ([string]$Transaction.Phase) {
        'MovingOldComponents' {
            if (Test-Path -LiteralPath $componentRecoveryPath -PathType Container) {
                $Transaction.OldComponentsMoved = $true
            }
        }
        'PlacingComponents' {
            if (-not (Test-Path -LiteralPath $Transaction.CandidateComponents -PathType Container) -and
                (Test-Path -LiteralPath $Transaction.LiveComponents -PathType Container)) {
                $Transaction.CandidateComponentsPlaced = $true
            }
        }
    }
    $Transaction.Phase = 'RollingBack'
    Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
    $rollbackErrors = New-Object 'System.Collections.Generic.List[string]'

    try {
        if ($Transaction.CandidateInstallStatePlaced) {
            if (Test-Path -LiteralPath $Transaction.LiveInstallState) {
                Remove-Item -LiteralPath $Transaction.LiveInstallState -Force
            }
            $Transaction.CandidateInstallStatePlaced = $false
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        }
        Invoke-DtmApiRuntimeRollbackFault -Transaction $Transaction -Phase 'InstallState'
        $recoveryPath = Join-Path $Transaction.RecoveryState 'install-state.json'
        if ($Transaction.OldInstallStateMoved) {
            if (Test-Path -LiteralPath $recoveryPath -PathType Leaf) {
                [System.IO.File]::Move($recoveryPath, $Transaction.LiveInstallState)
                $Transaction.OldInstallStateMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            elseif (Test-Path -LiteralPath $Transaction.LiveInstallState -PathType Leaf) {
                $Transaction.OldInstallStateMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            else {
                throw "Old install-state recovery file is missing: $recoveryPath"
            }
        }
    }
    catch { $rollbackErrors.Add('install-state: ' + [string]$_.Exception.Message) | Out-Null }

    try {
        if ($Transaction.CandidateReleaseManifestPlaced) {
            if (Test-Path -LiteralPath $Transaction.LiveReleaseManifest) {
                Remove-Item -LiteralPath $Transaction.LiveReleaseManifest -Force
            }
            $Transaction.CandidateReleaseManifestPlaced = $false
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        }
        Invoke-DtmApiRuntimeRollbackFault -Transaction $Transaction -Phase 'ReleaseManifest'
        $recoveryPath = Join-Path $Transaction.RecoveryState 'release-manifest.json'
        if ($Transaction.OldReleaseManifestMoved) {
            if (Test-Path -LiteralPath $recoveryPath -PathType Leaf) {
                [System.IO.File]::Move($recoveryPath, $Transaction.LiveReleaseManifest)
                $Transaction.OldReleaseManifestMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            elseif (Test-Path -LiteralPath $Transaction.LiveReleaseManifest -PathType Leaf) {
                $Transaction.OldReleaseManifestMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            else {
                throw "Old release-manifest recovery file is missing: $recoveryPath"
            }
        }
    }
    catch { $rollbackErrors.Add('release-manifest: ' + [string]$_.Exception.Message) | Out-Null }

    try {
        if ($Transaction.CandidateToolsPlaced) {
            if (Test-Path -LiteralPath $Transaction.LiveTools) {
                Remove-Item -LiteralPath $Transaction.LiveTools -Recurse -Force
            }
            $Transaction.CandidateToolsPlaced = $false
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        }
        Invoke-DtmApiRuntimeRollbackFault -Transaction $Transaction -Phase 'Tools'
        $recoveryPath = Join-Path $Transaction.RecoveryState 'tools'
        if ($Transaction.OldToolsMoved) {
            if (Test-Path -LiteralPath $recoveryPath -PathType Container) {
                [System.IO.Directory]::Move($recoveryPath, $Transaction.LiveTools)
                $Transaction.OldToolsMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            elseif (Test-Path -LiteralPath $Transaction.LiveTools -PathType Container) {
                $Transaction.OldToolsMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            else {
                throw "Old tools recovery directory is missing: $recoveryPath"
            }
        }
    }
    catch { $rollbackErrors.Add('tools: ' + [string]$_.Exception.Message) | Out-Null }

    try {
        if ($Transaction.CandidateComponentsPlaced) {
            if (Test-Path -LiteralPath $Transaction.LiveComponents) {
                Remove-Item -LiteralPath $Transaction.LiveComponents -Recurse -Force
            }
            $Transaction.CandidateComponentsPlaced = $false
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        }
        Invoke-DtmApiRuntimeRollbackFault -Transaction $Transaction -Phase 'Components'
        $recoveryPath = Join-Path $Transaction.RecoveryState 'components'
        if ($Transaction.OldComponentsMoved) {
            if (Test-Path -LiteralPath $recoveryPath -PathType Container) {
                [System.IO.Directory]::Move($recoveryPath, $Transaction.LiveComponents)
                $Transaction.OldComponentsMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            elseif (Test-Path -LiteralPath $Transaction.LiveComponents -PathType Container) {
                $Transaction.OldComponentsMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            else {
                throw "Old components recovery directory is missing: $recoveryPath"
            }
        }
    }
    catch { $rollbackErrors.Add('components: ' + [string]$_.Exception.Message) | Out-Null }

    try {
        if ($Transaction.CandidatePluginPlaced) {
            if (Test-Path -LiteralPath $pluginDir) {
                Remove-Item -LiteralPath $pluginDir -Recurse -Force
            }
            $Transaction.CandidatePluginPlaced = $false
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        }
        Invoke-DtmApiRuntimeRollbackFault -Transaction $Transaction -Phase 'Runtime'
        if ($Transaction.OldPluginMoved) {
            if (Test-Path -LiteralPath $Transaction.RecoveryPlugin -PathType Container) {
                [System.IO.Directory]::Move($Transaction.RecoveryPlugin, $pluginDir)
                $Transaction.OldPluginMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            elseif (Test-Path -LiteralPath $pluginDir -PathType Container) {
                $Transaction.OldPluginMoved = $false
                Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            }
            else {
                throw "Old Runtime recovery directory is missing: $($Transaction.RecoveryPlugin)"
            }
        }
    }
    catch { $rollbackErrors.Add('runtime-directory: ' + [string]$_.Exception.Message) | Out-Null }

    if ($rollbackErrors.Count -gt 0) {
        $Transaction.RollbackSucceeded = $false
        $Transaction.Phase = 'RollbackFailed'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        throw ('Could not restore the complete previous DTMAPI Runtime set. Recovery data was preserved. ' + ($rollbackErrors.ToArray() -join ' | '))
    }

    $Transaction.RollbackSucceeded = $true
    $Transaction.Phase = 'RolledBack'
    Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
    Remove-DtmApiRuntimeInstallTransactionArtifacts -Transaction $Transaction
}

function Assert-DtmApiRuntimeTransactionPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Actual,
        [Parameter(Mandatory = $true)] [string] $Expected,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $actualFull = [System.IO.Path]::GetFullPath($Actual)
    $expectedFull = [System.IO.Path]::GetFullPath($Expected)
    if (-not [string]::Equals($actualFull, $expectedFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Interrupted DTMAPI Runtime transaction has an unsafe $Label path. Expected=$expectedFull Actual=$actualFull"
    }
}

function Read-DtmApiRuntimeTransactionReceipt {
    param([Parameter(Mandatory = $true)] [string] $RuntimeTransactionRoot)

    $receiptPath = Join-Path $RuntimeTransactionRoot 'transaction.json'
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        throw "Interrupted DTMAPI Runtime transaction has no recovery receipt: $RuntimeTransactionRoot"
    }
    try {
        $receipt = Get-Content -Raw -Encoding UTF8 -LiteralPath $receiptPath | ConvertFrom-Json
    }
    catch {
        throw "Interrupted DTMAPI Runtime transaction receipt is unreadable: $receiptPath. $($_.Exception.Message)"
    }
    $requiredProperties = @(
        'SchemaVersion', 'GameDir', 'StateDir', 'PluginDir', 'Phase', 'RuntimeTransactionRoot', 'TransactionRoot',
        'CandidatePlugin', 'RecoveryPlugin', 'CandidateTools', 'CandidateReleaseManifest', 'CandidateInstallState',
        'RecoveryState', 'LiveTools', 'LiveReleaseManifest', 'LiveInstallState', 'OldPluginExisted', 'OldPluginMoved',
        'CandidatePluginPlaced', 'OldToolsExisted', 'OldToolsMoved', 'CandidateToolsPlaced',
        'OldReleaseManifestExisted', 'OldReleaseManifestMoved', 'CandidateReleaseManifestPlaced',
        'OldInstallStateExisted', 'OldInstallStateMoved', 'CandidateInstallStatePlaced', 'CommitSucceeded'
    )
    foreach ($propertyName in $requiredProperties) {
        if ($null -eq $receipt.PSObject.Properties[$propertyName]) {
            throw "Interrupted DTMAPI Runtime transaction receipt is missing ${propertyName}: $receiptPath"
        }
    }
    if ([int]$receipt.SchemaVersion -ne 1) {
        throw "Interrupted DTMAPI Runtime transaction receipt has unsupported schema $($receipt.SchemaVersion): $receiptPath"
    }
    $componentReceiptProperties = @(
        'CandidateComponents', 'LiveComponents', 'OldComponentsExisted', 'OldComponentsMoved', 'CandidateComponentsPlaced'
    )
    $componentReceiptPropertyCount = @($componentReceiptProperties | Where-Object { $null -ne $receipt.PSObject.Properties[$_] }).Count
    if ($componentReceiptPropertyCount -ne 0 -and $componentReceiptPropertyCount -ne $componentReceiptProperties.Count) {
        throw "Interrupted DTMAPI Runtime transaction receipt has an incomplete optional-component projection: $receiptPath"
    }
    $hasComponentReceipt = $componentReceiptPropertyCount -eq $componentReceiptProperties.Count

    $runtimeRootFull = [System.IO.Path]::GetFullPath($RuntimeTransactionRoot)
    $runtimeRootName = Split-Path -Leaf $runtimeRootFull
    $runtimePrefix = '.dtmapi-runtime-install-'
    if (-not $runtimeRootName.StartsWith($runtimePrefix, [System.StringComparison]::Ordinal) -or $runtimeRootName.Length -le $runtimePrefix.Length) {
        throw "Interrupted DTMAPI Runtime transaction directory has an invalid name: $runtimeRootFull"
    }
    Assert-DtmApiRuntimeTransactionPath -Actual (Split-Path -Parent $runtimeRootFull) -Expected $gameDir -Label 'game-root parent'
    $stamp = $runtimeRootName.Substring($runtimePrefix.Length)
    $expectedStateTransactionRoot = Join-Path $stateDir ('.runtime-install-transaction-' + $stamp)
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.GameDir) -Expected $gameDir -Label 'game directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.StateDir) -Expected $stateDir -Label 'state directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.PluginDir) -Expected $pluginDir -Label 'live plugin directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RuntimeTransactionRoot) -Expected $runtimeRootFull -Label 'runtime transaction root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.TransactionRoot) -Expected $expectedStateTransactionRoot -Label 'state transaction root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidatePlugin) -Expected (Join-Path $runtimeRootFull 'candidate-plugin') -Label 'candidate plugin'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RecoveryPlugin) -Expected (Join-Path $runtimeRootFull 'recovery-plugin') -Label 'recovery plugin'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateTools) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\tools') -Label 'candidate tools'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateReleaseManifest) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\release-manifest.json') -Label 'candidate release manifest'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateInstallState) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\install-state.json') -Label 'candidate install state'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RecoveryState) -Expected (Join-Path $expectedStateTransactionRoot 'recovery') -Label 'state recovery root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveTools) -Expected (Join-Path $stateDir 'tools') -Label 'live tools'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveReleaseManifest) -Expected (Join-Path $stateDir 'release-manifest.json') -Label 'live release manifest'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveInstallState) -Expected (Join-Path $stateDir 'install-state.json') -Label 'live install state'
    if ($hasComponentReceipt) {
        Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateComponents) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\components') -Label 'candidate components'
        Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveComponents) -Expected (Join-Path $stateDir 'components') -Label 'live components'
    }

    $rollbackSucceeded = $null
    if ($null -ne $receipt.PSObject.Properties['RollbackSucceeded'] -and $null -ne $receipt.RollbackSucceeded) {
        $rollbackSucceeded = [bool]$receipt.RollbackSucceeded
    }
    $transaction = [pscustomobject][ordered]@{
        Phase = [string]$receipt.Phase
        FailedPhase = if ($null -eq $receipt.PSObject.Properties['FailedPhase']) { '' } else { [string]$receipt.FailedPhase }
        RuntimeTransactionRoot = $runtimeRootFull
        ReceiptPath = [System.IO.Path]::GetFullPath($receiptPath)
        TransactionRoot = [System.IO.Path]::GetFullPath([string]$receipt.TransactionRoot)
        CandidatePlugin = [System.IO.Path]::GetFullPath([string]$receipt.CandidatePlugin)
        RecoveryPlugin = [System.IO.Path]::GetFullPath([string]$receipt.RecoveryPlugin)
        CandidateTools = [System.IO.Path]::GetFullPath([string]$receipt.CandidateTools)
        CandidateComponents = [System.IO.Path]::GetFullPath($(if ($hasComponentReceipt) { [string]$receipt.CandidateComponents } else { Join-Path ([string]$receipt.TransactionRoot) 'candidate\components' }))
        CandidateReleaseManifest = [System.IO.Path]::GetFullPath([string]$receipt.CandidateReleaseManifest)
        CandidateInstallState = [System.IO.Path]::GetFullPath([string]$receipt.CandidateInstallState)
        RecoveryState = [System.IO.Path]::GetFullPath([string]$receipt.RecoveryState)
        LiveTools = [System.IO.Path]::GetFullPath([string]$receipt.LiveTools)
        LiveComponents = [System.IO.Path]::GetFullPath($(if ($hasComponentReceipt) { [string]$receipt.LiveComponents } else { Join-Path $stateDir 'components' }))
        LiveReleaseManifest = [System.IO.Path]::GetFullPath([string]$receipt.LiveReleaseManifest)
        LiveInstallState = [System.IO.Path]::GetFullPath([string]$receipt.LiveInstallState)
        RuntimeMetadata = @()
        RuntimeFileRecords = @()
        StateToolRecords = @()
        OptionalComponentMetadata = @()
        OptionalComponentFileRecords = @()
        AssetRecord = $null
        OldPluginExisted = [bool]$receipt.OldPluginExisted
        OldPluginMoved = [bool]$receipt.OldPluginMoved
        CandidatePluginPlaced = [bool]$receipt.CandidatePluginPlaced
        OldToolsExisted = [bool]$receipt.OldToolsExisted
        OldToolsMoved = [bool]$receipt.OldToolsMoved
        CandidateToolsPlaced = [bool]$receipt.CandidateToolsPlaced
        OldComponentsExisted = if ($hasComponentReceipt) { [bool]$receipt.OldComponentsExisted } else { $false }
        OldComponentsMoved = if ($hasComponentReceipt) { [bool]$receipt.OldComponentsMoved } else { $false }
        CandidateComponentsPlaced = if ($hasComponentReceipt) { [bool]$receipt.CandidateComponentsPlaced } else { $false }
        OldReleaseManifestExisted = [bool]$receipt.OldReleaseManifestExisted
        OldReleaseManifestMoved = [bool]$receipt.OldReleaseManifestMoved
        CandidateReleaseManifestPlaced = [bool]$receipt.CandidateReleaseManifestPlaced
        OldInstallStateExisted = [bool]$receipt.OldInstallStateExisted
        OldInstallStateMoved = [bool]$receipt.OldInstallStateMoved
        CandidateInstallStatePlaced = [bool]$receipt.CandidateInstallStatePlaced
        CommitSucceeded = [bool]$receipt.CommitSucceeded
        RollbackSucceeded = $rollbackSucceeded
        RollbackFaultConsumed = $false
    }

    switch ([string]$transaction.Phase) {
        'MovingOldRuntime' { if (Test-Path -LiteralPath $transaction.RecoveryPlugin -PathType Container) { $transaction.OldPluginMoved = $true } }
        'PlacingCandidate' { if (-not (Test-Path -LiteralPath $transaction.CandidatePlugin -PathType Container) -and (Test-Path -LiteralPath $pluginDir -PathType Container)) { $transaction.CandidatePluginPlaced = $true } }
        'MovingOldTools' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'tools') -PathType Container) { $transaction.OldToolsMoved = $true } }
        'PlacingTools' { if (-not (Test-Path -LiteralPath $transaction.CandidateTools -PathType Container) -and (Test-Path -LiteralPath $transaction.LiveTools -PathType Container)) { $transaction.CandidateToolsPlaced = $true } }
        'MovingOldComponents' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'components') -PathType Container) { $transaction.OldComponentsMoved = $true } }
        'PlacingComponents' { if (-not (Test-Path -LiteralPath $transaction.CandidateComponents -PathType Container) -and (Test-Path -LiteralPath $transaction.LiveComponents -PathType Container)) { $transaction.CandidateComponentsPlaced = $true } }
        'MovingOldReleaseManifest' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'release-manifest.json') -PathType Leaf) { $transaction.OldReleaseManifestMoved = $true } }
        'PlacingReleaseManifest' { if (-not (Test-Path -LiteralPath $transaction.CandidateReleaseManifest -PathType Leaf) -and (Test-Path -LiteralPath $transaction.LiveReleaseManifest -PathType Leaf)) { $transaction.CandidateReleaseManifestPlaced = $true } }
        'MovingOldInstallState' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'install-state.json') -PathType Leaf) { $transaction.OldInstallStateMoved = $true } }
        'PlacingInstallState' { if (-not (Test-Path -LiteralPath $transaction.CandidateInstallState -PathType Leaf) -and (Test-Path -LiteralPath $transaction.LiveInstallState -PathType Leaf)) { $transaction.CandidateInstallStatePlaced = $true } }
    }
    return $transaction
}

function Recover-DtmApiInterruptedRuntimeInstallTransactions {
    $runtimeTransactions = @(Get-ChildItem -LiteralPath $gameDir -Directory -Force -Filter '.dtmapi-runtime-install-*' -ErrorAction SilentlyContinue | Sort-Object Name)
    foreach ($runtimeTransaction in $runtimeTransactions) {
        $transaction = Read-DtmApiRuntimeTransactionReceipt -RuntimeTransactionRoot $runtimeTransaction.FullName
        Restore-DtmApiRuntimeInstallTransaction -Transaction $transaction
        Write-Warning "Recovered interrupted DTMAPI Runtime transaction from $($runtimeTransaction.FullName)."
    }

    $orphanedStateTransactions = @()
    if (Test-Path -LiteralPath $stateDir -PathType Container) {
        $orphanedStateTransactions = @(Get-ChildItem -LiteralPath $stateDir -Directory -Force -Filter '.runtime-install-transaction-*' -ErrorAction SilentlyContinue)
    }
    if ($orphanedStateTransactions.Count -gt 0) {
        $orphanedPaths = @($orphanedStateTransactions | ForEach-Object { $_.FullName })
        throw "DTMAPI Runtime state recovery data has no matching validated transaction receipt. Refusing to install: $($orphanedPaths -join '; ')"
    }
}

function Assert-DtmApiCommittedRuntimeInstall {
    param([Parameter(Mandatory = $true)] $Transaction)

    $liveMetadata = @(Get-DtmApiRuntimeAssemblyMetadata -Root $pluginDir -IntendedLiveRoot $pluginDir)
    foreach ($expected in @($Transaction.RuntimeMetadata)) {
        $actual = @($liveMetadata | Where-Object { [string]$_.FileName -eq [string]$expected.FileName })
        if ($actual.Count -ne 1 -or
            -not [string]::Equals([string]$actual[0].Sha256, [string]$expected.Sha256, [System.StringComparison]::Ordinal)) {
            throw "Committed DTMAPI Runtime assembly validation failed for $($expected.FileName)."
        }
    }
    $expectedComponentFiles = New-Object 'System.Collections.Generic.List[string]'
    foreach ($expected in @($Transaction.OptionalComponentMetadata)) {
        $componentPath = [string]$expected.Path
        $expectedComponentFiles.Add([System.IO.Path]::GetFullPath($componentPath)) | Out-Null
        if (-not (Test-Path -LiteralPath $componentPath -PathType Leaf)) {
            throw "Committed optional component is missing: $componentPath"
        }
        $componentItem = Get-Item -LiteralPath $componentPath -ErrorAction Stop
        $componentHash = (Get-FileHash -LiteralPath $componentPath -Algorithm SHA256).Hash
        if ([long]$componentItem.Length -ne [long]$expected.Length -or
            -not [string]::Equals($componentHash, [string]$expected.Sha256, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Committed optional component validation failed for $($expected.ComponentId)."
        }
    }
    $actualComponentFiles = if (Test-Path -LiteralPath $Transaction.LiveComponents -PathType Container) {
        @(Get-ChildItem -LiteralPath $Transaction.LiveComponents -File -Recurse | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
    }
    else { @() }
    if ((@($expectedComponentFiles.ToArray() | Sort-Object) -join '|') -ne ($actualComponentFiles -join '|')) {
        throw 'Committed optional component file set is not exact.'
    }
    $committedPlayerDoctorRecords = New-Object 'System.Collections.Generic.List[object]'
    foreach ($record in @($Transaction.StateToolRecords)) {
        $recordPath = [string](Get-DtmApiMapValue -Map $record -Key 'Path' -Default '')
        $recordKind = [string](Get-DtmApiMapValue -Map $record -Key 'Kind' -Default '')
        if (-not (Test-Path -LiteralPath $recordPath -PathType Leaf)) {
            throw "Committed DTMAPI state tool is missing: $recordPath"
        }

        $expectedHash = [string](Get-DtmApiMapValue -Map $record -Key 'Sha256' -Default '')
        if (-not [string]::IsNullOrWhiteSpace($expectedHash)) {
            $actualHash = (Get-FileHash -LiteralPath $recordPath -Algorithm SHA256).Hash
            if (-not [string]::Equals($actualHash, $expectedHash, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Committed DTMAPI state tool hash validation failed: $recordPath"
            }
        }

        if ($recordKind -eq 'player-doctor' -or $recordKind -eq 'player-doctor-license') {
            if ([string]::IsNullOrWhiteSpace($expectedHash)) {
                throw "Committed Player Doctor state record is missing its SHA-256 receipt: $recordPath"
            }
            $committedPlayerDoctorRecords.Add($record) | Out-Null
        }
    }
    $committedPlayerDoctorNames = @($committedPlayerDoctorRecords.ToArray() | ForEach-Object {
        [System.IO.Path]::GetFileName([string](Get-DtmApiMapValue -Map $_ -Key 'Path' -Default ''))
    } | Sort-Object)
    $expectedCommittedPlayerDoctorNames = @($playerDoctorFileNames | Sort-Object)
    if (($committedPlayerDoctorNames -join '|') -ne ($expectedCommittedPlayerDoctorNames -join '|')) {
        throw "Committed Player Doctor state records are not exact. Expected=$($expectedCommittedPlayerDoctorNames -join ',') Actual=$($committedPlayerDoctorNames -join ',')"
    }
    $committedPlayerDoctorRoot = Join-Path $Transaction.LiveTools 'player-doctor'
    $committedPlayerDoctorFiles = @(Get-ChildItem -LiteralPath $committedPlayerDoctorRoot -File -Recurse | ForEach-Object {
        $_.FullName.Substring(([System.IO.Path]::GetFullPath($committedPlayerDoctorRoot)).TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
    } | Sort-Object)
    if (($committedPlayerDoctorFiles -join '|') -ne ($expectedCommittedPlayerDoctorNames -join '|')) {
        throw "Committed Player Doctor file set is not exact. Expected=$($expectedCommittedPlayerDoctorNames -join ',') Actual=$($committedPlayerDoctorFiles -join ',')"
    }
    $committedPlayerDoctorExe = Join-Path $committedPlayerDoctorRoot 'dtmapi-player-doctor.exe'
    $committedPlayerDoctorVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($committedPlayerDoctorExe)
    if (-not [string]::Equals([string]$committedPlayerDoctorVersion.FileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$committedPlayerDoctorVersion.ProductVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal)) {
        throw "Committed Player Doctor version validation failed. Expected=$script:DtmApiReleaseVersion/$script:DtmApiBinaryVersion Actual=$($committedPlayerDoctorVersion.ProductVersion)/$($committedPlayerDoctorVersion.FileVersion)"
    }
    $release = Get-Content -Raw -Encoding UTF8 -LiteralPath $Transaction.LiveReleaseManifest | ConvertFrom-Json
    $state = Get-Content -Raw -Encoding UTF8 -LiteralPath $Transaction.LiveInstallState | ConvertFrom-Json
    if (-not [string]::Equals([string]$release.DTMAPIVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$state.DTMAPIVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal)) {
        throw 'Committed DTMAPI Runtime state version validation failed.'
    }
    foreach ($expected in @($Transaction.RuntimeMetadata)) {
        $projected = @($release.IncludedAssemblies | Where-Object { [string]$_.FileName -eq [string]$expected.FileName })
        if ($projected.Count -ne 1 -or
            -not [string]::Equals([string]$projected[0].Sha256, [string]$expected.Sha256, [System.StringComparison]::Ordinal)) {
            throw "Committed DTMAPI release manifest does not describe $($expected.FileName)."
        }
    }
    $releaseComponents = @((Get-DtmApiObjectProperty -Object $release -Name 'OptionalComponents' -Default @()))
    $stateComponents = @((Get-DtmApiObjectProperty -Object $state -Name 'OptionalComponents' -Default @()))
    if ($releaseComponents.Count -ne @($Transaction.OptionalComponentMetadata).Count -or
        $stateComponents.Count -ne @($Transaction.OptionalComponentMetadata).Count) {
        throw 'Committed DTMAPI optional-component receipt count mismatch.'
    }
    foreach ($expected in @($Transaction.OptionalComponentMetadata)) {
        $releaseRows = @($releaseComponents | Where-Object { [string]$_.ComponentId -eq [string]$expected.ComponentId })
        $stateRows = @($stateComponents | Where-Object { [string]$_.ComponentId -eq [string]$expected.ComponentId })
        if ($releaseRows.Count -ne 1 -or $stateRows.Count -ne 1 -or
            -not [string]::Equals([string]$releaseRows[0].Sha256, [string]$expected.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals([string]$stateRows[0].Sha256, [string]$expected.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals([string]$releaseRows[0].RelativePath, [string]$expected.RelativePath, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateRows[0].Path, [string]$expected.Path, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Committed DTMAPI receipts do not describe optional component $($expected.ComponentId)."
        }
    }
}

function Complete-DtmApiRuntimeInstallTransaction {
    param([Parameter(Mandatory = $true)] $Transaction)

    try {
        Assert-DtmApiGameNotRunning -GameDir $gameDir -Operation 'Runtime transaction commit'
        $Transaction.Phase = 'SwitchingRuntime'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        $Transaction.OldPluginExisted = Test-Path -LiteralPath $pluginDir -PathType Container
        if ($Transaction.OldPluginExisted) {
            $Transaction.Phase = 'MovingOldRuntime'
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            [System.IO.Directory]::Move($pluginDir, $Transaction.RecoveryPlugin)
            $Transaction.OldPluginMoved = $true
        }
        $Transaction.Phase = 'OldRuntimeMoved'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        Invoke-DtmApiRuntimeInstallFault -Phase 'OldRuntimeMoved'

        $Transaction.Phase = 'PlacingCandidate'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        $pluginParent = Split-Path -Parent $pluginDir
        if (-not (Test-Path -LiteralPath $pluginParent -PathType Container)) {
            New-Item -ItemType Directory -Force -Path $pluginParent | Out-Null
        }
        [System.IO.Directory]::Move($Transaction.CandidatePlugin, $pluginDir)
        $Transaction.CandidatePluginPlaced = $true
        $Transaction.Phase = 'CandidatePlaced'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        Invoke-DtmApiRuntimeInstallFault -Phase 'CandidatePlaced'

        New-Item -ItemType Directory -Force -Path $Transaction.RecoveryState | Out-Null
        $Transaction.OldComponentsExisted = Test-Path -LiteralPath $Transaction.LiveComponents -PathType Container
        if ($Transaction.OldComponentsExisted) {
            $Transaction.Phase = 'MovingOldComponents'
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            [System.IO.Directory]::Move($Transaction.LiveComponents, (Join-Path $Transaction.RecoveryState 'components'))
            Invoke-DtmApiRuntimeInstallFault -Phase 'MovingOldComponents'
            $Transaction.OldComponentsMoved = $true
        }
        $Transaction.Phase = 'PlacingComponents'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        [System.IO.Directory]::Move($Transaction.CandidateComponents, $Transaction.LiveComponents)
        Invoke-DtmApiRuntimeInstallFault -Phase 'PlacingComponents'
        $Transaction.CandidateComponentsPlaced = $true
        $Transaction.Phase = 'ComponentsCommitted'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction

        $Transaction.OldToolsExisted = Test-Path -LiteralPath $Transaction.LiveTools -PathType Container
        if ($Transaction.OldToolsExisted) {
            $Transaction.Phase = 'MovingOldTools'
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            [System.IO.Directory]::Move($Transaction.LiveTools, (Join-Path $Transaction.RecoveryState 'tools'))
            $Transaction.OldToolsMoved = $true
        }
        $Transaction.Phase = 'PlacingTools'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        [System.IO.Directory]::Move($Transaction.CandidateTools, $Transaction.LiveTools)
        $Transaction.CandidateToolsPlaced = $true
        $Transaction.Phase = 'ToolsCommitted'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        Invoke-DtmApiRuntimeInstallFault -Phase 'ToolsCommitted'

        $Transaction.OldReleaseManifestExisted = Test-Path -LiteralPath $Transaction.LiveReleaseManifest -PathType Leaf
        if ($Transaction.OldReleaseManifestExisted) {
            $Transaction.Phase = 'MovingOldReleaseManifest'
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            [System.IO.File]::Move($Transaction.LiveReleaseManifest, (Join-Path $Transaction.RecoveryState 'release-manifest.json'))
            $Transaction.OldReleaseManifestMoved = $true
        }
        $Transaction.Phase = 'PlacingReleaseManifest'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        [System.IO.File]::Move($Transaction.CandidateReleaseManifest, $Transaction.LiveReleaseManifest)
        $Transaction.CandidateReleaseManifestPlaced = $true
        $Transaction.Phase = 'ReleaseManifestCommitted'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        Invoke-DtmApiRuntimeInstallFault -Phase 'ReleaseManifestCommitted'

        $Transaction.OldInstallStateExisted = Test-Path -LiteralPath $Transaction.LiveInstallState -PathType Leaf
        if ($Transaction.OldInstallStateExisted) {
            $Transaction.Phase = 'MovingOldInstallState'
            Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
            [System.IO.File]::Move($Transaction.LiveInstallState, (Join-Path $Transaction.RecoveryState 'install-state.json'))
            $Transaction.OldInstallStateMoved = $true
        }
        $Transaction.Phase = 'PlacingInstallState'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        [System.IO.File]::Move($Transaction.CandidateInstallState, $Transaction.LiveInstallState)
        $Transaction.CandidateInstallStatePlaced = $true
        $Transaction.Phase = 'InstallStateCommitted'
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        Invoke-DtmApiRuntimeInstallFault -Phase 'InstallStateCommitted'

        Assert-DtmApiCommittedRuntimeInstall -Transaction $Transaction
        $Transaction.Phase = 'Committed'
        $Transaction.CommitSucceeded = $true
        $Transaction.RollbackSucceeded = $null
        Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction
        try {
            Remove-DtmApiRuntimeInstallTransactionArtifacts -Transaction $Transaction
        }
        catch {
            $cleanupError = $_
            $Transaction.Phase = 'CommittedCleanupPending'
            try { Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction } catch { }
            Write-Warning "DTMAPI Runtime was committed and validated, but transaction recovery cleanup is pending: $($cleanupError.Exception.Message)"
        }
    }
    catch {
        $originalError = $_
        if ([string]::IsNullOrWhiteSpace([string]$Transaction.FailedPhase)) {
            $Transaction.FailedPhase = [string]$Transaction.Phase
        }
        try { Write-DtmApiRuntimeTransactionReceipt -Transaction $Transaction } catch { }
        try {
            Restore-DtmApiRuntimeInstallTransaction -Transaction $Transaction
        }
        catch {
            throw "DTMAPI Runtime install failed at $($Transaction.FailedPhase), and rollback also failed. $($originalError.Exception.Message) Rollback: $($_.Exception.Message)"
        }
        throw $originalError
    }
}

function Test-DtmApiDefinitionFlag {
    param(
        $Mod,
        [Parameter(Mandatory = $true)] [string] $Key
    )

    return (Test-DtmApiMapKey -Map $Mod -Key $Key) -and [bool](Get-DtmApiMapValue -Map $Mod -Key $Key -Default $false)
}

function Select-DtmApiOfficialLocalModDefinitions {
    $definitions = @()
    if ($InstallPublishedModsOnly) {
        $definitions = @(Get-DtmApiPublishedModDefinitions)
    }
    else {
        $definitions = @(Get-DtmApiDeveloperOfficialModDefinitions)
        if (-not $InstallQaFixtures) {
            $definitions = @($definitions | Where-Object { -not (Test-DtmApiDefinitionFlag -Mod $_ -Key 'QaFixture') })
        }
    }

    if ($LegacyOfficialLocalOnly) {
        # This scope exists for tests and maintenance which explicitly exercise
        # the generic collision-failing directory publisher. Managed products
        # remain selected by every production/default scope and may only deploy
        # through their Author SDK authority below.
        $definitions = @($definitions | Where-Object { -not (Test-DtmApiDefinitionFlag -Mod $_ -Key 'AuthorSdkProject') })
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$env:DTMAPI_INSTALL_AUTHOR_SDK_ONLY_UNIQUE_ID)) {
        $testUniqueId = [string]$env:DTMAPI_INSTALL_AUTHOR_SDK_ONLY_UNIQUE_ID
        $definitions = @($definitions | Where-Object {
            (Test-DtmApiDefinitionFlag -Mod $_ -Key 'AuthorSdkProject') -and
            [string]::Equals([string]$_.UniqueID, $testUniqueId, [System.StringComparison]::Ordinal)
        })
        if ($definitions.Count -ne 1) {
            throw "Author SDK installer transaction test filter did not select exactly one managed product: $testUniqueId"
        }
    }

    return $definitions
}

function Get-DtmApiOfficialLocalInstallMode {
    $scope = if ($LegacyOfficialLocalOnly) { ' (explicit legacy official-local directory scope; managed Author SDK products excluded)' } else { '' }
    if ($InstallPublishedModsOnly) {
        return 'published release mods only' + $scope
    }

    if ($InstallQaFixtures) {
        return 'developer local official mods plus explicit QA fixtures' + $scope
    }

    return 'developer local official mods without QA fixtures' + $scope
}

$optionalComponentSourceDefinitions = @(Get-DtmApiOptionalComponentSourceDefinitions)
$packageOptionalComponentMetadata = @()
if ($usingPackagePayload) {
    $packageRuntimeMetadata = @(Get-DtmApiRuntimeAssemblyMetadata -Root $outDir -IntendedLiveRoot $pluginDir)
    Assert-DtmApiPackagedRuntimeManifestMatchesPayload `
        -Manifest $packageSourceReleaseManifest `
        -ManifestPath $packageSourceReleaseManifestPath `
        -ActualMetadata $packageRuntimeMetadata
    $packageOptionalComponentMetadata = @(Get-DtmApiOptionalComponentMetadata `
        -Definitions $optionalComponentSourceDefinitions `
        -IntendedLiveRoot $stateDir)
    $packageComponentsRoot = Join-Path $PackagePayloadRoot 'DTMAPI\components'
    $expectedPackageComponentPaths = @($packageOptionalComponentMetadata | ForEach-Object {
        ([string]$_.RelativePath).Substring('DTMAPI/'.Length).Replace('\', '/')
    } | Sort-Object)
    $actualPackageComponentPaths = if (Test-Path -LiteralPath $packageComponentsRoot -PathType Container) {
        $resolvedPackageComponentsRoot = [System.IO.Path]::GetFullPath($packageComponentsRoot).TrimEnd('\')
        @(Get-ChildItem -LiteralPath $packageComponentsRoot -File -Recurse | ForEach-Object {
            'components/' + $_.FullName.Substring($resolvedPackageComponentsRoot.Length).TrimStart('\').Replace('\', '/')
        } | Sort-Object)
    }
    else { @() }
    if (($expectedPackageComponentPaths -join '|') -ne ($actualPackageComponentPaths -join '|')) {
        throw "Packaged optional component file set is not exact. Expected=$($expectedPackageComponentPaths -join ',') Actual=$($actualPackageComponentPaths -join ',')"
    }
    $script:DtmInstallSourceCommit = $packageBuildCommit.Trim()
}

if ($DryRun) {
    $legacyDetections = @(Get-DtmApiLegacyDetections -GameDir $gameDir)
    $plannedOfficialMods = if ($SkipOfficialLocalMods) { @() } else { @(Select-DtmApiOfficialLocalModDefinitions) }
    $plannedQaFixtures = @($plannedOfficialMods | Where-Object { Test-DtmApiDefinitionFlag -Mod $_ -Key 'QaFixture' } | ForEach-Object {
        [ordered]@{
            OfficialFolder = $_.OfficialFolder
            UniqueID = $_.UniqueID
            PackageName = $_.PackageName
        }
    })
    $plannedOptionalComponents = @($optionalComponentSourceDefinitions | ForEach-Object {
        [ordered]@{
            ComponentId = [string]$_.ComponentId
            Distribution = [string]$_.Distribution
            LoadPolicy = [string]$_.LoadPolicy
            RelativePath = [string]$_.RelativePath
            AssemblyName = [string]$_.AssemblyName
            AssemblyVersion = [string]$_.ExpectedAssemblyVersion
            FileVersion = [string]$_.ExpectedFileVersion
            TargetFramework = [string]$_.TargetFramework
            DefaultLoadState = [string]$_.DefaultLoadState
            IncludedInDownloadPackage = [bool]$_.IncludedInDownloadPackage
            Status = 'planned'
        }
    })
    $plannedRelease = New-DtmApiReleaseManifest -RepoRoot $repo -PackageKind 'dry-run' -IncludedAssemblies $runtimeFiles -OptionalComponents $plannedOptionalComponents -BundledMods $plannedOfficialMods -BuildCommit $script:DtmInstallSourceCommit
    $plannedState = New-DtmApiInstallState -RepoRoot $repo -GameDir $gameDir -PluginDir $pluginDir -FilesInstalled $runtimeFiles -OptionalComponents $plannedOptionalComponents -BepInExDetectedBeforeInstall $bepInExDetectedBeforeInstall -BepInExInstalledByDTMAPI $false -BackupsCreated @() -LegacyModsMoved @() -LegacyDetections $legacyDetections -QaFixturesInstalled $plannedQaFixtures -DryRun $true -SourceRepoCommit $script:DtmInstallSourceCommit
    Write-Host "DRY RUN: would install DTMAPI $($plannedRelease.DTMAPIVersion) to $pluginDir"
    Write-Host "DRY RUN: would write release manifest to $(Join-Path $stateDir 'release-manifest.json')"
    Write-Host "DRY RUN: would write install state to $(Join-Path $stateDir 'install-state.json')"
    Write-Host "DRY RUN: detected legacy item count = $($legacyDetections.Count)"
    if (-not $SkipOfficialLocalMods) {
        Write-Host "DRY RUN: official local mod install mode = $(Get-DtmApiOfficialLocalInstallMode)"
        Write-Host "DRY RUN: official local mod count = $($plannedOfficialMods.Count)"
        Write-Host "DRY RUN: QA fixture install count = $($plannedQaFixtures.Count)"
    }
    $plannedState | ConvertTo-Json -Depth 12
    return
}

Assert-DtmApiGameNotRunning -GameDir $gameDir -Operation 'install'

$script:DtmRuntimeInstallTransaction = New-DtmApiRuntimeInstallTransaction
Initialize-DtmApiRuntimeInstallCandidate -Transaction $script:DtmRuntimeInstallTransaction

$bepInExInstalledByDTMAPI = $false
if (-not (Test-DtmApiBepInExInstallComplete -GameDir $gameDir)) {
    if ($InstallBepInEx) {
        & (Join-Path $installerToolSourceRoot 'install-bepinex.ps1') -Force:$bepInExDetectedBeforeInstall
        $bepInExInstalledByDTMAPI = (-not $bepInExCompleteBeforeInstall) -and (Test-DtmApiBepInExInstallComplete -GameDir $gameDir)
        if (-not (Test-DtmApiBepInExInstallComplete -GameDir $gameDir)) {
            throw "BepInEx installation did not produce all required files."
        }
    }
    else {
        Write-Warning "BepInEx is incomplete. The DTMAPI Runtime candidate is ready, but the game still needs BepInEx installed once."
    }
}

function Backup-GameModDirectory {
    param(
        [Parameter(Mandatory = $true)] [string] $ModsRoot,
        [Parameter(Mandatory = $true)] [string] $ModId,
        [Parameter(Mandatory = $true)] [string] $BackupRoot,
        [Parameter(Mandatory = $true)] [string] $Reason
    )

    $source = Join-Path $ModsRoot $ModId
    if (-not (Test-Path $source -PathType Container)) {
        return
    }

    $resolvedModsRoot = [System.IO.Path]::GetFullPath($ModsRoot).TrimEnd('\') + '\'
    $resolvedSource = [System.IO.Path]::GetFullPath($source)
    if (-not $resolvedSource.StartsWith($resolvedModsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to move $ModId because the resolved source is outside ModsRoot."
    }

    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
    $dest = Join-Path $BackupRoot $ModId
    Move-Item -LiteralPath $source -Destination $dest
    $record = [ordered]@{
        ModId = $ModId
        Source = [System.IO.Path]::GetFullPath($source)
        Destination = [System.IO.Path]::GetFullPath($dest)
        Reason = $Reason
    }
    $script:DtmInstallBackupsCreated.Add($record) | Out-Null
    $script:DtmInstallLegacyModsMoved.Add($record) | Out-Null
    Write-Host "Moved $ModId out of game Mods to $dest ($Reason)"
}

function Backup-LegacyMigratedGameMods {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [switch] $KeepLegacyMigratedGameMods
    )

    if ($KeepLegacyMigratedGameMods) {
        return
    }

    $modsRoot = Join-Path $GameDir 'Mods'
    $legacyIds = @(
        'Yuuka.DTMAPI.ActionSpeed',
        'Yuuka.DTMAPI.AutoFishing',
        'Yuuka.DTMAPI.OneActionComplete',
        'Yuuka.DTMAPI.FishBreedingAssistant',
        'Yuuka.DTMAPI.AnimalHusbandryProgress',
        'DTMAPI.DebugConsoleMod'
    )
    $existing = New-Object 'System.Collections.Generic.List[string]'
    foreach ($id in $legacyIds) {
        $candidate = Join-Path $modsRoot $id
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            continue
        }
        $authorReceiptPath = Join-Path $candidate '.dtmapi-author-receipt.json'
        if (Test-Path -LiteralPath $authorReceiptPath -PathType Leaf) {
            $authorReceipt = $null
            try { $authorReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath $authorReceiptPath | ConvertFrom-Json }
            catch { $authorReceipt = $null }
            if ($null -ne $authorReceipt -and
                [string]$authorReceipt.uniqueId -ceq $id -and
                [string]$authorReceipt.codeModKind -ceq 'Advanced' -and
                [string]$authorReceipt.destinationRelativePath -ceq ('Mods/' + $id)) {
                Write-Host "Retained $id in game Mods because its exact Author SDK deployment receipt owns the destination."
                continue
            }
        }
        $existing.Add($id) | Out-Null
    }
    if ($existing.Count -eq 0) {
        return
    }

    $backupRoot = Join-Path $GameDir ('DTMAPI\backups\official-local-migration-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    foreach ($id in $existing.ToArray()) {
        Backup-GameModDirectory -ModsRoot $modsRoot -ModId $id -BackupRoot $backupRoot -Reason 'official local package now owns enablement'
    }
}

function Backup-StaleHookProbe {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [switch] $IncludeHookProbe
    )

    if ($IncludeHookProbe) {
        return
    }

    $modsRoot = Join-Path $GameDir 'Mods'
    $backupRoot = Join-Path $GameDir ('DTMAPI\backups\disabled-testmods-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Backup-GameModDirectory -ModsRoot $modsRoot -ModId 'DTMAPI.HookProbeMod' -BackupRoot $backupRoot -Reason 'HookProbe is test-only and was not requested'
}

function Backup-StaleSampleMods {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [switch] $IncludeTestMods,
        [switch] $IncludeDebugConsoleMod
    )

    if ($IncludeTestMods) {
        return
    }

    $modsRoot = Join-Path $GameDir 'Mods'
    $backupRoot = Join-Path $GameDir ('DTMAPI\backups\disabled-testmods-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    foreach ($id in @('DTMAPI.HelloDtmMod', 'DTMAPI.ConfigMenuExample', 'DTMAPI.DebugConsoleMod')) {
        if ($id -eq 'DTMAPI.DebugConsoleMod' -and $IncludeDebugConsoleMod) {
            continue
        }
        Backup-GameModDirectory -ModsRoot $modsRoot -ModId $id -BackupRoot $backupRoot -Reason 'loose development copy was not requested'
    }
}

if ($IncludeTestMods -or $IncludeDebugConsoleMod) {
    $modsRoot = Join-Path $gameDir 'Mods'
    $testMods = @()
    if ($IncludeTestMods) {
        $testMods += @{ Id = 'DTMAPI.HelloDtmMod'; SourceRoot = 'author-sdk\examples\HelloDtm'; Dll = 'HelloDtmMod.dll' }
        $testMods += @{ Id = 'DTMAPI.ConfigMenuExample'; SourceRoot = 'author-sdk\examples\ConfigMenu'; Dll = 'ConfigMenuExample.dll' }
    }
    if ($IncludeDebugConsoleMod) {
        $testMods += @{ Id = 'DTMAPI.DebugConsoleMod'; SourceRoot = 'products\first-party\DebugConsole'; Dll = 'DebugConsoleMod.dll' }
    }
    if ($IncludeHookProbe) {
        $testMods += @{ Id = 'DTMAPI.HookProbeMod'; SourceRoot = 'tests\mod-fixtures\qa\HookProbe'; Dll = 'HookProbeMod.dll' }
    }
    foreach ($mod in $testMods) {
        $source = Join-Path $repo "$($mod.SourceRoot)\bin\$Configuration\netstandard2.0"
        $dest = Join-Path $modsRoot $mod.Id
        Copy-DirectoryContents -Source $source -Destination $dest -Include @($mod.Dll, 'manifest.json', 'i18n')
    }
}

if ($usingPackagePayload) {
    Write-Host "Package payload install detected; leaving existing game Mods folders untouched."
}
else {
    Backup-LegacyMigratedGameMods -GameDir $gameDir -KeepLegacyMigratedGameMods:$KeepLegacyMigratedGameMods
    Backup-StaleSampleMods -GameDir $gameDir -IncludeTestMods:$IncludeTestMods -IncludeDebugConsoleMod:$IncludeDebugConsoleMod
    Backup-StaleHookProbe -GameDir $gameDir -IncludeHookProbe:$IncludeHookProbe
}

function Get-DolocTownPersistentRoot {
    if ($env:DTMAPI_DOLOC_PERSISTENT_ROOT) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_DOLOC_PERSISTENT_ROOT)
    }

    return Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\RedSawGames\DolocTown'
}

function Write-JsonObject {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $json = $Value | ConvertTo-Json -Depth 10
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Write-JsonObjectAtomic {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $tempPath = $Path + ".tmp-" + [Guid]::NewGuid().ToString('N')
    try {
        Write-JsonObject -Path $tempPath -Value $Value
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            Move-Item -LiteralPath $tempPath -Destination $Path -Force
        }
        else {
            [System.IO.File]::Move($tempPath, $Path)
        }
    }
    finally {
        if (Test-Path -LiteralPath $tempPath) {
            Remove-Item -LiteralPath $tempPath -Force
        }
    }
}

function Get-DtmApiPackageTreeFingerprint {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return '<missing>'
    }

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $prefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
    $rows = New-Object 'System.Collections.Generic.List[string]'
    foreach ($directory in @(Get-ChildItem -LiteralPath $rootFull -Directory -Recurse -Force | Sort-Object FullName)) {
        $relative = [System.IO.Path]::GetFullPath($directory.FullName).Substring($prefix.Length).Replace('\', '/')
        $rows.Add('D|' + $relative) | Out-Null
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $rootFull -File -Recurse -Force | Sort-Object FullName)) {
        $relative = [System.IO.Path]::GetFullPath($file.FullName).Substring($prefix.Length).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $rows.Add(('F|{0}|{1}|{2}' -f $relative, $file.Length, $hash)) | Out-Null
    }

    return (@($rows.ToArray()) | Sort-Object) -join "`n"
}

function Read-OfficialLocalDtmApiEnablementState {
    $persistentRoot = Get-DolocTownPersistentRoot
    $enablementPath = Join-Path $persistentRoot 'SAVE\mod_infos.json'
    if (Test-Path -LiteralPath $enablementPath -PathType Leaf) {
        try {
            $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $enablementPath | ConvertFrom-Json
        }
        catch {
            throw "Could not read $enablementPath. Refusing to overwrite mod_infos.json. $($_.Exception.Message)"
        }
    }
    else {
        $data = [pscustomobject]@{ modInfos = [pscustomobject]@{} }
    }

    if ($null -eq $data -or $data -is [System.Array] -or $data -is [string] -or $data -is [ValueType]) {
        throw "Could not read $enablementPath. Refusing to overwrite mod_infos.json. The JSON root must be an object."
    }
    if ($null -eq $data.PSObject.Properties['modInfos'] -or $null -eq $data.modInfos) {
        $data | Add-Member -MemberType NoteProperty -Name 'modInfos' -Value ([pscustomobject]@{}) -Force
    }
    elseif ($data.modInfos -is [System.Array] -or $data.modInfos -is [string] -or $data.modInfos -is [ValueType]) {
        throw "Could not read $enablementPath. Refusing to overwrite mod_infos.json. The modInfos value must be an object."
    }

    return [pscustomobject]@{
        Path = $enablementPath
        Data = $data
    }
}

function Assert-OfficialLocalDtmApiEnablementPreflight {
    $attempt = [ordered]@{
        OfficialFolder = ''
        EnablementId = ''
        Phase = 'EnablementPreflight'
        PublishSucceeded = $false
        PublishRollback = 'NotRequired'
        DestructiveAuthority = 'None'
        Error = ''
    }
    $script:DtmInstallOfficialLocalAttempts.Add($attempt) | Out-Null
    try {
        $null = Read-OfficialLocalDtmApiEnablementState
        $attempt['Phase'] = 'EnablementPreflightCompleted'
    }
    catch {
        $attempt['Error'] = [string]$_.Exception.Message
        throw
    }
}

function Backup-ModInfosBeforeWrite {
    param([Parameter(Mandatory = $true)] [string] $EnablementPath)

    if (-not (Test-Path -LiteralPath $EnablementPath -PathType Leaf)) {
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($script:DtmInstallModInfosBackupPath)) {
        return
    }

    $backupRoot = Join-Path $stateDir ('backups\install-' + $script:DtmInstallStamp)
    New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
    $backupPath = Join-Path $backupRoot 'mod_infos.before.json'
    Copy-Item -LiteralPath $EnablementPath -Destination $backupPath -Force
    $script:DtmInstallModInfosBackupPath = [System.IO.Path]::GetFullPath($backupPath)
    $script:DtmInstallBackupsCreated.Add([ordered]@{
        Kind = 'mod-infos-before-install'
        Path = [System.IO.Path]::GetFullPath($EnablementPath)
        BackupPath = $script:DtmInstallModInfosBackupPath
        Reason = 'Before adding DTMAPI official-local enablement entries'
    }) | Out-Null
}

function Get-NumericPriorityOrNull {
    param($Value)

    if ($null -eq $Value) {
        return $null
    }

    try {
        return [int]$Value
    }
    catch {
        return $null
    }
}

function Resolve-DtmApiOfficialModSourceRoot {
    param(
        $Mod
    )

    $sourceRoot = Get-DtmApiMapValue -Map $Mod -Key 'SourceRoot' -Default ''
    if (-not [string]::IsNullOrWhiteSpace([string]$sourceRoot)) {
        return Join-Path $repo ([string]$sourceRoot)
    }
    throw "Official-local definition '$($Mod.UniqueID)' must declare SourceRoot after C1 physical classification."
}

function Invoke-DtmApiAuthorSdkInstallFault {
    param([Parameter(Mandatory = $true)] [string] $Phase)

    if ([string]::Equals([string]$env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal) -and
        [string]::Equals([string]$env:DTMAPI_AUTHOR_SDK_INSTALL_FAIL_PHASE, $Phase, [System.StringComparison]::Ordinal)) {
        throw "Injected Author SDK installer transaction failure at phase $Phase."
    }
}

function Get-DtmApiZipEntryBytes {
    param(
        [Parameter(Mandatory = $true)] [object] $Archive,
        [Parameter(Mandatory = $true)] [string] $EntryName
    )

    $matches = @($Archive.Entries | Where-Object {
        [string]::Equals([string]$_.FullName, $EntryName, [System.StringComparison]::Ordinal)
    })
    if ($matches.Count -ne 1) {
        throw "Author SDK package preflight requires exactly one case-exact '$EntryName' entry."
    }
    $stream = $matches[0].Open()
    $memory = New-Object System.IO.MemoryStream
    try {
        $stream.CopyTo($memory)
        return $memory.ToArray()
    }
    finally {
        $stream.Dispose()
        $memory.Dispose()
    }
}

function Read-DtmApiAuthorSdkCatalogPackagePreflight {
    param(
        [Parameter(Mandatory = $true)] [string] $PackagePath,
        [Parameter(Mandatory = $true)] [string] $ExpectedUniqueId,
        [Parameter(Mandatory = $true)] [string] $ExpectedVersion
    )

    if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
        throw "Author SDK package preflight source is missing: $PackagePath"
    }
    if (-not [string]::Equals([System.IO.Path]::GetExtension($PackagePath), '.zip', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Author SDK package preflight accepts only one .zip source: $PackagePath"
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
        $entryNames = @($archive.Entries | ForEach-Object { [string]$_.FullName })
        if ($archive.Entries.Count -gt 10000) {
            throw 'Author SDK package preflight rejected more than 10,000 ZIP entries.'
        }
        [long]$uncompressedBytes = 0
        foreach ($entry in $archive.Entries) {
            $uncompressedBytes += [long]$entry.Length
            if ($uncompressedBytes -gt 536870912) {
                throw 'Author SDK package preflight rejected more than 512 MiB of uncompressed ZIP data.'
            }
        }
        $caseInsensitiveNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($entryName in $entryNames) {
            if (-not $caseInsensitiveNames.Add($entryName)) {
                throw "Author SDK package preflight found a case-insensitive duplicate ZIP entry: $entryName"
            }
        }

        $manifestEntry = 'Content/DTMAPI/manifest.json'
        $infoEntry = 'info.json'
        $manifestBytes = Get-DtmApiZipEntryBytes -Archive $archive -EntryName $manifestEntry
        $infoBytes = Get-DtmApiZipEntryBytes -Archive $archive -EntryName $infoEntry
        $manifest = [System.Text.Encoding]::UTF8.GetString($manifestBytes) | ConvertFrom-Json
        $info = [System.Text.Encoding]::UTF8.GetString($infoBytes) | ConvertFrom-Json

        if ([string]$manifest.UniqueID -cne $ExpectedUniqueId -or
            [string]$manifest.Version -cne $ExpectedVersion -or
            [string]$manifest.Type -cne 'CodeMod' -or
            [string]$manifest.CodeModKind -cne 'Advanced' -or
            [string]$info.version -cne $ExpectedVersion) {
            throw "Author SDK package preflight identity/version does not match Catalog Advanced product $ExpectedUniqueId@$ExpectedVersion."
        }

        return [pscustomobject]@{
            UniqueId = [string]$manifest.UniqueID
            Version = [string]$manifest.Version
            PackageSha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
        }
    }
    catch {
        throw "Author SDK package preflight failed before any managed destination mutation. $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $archive) {
            $archive.Dispose()
        }
    }
}

function Install-OfficialLocalDtmApiMod {
    param(
        $Mod
    )

    if (Test-DtmApiDefinitionFlag -Mod $Mod -Key 'AuthorSdkProject') {
        $builderName = [string](Get-DtmApiMapValue -Map $Mod -Key 'AuthorSdkBuildScript' -Default '')
        if ([string]::IsNullOrWhiteSpace($builderName)) {
            throw "Author SDK product $($Mod.UniqueID) is missing its tracked build script."
        }
        $authorPackageName = [string](Get-DtmApiMapValue -Map $Mod -Key 'AuthorSdkPackageFile' -Default '')
        if ([string]::IsNullOrWhiteSpace($authorPackageName) -or [IO.Path]::GetFileName($authorPackageName) -cne $authorPackageName) {
            throw "Author SDK product $($Mod.UniqueID) is missing one safe tracked package filename."
        }
        $builderCatalogId = [string](Get-DtmApiMapValue -Map $Mod -Key 'AuthorSdkCatalogId' -Default '')
        if ([string]::IsNullOrWhiteSpace($builderCatalogId)) {
            throw "Author SDK product $($Mod.UniqueID) is missing its Catalog-driven build id."
        }
        $catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
        $catalogRows = @((Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json).products | Where-Object {
            [string]$_.catalogId -ceq $builderCatalogId
        })
        if ($catalogRows.Count -ne 1 -or
            [string]$catalogRows[0].uniqueId -cne [string]$Mod.UniqueID -or
            [string]$catalogRows[0].codeModKind -cne 'Advanced' -or
            [string]::IsNullOrWhiteSpace([string]$catalogRows[0].sourceVersion)) {
            throw "Author SDK product $($Mod.UniqueID) has no exact Advanced Catalog identity/version row."
        }
        $expectedProductVersion = if (-not [string]::IsNullOrWhiteSpace([string]$env:DTMAPI_INSTALL_AUTHOR_SDK_EXPECTED_VERSION)) {
            [string]$env:DTMAPI_INSTALL_AUTHOR_SDK_EXPECTED_VERSION
        }
        else {
            [string]$catalogRows[0].sourceVersion
        }
        $artifactKey = ([string]$Mod.UniqueID).ToLowerInvariant() -replace '[^a-z0-9]+', '-'
        $authorOutput = if (-not [string]::IsNullOrWhiteSpace([string]$env:DTMAPI_INSTALL_AUTHOR_SDK_ARTIFACT_ROOT)) {
            [System.IO.Path]::GetFullPath([string]$env:DTMAPI_INSTALL_AUTHOR_SDK_ARTIFACT_ROOT)
        }
        else {
            Join-Path $repo ("temp\install-$artifactKey-author-sdk-package")
        }
        $authorPackage = Join-Path $authorOutput $authorPackageName
        $authorSdkExe = Join-Path $authorOutput 'author-sdk\DTMAPI-Author-SDK-0.1.0-win-x64\dtmapi-author.exe'
        if (-not $SkipBuild) {
            & (Join-Path $PSScriptRoot $builderName) -CatalogId $builderCatalogId -Configuration $Configuration -GameDir $GameDir -OutputRoot $authorOutput
            if ($LASTEXITCODE -ne 0) {
                throw "Author SDK package build failed for $($Mod.UniqueID)."
            }
        }
        if (-not (Test-Path -LiteralPath $authorPackage -PathType Leaf) -or
            -not (Test-Path -LiteralPath $authorSdkExe -PathType Leaf)) {
            throw "Author SDK package/deployment authority is unavailable for $($Mod.UniqueID): $authorOutput"
        }

        $attempt = [ordered]@{
            OfficialFolder = $Mod.OfficialFolder
            EnablementId = $Mod.UniqueID
            Phase = 'AuthorSdkPreflight'
            PublishSucceeded = $false
            PublishRollback = 'NotRequiredPreflightNoMutation'
            DestructiveAuthority = 'ExactAuthorSdkDeploymentReceiptOnly'
            Destination = ''
            Error = ''
        }
        $script:DtmInstallOfficialLocalAttempts.Add($attempt) | Out-Null
        try {
            # This installer owns only Catalog identity/version projection. The
            # Author SDK owns the immutable package copy, deep receipt/marker/
            # entry/native validation, deployment, source selection and exact
            # rollback under one game-root lock.
            $packagePreflight = Read-DtmApiAuthorSdkCatalogPackagePreflight -PackagePath $authorPackage `
                -ExpectedUniqueId ([string]$Mod.UniqueID) -ExpectedVersion $expectedProductVersion
            Invoke-DtmApiAuthorSdkInstallFault -Phase 'AfterPackagePreflight'
        }
        catch {
            $attempt['Error'] = [string]$_.Exception.Message
            throw
        }
        $attempt['Phase'] = 'AuthorSdkAtomicLocalInstall'
        $attempt['PackagePreflightSha256'] = [string]$packagePreflight.PackageSha256
        $destinationPath = Join-Path (Join-Path $GameDir 'Mods') ([string]$Mod.UniqueID)
        try {
            $deploy = $null
            $deployExit = -1
            $deployText = ''
            $deployLines = @(& $authorSdkExe install-local $authorPackage `
                --game-root $GameDir `
                --expected-unique-id ([string]$Mod.UniqueID) `
                --expected-version $expectedProductVersion `
                --expected-package-sha256 ([string]$packagePreflight.PackageSha256) `
                --json 2>&1)
            $deployExit = $LASTEXITCODE
            $deployText = [string]::Join([Environment]::NewLine, @($deployLines | ForEach-Object { [string]$_ }))
            if ([string]::Equals([string]$env:DTMAPI_INSTALL_AUTHOR_SDK_CORRUPT_REPORT, '1', [System.StringComparison]::Ordinal)) {
                $deployText = '{test-corrupt-author-sdk-report'
            }
            try {
                $deploy = $deployText | ConvertFrom-Json
            }
            catch {
                # Never replay a mutating command after a lost or corrupt
                # response. Reconcile only through the SDK's read-only journal
                # plus source-state authority, and fail closed if that proof is
                # unavailable.
                $statusLines = @(& $authorSdkExe install-local-status ([string]$Mod.UniqueID) `
                    --game-root $GameDir `
                    --expected-version $expectedProductVersion `
                    --expected-package-sha256 ([string]$packagePreflight.PackageSha256) `
                    --json 2>&1)
                $statusExit = $LASTEXITCODE
                $statusText = [string]::Join([Environment]::NewLine, @($statusLines | ForEach-Object { [string]$_ }))
                try {
                    $deploy = $statusText | ConvertFrom-Json
                }
                catch {
                    $attempt['Error'] = $deployText
                    throw "Author SDK install-local report was unreadable and read-only reconciliation was also unreadable for $($Mod.UniqueID). Mutation was not replayed. Reconciliation: $statusText"
                }
                if ($statusExit -ne 0 -or -not [bool]$deploy.success) {
                    $attempt['Error'] = $deployText
                    throw "Author SDK install-local result is unknown and read-only reconciliation did not prove a commit for $($Mod.UniqueID). Mutation was not replayed. Reconciliation: $statusText"
                }
                $statusProperty = $deploy.values.PSObject.Properties['status']
                $deploymentTreeProperty = $deploy.PSObject.Properties['sha256']
                $sourceTreeProperty = $deploy.values.PSObject.Properties['sourceTreeSha256']
                $reconciliationStatus = if ($null -ne $statusProperty) {
                    [string]$statusProperty.Value
                }
                else {
                    ''
                }
                $deploymentTreeSha256 = if ($null -ne $deploymentTreeProperty) {
                    [string]$deploymentTreeProperty.Value
                }
                else {
                    ''
                }
                $sourceTreeSha256 = if ($null -ne $sourceTreeProperty) {
                    [string]$sourceTreeProperty.Value
                }
                else {
                    ''
                }
                $exactCommitProven =
                    [string]::Equals(
                        $reconciliationStatus,
                        'CommittedLocalDevelopment',
                        [System.StringComparison]::Ordinal) -and
                    $deploymentTreeSha256 -match '^[0-9a-fA-F]{64}$' -and
                    $sourceTreeSha256 -match '^[0-9a-fA-F]{64}$'
                if (-not $exactCommitProven) {
                    $attempt['Error'] = $deployText
                    if ([string]::Equals(
                        $reconciliationStatus,
                        'RecoveryRequired',
                        [System.StringComparison]::Ordinal)) {
                        throw "Author SDK install-local was interrupted for $($Mod.UniqueID). Read-only reconciliation returned RecoveryRequired, not CommittedLocalDevelopment. Run the exact SDK recover command before retrying installation; the product was not registered as installed. Mutation was not replayed. Reconciliation: $statusText"
                    }
                    throw "Author SDK install-local result is unknown and read-only reconciliation did not prove exact CommittedLocalDevelopment plus deployment/source tree digests for $($Mod.UniqueID). Mutation was not replayed. Reconciliation: $statusText"
                }
                $deploy.values | Add-Member -NotePropertyName operation -NotePropertyValue 'reconciled' -Force
                $deploy.values | Add-Member -NotePropertyName rollback -NotePropertyValue 'reconciled-authority-no-replay' -Force
                $deployExit = 0
                $attempt['ReadOnlyReconciliation'] = 'CommittedLocalDevelopment'
            }
            if ($null -eq $deploy -or $deployExit -ne 0 -or -not [bool]$deploy.success) {
                $attempt['Error'] = $deployText
                $attempt['PublishRollback'] = if ($null -ne $deploy -and $null -ne $deploy.values.PSObject.Properties['rollback']) {
                    [string]$deploy.values.rollback
                }
                else {
                    'UnknownAuthorSdkResult'
                }
                throw "Author SDK install-local failed for $($Mod.UniqueID) with exit $deployExit. Report: $deployText"
            }
            $attempt['PublishRollback'] = [string]$deploy.values.rollback
        }
        catch {
            $attempt['Error'] = [string]$_.Exception.Message
            throw
        }

        $operation = [string]$deploy.values.operation
        $attempt['Phase'] = 'AuthorSdkAtomicLocalInstallCommitted'
        $attempt['PublishSucceeded'] = $true
        $attempt['Destination'] = $destinationPath
        $script:DtmInstallBundledMods.Add([ordered]@{
            Id = $Mod.UniqueID
            Name = $Mod.DisplayName
            Version = $expectedProductVersion
            Path = $destinationPath
            Source = 'AuthorSdkAtomicLocalInstall'
        }) | Out-Null
        $script:DtmInstallFilesInstalled.Add([ordered]@{
            Kind = 'managed-advanced-codemod'
            Path = $destinationPath
            UniqueID = $Mod.UniqueID
            PackageSha256 = [string]$packagePreflight.PackageSha256
            PackagePreflightSha256 = [string]$packagePreflight.PackageSha256
            DeploymentTreeSha256 = [string]$deploy.sha256
            SourceTreeSha256 = [string]$deploy.values.sourceTreeSha256
        }) | Out-Null
        Write-Host "Installed $($Mod.UniqueID) through Author SDK atomic $operation plus exact local-source selection at $destinationPath"
        return
    }

    $persistentRoot = Get-DolocTownPersistentRoot
    $officialModsRoot = Join-Path $persistentRoot 'MODS'
    New-Item -ItemType Directory -Force -Path $officialModsRoot | Out-Null

    $dest = Join-Path $officialModsRoot $Mod.OfficialFolder
    if (Test-Path -LiteralPath $dest) {
        Write-Warning "Skipping $($Mod.OfficialFolder): an official-local destination already exists. Legacy package metadata is not an installer receipt, so this installer never overwrites the directory. Existing content was left untouched."
        return
    }

    $modSourceRoot = Resolve-DtmApiOfficialModSourceRoot -Mod $Mod
    $contentOnly = Test-DtmApiDefinitionFlag -Mod $Mod -Key 'ContentOnly'
    $source = ''
    $sourceDllPath = ''
    if ($contentOnly) {
        $sourceManifestPath = Join-Path $modSourceRoot 'manifest.json'
        if (-not (Test-Path -LiteralPath $sourceManifestPath -PathType Leaf)) {
            Write-Warning "Skipping $($Mod.OfficialFolder): content-only manifest was not found at $sourceManifestPath."
            return
        }
    }
    else {
        $source = Join-Path $modSourceRoot "bin\$Configuration\netstandard2.0"
        $sourceManifestPath = Join-Path $source 'manifest.json'
        $sourceDllPath = Join-Path $source $Mod.SourceDll
        if (-not (Test-Path -LiteralPath $sourceManifestPath -PathType Leaf) -or -not (Test-Path -LiteralPath $sourceDllPath -PathType Leaf)) {
            Write-Warning "Skipping $($Mod.OfficialFolder): built mod output was not found at $source."
            return
        }
    }

    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourceManifestPath | ConvertFrom-Json
    if ($contentOnly) {
        $manifestType = [string](Get-DtmApiMapValue -Map $manifest -Key 'Type' -Default '')
        if (-not [string]::Equals($manifestType, 'ContentPack', [System.StringComparison]::Ordinal)) {
            throw "Content-only official-local package $($Mod.OfficialFolder) must declare manifest Type=ContentPack."
        }
        foreach ($forbiddenField in @('EntryDll', 'MinimumDTMApiVersion', 'Dependencies')) {
            if (Test-DtmApiMapKey -Map $manifest -Key $forbiddenField) {
                throw "Content-only official-local package $($Mod.OfficialFolder) must not declare manifest field $forbiddenField."
            }
        }
        $unexpectedSourceDlls = @(Get-ChildItem -LiteralPath $modSourceRoot -Recurse -File -Filter '*.dll' -ErrorAction Stop)
        if ($unexpectedSourceDlls.Count -gt 0) {
            $relativeDlls = @($unexpectedSourceDlls | ForEach-Object { $_.FullName.Substring($modSourceRoot.Length).TrimStart('\', '/') })
            throw "Content-only official-local package $($Mod.OfficialFolder) must not contain DLL files anywhere under its source root: $([string]::Join(', ', $relativeDlls))"
        }
        $contentSourceRoot = Join-Path $modSourceRoot 'Content'
        $nestedSourceManifests = @()
        if (Test-Path -LiteralPath $contentSourceRoot -PathType Container) {
            $nestedSourceManifests = @(Get-ChildItem -LiteralPath $contentSourceRoot -Recurse -File -Filter 'manifest.json' -ErrorAction Stop)
        }
        if ($nestedSourceManifests.Count -gt 0) {
            $relativeManifests = @($nestedSourceManifests | ForEach-Object { $_.FullName.Substring($modSourceRoot.Length).TrimStart('\', '/') })
            throw "Content-only official-local package $($Mod.OfficialFolder) must use only its top-level source manifest; nested Content manifests are forbidden: $([string]::Join(', ', $relativeManifests))"
        }
    }
    else {
        $manifest.EntryDll = "Content/DTMAPI/$($Mod.PackageDll)"
    }

    $officialInfoPath = Join-Path $modSourceRoot 'official-info.json'
    if (-not (Test-Path $officialInfoPath)) {
        Write-Warning "Skipping $($Mod.OfficialFolder): official-info.json was not found."
        return
    }

    $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $officialInfoPath | ConvertFrom-Json
    $developerOnly = Test-DtmApiDefinitionFlag -Mod $Mod -Key 'DeveloperOnly'
    if ((-not $developerOnly) -or $contentOnly) {
        Assert-DtmApiCurrentProductProjection `
            -RepoRoot $repo `
            -Definition $Mod `
            -Manifest $manifest `
            -Info $info `
            -Context "Official-local package $($Mod.OfficialFolder)"
    }
    elseif (-not $info.PSObject.Properties['version']) {
        $info | Add-Member -NotePropertyName version -NotePropertyValue $manifest.Version
    }
    elseif ([string]::IsNullOrWhiteSpace([string]$info.version)) {
        $info.version = $manifest.Version
    }

    $attempt = [ordered]@{
        OfficialFolder = [string]$Mod.OfficialFolder
        EnablementId = 'Local.' + [string]$Mod.OfficialFolder
        PackagePath = [System.IO.Path]::GetFullPath($dest)
        Phase = 'PreparePackage'
        PublishSucceeded = $false
        PublishRollback = 'NotRequired'
        PackagePresentAfterFailure = $false
        PreservedRecoveryPath = ''
        RollbackBasis = 'TransientCurrentInvocationPublicationOnly'
        DestructiveAuthority = 'None'
        Error = ''
    }
    $script:DtmInstallOfficialLocalAttempts.Add($attempt) | Out-Null

    # Prepare a complete package outside the native MODS scan root but on the same
    # persistent volume, then publish it with Directory.Move. The move fails instead
    # of merging if another process creates the destination after the initial check.
    $stagingPath = Join-Path $persistentRoot ('.dtmapi-staging-' + [Guid]::NewGuid().ToString('N'))
    $stagedContentRoot = Join-Path $stagingPath 'Content\DTMAPI'
    $published = $false
    $stagingCleanupAllowed = $true
    [System.IO.Directory]::CreateDirectory($stagedContentRoot) | Out-Null

    try {
        if (-not $contentOnly) {
            Copy-Item -Force -LiteralPath $sourceDllPath -Destination (Join-Path $stagedContentRoot $Mod.PackageDll)
        }

        $i18nSource = Join-Path $modSourceRoot 'i18n'
        if (Test-Path $i18nSource) {
            Copy-DirectoryContents -Source (Split-Path -Parent $i18nSource) -Destination $stagingPath -Include @('i18n')
        }

        $contentSource = Join-Path $modSourceRoot 'Content'
        if (Test-Path $contentSource) {
            Copy-DirectoryContents -Source (Split-Path -Parent $contentSource) -Destination $stagingPath -Include @('Content')
        }

        if (-not $contentOnly) {
            $assetsSource = Join-Path $source 'assets'
            if (Test-Path $assetsSource) {
                Copy-DirectoryContents -Source (Split-Path -Parent $assetsSource) -Destination $stagedContentRoot -Include @('assets')
            }
        }

        # Write the validated canonical manifest after copying source content so a
        # nested source file can never replace the manifest which was inspected.
        $stagedManifestPath = Join-Path $stagedContentRoot 'manifest.json'
        Write-JsonObject -Path $stagedManifestPath -Value $manifest
        if ($contentOnly) {
            $unexpectedStagedDlls = @(Get-ChildItem -LiteralPath $stagingPath -Recurse -File -Filter '*.dll' -ErrorAction Stop)
            if ($unexpectedStagedDlls.Count -gt 0) {
                $stagedDllPaths = @($unexpectedStagedDlls | ForEach-Object { $_.FullName.Substring($stagingPath.Length).TrimStart('\', '/') })
                throw "Content-only official-local package $($Mod.OfficialFolder) staged unexpected DLL files: $([string]::Join(', ', $stagedDllPaths))"
            }
            $stagedManifests = @(Get-ChildItem -LiteralPath $stagingPath -Recurse -File -Filter 'manifest.json' -ErrorAction Stop)
            if ($stagedManifests.Count -ne 1 -or -not [string]::Equals([System.IO.Path]::GetFullPath($stagedManifests[0].FullName), [System.IO.Path]::GetFullPath($stagedManifestPath), [System.StringComparison]::OrdinalIgnoreCase)) {
                $stagedManifestPaths = @($stagedManifests | ForEach-Object { $_.FullName.Substring($stagingPath.Length).TrimStart('\', '/') })
                throw "Content-only official-local package $($Mod.OfficialFolder) must stage exactly one canonical Content/DTMAPI/manifest.json. Found: $([string]::Join(', ', $stagedManifestPaths))"
            }
        }

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

        if (Test-Path $assetIcon) {
            Copy-Item -Force -LiteralPath $assetIcon -Destination (Join-Path $stagingPath 'icon.png')
        }
        if (Test-Path $assetPreview) {
            Copy-Item -Force -LiteralPath $assetPreview -Destination (Join-Path $stagingPath 'preview.png')
        }

        Write-JsonObject -Path (Join-Path $stagingPath 'info.json') -Value $info
        $preparedPackageFingerprint = Get-DtmApiPackageTreeFingerprint -Root $stagingPath
        $attempt['Phase'] = 'PublishPackage'

        try {
            [System.IO.Directory]::Move($stagingPath, $dest)
        }
        catch {
            if (Test-Path -LiteralPath $dest) {
                $attempt['Phase'] = 'PublishCollision'
                Write-Warning "Skipping $($Mod.OfficialFolder): the official-local destination appeared while the package was being prepared. No existing content or enablement state was changed."
                return
            }
            throw
        }
        $published = $true
        $stagingCleanupAllowed = $false
        $attempt['PublishSucceeded'] = $true
        $attempt['PackagePresentAfterFailure'] = $true
        $attempt['PublishRollback'] = 'PendingEnablementCommit'
        $attempt['Phase'] = 'EnablementCommit'
        try {
            $enablementAction = Ensure-OfficialLocalDtmApiEnablement -OfficialFolder $Mod.OfficialFolder -Info $info
        }
        catch {
            $enablementError = [string]$_.Exception.Message
            $attempt['Error'] = $enablementError
            try {
                $publishedPackageFingerprint = Get-DtmApiPackageTreeFingerprint -Root $dest
                if ([string]::Equals($preparedPackageFingerprint, $publishedPackageFingerprint, [System.StringComparison]::Ordinal)) {
                    if ([string]::Equals($env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE, '1', [System.StringComparison]::Ordinal)) {
                        $testPauseMilliseconds = 0
                        if ([int]::TryParse([string]$env:DTMAPI_TEST_ROLLBACK_POST_FINGERPRINT_PAUSE_MS, [ref]$testPauseMilliseconds) -and
                            $testPauseMilliseconds -gt 0 -and $testPauseMilliseconds -le 5000) {
                            $testSignalPath = Join-Path $persistentRoot ('.dtmapi-transaction-test-' + [string]$Mod.OfficialFolder + '-post-fingerprint.signal')
                            [System.IO.File]::WriteAllText($testSignalPath, 'ready', (New-Object System.Text.UTF8Encoding($false)))
                            Start-Sleep -Milliseconds $testPauseMilliseconds
                        }
                    }
                    [System.IO.Directory]::Move($dest, $stagingPath)
                    $published = $false
                    $postMoveFingerprint = Get-DtmApiPackageTreeFingerprint -Root $stagingPath
                    if ([string]::Equals($preparedPackageFingerprint, $postMoveFingerprint, [System.StringComparison]::Ordinal)) {
                        # The second fingerprint closes the compare/move gap. Only
                        # bytes still matching this invocation's prepared package
                        # may be deleted; the generic finally cleanup is disabled
                        # once publication has occurred.
                        Remove-Item -LiteralPath $stagingPath -Recurse -Force
                        $attempt['PublishRollback'] = 'Completed'
                        $attempt['PackagePresentAfterFailure'] = $false
                    }
                    else {
                        try {
                            [System.IO.Directory]::Move($stagingPath, $dest)
                            $published = $true
                            $attempt['PublishRollback'] = 'RollbackRefusedUnknownChanges'
                            $attempt['PackagePresentAfterFailure'] = $true
                        }
                        catch {
                            $attempt['PublishRollback'] = 'RollbackFailedPreservedRecovery'
                            $attempt['PackagePresentAfterFailure'] = $false
                            $attempt['PreservedRecoveryPath'] = [System.IO.Path]::GetFullPath($stagingPath)
                            $attempt['Error'] = $enablementError + ' Post-move fingerprint changed and restoring the published path failed: ' + [string]$_.Exception.Message
                        }
                    }
                }
                else {
                    $attempt['PublishRollback'] = 'RollbackRefusedUnknownChanges'
                }
            }
            catch {
                if (Test-Path -LiteralPath $stagingPath -PathType Container) {
                    $attempt['PublishRollback'] = 'RollbackFailedPreservedRecovery'
                    $attempt['PackagePresentAfterFailure'] = $false
                    $attempt['PreservedRecoveryPath'] = [System.IO.Path]::GetFullPath($stagingPath)
                }
                else {
                    $attempt['PublishRollback'] = 'RollbackFailed'
                }
                $attempt['Error'] = $enablementError + ' Rollback error: ' + [string]$_.Exception.Message
            }

            throw "Official-local enablement commit failed for $($Mod.OfficialFolder). PublishRollback=$($attempt['PublishRollback']). $($attempt['Error'])"
        }
        $attempt['PublishRollback'] = 'NotRequired'
        $attempt['Phase'] = 'Completed'
        try {
            Write-Host "Official local enablement $enablementAction for Local.$($Mod.OfficialFolder)"
        }
        catch {
            # Enablement and package publication are already committed. Console
            # output is deliberately outside the correctness transaction.
        }
    }
    finally {
        if ($stagingCleanupAllowed -and -not $published -and (Test-Path -LiteralPath $stagingPath)) {
            Remove-Item -LiteralPath $stagingPath -Recurse -Force
        }
    }

    $contentRoot = Join-Path $dest 'Content\DTMAPI'
    if (-not $contentOnly) {
        $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'official-local-dll'; Path = [System.IO.Path]::GetFullPath((Join-Path $contentRoot $Mod.PackageDll)) }) | Out-Null
    }
    $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'official-local-manifest'; Path = [System.IO.Path]::GetFullPath((Join-Path $contentRoot 'manifest.json')) }) | Out-Null
    $script:DtmInstallFilesInstalled.Add([ordered]@{ Kind = 'official-local-info'; Path = [System.IO.Path]::GetFullPath((Join-Path $dest 'info.json')) }) | Out-Null

    $script:DtmInstallBundledMods.Add([ordered]@{
        OfficialFolder = $Mod.OfficialFolder
        UniqueID = $manifest.UniqueID
        Version = $manifest.Version
        PackageDll = if ($contentOnly) { $null } else { $Mod.PackageDll }
        ContentOnly = $contentOnly
        Path = [System.IO.Path]::GetFullPath($dest)
    }) | Out-Null
    if (Test-DtmApiDefinitionFlag -Mod $Mod -Key 'QaFixture') {
        $script:DtmInstallQaFixturesInstalled.Add([ordered]@{
            OfficialFolder = $Mod.OfficialFolder
            UniqueID = $manifest.UniqueID
            Version = $manifest.Version
            PackageDll = if ($contentOnly) { $null } else { $Mod.PackageDll }
            ContentOnly = $contentOnly
            Path = [System.IO.Path]::GetFullPath($dest)
        }) | Out-Null
    }
    Write-Host "Installed official local DTMAPI mod package to $dest"
}

function Ensure-OfficialLocalDtmApiEnablement {
    param(
        [Parameter(Mandatory = $true)] [string] $OfficialFolder,
        [Parameter(Mandatory = $true)] $Info
    )

    $enablementState = Read-OfficialLocalDtmApiEnablementState
    $enablementPath = [string]$enablementState.Path
    $data = $enablementState.Data
    $saveRoot = Split-Path -Parent $enablementPath
    New-Item -ItemType Directory -Force -Path $saveRoot | Out-Null

    $id = "Local.$OfficialFolder"
    $title = $null
    if ($Info.PSObject.Properties['title']) {
        $title = $Info.title
    }
    if (-not $title -and $Info.PSObject.Properties['name']) {
        $title = $Info.name
    }
    if ($Info.PSObject.Properties['localized_name'] -and $Info.localized_name.PSObject.Properties['schinese']) {
        $title = $Info.localized_name.schinese
    }
    if (-not $title) {
        $title = $OfficialFolder
    }

    if ($data.modInfos.PSObject.Properties[$id]) {
        $existing = $data.modInfos.PSObject.Properties[$id].Value
        if (-not $existing.PSObject.Properties['title']) {
            Backup-ModInfosBeforeWrite -EnablementPath $enablementPath
            $existing | Add-Member -MemberType NoteProperty -Name 'title' -Value $title
            Write-JsonObjectAtomic -Path $enablementPath -Value $data
            return 'updated-title'
        }

        if ([string]$existing.title -ne [string]$title) {
            Backup-ModInfosBeforeWrite -EnablementPath $enablementPath
            $existing.title = $title
            Write-JsonObjectAtomic -Path $enablementPath -Value $data
            return 'updated-title'
        }
        return 'unchanged'
    }

    $priority = 0
    foreach ($property in $data.modInfos.PSObject.Properties) {
        $value = $property.Value
        if ($value -and $value.PSObject.Properties['enabled'] -and [bool]$value.enabled -and $value.PSObject.Properties['priority']) {
            $existingPriority = Get-NumericPriorityOrNull -Value $value.priority
            if ($null -ne $existingPriority) {
                $priority = [Math]::Max($priority, $existingPriority + 1)
            }
        }
    }

    $entry = [ordered]@{
        id = $id
        enabled = $true
        priority = $priority
        source = 'Local'
        title = $title
    }
    Backup-ModInfosBeforeWrite -EnablementPath $enablementPath
    $data.modInfos | Add-Member -MemberType NoteProperty -Name $id -Value ([pscustomobject]$entry)
    Write-JsonObjectAtomic -Path $enablementPath -Value $data
    return 'added'
}

$releaseManifestPath = Join-Path $stateDir 'release-manifest.json'
$releaseManifestRecord = [ordered]@{ Kind = 'release-manifest'; Path = [System.IO.Path]::GetFullPath($releaseManifestPath) }
$installStatePath = Join-Path $stateDir 'install-state.json'

function New-DtmApiInstallReceiptProjection {
    param([switch] $IncludePlannedRuntime)

    $filesInstalled = New-Object 'System.Collections.Generic.List[object]'
    foreach ($record in @($script:DtmInstallFilesInstalled.ToArray())) {
        $filesInstalled.Add($record) | Out-Null
    }
    if ($IncludePlannedRuntime) {
        foreach ($record in @($script:DtmRuntimeInstallTransaction.RuntimeFileRecords)) {
            $filesInstalled.Add($record) | Out-Null
        }
        if ($null -ne $script:DtmRuntimeInstallTransaction.AssetRecord) {
            $filesInstalled.Add($script:DtmRuntimeInstallTransaction.AssetRecord) | Out-Null
        }
        foreach ($record in @($script:DtmRuntimeInstallTransaction.StateToolRecords)) {
            $filesInstalled.Add($record) | Out-Null
        }
        foreach ($record in @($script:DtmRuntimeInstallTransaction.OptionalComponentFileRecords)) {
            $filesInstalled.Add($record) | Out-Null
        }
        $filesInstalled.Add($releaseManifestRecord) | Out-Null
    }

    $bundledMods = $script:DtmInstallBundledMods.ToArray()
    $qaFixturesInstalled = $script:DtmInstallQaFixturesInstalled.ToArray()
    $installPackageKind = if ($usingPackagePayload) { 'workshop-runtime-install' } else { 'local-install' }
    $releaseManifest = New-DtmApiReleaseManifest `
        -RepoRoot $repo `
        -PackageKind $installPackageKind `
        -IncludedAssemblies @($script:DtmRuntimeInstallTransaction.RuntimeMetadata) `
        -OptionalComponents @($script:DtmRuntimeInstallTransaction.OptionalComponentMetadata) `
        -BundledMods $bundledMods `
        -BuildCommit $script:DtmInstallSourceCommit
    $installState = New-DtmApiInstallState `
        -RepoRoot $repo `
        -GameDir $gameDir `
        -PluginDir $pluginDir `
        -FilesInstalled $filesInstalled.ToArray() `
        -OptionalComponents @($script:DtmRuntimeInstallTransaction.OptionalComponentMetadata) `
        -BepInExDetectedBeforeInstall $bepInExDetectedBeforeInstall `
        -BepInExInstalledByDTMAPI $bepInExInstalledByDTMAPI `
        -BackupsCreated $script:DtmInstallBackupsCreated.ToArray() `
        -LegacyModsMoved $script:DtmInstallLegacyModsMoved.ToArray() `
        -LegacyDetections @(Get-DtmApiLegacyDetections -GameDir $gameDir) `
        -QaFixturesInstalled $qaFixturesInstalled `
        -DryRun $false `
        -SourceRepoCommit $script:DtmInstallSourceCommit
    return [pscustomobject]@{
        ReleaseManifest = $releaseManifest
        InstallState = $installState
    }
}

function Set-DtmApiCommittedInstallReceiptProjection {
    param([Parameter(Mandatory = $true)] $Projection)

    $transactionRoot = Join-Path $stateDir ('.install-receipt-update-' + $script:DtmInstallStamp + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    $candidateRoot = Join-Path $transactionRoot 'candidate'
    $recoveryRoot = Join-Path $transactionRoot 'recovery'
    $candidateRelease = Join-Path $candidateRoot 'release-manifest.json'
    $candidateState = Join-Path $candidateRoot 'install-state.json'
    $recoveryRelease = Join-Path $recoveryRoot 'release-manifest.json'
    $recoveryState = Join-Path $recoveryRoot 'install-state.json'
    $oldReleaseMoved = $false
    $newReleasePlaced = $false
    $oldStateMoved = $false
    $newStatePlaced = $false
    $cleanupAllowed = $false
    New-Item -ItemType Directory -Force -Path $candidateRoot, $recoveryRoot | Out-Null
    try {
        Write-Utf8NoBomJson -Path $candidateRelease -Value $Projection.ReleaseManifest
        Write-Utf8NoBomJson -Path $candidateState -Value $Projection.InstallState
        $parsedRelease = Get-Content -Raw -Encoding UTF8 -LiteralPath $candidateRelease | ConvertFrom-Json
        $parsedState = Get-Content -Raw -Encoding UTF8 -LiteralPath $candidateState | ConvertFrom-Json
        if ([string]$parsedRelease.DTMAPIVersion -ne $script:DtmApiReleaseVersion -or [string]$parsedState.DTMAPIVersion -ne $script:DtmApiReleaseVersion) {
            throw 'Updated DTMAPI install receipts failed Runtime version validation.'
        }

        if (Test-Path -LiteralPath $releaseManifestPath -PathType Leaf) {
            [System.IO.File]::Move($releaseManifestPath, $recoveryRelease)
            $oldReleaseMoved = $true
        }
        [System.IO.File]::Move($candidateRelease, $releaseManifestPath)
        $newReleasePlaced = $true
        if (Test-Path -LiteralPath $installStatePath -PathType Leaf) {
            [System.IO.File]::Move($installStatePath, $recoveryState)
            $oldStateMoved = $true
        }
        [System.IO.File]::Move($candidateState, $installStatePath)
        $newStatePlaced = $true

        $liveRelease = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseManifestPath | ConvertFrom-Json
        $liveState = Get-Content -Raw -Encoding UTF8 -LiteralPath $installStatePath | ConvertFrom-Json
        if ([string]$liveRelease.DTMAPIVersion -ne $script:DtmApiReleaseVersion -or
            [string]$liveState.DTMAPIVersion -ne $script:DtmApiReleaseVersion -or
            -not [string]::Equals([string]$liveRelease.BuildCommit, $script:DtmInstallSourceCommit, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$liveState.SourceRepoCommit, $script:DtmInstallSourceCommit, [System.StringComparison]::Ordinal) -or
            @($liveRelease.BundledMods).Count -ne @($Projection.ReleaseManifest.BundledMods).Count) {
            throw 'Committed DTMAPI install receipts failed final validation.'
        }
        $cleanupAllowed = $true
    }
    catch {
        $projectionError = $_
        try {
            if ($newStatePlaced -and (Test-Path -LiteralPath $installStatePath)) { Remove-Item -LiteralPath $installStatePath -Force }
            if ($oldStateMoved -and (Test-Path -LiteralPath $recoveryState)) { [System.IO.File]::Move($recoveryState, $installStatePath) }
            if ($newReleasePlaced -and (Test-Path -LiteralPath $releaseManifestPath)) { Remove-Item -LiteralPath $releaseManifestPath -Force }
            if ($oldReleaseMoved -and (Test-Path -LiteralPath $recoveryRelease)) { [System.IO.File]::Move($recoveryRelease, $releaseManifestPath) }
            $cleanupAllowed = $true
        }
        catch {
            throw "DTMAPI install receipt update failed and its state rollback also failed. $($projectionError.Exception.Message) Rollback: $($_.Exception.Message) Recovery=$transactionRoot"
        }
        throw $projectionError
    }
    finally {
        if ($cleanupAllowed -and (Test-Path -LiteralPath $transactionRoot)) {
            try { Remove-Item -LiteralPath $transactionRoot -Recurse -Force }
            catch { Write-Warning "DTMAPI install receipt transaction cleanup is pending at $transactionRoot. $($_.Exception.Message)" }
        }
    }
}

# Commit the validated Runtime and its base receipts before publishing any
# developer official-local product. Later product failure therefore retains the
# new Runtime instead of recreating the old-Runtime/new-product mismatch.
$baseProjection = New-DtmApiInstallReceiptProjection -IncludePlannedRuntime
Set-DtmApiRuntimeInstallCandidateState `
    -Transaction $script:DtmRuntimeInstallTransaction `
    -ReleaseManifest $baseProjection.ReleaseManifest `
    -InstallState $baseProjection.InstallState
Complete-DtmApiRuntimeInstallTransaction -Transaction $script:DtmRuntimeInstallTransaction

foreach ($record in @($script:DtmRuntimeInstallTransaction.RuntimeFileRecords)) {
    $script:DtmInstallFilesInstalled.Add($record) | Out-Null
}
if ($null -ne $script:DtmRuntimeInstallTransaction.AssetRecord) {
    $script:DtmInstallFilesInstalled.Add($script:DtmRuntimeInstallTransaction.AssetRecord) | Out-Null
}
foreach ($record in @($script:DtmRuntimeInstallTransaction.StateToolRecords)) {
    $script:DtmInstallFilesInstalled.Add($record) | Out-Null
}
$script:DtmInstallFilesInstalled.Add($releaseManifestRecord) | Out-Null

if (-not $SkipOfficialLocalMods) {
    $officialLocalMods = @(Select-DtmApiOfficialLocalModDefinitions)
    Write-Host "Installing official local DTMAPI packages using mode: $(Get-DtmApiOfficialLocalInstallMode)"
    Assert-OfficialLocalDtmApiEnablementPreflight

    foreach ($mod in $officialLocalMods) {
        Install-OfficialLocalDtmApiMod -Mod $mod
    }

    if ($script:DtmInstallBundledMods.Count -gt 0) {
        $finalProjection = New-DtmApiInstallReceiptProjection
        Set-DtmApiCommittedInstallReceiptProjection -Projection $finalProjection
    }
}

Write-Host "Installed DTMAPI to $pluginDir"
Write-Host "Wrote DTMAPI release manifest to $releaseManifestPath"
Write-Host "Wrote DTMAPI install state to $installStatePath"
