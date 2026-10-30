# Shared repository helpers. Definitions only; PowerShell 5.1 compatible.
function Get-RepoRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path -LiteralPath (Join-Path $scriptDir '../../..')).Path
}

function Get-DtmApiObjectProperty {
    param(
        $Object,
        [Parameter(Mandatory = $true)] [string] $Name,
        $Default = $null
    )

    if ($null -ne $Object -and $Object.PSObject.Properties[$Name]) {
        return $Object.$Name
    }
    return $Default
}

function Invoke-DtmApiGitValue {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        return ''
    }

    $output = & $git.Source -C $RepoRoot @Arguments 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $output) {
        return ''
    }

    return [string]($output | Select-Object -First 1)
}

function Get-DtmApiGitCommonDir {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $commonDir = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('rev-parse', '--git-common-dir')
    if (-not [string]::IsNullOrWhiteSpace($commonDir)) {
        if (-not [System.IO.Path]::IsPathRooted($commonDir)) {
            $commonDir = Join-Path $RepoRoot $commonDir
        }
        return [System.IO.Path]::GetFullPath($commonDir)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot '.git'))
}

function Get-DtmApiCurrentWorktree {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $worktree = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('rev-parse', '--show-toplevel')
    if ([string]::IsNullOrWhiteSpace($worktree)) {
        return [System.IO.Path]::GetFullPath($RepoRoot)
    }
    return [System.IO.Path]::GetFullPath($worktree)
}

function Get-DtmApiCurrentBranch {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $branch = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('branch', '--show-current')
    if ([string]::IsNullOrWhiteSpace($branch)) {
        return '(detached)'
    }
    return $branch
}

function Get-DtmApiCurrentCommit {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $commit = Invoke-DtmApiGitValue -RepoRoot $RepoRoot -Arguments @('rev-parse', '--short=12', 'HEAD')
    if ([string]::IsNullOrWhiteSpace($commit)) {
        return '(unknown)'
    }
    return $commit
}

function Read-LocalSettings {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $path = Join-Path $RepoRoot 'local.settings.json'
    if (Test-Path $path) {
        return Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
    }
    return $null
}

function Get-DtmapiOutputDir {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Configuration = 'Release'
    )
    return Join-Path $RepoRoot "src\DTMAPI.BepInExBootstrap\bin\$Configuration\netstandard2.0"
}
