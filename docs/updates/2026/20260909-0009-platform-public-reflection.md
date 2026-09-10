# 20260909-0009: PN-021 公开反射与作者候选

## Metadata

- Update ID: `20260909-0009`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 连续执行授权及 [PN-021](../../planning/platform-next/tasks.md#pn-021公开反射与作者示例)，沿已确定的 A06/AD-06 实施。

## Summary

通过 optional services 提供 A06 的完整精确反射 V1；保留 IDtmHelper 原 ABI 和冻结的 0.5.5/0.7.0 载荷。API 0.8.0 / SDK 0.3.0 / Runtime 0.8.0 隔离候选已完成外部作者和 shipping Mono 验收。普通 catalog/default 仍为已冻结 API 0.7.0，本片未发布或冻结 M3 共享依赖格式。

## Changed Files

Abstractions 四个反射接口及 GetReflection 扩展；Core owner facade 复用 PN-006 内核与既有 owner cleanup participant。取得服务仍在 Runtime 线程；取得后使用锁保护的 owner 状态检查，普通托管对象的查找/调用同步发生在调用者线程。新候选构建脚本与 [作者说明](../../../author-sdk/REFLECTION.md)，两套仓库外 SDK 工程，以及公开 facade/owner 测试。矩阵新增 Experimental 行，Catalog 反映当前 53 行和 319 个生产源文件，不修改发布事实。

## Validation

- `platform-reflection-public` 4 个注册入口 Passed，覆盖精确类型/错误/弱登记、显式 Dispose、两个 owner 隔离、Runtime 关闭和已开始调用；`public-api-status` Passed。原 `.cs` 与候选运行源逐文件 hash 相等，见 source-parity.json。
- 既有 `platform-services-core` 三个入口回归 Passed；Catalog、文档治理及 diff whitespace 检查 Passed。
- 独立 SDK 0.3.0 候选包检查 Passed，包含三个 target；第一次构建遗漏旧 payload 根文件，发行检查准确拒绝，修复目录复制后全新 candidate-b 通过。原失败保留。
- 外部正常作者 new/build/pack Passed，重复 pack hash 相等；冻结 SDK 0.2 拒绝未知 0.8，显式旧 API 0.7 编译反射调用返回 CS0246/CS1061，不允许借新 Runtime 绕过旧载荷。
- 真实 Mono 故意 Entry 失败的作者，在 Dispose 时已拒绝 wrapper 调用；其后仍持有 wrapper 的独立作者观察 target GC 回收、包装器失效且自己仍可访问。该负例轮因已知 recent owner failure 使 GameBridgeFinalHealthSnapshot/RunStatus=Failed，保留原结果；所有 NoNativeSave/保护/进程/owner 清理 gate Passed。
- 撤回失败作者后，同一候选和正常作者完整冷启/加载/返回标题/退出烟测 Passed。原 retained helper 与 consumer DLL 未重编译，在新 Abstractions 上 shipping Mono 得到 result=73、writes=1、optionalMissing/requiredMissing=True。正常作者的 Runtime 关闭前置拒绝也 Passed。
- 两包公开 withdraw；五 DLL 恢复、30 存档及 5 sidecar 的 hash/length/mtime 和数量不变，真实 SAVE 启用不变、fixture 启用恢复、QA 清除、runtime lock 释放。没有 native save 或玩家存档写回。

## Evidence

内部内核见 [PN-006](20260909-0008-platform-reflection-core.md)。[实机/候选/恢复证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn021-runtime/README.md)，其余编译、旧 target 拒绝、包检查在 artifacts/pn021-*。本片没有重新运行未变化的 M1/M2 全套场景。

## Rollback Notes

撤回后续 target 候选及新接口/facade；旧冻结载荷不变；不涉及 native save。

## Follow-Up

本片的公开反射候选已验收，稳定性仍 Experimental。M3 整体未完成；PN-011/010/022/023 仍需明确 shared/private inventory、新版本区间与 provenance schema、引用/许可证闭包、跨工程失败语料及 R3 冻结对象。它们目前是下一轮待细化工程包，不把方向条目或本片成功解释为完整开放生态已经交付。普通 target 开放与 M3 发行必须另有实际验收和冻结记录。

PN-036 successor：[新内部 0.6.2 组合](20260909-0011-platform-internal-version-realignment.md)已验证；本记录原 0.7/0.8 字节和当时结论仍保留。后续 M3 细则已由 [执行包](../../planning/platform-next/execution-next.md)补齐，继续实施。
