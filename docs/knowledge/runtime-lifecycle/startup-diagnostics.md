# 启动与进程注入的历史诊断

按需复用已证失败层次。当前症状及后续工作由 [ISSUE-001](../../debug/issues/ISSUE-001-direct-exe-fatal-popup.md)、[ISSUE-004](../../debug/issues/ISSUE-004-steam-launch-stuck.md) 和 [ISSUE-012](../../debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md) 拥有；本页不维护第二份状态表。

## 先确定等待发生在哪里

2026-05-31 多批正常样本中 Runtime Awake 约 0.58–0.64 秒，启动墙钟可以超过十秒；另一类失败没有新 DolocTown 进程或日志。2026-07-15 的 26.8 秒墙钟中 Runtime 仅 658 ms。把 Launch→Process、Process→新 Runtime 日志、Runtime 内部分段分开，才能选择下一层。不能用更多正常样本否定间歇故障，或用预进程卡顿要求优化 Harmony/扫描。

人工 1 ms 阈值证明 stop 分支，不是慢启动实证。早期正常采样已完成而缺真正慢样本时，原任务明确停止重跑 A–E；采样器、monitor、observer 是同一调查的工具，不是普通 Mod 的持续验收义务。分类器、阈值和汇总文案变化可使用已有原始时间线或模拟输入。历史 `Awake` 字段的回退标签不能误述当前实际启动时点。

默认 Steam 启动缓解直接 EXE 留下“另一个实例”fatal 窗口的问题；进程消失后窗口仍可能存在。Steam 预进程卡住曾在重启 Steam 后恢复，不应归为游戏 Hook 失败。采集器须分别保留进程、窗口和新鲜日志，不能先杀进程再判断 fatal。

来源：[5 月启动/采样记录](../../archive/updates/2026/20260531-0014-startup-normal-vs-blocked.md)、[ISSUE-001](../../debug/issues/ISSUE-001-direct-exe-fatal-popup.md)、[ISSUE-004](../../debug/issues/ISSUE-004-steam-launch-stuck.md)。具体旧文件从[迁移清单](../../archive/migrations/20260908-workspace.json)查询；旧 observer 清日志动作不构成当前证据处置规则。

## 静态安装、当前注入和按钮可见分别取证

7 月玩家标题按钮缺失案，旧日志正常不证明本次进程启动了 Runtime。调查后来证明全部 22 个 Loader 文件与正确分发 ZIP 一致，移除第三方 Mxx DLL 后仍失败；不能归罪其直接干扰或损坏。游戏目录和系统 `WINHTTP.dll` 同时驻留可以是代理转发现象，不能单凭名称判冲突。Procmon `QueryOpen` 仅证明元数据访问，不能写成已经 `CreateFile/ReadFile` 或执行 Preloader。

一次错误把不存在的解压缓存目录发给玩家作“可信基线”，空集合遂将正常文件全部报 extra；游戏已退出后 Modules 为空也只是前提失败。后改用已校验的分发 ZIP。应先验证路径、PID 和启动时间，避免让玩家承担工具假设错误。

最终重启 Windows/Steam 会话后，冷/热启动都有新 Runtime 与按钮日志，玩家也确认可见；该事故的恢复已成立。精确 Doorstop 继承环境和 IAT 状态没有在旧进程存活时捕获，瞬态 native 原因未证实。用户/机器环境为空不能证明长期 Steam 进程的继承环境为空。

冷 Steam 的成功进程在 71 秒才出现，旧 probe 只等 60 秒，超时后丢失迟到 PID 所有权。可复用的改进是有界冷启动等待、迟到进程处理和只收必要的实际进程状态；不应为已恢复事故继续修改 UI 或重装游戏。来源：[完整人工调查](../../archive/reviews/manual-qa/2026/20260711-0001-player-title-settings-runtime-missing.md)、[ISSUE-012](../../debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md)。

## 工具输出不能覆盖首个失败

主运行目录中的 timeline/result/Steam 尾日志属于一次观测。外层只接受非空、存在且在允许范围内的 evidence 路径；child exit、stderr 和证据发现失败分别保留。观察外部启动的 observer 不伪造启动，也不把旧日志认成本轮。具体进程、证据和 deadline 的编排经验见 [运行器知识](runner-evidence-and-cost.md)。
