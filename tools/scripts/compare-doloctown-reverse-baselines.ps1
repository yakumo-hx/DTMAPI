[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $BeforeBuildRoot,
    [Parameter(Mandatory = $true)][string] $AfterBuildRoot,
    [string] $OutputDirectory,
    [ValidateSet('code', 'resources', 'all')][string] $Scope = 'code',
    [string[]] $AssetPath = @()
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/reverse-baseline-path-safety.ps1"
$repo = Get-RepoRoot
$before = (Resolve-Path -LiteralPath $BeforeBuildRoot).Path
$after = (Resolve-Path -LiteralPath $AfterBuildRoot).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $after ('diffs/compare-' + (Split-Path -Leaf $before)) }
$output = Resolve-DtmApiReverseBaselineBuildRoot -RepoRoot $repo -BuildRoot $OutputDirectory
$arguments = @((Join-Path $repo 'tools/reverse-capture/compare_baselines.py'), '--before', $before, '--after', $after, '--output-dir', $output, '--scope', $Scope)
foreach ($asset in $AssetPath) { $arguments += @('--asset', $asset) }
& python @arguments
if ($LASTEXITCODE -ne 0) { throw "Reverse baseline comparison failed, exit $LASTEXITCODE." }
