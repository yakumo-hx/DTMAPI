# Shared game-session helpers. Definitions only; PowerShell 5.1 compatible.
function Test-DtmApiDolocTownGamePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    try {
        if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
            return $false
        }

        $required = @(
            (Join-Path $Path 'DolocTown.exe'),
            (Join-Path $Path 'DolocTown_Data')
        )

        foreach ($item in $required) {
            if (-not (Test-Path -LiteralPath $item)) {
                return $false
            }
        }
    }
    catch {
        return $false
    }

    return $true
}

function Assert-DtmApiDolocTownGamePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Source
    )

    if (-not (Test-DtmApiDolocTownGamePath -Path $Path)) {
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1002' `
            -Chinese '游戏目录无效；目标文件夹必须同时包含 DolocTown.exe 和 DolocTown_Data。' `
            -English 'The game directory is invalid; it must contain both DolocTown.exe and DolocTown_Data.' `
            -Detail "$Source`: $Path"
    }
}

function Resolve-DtmApiExplicitGamePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Source
    )

    try {
        $full = [System.IO.Path]::GetFullPath($Path)
        if (-not (Test-Path -LiteralPath $full -PathType Container)) {
            Throw-DtmApiInstallerError `
                -Code 'DTM-E1002' `
                -Chinese '指定的游戏目录不存在。' `
                -English 'The configured game directory does not exist.' `
                -Detail "$Source`: $Path"
        }
        $resolved = (Resolve-Path -LiteralPath $full).Path
        Assert-DtmApiDolocTownGamePath -Path $resolved -Source $Source
        return $resolved
    }
    catch {
        if ((Get-DtmApiInstallerFailureInfo -ErrorRecord $_).Code -eq 'DTM-E1002') {
            throw
        }
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1002' `
            -Chinese '指定的游戏路径包含非法字符或无法解析。' `
            -English 'The configured game path contains invalid characters or cannot be resolved.' `
            -Detail "$Source`: $Path; $($_.Exception.Message)" `
            -InnerException $_.Exception
    }
}

function Resolve-DtmApiSteamLibraryGamePath {
    param([Parameter(Mandatory = $true)] [string] $LibraryRoot)

    try {
        $libraryRootFull = [System.IO.Path]::GetFullPath($LibraryRoot)
    }
    catch {
        return $null
    }
    $manifest = Join-Path $libraryRootFull 'steamapps\appmanifest_2285550.acf'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
        return $null
    }

    $installDirectoryName = 'Doloc Town'
    try {
        $manifestText = Get-Content -Raw -LiteralPath $manifest
        $installDirMatch = [regex]::Match($manifestText, '"installdir"\s+"([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($installDirMatch.Success -and -not [string]::IsNullOrWhiteSpace($installDirMatch.Groups[1].Value)) {
            $installDirectoryName = $installDirMatch.Groups[1].Value.Replace('\\', '\')
        }
    }
    catch {
        Write-Verbose "Could not read Steam appmanifest installdir from ${manifest}: $($_.Exception.Message)"
    }

    $candidate = Join-Path $libraryRootFull (Join-Path 'steamapps\common' $installDirectoryName)
    if (Test-DtmApiDolocTownGamePath -Path $candidate) {
        return (Resolve-Path -LiteralPath $candidate).Path
    }
    return $null
}

function Get-DtmApiWorkshopLibraryRoot {
    param([Parameter(Mandatory = $true)] [string] $RepoRoot)

    $repoRootFull = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/')
    $match = [regex]::Match(
        $repoRootFull,
        '^(?<library>.+)[\\/]steamapps[\\/]workshop[\\/]content[\\/]2285550[\\/][^\\/]+[\\/]Content$',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        return ''
    }
    return [System.IO.Path]::GetFullPath($match.Groups['library'].Value)
}

function Resolve-DolocTownGamePath {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $AllowMissing
    )

    if ($env:DTMAPI_GAME_DIR) {
        return Resolve-DtmApiExplicitGamePath -Path $env:DTMAPI_GAME_DIR -Source 'DTMAPI_GAME_DIR'
    }

    $settings = Read-LocalSettings -RepoRoot $RepoRoot
    $settingsGameDir = if ($settings) { Get-DtmApiObjectProperty -Object $settings -Name 'GameDir' -Default '' } else { '' }
    if ($settingsGameDir) {
        return Resolve-DtmApiExplicitGamePath -Path ([string]$settingsGameDir) -Source 'local.settings.json GameDir'
    }

    $repoRootFull = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/')
    foreach ($colocatedCandidate in @($repoRootFull, (Split-Path -Parent $repoRootFull))) {
        if (-not [string]::IsNullOrWhiteSpace($colocatedCandidate) -and
            (Test-DtmApiDolocTownGamePath -Path $colocatedCandidate)) {
            return (Resolve-Path -LiteralPath $colocatedCandidate).Path
        }
    }

    $workshopLibraryRoot = Get-DtmApiWorkshopLibraryRoot -RepoRoot $RepoRoot
    if (-not [string]::IsNullOrWhiteSpace($workshopLibraryRoot)) {
        $workshopLibraryGame = Resolve-DtmApiSteamLibraryGamePath -LibraryRoot $workshopLibraryRoot
        if (-not [string]::IsNullOrWhiteSpace($workshopLibraryGame)) {
            return $workshopLibraryGame
        }
    }

    if ($AllowMissing) {
        return $null
    }
    Throw-DtmApiInstallerError `
        -Code 'DTM-E1002' `
        -Chinese '无法定位《多洛可小镇》目录。请把安装包完整复制到 DolocTown.exe 所在文件夹，或设置 DTMAPI_GAME_DIR/local.settings.json。' `
        -English 'Doloc Town could not be located. Copy the complete installer package beside DolocTown.exe, or set DTMAPI_GAME_DIR/local.settings.json.' `
        -Detail 'Only an explicit path, a colocated package, or the current Workshop package library appmanifest is considered.'
}

function Resolve-DtmApiStateDir {
    param(
        [string] $GameDir
    )

    if ($env:DTMAPI_RUNTIME_DIR) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_RUNTIME_DIR)
    }

    if ($env:DTMAPI_STATE_DIR) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_STATE_DIR)
    }

    return Join-Path $GameDir 'DTMAPI'
}

function Get-DtmApiCurrentSaveArchiveFamilySnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $SaveRoot,
        [Parameter(Mandatory = $true)] [ValidateRange(0, 999)] [int] $ArchiveIndex,
        [Parameter(Mandatory = $false)] [string] $ArchiveFileNameFormat = 'doloc-save-{0}.data'
    )

    $root = [System.IO.Path]::GetFullPath($SaveRoot)
    if ([string]::IsNullOrWhiteSpace($ArchiveFileNameFormat)) {
        throw 'The current save archive filename format is empty.'
    }
    try {
        $currentName = [string]::Format(
            [System.Globalization.CultureInfo]::InvariantCulture,
            $ArchiveFileNameFormat,
            [object]$ArchiveIndex)
    }
    catch {
        throw "The current save archive filename format is invalid: $ArchiveFileNameFormat"
    }
    if ([string]::IsNullOrWhiteSpace($currentName) -or
        [System.IO.Path]::IsPathRooted($currentName) -or
        -not [string]::Equals($currentName, [System.IO.Path]::GetFileName($currentName), [System.StringComparison]::Ordinal) -or
        [string]::Equals($currentName, $ArchiveFileNameFormat, [System.StringComparison]::Ordinal)) {
        throw "The current save archive filename format did not produce one safe indexed filename: $ArchiveFileNameFormat"
    }
    $currentPath = Join-Path $root $currentName
    if (-not (Test-Path -LiteralPath $currentPath -PathType Leaf)) {
        throw "The selected current save archive is missing; legacy backup names cannot satisfy NoNativeSave proof: $currentPath"
    }
    $backupPattern = '^' + [Text.RegularExpressions.Regex]::Escape($currentName) + '\.(?:prev[0-9]+|bak)$'
    $paths = New-Object 'System.Collections.Generic.List[string]'
    $paths.Add($currentPath) | Out-Null
    if (Test-Path -LiteralPath $root -PathType Container) {
        foreach ($item in @(Get-ChildItem -LiteralPath $root -File -ErrorAction Stop | Where-Object {
            [Text.RegularExpressions.Regex]::IsMatch(
                $_.Name,
                $backupPattern,
                [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        } | Sort-Object Name)) {
            $paths.Add($item.FullName) | Out-Null
        }
    }

    $files = @($paths.ToArray() | ForEach-Object {
        $path = [System.IO.Path]::GetFullPath($_)
        $name = [System.IO.Path]::GetFileName($path)
        $exists = Test-Path -LiteralPath $path -PathType Leaf
        $item = if ($exists) { Get-Item -LiteralPath $path -ErrorAction Stop } else { $null }
        [pscustomobject]@{
            Name = $name
            Kind = if ([string]::Equals($name, $currentName, [System.StringComparison]::OrdinalIgnoreCase)) { 'CurrentArchive' } else { 'CurrentArchiveBackup' }
            Path = $path
            Existed = [bool]$exists
            Length = if ($exists) { [int64]$item.Length } else { [int64]0 }
            Sha256 = if ($exists) { Get-DtmApiFileSha256 -Path $path } else { '' }
            LastWriteTimeUtc = if ($exists) { $item.LastWriteTimeUtc.ToString('o') } else { '' }
            LastWriteTimeUtcTicks = if ($exists) { [int64]$item.LastWriteTimeUtc.Ticks } else { [int64]0 }
        }
    })

    return [pscustomobject]@{
        SaveRoot = $root
        ArchiveIndex = $ArchiveIndex
        ArchiveFileNameFormat = $ArchiveFileNameFormat
        CurrentName = $currentName
        BackupNamePattern = $backupPattern
        Files = $files
    }
}

function Compare-DtmApiCurrentSaveArchiveFamilySnapshot {
    param(
        [Parameter(Mandatory = $true)] $Baseline
    )

    $actual = Get-DtmApiCurrentSaveArchiveFamilySnapshot `
        -SaveRoot ([string]$Baseline.SaveRoot) `
        -ArchiveIndex ([int]$Baseline.ArchiveIndex) `
        -ArchiveFileNameFormat ([string]$Baseline.ArchiveFileNameFormat)
    $beforeByName = @{}
    $afterByName = @{}
    foreach ($file in @($Baseline.Files)) { $beforeByName[[string]$file.Name] = $file }
    foreach ($file in @($actual.Files)) { $afterByName[[string]$file.Name] = $file }
    $names = @(@($beforeByName.Keys) + @($afterByName.Keys) | Sort-Object -Unique)
    $checks = New-Object 'System.Collections.Generic.List[object]'
    $passed = $true
    foreach ($name in $names) {
        $before = if ($beforeByName.ContainsKey($name)) { $beforeByName[$name] } else { $null }
        $after = if ($afterByName.ContainsKey($name)) { $afterByName[$name] } else { $null }
        $beforeExists = $null -ne $before -and [bool]$before.Existed
        $afterExists = $null -ne $after -and [bool]$after.Existed
        $unchanged = $null -ne $before -and $null -ne $after -and
            $beforeExists -eq $afterExists -and
            [int64]$before.Length -eq [int64]$after.Length -and
            [string]::Equals([string]$before.Sha256, [string]$after.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -and
            [int64]$before.LastWriteTimeUtcTicks -eq [int64]$after.LastWriteTimeUtcTicks
        $changeKind = if ($unchanged) {
            'Unchanged'
        }
        elseif (-not $beforeExists -and $afterExists) {
            'Added'
        }
        elseif ($beforeExists -and -not $afterExists) {
            'Deleted'
        }
        else {
            'Modified'
        }
        $passed = $passed -and $unchanged
        $checks.Add([pscustomobject]@{
            Name = $name
            Kind = if ($null -ne $after) { [string]$after.Kind } elseif ($null -ne $before) { [string]$before.Kind } else { '' }
            Path = if ($null -ne $after) { [string]$after.Path } elseif ($null -ne $before) { [string]$before.Path } else { '' }
            ExistedBefore = $beforeExists
            ExistsBeforeCleanup = $afterExists
            LengthBefore = if ($null -ne $before) { [int64]$before.Length } else { [int64]0 }
            LengthBeforeCleanup = if ($null -ne $after) { [int64]$after.Length } else { [int64]0 }
            Sha256Before = if ($null -ne $before) { [string]$before.Sha256 } else { '' }
            Sha256BeforeCleanup = if ($null -ne $after) { [string]$after.Sha256 } else { '' }
            LastWriteTimeUtcTicksBefore = if ($null -ne $before) { [int64]$before.LastWriteTimeUtcTicks } else { [int64]0 }
            LastWriteTimeUtcTicksBeforeCleanup = if ($null -ne $after) { [int64]$after.LastWriteTimeUtcTicks } else { [int64]0 }
            ChangeKind = $changeKind
            UnchangedBeforeCleanup = $unchanged
        }) | Out-Null
    }

    return [pscustomobject]@{
        SaveRoot = [string]$Baseline.SaveRoot
        ArchiveIndex = [int]$Baseline.ArchiveIndex
        ArchiveFileNameFormat = [string]$Baseline.ArchiveFileNameFormat
        CurrentName = [string]$Baseline.CurrentName
        BackupNamePattern = [string]$Baseline.BackupNamePattern
        Files = @($checks.ToArray())
        Passed = [bool]$passed
    }
}

function Get-DtmApiMoreSavesLegacyArchiveCandidates {
    param(
        [Parameter(Mandatory = $true)] [string] $SaveRoot
    )

    $root = [System.IO.Path]::GetFullPath($SaveRoot)
    $candidates = New-Object 'System.Collections.Generic.List[object]'
    foreach ($archiveIndex in 6..11) {
        foreach ($name in @(
            "ea-playtest-doloc-archive-$archiveIndex.data",
            "ea-playtest-doloc-archive-$archiveIndex-prev.data",
            "ea-playtest-doloc-archive-$archiveIndex-bak.data"
        )) {
            $path = [System.IO.Path]::GetFullPath((Join-Path $root $name))
            if (-not [System.IO.File]::Exists($path)) {
                continue
            }
            $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
            $candidates.Add([pscustomobject]@{
                ArchiveIndex = $archiveIndex
                Name = $name
                Path = $path
                Length = [int64]$item.Length
                ReparsePoint = [bool](($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)
            }) | Out-Null
        }
    }

    return @($candidates.ToArray())
}

function Get-DtmApiEnabledModInfoIds {
    param(
        [Parameter(Mandatory = $true)] [string] $ModInfoPath
    )

    $path = [System.IO.Path]::GetFullPath($ModInfoPath)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "The native mod enablement file is unavailable for pre-launch mutation classification: $path"
    }
    try {
        $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $path | ConvertFrom-Json
    }
    catch {
        throw "The native mod enablement file is invalid JSON and cannot classify startup mutation: $($_.Exception.Message)"
    }
    if ($null -eq $data -or
        -not $data.PSObject.Properties['modInfos'] -or
        $null -eq $data.modInfos -or
        $data.modInfos -is [System.Array]) {
        throw 'The native mod enablement file must contain one modInfos object before startup mutation can be classified.'
    }

    return @($data.modInfos.PSObject.Properties | Where-Object {
        $null -ne $_.Value -and
        $_.Value.PSObject.Properties['enabled'] -and
        [bool]$_.Value.enabled
    } | ForEach-Object { [string]$_.Name } | Sort-Object -Unique)
}

function Test-DtmApiGameProcessRunningForDir {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    foreach ($process in @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
        try {
            $processPath = [string]$process.Path
            if (-not [string]::IsNullOrWhiteSpace($processPath) -and (Test-DtmApiPathIsSameOrChild -Child $processPath -Parent $GameDir)) {
                return $true
            }
        }
        catch {
        }
    }

    return $false
}

function Assert-DtmApiGameNotRunning {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $Operation
    )

    if (Test-DtmApiGameProcessRunningForDir -GameDir $GameDir) {
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1001' `
            -Chinese '检测到《多洛可小镇》仍在运行。请完全退出游戏；如果 Steam 仍显示“运行中”，也请退出 Steam 后重试。' `
            -English 'Doloc Town is still running. Fully close the game; if Steam still shows it as running, exit Steam too, then retry.' `
            -Detail "Operation=$Operation; GameDir=$GameDir"
    }
}

function Assert-DtmApiGameDirectoryMutationRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    $gameFull = [System.IO.Path]::GetFullPath($GameDir).TrimEnd('\', '/')
    $driveRoot = [System.IO.Path]::GetPathRoot($gameFull).TrimEnd('\', '/')
    if ([string]::Equals($gameFull, $driveRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to mutate a drive root as a Doloc Town game directory: $gameFull"
    }
    Assert-DtmApiDolocTownGamePath -Path $gameFull -Source 'installer mutation target'
}

function Copy-DtmRuntimeEvidenceWindow {
    param(
        [Parameter(Mandatory = $true)] [string] $SourceRoot,
        [Parameter(Mandatory = $true)] [string] $DestinationRoot,
        [Parameter(Mandatory = $true)] [datetime] $SinceUtc,
        [Parameter(Mandatory = $true)] [string] $SummaryPath,
        [long] $MaxTotalBytes = 512MB,
        [long] $MaxFileBytes = 128MB,
        [int] $MaxFiles = 2000,
        [int] $MaxDirectories = 128
    )

    if ($MaxTotalBytes -le 0 -or $MaxFileBytes -le 0 -or $MaxFiles -le 0 -or $MaxDirectories -le 0) {
        throw 'Runtime evidence limits must all be positive.'
    }

    if (Test-DtmApiPathIsSameOrChild -Child $DestinationRoot -Parent $SourceRoot) {
        throw "Refusing to copy runtime evidence because destination is inside source. Source: $SourceRoot. Destination: $DestinationRoot"
    }

    if (Test-DtmApiPathIsSameOrChild -Child $SourceRoot -Parent $DestinationRoot) {
        throw "Refusing to copy runtime evidence because source is inside destination. Source: $SourceRoot. Destination: $DestinationRoot"
    }

    $summary = New-Object 'System.Collections.Generic.List[string]'
    $since = $SinceUtc.ToUniversalTime()
    $summary.Add('Mode=current-run-window')
    $summary.Add("SourceRoot=$SourceRoot")
    $summary.Add("DestinationRoot=$DestinationRoot")
    $summary.Add("SinceUtc=$($since.ToString('o'))")
    $summary.Add("MaxTotalBytes=$MaxTotalBytes")
    $summary.Add("MaxFileBytes=$MaxFileBytes")
    $summary.Add("MaxFiles=$MaxFiles")
    $summary.Add("MaxDirectories=$MaxDirectories")

    if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) {
        $summary.Add('Status=source-missing')
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $SummaryPath) | Out-Null
        $summary | Set-Content -LiteralPath $SummaryPath
        return [pscustomobject]@{
            Status = 'source-missing'
            DirectoryCount = 0
            FileCount = 0
            TotalBytes = [long]0
        }
    }

    $candidateDirectories = New-Object 'System.Collections.Generic.List[System.IO.DirectoryInfo]'
    foreach ($caseDirectory in @(Get-ChildItem -LiteralPath $SourceRoot -Directory -Force -ErrorAction SilentlyContinue | Sort-Object Name)) {
        foreach ($runDirectory in @(Get-ChildItem -LiteralPath $caseDirectory.FullName -Directory -Force -ErrorAction SilentlyContinue | Sort-Object Name)) {
            if ($runDirectory.CreationTimeUtc -ge $since -or $runDirectory.LastWriteTimeUtc -ge $since) {
                $candidateDirectories.Add($runDirectory)
            }
        }
    }

    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $totalBytes = [long]0
    $oversizedFiles = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    if ($candidateDirectories.Count -le $MaxDirectories) {
        foreach ($directory in $candidateDirectories) {
            foreach ($file in @(Get-ChildItem -LiteralPath $directory.FullName -File -Recurse -Force -ErrorAction SilentlyContinue)) {
                $files.Add($file)
                $totalBytes += [long]$file.Length
                if ($file.Length -gt $MaxFileBytes) {
                    $oversizedFiles.Add($file)
                }
            }
        }
    }

    $summary.Add("SelectedDirectories=$($candidateDirectories.Count)")
    $summary.Add("SelectedFiles=$($files.Count)")
    $summary.Add("SelectedBytes=$totalBytes")
    foreach ($directory in $candidateDirectories) {
        $summary.Add("SelectedDirectory=$($directory.Parent.Name)/$($directory.Name)")
    }
    foreach ($file in $oversizedFiles) {
        $summary.Add("OversizedFile=$($file.FullName);bytes=$($file.Length)")
    }

    $blockReasons = New-Object 'System.Collections.Generic.List[string]'
    if ($candidateDirectories.Count -gt $MaxDirectories) {
        $blockReasons.Add("directory-count $($candidateDirectories.Count) exceeds $MaxDirectories")
    }
    if ($files.Count -gt $MaxFiles) {
        $blockReasons.Add("file-count $($files.Count) exceeds $MaxFiles")
    }
    if ($totalBytes -gt $MaxTotalBytes) {
        $blockReasons.Add("total-bytes $totalBytes exceeds $MaxTotalBytes")
    }
    if ($oversizedFiles.Count -gt 0) {
        $blockReasons.Add("oversized-files $($oversizedFiles.Count) exceed per-file limit $MaxFileBytes")
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $SummaryPath) | Out-Null
    if ($blockReasons.Count -gt 0) {
        $summary.Add('Status=blocked-by-limit')
        foreach ($reason in $blockReasons) {
            $summary.Add("BlockReason=$reason")
        }
        $summary | Set-Content -LiteralPath $SummaryPath
        return [pscustomobject]@{
            Status = 'blocked-by-limit'
            DirectoryCount = $candidateDirectories.Count
            FileCount = $files.Count
            TotalBytes = $totalBytes
        }
    }

    foreach ($directory in $candidateDirectories) {
        $caseDestination = Join-Path $DestinationRoot $directory.Parent.Name
        $runDestination = Join-Path $caseDestination $directory.Name
        New-Item -ItemType Directory -Force -Path $runDestination | Out-Null
        foreach ($item in @(Get-ChildItem -LiteralPath $directory.FullName -Force -ErrorAction SilentlyContinue)) {
            Copy-Item -LiteralPath $item.FullName -Destination $runDestination -Recurse -Force
        }
    }

    $status = if ($candidateDirectories.Count -eq 0) { 'no-current-run-evidence' } else { 'copied' }
    $summary.Add("Status=$status")
    $summary | Set-Content -LiteralPath $SummaryPath
    return [pscustomobject]@{
        Status = $status
        DirectoryCount = $candidateDirectories.Count
        FileCount = $files.Count
        TotalBytes = $totalBytes
    }
}

function Wait-ForLogLine {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds = 120,
        [switch] $Regex,
        [switch] $AbortOnFatalInstanceWindow
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $processObserved = $false
    while ((Get-Date) -lt $deadline) {
        $text = ''
        if (Test-Path $LogPath) {
            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            $matched = if ($Regex) {
                $text -match $Pattern
            }
            else {
                $text -match [regex]::Escape($Pattern)
            }
            if ($matched) {
                return $true
            }
            if ($text -match 'QA host lifecycle .*state=failed' -or
                $text -match 'QA scenario .* FAILED' -or
                $text -match 'InternalFixture\.QaHost = failed-closed') {
                return $false
            }
        }
        if ($text -match 'DTMAPI runtime starting\.' -or
            $text -match 'GameLaunched dispatched\.' -or
            $text -match 'QA host lifecycle .*state=(validated|activated|attached|started|updated)') {
            $processObserved = $true
        }
        if ($AbortOnFatalInstanceWindow -and (Test-FatalInstanceWindow)) {
            return $false
        }
        # Once the requested run has been observed, process exit is a terminal
        # negative result for a still-missing log receipt. This avoids masking a
        # fail-closed QA exit behind the full scenario timeout.
        $process = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($process) {
            $processObserved = $true
        }
        elseif ($processObserved -or $text -match 'Smoke automation requesting game quit:') {
            return $false
        }
        Start-Sleep -Seconds 1
    }
    return $false
}

function Wait-ForLogLineAfterOffset {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [long] $Offset = 0,
        [int] $TimeoutSeconds = 120,
        [switch] $AbortOnFatalInstanceWindow
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $processObserved = $false
    while ((Get-Date) -lt $deadline) {
        $text = ''
        if (Test-Path $LogPath) {
            $stream = $null
            $reader = $null
            try {
                $stream = [System.IO.File]::Open($LogPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                $start = [Math]::Min([Math]::Max(0, $Offset), $stream.Length)
                [void]$stream.Seek($start, [System.IO.SeekOrigin]::Begin)
                $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8, $true)
                $text = $reader.ReadToEnd()
                if ($text -match [regex]::Escape($Pattern)) {
                    return $true
                }
                if ($text -match 'QA host lifecycle .*state=failed' -or
                    $text -match 'QA scenario .* FAILED' -or
                    $text -match 'InternalFixture\.QaHost = failed-closed') {
                    return $false
                }
            }
            catch {
            }
            finally {
                if ($reader) {
                    $reader.Dispose()
                }
                elseif ($stream) {
                    $stream.Dispose()
                }
            }
        }
        if ($text -match 'DTMAPI runtime starting\.' -or
            $text -match 'GameLaunched dispatched\.' -or
            $text -match 'QA host lifecycle .*state=(validated|activated|attached|started|updated)') {
            $processObserved = $true
        }
        if ($AbortOnFatalInstanceWindow -and (Test-FatalInstanceWindow)) {
            return $false
        }
        $process = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($process) {
            $processObserved = $true
        }
        elseif ($processObserved -or $text -match 'Smoke automation requesting game quit:') {
            return $false
        }
        Start-Sleep -Seconds 1
    }
    return $false
}

function New-EvidenceDir {
    param(
        [string] $RepoRoot,
        [string] $CaseId
    )
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $dir = Join-Path $RepoRoot "docs\debug\evidence\$CaseId\$stamp"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    return $dir
}

function Write-ProcessCheck {
    param(
        [string] $Path
    )
    $processes = Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Select-Object Id, ProcessName, Path, StartTime | Format-List | Out-String | Set-Content -LiteralPath $Path
    }
    else {
        'No DolocTown.exe process found.' | Set-Content -LiteralPath $Path
    }
}

function Ensure-WindowInspector {
    if ('DtmapiSmoke.NativeWindow' -as [type]) {
        return
    }

    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DtmapiSmoke
{
    public static class NativeWindow
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        public static IntPtr[] GetTopLevelWindows()
        {
            var handles = new List<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                handles.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            return handles.ToArray();
        }

        public static IntPtr[] GetChildWindows(IntPtr parent)
        {
            var handles = new List<IntPtr>();
            EnumChildWindows(parent, (hWnd, lParam) =>
            {
                handles.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            return handles.ToArray();
        }

        public static string GetText(IntPtr hWnd)
        {
            var builder = new StringBuilder(1024);
            GetWindowText(hWnd, builder, builder.Capacity);
            return builder.ToString();
        }
    }
}
'@
}

function Get-FatalInstanceWindows {
    Ensure-WindowInspector
    $type = 'DtmapiSmoke.NativeWindow' -as [type]
    $matches = @()

    foreach ($handle in $type::GetTopLevelWindows()) {
        if (-not $type::IsWindowVisible($handle)) {
            continue
        }

        $title = $type::GetText($handle)
        $childTexts = New-Object System.Collections.Generic.List[string]
        foreach ($child in $type::GetChildWindows($handle)) {
            $text = $type::GetText($child)
            if ($text) {
                $childTexts.Add($text)
            }
        }

        $combined = "$title`n$($childTexts -join "`n")"
        if ($title -like 'Fatal error*' -or
            $combined -like '*Fatal error in GC*' -or
            $combined -like '*Unexpected mark stack overflow*' -or
            $combined -like '*Another instance is already running*') {
            $matches += [pscustomobject]@{
                Hwnd = ('0x{0:X}' -f $handle.ToInt64())
                Title = $title
                Text = $combined.Trim()
            }
        }
    }

    foreach ($proc in Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue) {
        $title = [string]$proc.MainWindowTitle
        if ($title -like 'Fatal error*' -or
            $title -like '*Fatal error in GC*' -or
            $title -like '*Unexpected mark stack overflow*') {
            $matches += [pscustomobject]@{
                Hwnd = if ($proc.MainWindowHandle -ne 0) { ('0x{0:X}' -f $proc.MainWindowHandle.ToInt64()) } else { 'process:' + $proc.Id }
                Title = $title
                Text = "ProcessId=$($proc.Id); MainWindowTitle=$title"
            }
        }
    }

    return $matches
}

function Test-FatalInstanceWindow {
    return [bool]@(Get-FatalInstanceWindows)
}

function Write-FatalWindowCheck {
    param(
        [string] $Path
    )

    $windows = @(Get-FatalInstanceWindows)
    if ($windows.Count -eq 0) {
        if ($Path) {
            'No fatal instance popup found.' | Set-Content -LiteralPath $Path
        }
        else {
            'No fatal instance popup found.'
        }
        return
    }

    $text = $windows | Format-List | Out-String
    if ($Path) {
        $text | Set-Content -LiteralPath $Path
    }
    else {
        $text
    }
}
