# MoreEquipmentSlots 删档重建后旧 Product-v3 侧车占用新档审查

## Review Header

- Review ID: `20260824-0001`
- Date: `2026-08-24`
- Status: `root cause confirmed; bounded Product-v3 mitigation implemented and runtime-validated; residuals explicit`
- Source:
  - 用户转述：“之前开了一个档能正常用，刚刚删档后开新档 mod 就失效了”；
  - 用户提供的 3 张 DTMAPI 设置页现场图；
  - 只读支持包 `D:\下载\DTMAPI-player-support-20260824-112450-172-3ba73f65_ LAX`。
- Scope: 对齐旧档、删档/新建、首次保存、冷重载和 `scope-revision-regressed`；判断是否为 DTMAPI/MoreEquipmentSlots 代码问题。本轮不启动游戏，不修改玩家存档、配置、侧车或安装。
- Related records:
  - [ISSUE-024: NewGame null slot suppresses Product UI](../../../../debug/issues/ISSUE-024-20260817-moreequipment-newgame-null-slot-ui.md)
  - [ISSUE-026: legacy sidecar player mismatch](../../../../debug/issues/ISSUE-026-20260820-moreequipment-legacy-sidecar-player-mismatch.md)
  - [ISSUE-028: deleted save-slot reuse retains Product-v3 sidecar](../../../../debug/issues/ISSUE-028-20260824-moreequipment-save-slot-reuse-v3-sidecar.md)
  - [MoreEquipmentSlots 1.0.0 publication lifecycle](../../../updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md)

支持包在 `2026-08-24 11:24:52 +08:00` 完成，拷贝 `64` 个 SAVE 文件且零复制失败。关键 DTMAPI 日志 SHA-256 为：

- `latest-20260824-024603183.log`: `E45A2A178F7CB0E016D3CA4AE31612283C87ABC37A80D2AF3B8923FBECD6A19D`；
- `latest-20260824-025722203.log`: `D68A3D5D0AF55D51E3BBBBE87F9FF6FC6E0F63F07AD5AFF34ED538CEB16EEE86`；
- `latest-20260824-031524770.log`: `ABDF2E7B5F6367B73D0D899876D910EAE1F624955D890FC03A707BF5EBE4F682`；
- `latest.log`: `D83994DD23C02BA0EBD81D2EE0E79153CB15928223FEB0B4DF242658C5E5EA9D`。

## Issue 1：删除原生 0 号档并在同一位置建新档后，MoreEquipmentSlots 不再显示三个额外槽

### Original feedback

```text
之前开了一个档能正常用，刚刚删档后开新档mod就失效了
```

### Screenshot transcription

1. 图 1 为 `DTMAPI 设置 0.6.1 -> 状态`：“总体状态：存在错误”；`Mod: 已载入 4 | 被阻止 1 | 已禁用 1`；依赖问题 `0`；当前需要重启 `0`；需要关注为错误 `2` / 警告 `0`。
2. 图 2 为错误页，`10:52:32` 有两条 `DTMAPI.MoreEquipmentSlotsMod` 记录：一条是 `Mod monitor reported an error`，一条是 `Unhandled exception in Save.SaveLoaded event handler`，后者展示 `System.IO.InvalidDataException` 开头。
3. 图 3 为稍后的警告页，`10:57:13` 显示 5 条 `RefactorScaffold` 的 `ReturnedToTitle` 资源生命周期/边界重复刷新警告。

错误页的两条不是两个独立根因：Product 先向自己的 Monitor 写入一次 Error，然后重抛同一异常；Core 的事件隔离层再将它记为 `Save.SaveLoaded` handler 异常。图 3 的 5 条警告发生在返回标题后的重复 hot refresh，与侧车读取失败是两个诊断域。

### Exact timeline

| 时间 | 只读证据 | 含义 |
| --- | --- | --- |
| `09:08:46` | 原生 `slot/index=0 isNewGame=False` 载入，MoreEquipment 没有 storage error | 与玩家“旧档正常”一致 |
| `09:20:30` | 从旧 `slot=0` 返回标题 | Product 清空内存 document，但不删除磁盘侧车 |
| `09:20:37` | `SaveLoaded slot/index=unknown isNewGame=True` | 第一次新游戏进入；没有权威槽号 |
| `09:23:08` | 返回标题；两者之间没有 `SaveSaving/SaveSaved` | 这次新游戏未原生保存 |
| `09:24:53` | 再次 `SaveLoaded slot/index=unknown isNewGame=True` | 第二次新游戏进入，仍无槽号 |
| `09:48:33` | `SaveSaving slot=0` 随后 `SaveSaved slot=0` | 新档第一次可见的成功原生保存 |
| `10:05:25` | 冷载入 `slot=0`，`saveClock=1420`，首次 `scope-revision-regressed` | 新档遇到槽 0 中保留的旧 Product-v3 侧车 |
| `10:22:49` / `10:44:29` | 新档时钟分别为 `2441` / `3717`，仍同样拒绝 | 问题随正常保存/重载持续 |
| `10:52:32` | 全新进程再次以 `saveClock=3717` 拒绝 | 非暖进程残留，也非一次性 UI 绘制问题 |
| `11:01` 至 `11:19` | 多进程/多次载入仍重复同一拒绝 | 磁盘旧侧车一直保留 |

`source-file-inventory.csv` 的原生轮转时间独立印证了这条链：`prev2` 为 `07:04`，`prev1` 为 `09:48`，`prev0` 为 `10:22`，current 为 `10:44`。日志没有 DeleteGame Hook，因此“玩家在 UI 中删档”的动作来自用户确认；但旧档最后载入、两次 NewGame、新档首存和后续槽 0 冷载入均由日志独立证实。

### Root cause

1. 侧车路径只按原生数字槽号分区：

   ```text
   DTMAPI/config/protected-items/equipment-slots/slot-0/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json
   ```

   原生删档只删除/轮转 `SAVE/doloc-save-0.data`；MoreEquipmentSlots 没有订阅或 Patch `DolocAPI/DataPersistenceManager/LocalSave.DeleteGame(index)`，因而旧侧车不知道该槽位的原生 owner 已被删除。
2. `SaveLoadedEventArgs` 已含 `IsNewGame`，但 Product `ModEntry.OnSaveLoaded` 只把 `e.SaveSlot` 传给 `runtime.OnSaveLoaded(int?)`。NewGame 的 `slot=null` 会让 runtime 将 `archiveIndex=-1`、`document=null`、`sidecarPath=""` 后立即返回。这是已记录的 ISSUE-024，也解释了新档首会话为何没有三槽。
3. 第一次原生保存终于带来 `slot=0`，但 `OnSaveSaving` 首先要求已有一个与该槽匹配的非空 `document`。新档内存状态恰好是 null，所以 `09:48` 的 `SaveSaving` 和 `SaveSaved` 对 Product 均为 no-op；它既没有建立新档 document，也没有将旧侧车转为 orphan。
4. 下次以已知 `slot=0` 载入时，Product 才尝试读取槽 0 侧车。格式分类明确为 `ProductV3`，不是 2026-08-20 ISSUE-026 的 flat legacy 文件。
5. Product-v3 验证先比较 `archiveIndex + playerName + customPlayerName`，然后才比较时钟。终端原因是 `scope-revision-regressed`，而不是 `scope-mismatch`，所以可以从代码顺序确定：新旧两个档在 Product 所有现有名称身份字段上完全相同，但旧侧车记录的 `totalGameSeconds` 比新档高出超过 `300` 秒容差。
6. 异常被重抛后，当前会话没有 Product document；UI Hook 虽然安装成功，渲染路径仍因 document 为 null 而无法创建三个额外槽。

因此，这次是 **MoreEquipmentSlots ProductNative 的档位生命周期/侧车 owner 设计缺口**。玩家按原版界面删档并重用同一位置是正常操作；不应由玩家知道或手动管理 DTMAPI 侧车。

### Data-integrity risk beyond the visible failure

`RawScopeRevisionIsCompatible` 只检查：

```text
current.TotalGameSeconds + 300 >= document.TotalGameSeconds
```

所以当前的 fail-closed 不是持久的新旧 owner 隔离。本现场在新档时钟 `3717` 时仍拒绝，说明旧侧车时钟至少大于 `4017`；精确值因支持包没有收集侧车而不可知。

如果玩家继续新档，当新档时钟追到旧值的 `300` 秒以内，而新旧姓名仍相同，现有验证将把同一个旧 Product-v3 文档视为可兼容。若其三槽中有物品，就存在将已删除角色的额外槽/物品挂入新角色的风险。支持包缺少 JSON，故“该玩家的旧三槽是否实际有物品”仍未证实；但允许时钟追平后重新通过的代码路径是确定的。用户随后明确该原生档案是有意删除，旧 Product 数据无需恢复或保留，因此该缺失不再阻塞本现场的清理决定。

### Rejected hypotheses

- **MoreEquipmentSlots 没有加载或被玩家禁用：** `10:51:50` 新进程明确从 Workshop 加载当前 `1.0.0`，安装 `5` 个 Product Hooks，报告 `slotCount=3`，Entry 与 Advanced 交易均成功。被原生 ModManager 禁用的是 `Codex.DolocTownQoL`，不是 MoreEquipmentSlots。
- **当前游戏 build drift 使 UI Hook 失效：** `compiledBuild=24456188` / `installedBuild=24788406` 的 Drift 准入已通过，五个 Hook 已安装；实际终止点是可重复的 Product-v3 存储验证。
- **两条错误表示两个独立异常：** 是同一异常在 Monitor 和 EventManager 两个诊断面的投影。
- **`10:57:13` 的五条 Refactor 警告导致三槽消失：** 警告发生在后来的标题页重复发现/资源刷新；侧车拒绝早已在 `10:05`、`10:22`、`10:44` 和 `10:52` 重复出现。
- **需要用一次饰品包才解锁 Product 三槽：** 饰品包只扩展原生 `passiveItems` 数组并触发 UI 重绘，不会创建 Product document、绑定存档槽或清理旧侧车。在 document 为 null/读取异常的会话中，重绘仍会早退。
- **2026-08-20 安装器直接写坏了这个文件：** 本现场的决定性转折是同一进程内的删档/新游戏/首存，异常格式是早已存在的 Product-v3。Runtime 安装器不拥有 `DTMAPI/config`，也不是这条槽位重用链的作者。

### Technical debt exposed

1. 侧车 scope 没有不可变的“archive incarnation”；槽号、姓名和可追平的游戏时钟不能长期证明是同一个存档 owner。
2. Product 没有删档成功边界，也没有从 active 槽转移到 orphan/quarantine 的权威。
3. `isNewGame=true, slot=null` 没有 pending-scope 状态；首次 `SaveSaving/SaveSaved` 又只接受已加载 document，两段状态机之间存在永久空洞。
4. 现有 Unit 用例证明“侧车时钟比当前档超前 301 秒时拒绝”，但没有测试“同名新档稍后追平时仍永久拒绝”，也没有“删除已占用槽 -> 保留侧车 -> 同槽 NewGame -> 首存 -> 冷载入”的 disposable `ArchiveMutation` 矩阵。
5. 玩家支持包收集器拷贝 SAVE、DTMAPI logs/reports 和少量 Runtime state，但明确没有收集 `DTMAPI/config/protected-items`。因此本次虽能证实格式、身份相等和时钟回退，却不能确定旧三槽、`journal`、`gameplayCandidate` 和精确时钟。这是一个与保存敏感度需同时设计的诊断缺口。
6. 设置页将 Product 自报 Error 和同一 handler 异常列为两个错误，而后续重复 hot-refresh 又产生五条噪声警告；这不是根因，但降低了玩家判断唯一可操作故障的能力。
7. 原生存档页还提供 `DuplicateGame(source, out target)`。当前 Product 不复制对应侧车；因此复制一个含 Product 三槽物品的原生档后，目标档会得到空 Product 三槽。这是同一“数字槽生命周期不完整”家族的独立债务，但安全复制还要处理 source/target scope、未决 journal/candidate 和原生复制成功结果，不适合作为本次删档重建修复的顺手改动。

### Player safety disposition

- 用户已明确旧原生档案是有意删除，且对应 Product 数据不需要恢复或保留；本现场不再要求先保存或检查旧三槽内容。
- 一次性玩家修复应在完全退出游戏后，删除整个 `DTMAPI\config\protected-items\equipment-slots\slot-0` Product 目录，而不是只删 live JSON；否则 `.previous` 或过渡候选仍可能被恢复为旧 authority。若仍有旧的全局 `DTMAPI\config\equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json*`，也应按这次已放弃的旧 owner 一并清理。
- 清理后重新载入当前新档时，既有档路径会为 slot `0` 建立一个新的空三槽内存 document；这可作为当前玩家的有界恢复，但不能代替产品修复。

### Preferred bounded implementation candidate after simplification review

本轮仍是 audit/design-only，不修改 Product/Runtime/玩家数据，不创建 Update。用户明确不把先前草案当作限定方案；重新比较 DeleteGame 清理、NewGame 清理、Core NewGame 槽号 Hook 和 Product 本地解析后，当前首选仍是 **Product 内部的 NewGame reset**，但它只是实现前的首选候选，不是不可变约束。真正开始实现时应建立新的有界 MoreEquipmentSlots Update，并一起闭合 ISSUE-024 与 ISSUE-028。

1. `ModEntry` 把 `e.SaveSlot` 与 `e.IsNewGame` 一起传给 runtime。`SaveLoaded(isNewGame=false, slot有效)` 保持现有既有档载入；`null,false` 继续明确 fail-closed。
2. `SaveLoaded(isNewGame=true)` 优先接受事件中的有效 slot；事件仍为 null 时，使用 Product 已有 native reflection 边界从已经初始化的 `archiveHandle.archiveIndex` 读取确切 index。若两者同时存在但不相等则 fail-closed，不猜空槽。
3. 只有证明该 index 的 native current 文件不存在时，才幂等删除整个 `protected-items/equipment-slots/slot-{index}` Product 目录，包括 live、`.previous`、transition、invalid、migration 和临时残留；目录已经不存在时视为成功，不增加“已删除”标记或第二套事务。随后建立仅内存的三个空槽 document，并标记 `newGamePending`；该分支不得调用 `LoadOrMigrate` 读取旧 owner。
4. 不新增 DeleteGame Hook：旧 Product 数据在没有新 owner 占用槽时只是不可达磁盘残留，真正需要清除的时刻是已确认 NewGame。这样不必判断原生 UI 未检查的 DeleteGame 成功值，也没有 delete Postfix 丢失窗口。也不为本 Product 新增 Core/GameBridge NewGame Hook；共享 `SaveLoaded.SaveSlot` 的完整性是可记录的平台债务，但 Product 已能从同一 native holder 取得精确 index，扩大平台 Hook 不是修复本问题的必要条件。
5. 新游戏开场对白会在 `SaveLoaded` **之后**执行 `open_input_player_name_panel -> archiveHandle.SetPlayerName`。因此 `newGamePending` 的首次 `SaveSaving` 必须重新读取当前 scope，并在尚未存在已提交 Product 文件的前提下，将内存 document 绑定到最终姓名和当前时钟；这不是一般既有档改名迁移。
6. `newGamePending` 期间所有需要 native fingerprint 的路径都允许精确的 `current=missing`，仍记录实际 `prev0..N` 和 `.bak` 哈希。首次 `SaveSaving` 无论三槽是否改变，都复用现有 gameplay-candidate 协议准备一份 candidate；空三槽 candidate 既避免新增第二套“首存 pending”状态，也保证原生已提交但 `SaveSaved` 通知中断时能够冷恢复出新的空 authority。
7. 只有成功 `SaveSaved` 或现有 candidate recovery 对原生提交给出精确证明后，才结束 `newGamePending` 并按既有 promote/finalize 路径发布 Product-v3 document。玩家根本没有发起原生保存就返回标题时，只丢弃内存 document，不写新侧车；保存尝试失败可留下未提交 candidate，下一次保存用同一 `current=missing` preimage 丢弃/重备，下一次 NewGame 也会幂等清除它。
8. 本次不改变 Product-v3 格式、一般冷载身份规则或全局 name mismatch。删档后新档选择同名或异名都由 NewGame reset 覆盖；当前 build 中唯一找到的生产 `SetPlayerName` 调用来自新游戏初始对白，已由第 5 项覆盖。调试命令或未来内容在既有档上改名仍保持 fail-closed，等真实普通玩家路径/证据出现后再单独设计。
9. 若 Product 组件在删档和 NewGame 的全过程都未加载，之后才重新加载，则它错过了唯一正向 owner 边界；简化方案不自动猜测。可见 name/clock mismatch 继续 fail-closed；同名且时钟已追平的旧 v3 与新 owner 在现有字段上无法可靠区分。这是明确保留的非普通路径限制。
10. 原生 DuplicateGame 侧车复制、支持包收集 protected sidecars 和设置页重复错误降噪继续作为独立债务；前两者都需要各自的数据完整性边界，不并入这次最小修复。

### Acceptance checks

1. Focused Unit 覆盖 `null,true` pending state、从 native holder 取得 exact index、事件/native index 冲突、native current 必须缺失、整目录幂等清理，以及 `null,false` 继续 fail-closed。
2. 使用脱离 Steam AutoCloud 的 disposable `ArchiveMutation` 夹具执行：旧档三槽为空与占用两类 -> 原生 UI 删除 -> 同槽同名和异名 NewGame -> 首会话打开装备 UI -> 正常保存 -> 冷重载。
3. 新档在首次保存前可见三个 Product 槽；未发起保存就返回标题不留下任何新侧车，失败的保存尝试至多留下可由精确 preimage 丢弃的 prepared candidate，不得形成 committed authority。
4. 首次命名发生在 `SaveLoaded` 后时，首次 `SaveSaving` 用最终姓名重新绑定未提交的内存 scope；不得触发一般 name-mismatch 放宽。
5. 首次保存前 `current=missing` 的精确 preimage 可供装备、卸下、candidate 准备和失败重试使用；成功 `SaveSaved` 后冷载物品恰好一份，失败窗口不丢失、不复制且不提前提交。
6. NewGame 清理后该 slot 的 Product live、`.previous`、journal/candidate 和过渡残留全部消失；无论旧三槽是否有物品，都不得进入新角色。三槽为空的首次保存也通过同一 candidate 协议发布新的空 v3 authority。
7. 修复候选仍为现有五个 Product Hooks；实机验收需要三槽可见/可用、无 storage/event error、无旧物品串档、正常返回标题和零残留进程。
8. 同名和异名删档重建均覆盖；一般既有档改名、Product 全程未加载、DuplicateGame 路径明确不由本轮验收暗示为已解决。

### 2026-08-25：原生存档内嵌方案重新评估

用户进一步明确，可以接受“关闭/移除 Product 后，额外槽物品留在存档中但暂时不可访问；重新启用 Product 后恢复”的产品语义，不再把自动退回背包/邮件视为必须能力。这个约束变化会改变首选设计：上面的 Product-local NewGame reset 仍是保留现有 Product-v3 侧车协议时的最小补丁，但不再是当前首选的长期方案。

#### 已确认的当前 build 保存模型

对 `24788406_public_F06183` 的只读 native-owner 复核得到以下事实：

1. `ArchiveDataHandle` 使用 `JsonObject(MemberSerialization.OptIn)`；`LocalSave.SaveGame` 将这个对象序列化到临时文件后再 replace/move 为 current，原生 current、`prev*` 和 `.bak` 因而是一个提交域。
2. 原生存档没有公开的通用 Mod-data 字段，也没有 `JsonExtensionData`。直接向存档 JSON 顶层插入未知 `dtmapi` 属性并不稳定：下一次按 `ArchiveDataHandle` 反序列化、再由原版保存时，未知属性不会重新输出。要维持这种顶层属性必须 Patch 加密/解密和序列化边界，侵入面不适合只为一个 Product 建立。
3. 当前 `AgentEquipmentManager.passiveItems` 已是可变长、直接序列化的 `Item[]`；官方 `add_accessory_slot` 也调用 `SetPassiveSlotCount(length + 1)`。因此“游戏绝对无法保存更多饰品槽”并不成立。
4. 但直接把 Product 三槽追加到 `passiveItems` 不是合适的兼容模型：数组长度同时是官方饰品包进度，无法可靠区分官方槽和 Product 槽；Product 缺失时原生 UI 与 `AfterLoadData` 仍会渲染这些项并应用效果；原生 passive 路径只接纳 `ItemPassive`，不能直接保持 Product 当前的帽子、防御和护盾语义。
5. `ArchiveDataHandle.cityData.dialogueManager.variableStorage` 是原生存档已序列化的 `DialogueVariableStorage`。它保存任意 `$` 开头名称的 `string/float/bool` 字典；当前生产源搜索没有发现外部调用 `Clear()`。因此一个 Product 命名空间字符串是比扩原生槽数组或注入未知顶层 JSON 更窄的候选，例如：

   ```text
   $dtmapi_moreequipmentslots_v1 = <仅含 schema 与三个槽 DTO 的紧凑 JSON 字符串>
   ```

这仍是利用原生内容变量容器的 ProductNative 私有适配，并不是游戏官方承诺的通用 ModData API。实现前必须用 disposable archive 证明未知命名空间值在有 Product、无 Product、普通保存和冷载入之间都能保留，且 Yarn program 初始化不会覆盖或清空它；在该证据通过前，此路线是首选候选而非已验证事实。

#### 推荐的稳态语义

1. Product 只在内存中读写上述一个字符串；物品进入/离开三槽时同步更新工作状态和原生 `ArchiveDataHandle` 内的字符串，不立即写任何外部文件。
2. 原生 `SaveGame` 一次性提交背包变化与 Product 字符串。保存失败或未保存返回标题时，两者一起回滚；原生保存已经成功而 DTMAPI `SaveSaved` 通知中断时，字符串也已经随 native current 提交，不再需要 fingerprint/candidate 猜测。
3. 新档创建的是新的 `ArchiveDataHandle`，没有该 key，天然得到三个空槽；无需根据数字槽删除旧 Product 目录，也不依赖 `SaveLoaded.SaveSlot` 在 NewGame 时是否为 null。
4. `DuplicateGame` 读取完整 archive JSON、只改 archive index 后写入目标，因此内嵌值会随原生复制；删档把原生 current 移到 `.bak`，旧 Product 数据与旧档一起离开 current，新建同槽不会继承；改名不再是存储身份变更。
5. Product 禁用或卸载时，原版只保存一个不解释的字符串，Product UI、效果和 Hook 不存在；重新启用后再解释并恢复三槽。若玩家要永久卸载，应在仍启用时先卸下全部物品，或以后提供一个显式“卸下全部并保存”操作；不再承诺缺失 Product 时自动退回背包/邮件。
6. 内嵌 payload 不再保存 `archiveIndex`、玩家姓名或游戏时钟；物理 archive 本身就是 owner。只保留 schema、三个槽及恢复物品所需的最小状态。不要把这一单 Product 需求升级成公共 `IModSaveData`，除非出现第二个真实消费者并单独完成 SharedNative/Platform owner 审查。

#### 现有玩家迁移

现有 Product-v3/legacy 侧车不能直接删除。首个迁移版本应先以当前严格规则完成 journal/candidate 处理并读取有效 committed 三槽；当内嵌 key 不存在时，将其复制到本次会话的 native 内存对象。没有原生保存就返回标题时，旧侧车仍是 authority；原生保存成功后，内嵌 key 成为唯一 authority，`SaveSaved` 只负责幂等归档旧侧车。若 native save 已成功但进程在侧车归档前中断，下次载入只要内嵌 key 有效就优先采用它并忽略旧侧车，不能再次导入。损坏的内嵌值必须 fail-closed，不能静默回退到可能更旧的侧车。

迁移期仍需保留旧 reader/reconciliation 和 Compatibility Host 的旧侧车恢复入口；稳态玩家完成一次正常保存后，Product 不再需要数字槽目录、scope/name/clock、native fingerprint、gameplay candidate、DeleteGame/DuplicateGame Hook 或普通 orphan recovery。这里减少的是长期协议复杂度，不是省略一次性迁移安全。

#### 更新后的验收边界

实现前先做一个最小可行性尖刺，不发布、不改玩家档：在 AutoCloud 隔离的 disposable archive 写入命名空间字符串，分别证明 Product 在场保存、Product 缺席的原版载入再保存、重新启用、原生复制、原生删除后同槽新建、current/prev/bak 轮转均符合预期。通过后再实现 Product-v3 -> embedded 的一次性迁移，并跑正常保存、无保存回滚、保存通知中断、帽子/护盾状态、官方饰品包前后解锁与冷重载矩阵。若命名空间值在无 Product 保存后丢失或被 Yarn 初始化清理，则放弃该容器，退回上面的 Product-local NewGame reset；不得转而无证据 Patch 整个原生序列化器。

### 2026-08-26：小修正实施与验证回填

用户决定先保留当前外挂侧车提供的禁用 Product 物品回退能力，把原生
archive 内嵌方案交给后续玩家投票；因此本次实际采用上文的
Product-local NewGame reset，生命周期由 Update `20260826-0001` 管理。

MoreEquipmentSlots `1.0.1` 在 `isNewGame=true` 时从已初始化的原生 holder
取得确切 archive index，要求 native current 尚不存在，删除该 index 的完整
Product 目录，再创建三个仅内存空槽。首次 `SaveSaving` 重新取得命名后的
scope，并用现有 gameplay-candidate 协议提交；既有档的 name/clock 校验、
Product-v3 格式和五个 Product Hooks 均未放宽或增加。

自动测试覆盖同名/异名新 owner、已占用旧侧车及过渡残留、事件/native
index 冲突、native current 已存在拒绝、整目录清理、首次空槽保存和冷载。
实机隔离 archive index `11` 在 `GAME-SMOKE/20260826-022728` 通过
NewGame 清理和第一次原生保存，随后 `GAME-SMOKE/20260826-023044` 作为
既有档冷载通过；后者在 `NoNativeSave` 模式证明原生档和 committed 侧车
在清理前未变化，且两次运行均无 storage/event error、fatal 或残留进程。

本 Review 的原始根因不变；普通玩家“删档后同槽新建”的已加载 Product
路径已缓解。若 Product 在删除和 NewGame 全程都不在场，它仍会错过唯一
正向边界；DuplicateGame、一般既有档改名以及原生内嵌迁移继续作为独立
债务。首次会话 equipment UI 未在本次 smoke 中留截图，因此 ISSUE-024
与 ISSUE-028 均记为 `mitigated`，不记为 `verified`。
