param(
    [string] $Configuration = 'Release',
    [switch] $Quiet,
    [switch] $HostMatrixChild
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
$installScript = Join-Path $PSScriptRoot 'install-to-game.ps1'
$runtimeOutput = Get-DtmapiOutputDir -RepoRoot $repo -Configuration $Configuration
$compatibilityHostOutput = Join-Path $repo "src\DTMAPI.GameBridge.DolocTown.Compatibility\bin\$Configuration\netstandard2.0\DTMAPI.GameBridge.DolocTown.Compatibility.dll"
$compatibilityHostRelativePath = 'DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll'
$runtimeFiles = @(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)
$unicodePathSegment = ([char]0x4E2D).ToString() + ([char]0x6587).ToString()
$script:RuntimeTransactionPackageBuildCommit = 'feedfacecafe'

function Assert-RuntimeTransactionTest {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw "DTMAPI Runtime upgrade transaction test failed: $Message"
    }
}

function Get-RuntimeTransactionQaHostEntries {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return @()
    }
    $resolvedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd('\')
    return @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force | Where-Object {
        $relative = $_.FullName.Substring($resolvedRoot.Length).TrimStart('\').Replace('\', '/')
        $relative -match '(^|/)qa-host(/|$)' -or
        $_.Name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
        $_.Name -match '^(?i:qa-settings\.json|qa-host.*\.json)$'
    })
}

function Get-RuntimeTransactionPowerShellHost {
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

function Assert-RuntimeBuildOutput {
    foreach ($fileName in $runtimeFiles) {
        $path = Join-Path $runtimeOutput $fileName
        Assert-RuntimeTransactionTest -Condition (Test-Path -LiteralPath $path -PathType Leaf) -Message "Build output is missing $path. Run tools/scripts/build.ps1 first."
        $fileVersion = [string][System.Diagnostics.FileVersionInfo]::GetVersionInfo($path).FileVersion
        Assert-RuntimeTransactionTest -Condition ([string]::Equals($fileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) -Message "Build output $fileName has FileVersion=$fileVersion; expected $script:DtmApiBinaryVersion."
    }
    $actualDtmApiDlls = @(Get-ChildItem -LiteralPath $runtimeOutput -File -Filter 'DTMAPI*.dll' | ForEach-Object { $_.Name } | Sort-Object)
    Assert-RuntimeTransactionTest -Condition (($actualDtmApiDlls -join '|') -eq (($runtimeFiles | Sort-Object) -join '|')) -Message "Runtime build output does not contain the exact five production DTMAPI DLLs. Actual=$($actualDtmApiDlls -join ',')"
    Assert-RuntimeTransactionTest -Condition (@(Get-RuntimeTransactionQaHostEntries -Root $runtimeOutput).Count -eq 0) -Message 'Runtime build output contains developer-only QA host material.'
    Assert-RuntimeTransactionTest -Condition (Test-Path -LiteralPath $compatibilityHostOutput -PathType Leaf) -Message "Compatibility Host build output is missing: $compatibilityHostOutput"
    $compatibilityIdentity = [System.Reflection.AssemblyName]::GetAssemblyName($compatibilityHostOutput)
    Assert-RuntimeTransactionTest -Condition (
        [string]::Equals([string]$compatibilityIdentity.Name, 'DTMAPI.GameBridge.DolocTown.Compatibility', [System.StringComparison]::Ordinal) -and
        [string]::Equals([string]$compatibilityIdentity.Version, $script:DtmApiAssemblyCompatibilityVersion, [System.StringComparison]::Ordinal)
    ) -Message 'Compatibility Host build identity is not the frozen Catalog identity.'
}

if (-not $HostMatrixChild) {
    Assert-RuntimeBuildOutput
    Test-DtmApiWindowsPowerShellSyntax -Paths @($installScript, $PSCommandPath) -AllowCoreFallback

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
    Assert-RuntimeTransactionTest -Condition ($hosts.Count -gt 0) -Message 'No PowerShell host was available for the host matrix.'
    foreach ($hostExe in $hosts) {
        $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Configuration', $Configuration, '-HostMatrixChild')
        if ($Quiet) {
            $arguments += '-Quiet'
        }
        & $hostExe @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "DTMAPI Runtime upgrade transaction matrix failed under $hostExe with exit code $LASTEXITCODE."
        }
    }

    if (-not $Quiet) {
        Write-Host "DTMAPI Runtime upgrade transaction host matrix: OK (hosts=$($hosts.Count))"
    }
    return
}

function Write-RuntimeTransactionText {
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

function Get-RuntimeTransactionPackageAssemblyReceipts {
    param([Parameter(Mandatory = $true)] [string] $PluginRoot)

    $receipts = New-Object 'System.Collections.Generic.List[object]'
    foreach ($fileName in $runtimeFiles) {
        $payloadPath = Join-Path $PluginRoot $fileName
        $sourcePath = Join-Path $runtimeOutput $fileName
        $item = Get-Item -LiteralPath $payloadPath -ErrorAction Stop
        $fileVersion = [string][System.Diagnostics.FileVersionInfo]::GetVersionInfo($sourcePath).FileVersion
        $receipts.Add([ordered]@{
            FileName = $fileName
            Length = [long]$item.Length
            Sha256 = (Get-FileHash -LiteralPath $payloadPath -Algorithm SHA256).Hash.ToLowerInvariant()
            FileVersion = $fileVersion
        }) | Out-Null
    }
    return $receipts.ToArray()
}

function Get-RuntimeTransactionOptionalComponentReceipt {
    param([Parameter(Mandatory = $true)] [string] $ComponentPath)

    $item = Get-Item -LiteralPath $ComponentPath -ErrorAction Stop
    $identity = [System.Reflection.AssemblyName]::GetAssemblyName($ComponentPath)
    return [ordered]@{
        ComponentId = 'gamebridge-compatibility-host'
        Distribution = 'dormant-shipped'
        LoadPolicy = 'first-frozen-abi-call'
        RelativePath = $compatibilityHostRelativePath
        Length = [long]$item.Length
        Sha256 = (Get-FileHash -LiteralPath $ComponentPath -Algorithm SHA256).Hash.ToLowerInvariant()
        AssemblyName = [string]$identity.Name
        AssemblyVersion = [string]$identity.Version
        FileVersion = [string][System.Diagnostics.FileVersionInfo]::GetVersionInfo($ComponentPath).FileVersion
        TargetFramework = 'netstandard2.0'
        DefaultLoadState = 'dormant'
        IncludedInDownloadPackage = $true
    }
}

function Get-RuntimeTransactionRelativePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    $prefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
    Assert-RuntimeTransactionTest -Condition ($pathFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) -Message "Snapshot path escaped its root: $pathFull"
    return $pathFull.Substring($prefix.Length).Replace('\', '/')
}

function Get-RuntimeTransactionTreeFingerprint {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return '<missing>'
    }

    $rows = New-Object 'System.Collections.Generic.List[string]'
    foreach ($directory in @(Get-ChildItem -LiteralPath $Root -Directory -Recurse -Force | Sort-Object FullName)) {
        $rows.Add('D|' + (Get-RuntimeTransactionRelativePath -Root $Root -Path $directory.FullName)) | Out-Null
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force | Sort-Object FullName)) {
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $rows.Add(('F|{0}|{1}|{2}' -f (Get-RuntimeTransactionRelativePath -Root $Root -Path $file.FullName), $file.Length, $hash)) | Out-Null
    }
    return (@($rows.ToArray()) | Sort-Object) -join "`n"
}

function New-RuntimeTransactionPayload {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [switch] $WithoutReleaseManifest
    )

    $payloadRoot = Join-Path $Root 'Content\DTMAPIInstaller\Payload'
    $pluginRoot = Join-Path $payloadRoot 'BepInEx\plugins\DTMAPI'
    $toolsRoot = Join-Path $Root 'Content\DTMAPIInstaller\tools'
    New-Item -ItemType Directory -Force -Path $pluginRoot | Out-Null
    foreach ($fileName in $runtimeFiles) {
        Copy-Item -LiteralPath (Join-Path $runtimeOutput $fileName) -Destination (Join-Path $pluginRoot $fileName) -Force
    }
    $componentPath = Join-Path $payloadRoot ($compatibilityHostRelativePath.Replace('/', '\'))
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $componentPath) | Out-Null
    Copy-Item -LiteralPath $compatibilityHostOutput -Destination $componentPath -Force
    New-Item -ItemType Directory -Force -Path $toolsRoot | Out-Null
    foreach ($scriptName in @(
        'common.ps1',
        'release-common.ps1',
        'install-to-game.ps1',
        'install-bepinex.ps1',
        'uninstall-dtmapi.ps1',
        'check-dtmapi-status.ps1',
        'collect-logs.ps1',
        'analyze-startup-evidence.ps1'
    )) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $toolsRoot $scriptName) -Force
    }
    Copy-Item -LiteralPath $script:DtmApiVersionAuthority.Path -Destination (Join-Path $toolsRoot 'dtmapi-runtime-version.props') -Force
    if (-not $WithoutReleaseManifest) {
        $releaseRoot = Join-Path $Root 'Content\DTMAPI'
        New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null
        Write-Utf8NoBomJson -Path (Join-Path $releaseRoot 'release-manifest.json') -Value ([ordered]@{
            SchemaVersion = 1
            DTMAPIVersion = $script:DtmApiReleaseVersion
            BinaryVersion = $script:DtmApiBinaryVersion
            BuildCommit = $script:RuntimeTransactionPackageBuildCommit
            PackageKind = 'workshop-runtime'
            IncludedAssemblies = @(Get-RuntimeTransactionPackageAssemblyReceipts -PluginRoot $pluginRoot)
            OptionalComponents = @((Get-RuntimeTransactionOptionalComponentReceipt -ComponentPath $componentPath))
        })
    }
    return $payloadRoot
}

function Get-RuntimeTransactionPackageManifestPath {
    param([Parameter(Mandatory = $true)] [string] $PayloadRoot)

    return [System.IO.Path]::GetFullPath((Join-Path $PayloadRoot '..\..\DTMAPI\release-manifest.json'))
}

function New-RuntimeTransactionFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $PayloadRoot
    )

    $gameDir = Join-Path $Root ('Game ' + $unicodePathSegment)
    $pluginDir = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
    $stateDir = Join-Path $gameDir 'DTMAPI'
    $toolsDir = Join-Path $stateDir 'tools'
    $componentsDir = Join-Path $stateDir 'components'
    New-Item -ItemType Directory -Force -Path (Join-Path $gameDir 'DolocTown_Data') | Out-Null
    Write-RuntimeTransactionText -Path (Join-Path $gameDir 'DolocTown.exe') -Text 'fake-executable-only-never-launched'
    foreach ($fileName in $runtimeFiles) {
        Write-RuntimeTransactionText -Path (Join-Path $pluginDir $fileName) -Text ('old-runtime-' + $fileName)
    }
    Write-RuntimeTransactionText -Path (Join-Path $pluginDir 'old-only-sentinel.txt') -Text 'old-runtime-sentinel'
    Write-RuntimeTransactionText -Path (Join-Path $pluginDir 'assets\branding\old-icon.txt') -Text 'old-asset-sentinel'
    Write-RuntimeTransactionText -Path (Join-Path $componentsDir 'compatibility\old-host-sentinel.dll') -Text 'old-optional-component'
    foreach ($toolName in @('dtmapi-runtime-version.props', 'common.ps1', 'release-common.ps1', 'uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1', 'old-only-tool.ps1')) {
        Write-RuntimeTransactionText -Path (Join-Path $toolsDir $toolName) -Text ('old-tool-' + $toolName)
    }
    Write-RuntimeTransactionText -Path (Join-Path $stateDir 'release-manifest.json') -Text '{"DTMAPIVersion":"0.5.3-alpha","BinaryVersion":"0.5.3.0","sentinel":"old-release"}'
    Write-RuntimeTransactionText -Path (Join-Path $stateDir 'install-state.json') -Text '{"DTMAPIVersion":"0.5.3-alpha","BinaryVersion":"0.5.3.0","sentinel":"old-state"}'
    Write-RuntimeTransactionText -Path (Join-Path $stateDir 'reports\keep-report.txt') -Text 'unrelated-report-sentinel'
    Write-RuntimeTransactionText -Path (Join-Path $stateDir 'config\keep-config.json') -Text '{"keep":true}'

    return [pscustomobject]@{
        Root = $Root
        GameDir = $gameDir
        PluginDir = $pluginDir
        StateDir = $stateDir
        ToolsDir = $toolsDir
        ComponentsDir = $componentsDir
        PayloadRoot = $PayloadRoot
        PersistentRoot = (Join-Path $Root ('User ' + $unicodePathSegment))
        PluginBefore = Get-RuntimeTransactionTreeFingerprint -Root $pluginDir
        ToolsBefore = Get-RuntimeTransactionTreeFingerprint -Root $toolsDir
        ComponentsBefore = Get-RuntimeTransactionTreeFingerprint -Root $componentsDir
        ReleaseBefore = (Get-FileHash -LiteralPath (Join-Path $stateDir 'release-manifest.json') -Algorithm SHA256).Hash
        StateBefore = (Get-FileHash -LiteralPath (Join-Path $stateDir 'install-state.json') -Algorithm SHA256).Hash
    }
}

function Invoke-RuntimeTransactionInstaller {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [string] $FaultPhase = '',
        [string] $RollbackFaultPhase = '',
        [string] $ReceiptFaultStage = '',
        [int] $ReceiptFaultCount = 0,
        [int] $ReceiptCleanupFaultCount = 0,
        [switch] $ExplicitPayloadRoot
    )

    $tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $fixtureGame = [System.IO.Path]::GetFullPath([string]$Fixture.GameDir)
    Assert-RuntimeTransactionTest -Condition ($fixtureGame.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase)) -Message "Refusing to run the transaction matrix outside system temp: $fixtureGame"

    $oldGameDir = $env:DTMAPI_GAME_DIR
    $oldRuntimeDir = $env:DTMAPI_RUNTIME_DIR
    $oldPersistentRoot = $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    $oldTestMode = $env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE
    $oldFaultPhase = $env:DTMAPI_RUNTIME_INSTALL_FAIL_PHASE
    $oldRollbackFaultPhase = $env:DTMAPI_RUNTIME_INSTALL_ROLLBACK_FAIL_PHASE
    $oldReceiptFaultStage = $env:DTMAPI_RUNTIME_RECEIPT_TEST_FAIL_STAGE
    $oldReceiptFaultCount = $env:DTMAPI_RUNTIME_RECEIPT_TEST_FAIL_COUNT
    $oldReceiptCleanupFaultCount = $env:DTMAPI_RUNTIME_RECEIPT_TEST_CLEANUP_FAIL_COUNT
    try {
        $env:DTMAPI_GAME_DIR = $Fixture.GameDir
        $env:DTMAPI_RUNTIME_DIR = $Fixture.StateDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $Fixture.PersistentRoot
        $env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE = '1'
        $env:DTMAPI_RUNTIME_INSTALL_FAIL_PHASE = $FaultPhase
        $env:DTMAPI_RUNTIME_INSTALL_ROLLBACK_FAIL_PHASE = $RollbackFaultPhase
        $env:DTMAPI_RUNTIME_RECEIPT_TEST_FAIL_STAGE = $ReceiptFaultStage
        $env:DTMAPI_RUNTIME_RECEIPT_TEST_FAIL_COUNT = if ($ReceiptFaultCount -gt 0) { [string]$ReceiptFaultCount } else { '' }
        $env:DTMAPI_RUNTIME_RECEIPT_TEST_CLEANUP_FAIL_COUNT = if ($ReceiptCleanupFaultCount -gt 0) { [string]$ReceiptCleanupFaultCount } else { '' }
        $hostExe = Get-RuntimeTransactionPowerShellHost
        $packageToolsRoot = [System.IO.Path]::GetFullPath((Join-Path $Fixture.PayloadRoot '..\tools'))
        $packagedInstallScript = Join-Path $packageToolsRoot 'install-to-game.ps1'
        Assert-RuntimeTransactionTest -Condition (Test-Path -LiteralPath $packagedInstallScript -PathType Leaf) -Message "Package-like installer is missing: $packagedInstallScript"
        $arguments = @(
            '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $packagedInstallScript,
            '-Configuration', $Configuration,
            '-SkipBuild',
            '-SkipOfficialLocalMods'
        )
        if ($ExplicitPayloadRoot) {
            $arguments += @('-PackagePayloadRoot', $Fixture.PayloadRoot)
        }
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $output = @(& $hostExe @arguments 2>&1 | ForEach-Object { [string]$_ })
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousPreference
        }
        return [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
    }
    finally {
        $env:DTMAPI_GAME_DIR = $oldGameDir
        $env:DTMAPI_RUNTIME_DIR = $oldRuntimeDir
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $oldPersistentRoot
        $env:DTMAPI_INSTALL_TRANSACTION_TEST_MODE = $oldTestMode
        $env:DTMAPI_RUNTIME_INSTALL_FAIL_PHASE = $oldFaultPhase
        $env:DTMAPI_RUNTIME_INSTALL_ROLLBACK_FAIL_PHASE = $oldRollbackFaultPhase
        $env:DTMAPI_RUNTIME_RECEIPT_TEST_FAIL_STAGE = $oldReceiptFaultStage
        $env:DTMAPI_RUNTIME_RECEIPT_TEST_FAIL_COUNT = $oldReceiptFaultCount
        $env:DTMAPI_RUNTIME_RECEIPT_TEST_CLEANUP_FAIL_COUNT = $oldReceiptCleanupFaultCount
    }
}

function Invoke-RuntimeTransactionStatus {
    param([Parameter(Mandatory = $true)] $Fixture)

    $hostExe = Get-RuntimeTransactionPowerShellHost
    $statusScript = Join-Path ([System.IO.Path]::GetFullPath((Join-Path $Fixture.PayloadRoot '..\tools'))) 'check-dtmapi-status.ps1'
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $hostExe -NoProfile -ExecutionPolicy Bypass -File $statusScript -GameDir $Fixture.GameDir 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
}

function Invoke-RuntimeTransactionUninstaller {
    param([Parameter(Mandatory = $true)] $Fixture)

    $hostExe = Get-RuntimeTransactionPowerShellHost
    $uninstallScript = Join-Path ([System.IO.Path]::GetFullPath((Join-Path $Fixture.PayloadRoot '..\tools'))) 'uninstall-dtmapi.ps1'
    $oldGameDir = $env:DTMAPI_GAME_DIR
    $oldRuntimeDir = $env:DTMAPI_RUNTIME_DIR
    try {
        $env:DTMAPI_GAME_DIR = $Fixture.GameDir
        $env:DTMAPI_RUNTIME_DIR = $Fixture.StateDir
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $output = @(& $hostExe -NoProfile -ExecutionPolicy Bypass -File $uninstallScript -GameDir $Fixture.GameDir 2>&1 | ForEach-Object { [string]$_ })
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousPreference
        }
        return [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
    }
    finally {
        $env:DTMAPI_GAME_DIR = $oldGameDir
        $env:DTMAPI_RUNTIME_DIR = $oldRuntimeDir
    }
}

function New-RuntimeTransactionResidueRoot {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Stamp
    )

    $runtimeRoot = Join-Path $Fixture.GameDir ('.dtmapi-runtime-install-' + $Stamp)
    New-Item -ItemType Directory -Force -Path $runtimeRoot | Out-Null
    return $runtimeRoot
}

function Assert-RuntimeTransactionStatusReadOnly {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $ExpectedPattern,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $before = Get-RuntimeTransactionTreeFingerprint -Root $Fixture.GameDir
    $status = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    $after = Get-RuntimeTransactionTreeFingerprint -Root $Fixture.GameDir
    Assert-RuntimeTransactionTest -Condition ($status.ExitCode -eq 1) -Message "$Label status exit code was $($status.ExitCode), expected 1. Output=$($status.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition (($status.Output -join "`n") -match $ExpectedPattern) -Message "$Label status output omitted '$ExpectedPattern'. Output=$($status.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition ([string]::Equals($before, $after, [System.StringComparison]::Ordinal)) -Message "$Label status check changed files."
    return $status
}

function Assert-RuntimeTransactionBlockedBeforeMutation {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] $Result,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    Assert-RuntimeTransactionTest -Condition ($Result.ExitCode -ne 0) -Message "$Label unexpectedly succeeded. Output=$($Result.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition (($Result.Output -join "`n") -match 'DTM-E1303') -Message "$Label omitted DTM-E1303. Output=$($Result.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.PluginDir) -eq $Fixture.PluginBefore) -Message "$Label changed the old Runtime directory."
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.ToolsDir) -eq $Fixture.ToolsBefore) -Message "$Label changed the old tools directory."
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.ComponentsDir) -eq $Fixture.ComponentsBefore) -Message "$Label changed the old optional-component directory."
    Assert-RuntimeTransactionTest -Condition ((Get-FileHash -LiteralPath (Join-Path $Fixture.StateDir 'release-manifest.json') -Algorithm SHA256).Hash -eq $Fixture.ReleaseBefore) -Message "$Label changed the old release manifest."
    Assert-RuntimeTransactionTest -Condition ((Get-FileHash -LiteralPath (Join-Path $Fixture.StateDir 'install-state.json') -Algorithm SHA256).Hash -eq $Fixture.StateBefore) -Message "$Label changed the old install state."
}

function Assert-NoRuntimeTransactionResidue {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $pluginParent = Split-Path -Parent $Fixture.PluginDir
    $pluginResidue = @(Get-ChildItem -LiteralPath $pluginParent -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like '.DTMAPI-candidate-*' -or $_.Name -like '.DTMAPI-recovery-*' })
    $runtimeResidue = @(Get-ChildItem -LiteralPath $Fixture.GameDir -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like '.dtmapi-runtime-install-*' })
    $stateResidue = @(Get-ChildItem -LiteralPath $Fixture.StateDir -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like '.runtime-install-transaction-*' })
    Assert-RuntimeTransactionTest -Condition ($pluginResidue.Count -eq 0) -Message "$Label left candidate/recovery plugin directories."
    Assert-RuntimeTransactionTest -Condition ($runtimeResidue.Count -eq 0) -Message "$Label left the game-root Runtime transaction directory."
    Assert-RuntimeTransactionTest -Condition ($stateResidue.Count -eq 0) -Message "$Label left state transaction directories."
}

function Assert-RuntimeTransactionRollback {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label,
        [string] $ExpectedPhase = ''
    )

    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.PluginDir) -eq $Fixture.PluginBefore) -Message "$Label did not restore the byte-identical old Runtime directory."
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.ToolsDir) -eq $Fixture.ToolsBefore) -Message "$Label did not restore the byte-identical old tools directory."
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.ComponentsDir) -eq $Fixture.ComponentsBefore) -Message "$Label did not restore the byte-identical old optional-component directory."
    Assert-RuntimeTransactionTest -Condition ((Get-FileHash -LiteralPath (Join-Path $Fixture.StateDir 'release-manifest.json') -Algorithm SHA256).Hash -eq $Fixture.ReleaseBefore) -Message "$Label did not restore the old release manifest."
    Assert-RuntimeTransactionTest -Condition ((Get-FileHash -LiteralPath (Join-Path $Fixture.StateDir 'install-state.json') -Algorithm SHA256).Hash -eq $Fixture.StateBefore) -Message "$Label did not restore the old install state."
    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $Fixture.StateDir 'reports\keep-report.txt')) -eq 'unrelated-report-sentinel') -Message "$Label changed unrelated report state."
    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $Fixture.StateDir 'config\keep-config.json')) -eq '{"keep":true}') -Message "$Label changed unrelated config state."
    Assert-NoRuntimeTransactionResidue -Fixture $Fixture -Label $Label

    $failureFiles = @(Get-ChildItem -LiteralPath $Fixture.StateDir -Filter 'install-state.failed-*.json' -File)
    Assert-RuntimeTransactionTest -Condition ($failureFiles.Count -eq 1) -Message "$Label did not write exactly one failure receipt."
    $failure = Get-Content -Raw -Encoding UTF8 -LiteralPath $failureFiles[0].FullName | ConvertFrom-Json
    Assert-RuntimeTransactionTest -Condition ([bool]$failure.RuntimeRollbackSucceeded) -Message "$Label failure receipt did not record successful rollback."
    if (-not [string]::IsNullOrWhiteSpace($ExpectedPhase)) {
        Assert-RuntimeTransactionTest -Condition ([string]::Equals([string]$failure.RuntimeTransactionPhase, $ExpectedPhase, [System.StringComparison]::Ordinal)) -Message "$Label failure receipt phase was '$($failure.RuntimeTransactionPhase)', expected '$ExpectedPhase'."
    }
}

function Assert-RuntimePackagePreflightFailure {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] $Result,
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $ExpectedError
    )

    Assert-RuntimeTransactionTest -Condition ($Result.ExitCode -ne 0) -Message "$Label unexpectedly succeeded. Output=$($Result.Output -join ' | ')"
    $output = $Result.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($output.IndexOf($ExpectedError, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -Message "$Label output omitted the original package-manifest error. Output=$($Result.Output -join ' | ')"
    foreach ($forbidden in @(
        'DtmRuntimeInstallTransaction',
        'cannot be retrieved because it has not been set',
        'The variable cannot be validated because the value',
        'InvalidOperation:'
    )) {
        Assert-RuntimeTransactionTest -Condition ($output.IndexOf($forbidden, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) -Message "$Label exposed a secondary trap/StrictMode error '$forbidden'. Output=$($Result.Output -join ' | ')"
    }

    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.PluginDir) -eq $Fixture.PluginBefore) -Message "$Label changed the old Runtime directory before package preflight completed."
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.ToolsDir) -eq $Fixture.ToolsBefore) -Message "$Label changed the old tools directory before package preflight completed."
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $Fixture.ComponentsDir) -eq $Fixture.ComponentsBefore) -Message "$Label changed the old optional-component directory before package preflight completed."
    Assert-RuntimeTransactionTest -Condition ((Get-FileHash -LiteralPath (Join-Path $Fixture.StateDir 'release-manifest.json') -Algorithm SHA256).Hash -eq $Fixture.ReleaseBefore) -Message "$Label changed the old release manifest before package preflight completed."
    Assert-RuntimeTransactionTest -Condition ((Get-FileHash -LiteralPath (Join-Path $Fixture.StateDir 'install-state.json') -Algorithm SHA256).Hash -eq $Fixture.StateBefore) -Message "$Label changed the old install state before package preflight completed."
    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $Fixture.StateDir 'reports\keep-report.txt')) -eq 'unrelated-report-sentinel') -Message "$Label changed unrelated report state."
    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $Fixture.StateDir 'config\keep-config.json')) -eq '{"keep":true}') -Message "$Label changed unrelated config state."
    Assert-NoRuntimeTransactionResidue -Fixture $Fixture -Label $Label

    $failureFiles = @(Get-ChildItem -LiteralPath $Fixture.StateDir -Filter 'install-state.failed-*.json' -File)
    Assert-RuntimeTransactionTest -Condition ($failureFiles.Count -eq 1) -Message "$Label did not write exactly one package-preflight failure receipt."
    $failure = Get-Content -Raw -Encoding UTF8 -LiteralPath $failureFiles[0].FullName | ConvertFrom-Json
    Assert-RuntimeTransactionTest -Condition ([string]$failure.Error -like ('*' + $ExpectedError + '*')) -Message "$Label failure receipt did not preserve the original package-manifest error."
    Assert-RuntimeTransactionTest -Condition ([string]::IsNullOrWhiteSpace([string]$failure.RuntimeTransactionPhase)) -Message "$Label incorrectly recorded a Runtime transaction phase before transaction creation."
    Assert-RuntimeTransactionTest -Condition ($null -eq $failure.RuntimeRollbackSucceeded) -Message "$Label incorrectly claimed Runtime rollback before transaction creation."
    Assert-RuntimeTransactionTest -Condition (-not [bool]$failure.RuntimeCommitSucceeded) -Message "$Label incorrectly claimed Runtime commit before package preflight completed."
    Assert-RuntimeTransactionTest -Condition (@($failure.FilesInstalledBeforeFailure).Count -eq 0) -Message "$Label failure receipt recorded installed files before package preflight completed."
}

function Assert-RuntimeTransactionSuccess {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label,
        [AllowEmptyString()] [string] $ExpectedSourceCommit = $script:RuntimeTransactionPackageBuildCommit,
        [int] $ExpectedFailureReceiptCount = 0
    )

    $actualDlls = @(Get-ChildItem -LiteralPath $Fixture.PluginDir -Filter '*.dll' -File | ForEach-Object { $_.Name } | Sort-Object)
    Assert-RuntimeTransactionTest -Condition (($actualDlls -join '|') -eq (($runtimeFiles | Sort-Object) -join '|')) -Message "$Label did not install the exact five-DLL Runtime set."
    foreach ($fileName in $runtimeFiles) {
        $livePath = Join-Path $Fixture.PluginDir $fileName
        $payloadPath = Join-Path $Fixture.PayloadRoot ('BepInEx\plugins\DTMAPI\' + $fileName)
        $liveHash = (Get-FileHash -LiteralPath $livePath -Algorithm SHA256).Hash
        $payloadHash = (Get-FileHash -LiteralPath $payloadPath -Algorithm SHA256).Hash
        Assert-RuntimeTransactionTest -Condition ($liveHash -eq $payloadHash) -Message "$Label installed the wrong bytes for $fileName."
    }
    Assert-RuntimeTransactionTest -Condition (-not (Test-Path -LiteralPath (Join-Path $Fixture.PluginDir 'old-only-sentinel.txt'))) -Message "$Label retained an old-only Runtime file."
    $liveComponent = Join-Path $Fixture.GameDir ($compatibilityHostRelativePath.Replace('/', '\'))
    $payloadComponent = Join-Path $Fixture.PayloadRoot ($compatibilityHostRelativePath.Replace('/', '\'))
    Assert-RuntimeTransactionTest -Condition (Test-Path -LiteralPath $liveComponent -PathType Leaf) -Message "$Label did not install the dormant-shipped Compatibility Host."
    Assert-RuntimeTransactionTest -Condition (
        [string]::Equals(
            (Get-FileHash -LiteralPath $liveComponent -Algorithm SHA256).Hash,
            (Get-FileHash -LiteralPath $payloadComponent -Algorithm SHA256).Hash,
            [System.StringComparison]::OrdinalIgnoreCase)
    ) -Message "$Label installed the wrong Compatibility Host bytes."
    Assert-RuntimeTransactionTest -Condition (@(Get-ChildItem -LiteralPath $Fixture.ComponentsDir -File -Recurse).Count -eq 1) -Message "$Label did not install the exact one-file optional-component set."
    Assert-RuntimeTransactionTest -Condition (@(Get-RuntimeTransactionQaHostEntries -Root $Fixture.PluginDir).Count -eq 0) -Message "$Label installed QA host material under the Runtime plugin root."
    Assert-RuntimeTransactionTest -Condition (@(Get-RuntimeTransactionQaHostEntries -Root $Fixture.StateDir).Count -eq 0) -Message "$Label installed or retained QA host activation/staging state."
    Assert-RuntimeTransactionTest -Condition (-not (Test-Path -LiteralPath (Join-Path $Fixture.ToolsDir 'old-only-tool.ps1'))) -Message "$Label retained an old-only state tool."
    foreach ($toolName in @('dtmapi-runtime-version.props', 'common.ps1', 'release-common.ps1', 'uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1')) {
        Assert-RuntimeTransactionTest -Condition (Test-Path -LiteralPath (Join-Path $Fixture.ToolsDir $toolName) -PathType Leaf) -Message "$Label missed installed tool $toolName."
    }
    $livePlayerDoctorRoot = Join-Path $Fixture.ToolsDir 'player-doctor'
    Assert-RuntimeTransactionTest -Condition (-not (Test-Path -LiteralPath $livePlayerDoctorRoot)) -Message "$Label installed the optional Player Doctor in the normal Runtime transaction."

    $release = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Fixture.StateDir 'release-manifest.json') | ConvertFrom-Json
    $state = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Fixture.StateDir 'install-state.json') | ConvertFrom-Json
    Assert-RuntimeTransactionTest -Condition ([string]$release.DTMAPIVersion -eq $script:DtmApiReleaseVersion -and [string]$release.BinaryVersion -eq $script:DtmApiBinaryVersion) -Message "$Label release manifest version projection is wrong."
    Assert-RuntimeTransactionTest -Condition ([string]$state.DTMAPIVersion -eq $script:DtmApiReleaseVersion -and [string]$state.BinaryVersion -eq $script:DtmApiBinaryVersion) -Message "$Label install state version projection is wrong."
    Assert-RuntimeTransactionTest -Condition ([string]::Equals([string]$release.BuildCommit, $ExpectedSourceCommit, [System.StringComparison]::Ordinal)) -Message "$Label release manifest did not preserve the expected source provenance."
    Assert-RuntimeTransactionTest -Condition ([string]::Equals([string]$state.SourceRepoCommit, $ExpectedSourceCommit, [System.StringComparison]::Ordinal)) -Message "$Label install state did not project the expected source provenance."
    Assert-RuntimeTransactionTest -Condition (@($release.IncludedAssemblies).Count -eq $runtimeFiles.Count) -Message "$Label release manifest assembly count is wrong."
    Assert-RuntimeTransactionTest -Condition (@($release.OptionalComponents).Count -eq 1 -and @($state.OptionalComponents).Count -eq 1) -Message "$Label did not commit one optional-component receipt to both authorities."
    foreach ($componentReceipt in @($release.OptionalComponents[0], $state.OptionalComponents[0])) {
        Assert-RuntimeTransactionTest -Condition (
            [string]$componentReceipt.ComponentId -eq 'gamebridge-compatibility-host' -and
            [string]$componentReceipt.RelativePath -eq $compatibilityHostRelativePath -and
            [string]$componentReceipt.AssemblyName -eq 'DTMAPI.GameBridge.DolocTown.Compatibility' -and
            [string]$componentReceipt.AssemblyVersion -eq $script:DtmApiAssemblyCompatibilityVersion -and
            [string]$componentReceipt.TargetFramework -eq 'netstandard2.0' -and
            [string]$componentReceipt.Sha256 -eq (Get-FileHash -LiteralPath $liveComponent -Algorithm SHA256).Hash.ToLowerInvariant()
        ) -Message "$Label committed an invalid optional-component identity/hash receipt."
    }
    foreach ($assembly in @($release.IncludedAssemblies)) {
        $liveHash = (Get-FileHash -LiteralPath (Join-Path $Fixture.PluginDir ([string]$assembly.FileName)) -Algorithm SHA256).Hash.ToLowerInvariant()
        Assert-RuntimeTransactionTest -Condition ([string]$assembly.Sha256 -eq $liveHash) -Message "$Label release manifest hash is wrong for $($assembly.FileName)."
    }
    $playerDoctorStateRecords = @($state.FilesInstalled | Where-Object { [string]$_.Kind -eq 'player-doctor' -or [string]$_.Kind -eq 'player-doctor-license' })
    Assert-RuntimeTransactionTest -Condition ($playerDoctorStateRecords.Count -eq 0) -Message "$Label retained an unexpected Player Doctor install-state receipt."
    $cleanStatus = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    $cleanStatusText = $cleanStatus.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($cleanStatusText -match ('Installed provenance commit receipts match: ' + [regex]::Escape($ExpectedSourceCommit))) -Message "$Label status check did not verify matching non-empty provenance receipts. Output=$($cleanStatus.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition ($cleanStatusText -match 'Player Doctor is not installed; the normal Runtime package intentionally contains no EXE\.') -Message "$Label status check treated the optional Player Doctor as required. Output=$($cleanStatus.Output -join ' | ')"

    $installedVersionAuthorityPath = Join-Path $Fixture.ToolsDir 'dtmapi-runtime-version.props'
    $installedVersionAuthorityText = [System.IO.File]::ReadAllText($installedVersionAuthorityPath)
    Remove-Item -LiteralPath $installedVersionAuthorityPath -Force
    $missingVersionAuthorityStatus = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    Write-RuntimeTransactionText -Path $installedVersionAuthorityPath -Text $installedVersionAuthorityText
    $missingVersionAuthorityText = $missingVersionAuthorityStatus.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($missingVersionAuthorityStatus.ExitCode -ne 0 -and
        $missingVersionAuthorityText -match ('The installed ' + [regex]::Escape($script:DtmApiReleaseVersion) + ' state is missing its Runtime version authority\.')) -Message "$Label status check did not project the current release into the missing-authority finding. Output=$($missingVersionAuthorityStatus.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition ($missingVersionAuthorityText -notmatch 'The installed 0\.5\.5 state is missing its Runtime version authority\.') -Message "$Label status check retained the stale 0.5.5 missing-authority finding. Output=$($missingVersionAuthorityStatus.Output -join ' | ')"

    $releaseReceiptPath = Join-Path $Fixture.StateDir 'release-manifest.json'
    $stateReceiptPath = Join-Path $Fixture.StateDir 'install-state.json'
    $releaseReceiptText = [System.IO.File]::ReadAllText($releaseReceiptPath)
    $stateReceiptText = [System.IO.File]::ReadAllText($stateReceiptPath)

    $legacyOmittedHostRelease = $releaseReceiptText | ConvertFrom-Json
    $legacyOmittedHostState = $stateReceiptText | ConvertFrom-Json
    $legacyOmittedHostRelease.PSObject.Properties.Remove('OptionalComponents')
    $legacyOmittedHostState.PSObject.Properties.Remove('OptionalComponents')
    $legacyOmittedHostRelease.DTMAPIVersion = '0.5.4'
    $legacyOmittedHostState.DTMAPIVersion = '0.5.4'
    Write-Utf8NoBomJson -Path $releaseReceiptPath -Value $legacyOmittedHostRelease
    Write-Utf8NoBomJson -Path $stateReceiptPath -Value $legacyOmittedHostState
    $legacyOmittedHostStatus = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    Write-RuntimeTransactionText -Path $releaseReceiptPath -Text $releaseReceiptText
    Write-RuntimeTransactionText -Path $stateReceiptPath -Text $stateReceiptText
    $legacyOmittedHostStatusText = $legacyOmittedHostStatus.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($legacyOmittedHostStatusText -notmatch '0\.5\.5 and later require both receipts to project exactly one dormant Compatibility Host') -Message "$Label status check imposed the 0.5.5 Compatibility Host requirement on historical 0.5.4 receipts. Output=$($legacyOmittedHostStatus.Output -join ' | ')"

    $omittedHostRelease = $releaseReceiptText | ConvertFrom-Json
    $omittedHostState = $stateReceiptText | ConvertFrom-Json
    $omittedHostRelease.PSObject.Properties.Remove('OptionalComponents')
    $omittedHostState.PSObject.Properties.Remove('OptionalComponents')
    Write-Utf8NoBomJson -Path $releaseReceiptPath -Value $omittedHostRelease
    Write-Utf8NoBomJson -Path $stateReceiptPath -Value $omittedHostState
    $omittedHostStatus = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    Write-RuntimeTransactionText -Path $releaseReceiptPath -Text $releaseReceiptText
    Write-RuntimeTransactionText -Path $stateReceiptPath -Text $stateReceiptText
    $omittedHostStatusText = $omittedHostStatus.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($omittedHostStatus.ExitCode -ne 0 -and
        $omittedHostStatusText -match '0\.5\.5 and later require both receipts to project exactly one dormant Compatibility Host') -Message "$Label status check accepted two current receipts that both omitted the required Compatibility Host. Output=$($omittedHostStatus.Output -join ' | ')"

    $missingReleaseCommit = $releaseReceiptText | ConvertFrom-Json
    $missingReleaseCommit.BuildCommit = ''
    Write-Utf8NoBomJson -Path $releaseReceiptPath -Value $missingReleaseCommit
    $missingReleaseCommitStatus = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    Write-RuntimeTransactionText -Path $releaseReceiptPath -Text $releaseReceiptText
    $missingReleaseCommitText = $missingReleaseCommitStatus.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($missingReleaseCommitText -match 'Installed release-manifest BuildCommit is missing\.') -Message "$Label status check did not reject a missing release BuildCommit. Output=$($missingReleaseCommitStatus.Output -join ' | ')"

    $missingStateCommit = $stateReceiptText | ConvertFrom-Json
    $missingStateCommit.SourceRepoCommit = ''
    Write-Utf8NoBomJson -Path $stateReceiptPath -Value $missingStateCommit
    $missingStateCommitStatus = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    Write-RuntimeTransactionText -Path $stateReceiptPath -Text $stateReceiptText
    $missingStateCommitText = $missingStateCommitStatus.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($missingStateCommitText -match 'Installed install-state SourceRepoCommit is missing\.') -Message "$Label status check did not reject a missing install-state SourceRepoCommit. Output=$($missingStateCommitStatus.Output -join ' | ')"

    $mismatchedStateCommit = $stateReceiptText | ConvertFrom-Json
    $mismatchedStateCommit.SourceRepoCommit = 'deadbeefcafe'
    Write-Utf8NoBomJson -Path $stateReceiptPath -Value $mismatchedStateCommit
    $mismatchedCommitStatus = Invoke-RuntimeTransactionStatus -Fixture $Fixture
    Write-RuntimeTransactionText -Path $stateReceiptPath -Text $stateReceiptText
    $mismatchedCommitText = $mismatchedCommitStatus.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($mismatchedCommitText -match 'Installed provenance commit mismatch\.') -Message "$Label status check did not reject mismatched provenance receipts. Output=$($mismatchedCommitStatus.Output -join ' | ')"

    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $Fixture.StateDir 'reports\keep-report.txt')) -eq 'unrelated-report-sentinel') -Message "$Label changed unrelated report state."
    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $Fixture.StateDir 'config\keep-config.json')) -eq '{"keep":true}') -Message "$Label changed unrelated config state."
    Assert-NoRuntimeTransactionResidue -Fixture $Fixture -Label $Label
    Assert-RuntimeTransactionTest -Condition (@(Get-ChildItem -LiteralPath $Fixture.StateDir -Filter 'install-state.failed-*.json' -File).Count -eq $ExpectedFailureReceiptCount) -Message "$Label failure-receipt count did not match expected $ExpectedFailureReceiptCount."
}

function Set-InterruptedRuntimeTransactionFixture {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $LivePayloadRoot
    )

    $stamp = '20260820-010101-001-a1b2c3d4'
    $runtimeRoot = Join-Path $Fixture.GameDir ('.dtmapi-runtime-install-' + $stamp)
    $stateTransactionRoot = Join-Path $Fixture.StateDir ('.runtime-install-transaction-' + $stamp)
    $recoveryPlugin = Join-Path $runtimeRoot 'recovery-plugin'
    $recoveryState = Join-Path $stateTransactionRoot 'recovery'
    New-Item -ItemType Directory -Force -Path $runtimeRoot, $recoveryState | Out-Null
    [System.IO.Directory]::Move($Fixture.PluginDir, $recoveryPlugin)
    New-Item -ItemType Directory -Force -Path $Fixture.PluginDir | Out-Null
    foreach ($fileName in $runtimeFiles) {
        Copy-Item -LiteralPath (Join-Path $LivePayloadRoot ('BepInEx\plugins\DTMAPI\' + $fileName)) -Destination (Join-Path $Fixture.PluginDir $fileName) -Force
    }
    [System.IO.Directory]::Move($Fixture.ToolsDir, (Join-Path $recoveryState 'tools'))
    Write-RuntimeTransactionText -Path (Join-Path $Fixture.ToolsDir 'interrupted-new-tool.ps1') -Text 'interrupted-new-tools'
    [System.IO.Directory]::Move($Fixture.ComponentsDir, (Join-Path $recoveryState 'components'))
    New-Item -ItemType Directory -Force -Path (Join-Path $Fixture.ComponentsDir 'compatibility') | Out-Null
    Copy-Item -LiteralPath (Join-Path $LivePayloadRoot ($compatibilityHostRelativePath.Replace('/', '\'))) -Destination (Join-Path $Fixture.ComponentsDir 'compatibility\DTMAPI.GameBridge.DolocTown.Compatibility.dll') -Force
    [System.IO.File]::Move((Join-Path $Fixture.StateDir 'release-manifest.json'), (Join-Path $recoveryState 'release-manifest.json'))
    [System.IO.File]::Move((Join-Path $Fixture.StateDir 'install-state.json'), (Join-Path $recoveryState 'install-state.json'))
    Write-RuntimeTransactionText -Path (Join-Path $Fixture.StateDir 'release-manifest.json') -Text '{"DTMAPIVersion":"interrupted-new"}'
    Write-RuntimeTransactionText -Path (Join-Path $Fixture.StateDir 'install-state.json') -Text '{"DTMAPIVersion":"interrupted-new"}'

    $candidateRoot = Join-Path $stateTransactionRoot 'candidate'
    $receipt = [ordered]@{
        SchemaVersion = 1
        GameDir = [System.IO.Path]::GetFullPath($Fixture.GameDir)
        StateDir = [System.IO.Path]::GetFullPath($Fixture.StateDir)
        PluginDir = [System.IO.Path]::GetFullPath($Fixture.PluginDir)
        Phase = 'InstallStateCommitted'
        FailedPhase = ''
        RuntimeTransactionRoot = [System.IO.Path]::GetFullPath($runtimeRoot)
        TransactionRoot = [System.IO.Path]::GetFullPath($stateTransactionRoot)
        CandidatePlugin = [System.IO.Path]::GetFullPath((Join-Path $runtimeRoot 'candidate-plugin'))
        RecoveryPlugin = [System.IO.Path]::GetFullPath($recoveryPlugin)
        CandidateTools = [System.IO.Path]::GetFullPath((Join-Path $candidateRoot 'tools'))
        CandidateComponents = [System.IO.Path]::GetFullPath((Join-Path $candidateRoot 'components'))
        CandidateReleaseManifest = [System.IO.Path]::GetFullPath((Join-Path $candidateRoot 'release-manifest.json'))
        CandidateInstallState = [System.IO.Path]::GetFullPath((Join-Path $candidateRoot 'install-state.json'))
        RecoveryState = [System.IO.Path]::GetFullPath($recoveryState)
        LiveTools = [System.IO.Path]::GetFullPath($Fixture.ToolsDir)
        LiveComponents = [System.IO.Path]::GetFullPath($Fixture.ComponentsDir)
        LiveReleaseManifest = [System.IO.Path]::GetFullPath((Join-Path $Fixture.StateDir 'release-manifest.json'))
        LiveInstallState = [System.IO.Path]::GetFullPath((Join-Path $Fixture.StateDir 'install-state.json'))
        OldPluginExisted = $true
        OldPluginMoved = $true
        CandidatePluginPlaced = $true
        OldToolsExisted = $true
        OldToolsMoved = $true
        CandidateToolsPlaced = $true
        OldComponentsExisted = $true
        OldComponentsMoved = $true
        CandidateComponentsPlaced = $true
        OldReleaseManifestExisted = $true
        OldReleaseManifestMoved = $true
        CandidateReleaseManifestPlaced = $true
        OldInstallStateExisted = $true
        OldInstallStateMoved = $true
        CandidateInstallStatePlaced = $true
        CommitSucceeded = $false
        RollbackSucceeded = $null
    }
    Write-Utf8NoBomJson -Path (Join-Path $runtimeRoot 'transaction.json') -Value $receipt
}

Assert-RuntimeBuildOutput
$tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$tempRoot = [System.IO.Path]::GetFullPath((Join-Path $tempBase ('DTMAPI Tx ' + $unicodePathSegment + ' ' + [Guid]::NewGuid().ToString('N').Substring(0, 8))))
Assert-RuntimeTransactionTest -Condition ($tempRoot.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase)) -Message "Temporary root escaped system temp: $tempRoot"
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

try {
    $payloadRoot = New-RuntimeTransactionPayload -Root (Join-Path $tempRoot ('Payload ' + $unicodePathSegment))
    $reliabilityCaseCount = 0

    $writeRetryFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R01 receipt write retry ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $writeRetryResult = Invoke-RuntimeTransactionInstaller -Fixture $writeRetryFixture -ReceiptFaultStage 'Write' -ReceiptFaultCount 2
    Assert-RuntimeTransactionTest -Condition ($writeRetryResult.ExitCode -eq 0) -Message "Two transient receipt-write failures did not recover on the third attempt. Output=$($writeRetryResult.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition (($writeRetryResult.Output -join "`n") -match 'DTM-S1001') -Message 'Receipt-write retry success omitted DTM-S1001.'
    Assert-RuntimeTransactionSuccess -Fixture $writeRetryFixture -Label 'Receipt write retry'
    $reliabilityCaseCount++

    $publishRetryFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R02 receipt publish cleanup retry ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $publishRetryResult = Invoke-RuntimeTransactionInstaller -Fixture $publishRetryFixture -ReceiptFaultStage 'Publish' -ReceiptFaultCount 2 -ReceiptCleanupFaultCount 2
    Assert-RuntimeTransactionTest -Condition ($publishRetryResult.ExitCode -eq 0) -Message "Transient publish plus best-effort cleanup failures did not recover. Output=$($publishRetryResult.Output -join ' | ')"
    Assert-RuntimeTransactionSuccess -Fixture $publishRetryFixture -Label 'Receipt publish and cleanup retry'
    $reliabilityCaseCount++

    $exhaustedFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R03 receipt exhaustion retry ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $exhaustedResult = Invoke-RuntimeTransactionInstaller -Fixture $exhaustedFixture -ReceiptFaultStage 'Publish' -ReceiptFaultCount 6 -ReceiptCleanupFaultCount 6
    Assert-RuntimeTransactionTest -Condition ($exhaustedResult.ExitCode -ne 0) -Message 'Six first-receipt failures unexpectedly succeeded.'
    $exhaustedText = $exhaustedResult.Output -join "`n"
    Assert-RuntimeTransactionTest -Condition ($exhaustedText -match 'DTM-E1301') -Message "Receipt exhaustion omitted DTM-E1301. Output=$($exhaustedResult.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition ($exhaustedText -match 'DTM-W1301') -Message "Receipt exhaustion did not report precise sterile-shell cleanup. Output=$($exhaustedResult.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $exhaustedFixture.PluginDir) -eq $exhaustedFixture.PluginBefore) -Message 'Receipt exhaustion changed the old Runtime.'
    Assert-RuntimeTransactionTest -Condition ((Get-RuntimeTransactionTreeFingerprint -Root $exhaustedFixture.ToolsDir) -eq $exhaustedFixture.ToolsBefore) -Message 'Receipt exhaustion changed the old tools.'
    Assert-NoRuntimeTransactionResidue -Fixture $exhaustedFixture -Label 'Receipt exhaustion cleanup'
    Assert-RuntimeTransactionTest -Condition (@(Get-ChildItem -LiteralPath $exhaustedFixture.StateDir -Filter 'install-state.failed-*.json' -File).Count -eq 0) -Message 'Receipt exhaustion wrote an ordinary failure-state file before the first transaction receipt existed.'
    $exhaustedRetryResult = Invoke-RuntimeTransactionInstaller -Fixture $exhaustedFixture
    Assert-RuntimeTransactionTest -Condition ($exhaustedRetryResult.ExitCode -eq 0) -Message "The clean retry after receipt exhaustion failed. Output=$($exhaustedRetryResult.Output -join ' | ')"
    Assert-RuntimeTransactionSuccess -Fixture $exhaustedFixture -Label 'Receipt exhaustion next retry' -ExpectedFailureReceiptCount 0
    $reliabilityCaseCount += 2

    $sterileFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R04 sterile empty ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $sterileRoot = New-RuntimeTransactionResidueRoot -Fixture $sterileFixture -Stamp '20260820-020201-001-11111111'
    $null = Assert-RuntimeTransactionStatusReadOnly -Fixture $sterileFixture -ExpectedPattern 'REPAIRABLE_STALE' -Label 'Empty sterile root'
    Assert-RuntimeTransactionTest -Condition (Test-Path -LiteralPath $sterileRoot -PathType Container) -Message 'Read-only status removed the empty sterile root.'
    $sterileResult = Invoke-RuntimeTransactionInstaller -Fixture $sterileFixture
    Assert-RuntimeTransactionTest -Condition ($sterileResult.ExitCode -eq 0 -and ($sterileResult.Output -join "`n") -match 'DTM-W1301') -Message "Install did not clean and continue from an empty sterile root. Output=$($sterileResult.Output -join ' | ')"
    Assert-RuntimeTransactionSuccess -Fixture $sterileFixture -Label 'Empty sterile root repair'
    $reliabilityCaseCount++

    $tempOnlyFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R05 sterile temp only ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $tempOnlyRoot = New-RuntimeTransactionResidueRoot -Fixture $tempOnlyFixture -Stamp '20260820-020202-002-22222222'
    Write-RuntimeTransactionText -Path (Join-Path $tempOnlyRoot 'transaction.json.tmp-0123456789abcdef0123456789abcdef') -Text '{partial'
    Write-RuntimeTransactionText -Path (Join-Path $tempOnlyRoot 'transaction.json.bak-fedcba9876543210fedcba9876543210') -Text '{old-partial'
    $tempOnlyResult = Invoke-RuntimeTransactionInstaller -Fixture $tempOnlyFixture
    Assert-RuntimeTransactionTest -Condition ($tempOnlyResult.ExitCode -eq 0 -and ($tempOnlyResult.Output -join "`n") -match 'DTM-W1301') -Message "Install did not repair a temp-only sterile root. Output=$($tempOnlyResult.Output -join ' | ')"
    Assert-RuntimeTransactionSuccess -Fixture $tempOnlyFixture -Label 'Temp-only sterile root repair'
    $reliabilityCaseCount++

    $unknownFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R06 unsafe unknown file ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $unknownRoot = New-RuntimeTransactionResidueRoot -Fixture $unknownFixture -Stamp '20260820-020203-003-33333333'
    Write-RuntimeTransactionText -Path (Join-Path $unknownRoot 'unknown.bin') -Text 'do-not-delete'
    $null = Assert-RuntimeTransactionStatusReadOnly -Fixture $unknownFixture -ExpectedPattern 'BLOCKED' -Label 'Unknown receiptless file'
    $unknownResult = Invoke-RuntimeTransactionInstaller -Fixture $unknownFixture
    Assert-RuntimeTransactionBlockedBeforeMutation -Fixture $unknownFixture -Result $unknownResult -Label 'Unknown receiptless file'
    Assert-RuntimeTransactionTest -Condition (Test-Path -LiteralPath (Join-Path $unknownRoot 'unknown.bin') -PathType Leaf) -Message 'Blocked install deleted the unknown receiptless file.'
    $reliabilityCaseCount++

    $candidateFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R07 unsafe candidate dir ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $candidateRoot = New-RuntimeTransactionResidueRoot -Fixture $candidateFixture -Stamp '20260820-020204-004-44444444'
    New-Item -ItemType Directory -Force -Path (Join-Path $candidateRoot 'candidate-plugin') | Out-Null
    $candidateResult = Invoke-RuntimeTransactionInstaller -Fixture $candidateFixture
    Assert-RuntimeTransactionBlockedBeforeMutation -Fixture $candidateFixture -Result $candidateResult -Label 'Receiptless candidate directory'
    $reliabilityCaseCount++

    $pairedStateFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R08 unsafe paired state ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $pairedStamp = '20260820-020205-005-55555555'
    $null = New-RuntimeTransactionResidueRoot -Fixture $pairedStateFixture -Stamp $pairedStamp
    New-Item -ItemType Directory -Force -Path (Join-Path $pairedStateFixture.StateDir ('.runtime-install-transaction-' + $pairedStamp)) | Out-Null
    $pairedStateResult = Invoke-RuntimeTransactionInstaller -Fixture $pairedStateFixture
    Assert-RuntimeTransactionBlockedBeforeMutation -Fixture $pairedStateFixture -Result $pairedStateResult -Label 'Receiptless root with paired state'
    $reliabilityCaseCount++

    $invalidReceiptFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R09 invalid receipt ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $invalidReceiptRoot = New-RuntimeTransactionResidueRoot -Fixture $invalidReceiptFixture -Stamp '20260820-020206-006-66666666'
    Write-RuntimeTransactionText -Path (Join-Path $invalidReceiptRoot 'transaction.json') -Text '{not-json'
    $invalidReceiptResult = Invoke-RuntimeTransactionInstaller -Fixture $invalidReceiptFixture
    Assert-RuntimeTransactionBlockedBeforeMutation -Fixture $invalidReceiptFixture -Result $invalidReceiptResult -Label 'Invalid final receipt'
    $reliabilityCaseCount++

    $orphanFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('R10 orphan state ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    New-Item -ItemType Directory -Force -Path (Join-Path $orphanFixture.StateDir '.runtime-install-transaction-20260820-020207-007-77777777') | Out-Null
    $null = Assert-RuntimeTransactionStatusReadOnly -Fixture $orphanFixture -ExpectedPattern 'BLOCKED' -Label 'Orphan state transaction'
    $orphanResult = Invoke-RuntimeTransactionInstaller -Fixture $orphanFixture
    Assert-RuntimeTransactionBlockedBeforeMutation -Fixture $orphanFixture -Result $orphanResult -Label 'Orphan state transaction'
    $reliabilityCaseCount++

    $faultPhases = @(
        'CandidatePrepared',
        'OldRuntimeMoved',
        'CandidatePlaced',
        'MovingOldComponents',
        'PlacingComponents',
        'ToolsCommitted',
        'ReleaseManifestCommitted',
        'InstallStateCommitted'
    )
    $caseNumber = 0
    foreach ($phase in $faultPhases) {
        $caseNumber++
        $fixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot (('{0:00} fail ' -f $caseNumber) + $unicodePathSegment)) -PayloadRoot $payloadRoot
        $result = Invoke-RuntimeTransactionInstaller -Fixture $fixture -FaultPhase $phase
        Assert-RuntimeTransactionTest -Condition ($result.ExitCode -ne 0) -Message "$phase fault unexpectedly succeeded. Output=$($result.Output -join ' | ')"
        Assert-RuntimeTransactionTest -Condition (($result.Output -join "`n") -match [regex]::Escape("Injected DTMAPI Runtime install transaction failure at phase $phase")) -Message "$phase fault output omitted the injected boundary. Output=$($result.Output -join ' | ')"
        Assert-RuntimeTransactionRollback -Fixture $fixture -Label $phase -ExpectedPhase $phase
    }

    $retryFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('07 retry ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $retryResult = Invoke-RuntimeTransactionInstaller -Fixture $retryFixture -FaultPhase 'InstallStateCommitted' -RollbackFaultPhase 'Tools'
    Assert-RuntimeTransactionTest -Condition ($retryResult.ExitCode -ne 0) -Message 'One-time rollback fault unexpectedly succeeded.'
    Assert-RuntimeTransactionTest -Condition (($retryResult.Output -join "`n") -match 'rollback also failed') -Message "One-time rollback fault output omitted the first rollback failure. Output=$($retryResult.Output -join ' | ')"
    Assert-RuntimeTransactionRollback -Fixture $retryFixture -Label 'One-time rollback retry' -ExpectedPhase 'InstallStateCommitted'

    $interruptedFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('08 interrupted ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    Set-InterruptedRuntimeTransactionFixture -Fixture $interruptedFixture -LivePayloadRoot $payloadRoot
    $null = Assert-RuntimeTransactionStatusReadOnly -Fixture $interruptedFixture -ExpectedPattern 'RECOVERY_PENDING' -Label 'Validated interrupted transaction'
    $interruptedBeforeUninstall = Get-RuntimeTransactionTreeFingerprint -Root $interruptedFixture.GameDir
    $interruptedUninstall = Invoke-RuntimeTransactionUninstaller -Fixture $interruptedFixture
    Assert-RuntimeTransactionTest -Condition ($interruptedUninstall.ExitCode -ne 0 -and ($interruptedUninstall.Output -join "`n") -match 'DTM-E1302') -Message "Uninstall did not refuse a validated recoverable transaction. Output=$($interruptedUninstall.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition ([string]::Equals($interruptedBeforeUninstall, (Get-RuntimeTransactionTreeFingerprint -Root $interruptedFixture.GameDir), [System.StringComparison]::Ordinal)) -Message 'Uninstall changed a validated recoverable transaction.'
    $interruptedResult = Invoke-RuntimeTransactionInstaller -Fixture $interruptedFixture -FaultPhase 'CandidatePrepared'
    Assert-RuntimeTransactionTest -Condition ($interruptedResult.ExitCode -ne 0) -Message 'Interrupted transaction recovery plus injected candidate failure unexpectedly succeeded.'
    Assert-RuntimeTransactionTest -Condition (($interruptedResult.Output -join "`n") -match 'DTM-W1302' -and ($interruptedResult.Output -join "`n") -match 'Recovered the previously interrupted Runtime transaction') -Message "Interrupted transaction recovery output omitted the recovery boundary. Output=$($interruptedResult.Output -join ' | ')"
    Assert-RuntimeTransactionRollback -Fixture $interruptedFixture -Label 'Interrupted transaction restart recovery' -ExpectedPhase 'CandidatePrepared'

    $missingPayload = New-RuntimeTransactionPayload -Root (Join-Path $tempRoot ('Missing ' + $unicodePathSegment))
    Remove-Item -LiteralPath (Join-Path $missingPayload 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll') -Force
    $missingFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('09 missing ' + $unicodePathSegment)) -PayloadRoot $missingPayload
    $missingResult = Invoke-RuntimeTransactionInstaller -Fixture $missingFixture
    Assert-RuntimePackagePreflightFailure `
        -Fixture $missingFixture `
        -Result $missingResult `
        -Label 'Missing packaged Runtime DLL' `
        -ExpectedError 'DTMAPI Runtime candidate DLL set is not exact.'

    $corruptPayload = New-RuntimeTransactionPayload -Root (Join-Path $tempRoot ('Corrupt ' + $unicodePathSegment))
    Write-RuntimeTransactionText -Path (Join-Path $corruptPayload 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll') -Text 'not-a-managed-assembly'
    $corruptFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('10 corrupt ' + $unicodePathSegment)) -PayloadRoot $corruptPayload
    $corruptResult = Invoke-RuntimeTransactionInstaller -Fixture $corruptFixture
    Assert-RuntimePackagePreflightFailure `
        -Fixture $corruptFixture `
        -Result $corruptResult `
        -Label 'Corrupt packaged Runtime DLL' `
        -ExpectedError 'DTMAPI Runtime candidate assembly is not a valid managed DLL:'

    $successFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('11 success ' + $unicodePathSegment)) -PayloadRoot $payloadRoot
    $successResult = Invoke-RuntimeTransactionInstaller -Fixture $successFixture
    Assert-RuntimeTransactionTest -Condition ($successResult.ExitCode -eq 0) -Message "Successful upgrade failed. Output=$($successResult.Output -join ' | ')"
    Assert-RuntimeTransactionSuccess -Fixture $successFixture -Label 'Successful upgrade'
    $uninstallSterileRoot = New-RuntimeTransactionResidueRoot -Fixture $successFixture -Stamp '20260820-020208-008-88888888'
    $null = Assert-RuntimeTransactionStatusReadOnly -Fixture $successFixture -ExpectedPattern 'REPAIRABLE_STALE' -Label 'Sterile root before uninstall'
    $uninstallResult = Invoke-RuntimeTransactionUninstaller -Fixture $successFixture
    Assert-RuntimeTransactionTest -Condition ($uninstallResult.ExitCode -eq 0) -Message "Installed Runtime uninstall failed. Output=$($uninstallResult.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition (($uninstallResult.Output -join "`n") -match 'DTM-W1301' -and ($uninstallResult.Output -join "`n") -match 'DTM-S2001') -Message "Uninstall did not report sterile cleanup and success. Output=$($uninstallResult.Output -join ' | ')"
    Assert-RuntimeTransactionTest -Condition (-not (Test-Path -LiteralPath $uninstallSterileRoot)) -Message 'Uninstall retained the sterile Runtime root.'
    Assert-RuntimeTransactionTest -Condition (-not (Test-Path -LiteralPath $successFixture.ComponentsDir)) -Message 'Runtime uninstall retained the optional-component directory.'
    Assert-RuntimeTransactionTest -Condition (-not (Test-Path -LiteralPath $successFixture.PluginDir)) -Message 'Runtime uninstall retained the mandatory Runtime directory.'
    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $successFixture.StateDir 'reports\keep-report.txt')) -eq 'unrelated-report-sentinel') -Message 'Runtime uninstall changed unrelated reports.'
    Assert-RuntimeTransactionTest -Condition ([System.IO.File]::ReadAllText((Join-Path $successFixture.StateDir 'config\keep-config.json')) -eq '{"keep":true}') -Message 'Runtime uninstall changed unrelated configs.'

    $explicitPayload = New-RuntimeTransactionPayload -Root (Join-Path $tempRoot ('Explicit ' + $unicodePathSegment))
    $explicitFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('12 explicit ' + $unicodePathSegment)) -PayloadRoot $explicitPayload
    $explicitResult = Invoke-RuntimeTransactionInstaller -Fixture $explicitFixture -ExplicitPayloadRoot
    Assert-RuntimeTransactionTest -Condition ($explicitResult.ExitCode -eq 0) -Message "Explicit packaged payload failed. Output=$($explicitResult.Output -join ' | ')"
    Assert-RuntimeTransactionSuccess -Fixture $explicitFixture -Label 'Explicit packaged payload'

    $missingManifestPayload = New-RuntimeTransactionPayload -Root (Join-Path $tempRoot ('Missing manifest package ' + $unicodePathSegment)) -WithoutReleaseManifest
    $missingManifestFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('13 missing manifest ' + $unicodePathSegment)) -PayloadRoot $missingManifestPayload
    $missingManifestResult = Invoke-RuntimeTransactionInstaller -Fixture $missingManifestFixture -ExplicitPayloadRoot
    Assert-RuntimePackagePreflightFailure `
        -Fixture $missingManifestFixture `
        -Result $missingManifestResult `
        -Label 'Missing packaged release manifest' `
        -ExpectedError 'Packaged DTMAPI release manifest is missing:'

    $damagedManifestPayload = New-RuntimeTransactionPayload -Root (Join-Path $tempRoot ('Damaged manifest package ' + $unicodePathSegment))
    Write-RuntimeTransactionText -Path (Get-RuntimeTransactionPackageManifestPath -PayloadRoot $damagedManifestPayload) -Text '{not-valid-json'
    $damagedManifestFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('14 damaged manifest ' + $unicodePathSegment)) -PayloadRoot $damagedManifestPayload
    $damagedManifestResult = Invoke-RuntimeTransactionInstaller -Fixture $damagedManifestFixture
    Assert-RuntimePackagePreflightFailure `
        -Fixture $damagedManifestFixture `
        -Result $damagedManifestResult `
        -Label 'Damaged packaged release manifest' `
        -ExpectedError 'Packaged DTMAPI release manifest is unreadable:'

    $receiptMismatchPayload = New-RuntimeTransactionPayload -Root (Join-Path $tempRoot ('Receipt mismatch package ' + $unicodePathSegment))
    $receiptMismatchCore = Join-Path $receiptMismatchPayload 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'
    $appendBytes = [System.Text.Encoding]::UTF8.GetBytes('same-version-payload-replacement')
    $appendStream = [System.IO.File]::Open($receiptMismatchCore, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
    try {
        $appendStream.Position = $appendStream.Length
        $appendStream.Write($appendBytes, 0, $appendBytes.Length)
    }
    finally {
        $appendStream.Dispose()
    }
    $receiptMismatchFixture = New-RuntimeTransactionFixture -Root (Join-Path $tempRoot ('15 receipt mismatch ' + $unicodePathSegment)) -PayloadRoot $receiptMismatchPayload
    $receiptMismatchResult = Invoke-RuntimeTransactionInstaller -Fixture $receiptMismatchFixture -ExplicitPayloadRoot
    Assert-RuntimePackagePreflightFailure `
        -Fixture $receiptMismatchFixture `
        -Result $receiptMismatchResult `
        -Label 'Same-version packaged DLL replacement' `
        -ExpectedError 'Packaged DTMAPI release manifest payload receipt mismatch for DTMAPI.Core.dll.'

    if (-not $Quiet) {
        $totalCases = $faultPhases.Count + 9 + $reliabilityCaseCount
        Write-Host "DTMAPI Runtime upgrade transaction child matrix: OK (host=$((Get-RuntimeTransactionPowerShellHost)) cases=$totalCases)"
    }
}
finally {
    if ((Test-Path -LiteralPath $tempRoot) -and
        ([System.IO.Path]::GetFullPath($tempRoot).StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase))) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
