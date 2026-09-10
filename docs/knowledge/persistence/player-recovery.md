# 玩家存档恢复的已证边界

本页整理历史维护经验，不包含玩家文件、解密材料或私有脚本。当前存档语义与工作流由 [PROJECT](../../../PROJECT.md)、[玩家反馈流程](../../workflows/codex-feedback-to-goal.md) 拥有。修档必须以具体输入、已证状态与明确授权为界；历史个案不是任意存档通用修复许可。

## 旧城恢复信：只补已证缺口

[20260905-0001 审查](../../reviews/manual-qa/2026/20260905-0001-xingchen-ruined-city-save-repair.md) 使用第一槽离线档，核验的是前置主线完成、正常前置信已读且跨日、旧城入口访问过而候选与未处理对话不存在、旧城无活跃/已完成任务、没有恢复信。任务 chainInfos 清单只是定义，不能当作已开始；当前 enabledMods 为空也不能证明历史从未用 Mod。

当前构建的官方恢复路线以旧城入口访问计数大于零发送 `ruinedcity_continue`，该信附带 `ruinedcity_main` 且首次阅读自动接受。此档符合精确准入，足以补一封未读原生信，不足以归因某个 Mod，也不应降低版本重跑全部迁移、恢复已访问对话候选或直接完成主线。

只插入一个对象；日期、版本、顶层及 baseData 槽号、任务、对话、背包、世界保留原明文字节。删除插入段应逐字节重建原明文，从最终 ZIP 重开也要得到同一结果。第一槽交付保持第一槽身份，游戏读档验证与首次读信接任务/继续推进是不同验收事实；一次无保存隔离载入只能关闭前者。

## 邮件投递与外部行为是不同结论

[20260603-0008](../../archive/updates/2026/20260603-0008-second-motor-mail-status-smoke.md) 用背包与未领取邮件去重，再走原生模板投递；当时原生忽略自定义标题/正文/发件人参数，因此不能按 API 参数存在就宣传自定义邮件。第二摩托 key/骑乘/回退 PASS 仍伴随 `dualVisibleAfterKey=False`，正式可见探针把真实单例限制保留下来。邮件发送成功不能替代最终用户行为成立。

## 采集与修复应复用哪些已有事实

[20260808-0002 collector](../../archive/updates/2026/20260808-0002-player-save-crash-log-collector.md) 是独立三文件双击包，不是 Runtime 四个安装动作之一。历史合同为退出游戏后只读定位完整 SAVE 与 bounded crash/log/state，复制稳定性用 source length/mtime 与两端 hash 证明；缺 SAVE 或不稳定仍输出明确 Incomplete。特殊字符路径、PowerShell 5.1、最终解包字节均已测，未运行游戏。它不解密/修复/重命名/恢复 archive。完整 SAVE 是该次用户的明确需求，不意味着所有反馈都必须完整采集；protected-items 缺失这一诊断口由 ISSUE-028 明确指出，应按具体 bug 选取。

[20260809-0001](../../archive/updates/2026/20260809-0001-ruined-city-player-save-repair.md) 是同类首次定点修复：只插 337 bytes，136→137 封，原 ZIP 不变，从最终包再开核验。该次实机为了隔离把测试副本两个 index 改到 11，但交付保持 0；9 月实例已经保持原第一槽做隔离验收，可复用工程应采用后一种身份一致方式，不把历史改 index 当必要步骤。两个个案都只证明载入兼容，玩家首次读信推进仍需区分。

[20260905-0001 实施](../../updates/2026/20260905-0001-xingchen-ruined-city-save-repair.md) 以原槽位/原版本的交付字节完成 CoreOnly 隔离 NoNativeSave，源明文只增加 338 bytes、152→153 信；游戏测试没有例行 archive 备份/写回，交付前显式原件备份是另一目的。该次没有改 Runtime/Hook/产品，因此无需源码 build 或完整 Release。首次治理失败是 Update 缺四个标准章节，已补齐；不能把它算成游戏失败。玩家任务推进仍未确认，交付快照已注明替换后的回档日期。

## slot 0 修复：原件留在维护端，不常规再备份玩家端

[定点恢复 Update](../../updates/2026/20260808-0003-player-slot0-verified-recovery.md) 的用户明确要求不在玩家电脑额外备份。实现以已收集损坏 current hash 为唯一输入，验证仍加密的恢复源后，在同目录 partial 验证并 File.Replace；只换 current，原生 prev/JSON/AutoCloud 与其他槽不变，变动输入拒绝，重复运行幂等。该工具针对已授权的存档写入，需要 disposable 脚本矩阵；不能套为普通无保存游戏测试的流程。

第一版恢复旧 prev 仅修好存档列表，世界仍黑。后继日志与六代存档证明空 ChickenNest 与有 bot 的 AutomateBotStation 早已重叠；cold load 的拥挤移除先于 bot 初始化，对空 decisionMaker 调 OnUnload，使 farm 未建完。后续 room/树 NRE 是级联，不是首因。把空窝左移一格仅改两个数值的三个明文字节，保留 bot/站和其余状态，脚本 exact-byte 与拒绝/幂等均通过。原始硬闪退原因仍未知，不能用这个正常退出的 cold-load NRE 反推它。

截至该 Update 与其 [Manual QA owner](../../archive/reviews/manual-qa/2026/20260808-0001-save-list-null-scene-after-debug-save.md) 的最终记录，玩家冷启动后无需 Escape 就能看到角色/地形/物件仍待确认，因此保留 implemented/mitigated。不能把加密回读、离线无重叠或恢复 BAT OK 当作玩家世界可用。
