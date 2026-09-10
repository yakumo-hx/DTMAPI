# 20260909-0016: PN-033.a 弃用与迁移准备

## Metadata

- Update ID: `20260909-0016`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 连续执行授权及 [PN-033.a](../../planning/platform-next/execution-next.md#pn-033a07-系列的弃用准备)。沿现行 API matrix 的 Frozen/Disabled 决定；不新增删除决定。

## Summary

保持旧 ABI，完善按作者/能力去重的运行提醒、离线调用诊断和逐族迁移说明，为 0.7 候选准备可核对的旧二进制语料。最早 0.8 删除仍需实际公告和逐族门；本卡不删接口、DTO、provider 或存储 reader。

## Changed Files

Core registry 增加作者/旧契约去重提醒并在 owner 关闭时释放，复用现有非 error Obsolete；Doctor 补逐族产品替代与能力损失说明，现行 API matrix 统一保有候选日期与逐族处置。SDK 随包携带迁移指南和可编译的 unavailable 示例；原 SDK 冻结包不变，下一组合包由 PN-031 承接。

## Validation

Core focused 两入口通过：一万次重复 lookup 只提醒一次、没有超过普通 lookup 的新增分配、作者隔离、释放/重新激活、非旧 API 不误报、诊断异常不改变返回。初始“整个 registry lookup 零分配”断言错误地覆盖了既有 facade 闭包分配，改为衡量新增诊断成本，保留原失败日志。

Doctor 全部测试通过；第一次运行发现四个 Runtime 身份 fixture 仍硬编码 0.6.1，与当前 Abstractions/Compatibility Host 不一致，导致两个 installed-version 用例失败。仅将可变测试 fixture 接入已有版本 props，原发布/retained DLL 不变，重跑通过。最终 Runtime 构建通过，Core 155 入口通过。

指定四根目录的 31 DLL 对照 58 旧类型完成只读元数据扫描；本地 MineMod、StrongPlantingGunMod 仍有旧引用，不能推断无人使用。公开 SDK 的 0.5.5 旧消费者与同 ID 0.6.4 迁移版本 pack 通过；实机 181822/181952 均 Passed：旧查询一万次仅一条提醒、原 disabled 行为不变，新版加载/进档不再查旧接口。原 retained 两 DLL 两轮 result=73/writes=1、未重编译。公开撤回、原 Runtime/fixture 选择恢复、30 存档和 5 sidecar hash/长度/mtime/数量不变，真实选择不变，QA 清除、锁释放。

## Evidence

[实机与扫描范围](../../debug/evidence/GAME-SMOKE/20260909-platform-pn033-runtime/README.md)，artifacts/pn033-* 与 artifacts/pn033/ 原始报告。没有宣称未知外部调用不存在，也没有以新编译示例冒充原发布 Mod；原 retained ABI 与新增旧 target 示例分别标明。

## Rollback Notes

移除本卡新增诊断与材料即可回退；冻结 payload 和旧 ABI 不变。

## Follow-Up

合入完整 0.7 候选验收和 PN-031.a，物理删除留给 PN-033.b。
