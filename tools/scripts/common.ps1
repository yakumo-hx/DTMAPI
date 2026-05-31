Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptDir '..\..')).Path
}

function Get-DotNetExe {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($systemDotnet) {
        $sdks = & $systemDotnet.Source --list-sdks 2>$null
        if ($LASTEXITCODE -eq 0 -and $sdks) {
            return $systemDotnet.Source
        }
    }

    $localDotnet = Join-Path $RepoRoot '.tools\dotnet\dotnet.exe'
    if (Test-Path $localDotnet) {
        $sdks = & $localDotnet --list-sdks 2>$null
        if ($LASTEXITCODE -eq 0 -and $sdks) {
            return $localDotnet
        }
    }

    $toolsDir = Join-Path $RepoRoot '.tools'
    New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
    $installScript = Join-Path $toolsDir 'dotnet-install.ps1'
    if (-not (Test-Path $installScript)) {
        Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installScript
    }

    $installLog = Join-Path $toolsDir 'dotnet-install.log'
    & powershell -NoProfile -ExecutionPolicy Bypass -File $installScript -Channel 8.0 -InstallDir (Join-Path $toolsDir 'dotnet') *> $installLog
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install local .NET SDK. See $installLog"
    }
    return (Resolve-Path $localDotnet).Path
}

function Read-LocalSettings {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $path = Join-Path $RepoRoot 'local.settings.json'
    if (Test-Path $path) {
        return Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
    }
    return $null
}

function Resolve-DolocTownGamePath {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $AllowMissing
    )

    if ($env:DTMAPI_GAME_DIR -and (Test-Path $env:DTMAPI_GAME_DIR)) {
        return (Resolve-Path $env:DTMAPI_GAME_DIR).Path
    }

    $settings = Read-LocalSettings -RepoRoot $RepoRoot
    if ($settings -and $settings.GameDir -and (Test-Path $settings.GameDir)) {
        return (Resolve-Path $settings.GameDir).Path
    }

    $steamRoots = New-Object System.Collections.Generic.List[string]
    foreach ($registryPath in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $steamPath = (Get-ItemProperty -Path $registryPath -ErrorAction Stop).SteamPath
            if ($steamPath -and (Test-Path $steamPath)) {
                $steamRoots.Add((Resolve-Path $steamPath).Path)
            }
        }
        catch {
        }
    }

    foreach ($steamRoot in @($steamRoots)) {
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        $libraryRoots = New-Object System.Collections.Generic.List[string]
        $libraryRoots.Add($steamRoot)
        if (Test-Path $libraryFile) {
            $text = Get-Content -Raw -LiteralPath $libraryFile
            foreach ($match in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
                $path = $match.Groups[1].Value.Replace('\\', '\')
                if (Test-Path $path) {
                    $libraryRoots.Add((Resolve-Path $path).Path)
                }
            }
        }

        foreach ($libraryRoot in @($libraryRoots)) {
            $manifest = Join-Path $libraryRoot 'steamapps\appmanifest_2285550.acf'
            if (Test-Path $manifest) {
                $candidate = Join-Path $libraryRoot 'steamapps\common\Doloc Town'
                if (Test-Path $candidate) {
                    return (Resolve-Path $candidate).Path
                }
            }
        }
    }

    if ($AllowMissing) {
        return $null
    }
    throw "Doloc Town game path not found. Set DTMAPI_GAME_DIR or create local.settings.json with { `"GameDir`": `"C:\\path\\to\\Doloc Town`" }."
}

function Get-DtmapiOutputDir {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Configuration = 'Release'
    )
    return Join-Path $RepoRoot "src\DTMAPI.BepInExBootstrap\bin\$Configuration\netstandard2.0"
}

function Copy-DirectoryContents {
    param(
        [string] $Source,
        [string] $Destination,
        [string[]] $Include
    )

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($name in $Include) {
        $path = Join-Path $Source $name
        if (Test-Path $path) {
            $target = Join-Path $Destination $name
            if (Test-Path $path -PathType Container) {
                New-Item -ItemType Directory -Force -Path $target | Out-Null
                Get-ChildItem -LiteralPath $path -Force | Copy-Item -Recurse -Force -Destination $target
            }
            else {
                Copy-Item -Force -LiteralPath $path -Destination $target
            }
        }
    }
}

function Wait-ForLogLine {
    param(
        [string] $LogPath,
        [string] $Pattern,
        [int] $TimeoutSeconds = 120,
        [switch] $AbortOnFatalInstanceWindow
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $LogPath) {
            $text = Get-Content -Raw -LiteralPath $LogPath -ErrorAction SilentlyContinue
            if ($text -match [regex]::Escape($Pattern)) {
                return $true
            }
        }
        if ($AbortOnFatalInstanceWindow -and (Test-FatalInstanceWindow)) {
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
        if ($title -eq 'Fatal error' -or $combined -like '*Another instance is already running*') {
            $matches += [pscustomobject]@{
                Hwnd = ('0x{0:X}' -f $handle.ToInt64())
                Title = $title
                Text = $combined.Trim()
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
