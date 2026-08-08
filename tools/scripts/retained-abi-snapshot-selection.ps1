function Test-ExactRetainedWorkshopSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $PayloadRoot,
        [Parameter(Mandatory = $true)] [object[]] $ExpectedFiles
    )

    foreach ($expected in $ExpectedFiles) {
        $itemRoot = Join-Path $PayloadRoot ([string]$expected.WorkshopId)
        if (-not (Test-Path -LiteralPath $itemRoot -PathType Container)) {
            return $false
        }
        $matches = @(Get-ChildItem -LiteralPath $itemRoot -Recurse -File -Filter ([string]$expected.FileName) -ErrorAction Stop)
        if ($matches.Count -ne 1) {
            return $false
        }
        $actualSha256 = (Get-FileHash -LiteralPath $matches[0].FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actualSha256 -cne ([string]$expected.Sha256).ToUpperInvariant()) {
            return $false
        }
    }
    return $true
}

function Resolve-UniqueExactRetainedWorkshopSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string] $SubscriptionRoot,
        [Parameter(Mandatory = $true)] [string] $SteamAppId,
        [Parameter(Mandatory = $true)] [object[]] $ExpectedFiles
    )

    $snapshotCandidates = @(Get-ChildItem -LiteralPath $SubscriptionRoot -Directory -Force -ErrorAction Stop |
        Where-Object { $_.Name -like "workshop-$SteamAppId-*" } |
        Sort-Object Name)
    $exactSnapshotCandidates = @()
    foreach ($snapshot in $snapshotCandidates) {
        $payload = Join-Path $snapshot.FullName 'payload'
        if (-not (Test-Path -LiteralPath $payload -PathType Container)) {
            continue
        }
        if (Test-ExactRetainedWorkshopSnapshot -PayloadRoot $payload -ExpectedFiles $ExpectedFiles) {
            $exactSnapshotCandidates += [System.IO.Path]::GetFullPath($payload)
        }
    }
    if ($exactSnapshotCandidates.Count -eq 0) {
        throw "No exact retained Workshop subscription snapshot under '$SubscriptionRoot' matches all Catalog/binary-audit file identities and SHA-256 values."
    }
    if ($exactSnapshotCandidates.Count -ne 1) {
        throw "Multiple exact retained Workshop subscription snapshots match all Catalog/binary-audit identities; authority is ambiguous: $($exactSnapshotCandidates -join ', ')."
    }
    return $exactSnapshotCandidates[0]
}
