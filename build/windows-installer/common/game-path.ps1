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
