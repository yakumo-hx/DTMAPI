# 20260909-0011: PN-036 内部版本与候选归位

## Metadata

- Update ID: `20260909-0011`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 用户新版本决定及 [独立验收](../../reviews/code/2026/20260909-0004-platform-m2-m3-acceptance.md)、[PN-036 执行规格](../../planning/platform-next/execution-next.md#pn-036内部版本整理和候选归位)。

## Summary

首发前版本统一回到 0.6.X：建立 Runtime/API/SDK 0.6.2 内部候选，包含已验收 M2 与反射。原 M2 0.7 和反射 0.8 是未发布历史快照，保留原字节/recipe/证据，公开 0.7 保持 planned。不改已发布事实、冻结 0.5.5、assembly compatibility 或会话/schema 编号。

## Changed Files

版本 props、可变 Catalog、target catalog、SDK 当前工程/最低版本/模板及新 compatibility recipe；旧内部快照移入 internal-snapshots。历史结论仅加 successor 路由。

## Validation

- version projection、Runtime/SDK target matrix、session handshake、两个 compatibility recipe 及普通 SDK 构建/包检查 Passed。公开 0.7 planned 拒绝，旧 payload 不能仅靠 marker 改写进入新目标。
- 外部 CLI new/pack Passed，重复 pack SHA 相同；同一源在 0.5.5 目标下编译因缺新 API 被拒绝。新 SDK 不含 internal-snapshots 或旧 0.7 payload；冻结 0.5.5、原 M2 全清单 hash/length/数量和原 SDK 0.2 ZIP 不变。
- 新五 DLL 的真实 Mono runner 20260909-133823 Passed：会话 API target/Host 0.6.2，后台提交到 thread 1，Completion Succeeded，反射值 62，会话命令成功，owner 在 Mod.Dispose 前关闭。原 retained 两 DLL 未重编译，result=73、writes=1；加载/返回标题/退出及清理通过。
- 测试包公开 withdraw、会话 clear、fixture 启用恢复、五 DLL 恢复，30 存档和 5 sidecar 的 hash/length/mtime/数量不变，真实 SAVE 启用不变、QA 清除、锁释放。
- Catalog/source metadata 检查 Passed。这里没有正式 Runtime 包：Doctor 记录候选 DLL 与原安装 receipt 版本不同、无法确认安装版本；不能记安装一致性 PASS。误将 SDK 工具目录传给要求十个产品包及 repeat 根的 release-contract 检查所产生的失败保留，该产品发行门留给 PN-031。

## Evidence

[实机与恢复](../../debug/evidence/GAME-SMOKE/20260909-platform-pn036-runtime/README.md)，构建/CLI/冻结清单在 artifacts/pn036-*；原历史路径在快照 README 中路由。

## Rollback Notes

恢复本卡可变投影和旧目录布局；原冻结及历史 payload 不能重写。游戏测试后恢复明确改变的部署资产，不写回玩家存档。

## Follow-Up

完成本卡后直接继续 PN-011/R3.shared 及本批其余已细化任务，不在版本整理后停止。
