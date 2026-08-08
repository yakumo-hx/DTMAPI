[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-DtmRecoveryTest {
    param(
        [bool] $Condition,
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-DtmRecoveryTestSha256 {
    param([string] $Path)

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Write-DtmRecoveryTestBytes {
    param(
        [string] $Path,
        [byte[]] $Bytes
    )

    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($Path)) | Out-Null
    [System.IO.File]::WriteAllBytes($Path, $Bytes)
}

function Write-DtmRecoveryTestText {
    param(
        [string] $Path,
        [string] $Text
    )

    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

function Write-DtmRecoveryTestManifest {
    param(
        [string] $Path,
        [string] $DamagedHash,
        [long] $DamagedLength,
        [string] $RecoveryHash,
        [long] $RecoveryLength
    )

    $manifest = [ordered]@{
        FormatVersion = 1
        CaseId = 'TEST-player-slot0'
        SlotIndex = 0
        TargetFileName = 'doloc-save-0.data'
        RecoverySourceFileName = 'verified-recovery-source.data'
        DamagedCurrentSha256 = $DamagedHash
        DamagedCurrentLength = $DamagedLength
        RecoverySha256 = $RecoveryHash
        RecoveryLength = $RecoveryLength
        TestFixture = $true
    }
    Write-DtmRecoveryTestText -Path $Path -Text (($manifest | ConvertTo-Json -Depth 3) + [Environment]::NewLine)
}

function Invoke-DtmRecoveryTestPowerShell {
    param(
        [string] $WinPs,
        [string] $Script,
        [string] $Manifest,
        [string] $Source,
        [string] $SaveRoot,
        [string] $OutputBase
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $WinPs
    $startInfo.Arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "' + $Script + '" -ManifestPath "' + $Manifest + '" -RecoverySourcePath "' + $Source + '" -SaveDirectory "' + $SaveRoot + '" -SkipProcessCheck'
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw 'Could not start Windows PowerShell recovery test.'
    }
    if (-not $process.WaitForExit(60000)) {
        try { $process.Kill() } catch { }
        throw 'Windows PowerShell recovery test timed out.'
    }
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    Write-DtmRecoveryTestText -Path ($OutputBase + '.stdout.txt') -Text $stdout
    Write-DtmRecoveryTestText -Path ($OutputBase + '.stderr.txt') -Text $stderr
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        StdOut = $stdout
        StdErr = $stderr
    }
}

function Invoke-DtmRecoveryTestBat {
    param(
        [string] $BatPath,
        [string] $UserProfileRoot,
        [string] $OutputBase
    )

    $oldProfile = [Environment]::GetEnvironmentVariable('USERPROFILE', 'Process')
    $oldPause = [Environment]::GetEnvironmentVariable('DTMAPI_RECOVERY_NO_PAUSE', 'Process')
    $oldSkip = [Environment]::GetEnvironmentVariable('DTMAPI_RECOVERY_TEST_SKIP_PROCESS_CHECK', 'Process')
    [Environment]::SetEnvironmentVariable('USERPROFILE', $UserProfileRoot, 'Process')
    [Environment]::SetEnvironmentVariable('DTMAPI_RECOVERY_NO_PAUSE', '1', 'Process')
    [Environment]::SetEnvironmentVariable('DTMAPI_RECOVERY_TEST_SKIP_PROCESS_CHECK', '1', 'Process')
    try {
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $env:ComSpec
        $startInfo.Arguments = '/d /e:off /v:off /c call "' + $BatPath + '"'
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw 'Could not start recovery BAT.'
        }
        if (-not $process.WaitForExit(60000)) {
            try { $process.Kill() } catch { }
            throw 'Recovery BAT timed out.'
        }
        $stdout = $process.StandardOutput.ReadToEnd()
        $stderr = $process.StandardError.ReadToEnd()
        Write-DtmRecoveryTestText -Path ($OutputBase + '.stdout.txt') -Text $stdout
        Write-DtmRecoveryTestText -Path ($OutputBase + '.stderr.txt') -Text $stderr
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            StdOut = $stdout
            StdErr = $stderr
        }
    }
    finally {
        [Environment]::SetEnvironmentVariable('USERPROFILE', $oldProfile, 'Process')
        [Environment]::SetEnvironmentVariable('DTMAPI_RECOVERY_NO_PAUSE', $oldPause, 'Process')
        [Environment]::SetEnvironmentVariable('DTMAPI_RECOVERY_TEST_SKIP_PROCESS_CHECK', $oldSkip, 'Process')
    }
}

$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$sourceRoot = Join-Path $repo 'tools\release\player-slot0-recovery'
$sourceScript = Join-Path $sourceRoot 'restore-verified-slot.ps1'
$sourceBat = Join-Path $sourceRoot '1_restore_verified_slot0.bat'
$builder = Join-Path $repo 'tools\scripts\build-player-slot0-recovery.ps1'
$winPs = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
foreach ($required in @($sourceScript, $sourceBat, $builder, $winPs)) {
    Assert-DtmRecoveryTest -Condition ([System.IO.File]::Exists($required)) -Message ('Required test input is missing: ' + $required)
}

$sessionFull = [System.IO.Path]::GetFullPath((Join-Path $repo ('temp\player-slot0-recovery-test-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))))
$allowedPrefix = ([System.IO.Path]::GetFullPath((Join-Path $repo 'temp'))).TrimEnd('\') + '\'
Assert-DtmRecoveryTest -Condition ($sessionFull.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) -Message 'Test session escaped repository temp.'
[System.IO.Directory]::CreateDirectory($sessionFull) | Out-Null

$parseTargets = @($sourceScript, $builder, $PSCommandPath)
foreach ($parseTarget in $parseTargets) {
    $env:DTMAPI_PARSE_TARGET = $parseTarget
    $parseOutput = & $winPs -NoLogo -NoProfile -Command '$tokens=$null;$errors=$null;[System.Management.Automation.Language.Parser]::ParseFile($env:DTMAPI_PARSE_TARGET,[ref]$tokens,[ref]$errors)|Out-Null;$errors|ForEach-Object{$_.ToString()};if($errors.Count){exit 1}' 2>&1
    Assert-DtmRecoveryTest -Condition ($LASTEXITCODE -eq 0) -Message ('Windows PowerShell parser failed for ' + $parseTarget + ': ' + ($parseOutput -join ' | '))
}

$nonAscii = ([char]0x4E2D).ToString() + ([char]0x6587).ToString()
$packageRoot = Join-Path $sessionFull ('Program Files (x86);' + $nonAscii + ' & recovery\package')
[System.IO.Directory]::CreateDirectory($packageRoot) | Out-Null
foreach ($name in @('1_restore_verified_slot0.bat', 'restore-verified-slot.ps1', 'README.txt')) {
    [System.IO.File]::Copy((Join-Path $sourceRoot $name), (Join-Path $packageRoot $name), $false)
}

$badBytes = New-Object byte[] 4096
$goodBytes = New-Object byte[] 6144
$prevOneBytes = New-Object byte[] 2048
for ($i = 0; $i -lt $badBytes.Length; $i++) { $badBytes[$i] = [byte](($i * 17 + 3) % 251) }
for ($i = 0; $i -lt $goodBytes.Length; $i++) { $goodBytes[$i] = [byte](($i * 29 + 7) % 253) }
for ($i = 0; $i -lt $prevOneBytes.Length; $i++) { $prevOneBytes[$i] = [byte](($i * 11 + 5) % 247) }
$sourceFile = Join-Path $packageRoot 'verified-recovery-source.data'
Write-DtmRecoveryTestBytes -Path $sourceFile -Bytes $goodBytes
$badFixture = Join-Path $sessionFull 'hash-fixture-bad.data'
$goodFixture = Join-Path $sessionFull 'hash-fixture-good.data'
Write-DtmRecoveryTestBytes -Path $badFixture -Bytes $badBytes
Write-DtmRecoveryTestBytes -Path $goodFixture -Bytes $goodBytes
$badHash = Get-DtmRecoveryTestSha256 -Path $badFixture
$goodHash = Get-DtmRecoveryTestSha256 -Path $goodFixture
$manifestFile = Join-Path $packageRoot 'recovery-manifest.json'
Write-DtmRecoveryTestManifest -Path $manifestFile -DamagedHash $badHash -DamagedLength $badBytes.Length -RecoveryHash $goodHash -RecoveryLength $goodBytes.Length

$userProfile = Join-Path $sessionFull 'player profile'
$saveRoot = Join-Path $userProfile 'AppData\LocalLow\RedSawGames\DolocTown\SAVE'
[System.IO.Directory]::CreateDirectory($saveRoot) | Out-Null
$currentFile = Join-Path $saveRoot 'doloc-save-0.data'
$prevZeroFile = Join-Path $saveRoot 'doloc-save-0.data.prev0'
$prevOneFile = Join-Path $saveRoot 'doloc-save-0.data.prev1'
$flagsFile = Join-Path $saveRoot 'flags.json'
Write-DtmRecoveryTestBytes -Path $currentFile -Bytes $badBytes
Write-DtmRecoveryTestBytes -Path $prevZeroFile -Bytes $goodBytes
Write-DtmRecoveryTestBytes -Path $prevOneFile -Bytes $prevOneBytes
Write-DtmRecoveryTestText -Path $flagsFile -Text "{}`r`n"
$protectedBefore = @{}
foreach ($path in @($prevZeroFile, $prevOneFile, $flagsFile)) {
    $protectedBefore[$path] = Get-DtmRecoveryTestSha256 -Path $path
}
$namesBefore = @([System.IO.Directory]::GetFiles($saveRoot) | ForEach-Object { [System.IO.Path]::GetFileName($_) } | Sort-Object)

$success = Invoke-DtmRecoveryTestPowerShell -WinPs $winPs -Script (Join-Path $packageRoot 'restore-verified-slot.ps1') -Manifest $manifestFile -Source $sourceFile -SaveRoot $saveRoot -OutputBase (Join-Path $sessionFull '01-success')
Assert-DtmRecoveryTest -Condition ($success.ExitCode -eq 0) -Message ('Successful recovery returned ' + $success.ExitCode)
Assert-DtmRecoveryTest -Condition ((Get-DtmRecoveryTestSha256 -Path $currentFile) -eq $goodHash) -Message 'Successful recovery did not publish exact recovery bytes.'
foreach ($path in $protectedBefore.Keys) {
    Assert-DtmRecoveryTest -Condition ((Get-DtmRecoveryTestSha256 -Path $path) -eq $protectedBefore[$path]) -Message ('Protected SAVE file changed: ' + $path)
}
$namesAfter = @([System.IO.Directory]::GetFiles($saveRoot) | ForEach-Object { [System.IO.Path]::GetFileName($_) } | Sort-Object)
Assert-DtmRecoveryTest -Condition (($namesBefore -join "`n") -ceq ($namesAfter -join "`n")) -Message 'Recovery created a backup, receipt or leftover partial in SAVE.'

$idempotent = Invoke-DtmRecoveryTestPowerShell -WinPs $winPs -Script (Join-Path $packageRoot 'restore-verified-slot.ps1') -Manifest $manifestFile -Source $sourceFile -SaveRoot $saveRoot -OutputBase (Join-Path $sessionFull '02-idempotent')
Assert-DtmRecoveryTest -Condition ($idempotent.ExitCode -eq 0 -and $idempotent.StdOut.Contains('already restored')) -Message 'Idempotent recovery did not return explicit success.'

$changedBytes = New-Object byte[] 3072
for ($i = 0; $i -lt $changedBytes.Length; $i++) { $changedBytes[$i] = [byte](($i * 31 + 9) % 241) }
Write-DtmRecoveryTestBytes -Path $currentFile -Bytes $changedBytes
$changedHash = Get-DtmRecoveryTestSha256 -Path $currentFile
$changed = Invoke-DtmRecoveryTestPowerShell -WinPs $winPs -Script (Join-Path $packageRoot 'restore-verified-slot.ps1') -Manifest $manifestFile -Source $sourceFile -SaveRoot $saveRoot -OutputBase (Join-Path $sessionFull '03-changed-current')
Assert-DtmRecoveryTest -Condition ($changed.ExitCode -eq 30) -Message 'Changed current did not fail closed with exit 30.'
Assert-DtmRecoveryTest -Condition ((Get-DtmRecoveryTestSha256 -Path $currentFile) -eq $changedHash) -Message 'Changed current was modified on refusal.'

Write-DtmRecoveryTestBytes -Path $currentFile -Bytes $badBytes
$tamperedSource = New-Object byte[] $goodBytes.Length
[Array]::Copy($goodBytes, $tamperedSource, $goodBytes.Length)
$tamperedSource[0] = [byte]($tamperedSource[0] -bxor 255)
Write-DtmRecoveryTestBytes -Path $sourceFile -Bytes $tamperedSource
$sourceMismatch = Invoke-DtmRecoveryTestPowerShell -WinPs $winPs -Script (Join-Path $packageRoot 'restore-verified-slot.ps1') -Manifest $manifestFile -Source $sourceFile -SaveRoot $saveRoot -OutputBase (Join-Path $sessionFull '04-source-mismatch')
Assert-DtmRecoveryTest -Condition ($sourceMismatch.ExitCode -eq 30) -Message 'Tampered recovery source did not fail closed with exit 30.'
Assert-DtmRecoveryTest -Condition ((Get-DtmRecoveryTestSha256 -Path $currentFile) -eq $badHash) -Message 'Current changed after recovery-source refusal.'
Write-DtmRecoveryTestBytes -Path $sourceFile -Bytes $goodBytes

[System.IO.File]::Delete($currentFile)
$missing = Invoke-DtmRecoveryTestPowerShell -WinPs $winPs -Script (Join-Path $packageRoot 'restore-verified-slot.ps1') -Manifest $manifestFile -Source $sourceFile -SaveRoot $saveRoot -OutputBase (Join-Path $sessionFull '05-missing-current')
Assert-DtmRecoveryTest -Condition ($missing.ExitCode -eq 30) -Message 'Missing current did not fail closed with exit 30.'
Write-DtmRecoveryTestBytes -Path $currentFile -Bytes $badBytes

$bat = Join-Path $packageRoot '1_restore_verified_slot0.bat'
$batResult = Invoke-DtmRecoveryTestBat -BatPath $bat -UserProfileRoot $userProfile -OutputBase (Join-Path $sessionFull '06-bat-special-path')
Assert-DtmRecoveryTest -Condition ($batResult.ExitCode -eq 0) -Message ('BAT special-path recovery returned ' + $batResult.ExitCode + ': ' + $batResult.StdOut)
Assert-DtmRecoveryTest -Condition ((Get-DtmRecoveryTestSha256 -Path $currentFile) -eq $goodHash) -Message 'BAT special-path recovery did not restore exact bytes.'

Add-Type -AssemblyName System.IO.Compression.FileSystem
$syntheticSupportRoot = Join-Path $sessionFull 'synthetic-support'
$syntheticSave = Join-Path $syntheticSupportRoot 'PlayerData\Persistent-01-DolocTown\SAVE'
[System.IO.Directory]::CreateDirectory($syntheticSave) | Out-Null
Write-DtmRecoveryTestBytes -Path (Join-Path $syntheticSave 'doloc-save-0.data') -Bytes $badBytes
Write-DtmRecoveryTestBytes -Path (Join-Path $syntheticSave 'doloc-save-0.data.prev0') -Bytes $goodBytes
$syntheticSupportZip = Join-Path $sessionFull 'synthetic-support.zip'
[System.IO.Compression.ZipFile]::CreateFromDirectory($syntheticSupportRoot, $syntheticSupportZip)
$builtZip = Join-Path $sessionFull 'built-player-recovery.zip'
$buildResult = & $winPs -NoLogo -NoProfile -ExecutionPolicy Bypass -File $builder -SupportZip $syntheticSupportZip -OutputZip $builtZip -CaseId 'synthetic-case' -ExpectedDamagedSha256 $badHash -ExpectedRecoverySha256 $goodHash 2>&1
$buildExit = $LASTEXITCODE
Write-DtmRecoveryTestText -Path (Join-Path $sessionFull '07-builder-output.txt') -Text ($buildResult -join [Environment]::NewLine)
Assert-DtmRecoveryTest -Condition ($buildExit -eq 0 -and [System.IO.File]::Exists($builtZip)) -Message ('Recovery builder failed: ' + ($buildResult -join ' | '))
$builtArchive = [System.IO.Compression.ZipFile]::OpenRead($builtZip)
try {
    $builtNames = @($builtArchive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') } | Sort-Object)
    $expectedBuiltNames = @('1_restore_verified_slot0.bat', 'README.txt', 'recovery-manifest.json', 'restore-verified-slot.ps1', 'verified-recovery-source.data') | Sort-Object
    Assert-DtmRecoveryTest -Condition (($builtNames -join "`n") -ceq ($expectedBuiltNames -join "`n")) -Message 'Builder ZIP entry set is not exact.'
    $builtSourceEntry = @($builtArchive.Entries | Where-Object { $_.FullName -ceq 'verified-recovery-source.data' })[0]
    $builtSourceStream = $builtSourceEntry.Open()
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $builtSourceHash = ([BitConverter]::ToString($sha.ComputeHash($builtSourceStream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $builtSourceStream.Dispose()
    }
    Assert-DtmRecoveryTest -Condition ($builtSourceHash -eq $goodHash) -Message 'Builder ZIP recovery source hash mismatch.'
}
finally {
    $builtArchive.Dispose()
}

$preparedBytes = New-Object byte[] 7168
for ($i = 0; $i -lt $preparedBytes.Length; $i++) { $preparedBytes[$i] = [byte](($i * 37 + 11) % 239) }
$preparedFile = Join-Path $sessionFull 'prepared-recovery-source.data'
Write-DtmRecoveryTestBytes -Path $preparedFile -Bytes $preparedBytes
$preparedHash = Get-DtmRecoveryTestSha256 -Path $preparedFile
$preparedBuiltZip = Join-Path $sessionFull 'built-player-prepared-recovery.zip'
$preparedBuildResult = & $winPs -NoLogo -NoProfile -ExecutionPolicy Bypass -File $builder -SupportZip $syntheticSupportZip -OutputZip $preparedBuiltZip -CaseId 'synthetic-prepared-case' -ExpectedDamagedSha256 $badHash -ExpectedRecoverySha256 $preparedHash -PreparedRecoveryFile $preparedFile 2>&1
$preparedBuildExit = $LASTEXITCODE
Write-DtmRecoveryTestText -Path (Join-Path $sessionFull '08-prepared-builder-output.txt') -Text ($preparedBuildResult -join [Environment]::NewLine)
Assert-DtmRecoveryTest -Condition ($preparedBuildExit -eq 0 -and [System.IO.File]::Exists($preparedBuiltZip)) -Message ('Prepared recovery builder failed: ' + ($preparedBuildResult -join ' | '))
$preparedArchive = [System.IO.Compression.ZipFile]::OpenRead($preparedBuiltZip)
try {
    $preparedSourceEntry = @($preparedArchive.Entries | Where-Object { $_.FullName -ceq 'verified-recovery-source.data' })[0]
    $preparedSourceStream = $preparedSourceEntry.Open()
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $preparedBuiltHash = ([BitConverter]::ToString($sha.ComputeHash($preparedSourceStream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $preparedSourceStream.Dispose()
    }
    Assert-DtmRecoveryTest -Condition ($preparedBuiltHash -eq $preparedHash) -Message 'Prepared builder ZIP recovery source hash mismatch.'
    $preparedManifestEntry = @($preparedArchive.Entries | Where-Object { $_.FullName -ceq 'recovery-manifest.json' })[0]
    $manifestReader = New-Object System.IO.StreamReader($preparedManifestEntry.Open())
    try {
        $preparedManifest = $manifestReader.ReadToEnd() | ConvertFrom-Json
    }
    finally {
        $manifestReader.Dispose()
    }
    Assert-DtmRecoveryTest -Condition ($preparedManifest.RecoverySourceKind -ceq 'PreparedEncryptedArchive') -Message 'Prepared builder source kind was not recorded.'
    Assert-DtmRecoveryTest -Condition ($preparedManifest.RecoverySourceFileName -ceq 'verified-recovery-source.data') -Message 'Prepared builder fixed package filename was not preserved.'
    Assert-DtmRecoveryTest -Condition ($preparedManifest.RecoveryOriginalFileName -ceq ([System.IO.Path]::GetFileName($preparedFile))) -Message 'Prepared builder original filename was not recorded.'
}
finally {
    $preparedArchive.Dispose()
}

$sourceAsciiFiles = @($sourceBat, $sourceScript, $builder, $PSCommandPath)
foreach ($path in $sourceAsciiFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    Assert-DtmRecoveryTest -Condition (-not ($bytes | Where-Object { $_ -gt 127 } | Select-Object -First 1)) -Message ('ASCII-only source check failed: ' + $path)
}

$summary = @(
    'Status=Passed',
    'WindowsPowerShellParser=Passed',
    'AtomicRestore=Passed',
    'NoPlayerBackup=Passed',
    'BackupFamilyUnchanged=Passed',
    'Idempotence=Passed',
    'ChangedCurrentRefusal=Passed',
    'TamperedSourceRefusal=Passed',
    'MissingCurrentRefusal=Passed',
    'BatSpecialPath=Passed',
    'BuilderExactPackage=Passed',
    'PreparedRecoveryBuilder=Passed'
)
Write-DtmRecoveryTestText -Path (Join-Path $sessionFull 'test-summary.txt') -Text (($summary -join [Environment]::NewLine) + [Environment]::NewLine)
Write-Output ('Player slot 0 recovery tests passed. Evidence: ' + $sessionFull)
