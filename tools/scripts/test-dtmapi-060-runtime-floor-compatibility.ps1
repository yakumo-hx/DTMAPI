[CmdletBinding()]
param(
    [string] $RetainedRuntimeRoot = '',
    [string] $RetainedArtifactsRoot = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
$runtimeWorkshopId = [string]$catalog.runtime.workshopId

function Assert-RuntimeFloor {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )
    if (-not $Condition) {
        throw $Message
    }
}

if ([string]::IsNullOrWhiteSpace($RetainedRuntimeRoot)) {
    if ([string]::IsNullOrWhiteSpace($RetainedArtifactsRoot)) {
        $RetainedArtifactsRoot = Join-Path (Split-Path -Parent $repo) 'DTMAPI-retained-artifacts'
    }
    $candidateAuthorityRoot = Join-Path ([System.IO.Path]::GetFullPath($RetainedArtifactsRoot)) 'release-candidates'
    Assert-RuntimeFloor (Test-Path -LiteralPath $candidateAuthorityRoot -PathType Container) `
        "Retained release-candidate authority root is unavailable: $candidateAuthorityRoot"

    $matches = New-Object 'System.Collections.Generic.List[object]'
    foreach ($candidateDirectory in @(Get-ChildItem -LiteralPath $candidateAuthorityRoot -Directory -Force)) {
        $candidatePath = Join-Path $candidateDirectory.FullName 'candidate.json'
        if (-not (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
            continue
        }
        try {
            $candidate = Get-Content -LiteralPath $candidatePath -Raw -Encoding UTF8 | ConvertFrom-Json
            $selected = @($candidate.selectedWorkshopIds | ForEach-Object { [string]$_ })
            $published = @($candidate.publishWorkshopIds | ForEach-Object { [string]$_ })
            if ([int]$candidate.schemaVersion -ne 1 -or
                $runtimeWorkshopId -notin $selected -or
                $runtimeWorkshopId -notin $published) {
                continue
            }
            $runtimeRoot = Join-Path $candidateDirectory.FullName $runtimeWorkshopId
            $infoPath = Join-Path $runtimeRoot 'info.json'
            $corePath = Join-Path $runtimeRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'
            $abstractionsPath = Join-Path $runtimeRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Abstractions.dll'
            if (-not (Test-Path -LiteralPath $infoPath -PathType Leaf) -or
                -not (Test-Path -LiteralPath $corePath -PathType Leaf) -or
                -not (Test-Path -LiteralPath $abstractionsPath -PathType Leaf)) {
                continue
            }
            $info = Get-Content -LiteralPath $infoPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ([string]$info.version -cne '0.5.5') {
                continue
            }
            $matches.Add([pscustomobject]@{
                CandidateRoot = $candidateDirectory.FullName
                RuntimeRoot = $runtimeRoot
                CorePath = $corePath
                AbstractionsPath = $abstractionsPath
            }) | Out-Null
        }
        catch {
            continue
        }
    }
    Assert-RuntimeFloor ($matches.Count -eq 1) `
        "Expected one retained manually accepted Runtime 0.5.5 candidate, found $($matches.Count)."
    $retained = $matches[0]
    $RetainedRuntimeRoot = [string]$retained.RuntimeRoot

    $sha256SumsPath = Join-Path ([string]$retained.CandidateRoot) 'SHA256SUMS'
    Assert-RuntimeFloor (Test-Path -LiteralPath $sha256SumsPath -PathType Leaf) `
        "Retained Runtime candidate is missing its exact SHA256SUMS authority: $sha256SumsPath"
    foreach ($file in @(
        [pscustomobject]@{ Relative = "$runtimeWorkshopId/Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.Abstractions.dll"; Path = [string]$retained.AbstractionsPath },
        [pscustomobject]@{ Relative = "$runtimeWorkshopId/Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.Core.dll"; Path = [string]$retained.CorePath }
    )) {
        $pattern = '^[0-9a-f]{64}  ' + [Text.RegularExpressions.Regex]::Escape([string]$file.Relative) + '$'
        $authorityRows = @(Get-Content -LiteralPath $sha256SumsPath -Encoding UTF8 | Where-Object { $_ -cmatch $pattern })
        Assert-RuntimeFloor ($authorityRows.Count -eq 1) "Retained SHA256SUMS does not contain one exact row for $($file.Relative)."
        $expectedSha256 = $authorityRows[0].Substring(0, 64).ToUpperInvariant()
        $actualSha256 = (Get-FileHash -LiteralPath ([string]$file.Path) -Algorithm SHA256).Hash.ToUpperInvariant()
        Assert-RuntimeFloor ($actualSha256 -ceq $expectedSha256) "Retained Runtime file hash drifted: $($file.Relative)."
    }
}

$retainedRuntime = [System.IO.Path]::GetFullPath($RetainedRuntimeRoot)
$retainedCore = Join-Path $retainedRuntime 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'
$retainedAbstractions = Join-Path $retainedRuntime 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Abstractions.dll'
Assert-RuntimeFloor (Test-Path -LiteralPath $retainedCore -PathType Leaf) "Retained Runtime 0.5.5 Core is unavailable: $retainedCore"
Assert-RuntimeFloor (Test-Path -LiteralPath $retainedAbstractions -PathType Leaf) "Retained Runtime 0.5.5 Abstractions is unavailable: $retainedAbstractions"
$retainedCoreVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($retainedCore)
Assert-RuntimeFloor ([string]$retainedCoreVersion.FileVersion -ceq '0.5.5.0') 'Retained Core FileVersion is not exactly 0.5.5.0.'
Assert-RuntimeFloor ([string]$retainedCoreVersion.ProductVersion -ceq '0.5.5') 'Retained Core ProductVersion is not exactly 0.5.5.'

$currentRegistry = Get-Content -LiteralPath (Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$historyRegistry = Get-Content -LiteralPath (Join-Path $repo 'author-sdk\advanced-reference-policies\history\runtime-registry.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Assert-RuntimeFloor ([int]$currentRegistry.schemaVersion -eq 2 -and [int]$historyRegistry.schemaVersion -eq 2) `
    'Current and historical Advanced registries must both use schema 2.'
foreach ($product in @(
    [pscustomobject]@{ Root = 'AutoFishing'; UniqueId = 'Yuuka.DTMAPI.AutoFishing'; CurrentPolicy = 'doloctown-24456188-autofishing-v1'; HistoricalPolicy = 'doloctown-23762374-autofishing-v1' },
    [pscustomobject]@{ Root = 'DebugConsole'; UniqueId = 'DTMAPI.DebugConsoleMod'; CurrentPolicy = 'doloctown-24456188-debugconsole-v1'; HistoricalPolicy = 'doloctown-23762374-debugconsole-v1' },
    [pscustomobject]@{ Root = 'MoreEquipmentSlots'; UniqueId = 'DTMAPI.MoreEquipmentSlotsMod'; CurrentPolicy = 'doloctown-24456188-moreequipmentslots-v1'; HistoricalPolicy = 'doloctown-23762374-moreequipmentslots-v1' },
    [pscustomobject]@{ Root = 'MoreSaves'; UniqueId = 'DTMAPI.MoreSavesMod'; CurrentPolicy = 'doloctown-24456188-moresaves-v1'; HistoricalPolicy = 'doloctown-23762374-moresaves-v1' }
)) {
    $productRoot = Join-Path $repo ("products\first-party\{0}" -f $product.Root)
    $manifest = Get-Content -LiteralPath (Join-Path $productRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $author = Get-Content -LiteralPath (Join-Path $productRoot 'dtmapi.author.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $currentRows = @($currentRegistry.policies | Where-Object { [string]$_.requiredUniqueId -ceq [string]$product.UniqueId })
    $historyRows = @($historyRegistry.policies | Where-Object { [string]$_.requiredUniqueId -ceq [string]$product.UniqueId })
    Assert-RuntimeFloor ([string]$manifest.MinimumDTMApiVersion -ceq '0.6.0') "$($product.Root) must truthfully require Runtime 0.6.0."
    Assert-RuntimeFloor ([string]$author.targetDtmApiVersion -ceq '0.5.5') "$($product.Root) must retain the frozen 0.5.5 public-API compile target."
    Assert-RuntimeFloor ($currentRows.Count -eq 1 -and
        [string]$currentRows[0].policyId -ceq [string]$product.CurrentPolicy -and
        [string]$currentRows[0].minimumDtmApiVersion -ceq '0.6.0') `
        "$($product.Root) current authoring policy/floor is not exact."
    Assert-RuntimeFloor ($historyRows.Count -eq 1 -and
        [string]$historyRows[0].policyId -ceq [string]$product.HistoricalPolicy -and
        [string]$historyRows[0].minimumDtmApiVersion -ceq '0.5.5') `
        "$($product.Root) historical Runtime/Doctor acceptance row is not exact."
}

$pwsh = Get-Command pwsh.exe -ErrorAction SilentlyContinue
if ($null -eq $pwsh) {
    $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
}
Assert-RuntimeFloor ($null -ne $pwsh) 'PowerShell 7 is required for the isolated retained-Core reflection check.'
$previousReflectRoot = $env:DTMAPI_RETAINED_CORE_REFLECT_ROOT
try {
    $env:DTMAPI_RETAINED_CORE_REFLECT_ROOT = $retainedRuntime
    $reflectionProbe = @'
$ErrorActionPreference = 'Stop'
$root = $env:DTMAPI_RETAINED_CORE_REFLECT_ROOT
$payload = Join-Path $root 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
[void][Reflection.Assembly]::LoadFrom((Join-Path $payload 'DTMAPI.Abstractions.dll'))
$core = [Reflection.Assembly]::LoadFrom((Join-Path $payload 'DTMAPI.Core.dll'))
$runtimeType = $core.GetType('DTMAPI.Core.Runtime.DtmApiRuntime', $true)
$method = $runtimeType.GetMethod('IsVersionRequirementSatisfied', [Reflection.BindingFlags]'NonPublic,Static')
$arguments = [object[]]@('0.6.0', '0.5.5', '')
$satisfied = $method.Invoke($null, $arguments)
$resourceName = 'DTMAPI.Core.AdvancedReferencePolicyRegistry.json'
$stream = $core.GetManifestResourceStream($resourceName)
if ($null -eq $stream) { throw "Retained Core is missing $resourceName." }
$reader = New-Object IO.StreamReader($stream, (New-Object Text.UTF8Encoding($false, $true)), $true)
try { $registry = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose(); $stream.Dispose() }
[pscustomobject]@{
    ApiVersion = [string]$runtimeType.GetField('ApiVersion', [Reflection.BindingFlags]'Public,Static').GetRawConstantValue()
    Satisfied = [bool]$satisfied
    Reason = [string]$arguments[2]
    PolicyIds = @($registry.policies | ForEach-Object { [string]$_.policyId })
} | ConvertTo-Json -Compress
'@
    $probeOutput = @(& $pwsh.Source -NoProfile -Command $reflectionProbe 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Retained Core reflection probe failed: $($probeOutput -join [Environment]::NewLine)"
    }
    $probe = ($probeOutput -join [Environment]::NewLine) | ConvertFrom-Json
}
finally {
    $env:DTMAPI_RETAINED_CORE_REFLECT_ROOT = $previousReflectRoot
}

$oldPolicyIds = @($probe.PolicyIds | ForEach-Object { [string]$_ })
Assert-RuntimeFloor ([string]$probe.ApiVersion -ceq '0.5.5') 'The retained Core did not project ApiVersion 0.5.5.'
Assert-RuntimeFloor (-not [bool]$probe.Satisfied -and [string]$probe.Reason -ceq 'Actual version is older than the required minimum.') `
    'The retained 0.5.5 Core did not reject a 0.6.0 minimum with its own version comparator.'
Assert-RuntimeFloor ('doloctown-23762374-autofishing-v1' -in $oldPolicyIds -and
    'doloctown-23762374-debugconsole-v1' -in $oldPolicyIds -and
    'doloctown-23762374-moreequipmentslots-v1' -in $oldPolicyIds -and
    'doloctown-23762374-moresaves-v1' -in $oldPolicyIds -and
    'doloctown-24456188-autofishing-v1' -notin $oldPolicyIds -and
    'doloctown-24456188-debugconsole-v1' -notin $oldPolicyIds -and
    'doloctown-24456188-moreequipmentslots-v1' -notin $oldPolicyIds -and
    'doloctown-24456188-moresaves-v1' -notin $oldPolicyIds) `
    'The retained 0.5.5 Core policy registry does not preserve the expected old/current policy separation.'

Write-Host 'DTMAPI 0.6 Runtime-floor compatibility: PASS'
Write-Host '  Author SDK API target=0.5.5; current AutoFishing/DebugConsole/MoreEquipmentSlots/MoreSaves floor=0.6.0'
Write-Host '  retained Core=0.5.5; own comparator rejects 0.6.0 before code loading'
Write-Host '  old policies=retained Core current set; 244 policies=current 0.6 authoring set only'
