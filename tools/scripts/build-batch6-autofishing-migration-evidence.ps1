[CmdletBinding()]
param(
    [string] $BaselineGitRef = '9fb8d87d5c03916f2ca083ffb235b293aea58d61',
    [string] $ImplementationGitRef = 'HEAD',
    [string] $CapturedAtUtc = '',
    [string] $OutputPath = 'tools/release/baselines/batch6-autofishing-migration-evidence.json',
    [switch] $Check,
    [switch] $SelfTest
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$utf8 = New-Object Text.UTF8Encoding($false)
$immutableBaseline = '9fb8d87d5c03916f2ca083ffb235b293aea58d61'
$baselineArtifactPath = 'tools/release/baselines/batch6-autofishing-pre-migration-baseline.json'
$immutableBaselineTree = 'cd7090868ef3f979bd86b81c28238da3b1114202'
$immutableBaselineCommittedAtUtc = '2026-07-20T15:05:34Z'
$mandatoryRoots = [string[]]@(
    'src/DTMAPI.Abstractions',
    'src/DTMAPI.Core',
    'src/DTMAPI.BepInExBootstrap',
    'src/DTMAPI.GameBridge.DolocTown',
    'src/DTMAPI.ModConfigMenu'
)
$runtimeDlls = [string[]]@(
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Abstractions.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
)
$mandatoryRootTrees = [ordered]@{
    'src/DTMAPI.Abstractions' = '57cc8d874b662888405106ad2bae39e736e97ecb'
    'src/DTMAPI.Core' = 'fb3fdf2f40d4a8420271327e8cf10490a4192ed1'
    'src/DTMAPI.BepInExBootstrap' = 'c0317d903672dd64e69f37a719009daed6f540e7'
    'src/DTMAPI.GameBridge.DolocTown' = '3532484f9d94a87ff4b6f5c074902e686b67c386'
    'src/DTMAPI.ModConfigMenu' = '30d474a8a19c458eed3dac30599a6ffeeec43a4e'
}
$baselineProductNativeRoot = 'src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation'
$baselinePrimitivePath = 'src/DTMAPI.Abstractions/FirstPartyFishingPrimitives.cs'
$friendPath = 'src/DTMAPI.Abstractions/AssemblyInfo.cs'
$productUniqueIdPattern = 'Yuuka\.DTMAPI\.AutoFishing'
$hardProductIdentityPattern = 'dtmapi\.mod\.yuuka\.dtmapi\.autofishing|namespace\s+Yuuka\.DTMAPI\.AutoFishing'
$productIdentityPattern = $productUniqueIdPattern + '|' + $hardProductIdentityPattern
$compatibilityPattern = 'FishingCompatibility|FishingAutomationCompatibility|IFishingCompatibility|OwnerBoundFishingAutomationApi|IFishingAutomationApi'
$productBehaviorPattern = 'IFirstPartyFishing|FirstPartyFishing|FishingPrimitive|AutoFishingMod|FishingAutomation(?!Compatibility)|FishingNative(?:Adapter|State|Transaction|Energy|Control|Accessors|Frame)|FishingMiniGameNativeCache|FishingAnimationNativeCache|FishingInputOverride|FishingRuntimeComponents|IFishingHook(?:Runtime|Coordinator)'
$coLocatedProductNativePattern = 'FishingProduct|ProductNative|IFirstPartyFishing|FirstPartyFishing|FishingPrimitive|AutoFishingMod|FishingNative(?:Adapter|State|Transaction|Energy|Control|Accessors|Frame)|FishingMiniGameNativeCache|FishingAnimationNativeCache|FishingInputOverride|FishingRuntimeComponents|IFishingHook(?:Runtime|Coordinator)|(?:class|struct|interface)\s+[A-Za-z0-9_]*(?<!Compatibility)(?:StateMachine|AnimationController|NativeCache|TransactionCache)\b'
$gameBridgeSuspiciousPattern = 'Harmony|Patch|StateMachine|InputOverride|AnimationController|NativeCache|TransactionCache|KeyCode|UnityEngine\.Input|FishRodRenderer|Fishing(?:Ready|Wait|Pull|Cast|Reel|Bite)'

function Invoke-GitLines([string[]] $Arguments) {
    $output = @(& git -C $repo @Arguments 2>$null)
    if ($LASTEXITCODE -ne 0) { throw "git $([string]::Join(' ', $Arguments)) failed with exit $LASTEXITCODE." }
    return @($output | ForEach-Object { ([string]$_).TrimEnd("`r") })
}

function Get-Sha256Text([string] $Text) {
    $normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($utf8.GetBytes($normalized)))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Normalize-CapturedAtUtc([string] $Value) {
    $parsed = [DateTimeOffset]::Parse($Value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    return [DateTimeOffset]::ParseExact($parsed.ToString('yyyy-MM-ddTHH:mm:sszzz'), 'yyyy-MM-ddTHH:mm:sszzz', [Globalization.CultureInfo]::InvariantCulture).ToString('o')
}

function ConvertFrom-JsonDocument([string] $Text) {
    # PowerShell 7.5+ converts ISO 8601-looking strings to DateTime by default,
    # while Windows PowerShell 5.1 preserves them as strings. Evidence values
    # must have identical ordinal semantics on both supported hosts.
    $command = Get-Command ConvertFrom-Json
    if ($command.Parameters.ContainsKey('DateKind')) {
        return $Text | ConvertFrom-Json -DateKind String
    }
    return $Text | ConvertFrom-Json
}

function Get-Blob([string] $Commit, [string] $Path) {
    $row = @(Invoke-GitLines @('ls-tree', $Commit, '--', $Path))
    if ($row.Count -ne 1 -or $row[0] -notmatch '^[0-9]{6}\s+blob\s+([0-9a-f]{40,64})\t') { return $null }
    return [string]$Matches[1]
}

function Get-Text([string] $Commit, [string] $Path) {
    if ($null -eq (Get-Blob $Commit $Path)) { return $null }
    return [string]::Join("`n", @(Invoke-GitLines @('show', "$Commit`:$Path"))) + "`n"
}

function Assert-ExactProperties([object] $Value, [string[]] $Expected, [string] $Label) {
    [string[]]$actual = @($Value.PSObject.Properties.Name)
    [string[]]$expectedCopy = @($Expected)
    [Array]::Sort($actual, [StringComparer]::Ordinal)
    [Array]::Sort($expectedCopy, [StringComparer]::Ordinal)
    if (($actual -join "`n") -cne ($expectedCopy -join "`n")) {
        throw "$Label has missing or additional properties. expected=$($expectedCopy -join ','); actual=$($actual -join ',')"
    }
}

function Assert-PreMigrationBaseline([string] $ImplementationCommit) {
    $text = Get-Text $ImplementationCommit $baselineArtifactPath
    if ([string]::IsNullOrWhiteSpace($text)) {
        throw "G4 pre-migration baseline is absent from implementation commit: $baselineArtifactPath"
    }
    try { $baseline = ConvertFrom-JsonDocument $text }
    catch { throw [IO.InvalidDataException]::new('G4 pre-migration baseline is invalid JSON.', $_.Exception) }
    Assert-ExactProperties $baseline @(
        'schemaVersion','receiptKind','baselineCommit','baselineTree','baselineCommittedAtUtc','capturedAtUtc',
        'sourceState','mandatoryRoots','reproduceCommand','notes') 'G4 pre-migration baseline'
    if ([int]$baseline.schemaVersion -ne 1 -or
        [string]$baseline.receiptKind -cne 'batch6-autofishing-pre-migration-baseline' -or
        [string]$baseline.baselineCommit -cne $immutableBaseline -or
        [string]$baseline.baselineTree -cne $immutableBaselineTree -or
        [string]$baseline.baselineCommittedAtUtc -cne $immutableBaselineCommittedAtUtc -or
        [string]$baseline.sourceState -cne 'immutable-clean-post-g2-closure-commit') {
        throw 'G4 pre-migration baseline header drifted from the immutable post-G2 authority.'
    }

    [string]$actualTree = [string](@(Invoke-GitLines @('show', '-s', '--format=%T', $immutableBaseline))[0])
    [DateTimeOffset]$actualBaselineTime = [DateTimeOffset]::Parse(
        [string](@(Invoke-GitLines @('show', '-s', '--format=%cI', $immutableBaseline))[0])).ToUniversalTime()
    if ($actualTree -cne $immutableBaselineTree -or
        $actualBaselineTime.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture) -cne $immutableBaselineCommittedAtUtc) {
        throw 'G4 immutable baseline commit tree or commit time drifted.'
    }

    $rows = @($baseline.mandatoryRoots)
    if ($rows.Count -ne $mandatoryRoots.Count) { throw 'G4 pre-migration baseline must bind exactly five mandatory roots.' }
    for ($index = 0; $index -lt $mandatoryRoots.Count; $index++) {
        $row = $rows[$index]
        Assert-ExactProperties $row @('path','tree') "G4 pre-migration root row $index"
        [string]$path = $mandatoryRoots[$index]
        [string]$expectedTree = [string]$mandatoryRootTrees[$path]
        [string]$actualRootTree = [string](@(Invoke-GitLines @('rev-parse', "$immutableBaseline`:$path"))[0])
        if ([string]$row.path -cne $path -or [string]$row.tree -cne $expectedTree -or $actualRootTree -cne $expectedTree) {
            throw "G4 pre-migration mandatory root binding drifted: $path"
        }
    }

    $artifactCommitRows = @(Invoke-GitLines @('log', '-1', '--format=%H', $ImplementationCommit, '--', $baselineArtifactPath))
    if ($artifactCommitRows.Count -ne 1) { throw 'G4 pre-migration baseline has no containing commit in the implementation ancestry.' }
    [string]$artifactCommit = $artifactCommitRows[0]
    [string]$artifactBlob = Get-Blob $artifactCommit $baselineArtifactPath
    [string]$implementationBlob = Get-Blob $ImplementationCommit $baselineArtifactPath
    if ([string]::IsNullOrWhiteSpace($artifactBlob) -or $artifactBlob -cne $implementationBlob) {
        throw 'G4 pre-migration baseline bytes changed after their containing commit.'
    }
    & git -C $repo merge-base --is-ancestor $immutableBaseline $artifactCommit 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'G4 pre-migration baseline commit must descend from the immutable G2 baseline.' }
    & git -C $repo merge-base --is-ancestor $artifactCommit $ImplementationCommit 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'G4 implementation commit must descend from the committed pre-migration baseline artifact.' }

    [DateTimeOffset]$captured = [DateTimeOffset]::Parse(
        [string]$baseline.capturedAtUtc,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    [DateTimeOffset]$artifactCommitTime = [DateTimeOffset]::Parse(
        [string](@(Invoke-GitLines @('show', '-s', '--format=%cI', $artifactCommit))[0])).ToUniversalTime()
    if ($captured -lt $actualBaselineTime -or $captured -ge $artifactCommitTime) {
        throw 'G4 pre-migration baseline capturedAtUtc must be at/after the source commit and strictly before its containing commit.'
    }
}

function Get-DiffLines([string] $Baseline, [string] $Target, [string] $Path) {
    return @(Invoke-GitLines @('diff', '--no-ext-diff', '--no-textconv', '--no-renames', '--unified=0', $Baseline, $Target, '--', $Path))
}

function Get-NumStat([string] $Baseline, [string] $Target, [string] $Path) {
    $lines = @(Invoke-GitLines @('diff', '--numstat', '--no-renames', $Baseline, $Target, '--', $Path))
    if ($lines.Count -eq 0) { return @([int]0, [int]0) }
    if ($lines.Count -ne 1) { throw "Mandatory Runtime path produced an ambiguous numstat: $Path" }
    $parts = $lines[0] -split "`t"
    if ($parts.Count -lt 3 -or $parts[0] -eq '-' -or $parts[1] -eq '-') { throw "Binary mandatory Runtime delta is forbidden: $Path" }
    return @([int]$parts[0], [int]$parts[1])
}

function Get-OrdinalUnique([object[]] $Values) {
    [string[]]$ordered = @($Values | ForEach-Object { [string]$_ })
    [Array]::Sort($ordered, [StringComparer]::Ordinal)
    $result = New-Object Collections.ArrayList
    $previous = $null
    foreach ($value in $ordered) {
        if ($null -ne $previous -and $value -ceq $previous) { continue }
        [void]$result.Add($value)
        $previous = $value
    }
    return @($result)
}

function Test-MandatoryPath([string] $Path) {
    foreach ($root in $mandatoryRoots) {
        if ($Path.Equals($root, [StringComparison]::Ordinal) -or $Path.StartsWith($root + '/', [StringComparison]::Ordinal)) { return $true }
    }
    return $false
}

function Test-BaselineProductNativePath([string] $Path) {
    return $Path.Equals($baselinePrimitivePath, [StringComparison]::Ordinal) -or
        $Path.Equals($baselineProductNativeRoot, [StringComparison]::Ordinal) -or
        $Path.StartsWith($baselineProductNativeRoot + '/', [StringComparison]::Ordinal)
}

function Get-DeclaredTypeSymbols([string] $Text) {
    if ([string]::IsNullOrEmpty($Text)) { return @() }
    $symbols = New-Object Collections.ArrayList
    foreach ($match in [Text.RegularExpressions.Regex]::Matches($Text, '\b(?:class|interface|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)')) {
        [void]$symbols.Add([string]$match.Groups[1].Value)
    }
    return @(Get-OrdinalUnique @($symbols))
}

function Get-NearestScopeSymbol([string] $Text, [int] $StartLine) {
    if ([string]::IsNullOrEmpty($Text)) { return '' }
    [string[]]$lines = @($Text.Replace("`r`n", "`n").Replace("`r", "`n") -split "`n")
    $index = [Math]::Min([Math]::Max(0, $StartLine - 1), [Math]::Max(0, $lines.Count - 1))
    for (; $index -ge 0; $index--) {
        $line = $lines[$index]
        if ($line -match '\b(?:class|interface|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)') { return [string]$Matches[1] }
        if ($line -match '\b(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|sealed|async|partial|readonly)\s+)*(?:[A-Za-z_][A-Za-z0-9_<>?,.\[\]]*\s+)+([A-Za-z_][A-Za-z0-9_]*)\s*\(') { return [string]$Matches[1] }
    }
    return ''
}

function Get-HunkSymbols([string] $SourceText, [int] $StartLine, [string[]] $ChangedLines, [string] $Path) {
    $text = [string]::Join("`n", $ChangedLines)
    $symbols = New-Object Collections.ArrayList
    foreach ($symbol in @(Get-DeclaredTypeSymbols $text)) { [void]$symbols.Add($symbol) }
    $nearest = Get-NearestScopeSymbol $SourceText $StartLine
    if (-not [string]::IsNullOrWhiteSpace($nearest)) { [void]$symbols.Add($nearest) }
    if ($symbols.Count -eq 0) {
        [void]$symbols.Add($(if ($Path.EndsWith('.csproj', [StringComparison]::OrdinalIgnoreCase)) { '<project-file>' } else { '<file-scope>' }))
    }
    return @(Get-OrdinalUnique @($symbols))
}

function Test-VerifiedCompatibilitySource([string] $Text) {
    if ([string]::IsNullOrWhiteSpace($Text) -or [Text.RegularExpressions.Regex]::IsMatch($Text, $hardProductIdentityPattern, 'IgnoreCase,CultureInvariant')) { return $false }
    $frozenApi = [Text.RegularExpressions.Regex]::IsMatch($Text, 'IFishingAutomationApi', 'CultureInvariant') -and
        [Text.RegularExpressions.Regex]::IsMatch($Text, 'frozen|deprecated|compatibility', 'IgnoreCase,CultureInvariant')
    [string[]]$types = @(Get-DeclaredTypeSymbols $Text)
    $hasCompatibilityType = @($types | Where-Object { $_ -match 'FishingCompatibility|FishingAutomationCompatibility|LegacyFishingAutomation|OwnerBoundFishingAutomation|IFishingCompatibility' }).Count -gt 0
    $hasHostType = @($types | Where-Object { $_ -in @('DolocTownGameBridge', 'GameBridgeDemandRoutes', 'DolocTownHookCallbacks', 'OwnerBoundGameBridgeApis', 'GameBridgeModOwnerCleanupParticipant') }).Count -gt 0
    return $frozenApi -or ($hasCompatibilityType -and -not $hasHostType)
}

function Test-VerifiedCompatibilityContractSource([string] $Text) {
    if (-not (Test-VerifiedCompatibilitySource $Text)) { return $false }
    # A signature-only private adapter is compatibility plumbing, not a new
    # ProductNative implementation. Keep this narrow so classes, structs,
    # delegates, enums, expression bodies, and method bodies remain scannable.
    return [Text.RegularExpressions.Regex]::IsMatch(
            $Text,
            '\binternal\s+interface\s+IFishingCompatibility[A-Za-z0-9_]*\b',
            'CultureInvariant') -and
        -not [Text.RegularExpressions.Regex]::IsMatch($Text, '\b(class|struct|delegate|enum)\b|=>|\bnew\s+[A-Za-z_]|\breturn\b', 'CultureInvariant')
}

function Test-TextContainsProductSymbol([string] $Text, [string[]] $ProductSymbols) {
    foreach ($symbol in $ProductSymbols) {
        if ($symbol.Length -lt 6) { continue }
        if ([Text.RegularExpressions.Regex]::IsMatch($Text, '(?<![A-Za-z0-9_])' + [Text.RegularExpressions.Regex]::Escape($symbol) + '(?![A-Za-z0-9_])', 'CultureInvariant')) { return $true }
    }
    return $false
}

function Get-SideOwnership(
    [ValidateSet('Added', 'Deleted')] [string] $Side,
    [string] $Path,
    [string] $SourceText,
    [string[]] $Lines,
    [string[]] $BaselineProductSymbols,
    [bool] $BaselineProductPhysical) {

    if ($Lines.Count -eq 0) { return [ordered]@{ owner = 'None'; evidence = @('delta:none') } }
    $deltaText = [string]::Join("`n", $Lines)
    if ($Side -eq 'Deleted') {
        if ($BaselineProductPhysical) { return [ordered]@{ owner = 'ProductNativeRemoval'; evidence = @('baseline-physical-owner:ProductNative') } }
        if (Test-VerifiedCompatibilitySource $SourceText) { return [ordered]@{ owner = 'Compatibility'; evidence = @('baseline-source:frozen-compatibility') } }
        if ([Text.RegularExpressions.Regex]::IsMatch($deltaText, $productIdentityPattern + '|' + $productBehaviorPattern, 'IgnoreCase,CultureInvariant') -or
            (Test-TextContainsProductSymbol $deltaText $BaselineProductSymbols)) {
            return [ordered]@{ owner = 'ProductNativeRemoval'; evidence = @('baseline-delta:product-symbol') }
        }
        if ($Path.StartsWith('src/DTMAPI.GameBridge.DolocTown/', [StringComparison]::Ordinal) -and
            [Text.RegularExpressions.Regex]::IsMatch($deltaText, 'Fishing|FishRod|AutoFishing', 'IgnoreCase,CultureInvariant')) {
            return [ordered]@{ owner = 'ProductNativeRemoval'; evidence = @('baseline-delta:gameplay-route') }
        }
        return [ordered]@{ owner = 'Platform'; evidence = @('baseline-delta:platform') }
    }

    if ([Text.RegularExpressions.Regex]::IsMatch($deltaText, $hardProductIdentityPattern, 'IgnoreCase,CultureInvariant')) {
        return [ordered]@{ owner = 'ProductNative'; evidence = @('target-delta:product-identity') }
    }
    $verifiedCompatibility = Test-VerifiedCompatibilitySource $SourceText
    $advancedPolicyPlatform = $Path.StartsWith('src/DTMAPI.Core/', [StringComparison]::Ordinal) -and
        [Text.RegularExpressions.Regex]::IsMatch($deltaText + "`n" + $SourceText, 'AdvancedReferencePolicy|referencePolicyId|PolicyRegistry', 'IgnoreCase,CultureInvariant') -and
        -not [Text.RegularExpressions.Regex]::IsMatch($deltaText, $productBehaviorPattern, 'IgnoreCase,CultureInvariant')
    if ($advancedPolicyPlatform) {
        return [ordered]@{ owner = 'Platform'; evidence = @('target-source:advanced-policy-platform') }
    }
    $hasUniqueId = [Text.RegularExpressions.Regex]::IsMatch($deltaText, $productUniqueIdPattern, 'IgnoreCase,CultureInvariant')
    $isMigrationWarning = [Text.RegularExpressions.Regex]::IsMatch($deltaText, 'migrate|migration|deprecated|frozen|compatibility', 'IgnoreCase,CultureInvariant')
    if ($hasUniqueId -and -not ($verifiedCompatibility -and $isMigrationWarning)) {
        return [ordered]@{ owner = 'ProductNative'; evidence = @('target-delta:product-identity') }
    }
    if ($verifiedCompatibility -and (Test-VerifiedCompatibilityContractSource $SourceText)) {
        return [ordered]@{ owner = 'Compatibility'; evidence = @('target-source:signature-only-frozen-compatibility-contract') }
    }
    # A Compatibility file is not a blanket ownership exemption. Inspect the
    # actual added hunk before accepting its source container, so a new product
    # state machine/cache/primitive cannot hide beside the frozen ABI facade.
    if ([Text.RegularExpressions.Regex]::IsMatch($deltaText, $coLocatedProductNativePattern, 'IgnoreCase,CultureInvariant')) {
        return [ordered]@{ owner = 'ProductNative'; evidence = @('target-delta:co-located-product-native') }
    }
    if ($verifiedCompatibility) {
        return [ordered]@{ owner = 'Compatibility'; evidence = @('target-source:verified-frozen-compatibility') }
    }
    if ([Text.RegularExpressions.Regex]::IsMatch($deltaText, $compatibilityPattern, 'IgnoreCase,CultureInvariant')) {
        return [ordered]@{ owner = 'Compatibility'; evidence = @('target-delta:compatibility-contract') }
    }
    if ([Text.RegularExpressions.Regex]::IsMatch($deltaText, $productBehaviorPattern, 'IgnoreCase,CultureInvariant') -or
        (Test-TextContainsProductSymbol $deltaText $BaselineProductSymbols)) {
        return [ordered]@{ owner = 'ProductNative'; evidence = @('target-delta:product-symbol') }
    }
    $diagnosticStatusOnly = $Path.StartsWith('src/DTMAPI.GameBridge.DolocTown/Diagnostics/', [StringComparison]::Ordinal) -and
        @($Lines | Where-Object { -not [Text.RegularExpressions.Regex]::IsMatch($_, '^\s*runtime\.SetHookStatus\(', 'CultureInvariant') }).Count -eq 0
    if ($diagnosticStatusOnly) {
        return [ordered]@{ owner = 'Platform'; evidence = @('target-delta:diagnostic-status-only') }
    }
    if ($Path.StartsWith('src/DTMAPI.GameBridge.DolocTown/', [StringComparison]::Ordinal) -and
        [Text.RegularExpressions.Regex]::IsMatch($deltaText, $gameBridgeSuspiciousPattern, 'IgnoreCase,CultureInvariant')) {
        return [ordered]@{ owner = 'ProductNative'; evidence = @('target-delta:unbounded-gameplay-or-patch') }
    }
    return [ordered]@{ owner = 'Platform'; evidence = @('target-delta:platform-no-product-signal') }
}

function ConvertTo-DiffHunks([string[]] $DiffLines) {
    $hunks = New-Object Collections.ArrayList
    $current = $null
    foreach ($line in $DiffLines) {
        if ($line -match '^@@ -([0-9]+)(?:,([0-9]+))? \+([0-9]+)(?:,([0-9]+))? @@') {
            if ($null -ne $current) { [void]$hunks.Add($current) }
            $baselineCount = if ([string]::IsNullOrEmpty([string]$Matches[2])) { 1 } else { [int]$Matches[2] }
            $targetCount = if ([string]::IsNullOrEmpty([string]$Matches[4])) { 1 } else { [int]$Matches[4] }
            $current = [ordered]@{
                header = [string]$line
                baselineStart = [int]$Matches[1]
                baselineCount = $baselineCount
                targetStart = [int]$Matches[3]
                targetCount = $targetCount
                rawLines = New-Object Collections.ArrayList
            }
            continue
        }
        if ($null -ne $current -and -not $line.StartsWith('\ No newline at end of file', [StringComparison]::Ordinal)) { [void]$current.rawLines.Add([string]$line) }
    }
    if ($null -ne $current) { [void]$hunks.Add($current) }
    return @($hunks)
}

function Get-BaselineProductNativeSources([string] $Baseline) {
    [string[]]$paths = @(Invoke-GitLines @('ls-tree', '-r', '--name-only', $Baseline, '--', $baselineProductNativeRoot) | Where-Object { $_.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) })
    if ($null -ne (Get-Blob $Baseline $baselinePrimitivePath)) { $paths += $baselinePrimitivePath }
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $rows = New-Object Collections.ArrayList
    foreach ($path in $paths) { [void]$rows.Add([ordered]@{ path = $path; blob = Get-Blob $Baseline $path }) }
    return @($rows)
}

function Get-BaselineProductSymbols([string] $Baseline, [object[]] $Sources) {
    $symbols = New-Object Collections.ArrayList
    foreach ($source in $Sources) {
        foreach ($symbol in @(Get-DeclaredTypeSymbols (Get-Text $Baseline ([string]$source.path)))) {
            if ($symbol -match 'Fishing|Fish|Animation|Input|Primitive') { [void]$symbols.Add($symbol) }
        }
    }
    return [string[]]@(Get-OrdinalUnique @($symbols))
}

function Get-TreeMetrics([string] $Commit, [string[]] $Roots) {
    $arguments = New-Object Collections.Generic.List[string]
    foreach ($value in @('ls-tree', '-r', '--name-only', $Commit, '--')) { $arguments.Add($value) }
    foreach ($root in $Roots) { $arguments.Add($root) }
    [string[]]$files = @(Invoke-GitLines $arguments.ToArray() | Where-Object { $_.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) })
    [Array]::Sort($files, [StringComparer]::Ordinal)
    $lineCount = 0
    $identityLines = New-Object Collections.ArrayList
    foreach ($path in $files) {
        $text = Get-Text $Commit $path
        $lines = if ([string]::IsNullOrEmpty($text)) { 0 } else { @($text.TrimEnd("`n") -split "`n").Count }
        $lineCount += [int]$lines
        [void]$identityLines.Add("$path`t$(Get-Blob $Commit $path)`t$lines")
    }
    return [ordered]@{
        fileCount = $files.Count
        physicalLines = $lineCount
        treeSha256 = Get-Sha256Text ([string]::Join("`n", @($identityLines)) + "`n")
    }
}

function Count-SourceToken([string] $Commit, [string] $Root, [string] $Pattern) {
    [string[]]$paths = @(Invoke-GitLines @('ls-tree', '-r', '--name-only', $Commit, '--', $Root) | Where-Object { $_.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) })
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $count = 0
    foreach ($path in $paths) {
        $text = Get-Text $Commit $path
        if ($null -ne $text) { $count += [Text.RegularExpressions.Regex]::Matches($text, $Pattern).Count }
    }
    return $count
}

function Get-MigrationPathClass([string] $Path) {
    if (Test-MandatoryPath $Path) { return 'MandatoryRuntime' }
    if ($Path.StartsWith('products/first-party/AutoFishing/', [StringComparison]::Ordinal) -or
        $Path.StartsWith('first-party-mods/AutoFishingMod/', [StringComparison]::Ordinal)) { return 'AutoFishingProduct' }
    if ($Path.StartsWith('tests/', [StringComparison]::Ordinal) -or
        $Path.StartsWith('src/DTMAPI.GameBridge.DolocTown.QA/', [StringComparison]::Ordinal)) { return 'QualityAssurance' }
    if ($Path.StartsWith('docs/', [StringComparison]::Ordinal) -or $Path -in @('AGENTS.md', 'PROJECT.md')) { return 'Documentation' }
    if ($Path.StartsWith('tools/', [StringComparison]::Ordinal) -or
        $Path.StartsWith('author-sdk/', [StringComparison]::Ordinal) -or
        $Path.StartsWith('src/', [StringComparison]::Ordinal) -or
        $Path -in @('DTMAPI.sln', 'Directory.Build.props', 'Directory.Build.targets')) { return 'PlatformTooling' }
    return 'Other'
}

function Get-MigrationDelta([string] $Baseline, [string] $Target) {
    [string[]]$paths = @(Invoke-GitLines @('diff', '--name-only', '--no-renames', $Baseline, $Target, '--'))
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    if ($paths.Count -eq 0) { throw 'Consolidated migration evidence has no implementation delta.' }
    $classes = New-Object Collections.ArrayList
    foreach ($className in @('MandatoryRuntime','AutoFishingProduct','QualityAssurance','PlatformTooling','Documentation','Other')) {
        [string[]]$classPaths = @($paths | Where-Object { (Get-MigrationPathClass ([string]$_)) -ceq $className })
        if ($classPaths.Count -eq 0) { continue }
        [void]$classes.Add([ordered]@{
            classification = $className
            pathCount = $classPaths.Count
            pathsSha256 = Get-Sha256Text ([string]::Join("`n", $classPaths) + "`n")
        })
    }
    $other = @($classes | Where-Object { [string]$_.classification -ceq 'Other' })
    if ($other.Count -ne 0) {
        [string[]]$otherPaths = @($paths | Where-Object { (Get-MigrationPathClass ([string]$_)) -ceq 'Other' })
        throw "Consolidated migration evidence found unclassified paths: $([string]::Join(', ', $otherPaths))"
    }
    return [ordered]@{
        changedPathCount = $paths.Count
        changedPathsSha256 = Get-Sha256Text ([string]::Join("`n", $paths) + "`n")
        classifications = @($classes)
    }
}

function Get-MigrationAuthorityReferences([string] $Target) {
    [string[]]$paths = @(
        'tools/release/dtmapi-product-catalog.json',
        'tools/scripts/check-product-catalog.ps1',
        'tools/scripts/build-batch6-autofishing-advanced-pilot.ps1',
        'tools/scripts/check-release-contract.ps1',
        'author-sdk/compatibility/0.5.5/compatibility.contract.json',
        'tools/scripts/test-retained-autofishing-abi.ps1',
        'src/DTMAPI.InstallDoctor/AdvancedReferencePolicyAuthority.cs',
        'tools/scripts/build-release-workshop-packages.ps1'
    )
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $rows = New-Object Collections.ArrayList
    foreach ($path in $paths) {
        $blob = Get-Blob $Target $path
        if ([string]::IsNullOrWhiteSpace($blob)) { throw "Consolidated migration authority is missing at the implementation commit: $path" }
        [void]$rows.Add([ordered]@{ path = $path; blob = $blob })
    }
    return @($rows)
}

function Get-ReleaseRuntimeDlls([string] $ReleaseBuilderText) {
    $assignment = [Text.RegularExpressions.Regex]::Match(
        $ReleaseBuilderText,
        '(?m)^\s*\$runtimeFiles\s*=\s*@\((?<items>[^\r\n]*)\)\s*$')
    if (-not $assignment.Success) { throw 'G4 target release builder has no single-line source-derived $runtimeFiles assignment.' }
    [string]$items = $assignment.Groups['items'].Value
    $literalMatches = [Text.RegularExpressions.Regex]::Matches($items, "'(?<name>[^']+)'")
    [string]$residual = [Text.RegularExpressions.Regex]::Replace($items, "'[^']+'", '')
    if ($literalMatches.Count -eq 0 -or $residual -notmatch '^[,\s]*$') {
        throw 'G4 target release builder $runtimeFiles assignment contains non-literal or unparsed entries.'
    }
    [string[]]$names = @($literalMatches | ForEach-Object { [string]$_.Groups['name'].Value })
    if (@($names | Select-Object -Unique).Count -ne $names.Count) { throw 'G4 target release builder contains duplicate mandatory Runtime DLL entries.' }
    [Array]::Sort($names, [StringComparer]::Ordinal)
    return $names
}

function Assert-ExactReleaseRuntimeDlls([string] $ReleaseBuilderText) {
    [string[]]$actual = @(Get-ReleaseRuntimeDlls $ReleaseBuilderText)
    [string[]]$expected = @($runtimeDlls)
    [Array]::Sort($expected, [StringComparer]::Ordinal)
    if (($actual -join "`n") -cne ($expected -join "`n")) {
        throw 'G4 target does not expose the exact five mandatory Runtime assembly identities.'
    }
    return $actual
}

function Test-CanonicalObjectEquality([object] $Left, [object] $Right) {
    return (($Left | ConvertTo-Json -Depth 40 -Compress) -ceq ($Right | ConvertTo-Json -Depth 40 -Compress))
}

function Assert-Chronology([DateTimeOffset] $ImplementationTime, [DateTimeOffset] $CapturedTime, [Nullable[DateTimeOffset]] $ReceiptTime) {
    if ($CapturedTime.ToUniversalTime() -lt $ImplementationTime.ToUniversalTime()) { throw 'G4 capturedAtUtc must not predate the implementation commit.' }
    # PowerShell boxes a populated Nullable<T> as T and leaves an empty value as
    # $null, so HasValue/Value are not portable script-level members.
    if ($null -ne $ReceiptTime -and $CapturedTime.ToUniversalTime() -ge $ReceiptTime.ToUniversalTime()) { throw 'G4 capturedAtUtc must predate the commit containing the receipt.' }
}

function Build-Receipt([string] $Baseline, [string] $Target, [string] $Captured) {
    & git -C $repo merge-base --is-ancestor $Baseline $Target 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'G4 implementation commit must descend from the immutable post-G2 baseline.' }
    $implementationTime = [DateTimeOffset]::Parse([string](@(Invoke-GitLines @('show', '-s', '--format=%cI', $Target))[0])).ToUniversalTime()
    $capturedTime = [DateTimeOffset]::Parse($Captured, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    Assert-Chronology $implementationTime $capturedTime $null

    $baselineProductSources = @(Get-BaselineProductNativeSources $Baseline)
    [string[]]$baselineProductSymbols = @(Get-BaselineProductSymbols $Baseline $baselineProductSources)
    $sourceIdentity = @($baselineProductSources | ForEach-Object { "$($_.path)`t$($_.blob)" })

    [string[]]$changedPaths = @(Invoke-GitLines @('diff', '--name-only', '--no-renames', $Baseline, $Target, '--') | Where-Object { Test-MandatoryPath ([string]$_) })
    [Array]::Sort($changedPaths, [StringComparer]::Ordinal)
    if ($changedPaths.Count -eq 0) { throw 'G4 implementation has no mandatory Runtime source delta.' }

    $rows = New-Object Collections.ArrayList
    $diffIdentity = New-Object Collections.ArrayList
    $totalHunks = 0
    $platformAdded = 0
    $platformDeleted = 0
    $compatibilityAdded = 0
    $compatibilityDeleted = 0
    $productNativeAdded = 0
    $productNativeDeleted = 0
    $productNativeAdditionDiagnostics = New-Object Collections.ArrayList
    foreach ($path in $changedPaths) {
        $baselineBlob = Get-Blob $Baseline $path
        $targetBlob = Get-Blob $Target $path
        $baselineText = Get-Text $Baseline $path
        $targetText = Get-Text $Target $path
        $diffLines = @(Get-DiffLines $Baseline $Target $path)
        $diffSha256 = Get-Sha256Text ([string]::Join("`n", $diffLines) + "`n")
        $numstat = Get-NumStat $Baseline $Target $path
        $parsedHunks = @(ConvertTo-DiffHunks $diffLines)
        if ($parsedHunks.Count -eq 0) { throw "G4 changed mandatory path has no parseable Git hunk: $path" }
        $hunkRows = New-Object Collections.ArrayList
        $symbolRows = New-Object Collections.ArrayList
        $fileOwners = New-Object Collections.ArrayList
        $fileAdded = 0
        $fileDeleted = 0
        $ordinal = 0
        foreach ($parsed in $parsedHunks) {
            $ordinal++
            [string[]]$addedLines = @($parsed.rawLines | Where-Object { $_.StartsWith('+', [StringComparison]::Ordinal) } | ForEach-Object { $_.Substring(1) })
            [string[]]$deletedLines = @($parsed.rawLines | Where-Object { $_.StartsWith('-', [StringComparison]::Ordinal) } | ForEach-Object { $_.Substring(1) })
            $addedOwnership = Get-SideOwnership 'Added' $path $targetText $addedLines $baselineProductSymbols $false
            $deletedOwnership = Get-SideOwnership 'Deleted' $path $baselineText $deletedLines $baselineProductSymbols (Test-BaselineProductNativePath $path)
            $fileAdded += $addedLines.Count
            $fileDeleted += $deletedLines.Count
            $hunkPlatformAdded = $(if ($addedOwnership.owner -eq 'Platform') { $addedLines.Count } else { 0 })
            $hunkCompatibilityAdded = $(if ($addedOwnership.owner -eq 'Compatibility') { $addedLines.Count } else { 0 })
            $hunkProductAdded = $(if ($addedOwnership.owner -eq 'ProductNative') { $addedLines.Count } else { 0 })
            $hunkPlatformDeleted = $(if ($deletedOwnership.owner -eq 'Platform') { $deletedLines.Count } else { 0 })
            $hunkCompatibilityDeleted = $(if ($deletedOwnership.owner -eq 'Compatibility') { $deletedLines.Count } else { 0 })
            $hunkProductDeleted = $(if ($deletedOwnership.owner -eq 'ProductNativeRemoval') { $deletedLines.Count } else { 0 })
            if ($hunkProductAdded -gt 0) {
                [void]$productNativeAdditionDiagnostics.Add(
                    "$path#$ordinal added=$hunkProductAdded evidence=$([string]::Join(',', @($addedOwnership.evidence)))")
            }
            $platformAdded += $hunkPlatformAdded
            $compatibilityAdded += $hunkCompatibilityAdded
            $productNativeAdded += $hunkProductAdded
            $platformDeleted += $hunkPlatformDeleted
            $compatibilityDeleted += $hunkCompatibilityDeleted
            $productNativeDeleted += $hunkProductDeleted
            foreach ($owner in @($addedOwnership.owner, $deletedOwnership.owner)) { if ($owner -ne 'None') { [void]$fileOwners.Add($owner) } }
            [string[]]$baselineSymbols = @(Get-HunkSymbols $baselineText ([int]$parsed.baselineStart) $deletedLines $path)
            [string[]]$targetSymbols = @(Get-HunkSymbols $targetText ([int]$parsed.targetStart) $addedLines $path)
            if ($deletedLines.Count -gt 0) {
                foreach ($symbol in $baselineSymbols) { [void]$symbolRows.Add([ordered]@{ hunkOrdinal = $ordinal; side = 'Baseline'; symbol = $symbol; ownership = [string]$deletedOwnership.owner }) }
            }
            if ($addedLines.Count -gt 0) {
                foreach ($symbol in $targetSymbols) { [void]$symbolRows.Add([ordered]@{ hunkOrdinal = $ordinal; side = 'Target'; symbol = $symbol; ownership = [string]$addedOwnership.owner }) }
            }
            [string[]]$evidence = @(Get-OrdinalUnique @($addedOwnership.evidence + $deletedOwnership.evidence))
            $sideOwners = @(@($addedOwnership.owner, $deletedOwnership.owner) | Where-Object { $_ -ne 'None' })
            [string[]]$distinctOwners = @(Get-OrdinalUnique $sideOwners)
            $hunkOwner = if ($distinctOwners.Count -eq 1) { $distinctOwners[0] } elseif ($distinctOwners.Count -gt 1) { 'Mixed' } else { throw "G4 empty hunk is forbidden: $path#$ordinal" }
            [void]$hunkRows.Add([ordered]@{
                ordinal = $ordinal
                header = [string]$parsed.header
                baselineStart = [int]$parsed.baselineStart
                baselineCount = [int]$parsed.baselineCount
                targetStart = [int]$parsed.targetStart
                targetCount = [int]$parsed.targetCount
                addedPhysicalLines = $addedLines.Count
                deletedPhysicalLines = $deletedLines.Count
                addedOwnership = [string]$addedOwnership.owner
                deletedOwnership = [string]$deletedOwnership.owner
                ownership = $hunkOwner
                platformAddedLines = $hunkPlatformAdded
                platformDeletedLines = $hunkPlatformDeleted
                compatibilityAddedLines = $hunkCompatibilityAdded
                compatibilityDeletedLines = $hunkCompatibilityDeleted
                productNativeAddedLines = $hunkProductAdded
                productNativeDeletedLines = $hunkProductDeleted
                baselineSymbols = $baselineSymbols
                targetSymbols = $targetSymbols
                semanticEvidence = $evidence
                hunkSha256 = Get-Sha256Text ([string]$parsed.header + "`n" + [string]::Join("`n", @($parsed.rawLines)) + "`n")
            })
        }
        if ($fileAdded -ne [int]$numstat[0] -or $fileDeleted -ne [int]$numstat[1]) {
            throw "G4 parsed hunk totals do not match Git numstat for $path."
        }
        [string[]]$distinctFileOwners = @(Get-OrdinalUnique @($fileOwners))
        $fileOwner = if ($distinctFileOwners.Count -eq 1) { $distinctFileOwners[0] } else { 'Mixed' }
        if ($fileOwner -eq 'Mixed' -and $symbolRows.Count -eq 0) { throw "G4 Mixed file lacks symbol-level ownership evidence: $path" }
        $changeType = if ($null -eq $baselineBlob) { 'Added' } elseif ($null -eq $targetBlob) { 'Deleted' } else { 'Modified' }
        [void]$rows.Add([ordered]@{
            path = $path
            changeType = $changeType
            ownership = $fileOwner
            addedPhysicalLines = [int]$numstat[0]
            deletedPhysicalLines = [int]$numstat[1]
            baselineBlob = $baselineBlob
            targetBlob = $targetBlob
            diffSha256 = $diffSha256
            hunks = @($hunkRows)
            symbolOwnership = @($symbolRows)
        })
        [void]$diffIdentity.Add("$path`t$baselineBlob`t$targetBlob`t$diffSha256")
        $totalHunks += $hunkRows.Count
    }

    if ($productNativeAdded -ne 0) {
        throw "G4 mandatory Runtime ProductNative additions must be zero by source-derived hunk accounting; found $productNativeAdded. offenders=$([string]::Join('; ', @($productNativeAdditionDiagnostics)))"
    }
    if ($productNativeDeleted -le 0) { throw 'G4 source-derived hunk accounting found no ProductNative removal.' }
    foreach ($source in $baselineProductSources) {
        if ($null -ne (Get-Blob $Target ([string]$source.path))) { throw "G4 target retains baseline ProductNative source: $($source.path)" }
    }
    $targetFriendText = Get-Text $Target $friendPath
    if ($null -ne $targetFriendText -and $targetFriendText.Contains('InternalsVisibleTo("AutoFishingMod")')) { throw 'G4 target retains the AutoFishing friend opening in mandatory Abstractions.' }
    $mandatoryFirstPartyFishingTokens = Count-SourceToken $Target 'src/DTMAPI.GameBridge.DolocTown' 'IFirstPartyFishing[A-Za-z0-9_]+'
    if ($mandatoryFirstPartyFishingTokens -ne 0) { throw "G4 target retains $mandatoryFirstPartyFishingTokens IFirstPartyFishing tokens in mandatory GameBridge." }

    $releaseBuilder = Get-Text $Target 'tools/scripts/build-release-workshop-packages.ps1'
    if ($null -eq $releaseBuilder) { throw 'G4 target release builder is missing.' }
    [string[]]$uniqueRuntimeDlls = @(Assert-ExactReleaseRuntimeDlls $releaseBuilder)
    if (-not $releaseBuilder.Contains('Copy-DirectoryContents -Source $outDir -Destination $runtimePayload -Include $runtimeFiles') -or
        -not $releaseBuilder.Contains("Assert-PlayerPackageExcludesQaHost -PackageRoot (Join-Path `$OutputRoot 'DTMAPI')") -or
        $uniqueRuntimeDlls -contains 'DTMAPI.GameBridge.DolocTown.QA.dll' -or
        [Text.RegularExpressions.Regex]::IsMatch($releaseBuilder, '(?m)^\s*\$runtimeFiles\s*=.*Yuuka\.DTMAPI\.AutoFishing\.dll')) {
        throw 'G4 target player package boundary no longer derives from the exact five Runtime files with QA/product exclusion.'
    }

    $catalogText = Get-Text $Target 'tools/release/dtmapi-product-catalog.json'
    if ($null -eq $catalogText) { throw 'G4 target product Catalog is missing.' }
    $catalog = ConvertFrom-JsonDocument $catalogText
    $autoFishingRows = @($catalog.products | Where-Object { [string]$_.uniqueId -ceq 'Yuuka.DTMAPI.AutoFishing' })
    if ($autoFishingRows.Count -ne 1) { throw 'G4 target Catalog must contain exactly one AutoFishing product row.' }
    $product = $autoFishingRows[0]
    if ([string]$product.packageDll -cne 'Yuuka.DTMAPI.AutoFishing.dll' -or [string]$product.sourceRoot -cne 'products/first-party/AutoFishing' -or
        [string]$product.codeModKind -cne 'Advanced' -or [string]$product.referencePolicyId -cne 'doloctown-23762374-autofishing-v1' -or
        [string]$product.nativeOwnership -cne 'ProductNative' -or [string]$product.productionBuildAuthority -cne 'DTMAPI Author SDK build/pack/deploy') {
        throw 'G4 target Catalog does not bind the canonical AutoFishing ProductNative Advanced SDK identity.'
    }

    $mandatoryBaseline = Get-TreeMetrics $Baseline $mandatoryRoots
    $mandatoryTarget = Get-TreeMetrics $Target $mandatoryRoots
    $productBaseline = Get-TreeMetrics $Baseline @('products/first-party/AutoFishing')
    $productTarget = Get-TreeMetrics $Target @('products/first-party/AutoFishing')
    $qaBaseline = Get-TreeMetrics $Baseline @('src/DTMAPI.GameBridge.DolocTown.QA', 'products/first-party/AutoFishing/qa')
    $qaTarget = Get-TreeMetrics $Target @('src/DTMAPI.GameBridge.DolocTown.QA', 'products/first-party/AutoFishing/qa')
    $compatBaseline = Get-TreeMetrics $Baseline @('src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation')
    $compatTarget = Get-TreeMetrics $Target @('src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation')
    if ([int]$compatTarget.fileCount -le 0 -or [int]$compatTarget.physicalLines -le 0) { throw 'G4 target deleted the frozen Compatibility debt instead of preserving it.' }

    $migrationDelta = Get-MigrationDelta $Baseline $Target
    $authorityReferences = @(Get-MigrationAuthorityReferences $Target)
    $productLegacyApiTokens = Count-SourceToken $Target 'products/first-party/AutoFishing/src' 'IFishingAutomationApi'
    $productFirstPartyTokens = Count-SourceToken $Target 'products/first-party/AutoFishing/src' 'IFirstPartyFishing[A-Za-z0-9_]*'
    $mandatoryHarmonyOwnerTokens = Count-SourceToken $Target 'src' 'dtmapi\.mod\.yuuka\.dtmapi\.autofishing'
    $compatibilityLegacyApiTokens = Count-SourceToken $Target 'src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation' 'IFishingAutomationApi'
    $compatibilityProductIdentityTokens = Count-SourceToken $Target 'src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation' 'Yuuka\.DTMAPI\.AutoFishing'
    if ($productLegacyApiTokens -ne 0 -or $productFirstPartyTokens -ne 0 -or $mandatoryHarmonyOwnerTokens -ne 0 -or
        $compatibilityLegacyApiTokens -le 0 -or $compatibilityProductIdentityTokens -le 0) {
        throw 'Consolidated migration consumer state does not prove product independence plus the non-empty frozen Compatibility route.'
    }

    return [ordered]@{
        schemaVersion = 3
        receiptKind = 'DTMAPI.Batch6.AutoFishingMigrationEvidence'
        status = 'Passed'
        capturedAtUtc = $Captured
        baselineCommit = $Baseline
        implementationCommit = $Target
        implementationCommittedAtUtc = $implementationTime.ToString('o')
        ownershipAlgorithm = [ordered]@{
            id = 'DTMAPI.Batch6.AutoFishingMigration.SourceDeltaOwnership.v3'
            baselineProductNativeSources = $baselineProductSources
            baselineProductNativeSourceCount = $baselineProductSources.Count
            baselineProductNativeSourceSha256 = Get-Sha256Text ([string]::Join("`n", $sourceIdentity) + "`n")
            baselineProductNativeSymbols = $baselineProductSymbols
            unknownAddedLineCount = 0
            hunkCount = $totalHunks
        }
        migrationDelta = $migrationDelta
        authorityReferences = $authorityReferences
        consumerState = [ordered]@{
            productLegacyFishingApiTokenCount = $productLegacyApiTokens
            productFirstPartyFishingTokenCount = $productFirstPartyTokens
            mandatoryCanonicalHarmonyOwnerTokenCount = $mandatoryHarmonyOwnerTokens
            mandatoryFirstPartyFishingTokenCount = $mandatoryFirstPartyFishingTokens
            compatibilityLegacyFishingApiTokenCount = $compatibilityLegacyApiTokens
            compatibilityProductIdentityTokenCount = $compatibilityProductIdentityTokens
        }
        mandatoryRoots = $mandatoryRoots
        mandatoryBaseline = $mandatoryBaseline
        mandatoryTarget = $mandatoryTarget
        changedMandatoryFileCount = $rows.Count
        changedMandatoryDiffSha256 = Get-Sha256Text ([string]::Join("`n", @($diffIdentity)) + "`n")
        changedMandatoryFiles = @($rows)
        mandatoryPlatformAddedLines = $platformAdded
        mandatoryPlatformDeletedLines = $platformDeleted
        mandatoryCompatibilityAddedLines = $compatibilityAdded
        mandatoryCompatibilityDeletedLines = $compatibilityDeleted
        mandatoryProductNativeAddedLines = $productNativeAdded
        mandatoryProductNativeDeletedLines = $productNativeDeleted
        mandatoryFirstPartyFishingTokenCount = $mandatoryFirstPartyFishingTokens
        mandatoryTargetProductNativeResidue = @()
        productBaseline = $productBaseline
        productTarget = $productTarget
        qaBaseline = $qaBaseline
        qaTarget = $qaTarget
        compatibilityBaseline = $compatBaseline
        compatibilityTarget = $compatTarget
        playerAssemblyModel = [ordered]@{
            mandatoryRuntimeDlls = $runtimeDlls
            optionalProductDlls = @('Yuuka.DTMAPI.AutoFishing.dll')
            qaDlls = @()
        }
        lifecycleModel = [ordered]@{
            coldDisabled = 'zero product assembly load and zero canonical Harmony owner'
            loadedF6Off = 'canonical process-lifetime patches may remain; zero recurring updater and zero active transaction/input/animation override'
            postLoadDisabled = 'restart-required; clean next restart has zero product assembly and patch owner'
        }
    }
}

function Invoke-SelfTest {
    [string[]]$symbols = @('FishingAutomationFeature', 'FishingPrimitiveSession')
    $rejected = 0
    $masked = Get-SideOwnership 'Added' 'src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/Fake.cs' "namespace DTMAPI.GameBridge.DolocTown { class FishingProductStateMachine {} }" @('internal sealed class FishingProductStateMachine {}') $symbols $false
    if ($masked.owner -ne 'ProductNative') { throw 'G4 self-test accepted a ProductNative addition hidden under a Compatibility path.' } else { $rejected++ }
    $identity = Get-SideOwnership 'Added' 'src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/Fake.cs' "class FishingCompatibilityFacade { string owner = `"dtmapi.mod.yuuka.dtmapi.autofishing`"; }" @('string owner = "dtmapi.mod.yuuka.dtmapi.autofishing";') $symbols $false
    if ($identity.owner -ne 'ProductNative') { throw 'G4 self-test accepted the real product identity inside mandatory Compatibility.' } else { $rejected++ }
    $mixedCompatibility = Get-SideOwnership 'Added' 'src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs' "class LegacyFishingAutomationService {} class FishingProductStateMachine {}" @('internal sealed class FishingProductStateMachine {}') $symbols $false
    if ($mixedCompatibility.owner -ne 'ProductNative') { throw 'G4 self-test accepted a ProductNative state machine co-located with the frozen Compatibility executor.' } else { $rejected++ }
    $compatibility = Get-SideOwnership 'Added' 'src/DTMAPI.GameBridge.DolocTown/Any.cs' "// frozen IFishingAutomationApi compatibility`nclass FishingCompatibilityFacade {}" @('class FishingCompatibilityFacade {}') $symbols $false
    if ($compatibility.owner -ne 'Compatibility') { throw 'G4 self-test rejected a source-proven frozen Compatibility delta.' }
    $migrationWarning = Get-SideOwnership 'Added' 'src/DTMAPI.GameBridge.DolocTown/Any.cs' "// frozen IFishingAutomationApi compatibility`nclass FishingCompatibilityFacade {}" @('Log("deprecated compatibility: migrate to Yuuka.DTMAPI.AutoFishing");') $symbols $false
    if ($migrationWarning.owner -ne 'Compatibility') { throw 'G4 self-test misclassified the frozen Compatibility migration warning as ProductNative.' }
    $platform = Get-SideOwnership 'Added' 'src/DTMAPI.Core/Manifesting/Policy.cs' 'namespace DTMAPI.Core.Manifesting { class AdvancedReferencePolicyRegistry {} }' @('const string AutoFishingPolicyId = "policy";') $symbols $false
    if ($platform.owner -ne 'Platform') { throw 'G4 self-test misclassified generic Advanced policy metadata.' }
    $boundPlatform = Get-SideOwnership 'Added' 'src/DTMAPI.Core/Manifesting/Policy.cs' 'namespace DTMAPI.Core.Manifesting { class AdvancedReferencePolicyRegistry {} }' @('const string AutoFishingUniqueId = "Yuuka.DTMAPI.AutoFishing";') $symbols $false
    if ($boundPlatform.owner -ne 'Platform') { throw 'G4 self-test misclassified a Runtime policy binding as ProductNative implementation.' }
    $parsed = @(ConvertTo-DiffHunks @('diff --git a/a.cs b/a.cs', '@@ -2,1 +2,2 @@', '-old', '+new', '+next'))
    if ($parsed.Count -ne 1 -or $parsed[0].baselineStart -ne 2 -or $parsed[0].baselineCount -ne 1 -or $parsed[0].targetStart -ne 2 -or $parsed[0].targetCount -ne 2 -or $parsed[0].rawLines.Count -ne 3) {
        throw 'G4 self-test failed exact unified-diff hunk parsing.'
    }
    [string[]]$runtimeFixture = @(Assert-ExactReleaseRuntimeDlls "`$runtimeFiles = @('DTMAPI.BepInExBootstrap.dll', 'DTMAPI.Abstractions.dll', 'DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.dll', 'DTMAPI.ModConfigMenu.dll')")
    if (($runtimeFixture -join '|') -cne ((@($runtimeDlls) | Sort-Object) -join '|')) { throw 'G4 self-test failed source-derived Runtime DLL parsing.' }
    try { $null = Assert-ExactReleaseRuntimeDlls "`$runtimeFiles = @('DTMAPI.Core.dll', 'DTMAPI.GameBridge.DolocTown.QA.dll')"; throw 'expected extra DLL rejection' } catch { if ($_.Exception.Message -eq 'expected extra DLL rejection') { throw }; $rejected++ }
    $candidate = [ordered]@{ changedMandatoryFiles = @([ordered]@{ path = 'mixed.cs'; targetBlob = 'a'; hunks = @([ordered]@{ ordinal = 1; ownership = 'Mixed' }) }) }
    $missingHunk = [ordered]@{ changedMandatoryFiles = @([ordered]@{ path = 'mixed.cs'; targetBlob = 'a'; hunks = @() }) }
    $targetBlobDrift = [ordered]@{ changedMandatoryFiles = @([ordered]@{ path = 'mixed.cs'; targetBlob = 'b'; hunks = @([ordered]@{ ordinal = 1; ownership = 'Mixed' }) }) }
    $reclassified = [ordered]@{ changedMandatoryFiles = @([ordered]@{ path = 'mixed.cs'; targetBlob = 'a'; hunks = @([ordered]@{ ordinal = 1; ownership = 'Platform' }) }) }
    foreach ($mutation in @($missingHunk, $targetBlobDrift, $reclassified)) {
        if (Test-CanonicalObjectEquality $candidate $mutation) { throw 'G4 self-test failed to reject a receipt source-binding mutation.' }
        $rejected++
    }
    try { Assert-Chronology ([DateTimeOffset]'2026-07-20T01:00:00Z') ([DateTimeOffset]'2026-07-20T00:59:59Z') $null; throw 'expected chronology rejection' } catch { if ($_.Exception.Message -eq 'expected chronology rejection') { throw }; $rejected++ }
    try { Assert-Chronology ([DateTimeOffset]'2026-07-20T01:00:00Z') ([DateTimeOffset]'2026-07-20T01:01:00Z') ([Nullable[DateTimeOffset]]([DateTimeOffset]'2026-07-20T01:01:00Z')); throw 'expected chronology rejection' } catch { if ($_.Exception.Message -eq 'expected chronology rejection') { throw }; $rejected++ }
    Write-Host "Batch 6 consolidated AutoFishing migration evidence self-test: PASS (mutationsRejected=$rejected)."
}

if ($SelfTest) { Invoke-SelfTest; return }

$baselineCommit = [string](@(Invoke-GitLines @('rev-parse', "$BaselineGitRef^{commit}"))[0])
$implementationCommit = [string](@(Invoke-GitLines @('rev-parse', "$ImplementationGitRef^{commit}"))[0])
if ($baselineCommit -cne $immutableBaseline) { throw "AutoFishing migration baseline must remain immutable $immutableBaseline; received $baselineCommit." }
Assert-PreMigrationBaseline $implementationCommit
$outputFull = if ([IO.Path]::IsPathRooted($OutputPath)) { [IO.Path]::GetFullPath($OutputPath) } else { [IO.Path]::GetFullPath((Join-Path $repo $OutputPath)) }
$repoPrefix = $repo.TrimEnd([char[]]@('\', '/')) + [IO.Path]::DirectorySeparatorChar
if (-not $outputFull.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "AutoFishing migration evidence output must remain inside the repository: $outputFull" }
$relativeOutput = $outputFull.Substring($repoPrefix.Length).Replace('\', '/')

if ($Check) {
    if (-not (Test-Path -LiteralPath $outputFull -PathType Leaf)) { throw "Batch 6 AutoFishing migration evidence is missing: $outputFull" }
    $existing = ConvertFrom-JsonDocument ([IO.File]::ReadAllText($outputFull, [Text.Encoding]::UTF8))
    if ([string]$existing.baselineCommit -cne $baselineCommit -or [string]$existing.implementationCommit -cne $implementationCommit) { throw 'Batch 6 G4 receipt commit binding drifted.' }
    $normalizedCaptured = Normalize-CapturedAtUtc ([string]$existing.capturedAtUtc)
    $candidate = Build-Receipt $baselineCommit $implementationCommit $normalizedCaptured
    $existing.capturedAtUtc = $normalizedCaptured
    if (-not (Test-CanonicalObjectEquality $existing $candidate)) { throw 'Batch 6 G4 receipt is stale or its source/hunk/symbol/blob/diff facts were edited.' }
    $receiptCommit = @(Invoke-GitLines @('log', '-1', '--format=%H', '--', $relativeOutput))
    if ($receiptCommit.Count -ne 1) { throw 'G4 checked receipt must already be committed before it can become authority.' }
    $receiptCommitId = [string]$receiptCommit[0]
    $committedBlob = Get-Blob $receiptCommitId $relativeOutput
    $workingBlob = [string](@(Invoke-GitLines @('hash-object', '--', $outputFull))[0])
    if ([string]::IsNullOrWhiteSpace($committedBlob) -or $committedBlob -cne $workingBlob) { throw 'G4 checked receipt bytes are not the committed receipt authority.' }
    & git -C $repo merge-base --is-ancestor $implementationCommit $receiptCommitId 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'G4 receipt commit must descend from the bound implementation commit.' }
    $implementationTime = [DateTimeOffset]::Parse([string](@(Invoke-GitLines @('show', '-s', '--format=%cI', $implementationCommit))[0])).ToUniversalTime()
    $receiptTime = [DateTimeOffset]::Parse([string](@(Invoke-GitLines @('show', '-s', '--format=%cI', $receiptCommitId))[0])).ToUniversalTime()
    $capturedTime = [DateTimeOffset]::Parse($normalizedCaptured, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    Assert-Chronology $implementationTime $capturedTime ([Nullable[DateTimeOffset]]$receiptTime)
    Write-Host "Batch 6 AutoFishing migration evidence: PASS (source-derived mandatory ProductNative additions=0; implementation=$implementationCommit; evidence=$receiptCommitId)."
    return
}

if ([string]::IsNullOrWhiteSpace($CapturedAtUtc)) { $CapturedAtUtc = (Get-Date).ToUniversalTime().ToString('o') }
$receipt = Build-Receipt $baselineCommit $implementationCommit (Normalize-CapturedAtUtc $CapturedAtUtc)
$parent = Split-Path -Parent $outputFull
if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
# Pretty-print indentation differs between Windows PowerShell 5.1 and current
# PowerShell. Compact JSON keeps the committed receipt byte-reproducible across
# both supported hosts while the schema remains the human-readable authority.
$json = $receipt | ConvertTo-Json -Depth 40 -Compress
# Windows PowerShell escapes angle brackets while current PowerShell does not.
# They are valid unescaped JSON string characters, so normalize to the latter.
$json = $json.Replace('\u003c', '<').Replace('\u003e', '>')
[IO.File]::WriteAllText($outputFull, ($json + "`n"), $utf8)
Write-Host "Wrote Batch 6 consolidated AutoFishing migration evidence: $outputFull"
