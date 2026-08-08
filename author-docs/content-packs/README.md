# Content Pack Author Docs

这个文件夹收纳“内容包作者”需要的说明：作者只准备 JSON、图片、音频等资源，让 DTMAPI Runtime 和 Doloc Town 官方内容系统负责加载。

## 当前文档

- [JSON + PNG + WAV 自定义小动物指南](custom-animal-json-png-wav.md)
- [JSON 派生数值与生产时间校验](json-derived-value-validation.md)

## 放包位置的简短判断

普通内容包应该长这样：

```text
YourPack/
  info.json
  Content/
    ...
    DTMAPI/
      manifest.json
      custom-animals.json
      audio-replacements.json
```

不要把普通作者内容包放进：

```text
BepInEx/plugins/
```

`BepInEx/plugins/` 只属于 DTMAPI Runtime 自己的启动组件。作者包应通过官方本地/Workshop 内容包路径启用，然后由 DTMAPI 扫描已启用内容包。
