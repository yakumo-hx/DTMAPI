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
        [Parameter(Mandatory = $true)] [string] $Path,
        [string] $RepoRoot = (Get-RepoRoot)
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    $policyPath = Join-Path $RepoRoot 'global.json'
    if (-not (Test-Path -LiteralPath $policyPath -PathType Leaf)) { return $false }
    try {
        $policy = Get-Content -LiteralPath $policyPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ([string]$policy.sdk.version -notmatch '^8\.\d+\.\d+$' -or
            [string]$policy.sdk.rollForward -notin @('patch', 'latestPatch', 'disable') -or
            $policy.sdk.allowPrerelease -ne $false) { return $false }
        $required = [version]$policy.sdk.version
        $resolvedHost = (Resolve-Path -LiteralPath $Path).Path
        # SDK resolution starts at the invocation directory, not the csproj path.
        # A host listing an 8.x runtime can still select an incompatible SDK.
        Push-Location -LiteralPath $RepoRoot
        try {
            $selected = @(& $resolvedHost --version 2>$null)
            if ($LASTEXITCODE -ne 0 -or $selected.Count -ne 1 -or [string]$selected[0] -notmatch '^8\.\d+\.\d+$') { return $false }
            $actual = [version]([string]$selected[0]).Trim()
            if ($actual -lt $required -or $actual.Minor -ne $required.Minor -or
                [math]::Floor($actual.Build / 100) -ne [math]::Floor($required.Build / 100) -or
                ($policy.sdk.rollForward -eq 'disable' -and $actual -ne $required)) { return $false }
            $runtimes = @(& $resolvedHost --list-runtimes 2>$null)
            return [bool]($LASTEXITCODE -eq 0 -and ($runtimes -match '^Microsoft\.NETCore\.App 8\.'))
        }
        finally { Pop-Location }
    }
    catch { return $false }
}

function New-DtmApiInstallerException {
    param(
        [Parameter(Mandatory = $true)] [string] $Code,
        [Parameter(Mandatory = $true)] [string] $Chinese,
        [Parameter(Mandatory = $true)] [string] $English,
        [string] $Detail = '',
        [System.Exception] $InnerException = $null
    )

    $message = "[$Code] $Chinese / $English"
    if (-not [string]::IsNullOrWhiteSpace($Detail)) {
        $message += " Detail: $Detail"
    }
    $exception = if ($null -eq $InnerException) {
        New-Object -TypeName System.InvalidOperationException -ArgumentList $message
    }
    else {
        New-Object -TypeName System.InvalidOperationException -ArgumentList @($message, $InnerException)
    }
    $exception.Data['DtmApiMessageCode'] = $Code
    $exception.Data['DtmApiChinese'] = $Chinese
    $exception.Data['DtmApiEnglish'] = $English
    $exception.Data['DtmApiDetail'] = $Detail
    return $exception
}

function Throw-DtmApiInstallerError {
    param(
        [Parameter(Mandatory = $true)] [string] $Code,
        [Parameter(Mandatory = $true)] [string] $Chinese,
        [Parameter(Mandatory = $true)] [string] $English,
        [string] $Detail = '',
        [System.Exception] $InnerException = $null
    )

    throw (New-DtmApiInstallerException -Code $Code -Chinese $Chinese -English $English -Detail $Detail -InnerException $InnerException)
}

function Write-DtmApiInstallerMessage {
    param(
        [Parameter(Mandatory = $true)] [string] $Code,
        [Parameter(Mandatory = $true)] [string] $Chinese,
        [Parameter(Mandatory = $true)] [string] $English,
        [string] $Detail = '',
        [ValidateSet('Info', 'Success', 'Warning', 'Error')] [string] $Level = 'Info'
    )

    $color = switch ($Level) {
        'Success' { 'Green' }
        'Warning' { 'Yellow' }
        'Error' { 'Red' }
        default { 'Cyan' }
    }
    Write-Host ("[{0}] {1}" -f $Code, $Chinese) -ForegroundColor $color
    Write-Host ("[{0}] {1}" -f $Code, $English) -ForegroundColor $color
    if (-not [string]::IsNullOrWhiteSpace($Detail)) {
        Write-Host ("     {0}" -f $Detail)
    }
}

function Get-DtmApiInstallerFailureInfo {
    param([Parameter(Mandatory = $true)] $ErrorRecord)

    $exception = $ErrorRecord.Exception
    while ($null -ne $exception) {
        if ($exception.Data -and $exception.Data.Contains('DtmApiMessageCode')) {
            return [pscustomobject]@{
                Code = [string]$exception.Data['DtmApiMessageCode']
                Chinese = [string]$exception.Data['DtmApiChinese']
                English = [string]$exception.Data['DtmApiEnglish']
                Detail = [string]$exception.Data['DtmApiDetail']
            }
        }
        $exception = $exception.InnerException
    }

    $exception = $ErrorRecord.Exception
    while ($null -ne $exception) {
        if ($exception -is [System.UnauthorizedAccessException] -or $exception -is [System.IO.IOException]) {
            return [pscustomobject]@{
                Code = 'DTM-E1102'
                Chinese = '本地文件被占用或被安全软件拦截；这通常不是 Windows 防火墙问题。请关闭游戏及占用程序，检查杀毒软件或电脑管家的隔离/防护记录后重试。'
                English = 'A local file is locked or blocked by endpoint security; this is usually not a Windows Firewall issue. Close the game and file users, check antivirus or PC-manager protection history, then retry.'
                Detail = [string]$ErrorRecord.Exception.Message
            }
        }
        $exception = $exception.InnerException
    }

    $message = [string]$ErrorRecord.Exception.Message
    if ($message -match '(?i)ConstrainedLanguage|language mode|application control|DotSourceNotSupported') {
        return [pscustomobject]@{
            Code = 'DTM-E1101'
            Chinese = 'PowerShell 受到语言模式或应用控制策略限制，无法安全运行安装器。请修复 Windows 安全策略后重试。'
            English = 'PowerShell is restricted by language mode or application-control policy and cannot run the installer safely. Repair the Windows security policy, then retry.'
            Detail = $message
        }
    }
    if ($message -match '(?i)access (?:is )?denied|access to the path .* denied|being used by another process|file target is occupied by a directory|local file .*locked|endpoint security') {
        return [pscustomobject]@{
            Code = 'DTM-E1102'
            Chinese = '本地文件被占用或被安全软件拦截；这通常不是 Windows 防火墙问题。请关闭游戏及占用程序，检查杀毒软件或电脑管家的隔离/防护记录后重试。'
            English = 'A local file is locked or blocked by endpoint security; this is usually not a Windows Firewall issue. Close the game and file users, check antivirus or PC-manager protection history, then retry.'
            Detail = $message
        }
    }
    if ($message -match '(?i)package|payload|manifest|managed DLL|assembly|SHA-?256|hash mismatch|binary version') {
        return [pscustomobject]@{
            Code = 'DTM-E1201'
            Chinese = '安装包内容、哈希、清单或托管 DLL 校验失败。请让 Steam 重新下载本项目后重试。'
            English = 'Package content, hashes, manifest, or managed DLL validation failed. Redownload this Workshop item through Steam, then retry.'
            Detail = $message
        }
    }

    return [pscustomobject]@{
        Code = 'DTM-E1999'
        Chinese = '安装器遇到未分类错误。请保留下面的完整错误并运行检查脚本。'
        English = 'The installer encountered an unclassified error. Keep the complete error below and run the status checker.'
        Detail = [string]$ErrorRecord.Exception.Message
    }
}

function Write-DtmApiInstallerFailure {
    param([Parameter(Mandatory = $true)] $ErrorRecord)

    $info = Get-DtmApiInstallerFailureInfo -ErrorRecord $ErrorRecord
    Write-DtmApiInstallerMessage -Code $info.Code -Chinese $info.Chinese -English $info.English -Detail $info.Detail -Level Error
}

function Get-DotNetExe {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $NoProvision
    )

    $localDotnet = Join-Path $RepoRoot '.tools\dotnet\dotnet.exe'
    if (Test-DtmApiDotNet8Toolchain -Path $localDotnet -RepoRoot $RepoRoot) {
        return (Resolve-Path -LiteralPath $localDotnet).Path
    }

    $systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($systemDotnet -and (Test-DtmApiDotNet8Toolchain -Path $systemDotnet.Source -RepoRoot $RepoRoot)) {
        return $systemDotnet.Source
    }

    if ($NoProvision) {
        throw 'No .NET host can execute the stable SDK selected by this workspace global.json with a .NET 8 runtime. Run tools/scripts/prepare-workspace.ps1 -Dependency DotNet.'
    }

    $sdkPolicyPath = Join-Path $RepoRoot 'global.json'
    $sdkPolicy = Get-Content -LiteralPath $sdkPolicyPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $sdkVersion = [string]$sdkPolicy.sdk.version
    if ($sdkVersion -notmatch '^8\.\d+\.\d+$') { throw "Unsupported .NET SDK policy: $sdkPolicyPath" }
    $toolsDir = Join-Path $RepoRoot '.tools'
    New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
    $installScript = Join-Path $toolsDir 'dotnet-install.ps1'
    if (-not (Test-Path $installScript)) {
        Invoke-DtmApiFileDownload -Uri 'https://dot.net/v1/dotnet-install.ps1' -DestinationPath $installScript | Out-Null
    }

    $powershellHost = Get-DtmApiPowerShellHost
    if ([string]::IsNullOrWhiteSpace($powershellHost)) {
        throw "PowerShell host was not found; cannot install local .NET SDK."
    }

    $installLog = Join-Path $toolsDir 'dotnet-install.log'
    & $powershellHost -NoProfile -ExecutionPolicy Bypass -File $installScript -Version $sdkVersion -InstallDir (Join-Path $toolsDir 'dotnet') *> $installLog
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install local .NET SDK. See $installLog"
    }
    if (-not (Test-DtmApiDotNet8Toolchain -Path $localDotnet -RepoRoot $RepoRoot)) {
        throw "The local .NET installation cannot execute the SDK selected by $sdkPolicyPath with a Microsoft.NETCore.App 8.x runtime. See $installLog"
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

function Assert-DtmApiRuntimeTransactionPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Actual,
        [Parameter(Mandatory = $true)] [string] $Expected,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $actualFull = [System.IO.Path]::GetFullPath($Actual)
    $expectedFull = [System.IO.Path]::GetFullPath($Expected)
    if (-not [string]::Equals($actualFull, $expectedFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Interrupted DTMAPI Runtime transaction has an unsafe $Label path. Expected=$expectedFull Actual=$actualFull"
    }
}

function Assert-DtmApiRuntimeTransactionOwnedPathSafe {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Boundary,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $pathFull = [System.IO.Path]::GetFullPath($Path)
    $boundaryFull = [System.IO.Path]::GetFullPath($Boundary)
    if (-not (Test-DtmApiPathIsSameOrChild -Child $pathFull -Parent $boundaryFull)) {
        throw "Interrupted DTMAPI Runtime transaction $Label escaped its validated boundary. Boundary=$boundaryFull Path=$pathFull"
    }

    $cursor = $pathFull
    while (Test-DtmApiPathIsSameOrChild -Child $cursor -Parent $boundaryFull) {
        # The caller owns the boundary itself (game/state root). Only the
        # transaction-owned path below it must be free of reparse points.
        if ([string]::Equals($cursor, $boundaryFull, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Interrupted DTMAPI Runtime transaction $Label contains a reparse point: $($item.FullName)"
            }
        }
        $parent = [System.IO.Path]::GetDirectoryName($cursor)
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $cursor, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $cursor = $parent
    }
}

function Test-DtmApiRuntimeTransactionStamp {
    param([Parameter(Mandatory = $true)] [string] $Stamp)

    return [regex]::IsMatch($Stamp, '^\d{8}-\d{6}-\d{3}-[0-9a-fA-F]{8}$')
}

function Read-DtmApiRuntimeTransactionReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $RuntimeTransactionRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir
    )

    $receiptPath = Join-Path $RuntimeTransactionRoot 'transaction.json'
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        throw "Interrupted DTMAPI Runtime transaction has no recovery receipt: $RuntimeTransactionRoot"
    }
    try {
        $receipt = Get-Content -Raw -Encoding UTF8 -LiteralPath $receiptPath | ConvertFrom-Json
    }
    catch {
        throw "Interrupted DTMAPI Runtime transaction receipt is unreadable: $receiptPath. $($_.Exception.Message)"
    }

    $requiredProperties = @(
        'SchemaVersion', 'GameDir', 'StateDir', 'PluginDir', 'Phase', 'RuntimeTransactionRoot', 'TransactionRoot',
        'CandidatePlugin', 'RecoveryPlugin', 'CandidateTools', 'CandidateReleaseManifest', 'CandidateInstallState',
        'RecoveryState', 'LiveTools', 'LiveReleaseManifest', 'LiveInstallState', 'OldPluginExisted', 'OldPluginMoved',
        'CandidatePluginPlaced', 'OldToolsExisted', 'OldToolsMoved', 'CandidateToolsPlaced',
        'OldReleaseManifestExisted', 'OldReleaseManifestMoved', 'CandidateReleaseManifestPlaced',
        'OldInstallStateExisted', 'OldInstallStateMoved', 'CandidateInstallStatePlaced', 'CommitSucceeded'
    )
    foreach ($propertyName in $requiredProperties) {
        if ($null -eq $receipt.PSObject.Properties[$propertyName]) {
            throw "Interrupted DTMAPI Runtime transaction receipt is missing ${propertyName}: $receiptPath"
        }
    }
    if ([int]$receipt.SchemaVersion -ne 1) {
        throw "Interrupted DTMAPI Runtime transaction receipt has unsupported schema $($receipt.SchemaVersion): $receiptPath"
    }

    $componentReceiptProperties = @(
        'CandidateComponents', 'LiveComponents', 'OldComponentsExisted', 'OldComponentsMoved', 'CandidateComponentsPlaced'
    )
    $componentReceiptPropertyCount = @($componentReceiptProperties | Where-Object { $null -ne $receipt.PSObject.Properties[$_] }).Count
    if ($componentReceiptPropertyCount -ne 0 -and $componentReceiptPropertyCount -ne $componentReceiptProperties.Count) {
        throw "Interrupted DTMAPI Runtime transaction receipt has an incomplete optional-component projection: $receiptPath"
    }
    $hasComponentReceipt = $componentReceiptPropertyCount -eq $componentReceiptProperties.Count

    $runtimeRootFull = [System.IO.Path]::GetFullPath($RuntimeTransactionRoot)
    $runtimeRootName = Split-Path -Leaf $runtimeRootFull
    $runtimePrefix = '.dtmapi-runtime-install-'
    if (-not $runtimeRootName.StartsWith($runtimePrefix, [System.StringComparison]::Ordinal) -or $runtimeRootName.Length -le $runtimePrefix.Length) {
        throw "Interrupted DTMAPI Runtime transaction directory has an invalid name: $runtimeRootFull"
    }
    $stamp = $runtimeRootName.Substring($runtimePrefix.Length)
    if (-not (Test-DtmApiRuntimeTransactionStamp -Stamp $stamp)) {
        throw "Interrupted DTMAPI Runtime transaction directory has an invalid stamp: $runtimeRootFull"
    }

    $gameFull = [System.IO.Path]::GetFullPath($GameDir)
    $stateFull = [System.IO.Path]::GetFullPath($StateDir)
    $pluginFull = [System.IO.Path]::GetFullPath($PluginDir)
    $expectedStateTransactionRoot = Join-Path $stateFull ('.runtime-install-transaction-' + $stamp)
    Assert-DtmApiRuntimeTransactionPath -Actual (Split-Path -Parent $runtimeRootFull) -Expected $gameFull -Label 'game-root parent'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.GameDir) -Expected $gameFull -Label 'game directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.StateDir) -Expected $stateFull -Label 'state directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.PluginDir) -Expected $pluginFull -Label 'live plugin directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RuntimeTransactionRoot) -Expected $runtimeRootFull -Label 'runtime transaction root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.TransactionRoot) -Expected $expectedStateTransactionRoot -Label 'state transaction root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidatePlugin) -Expected (Join-Path $runtimeRootFull 'candidate-plugin') -Label 'candidate plugin'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RecoveryPlugin) -Expected (Join-Path $runtimeRootFull 'recovery-plugin') -Label 'recovery plugin'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateTools) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\tools') -Label 'candidate tools'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateReleaseManifest) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\release-manifest.json') -Label 'candidate release manifest'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateInstallState) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\install-state.json') -Label 'candidate install state'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RecoveryState) -Expected (Join-Path $expectedStateTransactionRoot 'recovery') -Label 'state recovery root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveTools) -Expected (Join-Path $stateFull 'tools') -Label 'live tools'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveReleaseManifest) -Expected (Join-Path $stateFull 'release-manifest.json') -Label 'live release manifest'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveInstallState) -Expected (Join-Path $stateFull 'install-state.json') -Label 'live install state'
    if ($hasComponentReceipt) {
        Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateComponents) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\components') -Label 'candidate components'
        Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveComponents) -Expected (Join-Path $stateFull 'components') -Label 'live components'
    }

    foreach ($ownedPath in @(
        [pscustomobject]@{ Path = $runtimeRootFull; Boundary = $gameFull; Label = 'Runtime transaction root' },
        [pscustomobject]@{ Path = [string]$receipt.CandidatePlugin; Boundary = $runtimeRootFull; Label = 'candidate plugin path' },
        [pscustomobject]@{ Path = [string]$receipt.RecoveryPlugin; Boundary = $runtimeRootFull; Label = 'recovery plugin path' },
        [pscustomobject]@{ Path = [string]$receipt.TransactionRoot; Boundary = $stateFull; Label = 'state transaction root' },
        [pscustomobject]@{ Path = [string]$receipt.CandidateTools; Boundary = [string]$receipt.TransactionRoot; Label = 'candidate tools path' },
        [pscustomobject]@{ Path = $(if ($hasComponentReceipt) { [string]$receipt.CandidateComponents } else { Join-Path ([string]$receipt.TransactionRoot) 'candidate\components' }); Boundary = [string]$receipt.TransactionRoot; Label = 'candidate components path' },
        [pscustomobject]@{ Path = [string]$receipt.CandidateReleaseManifest; Boundary = [string]$receipt.TransactionRoot; Label = 'candidate release-manifest path' },
        [pscustomobject]@{ Path = [string]$receipt.CandidateInstallState; Boundary = [string]$receipt.TransactionRoot; Label = 'candidate install-state path' },
        [pscustomobject]@{ Path = [string]$receipt.RecoveryState; Boundary = [string]$receipt.TransactionRoot; Label = 'state recovery path' }
    )) {
        Assert-DtmApiRuntimeTransactionOwnedPathSafe -Path $ownedPath.Path -Boundary $ownedPath.Boundary -Label $ownedPath.Label
    }

    $rollbackSucceeded = $null
    if ($null -ne $receipt.PSObject.Properties['RollbackSucceeded'] -and $null -ne $receipt.RollbackSucceeded) {
        $rollbackSucceeded = [bool]$receipt.RollbackSucceeded
    }
    $transaction = [pscustomobject][ordered]@{
        Phase = [string]$receipt.Phase
        FailedPhase = if ($null -eq $receipt.PSObject.Properties['FailedPhase']) { '' } else { [string]$receipt.FailedPhase }
        RuntimeTransactionRoot = $runtimeRootFull
        ReceiptPath = [System.IO.Path]::GetFullPath($receiptPath)
        TransactionRoot = [System.IO.Path]::GetFullPath([string]$receipt.TransactionRoot)
        CandidatePlugin = [System.IO.Path]::GetFullPath([string]$receipt.CandidatePlugin)
        RecoveryPlugin = [System.IO.Path]::GetFullPath([string]$receipt.RecoveryPlugin)
        CandidateTools = [System.IO.Path]::GetFullPath([string]$receipt.CandidateTools)
        CandidateComponents = [System.IO.Path]::GetFullPath($(if ($hasComponentReceipt) { [string]$receipt.CandidateComponents } else { Join-Path ([string]$receipt.TransactionRoot) 'candidate\components' }))
        CandidateReleaseManifest = [System.IO.Path]::GetFullPath([string]$receipt.CandidateReleaseManifest)
        CandidateInstallState = [System.IO.Path]::GetFullPath([string]$receipt.CandidateInstallState)
        RecoveryState = [System.IO.Path]::GetFullPath([string]$receipt.RecoveryState)
        LiveTools = [System.IO.Path]::GetFullPath([string]$receipt.LiveTools)
        LiveComponents = [System.IO.Path]::GetFullPath($(if ($hasComponentReceipt) { [string]$receipt.LiveComponents } else { Join-Path $stateFull 'components' }))
        LiveReleaseManifest = [System.IO.Path]::GetFullPath([string]$receipt.LiveReleaseManifest)
        LiveInstallState = [System.IO.Path]::GetFullPath([string]$receipt.LiveInstallState)
        RuntimeMetadata = @()
        RuntimeFileRecords = @()
        StateToolRecords = @()
        OptionalComponentMetadata = @()
        OptionalComponentFileRecords = @()
        AssetRecord = $null
        OldPluginExisted = [bool]$receipt.OldPluginExisted
        OldPluginMoved = [bool]$receipt.OldPluginMoved
        CandidatePluginPlaced = [bool]$receipt.CandidatePluginPlaced
        OldToolsExisted = [bool]$receipt.OldToolsExisted
        OldToolsMoved = [bool]$receipt.OldToolsMoved
        CandidateToolsPlaced = [bool]$receipt.CandidateToolsPlaced
        OldComponentsExisted = if ($hasComponentReceipt) { [bool]$receipt.OldComponentsExisted } else { $false }
        OldComponentsMoved = if ($hasComponentReceipt) { [bool]$receipt.OldComponentsMoved } else { $false }
        CandidateComponentsPlaced = if ($hasComponentReceipt) { [bool]$receipt.CandidateComponentsPlaced } else { $false }
        OldReleaseManifestExisted = [bool]$receipt.OldReleaseManifestExisted
        OldReleaseManifestMoved = [bool]$receipt.OldReleaseManifestMoved
        CandidateReleaseManifestPlaced = [bool]$receipt.CandidateReleaseManifestPlaced
        OldInstallStateExisted = [bool]$receipt.OldInstallStateExisted
        OldInstallStateMoved = [bool]$receipt.OldInstallStateMoved
        CandidateInstallStatePlaced = [bool]$receipt.CandidateInstallStatePlaced
        CommitSucceeded = [bool]$receipt.CommitSucceeded
        RollbackSucceeded = $rollbackSucceeded
        RollbackFaultConsumed = $false
        ReceiptEstablished = $true
    }

    switch ([string]$transaction.Phase) {
        'MovingOldRuntime' { if (Test-Path -LiteralPath $transaction.RecoveryPlugin -PathType Container) { $transaction.OldPluginMoved = $true } }
        'PlacingCandidate' { if (-not (Test-Path -LiteralPath $transaction.CandidatePlugin -PathType Container) -and (Test-Path -LiteralPath $pluginFull -PathType Container)) { $transaction.CandidatePluginPlaced = $true } }
        'MovingOldTools' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'tools') -PathType Container) { $transaction.OldToolsMoved = $true } }
        'PlacingTools' { if (-not (Test-Path -LiteralPath $transaction.CandidateTools -PathType Container) -and (Test-Path -LiteralPath $transaction.LiveTools -PathType Container)) { $transaction.CandidateToolsPlaced = $true } }
        'MovingOldComponents' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'components') -PathType Container) { $transaction.OldComponentsMoved = $true } }
        'PlacingComponents' { if (-not (Test-Path -LiteralPath $transaction.CandidateComponents -PathType Container) -and (Test-Path -LiteralPath $transaction.LiveComponents -PathType Container)) { $transaction.CandidateComponentsPlaced = $true } }
        'MovingOldReleaseManifest' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'release-manifest.json') -PathType Leaf) { $transaction.OldReleaseManifestMoved = $true } }
        'PlacingReleaseManifest' { if (-not (Test-Path -LiteralPath $transaction.CandidateReleaseManifest -PathType Leaf) -and (Test-Path -LiteralPath $transaction.LiveReleaseManifest -PathType Leaf)) { $transaction.CandidateReleaseManifestPlaced = $true } }
        'MovingOldInstallState' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'install-state.json') -PathType Leaf) { $transaction.OldInstallStateMoved = $true } }
        'PlacingInstallState' { if (-not (Test-Path -LiteralPath $transaction.CandidateInstallState -PathType Leaf) -and (Test-Path -LiteralPath $transaction.LiveInstallState -PathType Leaf)) { $transaction.CandidateInstallStatePlaced = $true } }
    }
    return $transaction
}

function Get-DtmApiRuntimeTransactionClassifications {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir
    )

    $gameFull = [System.IO.Path]::GetFullPath($GameDir)
    $stateFull = [System.IO.Path]::GetFullPath($StateDir)
    $pluginFull = [System.IO.Path]::GetFullPath($PluginDir)
    $runtimePrefix = '.dtmapi-runtime-install-'
    $statePrefix = '.runtime-install-transaction-'
    $runtimeItems = @(Get-ChildItem -LiteralPath $gameFull -Force -Filter ($runtimePrefix + '*') -ErrorAction Stop | Sort-Object Name)
    $stateItems = if (Test-Path -LiteralPath $stateFull -PathType Container) {
        @(Get-ChildItem -LiteralPath $stateFull -Force -Filter ($statePrefix + '*') -ErrorAction Stop | Sort-Object Name)
    }
    else {
        @()
    }
    $stateByStamp = @{}
    foreach ($stateItem in $stateItems) {
        $stateName = [string]$stateItem.Name
        if ($stateName.StartsWith($statePrefix, [System.StringComparison]::Ordinal) -and $stateName.Length -gt $statePrefix.Length) {
            $stateByStamp[$stateName.Substring($statePrefix.Length).ToUpperInvariant()] = $stateItem
        }
    }

    $results = New-Object System.Collections.Generic.List[object]
    $pairedStateStamps = @{}
    foreach ($runtimeItem in $runtimeItems) {
        $runtimePath = [System.IO.Path]::GetFullPath($runtimeItem.FullName)
        $runtimeName = [string]$runtimeItem.Name
        $stamp = if ($runtimeName.StartsWith($runtimePrefix, [System.StringComparison]::Ordinal) -and $runtimeName.Length -gt $runtimePrefix.Length) {
            $runtimeName.Substring($runtimePrefix.Length)
        }
        else {
            ''
        }
        $stampKey = $stamp.ToUpperInvariant()
        $stateItem = if ($stateByStamp.ContainsKey($stampKey)) { $stateByStamp[$stampKey] } else { $null }
        if ($null -ne $stateItem) { $pairedStateStamps[$stampKey] = $true }
        $receiptPath = Join-Path $runtimePath 'transaction.json'

        if (-not $runtimeItem.PSIsContainer -or -not (Test-DtmApiRuntimeTransactionStamp -Stamp $stamp)) {
            $results.Add([pscustomobject]@{
                Kind = 'UnsafeNoReceipt'; Stamp = $stamp; RuntimeRoot = $runtimePath; StateRoot = if ($null -eq $stateItem) { '' } else { [string]$stateItem.FullName }
                ReceiptPath = $receiptPath; ReasonCode = 'invalid-runtime-root'; Detail = 'The Runtime transaction item is not an exact supported transaction directory.'; Transaction = $null
            }) | Out-Null
            continue
        }

        if (Test-Path -LiteralPath $receiptPath -PathType Leaf) {
            try {
                $transaction = Read-DtmApiRuntimeTransactionReceipt -RuntimeTransactionRoot $runtimePath -GameDir $gameFull -StateDir $stateFull -PluginDir $pluginFull
                $results.Add([pscustomobject]@{
                    Kind = 'RecoverableReceipt'; Stamp = $stamp; RuntimeRoot = $runtimePath; StateRoot = [string]$transaction.TransactionRoot
                    ReceiptPath = $receiptPath; ReasonCode = 'validated-receipt'; Detail = ''; Transaction = $transaction
                }) | Out-Null
            }
            catch {
                $results.Add([pscustomobject]@{
                    Kind = 'InvalidReceipt'; Stamp = $stamp; RuntimeRoot = $runtimePath; StateRoot = if ($null -eq $stateItem) { '' } else { [string]$stateItem.FullName }
                    ReceiptPath = $receiptPath; ReasonCode = 'invalid-receipt'; Detail = [string]$_.Exception.Message; Transaction = $null
                }) | Out-Null
            }
            continue
        }

        $sterile = $null -eq $stateItem
        $sterileReason = if ($sterile) { '' } else { 'A matching state transaction exists.' }
        if (($runtimeItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            $sterile = $false
            $sterileReason = 'The Runtime transaction root is a reparse point.'
        }
        if ($sterile) {
            try {
                foreach ($entry in @(Get-ChildItem -LiteralPath $runtimePath -Force -ErrorAction Stop)) {
                    $isReparse = ($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
                    $isKnownReceiptTemp = -not $entry.PSIsContainer -and
                        [regex]::IsMatch([string]$entry.Name, '^transaction\.json\.(tmp|bak)-[0-9a-fA-F]{32}$')
                    if ($isReparse -or -not $isKnownReceiptTemp) {
                        $sterile = $false
                        $sterileReason = "Unknown or unsafe receiptless entry: $($entry.FullName)"
                        break
                    }
                }
            }
            catch {
                $sterile = $false
                $sterileReason = "Could not enumerate the receiptless transaction root: $($_.Exception.Message)"
            }
        }
        $results.Add([pscustomobject]@{
            Kind = if ($sterile) { 'SterileNoReceipt' } else { 'UnsafeNoReceipt' }
            Stamp = $stamp
            RuntimeRoot = $runtimePath
            StateRoot = if ($null -eq $stateItem) { '' } else { [System.IO.Path]::GetFullPath($stateItem.FullName) }
            ReceiptPath = $receiptPath
            ReasonCode = if ($sterile) { 'sterile-pre-receipt' } else { 'unsafe-receiptless' }
            Detail = $sterileReason
            Transaction = $null
        }) | Out-Null
    }

    foreach ($stateItem in $stateItems) {
        $stateName = [string]$stateItem.Name
        $stamp = if ($stateName.StartsWith($statePrefix, [System.StringComparison]::Ordinal) -and $stateName.Length -gt $statePrefix.Length) {
            $stateName.Substring($statePrefix.Length)
        }
        else {
            ''
        }
        if (-not $pairedStateStamps.ContainsKey($stamp.ToUpperInvariant())) {
            $results.Add([pscustomobject]@{
                Kind = 'OrphanState'; Stamp = $stamp; RuntimeRoot = ''; StateRoot = [System.IO.Path]::GetFullPath($stateItem.FullName)
                ReceiptPath = ''; ReasonCode = 'orphan-state'; Detail = 'State recovery data has no matching Runtime transaction root.'; Transaction = $null
            }) | Out-Null
        }
    }

    return @($results.ToArray())
}

function Remove-DtmApiSterileRuntimeTransactionRoot {
    param(
        [Parameter(Mandatory = $true)] $Classification,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir
    )

    $runtimeRoot = [System.IO.Path]::GetFullPath([string]$Classification.RuntimeRoot)
    $fresh = @(Get-DtmApiRuntimeTransactionClassifications -GameDir $GameDir -StateDir $StateDir -PluginDir $PluginDir |
        Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.RuntimeRoot) -and [string]::Equals([System.IO.Path]::GetFullPath([string]$_.RuntimeRoot), $runtimeRoot, [System.StringComparison]::OrdinalIgnoreCase) })
    if ($fresh.Count -ne 1 -or -not [string]::Equals([string]$fresh[0].Kind, 'SterileNoReceipt', [System.StringComparison]::Ordinal)) {
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1303' `
            -Chinese '无凭据事务残留在清理前发生变化，已拒绝删除。' `
            -English 'The receiptless transaction residue changed before cleanup; deletion was refused.' `
            -Detail $runtimeRoot
    }

    try {
        foreach ($entry in @(Get-ChildItem -LiteralPath $runtimeRoot -Force -ErrorAction Stop)) {
            [System.IO.File]::Delete([System.IO.Path]::GetFullPath($entry.FullName))
        }
        [System.IO.Directory]::Delete($runtimeRoot, $false)
    }
    catch {
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1102' `
            -Chinese '安全的事务空壳仍被占用或被安全软件拦截，无法清理。' `
            -English 'The safe transaction shell is still locked or blocked by endpoint security and could not be removed.' `
            -Detail "$runtimeRoot; $($_.Exception.Message)" `
            -InnerException $_.Exception
    }
}

function Get-DtmApiFileSha256 {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $stream = [System.IO.File]::Open(
        [System.IO.Path]::GetFullPath($Path),
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha256.ComputeHash($stream)
            return ([System.BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
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

function Initialize-DtmApiZipRuntime {
    $compressionAssembly = @([System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {
        [string]::Equals($_.GetName().Name, 'System.IO.Compression', [System.StringComparison]::OrdinalIgnoreCase)
    } | Select-Object -First 1)
    if ($compressionAssembly.Count -ne 1) {
        try {
            $loadedAssembly = [System.Reflection.Assembly]::Load('System.IO.Compression')
        }
        catch {
            $loadedAssembly = [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression')
        }
        if ($null -ne $loadedAssembly) {
            $compressionAssembly = @($loadedAssembly)
        }
    }

    if ($compressionAssembly.Count -ne 1) {
        throw 'The .NET ZIP runtime assembly is unavailable on this PowerShell host.'
    }

    $archiveType = $compressionAssembly[0].GetType('System.IO.Compression.ZipArchive', $false)
    $modeType = $compressionAssembly[0].GetType('System.IO.Compression.ZipArchiveMode', $false)
    if ($null -eq $archiveType -or $null -eq $modeType) {
        throw 'The .NET ZIP runtime is unavailable on this PowerShell host.'
    }

    $constructor = $archiveType.GetConstructor(@([System.IO.Stream], $modeType, [bool]))
    if ($null -eq $constructor) {
        throw 'The .NET ZIP reader constructor is unavailable on this PowerShell host.'
    }

    return [pscustomobject]@{
        ArchiveType = $archiveType
        ModeType = $modeType
        Constructor = $constructor
    }
}

function Test-DtmApiZipArchiveSupport {
    try {
        [void](Initialize-DtmApiZipRuntime)
        return $true
    }
    catch {
        return $false
    }
}

function Expand-DtmApiZipArchive {
    param(
        [Parameter(Mandatory = $true)] [string] $LiteralPath,
        [Parameter(Mandatory = $true)] [string] $DestinationPath
    )

    $sourceFull = [System.IO.Path]::GetFullPath($LiteralPath)
    if (-not (Test-Path -LiteralPath $sourceFull -PathType Leaf)) {
        throw "ZIP archive is missing: $sourceFull"
    }

    $destinationFull = [System.IO.Path]::GetFullPath($DestinationPath)
    [void][System.IO.Directory]::CreateDirectory($destinationFull)
    $zipRuntime = Initialize-DtmApiZipRuntime
    $readMode = [System.Enum]::Parse($zipRuntime.ModeType, 'Read')
    $stream = [System.IO.File]::Open(
        $sourceFull,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    $archive = $null
    try {
        $archive = $zipRuntime.Constructor.Invoke([object[]]@($stream, $readMode, $false))
        foreach ($entry in @($archive.Entries)) {
            $entryName = ([string]$entry.FullName).Replace('/', [System.IO.Path]::DirectorySeparatorChar).Replace('\', [System.IO.Path]::DirectorySeparatorChar)
            if ([string]::IsNullOrWhiteSpace($entryName)) {
                continue
            }

            $targetPath = [System.IO.Path]::GetFullPath((Join-Path $destinationFull $entryName))
            if (-not (Test-DtmApiPathIsSameOrChild -Child $targetPath -Parent $destinationFull)) {
                throw "ZIP archive contains an entry outside the destination: $($entry.FullName)"
            }

            if ([string]::IsNullOrEmpty([string]$entry.Name)) {
                [void][System.IO.Directory]::CreateDirectory($targetPath)
                continue
            }

            $targetParent = [System.IO.Path]::GetDirectoryName($targetPath)
            if (-not [string]::IsNullOrWhiteSpace($targetParent)) {
                [void][System.IO.Directory]::CreateDirectory($targetParent)
            }

            $entryStream = $entry.Open()
            try {
                $outputStream = [System.IO.File]::Open(
                    $targetPath,
                    [System.IO.FileMode]::Create,
                    [System.IO.FileAccess]::Write,
                    [System.IO.FileShare]::None)
                try {
                    $entryStream.CopyTo($outputStream)
                }
                finally {
                    $outputStream.Dispose()
                }
            }
            finally {
                $entryStream.Dispose()
            }
        }
    }
    finally {
        if ($null -ne $archive) {
            $archive.Dispose()
        }
        $stream.Dispose()
    }

    return $destinationFull
}

function Invoke-DtmApiFileDownload {
    param(
        [Parameter(Mandatory = $true)] [string] $Uri,
        [Parameter(Mandatory = $true)] [string] $DestinationPath
    )

    $destinationFull = [System.IO.Path]::GetFullPath($DestinationPath)
    $destinationParent = [System.IO.Path]::GetDirectoryName($destinationFull)
    if (-not [string]::IsNullOrWhiteSpace($destinationParent)) {
        [void][System.IO.Directory]::CreateDirectory($destinationParent)
    }

    $originalProtocol = [System.Net.ServicePointManager]::SecurityProtocol
    $client = $null
    try {
        try {
            $tls12 = [System.Enum]::Parse([System.Net.SecurityProtocolType], 'Tls12')
            [System.Net.ServicePointManager]::SecurityProtocol = $originalProtocol -bor $tls12
        }
        catch {
        }

        $client = New-Object System.Net.WebClient
        $client.DownloadFile($Uri, $destinationFull)
    }
    finally {
        if ($null -ne $client) {
            $client.Dispose()
        }
        [System.Net.ServicePointManager]::SecurityProtocol = $originalProtocol
    }

    return $destinationFull
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

function Enter-DtmApiInstallerMutationLock {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $Operation
    )

    $gameIdentity = [System.IO.Path]::GetFullPath($GameDir).TrimEnd('\', '/').ToUpperInvariant()
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $identityBytes = [System.Text.Encoding]::UTF8.GetBytes($gameIdentity)
        $digest = $sha.ComputeHash($identityBytes)
    }
    finally {
        $sha.Dispose()
    }
    $identityHash = -join @($digest | ForEach-Object { $_.ToString('x2') })
    $mutexName = 'Local\DTMAPI.RuntimeInstaller.' + $identityHash
    $mutex = New-Object System.Threading.Mutex($false, $mutexName)
    $acquired = $false
    try {
        try {
            $acquired = $mutex.WaitOne(0)
        }
        catch [System.Threading.AbandonedMutexException] {
            $acquired = $true
        }
        if (-not $acquired) {
            Throw-DtmApiInstallerError `
                -Code 'DTM-E1302' `
                -Chinese '同一游戏目录已有另一个 DTMAPI 安装或卸载正在运行，请等待其结束后重试。' `
                -English 'Another DTMAPI install or uninstall is already running for this game directory. Wait for it to finish, then retry.' `
                -Detail "GameDir=$gameIdentity"
        }
        return [pscustomobject]@{
            Mutex = $mutex
            Name = $mutexName
            GameDir = $gameIdentity
            Operation = $Operation
            Acquired = $true
        }
    }
    catch {
        if (-not $acquired) {
            $mutex.Dispose()
        }
        throw
    }
}

function Exit-DtmApiInstallerMutationLock {
    param($Lock)

    if ($null -eq $Lock -or -not [bool]$Lock.Acquired) {
        return
    }
    try {
        $Lock.Mutex.ReleaseMutex()
    }
    finally {
        $Lock.Mutex.Dispose()
        $Lock.Acquired = $false
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
