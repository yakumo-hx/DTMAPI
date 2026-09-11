# 20260911-0001: 0.7.0 玩家包同步与双平台发布记录

## Metadata

- Update ID: `20260911-0001`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs, unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: 用户确认验收通过，要求同步两个玩家上传包和三语言描述；随后确认两个版本均已自行发布，要求更新记录。此任务未执行上传，也不产生下一次上传授权。

## Summary

将[0012已验收](20260910-0012-sdk-msbuild-first-release.md)的Windows Runtime r6及多平台r2同步到两个官方上传目录，再按用户中文定稿同步三语描述。用户完成发布后，Steam公开接口和原生订阅清单共同确认两个Runtime 0.7.0条目，下载内容与上传目录一致。SDK仍独立交付，本次发布确认不包括SDK。

## Changed Files

- `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`：Windows r6。
- 同级`DTMAPI_MultiPlatform`：多平台r2；原`workshop.json`分别绑定3743016467及3792681186。
- `tools/release/dtmapi-mod-publish-zh.json`及`tools/release/dtmapi-multiplatform/info.json`：同步用户修订的三语言文案；不改变产品身份及源模板版本约定。
- `tools/release/dtmapi-product-catalog.json`及`current-subscription-manifest.json`：更新两个0.7.0发布及订阅收据；多平台保留`observed061Baseline`，新包的Runtime来源仍为r6 Candidate，不改写成旧Steam来源。
- Catalog校验脚本推进当前发布收据断言；多平台审计增加仅接受已观察准确内容/控制文件收据的`-AllowDeliveredWorkshopControlFile`。安装器本体不变。
- PROJECT、SDK入口、平台候选/状态和安装器边界更新当前发布事实，历史验收结果保持原范围。

## Validation

- PASS：已验收r6/r2 ZIP SHA256及逐条文件长度/hash；阶段目录、发布后完整树均与准确候选相同。Windows共29文件、多平台38文件，分别为28/37个内容文件加一个原Workshop控制文件。
- PASS（初次载荷同步）：原`workshop.json`逐字节保留，两个叶的版本均0.7.0；当时description/steamDescription和简中/繁中/英文描述与同步前相同。旧Windows根目录的多余dispatcher不混入当前准确候选，原件随整叶备份保留。
- PASS：对准确Windows上传叶执行`test-runtime-workshop-installer-061.ps1 -PackageRoot <upload>`，既有完整fake-game/复杂路径/安装检查日志卸载矩阵通过；脚本名061未作为本次输入版本。
- PASS：对准确多平台上传叶执行`test-dtmapi-multiplatform-package.ps1 -SourceKind Candidate -AcceptedPackageRoot <r6> -AllowWorkshopControlFile`，37文件、30,174,998字节、tree `a33ea74be6bd2a79411818b3192e1561c9d3231d05046f6edbf66788464d117e`，PowerShell5.1和WSL Bash语法检查均通过。
- 本次只同步未变的已验载荷，复用0012的Windows/WSL生命周期及实际游戏具名范围，没有重建候选、重跑游戏或将安装结果扩成Steam Deck/CrossOver实机证明。持锁完成同步后释放。
- PASS（后续文案同步）：两个上传叶逐文件SHA256对比均仅`info.json`变化，其余文件、Workshop控制文件及元数据非描述字段不变。三语言均包含对应Workshop路径、QQ联系方式、0.7.0更新及SDK地址；其他产品文案不变。
- PASS（后续文案同步）：使用同参数重新执行多平台结构审计，PowerShell5.1/WSL Bash语法检查通过。当前上传叶是r2的本地元数据后继，37个内容文件、30,179,521字节、tree `31cc70798457c05be20f7ce0e81c0ad3436c8afedca471ee78940d8abbba3fee`；载荷不变，复用初次Windows安装器矩阵。r6/r2候选目录及ZIP未改动。持锁写入后已释放。
- PASS（发布观察）：Steam `GetPublishedFileDetails/v1` 对3743016467及3792681186均返回成功、公开可见且未封禁；更新时间分别为2026-09-11 02:47:45Z、02:48:35Z。网页抓取曾返回旧缓存，最终以公开接口、用户确认及稳定原生ACF/实际下载三者交叉核实。
- PASS（准确下载）：Windows manifest `4856329672073593806`，29文件、4,218,718字节；多平台manifest `4215517148405505341`，38文件、30,179,554字节。两个包逐文件长度/SHA256与上传目录一致，三语言内容随包送达。完整收据见本记录证据目录，排除根控制文件后的内容另行计数。
- 本次多平台下载实际包含33字节的根`workshop.json`，与上传控制文件相同；此前0.6.1不交付该文件的观察不再推广到0.7.0。完整交付38文件与实际内容37文件分开记账，候选ZIP仍不包含控制文件。
- PASS：对准确多平台订阅运行`test-dtmapi-multiplatform-package.ps1 -SourceKind Candidate -AcceptedPackageRoot <r6> -AllowDeliveredWorkshopControlFile`，结构、Runtime/host来源、PowerShell5.1和WSL Bash语法均通过。未重跑安装器生命周期或实际游戏：这些字节与本记录初次同步及0012的已验载荷一致。公开发布和订阅成功不代表本机已安装0.7.0或Steam Deck/CrossOver实机验收。
- 原生ACF同时观察到ActionSpeed的installed manifest变化，仅刷新订阅成员观察，不改变该产品Catalog发布收据或行为验收。
- PASS：订阅控制文件反例保留正确Workshop ID但改变JSON字节，准确收据门拒绝；PowerShell7/5.1 Catalog检查、文档治理及本任务文件`git diff --check`通过。

## Evidence

- `artifacts/pn041/upload-sync-070/` 保存脚本、前后收据、备份、同步及审计结果、原中文描述。
- 已验收来源、实际游戏与安装器证据归0012；本次不重新构建候选或启动游戏。
- `artifacts/pn041/upload-sync-070/descriptions/` 保存三语文本、文案同步前的info及文案源备份、前后文件SHA256、同步结果与多平台结构审计。
- `artifacts/pn041/upload-sync-070/publication/` 保存Steam公开接口响应、稳定ACF副本、订阅/上传逐文件收据、发布记录修改前备份及校验日志。
- 同目录`control-file-negative-fixture`为独立测试副本，保留用于复核“有效JSON但不等于已观察字节”的拒绝证据；未修改实际Steam订阅或上传目录。

## Rollback Notes

旧上传叶完整备份在本次证据目录的`before-dtmapi-windows`和`before-dtmapi-multiplatform`，各有完整收据；临时阶段/旧叶已清理。其他官方MODS、游戏安装、配置、存档及订阅目录不变。失败时恢复本次拥有的上传叶，不动其他产品。

## Follow-Up

两个Runtime 0.7.0版本的用户发布及订阅核实已完成。Catalog继续保持无下一次上传授权；SDK独立发布、本机安装升级、Steam Deck/CrossOver实机验证均不由此次发布观察代替。
