Set-StrictMode -Version Latest

function Read-ReverseJsonItems {
    param([string] $Path)
    # Windows PowerShell 5.1 writes a JSON array as one pipeline object.
    # Enumerate the parsed value explicitly so one and many rows share a shape.
    $value = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    foreach ($item in $value) { $item }
}

function Get-ReverseValue {
    param($Object, [string] $Name, $Default = $null)
    if ($null -eq $Object) { return $Default }
    if ($Object -is [System.Collections.IDictionary]) {
        if ($Object.Contains($Name)) { return $Object[$Name] }
    }
    elseif ($Object.PSObject.Properties.Name -contains $Name) { return $Object.$Name }
    return $Default
}

function Get-ReverseDigest {
    param([Parameter(Mandatory = $true)] $Value)
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($json)))).Replace('-', '') }
    finally { $algorithm.Dispose() }
}

function Write-ReverseJson {
    param([string] $Path, $Value)
    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    $temporary = $Path + '.writing'
    [IO.File]::WriteAllText($temporary, ((ConvertTo-Json -InputObject $Value -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporary -Destination $Path -Force
}

function Resolve-ReverseChild {
    param([string] $Root, [string] $RelativePath)
    if ([IO.Path]::IsPathRooted($RelativePath)) { throw "Expected a relative stage path: $RelativePath" }
    $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $result = [IO.Path]::GetFullPath((Join-Path $Root $RelativePath))
    if (-not $result.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Stage path escapes root: $RelativePath" }
    $cursor = $result
    while ($cursor -and $cursor.Length -ge $prefix.TrimEnd('\', '/').Length) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Stage path traverses a reparse point: $cursor" }
        }
        $cursor = Split-Path -Parent $cursor
    }
    return $result
}

function Get-ReverseInventory {
    param([string] $Root)
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { throw "Stage output is missing: $Root" }
    $files = @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force | Sort-Object FullName)
    return @($files | ForEach-Object {
        $relative = $_.FullName.Substring($rootFull.Length).Replace('\', '/')
        $checkedPath = Resolve-ReverseChild -Root $Root -RelativePath $relative
        [ordered]@{ path = $relative; bytes = [long]$_.Length; sha256 = (Get-FileHash -LiteralPath $checkedPath -Algorithm SHA256).Hash.ToUpperInvariant() }
    })
}

function Test-ReverseInventory {
    param([string] $Root, [AllowEmptyCollection()][object[]] $Inventory, [switch] $AllowExtra)
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return $false }
    if ($Inventory.Count -eq 0) { return $false }
    if (-not $AllowExtra) {
        $actual = @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force)
        if ($actual.Count -ne $Inventory.Count) { return $false }
    }
    $seen = @{}
    foreach ($entry in $Inventory) {
        $path = Resolve-ReverseChild -Root $Root -RelativePath $entry.path
        if ($seen.ContainsKey($entry.path)) { return $false }
        $seen[$entry.path] = $true
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $false }
        if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes) { return $false }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { return $false }
    }
    return $true
}

function Get-ReverseStagePath {
    param([string] $BuildRoot, [string] $Stage)
    if ($Stage -notmatch '^[A-Za-z0-9._-]+$') { throw "Invalid stage name: $Stage" }
    return Resolve-ReverseChild -Root $BuildRoot -RelativePath ("stage-receipts/$Stage.json")
}

function Get-ReverseStageReceipt {
    param([string] $BuildRoot, [string] $Stage)
    $path = Get-ReverseStagePath -BuildRoot $BuildRoot -Stage $Stage
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    try { return Get-Content -Raw -LiteralPath $path | ConvertFrom-Json }
    catch { return $null }
}

function Test-ReverseStage {
    param([string] $BuildRoot, [string] $Stage, $Inputs, [string] $OutputRoot)
    $receipt = Get-ReverseStageReceipt -BuildRoot $BuildRoot -Stage $Stage
    if ((Get-ReverseValue $receipt 'status') -ne 'complete') { return $null }
    if ((Get-ReverseValue $receipt 'inputFingerprint') -cne (Get-ReverseDigest $Inputs)) { return $null }
    $inventory = @(Get-ReverseValue $receipt 'outputs' @())
    if ((Get-ReverseValue $receipt 'outputFingerprint') -cne (Get-ReverseDigest $inventory)) { return $null }
    if (-not (Test-ReverseInventory -Root $OutputRoot -Inventory $inventory -AllowExtra:((Get-ReverseValue $receipt 'outputSet') -eq 'listed-files'))) { return $null }
    return $receipt
}

function Complete-ReverseStage {
    param([string] $BuildRoot, [string] $Stage, $Inputs, [string] $OutputRoot, [AllowEmptyCollection()][object[]] $Inventory, [string] $Origin = 'executed', [switch] $AllowExtra)
    if (-not $PSBoundParameters.ContainsKey('Inventory')) { $Inventory = @(Get-ReverseInventory -Root $OutputRoot) }
    if ($Inventory.Count -eq 0) { throw "Stage cannot complete with empty output: $Stage" }
    $receipt = [ordered]@{
        schemaVersion = 1; stage = $Stage; status = 'complete'; completedAtUtc = [DateTime]::UtcNow.ToString('o')
        inputs = $Inputs; inputFingerprint = Get-ReverseDigest $Inputs
        outputRoot = $OutputRoot; outputs = @($Inventory); outputFingerprint = Get-ReverseDigest @($Inventory); origin = $Origin
        outputSet = $(if ($AllowExtra) { 'listed-files' } else { 'complete-tree' })
    }
    Write-ReverseJson -Path (Get-ReverseStagePath $BuildRoot $Stage) -Value $receipt
    return $receipt
}

function Start-ReverseStage {
    param([string] $BuildRoot, [string] $Stage, $Inputs, [string] $OutputRelativePath)
    $output = Resolve-ReverseChild -Root $BuildRoot -RelativePath $OutputRelativePath
    if (Test-Path -LiteralPath $output) {
        $preserved = Resolve-ReverseChild -Root $BuildRoot -RelativePath ('.stage-failures/' + $Stage + '-' + [Guid]::NewGuid().ToString('N'))
        [IO.Directory]::CreateDirectory((Split-Path -Parent $preserved)) | Out-Null
        Move-Item -LiteralPath $output -Destination $preserved
        Write-Host "Preserved incomplete or invalid $Stage output: $preserved"
    }
    Write-ReverseJson -Path (Get-ReverseStagePath $BuildRoot $Stage) -Value ([ordered]@{
        schemaVersion = 1; stage = $Stage; status = 'running'; startedAtUtc = [DateTime]::UtcNow.ToString('o')
        inputs = $Inputs; inputFingerprint = Get-ReverseDigest $Inputs; outputRoot = $output
    })
    return $output
}

function Fail-ReverseStage {
    param([string] $BuildRoot, [string] $Stage, [string] $Message)
    $receipt = Get-ReverseStageReceipt $BuildRoot $Stage
    if ($null -eq $receipt) { return }
    $receipt.status = 'failed'
    $receipt | Add-Member -NotePropertyName failure -NotePropertyValue $Message -Force
    Write-ReverseJson -Path (Get-ReverseStagePath $BuildRoot $Stage) -Value $receipt
}

function Get-ReverseCaptureStatus {
    param([Parameter(Mandatory = $true)][string] $BuildRoot)
    $full = [IO.Path]::GetFullPath($BuildRoot)
    $receipts = @()
    $receiptRoot = Join-Path $full 'stage-receipts'
    if (Test-Path -LiteralPath $receiptRoot -PathType Container) {
        $receipts = @(Get-ChildItem -LiteralPath $receiptRoot -Filter '*.json' -File | Sort-Object Name | ForEach-Object {
            try {
                $receipt = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json
                [ordered]@{ stage = $receipt.stage; status = $receipt.status; inputFingerprint = $receipt.inputFingerprint; outputFiles = @(Get-ReverseValue $receipt 'outputs' @()).Count; integrity = 'not-checked-by-status' }
            }
            catch { [ordered]@{ stage = $_.BaseName; status = 'unreadable-receipt'; integrity = 'not-checked-by-status' } }
        })
    }
    $legacy = $null
    $summaryPath = Join-Path $full 'full-baseline-inventory/summary.json'
    if (Test-Path -LiteralPath $summaryPath -PathType Leaf) {
        try {
            $summary = Get-Content -Raw -LiteralPath $summaryPath | ConvertFrom-Json
            $legacy = [ordered]@{ buildName = $summary.buildName; steamBuild = $summary.steamBuild; branch = $summary.branch; recordedAtUtc = $summary.generatedAtUtc; integrity = 'historical-record-only' }
        } catch { $legacy = [ordered]@{ status = 'unreadable-summary' } }
    }
    return [ordered]@{ schemaVersion = 1; buildRoot = $full; readOnly = $true; stages = $receipts; legacySummary = $legacy }
}

function Get-ReverseManagedDependencies {
    param([string] $AssemblyPath, [string] $ManagedRoot, [object[]] $SnapshotInventory)
    # Reflection-only assemblies cannot be unloaded from a Windows PowerShell
    # AppDomain. Keep only their metadata names, keyed by the already verified
    # snapshot SHA, across repeated entry-point calls in this process.
    if (-not (Get-Variable -Name DtmApiReverseReferenceNamesBySha256 -Scope Global -ErrorAction SilentlyContinue)) {
        $global:DtmApiReverseReferenceNamesBySha256 = @{}
        $global:DtmApiReverseReferenceHashByName = @{}
    }
    $identities = @{}
    foreach ($entry in $SnapshotInventory) {
        if ($entry.path -match '^DolocTown_Data/Managed/[^/]+\.dll$') { $identities[[IO.Path]::GetFileName($entry.path)] = $entry.sha256 }
    }
    $names = @{}
    $queue = [Collections.Generic.Queue[string]]::new()
    $queue.Enqueue($AssemblyPath)
    $mode = 'reflection-only-reference-closure'
    try {
        while ($queue.Count -gt 0) {
            $path = $queue.Dequeue()
            $name = [IO.Path]::GetFileName($path)
            if ($names.ContainsKey($name)) { continue }
            $names[$name] = $true
            $sha256 = $identities[$name]
            if ([string]::IsNullOrWhiteSpace($sha256)) { throw "Missing verified assembly identity: $name" }
            if (-not $global:DtmApiReverseReferenceNamesBySha256.ContainsKey($sha256)) {
                if ($global:DtmApiReverseReferenceHashByName.ContainsKey($name) -and $global:DtmApiReverseReferenceHashByName[$name] -ne $sha256) {
                    throw "A different same-name assembly was inspected in this process: $name"
                }
                $assembly = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($path))
                $references = @($assembly.GetReferencedAssemblies() | ForEach-Object { $_.Name })
                $global:DtmApiReverseReferenceNamesBySha256[$sha256] = $references
                $global:DtmApiReverseReferenceHashByName[$name] = $sha256
            }
            foreach ($reference in $global:DtmApiReverseReferenceNamesBySha256[$sha256]) {
                $dependency = Join-Path $ManagedRoot ($reference + '.dll')
                if (Test-Path -LiteralPath $dependency -PathType Leaf) { $queue.Enqueue($dependency) }
            }
        }
    }
    catch {
        $mode = 'all-managed-conservative-fallback'
        foreach ($entry in $SnapshotInventory) {
            if ($entry.path -match '^DolocTown_Data/Managed/[^/]+\.dll$') { $names[[IO.Path]::GetFileName($entry.path)] = $true }
        }
    }
    $rows = @($SnapshotInventory | Where-Object { $_.path -match '^DolocTown_Data/Managed/[^/]+\.dll$' -and $names.ContainsKey([IO.Path]::GetFileName($_.path)) } | Sort-Object path)
    if ($rows.Count -eq 0) { throw 'Managed input identity inventory is empty.' }
    return [ordered]@{ mode = $mode; files = $rows }
}
