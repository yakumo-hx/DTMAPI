# 架构检查：工程引用、平台代码里的 Mod 名字、代码里的阶段号。规则见 docs/architecture.md 的"架构检查"一节。
[CmdletBinding()]
param([ValidateSet('Debug', 'Release')] [string] $Configuration = 'Release')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/windows-installer/common.ps1"
$architectureRepo = Get-RepoRoot
$architectureDotnet = Get-DotNetExe -RepoRoot $architectureRepo
& $architectureDotnet test (Join-Path $architectureRepo 'tests/DTMAPI.Architecture.Tests/DTMAPI.Architecture.Tests.csproj') -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    throw '架构检查没有通过。新出现的问题要改代码解决；已经修好的问题，从 tests/DTMAPI.Architecture.Tests/known-violations.txt 删掉对应的行。'
}
