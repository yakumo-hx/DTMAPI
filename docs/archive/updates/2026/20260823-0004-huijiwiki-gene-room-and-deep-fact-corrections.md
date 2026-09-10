# 20260823-0004：HuijiWiki 基因、房间效果与四项事实深查

## Metadata

- Update ID: `20260823-0004`
- Date: `2026-08-23`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求基于当前 public `1.00.05` 实码深查既有审查项 1、3、6、7，明确排除信件大修和平台修改，并在确认有独立价值后建立 22 种基因总表、为建筑物页增加房间效果横向比较。

## Scope

- 以线上 HuijiWiki 当前源码和渲染结果为页面权威，以本地 `24788406_public_F06183 / 1.00.05` 配置、Yarn 事件图和窄范围反编译调用链为玩法事实证据。
- 深查分形作物、小飞象章鱼/鬼头刀解锁、奥兰多宝物商店解锁和鲟鱼售价口径。
- 创建缺失的 `基因` 概念页，覆盖全部 22 种基因，并区分固定加值、倍率项、向下取整、周期/概率与条件生效。
- 在 `建筑物` 增加植物大棚、温室、电力控制室、两类畜棚和四级地窖的房间效果总览。
- 不修改 `信件`；不修改 `平台`。用户确认且调用链证明手工平台无专门高度上限，数值 15 只用于建筑自动生成的承重柱。

## Changed Files

- `docs/reviews/code/2026/20260823-0003-huijiwiki-current-content-gap-audit.md`
- `docs/updates/2026/20260823-0004-huijiwiki-gene-room-and-deep-fact-corrections.md`
- `docs/updates/INDEX-2026-08.md`

没有修改 DTMAPI、游戏、Mod 或 Runtime 源码。官方逆向材料只作本地只读证据，没有复制到提交内容或 Wiki。

## Deep-trace Result

### 1. 分形作物

- `plant_tbcropgene.json:40-55` 配置 `crop_output_addition = 0.4`；`CropGeneFunctionFractalCrop.cs:26-29` 把它累加到 `finalCountMultiplication`。
- `CropGeneUtils.CalcFinalOutput` 依次计算 `origin + floor(origin * multiplier sum) + fixed addition sum`，最后保证至少为 1。
- `Crop.OriginGenCropOutput` 将每个 `RangedItem.randomCount` 送入上述基因结算。枚举当前 `plant_tbseed.json` 后，所有作物的原始单次产量上限都是 4。
- 因而旧页面的“基础产量达到 3 个时多 1 个”在当前内容范围内并未算错，只是省略了真实可扩展公式。修订改为同时说明 `⌊基础产量 × 40%⌋` 与当前 1～4 的实际结果，没有虚构现版本不存在的 `+2` 案例。

### 3. 小飞象章鱼和鬼头刀解锁

- `fishing_tbfish.json:1062-1064,1107-1109` 对两者均配置 `default_unlock = false`。
- `ResourceManager` 的新实例把 `unlockFishes` 初始化为空集合；`CheckFishUnlocked` 对 false-default 鱼只在集合包含该 ID 时返回真。
- `DolocAPI.RollFish` 和 `RollFishByRarity` 都在候选池阶段调用 `CheckFishUnlocked`，不是只影响图鉴或界面显示。
- `terraforming_function.yarn:273-282` 在 `terraforming_terravalley`（岩芯样本环境改造）节点显式解锁两者并完成 `MISSION_PROGRESS.TerraValley`。
- `version_patch.yarn:345-348` 只为已经完成该任务的旧存档补发解锁；新游戏 `Assets/TextAsset/unlock.txt` 没有任何鱼类解锁。
- 结论：二者不是默认解锁。冲突说法最可能来自 Wiki 手写映射漏项，或观察存档早已完成岩芯样本，而不是另一条正式默认路径。

### 6. 奥兰多宝物商店

- 商店共有 5 个默认商品和 28 个锁定商品，锁定条件分为四类：
  - 17 种怪物雕像：`monster_statue_guide.asset` 的全局首次击杀监听直接执行 `unlock_store_item orlando_exchange_shop statue_*`；
  - 8 种物品：`store_tbstoreitemunlock.json` 的 `ObtainItem`；
  - 菌菇帽：完成阵营任务 `毒蘑菇，危险又神秘！`；
  - 红色绒球帽、小鸟帽：读取对应兑换码奖励邮件。
- `StoreManager.AfterLoadData` 会按获得物品、完成阵营任务和读取邮件三类持久事件重新核对并解锁；怪物雕像是独立的任务图命令路径，不在该 JSON 表中。
- 因此旧页面“所有锁定商品都要先获得一件”的总括是系统性错误，不能只补一两个例外。

### 7. 鲟鱼售价口径

- `item_tbitem.json` 中鲟鱼、小飞象章鱼和鬼头刀均为 1500G；所有普通钓鱼候选中没有更高价格。
- `dumbo_octopus_variant`（小紫伞章鱼）为 2000G。`TbFarmFish.IsSubspeciesFish` 以“存在于养殖鱼表、但不存在于普通鱼表”判为亚种，因此它只能由养殖获得，不能直接钓到。
- 精确表述应为：鲟鱼与另两种鱼并列“可直接钓获鱼类”最高价；若把养殖亚种也计入鱼类道具，小紫伞章鱼更高。

### 平台排除项

- `settings_tbglobalparameter.json:108` 的 `max_support_height = 15` 被 `BuildingBuilder` 用于验证自动生成的 `BuildingSupport.ColumnHeights`。
- `PlatformBuilderHelper` 只检查 2～12 的宽度、地形/房间位置、平台有效性和成本，从不读取 `MaxSupportHeight`。
- 结论：当前 Wiki 的“平台没有高度限制”正确；本轮未修改平台页。

## New Overview Content

### 基因

- `plant_tbcropgene.json` 当前有 22 行：16 种默认可活性化，寄生体、野蛮生长、雨露均沾、昙华、气生根、富氧化由 `生物质目录` 环境改造统一解锁。
- `settings_tbglobalparameter.json` 的 `max_gene_count = 3` 给出每株作物的基因上限。
- 未创建正文的 `分类:基因胶囊` 虽会自动列出 22 个成员，但其构成是 21 个单基因胶囊加 1 个复合胶囊；`无籽` 因 `capsule_item` 为空而缺席，故不能代替 22 种基因概念总表。
- 新页复用现有 `{{基因|...|仅描述=true}}` 数据模块生成效果文本，另设“数值与结算类型”列，避免把动态效果复制成另一份手写真相。

### 建筑物房间效果

- `room_tbroomeffect.json` 给出植物大棚 `+0.2`、温室 `+0.1` 且忽略季节、电力控制室发电 `+0.4`、畜棚生长 `-0.4`。
- 地窖对所有作物 `-0.6`，对菌菇另加 `+1`，所以菌菇净效果为 `+0.4`；1～2 级用 `cellar`，3～4 级用 `cellar_up`，后者再覆盖烘干箱和电力烘干箱。
- `IRecipeGroup.TimeRatio = BaseTimeRatio * (1 + TimeAddition)`，所以 `-0.2` 是耗时变为 80%，不是速度增加 20%。
- `ElectronicComponentGenerator` 对室内发电机直接读取当前房间加值；室外发电机检查其覆盖格和邻接格中的建筑，并只取最高的相邻房间发电加值。

## External Wiki Revisions

| Revision | Page | Result |
| --- | --- | --- |
| `25782` | `模块:Fishing/FishingUtils` | 给小飞象章鱼、鬼头刀补上岩芯样本环境改造映射 |
| `25783` | `模块:Plant/PlantUtils` | 分形作物显示 40% 倍率、向下取整和当前 1～4 产量范围 |
| `25784` | `奥兰多的宝物商店` | 把锁定商品拆成首次击败怪物、获得物品、完成阵营任务、读取兑换码邮件四类 |
| `25785` | `鲟鱼` | 将最高价限定为可直接钓获鱼，并补充三者并列和养殖亚种 2000G |
| `25786` | `基因` | 新建 22 种基因总表、结算规则、活性化条件和设备入口 |
| `25787` | `建筑物` | 新增房间效果总览及地窖净加值、发电范围、加工耗时说明 |
| `25788` | `基因` | 统一富氧化的“精力”术语，并把昙华条件明确为成熟后的首个凌晨收获 |

## Validation

- 逐个保存前使用 HuijiWiki 预览或差异页检查；现有页面的差异只包含目标增补，`基因` 新页预览完整生成 22 行。
- 保存后通过 MediaWiki API 读取每个页面的最新 revision ID、编辑摘要和源码；`基因`、`建筑物` 的线上源码与提交草稿逐字一致。
- 再次打开七个受影响的渲染入口：小飞象章鱼、鬼头刀、基因胶囊（分形作物）、奥兰多的宝物商店、鲟鱼、基因、建筑物。所有目标文本出现，基因表为 22 行，没有 Lua、模板或表达式错误。
- 对当前 22 个基因、全部当前作物原始产量、Orlando 28 个锁定商品、普通鱼与养殖亚种价格、四级地窖房间配置做了独立枚举核对。
- `tools/scripts/check-doc-governance.ps1`: PASS (`6934` checks).
- 未启动游戏；这些结论由静态配置、事件图和直接调用链完整决定，不需要玩家存档验证。

## Evidence

- Current reverse baseline: `references/doloc-town/reverse/builds/24788406_public_F06183/`.
- Exact Wiki revision chain: `25782`, `25783`, `25784`, `25785`, `25786`, `25787`, `25788`; the MediaWiki API returned the expected page, parent revision, editor, timestamp, summary and latest source for every row.
- Post-save rendered checks were performed through the user's signed-in Chrome session against the live Website, not the local Wiki snapshot.
- The corrected reasoning and narrow source locations remain in [the owning Review](../../reviews/code/2026/20260823-0003-huijiwiki-current-content-gap-audit.md).

## Related Records

- [Current content gap audit](../../reviews/code/2026/20260823-0003-huijiwiki-current-content-gap-audit.md)
- [Fishing, drone and platform fact audit](../../reviews/code/2026/20260823-0001-huijiwiki-fishing-drone-platform-fact-audit.md)
- [Public 1.00.05 reverse capture](20260823-0002-public-10005-full-reverse-capture.md)

## Rollback Notes

- HuijiWiki 的七个修订可按 revision `25782`～`25788` 逐项撤销；其中 `25788` 依赖 `25786` 新建的基因页，其余页面彼此独立。不需要回退无关页面，也不应把平台或信件并入回退。
- 本地仅有文档记录和月度索引变化，可用普通 Git 反向提交回滚；不得删除或覆盖本地官方 reverse capture。

## Follow-Up

- `信件` 仍按用户要求留给该系统的大修，不把本轮发现扩写到邮件页。
- `平台` 的高度结论已经纠正为负向发现，不再提出 15 格修改。
- 原审查中 `无人机枪械` 老表、缺失的小鸟帽/神秘旋律页面和其余链接清理仍是独立候选，不由本 Update 声称完成。
