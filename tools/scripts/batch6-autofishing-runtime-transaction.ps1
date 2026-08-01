. "$PSScriptRoot\common.ps1"

$script:Batch6AutoFishingUniqueId = 'Yuuka.DTMAPI.AutoFishing'
$script:Batch6AutoFishingEntryDll = 'Yuuka.DTMAPI.AutoFishing.dll'
$script:Batch6AutoFishingPolicyId = 'doloctown-23762374-autofishing-v1'
$script:Batch6AutoFishingHarmonyOwner = 'dtmapi.mod.yuuka.dtmapi.autofishing'
$script:Batch6AutoFishingPackageName = 'DTMAPI-AutoFishing-advanced-pilot.zip'
$script:Batch6AutoFishingSdkName = 'DTMAPI-Author-SDK-0.1.0-win-x64.zip'
$script:Batch6AutoFishingTreeAlgorithm = 'DTMAPI-FileTree-SHA256-v1'
$script:Batch6AutoFishingUtf8 = New-Object System.Text.UTF8Encoding($false)
$script:Batch6AutoFishingPendingSourceTransaction = $null

function Get-Batch6AutoFishingCanonicalPath {
    param([Parameter(Mandatory = $true)] [string] $Path)
    return [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
}

function Test-Batch6AutoFishingPathEquals {
    param(
        [Parameter(Mandatory = $true)] [string] $Left,
        [Parameter(Mandatory = $true)] [string] $Right
    )
    return [string]::Equals(
        (Get-Batch6AutoFishingCanonicalPath -Path $Left),
        (Get-Batch6AutoFishingCanonicalPath -Path $Right),
        [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-Batch6AutoFishingSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Batch 6 AutoFishing hash input is missing: $Path"
    }
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

function Get-Batch6AutoFishingBytesSha256 {
    param([Parameter(Mandatory = $true)] [byte[]] $Bytes)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-', '').ToUpperInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Write-Batch6AutoFishingJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [object] $Value
    )
    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 100) + "`n"), $script:Batch6AutoFishingUtf8)
}

function Get-Batch6AutoFishingOptionalStringProperty {
    param(
        [AllowNull()] [object] $Value,
        [Parameter(Mandatory = $true)] [string] $Name
    )
    if ($null -eq $Value) { return '' }
    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) { return '' }
    return [string]$property.Value
}

function Get-Batch6AutoFishingPendingSourceTransaction {
    return $script:Batch6AutoFishingPendingSourceTransaction
}

function Get-Batch6AutoFishingGameProcessSnapshot {
    param([AllowEmptyString()] [string] $GameDir = '')

    $items = New-Object System.Collections.ArrayList
    foreach ($process in @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue)) {
        [string]$processPath = ''
        [string]$pathError = ''
        try { $processPath = [string]$process.Path }
        catch { $pathError = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message }
        $target = -not [string]::IsNullOrWhiteSpace($GameDir) -and
            -not [string]::IsNullOrWhiteSpace($processPath) -and
            (Test-DtmApiPathIsSameOrChild -Child $processPath -Parent $GameDir)
        [void]$items.Add([pscustomobject]@{
            Process = $process
            Id = [int]$process.Id
            Path = $processPath
            PathError = $pathError
            IsTargetGameProcess = [bool]$target
        })
    }
    return @($items.ToArray())
}

function ConvertTo-Batch6AutoFishingGameProcessReceiptItems {
    param([object[]] $Items)

    return @($Items | ForEach-Object {
        [ordered]@{
            Id = [int]$_.Id
            Path = [string]$_.Path
            PathError = [string]$_.PathError
            IsTargetGameProcess = [bool]$_.IsTargetGameProcess
        }
    })
}

function Wait-Batch6AutoFishingGameProcessExit {
    param(
        [AllowEmptyString()] [string] $GameDir = '',
        [Parameter(Mandatory = $true)] [int] $Seconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        $snapshot = @(Get-Batch6AutoFishingGameProcessSnapshot -GameDir $GameDir)
        if ($snapshot.Count -eq 0) { return $snapshot }
        if ([DateTime]::UtcNow -ge $deadline) { return $snapshot }
        Start-Sleep -Milliseconds 250
    } while ($true)
}

function Complete-Batch6AutoFishingGameProcessCleanup {
    param(
        [AllowEmptyString()] [string] $GameDir = '',
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] [string] $Label,
        [ValidateRange(0, 30)] [int] $GracefulWaitSeconds = 10,
        [ValidateRange(0, 30)] [int] $ForceWaitSeconds = 5
    )

    $receiptPath = Join-Path $ReceiptRoot ($Label + '-game-process-cleanup.json')
    $initial = @(Get-Batch6AutoFishingGameProcessSnapshot -GameDir $GameDir)
    $gracefulActions = New-Object System.Collections.ArrayList
    $forceActions = New-Object System.Collections.ArrayList
    foreach ($item in @($initial | Where-Object { [bool]$_.IsTargetGameProcess })) {
        $closed = $false
        [string]$error = ''
        try { $closed = [bool]$item.Process.CloseMainWindow() }
        catch { $error = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message }
        [void]$gracefulActions.Add([ordered]@{ Id=[int]$item.Id; CloseMainWindowReturned=$closed; Error=$error })
    }

    $afterGraceful = @()
    if ($initial.Count -ne 0) {
        $afterGraceful = @(Wait-Batch6AutoFishingGameProcessExit -GameDir $GameDir -Seconds $GracefulWaitSeconds)
    }
    foreach ($item in @($afterGraceful | Where-Object { [bool]$_.IsTargetGameProcess })) {
        [string]$error = ''
        $forced = $false
        try {
            $live = Get-Process -Id ([int]$item.Id) -ErrorAction Stop
            [string]$livePath = [string]$live.Path
            if ([string]::IsNullOrWhiteSpace($livePath) -or -not (Test-DtmApiPathIsSameOrChild -Child $livePath -Parent $GameDir)) {
                throw "PID $($item.Id) no longer has the exact target-game executable path; force termination refused."
            }
            $live | Stop-Process -Force -ErrorAction Stop
            $forced = $true
        }
        catch {
            if ($null -ne (Get-Process -Id ([int]$item.Id) -ErrorAction SilentlyContinue)) {
                $error = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
            }
        }
        [void]$forceActions.Add([ordered]@{ Id=[int]$item.Id; Forced=$forced; Error=$error })
    }

    $remaining = @()
    if ($afterGraceful.Count -ne 0) {
        $remaining = @(Wait-Batch6AutoFishingGameProcessExit -GameDir $GameDir -Seconds $ForceWaitSeconds)
    }
    $passed = $remaining.Count -eq 0
    $forcedAny = @($forceActions | Where-Object { [bool]$_.Forced }).Count -gt 0
    $receipt = [ordered]@{
        SchemaVersion = 1
        Status = if ($passed) { if ($forcedAny) { 'PassedForced' } else { 'Passed' } } else { 'ManualRecoveryRequired' }
        GameDir = $GameDir
        InitialProcesses = ConvertTo-Batch6AutoFishingGameProcessReceiptItems -Items $initial
        GracefulActions = @($gracefulActions.ToArray())
        ForceActions = @($forceActions.ToArray())
        RemainingProcesses = ConvertTo-Batch6AutoFishingGameProcessReceiptItems -Items $remaining
        SharedStateMutationAllowed = $passed
        RuntimeLockReleaseAllowed = $passed
    }
    Write-Batch6AutoFishingJson -Path $receiptPath -Value $receipt
    if (-not $passed) {
        [string]$remainingIds = [string]::Join(',', @($remaining | ForEach-Object { [string]$_.Id }))
        throw "Batch 6 AutoFishing cleanup could not prove every DolocTown.exe process exited (remaining=$remainingIds). No source or deployment state may be changed, and the shared runtime lock must remain held for manual recovery. Close the listed process(es), inspect $receiptPath, and do not force-release the lock while any game process remains."
    }
    return [pscustomobject]$receipt
}

function Get-Batch6AutoFishingJson {
    param([Parameter(Mandatory = $true)] [string] $Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Batch 6 AutoFishing JSON is missing: $Path"
    }
    try {
        return ([System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json)
    }
    catch {
        throw [System.IO.InvalidDataException]::new("Batch 6 AutoFishing JSON is invalid: $Path. $($_.Exception.Message)", $_.Exception)
    }
}

function Assert-Batch6AutoFishingHash {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Actual,
        [Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Expected,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    if ($Actual -notmatch '^[0-9A-Fa-f]{64}$' -or $Expected -notmatch '^[0-9A-Fa-f]{64}$' -or
        -not [string]::Equals($Actual, $Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label SHA-256 mismatch. expected=$Expected; actual=$Actual"
    }
}

function Read-Batch6AutoFishingZipEntryBytes {
    param(
        [Parameter(Mandatory = $true)] $Archive,
        [Parameter(Mandatory = $true)] [string] $EntryName
    )
    $entry = $Archive.GetEntry($EntryName)
    if ($null -eq $entry) {
        throw "Batch 6 AutoFishing package is missing ZIP entry $EntryName."
    }
    $stream = $entry.Open()
    $memory = New-Object System.IO.MemoryStream
    try {
        $stream.CopyTo($memory)
        return $memory.ToArray()
    }
    finally {
        $memory.Dispose()
        $stream.Dispose()
    }
}

function ConvertFrom-Batch6AutoFishingUtf8JsonBytes {
    param(
        [Parameter(Mandatory = $true)] [byte[]] $Bytes,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    try {
        $encoding = New-Object System.Text.UTF8Encoding($false, $true)
        return ($encoding.GetString($Bytes) | ConvertFrom-Json)
    }
    catch {
        throw [System.IO.InvalidDataException]::new("$Label is not strict UTF-8 JSON. $($_.Exception.Message)", $_.Exception)
    }
}

function Get-Batch6AutoFishingArtifactBinding {
    param(
        [Parameter(Mandatory = $true)] [string] $BuildOutputRoot,
        [string] $AuthorSdkOutputRoot = ''
    )

    $root = Get-Batch6AutoFishingCanonicalPath -Path $BuildOutputRoot
    $summaryPath = Join-Path $root 'summary.json'
    $packagePath = Join-Path $root $script:Batch6AutoFishingPackageName
    $sdkRoot = if ([string]::IsNullOrWhiteSpace($AuthorSdkOutputRoot)) {
        Join-Path $root 'author-sdk'
    }
    else {
        Get-Batch6AutoFishingCanonicalPath -Path $AuthorSdkOutputRoot
    }
    $sdkZipPath = Join-Path $sdkRoot $script:Batch6AutoFishingSdkName
    $sdkExePath = Join-Path (Join-Path $sdkRoot 'DTMAPI-Author-SDK-0.1.0-win-x64') 'dtmapi-author.exe'
    foreach ($required in @($summaryPath, $packagePath, $sdkZipPath, $sdkExePath)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "Batch 6 AutoFishing helper output is incomplete: $required"
        }
    }

    $summary = Get-Batch6AutoFishingJson -Path $summaryPath
    if ([int]$summary.schemaVersion -ne 1 -or [string]$summary.status -cne 'Passed' -or
        [string]$summary.uniqueId -cne $script:Batch6AutoFishingUniqueId -or
        [string]$summary.codeModKind -cne 'Advanced' -or
        [string]$summary.policyId -cne $script:Batch6AutoFishingPolicyId -or
        [string]$summary.harmonyOwner -cne $script:Batch6AutoFishingHarmonyOwner -or
        [string]$summary.gameBuildId -cne '23762374' -or
        -not (Test-Batch6AutoFishingPathEquals -Left ([string]$summary.packagePath) -Right $packagePath)) {
        throw 'Batch 6 AutoFishing helper summary drifted from the admitted product/package/policy identity.'
    }
    $packageSha256 = Get-Batch6AutoFishingSha256 -Path $packagePath
    Assert-Batch6AutoFishingHash -Actual $packageSha256 -Expected ([string]$summary.packageSha256) -Label 'AutoFishing helper package'

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        [string[]]$entries = @($archive.Entries | ForEach-Object { [string]$_.FullName })
        foreach ($requiredEntry in @(
            'info.json',
            'Content/DTMAPI/manifest.json',
            ('Content/DTMAPI/' + $script:Batch6AutoFishingEntryDll),
            'Content/DTMAPI/dtmapi-advanced-references.json',
            'Content/DTMAPI/dtmapi-package.json')) {
            if ($entries -cnotcontains $requiredEntry) {
                throw "Batch 6 AutoFishing package is missing exact entry $requiredEntry."
            }
        }
        $dllEntries = @($entries | Where-Object { $_.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase) })
        if ($dllEntries.Count -ne 1 -or $dllEntries[0] -cne ('Content/DTMAPI/' + $script:Batch6AutoFishingEntryDll)) {
            throw ('Batch 6 AutoFishing package must contain only its canonical product DLL; found=' + [string]::Join(',', $dllEntries))
        }
        foreach ($entryName in $entries) {
            $leaf = [System.IO.Path]::GetFileName($entryName)
            if ($leaf -ceq 'Assembly-CSharp.dll' -or $leaf -ceq '0Harmony.dll' -or $leaf -ceq 'BepInEx.dll' -or $leaf -like 'UnityEngine*.dll') {
                throw "Batch 6 AutoFishing package redistributed forbidden native/Runtime dependency $entryName."
            }
        }

        [byte[]]$manifestBytes = Read-Batch6AutoFishingZipEntryBytes -Archive $archive -EntryName 'Content/DTMAPI/manifest.json'
        [byte[]]$entryBytes = Read-Batch6AutoFishingZipEntryBytes -Archive $archive -EntryName ('Content/DTMAPI/' + $script:Batch6AutoFishingEntryDll)
        [byte[]]$referenceBytes = Read-Batch6AutoFishingZipEntryBytes -Archive $archive -EntryName 'Content/DTMAPI/dtmapi-advanced-references.json'
        [byte[]]$markerBytes = Read-Batch6AutoFishingZipEntryBytes -Archive $archive -EntryName 'Content/DTMAPI/dtmapi-package.json'
        $manifest = ConvertFrom-Batch6AutoFishingUtf8JsonBytes -Bytes $manifestBytes -Label 'AutoFishing manifest'
        $reference = ConvertFrom-Batch6AutoFishingUtf8JsonBytes -Bytes $referenceBytes -Label 'AutoFishing Advanced reference receipt'
        $marker = ConvertFrom-Batch6AutoFishingUtf8JsonBytes -Bytes $markerBytes -Label 'AutoFishing package marker'

        $manifestSha256 = Get-Batch6AutoFishingBytesSha256 -Bytes $manifestBytes
        $entrySha256 = Get-Batch6AutoFishingBytesSha256 -Bytes $entryBytes
        $referenceReceiptSha256 = Get-Batch6AutoFishingBytesSha256 -Bytes $referenceBytes
        $markerSha256 = Get-Batch6AutoFishingBytesSha256 -Bytes $markerBytes
        if ([string]$manifest.UniqueID -cne $script:Batch6AutoFishingUniqueId -or
            [string]$manifest.Type -cne 'CodeMod' -or [string]$manifest.CodeModKind -cne 'Advanced' -or
            [string]$manifest.EntryDll -cne ('Content/DTMAPI/' + $script:Batch6AutoFishingEntryDll) -or
            [string]$reference.uniqueId -cne $script:Batch6AutoFishingUniqueId -or
            [string]$reference.referencePolicyId -cne $script:Batch6AutoFishingPolicyId -or
            [string]$reference.harmonyOwner -cne $script:Batch6AutoFishingHarmonyOwner -or
            [string]$marker.uniqueId -cne $script:Batch6AutoFishingUniqueId -or
            [string]$marker.codeModKind -cne 'Advanced' -or
            [string]$marker.authority -cne 'dtmapi-author-sdk-package-binding') {
            throw 'Batch 6 AutoFishing ZIP identity drifted from its helper summary.'
        }
        Assert-Batch6AutoFishingHash -Actual $entrySha256 -Expected ([string]$summary.entryDllSha256) -Label 'AutoFishing entry DLL'
        Assert-Batch6AutoFishingHash -Actual $referenceReceiptSha256 -Expected ([string]$summary.advancedReferenceReceiptSha256) -Label 'AutoFishing Advanced reference receipt'
        Assert-Batch6AutoFishingHash -Actual ([string]$marker.entryDllSha256) -Expected $entrySha256 -Label 'AutoFishing package marker entry DLL'
        Assert-Batch6AutoFishingHash -Actual ([string]$marker.manifestSha256) -Expected $manifestSha256 -Label 'AutoFishing package marker manifest'
        Assert-Batch6AutoFishingHash -Actual ([string]$marker.advancedReferenceReceiptSha256) -Expected $referenceReceiptSha256 -Label 'AutoFishing package marker Advanced receipt'
        [string]$policySha256 = [string]$reference.referencePolicySha256
        if ($policySha256 -notmatch '^[0-9A-Fa-f]{64}$') {
            throw 'Batch 6 AutoFishing Advanced receipt lacks an exact referencePolicySha256.'
        }

        return [pscustomobject]@{
            SchemaVersion = 1
            BuildOutputRoot = $root
            SummaryPath = $summaryPath
            SummarySha256 = Get-Batch6AutoFishingSha256 -Path $summaryPath
            PackagePath = $packagePath
            PackageSha256 = $packageSha256
            SdkZipPath = $sdkZipPath
            SdkZipSha256 = Get-Batch6AutoFishingSha256 -Path $sdkZipPath
            SdkExePath = $sdkExePath
            SdkExeSha256 = Get-Batch6AutoFishingSha256 -Path $sdkExePath
            ManifestSha256 = $manifestSha256
            EntryDllSha256 = $entrySha256
            ReferenceReceiptSha256 = $referenceReceiptSha256
            ReferencePolicySha256 = $policySha256.ToUpperInvariant()
            PackageMarkerSha256 = $markerSha256
            PackageFilePaths = @($archive.Entries | Where-Object { -not [string]::IsNullOrEmpty([string]$_.Name) } | ForEach-Object { [string]$_.FullName } | Sort-Object)
            UniqueId = $script:Batch6AutoFishingUniqueId
            EntryDll = $script:Batch6AutoFishingEntryDll
            PolicyId = $script:Batch6AutoFishingPolicyId
            HarmonyOwner = $script:Batch6AutoFishingHarmonyOwner
            Version = [string]$manifest.Version
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-Batch6AutoFishingGameDestination {
    param([Parameter(Mandatory = $true)] [string] $GameDir)
    $game = Get-Batch6AutoFishingCanonicalPath -Path $GameDir
    $modsRoot = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path $game 'Mods')
    $destination = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path $modsRoot $script:Batch6AutoFishingUniqueId)
    if (-not (Test-Batch6AutoFishingPathEquals -Left (Split-Path -Parent $destination) -Right $modsRoot)) {
        throw "Batch 6 AutoFishing destination escaped the immediate game/Mods root: $destination"
    }
    if ($destination.IndexOf((Join-Path 'BepInEx' 'plugins'), [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Batch 6 AutoFishing cannot be deployed below BepInEx/plugins: $destination"
    }
    $persistentRoot = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_DOLOC_PERSISTENT_ROOT)) {
        Get-Batch6AutoFishingCanonicalPath -Path $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    }
    else {
        Get-Batch6AutoFishingCanonicalPath -Path (Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) 'AppData\LocalLow\RedSawGames\DolocTown')
    }
    $officialLocalRoot = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path $persistentRoot 'MODS')
    if ($destination.StartsWith($officialLocalRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Batch 6 AutoFishing runner cannot write an OfficialLocal/persistent MODS destination: $destination"
    }
    return [pscustomobject]@{
        GameDir = $game
        ModsRoot = $modsRoot
        DestinationPath = $destination
        PersistentRoot = $persistentRoot
        OfficialLocalRoot = $officialLocalRoot
    }
}

function Invoke-Batch6AutoFishingAuthorSdkJson {
    param(
        [Parameter(Mandatory = $true)] [string] $SdkExePath,
        [Parameter(Mandatory = $true)] [string[]] $Arguments,
        [Parameter(Mandatory = $true)] [string] $ReceiptPath,
        [switch] $AllowFailure
    )
    $lines = @(& $SdkExePath @Arguments --json 2>&1)
    $exitCode = $LASTEXITCODE
    $text = [string]::Join([Environment]::NewLine, @($lines | ForEach-Object { [string]$_ }))
    [System.IO.File]::WriteAllText($ReceiptPath, $text + [Environment]::NewLine, $script:Batch6AutoFishingUtf8)
    $report = $null
    try { $report = $text | ConvertFrom-Json }
    catch {
        throw [System.IO.InvalidDataException]::new("Author SDK emitted invalid JSON for $([string]::Join(' ', $Arguments)). Receipt: $ReceiptPath", $_.Exception)
    }
    $success = $exitCode -eq 0 -and [bool]$report.success
    if (-not $success -and -not $AllowFailure) {
        $diagnostics = [string]::Join(' | ', @($report.diagnostics | ForEach-Object { [string]$_.code + ': ' + [string]$_.message }))
        throw "Author SDK command failed closed: $([string]::Join(' ', $Arguments)); exit=$exitCode; diagnostics=$diagnostics; receipt=$ReceiptPath"
    }
    return [pscustomobject]@{
        Success = $success
        ExitCode = $exitCode
        Report = $report
        ReceiptPath = Get-Batch6AutoFishingCanonicalPath -Path $ReceiptPath
        ReceiptSha256 = Get-Batch6AutoFishingSha256 -Path $ReceiptPath
    }
}

function Get-Batch6AutoFishingExpectedJournalPath {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $canonical = Get-Batch6AutoFishingCanonicalPath -Path $GameDir
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($canonical.ToUpperInvariant())
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($bytes)
    }
    finally {
        $sha.Dispose()
    }
    $gameRootKey = ([System.BitConverter]::ToString($hash, 0, 16)).Replace('-', '').ToLowerInvariant()
    $stateBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_AUTHOR_STATE_ROOT)) {
        Get-Batch6AutoFishingCanonicalPath -Path $env:DTMAPI_AUTHOR_STATE_ROOT
    }
    else {
        Get-Batch6AutoFishingCanonicalPath -Path (Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'DTMAPI\AuthorSdk\state')
    }
    return Get-Batch6AutoFishingCanonicalPath -Path (Join-Path (Join-Path (Join-Path (Join-Path $stateBase 'installations') $gameRootKey) 'deployments') ($script:Batch6AutoFishingUniqueId + '.journal.json'))
}

function Get-Batch6AutoFishingDirectoryPresence {
    param([Parameter(Mandatory = $true)] [object] $Paths)

    $hiddenRoot = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path ([string]$Paths.ModsRoot) '.dtmapi-author')
    return [pscustomobject]@{
        HiddenRoot = $hiddenRoot
        HiddenRootExisted = Test-Path -LiteralPath $hiddenRoot -PathType Container
        StagingRoot = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path $hiddenRoot 'staging')
        StagingRootExisted = Test-Path -LiteralPath (Join-Path $hiddenRoot 'staging') -PathType Container
        RecoveryRoot = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path $hiddenRoot 'recovery')
        RecoveryRootExisted = Test-Path -LiteralPath (Join-Path $hiddenRoot 'recovery') -PathType Container
        FailedRoot = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path $hiddenRoot 'failed')
        FailedRootExisted = Test-Path -LiteralPath (Join-Path $hiddenRoot 'failed') -PathType Container
    }
}

function Get-Batch6AutoFishingDeploymentTreeSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $root = Get-Batch6AutoFishingCanonicalPath -Path $Path
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Batch 6 AutoFishing deployment tree is missing: $root"
    }
    $entries = @(Get-ChildItem -LiteralPath $root -Recurse -Force -ErrorAction Stop)
    $rootItem = Get-Item -LiteralPath $root -Force
    if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        @($entries | Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 }).Count -ne 0) {
        throw "Batch 6 AutoFishing deployment trees may not contain reparse points: $root"
    }
    [string[]]$directories = @($entries | Where-Object { $_.PSIsContainer } | ForEach-Object {
        $_.FullName.Substring($root.Length + 1).Replace([char]92, [char]47)
    })
    [string[]]$files = @($entries | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        $_.FullName.Substring($root.Length + 1).Replace([char]92, [char]47)
    })
    [System.Array]::Sort($directories, [System.StringComparer]::Ordinal)
    [System.Array]::Sort($files, [System.StringComparer]::Ordinal)
    $aggregate = New-Object System.IO.MemoryStream
    try {
        foreach ($directory in $directories) {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes("D`0$directory`0")
            $aggregate.Write($bytes, 0, $bytes.Length)
        }
        foreach ($relative in $files) {
            $full = Join-Path $root $relative.Replace([char]47, [char]92)
            $item = Get-Item -LiteralPath $full -Force
            # Author SDK DeploymentTree writes lowercase per-file SHA-256 text
            # into the aggregate. The final tree digest is case-insensitive,
            # but the aggregate input is not.
            $sha256 = (Get-Batch6AutoFishingSha256 -Path $full).ToLowerInvariant()
            $bytes = [System.Text.Encoding]::UTF8.GetBytes("F`0$relative`0$([int64]$item.Length)`0$sha256`0")
            $aggregate.Write($bytes, 0, $bytes.Length)
        }
        $aggregate.Position = 0
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([System.BitConverter]::ToString($sha.ComputeHash($aggregate))).Replace('-', '').ToUpperInvariant()
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $aggregate.Dispose()
    }
}

function Get-Batch6AutoFishingDeploymentState {
    param(
        [Parameter(Mandatory = $true)] [object] $Binding,
        [Parameter(Mandatory = $true)] [object] $Paths,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    $expectedJournalPath = Get-Batch6AutoFishingExpectedJournalPath -GameDir ([string]$Paths.GameDir)
    $journalParentExistedBeforeStatus = Test-Path -LiteralPath (Split-Path -Parent $expectedJournalPath) -PathType Container
    $directoryPresence = Get-Batch6AutoFishingDirectoryPresence -Paths $Paths
    $statusReceipt = Join-Path $ReceiptRoot ($Label + '-deployment-status.json')
    $command = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
        -Arguments @('deployment-status', $script:Batch6AutoFishingUniqueId, '--game-root', [string]$Paths.GameDir) `
        -ReceiptPath $statusReceipt -AllowFailure
    $destinationExists = Test-Path -LiteralPath ([string]$Paths.DestinationPath)
    if (-not $command.Success) {
        $journalExists = Test-Path -LiteralPath $expectedJournalPath
        if ($destinationExists -or $journalExists) {
            throw 'AutoFishing destination or deployment journal exists without a valid exact Author SDK deployment-status receipt.'
        }
        return [pscustomobject]@{
            Status = 'AbsentNoJournal'
            Installed = $false
            DestinationPath = [string]$Paths.DestinationPath
            JournalPath = $expectedJournalPath
            JournalSha256 = ''
            PackageSha256 = ''
            JournalParentExistedBeforeStatus = [bool]$journalParentExistedBeforeStatus
            DirectoryPresence = $directoryPresence
            StatusReceipt = $command
        }
    }
    $report = $command.Report
    if ([string]$report.values.uniqueID -cne $script:Batch6AutoFishingUniqueId -or
        -not (Test-Batch6AutoFishingPathEquals -Left ([string]$report.values.destinationPath) -Right ([string]$Paths.DestinationPath))) {
        throw 'Author SDK deployment-status returned a mismatched UniqueID or destination.'
    }
    [string]$status = [string]$report.values.status
    if ($status -notin @('Installed','Withdrawn')) {
        throw "AutoFishing deployment-status is not terminal Installed/Withdrawn: $status"
    }
    [string]$journalPath = [string]$report.values.journalPath
    if (-not (Test-Batch6AutoFishingPathEquals -Left $journalPath -Right $expectedJournalPath)) {
        throw 'Author SDK deployment-status returned a journal outside the exact installation-scoped path.'
    }
    $journal = Get-Batch6AutoFishingJson -Path $journalPath
    if ([int]$journal.schemaVersion -notin @(2,3) -or [string]$journal.uniqueId -cne $script:Batch6AutoFishingUniqueId -or
        [string]$journal.packageKind -cne 'CodeMod' -or [string]$journal.codeModKind -cne 'Advanced' -or [string]$journal.status -cne $status -or
        ($journal.PSObject.Properties.Name -contains 'localInstall' -and $null -ne $journal.localInstall) -or
        -not (Test-Batch6AutoFishingPathEquals -Left ([string]$journal.destinationPath) -Right ([string]$Paths.DestinationPath))) {
        throw 'AutoFishing deployment journal identity/status/path drifted.'
    }
    $installed = $status -eq 'Installed'
    [string]$committedPackage = ''
    if ($installed) {
        if (-not $destinationExists -or $null -eq $journal.committed) {
            throw 'Installed AutoFishing journal does not have its exact destination and committed record.'
        }
        $localReceiptPath = Join-Path ([string]$Paths.DestinationPath) '.dtmapi-author-receipt.json'
        $localReceipt = Get-Batch6AutoFishingJson -Path $localReceiptPath
        $committedPackage = [string]$journal.committed.packageSha256
        foreach ($pair in @(
            @('package', $committedPackage, [string]$Binding.PackageSha256),
            @('local package', [string]$localReceipt.packageSha256, [string]$Binding.PackageSha256),
            @('local entry', [string]$localReceipt.entryDllSha256, [string]$Binding.EntryDllSha256),
            @('local manifest', [string]$localReceipt.manifestSha256, [string]$Binding.ManifestSha256),
            @('local policy receipt', [string]$localReceipt.advancedReferenceReceiptSha256, [string]$Binding.ReferenceReceiptSha256))) {
            Assert-Batch6AutoFishingHash -Actual ([string]$pair[1]) -Expected ([string]$pair[2]) -Label ('AutoFishing deployment ' + [string]$pair[0])
        }
    }
    elseif ($destinationExists -or $null -ne $journal.committed) {
        throw 'Withdrawn AutoFishing journal retained a destination or committed record.'
    }
    return [pscustomobject]@{
        Status = $status
        Installed = $installed
        DestinationPath = [string]$Paths.DestinationPath
        JournalPath = Get-Batch6AutoFishingCanonicalPath -Path $journalPath
        JournalSha256 = Get-Batch6AutoFishingSha256 -Path $journalPath
        PackageSha256 = $committedPackage
        JournalParentExistedBeforeStatus = [bool]$journalParentExistedBeforeStatus
        DirectoryPresence = $directoryPresence
        StatusReceipt = $command
    }
}

function Invoke-Batch6AutoFishingDeploymentOperation {
    param(
        [Parameter(Mandatory = $true)] [ValidateSet('deploy','update','withdraw')] [string] $Operation,
        [Parameter(Mandatory = $true)] [object] $Binding,
        [Parameter(Mandatory = $true)] [object] $Paths,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    $arguments = if ($Operation -eq 'withdraw') {
        @('withdraw', $script:Batch6AutoFishingUniqueId, '--game-root', [string]$Paths.GameDir)
    }
    else {
        @($Operation, [string]$Binding.PackagePath, '--game-root', [string]$Paths.GameDir)
    }
    $operationReceipt = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) -Arguments $arguments `
        -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-' + $Operation + '.json'))
    $state = Get-Batch6AutoFishingDeploymentState -Binding $Binding -Paths $Paths -ReceiptRoot $ReceiptRoot -Label ($Label + '-after-' + $Operation)
    $expectedInstalled = $Operation -ne 'withdraw'
    if ([bool]$state.Installed -ne $expectedInstalled) {
        throw "Author SDK $Operation did not reach the exact expected AutoFishing deployment state."
    }
    return [pscustomobject]@{ Operation = $Operation; Command = $operationReceipt; State = $state }
}

function Remove-Batch6AutoFishingRunCreatedWithdrawnState {
    param(
        [Parameter(Mandatory = $true)] [object] $InitialState,
        [Parameter(Mandatory = $true)] [object] $CurrentState,
        [Parameter(Mandatory = $true)] [object] $Binding,
        [Parameter(Mandatory = $true)] [object] $Paths,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(DontShow = $true)] [scriptblock] $FinalStateResolverForTests
    )

    if ([string]$InitialState.Status -cne 'AbsentNoJournal' -or [bool]$InitialState.Installed -or
        [string]$CurrentState.Status -cne 'Withdrawn' -or [bool]$CurrentState.Installed) {
        throw 'Run-created journal cleanup is authorized only for an initial AbsentNoJournal state and a terminal Withdrawn state.'
    }
    if (Test-Path -LiteralPath ([string]$Paths.DestinationPath)) {
        throw 'Run-created journal cleanup refuses an extant AutoFishing destination.'
    }
    if (-not (Test-Batch6AutoFishingPathEquals -Left ([string]$CurrentState.JournalPath) -Right ([string]$InitialState.JournalPath)) -or
        -not (Test-Path -LiteralPath ([string]$CurrentState.JournalPath) -PathType Leaf)) {
        throw 'Run-created journal cleanup found a missing or substituted journal path.'
    }
    Assert-Batch6AutoFishingHash -Actual (Get-Batch6AutoFishingSha256 -Path ([string]$CurrentState.JournalPath)) `
        -Expected ([string]$CurrentState.JournalSha256) -Label 'run-created withdrawn journal'
    $journal = Get-Batch6AutoFishingJson -Path ([string]$CurrentState.JournalPath)
    if ([int]$journal.schemaVersion -notin @(2,3) -or [string]$journal.uniqueId -cne $script:Batch6AutoFishingUniqueId -or
        [string]$journal.packageKind -cne 'CodeMod' -or [string]$journal.codeModKind -cne 'Advanced' -or
        [string]$journal.status -cne 'Withdrawn' -or $null -ne $journal.active -or $null -ne $journal.committed -or
        ($journal.PSObject.Properties.Name -contains 'localInstall' -and $null -ne $journal.localInstall) -or
        -not (Test-Batch6AutoFishingPathEquals -Left ([string]$journal.gameRoot) -Right ([string]$Paths.GameDir)) -or
        -not (Test-Batch6AutoFishingPathEquals -Left ([string]$journal.destinationPath) -Right ([string]$Paths.DestinationPath))) {
        throw 'Run-created withdrawn journal identity/root/destination/status is not exact.'
    }
    $artifacts = @($journal.recoveryArtifacts)
    if ($artifacts.Count -lt 1) {
        throw 'Run-created withdrawn journal has no receipt-bound recovery artifact to prove the package removed by this run.'
    }
    $recoveryRoot = Get-Batch6AutoFishingCanonicalPath -Path (Join-Path ([string]$InitialState.DirectoryPresence.HiddenRoot) 'recovery')
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $verifiedArtifacts = New-Object System.Collections.ArrayList
    [string[]]$expectedFiles = @($Binding.PackageFilePaths + '.dtmapi-author-receipt.json')
    [System.Array]::Sort($expectedFiles, [System.StringComparer]::Ordinal)
    foreach ($artifact in $artifacts) {
        [string]$role = [string]$artifact.role
        [string]$artifactPath = Get-Batch6AutoFishingCanonicalPath -Path ([string]$artifact.path)
        if ($role -notin @('previous-version','withdrawn-package') -or
            -not (Test-Batch6AutoFishingPathEquals -Left (Split-Path -Parent $artifactPath) -Right $recoveryRoot) -or
            -not $seen.Add($artifactPath) -or -not (Test-Path -LiteralPath $artifactPath -PathType Container)) {
            throw 'Run-created withdrawn journal contains an unexpected, duplicate, or missing recovery artifact.'
        }
        [string[]]$actualFiles = @(Get-ChildItem -LiteralPath $artifactPath -Recurse -Force -File | ForEach-Object {
            $_.FullName.Substring($artifactPath.Length + 1).Replace([char]92, [char]47)
        })
        [System.Array]::Sort($actualFiles, [System.StringComparer]::Ordinal)
        if (($actualFiles -join "`n") -cne ($expectedFiles -join "`n")) {
            throw "Run-created recovery artifact file inventory drifted: $artifactPath"
        }
        Assert-Batch6AutoFishingHash -Actual (Get-Batch6AutoFishingDeploymentTreeSha256 -Path $artifactPath) `
            -Expected ([string]$artifact.treeSha256) -Label 'run-created recovery tree'
        $localReceipt = Get-Batch6AutoFishingJson -Path (Join-Path $artifactPath '.dtmapi-author-receipt.json')
        if ([int]$localReceipt.schemaVersion -ne 2 -or [string]$localReceipt.uniqueId -cne $script:Batch6AutoFishingUniqueId -or
            [string]$localReceipt.packageKind -cne 'CodeMod' -or [string]$localReceipt.codeModKind -cne 'Advanced' -or
            [string]$localReceipt.destinationRelativePath -cne ('Mods/' + $script:Batch6AutoFishingUniqueId)) {
            throw 'Run-created recovery artifact local receipt identity drifted.'
        }
        foreach ($pair in @(
            @('package', [string]$localReceipt.packageSha256, [string]$Binding.PackageSha256),
            @('entry', [string]$localReceipt.entryDllSha256, [string]$Binding.EntryDllSha256),
            @('manifest', [string]$localReceipt.manifestSha256, [string]$Binding.ManifestSha256),
            @('Advanced receipt', [string]$localReceipt.advancedReferenceReceiptSha256, [string]$Binding.ReferenceReceiptSha256),
            @('package marker', [string]$localReceipt.packageMarkerSha256, [string]$Binding.PackageMarkerSha256))) {
            Assert-Batch6AutoFishingHash -Actual ([string]$pair[1]) -Expected ([string]$pair[2]) -Label ('run-created artifact ' + [string]$pair[0])
        }
        $referencePath = Join-Path $artifactPath 'Content\DTMAPI\dtmapi-advanced-references.json'
        Assert-Batch6AutoFishingHash -Actual (Get-Batch6AutoFishingSha256 -Path $referencePath) `
            -Expected ([string]$Binding.ReferenceReceiptSha256) -Label 'run-created artifact Advanced receipt file'
        $reference = Get-Batch6AutoFishingJson -Path $referencePath
        Assert-Batch6AutoFishingHash -Actual ([string]$reference.referencePolicySha256) `
            -Expected ([string]$Binding.ReferencePolicySha256) -Label 'run-created artifact reference policy'
        [void]$verifiedArtifacts.Add([ordered]@{ Role = $role; Path = $artifactPath; TreeSha256 = ([string]$artifact.treeSha256).ToUpperInvariant() })
    }

    foreach ($artifact in $verifiedArtifacts) {
        Remove-Item -LiteralPath ([string]$artifact.Path) -Recurse -Force
    }
    Remove-Item -LiteralPath ([string]$CurrentState.JournalPath) -Force

    foreach ($candidate in @(
        [ordered]@{ Path = [string]$InitialState.DirectoryPresence.RecoveryRoot; Existed = [bool]$InitialState.DirectoryPresence.RecoveryRootExisted },
        [ordered]@{ Path = [string]$InitialState.DirectoryPresence.StagingRoot; Existed = [bool]$InitialState.DirectoryPresence.StagingRootExisted },
        [ordered]@{ Path = [string]$InitialState.DirectoryPresence.FailedRoot; Existed = [bool]$InitialState.DirectoryPresence.FailedRootExisted },
        [ordered]@{ Path = [string]$InitialState.DirectoryPresence.HiddenRoot; Existed = [bool]$InitialState.DirectoryPresence.HiddenRootExisted })) {
        if (-not $candidate.Existed -and (Test-Path -LiteralPath $candidate.Path -PathType Container) -and
            @(Get-ChildItem -LiteralPath $candidate.Path -Force).Count -eq 0) {
            Remove-Item -LiteralPath $candidate.Path -Force
        }
    }
    $journalParent = Split-Path -Parent ([string]$InitialState.JournalPath)
    if (-not [bool]$InitialState.JournalParentExistedBeforeStatus -and (Test-Path -LiteralPath $journalParent -PathType Container) -and
        @(Get-ChildItem -LiteralPath $journalParent -Force).Count -eq 0) {
        Remove-Item -LiteralPath $journalParent -Force
    }
    $final = if ($null -ne $FinalStateResolverForTests) {
        & $FinalStateResolverForTests
    }
    else {
        Get-Batch6AutoFishingDeploymentState -Binding $Binding -Paths $Paths -ReceiptRoot $ReceiptRoot -Label ($Label + '-verify')
    }
    if ([string]$final.Status -cne 'AbsentNoJournal' -or [bool]$final.Installed -or (Test-Path -LiteralPath ([string]$Paths.DestinationPath))) {
        throw 'Run-created journal cleanup did not restore AbsentNoJournal exactly.'
    }
    $receipt = [pscustomobject]@{
        SchemaVersion = 1
        Status = 'Passed'
        InitialStatus = [string]$InitialState.Status
        FinalStatus = [string]$final.Status
        RemovedJournalPath = [string]$CurrentState.JournalPath
        RemovedJournalSha256 = [string]$CurrentState.JournalSha256
        RemovedArtifacts = @($verifiedArtifacts.ToArray())
        DestinationAbsent = $true
    }
    Write-Batch6AutoFishingJson -Path (Join-Path $ReceiptRoot ($Label + '-absent-no-journal-restore.json')) -Value $receipt
    return $receipt
}

function Start-Batch6AutoFishingSourceTransaction {
    param(
        [Parameter(Mandatory = $true)] [ValidateSet('Absent','LocalDevelopment')] [string] $TargetMode,
        [Parameter(Mandatory = $true)] [object] $Binding,
        [Parameter(Mandatory = $true)] [object] $Paths,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    $before = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
        -Arguments @('source','status',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
        -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-before.json'))
    if ([string]$before.Report.values.playerReproductionActive -cne 'false') {
        throw 'Batch 6 AutoFishing source transaction refuses active Player Reproduction state.'
    }
    [string]$beforeMode = [string]$before.Report.values.mode
    if ($beforeMode -notin @('PlayerWorkshop','LocalDevelopment','WorkshopValidation')) {
        throw "Unsupported pre-stage AutoFishing source mode: $beforeMode"
    }
    $summary = [ordered]@{
        SchemaVersion = 1
        TargetMode = $TargetMode
        BeforeMode = $beforeMode
        BeforeSourcePath = Get-Batch6AutoFishingOptionalStringProperty -Value $before.Report.values -Name 'sourcePath'
        BeforeTreeSha256 = Get-Batch6AutoFishingOptionalStringProperty -Value $before.Report.values -Name 'expectedTreeSha256'
        MutationStarted = $false
        Applied = $false
        Restored = $false
        BeforeReceipt = $before
    }
    $script:Batch6AutoFishingPendingSourceTransaction = [pscustomobject]$summary
    try {
        if ($TargetMode -eq 'Absent') {
            if ($beforeMode -eq 'LocalDevelopment') {
                $summary.MutationStarted = $true
                $script:Batch6AutoFishingPendingSourceTransaction = [pscustomobject]$summary
                $null = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
                    -Arguments @('source','local','clear',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
                    -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-apply-clear-local.json'))
            }
            elseif ($beforeMode -eq 'WorkshopValidation') {
                $summary.MutationStarted = $true
                $script:Batch6AutoFishingPendingSourceTransaction = [pscustomobject]$summary
                $null = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
                    -Arguments @('source','workshop','clear',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
                    -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-apply-clear-workshop.json'))
            }
        }
        else {
            if (-not (Test-Path -LiteralPath ([string]$Paths.DestinationPath) -PathType Container)) {
                throw 'Batch 6 AutoFishing LocalDevelopment source target is absent.'
            }
            $summary.MutationStarted = $true
            $script:Batch6AutoFishingPendingSourceTransaction = [pscustomobject]$summary
            $null = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
                -Arguments @('source','local','select',$script:Batch6AutoFishingUniqueId,[string]$Paths.DestinationPath,'--game-root',[string]$Paths.GameDir) `
                -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-apply-select-local.json'))
        }
        $after = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
            -Arguments @('source','status',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
            -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-after-apply.json'))
        [string]$expectedMode = if ($TargetMode -eq 'Absent') { 'PlayerWorkshop' } else { 'LocalDevelopment' }
        if ([string]$after.Report.values.mode -cne $expectedMode) {
            throw "Batch 6 AutoFishing source transaction did not reach $expectedMode."
        }
        if ($TargetMode -eq 'LocalDevelopment') {
            [string]$appliedSourcePath = Get-Batch6AutoFishingOptionalStringProperty -Value $after.Report.values -Name 'sourcePath'
            [string]$appliedTreeSha256 = Get-Batch6AutoFishingOptionalStringProperty -Value $after.Report.values -Name 'expectedTreeSha256'
            if ([string]::IsNullOrWhiteSpace($appliedSourcePath) -or
                -not (Test-Batch6AutoFishingPathEquals -Left $appliedSourcePath -Right ([string]$Paths.DestinationPath)) -or
                $appliedTreeSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
                throw 'Batch 6 AutoFishing source local select did not bind the exact game/Mods tree.'
            }
            $summary['AppliedTreeSha256'] = $appliedTreeSha256.ToUpperInvariant()
        }
        $summary.Applied = $true
        $summary['AfterApplyReceipt'] = $after
        $script:Batch6AutoFishingPendingSourceTransaction = $null
        return [pscustomobject]$summary
    }
    catch {
        $originalError = $_
        if ([bool]$summary.MutationStarted) {
            try {
                $rollback = Complete-Batch6AutoFishingSourceTransaction -Summary ([pscustomobject]$summary) `
                    -Binding $Binding -Paths $Paths -ReceiptRoot $ReceiptRoot -Label ($Label + '-apply-failure')
                $summary.Restored = $true
                $summary['RollbackReceipt'] = $rollback
                Write-Batch6AutoFishingJson -Path (Join-Path $ReceiptRoot ($Label + '-source-apply-rollback.json')) `
                    -Value ([ordered]@{ SchemaVersion=1; Passed=$true; OriginalError=[string]$originalError.Exception.Message; Transaction=$summary })
            }
            catch {
                $rollbackError = $_
                Write-Batch6AutoFishingJson -Path (Join-Path $ReceiptRoot ($Label + '-source-apply-rollback.json')) `
                    -Value ([ordered]@{ SchemaVersion=1; Passed=$false; OriginalError=[string]$originalError.Exception.Message; RollbackError=[string]$rollbackError.Exception.Message; Transaction=$summary })
                $script:Batch6AutoFishingPendingSourceTransaction = [pscustomobject]$summary
                throw [System.InvalidOperationException]::new(
                    "Batch 6 AutoFishing source apply failed and exact rollback also failed. Apply: $($originalError.Exception.Message) Rollback: $($rollbackError.Exception.Message)",
                    $rollbackError.Exception)
            }
        }
        $script:Batch6AutoFishingPendingSourceTransaction = $null
        throw $originalError
    }
}

function Complete-Batch6AutoFishingSourceTransaction {
    param(
        [Parameter(Mandatory = $true)] [object] $Summary,
        [Parameter(Mandatory = $true)] [object] $Binding,
        [Parameter(Mandatory = $true)] [object] $Paths,
        [Parameter(Mandatory = $true)] [string] $ReceiptRoot,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    if (-not [bool]$Summary.MutationStarted -and -not [bool]$Summary.Applied) {
        throw 'Batch 6 AutoFishing cannot restore a source transaction that never started a mutation.'
    }
    $current = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
        -Arguments @('source','status',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
        -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-before-restore.json'))
    [string]$currentMode = [string]$current.Report.values.mode
    if ($currentMode -eq 'LocalDevelopment') {
        $null = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
            -Arguments @('source','local','clear',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
            -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-restore-clear-local.json'))
    }
    elseif ($currentMode -eq 'WorkshopValidation') {
        $null = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
            -Arguments @('source','workshop','clear',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
            -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-restore-clear-workshop.json'))
    }
    elseif ($currentMode -ne 'PlayerWorkshop') {
        throw "Batch 6 AutoFishing source restore found unsupported current mode $currentMode."
    }

    if ([string]$Summary.BeforeMode -eq 'LocalDevelopment') {
        if (-not (Test-Path -LiteralPath ([string]$Summary.BeforeSourcePath) -PathType Container)) {
            throw 'The pre-stage LocalDevelopment source disappeared; exact source-state restore is blocked.'
        }
        $null = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
            -Arguments @('source','local','select',$script:Batch6AutoFishingUniqueId,[string]$Summary.BeforeSourcePath,'--game-root',[string]$Paths.GameDir) `
            -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-restore-select-local.json'))
    }
    elseif ([string]$Summary.BeforeMode -eq 'WorkshopValidation') {
        $null = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
            -Arguments @('source','workshop','prepare',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
            -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-restore-workshop.json'))
    }
    $after = Invoke-Batch6AutoFishingAuthorSdkJson -SdkExePath ([string]$Binding.SdkExePath) `
        -Arguments @('source','status',$script:Batch6AutoFishingUniqueId,'--game-root',[string]$Paths.GameDir) `
        -ReceiptPath (Join-Path $ReceiptRoot ($Label + '-source-after-restore.json'))
    $restored = [string]$after.Report.values.mode -ceq [string]$Summary.BeforeMode
    [string]$restoredSourcePath = Get-Batch6AutoFishingOptionalStringProperty -Value $after.Report.values -Name 'sourcePath'
    [string]$beforeSourcePath = [string]$Summary.BeforeSourcePath
    [string]$restoredTreeSha256 = Get-Batch6AutoFishingOptionalStringProperty -Value $after.Report.values -Name 'expectedTreeSha256'
    [string]$beforeTreeSha256 = [string]$Summary.BeforeTreeSha256
    if ($restored) {
        $sourcePathMatches = if ([string]::IsNullOrWhiteSpace($beforeSourcePath)) {
            [string]::IsNullOrWhiteSpace($restoredSourcePath)
        }
        else {
            -not [string]::IsNullOrWhiteSpace($restoredSourcePath) -and
                (Test-Batch6AutoFishingPathEquals -Left $restoredSourcePath -Right $beforeSourcePath)
        }
        $treeMatches = [string]::Equals($restoredTreeSha256, $beforeTreeSha256, [System.StringComparison]::OrdinalIgnoreCase)
        $restored = $sourcePathMatches -and $treeMatches
    }
    if (-not $restored) {
        throw 'Batch 6 AutoFishing source transaction did not restore the exact pre-stage source selection.'
    }
    return [pscustomobject]@{
        SchemaVersion = 1
        Passed = $true
        RestoredMode = [string]$after.Report.values.mode
        RestoredSourcePath = $restoredSourcePath
        RestoredTreeSha256 = $restoredTreeSha256
        Receipt = $after
    }
}
