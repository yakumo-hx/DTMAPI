# 20260908-0003: 玩家支持按槽采集与受限存档修复工具

## Metadata

- Update ID: `20260908-0003`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 工作空间建设获批工程包；复用 [20260809-0001](../../archive/updates/2026/20260809-0001-ruined-city-player-save-repair.md) 与 [20260905-0001](20260905-0001-xingchen-ruined-city-save-repair.md) 的已证实补信边界，不新建通用编辑协议或重复根因 Review。

## Summary

维护者现在可以用 Inspect / Prepare / Verify 处理已知旧城缺信模式，固定 slot0、案件和源哈希，在全新目录生成修复件并复核最终文件字节，再交现有原子恢复包。存档采集器增加可选原生槽索引，保留不传参数时的完整 SAVE / BAT 入口。离线验证、真实派生件读档和首次读信任务接受均已通过。

## Changed Files

- `tools/scripts/invoke-player-save-repair.ps1`：三阶段编排，原件、案件、槽号、profile/格式身份绑定和最终产物复核。
- `tools/scripts/player-save-repair/`：独立 JSON span 读取、标准加密格式适配、已知准入及唯一邮件插入；说明其匹配基线和范围。
- `tools/release/player-save-crash-collector/collect-save-and-crash-logs.ps1`：`-SlotIndex 0–11`、选定槽家族和采集范围记录；缺当前档不会误报完整。
- `tools/scripts/test-player-save-repair.ps1`、`test-player-save-collector-slots.ps1`：必要合成正反例与包装器实际离线集成；原 collector 测试的 PS5.1 解析子进程改用 EncodedCommand，避免引号丢失。
- [玩家支持流程](../../workflows/player-support.md)：采集分流、命令、准入、真实游戏触发和结束条件。公共索引及月表由主建设流程统一同步。

## Validation

- Windows PowerShell `5.1`：修档 `44` 项通过，覆盖两保存版本、root/base 槽身份、任务及装饰节点归属、已有/回收邮件、跨日/对话前提、损坏 JSON、大小写别名、源/profile/case 绑定、生产资源指纹和支持 ZIP 全流程。
- 从最终 `.data` 与 ZIP 各自解密，唯一插入删除后原始 UTF-8 字节完全一致。额外改变数字拼写但保持对象语义一致、并重算外层哈希的反例仍被拒绝。
- 原件 hash/mtime 不变；重复 Inspect 不补发；重复 Verify 复核并复用未变证据。生成的现有 slot0 包在明确标记的合成副本中实际执行“应用 → 幂等重复 → 玩家新保存拒绝”，保留 `.prev0`。
- 指定槽采集 `4` 场景通过：完整、slot0、slot11、仅有 prev 而缺当前档。指定槽不夹带其他 SAVE 内容，相关日志仍收集。
- 原完整 collector BAT 路径矩阵及原 slot0 恢复矩阵通过。首次新测试暴露 PS5.1 需显式加载 `System.IO.Compression`，已补齐；指定槽测试的 ZIP 路径断言改用规范化分隔符。原 BAT 测试的旧引号失效已修复并重跑。
- 合成档只用于离线检查；真实原件按案件身份核对后生成派生件。匹配 GameManager 资源只提供本地格式字段，未复制反编译实现。
- `GAME-SMOKE/20260908-110211` 在实际 public build `25163613` 中读入最终修复副本，原生存档和 committed sidecars 未变。邮箱尚未打开，600 秒人工观察到时关闭；完整结果为 `Failed`，缺少正常终止时的 fixture/QA/OwnerLifetime 清理证明。仅复用读档与文件不变的证据，不把整轮记为通过。后续复用同一 fixture 做首次读信和正常退出。
- `GAME-SMOKE/20260908-112115` 复用同一副本完成首次读信：用户打开邮箱并报告解锁提示，随后直接观察原生《关于旧城市废墟》邮件，以及当前主线“旧城市废墟 → 和奥兰多聊聊”。两张截图与观察收据保留在本轮证据中。通过原生菜单返回主页，进程在期限内正常结束；原生档/committed sidecars 不变，fixture、QA、OwnerLifetime 清理及配置恢复全部通过。最终修复件 SHA 仍为 `901520F9…B7249D`，没有睡觉、保存或写回玩家原件。

## Evidence

真实指定源档准入补查：源 SHA `64FF8956…BEE044` 匹配，但首轮 `Inspect` 返回 `WetlandDecoratorIncomplete`。这是工具的字段归属错误：该档的 `wetland_main@1` 与 `_0`–`_6` 同在 `finishMissions`；`finishDecorators` 保存另外的 decorator ID。匹配 build 的 MissionManager 完成逻辑与 MissionDecorator 身份字段确认这一区别。原历史 Review 只记录这些任务已完成，没有把 `@1` 归入 decorator 字段。现把严格的 `@1` 完成要求移到 `finishMissions`，没有删除前提或修改任务状态。匿名化合成样本同步真实结构，新增“任务 ID 仅出现在 decorators 仍拒绝”的反例。

补查后 Windows PowerShell 5.1 的修档测试 `44` 项通过（`temp/player-save-repair-test-0829dc8362bc4d069a2a64ecd54a3f35/result.json`）。同一真实源档 `Inspect` 为 `Eligible / KnownMissingRecoveryMail`，邮件原为 `152`；实际 `Prepare / Verify` 已完成且原件未变。最终派生 `.data` SHA-256 为 `901520F9B3138895B9995E23DD04D2BCC2B53F0474E3352F7D3A7EA341B7249D`，与此前已交付的修复字节完全一致；最终 ZIP 也经独立解码验证只增加允许的那封邮件。离线结果在 `docs/debug/evidence/PLAYER-SAVE-REPAIR/20260908-engineering/actual-source-offline-results.json`；实际读信结果在 `GAME-SMOKE/20260908-112115/player-mail-observation.json`。

- [修档合成报告](../../../temp/player-save-repair-test-0829dc8362bc4d069a2a64ecd54a3f35/result.json)。合成 JSON 没有世界数据，不能进游戏。
- [指定槽采集报告](../../../temp/player-save-collector-slots-6f948706086d4b639722dfbcf3cba5b9/result.json)。
- 原 collector 回归：`temp/player-save-crash-collector-test-20260908-092527-776-0155fa6c/`；原恢复包矩阵：`temp/player-slot0-recovery-test-20260908-092506-636-956d0cbf/`。
- 本地格式 profile、真实派生件及 fixture 描述：`docs/debug/evidence/PLAYER-SAVE-REPAIR/20260908-engineering/`。profile 留在 ignored 目录，不进入收据、恢复包或 Git；原件、最终派生件和各次观察分别绑定自己的 SHA 与结果。

## Rollback Notes

代码回退不改变任何玩家原件；准备目录可作为案件证据保留。需要回退本案实际修复时，依据仍是本案原始快照，不能换成别人或别槽的数据。恢复包拒绝玩家继续保存后的当前档；不得关闭该保护或把新的当前哈希填进旧收据。普通 NoNativeSave 游戏测试仍遵循 PROJECT 的保存语义。

## Follow-Up

本模式验收完成；没有继续推进奥兰多对话或后续整条任务链。模式外、格式不匹配或原件已改变时只阻塞本模式，不扩大为万能存档编辑器或全游戏重新解包。
