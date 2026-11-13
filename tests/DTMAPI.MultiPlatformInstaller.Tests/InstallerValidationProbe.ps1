param(
    [Parameter(Mandatory = $true)] [string] $RepoRoot,
    [string] $LifecycleScriptPath = '',
    [switch] $ReportOnly
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path $RepoRoot 'build/windows-installer/common.ps1')
. (Join-Path $RepoRoot 'build/windows-installer/release-common.ps1')

# Import actual functions without the full package/host main body; the report
# probe below separately executes selected top-level statements and catch.
$scriptPath = if ($LifecycleScriptPath) { $LifecycleScriptPath } else { Join-Path $RepoRoot 'build/test-multiplatform-runtime-installer.ps1' }
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors)
if (@($parseErrors).Count -ne 0) { throw 'Installer lifecycle script has parser errors.' }
foreach ($definition in @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $false))) {
    . ([scriptblock]::Create($definition.Extent.Text))
}

$testBase = [IO.Path]::GetFullPath((Join-Path $RepoRoot 'tmp/test-runs'))
$root = [IO.Path]::GetFullPath((Join-Path $testBase ('installer-validation-probe-' + [Guid]::NewGuid().ToString('N'))))
if (-not (Test-DtmApiPathIsSameOrChild -Child $root -Parent $testBase)) { throw 'Probe root escaped managed test storage.' }

function Reset-Probe {
    $script:Calls = New-Object 'System.Collections.Generic.List[object]'
    $script:FailAt = 0
    $script:ProbeMode = 'transition'
    $script:DtmApiMultiPlatformCandidateProvenanceResults = @{}
    $script:DtmApiMultiPlatformVersionTransitionResults = @{}
    foreach ($lane in @('Windows', 'Linux')) {
        $script:DtmApiMultiPlatformCandidateProvenanceResults[$lane] = [pscustomobject]@{ Host = $lane; State = 'not-run'; CompletedVariants = 0; Reason = 'Host lane has not executed.' }
        $script:DtmApiMultiPlatformVersionTransitionResults[$lane] = [pscustomobject]@{ Host = $lane; State = 'not-run'; CompletedSteps = 0; CompletedRejections = 0; Routes = @(); Reason = 'Host lane has not executed.' }
    }
}

function New-VersionOnlyProbeInput {
    param([string] $Name, [int] $Schema, [string] $Version, [string] $Binary)
    $path = Join-Path $root $Name
    # These intentionally incomplete call fixtures cannot pass PackageLayout.Load:
    # no build identity, source receipts, Runtime binaries or executable host.
    Write-DtmApiMultiPlatformInstallerJsonFixture -Path (Join-Path $path 'Content/DTMAPI/release-manifest.json') -Value @{
        FixtureOnly = $true; DTMAPIVersion = $Version; BinaryVersion = $Binary; IncludedAssemblies = @()
    }
    Write-DtmApiMultiPlatformInstallerJsonFixture -Path (Join-Path $path 'Content/DTMAPIInstaller/multiplatform-package.json') -Value @{
        FixtureOnly = $true; SchemaVersion = $Schema; RuntimeVersion = $Version; InstallerBuildCommit = ''
        RuntimeSource = @{ Kind = 'Candidate'; BuildCommit = ''; SharedFiles = @('fixture-a', 'fixture-b') }
    }
    Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $path 'Content/DTMAPIInstaller/tools/collect-logs.ps1') -Text '# fixture only'
    Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $path 'DTMAPI-MultiPlatform-Installer.exe') -Text "non-executable call fixture: $Name"
    return $path
}

# Only the native host invocation is replaced. The actual transition/provenance
# functions still copy inputs, read/write version metadata, verify unchanged
# trees/sentinels, select host/package pairs, count successes and propagate failure.
function Invoke-DtmApiMultiPlatformInstallerHost {
    param([string] $HostKind, [string] $HostPath, [string] $Action, [string] $Package, [string] $Game)
    $releasePath = Join-Path $Package 'Content/DTMAPI/release-manifest.json'
    $release = Get-Content -LiteralPath $releasePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $manifest = Get-Content -LiteralPath (Join-Path $Package 'Content/DTMAPIInstaller/multiplatform-package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $tool = [IO.File]::ReadAllText((Join-Path $Package 'Content/DTMAPIInstaller/tools/collect-logs.ps1'))
    $script:Calls.Add([pscustomobject]@{ HostPath = $HostPath; Package = $Package; Action = $Action; Version = $release.DTMAPIVersion; Manifest = $manifest; SharedToolChanged = $tool.Contains('# changed') })
    if ($script:Calls.Count -eq $script:FailAt) { return [pscustomobject]@{ ExitCode = 89; Text = 'probe-injected host failure' } }
    if ($script:ProbeMode -eq 'provenance') {
        return [pscustomobject]@{ ExitCode = 3; Text = 'DTM-E1201: bounded rejection response' }
    }
    $ownPackage = Split-Path -Parent $HostPath
    if ($manifest.SchemaVersion -eq 2 -and $ownPackage -cne $Package) {
        return [pscustomobject]@{ ExitCode = 3; Text = 'DTM-E1201: schema-2 host/package mismatch' }
    }
    $installed = Join-Path $Game 'DTMAPI/release-manifest.json'
    if ($Action -eq 'install') {
        if (Test-Path -LiteralPath $installed) {
            $old = Get-Content -LiteralPath $installed -Raw -Encoding UTF8 | ConvertFrom-Json
            if ([version]$old.DTMAPIVersion -gt [version]$release.DTMAPIVersion) {
                return [pscustomobject]@{ ExitCode = 3; Text = 'DTM-E1202: downgrade' }
            }
        }
        Copy-Item -LiteralPath $releasePath -Destination $installed -Force
    }
    elseif ($Action -eq 'uninstall') { Remove-Item -LiteralPath $installed -Force }
    return [pscustomobject]@{ ExitCode = 0; Text = 'Files: HEALTHY' }
}

function Invoke-TransitionProbe {
    param([string] $Name, [string] $Package, [string] $Previous, [string] $Previous070)
    Test-DtmApiMultiPlatformVersionTransition -HostKind Windows -HostPath (Join-Path $Package 'DTMAPI-MultiPlatform-Installer.exe') -Package $Package -PreviousPackage $Previous -Previous070Package $Previous070 -TestRoot (Join-Path $root $Name) -HostReceipts @()
}

function Assert-ProbeFailure {
    param([scriptblock] $Action, [string] $Message)
    $failed = $false
    try { & $Action }
    catch { $failed = $_.Exception.Message -match $Message }
    Assert-DtmApiMultiPlatformInstallerTest -Condition $failed -Message "Expected propagated failure matching $Message"
}

function Test-TopLevelLifecycleReport {
    # Execute the script's actual initialization, lifecycle result assignments,
    # completion bookkeeping, final summary and catch. Omit unrelated package
    # preparation/WSL probes; only the two call boundaries return/throw controls.
    $topStatements = @($ast.EndBlock.Statements)
    $topTry = @($topStatements | Where-Object { $_ -is [Management.Automation.Language.TryStatementAst] -and $_.CatchClauses.Count -gt 0 })
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($topTry.Count -eq 1) -Message 'Expected one top-level installer report try/catch.'
    $topTry = $topTry[0]
    $resultInitializers = @($topStatements | Where-Object {
        $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left -is [Management.Automation.Language.VariableExpressionAst] -and
        $_.Left.VariablePath.UserPath -in @('windowsResult', 'linuxResult') -and $_.Extent.StartOffset -lt $topTry.Extent.StartOffset
    })
    $initStart = ($resultInitializers | Sort-Object { $_.Extent.StartOffset } | Select-Object -First 1).Extent.StartOffset
    $initializers = @($topStatements | Where-Object { $_.Extent.StartOffset -ge $initStart -and $_.Extent.EndOffset -lt $topTry.Extent.StartOffset })
    $body = @($topTry.Body.Statements)
    $windowsAssignment = @($body | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.VariablePath.UserPath -eq 'windowsResult' })[0]
    $linuxIf = @($body | Where-Object {
        $_ -is [Management.Automation.Language.IfStatementAst] -and $null -ne $_.Find({ param($node)
            $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left -is [Management.Automation.Language.VariableExpressionAst] -and $node.Left.VariablePath.UserPath -eq 'linuxResult'
        }, $true)
    })[0]
    $windowsStart = @($body | Where-Object {
        $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.VariablePath.UserPath -in @('activeHost', 'windowsState') -and $_.Extent.EndOffset -lt $windowsAssignment.Extent.StartOffset
    })
    $windowsCompletion = @($body | Where-Object { $_.Extent.StartOffset -ge $windowsAssignment.Extent.StartOffset -and $_.Extent.EndOffset -lt $linuxIf.Extent.StartOffset })
    $linuxBody = @($linuxIf.Clauses[0].Item2.Statements)
    $linuxRunnable = @($linuxBody | Where-Object { $_ -is [Management.Automation.Language.IfStatementAst] })[0].Clauses[0].Item2.Statements
    $linuxStart = @($linuxBody | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.VariablePath.UserPath -in @('activeHost', 'linuxState') })
    $linuxTransition = @($linuxRunnable | Where-Object {
        $null -ne $_.Find({ param($node) $node -is [Management.Automation.Language.CommandAst] -and $node.GetCommandName() -eq 'Test-DtmApiMultiPlatformVersionTransition' }, $true)
    })[0]
    $linuxAssignment = @($linuxRunnable | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.VariablePath.UserPath -eq 'linuxResult' })[0]
    $linuxCompletion = @($linuxRunnable | Where-Object { $_.Extent.StartOffset -ge $linuxAssignment.Extent.StartOffset })
    $summary = @($body | Where-Object { $_.Extent.StartOffset -gt $linuxIf.Extent.EndOffset })

    function Invoke-DtmApiMultiPlatformInstallerLifecycle {
        param([string] $HostKind, [switch] $SeedLegacyFourFileBepInEx)
        $lifecycleCalls.Add($HostKind)
        if ($scenario.FailHost -eq $HostKind) { throw $scenario.Failure }
        if ($HostKind -eq 'Windows') { return $completedWindows }
        return $completedLinux
    }
    function Test-DtmApiMultiPlatformVersionTransition {
        param([string] $HostKind)
        $transitionCalls.Add($HostKind)
        if ($scenario.FailTransition) { throw $scenario.Failure }
    }

    foreach ($scenario in @(
        @{ Name = 'windows-complete-linux-transition-fails'; FailHost = ''; FailTransition = $true; LinuxAvailable = $true; Failure = 'probe Linux transition failure' },
        @{ Name = 'windows-complete-linux-collect-fails'; FailHost = 'Linux'; FailTransition = $false; LinuxAvailable = $true; Failure = 'probe Linux collect failure' },
        @{ Name = 'windows-fails-linux-not-run'; FailHost = 'Windows'; FailTransition = $false; LinuxAvailable = $true; Failure = 'probe Windows lifecycle failure' },
        @{ Name = 'linux-unavailable-windows-passes'; FailHost = ''; FailTransition = $false; LinuxAvailable = $false; Failure = '' },
        @{ Name = 'both-lifecycle-results-complete'; FailHost = ''; FailTransition = $false; LinuxAvailable = $true; Failure = '' }
    )) {
        $lifecycleCalls = New-Object 'System.Collections.Generic.List[string]'
        $transitionCalls = New-Object 'System.Collections.Generic.List[string]'
        $reports = New-Object 'System.Collections.Generic.List[object]'
        $completedWindows = [pscustomobject]@{ FixtureOnly = $true; Host = 'Windows'; GameRoot = 'bounded-report/windows'; LogBundles = 2 }
        $completedLinux = [pscustomobject]@{ FixtureOnly = $true; Host = 'Linux'; GameRoot = 'bounded-report/linux'; LogBundles = 2 }
        $windowsHost = 'bounded-report/windows-host'
        $linuxHost = 'bounded-report/linux-host'
        $sourcePackage = 'FixtureOnly-top-level-report'
        $package = $sourcePackage
        $testRoot = $root
        $unicode = ''
        $hostReceipts = @()
        $PreviousPackageRoot = ''
        $Previous070PackageRoot = ''
        $selected = @($windowsStart) + @($windowsCompletion)
        if ($scenario.LinuxAvailable) { $selected += @($linuxStart) + @($linuxTransition) + @($linuxCompletion) }
        $selected += @($summary)
        $source = ($initializers.Extent.Text -join "`n") + "`ntry {`n" + ($selected.Extent.Text -join "`n") + "`n}`n" + $topTry.CatchClauses[0].Extent.Text
        $thrown = ''
        try { & ([scriptblock]::Create($source)) | ForEach-Object { $reports.Add($_) } }
        catch { $thrown = $_.Exception.Message }
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($thrown -ceq $scenario.Failure) -Message "Top-level catch swallowed/replaced the original failure for $($scenario.Name): $thrown"
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($reports.Count -eq 1) -Message "Top-level report count differs for $($scenario.Name)."
        $report = $reports[0]
        Write-Host ("Actual top-level report {0}: {1}" -f $scenario.Name, ($report | ConvertTo-Json -Depth 8 -Compress))
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($null -ne $report.PSObject.Properties['Windows'] -and $null -ne $report.PSObject.Properties['WindowsState'] -and $null -ne $report.PSObject.Properties['Linux'] -and $null -ne $report.PSObject.Properties['LinuxState']) -Message "Top-level report dropped lifecycle results/states for $($scenario.Name)."
        if ($scenario.Failure) {
            $expectedFailedHost = if ($scenario.FailHost -eq 'Windows') { 'Windows' } else { 'Linux' }
            Assert-DtmApiMultiPlatformInstallerTest -Condition (-not $report.ExecutedHostsPassed -and $report.Failure -ceq $scenario.Failure -and $report.FailedHost -ceq $expectedFailedHost) -Message 'Failed matrix lost the original failure/lane or claimed executed-host success.'
        }
        if ($scenario.FailHost -eq 'Windows') {
            Assert-DtmApiMultiPlatformInstallerTest -Condition (-not $report.Passed -and $null -eq $report.Windows -and $null -eq $report.Linux -and $report.WindowsState -like 'FAILED:*' -and $report.LinuxState -like 'not-run:*' -and ($lifecycleCalls -join '|') -ceq 'Windows' -and $transitionCalls.Count -eq 0) -Message 'Windows failure did not leave Linux explicitly unexecuted.'
        }
        else {
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($report.WindowsState -eq 'PASS' -and [object]::ReferenceEquals($report.Windows, $completedWindows)) -Message 'Completed Windows lifecycle result was lost or reclassified.'
            if ($scenario.Failure) {
                Assert-DtmApiMultiPlatformInstallerTest -Condition (-not $report.Passed -and $null -eq $report.Linux -and $report.LinuxState -like 'FAILED:*' -and $report.Failure -ceq $scenario.Failure -and ($transitionCalls -join '|') -ceq 'Linux') -Message 'Linux failure did not preserve the completed Windows result and original failure.'
                $expectedCalls = if ($scenario.FailTransition) { 'Windows' } else { 'Windows|Linux' }
                Assert-DtmApiMultiPlatformInstallerTest -Condition (($lifecycleCalls -join '|') -ceq $expectedCalls) -Message 'Failed Linux phase claimed an unreturned lifecycle result.'
            }
            elseif (-not $scenario.LinuxAvailable) {
                Assert-DtmApiMultiPlatformInstallerTest -Condition (-not $report.Passed -and $report.ExecutedHostsPassed -and $null -eq $report.Linux -and $report.LinuxState -like 'not-run:*' -and ($lifecycleCalls -join '|') -ceq 'Windows' -and $transitionCalls.Count -eq 0) -Message 'Unavailable Linux did not retain Windows success and normal PARTIAL semantics.'
            }
            else {
                Assert-DtmApiMultiPlatformInstallerTest -Condition ($report.Passed -and $report.ExecutedHostsPassed -and $report.LinuxState -eq 'PASS' -and [object]::ReferenceEquals($report.Linux, $completedLinux) -and ($lifecycleCalls -join '|') -ceq 'Windows|Linux') -Message 'Normal summary lost a completed lifecycle result.'
            }
        }
        Write-Host "PASS actual top-level report/catch: $($scenario.Name)"
    }
    Write-Host 'Top-level lifecycle report regression: PASS (5 bounded cases; actual package/host lifecycle not-run).'
}

try {
    if ($ReportOnly) { Test-TopLevelLifecycleReport; return }
    $current = New-VersionOnlyProbeInput 'current' 2 $script:DtmApiReleaseVersion $script:DtmApiBinaryVersion
    $previous061 = New-VersionOnlyProbeInput 'retained-061' 1 '0.6.1' '0.6.1.0'
    $previous070 = New-VersionOnlyProbeInput 'retained-070' 2 '0.7.0' '0.7.0.0'

    Reset-Probe
    Invoke-TransitionProbe 'current-transition' $current $previous061 $previous070
    $report = $script:DtmApiMultiPlatformVersionTransitionResults.Windows
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($report.State -eq 'passed' -and $report.CompletedSteps -eq 18 -and $report.CompletedRejections -eq 8 -and $script:Calls.Count -eq 26) -Message 'Current transition returned early or counted unexecuted steps.'
    Assert-DtmApiMultiPlatformInstallerTest -Condition (($report.Routes.From -join '|') -ceq '0.6.1|0.7.0' -and @($report.Routes | Where-Object { $_.State -ne 'passed' -or $_.To -cne $script:DtmApiReleaseVersion -or $_.CompletedSteps -ne 9 -or $_.CompletedRejections -ne 4 }).Count -eq 0) -Message 'Required predecessor routes did not both complete.'
    $old070Calls = @($script:Calls | Where-Object { $_.Version -eq '0.7.0' })
    Assert-DtmApiMultiPlatformInstallerTest -Condition (@($old070Calls | Where-Object { (Split-Path -Parent $_.HostPath) -cne $_.Package }).Count -eq 0) -Message 'New host was mixed with schema-2 predecessor bytes.'
    $old061Calls = @($script:Calls | Where-Object { $_.Version -eq '0.6.1' })
    Assert-DtmApiMultiPlatformInstallerTest -Condition (@($old061Calls | Where-Object { $_.Action -eq 'status' -and (Split-Path -Parent $_.HostPath) -ceq $current }).Count -eq 1) -Message 'Retained schema-1 new-reader status path was lost.'
    $summary = Get-DtmApiMultiPlatformInstallerValidationSummary
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($summary.VersionTransitions[1].State -eq 'not-run' -and $summary.VersionTransitions[1].CompletedSteps -eq 0) -Message 'Unexecuted Linux host claimed transition PASS.'
    Write-Host 'PASS current transition calls, predecessor pairing and unexecuted-host report'

    Reset-Probe
    Invoke-TransitionProbe 'retained-transition' $previous070 $previous061 ''
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls.Count -eq 13 -and $script:DtmApiMultiPlatformVersionTransitionResults.Windows.CompletedSteps -eq 9) -Message 'Original 0.6.1 -> 0.7.0 flow did not run.'
    Reset-Probe
    Invoke-TransitionProbe 'legacy-transition' $previous061 $previous070 $current
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls.Count -eq 0 -and $script:DtmApiMultiPlatformVersionTransitionResults.Windows.State -eq 'not-run') -Message 'Legacy package with predecessor arguments falsely claimed PASS.'
    Write-Host 'PASS retained 0.7.0 transition and legacy not-run'

    foreach ($missing in @('061', '070')) {
        Reset-Probe
        $old061 = if ($missing -eq '061') { '' } else { $previous061 }
        $old070 = if ($missing -eq '070') { '' } else { $previous070 }
        Assert-ProbeFailure { Invoke-TransitionProbe "missing-$missing" $current $old061 $old070 } 'lifecycle requires -Previous'
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls.Count -eq 0 -and $script:DtmApiMultiPlatformVersionTransitionResults.Windows.State -eq 'blocked') -Message 'Missing mandatory predecessor was silently skipped or invoked a host.'
    }
    Write-Host 'PASS each missing predecessor blocks before host calls'

    foreach ($failureAt in @(3, 4, 13, 16)) {
        Reset-Probe
        $script:FailAt = $failureAt
        Assert-ProbeFailure { Invoke-TransitionProbe "failed-$failureAt" $current $previous061 $previous070 } 'probe-injected host failure'
        $report = $script:DtmApiMultiPlatformVersionTransitionResults.Windows
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($report.State -eq 'failed' -and $script:Calls.Count -eq $failureAt -and $report.CompletedSteps + $report.CompletedRejections -eq $failureAt - 1) -Message 'Host failure was swallowed or counted as a completed action.'
    }
    Write-Host 'PASS install/rejection/uninstall/second-route failure propagation and partial counts'

    $badBinary = New-VersionOnlyProbeInput 'wrong-binary' 2 $script:DtmApiReleaseVersion '0.0.0.0'
    $unknown = New-VersionOnlyProbeInput 'unknown-version' 2 '9.9.9' '9.9.9.0'
    foreach ($bad in @($badBinary, $unknown)) {
        Reset-Probe
        Assert-ProbeFailure { Invoke-TransitionProbe 'invalid-tuple' $bad $previous061 $previous070 } 'authority tuple'
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls.Count -eq 0 -and $script:DtmApiMultiPlatformVersionTransitionResults.Windows.State -eq 'failed') -Message 'Unsupported tuple escaped validation.'
    }
    Reset-Probe
    Assert-ProbeFailure { Invoke-TransitionProbe 'wrong-predecessor' $current $previous070 $previous061 } 'must be schema 1'
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls.Count -eq 0) -Message 'Mismatched predecessor invoked a host.'
    Write-Host 'PASS unsupported current tuples and wrong predecessor rejected'

    Reset-Probe
    $script:ProbeMode = 'provenance'
    Test-DtmApiMultiPlatformCandidateProvenance -HostKind Windows -HostPath (Join-Path $current 'DTMAPI-MultiPlatform-Installer.exe') -Package $current -TestRoot (Join-Path $root 'provenance')
    $summary = Get-DtmApiMultiPlatformInstallerValidationSummary
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls.Count -eq 8 -and $summary.CandidateProvenanceVariantsPerHost.Windows -eq 8 -and $summary.CandidateProvenanceVariantsPerHost.Linux -eq 0 -and $summary.CandidateProvenance[1].State -eq 'not-run') -Message 'Provenance counts did not follow actual calls independently of predecessor parameters.'
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls[0].Manifest.SchemaVersion -eq 3 -and $script:Calls[1].Manifest.RuntimeVersion -eq '0.8.0' -and $script:Calls[2].Manifest.RuntimeSource.Kind -eq 'ObservedPublished' -and $script:Calls[3].Manifest.SourceWorkshopManifestId -eq '918505309011394484' -and $script:Calls[4].Manifest.RuntimeSource.BuildCommit -eq ('0' * 40) -and $script:Calls[5].Manifest.InstallerBuildCommit -eq ('0' * 40) -and @($script:Calls[6].Manifest.RuntimeSource.SharedFiles).Count -eq 1 -and $script:Calls[7].SharedToolChanged) -Message 'Actual provenance mutations did not reach host calls.'
    Assert-ProbeFailure { Invoke-TransitionProbe 'missing-after-provenance' $current '' '' } 'lifecycle requires -Previous'
    Assert-DtmApiMultiPlatformInstallerTest -Condition ((Get-DtmApiMultiPlatformInstallerValidationSummary).CandidateProvenanceVariantsPerHost.Windows -eq 8) -Message 'Missing predecessor erased executed provenance coverage.'
    Reset-Probe
    $script:ProbeMode = 'provenance'
    $script:FailAt = 5
    Assert-ProbeFailure { Test-DtmApiMultiPlatformCandidateProvenance -HostKind Windows -HostPath (Join-Path $current 'DTMAPI-MultiPlatform-Installer.exe') -Package $current -TestRoot (Join-Path $root 'failed-provenance') } 'probe-injected host failure'
    $summary = Get-DtmApiMultiPlatformInstallerValidationSummary
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($summary.CandidateProvenance[0].State -eq 'failed' -and $summary.CandidateProvenanceVariantsPerHost.Windows -eq 4) -Message 'Failed provenance variant claimed all eight completed.'
    Reset-Probe
    Test-DtmApiMultiPlatformCandidateProvenance -HostKind Windows -HostPath (Join-Path $previous061 'DTMAPI-MultiPlatform-Installer.exe') -Package $previous061 -TestRoot (Join-Path $root 'legacy-provenance')
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($script:Calls.Count -eq 0 -and (Get-DtmApiMultiPlatformInstallerValidationSummary).CandidateProvenanceVariantsPerHost.Windows -eq 0) -Message 'Legacy schema claimed Candidate variants.'
    Write-Host 'PASS actual provenance variants, independent counts, failed partial count and legacy not-run'
    Test-TopLevelLifecycleReport
}
finally {
    if (Test-Path -LiteralPath $root) {
        if (-not (Test-DtmApiPathIsSameOrChild -Child ([IO.Path]::GetFullPath($root)) -Parent $testBase) -or (Split-Path -Leaf $root) -notmatch '^installer-validation-probe-[0-9a-f]{32}$') { throw 'Refusing escaped probe cleanup.' }
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}
Write-Host 'Installer validation bounded call regression: PASS; real package/host lifecycle not-run; managed fixtures removed.'
