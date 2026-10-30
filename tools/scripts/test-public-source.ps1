param(
    [ValidateSet('Debug','Release')][string]$Configuration='Release',
    [ValidateSet('PublicWindows','Generic')][string]$Profile='PublicWindows',
    [switch]$NoBuild,[switch]$BuildOnly,[switch]$List,
    [string]$ReportPath='',
    [string[]]$SuiteId=@(),
    [string]$BaseCommit=''
)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/test-common.ps1"
. "$PSScriptRoot/test-registry.ps1"
. "$PSScriptRoot/test-support/build-receipt.ps1"
Assert-DtmApiFullTestEnvironment
if($NoBuild -and $BuildOnly) { throw '-NoBuild and -BuildOnly cannot be combined.' }
$repo=Get-RepoRoot
if($List) {
    Get-DtmApiPublicTestSelection -Registry (Read-DtmApiTestRegistry -RepoRoot $repo) -Profile $Profile | Select-Object id,runner,project,requires
    exit 0
}
$source=Get-DtmApiPublicSourceState -RepoRoot $repo
$endSource=$null
$inputIdentity=$null; $outputIdentity=$null; $endInput=$null; $endOutput=$null
$dotnet=''; $identityUnchanged=$true; $buildSourceCommit=$null
$results=New-Object 'System.Collections.Generic.List[object]'
$failures=New-Object 'System.Collections.Generic.List[string]'
$previousTestRoot=$env:DTMAPI_TEST_TEMP_ROOT
$previousAuthorDotnet=$env:DTMAPI_AUTHOR_DOTNET
try {
    $registry=Read-DtmApiTestRegistry -RepoRoot $repo
    $selection=@(Get-DtmApiPublicTestSelection -Registry $registry -Profile $Profile)
    if($SuiteId.Count) {
        $unknown=@($SuiteId | Where-Object {$_ -notin $selection.id})
        if($unknown.Count){throw "Unknown public suites: $($unknown -join ', ')"}
        $selection=@($selection | Where-Object {$_.id -in $SuiteId})
    }
    $windowsHost=[Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
    if($Profile -eq 'PublicWindows' -and -not $windowsHost) { throw 'PublicWindows requires a Windows host; use -Profile Generic for the declared Linux source scope.' }
    $dotnet=Get-DotNetExe -RepoRoot $repo -NoProvision:$NoBuild
    if($BaseCommit) {
        $changed=@(& git -C $repo -c core.quotePath=false diff --name-only $BaseCommit HEAD)
        if($LASTEXITCODE -ne 0){throw 'Cannot resolve affected source diff.'}
        $selection=@(Select-DtmApiAffectedPublicTests -RepoRoot $repo -Selection $selection -ChangedFiles $changed -Configuration $Configuration -DotNetExe $dotnet)
    }
    $requirements=@($selection | ForEach-Object {$_.requires} | Sort-Object -Unique)
    if($requirements -contains 'PowerShell51') {
        $powershell=Get-DtmApiPowerShellHost -RequireWindowsPowerShell
        if(-not $powershell) { throw 'Selected scripts require Windows PowerShell 5.1.' }
    }
    $projects=@($selection | ForEach-Object {$_.project} | Where-Object {$_} | Sort-Object -Unique)
    $identityArgs=@{RepoRoot=$repo;Profile=$Profile;Configuration=$Configuration;Projects=$projects;DotNetExe=$dotnet;Selection=$selection}
    if(-not $NoBuild -and $selection.Count -gt 0) {
        $dependencies=@($selection | ForEach-Object {$_.dependencies} | Where-Object {$_} | Sort-Object -Unique)
        if($dependencies.Count -gt 0) {
            & "$PSScriptRoot/prepare-unit-test-dependencies.ps1" -Dependency $dependencies
            if($LASTEXITCODE -ne 0) { throw 'Public test fixture preparation failed.' }
        }
        if($projects.Count) { Invoke-DtmApiProjectBuild -RepoRoot $repo -DotNetExe $dotnet -Projects $projects -Configuration $Configuration -RestoreOnly }
    }
    $receiptScope=''
    if($SuiteId.Count -or $BaseCommit) {
        # File naming only: separate selected builds so one suite cannot replace
        # another suite's reusable receipt.
        $scopeHash=[Security.Cryptography.SHA256]::Create()
        try { $receiptScope='-'+([BitConverter]::ToString($scopeHash.ComputeHash([Text.Encoding]::UTF8.GetBytes((@($selection | ForEach-Object {$_.id} | Sort-Object) -join '|'))))).Replace('-','').Substring(0,16).ToLowerInvariant() }
        finally { $scopeHash.Dispose() }
    }
    $buildReceiptPath=Join-Path $repo "tmp/public-build-$Profile-$Configuration$receiptScope.json"
    $inputIdentity=Get-DtmApiPublicBuildIdentity @identityArgs
    if($NoBuild) {
        if(-not (Test-Path -LiteralPath $buildReceiptPath -PathType Leaf)){throw 'NoBuild requires a matching prior Public source build receipt.'}
        $buildReceipt=Get-Content -LiteralPath $buildReceiptPath -Raw | ConvertFrom-Json
        $buildSourceCommit=$buildReceipt.SourceCommit
        $outputIdentity=Get-DtmApiPublicBuildIdentity @identityArgs -Outputs
        if($buildReceipt.Input -cne $inputIdentity -or $buildReceipt.Output -cne $outputIdentity){throw 'NoBuild selected inputs or outputs changed; rebuild the selected suites.'}
    } else {
        if($projects.Count) { Invoke-DtmApiProjectBuild -RepoRoot $repo -DotNetExe $dotnet -Projects $projects -Configuration $Configuration }
        if((Get-DtmApiPublicBuildIdentity @identityArgs) -cne $inputIdentity){throw 'Source inputs changed during build.'}
        $outputIdentity=Get-DtmApiPublicBuildIdentity @identityArgs -Outputs
        $buildSourceCommit=$source.Commit
        New-Item -ItemType Directory -Path (Split-Path $buildReceiptPath) -Force | Out-Null
        [IO.File]::WriteAllText($buildReceiptPath,(@{SourceCommit=$source.Commit;Input=$inputIdentity;Output=$outputIdentity}|ConvertTo-Json),(New-Object Text.UTF8Encoding($false)))
    }
    $env:DTMAPI_TEST_TEMP_ROOT=Join-Path $repo 'tmp/test-runs'
    $env:DTMAPI_AUTHOR_DOTNET=$dotnet
    foreach($suite in $selection) {
        $watch=[Diagnostics.Stopwatch]::StartNew(); $status='failed'
        try {
            if($BuildOnly) { $status='built'; continue }
            Write-Host "Running $($suite.id) [$($suite.runner)]"
            switch($suite.runner) {
                'unit-exe' {
                    & "$PSScriptRoot/test-unit.ps1" -Configuration $Configuration -Focus $suite.id -NoBuild
                    if($LASTEXITCODE -ne 0) { throw "Unit suite failed: $($suite.id)" }
                }
                'existing-exe-focus' {
                    $assembly=[IO.Path]::GetFileNameWithoutExtension($suite.project)
                    $binary=Join-Path (Split-Path (Join-Path $repo $suite.project)) "bin/$Configuration/net8.0/$assembly.dll"
                    if(-not (Test-Path -LiteralPath $binary -PathType Leaf)) { throw "Selected test binary missing: $binary" }
                    & $dotnet $binary @($suite.arguments)
                    if($LASTEXITCODE -ne 0) { throw "Executable test failed: $($suite.id)" }
                }
                'powershell' {
                    & $powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $repo $suite.entry) @($suite.arguments)
                    if($LASTEXITCODE -ne 0) { throw "Script test failed: $($suite.id)" }
                }
                'build-only' {
                    $project=Join-Path $repo $suite.project
                    [xml]$xml=Get-Content -LiteralPath $project
                    $framework=[string](@($xml.Project.PropertyGroup | Where-Object {$_.PSObject.Properties['TargetFramework'] -and $_.TargetFramework})[0].TargetFramework)
                    $assembly=[IO.Path]::GetFileNameWithoutExtension($project)
                    $declaredName=@($xml.Project.PropertyGroup | Where-Object {$_.PSObject.Properties['AssemblyName'] -and $_.AssemblyName})
                    if($declaredName.Count -gt 0) { $assembly=[string]$declaredName[0].AssemblyName }
                    $binary=Join-Path (Split-Path $project) "bin/$Configuration/$framework/$assembly.dll"
                    if(-not (Test-Path -LiteralPath $binary -PathType Leaf)) { throw "Build-only root output missing: $binary" }
                }
                default { throw "Unknown selected runner: $($suite.runner)" }
            }
            $status=if($suite.runner -eq 'build-only'){'built'}else{'passed'}
        }
        finally { $results.Add([pscustomobject]@{Id=$suite.id;Runner=$suite.runner;Status=$status;Seconds=$watch.Elapsed.TotalSeconds}) }
    }
} catch { $failures.Add($_.Exception.Message) }
finally {
    $env:DTMAPI_TEST_TEMP_ROOT=$previousTestRoot
    $env:DTMAPI_AUTHOR_DOTNET=$previousAuthorDotnet
    try {
        $endSource=Get-DtmApiPublicSourceState -RepoRoot $repo
        if($inputIdentity) {
            $endInput=Get-DtmApiPublicBuildIdentity @identityArgs
            if($endInput -cne $inputIdentity){throw 'Build inputs changed during validation.'}
        }
        if($outputIdentity) {
            $endOutput=Get-DtmApiPublicBuildIdentity @identityArgs -Outputs
            if($endOutput -cne $outputIdentity){throw 'Build outputs changed during validation.'}
        }
    } catch {
        $identityUnchanged=$false
        $failures.Add($_.Exception.Message)
        foreach($row in $results) {
            $row | Add-Member -NotePropertyName ExecutionStatus -NotePropertyValue $row.Status
            $row.Status='invalidated'
        }
    }
    $overall=if($failures.Count -gt 0){'failed'}elseif($BuildOnly){'built'}else{'passed'}
    if($ReportPath) {
        $report=[ordered]@{SchemaVersion=3;Status=$overall;SourceCommit=$source.Commit;SourceTree=$source.Tree;BuildSourceCommit=$buildSourceCommit;Dirty=$source.Dirty;EndSource=$endSource;IdentityUnchanged=$identityUnchanged;BuildInput=$inputIdentity;EndBuildInput=$endInput;BuildOutput=$outputIdentity;EndBuildOutput=$endOutput;Profile=$Profile;Configuration=$Configuration;Host=[Environment]::OSVersion.ToString();DotNet=$dotnet;Failures=$failures.ToArray();Results=$results.ToArray()}
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($ReportPath))) | Out-Null
        [IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath),($report | ConvertTo-Json -Depth 10),(New-Object Text.UTF8Encoding($false)))
    }
}
if($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
if($BuildOnly) { Write-Host "PASS: $Profile $Configuration build of $($projects.Count) declared roots; tests not run." }
else { Write-Host "PASS: $Profile $Configuration; $($results.Count) declared test/build entries completed." }
