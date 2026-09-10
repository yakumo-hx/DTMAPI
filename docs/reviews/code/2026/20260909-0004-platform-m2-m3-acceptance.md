# M2 / 反射独立验收与下一批工程决策

- Date: 2026-09-09
- Status: accepted with bounded follow-up
- Implementation: [本轮计划 Update](../../../updates/2026/20260909-0010-platform-m3-release-plan.md)
- Scope: 当前工作区源码、M2/R2、PN-006/021、候选包与后续任务设计；没有重新执行游戏。

## 结论

接收 M2 的已声明能力和 PN-006/021 反射切片。保持 R1/R2 对日志调试、宿主失败恢复、输入设备和焦点证明的限制。M3 整体未完成，当前不能发布为完整开放生态；下一步从版本整理与共享依赖开始，无须再重构平台分层。

## 独立核对

查看 optional service/owner scope、scheduler 队列与取消、反射绑定与清理、SDK 工程输入拒绝和符号错误处理。上一轮 A1 的不匹配 PDB 现在进入 SDK191；A2/A3 的构建输入投影、迁移保全与额外工程输入拒绝已进入 SDK pack-build 回归。独立构建相关测试项目，runtime、reflection、owner data、settings 和 pack-build focused 测试全部通过。这里的通过只覆盖这些检查，没有宣称穷尽并发行为或全部代码无缺陷。

重新计算普通 SDK 0.2.0 ZIP hash，与 [PN-007](../../../updates/2026/20260909-0006-platform-sdk-candidate.md)的固定包一致。核对 [PN-020](../../../updates/2026/20260909-0007-platform-m2-author-validation.md)的外部项目、三个最终 runner、原 retained helper/consumer 两 DLL 原字节调用；[PN-021](../../../updates/2026/20260909-0009-platform-public-reflection.md)的正常 runner 和恢复 JSON 与说明相符。反射故意 Entry 失败轮整体 Failed 被保留，没有当作普通 PASS。

这些游戏结果复用对应固定候选，不能外推到下一份版本改名后或 M3 合成包。原 retained ABI、实际程序集绑定和冷启动需要在最终合成候选抽验。此前所谓真实键盘证据来自系统输入注入到游戏的路径；用户没有试实体键盘，本机也没有实体手柄。不能把测试文字中的“物理输入”外推为人手按键或控制器验证。

## 需要修正的计划事实

1. 源码 Runtime 已写 0.7.0、普通 SDK 已将 M2 API 0.7.0 available/default；反射另外使用 API/Runtime 0.8.0 隔离候选。它们均未发布，却占用了用户刚确定的首发和清退版本。PN-036 先保留原冻结输入/字节作为历史内部快照，再产生全新 0.6.X 内部候选；不能全仓替换版本或覆写原 hash。公开 0.7.0 最早在 M3 和发行验收后产生。
2. status 把 PN-005/009/018/019 的“直接前置”改成 PN-020/R2，把最终证据回路误写为实施依赖，形成循环。本轮恢复任务真实前置；最终产品证据继续放 Update/R2 列。
3. PN-011/010/022 只有方向，缺少字段、旧 reader 分流、预加载冲突、引用闭包和独立工程语料。补为 [包契约设计](../../../architecture/platform-package-contracts.md)及 [执行包](../../../planning/platform-next/execution-next.md)，R3 用实际 Mono 证据确认；不是让实现任务再次从零设计格式。

## 发布与清退决定

M3 是 0.7.0 的最早能力门；还须发行、升级恢复和作者交付成立。M4/M5/M6 不必全部做完才有可发布版本，按路线分成 0.7.X 出口；某分支受阻不阻止无依赖能力。

目前部分旧实现已退役或禁用，但许多公开接口/DTO 仍以 Frozen、Obsolete 或 Disabled 壳保留。0.8.0 开始正式物理清退是可行的，需要在 0.7 系列先交付逐族迁移/不再提供能力的说明、旧 DLL 诊断和预览。首批候选包括 retired lamp、没有 native host 的 custom entity 壳、已迁出产品玩法的旧桥接入口；准确集合以现行 API matrix 与旧二进制扫描为准。没有把稳定 helper ABI、保存 reader 或所有 0.5.5 编译的 Mod 一刀切删除。

版本和公告日期必须同时达到；具体公开日期在首个 0.7 发布材料中给出，不把研究中的周期天数当现行承诺。某族条件未齐可移入后续 0.8.X，其他已齐族继续；作者未回复不等于没有用户，也不形成永久阻止清退的条件。

## 手柄决定

PN-037 分绑定与导航两片，复用 Input/ConfigMenu；不等待 M5 通用 UI。GameBridge 吸收设备采样、游戏菜单焦点与输入占用，AutoFishing 只保留玩法 toggle，Bootstrap/配置页面适配层负责可见菜单。先让方向键完整验证焦点模型，再接游戏已有的手柄方向/确认/返回动作。没有设备的部分保留实验状态，公开收集按设备/系统/Steam Input 路径标注的反馈后分别晋级，不以没人投诉判定完成。

## 执行出口

同一实施任务按 PN-036 → PN-011/R3.shared → PN-010/R3.native → PN-022 → PN-023 → 首发准备与弃用准备 → PN-037 两片连续开展。没有必要为了里程碑编号重新开任务。发布授权和手柄反馈待办与可继续的内部实施分开；完成本次已经细化的范围后统一交回验收。
