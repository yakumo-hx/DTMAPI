# 畜牧

用内容包给游戏加新的动物。物种、成长、日程、产物、价格都写在官方的 `Content/animal_tbanimal.json` 里，跟官方内容一样；DTMAPI 只补游戏没有的部分：用 PNG 换动物的画面、加载自带的动画机、给动物配叫声。

畜牧 API 是 Experimental，以后的版本还会改。完整的 JSON、PNG、WAV 例子见[动物教程](content-packs/custom-animal-tutorial.html)。

## PNG 动物

官方动物表里，成年和幼体的 `levels[*].animator.url` 直接写原版动物的动画机地址，选一个跟你的动物行为接近的：

| 原版动物 | 成年动画机 | 幼体动画机 | 模板帧前缀 |
| --- | --- | --- | --- |
| `chicken` | `game_anim_animal_chicken` | `game_anim_animal_chicken_child` | `anim_animal_chicken` |
| `goat` | `game_anim_animal_goat` | `game_anim_animal_goat_child` | `anim_animal_goat` |
| `marsh_pangolin` | `game_anim_animal_marsh_pangolin` | `game_anim_animal_marsh_pangolin_child` | `anim_animal_marsh_pangolin` |
| `slime` | `game_anim_animal_slime` | `game_anim_animal_slime_child` | `anim_animal_slime` |

然后在 `PackData/content.json` 里写画面怎么换：

```json
{
  "schemaVersion": 1,
  "animals": {
    "definitions": [{
      "speciesId": "my_bird",
      "animatorMode": "pngSpriteOverride",
      "frameManifest": "Content/Sprites/my_bird_frame_manifest.json",
      "templateSpritePrefix": "anim_animal_chicken",
      "customSpritePrefix": "anim_animal_my_bird"
    }]
  }
}
```

| 字段 | 说明 |
| --- | --- |
| `speciesId` | 跟官方动物表里的物种 ID 一样。 |
| `animatorMode` | 写 `pngSpriteOverride`。 |
| `templateSpritePrefix` | 原版动物的帧名前缀，见上表。必填。 |
| `customSpritePrefix` | 你的帧名前缀。游戏要画原版的某一帧时，改画你前缀下同名的那一帧。必填。 |
| `frameManifest` | 可选，帮你整理图片用的清单。写了文件就必须存在。 |

动物去哪、做什么，由官方动物表的 `schedule_id` 决定，同一个物种的每只动物各按自己的日程走。

## 自带动画机的动物

动物用自己做的动画机时，写 `assetBundle` 模式：

```json
{
  "speciesId": "my_goat",
  "animatorMode": "assetBundle",
  "templateSpeciesId": "goat",
  "animatorBundle": "PackData/animals.bundle",
  "adultAnimatorKey": "my_pack_goat_adult",
  "adultAnimatorAsset": "GoatAdult",
  "childAnimatorKey": "my_pack_goat_child",
  "childAnimatorAsset": "GoatChild"
}
```

- 这时官方动物表的 `animator.url` 写你自己的 key（上例的 `my_pack_goat_adult`、`my_pack_goat_child`）。
- 成年、幼体至少写一组；每组的 key 和 asset 要成对写。
- `templateSpeciesId` 只在 bundle 加载失败时用：改用这个原版动物的画面。省略时用 `speciesId`。它不影响动物的行为。

## 叫声

在 `audio` 一节注册声音，再在 `animals.voices` 里绑到物种上：

```json
{
  "schemaVersion": 1,
  "audio": {
    "resources": [{ "key": "bird-adult", "path": "PackData/audio/bird_adult.wav" }],
    "events": [{ "key": "bird-adult", "resource": "bird-adult", "use": "effect", "route": "NativeAnimal" }]
  },
  "animals": {
    "voices": [
      { "speciesId": "my_bird", "stage": "adult", "nativeEvent": "PLAY_ANIMAL_PET_CHICKEN", "sound": "bird-adult" }
    ]
  }
}
```

`stage` 写 `child` 或 `adult`，`nativeEvent` 写游戏在这个时机原本要播的事件名，`sound` 写 `audio.events` 里的 `key`。只换叫声的包不需要 `definitions`。声音的写法见[音频](AUDIO-CONTENT.md)。

## C# API

引用 `DTMAPI.AnimalContent.dll`（`Private=false`，不要复制进包里），用跟 JSON 一样的字段：

```csharp
using DTMAPI.AnimalContent;

var api = helper.ModRegistry.GetApi<IAnimalContentApi>(AnimalModule.ProviderId);
var registration = api.Register(new AnimalDefinitionBatch
{
    Definitions = new[]
    {
        new AnimalDefinition
        {
            SpeciesId = "my_bird",
            AnimatorMode = "pngSpriteOverride",
            TemplateSpritePrefix = "anim_animal_chicken",
            CustomSpritePrefix = "anim_animal_my_bird"
        }
    }
});

// 不再需要时释放。Mod 退出时也会自动释放。
registration.Dispose();
```

叫声用 `api.RegisterVoices(...)`：每项给出物种、`AnimalStage.Child` 或 `Adult`、原生事件名，以及从音频 API 拿到的 `IAudioSound`。

## 要注意的

- 内容只在包加载时读一次。改了文件，重新部署并重启游戏。
- 同一个物种 ID 只能有一个定义；自带动画机的 key 也不能重复。一批定义里有一项冲突，整批都不登记，之前登记的不受影响。释放以后可以再用同一个 ID。
- 叫声准备失败或被释放，动物照样在，播原来的叫声。
- 畜牧 API 不负责生成动物、繁殖、改存档；Mod 不在时也不替它保管动物。
