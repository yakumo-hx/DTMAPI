# MoreEquipmentSlots 旧侧车玩家名不匹配审查

- Review ID: `20260820-0001`
- Date: `2026-08-20`
- Status: `root cause classified; player artifact inspection pending`
- Scope: 玩家 `legacy-scope-mismatch:player-name-mismatch` 报错、与当日 Runtime 安装器更新的因果边界、旧额外槽物品保护
- Source: 玩家通过用户转交的单条 DTMAPI 错误和时间背景
- Current Product lifecycle: [20260811-0001 MoreEquipmentSlots 1.0.0 direct replacement](../../../updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md)
- Current installer lifecycle: [20260820-0001 Runtime installer 0.6.1 reliability/UX](../../../updates/2026/20260820-0001-runtime-installer-061-reliability-ux.md)
- Related Debug: [ISSUE-021](../../../../debug/issues/ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md), [ISSUE-025](../../../../debug/issues/ISSUE-025-20260820-runtime-installer-receiptless-residue.md), [ISSUE-026](../../../../debug/issues/ISSUE-026-20260820-moreequipment-legacy-sidecar-player-mismatch.md)

本记录按玩家反馈顺序区分已证事实、源码解释、安装器因果和待取证项。本轮只读审查当前源码与 Steam 订阅包，并在临时假游戏目录执行玩家包矩阵；没有启动游戏、修改真实安装、读取或写入玩家存档，也没有处理玩家侧车。

## 问题 1：更新安装器后，0 号档加载 MoreEquipment 旧侧车时报玩家名不匹配

### 原始反馈

```text
[Error :DTMAPI Bootstrap] 2026-08-20 15:02:25.403 +08:00 [Error] [DTMAPI.MoreEquipmentSlotsMod] MoreEquipmentSlots storage load failed closed archive=0; playerIdentityPresent=True; saveClock=11433; sidecar=E:\SteamLibrary\steamapps\common\Doloc Town\DTMAPI\config\protected-items\equipment-slots\slot-0\equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json; error=System.IO.InvalidDataException: The flat legacy equipment-slot sidecar could not be converted without data loss: legacy-scope-mismatch:player-name-mismatch
at DTMAPI.MoreEquipmentSlots.EquipmentSlotDocumentStore.MigrateLegacy (...) [0x00029] in <4268920ba1014f08b2622dc0909301dc>:0
```

- 用户补充：此前没有该报错，当天只更新了 DTMAPI 安装程序。
- 图片转写：无截图。

### 报错字段的精确含义

- `archive=0`：当前加载的是原生零基 0 号档，即玩家 UI 的第一个档位。
- `playerIdentityPresent=True`：当前原生档案能读到非空 `customPlayerName` 或 `playerName`；它只表示当前身份存在，不表示与旧文件相同。
- `saveClock=11433`：当前档案 scope 的游戏时钟是 `11433` 秒。
- `sidecar=...slot-0\...json`：Product 在 0 号档专属路径发现了一个文件。
- `flat legacy`：该文件不是当前 Product-v3 格式，而是仍需迁移的受支持旧 flat 格式。
- `player-name-mismatch`：旧文件内优先取 `customPlayerName`、否则取 `playerName`；当前档也按相同优先级取名。两者至少一方为空或不区分大小写仍不相等。该报错已通过前面的 `storageScope=slot-0` 与 `archiveIndex=0` 检查，所以问题集中在人物身份，不是槽号文本。
- 模块 MVID `4268920ba1014f08b2622dc0909301dc` 与当前 Steam MoreEquipmentSlots `1.0.0.0` DLL 精确相同；该 DLL SHA-256 为 `699E95BC05E79F67EE45D83C89D8119EB2BE723FF342CF2DA0CD0A2C7DBC8E31`。异常来自当前发布 Product，不是安装器内的一份未知 MoreEquipment DLL。

### 数据安全结论

这是有意的 fail-closed：若仅凭同一个 `slot-0` 路径就迁移，旧角色的三个额外槽及其物品可能被错误绑定到当前角色，造成串档、复制或遗失。

`TryConvert` 在玩家名不匹配时返回失败；`MigrateLegacy` 随即抛异常。精确旧文件备份、Product-v3 写入和迁移发布都在这一步之后，因此从该堆栈本身可知：

- 当前迁移没有成功；
- 原 flat 文件没有被本次转换覆盖；
- 没有发布一个冒充成功的 Product-v3 document；
- 当前会话的 MoreEquipment document 没有建立，三个 Product 槽和其中状态不能按正常路径载入。

不能仅凭这条错误判断旧文件属于“当前角色改名之前”还是“已经删除/替换的另一角色”，也不能判断三个旧槽是否为空。故不得直接删、改名、编辑 JSON 或强行改 `playerName`。

### 最可能的数据来源

按当前证据，以下来源均可能，尚不能排序为唯一根因：

1. 玩家删除或覆盖了 UI 第一档并新建另一角色，但 `DTMAPI\config` 下按槽号保存的旧 flat sidecar 被保留。
2. 玩家给同一角色改名，而旧格式把名字作为身份证明的一部分。
3. 玩家复制、云恢复或替换了原生存档，但本机 DTMAPI 配置来自另一时间点或另一机器。
4. 机器上曾有多个 Doloc Town 安装；当天安装器把 Runtime 安装到当前 Workshop 包所在 Steam library 的 `E:\SteamLibrary` 游戏副本，因而第一次让该副本里休眠的旧 config 与当前 LocalLow 存档相遇。
5. 旧 sidecar 曾被手工复制到 `slot-0`。当前错误不能证明是谁创建或移动了它。

需要安装输出中的最终 `GameDir`、更新前后的完整启动日志和旧 sidecar 元数据，才能区分这些路径。

## 与 2026-08-20 安装器更新的因果判断

### 已排除的直接因果

- 当日 Steam Runtime 更新只改变七个安装器/调度脚本；五个 Runtime DLL、Compatibility Host、`release-manifest.json`、版本和 MoreEquipment Product 包没有改变。
- Runtime 安装事务只拥有 `BepInEx\plugins\DTMAPI`、`DTMAPI\tools`、`DTMAPI\components`、`release-manifest.json` 和 `install-state.json`。`DTMAPI\config`、日志、报告、Mod 和存档在安装/卸载边界外并被保留。
- 当前 Steam 订阅包 `D:\Steam\steamapps\workshop\content\2285550\3743016467` 再次通过 `dtmapi-workshop-release-audit`：Windows PowerShell `5.1.26100.9168` 九个脚本解析为零错误；missing/empty install 按预期退出 `1`；临时有效游戏 install/check/collect/uninstall 为 `0/0/0/0`；卸载后 check 按预期为 `1`；blocker 为 `0`。证据位于 `tmp/test-runs/runtime-installer-20260820-sidecar-report-audit/DTMAPI Workshop Audit 20260820-173201/Results/stress-summary.md`。

因此，安装器没有创建这个 legacy sidecar，也没有把它改成错误玩家名；“安装器脚本直接写坏 MoreEquipment 数据”被当前源码、发布边界和包矩阵否决。

### 仍可能的间接触发

安装器更新可以改变“何时看见问题”，不能改变“旧文件属于谁”：

- 安装/修复后 Runtime 重新开始正常加载 Product；
- 玩家在重启后首次加载了 0 号档；
- 新安装器的有界路径选择命中了 `E:\SteamLibrary` 当前游戏副本，而旧副本或旧 Runtime 此前没有读到这棵 config；
- 安装器按设计保留 config，使一个此前休眠的身份冲突没有被删除，而是在 Product 真正读取时安全暴露。

这些解释与“今天才出现”一致，但都需要玩家完整日志和安装摘要确认。时间先后本身不足以证明安装器制造了 mismatch。

## 玩家当前安全处置

在确认旧文件归属和槽内容之前：

1. 不要删除、移动、重命名或手改报错 JSON，也不要把旧文件里的玩家名直接替换成当前名字。
2. 不要用 Debug/InstantSave 试图“刷新”它；最好退出该档且暂不继续保存，避免把原生档案继续推进而让归属判断更复杂。
3. 先复制保存以下证据：完整 `latest.log` 及相邻轮转日志；安装器最终摘要（含 `GameDir`）；报错 JSON 及同目录的 `.previous`、`.legacy-migrations`（若存在）；全局旧文件 `DTMAPI\config\equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json`（若存在）。复制只用于备份/审查，不在原路径操作。
4. 询问玩家：第一档是否删除重建、角色是否改名、是否迁移过 Steam library/电脑、是否恢复过云存档、是否存在多个游戏目录。
5. 审查 JSON 时至少比较 `storageScope`、`archiveIndex`、`playerName`、`customPlayerName`、`savedTotalGameSeconds`、三个 `slots`、`journal` 和 `gameplayCandidate`。含人物名的文件应私下传递或先做隐私处理，但不能把影响身份判断的字段从诊断副本中删除。

只有精确证明“合法同一角色改名”或“旧角色/孤儿状态应恢复”后，才能设计对应的重绑定或 owner/orphan recovery。空槽也必须先证明为空且没有 journal/candidate，不能仅凭文件年代推定可删。

## Implementation Record Decision

本轮为 audit-only：新增本 Review 和 ISSUE-026，不修改 Product/Runtime/安装器，不创建 Update。若后续决定增加身份恢复、删档联动或玩家工具，应先建立有界实现 Update，并单独验证正常保存、NoNativeSave 回滚、改名、删档重建、复制/云恢复以及 owner/orphan recovery；不得把放宽名字比较当作安全修复。
