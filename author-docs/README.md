# DTMAPI Author Docs

这是 DTMAPI 面向模组作者的独立说明文档区。

这个目录只放作者文档，不是游戏内容包，也不是 DTMAPI Runtime 的一部分。当前发布脚本只显式拷贝 `tools/release`、`assets/branding`、构建输出和 `testmods` 的发布内容，不会把仓库根目录的 `author-docs/` 自动放进游戏目录、官方本地 `MODS` 目录或 Workshop 上传包。

未来如果要单独发布作者文档，可以直接把这个目录作为文档包来源；不要把它复制进普通模组的 `Content/` 目录里。

## 文档索引

- [内容包作者文档](content-packs/README.md)
- [JSON + PNG + WAV 自定义小动物指南](content-packs/custom-animal-json-png-wav.md)
- [JSON 派生数值与生产时间校验](content-packs/json-derived-value-validation.md)

## 当前约定

- 本目录当前只覆盖 ContentPack 作者路线；Strict / Advanced / ContentPack / External 的规范定义以根目录 [`PROJECT.md`](../PROJECT.md) 为准。G2 只证明 synthetic fixture，当前真实 Advanced 产品也都由各自独立准入和 identity-bound tracked policy 授权；这些第一方策略没有开放通用 Advanced 作者路线。`SDK160` 继续保护 Strict；作者不得手工伪造 `CodeModKind=Advanced`、receipt 或包，也不得把受管 DLL 放进 `BepInEx/plugins`。
- 以后补充 Strict 文档时，必须同时说明 API stability 与 disposition；`DTMAPI.Abstractions` 中的 public 类型不等于全部 Stable。Diagnostic、Frozen、Disabled、DTMAPI-internal 和 Proposed 表面不能作为新普通 Mod 的推荐依赖，具体以 [`public-api-matrix.md`](../docs/api/public-api-matrix.md) 为准。
- 普通作者优先写内容包：`info.json`、官方 `Content/*.json`、`Content/DTMAPI/*.json`、PNG、WAV。
- 普通作者不需要写 DLL，不需要放文件到 `BepInEx/plugins`。
- 当前没有通用、独立、已发布的 Optional Content Host（G7 仍未实现）。现有已启用内容包由领域专用 Runtime/GameBridge 路线读取部分 DTMAPI 扩展 JSON；这是过渡实态，不代表通用 ContentPack host 已完成，也不是单产品 ProductNative 永久归属 GameBridge 的规则。
- 这里的文档会尽量写成可复制、可检查、可手测的步骤，不把反编译源码或官方二进制内容带入文档包。
