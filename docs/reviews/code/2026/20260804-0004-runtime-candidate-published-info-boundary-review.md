# Runtime 候选与已发布 info.json 边界审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：DTMAPI 0.6.0 Runtime package/Catalog root-cause review
- Source：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

本 Review 只保存完整 Release 门首次从干净 `0.6.0` 候选重建 Runtime 包时暴露的边界错误及修复约束；不接管产品生命周期或发布授权。

## 1. 失败事实

提交 `7fb3a4f5` 后从干净树运行 `tools/scripts/test.ps1 -Configuration Release`。Core Unit、QA Unit、InstallDoctor `12/12`、Player Doctor release/portable 均通过，随后 Runtime package Catalog gate 报告：

- 期望 `info.json` SHA-256：`4AB3D471747A7E2EC37BDA01884A604DCC63B0B74B764C6D34412B4DAE905144`；
- 实际 SHA-256：`C148F2EBB71805A9D749C7683F8EF9B053E74C6A049F64290EEC6AF432A82D41`。

两者都是 `9,192` 字节的 native-stable form，唯一预期语义差异是版本从已发布 `0.5.5` 投影为当前源码候选 `0.6.0`。以冻结 `0.5.5` 文件做候选包期望，会让任何合法的新 Runtime 版本都无法通过默认 Release。

## 2. 所有权分类

Catalog 已有两个不同事实 owner：

1. `runtime.currentSourceBaseline` 拥有当前候选版本三轴；
2. `runtime.currentPublishedArtifact` 拥有 Steam 已发布 `0.5.5` 的 manifest、树与 `info.json` 精确 identity。

失败来自 package 参数门在验证新构建时错误读取第二个 owner。不能把 `currentPublishedArtifact` 改成 `0.6.0`，因为 0.6 尚未发布；也不能删除已发布精确哈希或把候选版本降回 0.5.5。

## 3. 有界修复

- 在既有 `currentSourceBaseline` 内记录确定性的候选 `info.json` 长度、哈希及 `DeterministicSourceProjectionNotPublishedArtifact` 状态，不新增 receipt/schema/第二套 Catalog。
- package 参数门核对候选版本、候选 stable-form 长度/哈希和三个空 `localized_name` 字段；当候选版本不同于已发布版本时，另断言两者哈希不得相同。
- `currentPublishedArtifact` 的 `0.5.5` 版本、`4AB3...5144` 哈希、Steam manifest 与 owning Update 全部保持原样并继续由静态 Catalog 门验证。
- Unit 反证同时锁住两个 owner，并禁止恢复旧的 `Built Runtime ... matches published stable-form` 比较。

## 4. 验收与剩余边界

本修复只允许从源码构建的 `0.6.0` Runtime 包通过正确的候选 metadata gate。它不把包声明为已发布、不授权上传、不刷新 Steam 实物 identity，也不代替最终 package tree、installer、retained-consumer、game smoke 或 ISSUE-011 验收。候选整体冻结后仍须按路线图记录 exact candidate；实际发布后才可用新的 post-publication evidence 更新 `currentPublishedArtifact`。
