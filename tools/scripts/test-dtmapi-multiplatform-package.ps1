[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PackageRoot,
    [string] $AcceptedPackageRoot = '',
    [ValidateSet('ObservedPublished','Candidate')] [string] $SourceKind = 'ObservedPublished',
    [string] $MetadataRoot = '',
    [string] $IconPath = '',
    [string] $PreviewPath = '',
    [string] $EvidencePath = '',
    [switch] $AllowWorkshopControlFile,
    [switch] $AllowDeliveredWorkshopControlFile,
    [switch] $SkipPowerShell51Syntax,
    [switch] $SkipBashSyntax,
    [switch] $Quiet
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\multiplatform-package-common.ps1"

function Assert-DtmApiMultiPlatformAudit {
    param(
        [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw "DTMAPI multi-platform package audit failed: $Message"
    }
}

function Assert-DtmApiMultiPlatformFileBytesEqual {
    param(
        [Parameter(Mandatory = $true)] [string] $ExpectedPath,
        [Parameter(Mandatory = $true)] [string] $ActualPath,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    Assert-DtmApiMultiPlatformAudit -Condition (Test-Path -LiteralPath $ExpectedPath -PathType Leaf) -Message "$Label expected file is missing: $ExpectedPath"
    Assert-DtmApiMultiPlatformAudit -Condition (Test-Path -LiteralPath $ActualPath -PathType Leaf) -Message "$Label actual file is missing: $ActualPath"
    $expectedItem = Get-Item -LiteralPath $ExpectedPath -Force -ErrorAction Stop
    $actualItem = Get-Item -LiteralPath $ActualPath -Force -ErrorAction Stop
    Assert-DtmApiMultiPlatformAudit -Condition ($expectedItem.Length -eq $actualItem.Length) -Message "$Label length differs. Expected=$($expectedItem.Length) Actual=$($actualItem.Length)"
    $expectedHash = Get-DtmApiMultiPlatformFileSha256 -Path $ExpectedPath
    $actualHash = Get-DtmApiMultiPlatformFileSha256 -Path $ActualPath
    Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals($expectedHash, $actualHash, [System.StringComparison]::OrdinalIgnoreCase)) -Message "$Label SHA-256 differs. Expected=$expectedHash Actual=$actualHash"
}

function Test-DtmApiMultiPlatformPowerShell51Syntax {
    param([Parameter(Mandatory = $true)] [string[]] $Paths)

    $hostPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    Assert-DtmApiMultiPlatformAudit -Condition (Test-Path -LiteralPath $hostPath -PathType Leaf) -Message 'Windows PowerShell 5.1 host is unavailable.'
    $validatorPath = Join-Path ([System.IO.Path]::GetTempPath()) ('dtmapi-multiplatform-parser-' + [Guid]::NewGuid().ToString('N') + '.ps1')
    $validatorText = @'
param([Parameter(Mandatory = $true)] [string] $Path)
$ErrorActionPreference = 'Stop'
$tokens = $null
$errors = $null
[System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$errors) | Out-Null
if ($errors -and $errors.Count -gt 0) {
    foreach ($errorItem in $errors) {
        '{0}:{1} {2}' -f $errorItem.Extent.StartLineNumber, $errorItem.Extent.StartColumnNumber, $errorItem.Message
    }
    exit 1
}
exit 0
'@
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($validatorPath, $validatorText, $encoding)
    try {
        foreach ($path in $Paths) {
            $output = @(& $hostPath -NoProfile -ExecutionPolicy Bypass -File $validatorPath -Path $path 2>&1 | ForEach-Object { [string]$_ })
            $exitCode = $LASTEXITCODE
            Assert-DtmApiMultiPlatformAudit -Condition ($exitCode -eq 0) -Message ("Windows PowerShell 5.1 parser rejected {0}. Output={1}" -f $path, ([string]::Join(' | ', $output)))
        }
    }
    finally {
        if (Test-Path -LiteralPath $validatorPath -PathType Leaf) {
            Remove-Item -LiteralPath $validatorPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Test-DtmApiMultiPlatformBashSyntax {
    param([Parameter(Mandatory = $true)] [string[]] $Paths)

    $wsl = Get-Command wsl.exe -ErrorAction SilentlyContinue
    if ($wsl) {
        $oldPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            foreach ($path in $Paths) {
                $wslPathInput = ([System.IO.Path]::GetFullPath($path)).Replace('\', '/')
                $linuxPathOutput = @(& $wsl.Source --exec wslpath -a -u $wslPathInput 2>$null | ForEach-Object { [string]$_ })
                $pathExit = $LASTEXITCODE
                $linuxPathCandidates = @($linuxPathOutput | Where-Object { ([string]$_).Trim().StartsWith('/', [System.StringComparison]::Ordinal) })
                Assert-DtmApiMultiPlatformAudit -Condition ($pathExit -eq 0 -and $linuxPathCandidates.Count -gt 0) -Message ("WSL could not translate shell path {0}. Output={1}" -f $path, ([string]::Join(' | ', $linuxPathOutput)))
                $linuxPath = ([string]$linuxPathCandidates[$linuxPathCandidates.Count - 1]).Trim()
                $output = @(& $wsl.Source --exec bash -n -- $linuxPath 2>&1 | ForEach-Object { [string]$_ })
                $exitCode = $LASTEXITCODE
                Assert-DtmApiMultiPlatformAudit -Condition ($exitCode -eq 0) -Message ("WSL bash -n rejected {0}. Output={1}" -f $path, ([string]::Join(' | ', $output)))
            }
        }
        finally {
            $ErrorActionPreference = $oldPreference
        }
        return [string]$wsl.Source
    }

    $bash = Get-Command bash.exe -ErrorAction SilentlyContinue
    Assert-DtmApiMultiPlatformAudit -Condition ($null -ne $bash) -Message 'Neither wsl.exe nor bash.exe is available for shell syntax validation.'
    foreach ($path in $Paths) {
        $output = @(& $bash.Source -n -- $path 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
        Assert-DtmApiMultiPlatformAudit -Condition ($exitCode -eq 0) -Message ("bash -n rejected {0}. Output={1}" -f $path, ([string]::Join(' | ', $output)))
    }
    return [string]$bash.Source
}

$repo = Get-DtmApiMultiPlatformRepoRoot
$package = [System.IO.Path]::GetFullPath($PackageRoot)
if ($AllowWorkshopControlFile -and $AllowDeliveredWorkshopControlFile) {
    throw 'Select either the local upload control-file allowance or the observed subscription allowance.'
}
if ([string]::IsNullOrWhiteSpace($AcceptedPackageRoot)) {
    $AcceptedPackageRoot = Get-DtmApiMultiPlatformDefaultAcceptedPackageRoot
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
$metadata = [System.IO.Path]::GetFullPath($MetadataRoot)
$accepted = Assert-DtmApiMultiPlatformAcceptedRuntimePackage -PackageRoot ([System.IO.Path]::GetFullPath($AcceptedPackageRoot)) -SourceKind $SourceKind
$expectedSchema = if ($SourceKind -eq 'Candidate') { 2 } else { 1 }
$null = Assert-DtmApiMultiPlatformOrdinaryTree -Path $package -Context 'Multi-platform package candidate'
$null = Assert-DtmApiMultiPlatformOrdinaryTree -Path $metadata -Context 'Multi-platform metadata source'
$acceptedWindowsExeFiles = @(Get-ChildItem -LiteralPath $accepted.Root -Filter '*.exe' -File -Force -Recurse)
Assert-DtmApiMultiPlatformAudit -Condition ($acceptedWindowsExeFiles.Count -eq 0) -Message 'the accepted Windows Runtime package must remain the unchanged zero-EXE distribution.'

try {
    $packageMetadata = [System.IO.File]::ReadAllText((Join-Path $metadata 'package-metadata.json'), [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $info = [System.IO.File]::ReadAllText((Join-Path $package 'info.json'), [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $hostManifestPath = Join-Path $package (([string]$packageMetadata.hostManifestPackagePath).Replace('/', '\'))
    $hostManifest = [System.IO.File]::ReadAllText($hostManifestPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $runtimePackageManifestPath = Join-Path $package (([string]$packageMetadata.runtimePackageManifestPath).Replace('/', '\'))
    $runtimePackageManifest = [System.IO.File]::ReadAllText($runtimePackageManifestPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
}
catch {
    throw "DTMAPI multi-platform package audit failed: package JSON is invalid. $($_.Exception.Message)"
}
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$packageMetadata.windowsHostPackagePath, 'DTMAPI-MultiPlatform-Installer.exe', [System.StringComparison]::Ordinal)) -Message 'Windows host must remain the single root DTMAPI-MultiPlatform-Installer.exe targeted by every BAT shim.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$packageMetadata.workshopId, $script:DtmApiMultiPlatformWorkshopId, [System.StringComparison]::Ordinal)) -Message 'Package metadata Workshop ID is invalid.'

$expectedPaths = New-Object 'System.Collections.Generic.List[string]'
foreach ($relative in $accepted.SharedPaths) { $expectedPaths.Add($relative) | Out-Null }
foreach ($relative in @(
    '1_install_dtmapi.bat',
    '2_uninstall_dtmapi.bat',
    '3_check_dtmapi_status.bat',
    '4_collect_dtmapi_logs.bat',
    '1_install_dtmapi.sh',
    '2_uninstall_dtmapi.sh',
    '3_check_dtmapi_status.sh',
    '4_collect_dtmapi_logs.sh',
    'README_FIRST.txt',
    'info.json',
    'icon.png',
    'preview.png',
    ([string]$packageMetadata.windowsHostPackagePath).Replace('\', '/'),
    ([string]$packageMetadata.linuxHostPackagePath).Replace('\', '/'),
    ([string]$packageMetadata.hostManifestPackagePath).Replace('\', '/'),
    ([string]$packageMetadata.runtimePackageManifestPath).Replace('\', '/')
)) { $expectedPaths.Add($relative) | Out-Null }
if ($AllowWorkshopControlFile -or $AllowDeliveredWorkshopControlFile) {
    $expectedPaths.Add('workshop.json') | Out-Null
}
$expected = [string[]]$expectedPaths.ToArray()
[Array]::Sort($expected, [System.StringComparer]::Ordinal)
$actual = [string[]]@(Get-ChildItem -LiteralPath $package -File -Force -Recurse -ErrorAction Stop | ForEach-Object {
    Get-DtmApiMultiPlatformRelativePath -Root $package -Path $_.FullName
})
[Array]::Sort($actual, [System.StringComparer]::Ordinal)
$difference = @(Compare-Object -ReferenceObject $expected -DifferenceObject $actual -CaseSensitive)
Assert-DtmApiMultiPlatformAudit -Condition ($difference.Count -eq 0 -and $actual.Count -eq $expected.Count) -Message ("candidate file set is not exact. Expected={0} Actual={1} Difference={2}" -f $expected.Count, $actual.Count, ([string]::Join(', ', @($difference | ForEach-Object { $_.SideIndicator + $_.InputObject }))))

$workshopControlFiles = @(Get-ChildItem -LiteralPath $package -Filter 'workshop.json' -File -Force -Recurse)
$workshopControlReceipt = $null
if ($AllowWorkshopControlFile -or $AllowDeliveredWorkshopControlFile) {
    Assert-DtmApiMultiPlatformAudit -Condition ($workshopControlFiles.Count -eq 1 -and [string]::Equals($workshopControlFiles[0].FullName, (Join-Path $package 'workshop.json'), [System.StringComparison]::OrdinalIgnoreCase)) -Message 'control-file allowance requires exactly one root workshop.json.'
    $workshopControlReceipt = Assert-DtmApiMultiPlatformWorkshopControlFile `
        -Path $workshopControlFiles[0].FullName `
        -ExpectedWorkshopId ([string]$packageMetadata.workshopId)
}
else {
    Assert-DtmApiMultiPlatformAudit -Condition ($workshopControlFiles.Count -eq 0) -Message 'workshop.json requires an explicit local upload or exact observed subscription allowance.'
}
$zipPaths = @($actual | Where-Object { $_.EndsWith('.zip', [System.StringComparison]::OrdinalIgnoreCase) })
Assert-DtmApiMultiPlatformAudit -Condition ($zipPaths.Count -eq 1 -and [string]::Equals($zipPaths[0], 'Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip', [System.StringComparison]::Ordinal)) -Message 'candidate must contain only the accepted internal BepInEx ZIP and no outer/manual-extraction archive.'

foreach ($relative in $accepted.SharedPaths) {
    Assert-DtmApiMultiPlatformFileBytesEqual `
        -ExpectedPath (Join-Path $accepted.Root $relative.Replace('/', '\')) `
        -ActualPath (Join-Path $package $relative.Replace('/', '\')) `
        -Label ("shared published path $relative")
}
$sharedReceipt = Get-DtmApiMultiPlatformTreeReceipt -Root $package -RelativePaths $accepted.SharedPaths
Assert-DtmApiMultiPlatformAudit -Condition ($sharedReceipt.FileCount -eq $accepted.SharedPayload.FileCount -and $sharedReceipt.Bytes -eq $accepted.SharedPayload.Bytes -and [string]::Equals($sharedReceipt.TreeSha256, $accepted.SharedPayload.TreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'shared Runtime projection differs from the selected exact source.'

if ($SourceKind -eq 'Candidate') {
    $packageMetadata.installerVersion = '0.2.0-experimental'
    Assert-DtmApiMultiPlatformAudit -Condition ([IO.File]::ReadAllText((Join-Path $package 'info.json')) -ceq (Get-DtmApiMultiPlatformCandidateInfoText -MetadataRoot $metadata -RuntimeVersion $accepted.RuntimeVersion)) -Message 'Candidate info projection differs.'
} else {
Assert-DtmApiMultiPlatformFileBytesEqual -ExpectedPath (Join-Path $metadata 'info.json') -ActualPath (Join-Path $package 'info.json') -Label 'info.json metadata projection'
}
Assert-DtmApiMultiPlatformFileBytesEqual -ExpectedPath (Join-Path $metadata 'README.txt') -ActualPath (Join-Path $package 'README_FIRST.txt') -Label 'README metadata projection'
Assert-DtmApiMultiPlatformFileBytesEqual -ExpectedPath ([System.IO.Path]::GetFullPath($IconPath)) -ActualPath (Join-Path $package 'icon.png') -Label 'icon branding projection'
Assert-DtmApiMultiPlatformFileBytesEqual -ExpectedPath ([System.IO.Path]::GetFullPath($PreviewPath)) -ActualPath (Join-Path $package 'preview.png') -Label 'preview branding projection'

$expectedPackageName = 'DTMAPI-' + [char]0x591a + [char]0x5e73 + [char]0x53f0
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$info.name, $expectedPackageName, [System.StringComparison]::Ordinal)) -Message 'info.json package name is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$info.version, $accepted.RuntimeVersion, [System.StringComparison]::Ordinal)) -Message 'info.json version differs from the selected Runtime.'
Assert-DtmApiMultiPlatformAudit -Condition (-not [string]::IsNullOrWhiteSpace([string]$packageMetadata.requiredSteamDescriptionLead) -and ([string]$info.steamDescription).StartsWith([string]$packageMetadata.requiredSteamDescriptionLead, [System.StringComparison]::Ordinal) -and ([string]$info.localized_description.schinese).StartsWith([string]$packageMetadata.requiredSteamDescriptionLead, [System.StringComparison]::Ordinal)) -Message 'The required product overview is not the first Steam description text.'
$playerText = [string]$info.description + "`n" + [string]$info.steamDescription + "`n" + [System.IO.File]::ReadAllText((Join-Path $package 'README_FIRST.txt'), [System.Text.Encoding]::UTF8)
$requiredWorkshopContentPath = 'workshop/content/' + [string]$packageMetadata.steamAppId + '/' + [string]$packageMetadata.workshopId
Assert-DtmApiMultiPlatformAudit -Condition ($playerText.IndexOf($script:DtmApiMultiPlatformRequiredLaunchOption, [System.StringComparison]::Ordinal) -ge 0) -Message 'player text omits the exact required Steam Deck/Linux launch option.'
Assert-DtmApiMultiPlatformAudit -Condition ($playerText.IndexOf($requiredWorkshopContentPath, [System.StringComparison]::Ordinal) -ge 0 -and $playerText.IndexOf('<本项目 Workshop ID>', [System.StringComparison]::Ordinal) -lt 0 -and $playerText.IndexOf('<本項目 Workshop ID>', [System.StringComparison]::Ordinal) -lt 0 -and $playerText.IndexOf("<this item's Workshop ID>", [System.StringComparison]::Ordinal) -lt 0) -Message 'player text omits the real multi-platform Workshop path or retains an ID placeholder.'
Assert-DtmApiMultiPlatformAudit -Condition ($playerText.IndexOf('Native, then Builtin', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -Message 'player text omits the CrossOver winhttp Native, then Builtin route.'
Assert-DtmApiMultiPlatformAudit -Condition ($playerText.IndexOf('not code-signed', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $playerText.IndexOf('Doloc Town Steam Workshop', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -Message 'player text does not disclose the unsigned hosts and official Workshop source.'
foreach ($locale in @('schinese', 'tchinese', 'english')) {
    $localized = [string]$info.localized_description.$locale
    foreach ($requiredText in @($script:DtmApiMultiPlatformRequiredLaunchOption, $requiredWorkshopContentPath, 'Shift+F4', 'bash ./1_install_dtmapi.sh', 'bash ./2_uninstall_dtmapi.sh', 'bash ./3_check_dtmapi_status.sh', 'bash ./4_collect_dtmapi_logs.sh', 'DTMAPI-MultiPlatform-Installer.exe', 'Native, then Builtin', 'CrossOver')) {
        Assert-DtmApiMultiPlatformAudit -Condition ($localized.IndexOf($requiredText, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -Message "localized_description.$locale is incomplete; missing: $requiredText"
    }
    $localizedDisclosureMarkers = @($packageMetadata.localizedDisclosureMarkers.$locale)
    Assert-DtmApiMultiPlatformAudit -Condition ($localizedDisclosureMarkers.Count -eq 2) -Message "localized disclosure contract is invalid for: $locale"
    foreach ($requiredText in $localizedDisclosureMarkers) {
        Assert-DtmApiMultiPlatformAudit -Condition ($localized.IndexOf($requiredText, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -Message "localized_description.$locale disclosure is incomplete; missing: $requiredText"
    }
}

$windowsBatActionMap = [ordered]@{
    '1_install_dtmapi.bat' = 'install'
    '2_uninstall_dtmapi.bat' = 'uninstall'
    '3_check_dtmapi_status.bat' = 'status'
    '4_collect_dtmapi_logs.bat' = 'collect-logs'
}
$rootBatFiles = @(Get-ChildItem -LiteralPath $package -Filter '*.bat' -File -Force)
Assert-DtmApiMultiPlatformAudit -Condition ($rootBatFiles.Count -eq $windowsBatActionMap.Count) -Message 'candidate root must contain exactly the four multi-platform BAT shims.'
foreach ($entry in $windowsBatActionMap.GetEnumerator()) {
    $templatePath = Join-Path $metadata $entry.Key
    $batPath = Join-Path $package $entry.Key
    Assert-DtmApiMultiPlatformFileBytesEqual -ExpectedPath $templatePath -ActualPath $batPath -Label "$($entry.Key) Windows shim template"
    Assert-DtmApiMultiPlatformWindowsBatShim -Path $batPath -Action ([string]$entry.Value)
    $batText = [System.IO.File]::ReadAllText($batPath, [System.Text.Encoding]::ASCII)
    $exactDispatch = '"%~dp0DTMAPI-MultiPlatform-Installer.exe" ' + [string]$entry.Value + ' --pause %*'
    Assert-DtmApiMultiPlatformAudit -Condition ($batText.IndexOf($exactDispatch, [System.StringComparison]::Ordinal) -ge 0) -Message "$($entry.Key) does not quote the root host path and dispatch exactly '$($entry.Value)'."
    Assert-DtmApiMultiPlatformAudit -Condition ([regex]::Matches($batText, '(?i)DTMAPI-MultiPlatform-Installer\.exe').Count -eq 1) -Message "$($entry.Key) must invoke the root host exactly once."
    Assert-DtmApiMultiPlatformAudit -Condition ($batText.IndexOf('setlocal EnableExtensions DisableDelayedExpansion', [System.StringComparison]::Ordinal) -ge 0 -and $batText.IndexOf(' --pause %*', [System.StringComparison]::Ordinal) -ge 0 -and $batText.IndexOf('set "DTMAPI_EXIT=%ERRORLEVEL%"', [System.StringComparison]::Ordinal) -ge 0 -and $batText.IndexOf('exit /b %DTMAPI_EXIT%', [System.StringComparison]::Ordinal) -ge 0) -Message "$($entry.Key) does not preserve cmd.exe parsing safety, pause for double-click users, and propagate the host exit code."
    Assert-DtmApiMultiPlatformAudit -Condition ($batText.IndexOf('powershell', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -and $batText.IndexOf('invoke-dtmapi-action', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) -Message "$($entry.Key) must not enter the legacy PowerShell/CMD dispatcher route."
    Assert-DtmApiMultiPlatformAudit -Condition (-not [regex]::IsMatch($batText, '(?m)^\s*:[^:]')) -Message "$($entry.Key) must remain a label-free thin shim."
}
$rootCmdFiles = @(Get-ChildItem -LiteralPath $package -Filter '*.cmd' -File -Force)
Assert-DtmApiMultiPlatformAudit -Condition ($rootCmdFiles.Count -eq 0) -Message 'candidate must not expose the legacy root invoke-dtmapi-action.cmd dispatcher.'
$packagedCmdPaths = @($actual | Where-Object { $_.EndsWith('.cmd', [System.StringComparison]::OrdinalIgnoreCase) })
Assert-DtmApiMultiPlatformAudit -Condition ($packagedCmdPaths.Count -eq 1 -and [string]::Equals($packagedCmdPaths[0], 'Content/DTMAPIInstaller/tools/invoke-dtmapi-action.cmd', [System.StringComparison]::Ordinal)) -Message 'only the accepted Content-internal legacy CMD tool may remain for byte parity; no root CMD is allowed.'

$shellActionMap = [ordered]@{
    '1_install_dtmapi.sh' = 'install'
    '2_uninstall_dtmapi.sh' = 'uninstall'
    '3_check_dtmapi_status.sh' = 'status'
    '4_collect_dtmapi_logs.sh' = 'collect-logs'
}
$shellPaths = New-Object 'System.Collections.Generic.List[string]'
foreach ($entry in $shellActionMap.GetEnumerator()) {
    $shellPath = Join-Path $package $entry.Key
    Assert-DtmApiMultiPlatformLfNoBomShellFile -Path $shellPath
    $shellText = [System.IO.File]::ReadAllText($shellPath, [System.Text.Encoding]::UTF8)
    Assert-DtmApiMultiPlatformAudit -Condition ($shellText.IndexOf('Content/DTMAPIInstaller/hosts/linux-x64/dtmapi-installer', [System.StringComparison]::Ordinal) -ge 0) -Message "$($entry.Key) does not target the exact internal Linux host."
    Assert-DtmApiMultiPlatformAudit -Condition ($shellText.IndexOf(('"$host" ' + [string]$entry.Value + ' "$@"'), [System.StringComparison]::Ordinal) -ge 0 -and $shellText.IndexOf('exit_code=$?', [System.StringComparison]::Ordinal) -ge 0 -and $shellText.IndexOf('noexec', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $shellText.IndexOf('exit "$exit_code"', [System.StringComparison]::Ordinal) -ge 0) -Message "$($entry.Key) does not dispatch exactly one '$($entry.Value)' action and preserve a visible noexec failure."
    $shellPaths.Add($shellPath) | Out-Null
}

$windowsHostRelative = ([string]$packageMetadata.windowsHostPackagePath).Replace('/', '\')
$linuxHostRelative = ([string]$packageMetadata.linuxHostPackagePath).Replace('/', '\')
$windowsHost = Join-Path $package $windowsHostRelative
$linuxHost = Join-Path $package $linuxHostRelative
Assert-DtmApiMultiPlatformPeX64 -Path $windowsHost
Assert-DtmApiMultiPlatformElfX64 -Path $linuxHost
$exeFiles = @(Get-ChildItem -LiteralPath $package -Filter '*.exe' -File -Force -Recurse)
Assert-DtmApiMultiPlatformAudit -Condition ($exeFiles.Count -eq 1 -and [string]::Equals($exeFiles[0].FullName, [System.IO.Path]::GetFullPath($windowsHost), [System.StringComparison]::OrdinalIgnoreCase)) -Message 'candidate must contain exactly one allowlisted root win-x64 PE.'
$elfRelativePaths = New-Object 'System.Collections.Generic.List[string]'
foreach ($file in @(Get-ChildItem -LiteralPath $package -File -Force -Recurse)) {
    $stream = [System.IO.File]::Open($file.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        if ($stream.Length -ge 4) {
            $magic = New-Object byte[] 4
            $null = $stream.Read($magic, 0, 4)
            if ($magic[0] -eq 0x7f -and $magic[1] -eq 0x45 -and $magic[2] -eq 0x4c -and $magic[3] -eq 0x46) {
                $elfRelativePaths.Add((Get-DtmApiMultiPlatformRelativePath -Root $package -Path $file.FullName)) | Out-Null
            }
        }
    }
    finally {
        $stream.Dispose()
    }
}
Assert-DtmApiMultiPlatformAudit -Condition ($elfRelativePaths.Count -eq 1 -and [string]::Equals($elfRelativePaths[0], ([string]$packageMetadata.linuxHostPackagePath).Replace('\', '/'), [System.StringComparison]::Ordinal)) -Message 'candidate must contain exactly one allowlisted internal linux-x64 ELF.'
Assert-DtmApiMultiPlatformAudit -Condition (@(Get-ChildItem -LiteralPath $package -File -Force -Recurse | Where-Object { $_.Name -match '(?i)player.?doctor' }).Count -eq 0) -Message 'Player Doctor must not enter the multi-platform package.'

Assert-DtmApiMultiPlatformAudit -Condition ([int]$hostManifest.SchemaVersion -eq $expectedSchema) -Message 'host-artifacts.json schema differs from the selected source branch.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$hostManifest.DistributionId, 'dtmapi-multiplatform', [System.StringComparison]::Ordinal)) -Message 'host-artifacts.json distribution identity is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$hostManifest.DistributionState, $(if ($SourceKind -eq 'Candidate') { 'Candidate' } else { 'published-workshop-item-update-candidate' }), [System.StringComparison]::Ordinal) -and [string]::Equals([string]$hostManifest.WorkshopId, $script:DtmApiMultiPlatformWorkshopId, [System.StringComparison]::Ordinal)) -Message 'host-artifacts.json Workshop publication state is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$hostManifest.RuntimeVersion, $accepted.RuntimeVersion, [System.StringComparison]::Ordinal)) -Message 'host-artifacts.json Runtime version is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$hostManifest.RuntimeBuildCommit, [string]$accepted.BuildCommit, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'host-artifacts.json changed the imported Runtime build identity.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$hostManifest.RequiredLinuxLaunchOption, $script:DtmApiMultiPlatformRequiredLaunchOption, [System.StringComparison]::Ordinal)) -Message 'host-artifacts.json omits the exact required Linux launch option.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$hostManifest.CodeSigning, 'unsigned-experimental', [System.StringComparison]::Ordinal)) -Message 'host-artifacts.json does not disclose unsigned experimental hosts.'
if ($SourceKind -eq 'ObservedPublished') {
Assert-DtmApiMultiPlatformAudit -Condition ([int]$hostManifest.ImportedPublishedPlayerPayload.FileCount -eq $script:DtmApiMultiPlatformPublishedPayloadFileCount -and [long]$hostManifest.ImportedPublishedPlayerPayload.Bytes -eq $script:DtmApiMultiPlatformPublishedPayloadBytes -and [string]::Equals([string]$hostManifest.ImportedPublishedPlayerPayload.TreeSha256, $script:DtmApiMultiPlatformPublishedPayloadTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'host-artifacts.json imported Runtime receipt is invalid.'
}
Assert-DtmApiMultiPlatformAudit -Condition ([int]$hostManifest.SharedPayload.FileCount -eq $accepted.SharedPayload.FileCount -and [long]$hostManifest.SharedPayload.Bytes -eq $accepted.SharedPayload.Bytes -and [string]::Equals([string]$hostManifest.SharedPayload.TreeSha256, $accepted.SharedPayload.TreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'host-artifacts.json shared Runtime receipt is invalid.'
$hostRows = @($hostManifest.Hosts)
Assert-DtmApiMultiPlatformAudit -Condition ($hostRows.Count -eq 2) -Message 'host-artifacts.json must contain exactly win-x64 and linux-x64.'
foreach ($expectedHost in @(
    [pscustomobject]@{ Rid = 'win-x64'; Path = $windowsHost; RelativePath = ([string]$packageMetadata.windowsHostPackagePath).Replace('\', '/') },
    [pscustomobject]@{ Rid = 'linux-x64'; Path = $linuxHost; RelativePath = ([string]$packageMetadata.linuxHostPackagePath).Replace('\', '/') }
)) {
    $matches = @($hostRows | Where-Object { [string]::Equals([string]$_.Rid, $expectedHost.Rid, [System.StringComparison]::Ordinal) })
    Assert-DtmApiMultiPlatformAudit -Condition ($matches.Count -eq 1) -Message "host-artifacts.json must contain exactly one $($expectedHost.Rid) row."
    $item = Get-Item -LiteralPath $expectedHost.Path -Force -ErrorAction Stop
    $hash = Get-DtmApiMultiPlatformFileSha256 -Path $expectedHost.Path
    Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$matches[0].RelativePath, $expectedHost.RelativePath, [System.StringComparison]::Ordinal) -and [long]$matches[0].Length -eq [long]$item.Length -and [string]::Equals([string]$matches[0].Sha256, $hash, [System.StringComparison]::OrdinalIgnoreCase)) -Message "host-artifacts.json receipt mismatch for $($expectedHost.Rid)."
}

Assert-DtmApiMultiPlatformAudit -Condition ([int]$runtimePackageManifest.SchemaVersion -eq $expectedSchema) -Message 'multiplatform-package.json schema differs from the selected source branch.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.DistributionId, 'dtmapi-multiplatform', [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json distribution identity is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.WorkshopId, $script:DtmApiMultiPlatformWorkshopId, [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json Workshop identity is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.DisplayName, $expectedPackageName, [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json display name is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.InstallerVersion, [string]$packageMetadata.installerVersion, [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json installer version is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.RuntimeVersion, $accepted.RuntimeVersion, [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json Runtime version is invalid.'
if ($SourceKind -eq 'ObservedPublished') {
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.SourceWorkshopId, '3743016467', [System.StringComparison]::Ordinal) -and [string]::Equals([string]$runtimePackageManifest.SourceWorkshopManifestId, '918505309011394484', [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json published Workshop source identity is invalid.'
}
if ($SourceKind -eq 'ObservedPublished') {
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.SourcePlayerPayloadTreeSha256, $script:DtmApiMultiPlatformPublishedPayloadTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'multiplatform-package.json source player payload hash is invalid.'
}
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.RequiredLinuxLaunchOption, $script:DtmApiMultiPlatformRequiredLaunchOption, [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json required Linux launch option is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.CrossOverDllOverride, 'winhttp=n,b', [System.StringComparison]::Ordinal)) -Message 'multiplatform-package.json CrossOver override is invalid.'
$bepInExPath = Join-Path $package 'Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip'
$bepInExItem = Get-Item -LiteralPath $bepInExPath -Force -ErrorAction Stop
$bepInExHash = Get-DtmApiMultiPlatformFileSha256 -Path $bepInExPath
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$runtimePackageManifest.BepInExArchive.RelativePath, 'Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip', [System.StringComparison]::Ordinal) -and [long]$runtimePackageManifest.BepInExArchive.Length -eq [long]$bepInExItem.Length -and [string]::Equals([string]$runtimePackageManifest.BepInExArchive.Sha256, $bepInExHash, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'multiplatform-package.json BepInEx archive receipt is invalid.'
$runtimeHostRows = @($runtimePackageManifest.HostArtifacts)
Assert-DtmApiMultiPlatformAudit -Condition ($runtimeHostRows.Count -eq 2) -Message 'multiplatform-package.json must contain exactly two host artifacts.'
foreach ($expectedHost in @(
    [pscustomobject]@{ Rid = 'win-x64'; Path = $windowsHost; RelativePath = ([string]$packageMetadata.windowsHostPackagePath).Replace('\', '/') },
    [pscustomobject]@{ Rid = 'linux-x64'; Path = $linuxHost; RelativePath = ([string]$packageMetadata.linuxHostPackagePath).Replace('\', '/') }
)) {
    $matches = @($runtimeHostRows | Where-Object { [string]::Equals([string]$_.Rid, $expectedHost.Rid, [System.StringComparison]::Ordinal) })
    Assert-DtmApiMultiPlatformAudit -Condition ($matches.Count -eq 1) -Message "multiplatform-package.json must contain exactly one $($expectedHost.Rid) row."
    $item = Get-Item -LiteralPath $expectedHost.Path -Force -ErrorAction Stop
    $hash = Get-DtmApiMultiPlatformFileSha256 -Path $expectedHost.Path
    Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$matches[0].RelativePath, $expectedHost.RelativePath, [System.StringComparison]::Ordinal) -and [long]$matches[0].Length -eq [long]$item.Length -and [string]::Equals([string]$matches[0].Sha256, $hash, [System.StringComparison]::OrdinalIgnoreCase)) -Message "multiplatform-package.json receipt mismatch for $($expectedHost.Rid)."
}

if ($SourceKind -eq 'Candidate') {
    foreach ($manifest in @($runtimePackageManifest, $hostManifest)) {
        foreach ($key in @('SourceWorkshopId','SourceWorkshopManifestId','SourcePlayerPayloadTreeSha256','ImportedPublishedPlayerPayload')) {
            Assert-DtmApiMultiPlatformAudit -Condition ($null -eq $manifest.PSObject.Properties[$key]) -Message "Candidate must not claim published provenance: $key"
        }
        Assert-DtmApiMultiPlatformAudit -Condition (($manifest.RuntimeSource | ConvertTo-Json -Depth 20 -Compress) -ceq ($accepted.RuntimeSource | ConvertTo-Json -Depth 20 -Compress)) -Message 'Candidate Runtime source differs from selected Windows artifact.'
    }
    $imported = $hostManifest.ImportedRuntimePayload
    Assert-DtmApiMultiPlatformAudit -Condition ($imported.FileCount -eq $accepted.PlayerPayload.FileCount -and $imported.Bytes -eq $accepted.PlayerPayload.Bytes -and $imported.TreeSha256 -ceq $accepted.PlayerPayload.TreeSha256 -and $imported.Algorithm -ceq $accepted.PlayerPayload.Algorithm) -Message 'Candidate source payload receipt differs.'
    Assert-DtmApiMultiPlatformHostBuildReceipt -Receipt $hostManifest.InstallerBuildSource -WindowsHost $windowsHost -LinuxHost $linuxHost
    Assert-DtmApiMultiPlatformAudit -Condition ($hostManifest.InstallerBuildCommit -ceq $hostManifest.InstallerBuildSource.Commit -and $runtimePackageManifest.InstallerBuildCommit -ceq $hostManifest.InstallerBuildSource.Commit -and $hostManifest.InstallerVersion -ceq $packageMetadata.installerVersion -and $hostManifest.InstallerSourceTreeState -ceq 'clean-committed-inputs') -Message 'Candidate installer source differs from its build.'
}

$powershellSyntaxHost = 'skipped'
if (-not $SkipPowerShell51Syntax) {
    $packagedPowerShell = [string[]]@(Get-ChildItem -LiteralPath $package -Filter '*.ps1' -File -Force -Recurse | ForEach-Object { $_.FullName })
    Assert-DtmApiMultiPlatformAudit -Condition ($packagedPowerShell.Count -eq 9) -Message "accepted Windows route must contain exactly nine PowerShell scripts; found $($packagedPowerShell.Count)."
    Test-DtmApiMultiPlatformPowerShell51Syntax -Paths $packagedPowerShell
    $powershellSyntaxHost = (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe')
}

$bashSyntaxHost = 'skipped'
if (-not $SkipBashSyntax) {
    $bashSyntaxHost = Test-DtmApiMultiPlatformBashSyntax -Paths ([string[]]$shellPaths.ToArray())
}

$packageReceipt = Get-DtmApiMultiPlatformPublishedContentReceipt -Root $package
$catalog = Get-Content -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$catalogRows = @($catalog.runtime.distributions | Where-Object { [string]::Equals([string]$_.distributionId, 'dtmapi-multiplatform', [System.StringComparison]::Ordinal) })
Assert-DtmApiMultiPlatformAudit -Condition ($catalogRows.Count -eq 1) -Message 'Product Catalog must contain exactly one dtmapi-multiplatform Runtime distribution row.'
$catalogRow = $catalogRows[0]
Assert-DtmApiMultiPlatformAudit -Condition ($null -eq $catalogRow.PSObject.Properties['uniqueId']) -Message 'Distribution rows must not invent a second Runtime UniqueID.'
if ($AllowDeliveredWorkshopControlFile) {
    Assert-DtmApiMultiPlatformAudit -Condition ($SourceKind -eq 'Candidate' -and $catalogRow.workshopControlFileDelivered -eq $true -and $info.version -ceq $catalogRow.releaseVersion) -Message 'The delivered control file requires the current observed schema-2 release.'
    Assert-DtmApiMultiPlatformAudit -Condition ($packageReceipt.FileCount -eq $catalogRow.playerPayloadFileCount -and $packageReceipt.Bytes -eq $catalogRow.playerPayloadBytes -and $packageReceipt.TreeSha256 -ceq $catalogRow.playerPayloadTreeSha256 -and $workshopControlReceipt.Bytes -eq $catalogRow.workshopControlFileBytes -and $workshopControlReceipt.Sha256 -ceq $catalogRow.workshopControlFileSha256) -Message 'Delivered subscription bytes differ from the frozen content/control receipts.'
}
if ($SourceKind -eq 'ObservedPublished' -and $catalogRow.PSObject.Properties['observed061Baseline']) {
    # Schema 1 retains its original publication provenance after the current release advances.
    $catalogRow = $catalogRow.observed061Baseline
}
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$catalogRow.workshopId, $script:DtmApiMultiPlatformWorkshopId, [System.StringComparison]::Ordinal) -and [string]$catalogRow.workshopManifestId -match '^\d+$') -Message 'Multi-platform Catalog Workshop identity is invalid.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$catalogRow.artifactState, 'SteamPublishedObservedExact', [System.StringComparison]::Ordinal) -and [string]$catalogRow.candidateState -cin @('LocalMetadataSuccessorPendingUpload', 'PublishedMetadataSuccessor') -and [string]::Equals([string]$catalogRow.uploadAuthorization, 'None', [System.StringComparison]::Ordinal)) -Message 'Multi-platform Catalog publication/candidate state is invalid.'
if ($SourceKind -eq 'ObservedPublished') {
Assert-DtmApiMultiPlatformAudit -Condition ([int]$catalogRow.candidateFileCount -eq [int]$packageReceipt.FileCount -and [long]$catalogRow.candidateBytes -eq [long]$packageReceipt.Bytes -and [string]::Equals([string]$catalogRow.candidateTreeSha256, [string]$packageReceipt.TreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'Multi-platform Catalog candidate receipt does not match this package.'
Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$catalogRow.sourcePlayerPayloadTreeSha256, $script:DtmApiMultiPlatformPublishedPayloadTreeSha256, [System.StringComparison]::OrdinalIgnoreCase) -and [string]::Equals([string]$catalogRow.sharedPayloadTreeSha256, $script:DtmApiMultiPlatformSharedTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)) -Message 'Multi-platform Catalog source/shared Runtime receipts are invalid.'
$catalogHosts = @($catalogRow.hostArtifacts)
Assert-DtmApiMultiPlatformAudit -Condition ($catalogHosts.Count -eq 2) -Message 'Multi-platform Catalog row must contain exactly two host receipts.'
foreach ($manifestHost in $hostRows) {
    $catalogHost = @($catalogHosts | Where-Object { [string]::Equals([string]$_.rid, [string]$manifestHost.Rid, [System.StringComparison]::Ordinal) })
    Assert-DtmApiMultiPlatformAudit -Condition ($catalogHost.Count -eq 1) -Message "Multi-platform Catalog row must contain exactly one $($manifestHost.Rid) host."
    Assert-DtmApiMultiPlatformAudit -Condition ([string]::Equals([string]$catalogHost[0].relativePath, [string]$manifestHost.RelativePath, [System.StringComparison]::Ordinal) -and [long]$catalogHost[0].length -eq [long]$manifestHost.Length -and [string]::Equals([string]$catalogHost[0].sha256, [string]$manifestHost.Sha256, [System.StringComparison]::OrdinalIgnoreCase)) -Message "Multi-platform Catalog host receipt mismatch for $($manifestHost.Rid)."
}
}
$result = [ordered]@{
    SchemaVersion = 1
    SourceKind = $SourceKind
    Passed = $true
    PackageRoot = $package
    AcceptedRuntimeRoot = $accepted.Root
    AcceptedWindowsRuntime = [ordered]@{
        ExeCount = $acceptedWindowsExeFiles.Count
    }
    ImportedPlayerPayload = [ordered]@{
        FileCount = [int]$accepted.PlayerPayload.FileCount
        Bytes = [long]$accepted.PlayerPayload.Bytes
        TreeSha256 = [string]$accepted.PlayerPayload.TreeSha256
    }
    SharedPayload = [ordered]@{
        FileCount = [int]$sharedReceipt.FileCount
        Bytes = [long]$sharedReceipt.Bytes
        TreeSha256 = [string]$sharedReceipt.TreeSha256
    }
    Candidate = [ordered]@{
        FileCount = [int]$packageReceipt.FileCount
        Bytes = [long]$packageReceipt.Bytes
        TreeSha256 = [string]$packageReceipt.TreeSha256
        ExeCount = $exeFiles.Count
        ElfCount = $elfRelativePaths.Count
        RootBatShimCount = $rootBatFiles.Count
        RootCmdCount = $rootCmdFiles.Count
        WorkshopJsonCount = $workshopControlFiles.Count
    }
    WorkshopControl = if ($null -eq $workshopControlReceipt) {
        $null
    }
    else {
        [ordered]@{
            WorkshopId = [string]$workshopControlReceipt.WorkshopId
            Bytes = [long]$workshopControlReceipt.Bytes
            Sha256 = [string]$workshopControlReceipt.Sha256
        }
    }
    PowerShell51SyntaxHost = $powershellSyntaxHost
    BashSyntaxHost = $bashSyntaxHost
}

if (-not [string]::IsNullOrWhiteSpace($EvidencePath)) {
    $resolvedEvidence = [System.IO.Path]::GetFullPath($EvidencePath)
    Write-DtmApiMultiPlatformJsonNoBom -Path $resolvedEvidence -Value $result
}
if (-not $Quiet) {
    Write-Host ("DTMAPI multi-platform package audit PASS: {0}" -f $package)
    Write-Host ("Shared Runtime bytes: {0} files, {1} bytes, {2}" -f $sharedReceipt.FileCount, $sharedReceipt.Bytes, $sharedReceipt.TreeSha256)
    Write-Host ("Candidate: {0} files, {1} bytes, {2}" -f $packageReceipt.FileCount, $packageReceipt.Bytes, $packageReceipt.TreeSha256)
    Write-Host ("Host boundary: EXE={0}; ELF={1}; workshop.json={2}" -f $exeFiles.Count, $elfRelativePaths.Count, $workshopControlFiles.Count)
}

[pscustomobject]$result
