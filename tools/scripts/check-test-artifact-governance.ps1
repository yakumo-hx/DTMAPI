param([switch] $RunCleanupFixture)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

function Assert-SourceContains {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string[]] $Patterns
    )

    $text = Get-Content -Raw -LiteralPath $Path
    foreach ($pattern in $Patterns) {
        if (-not $text.Contains($pattern)) {
            throw "Test artifact governance check failed: '$Path' is missing '$pattern'."
        }
    }
}

$sessionSource = Join-Path $repo 'tests\Shared\DtmApiTestSession.cs'
Assert-SourceContains -Path $sessionSource -Patterns @(
    'owner = Owner',
    'FileShare.None',
    'DTMAPI_KEEP_FAILED_TEST_TEMP',
    'RetainedSessionAgeLimit = TimeSpan.FromHours(24)',
    'RetainedSessionByteLimit = 5L * 1024 * 1024 * 1024',
    'Environment.SetEnvironmentVariable("TEMP", RootPath)',
    'DeleteDirectoryWithRetries(RootPath)'
)
foreach ($project in @('DTMAPI.UnitTests', 'DTMAPI.QaUnitTests')) {
    Assert-SourceContains -Path (Join-Path $repo "tests\$project\$project.csproj") -Patterns @('..\Shared\DtmApiTestSession.cs')
    Assert-SourceContains -Path (Join-Path $repo "tests\$project\Program.cs") -Patterns @(
        'DtmApiTestSession.Start(',
        'testSession.MarkSucceeded()',
        'testSession.MarkFailed(ex)'
    )
}
Assert-SourceContains -Path (Join-Path $repo 'tools\scripts\test.ps1') -Patterns @(
    "Join-Path `$repo 'tmp\test-runs'",
    'DTMAPI_TEST_TEMP_ROOT',
    'DTMAPI_KEEP_FAILED_TEST_TEMP',
    'check-test-artifact-governance.ps1',
    'release-autofishing-acceptance-',
    'Release AutoFishing tests leaked',
    '.dtmapi-release-autofishing-test-owner',
    '$reparseDescendants = @(',
    '$reparseDescendants.Count -eq 0'
)
Assert-SourceContains -Path (Join-Path $repo 'tools\scripts\test-batch6-autofishing-behavior-matrix.ps1') -Patterns @(
    'DTMAPI_TEST_TEMP_ROOT',
    'new direct child of the managed test root',
    'ReparsePoint',
    'Remove-Item -LiteralPath $TestRoot -Recurse -Force'
)
Assert-SourceContains -Path (Join-Path $repo 'tools\scripts\test-batch6-autofishing-manager-lifecycle.ps1') -Patterns @(
    'Assert-Retired',
    'fail before creating evidence, acquiring the Runtime lock, deploying a package, or launching the game',
    'Batch 6 AutoFishing Manager lifecycle retirement tests passed.'
)
Assert-SourceContains -Path (Join-Path $repo 'tools\scripts\run-game-smoke.ps1') -Patterns @(
    "[string] `$FatalWindowProcessDumpMode = 'None'",
    "'DTMAPI.DumpCapture'",
    "'DTMAPI.ProcessDumpEvidence'",
    'Copy-DtmApiVerifiedFile',
    'DeletedAfterVerifiedHandoff',
    'Complete-DumpTempSession'
)
Assert-SourceContains -Path (Join-Path $repo 'tools\scripts\dump-governance.ps1') -Patterns @(
    'Dump handoff length mismatch',
    'Dump handoff SHA-256 mismatch',
    'Get-DtmApiFileSha256',
    'Verified = $true'
)

if ($RunCleanupFixture) {
    $fixtureRoot = Join-Path $repo ('tmp\test-artifact-governance-fixture-' + [Guid]::NewGuid().ToString('N'))
    $sessionRoot = Join-Path $fixtureRoot 'DTMAPI.UnitTests\completed-fixture'
    $activeSessionRoot = Join-Path $fixtureRoot 'DTMAPI.UnitTests\active-fixture'
    $isolatedSystemTemp = Join-Path $fixtureRoot 'system-temp'
    $dumpSessionRoot = Join-Path $isolatedSystemTemp 'DTMAPI-Dumps\completed-dump-fixture'
    $manifestPath = Join-Path $fixtureRoot 'cleanup.json'
    $secondManifestPath = Join-Path $fixtureRoot 'cleanup-second.json'
    $smokeEvidenceRoot = Join-Path $fixtureRoot 'smoke-evidence'
    $noCaptureSystemTemp = Join-Path $fixtureRoot 'no-capture-system-temp'
    $autoFishingManagedRoot = Join-Path $fixtureRoot 'autofishing-managed-root'
    $activeLease = $null
    try {
        New-Item -ItemType Directory -Force -Path $sessionRoot, $activeSessionRoot, $dumpSessionRoot | Out-Null
        'fixture' | Set-Content -LiteralPath (Join-Path $sessionRoot 'artifact.bin')
        '' | Set-Content -LiteralPath (Join-Path $sessionRoot 'active.lock')
        [ordered]@{
            owner = 'DTMAPI.TestSession'
            schemaVersion = 1
            status = 'completed-success'
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $sessionRoot 'session.json')

        'active-fixture' | Set-Content -LiteralPath (Join-Path $activeSessionRoot 'artifact.bin')
        '' | Set-Content -LiteralPath (Join-Path $activeSessionRoot 'active.lock')
        [ordered]@{
            owner = 'DTMAPI.TestSession'
            schemaVersion = 1
            status = 'completed-success'
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $activeSessionRoot 'session.json')
        $activeLease = [System.IO.File]::Open((Join-Path $activeSessionRoot 'active.lock'), [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)

        'dump-fixture' | Set-Content -LiteralPath (Join-Path $dumpSessionRoot 'artifact.bin')
        '' | Set-Content -LiteralPath (Join-Path $dumpSessionRoot 'active.lock')
        [ordered]@{
            owner = 'DTMAPI.DumpCapture'
            schemaVersion = 1
            status = 'completed-verified'
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dumpSessionRoot 'dump-session.json')

        & "$PSScriptRoot\cleanup-test-artifacts.ps1" -TestRoot $fixtureRoot -SystemTempRoot $isolatedSystemTemp -Apply -ManifestPath $manifestPath
        if (Test-Path -LiteralPath $sessionRoot) {
            throw 'Synthetic completed test session was not cleaned.'
        }
        if (Test-Path -LiteralPath $dumpSessionRoot) {
            throw 'Synthetic completed dump session was not cleaned.'
        }
        if (-not (Test-Path -LiteralPath $activeSessionRoot)) {
            throw 'The cleanup tool deleted a session whose exclusive lease was active.'
        }
        $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
        if ($manifest.candidateCount -ne 2 -or @($manifest.candidates | Where-Object { $_.Result -eq 'deleted' }).Count -ne 2) {
            throw 'Synthetic cleanup manifest did not record the completed test and dump sessions.'
        }

        $activeLease.Dispose()
        $activeLease = $null
        & "$PSScriptRoot\cleanup-test-artifacts.ps1" -TestRoot $fixtureRoot -SystemTempRoot $isolatedSystemTemp -Apply -ManifestPath $secondManifestPath
        if (Test-Path -LiteralPath $activeSessionRoot) {
            throw 'Synthetic session was not cleaned after its exclusive lease was released.'
        }

        New-Item -ItemType Directory -Force -Path $autoFishingManagedRoot | Out-Null
        $previousManagedTestRoot = $env:DTMAPI_TEST_TEMP_ROOT
        try {
            $env:DTMAPI_TEST_TEMP_ROOT = $autoFishingManagedRoot
            & "$PSScriptRoot\test-batch6-autofishing-behavior-matrix.ps1"
            if (-not $?) {
                throw 'AutoFishing behavior success-leak fixture failed.'
            }
            & "$PSScriptRoot\test-batch6-autofishing-manager-lifecycle.ps1"
            if (-not $?) {
                throw 'AutoFishing Manager success-leak fixture failed.'
            }
            if (@(Get-ChildItem -LiteralPath $autoFishingManagedRoot -Force).Count -ne 0) {
                throw 'Successful AutoFishing release tests leaked a GUID child tree inside the dedicated managed root.'
            }
        }
        finally {
            $env:DTMAPI_TEST_TEMP_ROOT = $previousManagedTestRoot
        }

        $tokens = $null
        $parseErrors = $null
        $smokeAst = [System.Management.Automation.Language.Parser]::ParseFile(
            (Join-Path $repo 'tools\scripts\run-game-smoke.ps1'),
            [ref]$tokens,
            [ref]$parseErrors)
        if ($parseErrors.Count -gt 0) {
            throw 'Could not parse run-game-smoke.ps1 for the no-capture dump lifecycle fixture.'
        }
        foreach ($functionName in @('Invoke-DtmApiDumpTempScavenge', 'Invoke-SmokeFatalProcessDump')) {
            $definition = $smokeAst.Find({
                param($node)
                $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $functionName
            }, $true)
            if (-not $definition) {
                throw "Could not find $functionName in run-game-smoke.ps1."
            }
            . ([ScriptBlock]::Create($definition.Extent.Text))
        }

        $previousTemp = $env:TEMP
        $previousTmp = $env:TMP
        try {
            New-Item -ItemType Directory -Force -Path $noCaptureSystemTemp, $smokeEvidenceRoot | Out-Null
            $env:TEMP = $noCaptureSystemTemp
            $env:TMP = $noCaptureSystemTemp
            $dumpStatus = Invoke-SmokeFatalProcessDump -EvidencePath $smokeEvidenceRoot -Mode 'None' -Process $null
            if ($dumpStatus -ne 'Skipped') {
                throw 'The normal no-capture dump path did not remain skipped.'
            }
            if (Test-Path -LiteralPath (Join-Path $noCaptureSystemTemp 'DTMAPI-Dumps')) {
                throw 'The normal no-capture dump path created a managed temp root.'
            }
            $dumpSummary = Get-Content -Raw -LiteralPath (Join-Path $smokeEvidenceRoot 'Process-Dumps\process-dump-summary.txt')
            if (-not $dumpSummary.Contains('Reason=FatalWindowProcessDumpMode=None')) {
                throw 'The normal no-capture dump path did not write its explicit skip receipt.'
            }

            . "$PSScriptRoot\dump-governance.ps1"
            $handoffSource = Join-Path $fixtureRoot 'handoff-source.bin'
            $handoffDestination = Join-Path $fixtureRoot 'handoff-destination.bin'
            [System.IO.File]::WriteAllBytes($handoffSource, [byte[]](0..255))
            $handoff = Copy-DtmApiVerifiedFile -SourcePath $handoffSource -DestinationPath $handoffDestination
            if (-not $handoff.Verified -or $handoff.Length -ne 256 -or
                $handoff.Sha256 -ne (Get-DtmApiFileSha256 -Path $handoffSource) -or
                -not (Test-Path -LiteralPath $handoffSource -PathType Leaf)) {
                throw 'Synthetic dump handoff did not verify length and SHA-256 while preserving source ownership for the caller finally block.'
            }
            $emptySource = Join-Path $fixtureRoot 'empty-source.bin'
            [System.IO.File]::WriteAllBytes($emptySource, [byte[]]@())
            if ($null -ne (Copy-DtmApiVerifiedFile -SourcePath $emptySource -DestinationPath (Join-Path $fixtureRoot 'empty-destination.bin'))) {
                throw 'Synthetic dump handoff accepted an empty source.'
            }
        }
        finally {
            $env:TEMP = $previousTemp
            $env:TMP = $previousTmp
        }
    }
    finally {
        if ($activeLease) {
            $activeLease.Dispose()
        }
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host 'Test artifact governance: OK'
