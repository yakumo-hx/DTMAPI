[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$sdkRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$sdkDotnet = Join-Path $sdkRoot 'toolchain/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $sdkDotnet -PathType Leaf)) {
    throw "The complete DTMAPI SDK toolchain is missing: $sdkDotnet. Extract the full SDK ZIP."
}
$env:DTMAPI_AUTHOR_SDK_ROOT = $sdkRoot
$env:DTMAPI_AUTHOR_DOTNET = $sdkDotnet
$env:DOTNET_ROOT = Split-Path -Parent $sdkDotnet
$env:PATH = $env:DOTNET_ROOT + [IO.Path]::PathSeparator + $sdkRoot + [IO.Path]::PathSeparator + $env:PATH
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
Write-Host 'DTMAPI SDK environment is active in this process and its children.'
Write-Host 'Run dtmapi-author commands here; open your IDE from this shell to use the bundled SDK.'
