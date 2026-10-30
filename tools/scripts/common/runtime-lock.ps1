# Shared runtime-lock helpers. Definitions only; PowerShell 5.1 compatible.
function Get-DtmApiRuntimeLockPath {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    if ($env:DTMAPI_RUNTIME_LOCK_PATH) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_RUNTIME_LOCK_PATH)
    }

    return Join-Path (Get-DtmApiGitCommonDir -RepoRoot $RepoRoot) 'dtmapi-runtime.lock.json'
}

function New-DtmApiRuntimeLockMetadata {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Reason = '',
        [string] $Owner = ''
    )

    $branch = Get-DtmApiCurrentBranch -RepoRoot $RepoRoot
    if ([string]::IsNullOrWhiteSpace($Owner)) {
        $Owner = '{0}@{1}/{2}' -f [Environment]::UserName, $env:COMPUTERNAME, $branch
    }

    return [ordered]@{
        SchemaVersion = 1
        Token = [guid]::NewGuid().ToString('N')
        Owner = $Owner
        Reason = $Reason
        Worktree = Get-DtmApiCurrentWorktree -RepoRoot $RepoRoot
        Branch = $branch
        Commit = Get-DtmApiCurrentCommit -RepoRoot $RepoRoot
        ProcessId = $PID
        Host = $env:COMPUTERNAME
        User = [Environment]::UserName
        CreatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    }
}

function Get-DtmApiRuntimeLockInfo {
    param(
        [string] $RepoRoot = (Get-RepoRoot)
    )

    $path = Get-DtmApiRuntimeLockPath -RepoRoot $RepoRoot
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        return [pscustomobject]@{
            Exists = $false
            IsReadable = $true
            Path = $path
            Data = $null
            Error = ''
        }
    }

    try {
        $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $path | ConvertFrom-Json
        return [pscustomobject]@{
            Exists = $true
            IsReadable = $true
            Path = $path
            Data = $data
            Error = ''
        }
    }
    catch {
        return [pscustomobject]@{
            Exists = $true
            IsReadable = $false
            Path = $path
            Data = $null
            Error = $_.Exception.Message
        }
    }
}

function Test-DtmApiRuntimeLockOwnedByCurrentWorktree {
    param(
        [Parameter(Mandatory = $true)] $LockInfo,
        [string] $RepoRoot = (Get-RepoRoot)
    )

    if (-not $LockInfo.Exists -or -not $LockInfo.IsReadable -or $null -eq $LockInfo.Data) {
        return $false
    }

    $currentWorktree = [System.IO.Path]::GetFullPath((Get-DtmApiCurrentWorktree -RepoRoot $RepoRoot)).TrimEnd('\')
    $lockedWorktree = [string](Get-DtmApiObjectProperty -Object $LockInfo.Data -Name 'Worktree' -Default '')
    if ([string]::IsNullOrWhiteSpace($lockedWorktree)) {
        return $false
    }

    $lockedWorktree = [System.IO.Path]::GetFullPath($lockedWorktree).TrimEnd('\')
    return [string]::Equals($currentWorktree, $lockedWorktree, [System.StringComparison]::OrdinalIgnoreCase)
}

function Format-DtmApiRuntimeLockInfo {
    param(
        [Parameter(Mandatory = $true)] $LockInfo
    )

    if (-not $LockInfo.Exists) {
        return "[FREE] Runtime lock is free. Path: $($LockInfo.Path)"
    }

    if (-not $LockInfo.IsReadable) {
        return "[LOCKED] Runtime lock exists but cannot be read. Path: $($LockInfo.Path). Error: $($LockInfo.Error)"
    }

    $data = $LockInfo.Data
    $createdAt = [string](Get-DtmApiObjectProperty -Object $data -Name 'CreatedAt' -Default '')
    $age = ''
    if (-not [string]::IsNullOrWhiteSpace($createdAt)) {
        try {
            $ageSpan = [DateTimeOffset]::UtcNow - [DateTimeOffset]::Parse($createdAt).ToUniversalTime()
            $age = ' age={0:hh\:mm\:ss}' -f $ageSpan
        }
        catch {
            $age = ''
        }
    }

    return "[LOCKED] owner=$((Get-DtmApiObjectProperty -Object $data -Name 'Owner' -Default '(unknown)')) branch=$((Get-DtmApiObjectProperty -Object $data -Name 'Branch' -Default '(unknown)')) commit=$((Get-DtmApiObjectProperty -Object $data -Name 'Commit' -Default '(unknown)')) reason=$((Get-DtmApiObjectProperty -Object $data -Name 'Reason' -Default ''))$age worktree=$((Get-DtmApiObjectProperty -Object $data -Name 'Worktree' -Default '(unknown)'))"
}

function Acquire-DtmApiRuntimeLock {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Reason = '',
        [string] $Owner = '',
        [switch] $AllowCurrentWorktreeReuse,
        [switch] $NoThrow
    )

    $path = Get-DtmApiRuntimeLockPath -RepoRoot $RepoRoot
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    $metadata = New-DtmApiRuntimeLockMetadata -RepoRoot $RepoRoot -Reason $Reason -Owner $Owner
    $json = ($metadata | ConvertTo-Json -Depth 8)
    $encoding = New-Object System.Text.UTF8Encoding($false)

    try {
        $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        try {
            $bytes = $encoding.GetBytes($json)
            $stream.Write($bytes, 0, $bytes.Length)
        }
        finally {
            $stream.Dispose()
        }

        return [pscustomobject]@{
            Acquired = $true
            Reused = $false
            Path = $path
            Data = [pscustomobject]$metadata
            Message = "Acquired DTMAPI runtime lock: $path"
        }
    }
    catch [System.IO.IOException] {
        $info = Get-DtmApiRuntimeLockInfo -RepoRoot $RepoRoot
        if ($AllowCurrentWorktreeReuse -and (Test-DtmApiRuntimeLockOwnedByCurrentWorktree -LockInfo $info -RepoRoot $RepoRoot)) {
            return [pscustomobject]@{
                Acquired = $false
                Reused = $true
                Path = $path
                Data = $info.Data
                Message = "Reusing DTMAPI runtime lock already owned by this worktree: $path"
            }
        }

        $message = "DTMAPI runtime lock is already held. $((Format-DtmApiRuntimeLockInfo -LockInfo $info))"
        if ($NoThrow) {
            return [pscustomobject]@{
                Acquired = $false
                Reused = $false
                Path = $path
                Data = $info.Data
                Message = $message
            }
        }
        throw $message
    }
}

function Wait-DtmApiRuntimeLock {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [string] $Reason = '',
        [string] $Owner = '',
        [int] $TimeoutSeconds = 3600,
        [int] $PollSeconds = 5,
        [switch] $AllowCurrentWorktreeReuse
    )

    if ($TimeoutSeconds -lt 0) {
        throw "TimeoutSeconds must be >= 0."
    }
    if ($PollSeconds -lt 1) {
        $PollSeconds = 1
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ($true) {
        $lock = Acquire-DtmApiRuntimeLock -RepoRoot $RepoRoot -Reason $Reason -Owner $Owner -AllowCurrentWorktreeReuse:$AllowCurrentWorktreeReuse -NoThrow
        if ($lock.Acquired -or $lock.Reused) {
            return $lock
        }

        if ((Get-Date) -ge $deadline) {
            throw "Timed out waiting for DTMAPI runtime lock after $TimeoutSeconds seconds. $($lock.Message)"
        }

        Write-Host "Waiting for DTMAPI runtime lock. $($lock.Message)"
        Start-Sleep -Seconds $PollSeconds
    }
}

function Release-DtmApiRuntimeLock {
    param(
        [string] $RepoRoot = (Get-RepoRoot),
        [switch] $Force
    )

    $info = Get-DtmApiRuntimeLockInfo -RepoRoot $RepoRoot
    if (-not $info.Exists) {
        return [pscustomobject]@{
            Released = $false
            Path = $info.Path
            Message = "DTMAPI runtime lock is already free."
        }
    }

    if (-not $Force -and -not (Test-DtmApiRuntimeLockOwnedByCurrentWorktree -LockInfo $info -RepoRoot $RepoRoot)) {
        throw "Refusing to release DTMAPI runtime lock owned by another worktree. Use -Force only after verifying the owner is stale. $((Format-DtmApiRuntimeLockInfo -LockInfo $info))"
    }

    Remove-Item -LiteralPath $info.Path -Force
    return [pscustomobject]@{
        Released = $true
        Path = $info.Path
        Message = "Released DTMAPI runtime lock: $($info.Path)"
    }
}

function Assert-DtmApiRuntimeTransactionPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Actual,
        [Parameter(Mandatory = $true)] [string] $Expected,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $actualFull = [System.IO.Path]::GetFullPath($Actual)
    $expectedFull = [System.IO.Path]::GetFullPath($Expected)
    if (-not [string]::Equals($actualFull, $expectedFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Interrupted DTMAPI Runtime transaction has an unsafe $Label path. Expected=$expectedFull Actual=$actualFull"
    }
}

function Assert-DtmApiRuntimeTransactionOwnedPathSafe {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Boundary,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $pathFull = [System.IO.Path]::GetFullPath($Path)
    $boundaryFull = [System.IO.Path]::GetFullPath($Boundary)
    if (-not (Test-DtmApiPathIsSameOrChild -Child $pathFull -Parent $boundaryFull)) {
        throw "Interrupted DTMAPI Runtime transaction $Label escaped its validated boundary. Boundary=$boundaryFull Path=$pathFull"
    }

    $cursor = $pathFull
    while (Test-DtmApiPathIsSameOrChild -Child $cursor -Parent $boundaryFull) {
        # The caller owns the boundary itself (game/state root). Only the
        # transaction-owned path below it must be free of reparse points.
        if ([string]::Equals($cursor, $boundaryFull, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Interrupted DTMAPI Runtime transaction $Label contains a reparse point: $($item.FullName)"
            }
        }
        $parent = [System.IO.Path]::GetDirectoryName($cursor)
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $cursor, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $cursor = $parent
    }
}

function Test-DtmApiRuntimeTransactionStamp {
    param([Parameter(Mandatory = $true)] [string] $Stamp)

    return [regex]::IsMatch($Stamp, '^\d{8}-\d{6}-\d{3}-[0-9a-fA-F]{8}$')
}

function Read-DtmApiRuntimeTransactionReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $RuntimeTransactionRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir
    )

    $receiptPath = Join-Path $RuntimeTransactionRoot 'transaction.json'
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        throw "Interrupted DTMAPI Runtime transaction has no recovery receipt: $RuntimeTransactionRoot"
    }
    try {
        $receipt = Get-Content -Raw -Encoding UTF8 -LiteralPath $receiptPath | ConvertFrom-Json
    }
    catch {
        throw "Interrupted DTMAPI Runtime transaction receipt is unreadable: $receiptPath. $($_.Exception.Message)"
    }

    $requiredProperties = @(
        'SchemaVersion', 'GameDir', 'StateDir', 'PluginDir', 'Phase', 'RuntimeTransactionRoot', 'TransactionRoot',
        'CandidatePlugin', 'RecoveryPlugin', 'CandidateTools', 'CandidateReleaseManifest', 'CandidateInstallState',
        'RecoveryState', 'LiveTools', 'LiveReleaseManifest', 'LiveInstallState', 'OldPluginExisted', 'OldPluginMoved',
        'CandidatePluginPlaced', 'OldToolsExisted', 'OldToolsMoved', 'CandidateToolsPlaced',
        'OldReleaseManifestExisted', 'OldReleaseManifestMoved', 'CandidateReleaseManifestPlaced',
        'OldInstallStateExisted', 'OldInstallStateMoved', 'CandidateInstallStatePlaced', 'CommitSucceeded'
    )
    foreach ($propertyName in $requiredProperties) {
        if ($null -eq $receipt.PSObject.Properties[$propertyName]) {
            throw "Interrupted DTMAPI Runtime transaction receipt is missing ${propertyName}: $receiptPath"
        }
    }
    if ([int]$receipt.SchemaVersion -ne 1) {
        throw "Interrupted DTMAPI Runtime transaction receipt has unsupported schema $($receipt.SchemaVersion): $receiptPath"
    }

    $componentReceiptProperties = @(
        'CandidateComponents', 'LiveComponents', 'OldComponentsExisted', 'OldComponentsMoved', 'CandidateComponentsPlaced'
    )
    $componentReceiptPropertyCount = @($componentReceiptProperties | Where-Object { $null -ne $receipt.PSObject.Properties[$_] }).Count
    if ($componentReceiptPropertyCount -ne 0 -and $componentReceiptPropertyCount -ne $componentReceiptProperties.Count) {
        throw "Interrupted DTMAPI Runtime transaction receipt has an incomplete optional-component projection: $receiptPath"
    }
    $hasComponentReceipt = $componentReceiptPropertyCount -eq $componentReceiptProperties.Count

    $runtimeRootFull = [System.IO.Path]::GetFullPath($RuntimeTransactionRoot)
    $runtimeRootName = Split-Path -Leaf $runtimeRootFull
    $runtimePrefix = '.dtmapi-runtime-install-'
    if (-not $runtimeRootName.StartsWith($runtimePrefix, [System.StringComparison]::Ordinal) -or $runtimeRootName.Length -le $runtimePrefix.Length) {
        throw "Interrupted DTMAPI Runtime transaction directory has an invalid name: $runtimeRootFull"
    }
    $stamp = $runtimeRootName.Substring($runtimePrefix.Length)
    if (-not (Test-DtmApiRuntimeTransactionStamp -Stamp $stamp)) {
        throw "Interrupted DTMAPI Runtime transaction directory has an invalid stamp: $runtimeRootFull"
    }

    $gameFull = [System.IO.Path]::GetFullPath($GameDir)
    $stateFull = [System.IO.Path]::GetFullPath($StateDir)
    $pluginFull = [System.IO.Path]::GetFullPath($PluginDir)
    $expectedStateTransactionRoot = Join-Path $stateFull ('.runtime-install-transaction-' + $stamp)
    Assert-DtmApiRuntimeTransactionPath -Actual (Split-Path -Parent $runtimeRootFull) -Expected $gameFull -Label 'game-root parent'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.GameDir) -Expected $gameFull -Label 'game directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.StateDir) -Expected $stateFull -Label 'state directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.PluginDir) -Expected $pluginFull -Label 'live plugin directory'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RuntimeTransactionRoot) -Expected $runtimeRootFull -Label 'runtime transaction root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.TransactionRoot) -Expected $expectedStateTransactionRoot -Label 'state transaction root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidatePlugin) -Expected (Join-Path $runtimeRootFull 'candidate-plugin') -Label 'candidate plugin'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RecoveryPlugin) -Expected (Join-Path $runtimeRootFull 'recovery-plugin') -Label 'recovery plugin'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateTools) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\tools') -Label 'candidate tools'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateReleaseManifest) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\release-manifest.json') -Label 'candidate release manifest'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateInstallState) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\install-state.json') -Label 'candidate install state'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.RecoveryState) -Expected (Join-Path $expectedStateTransactionRoot 'recovery') -Label 'state recovery root'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveTools) -Expected (Join-Path $stateFull 'tools') -Label 'live tools'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveReleaseManifest) -Expected (Join-Path $stateFull 'release-manifest.json') -Label 'live release manifest'
    Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveInstallState) -Expected (Join-Path $stateFull 'install-state.json') -Label 'live install state'
    if ($hasComponentReceipt) {
        Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.CandidateComponents) -Expected (Join-Path $expectedStateTransactionRoot 'candidate\components') -Label 'candidate components'
        Assert-DtmApiRuntimeTransactionPath -Actual ([string]$receipt.LiveComponents) -Expected (Join-Path $stateFull 'components') -Label 'live components'
    }

    foreach ($ownedPath in @(
        [pscustomobject]@{ Path = $runtimeRootFull; Boundary = $gameFull; Label = 'Runtime transaction root' },
        [pscustomobject]@{ Path = [string]$receipt.CandidatePlugin; Boundary = $runtimeRootFull; Label = 'candidate plugin path' },
        [pscustomobject]@{ Path = [string]$receipt.RecoveryPlugin; Boundary = $runtimeRootFull; Label = 'recovery plugin path' },
        [pscustomobject]@{ Path = [string]$receipt.TransactionRoot; Boundary = $stateFull; Label = 'state transaction root' },
        [pscustomobject]@{ Path = [string]$receipt.CandidateTools; Boundary = [string]$receipt.TransactionRoot; Label = 'candidate tools path' },
        [pscustomobject]@{ Path = $(if ($hasComponentReceipt) { [string]$receipt.CandidateComponents } else { Join-Path ([string]$receipt.TransactionRoot) 'candidate\components' }); Boundary = [string]$receipt.TransactionRoot; Label = 'candidate components path' },
        [pscustomobject]@{ Path = [string]$receipt.CandidateReleaseManifest; Boundary = [string]$receipt.TransactionRoot; Label = 'candidate release-manifest path' },
        [pscustomobject]@{ Path = [string]$receipt.CandidateInstallState; Boundary = [string]$receipt.TransactionRoot; Label = 'candidate install-state path' },
        [pscustomobject]@{ Path = [string]$receipt.RecoveryState; Boundary = [string]$receipt.TransactionRoot; Label = 'state recovery path' }
    )) {
        Assert-DtmApiRuntimeTransactionOwnedPathSafe -Path $ownedPath.Path -Boundary $ownedPath.Boundary -Label $ownedPath.Label
    }

    $rollbackSucceeded = $null
    if ($null -ne $receipt.PSObject.Properties['RollbackSucceeded'] -and $null -ne $receipt.RollbackSucceeded) {
        $rollbackSucceeded = [bool]$receipt.RollbackSucceeded
    }
    $transaction = [pscustomobject][ordered]@{
        Phase = [string]$receipt.Phase
        FailedPhase = if ($null -eq $receipt.PSObject.Properties['FailedPhase']) { '' } else { [string]$receipt.FailedPhase }
        RuntimeTransactionRoot = $runtimeRootFull
        ReceiptPath = [System.IO.Path]::GetFullPath($receiptPath)
        TransactionRoot = [System.IO.Path]::GetFullPath([string]$receipt.TransactionRoot)
        CandidatePlugin = [System.IO.Path]::GetFullPath([string]$receipt.CandidatePlugin)
        RecoveryPlugin = [System.IO.Path]::GetFullPath([string]$receipt.RecoveryPlugin)
        CandidateTools = [System.IO.Path]::GetFullPath([string]$receipt.CandidateTools)
        CandidateComponents = [System.IO.Path]::GetFullPath($(if ($hasComponentReceipt) { [string]$receipt.CandidateComponents } else { Join-Path ([string]$receipt.TransactionRoot) 'candidate\components' }))
        CandidateReleaseManifest = [System.IO.Path]::GetFullPath([string]$receipt.CandidateReleaseManifest)
        CandidateInstallState = [System.IO.Path]::GetFullPath([string]$receipt.CandidateInstallState)
        RecoveryState = [System.IO.Path]::GetFullPath([string]$receipt.RecoveryState)
        LiveTools = [System.IO.Path]::GetFullPath([string]$receipt.LiveTools)
        LiveComponents = [System.IO.Path]::GetFullPath($(if ($hasComponentReceipt) { [string]$receipt.LiveComponents } else { Join-Path $stateFull 'components' }))
        LiveReleaseManifest = [System.IO.Path]::GetFullPath([string]$receipt.LiveReleaseManifest)
        LiveInstallState = [System.IO.Path]::GetFullPath([string]$receipt.LiveInstallState)
        RuntimeMetadata = @()
        RuntimeFileRecords = @()
        StateToolRecords = @()
        OptionalComponentMetadata = @()
        OptionalComponentFileRecords = @()
        AssetRecord = $null
        OldPluginExisted = [bool]$receipt.OldPluginExisted
        OldPluginMoved = [bool]$receipt.OldPluginMoved
        CandidatePluginPlaced = [bool]$receipt.CandidatePluginPlaced
        OldToolsExisted = [bool]$receipt.OldToolsExisted
        OldToolsMoved = [bool]$receipt.OldToolsMoved
        CandidateToolsPlaced = [bool]$receipt.CandidateToolsPlaced
        OldComponentsExisted = if ($hasComponentReceipt) { [bool]$receipt.OldComponentsExisted } else { $false }
        OldComponentsMoved = if ($hasComponentReceipt) { [bool]$receipt.OldComponentsMoved } else { $false }
        CandidateComponentsPlaced = if ($hasComponentReceipt) { [bool]$receipt.CandidateComponentsPlaced } else { $false }
        OldReleaseManifestExisted = [bool]$receipt.OldReleaseManifestExisted
        OldReleaseManifestMoved = [bool]$receipt.OldReleaseManifestMoved
        CandidateReleaseManifestPlaced = [bool]$receipt.CandidateReleaseManifestPlaced
        OldInstallStateExisted = [bool]$receipt.OldInstallStateExisted
        OldInstallStateMoved = [bool]$receipt.OldInstallStateMoved
        CandidateInstallStatePlaced = [bool]$receipt.CandidateInstallStatePlaced
        CommitSucceeded = [bool]$receipt.CommitSucceeded
        RollbackSucceeded = $rollbackSucceeded
        RollbackFaultConsumed = $false
        ReceiptEstablished = $true
    }

    switch ([string]$transaction.Phase) {
        'MovingOldRuntime' { if (Test-Path -LiteralPath $transaction.RecoveryPlugin -PathType Container) { $transaction.OldPluginMoved = $true } }
        'PlacingCandidate' { if (-not (Test-Path -LiteralPath $transaction.CandidatePlugin -PathType Container) -and (Test-Path -LiteralPath $pluginFull -PathType Container)) { $transaction.CandidatePluginPlaced = $true } }
        'MovingOldTools' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'tools') -PathType Container) { $transaction.OldToolsMoved = $true } }
        'PlacingTools' { if (-not (Test-Path -LiteralPath $transaction.CandidateTools -PathType Container) -and (Test-Path -LiteralPath $transaction.LiveTools -PathType Container)) { $transaction.CandidateToolsPlaced = $true } }
        'MovingOldComponents' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'components') -PathType Container) { $transaction.OldComponentsMoved = $true } }
        'PlacingComponents' { if (-not (Test-Path -LiteralPath $transaction.CandidateComponents -PathType Container) -and (Test-Path -LiteralPath $transaction.LiveComponents -PathType Container)) { $transaction.CandidateComponentsPlaced = $true } }
        'MovingOldReleaseManifest' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'release-manifest.json') -PathType Leaf) { $transaction.OldReleaseManifestMoved = $true } }
        'PlacingReleaseManifest' { if (-not (Test-Path -LiteralPath $transaction.CandidateReleaseManifest -PathType Leaf) -and (Test-Path -LiteralPath $transaction.LiveReleaseManifest -PathType Leaf)) { $transaction.CandidateReleaseManifestPlaced = $true } }
        'MovingOldInstallState' { if (Test-Path -LiteralPath (Join-Path $transaction.RecoveryState 'install-state.json') -PathType Leaf) { $transaction.OldInstallStateMoved = $true } }
        'PlacingInstallState' { if (-not (Test-Path -LiteralPath $transaction.CandidateInstallState -PathType Leaf) -and (Test-Path -LiteralPath $transaction.LiveInstallState -PathType Leaf)) { $transaction.CandidateInstallStatePlaced = $true } }
    }
    return $transaction
}

function Get-DtmApiRuntimeTransactionClassifications {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir
    )

    $gameFull = [System.IO.Path]::GetFullPath($GameDir)
    $stateFull = [System.IO.Path]::GetFullPath($StateDir)
    $pluginFull = [System.IO.Path]::GetFullPath($PluginDir)
    $runtimePrefix = '.dtmapi-runtime-install-'
    $statePrefix = '.runtime-install-transaction-'
    $runtimeItems = @(Get-ChildItem -LiteralPath $gameFull -Force -Filter ($runtimePrefix + '*') -ErrorAction Stop | Sort-Object Name)
    $stateItems = if (Test-Path -LiteralPath $stateFull -PathType Container) {
        @(Get-ChildItem -LiteralPath $stateFull -Force -Filter ($statePrefix + '*') -ErrorAction Stop | Sort-Object Name)
    }
    else {
        @()
    }
    $stateByStamp = @{}
    foreach ($stateItem in $stateItems) {
        $stateName = [string]$stateItem.Name
        if ($stateName.StartsWith($statePrefix, [System.StringComparison]::Ordinal) -and $stateName.Length -gt $statePrefix.Length) {
            $stateByStamp[$stateName.Substring($statePrefix.Length).ToUpperInvariant()] = $stateItem
        }
    }

    $results = New-Object System.Collections.Generic.List[object]
    $pairedStateStamps = @{}
    foreach ($runtimeItem in $runtimeItems) {
        $runtimePath = [System.IO.Path]::GetFullPath($runtimeItem.FullName)
        $runtimeName = [string]$runtimeItem.Name
        $stamp = if ($runtimeName.StartsWith($runtimePrefix, [System.StringComparison]::Ordinal) -and $runtimeName.Length -gt $runtimePrefix.Length) {
            $runtimeName.Substring($runtimePrefix.Length)
        }
        else {
            ''
        }
        $stampKey = $stamp.ToUpperInvariant()
        $stateItem = if ($stateByStamp.ContainsKey($stampKey)) { $stateByStamp[$stampKey] } else { $null }
        if ($null -ne $stateItem) { $pairedStateStamps[$stampKey] = $true }
        $receiptPath = Join-Path $runtimePath 'transaction.json'

        if (-not $runtimeItem.PSIsContainer -or -not (Test-DtmApiRuntimeTransactionStamp -Stamp $stamp)) {
            $results.Add([pscustomobject]@{
                Kind = 'UnsafeNoReceipt'; Stamp = $stamp; RuntimeRoot = $runtimePath; StateRoot = if ($null -eq $stateItem) { '' } else { [string]$stateItem.FullName }
                ReceiptPath = $receiptPath; ReasonCode = 'invalid-runtime-root'; Detail = 'The Runtime transaction item is not an exact supported transaction directory.'; Transaction = $null
            }) | Out-Null
            continue
        }

        if (Test-Path -LiteralPath $receiptPath -PathType Leaf) {
            try {
                $transaction = Read-DtmApiRuntimeTransactionReceipt -RuntimeTransactionRoot $runtimePath -GameDir $gameFull -StateDir $stateFull -PluginDir $pluginFull
                $results.Add([pscustomobject]@{
                    Kind = 'RecoverableReceipt'; Stamp = $stamp; RuntimeRoot = $runtimePath; StateRoot = [string]$transaction.TransactionRoot
                    ReceiptPath = $receiptPath; ReasonCode = 'validated-receipt'; Detail = ''; Transaction = $transaction
                }) | Out-Null
            }
            catch {
                $results.Add([pscustomobject]@{
                    Kind = 'InvalidReceipt'; Stamp = $stamp; RuntimeRoot = $runtimePath; StateRoot = if ($null -eq $stateItem) { '' } else { [string]$stateItem.FullName }
                    ReceiptPath = $receiptPath; ReasonCode = 'invalid-receipt'; Detail = [string]$_.Exception.Message; Transaction = $null
                }) | Out-Null
            }
            continue
        }

        $sterile = $null -eq $stateItem
        $sterileReason = if ($sterile) { '' } else { 'A matching state transaction exists.' }
        if (($runtimeItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            $sterile = $false
            $sterileReason = 'The Runtime transaction root is a reparse point.'
        }
        if ($sterile) {
            try {
                foreach ($entry in @(Get-ChildItem -LiteralPath $runtimePath -Force -ErrorAction Stop)) {
                    $isReparse = ($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
                    $isKnownReceiptTemp = -not $entry.PSIsContainer -and
                        [regex]::IsMatch([string]$entry.Name, '^transaction\.json\.(tmp|bak)-[0-9a-fA-F]{32}$')
                    if ($isReparse -or -not $isKnownReceiptTemp) {
                        $sterile = $false
                        $sterileReason = "Unknown or unsafe receiptless entry: $($entry.FullName)"
                        break
                    }
                }
            }
            catch {
                $sterile = $false
                $sterileReason = "Could not enumerate the receiptless transaction root: $($_.Exception.Message)"
            }
        }
        $results.Add([pscustomobject]@{
            Kind = if ($sterile) { 'SterileNoReceipt' } else { 'UnsafeNoReceipt' }
            Stamp = $stamp
            RuntimeRoot = $runtimePath
            StateRoot = if ($null -eq $stateItem) { '' } else { [System.IO.Path]::GetFullPath($stateItem.FullName) }
            ReceiptPath = $receiptPath
            ReasonCode = if ($sterile) { 'sterile-pre-receipt' } else { 'unsafe-receiptless' }
            Detail = $sterileReason
            Transaction = $null
        }) | Out-Null
    }

    foreach ($stateItem in $stateItems) {
        $stateName = [string]$stateItem.Name
        $stamp = if ($stateName.StartsWith($statePrefix, [System.StringComparison]::Ordinal) -and $stateName.Length -gt $statePrefix.Length) {
            $stateName.Substring($statePrefix.Length)
        }
        else {
            ''
        }
        if (-not $pairedStateStamps.ContainsKey($stamp.ToUpperInvariant())) {
            $results.Add([pscustomobject]@{
                Kind = 'OrphanState'; Stamp = $stamp; RuntimeRoot = ''; StateRoot = [System.IO.Path]::GetFullPath($stateItem.FullName)
                ReceiptPath = ''; ReasonCode = 'orphan-state'; Detail = 'State recovery data has no matching Runtime transaction root.'; Transaction = $null
            }) | Out-Null
        }
    }

    return @($results.ToArray())
}

function Remove-DtmApiSterileRuntimeTransactionRoot {
    param(
        [Parameter(Mandatory = $true)] $Classification,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir
    )

    $runtimeRoot = [System.IO.Path]::GetFullPath([string]$Classification.RuntimeRoot)
    $fresh = @(Get-DtmApiRuntimeTransactionClassifications -GameDir $GameDir -StateDir $StateDir -PluginDir $PluginDir |
        Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.RuntimeRoot) -and [string]::Equals([System.IO.Path]::GetFullPath([string]$_.RuntimeRoot), $runtimeRoot, [System.StringComparison]::OrdinalIgnoreCase) })
    if ($fresh.Count -ne 1 -or -not [string]::Equals([string]$fresh[0].Kind, 'SterileNoReceipt', [System.StringComparison]::Ordinal)) {
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1303' `
            -Chinese '无凭据事务残留在清理前发生变化，已拒绝删除。' `
            -English 'The receiptless transaction residue changed before cleanup; deletion was refused.' `
            -Detail $runtimeRoot
    }

    try {
        foreach ($entry in @(Get-ChildItem -LiteralPath $runtimeRoot -Force -ErrorAction Stop)) {
            [System.IO.File]::Delete([System.IO.Path]::GetFullPath($entry.FullName))
        }
        [System.IO.Directory]::Delete($runtimeRoot, $false)
    }
    catch {
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1102' `
            -Chinese '安全的事务空壳仍被占用或被安全软件拦截，无法清理。' `
            -English 'The safe transaction shell is still locked or blocked by endpoint security and could not be removed.' `
            -Detail "$runtimeRoot; $($_.Exception.Message)" `
            -InnerException $_.Exception
    }
}
