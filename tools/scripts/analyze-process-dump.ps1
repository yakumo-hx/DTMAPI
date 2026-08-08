param(
    [Parameter(Mandatory = $true)]
    [string] $DumpPath,

    [Parameter(Mandatory = $true)]
    [string] $OutputDir
)

$ErrorActionPreference = 'Stop'

function Add-Text {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [AllowNull()] [string] $Text
    )

    if ($null -eq $Text) {
        '' | Add-Content -LiteralPath $Path
        return
    }

    $Text | Add-Content -LiteralPath $Path
}

function Find-Executable {
    param([Parameter(Mandatory = $true)] [string] $Name)

    $where = & where.exe $Name 2>$null
    if ($LASTEXITCODE -eq 0) {
        foreach ($entry in @($where)) {
            if (-not [string]::IsNullOrWhiteSpace($entry) -and (Test-Path -LiteralPath $entry)) {
                return (Resolve-Path -LiteralPath $entry).Path
            }
        }
    }

    return $null
}

function Find-Debugger {
    $candidates = New-Object 'System.Collections.Generic.List[string]'

    foreach ($name in @('cdb.exe', 'windbg.exe')) {
        $path = Find-Executable -Name $name
        if ($path) {
            $candidates.Add($path) | Out-Null
        }
    }

    foreach ($path in @(
            'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe',
            'C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe',
            'C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe',
            'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\windbg.exe',
            'C:\Program Files\Windows Kits\10\Debuggers\x64\windbg.exe')) {
        if (Test-Path -LiteralPath $path) {
            $candidates.Add((Resolve-Path -LiteralPath $path).Path) | Out-Null
        }
    }

    $vswhere = Find-Executable -Name 'vswhere.exe'
    if (-not $vswhere) {
        foreach ($path in @(
                'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe',
                'C:\Program Files\Microsoft Visual Studio\Installer\vswhere.exe')) {
            if (Test-Path -LiteralPath $path) {
                $vswhere = (Resolve-Path -LiteralPath $path).Path
                break
            }
        }
    }

    if ($vswhere) {
        try {
            $installations = & $vswhere -all -products * -property installationPath 2>$null
            foreach ($install in @($installations)) {
                if ([string]::IsNullOrWhiteSpace($install)) {
                    continue
                }

                foreach ($relative in @(
                        'Common7\IDE\Remote Debugger\x64\cdb.exe',
                        'Common7\IDE\Remote Debugger\x64\windbg.exe',
                        'Common7\IDE\CommonExtensions\Microsoft\Debugger\cdb.exe',
                        'Common7\IDE\CommonExtensions\Microsoft\Debugger\windbg.exe')) {
                    $candidate = Join-Path $install $relative
                    if (Test-Path -LiteralPath $candidate) {
                        $candidates.Add((Resolve-Path -LiteralPath $candidate).Path) | Out-Null
                    }
                }
            }
        }
        catch {
            # vswhere is only a discovery helper; keep searching with the fixed candidates.
        }
    }

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if ((Split-Path -Leaf $candidate).Equals('cdb.exe', [StringComparison]::OrdinalIgnoreCase)) {
            return $candidate
        }
    }

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        return $candidate
    }

    return $null
}

function Get-SectionText {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Lines,
        [Parameter(Mandatory = $true)] [string] $StartPattern,
        [Parameter(Mandatory = $true)] [string[]] $StopPatterns
    )

    $collecting = $false
    $result = New-Object 'System.Collections.Generic.List[string]'
    foreach ($line in $Lines) {
        if (-not $collecting -and $line -match $StartPattern) {
            $collecting = $true
        }

        if ($collecting) {
            foreach ($stopPattern in $StopPatterns) {
                if ($result.Count -gt 0 -and $line -match $stopPattern) {
                    return @($result)
                }
            }

            $result.Add($line) | Out-Null
        }
    }

    return @($result)
}

$resolvedDumpPath = (Resolve-Path -LiteralPath $DumpPath).Path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$resolvedOutputDir = (Resolve-Path -LiteralPath $OutputDir).Path

$summaryPath = Join-Path $resolvedOutputDir 'dump-analysis-summary.txt'
$commandPath = Join-Path $resolvedOutputDir 'windbg-command.txt'
$stdoutPath = Join-Path $resolvedOutputDir 'windbg-output.txt'
$stderrPath = Join-Path $resolvedOutputDir 'windbg-error.txt'
$modulesPath = Join-Path $resolvedOutputDir 'modules.txt'
$threadsPath = Join-Path $resolvedOutputDir 'threads.txt'
$exceptionPath = Join-Path $resolvedOutputDir 'exception-context.txt'
$missingPath = Join-Path $resolvedOutputDir 'MISSING-WINDBG-README.txt'

Remove-Item -LiteralPath $summaryPath, $commandPath, $stdoutPath, $stderrPath, $modulesPath, $threadsPath, $exceptionPath, $missingPath -Force -ErrorAction SilentlyContinue

$dumpItem = Get-Item -LiteralPath $resolvedDumpPath
$dumpHash = (Get-FileHash -LiteralPath $resolvedDumpPath -Algorithm SHA256).Hash
$startedAt = Get-Date
$debugger = Find-Debugger

$summary = New-Object 'System.Collections.Generic.List[string]'
$summary.Add("DumpPath=$resolvedDumpPath") | Out-Null
$summary.Add("DumpSize=$($dumpItem.Length)") | Out-Null
$summary.Add("DumpSha256=$dumpHash") | Out-Null
$summary.Add("AnalysisStartedAt=$($startedAt.ToString('o'))") | Out-Null

if (-not $debugger) {
    $finishedAt = Get-Date
    $summary.Add('DebuggerFound=False') | Out-Null
    $summary.Add('DebuggerPath=') | Out-Null
    $summary.Add("AnalysisFinishedAt=$($finishedAt.ToString('o'))") | Out-Null
    $summary.Add('ExitCode=') | Out-Null
    $summary.Add('CrashThreadGuess=unknown') | Out-Null
    $summary.Add('HasMonoFrames=unknown') | Out-Null
    $summary.Add('HasTerrainFrames=unknown') | Out-Null
    $summary.Add('HasLoadGameFrames=unknown') | Out-Null
    $summary.Add('HasDtmapiFrames=unknown') | Out-Null
    $summary.Add('HasBepInExFrames=unknown') | Out-Null
    $summary.Add('HasHarmonyFrames=unknown') | Out-Null
    $summary.Add('TopStackSummary=missing-debugger') | Out-Null
    $summary | Set-Content -LiteralPath $summaryPath -Encoding UTF8

    @"
No Windows debugger was found, so this dump was not decoded.

Looked for cdb.exe and windbg.exe through PATH, common Windows Kits paths, and Visual Studio/vswhere discovery.

DumpPath=$resolvedDumpPath
DumpSize=$($dumpItem.Length)
DumpSha256=$dumpHash

Install Windows Debugging Tools or provide cdb.exe/windbg.exe on PATH, then rerun:
tools/scripts/analyze-process-dump.ps1 -DumpPath "$resolvedDumpPath" -OutputDir "$resolvedOutputDir"
"@ | Set-Content -LiteralPath $missingPath -Encoding UTF8

    Write-Host "No debugger found. Wrote $missingPath"
    exit 0
}

$summary.Add('DebuggerFound=True') | Out-Null
$summary.Add("DebuggerPath=$debugger") | Out-Null

$commandsPath = Join-Path $resolvedOutputDir 'windbg-commands.txt'
@(
    '.symfix',
    '.reload',
    'vertarget',
    'lm',
    '~* kP 80',
    '.ecxr',
    'kP 80',
    '!analyze -v',
    'q'
) | Set-Content -LiteralPath $commandsPath -Encoding ASCII

$debuggerLeaf = Split-Path -Leaf $debugger
$arguments = if ($debuggerLeaf.Equals('cdb.exe', [StringComparison]::OrdinalIgnoreCase)) {
    @('-z', $resolvedDumpPath, '-cf', $commandsPath)
}
else {
    @('-z', $resolvedDumpPath, '-cf', $commandsPath, '-logo', $stdoutPath)
}

Add-Text -Path $commandPath -Text ("`"$debugger`" " + ($arguments | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' ')
$process = Start-Process -FilePath $debugger -ArgumentList $arguments -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
$finishedAt = Get-Date

if (-not (Test-Path -LiteralPath $stdoutPath)) {
    '' | Set-Content -LiteralPath $stdoutPath -Encoding UTF8
}

if (-not (Test-Path -LiteralPath $stderrPath)) {
    '' | Set-Content -LiteralPath $stderrPath -Encoding UTF8
}

$stdout = Get-Content -LiteralPath $stdoutPath -Raw -ErrorAction SilentlyContinue
$lines = if ($stdout) { $stdout -split "`r?`n" } else { @() }

$modules = Get-SectionText -Lines $lines -StartPattern '^start\s+end\s+module name|^ModLoad:|^Loaded Module' -StopPatterns @('^~\*', '^\.ecxr', '^!analyze')
$threads = Get-SectionText -Lines $lines -StartPattern '^\.?[0-9]+\s+Id:|^[0-9]+\s+Id:|^~\*' -StopPatterns @('^\.ecxr', '^!analyze', '^quit:')
$exception = Get-SectionText -Lines $lines -StartPattern '^\.ecxr|^ExceptionAddress|^CONTEXT:|^STACK_TEXT:' -StopPatterns @('^quit:', '^kd>', '^0:000>')

$modules | Set-Content -LiteralPath $modulesPath -Encoding UTF8
$threads | Set-Content -LiteralPath $threadsPath -Encoding UTF8
$exception | Set-Content -LiteralPath $exceptionPath -Encoding UTF8

$hasMono = $stdout -match '(?i)\bmono_|mono-2\.0|libmono|Mono'
$hasTerrain = $stdout -match '(?i)TerrainLayer|Terrain\.Create|Room\.ResetTerrain|Room\.AfterLoadData|Dungeon\.AfterLoadData'
$hasLoadGame = $stdout -match '(?i)DataPersistenceManager\.LoadGame|DolocAPI.*LoadGame|LoadGame'
$hasDtmapi = $stdout -match '(?i)DTMAPI|DtmApiRuntime|DolocTownHookCallbacks'
$hasBepInEx = $stdout -match '(?i)BepInEx'
$hasHarmony = $stdout -match '(?i)Harmony|HarmonyLib'

$topStackLines = @($lines | Where-Object { $_ -match '(?i)mono_|Terrain|Room|Dungeon|DataPersistenceManager|DolocAPI|DTMAPI|BepInEx|Harmony' } | Select-Object -First 12)
$topStackSummary = if ($topStackLines.Count -gt 0) { ($topStackLines -join ' | ') } else { 'none-detected' }
if ($topStackSummary.Length -gt 1800) {
    $topStackSummary = $topStackSummary.Substring(0, 1800) + '...'
}

$crashThreadGuess = 'unknown'
$threadLine = $lines | Where-Object { $_ -match '^\s*\#?\s*[0-9a-f`]+\s+.*(mono_|Terrain|Room|Dungeon|LoadGame|DTMAPI)' } | Select-Object -First 1
if ($threadLine) {
    $crashThreadGuess = $threadLine.Trim()
}

$summary.Add("AnalysisFinishedAt=$($finishedAt.ToString('o'))") | Out-Null
$summary.Add("ExitCode=$($process.ExitCode)") | Out-Null
$summary.Add("CrashThreadGuess=$crashThreadGuess") | Out-Null
$summary.Add("HasMonoFrames=$hasMono") | Out-Null
$summary.Add("HasTerrainFrames=$hasTerrain") | Out-Null
$summary.Add("HasLoadGameFrames=$hasLoadGame") | Out-Null
$summary.Add("HasDtmapiFrames=$hasDtmapi") | Out-Null
$summary.Add("HasBepInExFrames=$hasBepInEx") | Out-Null
$summary.Add("HasHarmonyFrames=$hasHarmony") | Out-Null
$summary.Add("TopStackSummary=$topStackSummary") | Out-Null
$summary | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "Dump analysis complete. Summary: $summaryPath"
