Set-StrictMode -Version Latest

function Get-DtmApiSteamManifestSectionValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ManifestText,
        [Parameter(Mandatory = $true)]
        [string] $SectionName,
        [Parameter(Mandatory = $true)]
        [string] $ValueName
    )

    $escapedSectionName = [System.Text.RegularExpressions.Regex]::Escape($SectionName)
    $sectionPattern = '(?ms)^\s*"' + $escapedSectionName + '"\s*\r?\n\s*\{\s*(?<body>.*?)^\s*\}'
    $sectionMatches = [System.Text.RegularExpressions.Regex]::Matches($ManifestText, $sectionPattern)
    if ($sectionMatches.Count -ne 1) {
        throw "Steam manifest must contain exactly one '$SectionName' section; found $($sectionMatches.Count)."
    }

    $escapedValueName = [System.Text.RegularExpressions.Regex]::Escape($ValueName)
    $valuePattern = '(?m)^\s*"' + $escapedValueName + '"\s+"(?<value>[^"]*)"\s*$'
    $valueMatches = [System.Text.RegularExpressions.Regex]::Matches(
        $sectionMatches[0].Groups['body'].Value,
        $valuePattern
    )
    if ($valueMatches.Count -ne 1) {
        throw "Steam manifest section '$SectionName' must contain exactly one '$ValueName' value; found $($valueMatches.Count)."
    }

    return $valueMatches[0].Groups['value'].Value
}

function Get-DtmApiSteamBranchIdentity {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ManifestPath,
        [string] $ExplicitBranch,
        [switch] $AllowUnknownSteamBranch
    )

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "Steam manifest is missing: $ManifestPath"
    }

    $manifestText = [System.IO.File]::ReadAllText($ManifestPath)
    $userBranch = Get-DtmApiSteamManifestSectionValue `
        -ManifestText $manifestText `
        -SectionName 'UserConfig' `
        -ValueName 'BetaKey'
    $mountedBranch = Get-DtmApiSteamManifestSectionValue `
        -ManifestText $manifestText `
        -SectionName 'MountedConfig' `
        -ValueName 'BetaKey'

    $safeBranchPattern = '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$'
    foreach ($candidate in @($userBranch, $mountedBranch)) {
        if ($candidate -notmatch $safeBranchPattern) {
            throw "Steam manifest BetaKey is missing or unsafe: '$candidate'."
        }
    }

    if (-not $userBranch.Equals($mountedBranch, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Steam manifest branch conflict: UserConfig='$userBranch', MountedConfig='$mountedBranch'."
    }

    $detectedBranch = $userBranch.ToLowerInvariant()
    $knownBranches = @('public', 'workshop', 'test')
    $isKnownBranch = $knownBranches -contains $detectedBranch

    if (-not [string]::IsNullOrWhiteSpace($ExplicitBranch)) {
        if ($ExplicitBranch -notmatch $safeBranchPattern) {
            throw "Explicit Steam branch is unsafe: '$ExplicitBranch'."
        }

        if (-not $ExplicitBranch.Equals($detectedBranch, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Explicit Steam branch '$ExplicitBranch' does not match manifest branch '$detectedBranch'."
        }
    }

    if (-not $isKnownBranch) {
        if (-not $AllowUnknownSteamBranch) {
            throw "Steam branch '$detectedBranch' is not in the reviewed allowlist (public, workshop, test)."
        }
        if ([string]::IsNullOrWhiteSpace($ExplicitBranch)) {
            throw "Unknown Steam branch '$detectedBranch' requires both -AllowUnknownSteamBranch and an explicit matching -Branch."
        }
    }

    return [pscustomobject]@{
        DetectedBranch = $detectedBranch
        ResolvedBranch = $detectedBranch
        Source = 'appmanifest.UserConfig+MountedConfig'
        IsKnownBranch = $isKnownBranch
    }
}

function Get-DtmApiSteamBuildIdentity {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ManifestPath,
        [string] $ExplicitBranch,
        [switch] $AllowUnknownSteamBranch
    )

    $branchParameters = @{
        ManifestPath = $ManifestPath
        ExplicitBranch = $ExplicitBranch
    }
    if ($AllowUnknownSteamBranch) {
        $branchParameters['AllowUnknownSteamBranch'] = $true
    }
    $branchIdentity = Get-DtmApiSteamBranchIdentity @branchParameters

    $manifestText = [System.IO.File]::ReadAllText($ManifestPath)
    $buildMatch = [System.Text.RegularExpressions.Regex]::Match($manifestText, '"buildid"\s+"(?<id>\d+)"')
    if (-not $buildMatch.Success) {
        throw "Steam buildid was not found in $ManifestPath"
    }

    return [pscustomobject]@{
        BuildId = $buildMatch.Groups['id'].Value
        Branch = $branchIdentity.ResolvedBranch
        BranchSource = $branchIdentity.Source
        IsKnownBranch = [bool]$branchIdentity.IsKnownBranch
        ManifestSha256 = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToUpperInvariant()
    }
}

function Assert-DtmApiSteamBuildIdentityMatch {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Expected,
        [Parameter(Mandatory = $true)]
        [object] $Actual,
        [string] $ExpectedLabel = 'expected',
        [string] $ActualLabel = 'actual'
    )

    $mismatches = [System.Collections.Generic.List[string]]::new()
    if (-not ([string]$Expected.BuildId).Equals([string]$Actual.BuildId, [System.StringComparison]::Ordinal)) {
        $mismatches.Add("buildid $ExpectedLabel=$($Expected.BuildId) $ActualLabel=$($Actual.BuildId)")
    }
    if (-not ([string]$Expected.Branch).Equals([string]$Actual.Branch, [System.StringComparison]::OrdinalIgnoreCase)) {
        $mismatches.Add("branch $ExpectedLabel=$($Expected.Branch) $ActualLabel=$($Actual.Branch)")
    }
    if (-not ([string]$Expected.ManifestSha256).Equals([string]$Actual.ManifestSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        $mismatches.Add("manifestSha256 $ExpectedLabel=$($Expected.ManifestSha256) $ActualLabel=$($Actual.ManifestSha256)")
    }

    if ($mismatches.Count -gt 0) {
        throw "Steam manifest identity changed or does not match the frozen copy: $($mismatches -join '; ')."
    }
}
