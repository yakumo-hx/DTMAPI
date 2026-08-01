param(
    [string] $Configuration = 'Release',
    [string] $AuthorSdkArtifactRoot = '',
    [string] $AuthorSdkRepeatArtifactRoot = '',
    [switch] $Quiet
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\release-common.ps1"

$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$publishPath = Join-Path $repo 'tools\release\dtmapi-mod-publish-zh.json'
$versionAuthorityPath = Join-Path $repo 'tools\release\dtmapi-runtime-version.props'
$failures = New-Object 'System.Collections.Generic.List[string]'

function Add-ReleaseContractFailure {
    param([Parameter(Mandatory = $true)] [string] $Message)

    $script:failures.Add($Message) | Out-Null
}

function Get-ReleaseContractValue {
    param(
        $Object,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    if ($null -eq $Object) {
        return $null
    }
    if ($Object -is [System.Collections.IDictionary]) {
        if ($Object.Contains($Name)) {
            return $Object[$Name]
        }
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($property) {
        return $property.Value
    }
    return $null
}

function Test-ReleaseContractProperty {
    param(
        $Object,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    if ($null -eq $Object) {
        return $false
    }
    if ($Object -is [System.Collections.IDictionary]) {
        return $Object.Contains($Name)
    }
    return $null -ne $Object.PSObject.Properties[$Name]
}

function Read-ReleaseContractJson {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-ReleaseContractFailure "Required JSON file is missing: $Path"
        return $null
    }
    try {
        return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    }
    catch {
        Add-ReleaseContractFailure ("JSON parse failed for {0}: {1}" -f $Path, $_.Exception.Message)
        return $null
    }
}

function Assert-ReleaseContractEqual {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        $Actual,
        $Expected
    )

    $actualText = if ($null -eq $Actual) { '<null>' } else { [string]$Actual }
    $expectedText = if ($null -eq $Expected) { '<null>' } else { [string]$Expected }
    if (-not [string]::Equals($actualText, $expectedText, [System.StringComparison]::Ordinal)) {
        Add-ReleaseContractFailure ("{0}: expected '{1}', got '{2}'" -f $Label, $expectedText, $actualText)
    }
}

function Assert-ReleaseContractTrue {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [bool] $Condition
    )

    if (-not $Condition) {
        Add-ReleaseContractFailure $Label
    }
}

function Normalize-ReleaseContractPath {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }
    $text = ([string]$Value).Replace('\', '/').TrimStart('/')
    if ([System.IO.Path]::IsPathRooted($text) -or @($text.Split('/')) -contains '..') {
        Add-ReleaseContractFailure "Release contract path must be repository-relative and traversal-free: $text"
        return ''
    }
    return $text
}

function Get-ReleaseContractXmlValue {
    param(
        [Parameter(Mandatory = $true)] [xml] $Document,
        [Parameter(Mandatory = $true)] [string] $XPath,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $nodes = @($Document.SelectNodes($XPath))
    if ($nodes.Count -ne 1) {
        Add-ReleaseContractFailure ("{0}: expected one XML node at {1}, got {2}" -f $Label, $XPath, $nodes.Count)
        return $null
    }
    return [string]$nodes[0].InnerText
}

function Get-ReleaseContractFourPartVersion {
    param(
        [Parameter(Mandatory = $true)] [string] $Version,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $numeric = $Version.Trim()
    $suffixIndex = $numeric.IndexOfAny([char[]]@('-', '+'))
    if ($suffixIndex -ge 0) {
        $numeric = $numeric.Substring(0, $suffixIndex)
    }

    $parsed = $null
    if (-not [Version]::TryParse($numeric, [ref]$parsed)) {
        Add-ReleaseContractFailure "$Label cannot be projected to a numeric assembly/file version: $Version"
        return ''
    }
    return ('{0}.{1}.{2}.{3}' -f $parsed.Major, $parsed.Minor, [Math]::Max(0, $parsed.Build), [Math]::Max(0, $parsed.Revision))
}

function Test-ReleaseContractAssembly {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $ExpectedAssemblyName,
        [Parameter(Mandatory = $true)] [string] $ExpectedAssemblyVersion,
        [Parameter(Mandatory = $true)] [string] $ExpectedFileVersion,
        [Parameter(Mandatory = $true)] [string] $ExpectedProductVersion
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-ReleaseContractFailure "$Label artifact is missing; run the tracked build first: $Path"
        return
    }

    try {
        $resolved = [System.IO.Path]::GetFullPath($Path)
        $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($resolved)
        $fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($resolved)
        Assert-ReleaseContractEqual -Label "$Label assembly name" -Actual $assemblyName.Name -Expected $ExpectedAssemblyName
        Assert-ReleaseContractEqual -Label "$Label AssemblyVersion" -Actual $assemblyName.Version.ToString() -Expected $ExpectedAssemblyVersion
        Assert-ReleaseContractEqual -Label "$Label FileVersion" -Actual $fileInfo.FileVersion -Expected $ExpectedFileVersion
        Assert-ReleaseContractEqual -Label "$Label Product/InformationalVersion" -Actual $fileInfo.ProductVersion -Expected $ExpectedProductVersion
        Assert-ReleaseContractTrue -Label "$Label artifact must not be empty: $resolved" -Condition ((Get-Item -LiteralPath $resolved).Length -gt 0)
    }
    catch {
        Add-ReleaseContractFailure ("{0} metadata read failed for {1}: {2}" -f $Label, $Path, $_.Exception.Message)
    }
}

function Test-ReleaseContractAssemblyExcludesReference {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $ForbiddenAssemblyName
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return
    }
    try {
        $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($Path))
        $references = @($assembly.GetReferencedAssemblies() | ForEach-Object { [string]$_.Name })
        Assert-ReleaseContractTrue -Label "$Label must not contain an AssemblyRef to $ForbiddenAssemblyName. Actual=[$($references -join ', ')]" -Condition ($references -notcontains $ForbiddenAssemblyName)
    }
    catch {
        Add-ReleaseContractFailure ("{0} AssemblyRef read failed for {1}: {2}" -f $Label, $Path, $_.Exception.Message)
    }
}

function Test-ReleaseContractManagedAssemblyMetadata {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $ExpectedAssemblyName,
        [Parameter(Mandatory = $true)] [string] $ExpectedAssemblyVersion,
        [Parameter(Mandatory = $true)] [string] $ExpectedFileVersion,
        [Parameter(Mandatory = $true)] [string] $ExpectedInformationalVersion
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-ReleaseContractFailure "$Label artifact is missing: $Path"
        return
    }
    try {
        $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName([System.IO.Path]::GetFullPath($Path))
        $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($Path))
        $attributes = @($assembly.GetCustomAttributesData())
        $fileVersions = @($attributes | Where-Object {
            $_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute'
        })
        $informationalVersions = @($attributes | Where-Object {
            $_.AttributeType.FullName -eq 'System.Reflection.AssemblyInformationalVersionAttribute'
        })
        Assert-ReleaseContractEqual -Label "$Label assembly name" -Actual $assemblyName.Name -Expected $ExpectedAssemblyName
        Assert-ReleaseContractEqual -Label "$Label AssemblyVersion" -Actual $assemblyName.Version.ToString() -Expected $ExpectedAssemblyVersion
        Assert-ReleaseContractEqual -Label "$Label AssemblyFileVersionAttribute count" -Actual $fileVersions.Count -Expected 1
        Assert-ReleaseContractEqual -Label "$Label AssemblyInformationalVersionAttribute count" -Actual $informationalVersions.Count -Expected 1
        if ($fileVersions.Count -eq 1) {
            Assert-ReleaseContractEqual -Label "$Label AssemblyFileVersionAttribute" -Actual ([string]$fileVersions[0].ConstructorArguments[0].Value) -Expected $ExpectedFileVersion
        }
        if ($informationalVersions.Count -eq 1) {
            Assert-ReleaseContractEqual -Label "$Label AssemblyInformationalVersionAttribute (source revision suffix suppressed)" -Actual ([string]$informationalVersions[0].ConstructorArguments[0].Value) -Expected $ExpectedInformationalVersion
        }
    }
    catch {
        Add-ReleaseContractFailure ("{0} managed metadata read failed for {1}: {2}" -f $Label, $Path, $_.Exception.Message)
    }
}

function Get-ReleaseContractSha256 {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-ReleaseContractFailure "$Label is missing: $Path"
        return ''
    }
    try {
        return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    catch {
        Add-ReleaseContractFailure ("{0} SHA-256 failed for {1}: {2}" -f $Label, $Path, $_.Exception.Message)
        return ''
    }
}

function Get-ReleaseContractSourceTreeSha256 {
    param(
        [Parameter(Mandatory = $true)] [string] $SourceRoot
    )

    if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) {
        Add-ReleaseContractFailure "Author SDK source root is missing: $SourceRoot"
        return ''
    }
    try {
        $root = [System.IO.Path]::GetFullPath($SourceRoot).TrimEnd('\', '/')
        $files = [string[]]@(Get-ChildItem -LiteralPath $root -Recurse -Filter '*.cs' -File | ForEach-Object {
            $_.FullName.Substring($root.Length + 1).Replace('\', '/')
        })
        [System.Array]::Sort($files, [System.StringComparer]::Ordinal)
        $aggregate = [System.Security.Cryptography.IncrementalHash]::CreateHash(
            [System.Security.Cryptography.HashAlgorithmName]::SHA256)
        try {
            foreach ($relative in $files) {
                $file = Join-Path $root $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
                $lengthText = ([System.IO.FileInfo]$file).Length.ToString([System.Globalization.CultureInfo]::InvariantCulture)
                $header = [System.Text.Encoding]::UTF8.GetBytes($relative + [char]0 + $lengthText + [char]0)
                $aggregate.AppendData($header)
                $stream = [System.IO.File]::OpenRead($file)
                try {
                    $buffer = New-Object byte[] 81920
                    while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                        $aggregate.AppendData($buffer, 0, $read)
                    }
                }
                finally {
                    $stream.Dispose()
                }
            }
            return ([System.BitConverter]::ToString($aggregate.GetHashAndReset())).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $aggregate.Dispose()
        }
    }
    catch {
        Add-ReleaseContractFailure ("Author SDK source-tree SHA-256 failed for {0}: {1}" -f $SourceRoot, $_.Exception.Message)
        return ''
    }
}

function Read-ReleaseContractZipEntryBytes {
    param(
        [Parameter(Mandatory = $true)] $Archive,
        [Parameter(Mandatory = $true)] [string] $EntryName,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $entry = $Archive.GetEntry($EntryName)
    if ($null -eq $entry) {
        Add-ReleaseContractFailure "$Label is missing ZIP entry: $EntryName"
        return $null
    }
    try {
        $stream = $entry.Open()
        try {
            $memory = New-Object System.IO.MemoryStream
            try {
                $stream.CopyTo($memory)
                return $memory.ToArray()
            }
            finally {
                $memory.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }
    catch {
        Add-ReleaseContractFailure ("{0} ZIP entry read failed for {1}: {2}" -f $Label, $EntryName, $_.Exception.Message)
        return $null
    }
}

function ConvertFrom-ReleaseContractJsonBytes {
    param(
        [byte[]] $Bytes,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if ($null -eq $Bytes) {
        return $null
    }
    try {
        $strictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
        return $strictUtf8.GetString($Bytes) | ConvertFrom-Json
    }
    catch {
        Add-ReleaseContractFailure ("{0} JSON parse failed: {1}" -f $Label, $_.Exception.Message)
        return $null
    }
}

function Get-ReleaseContractBytesSha256 {
    param([byte[]] $Bytes)

    if ($null -eq $Bytes) {
        return ''
    }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Assert-ReleaseContractNoWarningOrErrorDiagnostics {
    param(
        $Report,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $blocking = @((Get-ReleaseContractValue $Report 'diagnostics') | Where-Object {
        $severity = [string](Get-ReleaseContractValue $_ 'severity')
        $severity.Equals('warning', [System.StringComparison]::OrdinalIgnoreCase) -or
            $severity.Equals('error', [System.StringComparison]::OrdinalIgnoreCase)
    })
    Assert-ReleaseContractEqual -Label "$Label warning/error diagnostic count" -Actual $blocking.Count -Expected 0
    foreach ($diagnostic in $blocking) {
        Add-ReleaseContractFailure ("{0} emitted {1} {2}: {3} ({4})" -f
            $Label,
            [string](Get-ReleaseContractValue $diagnostic 'severity'),
            [string](Get-ReleaseContractValue $diagnostic 'code'),
            [string](Get-ReleaseContractValue $diagnostic 'message'),
            [string](Get-ReleaseContractValue $diagnostic 'path'))
    }
}

function Test-ReleaseContractAuthorSdkArtifact {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $ArtifactRoot,
        [Parameter(Mandatory = $true)] [string] $ProjectRoot,
        [Parameter(Mandatory = $true)] [string] $UniqueId,
        [Parameter(Mandatory = $true)] [string] $AssemblyName,
        [Parameter(Mandatory = $true)] [string] $EntryDll,
        [Parameter(Mandatory = $true)] [string] $Version,
        [Parameter(Mandatory = $true)] [string] $MinimumDtmApiVersion,
        [Parameter(Mandatory = $true)] [string] $ReferencePolicyId,
        [Parameter(Mandatory = $true)] [string] $HarmonyOwner,
        [Parameter(Mandatory = $true)] [string] $PackageFile,
        [Parameter(Mandatory = $true)] [string] $ExpectedBinaryVersion
    )

    if ([string]::IsNullOrWhiteSpace($ArtifactRoot)) {
        Add-ReleaseContractFailure "$Label requires an explicit tracked Author SDK artifact root."
        return $null
    }

    try {
        $root = [System.IO.Path]::GetFullPath($ArtifactRoot)
    }
    catch {
        Add-ReleaseContractFailure ("{0} artifact root is invalid: {1}" -f $Label, $_.Exception.Message)
        return $null
    }
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        Add-ReleaseContractFailure "$Label artifact root is missing; run the tracked Author SDK product builder first: $root"
        return $null
    }

    $buildReport = Read-ReleaseContractJson -Path (Join-Path $root 'build-report.json')
    $packReport = Read-ReleaseContractJson -Path (Join-Path $root 'pack-report.json')
    $summary = Read-ReleaseContractJson -Path (Join-Path $root 'summary.json')
    $validateReport = Read-ReleaseContractJson -Path (Join-Path $root 'validate-report.json')
    if ($null -eq $buildReport -or $null -eq $packReport -or $null -eq $summary -or $null -eq $validateReport) {
        return $null
    }

    foreach ($reportCase in @(
        [pscustomobject]@{ Name = 'validate'; Report = $validateReport },
        [pscustomobject]@{ Name = 'build'; Report = $buildReport },
        [pscustomobject]@{ Name = 'pack'; Report = $packReport }
    )) {
        Assert-ReleaseContractEqual -Label "$Label $($reportCase.Name) command" -Actual (Get-ReleaseContractValue $reportCase.Report 'command') -Expected $reportCase.Name
        Assert-ReleaseContractEqual -Label "$Label $($reportCase.Name) success" -Actual (Get-ReleaseContractValue $reportCase.Report 'success') -Expected 'True'
        Assert-ReleaseContractEqual -Label "$Label $($reportCase.Name) SDK target Runtime" -Actual (Get-ReleaseContractValue $reportCase.Report 'targetRuntimeVersion') -Expected $MinimumDtmApiVersion
        Assert-ReleaseContractEqual -Label "$Label $($reportCase.Name) project root" -Actual ([System.IO.Path]::GetFullPath([string](Get-ReleaseContractValue $reportCase.Report 'rootPath'))) -Expected ([System.IO.Path]::GetFullPath($ProjectRoot))
        Assert-ReleaseContractNoWarningOrErrorDiagnostics -Report $reportCase.Report -Label "$Label $($reportCase.Name) report"
    }

    $buildValues = Get-ReleaseContractValue $buildReport 'values'
    $packValues = Get-ReleaseContractValue $packReport 'values'
    $currentSourceTreeSha256 = Get-ReleaseContractSourceTreeSha256 -SourceRoot (Join-Path $ProjectRoot 'src')
    Assert-ReleaseContractEqual -Label "$Label SDK build/current source-tree SHA-256" `
        -Actual ([string](Get-ReleaseContractValue $buildValues 'sourceTreeSha256')).ToLowerInvariant() `
        -Expected $currentSourceTreeSha256
    Assert-ReleaseContractEqual -Label "$Label SDK TargetFramework" -Actual (Get-ReleaseContractValue $buildValues 'targetFramework') -Expected 'netstandard2.0'
    Assert-ReleaseContractEqual -Label "$Label SDK assembly name" -Actual (Get-ReleaseContractValue $buildValues 'assemblyName') -Expected $AssemblyName
    Assert-ReleaseContractEqual -Label "$Label SDK CodeModKind" -Actual (Get-ReleaseContractValue $buildValues 'codeModKind') -Expected 'Advanced'
    Assert-ReleaseContractEqual -Label "$Label SDK reference policy" -Actual (Get-ReleaseContractValue $buildValues 'referencePolicyId') -Expected $ReferencePolicyId
    Assert-ReleaseContractEqual -Label "$Label package project kind" -Actual (Get-ReleaseContractValue $packValues 'projectKind') -Expected 'CodeMod'
    Assert-ReleaseContractEqual -Label "$Label package CodeModKind" -Actual (Get-ReleaseContractValue $packValues 'codeModKind') -Expected 'Advanced'
    Assert-ReleaseContractEqual -Label "$Label package UniqueID" -Actual (Get-ReleaseContractValue $packValues 'uniqueID') -Expected $UniqueId
    Assert-ReleaseContractEqual -Label "$Label package Version" -Actual (Get-ReleaseContractValue $packValues 'version') -Expected $Version

    $buildArtifact = Join-Path $root ("build\{0}" -f $EntryDll)
    $packagePath = Join-Path $root $PackageFile
    Assert-ReleaseContractEqual -Label "$Label build report output" -Actual ([System.IO.Path]::GetFullPath([string](Get-ReleaseContractValue $buildReport 'outputPath'))) -Expected ([System.IO.Path]::GetFullPath($buildArtifact))
    Assert-ReleaseContractEqual -Label "$Label pack report output" -Actual ([System.IO.Path]::GetFullPath([string](Get-ReleaseContractValue $packReport 'outputPath'))) -Expected ([System.IO.Path]::GetFullPath($packagePath))
    Assert-ReleaseContractEqual -Label "$Label summary status" -Actual (Get-ReleaseContractValue $summary 'status') -Expected 'Passed'
    Assert-ReleaseContractEqual -Label "$Label summary UniqueID" -Actual (Get-ReleaseContractValue $summary 'uniqueId') -Expected $UniqueId
    Assert-ReleaseContractEqual -Label "$Label summary Version" -Actual (Get-ReleaseContractValue $summary 'version') -Expected $Version
    Assert-ReleaseContractEqual -Label "$Label summary CodeModKind" -Actual (Get-ReleaseContractValue $summary 'codeModKind') -Expected 'Advanced'
    Assert-ReleaseContractEqual -Label "$Label summary reference policy" -Actual (Get-ReleaseContractValue $summary 'policyId') -Expected $ReferencePolicyId
    Assert-ReleaseContractEqual -Label "$Label summary Harmony owner" -Actual (Get-ReleaseContractValue $summary 'harmonyOwner') -Expected $HarmonyOwner
    Assert-ReleaseContractEqual -Label "$Label summary project root" -Actual ([System.IO.Path]::GetFullPath([string](Get-ReleaseContractValue $summary 'projectRoot'))) -Expected ([System.IO.Path]::GetFullPath($ProjectRoot))
    Assert-ReleaseContractEqual -Label "$Label summary package path" -Actual ([System.IO.Path]::GetFullPath([string](Get-ReleaseContractValue $summary 'packagePath'))) -Expected ([System.IO.Path]::GetFullPath($packagePath))
    Assert-ReleaseContractEqual -Label "$Label summary build output" -Actual ([System.IO.Path]::GetFullPath([string](Get-ReleaseContractValue $summary 'buildOutput'))) -Expected ([System.IO.Path]::GetFullPath($buildArtifact))
    Assert-ReleaseContractEqual -Label "$Label bundled native dependency count" -Actual @((Get-ReleaseContractValue $summary 'bundledNativeDependencies')).Count -Expected 0

    $buildHash = Get-ReleaseContractSha256 -Label "$Label SDK build artifact" -Path $buildArtifact
    $packageHash = Get-ReleaseContractSha256 -Label "$Label SDK package" -Path $packagePath
    Assert-ReleaseContractEqual -Label "$Label build report SHA-256" -Actual (Get-ReleaseContractValue $buildReport 'sha256') -Expected $buildHash
    Assert-ReleaseContractEqual -Label "$Label summary entry SHA-256" -Actual (Get-ReleaseContractValue $summary 'entryDllSha256') -Expected $buildHash
    Assert-ReleaseContractEqual -Label "$Label package report SHA-256" -Actual (Get-ReleaseContractValue $packReport 'sha256') -Expected $packageHash
    Assert-ReleaseContractEqual -Label "$Label summary package SHA-256" -Actual ([string](Get-ReleaseContractValue $summary 'packageSha256')).ToLowerInvariant() -Expected $packageHash
    Test-ReleaseContractManagedAssemblyMetadata `
        -Label "$Label SDK-built product" `
        -Path $buildArtifact `
        -ExpectedAssemblyName $AssemblyName `
        -ExpectedAssemblyVersion $ExpectedBinaryVersion `
        -ExpectedFileVersion $ExpectedBinaryVersion `
        -ExpectedInformationalVersion $Version

    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        return $null
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
        try {
            $entryPath = "Content/DTMAPI/$EntryDll"
            $manifestPath = 'Content/DTMAPI/manifest.json'
            $referencePath = 'Content/DTMAPI/dtmapi-advanced-references.json'
            $markerPath = 'Content/DTMAPI/dtmapi-package.json'
            $dllEntries = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase) })
            Assert-ReleaseContractEqual -Label "$Label packaged DLL count" -Actual $dllEntries.Count -Expected 1
            if ($dllEntries.Count -eq 1) {
                Assert-ReleaseContractEqual -Label "$Label packaged entry DLL path" -Actual $dllEntries[0].FullName -Expected $entryPath
            }

            $entryBytes = Read-ReleaseContractZipEntryBytes -Archive $archive -EntryName $entryPath -Label $Label
            $manifestBytes = Read-ReleaseContractZipEntryBytes -Archive $archive -EntryName $manifestPath -Label $Label
            $referenceBytes = Read-ReleaseContractZipEntryBytes -Archive $archive -EntryName $referencePath -Label $Label
            $markerBytes = Read-ReleaseContractZipEntryBytes -Archive $archive -EntryName $markerPath -Label $Label
            $entryHash = Get-ReleaseContractBytesSha256 -Bytes $entryBytes
            $manifestHash = Get-ReleaseContractBytesSha256 -Bytes $manifestBytes
            $referenceHash = Get-ReleaseContractBytesSha256 -Bytes $referenceBytes
            Assert-ReleaseContractEqual -Label "$Label packaged entry/build SHA-256" -Actual $entryHash -Expected $buildHash
            Assert-ReleaseContractEqual -Label "$Label pack report entry SHA-256" -Actual (Get-ReleaseContractValue $packValues 'entryDllSha256') -Expected $entryHash
            Assert-ReleaseContractEqual -Label "$Label pack report manifest SHA-256" -Actual (Get-ReleaseContractValue $packValues 'manifestSha256') -Expected $manifestHash
            Assert-ReleaseContractEqual -Label "$Label pack report reference receipt SHA-256" -Actual (Get-ReleaseContractValue $packValues 'advancedReferenceReceiptSha256') -Expected $referenceHash
            Assert-ReleaseContractEqual -Label "$Label summary reference receipt SHA-256" -Actual (Get-ReleaseContractValue $summary 'advancedReferenceReceiptSha256') -Expected $referenceHash

            $packagedManifest = ConvertFrom-ReleaseContractJsonBytes -Bytes $manifestBytes -Label "$Label packaged manifest"
            $referenceReceipt = ConvertFrom-ReleaseContractJsonBytes -Bytes $referenceBytes -Label "$Label Advanced reference receipt"
            $packageMarker = ConvertFrom-ReleaseContractJsonBytes -Bytes $markerBytes -Label "$Label SDK package marker"
            if ($null -ne $packagedManifest) {
                Assert-ReleaseContractEqual -Label "$Label packaged manifest UniqueID" -Actual (Get-ReleaseContractValue $packagedManifest 'UniqueID') -Expected $UniqueId
                Assert-ReleaseContractEqual -Label "$Label packaged manifest Version" -Actual (Get-ReleaseContractValue $packagedManifest 'Version') -Expected $Version
                Assert-ReleaseContractEqual -Label "$Label packaged manifest EntryDll" -Actual (Get-ReleaseContractValue $packagedManifest 'EntryDll') -Expected $entryPath
                Assert-ReleaseContractEqual -Label "$Label packaged manifest CodeModKind" -Actual (Get-ReleaseContractValue $packagedManifest 'CodeModKind') -Expected 'Advanced'
            }
            if ($null -ne $referenceReceipt) {
                Assert-ReleaseContractEqual -Label "$Label receipt UniqueID" -Actual (Get-ReleaseContractValue $referenceReceipt 'uniqueId') -Expected $UniqueId
                Assert-ReleaseContractEqual -Label "$Label receipt TargetFramework" -Actual (Get-ReleaseContractValue $referenceReceipt 'targetFramework') -Expected 'netstandard2.0'
                Assert-ReleaseContractEqual -Label "$Label receipt reference policy" -Actual (Get-ReleaseContractValue $referenceReceipt 'referencePolicyId') -Expected $ReferencePolicyId
                Assert-ReleaseContractEqual -Label "$Label receipt CodeModKind" -Actual (Get-ReleaseContractValue $referenceReceipt 'codeModKind') -Expected 'Advanced'
                Assert-ReleaseContractEqual -Label "$Label receipt EntryDll" -Actual (Get-ReleaseContractValue $referenceReceipt 'entryDllPath') -Expected $entryPath
                Assert-ReleaseContractEqual -Label "$Label receipt Harmony owner" -Actual (Get-ReleaseContractValue $referenceReceipt 'harmonyOwner') -Expected $HarmonyOwner
                Assert-ReleaseContractEqual -Label "$Label receipt entry SHA-256" -Actual (Get-ReleaseContractValue $referenceReceipt 'entryDllSha256') -Expected $entryHash
                Assert-ReleaseContractEqual -Label "$Label receipt manifest SHA-256" -Actual (Get-ReleaseContractValue $referenceReceipt 'manifestSha256') -Expected $manifestHash
            }
            if ($null -ne $packageMarker) {
                Assert-ReleaseContractEqual -Label "$Label marker authority" -Actual (Get-ReleaseContractValue $packageMarker 'authority') -Expected 'dtmapi-author-sdk-package-binding'
                Assert-ReleaseContractEqual -Label "$Label marker UniqueID" -Actual (Get-ReleaseContractValue $packageMarker 'uniqueId') -Expected $UniqueId
                Assert-ReleaseContractEqual -Label "$Label marker CodeModKind" -Actual (Get-ReleaseContractValue $packageMarker 'codeModKind') -Expected 'Advanced'
                Assert-ReleaseContractEqual -Label "$Label marker entry SHA-256" -Actual (Get-ReleaseContractValue $packageMarker 'entryDllSha256') -Expected $entryHash
                Assert-ReleaseContractEqual -Label "$Label marker manifest SHA-256" -Actual (Get-ReleaseContractValue $packageMarker 'manifestSha256') -Expected $manifestHash
                Assert-ReleaseContractEqual -Label "$Label marker reference receipt SHA-256" -Actual (Get-ReleaseContractValue $packageMarker 'advancedReferenceReceiptSha256') -Expected $referenceHash
            }

            return [pscustomobject]@{
                PackageSha256 = $packageHash
                EntryDllSha256 = $entryHash
                ManifestSha256 = $manifestHash
                ReferenceReceiptSha256 = $referenceHash
                SourceTreeSha256 = [string](Get-ReleaseContractValue $buildValues 'sourceTreeSha256')
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    catch {
        Add-ReleaseContractFailure ("{0} SDK package validation failed: {1}" -f $Label, $_.Exception.Message)
        return $null
    }
}

$catalog = Read-ReleaseContractJson -Path $catalogPath
$publish = Read-ReleaseContractJson -Path $publishPath
if ($null -eq $catalog -or $null -eq $publish) {
    foreach ($failure in @($failures.ToArray())) {
        Write-Host "[FAIL] $failure" -ForegroundColor Red
    }
    exit 1
}

if (-not (Test-Path -LiteralPath $versionAuthorityPath -PathType Leaf)) {
    Add-ReleaseContractFailure "Runtime version authority is missing: $versionAuthorityPath"
    $versionAuthority = $null
}
else {
    try {
        [xml]$versionAuthority = [System.IO.File]::ReadAllText($versionAuthorityPath, [System.Text.Encoding]::UTF8)
    }
    catch {
        Add-ReleaseContractFailure ("Runtime version authority XML parse failed: {0}" -f $_.Exception.Message)
        $versionAuthority = $null
    }
}

$releaseVersion = ''
$binaryVersion = ''
$assemblyCompatibilityVersion = ''
if ($null -ne $versionAuthority) {
    $authoritySchema = Get-ReleaseContractXmlValue -Document $versionAuthority -XPath '/Project/PropertyGroup/DtmApiVersionAuthoritySchema' -Label 'Runtime authority schema'
    $releaseVersion = [string](Get-ReleaseContractXmlValue -Document $versionAuthority -XPath '/Project/PropertyGroup/DtmApiReleaseVersion' -Label 'Runtime release version')
    $binaryVersion = [string](Get-ReleaseContractXmlValue -Document $versionAuthority -XPath '/Project/PropertyGroup/DtmApiBinaryFileVersion' -Label 'Runtime binary version')
    $assemblyCompatibilityVersion = [string](Get-ReleaseContractXmlValue -Document $versionAuthority -XPath '/Project/PropertyGroup/DtmApiAssemblyCompatibilityVersion' -Label 'Runtime assembly compatibility version')
    Assert-ReleaseContractEqual -Label 'Runtime authority schema' -Actual $authoritySchema -Expected '1'
    Assert-ReleaseContractEqual -Label 'Runtime authority release version' -Actual $releaseVersion -Expected '0.5.5'
    Assert-ReleaseContractEqual -Label 'Runtime authority binary version' -Actual $binaryVersion -Expected '0.5.5.0'
    Assert-ReleaseContractEqual -Label 'Runtime authority assembly compatibility version' -Actual $assemblyCompatibilityVersion -Expected '0.5.3.0'
}

$runtime = Get-ReleaseContractValue $catalog 'runtime'
$currentRuntime = Get-ReleaseContractValue $runtime 'currentSourceBaseline'
$futureRuntime = Get-ReleaseContractValue $runtime 'futureTarget'
Assert-ReleaseContractEqual -Label 'Catalog current Runtime release version' -Actual (Get-ReleaseContractValue $currentRuntime 'releaseVersion') -Expected $releaseVersion
Assert-ReleaseContractEqual -Label 'Catalog current Runtime binary version' -Actual (Get-ReleaseContractValue $currentRuntime 'binaryFileVersion') -Expected $binaryVersion
Assert-ReleaseContractEqual -Label 'Catalog current Runtime assembly identity' -Actual (Get-ReleaseContractValue $currentRuntime 'assemblyCompatibilityIdentity') -Expected $assemblyCompatibilityVersion
Assert-ReleaseContractEqual -Label 'Catalog current Runtime source state' -Actual (Get-ReleaseContractValue $currentRuntime 'state') -Expected 'ReleaseCandidateSourceAuthority'
Assert-ReleaseContractEqual -Label 'Catalog blocked Runtime target release version' -Actual (Get-ReleaseContractValue $futureRuntime 'releaseVersion') -Expected $releaseVersion
Assert-ReleaseContractEqual -Label 'Catalog blocked Runtime target binary version' -Actual (Get-ReleaseContractValue $futureRuntime 'binaryFileVersion') -Expected $binaryVersion
Assert-ReleaseContractEqual -Label 'Catalog blocked Runtime target assembly identity' -Actual (Get-ReleaseContractValue $futureRuntime 'assemblyCompatibilityIdentity') -Expected $assemblyCompatibilityVersion

$runtimeAssemblyRoots = [ordered]@{
    'DTMAPI.BepInExBootstrap.dll' = 'src\DTMAPI.BepInExBootstrap'
    'DTMAPI.Abstractions.dll' = 'src\DTMAPI.Abstractions'
    'DTMAPI.Core.dll' = 'src\DTMAPI.Core'
    'DTMAPI.GameBridge.DolocTown.dll' = 'src\DTMAPI.GameBridge.DolocTown'
    'DTMAPI.ModConfigMenu.dll' = 'src\DTMAPI.ModConfigMenu'
}
$runtimeInvariant = Get-ReleaseContractValue $catalog 'playerRuntimePackageInvariant'
$catalogRuntimeAssemblies = @((Get-ReleaseContractValue $runtimeInvariant 'productionAssemblies') | ForEach-Object { [string]$_ } | Sort-Object)
$expectedRuntimeAssemblies = @($runtimeAssemblyRoots.Keys | ForEach-Object { [string]$_ } | Sort-Object)
$runtimeAssemblyDifference = @(Compare-Object -ReferenceObject $expectedRuntimeAssemblies -DifferenceObject $catalogRuntimeAssemblies)
Assert-ReleaseContractTrue -Label ("Catalog Runtime production assembly set mismatch: expected [{0}], got [{1}]" -f ($expectedRuntimeAssemblies -join ', '), ($catalogRuntimeAssemblies -join ', ')) -Condition ($runtimeAssemblyDifference.Count -eq 0 -and $catalogRuntimeAssemblies.Count -eq 5)
foreach ($assemblyFile in @($runtimeAssemblyRoots.Keys)) {
    $assemblyRoot = Join-Path $repo $runtimeAssemblyRoots[$assemblyFile]
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension([string]$assemblyFile)
    $projectPath = Join-Path $assemblyRoot ($projectName + '.csproj')
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        Add-ReleaseContractFailure "Runtime project is missing for QA reverse-reference gate: $projectPath"
    }
    else {
        try {
            [xml]$projectDocument = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
            $qaProjectReferences = @($projectDocument.SelectNodes('/Project/ItemGroup/ProjectReference') | Where-Object {
                [System.IO.Path]::GetFileName([string]$_.Include).Equals('DTMAPI.GameBridge.DolocTown.QA.csproj', [System.StringComparison]::OrdinalIgnoreCase)
            })
            Assert-ReleaseContractEqual -Label "$projectName ProjectReference to QA host count" -Actual $qaProjectReferences.Count -Expected 0
        }
        catch {
            Add-ReleaseContractFailure ("Runtime project QA reverse-reference validation failed for {0}: {1}" -f $projectPath, $_.Exception.Message)
        }
    }
    $artifactPath = Join-Path $assemblyRoot ("bin\{0}\netstandard2.0\{1}" -f $Configuration, $assemblyFile)
    Test-ReleaseContractAssembly `
        -Label "Runtime $assemblyFile" `
        -Path $artifactPath `
        -ExpectedAssemblyName ([System.IO.Path]::GetFileNameWithoutExtension($assemblyFile)) `
        -ExpectedAssemblyVersion $assemblyCompatibilityVersion `
        -ExpectedFileVersion $binaryVersion `
        -ExpectedProductVersion $releaseVersion
    Test-ReleaseContractAssemblyExcludesReference -Label "Runtime $assemblyFile" -Path $artifactPath -ForbiddenAssemblyName 'DTMAPI.GameBridge.DolocTown.QA'
}

$runtimeOutputRoot = Join-Path $repo ("src\DTMAPI.BepInExBootstrap\bin\{0}\netstandard2.0" -f $Configuration)
if (Test-Path -LiteralPath $runtimeOutputRoot -PathType Container) {
    $runtimeOutputDlls = @(Get-ChildItem -LiteralPath $runtimeOutputRoot -File -Filter 'DTMAPI*.dll' | ForEach-Object { $_.Name } | Sort-Object)
    $runtimeOutputDifference = @(Compare-Object -ReferenceObject $expectedRuntimeAssemblies -DifferenceObject $runtimeOutputDlls)
    Assert-ReleaseContractTrue -Label ("Bootstrap Runtime output must contain exactly five DTMAPI DLLs. Expected=[{0}] Actual=[{1}]" -f ($expectedRuntimeAssemblies -join ', '), ($runtimeOutputDlls -join ', ')) -Condition ($runtimeOutputDifference.Count -eq 0 -and $runtimeOutputDlls.Count -eq 5)
}

$qaProjectPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\DTMAPI.GameBridge.DolocTown.QA.csproj'
if (-not (Test-Path -LiteralPath $qaProjectPath -PathType Leaf)) {
    Add-ReleaseContractFailure "Optional QA host project is missing: $qaProjectPath"
}
else {
    try {
        [xml]$qaProjectDocument = [System.IO.File]::ReadAllText($qaProjectPath, [System.Text.Encoding]::UTF8)
        $qaFrameworkNodes = @($qaProjectDocument.SelectNodes('/Project/PropertyGroup/TargetFramework'))
        Assert-ReleaseContractTrue -Label 'QA host must have exactly one netstandard2.0 TargetFramework.' -Condition ($qaFrameworkNodes.Count -eq 1 -and [string]$qaFrameworkNodes[0].InnerText -eq 'netstandard2.0')
    }
    catch {
        Add-ReleaseContractFailure ("QA host project validation failed for {0}: {1}" -f $qaProjectPath, $_.Exception.Message)
    }
    $qaArtifactPath = Join-Path (Split-Path -Parent $qaProjectPath) ("bin\{0}\netstandard2.0\DTMAPI.GameBridge.DolocTown.QA.dll" -f $Configuration)
    Test-ReleaseContractAssembly `
        -Label 'Optional QA host' `
        -Path $qaArtifactPath `
        -ExpectedAssemblyName 'DTMAPI.GameBridge.DolocTown.QA' `
        -ExpectedAssemblyVersion $assemblyCompatibilityVersion `
        -ExpectedFileVersion $binaryVersion `
        -ExpectedProductVersion $releaseVersion
}

$playerDoctorPath = Join-Path $repo 'dist\player-doctor\win-x64\dtmapi-player-doctor.exe'
if (-not (Test-Path -LiteralPath $playerDoctorPath -PathType Leaf)) {
    Add-ReleaseContractFailure "Player Doctor artifact is missing; run build-player-doctor.ps1 first: $playerDoctorPath"
}
else {
    try {
        $playerDoctorInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($playerDoctorPath)
        Assert-ReleaseContractEqual -Label 'Player Doctor FileVersion' -Actual $playerDoctorInfo.FileVersion -Expected $binaryVersion
        Assert-ReleaseContractEqual -Label 'Player Doctor ProductVersion' -Actual $playerDoctorInfo.ProductVersion -Expected $releaseVersion
        Assert-ReleaseContractTrue -Label 'Player Doctor artifact must not be empty.' -Condition ((Get-Item -LiteralPath $playerDoctorPath).Length -gt 0)
    }
    catch {
        Add-ReleaseContractFailure ("Player Doctor metadata read failed: {0}" -f $_.Exception.Message)
    }
}

$products = @((Get-ReleaseContractValue $catalog 'products'))
$publicProducts = @($products | Where-Object { [string](Get-ReleaseContractValue $_ 'role') -eq 'PublishedProduct' })
Assert-ReleaseContractEqual -Label 'Public product count' -Actual $publicProducts.Count -Expected 11

$publishRows = @((Get-ReleaseContractValue $publish 'mods'))
$publishRowsByUniqueId = @{}
foreach ($row in $publishRows) {
    $uniqueId = [string](Get-ReleaseContractValue $row 'uniqueId')
    if ([string]::IsNullOrWhiteSpace($uniqueId)) {
        Add-ReleaseContractFailure 'Publish projection contains an empty uniqueId.'
        continue
    }
    if ($publishRowsByUniqueId.ContainsKey($uniqueId)) {
        Add-ReleaseContractFailure "Publish projection contains a duplicate uniqueId: $uniqueId"
        continue
    }
    $publishRowsByUniqueId[$uniqueId] = $row
}

Assert-ReleaseContractTrue -Label 'Runtime publish projection row is required.' -Condition ($publishRowsByUniqueId.ContainsKey('DTMAPI.Runtime'))
if ($publishRowsByUniqueId.ContainsKey('DTMAPI.Runtime')) {
    $runtimePublishRow = $publishRowsByUniqueId['DTMAPI.Runtime']
    Assert-ReleaseContractEqual -Label 'Runtime publish scope' -Actual (Get-ReleaseContractValue $runtimePublishRow 'scope') -Expected 'runtime'
    Assert-ReleaseContractEqual -Label 'Runtime publish version' -Actual (Get-ReleaseContractValue $runtimePublishRow 'version') -Expected $releaseVersion
    Assert-ReleaseContractEqual -Label 'Runtime publish WorkshopID' -Actual (Get-ReleaseContractValue $runtimePublishRow 'workshopId') -Expected (Get-ReleaseContractValue $runtime 'workshopId')
}

$publicUniqueIds = @($publicProducts | ForEach-Object { [string](Get-ReleaseContractValue $_ 'uniqueId') } | Sort-Object)
$publishedProjectionIds = @($publishRows | Where-Object { [string](Get-ReleaseContractValue $_ 'scope') -eq 'published' } | ForEach-Object { [string](Get-ReleaseContractValue $_ 'uniqueId') } | Sort-Object)
$publicProjectionDifference = @(Compare-Object -ReferenceObject $publicUniqueIds -DifferenceObject $publishedProjectionIds)
Assert-ReleaseContractTrue -Label ("Publish projection public set mismatch: Catalog [{0}], publish [{1}]" -f ($publicUniqueIds -join ', '), ($publishedProjectionIds -join ', ')) -Condition ($publicProjectionDifference.Count -eq 0 -and $publishedProjectionIds.Count -eq 11)

$releaseDefinitionsByUniqueId = @{}
foreach ($definition in @(Get-DtmApiPublishedModDefinitions)) {
    $definitionUniqueId = [string](Get-DtmApiMapValue -Map $definition -Key 'UniqueID' -Default '')
    if (-not [string]::IsNullOrWhiteSpace($definitionUniqueId)) {
        $releaseDefinitionsByUniqueId[$definitionUniqueId] = $definition
    }
}

foreach ($product in $publicProducts) {
    $catalogId = [string](Get-ReleaseContractValue $product 'catalogId')
    $uniqueId = [string](Get-ReleaseContractValue $product 'uniqueId')
    $sourceRoot = Normalize-ReleaseContractPath (Get-ReleaseContractValue $product 'sourceRoot')
    $sourceManifest = Normalize-ReleaseContractPath (Get-ReleaseContractValue $product 'sourceManifest')
    $projectName = [string](Get-ReleaseContractValue $product 'project')
    $sourceDll = [string](Get-ReleaseContractValue $product 'sourceDll')
    $sourceVersion = [string](Get-ReleaseContractValue $product 'sourceVersion')
    $sourceMinimum = [string](Get-ReleaseContractValue $product 'sourceMinimumDtmApiVersion')
    $expectedBinaryVersion = Get-ReleaseContractFourPartVersion -Version $sourceVersion -Label "$catalogId sourceVersion"
    $releaseDefinition = if ($releaseDefinitionsByUniqueId.ContainsKey($uniqueId)) { $releaseDefinitionsByUniqueId[$uniqueId] } else { $null }
    $authorSdkProject = $null -ne $releaseDefinition -and
        (Test-DtmApiMapKey -Map $releaseDefinition -Key 'AuthorSdkProject') -and
        [bool](Get-DtmApiMapValue -Map $releaseDefinition -Key 'AuthorSdkProject' -Default $false)

    Assert-ReleaseContractTrue -Label "$catalogId source root is present in the Catalog." -Condition (-not [string]::IsNullOrWhiteSpace($sourceRoot))
    Assert-ReleaseContractTrue -Label "$catalogId source manifest is present in the Catalog." -Condition (-not [string]::IsNullOrWhiteSpace($sourceManifest))
    Assert-ReleaseContractTrue -Label "$catalogId project is present in the Catalog." -Condition (-not [string]::IsNullOrWhiteSpace($projectName))
    Assert-ReleaseContractTrue -Label "$catalogId source DLL is present in the Catalog." -Condition (-not [string]::IsNullOrWhiteSpace($sourceDll))
    Assert-ReleaseContractTrue -Label "$catalogId current source version is present." -Condition (-not [string]::IsNullOrWhiteSpace($sourceVersion))
    Assert-ReleaseContractTrue -Label "$catalogId current minimum DTMAPI version is present." -Condition (-not [string]::IsNullOrWhiteSpace($sourceMinimum))
    Assert-ReleaseContractTrue -Label "$catalogId retained publishedVersion is recorded independently from current sourceVersion." -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ReleaseContractValue $product 'publishedVersion')))
    Assert-ReleaseContractTrue -Label "$catalogId retained published minimum remains recorded independently from the current source minimum." -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ReleaseContractValue $product 'publishedMinimumDtmApiVersion')))
    Assert-ReleaseContractEqual -Label "$catalogId migration target version" -Actual (Get-ReleaseContractValue $product 'targetVersion') -Expected '1.0.0'
    Assert-ReleaseContractEqual -Label "$catalogId migration target minimum" -Actual (Get-ReleaseContractValue $product 'targetMinimumDtmApiVersion') -Expected $releaseVersion

    $manifestPath = Join-Path $repo $sourceManifest.Replace('/', '\')
    $manifest = Read-ReleaseContractJson -Path $manifestPath
    if ($null -ne $manifest) {
        Assert-ReleaseContractEqual -Label "$catalogId manifest UniqueID" -Actual (Get-ReleaseContractValue $manifest 'UniqueID') -Expected $uniqueId
        Assert-ReleaseContractEqual -Label "$catalogId manifest Version" -Actual (Get-ReleaseContractValue $manifest 'Version') -Expected $sourceVersion
        Assert-ReleaseContractEqual -Label "$catalogId manifest MinimumDTMApiVersion" -Actual (Get-ReleaseContractValue $manifest 'MinimumDTMApiVersion') -Expected $sourceMinimum
        Assert-ReleaseContractEqual -Label "$catalogId manifest EntryDll" -Actual (Get-ReleaseContractValue $manifest 'EntryDll') -Expected $sourceDll
    }

    if (-not $publishRowsByUniqueId.ContainsKey($uniqueId)) {
        Add-ReleaseContractFailure "$catalogId is missing from the publish projection: $uniqueId"
        continue
    }
    $publishRow = $publishRowsByUniqueId[$uniqueId]
    Assert-ReleaseContractEqual -Label "$catalogId publish scope" -Actual (Get-ReleaseContractValue $publishRow 'scope') -Expected 'published'
    Assert-ReleaseContractEqual -Label "$catalogId publish version" -Actual (Get-ReleaseContractValue $publishRow 'version') -Expected $sourceVersion
    $officialInfoRelative = Normalize-ReleaseContractPath (Get-ReleaseContractValue $publishRow 'officialInfoPath')
    Assert-ReleaseContractEqual -Label "$catalogId official-info projection path" -Actual $officialInfoRelative -Expected ($sourceRoot + '/official-info.json')
    $officialInfoPath = Join-Path $repo $officialInfoRelative.Replace('/', '\')
    $officialInfo = Read-ReleaseContractJson -Path $officialInfoPath
    if ($null -ne $officialInfo) {
        Assert-ReleaseContractEqual -Label "$catalogId official-info version" -Actual (Get-ReleaseContractValue $officialInfo 'version') -Expected $sourceVersion
    }

    $projectRelative = $sourceRoot + '/' + $projectName + '.csproj'
    $projectPath = Join-Path $repo $projectRelative.Replace('/', '\')
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        Add-ReleaseContractFailure "$catalogId project file is missing: $projectPath"
        continue
    }

    if ($authorSdkProject) {
        Assert-ReleaseContractEqual -Label "$catalogId Catalog production build authority" -Actual (Get-ReleaseContractValue $product 'productionBuildAuthority') -Expected 'DTMAPI Author SDK build/pack/deploy'
        Assert-ReleaseContractEqual -Label "$catalogId Catalog CodeModKind" -Actual (Get-ReleaseContractValue $product 'codeModKind') -Expected 'Advanced'
        $referencePolicyId = [string](Get-ReleaseContractValue $product 'referencePolicyId')
        $harmonyOwner = [string](Get-ReleaseContractValue $product 'canonicalHarmonyOwner')
        Assert-ReleaseContractTrue -Label "$catalogId Catalog reference policy is required for an AuthorSdkProject." -Condition (-not [string]::IsNullOrWhiteSpace($referencePolicyId))
        Assert-ReleaseContractTrue -Label "$catalogId Catalog Harmony owner is required for an AuthorSdkProject." -Condition (-not [string]::IsNullOrWhiteSpace($harmonyOwner))
        $authorSdkBuildScript = [string](Get-DtmApiMapValue -Map $releaseDefinition -Key 'AuthorSdkBuildScript' -Default '')
        $authorSdkCatalogId = [string](Get-DtmApiMapValue -Map $releaseDefinition -Key 'AuthorSdkCatalogId' -Default '')
        $authorSdkPackageFile = [string](Get-DtmApiMapValue -Map $releaseDefinition -Key 'AuthorSdkPackageFile' -Default '')
        Assert-ReleaseContractEqual -Label "$catalogId shared Author SDK build script" -Actual $authorSdkBuildScript -Expected 'build-batch6-advanced-product.ps1'
        Assert-ReleaseContractTrue -Label "$catalogId tracked Author SDK build script exists." -Condition (Test-Path -LiteralPath (Join-Path $PSScriptRoot $authorSdkBuildScript) -PathType Leaf)
        Assert-ReleaseContractEqual -Label "$catalogId Catalog-driven Author SDK build id" -Actual $authorSdkCatalogId -Expected $catalogId
        Assert-ReleaseContractTrue -Label "$catalogId tracked Author SDK package file is declared." -Condition (-not [string]::IsNullOrWhiteSpace($authorSdkPackageFile))
        $primaryAuthorSdkRoot = if ([string]::IsNullOrWhiteSpace($AuthorSdkArtifactRoot)) { '' } else { Join-Path $AuthorSdkArtifactRoot $catalogId }
        $repeatAuthorSdkRoot = if ([string]::IsNullOrWhiteSpace($AuthorSdkRepeatArtifactRoot)) { '' } else { Join-Path $AuthorSdkRepeatArtifactRoot $catalogId }

        try {
            [xml]$authorProjectDocument = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
            $authorityImports = @($authorProjectDocument.SelectNodes('/Project/Import') | Where-Object {
                [string]$_.Project -eq '$(DTMAPI_AUTHOR_SDK_ROOT)\compatibility\0.5.5\DTMAPI.Author.props'
            })
            Assert-ReleaseContractEqual -Label "$catalogId Author SDK props import count" -Actual $authorityImports.Count -Expected 1
            foreach ($rawAuthorityNode in @('TargetFramework', 'Version', 'InformationalVersion', 'AssemblyVersion', 'FileVersion', 'IncludeSourceRevisionInInformationalVersion')) {
                Assert-ReleaseContractEqual -Label "$catalogId raw csproj $rawAuthorityNode declaration count" -Actual @($authorProjectDocument.SelectNodes("/Project/PropertyGroup/$rawAuthorityNode")).Count -Expected 0
            }
        }
        catch {
            Add-ReleaseContractFailure ("{0} Author SDK project authority validation failed for {1}: {2}" -f $catalogId, $projectPath, $_.Exception.Message)
        }

        $authorProjectPath = Join-Path (Join-Path $repo $sourceRoot.Replace('/', '\')) 'dtmapi.author.json'
        $authorProject = Read-ReleaseContractJson -Path $authorProjectPath
        if ($null -ne $authorProject) {
            Assert-ReleaseContractEqual -Label "$catalogId Author SDK schema" -Actual (Get-ReleaseContractValue $authorProject 'schemaVersion') -Expected '2'
            Assert-ReleaseContractEqual -Label "$catalogId Author SDK project kind" -Actual (Get-ReleaseContractValue $authorProject 'projectKind') -Expected 'CodeMod'
            Assert-ReleaseContractEqual -Label "$catalogId Author SDK target DTMAPI version" -Actual (Get-ReleaseContractValue $authorProject 'targetDtmApiVersion') -Expected $sourceMinimum
            Assert-ReleaseContractEqual -Label "$catalogId Author SDK assembly name" -Actual (Get-ReleaseContractValue $authorProject 'assemblyName') -Expected ([System.IO.Path]::GetFileNameWithoutExtension($sourceDll))
            Assert-ReleaseContractEqual -Label "$catalogId Author SDK CodeModKind" -Actual (Get-ReleaseContractValue $authorProject 'codeModKind') -Expected 'Advanced'
            $advancedIntent = Get-ReleaseContractValue $authorProject 'advanced'
            Assert-ReleaseContractEqual -Label "$catalogId Author SDK reference policy" -Actual (Get-ReleaseContractValue $advancedIntent 'referencePolicyId') -Expected $referencePolicyId
        }

        $primaryArtifact = Test-ReleaseContractAuthorSdkArtifact `
            -Label "$catalogId primary Author SDK artifact" `
            -ArtifactRoot $primaryAuthorSdkRoot `
            -ProjectRoot (Join-Path $repo $sourceRoot.Replace('/', '\')) `
            -UniqueId $uniqueId `
            -AssemblyName ([System.IO.Path]::GetFileNameWithoutExtension($sourceDll)) `
            -EntryDll $sourceDll `
            -Version $sourceVersion `
            -MinimumDtmApiVersion $sourceMinimum `
            -ReferencePolicyId $referencePolicyId `
            -HarmonyOwner $harmonyOwner `
            -PackageFile $authorSdkPackageFile `
            -ExpectedBinaryVersion $expectedBinaryVersion
        $repeatArtifact = Test-ReleaseContractAuthorSdkArtifact `
            -Label "$catalogId repeat Author SDK artifact" `
            -ArtifactRoot $repeatAuthorSdkRoot `
            -ProjectRoot (Join-Path $repo $sourceRoot.Replace('/', '\')) `
            -UniqueId $uniqueId `
            -AssemblyName ([System.IO.Path]::GetFileNameWithoutExtension($sourceDll)) `
            -EntryDll $sourceDll `
            -Version $sourceVersion `
            -MinimumDtmApiVersion $sourceMinimum `
            -ReferencePolicyId $referencePolicyId `
            -HarmonyOwner $harmonyOwner `
            -PackageFile $authorSdkPackageFile `
            -ExpectedBinaryVersion $expectedBinaryVersion
        if ($null -ne $primaryArtifact -and $null -ne $repeatArtifact) {
            Assert-ReleaseContractEqual -Label "$catalogId deterministic package SHA-256" -Actual $repeatArtifact.PackageSha256 -Expected $primaryArtifact.PackageSha256
            Assert-ReleaseContractEqual -Label "$catalogId deterministic entry DLL SHA-256" -Actual $repeatArtifact.EntryDllSha256 -Expected $primaryArtifact.EntryDllSha256
            Assert-ReleaseContractEqual -Label "$catalogId deterministic manifest SHA-256" -Actual $repeatArtifact.ManifestSha256 -Expected $primaryArtifact.ManifestSha256
            Assert-ReleaseContractEqual -Label "$catalogId deterministic Advanced receipt SHA-256" -Actual $repeatArtifact.ReferenceReceiptSha256 -Expected $primaryArtifact.ReferenceReceiptSha256
            Assert-ReleaseContractEqual -Label "$catalogId deterministic source tree SHA-256" -Actual $repeatArtifact.SourceTreeSha256 -Expected $primaryArtifact.SourceTreeSha256
        }
    }
    else {
        try {
            [xml]$projectDocument = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
            $targetFramework = Get-ReleaseContractXmlValue -Document $projectDocument -XPath '/Project/PropertyGroup/TargetFramework' -Label "$catalogId TargetFramework"
            $projectVersion = Get-ReleaseContractXmlValue -Document $projectDocument -XPath '/Project/PropertyGroup/Version' -Label "$catalogId project Version"
            $informationalVersion = Get-ReleaseContractXmlValue -Document $projectDocument -XPath '/Project/PropertyGroup/InformationalVersion' -Label "$catalogId project InformationalVersion"
            $assemblyVersion = Get-ReleaseContractXmlValue -Document $projectDocument -XPath '/Project/PropertyGroup/AssemblyVersion' -Label "$catalogId project AssemblyVersion"
            $fileVersion = Get-ReleaseContractXmlValue -Document $projectDocument -XPath '/Project/PropertyGroup/FileVersion' -Label "$catalogId project FileVersion"
            $includeRevision = Get-ReleaseContractXmlValue -Document $projectDocument -XPath '/Project/PropertyGroup/IncludeSourceRevisionInInformationalVersion' -Label "$catalogId source revision suffix switch"
            Assert-ReleaseContractEqual -Label "$catalogId TargetFramework" -Actual $targetFramework -Expected 'netstandard2.0'
            Assert-ReleaseContractEqual -Label "$catalogId project Version" -Actual $projectVersion -Expected $sourceVersion
            Assert-ReleaseContractEqual -Label "$catalogId project InformationalVersion" -Actual $informationalVersion -Expected $sourceVersion
            Assert-ReleaseContractEqual -Label "$catalogId project AssemblyVersion" -Actual $assemblyVersion -Expected $expectedBinaryVersion
            Assert-ReleaseContractEqual -Label "$catalogId project FileVersion" -Actual $fileVersion -Expected $expectedBinaryVersion
            Assert-ReleaseContractEqual -Label "$catalogId source revision suffix switch" -Actual $includeRevision -Expected 'false'

            $artifactPath = Join-Path (Join-Path $repo $sourceRoot.Replace('/', '\')) ("bin\{0}\{1}\{2}" -f $Configuration, $targetFramework, $sourceDll)
            Test-ReleaseContractAssembly `
                -Label "Product $catalogId" `
                -Path $artifactPath `
                -ExpectedAssemblyName ([System.IO.Path]::GetFileNameWithoutExtension($sourceDll)) `
                -ExpectedAssemblyVersion $expectedBinaryVersion `
                -ExpectedFileVersion $expectedBinaryVersion `
                -ExpectedProductVersion $sourceVersion
        }
        catch {
            Add-ReleaseContractFailure ("{0} project XML/metadata validation failed for {1}: {2}" -f $catalogId, $projectPath, $_.Exception.Message)
        }
    }
}

$oil = @($products | Where-Object { [string](Get-ReleaseContractValue $_ 'catalogId') -eq 'oil' }) | Select-Object -First 1
Assert-ReleaseContractTrue -Label 'Oil Catalog row is required.' -Condition ($null -ne $oil)
if ($null -ne $oil) {
    $oilSourceRoot = Normalize-ReleaseContractPath (Get-ReleaseContractValue $oil 'sourceRoot')
    $oilSourceVersion = [string](Get-ReleaseContractValue $oil 'sourceVersion')
    Assert-ReleaseContractEqual -Label 'Oil current source version' -Actual $oilSourceVersion -Expected '0.3.1-dtmapi'
    Assert-ReleaseContractEqual -Label 'Oil future target version' -Actual (Get-ReleaseContractValue $oil 'targetVersion') -Expected '1.0.0'
    Assert-ReleaseContractTrue -Label 'Oil current and future target versions must remain separate.' -Condition (-not [string]::Equals($oilSourceVersion, [string](Get-ReleaseContractValue $oil 'targetVersion'), [System.StringComparison]::Ordinal))
    Assert-ReleaseContractEqual -Label 'Oil implementation type' -Actual (Get-ReleaseContractValue $oil 'currentImplementationType') -Expected 'OfficialJsonContentPack'
    foreach ($field in @('project', 'sourceDll', 'packageDll', 'sourceMinimumDtmApiVersion', 'targetMinimumDtmApiVersion')) {
        Assert-ReleaseContractTrue -Label "Oil Catalog $field must remain null." -Condition ($null -eq (Get-ReleaseContractValue $oil $field))
    }

    $oilManifestRelative = Normalize-ReleaseContractPath (Get-ReleaseContractValue $oil 'sourceManifest')
    $oilManifest = Read-ReleaseContractJson -Path (Join-Path $repo $oilManifestRelative.Replace('/', '\'))
    if ($null -ne $oilManifest) {
        Assert-ReleaseContractEqual -Label 'Oil manifest UniqueID' -Actual (Get-ReleaseContractValue $oilManifest 'UniqueID') -Expected 'DTMAPI.OilMod'
        Assert-ReleaseContractEqual -Label 'Oil manifest Version' -Actual (Get-ReleaseContractValue $oilManifest 'Version') -Expected $oilSourceVersion
        Assert-ReleaseContractEqual -Label 'Oil manifest Type' -Actual (Get-ReleaseContractValue $oilManifest 'Type') -Expected 'ContentPack'
        Assert-ReleaseContractTrue -Label 'Oil manifest must not declare EntryDll.' -Condition (-not (Test-ReleaseContractProperty $oilManifest 'EntryDll'))
        Assert-ReleaseContractTrue -Label 'Oil manifest must not declare MinimumDTMApiVersion.' -Condition (-not (Test-ReleaseContractProperty $oilManifest 'MinimumDTMApiVersion'))
        Assert-ReleaseContractTrue -Label 'Oil manifest must not declare code dependencies.' -Condition (-not (Test-ReleaseContractProperty $oilManifest 'Dependencies'))
    }

    Assert-ReleaseContractTrue -Label 'Oil publish projection row is required.' -Condition $publishRowsByUniqueId.ContainsKey('DTMAPI.OilMod')
    if ($publishRowsByUniqueId.ContainsKey('DTMAPI.OilMod')) {
        $oilPublishRow = $publishRowsByUniqueId['DTMAPI.OilMod']
        Assert-ReleaseContractEqual -Label 'Oil publish scope' -Actual (Get-ReleaseContractValue $oilPublishRow 'scope') -Expected 'developerOfficial'
        Assert-ReleaseContractEqual -Label 'Oil publish current version' -Actual (Get-ReleaseContractValue $oilPublishRow 'version') -Expected $oilSourceVersion
        $oilInfoRelative = Normalize-ReleaseContractPath (Get-ReleaseContractValue $oilPublishRow 'officialInfoPath')
        Assert-ReleaseContractEqual -Label 'Oil official-info path' -Actual $oilInfoRelative -Expected ($oilSourceRoot + '/official-info.json')
        $oilInfo = Read-ReleaseContractJson -Path (Join-Path $repo $oilInfoRelative.Replace('/', '\'))
        if ($null -ne $oilInfo) {
            Assert-ReleaseContractEqual -Label 'Oil official-info current version' -Actual (Get-ReleaseContractValue $oilInfo 'version') -Expected $oilSourceVersion
        }
    }

    $oilRootPath = Join-Path $repo $oilSourceRoot.Replace('/', '\')
    $oilCodeOrDll = @(Get-ChildItem -LiteralPath $oilRootPath -Recurse -File | Where-Object {
        $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Extension -in @('.dll', '.cs', '.csproj')
    })
    Assert-ReleaseContractEqual -Label 'Oil ContentOnly code/DLL source count' -Actual $oilCodeOrDll.Count -Expected 0
}

if ($failures.Count -gt 0) {
    Write-Host ("DTMAPI Batch 2 release contract checks: FAILED ({0})" -f $failures.Count) -ForegroundColor Red
    foreach ($failure in @($failures.ToArray())) {
        Write-Host "[FAIL] $failure" -ForegroundColor Red
    }
    exit 1
}

if (-not $Quiet) {
    Write-Host ("DTMAPI Batch 2 release contract checks: OK (runtime-assemblies={0}, public-products={1}, oil=current-{2}/future-1.0.0)" -f $runtimeAssemblyRoots.Count, $publicProducts.Count, '0.3.1-dtmapi') -ForegroundColor Green
}
exit 0
