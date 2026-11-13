param(
    [string] $ProductsPath = '',
    [string] $ProductSourceRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:DtmApiProductsPath = $ProductsPath
$script:DtmApiProductSourceRoot = if ($ProductSourceRoot) { [IO.Path]::GetFullPath($ProductSourceRoot) } else { [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')) }

function Import-DtmApiRuntimeVersionAuthority {
    $candidates = @(
        (Join-Path $PSScriptRoot 'dtmapi-runtime-version.props'),
        (Join-Path (Split-Path -Parent $PSScriptRoot) 'dtmapi-runtime-version.props')
    )
    $path = @($candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1)
    if ($path.Count -ne 1) {
        throw "Missing DTMAPI Runtime version authority. Expected dtmapi-runtime-version.props beside release-common.ps1 or under build."
    }

    [xml] $document = Get-Content -Raw -Encoding UTF8 -LiteralPath $path[0]
    $schemaNode = $document.SelectSingleNode('/Project/PropertyGroup/DtmApiVersionAuthoritySchema')
    $releaseNode = $document.SelectSingleNode('/Project/PropertyGroup/DtmApiReleaseVersion')
    $binaryNode = $document.SelectSingleNode('/Project/PropertyGroup/DtmApiBinaryFileVersion')
    $assemblyNode = $document.SelectSingleNode('/Project/PropertyGroup/DtmApiAssemblyCompatibilityVersion')
    if ($null -eq $schemaNode -or [string]$schemaNode.InnerText -ne '1') {
        throw "Unsupported DTMAPI Runtime version authority schema in $($path[0])."
    }
    if ($null -eq $releaseNode -or [string]::IsNullOrWhiteSpace([string]$releaseNode.InnerText)) {
        throw "DTMAPI Runtime version authority has no release version: $($path[0])"
    }
    if ($null -eq $binaryNode -or [string]$binaryNode.InnerText -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "DTMAPI Runtime version authority has an invalid numeric binary version: $($path[0])"
    }
    if ($null -eq $assemblyNode -or [string]$assemblyNode.InnerText -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "DTMAPI Runtime version authority has an invalid assembly compatibility version: $($path[0])"
    }

    return [pscustomobject]@{
        Path = [System.IO.Path]::GetFullPath([string]$path[0])
        ReleaseVersion = [string]$releaseNode.InnerText
        BinaryFileVersion = [string]$binaryNode.InnerText
        AssemblyCompatibilityVersion = [string]$assemblyNode.InnerText
    }
}

$script:DtmApiVersionAuthority = Import-DtmApiRuntimeVersionAuthority
$script:DtmApiReleaseVersion = $script:DtmApiVersionAuthority.ReleaseVersion
$script:DtmApiBinaryVersion = $script:DtmApiVersionAuthority.BinaryFileVersion
$script:DtmApiAssemblyCompatibilityVersion = $script:DtmApiVersionAuthority.AssemblyCompatibilityVersion
$script:DtmApiInstallScriptVersion = '2'

function Write-Utf8NoBomJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value,
        [int] $Depth = 12
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $compactJson = $Value | ConvertTo-Json -Depth $Depth -Compress
    $json = ConvertTo-DtmApiDeterministicPrettyJson -CompactJson $compactJson
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function ConvertTo-DtmApiDeterministicPrettyJson {
    param(
        [Parameter(Mandatory = $true)] [string] $CompactJson
    )

    # Windows PowerShell 5.1 and PowerShell 7 serialize the same JSON graph with
    # different pretty-print whitespace. Their compressed representation is
    # byte-identical for the supported release metadata. Reformat that compact
    # representation ourselves so tracked package bytes do not depend on the
    # developer's PowerShell host.
    $builder = New-Object System.Text.StringBuilder
    $closingTokens = New-Object 'System.Collections.Generic.Stack[char]'
    $indent = 0
    $inString = $false
    $escaped = $false
    for ($index = 0; $index -lt $CompactJson.Length; $index++) {
        $character = $CompactJson[$index]
        if ($inString) {
            $builder.Append($character) | Out-Null
            if ($escaped) {
                $escaped = $false
            }
            elseif ($character -eq '\') {
                $escaped = $true
            }
            elseif ($character -eq '"') {
                $inString = $false
            }
            continue
        }

        if ($character -eq '"') {
            $inString = $true
            $builder.Append($character) | Out-Null
            continue
        }
        if ($character -eq '{' -or $character -eq '[') {
            $builder.Append($character) | Out-Null
            $closing = if ($character -eq '{') { '}' } else { ']' }
            if ($index + 1 -lt $CompactJson.Length -and $CompactJson[$index + 1] -eq $closing) {
                $builder.Append($closing) | Out-Null
                $index++
                continue
            }
            $closingTokens.Push([char]$closing)
            $indent = $closingTokens.Count
            $builder.Append([Environment]::NewLine) | Out-Null
            $builder.Append(' ' * ($indent * 2)) | Out-Null
            continue
        }
        if ($character -eq '}' -or $character -eq ']') {
            if ($closingTokens.Count -eq 0 -or $closingTokens.Peek() -ne $character) {
                throw "Deterministic JSON formatting encountered an unmatched closing token '$character'."
            }
            [void]$closingTokens.Pop()
            $indent = $closingTokens.Count
            $builder.Append([Environment]::NewLine) | Out-Null
            $builder.Append(' ' * ($indent * 2)) | Out-Null
            $builder.Append($character) | Out-Null
            continue
        }
        if ($character -eq ',') {
            $builder.Append($character) | Out-Null
            $builder.Append([Environment]::NewLine) | Out-Null
            $builder.Append(' ' * ($indent * 2)) | Out-Null
            continue
        }
        if ($character -eq ':') {
            $builder.Append(': ') | Out-Null
            continue
        }
        if (-not [char]::IsWhiteSpace($character)) {
            $builder.Append($character) | Out-Null
        }
    }

    if ($inString -or $escaped -or $closingTokens.Count -ne 0 -or $indent -ne 0) {
        throw 'Deterministic JSON formatting ended in an invalid parser state.'
    }
    return $builder.ToString()
}

function Write-Utf8BomTextFile {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Text
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($Path, $Text, $utf8Bom)
}

function Copy-DtmApiTextFileUtf8Bom {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Missing text source file: $Source"
    }

    $bytes = [System.IO.File]::ReadAllBytes($Source)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $parent = Split-Path -Parent $Destination
        if ($parent) {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
        }

        [System.IO.File]::WriteAllBytes($Destination, $bytes)
        return
    }

    $text = [System.IO.File]::ReadAllText($Source, [System.Text.Encoding]::UTF8)
    Write-Utf8BomTextFile -Path $Destination -Text $text
}

function Remove-DtmApiWorkshopDownloadMarkers {
    param(
        [Parameter(Mandatory = $true)] [string] $PackageRoot
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($PackageRoot)
    if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        throw "Workshop package root is missing: $resolvedRoot"
    }

    $getItemCommand = Get-Command Get-Item -ErrorAction Stop
    $removeItemCommand = Get-Command Remove-Item -ErrorAction Stop
    if (-not $getItemCommand.Parameters.ContainsKey('Stream') -or
        -not $removeItemCommand.Parameters.ContainsKey('Stream')) {
        throw 'The current PowerShell file-system provider cannot verify or remove alternate data streams from a Workshop package.'
    }

    $downloadMarkers = New-Object 'System.Collections.Generic.List[object]'
    foreach ($file in @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force -File)) {
        $alternateStreams = @(Get-Item -LiteralPath $file.FullName -Stream * -ErrorAction Stop |
            Where-Object { [string]$_.Stream -cne ':$DATA' })
        foreach ($stream in $alternateStreams) {
            $streamName = [string]$stream.Stream
            if (-not [string]::Equals($streamName, 'Zone.Identifier', [System.StringComparison]::Ordinal)) {
                throw "Workshop package contains an unexpected alternate data stream: $($file.FullName):$streamName"
            }

            $downloadMarkers.Add([pscustomobject]@{
                Path = $file.FullName
                Stream = $streamName
            }) | Out-Null
        }
    }

    foreach ($marker in @($downloadMarkers.ToArray())) {
        Remove-Item -LiteralPath $marker.Path -Stream $marker.Stream -Force -ErrorAction Stop
    }

    foreach ($file in @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force -File)) {
        $remaining = @(Get-Item -LiteralPath $file.FullName -Stream * -ErrorAction Stop |
            Where-Object { [string]$_.Stream -cne ':$DATA' })
        if ($remaining.Count -gt 0) {
            throw "Workshop package retained an alternate data stream after normalization: $($file.FullName):$($remaining[0].Stream)"
        }
    }
}

function Test-DtmApiWindowsPowerShellSyntax {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Paths,
        [switch] $WarningOnly,
        [switch] $PassThru,
        [switch] $AllowCoreFallback
    )

    $hosts = New-Object 'System.Collections.Generic.List[string]'
    $seenHosts = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $candidateHosts = New-Object 'System.Collections.Generic.List[string]'
    $candidateHosts.Add((Get-DtmApiPowerShellHost -RequireWindowsPowerShell)) | Out-Null
    if ($AllowCoreFallback) {
        if ($PSVersionTable.PSEdition -eq 'Core') {
            try {
                $currentHost = (Get-Process -Id $PID -ErrorAction Stop).Path
                if ($currentHost) {
                    $candidateHosts.Add($currentHost) | Out-Null
                }
            }
            catch {
            }
        }

        $pwsh = Get-Command pwsh.exe -ErrorAction SilentlyContinue
        if ($pwsh) {
            $candidateHosts.Add($pwsh.Source) | Out-Null
        }

        foreach ($candidateRoot in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
            if ([string]::IsNullOrWhiteSpace($candidateRoot)) {
                continue
            }

            $candidate = Join-Path $candidateRoot 'PowerShell\7\pwsh.exe'
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $candidateHosts.Add($candidate) | Out-Null
            }
        }
    }

    foreach ($candidate in @($candidateHosts.ToArray())) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        try {
            $fullCandidate = if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                [System.IO.Path]::GetFullPath($candidate)
            }
            else {
                [string]$candidate
            }
            if ($seenHosts.Add($fullCandidate)) {
                $hosts.Add($fullCandidate) | Out-Null
            }
        }
        catch {
            if ($seenHosts.Add([string]$candidate)) {
                $hosts.Add([string]$candidate) | Out-Null
            }
        }
    }

    if ($hosts.Count -eq 0) {
        Write-Warning "PowerShell host was not found; skipping syntax validation."
        if ($PassThru) {
            return @([pscustomobject]@{
                Path = ''
                Ok = $false
                Status = 'skipped'
                Message = 'PowerShell host was not found'
            })
        }
        return
    }

    $validatorScript = @"
param(
    [Parameter(Mandatory = `$true)] [string] `$Path
)

`$ErrorActionPreference = 'Stop'
try {
    `$tokens = `$null
    `$errors = `$null
    [System.Management.Automation.Language.Parser]::ParseFile(`$Path, [ref] `$tokens, [ref] `$errors) | Out-Null
    if (`$errors -and `$errors.Count -gt 0) {
        foreach (`$err in `$errors) {
            "{0}:{1} {2}" -f `$err.Extent.StartLineNumber, `$err.Extent.StartColumnNumber, `$err.Message
        }
        exit 1
    }
    exit 0
}
catch {
    "HOST_ERROR {0}" -f `$_.Exception.Message
    exit 2
}
"@
    $validatorPath = Join-Path ([System.IO.Path]::GetTempPath()) ("dtmapi-ps-parser-{0}.ps1" -f ([guid]::NewGuid().ToString('N')))
    Write-Utf8BomTextFile -Path $validatorPath -Text $validatorScript

    try {
        $results = New-Object 'System.Collections.Generic.List[object]'
        foreach ($path in $Paths) {
            $resolved = [System.IO.Path]::GetFullPath($path)
            if (-not (Test-Path -LiteralPath $resolved)) {
                $message = "PowerShell syntax validation target is missing: $resolved"
                if ($WarningOnly) {
                    Write-Warning $message
                    $results.Add([pscustomobject]@{
                        Path = $resolved
                        Ok = $false
                        Status = 'missing'
                        Message = $message
                    }) | Out-Null
                    continue
                }

                throw $message
            }

            $success = $false
            $attempts = New-Object 'System.Collections.Generic.List[object]'
            $validatedHost = ''
            foreach ($powershell in @($hosts.ToArray())) {
                $validationOutput = @(& $powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -Path $resolved 2>&1 | ForEach-Object { [string]$_ })
                $validationExitCode = $LASTEXITCODE
                if ($validationExitCode -eq 0) {
                    $success = $true
                    $validatedHost = $powershell
                    break
                }

                $attempts.Add([pscustomobject]@{
                    Host = $powershell
                    ExitCode = $validationExitCode
                    Output = @($validationOutput)
                }) | Out-Null
            }

            if ($success) {
                if ($AllowCoreFallback -and $hosts.Count -gt 0 -and
                    -not [string]::Equals($validatedHost, $hosts[0], [System.StringComparison]::OrdinalIgnoreCase)) {
                    Write-Host "[INFO] Windows PowerShell validation failed or was unavailable for '$resolved'; validated with fallback host '$validatedHost'."
                    foreach ($attempt in @($attempts.ToArray())) {
                        Write-Host ("[INFO]   failed host: {0} exit={1}" -f $attempt.Host, $attempt.ExitCode)
                        foreach ($line in @($attempt.Output | Select-Object -First 5)) {
                            $text = ([string]$line).Trim()
                            if (-not [string]::IsNullOrWhiteSpace($text)) {
                                Write-Host ("[INFO]     {0}" -f $text)
                            }
                        }
                    }
                }

                $results.Add([pscustomobject]@{
                    Path = $resolved
                    Ok = $true
                    Status = 'ok'
                    Message = ''
                    Host = $validatedHost
                }) | Out-Null
                continue
            }

            $message = "PowerShell syntax validation failed for $resolved"
            $details = New-Object 'System.Collections.Generic.List[string]'
            foreach ($attempt in @($attempts.ToArray())) {
                $details.Add(("Host: {0} exit={1}" -f $attempt.Host, $attempt.ExitCode)) | Out-Null
                foreach ($line in @($attempt.Output | Select-Object -First 8)) {
                    $text = ([string]$line).Trim()
                    if (-not [string]::IsNullOrWhiteSpace($text)) {
                        $details.Add(("  {0}" -f $text)) | Out-Null
                    }
                }
            }

            if ($WarningOnly) {
                Write-Warning $message
                foreach ($line in @($details.ToArray() | Select-Object -First 12)) {
                    Write-Warning ("  {0}" -f $line)
                }
                $results.Add([pscustomobject]@{
                    Path = $resolved
                    Ok = $false
                    Status = 'syntax-error'
                    Message = $message
                    Details = @($details.ToArray())
                }) | Out-Null
                continue
            }

            Write-Error $message -ErrorAction Continue
            foreach ($line in @($details.ToArray() | Select-Object -First 20)) {
                Write-Error ("  {0}" -f $line) -ErrorAction Continue
            }
            $finalMessage = 'Script cannot run in the current PowerShell environment. 脚本无法在当前 PowerShell 运行。Please include the parser/host details above when asking for help.'
            Write-Error $finalMessage -ErrorAction Continue
            throw ($message + "`n" + (($details.ToArray()) -join "`n") + "`n" + $finalMessage)
        }
    }
    finally {
        if (Test-Path -LiteralPath $validatorPath -PathType Leaf) {
            Remove-Item -LiteralPath $validatorPath -Force -ErrorAction SilentlyContinue
        }
    }

    if ($PassThru) {
        return @($results.ToArray())
    }
}

function Get-DtmApiSourceCommit {
    param(
        [string] $RepoRoot
    )

    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $RepoRoot -or -not $git) {
        return ''
    }

    try { $commit = & $git.Source -C $RepoRoot rev-parse --short=12 HEAD 2>$null }
    catch { return '' }
    if ($LASTEXITCODE -ne 0) {
        return ''
    }

    return ($commit | Select-Object -First 1)
}

function Get-DtmApiPersistentRoot {
    if ($env:DTMAPI_DOLOC_PERSISTENT_ROOT) {
        return [System.IO.Path]::GetFullPath($env:DTMAPI_DOLOC_PERSISTENT_ROOT)
    }

    return Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\RedSawGames\DolocTown'
}

function Get-DtmApiReleaseProducts {
    if (-not $script:DtmApiProductsPath) {
        throw 'Pass -ProductsPath to the release list.'
    }
    return @(Get-Content -Raw -Encoding UTF8 -LiteralPath $script:DtmApiProductsPath | ConvertFrom-Json)
}

function Get-DtmApiProductDefinitions {
    foreach ($product in @(Get-DtmApiReleaseProducts)) {
        $sourceRoot = Join-Path $script:DtmApiProductSourceRoot $product.sourceRoot
        $manifestPath = Join-Path $sourceRoot 'manifest.json'
        # Runtime packages have their own builders and no Mod manifest.
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { continue }
        $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
        $info = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $sourceRoot 'info.json') | ConvertFrom-Json
        $project = Get-ChildItem -LiteralPath $sourceRoot -Filter '*.csproj' -File | Select-Object -First 1
        $entryDll = [string](Get-DtmApiMapValue $manifest 'EntryDll' '')
        $packageDll = [IO.Path]::GetFileName($entryDll.Replace('\', '/'))
        $sourceDll = $packageDll
        if ($project) {
            [xml]$projectXml = Get-Content -Raw -Encoding UTF8 -LiteralPath $project.FullName
            $assemblyName = $projectXml.SelectSingleNode('/Project/PropertyGroup/AssemblyName')
            $sourceDll = $(if ($assemblyName) { $assemblyName.InnerText } else { $project.BaseName }) + '.dll'
        }
        [ordered]@{
            SourceRoot = [string]$product.sourceRoot
            WorkshopId = [string]$product.workshopId
            OfficialFolder = [string]$product.uploadFolder
            PackageName = [string]$product.uploadFolder
            UniqueID = [string]$manifest.UniqueID
            DisplayName = [string]$info.name
            Project = $(if ($project) { $project.BaseName } else { '' })
            SourceDll = $sourceDll
            PackageDll = $packageDll
            ContentOnly = [string]$manifest.Type -eq 'ContentPack'
        }
    }
}

function Test-DtmApiMapKey {
    param(
        [Parameter(Mandatory = $true)] $Map,
        [Parameter(Mandatory = $true)] [string] $Key
    )

    if ($Map -is [System.Collections.IDictionary]) {
        return $Map.Contains($Key)
    }

    return $null -ne $Map.PSObject.Properties[$Key]
}

function Get-DtmApiMapValue {
    param(
        [Parameter(Mandatory = $true)] $Map,
        [Parameter(Mandatory = $true)] [string] $Key,
        $Default = $null
    )

    if ($Map -is [System.Collections.IDictionary]) {
        if ($Map.Contains($Key)) {
            return $Map[$Key]
        }

        return $Default
    }

    $property = $Map.PSObject.Properties[$Key]
    if ($property) {
        return $property.Value
    }

    return $Default
}

function Get-DtmApiLegacyOfficialLocalMetadata {
    param(
        [string] $PersistentRoot = (Get-DtmApiPersistentRoot)
    )

    $modsRoot = Join-Path $PersistentRoot 'MODS'
    if (-not (Test-Path -LiteralPath $modsRoot -PathType Container)) {
        return @()
    }

    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($dir in @(Get-ChildItem -LiteralPath $modsRoot -Directory -ErrorAction SilentlyContinue)) {
        $marker = Join-Path $dir.FullName 'Content\DTMAPI\dtmapi-package.json'
        if (-not (Test-Path -LiteralPath $marker -PathType Leaf)) {
            continue
        }

        $uniqueId = ''
        $version = ''
        try {
            $markerData = Get-Content -Raw -Encoding UTF8 -LiteralPath $marker | ConvertFrom-Json
            if ($markerData.PSObject.Properties['uniqueId']) {
                $uniqueId = [string]$markerData.uniqueId
            }
        }
        catch {
            $uniqueId = ''
        }

        $manifest = Join-Path $dir.FullName 'Content\DTMAPI\manifest.json'
        if (Test-Path -LiteralPath $manifest -PathType Leaf) {
            try {
                $manifestData = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifest | ConvertFrom-Json
                if (-not $uniqueId -and $manifestData.PSObject.Properties['UniqueID']) {
                    $uniqueId = [string]$manifestData.UniqueID
                }
                if ($manifestData.PSObject.Properties['Version']) {
                    $version = [string]$manifestData.Version
                }
            }
            catch {
                $version = ''
            }
        }

        $items.Add([ordered]@{
            OfficialFolder = $dir.Name
            Path = [System.IO.Path]::GetFullPath($dir.FullName)
            MetadataPath = [System.IO.Path]::GetFullPath($marker)
            UniqueID = $uniqueId
            Version = $version
            Classification = 'LegacyMetadataOnly'
            Authority = 'None'
            DestructiveActionAllowed = $false
        }) | Out-Null
    }

    return $items.ToArray()
}

function Add-DtmApiLegacyDetection {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [System.Collections.Generic.List[object]] $Items,
        [Parameter(Mandatory = $true)] [string] $Kind,
        [Parameter(Mandatory = $true)] [string] $Path,
        [string] $Action = 'detected-only',
        [string] $Notes = ''
    )

    if (-not (Test-Path $Path)) {
        return
    }

    $Items.Add([ordered]@{
        Kind = $Kind
        Path = [System.IO.Path]::GetFullPath($Path)
        Action = $Action
        Notes = $Notes
    }) | Out-Null
}

function Get-DtmApiLegacyDetections {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [string] $PersistentRoot = (Get-DtmApiPersistentRoot)
    )

    $items = New-Object 'System.Collections.Generic.List[object]'
    Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-smapi-plugin' -Path (Join-Path $GameDir 'BepInEx\plugins\DolocTownSMAPI') -Notes 'Old DolocTown SMAPI plugin directory.'
    Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-smapi-runtime' -Path (Join-Path $GameDir 'BepInEx\DolocTownSMAPI') -Notes 'Old DolocTown SMAPI runtime directory.'
    Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-workshop-bridge' -Path (Join-Path $GameDir 'BepInEx\plugins\DLKWorkshopBridge') -Notes 'Old DLK Workshop bridge plugin directory.'

    $gameMods = Join-Path $GameDir 'Mods'
    foreach ($id in @('Yuuka.DTMAPI.ActionSpeed', 'Yuuka.DTMAPI.AutoFishing', 'Yuuka.DTMAPI.OneActionComplete', 'Yuuka.DTMAPI.FishBreedingAssistant', 'Yuuka.DTMAPI.AnimalHusbandryProgress', 'DTMAPI.HookProbeMod', 'DTMAPI.HelloDtmMod', 'DTMAPI.ConfigMenuExample')) {
        Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-game-mod' -Path (Join-Path $gameMods $id) -Notes 'Old local Mods/ package; installer may back up migrated local packages but will not delete Workshop content.'
    }

    $localMods = Join-Path $PersistentRoot 'MODS'
    if (Test-Path $localMods) {
        foreach ($dir in @(Get-ChildItem -LiteralPath $localMods -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'DLK_*' })) {
            Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-local-dlk-mod' -Path $dir.FullName -Notes 'Old local DLK_* official-local package; detected for migration guidance.'
        }
    }

    $workshopRoot = [System.IO.Path]::GetFullPath((Join-Path $GameDir '..\..\workshop\content\2285550'))
    $legacyWorkshopIds = @(
        @{ Id = '3726044511'; Name = 'DolocTown SMAPI runtime' },
        @{ Id = '3728035966'; Name = 'old AutoFishing' },
        @{ Id = '3728245966'; Name = 'old Y console candidate' },
        @{ Id = '3728240703'; Name = 'old OneActionComplete' },
        @{ Id = '3728240789'; Name = 'old ActionSpeed' },
        @{ Id = '3729655857'; Name = 'old FishBreedingAssistant' },
        @{ Id = '3729757101'; Name = 'old AnimalHusbandryProgress' }
    )
    foreach ($entry in $legacyWorkshopIds) {
        Add-DtmApiLegacyDetection -Items $items -Kind 'legacy-workshop-cache' -Path (Join-Path $workshopRoot $entry.Id) -Notes ($entry.Name + '; detected only, never deleted by DTMAPI.')
    }

    return $items.ToArray()
}

function New-DtmApiReleaseManifest {
    param(
        [string] $RepoRoot = '',
        [string] $PackageKind = 'local-install',
        [object[]] $IncludedAssemblies = @(),
        [object[]] $OptionalComponents = @(),
        [object[]] $BundledMods = @(),
        [string] $BuildCommit = ''
    )

    $commit = if ([string]::IsNullOrWhiteSpace($BuildCommit)) {
        Get-DtmApiSourceCommit -RepoRoot $RepoRoot
    }
    else {
        $BuildCommit.Trim()
    }
    [ordered]@{
        SchemaVersion = 1
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        BuildCommit = $commit
        BuildTime = (Get-Date).ToUniversalTime().ToString('o')
        PackageKind = $PackageKind
        SupportedGameVersion = 'Doloc Town Windows Steam build supported by the current Refactor smoke evidence'
        MinimumGameVersion = ''
        IncludedAssemblies = @($IncludedAssemblies)
        OptionalComponents = @($OptionalComponents)
        BundledMods = @($BundledMods)
        ExperimentalApiNotice = "DTMAPI $script:DtmApiReleaseVersion is a Developer Preview. Player-facing mods in this package are intended to be stable for normal use, but most GameBridge gameplay APIs remain Experimental for mod developers."
    }
}

function New-DtmApiInstallState {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $PluginDir,
        [object[]] $FilesInstalled = @(),
        [object[]] $OptionalComponents = @(),
        [bool] $BepInExDetectedBeforeInstall = $false,
        [bool] $BepInExInstalledByDTMAPI = $false,
        [object[]] $BackupsCreated = @(),
        [object[]] $LegacyModsMoved = @(),
        [object[]] $LegacyDetections = @(),
        [object[]] $QaFixturesInstalled = @(),
        [bool] $DryRun = $false,
        [string] $SourceRepoCommit = ''
    )

    $commit = if ([string]::IsNullOrWhiteSpace($SourceRepoCommit)) {
        Get-DtmApiSourceCommit -RepoRoot $RepoRoot
    }
    else {
        $SourceRepoCommit.Trim()
    }
    [ordered]@{
        SchemaVersion = 1
        InstalledAt = (Get-Date).ToUniversalTime().ToString('o')
        DTMAPIVersion = $script:DtmApiReleaseVersion
        BinaryVersion = $script:DtmApiBinaryVersion
        SourceRepoCommit = $commit
        GameDir = [System.IO.Path]::GetFullPath($GameDir)
        PluginDir = [System.IO.Path]::GetFullPath($PluginDir)
        FilesInstalled = @($FilesInstalled)
        OptionalComponents = @($OptionalComponents)
        BepInExDetectedBeforeInstall = $BepInExDetectedBeforeInstall
        BepInExInstalledByDTMAPI = $BepInExInstalledByDTMAPI
        BackupsCreated = @($BackupsCreated)
        LegacyModsMoved = @($LegacyModsMoved)
        LegacyDetections = @($LegacyDetections)
        QaFixturesInstalledCount = @($QaFixturesInstalled).Count
        QaFixturesInstalled = @($QaFixturesInstalled)
        InstallScriptVersion = $script:DtmApiInstallScriptVersion
        DryRun = $DryRun
    }
}
