Set-StrictMode -Version Latest

$script:PortableCaptureRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)

function Get-RepoRoot {
    return $script:PortableCaptureRoot
}

function Test-DtmApiDolocTownGamePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return $false
    }

    $requiredPaths = @(
        (Join-Path $Path 'DolocTown.exe'),
        (Join-Path $Path 'DolocTown_Data'),
        (Join-Path $Path 'DolocTown_Data\Managed\Assembly-CSharp.dll')
    )
    foreach ($requiredPath in $requiredPaths) {
        if (-not (Test-Path -LiteralPath $requiredPath)) {
            return $false
        }
    }

    return $true
}

function Assert-DtmApiDolocTownGamePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [string] $Source
    )

    if (-not (Test-DtmApiDolocTownGamePath -Path $Path)) {
        throw "$Source is not a valid Doloc Town folder: $Path. Expected DolocTown.exe, DolocTown_Data, and DolocTown_Data\Managed\Assembly-CSharp.dll."
    }
}

function Add-PortableSteamRoot {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[string]] $Roots,
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Container)) {
        return
    }

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if (-not ($Roots | Where-Object { $_.Equals($resolved, [System.StringComparison]::OrdinalIgnoreCase) })) {
        $Roots.Add($resolved)
    }
}

function Resolve-DolocTownGamePath {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $AllowMissing
    )

    if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_GAME_DIR)) {
        if (-not (Test-Path -LiteralPath $env:DTMAPI_GAME_DIR -PathType Container)) {
            throw "DTMAPI_GAME_DIR is set but does not exist: $env:DTMAPI_GAME_DIR"
        }
        $resolvedEnvironmentPath = (Resolve-Path -LiteralPath $env:DTMAPI_GAME_DIR).Path
        Assert-DtmApiDolocTownGamePath -Path $resolvedEnvironmentPath -Source 'DTMAPI_GAME_DIR'
        return $resolvedEnvironmentPath
    }

    $gamePathFile = Join-Path $RepoRoot 'GAME_PATH.txt'
    if (Test-Path -LiteralPath $gamePathFile -PathType Leaf) {
        $configuredPath = ([System.IO.File]::ReadAllText($gamePathFile, [System.Text.Encoding]::UTF8)).Trim().Trim('"')
        if ([string]::IsNullOrWhiteSpace($configuredPath)) {
            throw "GAME_PATH.txt is empty: $gamePathFile"
        }
        if (-not (Test-Path -LiteralPath $configuredPath -PathType Container)) {
            throw "GAME_PATH.txt points to a missing folder: $configuredPath"
        }
        $resolvedConfiguredPath = (Resolve-Path -LiteralPath $configuredPath).Path
        Assert-DtmApiDolocTownGamePath -Path $resolvedConfiguredPath -Source 'GAME_PATH.txt'
        return $resolvedConfiguredPath
    }

    $steamRoots = [System.Collections.Generic.List[string]]::new()
    foreach ($registryPath in @(
        'HKCU:\Software\Valve\Steam',
        'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
        'HKLM:\SOFTWARE\Valve\Steam'
    )) {
        try {
            $registryValue = Get-ItemProperty -Path $registryPath -ErrorAction Stop
            Add-PortableSteamRoot -Roots $steamRoots -Path ([string]$registryValue.SteamPath)
            Add-PortableSteamRoot -Roots $steamRoots -Path ([string]$registryValue.InstallPath)
        }
        catch {
        }
    }

    foreach ($programFilesRoot in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
        if (-not [string]::IsNullOrWhiteSpace($programFilesRoot)) {
            Add-PortableSteamRoot -Roots $steamRoots -Path (Join-Path $programFilesRoot 'Steam')
        }
    }

    $libraryRoots = [System.Collections.Generic.List[string]]::new()
    foreach ($steamRoot in @($steamRoots)) {
        Add-PortableSteamRoot -Roots $libraryRoots -Path $steamRoot
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (-not (Test-Path -LiteralPath $libraryFile -PathType Leaf)) {
            continue
        }

        $libraryText = [System.IO.File]::ReadAllText($libraryFile)
        foreach ($match in [System.Text.RegularExpressions.Regex]::Matches($libraryText, '"path"\s+"([^"]+)"')) {
            Add-PortableSteamRoot -Roots $libraryRoots -Path $match.Groups[1].Value.Replace('\\', '\')
        }
    }

    foreach ($libraryRoot in @($libraryRoots)) {
        $manifestPath = Join-Path $libraryRoot 'steamapps\appmanifest_2285550.acf'
        $candidate = Join-Path $libraryRoot 'steamapps\common\Doloc Town'
        if ((Test-Path -LiteralPath $manifestPath -PathType Leaf) -and (Test-DtmApiDolocTownGamePath -Path $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    if ($AllowMissing) {
        return $null
    }

    throw "Doloc Town was not found in the registered Steam libraries. Put the absolute game-folder path in '$gamePathFile', set DTMAPI_GAME_DIR, or pass -GameDir explicitly."
}
