<#
.SYNOPSIS
Runs the bounded 0.5.5 ActionSpeed and AutoFishing active-GC focus against exact frozen candidates.

.DESCRIPTION
The shared Runtime lock remains held for the complete operation. Each existing managed product
directory and its Author SDK deployment journal are leased by same-volume Directory.Move operations,
the frozen SDK deploys the exact candidate package, and the focused runners reuse the parent lock.
After every game process and source-state transaction is closed, the candidate directory/journal are
moved out and the exact originals are restored. Existing SDK recovery artifacts are never moved,
rewritten, or deleted. Each move intent is durably recorded before mutation; -RecoverOnly resumes an
interrupted lease only while the stale shared Runtime lock and exact recorded paths still match.
#>
param(
    [string] $GameDir = '',
    [string] $RuntimeCandidateRoot = '',
    [string] $ActionSpeedOutputRoot = '',
    [string] $AutoFishingOutputRoot = '',
    [string] $AuthorSdkOutputRoot = '',
    [string] $EvidenceRoot = '',
    [string] $TransactionId = '',
    [ValidateRange(1, 300)] [int] $MeasureSeconds = 10,
    [ValidateRange(1, 60)] [int] $SampleSeconds = 5,
    [ValidateRange(1, 500)] [int] $ActionTargetUnits = 1,
    [ValidateRange(0, 100)] [int] $AutoFishingWarmupFish = 5,
    [ValidateRange(1, 500)] [int] $AutoFishingTargetFish = 10,
    [ValidateRange(1, 86400)] [int] $TimeoutSeconds = 1200,
    [switch] $UseSteam,
    [switch] $RecoverOnly,
    [switch] $LibraryOnly,
    [Parameter(DontShow = $true)] [scriptblock] $DeployActionForTests,
    [Parameter(DontShow = $true)] [string] $InjectFailureAfterCandidateForTests = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\batch5-gc-source-transaction.ps1"
$prereleaseGcOuterGameDir = $GameDir
$prereleaseGcOuterEvidenceRoot = $EvidenceRoot
$prereleaseGcOuterTransactionId = $TransactionId
$prereleaseGcOuterLibraryOnly = [bool]$LibraryOnly
. "$PSScriptRoot\candidate11-source-transaction.ps1" -LibraryOnly
$GameDir = $prereleaseGcOuterGameDir
$EvidenceRoot = $prereleaseGcOuterEvidenceRoot
$TransactionId = $prereleaseGcOuterTransactionId
$LibraryOnly = $prereleaseGcOuterLibraryOnly
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$script:PrereleaseGcUtf8 = New-Object System.Text.UTF8Encoding($false)
$script:PrereleaseGcActionSpeedStages = @(
    'ActionSpeed-L0-Tool',
    'ActionSpeed-L1-Tool',
    'ActionSpeed-L3-Tool',
    'ActionSpeed-L3-Interact',
    'ActionSpeed-L3-Eat',
    'ActionSpeed-L3-ContinuousUse',
    'ActionSpeed-L4-Tool',
    'ActionSpeed-L5-Tool'
)
$script:PrereleaseGcAutoFishingLevels = @('L1','L3','L4','L5')

function Write-PrereleaseGcAtomicJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [object] $Value
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullPath)) | Out-Null
    $temporary = $fullPath + '.tmp-' + [Guid]::NewGuid().ToString('N')
    $backup = $fullPath + '.replace-backup-' + [Guid]::NewGuid().ToString('N')
    try {
        [System.IO.File]::WriteAllText($temporary, (($Value | ConvertTo-Json -Depth 100) + "`n"), $script:PrereleaseGcUtf8)
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            try {
                [System.IO.File]::Replace($temporary, $fullPath, $backup, $true)
            }
            catch [System.PlatformNotSupportedException] {
                Remove-Item -LiteralPath $fullPath -Force
                Move-Item -LiteralPath $temporary -Destination $fullPath
            }
        }
        else {
            Move-Item -LiteralPath $temporary -Destination $fullPath
        }
    }
    finally {
        if (Test-Path -LiteralPath $temporary -PathType Leaf) { Remove-Item -LiteralPath $temporary -Force }
        if (Test-Path -LiteralPath $backup -PathType Leaf) { Remove-Item -LiteralPath $backup -Force }
    }
}

function Get-PrereleaseGcFileSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $hasher = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    try {
        return ([System.BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-', '').ToUpperInvariant()
    }
    finally {
        $stream.Dispose()
        $hasher.Dispose()
    }
}

function Get-PrereleaseGcFileIdentity {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [ordered]@{ Exists=$false; Path=[System.IO.Path]::GetFullPath($Path); Length=[int64]0; Sha256='' }
    }
    $item = Get-Item -LiteralPath $Path -Force
    return [ordered]@{
        Exists = $true
        Path = [System.IO.Path]::GetFullPath($Path)
        Length = [int64]$item.Length
        Sha256 = Get-PrereleaseGcFileSha256 -Path $Path
    }
}

function Test-PrereleaseGcFileIdentity {
    param(
        [Parameter(Mandatory = $true)] [object] $Expected,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $actual = Get-PrereleaseGcFileIdentity -Path $Path
    return [bool]$Expected.Exists -eq [bool]$actual.Exists -and
        [int64]$Expected.Length -eq [int64]$actual.Length -and
        [string]::Equals([string]$Expected.Sha256, [string]$actual.Sha256, [System.StringComparison]::OrdinalIgnoreCase)
}

function Read-PrereleaseGcJson {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Required JSON is missing: $Path" }
    try { return ([System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json) }
    catch { throw [System.IO.InvalidDataException]::new("Invalid JSON at $Path. $($_.Exception.Message)", $_.Exception) }
}

function Get-PrereleaseGcPackageEntries {
    param([Parameter(Mandatory = $true)] [string] $PackagePath)

    if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
        throw "Frozen product package is missing: $PackagePath"
    }
    Add-Type -AssemblyName System.IO.Compression
    $stream = [System.IO.File]::Open(
        $PackagePath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    $archive = $null
    try {
        $archive = New-Object System.IO.Compression.ZipArchive -ArgumentList @(
            $stream,
            [System.IO.Compression.ZipArchiveMode]::Read,
            $false)
        $rows = New-Object System.Collections.ArrayList
        $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in @($archive.Entries | Sort-Object FullName)) {
            $relative = ([string]$entry.FullName).Replace([char]92, [char]47)
            if ([string]::IsNullOrWhiteSpace($relative) -or $relative.EndsWith('/', [System.StringComparison]::Ordinal)) {
                continue
            }
            if ($relative.StartsWith('/', [System.StringComparison]::Ordinal) -or
                $relative -match '(^|/)\.\.(/|$)' -or
                [string]::Equals($relative, '.dtmapi-author-receipt.json', [System.StringComparison]::OrdinalIgnoreCase) -or
                -not $seen.Add($relative)) {
                throw "Frozen product package contains an unsafe or duplicate entry: $relative"
            }
            $entryStream = $entry.Open()
            $hasher = [System.Security.Cryptography.SHA256]::Create()
            try {
                $hash = ([System.BitConverter]::ToString($hasher.ComputeHash($entryStream))).Replace('-', '')
            }
            finally {
                $hasher.Dispose()
                $entryStream.Dispose()
            }
            [void]$rows.Add([ordered]@{
                Path = $relative
                Length = [int64]$entry.Length
                Sha256 = $hash
            })
        }
        if ($rows.Count -lt 1) { throw "Frozen product package contains no files: $PackagePath" }
        return @($rows.ToArray())
    }
    finally {
        if ($null -ne $archive) { $archive.Dispose() }
        $stream.Dispose()
    }
}

function Test-PrereleaseGcPathEquals {
    param([Parameter(Mandatory = $true)] [string] $Left, [Parameter(Mandatory = $true)] [string] $Right)
    return [string]::Equals(
        [System.IO.Path]::GetFullPath($Left).TrimEnd([char]92, [char]47),
        [System.IO.Path]::GetFullPath($Right).TrimEnd([char]92, [char]47),
        [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-PrereleaseGcCandidatePackageSnapshot {
    param(
        [Parameter(Mandatory = $true)] [object] $State,
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $snapshot = Get-Candidate11TreeSnapshot -Path $Path -Context $Context
    $receiptPath = Join-Path $Path '.dtmapi-author-receipt.json'
    $receipt = Read-PrereleaseGcJson -Path $receiptPath
    if ([int]$receipt.schemaVersion -ne 2 -or [string]$receipt.uniqueId -cne [string]$State.UniqueId -or
        [string]$receipt.packageKind -cne 'CodeMod' -or [string]$receipt.codeModKind -cne 'Advanced' -or
        [string]$receipt.destinationRelativePath -cne ('Mods/' + [string]$State.UniqueId) -or
        -not [string]::Equals([string]$receipt.packageSha256, [string]$State.PackageSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Context receipt is not bound to the frozen candidate."
    }

    $expectedEntries = @($State.PackageEntries)
    if ($expectedEntries.Count -lt 1) { throw "$Context has no durable frozen-package entry ledger." }
    $actualFiles = @(
        Get-ChildItem -LiteralPath $Path -Recurse -Force -File -ErrorAction Stop |
            ForEach-Object {
                [pscustomobject]@{
                    Item = $_
                    Relative = $_.FullName.Substring(([System.IO.Path]::GetFullPath($Path)).TrimEnd([char]92, [char]47).Length + 1).Replace([char]92, [char]47)
                }
            } |
            Where-Object { -not [string]::Equals([string]$_.Relative, '.dtmapi-author-receipt.json', [System.StringComparison]::OrdinalIgnoreCase) }
    )
    if ($actualFiles.Count -ne $expectedEntries.Count) {
        throw "$Context file count does not match the frozen package."
    }
    foreach ($expected in $expectedEntries) {
        $matches = @($actualFiles | Where-Object { [string]$_.Relative -ceq [string]$expected.Path })
        if ($matches.Count -ne 1) { throw "$Context is missing frozen package entry $($expected.Path)." }
        $identity = Get-PrereleaseGcFileIdentity -Path ([string]$matches[0].Item.FullName)
        if ([int64]$identity.Length -ne [int64]$expected.Length -or
            -not [string]::Equals([string]$identity.Sha256, [string]$expected.Sha256, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "$Context package entry drifted: $($expected.Path)"
        }
    }
    return $snapshot
}

function Set-PrereleaseGcLeasePhase {
    param(
        [Parameter(Mandatory = $true)] [object] $Lease,
        [Parameter(Mandatory = $true)] [string] $Phase,
        [string] $PendingOperation = ''
    )

    $Lease.State.Phase = $Phase
    $Lease.State.PendingOperation = $PendingOperation
    Write-PrereleaseGcAtomicJson -Path ([string]$Lease.StatePath) -Value $Lease.State
}

function Invoke-PrereleaseGcProcessExitForTests {
    param(
        [Parameter(Mandatory = $true)] [object] $Lease,
        [Parameter(Mandatory = $true)] [string] $Point
    )

    if ([bool]$Lease.TestMode -and
        [string]::Equals([string]$env:DTMAPI_PRERELEASE_GC_TEST_EXIT_POINT, $Point, [System.StringComparison]::Ordinal)) {
        [Environment]::Exit(197)
    }
}

function Assert-PrereleaseGcJournalIdentity {
    param(
        [Parameter(Mandatory = $true)] [object] $Journal,
        [Parameter(Mandatory = $true)] [string] $UniqueId,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $Destination,
        [switch] $RequireInstalled,
        [string] $ExpectedPackageSha256 = ''
    )

    if ([int]$Journal.schemaVersion -notin @(2,3) -or [string]$Journal.uniqueId -cne $UniqueId -or
        [string]$Journal.packageKind -cne 'CodeMod' -or [string]$Journal.codeModKind -cne 'Advanced' -or
        -not (Test-PrereleaseGcPathEquals -Left ([string]$Journal.gameRoot) -Right $GameDir) -or
        -not (Test-PrereleaseGcPathEquals -Left ([string]$Journal.destinationPath) -Right $Destination)) {
        throw "Deployment journal identity drifted for $UniqueId."
    }
    if ($RequireInstalled -and ([string]$Journal.status -cne 'Installed' -or $null -eq $Journal.committed -or
        $null -ne $Journal.active -or ($Journal.PSObject.Properties.Name -contains 'localInstall' -and $null -ne $Journal.localInstall))) {
        throw "Deployment journal is not a terminal Installed authority for $UniqueId."
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedPackageSha256) -and
        -not [string]::Equals([string]$Journal.committed.packageSha256, $ExpectedPackageSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Deployment journal package SHA-256 drifted for $UniqueId."
    }
}

function Invoke-PrereleaseGcSdkJson {
    param(
        [Parameter(Mandatory = $true)] [string] $SdkExe,
        [Parameter(Mandatory = $true)] [string[]] $Arguments,
        [Parameter(Mandatory = $true)] [string] $ReceiptPath
    )

    $output = @(& $SdkExe @Arguments '--json' 2>&1)
    $exit = $LASTEXITCODE
    $text = [string]::Join([Environment]::NewLine, @($output | ForEach-Object { [string]$_ }))
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $ReceiptPath)) | Out-Null
    [System.IO.File]::WriteAllText($ReceiptPath, $text + [Environment]::NewLine, $script:PrereleaseGcUtf8)
    if ($exit -ne 0) { throw "Author SDK command failed (exit=$exit): $([string]::Join(' ', $Arguments)). Receipt: $ReceiptPath" }
    try { $report = $text | ConvertFrom-Json }
    catch { throw "Author SDK returned invalid JSON for $([string]::Join(' ', $Arguments)). Receipt: $ReceiptPath" }
    if (-not [bool]$report.success) { throw "Author SDK reported failure for $([string]::Join(' ', $Arguments)). Receipt: $ReceiptPath" }
    return $report
}

function Get-PrereleaseGcProductSpec {
    param(
        [Parameter(Mandatory = $true)] [ValidateSet('ActionSpeed','AutoFishing')] [string] $Domain,
        [Parameter(Mandatory = $true)] [string] $OutputRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $AuthorStateRoot
    )

    $summaryPath = Join-Path $OutputRoot 'summary.json'
    $summary = Read-PrereleaseGcJson -Path $summaryPath
    $expected = if ($Domain -eq 'ActionSpeed') {
        [ordered]@{ CatalogId='action-speed'; UniqueId='Yuuka.DTMAPI.ActionSpeed'; PackageName='DTMAPI-ActionSpeed-advanced-pilot.zip' }
    }
    else {
        [ordered]@{ CatalogId='auto-fishing'; UniqueId='Yuuka.DTMAPI.AutoFishing'; PackageName='DTMAPI-AutoFishing-advanced-pilot.zip' }
    }
    $packagePath = Join-Path $OutputRoot ([string]$expected.PackageName)
    if ([int]$summary.schemaVersion -ne 1 -or [string]$summary.status -cne 'Passed' -or
        [string]$summary.catalogId -cne [string]$expected.CatalogId -or
        [string]$summary.uniqueId -cne [string]$expected.UniqueId -or [string]$summary.version -cne '1.0.0' -or
        [string]$summary.codeModKind -cne 'Advanced' -or
        -not (Test-PrereleaseGcPathEquals -Left ([string]$summary.packagePath) -Right $packagePath)) {
        throw "Frozen $Domain product summary identity drifted."
    }
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) { throw "Frozen product package is missing: $packagePath" }
    $packageSha = Get-PrereleaseGcFileSha256 -Path $packagePath
    if (-not [string]::Equals($packageSha, [string]$summary.packageSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Frozen $Domain package SHA-256 drifted."
    }
    $destination = Join-Path (Join-Path $GameDir 'Mods') ([string]$expected.UniqueId)
    $journal = Join-Path (Join-Path $AuthorStateRoot 'deployments') (([string]$expected.UniqueId) + '.journal.json')
    return [pscustomobject]@{
        Domain = $Domain
        CatalogId = [string]$expected.CatalogId
        UniqueId = [string]$expected.UniqueId
        PackagePath = [System.IO.Path]::GetFullPath($packagePath)
        PackageSha256 = $packageSha
        PackageEntries = @(Get-PrereleaseGcPackageEntries -PackagePath $packagePath)
        EntryDllSha256 = ([string]$summary.entryDllSha256).ToUpperInvariant()
        DestinationPath = [System.IO.Path]::GetFullPath($destination)
        JournalPath = [System.IO.Path]::GetFullPath($journal)
    }
}

function Start-PrereleaseGcProductLease {
    param(
        [Parameter(Mandatory = $true)] [object] $Spec,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $SdkExe,
        [Parameter(Mandatory = $true)] [string] $Id,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [scriptblock] $TestDeployAction
    )

    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation "prerelease active-GC $($Spec.Domain) lease"
    $destination = [string]$Spec.DestinationPath
    $journalPath = [string]$Spec.JournalPath
    $originalSnapshot = Get-Candidate11TreeSnapshot -Path $destination -Context "$($Spec.Domain) original deployment" -AllowAlternateDataStreams
    $originalJournal = Read-PrereleaseGcJson -Path $journalPath
    Assert-PrereleaseGcJournalIdentity -Journal $originalJournal -UniqueId ([string]$Spec.UniqueId) -GameDir $GameDir `
        -Destination $destination -RequireInstalled
    $originalJournalIdentity = Get-PrereleaseGcFileIdentity -Path $journalPath

    $safeId = ([string]$Spec.UniqueId).Replace('.', '_')
    $productLeaseRoot = Join-Path (Join-Path $GameDir '.dtmapi-prerelease-active-gc') $Id
    $journalLeaseRoot = Join-Path (Join-Path (Split-Path -Parent (Split-Path -Parent $journalPath)) 'prerelease-active-gc') $Id
    $originalDestinationBackup = Join-Path $productLeaseRoot ($safeId + '-original')
    $candidateDestinationBackup = Join-Path $productLeaseRoot ($safeId + '-candidate')
    $originalJournalBackup = Join-Path $journalLeaseRoot ($safeId + '-original.journal.json')
    $candidateJournalBackup = Join-Path $journalLeaseRoot ($safeId + '-candidate.journal.json')
    foreach ($ownedPath in @($originalDestinationBackup,$candidateDestinationBackup,$originalJournalBackup,$candidateJournalBackup)) {
        if (Test-Path -LiteralPath $ownedPath) { throw "Prerelease active-GC owned lease path already exists: $ownedPath" }
    }

    $statePath = Join-Path $ReceiptRoot (([string]$Spec.CatalogId) + '-lease.json')
    $state = [pscustomobject][ordered]@{
        SchemaVersion = 2
        TransactionId = $Id
        Domain = [string]$Spec.Domain
        UniqueId = [string]$Spec.UniqueId
        Status = 'Prepared'
        Phase = 'Prepared'
        PendingOperation = ''
        GameDir = [System.IO.Path]::GetFullPath($GameDir)
        DestinationPath = $destination
        JournalPath = $journalPath
        OriginalDestinationBackup = $originalDestinationBackup
        CandidateDestinationBackup = $candidateDestinationBackup
        OriginalJournalBackup = $originalJournalBackup
        CandidateJournalBackup = $candidateJournalBackup
        OriginalSnapshot = $originalSnapshot
        OriginalJournalIdentity = $originalJournalIdentity
        CandidateSnapshot = $null
        CandidateJournalIdentity = $null
        PackagePath = [string]$Spec.PackagePath
        PackageSha256 = [string]$Spec.PackageSha256
        PackageEntries = @($Spec.PackageEntries)
        SdkExe = [string]$SdkExe
        ReceiptRoot = [System.IO.Path]::GetFullPath($ReceiptRoot)
        TestMode = $null -ne $TestDeployAction
        DestinationMoved = $false
        JournalMoved = $false
        CandidateInstalled = $false
        RestoredExact = $false
        GeneratedCandidateRemoved = $false
    }
    Write-PrereleaseGcAtomicJson -Path $statePath -Value $state
    $lease = [pscustomobject]@{ StatePath=$statePath; State=$state; Spec=$Spec; TestMode=($null -ne $TestDeployAction) }
    try {
        $operationLock = Open-Batch5GcAuthorOperationLock -GameDir $GameDir
        try {
            [System.IO.Directory]::CreateDirectory($productLeaseRoot) | Out-Null
            [System.IO.Directory]::CreateDirectory($journalLeaseRoot) | Out-Null
            Set-PrereleaseGcLeasePhase -Lease $lease -Phase 'OriginalDestinationLeasePending' -PendingOperation 'MoveOriginalDestinationToBackup'
            Move-Candidate11Directory -Source $destination -Destination $originalDestinationBackup -Context "$($Spec.Domain) lease original destination"
            Invoke-PrereleaseGcProcessExitForTests -Lease $lease -Point 'after-original-destination-move'
            $state.DestinationMoved = $true
            Set-PrereleaseGcLeasePhase -Lease $lease -Phase 'OriginalDestinationLeased'
            Set-PrereleaseGcLeasePhase -Lease $lease -Phase 'OriginalJournalLeasePending' -PendingOperation 'MoveOriginalJournalToBackup'
            Move-Item -LiteralPath $journalPath -Destination $originalJournalBackup
            Invoke-PrereleaseGcProcessExitForTests -Lease $lease -Point 'after-original-journal-move'
            $state.JournalMoved = $true
            Set-PrereleaseGcLeasePhase -Lease $lease -Phase 'OriginalsLeased'
        }
        finally {
            $operationLock.Dispose()
        }
        if (-not (Test-Candidate11SnapshotsEqual -Expected $originalSnapshot -Actual (
            Get-Candidate11TreeSnapshot -Path $originalDestinationBackup -Context "$($Spec.Domain) leased original" -AllowAlternateDataStreams))) {
            throw "$($Spec.Domain) original deployment changed during lease."
        }
        if (-not (Test-PrereleaseGcFileIdentity -Expected $originalJournalIdentity -Path $originalJournalBackup)) {
            throw "$($Spec.Domain) original journal changed during lease."
        }

        Set-PrereleaseGcLeasePhase -Lease $lease -Phase 'CandidatePublishPending' -PendingOperation 'PublishFrozenCandidate'
        if ($null -ne $TestDeployAction) {
            & $TestDeployAction $Spec $state
        }
        else {
            $null = Invoke-PrereleaseGcSdkJson -SdkExe $SdkExe -Arguments @('deploy',[string]$Spec.PackagePath,'--game-root',$GameDir) `
                -ReceiptPath (Join-Path $ReceiptRoot (([string]$Spec.CatalogId) + '-candidate-deploy.json'))
        }
        Invoke-PrereleaseGcProcessExitForTests -Lease $lease -Point 'after-candidate-publish'
        $candidateReceiptPath = Join-Path $destination '.dtmapi-author-receipt.json'
        $candidateReceipt = Read-PrereleaseGcJson -Path $candidateReceiptPath
        if ([int]$candidateReceipt.schemaVersion -ne 2 -or [string]$candidateReceipt.uniqueId -cne [string]$Spec.UniqueId -or
            [string]$candidateReceipt.packageKind -cne 'CodeMod' -or [string]$candidateReceipt.codeModKind -cne 'Advanced' -or
            [string]$candidateReceipt.destinationRelativePath -cne ('Mods/' + [string]$Spec.UniqueId) -or
            -not [string]::Equals([string]$candidateReceipt.packageSha256, [string]$Spec.PackageSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "$($Spec.Domain) candidate deployment receipt is not bound to the frozen package."
        }
        $candidateJournal = Read-PrereleaseGcJson -Path $journalPath
        Assert-PrereleaseGcJournalIdentity -Journal $candidateJournal -UniqueId ([string]$Spec.UniqueId) -GameDir $GameDir `
            -Destination $destination -RequireInstalled -ExpectedPackageSha256 ([string]$Spec.PackageSha256)
        if (@($candidateJournal.recoveryArtifacts).Count -ne 0) {
            throw "$($Spec.Domain) candidate deploy unexpectedly adopted or created recovery artifacts."
        }
        $state.CandidateSnapshot = Get-PrereleaseGcCandidatePackageSnapshot -State $state -Path $destination `
            -Context "$($Spec.Domain) installed candidate"
        $state.CandidateJournalIdentity = Get-PrereleaseGcFileIdentity -Path $journalPath
        $state.CandidateInstalled = $true
        $state.Status = 'CandidateInstalled'
        Set-PrereleaseGcLeasePhase -Lease $lease -Phase 'CandidateInstalled'
        return $lease
    }
    catch {
        $applyError = $_
        try {
            if ([bool]$state.DestinationMoved -and $null -eq $state.CandidateSnapshot -and
                (Test-Path -LiteralPath $destination -PathType Container)) {
                $receiptPath = Join-Path $destination '.dtmapi-author-receipt.json'
                if (Test-Path -LiteralPath $receiptPath -PathType Leaf) {
                    $receipt = Read-PrereleaseGcJson -Path $receiptPath
                    if ([string]$receipt.uniqueId -ceq [string]$Spec.UniqueId -and
                        [string]::Equals([string]$receipt.packageSha256, [string]$Spec.PackageSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                        $state.CandidateSnapshot = Get-PrereleaseGcCandidatePackageSnapshot -State $state -Path $destination `
                            -Context "$($Spec.Domain) failed-apply candidate"
                    }
                }
            }
            $null = Complete-PrereleaseGcProductLease -Lease $lease -GameDir $GameDir -SdkExe $SdkExe -ReceiptRoot $ReceiptRoot
        }
        catch {
            throw [InvalidOperationException]::new(
                "$($Spec.Domain) candidate apply failed and exact original restore also failed. Apply: $($applyError.Exception.Message) Restore: $($_.Exception.Message)",
                $_.Exception)
        }
        throw $applyError
    }
}

function Complete-PrereleaseGcProductLease {
    param(
        [Parameter(Mandatory = $true)] [object] $Lease,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $SdkExe,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot
    )

    $openedLease = Open-PrereleaseGcProductLeaseState -StatePath ([string]$Lease.StatePath)
    $state = $openedLease.State
    $Lease.State = $state
    $Lease.TestMode = [bool]$state.TestMode
    if (-not (Test-PrereleaseGcPathEquals -Left $GameDir -Right ([string]$state.GameDir)) -or
        -not (Test-PrereleaseGcPathEquals -Left $ReceiptRoot -Right ([string]$state.ReceiptRoot)) -or
        (-not [bool]$state.TestMode -and
         -not (Test-PrereleaseGcPathEquals -Left $SdkExe -Right ([string]$state.SdkExe)))) {
        throw "$($state.Domain) restore arguments do not match the durable lease authority."
    }
    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation "prerelease active-GC $($state.Domain) restore"
    $destination = [string]$state.DestinationPath
    $journalPath = [string]$state.JournalPath

    $originalJournalBackupExists = Test-Path -LiteralPath ([string]$state.OriginalJournalBackup) -PathType Leaf
    if ($originalJournalBackupExists -and (Test-Path -LiteralPath $journalPath -PathType Leaf) -and
        -not (Test-PrereleaseGcFileIdentity -Expected $state.OriginalJournalIdentity -Path $journalPath)) {
        $liveJournal = Read-PrereleaseGcJson -Path $journalPath
        Assert-PrereleaseGcJournalIdentity -Journal $liveJournal -UniqueId ([string]$state.UniqueId) -GameDir $GameDir -Destination $destination
        $hasPreparedOwner = $null -ne $liveJournal.active -or
            ($liveJournal.PSObject.Properties.Name -contains 'localInstall' -and $null -ne $liveJournal.localInstall)
        if ($hasPreparedOwner) {
            if ([bool]$Lease.TestMode) {
                throw "$($state.Domain) test fixture left a prepared SDK owner; automatic recovery is unavailable in test mode."
            }
            $null = Invoke-PrereleaseGcSdkJson -SdkExe $SdkExe -Arguments @('recover',[string]$state.UniqueId,'--game-root',$GameDir) `
                -ReceiptPath (Join-Path $ReceiptRoot (([string]$state.Domain) + '-candidate-recover.json'))
        }
    }

    $operationLock = Open-Batch5GcAuthorOperationLock -GameDir $GameDir
    try {
        $originalDestinationBackupExists = Test-Path -LiteralPath ([string]$state.OriginalDestinationBackup) -PathType Container
        $destinationExists = Test-Path -LiteralPath $destination -PathType Container
        $destinationIsOriginal = $false
        if ($destinationExists) {
            $currentSnapshotWithAds = Get-Candidate11TreeSnapshot -Path $destination `
                -Context "$($state.Domain) live destination before restore" -AllowAlternateDataStreams
            $destinationIsOriginal = Test-Candidate11SnapshotsEqual -Expected $state.OriginalSnapshot -Actual $currentSnapshotWithAds
            if (-not $destinationIsOriginal) {
                $currentSnapshot = Get-Candidate11TreeSnapshot -Path $destination -Context "$($state.Domain) candidate before restore"
                if ($null -ne $state.CandidateSnapshot) {
                    if (-not (Test-Candidate11SnapshotsEqual -Expected $state.CandidateSnapshot -Actual $currentSnapshot)) {
                        throw "$($state.Domain) destination no longer matches the exact run-owned candidate; restore refused."
                    }
                }
                else {
                    $state.CandidateSnapshot = Get-PrereleaseGcCandidatePackageSnapshot -State $state -Path $destination `
                        -Context "$($state.Domain) interrupted candidate publication"
                }
                if (Test-Path -LiteralPath ([string]$state.CandidateDestinationBackup)) {
                    throw "$($state.Domain) candidate destination and candidate backup both exist; restore refused."
                }
                Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateDestinationPreservePending' `
                    -PendingOperation 'MoveCandidateDestinationToBackup'
                Move-Candidate11Directory -Source $destination -Destination ([string]$state.CandidateDestinationBackup) `
                    -Context "$($state.Domain) preserve candidate before original restore"
                Invoke-PrereleaseGcProcessExitForTests -Lease $Lease -Point 'after-candidate-destination-preserve'
                Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateDestinationPreserved'
                $destinationExists = $false
            }
        }

        $journalExists = Test-Path -LiteralPath $journalPath -PathType Leaf
        $journalIsOriginal = $journalExists -and
            (Test-PrereleaseGcFileIdentity -Expected $state.OriginalJournalIdentity -Path $journalPath)
        if ($journalExists -and -not $journalIsOriginal) {
            $runJournal = Read-PrereleaseGcJson -Path $journalPath
            Assert-PrereleaseGcJournalIdentity -Journal $runJournal -UniqueId ([string]$state.UniqueId) -GameDir $GameDir `
                -Destination $destination -RequireInstalled -ExpectedPackageSha256 ([string]$state.PackageSha256)
            if (@($runJournal.recoveryArtifacts).Count -ne 0) {
                throw "$($state.Domain) interrupted candidate journal contains recovery artifacts; restore refused."
            }
            $state.CandidateJournalIdentity = Get-PrereleaseGcFileIdentity -Path $journalPath
            if (Test-Path -LiteralPath ([string]$state.CandidateJournalBackup)) {
                throw "$($state.Domain) candidate journal and candidate journal backup both exist; restore refused."
            }
            Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateJournalPreservePending' `
                -PendingOperation 'MoveCandidateJournalToBackup'
            Move-Item -LiteralPath $journalPath -Destination ([string]$state.CandidateJournalBackup)
            Invoke-PrereleaseGcProcessExitForTests -Lease $Lease -Point 'after-candidate-journal-preserve'
            Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateJournalPreserved'
            $journalExists = $false
        }

        if ($destinationIsOriginal -and $originalDestinationBackupExists) {
            throw "$($state.Domain) original destination exists in both live and backup paths; restore refused."
        }
        if (-not $destinationIsOriginal -and -not $destinationExists) {
            if (-not $originalDestinationBackupExists) { throw "$($state.Domain) exact original destination backup is missing." }
            $backupSnapshot = Get-Candidate11TreeSnapshot -Path ([string]$state.OriginalDestinationBackup) `
                -Context "$($state.Domain) original destination recovery backup" -AllowAlternateDataStreams
            if (-not (Test-Candidate11SnapshotsEqual -Expected $state.OriginalSnapshot -Actual $backupSnapshot)) {
                throw "$($state.Domain) original destination recovery backup drifted."
            }
            Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'OriginalDestinationRestorePending' `
                -PendingOperation 'MoveOriginalDestinationToLive'
            Move-Candidate11Directory -Source ([string]$state.OriginalDestinationBackup) -Destination $destination `
                -Context "$($state.Domain) restore original destination"
            Invoke-PrereleaseGcProcessExitForTests -Lease $Lease -Point 'after-original-destination-restore'
            Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'OriginalDestinationRestored'
        }

        $originalJournalBackupExists = Test-Path -LiteralPath ([string]$state.OriginalJournalBackup) -PathType Leaf
        $journalExists = Test-Path -LiteralPath $journalPath -PathType Leaf
        $journalIsOriginal = $journalExists -and
            (Test-PrereleaseGcFileIdentity -Expected $state.OriginalJournalIdentity -Path $journalPath)
        if ($journalIsOriginal -and $originalJournalBackupExists) {
            throw "$($state.Domain) original journal exists in both live and backup paths; restore refused."
        }
        if (-not $journalIsOriginal -and -not $journalExists) {
            if (-not $originalJournalBackupExists) { throw "$($state.Domain) exact original journal backup is missing." }
            if (-not (Test-PrereleaseGcFileIdentity -Expected $state.OriginalJournalIdentity -Path ([string]$state.OriginalJournalBackup))) {
                throw "$($state.Domain) original journal recovery backup drifted."
            }
            Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'OriginalJournalRestorePending' `
                -PendingOperation 'MoveOriginalJournalToLive'
            Move-Item -LiteralPath ([string]$state.OriginalJournalBackup) -Destination $journalPath
            Invoke-PrereleaseGcProcessExitForTests -Lease $Lease -Point 'after-original-journal-restore'
            Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'OriginalJournalRestored'
        }
    }
    finally {
        $operationLock.Dispose()
    }

    $restoredSnapshot = Get-Candidate11TreeSnapshot -Path $destination -Context "$($state.Domain) restored original" -AllowAlternateDataStreams
    $destinationRestored = Test-Candidate11SnapshotsEqual -Expected $state.OriginalSnapshot -Actual $restoredSnapshot
    $journalRestored = Test-PrereleaseGcFileIdentity -Expected $state.OriginalJournalIdentity -Path $journalPath
    if (-not $destinationRestored -or -not $journalRestored) {
        throw "$($state.Domain) exact original destination/journal restore verification failed."
    }
    if (-not [bool]$Lease.TestMode) {
        $restoredStatus = Invoke-PrereleaseGcSdkJson -SdkExe $SdkExe -Arguments @('deployment-status',[string]$state.UniqueId,'--game-root',$GameDir) `
            -ReceiptPath (Join-Path $ReceiptRoot (([string]$state.Domain) + '-original-status-after-restore.json'))
        if ([string]$restoredStatus.values.status -cne 'Installed') {
            throw "$($state.Domain) restored Author SDK authority is not Installed."
        }
    }

    if (Test-Path -LiteralPath ([string]$state.CandidateDestinationBackup) -PathType Container) {
        $candidateBackupSnapshot = Get-Candidate11TreeSnapshot -Path ([string]$state.CandidateDestinationBackup) `
            -Context "$($state.Domain) candidate cleanup backup"
        if ($null -eq $state.CandidateSnapshot -or
            -not (Test-Candidate11SnapshotsEqual -Expected $state.CandidateSnapshot -Actual $candidateBackupSnapshot)) {
            throw "$($state.Domain) candidate cleanup backup drifted; deletion refused."
        }
        Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateDestinationCleanupPending' `
            -PendingOperation 'RemoveCandidateDestinationBackup'
        Remove-Item -LiteralPath ([string]$state.CandidateDestinationBackup) -Recurse -Force
        Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateDestinationRemoved'
    }
    if (Test-Path -LiteralPath ([string]$state.CandidateJournalBackup) -PathType Leaf) {
        if ($null -eq $state.CandidateJournalIdentity -or
            -not (Test-PrereleaseGcFileIdentity -Expected $state.CandidateJournalIdentity -Path ([string]$state.CandidateJournalBackup))) {
            throw "$($state.Domain) candidate journal cleanup backup drifted; deletion refused."
        }
        Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateJournalCleanupPending' `
            -PendingOperation 'RemoveCandidateJournalBackup'
        Remove-Item -LiteralPath ([string]$state.CandidateJournalBackup) -Force
        Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'CandidateJournalRemoved'
    }
    foreach ($ownedRoot in @(
        (Split-Path -Parent ([string]$state.CandidateDestinationBackup)),
        (Split-Path -Parent ([string]$state.CandidateJournalBackup)))) {
        if ((Test-Path -LiteralPath $ownedRoot -PathType Container) -and
            @(Get-ChildItem -LiteralPath $ownedRoot -Force).Count -eq 0) {
            Remove-Item -LiteralPath $ownedRoot -Force
        }
    }
    $state.Status = 'Restored'
    $state.RestoredExact = $true
    $state.GeneratedCandidateRemoved = $true
    Set-PrereleaseGcLeasePhase -Lease $Lease -Phase 'Restored'
    return [pscustomobject]@{
        Domain = [string]$state.Domain
        RestoredExact = $true
        GeneratedCandidateRemoved = $true
        OriginalTreeSha256 = [string]$state.OriginalSnapshot.TreeSha256
        OriginalJournalSha256 = [string]$state.OriginalJournalIdentity.Sha256
    }
}

function Open-PrereleaseGcProductLeaseState {
    param([Parameter(Mandatory = $true)] [string] $StatePath)

    $fullStatePath = [System.IO.Path]::GetFullPath($StatePath)
    $state = Read-PrereleaseGcJson -Path $fullStatePath
    if ([int]$state.SchemaVersion -ne 2 -or [string]$state.Domain -notin @('ActionSpeed','AutoFishing') -or
        [string]::IsNullOrWhiteSpace([string]$state.TransactionId) -or
        [string]::IsNullOrWhiteSpace([string]$state.UniqueId)) {
        throw "Prerelease active-GC lease state identity is invalid: $fullStatePath"
    }
    $expectedIdentity = if ([string]$state.Domain -ceq 'ActionSpeed') {
        [pscustomobject]@{ UniqueId='Yuuka.DTMAPI.ActionSpeed'; StateFile='action-speed-lease.json' }
    }
    else {
        [pscustomobject]@{ UniqueId='Yuuka.DTMAPI.AutoFishing'; StateFile='auto-fishing-lease.json' }
    }
    if ([string]$state.UniqueId -cne [string]$expectedIdentity.UniqueId -or
        [System.IO.Path]::GetFileName($fullStatePath) -cne [string]$expectedIdentity.StateFile) {
        throw "Prerelease active-GC lease domain identity is invalid: $fullStatePath"
    }
    $expectedDestination = Join-Path (Join-Path ([string]$state.GameDir) 'Mods') ([string]$state.UniqueId)
    $expectedJournal = Join-Path (
        Join-Path (Split-Path -Parent (Get-Batch5GcAuthorSourceStatePath -GameDir ([string]$state.GameDir))) 'deployments') `
        (([string]$state.UniqueId) + '.journal.json')
    $expectedProductLeaseRoot = Join-Path (Join-Path ([string]$state.GameDir) '.dtmapi-prerelease-active-gc') ([string]$state.TransactionId)
    $expectedJournalLeaseRoot = Join-Path (
        Join-Path (Split-Path -Parent (Split-Path -Parent ([string]$state.JournalPath))) 'prerelease-active-gc') `
        ([string]$state.TransactionId)
    if (-not (Test-PrereleaseGcPathEquals -Left (Split-Path -Parent $fullStatePath) -Right ([string]$state.ReceiptRoot)) -or
        -not (Test-PrereleaseGcPathEquals -Left ([string]$state.DestinationPath) -Right $expectedDestination) -or
        -not (Test-PrereleaseGcPathEquals -Left ([string]$state.JournalPath) -Right $expectedJournal) -or
        -not (Test-PrereleaseGcPathEquals -Left (Split-Path -Parent ([string]$state.OriginalDestinationBackup)) -Right $expectedProductLeaseRoot) -or
        -not (Test-PrereleaseGcPathEquals -Left (Split-Path -Parent ([string]$state.CandidateDestinationBackup)) -Right $expectedProductLeaseRoot) -or
        -not (Test-PrereleaseGcPathEquals -Left (Split-Path -Parent ([string]$state.OriginalJournalBackup)) -Right $expectedJournalLeaseRoot) -or
        -not (Test-PrereleaseGcPathEquals -Left (Split-Path -Parent ([string]$state.CandidateJournalBackup)) -Right $expectedJournalLeaseRoot)) {
        throw "Prerelease active-GC lease paths are outside their exact transaction roots: $fullStatePath"
    }
    if (-not [string]::Equals(
        (Get-PrereleaseGcFileSha256 -Path ([string]$state.PackagePath)),
        [string]$state.PackageSha256,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Prerelease active-GC frozen package changed before recovery: $($state.PackagePath)"
    }
    $actualPackageEntries = @(Get-PrereleaseGcPackageEntries -PackagePath ([string]$state.PackagePath))
    $recordedPackageEntries = @($state.PackageEntries)
    if ($actualPackageEntries.Count -ne $recordedPackageEntries.Count) {
        throw 'Prerelease active-GC frozen-package entry ledger changed before recovery.'
    }
    for ($index = 0; $index -lt $actualPackageEntries.Count; $index++) {
        $actualEntry = $actualPackageEntries[$index]
        $recordedEntry = $recordedPackageEntries[$index]
        if ([string]$actualEntry.Path -cne [string]$recordedEntry.Path -or
            [int64]$actualEntry.Length -ne [int64]$recordedEntry.Length -or
            -not [string]::Equals(
                [string]$actualEntry.Sha256,
                [string]$recordedEntry.Sha256,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Prerelease active-GC frozen-package entry ledger changed before recovery.'
        }
    }
    return [pscustomobject]@{
        StatePath = $fullStatePath
        State = $state
        Spec = $null
        TestMode = [bool]$state.TestMode
    }
}

function Recover-PrereleaseGcProductLease {
    param(
        [Parameter(Mandatory = $true)] [string] $StatePath,
        [string] $GameDir = '',
        [string] $SdkExe = '',
        [string] $ReceiptRoot = ''
    )

    $lease = Open-PrereleaseGcProductLeaseState -StatePath $StatePath
    if ([string]::IsNullOrWhiteSpace($GameDir)) { $GameDir = [string]$lease.State.GameDir }
    if ([string]::IsNullOrWhiteSpace($SdkExe)) { $SdkExe = [string]$lease.State.SdkExe }
    if ([string]::IsNullOrWhiteSpace($ReceiptRoot)) { $ReceiptRoot = [string]$lease.State.ReceiptRoot }
    if (-not (Test-PrereleaseGcPathEquals -Left $GameDir -Right ([string]$lease.State.GameDir))) {
        throw 'Prerelease active-GC recovery GameDir does not match its durable lease state.'
    }
    return Complete-PrereleaseGcProductLease -Lease $lease -GameDir $GameDir -SdkExe $SdkExe -ReceiptRoot $ReceiptRoot
}

function Assert-PrereleaseGcRuntimeCandidateInstalled {
    param(
        [Parameter(Mandatory = $true)] [string] $CandidateRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    $payloadRoot = Join-Path $CandidateRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $installedRoot = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
    $rows = New-Object System.Collections.ArrayList
    foreach ($name in @(
        'DTMAPI.Abstractions.dll','DTMAPI.BepInExBootstrap.dll','DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll','DTMAPI.ModConfigMenu.dll')) {
        $candidate = Join-Path $payloadRoot $name
        $installed = Join-Path $installedRoot $name
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf) -or -not (Test-Path -LiteralPath $installed -PathType Leaf)) {
            throw "Prerelease Runtime candidate binding file is missing: $name"
        }
        $candidateHash = Get-PrereleaseGcFileSha256 -Path $candidate
        $installedHash = Get-PrereleaseGcFileSha256 -Path $installed
        if ($candidateHash -cne $installedHash) { throw "Installed Runtime differs from the frozen candidate: $name" }
        [void]$rows.Add([ordered]@{ Name=$name; Sha256=$candidateHash })
    }
    return @($rows.ToArray())
}

function Start-PrereleaseGcManagedIsolation {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $EnabledUniqueId,
        [Parameter(Mandatory = $true)] [string] $Id,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot
    )

    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation "prerelease active-GC $EnabledUniqueId isolation"
    $modsRoot = Join-Path $GameDir 'Mods'
    $rows = New-Object System.Collections.ArrayList
    $targetFound = $false
    foreach ($directory in @(Get-ChildItem -LiteralPath $modsRoot -Force -Directory -ErrorAction Stop | Sort-Object Name)) {
        $receiptPath = Join-Path $directory.FullName '.dtmapi-author-receipt.json'
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { continue }
        if (($directory.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Managed isolation refuses a reparse-point product directory: $($directory.FullName)"
        }
        $receipt = Read-PrereleaseGcJson -Path $receiptPath
        if ([int]$receipt.schemaVersion -ne 2 -or [string]$receipt.packageKind -cne 'CodeMod' -or
            [string]$receipt.codeModKind -cne 'Advanced' -or [string]$receipt.uniqueId -cne [string]$directory.Name -or
            [string]$receipt.destinationRelativePath -cne ('Mods/' + [string]$directory.Name)) {
            throw "Managed isolation receipt identity drifted: $receiptPath"
        }
        $markerPath = Join-Path $directory.FullName 'dtmapi.disabled'
        if ([string]$directory.Name -ceq $EnabledUniqueId) {
            $targetFound = $true
            if (Test-Path -LiteralPath $markerPath) {
                throw "Focused target is cold-disabled before the run: $EnabledUniqueId"
            }
            [void]$rows.Add([ordered]@{
                UniqueId=[string]$directory.Name
                MarkerPath=$markerPath
                Role='enabled-target'
                Created=$false
                CreatePlanned=$false
                Identity=$null
            })
            continue
        }
        if (Test-Path -LiteralPath $markerPath) {
            if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
                throw "Managed isolation refuses a non-file marker: $markerPath"
            }
            [void]$rows.Add([ordered]@{
                UniqueId=[string]$directory.Name
                MarkerPath=$markerPath
                Role='pre-existing-disabled'
                Created=$false
                CreatePlanned=$false
                Identity=(Get-PrereleaseGcFileIdentity -Path $markerPath)
            })
            continue
        }
        [void]$rows.Add([ordered]@{
            UniqueId=[string]$directory.Name
            MarkerPath=$markerPath
            Role='run-disabled'
            Created=$false
            CreatePlanned=$true
            Identity=$null
        })
    }
    if (-not $targetFound) { throw "Focused target does not have a receipt-bound managed deployment: $EnabledUniqueId" }
    $path = Join-Path $ReceiptRoot ('managed-isolation-' + $EnabledUniqueId + '.json')
    $summary = [pscustomobject][ordered]@{
        SchemaVersion=1
        TransactionId=$Id
        EnabledUniqueId=$EnabledUniqueId
        Status='Prepared'
        Products=@($rows.ToArray())
        CreatedCount=0
        PreExistingCount=@($rows | Where-Object { [string]$_.Role -ceq 'pre-existing-disabled' }).Count
        RestoredExact=$false
    }
    Write-PrereleaseGcAtomicJson -Path $path -Value $summary
    $createdRows = New-Object System.Collections.ArrayList
    try {
        foreach ($row in @($rows | Where-Object { [bool]$_.CreatePlanned })) {
            $bytes = $script:PrereleaseGcUtf8.GetBytes("DTMAPI prerelease active-GC isolation $Id $EnabledUniqueId`n")
            $stream = [System.IO.File]::Open(
                [string]$row.MarkerPath,
                [System.IO.FileMode]::CreateNew,
                [System.IO.FileAccess]::Write,
                [System.IO.FileShare]::None)
            $row.Created = $true
            [void]$createdRows.Add($row)
            try {
                $stream.Write($bytes, 0, $bytes.Length)
                $stream.Flush()
            }
            finally { $stream.Dispose() }
            $row.Identity = Get-PrereleaseGcFileIdentity -Path ([string]$row.MarkerPath)
            $summary.CreatedCount = @($rows | Where-Object { [bool]$_.Created }).Count
            Write-PrereleaseGcAtomicJson -Path $path -Value $summary
        }
        $summary.Status = 'Applied'
        Write-PrereleaseGcAtomicJson -Path $path -Value $summary
    }
    catch {
        $createError = $_
        $cleanupFailures = New-Object System.Collections.ArrayList
        foreach ($created in @($createdRows)) {
            try {
                if ($null -ne $created.Identity -and
                    -not (Test-PrereleaseGcFileIdentity -Expected $created.Identity -Path ([string]$created.MarkerPath))) {
                    throw "created marker ownership was lost: $($created.MarkerPath)"
                }
                if (Test-Path -LiteralPath ([string]$created.MarkerPath) -PathType Leaf) {
                    Remove-Item -LiteralPath ([string]$created.MarkerPath) -Force
                }
            }
            catch { [void]$cleanupFailures.Add($_.Exception.Message) }
        }
        if ($cleanupFailures.Count -gt 0) {
            $summary.Status = 'ApplyRollbackFailed'
            Write-PrereleaseGcAtomicJson -Path $path -Value $summary
            throw [InvalidOperationException]::new(
                $createError.Exception.Message + ' Isolation apply rollback also failed: ' +
                    [string]::Join(' | ', @($cleanupFailures)),
                $createError.Exception)
        }
        $summary.Status = 'RestoredAfterApplyFailure'
        $summary.RestoredExact = $true
        Write-PrereleaseGcAtomicJson -Path $path -Value $summary
        throw $createError
    }
    return [pscustomobject]@{ Path=$path; Summary=$summary }
}

function Complete-PrereleaseGcManagedIsolation {
    param(
        [Parameter(Mandatory = $true)] [object] $Lease,
        [Parameter(Mandatory = $true)] [string] $GameDir
    )

    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation "prerelease active-GC $($Lease.Summary.EnabledUniqueId) isolation restore"
    foreach ($row in @($Lease.Summary.Products)) {
        if ([bool]$row.Created) {
            if (-not (Test-PrereleaseGcFileIdentity -Expected $row.Identity -Path ([string]$row.MarkerPath))) {
                throw "Run-created managed isolation marker changed or disappeared: $($row.MarkerPath)"
            }
        }
        elseif ([string]$row.Role -ceq 'pre-existing-disabled' -and
            -not (Test-PrereleaseGcFileIdentity -Expected $row.Identity -Path ([string]$row.MarkerPath))) {
            throw "Pre-existing managed disabled marker changed during the focused run: $($row.MarkerPath)"
        }
        elseif ([string]$row.Role -ceq 'enabled-target' -and (Test-Path -LiteralPath ([string]$row.MarkerPath))) {
            throw "Focused target gained an unexpected disabled marker: $($row.MarkerPath)"
        }
    }
    foreach ($row in @($Lease.Summary.Products | Where-Object { [bool]$_.Created })) {
        Remove-Item -LiteralPath ([string]$row.MarkerPath) -Force
    }
    foreach ($row in @($Lease.Summary.Products)) {
        if ([bool]$row.Created -and (Test-Path -LiteralPath ([string]$row.MarkerPath))) {
            throw "Run-created managed isolation marker remained after cleanup: $($row.MarkerPath)"
        }
        if ([string]$row.Role -ceq 'pre-existing-disabled' -and
            -not (Test-PrereleaseGcFileIdentity -Expected $row.Identity -Path ([string]$row.MarkerPath))) {
            throw "Pre-existing managed disabled marker was not preserved exactly: $($row.MarkerPath)"
        }
    }
    $Lease.Summary.Status = 'Restored'
    $Lease.Summary.RestoredExact = $true
    Write-PrereleaseGcAtomicJson -Path ([string]$Lease.Path) -Value $Lease.Summary
}

function Invoke-PrereleaseGcManagedIsolation {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $EnabledUniqueId,
        [Parameter(Mandatory = $true)] [string] $Id,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] [scriptblock] $Action
    )

    $lease = Start-PrereleaseGcManagedIsolation -GameDir $GameDir -EnabledUniqueId $EnabledUniqueId -Id $Id -ReceiptRoot $ReceiptRoot
    $actionError = $null
    try { & $Action }
    catch { $actionError = $_ }
    try { Complete-PrereleaseGcManagedIsolation -Lease $lease -GameDir $GameDir }
    catch {
        if ($null -ne $actionError) {
            throw [InvalidOperationException]::new(
                $actionError.Exception.Message + ' Managed isolation restore also failed: ' + $_.Exception.Message,
                $_.Exception)
        }
        throw
    }
    if ($null -ne $actionError) { throw $actionError }
}

if ($LibraryOnly) { return }

$repo = Get-RepoRoot
if ($RecoverOnly) {
    if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
        throw '-RecoverOnly requires the exact interrupted run -EvidenceRoot.'
    }
    if (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue) {
        throw 'Prerelease active-GC recovery requires DolocTown.exe to be absent.'
    }
    $managedEvidenceRoot = [System.IO.Path]::GetFullPath(
        (Join-Path $repo 'docs\debug\evidence\PRERELEASE-ACTIVE-GC')).TrimEnd([char]92, [char]47)
    $EvidenceRoot = [System.IO.Path]::GetFullPath($EvidenceRoot).TrimEnd([char]92, [char]47)
    if (-not $EvidenceRoot.StartsWith(
            $managedEvidenceRoot + [System.IO.Path]::DirectorySeparatorChar,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Prerelease active-GC recovery EvidenceRoot is outside the managed evidence root: $EvidenceRoot"
    }
    $summaryPath = Join-Path $EvidenceRoot 'summary.json'
    $summary = Read-PrereleaseGcJson -Path $summaryPath
    if ([int]$summary.SchemaVersion -ne 1 -or
        [string]::IsNullOrWhiteSpace([string]$summary.TransactionId)) {
        throw "Prerelease active-GC recovery summary identity is invalid: $summaryPath"
    }
    if (-not [string]::IsNullOrWhiteSpace($TransactionId) -and
        [string]$summary.TransactionId -cne $TransactionId) {
        throw 'Prerelease active-GC recovery TransactionId does not match its summary.'
    }
    $TransactionId = [string]$summary.TransactionId
    $statePaths = @(
        Get-ChildItem -LiteralPath $EvidenceRoot -Force -File -ErrorAction Stop |
            Where-Object { $_.Name -in @('action-speed-lease.json','auto-fishing-lease.json') } |
            Sort-Object Name |
            ForEach-Object { $_.FullName }
    )
    if ($statePaths.Count -lt 1 -or $statePaths.Count -gt 2) {
        throw "Prerelease active-GC recovery requires one or two exact product lease states; found $($statePaths.Count)."
    }
    $leasesToRecover = @($statePaths | ForEach-Object { Open-PrereleaseGcProductLeaseState -StatePath $_ })
    $domains = @($leasesToRecover | ForEach-Object { [string]$_.State.Domain })
    if (@($domains | Select-Object -Unique).Count -ne $domains.Count -or
        @($leasesToRecover | Where-Object {
            [string]$_.State.TransactionId -cne $TransactionId -or
            -not (Test-PrereleaseGcPathEquals -Left ([string]$_.State.ReceiptRoot) -Right $EvidenceRoot)
        }).Count -gt 0) {
        throw 'Prerelease active-GC recovery lease identities do not match the interrupted run.'
    }
    $recordedGameDirs = @($leasesToRecover | ForEach-Object {
        [System.IO.Path]::GetFullPath([string]$_.State.GameDir).TrimEnd([char]92, [char]47)
    } | Select-Object -Unique)
    if ($recordedGameDirs.Count -ne 1) {
        throw 'Prerelease active-GC recovery lease states target different game directories.'
    }
    $recordedGameDir = [string]$recordedGameDirs[0]
    $configuredGameDir = if ([string]::IsNullOrWhiteSpace($GameDir)) {
        [System.IO.Path]::GetFullPath((Resolve-DolocTownGamePath -RepoRoot $repo)).TrimEnd([char]92, [char]47)
    }
    else {
        [System.IO.Path]::GetFullPath($GameDir).TrimEnd([char]92, [char]47)
    }
    if (-not (Test-PrereleaseGcPathEquals -Left $configuredGameDir -Right $recordedGameDir)) {
        throw "Prerelease active-GC recovery lease targets a different configured game directory: $recordedGameDir"
    }
    Assert-DtmApiDolocTownGamePath -Path $configuredGameDir -Source '-GameDir'
    Assert-DtmApiGameNotRunning -GameDir $configuredGameDir -Operation 'prerelease focused active-GC interrupted recovery'
    $lockInfo = Get-DtmApiRuntimeLockInfo -RepoRoot $repo
    if (-not [bool]$lockInfo.Exists -or
        -not (Test-DtmApiRuntimeLockOwnedByCurrentWorktree -LockInfo $lockInfo -RepoRoot $repo)) {
        throw 'Prerelease active-GC recovery requires the interrupted Runtime lock to belong to this worktree.'
    }
    $recordedProcessId = [int](Get-DtmApiObjectProperty -Object $lockInfo.Data -Name 'ProcessId' -Default 0)
    if ($recordedProcessId -gt 0 -and (Get-Process -Id $recordedProcessId -ErrorAction SilentlyContinue)) {
        throw "Prerelease active-GC recovery refuses a live lock owner process: $recordedProcessId"
    }

    $recoveryErrors = New-Object System.Collections.ArrayList
    $summary.Status = 'RecoveringInterruptedRun'
    $summary.RuntimeLockRelease = 'HeldForInterruptedRecovery'
    $summary.Restores = @()
    foreach ($lease in @($leasesToRecover | Sort-Object { [string]$_.State.Domain } -Descending)) {
        try {
            $restore = Complete-PrereleaseGcProductLease -Lease $lease -GameDir $configuredGameDir `
                -SdkExe ([string]$lease.State.SdkExe) -ReceiptRoot $EvidenceRoot
            $summary.Restores = @($summary.Restores) + @($restore)
        }
        catch { [void]$recoveryErrors.Add($_) }
    }
    if ($null -ne $summary.SourceStateBefore) {
        try {
            $authorStatePath = Get-Batch5GcAuthorSourceStatePath -GameDir $configuredGameDir
            $summary.SourceStateAfter = Get-PrereleaseGcFileIdentity -Path $authorStatePath
            $summary.SourceStateRestoredExact =
                [bool]$summary.SourceStateBefore.Exists -eq [bool]$summary.SourceStateAfter.Exists -and
                [int64]$summary.SourceStateBefore.Length -eq [int64]$summary.SourceStateAfter.Length -and
                [string]::Equals(
                    [string]$summary.SourceStateBefore.Sha256,
                    [string]$summary.SourceStateAfter.Sha256,
                    [System.StringComparison]::OrdinalIgnoreCase)
            if (-not $summary.SourceStateRestoredExact) {
                throw 'Author SDK source-state did not return to its exact pre-run identity.'
            }
        }
        catch { [void]$recoveryErrors.Add($_) }
    }
    if ($recoveryErrors.Count -eq 0) {
        & "$PSScriptRoot\release-runtime-lock.ps1"
        if (-not $?) { [void]$recoveryErrors.Add('Shared Runtime lock release failed after exact interrupted recovery.') }
    }
    if ($recoveryErrors.Count -eq 0) {
        $summary.Status = 'RecoveredAfterInterruptedRun'
        $summary.RuntimeLockRelease = 'ReleasedAfterExactInterruptedRecovery'
        $summary.Failure = ''
    }
    else {
        $summary.Status = 'RecoveryFailed'
        $summary.RuntimeLockRelease = 'RetainedForManualRecovery'
        $summary.Failure = [string]::Join(' | ', @($recoveryErrors | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { [string]$_ }
        }))
    }
    Write-PrereleaseGcAtomicJson -Path $summaryPath -Value $summary
    if ($recoveryErrors.Count -gt 0) { throw $summary.Failure }
    Write-Host "Prerelease active-GC interrupted recovery passed: $EvidenceRoot"
    return
}

if ($SampleSeconds -gt $MeasureSeconds) { throw '-SampleSeconds cannot exceed -MeasureSeconds.' }
if ($null -eq $DeployActionForTests -and ($AutoFishingWarmupFish -lt 5 -or $AutoFishingTargetFish -lt 10)) {
    throw 'The prerelease AutoFishing runtime focus requires at least 5 warm-up fish and 10 measured fish.'
}
if ([string]::IsNullOrWhiteSpace($TransactionId)) {
    $TransactionId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
}
if ([string]::IsNullOrWhiteSpace($RuntimeCandidateRoot)) {
    $RuntimeCandidateRoot = Join-Path $repo 'dist\prerelease-step5-candidate\DTMAPI'
}
if ([string]::IsNullOrWhiteSpace($ActionSpeedOutputRoot)) {
    $ActionSpeedOutputRoot = Join-Path $repo 'temp\prerelease-step5-product-action-speed'
}
if ([string]::IsNullOrWhiteSpace($AutoFishingOutputRoot)) {
    $AutoFishingOutputRoot = Join-Path $repo 'temp\prerelease-step5-product-auto-fishing'
}
if ([string]::IsNullOrWhiteSpace($AuthorSdkOutputRoot)) {
    $AuthorSdkOutputRoot = Join-Path $repo 'temp\prerelease-step5-author-sdk'
}
if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path (Join-Path $repo 'docs\debug\evidence\PRERELEASE-ACTIVE-GC') $TransactionId
}
$EvidenceRoot = [System.IO.Path]::GetFullPath($EvidenceRoot)
[System.IO.Directory]::CreateDirectory($EvidenceRoot) | Out-Null
$summaryPath = Join-Path $EvidenceRoot 'summary.json'
$summary = [ordered]@{
    SchemaVersion = 1
    TransactionId = $TransactionId
    Status = 'Starting'
    Authority = 'DTMAPI-0.5.5-focused-current-candidate'
    FullReleaseRun = $false
    FullHistoricalLadderRun = $false
    ForcedGc = $false
    ActionSpeedStages = @($script:PrereleaseGcActionSpeedStages)
    AutoFishingLevels = @($script:PrereleaseGcAutoFishingLevels)
    MeasureSeconds = $MeasureSeconds
    SampleSeconds = $SampleSeconds
    RuntimeBinding = @()
    ProductLeases = @()
    ActionSpeedExitCode = $null
    AutoFishingExitCode = $null
    SourceStateBefore = $null
    SourceStateAfter = $null
    SourceStateRestoredExact = $false
    Restores = @()
    RuntimeLockRelease = 'NotAcquired'
    Failure = ''
}
Write-PrereleaseGcAtomicJson -Path $summaryPath -Value $summary

$lockAcquired = $false
$releaseAllowed = $false
$sdkExe = ''
$leases = New-Object 'System.Collections.Generic.List[object]'
$primaryError = $null
$restoreErrors = New-Object System.Collections.ArrayList
try {
    & "$PSScriptRoot\wait-runtime-lock.ps1" -Reason '0.5.5 focused current-candidate ActionSpeed/AutoFishing active GC'
    if (-not $?) { throw 'Could not acquire the shared Runtime lock.' }
    $lockAcquired = $true
    $summary.RuntimeLockRelease = 'Held'
    if ([string]::IsNullOrWhiteSpace($GameDir)) { $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo }
    $GameDir = [System.IO.Path]::GetFullPath($GameDir)
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source '-GameDir'
    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'prerelease focused active-GC'

    $sdkExe = Join-Path (Join-Path $AuthorSdkOutputRoot 'DTMAPI-Author-SDK-0.1.0-win-x64') 'dtmapi-author.exe'
    if (-not (Test-Path -LiteralPath $sdkExe -PathType Leaf)) { throw "Frozen Author SDK executable is missing: $sdkExe" }
    $summary.RuntimeBinding = Assert-PrereleaseGcRuntimeCandidateInstalled -CandidateRoot $RuntimeCandidateRoot -GameDir $GameDir
    $authorStatePath = Get-Batch5GcAuthorSourceStatePath -GameDir $GameDir
    $authorStateRoot = Split-Path -Parent $authorStatePath
    $summary.SourceStateBefore = Get-PrereleaseGcFileIdentity -Path $authorStatePath

    $actionSpec = Get-PrereleaseGcProductSpec -Domain ActionSpeed -OutputRoot $ActionSpeedOutputRoot -GameDir $GameDir -AuthorStateRoot $authorStateRoot
    $fishingSpec = Get-PrereleaseGcProductSpec -Domain AutoFishing -OutputRoot $AutoFishingOutputRoot -GameDir $GameDir -AuthorStateRoot $authorStateRoot
    foreach ($spec in @($actionSpec,$fishingSpec)) {
        $lease = Start-PrereleaseGcProductLease -Spec $spec -GameDir $GameDir -SdkExe $sdkExe -Id $TransactionId `
            -ReceiptRoot $EvidenceRoot -TestDeployAction $DeployActionForTests
        $leases.Add($lease) | Out-Null
        $summary.ProductLeases = @($leases | ForEach-Object { [string]$_.StatePath })
        Write-PrereleaseGcAtomicJson -Path $summaryPath -Value $summary
        if ([string]$InjectFailureAfterCandidateForTests -ceq [string]$spec.Domain) {
            throw "Injected prerelease active-GC failure after $($spec.Domain) candidate."
        }
    }
    if ($null -ne $DeployActionForTests) {
        $summary.Status = 'FixtureCandidatesInstalled'
    }
    else {
        $actionSelection = New-Batch5GcLocalSourceSelection -Domain ActionSpeed -PersistentRoot (Get-Batch5GcPersistentRoot) `
            -SourcePath ([string]$actionSpec.DestinationPath)
        $actionOutput = Join-Path $EvidenceRoot 'action-speed'
        $actionArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'run-batch5-gc-ladder.ps1'),
            '-Domain','ActionSpeed','-MeasureSeconds',[string]$MeasureSeconds,'-SampleSeconds',[string]$SampleSeconds,
            '-ActionTargetUnits',[string]$ActionTargetUnits,'-TimeoutSeconds',[string]$TimeoutSeconds,
            '-OutputRoot',$actionOutput,'-SkipBuild','-SkipInstall','-RuntimeLockAlreadyHeld',
            '-LocalSourcePath',[string]$actionSpec.DestinationPath,
            '-ExpectedActionSpeedTreeSha256',[string]$actionSelection.ExpectedTreeSha256,
            '-StageIdCsv',([string]::Join(',', $script:PrereleaseGcActionSpeedStages)))
        if ($UseSteam) { $actionArgs += '-UseSteam' }
        Invoke-PrereleaseGcManagedIsolation -GameDir $GameDir -EnabledUniqueId ([string]$actionSpec.UniqueId) `
            -Id $TransactionId -ReceiptRoot $EvidenceRoot -Action {
                $actionLog = @(& powershell.exe @actionArgs 2>&1)
                $summary.ActionSpeedExitCode = $LASTEXITCODE
                $actionLog | Set-Content -LiteralPath (Join-Path $EvidenceRoot 'action-speed-output.txt') -Encoding UTF8
                if ($summary.ActionSpeedExitCode -ne 0) { throw "Focused ActionSpeed GC runner failed with exit $($summary.ActionSpeedExitCode)." }
            }

        $fishingOutput = Join-Path $EvidenceRoot 'auto-fishing'
        $fishingArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'run-batch6-autofishing-gc-ladder.ps1'),
            '-MeasureSeconds',[string]$MeasureSeconds,'-SampleSeconds',[string]$SampleSeconds,
            '-WarmupFish',[string]$AutoFishingWarmupFish,'-TargetFish',[string]$AutoFishingTargetFish,
            '-TimeoutSeconds',[string]$TimeoutSeconds,'-GameDir',$GameDir,'-OutputRoot',$fishingOutput,
            '-PilotOutputRoot',$AutoFishingOutputRoot,'-AuthorSdkOutputRoot',$AuthorSdkOutputRoot,
            '-SkipBuild','-SkipPilotBuild','-AllowPreinstalledExactCandidate','-RuntimeLockAlreadyHeld',
            '-FocusedLevelCsv',([string]::Join(',', $script:PrereleaseGcAutoFishingLevels)))
        if ($UseSteam) { $fishingArgs += '-UseSteam' }
        Invoke-PrereleaseGcManagedIsolation -GameDir $GameDir -EnabledUniqueId ([string]$fishingSpec.UniqueId) `
            -Id $TransactionId -ReceiptRoot $EvidenceRoot -Action {
                $fishingLog = @(& powershell.exe @fishingArgs 2>&1)
                $summary.AutoFishingExitCode = $LASTEXITCODE
                $fishingLog | Set-Content -LiteralPath (Join-Path $EvidenceRoot 'auto-fishing-output.txt') -Encoding UTF8
                if ($summary.AutoFishingExitCode -ne 0) { throw "Focused AutoFishing GC runner failed with exit $($summary.AutoFishingExitCode)." }
            }
        $summary.Status = 'FocusedRunnersPassed'
    }
}
catch {
    $primaryError = $_
    $summary.Status = 'FailedPendingRestore'
    $summary.Failure = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
}
finally {
    try {
        if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
            Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'prerelease focused active-GC final restore'
        }
        if ($null -ne $summary.SourceStateBefore -and -not [string]::IsNullOrWhiteSpace($GameDir)) {
            try {
                $authorStatePath = Get-Batch5GcAuthorSourceStatePath -GameDir $GameDir
                $summary.SourceStateAfter = Get-PrereleaseGcFileIdentity -Path $authorStatePath
                $summary.SourceStateRestoredExact = [bool]$summary.SourceStateBefore.Exists -eq [bool]$summary.SourceStateAfter.Exists -and
                    [int64]$summary.SourceStateBefore.Length -eq [int64]$summary.SourceStateAfter.Length -and
                    [string]::Equals([string]$summary.SourceStateBefore.Sha256, [string]$summary.SourceStateAfter.Sha256, [System.StringComparison]::OrdinalIgnoreCase)
                if (-not $summary.SourceStateRestoredExact) {
                    throw 'Author SDK source-state did not return to its exact pre-run identity.'
                }
            }
            catch { [void]$restoreErrors.Add($_) }
        }
        foreach ($lease in @($leases.ToArray()) | Sort-Object { [string]$_.State.Domain } -Descending) {
            try {
                $restore = Complete-PrereleaseGcProductLease -Lease $lease -GameDir $GameDir -SdkExe $sdkExe -ReceiptRoot $EvidenceRoot
                $summary.Restores = @($summary.Restores) + @($restore)
            }
            catch {
                [void]$restoreErrors.Add($_)
            }
        }
        if ($restoreErrors.Count -gt 0) {
            throw "One or more exact product restores failed: $([string]::Join(' | ', @($restoreErrors | ForEach-Object { $_.Exception.Message })))."
        }
        Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'prerelease focused active-GC lock release'
        $releaseAllowed = $true
    }
    catch {
        if ($null -eq $primaryError) { $primaryError = $_ }
        else {
            $primaryError = [InvalidOperationException]::new(
                $primaryError.Exception.Message + ' Final restore also failed: ' + $_.Exception.Message,
                $_.Exception)
        }
    }
    if ($lockAcquired -and $releaseAllowed) {
        & "$PSScriptRoot\release-runtime-lock.ps1"
        $summary.RuntimeLockRelease = 'ReleasedAfterExactRestore'
    }
    elseif ($lockAcquired) {
        $summary.RuntimeLockRelease = 'RetainedForManualRecovery'
    }
    $summary.Status = if ($null -eq $primaryError -and $releaseAllowed) { 'Passed' } else { 'Failed' }
    Write-PrereleaseGcAtomicJson -Path $summaryPath -Value $summary
}

if ($null -ne $primaryError) { throw $primaryError }
Write-Host "Prerelease focused active-GC candidate gate passed: $EvidenceRoot"
