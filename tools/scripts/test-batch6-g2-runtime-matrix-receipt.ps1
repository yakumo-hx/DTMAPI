param(
    [string] $ReceiptPath = '',
    [switch] $VerifyRawEvidence,
    [string] $EvidenceMapPath = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
# The validator deliberately accumulates independent shape failures from malformed
# receipts; missing JSON properties are handled as null by the assertions below.
Set-StrictMode -Off
$repo = Get-RepoRoot
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure([string] $message) {
    $script:failures.Add($message)
}

function Normalize-Path([string] $path) {
    return ([string]$path).Trim().TrimStart([char]'\', [char]'/').Replace('\', '/').TrimEnd('/')
}

function Resolve-File([string] $value, [string] $defaultRelativePath) {
    $candidate = if ([string]::IsNullOrWhiteSpace($value)) { $defaultRelativePath } else { $value }
    if (-not [System.IO.Path]::IsPathRooted($candidate)) { $candidate = Join-Path $repo $candidate }
    return [System.IO.Path]::GetFullPath($candidate)
}

function Read-Utf8([string] $path) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Failure "Required file is missing: $path"
        return ''
    }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function ConvertFrom-ReceiptJson([string] $text) {
    $command = Get-Command ConvertFrom-Json
    if ($command.Parameters.ContainsKey('DateKind')) {
        return ConvertFrom-Json -InputObject $text -DateKind String
    }
    return ConvertFrom-Json -InputObject $text
}

function Get-OptionalProperty([object] $value, [string] $name) {
    if ($null -eq $value) { return $null }
    $property = $value.PSObject.Properties[$name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Assert-ExactFields([string] $label, [object] $value, [string[]] $expected) {
    if ($null -eq $value) {
        Add-Failure "$label must be an object."
        return
    }
    $actual = @($value.PSObject.Properties | ForEach-Object { [string]$_.Name })
    Assert-ExactSet "$label fields" $actual $expected
}

function Sort-Ordinal([object[]] $values) {
    [string[]]$result = @($values | ForEach-Object { [string]$_ })
    [Array]::Sort($result, [StringComparer]::Ordinal)
    return $result
}

function Assert-ExactSet([string] $label, [object[]] $actual, [object[]] $expected) {
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $expectedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($item in @($actual)) {
        if (-not $actualSet.Add([string]$item)) { Add-Failure "$label contains duplicate '$item'." }
    }
    foreach ($item in @($expected)) {
        if (-not $expectedSet.Add([string]$item)) { throw "$label checker expectation contains duplicate '$item'." }
    }
    $missing = @(Sort-Ordinal @($expectedSet | Where-Object { -not $actualSet.Contains([string]$_) }))
    $extra = @(Sort-Ordinal @($actualSet | Where-Object { -not $expectedSet.Contains([string]$_) }))
    if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
        Add-Failure "$label mismatch. Missing=[$($missing -join ', ')] Extra=[$($extra -join ', ')]."
    }
}

function Assert-ExactSequence([string] $label, [object[]] $actual, [object[]] $expected) {
    $actualItems = @($actual | ForEach-Object { [string]$_ })
    $expectedItems = @($expected | ForEach-Object { [string]$_ })
    if ($actualItems.Count -ne $expectedItems.Count) {
        Add-Failure "$label count mismatch. Expected=$($expectedItems.Count) Actual=$($actualItems.Count)."
        return
    }
    for ($index = 0; $index -lt $expectedItems.Count; $index++) {
        if (-not $actualItems[$index].Equals($expectedItems[$index], [StringComparison]::Ordinal)) {
            Add-Failure "$label mismatch at index $index. Expected='$($expectedItems[$index])' Actual='$($actualItems[$index])'."
        }
    }
}

function Assert-String([string] $label, [object] $actual, [string] $expected) {
    if ($null -eq $actual -or -not ([string]$actual).Equals($expected, [StringComparison]::Ordinal)) {
        Add-Failure "$label must be '$expected'; found '$actual'."
    }
}

function Assert-NonEmptyString([string] $label, [object] $actual) {
    if ($null -eq $actual -or -not ($actual -is [string]) -or [string]::IsNullOrWhiteSpace([string]$actual)) {
        Add-Failure "$label must be a non-empty string."
    }
}

function Assert-Boolean([string] $label, [object] $actual, [bool] $expected) {
    if ($null -eq $actual -or -not ($actual -is [bool]) -or [bool]$actual -ne $expected) {
        Add-Failure "$label must be $expected; found '$actual'."
    }
}

function Assert-Integer([string] $label, [object] $actual, [long] $expected) {
    if ($null -eq $actual -or -not ($actual -is [byte] -or $actual -is [int16] -or $actual -is [int32] -or $actual -is [int64]) -or [long]$actual -ne $expected) {
        Add-Failure "$label must be integer $expected; found '$actual'."
    }
}

function Assert-NonNegativeInteger([string] $label, [object] $actual) {
    if ($null -eq $actual -or -not ($actual -is [byte] -or $actual -is [int16] -or $actual -is [int32] -or $actual -is [int64]) -or [long]$actual -lt 0) {
        Add-Failure "$label must be a non-negative integer; found '$actual'."
    }
}

function Assert-Sha256([string] $label, [object] $actual) {
    if ($null -eq $actual -or -not ([string]$actual -cmatch '^[0-9A-F]{64}$')) {
        Add-Failure "$label must be an uppercase SHA-256 value; found '$actual'."
    }
}

function ConvertTo-UtcInstant([string] $label, [object] $value) {
    if ($null -eq $value) {
        Add-Failure "$label is not a valid timestamp: '$value'."
        return $null
    }
    try {
        $parsed = [DateTimeOffset]::Parse([string]$value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
        return $parsed.ToUniversalTime()
    }
    catch {
        Add-Failure "$label is not a valid timestamp: '$value'."
        return $null
    }
}

function Invoke-GitLines([string[]] $arguments) {
    $previousErrorAction = $ErrorActionPreference
    $previousOutputEncoding = [Console]::OutputEncoding
    try {
        $ErrorActionPreference = 'Continue'
        [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
        $output = @(& git -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 2>$null)
        $exitCode = $LASTEXITCODE
    }
    finally {
        [Console]::OutputEncoding = $previousOutputEncoding
        $ErrorActionPreference = $previousErrorAction
    }
    if ($exitCode -ne 0) {
        throw "git $($arguments -join ' ') failed."
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Test-Git([string[]] $arguments) {
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & git -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 1>$null 2>$null
        return $LASTEXITCODE -eq 0
    }
    finally { $ErrorActionPreference = $previousErrorAction }
}

function ConvertTo-CanonicalJson([object] $value) {
    if ($null -eq $value) { return 'null' }
    if ($value -is [string] -or $value -is [char]) {
        return (ConvertTo-Json -InputObject ([string]$value) -Compress)
    }
    if ($value -is [bool]) { if ([bool]$value) { return 'true' } else { return 'false' } }
    if ($value -is [System.Collections.IDictionary]) {
        [string[]]$keys = @($value.Keys | ForEach-Object { [string]$_ })
        [Array]::Sort($keys, [StringComparer]::Ordinal)
        $parts = New-Object System.Collections.Generic.List[string]
        foreach ($key in $keys) {
            $parts.Add((ConvertTo-CanonicalJson $key) + ':' + (ConvertTo-CanonicalJson $value[$key]))
        }
        return '{' + ($parts -join ',') + '}'
    }
    if ($value -is [System.Collections.IEnumerable] -and -not ($value -is [string])) {
        $parts = New-Object System.Collections.Generic.List[string]
        foreach ($item in $value) { $parts.Add((ConvertTo-CanonicalJson $item)) }
        return '[' + ($parts -join ',') + ']'
    }
    $properties = @($value.PSObject.Properties | Where-Object { $_.MemberType -in @('NoteProperty', 'Property') })
    if ($properties.Count -gt 0 -and -not ($value -is [ValueType])) {
        [string[]]$names = @($properties | ForEach-Object { [string]$_.Name })
        [Array]::Sort($names, [StringComparer]::Ordinal)
        $parts = New-Object System.Collections.Generic.List[string]
        foreach ($name in $names) {
            $parts.Add((ConvertTo-CanonicalJson $name) + ':' + (ConvertTo-CanonicalJson $value.PSObject.Properties[$name].Value))
        }
        return '{' + ($parts -join ',') + '}'
    }
    if ($value -is [IFormattable]) { return ([IFormattable]$value).ToString($null, [Globalization.CultureInfo]::InvariantCulture) }
    return (ConvertTo-Json -InputObject $value -Compress)
}

function Get-TextSha256([string] $text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally { $sha.Dispose() }
}

function Assert-FileRow([string] $label, [object] $row, [bool] $allowNull) {
    if ($null -eq $row) {
        if (-not $allowNull) { Add-Failure "$label must not be null." }
        return
    }
    Assert-ExactFields $label $row @('path', 'length', 'sha256')
    Assert-NonEmptyString "$label path" $row.path
    Assert-NonNegativeInteger "$label length" $row.length
    Assert-Sha256 "$label sha256" $row.sha256
}

function Assert-Nullable([string] $label, [object] $actual) {
    if ($null -ne $actual) { Add-Failure "$label must be null for this case; found '$actual'." }
}

function Assert-CommonObservations([string] $caseId, [object] $common, [string] $expectedRunStatus, [object[]] $expectedFailureFields) {
    $label = "$caseId observations.common"
    Assert-ExactFields $label $common @(
        'resultSchemaVersion',
        'resultRunStatus',
        'failedResultFields',
        'saveLoaded',
        'hookProbe',
        'gameLaunched',
        'processExited',
        'noFatalInstanceWindow',
        'forcedClose',
        'qaHostCleanup',
        'qaSaveLoadedObservation',
        'officialModProfileRestored',
        'localAuthorSourceStateRestored',
        'local11AuthorSourceStateRestored',
        'isolateAllOfficialMods',
        'officialModProfileApplied',
        'summarySaveSlot',
        'summaryIncludeHookProbe',
        'summaryLaunchMode',
        'qaCleanupExactArtifacts',
        'qaCleanupExactQaEvidence',
        'qaCleanupExactTree',
        'qaCleanupCleaned',
        'qaCleanupRetained',
        'profileRestoreExactLength',
        'profileRestoreExactSha256',
        'profileRestorePassed',
        'processAbsent',
        'reportRuntimeLogIsRunPrefix',
        'reportStartedAtUtc',
        'reportGeneratedAtUtc',
        'playerDoctorRuntimeAssemblyHashMatch',
        'playerDoctorErrorCount',
        'playerDoctorWarningCount',
        'playerDoctorMisplacedCount'
    )
    Assert-Integer "$label resultSchemaVersion" $common.resultSchemaVersion 2
    Assert-String "$label resultRunStatus" $common.resultRunStatus $expectedRunStatus
    Assert-ExactSet "$label failedResultFields" @($common.failedResultFields) @($expectedFailureFields)
    foreach ($name in @('saveLoaded', 'hookProbe', 'gameLaunched', 'processExited', 'noFatalInstanceWindow', 'forcedClose', 'qaHostCleanup', 'qaSaveLoadedObservation')) {
        Assert-String "$label $name" (Get-OptionalProperty $common $name) 'Passed'
    }
    Assert-Boolean "$label officialModProfileRestored" $common.officialModProfileRestored $true
    foreach ($name in @('localAuthorSourceStateRestored', 'local11AuthorSourceStateRestored', 'isolateAllOfficialMods', 'officialModProfileApplied')) {
        Assert-Boolean "$label $name" (Get-OptionalProperty $common $name) $true
    }
    Assert-Integer "$label summarySaveSlot" $common.summarySaveSlot 3
    Assert-Boolean "$label summaryIncludeHookProbe" $common.summaryIncludeHookProbe $true
    Assert-String "$label summaryLaunchMode" $common.summaryLaunchMode 'Steam'
    foreach ($name in @('qaCleanupExactArtifacts', 'qaCleanupExactQaEvidence', 'qaCleanupExactTree', 'qaCleanupCleaned', 'profileRestoreExactLength', 'profileRestoreExactSha256', 'profileRestorePassed')) {
        Assert-Boolean "$label $name" (Get-OptionalProperty $common $name) $true
    }
    Assert-Boolean "$label qaCleanupRetained" $common.qaCleanupRetained $false
    Assert-Boolean "$label processAbsent" $common.processAbsent $true
    Assert-Boolean "$label reportRuntimeLogIsRunPrefix" $common.reportRuntimeLogIsRunPrefix $true
    [void](ConvertTo-UtcInstant "$label reportStartedAtUtc" $common.reportStartedAtUtc)
    [void](ConvertTo-UtcInstant "$label reportGeneratedAtUtc" $common.reportGeneratedAtUtc)
    Assert-Boolean "$label playerDoctorRuntimeAssemblyHashMatch" $common.playerDoctorRuntimeAssemblyHashMatch $true
    foreach ($name in @('playerDoctorErrorCount', 'playerDoctorWarningCount', 'playerDoctorMisplacedCount')) {
        Assert-Integer "$label $name" (Get-OptionalProperty $common $name) 0
    }
}

function Assert-StrictSiblingObservations([string] $caseId, [object] $strictSibling) {
    $label = "$caseId observations.strictSibling"
    Assert-ExactFields $label $strictSibling @('entryCount', 'gameLaunchedCount', 'oneSecondCount', 'saveLoadedSlot', 'saveLoadedCount', 'reportStatusCode', 'reportLoaded')
    Assert-Integer "$label entryCount" $strictSibling.entryCount 1
    Assert-Integer "$label gameLaunchedCount" $strictSibling.gameLaunchedCount 1
    Assert-NonNegativeInteger "$label oneSecondCount" $strictSibling.oneSecondCount
    if ($null -ne $strictSibling.oneSecondCount -and [long]$strictSibling.oneSecondCount -lt 1) { Add-Failure "$label oneSecondCount must be at least 1." }
    Assert-Integer "$label saveLoadedSlot" $strictSibling.saveLoadedSlot 2
    Assert-Integer "$label saveLoadedCount" $strictSibling.saveLoadedCount 1
    Assert-String "$label reportStatusCode" $strictSibling.reportStatusCode 'loaded'
    Assert-Boolean "$label reportLoaded" $strictSibling.reportLoaded $true
}

function Assert-AdvancedIdentityObservations([string] $caseId, [object] $identity, [long] $verifiedCount) {
    $label = "$caseId observations.advancedIdentity"
    Assert-ExactFields $label $identity @(
        'verifiedFixtureLoadSourceCount', 'foreignAdvancedLoadSourceCount', 'unexpectedManagedLoadSourceCount',
        'autoFishingLoadSourceCount', 'autoFishingEntryLogCount', 'autoFishingReportStatusCode', 'autoFishingReportLoaded',
        'autoFishingReportOfficialEnabled', 'autoFishingReportVersion', 'playerDoctorFixtureManagedIdentity',
        'playerDoctorFixtureProvenanceStatus', 'playerDoctorFixturePolicyId', 'playerDoctorFixtureGameBuildId',
        'playerDoctorFixtureHarmonyOwner', 'playerDoctorStrictSiblingCount', 'playerDoctorAutoFishingCount'
    )
    Assert-Integer "$label verifiedFixtureLoadSourceCount" $identity.verifiedFixtureLoadSourceCount $verifiedCount
    Assert-Integer "$label foreignAdvancedLoadSourceCount" $identity.foreignAdvancedLoadSourceCount 0
    Assert-Integer "$label unexpectedManagedLoadSourceCount" $identity.unexpectedManagedLoadSourceCount 0
    Assert-Integer "$label autoFishingLoadSourceCount" $identity.autoFishingLoadSourceCount 0
    Assert-Integer "$label autoFishingEntryLogCount" $identity.autoFishingEntryLogCount 0
    Assert-String "$label autoFishingReportStatusCode" $identity.autoFishingReportStatusCode 'disabled'
    Assert-Boolean "$label autoFishingReportLoaded" $identity.autoFishingReportLoaded $false
    Assert-Boolean "$label autoFishingReportOfficialEnabled" $identity.autoFishingReportOfficialEnabled $false
    Assert-String "$label autoFishingReportVersion" $identity.autoFishingReportVersion '1.4.3-dtmapi'
    Assert-String "$label playerDoctorFixtureManagedIdentity" $identity.playerDoctorFixtureManagedIdentity 'AdvancedCodeMod'
    Assert-String "$label playerDoctorFixtureProvenanceStatus" $identity.playerDoctorFixtureProvenanceStatus 'VerifiedReferenceReceipt'
    Assert-String "$label playerDoctorFixturePolicyId" $identity.playerDoctorFixturePolicyId 'doloctown-23762374-g2-v1'
    Assert-String "$label playerDoctorFixtureGameBuildId" $identity.playerDoctorFixtureGameBuildId '23762374'
    Assert-String "$label playerDoctorFixtureHarmonyOwner" $identity.playerDoctorFixtureHarmonyOwner 'dtmapi.mod.dtmapi.advancedfixture'
    Assert-Integer "$label playerDoctorStrictSiblingCount" $identity.playerDoctorStrictSiblingCount 3
    Assert-Integer "$label playerDoctorAutoFishingCount" $identity.playerDoctorAutoFishingCount 0
}

function Assert-FixtureObservations(
    [string] $caseId,
    [object] $fixture,
    [long] $entry,
    [long] $completion,
    [long] $postfix,
    [long] $query,
    [object] $equal,
    [long] $disabled,
    [long] $late,
    [object] $entryFailureMessage
) {
    $label = "$caseId observations.fixture"
    Assert-ExactFields $label $fixture @('entryCount', 'completionCount', 'patchPostfixCount', 'nativeQueryCount', 'queryAndPostfixResultEqual', 'disabledSkipCount', 'lateInstallCount', 'entryFailureMessage')
    Assert-Integer "$label entryCount" $fixture.entryCount $entry
    Assert-Integer "$label completionCount" $fixture.completionCount $completion
    Assert-Integer "$label patchPostfixCount" $fixture.patchPostfixCount $postfix
    Assert-Integer "$label nativeQueryCount" $fixture.nativeQueryCount $query
    if ($null -eq $equal) { Assert-Nullable "$label queryAndPostfixResultEqual" $fixture.queryAndPostfixResultEqual }
    else { Assert-Boolean "$label queryAndPostfixResultEqual" $fixture.queryAndPostfixResultEqual ([bool]$equal) }
    Assert-Integer "$label disabledSkipCount" $fixture.disabledSkipCount $disabled
    Assert-Integer "$label lateInstallCount" $fixture.lateInstallCount $late
    if ($null -eq $entryFailureMessage) { Assert-Nullable "$label entryFailureMessage" $fixture.entryFailureMessage }
    else { Assert-String "$label entryFailureMessage" $fixture.entryFailureMessage ([string]$entryFailureMessage) }
}

function Assert-CleanupObservations(
    [string] $caseId,
    [object] $cleanup,
    [object] $reason,
    [object] $removed,
    [object] $remaining,
    [object] $failureCount,
    [object] $machineCode,
    [object] $participants
) {
    $label = "$caseId observations.cleanup"
    Assert-ExactFields $label $cleanup @('reason', 'participants', 'removed', 'remaining', 'failures', 'machineCode', 'machineCodeRuntimeCount', 'machineCodeReportCount')
    if ($null -eq $reason) {
        foreach ($name in @('reason', 'participants', 'removed', 'remaining', 'failures', 'machineCode')) {
            Assert-Nullable "$label $name" (Get-OptionalProperty $cleanup $name)
        }
        Assert-Integer "$label machineCodeRuntimeCount" $cleanup.machineCodeRuntimeCount 0
        Assert-Integer "$label machineCodeReportCount" $cleanup.machineCodeReportCount 0
        return
    }
    Assert-String "$label reason" $cleanup.reason ([string]$reason)
    Assert-Integer "$label participants" $cleanup.participants ([long]$participants)
    Assert-Integer "$label removed" $cleanup.removed ([long]$removed)
    Assert-Integer "$label remaining" $cleanup.remaining ([long]$remaining)
    Assert-Integer "$label failures" $cleanup.failures ([long]$failureCount)
    Assert-String "$label machineCode" $cleanup.machineCode ([string]$machineCode)
    Assert-Integer "$label machineCodeRuntimeCount" $cleanup.machineCodeRuntimeCount 1
    Assert-Integer "$label machineCodeReportCount" $cleanup.machineCodeReportCount 1
}

function Assert-DiagnosticObservations(
    [string] $caseId,
    [object] $diagnostics,
    [long] $ownerMismatch,
    [long] $cleanupFailed,
    [long] $cleanupComplete,
    [long] $duplicatePatch,
    [long] $lateOwnerDrift,
    [long] $entryFailure,
    [long] $restartRequired,
    [long] $sdk602,
    [long] $sdk603
) {
    $label = "$caseId observations.diagnostics"
    Assert-ExactFields $label $diagnostics @('ownerMismatchCount', 'cleanupFailedCount', 'cleanupCompleteCount', 'duplicatePatchCount', 'lateOwnerDriftCount', 'entryFailureCount', 'restartRequiredCount', 'sdk602Count', 'sdk603Count')
    Assert-Integer "$label ownerMismatchCount" $diagnostics.ownerMismatchCount $ownerMismatch
    Assert-Integer "$label cleanupFailedCount" $diagnostics.cleanupFailedCount $cleanupFailed
    Assert-Integer "$label cleanupCompleteCount" $diagnostics.cleanupCompleteCount $cleanupComplete
    Assert-Integer "$label duplicatePatchCount" $diagnostics.duplicatePatchCount $duplicatePatch
    Assert-Integer "$label lateOwnerDriftCount" $diagnostics.lateOwnerDriftCount $lateOwnerDrift
    Assert-Integer "$label entryFailureCount" $diagnostics.entryFailureCount $entryFailure
    Assert-Integer "$label restartRequiredCount" $diagnostics.restartRequiredCount $restartRequired
    Assert-Integer "$label sdk602Count" $diagnostics.sdk602Count $sdk602
    Assert-Integer "$label sdk603Count" $diagnostics.sdk603Count $sdk603
}

function Assert-ReportObservations([string] $caseId, [object] $report, [string] $statusCode, [bool] $loaded, [bool] $officialEnabled, [string] $version) {
    $label = "$caseId observations.report"
    Assert-ExactFields $label $report @('statusCode', 'loaded', 'officialEnabled', 'version')
    Assert-String "$label statusCode" $report.statusCode $statusCode
    Assert-Boolean "$label loaded" $report.loaded $loaded
    Assert-Boolean "$label officialEnabled" $report.officialEnabled $officialEnabled
    Assert-String "$label version" $report.version $version
}

function Assert-EmptyState([string] $caseId, [object] $state) {
    $label = "$caseId observations.state"
    Assert-ExactFields $label $state @('markerAbsentBefore', 'queryObservedAtUtc', 'markerCreatedAtUtc', 'markerLength', 'reloadObservedAtUtc', 'markerAbsentAfter', 'configRestoredExact', 'processAbsentAfter', 'qaWorkshopReloadCompleted')
    foreach ($name in @('markerAbsentBefore', 'queryObservedAtUtc', 'markerCreatedAtUtc', 'markerLength', 'reloadObservedAtUtc', 'markerAbsentAfter', 'configRestoredExact', 'processAbsentAfter', 'qaWorkshopReloadCompleted')) {
        Assert-Nullable "$label $name" (Get-OptionalProperty $state $name)
    }
}

function Assert-PostLoadState([string] $caseId, [object] $state) {
    $label = "$caseId observations.state"
    Assert-ExactFields $label $state @('markerAbsentBefore', 'queryObservedAtUtc', 'markerCreatedAtUtc', 'markerLength', 'reloadObservedAtUtc', 'markerAbsentAfter', 'configRestoredExact', 'processAbsentAfter', 'qaWorkshopReloadCompleted')
    Assert-Boolean "$label markerAbsentBefore" $state.markerAbsentBefore $true
    Assert-Integer "$label markerLength" $state.markerLength 0
    Assert-Boolean "$label markerAbsentAfter" $state.markerAbsentAfter $true
    Assert-Boolean "$label configRestoredExact" $state.configRestoredExact $true
    Assert-Boolean "$label processAbsentAfter" $state.processAbsentAfter $true
    Assert-Boolean "$label qaWorkshopReloadCompleted" $state.qaWorkshopReloadCompleted $true
    $query = ConvertTo-UtcInstant "$label queryObservedAtUtc" $state.queryObservedAtUtc
    $marker = ConvertTo-UtcInstant "$label markerCreatedAtUtc" $state.markerCreatedAtUtc
    $reload = ConvertTo-UtcInstant "$label reloadObservedAtUtc" $state.reloadObservedAtUtc
    if ($null -ne $query -and $null -ne $marker -and $query -gt $marker) { Add-Failure "$label marker was created before the native query observation." }
    if ($null -ne $marker -and $null -ne $reload -and $marker -ge $reload) { Add-Failure "$label reload must occur after marker creation." }
}

function Assert-SessionFields([string] $label, [object] $session) {
    Assert-ExactFields $label $session @(
        'prepareSuccess',
        'sessionId',
        'deploymentStatusV1Success',
        'deploymentStatusV1RecoveryArtifacts',
        'snapshotSuccess',
        'snapshotRuntimeStatus',
        'snapshotRuntimeCode',
        'snapshotVersion',
        'snapshotTreeSha256',
        'updateSuccess',
        'updatedVersion',
        'deploymentStatusV2Success',
        'deploymentStatusV2RecoveryArtifacts',
        'reloadSuccess',
        'reloadRuntimeStatus',
        'reloadRuntimeCode',
        'reloadDiagnosticCodes',
        'clearSuccess',
        'credentialsCleared',
        'clearedCredentialFileCount',
        'preUpdateTreeSha256',
        'postUpdateTreeSha256'
    )
}

function Assert-EmptySession([string] $caseId, [object] $session) {
    $label = "$caseId observations.session"
    Assert-SessionFields $label $session
    foreach ($name in @(
        'prepareSuccess', 'sessionId', 'deploymentStatusV1Success', 'deploymentStatusV1RecoveryArtifacts',
        'snapshotSuccess', 'snapshotRuntimeStatus', 'snapshotRuntimeCode', 'snapshotVersion', 'snapshotTreeSha256',
        'updateSuccess', 'updatedVersion', 'deploymentStatusV2Success', 'deploymentStatusV2RecoveryArtifacts',
        'reloadSuccess', 'reloadRuntimeStatus', 'reloadRuntimeCode', 'clearSuccess', 'credentialsCleared',
        'clearedCredentialFileCount', 'preUpdateTreeSha256', 'postUpdateTreeSha256'
    )) { Assert-Nullable "$label $name" (Get-OptionalProperty $session $name) }
    if ($null -eq $session.reloadDiagnosticCodes) { Add-Failure "$label reloadDiagnosticCodes must be an empty array, not null." }
    elseif (@($session.reloadDiagnosticCodes).Count -ne 0) { Add-Failure "$label reloadDiagnosticCodes must be empty." }
}

function Assert-LiveUpdateSession([string] $caseId, [object] $session) {
    $label = "$caseId observations.session"
    Assert-SessionFields $label $session
    Assert-Boolean "$label prepareSuccess" $session.prepareSuccess $true
    if ($null -eq $session.sessionId -or [string]$session.sessionId -cnotmatch '^[0-9a-f]{32}$') { Add-Failure "$label sessionId must be one lowercase 32-character identifier." }
    Assert-Boolean "$label deploymentStatusV1Success" $session.deploymentStatusV1Success $true
    Assert-Integer "$label deploymentStatusV1RecoveryArtifacts" $session.deploymentStatusV1RecoveryArtifacts 0
    Assert-Boolean "$label snapshotSuccess" $session.snapshotSuccess $true
    Assert-String "$label snapshotRuntimeStatus" $session.snapshotRuntimeStatus 'ok'
    Assert-String "$label snapshotRuntimeCode" $session.snapshotRuntimeCode 'source-snapshot'
    Assert-String "$label snapshotVersion" $session.snapshotVersion '0.1.0'
    Assert-Sha256 "$label snapshotTreeSha256" $session.snapshotTreeSha256
    Assert-Boolean "$label updateSuccess" $session.updateSuccess $true
    Assert-String "$label updatedVersion" $session.updatedVersion '0.1.1'
    Assert-Boolean "$label deploymentStatusV2Success" $session.deploymentStatusV2Success $true
    Assert-Integer "$label deploymentStatusV2RecoveryArtifacts" $session.deploymentStatusV2RecoveryArtifacts 1
    Assert-Boolean "$label reloadSuccess" $session.reloadSuccess $true
    Assert-String "$label reloadRuntimeStatus" $session.reloadRuntimeStatus 'restart-required'
    Assert-String "$label reloadRuntimeCode" $session.reloadRuntimeCode 'manifest-changed-restart-required'
    Assert-ExactSequence "$label reloadDiagnosticCodes" @($session.reloadDiagnosticCodes) @('SDK602')
    Assert-Boolean "$label clearSuccess" $session.clearSuccess $true
    Assert-Boolean "$label credentialsCleared" $session.credentialsCleared $true
    Assert-Integer "$label clearedCredentialFileCount" $session.clearedCredentialFileCount 1
    Assert-Sha256 "$label preUpdateTreeSha256" $session.preUpdateTreeSha256
    Assert-Sha256 "$label postUpdateTreeSha256" $session.postUpdateTreeSha256
    if ($null -ne $session.snapshotTreeSha256 -and $null -ne $session.preUpdateTreeSha256 -and ([string]$session.snapshotTreeSha256).Equals([string]$session.preUpdateTreeSha256, [StringComparison]::Ordinal)) {
        Add-Failure "$label source-snapshot and pre-update deployment-inventory digest domains must remain distinct."
    }
    if ($null -ne $session.preUpdateTreeSha256 -and $null -ne $session.postUpdateTreeSha256 -and ([string]$session.preUpdateTreeSha256).Equals([string]$session.postUpdateTreeSha256, [StringComparison]::Ordinal)) {
        Add-Failure "$label pre/post update tree hashes must differ."
    }
}

function Assert-RepoRelativePath([string] $label, [object] $value) {
    if ($null -eq $value -or -not ($value -is [string]) -or [string]::IsNullOrWhiteSpace([string]$value)) {
        Add-Failure "$label must be a non-empty repository-relative path."
        return
    }
    $path = [string]$value
    if ([System.IO.Path]::IsPathRooted($path) -or $path.IndexOf('\', [StringComparison]::Ordinal) -ge 0 -or $path.IndexOf(':', [StringComparison]::Ordinal) -ge 0) {
        Add-Failure "$label must use repository-relative forward-slash form: '$path'."
    }
    if ($path -match '(^|/)\.\.(/|$)' -or $path.StartsWith('/', [StringComparison]::Ordinal)) {
        Add-Failure "$label escapes the repository boundary: '$path'."
    }
}

function Assert-Evidence([string] $caseId, [object] $evidence, [string] $kind) {
    $label = "$caseId evidence"
    $fields = @(
        'runRootPath', 'result', 'summary', 'runtimeLog', 'qaCleanup', 'profileRestore', 'processCheck', 'reportZip',
        'reportRuntimeContext', 'reportRuntimeLog', 'reportReleaseManifest', 'reportPlayerDoctor', 'runnerReceipt',
        'postLoadOrchestration', 'liveUpdateOrchestration', 'sessionPrepare', 'deploymentStatusV1',
        'sessionSnapshot', 'sdkUpdate', 'deploymentStatusV2', 'sessionReload', 'sessionClear'
    )
    Assert-ExactFields $label $evidence $fields
    Assert-RepoRelativePath "$label runRootPath" $evidence.runRootPath
    $runRoot = Normalize-Path ([string]$evidence.runRootPath)
    if ($runRoot -notmatch '^docs/debug/evidence/GAME-SMOKE/[0-9]{8}-[0-9]{6}$') {
        Add-Failure "$label runRootPath must be one managed GAME-SMOKE run root; found '$runRoot'."
    }

    foreach ($name in @('result', 'summary', 'runtimeLog', 'qaCleanup', 'profileRestore', 'processCheck', 'reportZip', 'reportRuntimeContext', 'reportRuntimeLog', 'reportReleaseManifest', 'reportPlayerDoctor')) {
        $row = Get-OptionalProperty $evidence $name
        Assert-FileRow "$label $name" $row $false
        if ($null -ne $row) {
            Assert-RepoRelativePath "$label $name path" $row.path
            $path = Normalize-Path ([string]$row.path)
            if (-not ($path.Equals($runRoot, [StringComparison]::Ordinal) -or $path.StartsWith($runRoot + '/', [StringComparison]::Ordinal))) {
                Add-Failure "$label $name path is outside its run root: '$path'."
            }
        }
    }

    $reportZipPath = if ($null -eq $evidence.reportZip) { '' } else { Normalize-Path ([string]$evidence.reportZip.path) }
    $expectedReportEntries = [ordered]@{
        reportRuntimeContext = 'DTMAPI-runtime-context.txt'
        reportRuntimeLog = 'DTMAPI-latest.log'
        reportReleaseManifest = 'release-manifest.json'
        reportPlayerDoctor = 'PlayerDoctor/player-doctor.json'
    }
    foreach ($name in @($expectedReportEntries.Keys)) {
        $row = Get-OptionalProperty $evidence $name
        if ($null -ne $row -and -not [string]::IsNullOrWhiteSpace($reportZipPath)) {
            Assert-String "$label $name ZIP entry path" $row.path ($reportZipPath + '!/' + [string]$expectedReportEntries[$name])
        }
    }

    $runnerRequired = $caseId -in @('wrong-owner-cold', 'duplicate-patch-cold', 'entry-failure-cold', 'late-owner-drift-cold', 'disabled-cold')
    $runnerRow = Get-OptionalProperty $evidence 'runnerReceipt'
    Assert-FileRow "$label runnerReceipt" $runnerRow (-not $runnerRequired)
    if (-not $runnerRequired -and $null -ne $runnerRow) { Add-Failure "$label runnerReceipt must be null outside the five cold-runner cases." }
    if ($null -ne $runnerRow) {
        Assert-RepoRelativePath "$label runnerReceipt path" $runnerRow.path
        $runnerPath = Normalize-Path ([string]$runnerRow.path)
        if ($runnerPath -notmatch '^docs/debug/evidence/GAME-SMOKE/[0-9]{8}-[0-9]{6}/g2-cold-cases-runner-receipt\.json$') {
            Add-Failure "$label runnerReceipt path is not a managed G2 cold-runner receipt: '$runnerPath'."
        }
    }

    $specialNames = @('postLoadOrchestration', 'liveUpdateOrchestration', 'sessionPrepare', 'deploymentStatusV1', 'sessionSnapshot', 'sdkUpdate', 'deploymentStatusV2', 'sessionReload', 'sessionClear')
    foreach ($name in $specialNames) {
        $required = ($kind -eq 'post-load-disable' -and $name -eq 'postLoadOrchestration') -or
            ($kind -eq 'live-update' -and $name -in @('liveUpdateOrchestration', 'sessionPrepare', 'deploymentStatusV1', 'sessionSnapshot', 'sdkUpdate', 'deploymentStatusV2', 'sessionReload', 'sessionClear'))
        $row = Get-OptionalProperty $evidence $name
        Assert-FileRow "$label $name" $row (-not $required)
        if ($required -and $null -eq $row) { continue }
        if (-not $required -and $null -ne $row) {
            Add-Failure "$label $name must be null for kind '$kind'."
            continue
        }
        if ($null -ne $row) {
            Assert-RepoRelativePath "$label $name path" $row.path
            $path = Normalize-Path ([string]$row.path)
            if (-not ($path.Equals($runRoot, [StringComparison]::Ordinal) -or $path.StartsWith($runRoot + '/', [StringComparison]::Ordinal))) {
                Add-Failure "$label $name path is outside its run root: '$path'."
            }
        }
    }
}

function Assert-ExpectedOutcome([string] $caseId, [object] $outcome, [hashtable] $expected) {
    $label = "$caseId expectedOutcome"
    Assert-ExactFields $label $outcome @('classification', 'statusCode', 'loaded', 'officialEnabled', 'fixtureVersion', 'restartRequired')
    Assert-String "$label classification" $outcome.classification 'Advanced'
    Assert-String "$label statusCode" $outcome.statusCode ([string]$expected.statusCode)
    Assert-Boolean "$label loaded" $outcome.loaded ([bool]$expected.loaded)
    Assert-Boolean "$label officialEnabled" $outcome.officialEnabled ([bool]$expected.officialEnabled)
    Assert-String "$label fixtureVersion" $outcome.fixtureVersion ([string]$expected.version)
    Assert-Boolean "$label restartRequired" $outcome.restartRequired ([bool]$expected.restartRequired)
}

function Assert-Case([object] $case, [hashtable] $expected) {
    $caseId = [string]$expected.caseId
    Assert-ExactFields "Case $caseId" $case @(
        'caseId', 'kind', 'startedAtUtc', 'completedAtUtc', 'genericSmokeExitCode', 'fixtureVersionAtStart',
        'expectedOutcome', 'expectedDiagnostics', 'evidence', 'observations', 'accepted'
    )
    Assert-String "$caseId caseId" $case.caseId $caseId
    Assert-String "$caseId kind" $case.kind ([string]$expected.kind)
    Assert-Integer "$caseId genericSmokeExitCode" $case.genericSmokeExitCode ([long]$expected.smokeExit)
    Assert-String "$caseId fixtureVersionAtStart" $case.fixtureVersionAtStart ([string]$expected.version)
    Assert-Boolean "$caseId accepted" $case.accepted $true
    Assert-ExpectedOutcome $caseId $case.expectedOutcome $expected
    Assert-ExactSequence "$caseId expectedDiagnostics" @($case.expectedDiagnostics) @($expected.expectedDiagnostics)
    Assert-Evidence $caseId $case.evidence ([string]$expected.kind)

    $started = ConvertTo-UtcInstant "$caseId startedAtUtc" $case.startedAtUtc
    $completed = ConvertTo-UtcInstant "$caseId completedAtUtc" $case.completedAtUtc
    if ($null -ne $started -and $null -ne $completed -and $started -gt $completed) {
        Add-Failure "$caseId startedAtUtc is later than completedAtUtc."
    }

    $observations = $case.observations
    Assert-ExactFields "$caseId observations" $observations @('common', 'strictSibling', 'advancedIdentity', 'fixture', 'cleanup', 'diagnostics', 'report', 'state', 'session')
    $expectedRunStatus = if ([long]$expected.smokeExit -eq 0) { 'Passed' } else { 'Failed' }
    Assert-CommonObservations $caseId $observations.common $expectedRunStatus @($expected.resultFailures)
    Assert-StrictSiblingObservations $caseId $observations.strictSibling
    Assert-AdvancedIdentityObservations $caseId $observations.advancedIdentity ([long]$expected.verifiedCount)
    Assert-FixtureObservations $caseId $observations.fixture ([long]$expected.entry) ([long]$expected.completion) ([long]$expected.postfix) ([long]$expected.query) $expected.equal ([long]$expected.disabled) ([long]$expected.late) $expected.entryFailureMessage
    Assert-CleanupObservations $caseId $observations.cleanup $expected.cleanupReason $expected.removed $expected.remaining $expected.cleanupFailures $expected.cleanupCode $expected.participants
    Assert-DiagnosticObservations $caseId $observations.diagnostics ([long]$expected.ownerMismatch) ([long]$expected.cleanupFailed) ([long]$expected.cleanupComplete) ([long]$expected.duplicatePatch) ([long]$expected.lateOwnerDrift) ([long]$expected.entryFailure) ([long]$expected.restartCount) ([long]$expected.sdk602) 0
    Assert-ReportObservations $caseId $observations.report ([string]$expected.statusCode) ([bool]$expected.loaded) ([bool]$expected.officialEnabled) ([string]$expected.version)
    if ([string]$expected.kind -eq 'post-load-disable') { Assert-PostLoadState $caseId $observations.state }
    else { Assert-EmptyState $caseId $observations.state }
    if ([string]$expected.kind -eq 'live-update') { Assert-LiveUpdateSession $caseId $observations.session }
    else { Assert-EmptySession $caseId $observations.session }

    $reportStarted = ConvertTo-UtcInstant "$caseId reportStartedAtUtc" $observations.common.reportStartedAtUtc
    $reportGenerated = ConvertTo-UtcInstant "$caseId reportGeneratedAtUtc" $observations.common.reportGeneratedAtUtc
    if ($null -ne $started -and $null -ne $reportStarted -and $reportStarted -lt $started) { Add-Failure "$caseId report started before the case interval." }
    if ($null -ne $reportStarted -and $null -ne $reportGenerated -and $reportGenerated -lt $reportStarted) { Add-Failure "$caseId report generated before report start." }
    if ($null -ne $reportGenerated -and $null -ne $completed -and $reportGenerated -gt $completed) { Add-Failure "$caseId report generated after the case interval." }

    return [ordered]@{ started = $started; completed = $completed; runRoot = [string]$case.evidence.runRootPath }
}

$caseExpectations = @(
    @{
        caseId='wrong-owner-cold'; kind='cold'; smokeExit=1; version='0.1.0'; statusCode='restart-required'; loaded=$false; officialEnabled=$true; restartRequired=$true; expectedDiagnostics=@('advanced-harmony-owner-mismatch','advanced-harmony-cleanup-failed');
        verifiedCount=1; entry=1; completion=0; postfix=1; query=0; equal=$null; disabled=0; late=0; entryFailureMessage=$null;
        cleanupReason='EntryFailed'; participants=2; removed=0; remaining=1; cleanupFailures=1; cleanupCode='advanced-harmony-cleanup-failed';
        ownerMismatch=1; cleanupFailed=1; cleanupComplete=0; duplicatePatch=0; lateOwnerDrift=0; entryFailure=0; restartCount=1; sdk602=0; resultFailures=@('ModOwnerLifecycle','GameBridgeFinalHealthSnapshot','FailedModRollback','RunStatus')
    },
    @{
        caseId='duplicate-patch-cold'; kind='cold'; smokeExit=1; version='0.1.0'; statusCode='restart-required'; loaded=$false; officialEnabled=$true; restartRequired=$true; expectedDiagnostics=@('advanced-harmony-duplicate-patch','advanced-harmony-cleanup-complete');
        verifiedCount=1; entry=1; completion=0; postfix=0; query=0; equal=$null; disabled=0; late=0; entryFailureMessage=$null;
        cleanupReason='EntryFailed'; participants=2; removed=2; remaining=0; cleanupFailures=0; cleanupCode='advanced-harmony-cleanup-complete';
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=1; duplicatePatch=1; lateOwnerDrift=0; entryFailure=0; restartCount=1; sdk602=0; resultFailures=@('GameBridgeFinalHealthSnapshot','RunStatus')
    },
    @{
        caseId='entry-failure-cold'; kind='cold'; smokeExit=1; version='0.1.0'; statusCode='restart-required'; loaded=$false; officialEnabled=$true; restartRequired=$true; expectedDiagnostics=@('advanced-harmony-cleanup-complete');
        verifiedCount=1; entry=1; completion=0; postfix=0; query=0; equal=$null; disabled=0; late=0; entryFailureMessage='Synthetic AdvancedFixture EntryFailure after canonical-owner patch installation.';
        cleanupReason='EntryFailed'; participants=2; removed=1; remaining=0; cleanupFailures=0; cleanupCode='advanced-harmony-cleanup-complete';
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=1; duplicatePatch=0; lateOwnerDrift=0; entryFailure=1; restartCount=1; sdk602=0; resultFailures=@('GameBridgeFinalHealthSnapshot','RunStatus')
    },
    @{
        caseId='late-owner-drift-cold'; kind='cold'; smokeExit=1; version='0.1.0'; statusCode='restart-required'; loaded=$false; officialEnabled=$true; restartRequired=$true; expectedDiagnostics=@('advanced-harmony-late-owner-drift','advanced-harmony-cleanup-complete');
        verifiedCount=1; entry=1; completion=1; postfix=1; query=1; equal=$true; disabled=0; late=1; entryFailureMessage=$null;
        cleanupReason='EntryFailed'; participants=2; removed=1; remaining=0; cleanupFailures=0; cleanupCode='advanced-harmony-cleanup-complete';
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=1; duplicatePatch=0; lateOwnerDrift=1; entryFailure=0; restartCount=1; sdk602=0; resultFailures=@('GameBridgeFinalHealthSnapshot','RunStatus')
    },
    @{
        caseId='disabled-cold'; kind='cold'; smokeExit=0; version='0.1.0'; statusCode='disabled'; loaded=$false; officialEnabled=$false; restartRequired=$false; expectedDiagnostics=@();
        verifiedCount=0; entry=0; completion=0; postfix=0; query=0; equal=$null; disabled=1; late=0; entryFailureMessage=$null;
        cleanupReason=$null; participants=$null; removed=$null; remaining=$null; cleanupFailures=$null; cleanupCode=$null;
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=0; duplicatePatch=0; lateOwnerDrift=0; entryFailure=0; restartCount=0; sdk602=0; resultFailures=@()
    },
    @{
        caseId='normal-v1-cold'; kind='cold'; smokeExit=0; version='0.1.0'; statusCode='loaded'; loaded=$true; officialEnabled=$true; restartRequired=$false; expectedDiagnostics=@();
        verifiedCount=1; entry=1; completion=1; postfix=1; query=1; equal=$true; disabled=0; late=0; entryFailureMessage=$null;
        cleanupReason=$null; participants=$null; removed=$null; remaining=$null; cleanupFailures=$null; cleanupCode=$null;
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=0; duplicatePatch=0; lateOwnerDrift=0; entryFailure=0; restartCount=0; sdk602=0; resultFailures=@()
    },
    @{
        caseId='postload-disable-v1'; kind='post-load-disable'; smokeExit=1; version='0.1.0'; statusCode='restart-required'; loaded=$false; officialEnabled=$false; restartRequired=$true; expectedDiagnostics=@('advanced-harmony-cleanup-complete');
        verifiedCount=1; entry=1; completion=1; postfix=1; query=1; equal=$true; disabled=1; late=0; entryFailureMessage=$null;
        cleanupReason='Unload'; participants=2; removed=1; remaining=0; cleanupFailures=0; cleanupCode='advanced-harmony-cleanup-complete';
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=1; duplicatePatch=0; lateOwnerDrift=0; entryFailure=0; restartCount=1; sdk602=0; resultFailures=@('GameBridgeFinalHealthSnapshot','RunStatus')
    },
    @{
        caseId='live-update-v1-to-v2'; kind='live-update'; smokeExit=0; version='0.1.0'; statusCode='loaded'; loaded=$true; officialEnabled=$true; restartRequired=$true; expectedDiagnostics=@('SDK602');
        verifiedCount=1; entry=1; completion=1; postfix=1; query=1; equal=$true; disabled=0; late=0; entryFailureMessage=$null;
        cleanupReason=$null; participants=$null; removed=$null; remaining=$null; cleanupFailures=$null; cleanupCode=$null;
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=0; duplicatePatch=0; lateOwnerDrift=0; entryFailure=0; restartCount=0; sdk602=1; resultFailures=@()
    },
    @{
        caseId='final-clean-v2-cold'; kind='cold'; smokeExit=0; version='0.1.1'; statusCode='loaded'; loaded=$true; officialEnabled=$true; restartRequired=$false; expectedDiagnostics=@();
        verifiedCount=1; entry=1; completion=1; postfix=1; query=1; equal=$true; disabled=0; late=0; entryFailureMessage=$null;
        cleanupReason=$null; participants=$null; removed=$null; remaining=$null; cleanupFailures=$null; cleanupCode=$null;
        ownerMismatch=0; cleanupFailed=0; cleanupComplete=0; duplicatePatch=0; lateOwnerDrift=0; entryFailure=0; restartCount=0; sdk602=0; resultFailures=@()
    }
)

$receiptFullPath = Resolve-File $ReceiptPath 'tools\release\baselines\batch6-g2-runtime-matrix-receipt.json'
$receiptText = Read-Utf8 $receiptFullPath
$receipt = $null
if (-not [string]::IsNullOrWhiteSpace($receiptText)) {
    try { $receipt = ConvertFrom-ReceiptJson $receiptText }
    catch { Add-Failure "Runtime matrix receipt is not parseable JSON: $($_.Exception.Message)" }
}

$schemaPath = Join-Path $repo 'tools\release\contracts\batch6-g2-runtime-matrix-receipt.schema.json'
$schemaText = Read-Utf8 $schemaPath
$schema = $null
if (-not [string]::IsNullOrWhiteSpace($schemaText)) {
    try { $schema = ConvertFrom-ReceiptJson $schemaText }
    catch { Add-Failure "Runtime matrix receipt schema is not parseable JSON: $($_.Exception.Message)" }
}
if ($null -ne $schema) {
    $receiptFields = @(
        'schemaVersion', 'contractId', 'receiptKind', 'architectureAuthority', 'reviewAuthority',
        'rootCauseReviewAuthority', 'updateAuthority', 'implementationCommit', 'capturedAtUtc',
        'runtimeRelease', 'fixture', 'environment', 'artifacts', 'cases', 'matrixSha256'
    )
    Assert-String 'Receipt schema dialect' (Get-OptionalProperty $schema '$schema') 'https://json-schema.org/draft/2020-12/schema'
    Assert-String 'Receipt schema ID' (Get-OptionalProperty $schema '$id') 'https://dtmapi.local/contracts/batch6-g2-runtime-matrix-receipt.schema.json'
    Assert-String 'Receipt schema root type' $schema.type 'object'
    Assert-Boolean 'Receipt schema root additionalProperties' $schema.additionalProperties $false
    Assert-ExactSequence 'Receipt schema required fields' @($schema.required) $receiptFields
    Assert-ExactSet 'Receipt schema property fields' @($schema.properties.PSObject.Properties.Name) $receiptFields
    Assert-Integer 'Receipt schema schemaVersion const' $schema.properties.schemaVersion.const 1
    Assert-String 'Receipt schema contractId const' $schema.properties.contractId.const 'batch6-g2-advanced-synthetic-vertical-slice'
    $schemaDefs = Get-OptionalProperty $schema '$defs'
    $schemaParity = @(
        [pscustomobject]@{ Name='evidence'; Fields=@('runRootPath','result','summary','runtimeLog','qaCleanup','profileRestore','processCheck','reportZip','reportRuntimeContext','reportRuntimeLog','reportReleaseManifest','reportPlayerDoctor','runnerReceipt','postLoadOrchestration','liveUpdateOrchestration','sessionPrepare','deploymentStatusV1','sessionSnapshot','sdkUpdate','deploymentStatusV2','sessionReload','sessionClear') },
        [pscustomobject]@{ Name='commonObservations'; Fields=@('resultSchemaVersion','resultRunStatus','failedResultFields','saveLoaded','hookProbe','gameLaunched','processExited','noFatalInstanceWindow','forcedClose','qaHostCleanup','qaSaveLoadedObservation','officialModProfileRestored','localAuthorSourceStateRestored','local11AuthorSourceStateRestored','isolateAllOfficialMods','officialModProfileApplied','summarySaveSlot','summaryIncludeHookProbe','summaryLaunchMode','qaCleanupExactArtifacts','qaCleanupExactQaEvidence','qaCleanupExactTree','qaCleanupCleaned','qaCleanupRetained','profileRestoreExactLength','profileRestoreExactSha256','profileRestorePassed','processAbsent','reportRuntimeLogIsRunPrefix','reportStartedAtUtc','reportGeneratedAtUtc','playerDoctorRuntimeAssemblyHashMatch','playerDoctorErrorCount','playerDoctorWarningCount','playerDoctorMisplacedCount') },
        [pscustomobject]@{ Name='advancedIdentityObservations'; Fields=@('verifiedFixtureLoadSourceCount','foreignAdvancedLoadSourceCount','unexpectedManagedLoadSourceCount','autoFishingLoadSourceCount','autoFishingEntryLogCount','autoFishingReportStatusCode','autoFishingReportLoaded','autoFishingReportOfficialEnabled','autoFishingReportVersion','playerDoctorFixtureManagedIdentity','playerDoctorFixtureProvenanceStatus','playerDoctorFixturePolicyId','playerDoctorFixtureGameBuildId','playerDoctorFixtureHarmonyOwner','playerDoctorStrictSiblingCount','playerDoctorAutoFishingCount') },
        [pscustomobject]@{ Name='sessionObservations'; Fields=@('prepareSuccess','sessionId','deploymentStatusV1Success','deploymentStatusV1RecoveryArtifacts','snapshotSuccess','snapshotRuntimeStatus','snapshotRuntimeCode','snapshotVersion','snapshotTreeSha256','updateSuccess','updatedVersion','deploymentStatusV2Success','deploymentStatusV2RecoveryArtifacts','reloadSuccess','reloadRuntimeStatus','reloadRuntimeCode','reloadDiagnosticCodes','clearSuccess','credentialsCleared','clearedCredentialFileCount','preUpdateTreeSha256','postUpdateTreeSha256') }
    )
    foreach ($spec in $schemaParity) {
        $definition = Get-OptionalProperty $schemaDefs ([string]$spec.Name)
        if ($null -eq $definition) { Add-Failure "Receipt schema lacks definition '$($spec.Name)'."; continue }
        Assert-String "Receipt schema $($spec.Name) type" $definition.type 'object'
        Assert-Boolean "Receipt schema $($spec.Name) additionalProperties" $definition.additionalProperties $false
        Assert-ExactSequence "Receipt schema $($spec.Name) required fields" @($definition.required) @($spec.Fields)
        Assert-ExactSet "Receipt schema $($spec.Name) property fields" @($definition.properties.PSObject.Properties.Name) @($spec.Fields)
    }
}

$caseTimes = New-Object System.Collections.Generic.List[object]
$captured = $null
$implementationCommit = ''
if ($null -ne $receipt) {
    Assert-ExactFields 'Receipt' $receipt @(
        'schemaVersion', 'contractId', 'receiptKind', 'architectureAuthority', 'reviewAuthority',
        'rootCauseReviewAuthority', 'updateAuthority', 'implementationCommit', 'capturedAtUtc',
        'runtimeRelease', 'fixture', 'environment', 'artifacts', 'cases', 'matrixSha256'
    )
    Assert-Integer 'Receipt schemaVersion' $receipt.schemaVersion 1
    Assert-String 'Receipt contractId' $receipt.contractId 'batch6-g2-advanced-synthetic-vertical-slice'
    Assert-String 'Receipt receiptKind' $receipt.receiptKind 'committed-runtime-matrix'
    Assert-String 'Receipt architectureAuthority' $receipt.architectureAuthority 'docs/architecture/batch6-managed-mod-identity-contract.md'
    Assert-String 'Receipt reviewAuthority' $receipt.reviewAuthority 'docs/reviews/code/2026/20260720-0004-batch6-g2-advanced-synthetic-vertical-slice-prerequisite.md'
    Assert-String 'Receipt rootCauseReviewAuthority' $receipt.rootCauseReviewAuthority 'docs/reviews/code/2026/20260720-0005-batch6-g2-first-runtime-matrix-root-cause.md'
    Assert-String 'Receipt updateAuthority' $receipt.updateAuthority 'docs/updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md'

    $implementationCommit = [string]$receipt.implementationCommit
    if ($implementationCommit -cnotmatch '^[0-9a-f]{40}$') {
        Add-Failure "Receipt implementationCommit must be one lowercase full Git commit; found '$implementationCommit'."
    }
    else {
        try {
            $resolvedCommit = ([string](Invoke-GitLines @('rev-parse', ($implementationCommit + '^{commit}')) | Select-Object -First 1)).Trim()
            Assert-String 'Resolved implementationCommit' $resolvedCommit $implementationCommit
            if (-not (Test-Git @('merge-base', '--is-ancestor', $implementationCommit, 'HEAD'))) {
                Add-Failure 'Receipt implementationCommit is not an ancestor of current HEAD.'
            }
        }
        catch { Add-Failure "Receipt implementationCommit is not resolvable: $implementationCommit" }
    }
    $captured = ConvertTo-UtcInstant 'Receipt capturedAtUtc' $receipt.capturedAtUtc
    if ($null -ne $captured -and $captured -gt [DateTimeOffset]::UtcNow.AddMinutes(1)) {
        Add-Failure 'Receipt capturedAtUtc is later than the trusted current UTC clock.'
    }

    $runtime = $receipt.runtimeRelease
    Assert-ExactFields 'Receipt runtimeRelease' $runtime @('buildCommitPrefix', 'buildTimeUtc', 'dtmApiVersion', 'releaseManifestSha256', 'assemblies')
    if ($null -eq $runtime.buildCommitPrefix -or [string]$runtime.buildCommitPrefix -cnotmatch '^[0-9a-f]{12}$') {
        Add-Failure "Receipt runtimeRelease.buildCommitPrefix must be a lowercase 12-character Git prefix; found '$($runtime.buildCommitPrefix)'."
    }
    elseif ($implementationCommit.Length -eq 40) {
        Assert-String 'Runtime release build/implementation binding' $runtime.buildCommitPrefix $implementationCommit.Substring(0, 12)
    }
    $buildTime = ConvertTo-UtcInstant 'Receipt runtimeRelease.buildTimeUtc' $runtime.buildTimeUtc
    Assert-String 'Receipt runtimeRelease.dtmApiVersion' $runtime.dtmApiVersion '0.5.5'
    Assert-Sha256 'Receipt runtimeRelease.releaseManifestSha256' $runtime.releaseManifestSha256
    $expectedAssemblies = @(
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Abstractions.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    )
    Assert-ExactSequence 'Receipt Runtime assembly order' @($runtime.assemblies | ForEach-Object { [string]$_.name }) $expectedAssemblies
    foreach ($assembly in @($runtime.assemblies)) {
        $name = [string]$assembly.name
        Assert-ExactFields "Receipt Runtime assembly $name" $assembly @('name', 'length', 'sha256')
        Assert-NonEmptyString "Receipt Runtime assembly $name name" $assembly.name
        Assert-NonNegativeInteger "Receipt Runtime assembly $name length" $assembly.length
        if ($null -ne $assembly.length -and [long]$assembly.length -eq 0) { Add-Failure "Receipt Runtime assembly $name length must be positive." }
        Assert-Sha256 "Receipt Runtime assembly $name sha256" $assembly.sha256
    }

    $fixture = $receipt.fixture
    Assert-ExactFields 'Receipt fixture' $fixture @('uniqueId', 'canonicalHarmonyOwner', 'gameBuildId', 'gameAssemblySha256', 'nativeType', 'nativeMethod', 'nativeMethodToken', 'saveSlot', 'runtimeSlot', 'strictSiblingUniqueId')
    Assert-String 'Receipt fixture uniqueId' $fixture.uniqueId 'DTMAPI.AdvancedFixture'
    Assert-String 'Receipt fixture canonicalHarmonyOwner' $fixture.canonicalHarmonyOwner 'dtmapi.mod.dtmapi.advancedfixture'
    Assert-String 'Receipt fixture gameBuildId' $fixture.gameBuildId '23762374'
    Assert-String 'Receipt fixture gameAssemblySha256' $fixture.gameAssemblySha256 'C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404'
    Assert-String 'Receipt fixture nativeType' $fixture.nativeType 'DolocAPI'
    Assert-String 'Receipt fixture nativeMethod' $fixture.nativeMethod 'Has087DemoData'
    Assert-String 'Receipt fixture nativeMethodToken' $fixture.nativeMethodToken '0x060000A0'
    Assert-Integer 'Receipt fixture saveSlot' $fixture.saveSlot 3
    Assert-Integer 'Receipt fixture runtimeSlot' $fixture.runtimeSlot 2
    Assert-String 'Receipt fixture strictSiblingUniqueId' $fixture.strictSiblingUniqueId 'DTMAPI.HookProbeMod'

    $environment = $receipt.environment
    Assert-ExactFields 'Receipt environment' $environment @('gameBuildId', 'gameAssemblySha256', 'launchMode', 'saveSlot', 'runtimeSlot')
    Assert-String 'Receipt environment gameBuildId' $environment.gameBuildId '23762374'
    Assert-String 'Receipt environment gameAssemblySha256' $environment.gameAssemblySha256 'C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404'
    Assert-String 'Receipt environment launchMode' $environment.launchMode 'Steam'
    Assert-Integer 'Receipt environment saveSlot' $environment.saveSlot 3
    Assert-Integer 'Receipt environment runtimeSlot' $environment.runtimeSlot 2

    $artifacts = $receipt.artifacts
    Assert-ExactFields 'Receipt artifacts' $artifacts @('authorSdkZip', 'fixturePackages')
    $sdk = $artifacts.authorSdkZip
    Assert-ExactFields 'Receipt artifacts.authorSdkZip' $sdk @('logicalName', 'path', 'length', 'sha256')
    Assert-NonEmptyString 'Receipt artifacts.authorSdkZip logicalName' $sdk.logicalName
    if ($null -ne $sdk.logicalName -and [string]$sdk.logicalName -cnotmatch '^DTMAPI-Author-SDK-.+\.zip$') {
        Add-Failure "Receipt artifacts.authorSdkZip logicalName is invalid: '$($sdk.logicalName)'."
    }
    Assert-RepoRelativePath 'Receipt artifacts.authorSdkZip path' $sdk.path
    Assert-NonNegativeInteger 'Receipt artifacts.authorSdkZip length' $sdk.length
    if ($null -ne $sdk.length -and [long]$sdk.length -eq 0) { Add-Failure 'Receipt artifacts.authorSdkZip length must be positive.' }
    Assert-Sha256 'Receipt artifacts.authorSdkZip sha256' $sdk.sha256

    Assert-ExactSequence 'Receipt fixture package versions' @($artifacts.fixturePackages | ForEach-Object { [string]$_.version }) @('0.1.0', '0.1.1')
    foreach ($package in @($artifacts.fixturePackages)) {
        $version = [string]$package.version
        Assert-ExactFields "Receipt fixture package $version" $package @('version', 'package', 'packageReport', 'manifestSha256', 'entryAssemblySha256', 'advancedReferenceReceiptSha256', 'fileCount', 'bundledNativeRuntimeDependencyCount')
        Assert-FileRow "Receipt fixture package $version package" $package.package $false
        Assert-FileRow "Receipt fixture package $version packageReport" $package.packageReport $false
        if ($null -ne $package.package) { Assert-RepoRelativePath "Receipt fixture package $version package path" $package.package.path }
        if ($null -ne $package.packageReport) { Assert-RepoRelativePath "Receipt fixture package $version packageReport path" $package.packageReport.path }
        Assert-Sha256 "Receipt fixture package $version manifestSha256" $package.manifestSha256
        Assert-Sha256 "Receipt fixture package $version entryAssemblySha256" $package.entryAssemblySha256
        Assert-Sha256 "Receipt fixture package $version advancedReferenceReceiptSha256" $package.advancedReferenceReceiptSha256
        Assert-Integer "Receipt fixture package $version fileCount" $package.fileCount 6
        Assert-Integer "Receipt fixture package $version bundledNativeRuntimeDependencyCount" $package.bundledNativeRuntimeDependencyCount 0
    }
    if (@($artifacts.fixturePackages).Count -eq 2) {
        if ([string]$artifacts.fixturePackages[0].package.sha256 -eq [string]$artifacts.fixturePackages[1].package.sha256) { Add-Failure 'Fixture v0.1.0 and v0.1.1 package hashes must differ.' }
        if ([string]$artifacts.fixturePackages[0].manifestSha256 -eq [string]$artifacts.fixturePackages[1].manifestSha256) { Add-Failure 'Fixture v0.1.0 and v0.1.1 manifest hashes must differ.' }
        if ([string]$artifacts.fixturePackages[0].entryAssemblySha256 -eq [string]$artifacts.fixturePackages[1].entryAssemblySha256) { Add-Failure 'Fixture v0.1.0 and v0.1.1 entry assembly hashes must differ.' }
        if ([string]$artifacts.fixturePackages[0].advancedReferenceReceiptSha256 -eq [string]$artifacts.fixturePackages[1].advancedReferenceReceiptSha256) { Add-Failure 'Fixture v0.1.0 and v0.1.1 reference receipt hashes must differ.' }
    }

    $cases = @($receipt.cases)
    Assert-ExactSequence 'Receipt case order' @($cases | ForEach-Object { [string]$_.caseId }) @($caseExpectations | ForEach-Object { [string]$_.caseId })
    for ($index = 0; $index -lt [Math]::Min($cases.Count, $caseExpectations.Count); $index++) {
        $caseTimes.Add((Assert-Case $cases[$index] $caseExpectations[$index]))
        if ($null -ne $cases[$index].evidence.reportReleaseManifest) {
            Assert-String "$($caseExpectations[$index].caseId) embedded release manifest binding" $cases[$index].evidence.reportReleaseManifest.sha256 ([string]$runtime.releaseManifestSha256)
        }
    }
    if ($cases.Count -ge 5 -and $null -ne $cases[0].evidence.runnerReceipt) {
        $runnerPath = [string]$cases[0].evidence.runnerReceipt.path
        $runnerSha = [string]$cases[0].evidence.runnerReceipt.sha256
        $runnerLength = [long]$cases[0].evidence.runnerReceipt.length
        for ($index = 1; $index -lt 5; $index++) {
            Assert-String "Cold-runner receipt path binding at case $index" $cases[$index].evidence.runnerReceipt.path $runnerPath
            Assert-String "Cold-runner receipt hash binding at case $index" $cases[$index].evidence.runnerReceipt.sha256 $runnerSha
            Assert-Integer "Cold-runner receipt length binding at case $index" $cases[$index].evidence.runnerReceipt.length $runnerLength
        }
    }
    Assert-ExactSet 'Receipt GAME-SMOKE run roots' @($caseTimes | ForEach-Object { [string]$_.runRoot }) @($caseTimes | ForEach-Object { [string]$_.runRoot } | Sort-Object -Unique)
    for ($index = 1; $index -lt $caseTimes.Count; $index++) {
        $previous = $caseTimes[$index - 1]
        $current = $caseTimes[$index]
        if ($null -ne $previous.completed -and $null -ne $current.started -and $current.started -lt $previous.completed) {
            Add-Failure "Receipt case '$($caseExpectations[$index].caseId)' started before the prior case completed."
        }
    }
    $validCompletionTimes = @($caseTimes | Where-Object { $null -ne $_.completed } | ForEach-Object { $_.completed } | Sort-Object)
    if ($validCompletionTimes.Count -eq 9 -and $null -ne $captured) {
        $latestCompletion = $validCompletionTimes[-1]
        if ($captured.UtcTicks -ne $latestCompletion.UtcTicks) {
            Add-Failure 'Receipt capturedAtUtc must equal the latest case completedAtUtc, not builder wall-clock time.'
        }
    }
    $validStartTimes = @($caseTimes | Where-Object { $null -ne $_.started } | ForEach-Object { $_.started } | Sort-Object)
    if ($validStartTimes.Count -gt 0 -and $null -ne $buildTime -and $buildTime -gt $validStartTimes[0]) {
        Add-Failure 'Runtime release buildTimeUtc is later than the first matrix case start.'
    }
    if ($implementationCommit -cmatch '^[0-9a-f]{40}$' -and $null -ne $captured) {
        try {
            $implementationCommitTimeText = ([string](Invoke-GitLines @('show', '-s', '--format=%cI', $implementationCommit) | Select-Object -First 1)).Trim()
            $implementationCommitTime = ConvertTo-UtcInstant 'Implementation commit time' $implementationCommitTimeText
            if ($null -ne $implementationCommitTime -and $captured -lt $implementationCommitTime) {
                Add-Failure 'Receipt capture precedes its implementation commit.'
            }
            if ($null -ne $implementationCommitTime -and $null -ne $buildTime -and $buildTime -lt $implementationCommitTime) {
                Add-Failure 'Runtime release buildTimeUtc precedes implementationCommit.'
            }
        }
        catch { Add-Failure 'Unable to read the implementation commit time.' }
    }

    Assert-Sha256 'Receipt matrixSha256' $receipt.matrixSha256
    $matrixJson = ConvertTo-Json -InputObject @($receipt.cases) -Depth 100 -Compress
    $computedMatrixSha = Get-TextSha256 $matrixJson
    Assert-String 'Receipt recomputed matrixSha256' $receipt.matrixSha256 $computedMatrixSha
}

$repoPrefix = [System.IO.Path]::GetFullPath($repo).TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
if ($null -ne $receipt -and $receiptFullPath.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    $receiptRelativePath = Normalize-Path $receiptFullPath.Substring($repoPrefix.Length)
    if (Test-Git @('ls-files', '--error-unmatch', '--', $receiptRelativePath)) {
        $status = @(Invoke-GitLines @('status', '--porcelain', '--', $receiptRelativePath))
        if ($status.Count -eq 0) {
            try {
                $containingCommit = ([string](Invoke-GitLines @('log', '-1', '--format=%H', '--', $receiptRelativePath) | Select-Object -First 1)).Trim()
                $containingCommitTimeText = ([string](Invoke-GitLines @('show', '-s', '--format=%cI', $containingCommit) | Select-Object -First 1)).Trim()
                $containingCommitTime = ConvertTo-UtcInstant 'Receipt-containing commit time' $containingCommitTimeText
                if ($null -ne $captured -and $null -ne $containingCommitTime -and $captured -gt $containingCommitTime) {
                    Add-Failure 'Receipt capturedAtUtc is later than the commit containing the current receipt.'
                }
                if (-not [string]::IsNullOrWhiteSpace($implementationCommit) -and -not (Test-Git @('merge-base', '--is-ancestor', $implementationCommit, $containingCommit))) {
                    Add-Failure 'Receipt-containing commit does not descend from implementationCommit.'
                }
                if (-not (Test-Git @('merge-base', '--is-ancestor', $containingCommit, 'HEAD'))) {
                    Add-Failure 'Receipt-containing commit is not an ancestor of current HEAD.'
                }
            }
            catch { Add-Failure "Unable to validate receipt-containing commit: $($_.Exception.Message)" }
        }
        else {
            Write-Warning 'Runtime matrix receipt is tracked but dirty; containing-commit time validation must be rerun after commit.'
        }
    }
}

if ($VerifyRawEvidence) {
    if ([string]::IsNullOrWhiteSpace($EvidenceMapPath)) {
        Add-Failure '-VerifyRawEvidence requires -EvidenceMapPath.'
    }
    elseif (-not (Test-Path -LiteralPath (Resolve-File $EvidenceMapPath $EvidenceMapPath) -PathType Leaf)) {
        Add-Failure "Raw evidence map is missing: $EvidenceMapPath"
    }
    else {
        $builderPath = Join-Path $PSScriptRoot 'build-batch6-g2-runtime-matrix-receipt.ps1'
        if (-not (Test-Path -LiteralPath $builderPath -PathType Leaf)) {
            Add-Failure "Runtime matrix receipt builder is missing: $builderPath"
        }
        else {
            $tempBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
                [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT)
            }
            else { Join-Path $repo 'tmp\test-runs' }
            if (-not (Test-Path -LiteralPath $tempBase -PathType Container)) { New-Item -ItemType Directory -Path $tempBase -Force | Out-Null }
            $candidatePath = Join-Path $tempBase ('batch6-g2-runtime-matrix-candidate-' + [Guid]::NewGuid().ToString('N') + '.json')
            try {
                $windowsPowerShell = Get-Command powershell.exe -ErrorAction Stop
                $builderOutput = @(& $windowsPowerShell.Source -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $builderPath -EvidenceMapPath (Resolve-File $EvidenceMapPath $EvidenceMapPath) -OutputPath $candidatePath 2>&1)
                $builderExitCode = $LASTEXITCODE
                if ($builderExitCode -ne 0) {
                    Add-Failure "Raw-evidence receipt rebuild failed with exit code $builderExitCode`: $($builderOutput -join [Environment]::NewLine)"
                }
                elseif (-not (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
                    Add-Failure 'Raw-evidence receipt builder did not create its requested candidate file.'
                }
                else {
                    try { $candidate = ConvertFrom-ReceiptJson (Read-Utf8 $candidatePath) }
                    catch { Add-Failure "Raw-evidence candidate is not parseable JSON: $($_.Exception.Message)"; $candidate = $null }
                    if ($null -ne $candidate -and $null -ne $receipt) {
                        $receiptCanonical = ConvertTo-CanonicalJson $receipt
                        $candidateCanonical = ConvertTo-CanonicalJson $candidate
                        if (-not $receiptCanonical.Equals($candidateCanonical, [StringComparison]::Ordinal)) {
                            Add-Failure 'Raw-evidence rebuild does not exactly match the committed receipt after canonical JSON normalization.'
                        }
                    }
                    foreach ($probe in @('GenericSmokeExit', 'AutoFishingIdentity', 'PackageZipEntry', 'RunnerReceipt')) {
                        $probePath = Join-Path $tempBase ('batch6-g2-runtime-matrix-mutation-' + $probe + '-' + [Guid]::NewGuid().ToString('N') + '.json')
                        try {
                            $previousErrorAction = $ErrorActionPreference
                            try {
                                $ErrorActionPreference = 'Continue'
                                $probeOutput = @(& $windowsPowerShell.Source -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $builderPath -EvidenceMapPath (Resolve-File $EvidenceMapPath $EvidenceMapPath) -OutputPath $probePath -MutationProbe $probe 2>&1)
                                $probeExitCode = $LASTEXITCODE
                            }
                            finally { $ErrorActionPreference = $previousErrorAction }
                            if ($probeExitCode -eq 0) { Add-Failure "Raw-evidence mutation probe '$probe' unexpectedly passed." }
                            if (Test-Path -LiteralPath $probePath -PathType Leaf) { Add-Failure "Raw-evidence mutation probe '$probe' wrote an accepted candidate." }
                        }
                        catch { Add-Failure "Raw-evidence mutation probe '$probe' could not run: $($_.Exception.Message)" }
                        finally { if (Test-Path -LiteralPath $probePath -PathType Leaf) { Remove-Item -LiteralPath $probePath -Force } }
                    }
                }
            }
            catch { Add-Failure "Raw-evidence receipt rebuild failed: $($_.Exception.Message)" }
            finally {
                if (Test-Path -LiteralPath $candidatePath -PathType Leaf) { Remove-Item -LiteralPath $candidatePath -Force }
            }
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }
    exit 1
}

$mode = if ($VerifyRawEvidence) { 'receipt+raw-evidence-rebuild' } else { 'receipt-only' }
Write-Host "Batch 6 G2 runtime matrix receipt PASSED: mode=$mode; implementation=$implementationCommit; cases=9; matrix=$($receipt.matrixSha256)."
exit 0
