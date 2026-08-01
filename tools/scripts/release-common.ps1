Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Import-DtmApiRuntimeVersionAuthority {
    $candidates = @(
        (Join-Path $PSScriptRoot 'dtmapi-runtime-version.props'),
        (Join-Path (Split-Path -Parent $PSScriptRoot) 'release\dtmapi-runtime-version.props')
    )
    $path = @($candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1)
    if ($path.Count -ne 1) {
        throw "Missing DTMAPI Runtime version authority. Expected dtmapi-runtime-version.props beside release-common.ps1 or under tools/release."
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

    $json = $Value | ConvertTo-Json -Depth $Depth
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
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

    if (-not $RepoRoot -or -not (Test-Path (Join-Path $RepoRoot '.git'))) {
        return ''
    }

    $commit = & git -C $RepoRoot rev-parse --short=12 HEAD 2>$null
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

function Get-DtmApiPublishedModDefinitions {
    return @(
        [ordered]@{
            OfficialFolder = 'DTMAPI_Zoom'
            Project = 'DTMAPI.Zoom'
            SourceRoot = 'products\first-party\Zoom'
            SourceDll = 'DTMAPI.Zoom.dll'
            PackageDll = 'DTMAPI.Zoom.dll'
            UniqueID = 'DTMAPI.ZoomMod'
            DisplayName = 'DTMAPI Zoom'
            PackageName = 'DTMAPI-Zoom'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'zoom'
            AuthorSdkPackageFile = 'DTMAPI-Zoom-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_ActionSpeed'
            Project = 'Yuuka.DTMAPI.ActionSpeed'
            SourceRoot = 'products\first-party\ActionSpeed'
            SourceDll = 'Yuuka.DTMAPI.ActionSpeed.dll'
            PackageDll = 'Yuuka.DTMAPI.ActionSpeed.dll'
            UniqueID = 'Yuuka.DTMAPI.ActionSpeed'
            DisplayName = 'DTMAPI Action Speed'
            PackageName = 'DTMAPI-ActionSpeed'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'action-speed'
            AuthorSdkPackageFile = 'DTMAPI-ActionSpeed-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_OneActionComplete'
            Project = 'Yuuka.DTMAPI.OneActionComplete'
            SourceRoot = 'products\first-party\OneActionComplete'
            SourceDll = 'Yuuka.DTMAPI.OneActionComplete.dll'
            PackageDll = 'Yuuka.DTMAPI.OneActionComplete.dll'
            UniqueID = 'Yuuka.DTMAPI.OneActionComplete'
            DisplayName = 'DTMAPI One Action Complete'
            PackageName = 'DTMAPI-OneActionComplete'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'one-action-complete'
            AuthorSdkPackageFile = 'DTMAPI-OneActionComplete-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_ChestLocatorEnhancer'
            Project = 'DTMAPI.ChestLocatorEnhancer'
            SourceRoot = 'products\first-party\ChestLocatorEnhancer'
            SourceDll = 'DTMAPI.ChestLocatorEnhancer.dll'
            PackageDll = 'DTMAPI.ChestLocatorEnhancer.dll'
            UniqueID = 'DTMAPI.ChestLocatorEnhancerMod'
            DisplayName = 'DTMAPI Chest Locator Enhancer'
            PackageName = 'DTMAPI-ChestLocatorEnhancer'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'chest-locator-enhancer'
            AuthorSdkPackageFile = 'DTMAPI-ChestLocatorEnhancer-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_YKeyConsole'
            Project = 'DTMAPI.DebugConsole'
            SourceRoot = 'products\first-party\DebugConsole'
            SourceDll = 'DTMAPI.DebugConsole.dll'
            PackageDll = 'DTMAPI.DebugConsole.dll'
            UniqueID = 'DTMAPI.DebugConsoleMod'
            DisplayName = 'DTMAPI Y-Key Console'
            PackageName = 'DTMAPI-YKeyConsole'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'y-console'
            AuthorSdkPackageFile = 'DTMAPI-YKeyConsole-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_FishBreedingAssistant'
            Project = 'Yuuka.DTMAPI.FishBreedingAssistant'
            SourceRoot = 'products\first-party\FishBreedingAssistant'
            SourceDll = 'Yuuka.DTMAPI.FishBreedingAssistant.dll'
            PackageDll = 'Yuuka.DTMAPI.FishBreedingAssistant.dll'
            UniqueID = 'Yuuka.DTMAPI.FishBreedingAssistant'
            DisplayName = 'DTMAPI Fish Roe Info Display'
            PackageName = 'DTMAPI-FishBreedingAssistant'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'fish-roe-info'
            AuthorSdkPackageFile = 'DTMAPI-FishBreedingAssistant-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_MoreSaves'
            Project = 'DTMAPI.MoreSaves'
            SourceRoot = 'products\first-party\MoreSaves'
            SourceDll = 'DTMAPI.MoreSaves.dll'
            PackageDll = 'DTMAPI.MoreSaves.dll'
            UniqueID = 'DTMAPI.MoreSavesMod'
            DisplayName = 'DTMAPI More Saves'
            PackageName = 'DTMAPI-MoreSaves'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'more-saves'
            AuthorSdkPackageFile = 'DTMAPI-MoreSaves-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_AnimalHusbandryProgress'
            Project = 'Yuuka.DTMAPI.AnimalHusbandryProgress'
            SourceRoot = 'products\first-party\AnimalHusbandryProgress'
            SourceDll = 'Yuuka.DTMAPI.AnimalHusbandryProgress.dll'
            PackageDll = 'Yuuka.DTMAPI.AnimalHusbandryProgress.dll'
            UniqueID = 'Yuuka.DTMAPI.AnimalHusbandryProgress'
            DisplayName = 'DTMAPI Animal Bell Hidden Produce Progress'
            PackageName = 'DTMAPI-AnimalHusbandryProgress'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'animal-husbandry-progress'
            AuthorSdkPackageFile = 'DTMAPI-AnimalHusbandryProgress-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_AutoFishing'
            Project = 'Yuuka.DTMAPI.AutoFishing'
            SourceRoot = 'products\first-party\AutoFishing'
            SourceDll = 'Yuuka.DTMAPI.AutoFishing.dll'
            PackageDll = 'Yuuka.DTMAPI.AutoFishing.dll'
            UniqueID = 'Yuuka.DTMAPI.AutoFishing'
            DisplayName = 'DTMAPI Auto Fishing'
            PackageName = 'DTMAPI-AutoFishing'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'auto-fishing'
            AuthorSdkPackageFile = 'DTMAPI-AutoFishing-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_MoreEquipmentSlots'
            Project = 'DTMAPI.MoreEquipmentSlots'
            SourceRoot = 'products\first-party\MoreEquipmentSlots'
            SourceDll = 'DTMAPI.MoreEquipmentSlots.dll'
            PackageDll = 'DTMAPI.MoreEquipmentSlots.dll'
            UniqueID = 'DTMAPI.MoreEquipmentSlotsMod'
            DisplayName = 'DTMAPI More Equipment Slots'
            PackageName = 'DTMAPI-MoreEquipmentSlots'
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'more-equipment-slots'
            AuthorSdkPackageFile = 'DTMAPI-MoreEquipmentSlots-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'Yuuka_DTMAPI_ManboCardboardAudio'
            Project = 'ManboCardboardAudioMod'
            SourceRoot = 'products\first-party\ManboCardboardAudio'
            SourceDll = 'ManboCardboardAudioMod.dll'
            PackageDll = 'Yuuka.DTMAPI.ManboCardboardAudio.dll'
            UniqueID = 'Yuuka.DTMAPI.ManboCardboardAudio'
            DisplayName = 'DTMAPI Manbo Cardboard Audio'
            PackageName = 'DTMAPI-ManboCardboardAudio'
        }
    )
}

function Get-DtmApiDeveloperOfficialModDefinitions {
    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($item in @(Get-DtmApiPublishedModDefinitions)) {
        $items.Add($item) | Out-Null
    }

    foreach ($item in @(
        [ordered]@{
            OfficialFolder = 'DTMAPI_Oil'
            SourceRoot = 'products\first-party\Oil'
            UniqueID = 'DTMAPI.OilMod'
            DisplayName = 'DTMAPI Oil'
            PackageName = 'DTMAPI-Oil'
            ContentOnly = $true
            DeveloperOnly = $true
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_Mine'
            Project = 'DTMAPI.Mine'
            SourceRoot = 'products\first-party\Mine'
            SourceDll = 'DTMAPI.Mine.dll'
            PackageDll = 'DTMAPI.Mine.dll'
            UniqueID = 'DTMAPI.MineMod'
            DisplayName = 'DTMAPI Mine'
            PackageName = 'DTMAPI-Mine'
            DeveloperOnly = $true
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'mine'
            AuthorSdkPackageFile = 'DTMAPI-Mine-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_StrongPlantingGun'
            Project = 'DTMAPI.StrongPlantingGun'
            SourceRoot = 'products\first-party\StrongPlantingGun'
            SourceDll = 'DTMAPI.StrongPlantingGun.dll'
            PackageDll = 'DTMAPI.StrongPlantingGun.dll'
            UniqueID = 'DTMAPI.StrongPlantingGunMod'
            DisplayName = 'DTMAPI Strong Planting Gun'
            PackageName = 'DTMAPI-StrongPlantingGun'
            DeveloperOnly = $true
            AuthorSdkProject = $true
            AuthorSdkBuildScript = 'build-batch6-advanced-product.ps1'
            AuthorSdkCatalogId = 'strong-planting-gun'
            AuthorSdkPackageFile = 'DTMAPI-StrongPlantingGun-advanced-pilot.zip'
        },
        [ordered]@{
            OfficialFolder = 'DTMAPI_CropHarvestingQA'
            Project = 'CropHarvestingQaMod'
            SourceRoot = 'tests\mod-fixtures\qa\CropHarvesting'
            SourceDll = 'CropHarvestingQaMod.dll'
            PackageDll = 'DTMAPI.CropHarvestingQA.dll'
            UniqueID = 'DTMAPI.CropHarvestingQaMod'
            DisplayName = 'DTMAPI Crop Harvesting QA'
            PackageName = 'DTMAPI-CropHarvestingQA'
            DeveloperOnly = $true
            QaFixture = $true
        }
    )) {
        $items.Add($item) | Out-Null
    }

    return $items.ToArray()
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

function Assert-DtmApiManifestInfoVersionParity {
    param(
        [Parameter(Mandatory = $true)] $Manifest,
        [Parameter(Mandatory = $true)] $Info,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $manifestVersion = [string](Get-DtmApiMapValue -Map $Manifest -Key 'Version' -Default '')
    $infoVersion = [string](Get-DtmApiMapValue -Map $Info -Key 'version' -Default '')
    if ([string]::IsNullOrWhiteSpace($manifestVersion)) {
        throw "$Context manifest Version must be present before packaging."
    }
    if ([string]::IsNullOrWhiteSpace($infoVersion)) {
        throw "$Context info.json version must be present before packaging."
    }
    if (-not [string]::Equals($manifestVersion, $infoVersion, [System.StringComparison]::Ordinal)) {
        throw "$Context version projection mismatch: manifest Version=$manifestVersion; info.json version=$infoVersion. Refusing to package divergent player-visible versions."
    }
}

function Assert-DtmApiCurrentProductProjection {
    param(
        [Parameter(Mandatory = $true)] [string] $RepoRoot,
        [Parameter(Mandatory = $true)] $Definition,
        [Parameter(Mandatory = $true)] $Manifest,
        [Parameter(Mandatory = $true)] $Info,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $catalogPath = Join-Path $RepoRoot 'tools\release\dtmapi-product-catalog.json'
    if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
        throw "$Context cannot validate its current projection because the product catalog is missing: $catalogPath"
    }

    $uniqueId = [string](Get-DtmApiMapValue -Map $Definition -Key 'UniqueID' -Default '')
    $manifestUniqueId = [string](Get-DtmApiMapValue -Map $Manifest -Key 'UniqueID' -Default '')
    if ([string]::IsNullOrWhiteSpace($uniqueId) -or -not [string]::Equals($manifestUniqueId, $uniqueId, [System.StringComparison]::Ordinal)) {
        throw "$Context UniqueID projection mismatch: definition=$uniqueId; manifest=$manifestUniqueId."
    }

    $catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath | ConvertFrom-Json
    $matches = @($catalog.products | Where-Object { [string]$_.uniqueId -eq $uniqueId })
    if ($matches.Count -ne 1) {
        throw "$Context requires exactly one product-catalog row for UniqueID '$uniqueId'; found $($matches.Count)."
    }

    $product = $matches[0]
    $manifestVersion = [string](Get-DtmApiMapValue -Map $Manifest -Key 'Version' -Default '')
    $catalogVersion = [string]$product.sourceVersion
    if (-not [string]::Equals($manifestVersion, $catalogVersion, [System.StringComparison]::Ordinal)) {
        throw "$Context current-version projection mismatch: manifest Version=$manifestVersion; catalog sourceVersion=$catalogVersion. Packaging must not rewrite either value."
    }

    $manifestMinimum = [string](Get-DtmApiMapValue -Map $Manifest -Key 'MinimumDTMApiVersion' -Default '')
    $catalogMinimum = [string]$product.sourceMinimumDtmApiVersion
    if (-not [string]::Equals($manifestMinimum, $catalogMinimum, [System.StringComparison]::Ordinal)) {
        throw "$Context current-minimum projection mismatch: manifest MinimumDTMApiVersion=$manifestMinimum; catalog sourceMinimumDtmApiVersion=$catalogMinimum. Packaging must not silently raise it to the Runtime release version."
    }

    Assert-DtmApiManifestInfoVersionParity -Manifest $Manifest -Info $Info -Context $Context
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
