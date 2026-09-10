# 读档生命周期与诊断成本

本页保留 2026-07 的历史调查方法与否证，不拥有 ISSUE-010 的当前根因、状态或新长测入口。继续该问题须从 [Debug 路由](../../debug/INDEX.md)找到现行 owner。

## 保存边界不是根因结论

Phase 8.6 的无闲置 20 次读档、分段闲置约 6535 秒均通过，连续一小时标题闲置加较大常驻对象图则曾崩溃。循环次数或累计时长不足以解释当时样本。各 Product save/title transient 为零，常驻配置与 owner 对象数较大却稳定，不能据此销毁原生对象或宣布泄漏。

独立源码审查未找到每次 LoadGame 重复注册而不清理的直接证据；它发现外部 fatal 未进入 runtime 计数、静态 Harmony callback 未清理和第三次起 SaveLoaded 阶段命名回退等独立缺口。`fatalEvents=0` 不能覆盖进程在记录前已崩溃；清理诊断字符串记录也不证明原生对象回收。

## 诊断本身也会改变崩溃窗口

Phase 8.9 比较已有两份包：无 probe 的崩溃发生于 terrain/dungeon 原生读档；强制 GC 样本到达 SaveLoaded 后在完整对象快照日志路径崩溃。后一份只证明重日志可能成为即时触发或放大器，不能反推它是最初根因。轻量 breadcrumb 只记录阶段、计数与关联 ID；对象遍历、LINQ 聚合、字典展开不应塞进关键路径。

Phase 8.16 用窄 continuation probe 再次看到 LoadGame.Enter 后、AfterLoadArchiveData/VersionPatcher/MapManager 之前的 terrain/dungeon 崩溃；未触发的 probe 不能被归为本次根因。Lite、无 HookProbe 与 UI isolation 未消除该样本；应保留该失败而不是退回已经穷举过的 UI 组合。大 dump 的摘要与真实文件分离，历史命令不是每次 Mod 修改都要执行的流程。

## 测试 oracle 也需要独立原生事实

20260724-0006 审查发现 Zoom 的 fake native reset 与实际 SetEnvCamera 不同，测试竟把产品自己放大的值当原生基线；同轮 NoNativeSave 可比较错误目录，也有实际 Product 操作未覆盖的问题。正确方向是校准 oracle 和观察路径，再重跑受影响场景。真实旧证据中路径正确的部分仍然有效，不必因一个通用 runner 缺口全部否定。

## 长测曾经回答的具体问题

[协调器实现](../../archive/updates/2026/20260703-0007-saveload-request-coordinator.md) 只抑制 DTMAPI/smoke 的同槽重复请求，不截获玩家原生 UI。`SaveLoaded` 可早于 LoadGame postfix；最初 ledger 因返回晚到凭空造第二请求，后来修正为同请求闭合。一次一小时 PASS 不能证明 Fatal GC 已消失。

[后续长测](../../archive/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md) 的两小时单次 load 成功、一小时周期首 load 失败、降 pending 频率后第二 cycle 失败，排除了“只有时长”和“重复 LoadGame”作为该样本充分解释。600 次 pending 压力独立通过进一步否定高 pending 数本身是必需触发；14/15、570/600 校准失败是采样时序漂移，不是 Runtime 崩溃。两次直接 native LoadGame 路径失败也是 harness 可靠性问题，后改用原有官方 auto-load 路径。

[功能二分](../../archive/updates/2026/20260705-0001-saveload-cycle-delta-ledger-bisection.md) 与 [按 owner 计数](../../archive/updates/2026/20260705-0002-saveload-per-owner-delta-cadence.md) 已完成无 idle 20 cycle、十分钟 idle 后五分钟间隔共 6,535 秒等控制。最小失败组合包含 UI 并不证明某个 UI owner 是根因，AutoFishing 缺席的失败也不能继续归它。原记录明确不要再开长测只证明同一 Fatal 存在；后续应等具体缓解、清晰 telemetry 或更窄原生边界，周期测试默认 opt-in。

## 长夜节：未保存的剧情进度回退不等于进程资源完全重建

[暖读档年夜饭 Review](../../reviews/manual-qa/2026/20260823-0003-evernight-newyear-warm-reload-partial-init.md) 记录先跑泽尼瑟支线、不睡觉保存而回标题，之后同进程重载，在20:20年夜饭两次落同一Lightman.OnRenderNpc空引用。IL偏移精确指向钓鱼绳Unity对象；早前强制显形成功反证安装从开始就缺资源。普通退场会回收并reparent钓鱼绳，强制ReturnHome清理绕过专用退场，可能在两个对象池保留失效父子关系。半初始化NPC摆放无回滚是已知owner事实，具体哪次池复用销毁仍无逐帧证据；暖残留必要性与Qiuzy混杂仍需最小冷/暖对照。

该玩家current与prev都结构可读；current是在最后一次载入后睡过跨年产生，没有保存半初始化节庆，问题是错过当年内容。单独保留的文件已经等于prev1（20:20），无需再找上上代；prev2提供更早准备时间，prev4才用于完整暖链，.bak是旧删除备份不是上一代。不能不问进度取舍就覆盖current。

9/7官方1.00.07补了final对话队列、时间地点门与普通transform-null保护，但Lightman/FestivalState与1.00.06逐字相同；不能靠公告“节庆已修”关闭本例。此Review仍是未完根因owner，未实施补丁、未新增smoke。本页保留这项缺口，不把旧建议中的四组测试无条件推给普通任务。
