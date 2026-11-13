# DTMAPI Author Docs

这是 DTMAPI 面向模组作者的独立说明文档区。

这里保存面向内容包作者的指南。作者文档不会自动进入游戏、官方本地 MODS 或 Workshop 包。

未来如果要单独发布作者文档，可以直接把这个目录作为文档包来源；不要把它复制进普通模组的 `Content/` 目录里。

## 文档索引

- [内容包作者文档](content-packs/README.md)
- [新增养殖动物 HTML 教程](content-packs/custom-animal-tutorial.html)
- [音效、音乐与磁带 HTML 教程](content-packs/custom-audio-tutorial.html)
- [早期 JSON + PNG + WAV 自定义小动物指南](content-packs/custom-animal-json-png-wav.md)
- [JSON 派生数值与生产时间校验](content-packs/json-derived-value-validation.md)

## 当前约定

- 本目录覆盖 ContentPack 作者路线；DLL Mod 从 [Author SDK](README.md) 开始。CodeMod、ContentPack、External BepInEx Plugin 的身份以 [架构说明](../architecture.md) 为准。
- 当前内容包使用根目录 `info.json`、`manifest.json`、官方 `Content/`，以及按需填写的 `PackData/content.json`、PNG、WAV。
- Runtime 0.7.5 当前候选内置畜牧、怪物和音频模块，按内容贡献启动。格式与职责见[模块边界](../architecture.md)及[畜牧指南](ANIMAL-CONTENT.md)。
- 作者包通过官方本地 MODS 或 Workshop 路径启用。纯内容包可直接编辑 JSON 和资源；SDK 为可选工具。
- 文档提供可复制、可检查、可手测的步骤。
