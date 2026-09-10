function New-SmokeDirectoryBaseline {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $BackupPath
    )

    $receipt = Get-SmokeDirectoryReceipt -Path $Path
    $resolvedBackup = [System.IO.Path]::GetFullPath($BackupPath)
    if (Test-Path -LiteralPath $resolvedBackup) {
        throw "Directory transaction backup already exists: $resolvedBackup"
    }
    if ($receipt.Existed) {
        Copy-Item -LiteralPath $receipt.Path -Destination $resolvedBackup -Recurse
    }
    return [pscustomobject]@{
        Path = $receipt.Path
        BackupPath = $resolvedBackup
        Existed = $receipt.Existed
        DirectoryCount = $receipt.DirectoryCount
        Directories = @($receipt.Directories)
        FileCount = $receipt.FileCount
        Files = @($receipt.Files)
    }
}

function Restore-SmokeDirectoryBaseline {
    param(
        [Parameter(Mandatory = $true)] $Baseline,
        [Parameter(Mandatory = $true)] [string] $AllowedRoot,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $target = [System.IO.Path]::GetFullPath([string]$Baseline.Path)
    $allowed = [System.IO.Path]::GetFullPath($AllowedRoot).TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $target.StartsWith($allowed, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Directory transaction target escaped its allowed root: target=$target allowed=$allowed"
    }
    # Verify recovery material before any destructive change to the target.
    if ([bool]$Baseline.Existed) {
        if (-not (Test-Path -LiteralPath ([string]$Baseline.BackupPath) -PathType Container)) {
            throw "Directory transaction backup is missing: $([string]$Baseline.BackupPath)"
        }
        $backupReceipt = Get-SmokeDirectoryReceipt -Path ([string]$Baseline.BackupPath)
        if (-not [bool](Compare-SmokeDirectoryReceipt -Before $Baseline -After $backupReceipt).Passed) {
            throw 'Directory transaction backup no longer matches its recorded baseline.'
        }
    }
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
    if ([bool]$Baseline.Existed) {
        if (-not (Test-Path -LiteralPath ([string]$Baseline.BackupPath) -PathType Container)) {
            throw "Directory transaction backup is missing: $([string]$Baseline.BackupPath)"
        }
        Copy-Item -LiteralPath ([string]$Baseline.BackupPath) -Destination $target -Recurse
    }
    $after = Get-SmokeDirectoryReceipt -Path $target
    $expectedDirectoriesJson = @($Baseline.Directories) | ConvertTo-Json -Depth 5 -Compress
    $actualDirectoriesJson = @($after.Directories) | ConvertTo-Json -Depth 5 -Compress
    $expectedFilesJson = @($Baseline.Files) | ConvertTo-Json -Depth 5 -Compress
    $actualFilesJson = @($after.Files) | ConvertTo-Json -Depth 5 -Compress
    $passed = [bool]$Baseline.Existed -eq [bool]$after.Existed -and
        [int]$Baseline.DirectoryCount -eq [int]$after.DirectoryCount -and
        [string]::Equals($expectedDirectoriesJson, $actualDirectoriesJson, [System.StringComparison]::Ordinal) -and
        [int]$Baseline.FileCount -eq [int]$after.FileCount -and
        [string]::Equals($expectedFilesJson, $actualFilesJson, [System.StringComparison]::Ordinal)
    Write-SmokeJsonObject -Path $EvidencePath -Value ([ordered]@{
        TargetPath = $target
        BackupPath = [string]$Baseline.BackupPath
        ExistedBefore = [bool]$Baseline.Existed
        ExistsAfter = [bool]$after.Existed
        ExpectedDirectoryCount = [int]$Baseline.DirectoryCount
        ActualDirectoryCount = [int]$after.DirectoryCount
        ExpectedDirectories = @($Baseline.Directories)
        ActualDirectories = @($after.Directories)
        ExpectedFileCount = [int]$Baseline.FileCount
        ActualFileCount = [int]$after.FileCount
        ExpectedFiles = @($Baseline.Files)
        ActualFiles = @($after.Files)
        Passed = $passed
    })
    return $passed
}

function Get-SmokePublishedProductDefinitions {
    $catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
    if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
        throw "Published-product smoke gate requires the tracked Product Catalog: $catalogPath"
    }

    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $products = @($catalog.products | Where-Object {
        [string]$_.role -eq 'PublishedProduct' -and [string]$_.distributionState -eq 'PublicWorkshop'
    } | Sort-Object { [string]$_.catalogId })
    if ($products.Count -ne 11) {
        throw "Published-product smoke gate requires exactly 11 public Product Catalog rows; found $($products.Count)."
    }
    foreach ($product in $products) {
        if ([string]::IsNullOrWhiteSpace([string]$product.workshopId) -or [string]::IsNullOrWhiteSpace([string]$product.uniqueId)) {
            throw "Published-product smoke gate found an incomplete Catalog row: $($product.catalogId)."
        }
    }

    return $products
}

function Get-SmokePublishedProductWorkshopIds {
    return @(Get-SmokePublishedProductDefinitions | ForEach-Object { 'Workshop.' + [string]$_.workshopId })
}

function Get-SmokePublishedProductLocalIds {
    $products = @(Get-SmokePublishedProductDefinitions)
    foreach ($product in $products) {
        if ([string]::IsNullOrWhiteSpace([string]$product.officialFolder)) {
            throw "Local11 smoke profile found a Catalog product without officialFolder: $($product.catalogId)."
        }
    }

    return @($products | ForEach-Object { 'Local.' + [string]$_.officialFolder })
}

function Get-SmokeAuthorSourceStatePath {
    param([Parameter(Mandatory = $true)] [string] $GameDir)

    $canonicalGameRoot = [System.IO.Path]::GetFullPath($GameDir).TrimEnd([char]92, [char]47)
    $stateBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_AUTHOR_STATE_ROOT)) {
        [System.IO.Path]::GetFullPath($env:DTMAPI_AUTHOR_STATE_ROOT)
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

function Set-SmokeRecoveryOnlyAuthorSourceState {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $canonicalGameRoot = [System.IO.Path]::GetFullPath($GameDir).TrimEnd([char]92, [char]47)
    $statePath = Get-SmokeAuthorSourceStatePath -GameDir $canonicalGameRoot
    $backupPath = Join-Path $EvidencePath 'recovery-only-author-source-state.before.json'
    $originalExisted = Test-Path -LiteralPath $statePath -PathType Leaf
    $originalLength = if ($originalExisted) { [int64](Get-Item -LiteralPath $statePath).Length } else { [int64]0 }
    $originalSha256 = if ($originalExisted) { Get-SmokeFileSha256 -Path $statePath } else { '' }
    if ($originalExisted) {
        Copy-Item -LiteralPath $statePath -Destination $backupPath -Force
    }
    else {
        'No pre-existing author source state file.' | Set-Content -LiteralPath $backupPath -Encoding UTF8
    }

    $summary = [ordered]@{
        Applied = $false
        StatePath = [System.IO.Path]::GetFullPath($statePath)
        BackupPath = [System.IO.Path]::GetFullPath($backupPath)
        OriginalExisted = $originalExisted
        OriginalLength = $originalLength
        OriginalSha256 = $originalSha256
        GameRoot = $canonicalGameRoot
        Purpose = 'RecoveryOnlyClear'
        RequestedLocalIds = @()
        Selections = @()
    }
    try {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $statePath) | Out-Null
        Write-SmokeJsonObject -Path $statePath -Value ([ordered]@{
            schemaVersion = 1
            gameRoot = $canonicalGameRoot
            playerReproductionActive = $false
            reproductionSnapshotId = ''
            selections = @()
        })
        $summary.Applied = $true
        Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'recovery-only-author-source-state-summary.json') -Value $summary
        return $summary
    }
    catch {
        if ($originalExisted) {
            Copy-Item -LiteralPath $backupPath -Destination $statePath -Force
        }
        elseif (Test-Path -LiteralPath $statePath -PathType Leaf) {
            Remove-Item -LiteralPath $statePath -Force
        }
        throw
    }
}

function Restore-SmokeRecoveryOnlyAuthorSourceState {
    param(
        [Parameter(Mandatory = $true)] $Summary,
        [Parameter(Mandatory = $true)] [string] $EvidencePath
    )

    $statePath = [string]$Summary.StatePath
    if ([bool]$Summary.OriginalExisted) {
        Copy-Item -LiteralPath ([string]$Summary.BackupPath) -Destination $statePath -Force
    }
    elseif (Test-Path -LiteralPath $statePath -PathType Leaf) {
        Remove-Item -LiteralPath $statePath -Force
    }
    $existsAfter = Test-Path -LiteralPath $statePath -PathType Leaf
    $actualLength = if ($existsAfter) { [int64](Get-Item -LiteralPath $statePath).Length } else { [int64]0 }
    $actualSha256 = if ($existsAfter) { Get-SmokeFileSha256 -Path $statePath } else { '' }
    $passed = $existsAfter -eq [bool]$Summary.OriginalExisted -and
        $actualLength -eq [int64]$Summary.OriginalLength -and
        [string]::Equals($actualSha256, [string]$Summary.OriginalSha256, [System.StringComparison]::OrdinalIgnoreCase)
    Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'recovery-only-author-source-state-restore-verification.json') -Value ([ordered]@{
        StatePath = $statePath
        OriginalExisted = [bool]$Summary.OriginalExisted
        ExistsAfter = $existsAfter
        ExpectedLength = [int64]$Summary.OriginalLength
        ActualLength = $actualLength
        ExpectedSha256 = [string]$Summary.OriginalSha256
        ActualSha256 = $actualSha256
        Passed = $passed
    })
    return $passed
}

function Get-SmokeOfficialModProfileKnownIds {
    return @(
        'Local.DTMAPI',
        'Local.DTMAPI_ChestLocatorEnhancer',
        'Local.DTMAPI_DreckoAssets',
        'Local.DTMAPI_ExtraVehicle',
        'Local.DTMAPI_HatchAssets',
        'Local.DTMAPI_LightningChicken',
        'Local.DTMAPI_Mine',
        'Local.DTMAPI_MoleAssets',
        'Local.DTMAPI_MoreEquipmentSlots',
        'Local.DTMAPI_MoreSaves',
        'Local.DTMAPI_Oil',
        'Local.DTMAPI_OilfloaterAssets',
        'Local.DTMAPI_ShellCrab',
        'Local.DTMAPI_StrongPlantingGun',
        'Local.DTMAPI_YKeyConsole',
        'Local.DTMAPI_Zoom',
        'Local.Yuuka_DTMAPI_ActionSpeed',
        'Local.Yuuka_DTMAPI_AnimalHusbandryProgress',
        'Local.Yuuka_DTMAPI_AutoFishing',
        'Local.Yuuka_DTMAPI_ChickenPetBag',
        'Local.Yuuka_DTMAPI_FishBreedingAssistant',
        'Local.Yuuka_DTMAPI_ManboCardboardAudio',
        'Local.Yuuka_DTMAPI_OneActionComplete',
        'Workshop.3742714442',
        'Workshop.3742717440',
        'Workshop.3742763050',
        'Workshop.3742763309',
        'Workshop.3742763540',
        'Workshop.3742763706',
        'Workshop.3742763843',
        'Workshop.3742765514',
        'Workshop.3743016467',
        'Workshop.3743799721',
        'Workshop.3744059735',
        'Workshop.3746319981'
    )
}

function Set-SmokeModInfoEnabled {
    param(
        [Parameter(Mandatory = $true)] $Entry,
        [Parameter(Mandatory = $true)] [bool] $Enabled,
        [Parameter(Mandatory = $true)] [int] $Priority
    )

    if ($Entry.PSObject.Properties['enabled']) {
        $Entry.enabled = $Enabled
    }
    else {
        $Entry | Add-Member -MemberType NoteProperty -Name 'enabled' -Value $Enabled
    }

    if ($Entry.PSObject.Properties['priority']) {
        $Entry.priority = $Priority
    }
    else {
        $Entry | Add-Member -MemberType NoteProperty -Name 'priority' -Value $Priority
    }
}

function Set-SmokeOfficialModProfile {
    param(
        [Parameter(Mandatory = $true)] [string] $Profile,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [string[]] $ExtraEnabledIds = @(),
        [bool] $IsolateAll = $false
    )

    # Native Workshop source capture reads the official ModManager before the
    # QA host redirects LocalSave.cloudDirPath. The temporary profile therefore
    # still targets the live pre-launch enablement file; Runtime Mod discovery
    # occurs only after the pre-Runtime save guard is installed.
    $persistentRoot = Get-DolocTownLivePersistentRootForSmoke
    $enablementPath = Join-Path $persistentRoot 'SAVE\mod_infos.json'
    $summaryPath = Join-Path $EvidencePath 'official-mod-profile-summary.json'
    $normalizedExtraEnabledIds = @($ExtraEnabledIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
    $profileRequiredEnabledIds = if ($Profile -eq 'Published11') {
        @(Get-SmokePublishedProductWorkshopIds)
    }
    elseif ($Profile -eq 'Local11') {
        @(Get-SmokePublishedProductLocalIds)
    }
    else {
        @()
    }
    $requiredEnabledIds = @($normalizedExtraEnabledIds + $profileRequiredEnabledIds | Sort-Object -Unique)
    if ($Profile -eq 'Current') {
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $false
            Reason = if ($normalizedExtraEnabledIds.Count -gt 0) { 'Current profile cannot apply ExtraEnabledIds; mod_infos.json was left unchanged.' } else { 'Current profile requested; mod_infos.json was left unchanged.' }
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = ''
            EnabledIds = @()
            DisabledIds = @()
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            MissingExtraEnabledIds = @($normalizedExtraEnabledIds)
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }

    if (-not (Test-Path -LiteralPath $enablementPath -PathType Leaf)) {
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $false
            Reason = 'mod_infos.json was not found; profile could not be applied.'
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = ''
            EnabledIds = @()
            DisabledIds = @()
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            MissingExtraEnabledIds = @($normalizedExtraEnabledIds)
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }

    $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $enablementPath | ConvertFrom-Json
    if ($null -eq $data.modInfos) {
        $data | Add-Member -MemberType NoteProperty -Name 'modInfos' -Value ([pscustomobject]@{}) -Force
    }

    $missingExtraEnabledIds = @($requiredEnabledIds | Where-Object { -not $data.modInfos.PSObject.Properties[$_] })
    if ($missingExtraEnabledIds.Count -gt 0) {
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $false
            Reason = 'One or more required profile/extra enabled IDs are missing from mod_infos.json; profile was not applied.'
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = ''
            EnabledIds = @()
            DisabledIds = @()
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            RequiredEnabledIds = @($requiredEnabledIds)
            MissingExtraEnabledIds = @($missingExtraEnabledIds)
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }

    $targetIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($id in @(Get-SmokeOfficialModProfileKnownIds)) {
        [void]$targetIds.Add($id)
    }

    if ($IsolateAll) {
        foreach ($property in @($data.modInfos.PSObject.Properties)) {
            if ($property.Name -like 'Local.*' -or $property.Name -like 'Workshop.*') {
                [void]$targetIds.Add($property.Name)
            }
        }
    }

    foreach ($property in @($data.modInfos.PSObject.Properties)) {
        $title = if ($property.Value -and $property.Value.PSObject.Properties['title']) { [string]$property.Value.title } else { '' }
        if (($property.Name -like 'Local.*' -or $property.Name -like 'Workshop.*') -and
            ($property.Name -match 'DTMAPI' -or $title -match 'DTMAPI|Y键控制台|哈奇|抛壳蟹|浮游生物|田鼠|壁虎|曼波')) {
            [void]$targetIds.Add($property.Name)
        }
    }

    $enabledIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    function Add-PreferredSmokeModId {
        param([string[]] $Candidates)

        foreach ($candidate in $Candidates) {
            if ($data.modInfos.PSObject.Properties[$candidate]) {
                [void]$enabledIds.Add($candidate)
                return
            }
        }

        if ($Candidates.Count -gt 0) {
            [void]$enabledIds.Add($Candidates[0])
        }
    }

    switch ($Profile) {
        'CoreOnly' {
        }
        'CoreUi' {
            Add-PreferredSmokeModId @('Workshop.3742717440','Local.DTMAPI_Zoom')
            Add-PreferredSmokeModId @('Workshop.3742763050','Local.DTMAPI_MoreSaves')
            Add-PreferredSmokeModId @('Workshop.3744059735','Local.DTMAPI_MoreEquipmentSlots')
            Add-PreferredSmokeModId @('Workshop.3742714442','Local.DTMAPI_YKeyConsole')
        }
        'CoreCustomAnimals' {
            Add-PreferredSmokeModId @('Local.DTMAPI_ShellCrab')
            Add-PreferredSmokeModId @('Local.DTMAPI_HatchAssets')
            Add-PreferredSmokeModId @('Local.DTMAPI_OilfloaterAssets')
            Add-PreferredSmokeModId @('Local.DTMAPI_MoleAssets')
            Add-PreferredSmokeModId @('Local.DTMAPI_DreckoAssets')
        }
        'CoreAnimalVoice' {
            Add-PreferredSmokeModId @('Local.DTMAPI_HatchAssets')
        }
        'CoreAutoFishing' {
            Add-PreferredSmokeModId @('Local.Yuuka_DTMAPI_AutoFishing','Workshop.3743799721')
        }
        'Published11' {
            foreach ($id in @(Get-SmokePublishedProductWorkshopIds)) {
                [void]$enabledIds.Add($id)
                [void]$targetIds.Add($id)
            }
        }
        'Local11' {
            foreach ($id in @(Get-SmokePublishedProductLocalIds)) {
                [void]$enabledIds.Add($id)
                [void]$targetIds.Add($id)
            }
        }
        'FullKnown' {
            foreach ($id in @($targetIds)) {
                [void]$enabledIds.Add($id)
            }
        }
    }
    foreach ($id in $normalizedExtraEnabledIds) {
        [void]$enabledIds.Add($id)
        [void]$targetIds.Add($id)
    }

    $backupPath = Join-Path $EvidencePath 'official-mod-profile.mod_infos.before.json'
    Copy-Item -Force -LiteralPath $enablementPath -Destination $backupPath
    $maxPriority = -1
    foreach ($property in @($data.modInfos.PSObject.Properties)) {
        $entry = $property.Value
        if ($entry -and $entry.PSObject.Properties['enabled'] -and [bool]$entry.enabled -and $entry.PSObject.Properties['priority']) {
            try {
                $maxPriority = [Math]::Max($maxPriority, [int]$entry.priority)
            }
            catch {
            }
        }
    }

    $enabledApplied = New-Object 'System.Collections.Generic.List[string]'
    $disabledApplied = New-Object 'System.Collections.Generic.List[string]'
    foreach ($property in @($data.modInfos.PSObject.Properties)) {
        if (-not $targetIds.Contains($property.Name)) {
            continue
        }

        if ($enabledIds.Contains($property.Name)) {
            $currentPriority = -1
            if ($property.Value -and $property.Value.PSObject.Properties['priority']) {
                try {
                    $currentPriority = [int]$property.Value.priority
                }
                catch {
                    $currentPriority = -1
                }
            }
            if ($currentPriority -lt 0) {
                $maxPriority++
                $currentPriority = $maxPriority
            }
            Set-SmokeModInfoEnabled -Entry $property.Value -Enabled $true -Priority $currentPriority
            $enabledApplied.Add($property.Name) | Out-Null
        }
        else {
            Set-SmokeModInfoEnabled -Entry $property.Value -Enabled $false -Priority -1
            $disabledApplied.Add($property.Name) | Out-Null
        }
    }

    try {
        Write-SmokeJsonObject -Path $enablementPath -Value $data
        Copy-Item -Force -LiteralPath $enablementPath -Destination (Join-Path $EvidencePath 'official-mod-profile.mod_infos.applied.json')
        $summary = [ordered]@{
            Profile = $Profile
            Applied = $true
            Reason = 'Temporary smoke feature profile applied; run-game-smoke restores the original mod_infos.json during cleanup.'
            EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
            BackupPath = [System.IO.Path]::GetFullPath($backupPath)
            EnabledIds = @($enabledApplied.ToArray())
            DisabledIds = @($disabledApplied.ToArray())
            ExtraEnabledIds = @($normalizedExtraEnabledIds)
            RequiredEnabledIds = @($requiredEnabledIds)
            MissingExtraEnabledIds = @()
            IsolateAllOfficialMods = $IsolateAll
        }
        Write-SmokeJsonObject -Path $summaryPath -Value $summary
        return $summary
    }
    catch {
        $applyError = $_
        try {
            [void](Restore-SmokeOfficialModProfile -Summary ([ordered]@{
                Profile = $Profile
                Applied = $true
                EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
                BackupPath = [System.IO.Path]::GetFullPath($backupPath)
            }) -EvidencePath $EvidencePath -Phase 'apply')
        }
        catch {
            $restoreError = $_
            try {
                Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-failure.json') -Value ([ordered]@{
                    Phase = 'apply'
                    Profile = $Profile
                    EnablementPath = [System.IO.Path]::GetFullPath($enablementPath)
                    BackupPath = [System.IO.Path]::GetFullPath($backupPath)
                    ApplyError = [string]$applyError.Exception.Message
                    RestoreError = [string]$restoreError.Exception.Message
                })
            }
            catch {
            }
            throw "Official Mod smoke profile apply failed and its backup could not be restored. applyError=$($applyError.Exception.Message); restoreError=$($restoreError.Exception.Message)"
        }
        throw $applyError
    }
}



function Restore-SmokeOfficialModProfile {
    param(
        [Parameter(Mandatory = $true)] $Summary,
        [Parameter(Mandatory = $true)] [string] $EvidencePath,
        [Parameter(Mandatory = $true)] [string] $Phase
    )

    if (-not [bool]$Summary.Applied) {
        return $true
    }

    $backupPath = [string]$Summary.BackupPath
    $enablementPath = [string]$Summary.EnablementPath
    if ([string]::IsNullOrWhiteSpace($backupPath) -or -not (Test-Path -LiteralPath $backupPath -PathType Leaf)) {
        try {
            Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-failure.json') -Value ([ordered]@{
                Phase = $Phase
                Profile = [string]$Summary.Profile
                EnablementPath = $enablementPath
                BackupPath = $backupPath
                RestoreError = 'The required mod_infos.json backup is missing.'
            })
        }
        catch {
        }
        throw "Official Mod smoke profile backup is missing during $Phase restoration: $backupPath"
    }

    try {
        $expectedLength = [int64](Get-Item -LiteralPath $backupPath).Length
        $expectedHash = Get-SmokeFileSha256 -Path $backupPath
        Copy-Item -Force -LiteralPath $backupPath -Destination $enablementPath
        $actualLength = [int64](Get-Item -LiteralPath $enablementPath).Length
        $actualHash = Get-SmokeFileSha256 -Path $enablementPath
        $passed = $actualLength -eq $expectedLength -and
            [string]::Equals($actualHash, $expectedHash, [System.StringComparison]::OrdinalIgnoreCase)
        Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-verification.json') -Value ([ordered]@{
            Phase = $Phase
            Profile = [string]$Summary.Profile
            EnablementPath = $enablementPath
            BackupPath = $backupPath
            ExpectedLength = $expectedLength
            ActualLength = $actualLength
            ExpectedSha256 = $expectedHash
            ActualSha256 = $actualHash
            Passed = $passed
        })
        if (-not $passed) {
            throw 'The restored mod_infos.json does not match its byte-for-byte backup.'
        }
        return $true
    }
    catch {
        $restoreError = $_
        try {
            Write-SmokeJsonObject -Path (Join-Path $EvidencePath 'official-mod-profile-restore-failure.json') -Value ([ordered]@{
                Phase = $Phase
                Profile = [string]$Summary.Profile
                EnablementPath = $enablementPath
                BackupPath = $backupPath
                RestoreError = [string]$restoreError.Exception.Message
            })
        }
        catch {
        }
        throw "Official Mod smoke profile restoration failed during $Phase. restoreError=$($restoreError.Exception.Message)"
    }
}
