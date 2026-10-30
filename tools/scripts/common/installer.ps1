# Shared installer helpers. Definitions only; PowerShell 5.1 compatible.
function Get-DtmApiOperationLogRoots {
    $local = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    if ($local) { Join-Path $local 'DTMAPI/InstallerLogs' }
    Join-Path ([IO.Path]::GetTempPath()) 'DTMAPI/InstallerLogs'
}

function Start-DtmApiOperationLog {
    param([string] $GameDir)
    foreach ($root in @(Get-DtmApiOperationLogRoots | Select-Object -Unique)) {
        try {
            [void][IO.Directory]::CreateDirectory($root)
            $path = Join-Path $root ('uninstall-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8) + '.log')
            $stream = New-Object IO.FileStream($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
            $writer = New-Object IO.StreamWriter($stream, (New-Object Text.UTF8Encoding($false)))
            $writer.AutoFlush = $true
            $log = [pscustomobject]@{Path=$path; Writer=$writer}
            Write-DtmApiOperationLog -Log $log -Message "GameDir=$GameDir"
            Write-Host "Log=$path"
            return $log
        } catch { Write-Warning "Log location unavailable: $root | $($_.Exception.Message)" }
    }
    throw 'Cannot create the uninstall log in local application data or the temporary directory. No game files were changed.'
}

function Write-DtmApiOperationLog {
    param($Log, [string] $Message)
    if ($null -ne $Log) { $Log.Writer.WriteLine([DateTime]::UtcNow.ToString('O') + ' ' + $Message) }
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

function Test-DtmApiInstallPathPresent {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [ValidateSet('Any', 'Container', 'Leaf')] [string] $PathType = 'Any'
    )
    try {
        $entry = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
        if ($PathType -eq 'Container') { return [bool]$entry.PSIsContainer }
        if ($PathType -eq 'Leaf') { return (-not $entry.PSIsContainer -and $entry.Length -gt 0) }
        return $true
    }
    catch { return $false }
}

function Get-DtmApiBepInExInstallState {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $required = @(Get-DtmApiBepInExRequiredInstallFiles -GameDir $GameDir)
    $missing = @($required | Where-Object { -not (Test-DtmApiInstallPathPresent -Path $_.Path -PathType $_.PathType) })
    $doorstop = Join-Path $GameDir 'doorstop_config.ini'
    $doorstopEnabled = Test-DtmApiDoorstopConfigEnabledForBepInEx -Path $doorstop
    return [pscustomobject]@{
        RequiredFiles = $required
        MissingFiles = $missing
        DoorstopConfigPresent = (Test-Path -LiteralPath $doorstop -PathType Leaf)
        DoorstopEnabled = $doorstopEnabled
        Complete = ($missing.Count -eq 0 -and $doorstopEnabled)
    }
}

function Test-DtmApiBepInExInstallComplete {
    param([Parameter(Mandatory = $true)] [string] $GameDir)
    return (Get-DtmApiBepInExInstallState -GameDir $GameDir).Complete
}
