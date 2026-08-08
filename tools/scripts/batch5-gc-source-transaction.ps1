$ErrorActionPreference = 'Stop'

function Get-Batch5GcCanonicalPath {
    param([Parameter(Mandatory = $true)] [string] $Path)

    return [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
}

function Get-Batch5GcPersistentRoot {
    if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_DOLOC_PERSISTENT_ROOT)) {
        return Get-Batch5GcCanonicalPath -Path $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    }
    return Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) 'AppData\LocalLow\RedSawGames\DolocTown'
}

function Get-Batch5GcAuthorSourceStatePath {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $canonicalGameRoot = Get-Batch5GcCanonicalPath -Path $GameDir
    $stateBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_AUTHOR_STATE_ROOT)) {
        Get-Batch5GcCanonicalPath -Path $env:DTMAPI_AUTHOR_STATE_ROOT
    }
    else {
        Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'DTMAPI\AuthorSdk\state'
    }
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $gameRootHash = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($canonicalGameRoot.ToUpperInvariant()))
    }
    finally {
        $hasher.Dispose()
    }
    $installationKey = -join @($gameRootHash[0..15] | ForEach-Object { $_.ToString('x2') })
    return Join-Path (Join-Path (Join-Path $stateBase 'installations') $installationKey) 'source-state.json'
}

function Open-Batch5GcAuthorOperationLock {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $statePath = Get-Batch5GcAuthorSourceStatePath -GameDir $GameDir
    $lockPath = Join-Path (Split-Path -Parent $statePath) 'operation.lock'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $lockPath) | Out-Null
    try {
        return [System.IO.File]::Open(
            $lockPath,
            [System.IO.FileMode]::OpenOrCreate,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
    }
    catch [System.IO.IOException] {
        throw [InvalidOperationException]::new("Another Author SDK operation owns the installation-scoped source-state lock: $lockPath", $_.Exception)
    }
}

function Assert-Batch5GcAuthorOperationLock {
    param(
        [Parameter(Mandatory = $true)] [string] $StatePath,
        [Parameter(Mandatory = $true)] $OperationLock
    )

    $expectedPath = Get-Batch5GcCanonicalPath -Path (Join-Path (Split-Path -Parent $StatePath) 'operation.lock')
    $actualPath = if ($null -ne $OperationLock) { Get-Batch5GcCanonicalPath -Path ([string]$OperationLock.Name) } else { '' }
    if ($null -eq $OperationLock -or -not [bool]$OperationLock.CanRead -or -not [bool]$OperationLock.CanWrite -or
        -not [string]::Equals($actualPath, $expectedPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Batch 5 GC source-state mutation requires the matching installation-scoped Author SDK operation.lock lease.'
    }
}

function Get-Batch5GcFileTreeDigest {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $root = Get-Batch5GcCanonicalPath -Path $Path
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Batch 5 GC local source tree does not exist: $root"
    }
    $entries = @(Get-ChildItem -LiteralPath $root -Recurse -Force -ErrorAction Stop)
    $rootItem = Get-Item -LiteralPath $root -Force
    if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        @($entries | Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 }).Count -gt 0) {
        throw "Batch 5 GC local source trees may not contain reparse points: $root"
    }

    $files = New-Object 'System.Collections.Generic.SortedDictionary[string,string]' ([System.StringComparer]::Ordinal)
    foreach ($file in @($entries | Where-Object { -not $_.PSIsContainer })) {
        $relative = $file.FullName.Substring($root.Length + 1).Replace([char]92, [char]47)
        $files.Add($relative, $file.FullName)
    }

    $aggregate = New-Object System.IO.MemoryStream
    try {
        foreach ($entry in $files.GetEnumerator()) {
            $relativeBytes = [System.Text.Encoding]::UTF8.GetBytes([string]$entry.Key)
            $aggregate.Write($relativeBytes, 0, $relativeBytes.Length)
            $aggregate.WriteByte(0)

            $fileInfo = Get-Item -LiteralPath ([string]$entry.Value) -Force
            $lengthBytes = [System.BitConverter]::GetBytes([int64]$fileInfo.Length)
            if (-not [System.BitConverter]::IsLittleEndian) {
                [System.Array]::Reverse($lengthBytes)
            }
            $aggregate.Write($lengthBytes, 0, $lengthBytes.Length)

            $fileHasher = [System.Security.Cryptography.SHA256]::Create()
            $fileStream = [System.IO.File]::Open(
                [string]$entry.Value,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::Read)
            try {
                $fileHash = $fileHasher.ComputeHash($fileStream)
                if ($fileStream.Position -ne $fileInfo.Length) {
                    throw "Batch 5 GC local source file changed while hashing: $($entry.Key)"
                }
            }
            finally {
                $fileStream.Dispose()
                $fileHasher.Dispose()
            }
            $aggregate.Write($fileHash, 0, $fileHash.Length)
        }

        $aggregate.Position = 0
        $treeHasher = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([System.BitConverter]::ToString($treeHasher.ComputeHash($aggregate))).Replace('-', '')
        }
        finally {
            $treeHasher.Dispose()
        }
    }
    finally {
        $aggregate.Dispose()
    }
}

function Write-Batch5GcAtomicBytes {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [byte[]] $Bytes
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $fullPath
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    $temporary = $fullPath + '.tmp-' + [Guid]::NewGuid().ToString('N')
    $replaceBackup = $fullPath + '.replace-backup-' + [Guid]::NewGuid().ToString('N')
    try {
        [System.IO.File]::WriteAllBytes($temporary, $Bytes)
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            try {
                [System.IO.File]::Replace($temporary, $fullPath, $replaceBackup, $true)
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
        if (Test-Path -LiteralPath $temporary -PathType Leaf) {
            Remove-Item -LiteralPath $temporary -Force
        }
        if (Test-Path -LiteralPath $replaceBackup -PathType Leaf) {
            Remove-Item -LiteralPath $replaceBackup -Force
        }
    }
}

function Get-Batch5GcBytesSha256 {
    param([Parameter(Mandatory = $true)] [byte[]] $Bytes)

    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($hasher.ComputeHash($Bytes))).Replace('-', '')
    }
    finally {
        $hasher.Dispose()
    }
}

function Get-Batch5GcFileSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $hasher = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    try {
        return ([System.BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-', '')
    }
    finally {
        $stream.Dispose()
        $hasher.Dispose()
    }
}

function Get-Batch5GcFileIdentity {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $exists = $false
    $length = [int64]0
    $sha256 = ''
    $failure = ''
    try {
        $exists = Test-Path -LiteralPath $Path -PathType Leaf
        if ($exists) {
            $length = [int64](Get-Item -LiteralPath $Path -Force).Length
            $sha256 = Get-Batch5GcFileSha256 -Path $Path
        }
    }
    catch {
        $failure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
    }
    return [ordered]@{
        Exists = $exists
        Length = $length
        Sha256 = $sha256
        Failure = $failure
        Available = [string]::IsNullOrWhiteSpace($failure)
    }
}

function ConvertTo-Batch5GcJsonBytes {
    param([Parameter(Mandatory = $true)] $Value)

    $json = $Value | ConvertTo-Json -Depth 40
    $encoding = New-Object System.Text.UTF8Encoding($false)
    return $encoding.GetBytes($json)
}

function Write-Batch5GcJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    Write-Batch5GcAtomicBytes -Path $Path -Bytes (ConvertTo-Batch5GcJsonBytes -Value $Value)
}

function Get-Batch5GcLocalSourceRequirement {
    param([Parameter(Mandatory = $true)] [ValidateSet('ActionSpeed','AutoFishing')] [string] $Domain)

    if ($Domain -eq 'ActionSpeed') {
        return [ordered]@{
            Domain = 'ActionSpeed'
            UniqueId = 'Yuuka.DTMAPI.ActionSpeed'
            OfficialFolder = 'Yuuka_DTMAPI_ActionSpeed'
            EntryDll = 'Content/DTMAPI/Yuuka.DTMAPI.ActionSpeed.dll'
            Mode = 'LocalDevelopment'
            DigestAlgorithm = 'DTMAPI-FileTree-SHA256-v1'
            RequiredRuntimeSource = 'OfficialLocal'
            RequiredWorkshopId = 'none'
        }
    }

    return [ordered]@{
        Domain = 'AutoFishing'
        UniqueId = 'Yuuka.DTMAPI.AutoFishing'
        OfficialFolder = 'Yuuka_DTMAPI_AutoFishing'
        EntryDll = 'Content/DTMAPI/Yuuka.DTMAPI.AutoFishing.dll'
        Mode = 'LocalDevelopment'
        DigestAlgorithm = 'DTMAPI-FileTree-SHA256-v1'
        RequiredRuntimeSource = 'OfficialLocal'
        RequiredWorkshopId = 'none'
    }
}

function New-Batch5GcLocalSourceSelection {
    param(
        [Parameter(Mandatory = $true)] [ValidateSet('ActionSpeed','AutoFishing')] [string] $Domain,
        [Parameter(Mandatory = $true)] [string] $PersistentRoot,
        [string] $SourcePath = ''
    )

    $requirement = Get-Batch5GcLocalSourceRequirement -Domain $Domain
    $usesExplicitSourcePath = -not [string]::IsNullOrWhiteSpace($SourcePath)
    $sourcePath = if (-not $usesExplicitSourcePath) {
        Get-Batch5GcCanonicalPath -Path (Join-Path (Join-Path $PersistentRoot 'MODS') ([string]$requirement.OfficialFolder))
    }
    else {
        Get-Batch5GcCanonicalPath -Path $SourcePath
    }
    $manifestPath = Join-Path $sourcePath 'Content\DTMAPI\manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Batch 5 GC local source manifest is missing: $manifestPath"
    }
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    if ([string]$manifest.UniqueID -cne [string]$requirement.UniqueId) {
        throw "Batch 5 GC local source UniqueID mismatch for $Domain. expected=$($requirement.UniqueId); actual=$($manifest.UniqueID)."
    }
    if ([string]$manifest.EntryDll -cne [string]$requirement.EntryDll) {
        throw "Batch 5 GC local source EntryDll mismatch for $Domain. expected=$($requirement.EntryDll); actual=$($manifest.EntryDll)."
    }
    $entryDllPath = Join-Path $sourcePath ([string]$requirement.EntryDll).Replace([char]47, [char]92)
    if (-not (Test-Path -LiteralPath $entryDllPath -PathType Leaf)) {
        throw "Batch 5 GC local source entry DLL is missing: $entryDllPath"
    }

    return [ordered]@{
        Domain = [string]$requirement.Domain
        UniqueId = [string]$requirement.UniqueId
        OfficialFolder = [string]$requirement.OfficialFolder
        Mode = [string]$requirement.Mode
        SourcePath = $sourcePath
        EntryDllPath = Get-Batch5GcCanonicalPath -Path $entryDllPath
        ExpectedTreeSha256 = Get-Batch5GcFileTreeDigest -Path $sourcePath
        DigestAlgorithm = [string]$requirement.DigestAlgorithm
        RequiredRuntimeSource = $(if ($usesExplicitSourcePath) { 'Local' } else { [string]$requirement.RequiredRuntimeSource })
        RequiredWorkshopId = [string]$requirement.RequiredWorkshopId
    }
}

function Start-Batch5GcAuthorSourceTransaction {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] $Selection,
        [Parameter(Mandatory = $true)] [string] $StageId,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] $OperationLock
    )

    $canonicalGameRoot = Get-Batch5GcCanonicalPath -Path $GameDir
    $statePath = Get-Batch5GcAuthorSourceStatePath -GameDir $canonicalGameRoot
    Assert-Batch5GcAuthorOperationLock -StatePath $statePath -OperationLock $OperationLock
    $transactionRoot = Join-Path $ReceiptRoot 'local-source-transaction'
    New-Item -ItemType Directory -Force -Path $transactionRoot | Out-Null
    $backupPath = Join-Path $transactionRoot 'source-state.before.bin'
    $originalIdentity = Get-Batch5GcFileIdentity -Path $statePath
    if (-not [bool]$originalIdentity.Available) {
        throw "Batch 5 GC could not inspect source-state before applying the transaction: $($originalIdentity.Failure)"
    }
    $originalExisted = [bool]$originalIdentity.Exists
    $originalLength = [int64]$originalIdentity.Length
    $originalSha256 = [string]$originalIdentity.Sha256
    if ($originalExisted) {
        Copy-Item -LiteralPath $statePath -Destination $backupPath -Force
        $backupIdentity = Get-Batch5GcFileIdentity -Path $backupPath
        if (-not [bool]$backupIdentity.Available -or -not [bool]$backupIdentity.Exists -or
            [int64]$backupIdentity.Length -ne $originalLength -or
            -not [string]::Equals([string]$backupIdentity.Sha256, $originalSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Batch 5 GC source-state changed or became unreadable while its exact backup was created.'
        }
    }
    else {
        'The source-state file did not exist before this transaction.' | Set-Content -LiteralPath (Join-Path $transactionRoot 'source-state.before.absent.txt') -Encoding UTF8
    }

    $summary = [ordered]@{
        SchemaVersion = 1
        StageId = $StageId
        Applied = $false
        StatePath = [System.IO.Path]::GetFullPath($statePath)
        BackupPath = if ($originalExisted) { [System.IO.Path]::GetFullPath($backupPath) } else { '' }
        OriginalExisted = $originalExisted
        OriginalLength = $originalLength
        OriginalSha256 = $originalSha256
        AppliedLength = [int64]0
        AppliedSha256 = ''
        GameRoot = $canonicalGameRoot
        OperationLockPath = Get-Batch5GcCanonicalPath -Path ([string]$OperationLock.Name)
        Selection = $Selection
        ReceiptRoot = [System.IO.Path]::GetFullPath($transactionRoot)
    }
    $stateApplied = $false
    try {
        $state = [ordered]@{
            schemaVersion = 1
            gameRoot = $canonicalGameRoot
            playerReproductionActive = $false
            reproductionSnapshotId = 'run-batch5-gc-ladder/' + $StageId
            selections = @([ordered]@{
                uniqueId = [string]$Selection.UniqueId
                mode = 'LocalDevelopment'
                sourcePath = [string]$Selection.SourcePath
                expectedTreeSha256 = [string]$Selection.ExpectedTreeSha256
            })
        }
        $stateBytes = ConvertTo-Batch5GcJsonBytes -Value $state
        $summary.AppliedLength = [int64]$stateBytes.Length
        $summary.AppliedSha256 = Get-Batch5GcBytesSha256 -Bytes $stateBytes
        $preApplyIdentity = Get-Batch5GcFileIdentity -Path $statePath
        $preApplyMatches = [bool]$preApplyIdentity.Available -and
            [bool]$preApplyIdentity.Exists -eq $originalExisted -and
            [int64]$preApplyIdentity.Length -eq $originalLength -and
            [string]::Equals([string]$preApplyIdentity.Sha256, $originalSha256, [System.StringComparison]::OrdinalIgnoreCase)
        if (-not $preApplyMatches) {
            throw 'Batch 5 GC source-state changed after backup and before apply; the transaction was not written.'
        }
        Write-Batch5GcAtomicBytes -Path $statePath -Bytes $stateBytes
        $stateApplied = $true
        $summary.Applied = $true
        Write-Batch5GcJson -Path (Join-Path $transactionRoot 'source-state-apply-summary.json') -Value $summary
        $null = Assert-Batch5GcAuthorSourceTransaction -Summary $summary -Phase 'after-apply'
        return $summary
    }
    catch {
        $applyError = $_
        $rollbackAttempted = $false
        $rollbackSucceeded = $false
        $rollbackSkippedReason = ''
        $rollbackFailure = ''
        try {
            if ($stateApplied) {
                $currentIdentity = Get-Batch5GcFileIdentity -Path $statePath
                $stillOwnsAppliedState = [bool]$currentIdentity.Available -and [bool]$currentIdentity.Exists -and
                    [int64]$currentIdentity.Length -eq [int64]$summary.AppliedLength -and
                    [string]::Equals([string]$currentIdentity.Sha256, [string]$summary.AppliedSha256, [System.StringComparison]::OrdinalIgnoreCase)
                if ($stillOwnsAppliedState) {
                    $rollbackAttempted = $true
                    if ($originalExisted) {
                        Write-Batch5GcAtomicBytes -Path $statePath -Bytes ([System.IO.File]::ReadAllBytes($backupPath))
                    }
                    elseif (Test-Path -LiteralPath $statePath -PathType Leaf) {
                        Remove-Item -LiteralPath $statePath -Force
                    }
                    $rollbackIdentity = Get-Batch5GcFileIdentity -Path $statePath
                    $rollbackSucceeded = [bool]$rollbackIdentity.Available -and
                        [bool]$rollbackIdentity.Exists -eq $originalExisted -and
                        [int64]$rollbackIdentity.Length -eq $originalLength -and
                        [string]::Equals([string]$rollbackIdentity.Sha256, $originalSha256, [System.StringComparison]::OrdinalIgnoreCase)
                }
                else {
                    $rollbackSkippedReason = 'applied-state-ownership-lost'
                }
            }
        }
        catch {
            $rollbackFailure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
        }
        $rollbackReceiptFailure = ''
        try {
            Write-Batch5GcJson -Path (Join-Path $transactionRoot 'source-state-apply-failure-rollback.json') -Value ([ordered]@{
                StateApplied = $stateApplied
                RollbackAttempted = $rollbackAttempted
                RollbackSucceeded = $rollbackSucceeded
                RollbackSkippedReason = $rollbackSkippedReason
                RollbackFailure = $rollbackFailure
            })
        }
        catch {
            $rollbackReceiptFailure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
        }
        if (-not [string]::IsNullOrWhiteSpace($rollbackFailure) -or -not [string]::IsNullOrWhiteSpace($rollbackReceiptFailure)) {
            throw [InvalidOperationException]::new(
                'Batch 5 GC source-state apply failed and rollback was not fully receipted. apply=' + $applyError.Exception.Message +
                    '; rollback=' + $rollbackFailure + '; receipt=' + $rollbackReceiptFailure,
                $applyError.Exception)
        }
        throw $applyError
    }
}

function Assert-Batch5GcAuthorSourceTransaction {
    param(
        [Parameter(Mandatory = $true)] $Summary,
        [Parameter(Mandatory = $true)] [string] $Phase
    )

    $statePath = [string]$Summary.StatePath
    $stateIdentity = Get-Batch5GcFileIdentity -Path $statePath
    $stateExists = [bool]$stateIdentity.Exists
    $stateLength = [int64]$stateIdentity.Length
    $stateSha256 = [string]$stateIdentity.Sha256
    $sourceDigest = ''
    $sourceFailure = ''
    try {
        $sourceDigest = Get-Batch5GcFileTreeDigest -Path ([string]$Summary.Selection.SourcePath)
    }
    catch {
        $sourceFailure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
    }
    $passed = [bool]$Summary.Applied -and [bool]$stateIdentity.Available -and $stateExists -and
        $stateLength -eq [int64]$Summary.AppliedLength -and
        [string]::Equals($stateSha256, [string]$Summary.AppliedSha256, [System.StringComparison]::OrdinalIgnoreCase) -and
        [string]::Equals($sourceDigest, [string]$Summary.Selection.ExpectedTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)
    $receipt = [ordered]@{
        Phase = $Phase
        StatePath = $statePath
        StateExists = $stateExists
        ExpectedStateLength = [int64]$Summary.AppliedLength
        ActualStateLength = $stateLength
        ExpectedStateSha256 = [string]$Summary.AppliedSha256
        ActualStateSha256 = $stateSha256
        StateInspectionFailure = [string]$stateIdentity.Failure
        SourcePath = [string]$Summary.Selection.SourcePath
        ExpectedTreeSha256 = [string]$Summary.Selection.ExpectedTreeSha256
        ActualTreeSha256 = $sourceDigest
        SourceFailure = $sourceFailure
        Passed = $passed
    }
    Write-Batch5GcJson -Path (Join-Path ([string]$Summary.ReceiptRoot) ('source-state-' + $Phase + '-verification.json')) -Value $receipt
    if (-not $passed) {
        throw "Batch 5 GC local source transaction drifted during '$Phase'. stateExists=$stateExists; sourceFailure=$sourceFailure."
    }
    return $receipt
}

function Complete-Batch5GcAuthorSourceTransaction {
    param(
        [Parameter(Mandatory = $true)] $Summary,
        [Parameter(Mandatory = $true)] $OperationLock
    )

    $statePath = [string]$Summary.StatePath
    Assert-Batch5GcAuthorOperationLock -StatePath $statePath -OperationLock $OperationLock
    $stateIdentityBefore = Get-Batch5GcFileIdentity -Path $statePath
    $stateExistsBefore = [bool]$stateIdentityBefore.Exists
    $stateLengthBefore = [int64]$stateIdentityBefore.Length
    $stateShaBefore = [string]$stateIdentityBefore.Sha256
    $appliedStateUnchanged = [bool]$stateIdentityBefore.Available -and $stateExistsBefore -and
        $stateLengthBefore -eq [int64]$Summary.AppliedLength -and
        [string]::Equals($stateShaBefore, [string]$Summary.AppliedSha256, [System.StringComparison]::OrdinalIgnoreCase)
    $sourceDigest = ''
    $sourceFailure = ''
    try {
        $sourceDigest = Get-Batch5GcFileTreeDigest -Path ([string]$Summary.Selection.SourcePath)
    }
    catch {
        $sourceFailure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
    }
    $sourceTreeUnchanged = [string]::Equals($sourceDigest, [string]$Summary.Selection.ExpectedTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)

    $restoreAttempted = $false
    $restoreSkippedReason = ''
    $restoreFailure = ''
    if ($appliedStateUnchanged) {
        $restoreAttempted = $true
        try {
            if ([bool]$Summary.OriginalExisted) {
                Write-Batch5GcAtomicBytes -Path $statePath -Bytes ([System.IO.File]::ReadAllBytes([string]$Summary.BackupPath))
            }
            elseif (Test-Path -LiteralPath $statePath -PathType Leaf) {
                Remove-Item -LiteralPath $statePath -Force
            }
        }
        catch {
            $restoreFailure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
        }
    }
    else {
        $restoreSkippedReason = if (-not [bool]$stateIdentityBefore.Available) { 'state-identity-unavailable' } else { 'applied-state-ownership-lost' }
    }

    $stateIdentityAfter = Get-Batch5GcFileIdentity -Path $statePath
    $existsAfter = [bool]$stateIdentityAfter.Exists
    $actualLength = [int64]$stateIdentityAfter.Length
    $actualSha256 = [string]$stateIdentityAfter.Sha256
    $restoredExact = $restoreAttempted -and [bool]$stateIdentityAfter.Available -and [string]::IsNullOrWhiteSpace($restoreFailure) -and
        $existsAfter -eq [bool]$Summary.OriginalExisted -and
        $actualLength -eq [int64]$Summary.OriginalLength -and
        [string]::Equals($actualSha256, [string]$Summary.OriginalSha256, [System.StringComparison]::OrdinalIgnoreCase)
    $passed = $appliedStateUnchanged -and $sourceTreeUnchanged -and $restoredExact
    $receipt = [ordered]@{
        StatePath = $statePath
        StageId = [string]$Summary.StageId
        StateInspectionFailure = [string]$stateIdentityBefore.Failure
        AppliedStateUnchanged = $appliedStateUnchanged
        SourceTreeUnchanged = $sourceTreeUnchanged
        ExpectedTreeSha256 = [string]$Summary.Selection.ExpectedTreeSha256
        ActualTreeSha256 = $sourceDigest
        SourceFailure = $sourceFailure
        OriginalExisted = [bool]$Summary.OriginalExisted
        ExistsAfter = $existsAfter
        ExpectedLength = [int64]$Summary.OriginalLength
        ActualLength = $actualLength
        ExpectedSha256 = [string]$Summary.OriginalSha256
        ActualSha256 = $actualSha256
        FinalStateInspectionFailure = [string]$stateIdentityAfter.Failure
        RestoreAttempted = $restoreAttempted
        RestoreSkippedReason = $restoreSkippedReason
        RestoreFailure = $restoreFailure
        RestoredExact = $restoredExact
        Passed = $passed
    }
    Write-Batch5GcJson -Path (Join-Path ([string]$Summary.ReceiptRoot) 'source-state-restore-verification.json') -Value $receipt
    return $receipt
}

function Get-Batch5GcOfficialLocalLoadReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] $Selection
    )

    $logPath = Join-Path $EvidencePath 'DTMAPI-latest.log'
    $ownerLines = @()
    if (Test-Path -LiteralPath $logPath -PathType Leaf) {
        $ownerPattern = 'Code mod load-source owner=' + [regex]::Escape([string]$Selection.UniqueId) + ';'
        $ownerLines = @(Select-String -LiteralPath $logPath -Pattern $ownerPattern | ForEach-Object { [string]$_.Line })
    }
    $source = ''
    $workshopId = ''
    $root = ''
    $dll = ''
    if ($ownerLines.Count -eq 1 -and $ownerLines[0] -match 'source=([^;]+); workshopId=([^;]+); root=(.*?); dll=(.+?)\.\s*$') {
        $source = $Matches[1].Trim()
        $workshopId = $Matches[2].Trim()
        $root = $Matches[3].Trim()
        $dll = $Matches[4].Trim()
    }
    $rootMatches = $false
    if (-not [string]::IsNullOrWhiteSpace($root)) {
        try {
            $rootMatches = [string]::Equals(
                (Get-Batch5GcCanonicalPath -Path $root),
                (Get-Batch5GcCanonicalPath -Path ([string]$Selection.SourcePath)),
                [System.StringComparison]::OrdinalIgnoreCase)
        }
        catch {
            $rootMatches = $false
        }
    }
    $dllMatches = $false
    if (-not [string]::IsNullOrWhiteSpace($dll)) {
        try {
            $dllMatches = [string]::Equals(
                (Get-Batch5GcCanonicalPath -Path $dll),
                (Get-Batch5GcCanonicalPath -Path ([string]$Selection.EntryDllPath)),
                [System.StringComparison]::OrdinalIgnoreCase)
        }
        catch {
            $dllMatches = $false
        }
    }
    $expectedSource = [string]$Selection.RequiredRuntimeSource
    if ($expectedSource -notin @('OfficialLocal','Local')) {
        throw "Batch 5 GC load receipt has an unsupported expected Runtime source: $expectedSource"
    }
    $passed = $ownerLines.Count -eq 1 -and $source -ceq $expectedSource -and $workshopId -ceq 'none' -and $rootMatches -and $dllMatches
    return [ordered]@{
        LogPath = [System.IO.Path]::GetFullPath($logPath)
        UniqueId = [string]$Selection.UniqueId
        ExpectedSource = $expectedSource
        ActualSource = $source
        ExpectedWorkshopId = 'none'
        ActualWorkshopId = $workshopId
        ExpectedRoot = [string]$Selection.SourcePath
        ActualRoot = $root
        ExpectedDll = [string]$Selection.EntryDllPath
        ActualDll = $dll
        OwnerLineCount = $ownerLines.Count
        RootMatches = $rootMatches
        DllMatches = $dllMatches
        Passed = $passed
    }
}
