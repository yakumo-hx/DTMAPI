function Get-SmokePendingProductRecovery {
    param(
        [Parameter(Mandatory = $true)] [string] $CatalogPath,
        [Parameter(Mandatory = $true)] [string] $StateDir,
        [Parameter(Mandatory = $true)] [string] $OfficialLocalModsRoot,
        [Parameter(Mandatory = $true)] [string] $WorkshopContentRoot,
        [AllowEmptyCollection()] [string[]] $EnabledSourceIds = @(),
        [int] $ArchiveIndex = -1,
        [string] $SaveTestMode = 'NoNativeSave',
        [bool] $ExplicitRecoveryScenario = $false
    )
    $result = [ordered]@{Passed=$true;Reason='NotRequested';ArchiveIndex=$ArchiveIndex;Owner='DTMAPI.MoreEquipmentSlotsMod';SourceIds=@();SidecarPath='';SidecarSha256='';PendingProductRecovery=$false;PendingProductMigration=$false;PendingProductColdRecovery=$false}
    if ($SaveTestMode -cne 'NoNativeSave' -or $ArchiveIndex -lt 0 -or $ExplicitRecoveryScenario) { return [pscustomobject]$result }
    $catalog=Get-Content -LiteralPath $CatalogPath -Raw -Encoding UTF8|ConvertFrom-Json
    $rows=@($catalog.products|Where-Object {$_.catalogId -ceq 'more-equipment-slots' -and $_.uniqueId -ceq $result.Owner})
    if($rows.Count -ne 1){throw 'Pending recovery preflight requires one Catalog MoreEquipment owner.'}
    $product=$rows[0]
    $sources=New-Object 'Collections.Generic.List[string]'
    foreach($source in @(
        @{Id=('Local.'+[string]$product.officialFolder);Root=(Join-Path $OfficialLocalModsRoot ([string]$product.officialFolder))},
        @{Id=('Workshop.'+[string]$product.workshopId);Root=(Join-Path $WorkshopContentRoot ([string]$product.workshopId))}
    )) {
        if($EnabledSourceIds -cnotcontains $source.Id){continue}
        $manifestPath=Join-Path $source.Root 'Content/DTMAPI/manifest.json'
        $expectedEntry=Join-Path $source.Root ('Content/DTMAPI/'+[string]$product.packageDll)
        if(-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or -not (Test-Path -LiteralPath $expectedEntry -PathType Leaf) -or
            -not (Test-Path -LiteralPath (Join-Path $source.Root 'info.json') -PathType Leaf)){continue}
        $manifest=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8|ConvertFrom-Json
        $relative=([string]$manifest.EntryDll).Replace('\','/')
        if($manifest.UniqueID -cne $result.Owner -or [string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or $relative -match '(^|/)\.\.(/|$)|:'){continue}
        if([string]::Equals([IO.Path]::GetFullPath((Join-Path $source.Root $relative)),[IO.Path]::GetFullPath($expectedEntry),[StringComparison]::OrdinalIgnoreCase)){$sources.Add($source.Id)}
    }
    $result.SourceIds=@($sources.ToArray())
    $templates=@($product.saveSidecars)
    if($templates.Count -ne 1){throw 'Pending recovery preflight requires the single Catalog MoreEquipment sidecar template.'}
    $relative=([string]$templates[0]).Replace('<archiveIndex>',$ArchiveIndex.ToString([Globalization.CultureInfo]::InvariantCulture)).Replace('\','/')
    if(-not $relative.StartsWith('DTMAPI/',[StringComparison]::Ordinal) -or $relative -match '<|>|(^|/)\.\.(/|$)|:'){throw 'Pending recovery sidecar template escaped its state root.'}
    $path=[IO.Path]::GetFullPath((Join-Path $StateDir $relative.Substring('DTMAPI/'.Length)))
    if([IO.Path]::GetFileName($path) -cne 'equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json'){throw 'Pending recovery sidecar does not bind the reviewed owner.'}
    $result.SidecarPath=$path
    if(-not (Test-Path -LiteralPath $path -PathType Leaf)){$result.Reason='NoCurrentSidecar';return [pscustomobject]$result}
    $result.SidecarSha256=Get-SmokeFileSha256 -Path $path
    $document=Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json
    $result.Reason='NoKnownPendingAttempt'
    # Match the Product classifier's shape distinction: schema 3 alone does
    # not distinguish Product v3 (scope) from the reviewed scoped flat file.
    # This is a preflight classification, not validation or migration of it.
    $hasProductShape=$document.PSObject.Properties['scope'] -and $null -ne $document.scope
    if($sources.Count -eq 0) {
        $result.Reason='OwnerNotSelectedOrUnavailable'
        # The dormant Compatibility Host owns disabled Product storage. Its
        # canonical Product route keeps a journal or stages occupied slots for
        # orphan recovery. Do not apply that rule to a loaded Product owner.
        $hasLegacyShape=($document.PSObject.Properties['ownerId'] -and $null -ne $document.ownerId) -or
            ($document.PSObject.Properties['storageScope'] -and $null -ne $document.storageScope) -or
            ($document.PSObject.Properties['archiveIndex'] -and $null -ne $document.archiveIndex)
        if($hasProductShape -and -not $hasLegacyShape -and $document.PSObject.Properties['schemaVersion'] -and [int]$document.schemaVersion -eq 3 -and
            $document.scope.PSObject.Properties['archiveIndex'] -and [int]$document.scope.archiveIndex -eq $ArchiveIndex) {
            $occupied=if($document.PSObject.Properties['slots']){@($document.slots | Where-Object {$_ -and $_.PSObject.Properties['itemId'] -and -not [string]::IsNullOrWhiteSpace([string]$_.itemId)}).Count}else{0}
            $escrow=0
            if($document.PSObject.Properties['journal'] -and $document.journal) {
                $journal=$document.journal
                if($journal.PSObject.Properties['origin'] -and [int]$journal.origin -in @(2,3) -and
                    $journal.PSObject.Properties['phase'] -and [int]$journal.phase -in @(0,1) -and
                    $journal.PSObject.Properties['scope'] -and $journal.scope -and $journal.scope.PSObject.Properties['archiveIndex'] -and
                    [int]$journal.scope.archiveIndex -eq $ArchiveIndex -and $journal.PSObject.Properties['escrow']) {
                    $escrow=@($journal.escrow | Where-Object {$_ -and $_.PSObject.Properties['itemId'] -and -not [string]::IsNullOrWhiteSpace([string]$_.itemId)}).Count
                }
            }
            if($occupied -gt 0 -or $escrow -gt 0) {
                $result.PendingProductColdRecovery=$true;$result.Passed=$false;$result.Reason='PendingProductColdRecovery'
                $result['SchemaVersion']=3;$result['StorageFormat']='ProductV3';$result['OccupiedSlotCount']=$occupied;$result['JournalItemCount']=$escrow
            }
        }
        return [pscustomobject]$result
    }
    if(-not $hasProductShape -and $document.PSObject.Properties['schemaVersion'] -and [int]$document.schemaVersion -eq 3 -and
        $document.PSObject.Properties['ownerId'] -and $document.PSObject.Properties['storageScope'] -and
        $document.PSObject.Properties['archiveIndex'] -and [int]$document.archiveIndex -eq $ArchiveIndex -and
        [string]::Equals(([string]$document.ownerId).Trim(),$result.Owner,[StringComparison]::OrdinalIgnoreCase) -and
        [string]::Equals(([string]$document.storageScope).Trim(),('slot-'+$ArchiveIndex.ToString([Globalization.CultureInfo]::InvariantCulture)),[StringComparison]::OrdinalIgnoreCase)) {
        $result.PendingProductMigration=$true;$result.Passed=$false;$result.Reason='PendingProductMigration'
        $result['SchemaVersion']=3;$result['StorageFormat']='ScopedFlat'
        return [pscustomobject]$result
    }
    if($document.PSObject.Properties['schemaVersion'] -and [int]$document.schemaVersion -eq 3 -and
        $document.PSObject.Properties['scope'] -and $document.scope -and $document.scope.PSObject.Properties['archiveIndex'] -and
        [int]$document.scope.archiveIndex -eq $ArchiveIndex -and $document.PSObject.Properties['journal'] -and $document.journal) {
        $journal=$document.journal
        if($journal.PSObject.Properties['origin'] -and $journal.PSObject.Properties['phase'] -and $journal.PSObject.Properties['attemptStarted'] -and
            [int]$journal.origin -eq 3 -and [int]$journal.phase -eq 0 -and $journal.attemptStarted -is [bool] -and $journal.attemptStarted) {
            $result.PendingProductRecovery=$true;$result.Passed=$false;$result.Reason='PendingProductRecovery'
            $result['SchemaVersion']=3;$result['JournalOrigin']=3;$result['JournalPhase']=0;$result['AttemptStarted']=$true
        }
    }
    return [pscustomobject]$result
}

function Get-SmokeNativeArchiveIndex {
    param([ValidateRange(1, 12)] [int] $UiSaveSlot)
    return $UiSaveSlot - 1
}

function Get-SmokeMoreSavesStartupRisk {
    param(
        [Parameter(Mandatory = $true)] [string] $EnablementPath,
        [Parameter(Mandatory = $true)] [string] $SaveRoot,
        [Parameter(Mandatory = $true)] [string] $OfficialLocalModsRoot,
        [Parameter(Mandatory = $true)] [string] $WorkshopContentRoot,
        [ValidateSet('NoNativeSave','NativeSaveExpected','ArchiveMutation')] [string] $SaveTestMode,
        [bool] $DisposableFixtureRequested,
        [bool] $StageQaHost,
        [bool] $DirectExe,
        [bool] $UseSteam
    )
    $enabled = @(Get-DtmApiEnabledModInfoIds -ModInfoPath $EnablementPath | Where-Object {
        $_ -in @('Workshop.3742763050', 'Local.DTMAPI_MoreSaves')
    } | Sort-Object -Unique)
    $candidates = @(Get-DtmApiMoreSavesLegacyArchiveCandidates -SaveRoot $SaveRoot)
    $sources = New-Object 'System.Collections.Generic.List[object]'
    $uncertain = $false
    if ($candidates.Count -gt 0) {
        foreach ($id in $enabled) {
            $root = if ($id -ceq 'Local.DTMAPI_MoreSaves') { Join-Path $OfficialLocalModsRoot 'DTMAPI_MoreSaves' }
                else { Join-Path $WorkshopContentRoot '3742763050' }
            $root = [IO.Path]::GetFullPath($root)
            $available = $false
            $reason = 'PackageAbsent'
            if (Test-Path -LiteralPath $root) {
                try {
                    $manifestPath = Join-Path $root 'Content/DTMAPI/manifest.json'
                    $entryPath = Join-Path $root 'Content/DTMAPI/DTMAPI.MoreSaves.dll'
                    foreach ($path in @($root, (Join-Path $root 'Content'), (Join-Path $root 'Content/DTMAPI'), (Join-Path $root 'info.json'), $manifestPath, $entryPath)) {
                        $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
                        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Reparse package path.' }
                    }
                    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
                    # Official package manifests bind EntryDll from the package
                    # root, unlike the unprojected source manifest template.
                    $entryRelative = ([string]$manifest.EntryDll).Replace('\','/')
                    if ([string]::IsNullOrWhiteSpace($entryRelative) -or [IO.Path]::IsPathRooted($entryRelative) -or
                        $entryRelative -match '(^|/)\.\.(/|$)|:') {
                        throw 'MoreSaves entry is not a bounded package-relative path.'
                    }
                    $resolvedEntry = [IO.Path]::GetFullPath((Join-Path $root $entryRelative))
                    if ($manifest.UniqueID -cne 'DTMAPI.MoreSavesMod' -or
                        -not [string]::Equals($resolvedEntry, [IO.Path]::GetFullPath($entryPath), [StringComparison]::OrdinalIgnoreCase)) {
                        throw 'MoreSaves source identity is not the reviewed managed entry.'
                    }
                    $available = $true
                    $reason = 'EnabledManagedEntryPresent'
                }
                catch {
                    $uncertain = $true
                    $reason = 'EnabledPackageIncompleteOrUnrecognized'
                }
            }
            $sources.Add([pscustomobject]@{ SourceId = $id; Root = $root; Available = $available; Reason = $reason })
        }
    }
    $detected = @($sources | Where-Object { $_.Available }).Count -gt 0 -and $candidates.Count -gt 0
    $classified = -not $uncertain -and (-not $detected -or
        ($SaveTestMode -ceq 'ArchiveMutation' -and $DisposableFixtureRequested -and $StageQaHost -and $DirectExe -and -not $UseSteam))
    return [pscustomobject]@{
        EnabledMoreSavesIds = @($enabled)
        AvailableSources = @($sources.ToArray())
        LegacyCandidates = @($candidates)
        StartupMigrationDetected = $detected
        PackageClassificationUncertain = $uncertain
        PreRuntimeSaveGuardRequired = $detected
        Passed = $classified
    }
}

function Get-DolocTownLivePersistentRootForSmoke {
    return [System.IO.Path]::GetFullPath(
        (Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\RedSawGames\DolocTown'))
}

function Get-DolocTownPersistentRootForSmoke {
    if (-not [string]::IsNullOrWhiteSpace($DisposableSaveFixtureRoot)) {
        return [System.IO.Path]::GetFullPath($DisposableSaveFixtureRoot)
    }
    if ($env:DTMAPI_DOLOC_PERSISTENT_ROOT) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_DOLOC_PERSISTENT_ROOT)
    }

    return Get-DolocTownLivePersistentRootForSmoke
}

function Assert-SmokeOrdinaryFixtureTree {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Mode
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $pending.Push($resolvedRoot)
    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Mode fixture must not contain a junction, symbolic link, or other reparse point: $current"
        }
        if (-not $item.PSIsContainer) {
            continue
        }
        foreach ($child in @(Get-ChildItem -LiteralPath $current -Force -ErrorAction Stop)) {
            if (($child.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Mode fixture must not contain a junction, symbolic link, or other reparse point: $($child.FullName)"
            }
            if ($child.PSIsContainer) {
                $pending.Push($child.FullName)
            }
        }
    }
}

function Remove-SmokeDisposableSaveFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $ExpectedMarkerSha256,
        [Parameter(Mandatory = $true)] [string] $Mode
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    $livePersistentRoot = Get-DolocTownLivePersistentRootForSmoke
    $fixturePrefix = $resolvedRoot.TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    $livePrefix = $livePersistentRoot.TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    if ([string]::Equals($resolvedRoot, $livePersistentRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        $fixturePrefix.StartsWith($livePrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        $livePrefix.StartsWith($fixturePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Mode fixture cleanup refused the live Steam AutoCloud persistent-data tree."
    }
    if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        throw "$Mode fixture cleanup could not find the exact disposable root: $resolvedRoot"
    }

    Assert-SmokeOrdinaryFixtureTree -Root $resolvedRoot -Mode "$Mode cleanup"
    $markerPath = Join-Path $resolvedRoot '.dtmapi-disposable-save-fixture.json'
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "$Mode fixture cleanup refused a root without its ownership marker."
    }
    $actualMarkerSha256 = (Get-FileHash -LiteralPath $markerPath -Algorithm SHA256).Hash
    if (-not [string]::Equals(
        $actualMarkerSha256,
        $ExpectedMarkerSha256,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Mode fixture cleanup refused a changed ownership marker."
    }
    $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
    if ([int]$marker.schemaVersion -ne 1 -or
        -not [bool]$marker.disposable -or
        -not [bool]$marker.steamAutoCloudIsolated) {
        throw "$Mode fixture cleanup refused a marker that no longer declares an isolated disposable fixture."
    }

    Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    if (Test-Path -LiteralPath $resolvedRoot) {
        throw "$Mode fixture cleanup did not remove the exact disposable root."
    }
}
