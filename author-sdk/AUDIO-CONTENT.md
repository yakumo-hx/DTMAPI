# Audio：注册、声音与播放

Runtime 0.7.5 候选内置 `DTMAPI.Audio`，与 Animals、Monsters 共用贡献生命周期。本次没有公开发布。普通包使用固定内容入口，高级作者直接引用纯托管 `DTMAPI.AudioContent.dll`（`Private=false`）；契约由 Runtime 供给，无需单独安装或启用 Audio Mod。

## 普通内容包

`PackData/content.json` 采用 schemaVersion 1。资源路径始终相对真实包根，官方原生数据仍在 `Content`。

```json
{
  "schemaVersion": 1,
  "audio": {
    "resources": [{"key":"jump","path":"PackData/audio/jump.wav"}],
    "events": [{"key":"jump","resource":"jump","use":"effect","route":"Actor","replaceNativeEvent":"PLAY_CHARACTER_JUMP","volumeDb":-12}]
  }
}
```

没有使用的分区可省略。内容登记自动建立所需模块使用关系，不填写内置 ContentPackFor 或重复模块依赖。显式声明的真实包 Required/VersionRange 仍须满足。参见[畜牧声音入口](ANIMAL-CONTENT.md)及[Content Host 指南](CONTENT-HOSTS.md)。

支持 WAV、MP3、FLAC、AAC（`.aac`、`.m4a`）。PCM WAV 直接准备，其余受支持格式由 Runtime 的固定解码组件准备；不会自动调整响度或裁切。媒体准备失败保留动物、怪物主体，并回退原生声音。注册状态和诊断用于判断可播放性，修正资源后可重新准备。

## 高级 API

```csharp
var api = helper.ModRegistry.GetApi<IAudioContentApi>(AudioModule.ProviderId);
var registration = api.Register(new AudioRegistration {
    Resources = new[] { new AudioResource { Key = "hit", Path = "PackData/audio/hit.wav" } },
    Events = new[] { new AudioEvent { Key = "hit", Resource = "hit", Use = AudioUse.Effect, Route = AudioRoute.NativeMonster } }
});
var sound = registration.GetSound("hit");
// 在 GetSnapshot().State == AudioPreparationState.Ready 后播放。
var playback = sound.Play(-6, new AudioPosition(x, y));
playback.Stop();
registration.Dispose();
```

注册持有需求并返回状态、局部声音句柄、`RetryPreparation()` 和释放入口。`GetSound` 只查本注册；没有跨注册字符串查找。播放句柄支持停止、暂停、音量与受支持的空间位置控制。清理未完成时注册仍持有资源及退出关系，继续尝试 Dispose；Owner 退出也会驱动清理。旧声音和播放句柄不能操作新注册。

`AudioUse` 为 Effect/Music；有限路由为 Actor、Ui、Ambience、NativeAnimal、NativeMonster、NativeMonsterDeath、NativeMonsterExplosion、Music。空间控制仅用于支持它的怪物路由；不要用任意 Wwise 参数替代这些原生路由。音量使用 dB，静音可用负无穷。音乐使用 Music 路由及相应会话生命周期。

Animals 决定物种、幼年/成年及原生发声选择；Monsters 决定受伤、死亡、技能和子体声音。将同 Owner 的 `IAudioSound` 交给它们的绑定 API，Audio 只执行播放。动物替换发生于原生发声准入后的 Post 边界；领域候选与通用替换共同仲裁，胜者未就绪时回退原声。声音撤回先停止领域播放，再释放媒体。

## 音乐与 Bank

音乐事件声明 `use: "music"`、`route: "Music"`，以 `nativeEvent` 对接原生 Content 中 CD/BGM 的逻辑名称。`resources[].stream` 仅用于音乐。原生随机选曲、播放列表和歌曲自然结束流程仍归游戏所有。第一方完整声明见 [EveningStarCassette](../products/first-party/EveningStarCassette/audio-data/content.json) 和 [TouhouMusicPoolPack](../products/first-party/TouhouMusicPoolPack/audio-data/content.json)。

预制 Bank 使用资源 `kind: "Bank"`、受控的 media/dependencies 相对路径和事件 `bankEvent`。准备工具和 AudioPreparation 为纯托管，可离线使用，不要求游戏或 Author SDK。CD 已解锁身份保护和启动 BGM 合并属于 GameBridge；无音频包、关闭 BGM append 或媒体组件不可用时，CD 身份保护仍执行。

无相关注册时不创建音频后端、准备任务、播放或业务 Tick。查询 API 不产生需求。成功回收后可在同进程再次注册，不承诺 CLR 程序集卸载。旧音频入口已清退；当前第一方包须与本次 Runtime 配套更新。
