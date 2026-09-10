[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'player-save-repair/NativeArchive.psm1') -Force
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $repoRoot ('temp/player-save-repair-test-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testRoot)
$utf8 = New-Object Text.UTF8Encoding($false, $true)
$tool = Join-Path $PSScriptRoot 'invoke-player-save-repair.ps1'
$windowsPowerShell = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$checks = New-Object 'System.Collections.Generic.List[string]'
function Assert([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }
function Pass([string] $Name) { [void]$checks.Add($Name) }
function Refuses([scriptblock] $Operation, [string] $Name) {
    $rejected = $false
    try { & $Operation | Out-Null } catch { $rejected = $true }
    Assert $rejected ('Expected refusal: ' + $Name)
    Pass $Name
}
$key = [byte[]](0..31)
$iv = [byte[]](32..47)
$profilePath = Join-Path $testRoot 'synthetic-profile.json'
Write-RepairNewJson $profilePath ([ordered]@{
    FormatVersion = 1; FormatId = 'doloc-aes-cbc-utf8-v1'; ReferenceBuild = '24966367_public_958EAF'
    ResourceSha256 = ('A' * 64); ArchiveFileName = 'doloc-save-{0}.data'; Prefix = 'DOLOC-TOWN:'
    SupportedSaveVersions = @('1.00.02', '1.00.06'); SyntheticFixture = $true
    KeyBase64 = [Convert]::ToBase64String($key); IVBase64 = [Convert]::ToBase64String($iv)
})
# Anonymous schema fixture only; no world/player data and never game-loadable.
# The real reviewed case stores wetland_main@1 in finishMissions. Its unrelated
# decorator IDs are represented by an empty set, never by the mission ID.
$date = '{"TotalTUs":123456,"TotalDays":170,"Minute":15,"Hour":12,"Day":2,"Month":3,"Year":2,"WeekDay":"Monday"}'
$fixtureText = @'
{
 "archiveIndex":0,
 "baseData":{"archiveIndex":0,"version":"1.00.06","dateNow":DATE},
 "unrelated":{"spaced" : [1e2, -0.0, "UNICODE", "\u4e2d", "brackets [ ] { }"]},
 "farmData":{
  "missionChainManager":{"handles":{},"completedChains":{"wetland_main":1},"chainInfos":{"ruinedcity_main":["ruinedcity_main_0"]}},
  "missionManager":{"finishMissions":["wetland_main_0","wetland_main_1","wetland_main_2","wetland_main_3","wetland_main_4","wetland_main_5","wetland_main_6","wetland_main@1"],"finishDecorators":[],"totalMissions":[]},
  "emailManager":{"emails":[ 
    {"id":"wetland_main","isNew":false,"emailAttaches":[],"sendData":{"TotalDays":60},"recycled":false,"collected":false}
  ]}},
 "cityData":{"dialogueManager":{"visitedArgs":{"ruinedcity_main_entsk":1},"dialogueDatas":{"aokema":{"candidateNodes":[]}},"unhandledDialogueNodes":[]}},
 "timeData":{"dateNow":DATE}
}
'@
$fixtureText = $fixtureText.Replace('DATE', $date).Replace('UNICODE', (([char]0x4E2D).ToString() + [char]0x6587))
function Make-Source([string] $Name, [string] $Text) {
    $directory = Join-Path $testRoot $Name
    [void][IO.Directory]::CreateDirectory($directory)
    $path = Join-Path $directory 'doloc-save-0.data'
    $bytes = [DtmApi.PlayerSaveRepair.ArchiveFormat]::Encode($utf8.GetBytes($Text), $key, $iv)
    Write-RepairNewBytes $path $bytes
    return [pscustomobject]@{ Path = $path; Hash = Get-RepairSha $bytes; Plain = $utf8.GetBytes($Text) }
}
function Inspect-Source($Source) {
    return & $tool -Action Inspect -CaseId FIXTURE-ruinedcity -SourceFile $Source.Path -ExpectedSourceSha256 $Source.Hash -FormatProfile $profilePath
}
function Prepare-Source($Source, [string] $Name) {
    $path = Join-Path $testRoot $Name
    & $tool -Action Prepare -CaseId FIXTURE-ruinedcity -SourceFile $Source.Path -ExpectedSourceSha256 $Source.Hash -FormatProfile $profilePath -PreparedDirectory $path | Out-Null
    return $path
}
function Verify-Source($Source, [string] $Path, [string] $Case = 'FIXTURE-ruinedcity') {
    return & $tool -Action Verify -CaseId $Case -SourceFile $Source.Path -ExpectedSourceSha256 $Source.Hash -FormatProfile $profilePath -PreparedDirectory $Path
}

try {
    $source = Make-Source 'source' $fixtureText
    $originalInfo = Get-Item -LiteralPath $source.Path
    $originalMtime = $originalInfo.LastWriteTimeUtc
    Assert ((Inspect-Source $source).Status -eq 'Eligible') 'Known case was not admitted.'
    Pass 'known case eligible; definition-only chainInfos does not count as active'
    $oldVersion = Make-Source 'older-version' $fixtureText.Replace('1.00.06', '1.00.02')
    Assert ((Inspect-Source $oldVersion).Status -eq 'Eligible') 'Second reviewed version was not admitted.'
    Pass 'both reviewed save versions admitted'
    $prepared = Prepare-Source $source 'prepared'
    $candidatePath = Join-Path $prepared 'doloc-save-0.data'
    $candidateBytes = Read-RepairBytes $candidatePath
    $candidatePlain = [DtmApi.PlayerSaveRepair.ArchiveFormat]::Decode($candidateBytes, $key, $iv)
    $candidateText = $utf8.GetString($candidatePlain)
    $firstBracket = $fixtureText.IndexOf('"emails":[') + '"emails":['.Length
    $insertionLength = $candidateText.Length - $fixtureText.Length
    Assert ($candidateText.Remove($firstBracket, $insertionLength) -ceq $fixtureText) 'Independent text-span removal differs.'
    Assert ([DtmApi.PlayerSaveRepair.ArchiveFormat]::Same($utf8.GetBytes($candidateText.Remove($firstBracket, $insertionLength)), $source.Plain)) 'Original plaintext bytes differ.'
    Assert (([regex]::Matches($candidateText, '"id":"ruinedcity_continue"')).Count -eq 1) 'Mail insertion is not unique.'
    $parsedCandidate = $candidateText | ConvertFrom-Json
    $addedMail = $parsedCandidate.farmData.emailManager.emails[0]
    Assert ($addedMail.isNew -and -not $addedMail.recycled -and -not $addedMail.collected) 'Mail flags are wrong.'
    Assert ($addedMail.emailAttaches[0].autoAccept -and -not $addedMail.emailAttaches[0].isAccept) 'Native mission attachment flags are wrong.'
    Assert ($addedMail.sendData.TotalDays -eq 170) 'Mail changed the archive date.'
    Pass 'single native mail and exact plaintext restoration including UTF-8, whitespace, exponent and escapes'
    $verified = Verify-Source $source $prepared
    Assert ($verified.Status -eq 'VerifiedOffline' -and $verified.RuntimeValidation -eq 'not-run') 'Offline validation misclassified.'
    $delivery = [IO.Compression.ZipFile]::OpenRead($verified.RecoveryZip)
    try {
        Assert ($delivery.Entries.Count -eq 5) 'Recovery package contract changed.'
        $entry = $delivery.GetEntry('verified-recovery-source.data')
        $stream = $entry.Open(); $memory = New-Object IO.MemoryStream
        try { $stream.CopyTo($memory); Assert ([DtmApi.PlayerSaveRepair.ArchiveFormat]::Same($memory.ToArray(), $candidateBytes)) 'Final delivery payload differs.' }
        finally { $stream.Dispose(); $memory.Dispose() }
    }
    finally { $delivery.Dispose() }
    Pass 'Verify integrates existing five-file slot0 recovery wrapper; final ZIP payload byte equality'
    Refuses { Prepare-Source $source 'prepared' } 'existing output never overwritten'
    $verificationMtime = (Get-Item -LiteralPath (Join-Path $prepared 'verification.json')).LastWriteTimeUtc
    Assert ((Verify-Source $source $prepared).Reused) 'Repeated Verify did not reuse the unchanged proof.'
    Assert ((Get-Item -LiteralPath (Join-Path $prepared 'verification.json')).LastWriteTimeUtc -eq $verificationMtime) 'Repeated Verify rewrote the proof.'
    Pass 'repeated Verify rechecks bytes and reuses evidence without rewriting'
    Refuses { Verify-Source $source $prepared 'FIXTURE-other-case' } 'receipt cannot cross case identity'
    $repairedSource = [pscustomobject]@{ Path = $candidatePath; Hash = Get-RepairSha $candidateBytes }
    Assert ((Inspect-Source $repairedSource).Status -eq 'NoRepairNeeded') 'Repeated repair was not a no-op.'
    Refuses { Prepare-Source $repairedSource 'twice' } 'prepared source cannot receive duplicate mail'
    Pass 'Inspect reports existing mail as no repair needed'

    $negativeCases = @(
        @('wrong-slot', $fixtureText.Replace('"archiveIndex":0', '"archiveIndex":1'), 'NativeSlotIdentityMismatch'),
        @('wrong-base-slot', $fixtureText.Replace('"baseData":{"archiveIndex":0', '"baseData":{"archiveIndex":1'), 'NativeSlotIdentityMismatch'),
        @('unknown-version', $fixtureText.Replace('1.00.06', '1.99.99'), 'UnsupportedSaveVersion'),
        @('active-chain', $fixtureText.Replace('"handles":{}', '"handles":{"ruinedcity_main":{}}'), 'RuinedCityTaskAlreadyStartedOrCompleted'),
        @('finished-chain', $fixtureText.Replace('"wetland_main":1', '"wetland_main":1,"ruinedcity_main":1'), 'RuinedCityTaskAlreadyStartedOrCompleted'),
        @('finished-mission', $fixtureText.Replace('"wetland_main_6"', '"wetland_main_6","ruinedcity_main_0"'), 'RuinedCityTaskAlreadyStartedOrCompleted'),
        @('active-mission', $fixtureText.Replace('"totalMissions":[]', '"totalMissions":[{"id":"ruinedcity_main_0"}]'), 'RuinedCityTaskAlreadyStartedOrCompleted'),
        @('finished-decorator', $fixtureText.Replace('"finishDecorators":[]', '"finishDecorators":["ruinedcity_main@1"]'), 'RuinedCityTaskAlreadyStartedOrCompleted'),
        @('missing-prerequisite', $fixtureText.Replace('"wetland_main":1', '"wetland_main":0'), 'WetlandChainNotCompleted'),
        @('unfinished-prerequisite-mission', $fixtureText.Replace('"wetland_main_6"', '"other_6"'), 'WetlandMissionsIncomplete'),
        @('unfinished-prerequisite-implicit-mission', $fixtureText.Replace('"wetland_main@1"', '"other@1"'), 'WetlandMissionsIncomplete'),
        @('mission-id-only-in-decorators', $fixtureText.Replace(',"wetland_main@1"', '').Replace('"finishDecorators":[]', '"finishDecorators":["wetland_main@1"]'), 'WetlandMissionsIncomplete'),
        @('unread-prerequisite', $fixtureText.Replace('"isNew":false', '"isNew":true'), 'PrerequisiteMailMissingUnreadOrRecycled'),
        @('waiting-for-day', $fixtureText.Replace('"TotalDays":60', '"TotalDays":170'), 'WaitingForNativeNextDay'),
        @('unvisited', $fixtureText.Replace('"ruinedcity_main_entsk":1', '"ruinedcity_main_entsk":0'), 'RecoveryDialogueNotVisited'),
        @('already-migrated', $fixtureText.Replace('"ruinedcity_main_entsk":1', '"ruinedcity_main_entsk":1,"version_patch0900":1'), 'RecoveryMigrationAlreadyVisited'),
        @('pending-dialogue', $fixtureText.Replace('"candidateNodes":[]', '"candidateNodes":["ruinedcity_main_entsk"]'), 'DialogueStillPending'),
        @('queued-dialogue', $fixtureText.Replace('"unhandledDialogueNodes":[]', '"unhandledDialogueNodes":[{}]'), 'DialogueStillPending')
    )
    foreach ($row in $negativeCases) {
        $caseSource = Make-Source $row[0] $row[1]
        $inspection = Inspect-Source $caseSource
        Assert ($inspection.Status -eq 'Refused' -and $inspection.Reason -eq $row[2]) ('Incorrect refusal: ' + $row[0])
        Pass ('admission/' + $row[0])
    }
    foreach ($existing in @(
        '{"id":"ruinedcity_continue","emailAttaches":[],"recycled":true}',
        '{"id":"ruinedcity_main","emailAttaches":[]}',
        '{"id":"other-mail","emailAttaches":[{"missionChainId":"ruinedcity_main"}]}')) {
        $existingSource = Make-Source ('existing-' + [Guid]::NewGuid().ToString('N')) $fixtureText.Replace('"emails":[', ('"emails":[' + $existing + ','))
        Assert ((Inspect-Source $existingSource).Status -eq 'NoRepairNeeded') 'Existing or recycled recovery mail was ignored.'
    }
    Pass 'existing normal/recovery/recycled/alternate attachment mail is never duplicated'
    $duplicate = Make-Source 'duplicate-json' $fixtureText.Replace('"archiveIndex":0,', '"archiveIndex":0,"archive\u0049ndex":0,')
    Refuses { Inspect-Source $duplicate } 'duplicate escaped JSON properties refused'
    $caseAlias = Make-Source 'case-alias-json' $fixtureText.Replace('"archiveIndex":0,', '"archiveIndex":0,"ArchiveIndex":9,')
    Refuses { Inspect-Source $caseAlias } 'native case-insensitive property aliases cannot bypass slot identity'
    $badShape = Make-Source 'bad-shape' $fixtureText.Replace('"totalMissions":[]', '"totalMissions":{}')
    Refuses { Inspect-Source $badShape } 'unknown field shape refused'
    $wrongHash = [pscustomobject]@{ Path = $source.Path; Hash = ('0' * 64) }
    Refuses { Inspect-Source $wrongHash } 'unmatched source SHA refused'
    $corrupt = Join-Path $testRoot 'corrupt/doloc-save-0.data'
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($corrupt))
    Write-RepairNewBytes $corrupt $utf8.GetBytes('DOLOC-TOWN:not-base64!')
    Refuses { Inspect-Source ([pscustomobject]@{ Path = $corrupt; Hash = Get-RepairSha (Read-RepairBytes $corrupt) }) } 'invalid envelope refused'
    Refuses { [DtmApi.PlayerSaveRepair.ArchiveFormat]::Decode([byte[]](255, 254, 0, 0), $key, $iv) } 'invalid UTF-8 refused'
    Refuses { [DtmApi.PlayerSaveRepair.JsonReader]::Parse(('[1,' * 130) + '0' + (']' * 130)) } 'excessive JSON depth refused'

    $supportZip = Join-Path $testRoot 'support.zip'
    Write-RepairNewZip $supportZip 'PlayerData/Test/SAVE/doloc-save-0.data' (Read-RepairBytes $source.Path)
    $zipInspect = & $tool -Action Inspect -CaseId FIXTURE-ruinedcity -SupportZip $supportZip -ExpectedSourceSha256 $source.Hash -FormatProfile $profilePath
    Assert ($zipInspect.Status -eq 'Eligible' -and $zipInspect.Binding.SourcePackageSha256.Length -eq 64) 'Support ZIP admission failed.'
    Pass 'collector-style support ZIP source binding'
    $zipPrepared = Join-Path $testRoot 'zip-prepared'
    & $tool -Action Prepare -CaseId FIXTURE-ruinedcity -SupportZip $supportZip -ExpectedSourceSha256 $source.Hash -FormatProfile $profilePath -PreparedDirectory $zipPrepared | Out-Null
    $zipVerified = & $tool -Action Verify -CaseId FIXTURE-ruinedcity -SupportZip $supportZip -ExpectedSourceSha256 $source.Hash -FormatProfile $profilePath -PreparedDirectory $zipPrepared
    Assert ($zipVerified.Status -eq 'VerifiedOffline') 'Support ZIP Prepare/Verify failed.'
    Pass 'support ZIP executes all three stages and generates a bound recovery package'
    $ambiguous = Join-Path $testRoot 'ambiguous.zip'
    Write-RepairNewZip $ambiguous 'PlayerData/One/SAVE/doloc-save-0.data' (Read-RepairBytes $source.Path)
    $zip = [IO.Compression.ZipFile]::Open($ambiguous, [IO.Compression.ZipArchiveMode]::Update)
    try { [void]$zip.CreateEntry('PlayerData/Two/SAVE/doloc-save-0.data') } finally { $zip.Dispose() }
    Refuses { Read-RepairZip -Path $ambiguous -SupportPackage } 'ambiguous support slot0 entries refused'

    $tampered = Prepare-Source $source 'tampered'
    $tamperData = Join-Path $tampered 'doloc-save-0.data'
    # Update all public hashes as an adversarial fixture: semantic equality must
    # still fail because the original whitespace/number spellings are evidence.
    $alteredPlain = $utf8.GetBytes($candidateText.Replace('1e2', '100'))
    $alteredBytes = [DtmApi.PlayerSaveRepair.ArchiveFormat]::Encode($alteredPlain, $key, $iv)
    [IO.File]::WriteAllBytes($tamperData, $alteredBytes)
    [IO.File]::Delete((Join-Path $tampered 'candidate.zip'))
    Write-RepairNewZip (Join-Path $tampered 'candidate.zip') 'doloc-save-0.data' $alteredBytes
    $tamperReceipt = Get-Content -LiteralPath (Join-Path $tampered 'repair-receipt.json') -Raw | ConvertFrom-Json
    $tamperReceipt.CandidateSha256 = Get-RepairSha $alteredBytes
    $tamperReceipt.CandidateLength = $alteredBytes.Length
    $tamperReceipt.CandidateZipSha256 = (Read-RepairZip (Join-Path $tampered 'candidate.zip')).ZipSha256
    [IO.File]::WriteAllText((Join-Path $tampered 'repair-receipt.json'), ($tamperReceipt | ConvertTo-Json -Depth 12), $utf8)
    Refuses { Verify-Source $source $tampered } 'semantic-equivalent unrelated byte edits fail even with recomputed artifact hashes'
    Assert (-not [IO.File]::Exists((Join-Path $tampered 'verified-slot0-recovery.zip'))) 'Rejected candidate produced a recovery package.'
    $changedSource = Make-Source 'advanced-source' $fixtureText.Replace('1e2', '2e2')
    Refuses { Verify-Source $changedSource $prepared } 'player continued saving cannot reuse old receipt'
    $profileChanged = Get-Content -LiteralPath $profilePath -Raw
    [IO.File]::WriteAllText($profilePath, ($profileChanged + "`n"), $utf8)
    Refuses { Verify-Source $source $prepared } 'profile fingerprint change invalidates prepared receipt'
    [IO.File]::WriteAllText($profilePath, $profileChanged, $utf8)
    $unreviewedProfile = $profileChanged | ConvertFrom-Json
    $unreviewedProfile.SyntheticFixture = $false
    $unreviewedPath = Join-Path $testRoot 'unreviewed-profile.json'
    Write-RepairNewJson $unreviewedPath $unreviewedProfile
    Refuses { Read-RepairProfile $unreviewedPath } 'production profile must pin the reviewed resource baseline'
    Assert ((Get-RepairSha (Read-RepairBytes $source.Path)) -eq $source.Hash -and (Get-Item -LiteralPath $source.Path).LastWriteTimeUtc -eq $originalMtime) 'Source changed.'
    Pass 'source length/hash/mtime unchanged throughout'

    # The generated production manifest is first verified unchanged. Only this
    # synthetic extracted copy receives the existing explicit test overrides.
    $restoreRoot = Join-Path $testRoot 'restore-wrapper'
    [IO.Compression.ZipFile]::ExtractToDirectory($verified.RecoveryZip, $restoreRoot)
    $manifestPath = Join-Path $restoreRoot 'recovery-manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    Assert ($manifest.CaseId -eq 'FIXTURE-ruinedcity' -and $manifest.DamagedCurrentSha256 -eq $source.Hash -and $manifest.SlotIndex -eq 0) 'Production manifest binding differs.'
    $manifest.CaseId = 'TEST-repair-integration'
    $manifest | Add-Member -NotePropertyName TestFixture -NotePropertyValue $true
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 8), $utf8)
    $saveRoot = Join-Path $testRoot 'restore-target/SAVE'
    [void][IO.Directory]::CreateDirectory($saveRoot)
    $live = Join-Path $saveRoot 'doloc-save-0.data'
    [IO.File]::Copy($source.Path, $live, $false)
    Write-RepairNewBytes (Join-Path $saveRoot 'doloc-save-0.data.prev0') $utf8.GetBytes('unrelated-backup')
    $restoreScript = Join-Path $restoreRoot 'restore-verified-slot.ps1'
    & $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $restoreScript -SaveDirectory $saveRoot -SkipProcessCheck | Out-Null
    Assert ($LASTEXITCODE -eq 0 -and (Get-RepairSha (Read-RepairBytes $live)) -eq (Get-RepairSha $candidateBytes)) 'Generated wrapper did not restore synthetic slot0.'
    & $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $restoreScript -SaveDirectory $saveRoot -SkipProcessCheck | Out-Null
    Assert ($LASTEXITCODE -eq 0) 'Generated wrapper not idempotent.'
    [IO.File]::WriteAllBytes($live, (Read-RepairBytes $changedSource.Path))
    & $windowsPowerShell -NoProfile -ExecutionPolicy Bypass -File $restoreScript -SaveDirectory $saveRoot -SkipProcessCheck | Out-Null
    Assert ($LASTEXITCODE -eq 30) 'Generated wrapper accepted a later save.'
    Assert ([IO.File]::ReadAllText((Join-Path $saveRoot 'doloc-save-0.data.prev0')) -eq 'unrelated-backup') 'Wrapper changed backup.'
    Pass 'actual wrapper execution on synthetic slot0: apply, idempotent retry, later-save refusal, prev untouched'

    $report = [ordered]@{ Status = 'Passed'; Checks = $checks.ToArray(); Count = $checks.Count; TestRoot = $testRoot; PowerShell = $PSVersionTable.PSVersion.ToString(); RuntimeValidation = 'not-run'; SyntheticFixture = $true }
    Write-RepairNewJson (Join-Path $testRoot 'result.json') $report
    [pscustomobject]@{ Status = 'Passed'; Checks = $checks.Count; Evidence = Join-Path $testRoot 'result.json' }
}
catch {
    Write-RepairNewJson (Join-Path $testRoot 'failure.json') ([ordered]@{ Status = 'Failed'; CompletedChecks = $checks.ToArray(); Error = $_.Exception.Message; Position = $_.InvocationInfo.PositionMessage })
    throw
}
