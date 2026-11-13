# 音频

DTMAPI 内置音频模块，可以替换游戏原有的音效、加新的音乐，也可以给动物和怪物配声音。不写代码时用内容包；要在代码里播放声音，用 C# API。

音频 API 是 Experimental，以后的版本还会改。

## 内容包：替换一个音效

包里放 `PackData/content.json` 和声音文件：

```json
{
  "schemaVersion": 1,
  "audio": {
    "resources": [
      { "key": "box", "path": "PackData/audio/box.wav" }
    ],
    "events": [
      { "key": "open-box", "resource": "box", "use": "effect", "replaceNativeEvent": "PLAY_RESOURCE_PAPER_BOX", "volumeDb": -12 }
    ]
  }
}
```

- `resources` 列出声音文件。`path` 从包根算起。
- `events` 说明每个声音怎么用。`replaceNativeEvent` 写游戏原有的事件名，游戏播放这个事件时改播你的声音。
- 不用写 `ContentPackFor`，也不用把音频模块写进依赖。

改了文件以后，重新部署并重启游戏。

## 字段

`resources[]`：

| 字段 | 说明 |
| --- | --- |
| `key` | 这个包里唯一的名字，给 `events` 引用。 |
| `path` | 声音文件，从包根算起。 |
| `stream` | `true` 时边读边播，只用于音乐。 |
| `kind` | `source`（默认，普通声音文件）或 `bank`（预先做好的声音包，见下）。 |
| `sha256` | 可选，一般不用写。 |

`events[]`：

| 字段 | 说明 |
| --- | --- |
| `key` | 这个包里唯一的名字。 |
| `resource` | 用哪个 `resources[].key`。 |
| `use` | `effect`（音效）或 `music`（音乐）。 |
| `route` | 声音走游戏的哪条通道。音效默认 `Actor`，还可以写 `Ui`、`Ambience`；音乐固定是 `Music`。给动物、怪物用的声音分别写 `NativeAnimal`、`NativeMonster`、`NativeMonsterDeath`、`NativeMonsterExplosion`。 |
| `replaceNativeEvent` | 替换游戏原有的这个音效事件。 |
| `nativeEvent` | 音乐用：对应官方内容里 CD 或 BGM 的名字。 |
| `volumeDb` | 音量，单位 dB，0 是原样，负数更小。写 `"-inf"` 就是静音。 |
| `loop` | 是否循环。 |
| `minimumIntervalMs` | 两次播放的最短间隔（毫秒），间隔内再触发就不播。触发很密的音效用它防止叠在一起。 |
| `bankEvent` | `kind` 是 `bank` 时，写声音包里的事件名。 |

支持 WAV、MP3、FLAC、AAC（`.aac`、`.m4a`）。PCM 格式的 WAV 直接用；其他格式由 Runtime 先解码一次。音量和长度不会被自动调整。

## 音乐

音乐写 `"use": "music"`，用 `nativeEvent` 接到官方内容里的 CD 或 BGM 名字，长曲目在资源上加 `"stream": true`。随机选曲、播放列表、一首放完接下一首，仍由游戏决定。

## 预先做好的声音包

直接放 WAV、MP3 等文件就能用。也可以提前把音频做成声音包（`.bnk`），游戏里就不用再处理一次。

Windows 上用 `DTMAPI.AudioPrepare.exe`（发行页的 `DTMAPI-AudioPrepare` 压缩包）：选音频文件、用途和一个空文件夹，点“生成”。命令行用 `dtmapi-author audio-prepare FILE --use effect --output DIR`。

完整解压后，双击顶层的 `DTMAPI.AudioPrepare.exe`。工具已带上 .NET 运行库，不用另装；保留旁边的 `audio-decoder` 和 `licenses` 文件夹。

两种方式都会在输出文件夹里写出 `PackData/content.json` 和声音文件。把声音文件按原路径复制进你的包，`resources`、`events` 合进包里的 `content.json`，再给事件加上 `replaceNativeEvent` 或 `nativeEvent`。生成的资源是 `"kind": "bank"`，事件用 `bankEvent` 播放。

工具里的“安静 / 日常 / 热闹”只是试听起点。进游戏和原版声音对比着听，再改 `volumeDb`，不用重新生成。

## C# API

引用 `DTMAPI.AudioContent.dll`（`Private=false`，不要复制进包里）：

```csharp
using DTMAPI.AudioContent;

var api = helper.ModRegistry.GetApi<IAudioContentApi>(AudioModule.ProviderId);
var registration = api.Register(new AudioRegistration
{
    Resources = new[] { new AudioResource { Key = "hit", Path = "PackData/audio/hit.wav" } },
    Events = new[] { new AudioEvent { Key = "hit", Resource = "hit", Use = AudioUse.Effect, Route = AudioRoute.Actor } }
});

// 声音准备好以后再播放。
if (registration.GetSnapshot().State == AudioPreparationState.Ready)
{
    var playback = registration.GetSound("hit").Play(-6);
    // playback.Stop(); playback.Pause(true); playback.SetVolumeDb(-12);
}

// 不再需要时释放。Mod 退出时也会自动释放。
registration.Dispose();
```

- `GetSound` 只找这一次注册里的声音。
- 准备失败时，`GetSnapshot().Diagnostic` 写着原因；修好文件后可以调 `RetryPreparation()`。
- 只有给怪物用的通道支持 `SetPosition` 设置声音的位置。

## 动物和怪物的声音

先用音频 API 注册声音，拿到 `IAudioSound`，再交给畜牧或怪物的 API 绑定：动物按物种、幼体或成年、原生事件名绑定，见[畜牧](ANIMAL-CONTENT.md)；怪物按受伤、死亡、技能等时机绑定，见[怪物](MONSTERS.md)。内容包里，动物声音写在 `animals.voices`，用 `sound` 引用 `audio.events` 里的 `key`。

## 要注意的

- 声音准备失败、或者还没准备好时，游戏播放原来的声音，动物和怪物本身不受影响。
- 释放声音时，正在播放的会先停下。
- 没有任何 Mod 注册声音时，音频模块不启动，不占资源。
