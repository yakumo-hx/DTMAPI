param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('BepInExFixture')]
    [string[]] $Dependency
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
foreach ($name in @($Dependency | Sort-Object -Unique)) {
    if ($name -eq 'BepInExFixture') {
        # Test fixtures consume the repository's redistributable bootstrap ZIP.
        # This does not install anything into a game or resolve a live game path.
        $archive = Join-Path $repo 'tools\release\bootstrap\BepInEx_win_x64_5.4.23.5.zip'
        if (-not (Test-Path -LiteralPath $archive -PathType Leaf)) { throw "Tracked Unit fixture input is missing: $archive" }
        $root = Join-Path $repo '.tools\bepinex\extract'
        $receipt = Join-Path $repo '.tools\bepinex\unit-fixture-source.sha256'
        $sourceHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
        $required = @('0Harmony.dll', 'HarmonyXInterop.dll', 'Mono.Cecil.dll', 'Mono.Cecil.Mdb.dll', 'Mono.Cecil.Pdb.dll', 'Mono.Cecil.Rocks.dll', 'MonoMod.RuntimeDetour.dll', 'MonoMod.Utils.dll')
        $missing = @($required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $root "BepInEx\core\$_") -PathType Leaf) })
        $oldHash = if (Test-Path -LiteralPath $receipt) { (Get-Content -LiteralPath $receipt -Raw).Trim() } else { '' }
        if ($missing.Count -gt 0 -or $oldHash -ne $sourceHash) {
            New-Item -ItemType Directory -Force -Path $root | Out-Null
            Expand-DtmApiZipArchive -LiteralPath $archive -DestinationPath $root | Out-Null
            $sourceHash | Set-Content -LiteralPath $receipt -Encoding ASCII
        }
        Write-Host "Unit fixture input ready: repository BepInEx bootstrap ($sourceHash)."
    }
}
exit 0
