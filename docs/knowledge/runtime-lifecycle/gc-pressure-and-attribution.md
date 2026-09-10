# 原生崩溃与持续压力的证据边界

当前入口是 [ISSUE-010](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)及匹配的 [标题待机输入压力协议](../../debug/protocols/title-idle-input-pressure.md)。本页保存历史推理和反例；旧的长测、隔离和广覆盖路线不会自动成为普通 Mod 修改的必跑条件。

标题连续待机后进档的原生 GC 故障，要分别写出时间窗口、触发条件和失败点。静态注册数量稳定不能推出没有压力：旧字符串按钮注册还在标题每帧调用 GetKeyDown/GetKey，既是根也是活动工作量。该路径被证明足以放大标题待机压力，未被证明是所有原生 GC 故障的唯一原因。

现有协议的重要转折是保留 Mod 及其代码/事件/配置根，仅移除一种活动根，再为该路径增加计数和可控放大器。50 个虚拟键能把低信息量长等待转为更短的定向复现。每帧输入采样及 typed edge 分发后来替代全局注册字符串轮询；仍要保护短按、按住和同帧按下/释放语义。

这类修复的压力样本、原始失败组合、近真实启用组合各回答不同问题。某一路线通过只支持该窗口缓解，不能关闭任意长时间游戏的原生 GC 大类。已有生命周期/原生 continuation/dump 证据应复用；发现活动输入根后继续无限按 Mod 二分，会拖延真正机制的定位。

## ISSUE-010 全文中的结论演变

6 月首轮识别的 UI binder、克隆和 pending cast 风险确实属于 DTMAPI，但“可疑持有点”不等于已经证明 GC 根因。早期审查建议 EnvironmentReset 都销毁，后续安全修正撤回：SetEnvCamera 很频繁，不能每次摧毁 UI、取消钓鱼或强制机器扫描。保存/回标题的破坏性清理和环境刷新必须分开。

7 月观测排除了若干充分条件：20 次无待机循环通过；间隔待机共 6,535 秒通过；两小时单次读档通过却一小时后的循环失败；600 条 pending 压力通过而把 706 条状态降到 19 条仍失败。重复 LoadGame 在协调器后已排除，旧的 SaveLoaded 早于 native return 还曾被诊断误算为第二请求。不能把累计时长、循环数、状态条数或“没有 managed exception”直接当因果结论。

完整快照、Lite 和强制 GC 探针产生不同即时窗口。原始 no-probe 在 Terrain/Room/Dungeon 激活前后、SaveLoaded 前失败；另一条件到 SaveLoaded 快照日志时失败；Lite 有时到 Hook.Exit 后、native return 前失败。没有命中 VersionPatcher/MapManager breadcrumb 的样本不能归罪这些后续方法。全量快照可放大压力，但它并非所有复现的必要条件；相同根计数既有 PASS 也有 Fatal。

移除 UI code roots 两次通过、回加 YConsole+Zoom 两次失败后，按根类型隔离才取得高价值进展：保留两者代码/事件/配置，仅去输入任一侧都能降到不复现的窗口。50 虚拟键把约一小时复现压到 20 分钟，原本计划的更长压力测随即停止。修正 scoped sampling 后，50 个根仍存在但标题轮询归零；随后补用户短按/连按回归，再验原组合和 FullKnown。

Issue 旧的“Next Required Fixes”保留许多阶段性保全/下一步指令，之后已被输入修正、用户验收、Batch 5 和候选验证更新。它们不是同时待执行的 33 项任务。7 月 13 日用户接受已测标题长待机基本解决用于规划；现行未完大类仍是任意长游戏的原生 GC，不能再以同一路线仍已知失败为前提不断补测。

## 短时 crash 与外部审查

[ISSUE-011](../../debug/issues/ISSUE-011-20260623-short-run-native-crash.md)单列旧 0.5.2 约 6.5 分钟的原生 crash：没有相邻 managed exception，也没有对应 dump。NoWeeds Harmony 失败及 InputSystem 噪声同样出现在正常退出对照中；Qiuzy 是该短时样本的独立 BepInEx 变量，不能拿它解释未加载该插件的长时样本。Y 给物品是场景事实，不是根因证明。

该 Issue 后来关闭的是精确 0.6 候选的 bounded release gate：第三档 NoNativeSave、标题/Y 搜索/tooltip/真实输入给物品、fresh crash/正常退出与部署恢复通过。原 0.5.2 crash owner 仍未证明，新玩家复现仍须新证据。verified 字段不能省略这个关闭范围。

20260628-0001 审查把早期风险按 Hook 线程、输入、UI churn、native roots、第三方插件及诊断列出；0002 再把 17 项扩成机制/收益/风险/比较问题。后者是同一审查的讨论投影，其“成熟系统可能做法”是推断，不是 SMAPI 调查结论，更不是额外实测。两篇旧的 30–60 分钟九组矩阵由后来的定向复现和当前验证流程取代；其中 off-thread Hook、ownerless 输入和保留 delegate 的发现保留为结构演进依据。

Phase 8.7 的七组通过只覆盖四个单项和 YConsole+装备、YConsole+存档、存档+装备三个组合，并未覆盖后来复现的 YConsole+Zoom。因此不能把“所测 high-value pairs 都过”概括成“UI 两两组合均排除”。阶段性的 stop 指令表示当时停止无新信息的猜测；后来新 profile/根类型证据可以合理开启一个更窄的实验。

8.12 的 no-HookProbe + Lite 证实特定条件仍会出 Fatal，但只有旧 crash 目录；8.15 的 UiRuntime 根隔离又明确没有关闭 DebugConsoleHost。缺 fresh stack、unsupported owner 和手工补写 result 都是证据边界，应保留于解释中。把相同实验详情复制到 Review、Issue、Update 不增加证据强度，本知识页只保留结论变化及原 owner。
