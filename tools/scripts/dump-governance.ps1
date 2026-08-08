function Get-DtmApiFileSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $getFileHashCommand = Get-Command -Name Get-FileHash -ErrorAction SilentlyContinue
    if ($getFileHashCommand) {
        try {
            return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        }
        catch {
        }
    }

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $hashBytes = $sha256.ComputeHash($stream)
            return ([System.BitConverter]::ToString($hashBytes)).Replace('-', '')
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}
function Copy-DtmApiVerifiedFile {
    param(
        [Parameter(Mandatory = $true)] [string] $SourcePath,
        [Parameter(Mandatory = $true)] [string] $DestinationPath
    )

    if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
        return $null
    }

    $sourceItem = Get-Item -LiteralPath $SourcePath
    if ($sourceItem.Length -le 0) {
        return $null
    }

    $sourceHash = Get-DtmApiFileSha256 -Path $SourcePath
    Copy-Item -Force -LiteralPath $SourcePath -Destination $DestinationPath
    $destinationItem = Get-Item -LiteralPath $DestinationPath
    if ($destinationItem.Length -ne $sourceItem.Length) {
        throw "Dump handoff length mismatch: source=$($sourceItem.Length), destination=$($destinationItem.Length)."
    }
    $destinationHash = Get-DtmApiFileSha256 -Path $DestinationPath
    if (-not [string]::Equals($sourceHash, $destinationHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Dump handoff SHA-256 mismatch: source=$sourceHash, destination=$destinationHash."
    }

    return [pscustomobject]@{
        Item = $destinationItem
        Length = [long]$destinationItem.Length
        Sha256 = $destinationHash
        Verified = $true
    }
}
