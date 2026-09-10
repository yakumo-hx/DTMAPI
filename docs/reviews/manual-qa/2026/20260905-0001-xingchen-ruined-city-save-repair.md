# 星辰神帝玩家档：旧城恢复信补发前核验

- Date: `2026-09-05`
- Review Status: `recorded`
- Source: 用户要求阅读必读文档、定位官方公告和本地修复记录，确认存档条件满足且未触发后直接补发旧城信。
- Source path correction: 用户给出 `D:\下载\多洛可\_ 星辰神帝`；本地实际唯一匹配目录为 `D:\下载\多洛可_ 星辰神帝`，内含 `doloc-save-0.data` 与 `Player.log`。
- Prior Review: [20260808-0002，Issue 5](20260808-0002-public-newgame-and-functional-mod-regressions.md)
- Prior implementation: [20260809-0001](../../../archive/updates/2026/20260809-0001-ruined-city-player-save-repair.md)
- Implementation owner: [20260905-0001](../../../updates/2026/20260905-0001-xingchen-ruined-city-save-repair.md)

## 原始反馈和可见证据

用户指向官方公告中的“去旧城的信无法触发”，未附截图。官方 Steam 公告的“已知问题公示”确实列有前置主线完成却未收到旧城市废墟邮件的症状，见[官方社区公告内容](https://steamcommunity.com/app/2285550?l=schinese)。公告对功能性模组的整体归类不能单独证明本玩家的历史致因。

原存档长度为 `2552395` bytes，SHA-256 为 `64FF8956758DA7C19C33D020690E1AEFD2083A08CB4BC69A1B93CBE734BEE044`。只在内存解密，未改原件。日志仅到标题页初始化，没有该次任务或升级现场；当前 `enabledModInfos` 为空，不能据此推断全部历史启用情况。

## 当前存档事实

- 原生槽位及 `baseData.archiveIndex` 均为 `0`；版本 `1.00.06`，历史版本记录含 `0.95.12` 与 `1.00.06`。
- 当前日期是总第 `170` 天、第 `2` 年第 `3` 月第 `2` 日，`12:15`。
- `wetland_main` 已完成，`wetland_main_0` 至 `_6` 及 `wetland_main@1` 全部完成。
- 正常前置信 `wetland_main` 于第 `60` 天发送，已读且未回收。前置跨日条件已经满足。
- `visitedArgs[ruinedcity_main_entsk]=1`；澳柯玛 `candidateNodes` 已无该节点，`unhandledDialogueNodes` 为空。
- `ruinedcity_main` 无 active handle、无 completed chain、无已完成子任务或独立活跃子任务。`chainInfos` 中的任务 ID 清单只是定义投影，不是任务已开始的证据。
- 邮件总数 `152`；没有 `ruinedcity_continue`、`ruinedcity_main` 或带对应主线的补救邮件；没有 `version_patch0900` 访问记录。

## 原生 owner 与推断边界

核对本地当前游戏相同 build `24966367_public_958EAF` 的本地只读参考：`Email`、`EmailAttachMission`、`EmailManager`、`LocalSave`、`AesEncryptor`、`DateInfo`，以及 `email_tbemail.json`、`version_patch.yarn`、`ruinedcity_main.yarn`。该 build 由 Steam appmanifest 解析定位；不复制官方源码或资产到交付包。

官方恢复脚本以 `ruinedcity_main_entsk` 的访问计数大于零为条件发送 `ruinedcity_continue`。该邮件标题为《关于旧城市废墟》，发件人为澳柯玛，附件为 `ruinedcity_main`，首次阅读自动接受。普通新流程与旧档恢复信是不同路径。

本档精确符合此前定点恢复的准入状态，足以支持补发一封官方恢复信。现有资料不能区分旧版内容结束、迁移未落地、历史对话中断或外部异常等原始触发者；不推断 DTMAPI 或某个第三方插件致因。版本已越过一次性迁移门，单纯继续跨日不足以补齐该状态。

已排除本例仍在等待跨日、邮件其实在回收站、主线已开始/已完成等情形。禁止通过降低版本重放整个迁移组，禁止重新添加已访问的对话候选或直接完成主线。

## 数据分类与验收

这是一笔用户明确授权、对下载的离线玩家档进行的 save-bound gameplay recovery。派生交付文件只插入一个未读原生邮件对象；日期、版本、槽位、任务、对话、背包和世界均保留原始明文字节。源文件及独立备份是回滚依据。

生成前再次检查准入条件与源哈希；生成后从交付 ZIP 重新解密解析，验证恰好增加一封信，删除该插入段能逐字节还原全部原明文。运行验证采用该玩家档的隔离第一槽副本、`NoNativeSave`，运行前取得共享 runtime lock，使用现有 smoke runner 的 fixture isolation、未变哈希和清理门；不写入 live AutoCloud。

本次不改 Runtime、Hook 或产品。游戏加载通过只证明格式和读档兼容；玩家首次读信后任务接受、与奥兰多推进仍须如实区分为玩家验收。
