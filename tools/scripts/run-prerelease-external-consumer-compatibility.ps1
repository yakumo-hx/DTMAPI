param(
    [string] $CandidateRoot = '',
    [ValidateRange(60, 900)]
    [int] $TimeoutSeconds = 240,
    [switch] $ValidateOnly,
    [switch] $RunSmokeChild
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ([string]::IsNullOrWhiteSpace($CandidateRoot)) {
    $CandidateRoot = Join-Path $repo 'dist\prerelease-step5-candidate\DTMAPI'
}
$CandidateRoot = [System.IO.Path]::GetFullPath($CandidateRoot)
$packagePayloadRoot = Join-Path $CandidateRoot 'Content\DTMAPIInstaller\Payload'

if ($RunSmokeChild) {
    $smokeParameters = @{
        UseSteam = $true
        SaveSlot = 3
        SaveTestMode = 'NoNativeSave'
        StageQaHost = $true
        QaObserveSaveLoaded = $true
        AutoExerciseTitleButtonLifecycle = $true
        OfficialModProfile = 'CoreOnly'
        OfficialModProfileExtraEnabledIds = @(
            'Workshop.3743621104',
            'Workshop.3743644065',
            'Workshop.3754869009'
        )
        IsolateAllOfficialMods = $true
        PackagePayloadRoot = $packagePayloadRoot
        TimeoutSeconds = $TimeoutSeconds
    }
    & (Join-Path $PSScriptRoot 'run-game-smoke.ps1') @smokeParameters
    exit $LASTEXITCODE
}

function Get-PrereleaseExternalTreeSnapshot {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return [pscustomobject]@{
            Root = $Root
            Exists = $false
            FileCount = 0
            Bytes = [int64]0
            TreeSha256 = ''
        }
    }
    $rows = @(Get-ChildItem -LiteralPath $Root -Recurse -File -ErrorAction Stop |
        ForEach-Object {
            [pscustomobject]@{
                File = $_
                RelativePath = $_.FullName.Substring($Root.Length).
                    TrimStart([char]92, [char]47).
                    Replace([char]92, [char]47)
            }
        } |
        Sort-Object RelativePath)
    $lines = New-Object 'System.Collections.Generic.List[string]'
    [int64] $bytes = 0
    foreach ($row in $rows) {
        $hash = (Get-FileHash -LiteralPath $row.File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $lines.Add($hash + '  ' + [string]$row.RelativePath) | Out-Null
        $bytes += [int64]$row.File.Length
    }
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $tree = [BitConverter]::ToString(
            $hasher.ComputeHash(
                [System.Text.Encoding]::UTF8.GetBytes($lines.ToArray() -join "`n"))).
            Replace('-', '').
            ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
    return [pscustomobject]@{
        Root = $Root
        Exists = $true
        FileCount = $rows.Count
        Bytes = $bytes
        TreeSha256 = $tree
    }
}

function Test-PrereleaseExternalTreeEqual {
    param(
        [Parameter(Mandatory = $true)] $Before,
        [Parameter(Mandatory = $true)] $After
    )

    return [bool]$Before.Exists -and [bool]$After.Exists -and
        [int]$Before.FileCount -eq [int]$After.FileCount -and
        [int64]$Before.Bytes -eq [int64]$After.Bytes -and
        [string]::Equals(
            [string]$Before.TreeSha256,
            [string]$After.TreeSha256,
            [System.StringComparison]::Ordinal)
}

function Write-PrereleaseExternalJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $Value | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $Path -Encoding UTF8
}

$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$baselinePath = Join-Path $repo 'tools\release\baselines\retained-runtime-052-public-api-audit-20260728.json'
$releaseManifestPath = Join-Path $CandidateRoot 'Content\DTMAPI\release-manifest.json'
foreach ($path in @($catalogPath, $baselinePath, $releaseManifestPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required pre-release compatibility authority is missing: $path"
    }
}
if (-not (Test-Path -LiteralPath $packagePayloadRoot -PathType Container)) {
    throw "Frozen Runtime candidate package payload is missing: $packagePayloadRoot"
}

$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$workshopContentRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $gameDir '..\..\workshop\content\2285550'))
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
$baseline = Get-Content -Raw -Encoding UTF8 -LiteralPath $baselinePath | ConvertFrom-Json
$releaseManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseManifestPath | ConvertFrom-Json
if ([string]$releaseManifest.DTMAPIVersion -cne '0.5.5') {
    throw "The selected Runtime candidate is not version 0.5.5: $releaseManifestPath"
}

$definitions = @(
    [pscustomobject]@{
        WorkshopId = '3743621104'
        UniqueId = 'com.user.dolocstorageexpansion'
        RelativeDll = 'DolocStorageExpansionMod.dll'
        SuccessPattern = '\u591a\u6d1b\u53ef\u5c0f\u9547\u7eb8\u7bb1\u5bb9\u91cf\u7ffb\u500d 1\.0\.10 \u52a0\u8f7d\u6210\u529f\uff01'
        ProviderProof = 'DtmMod.Monitor'
        ProviderFailureText = ''
    },
    [pscustomobject]@{
        WorkshopId = '3743644065'
        UniqueId = 'com.user.dolocstorecapacity'
        RelativeDll = 'DolocStoreCapacityMod.dll'
        SuccessPattern = '\u5546\u5e97\u8d2d\u7269\u6570\u91cf\u500d\u589e Mod \u52a0\u8f7d\u6210\u529f\uff01\u5f53\u524d\u500d\u7387:'
        ProviderProof = 'IDtmHelper.ReadConfig + DtmMod.Monitor'
        ProviderFailureText = ''
    },
    [pscustomobject]@{
        WorkshopId = '3754869009'
        UniqueId = 'Codex.DolocTownQoL'
        RelativeDll = 'Content\DTMAPI\DolocTownQoL.dll'
        SuccessPattern = 'Doloc Town QoL 0\.2\.3 loaded:'
        ProviderProof = 'IModRegistry.GetApi<IDtmConfigMenuApi> + registration'
        ProviderFailureText = 'DTMAPI config menu API was not available'
    }
)

$preflightRows = New-Object 'System.Collections.Generic.List[object]'
foreach ($definition in $definitions) {
    $catalogRows = @($catalog.externalCompatibilityBaseline | Where-Object {
        [string]$_.workshopId -ceq [string]$definition.WorkshopId -and
        [string]$_.uniqueId -ceq [string]$definition.UniqueId
    })
    $baselineRows = @($baseline.retainedExternalConsumers | Where-Object {
        [string]$_.workshopId -ceq [string]$definition.WorkshopId -and
        [string]$_.fileName -ceq [System.IO.Path]::GetFileName([string]$definition.RelativeDll)
    })
    if ($catalogRows.Count -ne 1 -or $baselineRows.Count -ne 1) {
        throw "External compatibility authority is not unique for Workshop $($definition.WorkshopId)."
    }
    if ([string]$catalogRows[0].releaseGate -notin @('ActualLoadLanePending', 'ActualLoadLaneVerified')) {
        throw "External compatibility release gate has an unexpected state for Workshop $($definition.WorkshopId)."
    }

    $root = [System.IO.Path]::GetFullPath(
        (Join-Path $workshopContentRoot ([string]$definition.WorkshopId)))
    $manifestPath = Join-Path $root 'Content\DTMAPI\manifest.json'
    $dllPath = Join-Path $root ([string]$definition.RelativeDll)
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
        throw "Subscribed external compatibility input is missing for Workshop $($definition.WorkshopId)."
    }
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    $dllSha256 = (Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ([string]$manifest.UniqueID -cne [string]$definition.UniqueId -or
        -not [string]::Equals(
            $dllSha256,
            [string]$baselineRows[0].sha256,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Subscribed external compatibility identity/hash drifted for Workshop $($definition.WorkshopId)."
    }
    $preflightRows.Add([pscustomobject]@{
        WorkshopId = [string]$definition.WorkshopId
        UniqueId = [string]$definition.UniqueId
        Root = $root
        ManifestPath = $manifestPath
        DllPath = $dllPath
        DllLength = [int64](Get-Item -LiteralPath $dllPath).Length
        ExpectedDllSha256 = ([string]$baselineRows[0].sha256).ToUpperInvariant()
        ActualDllSha256 = $dllSha256
        ReferencedAbstractionsVersion = [string]$baselineRows[0].referencedAbstractionsVersion
        MemberReferenceCount = [int]$baselineRows[0].resolvedAbstractionsMemberReferenceCount
        Tree = Get-PrereleaseExternalTreeSnapshot -Root $root
        Passed = $true
    }) | Out-Null
}

$preflight = [ordered]@{
    SchemaVersion = 1
    Status = 'Passed'
    CandidateRoot = $CandidateRoot
    CandidateVersion = [string]$releaseManifest.DTMAPIVersion
    CandidateBuildCommit = [string]$releaseManifest.BuildCommit
    WorkshopContentRoot = $workshopContentRoot
    Consumers = @($preflightRows.ToArray())
    Passed = $true
}
if ($ValidateOnly) {
    $preflight | ConvertTo-Json -Depth 16
    Write-Host 'Prerelease external-consumer compatibility preflight passed; game was not launched.'
    return
}

$smokeOutput = @(
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
        -RunSmokeChild `
        -CandidateRoot $CandidateRoot `
        -TimeoutSeconds $TimeoutSeconds 2>&1
)
$smokeExitCode = $LASTEXITCODE
$smokeOutput | ForEach-Object { Write-Host ([string]$_) }
$evidenceMarkers = @($smokeOutput | ForEach-Object {
    $text = [string]$_
    if ($text.StartsWith('DTMAPI_SMOKE_EVIDENCE_PATH=', [System.StringComparison]::Ordinal)) {
        $text.Substring('DTMAPI_SMOKE_EVIDENCE_PATH='.Length).Trim()
    }
} | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
if ($evidenceMarkers.Count -ne 1 -or
    -not (Test-Path -LiteralPath $evidenceMarkers[0] -PathType Container)) {
    throw "External-consumer smoke did not emit exactly one valid evidence path. exit=$smokeExitCode"
}
$evidencePath = [System.IO.Path]::GetFullPath($evidenceMarkers[0])
$resultPath = Join-Path $evidencePath 'result.json'
$logPath = Join-Path $evidencePath 'DTMAPI-latest.log'
if ($smokeExitCode -ne 0 -or
    -not (Test-Path -LiteralPath $resultPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    throw "External-consumer smoke did not complete its base gates. exit=$smokeExitCode evidence=$evidencePath"
}

$smoke = Get-Content -Raw -Encoding UTF8 -LiteralPath $resultPath | ConvertFrom-Json
$baseRunnerPassed =
    [string]$smoke.RunStatus -ceq 'Passed' -and
    [string]$smoke.SaveLoaded -ceq 'Passed' -and
    [string]$smoke.TitleButtonLifecycle -ceq 'Passed' -and
    [string]$smoke.PlayerSaveUnchangedBeforeCleanup -ceq 'Passed' -and
    [string]$smoke.CommittedSidecarsUnchangedBeforeCleanup -ceq 'Passed' -and
    [string]$smoke.OfficialModProfileRestored -ceq 'True' -and
    [string]$smoke.QaHostCleanup -ceq 'Passed' -and
    [string]$smoke.NoFatalInstanceWindow -ceq 'Passed' -and
    [string]$smoke.ProcessExited -ceq 'Passed' -and
    [string]$smoke.ForcedClose -ceq 'Passed'
[object[]]$compatibilityFailures = @(Select-String -LiteralPath $logPath -Pattern (
    'MissingMethodException|TypeLoadException|FileLoadException|' +
    'Failed to load (com\.user\.dolocstorageexpansion|com\.user\.dolocstorecapacity|Codex\.DolocTownQoL)|' +
    'Skipping (com\.user\.dolocstorageexpansion|com\.user\.dolocstorecapacity|Codex\.DolocTownQoL)|' +
    'Manifest discovery failed:[^\r\n]*(3743621104|3743644065|3754869009)|' +
    'Fatal error') -ErrorAction SilentlyContinue)

$resultRows = New-Object 'System.Collections.Generic.List[object]'
foreach ($definition in $definitions) {
    $pre = @($preflightRows | Where-Object { [string]$_.WorkshopId -ceq [string]$definition.WorkshopId })[0]
    $rootPattern = [regex]::Escape([string]$pre.Root)
    $dllPattern = [regex]::Escape([string]$pre.DllPath)
    $ownerPattern = [regex]::Escape([string]$definition.UniqueId)
    [object[]]$loadSourceMatches = @(Select-String -LiteralPath $logPath -Pattern (
        'Code mod load-source owner=' + $ownerPattern + ';') -ErrorAction SilentlyContinue)
    $loadSourcePattern =
        'Code mod load-source owner=' + $ownerPattern +
        ';[^\r\n]*; source=Workshop; workshopId=' + [string]$definition.WorkshopId +
        '; root=' + $rootPattern +
        '; dll=' + $dllPattern + '\.\s*$'
    $loadSourcePassed =
        $loadSourceMatches.Count -eq 1 -and
        $loadSourceMatches[0].Line -match $loadSourcePattern
    [object[]]$entryMatches = @(Select-String -LiteralPath $logPath -Pattern (
        '\[' + $ownerPattern + '\] Mod Entry completed\.') -ErrorAction SilentlyContinue)
    [object[]]$successMatches = @(Select-String -LiteralPath $logPath -Pattern (
        [string]$definition.SuccessPattern) -ErrorAction SilentlyContinue)
    [object[]]$providerFailureMatches = @(
        if (-not [string]::IsNullOrWhiteSpace([string]$definition.ProviderFailureText)) {
            Select-String -LiteralPath $logPath -SimpleMatch (
                [string]$definition.ProviderFailureText) -ErrorAction SilentlyContinue
        }
    )
    $postTree = Get-PrereleaseExternalTreeSnapshot -Root ([string]$pre.Root)
    $treeUnchanged = Test-PrereleaseExternalTreeEqual -Before $pre.Tree -After $postTree
    $rowPassed =
        $loadSourcePassed -and
        $entryMatches.Count -eq 1 -and
        $successMatches.Count -eq 1 -and
        $providerFailureMatches.Count -eq 0 -and
        $treeUnchanged
    $resultRows.Add([pscustomobject]@{
        WorkshopId = [string]$definition.WorkshopId
        UniqueId = [string]$definition.UniqueId
        DllPath = [string]$pre.DllPath
        DllSha256 = [string]$pre.ActualDllSha256
        WorkshopLoadSourcePassed = $loadSourcePassed
        LoadSourceLines = @($loadSourceMatches | ForEach-Object { [string]$_.Line })
        EntryCompletedCount = $entryMatches.Count
        BehaviorEntrySuccessCount = $successMatches.Count
        ProviderProof = [string]$definition.ProviderProof
        ProviderFailureCount = $providerFailureMatches.Count
        PostflightTree = $postTree
        WorkshopArtifactUnchanged = $treeUnchanged
        Passed = $rowPassed
    }) | Out-Null
}

$passed =
    $baseRunnerPassed -and
    $compatibilityFailures.Count -eq 0 -and
    @($resultRows | Where-Object { -not [bool]$_.Passed }).Count -eq 0
$gate = [ordered]@{
    SchemaVersion = 1
    Status = if ($passed) { 'Passed' } else { 'Failed' }
    Claim = 'bounded-three-external-consumer-actual-load'
    EvidencePath = $evidencePath
    CandidateRoot = $CandidateRoot
    CandidateVersion = [string]$releaseManifest.DTMAPIVersion
    CandidateBuildCommit = [string]$releaseManifest.BuildCommit
    SaveSlot = 3
    SaveTestMode = 'NoNativeSave'
    NativeSaveInvoked = $false
    Preflight = $preflight
    Consumers = @($resultRows.ToArray())
    BaseRunnerPassed = $baseRunnerPassed
    TitleExitCleanupPassed = $baseRunnerPassed
    CompatibilityFailureCount = $compatibilityFailures.Count
    CompatibilityFailureLines = @($compatibilityFailures | ForEach-Object { [string]$_.Line })
    Passed = $passed
}
Write-PrereleaseExternalJson `
    -Path (Join-Path $evidencePath 'prerelease-external-consumer-compatibility.json') `
    -Value $gate
Write-Output ('DTMAPI_EXTERNAL_COMPATIBILITY_EVIDENCE_PATH=' + $evidencePath)
if (-not $passed) {
    throw "Prerelease external-consumer compatibility gate failed. Evidence: $evidencePath"
}
Write-Host "Prerelease external-consumer compatibility gate passed. Evidence: $evidencePath"
