# 星辰神帝玩家档旧城恢复信定点补发

- Update ID: `20260905-0001`
- Date: `2026-09-05`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, runtime`
- Runtime Validation: `passed`
- Related Issue State: `mitigated`
- Area: `support/player/save/mission/email/pinpoint-repair`
- Source Request: 核实玩家旧城信触发条件；确认缺失后直接备份并补发官方信件。
- Source Review: [20260905-0001](../../reviews/manual-qa/2026/20260905-0001-xingchen-ruined-city-save-repair.md)

## Scope

已为下载目录中识别的星辰神帝第一槽存档生成修复文件。沿用 [20260809-0001](../../archive/updates/2026/20260809-0001-ruined-city-player-save-repair.md) 的原生邮件补发边界，在 `farmData.emailManager.emails` 首部增加一封 `ruinedcity_continue`。源档保留，另附备份；未重新安装 Runtime，也未写入用户实时 archive。测试期间的官方 Mod 配置由现有 runner 临时隔离并逐字节恢复。

## Changed Files

- 本 Update、对应 Review 和 2026-09 月台账行。
- 忽略目录中的本次定点修复脚本及验证报告。
- `D:\下载` 下玩家专用的派生存档、ZIP 和原档备份。
- active smoke 行 `RUINEDCITY-XINGCHEN-REPAIR-LOAD-20260905`。

## Validation

- 源档的湿地主线、跨日节点、前置对话访问和恢复信缺失已核实，细节由 Review 持有。生成脚本再次校验源 SHA-256、槽位、版本和全部准入条件，拒绝重复补发。
- 原始加密往返、原档/日志备份回读、修复文件与最终 ZIP 解密解析、邮件唯一性及全部无关明文字节不变验证通过。
- `GAME-SMOKE/20260905-210929` 使用交付文件的原槽位 `0`，未改 index 或版本。在当前 Steam 安装的 build `24966367`、现有 Runtime、CoreOnly 隔离配置下，`SaveLoaded slot=0; isNewGame=false`，`RunStatus=Passed`。
- 当前 Unity、BepInEx、DTMAPI 三份日志中反序列化错误、NullReferenceException、TargetInvocationException、Error/Fatal 和 Exception 模式共 `0` 次匹配。
- NoNativeSave、fixture isolation、archive/committed-sidecar 清理前未变、QA 清理、官方 Mod 配置恢复和进程退出门均通过；无 archive 备份/写回，测试副本已由 runner 清理，共享锁已释放。这里的无例行备份是游戏 smoke 语义，与交付前显式创建的源档回滚 ZIP 分开。
- 没有修改 Runtime、Hook 或产品，不需要源码构建和完整 Release 测试。文档治理检查在本记录和 smoke 行收尾后通过（`7715` 项）；首次检查指出缺少四个标准章节，已在本 Update 中补齐。

## Evidence

- 本地私有修复脚本和报告：`docs/debug/evidence/PLAYER-SAVE-REPAIR/20260905-xingchen/repair.ps1`、`repair-report.json`。
- 原始明文 `1914277` bytes；只增加 `338` bytes，邮件数 `152 -> 153`。从最终 ZIP 重新解密并删除该插入段，可逐字节还原完整原文。
- 修复 archive SHA-256：`901520F9B3138895B9995E23DD04D2BCC2B53F0474E3352F7D3A7EA341B7249D`。
- 修复 ZIP：`D:\下载\多洛可_ 星辰神帝-旧城信修复.zip`，SHA-256：`A3E31E85195813FC4C0F0D6AC455211305505A28B702C20B2200ADA17FB45213`。
- 原档及日志备份：`D:\下载\多洛可_ 星辰神帝-修复前原档备份-20260905.zip`，重新开包核对两文件哈希通过；原件 length/hash/mtime 均未改变。
- 游戏证据：[result.json](../../debug/evidence/GAME-SMOKE/20260905-210929/result.json)、[清理前存档未变证明](../../debug/evidence/GAME-SMOKE/20260905-210929/player-save-unchanged-before-cleanup.json)、[进程退出证明](../../debug/evidence/GAME-SMOKE/20260905-210929/process-exit-before-state-restore.json)。副本全程 SHA-256 与交付 archive 相同。

## Rollback Notes

原 `D:\下载\多洛可_ 星辰神帝\doloc-save-0.data` 保留不变；修复文件作为独立交付。玩家安装前也应保留其本机当前第一槽存档，需回滚时恢复原文件即可。

## Follow-Up

首次阅读《关于旧城市废墟》会由原生附件接受主线，之后找奥兰多继续；玩家侧实际推进尚未确认。交付说明标明这是第 170 天快照，若玩家此后另有保存，替换将回到提交时的状态。
