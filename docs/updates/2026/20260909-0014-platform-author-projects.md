# 20260909-0014: PN-022 作者项目、资源与显式 restore

## Metadata

- Update ID: `20260909-0014`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 连续执行授权、[P06](../../architecture/platform-package-contracts.md#p06多项目资源与显式-restore)、[R3.native](../../reviews/code/2026/20260909-0006-platform-r3-native.md)。

## Summary

普通作者可以在受控 workspace 中构建 library/ProjectReference DAG，声明嵌入、复制资源和已生成源码，并通过显式锁定 restore 准备纯托管依赖。CLI、IDE、CI 共用 BuildPlan，build/pack 不联网。

## Changed Files

已扩展 Authoring.Contracts 的 SDK-only 工程输入、ProjectGraph/BuildPlan、库模板、资源打包、NuGet.Protocol 7.9.0 锁定 restore、schema/CI 与对应测试。新工具为 0.6.5，保持已冻结 0.6.4 SDK/API 字节；原生生成组件单独保持 0.6.4 及精确 DLL hash，避免无行为变更的工具升级破坏已绑定契约。没有新的 Runtime API 或强制 DLL。

## Validation

项目/restore 集成、Native 契约回归、pack-build/sourceDirectory 迁移和 target matrix 均通过。NuGet 真实 Nett.Coma→Nett 冷缓存下载、离线空缓存拒绝与缓存重放通过；有许可传递库和资源由公开 pack 打包。图/路径/生成源码/资源/unsupported XML/TFM、cache hash、native/build/analyzer 负例通过，失败保留旧包。CLI/IDE 的 DLL/PDB/输入摘要相同；公开 CI recipe 及作者逻辑测试通过。目标矩阵原先默认目标仍写 0.6.3 的旧预期已修正为 0.6.4 后重跑通过，未改变生产默认值。

最终 SDK 与 candidate-a 字节相同，release check 通过，ZIP SHA-256 `008b429c3e3ac9841f5443dc29862327c313eb0eb9ca460910ab991117726a8f`。实际 Mono 170521 Passed：Entry、command、SaveLoaded 均验证第三方及两层自编库、两份嵌入资源、复制资源和生成值，退出关闭；原 retained 两 DLL 返回 73/writes=1。两个测试 Mod 已公开撤回，五 DLL/fixture 恢复，30+5 保护及真实 enablement 通过。完整 M3/官方 UI 不在本卡冒称完成。

## Evidence

本卡 `artifacts/pn022-*`、[固定实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn022-runtime/README.md)、[作者项目/restore 指南](../../../author-sdk/PROJECTS-AND-RESTORE.md)。独立目录保留旧输入和失败日志，无上传。

## Rollback Notes

撤回新 SDK-only 工程功能；已生成的旧 target/SDK 和玩家安装不迁移。

## Follow-Up

继续 PN-023 合成验收；显式 restore 已交付，没有从 M3 移出。NuGet 标准锁的创建/更新需要作者显式 .NET SDK 操作，自包含 Author SDK 负责锁定重放；未支持资产有明确拒绝。
