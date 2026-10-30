# Shared files helpers. Definitions only; PowerShell 5.1 compatible.
function Get-DtmApiNormalizedFullPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $full = [System.IO.Path]::GetFullPath($Path)
    return $full.TrimEnd([char[]]@('\', '/'))
}

function Test-DtmApiPathIsSameOrChild {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    $childFull = Get-DtmApiNormalizedFullPath -Path $Child
    $parentFull = Get-DtmApiNormalizedFullPath -Path $Parent
    if ([string]::Equals($childFull, $parentFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $parentPrefix = $parentFull + [System.IO.Path]::DirectorySeparatorChar
    return $childFull.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-DtmApiFileSha256 {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $stream = [System.IO.File]::Open(
        [System.IO.Path]::GetFullPath($Path),
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha256.ComputeHash($stream)
            return ([System.BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Initialize-DtmApiZipRuntime {
    $compressionAssembly = @([System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {
        [string]::Equals($_.GetName().Name, 'System.IO.Compression', [System.StringComparison]::OrdinalIgnoreCase)
    } | Select-Object -First 1)
    if ($compressionAssembly.Count -ne 1) {
        try {
            $loadedAssembly = [System.Reflection.Assembly]::Load('System.IO.Compression')
        }
        catch {
            $loadedAssembly = [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression')
        }
        if ($null -ne $loadedAssembly) {
            $compressionAssembly = @($loadedAssembly)
        }
    }

    if ($compressionAssembly.Count -ne 1) {
        throw 'The .NET ZIP runtime assembly is unavailable on this PowerShell host.'
    }

    $archiveType = $compressionAssembly[0].GetType('System.IO.Compression.ZipArchive', $false)
    $modeType = $compressionAssembly[0].GetType('System.IO.Compression.ZipArchiveMode', $false)
    if ($null -eq $archiveType -or $null -eq $modeType) {
        throw 'The .NET ZIP runtime is unavailable on this PowerShell host.'
    }

    $constructor = $archiveType.GetConstructor(@([System.IO.Stream], $modeType, [bool]))
    if ($null -eq $constructor) {
        throw 'The .NET ZIP reader constructor is unavailable on this PowerShell host.'
    }

    return [pscustomobject]@{
        ArchiveType = $archiveType
        ModeType = $modeType
        Constructor = $constructor
    }
}

function New-DtmApiZipArchive {
    param([Parameter(Mandatory=$true)][string]$SourceDirectory, [Parameter(Mandatory=$true)][string]$DestinationPath)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipRoot=(Resolve-Path -LiteralPath $SourceDirectory).Path.TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    $zipArchive=[IO.Compression.ZipFile]::Open([IO.Path]::GetFullPath($DestinationPath),[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach($zipFile in Get-ChildItem -LiteralPath $zipRoot -Recurse -File | Sort-Object FullName){
            $zipName=$zipFile.FullName.Substring($zipRoot.Length).Replace('\','/')
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zipArchive,$zipFile.FullName,$zipName)
        }
    } finally {$zipArchive.Dispose()}
}

function Test-DtmApiZipArchiveSupport {
    try {
        [void](Initialize-DtmApiZipRuntime)
        return $true
    }
    catch {
        return $false
    }
}

function Expand-DtmApiZipArchive {
    param(
        [Parameter(Mandatory = $true)] [string] $LiteralPath,
        [Parameter(Mandatory = $true)] [string] $DestinationPath
    )

    $sourceFull = [System.IO.Path]::GetFullPath($LiteralPath)
    if (-not (Test-Path -LiteralPath $sourceFull -PathType Leaf)) {
        throw "ZIP archive is missing: $sourceFull"
    }

    $destinationFull = [System.IO.Path]::GetFullPath($DestinationPath)
    [void][System.IO.Directory]::CreateDirectory($destinationFull)
    $zipRuntime = Initialize-DtmApiZipRuntime
    $readMode = [System.Enum]::Parse($zipRuntime.ModeType, 'Read')
    $stream = [System.IO.File]::Open(
        $sourceFull,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    $archive = $null
    try {
        $archive = $zipRuntime.Constructor.Invoke([object[]]@($stream, $readMode, $false))
        foreach ($entry in @($archive.Entries)) {
            $entryName = ([string]$entry.FullName).Replace('/', [System.IO.Path]::DirectorySeparatorChar).Replace('\', [System.IO.Path]::DirectorySeparatorChar)
            if ([string]::IsNullOrWhiteSpace($entryName)) {
                continue
            }

            $targetPath = [System.IO.Path]::GetFullPath((Join-Path $destinationFull $entryName))
            if (-not (Test-DtmApiPathIsSameOrChild -Child $targetPath -Parent $destinationFull)) {
                throw "ZIP archive contains an entry outside the destination: $($entry.FullName)"
            }

            if ([string]::IsNullOrEmpty([string]$entry.Name)) {
                [void][System.IO.Directory]::CreateDirectory($targetPath)
                continue
            }

            $targetParent = [System.IO.Path]::GetDirectoryName($targetPath)
            if (-not [string]::IsNullOrWhiteSpace($targetParent)) {
                [void][System.IO.Directory]::CreateDirectory($targetParent)
            }

            $entryStream = $entry.Open()
            try {
                $outputStream = [System.IO.File]::Open(
                    $targetPath,
                    [System.IO.FileMode]::Create,
                    [System.IO.FileAccess]::Write,
                    [System.IO.FileShare]::None)
                try {
                    $entryStream.CopyTo($outputStream)
                }
                finally {
                    $outputStream.Dispose()
                }
            }
            finally {
                $entryStream.Dispose()
            }
        }
    }
    finally {
        if ($null -ne $archive) {
            $archive.Dispose()
        }
        $stream.Dispose()
    }

    return $destinationFull
}

function Invoke-DtmApiFileDownload {
    param(
        [Parameter(Mandatory = $true)] [string] $Uri,
        [Parameter(Mandatory = $true)] [string] $DestinationPath
    )

    $destinationFull = [System.IO.Path]::GetFullPath($DestinationPath)
    $destinationParent = [System.IO.Path]::GetDirectoryName($destinationFull)
    if (-not [string]::IsNullOrWhiteSpace($destinationParent)) {
        [void][System.IO.Directory]::CreateDirectory($destinationParent)
    }

    $originalProtocol = [System.Net.ServicePointManager]::SecurityProtocol
    $client = $null
    try {
        try {
            $tls12 = [System.Enum]::Parse([System.Net.SecurityProtocolType], 'Tls12')
            [System.Net.ServicePointManager]::SecurityProtocol = $originalProtocol -bor $tls12
        }
        catch {
        }

        $client = New-Object System.Net.WebClient
        $client.DownloadFile($Uri, $destinationFull)
    }
    finally {
        if ($null -ne $client) {
            $client.Dispose()
        }
        [System.Net.ServicePointManager]::SecurityProtocol = $originalProtocol
    }

    return $destinationFull
}

function Assert-DtmApiPrivateArtifactPathOutsideRepository {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $RepositoryRoot,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
    if (Test-DtmApiPathIsSameOrChild -Child $resolvedPath -Parent $resolvedRepositoryRoot) {
        throw "$Label must stay outside the source/distribution tree: $resolvedPath"
    }

    $cursor = $resolvedPath
    while (-not [string]::IsNullOrWhiteSpace($cursor)) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Label path may not contain an existing reparse-point ancestor: $($item.FullName)"
            }
        }

        $parent = [System.IO.Directory]::GetParent($cursor)
        if ($null -eq $parent -or
            [string]::Equals($parent.FullName, $cursor, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $cursor = $parent.FullName
    }

    return $resolvedPath
}

function Copy-DirectoryContents {
    param(
        [string] $Source,
        [string] $Destination,
        [string[]] $Include,
        [switch] $Required,
        [switch] $AllowDestinationInsideSource
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        if ($Required) {
            throw "Required source directory is missing: $Source"
        }

        return
    }

    if (-not $AllowDestinationInsideSource) {
        if (Test-DtmApiPathIsSameOrChild -Child $Destination -Parent $Source) {
            throw "Refusing to copy directory contents because destination is inside source. Source: $Source. Destination: $Destination"
        }

        if (Test-DtmApiPathIsSameOrChild -Child $Source -Parent $Destination) {
            throw "Refusing to copy directory contents because source is inside destination. Source: $Source. Destination: $Destination"
        }
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($name in $Include) {
        if ([System.IO.Path]::IsPathRooted($name) -or @($name -split '[\\/]' | Where-Object { $_ -eq '..' }).Count -gt 0) {
            throw "Refusing to copy unsafe include path '$name'. Include values must be relative names inside the source directory."
        }

        $path = Join-Path $Source $name
        if (-not (Test-Path -LiteralPath $path)) {
            if ($Required) {
                throw "Required source item is missing: $path"
            }

            continue
        }

        $target = Join-Path $Destination $name
        if (Test-Path -LiteralPath $path -PathType Container) {
            New-Item -ItemType Directory -Force -Path $target | Out-Null
            Get-ChildItem -LiteralPath $path -Force | Copy-Item -Recurse -Force -Destination $target
        }
        else {
            Copy-Item -Force -LiteralPath $path -Destination $target
        }
    }
}
# Fixed Runtime module inventory; not an executable plugin extension point.
function Get-DtmApiRuntimeModuleFile {
    param([string]$RelativePath)
    $managed = @{
        'modules/DTMAPI.RuntimeModules' = 'runtime-modules'
        'monsters/DTMAPI.MonsterContent' = 'monster-contract'
        'monsters/DTMAPI.MonsterContent.Authoring' = 'monster-authoring'
        'monsters/DTMAPI.Modules.Monsters' = 'monster-backend'
        'animals/DTMAPI.AnimalContent' = 'animal-contract'
        'animals/DTMAPI.Modules.Animals' = 'animal-backend'
        'audio/DTMAPI.AudioContent' = 'audio-contract'
        'audio/DTMAPI.AudioContent.Authoring' = 'audio-authoring'
        'audio/DTMAPI.AudioPreparation' = 'audio-preparation'
        'audio/DTMAPI.Modules.Audio' = 'audio-backend'
    }
    foreach ($entry in $managed.GetEnumerator()) {
        $path = 'DTMAPI/components/' + $entry.Key + '.dll'
        if (-not $RelativePath -or $RelativePath -ceq $path) {
            [pscustomobject]@{ Id = $entry.Value; RelativePath = $path; AssemblyName = ($entry.Key -split '/')[-1]; AssemblyVersion = '0.1.0.0'; TargetFramework = 'netstandard2.0'; Auxiliary = $false; LoadPolicy = $(if ($entry.Key.StartsWith('audio/')) { 'declared-audio-demand' } else { 'accepted-module-contribution' }) }
        }
    }
    foreach ($name in @('ffmpeg.exe','LICENSE.txt','distribution.json')) {
        $path = 'DTMAPI/components/audio/audio-decoder/' + $name
        if (-not $RelativePath -or $RelativePath -ceq $path) {
            [pscustomobject]@{ Id = 'audio-decoder-' + $name; RelativePath = $path; AssemblyName = ''; AssemblyVersion = ''; TargetFramework = 'audio-decoder-data'; Auxiliary = $true; LoadPolicy = 'declared-audio-demand' }
        }
    }
}
