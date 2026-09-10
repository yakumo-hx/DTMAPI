# 20260908-0015: PN-008 外部作者闭环

## Metadata

- Update ID: `20260908-0015`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source,unit,runtime`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: 已授权 M1 实施任务，沿 platform-next 本卡与现有原生事实。

## Summary

公开 SDK→官方启用→真实异常定位→修复→冷更新→新进程→官方停用→撤回已实际贯通。A1–A3 返修与重启补证后，输入、跨档/同档、新游戏、真实 IO 失败同进程恢复和退出均已验证。R1 接受有明确限制的 M1 候选并开放 M2 实施；Runtime Validation 保留 partial，以免将未证断点和已失败的原生空档分支泛化为通过。

## Changed Files

- 两个仓库外 Strict 项目、对应源码/公开命令 JSON、固定 SDK/Runtime 候选、NoNativeSave 与恢复证据；继承 PN-014/015/004/016/017 相关实现。

## Validation

- E01 已证部分：陌生合法 ID 无 Catalog/白名单变更，真实候选 SDK new/validate/单次 pack/Doctor/install-local，官方界面启用；中文/空格路径 IDE 与 CLI 的有效输入、DLL/PDB 相同。包/磁盘更新不冒充加载成功，0.1.2 在新进程有版本和 MVID 证据。
- E02 已证部分：Entry 异常、事件异常、错误 owner 和配对符号、修复/重启、日志与配置特殊字符往返。Entry 行号精确；事件堆栈只落在对应回调末行，通过唯一错误消息定位 throw。断点未证明，旧驻留与强改磁盘不一致实机负例未执行。
- E03 partial：单档原生加载、主线程/单调 tick、SaveLoaded 对象缺失、随后 native frame 存在性、正常退出及官方停用 owner 清理。跨档与同档重载被自动化键盘无响应阻挡；新游戏和可控加载失败未执行。
- 故障及更正未抹去：首轮 info.json 原生迁移与回滚重复取锁由 PN-004 修复；修复后的 0.1.1 经 native discovery 直接 update 到 0.1.2，再经 discovery 直接 withdraw 均成功。外部首次把不存在的 InputScope.Any 用入源码时 CLI 给 CS0117，按已冻结 Always 更正；未修改 API。
- 现场最终 PASS：原生档 30 件、受保护 sidecar 5 件，全部 hash/长度/UTC mtime 不变且无新增；官方停用两个样例、公开 withdraw、session clear、原 Runtime/配置/MoreSaves/官方启用字节恢复、游戏退出、共享锁释放。保留本次诊断和事务 recovery；无原生保存、存档写回、Workshop 上传。
- 源码/包证据复用：配置 focus、pack-build、official-local、platform-sdk-targets、platform-session-core 与 SDK prepare/check 的相关实际 PASS。没有重新跑完整产品/Release 图来替代缺失交互证据。

## Evidence

- 当前 [重启验证](../../debug/evidence/GAME-SMOKE/20260909-platform-m1-reboot/README.md)补齐新游戏、三类受控失败和冷恢复。最后真实 IO 失败控制组在同进程恢复、返回标题并完成 owner/QA close，runner PASS；coordinator 失败终态修复由 0013 持有。缺失/特定损坏档转原生空档的 UI 重试仍失败，精确文件冷启动可恢复，后续 context 必须失效关闭。以下首轮和续测的 partial 叙述是各次实际结果，缺口状态以本段和 R1 为准。
- 现场恢复纠正：此前只验证了 profile 根目录 mod_infos.json，原“官方启用字节恢复”不能扩展到原生实际 SAVE/mod_infos.json。本次正确 SAVE 基线未变、30+5 玩家文件未变、DLL 恢复和锁释放见当前 evidence。

- 2026-09-09 [续测](../../debug/evidence/GAME-SMOKE/20260909-platform-m1-continuation/README.md)：标题 F8 可见打开状态页、world F8 有 owner callback、原生 Escape、跨档/同档重载和真实 resident/disk 不一致均补证。新游戏/原生失败仍缺，原因更新为隔离会话窗口捕获连续失败。正常关闭、公开撤回/清会话、现场恢复和 30+5 保护文件未变均已完成；不将这些子项通过扩大为全卡完成。

- [固定输入、执行链、输出和恢复](../../debug/evidence/GAME-SMOKE/20260908-225439-platform-m1/README.md)。此目录是实际 evidence owner，外部作者工程仍在 `E:/Python_project/DTMAPI-author-validation-20260908`。
- [活跃 smoke 行](../../debug/regressions/smoke-matrix.md)保留首次候选失败与最终 partial 两项。

## Rollback Notes

先证明游戏退出与 NoNativeSave 字节未变，再恢复精确非存档测试资产；不恢复/覆盖玩家存档。

## Follow-Up

[R1](../../reviews/code/2026/20260908-0016-platform-m1-r1.md) 接受 M1 有界出口并允许 M2 实施。断点未证、事件末行精度、原生空档失败分支及历史启用恢复声明纠正均保留；不能用本卡冻结新 target、宣称全部公共 API ready 或替代 PN-020 的双作者服务验收。
