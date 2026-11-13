. "$PSScriptRoot/project-build.ps1"

function Assert-DtmApiFullTestEnvironment {
    $processEnvironment = [Environment]::GetEnvironmentVariables('Process')
    $filters = @($processEnvironment.Keys | Where-Object {
        $_ -like 'DTMAPI_*_TEST_FOCUS' -and
        -not [string]::IsNullOrEmpty([string]$processEnvironment[$_])
    } | Sort-Object)
    if ($filters.Count -gt 0) {
        throw "This explicitly broad source-test entry does not accept focus filters: $($filters -join ', '). Use the selected test entry with its explicit focus."
    }
}
