# HuijiWiki fishing, drone, platform, and planting fact audit

- Date: 2026-08-23
- Review Status: `recorded`
- Scope: current Chinese HuijiWiki claims about fishing, drones and assist components, platforms, planting links, and planting-pot anchors
- Source request: trace the current public game's real code and extracted data for Wiki authoring
- Game baseline: Steam build `24788406`, branch `public`, capture `24788406_public_F06183`
- Public Wiki writes: none

## Review outcome

The fishing, drone price/source/effect/slot, and Wiki link or anchor corrections in the source request are supported by the current public game's decompiled code and extracted configuration. The current online pages still contain the identified errors. The Drone page history shows that its current revision was saved on 2026-08-20 at 20:43, but the incorrect rows remain present.

One proposed correction needs narrower wording: `max_support_height = 15` is enforced for the automatically generated supports beneath buildings. The manual platform builder does not read that value and contains no corresponding 15-tile check. The Platform page should therefore not replace “没有高度限制” with the blanket claim “平台高度上限为 15”. A code-faithful statement is that a building's generated support columns may not exceed 15 tiles, while manual platform construction has width, room, ground, occupancy, and affordability checks but no explicit 15-tile platform-height cap.

## Authoritative local material

- Capture authority: `references/doloc-town/reverse/builds/24788406_public_F06183/full-baseline-inventory/portable-full-capture-summary.json`
- ILSpy decompile: `references/doloc-town/reverse/builds/24788406_public_F06183/decompiled/Assembly-CSharp`
- AssetRipper-generated configuration: `references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas`
- Extracted dialogue database: `references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/GameDatabase/Dialogue`

These materials remain local reverse-engineering evidence. This review records symbols, fields, values, and conclusions without redistributing the official assembly or copied method bodies.

## 1. Fishing

Online page inspected: <https://doloctown.huijiwiki.com/wiki/钓鱼>

### 1.1 Bite-check interval and probability

The current Wiki says that the game first chooses a random 3–6 second wait, then checks once per second with a base 15% chance. The native path says otherwise:

- `DolocTown.AgentStateFishingWait.OnEnter` sets the recurring bite timer to `FishingRollInterval * HookingTimeMultiplier` and initializes the chance from `FishingBiteInitProbability` (`AgentStateFishingWait.cs:179-186`).
- `HandleFishOnHook` performs the probability roll only when that recurring timer ticks. A failed roll adds `FishingBiteAdditionalProbability` (`AgentStateFishingWait.cs:105-116`).
- `settings_tbglobalparameter.json:268-272` supplies `2`, `0.1`, `0.15`, `0.7`, and `10` for the roll interval, initial chance, failed-roll increment, pull timing, and energy cost respectively.
- `RedSaw.RSTimer.SetInterval` resets elapsed time to zero and `Tick` fires only after the configured interval is reached (`RSTimer.cs:43-49,67-75`). The first ordinary bite check is therefore after one full interval, not immediately.

The ordinary check sequence is 10%, 25%, 40%, 55%, 70%, 85%, then 100%.

### 1.2 The unrelated 3–6 second timer

The 3–6 second random value belongs to `_animationTimer`, not `_tuCounter`. When it expires, `HandleWaitBehaviours` plays `fishing_wait_blink` with 90% probability or `fishing_wait_yawn` otherwise (`AgentStateFishingWait.cs:34-40,154-160`). It is an idle-animation timer and does not schedule the bite roll.

### 1.3 Island Badge, expectation, response, and energy

- The Island Badge config has `rate = 0.5` and `energy_return = 5` (`player_tbagentequipmentskill.json:368-376`). `AgentEquipmentFunctionIslandBadge.DoExtraConfig` multiplies `hookingTimeMultiplier` by that rate (`AgentEquipmentFunctionIslandBadge.cs:19-22`). It therefore changes the ordinary bite interval from 2 seconds to 1 second.
- With survival probabilities before successive checks of `1`, `0.9`, `0.675`, `0.405`, `0.18225`, `0.054675`, and `0.00820125`, the expected number of intervals is `3.22512625`. The expected wait within `AgentStateFishingWait` is therefore about `6.4503` seconds normally and `3.2251` seconds with the badge.
- A successful bite opens `PullTiming = 0.7` seconds (`AgentStateFishingWait.cs:139-150`). The 10 energy is charged when the player responds with a valid fishing/tool/item input during that window, immediately before entering the fishing minigame or successful pull (`AgentStateFishingWait.cs:61-79`).
- The 5-energy return is narrower than the shorthand “逃脱返还 5 体力”: `FishEscape` is set when the fishing minigame ends in `Failed` (`AgentStateFishingBattle.cs:32-36`), and the badge adds its configured return during the pull state (`AgentStateFishingPull.cs:46-52`). A missed 0.7-second bite window sets the pull result to failed but does not set `FishEscape`, so that timeout is not the refund path.

Recommended Wiki wording: every `2 seconds × hooking-time multiplier`, perform a bite roll starting at 10%; after each failed roll add 15 percentage points. The Island Badge multiplies the interval by 0.5. The expected wait-state duration is about 6.45 seconds normally or 3.23 seconds with the badge. The 3–6 second random timer only controls blink/yawn idle animations. After a bite, the response window is 0.7 seconds; responding costs 10 energy, and a fish that escapes by failing the minigame returns 5 energy when the Island Badge is equipped.

## 2. Drone and assist-component pages

Online pages inspected: <https://doloctown.huijiwiki.com/wiki/无人机> and the linked component/detail pages.

### 2.1 Buying and selling prices

`item_tbitem.json` records the following current values:

| Item | Buying price | Selling price | Record start |
| --- | ---: | ---: | --- |
| 橡皮弹幕 | 5000 | 2500 | line 2472 |
| 自动收割舱 | 5000 | 2500 | line 2564 |
| 芯片溶解器 | 5000 | 2500 | line 2594 |
| 照明灯 | 2000 | 1000 | line 2381 |
| 垃圾内燃机 | 2000 | 1000 | line 2624 |

The Drone page currently shows `15000/3000`, `12000/2400`, `10000/2000`, `3500/1000`, and a crafting/350 entry respectively, so all five summary rows are wrong.

### 2.2 Garbage Engine source

- `store_tbexchangestore.json:1517-1534` places `drone_assist_garbage_engine` in the garbage exchange store at zero gold plus 100 `rubbish`, with `default_unlock = false`.
- `store_tbstoreitemunlock.json:51-54` unlocks that store item through condition type `0`; `StoreItemUnlockType.cs:3-7` identifies type 0 as `ObtainItem`.
- `npc_favorability/lank_favorability.yarn:154-168` gives the item from Lank during the relevant favorability dialogue.

Thus the detailed Garbage Engine page is already substantially correct: the story event grants the item, after which it can be exchanged for 100 rubbish. The incorrect “无人机商店制作 / 350” source is on the Drone summary page.

### 2.3 Burst Core link

The Drone page's Burst Core description is displayed in a second row titled “战术电容”, and that row's actual `href` also targets `/wiki/战术电容`. The navigation list later on the same page has the correct separate `/wiki/爆裂核心` target. This is a real page-link/label error, not a rendering interpretation.

### 2.4 Crow Controller effect

`drone_tbdroneskill.json:9-13` sets the Crow Controller's `power_recv` to `0.05`. `DroneFunctionCrow.OnEnemyDead` charges `PowerRecv * PowerCapacity` (`DroneFunctionCrow.cs:15-22`). The actual effect is therefore 5% of maximum capacity per qualifying drone kill. The Crow Controller page's basic-information box says 5%, while its prose still says 10%; the prose is wrong.

### 2.5 Assist slots

`drone_tbdronestructure.json` gives both `drone_structure_energy` (墨式机械无人机, lines 108-160) and `drone_structure_doloc` (多洛可警用无人机, lines 163-215) slot types `0`, `1`, `2`, and `3`. `ComponentType.cs:3-8` maps those values to Weapon, Engine, Assist, and Chip.

Both frames therefore have one assist slot. The Crow Controller, Collector Helper, and Tactical Capacitor prose claiming that only the Doloc Police Drone has an assist slot is wrong and also contradicts the current Mod-style Mechanical Drone and Drone summary pages.

## 3. Platform and planting links

Online pages inspected: <https://doloctown.huijiwiki.com/wiki/平台>, <https://doloctown.huijiwiki.com/wiki/种植>, and <https://doloctown.huijiwiki.com/wiki/种植盆设备>.

### 3.1 Platform-height boundary

`settings_tbglobalparameter.json:108-113` contains `max_support_height = 15` and the separate platform-width range `2..12`.

- `BuildingBuilder` generates `BuildingSupport`, requires the maximum generated column height to be no more than `MaxSupportHeight`, and rejects confirmation with the invalid-height message when the check fails (`BuildingBuilder.cs:310-318,349-364`). `IBuildingHost` applies the same support-height limit while checking whether a supporting building may be removed (`IBuildingHost.cs:190-200`).
- `PlatformBuilderHelper` uses only the `BuilderPlatformWidth` range plus ground-hit, room, occupancy, and cost checks (`PlatformBuilderHelper.cs:30-38,40-77,80-100`). `IPlatformHost.CheckPlatformValid` only checks that the platform positions are empty (`IPlatformHost.cs:216-219`). A full search of the current decompile finds no manual-platform use of `MaxSupportHeight`.

Conclusion: 15 is a building-support-column limit, not a general manual-platform height limit. This requested correction must be narrowed rather than applied verbatim.

### 3.2 Stone Platform navigation

The Platform page currently renders both “垃圾平台” and “石制平台” navigation links with `href="#垃圾平台"`, even though the document contains distinct `id="垃圾平台"` and `id="石制平台"` anchors. The Stone Platform link should target `#石制平台`.

### 3.3 Planting and planting-pot anchors

The Planting page currently shifts five links one entry backward:

| Visible text | Current target | Correct target |
| --- | --- | --- |
| 木制 | `#简易种植盆` | `#木制种植盆` |
| 纸箱 | `#木制种植盆` | `#纸箱种植盆` |
| 轮胎 | `#纸箱种植盆` | `#轮胎种植盆` |
| 铁皮 | `#轮胎种植盆` | `#铁皮种植盆` |
| 泡沫 | `#铁皮种植盆` | `#泡沫种植盆` |

The target Planting Pot Equipment page contains all six real anchors, from `#简易种植盆` through `#泡沫种植盆`, and no `#简介` anchor. Its internal “简易种植盆 → #简介” link is therefore broken and should target `#简易种植盆`.

## Validation and limits

- Read the required project, planning, reference, debug, review, and document-governance authorities before tracing.
- Confirmed the current rendered Wiki text, link targets, detail-page contradictions, and Drone revision history through the user's signed-in Chrome session.
- Traced the current public capture's ILSpy code, AssetRipper configuration, and extracted dialogue. No official binary or copied method body was added to the repository.
- Recomputed the fishing expectation directly from the native probability sequence.
- No game launch was needed: this is a static code/configuration and live-page fact audit, not runtime acceptance.
- No Wiki page was edited or saved.
