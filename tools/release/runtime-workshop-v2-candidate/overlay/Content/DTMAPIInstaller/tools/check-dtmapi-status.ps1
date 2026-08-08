$ErrorActionPreference = 'Stop'
$runtimeNames = @(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)

function Test-LiteGameDirectory([string] $Path) {
    return ((Test-Path -LiteralPath $Path -PathType Container) -and
        (Test-Path -LiteralPath (Join-Path $Path 'DolocTown.exe') -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $Path 'DolocTown_Data') -PathType Container))
}

function Resolve-LiteGameDirectory([string] $PackageRoot) {
    if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_GAME_DIR)) {
        $explicit = [System.IO.Path]::GetFullPath($env:DTMAPI_GAME_DIR)
        if (-not (Test-LiteGameDirectory $explicit)) {
            throw "DTMAPI_GAME_DIR is not a Doloc Town game directory: $explicit"
        }
        return $explicit
    }

    $packageFull = [System.IO.Path]::GetFullPath($PackageRoot).TrimEnd('\', '/')
    $match = [regex]::Match(
        $packageFull,
        '^(?<library>.+)[\\/]steamapps[\\/]workshop[\\/]content[\\/]2285550[\\/][^\\/]+$',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
        $library = $match.Groups['library'].Value
        $manifest = Join-Path $library 'steamapps\appmanifest_2285550.acf'
        $installName = 'Doloc Town'
        if (Test-Path -LiteralPath $manifest -PathType Leaf) {
            try {
                $text = Get-Content -LiteralPath $manifest -Raw -ErrorAction Stop
                $nameMatch = [regex]::Match($text, '"installdir"\s+"([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                if ($nameMatch.Success) {
                    $installName = $nameMatch.Groups[1].Value.Replace('\\', '\')
                }
            }
            catch {
            }
        }
        $candidate = Join-Path $library (Join-Path 'steamapps\common' $installName)
        if (Test-LiteGameDirectory $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    foreach ($registryPath in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $record = Get-ItemProperty -Path $registryPath -ErrorAction Stop
            $steamPath = if ($record.PSObject.Properties['SteamPath']) { [string]$record.SteamPath } else { [string]$record.InstallPath }
            $candidate = Join-Path $steamPath 'steamapps\common\Doloc Town'
            if (Test-LiteGameDirectory $candidate) {
                return [System.IO.Path]::GetFullPath($candidate)
            }
        }
        catch {
        }
    }
    throw 'Doloc Town could not be located.'
}

$errors = New-Object 'System.Collections.Generic.List[string]'
$warnings = New-Object 'System.Collections.Generic.List[string]'

Write-Host "[INFO] Lightweight DTMAPI status checker"
Write-Host "[INFO] PowerShell: $($PSVersionTable.PSVersion) $($PSVersionTable.PSEdition)"

$packageRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$requiredPackageFiles = @(
    (Join-Path $packageRoot 'Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip'),
    (Join-Path $packageRoot 'Content\DTMAPIInstaller\tools\install-dtmapi.ps1')
)
$payloadRoot = Join-Path $packageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
$requiredPackageFiles += @($runtimeNames | ForEach-Object { Join-Path $payloadRoot $_ })
foreach ($path in $requiredPackageFiles) {
    try {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $errors.Add("Package file missing: $path")
        }
    }
    catch {
        $errors.Add("Package file check failed: $path. $($_.Exception.Message)")
    }
}

$gameDir = ''
try {
    $gameDir = Resolve-LiteGameDirectory $packageRoot
    Write-Host "[OK] Game directory: $gameDir" -ForegroundColor Green
}
catch {
    $errors.Add($_.Exception.Message)
}

if (-not [string]::IsNullOrWhiteSpace($gameDir)) {
    foreach ($relative in @(
        'winhttp.dll',
        'doorstop_config.ini',
        'BepInEx\core\BepInEx.dll',
        'BepInEx\core\BepInEx.Preloader.dll',
        'BepInEx\core\0Harmony.dll'
    )) {
        $path = Join-Path $gameDir $relative
        try {
            if (Test-Path -LiteralPath $path -PathType Leaf) {
                Write-Host "[OK] $relative" -ForegroundColor Green
            }
            else {
                $errors.Add("Installed file missing: $path")
            }
        }
        catch {
            $errors.Add("Installed file check failed: $path. $($_.Exception.Message)")
        }
    }

    $runtimeRoot = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
    foreach ($name in $runtimeNames) {
        $path = Join-Path $runtimeRoot $name
        try {
            if (Test-Path -LiteralPath $path -PathType Leaf) {
                Write-Host "[OK] $name" -ForegroundColor Green
            }
            else {
                $errors.Add("DTMAPI Runtime file missing: $path")
            }
        }
        catch {
            $errors.Add("DTMAPI Runtime check failed: $path. $($_.Exception.Message)")
        }
    }

    $versionMarker = Join-Path $gameDir 'DTMAPI\installed-version.txt'
    if (Test-Path -LiteralPath $versionMarker -PathType Leaf) {
        try {
            Write-Host "[INFO] Installed marker: $((Get-Content -LiteralPath $versionMarker -First 1 -ErrorAction Stop))"
        }
        catch {
            $warnings.Add("Could not read installed version marker: $($_.Exception.Message)")
        }
    }
    else {
        $warnings.Add('Installed version marker is absent. Run 1_install_dtmapi.bat to converge the installation.')
    }

    $latestLog = Join-Path $gameDir 'DTMAPI\logs\latest.log'
    if (Test-Path -LiteralPath $latestLog -PathType Leaf) {
        Write-Host "[INFO] Latest DTMAPI log: $latestLog"
    }
    else {
        Write-Host '[INFO] Latest DTMAPI log is not present yet.'
    }
}

foreach ($warning in $warnings) {
    Write-Host "[WARN] $warning" -ForegroundColor Yellow
}
if ($errors.Count -gt 0) {
    foreach ($message in $errors) {
        Write-Host "[ERROR] $message" -ForegroundColor Red
    }
    Write-Host '[HELP] If package files are missing, resubscribe to DTMAPI.' -ForegroundColor Yellow
    Write-Host '[HELP] Otherwise run 1_install_dtmapi.bat again.' -ForegroundColor Yellow
    Write-Host '[HELP] If the checker itself cannot start, copy the whole DTMAPI folder to a path containing only English letters and numbers, then try again.' -ForegroundColor Yellow
    exit 1
}

Write-Host '[OK] Required BepInEx and DTMAPI Runtime files are present.' -ForegroundColor Green
Write-Host '[INFO] This is a static file check; it does not prove that the current game launch loaded BepInEx or DTMAPI.'
exit 0
