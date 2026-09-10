[CmdletBinding(DefaultParameterSetName = 'Data')]
param(
    [Parameter(Mandatory = $true)] [ValidateSet('Inspect', 'Prepare', 'Verify')] [string] $Action,
    [ValidateSet('ruinedcity-mail-v1')] [string] $RepairMode = 'ruinedcity-mail-v1',
    [Parameter(Mandatory = $true)] [ValidatePattern('^[A-Za-z0-9._-]+$')] [string] $CaseId,
    [ValidateRange(0, 0)] [int] $SlotIndex = 0,
    [Parameter(Mandatory = $true, ParameterSetName = 'Data')] [string] $SourceFile,
    [Parameter(Mandatory = $true, ParameterSetName = 'Zip')] [string] $SupportZip,
    [Parameter(Mandatory = $true)] [ValidatePattern('^[A-Fa-f0-9]{64}$')] [string] $ExpectedSourceSha256,
    [Parameter(Mandatory = $true)] [string] $FormatProfile,
    [string] $PreparedDirectory = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'player-save-repair/NativeArchive.psm1') -Force
if ($CaseId.StartsWith('TEST-', [StringComparison]::Ordinal)) { throw 'CaseId may not use the recovery-wrapper TEST- prefix.' }
$profile = Read-RepairProfile $FormatProfile
$sourceKind = $PSCmdlet.ParameterSetName
if ($profile.SyntheticFixture -and -not $CaseId.StartsWith('FIXTURE-', [StringComparison]::Ordinal)) {
    throw 'Synthetic profiles require an explicit FIXTURE- case identifier.'
}
$sourceFull = if ($PSCmdlet.ParameterSetName -eq 'Data') { Assert-RepairPlainPath $SourceFile } else { Assert-RepairPlainPath $SupportZip }
$sourceInfo = New-Object IO.FileInfo($sourceFull)
$sourceLength = $sourceInfo.Length
$sourceMtime = $sourceInfo.LastWriteTimeUtc
$sourceZipHash = ''
$sourceEntry = ''
if ($PSCmdlet.ParameterSetName -eq 'Data') {
    if ([IO.Path]::GetFileName($sourceFull) -cne 'doloc-save-0.data') { throw 'Direct input must retain its native doloc-save-0.data identity.' }
    $sourceBytes = Read-RepairBytes $sourceFull
}
else {
    $sourcePackage = Read-RepairZip -Path $sourceFull -SupportPackage
    $sourceBytes = $sourcePackage.Bytes
    $sourceZipHash = $sourcePackage.ZipSha256
    $sourceEntry = $sourcePackage.Entry
}
$sourceHash = Get-RepairSha $sourceBytes
if ($sourceHash -cne $ExpectedSourceSha256.ToUpperInvariant()) { throw 'Source SHA-256 changed or does not match the case. Recollect and reassess; do not update the old receipt.' }
$plain = [DtmApi.PlayerSaveRepair.ArchiveFormat]::Decode($sourceBytes, $profile.Key, $profile.IV)
$plan = [DtmApi.PlayerSaveRepair.ArchiveFormat]::Inspect($plain)
$binding = [ordered]@{
    FormatVersion = 1
    CaseId = $CaseId
    RepairMode = $RepairMode
    SlotIndex = $SlotIndex
    TargetFileName = 'doloc-save-0.data'
    SourceSha256 = $sourceHash
    SourceLength = $sourceBytes.Length
    SourcePlaintextSha256 = Get-RepairSha $plain
    SourcePackageSha256 = $sourceZipHash
    SourceEntry = $sourceEntry
    ProfileSha256 = $profile.Sha256
    FormatId = $profile.FormatId
    ReferenceBuild = $profile.ReferenceBuild
    ResourceSha256 = $profile.ResourceSha256
    SaveVersion = $plan.SaveVersion
    SyntheticFixture = $profile.SyntheticFixture
}

function Assert-SourceUnchanged {
    $current = New-Object IO.FileInfo($sourceFull)
    if ($current.Length -ne $sourceLength -or $current.LastWriteTimeUtc -ne $sourceMtime) { throw 'Source metadata changed during repair.' }
    if ($sourceKind -eq 'Data') { $now = Get-RepairSha (Read-RepairBytes $sourceFull) }
    else {
        $nowPackage = Read-RepairZip -Path $sourceFull -SupportPackage
        if ($nowPackage.ZipSha256 -cne $sourceZipHash) { throw 'Support ZIP changed during repair.' }
        $now = Get-RepairSha $nowPackage.Bytes
    }
    if ($now -cne $sourceHash) { throw 'Source bytes changed during repair.' }
}

if ($Action -eq 'Inspect') {
    Assert-SourceUnchanged
    [pscustomobject]@{ Action = $Action; CaseId = $CaseId; Status = $plan.Status; Reason = $plan.Reason; Binding = [pscustomobject]$binding; EmailCount = $plan.EmailCount }
    return
}
if ($plan.Status -ne 'Eligible') { throw ('Repair refused: ' + $plan.Status + '/' + $plan.Reason) }
if ([string]::IsNullOrWhiteSpace($PreparedDirectory)) { throw 'Prepare/Verify require -PreparedDirectory.' }
$output = Assert-RepairPlainPath $PreparedDirectory
$sourceParent = [IO.Path]::GetDirectoryName($sourceFull)
if ($output -ieq $sourceParent -or $output -ieq $sourceFull) { throw 'Prepared output must be a separate new directory.' }
$candidatePath = Join-Path $output 'doloc-save-0.data'
$candidateZipPath = Join-Path $output 'candidate.zip'
$receiptPath = Join-Path $output 'repair-receipt.json'
$wrapperInputPath = Join-Path $output 'source-slot0.zip'
$recoveryPath = Join-Path $output 'verified-slot0-recovery.zip'
$verificationPath = Join-Path $output 'verification.json'

if ($Action -eq 'Prepare') {
    if ([IO.Directory]::Exists($output) -or [IO.File]::Exists($output)) { throw 'PreparedDirectory already exists. Existing artifacts are never overwritten.' }
    $candidatePlain = [DtmApi.PlayerSaveRepair.ArchiveFormat]::Apply($plain, $plan)
    $candidateBytes = [DtmApi.PlayerSaveRepair.ArchiveFormat]::Encode($candidatePlain, $profile.Key, $profile.IV)
    [void][IO.Directory]::CreateDirectory($output)
    Write-RepairNewBytes -Path $candidatePath -Bytes $candidateBytes
    Write-RepairNewZip -Path $candidateZipPath -EntryName 'doloc-save-0.data' -Bytes $candidateBytes
    # A bounded wrapper input contains only the already submitted source snapshot.
    # It is not a fresh copy of the player's live SAVE directory.
    Write-RepairNewZip -Path $wrapperInputPath -EntryName 'PlayerData/Case/SAVE/doloc-save-0.data' -Bytes $sourceBytes
    $reopenedData = Read-RepairBytes $candidatePath
    $reopenedZip = Read-RepairZip $candidateZipPath
    if (-not [DtmApi.PlayerSaveRepair.ArchiveFormat]::Same($reopenedData, $reopenedZip.Bytes)) { throw 'Final candidate ZIP and .data differ.' }
    foreach ($bytes in @($reopenedData, $reopenedZip.Bytes)) {
        [DtmApi.PlayerSaveRepair.ArchiveFormat]::Verify($plain, [DtmApi.PlayerSaveRepair.ArchiveFormat]::Decode($bytes, $profile.Key, $profile.IV))
    }
    $receipt = [ordered]@{}
    foreach ($key in $binding.Keys) { $receipt[$key] = $binding[$key] }
    $receipt['CandidateSha256'] = Get-RepairSha $reopenedData
    $receipt['CandidateZipSha256'] = $reopenedZip.ZipSha256
    $receipt['CandidateLength'] = $reopenedData.Length
    $receipt['InsertionByteOffset'] = $plan.InsertionByteOffset
    $receipt['InsertionByteLength'] = $plan.Insertion.Length
    $receipt['InsertionSha256'] = Get-RepairSha $plan.Insertion
    $receipt['EmailCountBefore'] = $plan.EmailCount
    $receipt['EmailCountAfter'] = $plan.EmailCount + 1
    $receipt['CreatedUtc'] = [DateTime]::UtcNow.ToString('o')
    $receipt['RuntimeValidation'] = 'not-run'
    Assert-SourceUnchanged
    Write-RepairNewJson -Path $receiptPath -Value $receipt
    [pscustomobject]@{ Action = $Action; Status = 'Prepared'; CaseId = $CaseId; PreparedDirectory = $output; CandidateSha256 = $receipt.CandidateSha256; RuntimeValidation = 'not-run' }
    return
}

$receiptBytes = Read-RepairBytes -Path $receiptPath -Limit 65536
$utf8 = New-Object Text.UTF8Encoding($false, $true)
$receiptText = $utf8.GetString($receiptBytes)
[void][DtmApi.PlayerSaveRepair.JsonReader]::Parse($receiptText)
$receipt = $receiptText | ConvertFrom-Json
foreach ($key in $binding.Keys) {
    if ($null -eq $receipt.PSObject.Properties[$key] -or [string]$receipt.$key -cne [string]$binding[$key]) { throw ('Receipt binding mismatch: ' + $key) }
}
$candidateBytes = Read-RepairBytes $candidatePath
$candidateZip = Read-RepairZip $candidateZipPath
if ((Get-RepairSha $candidateBytes) -cne $receipt.CandidateSha256 -or $candidateBytes.Length -ne $receipt.CandidateLength -or
    $candidateZip.ZipSha256 -cne $receipt.CandidateZipSha256 -or
    -not [DtmApi.PlayerSaveRepair.ArchiveFormat]::Same($candidateBytes, $candidateZip.Bytes) -or
    $receipt.InsertionByteOffset -ne $plan.InsertionByteOffset -or $receipt.InsertionByteLength -ne $plan.Insertion.Length -or
    $receipt.InsertionSha256 -cne (Get-RepairSha $plan.Insertion) -or
    $receipt.EmailCountBefore -ne $plan.EmailCount -or $receipt.EmailCountAfter -ne ($plan.EmailCount + 1)) {
    throw 'Prepared artifacts or insertion receipt changed.'
}
foreach ($bytes in @($candidateBytes, $candidateZip.Bytes)) {
    [DtmApi.PlayerSaveRepair.ArchiveFormat]::Verify($plain, [DtmApi.PlayerSaveRepair.ArchiveFormat]::Decode($bytes, $profile.Key, $profile.IV))
}
$wrapperInput = Read-RepairZip -Path $wrapperInputPath -SupportPackage
if (-not [DtmApi.PlayerSaveRepair.ArchiveFormat]::Same($wrapperInput.Bytes, $sourceBytes)) { throw 'Wrapper source snapshot differs from the case source.' }
Assert-SourceUnchanged
if ([IO.File]::Exists($recoveryPath) -or [IO.File]::Exists($verificationPath)) {
    if (-not [IO.File]::Exists($recoveryPath) -or -not [IO.File]::Exists($verificationPath)) {
        throw 'Incomplete verification output exists. Keep it for diagnosis and use a new prepared directory.'
    }
    $previousText = $utf8.GetString((Read-RepairBytes -Path $verificationPath -Limit 65536))
    [void][DtmApi.PlayerSaveRepair.JsonReader]::Parse($previousText)
    $previous = $previousText | ConvertFrom-Json
    if ($previous.FormatVersion -ne 1 -or $previous.CaseId -cne $CaseId -or $previous.RepairMode -cne $RepairMode -or
        $previous.SlotIndex -ne 0 -or $previous.SourceSha256 -cne $sourceHash -or
        $previous.CandidateSha256 -cne $receipt.CandidateSha256 -or $previous.ProfileSha256 -cne $profile.Sha256 -or
        $previous.ReceiptSha256 -cne (Get-RepairSha $receiptBytes) -or
        $previous.RecoveryPackage.Sha256 -cne (Get-RepairSha (Read-RepairBytes -Path $recoveryPath -Limit 67108864))) {
        throw 'Existing verification or recovery package differs from this case.'
    }
    Assert-SourceUnchanged
    [pscustomobject]@{ Action = $Action; Status = 'VerifiedOffline'; CaseId = $CaseId; RecoveryZip = $recoveryPath; CandidateFile = $candidatePath; RuntimeValidation = 'not-run'; SyntheticFixture = $profile.SyntheticFixture; Reused = $true }
    return
}
$built = & (Join-Path $PSScriptRoot 'build-player-slot0-recovery.ps1') -SupportZip $wrapperInputPath -OutputZip $recoveryPath -CaseId $CaseId -ExpectedDamagedSha256 $sourceHash -ExpectedRecoverySha256 $receipt.CandidateSha256 -PreparedRecoveryFile $candidatePath
Assert-SourceUnchanged
Write-RepairNewJson -Path $verificationPath -Value ([ordered]@{
    FormatVersion = 1; CaseId = $CaseId; RepairMode = $RepairMode; SlotIndex = 0
    SourceSha256 = $sourceHash; CandidateSha256 = $receipt.CandidateSha256
    ProfileSha256 = $profile.Sha256; ReceiptSha256 = Get-RepairSha $receiptBytes
    PlaintextDifference = 'One mail insertion; removing it restores every original plaintext byte'
    FinalDataAndCandidateZip = 'passed'; RecoveryPackage = $built
    RuntimeValidation = 'not-run'; SyntheticFixture = $profile.SyntheticFixture
})
[pscustomobject]@{ Action = $Action; Status = 'VerifiedOffline'; CaseId = $CaseId; RecoveryZip = $recoveryPath; CandidateFile = $candidatePath; RuntimeValidation = 'not-run'; SyntheticFixture = $profile.SyntheticFixture }
