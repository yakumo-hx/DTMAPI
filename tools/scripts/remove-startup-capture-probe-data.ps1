param(
    [string] $OutputRoot = '',
    [string] $ToolRoot = '',
    [switch] $ConfirmCleanup
)

$ErrorActionPreference = 'Stop'

function Resolve-DtmProbeDesktop {
    $desktop = [Environment]::GetFolderPath('Desktop')
    if ([string]::IsNullOrWhiteSpace($desktop)) {
        $desktop = Join-Path $env:USERPROFILE 'Desktop'
    }
    return [System.IO.Path]::GetFullPath($desktop)
}

function Assert-DtmProbeCleanupPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $ExpectedLeaf
    )

    $full = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')
    $root = [System.IO.Path]::GetPathRoot($full).TrimEnd('\')
    if ([string]::IsNullOrWhiteSpace($full) -or $full -eq $root -or
        [System.IO.Path]::GetFileName($full) -ine $ExpectedLeaf) {
        throw "拒绝清理非探针专用路径：$full（预期末级目录：$ExpectedLeaf）"
    }
    return $full
}

function Get-DtmProbePathLength {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return 0L
    }
    return [long](@(Get-ChildItem -LiteralPath $Path -Recurse -Force -File -ErrorAction SilentlyContinue) |
        Measure-Object -Property Length -Sum).Sum
}

$desktop = Resolve-DtmProbeDesktop
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $desktop 'DTMAPI-startup-capture'
}
if ([string]::IsNullOrWhiteSpace($ToolRoot)) {
    $ToolRoot = Join-Path $env:LOCALAPPDATA 'DTMAPI\tools\ProcessMonitor'
}

$OutputRoot = Assert-DtmProbeCleanupPath -Path $OutputRoot -ExpectedLeaf 'DTMAPI-startup-capture'
$ToolRoot = Assert-DtmProbeCleanupPath -Path $ToolRoot -ExpectedLeaf 'ProcessMonitor'
$fallbackErrorPath = Join-Path (Split-Path -Parent $OutputRoot) 'DTMAPI-startup-capture-last-error.txt'

Write-Host '此操作只清理“DTMAPI 启动抓取小探针”产生的数据：'
Write-Host "  1. 抓取结果：$OutputRoot"
Write-Host "  2. 微软 Process Monitor 缓存：$ToolRoot"
Write-Host "  3. 失败记录：$fallbackErrorPath"
Write-Host '不会删除游戏、DTMAPI Runtime、BepInEx、MOD、配置或存档。'
Write-Host '请确认已经把需要的 DTMAPI-startup-capture-*.zip 回传给维护者。'

if (-not $ConfirmCleanup) {
    $answer = Read-Host '输入 DELETE 确认清理；直接关闭窗口或输入其他内容取消'
    if ($answer -cne 'DELETE') {
        Write-Host '已取消；没有删除任何内容。'
        exit 2
    }
}

$procmon = @(Get-Process -Name 'Procmon', 'Procmon64', 'Procmon64a' -ErrorAction SilentlyContinue)
if ($procmon.Count -gt 0) {
    throw '检测到 Process Monitor 仍在运行。请先关闭它，再重新运行清理。'
}

$bytes = (Get-DtmProbePathLength -Path $OutputRoot) + (Get-DtmProbePathLength -Path $ToolRoot)
$removed = New-Object 'System.Collections.Generic.List[string]'
foreach ($path in @($OutputRoot, $ToolRoot, $fallbackErrorPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        continue
    }
    Remove-Item -LiteralPath $path -Recurse -Force
    $removed.Add($path) | Out-Null
}

Write-Host ''
Write-Host "[OK] 已清理 $($removed.Count) 个探针路径，约 $([math]::Round($bytes / 1MB, 2)) MB。" -ForegroundColor Green
if ($removed.Count -eq 0) {
    Write-Host '[INFO] 没有发现需要清理的探针数据。'
}
Write-Host '[INFO] 现在只需手动删除解压出来的整个探针文件夹。'
exit 0
