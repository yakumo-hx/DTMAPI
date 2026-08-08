[CmdletBinding()]
param(
    [string] $DestinationDirectory = '',
    [string] $UserProfileRoot = '',
    [string[]] $PersistentRoot = @(),
    [string[]] $GameDirectory = @(),
    [string[]] $CrashTempRoot = @(),
    [switch] $SkipWindowsEvents,
    [switch] $SkipProcessCheck
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$script:Warnings = New-Object 'System.Collections.Generic.List[string]'
$script:Errors = New-Object 'System.Collections.Generic.List[string]'
$script:Records = New-Object 'System.Collections.Generic.List[object]'
$script:CopiedFiles = 0
$script:CopiedBytes = [long]0
$script:SaveRootsFound = 0
$script:SaveFilesCopied = 0
$script:SaveCopyFailures = 0
$script:StageRoot = ''
$script:StagePrefix = ''
$script:Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Test-DtmSupportEnvironmentTrue {
    param([string] $Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    return -not [string]::IsNullOrWhiteSpace($value) -and @('1', 'true', 'yes', 'on') -contains $value.Trim().ToLowerInvariant()
}

function Add-DtmSupportWarning {
    param([string] $Message)

    [void]$script:Warnings.Add($Message)
    Write-Host ('[WARN] ' + $Message) -ForegroundColor Yellow
}

function Add-DtmSupportError {
    param([string] $Message)

    [void]$script:Errors.Add($Message)
    Write-Host ('[ERROR] ' + $Message) -ForegroundColor Red
}

function Get-DtmSupportSafeName {
    param([string] $Value)

    if ($null -eq $Value) {
        $Value = ''
    }
    $safe = [regex]::Replace($Value, '[^A-Za-z0-9._-]+', '_')
    $safe = $safe.Trim([char[]]@('_', '.'))
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'item'
    }
    if ($safe.Length -gt 80) {
        return $safe.Substring(0, 80)
    }
    return $safe
}

function Get-DtmSupportSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        $share)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-DtmSupportUniqueExistingDirectories {
    param([object[]] $Candidates)

    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $result = New-Object 'System.Collections.Generic.List[string]'
    foreach ($candidateValue in @($Candidates)) {
        $candidate = [string]$candidateValue
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }
        try {
            $full = [System.IO.Path]::GetFullPath($candidate).TrimEnd('\', '/')
            if ([System.IO.Directory]::Exists($full) -and $seen.Add($full)) {
                [void]$result.Add($full)
            }
        }
        catch {
            Add-DtmSupportWarning ("Ignored invalid directory candidate '{0}': {1}" -f $candidate, $_.Exception.Message)
        }
    }
    return @($result)
}

function Join-DtmSupportStagePath {
    param([Parameter(Mandatory = $true)] [string] $RelativePath)

    $target = [System.IO.Path]::GetFullPath((Join-Path $script:StageRoot $RelativePath))
    if (-not $target.StartsWith($script:StagePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Collector destination escaped the staging root: $RelativePath"
    }
    return $target
}

function Add-DtmSupportRecord {
    param(
        [string] $Category,
        [string] $Source,
        [string] $Destination,
        [long] $Length,
        [string] $LastWriteTimeUtc,
        [string] $Sha256,
        [string] $Status,
        [string] $Details
    )

    [void]$script:Records.Add([pscustomobject]@{
        Category = $Category
        Source = $Source
        Destination = $Destination
        Length = $Length
        LastWriteTimeUtc = $LastWriteTimeUtc
        Sha256 = $Sha256
        Status = $Status
        Details = $Details
    })
}

function Copy-DtmSupportSourceFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $DestinationRelative,
        [Parameter(Mandatory = $true)] [string] $Category
    )

    $destination = Join-DtmSupportStagePath -RelativePath $DestinationRelative
    try {
        $before = New-Object System.IO.FileInfo($Source)
        $before.Refresh()
        if (-not $before.Exists) {
            throw 'Source file disappeared before copy.'
        }
        $beforeLength = $before.Length
        $beforeWriteTicks = $before.LastWriteTimeUtc.Ticks
        $beforeWriteText = $before.LastWriteTimeUtc.ToString('o')

        $parent = [System.IO.Path]::GetDirectoryName($destination)
        if (-not [string]::IsNullOrWhiteSpace($parent)) {
            [System.IO.Directory]::CreateDirectory($parent) | Out-Null
        }
        [System.IO.File]::Copy($before.FullName, $destination, $false)

        $sourceHash = Get-DtmSupportSha256 -Path $before.FullName
        $destinationHash = Get-DtmSupportSha256 -Path $destination
        $after = New-Object System.IO.FileInfo($before.FullName)
        $after.Refresh()
        if (-not $after.Exists -or
            $after.Length -ne $beforeLength -or
            $after.LastWriteTimeUtc.Ticks -ne $beforeWriteTicks) {
            throw 'Source file changed while it was being copied.'
        }
        if ((New-Object System.IO.FileInfo($destination)).Length -ne $beforeLength -or
            -not $sourceHash.Equals($destinationHash, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Destination length or SHA-256 did not match the stable source.'
        }

        $relativeOutput = $destination.Substring($script:StagePrefix.Length).Replace('\', '/')
        Add-DtmSupportRecord -Category $Category -Source $before.FullName -Destination $relativeOutput -Length $beforeLength -LastWriteTimeUtc $beforeWriteText -Sha256 $sourceHash -Status 'copied' -Details ''
        $script:CopiedFiles++
        $script:CopiedBytes += $beforeLength
        if ($Category -eq 'SAVE') {
            $script:SaveFilesCopied++
        }
        return $true
    }
    catch {
        if ([System.IO.File]::Exists($destination)) {
            try {
                [System.IO.File]::Delete($destination)
            }
            catch {
            }
        }
        $details = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
        Add-DtmSupportRecord -Category $Category -Source $Source -Destination $DestinationRelative.Replace('\', '/') -Length 0 -LastWriteTimeUtc '' -Sha256 '' -Status 'failed' -Details $details
        Add-DtmSupportError ("Could not copy '{0}': {1}" -f $Source, $details)
        if ($Category -eq 'SAVE') {
            $script:SaveCopyFailures++
        }
        return $false
    }
}

function Copy-DtmSupportTree {
    param(
        [Parameter(Mandatory = $true)] [string] $SourceRoot,
        [Parameter(Mandatory = $true)] [string] $DestinationRelativeRoot,
        [Parameter(Mandatory = $true)] [string] $Category
    )

    $rootInfo = New-Object System.IO.DirectoryInfo($SourceRoot)
    $rootInfo.Refresh()
    if (-not $rootInfo.Exists) {
        return
    }
    if (($rootInfo.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        Add-DtmSupportError ("Refused reparse-point source directory: {0}" -f $rootInfo.FullName)
        if ($Category -eq 'SAVE') {
            $script:SaveCopyFailures++
        }
        return
    }

    $sourceFull = $rootInfo.FullName.TrimEnd('\', '/')
    $stack = New-Object 'System.Collections.Generic.Stack[string]'
    $stack.Push($sourceFull)
    while ($stack.Count -gt 0) {
        $current = $stack.Pop()
        $currentRelative = $current.Substring($sourceFull.Length).TrimStart([char[]]@('\', '/'))
        $destinationDirectoryRelative = $DestinationRelativeRoot
        if (-not [string]::IsNullOrWhiteSpace($currentRelative)) {
            $destinationDirectoryRelative = Join-Path $DestinationRelativeRoot $currentRelative
        }
        [System.IO.Directory]::CreateDirectory((Join-DtmSupportStagePath -RelativePath $destinationDirectoryRelative)) | Out-Null

        try {
            $children = @(Get-ChildItem -LiteralPath $current -Force -ErrorAction Stop)
        }
        catch {
            $details = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
            Add-DtmSupportError ("Could not enumerate '{0}': {1}" -f $current, $details)
            if ($Category -eq 'SAVE') {
                $script:SaveCopyFailures++
            }
            continue
        }

        foreach ($child in $children) {
            if (($child.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                if ($Category -eq 'SAVE') {
                    Add-DtmSupportError ("Refused SAVE reparse point: {0}" -f $child.FullName)
                    $script:SaveCopyFailures++
                }
                else {
                    Add-DtmSupportWarning ("Skipped reparse point: {0}" -f $child.FullName)
                }
                continue
            }
            if ($child.PSIsContainer) {
                $stack.Push($child.FullName)
                continue
            }

            $relative = $child.FullName.Substring($sourceFull.Length).TrimStart([char[]]@('\', '/'))
            $destinationRelative = Join-Path $DestinationRelativeRoot $relative
            [void](Copy-DtmSupportSourceFile -Source $child.FullName -DestinationRelative $destinationRelative -Category $Category)
        }
    }
}

function Test-DtmSupportGameRoot {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not [System.IO.Directory]::Exists($Path)) {
        return $false
    }
    return [System.IO.File]::Exists((Join-Path $Path 'DolocTown.exe')) -or
        ([System.IO.Directory]::Exists((Join-Path $Path 'DTMAPI\logs')) -and
        [System.IO.File]::Exists((Join-Path $Path 'BepInEx\LogOutput.log')))
}

function Add-DtmSupportSteamCandidates {
    param([System.Collections.Generic.List[string]] $Candidates)

    $steamRoots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($registryPath in @(
        'HKCU:\Software\Valve\Steam',
        'HKLM:\Software\Valve\Steam',
        'HKLM:\Software\WOW6432Node\Valve\Steam')) {
        try {
            $item = Get-ItemProperty -LiteralPath $registryPath -ErrorAction Stop
            foreach ($propertyName in @('SteamPath', 'InstallPath')) {
                $value = [string]$item.$propertyName
                if (-not [string]::IsNullOrWhiteSpace($value)) {
                    [void]$steamRoots.Add($value)
                }
            }
        }
        catch {
        }
    }

    $uniqueSteamRoots = Get-DtmSupportUniqueExistingDirectories -Candidates @($steamRoots)
    foreach ($steamRoot in $uniqueSteamRoots) {
        [void]$Candidates.Add((Join-Path $steamRoot 'steamapps\common\Doloc Town'))
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (-not [System.IO.File]::Exists($libraryFile)) {
            continue
        }
        try {
            $content = [System.IO.File]::ReadAllText($libraryFile)
            foreach ($match in [regex]::Matches($content, '"path"\s+"([^"]+)"', [Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
                $libraryRoot = $match.Groups[1].Value.Replace('\\', '\')
                if (-not [string]::IsNullOrWhiteSpace($libraryRoot)) {
                    [void]$Candidates.Add((Join-Path $libraryRoot 'steamapps\common\Doloc Town'))
                }
            }
        }
        catch {
            Add-DtmSupportWarning ("Could not read Steam library metadata '{0}': {1}" -f $libraryFile, $_.Exception.Message)
        }
    }
}

function Get-DtmSupportGameDirectories {
    param(
        [string[]] $ExplicitCandidates,
        [string] $ProfileRoot,
        [string] $DesktopRoot,
        [bool] $DisableDiscovery
    )

    $candidates = New-Object 'System.Collections.Generic.List[string]'
    foreach ($value in @($ExplicitCandidates)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
            [void]$candidates.Add([string]$value)
        }
    }

    if (-not $DisableDiscovery) {
        $supportRoot = Join-Path $DesktopRoot 'DTMAPI-logs'
        if ([System.IO.Directory]::Exists($supportRoot)) {
            foreach ($summary in @(Get-ChildItem -LiteralPath $supportRoot -Recurse -File -Filter 'summary.txt' -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 20)) {
                try {
                    $text = [System.IO.File]::ReadAllText($summary.FullName)
                    $match = [regex]::Match($text, '(?m)^GameDir=(.+?)\s*$')
                    if ($match.Success) {
                        [void]$candidates.Add($match.Groups[1].Value.Trim())
                    }
                }
                catch {
                }
            }
        }

        $tencentRoot = Join-Path $ProfileRoot 'Documents\Tencent Files'
        if ([System.IO.Directory]::Exists($tencentRoot)) {
            foreach ($account in @(Get-ChildItem -LiteralPath $tencentRoot -Directory -Force -ErrorAction SilentlyContinue)) {
                [void]$candidates.Add((Join-Path $account.FullName 'FileRecv\steamapps\common\Doloc Town'))
            }
            [void]$candidates.Add((Join-Path $tencentRoot 'FileRecv\steamapps\common\Doloc Town'))
        }

        Add-DtmSupportSteamCandidates -Candidates $candidates

        $ancestor = New-Object System.IO.DirectoryInfo($PSScriptRoot)
        for ($i = 0; $i -lt 8 -and $null -ne $ancestor; $i++) {
            [void]$candidates.Add($ancestor.FullName)
            $ancestor = $ancestor.Parent
        }
    }

    $result = New-Object 'System.Collections.Generic.List[string]'
    foreach ($candidate in @(Get-DtmSupportUniqueExistingDirectories -Candidates @($candidates))) {
        if (Test-DtmSupportGameRoot -Path $candidate) {
            [void]$result.Add($candidate)
        }
    }
    return @($result)
}

function Write-DtmSupportWindowsEvents {
    param([string] $DestinationRelative)

    $destination = Join-DtmSupportStagePath -RelativePath $DestinationRelative
    try {
        $startTime = (Get-Date).AddDays(-7)
        $events = @(Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = $startTime } -MaxEvents 3000 -ErrorAction Stop |
            Where-Object {
                $provider = [string]$_.ProviderName
                $message = [string]$_.Message
                ($provider -match 'Application Error|Windows Error Reporting|\.NET Runtime') -and
                (($message + ' ' + $provider) -match 'DolocTown|UnityPlayer|mono-2\.0-bdwgc|UnityCrashHandler')
            } |
            Select-Object -First 250)

        $lines = New-Object 'System.Collections.Generic.List[string]'
        [void]$lines.Add('DTMAPI player support - matching Windows Application crash events')
        [void]$lines.Add('StartTime=' + $startTime.ToString('o'))
        [void]$lines.Add('Matched=' + $events.Count)
        [void]$lines.Add('')
        foreach ($event in $events) {
            [void]$lines.Add(('TimeCreated={0}' -f $event.TimeCreated.ToString('o')))
            [void]$lines.Add(('Provider={0}' -f $event.ProviderName))
            [void]$lines.Add(('Id={0}; Level={1}; RecordId={2}' -f $event.Id, $event.LevelDisplayName, $event.RecordId))
            [void]$lines.Add(([string]$event.Message))
            [void]$lines.Add('---')
        }
        [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($destination)) | Out-Null
        [System.IO.File]::WriteAllLines($destination, $lines.ToArray(), $script:Utf8NoBom)
    }
    catch {
        Add-DtmSupportWarning ("Windows Application crash events were not collected: {0}" -f $_.Exception.Message)
    }
}

if ([string]::IsNullOrWhiteSpace($UserProfileRoot)) {
    $UserProfileRoot = [Environment]::GetEnvironmentVariable('DTMAPI_SUPPORT_USERPROFILE')
}
if ([string]::IsNullOrWhiteSpace($UserProfileRoot)) {
    $UserProfileRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
}
if ([string]::IsNullOrWhiteSpace($UserProfileRoot)) {
    $UserProfileRoot = $env:USERPROFILE
}
$UserProfileRoot = [System.IO.Path]::GetFullPath($UserProfileRoot).TrimEnd('\', '/')

if (@($PersistentRoot).Count -eq 0 -and -not [string]::IsNullOrWhiteSpace($env:DTMAPI_SUPPORT_PERSISTENT_ROOT)) {
    $PersistentRoot = @($env:DTMAPI_SUPPORT_PERSISTENT_ROOT)
}
if (@($GameDirectory).Count -eq 0 -and -not [string]::IsNullOrWhiteSpace($env:DTMAPI_SUPPORT_GAME_DIR)) {
    $GameDirectory = @($env:DTMAPI_SUPPORT_GAME_DIR)
}
if (@($CrashTempRoot).Count -eq 0 -and -not [string]::IsNullOrWhiteSpace($env:DTMAPI_SUPPORT_TEMP_ROOT)) {
    $CrashTempRoot = @($env:DTMAPI_SUPPORT_TEMP_ROOT)
}
if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) {
    $DestinationDirectory = $env:DTMAPI_SUPPORT_OUTPUT_DIR
}
if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) {
    $DestinationDirectory = [Environment]::GetFolderPath([Environment+SpecialFolder]::Desktop)
}
if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) {
    $DestinationDirectory = Join-Path $UserProfileRoot 'Desktop'
}

$skipProcess = $SkipProcessCheck -or (Test-DtmSupportEnvironmentTrue -Name 'DTMAPI_SUPPORT_SKIP_PROCESS_CHECK')
if (-not $skipProcess -and @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue).Count -gt 0) {
    Write-Host '[ERROR] Doloc Town is still running.' -ForegroundColor Red
    Write-Host 'Close the game completely, then run this collector again.'
    exit 3
}

$workRoot = $env:DTMAPI_SUPPORT_WORK_ROOT
if ([string]::IsNullOrWhiteSpace($workRoot)) {
    $workRoot = [System.IO.Path]::GetTempPath()
}
[System.IO.Directory]::CreateDirectory($workRoot) | Out-Null
try {
    [System.IO.Directory]::CreateDirectory($DestinationDirectory) | Out-Null
}
catch {
    $fallback = Join-Path ([System.IO.Path]::GetTempPath()) 'DTMAPI-player-support'
    [System.IO.Directory]::CreateDirectory($fallback) | Out-Null
    Write-Host ("[WARN] Could not use output directory '{0}'. Falling back to '{1}'." -f $DestinationDirectory, $fallback) -ForegroundColor Yellow
    $DestinationDirectory = $fallback
}
$DestinationDirectory = [System.IO.Path]::GetFullPath($DestinationDirectory).TrimEnd('\', '/')

$token = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$baseName = 'DTMAPI-player-support-' + $token
$script:StageRoot = [System.IO.Path]::GetFullPath((Join-Path $workRoot ('.' + $baseName + '.partial'))).TrimEnd('\', '/')
$script:StagePrefix = $script:StageRoot + [System.IO.Path]::DirectorySeparatorChar
$partialZip = Join-Path $DestinationDirectory ('.' + $baseName + '.partial.zip')
$finalZip = Join-Path $DestinationDirectory ($baseName + '.zip')
$published = $false

try {
    if ([System.IO.Directory]::Exists($script:StageRoot) -or [System.IO.File]::Exists($partialZip) -or [System.IO.File]::Exists($finalZip)) {
        throw 'Unique collector staging/output token unexpectedly already exists.'
    }
    [System.IO.Directory]::CreateDirectory($script:StageRoot) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $script:StageRoot '.dtmapi-player-support-staging'), $token, $script:Utf8NoBom)

    Write-Host '[INFO] Locating the hidden Doloc Town SAVE folder...'
    $persistentCandidates = New-Object 'System.Collections.Generic.List[string]'
    foreach ($value in @($PersistentRoot)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
            [void]$persistentCandidates.Add([string]$value)
        }
    }
    [void]$persistentCandidates.Add((Join-Path $UserProfileRoot 'AppData\LocalLow\RedSawGames\DolocTown'))
    [void]$persistentCandidates.Add((Join-Path $UserProfileRoot 'AppData\LocalLow\RedSawGames\Doloc Town'))
    $persistentRoots = @(Get-DtmSupportUniqueExistingDirectories -Candidates @($persistentCandidates))

    $persistentIndex = 0
    foreach ($root in $persistentRoots) {
        $persistentIndex++
        $label = ('Persistent-{0:D2}-{1}' -f $persistentIndex, (Get-DtmSupportSafeName -Value ([System.IO.Path]::GetFileName($root))))
        $saveRoot = Join-Path $root 'SAVE'
        if ([System.IO.Directory]::Exists($saveRoot)) {
            $script:SaveRootsFound++
            Write-Host ('[FOUND] SAVE: ' + $saveRoot) -ForegroundColor Green
            Copy-DtmSupportTree -SourceRoot $saveRoot -DestinationRelativeRoot (Join-Path (Join-Path 'PlayerData' $label) 'SAVE') -Category 'SAVE'
        }
        else {
            Add-DtmSupportWarning ("SAVE directory was not present under '{0}'." -f $root)
        }

        foreach ($unityLogName in @('Player.log', 'Player-prev.log', 'output_log.txt')) {
            $unityLog = Join-Path $root $unityLogName
            if ([System.IO.File]::Exists($unityLog)) {
                [void](Copy-DtmSupportSourceFile -Source $unityLog -DestinationRelative (Join-Path (Join-Path 'PlayerData' $label) $unityLogName) -Category 'UnityLog')
            }
        }
    }

    if ($script:SaveRootsFound -eq 0) {
        Add-DtmSupportError ("No SAVE directory was found under '{0}\AppData\LocalLow\RedSawGames'." -f $UserProfileRoot)
    }
    elseif ($script:SaveFilesCopied -eq 0) {
        Add-DtmSupportError 'The SAVE directory was found but no SAVE files were copied.'
        $script:SaveCopyFailures++
    }

    $disableGameDiscovery = Test-DtmSupportEnvironmentTrue -Name 'DTMAPI_SUPPORT_NO_GAME_DISCOVERY'
    $gameRoots = @(Get-DtmSupportGameDirectories -ExplicitCandidates $GameDirectory -ProfileRoot $UserProfileRoot -DesktopRoot $DestinationDirectory -DisableDiscovery $disableGameDiscovery)
    if ($gameRoots.Count -eq 0) {
        Add-DtmSupportWarning 'No Doloc Town game directory was found. SAVE and user/crash evidence collection will continue.'
    }
    $gameIndex = 0
    foreach ($gameRoot in $gameRoots) {
        $gameIndex++
        $gameLabel = ('Game-{0:D2}' -f $gameIndex)
        Write-Host ('[FOUND] Game: ' + $gameRoot) -ForegroundColor Green
        $dtmapiLogs = Join-Path $gameRoot 'DTMAPI\logs'
        if ([System.IO.Directory]::Exists($dtmapiLogs)) {
            Copy-DtmSupportTree -SourceRoot $dtmapiLogs -DestinationRelativeRoot (Join-Path $gameLabel 'DTMAPI\logs') -Category 'DTMAPILog'
        }
        else {
            Add-DtmSupportWarning ("DTMAPI logs were not found under '{0}'." -f $gameRoot)
        }

        $bepInExLog = Join-Path $gameRoot 'BepInEx\LogOutput.log'
        if ([System.IO.File]::Exists($bepInExLog)) {
            [void](Copy-DtmSupportSourceFile -Source $bepInExLog -DestinationRelative (Join-Path $gameLabel 'BepInEx\LogOutput.log') -Category 'BepInExLog')
        }
        else {
            Add-DtmSupportWarning ("BepInEx LogOutput.log was not found under '{0}'." -f $gameRoot)
        }

        $reports = Join-Path $gameRoot 'DTMAPI\reports'
        if ([System.IO.Directory]::Exists($reports)) {
            Copy-DtmSupportTree -SourceRoot $reports -DestinationRelativeRoot (Join-Path $gameLabel 'DTMAPI\reports') -Category 'DTMAPIReport'
        }

        $stateFiles = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
        foreach ($pattern in @('install-state.json', 'release-manifest.json', 'install-state.failed-*.json', 'uninstall-state-*.json')) {
            foreach ($file in @(Get-ChildItem -LiteralPath (Join-Path $gameRoot 'DTMAPI') -File -Filter $pattern -ErrorAction SilentlyContinue)) {
                [void]$stateFiles.Add($file)
            }
        }
        foreach ($stateFile in $stateFiles) {
            [void](Copy-DtmSupportSourceFile -Source $stateFile.FullName -DestinationRelative (Join-Path $gameLabel ('DTMAPI\state\' + $stateFile.Name)) -Category 'DTMAPIState')
        }

        $debugConsoleState = Join-Path $gameRoot 'DTMAPI\debug-console-last-give.txt'
        if ([System.IO.File]::Exists($debugConsoleState)) {
            [void](Copy-DtmSupportSourceFile -Source $debugConsoleState -DestinationRelative (Join-Path $gameLabel 'DTMAPI\state\debug-console-last-give.txt') -Category 'DTMAPIState')
        }
    }

    $tempCandidates = New-Object 'System.Collections.Generic.List[string]'
    foreach ($value in @($CrashTempRoot)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
            [void]$tempCandidates.Add([string]$value)
        }
    }
    if (@($CrashTempRoot).Count -eq 0) {
        foreach ($value in @([System.IO.Path]::GetTempPath(), $env:TEMP, $env:TMP, (Join-Path $UserProfileRoot 'AppData\Local\Temp'))) {
            if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
                [void]$tempCandidates.Add([string]$value)
            }
        }
    }

    $crashRootCandidates = New-Object 'System.Collections.Generic.List[string]'
    foreach ($tempRoot in @(Get-DtmSupportUniqueExistingDirectories -Candidates @($tempCandidates))) {
        [void]$crashRootCandidates.Add((Join-Path $tempRoot 'RedSawGames\DolocTown\Crashes'))
        [void]$crashRootCandidates.Add((Join-Path $tempRoot 'RedSawGames\Doloc Town\Crashes'))
    }
    $crashRoots = @(Get-DtmSupportUniqueExistingDirectories -Candidates @($crashRootCandidates))
    if ($crashRoots.Count -eq 0) {
        Add-DtmSupportWarning 'No Unity Crashes directory was found for the current Windows user.'
    }
    $crashIndex = 0
    foreach ($crashRoot in $crashRoots) {
        $crashIndex++
        Write-Host ('[FOUND] Unity crashes: ' + $crashRoot) -ForegroundColor Green
        Copy-DtmSupportTree -SourceRoot $crashRoot -DestinationRelativeRoot ('Unity-Crashes\Root-{0:D2}' -f $crashIndex) -Category 'UnityCrash'
    }

    $localCrashDumpRoot = Join-Path $UserProfileRoot 'AppData\Local\CrashDumps'
    if ([System.IO.Directory]::Exists($localCrashDumpRoot)) {
        foreach ($dump in @(Get-ChildItem -LiteralPath $localCrashDumpRoot -File -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^(DolocTown|UnityCrashHandler).*\.dmp$' })) {
            [void](Copy-DtmSupportSourceFile -Source $dump.FullName -DestinationRelative (Join-Path 'Windows-CrashDumps' $dump.Name) -Category 'WindowsCrashDump')
        }
    }

    $skipEvents = $SkipWindowsEvents -or (Test-DtmSupportEnvironmentTrue -Name 'DTMAPI_SUPPORT_SKIP_WINDOWS_EVENTS')
    if (-not $skipEvents) {
        Write-DtmSupportWindowsEvents -DestinationRelative 'Windows-Events\Application-crash-events.txt'
    }

    $inventoryPath = Join-DtmSupportStagePath -RelativePath 'source-file-inventory.csv'
    if ($script:Records.Count -gt 0) {
        $script:Records.ToArray() | Export-Csv -LiteralPath $inventoryPath -NoTypeInformation -Encoding UTF8
    }
    else {
        [System.IO.File]::WriteAllText($inventoryPath, 'Category,Source,Destination,Length,LastWriteTimeUtc,Sha256,Status,Details', $script:Utf8NoBom)
    }

    $collectionComplete = $script:SaveRootsFound -gt 0 -and $script:SaveFilesCopied -gt 0 -and $script:SaveCopyFailures -eq 0 -and $script:Errors.Count -eq 0
    $status = if ($collectionComplete) { 'Complete' } else { 'Incomplete' }
    $summary = New-Object 'System.Collections.Generic.List[string]'
    [void]$summary.Add('DTMAPI player SAVE and crash-log collector')
    [void]$summary.Add('CollectionStatus=' + $status)
    [void]$summary.Add('CollectedAt=' + (Get-Date).ToString('o'))
    [void]$summary.Add('UserProfileRoot=' + $UserProfileRoot)
    [void]$summary.Add('SaveRootsFound=' + $script:SaveRootsFound)
    [void]$summary.Add('SaveFilesCopied=' + $script:SaveFilesCopied)
    [void]$summary.Add('SaveCopyFailures=' + $script:SaveCopyFailures)
    [void]$summary.Add('PersistentRootsFound=' + $persistentRoots.Count)
    [void]$summary.Add('GameRootsFound=' + $gameRoots.Count)
    [void]$summary.Add('UnityCrashRootsFound=' + $crashRoots.Count)
    [void]$summary.Add('FilesCopied=' + $script:CopiedFiles)
    [void]$summary.Add('BytesCopied=' + $script:CopiedBytes)
    [void]$summary.Add('Warnings=' + $script:Warnings.Count)
    [void]$summary.Add('Errors=' + $script:Errors.Count)
    [void]$summary.Add('')
    [void]$summary.Add('Persistent roots:')
    foreach ($value in $persistentRoots) { [void]$summary.Add('- ' + $value) }
    [void]$summary.Add('Game roots:')
    foreach ($value in $gameRoots) { [void]$summary.Add('- ' + $value) }
    [void]$summary.Add('Unity crash roots:')
    foreach ($value in $crashRoots) { [void]$summary.Add('- ' + $value) }
    [void]$summary.Add('')
    [void]$summary.Add('Warnings:')
    if ($script:Warnings.Count -eq 0) { [void]$summary.Add('- none') }
    foreach ($value in $script:Warnings) { [void]$summary.Add('- ' + $value) }
    [void]$summary.Add('')
    [void]$summary.Add('Errors:')
    if ($script:Errors.Count -eq 0) { [void]$summary.Add('- none') }
    foreach ($value in $script:Errors) { [void]$summary.Add('- ' + $value) }
    [System.IO.File]::WriteAllLines((Join-DtmSupportStagePath -RelativePath 'collection-summary.txt'), $summary.ToArray(), $script:Utf8NoBom)

    $sendText = @(
        'Send the complete outer ZIP to the DTMAPI maintainer.',
        'Do not edit or remove files inside it.',
        'This package contains complete save data and local filesystem paths.',
        ('CollectionStatus=' + $status)
    )
    [System.IO.File]::WriteAllLines((Join-DtmSupportStagePath -RelativePath 'SEND_THIS_ZIP.txt'), [string[]]$sendText, $script:Utf8NoBom)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $script:StageRoot,
        $partialZip,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)
    if (-not [System.IO.File]::Exists($partialZip) -or (New-Object System.IO.FileInfo($partialZip)).Length -le 0) {
        throw 'ZIP publication produced no usable file.'
    }
    [System.IO.File]::Move($partialZip, $finalZip)
    $published = $true

    Write-Host ''
    Write-Host ('CollectionStatus: ' + $status) -ForegroundColor $(if ($collectionComplete) { 'Green' } else { 'Yellow' })
    Write-Host 'Send this ZIP:' -ForegroundColor Cyan
    Write-Host $finalZip -ForegroundColor Cyan
    if (-not $collectionComplete) {
        Write-Host 'The ZIP contains collection-summary.txt with the missing or unstable source details.' -ForegroundColor Yellow
    }
}
catch {
    Write-Host ('[FATAL] Collector failed: ' + $_.Exception.GetType().Name + ': ' + $_.Exception.Message) -ForegroundColor Red
    if ([System.IO.Directory]::Exists($script:StageRoot)) {
        try {
            [System.IO.File]::WriteAllText((Join-Path $script:StageRoot 'COLLECTOR-FATAL-ERROR.txt'), ($_ | Out-String), $script:Utf8NoBom)
        }
        catch {
        }
        Write-Host ('Collector staging was retained for support: ' + $script:StageRoot) -ForegroundColor Yellow
    }
    if ([System.IO.File]::Exists($partialZip)) {
        try { [System.IO.File]::Delete($partialZip) } catch { }
    }
    exit 1
}
finally {
    if ($published -and [System.IO.Directory]::Exists($script:StageRoot)) {
        try {
            $resolvedWorkRoot = [System.IO.Path]::GetFullPath($workRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
            if (-not $script:StageRoot.StartsWith($resolvedWorkRoot, [StringComparison]::OrdinalIgnoreCase) -or
                -not ([System.IO.Path]::GetFileName($script:StageRoot)).StartsWith('.DTMAPI-player-support-', [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Refused to clean a staging directory outside the collector-owned work root.'
            }
            Remove-Item -LiteralPath $script:StageRoot -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-Host ('[WARN] Completed ZIP is valid, but temporary staging cleanup failed: ' + $_.Exception.Message) -ForegroundColor Yellow
        }
    }
}

if ($script:SaveRootsFound -gt 0 -and $script:SaveFilesCopied -gt 0 -and $script:SaveCopyFailures -eq 0 -and $script:Errors.Count -eq 0) {
    exit 0
}
exit 2
