# 畜牧内容

`DTMAPI.Animals` 随 Runtime 0.7.5 供给。普通包使用 `PackData/content.json` 的 animals 分区；高级作者直接引用纯托管 `DTMAPI.AnimalContent.dll`，不复制进产品。

```json
{
  "schemaVersion": 1,
  "animals": {
    "definitions": [{
      "speciesId": "hatch",
      "templateSpeciesId": "chicken",
      "aiTemplate": "chicken",
      "animatorMode": "pngSpriteOverride",
      "adultAnimatorKey": "dtmapi_anim_animal_hatch",
      "childAnimatorKey": "dtmapi_anim_animal_hatch_child",
      "frameManifest": "Content/Sprites/hatch_frame_manifest.json",
      "templateSpritePrefix": "anim_animal_chicken",
      "customSpritePrefix": "anim_animal_hatch"
    }]
  }
}
```

物种、物品、文档和其他原生数据仍由官方 Content 定义，必须保持原生 ID。资源路径相对包根；本文件仅补充 AI、Animator、PNG/Bundle 映射。

```csharp
var api = helper.ModRegistry.GetApi<IAnimalContentApi>(AnimalModule.ProviderId);
var registration = api.Register(new AnimalDefinitionBatch { Definitions = definitions });
var state = registration.GetSnapshot();
registration.Dispose();
```

每个注册可独立撤回。物种和 Animator 键全局排他，整批冲突拒绝且保留原注册；文件的坏候选保留最后有效代次。没有覆盖优先级。注册后即持有定义需求，不根据当前可见动物数量释放。成功撤回后允许再次注册。

声音另用 `RegisterVoices(AnimalVoiceBinding[])`。通过同 Owner 的 Audio 注册取得 `IAudioSound`，绑定物种、child/adult 阶段和完整原生事件名。纯声音包只需 audio 与 animals.voices，无需 definitions。声音失败或撤回保留动物主体，原生准入与回退仍有效。示例见 [AnimalPack](../products/first-party/AnimalPack/PackData/content.json) 和 [音频指南](AUDIO-CONTENT.md)。

本 API 不提供动物生成、繁殖、存档修改或缺包恢复。关闭单个注册只撤回该注册，Owner 退出撤回其全部贡献；清理未完成时保留句柄并重试。内容入口与包依赖见[Content Host 指南](CONTENT-HOSTS.md)。
