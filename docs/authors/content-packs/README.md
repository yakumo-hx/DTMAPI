# Content Pack Author Docs

这个文件夹收纳“内容包作者”需要的说明：作者只准备 JSON、图片、音频等资源，让 DTMAPI Runtime 和 Doloc Town 官方内容系统负责加载。

## 当前文档

- [新增养殖动物 HTML 教程](custom-animal-tutorial.html)：按文件复制完整 JSON，含字段说明、PNG/WAV 与游戏验证步骤。
- [音效、音乐与磁带 HTML 教程](custom-audio-tutorial.html)：完整 JSON、31 个曲池范围、磁带和音频预制工具。
- [JSON + PNG + WAV 自定义小动物指南](custom-animal-json-png-wav.md)
- [JSON 派生数值与生产时间校验](json-derived-value-validation.md)

## 放包位置的简短判断

普通内容包应该长这样：

```text
YourPack/
  info.json
  manifest.json
  Content/
    ...
    Sprites/
      ...
  PackData/
    content.json
    audio/
      ...
```

作者包通过官方本地 MODS 或 Workshop 内容包路径启用。Runtime 0.7.5 当前候选从统一入口 `PackData/content.json` 读取动物、怪物与音频分区；资源路径相对包根。详见[当前模块边界](../../architecture.md)。
