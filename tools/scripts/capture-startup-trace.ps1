param(
    [ValidateRange(5, 180)] [int] $CaptureSeconds = 45,
    [ValidateRange(5, 180)] [int] $LaunchTimeoutSeconds = 60,
    [string] $OutputRoot = '',
    [string] $ProcmonPath = '',
    [string] $TestLaunchExecutable = '',
    [string] $TestLaunchArgumentLine = '',
    [switch] $SkipElevation,
    [switch] $SkipConsent,
    [switch] $SkipLogCollection,
    [switch] $Elevated
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

$script:DtmProcmonOwned = $false
$script:DtmProcmonExe = ''
$script:DtmCaptureDirectory = ''
$script:DtmCaptureZip = ''
$script:DtmCaptureWarnings = New-Object 'System.Collections.Generic.List[string]'

function Test-DtmAdministrator {
    try {
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }
    catch {
        return $false
    }
}

function ConvertTo-DtmQuotedArgument {
    param([Parameter(Mandatory = $true)] [string] $Value)

    return '"' + $Value.Replace('"', '\"') + '"'
}

function Add-DtmCaptureWarning {
    param([Parameter(Mandatory = $true)] [string] $Message)

    $script:DtmCaptureWarnings.Add($Message) | Out-Null
    Write-Warning $Message
}

function Get-DtmFileSnapshot {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{
            Path = $Path
            Exists = $false
            Length = $null
            LastWriteTimeUtc = $null
            Sha256 = $null
        }
    }

    try {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        return [ordered]@{
            Path = $item.FullName
            Exists = $true
            Length = $item.Length
            LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
            Sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
        }
    }
    catch {
        Add-DtmCaptureWarning -Message "Could not snapshot '$Path': $($_.Exception.Message)"
        return [ordered]@{
            Path = $Path
            Exists = $true
            Length = $null
            LastWriteTimeUtc = $null
            Sha256 = $null
        }
    }
}

function Test-DtmSnapshotFresh {
    param(
        $Snapshot,
        [Parameter(Mandatory = $true)] [datetime] $StartedUtc
    )

    if (-not $Snapshot.Exists -or [string]::IsNullOrWhiteSpace([string]$Snapshot.LastWriteTimeUtc)) {
        return $false
    }

    try {
        return ([datetime]::Parse([string]$Snapshot.LastWriteTimeUtc).ToUniversalTime() -ge $StartedUtc.AddSeconds(-2))
    }
    catch {
        return $false
    }
}

function Test-DtmMicrosoftProcmon {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    try {
        $signature = Get-AuthenticodeSignature -LiteralPath $Path
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            return $false
        }
        if ($null -eq $signature.SignerCertificate) {
            return $false
        }
        if (([string]$signature.SignerCertificate.Subject) -notmatch '(^|,\s*)O=Microsoft Corporation(,|$)') {
            return $false
        }

        $versionInfo = (Get-Item -LiteralPath $Path -ErrorAction Stop).VersionInfo
        return ([string]$versionInfo.ProductName -eq 'Sysinternals Procmon' -and
            [string]$versionInfo.FileDescription -eq 'Process Monitor')
    }
    catch {
        return $false
    }
}

function Resolve-DtmProcmon {
    param(
        [string] $RequestedPath,
        [Parameter(Mandatory = $true)] [string] $Stamp
    )

    $candidates = New-Object 'System.Collections.Generic.List[string]'
    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $candidates.Add([System.IO.Path]::GetFullPath($RequestedPath)) | Out-Null
    }

    $toolRoot = Join-Path $env:LOCALAPPDATA 'DTMAPI\tools\ProcessMonitor'
    $cachedExe = Join-Path $toolRoot 'Procmon64.exe'
    $candidates.Add($cachedExe) | Out-Null

    $command = Get-Command 'Procmon64.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command -and -not [string]::IsNullOrWhiteSpace([string]$command.Source)) {
        $candidates.Add([string]$command.Source) | Out-Null
    }

    foreach ($candidate in $candidates) {
        if (Test-DtmMicrosoftProcmon -Path $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        throw "指定的 Process Monitor 不存在，或未通过微软签名与产品身份验证：$RequestedPath"
    }

    Write-Host '[2/7] 正在下载微软 Sysinternals Process Monitor...'
    New-Item -ItemType Directory -Force -Path $toolRoot | Out-Null
    $downloadDir = Join-Path $toolRoot ("download-$Stamp")
    New-Item -ItemType Directory -Force -Path $downloadDir | Out-Null
    $downloadZip = Join-Path $downloadDir 'ProcessMonitor.zip'
    $downloadUri = 'https://download.sysinternals.com/files/ProcessMonitor.zip'

    try {
        Invoke-WebRequest -UseBasicParsing -Uri $downloadUri -OutFile $downloadZip
        Expand-Archive -LiteralPath $downloadZip -DestinationPath $downloadDir -Force
    }
    catch {
        throw "无法下载或解压微软 Process Monitor。请检查网络后重新运行本 BAT。地址：$downloadUri。错误：$($_.Exception.Message)"
    }

    $downloadedExe = Join-Path $downloadDir 'Procmon64.exe'
    if (-not (Test-DtmMicrosoftProcmon -Path $downloadedExe)) {
        throw "下载的 Process Monitor 未通过微软签名与产品身份验证：$downloadedExe"
    }

    Copy-Item -LiteralPath $downloadedExe -Destination $cachedExe -Force
    if (-not (Test-DtmMicrosoftProcmon -Path $cachedExe)) {
        throw "缓存的 Process Monitor 未通过微软签名与产品身份验证：$cachedExe"
    }

    return $cachedExe
}

function Stop-DtmOwnedProcmon {
    if (-not $script:DtmProcmonOwned -or [string]::IsNullOrWhiteSpace($script:DtmProcmonExe)) {
        return
    }

    try {
        & $script:DtmProcmonExe /Terminate /Quiet | Out-Null
    }
    catch {
        Add-DtmCaptureWarning -Message "Process Monitor terminate command failed: $($_.Exception.Message)"
    }

    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        $remaining = @(Get-Process -Name 'Procmon', 'Procmon64', 'Procmon64a' -ErrorAction SilentlyContinue)
        if ($remaining.Count -eq 0) {
            $script:DtmProcmonOwned = $false
            return
        }
        Start-Sleep -Milliseconds 250
    }

    Add-DtmCaptureWarning -Message 'Process Monitor did not exit within 15 seconds after /Terminate.'
}

function Wait-DtmFileReady {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [int] $TimeoutSeconds = 20
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastLength = -1L
    $stableCount = 0
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            try {
                $length = (Get-Item -LiteralPath $Path).Length
                if ($length -gt 0 -and $length -eq $lastLength) {
                    $stableCount++
                    if ($stableCount -ge 2) {
                        return $true
                    }
                }
                else {
                    $stableCount = 0
                    $lastLength = $length
                }
            }
            catch {
            }
        }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

function Get-DtmGameProcess {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    foreach ($process in @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
        try {
            if (-not [string]::IsNullOrWhiteSpace([string]$process.Path) -and
                (Test-DtmApiPathIsSameOrChild -Child $process.Path -Parent $GameDir)) {
                return $process
            }
        }
        catch {
        }
    }
    return $null
}

function Stop-DtmCapturedGame {
    param(
        $Process,
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    $result = [ordered]@{
        Status = 'NotObserved'
        ProcessId = if ($Process) { $Process.Id } else { $null }
        GracefulCloseRequested = $false
        Forced = $false
        Exited = $false
    }
    if (-not $Process) {
        return [pscustomobject]$result
    }

    $current = Get-Process -Id $Process.Id -ErrorAction SilentlyContinue
    if (-not $current) {
        $result.Status = 'AlreadyExited'
        $result.Exited = $true
        return [pscustomobject]$result
    }

    try {
        if ([string]::IsNullOrWhiteSpace([string]$current.Path) -or
            -not (Test-DtmApiPathIsSameOrChild -Child $current.Path -Parent $GameDir)) {
            $result.Status = 'RefusedPathMismatch'
            Add-DtmCaptureWarning -Message "Refusing to stop PID $($current.Id) because its executable is outside the selected game directory."
            return [pscustomobject]$result
        }
    }
    catch {
        $result.Status = 'RefusedPathUnavailable'
        Add-DtmCaptureWarning -Message "Refusing to stop PID $($current.Id) because its executable path could not be verified: $($_.Exception.Message)"
        return [pscustomobject]$result
    }

    Write-Host "[INFO] 抓取与日志收集已完成，正在关闭本次由探针启动的游戏（PID $($current.Id)）..."
    try {
        if ($current.MainWindowHandle -ne [IntPtr]::Zero) {
            $result.GracefulCloseRequested = [bool]$current.CloseMainWindow()
        }
    }
    catch {
        Add-DtmCaptureWarning -Message "Graceful game close request failed: $($_.Exception.Message)"
    }

    if ($result.GracefulCloseRequested) {
        $deadline = (Get-Date).AddSeconds(10)
        while ((Get-Date) -lt $deadline -and (Get-Process -Id $current.Id -ErrorAction SilentlyContinue)) {
            Start-Sleep -Milliseconds 250
        }
    }

    if (Get-Process -Id $current.Id -ErrorAction SilentlyContinue) {
        try {
            Stop-Process -Id $current.Id -Force -ErrorAction Stop
            $result.Forced = $true
        }
        catch {
            $result.Status = 'StopFailed'
            Add-DtmCaptureWarning -Message "Could not stop captured game PID $($current.Id): $($_.Exception.Message)"
            return [pscustomobject]$result
        }
    }

    $deadline = (Get-Date).AddSeconds(10)
    while ((Get-Date) -lt $deadline -and (Get-Process -Id $current.Id -ErrorAction SilentlyContinue)) {
        Start-Sleep -Milliseconds 250
    }
    $result.Exited = -not [bool](Get-Process -Id $current.Id -ErrorAction SilentlyContinue)
    $result.Status = if (-not $result.Exited) { 'StillRunning' } elseif ($result.Forced) { 'ForcedExit' } else { 'GracefulExit' }
    if (-not $result.Exited) {
        Add-DtmCaptureWarning -Message "Captured game PID $($current.Id) is still running after the close timeout."
    }
    return [pscustomobject]$result
}

function Protect-DtmDiagnosticText {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }
    $protected = $Value
    if (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
        $protected = [regex]::Replace($protected, [regex]::Escape($env:USERPROFILE), '%USERPROFILE%', 'IgnoreCase')
    }
    $protected = [regex]::Replace(
        $protected,
        '(?i)((?:--?|/)[^\s=]*(?:token|password|passwd|secret|api[-_]?key)[^\s=]*[=\s]+)("[^"]*"|\S+)',
        '$1[REDACTED]'
    )
    if ($protected.Length -gt 4000) {
        return $protected.Substring(0, 4000) + '...[truncated]'
    }
    return $protected
}

function Get-DtmLoaderEnvironmentContext {
    $knownNames = @(
        'DOORSTOP_DISABLE',
        'DOORSTOP_INVOKE_DLL_PATH',
        'DOORSTOP_MONO_DLL_PATH',
        'DOORSTOP_CLR_RUNTIME_CORECLR_PATH',
        'DOORSTOP_CORLIB_OVERRIDE_PATH',
        'DOORSTOP_MONO_DEBUG_ENABLED',
        'BEPINEX_CONFIG_PATH',
        'BEPINEX_ROOT_PATH',
        'MONO_PATH',
        'MONO_ENV_OPTIONS',
        'MONO_LOG_LEVEL',
        'MONO_LOG_MASK'
    )
    $scopes = New-Object 'System.Collections.Generic.List[object]'
    foreach ($scopeName in @('Process', 'User', 'Machine')) {
        try {
            $target = switch ($scopeName) {
                'User' { [EnvironmentVariableTarget]::User }
                'Machine' { [EnvironmentVariableTarget]::Machine }
                default { [EnvironmentVariableTarget]::Process }
            }
            $raw = [Environment]::GetEnvironmentVariables($target)
            $names = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
            foreach ($name in $knownNames) { $names.Add($name) | Out-Null }
            foreach ($name in @($raw.Keys)) {
                if ([string]$name -match '^(?i)(DOORSTOP_|BEPINEX_)') {
                    $names.Add([string]$name) | Out-Null
                }
            }
            $variables = @($names | Sort-Object | ForEach-Object {
                $name = [string]$_
                $present = $raw.Contains($name)
                $value = if ($present) { [string]$raw[$name] } else { '' }
                if ($name -match '(?i)(TOKEN|PASSWORD|PASSWD|SECRET|KEY)') {
                    $value = if ($present) { '[REDACTED]' } else { '' }
                }
                [ordered]@{ Name = $name; Present = $present; Value = $value }
            })
            $scopes.Add([ordered]@{
                Scope = if ($scopeName -eq 'Process') { 'ProbeProcess (not Steam/DolocTown)' } else { $scopeName }
                Variables = $variables
                Error = ''
            }) | Out-Null
        }
        catch {
            $scopes.Add([ordered]@{ Scope = $scopeName; Variables = @(); Error = $_.Exception.Message }) | Out-Null
        }
    }
    return @($scopes.ToArray())
}

function Get-DtmRegistryValueMap {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [ordered]@{ Path = $Path; Exists = $false; Values = @{}; Error = '' }
    }
    try {
        $item = Get-ItemProperty -LiteralPath $Path -ErrorAction Stop
        $values = [ordered]@{}
        foreach ($property in $item.PSObject.Properties) {
            if ($property.Name -notmatch '^PS') {
                $values[$property.Name] = [string]$property.Value
            }
        }
        return [ordered]@{ Path = $Path; Exists = $true; Values = $values; Error = '' }
    }
    catch {
        return [ordered]@{ Path = $Path; Exists = $true; Values = @{}; Error = $_.Exception.Message }
    }
}

function Get-DtmLaunchContext {
    param(
        [Parameter(Mandatory = $true)] [int] $ProcessId,
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    $chain = New-Object 'System.Collections.Generic.List[object]'
    $errors = New-Object 'System.Collections.Generic.List[string]'
    $seen = New-Object 'System.Collections.Generic.HashSet[int]'
    $currentId = $ProcessId
    for ($depth = 0; $depth -lt 6 -and $currentId -gt 0 -and $seen.Add($currentId); $depth++) {
        try {
            $entry = Get-CimInstance -ClassName Win32_Process -Filter ("ProcessId = $currentId") -ErrorAction Stop | Select-Object -First 1
            if (-not $entry) { break }
            $chain.Add([ordered]@{
                Depth = $depth
                ProcessId = [int]$entry.ProcessId
                ParentProcessId = [int]$entry.ParentProcessId
                Name = [string]$entry.Name
                ExecutablePath = [string]$entry.ExecutablePath
                CommandLine = Protect-DtmDiagnosticText -Value ([string]$entry.CommandLine)
                CreationDate = if ($entry.CreationDate) { ([datetime]$entry.CreationDate).ToString('o') } else { '' }
            }) | Out-Null
            $currentId = [int]$entry.ParentProcessId
        }
        catch {
            $errors.Add("PID ${currentId}: $($_.Exception.Message)") | Out-Null
            break
        }
    }

    $gameExe = Join-Path $GameDir 'DolocTown.exe'
    $ifeo = @(
        (Get-DtmRegistryValueMap -Path 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DolocTown.exe'),
        (Get-DtmRegistryValueMap -Path 'HKLM:\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DolocTown.exe'),
        (Get-DtmRegistryValueMap -Path 'HKLM:\Software\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DolocTown.exe')
    )
    $compatibility = New-Object 'System.Collections.Generic.List[object]'
    foreach ($scope in @('HKCU:', 'HKLM:')) {
        $path = "$scope\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"
        $value = ''
        $errorMessage = ''
        try {
            if (Test-Path -LiteralPath $path) {
                $item = Get-ItemProperty -LiteralPath $path -ErrorAction Stop
                $property = $item.PSObject.Properties | Where-Object { $_.Name -ieq $gameExe } | Select-Object -First 1
                if ($property) { $value = [string]$property.Value }
            }
        }
        catch { $errorMessage = $_.Exception.Message }
        $compatibility.Add([ordered]@{ Scope = $scope; GameExecutable = $gameExe; Value = $value; Error = $errorMessage }) | Out-Null
    }

    $mitigation = [ordered]@{ Available = $false; Text = ''; Error = '' }
    try {
        $command = Get-Command 'Get-ProcessMitigation' -ErrorAction SilentlyContinue
        if ($command) {
            $mitigation.Available = $true
            $mitigation.Text = ((& $command -Name 'DolocTown.exe' | Out-String -Width 240).Trim())
        }
    }
    catch { $mitigation.Error = $_.Exception.Message }

    return [ordered]@{
        SchemaVersion = 1
        CapturedAt = (Get-Date).ToString('o')
        GameProcessId = $ProcessId
        ProcessChain = @($chain.ToArray())
        LoaderEnvironment = @(Get-DtmLoaderEnvironmentContext)
        ImageFileExecutionOptions = $ifeo
        AppCompatLayers = @($compatibility.ToArray())
        ProcessMitigation = $mitigation
        Errors = @($errors.ToArray())
        PrivacyNote = 'Only known loader variables are included. Process scope is the probe process, not the Steam or DolocTown environment block.'
    }
}

function Get-DtmSecurityContext {
    $errors = New-Object 'System.Collections.Generic.List[string]'
    $products = @()
    try {
        $products = @(Get-CimInstance -Namespace 'root\SecurityCenter2' -ClassName 'AntiVirusProduct' -ErrorAction Stop | ForEach-Object {
            [ordered]@{
                DisplayName = [string]$_.displayName
                ProductState = [int]$_.productState
                Timestamp = [string]$_.timestamp
            }
        })
    }
    catch { $errors.Add("SecurityCenter2 AntiVirusProduct: $($_.Exception.Message)") | Out-Null }

    $defenderStatus = [ordered]@{ Available = $false; Values = @{}; Error = '' }
    try {
        if (Get-Command 'Get-MpComputerStatus' -ErrorAction SilentlyContinue) {
            $status = Get-MpComputerStatus -ErrorAction Stop
            $values = [ordered]@{}
            foreach ($name in @(
                'AMServiceEnabled', 'AntispywareEnabled', 'AntivirusEnabled', 'BehaviorMonitorEnabled',
                'IoavProtectionEnabled', 'NISEnabled', 'RealTimeProtectionEnabled', 'IsTamperProtected',
                'AMServiceVersion', 'AntivirusSignatureVersion', 'AntivirusSignatureLastUpdated'
            )) {
                if ($status.PSObject.Properties[$name]) { $values[$name] = $status.$name }
            }
            $defenderStatus.Available = $true
            $defenderStatus.Values = $values
        }
    }
    catch { $defenderStatus.Error = $_.Exception.Message }

    $defenderPolicy = [ordered]@{ Available = $false; Values = @{}; Error = ''; PrivacyNote = 'Defender exclusions are intentionally not collected.' }
    try {
        if (Get-Command 'Get-MpPreference' -ErrorAction SilentlyContinue) {
            $preference = Get-MpPreference -ErrorAction Stop
            $values = [ordered]@{}
            foreach ($name in @('DisableRealtimeMonitoring', 'EnableControlledFolderAccess', 'PUAProtection')) {
                if ($preference.PSObject.Properties[$name]) { $values[$name] = $preference.$name }
            }
            $asr = New-Object 'System.Collections.Generic.List[object]'
            $ids = @($preference.AttackSurfaceReductionRules_Ids | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
            $actions = @($preference.AttackSurfaceReductionRules_Actions)
            for ($i = 0; $i -lt $ids.Count; $i++) {
                $asr.Add([ordered]@{ Id = [string]$ids[$i]; Action = if ($i -lt $actions.Count) { [string]$actions[$i] } else { '' } }) | Out-Null
            }
            $values['AttackSurfaceReductionRules'] = @($asr.ToArray())
            $defenderPolicy.Available = $true
            $defenderPolicy.Values = $values
        }
    }
    catch { $defenderPolicy.Error = $_.Exception.Message }

    return [ordered]@{
        SchemaVersion = 1
        CapturedAt = (Get-Date).ToString('o')
        AntivirusProducts = $products
        WindowsDefenderStatus = $defenderStatus
        WindowsDefenderPolicy = $defenderPolicy
        Errors = @($errors.ToArray())
    }
}

function Get-DtmCriticalFileSecurity {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $paths = @(
        (Join-Path $GameDir 'winhttp.dll'),
        (Join-Path $GameDir 'doorstop_config.ini'),
        (Join-Path $GameDir 'BepInEx\core\BepInEx.Preloader.dll'),
        (Join-Path $GameDir 'BepInEx\core\BepInEx.dll'),
        (Join-Path $GameDir 'BepInEx\core\Mono.Cecil.dll'),
        (Join-Path $GameDir 'BepInEx\config\BepInEx.cfg'),
        (Join-Path $GameDir 'BepInEx\LogOutput.log'),
        (Join-Path $GameDir 'BepInEx\plugins\DTMAPI\DTMAPI.BepInExBootstrap.dll'),
        (Join-Path $GameDir 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'),
        (Join-Path $GameDir 'DTMAPI\logs\latest.log')
    )
    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($path in $paths) {
        $record = [ordered]@{
            Path = $path
            Exists = Test-Path -LiteralPath $path -PathType Leaf
            Length = $null
            Owner = ''
            Access = @()
            ZoneIdentifier = [ordered]@{ Present = $false; ZoneId = $null }
            Error = ''
        }
        if ($record.Exists) {
            try {
                $record.Length = (Get-Item -LiteralPath $path -ErrorAction Stop).Length
                $acl = Get-Acl -LiteralPath $path -ErrorAction Stop
                $record.Owner = [string]$acl.Owner
                $record.Access = @($acl.Access | Select-Object -First 50 | ForEach-Object {
                    [ordered]@{
                        Identity = [string]$_.IdentityReference
                        Rights = [string]$_.FileSystemRights
                        Type = [string]$_.AccessControlType
                        Inherited = [bool]$_.IsInherited
                    }
                })
                try {
                    $zoneText = Get-Content -LiteralPath $path -Stream 'Zone.Identifier' -ErrorAction Stop
                    $record.ZoneIdentifier.Present = $true
                    $zoneLine = $zoneText | Where-Object { $_ -match '^ZoneId=' } | Select-Object -First 1
                    if ($zoneLine) { $record.ZoneIdentifier.ZoneId = [int]($zoneLine -replace '^ZoneId=', '') }
                }
                catch {
                }
            }
            catch { $record.Error = $_.Exception.Message }
        }
        $items.Add($record) | Out-Null
    }
    return [ordered]@{
        SchemaVersion = 1
        CapturedAt = (Get-Date).ToString('o')
        Files = @($items.ToArray())
        PrivacyNote = 'Zone source and referrer URLs are intentionally not collected.'
    }
}

function Get-DtmRelevantSecurityEvents {
    param(
        [Parameter(Mandatory = $true)] [datetime] $StartedUtc,
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    $windowStart = $StartedUtc.AddMinutes(-2).ToLocalTime()
    $windowEnd = Get-Date
    $logs = @(
        'Microsoft-Windows-Windows Defender/Operational',
        'Microsoft-Windows-CodeIntegrity/Operational',
        'Microsoft-Windows-AppLocker/EXE and DLL'
    )
    $logStates = New-Object 'System.Collections.Generic.List[object]'
    $events = New-Object 'System.Collections.Generic.List[object]'
    $errors = New-Object 'System.Collections.Generic.List[string]'
    $pattern = '(?i)(DolocTown|BepInEx|DTMAPI|winhttp\.dll|Preloader|MonoBleedingEdge|Process Monitor|Procmon)'
    foreach ($logName in $logs) {
        try {
            $log = Get-WinEvent -ListLog $logName -ErrorAction Stop
            $logStates.Add([ordered]@{ LogName = $logName; Available = $true; Enabled = [bool]$log.IsEnabled; Error = '' }) | Out-Null
            if (-not $log.IsEnabled) { continue }
            $candidates = @(Get-WinEvent -FilterHashtable @{ LogName = $logName; StartTime = $windowStart; EndTime = $windowEnd } -MaxEvents 500 -ErrorAction SilentlyContinue)
            foreach ($event in $candidates) {
                if ($events.Count -ge 200) { break }
                $message = [string]$event.Message
                if ([string]::IsNullOrWhiteSpace($message) -or $message -notmatch $pattern) { continue }
                $events.Add([ordered]@{
                    TimeCreated = if ($event.TimeCreated) { $event.TimeCreated.ToString('o') } else { '' }
                    LogName = $logName
                    Provider = [string]$event.ProviderName
                    EventId = [int]$event.Id
                    Level = [string]$event.LevelDisplayName
                    Message = Protect-DtmDiagnosticText -Value $message
                }) | Out-Null
            }
        }
        catch {
            $logStates.Add([ordered]@{ LogName = $logName; Available = $false; Enabled = $false; Error = $_.Exception.Message }) | Out-Null
            $errors.Add("${logName}: $($_.Exception.Message)") | Out-Null
        }
    }
    return [ordered]@{
        SchemaVersion = 1
        WindowStart = $windowStart.ToString('o')
        WindowEnd = $windowEnd.ToString('o')
        MatchPattern = $pattern
        MaxEvents = 200
        Logs = @($logStates.ToArray())
        Events = @($events.ToArray())
        Errors = @($errors.ToArray())
        PrivacyNote = 'Only bounded event messages matching DTMAPI/DolocTown/loader terms are included; complete Windows event logs are not exported.'
    }
}

function Write-DtmCaptureArchive {
    if ([string]::IsNullOrWhiteSpace($script:DtmCaptureDirectory) -or
        -not (Test-Path -LiteralPath $script:DtmCaptureDirectory -PathType Container) -or
        [string]::IsNullOrWhiteSpace($script:DtmCaptureZip)) {
        return
    }

    try {
        if (Test-Path -LiteralPath $script:DtmCaptureZip -PathType Leaf) {
            Remove-Item -LiteralPath $script:DtmCaptureZip -Force
        }
        Compress-Archive -Path (Join-Path $script:DtmCaptureDirectory '*') -DestinationPath $script:DtmCaptureZip -CompressionLevel Optimal
    }
    catch {
        Add-DtmCaptureWarning -Message "Could not create support ZIP '$script:DtmCaptureZip': $($_.Exception.Message)"
    }
}

function Write-DtmStartupFallbackError {
    param([Parameter(Mandatory = $true)] $Failure)

    try {
        $fallbackRoot = $OutputRoot
        if ([string]::IsNullOrWhiteSpace($fallbackRoot)) {
            $fallbackRoot = [Environment]::GetFolderPath('Desktop')
            if ([string]::IsNullOrWhiteSpace($fallbackRoot)) {
                $fallbackRoot = Join-Path $env:USERPROFILE 'Desktop'
            }
        }
        $fallbackRoot = [System.IO.Path]::GetFullPath($fallbackRoot)
        New-Item -ItemType Directory -Force -Path $fallbackRoot | Out-Null
        $fallbackPath = Join-Path $fallbackRoot 'DTMAPI-startup-capture-last-error.txt'
        @(
            'DTMAPI 启动抓取未能完成。'
            '请把此文件发给维护者。'
            ''
            ('时间: ' + (Get-Date).ToString('o'))
            ('PowerShell: ' + $PSVersionTable.PSVersion.ToString())
            ('错误: ' + [string]$Failure)
        ) | Set-Content -LiteralPath $fallbackPath -Encoding UTF8
        Write-Host ''
        Write-Host '[ERROR] DTMAPI 启动抓取未能完成。' -ForegroundColor Red
        Write-Host "[ERROR] 错误详情已保存：$fallbackPath" -ForegroundColor Red
    }
    catch {
        Write-Host ''
        Write-Host "[ERROR] DTMAPI 启动抓取未能完成：$Failure" -ForegroundColor Red
    }
}

trap {
    Write-DtmStartupFallbackError -Failure $_
    exit 1
}

if (-not (Test-DtmAdministrator)) {
    if ($SkipElevation) {
        throw '抓取启动过程需要管理员权限。'
    }
    if ($Elevated) {
        throw '授权后仍未获得管理员权限。'
    }

    Write-Host '[1/7] 正在请求管理员权限以运行微软 Process Monitor...'
    $hostExe = (Get-Process -Id $PID).Path
    $arguments = New-Object 'System.Collections.Generic.List[string]'
    foreach ($argument in @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Elevated', '-CaptureSeconds', [string]$CaptureSeconds, '-LaunchTimeoutSeconds', [string]$LaunchTimeoutSeconds)) {
        if ([string]$argument -match '[\s"]') {
            $arguments.Add((ConvertTo-DtmQuotedArgument -Value ([string]$argument))) | Out-Null
        }
        else {
            $arguments.Add([string]$argument) | Out-Null
        }
    }
    foreach ($pair in @(
        @('-OutputRoot', $OutputRoot),
        @('-ProcmonPath', $ProcmonPath),
        @('-TestLaunchExecutable', $TestLaunchExecutable),
        @('-TestLaunchArgumentLine', $TestLaunchArgumentLine)
    )) {
        if (-not [string]::IsNullOrWhiteSpace([string]$pair[1])) {
            $arguments.Add([string]$pair[0]) | Out-Null
            $arguments.Add((ConvertTo-DtmQuotedArgument -Value ([string]$pair[1]))) | Out-Null
        }
    }
    if ($SkipConsent) { $arguments.Add('-SkipConsent') | Out-Null }
    if ($SkipLogCollection) { $arguments.Add('-SkipLogCollection') | Out-Null }

    try {
        $child = Start-Process -FilePath $hostExe -Verb RunAs -ArgumentList ($arguments -join ' ') -Wait -PassThru
        exit $child.ExitCode
    }
    catch {
        throw "管理员授权被取消或失败：$($_.Exception.Message)"
    }
}

$repo = Get-RepoRoot
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Resolve-DtmApiStateDir -GameDir $gameDir
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

if (Get-DtmGameProcess -GameDir $gameDir) {
    throw '检测到多洛可小镇仍在运行。请彻底退出游戏，再重新双击本 BAT。'
}

if (-not $SkipConsent) {
    Write-Host ''
    Write-Host '本工具会从微软官网下载并运行 Sysinternals Process Monitor。'
    Write-Host '只抓取 DolocTown.exe 的启动事件，最后在桌面生成一个可直接发送的 ZIP。'
    Write-Host '继续即表示你同意微软 Sysinternals Process Monitor 的许可协议。'
    $consent = Read-Host '直接按 Enter 继续；输入 N 取消'
    if ([string]$consent -match '^[Nn]') {
        Write-Host '已取消。'
        exit 2
    }
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $desktop = [Environment]::GetFolderPath('Desktop')
    if ([string]::IsNullOrWhiteSpace($desktop)) {
        $desktop = Join-Path $env:USERPROFILE 'Desktop'
    }
    $OutputRoot = Join-Path $desktop 'DTMAPI-startup-capture'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
$script:DtmCaptureDirectory = Join-Path $OutputRoot $stamp
New-Item -ItemType Directory -Force -Path $script:DtmCaptureDirectory | Out-Null
$script:DtmCaptureZip = Join-Path $OutputRoot ("DTMAPI-startup-capture-$stamp.zip")

$captureStartedUtc = (Get-Date).ToUniversalTime()
$bepInExLogPath = Join-Path $gameDir 'BepInEx\LogOutput.log'
$dtmapiLogPath = Join-Path $dtmapiDir 'logs\latest.log'
$bepInExBefore = Get-DtmFileSnapshot -Path $bepInExLogPath
$dtmapiBefore = Get-DtmFileSnapshot -Path $dtmapiLogPath
$pmlPath = Join-Path $script:DtmCaptureDirectory 'DolocTown-startup.pml'
$csvPath = Join-Path $script:DtmCaptureDirectory 'DolocTown-startup.csv'
$importantCsvPath = Join-Path $script:DtmCaptureDirectory 'important-events.csv'
$filterPath = Join-Path $script:DtmCaptureDirectory 'DolocTown-only.pmc'
$filterBase64 = 'OAAAABAAAAA0AAAABAAAAEQAZQBzAHQAcgB1AGMAdABpAHYAZQBGAGkAbAB0AGUAcgAAAAEAAABeAAAAEAAAACgAAAA2AAAARgBpAGwAdABlAHIAUgB1AGwAZQBzAAAAAQEAAAB1nAAAAAAAAAEcAAAARABvAGwAbwBjAFQAbwB3AG4ALgBlAHgAZQAAAAAAAAAAAAAA'
[System.IO.File]::WriteAllBytes($filterPath, [Convert]::FromBase64String($filterBase64))

$gameProcess = $null
$gameProcessObservedAt = $null
$moduleInventory = @()
$collectorExitCode = $null
$classification = 'NeedsReview'
$totalEvents = 0
$importantEvents = New-Object 'System.Collections.Generic.List[object]'
$preloaderEventCount = 0
$preloaderSuccessCount = 0
$configEventCount = 0
$loaderRelevantEventCount = 0
$loaderResultCounts = @{}
$loaderNonSuccessCounts = @{}
$launchContext = [ordered]@{ Collected = $false; Error = 'Game process not observed.' }
$securityContext = [ordered]@{ AntivirusProducts = @(); WindowsDefenderStatus = @{}; WindowsDefenderPolicy = @{}; Errors = @('Not collected.') }
$criticalFileSecurity = [ordered]@{ Files = @(); Error = 'Not collected.' }
$securityEvents = [ordered]@{ Events = @(); Errors = @('Not collected.') }
$gameCloseAttempted = $false
$gameClose = [pscustomobject][ordered]@{
    Status = 'NotAttempted'
    ProcessId = $null
    GracefulCloseRequested = $false
    Forced = $false
    Exited = $false
}

try {
    $existingProcmon = @(Get-Process -Name 'Procmon', 'Procmon64', 'Procmon64a' -ErrorAction SilentlyContinue)
    if ($existingProcmon.Count -gt 0) {
        throw '检测到 Process Monitor 已在运行。请先关闭它再重试；本工具不会打断已有抓取。'
    }

    $script:DtmProcmonExe = Resolve-DtmProcmon -RequestedPath $ProcmonPath -Stamp $stamp
    $procmonSignature = Get-AuthenticodeSignature -LiteralPath $script:DtmProcmonExe
    $procmonItem = Get-Item -LiteralPath $script:DtmProcmonExe
    $procmonHash = (Get-FileHash -LiteralPath $script:DtmProcmonExe -Algorithm SHA256).Hash

    Write-Host '[3/7] 正在启动仅限 DolocTown.exe 的抓取...'
    $maximumRuntime = $LaunchTimeoutSeconds + $CaptureSeconds + 45
    $captureArgumentLine = @(
        '/AcceptEula',
        '/Quiet',
        '/Minimized',
        '/LoadConfig', (ConvertTo-DtmQuotedArgument -Value $filterPath),
        '/BackingFile', (ConvertTo-DtmQuotedArgument -Value $pmlPath),
        '/Runtime', [string]$maximumRuntime
    ) -join ' '
    Start-Process -FilePath $script:DtmProcmonExe -ArgumentList $captureArgumentLine | Out-Null
    $script:DtmProcmonOwned = $true
    Start-Sleep -Seconds 1
    & $script:DtmProcmonExe /WaitForIdle /Quiet | Out-Null

    Write-Host '[4/7] 正在通过 Steam 启动多洛可小镇。启动后请停留在标题界面。'
    if (-not [string]::IsNullOrWhiteSpace($TestLaunchExecutable)) {
        if ([string]::IsNullOrWhiteSpace($TestLaunchArgumentLine)) {
            Start-Process -FilePath $TestLaunchExecutable | Out-Null
        }
        else {
            Start-Process -FilePath $TestLaunchExecutable -ArgumentList $TestLaunchArgumentLine | Out-Null
        }
    }
    else {
        $explorerExe = Join-Path $env:SystemRoot 'explorer.exe'
        Start-Process -FilePath $explorerExe -ArgumentList '"steam://rungameid/2285550"' | Out-Null
    }

    $launchDeadline = (Get-Date).AddSeconds($LaunchTimeoutSeconds)
    while ((Get-Date) -lt $launchDeadline) {
        $gameProcess = Get-DtmGameProcess -GameDir $gameDir
        if ($gameProcess) {
            $gameProcessObservedAt = Get-Date
            break
        }
        Start-Sleep -Milliseconds 500
    }

    if (-not $gameProcess) {
        Add-DtmCaptureWarning -Message "$LaunchTimeoutSeconds 秒内没有检测到 DolocTown.exe。"
    }
    else {
        Write-Host "[OK] 已检测到游戏进程：PID $($gameProcess.Id)"
        Start-Sleep -Seconds 2
        try {
            $moduleInventory = @($gameProcess.Modules | ForEach-Object {
                [ordered]@{
                    ModuleName = $_.ModuleName
                    FileName = $_.FileName
                    FileVersion = [string]$_.FileVersionInfo.FileVersion
                }
            })
        }
        catch {
            Add-DtmCaptureWarning -Message "Could not enumerate game modules: $($_.Exception.Message)"
        }

        try {
            $launchContext = Get-DtmLaunchContext -ProcessId $gameProcess.Id -GameDir $gameDir
            $launchContext['Collected'] = $true
            $launchContext | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'launch-context.json') -Encoding UTF8
        }
        catch {
            $launchContext = [ordered]@{ Collected = $false; Error = $_.Exception.Message }
            $launchContext | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'launch-context.json') -Encoding UTF8
        }

        Write-Host "[5/7] 正在抓取启动过程，共 $CaptureSeconds 秒..."
        for ($elapsed = 0; $elapsed -lt $CaptureSeconds; $elapsed++) {
            if (-not (Get-Process -Id $gameProcess.Id -ErrorAction SilentlyContinue)) {
                Add-DtmCaptureWarning -Message "DolocTown.exe 在抓取约 $elapsed 秒后退出。"
                break
            }
            if ($elapsed -gt 0 -and ($elapsed % 10) -eq 0) {
                Write-Host "       已完成 $elapsed / $CaptureSeconds 秒"
            }
            Start-Sleep -Seconds 1
        }
    }

    Stop-DtmOwnedProcmon
    if (-not (Wait-DtmFileReady -Path $pmlPath -TimeoutSeconds 20)) {
        throw "Process Monitor did not produce a stable PML file: $pmlPath"
    }

    Write-Host '[6/7] 正在导出并分析启动抓取...'
    $convertArgumentLine = @(
        '/AcceptEula',
        '/Quiet',
        '/OpenLog', (ConvertTo-DtmQuotedArgument -Value $pmlPath),
        '/SaveAs', (ConvertTo-DtmQuotedArgument -Value $csvPath)
    ) -join ' '
    $convertProcess = Start-Process -FilePath $script:DtmProcmonExe -ArgumentList $convertArgumentLine -PassThru
    if (-not $convertProcess.WaitForExit(60000)) {
        try { Stop-Process -Id $convertProcess.Id -Force -ErrorAction SilentlyContinue } catch {}
        throw 'Process Monitor 导出 CSV 超过 60 秒，已停止导出。'
    }
    if ($convertProcess.ExitCode -ne 0) {
        Add-DtmCaptureWarning -Message "Process Monitor CSV export exit code: $($convertProcess.ExitCode)"
    }
    if (-not (Wait-DtmFileReady -Path $csvPath -TimeoutSeconds 30)) {
        throw "Process Monitor did not produce a readable CSV file: $csvPath"
    }

    foreach ($event in Import-Csv -LiteralPath $csvPath) {
        $totalEvents++
        $path = [string]$event.Path
        $result = [string]$event.Result
        $isRelevantPath = $path -match '(?i)(BepInEx|doorstop|DTMAPI|winhttp\.dll|MonoBleedingEdge|DolocTown_Data\\Managed|LogOutput\.log)'
        if ($isRelevantPath) {
            $importantEvents.Add($event) | Out-Null
            $loaderRelevantEventCount++
            if (-not $loaderResultCounts.ContainsKey($result)) { $loaderResultCounts[$result] = 0 }
            $loaderResultCounts[$result]++
            if ($result -ne 'SUCCESS') {
                $operation = [string]$event.Operation
                $key = $result + [char]31 + $operation + [char]31 + $path
                if (-not $loaderNonSuccessCounts.ContainsKey($key)) { $loaderNonSuccessCounts[$key] = 0 }
                $loaderNonSuccessCounts[$key]++
            }
        }
        if ($path -match '(?i)BepInEx\\core\\BepInEx\.Preloader\.dll$') {
            $preloaderEventCount++
            if ($result -eq 'SUCCESS') {
                $preloaderSuccessCount++
            }
        }
        if ($path -match '(?i)doorstop_config\.ini$') {
            $configEventCount++
        }
    }
    if ($importantEvents.Count -gt 0) {
        $importantEvents.ToArray() | Export-Csv -LiteralPath $importantCsvPath -NoTypeInformation -Encoding UTF8
    }
    else {
        'No BepInEx/Doorstop/DTMAPI path events were captured.' | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'NO-IMPORTANT-EVENTS.txt') -Encoding UTF8
    }

    $resultGroups = @($loaderResultCounts.GetEnumerator() | Sort-Object Name | ForEach-Object {
        [ordered]@{ Result = [string]$_.Name; Count = [int]$_.Value }
    })
    $nonSuccessGroups = @($loaderNonSuccessCounts.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 200 | ForEach-Object {
        $parts = ([string]$_.Name).Split([char]31)
        [ordered]@{
            Count = [int]$_.Value
            Result = if ($parts.Count -gt 0) { $parts[0] } else { '' }
            Operation = if ($parts.Count -gt 1) { $parts[1] } else { '' }
            Path = if ($parts.Count -gt 2) { $parts[2] } else { '' }
        }
    })
    $loaderAccessSummary = [ordered]@{
        SchemaVersion = 1
        TotalCapturedEvents = $totalEvents
        RelevantLoaderEvents = $loaderRelevantEventCount
        ResultCounts = $resultGroups
        NonSuccessGroups = $nonSuccessGroups
        NonSuccessGroupLimit = 200
        InterpretationNote = 'Non-success does not always mean failure; NAME NOT FOUND, NO MORE FILES, END OF FILE, and FAST IO DISALLOWED are often normal probe/fallback behavior.'
    }
    $loaderAccessSummary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'loader-access-summary.json') -Encoding UTF8

    $localWinHttp = @($moduleInventory | Where-Object {
        [string]$_.ModuleName -ieq 'winhttp.dll' -and
        -not [string]::IsNullOrWhiteSpace([string]$_.FileName) -and
        (Test-DtmApiPathIsSameOrChild -Child ([string]$_.FileName) -Parent $gameDir)
    })
    if (-not $SkipLogCollection) {
        $logsDirectory = Join-Path $script:DtmCaptureDirectory 'DTMAPI-logs'
        try {
            & "$PSScriptRoot\collect-logs.ps1" -CaseId 'PLAYER-STARTUP-CAPTURE' -OutputDirectory $logsDirectory
            $collectorExitCode = $LASTEXITCODE
        }
        catch {
            $collectorExitCode = 1
            Add-DtmCaptureWarning -Message "Bounded log collection failed: $($_.Exception.Message)"
        }
    }

    $gameCloseAttempted = $true
    $gameClose = Stop-DtmCapturedGame -Process $gameProcess -GameDir $gameDir

    try {
        $securityContext = Get-DtmSecurityContext
        $securityContext | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'security-context.json') -Encoding UTF8
    }
    catch {
        $securityContext = [ordered]@{ Errors = @($_.Exception.Message); AntivirusProducts = @() }
        $securityContext | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'security-context.json') -Encoding UTF8
    }
    try {
        $criticalFileSecurity = Get-DtmCriticalFileSecurity -GameDir $gameDir
        $criticalFileSecurity | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'critical-loader-file-security.json') -Encoding UTF8
    }
    catch {
        $criticalFileSecurity = [ordered]@{ Files = @(); Error = $_.Exception.Message }
        $criticalFileSecurity | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'critical-loader-file-security.json') -Encoding UTF8
    }
    try {
        $securityEvents = Get-DtmRelevantSecurityEvents -StartedUtc $captureStartedUtc -GameDir $gameDir
        $securityEvents | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'security-events.json') -Encoding UTF8
    }
    catch {
        $securityEvents = [ordered]@{ Events = @(); Errors = @($_.Exception.Message) }
        $securityEvents | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'security-events.json') -Encoding UTF8
    }

    # Retry freshness after closing the captured process. BepInEx can hold LogOutput.log
    # exclusively while the game runs, which must not override a fresh DTMAPI log.
    $bepInExAfter = Get-DtmFileSnapshot -Path $bepInExLogPath
    $dtmapiAfter = Get-DtmFileSnapshot -Path $dtmapiLogPath
    $bepInExFresh = Test-DtmSnapshotFresh -Snapshot $bepInExAfter -StartedUtc $captureStartedUtc
    $dtmapiFresh = Test-DtmSnapshotFresh -Snapshot $dtmapiAfter -StartedUtc $captureStartedUtc
    if (-not $gameProcess) {
        $classification = 'GameProcessNotObserved'
    }
    elseif ($dtmapiFresh) {
        $classification = 'FreshDtmapiStartup'
    }
    elseif ($bepInExFresh) {
        $classification = 'FreshBepInExNoDtmapi'
    }
    elseif ($moduleInventory.Count -gt 0 -and $localWinHttp.Count -eq 0) {
        $classification = 'LocalDoorstopModuleNotObserved'
    }
    elseif ($preloaderEventCount -eq 0) {
        $classification = 'DoorstopLoadedPreloaderNotRequested'
    }
    elseif ($preloaderSuccessCount -gt 0) {
        $classification = 'PreloaderReadNoFreshBepInExLog'
    }

    $captureCompletedUtc = (Get-Date).ToUniversalTime()
    $summary = [ordered]@{
        SchemaVersion = 2
        Classification = $classification
        CaptureStartedUtc = $captureStartedUtc.ToString('o')
        CaptureCompletedUtc = $captureCompletedUtc.ToString('o')
        GameDir = $gameDir
        GameProcessObserved = $null -ne $gameProcess
        GameProcessId = if ($gameProcess) { $gameProcess.Id } else { $null }
        GameProcessObservedAt = if ($gameProcessObservedAt) { $gameProcessObservedAt.ToString('o') } else { $null }
        GameClose = $gameClose
        CaptureSeconds = $CaptureSeconds
        LaunchTimeoutSeconds = $LaunchTimeoutSeconds
        ProcmonPath = $script:DtmProcmonExe
        ProcmonVersion = [string]$procmonItem.VersionInfo.FileVersion
        ProcmonSha256 = $procmonHash
        ProcmonSignatureStatus = [string]$procmonSignature.Status
        ProcmonSigner = if ($procmonSignature.SignerCertificate) { [string]$procmonSignature.SignerCertificate.Subject } else { '' }
        ProcmonFilter = 'Process Name is DolocTown.exe / Include; destructive filtering enabled'
        ProcmonTotalEvents = $totalEvents
        ProcmonImportantEvents = $importantEvents.Count
        DoorstopConfigEvents = $configEventCount
        PreloaderEvents = $preloaderEventCount
        PreloaderSuccessEvents = $preloaderSuccessCount
        LocalWinHttpObserved = $localWinHttp.Count -gt 0
        ModuleInventory = @($moduleInventory)
        BepInExLogBefore = $bepInExBefore
        BepInExLogAfter = $bepInExAfter
        BepInExLogFresh = $bepInExFresh
        DtmapiLogBefore = $dtmapiBefore
        DtmapiLogAfter = $dtmapiAfter
        DtmapiLogFresh = $dtmapiFresh
        CollectorExitCode = $collectorExitCode
        ExtendedContext = [ordered]@{
            LaunchContextCollected = [bool]$launchContext.Collected
            AntivirusProductCount = @($securityContext.AntivirusProducts).Count
            SecurityEventCount = @($securityEvents.Events).Count
            CriticalFileCount = @($criticalFileSecurity.Files).Count
            LoaderRelevantEventCount = $loaderRelevantEventCount
            LoaderNonSuccessGroupCount = $nonSuccessGroups.Count
        }
        Warnings = @($script:DtmCaptureWarnings)
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'capture-summary.json') -Encoding UTF8

    $summaryLines = New-Object 'System.Collections.Generic.List[string]'
    $summaryLines.Add("Classification=$classification") | Out-Null
    $summaryLines.Add("GameDir=$gameDir") | Out-Null
    $summaryLines.Add("GameProcessObserved=$($null -ne $gameProcess)") | Out-Null
    $summaryLines.Add("GameCloseStatus=$($gameClose.Status)") | Out-Null
    $summaryLines.Add("LocalWinHttpObserved=$($localWinHttp.Count -gt 0)") | Out-Null
    $summaryLines.Add("DoorstopConfigEvents=$configEventCount") | Out-Null
    $summaryLines.Add("PreloaderEvents=$preloaderEventCount") | Out-Null
    $summaryLines.Add("PreloaderSuccessEvents=$preloaderSuccessCount") | Out-Null
    $summaryLines.Add("BepInExLogFresh=$bepInExFresh") | Out-Null
    $summaryLines.Add("DtmapiLogFresh=$dtmapiFresh") | Out-Null
    $summaryLines.Add("ProcmonTotalEvents=$totalEvents") | Out-Null
    $summaryLines.Add("ProcmonImportantEvents=$($importantEvents.Count)") | Out-Null
    $summaryLines.Add("LaunchContextCollected=$([bool]$launchContext.Collected)") | Out-Null
    $summaryLines.Add("AntivirusProductCount=$(@($securityContext.AntivirusProducts).Count)") | Out-Null
    $summaryLines.Add("SecurityEventCount=$(@($securityEvents.Events).Count)") | Out-Null
    $summaryLines.Add("LoaderNonSuccessGroupCount=$($nonSuccessGroups.Count)") | Out-Null
    $summaryLines.Add("Warnings=$($script:DtmCaptureWarnings.Count)") | Out-Null
    $summaryLines | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'capture-summary.txt') -Encoding UTF8

    Write-Host '[7/7] 正在生成一个可直接发送的支持 ZIP...'
    Write-DtmCaptureArchive

    Write-Host ''
    Write-Host "[OK] 启动抓取分类：$classification" -ForegroundColor Green
    Write-Host "[OK] 请把这个 ZIP 发给维护者：$($script:DtmCaptureZip)" -ForegroundColor Green
    Write-Host '[INFO] 抓取只保留 DolocTown.exe，但仍可能包含本机用户名或存档路径。'
    exit 0
}
catch {
    $errorText = $_ | Out-String
    Write-Host ''
    Write-Host "[ERROR] 启动抓取失败：$($_.Exception.Message)" -ForegroundColor Red
    if (-not [string]::IsNullOrWhiteSpace($script:DtmCaptureDirectory) -and (Test-Path -LiteralPath $script:DtmCaptureDirectory -PathType Container)) {
        $errorText | Set-Content -LiteralPath (Join-Path $script:DtmCaptureDirectory 'capture-error.txt') -Encoding UTF8
        Stop-DtmOwnedProcmon
        Write-DtmCaptureArchive
        if (Test-Path -LiteralPath $script:DtmCaptureZip -PathType Leaf) {
            Write-Host "[INFO] 已生成部分证据 ZIP，也请一并发送：$($script:DtmCaptureZip)"
        }
    }
    else {
        Stop-DtmOwnedProcmon
    }
    exit 1
}
finally {
    Stop-DtmOwnedProcmon
    if (-not $gameCloseAttempted -and $gameProcess) {
        $gameCloseAttempted = $true
        $gameClose = Stop-DtmCapturedGame -Process $gameProcess -GameDir $gameDir
    }
}
