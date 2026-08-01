param(
    [string] $ContractPath = '',
    [string] $SemanticAllowancesPath = '',
    [string] $SemanticAllowancesSchemaPath = '',
    [string] $BaselineGitRef = '',
    [string] $TargetGitRef = '',
    [string] $OutputPath = '',
    [switch] $Worktree,
    [string] $ClassificationMutationProbePath = '',
    [ValidateSet('UnmappedMandatory', 'ProductNative', 'UnauditedMandatory')]
    [string] $ClassificationMutationProbeKind = 'UnmappedMandatory',
    [ValidateSet('None', 'ExtraAllowance', 'TargetBlob', 'DiffSha256')]
    [string] $SemanticAllowanceMutationProbe = 'None'
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

function Normalize-Path([string] $path) {
    return ([string]$path).Trim().TrimStart([char]'\', [char]'/').Replace('\', '/').TrimEnd('/')
}

function Test-AuditedSourcePath([string] $path) {
    return ($path.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) -or
        $path.EndsWith('.csproj', [StringComparison]::OrdinalIgnoreCase))
}

function Resolve-RepoFile([string] $value, [string] $defaultRelativePath) {
    $candidate = if ([string]::IsNullOrWhiteSpace($value)) { $defaultRelativePath } else { $value }
    if (-not [System.IO.Path]::IsPathRooted($candidate)) { $candidate = Join-Path $repo $candidate }
    return [System.IO.Path]::GetFullPath($candidate)
}

function Get-RepoRelativePath([string] $fullPath) {
    $repoPrefix = [System.IO.Path]::GetFullPath($repo).TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
    $resolved = [System.IO.Path]::GetFullPath($fullPath)
    if (-not $resolved.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "G2 ownership authority escaped the repository: $resolved"
    }
    return Normalize-Path $resolved.Substring($repoPrefix.Length)
}

function Assert-ExactProperties([object] $value, [string[]] $expected, [string] $label) {
    if ($null -eq $value) { throw "$label is missing." }
    $actual = @($value.PSObject.Properties | ForEach-Object { [string]$_.Name })
    Assert-ExactSet $actual $expected "$label properties"
}

function Assert-ExactSet([string[]] $actual, [string[]] $expected, [string] $label) {
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $expectedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $actualHasDuplicate = $false
    $expectedHasDuplicate = $false
    foreach ($value in $actual) { if (-not $actualSet.Add([string]$value)) { $actualHasDuplicate = $true } }
    foreach ($value in $expected) { if (-not $expectedSet.Add([string]$value)) { $expectedHasDuplicate = $true } }
    $missing = @($expectedSet | Where-Object { -not $actualSet.Contains([string]$_) })
    $extra = @($actualSet | Where-Object { -not $expectedSet.Contains([string]$_) })
    if ($missing.Count -gt 0 -or $extra.Count -gt 0 -or $actualHasDuplicate -or $expectedHasDuplicate) {
        throw "$label set mismatch; missing=[$($missing -join ', ')]; extra=[$($extra -join ', ')]; actualDuplicate=$actualHasDuplicate; expectedDuplicate=$expectedHasDuplicate."
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
        try {
            $ErrorActionPreference = 'Continue'
            [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
            $details = @(& git -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false @arguments 2>&1)
        }
        finally {
            [Console]::OutputEncoding = $previousOutputEncoding
            $ErrorActionPreference = $previousErrorAction
        }
        throw "git $($arguments -join ' ') failed: $($details -join [Environment]::NewLine)"
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Invoke-GitWorktreeLines([string[]] $arguments) {
    $previousErrorAction = $ErrorActionPreference
    $previousOutputEncoding = [Console]::OutputEncoding
    try {
        $ErrorActionPreference = 'Continue'
        [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
        $output = @(& git -c core.quotepath=false @arguments 2>$null)
        $exitCode = $LASTEXITCODE
    }
    finally {
        [Console]::OutputEncoding = $previousOutputEncoding
        $ErrorActionPreference = $previousErrorAction
    }
    if ($exitCode -ne 0) {
        try {
            $ErrorActionPreference = 'Continue'
            [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
            $details = @(& git -c core.quotepath=false @arguments 2>&1)
        }
        finally {
            [Console]::OutputEncoding = $previousOutputEncoding
            $ErrorActionPreference = $previousErrorAction
        }
        throw "git $($arguments -join ' ') failed: $($details -join [Environment]::NewLine)"
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Test-GitObject([string] $objectName) {
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & git cat-file -e $objectName 2>$null
        return $LASTEXITCODE -eq 0
    }
    finally { $ErrorActionPreference = $previousErrorAction }
}

function Get-StringSha256([string] $value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($value)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally { $sha.Dispose() }
}

function Get-NormalizedTextFileSha256([string] $path) {
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    return Get-StringSha256 $normalized
}

function Get-PhysicalLineCount([string] $path) {
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    if ($text.Length -eq 0) { return 0 }
    $lineBreakCount = [Text.RegularExpressions.Regex]::Matches($text, "\r\n|\n|\r").Count
    if ($text.EndsWith("`r", [StringComparison]::Ordinal) -or $text.EndsWith("`n", [StringComparison]::Ordinal)) {
        return $lineBreakCount
    }
    return $lineBreakCount + 1
}

function Test-PathUnderRoot([string] $path, [string] $root) {
    $normalizedPath = Normalize-Path $path
    $normalizedRoot = Normalize-Path $root
    return $normalizedPath.Equals($normalizedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $normalizedPath.StartsWith($normalizedRoot + '/', [StringComparison]::OrdinalIgnoreCase)
}

function Test-PathUnderAnyRoot([string] $path, [object[]] $roots) {
    return @($roots | Where-Object { Test-PathUnderRoot $path ([string]$_) }).Count -gt 0
}

function Test-GitAncestor([string] $ancestor, [string] $descendant) {
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & git merge-base --is-ancestor $ancestor $descendant 2>$null
        return $LASTEXITCODE -eq 0
    }
    finally { $ErrorActionPreference = $previousErrorAction }
}

function Resolve-NonMandatoryOwnership([string] $path, [object] $audit) {
    if (Test-PathUnderAnyRoot $path @($audit.mandatoryRuntimeRoots)) {
        throw "G2 mandatory Runtime source requires an explicit semantic allowance: $path"
    }
    foreach ($root in @($audit.syntheticProductRoots)) {
        if (Test-PathUnderRoot $path ([string]$root)) { return 'SyntheticProductNative' }
    }
    if (Test-PathUnderRoot $path ([string]$audit.stableAbstractionsRoot)) { return 'StableAbstractions' }
    if (Test-PathUnderRoot $path ([string]$audit.sharedNativeRoot)) { return 'SharedNative' }
    foreach ($root in @($audit.platformSourceRoots)) {
        if (Test-PathUnderRoot $path ([string]$root)) { return 'Platform' }
    }
    throw "G2 ownership audit cannot classify changed source file: $path"
}

function Get-BlobAtCommit([string] $commit, [string] $path) {
    $objectName = $commit + ':' + (Normalize-Path $path)
    if (-not (Test-GitObject $objectName)) { return $null }
    return ([string](Invoke-GitLines @('rev-parse', $objectName) | Select-Object -First 1)).Trim()
}

function Get-WorktreeBlob([string] $path) {
    $fullPath = Join-Path $repo (Normalize-Path $path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { return $null }
    $normalizedPath = Normalize-Path $path
    return ([string](Invoke-GitWorktreeLines @('hash-object', "--path=$normalizedPath", '--', $normalizedPath) | Select-Object -First 1)).Trim()
}

function Get-DiffLines([string] $baseline, [string] $target, [string] $path, [bool] $useWorktree, [bool] $isUntracked) {
    if ($isUntracked) {
        $fullPath = Join-Path $repo (Normalize-Path $path)
        $blob = Get-WorktreeBlob $path
        return @("DTMAPI-UNTRACKED $path", "blob $blob", "physicalLines $(Get-PhysicalLineCount $fullPath)")
    }
    $arguments = New-Object System.Collections.Generic.List[string]
    foreach ($value in @('diff', '--no-ext-diff', '--no-textconv', '--no-renames', '--unified=0', $baseline)) { $arguments.Add($value) }
    if (-not $useWorktree) { $arguments.Add($target) }
    $arguments.Add('--')
    $arguments.Add((Normalize-Path $path))
    if ($useWorktree) { return @(Invoke-GitWorktreeLines $arguments.ToArray()) }
    return @(Invoke-GitLines $arguments.ToArray())
}

function Get-NumStat([string] $baseline, [string] $target, [string] $path, [bool] $useWorktree, [bool] $isUntracked) {
    if ($isUntracked) {
        return [ordered]@{ added = Get-PhysicalLineCount (Join-Path $repo (Normalize-Path $path)); deleted = 0 }
    }
    $arguments = New-Object System.Collections.Generic.List[string]
    foreach ($value in @('diff', '--numstat', '--no-renames', $baseline)) { $arguments.Add($value) }
    if (-not $useWorktree) { $arguments.Add($target) }
    $arguments.Add('--')
    $arguments.Add((Normalize-Path $path))
    $rows = @(if ($useWorktree) {
        Invoke-GitWorktreeLines $arguments.ToArray() | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    }
    else {
        Invoke-GitLines $arguments.ToArray() | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    })
    if ($rows.Count -ne 1) { throw "Expected one numstat row for '$path'; found $($rows.Count)." }
    $parts = $rows[0].Split("`t")
    if ($parts.Count -lt 3 -or $parts[0] -eq '-' -or $parts[1] -eq '-') { throw "G2 source audit requires a text numstat row for '$path': $($rows[0])" }
    return [ordered]@{ added = [int]$parts[0]; deleted = [int]$parts[1] }
}

if ($Worktree -and -not [string]::IsNullOrWhiteSpace($TargetGitRef)) {
    throw 'Use either -Worktree or -TargetGitRef, not both.'
}

$contractFullPath = Resolve-RepoFile $ContractPath 'tools\release\contracts\batch6-g2-advanced-synthetic-contract.json'
$contract = [System.IO.File]::ReadAllText($contractFullPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$audit = $contract.ownershipAudit
if ($null -eq $audit) { throw 'G2 contract is missing ownershipAudit.' }

$allowancePathFromAudit = @($audit.PSObject.Properties | Where-Object { $_.Name -eq 'semanticAllowancePath' } | ForEach-Object { [string]$_.Value })
$allowanceSchemaPathFromAudit = @($audit.PSObject.Properties | Where-Object { $_.Name -eq 'semanticAllowanceSchemaPath' } | ForEach-Object { [string]$_.Value })
$allowanceDefaultPath = if ($allowancePathFromAudit.Count -eq 1 -and -not [string]::IsNullOrWhiteSpace($allowancePathFromAudit[0])) {
    $allowancePathFromAudit[0]
}
else {
    'tools\release\contracts\batch6-g2-ownership-semantic-allowances.json'
}
$allowanceSchemaDefaultPath = if ($allowanceSchemaPathFromAudit.Count -eq 1 -and -not [string]::IsNullOrWhiteSpace($allowanceSchemaPathFromAudit[0])) {
    $allowanceSchemaPathFromAudit[0]
}
else {
    'tools\release\contracts\batch6-g2-ownership-semantic-allowances.schema.json'
}
$allowanceFullPath = Resolve-RepoFile $SemanticAllowancesPath $allowanceDefaultPath
$allowanceSchemaFullPath = Resolve-RepoFile $SemanticAllowancesSchemaPath $allowanceSchemaDefaultPath
$allowanceRelativePath = Get-RepoRelativePath $allowanceFullPath
$allowanceSchemaRelativePath = Get-RepoRelativePath $allowanceSchemaFullPath
if (-not (Test-Path -LiteralPath $allowanceFullPath -PathType Leaf)) { throw "G2 ownership semantic allowance contract is missing: $allowanceRelativePath" }
if (-not (Test-Path -LiteralPath $allowanceSchemaFullPath -PathType Leaf)) { throw "G2 ownership semantic allowance schema is missing: $allowanceSchemaRelativePath" }

$allowanceText = [System.IO.File]::ReadAllText($allowanceFullPath, [System.Text.Encoding]::UTF8)
$allowanceContract = $allowanceText | ConvertFrom-Json
$allowanceSchema = [System.IO.File]::ReadAllText($allowanceSchemaFullPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$allowanceRootFields = @('schemaVersion', 'contractId', 'schemaPath', 'architectureAuthority', 'reviewAuthority', 'baselineCommit', 'implementationCommit', 'allowances')
$allowanceRowFields = @('path', 'classification', 'review', 'reason', 'expectedTargetBlob', 'expectedDiffSha256')
Assert-ExactProperties $allowanceContract $allowanceRootFields 'G2 ownership semantic allowance contract'
Assert-ExactProperties $allowanceSchema @('$schema', '$id', 'title', 'type', 'additionalProperties', 'required', 'properties', '$defs') 'G2 ownership semantic allowance schema'
if ([string]$allowanceSchema.'$schema' -ne 'https://json-schema.org/draft/2020-12/schema') { throw 'G2 ownership semantic allowance schema must use JSON Schema draft 2020-12.' }
if ([string]$allowanceSchema.'$id' -ne 'https://dtmapi.local/contracts/batch6-g2-ownership-semantic-allowances.schema.json') { throw 'G2 ownership semantic allowance schema has an unexpected $id.' }
if ([string]$allowanceSchema.type -ne 'object' -or [bool]$allowanceSchema.additionalProperties) { throw 'G2 ownership semantic allowance schema root must be a closed object.' }
Assert-ExactSet @($allowanceSchema.required | ForEach-Object { [string]$_ }) $allowanceRootFields 'G2 ownership semantic allowance schema root required fields'
$allowanceSchemaRow = $allowanceSchema.'$defs'.allowance
if ($null -eq $allowanceSchemaRow -or [string]$allowanceSchemaRow.type -ne 'object' -or [bool]$allowanceSchemaRow.additionalProperties) { throw 'G2 ownership semantic allowance schema row must be a closed object.' }
Assert-ExactSet @($allowanceSchemaRow.required | ForEach-Object { [string]$_ }) $allowanceRowFields 'G2 ownership semantic allowance schema row required fields'
Assert-ExactSet @($allowanceSchemaRow.properties.classification.enum | ForEach-Object { [string]$_ }) @('Platform', 'SharedNative', 'ProductNative', 'StableAbstractions') 'G2 ownership semantic allowance schema classifications'

if ([int]$allowanceContract.schemaVersion -ne 1) { throw "G2 ownership semantic allowance schemaVersion must be 1; found $($allowanceContract.schemaVersion)." }
if ([string]$allowanceContract.contractId -ne [string]$contract.contractId) { throw 'G2 ownership semantic allowance contractId does not match the G2 contract.' }
if ([string]$allowanceContract.schemaPath -ne $allowanceSchemaRelativePath) { throw "G2 ownership semantic allowance schema path mismatch: $($allowanceContract.schemaPath) != $allowanceSchemaRelativePath" }
if ([string]$allowanceContract.architectureAuthority -ne [string]$contract.authorities.architecture) { throw 'G2 ownership semantic allowance architecture authority does not match the G2 contract.' }
if ([string]$allowanceContract.reviewAuthority -ne [string]$contract.authorities.review) { throw 'G2 ownership semantic allowance review authority does not match the G2 contract.' }

foreach ($binding in @(
    @{ Name = 'semanticAllowanceSha256'; Path = $allowanceFullPath },
    @{ Name = 'semanticAllowanceSchemaSha256'; Path = $allowanceSchemaFullPath }
)) {
    $bindingProperty = @($audit.PSObject.Properties | Where-Object { $_.Name -eq [string]$binding.Name })
    if ($bindingProperty.Count -eq 1) {
        $expectedHash = [string]$bindingProperty[0].Value
        $actualHash = Get-NormalizedTextFileSha256 ([string]$binding.Path)
        if (-not $actualHash.Equals($expectedHash, [StringComparison]::Ordinal)) {
            throw "G2 ownership $($binding.Name) mismatch: expected=$expectedHash actual=$actualHash"
        }
    }
}

$baselineRef = if ([string]::IsNullOrWhiteSpace($BaselineGitRef)) { [string]$audit.baselineCommit } else { $BaselineGitRef }
$baselineCommit = ([string](Invoke-GitLines @('rev-parse', "$baselineRef^{commit}") | Select-Object -First 1)).Trim()
$useWorktree = $Worktree -or [string]::IsNullOrWhiteSpace($TargetGitRef)
$targetCommit = $null
if (-not $useWorktree) {
    $targetCommit = ([string](Invoke-GitLines @('rev-parse', "$TargetGitRef^{commit}") | Select-Object -First 1)).Trim()
}

if ([string]$allowanceContract.baselineCommit -ne $baselineCommit) {
    throw "G2 ownership semantic allowance baseline mismatch: $($allowanceContract.baselineCommit) != $baselineCommit"
}
$allowanceImplementationCommit = [string]$allowanceContract.implementationCommit
if ($allowanceImplementationCommit -notmatch '^[0-9a-f]{40}$' -or -not (Test-GitObject "$allowanceImplementationCommit^{commit}")) {
    throw "G2 ownership semantic allowance implementation commit is invalid: $allowanceImplementationCommit"
}
$headCommit = ([string](Invoke-GitLines @('rev-parse', 'HEAD^{commit}') | Select-Object -First 1)).Trim()
if ($useWorktree) {
    if (-not (Test-GitAncestor $allowanceImplementationCommit $headCommit)) {
        throw "G2 ownership semantic allowance implementation commit is not an ancestor of worktree HEAD: $allowanceImplementationCommit -> $headCommit"
    }
}
elseif ($targetCommit -ne $allowanceImplementationCommit) {
    throw "G2 ownership semantic allowance is bound to implementation $allowanceImplementationCommit, not requested target $targetCommit."
}

$allowanceByPath = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
$allowanceRows = @($allowanceContract.allowances)
if ($allowanceRows.Count -eq 0) { throw 'G2 ownership semantic allowance contract must contain at least one allowance.' }
$allowedMandatoryClassifications = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
foreach ($value in @('Platform', 'SharedNative', 'ProductNative', 'StableAbstractions')) { [void]$allowedMandatoryClassifications.Add($value) }
$previousAllowancePath = $null
foreach ($allowance in $allowanceRows) {
    Assert-ExactProperties $allowance $allowanceRowFields 'G2 ownership semantic allowance row'
    $path = [string]$allowance.path
    $normalizedPath = Normalize-Path $path
    if (-not $path.Equals($normalizedPath, [StringComparison]::Ordinal) -or [System.IO.Path]::IsPathRooted($path) -or @($path.Split('/') | Where-Object { $_ -eq '..' }).Count -gt 0 -or -not (Test-AuditedSourcePath $path)) {
        throw "G2 ownership semantic allowance path is not a canonical relative audited source/build-input path: $path"
    }
    if (-not (Test-PathUnderAnyRoot $path @($audit.mandatoryRuntimeRoots))) {
        throw "G2 ownership semantic allowance escaped mandatory Runtime roots: $path"
    }
    if ($null -ne $previousAllowancePath -and [StringComparer]::Ordinal.Compare($previousAllowancePath, $path) -ge 0) {
        throw "G2 ownership semantic allowances must be unique and ordinally sorted: $previousAllowancePath then $path"
    }
    $classification = [string]$allowance.classification
    if (-not $allowedMandatoryClassifications.Contains($classification)) {
        throw "G2 ownership semantic allowance has unsupported classification '$classification': $path"
    }
    if ([string]$allowance.review -ne [string]$allowanceContract.reviewAuthority) { throw "G2 ownership semantic allowance review mismatch: $path" }
    $reason = [string]$allowance.reason
    if ([string]::IsNullOrWhiteSpace($reason) -or $reason.Length -gt 500 -or $reason.Contains("`r") -or $reason.Contains("`n")) { throw "G2 ownership semantic allowance reason is invalid: $path" }
    if ([string]$allowance.expectedTargetBlob -notmatch '^[0-9a-f]{40,64}$') { throw "G2 ownership semantic allowance target blob is invalid: $path" }
    if ([string]$allowance.expectedDiffSha256 -notmatch '^[0-9A-F]{64}$') { throw "G2 ownership semantic allowance diff SHA-256 is invalid: $path" }
    $allowanceByPath.Add($path, $allowance)
    $previousAllowancePath = $path
}

if ($SemanticAllowanceMutationProbe -eq 'ExtraAllowance') {
    $probePath = 'src/DTMAPI.Core/Batch6G2ExtraSemanticAllowanceMutationProbe.cs'
    $allowanceByPath.Add($probePath, [pscustomobject]@{
        path = $probePath
        classification = 'Platform'
        review = [string]$allowanceContract.reviewAuthority
        reason = 'In-memory mutation probe.'
        expectedTargetBlob = ('0' * 40) -join ''
        expectedDiffSha256 = ('0' * 64) -join ''
    })
}
elseif ($SemanticAllowanceMutationProbe -eq 'TargetBlob' -or $SemanticAllowanceMutationProbe -eq 'DiffSha256') {
    $probeAllowance = $allowanceByPath[[string]$allowanceRows[0].path]
    if ($SemanticAllowanceMutationProbe -eq 'TargetBlob') { $probeAllowance.expectedTargetBlob = ('0' * 40) -join '' }
    else { $probeAllowance.expectedDiffSha256 = ('0' * 64) -join '' }
}

$roots = @($audit.auditSourceRoots | ForEach-Object { Normalize-Path ([string]$_) })
$nameArguments = New-Object System.Collections.Generic.List[string]
foreach ($value in @('diff', '--name-only', '--no-renames', $baselineCommit)) { $nameArguments.Add($value) }
if (-not $useWorktree) { $nameArguments.Add($targetCommit) }
$nameArguments.Add('--')
foreach ($root in $roots) { $nameArguments.Add($root) }
$allChangedPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$changedPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$nameRows = if ($useWorktree) { @(Invoke-GitWorktreeLines $nameArguments.ToArray()) } else { @(Invoke-GitLines $nameArguments.ToArray()) }
foreach ($path in $nameRows) {
    $normalized = Normalize-Path $path
    [void]$allChangedPaths.Add($normalized)
    if (Test-AuditedSourcePath $normalized) { [void]$changedPaths.Add($normalized) }
}

$untrackedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
if ($useWorktree) {
    $untrackedArguments = New-Object System.Collections.Generic.List[string]
    foreach ($value in @('ls-files', '--others', '--exclude-standard', '--')) { $untrackedArguments.Add($value) }
    foreach ($root in $roots) { $untrackedArguments.Add($root) }
    foreach ($path in @(Invoke-GitLines $untrackedArguments.ToArray())) {
        $normalized = Normalize-Path $path
        [void]$allChangedPaths.Add($normalized)
        if (Test-AuditedSourcePath $normalized) {
            [void]$untrackedSet.Add($normalized)
            [void]$changedPaths.Add($normalized)
        }
    }
}

foreach ($path in @($allChangedPaths)) {
    if ((Test-PathUnderAnyRoot ([string]$path) @($audit.mandatoryRuntimeRoots)) -and -not (Test-AuditedSourcePath ([string]$path))) {
        throw "G2 mandatory Runtime changed path has an unaudited extension: $path"
    }
}

$mutationProbePath = $null
if (-not [string]::IsNullOrWhiteSpace($ClassificationMutationProbePath)) {
    $mutationProbePath = Normalize-Path $ClassificationMutationProbePath
    if (-not $ClassificationMutationProbePath.Equals($mutationProbePath, [StringComparison]::Ordinal) -or
        [System.IO.Path]::IsPathRooted($ClassificationMutationProbePath) -or
        @($ClassificationMutationProbePath.Split('/') | Where-Object { $_ -eq '..' }).Count -gt 0) {
        throw "G2 ownership classification mutation probe is not a canonical relative path: $ClassificationMutationProbePath"
    }
    if (-not (Test-PathUnderAnyRoot $mutationProbePath @($audit.mandatoryRuntimeRoots))) {
        throw "G2 ownership classification mutation probe must be under a mandatory Runtime root: $mutationProbePath"
    }
    if ($allChangedPaths.Contains($mutationProbePath) -or $allowanceByPath.ContainsKey($mutationProbePath)) {
        throw "G2 ownership classification mutation probe must name a new, unallowed source path: $mutationProbePath"
    }
    if ($ClassificationMutationProbeKind -eq 'UnauditedMandatory') {
        if (Test-AuditedSourcePath $mutationProbePath) { throw "G2 unaudited mandatory mutation probe must use an extension outside .cs/.csproj: $mutationProbePath" }
        throw "G2 mandatory Runtime changed path has an unaudited extension: $mutationProbePath"
    }
    if (-not $mutationProbePath.EndsWith('.cs', [StringComparison]::Ordinal)) {
        throw "G2 ownership classification mutation probe must use a C# path for $ClassificationMutationProbeKind`: $mutationProbePath"
    }
}

$mandatoryPathsRequiringAllowance = New-Object 'System.Collections.Generic.List[string]'
foreach ($path in @($changedPaths)) {
    if (Test-PathUnderAnyRoot ([string]$path) @($audit.mandatoryRuntimeRoots)) { $mandatoryPathsRequiringAllowance.Add([string]$path) }
}
if ($null -ne $mutationProbePath -and $ClassificationMutationProbeKind -eq 'UnmappedMandatory') {
    $mandatoryPathsRequiringAllowance.Add($mutationProbePath)
}
Assert-ExactSet $mandatoryPathsRequiringAllowance.ToArray() @($allowanceByPath.Keys) 'G2 mandatory Runtime semantic allowance coverage'

if ($null -ne $mutationProbePath -and $ClassificationMutationProbeKind -eq 'ProductNative') {
    [void]$changedPaths.Add($mutationProbePath)
}

[string[]]$orderedPaths = @($changedPaths)
[Array]::Sort($orderedPaths, [StringComparer]::Ordinal)
$files = New-Object System.Collections.Generic.List[object]
$totalAdded = 0
$totalDeleted = 0
foreach ($path in $orderedPaths) {
    $isProductNativeProbe = $null -ne $mutationProbePath -and
        $ClassificationMutationProbeKind -eq 'ProductNative' -and
        $path.Equals($mutationProbePath, [StringComparison]::Ordinal)
    if ($isProductNativeProbe) {
        $numStat = [ordered]@{ added = 1; deleted = 0 }
        $diffText = "DTMAPI-G2-PRODUCT-NATIVE-MUTATION-PROBE $path`n"
        $baselineBlob = $null
        $targetBlob = $null
        $ownership = 'ProductNative'
    }
    else {
        $isUntracked = $untrackedSet.Contains($path)
        $numStat = Get-NumStat $baselineCommit $targetCommit $path $useWorktree $isUntracked
        $diffLines = @(Get-DiffLines $baselineCommit $targetCommit $path $useWorktree $isUntracked)
        $diffText = if ($diffLines.Count -eq 0) { '' } else { ($diffLines -join "`n") + "`n" }
        $baselineBlob = Get-BlobAtCommit $baselineCommit $path
        $targetBlob = if ($useWorktree) { Get-WorktreeBlob $path } else { Get-BlobAtCommit $targetCommit $path }
        if (Test-PathUnderAnyRoot $path @($audit.mandatoryRuntimeRoots)) {
            if (-not $allowanceByPath.ContainsKey($path)) { throw "G2 mandatory Runtime source is missing a semantic allowance: $path" }
            $allowance = $allowanceByPath[$path]
            $diffSha256 = Get-StringSha256 $diffText
            if (-not ([string]$targetBlob).Equals([string]$allowance.expectedTargetBlob, [StringComparison]::Ordinal)) {
                throw "G2 ownership semantic allowance target blob mismatch for $path`: expected=$($allowance.expectedTargetBlob) actual=$targetBlob"
            }
            if (-not $diffSha256.Equals([string]$allowance.expectedDiffSha256, [StringComparison]::Ordinal)) {
                throw "G2 ownership semantic allowance diff SHA-256 mismatch for $path`: expected=$($allowance.expectedDiffSha256) actual=$diffSha256"
            }
            $ownership = [string]$allowance.classification
        }
        else {
            $ownership = Resolve-NonMandatoryOwnership $path $audit
        }
    }
    $rowDiffSha256 = Get-StringSha256 $diffText
    $totalAdded += [int]$numStat.added
    $totalDeleted += [int]$numStat.deleted
    $files.Add([ordered]@{
        path = $path
        ownership = $ownership
        addedPhysicalLines = [int]$numStat.added
        deletedPhysicalLines = [int]$numStat.deleted
        baselineBlob = $baselineBlob
        targetBlob = $targetBlob
        diffSha256 = $rowDiffSha256
    })
}

$canonicalRows = @($files.ToArray() | ForEach-Object {
    ([string]$_.path) + "`t" + ([string]$_.ownership) + "`t" + ([string]$_.addedPhysicalLines) + "`t" + ([string]$_.deletedPhysicalLines) + "`t" +
        ([string]$_.baselineBlob) + "`t" + ([string]$_.targetBlob) + "`t" + ([string]$_.diffSha256)
})
$canonicalDiff = if ($canonicalRows.Count -eq 0) { '' } else { ($canonicalRows -join "`n") + "`n" }
$headCommit = ([string](Invoke-GitLines @('rev-parse', 'HEAD^{commit}') | Select-Object -First 1)).Trim()
$mandatoryRuntimeProductNativeAdded = 0
$stableAbstractionsAdded = 0
$stableAbstractionsDeleted = 0
foreach ($file in $files.ToArray()) {
    $inMandatoryRuntime = @($audit.mandatoryRuntimeRoots | Where-Object { Test-PathUnderRoot ([string]$file.path) ([string]$_) }).Count -gt 0
    if ($inMandatoryRuntime -and ([string]$file.ownership).Equals('ProductNative', [StringComparison]::Ordinal)) {
        $mandatoryRuntimeProductNativeAdded += [int]$file.addedPhysicalLines
    }
    if (Test-PathUnderRoot ([string]$file.path) ([string]$audit.stableAbstractionsRoot)) {
        $stableAbstractionsAdded += [int]$file.addedPhysicalLines
        $stableAbstractionsDeleted += [int]$file.deletedPhysicalLines
    }
}
if ($mandatoryRuntimeProductNativeAdded -gt [int]$audit.mandatoryRuntimeProductNativeAddedPhysicalLinesMax) {
    throw "G2 mandatory Runtime ProductNative mutation rejected: addedPhysicalLines=$mandatoryRuntimeProductNativeAdded maximum=$($audit.mandatoryRuntimeProductNativeAddedPhysicalLinesMax)."
}
if ($stableAbstractionsAdded -gt [int]$audit.stableAbstractionsAddedPhysicalLinesMax -or
    $stableAbstractionsDeleted -gt [int]$audit.stableAbstractionsDeletedPhysicalLinesMax) {
    throw "G2 stable Abstractions mutation rejected: addedPhysicalLines=$stableAbstractionsAdded deletedPhysicalLines=$stableAbstractionsDeleted."
}
$receipt = [ordered]@{
    schemaVersion = [int]$audit.receiptSchemaVersion
    contractId = [string]$contract.contractId
    architectureAuthority = [string]$contract.authorities.architecture
    reviewAuthority = [string]$contract.authorities.review
    targetKind = if ($useWorktree) { 'worktree-candidate' } else { 'committed-implementation' }
    baselineCommit = $baselineCommit
    implementationCommit = $targetCommit
    worktreeHeadCommit = if ($useWorktree) { $headCommit } else { $null }
    sourceDiffSha256 = Get-StringSha256 $canonicalDiff
    changedFileCount = $files.Count
    addedPhysicalLines = $totalAdded
    deletedPhysicalLines = $totalDeleted
    mandatoryRuntimeProductNativeAddedPhysicalLines = $mandatoryRuntimeProductNativeAdded
    stableAbstractionsAddedPhysicalLines = $stableAbstractionsAdded
    stableAbstractionsDeletedPhysicalLines = $stableAbstractionsDeleted
    files = @($files.ToArray())
}

$json = ($receipt | ConvertTo-Json -Depth 20) + "`n"
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputFullPath = Resolve-RepoFile $OutputPath $OutputPath
    $parent = Split-Path -Parent $outputFullPath
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($outputFullPath, $json, $utf8NoBom)
    Write-Host "Batch 6 G2 ownership receipt written: $outputFullPath"
}
else {
    Write-Output $json
}

Write-Host "Batch 6 G2 ownership audit candidate: target=$($receipt.targetKind); files=$($receipt.changedFileCount); added=$totalAdded; deleted=$totalDeleted; diffSha256=$($receipt.sourceDiffSha256)."
