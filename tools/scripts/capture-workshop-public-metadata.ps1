[CmdletBinding()]
param(
    [string] $OutputPath = 'tools/release/baselines/workshop-public-metadata-20260728.json'
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

$appId = '2285550'
$metadataEndpoint = 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/'
$browseEndpoint = 'https://steamcommunity.com/workshop/browse/'
$queries = @(
    'DTMAPI',
    'DTM API',
    'Doloc Town Modding API',
    'DolocTown SMAPI'
)
$preservedCompatibilityIds = @(
    '3726044511',
    '3742618545',
    '3742771572'
)
$newClassifications = @{
    '3772057085' = @{
        classification = 'SearchFalsePositiveExternalBepInExPluginExplicitNoDtmapiUse'
        catalogId = $null
        knownUniqueId = $null
    }
}

function Get-WorkshopQueryIds {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Query
    )

    $queryText = [uri]::EscapeDataString($Query)
    $seen = @{}
    $maximumPages = 100
    for ($page = 1; $page -le $maximumPages; $page++) {
        $uri = "$browseEndpoint`?appid=$appId&searchtext=$queryText&browsesort=textsearch&section=readytouseitems&actualsort=textsearch&p=$page&numperpage=30"
        $response = Invoke-WebRequest -UseBasicParsing -Uri $uri
        $matches = [regex]::Matches(
            [string]$response.Content,
            'sharedfiles/filedetails/\?id=(?<id>\d+)',
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $pageIds = @(
            $matches |
                ForEach-Object { $_.Groups['id'].Value } |
                Sort-Object -Unique
        )
        if ($pageIds.Count -eq 0) {
            return [pscustomobject][ordered]@{
                publishedFileIds = @($seen.Keys | Sort-Object)
                pagesFetched = $page
                terminalReason = 'EmptyPage'
            }
        }

        $newIdCount = 0
        foreach ($publishedFileId in $pageIds) {
            if (-not $seen.ContainsKey($publishedFileId)) {
                $seen[$publishedFileId] = $true
                $newIdCount++
            }
        }
        if ($newIdCount -eq 0) {
            return [pscustomobject][ordered]@{
                publishedFileIds = @($seen.Keys | Sort-Object)
                pagesFetched = $page
                terminalReason = 'NoNewIds'
            }
        }
    }

    throw "Workshop query '$Query' did not reach an empty or stable page within $maximumPages pages."
}

function Convert-UnixTimeToUtcText {
    param([long] $Value)
    return [DateTimeOffset]::FromUnixTimeSeconds($Value).UtcDateTime.ToString(
        'yyyy-MM-ddTHH:mm:ssZ',
        [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-NullableText {
    param($Value)
    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        return $null
    }
    return [string]$Value
}

function Get-OptionalPropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        $InputObject,
        [Parameter(Mandatory = $true)]
        [string] $Name
    )
    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }
    return $property.Value
}

$repo = Get-RepoRoot
$oldSnapshotPath = Join-Path $repo 'tools\release\baselines\workshop-public-metadata-20260713.json'
$oldSnapshot = Get-Content -LiteralPath $oldSnapshotPath -Raw | ConvertFrom-Json
$oldItems = @{}
foreach ($item in @($oldSnapshot.items)) {
    $oldItems[[string]$item.publishedFileId] = $item
}

$searchCapturedAtUtc = [DateTime]::UtcNow
$queryResults = [ordered]@{}
foreach ($query in $queries) {
    $queryResults[$query] = Get-WorkshopQueryIds -Query $query
}

$selectedIds = @(
    @($queryResults['DTMAPI'].publishedFileIds) +
    $preservedCompatibilityIds |
        Sort-Object -Unique
)
if ($selectedIds.Count -ne 22) {
    throw "Expected 22 authoritative cutoff items, found $($selectedIds.Count): $($selectedIds -join ',')"
}

$body = @{
    itemcount = $selectedIds.Count
}
for ($index = 0; $index -lt $selectedIds.Count; $index++) {
    $body["publishedfileids[$index]"] = $selectedIds[$index]
}
$metadataResponse = Invoke-RestMethod -Method Post -Uri $metadataEndpoint -Body $body
$metadataCapturedAtUtc = [DateTime]::UtcNow
$details = @($metadataResponse.response.publishedfiledetails)
if ($details.Count -ne $selectedIds.Count) {
    throw "Expected $($selectedIds.Count) metadata rows, found $($details.Count)."
}

$items = New-Object 'System.Collections.Generic.List[object]'
foreach ($detail in @($details | Sort-Object { [string]$_.publishedfileid })) {
    $publishedFileId = [string]$detail.publishedfileid
    $classification = $null
    if ($oldItems.ContainsKey($publishedFileId)) {
        $classification = @{
            classification = [string]$oldItems[$publishedFileId].classification
            catalogId = Get-NullableText (Get-OptionalPropertyValue $oldItems[$publishedFileId] 'catalogId')
            knownUniqueId = Get-NullableText (Get-OptionalPropertyValue $oldItems[$publishedFileId] 'knownUniqueId')
        }
    }
    elseif ($newClassifications.ContainsKey($publishedFileId)) {
        $classification = $newClassifications[$publishedFileId]
    }
    else {
        throw "No reviewed classification exists for Workshop item $publishedFileId."
    }

    if ([int]$detail.result -eq 1) {
        $row = [ordered]@{
            publishedFileId = $publishedFileId
            appId = [string]$detail.consumer_app_id
            result = [int]$detail.result
            visibility = [int]$detail.visibility
            title = [string]$detail.title
            creatorId = [string]$detail.creator
            timeCreatedUtc = Convert-UnixTimeToUtcText ([long]$detail.time_created)
            timeUpdatedUtc = Convert-UnixTimeToUtcText ([long]$detail.time_updated)
            fileSize = [long]$detail.file_size
            tags = @($detail.tags | ForEach-Object { [string]$_.tag })
            classification = [string]$classification.classification
            catalogId = $classification.catalogId
            knownUniqueId = $classification.knownUniqueId
        }
    }
    elseif ($oldItems.ContainsKey($publishedFileId)) {
        $oldItem = $oldItems[$publishedFileId]
        $row = [ordered]@{
            publishedFileId = $publishedFileId
            appId = [string]$oldItem.appId
            result = [int]$detail.result
            visibility = [int]$oldItem.visibility
            title = [string]$oldItem.title
            creatorId = [string]$oldItem.creatorId
            timeCreatedUtc = [string]$oldItem.timeCreatedUtc
            timeUpdatedUtc = [string]$oldItem.timeUpdatedUtc
            fileSize = [long]$oldItem.fileSize
            tags = @($oldItem.tags)
            classification = [string]$classification.classification
            catalogId = $classification.catalogId
            knownUniqueId = $classification.knownUniqueId
            metadataState = "CurrentResult$([int]$detail.result)WithLastKnownDetailsFrom20260713"
        }
    }
    else {
        throw "Workshop item $publishedFileId returned result $([int]$detail.result) with no reviewed last-known metadata."
    }
    $items.Add([pscustomobject]$row) | Out-Null
}

$digestRows = @($items | ForEach-Object {
    @(
        [string]$_.publishedFileId,
        [string]$_.appId,
        [string]$_.result,
        [string]$_.visibility,
        [string]$_.title,
        [string]$_.creatorId,
        [string]$_.timeCreatedUtc,
        [string]$_.timeUpdatedUtc,
        [string]$_.fileSize,
        (@($_.tags) -join ','),
        [string]$_.classification,
        [string]$_.catalogId,
        [string]$_.knownUniqueId
    ) -join '|'
})
$normalized = $digestRows -join "`n"
$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $digest = [System.BitConverter]::ToString(
        $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($normalized))).Replace('-', '').ToLowerInvariant()
}
finally {
    $sha256.Dispose()
}

$browseQueries = @($queries | ForEach-Object {
    $queryResult = $queryResults[$_]
    [ordered]@{
        text = $_
        resultCount = @($queryResult.publishedFileIds).Count
        pagesFetched = [int]$queryResult.pagesFetched
        terminalReason = [string]$queryResult.terminalReason
        publishedFileIds = @($queryResult.publishedFileIds)
    }
})
$broadUnion = @(
    $queryResults.Values |
        ForEach-Object { $_.publishedFileIds } |
        Sort-Object -Unique
)
$snapshot = [ordered]@{
    schemaVersion = 2
    snapshotId = 'workshop-public-metadata-20260728'
    steamAppId = $appId
    metadataCapturedAtUtc = $metadataCapturedAtUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
    searchCapturedAtUtc = $searchCapturedAtUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
    metadataDigest = [ordered]@{
        normalization = 'Items sorted by PublishedFileID. Format CreatedUTC and UpdatedUTC as yyyy-MM-ddTHH:mm:ssZ. Join PublishedFileID|AppID|Result|Visibility|Title|CreatorID|CreatedUTC|UpdatedUTC|FileSize|comma-joined Tags|Classification|CatalogID-or-empty|KnownUniqueID-or-empty with LF; UTF-8; SHA-256.'
        rowCount = $items.Count
        sha256 = $digest
    }
    sources = [ordered]@{
        metadataEndpoint = $metadataEndpoint
        metadataMethod = 'POST'
        browseEndpoint = "https://steamcommunity.com/workshop/browse/?appid=$appId"
        browseQueries = $browseQueries
        broadQueryUnionCount = $broadUnion.Count
        authoritativeSelection = 'Exact DTMAPI query union preserved compatibility IDs 3726044511, 3742618545 and 3742771572; broad query counts are discovery-only because Steam text relevance currently returns unrelated Mods.'
        queryUnionCount = $items.Count
    }
    verificationBoundary = [ordered]@{
        publicMetadata = 'Verified for the captured response: result, app id, visibility, title, creator id, timestamps, file size, and tags.'
        semanticVersion = 'The public API response does not expose each DTMAPI manifest version. First-party semantic versions come only from the retained read-only subscription cutoff.'
        accountControl = 'PendingAccountControlVerification'
        creatorIdInference = 'All Runtime and eleven first-party items share creator 76561198946111933. This is public metadata evidence, not proof that the currently authenticated account controls the items.'
        releaseCutoff = 'This is the 0.5.5 RC public-metadata cutoff. Exact downloadable consumer bytes remain governed by the retained read-only subscription inventory and ABI gate.'
    }
    firstPartyPublicCreatorId = '76561198946111933'
    activeDtmapiPublicItemCount = 12
    sameCreatorQueryResultCount = @($items | Where-Object { $_.creatorId -eq '76561198946111933' }).Count
    firstPartyPublishedProductCount = 11
    items = $items.ToArray()
}

$resolvedOutputPath = if ([IO.Path]::IsPathRooted($OutputPath)) {
    [IO.Path]::GetFullPath($OutputPath)
}
else {
    [IO.Path]::GetFullPath((Join-Path $repo $OutputPath))
}
$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}
$snapshot | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8
Write-Output "Workshop snapshot captured: $resolvedOutputPath"
Write-Output "AuthoritativeItems=$($items.Count)"
Write-Output "BroadQueryUnion=$($broadUnion.Count)"
Write-Output "Digest=$digest"
