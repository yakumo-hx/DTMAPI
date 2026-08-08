$script:DtmApiRuntimeFiles = @(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)

function Write-DtmApiPlayerHelp {
    param([switch] $PackageIncomplete)

    if ($PackageIncomplete) {
        Write-Host '[HELP] DTMAPI package files are missing. Resubscribe to DTMAPI and try again.' -ForegroundColor Yellow
    }
    Write-Host '[HELP] If the problem continues, copy the whole DTMAPI folder to a path containing only English letters and numbers, then try again.' -ForegroundColor Yellow
    Write-Host '[HELP] Run 3_check_dtmapi_status.bat or send a screenshot of the whole window.' -ForegroundColor Yellow
}

function Get-DtmApiPackageRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
}

function Get-DtmApiFullPath {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $full = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetPathRoot($full)
    if ([string]::Equals($full, $root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $root
    }
    return $full.TrimEnd('\', '/')
}

function Test-DtmApiSameOrChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    $childFull = Get-DtmApiFullPath -Path $Child
    $parentFull = Get-DtmApiFullPath -Path $Parent
    if ([string]::Equals($childFull, $parentFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }
    $prefix = $parentFull + [System.IO.Path]::DirectorySeparatorChar
    return $childFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-DtmApiGameDirectory {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return $false
    }
    return ((Test-Path -LiteralPath (Join-Path $Path 'DolocTown.exe') -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $Path 'DolocTown_Data') -PathType Container))
}

function Assert-DtmApiGameDirectory {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $full = Get-DtmApiFullPath -Path $Path
    $driveRoot = [System.IO.Path]::GetPathRoot($full)
    if ([string]::Equals($full.TrimEnd('\', '/'), $driveRoot.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "A drive root can never be used as the Doloc Town game directory: $full"
    }
    if (-not (Test-DtmApiGameDirectory -Path $full)) {
        throw "Doloc Town was not found at '$full'. Expected DolocTown.exe and DolocTown_Data."
    }
    return $full
}

function Resolve-DtmApiSteamLibraryGame {
    param([Parameter(Mandatory = $true)] [string] $LibraryRoot)

    $library = Get-DtmApiFullPath -Path $LibraryRoot
    $manifest = Join-Path $library 'steamapps\appmanifest_2285550.acf'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
        return ''
    }
    $installName = 'Doloc Town'
    try {
        $text = Get-Content -LiteralPath $manifest -Raw -ErrorAction Stop
        $match = [regex]::Match($text, '"installdir"\s+"([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($match.Success -and -not [string]::IsNullOrWhiteSpace($match.Groups[1].Value)) {
            $installName = $match.Groups[1].Value.Replace('\\', '\')
        }
    }
    catch {
    }
    $candidate = Join-Path $library (Join-Path 'steamapps\common' $installName)
    if (Test-DtmApiGameDirectory -Path $candidate) {
        return (Get-DtmApiFullPath -Path $candidate)
    }
    return ''
}

function Resolve-DtmApiGameDirectory {
    param([string] $PackageRoot = (Get-DtmApiPackageRoot))

    if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_GAME_DIR)) {
        return (Assert-DtmApiGameDirectory -Path $env:DTMAPI_GAME_DIR)
    }

    $packageFull = Get-DtmApiFullPath -Path $PackageRoot
    $workshopMatch = [regex]::Match(
        $packageFull,
        '^(?<library>.+)[\\/]steamapps[\\/]workshop[\\/]content[\\/]2285550[\\/][^\\/]+$',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($workshopMatch.Success) {
        $workshopGame = Resolve-DtmApiSteamLibraryGame -LibraryRoot $workshopMatch.Groups['library'].Value
        if (-not [string]::IsNullOrWhiteSpace($workshopGame)) {
            return $workshopGame
        }
    }

    $steamRoots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($registryPath in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $record = Get-ItemProperty -Path $registryPath -ErrorAction Stop
            $steamPath = ''
            if ($record.PSObject.Properties['SteamPath']) {
                $steamPath = [string]$record.SteamPath
            }
            elseif ($record.PSObject.Properties['InstallPath']) {
                $steamPath = [string]$record.InstallPath
            }
            if (-not [string]::IsNullOrWhiteSpace($steamPath) -and (Test-Path -LiteralPath $steamPath -PathType Container)) {
                $steamRoots.Add((Get-DtmApiFullPath -Path $steamPath))
            }
        }
        catch {
        }
    }

    foreach ($steamRoot in $steamRoots) {
        $libraries = New-Object 'System.Collections.Generic.List[string]'
        $libraries.Add($steamRoot)
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $libraryFile -PathType Leaf) {
            try {
                $libraryText = Get-Content -LiteralPath $libraryFile -Raw -ErrorAction Stop
                foreach ($match in [regex]::Matches($libraryText, '"path"\s+"([^"]+)"')) {
                    $path = $match.Groups[1].Value.Replace('\\', '\')
                    if (Test-Path -LiteralPath $path -PathType Container) {
                        $libraries.Add((Get-DtmApiFullPath -Path $path))
                    }
                }
            }
            catch {
            }
        }
        foreach ($library in $libraries) {
            $game = Resolve-DtmApiSteamLibraryGame -LibraryRoot $library
            if (-not [string]::IsNullOrWhiteSpace($game)) {
                return $game
            }
        }
    }

    throw 'Doloc Town could not be located. Start Steam once and verify the game is installed.'
}

function Assert-DtmApiGameNotRunning {
    $running = @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)
    if ($running.Count -gt 0) {
        throw 'Doloc Town is running. Close the game before installing, uninstalling or collecting logs.'
    }
}

function Enter-DtmApiInstallerLock {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $lockPath = Join-Path $GameDir '.dtmapi-installer.lock'
    try {
        $stream = [System.IO.FileStream]::new(
            $lockPath,
            [System.IO.FileMode]::OpenOrCreate,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
        $text = "pid=$PID`r`nstarted=$([DateTime]::UtcNow.ToString('o'))`r`n"
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        $stream.SetLength(0)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush()
        return [pscustomobject]@{ Path = $lockPath; Stream = $stream }
    }
    catch {
        throw "Another DTMAPI install or uninstall is already using this game directory. Close the other installer and try again. $($_.Exception.Message)"
    }
}

function Exit-DtmApiInstallerLock {
    param($Lock)

    if ($null -eq $Lock) {
        return
    }
    try {
        if ($null -ne $Lock.Stream) {
            $Lock.Stream.Dispose()
        }
    }
    finally {
        try {
            if (Test-Path -LiteralPath $Lock.Path -PathType Leaf) {
                Remove-Item -LiteralPath $Lock.Path -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
        }
    }
}

function Get-DtmApiExactChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $RelativePath
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath) -or [System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "Unsafe installer relative path: '$RelativePath'"
    }
    $segments = @($RelativePath -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($segments.Count -eq 0 -or @($segments | Where-Object { $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        throw "Unsafe installer relative path: '$RelativePath'"
    }
    $gameFull = Get-DtmApiFullPath -Path $GameDir
    $target = Get-DtmApiFullPath -Path (Join-Path $gameFull $RelativePath)
    $expected = Get-DtmApiFullPath -Path (Join-Path $gameFull ([string]::Join('\', $segments)))
    if (-not [string]::Equals($target, $expected, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-DtmApiSameOrChildPath -Child $target -Parent $gameFull) -or
        [string]::Equals($target, $gameFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Installer path escaped the game directory: '$RelativePath' -> '$target'"
    }
    return $target
}

function Assert-DtmApiNoReparsePointBelowGame {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $RelativePath
    )

    $current = Get-DtmApiFullPath -Path $GameDir
    foreach ($segment in @($RelativePath -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) {
            continue
        }
        $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Installer refuses to modify a reparse point below the game directory: $current"
        }
    }
}

function Remove-DtmApiExactChild {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $RelativePath
    )

    $target = Get-DtmApiExactChildPath -GameDir $GameDir -RelativePath $RelativePath
    Assert-DtmApiNoReparsePointBelowGame -GameDir $GameDir -RelativePath $RelativePath
    if (-not (Test-Path -LiteralPath $target)) {
        return $false
    }
    $item = Get-Item -LiteralPath $target -Force -ErrorAction Stop
    if ($item.PSIsContainer) {
        $pending = New-Object 'System.Collections.Generic.Queue[string]'
        $pending.Enqueue($target)
        while ($pending.Count -gt 0) {
            $directory = $pending.Dequeue()
            foreach ($child in @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop)) {
                if (($child.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw "Installer refuses to recursively remove a tree containing a reparse point: $($child.FullName)"
                }
                if ($child.PSIsContainer) {
                    $pending.Enqueue($child.FullName)
                }
            }
        }
        Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction Stop
    }
    else {
        Remove-Item -LiteralPath $target -Force -ErrorAction Stop
    }
    return $true
}

function Copy-DtmApiTree {
    param(
        [Parameter(Mandatory = $true)] [string] $SourceRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $DestinationRelativePath
    )

    if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) {
        throw "Package directory is missing: $SourceRoot"
    }
    $sourceFull = Get-DtmApiFullPath -Path $SourceRoot
    $destination = Get-DtmApiExactChildPath -GameDir $GameDir -RelativePath $DestinationRelativePath
    Assert-DtmApiNoReparsePointBelowGame -GameDir $GameDir -RelativePath $DestinationRelativePath
    [System.IO.Directory]::CreateDirectory($destination) | Out-Null
    foreach ($file in @(Get-ChildItem -LiteralPath $sourceFull -Recurse -File -Force -ErrorAction Stop)) {
        if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Package contains an unsupported reparse point: $($file.FullName)"
        }
        $relative = $file.FullName.Substring($sourceFull.Length).TrimStart('\', '/')
        $target = [System.IO.Path]::GetFullPath((Join-Path $destination $relative))
        if (-not (Test-DtmApiSameOrChildPath -Child $target -Parent $destination)) {
            throw "Package file escaped its destination: $($file.FullName)"
        }
        $targetRelative = $target.Substring((Get-DtmApiFullPath -Path $GameDir).Length).TrimStart('\', '/')
        Assert-DtmApiNoReparsePointBelowGame -GameDir $GameDir -RelativePath $targetRelative
        $parent = Split-Path -Parent $target
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
        [System.IO.File]::Copy($file.FullName, $target, $true)
    }
}

function Get-DtmApiBepInExRequiredPaths {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    return @(
        (Join-Path $GameDir 'winhttp.dll'),
        (Join-Path $GameDir 'doorstop_config.ini'),
        (Join-Path $GameDir 'BepInEx\core\BepInEx.dll'),
        (Join-Path $GameDir 'BepInEx\core\BepInEx.Preloader.dll'),
        (Join-Path $GameDir 'BepInEx\core\0Harmony.dll')
    )
}

function Test-DtmApiBepInExComplete {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    foreach ($path in Get-DtmApiBepInExRequiredPaths -GameDir $GameDir) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            return $false
        }
    }
    return $true
}

function Expand-DtmApiBundledBepInEx {
    param(
        [Parameter(Mandatory = $true)] [string] $ZipPath,
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    if (-not (Test-Path -LiteralPath $ZipPath -PathType Leaf)) {
        throw "Bundled BepInEx archive is missing: $ZipPath"
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    $gameFull = Get-DtmApiFullPath -Path $GameDir
    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        foreach ($entry in $archive.Entries) {
            $relative = $entry.FullName.Replace('/', '\').TrimStart('\')
            if ([string]::IsNullOrWhiteSpace($relative)) {
                continue
            }
            $allowedRootFiles = @('.doorstop_version', 'changelog.txt', 'doorstop_config.ini', 'winhttp.dll')
            $allowed = ($allowedRootFiles -contains $relative) -or $relative.StartsWith('BepInEx\core\', [System.StringComparison]::OrdinalIgnoreCase)
            if (-not $allowed) {
                throw "Bundled BepInEx archive contains an unexpected path: $relative"
            }
            $target = [System.IO.Path]::GetFullPath((Join-Path $gameFull $relative))
            if (-not (Test-DtmApiSameOrChildPath -Child $target -Parent $gameFull) -or
                [string]::Equals($target, $gameFull, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Bundled BepInEx archive escaped the game directory: $relative"
            }
            Assert-DtmApiNoReparsePointBelowGame -GameDir $gameFull -RelativePath $relative
            if ([string]::IsNullOrEmpty($entry.Name)) {
                [System.IO.Directory]::CreateDirectory($target) | Out-Null
                continue
            }
            $parent = Split-Path -Parent $target
            [System.IO.Directory]::CreateDirectory($parent) | Out-Null
            $input = $entry.Open()
            $output = [System.IO.FileStream]::new($target, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $input.CopyTo($output)
            }
            finally {
                $output.Dispose()
                $input.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }
    if (-not (Test-DtmApiBepInExComplete -GameDir $gameFull)) {
        throw 'Bundled BepInEx copy finished but required Doorstop/BepInEx files are still missing.'
    }
}

function Get-DtmApiRuntimeRequiredPaths {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $plugin = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
    return @($script:DtmApiRuntimeFiles | ForEach-Object { Join-Path $plugin $_ })
}

function Test-DtmApiRuntimeComplete {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    foreach ($path in Get-DtmApiRuntimeRequiredPaths -GameDir $GameDir) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            return $false
        }
    }
    return $true
}

function Assert-DtmApiPackageComplete {
    param([Parameter(Mandatory = $true)] [string] $PackageRoot)

    $missing = New-Object 'System.Collections.Generic.List[string]'
    $tools = Join-Path $PackageRoot 'Content\DTMAPIInstaller\tools'
    foreach ($name in @('invoke-dtmapi-action.cmd', 'probe-powershell-host.ps1', 'player-common.ps1', 'install-dtmapi.ps1', 'uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1', 'collect-logs.ps1')) {
        $path = Join-Path $tools $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $missing.Add($path)
        }
    }
    $zip = Join-Path $PackageRoot 'Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip'
    if (-not (Test-Path -LiteralPath $zip -PathType Leaf)) {
        $missing.Add($zip)
    }
    $payload = Join-Path $PackageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    foreach ($name in $script:DtmApiRuntimeFiles) {
        $path = Join-Path $payload $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $missing.Add($path)
        }
    }
    $compatibility = Join-Path $PackageRoot 'Content\DTMAPIInstaller\Payload\DTMAPI\components\compatibility\DTMAPI.GameBridge.DolocTown.Compatibility.dll'
    if (-not (Test-Path -LiteralPath $compatibility -PathType Leaf)) {
        $missing.Add($compatibility)
    }
    if ($missing.Count -gt 0) {
        throw "DTMAPI package is incomplete. Missing: $([string]::Join('; ', $missing))"
    }
}

function Get-DtmApiFileSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
        $stream.Dispose()
    }
}
