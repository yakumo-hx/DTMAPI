# Author SDK 精确引用夹具来源审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：DTMAPI 0.6.0 Author SDK Advanced integration/root-cause review
- Source：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

本 Review 只保存默认 Release 的 Author SDK Advanced 集成夹具为何在 Doloc Town 1.00 本机环境下触发 `SDK202`、精确输入应由谁拥有，以及不允许采用的伪修复。

## 1. 失败事实

非验收诊断尾段在 Author SDK release/portable 前置通过后，`DTMAPI.AuthorSdk.Tests` 的第一个 Advanced build 报告 `Tracked Advanced game reference length/hash mismatch`。测试把当前安装的 `24456188` / `E861E07E...0923` `Assembly-CSharp.dll` 复制到临时游戏，却把临时 Steam build ID 写成 `23762374`，随后要求 `doloctown-23762374-g2-v1` 成功。这不是游戏 Drift 激活语义：Author SDK 构建必须按 policy 对 game build、长度和哈希全部 exact，`SDK202` 是正确失败。

## 2. 所有权结论

未改源码的既有 Advanced 产品仍按路线图保留各自 `23762374` policy/package，并在新 Runtime 中通过 Drift 激活。不能为了让测试适配当前游戏而新增通用 `24456188-g2` policy、给未改产品批量重签，或把当前字节伪标成旧 build。

旧 policy 的构建测试应读取本地私有、已冻结且哈希精确的 `23762374_public_C416D4` reverse raw snapshot；Harmony 由已跟踪的离线 BepInEx ZIP 提供，其精确 identity 仍是 `204,800` / `1A21CC03...031`。两个输入只复制进受管测试会话，不进入 SDK、产品包、Git 或发布树。测试还接受显式 `DTMAPI_AUTHOR_ADVANCED_REFERENCE_ROOT`，但该 root 同样必须先通过旧 Assembly 与 Harmony 的长度/哈希核验。

## 3. 有界修复

1. Author SDK Tests 不再从“当前安装游戏”构造旧 policy 夹具；只用上述精确输入或诚实报告 exact input unavailable。
2. 原有 G2 build/pack/deploy、product policy selection、compiler-surface 排除、receipt tamper、native/helper bundling rejection 和 wrong-build 反证全部保持原 policy 与断言。
3. 测试工作区迁入共享 `DtmApiTestSession`，遵守 `DTMAPI_TEST_TEMP_ROOT`、lease、receipt、失败保留和清理规则；不再创建仓库顶层 `.tmp/author-sdk-tests-*` 根。

## 4. 非授权事项

本修复不改变 SDK `0.1.0` / target `0.5.5`，不开放通用 Advanced authoring，不改变 `SDK160`，不修改任何现有产品的 policy/receipt/package，也不声称旧 23762374 产品已在 1.00 重新构建。DebugConsole 是本轮唯一因实际源码修正而切换到 24456188 policy 的产品，仍由其独立 Review 和路线图证据管理。

## 5. Resolution Owner

后续 all-Advanced 默认调用链再次暴露了同一来源边界：未显式传入精确 game root 时，构建器会误取当前安装游戏，而不是产品 policy 所绑定的 build/reference bytes。这仍是本 Review 已记录的 exact-reference fixture 根因，不是新 compatibility 决定，也不授权给未改产品重签当前游戏 policy。

调用链修复、changed files、正负矩阵、确定性包结果、临时工件清理和 Release 状态只由 [DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md) 维护；本 Review 不再追加实施或 PASS 叙事。
