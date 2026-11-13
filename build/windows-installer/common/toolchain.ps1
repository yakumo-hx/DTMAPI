# Shared toolchain helpers. Definitions only; PowerShell 5.1 compatible.
function Test-DtmApiDotNet8Toolchain {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [string] $RepoRoot = (Get-RepoRoot)
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -and $Path -match '(?i)\.exe$') { return $false }
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

function Get-DotNetExe {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $NoProvision
    )

    $windowsHost = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
    if ($env:DTMAPI_DOTNET_EXE) {
        if (-not (Test-DtmApiDotNet8Toolchain -Path $env:DTMAPI_DOTNET_EXE -RepoRoot $RepoRoot)) {
            throw 'DTMAPI_DOTNET_EXE does not provide the stable .NET 8 SDK selected by global.json.'
        }
        return (Resolve-Path -LiteralPath $env:DTMAPI_DOTNET_EXE).Path
    }
    $localDotnet = Join-Path $RepoRoot $(if($windowsHost) { '.tools/dotnet/dotnet.exe' } else { '.tools/dotnet/dotnet' })
    if (Test-DtmApiDotNet8Toolchain -Path $localDotnet -RepoRoot $RepoRoot) {
        return (Resolve-Path -LiteralPath $localDotnet).Path
    }

    $systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($systemDotnet -and (Test-DtmApiDotNet8Toolchain -Path $systemDotnet.Source -RepoRoot $RepoRoot)) {
        return $systemDotnet.Source
    }

    if ($NoProvision -or -not $windowsHost) {
        throw 'No .NET host can execute the stable SDK selected by this workspace global.json with a .NET 8 runtime. Run build/prepare-workspace.ps1 -Dependency DotNet.'
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

function Get-DtmApiPowerShellHost {
    param([switch] $RequireWindowsPowerShell)
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        if ($RequireWindowsPowerShell) { return '' }
        $nativeHost = if ($PSVersionTable.PSEdition -eq 'Core') { (Get-Process -Id $PID).Path } else { (Get-Command pwsh -ErrorAction SilentlyContinue).Source }
        if ($nativeHost -and $nativeHost -notmatch '(?i)\.exe$') { return $nativeHost }
        return ''
    }

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
