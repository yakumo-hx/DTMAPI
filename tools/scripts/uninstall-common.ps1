# Shared cleanup behavior for the portable PowerShell entry. No release/payload dependency.
function New-DtmCleanup {
    param([string] $GameDir, [string] $PolicyPath, $Log)
    return [pscustomobject]@{
        Game = [IO.Path]::GetFullPath($GameDir).TrimEnd('\','/'); Policy = (Get-Content -LiteralPath $PolicyPath -Raw -Encoding UTF8 | ConvertFrom-Json)
        Log = $Log; Targets = (New-Object 'System.Collections.Generic.List[object]'); Transactions = (New-Object 'System.Collections.Generic.List[object]')
        Roots = (New-Object 'System.Collections.Generic.List[string]'); Equipment = (New-Object 'System.Collections.Generic.List[string]')
        Problems = (New-Object 'System.Collections.Generic.List[string]'); Restored = (New-Object 'System.Collections.Generic.List[string]'); Removed = 0
    }
}

function Get-DtmCleanupChild {
    param([string] $Root, [string] $Relative)
    if ([string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.?([/\\]|$)' -or $Relative.Contains(':')) { throw "Unsafe relative cleanup path: $Relative" }
    $path = [IO.Path]::GetFullPath((Join-Path $Root $Relative))
    $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Cleanup path escaped root: $path" }
    return $path
}

function Get-DtmCleanupItem {
    param([string] $Path)
    try { Get-Item -LiteralPath $Path -Force -ErrorAction Stop }
    catch {
        if ($_.CategoryInfo.Category -ne [Management.Automation.ErrorCategory]::ObjectNotFound) { throw }
    }
}

function Assert-DtmCleanupPath {
    param([string] $Root, [string] $Path)
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    $pathFull = [IO.Path]::GetFullPath($Path).TrimEnd('\','/')
    if ($pathFull -ine $rootFull -and -not $pathFull.StartsWith($rootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "Cleanup path escaped root: $pathFull" }
    $current = $pathFull
    while ($current) {
        $item = Get-DtmCleanupItem $current
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Linked path preserved: $current" }
        $current = Split-Path -Parent $current
    }
}

function Add-DtmCleanupProblem {
    param($Cleanup, [string] $Path, [string] $Message)
    $detail = "$Path | $Message"
    $Cleanup.Problems.Add($detail)
    Write-DtmApiOperationLog -Log $Cleanup.Log -Message "REMAINING $detail"
}

function Add-DtmCleanupTarget {
    param($Cleanup, [string] $Root, [string] $Relative)
    $path = Get-DtmCleanupChild $Root $Relative
    try { if ($null -ne (Get-DtmCleanupItem $path)) { $Cleanup.Targets.Add([pscustomobject]@{Root=$Root; Path=$path}) } }
    catch { Add-DtmCleanupProblem $Cleanup $path $_.Exception.Message }
}

function Test-DtmCleanupSharedFile {
    param($Cleanup, [string] $Path, [string] $Relative)
    Assert-DtmCleanupPath (Split-Path -Parent $Path) $Path
    $item = Get-DtmCleanupItem $Path
    if ($null -eq $item -or $item.PSIsContainer -or $item.Length -gt 4MB) { return $false }
    $pattern = [string]$Cleanup.Policy.SharedFiles.PSObject.Properties[$Relative].Value
    $bytes = [IO.File]::ReadAllBytes($Path)
    $text = [Text.Encoding]::UTF8.GetString($bytes)
    if ($Relative -eq 'winhttp.dll') { $text += "`n" + [Text.Encoding]::Unicode.GetString($bytes) }
    if (-not [regex]::IsMatch($text, $pattern)) { return $false }
    if ($Relative -in @('.doorstop_version','winhttp.dll')) {
        $config = Get-DtmCleanupChild $Cleanup.Game 'doorstop_config.ini'
        if (Test-Path -LiteralPath $config -PathType Leaf) { return Test-DtmCleanupSharedFile $Cleanup $config 'doorstop_config.ini' }
        return $Relative -eq 'winhttp.dll' -and ((Test-Path -LiteralPath (Get-DtmCleanupChild $Cleanup.Game 'BepInEx/core/BepInEx.dll')) -or (Test-Path -LiteralPath (Get-DtmCleanupChild $Cleanup.Game 'BepInEx/plugins/DTMAPI')))
    }
    return $true
}

function Test-DtmCleanupOwnedRelative {
    param($Cleanup, [string] $Relative)
    [void](Get-DtmCleanupChild $Cleanup.Game $Relative)
    $relativePath = $Relative.Replace('\','/')
    $files = @($Cleanup.Policy.GameFiles) + @($Cleanup.Policy.CoreFiles | ForEach-Object { "BepInEx/core/$_" }) + @($Cleanup.Policy.RuntimeFiles | ForEach-Object { "DTMAPI/$_" })
    if ($files -contains $relativePath) { return $true }
    foreach ($directory in (@($Cleanup.Policy.GameDirectories) + @($Cleanup.Policy.RuntimeDirectories | ForEach-Object { "DTMAPI/$_" }))) {
        if ($relativePath -ieq $directory -or $relativePath.StartsWith($directory + '/', [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

function Get-DtmCleanupTreeFiles {
    param([string] $Root, [string] $Path)
    Assert-DtmCleanupPath $Root $Path
    foreach ($item in @(Get-ChildItem -LiteralPath $Path -Force)) {
        Assert-DtmCleanupPath $Root $item.FullName
        if ($item.PSIsContainer) { Get-DtmCleanupTreeFiles $Root $item.FullName }
        else { $item.FullName }
    }
}

function Test-DtmCleanupEquipmentPath {
    param([string] $Path)
    return [IO.Path]::GetFileName($Path).StartsWith('equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json', [StringComparison]::OrdinalIgnoreCase) -or
        ($Path.Replace('\','/') -match '/equipment-slots/' -and $Path.EndsWith('.flat.json', [StringComparison]::OrdinalIgnoreCase))
}

function Find-DtmCleanupEquipment {
    param($Cleanup, [string] $Root, [string] $Path)
    foreach ($file in @(Get-DtmCleanupTreeFiles $Root $Path)) {
        if (Test-DtmCleanupEquipmentPath $file) { $Cleanup.Equipment.Add($file) }
    }
}

function Add-DtmCleanupPsState {
    param($Cleanup, [string] $Root, [string] $Path)
    try {
        Assert-DtmCleanupPath $Root $Path
        if ((Split-Path -Leaf $Path) -notmatch '^\.runtime-install-transaction-\d{8}-\d{6}-\d{3}-[0-9a-fA-F]{8}$') { throw 'Unknown legacy transaction name.' }
        foreach ($entry in @(Get-ChildItem -LiteralPath $Path -Force)) {
            if ($entry.Name -notin @('candidate','recovery') -or -not $entry.PSIsContainer) { throw "Unknown legacy transaction content: $($entry.FullName)" }
            foreach ($child in @(Get-ChildItem -LiteralPath $entry.FullName -Force)) {
                if ($child.Name -notin @('tools','components','release-manifest.json','install-state.json')) { throw "Unknown legacy state content: $($child.FullName)" }
            }
        }
        [void]@(Get-DtmCleanupTreeFiles $Root $Path)
        $Cleanup.Transactions.Add([pscustomobject]@{Root=$Root;Path=$Path;Originals=@()})
    } catch { Add-DtmCleanupProblem $Cleanup $Path $_.Exception.Message }
}

function Add-DtmCleanupTransaction {
    param($Cleanup, [string] $Root, [string] $Path)
    try {
        Assert-DtmCleanupPath $Root $Path
        $files = @(Get-DtmCleanupTreeFiles $Root $Path)
        if ($Root -ieq $Cleanup.Game -and (Split-Path -Leaf $Path) -notmatch '^\.dtmapi-(?:(?:runtime|multiplatform)-install-\d{8}-\d{6}-\d{3}-[0-9a-fA-F]{8}|multiplatform-genesis-[0-9a-fA-F]{32})$') { throw 'Unknown transaction directory name.' }
        $receiptPath = Get-DtmCleanupChild $Path 'transaction.json'
        $originals = New-Object 'System.Collections.Generic.List[object]'
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
            if (@(Get-ChildItem -LiteralPath $Path -Force | Where-Object { $_.PSIsContainer -or $_.Name -notmatch '^transaction\.json\.(tmp|bak)-[0-9a-fA-F]{32}$' }).Count -ne 0) { throw 'Unmapped transaction content preserved (receipt missing).' }
        } else {
            $receipt = Get-Content -LiteralPath $receiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($receipt.SchemaVersion -ne 1) { throw 'Unknown transaction schema.' }
            if ($null -ne $receipt.PSObject.Properties['Engine']) {
                if ($receipt.Engine -notin @('DTMAPI.MultiPlatform/install','DTMAPI.MultiPlatform/bepinex','DTMAPI.MultiPlatform/uninstall')) { throw 'Unknown transaction engine.' }
                $allowed = New-Object 'System.Collections.Generic.List[string]'
                $allowed.Add('transaction.json')
                if ($receipt.Engine -eq 'DTMAPI.MultiPlatform/uninstall') { $allowed.Add('uninstall-state.json') }
                $foreignDoorstop = $false
                foreach ($configOperation in @($receipt.Operations | Where-Object TargetRelativePath -EQ 'doorstop_config.ini')) {
                    $backupConfig = Get-DtmCleanupChild $Path ([string]$configOperation.BackupRelativePath)
                    if ((Test-Path -LiteralPath $backupConfig -PathType Leaf) -and -not (Test-DtmCleanupSharedFile $Cleanup $backupConfig 'doorstop_config.ini')) { $foreignDoorstop = $true }
                }
                foreach ($op in $receipt.Operations) {
                    $relative = ([string]$op.TargetRelativePath).Replace('\','/')
                    [void](Get-DtmCleanupChild $Cleanup.Game $relative)
                    $shared = $null -ne $Cleanup.Policy.SharedFiles.PSObject.Properties[$relative]
                    if (-not $shared -and -not (Test-DtmCleanupOwnedRelative $Cleanup $relative)) { throw "Unknown transaction target: $relative" }
                    $backup = ([string]$op.BackupRelativePath).Replace('\','/')
                    $candidate = ([string]$op.CandidateRelativePath).Replace('\','/')
                    if ($backup -notmatch '^backup/[0-9]{4,}\.bin$' -or ($candidate -and $candidate -notmatch '^candidate/[0-9]{4,}\.bin$')) { throw 'Unknown transaction mapping.' }
                    $allowed.Add($backup)
                    if ($candidate) { $allowed.Add($candidate) }
                    $source = Get-DtmCleanupChild $Path $backup
                    if (Test-DtmCleanupEquipmentPath $relative) {
                        if (Test-Path -LiteralPath $source -PathType Leaf) { $Cleanup.Equipment.Add("$source -> $relative") }
                        if ($candidate -and (Test-Path -LiteralPath (Get-DtmCleanupChild $Path $candidate) -PathType Leaf)) { $Cleanup.Equipment.Add("$Path/$candidate -> $relative") }
                    }
                    if ($shared -and (Test-Path -LiteralPath $source -PathType Leaf) -and (($foreignDoorstop -and $relative -in @('winhttp.dll','.doorstop_version')) -or -not (Test-DtmCleanupSharedFile $Cleanup $source $relative))) { $originals.Add([pscustomobject]@{Source=$source;Target=(Get-DtmCleanupChild $Cleanup.Game $relative)}) }
                }
                foreach ($file in $files) {
                    $relative = $file.Substring($Path.Length).TrimStart('\','/').Replace('\','/')
                    if ($relative -notin $allowed -and $relative -notmatch '^transaction\.json\.(tmp|bak)-[0-9a-fA-F]{32}$') { throw "Unmapped transaction file: $relative" }
                }
            } else {
                if ($null -eq $receipt.PSObject.Properties['CandidatePlugin'] -or $null -eq $receipt.PSObject.Properties['RecoveryPlugin']) { throw 'Unknown legacy transaction receipt.' }
                foreach ($entry in @(Get-ChildItem -LiteralPath $Path -Force)) {
                    if ($entry.Name -notin @('candidate-plugin','recovery-plugin','transaction.json')) { throw "Unknown legacy transaction content: $($entry.FullName)" }
                }
            }
        }
        $Cleanup.Transactions.Add([pscustomobject]@{Root=$Root;Path=$Path;Originals=$originals.ToArray()})
    } catch { Add-DtmCleanupProblem $Cleanup $Path $_.Exception.Message }
}

function Add-DtmCleanupBackup {
    param($Cleanup, [string] $Root, [string] $Path)
    try {
        Assert-DtmCleanupPath $Root $Path
        if (Test-Path -LiteralPath (Get-DtmCleanupChild $Path 'transaction.json') -PathType Leaf) { Add-DtmCleanupTransaction $Cleanup $Root $Path; return }
        $name = Split-Path -Leaf $Path
        if ($name -notmatch '^(bepinex-install|uninstall)-') { throw 'Backup is not an owned framework backup; preserved.' }
        $originals = New-Object 'System.Collections.Generic.List[object]'
        $backupConfig = Get-DtmCleanupChild $Path 'doorstop_config.ini'
        $foreignDoorstop = (Test-Path -LiteralPath $backupConfig -PathType Leaf) -and -not (Test-DtmCleanupSharedFile $Cleanup $backupConfig 'doorstop_config.ini')
        foreach ($file in @(Get-DtmCleanupTreeFiles $Root $Path)) {
            $relative = $file.Substring($Path.Length).TrimStart('\','/').Replace('\','/')
            if ($relative -eq 'install-summary.txt' -and $name.StartsWith('bepinex-install-')) { continue }
            if ($null -ne $Cleanup.Policy.SharedFiles.PSObject.Properties[$relative]) {
                if (($foreignDoorstop -and $relative -in @('winhttp.dll','.doorstop_version')) -or -not (Test-DtmCleanupSharedFile $Cleanup $file $relative)) { $originals.Add([pscustomobject]@{Source=$file;Target=(Get-DtmCleanupChild $Cleanup.Game $relative)}) }
            } elseif (-not (Test-DtmCleanupOwnedRelative $Cleanup $relative)) { throw "Unmapped backup file preserved: $relative" }
        }
        Find-DtmCleanupEquipment $Cleanup $Root $Path
        $Cleanup.Transactions.Add([pscustomobject]@{Root=$Root;Path=$Path;Originals=$originals.ToArray()})
    } catch { Add-DtmCleanupProblem $Cleanup $Path $_.Exception.Message }
}

function Find-DtmCleanupTargets {
    param($Cleanup)
    $defaultRoot = Get-DtmCleanupChild $Cleanup.Game 'DTMAPI'
    $Cleanup.Roots.Add($defaultRoot)
    $custom = if ($env:DTMAPI_RUNTIME_DIR) { $env:DTMAPI_RUNTIME_DIR } else { $env:DTMAPI_STATE_DIR }
    if ($custom) {
        try {
            $custom = [IO.Path]::GetFullPath($custom).TrimEnd('\','/')
            if ($custom -ine $defaultRoot) {
                if ($custom -ieq [IO.Path]::GetPathRoot($custom).TrimEnd('\','/') -or (Test-DtmApiPathIsSameOrChild -Child $Cleanup.Game -Parent $custom)) { throw 'Custom Runtime directory contains the game directory or is a drive root.' }
                Assert-DtmCleanupPath $custom $custom
                $state = Get-Content -LiteralPath (Get-DtmCleanupChild $custom 'install-state.json') -Raw -Encoding UTF8 | ConvertFrom-Json
                if ([IO.Path]::GetFullPath([string]$state.GameDir).TrimEnd('\','/') -ine $Cleanup.Game) { throw 'Custom Runtime directory has no ownership record for the selected game.' }
                $Cleanup.Roots.Add($custom)
            }
        } catch { Add-DtmCleanupProblem $Cleanup $custom $_.Exception.Message }
    }
    foreach ($root in $Cleanup.Roots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        try {
            Assert-DtmCleanupPath $root $root
            # Resolve the data-loss question before registering a recursive config deletion.
            $config = Get-DtmCleanupChild $root 'config'
            if (Test-Path -LiteralPath $config -PathType Container) { Find-DtmCleanupEquipment $Cleanup $root $config }
            foreach ($relative in (@($Cleanup.Policy.RuntimeDirectories) + @($Cleanup.Policy.RuntimeFiles))) { Add-DtmCleanupTarget $Cleanup $root $relative }
            foreach ($pattern in $Cleanup.Policy.RuntimeFilePatterns) {
                foreach ($file in @(Get-ChildItem -LiteralPath $root -Filter $pattern -File -Force)) { $Cleanup.Targets.Add([pscustomobject]@{Root=$root;Path=$file.FullName}) }
            }
            foreach ($scratch in @(Get-ChildItem -LiteralPath $root -Force)) {
                if ($scratch.Name -match $Cleanup.Policy.RuntimeScratchPattern) { $Cleanup.Targets.Add([pscustomobject]@{Root=$root;Path=$scratch.FullName}) }
            }
            $backups = Get-DtmCleanupChild $root 'backups'
            if (Test-Path -LiteralPath $backups -PathType Container) {
                Assert-DtmCleanupPath $root $backups
                foreach ($backup in @(Get-ChildItem -LiteralPath $backups -Force)) { Add-DtmCleanupBackup $Cleanup $root $backup.FullName }
            }
            foreach ($txn in @(Get-ChildItem -LiteralPath $root -Filter '.runtime-install-transaction-*' -Force)) { Add-DtmCleanupPsState $Cleanup $root $txn.FullName }
        } catch { Add-DtmCleanupProblem $Cleanup $root $_.Exception.Message }
    }
    foreach ($relative in (@($Cleanup.Policy.GameDirectories) + @($Cleanup.Policy.GameFiles) + @($Cleanup.Policy.CoreFiles | ForEach-Object { "BepInEx/core/$_" }))) { Add-DtmCleanupTarget $Cleanup $Cleanup.Game $relative }
    foreach ($shared in $Cleanup.Policy.SharedFiles.PSObject.Properties) {
        $path = Get-DtmCleanupChild $Cleanup.Game $shared.Name
        try {
            if ($null -eq (Get-DtmCleanupItem $path)) { continue }
            if (Test-DtmCleanupSharedFile $Cleanup $path $shared.Name) { $Cleanup.Targets.Add([pscustomobject]@{Root=$Cleanup.Game;Path=$path}) }
            else { Add-DtmCleanupProblem $Cleanup $path 'Unrecognized shared file preserved; export logs before replacing it.' }
        } catch { Add-DtmCleanupProblem $Cleanup $path $_.Exception.Message }
    }
    foreach ($entry in @(Get-ChildItem -LiteralPath $Cleanup.Game -Force)) {
        if ($entry.Name -match $Cleanup.Policy.GameScratchPattern) { $Cleanup.Targets.Add([pscustomobject]@{Root=$Cleanup.Game;Path=$entry.FullName}) }
        if ($entry.Name -match '^\.dtmapi-(runtime-install|multiplatform-install|multiplatform-genesis)-') { Add-DtmCleanupTransaction $Cleanup $Cleanup.Game $entry.FullName }
    }
}

function Remove-DtmCleanupEntry {
    param($Cleanup, [string] $Root, [string] $Path)
    $item = Get-DtmCleanupItem $Path
    if ($null -eq $item) { return }
    Assert-DtmCleanupPath $Root $Path
    if ($item.PSIsContainer) {
        foreach ($child in @(Get-ChildItem -LiteralPath $Path -Force | Where-Object Name -NE 'transaction.json')) {
            try { Remove-DtmCleanupEntry $Cleanup $Root $child.FullName }
            catch { Add-DtmCleanupProblem $Cleanup $child.FullName ("$($_.Exception.Message) (0x{0:X8})" -f $_.Exception.HResult) }
        }
        if (@(Get-ChildItem -LiteralPath $Path -Force | Where-Object Name -NE 'transaction.json').Count -gt 0) { return }
        $receipt = Join-Path $Path 'transaction.json'
        if (Test-Path -LiteralPath $receipt -PathType Leaf) { Remove-DtmCleanupEntry $Cleanup $Root $receipt }
        [IO.Directory]::Delete($Path)
    } else { Remove-Item -LiteralPath $Path -Force }
    $Cleanup.Removed++
    Write-DtmApiOperationLog -Log $Cleanup.Log -Message "REMOVED $Path"
}

function Remove-DtmCleanupEmptyDirectory {
    param($Cleanup, [string] $Root, [string] $Path)
    try {
        if (-not (Test-Path -LiteralPath $Path -PathType Container)) { return }
        Assert-DtmCleanupPath $Root $Path
        if (@(Get-ChildItem -LiteralPath $Path -Force).Count -eq 0) { [IO.Directory]::Delete($Path) }
    } catch { Add-DtmCleanupProblem $Cleanup $Path $_.Exception.Message }
}

function Invoke-DtmCleanup {
    param($Cleanup)
    foreach ($txn in $Cleanup.Transactions) {
        try {
            foreach ($original in @($txn.Originals | Sort-Object { if ((Split-Path -Leaf $_.Target) -eq 'doorstop_config.ini') { 1 } else { 0 } })) {
                Assert-DtmCleanupPath $txn.Root $original.Source
                Assert-DtmCleanupPath $Cleanup.Game $original.Target
                if ((Test-Path -LiteralPath $original.Target -PathType Leaf) -and -not (Test-DtmCleanupSharedFile $Cleanup $original.Target (Split-Path -Leaf $original.Target))) {
                    if ((Get-Item -LiteralPath $original.Target).Length -gt 4MB -or (Get-Item -LiteralPath $original.Source).Length -gt 4MB -or
                        [Convert]::ToBase64String([IO.File]::ReadAllBytes($original.Target)) -cne [Convert]::ToBase64String([IO.File]::ReadAllBytes($original.Source))) { throw "Original conflicts with a current non-framework file; both copies preserved: $($original.Target)" }
                }
                [IO.File]::Copy($original.Source, $original.Target, $true)
                $Cleanup.Restored.Add($original.Target)
                foreach ($problem in @($Cleanup.Problems.ToArray())) { if ($problem.StartsWith($original.Target + ' | Unrecognized shared file', [StringComparison]::OrdinalIgnoreCase)) { [void]$Cleanup.Problems.Remove($problem) } }
                Write-DtmApiOperationLog -Log $Cleanup.Log -Message "RESTORED $($original.Target)"
            }
            Remove-DtmCleanupEntry $Cleanup $txn.Root $txn.Path
        } catch { Add-DtmCleanupProblem $Cleanup $txn.Path $_.Exception.Message }
    }
    foreach ($target in $Cleanup.Targets) {
        if ($Cleanup.Restored -contains $target.Path) { continue }
        if ((Split-Path -Leaf $target.Path) -eq 'install-state.json' -and @($Cleanup.Problems | Where-Object { $_.StartsWith($target.Root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) { continue }
        try { Remove-DtmCleanupEntry $Cleanup $target.Root $target.Path }
        catch { Add-DtmCleanupProblem $Cleanup $target.Path ("$($_.Exception.Message) (0x{0:X8})" -f $_.Exception.HResult) }
    }
    foreach ($relative in $Cleanup.Policy.EmptyDirectories) { Remove-DtmCleanupEmptyDirectory $Cleanup $Cleanup.Game (Get-DtmCleanupChild $Cleanup.Game $relative) }
    foreach ($root in $Cleanup.Roots) {
        foreach ($relative in @('cache','backups')) { Remove-DtmCleanupEmptyDirectory $Cleanup $root (Get-DtmCleanupChild $root $relative) }
        Remove-DtmCleanupEmptyDirectory $Cleanup $root $root
        if ((Test-Path -LiteralPath $root -PathType Container) -and @($Cleanup.Problems | Where-Object { $_.StartsWith($root,[StringComparison]::OrdinalIgnoreCase) }).Count -eq 0) { Add-DtmCleanupProblem $Cleanup $root 'Unrecognized remaining content preserved; export logs for review.' }
    }
}
