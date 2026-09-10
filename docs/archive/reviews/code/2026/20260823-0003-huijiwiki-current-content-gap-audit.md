# HuijiWiki current content gaps and live-page audit

- Date: 2026-08-23
- Review Status: `recorded`
- Scope: current Chinese HuijiWiki gameplay facts, missing high-value pages, stale hand-authored summaries, links, anchors, maintenance categories, and opportunities for data-driven consolidation
- Source request: identify worthwhile Wiki corrections and additions after the previous explicit repair batches, treating the changing live Website as authority and current public game code/configuration as fact evidence
- Game baseline: Steam build `24788406`, branch `public`, semantic version `1.00.05`, capture `24788406_public_F06183`
- Live Wiki writes at the original audit boundary: none. The later, user-authorized follow-up and exact Wiki revisions are owned by [Update 20260823-0004](../../../updates/2026/20260823-0004-huijiwiki-gene-room-and-deep-fact-corrections.md).
- Local Wiki snapshots: discovery aid only; no conclusion in this review treats a snapshot as current authority

## Review outcome

The current online Wiki has already absorbed most of the recent price, recipe, fishing, drone-assist, favorability-reward, and navigation corrections. The most useful next work is no longer a broad pass over ordinary item prices. It is concentrated in four classes:

1. deterministic mechanism errors that remain in rendered pages;
2. hand-authored aggregate tables that have diverged from data-driven item pages;
3. newly added official items whose data and images exist but whose main pages do not;
4. systemic link, anchor, and maintenance-classification gaps that hide unfinished content.

The highest-value factual corrections are:

- `基因胶囊（分形作物）` uses `floor(base output * 40%)`; because every current raw crop-output range tops out at four, the old visible `+1` description was behaviorally correct for current content but hid the scalable formula and rounding boundary;
- `无人机枪械` is still an EA `0.92.34` table and disagrees with current configuration in many rows;
- `小飞象章鱼` and `鬼头刀` are unlocked by the 岩芯样本 terraforming step, not by default;
- `平台` correctly says player-built platforms have no dedicated height limit; the configured value 15 is consumed by building-generated support-column validation, not `PlatformBuilderHelper`;
- `信件` labels two redemption-code mails as unconditional;
- `奥兰多的宝物商店` incorrectly says every locked item is unlocked by obtaining one copy;
- `鲟鱼` is not the highest-selling fish once the farmed variant 小紫伞章鱼 is included;
- the `小鸟帽` and `神秘的旋律＃4` item pages are still missing.

The highest-value structural corrections are:

- rebuild the old drone-weapon tables from current data rather than patching individual cells;
- add stable per-mail row anchors based on the unique email ID;
- import the store-item-unlock data so store rows can display their actual conditions;
- add a real `基因` overview and a cross-building room-effect table;
- fix the 56 confirmed dead section links and make incomplete email conditions visible to the maintenance dashboard.

## Authority and method

Live-page inspection used the user's signed-in Chrome session and the current HuijiWiki Website/API on 2026-08-23. Important source revisions seen during the final pass included:

- `钓鱼` revision `25745` (2026-08-23 00:51:36 UTC);
- `平台` revision `25741` (2026-08-23 00:32:39 UTC);
- `无人机枪械` revision `5775` (2025-07-13 02:55:34 UTC);
- `基因胶囊（分形作物）` revision `22142` (2026-03-10 09:15:33 UTC; its detailed effect text is rendered through a module/data template);
- `小飞象章鱼` revision `25577` and `鬼头刀` revision `25173`;
- `信件` revision `22631`, `奥兰多的宝物商店` revision `21194`, and `鲟鱼` revision `25176`.

The live API reported `小鸟帽` and `神秘的旋律＃4` as missing during the final pass.

Current game evidence comes from:

- `references/doloc-town/reverse/builds/24788406_public_F06183/decompiled/Assembly-CSharp`
- `references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas`
- `references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/GameDatabase`

This review records conclusions and narrow code/configuration locations without redistributing official assemblies or copied decompiled source.

## 1. Remaining deterministic gameplay errors

### 1.1 Fractal Crop uses a proportional term, but the current `+1` result was not false

Live page: <https://doloctown.huijiwiki.com/wiki/基因胶囊（分形作物）>

The rendered effect said that a qualifying harvest receives one additional product. Current implementation adds a `0.4` final-count multiplier and calculates the added count with a floor operation:

- `plant_tbcropgene.json:40-55`: `fractal_crop`, `crop_output_addition = 0.4`;
- `DolocTown/CropGeneFunctionFractalCrop.cs:26-29`: adds the configured value to `finalCountMultiplication`;
- `DolocTown/CropGeneUtils.cs:205-211`: computes `FloorToInt(originCount * finalCountMultiplication)`.

`Crop.OriginGenCropOutput` passes each configured `RangedItem.randomCount` through the gene pipeline. Enumerating every current `plant_tbseed.json` crop output gives a maximum raw count of four. Consequently, the existing threshold description was behaviorally exact for the current content: raw counts 1–2 add zero and 3–4 add one. It would become incomplete only if a later crop had a raw count of at least five.

Code-faithful, future-proof wording:

> 实际额外产量为 `⌊当次基础产量 × 40%⌋`。当前所有作物的单次基础产量均不超过 4 个，因此实际表现为基础产量 1～2 个时不增加、3～4 个时额外增加 1 个。

The localized description's reference to harvests of at least three follows naturally from the floor result; it is not a separate fixed `+1` rule. This was therefore a documentation-depth improvement, not a correction of a wrong current outcome.

### 1.2 Drone weapon aggregate tables are obsolete

Live page: <https://doloctown.huijiwiki.com/wiki/无人机枪械>

The page explicitly says its data comes from EA `0.92.34`. Its nine-row performance table and acquisition table now conflict with current configuration and omit four weapons.

Representative differences from `drone_tbdroneweapon.json`:

| Weapon | Live old value | Current value |
| --- | --- | --- |
| 重型手枪 | magazine 10, critical 5% | magazine 8, critical 10% |
| 双管发射器 | attack 4 | attack 6 |
| 连续发射器 | attack 4 | attack 5 |
| 狙击发射器 | attack 30 | attack 25 |
| 追踪发射器 | speed 1.5, magazine 4 | speed 3, magazine 15 |
| 飞鱼发射器 | attack 8, critical 0% | attack 6, critical 25% |
| 机械大剑 | attack 20 | attack 16 |

The missing weapons are `采矿枪`, `强弓发射器`, `柯奥德的枪`, and `家用木工电锯套组`. Acquisition rows are also stale: for example, current 狙击/飞鱼 exchanges use 10 materials and 5000G, not the displayed 20 materials and 3000G.

This page should be rebuilt from `drone_tbdroneweapon.json`, `item_tbitem.json`, and current store data, preferably through the same source used by individual weapon cards. Row-by-row manual repairs would preserve the divergence mechanism.

Related residual error: <https://doloctown.huijiwiki.com/wiki/无人机> displays the 简易发射器 buy/sell pair as `100/500`; current `item_tbitem.json:1368-1380` and the individual item page both give `100/50`.

### 1.3 Dumbo Octopus and Mahi-mahi are not default-unlocked

Live pages:

- <https://doloctown.huijiwiki.com/wiki/小飞象章鱼>
- <https://doloctown.huijiwiki.com/wiki/鬼头刀>
- <https://doloctown.huijiwiki.com/wiki/钓鱼>

At the original audit boundary, both item cards rendered `游戏流程 解锁条件：默认解锁`; their rows in the fishing overview had an empty unlock column. Current game-code evidence is unambiguous:

- `fishing_tbfish.json:1062-1064` and `1107-1109` set both `default_unlock` fields to false;
- `ResourceManager.cs:22-25` creates a new save's `unlockFishes` as an empty set, and `ResourceManager.cs:68-84` requires a false-default fish to be present in that set;
- `DolocAPI.cs:2397-2416` and `2438-2461` filter both ordinary and rarity-specific fishing rolls through `CheckFishUnlocked`;
- `GameDatabase/Dialogue/interactable_objects/terraforming_function.yarn:273-282` unlocks both fish in `terraforming_terravalley`, the 岩芯样本 activation node;
- `version_patch/version_patch.yarn:346-347` retroactively unlocks them only for old saves that already completed the relevant mission.

The normal new-game `Assets/TextAsset/unlock.txt` contains no fish unlock. The version patch is save migration, not an alternate default-unlock declaration. The likely source of the conflicting claim was the Wiki module's incomplete hand-maintained mapping (and saves that had already completed 岩芯样本), not the runtime rule.

The required wording was `岩芯样本环境改造` on both cards and `岩芯样本` in the two fishing-table rows. All other eight implemented fish with `default_unlock = false` were checked and their live unlock labels were already correct.

### 1.4 The platform-height correction is retracted

Live page: <https://doloctown.huijiwiki.com/wiki/平台>

The original audit incorrectly mapped `settings_tbglobalparameter.json:max_support_height = 15` onto manual platform placement. Deeper call-site review separates the two systems:

- `BuildingBuilder.cs:313-318` creates a `BuildingSupport` and rejects it when the generated column height exceeds `MaxSupportHeight`;
- `PlatformBuilderHelper.cs:49-72` validates manual platforms by configured width, terrain/room occupancy, platform validity, and cost, and never reads `MaxSupportHeight`;
- `settings_tbglobalparameter.json:111-115` separately gives the manual builder a width range of 2–12.

Therefore the live statement that manual platforms have no height limit is correct. The value 15 limits automatically generated building support columns. Per the user's explicit instruction, no live Platform edit belongs to this follow-up. The earlier stone-platform navigation and planting-pot anchor corrections remain closed and were not reopened.

### 1.5 Redemption-code emails are not unconditional

Live page: <https://doloctown.huijiwiki.com/wiki/信件>

The table renders `code_game_studio_4` and `code_celebrate_1` as `无条件` because the page provides neither ID as a parameter and the module falls back to that text.

Current event/configuration evidence:

- `GameDatabase/Dialogue/interactable_objects/city.yarn:592-603` maps telephone codes `45451` and `12315` to the two emails;
- `email_tbemail.json:4825-4845` attaches `cd_52` to `code_game_studio_4`;
- `email_tbemail.json:4851-4871` attaches `bird_hat` to `code_celebrate_1`.

Suggested parameters:

```wikitext
|code_game_studio_4 = 在任意电话亭输入 45451
|code_celebrate_1 = 在任意电话亭输入 12315
```

The same pass should review the remaining `code_*` emails and can add a compact telephone-code overview.

### 1.6 Orlando store unlocks use four paths

Live page: <https://doloctown.huijiwiki.com/wiki/奥兰多的宝物商店>

The current lead said every locked product is unlocked by obtaining one copy through normal play. The complete current store contains four distinct paths:

- `ObtainItem`: obtain the associated item;
- `CompleteFactionMission`: 菌菇帽 is unlocked by completing `毒蘑菇，危险又神秘！`;
- `ReadEmail`: 红色绒球帽 and 小鸟帽 are unlocked by reading their code-reward emails.
- `monster_statue_guide.asset`: 17 global first-kill listeners directly run `unlock_store_item orlando_exchange_shop statue_*` for the corresponding monster statues.

For the three table-driven conditions, `StoreManager.AfterLoadData` reconciles the saved event/mission state on load and calls `UnlockStoreItem`. The first-kill statue path bypasses `store_tbstoreitemunlock.json` and issues the unlock command from the mission graph. The Orlando store has five default rows and 28 locked rows: 17 statue rows plus eight obtain-item rows, one faction-mission row, and two read-email rows.

The online `Data:Store/tbstoreitemunlock.json` page is missing, so `模块:Store/StoreTable` can currently show only generic `默认/需解锁` labels. Importing this data and rendering the actual per-row condition is the durable correction.

### 1.7 Sturgeon does not have the absolute highest fish price

Live page: <https://doloctown.huijiwiki.com/wiki/鲟鱼>

At the original audit boundary, the lead said 鲟鱼 had the highest selling price among fish. Current `item_tbitem.json` gives:

- 鲟鱼: 1500G;
- 小飞象章鱼 and 鬼头刀: 1500G;
- the farm-only variant 小紫伞章鱼: 2000G.

The result is not merely a Wiki-category ambiguity. `TbFarmFish.IsSubspeciesFish` classifies a farm-fish record as a subspecies when its ID is absent from the normal `TbFish` table; `dumbo_octopus_variant` meets that test and therefore cannot be rolled by ordinary fishing. Among directly catchable fish, 1500G is the maximum and the three named fish tie. Across all fish items, the 2000G farm-only subspecies is higher.

Suggested wording:

> 鲟鱼售价为 1500G，与小飞象章鱼、鬼头刀相同；若计入养殖获得的变种鱼，小紫伞章鱼售价 2000G 更高。

## 2. High-value missing pages and overview content

### 2.1 Small Bird Hat

Missing page: <https://doloctown.huijiwiki.com/wiki/小鸟帽>

The live navigation, mail table, store table, and icon already reference this 1.00.04 item. Current facts suitable for a page are:

- `item_tbitem.json:3254-3279`: hat item, sell price 500G;
- `player_tbhat.json:459` and `player_tbagentequipmentskill.json:413-420`: the `bird` equipment skill;
- `DolocTown/EnvObjectBird.cs:34-49`: while the skill is equipped, touching an environmental bird does not make it fly away or execute its drop path;
- `city.yarn:598-603` and `email_tbemail.json:4851-4871`: code `12315` sends one hat;
- `store_tbstoreitemunlock.json:129-132`: reading that email is the Orlando-store unlock condition;
- `store_tbexchangestore.json:2148-2163`: after unlock, one hat costs three corn and storage `0` means unlimited exchange.

Suggested lead:

> 小鸟帽是一种帽子。装备后，玩家接触环境中的小鸟时不会使其飞走。可在任意电话亭输入 12315，于“订单配送完成通知”中领取；读取该邮件也是奥兰多宝物商店商品的解锁条件，解锁后可用 3 个玉米兑换。

The practical tradeoff is worth stating: while worn, the prevented fly-away path also does not produce the bird's normal drop; removing the hat while still touching the bird invokes the ordinary touch path.

### 2.2 Mysterious Melody #4

Missing page (full-width number sign is needed in the Wiki title): <https://doloctown.huijiwiki.com/wiki/神秘的旋律＃4>

- `item_tbitem.json:23633-23658`: item `cd_52`;
- `sound_tbcd.json:251-256`: unlocks `32.Duallel Spirit`;
- `city.yarn:592-597`: telephone code `45451`;
- `email_tbemail.json:4825-4845`: the email attaches one `cd_52`.

Suggested lead:

> 神秘的旋律＃4是一种特殊物品，使用后可在随身听中解锁音乐“32.Duallel Spirit”。可在任意电话亭输入 45451，于“订单配送完成通知”中获得。

### 2.3 Gene overview

Missing page at the original audit boundary: <https://doloctown.huijiwiki.com/wiki/基因>

At the original audit boundary, detailed facts sat across 22 gene-capsule pages and several devices while `种植` had no gene overview. A useful data-backed article could cover:

- the three-gene maximum (`settings_tbglobalparameter.json`, `max_gene_count = 3`);
- a 22-row effect table from `plant_tbcropgene.json`;
- reading, cloning, compressing, synthesizing, and activation flow;
- inheritance probabilities and special groups;
- an explicit column distinguishing fixed addition, multiplier, and rounding behavior.

The uncreated `分类:基因胶囊` still auto-lists 22 member pages, but it is an item-category listing rather than a gene table: 21 are single-gene capsule items and the remaining member is `复合型基因胶囊`. It omits `无籽`, whose `plant_tbcropgene.json` row has an empty `capsule_item`. The category therefore does not replace a 22-gene concept overview.

### 2.4 Building room-effect overview

Live overview: <https://doloctown.huijiwiki.com/wiki/建筑物>

At the original audit boundary, individual building pages contained much of the data but the main page had no comparison section. A compact table sourced from `room_tbroomeffect.json` should compare planting, season, generation, and processing effects for 植物大棚、温室、电力控制室、畜棚, and cellar upgrades. This would make cross-building decisions discoverable and reduce duplicated prose.

The code-level comparison must show net effects, not copy isolated fields: both cellar profiles apply `growth_addition = -0.6` to every crop and an additional `growth_addition_fungus = +1`, so fungi have net `+0.4`. Cellar levels 1–2 use `cellar`; levels 3–4 use `cellar_up`, whose only extra processing targets are 烘干箱 and 电力烘干箱. `IRecipeGroup.TimeRatio` multiplies base time by `1 + TimeAddition`, making `-0.2` a duration of 80%, not a speed increase of 20%.

## 3. Link, anchor, and maintenance audit

The live structural scan covered 982 main pages and 228 redirects. It found 56 static section links whose target ID is absent from the rendered target page, affecting 45 source pages and 29 distinct targets.

Largest clusters:

- `地图#旧城市废墟地图`: 10 links; the map overview currently has no old-city section;
- `信件#订单配送完成通知`: 6 links;
- `信件#关于失而复得的矿稿`: 5 links;
- `平台#简介`: 4 links (observed structural residue, explicitly excluded from the follow-up edit scope by the user);
- `阵营#撑犁子部落`: 3 links; the current anchor is `#撑犁子`.

Direct low-risk replacements include:

- `成就#狸猫换太子` -> `成就#狸猫和太子`;
- `澳柯玛#好感度事件` -> `澳柯玛#好感事件`;
- `任务#救救柯奥德` -> `任务#救救科奥德`;
- `任务#商埠维修` -> `任务#修缮商埠`;
- `地点#码头右侧` -> `地点#码头东侧`;
- `阵营交易#康提基群岛联盟` -> `阵营交易#康提基`;
- `设备#垃圾分解机` -> the standalone item page;
- the `种植` link to nonexistent `年历与天气#季节` -> an existing year/season anchor or a newly created stable anchor.

### 3.1 Email anchor root cause

The email page creates section IDs for senders, not for individual mail rows. Consequently, cross-links for the recently corrected favorability rewards, as well as repeated titles such as `订单配送完成通知`, land at the page top. The durable fix is to generate a stable row anchor from the unique email ID. Title-derived anchors alone are insufficient because titles repeat.

### 3.2 Redirect and typo residue

- standard BrokenRedirects and DoubleRedirects were both empty;
- `多洛可商埠` is nevertheless a fragment redirect to nonexistent `阵营#多洛可商埠`;
- `硬币` links `旧城"守护者"` instead of the typographic title `旧城“守护者”`;
- `霍特` links a nonexistent item `炸薯条`; the current item page is `薯条`;
- the email module links sender `达达！` as a page title; map it to `达达` or add a redirect.

### 3.3 Maintenance visibility

- `信件` still supplies 38 trigger-condition parameters as `待补充`; at least 37 rendered rows show that text, but the page is not in `内容待补充`;
- the home maintenance panel therefore reports only one page needing supplementation while 63 pages are in `需要校对`;
- a module-level detector should add the supplementation category when any mail condition remains `待补充`, and the home panel should expose the proofread count;
- there are 11 currently uncategorized main pages, and 10 pages using the location information template lack a thematic category; adding the category in the template is lower-cost than editing each page;
- `钓鱼概率计算器` is the sole dead-end page and should link back to `钓鱼`, `鱼类`, or the tool index.

The standard WantedPages list contains 345 targets; 72 have at least 20 incoming links. High-frequency, data-backed missing furniture/item pages include 躺椅 (212 incoming links), four mushroom/furniture entries with 195 each, `多洛可之王`雕像 (193), four wallpapers (119), and three mixed-seed bags (118). These are appropriate for automated page scaffolding after the factual corrections above.

WantedCategories entries are not broken category functionality: HuijiWiki still lists category members on an uncreated category page. They indicate missing descriptions/parent categories and are lower priority. WantedFiles is likewise dominated by site/help examples and is not a high-value game-content queue.

## 4. Stale prose cleanup

The following old-version wording remains online:

- `畜牧与渔业` uses `目前版本0.94.09` twice; the fish-tank 30-minute interval remains correct in 1.00.05, so retain the fact and remove the stale framing;
- `种子压缩机` and `基因合成器` use `截至编辑时0.95.14`;
- `工程机库` and `镇政厅` use unversioned `目前版本` wording;
- `电力` still mentions a possible bug immediately before explaining that the formal-release behavior fixed the old issue.

The editorial rule should remain: use current-data prose for current facts; retain an old version only inside an explicitly historical note with a bounded version range.

## 5. Useful negative findings

The audit deliberately excluded already-correct areas:

- the live `Data:Item/tbitem.json` and current public `item_tbitem.json` matched across 990 core records;
- all 111 current buying prices and 103 current selling prices were imported, and the affected item pages showed no residual old hard-coded prices in the price fields;
- current 1.00.04 recipe and fish-economy changes sampled in the audit are already represented;
- the current `钓鱼` timing/mechanism section is code-faithful;
- the current `保底机制` page matches configuration and counter/reset behavior;
- `伊甸果精华` is not blank: its folded live content already lists all nine current sources;
- the previously reported stone-platform navigation link and planting-pot anchor shift are fixed.

This negative evidence matters because broad price or recipe rewrites would now create more risk than value. The remaining work is concentrated in the pages and modules above.

## Follow-up route

The user-authorized live corrections and additions that followed this audit are owned by [Update 20260823-0004](../../../updates/2026/20260823-0004-huijiwiki-gene-room-and-deep-fact-corrections.md). That Update records the exact Wiki revisions, validation, rollback route, and explicit exclusions. This Review retains the evidence and corrected reasoning only.

## Validation and limits

- Read the project, reverse-reference, review, and documentation-governance authorities before tracing.
- Used the live Website/API through Chrome as the current Wiki authority; local Wiki snapshots were not used to establish live defects.
- Checked current public configuration, serialized dialogue events, and narrow decompiled behavior for deterministic claims.
- `tools/scripts/check-doc-governance.ps1`: PASS (`6903` checks).
- No game launch was needed for static configuration, event graph, or link/DOM claims.
- At the original audit boundary, no Wiki page was edited or saved. The later follow-up is not part of that audit-only validation statement.
- At the original audit boundary, no local project source was changed and this Review was the sole task artifact. The follow-up documentation changes are recorded by the owning Update above.
