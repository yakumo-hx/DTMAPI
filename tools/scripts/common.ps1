Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path -LiteralPath (Join-Path $scriptDir '..\..')).Path
}

function Get-DtmApiObjectProperty {
    param(
        $Object,
        [Parameter(Mandatory = $true)] [string] $Name,
        $Default = $null
    )

    if ($null -ne $Object -and $Object.PSObject.Properties[$Name]) {
        return $Object.$Name
    }
    return $Default
}

function Invoke-DtmApiGitValue {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        return ''
    }

    $output = & $git.Source -C $RepoRoot @Arguments 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $output) {
        return ''
    }

    return [string]($output | Select-Object -First 1)
}

function Get-DtmApiGitCommonDir {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $commonDir = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('rev-parse', '--git-common-dir')
    if (-not [string]::IsNullOrWhiteSpace($commonDir)) {
        if (-not [System.IO.Path]::IsPathRooted($commonDir)) {
            $commonDir = Join-Path $RepoRoot $commonDir
        }
        return [System.IO.Path]::GetFullPath($commonDir)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot '.git'))
}

function Get-DtmApiCurrentWorktree {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $worktree = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('rev-parse', '--show-toplevel')
    if ([string]::IsNullOrWhiteSpace($worktree)) {
        return [System.IO.Path]::GetFullPath($RepoRoot)
    }
    return [System.IO.Path]::GetFullPath($worktree)
}

function Get-DtmApiCurrentBranch {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $branch = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('branch', '--show-current')
    if ([string]::IsNullOrWhiteSpace($branch)) {
        return '(detached)'
    }
    return $branch
}

function Get-DtmApiCurrentCommit {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $commit = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('rev-parse', '--short=12', 'HEAD')
    if ([string]::IsNullOrWhiteSpace($commit)) {
        return '(unknown)'
    }
    return $commit
}

function Get-DtmApiRuntimeLockPath {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    if ($env:DTMAPI_RUNTIME_LOCK_PATH) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_RUNTIME_LOCK_PATH)
    }

    return Join-Path (Get-DtmApiGitCommonDir -RepoRoot $RepoRoot) 'dtmapi-runtime.lock.json'
}

function New-DtmApiRuntimeLockMetadata {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Reason = '',
        [string] $Owner = ''
    )

    $branch = Get-DtmApiCurrentBranch -RepoRoot $RepoRoot
    if ([string]::IsNullOrWhiteSpace($Owner)) {
        $Owner = '{0}@{1}/{2}' -f [Environment]::UserName, $env:COMPUTERNAME, $branch
    }

    return [ordered]@{
        SchemaVersion = 1
        Token = [guid]::NewGuid().ToString('N')
        Owner = $Owner
        Reason = $Reason
        Worktree = Get-DtmApiCurrentWorktree -RepoRoot $RepoRoot
        Branch = $branch
        Commit = Get-DtmApiCurrentCommit -RepoRoot $RepoRoot
        ProcessId = $PID
        Host = $env:COMPUTERNAME
        User = [Environment]::UserName
        CreatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    }
}

function Get-DtmApiRuntimeLockInfo {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $path = Get-DtmApiRuntimeLockPath -RepoRoot $RepoRoot
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        return [pscustomobject]@{
            Exists = $false
            IsReadable = $true
            Path = $path
            Data = $null
            Error = ''
        }
    }

    try {
        $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $path | ConvertFrom-Json
        return [pscustomobject]@{
            Exists = $true
            IsReadable = $true
            Path = $path
            Data = $data
            Error = ''
        }
    }
    catch {
        return [pscustomobject]@{
            Exists = $true
            IsReadable = $false
            Path = $path
            Data = $null
            Error = $_.Exception.Message
        }
    }
}

function Test-DtmApiRuntimeLockOwnedByCurrentWorktree {
    param(
        [Parameter(Mandatory = $true)] $LockInfo,
        [string] $RepoRoot = (Get-RepoRoot)
    )

    if (-not $LockInfo.Exists -or -not $LockInfo.IsReadable -or $null -eq $LockInfo.Data) {
        return $false
    }

    $currentWorktree = [System.IO.Path]::GetFullPath((Get-DtmApiCurrentWorktree -RepoRoot $RepoRoot)).TrimEnd('\')
    $lockedWorktree = [string](Get-DtmApiObjectProperty -Object $LockInfo.Data -Name 'Worktree' -Default '')
    if ([string]::IsNullOrWhiteSpace($lockedWorktree)) {
        return $false
    }

    $lockedWorktree = [System.IO.Path]::GetFullPath($lockedWorktree).TrimEnd('\')
    return [string]::Equals($currentWorktree, $lockedWorktree, [System.StringComparison]::OrdinalIgnoreCase)
}

function Format-DtmApiRuntimeLockInfo {
    param(
        [Parameter(Mandatory = $true)] $LockInfo
    )

    if (-not $LockInfo.Exists) {
        return "[FREE] Runtime lock is free. Path: $($LockInfo.Path)"
    }

    if (-not $LockInfo.IsReadable) {
        return "[LOCKED] Runtime lock exists but cannot be read. Path: $($LockInfo.Path). Error: $($LockInfo.Error)"
    }

    $data = $LockInfo.Data
    $createdAt = [string](Get-DtmApiObjectProperty -Object $data -Name 'CreatedAt' -Default '')
    $age = ''
    if (-not [string]::IsNullOrWhiteSpace($createdAt)) {
        try {
            $ageSpan = [DateTimeOffset]::UtcNow - [DateTimeOffset]::Parse($createdAt).ToUniversalTime()
            $age = ' age={0:hh\:mm\:ss}' -f $ageSpan
        }
        catch {
            $age = ''
        }
    }

    return "[LOCKED] owner=$((Get-DtmApiObjectProperty -Object $data -Name 'Owner' -Default '(unknown)')) branch=$((Get-DtmApiObjectProperty -Object $data -Name 'Branch' -Default '(unknown)')) commit=$((Get-DtmApiObjectProperty -Object $data -Name 'Commit' -Default '(unknown)')) reason=$((Get-DtmApiObjectProperty -Object $data -Name 'Reason' -Default ''))$age worktree=$((Get-DtmApiObjectProperty -Object $data -Name 'Worktree' -Default '(unknown)'))"
}

function Acquire-DtmApiRuntimeLock {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Reason = '',
        [string] $Owner = '',
        [switch] $AllowCurrentWorktreeReuse,
        [switch] $NoThrow
    )

    $path = Get-DtmApiRuntimeLockPath -RepoRoot $RepoRoot
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    $metadata = New-DtmApiRuntimeLockMetadata -RepoRoot $RepoRoot -Reason $Reason -Owner $Owner
    $json = ($metadata | ConvertTo-Json -Depth 8)
    $encoding = New-Object System.Text.UTF8Encoding($false)

    try {
        $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        try {
            $bytes = $encoding.GetBytes($json)
            $stream.Write($bytes, 0, $bytes.Length)
        }
        finally {
            $stream.Dispose()
        }

        return [pscustomobject]@{
            Acquired = $true
            Reused = $false
            Path = $path
            Data = [pscustomobject]$metadata
            Message = "Acquired DTMAPI runtime lock: $path"
        }
    }
    catch [System.IO.IOException] {
        $info = Get-DtmApiRuntimeLockInfo -RepoRoot $RepoRoot
        if ($AllowCurrentWorktreeReuse -and (Test-DtmApiRuntimeLockOwnedByCurrentWorktree -LockInfo $info -RepoRoot $RepoRoot)) {
            return [pscustomobject]@{
                Acquired = $false
                Reused = $true
                Path = $path
                Data = $info.Data
                Message = "Reusing DTMAPI runtime lock already owned by this worktree: $path"
            }
        }

        $message = "DTMAPI runtime lock is already held. $((Format-DtmApiRuntimeLockInfo -LockInfo $info))"
        if ($NoThrow) {
            return [pscustomobject]@{
                Acquired = $false
                Reused = $false
                Path = $path
                Data = $info.Data
                Message = $message
            }
        }
        throw $message
    }
}

function Wait-DtmApiRuntimeLock {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Reason = '',
        [string] $Owner = '',
        [int] $TimeoutSeconds = 3600,
        [int] $PollSeconds = 5,
        [switch] $AllowCurrentWorktreeReuse
    )

    if ($TimeoutSeconds -lt 0) {
        throw "TimeoutSeconds must be >= 0."
    }
    if ($PollSeconds -lt 1) {
        $PollSeconds = 1
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ($true) {
        $lock = Acquire-DtmApiRuntimeLock -RepoRoot $RepoRoot -Reason $Reason -Owner $Owner -AllowCurrentWorktreeReuse:$AllowCurrentWorktreeReuse -NoThrow
        if ($lock.Acquired -or $lock.Reused) {
            return $lock
        }

        if ((Get-Date) -ge $deadline) {
            throw "Timed out waiting for DTMAPI runtime lock after $TimeoutSeconds seconds. $($lock.Message)"
        }

        Write-Host "Waiting for DTMAPI runtime lock. $($lock.Message)"
        Start-Sleep -Seconds $PollSeconds
    }
}

function Release-DtmApiRuntimeLock {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $Force
    )

    $info = Get-DtmApiRuntimeLockInfo -RepoRoot $RepoRoot
    if (-not $info.Exists) {
        return [pscustomobject]@{
            Released = $false
            Path = $info.Path
            Message = "DTMAPI runtime lock is already free."
        }
    }

    if (-not $Force -and -not (Test-DtmApiRuntimeLockOwnedByCurrentWorktree -LockInfo $info -RepoRoot $RepoRoot)) {
        throw "Refusing to release DTMAPI runtime lock owned by another worktree. Use -Force only after verifying the owner is stale. $((Format-DtmApiRuntimeLockInfo -LockInfo $info))"
    }

    Remove-Item -LiteralPath $info.Path -Force
    return [pscustomobject]@{
        Released = $true
        Path = $info.Path
        Message = "Released DTMAPI runtime lock: $($info.Path)"
    }
}

function Test-DtmApiDotNet8Toolchain {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    $sdks = & $Path --list-sdks 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $sdks) {
        return $false
    }

    $runtimes = & $Path --list-runtimes 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $runtimes) {
        return $false
    }

    return [bool]($runtimes -match '^Microsoft\.NETCore\.App 8\.')
}

function Get-DotNetExe {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $localDotnet = Join-Path $RepoRoot '.tools\dotnet\dotnet.exe'
    if (Test-DtmApiDotNet8Toolchain -Path $localDotnet) {
        return (Resolve-Path -LiteralPath $localDotnet).Path
    }

    $systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($systemDotnet -and (Test-DtmApiDotNet8Toolchain -Path $systemDotnet.Source)) {
        return $systemDotnet.Source
    }

    $toolsDir = Join-Path $RepoRoot '.tools'
    New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
    $installScript = Join-Path $toolsDir 'dotnet-install.ps1'
    if (-not (Test-Path $installScript)) {
        Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installScript
    }

    $powershellHost = Get-DtmApiPowerShellHost
    if ([string]::IsNullOrWhiteSpace($powershellHost)) {
        throw "PowerShell host was not found; cannot install local .NET SDK."
    }

    $installLog = Join-Path $toolsDir 'dotnet-install.log'
    & $powershellHost -NoProfile -ExecutionPolicy Bypass -File $installScript -Channel 8.0 -InstallDir (Join-Path $toolsDir 'dotnet') *> $installLog
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install local .NET SDK. See $installLog"
    }
    if (-not (Test-DtmApiDotNet8Toolchain -Path $localDotnet)) {
        throw "The local .NET SDK installation completed without a usable Microsoft.NETCore.App 8.x runtime. See $installLog"
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

function Get-DtmApiPowerShellHost {
    param([switch] $RequireWindowsPowerShell)

    if (-not [string]::IsNullOrWhiteSpace($env:SystemRoot)) {
        $windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        if (Test-Path -LiteralPath $windowsPowerShell -PathType Leaf) {
            return [System.IO.Path]::GetFullPath($windowsPowerShell)
        }
    }

    $powershell = Get-Command powershell.exe -ErrorAction SilentlyContinue
    if ($powershell) {
        return $powershell.Source
    }

    if (-not $RequireWindowsPowerShell) {
        if ($PSVersionTable.PSEdition -eq 'Core') {
            try {
                $currentHost = (Get-Process -Id $PID -ErrorAction Stop).Path
                if ($currentHost -and (Test-Path -LiteralPath $currentHost -PathType Leaf)) {
                    return [System.IO.Path]::GetFullPath($currentHost)
                }
            }
            catch {
            }
        }

        $pwsh = Get-Command pwsh.exe -ErrorAction SilentlyContinue
        if ($pwsh) {
            return $pwsh.Source
        }

        foreach ($candidateRoot in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
            if ([string]::IsNullOrWhiteSpace($candidateRoot)) {
                continue
            }

            $candidate = Join-Path $candidateRoot 'PowerShell\7\pwsh.exe'
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return [System.IO.Path]::GetFullPath($candidate)
            }
        }
    }

    return ''
}

function Test-DtmApiDolocTownGamePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

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

    return $true
}

function Assert-DtmApiDolocTownGamePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Source
    )

    if (-not (Test-DtmApiDolocTownGamePath -Path $Path)) {
        throw "$Source does not point to a valid Doloc Town game folder: $Path. Expected DolocTown.exe and DolocTown_Data in this folder."
    }
}

function Resolve-DolocTownGamePath {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $AllowMissing
    )

    if ($env:DTMAPI_GAME_DIR) {
        if (Test-Path -LiteralPath $env:DTMAPI_GAME_DIR) {
            $resolved = (Resolve-Path -LiteralPath $env:DTMAPI_GAME_DIR).Path
            Assert-DtmApiDolocTownGamePath -Path $resolved -Source 'DTMAPI_GAME_DIR'
            return $resolved
        }
        throw "DTMAPI_GAME_DIR is set but does not exist: $env:DTMAPI_GAME_DIR"
    }

    $settings = Read-LocalSettings -RepoRoot $RepoRoot
    $settingsGameDir = if ($settings) { Get-DtmApiObjectProperty -Object $settings -Name 'GameDir' -Default '' } else { '' }
    if ($settingsGameDir) {
        if (Test-Path -LiteralPath $settingsGameDir) {
            $resolved = (Resolve-Path -LiteralPath $settingsGameDir).Path
            Assert-DtmApiDolocTownGamePath -Path $resolved -Source 'local.settings.json GameDir'
            return $resolved
        }
        throw "local.settings.json GameDir does not exist: $settingsGameDir"
    }

    $steamRoots = New-Object System.Collections.Generic.List[string]
    foreach ($registryPath in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $steamPath = (Get-ItemProperty -Path $registryPath -ErrorAction Stop).SteamPath
            if ($steamPath -and (Test-Path -LiteralPath $steamPath)) {
                $steamRoots.Add((Resolve-Path -LiteralPath $steamPath).Path)
            }
        }
        catch {
        }
    }

    foreach ($steamRoot in @($steamRoots)) {
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        $libraryRoots = New-Object System.Collections.Generic.List[string]
        $libraryRoots.Add($steamRoot)
        if (Test-Path -LiteralPath $libraryFile) {
            $text = Get-Content -Raw -LiteralPath $libraryFile
            foreach ($match in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
                $path = $match.Groups[1].Value.Replace('\\', '\')
                if (Test-Path -LiteralPath $path) {
                    $libraryRoots.Add((Resolve-Path -LiteralPath $path).Path)
                }
            }
        }

        foreach ($libraryRoot in @($libraryRoots)) {
            $manifest = Join-Path $libraryRoot 'steamapps\appmanifest_2285550.acf'
            if (Test-Path -LiteralPath $manifest) {
                $candidate = Join-Path $libraryRoot 'steamapps\common\Doloc Town'
                if (Test-DtmApiDolocTownGamePath -Path $candidate) {
                    return (Resolve-Path -LiteralPath $candidate).Path
                }
            }
        }
    }

    if ($AllowMissing) {
        return $null
    }
    throw "Doloc Town game path not found. Set DTMAPI_GAME_DIR or create local.settings.json with { `"GameDir`": `"C:\\path\\to\\Doloc Town`" }."
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

function Get-DtmApiNormalizedFullPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $full = [System.IO.Path]::GetFullPath($Path)
    return $full.TrimEnd([char[]]@('\', '/'))
}

function Test-DtmApiPathIsSameOrChild {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    $childFull = Get-DtmApiNormalizedFullPath -Path $Child
    $parentFull = Get-DtmApiNormalizedFullPath -Path $Parent
    if ([string]::Equals($childFull, $parentFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $parentPrefix = $parentFull + [System.IO.Path]::DirectorySeparatorChar
    return $childFull.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-DtmApiPrivateArtifactPathOutsideRepository {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $RepositoryRoot,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
    if (Test-DtmApiPathIsSameOrChild -Child $resolvedPath -Parent $resolvedRepositoryRoot) {
        throw "$Label must stay outside the source/distribution tree: $resolvedPath"
    }

    $cursor = $resolvedPath
    while (-not [string]::IsNullOrWhiteSpace($cursor)) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Label path may not contain an existing reparse-point ancestor: $($item.FullName)"
            }
        }

        $parent = [System.IO.Directory]::GetParent($cursor)
        if ($null -eq $parent -or
            [string]::Equals($parent.FullName, $cursor, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $cursor = $parent.FullName
    }

    return $resolvedPath
}

function Get-DtmApiBepInExRequiredInstallFiles {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    return @(
        [pscustomobject]@{ Label = 'Doorstop version marker'; Path = (Join-Path $GameDir '.doorstop_version'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'BepInEx changelog'; Path = (Join-Path $GameDir 'changelog.txt'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'Doorstop winhttp.dll'; Path = (Join-Path $GameDir 'winhttp.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'Doorstop config'; Path = (Join-Path $GameDir 'doorstop_config.ini'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = '0Harmony20'; Path = (Join-Path $GameDir 'BepInEx\core\0Harmony20.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'BepInEx core'; Path = (Join-Path $GameDir 'BepInEx\core\BepInEx.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'BepInEx preloader'; Path = (Join-Path $GameDir 'BepInEx\core\BepInEx.Preloader.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'BepInEx Harmony bridge'; Path = (Join-Path $GameDir 'BepInEx\core\BepInEx.Harmony.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'Harmony'; Path = (Join-Path $GameDir 'BepInEx\core\0Harmony.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'HarmonyXInterop'; Path = (Join-Path $GameDir 'BepInEx\core\HarmonyXInterop.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'Mono.Cecil'; Path = (Join-Path $GameDir 'BepInEx\core\Mono.Cecil.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'Mono.Cecil.Mdb'; Path = (Join-Path $GameDir 'BepInEx\core\Mono.Cecil.Mdb.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'Mono.Cecil.Pdb'; Path = (Join-Path $GameDir 'BepInEx\core\Mono.Cecil.Pdb.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'Mono.Cecil.Rocks'; Path = (Join-Path $GameDir 'BepInEx\core\Mono.Cecil.Rocks.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'MonoMod.RuntimeDetour'; Path = (Join-Path $GameDir 'BepInEx\core\MonoMod.RuntimeDetour.dll'); PathType = 'Leaf' },
        [pscustomobject]@{ Label = 'MonoMod.Utils'; Path = (Join-Path $GameDir 'BepInEx\core\MonoMod.Utils.dll'); PathType = 'Leaf' }
    )
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
        throw "DolocTown.exe is running from this game folder. Please close Doloc Town before running DTMAPI $Operation."
    }
}

function Test-DtmApiDoorstopConfigEnabledForBepInEx {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    $enabledValue = $null
    $targetValue = $null
    try {
        foreach ($line in Get-Content -LiteralPath $Path) {
            $trimmed = [string]$line
            $trimmed = $trimmed.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                continue
            }

            if ($trimmed.StartsWith('#') -or $trimmed.StartsWith(';')) {
                continue
            }

            if ($trimmed -match '^(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>.*?)\s*(?:[#;].*)?$') {
                $key = $Matches['key']
                $value = $Matches['value'].Trim().Trim('"').Trim("'")
                if ([string]::Equals($key, 'enabled', [System.StringComparison]::OrdinalIgnoreCase)) {
                    $enabledValue = $value
                }
                elseif ([string]::Equals($key, 'target_assembly', [System.StringComparison]::OrdinalIgnoreCase) -or
                    [string]::Equals($key, 'targetAssembly', [System.StringComparison]::OrdinalIgnoreCase)) {
                    $targetValue = $value
                }
            }
        }
    }
    catch {
        return $false
    }

    if (-not [string]::Equals($enabledValue, 'true', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    $normalizedTarget = ([string]$targetValue).Trim().Replace('/', '\')
    return [string]::Equals($normalizedTarget, 'BepInEx\core\BepInEx.Preloader.dll', [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-DtmApiBepInExInstallComplete {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    foreach ($item in Get-DtmApiBepInExRequiredInstallFiles -GameDir $GameDir) {
        if (-not (Test-Path -LiteralPath $item.Path -PathType $item.PathType)) {
            return $false
        }

        if ($item.PathType -eq 'Leaf') {
            try {
                if ((Get-Item -LiteralPath $item.Path).Length -le 0) {
                    return $false
                }
            }
            catch {
                return $false
            }
        }
    }

    if (-not (Test-DtmApiDoorstopConfigEnabledForBepInEx -Path (Join-Path $GameDir 'doorstop_config.ini'))) {
        return $false
    }

    return $true
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
        [string[]] $Include,
        [switch] $Required,
        [switch] $AllowDestinationInsideSource
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        if ($Required) {
            throw "Required source directory is missing: $Source"
        }

        return
    }

    if (-not $AllowDestinationInsideSource) {
        if (Test-DtmApiPathIsSameOrChild -Child $Destination -Parent $Source) {
            throw "Refusing to copy directory contents because destination is inside source. Source: $Source. Destination: $Destination"
        }

        if (Test-DtmApiPathIsSameOrChild -Child $Source -Parent $Destination) {
            throw "Refusing to copy directory contents because source is inside destination. Source: $Source. Destination: $Destination"
        }
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($name in $Include) {
        if ([System.IO.Path]::IsPathRooted($name) -or @($name -split '[\\/]' | Where-Object { $_ -eq '..' }).Count -gt 0) {
            throw "Refusing to copy unsafe include path '$name'. Include values must be relative names inside the source directory."
        }

        $path = Join-Path $Source $name
        if (-not (Test-Path -LiteralPath $path)) {
            if ($Required) {
                throw "Required source item is missing: $path"
            }

            continue
        }

        $target = Join-Path $Destination $name
        if (Test-Path -LiteralPath $path -PathType Container) {
            New-Item -ItemType Directory -Force -Path $target | Out-Null
            Get-ChildItem -LiteralPath $path -Force | Copy-Item -Recurse -Force -Destination $target
        }
        else {
            Copy-Item -Force -LiteralPath $path -Destination $target
        }
    }
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
