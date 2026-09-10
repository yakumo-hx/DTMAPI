# Location adapter for archived records. IDs and historical release strings stay unchanged.
$script:DtmApiDocumentLocations = $null
$script:DtmApiDocumentPathMap = @{}
$script:DtmApiDocumentRepo = ''

function Get-DtmApiDocumentManifest {
    param([string] $RepoRoot)
    $canonicalRoot = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/')
    if ($null -eq $script:DtmApiDocumentLocations -or $script:DtmApiDocumentRepo -ine $canonicalRoot) {
        $script:DtmApiDocumentRepo = $canonicalRoot
        $script:DtmApiDocumentPathMap = @{}
        $path = Join-Path $RepoRoot 'docs/archive/migrations/20260908-workspace.json'
        $script:DtmApiDocumentLocations = if (Test-Path -LiteralPath $path -PathType Leaf) {
            Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
        } else { [pscustomobject]@{ files = @() } }
        foreach ($row in $script:DtmApiDocumentLocations.files) {
            if ($script:DtmApiDocumentPathMap.ContainsKey([string]$row.source)) { throw "Duplicate historical document identity: $($row.source)" }
            $script:DtmApiDocumentPathMap[[string]$row.source] = [string]$row.current
        }
    }
    return $script:DtmApiDocumentLocations
}

function Resolve-DtmApiDocumentPath {
    param([string] $RepoRoot, [string] $RelativePath)
    $null = Get-DtmApiDocumentManifest $RepoRoot
    $key = $RelativePath.Replace('\', '/')
    $relative = if ($script:DtmApiDocumentPathMap.ContainsKey($key)) { $script:DtmApiDocumentPathMap[$key] } else { $RelativePath }
    if ([System.IO.Path]::IsPathRooted($relative)) { throw 'Document location must be repository-relative.' }
    $root = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/')
    $path = [System.IO.Path]::GetFullPath((Join-Path $root $relative))
    if (-not $path.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Document location escapes repository: $relative"
    }
    $node = $path
    while ($node.Length -gt $root.Length) {
        if (Test-Path -LiteralPath $node) {
            if (((Get-Item -LiteralPath $node -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Document location contains a reparse point: $node"
            }
        }
        $node = [System.IO.Path]::GetDirectoryName($node)
    }
    return $path
}

function Get-DtmApiUpdateRecordFiles {
    param([string] $RepoRoot)
    $locations = @{}
    foreach ($row in (Get-DtmApiDocumentManifest $RepoRoot).files) { $locations[[string]$row.source] = [string]$row.current }
    foreach ($relativeRoot in @('docs/updates', 'docs/archive/updates')) {
        $root = Join-Path $RepoRoot $relativeRoot
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.md') {
            if ($file.Name -notmatch '^\d{8}-\d{4}-.+\.md$') { continue }
            $relative = $file.FullName.Substring($RepoRoot.TrimEnd('\', '/').Length + 1).Replace('\', '/')
            if ($locations.ContainsKey($relative) -and $locations[$relative] -cne $relative) { continue }
            $file
        }
    }
}
