# 20260909-0008: PN-006 内部精确反射内核

## Metadata

- Update ID: `20260909-0008`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 连续执行授权，PN-007.b 完成后领取 [PN-006](../../planning/platform-next/tasks.md#pn-006内部通用反射内核)，沿 A06 已确定签名/失败/寿命规则。

## Summary

实现 Core 内部 BCL-only 精确反射解析、字段/属性/方法 wrapper 和 scope 关闭。缓存仅保存有界成功元数据，实际 Type 身份参与 key；无负缓存因此不阻止晚加载。迁移 GameBridge 的 CurrentL10nId 和 ItemProto.Title 两条只读路径，原 QueryItemProto out 适配保留。未新增公共服务或修改冻结 API 0.7.0。

## Changed Files

Core 内部 reflection namespace、聚焦测试；GameBridge 两条既有只读访问点。新公共 facade 属 PN-021，不在本片。

## Validation

`platform-reflection-core` 三个注册测试入口通过：私有继承/隐藏/精确重载、静态/实例、合法 null/Missing、错误类型/不可写/unsupported/目标异常、并发有界元数据/同名跨 assembly/晚加载、显式 Dispose 与 owner 关闭两类 GC roots。最终增加弱注册及调用准入锁：丢弃 wrapper 不被 scope 留根；已进入的调用在关闭后用局部强引用完成，关闭后新调用拒绝，不持锁执行目标代码。A06 的 MissingFieldException、参数 ArgumentException、void Invoke unsupported 和歧义候选诊断一并校准。最终内核日志 `artifacts/pn006-core-lifetime-final.log`，现有 item-display-name 回归通过。

两条真实 Mono 只读路径已对照。语言轮覆盖中文、英文、繁体及 Title/LoadingWorld/WorldReady；6 个对照组合全部相等。该轮手动退出超时，保留 Failed 原记录，不能冒充整体通过；随后短烟测正常退出 Passed。最终弱注册/准入锁候选又完成一次两条中文路径的完整烟测 Passed，未修改冻结 ABI。两个最终 run 均验证 NoNativeSave、存档/sidecar 保护、QA/owner 关闭和进程退出。

## Evidence

复用 [R2](../../reviews/code/2026/20260909-0003-platform-m2-r2.md) 的未变化 M2 验收。[语言对照与失败保留](../../debug/evidence/GAME-SMOKE/20260909-platform-pn006-runtime/README.md)、[最终内核烟测](../../debug/evidence/GAME-SMOKE/20260909-platform-pn006-lifetime/README.md)。外部探针 0.1.2 通过冻结 SDK 0.2/API0.7 生成；早期 EntryType 不匹配是探针错误，修正后冷启，未隐藏历史失败。

## Rollback Notes

恢复两条原只读适配，移除内部实现/测试；不触碰玩家存档、已冻结载荷或公共 ABI。

## Follow-Up

本片完成。按已细化 A06 继续 [PN-021](20260909-0009-platform-public-reflection.md)；该卡公共接口和候选 target 不回填 PN-006 的历史游戏字节证据。
