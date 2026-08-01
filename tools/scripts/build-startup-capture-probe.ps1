param(
    [string] $OutputRoot = '',
    [string] $ArchivePath = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo 'dist\player-probes\20260711-startup-capture'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$packageName = 'DTMAPI-startup-capture-probe'
$packageRoot = Join-Path $OutputRoot $packageName
$toolsRoot = Join-Path $packageRoot 'Content\DTMAPIInstaller\tools'

if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $toolsRoot | Out-Null

foreach ($batName in @('5_capture_dtmapi_startup.bat', '6_remove_startup_capture_probe_data.bat')) {
    $source = Join-Path $repo "tools\release\startup-capture-probe\$batName"
    $destination = Join-Path $packageRoot $batName
    $text = [System.IO.File]::ReadAllText($source)
    $text = [regex]::Replace($text, "\r?\n", "`r`n")
    [System.IO.File]::WriteAllText($destination, $text, (New-Object System.Text.UTF8Encoding($false)))
}

foreach ($scriptName in @(
    'common.ps1',
    'capture-startup-trace.ps1',
    'collect-logs.ps1',
    'analyze-startup-evidence.ps1',
    'remove-startup-capture-probe-data.ps1'
)) {
    Copy-DtmApiTextFileUtf8Bom -Source (Join-Path $PSScriptRoot $scriptName) -Destination (Join-Path $toolsRoot $scriptName)
}

$readme = @'
DTMAPI 启动抓取小探针

使用前：
1. 必须先解压整个 ZIP，不能直接在压缩包内运行。
2. 彻底关闭多洛可小镇。

抓取：
1. 双击 5_capture_dtmapi_startup.bat。
2. 管理员权限弹窗选择“是”。
3. 按提示直接按 Enter。
4. 工具会自动启动游戏、抓取约一分钟，然后自动关闭本次启动的游戏。
5. 把桌面 DTMAPI-startup-capture 文件夹内最新的 DTMAPI-startup-capture-*.zip 私下发给维护者。

诊断范围：
- 自动记录有限的启动父进程链、已知 Loader 环境、兼容/缓解配置；
- 自动记录安全产品、与游戏/Loader 相关的限时安全事件、关键文件权限和 Zone 标记；
- 不导出完整环境变量、Defender 排除项或完整 Windows 事件日志；疑似令牌/密码变量会隐藏值。

回传后的清理：
1. 确认已经把 ZIP 发给维护者。
2. 双击 6_remove_startup_capture_probe_data.bat。
3. 按提示输入 DELETE。
4. 清理成功后，手动删除整个解压出来的 DTMAPI-startup-capture-probe 文件夹。

6 只删除此小探针创建的桌面抓取结果、失败记录和微软 Process Monitor 缓存；
不会删除游戏、DTMAPI Runtime、BepInEx、MOD、配置或存档。
'@
[System.IO.File]::WriteAllText((Join-Path $packageRoot '使用说明.txt'), $readme, (New-Object System.Text.UTF8Encoding($true)))

$manifest = @(
    'Probe=DTMAPI startup capture'
    'ProbeVersion=2'
    'Standalone=True'
    'PublishedWithRuntime=False'
    'IncludesProcessMonitorBinary=False'
    'CaptureEntry=5_capture_dtmapi_startup.bat'
    'CleanupEntry=6_remove_startup_capture_probe_data.bat'
) -join "`r`n"
[System.IO.File]::WriteAllText((Join-Path $packageRoot 'PROBE-MANIFEST.txt'), $manifest + "`r`n", (New-Object System.Text.UTF8Encoding($false)))

if ([string]::IsNullOrWhiteSpace($ArchivePath)) {
    $ArchivePath = Join-Path $repo 'dist\DTMAPI-player-probe-20260711-startup-capture.zip'
}
$ArchivePath = [System.IO.Path]::GetFullPath($ArchivePath)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ArchivePath) | Out-Null
if (Test-Path -LiteralPath $ArchivePath -PathType Leaf) {
    Remove-Item -LiteralPath $ArchivePath -Force
}
Compress-Archive -LiteralPath $packageRoot -DestinationPath $ArchivePath -CompressionLevel Optimal

$redistributedProcmon = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -File | Where-Object {
    $_.Name -match '^(?i)Procmon(64|64a)?\.exe$|^ProcessMonitor\.zip$'
})
if ($redistributedProcmon.Count -gt 0) {
    throw "Probe package must not redistribute Process Monitor: $($redistributedProcmon.FullName -join ', ')"
}

$archive = Get-Item -LiteralPath $ArchivePath
$hash = (Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash
Write-Host "Standalone startup capture probe written to $ArchivePath"
Write-Host "SHA256=$hash"
Write-Host "Length=$($archive.Length)"
