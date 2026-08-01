<#
.SYNOPSIS
Runs a byte-bound smoke transaction against the exact eleven public Catalog products.

.DESCRIPTION
Validates the candidate package inventory, rejects reparse points and alternate data
streams, copies each package into same-volume staging outside MODS, and swaps only the
Catalog-mapped official folders with Directory.Move. The supplied smoke script runs in
a child PowerShell process. Tested candidate trees are then moved into the transaction
work root and every original tree is restored from its exact before-image in finally.

The work root is intentionally retained for audit/recovery; this helper never deletes or
overwrites a product tree. A normal smoke invocation must use the installed Runtime with
-UseSteam -SkipInstall -OfficialModProfile Local11 -IsolateAllOfficialMods
-AssertNoQaUiEvidence. After the child exits, the installed five Runtime assemblies and
installed release-manifest projection are compared to the same candidate Runtime package.

.EXAMPLE
.\tools\scripts\candidate11-source-transaction.ps1 `
    -CandidateRoot 'E:\candidate' `
    -SmokeScriptPath '.\tools\scripts\run-game-smoke.ps1' `
    -SmokeArgumentList @('-UseSteam','-SkipInstall','-SaveSlot','3','-OfficialModProfile','Local11','-IsolateAllOfficialMods','-AssertNoQaUiEvidence')
#>
param(
    [string] $CandidateRoot = '',
    [string] $CatalogPath = '',
    [string] $PersistentRoot = '',
    [string] $GameDir = '',
    [string] $EvidenceRoot = '',
    [string] $TransactionId = '',
    [string] $SmokeScriptPath = '',
    [string[]] $SmokeArgumentList = @(),
    [ValidateRange(0, 86400)]
    [int] $RuntimeLockTimeoutSeconds = 3600,
    [ValidateRange(0, 300)]
    [int] $PostSmokeProcessExitTimeoutSeconds = 30,
    [switch] $LibraryOnly,
    [switch] $TestOnlySkipRuntimeLock,
    [switch] $TestOnlySkipProcessCheck,
    [switch] $TestOnlySkipSmokeContract
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$script:Candidate11ProductCount = 11
$script:Candidate11DigestAlgorithm = 'DTMAPI-FileTree-SHA256-v1'
$script:Candidate11StructureDigestAlgorithm = 'DTMAPI-CandidateStructure-SHA256-v1'
$script:Candidate11RuntimeBindingDigestAlgorithm = 'DTMAPI-CandidateRuntimeBinding-SHA256-v1'
$script:Candidate11RuntimeAssemblyDigestAlgorithm = 'DTMAPI-CandidateRuntimeAssemblies-SHA256-v1'
$script:Candidate11RuntimeAssemblyNames = @(
    'DTMAPI.Abstractions.dll',
    'DTMAPI.BepInExBootstrap.dll',
    'DTMAPI.Core.dll',
    'DTMAPI.GameBridge.DolocTown.dll',
    'DTMAPI.ModConfigMenu.dll'
) | Sort-Object

function Get-Candidate11CanonicalPath {
    param([Parameter(Mandatory = $true)] [string] $Path)

    return [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
}

function Test-Candidate11SameOrChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    $childPath = Get-Candidate11CanonicalPath -Path $Child
    $parentPath = Get-Candidate11CanonicalPath -Path $Parent
    if ([string]::Equals($childPath, $parentPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }
    return $childPath.StartsWith(
        $parentPath + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-Candidate11Sha256Bytes {
    param([Parameter(Mandatory = $true)] [byte[]] $Bytes)

    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($hasher.ComputeHash($Bytes))).Replace('-', '')
    }
    finally {
        $hasher.Dispose()
    }
}

function ConvertTo-Candidate11JsonBytes {
    param([Parameter(Mandatory = $true)] $Value)

    $json = $Value | ConvertTo-Json -Depth 24
    return (New-Object System.Text.UTF8Encoding($false)).GetBytes($json + [Environment]::NewLine)
}

function Write-Candidate11NewBytes {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [byte[]] $Bytes
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullPath)) | Out-Null
    $stream = [System.IO.File]::Open(
        $fullPath,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)
    try {
        $stream.Write($Bytes, 0, $Bytes.Length)
        $stream.Flush()
    }
    finally {
        $stream.Dispose()
    }
}

function Write-Candidate11NewJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    Write-Candidate11NewBytes -Path $Path -Bytes (ConvertTo-Candidate11JsonBytes -Value $Value)
}

function Write-Candidate11NewText {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [AllowEmptyString()] [string] $Text
    )

    Write-Candidate11NewBytes -Path $Path -Bytes ((New-Object System.Text.UTF8Encoding($false)).GetBytes($Text))
}

function Get-Candidate11StreamProviderPath {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $fullPath = Get-Candidate11CanonicalPath -Path $Path
    if ($fullPath.StartsWith('\\?\', [System.StringComparison]::Ordinal)) {
        return $fullPath
    }
    if ($fullPath.StartsWith('\\', [System.StringComparison]::Ordinal)) {
        return '\\?\UNC\' + $fullPath.Substring(2)
    }
    return '\\?\' + $fullPath
}

function Assert-Candidate11SafeTree {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Context,
        [switch] $AllowAlternateDataStreams
    )

    $root = Get-Candidate11CanonicalPath -Path $Path
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "$Context does not exist as a directory: $root"
    }

    $items = @(
        Get-Item -LiteralPath $root -Force
        Get-ChildItem -LiteralPath $root -Recurse -Force -ErrorAction Stop
    )
    $reparse = @($items | Where-Object {
        ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
    })
    if ($reparse.Count -gt 0) {
        throw "$Context contains a forbidden reparse point: $($reparse[0].FullName)"
    }

    $getItemCommand = Get-Command Get-Item -ErrorAction Stop
    if (-not $getItemCommand.Parameters.ContainsKey('Stream')) {
        throw "$Context cannot be validated because this PowerShell host cannot enumerate NTFS alternate data streams."
    }
    foreach ($file in @($items | Where-Object { -not $_.PSIsContainer })) {
        $streamProviderPath = Get-Candidate11StreamProviderPath -Path $file.FullName
        $streams = @(Get-Item -LiteralPath $streamProviderPath -Stream * -ErrorAction Stop)
        $alternate = @($streams | Where-Object { [string]$_.Stream -cne ':$DATA' })
        if ($alternate.Count -gt 0 -and -not $AllowAlternateDataStreams) {
            throw "$Context contains a forbidden alternate data stream: $($file.FullName):$($alternate[0].Stream)"
        }
    }
}

function Assert-Candidate11DirectoryRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $root = Get-Candidate11CanonicalPath -Path $Path
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "$Context does not exist as a directory: $root"
    }
    $item = Get-Item -LiteralPath $root -Force
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Context is a forbidden reparse point: $root"
    }
}

function Get-Candidate11TreeSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Context,
        [switch] $AllowAlternateDataStreams
    )

    Assert-Candidate11SafeTree -Path $Path -Context $Context -AllowAlternateDataStreams:$AllowAlternateDataStreams
    $root = Get-Candidate11CanonicalPath -Path $Path
    $directoryRows = @(
        Get-ChildItem -LiteralPath $root -Recurse -Force -Directory -ErrorAction Stop |
            ForEach-Object {
                $_.FullName.Substring($root.Length + 1).Replace([char]92, [char]47)
            } |
            Sort-Object
    )
    $fileItems = @(
        Get-ChildItem -LiteralPath $root -Recurse -Force -File -ErrorAction Stop |
            Sort-Object @{ Expression = { $_.FullName.Substring($root.Length + 1).Replace([char]92, [char]47) } }
    )

    $fileRows = New-Object 'System.Collections.Generic.List[object]'
    $alternateStreamRows = New-Object 'System.Collections.Generic.List[object]'
    $fileDigestInput = New-Object System.IO.MemoryStream
    $structureDigestInput = New-Object System.IO.MemoryStream
    $totalBytes = [int64]0
    $alternateStreamBytes = [int64]0
    try {
        foreach ($directory in $directoryRows) {
            $directoryBytes = [System.Text.Encoding]::UTF8.GetBytes('D' + [char]0 + [string]$directory + [char]0)
            $structureDigestInput.Write($directoryBytes, 0, $directoryBytes.Length)
        }

        foreach ($file in $fileItems) {
            $relative = $file.FullName.Substring($root.Length + 1).Replace([char]92, [char]47)
            $lengthBefore = [int64]$file.Length
            $fileHasher = [System.Security.Cryptography.SHA256]::Create()
            $fileStream = [System.IO.File]::Open(
                $file.FullName,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::Read)
            try {
                $hashBytes = $fileHasher.ComputeHash($fileStream)
                if ($fileStream.Position -ne $lengthBefore) {
                    throw "$Context changed while it was hashed: $relative"
                }
            }
            finally {
                $fileStream.Dispose()
                $fileHasher.Dispose()
            }
            $lengthAfter = [int64](Get-Item -LiteralPath $file.FullName -Force).Length
            if ($lengthAfter -ne $lengthBefore) {
                throw "$Context changed while it was hashed: $relative"
            }
            $sha256 = ([System.BitConverter]::ToString($hashBytes)).Replace('-', '')
            $fileRows.Add([ordered]@{
                Path = $relative
                Length = $lengthBefore
                Sha256 = $sha256
            }) | Out-Null
            $totalBytes += $lengthBefore

            $relativeBytes = [System.Text.Encoding]::UTF8.GetBytes($relative)
            $lengthBytes = [System.BitConverter]::GetBytes($lengthBefore)
            if (-not [System.BitConverter]::IsLittleEndian) {
                [System.Array]::Reverse($lengthBytes)
            }
            $fileDigestInput.Write($relativeBytes, 0, $relativeBytes.Length)
            $fileDigestInput.WriteByte(0)
            $fileDigestInput.Write($lengthBytes, 0, $lengthBytes.Length)
            $fileDigestInput.Write($hashBytes, 0, $hashBytes.Length)

            $structurePrefix = [System.Text.Encoding]::UTF8.GetBytes('F' + [char]0)
            $structureDigestInput.Write($structurePrefix, 0, $structurePrefix.Length)
            $structureDigestInput.Write($relativeBytes, 0, $relativeBytes.Length)
            $structureDigestInput.WriteByte(0)
            $structureDigestInput.Write($lengthBytes, 0, $lengthBytes.Length)
            $structureDigestInput.Write($hashBytes, 0, $hashBytes.Length)

            if ($AllowAlternateDataStreams) {
                $streamProviderPath = Get-Candidate11StreamProviderPath -Path $file.FullName
                $alternateStreams = @(Get-Item -LiteralPath $streamProviderPath -Stream * -ErrorAction Stop |
                    Where-Object { [string]$_.Stream -cne ':$DATA' } |
                    Sort-Object Stream)
                foreach ($alternateStream in $alternateStreams) {
                    $streamName = [string]$alternateStream.Stream
                    $streamLengthBefore = [int64]$alternateStream.Length
                    $streamHasher = [System.Security.Cryptography.SHA256]::Create()
                    try {
                        $getContentCommand = Get-Command Get-Content -ErrorAction Stop
                        if ($getContentCommand.Parameters.ContainsKey('AsByteStream')) {
                            [byte[]]$streamBytes = Get-Content -LiteralPath $streamProviderPath -Stream $streamName -AsByteStream -Raw -ErrorAction Stop
                        }
                        else {
                            [byte[]]$streamBytes = Get-Content -LiteralPath $streamProviderPath -Stream $streamName -Encoding Byte -Raw -ErrorAction Stop
                        }
                        if ([int64]$streamBytes.Length -ne $streamLengthBefore) {
                            throw "$Context alternate stream changed while it was hashed: $relative`:$streamName"
                        }
                        $streamHashBytes = $streamHasher.ComputeHash($streamBytes)
                    }
                    finally {
                        $streamHasher.Dispose()
                    }
                    $streamSha256 = ([System.BitConverter]::ToString($streamHashBytes)).Replace('-', '')
                    $alternateStreamRows.Add([ordered]@{
                        FilePath = $relative
                        Stream = $streamName
                        Length = $streamLengthBefore
                        Sha256 = $streamSha256
                    }) | Out-Null
                    $alternateStreamBytes += $streamLengthBefore

                    $streamPrefix = [System.Text.Encoding]::UTF8.GetBytes('S' + [char]0)
                    $streamNameBytes = [System.Text.Encoding]::UTF8.GetBytes($streamName)
                    $streamLengthBytes = [System.BitConverter]::GetBytes($streamLengthBefore)
                    if (-not [System.BitConverter]::IsLittleEndian) {
                        [System.Array]::Reverse($streamLengthBytes)
                    }
                    $structureDigestInput.Write($streamPrefix, 0, $streamPrefix.Length)
                    $structureDigestInput.Write($relativeBytes, 0, $relativeBytes.Length)
                    $structureDigestInput.WriteByte(0)
                    $structureDigestInput.Write($streamNameBytes, 0, $streamNameBytes.Length)
                    $structureDigestInput.WriteByte(0)
                    $structureDigestInput.Write($streamLengthBytes, 0, $streamLengthBytes.Length)
                    $structureDigestInput.Write($streamHashBytes, 0, $streamHashBytes.Length)
                }
            }
        }

        $fileDigestInput.Position = 0
        $fileTreeHasher = [System.Security.Cryptography.SHA256]::Create()
        $structureDigestInput.Position = 0
        $structureHasher = [System.Security.Cryptography.SHA256]::Create()
        try {
            $fileTreeSha256 = ([System.BitConverter]::ToString($fileTreeHasher.ComputeHash($fileDigestInput))).Replace('-', '')
            $structureSha256 = ([System.BitConverter]::ToString($structureHasher.ComputeHash($structureDigestInput))).Replace('-', '')
        }
        finally {
            $fileTreeHasher.Dispose()
            $structureHasher.Dispose()
        }
    }
    finally {
        $fileDigestInput.Dispose()
        $structureDigestInput.Dispose()
    }

    return [ordered]@{
        Root = $root
        DigestAlgorithm = $script:Candidate11DigestAlgorithm
        TreeSha256 = $fileTreeSha256
        StructureDigestAlgorithm = $script:Candidate11StructureDigestAlgorithm
        StructureSha256 = $structureSha256
        DirectoryCount = @($directoryRows).Count
        FileCount = $fileRows.Count
        TotalBytes = $totalBytes
        AlternateStreamCount = $alternateStreamRows.Count
        AlternateStreamBytes = $alternateStreamBytes
        Directories = @($directoryRows)
        Files = @($fileRows.ToArray())
        AlternateStreams = @($alternateStreamRows.ToArray())
    }
}

function Test-Candidate11SnapshotsEqual {
    param(
        [Parameter(Mandatory = $true)] $Expected,
        [Parameter(Mandatory = $true)] $Actual
    )

    return [int]$Expected.DirectoryCount -eq [int]$Actual.DirectoryCount -and
        [int]$Expected.FileCount -eq [int]$Actual.FileCount -and
        [int64]$Expected.TotalBytes -eq [int64]$Actual.TotalBytes -and
        [int]$Expected.AlternateStreamCount -eq [int]$Actual.AlternateStreamCount -and
        [int64]$Expected.AlternateStreamBytes -eq [int64]$Actual.AlternateStreamBytes -and
        [string]::Equals([string]$Expected.TreeSha256, [string]$Actual.TreeSha256, [System.StringComparison]::OrdinalIgnoreCase) -and
        [string]::Equals([string]$Expected.StructureSha256, [string]$Actual.StructureSha256, [System.StringComparison]::OrdinalIgnoreCase)
}

function Copy-Candidate11TreeNew {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination,
        [Parameter(Mandatory = $true)] $ExpectedSnapshot,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $sourceRoot = Get-Candidate11CanonicalPath -Path $Source
    $destinationRoot = Get-Candidate11CanonicalPath -Path $Destination
    if (Test-Path -LiteralPath $destinationRoot) {
        throw "$Context staging destination already exists: $destinationRoot"
    }
    [System.IO.Directory]::CreateDirectory($destinationRoot) | Out-Null
    foreach ($directory in @($ExpectedSnapshot.Directories)) {
        [System.IO.Directory]::CreateDirectory((Join-Path $destinationRoot ([string]$directory).Replace([char]47, [char]92))) | Out-Null
    }
    foreach ($file in @($ExpectedSnapshot.Files)) {
        $relativeWindows = ([string]$file.Path).Replace([char]47, [char]92)
        $sourcePath = Join-Path $sourceRoot $relativeWindows
        $destinationPath = Join-Path $destinationRoot $relativeWindows
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $destinationPath)) | Out-Null
        [System.IO.File]::Copy($sourcePath, $destinationPath, $false)
    }

    $sourceAfter = Get-Candidate11TreeSnapshot -Path $sourceRoot -Context "$Context candidate source after copy"
    $staged = Get-Candidate11TreeSnapshot -Path $destinationRoot -Context "$Context same-volume staging"
    if (-not (Test-Candidate11SnapshotsEqual -Expected $ExpectedSnapshot -Actual $sourceAfter)) {
        throw "$Context candidate source changed while it was staged."
    }
    if (-not (Test-Candidate11SnapshotsEqual -Expected $ExpectedSnapshot -Actual $staged)) {
        throw "$Context same-volume staged tree does not match the candidate source."
    }
    return $staged
}

function Get-Candidate11FileReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $FileName
    )

    $fullPath = Get-Candidate11CanonicalPath -Path $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Candidate11 required file is missing: $fullPath"
    }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if ([int64]$item.Length -le 0) {
        throw "Candidate11 required file is empty: $fullPath"
    }
    return [ordered]@{
        FileName = $FileName
        Path = $fullPath
        Length = [int64]$item.Length
        Sha256 = ([string](Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash).ToLowerInvariant()
    }
}

function Get-Candidate11RuntimeAssemblyDigest {
    param([Parameter(Mandatory = $true)] [object[]] $Assemblies)

    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($assembly in @($Assemblies | Sort-Object FileName)) {
        $lines.Add(
            [string]$assembly.FileName + [char]0 +
            [string]([int64]$assembly.Length) + [char]0 +
            ([string]$assembly.Sha256).ToLowerInvariant()) | Out-Null
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes([string]::Join("`n", $lines.ToArray()))
    return (Get-Candidate11Sha256Bytes -Bytes $bytes).ToLowerInvariant()
}

function Get-Candidate11RuntimePackageSnapshot {
    param([Parameter(Mandatory = $true)] [string] $CandidateRoot)

    $candidateRootFullPath = Get-Candidate11CanonicalPath -Path $CandidateRoot
    $runtimePackageRoot = Get-Candidate11CanonicalPath -Path (Join-Path $candidateRootFullPath 'DTMAPI')
    Assert-Candidate11SafeTree -Path $runtimePackageRoot -Context 'Candidate11 Runtime package'

    $releaseManifestPath = Join-Path $runtimePackageRoot 'Content\DTMAPI\release-manifest.json'
    $payloadRoot = Get-Candidate11CanonicalPath -Path (Join-Path $runtimePackageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI')
    if (-not (Test-Path -LiteralPath $payloadRoot -PathType Container)) {
        throw "Candidate11 Runtime payload root is missing: $payloadRoot"
    }
    Assert-Candidate11SafeTree -Path $payloadRoot -Context 'Candidate11 Runtime assembly payload'

    $manifestReceipt = Get-Candidate11FileReceipt -Path $releaseManifestPath -FileName 'release-manifest.json'
    try {
        $releaseManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseManifestPath | ConvertFrom-Json
    }
    catch {
        throw "Candidate11 Runtime release-manifest is unreadable: $releaseManifestPath. $($_.Exception.Message)"
    }
    $buildCommit = [string](Get-DtmApiObjectProperty -Object $releaseManifest -Name 'BuildCommit' -Default '')
    $packageKind = [string](Get-DtmApiObjectProperty -Object $releaseManifest -Name 'PackageKind' -Default '')
    $releaseVersion = [string](Get-DtmApiObjectProperty -Object $releaseManifest -Name 'DTMAPIVersion' -Default '')
    $binaryVersion = [string](Get-DtmApiObjectProperty -Object $releaseManifest -Name 'BinaryVersion' -Default '')
    $bundledMods = @((Get-DtmApiObjectProperty -Object $releaseManifest -Name 'BundledMods' -Default @()))
    if ([int](Get-DtmApiObjectProperty -Object $releaseManifest -Name 'SchemaVersion' -Default 0) -ne 1 -or
        -not [string]::Equals($packageKind, 'workshop-runtime', [System.StringComparison]::Ordinal) -or
        -not [string]::Equals($releaseVersion, $script:DtmApiReleaseVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals($binaryVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal) -or
        [string]::IsNullOrWhiteSpace($buildCommit) -or $buildCommit.Trim() -notmatch '^[0-9a-fA-F]{7,64}$' -or
        $bundledMods.Count -ne 0) {
        throw "Candidate11 Runtime release-manifest failed schema/version/kind/build/bundled-Mod validation: $releaseManifestPath"
    }

    $manifestAssemblies = @((Get-DtmApiObjectProperty -Object $releaseManifest -Name 'IncludedAssemblies' -Default @()))
    if ($manifestAssemblies.Count -ne $script:Candidate11RuntimeAssemblyNames.Count) {
        throw "Candidate11 Runtime release-manifest must contain exactly five assembly receipts; found $($manifestAssemblies.Count)."
    }
    $actualDllFiles = @(Get-ChildItem -LiteralPath $payloadRoot -Recurse -Force -File -Filter '*.dll' -ErrorAction Stop |
        Sort-Object @{ Expression = { $_.FullName.Substring($payloadRoot.Length + 1).Replace([char]92, [char]47) } })
    $actualRelativeDlls = @($actualDllFiles | ForEach-Object {
        $_.FullName.Substring($payloadRoot.Length + 1).Replace([char]92, [char]47)
    })
    if (-not [string]::Equals(
        (@($actualRelativeDlls) -join "`n"),
        (@($script:Candidate11RuntimeAssemblyNames) -join "`n"),
        [System.StringComparison]::Ordinal)) {
        throw "Candidate11 Runtime payload DLL set is not the exact five Runtime assemblies. Expected=$($script:Candidate11RuntimeAssemblyNames -join '|') Actual=$($actualRelativeDlls -join '|')"
    }

    $assemblyRows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($fileName in $script:Candidate11RuntimeAssemblyNames) {
        $projected = @($manifestAssemblies | Where-Object {
            [string]::Equals(
                [string](Get-DtmApiObjectProperty -Object $_ -Name 'FileName' -Default ''),
                $fileName,
                [System.StringComparison]::Ordinal)
        })
        if ($projected.Count -ne 1) {
            throw "Candidate11 Runtime release-manifest must contain exactly one receipt for $fileName."
        }
        $actual = Get-Candidate11FileReceipt -Path (Join-Path $payloadRoot $fileName) -FileName $fileName
        $manifestRow = $projected[0]
        $manifestLength = [int64](Get-DtmApiObjectProperty -Object $manifestRow -Name 'Length' -Default 0)
        $manifestSha256 = [string](Get-DtmApiObjectProperty -Object $manifestRow -Name 'Sha256' -Default '')
        $manifestFileVersion = [string](Get-DtmApiObjectProperty -Object $manifestRow -Name 'FileVersion' -Default '')
        if ($manifestLength -ne [int64]$actual.Length -or
            $manifestSha256 -notmatch '^[0-9a-fA-F]{64}$' -or
            -not [string]::Equals($manifestSha256, [string]$actual.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals($manifestFileVersion, $script:DtmApiBinaryVersion, [System.StringComparison]::Ordinal)) {
            throw "Candidate11 Runtime release-manifest receipt does not match payload bytes for $fileName."
        }
        $assemblyRows.Add([ordered]@{
            FileName = $fileName
            CandidatePath = [string]$actual.Path
            Length = [int64]$actual.Length
            Sha256 = [string]$actual.Sha256
            FileVersion = $manifestFileVersion
            ManifestLength = $manifestLength
            ManifestSha256 = $manifestSha256.ToLowerInvariant()
        }) | Out-Null
    }

    $assemblies = @($assemblyRows.ToArray())
    $assemblySetSha256 = Get-Candidate11RuntimeAssemblyDigest -Assemblies $assemblies
    $bindingBytes = [System.Text.Encoding]::UTF8.GetBytes(
        [string]$manifestReceipt.Length + [char]0 +
        ([string]$manifestReceipt.Sha256).ToLowerInvariant() + [char]0 +
        $assemblySetSha256)
    return [ordered]@{
        SchemaVersion = 1
        PackageRoot = $runtimePackageRoot
        ReleaseManifestPath = [string]$manifestReceipt.Path
        ReleaseManifestLength = [int64]$manifestReceipt.Length
        ReleaseManifestSha256 = [string]$manifestReceipt.Sha256
        ReleaseManifest = [ordered]@{
            SchemaVersion = [int](Get-DtmApiObjectProperty -Object $releaseManifest -Name 'SchemaVersion' -Default 0)
            DTMAPIVersion = $releaseVersion
            BinaryVersion = $binaryVersion
            BuildCommit = $buildCommit.Trim()
            BuildTime = [string](Get-DtmApiObjectProperty -Object $releaseManifest -Name 'BuildTime' -Default '')
            PackageKind = $packageKind
            BundledModCount = $bundledMods.Count
            IncludedAssemblyCount = $manifestAssemblies.Count
        }
        PayloadRoot = $payloadRoot
        AssemblyCount = $assemblies.Count
        AssemblyDigestAlgorithm = $script:Candidate11RuntimeAssemblyDigestAlgorithm
        AssemblySetSha256 = $assemblySetSha256
        Assemblies = $assemblies
        BindingDigestAlgorithm = $script:Candidate11RuntimeBindingDigestAlgorithm
        BindingSha256 = (Get-Candidate11Sha256Bytes -Bytes $bindingBytes).ToLowerInvariant()
        Passed = $true
    }
}

function Test-Candidate11RuntimePackageSnapshotsEqual {
    param(
        [Parameter(Mandatory = $true)] $Expected,
        [Parameter(Mandatory = $true)] $Actual
    )

    return [int]$Expected.AssemblyCount -eq $script:Candidate11RuntimeAssemblyNames.Count -and
        [int]$Actual.AssemblyCount -eq $script:Candidate11RuntimeAssemblyNames.Count -and
        [string]::Equals([string]$Expected.BindingSha256, [string]$Actual.BindingSha256, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-Candidate11InstalledRuntimeBinding {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDirectory,
        [Parameter(Mandatory = $true)] $CandidateRuntime
    )

    $gameRoot = Get-Candidate11CanonicalPath -Path $GameDirectory
    Assert-DtmApiDolocTownGamePath -Path $gameRoot -Source 'Candidate11 game directory'
    $installedRoot = Get-Candidate11CanonicalPath -Path (Join-Path $gameRoot 'BepInEx\plugins\DTMAPI')
    Assert-Candidate11SafeTree -Path $installedRoot -Context 'Candidate11 installed Runtime'
    $actualDllFiles = @(Get-ChildItem -LiteralPath $installedRoot -Recurse -Force -File -Filter '*.dll' -ErrorAction Stop |
        Sort-Object @{ Expression = { $_.FullName.Substring($installedRoot.Length + 1).Replace([char]92, [char]47) } })
    $actualRelativeDlls = @($actualDllFiles | ForEach-Object {
        $_.FullName.Substring($installedRoot.Length + 1).Replace([char]92, [char]47)
    })
    if (-not [string]::Equals(
        (@($actualRelativeDlls) -join "`n"),
        (@($script:Candidate11RuntimeAssemblyNames) -join "`n"),
        [System.StringComparison]::Ordinal)) {
        throw "Candidate11 installed Runtime DLL set is not the exact candidate five. Expected=$($script:Candidate11RuntimeAssemblyNames -join '|') Actual=$($actualRelativeDlls -join '|')"
    }

    $installedRows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($expected in @($CandidateRuntime.Assemblies | Sort-Object FileName)) {
        $actual = Get-Candidate11FileReceipt -Path (Join-Path $installedRoot ([string]$expected.FileName)) -FileName ([string]$expected.FileName)
        $matches = [int64]$actual.Length -eq [int64]$expected.Length -and
            [string]::Equals([string]$actual.Sha256, [string]$expected.Sha256, [System.StringComparison]::OrdinalIgnoreCase)
        $installedRows.Add([ordered]@{
            FileName = [string]$expected.FileName
            InstalledPath = [string]$actual.Path
            CandidateLength = [int64]$expected.Length
            InstalledLength = [int64]$actual.Length
            CandidateSha256 = [string]$expected.Sha256
            InstalledSha256 = [string]$actual.Sha256
            MatchesCandidate = $matches
        }) | Out-Null
        if (-not $matches) {
            throw "Candidate11 installed Runtime bytes do not match the candidate release-manifest for $($expected.FileName)."
        }
    }

    $stateDir = Get-Candidate11CanonicalPath -Path (Resolve-DtmApiStateDir -GameDir $gameRoot)
    $installedManifestPath = Join-Path $stateDir 'release-manifest.json'
    $installedManifestReceipt = Get-Candidate11FileReceipt -Path $installedManifestPath -FileName 'release-manifest.json'
    try {
        $installedManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $installedManifestPath | ConvertFrom-Json
    }
    catch {
        throw "Candidate11 installed Runtime release-manifest is unreadable: $installedManifestPath. $($_.Exception.Message)"
    }
    $installedPackageKind = [string](Get-DtmApiObjectProperty -Object $installedManifest -Name 'PackageKind' -Default '')
    $installedBuildCommit = [string](Get-DtmApiObjectProperty -Object $installedManifest -Name 'BuildCommit' -Default '')
    $candidateManifest = $CandidateRuntime.ReleaseManifest
    if ([int](Get-DtmApiObjectProperty -Object $installedManifest -Name 'SchemaVersion' -Default 0) -ne 1 -or
        @('local-install','workshop-runtime-install') -cnotcontains $installedPackageKind -or
        -not [string]::Equals([string](Get-DtmApiObjectProperty -Object $installedManifest -Name 'DTMAPIVersion' -Default ''), [string]$candidateManifest.DTMAPIVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals([string](Get-DtmApiObjectProperty -Object $installedManifest -Name 'BinaryVersion' -Default ''), [string]$candidateManifest.BinaryVersion, [System.StringComparison]::Ordinal) -or
        -not [string]::Equals($installedBuildCommit, [string]$candidateManifest.BuildCommit, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Candidate11 installed Runtime release-manifest provenance does not match the candidate Runtime projection: $installedManifestPath"
    }
    $installedManifestAssemblies = @((Get-DtmApiObjectProperty -Object $installedManifest -Name 'IncludedAssemblies' -Default @()))
    if ($installedManifestAssemblies.Count -ne $script:Candidate11RuntimeAssemblyNames.Count) {
        throw "Candidate11 installed Runtime release-manifest does not contain exactly five assembly receipts."
    }
    foreach ($expected in @($CandidateRuntime.Assemblies)) {
        $projected = @($installedManifestAssemblies | Where-Object {
            [string]::Equals(
                [string](Get-DtmApiObjectProperty -Object $_ -Name 'FileName' -Default ''),
                [string]$expected.FileName,
                [System.StringComparison]::Ordinal)
        })
        if ($projected.Count -ne 1) {
            throw "Candidate11 installed Runtime release-manifest must contain exactly one receipt for $($expected.FileName)."
        }
        $receipt = $projected[0]
        if ([int64](Get-DtmApiObjectProperty -Object $receipt -Name 'Length' -Default 0) -ne [int64]$expected.Length -or
            -not [string]::Equals([string](Get-DtmApiObjectProperty -Object $receipt -Name 'Sha256' -Default ''), [string]$expected.Sha256, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals([string](Get-DtmApiObjectProperty -Object $receipt -Name 'FileVersion' -Default ''), [string]$expected.FileVersion, [System.StringComparison]::Ordinal)) {
            throw "Candidate11 installed Runtime release-manifest receipt does not match candidate bytes for $($expected.FileName)."
        }
    }

    $installedAssemblies = @($installedRows.ToArray())
    return [ordered]@{
        SchemaVersion = 1
        GameDir = $gameRoot
        InstalledRoot = $installedRoot
        CandidateBindingSha256 = [string]$CandidateRuntime.BindingSha256
        CandidateAssemblySetSha256 = [string]$CandidateRuntime.AssemblySetSha256
        AssemblyCount = $installedAssemblies.Count
        Assemblies = $installedAssemblies
        AllAssemblyBytesMatched = @($installedAssemblies | Where-Object { -not [bool]$_.MatchesCandidate }).Count -eq 0
        InstalledReleaseManifestPath = [string]$installedManifestReceipt.Path
        InstalledReleaseManifestLength = [int64]$installedManifestReceipt.Length
        InstalledReleaseManifestSha256 = [string]$installedManifestReceipt.Sha256
        InstalledReleaseManifestProjection = [ordered]@{
            SchemaVersion = [int](Get-DtmApiObjectProperty -Object $installedManifest -Name 'SchemaVersion' -Default 0)
            DTMAPIVersion = [string](Get-DtmApiObjectProperty -Object $installedManifest -Name 'DTMAPIVersion' -Default '')
            BinaryVersion = [string](Get-DtmApiObjectProperty -Object $installedManifest -Name 'BinaryVersion' -Default '')
            BuildCommit = $installedBuildCommit
            PackageKind = $installedPackageKind
            IncludedAssemblyCount = $installedManifestAssemblies.Count
        }
        ReleaseManifestProjectionMatched = $true
        Passed = $true
    }
}

function Get-Candidate11CatalogSelection {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Root
    )

    $catalogFullPath = Get-Candidate11CanonicalPath -Path $Path
    if (-not (Test-Path -LiteralPath $catalogFullPath -PathType Leaf)) {
        throw "Candidate11 requires the tracked Product Catalog: $catalogFullPath"
    }
    $catalogBytes = [System.IO.File]::ReadAllBytes($catalogFullPath)
    $catalog = (New-Object System.Text.UTF8Encoding($false)).GetString($catalogBytes) | ConvertFrom-Json
    $products = @($catalog.products | Where-Object {
        [string]$_.role -ceq 'PublishedProduct' -and [string]$_.distributionState -ceq 'PublicWorkshop'
    } | Sort-Object catalogId)
    if ($products.Count -ne $script:Candidate11ProductCount) {
        throw "Candidate11 requires exactly $($script:Candidate11ProductCount) PublishedProduct/PublicWorkshop Catalog rows; found $($products.Count)."
    }

    foreach ($field in @('catalogId','uniqueId','workshopId','officialFolder','packageName','packageDll')) {
        $missing = @($products | Where-Object { [string]::IsNullOrWhiteSpace([string]($_.$field)) })
        if ($missing.Count -gt 0) {
            throw "Candidate11 Catalog selection contains an incomplete '$field' field."
        }
        $values = @($products | ForEach-Object { [string]($_.$field) })
        if (@($values | Sort-Object -Unique).Count -ne $values.Count) {
            throw "Candidate11 Catalog selection contains duplicate '$field' values."
        }
    }
    if (@($products | Where-Object { [string]::IsNullOrWhiteSpace([string]$_.sourceVersion) }).Count -gt 0) {
        throw "Candidate11 Catalog selection contains an incomplete 'sourceVersion' field."
    }

    $candidateRootFullPath = Get-Candidate11CanonicalPath -Path $Root
    Assert-Candidate11SafeTree -Path $candidateRootFullPath -Context 'Candidate11 root'
    $topLevelFiles = @(Get-ChildItem -LiteralPath $candidateRootFullPath -Force -File -ErrorAction Stop)
    if ($topLevelFiles.Count -gt 0) {
        throw "Candidate11 root contains unexpected top-level files: $([string]::Join('|', @($topLevelFiles.Name | Sort-Object)))"
    }
    $expectedNames = @(@($products | ForEach-Object { [string]$_.packageName }) + 'DTMAPI')
    $allowedNames = @($expectedNames)
    $actualDirectories = @(Get-ChildItem -LiteralPath $candidateRootFullPath -Force -Directory -ErrorAction Stop)
    $actualDirectoryNames = @($actualDirectories | ForEach-Object { [string]$_.Name })
    $unexpected = @($actualDirectories | Where-Object { $allowedNames -cnotcontains $_.Name })
    $unexpectedNames = @($unexpected | ForEach-Object { [string]$_.Name })
    $missingPackages = @($expectedNames | Where-Object { $actualDirectoryNames -cnotcontains $_ })
    if ($unexpected.Count -gt 0 -or $missingPackages.Count -gt 0) {
        throw "Candidate11 top-level package inventory is not exact. missing=$([string]::Join('|', $missingPackages)); unexpected=$([string]::Join('|', $unexpectedNames))."
    }

    $runtimeSnapshot = Get-Candidate11RuntimePackageSnapshot -CandidateRoot $candidateRootFullPath
    $selectionRows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($product in $products) {
        foreach ($leafField in @('officialFolder','packageName','packageDll')) {
            $leaf = [string]$product.$leafField
            if ($leaf -in @('.','..') -or [System.IO.Path]::IsPathRooted($leaf) -or
                [System.IO.Path]::GetFileName($leaf) -cne $leaf -or
                $leaf.TrimEnd([char]32, [char]46) -cne $leaf -or
                $leaf.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
                throw "Candidate11 Catalog field '$leafField' is not a safe leaf name for $($product.catalogId): $leaf"
            }
        }
        if ([string]::Equals([string]$product.packageName, 'DTMAPI', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Candidate11 Catalog packageName is reserved for the Runtime package: $($product.catalogId)"
        }
        $packageRoot = Join-Path $candidateRootFullPath ([string]$product.packageName)
        $manifestPath = Join-Path $packageRoot 'Content\DTMAPI\manifest.json'
        $infoPath = Join-Path $packageRoot 'info.json'
        $packageMarkerPath = Join-Path $packageRoot 'Content\DTMAPI\dtmapi-package.json'
        foreach ($requiredFile in @($manifestPath, $infoPath, $packageMarkerPath)) {
            if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
                throw "Candidate11 package '$($product.catalogId)' is missing required file: $requiredFile"
            }
        }
        $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
        $info = Get-Content -Raw -Encoding UTF8 -LiteralPath $infoPath | ConvertFrom-Json
        $packageMarker = Get-Content -Raw -Encoding UTF8 -LiteralPath $packageMarkerPath | ConvertFrom-Json
        $expectedEntryDll = 'Content/DTMAPI/' + [string]$product.packageDll
        $expectedMinimum = if ($product.PSObject.Properties['sourceMinimumDtmApiVersion']) { [string]$product.sourceMinimumDtmApiVersion } else { '' }
        if ([string]$manifest.UniqueID -cne [string]$product.uniqueId -or
            [string]$manifest.EntryDll -cne $expectedEntryDll -or
            [string]$manifest.Version -cne [string]$product.sourceVersion -or
            [string]$info.version -cne [string]$product.sourceVersion -or
            (-not [string]::IsNullOrWhiteSpace($expectedMinimum) -and [string]$manifest.MinimumDTMApiVersion -cne $expectedMinimum) -or
            [string]$packageMarker.uniqueId -cne [string]$product.uniqueId -or
            [string]$packageMarker.owner -cne 'DTMAPI' -or
            [string]$packageMarker.packageKind -cne 'workshop-mod') {
            throw "Candidate11 package '$($product.catalogId)' does not match its Catalog identity/version projection."
        }
        $entryDllPath = Join-Path $packageRoot $expectedEntryDll.Replace([char]47, [char]92)
        if (-not (Test-Path -LiteralPath $entryDllPath -PathType Leaf)) {
            throw "Candidate11 package '$($product.catalogId)' is missing its Catalog DLL: $entryDllPath"
        }
        $packageDlls = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -Force -File -Filter '*.dll' -ErrorAction Stop)
        if ($packageDlls.Count -ne 1 -or $packageDlls[0].Name -cne [string]$product.packageDll) {
            throw "Candidate11 package '$($product.catalogId)' does not contain exactly its one Catalog DLL."
        }
        $forbiddenQa = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -Force -ErrorAction Stop | Where-Object {
            $relative = $_.FullName.Substring((Get-Candidate11CanonicalPath -Path $packageRoot).Length + 1).Replace([char]92, [char]47)
            $relative -match '(^|/)(?i:qa-host)(/|$)' -or
            $_.Name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb)|DTMAPI\.(Smoke|Tests)\.(dll|pdb)|qa-settings\.json|smoke-settings\.json|qa-host.*\.json)$'
        })
        if ($forbiddenQa.Count -gt 0) {
            throw "Candidate11 package '$($product.catalogId)' contains developer-only QA material: $($forbiddenQa[0].FullName)"
        }

        $snapshot = Get-Candidate11TreeSnapshot -Path $packageRoot -Context "Candidate11 package $($product.catalogId)"
        $selectionRows.Add([ordered]@{
            CatalogId = [string]$product.catalogId
            UniqueId = [string]$product.uniqueId
            WorkshopId = [string]$product.workshopId
            OfficialFolder = [string]$product.officialFolder
            PackageName = [string]$product.packageName
            PackageDll = [string]$product.packageDll
            CandidatePath = Get-Candidate11CanonicalPath -Path $packageRoot
            CandidateSnapshot = $snapshot
        }) | Out-Null
    }

    return [ordered]@{
        CatalogPath = $catalogFullPath
        CatalogSha256 = Get-Candidate11Sha256Bytes -Bytes $catalogBytes
        CandidateRoot = $candidateRootFullPath
        RuntimePackagePresent = $true
        Runtime = $runtimeSnapshot
        ProductCount = $selectionRows.Count
        Products = @($selectionRows.ToArray())
    }
}

function Get-Candidate11PersistentRoot {
    param([string] $ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        return Get-Candidate11CanonicalPath -Path $ExplicitPath
    }
    if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_DOLOC_PERSISTENT_ROOT)) {
        return Get-Candidate11CanonicalPath -Path $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    }
    return Get-Candidate11CanonicalPath -Path (Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) 'AppData\LocalLow\RedSawGames\DolocTown')
}

function Assert-Candidate11SmokeContract {
    param([Parameter(Mandatory = $true)] [string[]] $Arguments)

    $normalized = @($Arguments | ForEach-Object { [string]$_ })
    foreach ($requiredSwitch in @('-UseSteam','-SkipInstall','-IsolateAllOfficialMods','-AssertNoQaUiEvidence')) {
        if ($normalized -inotcontains $requiredSwitch) {
            throw "Candidate11 smoke requires $requiredSwitch."
        }
    }
    $profileIndex = [Array]::FindIndex([string[]]$normalized, [Predicate[string]]{ param($value) [string]::Equals($value, '-OfficialModProfile', [System.StringComparison]::OrdinalIgnoreCase) })
    if ($profileIndex -lt 0 -or $profileIndex + 1 -ge $normalized.Count -or [string]$normalized[$profileIndex + 1] -cne 'Local11') {
        throw 'Candidate11 smoke requires -OfficialModProfile Local11.'
    }
    if ($normalized -icontains '-PackagePayloadRoot') {
        throw 'Candidate11 smoke must use the already-installed Runtime and cannot supply -PackagePayloadRoot.'
    }
    if ($normalized -icontains '-StageQaHost') {
        throw 'Candidate11 smoke must remain an ordinary no-QA Local11 run and cannot supply -StageQaHost.'
    }
}

function Get-Candidate11PowerShellHost {
    $current = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    if ((Test-Path -LiteralPath $current -PathType Leaf) -and [System.IO.Path]::GetExtension($current) -ieq '.exe') {
        return Get-Candidate11CanonicalPath -Path $current
    }
    foreach ($candidate in @(
        (Join-Path $PSHOME 'powershell.exe'),
        (Join-Path $PSHOME 'pwsh.exe'),
        (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe')
    )) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return Get-Candidate11CanonicalPath -Path $candidate
        }
    }
    throw 'Candidate11 could not resolve a PowerShell child-process host.'
}

function Invoke-Candidate11SmokeChild {
    param(
        [Parameter(Mandatory = $true)] [string] $ScriptPath,
        [Parameter(Mandatory = $true)] [string[]] $Arguments,
        [Parameter(Mandatory = $true)] [string] $OutputPath
    )

    $scriptFullPath = Get-Candidate11CanonicalPath -Path $ScriptPath
    if (-not (Test-Path -LiteralPath $scriptFullPath -PathType Leaf)) {
        throw "Candidate11 smoke script does not exist: $scriptFullPath"
    }
    $hostExe = Get-Candidate11PowerShellHost
    $childArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $scriptFullPath) + @($Arguments)
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $hostExe @childArguments 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    Write-Candidate11NewText -Path $OutputPath -Text ([string]::Join([Environment]::NewLine, $output) + [Environment]::NewLine)
    return [ordered]@{
        Host = $hostExe
        Script = $scriptFullPath
        Arguments = @($Arguments)
        ExitCode = [int]$exitCode
        OutputPath = Get-Candidate11CanonicalPath -Path $OutputPath
    }
}

function Test-Candidate11DolocTownProcessAbsent {
    return @(Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue).Count -eq 0
}

function Wait-Candidate11DolocTownProcessAbsent {
    param([Parameter(Mandatory = $true)] [int] $TimeoutSeconds)

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (Test-Candidate11DolocTownProcessAbsent) {
            return $true
        }
        if ([DateTime]::UtcNow -ge $deadline) {
            return $false
        }
        Start-Sleep -Milliseconds 500
    } while ($true)
}

function Move-Candidate11Directory {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $sourcePath = Get-Candidate11CanonicalPath -Path $Source
    $destinationPath = Get-Candidate11CanonicalPath -Path $Destination
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Container)) {
        throw "$Context source directory is missing: $sourcePath"
    }
    if (Test-Path -LiteralPath $destinationPath) {
        throw "$Context destination already exists: $destinationPath"
    }
    if (-not [string]::Equals(
        [System.IO.Path]::GetPathRoot($sourcePath),
        [System.IO.Path]::GetPathRoot($destinationPath),
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Context is not a same-volume directory move: $sourcePath -> $destinationPath"
    }
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $destinationPath)) | Out-Null
    [System.IO.Directory]::Move($sourcePath, $destinationPath)
}

function Restore-Candidate11ProductStates {
    param(
        [Parameter(Mandatory = $true)] [object[]] $States,
        [Parameter(Mandatory = $true)] [bool] $ProcessAbsent
    )

    $rows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($state in @($States | Sort-Object @{ Expression = { [string]$_.CatalogId }; Descending = $true })) {
        $failure = ''
        $candidatePreserved = -not [bool]$state.CandidateMoved
        $originalReturned = -not [bool]$state.OriginalMoved
        try {
            if (-not $ProcessAbsent -and ([bool]$state.CandidateMoved -or [bool]$state.OriginalMoved)) {
                throw 'DolocTown.exe is still running; product-tree restoration is blocked to avoid live runtime mutation.'
            }
            if ([bool]$state.CandidateMoved) {
                Move-Candidate11Directory -Source ([string]$state.DestinationPath) -Destination ([string]$state.ObservedPath) -Context "Candidate11 preserve tested candidate $($state.CatalogId)"
                $candidatePreserved = $true
                $state.CandidateMoved = $false
            }
            if ([bool]$state.OriginalMoved) {
                try {
                    Move-Candidate11Directory -Source ([string]$state.BackupPath) -Destination ([string]$state.DestinationPath) -Context "Candidate11 restore original $($state.CatalogId)"
                    $originalReturned = $true
                    $state.OriginalMoved = $false
                }
                catch {
                    if ($candidatePreserved -and (Test-Path -LiteralPath ([string]$state.ObservedPath) -PathType Container) -and
                        -not (Test-Path -LiteralPath ([string]$state.DestinationPath))) {
                        Move-Candidate11Directory -Source ([string]$state.ObservedPath) -Destination ([string]$state.DestinationPath) -Context "Candidate11 restoration rollback $($state.CatalogId)"
                        $state.CandidateMoved = $true
                        $candidatePreserved = $false
                    }
                    throw
                }
            }
        }
        catch {
            $failure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
        }

        $destinationExists = Test-Path -LiteralPath ([string]$state.DestinationPath) -PathType Container
        $destinationSnapshot = $null
        $destinationSnapshotFailure = ''
        if ($destinationExists) {
            try {
                $destinationSnapshot = Get-Candidate11TreeSnapshot -Path ([string]$state.DestinationPath) -Context "Candidate11 restored destination $($state.CatalogId)" -AllowAlternateDataStreams
            }
            catch {
                $destinationSnapshotFailure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
            }
        }
        $restoredExact = [string]::IsNullOrWhiteSpace($failure) -and [string]::IsNullOrWhiteSpace($destinationSnapshotFailure) -and
            ($destinationExists -eq [bool]$state.OriginalExisted) -and
            ((-not [bool]$state.OriginalExisted) -or (Test-Candidate11SnapshotsEqual -Expected $state.OriginalSnapshot -Actual $destinationSnapshot))
        $observedExists = Test-Path -LiteralPath ([string]$state.ObservedPath) -PathType Container
        $observedSnapshot = $null
        $observedSnapshotFailure = ''
        if ($observedExists) {
            try {
                $observedSnapshot = Get-Candidate11TreeSnapshot -Path ([string]$state.ObservedPath) -Context "Candidate11 preserved tested candidate $($state.CatalogId)" -AllowAlternateDataStreams
            }
            catch {
                $observedSnapshotFailure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
            }
        }
        $expectedObservedSnapshot = if ($null -ne $state.PostSmokeSnapshot) { $state.PostSmokeSnapshot } else { $state.StagedSnapshot }
        $observedMatchesExpected = $observedExists -and [string]::IsNullOrWhiteSpace($observedSnapshotFailure) -and
            $null -ne $observedSnapshot -and (Test-Candidate11SnapshotsEqual -Expected $expectedObservedSnapshot -Actual $observedSnapshot)
        $rows.Add([ordered]@{
            CatalogId = [string]$state.CatalogId
            DestinationPath = [string]$state.DestinationPath
            OriginalExisted = [bool]$state.OriginalExisted
            OriginalSnapshot = $state.OriginalSnapshot
            DestinationExistsAfter = $destinationExists
            DestinationSnapshotAfter = $destinationSnapshot
            ObservedCandidatePath = [string]$state.ObservedPath
            CandidatePreservedOutsideMods = $observedExists
            ObservedCandidateSnapshot = $observedSnapshot
            ObservedCandidateSnapshotFailure = $observedSnapshotFailure
            ObservedCandidateMatchesLastInspectedBytes = $observedMatchesExpected
            Failure = $failure
            SnapshotFailure = $destinationSnapshotFailure
            RestoredExact = $restoredExact
        }) | Out-Null
    }
    return @($rows.ToArray())
}

function Invoke-DtmApiCandidate11SourceTransaction {
    param(
        [Parameter(Mandatory = $true)] [string] $CandidatePackageRoot,
        [Parameter(Mandatory = $true)] [string] $ProductCatalogPath,
        [Parameter(Mandatory = $true)] [string] $DolocPersistentRoot,
        [string] $GameDirectory = '',
        [Parameter(Mandatory = $true)] [string] $FormalEvidenceRoot,
        [Parameter(Mandatory = $true)] [string] $Id,
        [Parameter(Mandatory = $true)] [string] $SmokePath,
        [Parameter(Mandatory = $true)] [string[]] $SmokeArguments,
        [int] $LockTimeoutSeconds = 3600,
        [int] $ProcessExitTimeoutSeconds = 30,
        [switch] $SkipRuntimeLockForTest,
        [switch] $SkipProcessCheckForTest,
        [switch] $SkipSmokeContractForTest
    )

    if (($SkipRuntimeLockForTest -or $SkipProcessCheckForTest -or $SkipSmokeContractForTest) -and
        [string]$env:DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE -cne '1') {
        throw 'Candidate11 TestOnly switches require DTMAPI_CANDIDATE11_TRANSACTION_TEST_MODE=1.'
    }
    if ([string]::IsNullOrWhiteSpace($Id) -or $Id -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$' -or $Id.EndsWith('.')) {
        throw "Candidate11 transaction id is not a safe leaf name: '$Id'"
    }
    if (-not $SkipSmokeContractForTest) {
        Assert-Candidate11SmokeContract -Arguments $SmokeArguments
    }

    $repoRoot = Get-RepoRoot
    $persistent = Get-Candidate11CanonicalPath -Path $DolocPersistentRoot
    $modsRoot = Get-Candidate11CanonicalPath -Path (Join-Path $persistent 'MODS')
    Assert-Candidate11DirectoryRoot -Path $persistent -Context 'Candidate11 persistent root'
    if (-not (Test-Path -LiteralPath $modsRoot -PathType Container)) {
        throw "Candidate11 requires an existing official MODS root: $modsRoot"
    }
    Assert-Candidate11DirectoryRoot -Path $modsRoot -Context 'Candidate11 official MODS root before transaction'
    $candidateSelection = Get-Candidate11CatalogSelection -Path $ProductCatalogPath -Root $CandidatePackageRoot
    $resolvedGameDir = if ([string]::IsNullOrWhiteSpace($GameDirectory)) {
        Get-Candidate11CanonicalPath -Path (Resolve-DolocTownGamePath -RepoRoot $repoRoot)
    }
    else {
        Get-Candidate11CanonicalPath -Path $GameDirectory
    }
    Assert-DtmApiDolocTownGamePath -Path $resolvedGameDir -Source 'Candidate11 game directory'

    $workParent = Get-Candidate11CanonicalPath -Path (Join-Path $persistent '.dtmapi-candidate11-transactions')
    if (-not (Test-Path -LiteralPath $workParent)) {
        [System.IO.Directory]::CreateDirectory($workParent) | Out-Null
    }
    Assert-Candidate11DirectoryRoot -Path $workParent -Context 'Candidate11 transaction parent'
    $workRoot = Get-Candidate11CanonicalPath -Path (Join-Path $workParent $Id)
    $evidence = Get-Candidate11CanonicalPath -Path $FormalEvidenceRoot
    if (Test-Path -LiteralPath $workRoot) {
        throw "Candidate11 transaction work root already exists: $workRoot"
    }
    if (Test-Path -LiteralPath $evidence) {
        throw "Candidate11 evidence root already exists: $evidence"
    }
    if (Test-Candidate11SameOrChildPath -Child $workRoot -Parent $modsRoot) {
        throw "Candidate11 same-volume staging must remain outside MODS: $workRoot"
    }
    if ((Test-Candidate11SameOrChildPath -Child ([string]$candidateSelection.CandidateRoot) -Parent $modsRoot) -or
        (Test-Candidate11SameOrChildPath -Child $modsRoot -Parent ([string]$candidateSelection.CandidateRoot))) {
        throw 'Candidate11 candidate source and official MODS roots may not overlap.'
    }
    if ((Test-Candidate11SameOrChildPath -Child $workRoot -Parent ([string]$candidateSelection.CandidateRoot)) -or
        (Test-Candidate11SameOrChildPath -Child ([string]$candidateSelection.CandidateRoot) -Parent $workRoot)) {
        throw 'Candidate11 same-volume work root and candidate source may not overlap.'
    }
    if (-not [string]::Equals(
        [System.IO.Path]::GetPathRoot($workRoot),
        [System.IO.Path]::GetPathRoot($modsRoot),
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Candidate11 work root and MODS root are not on the same volume: work=$workRoot mods=$modsRoot"
    }
    foreach ($protectedRoot in @($modsRoot, [string]$candidateSelection.CandidateRoot)) {
        if (Test-Candidate11SameOrChildPath -Child $evidence -Parent $protectedRoot) {
            throw "Candidate11 evidence root cannot be inside a mutable package tree: $evidence"
        }
    }

    [System.IO.Directory]::CreateDirectory($workRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($evidence) | Out-Null
    $workReceipts = Join-Path $workRoot 'receipts'
    [System.IO.Directory]::CreateDirectory($workReceipts) | Out-Null
    Write-Candidate11NewJson -Path (Join-Path $workReceipts '00-boundary.json') -Value ([ordered]@{
        SchemaVersion = 2
        TransactionId = $Id
        CandidateRoot = [string]$candidateSelection.CandidateRoot
        CatalogPath = [string]$candidateSelection.CatalogPath
        CatalogSha256 = [string]$candidateSelection.CatalogSha256
        CatalogSha256Role = 'Selection provenance only; final pass is bound to per-tree product snapshots plus Runtime manifest/DLL receipts.'
        ProductCount = [int]$candidateSelection.ProductCount
        RuntimeCandidate = $candidateSelection.Runtime
        GameDir = $resolvedGameDir
        ModsRoot = $modsRoot
        WorkRoot = $workRoot
        EvidenceRoot = $evidence
        SameVolume = $true
        StagingOutsideMods = $true
        DestructiveAuthority = 'None'
        ProductTreeOperations = 'Directory.Move only; tested candidate trees are preserved outside MODS; originals are restored in finally.'
    })

    $runtimeLock = $null
    $runtimeLockAcquired = $false
    $states = New-Object 'System.Collections.Generic.List[object]'
    $primaryError = $null
    $smokeResult = $null
    $postSmokeRows = @()
    $restoreRows = @()
    $installedRuntimePostSmoke = $null
    $runtimeCandidateFinal = $null
    $runtimeCandidateSourceUnchanged = $false
    $processAbsentBefore = $SkipProcessCheckForTest
    $processAbsentAfter = $SkipProcessCheckForTest
    try {
        if (-not $SkipRuntimeLockForTest) {
            $runtimeLock = Wait-DtmApiRuntimeLock -RepoRoot $repoRoot -Reason "Candidate11 byte-bound transaction $Id" -TimeoutSeconds $LockTimeoutSeconds
            $runtimeLockAcquired = [bool]$runtimeLock.Acquired
            if (-not $runtimeLockAcquired) {
                throw 'Candidate11 requires a newly acquired runtime lock and will not reuse another operation lease.'
            }
        }
        if (-not $SkipProcessCheckForTest) {
            $processAbsentBefore = Test-Candidate11DolocTownProcessAbsent
        }
        if (-not $processAbsentBefore) {
            throw 'Candidate11 refuses to stage product trees while DolocTown.exe is running.'
        }

        $incomingRoot = Join-Path $workRoot 'incoming'
        $backupRoot = Join-Path $workRoot 'original-backup'
        $observedRoot = Join-Path $workRoot 'tested-candidate'
        [System.IO.Directory]::CreateDirectory($incomingRoot) | Out-Null
        [System.IO.Directory]::CreateDirectory($backupRoot) | Out-Null
        [System.IO.Directory]::CreateDirectory($observedRoot) | Out-Null

        $preflightRows = New-Object 'System.Collections.Generic.List[object]'
        foreach ($product in @($candidateSelection.Products)) {
            $incoming = Join-Path $incomingRoot ([string]$product.OfficialFolder)
            $stagedSnapshot = Copy-Candidate11TreeNew `
                -Source ([string]$product.CandidatePath) `
                -Destination $incoming `
                -ExpectedSnapshot $product.CandidateSnapshot `
                -Context "Candidate11 $($product.CatalogId)"
            $destination = Join-Path $modsRoot ([string]$product.OfficialFolder)
            $originalExisted = Test-Path -LiteralPath $destination -PathType Container
            $originalSnapshot = $null
            if ($originalExisted) {
                $originalSnapshot = Get-Candidate11TreeSnapshot -Path $destination -Context "Candidate11 original $($product.CatalogId)" -AllowAlternateDataStreams
            }
            elseif (Test-Path -LiteralPath $destination) {
                throw "Candidate11 official destination exists but is not a directory: $destination"
            }
            $state = [pscustomobject][ordered]@{
                CatalogId = [string]$product.CatalogId
                UniqueId = [string]$product.UniqueId
                OfficialFolder = [string]$product.OfficialFolder
                CandidateSourcePath = [string]$product.CandidatePath
                CandidateSnapshot = $product.CandidateSnapshot
                IncomingPath = Get-Candidate11CanonicalPath -Path $incoming
                StagedSnapshot = $stagedSnapshot
                DestinationPath = Get-Candidate11CanonicalPath -Path $destination
                BackupPath = Get-Candidate11CanonicalPath -Path (Join-Path $backupRoot ([string]$product.OfficialFolder))
                ObservedPath = Get-Candidate11CanonicalPath -Path (Join-Path $observedRoot ([string]$product.OfficialFolder))
                OriginalExisted = $originalExisted
                OriginalSnapshot = $originalSnapshot
                OriginalMoved = $false
                CandidateMoved = $false
                PostSmokeSnapshot = $null
                PostSmokeInspectionFailure = ''
                PostSmokeMatched = $false
            }
            $states.Add($state) | Out-Null
            $preflightRows.Add([ordered]@{
                CatalogId = [string]$state.CatalogId
                UniqueId = [string]$state.UniqueId
                OfficialFolder = [string]$state.OfficialFolder
                CandidateSourcePath = [string]$state.CandidateSourcePath
                CandidateSnapshot = $state.CandidateSnapshot
                IncomingPath = [string]$state.IncomingPath
                StagedSnapshot = $state.StagedSnapshot
                DestinationPath = [string]$state.DestinationPath
                OriginalExisted = [bool]$state.OriginalExisted
                OriginalSnapshot = $state.OriginalSnapshot
            }) | Out-Null
        }
        Write-Candidate11NewJson -Path (Join-Path $workReceipts '10-preflight-inventory.json') -Value ([ordered]@{
            SchemaVersion = 2
            CatalogSha256 = [string]$candidateSelection.CatalogSha256
            CatalogSha256Role = 'Selection provenance only'
            Runtime = $candidateSelection.Runtime
            ProductCount = $states.Count
            Products = @($preflightRows.ToArray())
        })

        foreach ($state in @($states.ToArray())) {
            if ([bool]$state.OriginalExisted) {
                Move-Candidate11Directory -Source ([string]$state.DestinationPath) -Destination ([string]$state.BackupPath) -Context "Candidate11 backup original $($state.CatalogId)"
                $state.OriginalMoved = $true
            }
            Move-Candidate11Directory -Source ([string]$state.IncomingPath) -Destination ([string]$state.DestinationPath) -Context "Candidate11 activate candidate $($state.CatalogId)"
            $state.CandidateMoved = $true
        }

        $appliedRows = New-Object 'System.Collections.Generic.List[object]'
        foreach ($state in @($states.ToArray())) {
            $appliedSnapshot = Get-Candidate11TreeSnapshot -Path ([string]$state.DestinationPath) -Context "Candidate11 applied $($state.CatalogId)"
            $matches = Test-Candidate11SnapshotsEqual -Expected $state.StagedSnapshot -Actual $appliedSnapshot
            $appliedRows.Add([ordered]@{
                CatalogId = [string]$state.CatalogId
                DestinationPath = [string]$state.DestinationPath
                ExpectedSnapshot = $state.StagedSnapshot
                ActualSnapshot = $appliedSnapshot
                Matches = $matches
            }) | Out-Null
            if (-not $matches) {
                throw "Candidate11 applied tree does not match the same-volume staged bytes for $($state.CatalogId)."
            }
        }
        Write-Candidate11NewJson -Path (Join-Path $workReceipts '20-applied-inventory.json') -Value ([ordered]@{
            ProductCount = $states.Count
            Products = @($appliedRows.ToArray())
            AllMatched = @($appliedRows.ToArray() | Where-Object { -not [bool]$_.Matches }).Count -eq 0
        })

        $smokeResult = Invoke-Candidate11SmokeChild `
            -ScriptPath $SmokePath `
            -Arguments $SmokeArguments `
            -OutputPath (Join-Path $workReceipts '30-smoke-output.txt')
        Write-Candidate11NewJson -Path (Join-Path $workReceipts '31-smoke-result.json') -Value $smokeResult

        $postRows = New-Object 'System.Collections.Generic.List[object]'
        foreach ($state in @($states.ToArray())) {
            $actual = $null
            $failure = ''
            try {
                $actual = Get-Candidate11TreeSnapshot -Path ([string]$state.DestinationPath) -Context "Candidate11 post-smoke $($state.CatalogId)" -AllowAlternateDataStreams
            }
            catch {
                $failure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
            }
            $matches = [string]::IsNullOrWhiteSpace($failure) -and $null -ne $actual -and
                (Test-Candidate11SnapshotsEqual -Expected $state.StagedSnapshot -Actual $actual)
            $state.PostSmokeSnapshot = $actual
            $state.PostSmokeInspectionFailure = $failure
            $state.PostSmokeMatched = $matches
            $postRows.Add([ordered]@{
                CatalogId = [string]$state.CatalogId
                DestinationPath = [string]$state.DestinationPath
                ExpectedSnapshot = $state.StagedSnapshot
                ActualSnapshot = $actual
                InspectionFailure = $failure
                Matches = $matches
            }) | Out-Null
        }
        $postSmokeRows = @($postRows.ToArray())
        Write-Candidate11NewJson -Path (Join-Path $workReceipts '40-post-smoke-inventory.json') -Value ([ordered]@{
            ProductCount = $states.Count
            Products = $postSmokeRows
            AllMatched = @($postSmokeRows | Where-Object { -not [bool]$_.Matches }).Count -eq 0
        })
        if (-not $SkipProcessCheckForTest) {
            $processAbsentAfter = Wait-Candidate11DolocTownProcessAbsent -TimeoutSeconds $ProcessExitTimeoutSeconds
        }
        if (-not $processAbsentAfter) {
            throw 'Candidate11 smoke child returned while DolocTown.exe remained alive; restoration is blocked until process absence is proven.'
        }
        try {
            $installedRuntimePostSmoke = Get-Candidate11InstalledRuntimeBinding `
                -GameDirectory $resolvedGameDir `
                -CandidateRuntime $candidateSelection.Runtime
        }
        catch {
            $installedRuntimePostSmoke = [ordered]@{
                SchemaVersion = 1
                GameDir = $resolvedGameDir
                CandidateBindingSha256 = [string]$candidateSelection.Runtime.BindingSha256
                CandidateAssemblySetSha256 = [string]$candidateSelection.Runtime.AssemblySetSha256
                Passed = $false
                Error = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
            }
            Write-Candidate11NewJson -Path (Join-Path $workReceipts '41-installed-runtime-post-smoke.json') -Value $installedRuntimePostSmoke
            throw "Candidate11 installed Runtime cross-check failed after child smoke: $($_.Exception.Message)"
        }
        Write-Candidate11NewJson -Path (Join-Path $workReceipts '41-installed-runtime-post-smoke.json') -Value $installedRuntimePostSmoke
        if ([int]$smokeResult.ExitCode -ne 0) {
            throw "Candidate11 smoke child failed with exit code $($smokeResult.ExitCode)."
        }
        if (@($postSmokeRows | Where-Object { -not [bool]$_.Matches }).Count -gt 0) {
            throw 'Candidate11 post-smoke product bytes drifted from the staged candidate inventory.'
        }
    }
    catch {
        $primaryError = $_
        if (-not $SkipProcessCheckForTest) {
            $processAbsentAfter = Wait-Candidate11DolocTownProcessAbsent -TimeoutSeconds $ProcessExitTimeoutSeconds
        }
    }
    finally {
        if ($states.Count -gt 0) {
            try {
                $restoreRows = @(Restore-Candidate11ProductStates -States @($states.ToArray()) -ProcessAbsent ([bool]$processAbsentAfter))
                Write-Candidate11NewJson -Path (Join-Path $workReceipts '50-restore-inventory.json') -Value ([ordered]@{
                    ProductCount = $states.Count
                    ProcessAbsent = [bool]$processAbsentAfter
                    Products = $restoreRows
                    AllRestoredExact = @($restoreRows | Where-Object { -not [bool]$_.RestoredExact }).Count -eq 0
                })
            }
            catch {
                if ($null -eq $primaryError) {
                    $primaryError = $_
                }
                else {
                    $primaryError = [System.InvalidOperationException]::new(
                        $primaryError.Exception.Message + '; restore receipt failure: ' + $_.Exception.Message,
                        $primaryError.Exception)
                }
            }
        }
        if ($runtimeLockAcquired) {
            try {
                $null = Release-DtmApiRuntimeLock -RepoRoot $repoRoot
            }
            catch {
                if ($null -eq $primaryError) {
                    $primaryError = $_
                }
                else {
                    $primaryError = [System.InvalidOperationException]::new(
                        $primaryError.Exception.Message + '; runtime-lock release failure: ' + $_.Exception.Message,
                        $primaryError.Exception)
                }
            }
        }
    }

    $sourceFinalRows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($product in @($candidateSelection.Products)) {
        $actual = $null
        $failure = ''
        try {
            $actual = Get-Candidate11TreeSnapshot -Path ([string]$product.CandidatePath) -Context "Candidate11 final source $($product.CatalogId)"
        }
        catch {
            $failure = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
        }
        $matches = [string]::IsNullOrWhiteSpace($failure) -and $null -ne $actual -and
            (Test-Candidate11SnapshotsEqual -Expected $product.CandidateSnapshot -Actual $actual)
        $sourceFinalRows.Add([ordered]@{
            CatalogId = [string]$product.CatalogId
            CandidatePath = [string]$product.CandidatePath
            ExpectedSnapshot = $product.CandidateSnapshot
            ActualSnapshot = $actual
            InspectionFailure = $failure
            Matches = $matches
        }) | Out-Null
    }
    $allSourcesUnchanged = @($sourceFinalRows.ToArray() | Where-Object { -not [bool]$_.Matches }).Count -eq 0
    Write-Candidate11NewJson -Path (Join-Path $workReceipts '60-candidate-source-final.json') -Value ([ordered]@{
        ProductCount = [int]$candidateSelection.ProductCount
        Products = @($sourceFinalRows.ToArray())
        AllUnchanged = $allSourcesUnchanged
    })
    if (-not $allSourcesUnchanged -and $null -eq $primaryError) {
        $primaryError = [System.InvalidOperationException]::new('Candidate11 source package bytes changed during the transaction.')
    }

    try {
        $runtimeCandidateFinal = Get-Candidate11RuntimePackageSnapshot -CandidateRoot ([string]$candidateSelection.CandidateRoot)
        $runtimeCandidateSourceUnchanged = Test-Candidate11RuntimePackageSnapshotsEqual `
            -Expected $candidateSelection.Runtime `
            -Actual $runtimeCandidateFinal
    }
    catch {
        $runtimeCandidateFinal = [ordered]@{
            SchemaVersion = 1
            PackageRoot = [string]$candidateSelection.Runtime.PackageRoot
            Passed = $false
            Error = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
        }
        $runtimeCandidateSourceUnchanged = $false
    }
    Write-Candidate11NewJson -Path (Join-Path $workReceipts '61-runtime-candidate-source-final.json') -Value ([ordered]@{
        Expected = $candidateSelection.Runtime
        Actual = $runtimeCandidateFinal
        Unchanged = $runtimeCandidateSourceUnchanged
    })
    if (-not $runtimeCandidateSourceUnchanged -and $null -eq $primaryError) {
        $primaryError = [System.InvalidOperationException]::new('Candidate11 Runtime source manifest/DLL bytes changed during the transaction.')
    }

    $allPostMatched = $postSmokeRows.Count -eq $script:Candidate11ProductCount -and
        @($postSmokeRows | Where-Object { -not [bool]$_.Matches }).Count -eq 0
    $allRestored = $restoreRows.Count -eq $script:Candidate11ProductCount -and
        @($restoreRows | Where-Object { -not [bool]$_.RestoredExact }).Count -eq 0
    $runtimeBindingPassed = [bool]$candidateSelection.Runtime.Passed -and
        $null -ne $installedRuntimePostSmoke -and
        [bool]$installedRuntimePostSmoke.Passed -and
        $runtimeCandidateSourceUnchanged
    $passed = $null -eq $primaryError -and $null -ne $smokeResult -and [int]$smokeResult.ExitCode -eq 0 -and
        $allPostMatched -and $allRestored -and $allSourcesUnchanged -and $runtimeBindingPassed
    $result = [ordered]@{
        SchemaVersion = 2
        TransactionId = $Id
        RunStatus = if ($passed) { 'Passed' } else { 'Failed' }
        ProductCount = [int]$candidateSelection.ProductCount
        CandidateRoot = [string]$candidateSelection.CandidateRoot
        CatalogPath = [string]$candidateSelection.CatalogPath
        CatalogSha256 = [string]$candidateSelection.CatalogSha256
        CatalogSha256Role = 'Selection provenance only; not a self-referential final pass gate.'
        RuntimeBinding = [ordered]@{
            CandidatePreflight = $candidateSelection.Runtime
            InstalledPostSmoke = $installedRuntimePostSmoke
            CandidateFinal = $runtimeCandidateFinal
            CandidateSourceUnchanged = $runtimeCandidateSourceUnchanged
            Passed = $runtimeBindingPassed
        }
        ModsRoot = $modsRoot
        WorkRoot = $workRoot
        EvidenceRoot = $evidence
        RuntimeLockAcquired = $runtimeLockAcquired
        ProcessAbsentBefore = [bool]$processAbsentBefore
        ProcessAbsentAfter = [bool]$processAbsentAfter
        Smoke = $smokeResult
        PostSmokeAllMatched = $allPostMatched
        RestoreAllExact = $allRestored
        CandidateSourcesAllUnchanged = $allSourcesUnchanged
        TestedCandidateTreesPreservedRoot = Get-Candidate11CanonicalPath -Path (Join-Path $workRoot 'tested-candidate')
        Error = if ($null -ne $primaryError) { $primaryError.Exception.GetType().Name + ': ' + $primaryError.Exception.Message } else { '' }
        ErrorScriptStackTrace = if ($null -ne $primaryError) { [string]$primaryError.ScriptStackTrace } else { '' }
        DestructiveAuthority = 'None'
        Passed = $passed
    }
    Write-Candidate11NewJson -Path (Join-Path $workReceipts '99-result.json') -Value $result

    foreach ($receipt in @(Get-ChildItem -LiteralPath $workReceipts -Force -File -ErrorAction Stop | Sort-Object Name)) {
        [System.IO.File]::Copy($receipt.FullName, (Join-Path $evidence $receipt.Name), $false)
    }

    if (-not $passed) {
        throw "Candidate11 byte-bound transaction failed. Originals restored=$allRestored; evidence=$evidence; work=$workRoot; error=$($result.Error)"
    }
    return $result
}

if ($LibraryOnly) {
    return
}

foreach ($required in @(
    [ordered]@{ Name = 'CandidateRoot'; Value = $CandidateRoot },
    [ordered]@{ Name = 'SmokeScriptPath'; Value = $SmokeScriptPath }
)) {
    if ([string]::IsNullOrWhiteSpace([string]$required.Value)) {
        throw "-$($required.Name) is required."
    }
}
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($CatalogPath)) {
    $CatalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
}
$PersistentRoot = Get-Candidate11PersistentRoot -ExplicitPath $PersistentRoot
if ([string]::IsNullOrWhiteSpace($TransactionId)) {
    $TransactionId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
}
if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path (Join-Path $repo 'docs\debug\evidence\CANDIDATE11') $TransactionId
}

$result = Invoke-DtmApiCandidate11SourceTransaction `
    -CandidatePackageRoot $CandidateRoot `
    -ProductCatalogPath $CatalogPath `
    -DolocPersistentRoot $PersistentRoot `
    -GameDirectory $GameDir `
    -FormalEvidenceRoot $EvidenceRoot `
    -Id $TransactionId `
    -SmokePath $SmokeScriptPath `
    -SmokeArguments $SmokeArgumentList `
    -LockTimeoutSeconds $RuntimeLockTimeoutSeconds `
    -ProcessExitTimeoutSeconds $PostSmokeProcessExitTimeoutSeconds `
    -SkipRuntimeLockForTest:$TestOnlySkipRuntimeLock `
    -SkipProcessCheckForTest:$TestOnlySkipProcessCheck `
    -SkipSmokeContractForTest:$TestOnlySkipSmokeContract

Write-Host "Candidate11 byte-bound transaction passed. Evidence: $($result.EvidenceRoot)"
