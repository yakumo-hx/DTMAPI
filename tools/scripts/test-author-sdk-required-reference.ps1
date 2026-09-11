[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $TestRoot = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$dotnet = Get-DotNetExe -RepoRoot $repo
$managedRoot = if (-not [string]::IsNullOrWhiteSpace($TestRoot)) {
    [System.IO.Path]::GetFullPath($TestRoot)
}
elseif (-not [string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repo 'tmp\test-runs'))
}
$fixtureRoot = Join-Path $managedRoot ('author-sdk-required-reference-' + [Guid]::NewGuid().ToString('N'))
$missingReferenceRoot = Join-Path $fixtureRoot 'missing-reference'
$previousReferenceRoot = $env:DTMAPI_AUTHOR_ADVANCED_REFERENCE_ROOT
$previousTestRoot = $env:DTMAPI_TEST_TEMP_ROOT
$previousErrorAction = $ErrorActionPreference

try {
    [System.IO.Directory]::CreateDirectory($missingReferenceRoot) | Out-Null
    $env:DTMAPI_AUTHOR_ADVANCED_REFERENCE_ROOT = $missingReferenceRoot
    $env:DTMAPI_TEST_TEMP_ROOT = $fixtureRoot
    $ErrorActionPreference = 'Continue'
    $output = & $dotnet run `
        --project (Join-Path $repo 'tests\DTMAPI.AuthorSdk.Tests\DTMAPI.AuthorSdk.Tests.csproj') `
        -c $Configuration `
        --no-build `
        -- `
        --focus advanced-policy --require-exact-advanced-reference 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($exitCode -eq 0) {
        throw 'Author SDK missing exact-reference negative unexpectedly passed.'
    }
    if ($output.IndexOf('DTMAPI Author SDK tests: OK', [StringComparison]::Ordinal) -ge 0) {
        throw 'Author SDK missing exact-reference negative printed the final OK marker.'
    }
    if ($output.IndexOf('configured 23762374 Assembly-CSharp is missing', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Author SDK missing exact-reference negative lacked the exact diagnostic: $output"
    }
    Write-Host "Author SDK required exact-reference negative: PASS exit=$exitCode"
}
finally {
    $ErrorActionPreference = $previousErrorAction
    $env:DTMAPI_AUTHOR_ADVANCED_REFERENCE_ROOT = $previousReferenceRoot
    $env:DTMAPI_TEST_TEMP_ROOT = $previousTestRoot
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
