[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PackageRoot,
    [string] $PreviousPackageRoot = '',
    [switch] $KeepTemp
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$script:DtmApiMultiPlatformRequiredBepInExPaths = @(
    '.doorstop_version',
    'changelog.txt',
    'winhttp.dll',
    'doorstop_config.ini',
    'BepInEx/core/0Harmony20.dll',
    'BepInEx/core/BepInEx.dll',
    'BepInEx/core/BepInEx.Preloader.dll',
    'BepInEx/core/BepInEx.Harmony.dll',
    'BepInEx/core/0Harmony.dll',
    'BepInEx/core/HarmonyXInterop.dll',
    'BepInEx/core/Mono.Cecil.dll',
    'BepInEx/core/Mono.Cecil.Mdb.dll',
    'BepInEx/core/Mono.Cecil.Pdb.dll',
    'BepInEx/core/Mono.Cecil.Rocks.dll',
    'BepInEx/core/MonoMod.RuntimeDetour.dll',
    'BepInEx/core/MonoMod.Utils.dll'
)
$script:DtmApiMultiPlatformLegacyBepInExPaths = @(
    'winhttp.dll',
    'doorstop_config.ini',
    'BepInEx/core/BepInEx.dll',
    'BepInEx/core/BepInEx.Preloader.dll'
)

function Assert-DtmApiMultiPlatformInstallerTest {
    param(
        [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw "DTMAPI multi-platform installer test failed: $Message"
    }
}

function Get-DtmApiMultiPlatformInstallerSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
        $stream.Dispose()
    }
}

function Get-DtmApiMultiPlatformInstallerRelativePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([char]92, [char]47)
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $prefix = $resolvedRoot + [System.IO.Path]::DirectorySeparatorChar
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ($resolvedPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) `
        -Message "Fixture path escaped its expected root. Root=$resolvedRoot Path=$resolvedPath"
    return $resolvedPath.Substring($prefix.Length).Replace('\', '/')
}

function Get-DtmApiMultiPlatformInstallerTreeReceipt {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $resolvedRoot -PathType Container) -Message "Tree root is missing: $resolvedRoot"
    $entries = New-Object 'System.Collections.Generic.List[object]'
    foreach ($file in @(Get-ChildItem -LiteralPath $resolvedRoot -File -Force -Recurse -ErrorAction Stop)) {
        $entries.Add([pscustomobject][ordered]@{
            RelativePath = Get-DtmApiMultiPlatformInstallerRelativePath -Root $resolvedRoot -Path $file.FullName
            Length = [long]$file.Length
            Sha256 = Get-DtmApiMultiPlatformInstallerSha256 -Path $file.FullName
        }) | Out-Null
    }

    $relativePaths = [string[]]@($entries.ToArray() | ForEach-Object { [string]$_.RelativePath })
    [Array]::Sort($relativePaths, [System.StringComparer]::Ordinal)
    $byPath = @{}
    foreach ($entry in @($entries.ToArray())) {
        $byPath[[string]$entry.RelativePath] = $entry
    }
    $rows = New-Object 'System.Collections.Generic.List[string]'
    [long]$bytes = 0
    foreach ($relative in $relativePaths) {
        $entry = $byPath[$relative]
        $rows.Add(('{0}|{1}|{2}' -f $relative, $entry.Length, $entry.Sha256)) | Out-Null
        $bytes += [long]$entry.Length
    }

    return [pscustomobject][ordered]@{
        FileCount = $relativePaths.Count
        Bytes = $bytes
        Manifest = [string]::Join("`n", $rows.ToArray())
    }
}

function Assert-DtmApiMultiPlatformInstallerTreeUnchanged {
    param(
        [Parameter(Mandatory = $true)] $Before,
        [Parameter(Mandatory = $true)] $After,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ($Before.FileCount -eq $After.FileCount -and $Before.Bytes -eq $After.Bytes -and $Before.Manifest -ceq $After.Manifest) `
        -Message "$Label changed the game-shaped fixture. Before=$($Before.FileCount)/$($Before.Bytes) After=$($After.FileCount)/$($After.Bytes)"
}

function Copy-DtmApiMultiPlatformInstallerTree {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    [System.IO.Directory]::CreateDirectory($Destination) | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $Source -Force -ErrorAction Stop)) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }
}

function Write-DtmApiMultiPlatformInstallerFixtureText {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Text
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Text, $encoding)
}

function New-DtmApiMultiPlatformInstallerFakeGame {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    [System.IO.Directory]::CreateDirectory($Path) | Out-Null
    [System.IO.Directory]::CreateDirectory((Join-Path $Path 'DolocTown_Data')) | Out-Null
    $sentinelDefinitions = [ordered]@{
        'DolocTown.exe' = "fake Doloc Town executable: $Label"
        'BepInEx/plugins/External.Owner/keep.dll' = "external BepInEx plugin sentinel: $Label"
        'BepInEx/config/external-owner.cfg' = "external BepInEx config sentinel: $Label"
        'BepInEx/LogOutput.log' = "external BepInEx log sentinel: $Label"
        'DTMAPI/config/external-owner.cfg' = "external DTMAPI config sentinel: $Label"
        'Mods/External.Mod/keep.json' = "external managed Mod sentinel: $Label"
        'ContentPacks/External.Pack/keep.json' = "external ContentPack sentinel: $Label"
    }
    foreach ($index in 1..12) {
        $sentinelDefinitions[('DTMAPI/logs/log-{0:D2}.log' -f $index)] = "DTMAPI retained log $index : $Label"
    }

    foreach ($entry in $sentinelDefinitions.GetEnumerator()) {
        $target = Join-Path $Path ([string]$entry.Key).Replace('/', '\')
        Write-DtmApiMultiPlatformInstallerFixtureText -Path $target -Text ([string]$entry.Value)
    }
    $baseWriteTime = [DateTime]::UtcNow.AddHours(-2)
    foreach ($index in 1..12) {
        $log = Join-Path $Path ('DTMAPI\logs\log-{0:D2}.log' -f $index)
        [System.IO.File]::SetLastWriteTimeUtc($log, $baseWriteTime.AddMinutes($index))
    }

    $sentinels = New-Object 'System.Collections.Generic.List[object]'
    foreach ($relative in @($sentinelDefinitions.Keys)) {
        $target = Join-Path $Path ([string]$relative).Replace('/', '\')
        $item = Get-Item -LiteralPath $target -Force -ErrorAction Stop
        $sentinels.Add([pscustomobject][ordered]@{
            RelativePath = [string]$relative
            Length = [long]$item.Length
            Sha256 = Get-DtmApiMultiPlatformInstallerSha256 -Path $target
        }) | Out-Null
    }

    return [pscustomobject][ordered]@{
        GameRoot = [System.IO.Path]::GetFullPath($Path)
        Sentinels = @($sentinels.ToArray())
    }
}

function Copy-DtmApiMultiPlatformInstallerBepInExFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $GameRoot,
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    $hostManifestPath = Join-Path $Package 'Content\DTMAPIInstaller\multiplatform-package.json'
    $hostManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $hostManifestPath | ConvertFrom-Json
    $archive = Join-Path $Package ([string]$hostManifest.BepInExArchive.RelativePath).Replace('/', '\')
    Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $archive -PathType Leaf) -Message "BepInEx fixture archive is missing: $archive"

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        foreach ($rawRelative in $RelativePaths) {
            $relative = $rawRelative.Replace('\', '/')
            $matches = @($zip.Entries | Where-Object { ([string]$_.FullName).Replace('\', '/') -ceq $relative })
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($matches.Count -eq 1) -Message "BepInEx fixture archive does not contain exactly one $relative entry."
            $target = Join-Path $GameRoot $relative.Replace('/', '\')
            [System.IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
            $input = $matches[0].Open()
            $output = [System.IO.File]::Open($target, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try { $input.CopyTo($output); $output.Flush() }
            finally { $output.Dispose(); $input.Dispose() }
        }
    }
    finally {
        $zip.Dispose()
    }
}

function New-DtmApiMultiPlatformInstallerTransactionStamp {
    return ([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}

function Write-DtmApiMultiPlatformInstallerJsonFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    Write-DtmApiMultiPlatformInstallerFixtureText -Path $Path -Text ($Value | ConvertTo-Json -Depth 20)
}

function New-DtmApiMultiPlatformInstallerJunction {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Target
    )

    try {
        [System.IO.Directory]::CreateDirectory($Target) | Out-Null
        $null = New-Item -ItemType Junction -Path $Path -Target $Target -ErrorAction Stop
        return $true
    }
    catch {
        Write-Warning "Junction regression fixture is unavailable and will be reported as skipped: $($_.Exception.Message)"
        return $false
    }
}

function Assert-DtmApiMultiPlatformInstallerSentinels {
    param(
        [Parameter(Mandatory = $true)] $Fixture,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    foreach ($sentinel in @($Fixture.Sentinels)) {
        $path = Join-Path $Fixture.GameRoot ([string]$sentinel.RelativePath).Replace('/', '\')
        Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $path -PathType Leaf) -Message "$Label removed sentinel $($sentinel.RelativePath)."
        $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($item.Length -eq [long]$sentinel.Length -and (Get-DtmApiMultiPlatformInstallerSha256 -Path $path) -eq [string]$sentinel.Sha256) `
            -Message "$Label changed sentinel $($sentinel.RelativePath)."
    }
}

function ConvertTo-DtmApiMultiPlatformInstallerWslPath {
    param([Parameter(Mandatory = $true)] [string] $Path)

    Assert-DtmApiMultiPlatformInstallerTest -Condition (-not [string]::IsNullOrWhiteSpace($script:DtmApiMultiPlatformWslExe)) -Message 'WSL path translation was requested without an available WSL host.'
    $oldPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $script:DtmApiMultiPlatformWslExe --exec wslpath -a -u ([System.IO.Path]::GetFullPath($Path)) 2>$null | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
    $candidate = @($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($exitCode -eq 0 -and $candidate.Count -eq 1 -and $candidate[0].StartsWith('/')) -Message "WSL could not translate path: $Path"
    return [string]$candidate[0]
}

function Invoke-DtmApiMultiPlatformInstallerHost {
    param(
        [Parameter(Mandatory = $true)] [ValidateSet('Windows', 'Linux')] [string] $HostKind,
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $Action,
        [Parameter(Mandatory = $true)] [string] $Package,
        [string] $Game = '',
        [string] $Output = '',
        [switch] $OmitGamePath
    )

    if ($HostKind -eq 'Windows') {
        $hostArguments = @($Action, '--package-root', $Package)
        if (-not $OmitGamePath) {
            $hostArguments += @('--game-path', $Game)
        }
        $hostArguments += '--non-interactive'
        if (-not [string]::IsNullOrWhiteSpace($Output)) {
            $hostArguments += @('--output', $Output)
        }
        $oldPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $lines = @(& $HostPath @hostArguments 2>&1 | ForEach-Object { [string]$_ })
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $oldPreference
        }
    }
    else {
        $linuxHost = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $HostPath
        $linuxPackage = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $Package
        $hostArguments = @('--exec', 'env', 'WINEDLLOVERRIDES=winhttp=n,b', $linuxHost, $Action, '--package-root', $linuxPackage)
        if (-not $OmitGamePath) {
            $linuxGame = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $Game
            $hostArguments += @('--game-path', $linuxGame)
        }
        $hostArguments += '--non-interactive'
        if (-not [string]::IsNullOrWhiteSpace($Output)) {
            $hostArguments += @('--output', (ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $Output))
        }
        $oldPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $lines = @(& $script:DtmApiMultiPlatformWslExe @hostArguments 2>&1 | ForEach-Object { [string]$_ })
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $oldPreference
        }
    }

    return [pscustomobject][ordered]@{
        ExitCode = [int]$exitCode
        Text = [string]::Join("`n", @($lines))
    }
}

function Assert-DtmApiMultiPlatformInstallerNoHostInGame {
    param(
        [Parameter(Mandatory = $true)] [string] $GameRoot,
        [Parameter(Mandatory = $true)] [object[]] $HostReceipts,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    $forbiddenNames = @('DTMAPI-MultiPlatform-Installer.exe', 'dtmapi-installer', 'host-artifacts.json', 'multiplatform-package.json')
    $named = @(Get-ChildItem -LiteralPath $GameRoot -File -Force -Recurse -ErrorAction Stop | Where-Object { $forbiddenNames -contains $_.Name })
    $namedPaths = @($named | ForEach-Object { $_.FullName }) -join ', '
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($named.Count -eq 0) -Message "$Label copied a host/host manifest into the game: $namedPaths"

    foreach ($receipt in $HostReceipts) {
        foreach ($candidate in @(Get-ChildItem -LiteralPath $GameRoot -File -Force -Recurse -ErrorAction Stop | Where-Object { $_.Length -eq [long]$receipt.Length })) {
            Assert-DtmApiMultiPlatformInstallerTest `
                -Condition ((Get-DtmApiMultiPlatformInstallerSha256 -Path $candidate.FullName) -ne [string]$receipt.Sha256) `
                -Message "$Label copied exact $($receipt.Label) host bytes into the game: $($candidate.FullName)"
        }
    }
}

function Assert-DtmApiMultiPlatformInstallerLogBundle {
    param(
        [Parameter(Mandatory = $true)] [string] $Bundle,
        [Parameter(Mandatory = $true)] [string] $GameRoot,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    foreach ($required in @('collection-manifest.json', 'status.txt', 'environment.txt')) {
        Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath (Join-Path $Bundle $required) -PathType Leaf) -Message "$Label bundle is missing $required."
    }
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Bundle 'collection-manifest.json') | ConvertFrom-Json
    $dtmapiRows = @($manifest.Files | Where-Object { ([string]$_.Destination).Replace('\', '/').StartsWith('DTMAPI/') })
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($dtmapiRows.Count -eq 10) -Message "$Label bundle must contain the newest ten DTMAPI logs; found $($dtmapiRows.Count)."

    $actualNames = New-Object 'System.Collections.Generic.List[string]'
    foreach ($row in $dtmapiRows) {
        $sourceName = (([string]$row.Source).Replace('\', '/') -split '/')[-1]
        $actualNames.Add($sourceName) | Out-Null
        $source = Join-Path $GameRoot ('DTMAPI\logs\' + $sourceName)
        $destination = Join-Path $Bundle (([string]$row.Destination).Replace('/', '\'))
        Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $destination -PathType Leaf) -Message "$Label bundle manifest points at a missing copy: $($row.Destination)"
        $copy = Get-Item -LiteralPath $destination -Force -ErrorAction Stop
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($copy.Length -eq (Get-Item -LiteralPath $source -Force).Length -and (Get-DtmApiMultiPlatformInstallerSha256 -Path $destination) -eq (Get-DtmApiMultiPlatformInstallerSha256 -Path $source) -and [long]$row.Length -eq [long]$copy.Length -and [string]$row.Sha256 -eq (Get-DtmApiMultiPlatformInstallerSha256 -Path $copy.FullName)) `
            -Message "$Label bundle did not preserve the complete bytes for $sourceName."
    }
    $actual = [string[]]$actualNames.ToArray()
    [Array]::Sort($actual, [System.StringComparer]::Ordinal)
    $expected = [string[]]@(3..12 | ForEach-Object { 'log-{0:D2}.log' -f $_ })
    [Array]::Sort($expected, [System.StringComparer]::Ordinal)
    Assert-DtmApiMultiPlatformInstallerTest -Condition (($actual -join '|') -ceq ($expected -join '|')) -Message "$Label newest-ten selection differs. Actual=$($actual -join ',')"
    Assert-DtmApiMultiPlatformInstallerTest -Condition (@(Get-ChildItem -LiteralPath $Bundle -Filter '*.dmp' -File -Force -Recurse).Count -eq 0) -Message "$Label bundle unexpectedly contains a crash dump."
}

function Invoke-DtmApiMultiPlatformInstallerLifecycle {
    param(
        [Parameter(Mandatory = $true)] [ValidateSet('Windows', 'Linux')] [string] $HostKind,
        [Parameter(Mandatory = $true)] [string] $HostPath,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $GameRoot,
        [Parameter(Mandatory = $true)] [string] $OutputRoot,
        [Parameter(Mandatory = $true)] [object[]] $HostReceipts,
        [Parameter(Mandatory = $true)] [string] $Label,
        [switch] $SeedLegacyFourFileBepInEx
    )

    $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path $GameRoot -Label $Label
    if ($SeedLegacyFourFileBepInEx) {
        Copy-DtmApiMultiPlatformInstallerBepInExFixture `
            -Package $Package `
            -GameRoot $fixture.GameRoot `
            -RelativePaths $script:DtmApiMultiPlatformLegacyBepInExPaths
        foreach ($missingBeforeRepair in @($script:DtmApiMultiPlatformRequiredBepInExPaths | Where-Object { $script:DtmApiMultiPlatformLegacyBepInExPaths -notcontains $_ })) {
            Assert-DtmApiMultiPlatformInstallerTest `
                -Condition (-not (Test-Path -LiteralPath (Join-Path $fixture.GameRoot $missingBeforeRepair.Replace('/', '\')) -PathType Leaf)) `
                -Message "$Label partial-BepInEx fixture unexpectedly contains $missingBeforeRepair before repair."
        }
    }
    [System.IO.Directory]::CreateDirectory($OutputRoot) | Out-Null

    $install = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $HostPath -Action 'install' -Package $Package -Game $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($install.ExitCode -eq 0 -and $install.Text -match 'DTM-S1001') -Message "$Label install failed. Exit=$($install.ExitCode) Output=$($install.Text)"
    Assert-DtmApiMultiPlatformInstallerSentinels -Fixture $fixture -Label "$Label install"
    Assert-DtmApiMultiPlatformInstallerNoHostInGame -GameRoot $fixture.GameRoot -HostReceipts $HostReceipts -Label "$Label install"
    foreach ($required in @($script:DtmApiMultiPlatformRequiredBepInExPaths + 'DTMAPI/install-state.json')) {
        Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath (Join-Path $fixture.GameRoot $required.Replace('/', '\')) -PathType Leaf) -Message "$Label install is missing $required."
    }

    $beforeStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    $status = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $HostPath -Action 'status' -Package $Package -Game $fixture.GameRoot
    $afterStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($status.ExitCode -eq 0 -and $status.Text -match 'DTM-S3001' -and $status.Text -match 'Files:\s+HEALTHY') -Message "$Label status did not report healthy files. Exit=$($status.ExitCode) Output=$($status.Text)"
    if ($HostKind -eq 'Linux') {
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($status.Text -match 'Launch integration:\s+UNVERIFIED' -and $status.Text -notmatch 'Launch integration:\s+CONFIGURED') `
            -Message "$Label treated process-only WINEDLLOVERRIDES as persistent Steam configuration. Output=$($status.Text)"
    }
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeStatus -After $afterStatus -Label "$Label read-only status"
    Assert-DtmApiMultiPlatformInstallerSentinels -Fixture $fixture -Label "$Label status"

    $beforeCollect = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    $collectOne = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $HostPath -Action 'collect-logs' -Package $Package -Game $fixture.GameRoot -Output $OutputRoot
    $collectTwo = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $HostPath -Action 'collect-logs' -Package $Package -Game $fixture.GameRoot -Output $OutputRoot
    $afterCollect = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($collectOne.ExitCode -eq 0 -and $collectOne.Text -match 'DTM-S4001') -Message "$Label first log collection failed. Exit=$($collectOne.ExitCode) Output=$($collectOne.Text)"
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($collectTwo.ExitCode -eq 0 -and $collectTwo.Text -match 'DTM-S4001') -Message "$Label second log collection failed. Exit=$($collectTwo.ExitCode) Output=$($collectTwo.Text)"
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeCollect -After $afterCollect -Label "$Label log collection"
    Assert-DtmApiMultiPlatformInstallerSentinels -Fixture $fixture -Label "$Label log collection"
    $bundles = @(Get-ChildItem -LiteralPath $OutputRoot -Directory -Force -ErrorAction Stop | Where-Object { $_.Name -like 'DTMAPI-logs-*' })
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($bundles.Count -eq 2 -and $bundles[0].FullName -cne $bundles[1].FullName) -Message "$Label must publish two unique log bundles; found $($bundles.Count)."
    Assert-DtmApiMultiPlatformInstallerTest -Condition (@(Get-ChildItem -LiteralPath $OutputRoot -Directory -Force | Where-Object { $_.Name -like '*.partial-*' }).Count -eq 0) -Message "$Label left a partial log bundle."
    foreach ($bundle in $bundles) {
        Assert-DtmApiMultiPlatformInstallerLogBundle -Bundle $bundle.FullName -GameRoot $fixture.GameRoot -Label $Label
    }

    $installStatePath = Join-Path $fixture.GameRoot 'DTMAPI\install-state.json'
    $installState = Get-Content -Raw -Encoding UTF8 -LiteralPath $installStatePath | ConvertFrom-Json
    $ownedLivePaths = @($installState.FilesInstalled | ForEach-Object { [string]$_.RelativePath })
    Assert-DtmApiMultiPlatformInstallerTest -Condition (@($ownedLivePaths | Where-Object { $_ -match '(?i)MultiPlatform-Installer|dtmapi-installer|host-artifacts|multiplatform-package' }).Count -eq 0) -Message "$Label install receipt includes a host artifact."

    $uninstall = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $HostPath -Action 'uninstall' -Package $Package -Game $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($uninstall.ExitCode -eq 0 -and $uninstall.Text -match 'DTM-S2001') -Message "$Label uninstall failed. Exit=$($uninstall.ExitCode) Output=$($uninstall.Text)"
    foreach ($relative in $ownedLivePaths) {
        Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath (Join-Path $fixture.GameRoot $relative.Replace('/', '\')) -PathType Leaf)) -Message "$Label uninstall retained owned live path $relative."
    }
    Assert-DtmApiMultiPlatformInstallerSentinels -Fixture $fixture -Label "$Label uninstall"
    Assert-DtmApiMultiPlatformInstallerNoHostInGame -GameRoot $fixture.GameRoot -HostReceipts $HostReceipts -Label "$Label uninstall"
    foreach ($bootstrap in $script:DtmApiMultiPlatformRequiredBepInExPaths) {
        Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath (Join-Path $fixture.GameRoot $bootstrap.Replace('/', '\')) -PathType Leaf) -Message "$Label Runtime-only uninstall removed retained BepInEx file $bootstrap."
    }

    $uninstallReceiptsBefore = @(Get-ChildItem -LiteralPath (Join-Path $fixture.GameRoot 'DTMAPI') -Filter 'uninstall-state-*.json' -File -Force -ErrorAction Stop).Count
    $repeat = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $HostPath -Action 'uninstall' -Package $Package -Game $fixture.GameRoot
    $uninstallReceiptsAfter = @(Get-ChildItem -LiteralPath (Join-Path $fixture.GameRoot 'DTMAPI') -Filter 'uninstall-state-*.json' -File -Force -ErrorAction Stop).Count
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($repeat.ExitCode -eq 0 -and $repeat.Text -match 'DTM-S2001' -and $repeat.Text -match 'Removed=0') -Message "$Label repeated uninstall was not an explicit no-op. Exit=$($repeat.ExitCode) Output=$($repeat.Text)"
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($uninstallReceiptsAfter -eq ($uninstallReceiptsBefore + 1)) -Message "$Label repeated uninstall did not publish a distinct receipt."
    Assert-DtmApiMultiPlatformInstallerSentinels -Fixture $fixture -Label "$Label repeated uninstall"
    Assert-DtmApiMultiPlatformInstallerNoHostInGame -GameRoot $fixture.GameRoot -HostReceipts $HostReceipts -Label "$Label repeated uninstall"

    return [pscustomobject][ordered]@{
        HostKind = $HostKind
        GameRoot = $fixture.GameRoot
        LogBundles = $bundles.Count
        RepeatUninstallReceiptCount = $uninstallReceiptsAfter
    }
}

function Test-DtmApiMultiPlatformAcceptedPowerShellClassifier {
    param(
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $GameRoot,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $windowsPowerShell = Get-Command powershell.exe -ErrorAction SilentlyContinue
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($null -ne $windowsPowerShell) -Message 'Accepted PowerShell transaction classifier requires Windows PowerShell 5.1.'
    $toolsRoot = Join-Path $Package 'Content\DTMAPIInstaller\tools'
    $common = Join-Path $toolsRoot 'common.ps1'
    $check = Join-Path $toolsRoot 'check-dtmapi-status.ps1'
    foreach ($required in @($common, $check, (Join-Path $toolsRoot 'release-common.ps1'))) {
        Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $required -PathType Leaf) -Message "Accepted Content classifier dependency is missing: $required"
    }

    $harness = Join-Path $TestRoot 'invoke-accepted-transaction-classifier.ps1'
    $harnessText = @'
param(
    [Parameter(Mandatory = $true)] [string] $CommonPath,
    [Parameter(Mandatory = $true)] [string] $GameDir,
    [Parameter(Mandatory = $true)] [string] $StateDir,
    [Parameter(Mandatory = $true)] [string] $PluginDir
)
. $CommonPath
@(Get-DtmApiRuntimeTransactionClassifications -GameDir $GameDir -StateDir $StateDir -PluginDir $PluginDir) |
    ConvertTo-Json -Depth 12 -Compress
'@
    Write-DtmApiMultiPlatformInstallerFixtureText -Path $harness -Text $harnessText

    $before = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $GameRoot
    $oldPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $classificationLines = @(& $windowsPowerShell.Source -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $harness `
            -CommonPath $common `
            -GameDir $GameRoot `
            -StateDir (Join-Path $GameRoot 'DTMAPI') `
            -PluginDir (Join-Path $GameRoot 'BepInEx\plugins\DTMAPI') 2>&1 | ForEach-Object { [string]$_ })
        $classificationExit = $LASTEXITCODE
        $checkLines = @(& $windowsPowerShell.Source -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $check -GameDir $GameRoot 2>&1 | ForEach-Object { [string]$_ })
        $checkExit = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
    $after = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $GameRoot

    Assert-DtmApiMultiPlatformInstallerTest -Condition ($classificationExit -eq 0) -Message "Accepted classifier invocation failed: $([string]::Join(' | ', @($classificationLines)))"
    $classification = @(([string]::Join("`n", @($classificationLines)) | ConvertFrom-Json))
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ($classification.Count -eq 1 -and [string]$classification[0].Kind -ceq 'InvalidReceipt' -and [string]$classification[0].ReasonCode -ceq 'invalid-receipt') `
        -Message "Accepted classifier did not fail closed on the new canonical receipt: $([string]::Join(' | ', @($classificationLines)))"
    $checkText = [string]::Join("`n", @($checkLines))
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ($checkExit -eq 1 -and $checkText -match 'Kind=InvalidReceipt' -and $checkText -match 'DTM-E1303' -and $checkText -match 'BLOCKED') `
        -Message "Accepted read-only status route did not surface InvalidReceipt as BLOCKED. Exit=$checkExit Output=$checkText"
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $before -After $after -Label 'Accepted PowerShell cross-engine classifier/status'
}

function Test-DtmApiMultiPlatformInstallerInstallPhaseRecoveryMatrix {
    param(
        [Parameter(Mandatory = $true)] [string] $WindowsHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $variants = @(
        [pscustomobject]@{ Name = 'Prepared'; Phase = 'Prepared'; State = 'Pending'; Live = 'Old'; Candidate = $false; Backup = $false; Expected = 'Old' },
        [pscustomobject]@{ Name = 'CandidateReady'; Phase = 'CandidateReady'; State = 'Pending'; Live = 'Old'; Candidate = $true; Backup = $false; Expected = 'Old' },
        [pscustomobject]@{ Name = 'Committing-MovingOriginal'; Phase = 'Committing'; State = 'MovingOriginal'; Live = 'Old'; Candidate = $true; Backup = $false; Expected = 'Old' },
        [pscustomobject]@{ Name = 'Committing-OriginalMoved'; Phase = 'Committing'; State = 'OriginalMoved'; Live = 'None'; Candidate = $true; Backup = $true; Expected = 'Old' },
        [pscustomobject]@{ Name = 'Committing-CandidatePublished'; Phase = 'Committing'; State = 'CandidatePublished'; Live = 'New'; Candidate = $false; Backup = $true; Expected = 'Old' },
        [pscustomobject]@{ Name = 'Committed'; Phase = 'Committed'; State = 'CandidatePublished'; Live = 'New'; Candidate = $false; Backup = $true; Expected = 'New' }
    )

    foreach ($variant in $variants) {
        $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot ('Install phase ' + $variant.Name)) -Label ('install-phase-' + $variant.Name)
        Copy-DtmApiMultiPlatformInstallerBepInExFixture -Package $Package -GameRoot $fixture.GameRoot -RelativePaths $script:DtmApiMultiPlatformRequiredBepInExPaths
        $oldText = 'old-install-phase-bytes-' + $variant.Name
        $newText = 'new-install-phase-bytes-' + $variant.Name
        $oldBytesPath = Join-Path $TestRoot ('install-old-' + $variant.Name + '.bin')
        $newBytesPath = Join-Path $TestRoot ('install-new-' + $variant.Name + '.bin')
        Write-DtmApiMultiPlatformInstallerFixtureText -Path $oldBytesPath -Text $oldText
        Write-DtmApiMultiPlatformInstallerFixtureText -Path $newBytesPath -Text $newText
        $oldHash = Get-DtmApiMultiPlatformInstallerSha256 -Path $oldBytesPath
        $newHash = Get-DtmApiMultiPlatformInstallerSha256 -Path $newBytesPath
        $newLength = (Get-Item -LiteralPath $newBytesPath -Force).Length
        $targetRelative = 'phase-fixtures/install-' + $variant.Name + '.bin'
        $target = Join-Path $fixture.GameRoot $targetRelative.Replace('/', '\')
        if ($variant.Live -eq 'Old') { Write-DtmApiMultiPlatformInstallerFixtureText -Path $target -Text $oldText }
        elseif ($variant.Live -eq 'New') { Write-DtmApiMultiPlatformInstallerFixtureText -Path $target -Text $newText }

        $stamp = New-DtmApiMultiPlatformInstallerTransactionStamp
        $transactionRoot = Join-Path $fixture.GameRoot ('.dtmapi-runtime-install-' + $stamp)
        [System.IO.Directory]::CreateDirectory($transactionRoot) | Out-Null
        if ($variant.Candidate) {
            Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $transactionRoot 'candidate\0000.bin') -Text $newText
        }
        if ($variant.Backup) {
            Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $transactionRoot 'backup\0000.bin') -Text $oldText
        }
        $receipt = [ordered]@{
            SchemaVersion = 1
            TransactionId = $stamp
            CreatedAtUtc = [DateTime]::UtcNow.ToString('O')
            GameDir = $fixture.GameRoot
            Engine = 'DTMAPI.MultiPlatform/install'
            Phase = [string]$variant.Phase
            Operations = @([ordered]@{
                Ordinal = 0
                TargetRelativePath = $targetRelative
                CandidateRelativePath = 'candidate/0000.bin'
                BackupRelativePath = 'backup/0000.bin'
                CandidateSha256 = $newHash
                CandidateLength = [long]$newLength
                HadOriginal = $true
                State = [string]$variant.State
            })
        }
        Write-DtmApiMultiPlatformInstallerJsonFixture -Path (Join-Path $transactionRoot 'transaction.json') -Value $receipt

        $beforeStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        $status = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'status' -Package $Package -Game $fixture.GameRoot
        $afterStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($status.ExitCode -eq 1 -and $status.Text -match 'Files:\s+BLOCKED') -Message "Install phase $($variant.Name) was not reported BLOCKED. Output=$($status.Text)"
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeStatus -After $afterStatus -Label "Install phase $($variant.Name) read-only status"

        if ($variant.Name -eq 'Prepared') {
            Test-DtmApiMultiPlatformAcceptedPowerShellClassifier -Package $Package -GameRoot $fixture.GameRoot -TestRoot $TestRoot
        }

        $recovered = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($recovered.ExitCode -eq 0 -and $recovered.Text -match 'DTM-S1001') -Message "Install phase $($variant.Name) did not converge. Exit=$($recovered.ExitCode) Output=$($recovered.Text)"
        Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath $transactionRoot)) -Message "Install phase $($variant.Name) transaction root was not cleaned."
        $expectedHash = if ($variant.Expected -eq 'Old') { $oldHash } else { $newHash }
        Assert-DtmApiMultiPlatformInstallerTest -Condition ((Test-Path -LiteralPath $target -PathType Leaf) -and (Get-DtmApiMultiPlatformInstallerSha256 -Path $target) -eq $expectedHash) -Message "Install phase $($variant.Name) converged to the wrong live bytes."
    }
}

function Test-DtmApiMultiPlatformInstallerUninstallPhaseRecoveryMatrix {
    param(
        [Parameter(Mandatory = $true)] [string] $WindowsHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $variants = @(
        [pscustomobject]@{ Name = 'Prepared'; Phase = 'Prepared'; States = @('Pending'); Layouts = @('Live'); Committed = $false },
        [pscustomobject]@{ Name = 'Committing-Multi'; Phase = 'Committing'; States = @('OriginalMoved', 'MovingOriginal', 'Pending'); Layouts = @('Backup', 'Live', 'Live'); Committed = $false },
        [pscustomobject]@{ Name = 'Committed-Finalize'; Phase = 'Committed'; States = @('OriginalMoved', 'OriginalMoved'); Layouts = @('Backup', 'Backup'); Committed = $true }
    )

    foreach ($variant in $variants) {
        $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot ('Uninstall phase ' + $variant.Name)) -Label ('uninstall-phase-' + $variant.Name)
        Copy-DtmApiMultiPlatformInstallerBepInExFixture -Package $Package -GameRoot $fixture.GameRoot -RelativePaths $script:DtmApiMultiPlatformRequiredBepInExPaths
        $stamp = New-DtmApiMultiPlatformInstallerTransactionStamp
        $transactionRoot = Join-Path $fixture.GameRoot ('.dtmapi-runtime-install-' + $stamp)
        [System.IO.Directory]::CreateDirectory($transactionRoot) | Out-Null
        $operations = New-Object 'System.Collections.Generic.List[object]'
        $expected = New-Object 'System.Collections.Generic.List[object]'
        for ($index = 0; $index -lt $variant.States.Count; $index++) {
            $text = "uninstall-old-$($variant.Name)-$index"
            $source = Join-Path $TestRoot ("uninstall-old-$($variant.Name)-$index.bin")
            Write-DtmApiMultiPlatformInstallerFixtureText -Path $source -Text $text
            $hash = Get-DtmApiMultiPlatformInstallerSha256 -Path $source
            $length = (Get-Item -LiteralPath $source -Force).Length
            $targetRelative = "phase-fixtures/uninstall-$($variant.Name)-$index.bin"
            $target = Join-Path $fixture.GameRoot $targetRelative.Replace('/', '\')
            $backupRelative = ('backup/{0:D4}.bin' -f $index)
            $backup = Join-Path $transactionRoot $backupRelative.Replace('/', '\')
            if ($variant.Layouts[$index] -eq 'Live') { Write-DtmApiMultiPlatformInstallerFixtureText -Path $target -Text $text }
            else { Write-DtmApiMultiPlatformInstallerFixtureText -Path $backup -Text $text }
            $operations.Add([ordered]@{
                Ordinal = $index
                TargetRelativePath = $targetRelative
                CandidateRelativePath = ('candidate/{0:D4}.bin' -f $index)
                BackupRelativePath = $backupRelative
                CandidateSha256 = $hash
                CandidateLength = [long]$length
                HadOriginal = $true
                State = [string]$variant.States[$index]
            }) | Out-Null
            $expected.Add([pscustomobject]@{ Target = $target; BackupRelative = $backupRelative; Hash = $hash }) | Out-Null
        }

        $receipt = [ordered]@{
            SchemaVersion = 1
            TransactionId = $stamp
            CreatedAtUtc = [DateTime]::UtcNow.ToString('O')
            GameDir = $fixture.GameRoot
            Engine = 'DTMAPI.MultiPlatform/uninstall'
            Phase = [string]$variant.Phase
            Operations = @($operations.ToArray())
        }
        if ($variant.Committed) {
            $finalBackupRoot = Join-Path $fixture.GameRoot ('DTMAPI\backups\uninstall-' + $stamp)
            $uninstallState = [ordered]@{
                SchemaVersion = 1
                RemovedAtUtc = [DateTime]::UtcNow.ToString('O')
                GameDir = $fixture.GameRoot
                BackupRoot = $finalBackupRoot
                Removed = @($operations.ToArray() | ForEach-Object { [string]$_['TargetRelativePath'] })
                Preserved = @()
                NoOp = $false
            }
            Write-DtmApiMultiPlatformInstallerJsonFixture -Path (Join-Path $transactionRoot 'uninstall-state.json') -Value $uninstallState
        }
        Write-DtmApiMultiPlatformInstallerJsonFixture -Path (Join-Path $transactionRoot 'transaction.json') -Value $receipt

        $beforeStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        $status = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'status' -Package $Package -Game $fixture.GameRoot
        $afterStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($status.ExitCode -eq 1 -and $status.Text -match 'Files:\s+BLOCKED') -Message "Uninstall phase $($variant.Name) was not reported BLOCKED. Output=$($status.Text)"
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeStatus -After $afterStatus -Label "Uninstall phase $($variant.Name) read-only status"

        $recovered = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($recovered.ExitCode -eq 0 -and $recovered.Text -match 'DTM-S1001') -Message "Uninstall phase $($variant.Name) did not converge. Exit=$($recovered.ExitCode) Output=$($recovered.Text)"
        Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath $transactionRoot)) -Message "Uninstall phase $($variant.Name) canonical transaction root was not cleaned/finalized."
        if ($variant.Committed) {
            Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $finalBackupRoot -PathType Container) -Message 'Committed uninstall transaction was not finalized into DTMAPI/backups.'
            foreach ($item in @($expected.ToArray())) {
                $finalBackup = Join-Path $finalBackupRoot ([string]$item.BackupRelative).Replace('/', '\')
                Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath $item.Target)) -Message 'Committed uninstall recovery recreated a removed phase fixture target.'
                Assert-DtmApiMultiPlatformInstallerTest -Condition ((Test-Path -LiteralPath $finalBackup -PathType Leaf) -and (Get-DtmApiMultiPlatformInstallerSha256 -Path $finalBackup) -eq [string]$item.Hash) -Message 'Committed uninstall final backup bytes are invalid.'
            }
            Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath (Join-Path $fixture.GameRoot ('DTMAPI\uninstall-state-' + $stamp + '.json')) -PathType Leaf) -Message 'Committed uninstall finalization did not publish the top-level state receipt.'
        }
        else {
            foreach ($item in @($expected.ToArray())) {
                Assert-DtmApiMultiPlatformInstallerTest -Condition ((Test-Path -LiteralPath $item.Target -PathType Leaf) -and (Get-DtmApiMultiPlatformInstallerSha256 -Path $item.Target) -eq [string]$item.Hash) -Message "Uninstall phase $($variant.Name) did not restore an old live target."
            }
        }
    }
}

function Test-DtmApiMultiPlatformInstallerSettingsFailures {
    param(
        [Parameter(Mandatory = $true)] [string] $WindowsHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $settingsPath = Join-Path $Package 'dtmapi-installer.settings.json'
    Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath $settingsPath)) -Message 'Candidate package unexpectedly contains mutable installer settings.'
    $oldGameDir = [Environment]::GetEnvironmentVariable('DTMAPI_GAME_DIR', [EnvironmentVariableTarget]::Process)
    try {
        [Environment]::SetEnvironmentVariable('DTMAPI_GAME_DIR', $null, [EnvironmentVariableTarget]::Process)

        Write-DtmApiMultiPlatformInstallerFixtureText -Path $settingsPath -Text '{ malformed settings json'
        $malformed = Invoke-DtmApiMultiPlatformInstallerHost `
            -HostKind Windows -HostPath $WindowsHost -Action 'status' -Package $Package -OmitGamePath
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($malformed.ExitCode -eq 2 -and $malformed.Text -match 'DTM-E1002') `
            -Message "Malformed settings did not map to DTM-E1002. Exit=$($malformed.ExitCode) Output=$($malformed.Text)"

        $invalidGame = Join-Path $TestRoot 'settings-invalid-game-path'
        [System.IO.Directory]::CreateDirectory($invalidGame) | Out-Null
        Write-DtmApiMultiPlatformInstallerJsonFixture -Path $settingsPath -Value ([ordered]@{ GamePath = $invalidGame })
        $invalid = Invoke-DtmApiMultiPlatformInstallerHost `
            -HostKind Windows -HostPath $WindowsHost -Action 'status' -Package $Package -OmitGamePath
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($invalid.ExitCode -eq 2 -and $invalid.Text -match 'DTM-E1002') `
            -Message "Invalid settings/manual game path did not map to DTM-E1002. Exit=$($invalid.ExitCode) Output=$($invalid.Text)"
    }
    finally {
        if (Test-Path -LiteralPath $settingsPath -PathType Leaf) {
            Remove-Item -LiteralPath $settingsPath -Force
        }
        [Environment]::SetEnvironmentVariable('DTMAPI_GAME_DIR', $oldGameDir, [EnvironmentVariableTarget]::Process)
    }
}

function Test-DtmApiMultiPlatformInstallerInterruptedUninstallRecovery {
    param(
        [Parameter(Mandatory = $true)] [string] $WindowsHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot 'Interrupted uninstall game') -Label 'interrupted-uninstall'
    Copy-DtmApiMultiPlatformInstallerBepInExFixture -Package $Package -GameRoot $fixture.GameRoot -RelativePaths $script:DtmApiMultiPlatformRequiredBepInExPaths
    $initialInstall = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($initialInstall.ExitCode -eq 0 -and $initialInstall.Text -match 'DTM-S1001') -Message "Interrupted-uninstall fixture install failed. Output=$($initialInstall.Text)"

    $targetRelative = 'BepInEx/plugins/DTMAPI/DTMAPI.Core.dll'
    $target = Join-Path $fixture.GameRoot $targetRelative.Replace('/', '\')
    $originalLength = (Get-Item -LiteralPath $target -Force).Length
    $originalHash = Get-DtmApiMultiPlatformInstallerSha256 -Path $target
    $stamp = New-DtmApiMultiPlatformInstallerTransactionStamp
    $transactionRoot = Join-Path $fixture.GameRoot ('.dtmapi-runtime-install-' + $stamp)
    $backup = Join-Path $transactionRoot 'backup\0000.bin'
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $backup)) | Out-Null
    [System.IO.File]::Move($target, $backup)
    $receipt = [ordered]@{
        SchemaVersion = 1
        TransactionId = $stamp
        CreatedAtUtc = [DateTime]::UtcNow.ToString('O')
        GameDir = $fixture.GameRoot
        Engine = 'DTMAPI.MultiPlatform/uninstall'
        Phase = 'Committing'
        Operations = @([ordered]@{
            Ordinal = 0
            TargetRelativePath = $targetRelative
            CandidateRelativePath = 'candidate/0000.bin'
            BackupRelativePath = 'backup/0000.bin'
            CandidateSha256 = $originalHash
            CandidateLength = [long]$originalLength
            HadOriginal = $true
            State = 'OriginalMoved'
        })
    }
    Write-DtmApiMultiPlatformInstallerJsonFixture -Path (Join-Path $transactionRoot 'transaction.json') -Value $receipt
    $mixedOrphanStamp = New-DtmApiMultiPlatformInstallerTransactionStamp
    $mixedOrphan = Join-Path $fixture.GameRoot ('DTMAPI\.runtime-install-transaction-' + $mixedOrphanStamp)
    [System.IO.Directory]::CreateDirectory($mixedOrphan) | Out-Null
    Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $mixedOrphan 'orphan-sentinel.txt') -Text 'must block before own uninstall recovery mutates the tree'
    $matchingStateFile = Join-Path $fixture.GameRoot ('DTMAPI\.runtime-install-transaction-' + $stamp)
    Write-DtmApiMultiPlatformInstallerFixtureText -Path $matchingStateFile -Text 'matching legacy state namespace occupied by a file'

    $beforeStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    $status = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'status' -Package $Package -Game $fixture.GameRoot
    $afterStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ($status.ExitCode -eq 1 -and $status.Text -match 'DTM-E3001' -and $status.Text -match 'Files:\s+BLOCKED') `
        -Message "Interrupted uninstall was not reported read-only as BLOCKED. Exit=$($status.ExitCode) Output=$($status.Text)"
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeStatus -After $afterStatus -Label 'Interrupted-uninstall status'

    $beforeBlockedRecovery = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    $blockedRecovery = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $fixture.GameRoot
    $afterBlockedRecovery = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($blockedRecovery.ExitCode -eq 3 -and $blockedRecovery.Text -match 'DTM-E1303') -Message "Mixed own/orphan transaction did not fail before recovery. Output=$($blockedRecovery.Text)"
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeBlockedRecovery -After $afterBlockedRecovery -Label 'Mixed own/orphan preflight'
    Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath $target -PathType Leaf)) -Message 'Mixed transaction preflight restored the own target before rejecting the orphan.'

    Remove-Item -LiteralPath $mixedOrphan -Recurse -Force
    Remove-Item -LiteralPath $matchingStateFile -Force
    $recovered = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($recovered.ExitCode -eq 0 -and $recovered.Text -match 'DTM-S1001') -Message "Install did not recover interrupted uninstall. Exit=$($recovered.ExitCode) Output=$($recovered.Text)"
    Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath $transactionRoot)) -Message 'Recovered uninstall transaction root was not removed/finalized.'
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ((Test-Path -LiteralPath $target -PathType Leaf) -and (Get-DtmApiMultiPlatformInstallerSha256 -Path $target) -eq $originalHash) `
        -Message 'Interrupted uninstall recovery did not restore the exact Runtime target.'
}

function Test-DtmApiMultiPlatformInstallerOrphanLegacyStateFailsClosed {
    param(
        [Parameter(Mandatory = $true)] [string] $WindowsHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot 'Orphan legacy state game') -Label 'orphan-state'
    $stamp = New-DtmApiMultiPlatformInstallerTransactionStamp
    $orphan = Join-Path $fixture.GameRoot ('DTMAPI\.runtime-install-transaction-' + $stamp)
    [System.IO.Directory]::CreateDirectory($orphan) | Out-Null
    Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $orphan 'orphan-sentinel.txt') -Text 'legacy state root without matching canonical transaction root'
    Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $fixture.GameRoot 'DTMAPI\.multiplatform-installer.lock') -Text 'pre-existing lock file fixture'

    $beforeStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    $status = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'status' -Package $Package -Game $fixture.GameRoot
    $afterStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($status.ExitCode -eq 1 -and $status.Text -match 'Files:\s+BLOCKED') -Message "Orphan legacy state was not reported BLOCKED. Output=$($status.Text)"
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeStatus -After $afterStatus -Label 'Orphan-state status'

    foreach ($action in @('install', 'uninstall')) {
        $beforeMutation = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        $result = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action $action -Package $Package -Game $fixture.GameRoot
        $afterMutation = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($result.ExitCode -eq 3 -and $result.Text -match 'DTM-E1303') `
            -Message "Orphan legacy state did not fail closed for $action. Exit=$($result.ExitCode) Output=$($result.Text)"
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeMutation -After $afterMutation -Label "Orphan-state $action"
    }

    foreach ($variant in @('CanonicalRootFile', 'LegacyStateFile')) {
        $fileFixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot ('Transaction prefix file ' + $variant)) -Label ('prefix-file-' + $variant)
        Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $fileFixture.GameRoot 'DTMAPI\.multiplatform-installer.lock') -Text 'pre-existing lock file fixture'
        $fileStamp = New-DtmApiMultiPlatformInstallerTransactionStamp
        $prefixFile = if ($variant -eq 'CanonicalRootFile') {
            Join-Path $fileFixture.GameRoot ('.dtmapi-runtime-install-' + $fileStamp)
        }
        else {
            Join-Path $fileFixture.GameRoot ('DTMAPI\.runtime-install-transaction-' + $fileStamp)
        }
        Write-DtmApiMultiPlatformInstallerFixtureText -Path $prefixFile -Text 'transaction namespace must be a directory'
        $beforePrefix = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fileFixture.GameRoot
        $prefixStatus = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'status' -Package $Package -Game $fileFixture.GameRoot
        $afterPrefixStatus = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fileFixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($prefixStatus.ExitCode -eq 1 -and $prefixStatus.Text -match 'Files:\s+BLOCKED') -Message "$variant was not reported BLOCKED. Output=$($prefixStatus.Text)"
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforePrefix -After $afterPrefixStatus -Label "$variant status"
        foreach ($action in @('install', 'uninstall')) {
            $prefixResult = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action $action -Package $Package -Game $fileFixture.GameRoot
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($prefixResult.ExitCode -eq 3 -and $prefixResult.Text -match 'DTM-E1303') -Message "$variant did not fail closed for $action. Output=$($prefixResult.Text)"
        }
        $afterPrefixMutations = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fileFixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforePrefix -After $afterPrefixMutations -Label "$variant mutations"
    }
}

function Test-DtmApiMultiPlatformInstallerInstalledReceiptSkewFailsClosed {
    param(
        [Parameter(Mandatory = $true)] [string] $WindowsHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    foreach ($variant in @(
        'BuildCommit',
        'FileReceipt',
        'EmptyVersion',
        'UnknownDistribution',
        'StateVersionSkew',
        'ReleaseVersionSkew',
        'LowerVersionUnknownDistribution',
        'LowerVersionLegacyWithoutRelease')) {
        $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot ('Receipt skew ' + $variant)) -Label ('receipt-skew-' + $variant)
        Copy-DtmApiMultiPlatformInstallerBepInExFixture -Package $Package -GameRoot $fixture.GameRoot -RelativePaths $script:DtmApiMultiPlatformRequiredBepInExPaths
        $first = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($first.ExitCode -eq 0 -and $first.Text -match 'DTM-S1001') -Message "Receipt-skew fixture install failed for $variant. Output=$($first.Text)"

        $statePath = Join-Path $fixture.GameRoot 'DTMAPI\install-state.json'
        $state = Get-Content -Raw -Encoding UTF8 -LiteralPath $statePath | ConvertFrom-Json
        switch ($variant) {
            'BuildCommit' {
                $state.SourceRepoCommit = 'same-version-different-build-commit'
            }
            'FileReceipt' {
                $row = @($state.FilesInstalled | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.Sha256) } | Select-Object -First 1)
                Assert-DtmApiMultiPlatformInstallerTest -Condition ($row.Count -eq 1) -Message 'Installed-state fixture has no hash-bearing file receipt.'
                $row[0].Sha256 = ('0' * 64)
            }
            'EmptyVersion' {
                $state.DTMAPIVersion = ''
            }
            'UnknownDistribution' {
                $state.InstallerDistribution = 'Unknown.SameVersion.Engine'
            }
            'StateVersionSkew' {
                $state.DTMAPIVersion = '0.6.0'
                $state.BinaryVersion = '0.6.0.0'
            }
            'ReleaseVersionSkew' {
                # The installed release authority is changed after the state is
                # written below, so the two existing authorities conflict.
            }
            'LowerVersionUnknownDistribution' {
                $state.DTMAPIVersion = '0.6.0'
                $state.BinaryVersion = '0.6.0.0'
                $state.InstallerDistribution = 'Unknown.LowerVersion.Engine'
            }
            'LowerVersionLegacyWithoutRelease' {
                $state.DTMAPIVersion = '0.6.0'
                $state.BinaryVersion = '0.6.0.0'
                $state.InstallerDistribution = ''
            }
        }
        Write-DtmApiMultiPlatformInstallerJsonFixture -Path $statePath -Value $state
        if ($variant -in @('ReleaseVersionSkew', 'LowerVersionUnknownDistribution')) {
            $releasePath = Join-Path $fixture.GameRoot 'DTMAPI\release-manifest.json'
            $release = Get-Content -Raw -Encoding UTF8 -LiteralPath $releasePath | ConvertFrom-Json
            $release.DTMAPIVersion = '0.6.0'
            $release.BinaryVersion = '0.6.0.0'
            Write-DtmApiMultiPlatformInstallerJsonFixture -Path $releasePath -Value $release
        }
        elseif ($variant -eq 'LowerVersionLegacyWithoutRelease') {
            Remove-Item -LiteralPath (Join-Path $fixture.GameRoot 'DTMAPI\release-manifest.json') -Force
        }

        $before = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        $result = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $fixture.GameRoot
        $after = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($result.ExitCode -eq 3 -and $result.Text -match 'DTM-E1202') `
            -Message "Runtime provenance variant $variant did not fail closed. Exit=$($result.ExitCode) Output=$($result.Text)"
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $before -After $after -Label "Runtime provenance $Variant install"
    }
}

function Test-DtmApiMultiPlatformInstallerLinkedPaths {
    param(
        [Parameter(Mandatory = $true)] [string] $WindowsHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $uninstallFixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot 'Linked uninstall game') -Label 'linked-uninstall'
    Copy-DtmApiMultiPlatformInstallerBepInExFixture -Package $Package -GameRoot $uninstallFixture.GameRoot -RelativePaths $script:DtmApiMultiPlatformRequiredBepInExPaths
    $installed = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'install' -Package $Package -Game $uninstallFixture.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($installed.ExitCode -eq 0) -Message "Linked-uninstall fixture install failed. Output=$($installed.Text)"
    $outsideBackup = Join-Path $TestRoot 'Outside linked backup target'
    [System.IO.Directory]::CreateDirectory($outsideBackup) | Out-Null
    $outsideSentinel = Join-Path $outsideBackup 'outside-sentinel.txt'
    Write-DtmApiMultiPlatformInstallerFixtureText -Path $outsideSentinel -Text 'must never receive or lose Runtime files'
    $backupLink = Join-Path $uninstallFixture.GameRoot 'DTMAPI\backups'
    if (New-DtmApiMultiPlatformInstallerJunction -Path $backupLink -Target $outsideBackup) {
        $ownedTarget = Join-Path $uninstallFixture.GameRoot 'BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'
        $ownedHash = Get-DtmApiMultiPlatformInstallerSha256 -Path $ownedTarget
        $outsideBefore = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $outsideBackup
        $uninstall = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'uninstall' -Package $Package -Game $uninstallFixture.GameRoot
        $outsideAfter = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $outsideBackup
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($uninstall.ExitCode -eq 3 -and $uninstall.Text -match 'DTM-E(1102|1303)') -Message "Linked backup root was not rejected before uninstall. Exit=$($uninstall.ExitCode) Output=$($uninstall.Text)"
        Assert-DtmApiMultiPlatformInstallerTest -Condition ((Test-Path -LiteralPath $ownedTarget -PathType Leaf) -and (Get-DtmApiMultiPlatformInstallerSha256 -Path $ownedTarget) -eq $ownedHash) -Message 'Linked backup rejection moved or changed a Runtime file.'
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $outsideBefore -After $outsideAfter -Label 'Linked uninstall backup target'
    }

    $collectFixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot 'Linked logs game') -Label 'linked-logs'
    $logsLink = Join-Path $collectFixture.GameRoot 'DTMAPI\logs'
    Remove-Item -LiteralPath $logsLink -Recurse -Force
    $outsideLogs = Join-Path $TestRoot 'Outside linked logs target'
    [System.IO.Directory]::CreateDirectory($outsideLogs) | Out-Null
    $secretMarker = 'DTMAPI-LINK-SECRET-' + [Guid]::NewGuid().ToString('N')
    Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $outsideLogs 'secret.log') -Text $secretMarker
    if (New-DtmApiMultiPlatformInstallerJunction -Path $logsLink -Target $outsideLogs) {
        $output = Join-Path $TestRoot 'Linked logs output'
        [System.IO.Directory]::CreateDirectory($output) | Out-Null
        $collect = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'collect-logs' -Package $Package -Game $collectFixture.GameRoot -Output $output
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($collect.ExitCode -eq 0 -and $collect.Text -match 'DTM-S4001') -Message "Linked log source was not safely skipped. Exit=$($collect.ExitCode) Output=$($collect.Text)"
        $bundles = @(Get-ChildItem -LiteralPath $output -Directory -Force -ErrorAction Stop | Where-Object { $_.Name -like 'DTMAPI-logs-*' })
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($bundles.Count -eq 1) -Message "Linked log collection produced $($bundles.Count) bundles instead of one."
        $manifestText = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $bundles[0].FullName 'collection-manifest.json')
        $manifest = $manifestText | ConvertFrom-Json
        Assert-DtmApiMultiPlatformInstallerTest -Condition (([string]::Join(' | ', @($manifest.MissingOrSkipped))) -match '(?i)link|reparse|symbolic') -Message 'Linked log source was skipped without an explicit manifest finding.'
        foreach ($file in @(Get-ChildItem -LiteralPath $bundles[0].FullName -File -Force -Recurse)) {
            $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($text.IndexOf($secretMarker, [System.StringComparison]::Ordinal) -lt 0) -Message "Linked external log bytes leaked into bundle file $($file.FullName)."
        }
    }

    $cleanupFixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot 'Linked cleanup game') -Label 'linked-cleanup'
    $outsideCleanup = Join-Path $TestRoot 'Outside linked cleanup target'
    $outsideBranding = Join-Path $outsideCleanup 'assets\branding'
    [System.IO.Directory]::CreateDirectory($outsideBranding) | Out-Null
    $pluginLink = Join-Path $cleanupFixture.GameRoot 'BepInEx\plugins\DTMAPI'
    if (New-DtmApiMultiPlatformInstallerJunction -Path $pluginLink -Target $outsideCleanup) {
        $cleanupResult = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $WindowsHost -Action 'uninstall' -Package $Package -Game $cleanupFixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($cleanupResult.ExitCode -eq 0 -and $cleanupResult.Text -match 'DTM-S2001') -Message "Linked cleanup fixture uninstall failed. Output=$($cleanupResult.Text)"
        Assert-DtmApiMultiPlatformInstallerTest -Condition ((Test-Path -LiteralPath $outsideCleanup -PathType Container) -and (Test-Path -LiteralPath $outsideBranding -PathType Container)) -Message 'Non-authoritative empty-directory cleanup followed a parent junction outside the game tree.'
    }
}

function Invoke-DtmApiMultiPlatformInstallerShellEntry {
    param(
        [Parameter(Mandatory = $true)] [string] $Script,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $Game,
        [string] $Output = ''
    )

    $linuxScript = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $Script
    $linuxPackage = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $Package
    $linuxGame = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $Game
    $arguments = @('--exec', 'env', 'WINEDLLOVERRIDES=winhttp=n,b', 'bash', $linuxScript, '--package-root', $linuxPackage, '--game-path', $linuxGame, '--non-interactive')
    if (-not [string]::IsNullOrWhiteSpace($Output)) {
        $arguments += @('--output', (ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $Output))
    }
    $oldPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $lines = @(& $script:DtmApiMultiPlatformWslExe @arguments 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
    return [pscustomobject][ordered]@{ ExitCode = [int]$exitCode; Text = [string]::Join("`n", @($lines)) }
}

function Test-DtmApiMultiPlatformInstallerShellEntriesExecute {
    param(
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot 'Shell wrapper Linux game') -Label 'shell-wrapper'
    $output = Join-Path $TestRoot 'Shell wrapper logs'
    [System.IO.Directory]::CreateDirectory($output) | Out-Null
    $calls = @(
        [pscustomobject]@{ File = '1_install_dtmapi.sh'; Code = 'DTM-S1001'; Output = '' },
        [pscustomobject]@{ File = '3_check_dtmapi_status.sh'; Code = 'DTM-S3001'; Output = '' },
        [pscustomobject]@{ File = '4_collect_dtmapi_logs.sh'; Code = 'DTM-S4001'; Output = $output },
        [pscustomobject]@{ File = '2_uninstall_dtmapi.sh'; Code = 'DTM-S2001'; Output = '' }
    )
    foreach ($call in $calls) {
        $result = Invoke-DtmApiMultiPlatformInstallerShellEntry -Script (Join-Path $Package $call.File) -Package $Package -Game $fixture.GameRoot -Output $call.Output
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($result.ExitCode -eq 0 -and $result.Text -match [regex]::Escape($call.Code)) -Message "$($call.File) did not execute its fixed action. Exit=$($result.ExitCode) Output=$($result.Text)"
        if ($call.File -eq '3_check_dtmapi_status.sh') {
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($result.Text -match 'Launch integration:\s+UNVERIFIED') -Message "Linux shell status treated process-only WINEDLLOVERRIDES as verified Steam configuration. Output=$($result.Text)"
        }
    }
}

function Test-DtmApiMultiPlatformInstallerLinuxExternalPlayerLogSymlink {
    param(
        [Parameter(Mandatory = $true)] [string] $LinuxHost,
        [Parameter(Mandatory = $true)] [string] $Package,
        [Parameter(Mandatory = $true)] [string] $TestRoot
    )

    $steamApps = Join-Path $TestRoot 'Symlink Steam Library\steamapps'
    $gameRoot = Join-Path $steamApps 'common\DolocTown PlayerLog Symlink'
    $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path $gameRoot -Label 'linux-external-playerlog-symlink'
    $usersRoot = Join-Path $steamApps 'compatdata\2285550\pfx\drive_c\users'
    [System.IO.Directory]::CreateDirectory($usersRoot) | Out-Null

    $externalUser = Join-Path $TestRoot 'External PlayerLog user'
    $externalLog = Join-Path $externalUser 'AppData\LocalLow\RedSawGames\DolocTown\Player.log'
    $secretMarker = 'DTMAPI-EXTERNAL-PLAYERLOG-SECRET-' + [Guid]::NewGuid().ToString('N')
    Write-DtmApiMultiPlatformInstallerFixtureText -Path $externalLog -Text $secretMarker
    $wineUserLink = Join-Path $usersRoot 'deck'

    $linuxExternalUser = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $externalUser
    $linuxWineUserLink = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $wineUserLink
    $oldPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $linkOutput = @(& $script:DtmApiMultiPlatformWslExe --exec ln -s -- $linuxExternalUser $linuxWineUserLink 2>&1 | ForEach-Object { [string]$_ })
        $linkExit = $LASTEXITCODE
        if ($linkExit -eq 0) {
            $readLinkOutput = @(& $script:DtmApiMultiPlatformWslExe --exec readlink -- $linuxWineUserLink 2>&1 | ForEach-Object { [string]$_ })
            $readLinkExit = $LASTEXITCODE
        }
        else {
            $readLinkOutput = @()
            $readLinkExit = 1
        }
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }

    if ($linkExit -ne 0 -or $readLinkExit -ne 0) {
        $reason = "SKIPPED: WSL/host mount could not create and verify the Player.log parent symlink. ln=$linkExit readlink=$readLinkExit Output=$([string]::Join(' | ', @($linkOutput + $readLinkOutput)))"
        Write-Warning $reason
        return [pscustomobject][ordered]@{ State = $reason; GameRoot = $fixture.GameRoot; Bundle = '' }
    }

    $outputRoot = Join-Path $TestRoot 'Linux external PlayerLog symlink output'
    [System.IO.Directory]::CreateDirectory($outputRoot) | Out-Null
    $collect = Invoke-DtmApiMultiPlatformInstallerHost `
        -HostKind Linux `
        -HostPath $LinuxHost `
        -Action 'collect-logs' `
        -Package $Package `
        -Game $fixture.GameRoot `
        -Output $outputRoot
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ($collect.ExitCode -eq 0 -and $collect.Text -match 'DTM-S4001') `
        -Message "Linux external Player.log symlink collection failed instead of explicitly skipping the source. Exit=$($collect.ExitCode) Output=$($collect.Text)"

    $bundles = @(Get-ChildItem -LiteralPath $outputRoot -Directory -Force -ErrorAction Stop | Where-Object { $_.Name -like 'DTMAPI-logs-*' })
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($bundles.Count -eq 1) -Message "Linux external Player.log symlink collection produced $($bundles.Count) bundles instead of one."
    $manifestPath = Join-Path $bundles[0].FullName 'collection-manifest.json'
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    $skipped = [string]::Join(' | ', @($manifest.MissingOrSkipped))
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition ($skipped -match '(?i)Unity player-log roots.*not followed|symlink|symbolic|reparse') `
        -Message "Linux external Player.log parent symlink was not recorded as explicitly skipped. MissingOrSkipped=$skipped"
    Assert-DtmApiMultiPlatformInstallerTest `
        -Condition (@($manifest.Files | Where-Object { ([string]$_.Source).Replace('\', '/') -match '/compatdata/2285550/pfx/drive_c/users/deck/' }).Count -eq 0) `
        -Message 'Linux collection manifest included a file reached through the linked Wine user directory.'
    foreach ($file in @(Get-ChildItem -LiteralPath $bundles[0].FullName -File -Force -Recurse)) {
        $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
        Assert-DtmApiMultiPlatformInstallerTest `
            -Condition ($text.IndexOf($secretMarker, [System.StringComparison]::Ordinal) -lt 0) `
            -Message "External Player.log secret leaked into bundle file $($file.FullName)."
    }

    return [pscustomobject][ordered]@{
        State = 'PASS'
        GameRoot = $fixture.GameRoot
        Bundle = $bundles[0].FullName
    }
}

function Test-DtmApiMultiPlatformInstallerShellEntries {
    param([Parameter(Mandatory = $true)] [string] $Package)

    $actionMap = [ordered]@{
        '1_install_dtmapi.sh' = 'install'
        '2_uninstall_dtmapi.sh' = 'uninstall'
        '3_check_dtmapi_status.sh' = 'status'
        '4_collect_dtmapi_logs.sh' = 'collect-logs'
    }
    $actual = [string[]]@(Get-ChildItem -LiteralPath $Package -Filter '*.sh' -File -Force -ErrorAction Stop | ForEach-Object { $_.Name })
    [Array]::Sort($actual, [System.StringComparer]::Ordinal)
    $expected = [string[]]@($actionMap.Keys)
    [Array]::Sort($expected, [System.StringComparer]::Ordinal)
    Assert-DtmApiMultiPlatformInstallerTest -Condition (($actual -join '|') -ceq ($expected -join '|')) -Message "Root shell entry set is not exact. Actual=$($actual -join ',')"

    foreach ($entry in $actionMap.GetEnumerator()) {
        $path = Join-Path $Package ([string]$entry.Key)
        $bytes = [System.IO.File]::ReadAllBytes($path)
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($bytes.Length -gt 20) -Message "$($entry.Key) is empty."
        Assert-DtmApiMultiPlatformInstallerTest -Condition (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 0xef -and $bytes[1] -eq 0xbb -and $bytes[2] -eq 0xbf)) -Message "$($entry.Key) contains a UTF-8 BOM."
        Assert-DtmApiMultiPlatformInstallerTest -Condition (@($bytes | Where-Object { $_ -eq 0x0d }).Count -eq 0) -Message "$($entry.Key) is not LF-only."
        $strictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
        try { $text = $strictUtf8.GetString($bytes) }
        catch { throw "DTMAPI multi-platform installer test failed: $($entry.Key) is not valid UTF-8. $($_.Exception.Message)" }
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($text.StartsWith("#!/usr/bin/env bash`n", [System.StringComparison]::Ordinal)) -Message "$($entry.Key) has the wrong shebang."
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($text.IndexOf('host="$script_dir/Content/DTMAPIInstaller/hosts/linux-x64/dtmapi-installer"', [System.StringComparison]::Ordinal) -ge 0) -Message "$($entry.Key) does not bind the fixed linux-x64 distribution host."
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($text.IndexOf(('"$host" ' + [string]$entry.Value + ' "$@"'), [System.StringComparison]::Ordinal) -ge 0 -and $text.IndexOf('exit_code=$?', [System.StringComparison]::Ordinal) -ge 0 -and $text.IndexOf('noexec', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $text.IndexOf('exit "$exit_code"', [System.StringComparison]::Ordinal) -ge 0) -Message "$($entry.Key) does not dispatch fixed action $($entry.Value) with visible noexec failure handling."
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($text.IndexOf('chmod u+x -- "$host"', [System.StringComparison]::Ordinal) -ge 0) -Message "$($entry.Key) does not limit executable-bit repair to the fixed internal host."

        if (-not [string]::IsNullOrWhiteSpace($script:DtmApiMultiPlatformWslExe)) {
            $linuxPath = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $path
            $oldPreference = $ErrorActionPreference
            try {
                $ErrorActionPreference = 'Continue'
                $syntaxOutput = @(& $script:DtmApiMultiPlatformWslExe --exec bash -n -- $linuxPath 2>&1 | ForEach-Object { [string]$_ })
                $syntaxExit = $LASTEXITCODE
            }
            finally {
                $ErrorActionPreference = $oldPreference
            }
        }
        else {
            $bash = Get-Command bash.exe -ErrorAction SilentlyContinue
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($null -ne $bash) -Message 'bash -n is required, but neither WSL nor bash.exe is available.'
            $oldPreference = $ErrorActionPreference
            try {
                $ErrorActionPreference = 'Continue'
                $syntaxOutput = @(& $bash.Source -n -- $path 2>&1 | ForEach-Object { [string]$_ })
                $syntaxExit = $LASTEXITCODE
            }
            finally {
                $ErrorActionPreference = $oldPreference
            }
        }
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($syntaxExit -eq 0) -Message "bash -n rejected $($entry.Key): $([string]::Join(' | ', @($syntaxOutput)))"
    }
}

$repo = Get-RepoRoot
$sourcePackage = [System.IO.Path]::GetFullPath($PackageRoot)
Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $sourcePackage -PathType Container) -Message "Package root is missing: $sourcePackage"

$testBase = if (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo 'tmp\test-runs'))
}
$unicode = -join @([char]0x4E2D, [char]0x6587)
$testRoot = [System.IO.Path]::GetFullPath((Join-Path $testBase ('multiplatform-installer-' + [Guid]::NewGuid().ToString('N'))))
Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-DtmApiPathIsSameOrChild -Child $testRoot -Parent $testBase) -Message "Managed test root escaped its boundary: $testRoot"


function Test-DtmApiMultiPlatformCandidateProvenance {
    param([string] $HostKind, [string] $HostPath, [string] $Package, [string] $TestRoot)
    $manifestRelative = 'Content/DTMAPIInstaller/multiplatform-package.json'
    $original = Get-Content -LiteralPath (Join-Path $Package $manifestRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($original.SchemaVersion -ne 2) { return }
    $bad = Join-Path $TestRoot ("candidate-provenance-$HostKind")
    Copy-DtmApiMultiPlatformInstallerTree -Source $Package -Destination $bad
    $manifestPath = Join-Path $bad $manifestRelative
    $originalText = [IO.File]::ReadAllText($manifestPath)
    $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot "provenance-game-$HostKind") -Label 'candidate-provenance'
    $before = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
    foreach ($variant in @('unknown-schema','unknown-version','published-kind','mixed-steam','runtime-commit','installer-commit','missing-shared','shared-tool')) {
        $manifest = $originalText | ConvertFrom-Json
        $toolPath = Join-Path $bad 'Content/DTMAPIInstaller/tools/collect-logs.ps1'
        $toolBytes = [IO.File]::ReadAllBytes($toolPath)
        switch ($variant) {
            'unknown-schema' { $manifest.SchemaVersion = 3 }
            'unknown-version' { $manifest.RuntimeVersion = '0.8.0' }
            'published-kind' { $manifest.RuntimeSource.Kind = 'ObservedPublished' }
            'mixed-steam' { $manifest | Add-Member NoteProperty SourceWorkshopManifestId '918505309011394484' }
            'runtime-commit' { $manifest.RuntimeSource.BuildCommit = '0' * 40 }
            'installer-commit' { $manifest.InstallerBuildCommit = '0' * 40 }
            'missing-shared' { $manifest.RuntimeSource.SharedFiles = @($manifest.RuntimeSource.SharedFiles | Select-Object -Skip 1) }
            'shared-tool' { [IO.File]::AppendAllText($toolPath, "`n# changed") }
        }
        [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 30), (New-Object Text.UTF8Encoding($false)))
        $result = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $HostPath -Action install -Package $bad -Game $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($result.ExitCode -eq 3 -and $result.Text -match 'DTM-E1201') -Message "$HostKind candidate $variant did not reject before mutation: $($result.Text)"
        Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $before -After (Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot) -Label "$HostKind candidate $variant"
        [IO.File]::WriteAllBytes($toolPath, $toolBytes)
    }
    Write-Host "$HostKind candidate provenance: PASS (8 rejection variants)"
}

function Test-DtmApiMultiPlatformVersionTransition {
    param([string] $HostKind, [string] $HostPath, [string] $Package, [string] $PreviousPackage, [string] $TestRoot, [object[]] $HostReceipts)
    $releaseRelative = 'Content/DTMAPI/release-manifest.json'
    $newRelease = Get-Content -LiteralPath (Join-Path $Package $releaseRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($newRelease.DTMAPIVersion -ne '0.7.0') { return }
    Assert-DtmApiMultiPlatformInstallerTest -Condition (-not [string]::IsNullOrWhiteSpace($PreviousPackage)) -Message '0.7.0 lifecycle requires -PreviousPackageRoot with exact 0.6.1 schema 1 package.'
    $old = Join-Path $TestRoot ("previous-package-$HostKind")
    Copy-DtmApiMultiPlatformInstallerTree -Source $PreviousPackage -Destination $old
    $oldRelease = Get-Content -LiteralPath (Join-Path $old $releaseRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    $oldManifest = Get-Content -LiteralPath (Join-Path $old 'Content/DTMAPIInstaller/multiplatform-package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($oldRelease.DTMAPIVersion -ceq '0.6.1' -and $oldManifest.SchemaVersion -eq 1) -Message 'Previous package must be schema 1 / 0.6.1.'
    $oldHost = Join-Path $old $(if ($HostKind -eq 'Windows') { 'DTMAPI-MultiPlatform-Installer.exe' } else { 'Content/DTMAPIInstaller/hosts/linux-x64/dtmapi-installer' })
    if ($HostKind -eq 'Linux') {
        & $script:DtmApiMultiPlatformWslExe --exec chmod u+x -- (ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $oldHost)
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($LASTEXITCODE -eq 0) -Message 'Cannot execute previous Linux host.'
    }
    $fixture = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $TestRoot "upgrade-game-$HostKind") -Label 'version-transition'
    # Original 0.6.1 host -> new reader of old schema -> new Runtime -> explicit withdrawal -> old reinstall -> new reinstall.
    $steps = @(
        @{ HostPath = $oldHost; Package = $old; Action = 'install'; Version = '0.6.1' },
        @{ HostPath = $HostPath; Package = $old; Action = 'status'; Version = '0.6.1' },
        @{ HostPath = $HostPath; Package = $Package; Action = 'install'; Version = '0.7.0' },
        @{ HostPath = $HostPath; Package = $Package; Action = 'status'; Version = '0.7.0' },
        @{ HostPath = $HostPath; Package = $Package; Action = 'uninstall'; Version = '' },
        @{ HostPath = $HostPath; Package = $old; Action = 'install'; Version = '0.6.1' },
        @{ HostPath = $oldHost; Package = $old; Action = 'status'; Version = '0.6.1' },
        @{ HostPath = $HostPath; Package = $Package; Action = 'install'; Version = '0.7.0' },
        @{ HostPath = $HostPath; Package = $Package; Action = 'uninstall'; Version = '' }
    )
    foreach ($step in $steps) {
        $before = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
        $result = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $step.HostPath -Action $step.Action -Package $step.Package -Game $fixture.GameRoot
        Assert-DtmApiMultiPlatformInstallerTest -Condition ($result.ExitCode -eq 0) -Message "$HostKind transition $($step.Action) $($step.Version) failed: $($result.Text)"
        if ($step.Action -eq 'status') {
            Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $before -After (Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot) -Label "$HostKind transition status"
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($result.Text -match 'Files:\s+HEALTHY') -Message 'Transition status is not healthy.'
        }
        $installed = Join-Path $fixture.GameRoot 'DTMAPI/release-manifest.json'
        if ($step.Version) {
            $actual = Get-Content -LiteralPath $installed -Raw -Encoding UTF8 | ConvertFrom-Json
            Assert-DtmApiMultiPlatformInstallerTest -Condition ($actual.DTMAPIVersion -ceq $step.Version) -Message 'Transition installed the wrong version.'
            foreach ($assembly in $actual.IncludedAssemblies) {
                $dll = Join-Path $fixture.GameRoot ('BepInEx/plugins/DTMAPI/' + $assembly.FileName)
                Assert-DtmApiMultiPlatformInstallerTest -Condition ((Get-DtmApiMultiPlatformInstallerSha256 -Path $dll) -ceq $assembly.Sha256) -Message 'Transition assembly bytes differ.'
            }
        } else {
            Assert-DtmApiMultiPlatformInstallerTest -Condition (-not (Test-Path -LiteralPath $installed)) -Message 'Withdrawal retained release receipt.'
        }
        Assert-DtmApiMultiPlatformInstallerSentinels -Fixture $fixture -Label "$HostKind transition"
        Assert-DtmApiMultiPlatformInstallerNoHostInGame -GameRoot $fixture.GameRoot -HostReceipts $HostReceipts -Label "$HostKind transition"
        if ($step.Version -eq '0.7.0' -and $step.Action -eq 'install') {
            $beforeReject = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot
            foreach ($rejection in @(
                @{ Host = $HostPath; Package = $old; Code = 'DTM-E1202' },
                @{ Host = $oldHost; Package = $Package; Code = 'DTM-E1201' }
            )) {
                $rejected = Invoke-DtmApiMultiPlatformInstallerHost -HostKind $HostKind -HostPath $rejection.Host -Action install -Package $rejection.Package -Game $fixture.GameRoot
                Assert-DtmApiMultiPlatformInstallerTest -Condition ($rejected.ExitCode -ne 0 -and $rejected.Text -match $rejection.Code) -Message "$HostKind downgrade/old-reader rejection failed: $($rejected.Text)"
                Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $beforeReject -After (Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $fixture.GameRoot) -Label "$HostKind downgrade/old-reader rejection"
            }
        }
    }
    Write-Host "$HostKind 0.6.1 -> 0.7.0 -> withdrawal/reinstall: PASS"
}

$script:DtmApiMultiPlatformWslExe = ''
$wsl = Get-Command wsl.exe -ErrorAction SilentlyContinue
if ($wsl) {
    $oldPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $null = @(& $wsl.Source --exec bash -c 'exit 0' 2>&1)
        if ($LASTEXITCODE -eq 0) { $script:DtmApiMultiPlatformWslExe = [string]$wsl.Source }
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
}

$environmentNames = @('DTMAPI_GAME_DIR', 'WINEPREFIX', 'WINELOADERNOEXEC', 'CX_BOTTLE', 'CX_ROOT', 'WINEDATADIR')
$environmentBefore = @{}
foreach ($name in $environmentNames) {
    $environmentBefore[$name] = [Environment]::GetEnvironmentVariable($name, [EnvironmentVariableTarget]::Process)
}

$linuxResult = $null
try {
    [System.IO.Directory]::CreateDirectory($testRoot) | Out-Null
    foreach ($name in @('WINEPREFIX', 'WINELOADERNOEXEC', 'CX_BOTTLE', 'CX_ROOT', 'WINEDATADIR')) {
        [Environment]::SetEnvironmentVariable($name, $null, [EnvironmentVariableTarget]::Process)
    }
    [Environment]::SetEnvironmentVariable('DTMAPI_GAME_DIR', (Join-Path $testRoot 'poison-auto-discovery'), [EnvironmentVariableTarget]::Process)

    $package = Join-Path $testRoot ('Package (x64) & ' + $unicode + '; candidate')
    Copy-DtmApiMultiPlatformInstallerTree -Source $sourcePackage -Destination $package
    $windowsHost = Join-Path $package 'DTMAPI-MultiPlatform-Installer.exe'
    $linuxHost = Join-Path $package 'Content\DTMAPIInstaller\hosts\linux-x64\dtmapi-installer'
    Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $windowsHost -PathType Leaf) -Message "Windows host is missing: $windowsHost"
    Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $linuxHost -PathType Leaf) -Message "Linux host is missing: $linuxHost"
    $hostReceipts = @(
        [pscustomobject]@{ Label = 'win-x64'; Length = (Get-Item -LiteralPath $windowsHost).Length; Sha256 = Get-DtmApiMultiPlatformInstallerSha256 -Path $windowsHost },
        [pscustomobject]@{ Label = 'linux-x64'; Length = (Get-Item -LiteralPath $linuxHost).Length; Sha256 = Get-DtmApiMultiPlatformInstallerSha256 -Path $linuxHost }
    )

    Test-DtmApiMultiPlatformCandidateProvenance -HostKind Windows -HostPath $windowsHost -Package $package -TestRoot $testRoot
    Test-DtmApiMultiPlatformVersionTransition -HostKind Windows -HostPath $windowsHost -Package $package -PreviousPackage $PreviousPackageRoot -TestRoot $testRoot -HostReceipts $hostReceipts
    Test-DtmApiMultiPlatformInstallerShellEntries -Package $package
    Test-DtmApiMultiPlatformInstallerSettingsFailures -WindowsHost $windowsHost -Package $package -TestRoot $testRoot

    $badPath = Join-Path $testRoot ('Explicit bad game & ' + $unicode + '; no exe')
    [System.IO.Directory]::CreateDirectory($badPath) | Out-Null
    Write-DtmApiMultiPlatformInstallerFixtureText -Path (Join-Path $badPath 'keep.txt') -Text 'explicit invalid path sentinel'
    $badPathBefore = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $badPath
    $badPathResult = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath $windowsHost -Action 'status' -Package $package -Game $badPath
    $badPathAfter = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $badPath
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($badPathResult.ExitCode -eq 2 -and $badPathResult.Text -match 'DTM-E1002') -Message "Explicit invalid --game-path did not fail as DTM-E1002. Exit=$($badPathResult.ExitCode) Output=$($badPathResult.Text)"
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $badPathBefore -After $badPathAfter -Label 'Explicit invalid --game-path'

    $badPackage = Join-Path $testRoot ('Bad package & ' + $unicode + '; corrupted')
    Copy-DtmApiMultiPlatformInstallerTree -Source $package -Destination $badPackage
    $corruptPayload = Join-Path $badPackage 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Core.dll'
    Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-Path -LiteralPath $corruptPayload -PathType Leaf) -Message 'Bad-package fixture payload is missing.'
    $append = [System.IO.File]::Open($corruptPayload, [System.IO.FileMode]::Append, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try { $append.WriteByte(0x5a); $append.Flush() }
    finally { $append.Dispose() }
    $badPackageGame = New-DtmApiMultiPlatformInstallerFakeGame -Path (Join-Path $testRoot ('Bad package game & ' + $unicode + '; path')) -Label 'bad-package-preflight'
    $badPackageBefore = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $badPackageGame.GameRoot
    $badPackageResult = Invoke-DtmApiMultiPlatformInstallerHost -HostKind Windows -HostPath (Join-Path $badPackage 'DTMAPI-MultiPlatform-Installer.exe') -Action 'install' -Package $badPackage -Game $badPackageGame.GameRoot
    $badPackageAfter = Get-DtmApiMultiPlatformInstallerTreeReceipt -Root $badPackageGame.GameRoot
    Assert-DtmApiMultiPlatformInstallerTest -Condition ($badPackageResult.ExitCode -eq 3 -and $badPackageResult.Text -match 'DTM-E1201') -Message "Corrupt package did not fail closed as DTM-E1201. Exit=$($badPackageResult.ExitCode) Output=$($badPackageResult.Text)"
    Assert-DtmApiMultiPlatformInstallerTreeUnchanged -Before $badPackageBefore -After $badPackageAfter -Label 'Corrupt-package preflight'
    Assert-DtmApiMultiPlatformInstallerSentinels -Fixture $badPackageGame -Label 'Corrupt-package preflight'

    Test-DtmApiMultiPlatformInstallerInstallPhaseRecoveryMatrix -WindowsHost $windowsHost -Package $package -TestRoot $testRoot
    Test-DtmApiMultiPlatformInstallerUninstallPhaseRecoveryMatrix -WindowsHost $windowsHost -Package $package -TestRoot $testRoot
    Test-DtmApiMultiPlatformInstallerInterruptedUninstallRecovery -WindowsHost $windowsHost -Package $package -TestRoot $testRoot
    Test-DtmApiMultiPlatformInstallerOrphanLegacyStateFailsClosed -WindowsHost $windowsHost -Package $package -TestRoot $testRoot
    Test-DtmApiMultiPlatformInstallerInstalledReceiptSkewFailsClosed -WindowsHost $windowsHost -Package $package -TestRoot $testRoot
    Test-DtmApiMultiPlatformInstallerLinkedPaths -WindowsHost $windowsHost -Package $package -TestRoot $testRoot

    $windowsResult = Invoke-DtmApiMultiPlatformInstallerLifecycle `
        -HostKind Windows `
        -HostPath $windowsHost `
        -Package $package `
        -GameRoot (Join-Path $testRoot ('Windows Game (x64) & ' + $unicode + '; path')) `
        -OutputRoot (Join-Path $testRoot ('Windows Logs & ' + $unicode + '; output')) `
        -HostReceipts $hostReceipts `
        -Label 'win-x64' `
        -SeedLegacyFourFileBepInEx

    $linuxState = 'SKIPPED: WSL/bash is unavailable.'
    $linuxExternalPlayerLogSymlinkState = 'SKIPPED: WSL/bash is unavailable, so the external Player.log parent symlink fixture could not run.'
    if (-not [string]::IsNullOrWhiteSpace($script:DtmApiMultiPlatformWslExe)) {
        $linuxHostPath = ConvertTo-DtmApiMultiPlatformInstallerWslPath -Path $linuxHost
        $oldPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $chmodOutput = @(& $script:DtmApiMultiPlatformWslExe --exec chmod u+x -- $linuxHostPath 2>&1 | ForEach-Object { [string]$_ })
            $chmodExit = $LASTEXITCODE
            if ($chmodExit -eq 0) {
                $helpOutput = @(& $script:DtmApiMultiPlatformWslExe --exec $linuxHostPath --help 2>&1 | ForEach-Object { [string]$_ })
                $helpExit = $LASTEXITCODE
            }
            else {
                $helpOutput = @()
                $helpExit = 126
            }
        }
        finally {
            $ErrorActionPreference = $oldPreference
        }

        if ($chmodExit -eq 0 -and $helpExit -eq 0) {
            Test-DtmApiMultiPlatformCandidateProvenance -HostKind Linux -HostPath $linuxHost -Package $package -TestRoot $testRoot
            Test-DtmApiMultiPlatformVersionTransition -HostKind Linux -HostPath $linuxHost -Package $package -PreviousPackage $PreviousPackageRoot -TestRoot $testRoot -HostReceipts $hostReceipts
            Test-DtmApiMultiPlatformInstallerShellEntriesExecute -Package $package -TestRoot $testRoot
            $linuxExternalPlayerLogSymlinkResult = Test-DtmApiMultiPlatformInstallerLinuxExternalPlayerLogSymlink `
                -LinuxHost $linuxHost `
                -Package $package `
                -TestRoot $testRoot
            $linuxExternalPlayerLogSymlinkState = [string]$linuxExternalPlayerLogSymlinkResult.State
            $linuxResult = Invoke-DtmApiMultiPlatformInstallerLifecycle `
                -HostKind Linux `
                -HostPath $linuxHost `
                -Package $package `
                -GameRoot (Join-Path $testRoot ('Linux Game (x64) & ' + $unicode + '; path')) `
                -OutputRoot (Join-Path $testRoot ('Linux Logs & ' + $unicode + '; output')) `
                -HostReceipts $hostReceipts `
                -Label 'linux-x64 via WSL'
            $linuxState = 'PASS'
        }
        else {
            $linuxState = "SKIPPED: linux-x64 host is not runnable through WSL. chmod=$chmodExit help=$helpExit Output=$([string]::Join(' | ', @($chmodOutput + $helpOutput)))"
            $linuxExternalPlayerLogSymlinkState = "SKIPPED: linux-x64 host is not runnable through WSL, so the external Player.log parent symlink fixture could not run. chmod=$chmodExit help=$helpExit"
        }
    }

    Write-Host 'DTMAPI multi-platform Runtime installer focused matrix: PASS'
    Write-Host "Package: $sourcePackage"
    Write-Host "Windows: PASS; Game=$($windowsResult.GameRoot); Collections=$($windowsResult.LogBundles)"
    Write-Host "Linux: $linuxState"
    Write-Host "Linux external Player.log parent symlink: $linuxExternalPlayerLogSymlinkState"
    [pscustomobject][ordered]@{
        Passed = $true
        PackageRoot = $sourcePackage
        PreviousPackageRoot = $PreviousPackageRoot
        CandidateProvenanceVariantsPerExecutedHost = if ($PreviousPackageRoot) { 8 } else { 0 }
        VersionTransitions = if ($PreviousPackageRoot) { 'PASS on each executed host' } else { 'not-run (legacy package)' }
        Windows = $windowsResult
        Linux = $linuxResult
        LinuxState = $linuxState
        LinuxExternalPlayerLogSymlink = $linuxExternalPlayerLogSymlinkState
        ShellEntries = 4
        ShellEntriesExecuted = if ($linuxState -eq 'PASS') { 4 } else { 0 }
        InvalidExplicitPath = 'DTM-E1002'
        InvalidSettings = 'DTM-E1002'
        InvalidPackage = 'DTM-E1201-pre-mutation'
        InstallPhaseRecoveryVariants = 6
        UninstallPhaseRecoveryVariants = 3
        AcceptedPowerShellClassifier = 'InvalidReceipt-DTM-E1303'
        InterruptedUninstallRecovery = 'PASS'
        OrphanLegacyState = 'DTM-E1303-fail-closed'
        RuntimeProvenanceConflictVariants = 8
        LowerVersionUnknownDistribution = 'DTM-E1202-zero-mutation'
        LowerVersionLegacyWithoutRelease = 'DTM-E1202-zero-mutation'
        LinkedPaths = 'reject-or-skip'
    }
}
finally {
    foreach ($name in $environmentBefore.Keys) {
        [Environment]::SetEnvironmentVariable($name, $environmentBefore[$name], [EnvironmentVariableTarget]::Process)
    }
    if (-not $KeepTemp -and (Test-Path -LiteralPath $testRoot -PathType Container)) {
        Assert-DtmApiMultiPlatformInstallerTest -Condition (Test-DtmApiPathIsSameOrChild -Child $testRoot -Parent $testBase) -Message "Cleanup escaped managed test root: $testRoot"
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
    elseif ($KeepTemp) {
        Write-Host "Retained test root: $testRoot"
    }
}
