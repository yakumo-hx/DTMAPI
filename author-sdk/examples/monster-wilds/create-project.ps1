[CmdletBinding()]
param([string]$Version='0.1.3',[string]$OutputPath='')
$ErrorActionPreference='Stop'
if(-not $OutputPath){$OutputPath=Join-Path $PSScriptRoot 'Wilds.dtmonster.json'}
$project=[ordered]@{
    format='dtmapi.monster-project';version=4
    pack=@{id='Samples.MonsterWilds';name='异兽集';author='DTMAPI';version=$Version}
    monsters=@(@{
        id='mechanical-amoeba';templateId='amoeba'
        overrides=@{health=80;defense=2;tint='#A0B6C8';display=@{name='废城机械变形虫';nameplate=$true;healthBar='Damaged'}}
        behavior=@{mode='inherit-native'}
        drops=@{mode='replace';source='local';poolId='mechanical-materials';minCount=2;maxCount=2}
    })
    nativeOverrides=@()
    dropPools=@(@{id='mechanical-materials';entries=@(
        @{itemId='slime_gel';weight=1;minCount=1;maxCount=1},
        @{itemId='combat_chip';weight=1;minCount=1;maxCount=1}
    )})
    spawnRules=@(0..2|ForEach-Object{@{id="lowland-$_";definitionId='mechanical-amoeba';roomId="dungeon_旧城市废墟.郊野低地-$_";regionId='';count=1}})
    assets=@();dependencies=@();catalog=$null
}
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath),($project|ConvertTo-Json -Depth 25),[Text.UTF8Encoding]::new($false))
Write-Output "Author project: $OutputPath"
