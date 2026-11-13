# 公开构建所需的路径、文件和工具链函数。
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
foreach ($part in @('repository','files','toolchain','game-path')) {
    . (Join-Path $PSScriptRoot "common/$part.ps1")
}
