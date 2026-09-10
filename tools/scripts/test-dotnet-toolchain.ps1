[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
$repo = Get-RepoRoot
$root = Join-Path $repo ('tmp/test-runs/dotnet-toolchain-' + [guid]::NewGuid().ToString('N'))
$checks = 0
function Assert-Toolchain([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}

try {
    $hostPath = Get-DotNetExe -RepoRoot $repo -NoProvision
    Push-Location -LiteralPath $repo
    try {
        $actualSdk = [string](& $hostPath --version)
        Assert-Toolchain ($LASTEXITCODE -eq 0 -and $actualSdk -match '^8\.0\.4\d\d$') 'The selected real host did not execute the workspace SDK.'
    }
    finally { Pop-Location }
    $policyText = Get-Content -LiteralPath (Join-Path $repo 'global.json') -Raw -Encoding UTF8
    $policy = $policyText | ConvertFrom-Json
    Assert-Toolchain ([version]$actualSdk -ge [version]$policy.sdk.version) 'The selected SDK is older than global.json.'

    New-Item -ItemType Directory -Path $root -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $root 'global.json'), $policyText, [Text.Encoding]::UTF8)
    $script:fakeDotnet = Join-Path $root 'fake-dotnet.ps1'
    @'
param([string] $Argument)
$case = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'host.json') -Raw | ConvertFrom-Json
switch ($Argument) {
    '--version' {
        if ((Get-Location).Path -ine $case.repo -or $case.failVersion) { exit 1 }
        Write-Output $case.selected
        exit 0
    }
    '--list-sdks' { Write-Output '8.0.421 [public-sdk]'; Write-Output '10.0.100 [public-sdk]'; exit 0 }
    '--list-runtimes' { Write-Output $case.runtimes; exit 0 }
    default { throw 'The toolchain probe attempted an operation other than SDK/runtime inspection.' }
}
'@ | Set-Content -LiteralPath $script:fakeDotnet -Encoding UTF8
    # These hosts all advertise an installed SDK 8, including the unusable ones.
    # The selected executable command and its runtime must both satisfy the policy.
    foreach ($case in @(
        @{ selected = '8.0.421'; accepted = $true },
        @{ selected = '8.0.422'; accepted = $true },
        @{ selected = '8.0.420'; accepted = $false },
        @{ selected = '8.0.399'; accepted = $false },
        @{ selected = '8.0.500'; accepted = $false },
        @{ selected = '9.0.100'; accepted = $false },
        @{ selected = '10.0.100'; accepted = $false },
        @{ selected = '8.0.421-preview.1'; accepted = $false },
        @{ selected = '8.0.421'; accepted = $false; failVersion = $true },
        @{ selected = '8.0.421'; accepted = $false; noRuntime8 = $true }
    )) {
        $value = @{
            repo = $root; selected = $case.selected; failVersion = [bool]$case['failVersion']
            runtimes = if ($case['noRuntime8']) { @('Microsoft.NETCore.App 10.0.0 [runtime]') } else { @('Microsoft.NETCore.App 8.0.27 [runtime]', 'Microsoft.NETCore.App 10.0.0 [runtime]') }
        }
        $value | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'host.json') -Encoding UTF8
        $locationBefore = (Get-Location).Path
        Assert-Toolchain ((Test-DtmApiDotNet8Toolchain -Path $script:fakeDotnet -RepoRoot $root) -eq $case.accepted) ("Wrong toolchain classification: " + ($value | ConvertTo-Json -Compress))
        Assert-Toolchain ((Get-Location).Path -ceq $locationBefore) 'The SDK probe changed the caller location.'
    }
    function Get-Command {
        param([string] $Name)
        if ($Name -eq 'dotnet') { return [pscustomobject]@{ Source = $script:fakeDotnet } }
        return Microsoft.PowerShell.Core\Get-Command @PSBoundParameters
    }
    @{ repo = $root; selected = '10.0.100'; failVersion = $false; runtimes = @('Microsoft.NETCore.App 8.0.27 [runtime]') } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'host.json') -Encoding UTF8
    $rejected = $false
    try { Get-DotNetExe -RepoRoot $root -NoProvision | Out-Null } catch { $rejected = $_.Exception.Message -match 'global.json' }
    Assert-Toolchain $rejected 'NoProvision accepted a PATH host selecting SDK 10 merely because SDK/runtime 8 was installed.'
    Assert-Toolchain (-not (Test-Path -LiteralPath (Join-Path $root '.tools'))) 'NoProvision created a local SDK/download directory.'

    @{ repo = $root; selected = '8.0.421'; failVersion = $false; runtimes = @('Microsoft.NETCore.App 8.0.27 [runtime]') } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'host.json') -Encoding UTF8
    Assert-Toolchain ((Get-DotNetExe -RepoRoot $root -NoProvision) -ceq $script:fakeDotnet) 'A PATH host executing the required SDK was not selected.'
    $policy.sdk.rollForward = 'latestMajor'
    $policy | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'global.json') -Encoding UTF8
    Assert-Toolchain (-not (Test-DtmApiDotNet8Toolchain -Path $script:fakeDotnet -RepoRoot $root)) 'A policy allowing major roll-forward was accepted.'
    Write-Host "Dotnet toolchain: PASS ($checks checks; real SDK $actualSdk; host $hostPath; no build or provisioning)."
}
finally {
    $resolved = [IO.Path]::GetFullPath($root)
    $boundary = [IO.Path]::GetFullPath((Join-Path $repo 'tmp/test-runs')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($boundary, [StringComparison]::OrdinalIgnoreCase)) { throw 'Refusing toolchain fixture cleanup outside tmp/test-runs.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
exit 0
