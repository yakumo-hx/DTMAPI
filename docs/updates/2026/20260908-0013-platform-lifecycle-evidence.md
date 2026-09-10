# 20260908-0013: PN-016 生命周期事实

## Metadata

- Update ID: `20260908-0013`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 已授权 M1 实施任务；原卡返修和重启补证，R1 沿用既有 Review。

## Summary

外部 Strict 与既有原生边界观测补齐 M1 生命周期事实：启动、跨档/同档重载、新游戏、加载失败与恢复、返回标题和退出。SaveLoaded 时 room 尚不可用；不能据此发布 WorldReady。受控失败另修正 coordinator 未结束原生 false 返回请求的问题。缺失/部分损坏档的原生空档分支仍有同进程 UI 重试限制，已作为 PN-009 的失效约束记录。

## Changed Files

- GameBridge `LifecycleObservation` partial、既有 Hook callback 的 SafeCallback 观测和外部 Strict 样例；[focused map](../../hook-map/focused/RuntimeLifecycle.md)。
- Core coordinator 将原生 false 返回结束为 native-failed；原生 bool 日志与有意义的失败/重试/超时回归。

## Validation

- PASS：Entry→GameLaunched→Update、事件异常后下一 tick 继续；持键 F8 标题状态页可见、world callback 与原生 Escape；同进程 A→标题→B→标题→同 B 重载→标题。线程 1，tick 单调，内部 generation 0→1→2→3。
- PASS：重启后空隔离槽原生新游戏、教程、帐篷保存进度与返回标题，QA/owner close 和 runner 通过。新游戏绕过 LoadGame；SaveLoaded 时 normal=true、room=false，教程后首个观测 native frame 五项均 true。原生保存仅限此新建 disposable slot。
- PASS：真实文件共享冲突返回 false 且无 SaveLoaded；解除锁后同进程新请求成功读档、人物/HUD 可见，随后返回标题、QA/owner close、正常退出和清理。`20260909-082409` runner PASS。失败后旧请求终止，不抑制下一请求。
- PASS：`phase1-core-cleanup` 27 入口，覆盖 false→新请求→成功及晚返回不改写 timeout 终态；兼容 .NET 8 Core/Bootstrap/QA 构建。修复候选在 shipping Mono 明确记录 false、active=none，并完成上述重试。
- 已证限制：移走或注入特定损坏数据会走原生 success/null→NewGame→false 分支，原生 loaded 标志可能为 true，但无 AfterLoadArchiveData；恢复数据后 UI 重试无第二次 LoadGame，停留背景，原生档案更新报错。这些 runner FAILED 保留，不能用最后 IO 控制组覆盖。精确原档经冷启动恢复成功；具体 UI completion 取消机制仍是匹配源码及对照支持的推断。
- 30 玩家档案与 5 sidecar 的 hash/长度/mtime 及清单不变；未写回玩家存档。五个原 DLL 恢复，QA 清理、游戏退出、锁释放。

## Evidence

- [重启六次会话与逐项结果](../../debug/evidence/GAME-SMOKE/20260909-platform-m1-reboot/README.md)：保留三类失败、冷恢复、真正 IO 失败同进程恢复、最终现场恢复和修复日志。
- [跨档/同档与输入续测](../../debug/evidence/GAME-SMOKE/20260909-platform-m1-continuation/README.md)；[首轮实测](../../debug/evidence/GAME-SMOKE/20260908-225439-platform-m1/README.md)。早先输入和窗口障碍均已解除，历史失败不抹去。
- 恢复声明纠正：早先核对的是 profile 根目录 mod_infos.json，不能证明原生 SAVE/mod_infos.json 已恢复到最初值。本次正确保留并验证实际 SAVE 基线；不影响档案/sidecar 不变证明。

## Rollback Notes

确认进程退出和玩家文件不变后，仅恢复精确非存档测试资产；不覆盖玩家档案。诊断回退不改变原有事件或 native-frame drain。

## Follow-Up

[R1](../../reviews/code/2026/20260908-0016-platform-m1-r1.md) 接受 M1 事实出口，开放 PN-009 实施。PN-009 必须先失效再尝试，失败保持不可用；新游戏需自己的原生开始边界，不能以 LoadGame 覆盖它。世界/场景 ready 还需匹配 native 证据和 PN-020 功能实测，不能从存在性采样、IsDataLoaded 或持续 tick 推断通用就绪。
