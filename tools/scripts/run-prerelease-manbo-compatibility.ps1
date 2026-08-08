param(
    [string] $CandidateRoot = '',
    [string] $ExpectedCandidateVersion = '0.5.5',
    [string] $WorkshopRoot = 'D:\Steam\steamapps\workshop\content\2285550\3746319981',
    [ValidateRange(60, 900)]
    [int] $TimeoutSeconds = 240,
    [switch] $ValidateOnly
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ([string]::IsNullOrWhiteSpace($CandidateRoot)) {
    $CandidateRoot = Join-Path $repo 'dist\prerelease-step5-candidate\DTMAPI'
}
$CandidateRoot = [System.IO.Path]::GetFullPath($CandidateRoot)
$WorkshopRoot = [System.IO.Path]::GetFullPath($WorkshopRoot)
if ([string]::IsNullOrWhiteSpace($ExpectedCandidateVersion)) {
    throw '-ExpectedCandidateVersion must be a non-empty exact Runtime release version.'
}
$ExpectedCandidateVersion = $ExpectedCandidateVersion.Trim()
$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$releaseManifestPath = Join-Path $CandidateRoot 'Content\DTMAPI\release-manifest.json'
$packagePayloadRoot = Join-Path $CandidateRoot 'Content\DTMAPIInstaller\Payload'

function Get-PrereleaseManboTreeSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] $RetainedArtifact
    )

    $snapshot = [ordered]@{
        Root = $Root
        Exists = Test-Path -LiteralPath $Root -PathType Container
        ExpectedFileCount = [int]$RetainedArtifact.fileCount
        ExpectedBytes = [int64]$RetainedArtifact.bytes
        ExpectedTreeSha256 = ([string]$RetainedArtifact.treeSha256).ToLowerInvariant()
        ActualFileCount = 0
        ActualBytes = [int64]0
        ActualTreeSha256 = ''
        Passed = $false
    }
    if (-not $snapshot.Exists) {
        return $snapshot
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
    foreach ($row in $rows) {
        $fileHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $row.File.FullName).Hash.ToLowerInvariant()
        $lines.Add($fileHash + '  ' + [string]$row.RelativePath) | Out-Null
        $snapshot.ActualBytes += [int64]$row.File.Length
    }
    $snapshot.ActualFileCount = $rows.Count
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $normalizedTree = $lines.ToArray() -join "`n"
        $snapshot.ActualTreeSha256 = [BitConverter]::ToString(
            $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($normalizedTree))).
            Replace('-', '').
            ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
    $snapshot.Passed =
        $snapshot.ActualFileCount -eq $snapshot.ExpectedFileCount -and
        $snapshot.ActualBytes -eq $snapshot.ExpectedBytes -and
        [string]::Equals(
            $snapshot.ActualTreeSha256,
            $snapshot.ExpectedTreeSha256,
            [System.StringComparison]::Ordinal)
    return $snapshot
}

function Write-PrereleaseManboJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $Value |
        ConvertTo-Json -Depth 16 |
        Set-Content -LiteralPath $Path -Encoding UTF8
}

if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
    throw "Product Catalog is missing: $catalogPath"
}
if (-not (Test-Path -LiteralPath $releaseManifestPath -PathType Leaf)) {
    throw "Frozen Runtime candidate manifest is missing: $releaseManifestPath"
}
if (-not (Test-Path -LiteralPath $packagePayloadRoot -PathType Container)) {
    throw "Frozen Runtime candidate package payload is missing: $packagePayloadRoot"
}

$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
$products = @($catalog.products | Where-Object {
    [string]$_.catalogId -ceq 'manbo-cardboard-audio' -and
    [string]$_.uniqueId -ceq 'Yuuka.DTMAPI.ManboCardboardAudio' -and
    [string]$_.workshopId -ceq '3746319981'
})
if ($products.Count -ne 1) {
    throw "Expected exactly one frozen Manbo Catalog row; found $($products.Count)."
}
$product = $products[0]
$artifact = $product.retainedArtifact
if ([string]$product.publishedVersion -cne '0.1.0-dtmapi' -or
    [string]$product.publishedMinimumDtmApiVersion -cne '0.5.2-alpha' -or
    [string]$artifact.entryDll -cne 'Yuuka.DTMAPI.ManboCardboardAudio.dll' -or
    [string]$artifact.entryDllSha256 -notmatch '^[0-9A-Fa-f]{64}$' -or
    [string]$artifact.treeSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
    throw 'The frozen Manbo Catalog compatibility identity is incomplete or drifted.'
}

$releaseManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseManifestPath | ConvertFrom-Json
if ([string]$releaseManifest.DTMAPIVersion -cne $ExpectedCandidateVersion) {
    throw "The selected Runtime candidate is not the expected version ${ExpectedCandidateVersion}: $releaseManifestPath"
}

$preflight = Get-PrereleaseManboTreeSnapshot -Root $WorkshopRoot -RetainedArtifact $artifact
$entryDllPath = Join-Path $WorkshopRoot ('Content\DTMAPI\' + [string]$artifact.entryDll)
$entryDllExists = Test-Path -LiteralPath $entryDllPath -PathType Leaf
$entryDllLength = if ($entryDllExists) { [int64](Get-Item -LiteralPath $entryDllPath).Length } else { [int64]0 }
$entryDllSha256 = if ($entryDllExists) {
    (Get-FileHash -Algorithm SHA256 -LiteralPath $entryDllPath).Hash.ToLowerInvariant()
}
else {
    ''
}
$entryDllPassed =
    $entryDllExists -and
    $entryDllLength -gt 0 -and
    [string]::Equals(
        $entryDllSha256,
        ([string]$artifact.entryDllSha256).ToLowerInvariant(),
        [System.StringComparison]::Ordinal)
$preflightPassed = [bool]$preflight.Passed -and $entryDllPassed
$preflightResult = [ordered]@{
    SchemaVersion = 1
    Status = if ($preflightPassed) { 'Passed' } else { 'Failed' }
    CandidateRoot = $CandidateRoot
    CandidateVersion = [string]$releaseManifest.DTMAPIVersion
    ExpectedCandidateVersion = $ExpectedCandidateVersion
    CandidateBuildCommit = [string]$releaseManifest.BuildCommit
    CatalogPath = [System.IO.Path]::GetFullPath($catalogPath)
    CatalogId = [string]$product.catalogId
    UniqueId = [string]$product.uniqueId
    WorkshopId = [string]$product.workshopId
    PublishedVersion = [string]$product.publishedVersion
    PublishedMinimumDtmApiVersion = [string]$product.publishedMinimumDtmApiVersion
    WorkshopTree = $preflight
    EntryDllPath = [System.IO.Path]::GetFullPath($entryDllPath)
    EntryDllExists = $entryDllExists
    EntryDllLength = $entryDllLength
    ExpectedEntryDllSha256 = ([string]$artifact.entryDllSha256).ToLowerInvariant()
    ActualEntryDllSha256 = $entryDllSha256
    EntryDllPassed = $entryDllPassed
    Passed = $preflightPassed
}
if (-not $preflightPassed) {
    throw 'The frozen Manbo Workshop artifact preflight failed; Steam subscription contents must not be rewritten by this gate.'
}
if ($ValidateOnly) {
    $preflightResult | ConvertTo-Json -Depth 16
    Write-Host 'Prerelease Manbo compatibility preflight passed; game was not launched.'
    return
}

$smokeArguments = @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
    '-UseSteam',
    '-SaveSlot', '3',
    '-SaveTestMode', 'NoNativeSave',
    '-StageQaHost',
    '-QaObserveSaveLoaded',
    '-AutoExerciseTitleButtonLifecycle',
    '-OfficialModProfile', 'CoreOnly',
    '-OfficialModProfileExtraEnabledIds', 'Workshop.3746319981',
    '-IsolateAllOfficialMods',
    '-PackagePayloadRoot', $packagePayloadRoot,
    '-TimeoutSeconds', [string]$TimeoutSeconds
)
$smokeOutput = @(& powershell.exe @smokeArguments 2>&1)
$smokeExitCode = $LASTEXITCODE
$smokeOutput | ForEach-Object { Write-Host ([string]$_) }
$evidenceMarkers = @($smokeOutput | ForEach-Object {
    $text = [string]$_
    if ($text.StartsWith('DTMAPI_SMOKE_EVIDENCE_PATH=', [System.StringComparison]::Ordinal)) {
        $text.Substring('DTMAPI_SMOKE_EVIDENCE_PATH='.Length).Trim()
    }
} | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
if ($evidenceMarkers.Count -ne 1 -or -not (Test-Path -LiteralPath $evidenceMarkers[0] -PathType Container)) {
    throw "Manbo compatibility smoke did not emit exactly one valid evidence path. exit=$smokeExitCode"
}
$evidencePath = [System.IO.Path]::GetFullPath($evidenceMarkers[0])
$resultPath = Join-Path $evidencePath 'result.json'
$logPath = Join-Path $evidencePath 'DTMAPI-latest.log'
if ($smokeExitCode -ne 0 -or
    -not (Test-Path -LiteralPath $resultPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    throw "Manbo compatibility smoke did not complete its base runner gates. exit=$smokeExitCode evidence=$evidencePath"
}

$smoke = Get-Content -Raw -Encoding UTF8 -LiteralPath $resultPath | ConvertFrom-Json
$escapedWorkshopRoot = [regex]::Escape($WorkshopRoot)
$escapedEntryDllPath = [regex]::Escape([System.IO.Path]::GetFullPath($entryDllPath))
$loadSourceMatches = @(Select-String -LiteralPath $logPath -Pattern 'Code mod load-source owner=Yuuka\.DTMAPI\.ManboCardboardAudio;' -ErrorAction SilentlyContinue)
$loadSourcePattern =
    'Code mod load-source owner=Yuuka\.DTMAPI\.ManboCardboardAudio;[^\r\n]*; source=Workshop; workshopId=3746319981; root=' +
    $escapedWorkshopRoot +
    '; dll=' +
    $escapedEntryDllPath +
    '\.\s*$'
$loadSourcePassed =
    $loadSourceMatches.Count -eq 1 -and
    $loadSourceMatches[0].Line -match $loadSourcePattern
$duplicateMatches = @(Select-String -LiteralPath $logPath -Pattern 'Duplicate UniqueID Yuuka\.DTMAPI\.ManboCardboardAudio discovered; using ' -ErrorAction SilentlyContinue)
$duplicatePattern =
    'Duplicate UniqueID Yuuka\.DTMAPI\.ManboCardboardAudio discovered; using Workshop enabled=true root=' +
    $escapedWorkshopRoot +
    '(?:[.;|]|$)'
$duplicateSelectionPassed =
    $duplicateMatches.Count -gt 0 -and
    @($duplicateMatches | Where-Object { $_.Line -notmatch $duplicatePattern }).Count -eq 0
$entryMatches = @(Select-String -LiteralPath $logPath -Pattern '\[Yuuka\.DTMAPI\.ManboCardboardAudio\] Mod Entry completed\.' -ErrorAction SilentlyContinue)
$registerMatches = @(Select-String -LiteralPath $logPath -Pattern '\[Yuuka\.DTMAPI\.ManboCardboardAudio\] ManboCardboardAudio register success=True event=PLAY_RESOURCE_PAPER_BOX ' -ErrorAction SilentlyContinue)
$wavReadyMatches = @(Select-String -LiteralPath $logPath -Pattern 'AudioReplacement local WAV ready owner=Yuuka\.DTMAPI\.ManboCardboardAudio replacement=manbo-paper-box event=PLAY_RESOURCE_PAPER_BOX ' -ErrorAction SilentlyContinue)
$nativeHookMatches = @(Select-String -LiteralPath $logPath -Pattern 'Hook status: Audio\.SoundEventReplacement = experimental\.[^\r\n]*Paper-box native owner diagnostic patched=True' -ErrorAction SilentlyContinue)
$finalHookInstalledMatches = @(Select-String -LiteralPath $logPath -Pattern 'GameBridge final health snapshot reason=ReturnedToTitle[^\r\n]*audioReplacement=\{[^\r\n]*hookInstalled=True' -ErrorAction SilentlyContinue)
$compatibilityFailures = @(Select-String -LiteralPath $logPath -Pattern 'MissingMethodException|TypeLoadException|FileLoadException|ManboCardboardAudio API missing|Failed to load Yuuka\.DTMAPI\.ManboCardboardAudio|Skipping Yuuka\.DTMAPI\.ManboCardboardAudio|provider.*(failed|missing)|manifest.*Yuuka\.DTMAPI\.ManboCardboardAudio.*(failed|error)|Loader.*Yuuka\.DTMAPI\.ManboCardboardAudio.*(failed|error)|Fatal error' -ErrorAction SilentlyContinue)
$postflight = Get-PrereleaseManboTreeSnapshot -Root $WorkshopRoot -RetainedArtifact $artifact
$artifactUnchanged =
    [bool]$postflight.Passed -and
    [int]$postflight.ActualFileCount -eq [int]$preflight.ActualFileCount -and
    [int64]$postflight.ActualBytes -eq [int64]$preflight.ActualBytes -and
    [string]::Equals(
        [string]$postflight.ActualTreeSha256,
        [string]$preflight.ActualTreeSha256,
        [System.StringComparison]::Ordinal)
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
$passed =
    $baseRunnerPassed -and
    $loadSourcePassed -and
    $duplicateSelectionPassed -and
    $entryMatches.Count -eq 1 -and
    $registerMatches.Count -eq 1 -and
    $wavReadyMatches.Count -ge 1 -and
    $nativeHookMatches.Count -ge 1 -and
    $finalHookInstalledMatches.Count -ge 1 -and
    $compatibilityFailures.Count -eq 0 -and
    $artifactUnchanged

$gate = [ordered]@{
    SchemaVersion = 1
    Status = if ($passed) { 'Passed' } else { 'Failed' }
    Claim = 'bounded-old-subscription-compatibility'
    EvidencePath = $evidencePath
    CandidateRoot = $CandidateRoot
    CandidateVersion = [string]$releaseManifest.DTMAPIVersion
    ExpectedCandidateVersion = $ExpectedCandidateVersion
    CandidateBuildCommit = [string]$releaseManifest.BuildCommit
    SaveSlot = 3
    SaveTestMode = 'NoNativeSave'
    Preflight = $preflightResult
    PostflightWorkshopTree = $postflight
    WorkshopArtifactUnchanged = $artifactUnchanged
    BaseRunnerPassed = $baseRunnerPassed
    LoadSourceEvidenceCount = $loadSourceMatches.Count
    LoadSourceLines = @($loadSourceMatches | ForEach-Object { [string]$_.Line })
    WorkshopLoadSourcePassed = $loadSourcePassed
    DuplicateSelectionEvidenceCount = $duplicateMatches.Count
    DuplicateSelectionLines = @($duplicateMatches | ForEach-Object { [string]$_.Line })
    LocalDuplicateIsolated = $duplicateSelectionPassed
    EntryCompletedCount = $entryMatches.Count
    RegisterSuccessCount = $registerMatches.Count
    WavReadyCount = $wavReadyMatches.Count
    NativeHookInstalledCount = $nativeHookMatches.Count
    NativeHookInstalledLines = @($nativeHookMatches | ForEach-Object { [string]$_.Line })
    FinalHookInstalledCount = $finalHookInstalledMatches.Count
    FinalHookInstalledLines = @($finalHookInstalledMatches | ForEach-Object { [string]$_.Line })
    CompatibilityFailureCount = $compatibilityFailures.Count
    CompatibilityFailureLines = @($compatibilityFailures | ForEach-Object { [string]$_.Line })
    CardboardInteractionRequired = $false
    NativeSaveInvoked = $false
    Passed = $passed
}
Write-PrereleaseManboJson -Path (Join-Path $evidencePath 'prerelease-manbo-compatibility.json') -Value $gate
Write-Output ('DTMAPI_MANBO_COMPATIBILITY_EVIDENCE_PATH=' + $evidencePath)
if (-not $passed) {
    throw "Prerelease Manbo compatibility gate failed. Evidence: $evidencePath"
}
Write-Host "Prerelease Manbo compatibility gate passed. Evidence: $evidencePath"
