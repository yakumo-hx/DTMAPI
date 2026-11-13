[CmdletBinding()]
param([string]$GameDir = '', [string]$OutputRoot = '', [string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/windows-installer/common.ps1"
$moduleRepo = Get-RepoRoot
$moduleDotnet = Get-DotNetExe -RepoRoot $moduleRepo
if (-not $GameDir) { $GameDir = Resolve-DolocTownGamePath -RepoRoot $moduleRepo }
$moduleReferences = Join-Path $moduleRepo '.tools/runtime-native/references'
$moduleAssemblies = @('Assembly-CSharp','Assembly-CSharp-firstpass','AK.Wwise.Unity.API','Unity.RenderPipelines.Universal.Runtime',
    'UnityEngine.CoreModule','UnityEngine.Physics2DModule','UnityEngine.AnimationModule','UnityEngine.AssetBundleModule',
    'UnityEngine.JSONSerializeModule','UnityEngine.ImageConversionModule','UnityEngine.TextRenderingModule','UnityEngine.UIModule','UnityEngine.UI','DOTween')
$moduleInputs = @($moduleAssemblies | ForEach-Object { Join-Path $GameDir "DolocTown_Data/Managed/$_.dll" }) + @(Join-Path $GameDir 'BepInEx/core/0Harmony.dll')
& "$PSScriptRoot/build.ps1" -SkipTests -Projects src/DTMAPI.NativeReferences/DTMAPI.NativeReferences.csproj
& $moduleDotnet "$moduleRepo/src/DTMAPI.NativeReferences/bin/Release/net8.0/DTMAPI.NativeReferences.dll" $moduleReferences @moduleInputs
if ($LASTEXITCODE) { throw 'Runtime module native reference conversion failed.' }
$moduleProjects = @(Get-DtmApiRuntimeComponentFile | Where-Object { $_.AssemblyName -like 'DTMAPI.Modules.*' })
foreach ($module in $moduleProjects) {
    & $moduleDotnet build (Join-Path $moduleRepo $module.Project) -c $Configuration --nologo -m:1 -p:UseSharedCompilation=false "-p:DtmApiNativeReferences=$moduleReferences"
    if ($LASTEXITCODE) { throw "Runtime module build failed: $($module.AssemblyName)" }
}
if ($OutputRoot) {
    foreach ($row in @(Get-DtmApiRuntimeComponentFile | Where-Object { -not $_.Auxiliary })) {
        $moduleSource = Join-Path (Split-Path -Parent (Join-Path $moduleRepo $row.Project)) "bin/$Configuration/$($row.TargetFramework)/$($row.AssemblyName).dll"
        $moduleTarget = Join-Path $OutputRoot $row.relativePath
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $moduleTarget))
        Copy-Item -LiteralPath $moduleSource -Destination $moduleTarget -Force
    }
    & "$PSScriptRoot/prepare-audio-decoder.ps1" -OutputRoot (Join-Path $OutputRoot 'DTMAPI/components/audio/audio-decoder')
}

