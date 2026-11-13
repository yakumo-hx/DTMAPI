Set-StrictMode -Version 2.0

function Assert-DtmApiBuildPathsDisjoint {
    param([string] $OutputPath, [string[]] $InputPaths)
    $output = [IO.Path]::GetFullPath($OutputPath)
    foreach ($inputPath in $InputPaths) {
        if ([string]::IsNullOrWhiteSpace($inputPath)) { continue }
        $inputFull = [IO.Path]::GetFullPath($inputPath)
        if ((Test-DtmApiPathIsSameOrChild -Child $output -Parent $inputFull) -or
            (Test-DtmApiPathIsSameOrChild -Child $inputFull -Parent $output)) {
            throw "Build output overlaps an input; nothing was cleaned: output=$output input=$inputFull"
        }
    }
    $ancestor = $output
    while (-not [string]::IsNullOrWhiteSpace($ancestor)) {
        if ((Test-Path -LiteralPath $ancestor) -and
            ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Build output traverses a reparse point; nothing was cleaned: $ancestor"
        }
        $ancestor = Split-Path -Parent $ancestor
    }
}
