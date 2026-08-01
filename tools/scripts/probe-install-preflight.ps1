param(
    [string] $GameDir = '',
    [string] $ReportDir = ''
)

$ErrorActionPreference = 'Stop'

$script:ProbeSteps = New-Object 'System.Collections.Generic.List[object]'
$script:ProbeContext = [ordered]@{}
$script:ProbeStartedAt = [DateTimeOffset]::UtcNow
$script:ProbePackageRoot = ''
$script:ProbeGameDir = ''
$script:ProbePayloadRoot = ''
$script:ProbePluginDir = ''
$script:ProbeStateDir = ''
$script:ProbeReportPath = ''

function Write-ProbeJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $json = $Value | ConvertTo-Json -Depth 24
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Get-ProbeCurrentHostPath {
    try {
        $current = (Get-Process -Id $PID -ErrorAction Stop).Path
        if ($current -and (Test-Path -LiteralPath $current -PathType Leaf)) {
            return [System.IO.Path]::GetFullPath($current)
        }
    }
    catch {
    }

    return ''
}

function New-ProbeReportObject {
    param(
        [string] $Status = 'running'
    )

    $steps = @($script:ProbeSteps.ToArray())
    $okCount = @($steps | Where-Object { $_.Status -eq 'OK' }).Count
    $warnCount = @($steps | Where-Object { $_.Status -eq 'WARN' }).Count
    $failCount = @($steps | Where-Object { $_.Status -eq 'FAIL' }).Count
    $skippedCount = @($steps | Where-Object { $_.Status -eq 'SKIPPED' }).Count
    $blockingFailures = @($steps | Where-Object { $_.Status -eq 'FAIL' -and $_.Blocking })

    return [ordered]@{
        SchemaVersion = 1
        ProbeKind = 'DTMAPI install preflight'
        Status = $Status
        StartedAt = $script:ProbeStartedAt.ToString('o')
        CompletedAt = [DateTimeOffset]::UtcNow.ToString('o')
        ProbeHost = [ordered]@{
            Path = Get-ProbeCurrentHostPath
            PSVersion = [string]$PSVersionTable.PSVersion
            PSEdition = [string]$PSVersionTable.PSEdition
            ProcessId = $PID
        }
        Context = $script:ProbeContext
        Summary = [ordered]@{
            Ok = $okCount
            Warn = $warnCount
            Fail = $failCount
            Skipped = $skippedCount
            BlockingFailureCount = @($blockingFailures).Count
            BlockingFailures = @($blockingFailures | ForEach-Object { $_.Name })
        }
        Steps = $steps
    }
}

function Write-ProbeReport {
    param([string] $Status = 'running')

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $fileName = "dtmapi-install-preflight-probe-$stamp.json"
    $candidateDirs = New-Object 'System.Collections.Generic.List[string]'
    if (-not [string]::IsNullOrWhiteSpace($ReportDir)) {
        $candidateDirs.Add([System.IO.Path]::GetFullPath($ReportDir)) | Out-Null
    }
    if (-not [string]::IsNullOrWhiteSpace($script:ProbePackageRoot)) {
        $candidateDirs.Add($script:ProbePackageRoot) | Out-Null
    }
    if (-not [string]::IsNullOrWhiteSpace($env:TEMP)) {
        $candidateDirs.Add([System.IO.Path]::GetFullPath($env:TEMP)) | Out-Null
    }

    foreach ($dir in @($candidateDirs.ToArray())) {
        try {
            $path = Join-Path $dir $fileName
            Write-ProbeJson -Path $path -Value (New-ProbeReportObject -Status $Status)
            $script:ProbeReportPath = [System.IO.Path]::GetFullPath($path)
            return $script:ProbeReportPath
        }
        catch {
        }
    }

    return ''
}

function Add-ProbeStep {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Status,
        [string] $Message = '',
        $Details = $null,
        [bool] $Blocking = $false,
        [int] $DurationMs = 0
    )

    $index = $script:ProbeSteps.Count + 1
    $step = [ordered]@{
        Index = $index
        Name = $Name
        Status = $Status
        Blocking = $Blocking
        DurationMs = $DurationMs
        Message = $Message
        Details = $Details
    }
    $script:ProbeSteps.Add($step) | Out-Null

    $color = [ConsoleColor]::Gray
    if ($Status -eq 'OK') {
        $color = [ConsoleColor]::Green
    }
    elseif ($Status -eq 'WARN') {
        $color = [ConsoleColor]::Yellow
    }
    elseif ($Status -eq 'FAIL') {
        $color = [ConsoleColor]::Red
    }
    elseif ($Status -eq 'SKIPPED') {
        $color = [ConsoleColor]::DarkGray
    }

    Write-Host ("[{0}] {1}. {2}" -f $Status, $index, $Name) -ForegroundColor $color
    if (-not [string]::IsNullOrWhiteSpace($Message)) {
        Write-Host ("     {0}" -f $Message)
    }
}

function Invoke-ProbeStep {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [scriptblock] $Action,
        [switch] $NonBlocking
    )

    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $result = & $Action
        $watch.Stop()

        $status = 'OK'
        $message = ''
        $details = $null
        if ($null -ne $result) {
            if ($result.PSObject.Properties['Status']) {
                $status = [string]$result.Status
            }
            if ($result.PSObject.Properties['Message']) {
                $message = [string]$result.Message
            }
            if ($result.PSObject.Properties['Details']) {
                $details = $result.Details
            }
        }

        $blocking = $false
        if ($status -eq 'FAIL' -and -not $NonBlocking) {
            $blocking = $true
        }

        Add-ProbeStep -Name $Name -Status $status -Message $message -Details $details -Blocking:$blocking -DurationMs ([int]$watch.ElapsedMilliseconds)
        return $result
    }
    catch {
        $watch.Stop()
        $details = [ordered]@{
            Error = $_.Exception.Message
            ScriptName = [string]$_.InvocationInfo.ScriptName
            ScriptLineNumber = $_.InvocationInfo.ScriptLineNumber
            Line = [string]$_.InvocationInfo.Line
        }
        Add-ProbeStep -Name $Name -Status 'FAIL' -Message $_.Exception.Message -Details $details -Blocking:(!$NonBlocking) -DurationMs ([int]$watch.ElapsedMilliseconds)
        return $null
    }
}

function New-ProbeResult {
    param(
        [string] $Status,
        [string] $Message = '',
        $Details = $null
    )

    return [pscustomobject]@{
        Status = $Status
        Message = $Message
        Details = $Details
    }
}

$loadFailures = New-Object 'System.Collections.Generic.List[object]'
foreach ($scriptName in @('common.ps1', 'release-common.ps1')) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    try {
        . $scriptPath
    }
    catch {
        $loadFailures.Add([ordered]@{
            Script = $scriptPath
            Error = $_.Exception.Message
            ScriptName = [string]$_.InvocationInfo.ScriptName
            ScriptLineNumber = $_.InvocationInfo.ScriptLineNumber
            Line = [string]$_.InvocationInfo.Line
        }) | Out-Null
    }
}

if ($loadFailures.Count -gt 0) {
    Add-ProbeStep -Name 'Bootstrap shared installer scripts' -Status 'FAIL' -Message 'The probe could not load required shared installer scripts.' -Details @($loadFailures.ToArray()) -Blocking:$true
    $report = Write-ProbeReport -Status 'blocked'
    if (-not [string]::IsNullOrWhiteSpace($report)) {
        Write-Host "[INFO] Probe report: $report"
    }
    exit 2
}

function Add-ProbeHostCandidate {
    param(
        [Parameter(Mandatory = $true)] $List,
        [Parameter(Mandatory = $true)] $Seen,
        [string] $Path,
        [string] $Label
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return
    }

    $full = [System.IO.Path]::GetFullPath($Path)
    if ($Seen.Add($full)) {
        $List.Add([pscustomobject]@{
            Label = $Label
            Path = $full
        }) | Out-Null
    }
}

function Get-ProbePowerShellHostCandidates {
    $list = New-Object 'System.Collections.Generic.List[object]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    Add-ProbeHostCandidate -List $list -Seen $seen -Path (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') -Label 'Windows PowerShell fixed path'

    foreach ($powershell in @(Get-Command powershell.exe -ErrorAction SilentlyContinue)) {
        Add-ProbeHostCandidate -List $list -Seen $seen -Path $powershell.Source -Label 'Windows PowerShell PATH'
    }

    foreach ($candidateRoot in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if (-not [string]::IsNullOrWhiteSpace($candidateRoot)) {
            Add-ProbeHostCandidate -List $list -Seen $seen -Path (Join-Path $candidateRoot 'PowerShell\7\pwsh.exe') -Label 'PowerShell 7 fixed path'
        }
    }

    foreach ($pwsh in @(Get-Command pwsh.exe -ErrorAction SilentlyContinue)) {
        Add-ProbeHostCandidate -List $list -Seen $seen -Path $pwsh.Source -Label 'PowerShell 7 PATH'
    }

    Add-ProbeHostCandidate -List $list -Seen $seen -Path (Get-ProbeCurrentHostPath) -Label 'Current probe host'

    return @($list.ToArray())
}

function Invoke-ProbePowerShellProcess {
    param(
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    try {
        $output = @(& $HostPath @Arguments 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    catch {
        $output = @($_.Exception.Message)
        $exitCode = -999
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Ok = ($exitCode -eq 0)
        Output = @($output)
    }
}

function Invoke-ProbePowerShellTempFile {
    param(
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $ScriptText,
        [string[]] $ExtraArguments = @()
    )

    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) ("dtmapi-probe-host-{0}.ps1" -f ([guid]::NewGuid().ToString('N')))
    try {
        $utf8Bom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($tempPath, $ScriptText, $utf8Bom)
        $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $tempPath)
        if ($ExtraArguments) {
            $arguments += $ExtraArguments
        }

        return Invoke-ProbePowerShellProcess -HostPath $HostPath -Arguments $arguments
    }
    finally {
        if (Test-Path -LiteralPath $tempPath -PathType Leaf) {
            Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Invoke-ProbePowerShellEncoded {
    param(
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $ScriptText
    )

    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($ScriptText))
    return Invoke-ProbePowerShellProcess -HostPath $HostPath -Arguments @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $encoded)
}

function Convert-ProbeFirstJsonOutput {
    param([object[]] $Output)

    $first = @($Output | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -First 1)
    if ($first.Count -eq 0) {
        return $null
    }

    try {
        return ([string]$first[0] | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

function Invoke-ProbePowerShellHostSelfDiagnostic {
    param(
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $fileState = Get-ProbeFileState -Path $HostPath -PathType Leaf
    $versionInfo = $null
    try {
        $item = Get-Item -LiteralPath $HostPath -ErrorAction Stop
        $versionInfo = [ordered]@{
            FileVersion = [string]$item.VersionInfo.FileVersion
            ProductVersion = [string]$item.VersionInfo.ProductVersion
            ProductName = [string]$item.VersionInfo.ProductName
            CompanyName = [string]$item.VersionInfo.CompanyName
            Length = $item.Length
        }
    }
    catch {
        $versionInfo = [ordered]@{
            Error = $_.Exception.Message
        }
    }

    $runtimeInfoScript = @'
$ErrorActionPreference = 'Stop'
$policyList = @()
try {
    $policyList = @(Get-ExecutionPolicy -List | ForEach-Object {
        [ordered]@{
            Scope = [string]$_.Scope
            ExecutionPolicy = [string]$_.ExecutionPolicy
        }
    })
}
catch {
}

$processPath = ''
try {
    $processPath = (Get-Process -Id $PID -ErrorAction Stop).Path
}
catch {
}

$runtime = [ordered]@{
    PSVersion = [string]$PSVersionTable.PSVersion
    PSEdition = [string]$PSVersionTable.PSEdition
    CLRVersion = [string]$PSVersionTable.CLRVersion
    GitCommitId = [string]$PSVersionTable.GitCommitId
    PSHome = [string]$PSHOME
    HostName = [string]$Host.Name
    HostVersion = [string]$Host.Version
    LanguageMode = [string]$ExecutionContext.SessionState.LanguageMode
    Is64BitProcess = [Environment]::Is64BitProcess
    OSVersion = [Environment]::OSVersion.VersionString
    ProcessPath = $processPath
    ExecutionPolicy = [string](Get-ExecutionPolicy)
    ExecutionPolicyList = @($policyList)
}
$runtime | ConvertTo-Json -Depth 6 -Compress
'@

    $parseInputScript = @'
$ErrorActionPreference = 'Stop'
try {
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseInput('Write-Output 1', [ref] $tokens, [ref] $errors) | Out-Null
    if ($errors -and $errors.Count -gt 0) {
        foreach ($err in $errors) {
            "{0}:{1} {2}" -f $err.Extent.StartLineNumber, $err.Extent.StartColumnNumber, $err.Message
        }
        exit 1
    }

    'parseinput-ok'
    exit 0
}
catch {
    "HOST_ERROR {0}" -f $_.Exception.Message
    exit 2
}
'@

    $commandMinimal = Invoke-ProbePowerShellProcess -HostPath $HostPath -Arguments @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', '$PSVersionTable.PSVersion.ToString(); exit 0')
    $fileRuntime = Invoke-ProbePowerShellTempFile -HostPath $HostPath -ScriptText $runtimeInfoScript
    $fileRuntimeInfo = Convert-ProbeFirstJsonOutput -Output $fileRuntime.Output
    $fileParseInput = Invoke-ProbePowerShellTempFile -HostPath $HostPath -ScriptText $parseInputScript
    $encodedMinimal = Invoke-ProbePowerShellEncoded -HostPath $HostPath -ScriptText "Write-Output 'encoded-ok'; exit 0"
    $encodedParseInput = Invoke-ProbePowerShellEncoded -HostPath $HostPath -ScriptText $parseInputScript

    return [ordered]@{
        Label = $Label
        Host = $HostPath
        FileState = $fileState
        VersionInfo = $versionInfo
        CommandMinimal = $commandMinimal
        FileRuntimeInfo = $fileRuntime
        RuntimeInfo = $fileRuntimeInfo
        FileParseInput = $fileParseInput
        EncodedMinimal = $encodedMinimal
        EncodedParseInput = $encodedParseInput
    }
}

function Invoke-ProbeParserCheck {
    param(
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $ScriptPath
    )

    $encodedPath = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($ScriptPath))
    $checkScript = @"
`$ErrorActionPreference = 'Stop'
`$path = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('$encodedPath'))
try {
    `$tokens = `$null
    `$errors = `$null
    [System.Management.Automation.Language.Parser]::ParseFile(`$path, [ref] `$tokens, [ref] `$errors) | Out-Null
    if (`$errors -and `$errors.Count -gt 0) {
        foreach (`$err in `$errors) {
            "{0}:{1} {2}" -f `$err.Extent.StartLineNumber, `$err.Extent.StartColumnNumber, `$err.Message
        }
        exit 1
    }
    exit 0
}
catch {
    "HOST_ERROR {0}" -f `$_.Exception.Message
    exit 2
}
"@
    $encodedCommand = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($checkScript))

    try {
        $output = @(& $HostPath -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encodedCommand 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    catch {
        $output = @($_.Exception.Message)
        $exitCode = -999
    }

    return [pscustomobject]@{
        Script = $ScriptPath
        Method = 'EncodedCommand'
        Ok = ($exitCode -eq 0)
        ExitCode = $exitCode
        Output = @($output)
    }
}

function Invoke-ProbeParserCheckFile {
    param(
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $ScriptPath
    )

    $validatorScript = @"
param(
    [Parameter(Mandatory = `$true)] [string] `$Path
)

`$ErrorActionPreference = 'Stop'
try {
    `$tokens = `$null
    `$errors = `$null
    [System.Management.Automation.Language.Parser]::ParseFile(`$Path, [ref] `$tokens, [ref] `$errors) | Out-Null
    if (`$errors -and `$errors.Count -gt 0) {
        foreach (`$err in `$errors) {
            "{0}:{1} {2}" -f `$err.Extent.StartLineNumber, `$err.Extent.StartColumnNumber, `$err.Message
        }
        exit 1
    }
    exit 0
}
catch {
    "HOST_ERROR {0}" -f `$_.Exception.Message
    exit 2
}
"@

    $validatorPath = Join-Path ([System.IO.Path]::GetTempPath()) ("dtmapi-probe-parser-{0}.ps1" -f ([guid]::NewGuid().ToString('N')))
    try {
        $utf8Bom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($validatorPath, $validatorScript, $utf8Bom)
        $output = @(& $HostPath -NoProfile -ExecutionPolicy Bypass -File $validatorPath -Path $ScriptPath 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    catch {
        $output = @($_.Exception.Message)
        $exitCode = -999
    }
    finally {
        if (Test-Path -LiteralPath $validatorPath -PathType Leaf) {
            Remove-Item -LiteralPath $validatorPath -Force -ErrorAction SilentlyContinue
        }
    }

    return [pscustomobject]@{
        Script = $ScriptPath
        Method = 'FileValidator'
        Ok = ($exitCode -eq 0)
        ExitCode = $exitCode
        Output = @($output)
    }
}

function Get-ProbeFileState {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [ValidateSet('Any', 'Container', 'Leaf')] [string] $PathType = 'Any'
    )

    $exists = Test-Path -LiteralPath $Path -PathType $PathType
    $length = $null
    if ($exists -and $PathType -eq 'Leaf') {
        try {
            $length = (Get-Item -LiteralPath $Path).Length
        }
        catch {
            $exists = $false
        }
    }

    return [ordered]@{
        Path = [System.IO.Path]::GetFullPath($Path)
        PathType = $PathType
        Exists = [bool]$exists
        Length = $length
    }
}

function Get-ProbeSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([System.BitConverter]::ToString($sha256.ComputeHash($stream))).Replace('-', '').ToUpperInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

Write-Host 'DTMAPI install preflight probe'
Write-Host '[INFO] This probe does not install, uninstall, copy runtime files, or modify the game folder.'
Write-Host '[INFO] It only writes a probe report JSON.'
Write-Host ''

Invoke-ProbeStep -Name 'Package layout and probe host' -Action {
    $script:ProbePackageRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
    $script:ProbePayloadRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Payload'))
    $script:ProbeContext['PackageRoot'] = $script:ProbePackageRoot
    $script:ProbeContext['ToolsDir'] = [System.IO.Path]::GetFullPath($PSScriptRoot)
    $script:ProbeContext['PayloadRoot'] = $script:ProbePayloadRoot
    $script:ProbeContext['CurrentDirectory'] = [System.IO.Directory]::GetCurrentDirectory()
    $script:ProbeContext['EnvironmentGameDir'] = [string]$env:DTMAPI_GAME_DIR
    $script:ProbeContext['EnvironmentStateDir'] = [string]$env:DTMAPI_STATE_DIR
    $script:ProbeContext['EnvironmentRuntimeDir'] = [string]$env:DTMAPI_RUNTIME_DIR
    $script:ProbeContext['NoInstallGuarantee'] = 'No installer, uninstaller, BepInEx installer, copy, remove, or game-launch step is invoked.'

    $required = @(
        (Join-Path $PSScriptRoot 'dtmapi-runtime-version.props'),
        (Join-Path $PSScriptRoot 'common.ps1'),
        (Join-Path $PSScriptRoot 'release-common.ps1'),
        (Join-Path $PSScriptRoot 'install-to-game.ps1'),
        (Join-Path $PSScriptRoot 'install-bepinex.ps1'),
        (Join-Path $PSScriptRoot 'probe-install-preflight.ps1'),
        (Join-Path $PSScriptRoot 'probe-powershell-host.ps1'),
        (Join-Path $PSScriptRoot 'player-doctor\dtmapi-player-doctor.exe'),
        (Join-Path $PSScriptRoot 'player-doctor\dotnet-LICENSE.txt'),
        (Join-Path $PSScriptRoot 'player-doctor\dotnet-ThirdPartyNotices.txt')
    )
    $missing = @($required | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })
    if ($missing.Count -gt 0) {
        return New-ProbeResult -Status 'FAIL' -Message 'Required probe/install scripts are missing from the package.' -Details @{ Missing = @($missing) }
    }

    return New-ProbeResult -Status 'OK' -Message ("Package root: {0}" -f $script:ProbePackageRoot)
} | Out-Null

Invoke-ProbeStep -Name 'PowerShell host self diagnostics' -Action {
    $hosts = @(Get-ProbePowerShellHostCandidates)
    if ($hosts.Count -eq 0) {
        return New-ProbeResult -Status 'FAIL' -Message 'No PowerShell host candidates were found.' -Details @{ Hosts = @() }
    }

    $diagnostics = New-Object 'System.Collections.Generic.List[object]'
    foreach ($hostCandidate in $hosts) {
        Write-Host ("[INFO] Host diagnostic candidate: {0} ({1})" -f $hostCandidate.Path, $hostCandidate.Label)
        $diagnostic = Invoke-ProbePowerShellHostSelfDiagnostic -HostPath $hostCandidate.Path -Label $hostCandidate.Label
        $diagnostics.Add($diagnostic) | Out-Null

        $runtime = $diagnostic.RuntimeInfo
        if ($null -ne $runtime) {
            Write-Host ("[INFO]   Runtime: PS {0} {1}; LanguageMode={2}; Policy={3}; 64Bit={4}" -f $runtime.PSVersion, $runtime.PSEdition, $runtime.LanguageMode, $runtime.ExecutionPolicy, $runtime.Is64BitProcess)
        }
        else {
            Write-Host ("[WARN]   Runtime info probe failed. fileExit={0}" -f $diagnostic.FileRuntimeInfo.ExitCode) -ForegroundColor Yellow
        }

        Write-Host ("[INFO]   CommandMinimal={0} FileRuntime={1} FileParseInput={2} EncodedMinimal={3} EncodedParseInput={4}" -f $diagnostic.CommandMinimal.ExitCode, $diagnostic.FileRuntimeInfo.ExitCode, $diagnostic.FileParseInput.ExitCode, $diagnostic.EncodedMinimal.ExitCode, $diagnostic.EncodedParseInput.ExitCode)
    }

    $allDiagnostics = @($diagnostics.ToArray())
    $fileUsable = @($allDiagnostics | Where-Object { $_.FileRuntimeInfo.Ok -and $_.FileParseInput.Ok }).Count -gt 0
    if (-not $fileUsable) {
        return New-ProbeResult -Status 'FAIL' -Message 'No PowerShell host could run file-based runtime diagnostics and Parser.ParseInput.' -Details @{ Hosts = $allDiagnostics }
    }

    $encodedIssue = @($allDiagnostics | Where-Object { $_.FileParseInput.Ok -and -not $_.EncodedParseInput.Ok }).Count -gt 0
    if ($encodedIssue) {
        return New-ProbeResult -Status 'WARN' -Message 'At least one PowerShell host can run file-based Parser.ParseInput but fails the encoded-command Parser.ParseInput route.' -Details @{ Hosts = $allDiagnostics }
    }

    return New-ProbeResult -Status 'OK' -Message 'PowerShell host self diagnostics passed for at least one host.' -Details @{ Hosts = $allDiagnostics }
} -NonBlocking | Out-Null

Invoke-ProbeStep -Name 'PowerShell host and parser matrix' -Action {
    $installPreflightScriptNames = @('common.ps1', 'release-common.ps1', 'install-to-game.ps1', 'install-bepinex.ps1', 'probe-powershell-host.ps1')
    $supportScriptNames = @('uninstall-dtmapi.ps1', 'check-dtmapi-status.ps1', 'collect-logs.ps1', 'analyze-startup-evidence.ps1', 'probe-install-preflight.ps1')
    $allScriptNames = @($installPreflightScriptNames + $supportScriptNames)
    $strictScripts = @($installPreflightScriptNames | ForEach-Object { Join-Path $PSScriptRoot $_ })
    $allScripts = @($allScriptNames | ForEach-Object { Join-Path $PSScriptRoot $_ })
    $hosts = @(Get-ProbePowerShellHostCandidates)
    if ($hosts.Count -eq 0) {
        return New-ProbeResult -Status 'FAIL' -Message 'No PowerShell host candidates were found.' -Details @{ Hosts = @() }
    }

    $hostResults = New-Object 'System.Collections.Generic.List[object]'
    foreach ($hostCandidate in $hosts) {
        Write-Host ("[INFO] Parser host candidate: {0} ({1})" -f $hostCandidate.Path, $hostCandidate.Label)
        $scriptResults = New-Object 'System.Collections.Generic.List[object]'
        foreach ($scriptPath in $allScripts) {
            if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
                $scriptResults.Add([ordered]@{
                    Script = $scriptPath
                    RequiredForInstall = ($strictScripts -contains $scriptPath)
                    Ok = $false
                    ExitCode = -3
                    Output = @('script missing')
                }) | Out-Null
                continue
            }

            $encodedCheck = Invoke-ProbeParserCheck -HostPath $hostCandidate.Path -ScriptPath $scriptPath
            $fileCheck = Invoke-ProbeParserCheckFile -HostPath $hostCandidate.Path -ScriptPath $scriptPath
            $relativeName = [System.IO.Path]::GetFileName($scriptPath)
            if ($fileCheck.Ok) {
                Write-Host ("[INFO]   OK {0}" -f $relativeName)
                if (-not $encodedCheck.Ok) {
                    Write-Host ("[WARN]     EncodedCommand parser failed but file validator passed. encodedExit={0}" -f $encodedCheck.ExitCode) -ForegroundColor Yellow
                }
            }
            else {
                Write-Host ("[WARN]   FAIL {0} fileExit={1} encodedExit={2}" -f $relativeName, $fileCheck.ExitCode, $encodedCheck.ExitCode) -ForegroundColor Yellow
                foreach ($line in @($fileCheck.Output | Select-Object -First 6)) {
                    if (-not [string]::IsNullOrWhiteSpace([string]$line)) {
                        Write-Host ("[WARN]     {0}" -f ([string]$line).Trim()) -ForegroundColor Yellow
                    }
                }
            }

            $scriptResults.Add([ordered]@{
                Script = $scriptPath
                RequiredForInstall = ($strictScripts -contains $scriptPath)
                Ok = [bool]$fileCheck.Ok
                ExitCode = $fileCheck.ExitCode
                Output = @($fileCheck.Output)
                EncodedCommandOk = [bool]$encodedCheck.Ok
                EncodedCommandExitCode = $encodedCheck.ExitCode
                EncodedCommandOutput = @($encodedCheck.Output)
            }) | Out-Null
        }

        $strictOk = @($scriptResults.ToArray() | Where-Object { $_.RequiredForInstall -and -not $_.Ok }).Count -eq 0
        $strictEncodedOk = @($scriptResults.ToArray() | Where-Object { $_.RequiredForInstall -and -not $_.EncodedCommandOk }).Count -eq 0
        $hostResults.Add([ordered]@{
            Label = $hostCandidate.Label
            Host = $hostCandidate.Path
            StrictInstallScriptsOk = $strictOk
            StrictEncodedCommandInstallScriptsOk = $strictEncodedOk
            Scripts = @($scriptResults.ToArray())
        }) | Out-Null
    }

    $allHostResults = @($hostResults.ToArray())
    $fallbackStrictOk = @($allHostResults | Where-Object { $_.StrictInstallScriptsOk }).Count -gt 0
    $windowsHost = @($allHostResults | Where-Object { $_.Host -like '*\WindowsPowerShell\v1.0\powershell.exe' } | Select-Object -First 1)
    $windowsStrictOk = $false
    if ($windowsHost.Count -gt 0) {
        $windowsStrictOk = [bool]$windowsHost[0].StrictInstallScriptsOk
    }
    $windowsEncodedStrictOk = $false
    if ($windowsHost.Count -gt 0) {
        $windowsEncodedStrictOk = [bool]$windowsHost[0].StrictEncodedCommandInstallScriptsOk
    }

    if (-not $fallbackStrictOk) {
        return New-ProbeResult -Status 'FAIL' -Message 'No discovered PowerShell host can parse all strict pre-install scripts.' -Details @{ Hosts = $allHostResults }
    }
    if ($windowsStrictOk -and -not $windowsEncodedStrictOk) {
        return New-ProbeResult -Status 'WARN' -Message 'Windows PowerShell file-based parsing passed, but EncodedCommand parsing failed; old installer validation may be incompatible with this environment.' -Details @{ Hosts = $allHostResults }
    }
    if (-not $windowsStrictOk) {
        return New-ProbeResult -Status 'WARN' -Message 'Windows PowerShell failed strict pre-install parsing, but at least one fallback host passed.' -Details @{ Hosts = $allHostResults }
    }

    return New-ProbeResult -Status 'OK' -Message 'At least one host can parse all strict pre-install scripts; Windows PowerShell also passed.' -Details @{ Hosts = $allHostResults }
} -NonBlocking | Out-Null

Invoke-ProbeStep -Name 'Resolve Doloc Town game folder' -Action {
    if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
        $resolved = [System.IO.Path]::GetFullPath($GameDir)
        Assert-DtmApiDolocTownGamePath -Path $resolved -Source 'probe -GameDir'
        $script:ProbeGameDir = $resolved
    }
    else {
        $script:ProbeGameDir = Resolve-DolocTownGamePath -RepoRoot (Get-RepoRoot)
    }

    $script:ProbePluginDir = Join-Path $script:ProbeGameDir 'BepInEx\plugins\DTMAPI'
    $script:ProbeStateDir = Resolve-DtmApiStateDir -GameDir $script:ProbeGameDir
    $script:ProbeContext['GameDir'] = $script:ProbeGameDir
    $script:ProbeContext['PluginDir'] = $script:ProbePluginDir
    $script:ProbeContext['StateDir'] = $script:ProbeStateDir

    return New-ProbeResult -Status 'OK' -Message ("Game folder: {0}" -f $script:ProbeGameDir)
} | Out-Null

Invoke-ProbeStep -Name 'Check whether Doloc Town is running' -Action {
    if ([string]::IsNullOrWhiteSpace($script:ProbeGameDir)) {
        return New-ProbeResult -Status 'SKIPPED' -Message 'Skipped because the game folder was not resolved.'
    }

    Assert-DtmApiGameNotRunning -GameDir $script:ProbeGameDir -Operation 'install probe'
    return New-ProbeResult -Status 'OK' -Message 'No DolocTown.exe process is running from this game folder.'
} | Out-Null

Invoke-ProbeStep -Name 'Check package runtime payload files' -Action {
    if ([string]::IsNullOrWhiteSpace($script:ProbePackageRoot) -or [string]::IsNullOrWhiteSpace($script:ProbePayloadRoot)) {
        return New-ProbeResult -Status 'FAIL' -Message 'Package/payload root was not resolved; QA and exact-five gates cannot run.'
    }
    $runtimeRoot = Join-Path $script:ProbePayloadRoot 'BepInEx\plugins\DTMAPI'
    $requiredFiles = @(
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Abstractions.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    )

    $packageRoot = [System.IO.Path]::GetFullPath($script:ProbePackageRoot).TrimEnd('\')
    $forbiddenPackageEntries = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -Force | Where-Object {
        $relative = $_.FullName.Substring($packageRoot.Length).TrimStart('\').Replace('\', '/')
        $name = $_.Name
        $relative -match '(^|/)qa-host(/|$)' -or
        $name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
        $name -match '^(?i:DTMAPI\.(Smoke|Tests)\.(dll|pdb))$' -or
        $name -match '^(?i:qa-settings\.json|smoke-settings\.json|qa-host.*\.json)$'
    })
    if ($forbiddenPackageEntries.Count -gt 0) {
        return New-ProbeResult -Status 'FAIL' -Message 'Player package contains developer-only QA host material.' -Details @{
            PackageRoot = $packageRoot
            Forbidden = @($forbiddenPackageEntries | ForEach-Object {
                $_.FullName.Substring($packageRoot.Length).TrimStart('\').Replace('\', '/')
            } | Sort-Object -Unique)
        }
    }

    $states = New-Object 'System.Collections.Generic.List[object]'
    foreach ($file in $requiredFiles) {
        $states.Add((Get-ProbeFileState -Path (Join-Path $runtimeRoot $file) -PathType Leaf)) | Out-Null
    }
    $states.Add((Get-ProbeFileState -Path (Join-Path $runtimeRoot 'assets\branding\dtmapi-icon.png') -PathType Leaf)) | Out-Null

    $missing = @($states.ToArray() | Where-Object { -not $_.Exists -or ($null -ne $_.Length -and $_.Length -le 0) })
    if ($missing.Count -gt 0) {
        return New-ProbeResult -Status 'FAIL' -Message ("Runtime payload is incomplete: {0} item(s) missing or empty." -f $missing.Count) -Details @{ RuntimeRoot = $runtimeRoot; Files = @($states.ToArray()) }
    }

    $actualDlls = @(Get-ChildItem -LiteralPath $runtimeRoot -Filter '*.dll' -File -Recurse | ForEach-Object { $_.Name } | Sort-Object)
    if (($actualDlls -join '|') -ne (($requiredFiles | Sort-Object) -join '|')) {
        return New-ProbeResult -Status 'FAIL' -Message 'Runtime payload DLL set is not the exact five production assemblies.' -Details @{ Expected = @($requiredFiles); Actual = @($actualDlls) }
    }
    $forbiddenPayloadFiles = @(Get-ChildItem -LiteralPath $runtimeRoot -File -Recurse | Where-Object {
        $_.Name.Equals('manifest.json', [System.StringComparison]::OrdinalIgnoreCase) -or
        $_.Name.Equals('smoke-settings.json', [System.StringComparison]::OrdinalIgnoreCase) -or
        $_.Name.Equals('qa-settings.json', [System.StringComparison]::OrdinalIgnoreCase) -or
        $_.Name.Equals('qa-host-activation.json', [System.StringComparison]::OrdinalIgnoreCase) -or
        $_.Name.Equals('DTMAPI.GameBridge.DolocTown.QA.dll', [System.StringComparison]::OrdinalIgnoreCase) -or
        $_.Name.Equals('DTMAPI.GameBridge.DolocTown.QA.pdb', [System.StringComparison]::OrdinalIgnoreCase)
    })
    if ($forbiddenPayloadFiles.Count -gt 0) {
        return New-ProbeResult -Status 'FAIL' -Message 'Runtime payload contains QA/settings/ordinary-Mod material.' -Details @{ Forbidden = @($forbiddenPayloadFiles | ForEach-Object { $_.FullName }) }
    }

    $releaseManifestPath = Join-Path $script:ProbePackageRoot 'Content\DTMAPI\release-manifest.json'
    try {
        $releaseManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseManifestPath | ConvertFrom-Json
        $optionalRows = @((Get-DtmApiMapValue -Map $releaseManifest -Key 'OptionalComponents' -Default @()))
        if ($optionalRows.Count -eq 0) {
            throw 'release-manifest has no optional component receipts'
        }
        $expectedOptionalPaths = New-Object 'System.Collections.Generic.List[string]'
        foreach ($component in $optionalRows) {
            $relativePath = ([string](Get-DtmApiMapValue -Map $component -Key 'RelativePath' -Default '')).Replace('\', '/')
            if (-not $relativePath.StartsWith('DTMAPI/components/', [System.StringComparison]::Ordinal) -or $relativePath.Contains('../')) {
                throw "unsafe optional component path '$relativePath'"
            }
            $componentPath = Join-Path $script:ProbePayloadRoot ($relativePath.Replace('/', '\'))
            $expectedOptionalPaths.Add([System.IO.Path]::GetFullPath($componentPath)) | Out-Null
            $item = Get-Item -LiteralPath $componentPath -ErrorAction Stop
            $hash = (Get-FileHash -LiteralPath $componentPath -Algorithm SHA256).Hash
            $managedName = [System.Reflection.AssemblyName]::GetAssemblyName($componentPath)
            if ([long]$item.Length -ne [long](Get-DtmApiMapValue -Map $component -Key 'Length' -Default 0) -or
                -not [string]::Equals($hash, [string](Get-DtmApiMapValue -Map $component -Key 'Sha256' -Default ''), [System.StringComparison]::OrdinalIgnoreCase) -or
                -not [string]::Equals([string]$managedName.Name, [string](Get-DtmApiMapValue -Map $component -Key 'AssemblyName' -Default ''), [System.StringComparison]::Ordinal) -or
                -not [string]::Equals([string]$managedName.Version, [string](Get-DtmApiMapValue -Map $component -Key 'AssemblyVersion' -Default ''), [System.StringComparison]::Ordinal) -or
                -not [string]::Equals([string](Get-DtmApiMapValue -Map $component -Key 'Distribution' -Default ''), 'dormant-shipped', [System.StringComparison]::Ordinal)) {
                throw "optional component receipt mismatch for '$relativePath'"
            }
        }
        $componentRoot = Join-Path $script:ProbePayloadRoot 'DTMAPI\components'
        $actualOptionalPaths = @(Get-ChildItem -LiteralPath $componentRoot -File -Recurse | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullName) } | Sort-Object)
        if ((@($expectedOptionalPaths.ToArray() | Sort-Object) -join '|') -ne ($actualOptionalPaths -join '|')) {
            throw 'optional component payload file set is not exact'
        }
    }
    catch {
        return New-ProbeResult -Status 'FAIL' -Message ('Dormant-shipped optional component validation failed: ' + $_.Exception.Message) -Details @{ ReleaseManifest = $releaseManifestPath }
    }

    $playerDoctorRoot = Join-Path $PSScriptRoot 'player-doctor'
    $playerDoctorExe = Join-Path $playerDoctorRoot 'dtmapi-player-doctor.exe'
    $playerDoctorFiles = @(Get-ChildItem -LiteralPath $playerDoctorRoot -File -Recurse | ForEach-Object {
        $_.FullName.Substring($playerDoctorRoot.TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
    } | Sort-Object)
    $expectedPlayerDoctorFiles = @('dtmapi-player-doctor.exe', 'dotnet-LICENSE.txt', 'dotnet-ThirdPartyNotices.txt') | Sort-Object
    if (($playerDoctorFiles -join '|') -ne ($expectedPlayerDoctorFiles -join '|')) {
        return New-ProbeResult -Status 'FAIL' -Message 'Player Doctor file set is not exact.' -Details @{ Expected = @($expectedPlayerDoctorFiles); Actual = @($playerDoctorFiles) }
    }
    $doctorVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($playerDoctorExe)
    if (-not [string]::Equals([string]$doctorVersion.FileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$doctorVersion.ProductVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal)) {
        return New-ProbeResult -Status 'FAIL' -Message 'Player Doctor version does not match the Runtime package.' -Details @{ FileVersion = [string]$doctorVersion.FileVersion; ProductVersion = [string]$doctorVersion.ProductVersion }
    }

    return New-ProbeResult -Status 'OK' -Message ("Runtime payload and read-only Player Doctor are complete: {0}" -f $runtimeRoot) -Details @{ RuntimeRoot = $runtimeRoot; Files = @($states.ToArray()); PlayerDoctor = $playerDoctorExe }
} | Out-Null

Invoke-ProbeStep -Name 'Check current BepInEx/Doorstop state' -Action {
    if ([string]::IsNullOrWhiteSpace($script:ProbeGameDir)) {
        return New-ProbeResult -Status 'SKIPPED' -Message 'Skipped because the game folder was not resolved.'
    }

    $required = @(Get-DtmApiBepInExRequiredInstallFiles -GameDir $script:ProbeGameDir)
    $states = New-Object 'System.Collections.Generic.List[object]'
    foreach ($item in $required) {
        $state = Get-ProbeFileState -Path $item.Path -PathType $item.PathType
        $state['Label'] = $item.Label
        $states.Add($state) | Out-Null
    }
    $doorstopEnabled = Test-DtmApiDoorstopConfigEnabledForBepInEx -Path (Join-Path $script:ProbeGameDir 'doorstop_config.ini')
    $missing = @($states.ToArray() | Where-Object { -not $_.Exists -or ($null -ne $_.Length -and $_.Length -le 0) })
    if ($missing.Count -eq 0 -and $doorstopEnabled) {
        return New-ProbeResult -Status 'OK' -Message 'BepInEx/Doorstop is already complete.' -Details @{ Files = @($states.ToArray()); DoorstopConfigEnabled = $doorstopEnabled }
    }

    return New-ProbeResult -Status 'WARN' -Message ("BepInEx/Doorstop is incomplete; installer would try the bundled offline BepInEx path. Missing/invalid: {0}" -f $missing.Count) -Details @{ Files = @($states.ToArray()); DoorstopConfigEnabled = $doorstopEnabled }
} -NonBlocking | Out-Null

Invoke-ProbeStep -Name 'Check bundled BepInEx offline package' -Action {
    $assetName = 'BepInEx_win_x64_5.4.23.5.zip'
    $expectedSha256 = '82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4'
    $zip = Join-Path ([System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))) ".tools\bepinex\$assetName"
    if (-not (Test-Path -LiteralPath $zip -PathType Leaf)) {
        return New-ProbeResult -Status 'FAIL' -Message ("Bundled BepInEx offline package is missing: {0}" -f $zip) -Details @{ Path = $zip; ExpectedSha256 = $expectedSha256 }
    }

    $actual = Get-ProbeSha256 -Path $zip
    if ($actual -ne $expectedSha256) {
        return New-ProbeResult -Status 'FAIL' -Message 'Bundled BepInEx offline package hash mismatch.' -Details @{ Path = $zip; ExpectedSha256 = $expectedSha256; ActualSha256 = $actual }
    }

    return New-ProbeResult -Status 'OK' -Message 'Bundled BepInEx offline package is present and hash-valid.' -Details @{ Path = $zip; Sha256 = $actual }
} | Out-Null

Invoke-ProbeStep -Name 'Check existing DTMAPI install state files' -Action {
    if ([string]::IsNullOrWhiteSpace($script:ProbeStateDir)) {
        return New-ProbeResult -Status 'SKIPPED' -Message 'Skipped because the state folder was not resolved.'
    }

    $installState = Join-Path $script:ProbeStateDir 'install-state.json'
    $releaseManifest = Join-Path $script:ProbeStateDir 'release-manifest.json'
    $failedStates = @()
    if (Test-Path -LiteralPath $script:ProbeStateDir -PathType Container) {
        $failedStates = @(Get-ChildItem -LiteralPath $script:ProbeStateDir -Filter 'install-state.failed-*.json' -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 5 |
            ForEach-Object {
                [ordered]@{
                    Path = $_.FullName
                    LastWriteTime = $_.LastWriteTime.ToString('o')
                    Length = $_.Length
                }
            })
    }

    $details = [ordered]@{
        StateDir = $script:ProbeStateDir
        InstallState = Get-ProbeFileState -Path $installState -PathType Leaf
        ReleaseManifest = Get-ProbeFileState -Path $releaseManifest -PathType Leaf
        RecentFailedInstallStates = @($failedStates)
    }

    if (@($failedStates).Count -gt 0) {
        return New-ProbeResult -Status 'WARN' -Message ("Found {0} recent failed install state file(s); retained for diagnostics only." -f @($failedStates).Count) -Details $details
    }

    return New-ProbeResult -Status 'OK' -Message 'No recent failed install state files found.' -Details $details
} -NonBlocking | Out-Null

Invoke-ProbeStep -Name 'Detect legacy DLK/SMAPI items' -Action {
    if ([string]::IsNullOrWhiteSpace($script:ProbeGameDir)) {
        return New-ProbeResult -Status 'SKIPPED' -Message 'Skipped because the game folder was not resolved.'
    }

    $items = @(Get-DtmApiLegacyDetections -GameDir $script:ProbeGameDir)
    if ($items.Count -gt 0) {
        return New-ProbeResult -Status 'WARN' -Message ("Detected {0} legacy item(s). The probe does not move or delete them." -f $items.Count) -Details @{ Items = @($items) }
    }

    return New-ProbeResult -Status 'OK' -Message 'No legacy DLK/SMAPI items detected.' -Details @{ Items = @() }
} -NonBlocking | Out-Null

Invoke-ProbeStep -Name 'Summarize planned install destination' -Action {
    if ([string]::IsNullOrWhiteSpace($script:ProbeGameDir)) {
        return New-ProbeResult -Status 'SKIPPED' -Message 'Skipped because the game folder was not resolved.'
    }

    return New-ProbeResult -Status 'OK' -Message ("If installed, DTMAPI Runtime would be copied to: {0}" -f $script:ProbePluginDir) -Details @{
        GameDir = $script:ProbeGameDir
        PluginDir = $script:ProbePluginDir
        StateDir = $script:ProbeStateDir
        NoFilesInstalled = $true
    }
} | Out-Null

$blockingFailures = @($script:ProbeSteps.ToArray() | Where-Object { $_.Status -eq 'FAIL' -and $_.Blocking })
$finalStatus = if ($blockingFailures.Count -gt 0) { 'blocked' } else { 'complete' }
$reportPath = Write-ProbeReport -Status $finalStatus
$steps = @($script:ProbeSteps.ToArray())
$okCount = @($steps | Where-Object { $_.Status -eq 'OK' }).Count
$warnCount = @($steps | Where-Object { $_.Status -eq 'WARN' }).Count
$failCount = @($steps | Where-Object { $_.Status -eq 'FAIL' }).Count
$skippedCount = @($steps | Where-Object { $_.Status -eq 'SKIPPED' }).Count

Write-Host ''
Write-Host '[INFO] Probe summary'
Write-Host ("[INFO]   OK={0} WARN={1} FAIL={2} SKIPPED={3}" -f $okCount, $warnCount, $failCount, $skippedCount)
if (-not [string]::IsNullOrWhiteSpace($reportPath)) {
    Write-Host ("[INFO]   Report: {0}" -f $reportPath)
}
if ($blockingFailures.Count -gt 0) {
    Write-Host ("[ERROR] Blocking preflight failure count: {0}" -f $blockingFailures.Count) -ForegroundColor Red
    foreach ($failure in $blockingFailures) {
        Write-Host ("[ERROR]   {0}: {1}" -f $failure.Name, $failure.Message) -ForegroundColor Red
    }
    exit 1
}

if ($warnCount -gt 0) {
    Write-Host '[WARN] Probe finished with warnings; inspect the report before deciding whether to install.' -ForegroundColor Yellow
}
else {
    Write-Host '[OK] Probe finished without warnings.' -ForegroundColor Green
}
exit 0
