param(
    [string] $PackagePath = '',
    [switch] $KeepTemp
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\author-sdk-release-common.ps1"
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Quote-PortableArgument {
    param([Parameter(Mandatory = $true)] [AllowEmptyString()] [string] $Value)
    if ($Value.Contains('"')) { throw "Portable gate does not accept a quote in a generated argument: $Value" }
    return '"' + $Value + '"'
}

function Invoke-PortableAuthor {
    param(
        [Parameter(Mandatory = $true)] [string] $Executable,
        [Parameter(Mandatory = $true)] [string] $WorkingDirectory,
        [Parameter(Mandatory = $true)] [string] $ProfileRoot,
        [Parameter(Mandatory = $true)] [string] $NuGetRoot,
        [Parameter(Mandatory = $true)] [string] $FakeGameRoot,
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $Executable
    $start.WorkingDirectory = $WorkingDirectory
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $start.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    $start.Arguments = (@($Arguments | ForEach-Object { Quote-PortableArgument -Value $_ }) -join ' ')
    $start.EnvironmentVariables['PATH'] = ''
    $start.EnvironmentVariables['DOTNET_ROOT'] = ''
    $start.EnvironmentVariables['DOTNET_ROOT_X64'] = ''
    $start.EnvironmentVariables['DOTNET_ROOT_X86'] = ''
    $start.EnvironmentVariables['DOTNET_ROOT(x86)'] = ''
    $start.EnvironmentVariables['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    $start.EnvironmentVariables['USERPROFILE'] = $ProfileRoot
    $start.EnvironmentVariables['HOME'] = $ProfileRoot
    $start.EnvironmentVariables['NUGET_PACKAGES'] = $NuGetRoot
    $start.EnvironmentVariables['DTMAPI_AUTHOR_STATE_ROOT'] = (Join-Path $ProfileRoot 'author-state')
    $start.EnvironmentVariables['DTMAPI_GAME_DIR'] = $FakeGameRoot
    $start.EnvironmentVariables.Remove('DTMAPI_AUTHOR_COMPAT_ROOT') | Out-Null
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    if (-not $process.Start()) { throw 'Failed to start the self-contained Author SDK CLI.' }
    try {
        $stdoutBytes = New-Object System.IO.MemoryStream
        $stderrBytes = New-Object System.IO.MemoryStream
        $stdoutCopy = $process.StandardOutput.BaseStream.CopyToAsync($stdoutBytes)
        $stderrCopy = $process.StandardError.BaseStream.CopyToAsync($stderrBytes)
        $process.WaitForExit()
        [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($stdoutCopy, $stderrCopy))
        $stdout = Convert-PortableOutputBytes -Bytes $stdoutBytes.ToArray()
        $stderr = Convert-PortableOutputBytes -Bytes $stderrBytes.ToArray()
        $stdoutBytes.Dispose()
        $stderrBytes.Dispose()
        return [pscustomobject]@{ ExitCode = $process.ExitCode; StdOut = $stdout; StdErr = $stderr }
    }
    finally { $process.Dispose() }
}

function Assert-PortableSuccess {
    param([string] $Label, $Result)
    if ($Result.ExitCode -ne 0) {
        throw "$Label failed with exit $($Result.ExitCode). stdout=$($Result.StdOut) stderr=$($Result.StdErr)"
    }
}

function Get-PortableExactTreeSnapshot {
    param([Parameter(Mandatory = $true)] [string] $Root)

    $rootFull = [System.IO.Path]::GetFullPath($Root)
    if (-not (Test-Path -LiteralPath $rootFull -PathType Container)) {
        throw "Portable exact-tree root is missing: $rootFull"
    }

    [string[]]$entryPaths = @(Get-ChildItem -LiteralPath $rootFull -Force -Recurse | ForEach-Object { $_.FullName })
    [Array]::Sort($entryPaths, [System.Collections.Generic.Comparer[string]]::Create([System.Comparison[string]]{
        param($left, $right)
        return [System.StringComparer]::Ordinal.Compare(
            (Get-AuthorSdkRelativePath -Root $rootFull -Path $left),
            (Get-AuthorSdkRelativePath -Root $rootFull -Path $right))
    }))

    $rows = New-Object 'System.Collections.Generic.List[string]'
    $rootItem = Get-Item -LiteralPath $rootFull -Force
    $rows.Add("R`t.`t$([int]$rootItem.Attributes)`t$($rootItem.LastWriteTimeUtc.Ticks)") | Out-Null
    foreach ($entryPath in $entryPaths) {
        $entry = Get-Item -LiteralPath $entryPath -Force
        $relative = Get-AuthorSdkRelativePath -Root $rootFull -Path $entryPath
        if ($entry.PSIsContainer) {
            $rows.Add("D`t$relative`t$([int]$entry.Attributes)`t$($entry.LastWriteTimeUtc.Ticks)") | Out-Null
        }
        elseif (Test-Path -LiteralPath $entryPath -PathType Leaf) {
            $rows.Add("F`t$relative`t$([int]$entry.Attributes)`t$($entry.LastWriteTimeUtc.Ticks)`t$($entry.Length)`t$(Get-AuthorSdkSha256 -Path $entryPath)") | Out-Null
        }
        else {
            throw "Portable exact-tree snapshot found an unsupported entry: $entryPath"
        }
    }

    $identity = [string]::Join("`n", $rows.ToArray())
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $digest = ([BitConverter]::ToString(
            $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($identity)))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
    return [pscustomobject]@{
        EntryCount = $rows.Count
        Sha256 = $digest
        Identity = $identity
    }
}

function Assert-PortableExactTreeUnchanged {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] $Expected,
        [Parameter(Mandatory = $true)] [string] $Root
    )

    $actual = Get-PortableExactTreeSnapshot -Root $Root
    if ([int]$actual.EntryCount -ne [int]$Expected.EntryCount -or
        [string]$actual.Identity -cne [string]$Expected.Identity) {
        throw "$Label changed. expected=$($Expected.EntryCount)/$($Expected.Sha256) actual=$($actual.EntryCount)/$($actual.Sha256)"
    }
}

function Assert-PortablePausedMutation {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Executable,
        [Parameter(Mandatory = $true)] [string] $WorkingDirectory,
        [Parameter(Mandatory = $true)] [string] $ProfileRoot,
        [Parameter(Mandatory = $true)] [string] $NuGetRoot,
        [Parameter(Mandatory = $true)] [string] $FakeGameRoot,
        [Parameter(Mandatory = $true)] [string[]] $Arguments,
        [Parameter(Mandatory = $true)] $ExpectedGameTree,
        [Parameter(Mandatory = $true)] [string] $AuthorStateRoot,
        [Parameter(Mandatory = $true)] $ExpectedAuthorStateTree,
        [Parameter(Mandatory = $true)] [string] $BogusPackageRoot,
        [Parameter(Mandatory = $true)] $ExpectedBogusPackageTree,
        [Parameter(Mandatory = $true)] [string] $BogusPackagePath,
        [Parameter(Mandatory = $true)] [string] $ExpectedBogusPackageSha256
    )

    $result = Invoke-PortableAuthor `
        -Executable $Executable `
        -WorkingDirectory $WorkingDirectory `
        -ProfileRoot $ProfileRoot `
        -NuGetRoot $NuGetRoot `
        -FakeGameRoot $FakeGameRoot `
        -Arguments $Arguments
    if ($result.ExitCode -ne 1) {
        throw "$Label did not exit 1. exit=$($result.ExitCode) stdout=$($result.StdOut) stderr=$($result.StdErr)"
    }
    try {
        $report = $result.StdOut | ConvertFrom-Json
    }
    catch {
        throw "$Label did not return valid JSON. stdout=$($result.StdOut) stderr=$($result.StdErr)"
    }
    $diagnostics = @($report.diagnostics)
    if ([bool]$report.success -or
        $diagnostics.Count -ne 1 -or
        [string]$diagnostics[0].code -cne 'SDK003') {
        throw "$Label did not return exactly one diagnostic with code SDK003. stdout=$($result.StdOut) stderr=$($result.StdErr)"
    }

    Assert-PortableExactTreeUnchanged -Label "$Label fake game tree" -Expected $ExpectedGameTree -Root $FakeGameRoot
    Assert-PortableExactTreeUnchanged -Label "$Label Author state tree" -Expected $ExpectedAuthorStateTree -Root $AuthorStateRoot
    Assert-PortableExactTreeUnchanged -Label "$Label bogus package tree" -Expected $ExpectedBogusPackageTree -Root $BogusPackageRoot
    if ((Get-AuthorSdkSha256 -Path $BogusPackagePath) -cne $ExpectedBogusPackageSha256) {
        throw "$Label changed the bogus package bytes: $BogusPackagePath"
    }
}

function Get-PortableUnicodeLabel {
    param([Parameter(Mandatory = $true)] [int[]] $CodePoints)
    return -join @($CodePoints | ForEach-Object { [char]$_ })
}

function Convert-PortableOutputBytes {
    param([Parameter(Mandatory = $true)] [AllowEmptyCollection()] [byte[]] $Bytes)
    $strictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
    try {
        return $strictUtf8.GetString($Bytes)
    }
    catch [System.Text.DecoderFallbackException] {
        $codePage = [System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ANSICodePage
        return [System.Text.Encoding]::GetEncoding($codePage).GetString($Bytes)
    }
}

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $PackagePath = Join-Path $repo 'dist\author-sdk\DTMAPI-Author-SDK-0.1.0-win-x64.zip'
}
$packageFull = [System.IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $packageFull -PathType Leaf)) { throw "Author SDK portable ZIP is missing: $packageFull" }
& "$PSScriptRoot\check-author-sdk-release.ps1" -PackagePath $packageFull
if (-not $?) { throw 'Author SDK release structure gate failed before the portable test.' }

$portableGateLabel = Get-PortableUnicodeLabel -CodePoints @(0x4FBF, 0x643A, 0x95E8, 0x7981)
$containsLabel = Get-PortableUnicodeLabel -CodePoints @(0x542B)
$temporaryBase = Join-Path ([System.IO.Path]::GetTempPath()) ('DTMAPI Author SDK ' + $portableGateLabel)
$temporaryRoot = Join-Path $temporaryBase ($containsLabel + ' space-' + [Guid]::NewGuid().ToString('N'))
Assert-AuthorSdkChildPath -Root $temporaryBase -Path $temporaryRoot -Label 'portable test root' | Out-Null
$playerDllLock = $null
try {
    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    $extractLabel = Get-PortableUnicodeLabel -CodePoints @(0x89E3, 0x538B, 0x76EE, 0x5F55)
    $emptyLabel = Get-PortableUnicodeLabel -CodePoints @(0x7A7A)
    $cacheLabel = Get-PortableUnicodeLabel -CodePoints @(0x7F13, 0x5B58)
    $workLabel = Get-PortableUnicodeLabel -CodePoints @(0x5DE5, 0x4F5C, 0x76EE, 0x5F55)
    $fakeGameLabel = Get-PortableUnicodeLabel -CodePoints @(0x73A9, 0x5BB6, 0x6E38, 0x620F, 0x5047, 0x76EE, 0x5F55)
    $authorExampleLabel = Get-PortableUnicodeLabel -CodePoints @(0x4F5C, 0x8005, 0x793A, 0x4F8B)
    $packageOutputLabel = Get-PortableUnicodeLabel -CodePoints @(0x6253, 0x5305, 0x8F93, 0x51FA)
    $doctorTargetLabel = Get-PortableUnicodeLabel -CodePoints @(0x6B63, 0x5E38, 0x5305)
    $sdkRoot = Join-Path $temporaryRoot ('SDK ' + $extractLabel)
    Expand-AuthorSdkZipSafely -ZipPath $packageFull -Destination $sdkRoot | Out-Null
    $exe = Join-Path $sdkRoot 'dtmapi-author.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Self-contained CLI is missing after extraction: $exe" }

    $profile = Join-Path $temporaryRoot ($emptyLabel + ' profile')
    $nuget = Join-Path $profile ($emptyLabel + ' NuGet ' + $cacheLabel)
    $working = Join-Path $temporaryRoot $workLabel
    $fakeGame = Join-Path $temporaryRoot $fakeGameLabel
    foreach ($directory in @($profile, $nuget, $working, $fakeGame)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    $playerDll = Join-Path $fakeGame 'BepInEx\plugins\DTMAPI\DTMAPI.Abstractions.dll'
    New-Item -ItemType Directory -Path (Split-Path -Parent $playerDll) -Force | Out-Null
    [System.IO.File]::WriteAllBytes($playerDll, [byte[]](0, 1, 2, 3, 4, 5))
    $playerDllHash = Get-AuthorSdkSha256 -Path $playerDll
    $playerDllWriteTime = (Get-Item -LiteralPath $playerDll).LastWriteTimeUtc

    $legacySource = Join-Path $fakeGame 'Mods\Tests.Paused'
    New-Item -ItemType Directory -Path $legacySource -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $fakeGame 'empty-tree-guard') -Force | Out-Null
    [System.IO.File]::WriteAllBytes(
        (Join-Path $legacySource 'sentinel.bin'),
        [System.Text.Encoding]::UTF8.GetBytes('retired game Mods deployment must remain exact'))
    $authorStateRoot = Join-Path $profile 'author-state'
    New-Item -ItemType Directory -Path $authorStateRoot -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $authorStateRoot 'empty-tree-guard') -Force | Out-Null
    [System.IO.File]::WriteAllBytes(
        (Join-Path $authorStateRoot 'sentinel.bin'),
        [System.Text.Encoding]::UTF8.GetBytes('legacy Author state must remain exact'))
    $bogusPackageRoot = Join-Path $working 'Paused package fixture'
    New-Item -ItemType Directory -Path $bogusPackageRoot -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $bogusPackageRoot 'empty-tree-guard') -Force | Out-Null
    $bogusPackage = Join-Path $bogusPackageRoot 'bogus-package.zip'
    [System.IO.File]::WriteAllBytes(
        $bogusPackage,
        [System.Text.Encoding]::UTF8.GetBytes('not a package; SDK003 must happen before this file is read'))
    $bogusPackageSha256 = Get-AuthorSdkSha256 -Path $bogusPackage
    $pausedGameTree = Get-PortableExactTreeSnapshot -Root $fakeGame
    $pausedAuthorStateTree = Get-PortableExactTreeSnapshot -Root $authorStateRoot
    $pausedPackageTree = Get-PortableExactTreeSnapshot -Root $bogusPackageRoot
    $pausedCommands = @(
        [pscustomobject]@{
            Label = 'Portable paused deploy'
            Arguments = @('deploy', $bogusPackage, '--game-root', $fakeGame, '--json')
        },
        [pscustomobject]@{
            Label = 'Portable paused update'
            Arguments = @('update', $bogusPackage, '--game-root', $fakeGame, '--json')
        },
        [pscustomobject]@{
            Label = 'Portable paused install-local'
            Arguments = @(
                'install-local', $bogusPackage,
                '--game-root', $fakeGame,
                '--expected-unique-id', 'Tests.Paused',
                '--expected-version', '1.0.0',
                '--expected-package-sha256', $bogusPackageSha256,
                '--json')
        },
        [pscustomobject]@{
            Label = 'Portable paused source local select'
            Arguments = @('source', 'local', 'select', 'Tests.Paused', $legacySource, '--game-root', $fakeGame, '--json')
        }
    )
    foreach ($pausedCommand in $pausedCommands) {
        Assert-PortablePausedMutation `
            -Label ([string]$pausedCommand.Label) `
            -Executable $exe `
            -WorkingDirectory $working `
            -ProfileRoot $profile `
            -NuGetRoot $nuget `
            -FakeGameRoot $fakeGame `
            -Arguments @($pausedCommand.Arguments) `
            -ExpectedGameTree $pausedGameTree `
            -AuthorStateRoot $authorStateRoot `
            -ExpectedAuthorStateTree $pausedAuthorStateTree `
            -BogusPackageRoot $bogusPackageRoot `
            -ExpectedBogusPackageTree $pausedPackageTree `
            -BogusPackagePath $bogusPackage `
            -ExpectedBogusPackageSha256 $bogusPackageSha256
    }
    Write-Host 'Packaged paused mutation matrix: PASS (4/4 SDK003; exact game/state/package identities unchanged)'

    $playerDllLock = New-Object System.IO.FileStream($playerDll, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::None)

    $version = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('version')
    Assert-PortableSuccess -Label 'Portable version' -Result $version
    if ($version.StdOut.IndexOf('0.1.0', [System.StringComparison]::Ordinal) -lt 0 -or $version.StdOut.IndexOf('0.5.5', [System.StringComparison]::Ordinal) -lt 0) {
        throw "Portable version output does not identify SDK 0.1.0 / Runtime 0.5.5: $($version.StdOut)"
    }
    $help = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('help')
    Assert-PortableSuccess -Label 'Portable help' -Result $help
    if ($help.StdOut.IndexOf('dtmapi-author doctor', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw 'Portable help does not expose the integrated read-only Doctor command.'
    }

    $project = Join-Path $working ($authorExampleLabel + ' Mod')
    $new = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('new', 'codemod', $project, '--id', 'Portable.Sample', '--name', 'Portable Sample', '--author', 'DTMAPI', '--json')
    Assert-PortableSuccess -Label 'Portable new' -Result $new
    $null = $new.StdOut | ConvertFrom-Json
    $validate = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('validate', $project, '--json')
    Assert-PortableSuccess -Label 'Portable validate' -Result $validate
    $null = $validate.StdOut | ConvertFrom-Json
    $build = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('build', $project, '--json')
    Assert-PortableSuccess -Label 'Portable build' -Result $build
    $buildReport = $build.StdOut | ConvertFrom-Json
    $buildOutputPath = [string]$buildReport.outputPath
    for ($attempt = 0; $attempt -lt 20 -and -not (Test-Path -LiteralPath $buildOutputPath -PathType Leaf); $attempt++) {
        Start-Sleep -Milliseconds 50
    }
    if (-not (Test-Path -LiteralPath $buildOutputPath -PathType Leaf)) {
        throw "Portable CodeMod build did not create the reported DLL '$buildOutputPath'. report=$($build.StdOut)"
    }

    $packageOutput = Join-Path $working $packageOutputLabel
    $pack = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('pack', $project, '--output', $packageOutput, '--json')
    Assert-PortableSuccess -Label 'Portable pack' -Result $pack
    $packReport = $pack.StdOut | ConvertFrom-Json
    $modZip = [string]$packReport.outputPath
    for ($attempt = 0; $attempt -lt 20 -and -not (Test-Path -LiteralPath $modZip -PathType Leaf); $attempt++) {
        Start-Sleep -Milliseconds 50
    }
    if (-not (Test-Path -LiteralPath $modZip -PathType Leaf)) { throw "Portable pack did not create the reported deterministic package '$modZip'." }

    $doctorTarget = Join-Path $working ('Doctor ' + $doctorTargetLabel)
    Expand-AuthorSdkZipSafely -ZipPath $modZip -Destination $doctorTarget | Out-Null
    $doctorTreeBefore = Get-AuthorSdkFileTreeDigestV1 -Root $doctorTarget
    $doctorHuman = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('doctor', $doctorTarget)
    Assert-PortableSuccess -Label 'Portable Doctor human' -Result $doctorHuman
    if ($doctorHuman.StdOut.IndexOf('DTMAPI Install Doctor', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Portable Doctor human output is missing its read-only report header: $($doctorHuman.StdOut)"
    }
    $doctorJson = Invoke-PortableAuthor -Executable $exe -WorkingDirectory $working -ProfileRoot $profile -NuGetRoot $nuget -FakeGameRoot $fakeGame -Arguments @('doctor', $doctorTarget, '--json')
    Assert-PortableSuccess -Label 'Portable Doctor JSON' -Result $doctorJson
    $doctorReport = $doctorJson.StdOut | ConvertFrom-Json
    if ($null -eq $doctorReport) { throw 'Portable Doctor JSON did not return a JSON object.' }
    $doctorTreeAfter = Get-AuthorSdkFileTreeDigestV1 -Root $doctorTarget
    if ($doctorTreeBefore -ne $doctorTreeAfter) { throw 'Portable Doctor changed the inspected package tree.' }

    $playerDllLock.Dispose()
    $playerDllLock = $null
    if ((Get-AuthorSdkSha256 -Path $playerDll) -ne $playerDllHash -or (Get-Item -LiteralPath $playerDll).LastWriteTimeUtc -ne $playerDllWriteTime) {
        throw 'Portable Author SDK modified the decoy player-install DTMAPI.Abstractions.dll.'
    }
    if (@(Get-ChildItem -LiteralPath $nuget -Force -Recurse).Count -ne 0) {
        throw 'Portable Author SDK wrote into the isolated empty NuGet cache; the offline compiler is not self-contained.'
    }

    Write-Host 'Author SDK portable gate: PASS'
    Write-Host "Package: $packageFull"
    Write-Host "Test root: $temporaryRoot"
    Write-Host 'Environment: empty PATH/DOTNET_ROOT, isolated USERPROFILE/HOME/NUGET_PACKAGES, spaces + non-ASCII path'
}
finally {
    if ($null -ne $playerDllLock) {
        $playerDllLock.Dispose()
    }
    if (-not $KeepTemp -and (Test-Path -LiteralPath $temporaryRoot)) {
        Remove-AuthorSdkTreeSafely -AllowedRoot $temporaryBase -Path $temporaryRoot
    }
    elseif ($KeepTemp) {
        Write-Host "Portable evidence retained: $temporaryRoot"
    }
}
