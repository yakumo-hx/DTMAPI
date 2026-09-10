# HuijiWiki favorability rewards, Mint Oil, electricity, and stale-copy audit

- Date: 2026-08-23
- Review Status: `recorded`
- Scope: current Chinese HuijiWiki favorability-reward conditions, Mint Oil use, the Electricity/Power Control Room bug note, and obsolete 0.94/0.95 “current version” prose
- Source request: trace the current public game's real event chains, code, and extracted configuration for Wiki authoring
- Game baseline: Steam build `24788406`, branch `public`, capture `24788406_public_F06183`
- Public Wiki writes: none

## Review outcome

All requested reward-delay and trigger corrections are supported by the current public game's serialized mission graphs and email configuration. The current online pages still omit or mislabel the stated conditions.

One terminology correction is necessary before applying the changes: the native event graphs use internal favorability levels, while the UI renders two internal levels per heart icon. Internal level 2 is one full displayed heart, level 3 is one and a half displayed hearts, and the ordinary maximum level 10 is five full displayed hearts. Therefore “飞廉十心” should be written as “内部好感等级 10（界面 5 颗完整心／好感上限）”, not as ten visible hearts.

The Mint Oil and Electricity corrections are also supported. The obsolete-version cleanup is justified as an editorial freshness correction. For 烤鱼 specifically, the current page says “截至版本0.94.10”, not literally “当前版本”; it is still a dated claim that needs revalidation or historical labeling rather than being presented as current reference prose.

The Claude trace exposed one additional deterministic page error: the follow-up Town Hall event checks the inclusive hour range 6 through 17, so the currently displayed `6:00–19:00` range is not code-faithful. A precise range is `6:00–17:59` under the game's hour-only check.

## Authoritative local material

- Capture authority: `references/doloc-town/reverse/builds/24788406_public_F06183/full-baseline-inventory/portable-full-capture-summary.json`
- ILSpy decompile: `references/doloc-town/reverse/builds/24788406_public_F06183/decompiled/Assembly-CSharp`
- AssetRipper-generated configuration: `references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas`
- Main favorability graph: `references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/GameDatabase/GU/GU_mission_chain/drive_chain/npc_favorability.asset`
- Mody favorability quest graph: `references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/GameDatabase/GU/GU_mission_chain/favorability_quest/mody_favorability4_mission.asset`

These materials remain local reverse-engineering evidence. This review records graph node identifiers, event types, conditions, configuration fields, and conclusions without redistributing the official assembly or copied method bodies.

## Favorability terminology and timer semantics

- `Liking.LikingLevel` is the integer quotient of the stored value divided by `liking_ceiling`; the current ceiling is 100 (`Liking.cs:19`, `settings_tbglobalparameter.json:22`).
- `FavorabilityHeartViewer.Render` allocates one icon per two levels, fills icons from `likingLevel / 2`, and uses a half-heart sprite for an odd level (`FavorabilityHeartViewer.cs:32-45`). The configured upper favorability level is 10 (`settings_tbglobalparameter.json:25`).
- `LikingManager` emits `NPC_LIKING_RISE` when the integer internal level increases (`LikingManager.cs:118-126`).
- A `DAY_PASSED` event is emitted once when the game changes day (`ArchiveDataHandle.cs:736-740`). A graph listener with counts 1, 2, or 3 therefore represents that many day transitions.
- `WAKE_UP` is emitted by the wake-up path (`DolocAPI.cs:4351-4355`). Where a reward graph explicitly chains `DAY_PASSED` and then `WAKE_UP`, both conditions are required.

## 1. Favorability reward acquisition conditions

### 1.1 Claude: “多出来的种植盆” and the following story heading

Online page inspected: <https://doloctown.huijiwiki.com/wiki/克劳德>

The current page puts “多出来的种植盆” under `半心事件` and the following Town Hall story under `一心事件`. Both headings are one half-heart too early.

- Main-graph node 26 listens for `NPC_LIKING_RISE` for `claud`, with the additional condition `likingLv >= 2`.
- Node 26 connects to node 27, which listens for `DAY_PASSED` with `count = 1`.
- Node 27 connects to action node 28, which sends `claud_favorability1` once.
- `email_tbemail.json:2699-2722` identifies that email as “多出来的种植盆” and gives five `plantbasin_fungus` items.

Internal level 2 renders as one full heart, so the acquisition condition is: reach one heart, then pass one day.

The following story is main-graph node 14. It requires entering `镇政厅`, `likingLv >= 3`, and an inclusive hour check from 6 through 17. Internal level 3 renders as one and a half hearts. Its Wiki heading should be `一心半事件`; the code-faithful time is `6:00–17:59`, not the current `6:00–19:00` (`DialogueTask_HourCheck.cs:49-59`).

Recommended Wiki wording: “好感度达到一心后，经过一天收到邮件‘多出来的种植盆’。” Put the next Town Hall story under “一心半事件”.

### 1.2 Sacco: “回执邮件”

Online page inspected: <https://doloctown.huijiwiki.com/wiki/萨科>

The current page puts “回执邮件” under `半心事件`. The graph does not use a half-heart threshold:

- Main-graph node 87 listens for `WAKE_UP`, with `sacco likingLv >= 2`.
- Node 87 directly branches to action node 102, which sends `sacco_favorability1` once.
- `email_tbemail.json:3381-3404` identifies the message as “回执邮件”.

Internal level 2 is one full displayed heart. The reward is received on the next wake-up after the one-heart threshold is satisfied. The section should be `一心事件`, not `半心事件`.

### 1.3 Backup Power

Online page inspected: <https://doloctown.huijiwiki.com/wiki/备用电源>

The current page says the email arrives after the displayed five-heart maximum is reached, but omits the room-entry story and delay.

- Main-graph node 179 listens for `ARRIVE_ROOM_CITY` with room `种子店` and `villain likingLv >= 10`. Its graph tag identifies the Saint-appearance story.
- Node 179 connects to node 180, which listens for three `DAY_PASSED` events.
- Node 180 connects to action node 181, which sends `villain_favorability10` once.
- `email_tbemail.json:3673-3704` identifies the message as “名字确定感谢！” and includes `backup_power`.

The complete condition is: reach the favorability maximum (internal level 10, displayed as five full hearts), enter the Seed Shop to trigger the required story, then pass three days. The graph does not add a separate `WAKE_UP` listener after those three day transitions.

### 1.4 Conductor's Pocket Watch

Online page inspected: <https://doloctown.huijiwiki.com/wiki/列车长的怀表>

The current page mentions maximum favorability and prior events but omits the two-day delay.

- Main-graph node 18 is Claude's preceding Town Hall story and requires `claud likingLv >= 8`.
- Node 18 connects to node 19. Node 19 additionally requires `claud likingLv >= 10` and listens for two `DAY_PASSED` events.
- Node 19 connects to action node 21, which sends `claud_favorability10` once.
- `email_tbemail.json:2725-2748` identifies that email as “列车终站” and gives `conductor_pocket_watch`.

The code-faithful summary is: after the preceding Claude event and maximum-favorability condition are satisfied, pass two days before receiving the mail.

### 1.5 Thruster Radiator

Online page inspected: <https://doloctown.huijiwiki.com/wiki/推进装置散热器>

The current page says the reward follows completion of “飞向宇宙” but omits both subsequent listeners.

- In `mody_favorability4_mission.asset`, node 5 completes on `COMPLETE_DIALOGUE` for `mody_favorability4_actsk` and is marked as the end event node.
- Node 5 connects to node 6, which listens for three `DAY_PASSED` events.
- Node 6 connects to node 7, which listens for one `WAKE_UP` event.
- Node 7 connects to action node 8, which sends `mody_favorability10_end`.
- `email_tbemail.json:2575-2598` identifies the message as “意外的收获” and gives `thruster_radiator`.

The reward therefore requires mission completion, three day transitions, and the ensuing wake-up event. In the normal sleep path, elapsed time and its day-change events are processed before `OnWakeUp` broadcasts `WAKE_UP` (`SleepUiState.cs:119-132`), so this means mail on waking after the third day transition, not an extra fourth day. The wake-up is an explicit graph condition rather than merely the usual moment when mail happens to be checked.

### 1.6 Tank Caller and Small Statuette

Online pages inspected: <https://doloctown.huijiwiki.com/wiki/战车呼叫器> and <https://doloctown.huijiwiki.com/wiki/小雕像>

The current pages omit the post-story day delays.

For Tank Caller:

- Main-graph node 226 is Zenis's level-10 story, triggered by entering `多洛可商店街` during its configured hour range.
- It connects to node 229, which listens for one `DAY_PASSED` event, then action node 230 sends `zenis_favorability10`.
- `email_tbemail.json:2466-2489` identifies that email as “备用钥匙” and gives `zenis_key`, the Tank Caller item.

The missing wait is one day after the required story.

For Small Statuette:

- Main-graph node 36 is Orlando's level-10 story, triggered by entering `泽尼瑟酒馆` during its configured hour range.
- It connects to node 37, which listens for two `DAY_PASSED` events, then action node 38 sends `orlando_favorability10`.
- `email_tbemail.json:3355-3378` identifies that email as “羁绊的象征” and gives `statuette`.

The missing wait is two days after the required story.

## 2. Other deterministic errors

### 2.1 Mint Oil has a mission use

Online page inspected: <https://doloctown.huijiwiki.com/wiki/薄荷油>

The lead currently says that, as of version 0.95.12, Mint Oil has no use other than increasing the value of Peppermint. The same page's task section already contradicts that claim.

`mission_tbfactionmission.json:2610-2658` defines Cerro Rico faction mission `cerro_rico_cellar_upgrade3`, titled “极限利用”. Its required-item list includes `peppermint_oil` with `item_count = 20` (`mission_tbfactionmission.json:2634-2636`).

The “没有用” sentence is false and should be removed. Recommended evergreen wording: “薄荷油可用于里科山阵营交易‘极限利用’，需提交 20 个；也可出售或送礼。”

### 2.2 Electricity's Power Control Room bug pointer is obsolete

Online pages inspected: <https://doloctown.huijiwiki.com/wiki/电力> and <https://doloctown.huijiwiki.com/wiki/电力控制室>

The Electricity page still directs readers to a “possible Bug”. The Power Control Room page itself now explains that the old indoor wind/solar placement behavior belongs to an obsolete version and that formal-release placement/effect behavior changed. The current code and configuration agree with the newer explanation:

- `equipment_tbequipment.json` gives both `solar_generator` and `wind_generator` `env_type = 2` (record starts at lines 650 and 8150).
- `EquipmentEnvType.cs:3-8` maps value 2 to `OUTDOOR`. `EquipmentBuilder.cs:75-87` and the corresponding builder-tip checks enforce the indoor/outdoor environment constraint.
- `room_tbroomeffect.json:30-36` gives `power_control_compartment` `power_generation_addition = 0.4`.
- `building_tbbuilding.json:1383-1390,1517-1520` binds that room effect to the Power Control Room and describes the interior/adjacent generator benefit.
- `ElectronicComponentGenerator.cs:26-44` uses the current room effect for an indoor generator. For an outdoor generator, it scans buildings at covered and adjacent positions and applies the maximum matching building room-effect bonus.

Thus the main Electricity page's generic “可能存在 Bug” pointer is stale. It should be removed or replaced with a clearly historical note: wind and solar generators are now outdoor-only, and outdoor generators that overlap or adjoin the Power Control Room's checked footprint can receive its 40% bonus.

### 2.3 Obsolete 0.94/0.95 “current version” prose

Online pages inspected: <https://doloctown.huijiwiki.com/wiki/生肉>, <https://doloctown.huijiwiki.com/wiki/瞬发炸弹>, <https://doloctown.huijiwiki.com/wiki/腌笃鲜>, and <https://doloctown.huijiwiki.com/wiki/烤鱼>.

The current rendered copy contains:

| Page | Current dated wording pattern | Audit classification |
| --- | --- | --- |
| 生肉 | “当前版本版本0.94.10……” | Obsolete “current version” label |
| 瞬发炸弹 | “…在当前版本版本0.94.10仅是……” | Obsolete label plus subjective utility judgment |
| 腌笃鲜 | “当前版本版本0.94.10……” | Obsolete “current version” label |
| 烤鱼 | “截至版本0.94.10，其分类属于烘焙” | Historical cutoff, not literally “current version”, but not a current-data statement |
| 薄荷油 | “截至当前版本版本0.95.12……” | Obsolete label attached to a factually false utility claim |

A signed-in Wiki search for the paired terms `“当前版本” “版本0.94”` returns 生肉, 瞬发炸弹, and 腌笃鲜 as direct stale-copy matches. It also returns 畜牧与渔业 through a broader term match, which should be reviewed separately rather than automatically edited. The paired 0.95 search directly exposes 薄荷油; version-history pages are expected search noise and should not be changed merely for containing the words “当前版本”.

Recommended cleanup rule:

1. Remove “当前版本 + old version number” from evergreen item pages.
2. Revalidate the underlying claim against current configuration before retaining it.
3. If a statement is intentionally historical, move it to a history/trivia context with past-tense wording and a specific version boundary.
4. Delete unsupported subjective conclusions such as “只是玩具” or “没有用” instead of periodically replacing one stale version number with another.

## Validation and limits

- Read the required project, planning, reference, debug, review, and document-governance authorities before tracing.
- Confirmed the current rendered Wiki text and search results through the user's signed-in Chrome session.
- Parsed the current public capture's serialized mission graphs and checked their connections, listener event types, counts, additional conditions, and email actions.
- Cross-checked reward email identifiers against `email_tbemail.json` and the Mint Oil requirement against the current faction-mission configuration.
- Traced favorability display conversion, day-change and wake-up broadcasts, equipment environment restrictions, and Power Control Room generation bonuses in the current decompile.
- No game launch was needed: these are deterministic graph/configuration and live-page facts, not runtime acceptance claims.
- No Wiki page was edited or saved.
