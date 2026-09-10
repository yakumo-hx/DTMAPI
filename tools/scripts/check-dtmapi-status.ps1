param(
    [string] $GameDir = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($GameDir)) {
    try {
        $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo
    }
    catch {
        Write-DtmApiInstallerFailure -ErrorRecord $_
        exit 1
    }
}
else {
    try {
        $GameDir = Resolve-DtmApiExplicitGamePath -Path $GameDir -Source '-GameDir'
    }
    catch {
        Write-DtmApiInstallerFailure -ErrorRecord $_
        exit 1
    }
}

$stateDir = Resolve-DtmApiStateDir -GameDir $GameDir
$pluginDir = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
$doorstopDll = Join-Path $GameDir 'winhttp.dll'
$doorstopConfig = Join-Path $GameDir 'doorstop_config.ini'
$bepInExCore = Join-Path $GameDir 'BepInEx\core\BepInEx.dll'
$titleIconAsset = Join-Path $pluginDir 'assets\branding\dtmapi-icon.png'
$installStatePath = Join-Path $stateDir 'install-state.json'
$releaseManifestPath = Join-Path $stateDir 'release-manifest.json'
$latestLog = Join-Path $stateDir 'logs\latest.log'
$latestReportPointer = Join-Path $stateDir 'reports\latest-report.txt'
$latestReport = ''
$packagedPlayerDoctorExe = Join-Path $PSScriptRoot 'player-doctor\dtmapi-player-doctor.exe'
$installedPlayerDoctorRoot = Join-Path $stateDir 'tools\player-doctor'
$installedPlayerDoctorExe = Join-Path $installedPlayerDoctorRoot 'dtmapi-player-doctor.exe'
$playerDoctorExe = ''
$playerDoctorSource = ''
$sourceToolsRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'tools\scripts')).TrimEnd('\', '/')
$currentToolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\', '/')
$playerDoctorCandidates = New-Object 'System.Collections.Generic.List[object]'
$playerDoctorCandidates.Add([pscustomobject]@{ Path = $packagedPlayerDoctorExe; Source = 'optional-support-package' }) | Out-Null
$playerDoctorCandidates.Add([pscustomobject]@{ Path = $installedPlayerDoctorExe; Source = 'legacy-installed-runtime' }) | Out-Null
if ([string]::Equals($currentToolsRoot, $sourceToolsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    $playerDoctorCandidates.Add([pscustomobject]@{
        Path = Join-Path $repo 'dist\player-doctor\win-x64\dtmapi-player-doctor.exe'
        Source = 'developer-build'
    }) | Out-Null
}
foreach ($candidate in $playerDoctorCandidates) {
    if (Test-Path -LiteralPath $candidate.Path -PathType Leaf) {
        $playerDoctorExe = [string]$candidate.Path
        $playerDoctorSource = [string]$candidate.Source
        break
    }
}
if (Test-Path $latestReportPointer) {
    $latestReport = (Get-Content -Raw -LiteralPath $latestReportPointer).Trim()
}
$legacyDetections = @(Get-DtmApiLegacyDetections -GameDir $GameDir)
$legacyOfficialLocalMetadata = @(Get-DtmApiLegacyOfficialLocalMetadata)
$script:DtmRequiredMissing = 0
$script:DtmRequiredInvalid = 0
$script:DtmOptionalMissing = 0
$script:DtmUpdateRequired = 0
$script:DtmTransactionStatus = 'Clean'
try {
    $transactionClassifications = @(Get-DtmApiRuntimeTransactionClassifications -GameDir $GameDir -StateDir $stateDir -PluginDir $pluginDir)
}
catch {
    Write-DtmApiInstallerFailure -ErrorRecord $_
    exit 1
}

function Write-DtmStatusLine {
    param(
        [Parameter(Mandatory = $true)] [string] $Tag,
        [Parameter(Mandatory = $true)] [string] $Message,
        [ConsoleColor] $Color = [ConsoleColor]::Gray
    )

    Write-Host ("[{0}] {1}" -f $Tag, $Message) -ForegroundColor $Color
}

function Write-DtmStatusDetail {
    param([string] $Text)

    if (-not [string]::IsNullOrWhiteSpace($Text)) {
        Write-Host ("     {0}" -f $Text)
    }
}

function Write-DtmPathStatus {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Path,
        [ValidateSet('Any', 'Container', 'Leaf')] [string] $PathType = 'Any',
        [switch] $Required
    )

    $exists = Test-Path -LiteralPath $Path -PathType $PathType
    if ($exists) {
        Write-DtmStatusLine -Tag 'OK' -Message $Label -Color Green
    }
    else {
        if ($Required) {
            Write-DtmStatusLine -Tag 'MISSING' -Message $Label -Color Red
            $script:DtmRequiredMissing++
        }
        else {
            Write-DtmStatusLine -Tag 'INFO' -Message ("{0}: not present (optional)" -f $Label) -Color Cyan
            $script:DtmOptionalMissing++
        }
    }
    Write-DtmStatusDetail -Text $Path
    return $exists
}

function Test-DtmPathPresent {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [ValidateSet('Any', 'Container', 'Leaf')] [string] $PathType = 'Any'
    )

    if (-not (Test-Path -LiteralPath $Path -PathType $PathType)) {
        return $false
    }

    if ($PathType -eq 'Leaf') {
        try {
            return (Get-Item -LiteralPath $Path).Length -gt 0
        }
        catch {
            return $false
        }
    }

    return $true
}

function Read-DtmStatusJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [switch] $Required
    )

    try {
        return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    }
    catch {
        Write-DtmStatusLine -Tag 'WARN' -Message "Could not parse JSON: $Path" -Color Yellow
        Write-DtmStatusDetail -Text $_.Exception.Message
        if ($Required) {
            $script:DtmRequiredInvalid++
        }
        return $null
    }
}

function Get-DtmLatestStateFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Directory,
        [Parameter(Mandatory = $true)] [string] $Filter
    )

    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $Directory -Filter $Filter -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Convert-DtmStatusTimestamp {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    try {
        return [datetimeoffset]::Parse(
            $Value,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [System.Globalization.DateTimeStyles]::AssumeUniversal)
    }
    catch {
        return $null
    }
}

function Compare-DtmInstalledVersion {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [string] $Actual,
        [Parameter(Mandatory = $true)] [string] $Expected
    )

    if ([string]::IsNullOrWhiteSpace($Actual)) {
        Write-DtmStatusLine -Tag 'WARN' -Message ("{0} does not record a version; expected {1}." -f $Label, $Expected) -Color Yellow
        $script:DtmRequiredInvalid++
        return
    }
    if (-not [string]::Equals($Actual.Trim(), $Expected, [System.StringComparison]::Ordinal)) {
        Write-DtmStatusLine -Tag 'UPDATE' -Message ("{0} is {1}; Workshop package expects {2}. Run 1_install_dtmapi.bat." -f $Label, $Actual.Trim(), $Expected) -Color Yellow
        $script:DtmUpdateRequired++
        return
    }

    Write-DtmStatusLine -Tag 'OK' -Message ("{0}: {1}" -f $Label, $Expected) -Color Green
}

function Test-DtmStatusVersionRequiresCompatibilityHost {
    param([string] $Version)

    if ([string]::IsNullOrWhiteSpace($Version)) {
        return $false
    }
    $numeric = (([string]$Version).Trim() -split '[-+]', 2)[0]
    $parsed = $null
    if (-not [System.Version]::TryParse($numeric, [ref]$parsed)) {
        return $false
    }
    return $parsed -ge [System.Version]'0.5.5'
}

function Test-DtmInstalledOptionalComponents {
    param(
        $InstallState,
        $ReleaseManifest
    )

    if ($null -eq $InstallState -or $null -eq $ReleaseManifest) {
        return
    }
    $stateHasProjection = $null -ne $InstallState.PSObject.Properties['OptionalComponents']
    $releaseHasProjection = $null -ne $ReleaseManifest.PSObject.Properties['OptionalComponents']
    $hostRequired =
        (Test-DtmStatusVersionRequiresCompatibilityHost -Version ([string](Get-DtmApiMapValue -Map $InstallState -Key 'DTMAPIVersion' -Default ''))) -or
        (Test-DtmStatusVersionRequiresCompatibilityHost -Version ([string](Get-DtmApiMapValue -Map $ReleaseManifest -Key 'DTMAPIVersion' -Default '')))
    if (-not $stateHasProjection -and -not $releaseHasProjection) {
        if ($hostRequired) {
            Write-DtmStatusLine -Tag 'INVALID' -Message 'DTMAPI 0.5.5 and later require both receipts to project exactly one dormant Compatibility Host.' -Color Red
            $script:DtmRequiredInvalid++
        }
        return
    }
    $releaseRows = @((Get-DtmApiMapValue -Map $ReleaseManifest -Key 'OptionalComponents' -Default @()))
    $stateRows = @((Get-DtmApiMapValue -Map $InstallState -Key 'OptionalComponents' -Default @()))
    if ($stateHasProjection -ne $releaseHasProjection -or
        $releaseRows.Count -ne $stateRows.Count -or
        ($hostRequired -and ($releaseRows.Count -ne 1 -or $stateRows.Count -ne 1)) -or
        (-not $hostRequired -and $releaseRows.Count -eq 0)) {
        Write-DtmStatusLine -Tag 'INVALID' -Message ("Optional component receipts are missing or inconsistent. Release={0} State={1}" -f $releaseRows.Count, $stateRows.Count) -Color Red
        $script:DtmRequiredInvalid++
        return
    }

    $seenIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    $seenPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $stateRootFull = [System.IO.Path]::GetFullPath($stateDir).TrimEnd('\')
    foreach ($releaseRow in $releaseRows) {
        $componentId = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'ComponentId' -Default '')
        $relativePath = ([string](Get-DtmApiMapValue -Map $releaseRow -Key 'RelativePath' -Default '')).Replace('\', '/')
        $expectedHash = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'Sha256' -Default '')
        $expectedLength = [long](Get-DtmApiMapValue -Map $releaseRow -Key 'Length' -Default 0)
        $assemblyName = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'AssemblyName' -Default '')
        $assemblyVersion = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'AssemblyVersion' -Default '')
        $fileVersion = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'FileVersion' -Default '')
        $targetFramework = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'TargetFramework' -Default '')
        $distribution = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'Distribution' -Default '')
        $loadPolicy = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'LoadPolicy' -Default '')
        $defaultLoadState = [string](Get-DtmApiMapValue -Map $releaseRow -Key 'DefaultLoadState' -Default '')
        $downloadIncluded = [bool](Get-DtmApiMapValue -Map $releaseRow -Key 'IncludedInDownloadPackage' -Default $false)
        if (-not [string]::Equals($componentId, 'gamebridge-compatibility-host', [System.StringComparison]::Ordinal) -or
            -not $seenIds.Add($componentId) -or
            -not [string]::Equals($relativePath, 'DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll', [System.StringComparison]::Ordinal) -or
            -not $seenPaths.Add($relativePath) -or
            $relativePath.Contains('../') -or $relativePath.Contains('/..') -or
            $expectedHash -notmatch '^[0-9a-fA-F]{64}$' -or $expectedLength -le 0 -or
            -not [string]::Equals($assemblyName, 'DTMAPI.GameBridge.DolocTown.Compatibility', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($assemblyVersion, '0.5.3.0', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($targetFramework, 'netstandard2.0', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($distribution, 'dormant-shipped', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($loadPolicy, 'first-frozen-abi-call', [System.StringComparison]::Ordinal) -or
            -not [string]::Equals($defaultLoadState, 'dormant', [System.StringComparison]::Ordinal) -or
            -not $downloadIncluded) {
            Write-DtmStatusLine -Tag 'INVALID' -Message ("Optional component receipt is unsafe or incomplete: {0}" -f $componentId) -Color Red
            $script:DtmRequiredInvalid++
            continue
        }

        $stateMatches = @($stateRows | Where-Object { [string]$_.ComponentId -eq $componentId })
        $expectedPath = [System.IO.Path]::GetFullPath((Join-Path $GameDir ($relativePath.Replace('/', '\'))))
        if (-not $expectedPath.StartsWith($stateRootFull + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
            $stateMatches.Count -ne 1 -or
            -not [string]::Equals([string]$stateMatches[0].RelativePath, $relativePath, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateMatches[0].Path, $expectedPath, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals([string]$stateMatches[0].Sha256, $expectedHash, [System.StringComparison]::OrdinalIgnoreCase) -or
            [long]$stateMatches[0].Length -ne $expectedLength -or
            -not [string]::Equals([string]$stateMatches[0].AssemblyName, $assemblyName, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateMatches[0].AssemblyVersion, $assemblyVersion, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateMatches[0].FileVersion, $fileVersion, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateMatches[0].TargetFramework, $targetFramework, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateMatches[0].Distribution, $distribution, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateMatches[0].LoadPolicy, $loadPolicy, [System.StringComparison]::Ordinal) -or
            -not [string]::Equals([string]$stateMatches[0].DefaultLoadState, $defaultLoadState, [System.StringComparison]::Ordinal) -or
            [bool]$stateMatches[0].IncludedInDownloadPackage -ne $downloadIncluded) {
            Write-DtmStatusLine -Tag 'INVALID' -Message ("Optional component install-state projection mismatch: {0}" -f $componentId) -Color Red
            $script:DtmRequiredInvalid++
            continue
        }
        if (-not (Test-Path -LiteralPath $expectedPath -PathType Leaf)) {
            Write-DtmStatusLine -Tag 'MISSING' -Message ("Optional component is missing: {0}" -f $componentId) -Color Red
            Write-DtmStatusDetail -Text $expectedPath
            $script:DtmRequiredMissing++
            continue
        }
        try {
            $item = Get-Item -LiteralPath $expectedPath -ErrorAction Stop
            $actualHash = Get-DtmApiFileSha256 -Path $expectedPath
            $managedName = [System.Reflection.AssemblyName]::GetAssemblyName($expectedPath)
            $actualFileVersion = [string]([System.Diagnostics.FileVersionInfo]::GetVersionInfo($expectedPath).FileVersion)
            if ([long]$item.Length -ne $expectedLength -or
                -not [string]::Equals($actualHash, $expectedHash, [System.StringComparison]::OrdinalIgnoreCase) -or
                -not [string]::Equals([string]$managedName.Name, $assemblyName, [System.StringComparison]::Ordinal) -or
                -not [string]::Equals([string]$managedName.Version, $assemblyVersion, [System.StringComparison]::Ordinal) -or
                -not [string]::Equals($actualFileVersion, $fileVersion, [System.StringComparison]::Ordinal)) {
                throw 'length/hash/managed identity/version mismatch'
            }
            Write-DtmStatusLine -Tag 'OK' -Message ("Optional framework component: {0} ({1}; {2})" -f $componentId, $distribution, $loadPolicy) -Color Green
            Write-DtmStatusDetail -Text $expectedPath
        }
        catch {
            Write-DtmStatusLine -Tag 'INVALID' -Message ("Optional component bytes do not match their receipts: {0}" -f $componentId) -Color Red
            Write-DtmStatusDetail -Text $_.Exception.Message
            $script:DtmRequiredInvalid++
        }
    }
}

Write-Host "DTMAPI status"
Write-DtmStatusLine -Tag 'INFO' -Message "Game folder: $GameDir" -Color Cyan
Write-DtmStatusLine -Tag 'INFO' -Message "State folder: $stateDir" -Color Cyan
Write-Host ""

Write-DtmStatusLine -Tag 'INFO' -Message 'Runtime transaction state (read-only)' -Color Cyan
if ($transactionClassifications.Count -eq 0) {
    Write-DtmStatusLine -Tag 'OK' -Message 'No Runtime transaction residue detected.' -Color Green
}
else {
    foreach ($classification in $transactionClassifications) {
        $path = if (-not [string]::IsNullOrWhiteSpace([string]$classification.RuntimeRoot)) { [string]$classification.RuntimeRoot } else { [string]$classification.StateRoot }
        switch ([string]$classification.Kind) {
            'RecoverableReceipt' {
                if (-not [string]::Equals($script:DtmTransactionStatus, 'Blocked', [System.StringComparison]::Ordinal)) {
                    $script:DtmTransactionStatus = 'RecoveryPending'
                }
                Write-DtmApiInstallerMessage `
                    -Code 'DTM-E1302' `
                    -Chinese '发现可恢复的中断事务；检查器不会修改文件。请运行 1_install_dtmapi.bat 完成恢复。' `
                    -English 'A recoverable interrupted transaction exists; the checker will not modify files. Run 1_install_dtmapi.bat to recover it.' `
                    -Detail $path `
                    -Level Error
            }
            'SterileNoReceipt' {
                if ([string]::Equals($script:DtmTransactionStatus, 'Clean', [System.StringComparison]::Ordinal)) {
                    $script:DtmTransactionStatus = 'RepairableStale'
                }
                Write-DtmApiInstallerMessage `
                    -Code 'DTM-W1301' `
                    -Chinese '发现可安全修复的无凭据事务空壳；检查器保持只读。运行安装或卸载脚本即可精准清理。' `
                    -English 'A safely repairable receiptless transaction shell exists; the checker remains read-only. Run install or uninstall to remove it precisely.' `
                    -Detail $path `
                    -Level Warning
            }
            default {
                $script:DtmTransactionStatus = 'Blocked'
                Write-DtmApiInstallerMessage `
                    -Code 'DTM-E1303' `
                    -Chinese '发现不安全或无法验证的 Runtime 事务残留；检查器不会删除它。' `
                    -English 'Unsafe or unverifiable Runtime transaction residue exists; the checker will not delete it.' `
                    -Detail "Kind=$($classification.Kind); Path=$path; $($classification.Detail)" `
                    -Level Error
            }
        }
    }
}
Write-Host ""

Write-DtmStatusLine -Tag 'INFO' -Message 'Required install files' -Color Cyan
$bepInExRequiredFiles = @(Get-DtmApiBepInExRequiredInstallFiles -GameDir $GameDir)
$missingBepInExFiles = @($bepInExRequiredFiles | Where-Object { -not (Test-DtmPathPresent -Path $_.Path -PathType $_.PathType) })
if ($missingBepInExFiles.Count -eq 0 -and (Test-DtmApiDoorstopConfigEnabledForBepInEx -Path $doorstopConfig)) {
    Write-DtmStatusLine -Tag 'OK' -Message 'BepInEx/Doorstop runtime' -Color Green
}
else {
    Write-DtmStatusLine -Tag 'MISSING' -Message ("BepInEx/Doorstop runtime incomplete: {0}/{1} required files missing." -f $missingBepInExFiles.Count, $bepInExRequiredFiles.Count) -Color Red
    $script:DtmRequiredMissing++
    foreach ($bepInExFile in ($missingBepInExFiles | Select-Object -First 8)) {
        Write-DtmStatusDetail -Text ("{0}: {1}" -f $bepInExFile.Label, $bepInExFile.Path)
    }
    if ($missingBepInExFiles.Count -gt 8) {
        Write-DtmStatusDetail -Text ("... and {0} more BepInEx/Doorstop files." -f ($missingBepInExFiles.Count - 8))
    }
}
if (Test-Path -LiteralPath $doorstopConfig -PathType Leaf) {
    if (Test-DtmApiDoorstopConfigEnabledForBepInEx -Path $doorstopConfig) {
        Write-DtmStatusLine -Tag 'OK' -Message 'Doorstop config enables BepInEx preloader.' -Color Green
    }
    else {
        Write-DtmStatusLine -Tag 'WARN' -Message 'Doorstop config does not enable BepInEx preloader.' -Color Yellow
        $script:DtmRequiredInvalid++
    }
}
$dtmApiRuntimeRequiredFiles = @(
    [pscustomobject]@{ Label = 'DTMAPI plugin folder'; Path = $pluginDir; PathType = 'Container' }
)
$dtmApiRuntimeRequiredFiles += @(
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Abstractions.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    ) | ForEach-Object { [pscustomobject]@{ Label = $_; Path = (Join-Path $pluginDir $_); PathType = 'Leaf' } }
$missingDtmApiRuntimeFiles = @($dtmApiRuntimeRequiredFiles | Where-Object { -not (Test-DtmPathPresent -Path $_.Path -PathType $_.PathType) })
if ($missingDtmApiRuntimeFiles.Count -eq 0) {
    Write-DtmStatusLine -Tag 'OK' -Message 'DTMAPI runtime plugin' -Color Green
}
else {
    Write-DtmStatusLine -Tag 'MISSING' -Message ("DTMAPI runtime plugin incomplete: {0}/{1} required files missing." -f $missingDtmApiRuntimeFiles.Count, $dtmApiRuntimeRequiredFiles.Count) -Color Red
    $script:DtmRequiredMissing++
    foreach ($runtimeFile in $missingDtmApiRuntimeFiles) {
        Write-DtmStatusDetail -Text ("{0}: {1}" -f $runtimeFile.Label, $runtimeFile.Path)
    }
}
$expectedRuntimeDllNames = @($dtmApiRuntimeRequiredFiles | Where-Object { $_.PathType -eq 'Leaf' } | ForEach-Object { [System.IO.Path]::GetFileName($_.Path) } | Sort-Object)
$actualRuntimeDllNames = if (Test-Path -LiteralPath $pluginDir -PathType Container) {
    @(Get-ChildItem -LiteralPath $pluginDir -File -Filter '*.dll' -Recurse | ForEach-Object { $_.Name } | Sort-Object)
}
else {
    @()
}
if (($actualRuntimeDllNames -join '|') -eq ($expectedRuntimeDllNames -join '|')) {
    Write-DtmStatusLine -Tag 'OK' -Message 'Installed Runtime DLL set is the exact five production assemblies.' -Color Green
}
elseif (@($actualRuntimeDllNames).Count -gt 0) {
    Write-DtmStatusLine -Tag 'INVALID' -Message 'Installed Runtime DLL set is not the exact five production assemblies.' -Color Red
    Write-DtmStatusDetail -Text ("Expected: {0}" -f ($expectedRuntimeDllNames -join ', '))
    Write-DtmStatusDetail -Text ("Actual: {0}" -f ($actualRuntimeDllNames -join ', '))
    $script:DtmRequiredInvalid++
}

$qaHostResidue = New-Object 'System.Collections.Generic.List[string]'
if (Test-Path -LiteralPath $pluginDir -PathType Container) {
    foreach ($entry in @(Get-ChildItem -LiteralPath $pluginDir -Recurse -Force | Where-Object {
        $_.Name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
        $_.Name -match '^(?i:qa-settings\.json|qa-host.*\.json)$' -or
        $_.Name.Equals('qa-host', [System.StringComparison]::OrdinalIgnoreCase)
    })) {
        $qaHostResidue.Add($entry.FullName) | Out-Null
    }
}
if (Test-Path -LiteralPath $stateDir -PathType Container) {
    foreach ($rootEntry in @(Get-ChildItem -LiteralPath $stateDir -Force | Where-Object { $_.Name -notlike '.runtime-install-transaction-*' })) {
        $entries = if ($rootEntry.PSIsContainer -and ($rootEntry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0) {
            @($rootEntry) + @(Get-ChildItem -LiteralPath $rootEntry.FullName -Recurse -Force)
        }
        else {
            @($rootEntry)
        }
        foreach ($entry in @($entries | Where-Object {
            $_.Name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
            $_.Name -match '^(?i:qa-settings\.json|qa-host.*\.json)$' -or
            $_.Name.Equals('qa-host', [System.StringComparison]::OrdinalIgnoreCase)
        })) {
            $qaHostResidue.Add($entry.FullName) | Out-Null
        }
    }
}
if ($qaHostResidue.Count -gt 0) {
    Write-DtmStatusLine -Tag 'INVALID' -Message ("Developer-only QA host residue is present: {0} item(s)." -f $qaHostResidue.Count) -Color Red
    foreach ($path in @($qaHostResidue.ToArray() | Sort-Object -Unique)) {
        Write-DtmStatusDetail -Text $path
    }
    Write-DtmStatusDetail -Text 'Do not load, move, or delete unverified files; use the originating runner recovery receipt.'
    $script:DtmRequiredInvalid++
}
Write-DtmPathStatus -Label 'DTMAPI title icon asset' -Path $titleIconAsset -PathType Leaf | Out-Null
$hasInstallFootprint = ($missingDtmApiRuntimeFiles.Count -lt $dtmApiRuntimeRequiredFiles.Count) -or
    (Test-Path -LiteralPath $installStatePath -PathType Leaf) -or
    (Test-Path -LiteralPath $releaseManifestPath -PathType Leaf)
if ($hasInstallFootprint) {
    Write-DtmPathStatus -Label 'Install state' -Path $installStatePath -PathType Leaf -Required | Out-Null
    Write-DtmPathStatus -Label 'Release manifest' -Path $releaseManifestPath -PathType Leaf -Required | Out-Null
}
else {
    Write-DtmStatusLine -Tag 'INFO' -Message 'Install state and release manifest: skipped because DTMAPI is not installed.' -Color Cyan
}
$requiredHelperScriptNames = @(
    'common.ps1',
    'release-common.ps1',
    'uninstall-dtmapi.ps1',
    'check-dtmapi-status.ps1'
)
$diagnosticHelperScriptNames = @(
    'collect-logs.ps1',
    'analyze-startup-evidence.ps1'
)
$installedRequiredHelperScripts = @($requiredHelperScriptNames | ForEach-Object { Join-Path (Join-Path $stateDir 'tools') $_ })
$installedDiagnosticHelperScripts = @($diagnosticHelperScriptNames | ForEach-Object { Join-Path (Join-Path $stateDir 'tools') $_ })
$installedVersionAuthority = Join-Path (Join-Path $stateDir 'tools') 'dtmapi-runtime-version.props'
if ($hasInstallFootprint) {
    Write-DtmPathStatus -Label 'Installed helper scripts folder' -Path (Join-Path $stateDir 'tools') -PathType Container | Out-Null
    if (Test-Path -LiteralPath $installedVersionAuthority -PathType Leaf) {
        Write-DtmStatusLine -Tag 'OK' -Message 'Installed Runtime version authority' -Color Green
        Write-DtmStatusDetail -Text $installedVersionAuthority
    }
    else {
        Write-DtmStatusLine -Tag 'INFO' -Message 'Installed Runtime version authority is absent; version-state classification follows below.' -Color Cyan
        Write-DtmStatusDetail -Text $installedVersionAuthority
    }
    foreach ($helperScript in $installedRequiredHelperScripts) {
        Write-DtmPathStatus -Label ("Installed helper {0}" -f [System.IO.Path]::GetFileName($helperScript)) -Path $helperScript -PathType Leaf | Out-Null
    }
    foreach ($helperScript in $installedDiagnosticHelperScripts) {
        Write-DtmPathStatus -Label ("Installed diagnostic helper {0}" -f [System.IO.Path]::GetFileName($helperScript)) -Path $helperScript -PathType Leaf | Out-Null
    }
    $existingHelperScripts = @($installedRequiredHelperScripts + $installedDiagnosticHelperScripts | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })
    if ($existingHelperScripts.Count -gt 0) {
        $helperSyntaxResults = @(Test-DtmApiWindowsPowerShellSyntax -Paths $existingHelperScripts -WarningOnly -PassThru -AllowCoreFallback)
        $helperSyntaxWarnings = @($helperSyntaxResults | Where-Object { -not $_.Ok })
        if ($helperSyntaxWarnings.Count -gt 0) {
            Write-DtmStatusLine -Tag 'WARN' -Message ("Installed helper scripts have optional syntax warnings: {0}" -f $helperSyntaxWarnings.Count) -Color Yellow
        }
        else {
            Write-DtmStatusLine -Tag 'OK' -Message 'Installed helper scripts were checked as optional support files.' -Color Green
        }
    }
}
else {
    Write-DtmStatusLine -Tag 'INFO' -Message 'Installed helper scripts: skipped because no DTMAPI install footprint was found.' -Color Cyan
}

$installState = $null
$releaseManifest = $null
$installedAt = $null
if (Test-Path -LiteralPath $installStatePath -PathType Leaf) {
    $installState = Read-DtmStatusJson -Path $installStatePath -Required
    if ($installState) {
        $recordedInstallVersion = [string](Get-DtmApiMapValue -Map $installState -Key 'DTMAPIVersion' -Default '')
        $recordedInstalledAt = [string](Get-DtmApiMapValue -Map $installState -Key 'InstalledAt' -Default '(not recorded by this legacy/partial state)')
        Write-DtmStatusLine -Tag 'INFO' -Message ("Installed DTMAPI version: {0}" -f $recordedInstallVersion) -Color Cyan
        Write-DtmStatusLine -Tag 'INFO' -Message ("Installed at: {0}" -f $recordedInstalledAt) -Color Cyan
        Compare-DtmInstalledVersion -Label 'Install-state DTMAPI version' -Actual ([string](Get-DtmApiMapValue -Map $installState -Key 'DTMAPIVersion' -Default '')) -Expected $script:DtmApiReleaseVersion
        Compare-DtmInstalledVersion -Label 'Install-state binary version' -Actual ([string](Get-DtmApiMapValue -Map $installState -Key 'BinaryVersion' -Default '')) -Expected $script:DtmApiBinaryVersion
        $installedAt = Convert-DtmStatusTimestamp -Value ([string](Get-DtmApiMapValue -Map $installState -Key 'InstalledAt' -Default ''))
    }
}
if (Test-Path -LiteralPath $releaseManifestPath -PathType Leaf) {
    $releaseManifest = Read-DtmStatusJson -Path $releaseManifestPath -Required
    if ($releaseManifest) {
        Write-DtmStatusLine -Tag 'INFO' -Message ("Package kind: {0}" -f [string](Get-DtmApiMapValue -Map $releaseManifest -Key 'PackageKind' -Default '(not recorded by this legacy/partial manifest)')) -Color Cyan
        Write-DtmStatusLine -Tag 'INFO' -Message ("Package version: {0}" -f [string](Get-DtmApiMapValue -Map $releaseManifest -Key 'DTMAPIVersion' -Default '')) -Color Cyan
        Compare-DtmInstalledVersion -Label 'Installed release-manifest DTMAPI version' -Actual ([string](Get-DtmApiMapValue -Map $releaseManifest -Key 'DTMAPIVersion' -Default '')) -Expected $script:DtmApiReleaseVersion
        Compare-DtmInstalledVersion -Label 'Installed release-manifest binary version' -Actual ([string](Get-DtmApiMapValue -Map $releaseManifest -Key 'BinaryVersion' -Default '')) -Expected $script:DtmApiBinaryVersion
    }
}

if ($installState -and $releaseManifest) {
    Test-DtmInstalledOptionalComponents -InstallState $installState -ReleaseManifest $releaseManifest
    $releaseBuildCommit = [string](Get-DtmApiMapValue -Map $releaseManifest -Key 'BuildCommit' -Default '')
    $stateSourceCommit = [string](Get-DtmApiMapValue -Map $installState -Key 'SourceRepoCommit' -Default '')
    $provenanceClaimsCurrent = [string]::Equals(
            [string](Get-DtmApiMapValue -Map $installState -Key 'DTMAPIVersion' -Default ''),
            $script:DtmApiReleaseVersion,
            [System.StringComparison]::Ordinal) -or
        [string]::Equals(
            [string](Get-DtmApiMapValue -Map $releaseManifest -Key 'DTMAPIVersion' -Default ''),
            $script:DtmApiReleaseVersion,
            [System.StringComparison]::Ordinal)

    if ([string]::IsNullOrWhiteSpace($releaseBuildCommit)) {
        if ($provenanceClaimsCurrent) {
            Write-DtmStatusLine -Tag 'INVALID' -Message 'Installed release-manifest BuildCommit is missing.' -Color Red
            $script:DtmRequiredInvalid++
        }
        else {
            Write-DtmStatusLine -Tag 'UPDATE' -Message 'Legacy release-manifest has no BuildCommit provenance. Run 1_install_dtmapi.bat.' -Color Yellow
            $script:DtmUpdateRequired++
        }
    }
    if ([string]::IsNullOrWhiteSpace($stateSourceCommit)) {
        if ($provenanceClaimsCurrent) {
            Write-DtmStatusLine -Tag 'INVALID' -Message 'Installed install-state SourceRepoCommit is missing.' -Color Red
            $script:DtmRequiredInvalid++
        }
        else {
            Write-DtmStatusLine -Tag 'UPDATE' -Message 'Legacy install-state has no SourceRepoCommit provenance. Run 1_install_dtmapi.bat.' -Color Yellow
            $script:DtmUpdateRequired++
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($releaseBuildCommit) -and
        -not [string]::IsNullOrWhiteSpace($stateSourceCommit)) {
        if ([string]::Equals($releaseBuildCommit.Trim(), $stateSourceCommit.Trim(), [System.StringComparison]::Ordinal)) {
            Write-DtmStatusLine -Tag 'OK' -Message ("Installed provenance commit receipts match: {0}" -f $releaseBuildCommit.Trim()) -Color Green
        }
        else {
            Write-DtmStatusLine -Tag 'INVALID' -Message ("Installed provenance commit mismatch. Release={0} State={1}" -f $releaseBuildCommit.Trim(), $stateSourceCommit.Trim()) -Color Red
            $script:DtmRequiredInvalid++
        }
    }
}

if ($hasInstallFootprint -and -not (Test-Path -LiteralPath $installedVersionAuthority -PathType Leaf)) {
    $recordedInstallVersion = if ($installState) { [string](Get-DtmApiMapValue -Map $installState -Key 'DTMAPIVersion' -Default '') } else { '' }
    $recordedPackageVersion = if ($releaseManifest) { [string](Get-DtmApiMapValue -Map $releaseManifest -Key 'DTMAPIVersion' -Default '') } else { '' }
    $claimsCurrentSchema = [string]::Equals($recordedInstallVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -or
        [string]::Equals($recordedPackageVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal)
    if ($claimsCurrentSchema) {
        Write-DtmStatusLine -Tag 'MISSING' -Message ("The installed {0} state is missing its Runtime version authority." -f $script:DtmApiReleaseVersion) -Color Red
        Write-DtmStatusDetail -Text $installedVersionAuthority
        $script:DtmRequiredMissing++
    }
    elseif (-not [string]::IsNullOrWhiteSpace($recordedInstallVersion) -or -not [string]::IsNullOrWhiteSpace($recordedPackageVersion)) {
        Write-DtmStatusLine -Tag 'UPDATE' -Message 'This legacy Runtime predates the installed version-authority file. Run 1_install_dtmapi.bat.' -Color Yellow
        $script:DtmUpdateRequired++
    }
}

$installedRuntimeVersionRows = New-Object 'System.Collections.Generic.List[object]'
foreach ($runtimeFileName in @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Abstractions.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')) {
    $runtimeFilePath = Join-Path $pluginDir $runtimeFileName
    if (-not (Test-Path -LiteralPath $runtimeFilePath -PathType Leaf)) {
        continue
    }
    try {
        $actualFileVersion = [string]([System.Diagnostics.FileVersionInfo]::GetVersionInfo($runtimeFilePath).FileVersion)
        $installedRuntimeVersionRows.Add([pscustomobject]@{ Name = $runtimeFileName; Version = $actualFileVersion }) | Out-Null
        if (-not [string]::Equals($actualFileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
            $script:DtmUpdateRequired++
        }
    }
    catch {
        Write-DtmStatusLine -Tag 'WARN' -Message ("Could not read Runtime DLL FileVersion: {0}" -f $runtimeFileName) -Color Yellow
        Write-DtmStatusDetail -Text $_.Exception.Message
        $script:DtmRequiredInvalid++
    }
}
if ($installedRuntimeVersionRows.Count -gt 0) {
    $outdatedRuntimeVersionRows = @($installedRuntimeVersionRows.ToArray() | Where-Object { -not [string]::Equals([string]$_.Version, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) })
    if ($outdatedRuntimeVersionRows.Count -eq 0) {
        Write-DtmStatusLine -Tag 'OK' -Message ("All installed Runtime DLL FileVersions match {0}." -f $script:DtmApiBinaryVersion) -Color Green
    }
    else {
        Write-DtmStatusLine -Tag 'UPDATE' -Message ("Installed Runtime DLL FileVersion drift: {0}/{1} files do not match {2}. Run 1_install_dtmapi.bat." -f $outdatedRuntimeVersionRows.Count, $installedRuntimeVersionRows.Count, $script:DtmApiBinaryVersion) -Color Yellow
        foreach ($row in $outdatedRuntimeVersionRows) {
            Write-DtmStatusDetail -Text ("{0}: {1}" -f $row.Name, $row.Version)
        }
    }
}

if (Test-Path -LiteralPath $installedPlayerDoctorRoot -PathType Container) {
    Write-Host ""
    Write-DtmStatusLine -Tag 'INFO' -Message 'A legacy or separately installed Player Doctor folder is present; it is not part of Runtime integrity.' -Color Cyan
    Write-DtmStatusDetail -Text $installedPlayerDoctorRoot
}

$latestInstallFailure = Get-DtmLatestStateFile -Directory $stateDir -Filter 'install-state.failed-*.json'
if ($latestInstallFailure) {
    $failureState = Read-DtmStatusJson -Path $latestInstallFailure.FullName
    $failureAtText = if ($failureState) { [string](Get-DtmApiMapValue -Map $failureState -Key 'FailedAt' -Default '') } else { '' }
    $failureAt = if ($failureState) { Convert-DtmStatusTimestamp -Value $failureAtText } else { $null }
    $failureIsOlderThanCurrentInstall = $false
    if ($installedAt -and $failureAt -and $failureAt.UtcDateTime -le $installedAt.UtcDateTime) {
        $failureIsOlderThanCurrentInstall = $true
    }

    if ($failureIsOlderThanCurrentInstall) {
        Write-DtmStatusLine -Tag 'INFO' -Message 'Previous failed install state found before the latest successful install.' -Color Cyan
    }
    else {
        Write-DtmStatusLine -Tag 'WARN' -Message 'Latest failed install state found.' -Color Yellow
    }

    Write-DtmStatusDetail -Text $latestInstallFailure.FullName
    if ($failureState) {
        Write-DtmStatusDetail -Text ("FailedAt: {0}" -f $failureAtText)
        if ($failureIsOlderThanCurrentInstall) {
            Write-DtmStatusDetail -Text ("Latest successful install: {0}" -f [string](Get-DtmApiMapValue -Map $installState -Key 'InstalledAt' -Default ''))
        }
        Write-DtmStatusDetail -Text ("Error: {0}" -f [string](Get-DtmApiMapValue -Map $failureState -Key 'Error' -Default '(not recorded)'))
    }
}
$latestUninstallState = Get-DtmLatestStateFile -Directory $stateDir -Filter 'uninstall-state-*.json'
if ($latestUninstallState) {
    Write-DtmStatusLine -Tag 'INFO' -Message 'Latest uninstall state found.' -Color Cyan
    Write-DtmStatusDetail -Text $latestUninstallState.FullName
}

Write-Host ""
Write-DtmStatusLine -Tag 'INFO' -Message 'Optional read-only Player Doctor' -Color Cyan
if (-not [string]::IsNullOrWhiteSpace($playerDoctorExe) -and (Test-Path -LiteralPath $playerDoctorExe -PathType Leaf)) {
    Write-DtmStatusLine -Tag 'OK' -Message ("Player Doctor helper is present (source: {0})." -f $playerDoctorSource) -Color Green
    Write-DtmStatusDetail -Text $playerDoctorExe
    & $playerDoctorExe inspect --game-root $GameDir --scan-context installed-game
    $playerDoctorExit = $LASTEXITCODE
    if ($playerDoctorExit -eq 0) {
        Write-DtmStatusLine -Tag 'OK' -Message 'Player Doctor found no installation or minimum-version errors.' -Color Green
    }
    elseif ($playerDoctorExit -eq 2) {
        Write-DtmStatusLine -Tag 'WARN' -Message 'Player Doctor found installation, placement, or minimum-version errors.' -Color Yellow
        $script:DtmRequiredInvalid++
    }
    else {
        Write-DtmStatusLine -Tag 'WARN' -Message ("Player Doctor could not complete. Exit code: {0}" -f $playerDoctorExit) -Color Yellow
    }
}
else {
    Write-DtmStatusLine -Tag 'INFO' -Message 'Player Doctor is not installed; the normal Runtime package intentionally contains no EXE.' -Color Cyan
    Write-DtmStatusDetail -Text 'Core Runtime, BepInEx, receipt and version checks above remain authoritative for this status result.'
}

Write-Host ""
Write-DtmStatusLine -Tag 'INFO' -Message 'Recent diagnostics' -Color Cyan
Write-DtmPathStatus -Label 'Latest log' -Path $latestLog -PathType Leaf | Out-Null
if ([string]::IsNullOrWhiteSpace($latestReport)) {
    Write-DtmStatusLine -Tag 'INFO' -Message 'Latest report: not exported yet' -Color Cyan
}
else {
    Write-DtmPathStatus -Label 'Latest report' -Path $latestReport -PathType Leaf | Out-Null
}

Write-Host ""
if ($script:DtmRequiredMissing -eq 0) {
    Write-DtmStatusLine -Tag 'OK' -Message 'Required DTMAPI install files are present.' -Color Green
}
else {
    Write-DtmStatusLine -Tag 'MISSING' -Message ("Required DTMAPI install files missing: {0}" -f $script:DtmRequiredMissing) -Color Red
}
if ($script:DtmRequiredInvalid -gt 0) {
    Write-DtmStatusLine -Tag 'WARN' -Message ("Required DTMAPI install files invalid: {0}" -f $script:DtmRequiredInvalid) -Color Yellow
}
if ($script:DtmOptionalMissing -gt 0) {
    Write-DtmStatusLine -Tag 'INFO' -Message ("Optional diagnostics missing: {0}" -f $script:DtmOptionalMissing) -Color Cyan
}
if ($script:DtmUpdateRequired -gt 0) {
    Write-DtmStatusLine -Tag 'UPDATE' -Message ("Installed DTMAPI is older or inconsistent with this Workshop package ({0} version checks)." -f $script:DtmUpdateRequired) -Color Yellow
}

Write-Host ""
Write-DtmStatusLine -Tag 'INFO' -Message "Legacy/non-destructive DTMAPI package metadata markers: $($legacyOfficialLocalMetadata.Count)" -Color Cyan
Write-DtmStatusDetail -Text 'These markers are not installer receipts and do not authorize overwrite, removal, adoption, or enablement changes.'
foreach ($package in $legacyOfficialLocalMetadata) {
    Write-DtmStatusLine -Tag 'INFO' -Message ("{0}: {1} {2}" -f $package.OfficialFolder, $package.UniqueID, $package.Version) -Color Cyan
}
Write-Host ""
if ($legacyDetections.Count -eq 0) {
    Write-DtmStatusLine -Tag 'OK' -Message 'No legacy DLK/SMAPI items detected.' -Color Green
}
else {
    Write-DtmStatusLine -Tag 'WARN' -Message "Legacy detections: $($legacyDetections.Count)" -Color Yellow
}
foreach ($item in $legacyDetections) {
    Write-DtmStatusLine -Tag 'WARN' -Message ("{0}: {1} [{2}]" -f $item.Kind, $item.Path, $item.Action) -Color Yellow
}
Write-Host ""
Write-DtmStatusLine -Tag 'INFO' -Message 'Next steps' -Color Cyan
Write-DtmStatusDetail -Text 'If any required item is [MISSING], run 1_install_dtmapi.bat again.'
Write-DtmStatusDetail -Text 'If old DLK/SMAPI items are listed, unsubscribe/disable them in Steam or the in-game mod list, then restart Steam and Doloc Town.'
Write-DtmStatusDetail -Text 'Use DTMAPI Settings > Logs > Export Report when asking for help.'
Write-DtmStatusDetail -Text 'If the game crashes before you can export a report, run 4_collect_dtmapi_logs.bat and send the Desktop\DTMAPI-logs folder.'
Write-DtmStatusDetail -Text 'The player uninstaller removes DTMAPI Runtime only and always preserves official-local/content packages and their enablement state.'

Write-Host ""
switch ([string]$script:DtmTransactionStatus) {
    'Blocked' {
        Write-DtmApiInstallerMessage -Code 'DTM-E1303' -Chinese '最终状态：BLOCKED（事务残留不安全或无法验证）。' -English 'Final state: BLOCKED (transaction residue is unsafe or unverifiable).' -Level Error
        exit 1
    }
    'RecoveryPending' {
        Write-DtmApiInstallerMessage -Code 'DTM-E1302' -Chinese '最终状态：RECOVERY_PENDING（请先运行安装脚本恢复）。' -English 'Final state: RECOVERY_PENDING (run the installer to recover first).' -Level Error
        exit 1
    }
    'RepairableStale' {
        Write-DtmApiInstallerMessage -Code 'DTM-W1301' -Chinese '最终状态：REPAIRABLE_STALE（检查器未修改文件）。' -English 'Final state: REPAIRABLE_STALE (the checker did not modify files).' -Level Warning
        exit 1
    }
}
if ($script:DtmRequiredMissing -gt 0 -or $script:DtmRequiredInvalid -gt 0) {
    if (-not $hasInstallFootprint) {
        Write-DtmApiInstallerMessage -Code 'DTM-I3002' -Chinese '最终状态：NOT_INSTALLED（未检测到 DTMAPI 安装痕迹）。' -English 'Final state: NOT_INSTALLED (no DTMAPI install footprint was detected).' -Level Info
    }
    else {
        Write-DtmApiInstallerMessage -Code 'DTM-E3002' -Chinese '最终状态：PARTIAL（必需文件缺失或无效）。' -English 'Final state: PARTIAL (required files are missing or invalid).' -Level Error
    }
    exit 1
}
if ($script:DtmUpdateRequired -gt 0) {
    Write-DtmApiInstallerMessage -Code 'DTM-W3003' -Chinese '最终状态：UPDATE_REQUIRED（已安装版本或字节与当前 0.6.1 包不一致）。' -English 'Final state: UPDATE_REQUIRED (the installed version or bytes differ from the current 0.6.1 package).' -Level Warning
    exit 2
}

Write-DtmApiInstallerMessage `
    -Code 'DTM-S3001' `
    -Chinese '最终状态：INSTALLED_HEALTHY。DTMAPI 0.6.1、BepInEx/Doorstop、五个 Runtime DLL、兼容组件和安装凭据均通过检查。' `
    -English 'Final state: INSTALLED_HEALTHY. DTMAPI 0.6.1, BepInEx/Doorstop, all five Runtime DLLs, the compatibility component, and install receipts passed validation.' `
    -Detail "GameDir=$GameDir" `
    -Level Success
exit 0
