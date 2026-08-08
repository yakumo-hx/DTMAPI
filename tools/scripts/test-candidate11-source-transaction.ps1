param(
    [switch] $Quiet
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
. "$PSScriptRoot\candidate11-source-transaction.ps1" -LibraryOnly
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-Candidate11Test {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw "Candidate11 source transaction test failed: $Message"
    }
}

function Write-Candidate11TestText {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [AllowEmptyString()] [string] $Text
    )

    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

function Write-Candidate11TestJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    Write-Candidate11TestText -Path $Path -Text (($Value | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
}

function New-Candidate11TestFixture {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $candidateRoot = Join-Path $Root 'C'
    $persistentRoot = Join-Path $Root 'P'
    $modsRoot = Join-Path $persistentRoot 'MODS'
    $gameDir = Join-Path $Root 'G'
    [System.IO.Directory]::CreateDirectory($candidateRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($modsRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory((Join-Path $gameDir 'DolocTown_Data')) | Out-Null
    Write-Candidate11TestText -Path (Join-Path $gameDir 'DolocTown.exe') -Text 'fixture-game'

    $runtimePackageRoot = Join-Path $candidateRoot 'DTMAPI'
    $runtimePayloadRoot = Join-Path $runtimePackageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $runtimeAssemblyReceipts = New-Object 'System.Collections.Generic.List[object]'
    foreach ($runtimeAssemblyName in $script:Candidate11RuntimeAssemblyNames) {
        $runtimeAssemblyPath = Join-Path $runtimePayloadRoot $runtimeAssemblyName
        Write-Candidate11TestText -Path $runtimeAssemblyPath -Text ('runtime-candidate-' + $runtimeAssemblyName)
        $runtimeItem = Get-Item -LiteralPath $runtimeAssemblyPath -Force
        $runtimeAssemblyReceipts.Add([ordered]@{
            FileName = $runtimeAssemblyName
            Length = [int64]$runtimeItem.Length
            Sha256 = ([string](Get-FileHash -LiteralPath $runtimeAssemblyPath -Algorithm SHA256).Hash).ToLowerInvariant()
            FileVersion = $script:DtmApiBinaryVersion
        }) | Out-Null
    }
    $runtimeBuildCommit = '0123456789abcdef0123456789abcdef01234567'
    $runtimeManifest = [ordered]@{
        SchemaVersion = 1
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        BuildCommit = $runtimeBuildCommit
        BuildTime = '2026-07-19T00:00:00.0000000Z'
        PackageKind = 'workshop-runtime'
        IncludedAssemblies = @($runtimeAssemblyReceipts.ToArray())
        BundledMods = @()
    }
    Write-Candidate11TestJson -Path (Join-Path $runtimePackageRoot 'Content\DTMAPI\release-manifest.json') -Value $runtimeManifest

    $installedRuntimeRoot = Join-Path $gameDir 'BepInEx\plugins\DTMAPI'
    foreach ($runtimeAssemblyName in $script:Candidate11RuntimeAssemblyNames) {
        $installedRuntimePath = Join-Path $installedRuntimeRoot $runtimeAssemblyName
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $installedRuntimePath)) | Out-Null
        [System.IO.File]::Copy((Join-Path $runtimePayloadRoot $runtimeAssemblyName), $installedRuntimePath, $false)
    }
    $installedManifest = [ordered]@{
        SchemaVersion = 1
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        BuildCommit = $runtimeBuildCommit
        BuildTime = '2026-07-19T00:00:01.0000000Z'
        PackageKind = 'local-install'
        IncludedAssemblies = @($runtimeAssemblyReceipts.ToArray())
        BundledMods = @()
    }
    Write-Candidate11TestJson -Path (Join-Path $gameDir 'DTMAPI\release-manifest.json') -Value $installedManifest

    $trackedCatalog = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
    $trackedCurrentProducts = @(Get-DtmApiReleaseContractAdvancedProducts -Catalog $trackedCatalog)
    $trackedManbo = @($trackedCatalog.products | Where-Object { [string]$_.catalogId -ceq 'manbo-cardboard-audio' })
    $trackedMoreEquipment = @($trackedCatalog.products | Where-Object { [string]$_.catalogId -ceq 'more-equipment-slots' })
    if ($trackedCurrentProducts.Count -ne 9 -or $trackedManbo.Count -ne 1 -or $trackedMoreEquipment.Count -ne 1) {
        throw 'Candidate11 fixture requires the tracked nine-source/two-retained authority set.'
    }
    $policyRegistry = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json') | ConvertFrom-Json

    $products = New-Object 'System.Collections.Generic.List[object]'
    $mutationEntrypoints = New-Object 'System.Collections.Generic.List[object]'
    $originalSnapshots = [ordered]@{}
    for ($index = 1; $index -le 11; $index++) {
        $suffix = $index.ToString('00')
        $isManbo = $index -eq 10
        $isMoreEquipment = $index -eq 11
        $isCurrentSourceCandidate = -not $isManbo -and -not $isMoreEquipment
        $trackedProduct = if ($isManbo) {
            $trackedManbo[0]
        }
        elseif ($isMoreEquipment) {
            $trackedMoreEquipment[0]
        }
        else {
            $trackedCurrentProducts[$index - 1]
        }
        $catalogId = [string]$trackedProduct.catalogId
        $uniqueId = [string]$trackedProduct.uniqueId
        $officialFolder = 'Fixture_Official_' + $suffix
        $packageName = 'Fixture-Package-' + $suffix
        $packageDll = 'Fixture.Candidate11.Product' + $suffix + '.dll'
        $sourceVersion = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'sourceVersion' -Default '')
        $sourceMinimum = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'sourceMinimumDtmApiVersion' -Default '')
        $publishedVersion = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'publishedVersion' -Default '')
        $publishedMinimum = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'publishedMinimumDtmApiVersion' -Default '')
        $candidateVersion = if ($isCurrentSourceCandidate) { $sourceVersion } else { $publishedVersion }
        $candidateMinimum = if ($isCurrentSourceCandidate) { $sourceMinimum } else { $publishedMinimum }
        $candidateInfoVersion = if ($isCurrentSourceCandidate) { $sourceVersion } else { '1.0.0' }
        $product = [ordered]@{
            catalogId = $catalogId
            role = 'PublishedProduct'
            productType = 'FunctionalCodeMod'
            distributionState = 'PublicWorkshop'
            releaseEligibility = 'RebuildBlocked'
            uniqueId = $uniqueId
            workshopId = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'workshopId' -Default '')
            officialFolder = $officialFolder
            packageName = $packageName
            packageDll = $packageDll
            sourceVersion = $sourceVersion
            sourceMinimumDtmApiVersion = $sourceMinimum
            publishedVersion = $publishedVersion
            publishedMinimumDtmApiVersion = $publishedMinimum
            codeModKind = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'codeModKind' -Default '')
            nativeOwnership = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'nativeOwnership' -Default '')
        }
        if ($isCurrentSourceCandidate) {
            $product['referencePolicyId'] = [string]$trackedProduct.referencePolicyId
            $product['canonicalHarmonyOwner'] = [string]$trackedProduct.canonicalHarmonyOwner
        }

        $packageRoot = Join-Path $candidateRoot $packageName
        $contentRoot = Join-Path $packageRoot 'Content\DTMAPI'
        $manifestPath = Join-Path $contentRoot 'manifest.json'
        Write-Candidate11TestJson -Path $manifestPath -Value ([ordered]@{
            Name = 'Fixture ' + $suffix
            Author = 'DTMAPI Test'
            Version = $candidateVersion
            MinimumDTMApiVersion = $candidateMinimum
            UniqueID = $uniqueId
            Type = 'CodeMod'
            CodeModKind = if ($isCurrentSourceCandidate) { 'Advanced' } else { [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'codeModKind' -Default '') }
            EntryDll = 'Content/DTMAPI/' + $packageDll
            EntryType = 'Fixture.Candidate11.EntryPoint'
        })
        Write-Candidate11TestJson -Path (Join-Path $packageRoot 'info.json') -Value ([ordered]@{
            name = 'Fixture ' + $suffix
            author = 'DTMAPI Test'
            version = $candidateInfoVersion
        })
        $entryDllPath = Join-Path $contentRoot $packageDll
        Write-Candidate11TestText -Path $entryDllPath -Text ('candidate-dll-' + $suffix)
        $packageMarker = if ($isCurrentSourceCandidate) {
            $policyId = [string]$trackedProduct.referencePolicyId
            $policyPath = Join-Path $repo ('author-sdk\advanced-reference-policies\' + $policyId + '.json')
            $policyBytes = [System.IO.File]::ReadAllBytes($policyPath)
            $policy = (New-Object System.Text.UTF8Encoding($false)).GetString($policyBytes) | ConvertFrom-Json
            $policyRegistryRows = @($policyRegistry.policies | Where-Object { [string]$_.policyId -ceq $policyId })
            if ($policyRegistryRows.Count -ne 1) {
                throw "Candidate11 fixture cannot resolve the tracked policy registry row for $policyId."
            }
            $manifestSha256 = ([string](Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash).ToLowerInvariant()
            $entryItem = Get-Item -LiteralPath $entryDllPath -Force
            $entrySha256 = ([string](Get-FileHash -LiteralPath $entryDllPath -Algorithm SHA256).Hash).ToLowerInvariant()
            $receiptPath = Join-Path $contentRoot 'dtmapi-advanced-references.json'
            Write-Candidate11TestJson -Path $receiptPath -Value ([ordered]@{
                schemaVersion = 1
                receiptKind = 'DTMAPI.AdvancedCodeMod.ReferenceReceipt'
                referencePolicyId = $policyId
                referencePolicyVersion = [int]$policy.policyVersion
                referencePolicySha256 = ([string](Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash).ToLowerInvariant()
                uniqueId = $uniqueId
                codeModKind = 'Advanced'
                targetFramework = 'netstandard2.0'
                gameBuildId = [string]$policy.gameBuildId
                gameAssemblyRelativePath = [string]$policy.gameAssemblyRelativePath
                gameAssemblySha256 = ([string]$policy.gameAssemblySha256).ToLowerInvariant()
                manifestPath = 'Content/DTMAPI/manifest.json'
                manifestSha256 = $manifestSha256
                entryDllPath = 'Content/DTMAPI/' + $packageDll
                entryDllLength = [int64]$entryItem.Length
                entryDllSha256 = $entrySha256
                harmonyOwner = [string]$trackedProduct.canonicalHarmonyOwner
                references = @($policy.references | ForEach-Object {
                    [ordered]@{
                        gameRelativePath = [string]$_.gameRelativePath
                        assemblyName = [string]$_.assemblyName
                        length = [int64]$_.length
                        sha256 = ([string]$_.sha256).ToLowerInvariant()
                        copyLocal = $false
                    }
                })
            })
            $receiptSha256 = ([string](Get-FileHash -LiteralPath $receiptPath -Algorithm SHA256).Hash).ToLowerInvariant()
            [ordered]@{
                schemaVersion = 2
                owner = 'DTMAPI'
                uniqueId = $uniqueId
                version = $candidateVersion
                packageKind = 'CodeMod'
                codeModKind = 'Advanced'
                authorSdkVersion = '0.1.0'
                targetDtmApiVersion = '0.5.5'
                manifestPath = 'Content/DTMAPI/manifest.json'
                manifestSha256 = $manifestSha256
                entryDllPath = 'Content/DTMAPI/' + $packageDll
                entryDllSha256 = $entrySha256
                advancedReferenceReceiptPath = 'Content/DTMAPI/dtmapi-advanced-references.json'
                advancedReferenceReceiptSha256 = $receiptSha256
                authority = 'dtmapi-author-sdk-package-binding'
            }
        }
        else {
            [ordered]@{
                owner = 'DTMAPI'
                uniqueId = $uniqueId
                generatedBy = 'retained-fixture'
            }
        }
        Write-Candidate11TestJson -Path (Join-Path $contentRoot 'dtmapi-package.json') -Value $packageMarker
        Write-Candidate11TestText -Path (Join-Path $packageRoot 'candidate-byte-sentinel.txt') -Text ('candidate-' + $suffix)
        if (-not $isCurrentSourceCandidate) {
            $retainedSnapshot = Get-Candidate11TreeSnapshot -Path $packageRoot -Context "Candidate11 retained fixture $catalogId"
            $product['retainedArtifact'] = [ordered]@{
                fileCount = [int]$retainedSnapshot.FileCount
                bytes = [int64]$retainedSnapshot.TotalBytes
                treeSha256 = Get-Candidate11RetainedTreeSha256 -Snapshot $retainedSnapshot
            }
        }
        $products.Add($product) | Out-Null
        if ($isCurrentSourceCandidate -or $isManbo) {
            $mutationEntrypoints.Add([ordered]@{
                action = 'ExistingWorkshopUpdate'
                catalogId = $catalogId
                workshopId = [string](Get-DtmApiMapValue -Map $trackedProduct -Key 'workshopId' -Default '')
            }) | Out-Null
        }

        $officialRoot = Join-Path $modsRoot $officialFolder
        Write-Candidate11TestText -Path (Join-Path $officialRoot 'original-byte-sentinel.txt') -Text ('original-' + $suffix)
        Write-Candidate11TestText -Path (Join-Path $officialRoot 'nested\state.bin') -Text ('original-nested-' + $suffix)
        if ($index -eq 1) {
            Set-Content -LiteralPath (Join-Path $officialRoot 'original-byte-sentinel.txt') -Stream 'original-preserved' -Value 'original-stream-byte' -Encoding UTF8
        }
        $originalSnapshots[$catalogId] = Get-Candidate11TreeSnapshot -Path $officialRoot -Context "Candidate11 test original $catalogId" -AllowAlternateDataStreams
    }

    $catalogPath = Join-Path $Root 'catalog.json'
    Write-Candidate11TestJson -Path $catalogPath -Value ([ordered]@{
        schemaVersion = 1
        catalogId = 'candidate11-fixture'
        products = @($products.ToArray())
        releaseStop = [ordered]@{
            publicMutationEntrypoints = @($mutationEntrypoints.ToArray())
            explicitlyExcludedExistingWorkshopUpdates = @([ordered]@{
                catalogId = 'more-equipment-slots'
                workshopId = [string]$trackedMoreEquipment[0].workshopId
                retainedVersion = '0.3.1-dtmapi'
            })
        }
    })

    $smokePath = Join-Path $Root 'fake-candidate11-smoke.ps1'
    $smokeSource = @'
param(
    [Parameter(Mandatory = $true)] [string] $PersistentRoot,
    [Parameter(Mandatory = $true)] [string] $CatalogPath,
    [Parameter(Mandatory = $true)] [string] $GameDir,
    [switch] $Fail,
    [switch] $Drift,
    [switch] $RuntimeDrift,
    [ValidateSet('Valid','Omit','Stale','Tamper')]
    [string] $IssueReceiptMode = 'Valid'
)
$ErrorActionPreference = 'Stop'
function Write-FakeText {
    param([string] $Path, [string] $Text)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}
function Write-FakeJson {
    param([string] $Path, $Value)
    Write-FakeText -Path $Path -Text (($Value | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
}
function Get-FakeReceipt {
    param([string] $Path)
    $item = Get-Item -LiteralPath $Path -Force
    return [ordered]@{
        Path = [System.IO.Path]::GetFullPath($Path)
        Length = [int64]$item.Length
        Sha256 = ([string](Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash).ToLowerInvariant()
    }
}
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $CatalogPath | ConvertFrom-Json
foreach ($product in @($catalog.products)) {
    $root = Join-Path (Join-Path $PersistentRoot 'MODS') ([string]$product.officialFolder)
    $candidate = Join-Path $root 'candidate-byte-sentinel.txt'
    $original = Join-Path $root 'original-byte-sentinel.txt'
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf) -or (Test-Path -LiteralPath $original)) {
        throw "Fake smoke did not observe the candidate tree for $($product.catalogId)."
    }
}
if ($Drift) {
    $first = @($catalog.products | Sort-Object catalogId | Select-Object -First 1)[0]
    [System.IO.File]::WriteAllText(
        (Join-Path (Join-Path (Join-Path $PersistentRoot 'MODS') ([string]$first.officialFolder)) 'foreign-after-smoke.txt'),
        'preserve-this-drift',
        (New-Object System.Text.UTF8Encoding($false)))
}
if ($RuntimeDrift) {
    [System.IO.File]::AppendAllText(
        (Join-Path $GameDir 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'),
        '-runtime-drift',
        (New-Object System.Text.UTF8Encoding($false)))
}
Write-Output 'FAKE-CANDIDATE11-SMOKE-OBSERVED=11'
if ($IssueReceiptMode -ne 'Omit') {
    $issueEvidence = Join-Path $PersistentRoot 'fake-issue011-evidence'
    [System.IO.Directory]::CreateDirectory($issueEvidence) | Out-Null
    $runStarted = if ($IssueReceiptMode -eq 'Stale') { [DateTimeOffset]::UtcNow.AddHours(-3) } else { [DateTimeOffset]::UtcNow }
    $now = [DateTimeOffset]::UtcNow
    $noQaPath = Join-Path $issueEvidence 'no-qa-ui-evidence-gate.json'
    Write-FakeJson -Path $noQaPath -Value ([ordered]@{ Passed = $true })
    $logPath = Join-Path $issueEvidence 'BepInEx-LogOutput.log'
    $titleClickLine = "$($now.ToString('o')) [Info] DTMAPI title settings button clicked."
    $titleOpenedLine = "$($now.ToString('o')) [Info] DTMAPI title settings menu opened."
    $searchLine = "$($now.ToString('o')) [Info] Hook status: UI.DebugConsoleItemTooltip = visible. item=fixture_item, source=Vanilla, searchText=fixture."
    Write-FakeText -Path $logPath -Text (($titleClickLine, $titleOpenedLine, $searchLine) -join [Environment]::NewLine)
    $lastGivePath = Join-Path $issueEvidence 'debug-console-last-give.txt'
    Write-FakeText -Path $lastGivePath -Text ($now.ToString('o') + ' status=verified source=YConsole.ButtonLeftClick item=fixture_item requested=1 given=1 rightClick=False failure=reason=none.' + [Environment]::NewLine)
    $crashSummaryPath = Join-Path $issueEvidence 'Unity-Crashes\summary.txt'
    Write-FakeText -Path $crashSummaryPath -Text ("Collected=$($now.ToString('o'))`nNo Unity crash report directories were found for the current Windows user.`nFilesCopiedTotal=0`n")
    $steamSnapshotPath = Join-Path $issueEvidence 'issue-011-steam-lifecycle.txt'
    $addedLocal = [DateTime]::Now
    $removedLocal = $addedLocal.AddSeconds(1)
    $addedLine = '[' + $addedLocal.ToString('yyyy-MM-dd HH:mm:ss') + '] Game process added : AppID 2285550 "fixture", ProcID 43210, IP 0.0.0.0:0'
    $removedLine = '[' + $removedLocal.ToString('yyyy-MM-dd HH:mm:ss') + '] Game process removed: AppID 2285550 "fixture", ProcID 43210 '
    Write-FakeText -Path $steamSnapshotPath -Text ("Added=$addedLine`nRemoved=$removedLine`nWaitingForExitAfterRemovalCount=0`n")
    $resultPath = Join-Path $issueEvidence 'result.json'
    Write-FakeJson -Path $resultPath -Value ([ordered]@{
        RunStatus = 'Passed'
        SaveTestMode = 'NoNativeSave'
        OfficialModProfile = 'Local11'
        SaveLoaded = 'Passed'
        NoQaUiEvidence = 'Passed'
        Issue011Acceptance = 'Passed'
        NoFatalInstanceWindow = 'Passed'
        ProcessExited = 'Passed'
        ForcedClose = 'Passed'
    })
    $runtimeNames = @(
        'DTMAPI.Abstractions.dll',
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    ) | Sort-Object
    $runtimeRows = @($runtimeNames | ForEach-Object {
        $runtimePath = Join-Path $GameDir ('BepInEx\plugins\DTMAPI\' + $_)
        $runtimeReceipt = Get-FakeReceipt -Path $runtimePath
        [ordered]@{
            FileName = [string]$_
            Path = [string]$runtimeReceipt.Path
            Length = [int64]$runtimeReceipt.Length
            Sha256 = [string]$runtimeReceipt.Sha256
        }
    })
    $receiptPath = Join-Path $issueEvidence 'issue-011-acceptance.json'
    $receipt = [ordered]@{
        SchemaVersion = 1
        ReceiptKind = 'DTMAPI.ISSUE011.CurrentCandidateAcceptance'
        EvidenceRoot = [System.IO.Path]::GetFullPath($issueEvidence)
        ReceiptPath = [System.IO.Path]::GetFullPath($receiptPath)
        GameDir = [System.IO.Path]::GetFullPath($GameDir)
        RunStartedAt = $runStarted.ToString('o')
        CompletedAt = $now.ToString('o')
        SaveSlot = 3
        SaveTestMode = 'NoNativeSave'
        LaunchMode = 'Steam'
        OfficialModProfile = 'Local11'
        AssertNoQaUiEvidence = $true
        RuntimeAssemblies = $runtimeRows
        NoQaGate = [ordered]@{
            Path = [string](Get-FakeReceipt -Path $noQaPath).Path
            Length = [int64](Get-FakeReceipt -Path $noQaPath).Length
            Sha256 = [string](Get-FakeReceipt -Path $noQaPath).Sha256
            Passed = $true
        }
        BepInExLog = Get-FakeReceipt -Path $logPath
        TitleSettings = [ordered]@{
            ButtonClickCount = 1
            MenuOpenedCount = 1
            ButtonClickLine = $titleClickLine
            MenuOpenedLine = $titleOpenedLine
            Passed = $true
        }
        DebugConsole = [ordered]@{
            OpenUseClosePassed = $true
            InteractiveObserved = $true
            SearchEvidenceCount = 1
            SearchText = 'fixture'
            SearchLine = $searchLine
            LastGiveBaseline = [ordered]@{
                Path = [System.IO.Path]::GetFullPath((Join-Path $PersistentRoot 'DTMAPI\debug-console-last-give.txt'))
                Existed = $false
                Length = [int64]0
                Sha256 = ''
                LastWriteTimeUtc = ''
            }
            LastGiveFile = Get-FakeReceipt -Path $lastGivePath
            LastGiveTimestamp = $now.ToString('o')
            LastGiveStatus = 'verified'
            LastGiveItem = 'fixture_item'
            LastGiveRequested = 1
            LastGiveGiven = 1
            LastGiveRightClick = $false
            Passed = $true
        }
        InputSystem = [ordered]@{
            UninitializedCount = 0
            FallbackFailureCount = 0
            UninitializedLines = @()
            FallbackFailureLines = @()
            Passed = $true
        }
        CrashPackage = [ordered]@{
            Summary = Get-FakeReceipt -Path $crashSummaryPath
            CollectedAt = $now.ToString('o')
            FreshnessStatus = 'missing'
            FreshnessReason = 'No copied crash files or directories could be attributed to this run.'
            FreshDirectories = @()
            CrashDumpPaths = @()
            MissingReasonPath = ''
            MissingReason = 'No Unity crash report directories were found for the current Windows user.'
            PackageExplainsDumpState = $true
            NoFreshNativeCrash = $true
            Passed = $true
        }
        SteamLifecycle = [ordered]@{
            SourceConsoleLogPath = 'fixture-console-log.txt'
            LifecycleSnapshot = Get-FakeReceipt -Path $steamSnapshotPath
            ProcessId = 43210
            AddedAt = ([DateTimeOffset]$addedLocal).ToString('o')
            RemovedAt = ([DateTimeOffset]$removedLocal).ToString('o')
            AddedLine = $addedLine
            RemovedLine = $removedLine
            WaitingForExitAfterRemovalCount = 0
            Passed = $true
        }
        ProcessExit = [ordered]@{
            NoFatalInstanceWindow = $true
            ProcessExited = $true
            ForcedClose = $false
            Passed = $true
        }
        SmokeResult = Get-FakeReceipt -Path $resultPath
        Passed = $true
    }
    Write-FakeJson -Path $receiptPath -Value $receipt
    if ($IssueReceiptMode -eq 'Tamper') {
        [System.IO.File]::AppendAllText($logPath, "tampered-after-receipt`n", (New-Object System.Text.UTF8Encoding($false)))
    }
    Write-Output ('DTMAPI_ISSUE011_ACCEPTANCE_RECEIPT=' + [System.IO.Path]::GetFullPath($receiptPath))
    Write-Output ('DTMAPI_ISSUE011_ACCEPTANCE_RECEIPT_BASE64=' + [Convert]::ToBase64String((New-Object System.Text.UTF8Encoding($false)).GetBytes([System.IO.Path]::GetFullPath($receiptPath))))
}
if ($Fail) {
    exit 23
}
exit 0
'@
    Write-Candidate11TestText -Path $smokePath -Text $smokeSource

    return [pscustomobject]@{
        Root = $Root
        CandidateRoot = $candidateRoot
        PersistentRoot = $persistentRoot
        GameDir = $gameDir
        RuntimePackageRoot = $runtimePackageRoot
        RuntimePayloadRoot = $runtimePayloadRoot
        InstalledRuntimeRoot = $installedRuntimeRoot
        ModsRoot = $modsRoot
        CatalogPath = $catalogPath
        SmokePath = $smokePath
        Products = @($products.ToArray())
        OriginalSnapshots = $originalSnapshots
    }
}

function Assert-Candidate11OriginalsRestored {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    foreach ($product in @($Fixture.Products)) {
        $officialRoot = Join-Path $Fixture.ModsRoot ([string]$product.officialFolder)
        Assert-Candidate11Test -Condition (Test-Path -LiteralPath (Join-Path $officialRoot 'original-byte-sentinel.txt') -PathType Leaf) -Message "$Label did not restore $($product.catalogId)."
        Assert-Candidate11Test -Condition (-not (Test-Path -LiteralPath (Join-Path $officialRoot 'candidate-byte-sentinel.txt'))) -Message "$Label left candidate bytes active for $($product.catalogId)."
        $actual = Get-Candidate11TreeSnapshot -Path $officialRoot -Context "$Label restored $($product.catalogId)" -AllowAlternateDataStreams
        $catalogKey = [string]$product.catalogId
        $expected = $Fixture.OriginalSnapshots[$catalogKey]
        Assert-Candidate11Test -Condition (Test-Candidate11SnapshotsEqual -Expected $expected -Actual $actual) -Message "$Label did not restore exact original bytes for $($product.catalogId)."
    }
}

$repo = Get-RepoRoot
$managedParent = Join-Path $repo 'tmp\test-runs\c11'
$testRoot = Join-Path $managedParent ([Guid]::NewGuid().ToString('N'))
$resolvedManagedParent = Get-Candidate11CanonicalPath -Path $managedParent
$resolvedTestRoot = Get-Candidate11CanonicalPath -Path $testRoot
Assert-Candidate11Test -Condition (Test-Candidate11SameOrChildPath -Child $resolvedTestRoot -Parent $resolvedManagedParent) -Message 'Managed fixture root escaped tmp/test-runs.'
[System.IO.Directory]::CreateDirectory($resolvedTestRoot) | Out-Null

$oldTestMode = $env:DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE
try {
    $env:DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE = '1'
    $sourceText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'candidate11-source-transaction.ps1'), [System.Text.Encoding]::UTF8)
    Assert-Candidate11Test -Condition ($sourceText -notmatch '(?im)^\s*Remove-Item\b') -Message 'Production transaction helper contains a Remove-Item operation.'
    Assert-Candidate11Test -Condition ($sourceText -match '\[System\.IO\.Directory\]::Move') -Message 'Production helper does not use same-volume Directory.Move operations.'
    Assert-Candidate11Test -Condition ($sourceText -match '\[System\.IO\.FileMode\]::CreateNew') -Message 'Production receipts are not fail-closed CreateNew writes.'
    Test-DtmApiWindowsPowerShellSyntax -Paths @(
        (Join-Path $PSScriptRoot 'candidate11-source-transaction.ps1'),
        $PSCommandPath
    ) -AllowCoreFallback

    $retainedOrderingSnapshot = [pscustomobject]@{
        Files = @(
            [pscustomobject]@{ Path = 'Content/DTMAPI/assets/data.bin'; Sha256 = ('11' * 32) },
            [pscustomobject]@{ Path = 'Content/DTMAPI/Zeta.dll'; Sha256 = ('22' * 32) },
            [pscustomobject]@{ Path = 'icon.png'; Sha256 = ('33' * 32) }
        )
    }
    Assert-Candidate11Test `
        -Condition ((Get-Candidate11RetainedTreeSha256 -Snapshot $retainedOrderingSnapshot) -ceq '96867fc68cad382933c3fa644bb887dba48ffaf13c71b6b6155d7666c0317f8e') `
        -Message 'Retained tree digest no longer uses the frozen cross-host OrdinalIgnoreCase order.'
    $runtimeOrderingAssemblies = @(
        [ordered]@{ FileName = 'DTMAPI.GameBridge.dll'; Length = 3; Sha256 = ('11' * 32) },
        [ordered]@{ FileName = 'DTMAPI.Abstractions.dll'; Length = 1; Sha256 = ('22' * 32) },
        [ordered]@{ FileName = 'DTMAPI.ModConfigMenu.dll'; Length = 2; Sha256 = ('33' * 32) }
    )
    Assert-Candidate11Test `
        -Condition ((Get-Candidate11RuntimeAssemblyDigest -Assemblies $runtimeOrderingAssemblies) -ceq 'f6443f95c11cd2386413e9e44704cb7998339174a2a95bf2b8324b10af63b11a') `
        -Message 'Runtime assembly digest no longer uses the frozen cross-host Ordinal order.'
    $smokeSource = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'run-game-smoke.ps1')
    Assert-Candidate11Test `
        -Condition ($smokeSource.Contains("New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::OrdinalIgnoreCase)")) `
        -Message 'Game smoke retained artifact preflight no longer shares the frozen cross-host OrdinalIgnoreCase order.'

    $trackedCatalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
    $trackedCatalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $trackedCatalogPath | ConvertFrom-Json
    $trackedCandidate11 = @($trackedCatalog.products | Where-Object {
        [string]$_.role -ceq 'PublishedProduct' -and [string]$_.distributionState -ceq 'PublicWorkshop'
    })
    Assert-Candidate11Test -Condition ($trackedCandidate11.Count -eq 11) -Message "Tracked Catalog does not currently select exactly 11 Candidate11 products; found $($trackedCandidate11.Count)."
    Assert-Candidate11Test -Condition (@($trackedCandidate11 | Where-Object {
        [string]::IsNullOrWhiteSpace([string]$_.catalogId) -or
        [string]::IsNullOrWhiteSpace([string]$_.uniqueId) -or
        [string]::IsNullOrWhiteSpace([string]$_.officialFolder) -or
        [string]::IsNullOrWhiteSpace([string]$_.packageName) -or
        [string]::IsNullOrWhiteSpace([string]$_.packageDll)
    }).Count -eq 0) -Message 'Tracked Candidate11 Catalog rows contain incomplete identity/path fields.'
    $trackedSourceCandidates = @(Get-DtmApiReleaseContractAdvancedProducts -Catalog $trackedCatalog)
    $trackedSourceCandidateIds = @($trackedSourceCandidates | ForEach-Object { [string]$_.catalogId } | Sort-Object)
    $trackedRetainedIds = @($trackedCandidate11 | Where-Object {
        $trackedSourceCandidateIds -cnotcontains [string]$_.catalogId
    } | ForEach-Object { [string]$_.catalogId } | Sort-Object)
    Assert-Candidate11Test -Condition (
        $trackedSourceCandidateIds.Count -eq 9 -and
        ($trackedRetainedIds -join '|') -ceq 'manbo-cardboard-audio|more-equipment-slots'
    ) -Message 'Tracked Catalog did not resolve to the exact nine-source/two-retained Candidate11 boundary.'

    $validSmokeContract = @('-UseSteam','-SkipInstall','-SaveSlot','3','-SaveTestMode','NoNativeSave','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance')
    Assert-Candidate11SmokeContract -Arguments $validSmokeContract
    foreach ($invalidContract in @(
        [ordered]@{ Label = 'missing ISSUE-011 receipt gate'; Arguments = @('-UseSteam','-SkipInstall','-SaveSlot','3','-SaveTestMode','NoNativeSave','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence'); Pattern = 'exactly one -Issue011Acceptance' },
        [ordered]@{ Label = 'missing slot'; Arguments = @('-UseSteam','-SkipInstall','-SaveTestMode','NoNativeSave','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance'); Pattern = 'exactly one -SaveSlot' },
        [ordered]@{ Label = 'wrong slot'; Arguments = @('-UseSteam','-SkipInstall','-SaveSlot','5','-SaveTestMode','NoNativeSave','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance'); Pattern = '-SaveSlot 3' },
        [ordered]@{ Label = 'duplicate mode'; Arguments = @('-UseSteam','-SkipInstall','-SaveSlot','3','-SaveTestMode','NoNativeSave','-SaveTestMode','ArchiveMutation','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance'); Pattern = 'exactly one -SaveTestMode' },
        [ordered]@{ Label = 'alternate slot form'; Arguments = @('-UseSteam','-SkipInstall','-SaveSlot:3','-SaveTestMode','NoNativeSave','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance'); Pattern = 'alternate or duplicate' }
    )) {
        $contractFailure = ''
        try {
            Assert-Candidate11SmokeContract -Arguments @($invalidContract.Arguments)
        }
        catch {
            $contractFailure = [string]$_.Exception.Message
        }
        Assert-Candidate11Test -Condition ($contractFailure -match [string]$invalidContract.Pattern) -Message "Candidate11 $($invalidContract.Label) smoke contract did not fail before shared-state work. Actual=$contractFailure"
    }
    $badSmokeContract = ''
    try {
        Assert-Candidate11SmokeContract -Arguments @('-UseSteam','-SkipInstall','-SaveSlot','3','-SaveTestMode','NoNativeSave','-OfficialModProfile','Published11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance')
    }
    catch {
        $badSmokeContract = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($badSmokeContract -match 'Local11') -Message 'Published11 smoke arguments did not fail the Candidate11 contract.'
    $qaSmokeContract = ''
    try {
        Assert-Candidate11SmokeContract -Arguments @('-UseSteam','-SkipInstall','-SaveSlot','3','-SaveTestMode','NoNativeSave','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance','-StageQaHost')
    }
    catch {
        $qaSmokeContract = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($qaSmokeContract -match 'ordinary no-QA') -Message 'QA-host staging did not fail the ordinary Candidate11 contract.'

    $unicodeCaseLeaf = '01 ' + [char]0x4e2d + [char]0x6587
    $success = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot $unicodeCaseLeaf)
    $successEvidence = Join-Path $success.Root 'E'
    $successResult = Invoke-DtmApiCandidate11SourceTransaction `
        -CandidatePackageRoot $success.CandidateRoot `
        -ProductCatalogPath $success.CatalogPath `
        -DolocPersistentRoot $success.PersistentRoot `
        -GameDirectory $success.GameDir `
        -FormalEvidenceRoot $successEvidence `
        -Id 'success-case' `
        -SmokePath $success.SmokePath `
        -SmokeArguments @('-PersistentRoot',$success.PersistentRoot,'-CatalogPath',$success.CatalogPath,'-GameDir',$success.GameDir) `
        -SkipRuntimeLockForTest `
        -SkipProcessCheckForTest `
        -SkipSmokeContractForTest
    Assert-Candidate11Test -Condition ([bool]$successResult.Passed) -Message 'Happy-path transaction did not pass.'
    Assert-Candidate11Test -Condition ([int]$successResult.ProductCount -eq 11) -Message 'Happy-path transaction did not bind exactly 11 products.'
    Assert-Candidate11Test -Condition (
        [bool]$successResult.RuntimeBinding.Passed -and
        [int]$successResult.RuntimeBinding.CandidatePreflight.AssemblyCount -eq 5 -and
        [bool]$successResult.RuntimeBinding.InstalledPreSmoke.AllAssemblyBytesMatched -and
        [bool]$successResult.RuntimeBinding.InstalledPreSmoke.ReleaseManifestProjectionMatched -and
        [bool]$successResult.RuntimeBinding.InstalledPostSmoke.AllAssemblyBytesMatched -and
        [bool]$successResult.RuntimeBinding.InstalledPostSmoke.ReleaseManifestProjectionMatched -and
        [bool]$successResult.RuntimeBinding.CandidateSourceUnchanged
    ) -Message 'Happy-path transaction did not bind the candidate Runtime manifest/five DLLs to the installed Runtime.'
    Assert-Candidate11OriginalsRestored -Fixture $success -Label 'Happy path'
    $successWork = Join-Path (Join-Path $success.PersistentRoot '.dtmapi-candidate11-transactions') 'success-case'
    Assert-Candidate11Test -Condition (@(Get-ChildItem -LiteralPath (Join-Path $successWork 'tested-candidate') -Directory -ErrorAction Stop).Count -eq 11) -Message 'Happy path did not preserve all tested candidate trees outside MODS.'
    $successReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $successEvidence '99-result.json') | ConvertFrom-Json
    Assert-Candidate11Test -Condition (
        [int]$successReceipt.SchemaVersion -eq 2 -and
        [bool]$successReceipt.Passed -and
        [bool]$successReceipt.RestoreAllExact -and
        [bool]$successReceipt.RuntimeBinding.Passed -and
        [bool]$successReceipt.Issue011Acceptance.Passed -and
        [int]$successReceipt.RuntimeBinding.CandidatePreflight.AssemblyCount -eq 5 -and
        [string]$successReceipt.CatalogSha256Role -match 'not a self-referential final pass gate'
    ) -Message 'Happy-path formal result receipt is incomplete.'
    Assert-Candidate11Test -Condition (Test-Path -LiteralPath (Join-Path $successEvidence '05-installed-runtime-preflight.json') -PathType Leaf) -Message 'Happy path did not persist the pre-mutation installed Runtime receipt.'
    Assert-Candidate11Test -Condition (Test-Path -LiteralPath (Join-Path $successEvidence '41-installed-runtime-post-smoke.json') -PathType Leaf) -Message 'Happy path did not persist the post-smoke installed Runtime receipt.'
    Assert-Candidate11Test -Condition (Test-Path -LiteralPath (Join-Path $successEvidence '32-issue011-acceptance-binding.json') -PathType Leaf) -Message 'Happy path did not persist the current-run ISSUE-011 binding receipt.'
    $successPreflight = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $successEvidence '10-preflight-inventory.json') | ConvertFrom-Json
    $successSourceRows = @($successPreflight.Products | Where-Object { [string]$_.ArtifactBoundary -ceq 'CurrentSourceCandidate' })
    $successRetainedRows = @($successPreflight.Products | Where-Object { [string]$_.ArtifactBoundary -ceq 'RetainedPublished' } | Sort-Object CatalogId)
    $successManbo = @($successRetainedRows | Where-Object { [string]$_.CatalogId -ceq 'manbo-cardboard-audio' })[0]
    Assert-Candidate11Test -Condition (
        [int]$successPreflight.CurrentSourceCandidateCount -eq 9 -and
        [int]$successPreflight.RetainedPublishedCount -eq 2 -and
        $successSourceRows.Count -eq 9 -and
        ($successRetainedRows.CatalogId -join '|') -ceq 'manbo-cardboard-audio|more-equipment-slots' -and
        @($successSourceRows | Where-Object {
            [string]$_.PackageMarkerBoundary -cne 'AuthorSdkAdvanced' -or
            [string]$_.PackageMarkerKind -cne 'CodeMod' -or
            [string]$_.PackageMarkerCodeModKind -cne 'Advanced' -or
            -not [bool]$_.AdvancedBinding.Passed -or
            [string]$_.AdvancedBinding.AuthorSdkVersion -cne '0.1.0' -or
            [string]$_.AdvancedBinding.TargetDtmApiVersion -cne '0.5.5'
        }).Count -eq 0 -and
        @($successRetainedRows | Where-Object {
            [string]$_.PackageMarkerBoundary -cne 'ExactRetainedLegacy' -or
            -not [string]::IsNullOrWhiteSpace([string]$_.PackageMarkerKind)
        }).Count -eq 0 -and
        @($successRetainedRows | Where-Object { -not [bool]$_.RetainedAuthority.Passed }).Count -eq 0 -and
        [string]$successManbo.ExpectedManifestVersion -ceq '0.1.0-dtmapi' -and
        [string]$successManbo.ActualInfoVersion -ceq '1.0.0'
    ) -Message 'Happy-path preflight did not preserve the exact nine-source/two-retained artifact projection.'
    $successOutput = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $successEvidence '30-smoke-output.txt')
    Assert-Candidate11Test -Condition ($successOutput -match 'FAKE-CANDIDATE11-SMOKE-OBSERVED=11') -Message 'Happy-path child smoke output was not captured.'

    $failedSmoke = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '02')
    $failedEvidence = Join-Path $failedSmoke.Root 'E'
    $failedMessage = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $failedSmoke.CandidateRoot `
            -ProductCatalogPath $failedSmoke.CatalogPath `
            -DolocPersistentRoot $failedSmoke.PersistentRoot `
            -GameDirectory $failedSmoke.GameDir `
            -FormalEvidenceRoot $failedEvidence `
            -Id 'smoke-failure-case' `
            -SmokePath $failedSmoke.SmokePath `
            -SmokeArguments @('-PersistentRoot',$failedSmoke.PersistentRoot,'-CatalogPath',$failedSmoke.CatalogPath,'-GameDir',$failedSmoke.GameDir,'-Fail') `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest `
            -SkipSmokeContractForTest
    }
    catch {
        $failedMessage = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($failedMessage -match 'failed') -Message 'Non-zero child smoke did not fail the transaction.'
    Assert-Candidate11OriginalsRestored -Fixture $failedSmoke -Label 'Smoke failure'
    $failedReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $failedEvidence '99-result.json') | ConvertFrom-Json
    $failedSmokeExitCode = if ($null -ne $failedReceipt.Smoke -and $null -ne $failedReceipt.Smoke.PSObject.Properties['ExitCode']) { [int]$failedReceipt.Smoke.ExitCode } else { -1 }
    Assert-Candidate11Test -Condition (-not [bool]$failedReceipt.Passed -and $failedSmokeExitCode -eq 23 -and [bool]$failedReceipt.RestoreAllExact) -Message ('Smoke-failure receipt did not preserve exit/restoration facts: ' + ($failedReceipt | ConvertTo-Json -Depth 8 -Compress))

    $drift = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '03')
    $driftEvidence = Join-Path $drift.Root 'E'
    $driftMessage = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $drift.CandidateRoot `
            -ProductCatalogPath $drift.CatalogPath `
            -DolocPersistentRoot $drift.PersistentRoot `
            -GameDirectory $drift.GameDir `
            -FormalEvidenceRoot $driftEvidence `
            -Id 'post-smoke-drift-case' `
            -SmokePath $drift.SmokePath `
            -SmokeArguments @('-PersistentRoot',$drift.PersistentRoot,'-CatalogPath',$drift.CatalogPath,'-GameDir',$drift.GameDir,'-Drift') `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest `
            -SkipSmokeContractForTest
    }
    catch {
        $driftMessage = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($driftMessage -match 'drifted') -Message ("Post-smoke byte drift did not fail the transaction. Actual='$driftMessage'.")
    Assert-Candidate11OriginalsRestored -Fixture $drift -Label 'Post-smoke drift'
    $driftWork = Join-Path (Join-Path $drift.PersistentRoot '.dtmapi-candidate11-transactions') 'post-smoke-drift-case'
    $driftPreserved = @(Get-ChildItem -LiteralPath (Join-Path $driftWork 'tested-candidate') -Recurse -Force -File -Filter 'foreign-after-smoke.txt' -ErrorAction Stop)
    Assert-Candidate11Test -Condition ($driftPreserved.Count -eq 1) -Message 'Post-smoke unknown bytes were not preserved exactly once outside MODS.'
    Assert-Candidate11Test -Condition ((Get-Content -Raw -LiteralPath $driftPreserved[0].FullName) -ceq 'preserve-this-drift') -Message 'Preserved post-smoke drift bytes changed.'
    $driftReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $driftEvidence '99-result.json') | ConvertFrom-Json
    Assert-Candidate11Test -Condition (-not [bool]$driftReceipt.PostSmokeAllMatched -and [bool]$driftReceipt.RestoreAllExact) -Message 'Post-smoke drift receipt did not separate byte failure from exact restoration.'

    $extra = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '04')
    [System.IO.Directory]::CreateDirectory((Join-Path $extra.CandidateRoot 'Unexpected-Package')) | Out-Null
    $extraError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $extra.CatalogPath -Root $extra.CandidateRoot
    }
    catch {
        $extraError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($extraError -match 'not exact') -Message 'Unexpected top-level candidate package did not fail closed.'
    Assert-Candidate11OriginalsRestored -Fixture $extra -Label 'Unexpected package preflight'

    $ads = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '05')
    $adsFile = Join-Path $ads.CandidateRoot 'Fixture-Package-01\candidate-byte-sentinel.txt'
    Set-Content -LiteralPath $adsFile -Stream 'candidate11-forbidden' -Value 'foreign-stream' -Encoding UTF8
    $adsError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $ads.CatalogPath -Root $ads.CandidateRoot
    }
    catch {
        $adsError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($adsError -match 'alternate data stream') -Message 'Candidate ADS did not fail closed.'
    Assert-Candidate11OriginalsRestored -Fixture $ads -Label 'ADS preflight'

    $runtimeManifestMismatch = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '06')
    $runtimeManifestMismatchPath = Join-Path $runtimeManifestMismatch.RuntimePackageRoot 'Content\DTMAPI\release-manifest.json'
    $runtimeManifestMismatchJson = Get-Content -Raw -Encoding UTF8 -LiteralPath $runtimeManifestMismatchPath | ConvertFrom-Json
    $runtimeManifestMismatchJson.IncludedAssemblies[0].Sha256 = (('0' * 64) -join '')
    Write-Candidate11TestJson -Path $runtimeManifestMismatchPath -Value $runtimeManifestMismatchJson
    $runtimeManifestMismatchError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $runtimeManifestMismatch.CatalogPath -Root $runtimeManifestMismatch.CandidateRoot
    }
    catch {
        $runtimeManifestMismatchError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($runtimeManifestMismatchError -match 'receipt does not match payload bytes') -Message 'Runtime release-manifest/DLL SHA mismatch did not fail preflight.'
    Assert-Candidate11OriginalsRestored -Fixture $runtimeManifestMismatch -Label 'Runtime manifest mismatch preflight'

    $runtimeDrift = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '07')
    $runtimeDriftEvidence = Join-Path $runtimeDrift.Root 'E'
    $runtimeDriftMessage = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $runtimeDrift.CandidateRoot `
            -ProductCatalogPath $runtimeDrift.CatalogPath `
            -DolocPersistentRoot $runtimeDrift.PersistentRoot `
            -GameDirectory $runtimeDrift.GameDir `
            -FormalEvidenceRoot $runtimeDriftEvidence `
            -Id 'installed-runtime-drift-case' `
            -SmokePath $runtimeDrift.SmokePath `
            -SmokeArguments @('-PersistentRoot',$runtimeDrift.PersistentRoot,'-CatalogPath',$runtimeDrift.CatalogPath,'-GameDir',$runtimeDrift.GameDir,'-RuntimeDrift') `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest `
            -SkipSmokeContractForTest
    }
    catch {
        $runtimeDriftMessage = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($runtimeDriftMessage -match 'installed Runtime cross-check failed') -Message 'Post-smoke installed Runtime byte drift did not fail the transaction.'
    Assert-Candidate11OriginalsRestored -Fixture $runtimeDrift -Label 'Installed Runtime drift'
    $runtimeDriftReceipt = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $runtimeDriftEvidence '99-result.json') | ConvertFrom-Json
    Assert-Candidate11Test -Condition (
        -not [bool]$runtimeDriftReceipt.Passed -and
        -not [bool]$runtimeDriftReceipt.RuntimeBinding.Passed -and
        -not [bool]$runtimeDriftReceipt.RuntimeBinding.InstalledPostSmoke.Passed -and
        [bool]$runtimeDriftReceipt.RuntimeBinding.CandidateSourceUnchanged -and
        [bool]$runtimeDriftReceipt.RestoreAllExact
    ) -Message 'Installed Runtime drift receipt did not preserve Runtime failure and exact product restoration facts.'

    $missingRuntime = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '08')
    [System.IO.Directory]::Move($missingRuntime.RuntimePackageRoot, (Join-Path $missingRuntime.Root 'runtime-held-outside-candidate'))
    $missingRuntimeError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $missingRuntime.CatalogPath -Root $missingRuntime.CandidateRoot
    }
    catch {
        $missingRuntimeError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($missingRuntimeError -match 'missing=DTMAPI') -Message "Candidate root without the Runtime package did not fail the exact top-level inventory gate. Actual=$missingRuntimeError"
    Assert-Candidate11OriginalsRestored -Fixture $missingRuntime -Label 'Missing Runtime preflight'

    $retainedVersionMismatch = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '09')
    $retainedVersionProduct = @($retainedVersionMismatch.Products | Where-Object { [string]$_.catalogId -ceq 'more-equipment-slots' })[0]
    $retainedVersionManifestPath = Join-Path (Join-Path $retainedVersionMismatch.CandidateRoot ([string]$retainedVersionProduct.packageName)) 'Content\DTMAPI\manifest.json'
    $retainedVersionManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $retainedVersionManifestPath | ConvertFrom-Json
    $retainedVersionManifest.Version = [string]$retainedVersionProduct.sourceVersion
    Write-Candidate11TestJson -Path $retainedVersionManifestPath -Value $retainedVersionManifest
    $retainedVersionError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $retainedVersionMismatch.CatalogPath -Root $retainedVersionMismatch.CandidateRoot
    }
    catch {
        $retainedVersionError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($retainedVersionError -match 'RetainedPublished identity/version projection') -Message 'Deferred MoreEquipment source version did not fail the retained Candidate11 boundary.'
    Assert-Candidate11OriginalsRestored -Fixture $retainedVersionMismatch -Label 'Retained version mismatch preflight'

    $retainedTreeDrift = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '10')
    $retainedTreeProduct = @($retainedTreeDrift.Products | Where-Object { [string]$_.catalogId -ceq 'manbo-cardboard-audio' })[0]
    Write-Candidate11TestText `
        -Path (Join-Path (Join-Path $retainedTreeDrift.CandidateRoot ([string]$retainedTreeProduct.packageName)) 'retained-tree-drift.txt') `
        -Text 'retained-drift'
    $retainedTreeError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $retainedTreeDrift.CatalogPath -Root $retainedTreeDrift.CandidateRoot
    }
    catch {
        $retainedTreeError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($retainedTreeError -match 'does not match its exact Catalog tree') -Message 'Retained package tree drift did not fail before Candidate11 staging.'
    Assert-Candidate11OriginalsRestored -Fixture $retainedTreeDrift -Label 'Retained tree drift preflight'

    $sourceMarkerMismatch = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '11')
    $sourceMarkerProduct = @($sourceMarkerMismatch.Products | Where-Object {
        [string]$_.catalogId -notin @('manbo-cardboard-audio', 'more-equipment-slots')
    } | Select-Object -First 1)[0]
    $sourceMarkerPath = Join-Path (Join-Path $sourceMarkerMismatch.CandidateRoot ([string]$sourceMarkerProduct.packageName)) 'Content\DTMAPI\dtmapi-package.json'
    $sourceMarker = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourceMarkerPath | ConvertFrom-Json
    $sourceMarker.packageKind = 'workshop-mod'
    Write-Candidate11TestJson -Path $sourceMarkerPath -Value $sourceMarker
    $sourceMarkerError = ''
    try {
        $null = Get-Candidate11CatalogSelection -Path $sourceMarkerMismatch.CatalogPath -Root $sourceMarkerMismatch.CandidateRoot
    }
    catch {
        $sourceMarkerError = [string]$_.Exception.Message
    }
    Assert-Candidate11Test -Condition ($sourceMarkerError -match 'SDK marker does not bind') -Message 'Legacy workshop-mod marker did not fail the current source Candidate11 boundary.'
    Assert-Candidate11OriginalsRestored -Fixture $sourceMarkerMismatch -Label 'Current source marker mismatch preflight'

    $missingAdvancedReceipt = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '12')
    $missingReceiptProduct = @($missingAdvancedReceipt.Products | Where-Object { [string]$_.codeModKind -ceq 'Advanced' } | Select-Object -First 1)[0]
    $missingReceiptPath = Join-Path (Join-Path $missingAdvancedReceipt.CandidateRoot ([string]$missingReceiptProduct.packageName)) 'Content\DTMAPI\dtmapi-advanced-references.json'
    [System.IO.File]::Move($missingReceiptPath, (Join-Path $missingAdvancedReceipt.Root 'held-advanced-receipt.json'))
    $missingReceiptError = ''
    try { $null = Get-Candidate11CatalogSelection -Path $missingAdvancedReceipt.CatalogPath -Root $missingAdvancedReceipt.CandidateRoot }
    catch { $missingReceiptError = [string]$_.Exception.Message }
    Assert-Candidate11Test -Condition ($missingReceiptError -match 'missing its SDK reference receipt') -Message "Missing Advanced receipt did not fail before staging. Actual=$missingReceiptError"
    Assert-Candidate11OriginalsRestored -Fixture $missingAdvancedReceipt -Label 'Missing Advanced receipt preflight'

    $tamperedAdvancedEntry = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '13')
    $tamperedEntryProduct = @($tamperedAdvancedEntry.Products | Where-Object { [string]$_.codeModKind -ceq 'Advanced' } | Select-Object -First 1)[0]
    $tamperedEntryPath = Join-Path (Join-Path $tamperedAdvancedEntry.CandidateRoot ([string]$tamperedEntryProduct.packageName)) ('Content\DTMAPI\' + [string]$tamperedEntryProduct.packageDll)
    [System.IO.File]::AppendAllText($tamperedEntryPath, 'tamper-after-sdk-pack', (New-Object System.Text.UTF8Encoding($false)))
    $tamperedEntryError = ''
    try { $null = Get-Candidate11CatalogSelection -Path $tamperedAdvancedEntry.CatalogPath -Root $tamperedAdvancedEntry.CandidateRoot }
    catch { $tamperedEntryError = [string]$_.Exception.Message }
    Assert-Candidate11Test -Condition ($tamperedEntryError -match 'receipt identity/policy/manifest/entry/Harmony binding is invalid|SDK marker does not bind') -Message "Tampered Advanced entry DLL did not fail its payload binding. Actual=$tamperedEntryError"
    Assert-Candidate11OriginalsRestored -Fixture $tamperedAdvancedEntry -Label 'Tampered Advanced entry preflight'

    $wrongSdkTarget = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '14')
    $wrongTargetProduct = @($wrongSdkTarget.Products | Where-Object { [string]$_.codeModKind -ceq 'Advanced' } | Select-Object -First 1)[0]
    $wrongTargetMarkerPath = Join-Path (Join-Path $wrongSdkTarget.CandidateRoot ([string]$wrongTargetProduct.packageName)) 'Content\DTMAPI\dtmapi-package.json'
    $wrongTargetMarker = Get-Content -Raw -Encoding UTF8 -LiteralPath $wrongTargetMarkerPath | ConvertFrom-Json
    $wrongTargetMarker.targetDtmApiVersion = '0.6.0'
    Write-Candidate11TestJson -Path $wrongTargetMarkerPath -Value $wrongTargetMarker
    $wrongTargetError = ''
    try { $null = Get-Candidate11CatalogSelection -Path $wrongSdkTarget.CatalogPath -Root $wrongSdkTarget.CandidateRoot }
    catch { $wrongTargetError = [string]$_.Exception.Message }
    Assert-Candidate11Test -Condition ($wrongTargetError -match 'SDK marker does not bind') -Message "Wrong Author SDK target did not fail before staging. Actual=$wrongTargetError"
    Assert-Candidate11OriginalsRestored -Fixture $wrongSdkTarget -Label 'Wrong SDK target preflight'

    $wrongAdvancedKind = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '15')
    $wrongKindProduct = @($wrongAdvancedKind.Products | Where-Object { [string]$_.codeModKind -ceq 'Advanced' } | Select-Object -First 1)[0]
    $wrongKindManifestPath = Join-Path (Join-Path $wrongAdvancedKind.CandidateRoot ([string]$wrongKindProduct.packageName)) 'Content\DTMAPI\manifest.json'
    $wrongKindManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $wrongKindManifestPath | ConvertFrom-Json
    $wrongKindManifest.CodeModKind = 'Strict'
    Write-Candidate11TestJson -Path $wrongKindManifestPath -Value $wrongKindManifest
    $wrongKindError = ''
    try { $null = Get-Candidate11CatalogSelection -Path $wrongAdvancedKind.CatalogPath -Root $wrongAdvancedKind.CandidateRoot }
    catch { $wrongKindError = [string]$_.Exception.Message }
    Assert-Candidate11Test -Condition ($wrongKindError -match 'Type=CodeMod, CodeModKind=Advanced') -Message "Wrong manifest CodeModKind did not fail before staging. Actual=$wrongKindError"
    Assert-Candidate11OriginalsRestored -Fixture $wrongAdvancedKind -Label 'Wrong Advanced kind preflight'

    $unknownMarkerField = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '16')
    $unknownFieldProduct = @($unknownMarkerField.Products | Where-Object { [string]$_.codeModKind -ceq 'Advanced' } | Select-Object -First 1)[0]
    $unknownFieldMarkerPath = Join-Path (Join-Path $unknownMarkerField.CandidateRoot ([string]$unknownFieldProduct.packageName)) 'Content\DTMAPI\dtmapi-package.json'
    $unknownFieldMarker = Get-Content -Raw -Encoding UTF8 -LiteralPath $unknownFieldMarkerPath | ConvertFrom-Json
    $unknownFieldMarker | Add-Member -NotePropertyName unexpectedBinding -NotePropertyValue 'forbidden'
    Write-Candidate11TestJson -Path $unknownFieldMarkerPath -Value $unknownFieldMarker
    $unknownFieldError = ''
    try { $null = Get-Candidate11CatalogSelection -Path $unknownMarkerField.CatalogPath -Root $unknownMarkerField.CandidateRoot }
    catch { $unknownFieldError = [string]$_.Exception.Message }
    Assert-Candidate11Test -Condition ($unknownFieldError -match 'unknown, missing, duplicate, or wrong-version') -Message "Unknown SDK marker field did not fail the exact property set. Actual=$unknownFieldError"
    Assert-Candidate11OriginalsRestored -Fixture $unknownMarkerField -Label 'Unknown marker field preflight'

    $wrongInstalledRuntime = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '17')
    [System.IO.File]::AppendAllText((Join-Path $wrongInstalledRuntime.InstalledRuntimeRoot 'DTMAPI.Core.dll'), '-wrong-before-transaction', (New-Object System.Text.UTF8Encoding($false)))
    $wrongInstalledEvidence = Join-Path $wrongInstalledRuntime.Root 'E'
    $wrongInstalledError = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $wrongInstalledRuntime.CandidateRoot `
            -ProductCatalogPath $wrongInstalledRuntime.CatalogPath `
            -DolocPersistentRoot $wrongInstalledRuntime.PersistentRoot `
            -GameDirectory $wrongInstalledRuntime.GameDir `
            -FormalEvidenceRoot $wrongInstalledEvidence `
            -Id 'wrong-installed-runtime-before-mutation' `
            -SmokePath $wrongInstalledRuntime.SmokePath `
            -SmokeArguments @('-PersistentRoot',$wrongInstalledRuntime.PersistentRoot,'-CatalogPath',$wrongInstalledRuntime.CatalogPath,'-GameDir',$wrongInstalledRuntime.GameDir) `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest `
            -SkipSmokeContractForTest
    }
    catch { $wrongInstalledError = [string]$_.Exception.Message }
    Assert-Candidate11Test -Condition ($wrongInstalledError -match 'installed Runtime preflight failed before product staging') -Message "Wrong installed Runtime was not rejected before product staging. Actual=$wrongInstalledError"
    Assert-Candidate11Test -Condition (-not (Test-Path -LiteralPath (Join-Path $wrongInstalledEvidence '30-smoke-output.txt'))) -Message 'Wrong initial Runtime still invoked the child smoke.'
    Assert-Candidate11OriginalsRestored -Fixture $wrongInstalledRuntime -Label 'Wrong installed Runtime preflight'

    $badOuterContract = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '18')
    $badOuterEvidence = Join-Path $badOuterContract.Root 'E'
    $badOuterError = ''
    try {
        $null = Invoke-DtmApiCandidate11SourceTransaction `
            -CandidatePackageRoot $badOuterContract.CandidateRoot `
            -ProductCatalogPath $badOuterContract.CatalogPath `
            -DolocPersistentRoot $badOuterContract.PersistentRoot `
            -GameDirectory $badOuterContract.GameDir `
            -FormalEvidenceRoot $badOuterEvidence `
            -Id 'bad-outer-save-contract' `
            -SmokePath $badOuterContract.SmokePath `
            -SmokeArguments @('-UseSteam','-SkipInstall','-SaveSlot','3','-SaveTestMode','NoNativeSave','-SaveTestMode','ArchiveMutation','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence','-Issue011Acceptance') `
            -SkipRuntimeLockForTest `
            -SkipProcessCheckForTest
    }
    catch { $badOuterError = [string]$_.Exception.Message }
    Assert-Candidate11Test -Condition ($badOuterError -match 'exactly one -SaveTestMode') -Message "Invalid outer save contract did not stop before transaction setup. Actual=$badOuterError"
    Assert-Candidate11Test -Condition (-not (Test-Path -LiteralPath $badOuterEvidence)) -Message 'Invalid outer save contract created evidence before fail-close.'
    Assert-Candidate11OriginalsRestored -Fixture $badOuterContract -Label 'Invalid outer smoke contract'

    $issueReceiptNegativeIndex = 19
    foreach ($issueReceiptNegative in @(
        [ordered]@{ Mode = 'Omit'; Pattern = 'exactly one ISSUE-011 acceptance receipt marker' },
        [ordered]@{ Mode = 'Stale'; Pattern = 'timestamps are stale' },
        [ordered]@{ Mode = 'Tamper'; Pattern = 'length/SHA-256 binding' }
    )) {
        $issueFixture = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot $issueReceiptNegativeIndex.ToString('00'))
        $issueEvidence = Join-Path $issueFixture.Root 'E'
        $issueError = ''
        try {
            $null = Invoke-DtmApiCandidate11SourceTransaction `
                -CandidatePackageRoot $issueFixture.CandidateRoot `
                -ProductCatalogPath $issueFixture.CatalogPath `
                -DolocPersistentRoot $issueFixture.PersistentRoot `
                -GameDirectory $issueFixture.GameDir `
                -FormalEvidenceRoot $issueEvidence `
                -Id ('issue011-' + ([string]$issueReceiptNegative.Mode).ToLowerInvariant()) `
                -SmokePath $issueFixture.SmokePath `
                -SmokeArguments @('-PersistentRoot',$issueFixture.PersistentRoot,'-CatalogPath',$issueFixture.CatalogPath,'-GameDir',$issueFixture.GameDir,'-IssueReceiptMode',[string]$issueReceiptNegative.Mode) `
                -SkipRuntimeLockForTest `
                -SkipProcessCheckForTest `
                -SkipSmokeContractForTest
        }
        catch { $issueError = [string]$_.Exception.Message }
        Assert-Candidate11Test -Condition ($issueError -match [string]$issueReceiptNegative.Pattern) -Message "Exit-0 ISSUE-011 $($issueReceiptNegative.Mode) receipt was not rejected. Actual=$issueError"
        Assert-Candidate11OriginalsRestored -Fixture $issueFixture -Label "ISSUE-011 $($issueReceiptNegative.Mode) receipt negative"
        $issueResult = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $issueEvidence '99-result.json') | ConvertFrom-Json
        Assert-Candidate11Test -Condition (
            -not [bool]$issueResult.Passed -and [int]$issueResult.Smoke.ExitCode -eq 0 -and
            -not [bool]$issueResult.Issue011Acceptance.Passed -and [bool]$issueResult.RestoreAllExact
        ) -Message "Exit-0 ISSUE-011 $($issueReceiptNegative.Mode) result did not preserve fail-close/restoration facts."
        $issueReceiptNegativeIndex++
    }

    $liveProcessRecovery = New-Candidate11TestFixture -Root (Join-Path $resolvedTestRoot '22')
    Assert-Candidate11Test -Condition (Test-Candidate11DolocTownProcessAbsent) -Message 'Live-process recovery negative cannot run while the real game is active.'
    $liveProcessEvidence = Join-Path $liveProcessRecovery.Root 'E'
    $liveProcessWork = Join-Path (Join-Path $liveProcessRecovery.PersistentRoot '.dtmapi-candidate11-transactions') 'live-process-retains-lock'
    $oldRuntimeLockPathForCase = $env:DTMAPI_RUNTIME_LOCK_PATH
    $env:DTMAPI_RUNTIME_LOCK_PATH = Join-Path $liveProcessRecovery.Root 'candidate11-test-runtime.lock.json'
    try {
        $liveProcessError = ''
        try {
            $null = Invoke-DtmApiCandidate11SourceTransaction `
                -CandidatePackageRoot $liveProcessRecovery.CandidateRoot `
                -ProductCatalogPath $liveProcessRecovery.CatalogPath `
                -DolocPersistentRoot $liveProcessRecovery.PersistentRoot `
                -GameDirectory $liveProcessRecovery.GameDir `
                -FormalEvidenceRoot $liveProcessEvidence `
                -Id 'live-process-retains-lock' `
                -SmokePath $liveProcessRecovery.SmokePath `
                -SmokeArguments @('-PersistentRoot',$liveProcessRecovery.PersistentRoot,'-CatalogPath',$liveProcessRecovery.CatalogPath,'-GameDir',$liveProcessRecovery.GameDir) `
                -LockTimeoutSeconds 0 `
                -ProcessExitTimeoutSeconds 0 `
                -SkipSmokeContractForTest `
                -ForceProcessPresentAfterSmokeForTest
        }
        catch { $liveProcessError = [string]$_.Exception.Message }
        Assert-Candidate11Test -Condition ($liveProcessError -match 'runtime lock retained=True') -Message "Forced live-process failure did not retain its Runtime lock. Actual=$liveProcessError"
        $pendingRecoveryPath = Join-Path $liveProcessWork 'receipts\70-recovery-pending.json'
        Assert-Candidate11Test -Condition (Test-Path -LiteralPath $pendingRecoveryPath -PathType Leaf) -Message 'Live-process failure did not create a durable pending-recovery receipt.'
        $pendingRecovery = Get-Content -Raw -Encoding UTF8 -LiteralPath $pendingRecoveryPath | ConvertFrom-Json
        $heldLock = Get-DtmApiRuntimeLockInfo -RepoRoot $repo
        Assert-Candidate11Test -Condition (
            $heldLock.Exists -and $heldLock.IsReadable -and
            [string]$heldLock.Data.Token -ceq [string]$pendingRecovery.LockToken -and
            -not [bool]$pendingRecovery.ProcessAbsent
        ) -Message 'Live-process failure did not bind the retained lock token to its recovery receipt.'
        $blockedAcquire = Acquire-DtmApiRuntimeLock -RepoRoot $repo -Reason 'Candidate11 blocked-next-operation negative' -Owner 'blocked-probe' -NoThrow
        Assert-Candidate11Test -Condition (-not [bool]$blockedAcquire.Acquired -and -not [bool]$blockedAcquire.Reused) -Message 'A second shared-runtime operation was not blocked by the retained Candidate11 lock.'
        $recoveryResult = Invoke-DtmApiCandidate11Recovery -WorkRoot $liveProcessWork
        Assert-Candidate11Test -Condition (
            [bool]$recoveryResult.Passed -and [bool]$recoveryResult.AllRestoredExact -and
            [bool]$recoveryResult.InstalledRuntimeExact -and [bool]$recoveryResult.RuntimeLockReleased
        ) -Message 'Candidate11 bounded recovery did not restore exact products/runtime and release the retained lock.'
        Assert-Candidate11OriginalsRestored -Fixture $liveProcessRecovery -Label 'Live-process bounded recovery'
        Assert-Candidate11Test -Condition (-not (Get-DtmApiRuntimeLockInfo -RepoRoot $repo).Exists) -Message 'Candidate11 recovery left the test Runtime lock held.'
        Assert-Candidate11Test -Condition (Test-Path -LiteralPath (Join-Path $liveProcessWork 'receipts\72-recovery-complete.json') -PathType Leaf) -Message 'Candidate11 recovery did not persist its completion receipt.'
    }
    finally {
        $env:DTMAPI_RUNTIME_LOCK_PATH = $oldRuntimeLockPathForCase
    }

    if (-not $Quiet) {
        Write-Host 'Candidate11 source transaction tests: OK (9 exact SDK payload bindings + 2 legacy retained trees, pre/post Runtime+exact11 binding, exact slot/mode/ISSUE-011 contract, current-run acceptance receipt positives/negatives, child-smoke, restore, product/runtime drift, inventory/ADS preflight)'
    }
}
finally {
    $env:DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE = $oldTestMode
    $resolvedRoot = Get-Candidate11CanonicalPath -Path $resolvedTestRoot
    if ((Test-Candidate11SameOrChildPath -Child $resolvedRoot -Parent $resolvedManagedParent) -and
        (Split-Path -Leaf $resolvedRoot) -match '^[0-9a-f]{32}$' -and
        (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
    if ((Test-Path -LiteralPath $resolvedManagedParent -PathType Container) -and
        @(Get-ChildItem -LiteralPath $resolvedManagedParent -Force -ErrorAction Stop).Count -eq 0) {
        Remove-Item -LiteralPath $resolvedManagedParent -Force
    }
}
