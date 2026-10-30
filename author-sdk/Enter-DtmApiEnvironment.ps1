[CmdletBinding()]
param()
$sdkRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$env:DTMAPI_AUTHOR_SDK_ROOT=$sdkRoot
$env:DtmApiReferences=Join-Path $sdkRoot 'references'
$env:PATH=$sdkRoot+[IO.Path]::PathSeparator+$env:PATH
$bundled=Join-Path $sdkRoot 'toolchain/dotnet.exe'
if(Test-Path -LiteralPath $bundled){$env:DTMAPI_DOTNET_EXE=$bundled;$env:DOTNET_ROOT=Split-Path -Parent $bundled;$env:PATH=$env:DOTNET_ROOT+[IO.Path]::PathSeparator+$env:PATH}
Write-Host 'Optional author tools and API reference paths are available in this shell. Standard dotnet projects remain independent.'
