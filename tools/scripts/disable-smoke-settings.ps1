param()

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$dtmapiDir = Join-Path $gameDir 'DTMAPI'
New-Item -ItemType Directory -Force -Path $dtmapiDir | Out-Null

@{
    Enabled = $false
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dtmapiDir 'smoke-settings.json')

Write-Host "Disabled smoke settings at $(Join-Path $dtmapiDir 'smoke-settings.json')"
