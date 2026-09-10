# 20260909-0003: PN-018 owner 文件与全局数据内部片

## Metadata

- Update ID: `20260909-0003`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: [连续执行授权](20260909-0001-platform-m1-acceptance-continuation.md)，沿 PN-018/D01；依赖 [PN-005 内部片](20260909-0002-platform-optional-services.md)，主卡产品门不豁免。

## Summary

为活动 owner 提供包内只读文本和包外全局 JSON 数据。固定 Type 可选服务、规范化相对 key、路径/链接/大小写拒绝、owner/schema/摘要 envelope 与原子替换。读取区分缺失、损坏、未来格式、迁移不足/失败和 IO 权限错误。逐版迁移验证后一次发布，包撤回保留数据；无 SaveData 或 Steam Cloud 承诺。

## Changed Files

- Abstractions 候选文件/数据契约、Core 路径与数据服务、固定服务目录与 Runtime owner 上下文接入。
- 有界 Core 文件故障和迁移用例。

## Validation

最终出口：[PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)与 R2 已接受本卡有界结果；下列内部阶段的 pending/not-run 是当时状态，已由末尾实测结论收口。

- PASS：`platform-data-core` 五个入口。两个 owner 同包 key/global key 隔离、包撤回和服务重启保留数据、显式删除、关闭后拒绝旧引用。真实符号链接的子路径和根、越界/设备/大小写、非法 UTF-8、实际文件锁均覆盖；权限分类用有界文件边界注入。
- PASS：未来 schema/平台格式、损坏 JSON、摘要错配及摘要有效但载荷损坏均拒绝默认写入覆盖；1 MiB 序列化有界拒绝。逐版迁移一次发布，重复读取和新服务不重迁移；后续迁移失败、临时文件已落盘后的替换失败、迁移提交失败、回调再入/owner 关闭保留原字节。
- 首轮失败为测试断言仅捕获 InvalidOperationException，而路径按契约抛 ArgumentException；改用精确异常后通过，不是生产路径漏检。
- 实际 Mono 和正式 SDK 作者出口 pending，不能用 Core 结果冒称通过。

## Evidence

- `artifacts/pn018-core-first.log`、`pn018-core-second.log`、最终 `pn018-core-final.log`。临时非玩法文件由现有 test-session runner 清理。最终 PN-005 owner 回归和原两个 frozen DLL 的 ABI 调用亦通过。没有进程断电/Mono IO 证明，不把失败窗口注入扩大为平台持久性承诺。

## Rollback Notes

撤回可选入口时保留 global data 字节；不删除用户目录，不修改原生档与 sidecar。

## Follow-Up

内部验证已完成；M1 剩余实机场景、受控 Mono 和 PN-020 仍为主卡出口。最终 owner/frozen ABI、SDK 发布包检查、测试路由与文档治理均通过；available SDK target 仍只有 0.5.5，没有发布这些新契约。

最终实测结论：Mono 两 owner 的包文件分别为 17/29，全局计数四次冷启分别到 4/104；公开 withdraw 三包后两个 global 文件的摘要/长度/时间和数量不变。没有断电或原生保存事务承诺。 证据及失败沿 [PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)。公共面继续 Experimental；最终冻结归 PN-007.b，未发布。
